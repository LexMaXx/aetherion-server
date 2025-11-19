using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Менеджер выбора скиллов в Character Selection Scene
/// Управляет библиотекой скиллов (6 слотов: 5 скиллов класса + 1 пустой) и экипированными слотами (5 шт)
/// </summary>
public class SkillSelectionManager : MonoBehaviour
{
    [Header("UI Слоты")]
    [Tooltip("Слоты библиотеки скиллов (6 штук: 5 скиллов класса + 1 пустой слот)")]
    [SerializeField] private List<SkillSlotUI> librarySlots = new List<SkillSlotUI>(6);

    [Tooltip("Слоты экипированных скиллов (5 штук - все скиллы класса)")]
    [SerializeField] private List<SkillSlotUI> equippedSlots = new List<SkillSlotUI>(5);

    // REMOVED: [Header("Skill Database")] - больше не нужен SkillDatabase!
    // REMOVED: [SerializeField] private SkillDatabase skillDatabase;

    private CharacterClass currentCharacterClass;
    private List<int> equippedSkillIds = new List<int>(5); // Для сохранения в MongoDB (ИЗМЕНЕНО: 3 → 5)

    void Start()
    {
        Debug.Log("[SkillSelectionManager] ✅ Готов к работе (NEW SYSTEM: SkillConfig). Ожидаю выбора класса от CharacterSelectionManager");

        // НЕ загружаем скиллы сразу - ждем когда CharacterSelectionManager
        // загрузит персонажей с сервера и вызовет LoadSkillsForClass()
    }

    /// <summary>
    /// Загрузить скиллы для выбранного класса (NEW SYSTEM: SkillConfig)
    /// </summary>
    public void LoadSkillsForClass(CharacterClass characterClass)
    {
        currentCharacterClass = characterClass;

        // NEW SYSTEM: Используем SkillConfigLoader вместо SkillDatabase
        string className = characterClass.ToString();
        List<SkillConfig> classSkills = SkillConfigLoader.LoadSkillsForClass(className);

        Debug.Log($"[SkillSelectionManager] Загружаю скиллы для {characterClass}: {classSkills.Count} шт");

        // Проверяем что слоты назначены
        if (librarySlots == null || librarySlots.Count == 0)
        {
            Debug.LogError("[SkillSelectionManager] ❌ librarySlots не назначены в Inspector!");
            return;
        }

        Debug.Log($"[SkillSelectionManager] Найдено библиотечных слотов: {librarySlots.Count}");

        // Заполняем библиотеку: все 5 скиллов класса в первые 5 слотов, 6-й слот остаётся пустым
        for (int i = 0; i < librarySlots.Count; i++)
        {
            if (librarySlots[i] == null)
            {
                Debug.LogError($"[SkillSelectionManager] ❌ librarySlots[{i}] = null!");
                continue;
            }

            if (i < classSkills.Count)
            {
                // Заполняем первые 5 слотов скиллами класса
                Debug.Log($"[SkillSelectionManager] Устанавливаю скилл '{classSkills[i].skillName}' в библиотечный слот {i}");
                librarySlots[i].SetSkill(classSkills[i]);
            }
            else
            {
                // 6-й слот (индекс 5) остаётся пустым
                Debug.Log($"[SkillSelectionManager] Библиотечный слот {i} остаётся пустым");
                librarySlots[i].ClearSlot();
            }
        }

        // Автоматически экипируем ВСЕ 5 скиллов класса
        AutoEquipDefaultSkills(classSkills);
    }

    /// <summary>
    /// Автоматически экипировать ВСЕ 5 скиллов класса (NEW SYSTEM: SkillConfig)
    /// </summary>
    private void AutoEquipDefaultSkills(List<SkillConfig> skills)
    {
        if (equippedSlots == null || equippedSlots.Count == 0)
        {
            Debug.LogError("[SkillSelectionManager] ❌ equippedSlots не назначены в Inspector!");
            return;
        }

        Debug.Log($"[SkillSelectionManager] Автоэкипировка всех {skills.Count} скиллов класса. Слотов: {equippedSlots.Count}");

        for (int i = 0; i < equippedSlots.Count && i < skills.Count; i++)
        {
            if (equippedSlots[i] == null)
            {
                Debug.LogError($"[SkillSelectionManager] ❌ equippedSlots[{i}] = null!");
                continue;
            }

            Debug.Log($"[SkillSelectionManager] Экипирую '{skills[i].skillName}' (ID: {skills[i].skillId}) в экипированный слот {i + 1}");
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
    /// Обновить список ID экипированных скиллов (NEW SYSTEM: SkillConfig)
    /// </summary>
    private void UpdateEquippedSkillIds()
    {
        equippedSkillIds.Clear();

        foreach (SkillSlotUI slot in equippedSlots)
        {
            SkillConfig skill = slot.GetSkillConfig(); // ИЗМЕНЕНО: GetSkill() → GetSkillConfig()
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
    /// Загрузить ранее экипированные скиллы (из MongoDB) (NEW SYSTEM: SkillConfig)
    /// </summary>
    public void LoadEquippedSkills(List<int> skillIds)
    {
        // Очищаем экипированные слоты
        foreach (SkillSlotUI slot in equippedSlots)
        {
            slot.ClearSlot();
        }

        // NEW SYSTEM: Загружаем скиллы по ID через SkillConfigLoader
        for (int i = 0; i < skillIds.Count && i < equippedSlots.Count; i++)
        {
            SkillConfig skill = SkillConfigLoader.LoadSkillById(skillIds[i]);
            if (skill != null)
            {
                equippedSlots[i].SetSkill(skill);
            }
            else
            {
                Debug.LogWarning($"[SkillSelectionManager] ⚠️ Не удалось загрузить скилл с ID {skillIds[i]}");
            }
        }

        UpdateEquippedSkillIds();
        Debug.Log($"[SkillSelectionManager] ✅ Загружены сохранённые скиллы: {skillIds.Count}");
    }

    /// <summary>
    /// Проверка готовности (все 5 слотов заполнены?) (NEW SYSTEM: SkillConfig)
    /// </summary>
    public bool IsReadyToProceed()
    {
        int equippedCount = equippedSlots.Count(slot => slot.GetSkillConfig() != null);
        return equippedCount == 5;
    }

    /// <summary>
    /// Получить количество экипированных скиллов (NEW SYSTEM: SkillConfig)
    /// </summary>
    public int GetEquippedSkillCount()
    {
        return equippedSlots.Count(slot => slot.GetSkillConfig() != null);
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
