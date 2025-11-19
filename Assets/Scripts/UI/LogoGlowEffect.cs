using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Эффект свечения и пульсации для логотипа
/// Дополнительный эффект к LogoAppearanceAnimation
/// </summary>
public class LogoGlowEffect : MonoBehaviour
{
    [Header("Glow Pulse Settings")]
    [SerializeField] private bool enableGlowPulse = true;
    [SerializeField] private float pulseSpeed = 1f;
    [SerializeField] private float minGlow = 0.7f;
    [SerializeField] private float maxGlow = 1.3f;

    [Header("Scale Pulse Settings")]
    [SerializeField] private bool enableScalePulse = false;
    [SerializeField] private float scalePulseSpeed = 0.5f;
    [SerializeField] private float scalePulseAmount = 0.05f; // 5% увеличение

    [Header("Color Settings")]
    [SerializeField] private bool enableColorShift = false;
    [SerializeField] private Color glowColor = new Color(1f, 0.9f, 0.6f, 1f); // Золотой

    [Header("Floating Effect (Плаванье)")]
    [SerializeField] private bool enableFloating = true;
    [SerializeField] private float floatingSpeed = 1f; // Скорость плаванья
    [SerializeField] private float floatingAmplitudeY = 10f; // Амплитуда по вертикали (в пикселях)
    [SerializeField] private float floatingAmplitudeX = 5f; // Амплитуда по горизонтали (в пикселях)
    [SerializeField] private bool use8Pattern = true; // Движение "восьмеркой"

    [Header("Rotation Float")]
    [SerializeField] private bool enableFloatingRotation = false;
    [SerializeField] private float rotationSpeed = 0.5f;
    [SerializeField] private float rotationAmount = 5f; // Градусы покачивания

    private Image logoImage;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Color originalColor;
    private Material materialInstance;

    void Start()
    {
        logoImage = GetComponent<Image>();
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;

        if (logoImage != null)
        {
            originalColor = logoImage.color;

            // Создаем копию материала для независимого управления
            if (logoImage.material != null)
            {
                materialInstance = new Material(logoImage.material);
                logoImage.material = materialInstance;
            }
        }
    }

    void Update()
    {
        if (logoImage == null) return;

        float time = Time.time;

        // Пульсация свечения
        if (enableGlowPulse)
        {
            float glow = Mathf.Lerp(minGlow, maxGlow, (Mathf.Sin(time * pulseSpeed) + 1f) / 2f);

            if (enableColorShift)
            {
                logoImage.color = Color.Lerp(originalColor, glowColor, glow - 1f);
            }
        }

        // Пульсация размера
        if (enableScalePulse)
        {
            float pulse = Mathf.Sin(time * scalePulseSpeed) * scalePulseAmount;
            transform.localScale = originalScale * (1f + pulse);
        }

        // Эффект плаванья
        if (enableFloating)
        {
            Vector3 newPosition = originalPosition;

            if (use8Pattern)
            {
                // Движение "восьмеркой" (Lemniscate of Bernoulli)
                float x = Mathf.Sin(time * floatingSpeed) * floatingAmplitudeX;
                float y = Mathf.Sin(time * floatingSpeed * 2f) * floatingAmplitudeY * 0.5f;
                newPosition += new Vector3(x, y, 0);
            }
            else
            {
                // Простое плавное движение вверх-вниз и влево-вправо
                float x = Mathf.Sin(time * floatingSpeed) * floatingAmplitudeX;
                float y = Mathf.Sin(time * floatingSpeed * 0.7f) * floatingAmplitudeY; // Разная частота для естественности
                newPosition += new Vector3(x, y, 0);
            }

            transform.localPosition = newPosition;
        }

        // Покачивание (вращение)
        if (enableFloatingRotation)
        {
            float rotation = Mathf.Sin(time * rotationSpeed) * rotationAmount;
            transform.localRotation = Quaternion.Euler(0, 0, rotation);
        }
    }

    /// <summary>
    /// Включить/выключить эффекты
    /// </summary>
    public void SetEffectsEnabled(bool enabled)
    {
        enableGlowPulse = enabled;
        enableScalePulse = enabled;
        enableFloating = enabled;
        enableFloatingRotation = enabled;

        if (!enabled)
        {
            transform.localScale = originalScale;
            transform.localPosition = originalPosition;
            transform.localRotation = Quaternion.identity;

            if (logoImage != null)
            {
                logoImage.color = originalColor;
            }
        }
    }

    /// <summary>
    /// Изменить паттерн плаванья в рантайме
    /// </summary>
    public void SetFloatingPattern(bool use8)
    {
        use8Pattern = use8;
    }

    /// <summary>
    /// Изменить скорость и амплитуду плаванья
    /// </summary>
    public void SetFloatingParameters(float speed, float amplitudeY, float amplitudeX)
    {
        floatingSpeed = speed;
        floatingAmplitudeY = amplitudeY;
        floatingAmplitudeX = amplitudeX;
    }

    void OnDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}
