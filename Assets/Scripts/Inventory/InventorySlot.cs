using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// UI слот инвентаря
/// Поддерживает drag & drop, отображение иконки и количества
/// </summary>
public class InventorySlot : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject highlightFrame;

    [Header("Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;

    // Данные слота
    private ItemData currentItem;
    private int quantity = 0;
    private bool isEmpty = true;

    // Drag & Drop
    private static GameObject draggedObject;
    private static InventorySlot draggedSlot;
    private static GameObject dragIcon; // Временная иконка для перетаскивания
    private CanvasGroup canvasGroup;
    private Canvas parentCanvas;
    private Vector3 originalPosition;
    private Transform originalParent;

    void Awake()
    {
        // КРИТИЧНО: Проверяем обязательные ссылки
        if (iconImage == null)
        {
            Debug.LogError($"[InventorySlot] ❌❌❌ КРИТИЧЕСКАЯ ОШИБКА! iconImage не назначен в префабе {gameObject.name}! Предметы НЕ БУДУТ отображаться!", gameObject);
            enabled = false; // Отключаем компонент чтобы не было ложных срабатываний
            return;
        }

        if (quantityText == null)
        {
            Debug.LogWarning($"[InventorySlot] ⚠️ quantityText не назначен в префабе {gameObject.name}. Количество предметов не будет отображаться.", gameObject);
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        parentCanvas = GetComponentInParent<Canvas>();

        if (highlightFrame != null)
        {
            highlightFrame.SetActive(false);
        }

        ClearSlot();

        Debug.Log($"[InventorySlot] ✅ Awake OK: iconImage={(iconImage != null ? "OK" : "NULL")}, quantityText={(quantityText != null ? "OK" : "NULL")}");
    }

    /// <summary>
    /// Установить предмет в слот
    /// </summary>
    public void SetItem(ItemData item, int amount = 1)
    {
        if (item == null)
        {
            ClearSlot();
            return;
        }

        currentItem = item;
        quantity = amount;
        isEmpty = false;

        Debug.Log($"[InventorySlot] SetItem: {item.itemName} x{amount}, iconImage={(iconImage != null ? "OK" : "NULL")}, quantityText={(quantityText != null ? "OK" : "NULL")}");

        // Обновляем иконку
        if (iconImage != null)
        {
            // КРИТИЧНО: Включаем GameObject иконки (может быть выключен в префабе)
            if (iconImage.gameObject != null)
            {
                iconImage.gameObject.SetActive(true);
            }

            // ДИАГНОСТИКА: Проверяем что sprite действительно есть
            if (item.icon == null)
            {
                Debug.LogError($"[InventorySlot] ❌❌❌ КРИТИЧНО! item.icon is NULL for {item.itemName}! Sprite не назначен в ScriptableObject!");
            }
            else
            {
                Debug.Log($"[InventorySlot] 🔵 item.icon СУЩЕСТВУЕТ: name={item.icon.name}, texture={(item.icon.texture != null ? "YES" : "NO")}");
            }

            // КРИТИЧНО: Сначала включаем Image компонент
            iconImage.enabled = true;

            // КРИТИЧНО: Устанавливаем sprite НЕСКОЛЬКИМИ СПОСОБАМИ
            iconImage.sprite = item.icon;
            iconImage.overrideSprite = item.icon; // ALTERNATIVE WAY

            // ДИАГНОСТИКА: Проверяем что sprite действительно установился
            if (iconImage.sprite == null)
            {
                Debug.LogError($"[InventorySlot] ❌❌❌ КРИТИЧНО! iconImage.sprite остался NULL после присваивания!");
            }
            else
            {
                Debug.Log($"[InventorySlot] 🔵 iconImage.sprite УСТАНОВЛЕН: {iconImage.sprite.name}");
            }

            // ДИАГНОСТИКА: Принудительно устанавливаем яркий цвет для теста
            iconImage.color = Color.red; // ВРЕМЕННО: красный чтобы точно было видно!

            // КРИТИЧНО: Принудительно обновляем Canvas Renderer
            iconImage.SetAllDirty();
            iconImage.SetNativeSize(); // Устанавливаем размер по sprite
            Canvas.ForceUpdateCanvases(); // Обновляем ВСЕ канвасы

            Debug.Log($"[InventorySlot] 🔄 Canvas обновлён принудительно для {item.itemName}");

            Debug.Log($"[InventorySlot] ✅ Icon updated: sprite={(item.icon != null ? item.icon.name : "null")}, enabled={iconImage.enabled}, gameObject.activeSelf={iconImage.gameObject.activeSelf}, color={iconImage.color}");
        }
        else
        {
            Debug.LogError($"[InventorySlot] ❌ iconImage is NULL in prefab! Item {item.itemName} won't be visible!");
        }

        // Обновляем количество
        if (quantityText != null)
        {
            if (quantityText.gameObject != null)
            {
                quantityText.gameObject.SetActive(true);
            }

            if (item.isStackable && quantity > 1)
            {
                quantityText.text = quantity.ToString();
                quantityText.enabled = true;
            }
            else
            {
                quantityText.enabled = false;
            }
        }
        else
        {
            Debug.LogWarning($"[InventorySlot] ⚠️ quantityText is NULL in prefab");
        }
    }

    /// <summary>
    /// Очистить слот
    /// </summary>
    public void ClearSlot()
    {
        currentItem = null;
        quantity = 0;
        isEmpty = true;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (quantityText != null)
        {
            quantityText.enabled = false;
        }
    }

    /// <summary>
    /// Добавить количество к существующему предмету
    /// </summary>
    public bool AddQuantity(int amount)
    {
        if (isEmpty || currentItem == null) return false;

        if (!currentItem.isStackable) return false;

        int newQuantity = quantity + amount;
        if (newQuantity > currentItem.maxStackSize) return false;

        quantity = newQuantity;
        SetItem(currentItem, quantity);
        return true;
    }

    /// <summary>
    /// Убрать количество
    /// </summary>
    public void RemoveQuantity(int amount)
    {
        quantity -= amount;
        if (quantity <= 0)
        {
            ClearSlot();
        }
        else
        {
            SetItem(currentItem, quantity);
        }
    }

    // Геттеры
    public ItemData GetItem() => currentItem;
    public int GetQuantity() => quantity;
    public bool IsEmpty() => isEmpty;

    /// <summary>
    /// Клик по слоту
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isEmpty) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Правый клик - использовать/экипировать
            OnRightClick();
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Левый клик - показать информацию
            OnLeftClick();
        }
    }

    private void OnLeftClick()
    {
        Debug.Log($"[InventorySlot] Clicked on: {currentItem.itemName}");
        // Здесь можно показать tooltip с информацией о предмете
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ShowItemTooltip(currentItem, transform.position);
        }
    }

    private void OnRightClick()
    {
        Debug.Log($"[InventorySlot] Right clicked on: {currentItem.itemName}");

        if (currentItem.isEquippable)
        {
            // Экипировать
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.EquipItem(currentItem, this);
            }
        }
        else if (currentItem.itemType == ItemType.Consumable)
        {
            // Использовать
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.UseItem(currentItem, this);
            }
        }
    }

    // ===== DRAG & DROP =====

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isEmpty) return;

        draggedObject = gameObject;
        draggedSlot = this;

        // Сохраняем исходную позицию (НЕ меняем parent!)
        originalPosition = transform.position;
        originalParent = transform.parent;

        // Делаем слот полупрозрачным (но НЕ отключаем raycast - он остаётся на месте)
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        // ИСПРАВЛЕНИЕ: Создаём ВРЕМЕННУЮ иконку для перетаскивания вместо перемещения слота
        if (parentCanvas != null && currentItem != null && currentItem.icon != null)
        {
            // Создаём GameObject с иконкой
            dragIcon = new GameObject("DragIcon");
            dragIcon.transform.SetParent(parentCanvas.transform, false);

            // Добавляем Image компонент
            Image dragImage = dragIcon.AddComponent<Image>();
            dragImage.sprite = currentItem.icon;
            dragImage.raycastTarget = false; // Не блокируем raycast

            // Устанавливаем размер иконки
            RectTransform rect = dragIcon.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(60f, 60f); // Размер иконки при перетаскивании
            rect.position = eventData.position;

            // Добавляем CanvasGroup для прозрачности
            CanvasGroup iconCanvasGroup = dragIcon.AddComponent<CanvasGroup>();
            iconCanvasGroup.alpha = 0.8f;
            iconCanvasGroup.blocksRaycasts = false;
        }

        Debug.Log($"[InventorySlot] Begin drag: {currentItem.itemName}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        // ИСПРАВЛЕНИЕ: Двигаем ВРЕМЕННУЮ иконку, а не сам слот
        if (dragIcon != null)
        {
            dragIcon.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Возвращаем прозрачность и raycast
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // ИСПРАВЛЕНИЕ: Уничтожаем временную иконку
        if (dragIcon != null)
        {
            Object.Destroy(dragIcon);
            dragIcon = null;
        }

        // Слот остаётся на своём месте - не нужно возвращать parent/position!

        draggedObject = null;
        draggedSlot = null;

        Debug.Log($"[InventorySlot] End drag");
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (draggedSlot == null || draggedSlot == this) return;

        Debug.Log($"[InventorySlot] Drop {draggedSlot.GetItem()?.itemName} onto {currentItem?.itemName}");

        ItemData draggedItem = draggedSlot.GetItem();
        int draggedQuantity = draggedSlot.GetQuantity();

        // Если целевой слот пустой - просто перемещаем
        if (isEmpty)
        {
            SetItem(draggedItem, draggedQuantity);
            draggedSlot.ClearSlot();
            Debug.Log($"[InventorySlot] ✅ Переместили {draggedItem.itemName} в пустой слот");
        }
        // Если предметы одинаковые и stackable - пытаемся объединить
        else if (currentItem == draggedItem && draggedItem.isStackable)
        {
            int remainingSpace = draggedItem.maxStackSize - quantity;
            int amountToMove = Mathf.Min(draggedQuantity, remainingSpace);

            if (amountToMove > 0)
            {
                AddQuantity(amountToMove);
                draggedSlot.RemoveQuantity(amountToMove);
                Debug.Log($"[InventorySlot] ✅ Объединили стаки: +{amountToMove}");
            }
            else
            {
                // Нет места - swap
                SwapItems(draggedSlot);
            }
        }
        // Иначе - меняем местами
        else
        {
            SwapItems(draggedSlot);
        }

        // Синхронизация с сервером после drop
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SyncInventoryToServer();
        }
    }

    /// <summary>
    /// Обменять предметы между слотами
    /// </summary>
    private void SwapItems(InventorySlot otherSlot)
    {
        // Сохраняем данные этого слота
        ItemData tempItem = currentItem;
        int tempQuantity = quantity;
        bool tempEmpty = isEmpty;

        // Копируем данные из другого слота
        if (otherSlot.IsEmpty())
        {
            ClearSlot();
        }
        else
        {
            SetItem(otherSlot.GetItem(), otherSlot.GetQuantity());
        }

        // Копируем сохранённые данные в другой слот
        if (tempEmpty)
        {
            otherSlot.ClearSlot();
        }
        else
        {
            otherSlot.SetItem(tempItem, tempQuantity);
        }

        Debug.Log($"[InventorySlot] ✅ Swapped items");
    }

    /// <summary>
    /// Подсветить слот
    /// </summary>
    public void Highlight(bool enable)
    {
        if (highlightFrame != null)
        {
            highlightFrame.SetActive(enable);
        }
    }
}
