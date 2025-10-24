using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Конвертер SkillData (старая система) → SkillConfig (новая система)
/// Используется для загрузки скиллов из Resources/Skills/, которые сохранены как SkillData
/// </summary>
public static class SkillDataConverter
{
    /// <summary>
    /// Конвертировать SkillConfig в SkillData (для обратной совместимости с UI)
    /// </summary>
    public static SkillData ConvertToSkillData(SkillConfig skillConfig)
    {
        if (skillConfig == null)
        {
            Debug.LogError("[SkillDataConverter] ❌ skillConfig is NULL!");
            return null;
        }

        // Создаём новый SkillData
        SkillData data = ScriptableObject.CreateInstance<SkillData>();

        // Копируем основные поля
        data.skillId = skillConfig.skillId;
        data.skillName = skillConfig.skillName;
        data.description = skillConfig.description;
        data.icon = skillConfig.icon;
        data.characterClass = skillConfig.characterClass;

        // Параметры использования
        data.cooldown = skillConfig.cooldown;
        data.manaCost = skillConfig.manaCost;
        data.castRange = skillConfig.castRange;
        data.castTime = skillConfig.castTime;
        data.canUseWhileMoving = skillConfig.canUseWhileMoving;

        // Тип скилла (конвертируем обратно)
        data.skillType = ConvertSkillConfigTypeToOld(skillConfig.skillType);
        data.targetType = ConvertTargetTypeToOld(skillConfig.targetType);
        data.requiresTarget = skillConfig.requiresTarget;
        data.canTargetAllies = skillConfig.canTargetAllies;
        data.canTargetEnemies = skillConfig.canTargetEnemies;

        // Урон/лечение
        data.baseDamageOrHeal = skillConfig.baseDamageOrHeal;
        data.strengthScaling = skillConfig.strengthScaling;
        data.intelligenceScaling = skillConfig.intelligenceScaling;

        // Эффекты (конвертируем обратно List<EffectConfig> → List<SkillEffect>)
        data.effects = ConvertEffectsToOld(skillConfig.effects);

        // AOE
        data.aoeRadius = skillConfig.aoeRadius;
        data.maxTargets = skillConfig.maxTargets;

        // Снаряд
        data.projectilePrefab = skillConfig.projectilePrefab;
        data.projectileSpeed = skillConfig.projectileSpeed;
        data.projectileLifetime = skillConfig.projectileLifetime;
        data.projectileHoming = skillConfig.projectileHoming;

        // Анимация
        data.animationTrigger = skillConfig.animationTrigger;
        data.animationSpeed = skillConfig.animationSpeed;

        // Визуальные эффекты
        data.visualEffectPrefab = skillConfig.castEffectPrefab;
        data.casterEffectPrefab = skillConfig.casterEffectPrefab;
        data.projectileHitEffectPrefab = skillConfig.hitEffectPrefab;

        // Звуки
        data.castSound = skillConfig.castSound;
        data.impactSound = skillConfig.hitSound;

        // Движение
        data.enableMovement = skillConfig.enableMovement;
        data.movementType = ConvertMovementTypeToOld(skillConfig.movementType);
        data.movementDistance = skillConfig.movementDistance;
        data.movementSpeed = skillConfig.movementSpeed;
        data.movementDirection = ConvertMovementDirectionToOld(skillConfig.movementDirection);

        // Призыв
        data.summonPrefab = skillConfig.summonPrefab;
        data.summonCount = skillConfig.summonCount;
        data.summonDuration = skillConfig.summonDuration;

        // Трансформация
        data.transformationModel = skillConfig.transformationModel;
        data.transformationDuration = skillConfig.transformationDuration;
        data.hpBonusPercent = skillConfig.hpBonusPercent;
        data.physicalDamageBonusPercent = skillConfig.damageBonusPercent;

        return data;
    }

