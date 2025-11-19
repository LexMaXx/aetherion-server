using UnityEngine;

/// <summary>
/// Конфигурация статус-эффекта (DoT, CC, Buff, Debuff)
/// Используется в SkillConfig.effects[]
/// </summary>
[System.Serializable]
public class EffectConfig
{
    [Header("═══════════════════════════════════════════")]
    [Header("ТИП ЭФФЕКТА")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Тип эффекта")]
    public EffectType effectType;

    [Header("═══════════════════════════════════════════")]
    [Header("ПАРАМЕТРЫ")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Длительность эффекта (секунды)")]
    [Range(0.1f, 300f)]
    public float duration = 5f;

    [Tooltip("Сила эффекта (% для баффов/дебаффов, урон для DoT)")]
    [Range(0f, 500f)]
    public float power = 0f;

    [Header("═══════════════════════════════════════════")]
    [Header("УРОН / ЛЕЧЕНИЕ ВО ВРЕМЕНИ")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Урон или лечение за тик")]
    public float damageOrHealPerTick = 0f;

    [Tooltip("Интервал тиков (секунды)")]
    [Range(0.1f, 10f)]
    public float tickInterval = 1f;

    [Tooltip("Скейлинг от Intelligence (для магического DoT/HoT)")]
    [Range(0f, 10f)]
    public float intelligenceScaling = 0f;

    [Tooltip("Скейлинг от Strength (для физического DoT/HoT)")]
    [Range(0f, 10f)]
    public float strengthScaling = 0f;

    [Header("═══════════════════════════════════════════")]
    [Header("ВИЗУАЛЬНЫЕ ЭФФЕКТЫ")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Визуальный эффект на цели (огонь, яд, аура, ледяная глыба)")]
    public GameObject particleEffectPrefab;

    [Tooltip("Звук применения эффекта")]
    public AudioClip applySound;

    [Tooltip("Звук окончания эффекта")]
    public AudioClip removeSound;

    [Tooltip("Громкость звуков")]
    [Range(0f, 1f)]
    public float soundVolume = 0.5f;

    [Header("═══════════════════════════════════════════")]
    [Header("НАСТРОЙКИ")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Можно ли снять эффект (Dispel, Cleanse)")]
    public bool canBeDispelled = true;

    [Tooltip("Можно ли стакать эффект (несколько одинаковых эффектов одновременно)")]
    public bool canStack = false;

    [Tooltip("Максимум стаков")]
    [Range(1, 10)]
    public int maxStacks = 1;

    [Tooltip("Сбрасывается ли эффект при получении урона (для Sleep)")]
    public bool breakOnDamage = false;

    [Header("═══════════════════════════════════════════")]
    [Header("СЕТЕВАЯ СИНХРОНИЗАЦИЯ")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Синхронизировать эффект с сервером (ВСЕГДА TRUE для PvP)")]
    public bool syncWithServer = true;

    // ════════════════════════════════════════════════════════════
    // МЕТОДЫ
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Рассчитать финальный тиковый урон/лечение с учётом характеристик
    /// </summary>
    public float CalculateTickDamage(CharacterStats casterStats)
    {
        if (casterStats == null) return damageOrHealPerTick;

        float value = damageOrHealPerTick;
        value += casterStats.intelligence * intelligenceScaling;
        value += casterStats.strength * strengthScaling;

        return Mathf.Max(0f, value);
    }

    /// <summary>
    /// Это контроль эффект (блокирует действия)
    /// </summary>
    public bool IsCrowdControl()
    {
        return effectType == EffectType.Stun ||
               effectType == EffectType.Root ||
               effectType == EffectType.Sleep ||
               effectType == EffectType.Silence ||
               effectType == EffectType.Fear;
    }

    /// <summary>
    /// Это урон во времени
    /// </summary>
    public bool IsDamageOverTime()
    {
        return effectType == EffectType.Poison ||
               effectType == EffectType.Burn ||
               effectType == EffectType.Bleed ||
               effectType == EffectType.DamageOverTime;
    }

    /// <summary>
    /// Это лечение во времени
    /// </summary>
    public bool IsHealOverTime()
    {
        return effectType == EffectType.HealOverTime ||
               effectType == EffectType.IncreaseHPRegen;
    }

    /// <summary>
    /// Это баф (положительный эффект)
    /// </summary>
    public bool IsBuff()
    {
        return effectType == EffectType.IncreaseAttack ||
               effectType == EffectType.IncreaseDefense ||
               effectType == EffectType.IncreaseSpeed ||
               effectType == EffectType.IncreaseHPRegen ||
               effectType == EffectType.IncreaseMPRegen ||
               effectType == EffectType.Shield ||
               effectType == EffectType.Invulnerability ||
               effectType == EffectType.Invisibility;
    }

    /// <summary>
    /// Это дебаф (отрицательный эффект)
    /// </summary>
    public bool IsDebuff()
    {
        return effectType == EffectType.DecreaseAttack ||
               effectType == EffectType.DecreaseDefense ||
               effectType == EffectType.DecreaseSpeed ||
               IsDamageOverTime();
    }

    /// <summary>
    /// Блокирует движение
    /// </summary>
    public bool BlocksMovement()
    {
        return effectType == EffectType.Stun ||
               effectType == EffectType.Root ||
               effectType == EffectType.Sleep ||
               effectType == EffectType.Fear;
    }

    /// <summary>
    /// Блокирует атаки
    /// </summary>
    public bool BlocksAttacks()
    {
        return effectType == EffectType.Stun ||
               effectType == EffectType.Sleep ||
               effectType == EffectType.Fear;
    }

    /// <summary>
    /// Блокирует скиллы
    /// </summary>
    public bool BlocksSkills()
    {
        return effectType == EffectType.Stun ||
               effectType == EffectType.Sleep ||
               effectType == EffectType.Silence ||
               effectType == EffectType.Fear;
    }
}

// ════════════════════════════════════════════════════════════
// ENUM: ВСЕ ТИПЫ ЭФФЕКТОВ
// ════════════════════════════════════════════════════════════

/// <summary>
/// Типы статус-эффектов в игре
/// </summary>
public enum EffectType
{
    // ════════════════════════════════════════════════════════════
    // БАФФЫ (ПОЛОЖИТЕЛЬНЫЕ ЭФФЕКТЫ)
    // ════════════════════════════════════════════════════════════

    [Tooltip("Увеличение физической атаки (%)")]
    IncreaseAttack,

    [Tooltip("Увеличение защиты (%)")]
    IncreaseDefense,

    [Tooltip("Увеличение скорости передвижения (%)")]
    IncreaseSpeed,

    [Tooltip("Регенерация HP (HoT - Heal over Time)")]
    IncreaseHPRegen,

    [Tooltip("Регенерация MP (MoT - Mana over Time)")]
    IncreaseMPRegen,

    [Tooltip("Щит (поглощает определённое количество урона)")]
    Shield,

    [Tooltip("Увеличение радиуса обзора / восприятия")]
    IncreasePerception,

    // ════════════════════════════════════════════════════════════
    // ДЕБАФФЫ (ОТРИЦАТЕЛЬНЫЕ ЭФФЕКТЫ)
    // ════════════════════════════════════════════════════════════

    [Tooltip("Уменьшение физической атаки (%)")]
    DecreaseAttack,

    [Tooltip("Уменьшение защиты (%)")]
    DecreaseDefense,

    [Tooltip("Замедление (уменьшение скорости передвижения %)")]
    DecreaseSpeed,

    [Tooltip("Уменьшение восприятия (снижает радиус обзора, устанавливает perception = 1)")]
    DecreasePerception,

    [Tooltip("Яд (урон во времени - DoT)")]
    Poison,

    [Tooltip("Горение (урон во времени - DoT, обычно от огня)")]
    Burn,

    [Tooltip("Кровотечение (урон во времени - DoT, от физических атак)")]
    Bleed,

    // ════════════════════════════════════════════════════════════
    // КОНТРОЛЬ (CROWD CONTROL)
    // ════════════════════════════════════════════════════════════

    [Tooltip("Оглушение (не может двигаться, атаковать, использовать скиллы)")]
    Stun,

    [Tooltip("Корни (не может двигаться, но может атаковать и кастовать)")]
    Root,

    [Tooltip("Сон (не может действовать, сбрасывается при получении урона)")]
    Sleep,

    [Tooltip("Молчание (не может использовать скиллы, но может двигаться и атаковать)")]
    Silence,

    [Tooltip("Страх (убегает от врага, не контролирует персонажа)")]
    Fear,

    [Tooltip("Провокация (атакует кастера, не может выбрать другую цель)")]
    Taunt,

    // ════════════════════════════════════════════════════════════
    // ОСОБЫЕ ЭФФЕКТЫ
    // ════════════════════════════════════════════════════════════

    [Tooltip("Урон во времени (кастомный, настраиваемый)")]
    DamageOverTime,

    [Tooltip("Лечение во времени (HoT - Heal over Time)")]
    HealOverTime,

    [Tooltip("Неуязвимость (не получает урон)")]
    Invulnerability,

    [Tooltip("Невидимость (враги не видят персонажа)")]
    Invisibility,

    [Tooltip("Увеличение шанса крита (%)")]
    IncreaseCritChance,

    [Tooltip("Увеличение урона от критов (%)")]
    IncreaseCritDamage,

    [Tooltip("Вампиризм (% от нанесённого урона возвращается как HP)")]
    Lifesteal,

    [Tooltip("Отражение урона (% от полученного урона возвращается атакующему)")]
    ThornsEffect,

    [Tooltip("Призыв миньона (скелет, демон, элементаль и т.д.)")]
    SummonMinion,

    [Tooltip("Уменьшение ловкости (снижает Agility, минимум до 1)")]
    DecreaseAgility, // Index 29

    [Tooltip("Увеличение ловкости (+N к Agility)")]
    IncreaseAgility  // Index 30
}
