using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI для врагов - патрулирование, преследование, атака
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float patrolRadius = 15f;
    [SerializeField] private float patrolWaitTime = 3f;

    [Header("Combat")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackCooldown = 2f;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask playerLayer;

    // Компоненты
    private NavMeshAgent agent;
    private Animator animator;

    // Состояния AI
    private enum AIState { Idle, Patrol, Chase, Attack }
    private AIState currentState = AIState.Idle;

    // Патрулирование
    private Vector3 patrolPoint;
    private float patrolTimer = 0f;
    private Vector3 startPosition;

    // Атака
    private float attackTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        startPosition = transform.position;

        // Автопоиск игрока если не указан
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        // Начинаем с патрулирования
        SetNewPatrolPoint();
        currentState = AIState.Patrol;
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Обновляем таймеры
        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        // Машина состояний
        switch (currentState)
        {
            case AIState.Idle:
                UpdateIdleState();
                break;

            case AIState.Patrol:
                UpdatePatrolState(distanceToPlayer);
                break;

            case AIState.Chase:
                UpdateChaseState(distanceToPlayer);
                break;

            case AIState.Attack:
                UpdateAttackState(distanceToPlayer);
                break;
        }

        // Обновляем анимации
        UpdateAnimations();
    }

    void UpdateIdleState()
    {
        patrolTimer += Time.deltaTime;

        if (patrolTimer >= patrolWaitTime)
        {
            SetNewPatrolPoint();
            currentState = AIState.Patrol;
            patrolTimer = 0f;
        }

        // Проверяем игрока
        if (CanSeePlayer())
        {
            currentState = AIState.Chase;
        }
    }

    void UpdatePatrolState(float distanceToPlayer)
    {
        // Идём к точке патруля
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentState = AIState.Idle;
        }

        // Заметили игрока
        if (distanceToPlayer <= detectionRadius && CanSeePlayer())
        {
            currentState = AIState.Chase;
        }
    }

    void UpdateChaseState(float distanceToPlayer)
    {
        // Преследуем игрока
        agent.SetDestination(player.position);

        // Игрок в радиусе атаки
        if (distanceToPlayer <= attackRange)
        {
            currentState = AIState.Attack;
            agent.isStopped = true;
        }

        // Игрок убежал слишком далеко
        if (distanceToPlayer > detectionRadius * 1.5f)
        {
            currentState = AIState.Idle;
            agent.isStopped = false;
        }
    }

    void UpdateAttackState(float distanceToPlayer)
    {
        // Поворачиваемся к игроку
        Vector3 lookDirection = (player.position - transform.position).normalized;
        lookDirection.y = 0; // Не наклоняемся по Y
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookDirection),
                Time.deltaTime * 5f
            );
        }

        // Атакуем
        if (attackTimer <= 0f)
        {
            Attack();
            attackTimer = attackCooldown;
        }

        // Игрок отошёл
        if (distanceToPlayer > attackRange * 1.2f)
        {
            currentState = AIState.Chase;
            agent.isStopped = false;
        }
    }

    void Attack()
    {
        Debug.Log($"[EnemyAI] {gameObject.name} атакует игрока! Урон: {damage}");

        // Запускаем анимацию атаки
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Наносим урон игроку (если есть компонент Health)
        // PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        // if (playerHealth != null)
        // {
        //     playerHealth.TakeDamage(damage);
        // }
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        // Raycast к игроку
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Проверяем есть ли препятствия
        if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out RaycastHit hit, distanceToPlayer))
        {
            if (hit.transform == player)
            {
                return true;
            }
        }

        return false;
    }

    void SetNewPatrolPoint()
    {
        // Случайная точка в радиусе патрулирования
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += startPosition;

        // Находим точку на NavMesh
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            patrolPoint = hit.position;
            agent.SetDestination(patrolPoint);
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        // Скорость движения
        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);

        // Состояние
        animator.SetBool("IsChasing", currentState == AIState.Chase);
        animator.SetBool("IsAttacking", currentState == AIState.Attack);
    }

    // Визуализация в редакторе
    void OnDrawGizmosSelected()
    {
        // Радиус обнаружения
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Радиус атаки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Радиус патрулирования
        Gizmos.color = Color.blue;
        Vector3 startPos = Application.isPlaying ? startPosition : transform.position;
        Gizmos.DrawWireSphere(startPos, patrolRadius);

        // Текущая точка патруля
        if (Application.isPlaying && currentState == AIState.Patrol)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(patrolPoint, 0.5f);
            Gizmos.DrawLine(transform.position, patrolPoint);
        }

        // Линия к игроку
        if (Application.isPlaying && player != null && currentState == AIState.Chase)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}
