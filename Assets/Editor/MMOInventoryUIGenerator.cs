using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Автоматический генератор UI для MMO инвентаря
/// Создаёт весь UI одной кнопкой!
/// </summary>
public class MMOInventoryUIGenerator : EditorWindow
{
    private static Color darkBackground = new Color(0.08f, 0.08f, 0.08f, 0.95f);
    private static Color slotBackground = new Color(0.2f, 0.2f, 0.2f, 0.7f);
    private static Color headerColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    private static Color goldColor = new Color(1f, 0.84f, 0f, 1f);

    [MenuItem("Tools/MMO Inventory/Create UI (Auto)")]
    public static void CreateInventoryUI()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Создание MMO Inventory UI",
            "Это создаст полный UI для инвентаря в текущей сцене.\n\n" +
            "Будет создано:\n" +
            "- Canvas\n" +
            "- Inventory Panel\n" +
            "- 40 слотов (8x5)\n" +
            "- Заголовок\n" +
            "- Счётчик золота\n" +
            "- Кнопка закрытия\n\n" +
            "Продолжить?",
            "Да, создать",
            "Отмена"
        );

        if (!confirm)
            return;

        // Проверяем есть ли уже Canvas
        Canvas existingCanvas = GameObject.FindObjectOfType<Canvas>();
        if (existingCanvas != null)
        {
            bool useExisting = EditorUtility.DisplayDialog(
                "Canvas уже существует",
                "В сцене уже есть Canvas. Создать новый или использовать существующий?",
                "Использовать существующий",
                "Создать новый"
            );

            if (useExisting)
            {
                GameObject panel = CreateUIElements(existingCanvas.gameObject);
                CreateSlotPrefabInternal();
                CreateAndSetupManager(panel);
                return;
            }
        }

        // Создаём новый Canvas
        GameObject canvasObj = CreateCanvas();
        GameObject panelObj = CreateUIElements(canvasObj);

        // Автоматически создаём Prefab слота
        CreateSlotPrefabInternal();

        // Создаём MongoInventoryManager и настраиваем
        CreateAndSetupManager(panelObj);

        Debug.Log("✅ MMO Inventory UI создан успешно!");
        EditorUtility.DisplayDialog("Успех!",
            "UI инвентаря создан!\n\n" +
            "✅ Canvas создан\n" +
            "✅ Inventory Panel создан\n" +
            "✅ Slot Prefab создан\n" +
            "✅ MongoInventoryManager настроен\n\n" +
            "Теперь:\n" +
            "1. Добавьте ItemData в Item Database\n" +
            "2. Сохраните сцену (Ctrl+S)\n" +
            "3. Запустите игру и нажмите I",
            "OK");
    }

    [MenuItem("Tools/MMO Inventory/Create Slot Prefab")]
    public static void CreateSlotPrefab()
    {
        // Создаём временный GameObject для слота
        GameObject slotObj = CreateInventorySlot();

        // Сохраняем как prefab
        string path = "Assets/Resources/Prefabs/UI/MMOInventorySlot.prefab";

        // Создаём папки если их нет
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Resources/Prefabs", "UI");

        // Сохраняем prefab
        PrefabUtility.SaveAsPrefabAsset(slotObj, path);

        // Удаляем временный объект
        DestroyImmediate(slotObj);

        Debug.Log($"✅ Prefab слота создан: {path}");
        EditorUtility.DisplayDialog("Успех!", "Prefab слота создан!\n\nПуть: " + path, "OK");

        // Выделяем созданный prefab
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static GameObject CreateCanvas()
    {
        GameObject canvasObj = new GameObject("MMOInventoryCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Поверх всего

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        return canvasObj;
    }

    private static GameObject CreateUIElements(GameObject canvasObj)
    {
        // 1. Создаём панель инвентаря
        GameObject panelObj = CreateInventoryPanel(canvasObj.transform);

        // 2. Создаём заголовок
        CreateTitle(panelObj.transform);

        // 3. Создаём контейнер слотов
        CreateSlotsContainer(panelObj.transform);

        // 4. Создаём счётчик золота
        CreateGoldCounter(panelObj.transform);

        // 5. Создаём кнопку закрытия
        CreateCloseButton(panelObj.transform);

        // 6. Скрываем панель по умолчанию
        panelObj.SetActive(false);

        Selection.activeGameObject = panelObj;

        return panelObj;
    }

    private static GameObject CreateInventoryPanel(Transform parent)
    {
        GameObject panelObj = new GameObject("InventoryPanel");
        panelObj.transform.SetParent(parent, false);

        RectTransform rect = panelObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(700, 600);

        Image image = panelObj.AddComponent<Image>();
        image.color = darkBackground;
        image.raycastTarget = true;

        // Добавляем тень
        Shadow shadow = panelObj.AddComponent<Shadow>();
        shadow.effectDistance = new Vector2(5, -5);
        shadow.effectColor = new Color(0, 0, 0, 0.5f);

        // Добавляем Outline для красоты
        Outline outline = panelObj.AddComponent<Outline>();
        outline.effectDistance = new Vector2(2, -2);
        outline.effectColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        return panelObj;
    }

    private static void CreateTitle(Transform parent)
    {
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(parent, false);

        RectTransform rect = titleObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(680, 60);
        rect.anchoredPosition = new Vector2(0, -10);

        // Background для заголовка
        Image bgImage = titleObj.AddComponent<Image>();
        bgImage.color = headerColor;

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(titleObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "ИНВЕНТАРЬ";
        text.fontSize = 36;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = goldColor;

        // Outline для текста
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color(0, 0, 0, 0.5f);
    }

    private static void CreateSlotsContainer(Transform parent)
    {
        GameObject containerObj = new GameObject("SlotsContainer");
        containerObj.transform.SetParent(parent, false);

        RectTransform rect = containerObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = new Vector2(10, 80); // left, bottom
        rect.offsetMax = new Vector2(-10, -80); // right, top

        GridLayoutGroup grid = containerObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(70, 70);
        grid.spacing = new Vector2(5, 5);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.padding = new RectOffset(5, 5, 5, 5);
    }

    private static void CreateGoldCounter(Transform parent)
    {
        // Container для золота
        GameObject goldContainer = new GameObject("GoldContainer");
        goldContainer.transform.SetParent(parent, false);

        RectTransform containerRect = goldContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0f);
        containerRect.anchorMax = new Vector2(0.5f, 0f);
        containerRect.pivot = new Vector2(0.5f, 0f);
        containerRect.sizeDelta = new Vector2(300, 50);
        containerRect.anchoredPosition = new Vector2(0, 15);

        Image containerBg = goldContainer.AddComponent<Image>();
        containerBg.color = headerColor;

        // Иконка монеты (простой желтый круг)
        GameObject coinIcon = new GameObject("CoinIcon");
        coinIcon.transform.SetParent(goldContainer.transform, false);

        RectTransform coinRect = coinIcon.AddComponent<RectTransform>();
        coinRect.anchorMin = new Vector2(0f, 0.5f);
        coinRect.anchorMax = new Vector2(0f, 0.5f);
        coinRect.pivot = new Vector2(0.5f, 0.5f);
        coinRect.sizeDelta = new Vector2(30, 30);
        coinRect.anchoredPosition = new Vector2(30, 0);

        Image coinImage = coinIcon.AddComponent<Image>();
        coinImage.color = goldColor;

        // Делаем круглым
        coinImage.type = Image.Type.Filled;
        coinImage.fillMethod = Image.FillMethod.Radial360;

        // Text для количества золота
        GameObject goldText = new GameObject("GoldText");
        goldText.transform.SetParent(goldContainer.transform, false);

        RectTransform goldRect = goldText.AddComponent<RectTransform>();
        goldRect.anchorMin = new Vector2(0f, 0f);
        goldRect.anchorMax = new Vector2(1f, 1f);
        goldRect.offsetMin = new Vector2(70, 0);
        goldRect.offsetMax = new Vector2(-10, 0);

        TextMeshProUGUI text = goldText.AddComponent<TextMeshProUGUI>();
        text.text = "0";
        text.fontSize = 28;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = goldColor;
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color(0, 0, 0, 0.5f);
    }

    private static void CreateCloseButton(Transform parent)
    {
        GameObject buttonObj = new GameObject("CloseButton");
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(50, 50);
        rect.anchoredPosition = new Vector2(-10, -10);

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.8f, 0.2f, 0.2f, 1f);

        Button button = buttonObj.AddComponent<Button>();

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        colors.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);
        button.colors = colors;

        // Text "X"
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "✕";
        text.fontSize = 32;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
    }

    private static GameObject CreateInventorySlot()
    {
        GameObject slotObj = new GameObject("MMOInventorySlot");

        RectTransform rect = slotObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(70, 70);

        // Background
        Image bgImage = slotObj.AddComponent<Image>();
        bgImage.color = slotBackground;

        // Canvas Group для drag-drop
        CanvasGroup canvasGroup = slotObj.AddComponent<CanvasGroup>();

        // Добавляем компонент MMOInventorySlot
        slotObj.AddComponent<AetherionMMO.Inventory.MMOInventorySlot>();

        // Icon Image
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slotObj.transform, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(4, 4);
        iconRect.offsetMax = new Vector2(-4, -4);

        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.color = Color.white;
        iconImage.raycastTarget = false;
        iconImage.enabled = false; // По умолчанию скрыто

        // Quantity Text
        GameObject quantityObj = new GameObject("QuantityText");
        quantityObj.transform.SetParent(slotObj.transform, false);

        RectTransform quantityRect = quantityObj.AddComponent<RectTransform>();
        quantityRect.anchorMin = new Vector2(1f, 0f);
        quantityRect.anchorMax = new Vector2(1f, 0f);
        quantityRect.pivot = new Vector2(1f, 0f);
        quantityRect.sizeDelta = new Vector2(30, 20);
        quantityRect.anchoredPosition = new Vector2(-5, 5);

        TextMeshProUGUI quantityText = quantityObj.AddComponent<TextMeshProUGUI>();
        quantityText.text = "";
        quantityText.fontSize = 16;
        quantityText.fontStyle = FontStyles.Bold;
        quantityText.alignment = TextAlignmentOptions.BottomRight;
        quantityText.color = Color.white;
        quantityText.outlineWidth = 0.3f;
        quantityText.outlineColor = Color.black;
        quantityText.raycastTarget = false;

        // Highlight Border
        GameObject highlightObj = new GameObject("HighlightBorder");
        highlightObj.transform.SetParent(slotObj.transform, false);

        RectTransform highlightRect = highlightObj.AddComponent<RectTransform>();
        highlightRect.anchorMin = Vector2.zero;
        highlightRect.anchorMax = Vector2.one;
        highlightRect.offsetMin = new Vector2(-2, -2);
        highlightRect.offsetMax = new Vector2(2, 2);

        Image highlightImage = highlightObj.AddComponent<Image>();
        highlightImage.color = Color.yellow;
        highlightImage.raycastTarget = false;

        highlightObj.SetActive(false); // По умолчанию скрыто

        // Настраиваем ссылки в MMOInventorySlot через SerializedObject
        var slotComponent = slotObj.GetComponent<AetherionMMO.Inventory.MMOInventorySlot>();
        SerializedObject serializedSlot = new SerializedObject(slotComponent);

        serializedSlot.FindProperty("iconImage").objectReferenceValue = iconImage;
        serializedSlot.FindProperty("quantityText").objectReferenceValue = quantityText;
        serializedSlot.FindProperty("slotBackground").objectReferenceValue = bgImage;
        serializedSlot.FindProperty("highlightBorder").objectReferenceValue = highlightObj;

        serializedSlot.ApplyModifiedProperties();

        return slotObj;
    }

    /// <summary>
    /// Создать prefab слота без диалоговых окон (для автоматического вызова)
    /// </summary>
    private static GameObject CreateSlotPrefabInternal()
    {
        // Создаём временный GameObject для слота
        GameObject slotObj = CreateInventorySlot();

        // Сохраняем как prefab
        string path = "Assets/Resources/Prefabs/UI/MMOInventorySlot.prefab";

        // Создаём папки если их нет
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Resources/Prefabs", "UI");

        // Сохраняем prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(slotObj, path);

        // Удаляем временный объект
        DestroyImmediate(slotObj);

        Debug.Log($"✅ Prefab слота создан автоматически: {path}");

        return prefab;
    }

    /// <summary>
    /// Создать и настроить MongoInventoryManager автоматически
    /// </summary>
    private static void CreateAndSetupManager(GameObject panelObj)
    {
        // 1. Создаём GameObject для менеджера
        GameObject managerObj = new GameObject("MongoInventoryManager");

        // 2. Добавляем компонент MongoInventoryManager
        var manager = managerObj.AddComponent<AetherionMMO.Inventory.MongoInventoryManager>();

        // 3. Получаем SerializedObject для настройки через Inspector
        SerializedObject serializedManager = new SerializedObject(manager);

        // 4. Настраиваем Inventory Settings
        serializedManager.FindProperty("maxSlots").intValue = 40;
        serializedManager.FindProperty("rowSize").intValue = 8;

        // 5. Настраиваем UI References
        serializedManager.FindProperty("inventoryPanel").objectReferenceValue = panelObj;

        // 6. Находим SlotsContainer
        Transform slotsContainer = panelObj.transform.Find("SlotsContainer");
        if (slotsContainer != null)
        {
            serializedManager.FindProperty("slotsContainer").objectReferenceValue = slotsContainer;
        }

        // 7. Загружаем slot prefab
        GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/MMOInventorySlot.prefab");
        if (slotPrefab != null)
        {
            serializedManager.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
        }

        // 8. Находим GoldText
        Transform goldContainer = panelObj.transform.Find("GoldContainer");
        if (goldContainer != null)
        {
            Transform goldText = goldContainer.Find("GoldText");
            if (goldText != null)
            {
                serializedManager.FindProperty("goldText").objectReferenceValue = goldText.GetComponent<TextMeshProUGUI>();
            }
        }

        // 9. Находим CloseButton
        Transform closeButton = panelObj.transform.Find("CloseButton");
        if (closeButton != null)
        {
            serializedManager.FindProperty("closeButton").objectReferenceValue = closeButton.GetComponent<Button>();

            // Настраиваем OnClick для кнопки закрытия
            Button buttonComponent = closeButton.GetComponent<Button>();
            if (buttonComponent != null)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(
                    buttonComponent.onClick,
                    manager.CloseInventory
                );
            }
        }

        // 10. Загружаем все ItemData из Resources/Data/Items
        ItemData[] allItems = Resources.LoadAll<ItemData>("Data/Items");
        if (allItems.Length > 0)
        {
            SerializedProperty itemDatabaseProp = serializedManager.FindProperty("itemDatabase");
            itemDatabaseProp.ClearArray();

            for (int i = 0; i < allItems.Length; i++)
            {
                itemDatabaseProp.InsertArrayElementAtIndex(i);
                itemDatabaseProp.GetArrayElementAtIndex(i).objectReferenceValue = allItems[i];
            }

            Debug.Log($"✅ Загружено {allItems.Length} предметов в Item Database");
        }
        else
        {
            Debug.LogWarning("⚠️ Предметы не найдены в Resources/Data/Items. Добавьте ItemData вручную.");
        }

        // 11. Применяем все изменения
        serializedManager.ApplyModifiedProperties();

        Debug.Log("✅ MongoInventoryManager создан и настроен автоматически!");

        // 12. Выделяем менеджер в иерархии
        Selection.activeGameObject = managerObj;
    }
}
