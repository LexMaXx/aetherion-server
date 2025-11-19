using TMPro;
using UnityEngine;

/// <summary>
/// Автоматически применяет золотой стиль при активации объекта
/// Добавь этот компонент на любой UI элемент с TextMeshProUGUI
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
[ExecuteInEditMode] // Работает и в Editor режиме
public class AutoApplyGoldenText : MonoBehaviour
{
    [Header("Auto Apply Settings")]
    [SerializeField] private bool applyOnAwake = true;
    [SerializeField] private bool applyInEditor = true;

    [Header("Font Settings")]
    [SerializeField] private TMP_FontAsset cinzelFont; // Cinzel Decorative

    private TextMeshProUGUI tmp;
    private bool hasApplied = false;

    void Awake()
    {
        if (applyOnAwake && Application.isPlaying)
        {
            ApplyStyle();
        }
    }

    void OnEnable()
    {
        if (applyInEditor && !Application.isPlaying && !hasApplied)
        {
            ApplyStyle();
            hasApplied = true;
        }
    }

    void Reset()
    {
        // Вызывается при добавлении компонента
        ApplyStyle();
    }

    [ContextMenu("Apply Golden Style")]
    public void ApplyStyle()
    {
        if (tmp == null)
            tmp = GetComponent<TextMeshProUGUI>();

        if (tmp == null)
        {
            Debug.LogWarning("TextMeshProUGUI не найден на объекте!");
            return;
        }

        // НЕ МЕНЯЕМ ШРИФТ! Оставляем тот что есть (LiberationSans с кириллицей)
        // Применяем только цвета и эффекты

        // Базовый золотой цвет
        tmp.color = new Color(0.83f, 0.68f, 0.23f, 1f); // #D4AF37

        // Градиент от светлого золота к бронзе
        VertexGradient gradient = new VertexGradient(
            new Color(0.98f, 0.84f, 0.47f, 1f), // верх - светлое золото
            new Color(0.98f, 0.84f, 0.47f, 1f),
            new Color(0.65f, 0.48f, 0.15f, 1f), // низ - бронза
            new Color(0.65f, 0.48f, 0.15f, 1f)
        );
        tmp.colorGradient = gradient;

        // Настройка контура
        tmp.fontStyle = FontStyles.Bold;
        tmp.outlineColor = new Color(0.15f, 0.1f, 0.05f, 1f);
        tmp.outlineWidth = 0.2f;
        tmp.richText = true;

        Debug.Log($"✅ Золотой стиль применен к {tmp.gameObject.name}");
    }
}
