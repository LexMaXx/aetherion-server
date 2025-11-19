using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Автоматическая настройка Party System UI
/// Создает все необходимые панели, кнопки и префабы
/// </summary>
public class PartySystemSetup : EditorWindow
{
    [MenuItem("Aetherion/Setup Party System UI")]
    public static void ShowWindow()
    {
        GetWindow<PartySystemSetup>("Party System Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Party System Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("1. Create PartyManager GameObject", GUILayout.Height(40)))
        {
            CreatePartyManager();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("2. Create Party UI Panels", GUILayout.Height(40)))
        {
            CreatePartyUIPanels();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("3. Create Party Member Slot Prefab", GUILayout.Height(40)))
        {
            CreatePartyMemberSlotPrefab();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("4. Setup All (1+2+3)", GUILayout.Height(60)))
        {
            CreatePartyManager();
            CreatePartyUIPanels();
            CreatePartyMemberSlotPrefab();
            Debug.Log("✅ Party System полностью настроена!");
        }

        GUILayout.Space(20);
        EditorGUILayout.HelpBox("Эта утилита автоматически создаст:\n" +
            "• PartyManager GameObject (DontDestroyOnLoad)\n" +
            "• PartyUI Panel с дизайном\n" +
            "• PartyInviteUI Panel с кнопками\n" +
            "• PartyMemberSlot Prefab", MessageType.Info);
    }

    private static void CreatePartyManager()
    {
        // Проверяем существует ли уже
        PartyManager existing = FindObjectOfType<PartyManager>();
        if (existing != null)
        {
            Debug.LogWarning("⚠️ PartyManager уже существует!");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // Создаем GameObject
        GameObject partyManagerGO = new GameObject("PartyManager");
        partyManagerGO.AddComponent<PartyManager>();

        // DontDestroyOnLoad (в Editor режиме не работает, но добавляем для Runtime)
        DontDestroyOnLoad(partyManagerGO);

        Selection.activeGameObject = partyManagerGO;
        Debug.Log("✅ PartyManager создан!");
    }

    private static void CreatePartyUIPanels()
    {
        // Находим Canvas в BattleScene
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ Canvas не найден! Откройте BattleScene.");
            return;
        }

        // 1. Создаем PartyPanel
        GameObject partyPanel = CreatePartyPanel(canvas.transform);

        // 2. Создаем PartyInvitePanel
        GameObject invitePanel = CreatePartyInvitePanel(canvas.transform);

        Debug.Log("✅ Party UI Panels созданы!");
        Selection.activeGameObject = partyPanel;
    }

    private static GameObject CreatePartyPanel(Transform parent)
    {
        // Проверяем существует ли
        Transform existing = parent.Find("PartyPanel");
        if (existing != null)
        {
            Debug.LogWarning("⚠️ PartyPanel уже существует!");
            return existing.gameObject;
        }

        // Создаем основную панель
        GameObject panel = new GameObject("PartyPanel");
        panel.transform.SetParent(parent, false);

        // RectTransform
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(20, 0);
        rt.sizeDelta = new Vector2(300, 400);

        // Image (фон)
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // CanvasGroup для fade
        panel.AddComponent<CanvasGroup>();

        // PartyUI компонент
        PartyUI partyUI = panel.AddComponent<PartyUI>();

        // === Header ===
        GameObject header = new GameObject("Header");
        header.transform.SetParent(panel.transform, false);
        RectTransform headerRT = header.AddComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = new Vector2(0, 0);
        headerRT.sizeDelta = new Vector2(0, 50);

        Image headerImg = header.AddComponent<Image>();
        headerImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // Title Text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(header.transform, false);
        RectTransform titleRT = titleObj.AddComponent<RectTransform>();
        titleRT.anchorMin = Vector2.zero;
        titleRT.anchorMax = Vector2.one;
        titleRT.offsetMin = new Vector2(10, 0);
        titleRT.offsetMax = new Vector2(-50, 0);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Группа (0/5)";
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;

        // Close Button
        GameObject closeBtn = new GameObject("CloseButton");
        closeBtn.transform.SetParent(header.transform, false);
        RectTransform closeBtnRT = closeBtn.AddComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(1, 0.5f);
        closeBtnRT.anchorMax = new Vector2(1, 0.5f);
        closeBtnRT.pivot = new Vector2(1, 0.5f);
        closeBtnRT.anchoredPosition = new Vector2(-10, 0);
        closeBtnRT.sizeDelta = new Vector2(40, 40);

        Image closeBtnImg = closeBtn.AddComponent<Image>();
        closeBtnImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        Button closeBtnButton = closeBtn.AddComponent<Button>();

        GameObject closeBtnText = new GameObject("Text");
        closeBtnText.transform.SetParent(closeBtn.transform, false);
        RectTransform closeBtnTextRT = closeBtnText.AddComponent<RectTransform>();
        closeBtnTextRT.anchorMin = Vector2.zero;
        closeBtnTextRT.anchorMax = Vector2.one;
        closeBtnTextRT.offsetMin = Vector2.zero;
        closeBtnTextRT.offsetMax = Vector2.zero;

        TextMeshProUGUI closeBtnTextTMP = closeBtnText.AddComponent<TextMeshProUGUI>();
        closeBtnTextTMP.text = "X";
        closeBtnTextTMP.fontSize = 24;
        closeBtnTextTMP.fontStyle = FontStyles.Bold;
        closeBtnTextTMP.color = Color.white;
        closeBtnTextTMP.alignment = TextAlignmentOptions.Center;

        // === ScrollView для членов ===
        GameObject scrollView = new GameObject("MembersScrollView");
        scrollView.transform.SetParent(panel.transform, false);
        RectTransform scrollRT = scrollView.AddComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(10, 10);
        scrollRT.offsetMax = new Vector2(-10, -60);

        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollView.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 1f);

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        RectTransform viewportRT = viewport.AddComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        // Content (контейнер для слотов)
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5;
        vlg.padding = new RectOffset(5, 5, 5, 5);
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Подключаем ScrollRect
        scrollRect.content = contentRT;
        scrollRect.viewport = viewportRT;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        // Назначаем ссылки в PartyUI
        SetPrivateField(partyUI, "partyPanel", panel);
        SetPrivateField(partyUI, "partyTitleText", titleText);
        SetPrivateField(partyUI, "membersContainer", content.transform);
        SetPrivateField(partyUI, "closeButton", closeBtnButton);

        // По умолчанию скрыта
        panel.SetActive(false);

        return panel;
    }

