using UnityEngine;

/// <summary>
/// Контроллер персонажа игрока - управление движением и анимацией
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float gravity = 30f;  // Увеличена для лучшего контакта с землей

    [Header("Components")]
    private CharacterController characterController;
    private Animator animator;
    private CharacterStats characterStats;
    private StatsFormulas statsFormulas;

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

        // Загружаем глобальные формулы
        statsFormulas = Resources.Load<StatsFormulas>("StatsFormulas");
        if (statsFormulas == null)
        {
            Debug.LogWarning("[PlayerController] StatsFormulas не найден в Resources!");
        }
    }

    void Start()
    {
        // Ищем CharacterStats (может быть добавлен позже через ArenaManager)
        characterStats = GetComponent<CharacterStats>();

        // КРИТИЧЕСКОЕ: Добавляем SkillManager если его нет
        SkillManager skillManager = GetComponent<SkillManager>();
        if (skillManager == null)
        {
            skillManager = gameObject.AddComponent<SkillManager>();
            Debug.Log("[PlayerController] ✅ SkillManager автоматически добавлен к игроку");
        }

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
        HandleInput();
        HandleMovement();
        HandleAnimation();
    }

    /// <summary>
    /// Обработка ввода
    /// </summary>
    private void HandleInput()
    {
        // WASD или стрелки для движения
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D или стрелки влево/вправо
        float vertical = Input.GetAxisRaw("Vertical");     // W/S или стрелки вверх/вниз

        moveInput = new Vector2(horizontal, vertical);

        // Shift для бега (пока не используется, но готово для будущего)
        isRunning = Input.GetKey(KeyCode.LeftShift);
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
