using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

/// <summary>
/// Автоматическая настройка WorldMapScene
/// Создаёт все необходимые UI элементы
/// </summary>
public class WorldMapSceneSetup : EditorWindow
{
    [MenuItem("Aetherion/Setup World Map Scene UI")]
    public static void SetupWorldMapUI()
    {
        // Проверяем что мы в WorldMapScene
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("WorldMap"))
        {
            if (!EditorUtility.DisplayDialog("Внимание",
                $"Текущая сцена: {activeScene.name}\n\nВы уверены что хотите создать UI карты мира в этой сцене?",
                "Да, продолжить", "Отмена"))
            {
                return;
            }
        }

        Debug.Log("[WorldMapSceneSetup] Начало создания UI...");

        // Находим или создаём Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            canvas = CreateCanvas();
        }

        // Создаём UI элементы
        CreateInteractionPrompt(canvas.transform);
        CreateLocationInfoPanel(canvas.transform);
        CreateMobileInteractionButton(canvas.transform);

        Debug.Log("[WorldMapSceneSetup] ✅ UI карты мира создан!");
        EditorUtility.DisplayDialog("Готово", "UI карты мира успешно создан!\n\nТеперь назначьте ссылки в WorldMapPlayerController.", "OK");
    }

    private static void CreateMobileInteractionButton(Transform canvasTransform)
    {
        // Кнопка взаимодействия для мобильных устройств
        GameObject buttonObj = new GameObject("MobileInteractionButton");
        buttonObj.transform.SetParent(canvasTransform, false);

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-30, 30);
        buttonRect.sizeDelta = new Vector2(120, 120);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);

        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
        colors.highlightedColor = new Color(0.3f, 1f, 0.3f, 0.9f);
        colors.pressedColor = new Color(0.1f, 0.6f, 0.1f, 1f);
        button.colors = colors;

        // Текст кнопки
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "E\nВойти";
        buttonText.fontSize = 24;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        // Outline
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(2, -2);

        buttonObj.SetActive(false); // Скрыта по умолчанию

        Debug.Log("[WorldMapSceneSetup] MobileInteractionButton создана");
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        Debug.Log("[WorldMapSceneSetup] Canvas создан");
        return canvas;
    }

    private static void CreateInteractionPrompt(Transform canvasTransform)
    {
        GameObject promptObj = new GameObject("InteractionPrompt");
        promptObj.transform.SetParent(canvasTransform, false);

        RectTransform rect = promptObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0, 100);
        rect.sizeDelta = new Vector2(600, 50);

        TextMeshProUGUI text = promptObj.AddComponent<TextMeshProUGUI>();
        text.text = "Нажмите [E] чтобы войти в локацию";
        text.fontSize = 24;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.green;

        // Outline для читаемости
        Outline outline = promptObj.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(2, -2);

        promptObj.SetActive(false); // Выключен по умолчанию

        Debug.Log("[WorldMapSceneSetup] InteractionPrompt создан");
    }

    private static void CreateLocationInfoPanel(Transform canvasTransform)
    {
        // Панель
        GameObject panelObj = new GameObject("LocationInfoPanel");
        panelObj.transform.SetParent(canvasTransform, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-20, -20);
        panelRect.sizeDelta = new Vector2(350, 250);

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.8f);

        // Shadow для красоты
        Shadow shadow = panelObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(5, -5);

        panelObj.SetActive(false); // Выключен по умолчанию

        // LocationIcon (иконка локации)
        GameObject iconObj = new GameObject("LocationIcon");
        iconObj.transform.SetParent(panelObj.transform, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0, -10);
        iconRect.sizeDelta = new Vector2(64, 64);

        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.color = Color.white;

        // LocationNameText (название)
        GameObject nameObj = new GameObject("LocationNameText");
        nameObj.transform.SetParent(panelObj.transform, false);

        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0, -84);
        nameRect.sizeDelta = new Vector2(-20, 40);

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "Название Локации";
        nameText.fontSize = 28;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;

        // LocationDescriptionText (описание)
        GameObject descObj = new GameObject("LocationDescriptionText");
        descObj.transform.SetParent(panelObj.transform, false);

        RectTransform descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0f, 0f);
        descRect.anchorMax = new Vector2(1f, 1f);
        descRect.pivot = new Vector2(0.5f, 1f);
        descRect.anchoredPosition = new Vector2(0, -134);
        descRect.sizeDelta = new Vector2(-20, -184);

        TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = "Описание локации...";
        descText.fontSize = 16;
        descText.alignment = TextAlignmentOptions.TopLeft;
        descText.color = new Color(0.8f, 0.8f, 0.8f);
        descText.enableWordWrapping = true;

        // LocationLevelText (уровень сложности)
        GameObject levelObj = new GameObject("LocationLevelText");
        levelObj.transform.SetParent(panelObj.transform, false);

        RectTransform levelRect = levelObj.AddComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0f, 0f);
        levelRect.anchorMax = new Vector2(1f, 0f);
        levelRect.pivot = new Vector2(0.5f, 0f);
        levelRect.anchoredPosition = new Vector2(0, 10);
        levelRect.sizeDelta = new Vector2(-20, 30);

        TextMeshProUGUI levelText = levelObj.AddComponent<TextMeshProUGUI>();
        levelText.text = "Сложность: 1 | Рекомендуемый уровень: 1";
        levelText.fontSize = 14;
        levelText.alignment = TextAlignmentOptions.Center;
        levelText.color = Color.yellow;

        Debug.Log("[WorldMapSceneSetup] LocationInfoPanel создан");
    }

    [MenuItem("Aetherion/Assign World Map References")]
    public static void AssignWorldMapReferences()
    {
        // Находим Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Player не найден!\n\nСоздайте персонажа с тегом 'Player' и компонентом WorldMapPlayerController.", "OK");
            return;
        }

        WorldMapPlayerController controller = player.GetComponent<WorldMapPlayerController>();
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "WorldMapPlayerController не найден на Player!\n\nДобавьте компонент WorldMapPlayerController.", "OK");
            return;
        }

        // Находим UI элементы
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Canvas не найден!\n\nСначала создайте UI через меню:\nAetherion → Setup World Map Scene UI", "OK");
            return;
        }

        Transform canvasTransform = canvas.transform;

        // Назначаем ссылки через Reflection
        var controllerType = typeof(WorldMapPlayerController);

        SetField(controllerType, controller, "interactionPromptText",
            canvasTransform.Find("InteractionPrompt")?.GetComponent<TextMeshProUGUI>());

        SetField(controllerType, controller, "locationInfoPanel",
            canvasTransform.Find("LocationInfoPanel")?.gameObject);

        SetField(controllerType, controller, "mobileInteractionButton",
            canvasTransform.Find("MobileInteractionButton")?.GetComponent<Button>());

        Transform infoPanelTransform = canvasTransform.Find("LocationInfoPanel");
        if (infoPanelTransform != null)
        {
            SetField(controllerType, controller, "locationNameText",
                infoPanelTransform.Find("LocationNameText")?.GetComponent<TextMeshProUGUI>());

            SetField(controllerType, controller, "locationDescriptionText",
                infoPanelTransform.Find("LocationDescriptionText")?.GetComponent<TextMeshProUGUI>());

            SetField(controllerType, controller, "locationLevelText",
                infoPanelTransform.Find("LocationLevelText")?.GetComponent<TextMeshProUGUI>());

            SetField(controllerType, controller, "locationIconImage",
                infoPanelTransform.Find("LocationIcon")?.GetComponent<Image>());
        }

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[WorldMapSceneSetup] ✅ Ссылки назначены в WorldMapPlayerController!");
        EditorUtility.DisplayDialog("Готово", "Все ссылки успешно назначены в WorldMapPlayerController!", "OK");
    }

    private static void SetField(System.Type type, object instance, string fieldName, object value)
    {
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(instance, value);
            Debug.Log($"[WorldMapSceneSetup] Назначено: {fieldName} = {value?.GetType().Name ?? "null"}");
        }
        else
        {
            Debug.LogWarning($"[WorldMapSceneSetup] Поле '{fieldName}' не найдено в {type.Name}");
        }
    }
}
