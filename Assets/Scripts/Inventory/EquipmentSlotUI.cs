using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// UI слот экипировки
/// Принимает только определённый тип предмета
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("Slot Settings")]
    [Tooltip("Тип слота экипировки")]
    [SerializeField] private EquipmentSlot slotType = EquipmentSlot.Weapon;

    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image slotBackgroundImage;
    [SerializeField] private TextMeshProUGUI slotNameText;
    [SerializeField] private GameObject highlightFrame;

    [Header("Visual Settings")]
    [SerializeField] private Sprite defaultSlotIcon; // Иконка пустого слота
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;

    // Данные слота
    private ItemData equippedItem;
    private bool isEmpty = true;

    // Drag & Drop
    private static GameObject draggedObject;
    private static EquipmentSlotUI draggedEquipmentSlot;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (highlightFrame != null)
        {
            highlightFrame.SetActive(false);
        }

        // Устанавливаем название слота
        if (slotNameText != null)
        {
            slotNameText.text = GetSlotDisplayName();
        }

        ClearSlot();
    }

    /// <summary>
    /// Получить отображаемое имя слота
    /// </summary>
    private string GetSlotDisplayName()
    {
        switch (slotType)
        {
            case EquipmentSlot.Weapon: return "Weapon";
            case EquipmentSlot.Armor: return "Armor";
            case EquipmentSlot.Helmet: return "Helmet";
            case EquipmentSlot.Accessory: return "Accessory";
            default: return "Equipment";
        }
    }

    /// <summary>
    /// Экипировать предмет
    /// </summary>
    public bool EquipItem(ItemData item)
    {
        if (item == null) return false;

        // Проверяем что предмет можно экипировать
        if (!item.isEquippable)
        {
            Debug.LogWarning($"[EquipmentSlotUI] {item.itemName} is not equippable!");
            return false;
        }

        // Проверяем соответствие слота
        if (item.equipmentSlot != slotType)
        {
            Debug.LogWarning($"[EquipmentSlotUI] {item.itemName} cannot be equipped in {slotType} slot!");
            return false;
        }

        // Сохраняем старый предмет
        ItemData oldItem = equippedItem;

        // Экипируем новый
        equippedItem = item;
        isEmpty = false;

        // Обновляем иконку
        if (iconImage != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = true;
            iconImage.color = normalColor;
        }

        Debug.Log($"[EquipmentSlotUI] ✅ Equipped {item.itemName} in {slotType} slot");

        // Возвращаем старый предмет в инвентарь
        if (oldItem != null && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(oldItem, 1);
        }

        // НОВОЕ: Уведомляем InventoryManager об изменении экипировки
        // (InventoryManager вызовет UpdateCharacterStatsFromEquipment)

        return true;
    }

    /// <summary>
    /// Снять экипировку
    /// </summary>
    public ItemData UnequipItem()
    {
        if (isEmpty) return null;

        ItemData unequippedItem = equippedItem;
        ClearSlot();

        Debug.Log($"[EquipmentSlotUI] ✅ Unequipped {unequippedItem.itemName}");

        // НОВОЕ: Обновляем CharacterStats после разэкипировки
        UpdateCharacterStatsAfterUnequip();

        // Синхронизация инвентаря с сервером
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SyncInventoryToServer();
        }

        return unequippedItem;
    }

    /// <summary>
    /// Обновить CharacterStats после разэкипировки
    /// </summary>
    private void UpdateCharacterStatsAfterUnequip()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        CharacterStats stats = player.GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.UpdateEquipmentBonuses();
            Debug.Log("[EquipmentSlotUI] ✅ CharacterStats updated after unequip");
        }
    }

    /// <summary>
    /// Очистить слот
    /// </summary>
    public void ClearSlot()
    {
        equippedItem = null;
        isEmpty = true;

        if (iconImage != null)
        {
            iconImage.sprite = defaultSlotIcon;
            if (defaultSlotIcon == null)
            {
                iconImage.enabled = false;
            }
        }
    }

    // Геттеры
    public ItemData GetEquippedItem() => equippedItem;
    public bool IsEmpty() => isEmpty;
    public EquipmentSlot GetSlotType() => slotType;

    /// <summary>
    /// Клик по слоту
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isEmpty) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Правый клик - снять экипировку
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
        Debug.Log($"[EquipmentSlotUI] Clicked on: {equippedItem.itemName}");
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ShowItemTooltip(equippedItem, transform.position);
        }
    }

    private void OnRightClick()
    {
        Debug.Log($"[EquipmentSlotUI] Right clicked - unequipping: {equippedItem.itemName}");

        if (InventoryManager.Instance != null)
        {
            ItemData item = UnequipItem();
            if (item != null)
            {
                InventoryManager.Instance.AddItem(item, 1);
            }
        }
    }

    // ===== DRAG & DROP =====

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isEmpty) return;

        draggedObject = gameObject;
        draggedEquipmentSlot = this;

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        Debug.Log($"[EquipmentSlotUI] Begin drag: {equippedItem.itemName}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Визуально можно двигать за курсором
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        draggedObject = null;
        draggedEquipmentSlot = null;

        Debug.Log($"[EquipmentSlotUI] End drag");
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Проверяем что дропнули предмет из инвентаря
        InventorySlot inventorySlot = eventData.pointerDrag?.GetComponent<InventorySlot>();

        if (inventorySlot != null && !inventorySlot.IsEmpty())
        {
            ItemData droppedItem = inventorySlot.GetItem();

            // Проверяем можно ли экипировать
            if (droppedItem.isEquippable && droppedItem.equipmentSlot == slotType)
            {
                Debug.Log($"[EquipmentSlotUI] Drop {droppedItem.itemName} onto {slotType} slot");

                // Если слот пустой - просто экипируем
                if (isEmpty)
                {
                    if (EquipItem(droppedItem))
                    {
                        inventorySlot.RemoveQuantity(1);

                        // Обновляем CharacterStats
                        UpdateCharacterStatsAfterEquip();

                        // Синхронизация с сервером
                        if (InventoryManager.Instance != null)
                        {
                            InventoryManager.Instance.SyncInventoryToServer();
                        }
                    }
                }
                // Если слот занят - swap
                else
                {
                    ItemData currentlyEquipped = equippedItem;
                    UnequipItem();

                    if (EquipItem(droppedItem))
                    {
                        // Меняем местами: новый предмет экипирован, старый в инвентарь
                        inventorySlot.SetItem(currentlyEquipped, 1);

                        // Обновляем CharacterStats
                        UpdateCharacterStatsAfterEquip();

                        // Синхронизация с сервером
                        if (InventoryManager.Instance != null)
                        {
                            InventoryManager.Instance.SyncInventoryToServer();
                        }

                        Debug.Log($"[EquipmentSlotUI] ✅ Swap: {droppedItem.itemName} ↔ {currentlyEquipped.itemName}");
                    }
                    else
                    {
                        // Если не удалось экипировать - возвращаем старый предмет
                        EquipItem(currentlyEquipped);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[EquipmentSlotUI] ⚠️ Cannot equip {droppedItem.itemName} in {slotType} slot!");
            }
        }

        // Проверяем что дропнули из другого слота экипировки
        if (draggedEquipmentSlot != null && draggedEquipmentSlot != this)
        {
            // Меняем местами (только если тот же тип слота)
            if (draggedEquipmentSlot.GetSlotType() == slotType)
            {
                SwapEquipment(draggedEquipmentSlot);

                // Синхронизация с сервером
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.SyncInventoryToServer();
                }
            }
        }
    }

    /// <summary>
    /// Обновить CharacterStats после экипировки
    /// </summary>
    private void UpdateCharacterStatsAfterEquip()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        CharacterStats stats = player.GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.UpdateEquipmentBonuses();
            Debug.Log("[EquipmentSlotUI] ✅ CharacterStats updated after equip");
        }
    }

    /// <summary>
    /// Обменять экипировку между слотами
    /// </summary>
    private void SwapEquipment(EquipmentSlotUI otherSlot)
    {
        ItemData tempItem = equippedItem;
        bool tempEmpty = isEmpty;

        if (otherSlot.IsEmpty())
        {
            ClearSlot();
        }
        else
        {
            equippedItem = otherSlot.GetEquippedItem();
            isEmpty = false;
            if (iconImage != null)
            {
                iconImage.sprite = equippedItem.icon;
                iconImage.enabled = true;
            }
        }

        if (tempEmpty)
        {
            otherSlot.ClearSlot();
        }
        else
        {
            otherSlot.equippedItem = tempItem;
            otherSlot.isEmpty = false;
            if (otherSlot.iconImage != null)
            {
                otherSlot.iconImage.sprite = tempItem.icon;
                otherSlot.iconImage.enabled = true;
            }
        }

        Debug.Log($"[EquipmentSlotUI] ✅ Swapped equipment");
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
