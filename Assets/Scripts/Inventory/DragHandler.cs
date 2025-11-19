using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Обработчик перетаскивания предметов (Drag & Drop)
/// Прикрепляется к каждому InventorySlot
/// </summary>
public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Canvas parentCanvas;

    // Runtime данные
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 originalPosition;
    private int originalSiblingIndex;

    // Данные о перетаскиваемом слоте
    public InventorySlot sourceSlot { get; private set; }
    public EquipmentSlotUI sourceEquipmentSlot { get; private set; }

    // Статическая ссылка на текущий перетаскиваемый объект
    public static DragHandler currentlyDragging { get; private set; }

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Если CanvasGroup нет - добавляем
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Находим Canvas
        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }

        // Получаем ссылку на слот
        sourceSlot = GetComponent<InventorySlot>();
        sourceEquipmentSlot = GetComponent<EquipmentSlotUI>();
    }

    /// <summary>
    /// Начало перетаскивания
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Проверяем что слот не пустой
        bool isEmpty = false;

        if (sourceSlot != null)
        {
            isEmpty = sourceSlot.IsEmpty();
        }
        else if (sourceEquipmentSlot != null)
        {
            isEmpty = sourceEquipmentSlot.IsEmpty();
        }
        else
        {
            Debug.LogWarning("[DragHandler] ⚠️ No slot component found!");
            return;
        }

        if (isEmpty)
        {
            eventData.pointerDrag = null; // Отменяем drag
            return;
        }

        Debug.Log($"[DragHandler] 🖱️ Начало перетаскивания из {(sourceSlot != null ? "Inventory" : "Equipment")}");

        // Сохраняем оригинальные данные
        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;
        originalSiblingIndex = transform.GetSiblingIndex();

        // Делаем объект полупрозрачным и игнорируем raycast
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        // Перемещаем в корень Canvas для отображения поверх всего
        transform.SetParent(parentCanvas.transform);
        transform.SetAsLastSibling();

        // Устанавливаем как текущий перетаскиваемый объект
        currentlyDragging = this;
    }

    /// <summary>
    /// Процесс перетаскивания
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (currentlyDragging != this) return;

        // Следуем за мышью
        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }

    /// <summary>
    /// Конец перетаскивания
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (currentlyDragging != this) return;

        Debug.Log("[DragHandler] 🖱️ Конец перетаскивания");

        // Восстанавливаем визуал
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Проверяем что находится под курсором
        GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;

        bool dropHandled = false;

        if (dropTarget != null)
        {
            // Пытаемся найти InventorySlot или EquipmentSlotUI в dropTarget или родителях
            InventorySlot targetInventorySlot = dropTarget.GetComponent<InventorySlot>();
            if (targetInventorySlot == null)
            {
                targetInventorySlot = dropTarget.GetComponentInParent<InventorySlot>();
            }

            EquipmentSlotUI targetEquipmentSlot = dropTarget.GetComponent<EquipmentSlotUI>();
            if (targetEquipmentSlot == null)
            {
                targetEquipmentSlot = dropTarget.GetComponentInParent<EquipmentSlotUI>();
            }

            // Обрабатываем drop в зависимости от типа целевого слота
            if (targetInventorySlot != null)
            {
                dropHandled = HandleDropToInventorySlot(targetInventorySlot);
            }
            else if (targetEquipmentSlot != null)
            {
                dropHandled = HandleDropToEquipmentSlot(targetEquipmentSlot);
            }
        }

        // Если drop не обработан - возвращаем на место
        if (!dropHandled)
        {
            ReturnToOriginalPosition();
        }

        // Очищаем ссылку на текущий drag
        currentlyDragging = null;
    }

    /// <summary>
    /// Обработка drop в InventorySlot
    /// </summary>
    private bool HandleDropToInventorySlot(InventorySlot targetSlot)
    {
        if (targetSlot == sourceSlot)
        {
            Debug.Log("[DragHandler] 🔄 Drop в тот же слот - отмена");
            return false;
        }

        // Из Inventory → Inventory (swap или merge)
        if (sourceSlot != null)
        {
            ItemData sourceItem = sourceSlot.GetItem();
            int sourceQuantity = sourceSlot.GetQuantity();

            // Если целевой слот пустой - просто перемещаем
            if (targetSlot.IsEmpty())
            {
                targetSlot.SetItem(sourceItem, sourceQuantity);
                sourceSlot.ClearSlot();
                Debug.Log($"[DragHandler] ✅ Перемещение {sourceItem.itemName} в пустой слот");
            }
            // Если целевой слот содержит такой же предмет и он stackable - merge
            else if (targetSlot.GetItem() == sourceItem && sourceItem.isStackable)
            {
                int remainingSpace = sourceItem.maxStackSize - targetSlot.GetQuantity();
                int amountToMove = Mathf.Min(sourceQuantity, remainingSpace);

                if (amountToMove > 0)
                {
                    targetSlot.AddQuantity(amountToMove);
                    sourceSlot.RemoveQuantity(amountToMove);
                    Debug.Log($"[DragHandler] ✅ Объединение стаков: +{amountToMove} в целевой слот");
                }
                else
                {
                    // Нет места - swap
                    SwapInventorySlots(sourceSlot, targetSlot);
                }
            }
            // Иначе - swap
            else
            {
                SwapInventorySlots(sourceSlot, targetSlot);
            }

            return true;
        }
        // Из Equipment → Inventory (разэкипировка)
        else if (sourceEquipmentSlot != null)
        {
            ItemData equippedItem = sourceEquipmentSlot.GetEquippedItem();

            if (equippedItem != null)
            {
                // Если целевой слот пустой - разэкипируем в него
                if (targetSlot.IsEmpty())
                {
                    sourceEquipmentSlot.UnequipItem();
                    targetSlot.SetItem(equippedItem, 1);
                    Debug.Log($"[DragHandler] ✅ Разэкипировка {equippedItem.itemName} в слот инвентаря");
                    return true;
                }
                // Если в целевом слоте экипируемый предмет того же типа - swap
                else if (targetSlot.GetItem().isEquippable && targetSlot.GetItem().equipmentSlot == equippedItem.equipmentSlot)
                {
                    ItemData targetItem = targetSlot.GetItem();
                    sourceEquipmentSlot.UnequipItem();
                    sourceEquipmentSlot.EquipItem(targetItem);
                    targetSlot.SetItem(equippedItem, 1);
                    Debug.Log($"[DragHandler] ✅ Swap экипировки: {equippedItem.itemName} ↔ {targetItem.itemName}");
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Обработка drop в EquipmentSlot
    /// </summary>
    private bool HandleDropToEquipmentSlot(EquipmentSlotUI targetEquipmentSlot)
    {
        // Из Inventory → Equipment (экипировка)
        if (sourceSlot != null)
        {
            ItemData item = sourceSlot.GetItem();

            if (item == null) return false;

            // Проверяем что предмет можно экипировать и он подходит для этого слота
            if (item.isEquippable && item.equipmentSlot == targetEquipmentSlot.GetSlotType())
            {
                // Если слот пустой - просто экипируем
                if (targetEquipmentSlot.IsEmpty())
                {
                    if (targetEquipmentSlot.EquipItem(item))
                    {
                        sourceSlot.RemoveQuantity(1);
                        Debug.Log($"[DragHandler] ✅ Экипировка {item.itemName}");

                        // Обновляем CharacterStats
                        if (InventoryManager.Instance != null)
                        {
                            InventoryManager.Instance.SyncInventoryToServer();
                        }

                        return true;
                    }
                }
                // Если слот занят - swap
                else
                {
                    ItemData currentlyEquipped = targetEquipmentSlot.GetEquippedItem();
                    targetEquipmentSlot.UnequipItem();

                    if (targetEquipmentSlot.EquipItem(item))
                    {
                        sourceSlot.SetItem(currentlyEquipped, 1);
                        Debug.Log($"[DragHandler] ✅ Swap экипировки: {item.itemName} ↔ {currentlyEquipped.itemName}");

                        // Обновляем CharacterStats
                        if (InventoryManager.Instance != null)
                        {
                            InventoryManager.Instance.SyncInventoryToServer();
                        }

                        return true;
                    }
                    else
                    {
                        // Если не удалось экипировать - возвращаем старый предмет
                        targetEquipmentSlot.EquipItem(currentlyEquipped);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[DragHandler] ⚠️ {item.itemName} не подходит для слота {targetEquipmentSlot.GetSlotType()}");
            }
        }

        return false;
    }

    /// <summary>
    /// Swap двух слотов инвентаря
    /// </summary>
    private void SwapInventorySlots(InventorySlot slot1, InventorySlot slot2)
    {
        ItemData item1 = slot1.GetItem();
        int quantity1 = slot1.GetQuantity();

        ItemData item2 = slot2.GetItem();
        int quantity2 = slot2.GetQuantity();

        slot1.ClearSlot();
        slot2.ClearSlot();

        if (item2 != null)
        {
            slot1.SetItem(item2, quantity2);
        }

        if (item1 != null)
        {
            slot2.SetItem(item1, quantity1);
        }

        Debug.Log($"[DragHandler] 🔄 Swap слотов: {item1?.itemName ?? "empty"} ↔ {item2?.itemName ?? "empty"}");

        // Синхронизация с сервером
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SyncInventoryToServer();
        }
    }

    /// <summary>
    /// Вернуть объект на исходную позицию
    /// </summary>
    private void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
        rectTransform.anchoredPosition = originalPosition;

        Debug.Log("[DragHandler] ↩️ Возврат на исходную позицию");
    }
}
