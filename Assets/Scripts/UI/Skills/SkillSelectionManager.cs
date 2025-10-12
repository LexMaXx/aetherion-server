using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Менеджер выбора скиллов в Character Selection Scene
/// Управляет библиотекой скиллов (6 шт) и экипированными слотами (3 шт)
/// </summary>
public class SkillSelectionManager : MonoBehaviour
{
    [Header("UI Слоты")]
    [Tooltip("Слоты библиотеки скиллов (6 штук)")]
    [SerializeField] private List<SkillSlotUI> librarySlots = new List<SkillSlotUI>(6);

    [Tooltip("Слоты экипированных скиллов (3 штуки)")]
    [SerializeField] private List<SkillSlotUI> equippedSlots = new List<SkillSlotUI>(3);

    [Header("Skill Database")]
    [Tooltip("База данных всех скиллов")]
    [SerializeField] private SkillDatabase skillDatabase;

    private CharacterClass currentCharacterClass;
    private List<int> equippedSkillIds = new List<int>(3); // Для сохранения в MongoDB

    void Start()
    {
        // Если база не назначена в Inspector, пытаемся загрузить из Resources
        if (skillDatabase == null)
        {
            Debug.LogWarning("[SkillSelectionManager] SkillDatabase не назначена в Inspector, загружаю из Resources...");
            skillDatabase = Resources.Load<SkillDatabase>("SkillDatabase");

            if (skillDatabase == null)
            {
                Debug.LogError("[SkillSelectionManager] ❌ SkillDatabase не найдена! Проверь что файл существует в Assets/Resources/SkillDatabase.asset");
                return;
            }
            else
            {
                Debug.Log("[SkillSelectionManager] ✅ SkillDatabase загружена из Resources");
            }
        }

        Debug.Log("[SkillSelectionManager] ✅ Готов к работе. Ожидаю выбора класса от CharacterSelectionManager");

        // НЕ загружаем скиллы сразу - ждем когда CharacterSelectionManager
        // загрузит персонажей с сервера и вызовет LoadSkillsForClass()
    }

    /// <summary>
    /// Загрузить скиллы для выбранного класса
    /// </summary>
    public void LoadSkillsForClass(CharacterClass characterClass)
    {
        currentCharacterClass = characterClass;

        if (skillDatabase == null)
        {
            Debug.LogError("[SkillSelectionManager] SkillDatabase не назначена!");
            return;
        }

        // Получаем все скиллы для класса
        List<SkillData> classSkills = skillDatabase.GetSkillsForClass(characterClass);

        Debug.Log($"[SkillSelectionManager] Загружаю скиллы для {characterClass}: {classSkills.Count} шт");

        // Проверяем что слоты назначены
        if (librarySlots == null || librarySlots.Count == 0)
        {
            Debug.LogError("[SkillSelectionManager] ❌ librarySlots не назначены в Inspector!");
            return;
        }

        Debug.Log($"[SkillSelectionManager] Найдено библиотечных слотов: {librarySlots.Count}");

        // Заполняем библиотеку
        for (int i = 0; i < librarySlots.Count; i++)
        {
            if (librarySlots[i] == null)
            {
                Debug.LogError($"[SkillSelectionManager] ❌ librarySlots[{i}] = null!");
                continue;
            }

            if (i < classSkills.Count)
            {
                Debug.Log($"[SkillSelectionManager] Устанавливаю скилл '{classSkills[i].skillName}' в слот {i}");
                librarySlots[i].SetSkill(classSkills[i]);
            }
            else
            {
                librarySlots[i].ClearSlot();
            }
        }

        // Автоматически экипируем первые 3 скилла
        AutoEquipDefaultSkills(classSkills);
    }

