using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Представляет удаленного игрока в мультиплеере
/// Синхронизирует позицию, анимацию, здоровье
/// </summary>
public class NetworkPlayer : MonoBehaviour
{
    [Header("Network Info")]
    public string socketId;
    public string username;
    public string characterClass;

    [Header("Components")]
    private Animator animator;
    private CharacterController characterController;
    private NetworkTransform networkTransform;
    private EffectManager effectManager;
    private CharacterStats characterStats;
    private HealthSystem healthSystem;

    [Header("Health (for health bar only)")]
    private int currentHP = 100;
    private int maxHP = 100;
    
    // Animation state
    private string currentAnimationState = "Idle";

    // Health
    private int currentMP = 100;
    private int maxMP = 100;

    // Interpolation
    private bool hasReceivedFirstUpdate = false;
public bool IsDead => isDead;

    void Awake()
    {
        // ВАЖНО: Animator находится на дочернем объекте "Model"
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"[NetworkPlayer] ⚠️ Animator не найден для {gameObject.name}!");
        }
        else
        {
            // КРИТИЧЕСКИ ВАЖНО: Устанавливаем Animator Culling Mode = AlwaysAnimate
            // Это предотвращает "прыжки" когда NetworkPlayer входит/выходит из поля зрения камеры
            // По умолчанию Unity использует CullUpdateTransforms - анимация останавливается вне камеры
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Debug.Log($"[NetworkPlayer] ✅ Animator culling mode установлен в AlwaysAnimate (предотвращает прыжки при входе в камеру)");
        }

        // ═══════════════════════════════════════════════════════════════════
        // КРИТИЧЕСКИ ВАЖНО: Для AOE урона NetworkPlayer нужен ОБЫЧНЫЙ коллайдер!
        // CharacterController является коллайдером, но мы его отключаем.
        // Поэтому добавляем отдельный CapsuleCollider для Physics.OverlapSphere
        // ═══════════════════════════════════════════════════════════════════

        // Проверяем есть ли уже CapsuleCollider (не CharacterController)
        CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
        {
            capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
            // Настройки такие же как у CharacterController для точного хитбокса
            capsuleCollider.height = 2.16f;
            capsuleCollider.center = new Vector3(0, 1.08f, 0); // Центр на половине высоты
            capsuleCollider.radius = 0.3f;

            // КРИТИЧЕСКИ ВАЖНО: Для kinematic Rigidbody коллайдер должен быть ТРИГГЕРОМ!
            // Это предотвращает проваливание под террейн и конфликты с физикой
            capsuleCollider.isTrigger = true;

            // КРИТИЧЕСКИ ВАЖНО: Material должен быть None (Physics Material)!
            capsuleCollider.material = null;

            Debug.Log("[NetworkPlayer] ✅ CapsuleCollider добавлен как TRIGGER (height=2.16, radius=0.3, material=None)");
        }
        else
        {
            // Если коллайдер уже есть - ИСПРАВЛЯЕМ ВСЕ ПАРАМЕТРЫ (не только trigger и material)!
            // КРИТИЧЕСКОЕ: Префаб может иметь height=1 вместо правильного 2.16!
            capsuleCollider.height = 2.16f;
            capsuleCollider.center = new Vector3(0, 1.08f, 0);
            capsuleCollider.radius = 0.3f;
            capsuleCollider.isTrigger = true;
            capsuleCollider.material = null;
            Debug.Log("[NetworkPlayer] ✅ CapsuleCollider ИСПРАВЛЕН: height=2.16 (было неправильное), center=1.08, radius=0.3, trigger=true, material=None");
        }

        // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Добавляем Rigidbody (kinematic) для предотвращения проваливания под террейн
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Kinematic = управляется через Transform, не через физику
            rb.useGravity = false; // Гравитация отключена (NetworkTransform управляет позицией)
            rb.interpolation = RigidbodyInterpolation.None; // Интерполяцию делает NetworkTransform

            // КРИТИЧЕСКИ ВАЖНО: CollisionDetectionMode.Discrete НЕ РАБОТАЕТ для триггеров!
            // Используем ContinuousSpeculative для более точной детекции триггеров
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            // КРИТИЧЕСКИ ВАЖНО: Замораживаем только X и Z вращения, оставляем Y свободным для поворотов
            // Это предотвращает проваливание под террейн (фикс от пользователя)
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            Debug.Log("[NetworkPlayer] ✅ Rigidbody (kinematic) добавлен с FreezeRotationX|Z (Y rotation allowed), ContinuousSpeculative");
        }
        else
        {
            // Rigidbody уже есть - настраиваем его правильно
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            // Замораживаем только X и Z вращения, оставляем Y свободным для поворотов
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            Debug.Log("[NetworkPlayer] ✅ Rigidbody настроен (kinematic, no gravity, freeze rotation X|Z only, ContinuousSpeculative)");
        }

        // CharacterController больше не нужен для NetworkPlayer - удаляем его полностью
        characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            Debug.Log("[NetworkPlayer] 🗑️ Удаляем CharacterController (используем CapsuleCollider + Rigidbody вместо него)");
            Destroy(characterController);
        }

        Debug.Log("[NetworkPlayer] ✅ NetworkPlayer настроен: CapsuleCollider (trigger) + Rigidbody (kinematic, freeze rotation) - AOE урон работает, проваливание предотвращено!");

        // Добавляем или получаем NetworkTransform для плавной синхронизации
        networkTransform = GetComponent<NetworkTransform>();
        if (networkTransform == null)
        {
            networkTransform = gameObject.AddComponent<NetworkTransform>();
            Debug.Log($"[NetworkPlayer] ✅ NetworkTransform добавлен для плавного движения");
        }

        // КРИТИЧЕСКИ ВАЖНО: ОТКЛЮЧАЕМ локальные компоненты управления (НЕ УДАЛЯЕМ!)
        // ИСПРАВЛЕНО: Используем .enabled = false вместо Destroy()
        // Это позволит PlayerDeathHandler корректно блокировать/разблокировать компоненты
        var playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log("[NetworkPlayer] ✅ PlayerController ОТКЛЮЧЁН (управляется сервером через NetworkTransform)");
        }

        var playerAttack = GetComponent<PlayerAttack>();
        if (playerAttack != null)
        {
            playerAttack.enabled = false;
            Debug.Log("[NetworkPlayer] ✅ PlayerAttack ОТКЛЮЧЁН");
        }

        var playerAttackNew = GetComponent<PlayerAttackNew>();
        if (playerAttackNew != null)
        {
            playerAttackNew.enabled = false;
            Debug.Log("[NetworkPlayer] ✅ PlayerAttackNew ОТКЛЮЧЁН");
        }

        var targetSystem = GetComponent<TargetSystem>();
        if (targetSystem != null)
        {
            targetSystem.enabled = false;
            Debug.Log("[NetworkPlayer] ✅ TargetSystem ОТКЛЮЧЁН");
        }

        // КРИТИЧЕСКИ ВАЖНО: Удаляем ВСЕ FogOfWar и FogOfWarCanvas (вызывают визуальные "прыжки")
        // NetworkPlayer не должен иметь собственный FogOfWar - только локальный игрок имеет его
        // ВАЖНО: Может быть НЕСКОЛЬКО FogOfWar из-за дублирования в ArenaManager/BattleSceneManager!
        FogOfWar[] allFogOfWar = GetComponents<FogOfWar>();
        if (allFogOfWar.Length > 0)
        {
            foreach (FogOfWar fow in allFogOfWar)
            {
                Destroy(fow);
            }
            Debug.Log($"[NetworkPlayer] ✅ Удалено {allFogOfWar.Length} компонентов FogOfWar (только локальный игрок должен иметь FoW)");
        }

        FogOfWarCanvas[] allFogCanvas = GetComponents<FogOfWarCanvas>();
        if (allFogCanvas.Length > 0)
        {
            foreach (FogOfWarCanvas canvas in allFogCanvas)
            {
                Destroy(canvas);
            }
            Debug.Log($"[NetworkPlayer] ✅ Удалено {allFogCanvas.Length} компонентов FogOfWarCanvas (вызывали визуальные прыжки)");
        }

        // ИНИЦИАЛИЗАЦИЯ: Добавляем все необходимые компоненты
        InitializeNetworkPlayerComponents();
    }

    /// <summary>
    /// НОВАЯ СИСТЕМА: Инициализация всех компонентов NetworkPlayer
    /// Аналогично локальному игроку, но с отключенным управлением
    /// </summary>
    void InitializeNetworkPlayerComponents()
    {
        Debug.Log($"[NetworkPlayer] 🔧 Инициализация компонентов для {username}...");

        // ═══════════════════════════════════════════════════════════════════
        // 1. CharacterStats (SPECIAL характеристики)
        // ═══════════════════════════════════════════════════════════════════
        characterStats = GetComponent<CharacterStats>();
        if (characterStats == null)
        {
            characterStats = gameObject.AddComponent<CharacterStats>();

            // Устанавливаем дефолтные характеристики (будут обновлены от сервера)
            characterStats.strength = 2;
            characterStats.perception = 5;
            characterStats.endurance = 2;
            characterStats.wisdom = 1;
            characterStats.intelligence = 3;
            characterStats.agility = 4;
            characterStats.luck = 2;

            characterStats.RecalculateStats();
            Debug.Log($"[NetworkPlayer] ✅ CharacterStats добавлен (дефолтные SPECIAL)");
        }

        // ═══════════════════════════════════════════════════════════════════
        // 2. HealthSystem (HP система)
        // ═══════════════════════════════════════════════════════════════════
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            healthSystem = gameObject.AddComponent<HealthSystem>();
            Debug.Log($"[NetworkPlayer] ✅ HealthSystem добавлен");
        }

        // ═══════════════════════════════════════════════════════════════════
        // 3. ManaSystem (Mana система)
        // ═══════════════════════════════════════════════════════════════════
        ManaSystem manaSystem = GetComponent<ManaSystem>();
        if (manaSystem == null)
        {
            manaSystem = gameObject.AddComponent<ManaSystem>();
            Debug.Log($"[NetworkPlayer] ✅ ManaSystem добавлен");
        }

        // ═══════════════════════════════════════════════════════════════════
        // 4. EffectManager (система эффектов: Root, Stun, Slow, DoT)
        // ═══════════════════════════════════════════════════════════════════
        effectManager = GetComponent<EffectManager>();
        if (effectManager == null)
        {
            effectManager = gameObject.AddComponent<EffectManager>();
            Debug.Log($"[NetworkPlayer] ✅ EffectManager добавлен (Stun, Root, AOE эффекты)");
        }

        // ═══════════════════════════════════════════════════════════════════
        // 5. Enemy.cs (КРИТИЧЕСКИ ВАЖНО для PvP таргетинга!)
        // ═══════════════════════════════════════════════════════════════════
        Enemy enemyComponent = GetComponent<Enemy>();
        if (enemyComponent == null)
        {
            enemyComponent = gameObject.AddComponent<Enemy>();
            Debug.Log($"[NetworkPlayer] ✅ Enemy.cs добавлен (для PvP таргетинга)");
        }

        // ═══════════════════════════════════════════════════════════════════
        // 6. PlayerDeathHandler (обработка смерти и респавна)
        // ═══════════════════════════════════════════════════════════════════
        PlayerDeathHandler deathHandler = GetComponent<PlayerDeathHandler>();
        if (deathHandler == null)
        {
            deathHandler = gameObject.AddComponent<PlayerDeathHandler>();
            Debug.Log($"[NetworkPlayer] ✅ PlayerDeathHandler добавлен (DropToGround, анимация смерти)");
        }

        // КРИТИЧНО: NetworkPlayer не должен отправлять события смерти на сервер (только получать их)
        deathHandler.SetLocalPlayer(false);
        Debug.Log($"[NetworkPlayer] 🚫 SetLocalPlayer(false) - NetworkPlayer НЕ будет отправлять player_died");

        Debug.Log($"[NetworkPlayer] ✅✅✅ Все компоненты инициализированы для {username}!");
    }

    void Start()
    {
        // ВАЖНО: Устанавливаем боевую стойку для NetworkPlayer (InBattle = true)
        if (animator != null)
        {
            // Проверяем есть ли параметр InBattle
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "InBattle")
                {
                    animator.SetBool("InBattle", true);
                    Debug.Log($"[NetworkPlayer] ✅ Боевая стойка установлена для {username}");
                    break;
                }
            }
        }
    }

    void Update()
    {
        // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ ОТ ФРИЗОВ:
        // NetworkTransform ПОЛНОСТЬЮ управляет позицией и ротацией!
        // Убрали дублирующую интерполяцию которая конфликтовала с SmoothDamp
        //
        // ВАЖНО: Не трогаем transform.position и transform.rotation здесь!
        // NetworkTransform.Update() уже делает всю работу через Vector3.SmoothDamp
    }

    // УДАЛЕНО: Старая система nameplate - заменена на EnemyNameplate.cs

    /// <summary>
    /// Обновить позицию от сервера (с поддержкой velocity для Dead Reckoning)
    /// </summary>
    public void UpdatePosition(Vector3 position, Quaternion rotation, Vector3 velocity = default, float timestamp = 0f)
    {
        if (timestamp == 0f)
        {
            timestamp = Time.time;
        }

        // КРИТИЧЕСКОЕ: NetworkTransform ВСЕГДА управляет позицией!
        // Он использует SmoothDamp для плавного движения как в Dota 2
        if (networkTransform != null)
        {
            networkTransform.ReceivePositionUpdate(position, rotation, velocity, timestamp);
        }
        else
        {
            // Это не должно происходить! NetworkTransform добавляется в Awake()
            Debug.LogError($"[NetworkPlayer] ❌ NetworkTransform == null для {username}! Это баг!");

            // Emergency fallback - телепортация
            transform.position = position;
            transform.rotation = rotation;
        }

        hasReceivedFirstUpdate = true;
    }

    /// <summary>
    /// Обновить анимацию от сервера
    /// </summary>
    public void UpdateAnimation(string animationState)
    {
        // ДИАГНОСТИКА: Логируем ВСЕ попытки обновления анимации (ВСЕГДА, не только каждую секунду!)
        Debug.Log($"[NetworkPlayer] 🔄 UpdateAnimation вызван для {username}: текущее={currentAnimationState}, новое={animationState}");

        if (animator == null)
        {
            Debug.LogError($"[NetworkPlayer] ❌ КРИТИЧЕСКАЯ ОШИБКА: Animator is null для {username}!");
            Debug.LogError($"[NetworkPlayer] 🔍 Пытаюсь найти Animator в дочерних объектах...");

            // Пытаемся найти Animator снова
            animator = GetComponentInChildren<Animator>();

            if (animator == null)
            {
                Debug.LogError($"[NetworkPlayer] ❌ Animator НЕ НАЙДЕН даже в дочерних объектах!");
                return;
            }
            else
            {
                Debug.Log($"[NetworkPlayer] ✅ Animator найден в дочернем объекте: {animator.gameObject.name}");
            }
        }

        // ВАЖНО: ВСЕГДА обновляем анимацию для реал-тайм синхронизации
        bool stateChanged = (currentAnimationState != animationState);

        if (stateChanged)
        {
            Debug.Log($"[NetworkPlayer] 🎬 Анимация для {username}: {currentAnimationState} → {animationState}");
            currentAnimationState = animationState;
        }
        else
        {
            Debug.Log($"[NetworkPlayer] 🔄 Повторное применение анимации {animationState} для {username}");
        }

        // ВАЖНО: PlayerController использует Blend Tree систему
        // IsMoving (bool), MoveX (float), MoveY (float)
        // Не используем isWalking/isRunning, потому что они отсутствуют

        // Set new state
        Debug.Log($"[NetworkPlayer] 🎭 Применяю анимацию '{animationState}' для {username}");

        switch (animationState)
        {
            case "Idle":
                Debug.Log($"[NetworkPlayer] ➡️ Idle: IsMoving=false, MoveX=0, MoveY=0, speed=1.0");
                animator.SetBool("IsMoving", false);
                animator.SetFloat("MoveX", 0);
                animator.SetFloat("MoveY", 0);
                animator.speed = 1.0f;
                break;

            case "Walking":
                Debug.Log($"[NetworkPlayer] ➡️ Walking: IsMoving=true, MoveX=0, MoveY=0.5, speed=0.5");
                animator.SetBool("IsMoving", true);
                animator.SetFloat("MoveX", 0);
                animator.SetFloat("MoveY", 0.5f); // Напрямую устанавливаем значение (Animator сам сделает blend)
                animator.speed = 0.5f; // Замедленная анимация для ходьбы
                break;

            case "Running":
                Debug.Log($"[NetworkPlayer] ➡️ Running: IsMoving=true, MoveX=0, MoveY=1.0, speed=1.0");
                animator.SetBool("IsMoving", true);
                animator.SetFloat("MoveX", 0);
                animator.SetFloat("MoveY", 1.0f); // Напрямую устанавливаем значение (Animator сам сделает blend)
                animator.speed = 1.0f; // Нормальная скорость анимации
                break;

            case "Attacking":
            case "Attack":
                Debug.Log($"[NetworkPlayer] ➡️ Attack animation received");

                // КРИТИЧЕСКОЕ: Warrior/Paladin используют "WarriorAttack", другие используют "Attack"
                // Проверяем какой триггер есть в аниматоре
                string attackTrigger = "Attack"; // По умолчанию

                if (HasAnimatorParameter(animator, "WarriorAttack"))
                {
                    attackTrigger = "WarriorAttack";
                    Debug.Log($"[NetworkPlayer] ✅ Используем WarriorAttack триггер для {username}");
                }
                else if (HasAnimatorParameter(animator, "PaladinAttack"))
                {
                    attackTrigger = "PaladinAttack";
                    Debug.Log($"[NetworkPlayer] ✅ Используем PaladinAttack триггер для {username}");
                }
                else if (HasAnimatorParameter(animator, "Attack"))
                {
                    attackTrigger = "Attack";
                    Debug.Log($"[NetworkPlayer] ✅ Используем Attack триггер для {username}");
                }
                else
                {
                    Debug.LogWarning($"[NetworkPlayer] ⚠️ Не найден триггер атаки для {username}!");
                }

                animator.SetTrigger(attackTrigger);
                Debug.Log($"[NetworkPlayer] 🎬 Установлен триггер '{attackTrigger}' для {username}");
                break;

            case "Dead":
                Debug.Log($"[NetworkPlayer] ➡️ Dead: isDead=true, IsMoving=false");
                if (HasAnimatorParameter(animator, "isDead"))
                {
                    animator.SetBool("isDead", true);
                }
                animator.SetBool("IsMoving", false);
                break;

            case "Casting":
                Debug.Log($"[NetworkPlayer] ➡️ Casting: Trigger=Cast");
                animator.SetTrigger("Cast");
                break;

            default:
                Debug.LogWarning($"[NetworkPlayer] ⚠️ Неизвестное состояние анимации: '{animationState}' для {username}");
                break;
        }

        // Логируем итоговое состояние параметров аниматора
        Debug.Log($"[NetworkPlayer] 📊 Состояние Animator для {username}: IsMoving={animator.GetBool("IsMoving")}, MoveY={animator.GetFloat("MoveY"):F2}, speed={animator.speed:F2}");
    }

    /// <summary>
    /// Проверить есть ли параметр в Animator
    /// </summary>
    private bool HasAnimatorParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    /// <summary>
    /// Обновить здоровье от сервера
    /// </summary>
    public void UpdateHealth(int hp, int maxHp, int mp, int maxMp)
    {
        currentHP = hp;
        maxHP = maxHp;
        currentMP = mp;
        maxMP = maxMp;

        // УДАЛЕНО: UpdateHealthBar() - теперь EnemyNameplate сам обновляет HP бар через LateUpdate

        // УДАЛЕНО: Проверка смерти - HealthSystem сам вызовет PlayerDeathHandler при HP = 0
        // Смерть обрабатывается через HealthSystem.Die() → OnDeath событие → PlayerDeathHandler
    }

    // УДАЛЕНО: UpdateHealthBar() - теперь управляется через EnemyNameplate.cs

    /// <summary>
    /// Обработать смерть
    /// УПРОЩЕНО: Теперь используем PlayerDeathHandler для обработки смерти
    /// </summary>
    private bool isDead = false;

   /// <summary>
