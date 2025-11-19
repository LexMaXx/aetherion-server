using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Виртуальный джойстик для мобильных устройств
/// Поддерживает фиксированный и динамический режимы
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Joystick Settings")]
    [SerializeField] private bool isDynamic = false; // Динамический джойстик (появляется где нажали)
    [SerializeField] private float handleRange = 50f; // Радиус движения ручки
    [SerializeField] private float deadZone = 0.1f; // Мёртвая зона (игнорировать маленькие движения)

    [Header("Components")]
    [SerializeField] private RectTransform background; // Фон джойстика
    [SerializeField] private RectTransform handle; // Ручка джойстика
    [SerializeField] private Canvas canvas;

    // Outputs
    private Vector2 inputVector = Vector2.zero;
    private Vector2 joystickPosition = Vector2.zero;
    private bool isPressed = false;

    // Initial positions (for dynamic joystick)
    private Vector2 backgroundStartPos;

    void Start()
    {
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        // Сохраняем начальную позицию для динамического джойстика
        backgroundStartPos = background.anchoredPosition;

        // Если динамический - прячем в начале
        if (isDynamic)
        {
            background.gameObject.SetActive(false);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        Debug.Log($"[VirtualJoystick] 🎮 OnPointerDown - Джойстик нажат! Position: {eventData.position}");

        if (isDynamic)
        {
            // Показываем джойстик в месте нажатия
            background.gameObject.SetActive(true);
            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out position
            );
            background.anchoredPosition = position;
        }

        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        inputVector = Vector2.zero;

        // Возвращаем ручку в центр
        handle.anchoredPosition = Vector2.zero;

        // Прячем динамический джойстик
        if (isDynamic)
        {
            background.gameObject.SetActive(false);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isPressed) return;

        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            canvas.worldCamera,
            out position
        );

        // Нормализуем позицию относительно размера джойстика
        position = Vector2.ClampMagnitude(position, handleRange);

        // Двигаем ручку
        handle.anchoredPosition = position;

        // Рассчитываем input vector
        inputVector = position / handleRange;

        // Применяем мёртвую зону
        if (inputVector.magnitude < deadZone)
        {
            inputVector = Vector2.zero;
        }

        // Debug
        if (inputVector.magnitude > 0.01f)
        {
            Debug.Log($"[VirtualJoystick] 📍 OnDrag - Input: ({inputVector.x:F2}, {inputVector.y:F2}), Magnitude: {inputVector.magnitude:F2}");
        }
    }

    /// <summary>
    /// Получить направление движения (нормализованный вектор)
    /// </summary>
    public Vector2 GetInputDirection()
    {
        return inputVector;
    }

    /// <summary>
    /// Получить горизонтальный вход (-1 до 1)
    /// </summary>
    public float Horizontal
    {
        get { return inputVector.x; }
    }

    /// <summary>
    /// Получить вертикальный вход (-1 до 1)
    /// </summary>
    public float Vertical
    {
        get { return inputVector.y; }
    }

    /// <summary>
    /// Проверить нажат ли джойстик
    /// </summary>
    public bool IsPressed
    {
        get { return isPressed; }
    }

    /// <summary>
    /// Получить силу нажатия (0-1)
    /// </summary>
    public float GetMagnitude()
    {
        return inputVector.magnitude;
    }

    // Визуальный дебаг в редакторе
    void OnDrawGizmos()
    {
        if (background == null || handle == null) return;

        Gizmos.color = Color.green;
        Vector3 worldPos = background.TransformPoint(handle.anchoredPosition);
        Gizmos.DrawWireSphere(worldPos, 0.1f);

        if (inputVector.magnitude > 0.01f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(background.position, new Vector3(inputVector.x, 0, inputVector.y) * 2f);
        }
    }
}
