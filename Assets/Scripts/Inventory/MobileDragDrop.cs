using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Универсальная система Drag & Drop для ПК и мобильных устройств
/// Использует Unity EventSystem для обработки как мыши, так и касаний
/// </summary>
public class MobileDragDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Settings")]
    [SerializeField] private bool enableDragAndDrop = true;
    [SerializeField] private float dragAlpha = 0.6f;

    [Header("Touch Settings")]
    [SerializeField] private float longPressDuration = 0.3f; // Время удержания для начала drag
    [SerializeField] private bool requireLongPress = false; // Требовать ли долгое нажатие для drag

    // Runtime данные
    private CanvasGroup canvasGroup;
    private Canvas parentCanvas;
    private RectTransform rectTransform;
    private Transform originalParent;
    private Vector3 originalPosition;
    private int originalSiblingIndex;

    // Ссылки на слоты
    private InventorySlot inventorySlot;
    private EquipmentSlotUI equipmentSlot;

    // Статические данные для отслеживания drag
    private static MobileDragDrop currentlyDragging;

    // Touch данные
    private bool isDragging = false;
    private float touchStartTime;
    private Vector2 touchStartPosition;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        parentCanvas = GetComponentInParent<Canvas>();
        inventorySlot = GetComponent<InventorySlot>();
        equipmentSlot = GetComponent<EquipmentSlotUI>();

        // Автоопределение мобильной платформы
#if UNITY_ANDROID || UNITY_IOS
        requireLongPress = false; // На мобильных сразу начинаем drag без долгого нажатия
#endif
    }

    /// <summary>
    /// Проверка: можно ли начать drag
    /// </summary>
    private bool CanStartDrag()
    {
        if (!enableDragAndDrop) return false;

        // Проверяем что слот не пустой
        if (inventorySlot != null)
        {
            return !inventorySlot.IsEmpty();
        }

        if (equipmentSlot != null)
        {
            return !equipmentSlot.IsEmpty();
        }

        return false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanStartDrag()) return;

        touchStartTime = Time.time;
        touchStartPosition = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        touchStartTime = 0f;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanStartDrag())
        {
            eventData.pointerDrag = null;
            return;
        }

        // Проверка долгого нажатия (если требуется)
        if (requireLongPress)
        {
            float pressDuration = Time.time - touchStartTime;
            if (pressDuration < longPressDuration)
            {
                eventData.pointerDrag = null;
                return;
            }
        }

        isDragging = true;
        currentlyDragging = this;

        // Сохраняем оригинальные данные
        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;
        originalSiblingIndex = transform.GetSiblingIndex();

        // Визуальные изменения
        canvasGroup.alpha = dragAlpha;
        canvasGroup.blocksRaycasts = false;

        // Перемещаем в корень Canvas для отображения поверх всего
        if (parentCanvas != null)
        {
            transform.SetParent(parentCanvas.transform);
            transform.SetAsLastSibling();
        }

        string itemName = GetItemName();
        Debug.Log($"[MobileDragDrop] 📱 Begin drag: {itemName}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || currentlyDragging != this) return;

        // Следуем за указателем (мышь или палец)
        if (parentCanvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        // Восстанавливаем визуал
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Проверяем что под курсором/пальцем
        bool dropHandled = false;

        // Получаем объект под указателем
        var pointerEventData = eventData;
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        foreach (var result in raycastResults)
        {
            if (result.gameObject == gameObject) continue; // Пропускаем сам перетаскиваемый объект

            // Ищем InventorySlot
            InventorySlot targetInventorySlot = result.gameObject.GetComponent<InventorySlot>();
            if (targetInventorySlot == null)
            {
                targetInventorySlot = result.gameObject.GetComponentInParent<InventorySlot>();
            }

            // Ищем EquipmentSlotUI
            EquipmentSlotUI targetEquipmentSlot = result.gameObject.GetComponent<EquipmentSlotUI>();
            if (targetEquipmentSlot == null)
            {
                targetEquipmentSlot = result.gameObject.GetComponentInParent<EquipmentSlotUI>();
            }

            // Обрабатываем drop
            if (targetInventorySlot != null && targetInventorySlot != inventorySlot)
            {
                dropHandled = HandleDropToInventory(targetInventorySlot);
                if (dropHandled) break;
            }
            else if (targetEquipmentSlot != null && targetEquipmentSlot != equipmentSlot)
            {
                dropHandled = HandleDropToEquipment(targetEquipmentSlot);
                if (dropHandled) break;
            }
        }

        // Если drop не обработан - возвращаем на место
        if (!dropHandled)
        {
            ReturnToOriginalPosition();
        }

        currentlyDragging = null;
        Debug.Log($"[MobileDragDrop] 📱 End drag - handled: {dropHandled}");
    }

    /// <summary>
    /// Drop на слот инвентаря
    /// </summary>
    private bool HandleDropToInventory(InventorySlot targetSlot)
    {
        // Из инвентаря в инвентарь
        if (inventorySlot != null)
        {
            // Используем встроенную логику InventorySlot
            // Вызываем OnDrop напрямую через симуляцию события
            PointerEventData fakeEvent = new PointerEventData(EventSystem.current);
            fakeEvent.pointerDrag = gameObject;

            targetSlot.OnDrop(fakeEvent);
            return true;
        }

        // Из экипировки в инвентарь (разэкипировка)
        if (equipmentSlot != null)
        {
            ItemData item = equipmentSlot.GetEquippedItem();
            if (item != null && targetSlot.IsEmpty())
            {
                equipmentSlot.UnequipItem();
                targetSlot.SetItem(item, 1);

                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.SyncInventoryToServer();
                }

                Debug.Log($"[MobileDragDrop] ✅ Разэкипировка {item.itemName} в инвентарь");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Drop на слот экипировки
    /// </summary>
    private bool HandleDropToEquipment(EquipmentSlotUI targetSlot)
    {
        // Из инвентаря на экипировку
        if (inventorySlot != null)
        {
            // Используем встроенную логику EquipmentSlotUI
            PointerEventData fakeEvent = new PointerEventData(EventSystem.current);
            fakeEvent.pointerDrag = gameObject;

            targetSlot.OnDrop(fakeEvent);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Вернуть объект на исходную позицию
    /// </summary>
    private void ReturnToOriginalPosition()
    {
        if (originalParent != null)
        {
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
            rectTransform.anchoredPosition = originalPosition;
        }

        Debug.Log("[MobileDragDrop] ↩️ Возврат на исходную позицию");
    }

    /// <summary>
    /// Получить имя предмета для логирования
    /// </summary>
    private string GetItemName()
    {
        if (inventorySlot != null && !inventorySlot.IsEmpty())
        {
            return inventorySlot.GetItem().itemName;
        }

        if (equipmentSlot != null && !equipmentSlot.IsEmpty())
        {
            return equipmentSlot.GetEquippedItem().itemName;
        }

        return "Unknown";
    }
}
