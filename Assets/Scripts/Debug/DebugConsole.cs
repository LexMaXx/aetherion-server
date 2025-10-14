using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-game debug console for viewing logs in .exe builds
/// Press F12 to toggle console visibility
/// </summary>
public class DebugConsole : MonoBehaviour
{
    [Header("Console Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F12;
    [SerializeField] private int maxLogCount = 500;
    [SerializeField] private bool showOnStart = false;
    [SerializeField] private bool captureStackTraces = true;

    [Header("UI References (Auto-created if null)")]
    [SerializeField] private Canvas consoleCanvas;
    [SerializeField] private Text logText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject consolePanel;

    [Header("Filtering")]
    [SerializeField] private bool showLogs = true;
    [SerializeField] private bool showWarnings = true;
    [SerializeField] private bool showErrors = true;
    [SerializeField] private string filterTag = ""; // e.g., "[NetworkSync]"

    // Log storage
    private List<LogEntry> logs = new List<LogEntry>();
    private bool isVisible = false;
    private bool needsUpdate = false;

    private struct LogEntry
    {
        public string message;
        public string stackTrace;
        public LogType type;
        public System.DateTime timestamp;
    }

    void Awake()
    {
        // Singleton pattern - only one debug console
        if (FindObjectsByType<DebugConsole>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        // Subscribe to Unity log messages
        Application.logMessageReceived += HandleLog;

        // Log initialization
        Debug.Log($"[DebugConsole] ✅ Initialized! Press {toggleKey} to open console");
        Debug.Log($"[DebugConsole] Alternative keys: BackQuote (`), F11, F12");

        // Create UI if not assigned
        if (consoleCanvas == null)
        {
            CreateConsoleUI();
        }
        else
        {
            Debug.Log("[DebugConsole] Using existing Canvas from Inspector");
        }

        SetVisibility(showOnStart);
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void Update()
    {
        // Toggle console visibility - multiple key options
        if (Input.GetKeyDown(toggleKey) ||
            Input.GetKeyDown(KeyCode.BackQuote) ||  // ` key
            Input.GetKeyDown(KeyCode.F11))
        {
            ToggleConsole();
            Debug.Log($"[DebugConsole] Console toggled! isVisible = {isVisible}");
        }

        // Update UI if needed
        if (needsUpdate && isVisible)
        {
            UpdateLogDisplay();
            needsUpdate = false;
        }
    }

    /// <summary>
    /// Handle incoming log messages from Unity
    /// </summary>
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Apply type filter
        if (!ShouldShowLogType(type))
            return;

        // Apply tag filter
        if (!string.IsNullOrEmpty(filterTag) && !logString.Contains(filterTag))
            return;

        // Add to log list
        LogEntry entry = new LogEntry
        {
            message = logString,
            stackTrace = captureStackTraces ? stackTrace : "",
            type = type,
            timestamp = System.DateTime.Now
        };

        logs.Add(entry);

        // Limit log count
        if (logs.Count > maxLogCount)
        {
            logs.RemoveAt(0);
        }

        needsUpdate = true;
    }

    /// <summary>
    /// Check if log type should be shown based on filters
    /// </summary>
    private bool ShouldShowLogType(LogType type)
    {
        switch (type)
        {
            case LogType.Log:
                return showLogs;
            case LogType.Warning:
                return showWarnings;
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                return showErrors;
            default:
                return true;
        }
    }

    /// <summary>
    /// Toggle console visibility
    /// </summary>
    public void ToggleConsole()
    {
        SetVisibility(!isVisible);
    }

    /// <summary>
    /// Set console visibility
    /// </summary>
    public void SetVisibility(bool visible)
    {
        isVisible = visible;
        Debug.Log($"[DebugConsole] SetVisibility called: {visible}, consolePanel = {(consolePanel != null ? "exists" : "NULL")}");

        // ИСПРАВЛЕНО: НЕ отключаем Canvas! Только скрываем панель.
        // Canvas должен быть всегда включен, иначе он блокирует другие UI элементы

        if (consolePanel != null)
        {
            consolePanel.SetActive(visible);
            Debug.Log($"[DebugConsole] ConsolePanel.SetActive({visible}) called");
        }
        else
        {
            Debug.LogError("[DebugConsole] ❌ ConsolePanel is NULL! UI не создан!");
        }

        if (visible)
        {
            needsUpdate = true;
        }
    }

    /// <summary>
    /// Update the log display text
    /// </summary>
    private void UpdateLogDisplay()
    {
        if (logText == null)
            return;

        StringBuilder sb = new StringBuilder();

        // Build log text with color coding
        foreach (LogEntry entry in logs)
        {
            string color = GetColorForLogType(entry.type);
            string timestamp = entry.timestamp.ToString("HH:mm:ss");

            sb.AppendLine($"<color={color}>[{timestamp}] {entry.message}</color>");

            // Optionally show stack trace for errors
            if (entry.type == LogType.Error || entry.type == LogType.Exception)
            {
                if (!string.IsNullOrEmpty(entry.stackTrace))
                {
                    sb.AppendLine($"<color=#888888>{entry.stackTrace}</color>");
                }
            }
        }

        logText.text = sb.ToString();

        // Auto-scroll to bottom
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// Get color for log type
    /// </summary>
    private string GetColorForLogType(LogType type)
    {
        switch (type)
        {
            case LogType.Log:
                return "#FFFFFF"; // White
            case LogType.Warning:
                return "#FFFF00"; // Yellow
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                return "#FF0000"; // Red
            default:
                return "#FFFFFF";
        }
    }

    /// <summary>
    /// Create console UI programmatically
    /// </summary>
    private void CreateConsoleUI()
    {
        // Create canvas
        GameObject canvasObj = new GameObject("DebugConsoleCanvas");
        canvasObj.transform.SetParent(transform);
        consoleCanvas = canvasObj.AddComponent<Canvas>();
        consoleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        consoleCanvas.sortingOrder = 500; // ИСПРАВЛЕНО: Ниже чем было (было 1000), но все еще поверх игрового UI
        // НЕ отключаем Canvas.enabled - вместо этого скрываем только панель!

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
        raycaster.ignoreReversedGraphics = true;
        raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

        // Create background panel
        GameObject panelObj = new GameObject("ConsolePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        consolePanel = panelObj;

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 0.5f); // Bottom half of screen
        panelRect.offsetMin = new Vector2(10, 10);
        panelRect.offsetMax = new Vector2(-10, -10);

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.85f); // Semi-transparent black

        // Create scroll view
        GameObject scrollViewObj = new GameObject("ScrollView");
        scrollViewObj.transform.SetParent(panelObj.transform, false);

        RectTransform scrollViewRect = scrollViewObj.AddComponent<RectTransform>();
        scrollViewRect.anchorMin = Vector2.zero;
        scrollViewRect.anchorMax = Vector2.one;
        scrollViewRect.offsetMin = new Vector2(10, 50); // Leave space for buttons
        scrollViewRect.offsetMax = new Vector2(-10, -10);

        scrollRect = scrollViewObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        // Create viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollViewObj.transform, false);

        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;

        Image viewportMask = viewportObj.AddComponent<Image>();
        viewportMask.color = Color.clear;

        Mask mask = viewportObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        scrollRect.viewport = viewportRect;

        // Create content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);

        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 1000);

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;

        // Create log text
        GameObject textObj = new GameObject("LogText");
        textObj.transform.SetParent(contentObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);

        logText = textObj.AddComponent<Text>();
        logText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        logText.fontSize = 12;
        logText.color = Color.white;
        logText.alignment = TextAnchor.UpperLeft;
        logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        logText.verticalOverflow = VerticalWrapMode.Overflow;
        logText.supportRichText = true;

        // Create header text
        GameObject headerObj = new GameObject("HeaderText");
        headerObj.transform.SetParent(panelObj.transform, false);

        RectTransform headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.sizeDelta = new Vector2(0, 40);
        headerRect.anchoredPosition = new Vector2(0, -5);

        Text headerText = headerObj.AddComponent<Text>();
        headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        headerText.fontSize = 16;
        headerText.fontStyle = FontStyle.Bold;
        headerText.color = Color.yellow;
        headerText.alignment = TextAnchor.MiddleCenter;
        headerText.text = $"DEBUG CONSOLE (Press {toggleKey} to toggle) | Logs: {logs.Count}/{maxLogCount}";

        // Create clear button
        CreateButton(panelObj, "ClearButton", "Clear Logs", new Vector2(100, 10), new Vector2(150, 35), ClearLogs);

        // Create filter buttons
        CreateToggleButton(panelObj, "LogsToggle", "Logs", new Vector2(260, 10), new Vector2(100, 35), ref showLogs);
        CreateToggleButton(panelObj, "WarningsToggle", "Warnings", new Vector2(370, 10), new Vector2(100, 35), ref showWarnings);
        CreateToggleButton(panelObj, "ErrorsToggle", "Errors", new Vector2(480, 10), new Vector2(100, 35), ref showErrors);

        Debug.Log("[DebugConsole] ✅ UI created programmatically!");
        Debug.Log($"[DebugConsole] consoleCanvas = {consoleCanvas != null}, consolePanel = {consolePanel != null}");
        Debug.Log($"[DebugConsole] logText = {logText != null}, scrollRect = {scrollRect != null}");
    }

    /// <summary>
    /// Create a button
    /// </summary>
    private void CreateButton(GameObject parent, string name, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent.transform, false);

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0, 0);
        buttonRect.anchorMax = new Vector2(0, 0);
        buttonRect.pivot = new Vector2(0, 0);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = size;

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        Button button = buttonObj.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 12;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = label;
    }

