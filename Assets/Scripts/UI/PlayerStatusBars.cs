using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Визуальные полоски HP/MP над головой игрока с отображением никнейма
/// </summary>
public class PlayerStatusBars : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0); // Смещение над головой
    [SerializeField] private float barWidth = 200f;
    [SerializeField] private float barHeight = 20f;
    [SerializeField] private float spacing = 5f; // Расстояние между барами

    private Canvas canvas;
    private GameObject barsContainer;
    private Image hpBarFill;
    private Image mpBarFill;
    private Text usernameText;
    private Text hpText;
    private Text mpText;

    private HealthSystem healthSystem;
    private ManaSystem manaSystem;
    private Transform playerTransform;

    void Start()
    {
        CreateBarsUI();
        FindPlayerSystems();
    }

    void Update()
    {
        // Если системы ещё не найдены - ищем (персонаж создаётся динамически)
        if (healthSystem == null || manaSystem == null)
        {
            FindPlayerSystems();
        }

        // Обновляем позицию баров над головой игрока
        if (playerTransform != null && barsContainer != null)
        {
            Vector3 worldPosition = playerTransform.position + offset;
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            barsContainer.transform.position = screenPosition;
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
            CharacterStats[] allStats = FindObjectsOfType<CharacterStats>();
            if (allStats.Length > 0)
            {
                player = allStats[0].gameObject;
                Debug.Log("[PlayerStatusBars] ✅ Игрок найден через CharacterStats");
            }
            else
            {
                return;
            }
        }

        playerTransform = player.transform;
        healthSystem = player.GetComponentInChildren<HealthSystem>();
        manaSystem = player.GetComponentInChildren<ManaSystem>();

        if (healthSystem != null && manaSystem != null)
        {
            Debug.Log($"[PlayerStatusBars] ✅ Системы найдены: HP={healthSystem.CurrentHealth}/{healthSystem.MaxHealth}, MP={manaSystem.CurrentMana}/{manaSystem.MaxMana}");

            // Устанавливаем никнейм пользователя
            string username = PlayerPrefs.GetString("Username", "Player");
            if (usernameText != null)
            {
                usernameText.text = username;
                Debug.Log($"[PlayerStatusBars] Никнейм установлен: {username}");
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

    private void CreateBarsUI()
    {
        Debug.Log("[PlayerStatusBars] 🔧 Создание HP/MP баров...");

        // Находим или создаём Canvas
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("StatusBars_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // Ниже HUD, но выше игры

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
            Debug.Log("[PlayerStatusBars] Canvas создан");
        }

        // Контейнер для баров
        barsContainer = new GameObject("PlayerBars");
        barsContainer.transform.SetParent(canvas.transform, false);

        RectTransform containerRect = barsContainer.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(barWidth, barHeight * 2 + spacing + 30); // +30 для никнейма

        // === НИКНЕЙМ ===
        GameObject usernameObj = new GameObject("Username");
        usernameObj.transform.SetParent(barsContainer.transform, false);

        RectTransform usernameRect = usernameObj.AddComponent<RectTransform>();
        usernameRect.anchorMin = new Vector2(0.5f, 1);
        usernameRect.anchorMax = new Vector2(0.5f, 1);
        usernameRect.pivot = new Vector2(0.5f, 1);
        usernameRect.anchoredPosition = new Vector2(0, 0);
        usernameRect.sizeDelta = new Vector2(barWidth, 25);

        usernameText = usernameObj.AddComponent<Text>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null) usernameText.font = font;
        usernameText.fontSize = 16;
        usernameText.color = Color.white;
        usernameText.alignment = TextAnchor.MiddleCenter;
        usernameText.fontStyle = FontStyle.Bold;
        usernameText.text = "Loading...";

        // Outline для читаемости
        Outline usernameOutline = usernameObj.AddComponent<Outline>();
        usernameOutline.effectColor = Color.black;
        usernameOutline.effectDistance = new Vector2(1, -1);

        // === HP БАР ===
        float hpBarY = -30; // Под никнеймом
        CreateBar("HPBar", hpBarY, Color.red, out hpBarFill, out hpText, "HP");

        // === MP БАР ===
        float mpBarY = hpBarY - barHeight - spacing;
        CreateBar("MPBar", mpBarY, new Color(0.2f, 0.5f, 1f), out mpBarFill, out mpText, "MP"); // Синий цвет

        Debug.Log("[PlayerStatusBars] ✅ HP/MP бары созданы");
    }

    private void CreateBar(string name, float yPosition, Color fillColor, out Image fillImage, out Text valueText, string label)
    {
        // Контейнер бара
        GameObject barObj = new GameObject(name);
        barObj.transform.SetParent(barsContainer.transform, false);

        RectTransform barRect = barObj.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 1);
        barRect.anchorMax = new Vector2(0.5f, 1);
        barRect.pivot = new Vector2(0.5f, 1);
        barRect.anchoredPosition = new Vector2(0, yPosition);
        barRect.sizeDelta = new Vector2(barWidth, barHeight);

        // Фон бара (тёмный)
        Image background = barObj.AddComponent<Image>();
        background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

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
        valueText.fontSize = 12;
        valueText.color = Color.white;
        valueText.alignment = TextAnchor.MiddleCenter;
        valueText.fontStyle = FontStyle.Bold;
        valueText.text = $"{label}: 0/0";

        // Outline для текста
        Outline textOutline = textObj.AddComponent<Outline>();
        textOutline.effectColor = Color.black;
        textOutline.effectDistance = new Vector2(1, -1);
    }
}
