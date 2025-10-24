using UnityEngine;

/// <summary>
/// НОВАЯ система атаки с BasicAttackConfig
/// Заменяет старый PlayerAttack.cs
/// </summary>
public class PlayerAttackNew : MonoBehaviour
{
    [Header("⚔️ BASIC ATTACK CONFIG")]
    [Tooltip("Конфигурация базовой атаки (ScriptableObject)")]
    public BasicAttackConfig attackConfig;

    [Header("✨ SKILLS SYSTEM (NEW)")]
    [Tooltip("Скиллы загружаются автоматически или назначаются вручную")]
    private SkillExecutor skillExecutor;
    private EffectManager effectManager;

    [Header("Компоненты (автоматически найдутся)")]
    private Animator animator;
    private CharacterController characterController;
    private TargetSystem targetSystem;
    private CharacterStats characterStats;
    private ManaSystem manaSystem;
    private NetworkCombatSync combatSync;

    [Header("Состояние атаки")]
    private float lastAttackTime = 0f;
    private bool isAttacking = false;
    private Enemy currentTarget = null;

    void Start()
    {
        // Находим компоненты
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        targetSystem = GetComponent<TargetSystem>();
        characterStats = GetComponent<CharacterStats>();
        manaSystem = GetComponent<ManaSystem>();
        combatSync = GetComponent<NetworkCombatSync>();

        // НОВОЕ: Инициализация системы скиллов
        InitializeSkillSystem();

        // Проверяем конфиг
        if (attackConfig == null)
        {
            Debug.LogError($"[PlayerAttackNew] ❌ BasicAttackConfig НЕ НАЗНАЧЕН для {gameObject.name}!");
            Debug.LogError("[PlayerAttackNew] Назначьте BasicAttackConfig в Inspector!");
            enabled = false;
            return;
        }

        // Валидация конфига
        string validationError;
        if (!attackConfig.Validate(out validationError))
        {
            Debug.LogError($"[PlayerAttackNew] ❌ Ошибка валидации конфига: {validationError}");
        }

        Debug.Log($"[PlayerAttackNew] ✅ Инициализация завершена для {gameObject.name}");
        Debug.Log($"[PlayerAttackNew] Config: {attackConfig.name}, Damage: {attackConfig.baseDamage}, Type: {attackConfig.attackType}");
    }

    /// <summary>
    /// Инициализация системы скиллов (НОВОЕ)
    /// </summary>
    void InitializeSkillSystem()
    {
        // Ищем или добавляем SkillExecutor
        skillExecutor = GetComponent<SkillExecutor>();
        if (skillExecutor == null)
        {
            skillExecutor = gameObject.AddComponent<SkillExecutor>();
            Debug.Log("[PlayerAttackNew] ✅ Добавлен SkillExecutor");
        }

        // Ищем или добавляем EffectManager
        effectManager = GetComponent<EffectManager>();
        if (effectManager == null)
        {
            effectManager = gameObject.AddComponent<EffectManager>();
            Debug.Log("[PlayerAttackNew] ✅ Добавлен EffectManager");
        }

        Debug.Log("[PlayerAttackNew] ✨ Система скиллов инициализирована");
    }