    /// <summary>
    /// Автоматически экипировать первые 3 скилла
    /// </summary>
    private void AutoEquipDefaultSkills(List<SkillData> skills)
    {
        if (equippedSlots == null || equippedSlots.Count == 0)
        {
            Debug.LogError("[SkillSelectionManager] ❌ equippedSlots не назначены в Inspector!");
            return;
        }

        Debug.Log($"[SkillSelectionManager] Автоэкипировка первых 3 скиллов. Слотов: {equippedSlots.Count}");

        for (int i = 0; i < equippedSlots.Count && i < skills.Count; i++)
        {
            if (equippedSlots[i] == null)
            {
                Debug.LogError($"[SkillSelectionManager] ❌ equippedSlots[{i}] = null!");
                continue;
            }

            Debug.Log($"[SkillSelectionManager] Экипирую '{skills[i].skillName}' в слот {i}");
            equippedSlots[i].SetSkill(skills[i]);
        }

        UpdateEquippedSkillIds();
    }

    /// <summary>
    /// Вызывается когда скиллы изменены (Drag & Drop)
    /// </summary>
    public void OnSkillsChanged()
    {
        UpdateEquippedSkillIds();
        Debug.Log($"[SkillSelectionManager] Скиллы обновлены. Экипировано: {equippedSkillIds.Count}");
    }

    /// <summary>
    /// Обновить список ID экипированных скиллов
    /// </summary>
    private void UpdateEquippedSkillIds()
    {
        equippedSkillIds.Clear();

        foreach (SkillSlotUI slot in equippedSlots)
        {
            SkillData skill = slot.GetSkill();
            if (skill != null)
            {
                equippedSkillIds.Add(skill.skillId);
            }
        }

        Debug.Log($"[SkillSelectionManager] Экипированные скиллы: [{string.Join(", ", equippedSkillIds)}]");
    }

    /// <summary>
    /// Получить ID экипированных скиллов (для сохранения в MongoDB)
    /// </summary>
    public List<int> GetEquippedSkillIds()
    {
        UpdateEquippedSkillIds();
        return new List<int>(equippedSkillIds);
    }

    /// <summary>
    /// Загрузить ранее экипированные скиллы (из MongoDB)
    /// </summary>
    public void LoadEquippedSkills(List<int> skillIds)
    {
        if (skillDatabase == null) return;

        // Очищаем экипированные слоты
        foreach (SkillSlotUI slot in equippedSlots)
        {
            slot.ClearSlot();
        }

        // Загружаем скиллы по ID
        for (int i = 0; i < skillIds.Count && i < equippedSlots.Count; i++)
        {
            SkillData skill = skillDatabase.GetSkillById(skillIds[i]);
            if (skill != null)
            {
                equippedSlots[i].SetSkill(skill);
            }
        }

        UpdateEquippedSkillIds();
        Debug.Log($"[SkillSelectionManager] ✅ Загружены сохранённые скиллы: {skillIds.Count}");
    }

    /// <summary>
    /// Проверка готовности (все 3 слота заполнены?)
    /// </summary>
    public bool IsReadyToProceed()
    {
        int equippedCount = equippedSlots.Count(slot => slot.GetSkill() != null);
        return equippedCount == 3;
    }

    /// <summary>
    /// Получить количество экипированных скиллов
    /// </summary>
    public int GetEquippedSkillCount()
    {
        return equippedSlots.Count(slot => slot.GetSkill() != null);
    }

    /// <summary>
    /// Сохранить экипированные скиллы в PlayerPrefs (для передачи в Arena Scene)
    /// Вызывается перед загрузкой Arena Scene
    /// </summary>
    public void SaveEquippedSkillsToPlayerPrefs()
    {
        UpdateEquippedSkillIds();

        // Создаем JSON с ID скиллов
        EquippedSkillsData data = new EquippedSkillsData
        {
            skillIds = equippedSkillIds
        };

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("EquippedSkills", json);
        PlayerPrefs.Save();

        Debug.Log($"[SkillSelectionManager] ✅ Сохранены экипированные скиллы в PlayerPrefs: [{string.Join(", ", equippedSkillIds)}]");
    }
}
