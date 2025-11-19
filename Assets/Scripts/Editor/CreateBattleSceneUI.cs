using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEditor;

/// <summary>
/// Создает полноценный UI для BattleScene
/// Menu: Aetherion/UI/Create BattleScene UI
/// </summary>
public class CreateBattleSceneUI : Editor
{
    [MenuItem("Aetherion/UI/Create BattleScene UI")]
    public static void CreateUI()
    {
        // ═══════════════════════════════════════════════════════════
        // КРИТИЧЕСКОЕ: Создаем EventSystem для UI событий (клики, касания)
        // ═══════════════════════════════════════════════════════════
        if (GameObject.FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
            Debug.Log("[CreateBattleSceneUI] ✅ EventSystem создан (нужен для джойстика и кнопок)");
        }
        else
        {
            Debug.Log("[CreateBattleSceneUI] ✅ EventSystem уже существует");
        }

        // Создаем Canvas
        GameObject canvasGO = new GameObject("BattleSceneCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasGO.AddComponent<GraphicRaycaster>();

        // Добавляем BattleSceneUIManager
        BattleSceneUIManager uiManager = canvasGO.AddComponent<BattleSceneUIManager>();
        uiManager.mainCanvas = canvas;

        // ═══════════════════════════════════════════════════════════
        // 1. PLAYER STATS PANEL (Top Left)
        // ═══════════════════════════════════════════════════════════
        GameObject statsPanel = CreateStatsPanel(canvasGO.transform);
        uiManager.statsPanel = statsPanel.GetComponent<RectTransform>();

        // Получаем компоненты из stats panel
        uiManager.hpFillImage = statsPanel.transform.Find("HPBar/Fill").GetComponent<Image>();
        uiManager.mpFillImage = statsPanel.transform.Find("MPBar/Fill").GetComponent<Image>();
        uiManager.apFillImage = statsPanel.transform.Find("APBar/Fill").GetComponent<Image>();
        uiManager.hpText = statsPanel.transform.Find("HPBar/Text").GetComponent<TextMeshProUGUI>();
        uiManager.mpText = statsPanel.transform.Find("MPBar/Text").GetComponent<TextMeshProUGUI>();
        uiManager.apText = statsPanel.transform.Find("APBar/Text").GetComponent<TextMeshProUGUI>();
        uiManager.playerNameText = statsPanel.transform.Find("PlayerName").GetComponent<TextMeshProUGUI>();
        uiManager.levelText = statsPanel.transform.Find("Level").GetComponent<TextMeshProUGUI>();

        // ═══════════════════════════════════════════════════════════
        // 2. TARGET PANEL (Top Center)
        // ═══════════════════════════════════════════════════════════
        GameObject targetPanel = CreateTargetPanel(canvasGO.transform);
        uiManager.targetPanel = targetPanel.GetComponent<RectTransform>();
        uiManager.targetHpFillImage = targetPanel.transform.Find("HPBar/Fill").GetComponent<Image>();
        uiManager.targetNameText = targetPanel.transform.Find("TargetName").GetComponent<TextMeshProUGUI>();
        uiManager.targetHpText = targetPanel.transform.Find("HPBar/Text").GetComponent<TextMeshProUGUI>();

        // ═══════════════════════════════════════════════════════════
        // 3. SKILL BAR PANEL (Bottom Center)
        // ═══════════════════════════════════════════════════════════
        GameObject skillBarPanel = CreateSkillBarPanel(canvasGO.transform);
        uiManager.skillBarPanel = skillBarPanel.GetComponent<RectTransform>();

        // Получаем skill slots
        for (int i = 0; i < 5; i++)
        {
            Transform slotTransform = skillBarPanel.transform.Find($"SkillSlot{i + 1}");
            if (slotTransform != null)
            {
                uiManager.skillSlots[i] = slotTransform.GetComponent<BattleSkillSlot>();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 4. ACTION POINTS PANEL (Below Skill Bar)
        // ═══════════════════════════════════════════════════════════
        GameObject actionPointsPanel = CreateActionPointsPanel(canvasGO.transform);
        uiManager.actionPointsUI = actionPointsPanel.GetComponent<ActionPointsUI>();

        // ═══════════════════════════════════════════════════════════
        // 5. MOBILE CONTROLS PANEL (Bottom)
        // ═══════════════════════════════════════════════════════════
        GameObject mobilePanel = CreateMobileControlsPanel(canvasGO.transform);
        uiManager.mobileControlsPanel = mobilePanel.GetComponent<RectTransform>();
        uiManager.joystickPanel = mobilePanel.transform.Find("Joystick").GetComponent<RectTransform>();
        uiManager.joystickBackground = mobilePanel.transform.Find("Joystick/Background").GetComponent<Image>();
        uiManager.joystickHandle = mobilePanel.transform.Find("Joystick/Handle").GetComponent<Image>();
        uiManager.attackButton = mobilePanel.transform.Find("AttackButton").GetComponent<Button>();
        uiManager.attackButtonImage = mobilePanel.transform.Find("AttackButton").GetComponent<Image>();

        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("✅ BattleScene UI создан!");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("Компоненты:");
        Debug.Log("  📊 Player Stats Panel (Top Left)");
        Debug.Log("  🎯 Target Panel (Top Center)");
        Debug.Log("  🎮 Skill Bar (Bottom Center) - 5 слотов");
        Debug.Log("  ⚡ Action Points Panel (Below Skill Bar) - 10 шариков");
        Debug.Log("  📱 Mobile Controls (Joystick + Attack)");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("⚠️ ВАЖНО:");
        Debug.Log("  1. Все элементы можно редактировать в Inspector");
        Debug.Log("  2. Размеры и позиции можно менять вручную");
        Debug.Log("  3. Цвета и иконки настраиваются через компоненты");
        Debug.Log("═══════════════════════════════════════════════════════");

        Selection.activeGameObject = canvasGO;
    }

    // ═══════════════════════════════════════════════════════════
    // PLAYER STATS PANEL
    // ═══════════════════════════════════════════════════════════
    private static GameObject CreateStatsPanel(Transform parent)
    {
        GameObject panel = new GameObject("PlayerStatsPanel");
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.SetParent(parent, false);

        // Позиция: Top Left
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(20, -20);
        rt.sizeDelta = new Vector2(350, 180);

        // Background
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.6f);
        bg.raycastTarget = false;

        // Player Name
        GameObject nameText = CreateText("PlayerName", panel.transform, "Player Name", 24);
        RectTransform nameRT = nameText.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 1);
        nameRT.anchorMax = new Vector2(1, 1);
        nameRT.pivot = new Vector2(0.5f, 1);
        nameRT.anchoredPosition = new Vector2(0, -10);
        nameRT.sizeDelta = new Vector2(-20, 30);
        nameText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

        // Level Text
        GameObject levelText = CreateText("Level", panel.transform, "Lv 1", 20);
        RectTransform levelRT = levelText.GetComponent<RectTransform>();
        levelRT.anchorMin = new Vector2(1, 1);
        levelRT.anchorMax = new Vector2(1, 1);
        levelRT.pivot = new Vector2(1, 1);
        levelRT.anchoredPosition = new Vector2(-10, -10);
        levelRT.sizeDelta = new Vector2(80, 30);
        levelText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;

        // HP Bar
        CreateResourceBar("HPBar", panel.transform, new Color(0.8f, 0.1f, 0.1f), "999/999", new Vector2(0, -50));

        // MP Bar
        CreateResourceBar("MPBar", panel.transform, new Color(0.1f, 0.4f, 0.9f), "999/999", new Vector2(0, -90));

        // AP Bar (Action Points)
        CreateResourceBar("APBar", panel.transform, new Color(0.9f, 0.7f, 0.1f), "100/100", new Vector2(0, -130));

        return panel;
    }

    // ═══════════════════════════════════════════════════════════
    // TARGET PANEL
    // ═══════════════════════════════════════════════════════════
    private static GameObject CreateTargetPanel(Transform parent)
    {
        GameObject panel = new GameObject("TargetPanel");
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.SetParent(parent, false);

        // Позиция: Top Center
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -20);
        rt.sizeDelta = new Vector2(400, 80);

        // Background
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.2f, 0, 0, 0.7f);
        bg.raycastTarget = false;

