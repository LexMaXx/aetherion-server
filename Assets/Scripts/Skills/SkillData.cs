using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject для настройки скилла (Lineage 2 style)
/// Поддерживает все типы скиллов: атака, контроль, призыв, трансформация, бафы/дебафы
/// </summary>
[CreateAssetMenu(fileName = "New Skill", menuName = "Aetherion/Skills/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Основная информация")]
    [Tooltip("ID скилла (уникальный)")]
    public int skillId;

    [Tooltip("Название скилла")]
    public string skillName = "New Skill";

    [Tooltip("Описание скилла")]
    [TextArea(3, 5)]
    public string description = "Описание скилла";

    [Tooltip("Иконка скилла для UI")]
    public Sprite icon;

    [Tooltip("Класс персонажа (Warrior, Mage, Archer, Rogue, Paladin)")]
    public CharacterClass characterClass;

    [Header("Параметры использования")]
    [Tooltip("Время перезарядки (секунды)")]
    public float cooldown = 10f;

    [Tooltip("Стоимость маны")]
    public float manaCost = 50f;

    [Tooltip("Дальность применения (метры)")]
    public float castRange = 20f;

    [Tooltip("Время каста (0 = мгновенно)")]
    public float castTime = 0f;

    [Tooltip("Можно ли использовать во время движения")]
    public bool canUseWhileMoving = true;

    [Header("Тип скилла")]
    public SkillType skillType;

    [Header("Целевая система")]
    public OldSkillTargetType targetType;

    [Tooltip("Нужна ли цель для применения")]
    public bool requiresTarget = true;

    [Tooltip("Можно ли применять на союзников")]
    public bool canTargetAllies = false;

    [Tooltip("Можно ли применять на врагов")]
    public bool canTargetEnemies = true;

    [Header("Урон / Лечение")]
    [Tooltip("Базовый урон/лечение")]
    public float baseDamageOrHeal = 0f;

    [Tooltip("Скейлинг от Intelligence (для магов)")]
    public float intelligenceScaling = 0f;

    [Tooltip("Скейлинг от Strength (для воинов)")]
    public float strengthScaling = 0f;

    [Header("Эффекты (контроль, бафы, дебафы)")]
    public List<SkillEffect> effects = new List<SkillEffect>();

    [Header("AOE (Area of Effect)")]
    [Tooltip("Радиус области поражения (0 = одна цель)")]
    public float aoeRadius = 0f;

    [Tooltip("Максимум целей в AOE")]
    public int maxTargets = 1;

    [Header("Анимация каста")]
    [Tooltip("Имя триггера анимации в Animator (Attack, Cast, Spell, etc.)")]
    public string animationTrigger = "";

    [Tooltip("Скорость воспроизведения анимации (1.0 = нормально, 2.0 = в 2 раза быстрее)")]
    [Range(0.1f, 5f)]
    public float animationSpeed = 1.0f;

    [Tooltip("Блокировать движение во время каста?")]
    public bool blockMovementDuringCast = false;

    [Tooltip("Длительность блокировки движения (секунды, 0 = до конца анимации)")]
    public float movementBlockDuration = 0f;

    [Header("Движение при использовании скилла")]
    [Tooltip("Перемещать персонажа при использовании? (Dash, Charge, Teleport)")]
    public bool enableMovement = false;

    [Tooltip("Тип движения")]
    public OldMovementType movementType = OldMovementType.None;

    [Tooltip("Дистанция перемещения (метры)")]
    public float movementDistance = 5f;

    [Tooltip("Скорость перемещения (м/с, 0 = мгновенно)")]
    public float movementSpeed = 10f;

    [Tooltip("Триггер анимации движения (Dash, Roll, Teleport)")]
    public string movementAnimationTrigger = "";

    [Tooltip("Направление движения")]
    public OldMovementDirection movementDirection = OldMovementDirection.Forward;

    [Header("Визуальные эффекты")]
    [Tooltip("Префаб визуального эффекта каста (вспышка, круг на земле)")]
    public GameObject visualEffectPrefab;

    [Tooltip("Эффект на кастере во время каста (свечение рук, аура)")]
    public GameObject casterEffectPrefab;

    [Header("Снаряды")]
    [Tooltip("Префаб снаряда (файрбол, стрела, молот)")]
    public GameObject projectilePrefab;

    [Tooltip("Эффект попадания снаряда (взрыв, искры)")]
    public GameObject projectileHitEffectPrefab;

    [Tooltip("Скорость снаряда (м/с)")]
    public float projectileSpeed = 20f;

    [Tooltip("Самонаведение на цель")]
    public bool projectileHoming = false;

    [Tooltip("Время жизни снаряда (секунды)")]
    public float projectileLifetime = 5f;

    [Header("Звуки")]
    public AudioClip castSound;
    public AudioClip impactSound;

    [Tooltip("Звук попадания снаряда")]
    public AudioClip projectileHitSound;

    [Header("Призыв (для Rogue - скелеты)")]
    [Tooltip("Префаб призываемого существа")]
    public GameObject summonPrefab;

    [Tooltip("Количество призываемых существ")]
    public int summonCount = 1;

    [Tooltip("Длительность существования призванных (секунды)")]
    public float summonDuration = 30f;

    [Header("Трансформация (для Paladin - медведь)")]
    [Tooltip("Префаб модели трансформации")]
    public GameObject transformationModel;

    [Tooltip("Длительность трансформации (секунды)")]
    public float transformationDuration = 30f;

    [Tooltip("Бонус к HP во время трансформации")]
    public float hpBonusPercent = 0f;

    [Tooltip("Бонус к физической атаке")]
    public float physicalDamageBonusPercent = 0f;

    /// <summary>
    /// Расчёт финального урона/лечения с учётом характеристик
    /// </summary>
    public float CalculateDamage(CharacterStats stats)
    {
        if (stats == null) return baseDamageOrHeal;

        float damage = baseDamageOrHeal;
        damage += stats.intelligence * intelligenceScaling;
        damage += stats.strength * strengthScaling;

        return damage;
    }

    /// <summary>
    /// Проверка возможности использования скилла
    /// </summary>
    public bool CanUse(CharacterStats stats, ManaSystem manaSystem, float currentCooldown)
    {
        // Проверка кулдауна
        if (currentCooldown > 0f) return false;

        // Проверка маны
        if (manaSystem != null && manaSystem.CurrentMana < manaCost) return false;

        return true;
    }
}

