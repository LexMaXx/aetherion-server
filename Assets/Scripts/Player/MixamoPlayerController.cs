using UnityEngine;

/// <summary>
/// Player Controller для Mixamo персонажей
/// Простая логика движения из EasyStart + наши Mixamo анимации
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class MixamoPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Скорость обычной ходьбы (медленный бег - Slow Run)")]
    public float slowRunSpeed = 1.5f; // Было: 3.0f (-50%)

    [Tooltip("Скорость быстрого бега (Sprint на Shift)")]
    public float runSpeed = 3.0f; // Было: 6.0f (-50%)

    [Tooltip("Скорость поворота персонажа (градусы/сек)")]
    public float rotationSpeed = 540f;

    [Tooltip("Сила гравитации")]
    public float gravity = 9.8f;

    [Header("Animation")]
    [Tooltip("Минимальная скорость для активации анимации")]
    public float minimumSpeed = 0.5f;

    [Tooltip("Скорость анимации при медленном беге (0.5-1.0)")]
    [Range(0.3f, 1.5f)]
    public float slowRunAnimationSpeed = 0.7f;

    [Tooltip("Скорость анимации при быстром беге (обычно 1.0)")]
    [Range(0.5f, 2.0f)]
    public float fastRunAnimationSpeed = 1.0f;

    // Components
    private CharacterController characterController;
    private Animator animator;
    private PlayerAttack playerAttack;

    // Input
    private float inputHorizontal;
    private float inputVertical;
    private bool inputSprint;

    // Animation parameter hashes (наши Mixamo параметры)
    private readonly int moveXHash = Animator.StringToHash("MoveX");
    private readonly int moveYHash = Animator.StringToHash("MoveY");
    private readonly int isMovingHash = Animator.StringToHash("IsMoving");
    private readonly int inBattleHash = Animator.StringToHash("InBattle");

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerAttack = GetComponent<PlayerAttack>();

        if (animator == null)
        {
            Debug.LogWarning("[MixamoPlayerController] Animator не найден!");
        }
        else
        {
            // Устанавливаем боевую стойку по умолчанию
            animator.SetBool(inBattleHash, true);
        }
    }

    void Update()
    {
        HandleInput();
        HandleAnimation();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    /// <summary>
    /// Обработка ввода
    /// </summary>
    private void HandleInput()
    {
        // ВАЖНО: Если атакуем - игнорируем весь input движения
        if (playerAttack != null && playerAttack.IsAttacking())
        {
            inputHorizontal = 0;
            inputVertical = 0;
            inputSprint = false;
            return;
        }

        inputHorizontal = Input.GetAxis("Horizontal");
        inputVertical = Input.GetAxis("Vertical");
        inputSprint = Input.GetKey(KeyCode.LeftShift);
    }

    /// <summary>
    /// Обработка движения (упрощенная версия из EasyStart)
    /// </summary>
    private void HandleMovement()
    {
        // ВАЖНО: Если атакуем - НЕ двигаемся
        if (playerAttack != null && playerAttack.IsAttacking())
        {
            // Только гравитация (если контроллер активен)
            if (characterController != null && characterController.enabled)
            {
                Vector3 gravityOnly = Vector3.up * (-gravity * Time.deltaTime);
                characterController.Move(gravityOnly);
            }
            return;
        }

        // Определяем текущую скорость (быстрый бег с Shift или медленный бег без)
        float currentSpeed = inputSprint ? runSpeed : slowRunSpeed;

        // Направление движения
        float directionX = inputHorizontal * currentSpeed * Time.deltaTime;
        float directionZ = inputVertical * currentSpeed * Time.deltaTime;
        float directionY = -gravity * Time.deltaTime; // Гравитация

        // --- Поворот персонажа относительно камеры ---
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // ВАЖНО: Если атакуем - НЕ вращаем персонажа (он уже повёрнут к врагу)
        if (playerAttack != null && playerAttack.IsAttacking())
        {
            // Персонаж заморожен в текущем повороте (смотрит на врага)
            // Ничего не делаем - rotation остаётся как есть
        }
        // Персонаж всегда поворачивается лицом в направлении движения (360°)
        // Работает для W/A/S/D в любом направлении
        else if (inputHorizontal != 0 || inputVertical != 0)
        {
            // Вычисляем целевое направление относительно камеры
            Vector3 targetDirection = (forward * inputVertical + right * inputHorizontal).normalized;

            if (targetDirection.magnitude > 0.1f)
            {
                // Вычисляем целевой угол
                float targetAngle = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;

                // Плавный поворот к целевому углу
                float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetAngle, rotationSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0, angle, 0);
            }
        }

        // Вычисляем направление движения относительно камеры
        Vector3 moveDirection = (forward * inputVertical + right * inputHorizontal).normalized;

        // Применяем движение (только если контроллер активен)
        Vector3 verticalDirection = Vector3.up * directionY;
        Vector3 horizontalDirection = moveDirection * currentSpeed * Time.deltaTime;
        Vector3 movement = verticalDirection + horizontalDirection;

        if (characterController != null && characterController.enabled)
        {
            characterController.Move(movement);
        }
    }

    /// <summary>
    /// Обработка анимации (наши Mixamo Blend Tree параметры)
    /// </summary>
    private void HandleAnimation()
    {
        if (animator == null)
            return;

        // MoveX всегда 0 (не используем страф-анимации)
        animator.SetFloat(moveXHash, 0);

        // Проверяем есть ли вообще движение (любое направление)
        bool isMoving = Mathf.Abs(inputVertical) > 0.1f || Mathf.Abs(inputHorizontal) > 0.1f;
        animator.SetBool(isMovingHash, isMoving);

        if (isMoving)
        {
            // MoveY: 0.5 = Slow Run, 1.0 = Running
            // Используем одинаковые анимации для движения вперед и назад
            float moveYValue = inputSprint ? 1.0f : 0.5f;
            animator.SetFloat(moveYHash, moveYValue, 0.1f, Time.deltaTime);

            // Изменяем скорость воспроизведения анимации для реалистичности
            // Медленный бег = замедленная анимация, быстрый бег = нормальная скорость
            animator.speed = inputSprint ? fastRunAnimationSpeed : slowRunAnimationSpeed;
        }
        else
        {
            // Idle - нормальная скорость анимации
            animator.SetFloat(moveYHash, 0, 0.1f, Time.deltaTime);
            animator.speed = 1.0f;
        }
    }

    /// <summary>
    /// Переключить боевую стойку
    /// </summary>
    public void SetBattleStance(bool inBattle)
    {
        if (animator != null)
        {
            animator.SetBool(inBattleHash, inBattle);
        }
    }

    /// <summary>
    /// Получить текущую скорость
    /// </summary>
    public float GetCurrentSpeed()
    {
        return characterController.velocity.magnitude;
    }
}
