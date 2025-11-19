using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Анимация появления логотипа с fade in и увеличением scale
/// Идеально для меню и заставок
/// </summary>
public class LogoAppearanceAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 2f; // Длительность анимации
    [SerializeField] private float startDelay = 0.5f; // Задержка перед началом

    [Header("Scale Settings")]
    [SerializeField] private float startScale = 0.3f; // Начальный размер (30% от финального)
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Fade Settings")]
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Additional Effects")]
    [SerializeField] private bool enableRotation = false; // Вращение при появлении
    [SerializeField] private float rotationAmount = 360f; // Градусы вращения

    private Image logoImage;
    private CanvasGroup canvasGroup;
    private Vector3 targetScale;
    private Color originalColor;

    void Start()
    {
        // Сохраняем целевой scale
        targetScale = transform.localScale;

        // Получаем компоненты
        logoImage = GetComponent<Image>();

        // Добавляем CanvasGroup если его нет (для плавного fade)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Сохраняем оригинальный цвет
        if (logoImage != null)
        {
            originalColor = logoImage.color;
        }

        // Устанавливаем начальное состояние
        transform.localScale = targetScale * startScale;
        canvasGroup.alpha = 0f;

        // Запускаем анимацию
        StartCoroutine(AnimateLogo());
    }

    private IEnumerator AnimateLogo()
    {
        // Задержка перед началом
        if (startDelay > 0)
        {
            yield return new WaitForSeconds(startDelay);
        }

        float elapsedTime = 0f;
        Vector3 startScaleVec = targetScale * startScale;
        float startRotation = enableRotation ? rotationAmount : 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;

            // Применяем scale с кривой
            float scaleProgress = scaleCurve.Evaluate(progress);
            transform.localScale = Vector3.Lerp(startScaleVec, targetScale, scaleProgress);

            // Применяем fade с кривой
            float fadeProgress = fadeCurve.Evaluate(progress);
            canvasGroup.alpha = fadeProgress;

            // Опциональное вращение
            if (enableRotation)
            {
                float currentRotation = Mathf.Lerp(startRotation, 0f, progress);
                transform.rotation = Quaternion.Euler(0, 0, currentRotation);
            }

            yield return null;
        }

        // Финальные значения
        transform.localScale = targetScale;
        canvasGroup.alpha = 1f;

        if (enableRotation)
        {
            transform.rotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Запустить анимацию заново (можно вызвать из кода)
    /// </summary>
    public void PlayAnimation()
    {
        StopAllCoroutines();
        transform.localScale = targetScale * startScale;
        canvasGroup.alpha = 0f;
        StartCoroutine(AnimateLogo());
    }

    /// <summary>
    /// Анимация исчезновения (обратная)
    /// </summary>
    public void FadeOut(float duration = 1f)
    {
        StartCoroutine(FadeOutCoroutine(duration));
    }

    private IEnumerator FadeOutCoroutine(float duration)
    {
        float elapsedTime = 0f;
        Vector3 startScaleVec = transform.localScale;
        float startAlpha = canvasGroup.alpha;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            transform.localScale = Vector3.Lerp(startScaleVec, targetScale * startScale, progress);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);

            yield return null;
        }

        transform.localScale = targetScale * startScale;
        canvasGroup.alpha = 0f;
    }
}
