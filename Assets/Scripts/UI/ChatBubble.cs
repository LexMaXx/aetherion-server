using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Всплывающее сообщение над головой игрока (как в Lineage 2)
/// Белый текст на черном фоне с полупрозрачностью
/// </summary>
public class ChatBubble : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform backgroundRect;

    private Transform targetTransform;
    private Vector3 offset;
    private float displayTime;
    private float fadeDuration;
    private Camera mainCamera;

    /// <summary>
    /// Инициализация всплывающего сообщения
    /// </summary>
    public void Initialize(Transform target, string message, float display, float fade, Vector3 bubbleOffset)
    {
        targetTransform = target;
        displayTime = display;
        fadeDuration = fade;
        offset = bubbleOffset;
        mainCamera = Camera.main;

        // Устанавливаем текст
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = Color.white;
        }

        // Запускаем корутину исчезновения
        StartCoroutine(FadeOutAfterDelay());

        Debug.Log($"[ChatBubble] 💬 Создано сообщение: {message}");
    }

    void Update()
    {
        // Следуем за игроком
        if (targetTransform != null && mainCamera != null)
        {
            Vector3 worldPosition = targetTransform.position + offset;
            transform.position = worldPosition;

            // Billboard эффект (смотрим на камеру)
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }
        else if (targetTransform == null)
        {
            // Если игрок уничтожен - уничтожаем сообщение
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Корутина: показываем сообщение, затем плавно исчезаем
    /// </summary>
    private IEnumerator FadeOutAfterDelay()
    {
        // Показываем сообщение
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        // Ждем displayTime
        yield return new WaitForSeconds(displayTime);

        // Плавно исчезаем
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }

        // Уничтожаем объект
        Destroy(gameObject);
    }
}