    /// <summary>
    /// Конвертировать SkillData в SkillConfig
    /// </summary>
    public static SkillConfig ConvertToSkillConfig(SkillData skillData)
    {
        if (skillData == null)
        {
            Debug.LogError("[SkillDataConverter] ❌ skillData is NULL!");
            return null;
        }

        // Создаём новый SkillConfig
        SkillConfig config = ScriptableObject.CreateInstance<SkillConfig>();

        // Копируем основные поля
        config.skillId = skillData.skillId;
        config.skillName = skillData.skillName;
        config.description = skillData.description;
        config.icon = skillData.icon;
        config.characterClass = skillData.characterClass;

        // Параметры использования
        config.cooldown = skillData.cooldown;
        config.manaCost = skillData.manaCost;
        config.castRange = skillData.castRange;
        config.castTime = skillData.castTime;
        config.canUseWhileMoving = skillData.canUseWhileMoving;

        // Тип скилла (конвертируем enum)
        config.skillType = ConvertSkillType(skillData.skillType);
        config.targetType = ConvertTargetType(skillData.targetType);
        config.requiresTarget = skillData.requiresTarget;
        config.canTargetAllies = skillData.canTargetAllies;
        config.canTargetEnemies = skillData.canTargetEnemies;

        // Урон/лечение
        config.baseDamageOrHeal = skillData.baseDamageOrHeal;
        config.strengthScaling = skillData.strengthScaling;
        config.intelligenceScaling = skillData.intelligenceScaling;

        // Эффекты (конвертируем List<SkillEffect> → List<EffectConfig>)
        config.effects = ConvertEffects(skillData.effects);

        // AOE
        config.aoeRadius = skillData.aoeRadius;
        config.maxTargets = skillData.maxTargets;

        // Снаряд
        config.projectilePrefab = skillData.projectilePrefab;
        config.projectileSpeed = skillData.projectileSpeed;
        config.projectileLifetime = skillData.projectileLifetime;
        config.projectileHoming = skillData.projectileHoming;

        // Анимация
        config.animationTrigger = skillData.animationTrigger;
        config.animationSpeed = skillData.animationSpeed;

        // Визуальные эффекты
        config.castEffectPrefab = skillData.visualEffectPrefab; // SkillData.visualEffectPrefab → SkillConfig.castEffectPrefab
        config.casterEffectPrefab = skillData.casterEffectPrefab;
        config.hitEffectPrefab = skillData.projectileHitEffectPrefab; // SkillData.projectileHitEffectPrefab → SkillConfig.hitEffectPrefab

        // Звуки
        config.castSound = skillData.castSound;
        config.hitSound = skillData.impactSound; // SkillData.impactSound → SkillConfig.hitSound

        // Движение
        config.enableMovement = skillData.enableMovement;
        config.movementType = ConvertMovementType(skillData.movementType);
        config.movementDistance = skillData.movementDistance;
        config.movementSpeed = skillData.movementSpeed;
        config.movementDirection = ConvertMovementDirection(skillData.movementDirection);

        // Призыв
        config.summonPrefab = skillData.summonPrefab;
        config.summonCount = skillData.summonCount;
        config.summonDuration = skillData.summonDuration;

        // Трансформация
        config.transformationModel = skillData.transformationModel;
        config.transformationDuration = skillData.transformationDuration;
        config.hpBonusPercent = skillData.hpBonusPercent;
        config.damageBonusPercent = skillData.physicalDamageBonusPercent; // SkillData.physicalDamageBonusPercent → SkillConfig.damageBonusPercent

        // Сетевая синхронизация (по умолчанию true)
        config.syncProjectiles = true;
        config.syncHitEffects = true;
        config.syncStatusEffects = true;

        return config;
    }

    /// <summary>
    /// Конвертировать SkillType (старый) → SkillConfigType (новый)
    /// </summary>
    private static SkillConfigType ConvertSkillType(SkillType oldType)
    {
        switch (oldType)
        {
            case SkillType.Damage:
                return SkillConfigType.ProjectileDamage; // По умолчанию считаем что это снаряд
            case SkillType.Heal:
                return SkillConfigType.Heal;
            case SkillType.Buff:
                return SkillConfigType.Buff;
            case SkillType.Debuff:
                return SkillConfigType.Debuff;
            case SkillType.CrowdControl:
                return SkillConfigType.CrowdControl;
            case SkillType.Summon:
                return SkillConfigType.Summon;
            case SkillType.Transformation:
                return SkillConfigType.Transformation;
            case SkillType.Teleport:
                return SkillConfigType.Movement;
            case SkillType.Ressurect:
                return SkillConfigType.Resurrection;
            default:
                Debug.LogWarning($"[SkillDataConverter] ⚠️ Unknown SkillType: {oldType}. Defaulting to ProjectileDamage.");
                return SkillConfigType.ProjectileDamage;
        }
    }

