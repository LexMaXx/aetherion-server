using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Менеджер скиллов персонажа (использует SkillData из Resources/Skills/)
/// Упрощённая версия - просто хранит список скиллов и передаёт их в SkillExecutor
/// </summary>
public class SkillManager : MonoBehaviour
{
    [Header("Активные скиллы (SkillData)")]
    [Tooltip("До 5 скиллов, загружаются при спавне")]
    public List<SkillConfig> equippedSkills = new List<SkillConfig>(5);

    [Header("Все доступные скиллы класса")]
    [Tooltip("Все 5 скиллов класса")]
    public List<SkillConfig> allAvailableSkills = new List<SkillConfig>(5);

    [Header("Компоненты")]
    private SkillExecutor skillExecutor;

    void Start()
    {
        skillExecutor = GetComponent<SkillExecutor>();

        if (skillExecutor == null)
        {
            Debug.LogError("[SkillManager] ❌ SkillExecutor не найден! Добавьте компонент SkillExecutor.");
        }

        Debug.Log($"[SkillManager] Инициализирован. Экипировано скиллов: {equippedSkills.Count}");
    }

    /// <summary>
    /// Загрузить скиллы по массиву skillIds
    /// </summary>
    public void LoadEquippedSkills(List<int> skillIds)
    {
        equippedSkills.Clear();

        Debug.Log($"[SkillManager] 📚 Загрузка {skillIds.Count} скиллов: {string.Join(", ", skillIds)}");

        foreach (int skillId in skillIds)
        {
            SkillConfig skill = SkillConfigLoader.LoadSkillById(skillId);
            if (skill != null)
            {
                equippedSkills.Add(skill);
                Debug.Log($"[SkillManager] ✅ Загружен скилл: {skill.skillName} (ID: {skill.skillId})");
            }
            else
            {
                Debug.LogError($"[SkillManager] ❌ Не удалось загрузить скилл с ID: {skillId}");
            }
        }

        Debug.Log($"[SkillManager] ✅ Загружено {equippedSkills.Count}/{skillIds.Count} скиллов");

        // ВАЖНО: Передаём скиллы в SkillExecutor
        TransferSkillsToExecutor();
    }

    /// <summary>
    /// Загрузить ВСЕ скиллы класса в allAvailableSkills
    /// </summary>
    public void LoadAllSkillsForClass(string characterClass)
    {
        allAvailableSkills = SkillConfigLoader.LoadSkillsForClass(characterClass);
        Debug.Log($"[SkillManager] 📚 Загружено {allAvailableSkills.Count} скиллов для класса {characterClass}");
    }

    /// <summary>
    /// Передать скиллы в SkillExecutor (связывание систем)
    /// </summary>
    private void TransferSkillsToExecutor()
    {
        // Если skillExecutor ещё не установлен (Start не вызван), ищем его вручную
        if (skillExecutor == null)
        {
            skillExecutor = GetComponent<SkillExecutor>();
        }

        if (skillExecutor == null)
        {
            Debug.LogError("[SkillManager] ❌ SkillExecutor не найден! Убедитесь что компонент SkillExecutor добавлен.");
            return;
        }

        // ВАЖНО: SetSkill ожидает slotNumber в диапазоне 1-5 (внутри преобразуется в 0-4)
        // PlayerAttackNew передаёт индексы 0-2 при нажатии клавиш 1-3
        for (int i = 0; i < equippedSkills.Count && i < 5; i++)
        {
            skillExecutor.SetSkill(i + 1, equippedSkills[i]); // Передаём 1-5, внутри SetSkill преобразуется в 0-4
            Debug.Log($"[SkillManager] ✅ Скилл '{equippedSkills[i].skillName}' установлен в слот {i + 1} (клавиша {i + 1})");
        }

        Debug.Log($"[SkillManager] ✅ {equippedSkills.Count} скиллов переданы в SkillExecutor (слоты 0-{equippedSkills.Count - 1})");
    }

    /// <summary>
    /// Получить экипированный скилл по индексу (0-4)
    /// </summary>
    public SkillConfig GetEquippedSkill(int index)
    {
        if (index < 0 || index >= equippedSkills.Count)
        {
            return null;
        }
        return equippedSkills[index];
    }

