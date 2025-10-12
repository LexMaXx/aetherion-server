using TMPro;
using UnityEngine;

/// <summary>
/// Продвинутый стиль текста в стиле Aetherion с 3D рельефом и металлическим эффектом
/// Максимально приближен к логотипу игры
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class AetherionTextStyle : MonoBehaviour
{
    [Header("Font Settings")]
    [SerializeField] private TMP_FontAsset cinzelFont;

    [Header("3D Emboss Effect")]
    // [SerializeField] private bool enable3DEffect = true; // Закомментировано: не используется
    [SerializeField] private float faceDilate = 0.15f; // Толщина букв

    [Header("Metallic Gold Colors")]
    [SerializeField] private Color highlightGold = new Color(1f, 0.95f, 0.7f, 1f); // Яркий блик #FFF2B3
    [SerializeField] private Color midGold = new Color(0.83f, 0.68f, 0.23f, 1f); // Средний золотой #D4AF37
    [SerializeField] private Color deepGold = new Color(0.55f, 0.42f, 0.12f, 1f); // Темное золото #8C6B1E
    [SerializeField] private Color shadowBrown = new Color(0.18f, 0.12f, 0.05f, 1f); // Темная тень #2E1F0D

    [Header("Outline Settings")]
    [SerializeField] private float outlineThickness = 0.25f;
    [SerializeField] private Color outlineColor = new Color(0.1f, 0.06f, 0.02f, 1f); // Почти черный

    [Header("Underlay (3D Shadow)")]
    [SerializeField] private bool enableUnderlay = true;
    [SerializeField] private Vector2 underlayOffset = new Vector2(0.4f, -0.4f);
    [SerializeField] private Color underlayColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private float underlayDilate = 0.3f;
    [SerializeField] private float underlaySoftness = 0.15f;

    [Header("Glow Effect")]
    [SerializeField] private bool enableGlow = true;
    [SerializeField] private Color glowColor = new Color(1f, 0.84f, 0.4f, 0.5f); // Золотое свечение
    [SerializeField] private float glowPower = 0.5f;

    private TextMeshProUGUI tmp;
    private Material materialInstance;

    void Start()
    {
        ApplyAetherionStyle();
    }

    public void ApplyAetherionStyle()
    {
        tmp = GetComponent<TextMeshProUGUI>();

        // Применяем шрифт
        if (cinzelFont != null)
        {
            tmp.font = cinzelFont;
        }

        // Создаем материал для расширенных эффектов
        if (tmp.fontMaterial != null)
        {
            materialInstance = new Material(tmp.fontMaterial);
            tmp.fontMaterial = materialInstance;
        }

        // Базовый цвет (средний золотой)
        tmp.color = midGold;

        // Градиент для металлического эффекта (от светлого сверху к темному снизу)
        VertexGradient gradient = new VertexGradient(
            highlightGold,  // верх левый - светлый блик
            highlightGold,  // верх правый - светлый блик
            deepGold,       // низ левый - темное золото
            deepGold        // низ правый - темное золото
        );
        tmp.colorGradient = gradient;

        // Контур (темный, создает рельеф)
        tmp.fontStyle = FontStyles.Bold;
        tmp.outlineColor = outlineColor;
        tmp.outlineWidth = outlineThickness;

        // Применяем материал настройки если есть инстанс
        if (materialInstance != null)
        {
            // Face Dilate для объемности
            if (materialInstance.HasProperty("_FaceDilate"))
            {
                materialInstance.SetFloat("_FaceDilate", faceDilate);
            }

            // Underlay для 3D тени
            if (enableUnderlay)
            {
                if (materialInstance.HasProperty("_UnderlayColor"))
                {
                    materialInstance.SetColor("_UnderlayColor", underlayColor);
                }
                if (materialInstance.HasProperty("_UnderlayOffsetX"))
                {
                    materialInstance.SetFloat("_UnderlayOffsetX", underlayOffset.x);
                    materialInstance.SetFloat("_UnderlayOffsetY", underlayOffset.y);
                }
                if (materialInstance.HasProperty("_UnderlayDilate"))
                {
                    materialInstance.SetFloat("_UnderlayDilate", underlayDilate);
                }
                if (materialInstance.HasProperty("_UnderlaySoftness"))
                {
                    materialInstance.SetFloat("_UnderlaySoftness", underlaySoftness);
                }
            }

            // Glow для свечения
            if (enableGlow)
            {
                if (materialInstance.HasProperty("_GlowColor"))
                {
                    materialInstance.SetColor("_GlowColor", glowColor);
                }
                if (materialInstance.HasProperty("_GlowPower"))
                {
                    materialInstance.SetFloat("_GlowPower", glowPower);
                }
                if (materialInstance.HasProperty("_GlowOuter"))
                {
                    materialInstance.SetFloat("_GlowOuter", 0.4f);
                }
            }
        }

        tmp.richText = true;
    }

    /// <summary>
    /// Анимация пульсации свечения (для заголовков)
    /// </summary>
    public void EnableGlowPulse(float speed = 1f)
    {
        if (materialInstance != null && enableGlow)
        {
            StartCoroutine(GlowPulseCoroutine(speed));
        }
    }

    private System.Collections.IEnumerator GlowPulseCoroutine(float speed)
    {
        while (true)
        {
            float glow = Mathf.Lerp(0.3f, 0.8f, (Mathf.Sin(Time.time * speed) + 1f) / 2f);

            if (materialInstance != null && materialInstance.HasProperty("_GlowPower"))
            {
                materialInstance.SetFloat("_GlowPower", glow);
            }

            yield return null;
        }
    }

    void OnDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}
