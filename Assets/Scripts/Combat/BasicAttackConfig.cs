using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Тип атаки персонажа
/// </summary>
public enum AttackType
{
    Melee,   // Ближняя атака (Warrior, Paladin)
    Ranged   // Дальняя атака (Mage, Archer, Rogue)
}

// ПРИМЕЧАНИЕ: CharacterClass уже определён в Assets/Scripts/Data/CharacterData.cs

/// <summary>
/// Конфигурация базовой атаки класса персонажа
/// ScriptableObject для удобного редактирования в Inspector
///
/// НАЗНАЧЕНИЕ:
/// - Определяет тип атаки (ближняя/дальняя)
/// - Настройки снаряда для дальнего боя
/// - Урон и характеристики атаки
/// - Визуальные и звуковые эффекты
/// </summary>
[CreateAssetMenu(fileName = "BasicAttack_", menuName = "Aetherion/Combat/Basic Attack Config", order = 1)]
public class BasicAttackConfig : ScriptableObject
{
    [Header("═══════════ БАЗОВАЯ ИНФОРМАЦИЯ ═══════════")]
    [Tooltip("Название класса (Warrior, Mage, Archer, Rogue, Paladin)")]
    public CharacterClass characterClass;

    [Tooltip("Тип атаки")]
    public AttackType attackType = AttackType.Melee;

    [Tooltip("Описание атаки")]
    [TextArea(2, 3)]
    public string description = "Базовая атака класса";

    [Header("═══════════ УРОН ═══════════")]
    [Tooltip("Базовый урон атаки")]
    [Range(1f, 200f)]
    public float baseDamage = 25f;

    [Tooltip("Скейлинг от Strength (для физических атак)")]
    [Range(0f, 5f)]
    public float strengthScaling = 1.0f;

    [Tooltip("Скейлинг от Intelligence (для магических атак)")]
    [Range(0f, 5f)]
    public float intelligenceScaling = 0f;

    [Header("═══════════ СКОРОСТЬ АТАКИ ═══════════")]
    [Tooltip("Базовый кулдаун между атаками (секунды)")]
    [Range(0.3f, 3f)]
    public float attackCooldown = 1.0f;

    [Tooltip("Дальность атаки (метры)")]
    [Range(1f, 50f)]
    public float attackRange = 3f;

    [Header("═══════════ СНАРЯД (только для дальних атак) ═══════════")]
    [Tooltip("Префаб снаряда (стрела, файрбол, кинжал и т.д.)")]
    public GameObject projectilePrefab;

    [Tooltip("Скорость снаряда (м/с)")]
    [Range(5f, 50f)]
    public float projectileSpeed = 20f;

    [Tooltip("Время жизни снаряда (секунды)")]
    [Range(1f, 10f)]
    public float projectileLifetime = 5f;

    [Tooltip("Автонаведение на цель")]
    public bool projectileHoming = false;

    [Tooltip("Скорость поворота при автонаведении (градусы/сек)")]
    [Range(0f, 20f)]
    public float homingSpeed = 10f;

    [Tooltip("Радиус поиска цели для автонаведения (метры)")]
    [Range(0f, 50f)]
    public float homingRadius = 30f;

    [Header("═══════════ ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ═══════════")]
    [Tooltip("Эффект попадания (взрыв, искры, кровь)")]
    public GameObject hitEffectPrefab;

    [Tooltip("Эффект на оружии во время атаки (свечение, след)")]
    public GameObject weaponEffectPrefab;

    [Tooltip("Эффект на кончике оружия (для спавна снарядов)")]
    public GameObject muzzleFlashPrefab;

    [Header("═══════════ ЗВУКИ ═══════════")]
    [Tooltip("Звук выстрела/удара")]
    public AudioClip attackSound;

    [Tooltip("Звук попадания")]
    public AudioClip hitSound;

    [Tooltip("Громкость звуков (0-1)")]
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;

    [Header("═══════════ АНИМАЦИЯ ═══════════")]
    [Tooltip("Имя триггера анимации атаки в Animator")]
    public string animationTrigger = "Attack";

    [Tooltip("Скорость анимации атаки (1.0 = нормально)")]
    [Range(0.5f, 3f)]
    public float animationSpeed = 1.0f;

    [Tooltip("Момент выстрела/удара в анимации (0.0-1.0)")]
    [Range(0f, 1f)]
    public float attackHitTiming = 0.5f;

    [Header("═══════════ РАСХОД РЕСУРСОВ ═══════════")]
    [Tooltip("Стоимость маны за атаку (0 = бесплатно)")]
    [Range(0f, 100f)]
    public float manaCostPerAttack = 0f;

    [Tooltip("Стоимость Action Points за атаку")]
    [Range(1f, 10f)]
    public float actionPointsCost = 1f;

