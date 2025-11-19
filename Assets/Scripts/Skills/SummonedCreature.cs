using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Призванное существо (например скелеты для Rogue)
/// Автоматически атакует врагов и следует за хозяином
/// </summary>
[RequireComponent(typeof(Animator))]
public class SummonedCreature : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float followDistance = 10f;
    [SerializeField] private float aggroRange = 15f;

    private Transform owner; // Хозяин (тот кто призвал)
    private Transform currentTarget; // Текущая цель
    private float lifetime; // Время жизни
    private float remainingLifetime;
    private float lastAttackTime;
    private Animator animator;
    private NavMeshAgent navAgent;

    // Animation hashes
    private readonly int isMovingHash = Animator.StringToHash("IsMoving");
    private readonly int attackHash = Animator.StringToHash("Attack");

    /// <summary>
    /// Инициализация призванного существа
    /// </summary>
    public void Initialize(Transform owner, float lifetime)
    {
        this.owner = owner;
        this.lifetime = lifetime;
        this.remainingLifetime = lifetime;

        animator = GetComponent<Animator>();

        // Добавляем NavMeshAgent если нет
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            navAgent = gameObject.AddComponent<NavMeshAgent>();
            navAgent.speed = 3.5f;
            navAgent.acceleration = 8f;
            navAgent.angularSpeed = 120f;
            navAgent.stoppingDistance = attackRange * 0.8f;
        }

        Debug.Log($"[SummonedCreature] Инициализирован. Время жизни: {lifetime}с");
    }

    void Update()
    {
        // Время жизни
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f)
        {
            Die();
            return;
        }

        // Ищем цель если нет
        if (currentTarget == null || !IsTargetValid(currentTarget))
        {
            FindTarget();
        }

        // Есть цель - атакуем
        if (currentTarget != null)
        {
            AttackTarget();
        }
        // Нет цели - следуем за хозяином
        else
        {
            FollowOwner();
        }

        // Обновляем анимацию
        UpdateAnimation();
    }

    /// <summary>
    /// Найти цель для атаки
    /// </summary>
    private void FindTarget()
    {
        if (owner == null) return;

        // Проверяем цель хозяина (TargetSystem)
        TargetSystem ownerTargetSystem = owner.GetComponent<TargetSystem>();
        if (ownerTargetSystem != null && ownerTargetSystem.GetCurrentTarget() != null)
        {
            Transform targetTransform = ownerTargetSystem.GetCurrentTarget().transform;
            float distanceToTarget = Vector3.Distance(transform.position, targetTransform.position);
            if (distanceToTarget <= aggroRange)
            {
                currentTarget = targetTransform;
                Debug.Log($"[SummonedCreature] Атакую цель хозяина: {currentTarget.name}");
                return;
            }
        }

        // Ищем ближайшего врага
        Collider[] hits = Physics.OverlapSphere(transform.position, aggroRange);
        float closestDistance = float.MaxValue;
        Transform closestEnemy = null;

        foreach (Collider hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null && enemy.IsAlive())
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = hit.transform;
                }
            }
        }

        if (closestEnemy != null)
        {
            currentTarget = closestEnemy;
            Debug.Log($"[SummonedCreature] Найден враг: {currentTarget.name}");
        }
    }

    /// <summary>
    /// Атаковать цель
    /// </summary>
    private void AttackTarget()
    {
        if (currentTarget == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        // В радиусе атаки - атакуем
        if (distanceToTarget <= attackRange)
        {
            // Поворачиваемся к цели
            Vector3 direction = (currentTarget.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            // Останавливаем движение
            if (navAgent != null)
            {
                navAgent.isStopped = true;
            }

            // Атакуем если кулдаун прошёл
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                PerformAttack();
                lastAttackTime = Time.time;
            }
        }
        // Вне радиуса - двигаемся к цели
        else
        {
            if (navAgent != null)
            {
                navAgent.isStopped = false;
                navAgent.SetDestination(currentTarget.position);
            }
        }
    }

    /// <summary>
    /// Выполнить атаку
    /// </summary>
    private void PerformAttack()
    {
        // Анимация атаки
        if (animator != null)
        {
            animator.SetTrigger(attackHash);
        }

        // Наносим урон
        Enemy enemy = currentTarget.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(attackDamage);
            Debug.Log($"[SummonedCreature] ⚔️ Атака! Урон: {attackDamage}");
        }
    }

    /// <summary>
    /// Следовать за хозяином
    /// </summary>
    private void FollowOwner()
    {
        if (owner == null) return;

        float distanceToOwner = Vector3.Distance(transform.position, owner.position);

        // Слишком далеко - идём к хозяину
        if (distanceToOwner > followDistance)
        {
            if (navAgent != null)
            {
                navAgent.isStopped = false;
                navAgent.SetDestination(owner.position);
            }
        }
        // Рядом - стоим
        else
        {
            if (navAgent != null)
            {
                navAgent.isStopped = true;
            }
        }
    }

    /// <summary>
    /// Обновить анимацию
    /// </summary>
    private void UpdateAnimation()
    {
        if (animator == null) return;

        bool isMoving = navAgent != null && navAgent.velocity.magnitude > 0.1f;
        animator.SetBool(isMovingHash, isMoving);
    }

    /// <summary>
    /// Проверка валидности цели
    /// </summary>
    private bool IsTargetValid(Transform target)
    {
        if (target == null) return false;

        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy != null && !enemy.IsAlive())
        {
            return false;
        }

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > aggroRange * 1.5f) // Потеряли цель
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Смерть призванного существа
    /// </summary>
    private void Die()
    {
        Debug.Log("[SummonedCreature] 💀 Время жизни истекло");

        // TODO: Анимация смерти / эффект исчезновения
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        // Радиус агро
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        // Радиус атаки
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Радиус следования за хозяином
        if (owner != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(owner.position, followDistance);
        }
    }
}
