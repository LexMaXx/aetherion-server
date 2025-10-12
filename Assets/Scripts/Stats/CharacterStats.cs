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
    public StatsFormulas Formulas => formulas;
    public string ClassName => classPreset != null ? classPreset.className : "Unknown";

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
        maxHealth = formulas.CalculateMaxHealth(endurance);
        maxMana = formulas.CalculateMaxMana(wisdom);
        manaRegen = formulas.CalculateManaRegen(wisdom);
        maxActionPoints = formulas.CalculateMaxActionPoints(agility);
        actionPointsRegen = formulas.CalculateAPRegen(agility);
        visionRadius = formulas.CalculateVisionRadius(perception);
        critChance = formulas.CalculateCritChance(luck);

        // Уведомляем другие системы
        OnStatsChanged?.Invoke();

        Debug.Log($"[CharacterStats] Пересчитаны характеристики: HP:{maxHealth} MP:{maxMana} AP:{maxActionPoints} Vision:{visionRadius}m Crit:{critChance}%");
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
    /// </summary>
    public float CalculatePhysicalDamage(float weaponDamage)
    {
        if (formulas == null) return weaponDamage;
        return formulas.CalculatePhysicalDamage(weaponDamage, strength);
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
    /// Применить критический множитель
    /// </summary>
    public float ApplyCriticalDamage(float baseDamage)
    {
        if (formulas == null) return baseDamage;
        return formulas.ApplyCriticalDamage(baseDamage);
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

    // Для отладки в Inspector
    private void OnValidate()
    {
        if (Application.isPlaying && formulas != null)
        {
            RecalculateStats();
        }
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
