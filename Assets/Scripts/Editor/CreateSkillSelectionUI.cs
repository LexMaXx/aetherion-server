using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Автоматическое создание UI панели выбора скиллов
/// Использование: Tools → Aetherion → Create Skill Selection UI
/// </summary>
public class CreateSkillSelectionUI : EditorWindow
{
    [MenuItem("Tools/Aetherion/Create Skill Selection UI")]
    public static void CreateUI()
    {
        // Найти Canvas в сцене
        Canvas canvas = FindObjectOfType<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("[CreateSkillSelectionUI] Canvas не найден в сцене! Создайте Canvas сначала.");
            return;
        }

        Debug.Log("[CreateSkillSelectionUI] Начинаю создание UI панели...");

        // 1. Создать корневую панель
        GameObject rootPanel = CreateRootPanel(canvas.transform);

        // 2. Создать фон
        CreateBackground(rootPanel.transform);

        // 3. Создать заголовок
        CreateTitle(rootPanel.transform);

        // 4. Создать библиотеку скиллов (6 слотов)
        GameObject libraryPanel = CreateLibraryPanel(rootPanel.transform);
        SkillSlotUI[] librarySlots = CreateLibrarySlots(libraryPanel.transform);

        // 5. Создать панель выбранных скиллов (3 слота)
        GameObject equippedPanel = CreateEquippedPanel(rootPanel.transform);
        SkillSlotUI[] equippedSlots = CreateEquippedSlots(equippedPanel.transform);

        // 6. Создать панель информации
        GameObject infoPanel = CreateInfoPanel(rootPanel.transform);

        // 7. Подключить SkillSelectionManager
        ConnectSkillSelectionManager(librarySlots, equippedSlots, infoPanel);

        Debug.Log("[CreateSkillSelectionUI] ✅ UI панель создана успешно!");
        Debug.Log("[CreateSkillSelectionUI] Осталось назначить SkillDatabase в SkillSelectionManager!");

