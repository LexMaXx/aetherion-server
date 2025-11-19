using UnityEngine;

/// <summary>
/// Player Controller на основе EasyStart Third Person Controller
/// Полностью использует их анимации и систему управления
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class EasyStartPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Скорость ходьбы")]
    public float velocity = 2.5f; // Было: 5f (-50%)

    [Tooltip("Дополнительная скорость при спринте")]
    public float sprintAddition = 1.75f; // Было: 3.5f (-50%)

    [Tooltip("Сила гравитации")]
    public float gravity = 9.8f;

    [Header("Animation")]
    [Tooltip("Минимальная скорость для активации анимации бега")]
    public float minimumSpeed = 0.9f;

    // Components
    private CharacterController characterController;
    private Animator animator;

    // Input
    private float inputHorizontal;
    private float inputVertical;
    private bool inputSprint;

    // States
    private bool isSprinting = false;

    // Animation parameter hashes (EasyStart система)
    private readonly int runHash = Animator.StringToHash("run");
    private readonly int sprintHash = Animator.StringToHash("sprint");
    private readonly int airHash = Animator.StringToHash("air");
    private readonly int crouchHash = Animator.StringToHash("crouch");

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogWarning("[EasyStartPlayerController] Animator не найден! Анимации не будут работать.");
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
        inputHorizontal = Input.GetAxis("Horizontal");
        inputVertical = Input.GetAxis("Vertical");
        inputSprint = Input.GetKey(KeyCode.LeftShift);
    }

    /// <summary>
    /// Обработка движения (из EasyStart)
    /// </summary>
    private void HandleMovement()
    {
        // Спринт добавляет скорость
        float velocityAddition = 0;
        if (isSprinting)
        {
            velocityAddition = sprintAddition;
        }

        // Направление движения
        float directionX = inputHorizontal * (velocity + velocityAddition) * Time.deltaTime;
        float directionZ = inputVertical * (velocity + velocityAddition) * Time.deltaTime;
        float directionY = 0;

        // Добавляем гравитацию
        directionY = directionY - gravity * Time.deltaTime;

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

        // Связываем направление вперед с Z (глубина) и вправо с X (боковое движение)
        forward = forward * directionZ;
        right = right * directionX;

        // Поворачиваем персонажа
        if (directionX != 0 || directionZ != 0)
        {
            float angle = Mathf.Atan2(forward.x + right.x, forward.z + right.z) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 0.15f);
        }

        // --- Конец поворота ---

        Vector3 verticalDirection = Vector3.up * directionY;
        Vector3 horizontalDirection = forward + right;

        Vector3 movement = verticalDirection + horizontalDirection;
        characterController.Move(movement);
    }

    /// <summary>
    /// Обработка анимации (EasyStart система)
    /// </summary>
    private void HandleAnimation()
    {
        if (animator == null)
            return;

        // Анимация бега и спринта
        if (characterController.isGrounded)
        {
            // Run - персонаж двигается
            bool isRunning = characterController.velocity.magnitude > minimumSpeed;
            animator.SetBool(runHash, isRunning);

            // Sprint - персонаж бежит на Shift
            isSprinting = characterController.velocity.magnitude > minimumSpeed && inputSprint;
            animator.SetBool(sprintHash, isSprinting);
        }

        // Air - персонаж в воздухе
        animator.SetBool(airHash, !characterController.isGrounded);
    }

    /// <summary>
    /// Получить текущую скорость
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
