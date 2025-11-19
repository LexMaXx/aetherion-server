using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Управление переходами между сценами с fade-эффектом
/// Singleton, сохраняется между сценами (DontDestroyOnLoad)
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Fade Settings")]
    [Tooltip("Длительность fade-out (затемнение)")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Tooltip("Длительность fade-in (появление)")]
    [SerializeField] private float fadeInDuration = 0.5f;

    [Tooltip("Цвет fade-эффекта")]
    [SerializeField] private Color fadeColor = Color.black;

    [Header("Loading Screen")]
    [Tooltip("Показывать экран загрузки?")]
    [SerializeField] private bool showLoadingScreen = true;

    [Tooltip("Минимальное время отображения экрана загрузки")]
    [SerializeField] private float minLoadingTime = 1f;

    // UI элементы
    private Canvas fadeCanvas;
    private Image fadeImage;
    private GameObject loadingPanel;
    private Text loadingText;

    private bool isTransitioning = false;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFadeUI();
            Debug.Log("[SceneTransitionManager] ✅ Инициализирован");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Создание UI для fade-эффекта
    /// </summary>
    private void InitializeFadeUI()
    {
        // Создаём Canvas
        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform);

        fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999; // Поверх всего

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Создаём Image для fade-эффекта
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);

        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

        RectTransform rectTransform = imageObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // Создаём Loading Panel (опционально)
        if (showLoadingScreen)
        {
            loadingPanel = new GameObject("LoadingPanel");
            loadingPanel.transform.SetParent(canvasObj.transform, false);

            loadingText = loadingPanel.AddComponent<Text>();
            loadingText.text = "Загрузка...";
            loadingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            loadingText.fontSize = 36;
            loadingText.alignment = TextAnchor.MiddleCenter;
            loadingText.color = Color.white;

            RectTransform loadingRect = loadingPanel.GetComponent<RectTransform>();
            loadingRect.anchorMin = new Vector2(0.5f, 0.3f);
            loadingRect.anchorMax = new Vector2(0.5f, 0.3f);
            loadingRect.sizeDelta = new Vector2(400, 100);

            loadingPanel.SetActive(false);
        }

        // Изначально canvas выключен
        fadeCanvas.enabled = false;

        Debug.Log("[SceneTransitionManager] Fade UI создан");
    }

    /// <summary>
    /// Загрузить сцену с fade-эффектом
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (isTransitioning)
        {
            Debug.LogWarning($"[SceneTransitionManager] Уже выполняется переход, игнорируем запрос на '{sceneName}'");
            return;
        }

        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    /// <summary>
    /// Корутина загрузки сцены
    /// </summary>
    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        isTransitioning = true;
        fadeCanvas.enabled = true;

        Debug.Log($"[SceneTransitionManager] Переход в сцену: {sceneName}");

        // Fade Out (затемнение)
        yield return StartCoroutine(FadeOut());

        // Показываем loading screen
        if (showLoadingScreen && loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        float loadStartTime = Time.realtimeSinceStartup;

        // Загружаем сцену асинхронно
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Ждём пока сцена загрузится
        while (asyncLoad.progress < 0.9f)
        {
            if (loadingText != null)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                loadingText.text = $"Загрузка... {(int)(progress * 100)}%";
            }

            yield return null;
        }

        // Минимальное время загрузки (чтобы экран не мелькал)
        float loadTime = Time.realtimeSinceStartup - loadStartTime;
        if (loadTime < minLoadingTime)
        {
            yield return new WaitForSecondsRealtime(minLoadingTime - loadTime);
        }

        // Активируем сцену
        asyncLoad.allowSceneActivation = true;

        yield return null;

        // Скрываем loading screen
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        // Fade In (появление)
        yield return StartCoroutine(FadeIn());

        fadeCanvas.enabled = false;
        isTransitioning = false;

        Debug.Log($"[SceneTransitionManager] ✅ Переход завершён: {sceneName}");
    }

    /// <summary>
    /// Fade Out (затемнение)
    /// </summary>
    private IEnumerator FadeOut()
    {
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeOutDuration);
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
    }

    /// <summary>
    /// Fade In (появление)
    /// </summary>
    private IEnumerator FadeIn()
    {
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / fadeInDuration);
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
    }

    /// <summary>
    /// Перезагрузить текущую сцену
    /// </summary>
    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Загрузить сцену напрямую без эффектов (для отладки)
    /// </summary>
    public void LoadSceneImmediate(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
