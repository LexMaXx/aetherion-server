using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Проигрывает интро-видео при запуске игры
/// Автоматически переходит к следующей сцене после окончания
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class IntroVideoPlayer : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private VideoClip introVideo; // Твоё видео
    [SerializeField] private bool playOnStart = true;

    [Header("Scene Transition")]
    [SerializeField] private bool useSceneIndex = false; // Использовать индекс вместо имени
    [SerializeField] private int nextSceneIndex = 1; // Индекс следующей сцены (если useSceneIndex = true)
    [SerializeField] private string loadingSceneName = "LoadingScene"; // Сцена загрузки
    [SerializeField] private string targetSceneName = "LoginScene"; // Целевая сцена после загрузки
    [SerializeField] private float delayAfterVideo = 0.5f; // Задержка после видео

    [Header("Skip Settings")]
    [SerializeField] private bool allowSkip = true; // Можно ли пропустить
    [SerializeField] private KeyCode skipKey = KeyCode.Space; // Клавиша для пропуска
    [SerializeField] private bool skipOnClick = true; // Пропуск по клику мыши

    [Header("Fade Settings")]
    [SerializeField] private bool useFadeIn = true;
    [SerializeField] private bool useFadeOut = true;
    [SerializeField] private float fadeDuration = 1f;

    private VideoPlayer videoPlayer;
    private CanvasGroup canvasGroup;
    private bool isPlaying = false;
    private bool hasSkipped = false;

    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();

        // Добавляем CanvasGroup для fade эффекта
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasGroup = canvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (playOnStart)
        {
            PlayIntroVideo();
        }
    }

    void Update()
    {
        if (!isPlaying || hasSkipped) return;

        // Проверка пропуска
        if (allowSkip)
        {
            if (Input.GetKeyDown(skipKey) || (skipOnClick && Input.GetMouseButtonDown(0)))
            {
                SkipVideo();
            }
        }
    }

    public void PlayIntroVideo()
    {
        if (introVideo == null)
        {
            Debug.LogError("Intro Video не назначен! Назначьте видео в Inspector.");
            LoadNextScene();
            return;
        }

        StartCoroutine(PlayVideoCoroutine());
    }

    private IEnumerator PlayVideoCoroutine()
    {
        isPlaying = true;

        // Настройка видеоплеера
        videoPlayer.clip = introVideo;
        videoPlayer.isLooping = false;
        videoPlayer.playOnAwake = false;

        // Пробуем разные режимы рендера
        if (videoPlayer.targetTexture != null)
        {
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        }
        else
        {
            videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        }

        // Подготовка видео
        videoPlayer.Prepare();

        // Ждем пока видео подготовится
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        Debug.Log("Видео подготовлено и готово к воспроизведению");

        // Подписываемся на событие окончания видео
        videoPlayer.loopPointReached += OnVideoFinished;

        // Fade In
        if (useFadeIn && canvasGroup != null)
        {
            yield return StartCoroutine(FadeIn());
        }

        // Начинаем воспроизведение
        videoPlayer.Play();

        // Ждем пока видео играет
        while (videoPlayer.isPlaying && !hasSkipped)
        {
            yield return null;
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        if (!hasSkipped)
        {
            StartCoroutine(FinishAndLoadNextScene());
        }
    }

    private void SkipVideo()
    {
        hasSkipped = true;
        videoPlayer.Stop();
        StartCoroutine(FinishAndLoadNextScene());
    }

    private IEnumerator FinishAndLoadNextScene()
    {
        // Fade Out
        if (useFadeOut && canvasGroup != null)
        {
            yield return StartCoroutine(FadeOut());
        }

        // Небольшая задержка
        if (delayAfterVideo > 0)
        {
            yield return new WaitForSeconds(delayAfterVideo);
        }

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (useSceneIndex)
        {
            // Используем индекс сцены
            int sceneCount = SceneManager.sceneCountInBuildSettings;
            if (nextSceneIndex >= 0 && nextSceneIndex < sceneCount)
            {
                SceneManager.LoadScene(nextSceneIndex);
                Debug.Log($"Загружаем сцену по индексу: {nextSceneIndex}");
            }
            else
            {
                Debug.LogError($"Неверный индекс сцены! Индекс: {nextSceneIndex}, Всего сцен: {sceneCount}");
            }
        }
        else
        {
            // Сохраняем целевую сцену для LoadingScreen
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                PlayerPrefs.SetString("TargetScene", targetSceneName);
                PlayerPrefs.Save();
            }

            // Загружаем сцену загрузки
            if (!string.IsNullOrEmpty(loadingSceneName))
            {
                SceneManager.LoadScene(loadingSceneName);
                Debug.Log($"Загружаем LoadingScene → {targetSceneName}");
            }
            else
            {
                Debug.LogWarning("Loading Scene Name не указан!");
            }
        }
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
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

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}