    void Update()
    {
        // ═══════════════════════════════════════════════════════
        // ПРОВЕРКА: Контроль эффектов (Stun, Sleep, Fear)
        // ═══════════════════════════════════════════════════════

        if (effectManager != null && effectManager.IsUnderCrowdControl())
        {
            // Под контролем - не можем действовать
            return;
        }

        // ═══════════════════════════════════════════════════════
        // ЛКМ - BASIC ATTACK (УЖЕ РАБОТАЕТ)
        // ═══════════════════════════════════════════════════════

        if (Input.GetMouseButtonDown(0))
        {
            // Проверка может ли атаковать
            if (effectManager == null || effectManager.CanAttack())
            {
                TryAttack();
            }
            else
            {
                Debug.Log("[PlayerAttackNew] ❌ Не могу атаковать - под эффектом контроля!");
            }
        }

        // ═══════════════════════════════════════════════════════
        // КЛАВИШИ 1/2/3/4/5 - SKILLS (НОВОЕ)
        // ═══════════════════════════════════════════════════════

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            TryUseSkill(0); // Слот 1
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            TryUseSkill(1); // Слот 2
        }

        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            TryUseSkill(2); // Слот 3
        }

        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            TryUseSkill(3); // Слот 4
        }

        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
        {
            TryUseSkill(4); // Слот 5
        }

        // Обновляем состояние атаки
        if (isAttacking)
        {
            float timeSinceAttack = Time.time - lastAttackTime;
            if (timeSinceAttack >= 1.0f) // Фиксированная длительность атаки
            {
                isAttacking = false;
                if (characterController != null)
                {
                    characterController.enabled = true;
                }
            }
        }
    }

    /// <summary>
    /// Попытка использовать скилл (НОВОЕ)
    /// </summary>
    void TryUseSkill(int slotIndex)
    {
        if (skillExecutor == null)
        {
            Debug.LogWarning("[PlayerAttackNew] ⚠️ SkillExecutor не найден!");
            return;
        }

        // Проверка может ли использовать скиллы
        if (effectManager != null && !effectManager.CanUseSkills())
        {
            Debug.Log("[PlayerAttackNew] ❌ Не могу использовать скиллы - под эффектом контроля!");
            return;
        }

        // Получаем цель
        Transform targetTransform = null;
        Enemy enemy = GetEnemyTarget();
        DummyEnemy dummy = GetDummyTarget();

        if (enemy != null)
        {
            targetTransform = enemy.transform;
        }
        else if (dummy != null)
        {
            targetTransform = dummy.transform;
        }

        // Используем скилл (цель может быть NULL для self-cast скиллов)
        bool success = skillExecutor.UseSkill(slotIndex, targetTransform);

        if (success)
        {
            Debug.Log($"[PlayerAttackNew] ⚡ Использован скилл в слоте {slotIndex + 1}");
        }
    }

    /// <summary>
    /// Попытка атаковать
    /// </summary>
    void TryAttack()
    {
        if (attackConfig == null)
        {
            Debug.LogError("[PlayerAttackNew] Config не назначен!");
            return;
        }

        // Проверяем кулдаун
        if (Time.time - lastAttackTime < attackConfig.attackCooldown)
        {
            Debug.Log("[PlayerAttackNew] Кулдаун атаки");
            return;
        }

        // Проверяем что не атакуем сейчас
        if (isAttacking)
        {
            Debug.Log("[PlayerAttackNew] Уже атакуем");
            return;
        }

        // Получаем цель (Enemy или DummyEnemy)
        Transform targetTransform = null;
        Enemy enemy = GetEnemyTarget();
        DummyEnemy dummy = GetDummyTarget();

        if (enemy != null)
        {
            targetTransform = enemy.transform;
        }
        else if (dummy != null)
        {
            targetTransform = dummy.transform;
        }

        if (targetTransform == null)
        {
            Debug.Log("[PlayerAttackNew] Нет цели для атаки");
            return;
        }

        // Проверяем дистанцию
        float distance = Vector3.Distance(transform.position, targetTransform.position);
        if (distance > attackConfig.attackRange)
        {
            Debug.Log($"[PlayerAttackNew] Цель слишком далеко: {distance:F1}m > {attackConfig.attackRange}m");
            return;
        }

        // Сохраняем для использования
        currentTarget = enemy;

        // Проверяем ману (для магических атак)
        if (attackConfig.attackType == AttackType.Ranged && manaSystem != null)
        {
            bool isMagicalAttack = (characterStats != null &&
                (characterStats.ClassName == "Mage" || characterStats.ClassName == "Rogue"));

            if (isMagicalAttack && manaSystem.CurrentMana < attackConfig.manaCostPerAttack)
            {
                Debug.Log($"[PlayerAttackNew] Недостаточно маны: {manaSystem.CurrentMana:F0}/{attackConfig.manaCostPerAttack}");
                return;
            }

            if (isMagicalAttack)
            {
                manaSystem.SpendMana(attackConfig.manaCostPerAttack);
            }
        }

        // Выполняем атаку
        PerformAttack(targetTransform);
    }

    /// <summary>
    /// Получить Enemy цель
    /// </summary>
    Enemy GetEnemyTarget()
    {
        // Пытаемся получить из TargetSystem
        if (targetSystem != null && targetSystem.HasTarget())
        {
            Enemy target = targetSystem.GetCurrentTarget();
            if (target != null && target.IsAlive())
            {
                return target;
            }
        }

        return null;
    }

    /// <summary>
    /// Получить DummyEnemy цель
    /// </summary>
    DummyEnemy GetDummyTarget()
    {
        // Ищем ближайшего живого DummyEnemy
        DummyEnemy[] dummies = FindObjectsOfType<DummyEnemy>();

        Debug.Log($"[PlayerAttackNew] Найдено DummyEnemy: {dummies.Length}");

        DummyEnemy closest = null;
        float closestDist = float.MaxValue;

        foreach (DummyEnemy dummy in dummies)
        {
            if (!dummy.IsAlive())
            {
                Debug.Log($"[PlayerAttackNew] {dummy.name} мёртв - пропускаем");
                continue;
            }

            float dist = Vector3.Distance(transform.position, dummy.transform.position);
            Debug.Log($"[PlayerAttackNew] {dummy.name} дистанция: {dist:F1}m, Range: {attackConfig.attackRange}m");

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = dummy;
            }
        }

        if (closest != null)
        {
            Debug.Log($"[PlayerAttackNew] ✅ Выбран ближайший: {closest.name} на {closestDist:F1}m");
        }
        else
        {
            Debug.Log($"[PlayerAttackNew] ❌ Не найдено подходящих DummyEnemy");
        }

        return closest;
    }

    /// <summary>
    /// Выполнить атаку
    /// </summary>
    void PerformAttack(Transform targetTransform)
    {
        Debug.Log($"[PlayerAttackNew] ⚔️ Атака!");

        // Поворачиваем к цели
        if (targetTransform != null)
        {
            Vector3 direction = (targetTransform.position - transform.position).normalized;
            direction.y = 0;
            if (direction.magnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        // Запускаем анимацию
        if (animator != null && !string.IsNullOrEmpty(attackConfig.animationTrigger))
        {
            animator.SetTrigger(attackConfig.animationTrigger);
            animator.speed = attackConfig.animationSpeed;
        }

        // Блокируем движение
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        // Сохраняем состояние
        lastAttackTime = Time.time;
        isAttacking = true;

        // Наносим урон через задержку (имитация анимации)
        Invoke(nameof(DealDamage), 0.3f);
    }

    /// <summary>
    /// Нанести урон (вызывается через Invoke)
    /// </summary>
    void DealDamage()
    {
        // Рассчитываем урон
        float damage = attackConfig.baseDamage;
        if (characterStats != null)
        {
            damage = attackConfig.CalculateDamage(characterStats);
        }

        // Проверяем критический удар
        bool isCritical = false;
        float baseDamageBeforeCrit = damage; // Сохраняем базовый урон для расчёта
        if (Random.Range(0f, 100f) < attackConfig.baseCritChance)
        {
            isCritical = true;

            // Используем CharacterStats.ApplyCriticalDamage() для учёта модификаторов (Deadly Precision)
            if (characterStats != null)
            {
                damage = characterStats.ApplyCriticalDamage(baseDamageBeforeCrit);
                Debug.Log($"[PlayerAttackNew] 💥💥 КРИТИЧЕСКИЙ УРОН через CharacterStats! {damage:F1}");
            }
            else
            {
                // Fallback если нет CharacterStats
                damage *= attackConfig.critMultiplier;
                Debug.Log($"[PlayerAttackNew] 💥💥 КРИТИЧЕСКИЙ УРОН! {damage:F1} (×{attackConfig.critMultiplier})");
            }
        }
        else
        {
            Debug.Log($"[PlayerAttackNew] 💥 Урон рассчитан: {damage:F1}");
        }

        // Дальняя атака - создаём снаряд
        if (attackConfig.attackType == AttackType.Ranged && attackConfig.projectilePrefab != null)
        {
            SpawnProjectile(damage, isCritical);
        }
        // Ближняя атака - наносим урон сразу
        else
        {
            ApplyDamage(damage, isCritical);
        }

        // Отправляем на сервер (если есть мультиплеер)
        Enemy enemy = GetEnemyTarget();
        if (combatSync != null && enemy != null)
        {
            string attackType = attackConfig.attackType == AttackType.Ranged ? "ranged" : "melee";
            combatSync.SendAttack(enemy.gameObject, damage, attackType);
        }
    }

    /// <summary>
    /// Применить урон к текущей цели
    /// </summary>
    void ApplyDamage(float damage, bool isCritical = false)
    {
        Transform targetTransform = null;

        // Сначала проверяем Enemy
        Enemy enemy = GetEnemyTarget();
        if (enemy != null && enemy.IsAlive())
        {
            enemy.TakeDamage(damage);
            targetTransform = enemy.transform;
            Debug.Log($"[PlayerAttackNew] ⚔️ Урон {damage:F1} нанесён Enemy: {enemy.GetEnemyName()}");
        }
        else
        {
            // Проверяем DummyEnemy
            DummyEnemy[] dummies = FindObjectsOfType<DummyEnemy>();
            if (dummies.Length > 0)
            {
                DummyEnemy closest = dummies[0];
                float closestDist = Vector3.Distance(transform.position, closest.transform.position);

                foreach (DummyEnemy dummy in dummies)
                {
                    float dist = Vector3.Distance(transform.position, dummy.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = dummy;
                    }
                }

                if (closestDist <= attackConfig.attackRange && closest.IsAlive())
                {
                    closest.TakeDamage(damage);
                    targetTransform = closest.transform;
                    Debug.Log($"[PlayerAttackNew] ⚔️ Урон {damage:F1} нанесён DummyEnemy");
                }
            }
        }

        // Создаём визуальные эффекты попадания (для ближнего боя)
        if (targetTransform != null)
        {
            // Показываем цифру урона
            if (DamageNumberManager.Instance != null)
            {
                DamageNumberManager.Instance.ShowDamage(targetTransform.position, damage, isCritical);
            }

            // Эффект попадания (искры, взрыв и т.д.)
            if (attackConfig.hitEffectPrefab != null)
            {
                Vector3 hitPosition = targetTransform.position + Vector3.up * 1f; // Центр цели
                GameObject hitEffect = Instantiate(attackConfig.hitEffectPrefab, hitPosition, Quaternion.identity);
                Destroy(hitEffect, 2f); // Уничтожаем эффект через 2 секунды
                Debug.Log($"[PlayerAttackNew] 💥 Эффект попадания создан: {attackConfig.hitEffectPrefab.name}");
            }
        }
    }

    /// <summary>
    /// Создать снаряд
    /// </summary>
    void SpawnProjectile(float damage, bool isCritical = false)
    {
        if (attackConfig.projectilePrefab == null)
        {
            Debug.LogWarning("[PlayerAttackNew] Projectile Prefab не назначен!");
            ApplyDamage(damage); // Наносим урон напрямую
            return;
        }

        // Позиция спавна
        Vector3 spawnPos = transform.position + transform.forward * 0.5f + Vector3.up * 1.2f;

        // Находим цель
        Transform targetTransform = null;
        Enemy enemy = GetEnemyTarget();
        DummyEnemy dummy = GetDummyTarget();

        if (enemy != null)
        {
            targetTransform = enemy.transform;
        }
        else if (dummy != null)
        {
            targetTransform = dummy.transform;
        }

        // Направление
        Vector3 targetPos = targetTransform != null ? targetTransform.position : transform.position + transform.forward * 10f;
        targetPos += Vector3.up * 1f;
        Vector3 direction = (targetPos - spawnPos).normalized;

        // Создаём снаряд
        GameObject projectileObj = Instantiate(attackConfig.projectilePrefab, spawnPos, Quaternion.identity);

        // Пробуем разные типы снарядов
        CelestialProjectile celestialProj = projectileObj.GetComponent<CelestialProjectile>();
        ArrowProjectile arrowProj = projectileObj.GetComponent<ArrowProjectile>();
        Projectile baseProj = projectileObj.GetComponent<Projectile>();

        if (celestialProj != null)
        {
            celestialProj.Initialize(targetTransform, damage, direction, gameObject, null, false, isCritical);

            // Устанавливаем hitEffect из конфига
            if (attackConfig.hitEffectPrefab != null)
            {
                ParticleSystem hitEffect = attackConfig.hitEffectPrefab.GetComponent<ParticleSystem>();
                if (hitEffect != null)
                {
                    celestialProj.SetHitEffect(hitEffect);
                }
            }

            Debug.Log($"[PlayerAttackNew] 🎯 CelestialProjectile создан и инициализирован!");
        }
        else if (arrowProj != null)
        {
            arrowProj.Initialize(targetTransform, damage, direction, gameObject, null, false, isCritical);

            // Устанавливаем hitEffect из конфига
            if (attackConfig.hitEffectPrefab != null)
            {
                ParticleSystem hitEffect = attackConfig.hitEffectPrefab.GetComponent<ParticleSystem>();
                if (hitEffect != null)
                {
                    arrowProj.SetHitEffect(hitEffect);
                }
            }

            Debug.Log($"[PlayerAttackNew] 🎯 ArrowProjectile создан и инициализирован!");
        }
        else if (baseProj != null)
        {
            baseProj.Initialize(targetTransform, damage, direction, gameObject);

            // Устанавливаем hitEffect из конфига
            if (attackConfig.hitEffectPrefab != null)
            {
                baseProj.SetHitEffect(attackConfig.hitEffectPrefab);
            }

            Debug.Log($"[PlayerAttackNew] 🎯 Projectile создан и инициализирован!");
        }
        else
        {
            Debug.LogError("[PlayerAttackNew] У префаба снаряда нет компонента Projectile/CelestialProjectile/ArrowProjectile!");
            Destroy(projectileObj);
        }
    }

    /// <summary>
    /// Проверяем что атакуем сейчас
    /// </summary>
    public bool IsAttacking()
    {
        return isAttacking;
    }

    /// <summary>
    /// Gizmos для визуализации дальности
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (attackConfig != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackConfig.attackRange);
        }
    }
}