    private static GameObject CreatePartyInvitePanel(Transform parent)
    {
        // Проверяем существует ли
        Transform existing = parent.Find("PartyInvitePanel");
        if (existing != null)
        {
            Debug.LogWarning("⚠️ PartyInvitePanel уже существует!");
            return existing.gameObject;
        }

        // Создаем панель
        GameObject panel = new GameObject("PartyInvitePanel");
        panel.transform.SetParent(parent, false);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(400, 200);

        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        PartyInviteUI inviteUI = panel.AddComponent<PartyInviteUI>();

        // === Header Text ===
        GameObject headerText = new GameObject("InviterNameText");
        headerText.transform.SetParent(panel.transform, false);
        RectTransform headerRT = headerText.AddComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = new Vector2(0, -20);
        headerRT.sizeDelta = new Vector2(-40, 30);

        TextMeshProUGUI headerTMP = headerText.AddComponent<TextMeshProUGUI>();
        headerTMP.text = "PlayerName приглашает вас в группу";
        headerTMP.fontSize = 18;
        headerTMP.fontStyle = FontStyles.Bold;
        headerTMP.color = Color.white;
        headerTMP.alignment = TextAlignmentOptions.Center;

        // === Details Text ===
        GameObject detailsText = new GameObject("InviterDetailsText");
        detailsText.transform.SetParent(panel.transform, false);
        RectTransform detailsRT = detailsText.AddComponent<RectTransform>();
        detailsRT.anchorMin = new Vector2(0, 1);
        detailsRT.anchorMax = new Vector2(1, 1);
        detailsRT.pivot = new Vector2(0.5f, 1);
        detailsRT.anchoredPosition = new Vector2(0, -60);
        detailsRT.sizeDelta = new Vector2(-40, 25);

        TextMeshProUGUI detailsTMP = detailsText.AddComponent<TextMeshProUGUI>();
        detailsTMP.text = "Воин, Ур. 5";
        detailsTMP.fontSize = 16;
        detailsTMP.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        detailsTMP.alignment = TextAlignmentOptions.Center;

        // === Accept Button ===
        GameObject acceptBtn = CreateButton(panel.transform, "AcceptButton", "Принять",
            new Vector2(0.25f, 0), new Vector2(-110, -80), new Vector2(150, 50),
            new Color(0.2f, 0.8f, 0.2f, 1f));

        // === Decline Button ===
        GameObject declineBtn = CreateButton(panel.transform, "DeclineButton", "Отклонить",
            new Vector2(0.75f, 0), new Vector2(110, -80), new Vector2(150, 50),
            new Color(0.8f, 0.2f, 0.2f, 1f));

        // Назначаем ссылки
        SetPrivateField(inviteUI, "invitePanel", panel);
        SetPrivateField(inviteUI, "inviterNameText", headerTMP);
        SetPrivateField(inviteUI, "inviterDetailsText", detailsTMP);
        SetPrivateField(inviteUI, "acceptButton", acceptBtn.GetComponent<Button>());
        SetPrivateField(inviteUI, "declineButton", declineBtn.GetComponent<Button>());

        // По умолчанию скрыта
        panel.SetActive(false);

        return panel;
    }

