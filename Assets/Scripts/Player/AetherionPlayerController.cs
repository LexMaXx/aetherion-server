using UnityEngine;

/// <summary>
/// Aetherion Player Controller - гибрид EasyStart и нашей системы
/// Поддерживает ходьбу, бег (sprint), боевую стойку
/// Адаптирован под анимации Mixamo
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class AetherionPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Скорость обычной ходьбы")]
    [SerializeField] private float walkSpeed = 1.5f; // Было: 3f (-50%)

    [Tooltip("Скорость бега (Sprint на Shift)")]
    [SerializeField] private float runSpeed = 3f; // Было: 6f (-50%)

    [Tooltip("Скорость поворота персонажа")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Physics")]
    [Tooltip("Сила гравитации")]
    [SerializeField] private float gravity = 30f;

    [Header("Animation")]
    [Tooltip("Минимальная скорость для активации анимации бега")]
    [SerializeField] private float minimumRunSpeed = 0.5f;

    // Components
    private CharacterController characterController;
    private Animator animator;
    private StarterAssets.StarterAssetsInputs starterInputs; // Starter Assets Input System

    // Input
    private float inputHorizontal;
    private float inputVertical;
    private bool inputSprint;

    // States
    private bool isSprinting = false;
    private Vector3 velocity;

    // Animation parameter hashes
    private readonly int moveXHash = Animator.StringToHash("MoveX");
    private readonly int moveYHash = Animator.StringToHash("MoveY");
    private readonly int isMovingHash = Animator.StringToHash("IsMoving");
    private readonly int inBattleHash = Animator.StringToHash("InBattle");
    private readonly int speedHash = Animator.StringToHash("Speed"); // Для совместимости с EasyStart

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        starterInputs = GetComponent<StarterAssets.StarterAssetsInputs>();

        if (animator == null)
        {
            Debug.LogWarning("[AetherionPlayerController] Animator не найден! Анимации не будут работать.");
        }

        if (starterInputs != null)
        {
            Debug.Log("[AetherionPlayerController] ✅ Starter Assets Input System обнаружен");
        }
    }

    void Start()
    {
        // Устанавливаем боевую стойку по умолчанию
        if (animator != null)
        {
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
    /// Обработка ввода (поддержка мобильных контролов + Starter Assets)
    /// </summary>
    private void HandleInput()
    {
        // ПРИОРИТЕТ 1: Starter Assets Input System (работает на всех платформах)
        if (starterInputs != null)
        {
            inputHorizontal = starterInputs.move.x;
            inputVertical = starterInputs.move.y;
            inputSprint = starterInputs.sprint;
        }
        // ПРИОРИТЕТ 2: MobileInputManager (fallback для мобильных)
        else if (MobileInputManager.Instance != null && MobileInputManager.Instance.IsMobileMode())
        {
            // Используем виртуальный джойстик
            inputHorizontal = MobileInputManager.Instance.GetHorizontal();
            inputVertical = MobileInputManager.Instance.GetVertical();

            // На мобильных спринт всегда активен при движении джойстика
            // Можно добавить отдельную кнопку спринта если нужно
            inputSprint = MobileInputManager.Instance.GetJoystickMagnitude() > 0.7f;
        }
        // ПРИОРИТЕТ 3: Старый Input Manager (fallback для клавиатуры)
        else
        {
            inputHorizontal = Input.GetAxis("Horizontal");
            inputVertical = Input.GetAxis("Vertical");
            inputSprint = Input.GetKey(KeyCode.LeftShift);
        }
    }

    /// <summary>
    /// Обработка движения
    /// </summary>
    private void HandleMovement()
    {
        // Гравитация
        if (characterController.isGrounded)
        {
            velocity.y = -2f; // Прижимание к земле
        }
        else
        {
            velocity.y -= gravity * Time.deltaTime;
        }

        // Определяем текущую скорость (спринт или ходьба)
        float currentSpeed = inputSprint ? runSpeed : walkSpeed;
        isSprinting = inputSprint && (Mathf.Abs(inputHorizontal) > 0.1f || Mathf.Abs(inputVertical) > 0.1f);

        // Вычисляем направление движения относительно камеры
        Vector3 moveDirection = Vector3.zero;

        if (Mathf.Abs(inputHorizontal) > 0.1f || Mathf.Abs(inputVertical) > 0.1f)
        {
            // Получаем направление камеры
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 cameraForward = mainCamera.transform.forward;
                Vector3 cameraRight = mainCamera.transform.right;

                // Убираем вертикальную составляющую
                cameraForward.y = 0;
                cameraRight.y = 0;
                cameraForward.Normalize();
                cameraRight.Normalize();

                // Вычисляем направление движения
                moveDirection = (cameraForward * inputVertical + cameraRight * inputHorizontal).normalized;
                moveDirection *= currentSpeed;

                // Поворот персонажа в сторону движения
                if (moveDirection.magnitude > 0.1f)
                {
                    float angle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                    Quaternion rotation = Quaternion.Euler(0, angle, 0);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
                }
            }
        }

        // Объединяем горизонтальное движение и вертикальную скорость
        Vector3 finalMovement = moveDirection * Time.deltaTime + new Vector3(0, velocity.y * Time.deltaTime, 0);

        // Перемещаем персонажа
        characterController.Move(finalMovement);
    }

    /// <summary>
    /// Обработка анимации
    /// </summary>
    private void HandleAnimation()
    {
        if (animator == null)
            return;

        // Для Blend Tree (наши текущие анимации)
        animator.SetFloat(moveXHash, inputHorizontal);
        animator.SetFloat(moveYHash, inputVertical);

        // Проверяем, двигается ли персонаж
        bool isMoving = characterController.velocity.magnitude > minimumRunSpeed;
        animator.SetBool(isMovingHash, isMoving);

        // Для совместимости с EasyStart анимациями
        // animator.SetBool("run", isMoving);
        // animator.SetBool("sprint", isSprinting);

        // Скорость для плавного blend'а
        animator.SetFloat(speedHash, characterController.velocity.magnitude);
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
    /// Получить текущую скорость персонажа
    /// </summary>
    public float GetCurrentSpeed()
    {
        return characterController.velocity.magnitude;
    }

    /// <summary>
    /// Бежит ли персонаж (sprint)
    /// </summary>
    public bool IsSprinting()
    {
        return isSprinting;
    }
}
