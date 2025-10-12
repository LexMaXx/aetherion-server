using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class FantasyTextStyle : MonoBehaviour
{
    [Header("Golden Fantasy Text Settings")]
    [SerializeField] private bool applyOnStart = true;

    [Header("Font Settings")]
    [SerializeField] private TMP_FontAsset cinzelFont; // Cinzel Decorative шрифт

    [Header("Colors")]
    [SerializeField] private Color topGoldColor = new Color(0.98f, 0.84f, 0.47f, 1f); // Светлое золото
    [SerializeField] private Color bottomBronzeColor = new Color(0.65f, 0.48f, 0.15f, 1f); // Бронза
    [SerializeField] private Color outlineColor = new Color(0.15f, 0.1f, 0.05f, 1f); // Темный контур

    [Header("Effect Settings")]
    [SerializeField] private float outlineWidth = 0.2f;
    [SerializeField] private FontStyles fontStyle = FontStyles.Bold;

    private TextMeshProUGUI tmp;

    void Start()
    {
        if (applyOnStart)
        {
            ApplyGoldenStyle();
        }
    }

    public void ApplyGoldenStyle()
    {
        tmp = GetComponent<TextMeshProUGUI>();

        // Применяем шрифт Cinzel Decorative
        if (cinzelFont != null)
        {
            tmp.font = cinzelFont;
        }
        else
        {
            Debug.LogWarning("Cinzel Decorative шрифт не назначен! Назначьте в Inspector.");
        }

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

        // Настройка контура (emboss эффект)
        tmp.fontStyle = fontStyle;
        tmp.outlineColor = outlineColor;
        tmp.outlineWidth = outlineWidth;

        // Включаем богатый текст для дополнительных эффектов
        tmp.richText = true;
    }

    // Метод для изменения цвета в рантайме
    public void SetCustomGradient(Color top, Color bottom)
    {
        if (tmp == null) tmp = GetComponent<TextMeshProUGUI>();

        VertexGradient gradient = new VertexGradient(top, top, bottom, bottom);
        tmp.colorGradient = gradient;
    }
}
