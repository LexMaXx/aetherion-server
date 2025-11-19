using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Простая кнопка удержания для мобильных контролов (например, подъем/спуск на маунте).
/// </summary>
public class MobileHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Visuals")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    public bool IsPressed { get; private set; }

    private void Awake()
    {
        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }

        UpdateVisuals();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;
        UpdateVisuals();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsPressed = false;
        UpdateVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsPressed)
        {
            IsPressed = false;
            UpdateVisuals();
        }
    }

    private void OnDisable()
    {
        IsPressed = false;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (buttonImage == null)
            return;

        buttonImage.color = IsPressed ? pressedColor : normalColor;
    }
}
