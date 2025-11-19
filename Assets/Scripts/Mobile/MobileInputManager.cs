using UnityEngine;

/// <summary>
/// Централизованный менеджер мобильных контролов
/// Координирует VirtualJoystick, AttackButton, TouchCameraController
/// Предоставляет unified API для PlayerController
/// </summary>
public class MobileInputManager : MonoBehaviour
{
    public static MobileInputManager Instance { get; private set; }

    [Header("Mobile Controls")]
    [SerializeField] private VirtualJoystick virtualJoystick;
    [SerializeField] private AttackButton attackButton;
    [SerializeField] private TouchCameraController touchCamera;
    [Header("Mount Control")]
    [SerializeField] private PlayerMountHandler defaultMountHandler;
    [SerializeField] private MobileHoldButton ascendButton;
    [SerializeField] private MobileHoldButton descendButton;

    [Header("Mobile UI")]
    [SerializeField] private GameObject mobileControlsUI; // Canvas с мобильными контролами

    [Header("Settings")]
    [SerializeField] private bool forceMobileMode = false; // Принудительно включить мобильный режим (для тестов)

    // State
    private bool isMobileDevice = false;
    private PlayerMountHandler activeMountHandler;
    private bool manualAscendHeld;
    private bool manualDescendHeld;

    // Events
    public System.Action OnAttackButtonPressed;
    public System.Action OnAttackButtonReleased;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Определяем мобильное ли устройство
        DetectMobileDevice();

