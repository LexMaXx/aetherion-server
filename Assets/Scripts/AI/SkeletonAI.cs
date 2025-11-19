using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI для скелета-миньона (Raise Dead)
/// Атакует врага с Enemy компонентом
/// Работает с NavMeshAgent (если есть) или без него (простое движение)
/// </summary>
public class SkeletonAI : MonoBehaviour
{
    [Header("Владелец")]
    private GameObject owner;                    // Некромант, который призвал скелета
    private CharacterStats ownerStats;           // Характеристики некроманта
    private string ownerSocketId = "";           // Socket ID владельца (для мультиплеера)
    private string minionId = "";                // Уникальный ID миньона для сетевой синхронизации

    [Header("Урон")]
    private float baseDamage = 30f;              // Базовый урон
    private float intelligenceScaling = 0.5f;    // Скейлинг от Intelligence

    [Header("Таргетинг")]
    private Transform currentTarget;             // Текущая цель (враг с Target компонентом)
    private TargetableEntity currentTargetEntity;
    private float detectionRange = 20f;          // Дальность обнаружения
    private TargetSystem ownerTargetSystem;      // TargetSystem владельца
    private TargetableEntity ownerTargetable;    // TargetableEntity владельца

    [Header("Атака")]
    [SerializeField] private float attackRange = 2f;        // Дальность атаки
    [SerializeField] private float attackCooldown = 1.5f;   // Кулдаун атаки
    private float nextAttackTime = 0f;

    [Header("Движение")]
    private NavMeshAgent navAgent;              // Опционально, если нет NavMesh
    private Animator animator;
    [SerializeField] private float moveSpeed = 2.625f;   // Было: 5.25f (-50%)
    private bool useNavMesh = false;            // Использовать ли NavMeshAgent

    [Header("Lifetime")]
    private float lifetime = 20f;                // Время существования
    private float spawnTime;

    [Header("Логирование")]
    [SerializeField] private bool enableLogs = true;

    [Header("Сетевая синхронизация")]
    private string currentAnimationState = "Idle";   // Текущее состояние анимации
    private float animationSyncInterval = 0.1f;      // Интервал синхронизации (10 раз в секунду)
    private float lastAnimationSync = 0f;

    // ════════════════════════════════════════════════════════════
    // ИНИЦИАЛИЗАЦИЯ
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Инициализация скелета
    /// </summary>
    public void Initialize(GameObject summoner, CharacterStats stats, float damage, float intScaling, float duration, string ownerId = "")
    {
        owner = summoner;
        ownerStats = stats;
        baseDamage = damage;
        intelligenceScaling = intScaling;
        lifetime = duration;
        ownerSocketId = ownerId;
        ownerTargetSystem = owner != null ? owner.GetComponentInChildren<TargetSystem>() : null;
        ownerTargetable = owner != null ? owner.GetComponentInChildren<TargetableEntity>() : null;

        spawnTime = Time.time;

        // Генерируем уникальный ID миньона для сетевой синхронизации
        // ВАЖНО: Используем простой формат ownerSocketId_skeleton т.к. у каждого игрока может быть только 1 скелет
        minionId = $"{ownerSocketId}_skeleton";

        Log($"💀 Skeleton initialized - Owner: {owner.name}, OwnerID: {ownerSocketId}, MinionID: {minionId}, Damage: {GetDamage()}, Lifetime: {lifetime}s");
    }

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();

