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
    private ActionPointsSystem actionPointsSystem;
    private NetworkCombatSync combatSync;
    private TargetableEntity localPlayerEntity; // НОВОЕ: для проверки "не атаковать себя"
    private AudioSource audioSource; // Для воспроизведения звуков атаки

    [Header("Состояние атаки")]
    private float lastAttackTime = 0f;
    private bool isAttacking = false;
    private Enemy currentTarget = null;

    /// <summary>
    /// Проверить атакует ли сейчас персонаж (для блокировки других анимаций)
    /// </summary>
    public bool IsCurrentlyAttacking => isAttacking;

    void Start()
    {
        // Находим компоненты
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        targetSystem = GetComponent<TargetSystem>();
        characterStats = GetComponent<CharacterStats>();
        manaSystem = GetComponent<ManaSystem>();
        actionPointsSystem = GetComponent<ActionPointsSystem>();
        combatSync = GetComponent<NetworkCombatSync>();

        // КРИТИЧНО: Находим свою TargetableEntity для проверки "не атаковать себя"
        localPlayerEntity = GetComponent<TargetableEntity>();
        if (localPlayerEntity == null)
        {
            localPlayerEntity = GetComponentInParent<TargetableEntity>();
        }
        if (localPlayerEntity == null)
        {
            Debug.LogWarning("[PlayerAttackNew] ⚠️ TargetableEntity не найден - проверка 'не атаковать себя' не будет работать!");
        }

        // Инициализация AudioSource для звуков атаки
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D звук
            audioSource.minDistance = 5f;
            audioSource.maxDistance = 50f;
            Debug.Log("[PlayerAttackNew] 🔊 AudioSource создан автоматически для звуков атаки");
        }

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

        // КРИТИЧНО: Подключаем AttackButton для атаки
        ConnectAttackButton();
    }

    /// <summary>
    /// Подключить AttackButton к системе атаки
    /// </summary>
    void ConnectAttackButton()
    {
        // Ищем AttackButton в сцене
        AttackButton attackButton = FindFirstObjectByType<AttackButton>();
        if (attackButton != null)
        {
            // Подписываемся на событие нажатия
            attackButton.OnAttackPressed += HandleAttackButtonPressed;
            Debug.Log("[PlayerAttackNew] ✅ AttackButton подключен!");
        }
        else
        {
            Debug.LogWarning("[PlayerAttackNew] ⚠️ AttackButton не найден в сцене! Атака не будет работать.");
            Debug.LogWarning("[PlayerAttackNew] Добавьте AttackButton в Canvas для управления атакой.");
        }
    }

    /// <summary>
    /// Обработчик нажатия AttackButton
    /// </summary>
    void HandleAttackButtonPressed()
    {
        Debug.Log("[PlayerAttackNew] 🎯 AttackButton нажата!");

        // Проверка может ли атаковать (эффекты контроля)
        if (effectManager != null && !effectManager.CanAttack())
        {
            Debug.Log("[PlayerAttackNew] ❌ Не могу атаковать - под эффектом контроля!");
            return;
        }

        // Выполняем атаку
        TryAttack();
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
        // КРИТИЧЕСКАЯ ПРОВЕРКА: Блокировка при смерти
        // ═══════════════════════════════════════════════════════

        PlayerDeathHandler deathHandler = GetComponent<PlayerDeathHandler>();
        if (deathHandler != null && deathHandler.IsDead)
        {
            // Мертв - НЕ МОЖЕМ атаковать!
            return;
        }

        // ═══════════════════════════════════════════════════════
        // ПРОВЕРКА: Контроль эффектов (Stun, Sleep, Fear)
        // ═══════════════════════════════════════════════════════

        if (effectManager != null && effectManager.IsUnderCrowdControl())
        {
            // Под контролем - не можем действовать
            return;
        }

        // ═══════════════════════════════════════════════════════
        // АТАКА ТОЛЬКО ЧЕРЕЗ КНОПКУ AttackButton (НА ВСЕХ ПЛАТФОРМАХ!)
        // ═══════════════════════════════════════════════════════
        //
        // ЛКМ/Тап используется ТОЛЬКО для таргетинга (TargetSystem)
        // Атака вызывается через AttackButton.OnAttackPressed event
        //
        // Подключение AttackButton происходит автоматически в Start() или
        // вручную: attackButton.OnAttackPressed += () => TryAttack();
        //
        // СТАРЫЙ КОД (удалён):
        // #if !UNITY_ANDROID && !UNITY_IOS
        //     if (Input.GetMouseButtonDown(0)) { TryAttack(); }
        // #endif
        //
        // ═══════════════════════════════════════════════════════

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

        // PARTY SYSTEM: Проверяем что цель не член группы (не атакуем союзников скиллами)
        if (targetTransform != null)
        {
            TargetableEntity targetEntity = targetTransform.GetComponent<TargetableEntity>();
            if (targetEntity != null)
            {
                NetworkPlayer networkPlayer = targetEntity.GetComponent<NetworkPlayer>();
                if (networkPlayer != null && !string.IsNullOrEmpty(networkPlayer.socketId))
                {
                    if (PartyManager.Instance != null && PartyManager.Instance.IsAlly(networkPlayer.socketId))
                    {
                        Debug.LogWarning($"[PlayerAttackNew] ⛔ НЕЛЬЗЯ АТАКОВАТЬ СОЮЗНИКА СКИЛЛОМ! {networkPlayer.username} - член вашей группы!");
                        return;
                    }
                }
            }
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
        Debug.Log("[PlayerAttackNew] 🗡️ TryAttack вызван!");

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

        // КРИТИЧЕСКАЯ ПРОВЕРКА: Достаточно ли Action Points?
        if (actionPointsSystem != null)
        {
            int attackCost = actionPointsSystem.GetAttackCost();
            if (!actionPointsSystem.HasEnoughPointsForAttack())
            {
                Debug.Log($"[PlayerAttackNew] ❌ Недостаточно AP! Нужно: {attackCost}, Есть: {actionPointsSystem.GetCurrentPoints()}");
                return;
            }
        }

        // НОВОЕ: Получаем цель через TargetableEntity (поддержка PvP)
        Transform targetTransform = null;
        TargetableEntity targetEntity = GetTargetableEntity();

        if (targetEntity != null)
        {
            // Есть цель из TargetSystem (Enemy, NetworkPlayer, или Dummy)
            targetTransform = targetEntity.transform;
            Debug.Log($"[PlayerAttackNew] 🎯 Атакуем цель из TargetSystem: {targetEntity.GetEntityName()}");
        }
        else
        {
            // Fallback: ищем DummyEnemy напрямую (если TargetSystem не используется)
            DummyEnemy dummy = GetDummyTarget();
            if (dummy != null)
            {
                targetTransform = dummy.transform;
                Debug.Log($"[PlayerAttackNew] 🎯 Атакуем Dummy (fallback): {dummy.name}");
            }
        }

        if (targetTransform == null)
        {
            Debug.Log("[PlayerAttackNew] ❌ Нет цели для атаки");
            return;
        }

        // КРИТИЧЕСКАЯ ПРОВЕРКА: НЕ АТАКУЕМ САМОГО СЕБЯ!
        if (targetEntity != null && localPlayerEntity != null && targetEntity == localPlayerEntity)
        {
            Debug.LogError("[PlayerAttackNew] ⛔ НЕЛЬЗЯ АТАКОВАТЬ САМОГО СЕБЯ!");
            // Сбрасываем цель в TargetSystem
            if (targetSystem != null)
            {
                targetSystem.ClearTarget();
            }
            return;
        }

        // Дополнительная проверка по transform (на случай если targetEntity == null)
        if (targetTransform == transform || targetTransform.IsChildOf(transform) || transform.IsChildOf(targetTransform))
        {
            Debug.LogError("[PlayerAttackNew] ⛔ НЕЛЬЗЯ АТАКОВАТЬ САМОГО СЕБЯ (проверка по transform)!");
            if (targetSystem != null)
            {
                targetSystem.ClearTarget();
            }
            return;
        }

        // PARTY SYSTEM: Проверяем что цель не член группы (не атакуем союзников)
        if (targetEntity != null)
        {
            // Получаем NetworkPlayer цели
            NetworkPlayer networkPlayer = targetEntity.GetComponent<NetworkPlayer>();
            if (networkPlayer != null && !string.IsNullOrEmpty(networkPlayer.socketId))
            {
                // Проверяем является ли цель союзником (членом группы)
                if (PartyManager.Instance != null && PartyManager.Instance.IsAlly(networkPlayer.socketId))
                {
                    Debug.LogWarning($"[PlayerAttackNew] ⛔ НЕЛЬЗЯ АТАКОВАТЬ СОЮЗНИКА! {networkPlayer.username} - член вашей группы!");
                    return;
                }
            }
        }

        // Проверяем дистанцию
        float distance = Vector3.Distance(transform.position, targetTransform.position);
        if (distance > attackConfig.attackRange)
        {
            Debug.Log($"[PlayerAttackNew] Цель слишком далеко: {distance:F1}m > {attackConfig.attackRange}m");
            return;
        }

        // УДАЛЕНО: больше не сохраняем Enemy, так как теперь используем TargetableEntity
        // Старый код: currentTarget = enemy;

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

        // КРИТИЧЕСКАЯ ОПЕРАЦИЯ: Тратим Action Points
        if (actionPointsSystem != null)
        {
            int attackCost = actionPointsSystem.GetAttackCost();
            bool apSpent = actionPointsSystem.TrySpendActionPoints(attackCost);
            if (!apSpent)
            {
                Debug.Log($"[PlayerAttackNew] ❌ Не удалось потратить AP!");
                return;
            }
            Debug.Log($"[PlayerAttackNew] ✅ Потрачено {attackCost} AP. Осталось: {actionPointsSystem.GetCurrentPoints()}/{actionPointsSystem.GetMaxPoints()}");
        }

        // Выполняем атаку
        PerformAttack(targetTransform);
    }

    /// <summary>
    /// Получить текущую цель из TargetSystem (TargetableEntity)
    /// НОВОЕ: Возвращает TargetableEntity вместо Enemy для поддержки PvP
    /// </summary>
    TargetableEntity GetTargetableEntity()
    {
        if (targetSystem != null && targetSystem.HasTarget())
        {
            TargetableEntity targetEntity = targetSystem.GetCurrentTarget();
            if (targetEntity != null && targetEntity.IsEntityAlive())
            {
                Debug.Log($"[PlayerAttackNew] 🎯 Цель: {targetEntity.GetEntityName()} (Faction: {targetEntity.GetFaction()})");
                return targetEntity;
            }
        }

        return null;
    }

    /// <summary>
    /// Получить Enemy цель (для обратной совместимости)
    /// УСТАРЕЛО: Используйте GetTargetableEntity() вместо этого
    /// </summary>
    Enemy GetEnemyTarget()
    {
        // Пытаемся получить из TargetSystem
        if (targetSystem != null && targetSystem.HasTarget())
        {
            TargetableEntity targetEntity = targetSystem.GetCurrentTarget();
            if (targetEntity != null && targetEntity.IsEntityAlive())
            {
                // Пытаемся получить Enemy компонент из TargetableEntity
                Enemy enemy = targetEntity.GetComponent<Enemy>();
                if (enemy != null && enemy.IsAlive())
                {
                    return enemy;
                }

                // Если нет Enemy компонента, но есть TargetableEntity - это нормально
                // (например NetworkPlayer с NetworkPlayerEntity)
                Debug.Log($"[PlayerAttackNew] Цель {targetEntity.GetEntityName()} не имеет Enemy компонента (это нормально для игроков)");
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

        // ЗВУК: Воспроизводим attackSound при атаке
        if (attackConfig.attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackConfig.attackSound, attackConfig.soundVolume);
            Debug.Log($"[PlayerAttackNew] 🔊 Attack sound воспроизведён: {attackConfig.attackSound.name}");
        }

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
        Debug.Log($"[PlayerAttackNew] ⚡ Проверка анимации: animator={animator != null}, trigger='{attackConfig?.animationTrigger}'");

        if (animator != null && !string.IsNullOrEmpty(attackConfig.animationTrigger))
        {
            animator.SetTrigger(attackConfig.animationTrigger);
            animator.speed = attackConfig.animationSpeed;
            Debug.Log($"[PlayerAttackNew] ✅ Анимация запущена локально: {attackConfig.animationTrigger}");

            // КРИТИЧЕСКОЕ: Синхронизируем анимацию атаки с другими игроками!
            Debug.Log($"[PlayerAttackNew] 🔍 Проверка синхронизации: SocketIO={SocketIOManager.Instance != null}, Connected={SocketIOManager.Instance?.IsConnected ?? false}");

            if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
            {
                SocketIOManager.Instance.UpdateAnimation(attackConfig.animationTrigger, attackConfig.animationSpeed);
                Debug.Log($"[PlayerAttackNew] 🌐 Анимация атаки отправлена на сервер ({attackConfig.animationTrigger}, speed={attackConfig.animationSpeed}x)");
            }
            else
            {
                Debug.LogWarning($"[PlayerAttackNew] ⚠️ Синхронизация пропущена: SocketIO={SocketIOManager.Instance != null}, Connected={SocketIOManager.Instance?.IsConnected ?? false}");
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerAttackNew] ⚠️ Анимация НЕ запущена: animator={animator != null}, trigger='{attackConfig?.animationTrigger}'");
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

        // НОВОЕ: Отправляем атаку на сервер для мультиплеера (Enemy или NetworkPlayer)
        TargetableEntity targetEntity = GetTargetableEntity();
        if (combatSync != null && targetEntity != null)
        {
            string attackType = attackConfig.attackType == AttackType.Ranged ? "ranged" : "melee";
            combatSync.SendAttack(targetEntity.gameObject, damage, attackType);
            Debug.Log($"[PlayerAttackNew] 📤 Атака отправлена на сервер: {targetEntity.GetEntityName()}, урон: {damage:F1}");
        }
    }

    /// <summary>
    /// Применить урон к текущей цели
    /// </summary>
    void ApplyDamage(float damage, bool isCritical = false)
    {
        Transform targetTransform = null;


        // НОВОЕ: Используем TargetableEntity для поддержки всех типов целей
        TargetableEntity targetEntity = GetTargetableEntity();

        if (targetEntity != null && targetEntity.IsEntityAlive())
        {
            // Применяем урон к TargetableEntity (Enemy, NetworkPlayer, или другие)
            targetEntity.TakeDamage(damage, GetComponent<TargetableEntity>());
            targetTransform = targetEntity.transform;
            Debug.Log($"[PlayerAttackNew] ⚔️ Урон {damage:F1} нанесён {targetEntity.GetEntityName()} (Faction: {targetEntity.GetFaction()})");
        }
        else
        {
            // Fallback: Проверяем DummyEnemy напрямую (если TargetSystem не используется)
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
                    Debug.Log($"[PlayerAttackNew] ⚔️ Урон {damage:F1} нанесён DummyEnemy (fallback)");
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

                // КРИТИЧЕСКОЕ: Синхронизируем эффект попадания с другими игроками!
                if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
                {
                    string targetSocketId = "";
                    NetworkPlayer networkTarget = targetTransform.GetComponent<NetworkPlayer>();
                    if (networkTarget != null)
                    {
                        targetSocketId = networkTarget.socketId;
                    }

                    SocketIOManager.Instance.SendVisualEffectSpawned(
                        "hit_effect",
                        attackConfig.hitEffectPrefab.name,
                        hitPosition,
                        Quaternion.identity,
                        targetSocketId,
                        2f
                    );
                    Debug.Log($"[PlayerAttackNew] 🌐 Эффект попадания отправлен на сервер ({attackConfig.hitEffectPrefab.name})");
                }
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

        // Находим цель через TargetableEntity (поддержка PvP и единая система таргетинга)
        Transform targetTransform = null;
        TargetableEntity targetEntity = GetTargetableEntity();

        if (targetEntity != null)
        {
            // Используем цель из TargetSystem (NetworkPlayer, Enemy, или Dummy)
            targetTransform = targetEntity.transform;
            Debug.Log($"[PlayerAttackNew] 🎯 Снаряд летит в: {targetEntity.GetEntityName()} (Faction: {targetEntity.GetFaction()})");
        }
        else
        {
            // Fallback: ищем ближайший DummyEnemy если нет цели в TargetSystem
            DummyEnemy dummy = GetDummyTarget();
            if (dummy != null)
            {
                targetTransform = dummy.transform;
                Debug.Log($"[PlayerAttackNew] 🎯 Снаряд летит в Dummy (fallback): {dummy.name}");
            }
        }

        // Направление
        Vector3 targetPos = targetTransform != null ? targetTransform.position : transform.position + transform.forward * 10f;
        targetPos += Vector3.up * 1f;
        Vector3 direction = (targetPos - spawnPos).normalized;

        // Создаём снаряд
        GameObject projectileObj = Instantiate(attackConfig.projectilePrefab, spawnPos, Quaternion.identity);

        // 🚀 SYNC: Отправляем событие создания снаряда на сервер для других игроков
        string targetSocketId = "";
        if (targetTransform != null)
        {
            NetworkPlayer networkTarget = targetTransform.GetComponentInParent<NetworkPlayer>();
            if (networkTarget != null)
            {
                targetSocketId = networkTarget.socketId;
            }
            else if (targetEntity != null)
            {
                string ownerId = targetEntity.GetOwnerId();
                if (!string.IsNullOrEmpty(ownerId))
                {
                    targetSocketId = ownerId;
                }
            }
        }

        SocketIOManager socketIO = SocketIOManager.Instance;
        if (socketIO != null && socketIO.IsConnected)
        {
            // skillId = 0 для базовой атаки (не скилл)
            socketIO.SendProjectileSpawned(0, spawnPos, direction, targetSocketId);
            Debug.Log($"[PlayerAttackNew] 🌐 Снаряд синхронизирован с сервером: pos=({spawnPos.x:F1}, {spawnPos.y:F1}, {spawnPos.z:F1})");
        }

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

            // ЗВУК: Устанавливаем hitSound из конфига
            if (attackConfig.hitSound != null)
            {
                celestialProj.SetHitSound(attackConfig.hitSound);
                Debug.Log($"[PlayerAttackNew] 🔊 Hit sound установлен: {attackConfig.hitSound.name}");
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

            // ЗВУК: Устанавливаем hitSound из конфига
            if (attackConfig.hitSound != null)
            {
                arrowProj.SetHitSound(attackConfig.hitSound);
                Debug.Log($"[PlayerAttackNew] 🔊 Hit sound установлен: {attackConfig.hitSound.name}");
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

            // ЗВУК: Устанавливаем hitSound из конфига
            if (attackConfig.hitSound != null)
            {
                baseProj.SetHitSound(attackConfig.hitSound);
                Debug.Log($"[PlayerAttackNew] 🔊 Hit sound установлен: {attackConfig.hitSound.name}");
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
