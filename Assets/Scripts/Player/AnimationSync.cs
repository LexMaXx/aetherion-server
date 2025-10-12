using UnityEngine;

/// <summary>
/// Синхронизация скорости анимации с реальной скоростью движения персонажа
/// Предотвращает "скольжение ног" (foot sliding)
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class AnimationSync : MonoBehaviour
{
    [Header("Animation Speed Settings")]
    [Tooltip("Базовая скорость анимации ходьбы")]
    [SerializeField] private float baseWalkAnimationSpeed = 1f;

    [Tooltip("Базовая скорость анимации бега")]
    [SerializeField] private float baseRunAnimationSpeed = 1f;

    [Header("Movement Speed Reference")]
    [Tooltip("Скорость движения при ходьбе (должна совпадать с PlayerController)")]
    [SerializeField] private float walkSpeed = 3f;

    [Tooltip("Скорость движения при беге (должна совпадать с PlayerController)")]
    [SerializeField] private float runSpeed = 6f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private CharacterController characterController;
    private Animator animator;
    private Vector3 lastPosition;
    private float currentSpeed;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        lastPosition = transform.position;
    }

    void Update()
    {
        CalculateCurrentSpeed();
        SyncAnimationSpeed();
    }

    /// <summary>
    /// Вычислить текущую скорость движения персонажа
    /// </summary>
    private void CalculateCurrentSpeed()
    {
        // Вычисляем реальную скорость движения (только горизонтальная составляющая)
        Vector3 currentPosition = transform.position;
        Vector3 deltaPosition = currentPosition - lastPosition;
        deltaPosition.y = 0; // Игнорируем вертикальное движение

        currentSpeed = deltaPosition.magnitude / Time.deltaTime;
        lastPosition = currentPosition;
    }

    /// <summary>
    /// Синхронизировать скорость анимации с реальной скоростью движения
    /// </summary>
    private void SyncAnimationSpeed()
    {
        if (animator == null)
            return;

        // Определяем бежит персонаж или идет (по скорости)
        bool isRunning = currentSpeed > (walkSpeed + runSpeed) / 2f;

        float targetSpeed;
        float baseSpeed;

        if (isRunning)
        {
            // Бег
            baseSpeed = runSpeed;
            targetSpeed = baseRunAnimationSpeed;
        }
        else
        {
            // Ходьба
            baseSpeed = walkSpeed;
            targetSpeed = baseWalkAnimationSpeed;
        }

        // Вычисляем коэффициент синхронизации
        // Если персонаж движется медленнее чем baseSpeed, замедляем анимацию
        // Если быстрее - ускоряем
        float speedRatio = currentSpeed / baseSpeed;

        // Применяем коэффициент к базовой скорости анимации
        float finalAnimationSpeed = targetSpeed * speedRatio;

        // Ограничиваем скорость анимации разумными пределами
        finalAnimationSpeed = Mathf.Clamp(finalAnimationSpeed, 0.1f, 2f);

        // Устанавливаем скорость анимации
        animator.speed = finalAnimationSpeed;

        // Debug информация
        if (showDebugInfo && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[AnimSync] Speed: {currentSpeed:F2} m/s, AnimSpeed: {finalAnimationSpeed:F2}x, Mode: {(isRunning ? "RUN" : "WALK")}");
        }
    }

    /// <summary>
    /// Установить базовую скорость анимации ходьбы
    /// </summary>
    public void SetWalkAnimationSpeed(float speed)
    {
        baseWalkAnimationSpeed = Mathf.Clamp(speed, 0.1f, 3f);
    }

    /// <summary>
    /// Установить базовую скорость анимации бега
    /// </summary>
    public void SetRunAnimationSpeed(float speed)
    {
        baseRunAnimationSpeed = Mathf.Clamp(speed, 0.1f, 3f);
    }
}
