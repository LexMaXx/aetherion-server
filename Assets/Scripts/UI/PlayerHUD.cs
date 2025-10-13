using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD игрока - HP/MP бары вверху слева с иконкой класса
/// Action Points внизу по центру
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float barWidth = 250f;
    [SerializeField] private float barHeight = 25f;
    [SerializeField] private float spacing = 5f;

    private Canvas canvas;
    private GameObject hudContainer;

    // HP/MP элементы
    private Image classIcon;
    private Image hpBarFill;
    private Image mpBarFill;
    private Text hpText;
    private Text mpText;

    // Системы
    private HealthSystem healthSystem;
    private ManaSystem manaSystem;
    private CharacterStats characterStats;

    void Start()
    {
        CreateHUD();
        FindPlayerSystems();
    }

    void Update()
    {
        // Если системы ещё не найдены - ищем (персонаж создаётся динамически)
        if (healthSystem == null || manaSystem == null)
        {
            FindPlayerSystems();
        }

        // Обновляем значения баров
        if (healthSystem != null && manaSystem != null)
        {
            UpdateBars();
        }
    }

    private void FindPlayerSystems()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            CharacterStats[] allStats = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
            if (allStats.Length > 0)
            {
                player = allStats[0].gameObject;
            }
            else
            {
                return;
            }
        }

        healthSystem = player.GetComponentInChildren<HealthSystem>();
        manaSystem = player.GetComponentInChildren<ManaSystem>();
        characterStats = player.GetComponentInChildren<CharacterStats>();

        if (healthSystem != null && manaSystem != null)
        {
            Debug.Log($"[PlayerHUD] ✅ Системы найдены: HP={healthSystem.CurrentHealth}/{healthSystem.MaxHealth}, MP={manaSystem.CurrentMana}/{manaSystem.MaxMana}");

            // Загружаем иконку класса
            if (characterStats != null && classIcon != null)
            {
                LoadClassIcon(characterStats.ClassName);
            }
        }
    }

    private void UpdateBars()
    {
        // HP бар
        float hpPercent = healthSystem.HealthPercent;
        if (hpBarFill != null)
        {
            hpBarFill.fillAmount = hpPercent;

            // Цвет зависит от процента HP
            if (hpPercent > 0.5f)
                hpBarFill.color = Color.green;
            else if (hpPercent > 0.25f)
                hpBarFill.color = Color.yellow;
            else
                hpBarFill.color = Color.red;
        }

        if (hpText != null)
        {
            hpText.text = $"{healthSystem.CurrentHealth:F0}/{healthSystem.MaxHealth:F0}";
        }

        // MP бар
        float mpPercent = manaSystem.ManaPercent;
        if (mpBarFill != null)
        {
            mpBarFill.fillAmount = mpPercent;
        }

        if (mpText != null)
        {
            mpText.text = $"{manaSystem.CurrentMana:F0}/{manaSystem.MaxMana:F0}";
        }
    }

    private void CreateHUD()
    {
        Debug.Log("[PlayerHUD] 🔧 Создание HUD...");

        // Находим или создаём Canvas
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HUD_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // === КОНТЕЙНЕР HUD (верхний левый угол) ===
        hudContainer = new GameObject("PlayerHUD");
        hudContainer.transform.SetParent(canvas.transform, false);

        RectTransform hudRect = hudContainer.AddComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0, 1);
        hudRect.anchorMax = new Vector2(0, 1);
        hudRect.pivot = new Vector2(0, 1);
        hudRect.anchoredPosition = new Vector2(20, -20); // 20px от края
        hudRect.sizeDelta = new Vector2(80 + barWidth, barHeight * 2 + spacing + 10); // Иконка 80px

        // === ИКОНКА КЛАССА ===
        GameObject iconObj = new GameObject("ClassIcon");
        iconObj.transform.SetParent(hudContainer.transform, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(70, 70);

        classIcon = iconObj.AddComponent<Image>();
        classIcon.color = Color.white;

        // Фон для иконки
        GameObject iconBgObj = new GameObject("IconBackground");
        iconBgObj.transform.SetParent(iconObj.transform, false);
        iconBgObj.transform.SetAsFirstSibling(); // Позади иконки

        RectTransform iconBgRect = iconBgObj.AddComponent<RectTransform>();
        iconBgRect.anchorMin = Vector2.zero;
        iconBgRect.anchorMax = Vector2.one;
        iconBgRect.offsetMin = new Vector2(-5, -5);
        iconBgRect.offsetMax = new Vector2(5, 5);

        Image iconBg = iconBgObj.AddComponent<Image>();
        iconBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // === HP БАР ===
        CreateBar("HPBar", 80, 5, Color.red, out hpBarFill, out hpText, "HP");

        // === MP БАР ===
        CreateBar("MPBar", 80, -(barHeight + spacing + 5), new Color(0.2f, 0.5f, 1f), out mpBarFill, out mpText, "MP");

        Debug.Log("[PlayerHUD] ✅ HUD создан");
    }

    private void CreateBar(string name, float xPosition, float yPosition, Color fillColor, out Image fillImage, out Text valueText, string label)
    {
        // Контейнер бара
        GameObject barObj = new GameObject(name);
        barObj.transform.SetParent(hudContainer.transform, false);

        RectTransform barRect = barObj.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0, 0.5f);
        barRect.anchorMax = new Vector2(0, 0.5f);
        barRect.pivot = new Vector2(0, 0.5f);
        barRect.anchoredPosition = new Vector2(xPosition, yPosition);
        barRect.sizeDelta = new Vector2(barWidth, barHeight);

        // Фон бара (тёмный)
        Image background = barObj.AddComponent<Image>();
        background.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        // Outline для фона
        Outline bgOutline = barObj.AddComponent<Outline>();
        bgOutline.effectColor = Color.black;
        bgOutline.effectDistance = new Vector2(2, -2);

        // Fill (заполнение бара)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barObj.transform, false);

        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.zero;
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(barWidth, barHeight);

        fillImage = fillObj.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 1f;

        // Текст со значением
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(barObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        valueText = textObj.AddComponent<Text>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null) valueText.font = font;
        valueText.fontSize = 14;
        valueText.color = Color.white;
        valueText.alignment = TextAnchor.MiddleCenter;
        valueText.fontStyle = FontStyle.Bold;
        valueText.text = $"{label}: 0/0";

        // Outline для текста
        Outline textOutline = textObj.AddComponent<Outline>();
        textOutline.effectColor = Color.black;
        textOutline.effectDistance = new Vector2(1, -1);
    }

    private void LoadClassIcon(string className)
    {
        // Ищем иконку в Resources/UI/ClassIcons/
        Sprite icon = Resources.Load<Sprite>($"UI/ClassIcons/{className}");

        if (icon != null && classIcon != null)
        {
            classIcon.sprite = icon;
            Debug.Log($"[PlayerHUD] ✅ Иконка класса {className} загружена");
        }
        else
        {
            Debug.LogWarning($"[PlayerHUD] ⚠️ Иконка класса {className} не найдена в Resources/UI/ClassIcons/");
            // Можно установить дефолтную иконку
        }
    }
}
