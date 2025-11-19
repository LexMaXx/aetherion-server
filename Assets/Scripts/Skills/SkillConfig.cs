using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Новая система скиллов (аналогично BasicAttackConfig)
/// Поддерживает ПОЛНУЮ сетевую синхронизацию всех эффектов
/// </summary>
[CreateAssetMenu(fileName = "NewSkillConfig", menuName = "Aetherion/Combat/Skill Config")]
public class SkillConfig : ScriptableObject
{
    [Header("═══════════════════════════════════════════")]
    [Header("ОСНОВНАЯ ИНФОРМАЦИЯ")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Уникальный ID скилла (101-199 Warrior, 201-299 Mage, 301-399 Archer, 401-499 Rogue, 501-599 Paladin)")]
    public int skillId;

    [Tooltip("Название скилла")]
    public string skillName = "New Skill";

    [Tooltip("Описание скилла для UI")]
    [TextArea(2, 4)]
    public string description = "Описание скилла";

    [Tooltip("Иконка скилла для UI панели")]
    public Sprite icon;

    [Tooltip("Класс персонажа")]
    public CharacterClass characterClass;

    [Header("═══════════════════════════════════════════")]
    [Header("ПАРАМЕТРЫ ИСПОЛЬЗОВАНИЯ")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Время перезарядки (секунды)")]
    [Range(0f, 120f)]
    public float cooldown = 10f;

    [Tooltip("Стоимость маны")]
    [Range(0f, 200f)]
    public float manaCost = 50f;

    [Tooltip("Стоимость Action Points (очков действия)")]
    [Range(0, 10)]
    public int actionPointCost = 0;

    [Tooltip("Дальность применения (метры)")]
    [Range(0f, 50f)]
    public float castRange = 20f;

    [Tooltip("Время каста (0 = мгновенно)")]
    [Range(0f, 5f)]
    public float castTime = 0f;

    [Tooltip("Можно ли использовать во время движения")]
    public bool canUseWhileMoving = true;

    [Header("═══════════════════════════════════════════")]
    [Header("ТИП СКИЛЛА")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Тип скилла определяет основную механику")]
    public SkillConfigType skillType;

    [Tooltip("Тип цели")]
    public SkillTargetType targetType;

    [Tooltip("Требуется ли выбор цели")]
    public bool requiresTarget = true;

    [Tooltip("Можно ли применять на союзников")]
    public bool canTargetAllies = false;

    [Tooltip("Можно ли применять на врагов")]
    public bool canTargetEnemies = true;

    [Header("═══════════════════════════════════════════")]
    [Header("УРОН / ЛЕЧЕНИЕ")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Базовый урон или лечение")]
    public float baseDamageOrHeal = 0f;

    [Tooltip("Скейлинг от Strength (для физических атак)")]
    [Range(0f, 50f)]
    public float strengthScaling = 0f;

    [Tooltip("Скейлинг от Intelligence (для магических атак)")]
    [Range(0f, 50f)]
    public float intelligenceScaling = 0f;

    [Tooltip("Процент Life Steal (восстановление HP от нанесённого урона, 0-100%)")]
    [Range(0f, 100f)]
    public float lifeStealPercent = 0f;

    [Header("═══════════════════════════════════════════")]
    [Header("ЭФФЕКТЫ (СТАТУС-ЭФФЕКТЫ)")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Список эффектов которые накладывает этот скилл (DoT, CC, Buffs, Debuffs)")]
    public List<EffectConfig> effects = new List<EffectConfig>();

    [Header("═══════════════════════════════════════════")]
    [Header("AOE (ОБЛАСТЬ ПОРАЖЕНИЯ)")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Радиус области поражения (0 = одиночная цель)")]
    [Range(0f, 30f)]
    public float aoeRadius = 0f;

    [Tooltip("Максимум целей в AOE")]
    [Range(1, 20)]
    public int maxTargets = 1;

    [Header("═══════════════════════════════════════════")]
    [Header("СНАРЯД (PROJECTILE)")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Префаб снаряда (должен иметь CelestialProjectile компонент)")]
    public GameObject projectilePrefab;

    [Tooltip("Скорость снаряда (м/с)")]
    [Range(5f, 50f)]
    public float projectileSpeed = 20f;

    [Tooltip("Время жизни снаряда (секунды)")]
    [Range(1f, 10f)]
    public float projectileLifetime = 5f;

    [Tooltip("Автонаведение на цель")]
    public bool projectileHoming = true;

    [Tooltip("Скорость хоминга (м/с)")]
    [Range(0f, 20f)]
    public float homingSpeed = 10f;

    [Tooltip("Радиус хоминга (м)")]
    [Range(0f, 30f)]
    public float homingRadius = 20f;

    [Header("═══════════════════════════════════════════")]
    [Header("ВИЗУАЛЬНЫЕ ЭФФЕКТЫ")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Визуальный эффект каста (появляется на кастере)")]
    public GameObject castEffectPrefab;

    [Tooltip("Визуальный эффект попадания (появляется на цели)")]
    public GameObject hitEffectPrefab;

    [Tooltip("Визуальный эффект AOE (для ground target скиллов)")]
    public GameObject aoeEffectPrefab;

    [Tooltip("Визуальный эффект на кастере (для лечения, баффов и т.д.)")]
    public GameObject casterEffectPrefab;

    [Header("═══════════════════════════════════════════")]
    [Header("АНИМАЦИЯ")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Имя триггера анимации в Animator (Attack, Cast, Spell)")]
    public string animationTrigger = "Attack";

    [Tooltip("Скорость воспроизведения анимации")]
    [Range(0.1f, 5f)]
    public float animationSpeed = 1f;

    [Tooltip("Тайминг создания снаряда относительно анимации (0-1, где 0=начало, 1=конец)")]
    [Range(0f, 1f)]
    public float projectileSpawnTiming = 0.5f;

    [Header("═══════════════════════════════════════════")]
    [Header("ЗВУКИ")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Звук каста")]
    public AudioClip castSound;

    [Tooltip("Звук попадания")]
    public AudioClip hitSound;

    [Tooltip("Громкость звуков")]
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;

    [Header("═══════════════════════════════════════════")]
    [Header("ДВИЖЕНИЕ ПРИ ИСПОЛЬЗОВАНИИ")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Включить движение персонажа (Dash, Charge, Teleport)")]
    public bool enableMovement = false;

    [Tooltip("Тип движения")]
    public MovementType movementType = MovementType.None;

    [Tooltip("Дистанция перемещения (метры)")]
    [Range(0f, 30f)]
    public float movementDistance = 5f;

    [Tooltip("Скорость перемещения (м/с, 0 = мгновенно)")]
    [Range(0f, 30f)]
    public float movementSpeed = 10f;

    [Tooltip("Направление движения")]
    public MovementDirection movementDirection = MovementDirection.Forward;

    [Header("═══════════════════════════════════════════")]
    [Header("ПРИЗЫВ (SUMMON)")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Префаб призываемого существа (Rogue - Skeletons)")]
    public GameObject summonPrefab;

    [Tooltip("Количество призываемых существ")]
    [Range(1, 10)]
    public int summonCount = 1;

    [Tooltip("Длительность существования (секунды)")]
    [Range(10f, 300f)]
    public float summonDuration = 30f;

    [Header("═══════════════════════════════════════════")]
    [Header("ТРАНСФОРМАЦИЯ (TRANSFORMATION)")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Префаб модели трансформации (Paladin - Bear)")]
    public GameObject transformationModel;

    [Tooltip("Длительность трансформации (секунды)")]
    [Range(10f, 300f)]
    public float transformationDuration = 30f;

    [Tooltip("Бонус к максимальному HP (процент)")]
    [Range(0f, 200f)]
    public float hpBonusPercent = 0f;

    [Tooltip("Бонус к физическому урону (процент)")]
    [Range(0f, 200f)]
    public float damageBonusPercent = 0f;

    [Header("═══════════════════════════════════════════")]
    [Header("СЕТЕВАЯ СИНХРОНИЗАЦИЯ")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Синхронизировать снаряды с сервером (ВСЕГДА TRUE для PvP)")]
    public bool syncProjectiles = true;

    [Tooltip("Синхронизировать эффекты попадания")]
    public bool syncHitEffects = true;

    [Tooltip("Синхронизировать статус-эффекты (DoT, CC, Buffs)")]
    public bool syncStatusEffects = true;

    [Header("═══════════════════════════════════════════")]
    [Header("ДОПОЛНИТЕЛЬНЫЕ ПАРАМЕТРЫ")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Дополнительные данные для специальных скиллов (chain lightning, multi-hit, etc.)")]
    public SkillCustomData customData;

    // ════════════════════════════════════════════════════════════
    // МЕТОДЫ РАСЧЁТА
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Рассчитать финальный урон/лечение с учётом характеристик персонажа
    /// </summary>
    public float CalculateDamage(CharacterStats stats)
    {
        if (stats == null) return baseDamageOrHeal;

        float value = baseDamageOrHeal;
        value += stats.strength * strengthScaling;
        value += stats.intelligence * intelligenceScaling;

        return Mathf.Max(0f, value);
    }

    /// <summary>
    /// Проверка возможности использования скилла
    /// </summary>
    public bool CanUse(CharacterStats stats, ManaSystem manaSystem, float currentCooldown)
    {
        // Проверка кулдауна
        if (currentCooldown > 0f)
        {
            Debug.Log($"[SkillConfig] ❌ {skillName} на кулдауне: {currentCooldown:F1}с");
            return false;
        }

        // Проверка маны
        if (manaSystem != null && manaSystem.CurrentMana < manaCost)
        {
            Debug.Log($"[SkillConfig] ❌ {skillName} недостаточно маны: {manaSystem.CurrentMana:F0}/{manaCost:F0}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Получить длительность каста для синхронизации
    /// </summary>
    public float GetTotalCastTime()
    {
        return castTime;
    }

    /// <summary>
    /// Проверка это скилл с снарядом
    /// </summary>
    public bool HasProjectile()
    {
        return projectilePrefab != null;
    }

    /// <summary>
    /// Проверка это AOE скилл
    /// </summary>
    public bool IsAOE()
    {
        return aoeRadius > 0f;
    }

    /// <summary>
    /// Проверка есть ли эффекты
    /// </summary>
    public bool HasEffects()
    {
        return effects != null && effects.Count > 0;
    }
}

// ════════════════════════════════════════════════════════════
// ENUMS
// ════════════════════════════════════════════════════════════

/// <summary>
/// Тип скилла (определяет основную механику)
/// </summary>
public enum SkillConfigType
{
    ProjectileDamage,   // Снаряд с уроном (Fireball, Arrow, Hammer)
    InstantDamage,      // Мгновенный урон без снаряда (Holy Strike, Backstab)
    AOEDamage,          // Область поражения (Ice Nova, Explosive Arrow)
    Heal,               // Лечение (Lay on Hands, Healing Prayer)
    DamageAndHeal,      // Урон + лечение (Soul Drain - вампиризм)
    Buff,               // Положительный эффект на союзника (Battle Cry, Divine Shield)
    Debuff,             // Отрицательный эффект на врага (без урона, только эффект)
    CrowdControl,       // Контроль (Stun, Root, Sleep) - может быть с уроном или без
    Movement,           // Движение (Charge, Teleport, Shadow Step, Blink)
    Summon,             // Призыв существ (Rogue - Skeletons)
    Transformation,     // Трансформация (Paladin - Bear Form)
    Resurrection        // Воскрешение (Paladin - Resurrection)
}

/// <summary>
/// Тип цели
/// </summary>
public enum SkillTargetType
{
    Self,           // На себя (бафы, трансформация)
    Enemy,          // На врага (требует выбор цели)
    Ally,           // На союзника (лечение, бафы)
    Ground,         // По земле / позиции (AOE, ground target)
    NoTarget,       // Без цели (вокруг себя)
    Direction       // Направление (конус, линия)
}

/// <summary>
/// Тип движения
/// </summary>
public enum MovementType
{
    None,           // Нет движения
    Dash,           // Рывок вперёд (быстрое движение)
    Charge,         // Заряд на врага (к цели)
    Teleport,       // Телепорт (мгновенное перемещение)
    Blink,          // Мигание (короткий телепорт)
    Leap,           // Прыжок
    Roll            // Перекат
}

/// <summary>
/// Направление движения
/// </summary>
public enum MovementDirection
{
    Forward,        // Вперёд (по направлению персонажа)
    Backward,       // Назад
    ToTarget,       // К цели
    AwayFromTarget, // От цели
    MouseDirection  // По направлению курсора мыши
}

// ════════════════════════════════════════════════════════════
// CUSTOM DATA ДЛЯ СПЕЦИАЛЬНЫХ СКИЛЛОВ
// ════════════════════════════════════════════════════════════

/// <summary>
/// Дополнительные данные для специальных механик скиллов
/// </summary>
[System.Serializable]
public class SkillCustomData
{
    [Header("Chain Lightning (молнии перепрыгивают)")]
    [Tooltip("Количество прыжков молнии (0 = нет chain lightning)")]
    [Range(0, 10)]
    public int chainCount = 0;

    [Tooltip("Радиус поиска следующей цели для chain lightning")]
    [Range(0f, 20f)]
    public float chainRadius = 8f;

    [Tooltip("Множитель урона для каждого прыжка (1.0 = 100%, 0.7 = 70%)")]
    [Range(0.1f, 1f)]
    public float chainDamageMultiplier = 0.7f;

    [Header("Multi-Hit (несколько ударов)")]
    [Tooltip("Количество ударов (для скиллов типа Flurry, Rapid Fire)")]
    [Range(1, 10)]
    public int hitCount = 1;

    [Tooltip("Задержка между ударами (секунды)")]
    [Range(0f, 2f)]
    public float hitDelay = 0.1f;

    [Header("Blood for Mana (жертвенное заклинание)")]
    [Tooltip("Процент маны для восстановления (для Blood for Mana)")]
    [Range(0f, 100f)]
    public float manaRestorePercent = 0f;

    [Header("Другие параметры")]
    [Tooltip("Дополнительные флаги и настройки")]
    public bool piercing = false; // Пронзающий снаряд (проходит сквозь врагов)

    [Tooltip("Максимум пронзаемых целей")]
    [Range(1, 10)]
    public int maxPierceTargets = 3;
}
