using TMPro;
using UnityEngine;

/// <summary>
/// Настройка материала для золотого текста с эффектом свечения
/// Используется для создания рельефного фэнтези-текста
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class GoldenTextMaterialSetup : MonoBehaviour
{
    [Header("Material Settings")]
    [SerializeField] private Material goldenMaterial;

    [Header("Glow Settings")]
    [SerializeField] private Color glowColor = new Color(1f, 0.84f, 0f, 1f); // #FFD700
    [SerializeField] private float glowPower = 0.75f;
    [SerializeField] private float glowInner = 0.2f;
    [SerializeField] private float glowOuter = 0.3f;

    [Header("Shadow Settings")]
    [SerializeField] private Color shadowColor = new Color(0.29f, 0.2f, 0.06f, 0.8f);
    [SerializeField] private Vector2 shadowOffset = new Vector2(0.3f, -0.3f);
    [SerializeField] private float shadowDilate = 0.5f;
    [SerializeField] private float shadowSoftness = 0.2f;

    private TextMeshProUGUI tmp;
    private Material materialInstance;

    void Start()
    {
        tmp = GetComponent<TextMeshProUGUI>();

        if (goldenMaterial != null)
        {
            // Создаем копию материала для этого текста
            materialInstance = new Material(goldenMaterial);
            tmp.fontMaterial = materialInstance;

            ApplyMaterialSettings();
        }
        else
        {
            Debug.LogWarning("Golden Material не назначен! Назначьте материал в Inspector.");
        }
    }

    private void ApplyMaterialSettings()
    {
        if (materialInstance == null) return;

        // Настройка Glow
        if (materialInstance.HasProperty("_GlowColor"))
        {
            materialInstance.SetColor("_GlowColor", glowColor);
            materialInstance.SetFloat("_GlowPower", glowPower);
            materialInstance.SetFloat("_GlowInner", glowInner);
            materialInstance.SetFloat("_GlowOuter", glowOuter);
        }

        // Настройка тени (Underlay)
        if (materialInstance.HasProperty("_UnderlayColor"))
        {
            materialInstance.SetColor("_UnderlayColor", shadowColor);
            materialInstance.SetFloat("_UnderlayOffsetX", shadowOffset.x);
            materialInstance.SetFloat("_UnderlayOffsetY", shadowOffset.y);
            materialInstance.SetFloat("_UnderlayDilate", shadowDilate);
            materialInstance.SetFloat("_UnderlaySoftness", shadowSoftness);
        }
    }

    void OnDestroy()
    {
        // Освобождаем память от копии материала
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }

    // Метод для изменения интенсивности свечения в рантайме
    public void SetGlowIntensity(float intensity)
    {
        if (materialInstance != null && materialInstance.HasProperty("_GlowPower"))
        {
            materialInstance.SetFloat("_GlowPower", Mathf.Clamp01(intensity));
        }
    }

    // Метод для пульсации свечения (можно использовать для анимации)
    public void PulseGlow(float speed = 1f, float minIntensity = 0.3f, float maxIntensity = 1f)
    {
        float pulse = Mathf.Lerp(minIntensity, maxIntensity,
            (Mathf.Sin(Time.time * speed) + 1f) / 2f);
        SetGlowIntensity(pulse);
    }
}
