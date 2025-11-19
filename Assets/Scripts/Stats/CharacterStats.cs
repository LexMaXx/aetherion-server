using UnityEngine;
using System;

/// <summary>
/// Компонент характеристик персонажа (SPECIAL система)
/// Автоматически добавляется на персонажа в ArenaScene
/// </summary>
public class CharacterStats : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StatsFormulas formulas;
    [SerializeField] private ClassStatsPreset classPreset;

    [Header("SPECIAL Stats")]
    [Range(1, 10)] public int strength = 1;      // Сила - физ. урон
    [Range(1, 10)] public int perception = 1;    // Восприятие - радиус FoW
    [Range(1, 10)] public int endurance = 1;     // Выносливость - HP
    [Range(1, 10)] public int wisdom = 1;        // Мудрость - MP и реген
    [Range(1, 10)] public int intelligence = 1;  // Интеллект - маг. урон
    [Range(1, 10)] public int agility = 1;       // Ловкость - AP
    [Range(1, 10)] public int luck = 1;          // Удача - крит

    [Header("Calculated Stats (Read Only)")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float maxMana;
    [SerializeField] private float manaRegen;
    [SerializeField] private float maxActionPoints;
    [SerializeField] private float actionPointsRegen;
    [SerializeField] private float visionRadius;
    [SerializeField] private float critChance;

    [Header("Temporary Modifiers")]
    [SerializeField] private float critDamageModifier = 0f; // Модификатор критического урона в процентах
    [SerializeField] private float attackModifier = 0f; // Модификатор урона в процентах

    [Header("Equipment Bonuses")]
    [SerializeField] private int equipmentAttackBonus = 0;
    [SerializeField] private int equipmentDefenseBonus = 0;
    [SerializeField] private int equipmentHealthBonus = 0;
    [SerializeField] private int equipmentManaBonus = 0;

    // Сохранение оригинальных значений для проклятий
    private int originalPerception = -1; // -1 = не сохранено
    private int originalAgility = -1; // -1 = не сохранено (для DecreaseAgility эффекта)

    // События для уведомления других систем об изменении характеристик
    public event Action OnStatsChanged;

    // Геттеры для других систем
    public float MaxHealth => maxHealth;
    public float MaxMana => maxMana;
    public float ManaRegen => manaRegen;
    public float MaxActionPoints => maxActionPoints;
    public float ActionPointsRegen => actionPointsRegen;
    public float VisionRadius => visionRadius;
    public float CritChance => critChance;
    public float CritDamageModifier => critDamageModifier; // Доступ к модификатору критического урона
    public float AttackModifier => attackModifier; // Доступ к модификатору урона
    public StatsFormulas Formulas => formulas;
    public string ClassName => classPreset != null ? classPreset.className : "Unknown";

    // Геттеры для equipment bonuses
    public int EquipmentAttackBonus => equipmentAttackBonus;
    public int EquipmentDefenseBonus => equipmentDefenseBonus;
    public int EquipmentHealthBonus => equipmentHealthBonus;
    public int EquipmentManaBonus => equipmentManaBonus;

    void Start()
    {
        // Загружаем формулы если не установлены
        if (formulas == null)
        {
            formulas = Resources.Load<StatsFormulas>("StatsFormulas");
            if (formulas == null)
            {
                Debug.LogError("[CharacterStats] StatsFormulas не найдены! Создайте через Assets → Create → Aetherion → Stats Formulas");
                return;
            }
        }

        // Применяем начальные характеристики класса
        if (classPreset != null)
        {
            ApplyClassPreset();
        }
        else
        {
            Debug.LogWarning("[CharacterStats] ClassStatsPreset не установлен! Используются дефолтные значения.");
        }

        // Рассчитываем все характеристики
        RecalculateStats();
    }

    /// <summary>
    /// Применить начальные характеристики класса
    /// </summary>
    private void ApplyClassPreset()
    {
        strength = classPreset.strength;
        perception = classPreset.perception;
        endurance = classPreset.endurance;
        wisdom = classPreset.wisdom;
        intelligence = classPreset.intelligence;
        agility = classPreset.agility;
        luck = classPreset.luck;

        Debug.Log($"[CharacterStats] Применены характеристики класса {classPreset.className}: S:{strength} P:{perception} E:{endurance} W:{wisdom} I:{intelligence} A:{agility} L:{luck}");
    }

    /// <summary>
    /// Пересчитать все характеристики на основе SPECIAL
    /// </summary>
    public void RecalculateStats()
    {
        if (formulas == null)
        {
            Debug.LogError("[CharacterStats] Formulas не установлены!");
            return;
        }

        // Расчет характеристик через формулы
        maxHealth = formulas.CalculateMaxHealth(endurance) + equipmentHealthBonus;
        maxMana = formulas.CalculateMaxMana(wisdom) + equipmentManaBonus;
        manaRegen = formulas.CalculateManaRegen(wisdom);
        maxActionPoints = formulas.CalculateMaxActionPoints(agility);
        actionPointsRegen = formulas.CalculateAPRegen(agility);
        visionRadius = formulas.CalculateVisionRadius(perception);
        critChance = formulas.CalculateCritChance(luck);

        // Уведомляем другие системы
        OnStatsChanged?.Invoke();

        Debug.Log($"[CharacterStats] Пересчитаны характеристики: HP:{maxHealth} (Equipment: +{equipmentHealthBonus}) MP:{maxMana} (Equipment: +{equipmentManaBonus}) AP:{maxActionPoints} Vision:{visionRadius}m Crit:{critChance}%");
    }

    /// <summary>
    /// Обновить бонусы от экипировки (вызывается из InventoryManager)
    /// </summary>
    public void UpdateEquipmentBonuses()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[CharacterStats] InventoryManager не найден! Бонусы экипировки не применены.");
            return;
        }

        var bonuses = InventoryManager.Instance.GetTotalEquipmentBonuses();
        equipmentAttackBonus = bonuses.attack;
        equipmentDefenseBonus = bonuses.defense;
        equipmentHealthBonus = bonuses.health;
        equipmentManaBonus = bonuses.mana;

        Debug.Log($"[CharacterStats] 🎽 Бонусы экипировки обновлены: ATK+{equipmentAttackBonus}, DEF+{equipmentDefenseBonus}, HP+{equipmentHealthBonus}, MP+{equipmentManaBonus}");

        // Пересчитываем статы с новыми бонусами
        RecalculateStats();

        // Обновляем текущее HP/MP если бонусы изменились
        UpdateHealthAndManaFromBonuses();
    }

    /// <summary>
    /// Обновить текущее HP/MP после изменения бонусов от экипировки
    /// </summary>
    private void UpdateHealthAndManaFromBonuses()
    {
        HealthSystem healthSystem = GetComponent<HealthSystem>();

        if (healthSystem != null)
        {
            // Обновляем max HP
            healthSystem.SetMaxHealth(maxHealth);
            Debug.Log($"[CharacterStats] ✅ Max HP обновлено: {maxHealth}");
        }

        // ManaSystem автоматически обновится через событие OnStatsChanged
        // которое вызывается в RecalculateStats()
        Debug.Log($"[CharacterStats] ✅ Max Mana обновлено: {maxMana} (через OnStatsChanged)");
    }

    /// <summary>
    /// Увеличить характеристику (для системы прокачки)
    /// </summary>
    public bool Increasestat(string statName)
    {
        int currentValue = GetStat(statName);
        if (currentValue >= 10)
        {
            Debug.LogWarning($"[CharacterStats] {statName} уже максимальна (10)!");
            return false;
        }

        SetStat(statName, currentValue + 1);
        RecalculateStats();
        return true;
    }

    /// <summary>
    /// Получить значение характеристики по имени
    /// </summary>
    public int GetStat(string statName)
    {
        switch (statName.ToLower())
        {
            case "strength": return strength;
            case "perception": return perception;
            case "endurance": return endurance;
            case "wisdom": return wisdom;
            case "intelligence": return intelligence;
            case "agility": return agility;
            case "luck": return luck;
            default:
                Debug.LogWarning($"[CharacterStats] Неизвестная характеристика: {statName}");
                return 0;
        }
    }

    /// <summary>
    /// Установить значение характеристики по имени
    /// </summary>
    private void SetStat(string statName, int value)
    {
        value = Mathf.Clamp(value, 1, 10);

        switch (statName.ToLower())
        {
            case "strength": strength = value; break;
            case "perception": perception = value; break;
            case "endurance": endurance = value; break;
            case "wisdom": wisdom = value; break;
            case "intelligence": intelligence = value; break;
            case "agility": agility = value; break;
            case "luck": luck = value; break;
            default:
                Debug.LogWarning($"[CharacterStats] Неизвестная характеристика: {statName}");
                break;
        }
    }

    /// <summary>
    /// Расчет физического урона (для системы атаки)
    /// ОБНОВЛЕНО: Теперь учитывает бонусы от экипировки!
    /// </summary>
    public float CalculatePhysicalDamage(float weaponDamage)
    {
        if (formulas == null) return weaponDamage + equipmentAttackBonus;

        // Базовый урон + бонусы от Strength + бонусы от экипировки
        float baseDamage = formulas.CalculatePhysicalDamage(weaponDamage, strength);
        float totalDamage = baseDamage + equipmentAttackBonus;

        return totalDamage;
    }

    /// <summary>
    /// Расчет магического урона (для системы атаки)
    /// </summary>
    public float CalculateMagicalDamage(float spellDamage)
    {
        if (formulas == null) return spellDamage;
        return formulas.CalculateMagicalDamage(spellDamage, intelligence);
    }

    /// <summary>
    /// Проверка критического удара
    /// </summary>
    public bool RollCriticalHit()
    {
        if (formulas == null) return false;
        return formulas.RollCriticalHit(luck);
    }

    /// <summary>
    /// Применить критический множитель (с учётом модификаторов)
    /// </summary>
    public float ApplyCriticalDamage(float baseDamage)
    {
        if (formulas == null) return baseDamage;

        // Базовый критический урон
        float critDamage = formulas.ApplyCriticalDamage(baseDamage);

        Debug.Log($"[CharacterStats] 💥 КРИТ DEBUG: baseDamage={baseDamage:F1}, базовый крит={critDamage:F1}, модификатор={critDamageModifier}%");

        // Применяем модификатор критического урона (например, +40% от Deadly Precision)
        if (critDamageModifier > 0)
        {
            float bonus = baseDamage * (critDamageModifier / 100f);
            critDamage += bonus;
            Debug.Log($"[CharacterStats] 💥 С модификатором: {critDamage:F1} (базовый: {formulas.ApplyCriticalDamage(baseDamage):F1} + бонус: {bonus:F1})");
        }
        else
        {
            Debug.Log($"[CharacterStats] 💥 БЕЗ модификатора (модификатор = {critDamageModifier}%)");
        }

        return critDamage;
    }

    /// <summary>
    /// Получить данные для сохранения на сервер (JSON)
    /// </summary>
    public CharacterStatsData GetStatsData()
    {
        return new CharacterStatsData
        {
            strength = this.strength,
            perception = this.perception,
            endurance = this.endurance,
            wisdom = this.wisdom,
            intelligence = this.intelligence,
            agility = this.agility,
            luck = this.luck
        };
    }

    /// <summary>
    /// Загрузить данные с сервера
    /// </summary>
    public void LoadStatsData(CharacterStatsData data)
    {
        this.strength = data.strength;
        this.perception = data.perception;
        this.endurance = data.endurance;
        this.wisdom = data.wisdom;
        this.intelligence = data.intelligence;
        this.agility = data.agility;
        this.luck = data.luck;

        RecalculateStats();
    }

    // ═══════════════════════════════════════════
    // МОДИФИКАТОРЫ СТАТОВ (для баффов/дебаффов)
    // ═══════════════════════════════════════════

    [Header("Runtime Modifiers (Buffs/Debuffs)")]
    [SerializeField] private float physicalDamageModifier = 0f;
    [SerializeField] private float physicalDefenseModifier = 0f;
    [SerializeField] private float movementSpeedModifier = 0f;

    // Базовые значения
    public float physicalDamage => formulas != null ? formulas.CalculatePhysicalDamage(100f, strength) + physicalDamageModifier : 100f + physicalDamageModifier;
    public float physicalDefense => (endurance * 5f) + equipmentDefenseBonus + physicalDefenseModifier; // Простая формула: Endurance * 5 + Equipment Bonus
    public float movementSpeed => 2.5f + movementSpeedModifier; // Было: 5f (-50%)

    /// <summary>
    /// Модифицировать физический урон (для баффов/дебаффов)
    /// </summary>
    public void ModifyPhysicalDamage(float amount)
    {
        physicalDamageModifier += amount;
        Debug.Log($"[CharacterStats] Physical Damage Modifier: {physicalDamageModifier:F1} (total: {physicalDamage:F1})");
    }

    /// <summary>
    /// Модифицировать физическую защиту (для баффов/дебаффов)
    /// </summary>
    public void ModifyPhysicalDefense(float amount)
    {
        physicalDefenseModifier += amount;
        Debug.Log($"[CharacterStats] Physical Defense Modifier: {physicalDefenseModifier:F1} (total: {physicalDefense:F1})");
    }

    /// <summary>
    /// Модифицировать скорость движения (для баффов/дебаффов)
    /// </summary>
    public void ModifyMovementSpeed(float amount)
    {
        movementSpeedModifier += amount;
        Debug.Log($"[CharacterStats] Movement Speed Modifier: {movementSpeedModifier:F1} (total: {movementSpeed:F1})");
    }

    /// <summary>
    /// Модифицировать восприятие (для баффов/дебаффов Eagle Eye)
    /// </summary>
    public void ModifyPerception(int amount)
    {
        perception += amount;
        RecalculateStats(); // Пересчитываем visionRadius
        Debug.Log($"[CharacterStats] Perception изменено на {amount} (total: {perception}, visionRadius: {visionRadius}м)");
    }

    /// <summary>
    /// Добавить модификатор критического урона (для баффа Deadly Precision)
    /// </summary>
    public void AddCritDamageModifier(float percentModifier)
    {
        critDamageModifier += percentModifier;
        Debug.Log($"[CharacterStats] 💥 Крит урон изменён: {(percentModifier > 0 ? "+" : "")}{percentModifier}% (итого: +{critDamageModifier}%)");
    }

    /// <summary>
    /// Убрать модификатор критического урона
    /// </summary>
    public void RemoveCritDamageModifier(float percentModifier)
    {
        critDamageModifier -= percentModifier;
        Debug.Log($"[CharacterStats] 💥 Модификатор крит урона снят: {percentModifier}% (итого: +{critDamageModifier}%)");
    }

    /// <summary>
    /// Добавить модификатор урона (для баффа Battle Rage)
    /// </summary>
    public void AddAttackModifier(float percentModifier)
    {
        attackModifier += percentModifier;
        Debug.Log($"[CharacterStats] ⚔️ Урон изменён: {(percentModifier > 0 ? "+" : "")}{percentModifier}% (итого: +{attackModifier}%)");
    }

    /// <summary>
    /// Убрать модификатор урона
    /// </summary>
    public void RemoveAttackModifier(float percentModifier)
    {
        attackModifier -= percentModifier;
        Debug.Log($"[CharacterStats] ⚔️ Модификатор урона снят: {percentModifier}% (итого: +{attackModifier}%)");
    }

    /// <summary>
    /// Установить Perception в указанное значение (для проклятий)
    /// Сохраняет оригинальное значение для последующего восстановления
    /// </summary>
    public void SetPerception(int value)
    {
        // Сохраняем оригинальное значение если ещё не сохранено
        if (originalPerception == -1)
        {
            originalPerception = perception;
            Debug.Log($"[CharacterStats] 💾 Оригинальный Perception сохранён: {originalPerception}");
        }

        perception = value;
        RecalculateStats(); // Пересчитываем visionRadius
        Debug.Log($"[CharacterStats] 👁️ Perception установлен в {value} (было: {originalPerception}, visionRadius: {visionRadius}м)");
    }

    /// <summary>
    /// Восстановить оригинальный Perception (снятие проклятия)
    /// </summary>
    public void RestorePerception()
    {
        if (originalPerception != -1)
        {
            perception = originalPerception;
            RecalculateStats();
            Debug.Log($"[CharacterStats] 🔄 Perception восстановлен: {perception} (visionRadius: {visionRadius}м)");
            originalPerception = -1; // Сброс
        }
        else
        {
            Debug.LogWarning("[CharacterStats] ⚠️ Нет сохранённого Perception для восстановления!");
        }
    }

    /// <summary>
    /// Модифицировать ловкость (для баффов/дебаффов)
    /// </summary>
    public void ModifyAgility(int amount)
    {
        agility += amount;

        // Минимум Agility = 1
        if (agility < 1)
        {
            agility = 1;
        }

        RecalculateStats();
        Debug.Log($"[CharacterStats] 🏃 Agility изменено на {amount} (total: {agility})");
    }

    /// <summary>
    /// Установить Agility в указанное значение (для проклятий)
    /// Сохраняет оригинальное значение для последующего восстановления
    /// </summary>
    public void SetAgility(int value)
    {
        // Сохраняем оригинальное значение если ещё не сохранено
        if (originalAgility == -1)
        {
            originalAgility = agility;
            Debug.Log($"[CharacterStats] 💾 Оригинальный Agility сохранён: {originalAgility}");
        }

        agility = Mathf.Max(1, value); // Минимум 1
        RecalculateStats();
        Debug.Log($"[CharacterStats] 🏃 Agility установлен в {agility} (было: {originalAgility})");
    }

    /// <summary>
    /// Восстановить оригинальный Agility (снятие проклятия)
    /// </summary>
    public void RestoreAgility()
    {
        if (originalAgility != -1)
        {
            agility = originalAgility;
            RecalculateStats();
            Debug.Log($"[CharacterStats] 🔄 Agility восстановлен: {agility}");
            originalAgility = -1; // Сброс
        }
        else
        {
            Debug.LogWarning("[CharacterStats] ⚠️ Нет сохранённого Agility для восстановления!");
        }
    }

    // Для отладки в Inspector
    private void OnValidate()
    {
        if (Application.isPlaying && formulas != null)
        {
            RecalculateStats();
        }
    }

    // ═══════════════════════════════════════════════════════
    // SERVER SYNC - Методы для синхронизации с сервером
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Экспортировать данные характеристик для отправки на сервер
    /// </summary>
    public CharacterStatsData ExportData()
    {
        return new CharacterStatsData
        {
            strength = strength,
            perception = perception,
            endurance = endurance,
            wisdom = wisdom,
            intelligence = intelligence,
            agility = agility,
            luck = luck
        };
    }

    /// <summary>
    /// Импортировать данные характеристик с сервера
    /// </summary>
    public void ImportData(CharacterStatsData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[CharacterStats] ImportData: data is null!");
            return;
        }

        strength = data.strength;
        perception = data.perception;
        endurance = data.endurance;
        wisdom = data.wisdom;
        intelligence = data.intelligence;
        agility = data.agility;
        luck = data.luck;

        Debug.Log($"[CharacterStats] ✅ Данные импортированы: STR {strength}, PER {perception}, END {endurance}, WIS {wisdom}, INT {intelligence}, AGI {agility}, LCK {luck}");

        // Пересчитываем все производные характеристики
        RecalculateStats();

        // КРИТИЧЕСКИ ВАЖНО: Уведомляем UI об изменении данных!
        OnStatsChanged?.Invoke();
        Debug.Log("[CharacterStats] OnStatsChanged вызван после ImportData");
    }
}

/// <summary>
/// Данные характеристик для сериализации (отправка на сервер)
/// </summary>
[System.Serializable]
public class CharacterStatsData
{
    public int strength;
    public int perception;
    public int endurance;
    public int wisdom;
    public int intelligence;
    public int agility;
    public int luck;
}
