using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace AetherionMMO.Inventory
{
    /// <summary>
    /// Слот инвентаря с drag-drop (стиль WoW)
    /// </summary>
    public class MMOInventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Image slotBackground;
        [SerializeField] private GameObject highlightBorder;

        [Header("Settings")]
        [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color filledColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        [SerializeField] private Color highlightColor = Color.yellow;

        // Данные слота
        private int slotIndex;
        private ItemData currentItem;
        private int currentQuantity;
        private MongoInventoryManager manager;

        // Drag & Drop
        private Canvas canvas;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector2 originalPosition;
        private Transform originalParent;

        public int SlotIndex => slotIndex;
        public ItemData CurrentItem => currentItem;
        public int CurrentQuantity => currentQuantity;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            canvas = GetComponentInParent<Canvas>();

            if (highlightBorder != null)
            {
                highlightBorder.SetActive(false);
            }
        }

        /// <summary>
        /// Инициализация слота
        /// </summary>
        public void Initialize(int index, MongoInventoryManager manager)
        {
            this.slotIndex = index;
            this.manager = manager;

            Clear();
        }

        /// <summary>
        /// Установить предмет в слот
        /// </summary>
        public void SetItem(ItemData item, int quantity)
        {
            currentItem = item;
            currentQuantity = quantity;

            UpdateVisuals();
        }

        /// <summary>
        /// Очистить слот
        /// </summary>
        public void Clear()
        {
            currentItem = null;
            currentQuantity = 0;

            UpdateVisuals();
        }

        /// <summary>
        /// Проверить пустой ли слот
        /// </summary>
        public bool IsEmpty()
        {
            return currentItem == null || currentQuantity <= 0;
        }

        /// <summary>
        /// Обновить визуальное отображение
        /// </summary>
        private void UpdateVisuals()
        {
            if (IsEmpty())
            {
                // Пустой слот
                if (iconImage != null)
                {
                    iconImage.enabled = false;
                    iconImage.sprite = null;
                }

                if (quantityText != null)
                {
                    quantityText.text = "";
                }

                if (slotBackground != null)
                {
                    slotBackground.color = emptyColor;
                }
            }
            else
            {
                // Заполненный слот
                if (iconImage != null && currentItem.icon != null)
                {
                    iconImage.enabled = true;
                    iconImage.sprite = currentItem.icon;
                    Debug.Log($"[MMOSlot {slotIndex}] ✅ Icon set: {currentItem.itemName}, sprite={currentItem.icon.name}, enabled={iconImage.enabled}");
                }
                else if (currentItem != null && currentItem.icon == null)
                {
                    Debug.LogError($"[MMOSlot {slotIndex}] ❌ Icon is NULL for item: {currentItem.itemName}");
                    if (iconImage != null)
                        iconImage.enabled = false;
                }

                if (quantityText != null)
                {
                    if (currentQuantity > 1)
                    {
                        quantityText.text = currentQuantity.ToString();
                    }
                    else
                    {
                        quantityText.text = "";
                    }
                }

                if (slotBackground != null)
                {
                    slotBackground.color = filledColor;
                }
            }
        }

        // ═══════════════════════════════════════════
        // DRAG & DROP IMPLEMENTATION
        // ═══════════════════════════════════════════

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsEmpty())
                return;

            // Сохраняем оригинальную позицию
            originalPosition = rectTransform.anchoredPosition;
            originalParent = transform.parent;

            // Делаем слот прозрачным при перетаскивании
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.6f;
                canvasGroup.blocksRaycasts = false;
            }

            // Поднимаем слот выше других
            transform.SetParent(canvas.transform, true);

            // Уведомляем менеджер
            manager?.StartDrag(this);

            Debug.Log($"[MMOSlot] 🖱️ Начато перетаскивание предмета: {currentItem.itemName} из слота {slotIndex}");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsEmpty())
                return;

            // Двигаем слот за курсором
            if (rectTransform != null && canvas != null)
            {
                rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (IsEmpty())
                return;

            // Восстанавливаем видимость
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            // Возвращаем слот в оригинальный parent
            transform.SetParent(originalParent, true);
            rectTransform.anchoredPosition = originalPosition;

            // Проверяем куда бросили
            GameObject hitObject = eventData.pointerCurrentRaycast.gameObject;
            MMOInventorySlot targetSlot = null;

            if (hitObject != null)
            {
                targetSlot = hitObject.GetComponentInParent<MMOInventorySlot>();
            }

            // Уведомляем менеджер
            manager?.EndDrag(targetSlot);

            Debug.Log($"[MMOSlot] 🖱️ Завершено перетаскивание. Target: {(targetSlot != null ? $"слот {targetSlot.slotIndex}" : "null")}");
        }

        // ═══════════════════════════════════════════
        // HOVER & TOOLTIP
        // ═══════════════════════════════════════════

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsEmpty())
            {
                // Показываем highlight
                if (highlightBorder != null)
                {
                    highlightBorder.SetActive(true);
                }

                // TODO: Показать tooltip с описанием предмета
                Debug.Log($"[MMOSlot] 🔍 Hover: {currentItem.itemName} x{currentQuantity}");
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Убираем highlight
            if (highlightBorder != null)
            {
                highlightBorder.SetActive(false);
            }

            // TODO: Скрыть tooltip
        }

        // ═══════════════════════════════════════════
        // CLICK (ПКМ для удаления)
        // ═══════════════════════════════════════════

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsEmpty())
                return;

            // ПКМ - удалить предмет
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                // Показываем окно подтверждения
                bool confirmed = true; // TODO: Показать диалог подтверждения

                if (confirmed)
                {
                    manager?.RemoveItem(slotIndex);
                    Debug.Log($"[MMOSlot] 🗑️ Удалён предмет: {currentItem.itemName} из слота {slotIndex}");
                }
            }

            // Shift+ЛКМ - разделить стак (TODO)
            if (eventData.button == PointerEventData.InputButton.Left && Input.GetKey(KeyCode.LeftShift))
            {
                Debug.Log($"[MMOSlot] ✂️ Разделение стака (TODO)");
            }
        }
    }
}
