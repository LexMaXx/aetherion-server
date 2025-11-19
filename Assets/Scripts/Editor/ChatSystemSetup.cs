using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Автоматический установщик системы чата в Unity
/// Создает все префабы и UI элементы одной кнопкой
/// </summary>
public class ChatSystemSetup : EditorWindow
{
    private const string PREFABS_PATH = "Assets/Prefabs/UI/";
    private const string CHAT_MESSAGE_PREFAB_NAME = "ChatMessagePrefab.prefab";
    private const string CHAT_BUBBLE_PREFAB_NAME = "ChatBubblePrefab.prefab";

    [MenuItem("Aetherion/Setup Chat System")]
    public static void ShowWindow()
    {
        GetWindow<ChatSystemSetup>("Chat System Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Chat System Automatic Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Этот инструмент автоматически создаст:\n" +
            "✅ ChatMessagePrefab (элемент сообщения)\n" +
            "✅ ChatBubblePrefab (всплывающее сообщение)\n" +
            "✅ ChatPanel в BattleScene\n" +
            "✅ ChatBubbleManager в BattleScene\n\n" +
            "Убедитесь что BattleScene открыта!",
            MessageType.Info
        );

        GUILayout.Space(10);

        if (GUILayout.Button("🚀 Установить систему чата", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog(
                "Установка системы чата",
                "Начать автоматическую установку?\n\nЭто создаст все необходимые префабы и UI элементы.",
                "Да, установить",
                "Отмена"))
            {
                SetupChatSystem();
            }
        }

        GUILayout.Space(10);

        GUILayout.Label("Дополнительные опции:", EditorStyles.boldLabel);
        GUILayout.Space(5);

        if (GUILayout.Button("🔄 Переустановить (удалить старое + создать новое)", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Переустановка",
                "Это удалит старые ChatPanel и ChatBubbleManager и создаст новые.\nПродолжить?",
                "Да", "Отмена"))
            {
                CleanupOldChatObjects();
                SetupChatSystem();
            }
        }

        GUILayout.Space(5);

