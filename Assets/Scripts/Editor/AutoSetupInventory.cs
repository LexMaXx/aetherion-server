using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// Автоматическая настройка UI инвентаря
/// Создаёт всю структуру одним кликом
/// </summary>
public class AutoSetupInventory : EditorWindow
{
    [MenuItem("Aetherion/Auto Setup Inventory UI (One Click!)")]
    public static void ShowWindow()
    {
        AutoSetupInventory window = GetWindow<AutoSetupInventory>("Inventory Setup");
        window.minSize = new Vector2(400, 200);
    }

    void OnGUI()
    {
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Auto Setup Inventory System", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Это создаст:\n" +
            "• Canvas с InventoryPanel\n" +
            "• 40 слотов инвентаря (8x5)\n" +
            "• 4 слота экипировки\n" +
            "• Tooltip\n" +
            "• InventoryManager\n" +
            "• Prefab слота",
            MessageType.Info
        );

        EditorGUILayout.Space(20);

        if (GUILayout.Button("🚀 Create Inventory System", GUILayout.Height(40)))
        {
            CreateInventorySystem();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Create Test Items", GUILayout.Height(30)))
        {
            CreateTestItems();
        }
    }

    static void CreateInventorySystem()
    {
        Debug.Log("[AutoSetupInventory] 🚀 Начинаю создание системы инвентаря...");

        // 0. Проверяем EventSystem
        CreateEventSystemIfNeeded();

        // 1. Создаём Canvas
        GameObject canvasObj = CreateCanvas();
        Canvas canvas = canvasObj.GetComponent<Canvas>();

        // 2. Создаём InventoryPanel
        GameObject inventoryPanel = CreateInventoryPanel(canvasObj.transform);

        // 3. Создаём Title
        CreateTitle(inventoryPanel.transform);

        // 4. Создаём Close Button
        GameObject closeButton = CreateCloseButton(inventoryPanel.transform);

        // 5. Создаём InventorySlotsContainer
        GameObject slotsContainer = CreateInventorySlotsContainer(inventoryPanel.transform);

        // 6. Создаём EquipmentPanel
        GameObject equipmentPanel = CreateEquipmentPanel(inventoryPanel.transform);

        // 7. Создаём 4 Equipment Slots
        GameObject weaponSlot = CreateEquipmentSlot(equipmentPanel.transform, "WeaponSlot", EquipmentSlot.Weapon, -100);
        GameObject armorSlot = CreateEquipmentSlot(equipmentPanel.transform, "ArmorSlot", EquipmentSlot.Armor, -220);
        GameObject helmetSlot = CreateEquipmentSlot(equipmentPanel.transform, "HelmetSlot", EquipmentSlot.Helmet, -340);
        GameObject accessorySlot = CreateEquipmentSlot(equipmentPanel.transform, "AccessorySlot", EquipmentSlot.Accessory, -460);

        // 8. Создаём Tooltip
        GameObject tooltip = CreateTooltip(canvasObj.transform);

        // 9. Создаём prefab слота инвентаря
        GameObject slotPrefab = CreateInventorySlotPrefab();

        // 10. Создаём InventoryManager
        GameObject managerObj = CreateInventoryManager(
            inventoryPanel,
            slotsContainer,
            equipmentPanel,
            slotPrefab,
            tooltip,
            weaponSlot,
            armorSlot,
            helmetSlot,
            accessorySlot,
            closeButton
        );

        // 11. Скрываем панель инвентаря
        inventoryPanel.SetActive(false);

        Debug.Log("[AutoSetupInventory] ✅ Система инвентаря создана!");
        Debug.Log("[AutoSetupInventory] 📍 Теперь создайте тестовые предметы: Aetherion → Auto Setup Inventory → Create Test Items");

        Selection.activeGameObject = managerObj;
    }

    static void CreateEventSystemIfNeeded()
    {
        // Проверяем есть ли EventSystem в сцене
        UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();

        if (eventSystem == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Debug.Log("[AutoSetupInventory] ✅ EventSystem создан");
        }
        else
        {
            Debug.Log("[AutoSetupInventory] ✅ EventSystem уже существует");
        }
    }