    private static void CreatePartyMemberSlotPrefab()
    {
        // Создаем временный GameObject для префаба
        GameObject slot = new GameObject("PartyMemberSlot");

        RectTransform rt = slot.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(280, 80);

        // Background
        Image bg = slot.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        PartyMemberSlot slotScript = slot.AddComponent<PartyMemberSlot>();

        // === Username Text ===
        GameObject usernameObj = new GameObject("UsernameText");
        usernameObj.transform.SetParent(slot.transform, false);
        RectTransform usernameRT = usernameObj.AddComponent<RectTransform>();
        usernameRT.anchorMin = new Vector2(0, 1);
        usernameRT.anchorMax = new Vector2(1, 1);
        usernameRT.pivot = new Vector2(0, 1);
        usernameRT.anchoredPosition = new Vector2(10, -5);
        usernameRT.sizeDelta = new Vector2(-20, 25);

        TextMeshProUGUI usernameTMP = usernameObj.AddComponent<TextMeshProUGUI>();
        usernameTMP.text = "PlayerName";
        usernameTMP.fontSize = 16;
        usernameTMP.fontStyle = FontStyles.Bold;
        usernameTMP.color = Color.white;
        usernameTMP.alignment = TextAlignmentOptions.Left;

        // === Class & Level Text ===
        GameObject classObj = new GameObject("ClassText");
        classObj.transform.SetParent(slot.transform, false);
        RectTransform classRT = classObj.AddComponent<RectTransform>();
        classRT.anchorMin = new Vector2(0, 1);
        classRT.anchorMax = new Vector2(0.5f, 1);
        classRT.pivot = new Vector2(0, 1);
        classRT.anchoredPosition = new Vector2(10, -30);
        classRT.sizeDelta = new Vector2(-15, 20);

        TextMeshProUGUI classTMP = classObj.AddComponent<TextMeshProUGUI>();
        classTMP.text = "Воин";
        classTMP.fontSize = 12;
        classTMP.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        classTMP.alignment = TextAlignmentOptions.Left;

        GameObject levelObj = new GameObject("LevelText");
        levelObj.transform.SetParent(slot.transform, false);
        RectTransform levelRT = levelObj.AddComponent<RectTransform>();
        levelRT.anchorMin = new Vector2(0.5f, 1);
        levelRT.anchorMax = new Vector2(1, 1);
        levelRT.pivot = new Vector2(1, 1);
        levelRT.anchoredPosition = new Vector2(-10, -30);
        levelRT.sizeDelta = new Vector2(-15, 20);

        TextMeshProUGUI levelTMP = levelObj.AddComponent<TextMeshProUGUI>();
        levelTMP.text = "Ур. 1";
        levelTMP.fontSize = 12;
        levelTMP.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        levelTMP.alignment = TextAlignmentOptions.Right;

        // === Health Bar ===
        CreateBar(slot.transform, "HealthBar", new Vector2(10, -55), new Color(0.8f, 0.2f, 0.2f, 1f), "100/100", out Image hpBar, out TextMeshProUGUI hpText);

        // === Mana Bar ===
        CreateBar(slot.transform, "ManaBar", new Vector2(10, -70), new Color(0.2f, 0.4f, 0.8f, 1f), "100/100", out Image mpBar, out TextMeshProUGUI mpText);

        // === Leave Button (будет скрыт для других игроков) ===
        GameObject leaveBtn = CreateButton(slot.transform, "LeaveButton", "Выйти",
            new Vector2(1, 0.5f), new Vector2(-5, 0), new Vector2(60, 30),
            new Color(0.6f, 0.2f, 0.2f, 1f));
        leaveBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 12;

        // Назначаем ссылки через Reflection (т.к. поля SerializeField private)
        SetPrivateField(slotScript, "usernameText", usernameTMP);
        SetPrivateField(slotScript, "classText", classTMP);
        SetPrivateField(slotScript, "levelText", levelTMP);
        SetPrivateField(slotScript, "healthBar", hpBar);
        SetPrivateField(slotScript, "manaBar", mpBar);
        SetPrivateField(slotScript, "healthText", hpText);
        SetPrivateField(slotScript, "manaText", mpText);
        SetPrivateField(slotScript, "leaveButton", leaveBtn.GetComponent<Button>());

        // Сохраняем как префаб
        string prefabPath = "Assets/Prefabs/UI/PartyMemberSlot.prefab";

        // Создаем папку если не существует
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        PrefabUtility.SaveAsPrefabAsset(slot, prefabPath);
        DestroyImmediate(slot);

        Debug.Log($"✅ PartyMemberSlot префаб создан: {prefabPath}");

        // Назначаем префаб в PartyUI
        PartyUI partyUI = FindObjectOfType<PartyUI>();
        if (partyUI != null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            SetPrivateField(partyUI, "memberSlotPrefab", prefab);
            Debug.Log("✅ Префаб назначен в PartyUI!");
        }
    }