    /// <summary>
    /// Create a toggle button
    /// </summary>
    private void CreateToggleButton(GameObject parent, string name, string label, Vector2 position, Vector2 size, ref bool toggleValue)
    {
        bool localToggle = toggleValue;
        CreateButton(parent, name, $"{label}: ON", position, size, () =>
        {
            localToggle = !localToggle;
            needsUpdate = true;
        });
    }

    /// <summary>
    /// Clear all logs
    /// </summary>
    public void ClearLogs()
    {
        logs.Clear();
        needsUpdate = true;
        Debug.Log("[DebugConsole] Logs cleared");
    }

    /// <summary>
    /// Set filter tag (e.g., "[NetworkSync]")
    /// </summary>
    public void SetFilterTag(string tag)
    {
        filterTag = tag;
        Debug.Log($"[DebugConsole] Filter set to: {tag}");
    }

    /// <summary>
    /// Show console on GUI
    /// </summary>
    void OnGUI()
    {
        if (!isVisible)
            return;

        // Update header with current log count
        if (consolePanel != null)
        {
            Text headerText = consolePanel.GetComponentInChildren<Text>();
            if (headerText != null)
            {
                headerText.text = $"DEBUG CONSOLE (Press {toggleKey} to toggle) | Logs: {logs.Count}/{maxLogCount}" +
                    (string.IsNullOrEmpty(filterTag) ? "" : $" | Filter: {filterTag}");
            }
        }
    }
}
