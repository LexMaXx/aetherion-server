using UnityEngine;

/// <summary>
/// Контроллер персонажа игрока - управление движением и анимацией
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 1.5f; // Было: 3f (-50%)
    [SerializeField] private float runSpeed = 3f; // Было: 6f (-50%)
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float gravity = 30f;  // Увеличена для лучшего контакта с землей

    [Header("Components")]
    private CharacterController characterController;
    private Animator animator;
    private CharacterStats characterStats;
    private StatsFormulas statsFormulas;
    private StarterAssets.StarterAssetsInputs starterInputs; // Starter Assets Input System
    private PlayerDeathHandler deathHandler; // Проверка состояния смерти
    private EffectManager effectManager; // Проверка статус-эффектов (Stun, Root, Sleep)

    [Header("Input")]
    private Vector2 moveInput;
    private bool isRunning = false;

    [Header("Physics")]
    private Vector3 velocity;

    [Header("Animation Parameters")]
    private readonly int moveXHash = Animator.StringToHash("MoveX");
    private readonly int moveYHash = Animator.StringToHash("MoveY");
    private readonly int isMovingHash = Animator.StringToHash("IsMoving");
    private readonly int inBattleHash = Animator.StringToHash("InBattle");

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        starterInputs = GetComponent<StarterAssets.StarterAssetsInputs>();

        // ИСПРАВЛЕНО: Ищем PlayerDeathHandler на текущем объекте И на родителе
        deathHandler = GetComponent<PlayerDeathHandler>();
        if (deathHandler == null)
        {
            deathHandler = GetComponentInParent<PlayerDeathHandler>();
        }

        if (deathHandler != null)
        {
            Debug.Log("[PlayerController] ✅ PlayerDeathHandler найден для проверки смерти");
        }
        else
        {
            Debug.LogWarning("[PlayerController] ⚠️ PlayerDeathHandler не найден! Движение после смерти не будет блокироваться!");
        }

        // КРИТИЧЕСКИ ВАЖНО: Ищем EffectManager для проверки статус-эффектов (Stun, Root, Sleep)
        effectManager = GetComponent<EffectManager>();
        if (effectManager == null)
        {
            effectManager = GetComponentInParent<EffectManager>();
        }

        if (effectManager != null)
        {
            Debug.Log("[PlayerController] ✅ EffectManager найден для проверки статус-эффектов");
        }
        else
        {
            Debug.LogWarning("[PlayerController] ⚠️ EffectManager не найден! Stun/Root эффекты не будут блокировать движение!");
        }

        // Загружаем глобальные формулы
        statsFormulas = Resources.Load<StatsFormulas>("StatsFormulas");
        if (statsFormulas == null)
        {
            Debug.LogWarning("[PlayerController] StatsFormulas не найден в Resources!");
        }

        if (starterInputs != null)
        {
            Debug.Log("[PlayerController] ✅ Starter Assets Input System обнаружен");
        }
    }

    void Start()
    {
        // Ищем CharacterStats (может быть добавлен позже через ArenaManager)
        characterStats = GetComponent<CharacterStats>();

        // НОВАЯ СИСТЕМА: SkillExecutor добавляется в ArenaManager, не нужно здесь
        // SkillManager УДАЛЕН - используется только SkillExecutor

        // Устанавливаем в боевую стойку по умолчанию
        animator.SetBool(inBattleHash, true);

        // Debug информация
        Debug.Log($"[PlayerController] Инициализация");
        Debug.Log($"  Позиция: {transform.position}");
        Debug.Log($"  CharacterController центр: {characterController.center}");
        Debug.Log($"  CharacterController высота: {characterController.height}");

        if (characterStats != null)
        {
            Debug.Log($"  Ловкость (Agility): {characterStats.agility}");
            Debug.Log($"  Бонус скорости: +{GetAgilitySpeedMultiplier() * 100 - 100:F0}%");
        }
        else
        {
            Debug.LogWarning($"[PlayerController] CharacterStats не найден в Start()!");
        }
    }

    void Update()
    {
        // КРИТИЧЕСКИ ВАЖНО: Если персонаж мертв - не обрабатываем ввод и движение
        if (deathHandler != null && deathHandler.IsDead)
        {
            return;
        }

        // КРИТИЧЕСКИ ВАЖНО: Если персонаж под контролем (Stun, Root, Sleep) - блокируем движение
        if (effectManager != null && !effectManager.CanMove())
        {
            // DEBUG лог (показывать каждые 0.5 секунд чтобы не спамить)
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"[PlayerController] 🚫 Движение заблокировано! CanMove()=false для {gameObject.name}");
            }

            // Останавливаем движение
            moveInput = Vector2.zero;
            isRunning = false;

            // Обновляем анимацию (показываем что персонаж стоит)
            HandleAnimation();
            return;
        }

        HandleInput();
        HandleMovement();
        HandleAnimation();
    }

    /// <summary>
    /// Обработка ввода (поддержка Starter Assets + MobileInputManager + WASD параллельно)
    /// КРИТИЧЕСКОЕ: WASD и джойстик работают ОДНОВРЕМЕННО!
    /// </summary>
    private void HandleInput()
    {
        float horizontal = 0f;
        float vertical = 0f;
        bool sprint = false;

        // ИСТОЧНИК 1: Starter Assets Input System (джойстик + клавиатура)
        if (starterInputs != null)
        {
            horizontal += starterInputs.move.x;
            vertical += starterInputs.move.y;
            sprint = starterInputs.sprint;
        }

        // ИСТОЧНИК 2: MobileInputManager (джойстик на экране)
        if (MobileInputManager.Instance != null && MobileInputManager.Instance.IsMobileMode())
        {
            horizontal += MobileInputManager.Instance.GetHorizontal();
            vertical += MobileInputManager.Instance.GetVertical();
            if (MobileInputManager.Instance.GetJoystickMagnitude() > 0.7f)
                sprint = true;
        }

        // ИСТОЧНИК 3: WASD клавиатура (всегда активна параллельно!)
        // Проверяем WASD напрямую чтобы работало даже если Starter Assets не установлен
        if (Input.GetKey(KeyCode.W)) vertical += 1f;
        if (Input.GetKey(KeyCode.S)) vertical -= 1f;
        if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.D)) horizontal += 1f;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            sprint = true;

        // ВАЖНО: Нормализуем если вектор слишком большой (джойстик + WASD одновременно)
        moveInput = new Vector2(horizontal, vertical);
        if (moveInput.magnitude > 1f)
            moveInput.Normalize();

        isRunning = sprint;
    }

    /// <summary>
    /// Обработка движения
    /// </summary>
    private void HandleMovement()
    {
        // Сохраняем текущую Y позицию перед любым движением
        float currentY = transform.position.y;

        // Применяем гравитацию
        if (characterController.isGrounded)
        {
            // ВАЖНО: Достаточно сильное прижимание к земле для стабильного контакта
            velocity.y = -2f;
        }
        else
        {
            velocity.y -= gravity * Time.deltaTime;
        }

        // Горизонтальное движение
        Vector3 moveDirection = Vector3.zero;

        if (moveInput.magnitude >= 0.1f)
        {
            // Получаем направление движения относительно камеры
            moveDirection = GetCameraRelativeMovement();

            // Базовая скорость движения
            float baseSpeed = isRunning ? runSpeed : walkSpeed;

            // Применяем бонус от ловкости
            float speedMultiplier = GetAgilitySpeedMultiplier();
            float currentSpeed = baseSpeed * speedMultiplier;

            // Применяем скорость к направлению
            moveDirection *= currentSpeed;

            // Поворот персонажа в сторону движения
            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // Объединяем горизонтальное движение и вертикальную скорость
        Vector3 finalMovement = moveDirection * Time.deltaTime + new Vector3(0, velocity.y * Time.deltaTime, 0);

        // Перемещаем персонажа (только если контроллер активен)
        if (characterController != null && characterController.enabled)
        {
            characterController.Move(finalMovement);
        }

        // КРИТИЧНО: Отправляем позицию на сервер для синхронизации с другими игроками
        if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
        {
            SocketIOManager.Instance.UpdatePosition(
                transform.position,
                transform.rotation,
                velocity,
                characterController.isGrounded
            );
        }
    }

    /// <summary>
    /// Получить направление движения относительно камеры
    /// </summary>
    private Vector3 GetCameraRelativeMovement()
    {
        // Получаем камеру
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return Vector3.zero;

        // Направление вперед и вправо относительно камеры (только по горизонтали)
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;

        // Убираем вертикальную составляющую
        cameraForward.y = 0;
        cameraRight.y = 0;

        cameraForward.Normalize();
        cameraRight.Normalize();

        // Вычисляем направление движения
        Vector3 moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

        return moveDirection;
    }

    /// <summary>
    /// Обработка анимации
    /// </summary>
    private void HandleAnimation()
    {
        // КРИТИЧЕСКОЕ: НЕ ПЕРЕЗАПИСЫВАЕМ АНИМАЦИЮ АТАКИ!
        // Проверяем PlayerAttackNew - если атакует, не отправляем анимацию движения
        PlayerAttackNew playerAttackNew = GetComponent<PlayerAttackNew>();
        if (playerAttackNew != null && playerAttackNew.IsCurrentlyAttacking)
        {
            // Атака играет - НЕ ОТПРАВЛЯЕМ движение, чтобы не перезаписать!
            // Debug.Log("[PlayerController] ⚔️ Атака играет, пропускаем синхронизацию движения");
            return;
        }

        // ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА: Старая система (PlayerAttack)
        PlayerAttack playerAttack = GetComponent<PlayerAttack>();
        if (playerAttack != null)
        {
            // Проверяем через Animator state
            if (animator != null)
            {
                AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
                // Если играет атака и анимация еще не завершена
                if ((currentState.IsName("Attack") || currentState.IsName("Base Layer.Attack")) &&
                    currentState.normalizedTime < 0.95f)
                {
                    // Debug.Log("[PlayerController] ⚔️ Атака играет (старая система), пропускаем синхронизацию");
                    return;
                }
            }
        }

        // Проверяем, двигается ли персонаж
        bool moving = moveInput.magnitude > 0.1f;
        animator.SetBool(isMovingHash, moving);

        if (moving)
        {
            // MoveX всегда 0 (не используем стрейф)
            animator.SetFloat(moveXHash, 0);

            // MoveY: 0.5 = Slow Run (ходьба), 1.0 = Running (бег с Shift)
            float moveYValue = isRunning ? 1.0f : 0.5f;
            animator.SetFloat(moveYHash, moveYValue, 0.1f, Time.deltaTime);

            // Управление скоростью анимации
            // Ходьба (Slow Run) = 0.5x скорости анимации (замедленная)
            // Бег (Sprint) = 1.0x скорости анимации (нормальная)
            animator.speed = isRunning ? 1.0f : 0.5f;
        }
        else
        {
            // Idle
            animator.SetFloat(moveXHash, 0);
            animator.SetFloat(moveYHash, 0, 0.1f, Time.deltaTime);
            animator.speed = 1.0f; // Нормальная скорость для Idle
        }

        // КРИТИЧНО: Отправляем текущую анимацию на сервер
        // (только если не атакуем!)
        if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
        {
            string currentAnimation = moving ? (isRunning ? "Running" : "Walking") : "Idle";
            SocketIOManager.Instance.UpdateAnimation(currentAnimation, animator.speed);
        }
    }

    /// <summary>
    /// Переключить боевую стойку
    /// </summary>
    public void SetBattleStance(bool inBattle)
    {
        animator.SetBool(inBattleHash, inBattle);
    }

    /// <summary>
    /// Получить скорость персонажа (с учетом ловкости)
    /// </summary>
    public float GetCurrentSpeed()
    {
        float baseSpeed = isRunning ? runSpeed : walkSpeed;
        return baseSpeed * GetAgilitySpeedMultiplier();
    }

    /// <summary>
    /// Вычислить множитель скорости от ловкости используя StatsFormulas
    /// Формула: 1.0 + (agility * agilitySpeedBonus)
    /// Например: Agility 10 и bonus 0.05 = 1.0 + (10 * 0.05) = 1.5x (т.е. +50% скорости)
    /// </summary>
    private float GetAgilitySpeedMultiplier()
    {
        // Ищем CharacterStats если его нет (lazy initialization)
        if (characterStats == null)
        {
            characterStats = GetComponent<CharacterStats>();
        }

        if (characterStats == null)
        {
            // Логируем только раз в 2 секунды чтобы не спамить
            if (Time.frameCount % 120 == 0)
            {
                Debug.LogWarning("[PlayerController] GetAgilitySpeedMultiplier: characterStats == NULL! Используем множитель 1.0");
            }
            return 1.0f;
        }

        if (statsFormulas == null)
        {
            if (Time.frameCount % 120 == 0)
            {
                Debug.LogWarning("[PlayerController] GetAgilitySpeedMultiplier: statsFormulas == NULL! Используем множитель 1.0");
            }
            return 1.0f;
        }

        // Используем формулу из StatsFormulas
        return statsFormulas.CalculateSpeedMultiplier(characterStats.agility);
    }
}