    static GameObject CreateCanvas()
    {
        GameObject canvasObj = new GameObject("InventoryCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        Debug.Log("[AutoSetupInventory] ✅ Canvas создан");
        return canvasObj;
    }

    static GameObject CreateInventoryPanel(Transform parent)
    {
        GameObject panel = new GameObject("InventoryPanel");
        panel.transform.SetParent(parent);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1200, 700);
        rt.anchoredPosition = Vector2.zero;

        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.08f, 0.08f, 0.12f, 0.98f); // Тёмно-синий

        // Добавляем Outline для красивой рамки
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.5f, 0.8f, 1f); // Голубая рамка
        outline.effectDistance = new Vector2(3, -3);

        // Добавляем Shadow для глубины
        Shadow shadow = panel.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.8f);
        shadow.effectDistance = new Vector2(5, -5);

        Debug.Log("[AutoSetupInventory] ✅ InventoryPanel создана с красивым дизайном");
        return panel;
    }

    static void CreateTitle(Transform parent)
    {
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(parent);

        RectTransform rt = titleObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(400, 60);
        rt.anchoredPosition = new Vector2(0, -40);

        TextMeshProUGUI text = titleObj.AddComponent<TextMeshProUGUI>();
        text.text = "⚔ INVENTORY ⚔";
        text.fontSize = 42;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;

        // Градиент от золотого к белому
        text.enableVertexGradient = true;
        text.colorGradient = new VertexGradient(
            new Color(1f, 0.84f, 0f, 1f),    // Золотой верх
            new Color(1f, 0.84f, 0f, 1f),    // Золотой верх
            new Color(1f, 0.95f, 0.7f, 1f),  // Светло-золотой низ
            new Color(1f, 0.95f, 0.7f, 1f)   // Светло-золотой низ
        );

        // Outline для текста
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color(0.2f, 0.1f, 0f, 1f);

        Debug.Log("[AutoSetupInventory] ✅ Title создан с градиентом");
    }

    static GameObject CreateCloseButton(Transform parent)
    {
        GameObject btnObj = new GameObject("CloseButton");
        btnObj.transform.SetParent(parent);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(50, 50);
        rt.anchoredPosition = new Vector2(-30, -30);

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.7f, 0.15f, 0.15f, 1f); // Тёмно-красный

        Button btn = btnObj.AddComponent<Button>();

        // Настраиваем цвета кнопки
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.7f, 0.15f, 0.15f, 1f);     // Красный
        colors.highlightedColor = new Color(0.9f, 0.25f, 0.25f, 1f); // Светло-красный
        colors.pressedColor = new Color(0.5f, 0.1f, 0.1f, 1f);       // Тёмно-красный
        colors.selectedColor = new Color(0.7f, 0.15f, 0.15f, 1f);
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        btn.colors = colors;

        // Добавляем Shadow для глубины
        Shadow shadow = btnObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.8f);
        shadow.effectDistance = new Vector2(2, -2);

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        textRt.anchoredPosition = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "✖";
        text.fontSize = 30;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        Debug.Log("[AutoSetupInventory] ✅ Close Button создан");
        return btnObj; // Возвращаем для подключения onClick
    }

    static GameObject CreateInventorySlotsContainer(Transform parent)
    {
        GameObject container = new GameObject("InventorySlotsContainer");
        container.transform.SetParent(parent);

        RectTransform rt = container.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(30, 30); // Left, Bottom
        rt.offsetMax = new Vector2(-650, -100); // Right, Top

        GridLayoutGroup grid = container.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(70, 70);
        grid.spacing = new Vector2(5, 5);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;

        Debug.Log("[AutoSetupInventory] ✅ InventorySlotsContainer создан");
        return container;
    }

    static GameObject CreateEquipmentPanel(Transform parent)
    {
        GameObject panel = new GameObject("EquipmentPanel");
        panel.transform.SetParent(parent);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(-600, 30); // Left from right edge, Bottom
        rt.offsetMax = new Vector2(-20, -100); // Right, Top

        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.15f, 1f); // Немного темнее основной панели

        // Добавляем Outline
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.3f, 0.6f, 1f); // Фиолетовая рамка
        outline.effectDistance = new Vector2(2, -2);

        // Title
        GameObject titleObj = new GameObject("EquipmentTitle");
        titleObj.transform.SetParent(panel.transform);

        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(300, 40);
        titleRt.anchoredPosition = new Vector2(0, -30);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "⚔ EQUIPMENT ⚔";
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;

        // Градиент фиолетовый
        titleText.enableVertexGradient = true;
        titleText.colorGradient = new VertexGradient(
            new Color(0.8f, 0.6f, 1f, 1f),    // Светло-фиолетовый верх
            new Color(0.8f, 0.6f, 1f, 1f),
            new Color(0.6f, 0.4f, 0.9f, 1f),  // Фиолетовый низ
            new Color(0.6f, 0.4f, 0.9f, 1f)
        );

        // Outline
        titleText.outlineWidth = 0.2f;
        titleText.outlineColor = new Color(0.2f, 0.1f, 0.3f, 1f);

        Debug.Log("[AutoSetupInventory] ✅ EquipmentPanel создана");
        return panel;
    }

    static GameObject CreateEquipmentSlot(Transform parent, string name, EquipmentSlot slotType, float posY)
    {
        GameObject slot = new GameObject(name);
        slot.transform.SetParent(parent);

        RectTransform rt = slot.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(100, 100);
        rt.anchoredPosition = new Vector2(0, posY);

        Image img = slot.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.2f, 1f); // Немного синеватый

        // Добавляем Outline для красоты
        Outline slotOutline = slot.AddComponent<Outline>();
        slotOutline.effectColor = new Color(0.4f, 0.5f, 0.7f, 0.8f); // Голубая рамка
        slotOutline.effectDistance = new Vector2(2, -2);

        CanvasGroup cg = slot.AddComponent<CanvasGroup>();
        EquipmentSlotUI slotUI = slot.AddComponent<EquipmentSlotUI>();

        // Icon
        GameObject iconObj = new GameObject("ItemIcon");
        iconObj.transform.SetParent(slot.transform);

        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = new Vector2(10, 10);
        iconRt.offsetMax = new Vector2(-10, -10);

        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.enabled = false;

        // Slot Name
        GameObject nameObj = new GameObject("SlotNameText");
        nameObj.transform.SetParent(slot.transform);

        RectTransform nameRt = nameObj.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.5f, 0f);
        nameRt.anchorMax = new Vector2(0.5f, 0f);
        nameRt.pivot = new Vector2(0.5f, 0f);
        nameRt.sizeDelta = new Vector2(100, 30);
        nameRt.anchoredPosition = new Vector2(0, -25);

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = slotType.ToString();
        nameText.fontSize = 14;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = new Color(0.7f, 0.7f, 0.7f, 1f);

        // Подключаем ссылки через reflection
        var iconField = typeof(EquipmentSlotUI).GetField("iconImage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (iconField != null) iconField.SetValue(slotUI, iconImg);

        var bgField = typeof(EquipmentSlotUI).GetField("slotBackgroundImage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (bgField != null) bgField.SetValue(slotUI, img);

        var nameField = typeof(EquipmentSlotUI).GetField("slotNameText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (nameField != null) nameField.SetValue(slotUI, nameText);

        var typeField = typeof(EquipmentSlotUI).GetField("slotType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (typeField != null) typeField.SetValue(slotUI, slotType);

        Debug.Log($"[AutoSetupInventory] ✅ {name} создан");
        return slot;
    }

    static GameObject CreateTooltip(Transform parent)
    {
        GameObject tooltip = new GameObject("ItemTooltip");
        tooltip.transform.SetParent(parent);

        RectTransform rt = tooltip.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1f);
        rt.anchorMax = new Vector2(0, 1f);
        rt.pivot = new Vector2(0, 1f);
        rt.sizeDelta = new Vector2(300, 200);
        rt.anchoredPosition = Vector2.zero;

        Image img = tooltip.AddComponent<Image>();
        img.color = new Color(0.05f, 0.05f, 0.05f, 0.98f);

        // Name
        GameObject nameObj = new GameObject("TooltipItemName");
        nameObj.transform.SetParent(tooltip.transform);

        RectTransform nameRt = nameObj.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 1f);
        nameRt.anchorMax = new Vector2(1, 1f);
        nameRt.pivot = new Vector2(0.5f, 1f);
        nameRt.offsetMin = new Vector2(0, -40);
        nameRt.offsetMax = new Vector2(0, -20);

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 22;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = new Color(1f, 0.84f, 0f, 1f); // Золотой

        // Description
        GameObject descObj = new GameObject("TooltipDescription");
        descObj.transform.SetParent(tooltip.transform);

        RectTransform descRt = descObj.AddComponent<RectTransform>();
        descRt.anchorMin = Vector2.zero;
        descRt.anchorMax = Vector2.one;
        descRt.offsetMin = new Vector2(10, 10);
        descRt.offsetMax = new Vector2(-10, -70);

        TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.fontSize = 16;
        descText.alignment = TextAlignmentOptions.TopLeft;
        descText.color = Color.white;
        descText.enableWordWrapping = true;

        tooltip.SetActive(false);

        Debug.Log("[AutoSetupInventory] ✅ Tooltip создан");
        return tooltip;
    }

    static GameObject CreateInventorySlotPrefab()
    {
        // Создаём временный GameObject
        GameObject slot = new GameObject("InventorySlot");

        RectTransform rt = slot.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(70, 70);

        CanvasGroup cg = slot.AddComponent<CanvasGroup>();
        InventorySlot slotScript = slot.AddComponent<InventorySlot>();

        // Background
        GameObject bg = new GameObject("SlotBackground");
        bg.transform.SetParent(slot.transform);

        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;

        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.25f, 1f); // Немного синеватый
        bgImg.raycastTarget = true; // ВАЖНО для drag & drop

        // Добавляем тонкий Outline
        Outline bgOutline = bg.AddComponent<Outline>();
        bgOutline.effectColor = new Color(0.3f, 0.4f, 0.5f, 0.5f); // Тонкая голубая рамка
        bgOutline.effectDistance = new Vector2(1, -1);

        // Icon
        GameObject icon = new GameObject("ItemIcon");
        icon.transform.SetParent(slot.transform);

        RectTransform iconRt = icon.AddComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = new Vector2(5, 5);
        iconRt.offsetMax = new Vector2(-5, -5);

        Image iconImg = icon.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.enabled = false;

        // Quantity Text
        GameObject qty = new GameObject("QuantityText");
        qty.transform.SetParent(slot.transform);

        RectTransform qtyRt = qty.AddComponent<RectTransform>();
        qtyRt.anchorMin = new Vector2(1f, 0f);
        qtyRt.anchorMax = new Vector2(1f, 0f);
        qtyRt.pivot = new Vector2(1f, 0f);
        qtyRt.sizeDelta = new Vector2(40, 30);
        qtyRt.anchoredPosition = new Vector2(-5, 5);

        TextMeshProUGUI qtyText = qty.AddComponent<TextMeshProUGUI>();
        qtyText.fontSize = 18;
        qtyText.alignment = TextAlignmentOptions.BottomRight;
        qtyText.color = Color.white;
        qtyText.enableAutoSizing = false;
        qtyText.enabled = false;

        // Highlight (опционально)
        GameObject highlight = new GameObject("HighlightFrame");
        highlight.transform.SetParent(slot.transform);

        RectTransform hlRt = highlight.AddComponent<RectTransform>();
        hlRt.anchorMin = Vector2.zero;
        hlRt.anchorMax = Vector2.one;
        hlRt.sizeDelta = Vector2.zero;

        Image hlImg = highlight.AddComponent<Image>();
        hlImg.color = new Color(1f, 0.9f, 0f, 0.5f);

        highlight.SetActive(false);

        // Подключаем ссылки через reflection
        var iconField = typeof(InventorySlot).GetField("iconImage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (iconField != null) iconField.SetValue(slotScript, iconImg);

        var qtyField = typeof(InventorySlot).GetField("quantityText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (qtyField != null) qtyField.SetValue(slotScript, qtyText);

        var hlField = typeof(InventorySlot).GetField("highlightFrame",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (hlField != null) hlField.SetValue(slotScript, highlight);

        // Сохраняем как prefab
        string prefabPath = "Assets/Prefabs/UI";
        if (!AssetDatabase.IsValidFolder(prefabPath))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }

        string fullPath = $"{prefabPath}/InventorySlot.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(slot, fullPath);

        DestroyImmediate(slot);

        Debug.Log($"[AutoSetupInventory] ✅ Prefab создан: {fullPath}");
        return prefab;
    }

    static GameObject CreateInventoryManager(
        GameObject inventoryPanel,
        GameObject slotsContainer,
        GameObject equipmentPanel,
        GameObject slotPrefab,
        GameObject tooltip,
        GameObject weaponSlot,
        GameObject armorSlot,
        GameObject helmetSlot,
        GameObject accessorySlot,
        GameObject closeButton)
    {
        GameObject managerObj = new GameObject("InventoryManager");
        InventoryManager manager = managerObj.AddComponent<InventoryManager>();

        // Подключаем кнопку закрытия
        Button closeBtn = closeButton.GetComponent<Button>();
        if (closeBtn != null)
        {
            closeBtn.onClick.AddListener(() => manager.CloseInventory());
            Debug.Log("[AutoSetupInventory] ✅ Close Button подключён к InventoryManager.CloseInventory()");
        }

        // Подключаем ссылки через reflection
        var panelField = typeof(InventoryManager).GetField("inventoryPanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (panelField != null) panelField.SetValue(manager, inventoryPanel);

        var containerField = typeof(InventoryManager).GetField("inventorySlotsContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (containerField != null) containerField.SetValue(manager, slotsContainer.transform);

        var eqPanelField = typeof(InventoryManager).GetField("equipmentSlotsContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (eqPanelField != null) eqPanelField.SetValue(manager, equipmentPanel.transform);

        var prefabField = typeof(InventoryManager).GetField("inventorySlotPrefab",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (prefabField != null) prefabField.SetValue(manager, slotPrefab);

        var tooltipField = typeof(InventoryManager).GetField("itemTooltip",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (tooltipField != null) tooltipField.SetValue(manager, tooltip);

        var nameField = typeof(InventoryManager).GetField("tooltipNameText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (nameField != null) nameField.SetValue(manager, tooltip.transform.Find("TooltipItemName").GetComponent<TextMeshProUGUI>());

        var descField = typeof(InventoryManager).GetField("tooltipDescriptionText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (descField != null) descField.SetValue(manager, tooltip.transform.Find("TooltipDescription").GetComponent<TextMeshProUGUI>());

        var weaponField = typeof(InventoryManager).GetField("weaponSlot",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (weaponField != null) weaponField.SetValue(manager, weaponSlot.GetComponent<EquipmentSlotUI>());

        var armorField = typeof(InventoryManager).GetField("armorSlot",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (armorField != null) armorField.SetValue(manager, armorSlot.GetComponent<EquipmentSlotUI>());

        var helmetField = typeof(InventoryManager).GetField("helmetSlot",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (helmetField != null) helmetField.SetValue(manager, helmetSlot.GetComponent<EquipmentSlotUI>());

        var accessoryField = typeof(InventoryManager).GetField("accessorySlot",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (accessoryField != null) accessoryField.SetValue(manager, accessorySlot.GetComponent<EquipmentSlotUI>());

        // Добавляем тестер
        InventoryTester tester = managerObj.AddComponent<InventoryTester>();

        Debug.Log("[AutoSetupInventory] ✅ InventoryManager создан и настроен");
        return managerObj;
    }

    static void CreateTestItems()
    {
        string folderPath = "Assets/Data/Items";

        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Data", "Items");
        }

        // 1. Iron Sword
        ItemData sword = ScriptableObject.CreateInstance<ItemData>();
        sword.itemName = "Iron Sword";
        sword.description = "A sturdy iron sword for beginners";
        sword.itemType = ItemType.Weapon;
        sword.isStackable = false;
        sword.isEquippable = true;
        sword.equipmentSlot = EquipmentSlot.Weapon;
        sword.attackBonus = 10;
        sword.sellPrice = 50;
        sword.buyPrice = 100;
        AssetDatabase.CreateAsset(sword, $"{folderPath}/IronSword.asset");

        // 2. Leather Armor
        ItemData armor = ScriptableObject.CreateInstance<ItemData>();
        armor.itemName = "Leather Armor";
        armor.description = "Light leather armor providing basic protection";
        armor.itemType = ItemType.Armor;
        armor.isStackable = false;
        armor.isEquippable = true;
        armor.equipmentSlot = EquipmentSlot.Armor;
        armor.defenseBonus = 5;
        armor.healthBonus = 20;
        armor.sellPrice = 100;
        armor.buyPrice = 200;
        AssetDatabase.CreateAsset(armor, $"{folderPath}/LeatherArmor.asset");

        // 3. Iron Helmet
        ItemData helmet = ScriptableObject.CreateInstance<ItemData>();
        helmet.itemName = "Iron Helmet";
        helmet.description = "Protects your head from blows";
        helmet.itemType = ItemType.Helmet;
        helmet.isStackable = false;
        helmet.isEquippable = true;
        helmet.equipmentSlot = EquipmentSlot.Helmet;
        helmet.defenseBonus = 3;
        helmet.sellPrice = 30;
        helmet.buyPrice = 60;
        AssetDatabase.CreateAsset(helmet, $"{folderPath}/IronHelmet.asset");

        // 4. Magic Ring
        ItemData ring = ScriptableObject.CreateInstance<ItemData>();
        ring.itemName = "Magic Ring";
        ring.description = "A ring imbued with magical power";
        ring.itemType = ItemType.Accessory;
        ring.isStackable = false;
        ring.isEquippable = true;
        ring.equipmentSlot = EquipmentSlot.Accessory;
        ring.manaBonus = 30;
        ring.sellPrice = 150;
        ring.buyPrice = 300;
        AssetDatabase.CreateAsset(ring, $"{folderPath}/MagicRing.asset");

        // 5. Health Potion
        ItemData potion = ScriptableObject.CreateInstance<ItemData>();
        potion.itemName = "Health Potion";
        potion.description = "Restores 50 HP when consumed";
        potion.itemType = ItemType.Consumable;
        potion.isStackable = true;
        potion.maxStackSize = 99;
        potion.isEquippable = false;
        potion.healAmount = 50;
        potion.sellPrice = 10;
        potion.buyPrice = 20;
        AssetDatabase.CreateAsset(potion, $"{folderPath}/HealthPotion.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[AutoSetupInventory] ✅ Созданы тестовые предметы:");
        Debug.Log($"  • Iron Sword ({folderPath}/IronSword.asset)");
        Debug.Log($"  • Leather Armor ({folderPath}/LeatherArmor.asset)");
        Debug.Log($"  • Iron Helmet ({folderPath}/IronHelmet.asset)");
        Debug.Log($"  • Magic Ring ({folderPath}/MagicRing.asset)");
        Debug.Log($"  • Health Potion ({folderPath}/HealthPotion.asset)");
        Debug.Log("\n💡 Подключите их в InventoryManager → Inventory Tester");

        EditorUtility.DisplayDialog("Success",
            "Тестовые предметы созданы!\n\n" +
            "Найдите их в: Assets/Data/Items/\n\n" +
            "Подключите в InventoryManager → Inventory Tester",
            "OK");
    }
}