        // Показываем/скрываем мобильные контролы
        if (mobileControlsUI != null)
        {
            mobileControlsUI.SetActive(isMobileDevice);
        }
    }

    void Start()
    {
        // Подписываемся на события кнопки атаки
        if (attackButton != null)
        {
            attackButton.OnAttackPressed += HandleAttackPressed;
            attackButton.OnAttackReleased += HandleAttackReleased;
        }

        Debug.Log($"[MobileInputManager] Инициализирован. Мобильный режим: {isMobileDevice}");

        if (defaultMountHandler != null)
        {
            activeMountHandler = defaultMountHandler;
        }
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (attackButton != null)
        {
            attackButton.OnAttackPressed -= HandleAttackPressed;
            attackButton.OnAttackReleased -= HandleAttackReleased;
        }
    }

    /// <summary>
    /// Определить мобильное ли устройство
    /// </summary>
    private void DetectMobileDevice()
    {
        isMobileDevice = forceMobileMode;

#if UNITY_ANDROID || UNITY_IOS
        isMobileDevice = true;
#endif

        // Дополнительная проверка для редактора
#if UNITY_EDITOR
        if (forceMobileMode)
        {
            isMobileDevice = true;
        }
#endif

        Debug.Log($"[MobileInputManager] Мобильное устройство: {isMobileDevice} (Force: {forceMobileMode})");
    }

    /// <summary>
    /// Проверить является ли устройство мобильным
    /// </summary>
    public bool IsMobileDevice()
    {
        return isMobileDevice;
    }

    /// <summary>
    /// Обработчик нажатия кнопки атаки
    /// </summary>
    private void HandleAttackPressed()
    {
        Debug.Log("[MobileInputManager] Кнопка атаки нажата");
        OnAttackButtonPressed?.Invoke();
    }

    /// <summary>
    /// Обработчик отпускания кнопки атаки
    /// </summary>
    private void HandleAttackReleased()
    {
        Debug.Log("[MobileInputManager] Кнопка атаки отпущена");
        OnAttackButtonReleased?.Invoke();
    }

    // ==================== PUBLIC API ====================

    /// <summary>
    /// Проверить работает ли игра в мобильном режиме
    /// </summary>
    public bool IsMobileMode()
    {
        return isMobileDevice;
    }

    /// <summary>
    /// Получить направление движения от виртуального джойстика
    /// </summary>
    public Vector2 GetMovementInput()
    {
        if (!isMobileDevice || virtualJoystick == null)
        {
            return Vector2.zero;
        }

        return virtualJoystick.GetInputDirection();
    }

    /// <summary>
    /// Получить горизонтальный input
    /// </summary>
    public float GetHorizontal()
    {
        if (!isMobileDevice || virtualJoystick == null)
        {
            return Input.GetAxis("Horizontal"); // Fallback на клавиатуру
        }

        return virtualJoystick.Horizontal;
    }

    /// <summary>
    /// Получить вертикальный input
    /// </summary>
    public float GetVertical()
    {
        if (!isMobileDevice || virtualJoystick == null)
        {
            return Input.GetAxis("Vertical"); // Fallback на клавиатуру
        }

        return virtualJoystick.Vertical;
    }

    /// <summary>
    /// Проверить нажата ли кнопка атаки
    /// </summary>
    public bool IsAttackButtonPressed()
    {
        if (!isMobileDevice || attackButton == null)
        {
            return Input.GetMouseButton(0); // Fallback на мышь
        }

        return attackButton.IsPressed;
    }

    /// <summary>
    /// Проверить доступна ли кнопка атаки (не на cooldown)
    /// </summary>
    public bool IsAttackButtonAvailable()
    {
        if (!isMobileDevice || attackButton == null)
        {
            return true;
        }

        return attackButton.IsAvailable;
    }

    /// <summary>
    /// Запустить cooldown на кнопке атаки
    /// </summary>
    public void StartAttackCooldown(float duration)
    {
        if (attackButton != null)
        {
            attackButton.StartCooldown(duration);
        }
    }

    /// <summary>
    /// Сбросить cooldown кнопки атаки
    /// </summary>
    public void ResetAttackCooldown()
    {
        if (attackButton != null)
        {
            attackButton.ResetCooldown();
        }
    }

    /// <summary>
    /// Зарегистрировать актуальный PlayerMountHandler (вызывается после спавна игрока).
    /// </summary>
    public void SetMountHandler(PlayerMountHandler handler)
    {
        activeMountHandler = handler;
    }

    public void ClearMountHandler(PlayerMountHandler handler)
    {
        if (handler != null && activeMountHandler == handler)
        {
            activeMountHandler = defaultMountHandler;
        }
    }

    /// <summary>
    /// Вызвать переключение маунта (используется мобильной кнопкой Mount).
    /// </summary>
    public void ToggleMount()
    {
        if (activeMountHandler != null)
        {
            activeMountHandler.ToggleMount();
        }
        else
        {
            Debug.LogWarning("[MobileInputManager] MountHandler не назначен. Нажатие кнопки Mount проигнорировано.");
        }
    }

    public void AscendButtonDown() => manualAscendHeld = true;

    public void AscendButtonUp() => manualAscendHeld = false;

    public void DescendButtonDown() => manualDescendHeld = true;

    public void DescendButtonUp() => manualDescendHeld = false;

    /// <summary>
    /// Установить цель для камеры
    /// </summary>
    public void SetCameraTarget(Transform target)
    {
        if (touchCamera != null)
        {
            touchCamera.SetTarget(target);
        }
    }

    /// <summary>
    /// Показать/скрыть мобильные контролы
    /// </summary>
    public void SetMobileControlsVisible(bool visible)
    {
        if (mobileControlsUI != null)
        {
            mobileControlsUI.SetActive(visible);
        }
    }

    /// <summary>
    /// Включить/выключить виртуальный джойстик
    /// </summary>
    public void SetJoystickEnabled(bool enabled)
    {
        if (virtualJoystick != null)
        {
            virtualJoystick.enabled = enabled;
        }
    }

    /// <summary>
    /// Включить/выключить кнопку атаки
    /// </summary>
    public void SetAttackButtonEnabled(bool enabled)
    {
        if (attackButton != null)
        {
            attackButton.enabled = enabled;
        }
    }

    /// <summary>
    /// Удерживается ли кнопка подъема (маунт вверх).
    /// </summary>
    public bool IsFlightAscendHeld()
    {
        bool buttonHeld = ascendButton != null && ascendButton.IsPressed;
        return manualAscendHeld || buttonHeld;
    }

    /// <summary>
    /// Удерживается ли кнопка снижения (маунт вниз).
    /// </summary>
    public bool IsFlightDescendHeld()
    {
        bool buttonHeld = descendButton != null && descendButton.IsPressed;
        return manualDescendHeld || buttonHeld;
    }

    // ==================== UTILITY ====================

    /// <summary>
    /// Получить magnitude джойстика (сила нажатия)
    /// </summary>
    public float GetJoystickMagnitude()
    {
        if (!isMobileDevice || virtualJoystick == null)
        {
            return 0f;
        }

        return virtualJoystick.GetMagnitude();
    }

    /// <summary>
    /// Проверить нажат ли джойстик
    /// </summary>
    public bool IsJoystickPressed()
    {
        if (!isMobileDevice || virtualJoystick == null)
        {
            return false;
        }

        return virtualJoystick.IsPressed;
    }

    /// <summary>
    /// Debug info
    /// </summary>
    public string GetDebugInfo()
    {
        return $"Mobile: {isMobileDevice}\n" +
               $"Joystick: {GetMovementInput()}\n" +
               $"Attack: {IsAttackButtonPressed()}\n" +
               $"Attack Available: {IsAttackButtonAvailable()}";
    }
}
