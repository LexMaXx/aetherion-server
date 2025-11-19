using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using MsaaQuality = UnityEngine.Rendering.Universal.MsaaQuality;

/// <summary>
/// Менеджер настроек графики
/// Управление качеством графики, разрешением, FPS, тенями и другими параметрами
/// </summary>
public class GraphicsSettingsManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private CanvasGroup settingsCanvasGroup;
    [SerializeField] private ScrollRect settingsScrollRect;
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button applyButton;

    [Header("Quality Settings")]
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vsyncToggle;

    [Header("Graphics Settings")]
    [SerializeField] private TMP_Dropdown shadowQualityDropdown;
    [SerializeField] private TMP_Dropdown antiAliasingDropdown;
    [SerializeField] private Slider renderScaleSlider;
    [SerializeField] private TMP_Text renderScaleText;
    [SerializeField] private TMP_Dropdown vegetationQualityDropdown;
    [Header("View Distance Settings")]
    [SerializeField] private Slider entityViewDistanceSlider;
    [SerializeField] private TMP_Text entityViewDistanceText;
    [SerializeField] private Slider buildingViewDistanceSlider;
    [SerializeField] private TMP_Text buildingViewDistanceText;
    [SerializeField] private Slider vegetationDrawDistanceSlider;
    [SerializeField] private TMP_Text vegetationDrawDistanceText;

    [Header("Performance Settings")]
    [SerializeField] private TMP_Dropdown targetFpsDropdown;
    [SerializeField] private Toggle showFpsToggle;
    [SerializeField] private TMP_Text fpsCounterText;

    [Header("Advanced Optimization (Mobile)")]
    [SerializeField] private Toggle aggressiveOptimizationToggle;
    [SerializeField] private TMP_Dropdown optimizationPresetDropdown;

    [Header("Layout Tweaks")]
    [SerializeField] private float minLabelWidth = 260f;
    [SerializeField] private float minButtonWidth = 220f;

    // Singleton
    public static GraphicsSettingsManager Instance { get; private set; }

    // Settings state
    private Resolution[] resolutions;
    private int currentResolutionIndex;
    private bool isDirty = false; // Есть ли несохранённые изменения
    private float entityViewDistance = 250f;
    private float buildingViewDistance = 350f;
    private float vegetationDistanceMultiplier = 1f;
    private int currentVegetationQualityIndex = 2;
    private bool cullDistancesDirty = true;
    private readonly HashSet<int> configuredCameraIds = new HashSet<int>();

    // FPS Counter
    private float deltaTime = 0.0f;
    private bool showFps = false;

    // Dynamic Performance Scaling
    [Header("Dynamic Performance")]
    [SerializeField] private Toggle dynamicPerformanceToggle;
    private bool autoScaleEnabled = false;
    private Coroutine autoScaleCoroutine;
    private float originalScrollSensitivity = 10f;
    private int scrollLockCounter = 0;

    // Auto-scaling parameters
    private float minFps = 35f;           // Порог для уменьшения качества
    private float maxFps = 55f;           // Порог для увеличения качества
    private float minScale = 0.6f;        // Минимальный render scale
    private float scaleStep = 0.05f;      // Шаг изменения render scale
    private float checkInterval = 2f;     // Интервал проверки FPS (секунды)
    private float stabilizationDelay = 5f; // Задержка между изменениями (hysteresis)
    private float lastScaleChangeTime = 0f;

    // FPS tracking для average
    private Queue<float> fpsHistory = new Queue<float>();
    private const int fpsHistorySize = 30; // ~0.5 сек при 60 FPS

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnEnable()
    {
        Camera.onPreCull += HandleCameraPreCull;
    }

    void OnDisable()
    {
        Camera.onPreCull -= HandleCameraPreCull;
        configuredCameraIds.Clear();
    }

    void Start()
    {
        // Добавляем CanvasGroup если его нет
        if (settingsCanvasGroup == null && settingsPanel != null)
        {
            settingsCanvasGroup = settingsPanel.GetComponent<CanvasGroup>();
            if (settingsCanvasGroup == null)
            {
                settingsCanvasGroup = settingsPanel.AddComponent<CanvasGroup>();
                Debug.Log("[GraphicsSettings] ✅ CanvasGroup добавлен к SettingsPanel");
            }
        }

        if (settingsScrollRect == null && settingsPanel != null)
        {
            settingsScrollRect = settingsPanel.GetComponentInParent<ScrollRect>();
        }

        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystem = es.GetComponent<EventSystem>();
            }
        }

        // Проверяем и настраиваем Canvas Scaler для корректного UI на всех разрешениях
        EnsureCanvasScalerConfigured();

        // На мобильных устройствах проверяем настройки камер
        if (Application.isMobilePlatform)
        {
            EnsureCamerasConfiguredForMobile();
        }

        // Инициализация UI
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseSettings);
        }

        if (applyButton != null)
        {
            applyButton.onClick.AddListener(ApplySettings);
        }

        // Инициализация настроек
        InitializeQualitySettings();
        InitializeResolutionSettings();
        InitializeGraphicsSettings();
        InitializePerformanceSettings();
        InitializeDynamicPerformance();
        InitializeVegetationSettings();
        InitializeViewDistanceSettings();

        // Настраиваем управление скроллом для dropdown
        SetupDropdownScrollControl();

        // Загружаем сохранённые настройки
        LoadSettings();
        FixUILayout();
        InitializeAdvancedOptimization();
        ApplyPlatformDefaults();

        // Скрываем меню по умолчанию
        CloseSettings();

        Debug.Log("[GraphicsSettings] ✅ Инициализирован");
    }

    private void InitializeVegetationSettings()
    {
        if (vegetationQualityDropdown == null)
        {
            return;
        }

        vegetationQualityDropdown.ClearOptions();
        vegetationQualityDropdown.AddOptions(new List<string> { "Низкое", "Среднее", "Высокое", "Ультра" });
        vegetationQualityDropdown.onValueChanged.AddListener(OnVegetationQualityChanged);

        int savedIndex = PlayerPrefs.GetInt("VegetationQuality", 2);
        vegetationQualityDropdown.value = Mathf.Clamp(savedIndex, 0, vegetationQualityDropdown.options.Count - 1);
        currentVegetationQualityIndex = vegetationQualityDropdown.value;
        ApplyVegetationQuality(currentVegetationQualityIndex);
    }

    private void InitializeViewDistanceSettings()
    {
        ConfigureDistanceSlider(entityViewDistanceSlider, 60f, 600f, OnEntityViewDistanceChanged);
        ConfigureDistanceSlider(buildingViewDistanceSlider, 80f, 900f, OnBuildingViewDistanceChanged);
        ConfigureDistanceSlider(vegetationDrawDistanceSlider, 0.5f, 2f, OnVegetationDrawDistanceChanged);
    }

    private void ConfigureDistanceSlider(Slider slider, float min, float max, UnityAction<float> callback)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = false;
        slider.onValueChanged.AddListener(callback);
    }

    void Update()
    {
        // Обновление FPS счётчика и истории
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float currentFps = 1.0f / deltaTime;

        // Отображаем FPS если включено
        if (showFps && fpsCounterText != null)
        {
            fpsCounterText.text = $"FPS: {Mathf.Ceil(currentFps)}";
        }

        // Сохраняем историю FPS для динамического масштабирования
        if (autoScaleEnabled)
        {
            fpsHistory.Enqueue(currentFps);
            if (fpsHistory.Count > fpsHistorySize)
            {
                fpsHistory.Dequeue();
            }
        }

        // Открытие/закрытие меню настроек по клавише F1
        if (Input.GetKeyDown(KeyCode.F1))
        {
            bool isMenuOpen = settingsCanvasGroup != null ? settingsCanvasGroup.alpha > 0.5f : (settingsPanel != null && settingsPanel.activeSelf);
            if (isMenuOpen)
            {
                CloseSettings();
            }
            else
            {
                OpenSettings();
            }
        }
    }

    #region Initialization

    private void InitializeQualitySettings()
    {
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            List<string> options = new List<string>(QualitySettings.names);
            qualityDropdown.AddOptions(options);
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
            vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        }
    }

    private void InitializeResolutionSettings()
    {
        if (resolutionDropdown != null)
        {
            // На Android берём все разрешения, на PC фильтруем по refresh rate
            if (Application.isMobilePlatform)
            {
                // Для мобильных устройств: берём все уникальные разрешения, отсортированные по качеству
                resolutions = Screen.resolutions
                    .GroupBy(r => new { r.width, r.height })
                    .Select(g => g.OrderByDescending(r => r.refreshRate).First())
                    .OrderBy(r => r.width * r.height)
                    .ToArray();
            }
            else
            {
                // Для ПК: берём разрешения с текущей частотой обновления
                resolutions = Screen.resolutions
                    .Where(r => r.refreshRate == Screen.currentResolution.refreshRate)
                    .ToArray();
            }

            resolutionDropdown.ClearOptions();

            List<string> options = new List<string>();
            currentResolutionIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + " x " + resolutions[i].height;

                // На мобильных показываем соотношение сторон для удобства
                if (Application.isMobilePlatform)
                {
                    float aspectRatio = (float)resolutions[i].width / resolutions[i].height;
                    string aspectStr = GetAspectRatioString(aspectRatio);
                    option += $" ({aspectStr})";
                }

                options.Add(option);

                if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }
    }

    /// <summary>
    /// Получить читаемую строку соотношения сторон
    /// </summary>
    private string GetAspectRatioString(float aspectRatio)
    {
        if (Mathf.Abs(aspectRatio - 16f / 9f) < 0.01f) return "16:9";
        if (Mathf.Abs(aspectRatio - 18f / 9f) < 0.01f) return "18:9";
        if (Mathf.Abs(aspectRatio - 19f / 9f) < 0.01f) return "19:9";
        if (Mathf.Abs(aspectRatio - 19.5f / 9f) < 0.01f) return "19.5:9";
        if (Mathf.Abs(aspectRatio - 20f / 9f) < 0.01f) return "20:9";
        if (Mathf.Abs(aspectRatio - 21f / 9f) < 0.01f) return "21:9";
        if (Mathf.Abs(aspectRatio - 4f / 3f) < 0.01f) return "4:3";
        if (Mathf.Abs(aspectRatio - 3f / 2f) < 0.01f) return "3:2";
        return $"{aspectRatio:F2}:1";
    }

    private void InitializeGraphicsSettings()
    {
        // Shadow Quality
        if (shadowQualityDropdown != null)
        {
            shadowQualityDropdown.ClearOptions();
            var shadowOptions = new List<string> { "Отключены", "Только жёсткие", "Все тени" };
            shadowQualityDropdown.AddOptions(shadowOptions);
            int currentShadowIndex = Mathf.Clamp((int)QualitySettings.shadows, 0, shadowOptions.Count - 1);
            shadowQualityDropdown.value = currentShadowIndex;
            shadowQualityDropdown.onValueChanged.AddListener(OnShadowQualityChanged);
        }

        // Anti-Aliasing
        if (antiAliasingDropdown != null)
        {
            antiAliasingDropdown.ClearOptions();
            antiAliasingDropdown.AddOptions(new List<string> { "Отключен", "2x MSAA", "4x MSAA", "8x MSAA" });
            antiAliasingDropdown.value = GetAntiAliasingIndex();
            antiAliasingDropdown.onValueChanged.AddListener(OnAntiAliasingChanged);
        }

        // Render Scale
        if (renderScaleSlider != null)
        {
            renderScaleSlider.minValue = 0.5f;
            renderScaleSlider.maxValue = 2.0f;
            renderScaleSlider.value = GetRenderScale();
            renderScaleSlider.onValueChanged.AddListener(OnRenderScaleChanged);
            UpdateRenderScaleText(renderScaleSlider.value);
        }
    }

    private void InitializePerformanceSettings()
    {
        // Target FPS
        if (targetFpsDropdown != null)
        {
            targetFpsDropdown.ClearOptions();
            targetFpsDropdown.AddOptions(new List<string> { "30 FPS", "60 FPS", "120 FPS", "Без ограничений" });
            targetFpsDropdown.value = GetTargetFpsIndex();
            targetFpsDropdown.onValueChanged.AddListener(OnTargetFpsChanged);
        }

        // Show FPS Counter
        if (showFpsToggle != null)
        {
            showFpsToggle.isOn = false;
            showFpsToggle.onValueChanged.AddListener(OnShowFpsChanged);
        }

        if (fpsCounterText != null)
        {
            fpsCounterText.gameObject.SetActive(false);
        }
    }

    private void InitializeDynamicPerformance()
    {
        InitializeDynamicPerformanceToggleState();
    }

    /// <summary>
    /// Подключить управление скроллом ко всем dropdown для корректной работы на touch-устройствах
    /// </summary>
    private void SetupDropdownScrollControl()
    {
        RegisterDropdown(qualityDropdown);
        RegisterDropdown(resolutionDropdown);
        RegisterDropdown(shadowQualityDropdown);
        RegisterDropdown(antiAliasingDropdown);
        RegisterDropdown(vegetationQualityDropdown);
        RegisterDropdown(targetFpsDropdown);
    }

    private void RegisterDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return;

        dropdown.onValueChanged.AddListener(_ =>
        {
            UnlockScroll();
        });

        EventTrigger trigger = dropdown.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = dropdown.gameObject.AddComponent<EventTrigger>();
        }

        AddEventTriggerEntry(trigger, EventTriggerType.PointerDown, _ => LockScroll());
        AddEventTriggerEntry(trigger, EventTriggerType.BeginDrag, _ => LockScroll());
        AddEventTriggerEntry(trigger, EventTriggerType.PointerUp, _ => UnlockScrollDelayed());
        AddEventTriggerEntry(trigger, EventTriggerType.EndDrag, _ => UnlockScrollDelayed());
        AddEventTriggerEntry(trigger, EventTriggerType.Cancel, _ => UnlockScrollDelayed());
    }

    private void LockScroll()
    {
        if (settingsScrollRect == null) return;

        if (scrollLockCounter == 0)
        {
            settingsScrollRect.StopMovement();
            originalScrollSensitivity = settingsScrollRect.scrollSensitivity;
            settingsScrollRect.scrollSensitivity = 0f;
            settingsScrollRect.enabled = false;
        }

        scrollLockCounter++;
    }

    private void UnlockScroll()
    {
        if (settingsScrollRect == null) return;

        scrollLockCounter = Mathf.Max(0, scrollLockCounter - 1);
        if (scrollLockCounter == 0)
        {
            settingsScrollRect.enabled = true;
            settingsScrollRect.vertical = true;
            settingsScrollRect.scrollSensitivity = originalScrollSensitivity;
        }
    }

    private void UnlockScrollDelayed()
    {
        CancelInvoke(nameof(UnlockScroll));
        Invoke(nameof(UnlockScroll), 0.05f);
    }

    private void AddEventTriggerEntry(EventTrigger trigger, EventTriggerType type, UnityAction<BaseEventData> callback)
    {
        if (trigger == null) return;
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    private void InitializeDynamicPerformanceToggleState()
    {
        if (dynamicPerformanceToggle != null)
        {
            bool saved = PlayerPrefs.GetInt("DynamicPerformance", 1) == 1;
            dynamicPerformanceToggle.isOn = saved;
            dynamicPerformanceToggle.onValueChanged.AddListener(OnDynamicPerformanceChanged);

            if (saved)
            {
                EnableDynamicPerformance();
            }
            else
            {
                DisableDynamicPerformance();
            }
        }
        else
        {
            // Нет UI-элемента – включаем авто-масштабирование всегда
            EnableDynamicPerformance();
            PlayerPrefs.SetInt("DynamicPerformance", 1);
        }
    }

    private void InitializeAdvancedOptimization()
    {
        // Только для мобильных устройств
        if (!Application.isMobilePlatform)
        {
            if (aggressiveOptimizationToggle != null)
                aggressiveOptimizationToggle.gameObject.SetActive(false);
            if (optimizationPresetDropdown != null)
                optimizationPresetDropdown.gameObject.SetActive(false);
            return;
        }

        // Проверяем наличие PerformanceOptimizer
        PerformanceOptimizer optimizer = FindFirstObjectByType<PerformanceOptimizer>();
        if (optimizer == null)
        {
            // Создаём автоматически если его нет
            GameObject optimizerObj = new GameObject("PerformanceOptimizer");
            optimizer = optimizerObj.AddComponent<PerformanceOptimizer>();
            Debug.Log("[GraphicsSettings] ✅ PerformanceOptimizer создан автоматически");
        }

        // Aggressive Optimization Toggle
        if (aggressiveOptimizationToggle != null)
        {
            bool enabled = PlayerPrefs.GetInt("AggressiveOptimization", 1) == 1;
            aggressiveOptimizationToggle.isOn = enabled;
            aggressiveOptimizationToggle.onValueChanged.AddListener(OnAggressiveOptimizationChanged);
        }

        // Optimization Preset Dropdown
        if (optimizationPresetDropdown != null)
        {
            optimizationPresetDropdown.ClearOptions();
            optimizationPresetDropdown.AddOptions(new List<string>
            {
                "Сбалансированный (60 FPS)",
                "Производительность (90 FPS)",
                "Ультра производительность (120 FPS)"
            });

            int savedPreset = PlayerPrefs.GetInt("OptimizationPreset", 0);
            optimizationPresetDropdown.value = savedPreset;
            optimizationPresetDropdown.onValueChanged.AddListener(OnOptimizationPresetChanged);
        }

        Debug.Log("[GraphicsSettings] ✅ Advanced Optimization инициализирован");
    }

    private void OnAggressiveOptimizationChanged(bool enabled)
    {
        PerformanceOptimizer optimizer = FindFirstObjectByType<PerformanceOptimizer>();
        if (optimizer != null)
        {
            optimizer.SetOptimizationEnabled(enabled);
        }

        PlayerPrefs.SetInt("AggressiveOptimization", enabled ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"[GraphicsSettings] Агрессивная оптимизация: {enabled}");
    }

    private void OnOptimizationPresetChanged(int index)
    {
        PerformanceOptimizer optimizer = FindFirstObjectByType<PerformanceOptimizer>();
        if (optimizer != null)
        {
            PerformanceOptimizer.OptimizationPreset preset = (PerformanceOptimizer.OptimizationPreset)index;
            optimizer.SetOptimizationPreset(preset);
        }

        PlayerPrefs.SetInt("OptimizationPreset", index);
        PlayerPrefs.Save();

        string[] presetNames = { "Сбалансированный", "Производительность", "Ультра производительность" };
        Debug.Log($"[GraphicsSettings] Пресет оптимизации: {presetNames[index]}");
    }

    #endregion

    #region Settings Callbacks

    private void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index);
        isDirty = true;
        Debug.Log($"[GraphicsSettings] Качество изменено: {QualitySettings.names[index]}");
    }

    private void OnResolutionChanged(int index)
    {
        currentResolutionIndex = index;
        isDirty = true;
        Debug.Log($"[GraphicsSettings] Разрешение изменено: {resolutions[index].width}x{resolutions[index].height}");
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        isDirty = true;
        Debug.Log($"[GraphicsSettings] Полноэкранный режим: {isFullscreen}");
    }

    private void OnVSyncChanged(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        isDirty = true;
        Debug.Log($"[GraphicsSettings] VSync: {enabled}");
    }

    private void OnShadowQualityChanged(int index)
    {
        int clampedIndex = Mathf.Clamp(index, 0, System.Enum.GetValues(typeof(UnityEngine.ShadowQuality)).Length - 1);
        QualitySettings.shadows = (UnityEngine.ShadowQuality)clampedIndex;

        if (shadowQualityDropdown != null && shadowQualityDropdown.value != clampedIndex)
        {
            shadowQualityDropdown.SetValueWithoutNotify(clampedIndex);
        }

        isDirty = true;
        Debug.Log($"[GraphicsSettings] Качество теней: {(UnityEngine.ShadowQuality)clampedIndex}");
    }

    private void OnAntiAliasingChanged(int index)
    {
        int msaaValue = index == 0 ? 0 : (int)Mathf.Pow(2, index);
        SetAntiAliasing(msaaValue);
        isDirty = true;
        Debug.Log($"[GraphicsSettings] Anti-Aliasing: {msaaValue}x MSAA");
    }

    private void OnRenderScaleChanged(float value)
    {
        SetRenderScale(value);
        UpdateRenderScaleText(value);
        isDirty = true;
    }

    private void OnTargetFpsChanged(int index)
    {
        int targetFps = GetTargetFpsFromIndex(index);
        Application.targetFrameRate = targetFps;
        isDirty = true;
        Debug.Log($"[GraphicsSettings] Target FPS: {(targetFps == -1 ? "Без ограничений" : targetFps.ToString())}");
    }

    private void OnShowFpsChanged(bool show)
    {
        showFps = show;
        if (fpsCounterText != null)
        {
            fpsCounterText.gameObject.SetActive(show);
        }
        isDirty = true;
        Debug.Log($"[GraphicsSettings] FPS Counter: {show}");
    }

    private void OnVegetationQualityChanged(int index)
    {
        ApplyVegetationQuality(index);
        PlayerPrefs.SetInt("VegetationQuality", index);
        PlayerPrefs.Save();
    }

    private void OnDynamicPerformanceChanged(bool enabled)
    {
        if (enabled)
        {
            EnableDynamicPerformance();
        }
        else
        {
            DisableDynamicPerformance();
        }

        PlayerPrefs.SetInt("DynamicPerformance", enabled ? 1 : 0);
        PlayerPrefs.Save();
        isDirty = true;
        Debug.Log($"[GraphicsSettings] Dynamic Performance: {enabled}");
    }

    private void OnEntityViewDistanceChanged(float value)
    {
        ApplyEntityViewDistance(value);
        PlayerPrefs.SetFloat("EntityViewDistance", entityViewDistance);
        PlayerPrefs.Save();
        isDirty = true;
    }

    private void OnBuildingViewDistanceChanged(float value)
    {
        ApplyBuildingViewDistance(value);
        PlayerPrefs.SetFloat("BuildingViewDistance", buildingViewDistance);
        PlayerPrefs.Save();
        isDirty = true;
    }

    private void OnVegetationDrawDistanceChanged(float value)
    {
        ApplyVegetationDistanceMultiplier(value);
        PlayerPrefs.SetFloat("VegetationDistanceMultiplier", vegetationDistanceMultiplier);
        PlayerPrefs.Save();
        isDirty = true;
    }

    #endregion

    #region Dynamic Performance Scaling

    private void EnableDynamicPerformance()
    {
        autoScaleEnabled = true;
        fpsHistory.Clear();
        lastScaleChangeTime = Time.time;

        if (autoScaleCoroutine != null)
        {
            StopCoroutine(autoScaleCoroutine);
        }

        autoScaleCoroutine = StartCoroutine(AutoScaleRoutine());
        Debug.Log("[GraphicsSettings] 🎯 Dynamic Performance включен");
    }

    private void DisableDynamicPerformance()
    {
        autoScaleEnabled = false;

        if (autoScaleCoroutine != null)
        {
            StopCoroutine(autoScaleCoroutine);
            autoScaleCoroutine = null;
        }

        fpsHistory.Clear();
        Debug.Log("[GraphicsSettings] 🎯 Dynamic Performance выключен");
    }

    private System.Collections.IEnumerator AutoScaleRoutine()
    {
        while (autoScaleEnabled)
        {
            yield return new WaitForSeconds(checkInterval);

            float avgFps = GetAverageFPS();
            float currentScale = GetRenderScale();
            float timeSinceLastChange = Time.time - lastScaleChangeTime;

            // Hysteresis: не меняем чаще чем раз в 5 секунд
            if (timeSinceLastChange < stabilizationDelay)
            {
                continue;
            }

            // FPS низкий - уменьшаем качество
            if (avgFps < minFps && currentScale > minScale)
            {
                float newScale = Mathf.Max(minScale, currentScale - scaleStep);
                SetRenderScale(newScale);
                SyncRenderScaleUI(newScale);
                AdjustQualityForPerformance(newScale);
                lastScaleChangeTime = Time.time;

                Debug.Log($"[DynamicPerf] ⬇️ FPS low ({avgFps:F1}) - Снижаем качество до {newScale:F2}");
            }
            // FPS стабильно высокий - повышаем качество
            else if (avgFps > maxFps && currentScale < 1f)
            {
                float newScale = Mathf.Min(1f, currentScale + scaleStep);
                SetRenderScale(newScale);
                SyncRenderScaleUI(newScale);
                AdjustQualityForPerformance(newScale);
                lastScaleChangeTime = Time.time;

                Debug.Log($"[DynamicPerf] ⬆️ FPS high ({avgFps:F1}) - Повышаем качество до {newScale:F2}");
            }
        }
    }

    private float GetAverageFPS()
    {
        if (fpsHistory.Count == 0)
        {
            return 60f; // Значение по умолчанию
        }

        float sum = 0f;
        foreach (float fps in fpsHistory)
        {
            sum += fps;
        }

        return sum / fpsHistory.Count;
    }

    private void AdjustQualityForPerformance(float renderScale)
    {
        // Динамически корректируем дистанцию теней в зависимости от render scale
        if (renderScale < 0.75f)
        {
            // Низкое качество - уменьшаем дистанцию теней
            QualitySettings.shadowDistance = 30f;
        }
        else if (renderScale < 0.9f)
        {
            // Среднее качество
            QualitySettings.shadowDistance = 50f;
        }
        else
        {
            // Высокое качество
            QualitySettings.shadowDistance = 100f;
        }

        // Можно также динамически менять качество теней
        if (renderScale < 0.7f)
        {
            QualitySettings.shadows = UnityEngine.ShadowQuality.HardOnly;
        }
        else if (renderScale < 0.85f)
        {
            QualitySettings.shadows = UnityEngine.ShadowQuality.All;
        }

        Debug.Log($"[DynamicPerf] 🔧 Adjusted shadowDistance: {QualitySettings.shadowDistance}m, shadows: {QualitySettings.shadows}");
    }

    void OnDestroy()
    {
        // Останавливаем coroutine при уничтожении
        if (autoScaleCoroutine != null)
        {
            StopCoroutine(autoScaleCoroutine);
        }
    }

    #endregion

    #region Helper Methods

    private void FixUILayout()
    {
        if (settingsPanel == null)
        {
            return;
        }

        var panelRect = settingsPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        }

        TMP_Text titleText = null;

        foreach (TMP_Text text in settingsPanel.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null) continue;

            if (titleText == null && text.text.ToUpper().Contains("НАСТРОЙКИ"))
            {
                titleText = text;
            }

            text.enableWordWrapping = false;
            text.alignment = text == titleText ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;

            RectTransform rect = text.rectTransform;
            if (text != titleText && rect != null)
            {
                if (!rect.TryGetComponent(out LayoutElement layoutElement))
                {
                    layoutElement = rect.gameObject.AddComponent<LayoutElement>();
                }

                layoutElement.minWidth = Mathf.Max(layoutElement.minWidth, minLabelWidth);
            }
        }

        NormalizeButtonSize(applyButton);
        NormalizeButtonSize(closeButton);
    }

    private void NormalizeButtonSize(Button button)
    {
        if (button == null) return;

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(rect.rect.width, minButtonWidth));
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(rect.rect.height, 60f));
        }

        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Center;
        }
    }

    private void ApplyVegetationQuality(int index)
    {
        currentVegetationQualityIndex = Mathf.Clamp(index, 0, 3);

        float factor = Mathf.Clamp01(currentVegetationQualityIndex / 3f);
        float distanceMultiplier = Mathf.Max(0.25f, vegetationDistanceMultiplier);

        float detailDistance = Mathf.Lerp(25f, 130f, factor) * distanceMultiplier;
        float detailDensity = Mathf.Lerp(0.25f, 1f, factor);
        float treeDistance = Mathf.Lerp(80f, 650f, factor) * distanceMultiplier;

        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain == null) continue;

            terrain.detailObjectDistance = detailDistance;
            terrain.detailObjectDensity = detailDensity;
            terrain.treeDistance = treeDistance;
            terrain.treeBillboardDistance = treeDistance * 0.7f;
            terrain.treeCrossFadeLength = 20f;
        }

        Debug.Log($"[GraphicsSettings] 🌿 Vegetation quality {currentVegetationQualityIndex} (x{distanceMultiplier:F1}) -> detail {detailDistance:F0}m, trees {treeDistance:F0}m");
    }

    private void ApplyEntityViewDistance(float value, bool updateSlider = true)
    {
        entityViewDistance = ClampDistanceValue(entityViewDistanceSlider, value);

        if (updateSlider && entityViewDistanceSlider != null)
        {
            entityViewDistanceSlider.SetValueWithoutNotify(entityViewDistance);
        }

        UpdateDistanceLabel(entityViewDistanceText, entityViewDistance, "м");
        MarkCullDistancesDirty();
        ApplyLayerCullDistances();
    }

    private void ApplyBuildingViewDistance(float value, bool updateSlider = true)
    {
        buildingViewDistance = ClampDistanceValue(buildingViewDistanceSlider, value);

        if (updateSlider && buildingViewDistanceSlider != null)
        {
            buildingViewDistanceSlider.SetValueWithoutNotify(buildingViewDistance);
        }

        UpdateDistanceLabel(buildingViewDistanceText, buildingViewDistance, "м");
        MarkCullDistancesDirty();
        ApplyLayerCullDistances();
    }

    private void ApplyVegetationDistanceMultiplier(float value, bool updateSlider = true)
    {
        vegetationDistanceMultiplier = ClampDistanceValue(vegetationDrawDistanceSlider, value);

        if (updateSlider && vegetationDrawDistanceSlider != null)
        {
            vegetationDrawDistanceSlider.SetValueWithoutNotify(vegetationDistanceMultiplier);
        }

        UpdateDistanceLabel(vegetationDrawDistanceText, vegetationDistanceMultiplier, "x", true);
        ApplyVegetationQuality(currentVegetationQualityIndex);
    }

    private float ClampDistanceValue(Slider slider, float value)
    {
        if (slider == null)
        {
            return value;
        }

        return Mathf.Clamp(value, slider.minValue, slider.maxValue);
    }

    private void UpdateDistanceLabel(TMP_Text label, float value, string suffix, bool showDecimal = false)
    {
        if (label == null)
        {
            return;
        }

        label.text = showDecimal ? $"{value:F1}{suffix}" : $"{Mathf.RoundToInt(value)}{suffix}";
    }

    private void ApplyLayerCullDistances()
    {
        foreach (Camera cam in Camera.allCameras)
        {
            ApplyLayerCullDistances(cam);
        }
        cullDistancesDirty = false;
    }

    private void ApplyLayerCullDistances(Camera targetCamera)
    {
        if (targetCamera == null)
        {
            return;
        }

        int cameraId = targetCamera.GetInstanceID();
        bool alreadyConfigured = configuredCameraIds.Contains(cameraId);
        if (!cullDistancesDirty && alreadyConfigured)
        {
            return;
        }

        float[] distances = targetCamera.layerCullDistances;
        if (distances == null || distances.Length != 32)
        {
            distances = new float[32];
        }

        SetLayerCullDistance(distances, "Character", entityViewDistance);
        SetLayerCullDistance(distances, "Enemy", entityViewDistance);
        SetLayerCullDistance(distances, "Building", buildingViewDistance);
        SetLayerCullDistance(distances, "Cave", buildingViewDistance);

        targetCamera.layerCullDistances = distances;

        // layerCullSpherical работает только с Built-in Renderer, не с URP/HDRP
        #if !UNITY_PIPELINE_URP && !UNITY_PIPELINE_HDRP
        targetCamera.layerCullSpherical = true;
        #endif

        configuredCameraIds.Add(cameraId);
    }

    private void SetLayerCullDistance(float[] distances, string layerName, float value)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0 || layer >= distances.Length)
        {
            return;
        }

        distances[layer] = Mathf.Max(0f, value);
    }

    private void MarkCullDistancesDirty()
    {
        cullDistancesDirty = true;
        configuredCameraIds.Clear();
    }

    private void HandleCameraPreCull(Camera cam)
    {
        ApplyLayerCullDistances(cam);
    }

    private void ApplyResolutionByIndex(int index)
    {
        if (resolutions == null || resolutions.Length == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(index, 0, resolutions.Length - 1);
        currentResolutionIndex = clampedIndex;

        Resolution resolution = resolutions[clampedIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);

        if (resolutionDropdown != null)
        {
            resolutionDropdown.SetValueWithoutNotify(clampedIndex);
            resolutionDropdown.RefreshShownValue();
        }
    }

    private int FindResolutionIndex(int width, int height)
    {
        if (resolutions == null || resolutions.Length == 0)
        {
            return -1;
        }

        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == width && resolutions[i].height == height)
            {
                return i;
            }
        }

        return -1;
    }

    private void SyncRenderScaleUI(float value)
    {
        float displayedValue = value;

        if (renderScaleSlider != null)
        {
            displayedValue = Mathf.Clamp(value, renderScaleSlider.minValue, renderScaleSlider.maxValue);
            renderScaleSlider.SetValueWithoutNotify(displayedValue);
        }

        UpdateRenderScaleText(displayedValue);
    }

    /// <summary>
    /// Включить/выключить прокрутку ScrollRect (для корректной работы dropdown)
    /// </summary>
    public void SetScrollEnabled(bool enabled)
    {
        if (settingsScrollRect != null)
        {
            settingsScrollRect.enabled = enabled;
            settingsScrollRect.vertical = enabled;
        }
    }

    private void ApplyPlatformDefaults()
    {
        if (!Application.isMobilePlatform)
        {
            return;
        }

        // КРИТИЧНО: исправляем разрешение для Android устройств
        // Это предотвращает растяжение персонажа из-за неправильного aspect ratio
        EnsureCorrectResolutionForMobile();

        // Lower render scale for mobile if user не задавал
        float currentScale = GetRenderScale();
        if (currentScale > 0.9f)
        {
            SetRenderScale(0.85f);
            SyncRenderScaleUI(0.85f);
        }

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        int mobileVegIndex = Mathf.Min(PlayerPrefs.GetInt("VegetationQuality", 1), 2);
        ApplyVegetationQuality(mobileVegIndex);
    }

    /// <summary>
    /// Проверяем что камеры правильно настроены для мобильных устройств
    /// </summary>
    private void EnsureCamerasConfiguredForMobile()
    {
        Camera[] allCameras = Camera.allCameras;
        if (allCameras == null || allCameras.Length == 0)
        {
            Debug.LogWarning("[GraphicsSettings] ⚠️ Камеры не найдены в сцене!");
            return;
        }

        float screenAspect = (float)Screen.width / Screen.height;
        Debug.Log($"[GraphicsSettings] 📱 Настройка {allCameras.Length} камер для aspect ratio {screenAspect:F3}");

        foreach (Camera cam in allCameras)
        {
            if (cam == null) continue;

            // Проверяем, что камера использует правильный aspect ratio
            float cameraAspect = cam.aspect;
            float aspectDiff = Mathf.Abs(cameraAspect - screenAspect);

            if (aspectDiff > 0.01f)
            {
                Debug.Log($"[GraphicsSettings] ⚙️ Камера '{cam.name}': исправляем aspect ratio с {cameraAspect:F3} на {screenAspect:F3}");
                cam.ResetAspect(); // Сбрасываем на автоматический расчёт
            }

            // Для мобильных устройств рекомендуется использовать Perspective режим
            if (cam.orthographic && cam.CompareTag("MainCamera"))
            {
                Debug.LogWarning($"[GraphicsSettings] ⚠️ Главная камера '{cam.name}' в Orthographic режиме - это может вызвать растяжение!");
            }
        }
    }

    /// <summary>
    /// Настраиваем Canvas Scaler для корректного отображения UI на всех разрешениях
    /// </summary>
    private void EnsureCanvasScalerConfigured()
    {
        if (settingsPanel == null)
        {
            return;
        }

        // Находим Canvas в иерархии
        Canvas canvas = settingsPanel.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        UnityEngine.UI.CanvasScaler scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            Debug.Log("[GraphicsSettings] ✅ CanvasScaler добавлен к Canvas");
        }

        // Настраиваем Scale With Screen Size для адаптивности
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); // Базовое разрешение для дизайна
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        // На мобильных устройствах подстраиваемся под ширину (landscape) или высоту (portrait)
        if (Application.isMobilePlatform)
        {
            // Если экран шире чем 16:9, подстраиваемся под высоту (чтобы UI не был слишком мелким)
            float aspectRatio = (float)Screen.width / Screen.height;
            scaler.matchWidthOrHeight = aspectRatio > 1.8f ? 1.0f : 0.5f; // 1.0 = match height, 0.0 = match width
            Debug.Log($"[GraphicsSettings] 📱 Canvas Scaler настроен для aspect ratio {aspectRatio:F2} (match: {scaler.matchWidthOrHeight})");
        }
        else
        {
            scaler.matchWidthOrHeight = 0.5f; // Баланс между шириной и высотой для ПК
        }

        scaler.referencePixelsPerUnit = 100f;
    }

    /// <summary>
    /// Убеждаемся что на мобильных устройствах установлено правильное разрешение
    /// </summary>
    private void EnsureCorrectResolutionForMobile()
    {
        if (resolutions == null || resolutions.Length == 0)
        {
            return;
        }

        // Получаем текущее разрешение экрана устройства
        int deviceWidth = Screen.width;
        int deviceHeight = Screen.height;
        float deviceAspect = (float)deviceWidth / deviceHeight;

        Debug.Log($"[GraphicsSettings] 📱 Проверка разрешения: {deviceWidth}x{deviceHeight} (aspect: {deviceAspect:F3})");

        // Ищем точное совпадение
        int matchIndex = FindResolutionIndex(deviceWidth, deviceHeight);

        if (matchIndex >= 0)
        {
            Debug.Log($"[GraphicsSettings] ✅ Найдено точное совпадение разрешения: индекс {matchIndex}");
            currentResolutionIndex = matchIndex;
            if (resolutionDropdown != null)
            {
                resolutionDropdown.SetValueWithoutNotify(matchIndex);
                resolutionDropdown.RefreshShownValue();
            }
            return;
        }

        // Если точного совпадения нет, ищем ближайшее с таким же aspect ratio
        int closestIndex = -1;
        float minAspectDiff = float.MaxValue;

        for (int i = 0; i < resolutions.Length; i++)
        {
            float resAspect = (float)resolutions[i].width / resolutions[i].height;
            float aspectDiff = Mathf.Abs(resAspect - deviceAspect);

            if (aspectDiff < minAspectDiff)
            {
                minAspectDiff = aspectDiff;
                closestIndex = i;
            }
        }

        if (closestIndex >= 0 && minAspectDiff < 0.02f) // Допуск 2% для aspect ratio
        {
            Debug.Log($"[GraphicsSettings] ⚠️ Установка ближайшего разрешения: {resolutions[closestIndex].width}x{resolutions[closestIndex].height}");
            Screen.SetResolution(resolutions[closestIndex].width, resolutions[closestIndex].height, Screen.fullScreen);
            currentResolutionIndex = closestIndex;

            if (resolutionDropdown != null)
            {
                resolutionDropdown.SetValueWithoutNotify(closestIndex);
                resolutionDropdown.RefreshShownValue();
            }
        }
        else
        {
            // Если не нашли подходящее, используем максимальное разрешение устройства
            Debug.LogWarning($"[GraphicsSettings] ⚠️ Не найдено подходящее разрешение, используем нативное: {deviceWidth}x{deviceHeight}");
            Screen.SetResolution(deviceWidth, deviceHeight, true);
        }
    }

    private int GetAntiAliasingIndex()
    {
        // Получаем текущее значение Anti-Aliasing из URP Asset
        UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
        if (urpAsset != null)
        {
            // Конвертируем MsaaQuality enum в dropdown index
            MsaaQuality quality = (MsaaQuality)urpAsset.msaaSampleCount;
            switch (quality)
            {
                case MsaaQuality.Disabled: return 0;
                case MsaaQuality._2x: return 1;
                case MsaaQuality._4x: return 2;
                case MsaaQuality._8x: return 3;
            }
        }
        return 0;
    }

    private void SetAntiAliasing(int msaaValue)
    {
        UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
        if (urpAsset != null)
        {
            // Конвертируем int в MsaaQuality enum
            MsaaQuality quality = MsaaQuality.Disabled;
            switch (msaaValue)
            {
                case 0: quality = MsaaQuality.Disabled; break;
                case 2: quality = MsaaQuality._2x; break;
                case 4: quality = MsaaQuality._4x; break;
                case 8: quality = MsaaQuality._8x; break;
            }
            urpAsset.msaaSampleCount = (int)quality;
        }
    }

    private float GetRenderScale()
    {
        UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
        if (urpAsset != null)
        {
            return urpAsset.renderScale;
        }
        return 1.0f;
    }

    private void SetRenderScale(float scale)
    {
        UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
        if (urpAsset != null)
        {
            urpAsset.renderScale = scale;
        }
    }

    private void UpdateRenderScaleText(float value)
    {
        if (renderScaleText != null)
        {
            renderScaleText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    private int GetTargetFpsIndex()
    {
        int targetFps = Application.targetFrameRate;
        if (targetFps == 30) return 0;
        if (targetFps == 60) return 1;
        if (targetFps == 120) return 2;
        return 3; // Без ограничений
    }

    private int GetTargetFpsFromIndex(int index)
    {
        switch (index)
        {
            case 0: return 30;
            case 1: return 60;
            case 2: return 120;
            case 3: return -1; // Без ограничений
            default: return 60;
        }
    }

    #endregion

    #region Save/Load Settings

    private void SaveSettings()
    {
        PlayerPrefs.SetInt("QualityLevel", QualitySettings.GetQualityLevel());
        PlayerPrefs.SetInt("ResolutionWidth", Screen.width);
        PlayerPrefs.SetInt("ResolutionHeight", Screen.height);
        if (resolutions != null && resolutions.Length > 0)
        {
            PlayerPrefs.SetInt("ResolutionIndex", Mathf.Clamp(currentResolutionIndex, 0, resolutions.Length - 1));
        }
        PlayerPrefs.SetInt("Fullscreen", Screen.fullScreen ? 1 : 0);
        PlayerPrefs.SetInt("VSync", QualitySettings.vSyncCount);
        PlayerPrefs.SetInt("ShadowQuality", (int)QualitySettings.shadows);
        PlayerPrefs.SetInt("TargetFPS", Application.targetFrameRate);
        PlayerPrefs.SetInt("ShowFPS", showFps ? 1 : 0);
        PlayerPrefs.SetFloat("RenderScale", GetRenderScale());
        PlayerPrefs.SetInt("AntiAliasing", GetAntiAliasingIndex());
        PlayerPrefs.SetFloat("EntityViewDistance", entityViewDistance);
        PlayerPrefs.SetFloat("BuildingViewDistance", buildingViewDistance);
        PlayerPrefs.SetFloat("VegetationDistanceMultiplier", vegetationDistanceMultiplier);
        PlayerPrefs.Save();

        Debug.Log("[GraphicsSettings] ✅ Настройки сохранены");
    }

    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey("QualityLevel"))
        {
            int quality = PlayerPrefs.GetInt("QualityLevel");
            QualitySettings.SetQualityLevel(quality);
            if (qualityDropdown != null) qualityDropdown.value = quality;
        }

        if (PlayerPrefs.HasKey("Fullscreen"))
        {
            bool fullscreen = PlayerPrefs.GetInt("Fullscreen") == 1;
            Screen.fullScreen = fullscreen;
            if (fullscreenToggle != null) fullscreenToggle.isOn = fullscreen;
        }

        if (PlayerPrefs.HasKey("ResolutionIndex") && resolutions != null && resolutions.Length > 0)
        {
            ApplyResolutionByIndex(PlayerPrefs.GetInt("ResolutionIndex"));
        }
        else if (PlayerPrefs.HasKey("ResolutionWidth") && PlayerPrefs.HasKey("ResolutionHeight"))
        {
            int savedWidth = PlayerPrefs.GetInt("ResolutionWidth");
            int savedHeight = PlayerPrefs.GetInt("ResolutionHeight");
            Screen.SetResolution(savedWidth, savedHeight, Screen.fullScreen);

            if (resolutions != null && resolutions.Length > 0)
            {
                int matchedIndex = FindResolutionIndex(savedWidth, savedHeight);
                if (matchedIndex >= 0)
                {
                    ApplyResolutionByIndex(matchedIndex);
                }
            }
        }

        if (PlayerPrefs.HasKey("VSync"))
        {
            QualitySettings.vSyncCount = PlayerPrefs.GetInt("VSync");
            if (vsyncToggle != null) vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
        }

        if (PlayerPrefs.HasKey("ShadowQuality"))
        {
            int savedShadows = Mathf.Clamp(PlayerPrefs.GetInt("ShadowQuality"), 0, System.Enum.GetValues(typeof(UnityEngine.ShadowQuality)).Length - 1);
            QualitySettings.shadows = (UnityEngine.ShadowQuality)savedShadows;
            if (shadowQualityDropdown != null)
            {
                shadowQualityDropdown.SetValueWithoutNotify(savedShadows);
            }
        }

        if (PlayerPrefs.HasKey("TargetFPS"))
        {
            Application.targetFrameRate = PlayerPrefs.GetInt("TargetFPS");
            if (targetFpsDropdown != null) targetFpsDropdown.value = GetTargetFpsIndex();
        }

        if (PlayerPrefs.HasKey("ShowFPS"))
        {
            showFps = PlayerPrefs.GetInt("ShowFPS") == 1;
            if (showFpsToggle != null) showFpsToggle.isOn = showFps;
            if (fpsCounterText != null) fpsCounterText.gameObject.SetActive(showFps);
        }

        if (PlayerPrefs.HasKey("RenderScale"))
        {
            float renderScale = PlayerPrefs.GetFloat("RenderScale");
            SetRenderScale(renderScale);
            SyncRenderScaleUI(renderScale);
        }

        float entityDistanceDefault = entityViewDistanceSlider != null
            ? Mathf.Lerp(entityViewDistanceSlider.minValue, entityViewDistanceSlider.maxValue, 0.6f)
            : entityViewDistance;
        float buildingDistanceDefault = buildingViewDistanceSlider != null
            ? Mathf.Lerp(buildingViewDistanceSlider.minValue, buildingViewDistanceSlider.maxValue, 0.6f)
            : buildingViewDistance;
        float vegetationDistanceDefault = vegetationDrawDistanceSlider != null
            ? Mathf.Clamp(1f, vegetationDrawDistanceSlider.minValue, vegetationDrawDistanceSlider.maxValue)
            : vegetationDistanceMultiplier;

        ApplyEntityViewDistance(PlayerPrefs.GetFloat("EntityViewDistance", entityDistanceDefault));
        ApplyBuildingViewDistance(PlayerPrefs.GetFloat("BuildingViewDistance", buildingDistanceDefault));
        ApplyVegetationDistanceMultiplier(PlayerPrefs.GetFloat("VegetationDistanceMultiplier", vegetationDistanceDefault));

        if (PlayerPrefs.HasKey("AntiAliasing"))
        {
            int aaIndex = PlayerPrefs.GetInt("AntiAliasing");
            int msaaValue = aaIndex == 0 ? 0 : (int)Mathf.Pow(2, aaIndex);
            SetAntiAliasing(msaaValue);
            if (antiAliasingDropdown != null) antiAliasingDropdown.value = aaIndex;
        }

        Debug.Log("[GraphicsSettings] ✅ Настройки загружены");
    }

    #endregion

    #region Public Methods

    public void OpenSettings()
    {
        Debug.Log("[GraphicsSettings] 📂 Открываем меню настроек...");
        if (settingsCanvasGroup != null)
        {
            settingsCanvasGroup.alpha = 1f;
            settingsCanvasGroup.interactable = true;
            settingsCanvasGroup.blocksRaycasts = true;
        }
        else if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        Debug.Log("[GraphicsSettings] 📁 Закрываем меню настроек...");
        if (settingsCanvasGroup != null)
        {
            settingsCanvasGroup.alpha = 0f;
            settingsCanvasGroup.interactable = false;
            settingsCanvasGroup.blocksRaycasts = false;
        }
        else if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void ApplySettings()
    {
        Debug.Log("[GraphicsSettings] ✅ Применяем настройки...");

        // Применяем разрешение
        if (currentResolutionIndex >= 0 && currentResolutionIndex < resolutions.Length)
        {
            Resolution resolution = resolutions[currentResolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        }
        else if (resolutions != null && resolutions.Length > 0)
        {
            currentResolutionIndex = FindResolutionIndex(Screen.width, Screen.height);
            if (currentResolutionIndex >= 0)
            {
                Resolution resolution = resolutions[currentResolutionIndex];
                Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
            }
        }

        // Сохраняем настройки
        SaveSettings();
        isDirty = false;

        Debug.Log("[GraphicsSettings] ✅ Настройки применены и сохранены");
    }

    public void ToggleSettingsPanel()
    {
        bool isOpen = settingsCanvasGroup != null
            ? settingsCanvasGroup.alpha > 0.01f
            : (settingsPanel != null && settingsPanel.activeSelf);

        if (isOpen)
        {
            CloseSettings();
        }
        else
        {
            OpenSettings();
        }
    }

    #endregion
}
