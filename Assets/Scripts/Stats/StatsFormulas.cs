using UnityEngine;

/// <summary>
/// Редактируемые формулы расчета характеристик SPECIAL
/// Создать: Assets → Create → Aetherion → Stats Formulas
/// </summary>
[CreateAssetMenu(fileName = "StatsFormulas", menuName = "Aetherion/Stats Formulas", order = 1)]
public class StatsFormulas : ScriptableObject
{
    [Header("Strength - Физический урон")]
    [Tooltip("Бонус физического урона за каждую единицу Силы")]
    public float strengthDamageBonus = 5f; // Урон оружия + (Сила * 5)

    [Header("Perception - Fog of War радиус")]
    [Tooltip("Базовый радиус видимости")]
    public float baseVisionRadius = 10f;
    [Tooltip("Бонус радиуса за каждую единицу Восприятия")]
    public float perceptionRadiusBonus = 3f; // 10 + (Perception * 3) = 13-40м
    [Tooltip("Максимальный радиус видимости")]
    public float maxVisionRadius = 40f;

    [Header("Endurance - Health Points")]
    [Tooltip("Базовое здоровье")]
    public float baseHealth = 1000f; // УВЕЛИЧЕНО: 1000 вместо 100
    [Tooltip("Бонус HP за каждую единицу Выносливости")]
    public float enduranceHealthBonus = 200f; // УВЕЛИЧЕНО: 1000 + (Endurance * 200) = 3000 для Endurance 10

    [Header("Wisdom - Mana Points и Regeneration")]
    [Tooltip("Базовая мана")]
    public float baseMana = 50f;
    [Tooltip("Бонус MP за каждую единицу Мудрости")]
    public float wisdomManaBonus = 15f; // 50 + (Wisdom * 15)
    [Tooltip("Базовая скорость регенерации маны в секунду")]
    public float baseManaRegen = 1f;
    [Tooltip("Бонус регена маны за каждую единицу Мудрости")]
    public float wisdomManaRegenBonus = 0.5f; // 1 + (Wisdom * 0.5)

    [Header("Intelligence - Магический урон")]
    [Tooltip("Бонус магического урона за каждую единицу Интеллекта")]
    public float intelligenceDamageBonus = 5f; // Урон заклинания + (Интеллект * 5)

    [Header("Agility - Action Points")]
    [Tooltip("Базовое количество очков действия")]
    public float baseActionPoints = 3f;
    [Tooltip("Бонус AP за каждую единицу Ловкости")]
    public float agilityAPBonus = 0.9f; // 3 + (Agility * 0.9) = 3.9-12
    [Tooltip("Максимальное количество очков действия")]
    public float maxActionPoints = 12f;
    [Tooltip("Базовая скорость восстановления AP в секунду")]
    public float baseAPRegen = 1f;
    [Tooltip("Бонус скорости восстановления AP за каждую единицу Ловкости")]
    public float agilityAPRegenBonus = 0.1f; // 1 + (Agility * 0.1)

    [Header("Agility - Movement Speed")]
    [Tooltip("Процент бонуса скорости за каждую единицу Ловкости (0.05 = +5% за единицу)")]
    public float agilitySpeedBonus = 0f; // Было: 0.05f (ОТКЛЮЧЕНО для снижения скорости)

    [Header("Luck - Шанс критического удара")]
    [Tooltip("Базовый шанс крита (%)")]
    public float baseCritChance = 5f;
    [Tooltip("Бонус шанса крита за каждую единицу Удачи (%)")]
    public float luckCritBonus = 3f; // 5 + (Luck * 3) = 8-35%
    [Tooltip("Максимальный шанс крита (%)")]
    public float maxCritChance = 35f;

    [Header("Critical Hit Damage")]
    [Tooltip("Множитель урона при критическом ударе")]
    public float critDamageMultiplier = 2f; // x2 урона

    // ============ МЕТОДЫ РАСЧЕТА ============

    /// <summary>
    /// Расчет физического урона (Оружие + Сила)
    /// </summary>
    public float CalculatePhysicalDamage(float weaponDamage, int strength)
    {
        return weaponDamage + (strength * strengthDamageBonus);
    }

    /// <summary>
    /// Расчет магического урона (Заклинание + Интеллект)
    /// </summary>
    public float CalculateMagicalDamage(float spellDamage, int intelligence)
    {
        return spellDamage + (intelligence * intelligenceDamageBonus);
    }

    /// <summary>
    /// Расчет радиуса Fog of War (Восприятие)
    /// </summary>
    public float CalculateVisionRadius(int perception)
    {
        float radius = baseVisionRadius + (perception * perceptionRadiusBonus);
        return Mathf.Min(radius, maxVisionRadius);
    }

    /// <summary>
    /// Расчет максимального здоровья (Выносливость)
    /// </summary>
    public float CalculateMaxHealth(int endurance)
    {
        return baseHealth + (endurance * enduranceHealthBonus);
    }

    /// <summary>
    /// Расчет максимальной маны (Мудрость)
    /// </summary>
    public float CalculateMaxMana(int wisdom)
    {
        return baseMana + (wisdom * wisdomManaBonus);
    }

    /// <summary>
    /// Расчет регенерации маны (Мудрость)
    /// </summary>
    public float CalculateManaRegen(int wisdom)
    {
        return baseManaRegen + (wisdom * wisdomManaRegenBonus);
    }

    /// <summary>
    /// Расчет максимальных очков действия (Ловкость)
    /// </summary>
    public float CalculateMaxActionPoints(int agility)
    {
        float ap = baseActionPoints + (agility * agilityAPBonus);
        return Mathf.Min(ap, maxActionPoints);
    }

    /// <summary>
    /// Расчет скорости восстановления AP (Ловкость)
    /// </summary>
    public float CalculateAPRegen(int agility)
    {
        return baseAPRegen + (agility * agilityAPRegenBonus);
    }

    /// <summary>
    /// Расчет множителя скорости движения (Ловкость)
    /// </summary>
    public float CalculateSpeedMultiplier(int agility)
    {
        return 1.0f + (agility * agilitySpeedBonus);
    }

    /// <summary>
    /// Расчет шанса крита (Удача)
    /// </summary>
    public float CalculateCritChance(int luck)
    {
        float crit = baseCritChance + (luck * luckCritBonus);
        return Mathf.Min(crit, maxCritChance);
    }

    /// <summary>
    /// Проверка - произошел ли критический удар
    /// </summary>
    public bool RollCriticalHit(int luck)
    {
        float critChance = CalculateCritChance(luck);
        float roll = Random.Range(0f, 100f);
        return roll <= critChance;
    }

    /// <summary>
    /// Применить критический урон
    /// </summary>
    public float ApplyCriticalDamage(float baseDamage)
    {
        return baseDamage * critDamageMultiplier;
    }
}
