using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// Автоматический установщик меню настроек графики - ВЕРСИЯ 2.8
/// Все UI элементы создаются с правильной конфигурацией
///
/// Новое в v2.8:
/// - Добавлены настройки агрессивной оптимизации для Android
/// - Toggle для включения/выключения оптимизации
/// - Dropdown для выбора пресета (Balanced/Performance/Ultra Performance)
/// - Интеграция с PerformanceOptimizer.cs
///
/// Исправления v2.7:
/// - LabelRow в слайдерах: добавлена фиксированная высота 25px
/// - LabelRow: добавлены minHeight и preferredHeight
/// - Label и Value текст: raycastTarget = false (не блокируют слайдер)
/// - HorizontalLayoutGroup: childForceExpandHeight = false
/// - Теперь слайдеры доступны для редактирования без перекрытия текстом
/// </summary>
public class GraphicsSettingsSetup : EditorWindow
{
    [MenuItem("Aetherion/Setup Graphics Settings")]
    public static void ShowWindow()
    {
        GetWindow<GraphicsSettingsSetup>("Graphics Settings Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Установка меню настроек графики", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Улучшенная система создания меню:\n" +
            "• Корректные Dropdown с рабочими шаблонами\n" +
            "• Правильно настроенные Toggle\n" +
            "• Функциональные Slider с визуальным фидбеком\n" +
            "• Адаптивный дизайн",
            MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("🚀 Установить меню настроек графики", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Установка",
                "Это создаст полнофункциональное меню настроек графики в BattleScene.\nПродолжить?",
                "Да", "Отмена"))
            {
                SetupGraphicsSettings();
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button("🗑️ Удалить меню настроек", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Удаление",
                "Это удалит меню настроек графики из сцены.\nПродолжить?",
                "Да", "Отмена"))
            {
                CleanupGraphicsSettings();
            }
        }
    }

    private static void SetupGraphicsSettings()
    {
        Debug.Log("[GraphicsSettingsSetup] 🚀 Начинаем установку меню настроек графики...");

        // Проверяем что BattleScene открыта
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "BattleScene")
        {
            EditorUtility.DisplayDialog("Ошибка", "Откройте BattleScene перед установкой!", "OK");
            return;
        }

        // Удаляем старое меню если оно есть
        CleanupGraphicsSettings();

        // Находим Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Canvas не найден в сцене!", "OK");
            return;
        }

        // Проверяем наличие EventSystem
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
            Debug.Log("[GraphicsSettingsSetup] ✅ EventSystem создан!");
        }

        // Создаём меню настроек
        GameObject settingsPanel = CreateSettingsPanel(canvas.transform);

        // Создаём кнопку открытия настроек
        GameObject openButton = CreateOpenSettingsButton(canvas.transform, settingsPanel.GetComponent<GraphicsSettingsManager>());

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log("[GraphicsSettingsSetup] ✅ Меню настроек графики создано!");
        EditorUtility.DisplayDialog("Готово",
            "Меню настроек графики успешно создано!\n\n" +
            "• Кнопка 'Настройки' в правом верхнем углу\n" +
            "• Или нажмите F1 для открытия/закрытия меню\n" +
            "• Все элементы полностью функциональны",
            "OK");
    }

    private static void CleanupGraphicsSettings()
    {
        Debug.Log("[GraphicsSettingsSetup] 🧹 Очистка старого меню настроек...");

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            Transform settingsPanel = canvas.transform.Find("GraphicsSettingsPanel");
            if (settingsPanel != null)
            {
                Debug.Log("[GraphicsSettingsSetup] 🗑️ Удаляем старое меню настроек...");
                DestroyImmediate(settingsPanel.gameObject);
            }

            Transform openButton = canvas.transform.Find("OpenSettingsButton");
            if (openButton != null)
            {
                Debug.Log("[GraphicsSettingsSetup] 🗑️ Удаляем старую кнопку открытия...");
                DestroyImmediate(openButton.gameObject);
            }
        }

        GraphicsSettingsManager oldManager = Object.FindFirstObjectByType<GraphicsSettingsManager>();
        if (oldManager != null && oldManager.transform.parent == null)
        {
            Debug.Log("[GraphicsSettingsSetup] 🗑️ Удаляем старый GraphicsSettingsManager...");
            DestroyImmediate(oldManager.gameObject);
        }

        Debug.Log("[GraphicsSettingsSetup] ✅ Очистка завершена!");
    }

    private static GameObject CreateSettingsPanel(Transform parent)
    {
        // Создаём главную панель с улучшенным дизайном
        GameObject panel = new GameObject("GraphicsSettingsPanel", typeof(RectTransform));
        panel.transform.SetParent(parent, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(600, 750);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.96f);
        panelImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        panelImage.type = Image.Type.Sliced;

        // Outline для красоты
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.6f, 1f, 0.5f);
        outline.effectDistance = new Vector2(2, -2);

        // Добавляем Shadow для глубины
        Shadow shadow = panel.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.8f);
        shadow.effectDistance = new Vector2(4, -4);

        // Добавляем CanvasGroup
        CanvasGroup canvasGroup = panel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Создаём декоративную рамку
        CreateDecorationFrame(panel.transform);

        // Создаём заголовок
        CreateTitle(panel.transform);

        // Создаём ScrollView с настройками
        GameObject scrollView = CreateSettingsScrollView(panel.transform);

        // Создаём кнопки
        CreateButtons(panel.transform);

        // Создаём FPS Counter
        GameObject fpsCounter = CreateFPSCounter(parent);

        // Добавляем GraphicsSettingsManager
        GraphicsSettingsManager manager = panel.AddComponent<GraphicsSettingsManager>();
        AssignReferences(manager, panel, canvasGroup, scrollView, fpsCounter);

        panel.SetActive(true);

        Debug.Log("[GraphicsSettingsSetup] ✅ GraphicsSettingsPanel создан с улучшенным дизайном!");
        return panel;
    }

    private static void CreateDecorationFrame(Transform parent)
    {
        // Верхняя декоративная линия
        GameObject topLine = new GameObject("TopDecoration", typeof(RectTransform));
        topLine.transform.SetParent(parent, false);

        RectTransform topRect = topLine.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot = new Vector2(0.5f, 1);
        topRect.anchoredPosition = Vector2.zero;
        topRect.sizeDelta = new Vector2(-20, 3);

        Image topImage = topLine.AddComponent<Image>();
        topImage.color = new Color(0.2f, 0.7f, 1f, 0.8f);
    }

    private static void CreateTitle(Transform parent)
    {
        GameObject title = new GameObject("Title", typeof(RectTransform));
        title.transform.SetParent(parent, false);

        RectTransform rect = title.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -15);
        rect.sizeDelta = new Vector2(-40, 50);

        TextMeshProUGUI text = title.AddComponent<TextMeshProUGUI>();
        text.text = "НАСТРОЙКИ ГРАФИКИ";
        text.fontSize = 26;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(0.9f, 0.95f, 1f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;

        // Добавляем Outline для заголовка
        Outline outline = title.AddComponent<Outline>();
        outline.effectColor = new Color(0.1f, 0.4f, 0.8f, 1f);
        outline.effectDistance = new Vector2(2, -2);
    }

    private static GameObject CreateSettingsScrollView(Transform parent)
    {
        // ScrollView
        GameObject scrollView = new GameObject("ScrollView", typeof(RectTransform));
        scrollView.transform.SetParent(parent, false);

        RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.anchoredPosition = Vector2.zero;
        scrollRect.offsetMin = new Vector2(25, 85); // Bottom margin
        scrollRect.offsetMax = new Vector2(-25, -75); // Top margin

        ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 15f; // Для корректной работы touch-жестов
        scroll.movementType = ScrollRect.MovementType.Elastic; // Плавная прокрутка с возвратом
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;

        // Viewport
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(scrollView.transform, false);

        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        RectMask2D mask = viewport.AddComponent<RectMask2D>();

        // Content
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 18;
        layout.padding = new RectOffset(15, 15, 15, 15);

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Создаём вертикальный Scrollbar
        GameObject scrollbar = new GameObject("Scrollbar Vertical", typeof(RectTransform));
        scrollbar.transform.SetParent(scrollView.transform, false);

        RectTransform scrollbarRect = scrollbar.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 1);
        scrollbarRect.sizeDelta = new Vector2(20, 0);
        scrollbarRect.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Image scrollbarBg = scrollbar.AddComponent<UnityEngine.UI.Image>();
        scrollbarBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        Scrollbar scrollbarComponent = scrollbar.AddComponent<Scrollbar>();
        scrollbarComponent.direction = Scrollbar.Direction.BottomToTop;

        // Создаём Sliding Area
        GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbar.transform, false);

        RectTransform slidingAreaRect = slidingArea.GetComponent<RectTransform>();
        slidingAreaRect.anchorMin = Vector2.zero;
        slidingAreaRect.anchorMax = Vector2.one;
        slidingAreaRect.offsetMin = new Vector2(5, 5);
        slidingAreaRect.offsetMax = new Vector2(-5, -5);

        // Создаём Handle
        GameObject handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(slidingArea.transform, false);

        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;

        UnityEngine.UI.Image handleImage = handle.AddComponent<UnityEngine.UI.Image>();
        handleImage.color = new Color(0.4f, 0.4f, 0.4f, 1f);

        scrollbarComponent.handleRect = handleRect;
        scrollbarComponent.targetGraphic = handleImage;

        // Подключаем scrollbar к ScrollRect
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.verticalScrollbar = scrollbarComponent;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        // Корректируем viewport чтобы учесть scrollbar
        viewportRect.offsetMax = new Vector2(-25, 0); // Отступ справа для scrollbar

        // Добавляем настройки
        AddQualitySection(content.transform);
        AddGraphicsSection(content.transform);
        AddViewDistanceSection(content.transform);
        AddPerformanceSection(content.transform);

        return scrollView;
    }

    private static void AddQualitySection(Transform parent)
    {
        CreateSectionHeader(parent, "КАЧЕСТВО");
        CreateDropdownSetting(parent, "QualityDropdown", "Качество графики:");
        CreateDropdownSetting(parent, "ResolutionDropdown", "Разрешение:");
        CreateToggleSetting(parent, "FullscreenToggle", "Полноэкранный режим");
        CreateToggleSetting(parent, "VSyncToggle", "Вертикальная синхронизация");
    }

    private static void AddGraphicsSection(Transform parent)
    {
        CreateSectionHeader(parent, "ГРАФИКА");
        CreateDropdownSetting(parent, "ShadowQualityDropdown", "Качество теней:");
        CreateDropdownSetting(parent, "AntiAliasingDropdown", "Сглаживание (MSAA):");
        CreateDropdownSetting(parent, "VegetationQualityDropdown", "Качество растительности:");
        CreateSliderSetting(parent, "RenderScaleSlider", "Масштаб рендера:");
    }

    private static void AddViewDistanceSection(Transform parent)
    {
        CreateSectionHeader(parent, "ПРОРИСОВКА");
        CreateSliderSetting(parent, "EntityViewDistanceSlider", "Дальность прорисовки существ:");
        CreateSliderSetting(parent, "BuildingViewDistanceSlider", "Дальность прорисовки строений:");
        CreateSliderSetting(parent, "VegetationDrawDistanceSlider", "Дальность растительности:");
    }

    private static void AddPerformanceSection(Transform parent)
    {
        CreateSectionHeader(parent, "ПРОИЗВОДИТЕЛЬНОСТЬ");
        CreateDropdownSetting(parent, "TargetFPSDropdown", "Целевой FPS:");
        CreateToggleSetting(parent, "ShowFPSToggle", "Показывать FPS");
        CreateToggleSetting(parent, "DynamicPerformanceToggle", "Динамическое масштабирование (как L2M)");

        // Новые настройки агрессивной оптимизации
        CreateToggleSetting(parent, "AggressiveOptimizationToggle", "Агрессивная оптимизация (Android)");
        CreateDropdownSetting(parent, "OptimizationPresetDropdown", "Пресет оптимизации:");
    }

    private static void CreateSectionHeader(Transform parent, string title)
    {
        GameObject header = new GameObject(title + "_Header", typeof(RectTransform));
        header.transform.SetParent(parent, false);

        RectTransform rect = header.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 38);

        // Фоновый элемент для секции
        Image bgImage = header.AddComponent<Image>();
        bgImage.color = new Color(0.12f, 0.12f, 0.15f, 0.6f);
        bgImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        bgImage.type = Image.Type.Sliced;

        // Создаём отдельный объект для текста
        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(header.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "> " + title;
        text.fontSize = 19;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(0.3f, 0.85f, 1f);
        text.alignment = TextAlignmentOptions.Left;
        text.margin = new Vector4(10, 0, 0, 0);
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;

        // Outline для заголовков секций
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.05f, 0.2f, 0.4f, 1f);
        outline.effectDistance = new Vector2(1, -1);

        LayoutElement layout = header.AddComponent<LayoutElement>();
        layout.preferredHeight = 38;
        layout.minHeight = 38;
    }

    private static void CreateDropdownSetting(Transform parent, string name, string label)
    {
        GameObject setting = new GameObject(name + "_Setting", typeof(RectTransform));
        setting.transform.SetParent(parent, false);

        RectTransform settingRect = setting.GetComponent<RectTransform>();
        settingRect.sizeDelta = new Vector2(520, 40); // ДОБАВЛЕНО: минимальный размер

        HorizontalLayoutGroup hlayout = setting.AddComponent<HorizontalLayoutGroup>();
        hlayout.childForceExpandWidth = true;
        hlayout.childForceExpandHeight = false;
        hlayout.spacing = 10;

        LayoutElement layout = setting.AddComponent<LayoutElement>();
        layout.preferredHeight = 40;
        layout.preferredWidth = 520; // ДОБАВЛЕНО: предпочтительная ширина

        // Label
        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(setting.transform, false);

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 15;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.enableWordWrapping = false;
        labelText.overflowMode = TextOverflowModes.Overflow;

        LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 220;
        labelLayout.minWidth = 220;

        // Dropdown
        GameObject dropdown = new GameObject(name, typeof(RectTransform));
        dropdown.transform.SetParent(setting.transform, false);

        Image dropdownImage = dropdown.AddComponent<Image>();
        dropdownImage.color = new Color(0.18f, 0.18f, 0.18f, 1f);
        dropdownImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        dropdownImage.type = Image.Type.Sliced;

        RectTransform dropdownRect = dropdown.GetComponent<RectTransform>();
        dropdownRect.sizeDelta = new Vector2(150, 35); // ИСПРАВЛЕНО: было 0, теперь 150px минимум

        LayoutElement dropdownLayout = dropdown.AddComponent<LayoutElement>();
        dropdownLayout.flexibleWidth = 1;
        dropdownLayout.minWidth = 150;

        // Caption label
        GameObject captionObj = new GameObject("Label", typeof(RectTransform));
        captionObj.transform.SetParent(dropdown.transform, false);

        TextMeshProUGUI captionText = captionObj.AddComponent<TextMeshProUGUI>();
        captionText.text = "-";
        captionText.fontSize = 14;
        captionText.color = Color.white;
        captionText.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform captionRect = captionObj.GetComponent<RectTransform>();
        captionRect.anchorMin = Vector2.zero;
        captionRect.anchorMax = Vector2.one;
        captionRect.offsetMin = new Vector2(12, 2);
        captionRect.offsetMax = new Vector2(-30, -2);

        // Arrow indicator
        GameObject arrowObj = new GameObject("Arrow", typeof(RectTransform));
        arrowObj.transform.SetParent(dropdown.transform, false);

        TextMeshProUGUI arrowText = arrowObj.AddComponent<TextMeshProUGUI>();
        arrowText.text = "v";
        arrowText.fontSize = 12;
        arrowText.color = new Color(0.8f, 0.8f, 0.8f);
        arrowText.alignment = TextAlignmentOptions.Center;

        RectTransform arrowRect = arrowObj.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1, 0);
        arrowRect.anchorMax = new Vector2(1, 1);
        arrowRect.pivot = new Vector2(1, 0.5f);
        arrowRect.sizeDelta = new Vector2(25, 0);
        arrowRect.anchoredPosition = new Vector2(-5, 0);

        // Сначала создаём Template ПЕРЕД добавлением TMP_Dropdown
        var templateData = CreateDropdownTemplate(dropdown.transform);

        // Теперь добавляем TMP_Dropdown компонент
        TMP_Dropdown dropdownComponent = dropdown.AddComponent<TMP_Dropdown>();
        dropdownComponent.targetGraphic = dropdownImage;
        dropdownComponent.captionText = captionText;
        dropdownComponent.template = templateData.Item1; // RectTransform Template
        dropdownComponent.itemText = templateData.Item2; // TMP_Text itemLabel
        dropdownComponent.interactable = true; // ВАЖНО: делаем интерактивным

        // Transition настройки
        dropdownComponent.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = dropdownComponent.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 1f);
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        dropdownComponent.colors = colors;
    }

    private static System.Tuple<RectTransform, TMP_Text> CreateDropdownTemplate(Transform parent)
    {
        GameObject template = new GameObject("Template", typeof(RectTransform));
        template.transform.SetParent(parent, false);
        template.SetActive(false); // ВАЖНО: деактивируем СРАЗУ!

        RectTransform templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1);
        templateRect.anchoredPosition = new Vector2(0, 2);
        templateRect.sizeDelta = new Vector2(0, 200); // ИСПРАВЛЕНО: увеличена высота для видимости элементов

        Image templateImage = template.AddComponent<Image>();
        templateImage.color = new Color(0.12f, 0.12f, 0.12f, 0.98f);
        templateImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        templateImage.type = Image.Type.Sliced;

        // Viewport
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(template.transform, false);

        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(5, 5);
        viewportRect.offsetMax = new Vector2(-20, -5); // ИСПРАВЛЕНО: увеличен отступ справа для Scrollbar

        RectMask2D mask = viewport.AddComponent<RectMask2D>();

        // Content
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlayout = content.AddComponent<VerticalLayoutGroup>();
        vlayout.spacing = 2;
        vlayout.padding = new RectOffset(2, 2, 2, 2); // ДОБАВЛЕНО: отступы внутри Content
        vlayout.childControlWidth = false; // ИСПРАВЛЕНО: не контролируем ширину, Item сам установит
        vlayout.childControlHeight = true;
        vlayout.childForceExpandWidth = true; // Растягиваем по ширине Content
        vlayout.childForceExpandHeight = false;
        vlayout.childAlignment = TextAnchor.UpperLeft; // ДОБАВЛЕНО: выравнивание элементов

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ScrollRect
        ScrollRect scroll = template.AddComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.viewport = viewportRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 10f;

        // Scrollbar
        GameObject scrollbar = new GameObject("Scrollbar", typeof(RectTransform));
        scrollbar.transform.SetParent(template.transform, false);

        RectTransform scrollbarRect = scrollbar.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 0.5f);
        scrollbarRect.sizeDelta = new Vector2(15, 0);
        scrollbarRect.anchoredPosition = new Vector2(-2, 0);

        Image scrollbarBg = scrollbar.AddComponent<Image>();
        scrollbarBg.color = new Color(0.08f, 0.08f, 0.08f, 0.8f);

        Scrollbar scrollbarComponent = scrollbar.AddComponent<Scrollbar>();
        scrollbarComponent.direction = Scrollbar.Direction.BottomToTop;

        GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbar.transform, false);
        RectTransform slidingRect = slidingArea.GetComponent<RectTransform>();
        slidingRect.anchorMin = Vector2.zero;
        slidingRect.anchorMax = Vector2.one;
        slidingRect.offsetMin = new Vector2(2, 2);
        slidingRect.offsetMax = new Vector2(-2, -2);

        GameObject handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(slidingArea.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(10, 20);

        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.4f, 0.4f, 0.4f, 1f);

        scrollbarComponent.handleRect = handleRect;
        scrollbarComponent.targetGraphic = handleImage;

        scroll.verticalScrollbar = scrollbarComponent;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        // Item Template
        GameObject item = new GameObject("Item", typeof(RectTransform));
        item.transform.SetParent(content.transform, false);

        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0, 32); // ИСПРАВЛЕНО: ширина 0 т.к. растягивается по VerticalLayoutGroup

        // ДОБАВЛЕНО: LayoutElement для правильного размера
        LayoutElement itemLayout = item.AddComponent<LayoutElement>();
        itemLayout.minHeight = 32;
        itemLayout.preferredHeight = 32;
        itemLayout.flexibleWidth = 1; // Растягиваем по ширине

        Toggle toggle = item.AddComponent<Toggle>();
        toggle.toggleTransition = Toggle.ToggleTransition.Fade;
        toggle.isOn = false; // ДОБАВЛЕНО: по умолчанию не выбран
        toggle.interactable = true; // ДОБАВЛЕНО: делаем интерактивным
        toggle.navigation = Navigation.defaultNavigation; // ДОБАВЛЕНО: стандартная навигация

        // Item Background
        GameObject bg = new GameObject("Item Background", typeof(RectTransform));
        bg.transform.SetParent(item.transform, false);

        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        bgImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        bgImage.type = Image.Type.Sliced;

        toggle.targetGraphic = bgImage;

        // Настройка цветов для toggle
        ColorBlock toggleColors = toggle.colors;
        toggleColors.normalColor = new Color(1f, 1f, 1f, 0.7f);
        toggleColors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
        toggleColors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        toggleColors.selectedColor = new Color(1f, 1f, 1f, 1f);
        toggle.colors = toggleColors;

        // Item Checkmark
        GameObject checkmark = new GameObject("Item Checkmark", typeof(RectTransform));
        checkmark.transform.SetParent(item.transform, false);

        RectTransform checkRect = checkmark.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0, 0.5f);
        checkRect.anchorMax = new Vector2(0, 0.5f);
        checkRect.pivot = new Vector2(0, 0.5f);
        checkRect.anchoredPosition = new Vector2(8, 0);
        checkRect.sizeDelta = new Vector2(16, 16);

        Image checkmarkImage = checkmark.AddComponent<Image>();
        checkmarkImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        checkmarkImage.color = new Color(0.2f, 0.8f, 1f);

        toggle.graphic = checkmarkImage;

        // Item Label
        GameObject label = new GameObject("Item Label", typeof(RectTransform));
        label.transform.SetParent(item.transform, false);

        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(28, 2);
        labelRect.offsetMax = new Vector2(-5, -2);

        TextMeshProUGUI itemLabel = label.AddComponent<TextMeshProUGUI>();
        itemLabel.text = "Option";
        itemLabel.fontSize = 16; // УВЕЛИЧЕНО: было 14, теперь 16 для лучшей видимости
        itemLabel.color = Color.white;
        itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
        itemLabel.enableWordWrapping = false;
        itemLabel.overflowMode = TextOverflowModes.Ellipsis;
        itemLabel.raycastTarget = false; // ДОБАВЛЕНО: не блокируем raycast для Toggle

        // Возвращаем Tuple с RectTransform template и TMP_Text itemLabel
        return new System.Tuple<RectTransform, TMP_Text>(templateRect, itemLabel);
    }

    private static void CreateToggleSetting(Transform parent, string name, string label)
    {
        GameObject setting = new GameObject(name + "_Setting", typeof(RectTransform));
        setting.transform.SetParent(parent, false);

        RectTransform settingRect = setting.GetComponent<RectTransform>();
        settingRect.sizeDelta = new Vector2(520, 40); // ДОБАВЛЕНО: минимальный размер

        HorizontalLayoutGroup hlayout = setting.AddComponent<HorizontalLayoutGroup>();
        hlayout.childForceExpandWidth = false;
        hlayout.childForceExpandHeight = false;
        hlayout.spacing = 12;
        hlayout.childAlignment = TextAnchor.MiddleLeft;

        LayoutElement layout = setting.AddComponent<LayoutElement>();
        layout.preferredHeight = 40;
        layout.preferredWidth = 520; // ДОБАВЛЕНО: предпочтительная ширина

        // Toggle
        GameObject toggle = new GameObject(name, typeof(RectTransform));
        toggle.transform.SetParent(setting.transform, false);

        RectTransform toggleRect = toggle.GetComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(50, 26);

        Toggle toggleComponent = toggle.AddComponent<Toggle>();
        toggleComponent.toggleTransition = Toggle.ToggleTransition.Fade;
        toggleComponent.interactable = true; // ВАЖНО: делаем интерактивным

        // Background
        GameObject bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(toggle.transform, false);

        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        bgImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        bgImage.type = Image.Type.Sliced;

        toggleComponent.targetGraphic = bgImage;

        // Checkmark
        GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform));
        checkmark.transform.SetParent(bg.transform, false);

        RectTransform checkRect = checkmark.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(20, 20);

        Image checkImage = checkmark.AddComponent<Image>();
        checkImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        checkImage.color = new Color(0.2f, 1f, 0.3f, 1f);

        toggleComponent.graphic = checkImage;

        // Transition colors
        ColorBlock colors = toggleComponent.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 1f);
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.selectedColor = new Color(0.3f, 0.8f, 0.3f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        toggleComponent.colors = colors;

        // Label
        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(setting.transform, false);

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 15;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.enableWordWrapping = true;
        labelText.overflowMode = TextOverflowModes.Overflow;

        LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 400;
        labelLayout.flexibleWidth = 1;
    }

    private static void CreateSliderSetting(Transform parent, string name, string label)
    {
        GameObject setting = new GameObject(name + "_Setting", typeof(RectTransform));
        setting.transform.SetParent(parent, false);

        RectTransform settingRect = setting.GetComponent<RectTransform>();
        settingRect.sizeDelta = new Vector2(520, 65); // ДОБАВЛЕНО: минимальный размер

        VerticalLayoutGroup vlayout = setting.AddComponent<VerticalLayoutGroup>();
        vlayout.childForceExpandWidth = true;
        vlayout.childForceExpandHeight = false;
        vlayout.spacing = 6;

        LayoutElement layout = setting.AddComponent<LayoutElement>();
        layout.preferredHeight = 65;
        layout.preferredWidth = 520; // ДОБАВЛЕНО: предпочтительная ширина

        // Label Row
        GameObject labelRow = new GameObject("LabelRow", typeof(RectTransform));
        labelRow.transform.SetParent(setting.transform, false);

        RectTransform labelRowRect = labelRow.GetComponent<RectTransform>();
        labelRowRect.sizeDelta = new Vector2(0, 25); // ДОБАВЛЕНО: фиксированная высота чтобы не перекрывать слайдер

        LayoutElement labelRowLayout = labelRow.AddComponent<LayoutElement>();
        labelRowLayout.preferredHeight = 25; // ДОБАВЛЕНО: предпочтительная высота
        labelRowLayout.minHeight = 25; // ДОБАВЛЕНО: минимальная высота

        HorizontalLayoutGroup hlayout = labelRow.AddComponent<HorizontalLayoutGroup>();
        hlayout.childForceExpandWidth = true;
        hlayout.childForceExpandHeight = false; // ДОБАВЛЕНО: не растягивать по высоте
        hlayout.spacing = 5;

        // Label
        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(labelRow.transform, false);

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 15;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.enableWordWrapping = false;
        labelText.overflowMode = TextOverflowModes.Ellipsis;
        labelText.raycastTarget = false; // ДОБАВЛЕНО: не блокируем клики по слайдеру

        LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.flexibleWidth = 1;

        // Value Text
        GameObject valueObj = new GameObject(name + "Text", typeof(RectTransform));
        valueObj.transform.SetParent(labelRow.transform, false);

        TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
        valueText.text = "100%";
        valueText.fontSize = 15;
        valueText.color = new Color(0.3f, 1f, 0.3f);
        valueText.fontStyle = FontStyles.Bold;
        valueText.alignment = TextAlignmentOptions.MidlineRight;
        valueText.enableWordWrapping = false;
        valueText.overflowMode = TextOverflowModes.Overflow;
        valueText.raycastTarget = false; // ДОБАВЛЕНО: не блокируем клики по слайдеру

        LayoutElement valueLayout = valueObj.AddComponent<LayoutElement>();
        valueLayout.preferredWidth = 70;
        valueLayout.minWidth = 70;

        // Slider Container
        GameObject sliderContainer = new GameObject(name + "_Container", typeof(RectTransform));
        sliderContainer.transform.SetParent(setting.transform, false);

        RectTransform containerRect = sliderContainer.GetComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(200, 28); // ИСПРАВЛЕНО: было 0, теперь 200px минимум

        LayoutElement containerLayout = sliderContainer.AddComponent<LayoutElement>();
        containerLayout.preferredHeight = 28;
        containerLayout.flexibleWidth = 1;
        containerLayout.minWidth = 200; // ДОБАВЛЕНО: минимальная ширина

        // Slider
        GameObject slider = new GameObject(name, typeof(RectTransform));
        slider.transform.SetParent(sliderContainer.transform, false);

        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = new Vector2(5, 4);
        sliderRect.offsetMax = new Vector2(-5, -4);

        Slider sliderComponent = slider.AddComponent<Slider>();
        sliderComponent.minValue = 0f;
        sliderComponent.maxValue = 100f;
        sliderComponent.value = 50f;
        sliderComponent.wholeNumbers = false;
        sliderComponent.direction = Slider.Direction.LeftToRight;
        sliderComponent.interactable = true; // ВАЖНО: делаем интерактивным
        sliderComponent.navigation = Navigation.defaultNavigation; // ДОБАВЛЕНО: стандартная навигация

        // Background
        GameObject bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(slider.transform, false);

        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        bgImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        bgImage.type = Image.Type.Sliced;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(slider.transform, false);

        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);

        // Fill
        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.7f, 1f, 1f);
        fillImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fillImage.type = Image.Type.Sliced;

        sliderComponent.fillRect = fillRect;

        // Handle Slide Area
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(slider.transform, false);

        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, 0);
        handleAreaRect.offsetMax = new Vector2(-10, 0);

        // Handle
        GameObject handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(handleArea.transform, false);

        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(24, 24); // УВЕЛИЧЕНО: было 18, теперь 24 для удобного перетаскивания

        Image handleImage = handle.AddComponent<Image>();
        handleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        handleImage.color = Color.white;
        handleImage.raycastTarget = true; // ДОБАВЛЕНО: обязательно для взаимодействия

        sliderComponent.handleRect = handleRect;
        sliderComponent.targetGraphic = handleImage;

        // Transition settings
        sliderComponent.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = sliderComponent.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 1f);
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        sliderComponent.colors = colors;
    }

    private static void CreateButtons(Transform parent)
    {
        GameObject buttonsRow = new GameObject("ButtonsRow", typeof(RectTransform));
        buttonsRow.transform.SetParent(parent, false);

        HorizontalLayoutGroup hlayout = buttonsRow.AddComponent<HorizontalLayoutGroup>();
        hlayout.childForceExpandWidth = true;
        hlayout.childForceExpandHeight = false;
        hlayout.spacing = 15;
        hlayout.padding = new RectOffset(30, 30, 15, 15);

        RectTransform rowRect = buttonsRow.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0, 0);
        rowRect.anchorMax = new Vector2(1, 0);
        rowRect.pivot = new Vector2(0.5f, 0);
        rowRect.anchoredPosition = new Vector2(0, 15);
        rowRect.sizeDelta = new Vector2(0, 60);

        // Apply Button
        CreateButton(buttonsRow.transform, "ApplyButton", "Применить", new Color(0.2f, 0.75f, 0.2f));

        // Close Button
        CreateButton(buttonsRow.transform, "CloseButton", "Закрыть", new Color(0.75f, 0.2f, 0.2f));
    }

    private static void CreateButton(Transform parent, string name, string text, Color color)
    {
        GameObject button = new GameObject(name, typeof(RectTransform));
        button.transform.SetParent(parent, false);

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(0, 45);

        Image buttonImage = button.AddComponent<Image>();
        buttonImage.color = color;
        buttonImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        buttonImage.type = Image.Type.Sliced;

        Button buttonComponent = button.AddComponent<Button>();
        buttonComponent.targetGraphic = buttonImage;
        buttonComponent.interactable = true; // ВАЖНО: делаем интерактивным

        // Transition settings
        buttonComponent.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = buttonComponent.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 1f);
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = new Color(1f, 1f, 1f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        buttonComponent.colors = colors;

        // Shadow
        Shadow shadow = button.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(2, -2);

        // Text
        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(button.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 18;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.enableWordWrapping = false;
        buttonText.overflowMode = TextOverflowModes.Overflow;

        // Outline для текста кнопки
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(1, -1);
    }

    private static GameObject CreateFPSCounter(Transform parent)
    {
        GameObject fpsCounter = new GameObject("FPSCounter", typeof(RectTransform));
        fpsCounter.transform.SetParent(parent, false);

        TextMeshProUGUI text = fpsCounter.AddComponent<TextMeshProUGUI>();
        text.text = "FPS: 60";
        text.fontSize = 20;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.yellow;
        text.alignment = TextAlignmentOptions.TopRight;

        RectTransform rect = fpsCounter.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-20, -20);
        rect.sizeDelta = new Vector2(150, 40);

        fpsCounter.SetActive(false);

        return fpsCounter;
    }

    private static void AssignReferences(GraphicsSettingsManager manager, GameObject panel, CanvasGroup canvasGroup, GameObject scrollView, GameObject fpsCounter)
    {
        var type = typeof(GraphicsSettingsManager);

        // Основные ссылки
        SetField(type, manager, "settingsPanel", panel);
        SetField(type, manager, "settingsCanvasGroup", canvasGroup);
        SetField(type, manager, "settingsScrollRect", scrollView.GetComponent<ScrollRect>());

        // Кнопки
        Transform buttonsRow = panel.transform.Find("ButtonsRow");
        if (buttonsRow != null)
        {
            SetField(type, manager, "closeButton", buttonsRow.Find("CloseButton")?.GetComponent<Button>());
            SetField(type, manager, "applyButton", buttonsRow.Find("ApplyButton")?.GetComponent<Button>());
        }

        // Находим все настройки в ScrollView
        Transform content = scrollView.transform.Find("Viewport/Content");
        if (content != null)
        {
            // Quality Settings
            SetField(type, manager, "qualityDropdown", content.Find("QualityDropdown_Setting/QualityDropdown")?.GetComponent<TMP_Dropdown>());
            SetField(type, manager, "resolutionDropdown", content.Find("ResolutionDropdown_Setting/ResolutionDropdown")?.GetComponent<TMP_Dropdown>());
            SetField(type, manager, "fullscreenToggle", content.Find("FullscreenToggle_Setting/FullscreenToggle")?.GetComponent<Toggle>());
            SetField(type, manager, "vsyncToggle", content.Find("VSyncToggle_Setting/VSyncToggle")?.GetComponent<Toggle>());

            // Graphics Settings
            SetField(type, manager, "shadowQualityDropdown", content.Find("ShadowQualityDropdown_Setting/ShadowQualityDropdown")?.GetComponent<TMP_Dropdown>());
            SetField(type, manager, "antiAliasingDropdown", content.Find("AntiAliasingDropdown_Setting/AntiAliasingDropdown")?.GetComponent<TMP_Dropdown>());
            SetField(type, manager, "vegetationQualityDropdown", content.Find("VegetationQualityDropdown_Setting/VegetationQualityDropdown")?.GetComponent<TMP_Dropdown>());
            SetField(type, manager, "renderScaleSlider", content.Find("RenderScaleSlider_Setting/RenderScaleSlider_Container/RenderScaleSlider")?.GetComponent<Slider>());
            SetField(type, manager, "renderScaleText", content.Find("RenderScaleSlider_Setting/LabelRow/RenderScaleSliderText")?.GetComponent<TMP_Text>());
            SetField(type, manager, "entityViewDistanceSlider", content.Find("EntityViewDistanceSlider_Setting/EntityViewDistanceSlider_Container/EntityViewDistanceSlider")?.GetComponent<Slider>());
            SetField(type, manager, "entityViewDistanceText", content.Find("EntityViewDistanceSlider_Setting/LabelRow/EntityViewDistanceSliderText")?.GetComponent<TMP_Text>());
            SetField(type, manager, "buildingViewDistanceSlider", content.Find("BuildingViewDistanceSlider_Setting/BuildingViewDistanceSlider_Container/BuildingViewDistanceSlider")?.GetComponent<Slider>());
            SetField(type, manager, "buildingViewDistanceText", content.Find("BuildingViewDistanceSlider_Setting/LabelRow/BuildingViewDistanceSliderText")?.GetComponent<TMP_Text>());
            SetField(type, manager, "vegetationDrawDistanceSlider", content.Find("VegetationDrawDistanceSlider_Setting/VegetationDrawDistanceSlider_Container/VegetationDrawDistanceSlider")?.GetComponent<Slider>());
            SetField(type, manager, "vegetationDrawDistanceText", content.Find("VegetationDrawDistanceSlider_Setting/LabelRow/VegetationDrawDistanceSliderText")?.GetComponent<TMP_Text>());

            // Performance Settings
            SetField(type, manager, "targetFpsDropdown", content.Find("TargetFPSDropdown_Setting/TargetFPSDropdown")?.GetComponent<TMP_Dropdown>());
            SetField(type, manager, "showFpsToggle", content.Find("ShowFPSToggle_Setting/ShowFPSToggle")?.GetComponent<Toggle>());
            SetField(type, manager, "dynamicPerformanceToggle", content.Find("DynamicPerformanceToggle_Setting/DynamicPerformanceToggle")?.GetComponent<Toggle>());

            // Advanced Optimization Settings
            SetField(type, manager, "aggressiveOptimizationToggle", content.Find("AggressiveOptimizationToggle_Setting/AggressiveOptimizationToggle")?.GetComponent<Toggle>());
            SetField(type, manager, "optimizationPresetDropdown", content.Find("OptimizationPresetDropdown_Setting/OptimizationPresetDropdown")?.GetComponent<TMP_Dropdown>());
        }

        // FPS Counter
        SetField(type, manager, "fpsCounterText", fpsCounter.GetComponent<TMP_Text>());

        Debug.Log("[GraphicsSettingsSetup] ✅ Все ссылки назначены!");
    }

    private static void SetField(System.Type type, object instance, string fieldName, object value)
    {
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(instance, value);
            Debug.Log($"[GraphicsSettingsSetup]   ✅ {fieldName} = {(value != null ? value.ToString() : "null")}");
        }
        else
        {
            Debug.LogWarning($"[GraphicsSettingsSetup] ⚠️ Поле {fieldName} не найдено!");
        }
    }

    /// <summary>
    /// Создать кнопку открытия меню настроек
    /// </summary>
    private static GameObject CreateOpenSettingsButton(Transform parent, GraphicsSettingsManager settingsManager)
    {
        GameObject button = new GameObject("OpenSettingsButton", typeof(RectTransform));
        button.transform.SetParent(parent, false);

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1, 1);
        buttonRect.anchorMax = new Vector2(1, 1);
        buttonRect.pivot = new Vector2(1, 1);
        buttonRect.anchoredPosition = new Vector2(-15, -15);
        buttonRect.sizeDelta = new Vector2(140, 45);

        Image buttonImage = button.AddComponent<Image>();
        buttonImage.color = new Color(0.15f, 0.15f, 0.18f, 0.92f);
        buttonImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        buttonImage.type = Image.Type.Sliced;

        Button buttonComponent = button.AddComponent<Button>();
        buttonComponent.targetGraphic = buttonImage;
        buttonComponent.interactable = true; // ВАЖНО: делаем интерактивным

        // Transition settings
        buttonComponent.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = buttonComponent.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 1f);
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = new Color(1f, 1f, 1f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        buttonComponent.colors = colors;

        // Shadow
        Shadow shadow = button.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.6f);
        shadow.effectDistance = new Vector2(3, -3);

        // Назначаем действие на кнопку
        buttonComponent.onClick.AddListener(() => settingsManager.OpenSettings());

        // Создаем текст кнопки
        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(button.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = "Настройки";
        textComponent.fontSize = 17;
        textComponent.fontStyle = FontStyles.Bold;
        textComponent.color = new Color(0.9f, 0.95f, 1f);
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.enableWordWrapping = false;
        textComponent.overflowMode = TextOverflowModes.Overflow;

        // Outline для текста
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(1, -1);

        Debug.Log("[GraphicsSettingsSetup] ✅ Кнопка открытия настроек создана с улучшенным дизайном!");
        return button;
    }
}
