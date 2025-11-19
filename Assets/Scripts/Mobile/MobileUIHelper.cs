using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper скрипт для настройки Canvas приоритетов для мобильных контролов
/// Обеспечивает правильную работу кликов по кнопкам скиллов
/// Использование: Добавь этот скрипт на Canvas со скилл-баром
/// </summary>
[RequireComponent(typeof(Canvas))]
public class MobileUIHelper : MonoBehaviour
{
    [Header("Canvas Settings")]
    [Tooltip("Приоритет сортировки Canvas (чем выше, тем выше в иерархии)")]
    [SerializeField] private int sortingOrder = 100;

    [Header("Raycast Settings")]
    [Tooltip("Блокировать раycasts на нижележащих слоях")]
    [SerializeField] private bool blockRaycasts = true;

    [Header("Auto-Setup")]
    [Tooltip("Автоматически настроить GraphicRaycaster при старте")]
    [SerializeField] private bool autoSetupRaycaster = true;

    [Tooltip("Автоматически настроить все Button компоненты")]
    [SerializeField] private bool autoSetupButtons = true;

    private Canvas canvas;
    private GraphicRaycaster graphicRaycaster;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        graphicRaycaster = GetComponent<GraphicRaycaster>();

        if (graphicRaycaster == null)
        {
            graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            Debug.Log($"[MobileUIHelper] ✅ Добавлен GraphicRaycaster к {gameObject.name}");
        }

        SetupCanvas();

        if (autoSetupRaycaster)
        {
            SetupRaycaster();
        }

        if (autoSetupButtons)
        {
            SetupAllButtons();
        }
    }

    /// <summary>
    /// Настроить Canvas для правильной работы с мобильными контролами
    /// </summary>
    private void SetupCanvas()
    {
        // Устанавливаем sorting order
        canvas.sortingOrder = sortingOrder;

        // Проверяем render mode
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            Debug.Log($"[MobileUIHelper] Canvas '{gameObject.name}' в режиме Screen Space - Overlay (Order: {sortingOrder})");
        }
        else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Debug.Log($"[MobileUIHelper] Canvas '{gameObject.name}' в режиме Screen Space - Camera (Order: {sortingOrder})");

            if (canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
                Debug.Log("[MobileUIHelper] ⚠️ World Camera был null, установлен Main Camera");
            }
        }

        // Блокировка раycasts
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null && blockRaycasts)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            Debug.Log($"[MobileUIHelper] ✅ Добавлен CanvasGroup к {gameObject.name}");
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = blockRaycasts;
            canvasGroup.interactable = true;
        }
    }

    /// <summary>
    /// Настроить GraphicRaycaster для оптимальной работы
    /// </summary>
    private void SetupRaycaster()
    {
        if (graphicRaycaster == null) return;

        // Игнорируем Reversed Graphics (стандартная настройка)
        graphicRaycaster.ignoreReversedGraphics = true;

        // Блокируем Mask (если есть)
        graphicRaycaster.blockingMask = LayerMask.GetMask("UI");

        Debug.Log($"[MobileUIHelper] ✅ GraphicRaycaster настроен на {gameObject.name}");
    }

    /// <summary>
    /// Автоматически настроить все кнопки в этом Canvas
    /// </summary>
    private void SetupAllButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        int count = 0;
        foreach (Button button in buttons)
        {
            // Проверяем что кнопка interactable
            if (!button.interactable)
            {
                button.interactable = true;
                count++;
            }

            // Проверяем что у кнопки есть Image с raycastTarget
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null && !buttonImage.raycastTarget)
            {
                buttonImage.raycastTarget = true;
                count++;
            }
        }

        Debug.Log($"[MobileUIHelper] ✅ Проверено {buttons.Length} кнопок, исправлено {count}");
    }

    /// <summary>
    /// Публичный метод для установки sorting order во runtime
    /// </summary>
    public void SetSortingOrder(int order)
    {
        sortingOrder = order;
        if (canvas != null)
        {
            canvas.sortingOrder = order;
            Debug.Log($"[MobileUIHelper] Sorting order изменён на {order}");
        }
    }

    /// <summary>
    /// Публичный метод для переключения блокировки raycast
    /// </summary>
    public void SetBlockRaycasts(bool block)
    {
        blockRaycasts = block;
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = block;
        }
    }

    /// <summary>
    /// Debug: Вывести информацию о Canvas
    /// </summary>
    [ContextMenu("Debug Canvas Info")]
    public void DebugCanvasInfo()
    {
        Debug.Log("=== CANVAS INFO ===");
        Debug.Log($"Name: {gameObject.name}");
        Debug.Log($"Render Mode: {canvas.renderMode}");
        Debug.Log($"Sorting Order: {canvas.sortingOrder}");
        Debug.Log($"World Camera: {(canvas.worldCamera != null ? canvas.worldCamera.name : "NULL")}");

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            Debug.Log($"Blocks Raycasts: {canvasGroup.blocksRaycasts}");
            Debug.Log($"Interactable: {canvasGroup.interactable}");
        }

        if (graphicRaycaster != null)
        {
            Debug.Log($"GraphicRaycaster: ✓");
        }
        else
        {
            Debug.Log($"GraphicRaycaster: ✗ MISSING!");
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        Debug.Log($"Buttons found: {buttons.Length}");
        Debug.Log("===================");
    }
}
