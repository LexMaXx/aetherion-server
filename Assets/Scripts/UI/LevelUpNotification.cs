using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Визуальное уведомление о получении уровня
/// Показывает анимированное сообщение "LEVEL UP!" с эффектами
/// </summary>
public class LevelUpNotification : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private Text levelUpText;
    [SerializeField] private Text levelNumberText;
    [SerializeField] private Text bonusText;
    [SerializeField] private Image glowImage;

    [Header("Animation Settings")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float scaleUpAmount = 1.2f;
    [SerializeField] private float glowPulseDuration = 1f;

    [Header("Sound")]
    [SerializeField] private AudioClip levelUpSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem levelUpParticles;

    private static LevelUpNotification instance;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private bool isShowing = false;

    public static LevelUpNotification Instance => instance;

    void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Получаем компоненты
        if (notificationPanel != null)
        {
            canvasGroup = notificationPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = notificationPanel.AddComponent<CanvasGroup>();
            }

            rectTransform = notificationPanel.GetComponent<RectTransform>();
        }

        // Создаем AudioSource если нет
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Скрываем панель
        HideImmediate();
    }

    void Start()
    {
        // Подписываемся на события прокачки локального игрока
        StartCoroutine(FindAndSubscribeToLevelingSystem());
    }

    /// <summary>
    /// Найти LevelingSystem локального игрока и подписаться на события
    /// </summary>
    private IEnumerator FindAndSubscribeToLevelingSystem()
    {
        LevelingSystem levelingSystem = null;

        // Пытаемся найти систему несколько раз
        for (int i = 0; i < 10; i++)
        {
            // Ищем локального игрока
            LocalPlayerEntity localPlayer = FindFirstObjectByType<LocalPlayerEntity>();
            if (localPlayer != null)
            {
                levelingSystem = localPlayer.GetComponent<LevelingSystem>();
                if (levelingSystem != null)
                {
                    break;
                }
            }

            yield return new WaitForSeconds(0.5f);
        }

        if (levelingSystem != null)
        {
            levelingSystem.OnLevelUp += OnLocalPlayerLevelUp;
            Debug.Log("[LevelUpNotification] ✅ Подписка на OnLevelUp локального игрока");
        }
        else
        {
            Debug.LogWarning("[LevelUpNotification] ⚠️ LevelingSystem не найден!");
        }
    }

    /// <summary>
    /// Обработчик получения уровня локальным игроком
    /// </summary>
    private void OnLocalPlayerLevelUp(int newLevel)
    {
        ShowLevelUp(newLevel, true);
    }

    /// <summary>
    /// Показать уведомление о получении уровня
    /// </summary>
    /// <param name="newLevel">Новый уровень</param>
    /// <param name="isLocalPlayer">Это локальный игрок (true) или другой игрок (false)</param>
    public void ShowLevelUp(int newLevel, bool isLocalPlayer = true)
    {
        if (isShowing)
        {
            // Если уже показываем - останавливаем предыдущую анимацию
            StopAllCoroutines();
        }

        StartCoroutine(ShowLevelUpAnimation(newLevel, isLocalPlayer));
    }

    /// <summary>
    /// Анимация показа уведомления
    /// </summary>
    private IEnumerator ShowLevelUpAnimation(int newLevel, bool isLocalPlayer)
    {
        isShowing = true;

        // Показываем панель
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(true);
        }

        // Обновляем текст
        if (levelUpText != null)
        {
            levelUpText.text = isLocalPlayer ? "LEVEL UP!" : "LEVEL UP";
        }

        if (levelNumberText != null)
        {
            levelNumberText.text = $"Level {newLevel}";
        }

        if (bonusText != null && isLocalPlayer)
        {
            bonusText.text = "+1 Stat Point Available";
        }
        else if (bonusText != null)
        {
            bonusText.text = "";
        }

        // Воспроизводим звук
        if (levelUpSound != null && audioSource != null && isLocalPlayer)
        {
            audioSource.PlayOneShot(levelUpSound);
        }

        // Запускаем частицы
        if (levelUpParticles != null && isLocalPlayer)
        {
            levelUpParticles.Play();
        }

        // Fade in + Scale up анимация
        float elapsed = 0f;
        Vector3 startScale = rectTransform != null ? Vector3.one * 0.5f : Vector3.one;
        Vector3 targetScale = Vector3.one;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;

            // Fade in
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            }

            // Scale up с overshoot эффектом
            if (rectTransform != null)
            {
                float scale = Mathf.Lerp(0.5f, scaleUpAmount, t);
                rectTransform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        // Возвращаем нормальный масштаб
        if (rectTransform != null)
        {
            float elapsed2 = 0f;
            while (elapsed2 < 0.3f)
            {
                elapsed2 += Time.deltaTime;
                float t = elapsed2 / 0.3f;
                float scale = Mathf.Lerp(scaleUpAmount, 1f, t);
                rectTransform.localScale = Vector3.one * scale;
                yield return null;
            }
            rectTransform.localScale = Vector3.one;
        }

        // Запускаем пульсацию свечения
        if (glowImage != null)
        {
            StartCoroutine(PulseGlow());
        }

        // Ждем
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            }

            yield return null;
        }

        // Скрываем панель
        HideImmediate();
        isShowing = false;
    }

    /// <summary>
    /// Пульсация свечения
    /// </summary>
    private IEnumerator PulseGlow()
    {
        if (glowImage == null) yield break;

        Color startColor = glowImage.color;
        float elapsed = 0f;

        while (elapsed < displayDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed / glowPulseDuration, 1f);

            Color newColor = startColor;
            newColor.a = Mathf.Lerp(0.3f, 1f, t);
            glowImage.color = newColor;

            yield return null;
        }

        glowImage.color = startColor;
    }

    /// <summary>
    /// Немедленно скрыть панель
    /// </summary>
    private void HideImmediate()
    {
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// Показать уведомление о получении уровня другим игроком
    /// </summary>
    public void ShowOtherPlayerLevelUp(string playerName, int newLevel)
    {
        // TODO: Можно сделать отдельное уведомление для других игроков
        // Например, маленькое сообщение в углу экрана
        Debug.Log($"[LevelUpNotification] {playerName} достиг уровня {newLevel}!");
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        LevelingSystem levelingSystem = FindFirstObjectByType<LevelingSystem>();
        if (levelingSystem != null)
        {
            levelingSystem.OnLevelUp -= OnLocalPlayerLevelUp;
        }
    }
}