    // === Helper Methods ===

    private static GameObject CreateButton(Transform parent, string name, string text,
        Vector2 anchor, Vector2 position, Vector2 size, Color color)
    {
        GameObject btn = new GameObject(name);
        btn.transform.SetParent(parent, false);

        RectTransform rt = btn.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Image img = btn.AddComponent<Image>();
        img.color = color;

        Button button = btn.AddComponent<Button>();

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btn.transform, false);
        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 16;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    private static void CreateBar(Transform parent, string name, Vector2 position, Color color, string text,
        out Image fillImage, out TextMeshProUGUI textTMP)
    {
        // Background
        GameObject bg = new GameObject(name + "Background");
        bg.transform.SetParent(parent, false);
        RectTransform bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0);
        bgRT.anchorMax = new Vector2(0, 0);
        bgRT.pivot = new Vector2(0, 0);
        bgRT.anchoredPosition = position;
        bgRT.sizeDelta = new Vector2(180, 12);

        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        // Fill
        GameObject fill = new GameObject(name);
        fill.transform.SetParent(bg.transform, false);
        RectTransform fillRT = fill.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(1, 1);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        fillImage = fill.AddComponent<Image>();
        fillImage.color = color;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 1f;

        // Text
        GameObject textObj = new GameObject(name + "Text");
        textObj.transform.SetParent(bg.transform, false);
        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        textTMP = textObj.AddComponent<TextMeshProUGUI>();
        textTMP.text = text;
        textTMP.fontSize = 10;
        textTMP.color = Color.white;
        textTMP.alignment = TextAlignmentOptions.Center;
        textTMP.fontStyle = FontStyles.Bold;
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            Debug.LogWarning($"⚠️ Field '{fieldName}' не найдено в {obj.GetType().Name}");
        }
    }
}
