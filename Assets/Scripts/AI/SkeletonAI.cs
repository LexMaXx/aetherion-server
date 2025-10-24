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

    [Header("Урон")]
    private float baseDamage = 30f;              // Базовый урон
    private float intelligenceScaling = 0.5f;    // Скейлинг от Intelligence

    [Header("Таргетинг")]
    private Transform currentTarget;             // Текущая цель (враг с Target компонентом)
    private float detectionRange = 20f;          // Дальность обнаружения

    [Header("Атака")]
    [SerializeField] private float attackRange = 2f;        // Дальность атаки
    [SerializeField] private float attackCooldown = 1.5f;   // Кулдаун атаки
    private float nextAttackTime = 0f;

    [Header("Движение")]
    private NavMeshAgent navAgent;              // Опционально, если нет NavMesh
    private Animator animator;
    [SerializeField] private float moveSpeed = 3.5f;    // Скорость движения (если нет NavMesh)
    private bool useNavMesh = false;            // Использовать ли NavMeshAgent

    [Header("Lifetime")]
    private float lifetime = 20f;                // Время существования
    private float spawnTime;

    [Header("Логирование")]
    [SerializeField] private bool enableLogs = true;

    // ════════════════════════════════════════════════════════════
    // ИНИЦИАЛИЗАЦИЯ
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Инициализация скелета
    /// </summary>
    public void Initialize(GameObject summoner, CharacterStats stats, float damage, float intScaling, float duration)
    {
        owner = summoner;
        ownerStats = stats;
        baseDamage = damage;
        intelligenceScaling = intScaling;
        lifetime = duration;

        spawnTime = Time.time;

        Log($"💀 Skeleton initialized - Owner: {owner.name}, Damage: {GetDamage()}, Lifetime: {lifetime}s");
    }

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
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
            FindTarget();
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
                    animator.SetBool("IsMoving", false);
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
                        animator.SetBool("IsMoving", navAgent.velocity.magnitude > 0.1f);
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
                        animator.SetBool("IsMoving", true);
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
                animator.SetBool("IsMoving", false);
            }
        }
    }

    // ════════════════════════════════════════════════════════════
    // ТАРГЕТИНГ
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Поиск врага с Enemy компонентом
    /// </summary>
    private void FindTarget()
    {
        // Ищем всех врагов с Enemy компонентом
        Enemy[] enemies = FindObjectsOfType<Enemy>();

        float closestDistance = float.MaxValue;
        Transform closestTarget = null;

        foreach (Enemy enemy in enemies)
        {
            // Проверяем что это не владелец (некромант)
            if (enemy.gameObject == owner)
                continue;

            // Проверяем что враг жив
            if (!enemy.IsAlive())
                continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance <= detectionRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = enemy.transform;
            }
        }

        if (closestTarget != null)
        {
            currentTarget = closestTarget;
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

        // Проверяем что Enemy компонент ещё существует
        Enemy enemyComponent = currentTarget.GetComponent<Enemy>();
        if (enemyComponent == null)
            return false;

        // Проверяем что враг жив
        if (!enemyComponent.IsAlive())
            return false;

        // Проверяем дистанцию
        float distance = Vector3.Distance(transform.position, currentTarget.position);
        if (distance > detectionRange * 1.5f) // Небольшой hysteresis
            return false;

        return true;
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

        // Наносим урон
        float damage = GetDamage();

        // Проверяем Enemy компонент
        Enemy enemy = currentTarget.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Log($"⚔️ Skeleton атакует {currentTarget.name}: {damage:F1} урона");
        }
        else
        {
            // Fallback: проверяем HealthSystem для других целей
            HealthSystem targetHealth = currentTarget.GetComponent<HealthSystem>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage);
                Log($"⚔️ Skeleton атакует {currentTarget.name}: {damage:F1} урона");
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
    /// </summary>
    private void Die()
    {
        Log($"💀 Skeleton умирает (прожил {Time.time - spawnTime:F1}с)");

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
    // ЛОГИРОВАНИЕ
    // ════════════════════════════════════════════════════════════

    private void Log(string message)
    {
        if (enableLogs)
        {
            Debug.Log($"[SkeletonAI] {message}");
        }
    }
}
