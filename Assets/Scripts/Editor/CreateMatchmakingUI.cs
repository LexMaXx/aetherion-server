using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// Editor скрипт для автоматического создания UI матчмейкинга
/// Tools → Aetherion → Create Matchmaking UI
/// </summary>
public class CreateMatchmakingUI : EditorWindow
{
    [MenuItem("Tools/Aetherion/Create Matchmaking UI")]
    static void ShowWindow()
    {
        var window = GetWindow<CreateMatchmakingUI>("Create Matchmaking UI");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Создание UI матчмейкинга", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Этот скрипт создаст красивый UI лобби в текущей открытой сцене.\n\n" +
            "Убедитесь что:\n" +
            "1. Открыта сцена BattleScene\n" +
            "2. В сцене есть Canvas\n\n" +
            "UI будет создан автоматически и настроен!",
            MessageType.Info
        );

        GUILayout.Space(20);

        if (GUILayout.Button("✨ Создать UI лобби", GUILayout.Height(40)))
        {
            CreateUI();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("🗑️ Удалить UI лобби (если есть)", GUILayout.Height(30)))
        {
            DeleteUI();
        }
    }

    void CreateUI()
    {
        // Находим Canvas в сцене
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Canvas не найден в сцене! Создайте Canvas сначала.", "OK");
            return;
        }

        // Проверяем не существует ли уже
        Transform existing = canvas.transform.Find("MatchmakingLobby");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("UI уже существует",
                "UI лобби уже существует в сцене. Удалить и создать заново?",
                "Да, пересоздать", "Отмена"))
            {
                return;
            }
            DestroyImmediate(existing.gameObject);
        }

        Debug.Log("[CreateMatchmakingUI] Создание UI...");

        // 1. Создаем корневой объект
        GameObject lobby = new GameObject("MatchmakingLobby");
        lobby.transform.SetParent(canvas.transform, false);

        RectTransform lobbyRect = lobby.AddComponent<RectTransform>();
        lobbyRect.anchorMin = Vector2.zero;
        lobbyRect.anchorMax = Vector2.one;
        lobbyRect.sizeDelta = Vector2.zero;

        // 2. Создаем LobbyPanel (темный фон)
        GameObject lobbyPanel = new GameObject("LobbyPanel");
        lobbyPanel.transform.SetParent(lobby.transform, false);

        RectTransform panelRect = lobbyPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        Image panelImage = lobbyPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.85f); // Почти черный, 85% прозрачности

        // 3. Создаем контейнер по центру
        GameObject centerContainer = new GameObject("CenterContainer");
        centerContainer.transform.SetParent(lobbyPanel.transform, false);

        RectTransform containerRect = centerContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = new Vector2(600, 400);

        // Добавляем фон для контейнера (полупрозрачная панель)
        Image containerBg = centerContainer.AddComponent<Image>();
        containerBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // Добавляем Outline для красоты
        Outline outline = centerContainer.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.5f, 0.8f, 1f); // Синий outline
        outline.effectDistance = new Vector2(3, -3);

        // 4. Создаем Title
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(centerContainer.transform, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -30);
        titleRect.sizeDelta = new Vector2(500, 60);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "⚔️ ПОИСК СРАЖЕНИЯ ⚔️";
        titleText.fontSize = 42;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.85f, 0.3f); // Золотой цвет

        // Добавляем тень для красоты
        var titleShadow = titleObj.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0, 0, 0, 0.8f);
        titleShadow.effectDistance = new Vector2(3, -3);

        // 5. Создаем StatusText
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(centerContainer.transform, false);

        RectTransform statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.5f);
        statusRect.anchorMax = new Vector2(0.5f, 0.5f);
        statusRect.pivot = new Vector2(0.5f, 0.5f);
        statusRect.anchoredPosition = new Vector2(0, 80);
        statusRect.sizeDelta = new Vector2(500, 50);

        TextMeshProUGUI statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "Ожидание игроков...";
        statusText.fontSize = 32;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = new Color(1f, 1f, 0.5f); // Светло-желтый

        // 6. Создаем PlayerCountText
        GameObject playerCountObj = new GameObject("PlayerCountText");
        playerCountObj.transform.SetParent(centerContainer.transform, false);

        RectTransform playerCountRect = playerCountObj.AddComponent<RectTransform>();
        playerCountRect.anchorMin = new Vector2(0.5f, 0.5f);
        playerCountRect.anchorMax = new Vector2(0.5f, 0.5f);
        playerCountRect.pivot = new Vector2(0.5f, 0.5f);
        playerCountRect.anchoredPosition = new Vector2(0, 20);
        playerCountRect.sizeDelta = new Vector2(400, 50);

        TextMeshProUGUI playerCountText = playerCountObj.AddComponent<TextMeshProUGUI>();
        playerCountText.text = "👥 Игроков: 1/20";
        playerCountText.fontSize = 36;
        playerCountText.fontStyle = FontStyles.Bold;
        playerCountText.alignment = TextAlignmentOptions.Center;
        playerCountText.color = Color.white;

        // 7. Создаем TimerPanel (скрыт по умолчанию)
        GameObject timerPanel = new GameObject("TimerPanel");
        timerPanel.transform.SetParent(centerContainer.transform, false);
        timerPanel.SetActive(false); // Скрыт пока нет второго игрока

        RectTransform timerPanelRect = timerPanel.AddComponent<RectTransform>();
        timerPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        timerPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        timerPanelRect.pivot = new Vector2(0.5f, 0.5f);
        timerPanelRect.anchoredPosition = new Vector2(0, -50);
        timerPanelRect.sizeDelta = new Vector2(500, 120);

        // Фон для таймера
        Image timerBg = timerPanel.AddComponent<Image>();
        timerBg.color = new Color(0.2f, 0.3f, 0.2f, 0.8f); // Зеленоватый

        // TimerText
        GameObject timerTextObj = new GameObject("TimerText");
        timerTextObj.transform.SetParent(timerPanel.transform, false);

        RectTransform timerTextRect = timerTextObj.AddComponent<RectTransform>();
        timerTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        timerTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        timerTextRect.pivot = new Vector2(0.5f, 0.5f);
        timerTextRect.anchoredPosition = new Vector2(0, 20);
        timerTextRect.sizeDelta = new Vector2(450, 50);

        TextMeshProUGUI timerText = timerTextObj.AddComponent<TextMeshProUGUI>();
        timerText.text = "⏱️ Матч начнётся через: 20";
        timerText.fontSize = 32;
        timerText.fontStyle = FontStyles.Bold;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = new Color(0.3f, 1f, 0.3f); // Ярко-зеленый

        // Timer Fill Image (прогресс-бар)
        GameObject timerFillObj = new GameObject("TimerFillImage");
        timerFillObj.transform.SetParent(timerPanel.transform, false);

        RectTransform timerFillRect = timerFillObj.AddComponent<RectTransform>();
        timerFillRect.anchorMin = new Vector2(0.5f, 0f);
        timerFillRect.anchorMax = new Vector2(0.5f, 0f);
        timerFillRect.pivot = new Vector2(0.5f, 0f);
        timerFillRect.anchoredPosition = new Vector2(0, 10);
        timerFillRect.sizeDelta = new Vector2(450, 20);

        Image timerFillImage = timerFillObj.AddComponent<Image>();
        timerFillImage.type = Image.Type.Filled;
        timerFillImage.fillMethod = Image.FillMethod.Horizontal;
        timerFillImage.fillAmount = 1f;
        timerFillImage.color = new Color(0.2f, 0.8f, 0.2f); // Зеленый

        // 8. Создаем CancelButton
        GameObject cancelButton = new GameObject("CancelButton");
        cancelButton.transform.SetParent(centerContainer.transform, false);

        RectTransform cancelRect = cancelButton.AddComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.5f, 0f);
        cancelRect.anchorMax = new Vector2(0.5f, 0f);
        cancelRect.pivot = new Vector2(0.5f, 0f);
        cancelRect.anchoredPosition = new Vector2(0, 30);
        cancelRect.sizeDelta = new Vector2(250, 60);

        Image cancelBg = cancelButton.AddComponent<Image>();
        cancelBg.color = new Color(0.8f, 0.2f, 0.2f); // Красный

        Button buttonComponent = cancelButton.AddComponent<Button>();
        buttonComponent.targetGraphic = cancelBg;

        // Transition colors
        ColorBlock colors = buttonComponent.colors;
        colors.normalColor = new Color(0.8f, 0.2f, 0.2f);
        colors.highlightedColor = new Color(1f, 0.3f, 0.3f);
        colors.pressedColor = new Color(0.6f, 0.1f, 0.1f);
        colors.selectedColor = new Color(0.8f, 0.2f, 0.2f);
        buttonComponent.colors = colors;

        // Button Text
        GameObject buttonTextObj = new GameObject("Text");
        buttonTextObj.transform.SetParent(cancelButton.transform, false);

        RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "❌ ОТМЕНА";
        buttonText.fontSize = 28;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        // 9. Добавляем MatchmakingUI компонент
        MatchmakingUI uiComponent = lobby.AddComponent<MatchmakingUI>();

        // Назначаем все ссылки через Reflection (потому что поля SerializeField)
        var uiType = typeof(MatchmakingUI);

        SetPrivateField(uiComponent, "lobbyPanel", lobbyPanel);
        SetPrivateField(uiComponent, "statusText", statusText);
        SetPrivateField(uiComponent, "playerCountText", playerCountText);
        SetPrivateField(uiComponent, "timerText", timerText);
        SetPrivateField(uiComponent, "timerPanel", timerPanel);
        SetPrivateField(uiComponent, "cancelButton", buttonComponent);
        SetPrivateField(uiComponent, "timerFillImage", timerFillImage);

        // Скрываем лобби по умолчанию
        lobbyPanel.SetActive(false);

        // Сохраняем изменения
        EditorUtility.SetDirty(lobby);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        Debug.Log("[CreateMatchmakingUI] ✅ UI создан успешно!");
        EditorUtility.DisplayDialog("Успех!",
            "UI матчмейкинга создан успешно!\n\n" +
            "Объект: MatchmakingLobby\n" +
            "Компонент: MatchmakingUI (все ссылки назначены)\n\n" +
            "Проверьте Canvas в Hierarchy.",
            "OK");

        // Выбираем созданный объект
        Selection.activeGameObject = lobby;
    }

    void DeleteUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Canvas не найден в сцене!", "OK");
            return;
        }

        Transform existing = canvas.transform.Find("MatchmakingLobby");
        if (existing == null)
        {
            EditorUtility.DisplayDialog("Информация", "UI лобби не найден в сцене.", "OK");
            return;
        }

        DestroyImmediate(existing.gameObject);
        EditorUtility.DisplayDialog("Удалено", "UI лобби удален.", "OK");
    }

    void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(obj, value);
            Debug.Log($"[CreateMatchmakingUI] Назначено: {fieldName} = {value}");
        }
        else
        {
            Debug.LogWarning($"[CreateMatchmakingUI] Поле {fieldName} не найдено!");
        }
    }
}