    /// <summary>
    /// Конвертировать OldSkillTargetType → SkillTargetType
    /// </summary>
    private static SkillTargetType ConvertTargetType(OldSkillTargetType oldType)
    {
        switch (oldType)
        {
            case OldSkillTargetType.Self:
                return SkillTargetType.Self;
            case OldSkillTargetType.SingleTarget:
                return SkillTargetType.Enemy; // По умолчанию считаем что это враг
            case OldSkillTargetType.GroundTarget:
                return SkillTargetType.Ground;
            case OldSkillTargetType.NoTarget:
                return SkillTargetType.NoTarget;
            case OldSkillTargetType.Directional:
                return SkillTargetType.Direction;
            default:
                Debug.LogWarning($"[SkillDataConverter] ⚠️ Unknown OldSkillTargetType: {oldType}. Defaulting to Enemy.");
                return SkillTargetType.Enemy;
        }
    }

    /// <summary>
    /// Конвертировать OldMovementType → MovementType
    /// </summary>
    private static MovementType ConvertMovementType(OldMovementType oldType)
    {
        switch (oldType)
        {
            case OldMovementType.None:
                return MovementType.None;
            case OldMovementType.Dash:
                return MovementType.Dash;
            case OldMovementType.Charge:
                return MovementType.Charge;
            case OldMovementType.Teleport:
                return MovementType.Teleport;
            case OldMovementType.Leap:
                return MovementType.Leap;
            case OldMovementType.Roll:
                return MovementType.Roll;
            case OldMovementType.Blink:
                return MovementType.Blink;
            default:
                return MovementType.None;
        }
    }

    /// <summary>
    /// Конвертировать OldMovementDirection → MovementDirection
    /// </summary>
    private static MovementDirection ConvertMovementDirection(OldMovementDirection oldDir)
    {
        switch (oldDir)
        {
            case OldMovementDirection.Forward:
                return MovementDirection.Forward;
            case OldMovementDirection.Backward:
                return MovementDirection.Backward;
            case OldMovementDirection.ToTarget:
                return MovementDirection.ToTarget;
            case OldMovementDirection.AwayFromTarget:
                return MovementDirection.AwayFromTarget;
            case OldMovementDirection.MouseDirection:
                return MovementDirection.MouseDirection;
            default:
                return MovementDirection.Forward;
        }
    }

    /// <summary>
    /// Конвертировать List<SkillEffect> → List<EffectConfig>
    /// </summary>
    private static List<EffectConfig> ConvertEffects(List<SkillEffect> oldEffects)
    {
        List<EffectConfig> newEffects = new List<EffectConfig>();

        if (oldEffects == null || oldEffects.Count == 0)
            return newEffects;

        foreach (SkillEffect oldEffect in oldEffects)
        {
            EffectConfig newEffect = new EffectConfig
            {
                effectType = ConvertEffectType(oldEffect.effectType),
                duration = oldEffect.duration,
                power = oldEffect.power,
                damageOrHealPerTick = oldEffect.damageOrHealPerTick,
                tickInterval = oldEffect.tickInterval,
                particleEffectPrefab = oldEffect.particleEffectPrefab,
                applySound = oldEffect.applySound,
                removeSound = oldEffect.removeSound,
                canBeDispelled = oldEffect.canBeDispelled,
                canStack = oldEffect.canStack,
                maxStacks = oldEffect.maxStacks,
                syncWithServer = oldEffect.syncWithServer
            };

            newEffects.Add(newEffect);
        }

        return newEffects;
    }

