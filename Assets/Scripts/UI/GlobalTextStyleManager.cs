using TMPro;
using UnityEngine;

/// <summary>
/// Менеджер для применения глобального стиля текста ко всем TextMeshPro элементам в игре
/// Применяет золотой фэнтези стиль автоматически
/// </summary>
public class GlobalTextStyleManager : MonoBehaviour
{
    [Header("Global Settings")]
    [SerializeField] private bool applyToAllTextsOnStart = true;
    [SerializeField] private bool applyToNewTexts = true;

    [Header("Font & Material")]
    [SerializeField] private TMP_FontAsset cinzelFont; // Cinzel Decorative шрифт
    [SerializeField] private Material goldenTextMaterial;

    [Header("Color Settings")]
    [SerializeField] private Color topGoldColor = new Color(0.98f, 0.84f, 0.47f, 1f); // Светлое золото
    [SerializeField] private Color bottomBronzeColor = new Color(0.65f, 0.48f, 0.15f, 1f); // Бронза
    [SerializeField] private Color outlineColor = new Color(0.15f, 0.1f, 0.05f, 1f);

    [Header("Effect Settings")]
    [SerializeField] private float outlineWidth = 0.2f;
    [SerializeField] private FontStyles fontStyle = FontStyles.Bold;

    private static GlobalTextStyleManager instance;

    public static GlobalTextStyleManager Instance
    {
        get { return instance; }
    }

    void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (applyToAllTextsOnStart)
        {
            ApplyStyleToAllTexts();
        }
    }

    /// <summary>
    /// Применяет золотой стиль ко всем TextMeshProUGUI в сцене
    /// </summary>
    public void ApplyStyleToAllTexts()
    {
        TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var text in allTexts)
        {
            ApplyGoldenStyle(text);
        }

        Debug.Log($"Применен золотой стиль к {allTexts.Length} текстовым элементам");
    }

    /// <summary>
    /// Применяет золотой стиль к конкретному текстовому элементу
    /// </summary>
    /// <param name="tmp">TextMeshProUGUI компонент</param>
    public void ApplyGoldenStyle(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;

        // НЕ МЕНЯЕМ ШРИФТ! Оставляем тот что есть (LiberationSans с кириллицей)
        // Применяем только цвета и эффекты
        // Если нужен кастомный шрифт - назначьте его вручную в Inspector

        // Базовый золотой цвет
        tmp.color = new Color(0.83f, 0.68f, 0.23f, 1f); // #D4AF37

        // Градиент от светлого золота к бронзе
        VertexGradient gradient = new VertexGradient(
            topGoldColor,      // верх левый
            topGoldColor,      // верх правый
            bottomBronzeColor, // низ левый
            bottomBronzeColor  // низ правый
        );
        tmp.colorGradient = gradient;

        // Настройка контура
        tmp.fontStyle = fontStyle;
        tmp.outlineColor = outlineColor;
        tmp.outlineWidth = outlineWidth;

        // Включаем богатый текст
        tmp.richText = true;

        // Применяем материал с эффектами если задан
        if (goldenTextMaterial != null)
        {
            Material materialInstance = new Material(goldenTextMaterial);
            tmp.fontMaterial = materialInstance;
        }
    }

    /// <summary>
    /// Применяет стиль к новому тексту (вызывай при создании UI)
    /// </summary>
    /// <param name="tmp">Новый текстовый элемент</param>
    public static void ApplyToNewText(TextMeshProUGUI tmp)
    {
        if (Instance != null && Instance.applyToNewTexts)
        {
            Instance.ApplyGoldenStyle(tmp);
        }
    }

    /// <summary>
    /// Обновляет глобальные настройки цвета
    /// </summary>
    public void UpdateGlobalColors(Color topColor, Color bottomColor, Color outline)
    {
        topGoldColor = topColor;
        bottomBronzeColor = bottomColor;
        outlineColor = outline;

        ApplyStyleToAllTexts();
    }
}