/// <summary>
/// Типы скиллов
/// </summary>
public enum SkillType
{
    Damage,           // Урон (одиночный/AOE)
    Heal,             // Исцеление
    Buff,             // Положительный эффект
    Debuff,           // Отрицательный эффект
    CrowdControl,     // Контроль (стан, корни, сон)
    Summon,           // Призыв (скелеты для Rogue)
    Transformation,   // Трансформация (медведь для Paladin)
    Teleport,         // Телепорт
    Ressurect         // Воскрешение
}

/// <summary>
/// Тип цели (СТАРАЯ СИСТЕМА - используется в SkillData)
/// </summary>
public enum OldSkillTargetType
{
    Self,             // На себя
    SingleTarget,     // Одна цель
    GroundTarget,     // По земле (AOE)
    NoTarget,         // Без цели (вокруг себя)
    Directional       // Направленный (конус/линия)
}

/// <summary>
/// Тип движения при использовании скилла (СТАРАЯ СИСТЕМА - используется в SkillData)
/// </summary>
public enum OldMovementType
{
    None,           // Нет движения
    Dash,           // Рывок вперёд (быстро)
    Charge,         // Заряд/наскок на врага
    Teleport,       // Телепорт (мгновенно)
    Leap,           // Прыжок
    Roll,           // Перекат
    Blink           // Мигание (короткий телепорт)
}

/// <summary>
/// Направление движения (СТАРАЯ СИСТЕМА - используется в SkillData)
/// </summary>
public enum OldMovementDirection
{
    Forward,        // Вперёд
    Backward,       // Назад
    ToTarget,       // К цели
    AwayFromTarget, // От цели
    MouseDirection  // По направлению мыши
}

/// <summary>
/// Эффект скилла (баф/дебаф/контроль)
/// </summary>
[System.Serializable]
public class SkillEffect
{
    [Header("Основные параметры")]
    [Tooltip("Тип эффекта")]
    public OldEffectType effectType;

    [Tooltip("Длительность (секунды)")]
    public float duration = 5f;

    [Tooltip("Сила эффекта (% для баффов/дебаффов, урон для DoT)")]
    public float power = 0f;

    [Header("Урон/Лечение во времени")]
    [Tooltip("Тиковый урон/лечение в секунду")]
    public float damageOrHealPerTick = 0f;

    [Tooltip("Интервал тиков (секунды)")]
    public float tickInterval = 1f;

    [Header("Визуальные эффекты")]
    [Tooltip("Визуальный эффект на цели (огонь, яд, аура)")]
    public GameObject particleEffectPrefab;

    [Tooltip("Звук применения эффекта")]
    public AudioClip applySound;

    [Tooltip("Звук окончания эффекта")]
    public AudioClip removeSound;

    [Header("Настройки")]
    [Tooltip("Можно ли снять эффект")]
    public bool canBeDispelled = true;

    [Tooltip("Можно ли стакать эффект")]
    public bool canStack = false;

    [Tooltip("Максимум стаков")]
    public int maxStacks = 1;

    [Header("Сетевая синхронизация")]
    [Tooltip("Синхронизировать эффект с сервером (для PvP)")]
    public bool syncWithServer = true;
}

/// <summary>
/// Типы эффектов (СТАРАЯ СИСТЕМА - используется в SkillData)
/// </summary>
public enum OldEffectType
{
    // Баффы
    IncreaseAttack,      // Увеличение атаки
    IncreaseDefense,     // Увеличение защиты
    IncreaseSpeed,       // Увеличение скорости
    IncreaseHPRegen,     // Регенерация HP
    IncreaseMPRegen,     // Регенерация MP
    Shield,              // Щит (поглощение урона)
    IncreasePerception,  // Увеличение восприятия (радиус обзора)

    // Дебаффы
    DecreaseAttack,      // Уменьшение атаки
    DecreaseDefense,     // Уменьшение защиты
    DecreaseSpeed,       // Замедление
    Poison,              // Яд (урон во времени)
    Burn,                // Горение (урон во времени)
    Bleed,               // Кровотечение (урон во времени)

    // Контроль
    Stun,                // Оглушение (не может двигаться/атаковать)
    Root,                // Корни (не может двигаться, может атаковать)
    Sleep,               // Сон (снимается при уроне)
    Silence,             // Молчание (не может использовать скиллы)
    Fear,                // Страх (убегает от врага)
    Taunt,               // Провокация (атакует кастера)

    // Особые
    DamageOverTime,      // Урон во времени (кастомный)
    HealOverTime,        // Лечение во времени
    Invulnerability,     // Неуязвимость
    Invisibility         // Невидимость
}
