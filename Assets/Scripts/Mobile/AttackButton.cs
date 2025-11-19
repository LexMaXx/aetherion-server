using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Кнопка атаки для мобильных устройств
/// Поддерживает визуальную обратную связь и cooldown индикатор
/// </summary>
public class AttackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Button Settings")]
    [SerializeField] private bool enableCooldownVisual = true;
    [SerializeField] private float cooldownDuration = 0.5f; // Базовый cooldown между атаками

    [Header("Visual Feedback")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private Image cooldownOverlay; // Затемнение во время cooldown
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("Haptic Feedback")]
    [SerializeField] private bool enableHapticFeedback = true;

    // State
    private bool isPressed = false;
    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;

    // Events
    public System.Action OnAttackPressed;
    public System.Action OnAttackReleased;

    void Start()
    {
        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }

        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = 0f;
        }

        UpdateVisuals();
    }

    void Update()
    {
        // Обновляем cooldown таймер
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;

            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                cooldownTimer = 0f;
            }

            // Обновляем визуальный индикатор cooldown
            if (enableCooldownVisual && cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = cooldownTimer / cooldownDuration;
            }

            UpdateVisuals();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Проверяем можно ли атаковать
        if (isOnCooldown)
        {
            return;
        }

        isPressed = true;
        UpdateVisuals();

        // Тактильная обратная связь (вибрация)
        if (enableHapticFeedback)
        {
            TriggerHapticFeedback();
        }

        // Вызываем событие атаки
        OnAttackPressed?.Invoke();

        // Запускаем cooldown
        StartCooldown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        UpdateVisuals();

        // Вызываем событие отпускания
        OnAttackReleased?.Invoke();
    }

    /// <summary>
    /// Запустить cooldown
    /// </summary>
    private void StartCooldown()
    {
        isOnCooldown = true;
        cooldownTimer = cooldownDuration;
    }

    /// <summary>
    /// Установить кастомный cooldown (для разных типов атак)
    /// </summary>
    public void StartCooldown(float duration)
    {
        cooldownDuration = duration;
        StartCooldown();
    }

    /// <summary>
    /// Обновить визуальное состояние кнопки
    /// </summary>
    private void UpdateVisuals()
    {
        if (buttonImage == null) return;

        if (isOnCooldown)
        {
            buttonImage.color = disabledColor;
        }
        else if (isPressed)
        {
            buttonImage.color = pressedColor;
        }
        else
        {
            buttonImage.color = normalColor;
        }
    }

    /// <summary>
    /// Тактильная обратная связь (вибрация)
    /// </summary>
    private void TriggerHapticFeedback()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate(); // Короткая вибрация
#endif
    }

    /// <summary>
    /// Проверить доступна ли кнопка для нажатия
    /// </summary>
    public bool IsAvailable
    {
        get { return !isOnCooldown; }
    }

    /// <summary>
    /// Проверить нажата ли кнопка
    /// </summary>
    public bool IsPressed
    {
        get { return isPressed; }
    }

    /// <summary>
    /// Получить процент cooldown (0-1)
    /// </summary>
    public float GetCooldownPercent()
    {
        if (!isOnCooldown) return 0f;
        return cooldownTimer / cooldownDuration;
    }

    /// <summary>
    /// Принудительно сбросить cooldown
    /// </summary>
    public void ResetCooldown()
    {
        isOnCooldown = false;
        cooldownTimer = 0f;

        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = 0f;
        }

        UpdateVisuals();
    }

    // Дебаг визуализация в редакторе
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (isPressed)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        if (isOnCooldown)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);
        }
    }
}