    /// <summary>
    /// Конвертировать OldEffectType → EffectType
    /// </summary>
    private static EffectType ConvertEffectType(OldEffectType oldType)
    {
        switch (oldType)
        {
            // Баффы
            case OldEffectType.IncreaseAttack:
                return EffectType.IncreaseAttack;
            case OldEffectType.IncreaseDefense:
                return EffectType.IncreaseDefense;
            case OldEffectType.IncreaseSpeed:
                return EffectType.IncreaseSpeed;
            case OldEffectType.IncreaseHPRegen:
                return EffectType.IncreaseHPRegen;
            case OldEffectType.IncreaseMPRegen:
                return EffectType.IncreaseMPRegen;
            case OldEffectType.Shield:
                return EffectType.Shield;
            case OldEffectType.IncreasePerception:
                return EffectType.IncreasePerception;

            // Дебаффы
            case OldEffectType.DecreaseAttack:
                return EffectType.DecreaseAttack;
            case OldEffectType.DecreaseDefense:
                return EffectType.DecreaseDefense;
            case OldEffectType.DecreaseSpeed:
                return EffectType.DecreaseSpeed;
            case OldEffectType.Poison:
                return EffectType.Poison;
            case OldEffectType.Burn:
                return EffectType.Burn;
            case OldEffectType.Bleed:
                return EffectType.Bleed;

            // Контроль
            case OldEffectType.Stun:
                return EffectType.Stun;
            case OldEffectType.Root:
                return EffectType.Root;
            case OldEffectType.Sleep:
                return EffectType.Sleep;
            case OldEffectType.Silence:
                return EffectType.Silence;
            case OldEffectType.Fear:
                return EffectType.Fear;
            case OldEffectType.Taunt:
                return EffectType.Taunt;

            // Особые
            case OldEffectType.DamageOverTime:
                return EffectType.DamageOverTime;
            case OldEffectType.HealOverTime:
                return EffectType.HealOverTime;
            case OldEffectType.Invulnerability:
                return EffectType.Invulnerability;
            case OldEffectType.Invisibility:
                return EffectType.Invisibility;

            default:
                Debug.LogWarning($"[SkillDataConverter] ⚠️ Unknown OldEffectType: {oldType}. Defaulting to Stun.");
                return EffectType.Stun;
        }
    }

    // =============== ОБРАТНЫЕ МЕТОДЫ КОНВЕРТАЦИИ (SkillConfig → SkillData) ===============

    /// <summary>
    /// Конвертировать SkillConfigType → SkillType (старый)
    /// </summary>
    private static SkillType ConvertSkillConfigTypeToOld(SkillConfigType newType)
    {
        switch (newType)
        {
            case SkillConfigType.ProjectileDamage:
            case SkillConfigType.InstantDamage:
            case SkillConfigType.AOEDamage:
                return SkillType.Damage;
            case SkillConfigType.Heal:
                return SkillType.Heal;
            case SkillConfigType.Buff:
                return SkillType.Buff;
            case SkillConfigType.Debuff:
                return SkillType.Debuff;
            case SkillConfigType.CrowdControl:
                return SkillType.CrowdControl;
            case SkillConfigType.Summon:
                return SkillType.Summon;
            case SkillConfigType.Transformation:
                return SkillType.Transformation;
            case SkillConfigType.Movement:
                return SkillType.Teleport;
            case SkillConfigType.Resurrection:
                return SkillType.Ressurect;
            default:
                return SkillType.Damage;
        }
    }

    /// <summary>
    /// Конвертировать SkillTargetType → OldSkillTargetType
    /// </summary>
    private static OldSkillTargetType ConvertTargetTypeToOld(SkillTargetType newType)
    {
        switch (newType)
        {
            case SkillTargetType.Self:
                return OldSkillTargetType.Self;
            case SkillTargetType.Enemy:
            case SkillTargetType.Ally:
                return OldSkillTargetType.SingleTarget;
            case SkillTargetType.Ground:
                return OldSkillTargetType.GroundTarget;
            case SkillTargetType.NoTarget:
                return OldSkillTargetType.NoTarget;
            case SkillTargetType.Direction:
                return OldSkillTargetType.Directional;
            default:
                return OldSkillTargetType.SingleTarget;
        }
    }

    /// <summary>
    /// Конвертировать MovementType → OldMovementType
    /// </summary>
    private static OldMovementType ConvertMovementTypeToOld(MovementType newType)
    {
        switch (newType)
        {
            case MovementType.None:
                return OldMovementType.None;
            case MovementType.Dash:
                return OldMovementType.Dash;
            case MovementType.Charge:
                return OldMovementType.Charge;
            case MovementType.Teleport:
                return OldMovementType.Teleport;
            case MovementType.Leap:
                return OldMovementType.Leap;
            case MovementType.Roll:
                return OldMovementType.Roll;
            case MovementType.Blink:
                return OldMovementType.Blink;
            default:
                return OldMovementType.None;
        }
    }