        // Target Name
        GameObject nameText = CreateText("TargetName", panel.transform, "Enemy Name", 22);
        RectTransform nameRT = nameText.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 1);
        nameRT.anchorMax = new Vector2(1, 1);
        nameRT.pivot = new Vector2(0.5f, 1);
        nameRT.anchoredPosition = new Vector2(0, -10);
        nameRT.sizeDelta = new Vector2(-20, 25);
        nameText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // HP Bar
        CreateResourceBar("HPBar", panel.transform, new Color(0.8f, 0.1f, 0.1f), "999/999", new Vector2(0, -45));

        // Скрываем по умолчанию
        panel.SetActive(false);

        return panel;
    }

    // ═══════════════════════════════════════════════════════════
    // SKILL BAR PANEL
    // ═══════════════════════════════════════════════════════════
    private static GameObject CreateSkillBarPanel(Transform parent)
    {
        GameObject panel = new GameObject("SkillBarPanel");
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.SetParent(parent, false);

        // Позиция: Bottom Center
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 150);
        rt.sizeDelta = new Vector2(550, 100);

        // Background
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.5f);
        bg.raycastTarget = false;

        // Создаем 5 skill slots
        float slotSize = 90f;
        float spacing = 10f;
        float totalWidth = (slotSize * 5) + (spacing * 4);
        float startX = -(totalWidth / 2f) + (slotSize / 2f);

        for (int i = 0; i < 5; i++)
        {
            float posX = startX + (i * (slotSize + spacing));
            CreateSkillSlot($"SkillSlot{i + 1}", panel.transform, new Vector2(posX, 5), slotSize);
        }

        return panel;
    }

    // ═══════════════════════════════════════════════════════════
    // SKILL SLOT
    // ═══════════════════════════════════════════════════════════
    private static GameObject CreateSkillSlot(string name, Transform parent, Vector2 position, float size)
    {
        GameObject slot = new GameObject(name);
        RectTransform rt = slot.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(size, size);

        // Button
        Button button = slot.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1, 1, 1, 1);
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1);
        button.colors = colors;

        // Frame
        Image frame = slot.AddComponent<Image>();
        frame.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        // Icon
        GameObject iconGO = new GameObject("Icon");
        RectTransform iconRT = iconGO.AddComponent<RectTransform>();
        iconRT.SetParent(slot.transform, false);
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = new Vector2(5, 5);
        iconRT.offsetMax = new Vector2(-5, -5);

        Image iconImage = iconGO.AddComponent<Image>();
        iconImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        iconImage.raycastTarget = false;

        // Cooldown Overlay
        GameObject cooldownGO = new GameObject("CooldownOverlay");
        RectTransform cooldownRT = cooldownGO.AddComponent<RectTransform>();
        cooldownRT.SetParent(slot.transform, false);
        cooldownRT.anchorMin = Vector2.zero;
        cooldownRT.anchorMax = Vector2.one;
        cooldownRT.offsetMin = Vector2.zero;
        cooldownRT.offsetMax = Vector2.zero;

        Image cooldownImage = cooldownGO.AddComponent<Image>();
        cooldownImage.color = new Color(0, 0, 0, 0.7f);
        cooldownImage.type = Image.Type.Filled;
        cooldownImage.fillMethod = Image.FillMethod.Radial360;
        cooldownImage.fillOrigin = (int)Image.Origin360.Top;
        cooldownImage.fillAmount = 0f;
        cooldownImage.raycastTarget = false;

        // Cooldown Text
        GameObject cooldownTextGO = CreateText("CooldownText", slot.transform, "", 32);
        RectTransform cooldownTextRT = cooldownTextGO.GetComponent<RectTransform>();
        cooldownTextRT.anchorMin = Vector2.zero;
        cooldownTextRT.anchorMax = Vector2.one;
        cooldownTextRT.offsetMin = Vector2.zero;
        cooldownTextRT.offsetMax = Vector2.zero;
        TextMeshProUGUI cooldownTMP = cooldownTextGO.GetComponent<TextMeshProUGUI>();
        cooldownTMP.alignment = TextAlignmentOptions.Center;
        cooldownTMP.fontStyle = FontStyles.Bold;

        // BattleSkillSlot component
        BattleSkillSlot skillSlot = slot.AddComponent<BattleSkillSlot>();
        skillSlot.skillIconImage = iconImage;
        skillSlot.cooldownOverlay = cooldownImage;
        skillSlot.cooldownText = cooldownTMP;
        skillSlot.frameImage = frame;
        skillSlot.button = button;

        return slot;
    }

    // ═══════════════════════════════════════════════════════════
    // ACTION POINTS PANEL (Шарики как в ArenaScene)
    // ═══════════════════════════════════════════════════════════
    private static GameObject CreateActionPointsPanel(Transform parent)
    {
        GameObject panel = new GameObject("ActionPointsPanel");
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.SetParent(parent, false);

        // Позиция: чуть ниже Skill Bar (Bottom Center)
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 30); // 30px от низа (ниже skill bar на 150px)
        rt.sizeDelta = new Vector2(400, 50);

        // Добавляем компонент ActionPointsUI
        ActionPointsUI apUI = panel.AddComponent<ActionPointsUI>();

        // Настраиваем визуальные параметры
        var serializedObject = new UnityEditor.SerializedObject(apUI);
        serializedObject.FindProperty("pointSpacing").floatValue = 35f;
        serializedObject.FindProperty("pointSize").floatValue = 30f;
        serializedObject.FindProperty("activeColor").colorValue = new Color(1f, 0.8f, 0.2f, 1f); // Золотой
        serializedObject.FindProperty("inactiveColor").colorValue = new Color(0.3f, 0.3f, 0.3f, 0.3f); // Серый
        serializedObject.ApplyModifiedProperties();

        Debug.Log("[CreateBattleSceneUI] ✅ Action Points Panel создан (10 шариков)");

        return panel;
    }

    // ═══════════════════════════════════════════════════════════
    // MOBILE CONTROLS PANEL
    // ═══════════════════════════════════════════════════════════
    private static GameObject CreateMobileControlsPanel(Transform parent)
    {
        GameObject panel = new GameObject("MobileControlsPanel");
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // ═══════════════════════════════════════════════════════════
        // JOYSTICK (Bottom Left)
        // ═══════════════════════════════════════════════════════════
        GameObject joystickPanel = new GameObject("Joystick");
        RectTransform joystickRT = joystickPanel.AddComponent<RectTransform>();
        joystickRT.SetParent(panel.transform, false);
        joystickRT.anchorMin = new Vector2(0, 0);
        joystickRT.anchorMax = new Vector2(0, 0);
        joystickRT.pivot = new Vector2(0, 0);
        joystickRT.anchoredPosition = new Vector2(50, 50);
        joystickRT.sizeDelta = new Vector2(200, 200);

        // Joystick Background
        GameObject bgGO = new GameObject("Background");
        RectTransform bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.SetParent(joystickPanel.transform, false);
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(1, 1, 1, 0.3f);
        bgImage.raycastTarget = true;

        // Joystick Handle
        GameObject handleGO = new GameObject("Handle");
        RectTransform handleRT = handleGO.AddComponent<RectTransform>();
        handleRT.SetParent(joystickPanel.transform, false);
        handleRT.anchorMin = new Vector2(0.5f, 0.5f);
        handleRT.anchorMax = new Vector2(0.5f, 0.5f);
        handleRT.pivot = new Vector2(0.5f, 0.5f);
        handleRT.anchoredPosition = Vector2.zero;
        handleRT.sizeDelta = new Vector2(80, 80);

        Image handleImage = handleGO.AddComponent<Image>();
        handleImage.color = new Color(1, 1, 1, 0.8f);
        handleImage.raycastTarget = false;

        // ═══════════════════════════════════════════════════════════
        // ДОБАВЛЯЕМ VIRTUALJOYSTICK КОМПОНЕНТ
        // ═══════════════════════════════════════════════════════════
        VirtualJoystick virtualJoystick = joystickPanel.AddComponent<VirtualJoystick>();

        // Настраиваем VirtualJoystick через SerializedObject
        var joystickSerialized = new UnityEditor.SerializedObject(virtualJoystick);
        joystickSerialized.FindProperty("isDynamic").boolValue = false; // Фиксированный джойстик
        joystickSerialized.FindProperty("handleRange").floatValue = 50f; // Радиус движения ручки
        joystickSerialized.FindProperty("deadZone").floatValue = 0.1f; // Мёртвая зона
        joystickSerialized.FindProperty("background").objectReferenceValue = bgRT;
        joystickSerialized.FindProperty("handle").objectReferenceValue = handleRT;
        joystickSerialized.FindProperty("canvas").objectReferenceValue = null; // Будет найден автоматически
        joystickSerialized.ApplyModifiedProperties();

        Debug.Log("[CreateBattleSceneUI] ✅ VirtualJoystick компонент добавлен и настроен");

        // ═══════════════════════════════════════════════════════════
        // ATTACK BUTTON (Bottom Right)
        // ═══════════════════════════════════════════════════════════
        GameObject attackButton = new GameObject("AttackButton");
        RectTransform attackRT = attackButton.AddComponent<RectTransform>();
        attackRT.SetParent(panel.transform, false);
        attackRT.anchorMin = new Vector2(1, 0);
        attackRT.anchorMax = new Vector2(1, 0);
        attackRT.pivot = new Vector2(1, 0);
        attackRT.anchoredPosition = new Vector2(-50, 50);
        attackRT.sizeDelta = new Vector2(120, 120);

        Button attackBtn = attackButton.AddComponent<Button>();
        attackBtn.transition = Selectable.Transition.ColorTint;

        Image attackImage = attackButton.AddComponent<Image>();
        attackImage.color = new Color(0.9f, 0.2f, 0.2f, 0.7f);

        // Attack Button Text
        GameObject attackTextGO = CreateText("Text", attackButton.transform, "⚔️", 48);
        RectTransform attackTextRT = attackTextGO.GetComponent<RectTransform>();
        attackTextRT.anchorMin = Vector2.zero;
        attackTextRT.anchorMax = Vector2.one;
        attackTextRT.offsetMin = Vector2.zero;
        attackTextRT.offsetMax = Vector2.zero;
        attackTextGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        return panel;
    }

    // ═══════════════════════════════════════════════════════════
    // HELPER: Resource Bar (HP/MP/AP)
    // ═══════════════════════════════════════════════════════════
    private static GameObject CreateResourceBar(string name, Transform parent, Color fillColor, string defaultText, Vector2 position)
    {
        GameObject bar = new GameObject(name);
        RectTransform rt = bar.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(-20, 30);

        // Background
        GameObject bgGO = new GameObject("Background");
        RectTransform bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.SetParent(bar.transform, false);
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        bgImage.raycastTarget = false;

        // Fill
        GameObject fillGO = new GameObject("Fill");
        RectTransform fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.SetParent(bar.transform, false);
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(1, 1);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        Image fillImage = fillGO.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 1f;
        fillImage.raycastTarget = false;

        // Text
        GameObject textGO = CreateText("Text", bar.transform, defaultText, 18);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        textGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        return bar;
    }

    // ═══════════════════════════════════════════════════════════
    // HELPER: Text
    // ═══════════════════════════════════════════════════════════
    private static GameObject CreateText(string name, Transform parent, string text, int fontSize)
    {
        GameObject textGO = new GameObject(name);
        RectTransform rt = textGO.AddComponent<RectTransform>();
        rt.SetParent(parent, false);

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return textGO;
    }
}
