using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// UI слот для скилла (Drag & Drop)
/// </summary>
public class SkillSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI Components")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private TextMeshProUGUI hotkeyText;
    [SerializeField] private TextMeshProUGUI skillNameText; // Название скилла
    [SerializeField] private TextMeshProUGUI emptyText; // Текст "Пусто"
    [SerializeField] private TextMeshProUGUI keyBindText; // Альтернатива hotkeyText

    [Header("Settings")]
    [SerializeField] private bool isEquipSlot = false; // true = слот экипировки (3 штуки), false = слот библиотеки (6 штук)
    [SerializeField] private bool isLibrarySlot = true; // Для обратной совместимости с editor скриптом
    [SerializeField] private int slotIndex = 0;
    [SerializeField] private SkillSelectionManager skillSelectionManager; // Менеджер выбора скиллов

    private SkillData currentSkill;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private GameObject dragIcon; // Иконка при перетаскивании

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        rectTransform = GetComponent<RectTransform>();

        // Настраиваем иконку для растягивания на всю ячейку
        if (iconImage != null)
        {
            // Preserve Aspect = false, чтобы иконка растягивалась
            iconImage.preserveAspect = false;

            // Растягиваем на всю ячейку (anchors = stretch)
            RectTransform iconRect = iconImage.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.anchorMin = Vector2.zero; // (0, 0)
                iconRect.anchorMax = Vector2.one;   // (1, 1)
                iconRect.offsetMin = Vector2.zero;  // Left = 0, Bottom = 0
                iconRect.offsetMax = Vector2.zero;  // Right = 0, Top = 0
            }
        }

        // По умолчанию пусто
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = 0f;
        }
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Установить скилл в слот
    /// </summary>
    public void SetSkill(SkillData skill)
    {
        currentSkill = skill;

        if (skill != null)
        {
            Debug.Log($"[SkillSlotUI] SetSkill: {skill.skillName}, иконка: {(skill.icon != null ? "✓" : "❌ NULL")}");

            // Показываем иконку
            if (iconImage != null)
            {
                iconImage.sprite = skill.icon;
                iconImage.enabled = true;
                iconImage.color = Color.white;

                if (skill.icon == null)
                {
                    Debug.LogWarning($"[SkillSlotUI] ⚠️ У скилла '{skill.skillName}' нет иконки! Назначь Sprite в поле 'icon'");
                }
            }
            else
            {
                Debug.LogWarning("[SkillSlotUI] ⚠️ iconImage не назначен в Inspector!");
            }

            // Показываем название скилла
            if (skillNameText != null)
            {
                skillNameText.text = skill.skillName;
                skillNameText.gameObject.SetActive(true);
            }

            // Скрываем текст "Пусто"
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(false);
            }

            // Горячая клавиша (только для экипированных слотов)
            if (hotkeyText != null && isEquipSlot)
            {
                hotkeyText.text = $"{slotIndex + 1}";
                hotkeyText.gameObject.SetActive(true);
            }
            if (keyBindText != null && !isLibrarySlot)
            {
                keyBindText.text = $"{slotIndex + 1}";
                keyBindText.gameObject.SetActive(true);
            }
        }
        else
        {
            // Очищаем слот
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (skillNameText != null)
            {
                skillNameText.text = "";
                skillNameText.gameObject.SetActive(false);
            }

            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(true);
            }

            if (hotkeyText != null)
            {
                hotkeyText.gameObject.SetActive(false);
            }

            if (keyBindText != null)
            {
                keyBindText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Получить текущий скилл
    /// </summary>
    public SkillData GetSkill()
    {
        return currentSkill;
    }

    /// <summary>
    /// Очистить слот
    /// </summary>
    public void ClearSlot()
    {
        SetSkill(null);
    }

    /// <summary>
    /// Обновить кулдаун (вызывается из SkillSelectionManager)
    /// </summary>
    public void UpdateCooldown(float remaining, float total)
    {
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = remaining / total;
        }

        if (cooldownText != null)
        {
            if (remaining > 0f)
            {
                cooldownText.text = $"{remaining:F1}";
                cooldownText.gameObject.SetActive(true);
            }
            else
            {
                cooldownText.gameObject.SetActive(false);
            }
        }
    }

    // ===== DRAG & DROP =====

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentSkill == null) return;

        Debug.Log($"[SkillSlotUI] Начало перетаскивания: {currentSkill.skillName}");

        // Создаём временную иконку для перетаскивания
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform);
        dragIcon.transform.SetAsLastSibling();

        Image dragImage = dragIcon.AddComponent<Image>();
        dragImage.sprite = currentSkill.icon;
        dragImage.raycastTarget = false;

        RectTransform dragRect = dragIcon.GetComponent<RectTransform>();
        dragRect.sizeDelta = rectTransform.sizeDelta;
        dragRect.position = eventData.position;

        // Полупрозрачность оригинала
        canvasGroup.alpha = 0.5f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            dragIcon.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"[SkillSlotUI] Конец перетаскивания");

        // Удаляем временную иконку
        if (dragIcon != null)
        {
            Destroy(dragIcon);
        }

        // Восстанавливаем прозрачность
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"[SkillSlotUI] Drop событие");

        // Получаем перетаскиваемый слот
        SkillSlotUI draggedSlot = eventData.pointerDrag?.GetComponent<SkillSlotUI>();
        if (draggedSlot == null) return;

        // Меняем скиллы местами
        SkillData tempSkill = currentSkill;
        SetSkill(draggedSlot.GetSkill());
        draggedSlot.SetSkill(tempSkill);

        // Уведомляем менеджер о смене
        SkillSelectionManager manager = FindObjectOfType<SkillSelectionManager>();
        if (manager != null)
        {
            manager.OnSkillsChanged();
        }

        Debug.Log($"[SkillSlotUI] Скиллы обменены!");
    }

    /// <summary>
    /// Установить индекс слота
    /// </summary>
    public void SetSlotIndex(int index)
    {
        slotIndex = index;
    }

    /// <summary>
    /// Это слот экипировки?
    /// </summary>
    public bool IsEquipSlot()
    {
        return isEquipSlot;
    }
}
