using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Экран загрузки с прогресс-баром и подсказками
/// Красивая анимация для Aetherion MMO
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string sceneToLoad = "LoginScene"; // Сцена для загрузки
    [SerializeField] private float minimumLoadingTime = 2f; // Минимальное время показа загрузки

    [Header("UI References")]
    [SerializeField] private Slider progressBar; // Прогресс бар
    [SerializeField] private Image progressFill; // Заливка прогресс бара
    [SerializeField] private TextMeshProUGUI progressText; // Текст процентов
    [SerializeField] private TextMeshProUGUI loadingText; // Текст "Загрузка..."
    [SerializeField] private TextMeshProUGUI tipText; // Текст подсказок

    [Header("Loading Tips")]
    [SerializeField] private string[] loadingTips = new string[]
    {
        "Добро пожаловать в мир Aetherion...",
        "Исследуйте мистические земли и находите сокровища...",
        "Объединяйтесь с друзьями для эпических сражений...",
        "Прокачивайте своего героя и открывайте новые способности...",
        "Торгуйте с другими игроками на рынке...",
        "Вступайте в гильдии и завоевывайте территории..."
    };

    [Header("Animation Settings")]
    [SerializeField] private bool animateProgressBar = true;
    [SerializeField] private float progressBarSpeed = 1f;
    [SerializeField] private AnimationCurve progressCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Visual Effects")]
    [SerializeField] private bool useGlowEffect = true;
    [SerializeField] private bool rotateLoadingIcon = false;
    [SerializeField] private Transform loadingIcon; // Иконка загрузки (опционально)
    [SerializeField] private float rotationSpeed = 100f;

    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private bool useFadeIn = false; // Отключаем fade in для мгновенного появления
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private bool useFadeOut = true; // Оставляем fade out
    [SerializeField] private float fadeOutDuration = 0.5f;

    private float targetProgress = 0f;
    private float currentProgress = 0f;
    private bool isLoading = false;

    void Awake()
    {
        // ВАЖНО: Инициализация в Awake для мгновенного появления
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Делаем экран видимым сразу (без fade in)
        if (!useFadeIn)
        {
            canvasGroup.alpha = 1f;
        }
        else
        {
            canvasGroup.alpha = 0f;
        }
    }

    void Start()
    {
        // Проверяем, есть ли сохранённая целевая сцена
        string targetScene = PlayerPrefs.GetString("TargetScene", "");
        if (!string.IsNullOrEmpty(targetScene))
        {
            sceneToLoad = targetScene;
            // Очищаем после использования
            PlayerPrefs.DeleteKey("TargetScene");
            PlayerPrefs.Save();
        }

        // Инициализация UI
        if (progressBar != null)
        {
            progressBar.value = 0f;
        }

        // Показываем случайную подсказку
        if (tipText != null && loadingTips.Length > 0)
        {
            tipText.text = loadingTips[Random.Range(0, loadingTips.Length)];
        }

        // Запускаем музыку загрузки
        StartMusic();

        // Запускаем загрузку
        StartCoroutine(LoadSceneAsync());
    }

    /// <summary>
    /// Запустить музыку загрузки
    /// </summary>
    private void StartMusic()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayLoadingMusic();
        }
    }

    void Update()
    {
        // Анимация прогресс бара
        if (animateProgressBar && isLoading)
        {
            currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * progressBarSpeed);

            if (progressBar != null)
            {
                progressBar.value = currentProgress;
            }

            if (progressText != null)
            {
                progressText.text = $"{Mathf.RoundToInt(currentProgress * 100)}%";
            }
        }

        // Вращение иконки загрузки
        if (rotateLoadingIcon && loadingIcon != null && isLoading)
        {
            loadingIcon.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
        }
    }

    private IEnumerator LoadSceneAsync()
    {
        isLoading = true;

        // Fade In (только если включен)
        if (useFadeIn)
        {
            yield return StartCoroutine(FadeIn());
        }

        float startTime = Time.time;

        // Начинаем асинхронную загрузку сцены
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        asyncLoad.allowSceneActivation = false; // Не переключаемся автоматически

        // Обновляем прогресс
        while (!asyncLoad.isDone)
        {
            // Прогресс от 0 до 0.9 (Unity особенность)
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            targetProgress = progress;

            // Проверяем минимальное время загрузки
            float elapsedTime = Time.time - startTime;
            bool minimumTimeReached = elapsedTime >= minimumLoadingTime;

            // Если загрузка почти завершена и прошло минимальное время
            if (asyncLoad.progress >= 0.9f && minimumTimeReached)
            {
                targetProgress = 1f;

                // Ждем пока анимация прогресс бара дойдет до 100%
                while (currentProgress < 0.99f)
                {
                    yield return null;
                }

                // Fade Out (только если включен)
                if (useFadeOut)
                {
                    yield return StartCoroutine(FadeOut());
                }

                // Переключаемся на следующую сцену
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        isLoading = false;
    }

    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Загрузить конкретную сцену (можно вызвать из другого скрипта)
    /// </summary>
    public void LoadScene(string sceneName)
    {
        sceneToLoad = sceneName;
        StartCoroutine(LoadSceneAsync());
    }
}