/// Обработать респавн
/// </summary>
public void OnRespawn(Vector3 spawnPosition)
{
    isDead = false;
    currentHP = maxHP;
    currentMP = maxMP;

    Debug.Log($"[NetworkPlayer] 🔄 Респавн {username}...");

    // КРИТИЧНО: Вызываем PlayerDeathHandler.Respawn() для сброса isDead и включения компонентов
    // PlayerDeathHandler.Respawn() уже делает:
    // - Сброс isDead = false
    // - Восстановление HealthSystem
    // - Телепортацию на spawnPosition
    // - Включение CharacterController, PlayerController, PlayerAttack, SkillExecutor
    // - Сброс анимации
    PlayerDeathHandler deathHandler = GetComponent<PlayerDeathHandler>();
    if (deathHandler != null)
    {
        deathHandler.Respawn(spawnPosition);
        Debug.Log($"[NetworkPlayer] ✅ PlayerDeathHandler.Respawn() вызван для {username}");
    }
    else
    {
        Debug.LogError($"[NetworkPlayer] ❌ PlayerDeathHandler не найден для {username}!");
    }

    // Восстанавливаем NetworkPlayerEntity (для таргета и урона)
    NetworkPlayerEntity playerEntity = GetComponent<NetworkPlayerEntity>();
    if (playerEntity != null)
    {
        playerEntity.Revive(1f);
        Debug.Log($"[NetworkPlayer] ✅ {username}: NetworkPlayerEntity.isAlive восстановлен!");
    }

    // Включаем коллайдер (специфично для NetworkPlayer)
    CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
    if (capsuleCollider != null)
        capsuleCollider.enabled = true;

    // Включаем NetworkTransform (специфично для NetworkPlayer)
    if (networkTransform != null)
    {
        networkTransform.enabled = true;
        networkTransform.ResetState();
        Debug.Log($"[NetworkPlayer] ✅ NetworkTransform восстановлен для {username}");
    }

    Debug.Log($"[NetworkPlayer] ✅ {username} успешно возродился на {spawnPosition}");
}
    /// <summary>
    /// Полностью сбросить состояние Animator (для респавна)
    /// </summary>
    private void ResetAnimator()
    {
        if (animator == null)
        {
            Debug.LogWarning($"[NetworkPlayer] ⚠️ Animator == null для {username}!");
            return;
        }

        Debug.Log($"[NetworkPlayer] 🎬 Сброс Animator для {username}");

        // Сбрасываем все Bool параметры
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(param.name, false);
            }
            else if (param.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(param.name, 0f);
            }
            else if (param.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(param.name);
            }
        }

        // КРИТИЧЕСКИ ВАЖНО: Принудительно запускаем Idle анимацию
        animator.Play("Idle", 0, 0f);

        Debug.Log($"[NetworkPlayer] ✅ Animator сброшен в Idle для {username}");
    }

    /// <summary>
    /// Воспроизвести анимацию атаки С ЭФФЕКТАМИ
    /// </summary>
    public void PlayAttackAnimation(string attackType)
    {
        if (animator == null) return;

        Debug.Log($"[NetworkPlayer] 🎬 PlayAttackAnimation для {username}: тип={attackType}");

        // ЭФФЕКТ 1: СВЕЧЕНИЕ ОРУЖИЯ (через ClassWeaponManager)
        ClassWeaponManager weaponManager = GetComponentInChildren<ClassWeaponManager>();
        if (weaponManager != null)
        {
            Debug.Log($"[NetworkPlayer] ✨ Активирую свечение оружия для {username}");
            weaponManager.ActivateWeaponGlow();

            // Автоотключение через 1 секунду
            StartCoroutine(DeactivateWeaponGlowAfterDelay(weaponManager, 1.0f));
        }
        else
        {
            Debug.LogWarning($"[NetworkPlayer] ⚠️ ClassWeaponManager не найден для {username} - свечение оружия не работает");
        }

        // ЭФФЕКТ 2: АНИМАЦИЯ АТАКИ
        switch (attackType)
        {
            case "melee":
                Debug.Log($"[NetworkPlayer] ⚔️ Ближняя атака: Trigger=Attack");
                animator.SetTrigger("Attack");
                break;

            case "ranged":
                Debug.Log($"[NetworkPlayer] 🏹 Дальняя атака: Trigger=RangedAttack (+ снаряд)");
                if (HasAnimatorParameter(animator, "RangedAttack"))
                {
                    animator.SetTrigger("RangedAttack");
                }
                else
                {
                    // Fallback если нет RangedAttack
                    animator.SetTrigger("Attack");
                }

                // ЭФФЕКТ 3: СОЗДАНИЕ СНАРЯДА для дальних атак
                // ОТКЛЮЧЕНО: Теперь снаряды создаются через событие projectile_spawned от сервера
                // StartCoroutine(SpawnProjectileAfterDelay(attackType, 0.3f));
                Debug.Log($"[NetworkPlayer] ℹ️ Снаряд будет создан через projectile_spawned от сервера");
                break;

            case "skill":
            case "magic":
                Debug.Log($"[NetworkPlayer] 🔮 Магия/Скилл: Trigger=Cast");
                animator.SetTrigger("Cast");
                break;
        }
    }

    /// <summary>
    /// Корутина: отключить свечение оружия через задержку
    /// </summary>
    private System.Collections.IEnumerator DeactivateWeaponGlowAfterDelay(ClassWeaponManager weaponManager, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (weaponManager != null)
        {
            weaponManager.DeactivateWeaponGlow();
            Debug.Log($"[NetworkPlayer] 💤 Свечение оружия отключено для {username}");
        }
    }

    /// <summary>
    /// Корутина: создать снаряд через задержку (для дальних атак)
    /// </summary>
    private System.Collections.IEnumerator SpawnProjectileAfterDelay(string attackType, float delay)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log($"[NetworkPlayer] 🚀 Создание снаряда для {username}, класс={characterClass}");

        // Определяем префаб снаряда по классу персонажа
        string projectileName = GetProjectilePrefabName(characterClass);

        if (string.IsNullOrEmpty(projectileName))
        {
            Debug.LogWarning($"[NetworkPlayer] ⚠️ Нет снаряда для класса {characterClass}");
            yield break;
        }

        // Загружаем префаб снаряда
        GameObject projectilePrefab = Resources.Load<GameObject>($"Projectiles/{projectileName}");

        if (projectilePrefab == null)
        {
            // Пытаемся найти в Assets/Prefabs/Projectiles/
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{projectileName} t:Prefab");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                projectilePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Debug.Log($"[NetworkPlayer] ✅ Префаб загружен из Assets: {path}");
            }
