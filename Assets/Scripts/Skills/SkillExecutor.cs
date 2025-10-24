using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SkillExecutor : MonoBehaviour
{
    [Header("Equipped Skills")]
    public List<SkillConfig> equippedSkills = new List<SkillConfig>();

    [Header("Components")]
    private EffectManager effectManager;
    private CharacterStats stats;
    private ManaSystem manaSystem;
    private HealthSystem healthSystem;
    private Animator animator;

    [Header("Cooldowns")]
    private Dictionary<int, float> cooldownTimers = new Dictionary<int, float>();

    [Header("Minions")]
    private GameObject activeMinion; // Текущий активный миньон (скелет)

    [Header("Debug")]
    public bool enableLogs = true;

    void Awake()
    {
        effectManager = GetComponent<EffectManager>();
        if (effectManager == null)
        {
            effectManager = gameObject.AddComponent<EffectManager>();
        }

        stats = GetComponent<CharacterStats>();
        manaSystem = GetComponent<ManaSystem>();
        healthSystem = GetComponent<HealthSystem>();
        animator = GetComponent<Animator>();

        if (manaSystem == null)
        {
            Debug.LogWarning("[SkillExecutor] ⚠️ ManaSystem не найден! Скиллы не будут тратить ману.");
        }

        if (healthSystem == null)
        {
            Debug.LogWarning("[SkillExecutor] ⚠️ HealthSystem не найден!");
        }
    }

    void Update()
    {
        List<int> keys = new List<int>(cooldownTimers.Keys);
        foreach (int slotIndex in keys)
        {
            cooldownTimers[slotIndex] -= Time.deltaTime;
            if (cooldownTimers[slotIndex] <= 0f)
            {
                cooldownTimers.Remove(slotIndex);
            }
        }
    }

    public bool UseSkill(int slotIndex, Transform target = null, Vector3? groundTarget = null)
    {
        if (slotIndex < 0 || slotIndex >= equippedSkills.Count)
        {
            Log("Invalid slot index: " + slotIndex);
            return false;
        }

        SkillConfig skill = equippedSkills[slotIndex];
        if (skill == null)
        {
            Log("No skill in slot " + slotIndex);
            return false;
        }

        if (cooldownTimers.ContainsKey(slotIndex))
        {
            Log("Skill on cooldown: " + cooldownTimers[slotIndex]);
            return false;
        }

        // КРИТИЧЕСКАЯ ПРОВЕРКА: Достаточно ли маны?
        if (manaSystem != null && skill.manaCost > 0)
        {
            if (!manaSystem.HasEnoughMana(skill.manaCost))
            {
                Log($"❌ Недостаточно маны! Нужно: {skill.manaCost:F0}, Есть: {manaSystem.CurrentMana:F0}");
                return false;
            }
        }

        if (skill.requiresTarget && target == null && !groundTarget.HasValue)
        {
            Log("Skill requires target");
            return false;
        }

        // КРИТИЧЕСКАЯ ОПЕРАЦИЯ: Тратим ману ПЕРЕД выполнением скилла
        if (manaSystem != null && skill.manaCost > 0)
        {
            bool manaSpent = manaSystem.SpendMana(skill.manaCost);
            if (!manaSpent)
            {
                Log($"❌ Не удалось потратить ману!");
                return false;
            }
            Log($"✅ Потрачено {skill.manaCost:F0} маны. Осталось: {manaSystem.CurrentMana:F0}/{manaSystem.MaxMana:F0}");
        }

        Log("Using skill: " + skill.skillName);
        StartCoroutine(ExecuteSkill(skill, slotIndex, target, groundTarget));
        return true;
    }

    /// <summary>
    /// Установить скилл в определённый слот (1-5)
    /// Вызывается из SkillManager при загрузке персонажа
    /// </summary>
    public void SetSkill(int slotNumber, SkillConfig skill)
    {
        if (slotNumber < 1 || slotNumber > 5)
        {
            Debug.LogError($"[SkillExecutor] ❌ Некорректный номер слота: {slotNumber}. Должен быть 1-5.");
            return;
        }

        int slotIndex = slotNumber - 1; // Преобразуем 1-5 в 0-4

        // Расширяем список если нужно
        while (equippedSkills.Count <= slotIndex)
        {
            equippedSkills.Add(null);
        }

        equippedSkills[slotIndex] = skill;

        if (skill != null)
        {
            Log($"✅ Скилл '{skill.skillName}' (ID: {skill.skillId}) установлен в слот {slotNumber}");
        }
        else
        {
            Log($"⚠️ Слот {slotNumber} очищен (skill = null)");
        }
    }

    /// <summary>
    /// Получить скилл из слота (1-5)
    /// </summary>
    public SkillConfig GetSkill(int slotNumber)
    {
        if (slotNumber < 1 || slotNumber > 5)
        {
            Debug.LogError($"[SkillExecutor] ❌ Некорректный номер слота: {slotNumber}");
            return null;
        }

        int slotIndex = slotNumber - 1;
        if (slotIndex >= equippedSkills.Count)
        {
            return null;
        }

        return equippedSkills[slotIndex];
    }

    /// <summary>
    /// Очистить все слоты
    /// </summary>
    public void ClearAllSkills()
    {
        equippedSkills.Clear();
        cooldownTimers.Clear();
        Log("🧹 Все скиллы очищены");
    }

    public float GetCooldown(int slotIndex)
    {
        return cooldownTimers.ContainsKey(slotIndex) ? cooldownTimers[slotIndex] : 0f;
    }

    private IEnumerator ExecuteSkill(SkillConfig skill, int slotIndex, Transform target, Vector3? groundTarget)
    {
        if (animator != null && !string.IsNullOrEmpty(skill.animationTrigger))
        {
            animator.SetTrigger(skill.animationTrigger);
        }

        if (skill.castTime > 0f)
        {
            yield return new WaitForSeconds(skill.castTime);
        }

        cooldownTimers[slotIndex] = skill.cooldown;

        switch (skill.skillType)
        {
            case SkillConfigType.ProjectileDamage:
            case SkillConfigType.DamageAndHeal: // Soul Drain и другие вампирические скиллы
                ExecuteProjectile(skill, target, groundTarget);
                break;
            case SkillConfigType.AOEDamage:
                ExecuteAOEDamage(skill, target, groundTarget);
                break;
            case SkillConfigType.Movement:
                ExecuteMovement(skill, target, groundTarget);
                break;
            case SkillConfigType.Buff:
                ExecuteBuff(skill, target);
                break;
            case SkillConfigType.Heal:
                ExecuteHeal(skill, target);
                break;
            case SkillConfigType.Summon:
                ExecuteSummon(skill);
                break;
            case SkillConfigType.Transformation:
                ExecuteTransformation(skill);
                break;
        }
    }

    private void ExecuteProjectile(SkillConfig skill, Transform target, Vector3? groundTarget)
    {
        if (skill.projectilePrefab == null)
        {
            Log("No projectile prefab for " + skill.skillName);
            return;
        }

        // Multi-hit support (например, Rain of Arrows)
        if (skill.customData != null && skill.customData.hitCount > 1)
        {
            StartCoroutine(ExecuteMultipleProjectiles(skill, target, groundTarget));
            return;
        }

        // Single projectile
        LaunchProjectile(skill, target, groundTarget);
    }

    private IEnumerator ExecuteMultipleProjectiles(SkillConfig skill, Transform target, Vector3? groundTarget)
    {
        int hitCount = skill.customData.hitCount;
        float hitDelay = skill.customData.hitDelay;

        Log("Multi-hit: " + hitCount + " projectiles with " + hitDelay + "s delay");

        for (int i = 0; i < hitCount; i++)
        {
            LaunchProjectile(skill, target, groundTarget);

            if (i < hitCount - 1)
            {
                yield return new WaitForSeconds(hitDelay);
            }
        }
    }

    private void LaunchProjectile(SkillConfig skill, Transform target, Vector3? groundTarget)
    {
        Vector3 spawnPos = transform.position + transform.forward * 1f + Vector3.up * 1.5f;
        Vector3 direction;

        if (target != null)
        {
            direction = (target.position - spawnPos).normalized;
        }
        else if (groundTarget.HasValue)
        {
            direction = (groundTarget.Value - spawnPos).normalized;
        }
        else
        {
            direction = transform.forward;
        }

        GameObject projectile = Instantiate(skill.projectilePrefab, spawnPos, Quaternion.LookRotation(direction));

        // 🚀 SYNC: Send projectile to other players
        string targetSocketId = "";
        if (target != null)
        {
            NetworkPlayer networkTarget = target.GetComponent<NetworkPlayer>();
            if (networkTarget != null)
            {
                targetSocketId = networkTarget.socketId;
            }
        }

        SocketIOManager socketIO = SocketIOManager.Instance;
        if (socketIO != null && socketIO.IsConnected)
        {
            socketIO.SendProjectileSpawned(skill.skillId, spawnPos, direction, targetSocketId);
            Log($"🌐 Projectile synced to server: skillId={skill.skillId}");
        }

        // Try CelestialProjectile first (for mage)
        CelestialProjectile celestialProj = projectile.GetComponent<CelestialProjectile>();
        if (celestialProj != null)
        {
            float damage = CalculateDamage(skill);
            celestialProj.Initialize(target, damage, direction, gameObject, null, false, false);

            // Устанавливаем Life Steal если скилл имеет вампиризм
            if (skill.lifeStealPercent > 0)
            {
                celestialProj.SetLifeSteal(skill.lifeStealPercent, skill.casterEffectPrefab);
                Log($"🧛 Life Steal активирован: {skill.lifeStealPercent}%");
            }

            Log("Projectile launched: " + damage + " damage");
        }
        else
        {
            // Try ArrowProjectile (for archer)
            ArrowProjectile arrowProj = projectile.GetComponent<ArrowProjectile>();
            if (arrowProj != null)
            {
                float damage = CalculateDamage(skill);
                arrowProj.InitializeWithEffects(target, damage, direction, gameObject, skill.effects, stats, false, false);
                Log("Arrow launched: " + damage + " damage");
            }
            else
            {
                // Try old Projectile component (EntanglingArrow, etc)
                Projectile oldProj = projectile.GetComponent<Projectile>();
                if (oldProj != null)
                {
                    float damage = CalculateDamage(skill);
                    oldProj.Initialize(target, damage, direction, gameObject, null);
                    
                    // Set hit effect from skill
                    if (skill.hitEffectPrefab != null)
                    {
                        oldProj.SetHitEffect(skill.hitEffectPrefab);
                    }
                    
                    Log("Old Projectile launched: " + damage + " damage");

                    // Применяем эффекты вручную (старый Projectile не поддерживает EffectConfig)
                    if (skill.effects != null && skill.effects.Count > 0)
                    {
                        // Добавим MonoBehaviour для применения эффектов при попадании
                        ProjectileEffectApplier effectApplier = projectile.AddComponent<ProjectileEffectApplier>();
                        effectApplier.Initialize(skill.effects, stats);
                    }
                }
            }
        }

        // Эффект каста снаряда (на кастере)
        SpawnEffect(skill.castEffectPrefab, spawnPos, Quaternion.identity, 1f, "", "cast");
    }

    private void ExecuteAOEDamage(SkillConfig skill, Transform target, Vector3? groundTarget)
    {
        Vector3 aoeCenter;

        if (skill.targetType == SkillTargetType.Self || skill.targetType == SkillTargetType.NoTarget)
        {
            aoeCenter = transform.position;
        }
        else if (groundTarget.HasValue)
        {
            aoeCenter = groundTarget.Value;

            if (skill.skillName == "Meteor")
            {
                StartCoroutine(SpawnFallingMeteor(skill, aoeCenter));
                return;
            }
        }
        else if (target != null)
        {
            aoeCenter = target.position;
        }
        else
        {
            aoeCenter = transform.position + transform.forward * skill.aoeRadius;
        }

        Log("AOE center: " + aoeCenter + ", radius: " + skill.aoeRadius);

        Collider[] hits = Physics.OverlapSphere(aoeCenter, skill.aoeRadius, ~0);
        List<Transform> hitTargets = new List<Transform>();

        foreach (Collider hit in hits)
        {
            if (hit.transform == transform) continue;

            Enemy enemy = hit.GetComponent<Enemy>();
            DummyEnemy dummyEnemy = hit.GetComponent<DummyEnemy>();
            NetworkPlayer networkPlayer = hit.GetComponent<NetworkPlayer>();

            if (enemy != null || dummyEnemy != null || networkPlayer != null)
            {
                hitTargets.Add(hit.transform);

                if (hitTargets.Count >= skill.maxTargets && skill.maxTargets > 0)
                    break;
            }
        }

        Log("Found targets: " + hitTargets.Count);

        float damage = CalculateDamage(skill);

        foreach (Transform hitTarget in hitTargets)
        {
            Enemy enemy = hitTarget.GetComponent<Enemy>();
            DummyEnemy dummyEnemy = hitTarget.GetComponent<DummyEnemy>();
            NetworkPlayer networkPlayer = hitTarget.GetComponent<NetworkPlayer>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            else if (dummyEnemy != null)
            {
                dummyEnemy.TakeDamage(damage);
            }

            if (skill.effects != null && skill.effects.Count > 0)
            {
                EffectManager targetEffectManager = hitTarget.GetComponent<EffectManager>();
                if (targetEffectManager != null)
                {
                    foreach (EffectConfig effect in skill.effects)
                    {
                        targetEffectManager.ApplyEffect(effect, stats);
                    }
                }
            }

            // Эффект попадания на цели
            string targetSocketId = "";
            if (networkPlayer != null)
            {
                targetSocketId = networkPlayer.socketId;
            }
            SpawnEffect(skill.hitEffectPrefab, hitTarget.position, Quaternion.identity, 1f, targetSocketId, "hit");
        }

        // Эффект каста AOE в центре
        SpawnEffect(skill.castEffectPrefab, aoeCenter, Quaternion.identity, 2f, "", "aoe");

        if (skill.customData != null && skill.customData.chainCount > 0 && hitTargets.Count > 0)
        {
            List<Transform> alreadyHit = new List<Transform>(hitTargets);
            ExecuteChainLightning(skill, hitTargets[0], damage, alreadyHit, 0);
        }
    }

    private void ExecuteChainLightning(SkillConfig skill, Transform fromTarget, float baseDamage, List<Transform> alreadyHitTargets, int currentChain)
    {
        if (currentChain >= skill.customData.chainCount)
        {
            Log("Chain lightning completed: " + currentChain + " jumps");
            return;
        }

        float chainDamage = baseDamage * Mathf.Pow(skill.customData.chainDamageMultiplier, currentChain + 1);

        Log("Chain lightning jump " + (currentChain + 1) + ": " + chainDamage + " damage");

        Collider[] nearbyTargets = Physics.OverlapSphere(fromTarget.position, skill.customData.chainRadius, ~0);

        Transform nextTarget = null;
        float closestDistance = float.MaxValue;

        foreach (Collider hit in nearbyTargets)
        {
            if (hit.transform == transform) continue;
            if (alreadyHitTargets.Contains(hit.transform)) continue;

            Enemy enemy = hit.GetComponent<Enemy>();
            DummyEnemy dummyEnemy = hit.GetComponent<DummyEnemy>();
            NetworkPlayer networkPlayer = hit.GetComponent<NetworkPlayer>();

            if (enemy == null && dummyEnemy == null && networkPlayer == null) continue;

            float distance = Vector3.Distance(fromTarget.position, hit.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nextTarget = hit.transform;
            }
        }

        if (nextTarget == null)
        {
            Log("Chain lightning stopped: no targets in radius");
            return;
        }

        Enemy targetEnemy = nextTarget.GetComponent<Enemy>();
        DummyEnemy targetDummy = nextTarget.GetComponent<DummyEnemy>();

        if (targetEnemy != null)
        {
            targetEnemy.TakeDamage(chainDamage);
        }
        else if (targetDummy != null)
        {
            targetDummy.TakeDamage(chainDamage);
        }

        Log("Chain hit " + nextTarget.name + ": " + chainDamage + " damage");

        if (skill.effects != null && skill.effects.Count > 0)
        {
            EffectManager targetEffectManager = nextTarget.GetComponent<EffectManager>();
            if (targetEffectManager != null)
            {
                foreach (EffectConfig effect in skill.effects)
                {
                    targetEffectManager.ApplyEffect(effect, stats);
                }
            }
        }

        // Эффект chain lightning на следующей цели
        string chainTargetSocketId = "";
        NetworkPlayer chainNetPlayer = nextTarget.GetComponent<NetworkPlayer>();
        if (chainNetPlayer != null)
        {
            chainTargetSocketId = chainNetPlayer.socketId;
        }
        SpawnEffect(skill.hitEffectPrefab, nextTarget.position, Quaternion.identity, 1f, chainTargetSocketId, "chain_hit");

        alreadyHitTargets.Add(nextTarget);

        ExecuteChainLightning(skill, nextTarget, baseDamage, alreadyHitTargets, currentChain + 1);
    }

    private void ExecuteMovement(SkillConfig skill, Transform target, Vector3? groundTarget)
    {
        if (!skill.enableMovement)
        {
            Log("Movement not enabled for " + skill.skillName);
            return;
        }

        Vector3 destination = CalculateMovementDestination(skill, target, groundTarget);

        Log("Movement to " + destination);

        switch (skill.movementType)
        {
            case MovementType.Dash:
            case MovementType.Charge:
                StartCoroutine(DashToPosition(destination, skill.movementSpeed));
                break;

            case MovementType.Teleport:
            case MovementType.Blink:
                CharacterController cc = GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.enabled = false;
                    transform.position = destination;
                    cc.enabled = true;
                }
                else
                {
                    transform.position = destination;
                }

                Log("Teleported to " + destination);

                // Эффект телепорта в точке прибытия
                SpawnEffect(skill.hitEffectPrefab, destination, Quaternion.identity, 1f, "", "teleport_arrive");
                break;
        }

        // Эффект каста движения (в точке отправления)
        SpawnEffect(skill.castEffectPrefab, transform.position, Quaternion.identity, 1f, "", "movement_cast");

        // Применяем эффекты на цель после телепорта (например, Stun для Warrior Charge)
        if (target != null && skill.effects != null && skill.effects.Count > 0)
        {
            EffectManager targetEffectManager = target.GetComponent<EffectManager>();
            if (targetEffectManager != null)
            {
                foreach (EffectConfig effect in skill.effects)
                {
                    targetEffectManager.ApplyEffect(effect, stats);
                    Log("Effect applied to target: " + effect.effectType);
                }
            }
            else
            {
                Debug.LogWarning($"[SkillExecutor] Target {target.name} has no EffectManager!");
            }
        }
    }

    private IEnumerator DashToPosition(Vector3 destination, float speed)
    {
        Vector3 startPos = transform.position;
        float distance = Vector3.Distance(startPos, destination);
        float duration = distance / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, destination, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = destination;
    }

    private Vector3 CalculateMovementDestination(SkillConfig skill, Transform target, Vector3? groundTarget)
    {
        switch (skill.movementDirection)
        {
            case MovementDirection.Forward:
                return transform.position + transform.forward * skill.movementDistance;

            case MovementDirection.Backward:
                return transform.position - transform.forward * skill.movementDistance;

            case MovementDirection.ToTarget:
                if (target != null)
                {
                    // Рассчитываем дистанцию до цели
                    float distanceToTarget = Vector3.Distance(transform.position, target.position);

                    Log($"ToTarget: дистанция до цели = {distanceToTarget:F1}м, макс дистанция = {skill.movementDistance}м");

                    // Если цель ближе чем movementDistance - телепортируемся ПРЯМО К НЕЙ
                    // Если цель дальше - телепортируемся НА movementDistance К НЕЙ
                    if (distanceToTarget <= skill.movementDistance)
                    {
                        // Телепорт прямо к врагу (немного перед ним для визуала)
                        Vector3 dirToTarget = (target.position - transform.position).normalized;
                        Vector3 destination = target.position - dirToTarget * 1.5f; // На 1.5м перед врагом
                        Log($"⚡ Телепорт ПРЯМО К ВРАГУ! Позиция врага: {target.position}, точка телепорта: {destination}");
                        return destination;
                    }
                    else
                    {
                        // Цель далеко - телепортируемся на максимальную дистанцию к ней
                        Vector3 dirToTarget = (target.position - transform.position).normalized;
                        Vector3 destination = transform.position + dirToTarget * skill.movementDistance;
                        Log($"⚡ Цель далеко! Телепорт на {skill.movementDistance}м к врагу. Точка: {destination}");
                        return destination;
                    }
                }
                return transform.position + transform.forward * skill.movementDistance;

            case MovementDirection.MouseDirection:
                if (groundTarget.HasValue)
                {
                    return groundTarget.Value;
                }
                return transform.position + transform.forward * skill.movementDistance;

            default:
                return transform.position;
        }
    }

    private void ExecuteBuff(SkillConfig skill, Transform target)
    {
        // ════════════════════════════════════════════════════════════
        // СПЕЦИАЛЬНАЯ МЕХАНИКА: Blood for Mana (жертвенное заклинание)
        // ════════════════════════════════════════════════════════════
        if (skill.customData != null && skill.customData.manaRestorePercent > 0)
        {
            ExecuteBloodForMana(skill);
            return;
        }

        // ════════════════════════════════════════════════════════════
        // AOE BUFF (Divine Protection и другие групповые баффы)
        // ════════════════════════════════════════════════════════════
        if (skill.aoeRadius > 0 && (skill.targetType == SkillTargetType.NoTarget || skill.targetType == SkillTargetType.Self))
        {
            ExecuteAOEBuff(skill);
            return;
        }

        // ════════════════════════════════════════════════════════════
        // ОДИНОЧНЫЙ БАФФ (на одну цель)
        // ════════════════════════════════════════════════════════════
        Transform buffTarget = (skill.targetType == SkillTargetType.Self) ? transform : target;

        if (buffTarget == null)
        {
            Log("Buff target missing");
            return;
        }

        EffectManager targetEffectManager = buffTarget.GetComponent<EffectManager>();
        if (targetEffectManager != null && skill.effects != null)
        {
            foreach (EffectConfig effect in skill.effects)
            {
                targetEffectManager.ApplyEffect(effect, stats);
                Log("Buff applied: " + effect.effectType);
            }
        }

        // Эффект бафф каста на цели
        string buffTargetSocketId = "";
        NetworkPlayer buffNetPlayer = buffTarget.GetComponent<NetworkPlayer>();
        if (buffNetPlayer != null)
        {
            buffTargetSocketId = buffNetPlayer.socketId;
        }
        SpawnEffect(skill.castEffectPrefab, buffTarget.position, Quaternion.identity, 1f, buffTargetSocketId, "buff");
    }

    /// <summary>
    /// Применить AOE бафф на всех союзников в радиусе (Divine Protection, Group Heal и т.д.)
    /// </summary>
    private void ExecuteAOEBuff(SkillConfig skill)
    {
        Vector3 center = transform.position;
        Log($"AOE Buff center: {center}, radius: {skill.aoeRadius}");

        // Спавним эффект каста на кастере (AOE Buff начало)
        SpawnEffect(skill.castEffectPrefab, center, Quaternion.identity, 2f, "", "aoe_buff_cast");
        SpawnEffect(skill.casterEffectPrefab, center + Vector3.up * 1.5f, Quaternion.identity, 2f, "", "aoe_buff_aura");

        // Ищем всех в радиусе
        Collider[] hits = Physics.OverlapSphere(center, skill.aoeRadius, ~0);
        List<Transform> allies = new List<Transform>();

        foreach (Collider hit in hits)
        {
            if (hit.transform == transform)
            {
                // Сам кастер - всегда союзник
                allies.Add(transform);
                continue;
            }

            // Проверяем это союзник или враг
            // TODO: Добавить проверку команды когда будет система команд
            // Пока считаем союзниками всех игроков (NetworkPlayer, SimplePlayerController)

            bool isAlly = false;

            // Локальный игрок
            SimplePlayerController localPlayer = hit.GetComponent<SimplePlayerController>();
            if (localPlayer != null)
            {
                isAlly = true;
            }

            // Сетевые игроки
            NetworkPlayer networkPlayer = hit.GetComponent<NetworkPlayer>();
            if (networkPlayer != null)
            {
                // TODO: Проверить команду через networkPlayer.teamId
                isAlly = true;
            }

            // PlayerController (для арены)
            PlayerController playerController = hit.GetComponent<PlayerController>();
            if (playerController != null)
            {
                isAlly = true;
            }

            if (isAlly)
            {
                allies.Add(hit.transform);

                if (allies.Count >= skill.maxTargets && skill.maxTargets > 0)
                    break;
            }
        }

        Log($"Found {allies.Count} allies in radius");

        // Применяем баффы на всех союзников
        foreach (Transform ally in allies)
        {
            EffectManager effectManager = ally.GetComponent<EffectManager>();
            if (effectManager != null && skill.effects != null)
            {
                foreach (EffectConfig effect in skill.effects)
                {
                    effectManager.ApplyEffect(effect, stats);
                    Log($"✅ Buff {effect.effectType} applied to {ally.name}");
                }
            }

            // Спавним визуальный эффект на союзнике
            string allySocketId = "";
            NetworkPlayer allyNetPlayer = ally.GetComponent<NetworkPlayer>();
            if (allyNetPlayer != null)
            {
                allySocketId = allyNetPlayer.socketId;
            }
            SpawnEffect(skill.hitEffectPrefab, ally.position, Quaternion.identity, 1.5f, allySocketId, "buff_ally");
        }
    }

    private void ExecuteHeal(SkillConfig skill, Transform target)
    {
        // ════════════════════════════════════════════════════════════
        // AOE HEAL (Lay on Hands и другие групповые хилы)
        // ════════════════════════════════════════════════════════════
        if (skill.aoeRadius > 0 && (skill.targetType == SkillTargetType.NoTarget || skill.targetType == SkillTargetType.Self))
        {
            ExecuteAOEHeal(skill);
            return;
        }

        // ════════════════════════════════════════════════════════════
        // ОДИНОЧНЫЙ ХИЛ (на одну цель)
        // ════════════════════════════════════════════════════════════
        Transform healTarget = (skill.targetType == SkillTargetType.Self) ? transform : target;

        if (healTarget == null)
        {
            Log("Heal target missing");
            return;
        }

        float healAmount = CalculateHeal(skill);

        // Если healAmount отрицательное - это процент от максимального HP
        HealthSystem targetHealthSystem = healTarget.GetComponent<HealthSystem>();
        if (targetHealthSystem != null)
        {
            if (healAmount < 0)
            {
                // Отрицательное значение = процент от максимального HP
                float percentHeal = Mathf.Abs(healAmount);
                float actualHeal = targetHealthSystem.MaxHealth * (percentHeal / 100f);
                targetHealthSystem.Heal(actualHeal);
                Log($"⚕️ Лечение {healTarget.name}: {actualHeal:F1} HP ({percentHeal}% от {targetHealthSystem.MaxHealth:F0})");
            }
            else
            {
                // Положительное значение = фиксированное лечение
                targetHealthSystem.Heal(healAmount);
                Log($"⚕️ Лечение {healTarget.name}: {healAmount:F1} HP");
            }
        }

        // Визуальные эффекты лечения
        string healTargetSocketId = "";
        NetworkPlayer healNetPlayer = healTarget.GetComponent<NetworkPlayer>();
        if (healNetPlayer != null)
        {
            healTargetSocketId = healNetPlayer.socketId;
        }
        SpawnEffect(skill.castEffectPrefab, healTarget.position, Quaternion.identity, 1f, healTargetSocketId, "heal_cast");
        SpawnEffect(skill.hitEffectPrefab, healTarget.position, Quaternion.identity, 2f, healTargetSocketId, "heal_effect");
    }

    /// <summary>
    /// Применить AOE хил на всех союзников в радиусе (Lay on Hands, Group Heal и т.д.)
    /// </summary>
    private void ExecuteAOEHeal(SkillConfig skill)
    {
        Vector3 center = transform.position;
        Log($"AOE Heal center: {center}, radius: {skill.aoeRadius}");

        // Спавним эффект каста на кастере (AOE Heal начало)
        SpawnEffect(skill.castEffectPrefab, center, Quaternion.identity, 2f, "", "aoe_heal_cast");
        SpawnEffect(skill.casterEffectPrefab, center + Vector3.up * 1.5f, Quaternion.identity, 2f, "", "aoe_heal_aura");

        // Ищем всех в радиусе
        Collider[] hits = Physics.OverlapSphere(center, skill.aoeRadius, ~0);
        List<Transform> allies = new List<Transform>();

        foreach (Collider hit in hits)
        {
            if (hit.transform == transform)
            {
                // Сам кастер - всегда союзник
                allies.Add(transform);
                continue;
            }

            // Проверяем это союзник
            bool isAlly = false;

            // Локальный игрок
            SimplePlayerController localPlayer = hit.GetComponent<SimplePlayerController>();
            if (localPlayer != null)
            {
                isAlly = true;
            }

            // Сетевые игроки
            NetworkPlayer networkPlayer = hit.GetComponent<NetworkPlayer>();
            if (networkPlayer != null)
            {
                isAlly = true;
            }

            // PlayerController (для арены)
            PlayerController playerController = hit.GetComponent<PlayerController>();
            if (playerController != null)
            {
                isAlly = true;
            }

            if (isAlly)
            {
                allies.Add(hit.transform);

                if (allies.Count >= skill.maxTargets && skill.maxTargets > 0)
                    break;
            }
        }

        Log($"Found {allies.Count} allies to heal");

        float baseHealAmount = CalculateHeal(skill);

        // Лечим всех союзников
        foreach (Transform ally in allies)
        {
            HealthSystem healthSystem = ally.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                float actualHeal;

                if (baseHealAmount < 0)
                {
                    // Отрицательное значение = процент от MaxHP КАЖДОГО союзника
                    float percentHeal = Mathf.Abs(baseHealAmount);
                    actualHeal = healthSystem.MaxHealth * (percentHeal / 100f);
                    healthSystem.Heal(actualHeal);
                    Log($"⚕️ Лечение {ally.name}: {actualHeal:F1} HP ({percentHeal}% от {healthSystem.MaxHealth:F0})");
                }
                else
                {
                    // Положительное значение = фиксированное лечение
                    actualHeal = baseHealAmount;
                    healthSystem.Heal(actualHeal);
                    Log($"⚕️ Лечение {ally.name}: {actualHeal:F1} HP");
                }
            }

            // Спавним визуальный эффект на союзнике
            string healAllySocketId = "";
            NetworkPlayer healAllyNetPlayer = ally.GetComponent<NetworkPlayer>();
            if (healAllyNetPlayer != null)
            {
                healAllySocketId = healAllyNetPlayer.socketId;
            }
            SpawnEffect(skill.hitEffectPrefab, ally.position, Quaternion.identity, 1.5f, healAllySocketId, "heal_ally");
        }
    }

    /// <summary>
    /// Выполнить Blood for Mana (жертвенное заклинание)
    /// Жертвует 20% HP для восстановления 20% маны
    /// </summary>
    private void ExecuteBloodForMana(SkillConfig skill)
    {
        // Получаем HealthSystem и ManaSystem
        HealthSystem healthSystem = GetComponent<HealthSystem>();
        ManaSystem manaSystem = GetComponent<ManaSystem>();

        if (healthSystem == null)
        {
            Log("❌ HealthSystem не найден! Blood for Mana не может быть использован");
            return;
        }

        if (manaSystem == null)
        {
            Log("❌ ManaSystem не найден! Blood for Mana не может быть использован");
            return;
        }

        // Рассчитываем урон себе (процент от MaxHP)
        float hpSacrifice = 0f;
        if (skill.baseDamageOrHeal < 0)
        {
            // Отрицательное значение = процент от MaxHP
            float sacrificePercent = Mathf.Abs(skill.baseDamageOrHeal);
            hpSacrifice = healthSystem.MaxHealth * (sacrificePercent / 100f);
            Log($"🩸 Жертвуем {sacrificePercent}% HP ({hpSacrifice:F1} HP)");
        }

        // Проверяем что у нас достаточно HP (не можем убить себя)
        if (healthSystem.CurrentHealth <= hpSacrifice)
        {
            Log($"⚠️ Недостаточно HP! Нужно больше {hpSacrifice:F0} HP для жертвы");
            return;
        }

        // Рассчитываем восстановление маны
        float manaRestore = manaSystem.MaxMana * (skill.customData.manaRestorePercent / 100f);

        // Жертвуем HP
        healthSystem.TakeDamage(hpSacrifice);
        Log($"🩸 Жертва: -{hpSacrifice:F1} HP (осталось: {healthSystem.CurrentHealth:F0}/{healthSystem.MaxHealth:F0})");

        // Восстанавливаем ману
        manaSystem.RestoreMana(manaRestore);
        Log($"💙 Восстановлено: +{manaRestore:F1} MP ({skill.customData.manaRestorePercent}% от максимума)");

        // Визуальные эффекты Blood for Mana
        SpawnEffect(skill.castEffectPrefab, transform.position, Quaternion.identity, 1.5f, "", "blood_sacrifice"); // Кровавый эффект
        SpawnEffect(skill.casterEffectPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity, 2f, "", "mana_restore"); // Эффект восстановления маны

        Log($"✅ Blood for Mana: -{hpSacrifice:F0} HP → +{manaRestore:F0} MP");
    }

    private float CalculateDamage(SkillConfig skill)
    {
        float damage = skill.baseDamageOrHeal;

        if (stats != null)
        {
            if (skill.strengthScaling > 0f)
            {
                damage += stats.GetStat("Strength") * skill.strengthScaling;
            }

            if (skill.intelligenceScaling > 0f)
            {
                damage += stats.GetStat("Intelligence") * skill.intelligenceScaling;
            }

            // Применяем модификатор атаки (от Battle Rage и других баффов)
            if (stats.AttackModifier > 0)
            {
                float bonus = damage * (stats.AttackModifier / 100f);
                damage += bonus;
                Log($"⚔️ Attack modifier applied: +{stats.AttackModifier}% (+{bonus:F1} damage, total: {damage:F1})");
            }
        }

        Log("Damage calculated: " + damage + " (base: " + skill.baseDamageOrHeal + ")");
        return damage;
    }

    private float CalculateHeal(SkillConfig skill)
    {
        float heal = skill.baseDamageOrHeal;

        if (stats != null)
        {

            if (skill.intelligenceScaling > 0f)
            {
                heal += stats.GetStat("Intelligence") * skill.intelligenceScaling;
            }
        }

        return heal;
    }

    private void SpawnEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation, float lifetime = 1f, string targetSocketId = "", string effectType = "effect")
    {
        if (effectPrefab == null) return;

        // Создаём эффект локально
        GameObject effect = Instantiate(effectPrefab, position, rotation);
        Destroy(effect, lifetime);

        // 🌐 СИНХРОНИЗАЦИЯ: Отправляем визуальный эффект на сервер для других игроков
        if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
        {
            string prefabName = effectPrefab.name.Replace("(Clone)", "").Trim();
            SocketIOManager.Instance.SendVisualEffect(
                effectType,         // "cast", "hit", "buff", "explosion" и т.д.
                prefabName,         // Имя префаба для поиска в Resources
                position,           // Позиция эффекта
                rotation,           // Поворот эффекта
                targetSocketId,     // Если привязан к игроку (пустая строка = world space)
                lifetime            // Длительность эффекта
            );
            Log($"🌐 Визуальный эффект отправлен на сервер: {prefabName} at {position}");
        }
    }

    private IEnumerator SpawnFallingMeteor(SkillConfig skill, Vector3 targetPosition)
    {
        Vector3 skyPosition = targetPosition + Vector3.up * 30f;

        Log("Spawning meteor in sky: " + skyPosition);

        GameObject meteor = Instantiate(skill.projectilePrefab, skyPosition, Quaternion.identity);

        if (meteor == null)
        {
            Log("Failed to create meteor! Prefab: " + skill.projectilePrefab?.name);
            yield break;
        }

        Log("Meteor created: " + meteor.name + ", position: " + meteor.transform.position + ", scale: " + meteor.transform.localScale);

        meteor.transform.localScale = meteor.transform.localScale * 5f;

        Log("Scale increased to: " + meteor.transform.localScale);

        CelestialProjectile projectileScript = meteor.GetComponent<CelestialProjectile>();
        if (projectileScript != null)
        {
            projectileScript.enabled = false;
            Log("CelestialProjectile disabled");
        }

        TrailRenderer trail = meteor.GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.time = 0.3f;
            trail.startWidth = 10f;
            trail.endWidth = 2f;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.3f, 0f), 0.0f),
                    new GradientColorKey(new Color(1f, 0f, 0f), 0.5f),
                    new GradientColorKey(new Color(0.5f, 0f, 0f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.5f, 1.0f)
                }
            );
            trail.colorGradient = gradient;

            Log("Trail configured: time=" + trail.time + ", width=" + trail.startWidth + ", red color");
        }
        else
        {
            Log("TrailRenderer not found on meteor!");
        }

        GameObject fireEffect = Resources.Load<GameObject>("Effects/CFXR Fire");
        if (fireEffect != null)
        {
            GameObject fire = Instantiate(fireEffect, meteor.transform);
            fire.transform.localPosition = Vector3.zero;
            fire.transform.localScale = Vector3.one * 2f;
            Log("Fire effect added to meteor");
        }

        Light light = meteor.GetComponent<Light>();
        if (light != null)
        {
            light.color = new Color(1f, 0.3f, 0f);
            light.intensity = 5f;
            light.range = 30f;
        }

        float fallDuration = 1.2f;
        float elapsed = 0f;

        Log("Meteor falling from " + skyPosition + " to " + targetPosition);

        while (elapsed < fallDuration)
        {
            meteor.transform.position = Vector3.Lerp(skyPosition, targetPosition, elapsed / fallDuration);
            meteor.transform.Rotate(Vector3.forward * 360f * Time.deltaTime * 2f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        meteor.transform.position = targetPosition;

        Log("Meteor reached ground: " + targetPosition);

        Collider[] hits = Physics.OverlapSphere(targetPosition, skill.aoeRadius, ~0);
        List<Transform> hitTargets = new List<Transform>();

        foreach (Collider hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            DummyEnemy dummyEnemy = hit.GetComponent<DummyEnemy>();
            NetworkPlayer networkPlayer = hit.GetComponent<NetworkPlayer>();

            if (enemy != null || dummyEnemy != null || networkPlayer != null)
            {
                hitTargets.Add(hit.transform);

                if (hitTargets.Count >= skill.maxTargets && skill.maxTargets > 0)
                    break;
            }
        }

        float damage = CalculateDamage(skill);

        foreach (Transform hitTarget in hitTargets)
        {
            Enemy enemy = hitTarget.GetComponent<Enemy>();
            DummyEnemy dummyEnemy = hitTarget.GetComponent<DummyEnemy>();
            NetworkPlayer networkPlayer = hitTarget.GetComponent<NetworkPlayer>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            else if (dummyEnemy != null)
            {
                dummyEnemy.TakeDamage(damage);
            }

            if (skill.effects != null && skill.effects.Count > 0)
            {
                EffectManager targetEffectManager = hitTarget.GetComponent<EffectManager>();
                if (targetEffectManager != null)
                {
                    foreach (EffectConfig effect in skill.effects)
                    {
                        targetEffectManager.ApplyEffect(effect, stats);
                        Log("Burn applied to " + hitTarget.name);
                    }
                }
            }

            // Эффект попадания метеора
            string meteorTargetSocketId = "";
            NetworkPlayer meteorNetPlayer = hitTarget.GetComponent<NetworkPlayer>();
            if (meteorNetPlayer != null)
            {
                meteorTargetSocketId = meteorNetPlayer.socketId;
            }
            SpawnEffect(skill.hitEffectPrefab, hitTarget.position, Quaternion.identity, 1f, meteorTargetSocketId, "meteor_hit");
        }

        // Эффект взрыва метеора в точке падения
        SpawnEffect(skill.castEffectPrefab, targetPosition, Quaternion.identity, 3f, "", "meteor_explosion");

        Destroy(meteor, 0.2f);

        Log("Meteor fell on " + targetPosition + ", damage " + damage + " to " + hitTargets.Count + " targets");
    }

    /// <summary>
    /// Призыв миньона (Raise Dead)
    /// </summary>
    private void ExecuteSummon(SkillConfig skill)
    {
        Log($"Using skill: {skill.skillName}");

        // Спавним визуальные эффекты призыва миньона
        SpawnEffect(skill.castEffectPrefab, transform.position, Quaternion.identity, 2f, "", "summon_cast");
        SpawnEffect(skill.casterEffectPrefab, transform.position, Quaternion.identity, 2f, "", "summon_aura");

        // Получаем duration из эффекта
        float summonDuration = 20f; // default
        if (skill.effects != null && skill.effects.Count > 0)
        {
            foreach (var effect in skill.effects)
            {
                if (effect.effectType == EffectType.SummonMinion)
                {
                    summonDuration = effect.duration;
                    break;
                }
            }
        }

        // ════════════════════════════════════════════════════════════
        // СИСТЕМА ПРИЗЫВА МИНЬОНОВ
        // ════════════════════════════════════════════════════════════

        // 1. Проверка - есть ли уже активный миньон
        if (activeMinion != null)
        {
            Log($"⚠️ Старый миньон уничтожается");
            Destroy(activeMinion);
            activeMinion = null;
        }

        // 2. Загружаем префаб скелета
        GameObject skeletonPrefab = Resources.Load<GameObject>("Minions/Skeleton");
        if (skeletonPrefab == null)
        {
            Log($"❌ Skeleton prefab не найден в Resources/Minions/Skeleton!");
            return;
        }

        // 3. Позиция спавна (перед некромантом)
        Vector3 spawnPosition = transform.position + transform.forward * 2f;
        spawnPosition.y = transform.position.y; // Одинаковая высота

        // 4. Создаём скелета
        activeMinion = Instantiate(skeletonPrefab, spawnPosition, transform.rotation);
        activeMinion.name = "Skeleton (Summoned)";

        // 4.5. Проверяем и копируем компоненты с префаба
        SetupSkeletonComponents(activeMinion, skeletonPrefab);

        // 5. Настраиваем AI
        SkeletonAI skeletonAI = activeMinion.GetComponent<SkeletonAI>();
        if (skeletonAI == null)
        {
            skeletonAI = activeMinion.AddComponent<SkeletonAI>();
        }

        CharacterStats casterStats = GetComponent<CharacterStats>();
        skeletonAI.Initialize(
            gameObject,                     // owner (некромант)
            casterStats,                    // stats некроманта
            skill.baseDamageOrHeal,         // базовый урон (30)
            skill.intelligenceScaling,      // скейлинг INT (0.5)
            summonDuration                  // длительность (20 сек)
        );

        Log($"💀 Raise Dead: миньон призван на {summonDuration} секунд");
        Log($"⚔️ Урон миньона: {skill.baseDamageOrHeal} + {skill.intelligenceScaling * 100}% INT");
        Log($"📍 Позиция спавна: {spawnPosition}");

        // Применяем эффект SummonMinion через EffectManager (для логирования)
        if (skill.effects != null && skill.effects.Count > 0)
        {
            EffectManager effectManager = GetComponent<EffectManager>();
            if (effectManager == null)
            {
                effectManager = gameObject.AddComponent<EffectManager>();
            }

            CharacterStats stats = GetComponent<CharacterStats>();
            foreach (var effect in skill.effects)
            {
                if (effect.effectType == EffectType.SummonMinion)
                {
                    effectManager.ApplyEffect(effect, stats);
                }
            }
        }
    }

    /// <summary>
    /// Настройка компонентов скелета (Animator, Collider, etc)
    /// </summary>
    private void SetupSkeletonComponents(GameObject skeleton, GameObject prefab)
    {
        // ════════════════════════════════════════════════════════════
        // ANIMATOR
        // ════════════════════════════════════════════════════════════

        Animator skeletonAnimator = skeleton.GetComponent<Animator>();
        Animator prefabAnimator = prefab.GetComponent<Animator>();

        if (skeletonAnimator == null && prefabAnimator != null)
        {
            // Копируем Animator с префаба
            skeletonAnimator = skeleton.AddComponent<Animator>();
            skeletonAnimator.runtimeAnimatorController = prefabAnimator.runtimeAnimatorController;
            skeletonAnimator.avatar = prefabAnimator.avatar;
            skeletonAnimator.applyRootMotion = prefabAnimator.applyRootMotion;
            Log($"✅ Animator скопирован с префаба");
        }
        else if (skeletonAnimator != null && skeletonAnimator.runtimeAnimatorController == null)
        {
            // Animator есть, но Controller не назначен - копируем с префаба
            if (prefabAnimator != null && prefabAnimator.runtimeAnimatorController != null)
            {
                skeletonAnimator.runtimeAnimatorController = prefabAnimator.runtimeAnimatorController;
                skeletonAnimator.avatar = prefabAnimator.avatar;
                skeletonAnimator.applyRootMotion = prefabAnimator.applyRootMotion;
                Log($"✅ Animator Controller назначен: {skeletonAnimator.runtimeAnimatorController.name}");
            }
            else
            {
                // Если на префабе тоже нет - пытаемся загрузить RogueAnimator
                RuntimeAnimatorController rogueController = Resources.Load<RuntimeAnimatorController>("Animations/Controllers/RogueAnimator");
                if (rogueController == null)
                {
                    // Пробуем без папки Animations
                    rogueController = Resources.Load<RuntimeAnimatorController>("RogueAnimator");
                }

                if (rogueController != null)
                {
                    skeletonAnimator.runtimeAnimatorController = rogueController;
                    Log($"✅ Загружен RogueAnimator из Resources");
                }
                else
                {
                    Log($"⚠️ RogueAnimator не найден в Resources");
                }
            }
        }

        // ════════════════════════════════════════════════════════════
        // CAPSULE COLLIDER
        // ════════════════════════════════════════════════════════════

        CapsuleCollider skeletonCollider = skeleton.GetComponent<CapsuleCollider>();
        if (skeletonCollider == null)
        {
            // Проверяем есть ли на префабе
            CapsuleCollider prefabCollider = prefab.GetComponent<CapsuleCollider>();
            if (prefabCollider != null)
            {
                // Копируем настройки с префаба
                skeletonCollider = skeleton.AddComponent<CapsuleCollider>();
                skeletonCollider.center = prefabCollider.center;
                skeletonCollider.radius = prefabCollider.radius;
                skeletonCollider.height = prefabCollider.height;
                skeletonCollider.direction = prefabCollider.direction;
                Log($"✅ CapsuleCollider скопирован с префаба");
            }
            else
            {
                // Создаём дефолтный Capsule Collider для humanoid
                skeletonCollider = skeleton.AddComponent<CapsuleCollider>();
                skeletonCollider.center = new Vector3(0, 1f, 0);
                skeletonCollider.radius = 0.3f;
                skeletonCollider.height = 2f;
                skeletonCollider.direction = 1; // Y-axis
                Log($"✅ CapsuleCollider создан (default humanoid)");
            }
        }

        // ════════════════════════════════════════════════════════════
        // RIGIDBODY
        // ════════════════════════════════════════════════════════════

        Rigidbody skeletonRb = skeleton.GetComponent<Rigidbody>();
        if (skeletonRb == null)
        {
            Rigidbody prefabRb = prefab.GetComponent<Rigidbody>();
            if (prefabRb != null)
            {
                // Копируем Rigidbody с префаба
                skeletonRb = skeleton.AddComponent<Rigidbody>();
                skeletonRb.mass = prefabRb.mass;
                skeletonRb.linearDamping = prefabRb.linearDamping;
                skeletonRb.angularDamping = prefabRb.angularDamping;
                skeletonRb.useGravity = prefabRb.useGravity;
                skeletonRb.isKinematic = prefabRb.isKinematic;
                skeletonRb.interpolation = prefabRb.interpolation;
                skeletonRb.collisionDetectionMode = prefabRb.collisionDetectionMode;
                skeletonRb.constraints = prefabRb.constraints;
                Log($"✅ Rigidbody скопирован с префаба");
            }
            else
            {
                // Создаём дефолтный Rigidbody для скелета
                skeletonRb = skeleton.AddComponent<Rigidbody>();
                skeletonRb.mass = 50f;
                skeletonRb.useGravity = true;
                skeletonRb.isKinematic = false;
                skeletonRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                Log($"✅ Rigidbody создан (default skeleton)");
            }
        }

        Log($"🎭 Skeleton компоненты настроены");
    }

    /// <summary>
    /// Трансформация (Bear Form для Paladin/Druid)
    /// </summary>
    private void ExecuteTransformation(SkillConfig skill)
    {
        Log($"Using transformation skill: {skill.skillName}");

        // Спавним визуальные эффекты трансформации (Bear Form и т.д.)
        SpawnEffect(skill.castEffectPrefab, transform.position, Quaternion.identity, 2f, "", "transformation_cast");
        SpawnEffect(skill.casterEffectPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity, 2f, "", "transformation_aura");

        // Проверяем что prefab трансформации существует
        if (skill.transformationModel == null)
        {
            Log($"❌ Transformation model отсутствует для {skill.skillName}!");
            return;
        }

        // Получаем компонент SimpleTransformation
        SimpleTransformation transformation = GetComponent<SimpleTransformation>();
        if (transformation == null)
        {
            transformation = gameObject.AddComponent<SimpleTransformation>();
            Log($"✅ SimpleTransformation компонент добавлен");
        }

        // Получаем аниматор паладина для передачи в TransformTo
        Animator paladinAnimator = GetComponent<Animator>();
        if (paladinAnimator == null)
        {
            paladinAnimator = GetComponentInChildren<Animator>();
        }

        // Выполняем трансформацию
        bool success = transformation.TransformTo(skill.transformationModel, paladinAnimator);

        if (!success)
        {
            Log($"❌ Трансформация не удалась!");
            return;
        }

        Log($"✅ Трансформация выполнена: {skill.skillName}");
        Log($"🐻 Модель трансформации: {skill.transformationModel.name}");

        // Применяем статус-эффекты трансформации (если есть)
        if (skill.effects != null && skill.effects.Count > 0)
        {
            EffectManager targetEffectManager = GetComponent<EffectManager>();
            if (targetEffectManager != null)
            {
                foreach (EffectConfig effect in skill.effects)
                {
                    targetEffectManager.ApplyEffect(effect, stats);
                    Log($"Effect applied: {effect.effectType}");
                }
            }
        }

        // Применяем бонусы трансформации (HP, урон)
        if (skill.hpBonusPercent > 0)
        {
            HealthSystem healthSystem = GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                float hpBonus = healthSystem.MaxHealth * (skill.hpBonusPercent / 100f);
                // TODO: Добавить временный бонус к MaxHP
                Log($"💚 HP бонус: +{skill.hpBonusPercent}% (+{hpBonus:F0} HP)");
            }
        }

        if (skill.damageBonusPercent > 0)
        {
            // TODO: Добавить временный бонус к урону через CharacterStats
            Log($"⚔️ Урон бонус: +{skill.damageBonusPercent}%");
        }

        // Запускаем таймер возврата трансформации
        if (skill.transformationDuration > 0)
        {
            StartCoroutine(RevertTransformationAfterDelay(transformation, skill.transformationDuration));
            Log($"⏱️ Длительность трансформации: {skill.transformationDuration} секунд");
        }
    }

    /// <summary>
    /// Возврат из трансформации после задержки
    /// </summary>
    private IEnumerator RevertTransformationAfterDelay(SimpleTransformation transformation, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (transformation != null && transformation.IsTransformed())
        {
            transformation.RevertToOriginal();
            Log($"⏱️ Трансформация завершилась (время истекло)");

            // Спавним эффект возврата из трансформации
            GameObject revertEffect = Resources.Load<GameObject>("Effects/CFXR Magic Poof");
            if (revertEffect != null)
            {
                SpawnEffect(revertEffect, transform.position, Quaternion.identity, 1.5f, "", "transformation_revert");
            }
        }
    }

    private void Log(string message)
    {
        if (enableLogs)
        {
            Debug.Log("[SkillExecutor] " + message);
        }
    }

    public SkillConfig GetEquippedSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equippedSkills.Count)
        {
            return null;
        }
        return equippedSkills[slotIndex];
    }

    /// <summary>
    /// Проверить активен ли эффект Root/Stun (блокирует движение)
    /// </summary>
    public bool IsRooted()
    {
        if (effectManager == null)
        {
            effectManager = GetComponent<EffectManager>();
            if (effectManager == null) return false;
        }

        return effectManager.IsRooted();
    }

    /// <summary>
    /// Применить эффект на цель (для projectile скриптов)
    /// </summary>
    public void ApplyEffectToTarget(EffectConfig effect, Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning("[SkillExecutor] ⚠️ Цель для применения эффекта не найдена!");
            return;
        }

        EffectManager targetEffectManager = target.GetComponent<EffectManager>();
        if (targetEffectManager == null)
        {
            Debug.LogWarning($"[SkillExecutor] ⚠️ У цели {target.name} нет EffectManager!");
            return;
        }

        targetEffectManager.AddEffect(effect, transform);
        Log($"✨ Эффект {effect.effectType} применён на {target.name}");
    }
}