        if (GUILayout.Button("📦 Создать только префабы", GUILayout.Height(30)))
        {
            CreatePrefabsOnly();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("🎨 Создать только UI в сцене", GUILayout.Height(30)))
        {
            SetupSceneUI();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("🗑️ Удалить чат из сцены", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Удаление",
                "Это удалит ChatPanel и ChatBubbleManager из сцены.\nПрефабы останутся.\nПродолжить?",
                "Да", "Отмена"))
            {
                CleanupOldChatObjects();
                Scene activeScene = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                EditorUtility.DisplayDialog("Готово", "Чат удален из сцены!", "OK");
            }
        }
    }

    /// <summary>
    /// Полная установка системы чата
    /// </summary>
    private static void SetupChatSystem()
    {
        Debug.Log("[ChatSetup] 🚀 Начинаем установку системы чата...");

        // Проверяем что BattleScene открыта
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "BattleScene")
        {
            EditorUtility.DisplayDialog(
                "Ошибка",
                "Откройте BattleScene перед установкой!",
                "OK"
            );
            return;
        }

        // Создаем папку для префабов если её нет
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }

        // 1. Создаем префабы
        GameObject chatMessagePrefab = CreateChatMessagePrefab();
        GameObject chatBubblePrefab = CreateChatBubblePrefab();

        // 2. Создаем UI в сцене
        GameObject chatPanel = CreateChatPanelInScene(chatMessagePrefab);
        GameObject chatBubbleManager = CreateChatBubbleManagerInScene(chatBubblePrefab);

        // Сохраняем сцену
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log("[ChatSetup] ✅ Установка завершена!");

        EditorUtility.DisplayDialog(
            "Успех!",
            "Система чата успешно установлена!\n\n" +
            "✅ ChatMessagePrefab создан\n" +
            "✅ ChatBubblePrefab создан\n" +
            "✅ ChatPanel добавлен в сцену\n" +
            "✅ ChatBubbleManager добавлен в сцену\n\n" +
            "Теперь запустите игру и нажмите Enter для открытия чата!",
            "Отлично!"
        );

        // Выделяем ChatPanel в Hierarchy
        Selection.activeGameObject = chatPanel;
        EditorGUIUtility.PingObject(chatPanel);
    }

    /// <summary>
    /// Создать только префабы без UI в сцене
    /// </summary>
    private static void CreatePrefabsOnly()
    {
        Debug.Log("[ChatSetup] 📦 Создаем префабы...");

        CreateChatMessagePrefab();
        CreateChatBubblePrefab();

        Debug.Log("[ChatSetup] ✅ Префабы созданы!");
        EditorUtility.DisplayDialog("Готово", "Префабы успешно созданы!", "OK");
    }

    /// <summary>
    /// Создать только UI в сцене (префабы должны существовать)
    /// </summary>
    private static void SetupSceneUI()
    {
        Debug.Log("[ChatSetup] 🎨 Создаем UI в сцене...");

        // Проверяем что BattleScene открыта
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "BattleScene")
        {
            EditorUtility.DisplayDialog("Ошибка", "Откройте BattleScene!", "OK");
            return;
        }

        // Удаляем старые объекты если они есть
        CleanupOldChatObjects();

        // Загружаем префабы
        GameObject chatMessagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS_PATH + CHAT_MESSAGE_PREFAB_NAME);
        GameObject chatBubblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS_PATH + CHAT_BUBBLE_PREFAB_NAME);

        if (chatMessagePrefab == null || chatBubblePrefab == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Префабы не найдены! Сначала создайте префабы.", "OK");
            return;
        }

        CreateChatPanelInScene(chatMessagePrefab);
        CreateChatBubbleManagerInScene(chatBubblePrefab);

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log("[ChatSetup] ✅ UI создан!");
        EditorUtility.DisplayDialog("Готово", "UI успешно создан в сцене!", "OK");
    }

    // ═══════════════════════════════════════════════════════════════
    // ОЧИСТКА
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Удалить старые объекты чата из сцены
    /// </summary>
    private static void CleanupOldChatObjects()
    {
        Debug.Log("[ChatSetup] 🧹 Очистка старых объектов...");

        // Ищем Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            // Удаляем ChatPanel если он существует
            Transform chatPanel = canvas.transform.Find("ChatPanel");
            if (chatPanel != null)
            {
                Debug.Log("[ChatSetup] 🗑️ Удаляем старый ChatPanel...");
                DestroyImmediate(chatPanel.gameObject);
            }
        }

        // Удаляем ChatBubbleManager если он существует
        ChatBubbleManager oldBubbleManager = FindObjectOfType<ChatBubbleManager>();
        if (oldBubbleManager != null)
        {
            Debug.Log("[ChatSetup] 🗑️ Удаляем старый ChatBubbleManager...");
            DestroyImmediate(oldBubbleManager.gameObject);
        }

        Debug.Log("[ChatSetup] ✅ Очистка завершена!");
    }

    // ═══════════════════════════════════════════════════════════════
    // СОЗДАНИЕ ПРЕФАБОВ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Создать ChatMessagePrefab (элемент сообщения в чате)
    /// </summary>
    private static GameObject CreateChatMessagePrefab()
    {
        Debug.Log("[ChatSetup] 📝 Создаем ChatMessagePrefab...");

        // Создаем временный Canvas для создания UI
        GameObject tempCanvas = new GameObject("TempCanvas");
        Canvas canvas = tempCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Создаем Panel для сообщения
        GameObject messagePanel = new GameObject("ChatMessagePrefab");
        messagePanel.transform.SetParent(tempCanvas.transform, false);

        RectTransform rectTransform = messagePanel.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(380, 30);

        // Добавляем TextMeshPro
        GameObject textObj = new GameObject("MessageText");
        textObj.transform.SetParent(messagePanel.transform, false);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.overflowMode = TextOverflowModes.Overflow;
        text.enableWordWrapping = true;

        RectTransform textRect = textObj.GetComponent<RectTransform>();  // TextMeshProUGUI автоматически добавляет RectTransform
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(5, 2);
        textRect.offsetMax = new Vector2(-5, -2);

        // Сохраняем как префаб
        string prefabPath = PREFABS_PATH + CHAT_MESSAGE_PREFAB_NAME;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(messagePanel, prefabPath);

        // Удаляем временные объекты
        DestroyImmediate(tempCanvas);

        Debug.Log($"[ChatSetup] ✅ ChatMessagePrefab создан: {prefabPath}");
        return prefab;
    }

    /// <summary>
    /// Создать ChatBubblePrefab (всплывающее сообщение над головой)
    /// </summary>
    private static GameObject CreateChatBubblePrefab()
    {
        Debug.Log("[ChatSetup] 💬 Создаем ChatBubblePrefab...");

        // Создаем World Space Canvas
        GameObject bubbleCanvas = new GameObject("ChatBubblePrefab");
        Canvas canvas = bubbleCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = bubbleCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(300, 80);
        canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        CanvasScaler scaler = bubbleCanvas.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        // Добавляем CanvasGroup для анимации
        CanvasGroup canvasGroup = bubbleCanvas.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Создаем фон (черный Image)
        GameObject backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(bubbleCanvas.transform, false);

        Image background = backgroundObj.AddComponent<Image>();  // Image автоматически добавляет RectTransform
        background.color = new Color(0, 0, 0, 0.78f); // Черный с прозрачностью

        RectTransform bgRect = backgroundObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(5, 5);
        bgRect.offsetMax = new Vector2(-5, -5);

        // Создаем текст (белый TextMeshPro)
        GameObject textObj = new GameObject("MessageText");
        textObj.transform.SetParent(bubbleCanvas.transform, false);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();  // TextMeshProUGUI автоматически добавляет RectTransform
        text.fontSize = 18;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.overflowMode = TextOverflowModes.Overflow;
        text.enableWordWrapping = true;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);

        // Добавляем скрипт ChatBubble
        ChatBubble chatBubble = bubbleCanvas.AddComponent<ChatBubble>();

        // Используем reflection для установки private полей
        var messageTextField = typeof(ChatBubble).GetField("messageText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var canvasGroupField = typeof(ChatBubble).GetField("canvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var backgroundRectField = typeof(ChatBubble).GetField("backgroundRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (messageTextField != null) messageTextField.SetValue(chatBubble, text);
        if (canvasGroupField != null) canvasGroupField.SetValue(chatBubble, canvasGroup);
        if (backgroundRectField != null) backgroundRectField.SetValue(chatBubble, bgRect);

        // Сохраняем как префаб
        string prefabPath = PREFABS_PATH + CHAT_BUBBLE_PREFAB_NAME;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bubbleCanvas, prefabPath);

        // Удаляем временный объект
        DestroyImmediate(bubbleCanvas);

        Debug.Log($"[ChatSetup] ✅ ChatBubblePrefab создан: {prefabPath}");
        return prefab;
    }

    // ═══════════════════════════════════════════════════════════════
    // СОЗДАНИЕ UI В СЦЕНЕ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Создать ChatPanel в BattleScene
    /// </summary>
    private static GameObject CreateChatPanelInScene(GameObject chatMessagePrefab)
    {
        Debug.Log("[ChatSetup] 🎨 Создаем ChatPanel в сцене...");

        // Находим Canvas в сцене
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ChatSetup] ❌ Canvas не найден в сцене!");
            return null;
        }

        // Проверяем не существует ли уже ChatPanel
        Transform existingChatPanel = canvas.transform.Find("ChatPanel");
        if (existingChatPanel != null)
        {
            Debug.LogWarning("[ChatSetup] ⚠️ ChatPanel уже существует! Пропускаем создание.");
            return existingChatPanel.gameObject;
        }

        // Создаем ChatPanel
        GameObject chatPanel = new GameObject("ChatPanel");
        chatPanel.transform.SetParent(canvas.transform, false);

        Image panelImage = chatPanel.AddComponent<Image>();  // Image автоматически добавляет RectTransform
        panelImage.color = new Color(0, 0, 0, 0.59f); // Полупрозрачный черный

        RectTransform panelRect = chatPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 0);
        panelRect.pivot = new Vector2(0, 0);
        panelRect.anchoredPosition = new Vector2(10, 10);
        panelRect.sizeDelta = new Vector2(400, 250);

        // Создаем TabsContainer
        GameObject tabsContainer = new GameObject("TabsContainer");
        tabsContainer.transform.SetParent(chatPanel.transform, false);

        RectTransform tabsRect = tabsContainer.AddComponent<RectTransform>();
        tabsRect.anchorMin = new Vector2(0, 1);
        tabsRect.anchorMax = new Vector2(1, 1);
        tabsRect.pivot = new Vector2(0.5f, 0.5f);
        tabsRect.anchoredPosition = new Vector2(0, -17.5f);
        tabsRect.sizeDelta = new Vector2(-10, 35);

        // Создаем AllChatTab
        GameObject allTab = CreateTabButton(tabsContainer.transform, "AllChatTab", "ВСЕ", new Vector2(50, 0), true);

        // Создаем PartyChatTab
        GameObject partyTab = CreateTabButton(tabsContainer.transform, "PartyChatTab", "ГРУППА", new Vector2(160, 0), false);

        // Создаем ScrollView
        GameObject scrollView = CreateScrollView(chatPanel.transform);

        // Создаем InputField
        GameObject inputField = CreateInputField(chatPanel.transform);

        // Создаем SendButton
        GameObject sendButton = CreateSendButton(chatPanel.transform);

        // ВАЖНО: Убеждаемся что ChatPanel активен (GameObject должен быть активным для работы ChatManager)
        chatPanel.SetActive(true);

        // Добавляем CanvasGroup для показа/скрытия чата
        CanvasGroup canvasGroup = chatPanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f; // СКРЫТ по умолчанию (невидимый)
        canvasGroup.interactable = false; // Нельзя взаимодействовать
        canvasGroup.blocksRaycasts = false; // Не блокирует клики

        // Добавляем ChatManager
        ChatManager chatManager = chatPanel.AddComponent<ChatManager>();

        // Назначаем ссылки через reflection
        AssignChatManagerReferences(chatManager, chatPanel, canvasGroup, inputField, sendButton, scrollView, chatMessagePrefab, allTab, partyTab);

        // Принудительно помечаем объект как измененный
        EditorUtility.SetDirty(chatPanel);
        EditorUtility.SetDirty(chatManager);

        Debug.Log("[ChatSetup] ✅ ChatPanel создан в сцене и установлен активным!");
        return chatPanel;
    }

    /// <summary>
    /// Создать ChatBubbleManager в сцене
    /// </summary>
    private static GameObject CreateChatBubbleManagerInScene(GameObject chatBubblePrefab)
    {
        Debug.Log("[ChatSetup] 💬 Создаем ChatBubbleManager...");

        // Проверяем не существует ли уже
        ChatBubbleManager existing = FindObjectOfType<ChatBubbleManager>();
        if (existing != null)
        {
            Debug.LogWarning("[ChatSetup] ⚠️ ChatBubbleManager уже существует!");
            return existing.gameObject;
        }

        GameObject manager = new GameObject("ChatBubbleManager");
        ChatBubbleManager bubbleManager = manager.AddComponent<ChatBubbleManager>();

        // Назначаем префаб через reflection
        var prefabField = typeof(ChatBubbleManager).GetField("chatBubblePrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (prefabField != null)
        {
            prefabField.SetValue(bubbleManager, chatBubblePrefab);
        }

        Debug.Log("[ChatSetup] ✅ ChatBubbleManager создан!");
        return manager;
    }

    // ═══════════════════════════════════════════════════════════════
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ═══════════════════════════════════════════════════════════════

    private static GameObject CreateTabButton(Transform parent, string name, string text, Vector2 position, bool highlightActive)
    {
        GameObject button = new GameObject(name);
        button.transform.SetParent(parent, false);

        Image buttonImage = button.AddComponent<Image>();  // Image автоматически добавляет RectTransform
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        Button buttonComponent = button.AddComponent<Button>();

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0, 0.5f);
        buttonRect.anchorMax = new Vector2(0, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(100, 30);

        // Создаем текст кнопки
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(button.transform, false);

        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();  // TextMeshProUGUI автоматически добавляет RectTransform
        textComponent.text = text;
        textComponent.fontSize = 14;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Создаем Highlight
        GameObject highlight = new GameObject("Highlight");
        highlight.transform.SetParent(button.transform, false);

        Image highlightImage = highlight.AddComponent<Image>();  // Image автоматически добавляет RectTransform
        highlightImage.color = new Color(1f, 1f, 0f, 0.39f); // Желтый

        RectTransform highlightRect = highlight.GetComponent<RectTransform>();
        highlightRect.anchorMin = Vector2.zero;
        highlightRect.anchorMax = Vector2.one;
        highlightRect.offsetMin = new Vector2(2, 2);
        highlightRect.offsetMax = new Vector2(-2, -2);

        highlight.SetActive(highlightActive);

        return button;
    }

    private static GameObject CreateScrollView(Transform parent)
    {
        GameObject scrollView = new GameObject("ChatScrollView");
        scrollView.transform.SetParent(parent, false);

        RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.offsetMin = new Vector2(5, 40);
        scrollRect.offsetMax = new Vector2(-5, -45);

        Image scrollImage = scrollView.AddComponent<Image>();
        scrollImage.color = new Color(0, 0, 0, 0.2f);

        ScrollRect scrollRectComponent = scrollView.AddComponent<ScrollRect>();
        scrollRectComponent.horizontal = false;
        scrollRectComponent.vertical = true;
        scrollRectComponent.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);

        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = Color.clear;

        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 2;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRectComponent.viewport = viewportRect;
        scrollRectComponent.content = contentRect;

        return scrollView;
    }

    private static GameObject CreateInputField(Transform parent)
    {
        GameObject inputField = new GameObject("MessageInputField");
        inputField.transform.SetParent(parent, false);

        RectTransform inputRect = inputField.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0, 0);
        inputRect.anchorMax = new Vector2(1, 0);
        inputRect.pivot = new Vector2(0.5f, 0.5f);
        inputRect.anchoredPosition = new Vector2(0, 15);
        inputRect.sizeDelta = new Vector2(-85, 30);

        Image inputImage = inputField.AddComponent<Image>();
        inputImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        TMP_InputField inputFieldComponent = inputField.AddComponent<TMP_InputField>();
        inputFieldComponent.characterLimit = 200;

        // Text Area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputField.transform, false);

        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(5, 2);
        textAreaRect.offsetMax = new Vector2(-5, -2);

        RectMask2D mask = textArea.AddComponent<RectMask2D>();

        // Placeholder
        GameObject placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(textArea.transform, false);

        TextMeshProUGUI placeholderText = placeholder.AddComponent<TextMeshProUGUI>();  // TextMeshProUGUI автоматически добавляет RectTransform
        placeholderText.text = "Введите сообщение...";
        placeholderText.fontSize = 14;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholderText.fontStyle = FontStyles.Italic;

        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;

        // Text
        GameObject text = new GameObject("Text");
        text.transform.SetParent(textArea.transform, false);

        TextMeshProUGUI textComponent = text.AddComponent<TextMeshProUGUI>();  // TextMeshProUGUI автоматически добавляет RectTransform
        textComponent.fontSize = 14;
        textComponent.color = Color.white;

        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        inputFieldComponent.textViewport = textAreaRect;
        inputFieldComponent.textComponent = textComponent;
        inputFieldComponent.placeholder = placeholderText;

        return inputField;
    }

    private static GameObject CreateSendButton(Transform parent)
    {
        GameObject button = new GameObject("SendButton");
        button.transform.SetParent(parent, false);

        RectTransform buttonRect = button.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1, 0);
        buttonRect.anchorMax = new Vector2(1, 0);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(-42.5f, 15);
        buttonRect.sizeDelta = new Vector2(75, 30);

        Image buttonImage = button.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);

        Button buttonComponent = button.AddComponent<Button>();

        // Текст кнопки
        GameObject text = new GameObject("Text");
        text.transform.SetParent(button.transform, false);

        TextMeshProUGUI textComponent = text.AddComponent<TextMeshProUGUI>();  // TextMeshProUGUI автоматически добавляет RectTransform
        textComponent.text = "Отправить";
        textComponent.fontSize = 12;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private static void AssignChatManagerReferences(ChatManager chatManager, GameObject chatPanel, CanvasGroup canvasGroup, GameObject inputField, GameObject sendButton, GameObject scrollView, GameObject messagePrefab, GameObject allTab, GameObject partyTab)
    {
        var type = typeof(ChatManager);

        // Назначаем через reflection (так как поля SerializeField private)
        Debug.Log($"[ChatSetup] 📋 Назначаем ссылки в ChatManager:");
        SetField(type, chatManager, "chatPanel", chatPanel);
        SetField(type, chatManager, "chatCanvasGroup", canvasGroup);
        SetField(type, chatManager, "messageInputField", inputField.GetComponent<TMP_InputField>());
        SetField(type, chatManager, "sendButton", sendButton.GetComponent<Button>());
        SetField(type, chatManager, "chatScrollRect", scrollView.GetComponent<ScrollRect>());
        SetField(type, chatManager, "chatContentContainer", scrollView.transform.Find("Viewport/Content"));
        SetField(type, chatManager, "chatMessagePrefab", messagePrefab);
        SetField(type, chatManager, "allChatTab", allTab.GetComponent<Button>());
        SetField(type, chatManager, "partyChatTab", partyTab.GetComponent<Button>());
        SetField(type, chatManager, "allChatTabHighlight", allTab.transform.Find("Highlight").gameObject);
        SetField(type, chatManager, "partyChatTabHighlight", partyTab.transform.Find("Highlight").gameObject);
        SetField(type, chatManager, "maxMessagesInChat", 50);
        SetField(type, chatManager, "messageDisplayTime", 5f);
        SetField(type, chatManager, "autoHideDelay", 5f);
        SetField(type, chatManager, "allChatColor", Color.white);
        SetField(type, chatManager, "partyChatColor", Color.green);

        Debug.Log("[ChatSetup] ✅ ChatManager - все ссылки назначены!");

        // Проверка что все назначилось корректно
        var chatPanelField = type.GetField("chatPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (chatPanelField != null)
        {
            var value = chatPanelField.GetValue(chatManager);
            Debug.Log($"[ChatSetup] 🔍 Проверка: chatPanel = {(value != null ? "НАЗНАЧЕН" : "NULL!!!")}");
        }
    }

    private static void SetField(System.Type type, object instance, string fieldName, object value)
    {
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(instance, value);
            Debug.Log($"[ChatSetup]   ✅ {fieldName} = {(value != null ? value.ToString() : "null")}");
        }
        else
        {
            Debug.LogWarning($"[ChatSetup] ⚠️ Поле {fieldName} не найдено в типе {type.Name}!");
        }
    }
}