#endif
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[NetworkPlayer] ⚠️ Префаб снаряда '{projectileName}' не найден!");
            yield break;
        }

        // Находим точку спавна снаряда (правая рука или WeaponTip)
        Transform spawnPoint = FindWeaponTip();
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position + transform.forward * 0.5f + Vector3.up * 1.5f;

        // Направление полета (вперёд от игрока)
        Vector3 direction = transform.forward;

        // Создаём снаряд
        GameObject projectileObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.LookRotation(direction));

        // Настраиваем снаряд (проверяем сначала CelestialProjectile, потом Projectile)
        CelestialProjectile celestialProjectile = projectileObj.GetComponent<CelestialProjectile>();
        if (celestialProjectile != null)
        {
            // Для NetworkPlayer снаряд чисто визуальный (урон обрабатывает сервер)
            // isVisualOnly = true отключает автонаведение и коллизию
            celestialProjectile.Initialize(null, 0f, direction, this.gameObject, null, isVisualOnly: true);
            Debug.Log($"[NetworkPlayer] ✅ CelestialProjectile создан для {username} - визуальный режим (без автонаведения)");
        }
        else
        {
            // Fallback на старый Projectile для других классов
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(null, 0f, direction, this.gameObject);
                Debug.Log($"[NetworkPlayer] ✅ Снаряд создан: {projectileName} для {username}");
            }
        }
    }

    /// <summary>
    /// Получить имя префаба снаряда по классу персонажа
    /// </summary>
    private string GetProjectilePrefabName(string className)
    {
        switch (className)
        {
            case "Archer":
                return "ArrowProjectile";
            case "Mage":
                return "CelestialBallProjectile"; // Обновлено: Celestial Ball вместо Fireball
            case "Rogue":
                return "SoulShardsProjectile";
            default:
                return null; // Воин и Паладин - ближний бой, снарядов нет
        }
    }

    /// <summary>
    /// Найти точку оружия для спавна снарядов
    /// </summary>
    private Transform FindWeaponTip()
    {
        // Пытаемся найти WeaponTip
        Transform weaponTip = transform.Find("WeaponTip");
        if (weaponTip != null) return weaponTip;

        // Ищем в дочерних объектах
        string[] weaponTipNames = new string[]
        {
            "WeaponTip",
            "Weapon_Tip",
            "RightHandIndex3",
            "mixamorig:RightHandIndex3",
            "RightHand",
            "mixamorig:RightHand"
        };

        Transform[] allTransforms = GetComponentsInChildren<Transform>();
        foreach (string tipName in weaponTipNames)
        {
            foreach (Transform t in allTransforms)
            {
                if (t.name.Contains(tipName))
                {
                    Debug.Log($"[NetworkPlayer] ✅ Найдена точка оружия: {t.name}");
                    return t;
                }
            }
        }

        Debug.LogWarning($"[NetworkPlayer] ⚠️ Точка оружия не найдена для {username}");
        return null;
    }

    /// <summary>
    /// НОВЫЙ МЕТОД: Получить урон (аналогично DummyEnemy)
    /// </summary>
    public void TakeDamage(float damage)
    {
        // 1. Наносим урон через HealthSystem
        if (healthSystem != null)
        {
            healthSystem.TakeDamage(damage);
            Debug.Log($"[NetworkPlayer] ❤️ {username} HP: {healthSystem.CurrentHealth:F0}/{healthSystem.MaxHealth:F0}");
        }
        else
        {
            // Fallback если нет HealthSystem - обновляем локальные переменные
            currentHP -= (int)damage;
            currentHP = Mathf.Max(0, currentHP);
            Debug.Log($"[NetworkPlayer] ❤️ {username} HP: {currentHP}/{maxHP} (fallback)");
        }

        // 2. Визуальные эффекты
        ShowDamage(damage);

        // 3. Показываем цифры урона
        if (DamageNumberManager.Instance != null)
        {
            Vector3 damagePos = transform.position + Vector3.up * 2f;
            DamageNumberManager.Instance.ShowDamage(damagePos, damage, false, false);
        }

        // УДАЛЕНО: Проверка смерти
        // HealthSystem сам вызовет событие OnDeath → PlayerDeathHandler.OnPlayerDied()
        // Не нужно проверять здесь - система смерти автоматическая
    }

    /// <summary>
    /// Показать урон (визуальный эффект + взрыв)
    /// ИЗМЕНЕНО: Отключено мигание красным по запросу пользователя
    /// </summary>
    public void ShowDamage(float damage)
    {
        Debug.Log($"[NetworkPlayer] {username} получил {damage} урона!");

        // ЭФФЕКТ 1: Мигание красным (ОТКЛЮЧЕНО)
        // StartCoroutine(FlashRed());

        // ЭФФЕКТ 2: Hit effect (включён обратно с оптимизацией - не зацикленный)
        SpawnHitEffect();
    }

    /// <summary>
    /// Создать эффект попадания (взрыв)
    /// </summary>
    private void SpawnHitEffect()
    {
        // Загружаем префаб эффекта попадания
        GameObject hitEffectPrefab = Resources.Load<GameObject>("Effects/HitEffect");

        if (hitEffectPrefab == null)
        {
            Debug.LogWarning("[NetworkPlayer] ⚠️ HitEffect префаб не найден в Resources/Effects/");
            return;
        }

        // Позиция эффекта = центр персонажа (торс)
        Vector3 hitPosition = transform.position + Vector3.up * 1.0f;

        // Создаём эффект
        GameObject hitEffectObj = Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);

        Debug.Log($"[NetworkPlayer] 💥 Эффект попадания создан для {username} в позиции {hitPosition}");

        // Автоуничтожение через 2 секунды
        Destroy(hitEffectObj, 2.0f);
    }

    /// <summary>
    /// Мигание красным при получении урона
    /// ИСПРАВЛЕНО: Сохраняем ссылку на material instance чтобы избежать утечки памяти
    /// </summary>
    private System.Collections.IEnumerator FlashRed()
    {
        // КРИТИЧЕСКОЕ: Находим ВСЕ SkinnedMeshRenderer (может быть тело + одежда + оружие)
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning($"[NetworkPlayer] ⚠️ SkinnedMeshRenderer не найден для {username}!");
            yield break;
        }

        Debug.Log($"[NetworkPlayer] 💥 FlashRed для {username}: найдено {renderers.Length} mesh'ей");

        // Сохраняем оригинальные цвета ВСЕХ mesh'ей
        Material[] materialInstances = new Material[renderers.Length];
        Color[] originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            materialInstances[i] = renderers[i].material; // Создаём instance один раз
            originalColors[i] = materialInstances[i].color;

            // Красим в красный
            materialInstances[i].color = Color.red;
        }

        yield return new WaitForSeconds(0.1f);

        // Возвращаем оригинальные цвета
        for (int i = 0; i < materialInstances.Length; i++)
        {
            if (materialInstances[i] != null)
            {
                materialInstances[i].color = originalColors[i];
            }
        }

        Debug.Log($"[NetworkPlayer] ✅ FlashRed завершён для {username}");
    }

    // УДАЛЕНО: GetNameplate(), ShowNameplate(), HideNameplate(), OnDestroy() - заменено на EnemyNameplate.cs

    // ===== ТРАНСФОРМАЦИЯ (SIMPLE TRANSFORMATION) =====

    /// <summary>
    /// Применить трансформацию к сетевому игроку (SIMPLE TRANSFORMATION)
    /// Создаёт модель медведя как child, скрывает паладина
    /// </summary>
    public void ApplyTransformation(int skillId)
    {
        Debug.Log($"[NetworkPlayer] 🐻 ApplyTransformation (SIMPLE TRANSFORMATION) вызван для {username}, skillId={skillId}");

        // НОВАЯ СИСТЕМА: Получаем скилл из SkillConfigLoader (static class)
        SkillConfig skill = SkillConfigLoader.LoadSkillById(skillId);
        if (skill == null)
        {
            Debug.LogError($"[NetworkPlayer] ❌ SkillConfig с ID {skillId} не найден!");
            return;
        }

        if (skill.transformationModel == null)
        {
            Debug.LogError($"[NetworkPlayer] ❌ У скилла {skill.skillName} нет модели трансформации!");
            return;
        }

        Debug.Log($"[NetworkPlayer] 🔍 Скилл найден: {skill.skillName}, модель: {skill.transformationModel.name}");

        // Получаем или добавляем SimpleTransformation компонент
        SimpleTransformation simpleTransformation = GetComponent<SimpleTransformation>();
        if (simpleTransformation == null)
        {
            simpleTransformation = gameObject.AddComponent<SimpleTransformation>();
            Debug.Log($"[NetworkPlayer] ➕ Добавлен SimpleTransformation компонент для {username}");
        }

        // Выполняем трансформацию (передаём аниматор явно)
        bool success = simpleTransformation.TransformTo(skill.transformationModel, animator);
        if (!success)
        {
            Debug.LogError($"[NetworkPlayer] ❌ Трансформация не удалась для {username}!");
            return;
        }

        Debug.Log($"[NetworkPlayer] 🐻 ✅ SIMPLE TRANSFORMATION завершён для {username}!");
    }

    /// <summary>
    /// Завершить трансформацию сетевого игрока (SIMPLE TRANSFORMATION)
    /// </summary>
    public void EndTransformation()
    {
        Debug.Log($"[NetworkPlayer] 🔄 EndTransformation (SIMPLE TRANSFORMATION) вызван для {username}");

        // Получаем SimpleTransformation компонент
        SimpleTransformation simpleTransformation = GetComponent<SimpleTransformation>();
        if (simpleTransformation != null)
        {
            // Возвращаем паладина (удаляем медведя, показываем паладина)
            simpleTransformation.RevertToOriginal();
            Debug.Log($"[NetworkPlayer] ✅ Паладин восстановлен для {username}");
        }

        Debug.Log($"[NetworkPlayer] 🔄 ✅ Трансформация завершена для {username}!");
    }

    // ═══════════════════════════════════════════════════════════════
    // ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ СОВМЕСТИМОСТИ С СИСТЕМОЙ ЭФФЕКТОВ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Применить статус-эффект к NetworkPlayer (Stun, Root, Slow, Poison, Burn, etc.)
    /// </summary>
    public void ApplyStatusEffect(EffectType effectType, float duration, float magnitude = 0f)
    {
        if (effectManager == null)
        {
            Debug.LogWarning($"[NetworkPlayer] ⚠️ EffectManager не найден для {username}!");
            return;
        }

        // Создаём простой EffectConfig для эффекта
        EffectConfig effectConfig = new EffectConfig
        {
            effectType = effectType,
            duration = duration,
            power = magnitude,
            damageOrHealPerTick = magnitude, // Для DoT эффектов
            tickInterval = 1f
        };

        // Применяем через EffectManager (требует EffectConfig + CharacterStats)
        // Для NetworkPlayer casterStats = null (эффект приходит от другого игрока)
        effectManager.ApplyEffect(effectConfig, null, socketId);

        Debug.Log($"[NetworkPlayer] ✨ {username} получил эффект {effectType} на {duration:F1} сек");
    }

    /// <summary>
    /// Хил NetworkPlayer (для тестирования хила или Battle Heal скилла)
    /// </summary>
    public void Heal(float amount)
    {
        if (healthSystem != null)
        {
            healthSystem.Heal(amount);
            Debug.Log($"[NetworkPlayer] 💚 {username} восстановил {amount:F1} HP. HP: {healthSystem.CurrentHealth:F0}/{healthSystem.MaxHealth:F0}");
        }
        else
        {
            // Fallback
            currentHP += (int)amount;
            currentHP = Mathf.Min(currentHP, maxHP);
            Debug.Log($"[NetworkPlayer] 💚 {username} восстановил {amount:F1} HP. HP: {currentHP}/{maxHP} (fallback)");
        }

        // Показываем цифры хила
        if (DamageNumberManager.Instance != null)
        {
            Vector3 healPos = transform.position + Vector3.up * 2f;
            DamageNumberManager.Instance.ShowDamage(healPos, amount, false, true); // isHeal = true
        }
    }

    /// <summary>
    /// Получить текущий HP
    /// </summary>
    public float GetCurrentHealth()
    {
        if (healthSystem != null)
            return healthSystem.CurrentHealth;
        return currentHP;
    }

    /// <summary>
    /// Получить максимальный HP
    /// </summary>
    public float GetMaxHealth()
    {
        if (healthSystem != null)
            return healthSystem.MaxHealth;
        return maxHP;
    }

    /// <summary>
    /// Жив ли NetworkPlayer (для таргетинга)
    /// </summary>
    public bool IsAlive()
    {
        if (healthSystem != null)
            return healthSystem.CurrentHealth > 0f;
        return currentHP > 0;
    }

    /// <summary>
    /// Получить EffectManager (для проверки эффектов снаружи)
    /// </summary>
    public EffectManager GetEffectManager()
    {
        return effectManager;
    }

    /// <summary>
    /// Получить CharacterStats (для расчётов урона с учётом баффов)
    /// </summary>
    public CharacterStats GetCharacterStats()
    {
        return characterStats;
    }

    // Public getters (legacy - для обратной совместимости)
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public int CurrentMP => currentMP;
    public int MaxMP => maxMP;
    // IsAlive - используется метод IsAlive() выше (строка 830)
}