    [Header("═══════════ ДОПОЛНИТЕЛЬНЫЕ ЭФФЕКТЫ ═══════════")]
    [Tooltip("Эффекты при попадании (яд, поджог, замедление и т.д.)")]
    public List<SkillEffect> onHitEffects = new List<SkillEffect>();

    [Header("═══════════ ОСОБЫЕ МЕХАНИКИ ═══════════")]
    [Tooltip("Шанс критического удара (базовый, без учета Luck)")]
    [Range(0f, 100f)]
    public float baseCritChance = 5f;

    [Tooltip("Множитель критического урона")]
    [Range(1.5f, 3f)]
    public float critMultiplier = 2f;

    [Tooltip("Пробивание насквозь (снаряд продолжает лететь после попадания)")]
    public bool piercingAttack = false;

    [Tooltip("Максимум целей для пробивания (если pierce = true)")]
    [Range(1, 10)]
    public int maxPierceTargets = 1;

    [Header("═══════════ МУЛЬТИПЛЕЕР ═══════════")]
    [Tooltip("Синхронизировать снаряды с сервером")]
    public bool syncProjectiles = true;

    [Tooltip("Синхронизировать эффекты попадания")]
    public bool syncHitEffects = true;

    /// <summary>
    /// Рассчитать финальный урон с учетом характеристик
    /// </summary>
    public float CalculateDamage(CharacterStats stats)
    {
        if (stats == null)
        {
            Debug.LogWarning($"[BasicAttackConfig] CharacterStats is null, using base damage: {baseDamage}");
            return baseDamage;
        }

        float damage = baseDamage;
        damage += stats.strength * strengthScaling;
        damage += stats.intelligence * intelligenceScaling;

        // Применяем модификатор атаки (от Battle Rage и других баффов)
        if (stats.AttackModifier > 0)
        {
            float bonus = damage * (stats.AttackModifier / 100f);
            damage += bonus;
            Debug.Log($"[BasicAttackConfig] ⚔️ Attack modifier applied: +{stats.AttackModifier}% (+{bonus:F1} damage)");
        }

        Debug.Log($"[BasicAttackConfig] {characterClass} Attack Damage: {baseDamage} + STR({stats.strength}×{strengthScaling}) + INT({stats.intelligence}×{intelligenceScaling}) = {damage}");
        return damage;
    }

    /// <summary>
    /// Проверка валидности конфига
    /// </summary>
    public bool Validate(out string errorMessage)
    {
        errorMessage = "";

        // Проверка дальних атак
        if (attackType == AttackType.Ranged)
        {
            if (projectilePrefab == null)
            {
                errorMessage = $"[{characterClass}] Ranged attack требует projectilePrefab!";
                return false;
            }

            // Проверка что префаб имеет компонент снаряда
            var projectileComponent = projectilePrefab.GetComponent<Projectile>();
            var arrowComponent = projectilePrefab.GetComponent<ArrowProjectile>();
            var celestialComponent = projectilePrefab.GetComponent<CelestialProjectile>();

            if (projectileComponent == null && arrowComponent == null && celestialComponent == null)
            {
                errorMessage = $"[{characterClass}] projectilePrefab должен иметь компонент Projectile/ArrowProjectile/CelestialProjectile!";
                return false;
            }
        }

        // Проверка базовых параметров
        if (baseDamage <= 0)
        {
            errorMessage = $"[{characterClass}] baseDamage должен быть > 0!";
            return false;
        }

        if (attackCooldown <= 0)
        {
            errorMessage = $"[{characterClass}] attackCooldown должен быть > 0!";
            return false;
        }

        if (attackRange <= 0)
        {
            errorMessage = $"[{characterClass}] attackRange должен быть > 0!";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Получить информацию о конфиге для отладки
    /// </summary>
    public string GetDebugInfo()
    {
        return $@"
═══════════════════════════════════════════
BASIC ATTACK CONFIG: {characterClass}
═══════════════════════════════════════════
Type: {attackType}
Damage: {baseDamage} (STR×{strengthScaling}, INT×{intelligenceScaling})
Cooldown: {attackCooldown}s
Range: {attackRange}m
{(attackType == AttackType.Ranged ? $@"
PROJECTILE:
  Prefab: {(projectilePrefab != null ? projectilePrefab.name : "NULL")}
  Speed: {projectileSpeed}m/s
  Lifetime: {projectileLifetime}s
  Homing: {projectileHoming}
" : "")}
Animation: {animationTrigger} (speed: {animationSpeed}x)
Effects: {onHitEffects.Count} on-hit effects
Mana Cost: {manaCostPerAttack}
AP Cost: {actionPointsCost}
═══════════════════════════════════════════";
    }
}