using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// UI слот для скилла (Drag & Drop + Touch Click + Visual Feedback)
/// </summary>
public class SkillSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
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
    [SerializeField] private bool isEquipSlot = false; // true = слот экипировки (5 штук), false = слот библиотеки (6 штук)
    [SerializeField] private bool isLibrarySlot = true; // Для обратной совместимости с editor скриптом
    [SerializeField] private int slotIndex = 0;
    [SerializeField] private SkillSelectionManager skillSelectionManager; // Менеджер выбора скиллов

    // DUAL SYSTEM: Поддержка как старой (SkillData), так и новой (SkillConfig) системы
    private SkillData currentSkill; // OLD SYSTEM (deprecated)
    private SkillConfig currentSkillConfig; // NEW SYSTEM (primary)

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private GameObject dragIcon; // Иконка при перетаскивании

    // Button component для гарантированного клика даже через MobileControlsCanvas
    private Button buttonComponent;

    // ===== PUBLIC МЕТОДЫ ДЛЯ BUTTON =====

    /// <summary>
    /// Активация скилла при клике на Button (для Unity Inspector)
    /// </summary>
    public void UseSkill()
    {
        Debug.Log($"[SkillSlotUI] 🔘 UseSkill() вызван для слота {slotIndex}");

        if (currentSkill == null && currentSkillConfig == null)
        {
            Debug.Log("[SkillSlotUI] Слот пуст");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            SkillManager skillManager = player.GetComponentInChildren<SkillManager>();
            if (skillManager != null)
            {
                skillManager.UseSkill(slotIndex);
#if UNITY_ANDROID || UNITY_IOS
                Handheld.Vibrate();
#endif
            }
        }
    }

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

    void Start()
    {
        Debug.Log($"[SkillSlotUI] Start() вызван для {gameObject.name} (слот {slotIndex})");

        // Настраиваем Button для всех слотов
        SetupButtonComponent();
    }

    /// <summary>
    /// Настраивает Button component для обработки кликов (работает даже если MobileControlsCanvas перекрывает)
    /// </summary>
    private void SetupButtonComponent()
    {
        // Получаем или добавляем Button
        buttonComponent = GetComponent<Button>();
        if (buttonComponent == null)
        {
            buttonComponent = gameObject.AddComponent<Button>();
            Debug.Log($"[SkillSlotUI] ✅ Добавлен Button к {gameObject.name} (слот {slotIndex})");
        }

        // Убираем стандартные визуальные переходы Button (мы используем свои через OnPointerDown/Up)
        buttonComponent.transition = Selectable.Transition.None;
        buttonComponent.interactable = true;

        // Подписываемся на событие клика (используем простой метод UseSkill)
        buttonComponent.onClick.RemoveAllListeners();
        buttonComponent.onClick.AddListener(UseSkill);

        Debug.Log($"[SkillSlotUI] ✅ Button настроен: {gameObject.name} → UseSkill()");
    }

    /// <summary>
    /// Обработчик клика через Button (работает гарантированно даже через MobileControlsCanvas)
    /// ВАЖНО: PUBLIC чтобы Unity видел метод в Inspector для Button.onClick
    /// </summary>
    public void OnButtonClick()
    {
        Debug.Log($"[SkillSlotUI] 🔘 Button.onClick вызван для слота {slotIndex}");
        ActivateSkill();
    }

    /// <summary>
    /// Простой метод активации для Button.onClick (для Unity Inspector)
    /// </summary>
    public void ClickSkill()
    {
        Debug.Log($"[SkillSlotUI] 🔘 ClickSkill вызван для слота {slotIndex}");
        ActivateSkill();
    }

    /// <summary>
    /// Активация скилла (вынесено в отдельный метод для использования и через Button, и через IPointerClickHandler)
    /// </summary>
    private void ActivateSkill()
    {
        // Проверяем что есть скилл в слоте
        if (currentSkill == null && currentSkillConfig == null)
        {
            Debug.Log("[SkillSlotUI] Слот пуст, нечего активировать");
            return;
        }

        // Находим SkillManager на игроке
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[SkillSlotUI] Игрок не найден! Tag 'Player' не назначен.");
            return;
        }

        SkillManager skillManager = player.GetComponentInChildren<SkillManager>();
        if (skillManager == null)
        {
            Debug.LogError("[SkillSlotUI] SkillManager не найден на игроке!");
            return;
        }

        // Получаем ID скилла
        int skillId = currentSkillConfig != null ? currentSkillConfig.skillId : currentSkill.skillId;
        string skillName = currentSkillConfig != null ? currentSkillConfig.skillName : currentSkill.skillName;

        Debug.Log($"[SkillSlotUI] 🎯 Активация скилла: {skillName} (ID: {skillId}, Slot: {slotIndex})");

        // Активируем скилл через SkillManager
        skillManager.UseSkill(slotIndex);

        // Тактильная обратная связь (вибрация) для мобильных
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// Установить скилл в слот (OLD SYSTEM: SkillData)
    /// </summary>
    public void SetSkill(SkillData skill)
    {
        currentSkill = skill;
        currentSkillConfig = null; // Очищаем новую систему

        if (skill != null)
        {
            Debug.Log($"[SkillSlotUI] SetSkill (OLD): {skill.skillName}, иконка: {(skill.icon != null ? "✓" : "❌ NULL")}");

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
            ClearSlotInternal();
        }
    }

    /// <summary>
    /// Установить скилл в слот (NEW SYSTEM: SkillConfig)
    /// </summary>
    public void SetSkill(SkillConfig skill)
    {
        currentSkillConfig = skill;
        currentSkill = null; // Очищаем старую систему

        if (skill != null)
        {
            Debug.Log($"[SkillSlotUI] SetSkill (NEW): {skill.skillName} (ID: {skill.skillId}), иконка: {(skill.icon != null ? "✓" : "❌ NULL")}");

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
            ClearSlotInternal();
        }
    }

    /// <summary>
    /// Внутренний метод очистки слота
    /// </summary>
    private void ClearSlotInternal()
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

    /// <summary>
    /// Получить текущий скилл (OLD SYSTEM: SkillData)
    /// </summary>
    public SkillData GetSkill()
    {
        return currentSkill;
    }

    /// <summary>
    /// Получить текущий скилл (NEW SYSTEM: SkillConfig)
    /// </summary>
    public SkillConfig GetSkillConfig()
    {
        return currentSkillConfig;
    }

    /// <summary>
    /// Очистить слот
    /// </summary>
    public void ClearSlot()
    {
        currentSkill = null;
        currentSkillConfig = null;
        ClearSlotInternal();
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
        // DUAL SYSTEM: Проверяем обе системы
        if (currentSkill == null && currentSkillConfig == null) return;

        string skillName = currentSkill != null ? currentSkill.skillName : currentSkillConfig.skillName;
        Sprite icon = currentSkill != null ? currentSkill.icon : currentSkillConfig.icon;

        Debug.Log($"[SkillSlotUI] Начало перетаскивания: {skillName}");

        // Создаём временную иконку для перетаскивания
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform);
        dragIcon.transform.SetAsLastSibling();

        Image dragImage = dragIcon.AddComponent<Image>();
        dragImage.sprite = icon;
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

        // DUAL SYSTEM: Меняем скиллы местами (NEW system приоритетнее)
        if (draggedSlot.currentSkillConfig != null || currentSkillConfig != null)
        {
            // NEW SYSTEM
            SkillConfig tempSkill = currentSkillConfig;
            SetSkill(draggedSlot.GetSkillConfig());
            draggedSlot.SetSkill(tempSkill);
        }
        else
        {
            // OLD SYSTEM (fallback)
            SkillData tempSkill = currentSkill;
            SetSkill(draggedSlot.GetSkill());
            draggedSlot.SetSkill(tempSkill);
        }

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

    // ===== TOUCH/CLICK ACTIVATION =====

    /// <summary>
    /// Обработка клика/касания по иконке скилла (для мобильных устройств)
    /// </summary>
    /// <summary>
    /// IPointerClickHandler - альтернативный способ активации (работает если Button не срабатывает)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[SkillSlotUI] 👆 OnPointerClick вызван для {gameObject.name} (слот {slotIndex})");
        ActivateSkill();
    }

    // ===== VISUAL FEEDBACK =====

    private Color originalColor;
    private bool isPressed = false;

    /// <summary>
    /// Обработка нажатия (визуальная обратная связь)
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        // Только если есть скилл в слоте
        if (currentSkill == null && currentSkillConfig == null)
        {
            return;
        }

        isPressed = true;

        // Затемняем иконку при нажатии
        if (iconImage != null)
        {
            originalColor = iconImage.color;
            iconImage.color = new Color(originalColor.r * 0.7f, originalColor.g * 0.7f, originalColor.b * 0.7f, originalColor.a);
        }

        // Масштабируем слегка (эффект нажатия)
        transform.localScale = Vector3.one * 0.95f;
    }

    /// <summary>
    /// Обработка отпускания (возврат к нормальному виду)
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed)
        {
            return;
        }

        isPressed = false;

        // Возвращаем оригинальный цвет
        if (iconImage != null)
        {
            iconImage.color = originalColor;
        }

        // Возвращаем масштаб
        transform.localScale = Vector3.one;
    }
}
