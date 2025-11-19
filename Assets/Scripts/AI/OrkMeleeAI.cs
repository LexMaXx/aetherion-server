using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Простое AI для Орка в боевой сцене.
/// Ищет ближайшую цель в радиусе, преследует и наносит урон в ближнем бою.
/// </summary>
[RequireComponent(typeof(TargetableEntity))]
[RequireComponent(typeof(MonsterExperienceReward))]
public class OrkMeleeAI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRange = 25f;
    [SerializeField] private float targetRefreshInterval = 0.4f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 2.6f;
    [SerializeField] private float attackCooldown = 1.8f;
    [SerializeField] private float baseDamage = 40f;

    [Header("Movement Speeds")]
    [SerializeField] private float chaseMoveSpeed = 3.5f;
    [SerializeField] private float patrolMoveSpeed = 1.8f;
    [SerializeField] private float rotationSpeed = 8f;

    [Header("Target Filters")]
    [SerializeField] private bool attackPlayers = true;
    [SerializeField] private bool attackOtherPlayers = true;
    [SerializeField] private bool attackAllies = false;
    [SerializeField] private bool attackNeutral = false;
    [SerializeField] private bool attackEnemies = false;
    [SerializeField] private bool attackOnlyPlayers = true;

    [Header("Home Behavior")]
    [SerializeField] private float homeReturnThreshold = 1.5f;
    [SerializeField] private float homeIdleRotationLerp = 8f;
    [SerializeField] private float maxChaseDistance = 100f;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private bool loopPatrol = true;
    [SerializeField] private float patrolPointTolerance = 1.5f;
    [SerializeField] private float patrolWaitTime = 2f;

    [Header("Grounding")]
    [Tooltip("Использовать StickToGround только если NavMeshAgent отключен")]
    [SerializeField] private bool keepOnGround = false;
    [SerializeField] private float groundCheckOffset = 1.5f;
    [SerializeField] private float groundCheckDistance = 5f;
    [SerializeField] private LayerMask groundLayers = Physics.DefaultRaycastLayers;

    [Header("NavMesh Settings")]
    [SerializeField] private float navMeshSampleRadius = 3f;
    [SerializeField] private float navMeshRetryInterval = 2f;

    [Header("Death & Respawn")]
    [SerializeField] private float respawnDelay = 20f;

    [Header("Debug")]
    [SerializeField] private bool enableLogs = false;
    [Header("FX")]
    [Tooltip("Эффект удара (например, искры). Спавнится при попадании по игроку.")]
    [SerializeField] private GameObject hitEffectPrefab;
    [Header("Audio")]
    [SerializeField] private AudioClip attackSfx;
    [Range(0f, 1f)]
    [SerializeField] private float attackSfxVolume = 0.85f;

    private NavMeshAgent navAgent;
    private Animator animator;
    private TargetableEntity orkEntity;
    private HealthSystem healthSystem;
    private TargetableEntity currentTargetEntity;
    private Transform currentTarget;
    private float nextAttackTime;
    private float nextSearchTime;
    private bool useNavMesh;
    private bool isDead;
    private Coroutine respawnRoutine;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Collider[] cachedColliders;
    private Renderer[] cachedRenderers;
    private bool isReturningHome;
    private float nextNavMeshRetryTime;
    private float currentMoveSpeed;
    private MovementMode currentMovementMode = MovementMode.Patrol;
    private bool isInCombatState;
    private AudioSource audioSource;
    private enum MovementMode
    {
        Patrol,
        Chase
    }
    private int currentPatrolIndex;
    private float nextPatrolMoveTime;

    private static readonly int HashIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int HashInBattle = Animator.StringToHash("InBattle");
    private static readonly int HashAttack = Animator.StringToHash("Attack");
    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashIsDead = Animator.StringToHash("isDead");

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        orkEntity = GetComponent<TargetableEntity>();
        healthSystem = GetComponentInChildren<HealthSystem>();
        cachedColliders = GetComponentsInChildren<Collider>(true);
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = attackSfxVolume;

        if (animator != null && animator.applyRootMotion)
        {
            animator.applyRootMotion = false;
            Log("Animator Root Motion disabled for stability.");
        }
    }

    private void OnEnable()
    {
        if (orkEntity != null)
        {
            orkEntity.OnDeath += OnOrkDeath;
        }
    }

    private void OnDisable()
    {
        if (orkEntity != null)
        {
            orkEntity.OnDeath -= OnOrkDeath;
        }
    }

    private void Start()
    {
        if (navAgent != null)
        {
            // Пытаемся привязаться к NavMesh сразу после спавна
            if (!navAgent.isOnNavMesh)
            {
                TryAttachToNavMesh(true);
            }

            if (navAgent.isOnNavMesh)
            {
                if (!navAgent.enabled) navAgent.enabled = true;
                navAgent.speed = patrolMoveSpeed;
                navAgent.stoppingDistance = Mathf.Max(0.1f, attackRange * 0.75f);
                useNavMesh = true;
            }
            else
            {
                navAgent.enabled = false;
                useNavMesh = false;
            }
        }

        if (animator != null)
        {
            animator.enabled = true;
            SetCombatState(false);
        }

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    private void Update()
    {
        if (isDead)
            return;

        if (orkEntity != null && !orkEntity.IsEntityAlive())
        {
            HandleDeath();
            return;
        }

        if (navAgent != null && Time.time >= nextNavMeshRetryTime && (!useNavMesh || !navAgent.isActiveAndEnabled || !navAgent.isOnNavMesh))
        {
            nextNavMeshRetryTime = Time.time + navMeshRetryInterval;
            TryAttachToNavMesh(false);
        }

        // Сбрасываем цель, если ссылка потеряна (например, игрок умер/респавнился)
        if (!HasValidTarget())
        {
            currentTarget = null;
            currentTargetEntity = null;
        }

        // Пере-поиск цели по таймеру
        if (Time.time >= nextSearchTime)
        {
            nextSearchTime = Time.time + targetRefreshInterval;
            if (currentTarget == null)
            {
                AcquireTarget();
            }
        }

        if (currentTarget != null)
        {
            SetMovementMode(MovementMode.Chase);
            HandleChaseAndAttack();
        }
        else
        {
            SetMovementMode(MovementMode.Patrol);
            ReturnToSpawn();
        }

        // StickToGround ТОЛЬКО если NavMeshAgent НЕ используется или НЕ на NavMesh
        // Иначе NavMeshAgent сам управляет позицией
        if (keepOnGround && (!useNavMesh || navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh))
        {
            StickToGround();
        }
    }

    private void AcquireTarget()
    {
        TargetableEntity[] candidates = FindObjectsOfType<TargetableEntity>();
        float closest = detectionRange;
        TargetableEntity best = null;

        foreach (var entity in candidates)
        {
            if (!IsTargetAllowed(entity))
                continue;

            float distance = Vector3.Distance(transform.position, entity.transform.position);
            if (distance < closest)
            {
                closest = distance;
                best = entity;
            }
        }

        currentTargetEntity = best;
        currentTarget = best != null ? best.transform : null;

        if (best != null)
        {
            Log($"Target acquired: {best.GetEntityName()}");
        }
    }

    private bool IsTargetAllowed(TargetableEntity entity)
    {
        if (entity == null || entity == orkEntity || !entity.IsEntityAlive())
            return false;

        var faction = entity.GetFaction();

        if (attackOnlyPlayers && faction != Faction.Player && faction != Faction.OtherPlayer)
            return false;

        switch (entity.GetFaction())
        {
            case Faction.Player:
                return attackPlayers;
            case Faction.Ally:
                return attackAllies;
            case Faction.OtherPlayer:
                return attackOtherPlayers;
            case Faction.Neutral:
                return attackNeutral;
            case Faction.Enemy:
                return attackEnemies;
            default:
                return true;
        }
    }

    private bool HasValidTarget()
    {
        if (currentTargetEntity == null)
            return false;

        return IsTargetValid(currentTargetEntity);
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

    private void HandleChaseAndAttack()
    {
        if (currentTarget == null)
            return;

        float distance = Vector3.Distance(transform.position, currentTarget.position);
        float distanceFromSpawn = Vector3.Distance(spawnPosition, transform.position);

        if (distanceFromSpawn > maxChaseDistance)
        {
            currentTarget = null;
            currentTargetEntity = null;
            return;
        }

        if (distance > attackRange)
        {
            MoveTowardsTarget();
        }
        else
        {
            StopMovement();
            SetMoving(false);
            RotateTowards(currentTarget.position);

            if (Time.time >= nextAttackTime)
            {
                PerformAttack();
            }
        }
    }

    private void MoveTowardsTarget()
    {
        if (currentTarget == null)
            return;

        MoveTowardsPosition(currentTarget.position);
    }

    private void StopMovement()
    {
        if (useNavMesh && navAgent != null && navAgent.enabled && navAgent.hasPath)
        {
            navAgent.ResetPath();
        }
    }

    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void PerformAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger(HashAttack);
        }

        float damage = baseDamage;

        if (currentTargetEntity != null)
        {
            currentTargetEntity.TakeDamage(damage, orkEntity);
            SpawnHitEffect(currentTargetEntity.transform);
            PlayAttackSound();
        }
        else if (currentTarget != null)
        {
            HealthSystem hp = currentTarget.GetComponent<HealthSystem>();
            if (hp != null)
            {
                hp.TakeDamage(damage);
            }

            SpawnHitEffect(currentTarget);
            PlayAttackSound();
        }

        Log($"Hit {currentTarget?.name ?? "unknown"} for {damage:F0} damage");

        nextAttackTime = Time.time + attackCooldown;
    }

    private void SpawnHitEffect(Transform targetTransform)
    {
        if (hitEffectPrefab == null || targetTransform == null)
            return;

        Vector3 spawnPos = targetTransform.position + Vector3.up * 1.5f;
        GameObject fx = Instantiate(hitEffectPrefab, spawnPos, Quaternion.identity);
        Destroy(fx, 3f);
    }

    private void PlayAttackSound()
    {
        if (audioSource == null || attackSfx == null)
            return;

        audioSource.volume = attackSfxVolume;
        audioSource.PlayOneShot(attackSfx);
    }

    private void SetMoving(bool isMoving)
    {
        if (animator == null)
            return;

        bool current = animator.GetBool(HashIsMoving);
        if (current != isMoving)
        {
            animator.SetBool(HashIsMoving, isMoving);
        }

        if (!isMoving)
        {
            UpdateAnimatorSpeed(0f);
        }
    }

    private void Log(string message)
    {
        if (enableLogs)
        {
            Debug.Log($"[OrkMeleeAI] {message}");
        }
    }

    private void OnOrkDeath(TargetableEntity deadEntity)
    {
        if (deadEntity == orkEntity)
        {
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        if (isDead)
            return;

        isDead = true;

        StopMovement();
        SetMoving(false);
        SetCombatState(false);

        currentTarget = null;
        currentTargetEntity = null;

        if (animator != null)
        {
            animator.SetBool(HashIsDead, true);
        }

        if (navAgent != null && navAgent.enabled)
        {
            if (navAgent.isOnNavMesh)
            {
                navAgent.isStopped = true;
                if (navAgent.hasPath)
                {
                    navAgent.ResetPath();
                }
            }
            else
            {
                navAgent.enabled = false;
            }
        }

        if (animator != null)
        {
            SetCombatState(false);
            animator.SetBool(HashIsMoving, false);
        }

        ToggleColliders(false);

        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
        }

        respawnRoutine = StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }

    private void Respawn()
    {
        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.Warp(spawnPosition);
            navAgent.isStopped = false;
        }

        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        transform.rotation = spawnRotation;

        if (orkEntity != null)
        {
            orkEntity.Revive(1f);
        }
        else if (healthSystem != null)
        {
            healthSystem.SetHealth(healthSystem.MaxHealth);
        }

        ToggleColliders(true);
        ToggleRenderers(true);

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
            SetCombatState(false);
            animator.SetBool(HashIsDead, false);
        }

        isDead = false;
        respawnRoutine = null;
        isReturningHome = false;
        currentMovementMode = MovementMode.Patrol;
        currentMoveSpeed = patrolMoveSpeed;
        if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
        {
            navAgent.speed = currentMoveSpeed;
        }
    }

    private void ToggleColliders(bool enabled)
    {
        if (cachedColliders == null) return;

        foreach (var col in cachedColliders)
        {
            if (col != null)
            {
                col.enabled = enabled;
            }
        }
    }

    private void ToggleRenderers(bool enabled)
    {
        if (cachedRenderers == null) return;

        foreach (var renderer in cachedRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = enabled;
            }
        }
    }

    private void StickToGround()
    {
        Vector3 origin = transform.position + Vector3.up * groundCheckOffset;
        float maxDistance = groundCheckOffset + groundCheckDistance;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            Vector3 pos = transform.position;
            pos.y = hit.point.y;
            transform.position = pos;
        }
    }

    private void ReturnToSpawn()
    {
        if (HasPatrolRoute())
        {
            HandlePatrol();
            return;
        }

        float distanceToHome = Vector3.Distance(transform.position, spawnPosition);

        if (distanceToHome > homeReturnThreshold)
        {
            MoveTowardsPosition(spawnPosition);
            isReturningHome = true;
        }
        else
        {
            if (isReturningHome)
            {
                StopMovement();
                SetMoving(false);
                isReturningHome = false;
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, spawnRotation, homeIdleRotationLerp * Time.deltaTime);
        }
    }

    private void TryAttachToNavMesh(bool initialAttempt)
    {
        if (navAgent == null)
            return;

        if (!navAgent.isActiveAndEnabled)
        {
            navAgent.enabled = true;
        }

        if (navAgent.isOnNavMesh)
        {
            useNavMesh = true;
            return;
        }

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            if (navAgent.Warp(hit.position))
            {
                useNavMesh = true;
                navAgent.speed = currentMoveSpeed;
                transform.position = hit.position;
                return;
            }
        }

        if (initialAttempt)
        {
            Log("⚠️ Не удалось привязать Ork к NavMesh. Перехожу в ручной режим движения.");
        }

        useNavMesh = false;
        navAgent.enabled = false;
    }

    private void MoveTowardsPosition(Vector3 destination)
    {
        RotateTowards(destination);

        if (useNavMesh && navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            if (!navAgent.hasPath || Vector3.Distance(navAgent.destination, destination) > 0.25f)
            {
                navAgent.SetDestination(destination);
            }

            float animSpeed = navAgent.velocity.magnitude;
            if (animSpeed < 0.1f && navAgent.remainingDistance > navAgent.stoppingDistance + 0.05f)
            {
                animSpeed = currentMoveSpeed;
            }
            UpdateAnimatorSpeed(animSpeed);
        }
        else
        {
            Vector3 direction = (destination - transform.position).normalized;
            direction.y = 0f;
            transform.position += direction * currentMoveSpeed * Time.deltaTime;

            UpdateAnimatorSpeed(currentMoveSpeed);
        }

        SetMoving(true);
    }

    private bool HasPatrolRoute()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return false;

        foreach (var point in patrolPoints)
        {
            if (point != null)
                return true;
        }

        return false;
    }

    private Transform GetCurrentPatrolPoint()
    {
        if (!HasPatrolRoute())
            return null;

        int attempts = 0;
        int count = patrolPoints.Length;

        if (currentPatrolIndex >= count)
        {
            currentPatrolIndex = Mathf.Clamp(currentPatrolIndex, 0, count - 1);
        }

        while (attempts < count)
        {
            int index = (currentPatrolIndex + attempts) % count;
            Transform point = patrolPoints[index];
            if (point != null)
            {
                currentPatrolIndex = index;
                return point;
            }
            attempts++;
        }

        return null;
    }

    private void AdvancePatrolIndex()
    {
        if (!HasPatrolRoute())
            return;

        if (loopPatrol)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
        else
        {
            currentPatrolIndex = Mathf.Min(currentPatrolIndex + 1, patrolPoints.Length - 1);
        }

        // Skip null references
        GetCurrentPatrolPoint();
    }

    private void HandlePatrol()
    {
        isReturningHome = false;

        Transform patrolPoint = GetCurrentPatrolPoint();
        if (patrolPoint == null)
        {
            // Fallback to home if no valid points at run-time
            Vector3 direction = spawnPosition - transform.position;
            if (direction.sqrMagnitude > 0.01f)
            {
                MoveTowardsPosition(spawnPosition);
            }
            return;
        }

        float distance = Vector3.Distance(transform.position, patrolPoint.position);
        bool reachedNavPoint = false;
        if (useNavMesh && navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh && navAgent.hasPath && !navAgent.pathPending)
        {
            reachedNavPoint = navAgent.remainingDistance <= Mathf.Max(navAgent.stoppingDistance, patrolPointTolerance);
        }
        else
        {
            reachedNavPoint = distance <= patrolPointTolerance;
        }

        if (reachedNavPoint)
        {
            if (nextPatrolMoveTime <= Time.time)
            {
                nextPatrolMoveTime = Time.time + patrolWaitTime;
                AdvancePatrolIndex();
            }

            StopMovement();
            SetMoving(false);
            return;
        }

        if (Time.time < nextPatrolMoveTime)
        {
            StopMovement();
            SetMoving(false);
            return;
        }

        MoveTowardsPosition(patrolPoint.position);
    }

    private void SetMovementMode(MovementMode mode)
    {
        if (currentMovementMode == mode)
            return;

        currentMovementMode = mode;
        currentMoveSpeed = mode == MovementMode.Chase ? chaseMoveSpeed : patrolMoveSpeed;

        if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
        {
            navAgent.speed = currentMoveSpeed;
        }

        SetCombatState(mode == MovementMode.Chase);
    }

    private void SetCombatState(bool inCombat)
    {
        if (animator == null)
            return;

        if (isInCombatState == inCombat)
            return;

        animator.SetBool(HashInBattle, inCombat);
        isInCombatState = inCombat;
    }

    private void UpdateAnimatorSpeed(float speed)
    {
        if (animator == null)
            return;

        animator.SetFloat(HashSpeed, speed);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