    /// <summary>
    /// Получить все экипированные скиллы
    /// </summary>
    public List<SkillConfig> GetEquippedSkills()
    {
        return new List<SkillConfig>(equippedSkills);
    }

    /// <summary>
    /// Получить все доступные скиллы класса
    /// </summary>
    public List<SkillConfig> GetAllAvailableSkills()
    {
        return new List<SkillConfig>(allAvailableSkills);
    }

    /// <summary>
    /// Найти скилл по ID (старый метод для совместимости)
    /// </summary>
    public SkillConfig GetSkillById(int skillId)
    {
        // Ищем в экипированных
        foreach (var skill in equippedSkills)
        {
            if (skill.skillId == skillId)
                return skill;
        }

        // Ищем в доступных
        foreach (var skill in allAvailableSkills)
        {
            if (skill.skillId == skillId)
                return skill;
        }

        // Пробуем загрузить через SkillConfigLoader
        return SkillConfigLoader.LoadSkillById(skillId);
    }

    /// <summary>
    /// Экипировать скилл в определённый слот (для Character Selection)
    /// </summary>
    public void EquipSkillToSlot(int slotIndex, SkillConfig skill)
    {
        if (slotIndex < 0 || slotIndex >= 5)
        {
            Debug.LogError($"[SkillManager] ❌ Некорректный индекс слота: {slotIndex}");
            return;
        }

        // Расширяем список если нужно
        while (equippedSkills.Count <= slotIndex)
        {
            equippedSkills.Add(null);
        }

        equippedSkills[slotIndex] = skill;
        Debug.Log($"[SkillManager] ✅ Скилл {skill.skillName} экипирован в слот {slotIndex + 1}");

        // Обновляем SkillExecutor
        if (skillExecutor != null)
        {
            skillExecutor.SetSkill(slotIndex + 1, skill);
        }
    }

    /// <summary>
    /// Проверить экипирован ли скилл
    /// </summary>
    public bool IsSkillEquipped(int skillId)
    {
        foreach (var skill in equippedSkills)
        {
            if (skill != null && skill.skillId == skillId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Получить количество экипированных скиллов
    /// </summary>
    public int GetEquippedSkillCount()
    {
        int count = 0;
        foreach (var skill in equippedSkills)
        {
            if (skill != null)
                count++;
        }
        return count;
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // МЕТОДЫ ОБРАТНОЙ СОВМЕСТИМОСТИ (для старого кода)
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Проверить активен ли эффект Root/Stun (блокирует движение)
    /// Используется в ThirdPersonController
    /// </summary>
    public bool IsRooted()
    {
        if (skillExecutor == null)
        {
            skillExecutor = GetComponent<SkillExecutor>();
            if (skillExecutor == null) return false;
        }

        return skillExecutor.IsRooted();
    }

    /// <summary>
    /// Добавить эффект на цель (старый API для projectile скриптов)
    /// Делегирует в SkillExecutor
    /// </summary>
    public void AddEffect(EffectConfig effect, Transform target)
    {
        if (skillExecutor == null)
        {
            skillExecutor = GetComponent<SkillExecutor>();
            if (skillExecutor == null)
            {
                Debug.LogError("[SkillManager] ❌ SkillExecutor не найден для применения эффекта!");
                return;
            }
        }

        skillExecutor.ApplyEffectToTarget(effect, target);
    }

    /// <summary>
    /// Использовать скилл по индексу (старый API для PlayerAttack/SkillBarUI)
    /// Делегирует в SkillExecutor
    /// </summary>
    public bool UseSkill(int skillIndex, Transform target = null)
    {
        if (skillExecutor == null)
        {
            skillExecutor = GetComponent<SkillExecutor>();
            if (skillExecutor == null)
            {
                Debug.LogError("[SkillManager] ❌ SkillExecutor не найден для использования скилла!");
                return false;
            }
        }

        // SkillExecutor использует слоты 0-4, а старый код использует индексы 0-4
        return skillExecutor.UseSkill(skillIndex, target);
    }
}