    /// <summary>
    /// Конвертировать MovementDirection → OldMovementDirection
    /// </summary>
    private static OldMovementDirection ConvertMovementDirectionToOld(MovementDirection newDir)
    {
        switch (newDir)
        {
            case MovementDirection.Forward:
                return OldMovementDirection.Forward;
            case MovementDirection.Backward:
                return OldMovementDirection.Backward;
            case MovementDirection.ToTarget:
                return OldMovementDirection.ToTarget;
            case MovementDirection.AwayFromTarget:
                return OldMovementDirection.AwayFromTarget;
            case MovementDirection.MouseDirection:
                return OldMovementDirection.MouseDirection;
            default:
                return OldMovementDirection.Forward;
        }
    }

    /// <summary>
    /// Конвертировать List<EffectConfig> → List<SkillEffect>
    /// </summary>
    private static List<SkillEffect> ConvertEffectsToOld(List<EffectConfig> newEffects)
    {
        List<SkillEffect> oldEffects = new List<SkillEffect>();

        if (newEffects == null || newEffects.Count == 0)
            return oldEffects;

        foreach (EffectConfig newEffect in newEffects)
        {
            SkillEffect oldEffect = new SkillEffect
            {
                effectType = ConvertEffectTypeToOld(newEffect.effectType),
                duration = newEffect.duration,
                power = newEffect.power,
                damageOrHealPerTick = newEffect.damageOrHealPerTick,
                tickInterval = newEffect.tickInterval,
                particleEffectPrefab = newEffect.particleEffectPrefab,
                applySound = newEffect.applySound,
                removeSound = newEffect.removeSound,
                canBeDispelled = newEffect.canBeDispelled,
                canStack = newEffect.canStack,
                maxStacks = newEffect.maxStacks,
                syncWithServer = newEffect.syncWithServer
            };

            oldEffects.Add(oldEffect);
        }

        return oldEffects;
    }

    /// <summary>
    /// Конвертировать EffectType → OldEffectType
    /// </summary>
    private static OldEffectType ConvertEffectTypeToOld(EffectType newType)
    {
        switch (newType)
        {
            // Баффы
            case EffectType.IncreaseAttack:
                return OldEffectType.IncreaseAttack;
            case EffectType.IncreaseDefense:
                return OldEffectType.IncreaseDefense;
            case EffectType.IncreaseSpeed:
                return OldEffectType.IncreaseSpeed;
            case EffectType.IncreaseHPRegen:
                return OldEffectType.IncreaseHPRegen;
            case EffectType.IncreaseMPRegen:
                return OldEffectType.IncreaseMPRegen;
            case EffectType.Shield:
                return OldEffectType.Shield;
            case EffectType.IncreasePerception:
                return OldEffectType.IncreasePerception;

            // Дебаффы
            case EffectType.DecreaseAttack:
                return OldEffectType.DecreaseAttack;
            case EffectType.DecreaseDefense:
                return OldEffectType.DecreaseDefense;
            case EffectType.DecreaseSpeed:
                return OldEffectType.DecreaseSpeed;
            case EffectType.Poison:
                return OldEffectType.Poison;
            case EffectType.Burn:
                return OldEffectType.Burn;
            case EffectType.Bleed:
                return OldEffectType.Bleed;

            // Контроль
            case EffectType.Stun:
                return OldEffectType.Stun;
            case EffectType.Root:
                return OldEffectType.Root;
            case EffectType.Sleep:
                return OldEffectType.Sleep;
            case EffectType.Silence:
                return OldEffectType.Silence;
            case EffectType.Fear:
                return OldEffectType.Fear;
            case EffectType.Taunt:
                return OldEffectType.Taunt;

            // Особые
            case EffectType.DamageOverTime:
                return OldEffectType.DamageOverTime;
            case EffectType.HealOverTime:
                return OldEffectType.HealOverTime;
            case EffectType.Invulnerability:
                return OldEffectType.Invulnerability;
            case EffectType.Invisibility:
                return OldEffectType.Invisibility;

            default:
                return OldEffectType.Stun;
        }
    }
}