        // Выделить корневую панель
        Selection.activeGameObject = rootPanel;
    }

    private static GameObject CreateRootPanel(Transform canvas)
    {
        GameObject panel = new GameObject("SkillSelectionPanel");
        panel.transform.SetParent(canvas, false);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0.5f); // Right-Middle
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(1, 0.5f);
        rt.anchoredPosition = new Vector2(-50, 0);
        rt.sizeDelta = new Vector2(1100, 900);

        Debug.Log("[CreateUI] ✓ Корневая панель создана");
        return panel;
    }

    private static void CreateBackground(Transform parent)
    {
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(parent, false);

        RectTransform rt = bg.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image img = bg.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.78f); // Чёрный, 78% прозрачности

        Debug.Log("[CreateUI] ✓ Фон создан");
    }

    private static void CreateTitle(Transform parent)
    {
        GameObject title = new GameObject("TitleText");
        title.transform.SetParent(parent, false);

        RectTransform rt = title.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -40);
        rt.sizeDelta = new Vector2(800, 60);

        TextMeshProUGUI text = title.AddComponent<TextMeshProUGUI>();
        text.text = "ВЫБЕРИТЕ 3 НАВЫКА";
        text.fontSize = 42;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;

        Debug.Log("[CreateUI] ✓ Заголовок создан");
    }

    private static GameObject CreateLibraryPanel(Transform parent)
    {
        GameObject panel = new GameObject("SkillLibraryPanel");
        panel.transform.SetParent(parent, false);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -150);
        rt.sizeDelta = new Vector2(1000, 450);

        // GridLayoutGroup
        GridLayoutGroup grid = panel.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(150, 180);
        grid.spacing = new Vector2(30, 30);
        grid.padding = new RectOffset(20, 20, 20, 20);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperCenter;

        // Заголовок библиотеки
        GameObject titleObj = new GameObject("LibraryTitle");
        titleObj.transform.SetParent(panel.transform, false);
        titleObj.transform.SetAsFirstSibling(); // Поставить первым, чтобы не попал в Grid

        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1);
        titleRt.anchorMax = new Vector2(0.5f, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, 50);
        titleRt.sizeDelta = new Vector2(600, 40);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Доступные навыки";
        titleText.fontSize = 28;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.86f, 0.39f); // Золотистый

        Debug.Log("[CreateUI] ✓ Библиотека скиллов создана");
        return panel;
    }

    private static SkillSlotUI[] CreateLibrarySlots(Transform parent)
    {
        SkillSlotUI[] slots = new SkillSlotUI[6];

        for (int i = 0; i < 6; i++)
        {
            GameObject slot = CreateSlot($"LibrarySlot_{i + 1}", parent, true);
            slots[i] = slot.GetComponent<SkillSlotUI>();
        }

        Debug.Log("[CreateUI] ✓ 6 слотов библиотеки созданы");
        return slots;
    }

    private static GameObject CreateEquippedPanel(Transform parent)
    {
        GameObject panel = new GameObject("EquippedSkillsPanel");
        panel.transform.SetParent(parent, false);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 80);
        rt.sizeDelta = new Vector2(700, 220);

        // HorizontalLayoutGroup
        HorizontalLayoutGroup hLayout = panel.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 40;
        hLayout.padding = new RectOffset(20, 20, 20, 20);
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = false;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;

        // Заголовок
        GameObject titleObj = new GameObject("EquippedTitle");
        titleObj.transform.SetParent(panel.transform, false);
        titleObj.transform.SetAsFirstSibling();

        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1);
        titleRt.anchorMax = new Vector2(0.5f, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, 50);
        titleRt.sizeDelta = new Vector2(600, 40);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Выбранные навыки (1, 2, 3)";
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.39f, 1f, 0.39f); // Зелёный

        Debug.Log("[CreateUI] ✓ Панель выбранных скиллов создана");
        return panel;
    }

    private static SkillSlotUI[] CreateEquippedSlots(Transform parent)
    {
        SkillSlotUI[] slots = new SkillSlotUI[3];
        string[] keys = { "1", "2", "3" };

        for (int i = 0; i < 3; i++)
        {
            GameObject slot = CreateSlot($"EquippedSlot_{i + 1}", parent, false);
            SkillSlotUI slotUI = slot.GetComponent<SkillSlotUI>();

            // Установить размер (для HorizontalLayout)
            RectTransform rt = slot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180, 180);

            // Добавить текст клавиши
            GameObject keyText = new GameObject("KeyBindText");
            keyText.transform.SetParent(slot.transform, false);

            RectTransform keyRt = keyText.AddComponent<RectTransform>();
            keyRt.anchorMin = new Vector2(0, 1);
            keyRt.anchorMax = new Vector2(0, 1);
            keyRt.pivot = new Vector2(0, 1);
            keyRt.anchoredPosition = new Vector2(10, -10);
            keyRt.sizeDelta = new Vector2(40, 40);

            TextMeshProUGUI keyTMP = keyText.AddComponent<TextMeshProUGUI>();
            keyTMP.text = keys[i];
            keyTMP.fontSize = 32;
            keyTMP.fontStyle = FontStyles.Bold;
            keyTMP.alignment = TextAlignmentOptions.Center;
            keyTMP.color = Color.yellow;
            keyTMP.outlineWidth = 0.3f;
            keyTMP.outlineColor = Color.black;

            // Назначить в SkillSlotUI
            SerializedObject so = new SerializedObject(slotUI);
            so.FindProperty("keyBindText").objectReferenceValue = keyTMP;
            so.ApplyModifiedProperties();

            slots[i] = slotUI;
        }

        Debug.Log("[CreateUI] ✓ 3 слота выбранных скиллов созданы");
        return slots;
    }

    private static GameObject CreateSlot(string name, Transform parent, bool isLibrarySlot)
    {
        GameObject slot = new GameObject(name);
        slot.transform.SetParent(parent, false);

        RectTransform rt = slot.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(150, 180);

        // Фон слота
        Image img = slot.AddComponent<Image>();
        img.color = isLibrarySlot ? new Color(0.16f, 0.16f, 0.16f) : new Color(0.08f, 0.24f, 0.08f);

        // Компонент SkillSlotUI
        SkillSlotUI slotUI = slot.AddComponent<SkillSlotUI>();

        if (slotUI == null)
        {
            Debug.LogError("[CreateSkillSelectionUI] Не удалось добавить SkillSlotUI! Проверь что скрипт существует и скомпилирован.");
            return slot;
        }

        // Иконка
        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(slot.transform, false);
        RectTransform iconRt = icon.AddComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = new Vector2(10, 10);
        iconRt.offsetMax = new Vector2(-10, -10);
        Image iconImg = icon.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // Текст "Пусто"
        GameObject emptyText = new GameObject("EmptyText");
        emptyText.transform.SetParent(slot.transform, false);
        RectTransform emptyRt = emptyText.AddComponent<RectTransform>();
        emptyRt.anchorMin = Vector2.zero;
        emptyRt.anchorMax = Vector2.one;
        emptyRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI emptyTMP = emptyText.AddComponent<TextMeshProUGUI>();
        emptyTMP.text = "Пусто";
        emptyTMP.fontSize = 18;
        emptyTMP.alignment = TextAlignmentOptions.Center;
        emptyTMP.color = new Color(0.59f, 0.59f, 0.59f);

        // Название скилла
        GameObject nameText = new GameObject("SkillNameText");
        nameText.transform.SetParent(slot.transform, false);
        RectTransform nameRt = nameText.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.5f, 0);
        nameRt.anchorMax = new Vector2(0.5f, 0);
        nameRt.pivot = new Vector2(0.5f, 0);
        nameRt.anchoredPosition = new Vector2(0, 10);
        nameRt.sizeDelta = new Vector2(140, 40);
        TextMeshProUGUI nameTMP = nameText.AddComponent<TextMeshProUGUI>();
        nameTMP.fontSize = 14;
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.color = Color.white;
        nameTMP.enableWordWrapping = true;
        nameTMP.overflowMode = TextOverflowModes.Truncate;

        // Назначить ссылки в SkillSlotUI
        SerializedObject so = new SerializedObject(slotUI);
        so.FindProperty("isLibrarySlot").boolValue = isLibrarySlot;
        so.FindProperty("iconImage").objectReferenceValue = iconImg;
        so.FindProperty("skillNameText").objectReferenceValue = nameTMP;
        so.FindProperty("emptyText").objectReferenceValue = emptyTMP;
        so.ApplyModifiedProperties();

        return slot;
    }

    private static GameObject CreateInfoPanel(Transform parent)
    {
        GameObject panel = new GameObject("SkillInfoPanel");
        panel.transform.SetParent(parent, false);
        panel.SetActive(false); // Выключен по умолчанию

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-20, -300);
        rt.sizeDelta = new Vector2(350, 250);

        // Фон
        GameObject bg = new GameObject("InfoBackground");
        bg.transform.SetParent(panel.transform, false);
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.86f);

        // Название скилла
        GameObject nameObj = new GameObject("SkillNameText");
        nameObj.transform.SetParent(panel.transform, false);
        RectTransform nameRt = nameObj.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.5f, 1);
        nameRt.anchorMax = new Vector2(0.5f, 1);
        nameRt.pivot = new Vector2(0.5f, 1);
        nameRt.anchoredPosition = new Vector2(0, -30);
        nameRt.sizeDelta = new Vector2(320, 40);
        TextMeshProUGUI nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
        nameTMP.fontSize = 24;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.color = new Color(1f, 0.86f, 0.39f);

        // Описание
        GameObject descObj = new GameObject("SkillDescriptionText");
        descObj.transform.SetParent(panel.transform, false);
        RectTransform descRt = descObj.AddComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0.5f, 1);
        descRt.anchorMax = new Vector2(0.5f, 1);
        descRt.pivot = new Vector2(0.5f, 1);
        descRt.anchoredPosition = new Vector2(0, -100);
        descRt.sizeDelta = new Vector2(320, 100);
        TextMeshProUGUI descTMP = descObj.AddComponent<TextMeshProUGUI>();
        descTMP.fontSize = 16;
        descTMP.alignment = TextAlignmentOptions.TopLeft;
        descTMP.color = Color.white;
        descTMP.enableWordWrapping = true;

        // Статы
        GameObject statsObj = new GameObject("SkillStatsText");
        statsObj.transform.SetParent(panel.transform, false);
        RectTransform statsRt = statsObj.AddComponent<RectTransform>();
        statsRt.anchorMin = new Vector2(0.5f, 1);
        statsRt.anchorMax = new Vector2(0.5f, 1);
        statsRt.pivot = new Vector2(0.5f, 1);
        statsRt.anchoredPosition = new Vector2(0, -200);
        statsRt.sizeDelta = new Vector2(320, 60);
        TextMeshProUGUI statsTMP = statsObj.AddComponent<TextMeshProUGUI>();
        statsTMP.fontSize = 14;
        statsTMP.alignment = TextAlignmentOptions.TopLeft;
        statsTMP.color = new Color(0.78f, 0.78f, 0.78f);

        Debug.Log("[CreateUI] ✓ Панель информации создана");
        return panel;
    }

    private static void ConnectSkillSelectionManager(SkillSlotUI[] librarySlots, SkillSlotUI[] equippedSlots, GameObject infoPanel)
    {
        // Найти или создать SkillSelectionManager
        SkillSelectionManager manager = FindObjectOfType<SkillSelectionManager>();

        if (manager == null)
        {
            // Найти CharacterSelectionManager
            CharacterSelectionManager charSelManager = FindObjectOfType<CharacterSelectionManager>();

            if (charSelManager != null)
            {
                manager = charSelManager.gameObject.AddComponent<SkillSelectionManager>();
                Debug.Log("[CreateUI] ✓ SkillSelectionManager добавлен к CharacterSelectionManager");
            }
            else
            {
                // Создать новый GameObject
                GameObject managerObj = new GameObject("SkillSelectionManager");
                manager = managerObj.AddComponent<SkillSelectionManager>();
                Debug.Log("[CreateUI] ✓ SkillSelectionManager создан как новый объект");
            }
        }

        // Назначить ссылки
        SerializedObject so = new SerializedObject(manager);

        // Library slots
        SerializedProperty libSlotsProperty = so.FindProperty("librarySlots");
        libSlotsProperty.arraySize = 6;
        for (int i = 0; i < 6; i++)
        {
            libSlotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = librarySlots[i];
        }

        // Equipped slots
        SerializedProperty eqSlotsProperty = so.FindProperty("equippedSlots");
        eqSlotsProperty.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            eqSlotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = equippedSlots[i];
        }

        // Info panel
        so.FindProperty("skillInfoPanel").objectReferenceValue = infoPanel;

        // Info panel texts
        Transform infoPanelTransform = infoPanel.transform;
        so.FindProperty("skillNameText").objectReferenceValue = infoPanelTransform.Find("SkillNameText").GetComponent<TextMeshProUGUI>();
        so.FindProperty("skillDescriptionText").objectReferenceValue = infoPanelTransform.Find("SkillDescriptionText").GetComponent<TextMeshProUGUI>();
        so.FindProperty("skillStatsText").objectReferenceValue = infoPanelTransform.Find("SkillStatsText").GetComponent<TextMeshProUGUI>();

        so.ApplyModifiedProperties();

        // Назначить manager во все слоты
        foreach (SkillSlotUI slot in librarySlots)
        {
            SerializedObject slotSO = new SerializedObject(slot);
            slotSO.FindProperty("skillSelectionManager").objectReferenceValue = manager;
            slotSO.ApplyModifiedProperties();
        }

        foreach (SkillSlotUI slot in equippedSlots)
        {
            SerializedObject slotSO = new SerializedObject(slot);
            slotSO.FindProperty("skillSelectionManager").objectReferenceValue = manager;
            slotSO.ApplyModifiedProperties();
        }

        Debug.Log("[CreateUI] ✓ SkillSelectionManager подключен ко всем слотам");

        // Выделить manager для удобства
        EditorGUIUtility.PingObject(manager);
        Debug.Log("[CreateUI] ⚠️ ВАЖНО: Назначь SkillDatabase в SkillSelectionManager вручную!");
    }
}