        // ВАЖНО: Animator может быть на дочернем объекте (в модели)
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            Debug.Log($"[SkeletonAI] 🎬 Animator найден в Awake: {animator.name}");
            Debug.Log($"[SkeletonAI] 🎬 Animator.enabled = {animator.enabled}");
            Debug.Log($"[SkeletonAI] 🎬 Animator.runtimeAnimatorController = {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL")}");
        }
        else
        {
            Debug.LogError($"[SkeletonAI] ❌ Animator НЕ НАЙДЕН в Awake!");
        }
    }

    private void Start()
    {
        // Проверяем Animator
        if (animator != null)
        {
            if (animator.runtimeAnimatorController == null)
            {
                Log($"⚠️ Animator Controller не назначен! Назначь RogueAnimator.controller на Skeleton префаб");
                Log($"📍 Путь: Assets/Animations/Controllers/RogueAnimator.controller");
                animator = null; // Отключаем чтобы не было ошибок
            }
            else
            {
                Log($"✅ Animator Controller активен: {animator.runtimeAnimatorController.name}");
                Log($"🎬 Animator.enabled = {animator.enabled}");
                Log($"🎬 Animator.applyRootMotion = {animator.applyRootMotion}");
                Log($"🎬 Animator.updateMode = {animator.updateMode}");
                Log($"🎬 Animator.cullingMode = {animator.cullingMode}");

                // ВАЖНО: Принудительно включаем Animator (на всякий случай)
                animator.enabled = true;

                // КРИТИЧЕСКИ ВАЖНО: Устанавливаем cullingMode чтобы анимации играли ВСЕГДА
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Log($"🎥 Animator.cullingMode = AlwaysAnimate (анимации будут играть всегда, даже за камерой)");

                // ВАЖНО: Скелет всегда в боевой стойке (InBattle = true)
                animator.SetBool("InBattle", true);
                Log($"⚔️ InBattle = true (боевая стойка активирована)");

                // Проверяем что параметр установился
                bool inBattleValue = animator.GetBool("InBattle");
                Log($"🔍 Проверка: animator.GetBool('InBattle') = {inBattleValue}");
            }
        }

        // Проверяем NavMeshAgent
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = attackRange - 0.5f;
            useNavMesh = true;
            Log($"✅ NavMeshAgent активирован (скорость: {moveSpeed})");
        }
        else
        {
            useNavMesh = false;
            if (navAgent != null)
            {
                // Отключаем NavMeshAgent если он есть но не на NavMesh
                navAgent.enabled = false;
            }
            Log($"⚠️ NavMesh не найден, используем простое движение");
        }

        // Самоуничтожение через lifetime секунд
        Invoke(nameof(Die), lifetime);
    }

    // ════════════════════════════════════════════════════════════
    // UPDATE
    // ════════════════════════════════════════════════════════════

    private void Update()
    {
        // Проверка времени жизни
        if (Time.time >= spawnTime + lifetime)
        {
            Die();
            return;
        }

        // Ищем цель (враг с Target компонентом)
        if (currentTarget == null || !IsTargetValid())
        {
            if (!TryFollowOwnerTarget())
            {
                FindTarget();
            }
        }

        // Если есть цель
        if (currentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

            // Если в радиусе атаки
            if (distanceToTarget <= attackRange)
            {
                // Останавливаемся
                if (useNavMesh && navAgent.hasPath)
                {
                    navAgent.ResetPath();
                }

                // Поворачиваемся к цели
                Vector3 direction = (currentTarget.position - transform.position).normalized;
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 10f);
                }

                // Атакуем
                if (Time.time >= nextAttackTime)
                {
                    Attack();
                }

                // Анимация Idle
                if (animator != null)
                {
                    bool wasMoving = animator.GetBool("IsMoving");
                    animator.SetBool("IsMoving", false);
                    if (wasMoving)
                    {
                        Debug.Log($"[SkeletonAI] 🛑 IsMoving = false (в радиусе атаки)");
                        SyncAnimation("Idle"); // Синхронизируем с сервером
                    }
                }
            }
            else
            {
                // Двигаемся к цели
                if (useNavMesh)
                {
                    // Используем NavMeshAgent
                    navAgent.SetDestination(currentTarget.position);

                    // Анимация ходьбы
                    if (animator != null)
                    {
                        bool isMoving = navAgent.velocity.magnitude > 0.1f;
                        bool wasMoving = animator.GetBool("IsMoving");
                        animator.SetBool("IsMoving", isMoving);

                        if (isMoving != wasMoving)
                        {
                            Debug.Log($"[SkeletonAI] 🏃 IsMoving = {isMoving} (velocity: {navAgent.velocity.magnitude:F2})");
                            SyncAnimation(isMoving ? "Walking" : "Idle"); // Синхронизируем с сервером
                        }
                    }
                }
                else
                {
                    // Простое движение без NavMesh
                    Vector3 direction = (currentTarget.position - transform.position).normalized;
                    direction.y = 0; // Только горизонтальное движение

                    // Двигаемся к цели
                    transform.position += direction * moveSpeed * Time.deltaTime;

                    // Поворачиваемся к цели
                    if (direction != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 10f);
                    }

                    // Анимация ходьбы
                    if (animator != null)
                    {
                        bool wasMoving = animator.GetBool("IsMoving");
                        animator.SetBool("IsMoving", true);
                        if (!wasMoving)
                        {
                            SyncAnimation("Walking"); // Синхронизируем с сервером
                        }
                    }
                }
            }
        }
        else
        {
            // Нет цели - стоим на месте
            if (useNavMesh && navAgent.hasPath)
            {
                navAgent.ResetPath();
            }

            if (animator != null)
            {
                bool wasMoving = animator.GetBool("IsMoving");
                animator.SetBool("IsMoving", false);
                if (wasMoving)
                {
                    SyncAnimation("Idle"); // Синхронизируем с сервером
                }
            }
        }
    }

    // ════════════════════════════════════════════════════════════
    // ТАРГЕТИНГ
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Попытаться выбрать текущую цель владельца (TargetSystem)
    /// </summary>
    private bool TryFollowOwnerTarget()
    {
        if (ownerTargetSystem == null)
            return false;

        TargetableEntity ownerTarget = ownerTargetSystem.GetCurrentTarget();
        if (!IsTargetValid(ownerTarget))
            return false;

        if (currentTargetEntity != ownerTarget)
        {
            currentTargetEntity = ownerTarget;
            currentTarget = ownerTarget.transform;
            Log($"🎯 Следуем цели хозяина: {currentTarget.name}");
        }

        return true;
    }

    /// <summary>
    /// Поиск врага с TargetableEntity компонентом (для мультиплеера)
    /// </summary>
    private void FindTarget()
    {
        // НОВАЯ СИСТЕМА: Ищем TargetableEntity (работает с Enemy, NetworkPlayer, и т.д.)
        TargetableEntity[] targets = FindObjectsOfType<TargetableEntity>();

        float closestDistance = float.MaxValue;
        Transform closestTarget = null;

        foreach (TargetableEntity target in targets)
        {
            if (!IsTargetAllowed(target))
                continue;

            if (!target.IsAlive())
                continue;

            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance <= detectionRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target.transform;
                currentTargetEntity = target;
            }
        }

        if (closestTarget != null)
        {
            currentTarget = closestTarget;
            currentTargetEntity = closestTarget.GetComponent<TargetableEntity>();
            Log($"🎯 Новая цель найдена: {currentTarget.name} (дистанция: {closestDistance:F1}м)");
        }
    }

    /// <summary>
    /// Проверка валидности цели
    /// </summary>
    private bool IsTargetValid()
    {
        if (currentTarget == null)
            return false;

        TargetableEntity targetEntity = currentTarget.GetComponent<TargetableEntity>();
        if (targetEntity == null)
            return false;

        return IsTargetValid(targetEntity);
    }

    private bool IsTargetValid(TargetableEntity entity)
    {
        if (entity == null)
            return false;

        if (!IsTargetAllowed(entity))
            return false;

        Transform entityTransform = entity.transform;
        if (entityTransform == null)
            return false;

        float distance = Vector3.Distance(transform.position, entityTransform.position);
        return distance <= detectionRange * 1.25f;
    }

    // ════════════════════════════════════════════════════════════
    // АТАКА
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Атака текущей цели
    /// </summary>
    private void Attack()
    {
        if (currentTarget == null)
            return;

        // Триггерим анимацию атаки
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Синхронизируем анимацию атаки с сервером
        SyncAnimation("Attack");

        // Наносим урон
        float damage = GetDamage();

        // НОВАЯ СИСТЕМА: Используем TargetableEntity для урона (поддержка мультиплеера)
        TargetableEntity targetEntity = currentTarget.GetComponent<TargetableEntity>();
        if (targetEntity != null)
        {
            // Создаём фейкового "атакующего" для логирования
            // В будущем можно добавить SkeletonEntity компонент для правильного owner tracking
            targetEntity.TakeDamage(damage, null);
            Log($"⚔️ Skeleton атакует {currentTarget.name}: {damage:F1} урона");
        }
        else
        {
            // Fallback: старая система для Enemy (локальные боты)
            Enemy enemy = currentTarget.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Log($"⚔️ Skeleton атакует {currentTarget.name}: {damage:F1} урона (fallback Enemy)");
            }
            else
            {
                // Fallback 2: HealthSystem
                HealthSystem targetHealth = currentTarget.GetComponent<HealthSystem>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(damage);
                    Log($"⚔️ Skeleton атакует {currentTarget.name}: {damage:F1} урона (fallback HealthSystem)");
                }
            }
        }

        // Устанавливаем следующее время атаки
        nextAttackTime = Time.time + attackCooldown;
    }

    /// <summary>
    /// Рассчитать финальный урон скелета
    /// </summary>
    public float GetDamage()
    {
        float damage = baseDamage;

        if (ownerStats != null)
        {
            damage += ownerStats.intelligence * intelligenceScaling;
        }

        return damage;
    }

    // ════════════════════════════════════════════════════════════
    // СМЕРТЬ
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Смерть скелета (по таймеру или при убийстве)
    /// КРИТИЧЕСКИ ВАЖНО: Отписываемся из FogOfWar перед уничтожением!
    /// </summary>
    private void Die()
    {
        Log($"💀 Skeleton умирает (прожил {Time.time - spawnTime:F1}с)");

        // КРИТИЧЕСКИ ВАЖНО: Отписываемся из FogOfWar перед уничтожением!
        // Если этого не сделать, FogOfWar будет хранить ссылку на уничтоженный GameObject
        // что приведёт к проблемам с синхронизацией при повторном призыве скелета
        Enemy enemyComponent = GetComponent<Enemy>();
        if (enemyComponent != null)
        {
            FogOfWar fogOfWar = FindFirstObjectByType<FogOfWar>();
            if (fogOfWar != null)
            {
                fogOfWar.UnregisterEnemy(enemyComponent);
                Log($"✅ Скелет отписан из FogOfWar перед уничтожением");
            }
            else
            {
                Log($"⚠️ FogOfWar не найден (это нормально для одиночной игры)");
            }
        }
        else
        {
            Log($"⚠️ Enemy компонент не найден на скелете");
        }

        // КРИТИЧЕСКИ ВАЖНО: Отправляем событие minion_destroyed на сервер
        // Это нужно чтобы другие игроки удалили скелета из своих словарей
        if (!string.IsNullOrEmpty(ownerSocketId) &&
            SocketIOManager.Instance != null &&
            SocketIOManager.Instance.IsConnected &&
            NetworkSyncManager.Instance != null &&
            ownerSocketId == NetworkSyncManager.Instance.LocalPlayerSocketId)
        {
            SocketIOManager.Instance.SendMinionDestroyed(minionId);
            Log($"🌐 Отправлено событие minion_destroyed: {minionId}");
        }

        // TODO: Эффект смерти (кости рассыпаются)
        // GameObject deathEffect = Resources.Load<GameObject>("Effects/SkeletonDeath");
        // if (deathEffect != null)
        // {
        //     Instantiate(deathEffect, transform.position, Quaternion.identity);
        // }

        Destroy(gameObject);
    }

    // ════════════════════════════════════════════════════════════
    // ВИЗУАЛИЗАЦИЯ
    // ════════════════════════════════════════════════════════════

    private void OnDrawGizmosSelected()
    {
        // Радиус обнаружения
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Радиус атаки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Линия к текущей цели
        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }

    // ════════════════════════════════════════════════════════════
    // СЕТЕВАЯ СИНХРОНИЗАЦИЯ
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Синхронизировать анимацию с сервером
    /// ИСПРАВЛЕНО: Не блокирует локальные анимации, только отправляет на сервер
    /// </summary>
    private void SyncAnimation(string newAnimationState)
    {
        // Проверяем что анимация изменилась
        if (newAnimationState == currentAnimationState)
            return;

        // Сохраняем новое состояние
        string previousState = currentAnimationState;
        currentAnimationState = newAnimationState;

        // Отправляем анимацию на сервер (только если владелец - локальный игрок)
        if (string.IsNullOrEmpty(ownerSocketId))
            return; // Нет владельца - это локальная игра

        if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
            return; // Не подключены к серверу

        // КРИТИЧЕСКИ ВАЖНО: Отправляем только если это НАШ скелет (не сетевой)
        if (NetworkSyncManager.Instance != null && ownerSocketId == NetworkSyncManager.Instance.LocalPlayerSocketId)
        {
            // Защита от спама: проверяем интервал только для сетевой отправки
            if (Time.time - lastAnimationSync < animationSyncInterval)
                return;

            lastAnimationSync = Time.time;

            SocketIOManager.Instance.SendMinionAnimation(minionId, currentAnimationState);
            Log($"🌐 Анимация отправлена: {previousState} → {currentAnimationState}");
        }
    }

    // ════════════════════════════════════════════════════════════
    // ЛОГИРОВАНИЕ
    // ════════════════════════════════════════════════════════════

    private void Log(string message)
    {
        if (enableLogs)
        {
            Debug.Log($"[SkeletonAI] {message}");
        }
    }
    private bool IsTargetAllowed(TargetableEntity entity)
    {
        if (entity == null)
            return false;

        if (IsOwnerEntity(entity))
            return false;

        // Скелет атакует всех владельцев кроме хозяина, поэтому не фильтруем по фракциям
        return entity.IsEntityAlive();
    }

    private bool IsOwnerEntity(TargetableEntity entity)
    {
        if (entity == null || owner == null)
            return false;

        if (ownerTargetable != null && entity == ownerTargetable)
            return true;

        Transform entityTransform = entity.transform;
        if (entityTransform == owner.transform || entityTransform.IsChildOf(owner.transform))
            return true;

        if (!string.IsNullOrEmpty(ownerSocketId) && entity.GetOwnerId() == ownerSocketId)
            return true;

        return false;
    }
}
