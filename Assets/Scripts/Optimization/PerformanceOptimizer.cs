using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;
using Tenkoku.Effects;

/// <summary>
/// Агрессивная система оптимизации для достижения 60-120 FPS на Android
/// Может быть включена/выключена пользователем
/// </summary>
public class PerformanceOptimizer : MonoBehaviour
{
    public static PerformanceOptimizer Instance { get; private set; }

    [Header("Optimization Settings")]
    [SerializeField] private bool enableOptimization = true;
    [SerializeField] private OptimizationPreset preset = OptimizationPreset.Balanced;

    [Header("FPS Monitoring")]
    [SerializeField] private bool showFpsMonitor = true;
    [SerializeField] private float targetFps = 60f;
    [SerializeField] private bool enableAdaptiveQuality = true;

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Light mainLight;

    // Optimization states
    private bool isOptimizationActive = false;
    private OptimizationPreset currentPreset;

    // Original settings backup
    private BackupSettings backupSettings;

    // FPS tracking
    private float deltaTime = 0f;
    private float currentFps = 0f;
    private Queue<float> fpsHistory = new Queue<float>();
    private const int FPS_HISTORY_SIZE = 60;

    // Adaptive quality
    private Coroutine adaptiveQualityCoroutine;
    private float lastQualityChangeTime = 0f;
    private const float QUALITY_CHANGE_COOLDOWN = 3f;

    // TENKOKU references
    private TenkokuSkyFog tenkokuFog;
    private MonoBehaviour tenkokuModule;

    public enum OptimizationPreset
    {
        Balanced,           // 60 FPS target - сохраняет баланс качества
        Performance,        // 90 FPS target - приоритет производительности
        UltraPerformance    // 120 FPS target - максимальная оптимизация
    }

    private struct BackupSettings
    {
        // Render Pipeline
        public int msaaSampleCount;
        public float renderScale;
        public int mainLightShadowResolution;
        public int additionalLightsShadowResolution;
        public float shadowDistance;

        // Quality Settings
        public int qualityLevel;
        public UnityEngine.ShadowQuality shadows;
        public int vSyncCount;
        public float lodBias;
        public int pixelLightCount;

        // Camera
        public float farClipPlane;
        public bool hdr;

        // Terrain
        public float detailDistance;
        public float detailDensity;
        public float treeDistance;
        public int pixelError;

        // Post-processing
        public bool postProcessingEnabled;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Load saved settings
        enableOptimization = PlayerPrefs.GetInt("EnableOptimization", 1) == 1;
        preset = (OptimizationPreset)PlayerPrefs.GetInt("OptimizationPreset", (int)OptimizationPreset.Balanced);
    }

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainLight == null)
            mainLight = FindFirstObjectByType<Light>();

        // Find TENKOKU components
        FindTenkokuComponents();

        // Apply optimization on mobile platforms
        if (Application.isMobilePlatform && enableOptimization)
        {
            StartCoroutine(InitializeOptimization());
        }

        Debug.Log($"[PerformanceOptimizer] Initialized. Mobile: {Application.isMobilePlatform}, Optimization: {enableOptimization}");
    }

    private void FindTenkokuComponents()
    {
        if (mainCamera != null)
        {
            tenkokuFog = mainCamera.GetComponent<TenkokuSkyFog>();
        }

        tenkokuModule = FindFirstObjectByType(System.Type.GetType("Tenkoku.Core.TenkokuModule")) as MonoBehaviour;
    }

    private IEnumerator InitializeOptimization()
    {
        // Wait for scene to fully load
        yield return new WaitForSeconds(0.5f);

        BackupCurrentSettings();
        ApplyOptimizationPreset(preset);

        if (enableAdaptiveQuality)
        {
            StartAdaptiveQuality();
        }

        Debug.Log($"[PerformanceOptimizer] ✅ Optimization applied: {preset}");
    }

    void Update()
    {
        // Update FPS tracking
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        currentFps = 1.0f / deltaTime;

        fpsHistory.Enqueue(currentFps);
        if (fpsHistory.Count > FPS_HISTORY_SIZE)
        {
            fpsHistory.Dequeue();
        }
    }

    void OnGUI()
    {
        if (showFpsMonitor && Application.isMobilePlatform)
        {
            int w = Screen.width, h = Screen.height;
            GUIStyle style = new GUIStyle();

            Rect rect = new Rect(20, h - 100, 300, 80);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h / 40;
            style.normal.textColor = GetFpsColor(currentFps);

            float avgFps = GetAverageFPS();
            string text = $"FPS: {Mathf.Ceil(currentFps)}\n" +
                         $"AVG: {avgFps:F1}\n" +
                         $"Preset: {preset}";

            GUI.Label(rect, text, style);
        }
    }

    private Color GetFpsColor(float fps)
    {
        if (fps >= targetFps * 0.9f) return Color.green;
        if (fps >= targetFps * 0.6f) return Color.yellow;
        return Color.red;
    }

    private float GetAverageFPS()
    {
        if (fpsHistory.Count == 0) return 60f;

        float sum = 0f;
        foreach (float fps in fpsHistory)
        {
            sum += fps;
        }
        return sum / fpsHistory.Count;
    }

    #region Optimization Control

    public void SetOptimizationEnabled(bool enabled)
    {
        enableOptimization = enabled;
        PlayerPrefs.SetInt("EnableOptimization", enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (Application.isMobilePlatform)
        {
            if (enabled)
            {
                StartCoroutine(InitializeOptimization());
            }
            else
            {
                RestoreOriginalSettings();
            }
        }

        Debug.Log($"[PerformanceOptimizer] Optimization {(enabled ? "ENABLED" : "DISABLED")}");
    }

    public void SetOptimizationPreset(OptimizationPreset newPreset)
    {
        preset = newPreset;
        currentPreset = newPreset;
        PlayerPrefs.SetInt("OptimizationPreset", (int)newPreset);
        PlayerPrefs.Save();

        if (Application.isMobilePlatform && enableOptimization)
        {
            ApplyOptimizationPreset(newPreset);
        }

        Debug.Log($"[PerformanceOptimizer] Preset changed to: {newPreset}");
    }

    public void SetTargetFPS(float target)
    {
        targetFps = target;
        Application.targetFrameRate = (int)target;
        Debug.Log($"[PerformanceOptimizer] Target FPS: {target}");
    }

    #endregion

    #region Backup & Restore

    private void BackupCurrentSettings()
    {
        backupSettings = new BackupSettings();

        // URP Settings
        UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
        if (urpAsset != null)
        {
            backupSettings.msaaSampleCount = urpAsset.msaaSampleCount;
            backupSettings.renderScale = urpAsset.renderScale;
            backupSettings.mainLightShadowResolution = (int)urpAsset.mainLightShadowmapResolution;
            backupSettings.shadowDistance = urpAsset.shadowDistance;
        }

        // Quality Settings
        backupSettings.qualityLevel = QualitySettings.GetQualityLevel();
        backupSettings.shadows = QualitySettings.shadows;
        backupSettings.vSyncCount = QualitySettings.vSyncCount;
        backupSettings.lodBias = QualitySettings.lodBias;
        backupSettings.pixelLightCount = QualitySettings.pixelLightCount;

        // Camera
        if (mainCamera != null)
        {
            backupSettings.farClipPlane = mainCamera.farClipPlane;
            backupSettings.hdr = mainCamera.allowHDR;
        }

        // Terrain
        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains.Length > 0)
        {
            backupSettings.detailDistance = terrains[0].detailObjectDistance;
            backupSettings.detailDensity = terrains[0].detailObjectDensity;
            backupSettings.treeDistance = terrains[0].treeDistance;
            backupSettings.pixelError = (int)terrains[0].heightmapPixelError;
        }

        Debug.Log("[PerformanceOptimizer] 💾 Current settings backed up");
    }

    private void RestoreOriginalSettings()
    {
        UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
        if (urpAsset != null)
        {
            urpAsset.msaaSampleCount = backupSettings.msaaSampleCount;
            urpAsset.renderScale = backupSettings.renderScale;
            urpAsset.shadowDistance = backupSettings.shadowDistance;
        }

        QualitySettings.SetQualityLevel(backupSettings.qualityLevel);
        QualitySettings.shadows = backupSettings.shadows;
        QualitySettings.vSyncCount = backupSettings.vSyncCount;
        QualitySettings.lodBias = backupSettings.lodBias;
        QualitySettings.pixelLightCount = backupSettings.pixelLightCount;

        if (mainCamera != null)
        {
            mainCamera.farClipPlane = backupSettings.farClipPlane;
            mainCamera.allowHDR = backupSettings.hdr;
        }

        Terrain[] terrains = Terrain.activeTerrains;
        foreach (Terrain terrain in terrains)
        {
            terrain.detailObjectDistance = backupSettings.detailDistance;
            terrain.detailObjectDensity = backupSettings.detailDensity;
            terrain.treeDistance = backupSettings.treeDistance;
            terrain.heightmapPixelError = backupSettings.pixelError;
        }

        // Restore TENKOKU
        if (tenkokuFog != null)
        {
            tenkokuFog.enabled = true;
        }

        Debug.Log("[PerformanceOptimizer] ↩️ Original settings restored");
    }

    #endregion

    #region Optimization Presets

    private void ApplyOptimizationPreset(OptimizationPreset preset)
    {
        isOptimizationActive = true;
        currentPreset = preset;

        switch (preset)
        {
            case OptimizationPreset.Balanced:
                ApplyBalancedOptimization();
                targetFps = 60f;
                break;

            case OptimizationPreset.Performance:
                ApplyPerformanceOptimization();
                targetFps = 90f;
                break;

            case OptimizationPreset.UltraPerformance:
                ApplyUltraPerformanceOptimization();
                targetFps = 120f;
                break;
        }

        Application.targetFrameRate = (int)targetFps;

        // Common optimizations for all presets
        ApplyCommonOptimizations();

        Debug.Log($"[PerformanceOptimizer] 🚀 Applied {preset} preset (Target: {targetFps} FPS)");
    }

    private void ApplyBalancedOptimization()
    {
        // Render Scale: 75% (хороший баланс)
        SetRenderScale(0.75f);

        // Shadows: Low resolution, близкая дистанция
        SetShadowQuality(ShadowResolution._512, 30f, 1);

        // TENKOKU: Снижаем качество, но оставляем включённым
        OptimizeTenkoku(TenkokuQuality.Medium);

        // Camera: Far clip увеличен для видимости terrain вдали
        if (mainCamera != null)
        {
            mainCamera.farClipPlane = 600f; // Terrain виден далеко
        }

        // Terrain: Низкая детализация деталей (трава), но terrain виден далеко
        // OptimizeTerrain(detailDistance, detailDensity, treeDistance, pixelError)
        OptimizeTerrain(25f, 0.3f, 80f, 10);
        // detailDistance: 25m - трава видна близко
        // treeDistance: 80m - деревья исчезают раньше
        // terrain basemapDistance: 500m (в методе) - terrain виден всегда

        // Post-processing: Minimal
        DisableExpensivePostProcessing();

        // Particles: Limit count
        QualitySettings.particleRaycastBudget = 64;

        Debug.Log("[PerformanceOptimizer] ⚖️ Balanced: 75% scale, 512px shadows, terrain visible far");
    }

    private void ApplyPerformanceOptimization()
    {
        // Render Scale: 65% (производительность)
        SetRenderScale(0.65f);

        // Shadows: Very low resolution, очень близко
        SetShadowQuality(ShadowResolution._256, 20f, 1);

        // TENKOKU: Minimal quality
        OptimizeTenkoku(TenkokuQuality.Low);

        // Camera: Far clip для видимости terrain
        if (mainCamera != null)
        {
            mainCamera.farClipPlane = 500f; // Terrain виден
            mainCamera.allowHDR = false; // Disable HDR
        }

        // Terrain: Очень низкая детализация деталей
        OptimizeTerrain(20f, 0.2f, 60f, 15);
        // detailDistance: 20m - трава очень близко
        // treeDistance: 60m - деревья исчезают близко
        // terrain basemapDistance: 500m - terrain виден всегда

        // Post-processing: Disabled
        DisableAllPostProcessing();

        // Particles: Very limited
        QualitySettings.particleRaycastBudget = 32;

        // Disable soft shadows
        QualitySettings.shadows = UnityEngine.ShadowQuality.HardOnly;

        Debug.Log("[PerformanceOptimizer] ⚡ Performance: 65% scale, 256px shadows, 20m distance");
    }

    private void ApplyUltraPerformanceOptimization()
    {
        // Render Scale: 50% (максимальная производительность)
        SetRenderScale(0.5f);

        // Shadows: Disabled or minimal
        SetShadowQuality(ShadowResolution._256, 15f, 1);
        QualitySettings.shadows = UnityEngine.ShadowQuality.HardOnly;

        // TENKOKU: Disabled (самый большой прирост FPS)
        OptimizeTenkoku(TenkokuQuality.Disabled);

        // Camera: Far clip для terrain, но не слишком далеко
        if (mainCamera != null)
        {
            mainCamera.farClipPlane = 400f; // Terrain виден, но ближе
            mainCamera.allowHDR = false;
            mainCamera.allowMSAA = false;
        }

        // Terrain: Минимальная детализация деталей
        OptimizeTerrain(15f, 0.15f, 40f, 20);
        // detailDistance: 15m - трава минимально
        // treeDistance: 40m - деревья очень близко
        // terrain basemapDistance: 500m - terrain виден всегда

        // Post-processing: All disabled
        DisableAllPostProcessing();

        // Particles: Minimal
        QualitySettings.particleRaycastBudget = 16;

        // Additional aggressive optimizations
        QualitySettings.pixelLightCount = 1; // Only main light
        QualitySettings.lodBias = 0.5f; // Более агрессивный LOD

        // Disable reflection probes
        QualitySettings.realtimeReflectionProbes = false;

        Debug.Log("[PerformanceOptimizer] 🔥 Ultra Performance: 50% scale, minimal shadows, TENKOKU OFF");
    }

    private void ApplyCommonOptimizations()
    {
        // VSync: Always off on mobile
        QualitySettings.vSyncCount = 0;

        // Anisotropic filtering: Disabled
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;

        // Texture quality: Medium
        QualitySettings.globalTextureMipmapLimit = 1;

        // Skin weights: 2 bones (instead of 4)
        QualitySettings.skinWeights = SkinWeights.TwoBones;

        // Async upload: Optimize
        QualitySettings.asyncUploadTimeSlice = 2;
        QualitySettings.asyncUploadBufferSize = 16;

        // Realtime GI: Disabled
        RendererExtensions.UpdateGIMaterials(null);

        // КРИТИЧНО: Оптимизируем Point Lights (в BattleScene их 16!)
        OptimizePointLights();

        Debug.Log("[PerformanceOptimizer] ✅ Common optimizations applied");
    }

    /// <summary>
    /// Оптимизация Point Lights - снижение range и intensity для экономии FPS
    /// В BattleScene найдено 16 Point Lights - это ОЧЕНЬ много для мобильных!
    /// </summary>
    private void OptimizePointLights()
    {
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        int optimizedCount = 0;

        foreach (Light light in allLights)
        {
            if (light.type == LightType.Point || light.type == LightType.Spot)
            {
                // Снижаем range для уменьшения количества затронутых объектов
                if (light.range > 15f)
                {
                    light.range = Mathf.Min(light.range, 15f);
                }

                // Снижаем intensity у слишком ярких источников
                if (light.intensity > 100f)
                {
                    light.intensity = Mathf.Min(light.intensity, 100f);
                }

                // Убеждаемся что тени отключены (ОЧЕНЬ дорого на мобильных)
                light.shadows = LightShadows.None;

                optimizedCount++;
            }
        }

        Debug.Log($"[PerformanceOptimizer] 💡 Оптимизировано {optimizedCount} Point/Spot Lights (range ≤15m, intensity ≤100, shadows OFF)");
    }

    #endregion

    #region Specific Optimizations

    private void SetRenderScale(float scale)
    {
        UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
        if (urpAsset != null)
        {
            urpAsset.renderScale = scale;
            Debug.Log($"[PerformanceOptimizer] 📐 Render Scale: {scale * 100}%");
        }
    }

    private void SetShadowQuality(ShadowResolution resolution, float distance, int cascades)
    {
        UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
        if (urpAsset != null)
        {
            urpAsset.mainLightShadowmapResolution = (int)resolution;
            urpAsset.shadowDistance = distance;
            urpAsset.shadowCascadeCount = cascades;

            Debug.Log($"[PerformanceOptimizer] 🌑 Shadows: {resolution}, {distance}m, {cascades} cascade(s)");
        }
    }

    private enum TenkokuQuality
    {
        Disabled,
        Low,
        Medium
    }

    private enum ShadowResolution
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096
    }

    private void OptimizeTenkoku(TenkokuQuality quality)
    {
        if (tenkokuFog != null)
        {
            switch (quality)
            {
                case TenkokuQuality.Disabled:
                    tenkokuFog.enabled = false;
                    tenkokuFog.fogSkybox = false;
                    Debug.Log("[PerformanceOptimizer] 🌫️ TENKOKU: DISABLED (max FPS gain)");
                    break;

                case TenkokuQuality.Low:
                    tenkokuFog.enabled = true;
                    tenkokuFog.fogSkybox = false;
                    tenkokuFog.fogHorizon = false;
                    tenkokuFog.heightDensity = 0.001f;
                    tenkokuFog.heatDistance = 0f; // Disable heat distortion
                    Debug.Log("[PerformanceOptimizer] 🌫️ TENKOKU: LOW (minimal fog)");
                    break;

                case TenkokuQuality.Medium:
                    tenkokuFog.enabled = true;
                    tenkokuFog.fogSkybox = true;
                    tenkokuFog.fogHorizon = false;
                    tenkokuFog.heightDensity = 0.002f;
                    tenkokuFog.heatDistance = 0f;
                    Debug.Log("[PerformanceOptimizer] 🌫️ TENKOKU: MEDIUM (balanced fog)");
                    break;
            }
        }

        // Try to disable other TENKOKU components
        if (tenkokuModule != null)
        {
            // Disable sun rays if possible
            var type = tenkokuModule.GetType();
            var sunRaysField = type.GetField("useSunRays", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (sunRaysField != null)
            {
                sunRaysField.SetValue(tenkokuModule, false);
                Debug.Log("[PerformanceOptimizer] ☀️ TENKOKU Sun Rays: DISABLED");
            }
        }
    }

    private void OptimizeTerrain(float detailDistance, float detailDensity, float treeDistance, int pixelError)
    {
        Terrain[] terrains = Terrain.activeTerrains;
        foreach (Terrain terrain in terrains)
        {
            // ВАЖНО: detailDistance - трава/детали (низкое значение)
            terrain.detailObjectDistance = detailDistance;
            terrain.detailObjectDensity = detailDensity;

            // ВАЖНО: treeDistance - деревья/кусты (среднее значение, не влияет на terrain)
            terrain.treeDistance = treeDistance;
            terrain.treeBillboardDistance = treeDistance * 0.7f;

            // ВАЖНО: heightmapPixelError - качество геометрии terrain (выше = быстрее)
            terrain.heightmapPixelError = pixelError;

            // КРИТИЧНО: basemapDistance - дальность детальных текстур на terrain
            // Высокое значение = terrain виден далеко (но менее детальные текстуры вдали)
            terrain.basemapDistance = 500f; // Terrain видно всегда, детали упрощаются вдали

            // Reduce terrain quality
            terrain.groupingID = 0;
            terrain.allowAutoConnect = false;

            // Отключаем тени от terrain (ОЧЕНЬ дорого!)
            terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Отключаем reflection probes на terrain
            terrain.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        Debug.Log($"[PerformanceOptimizer] 🏔️ Terrain: detail {detailDistance}m, trees {treeDistance}m, basemap 500m, error {pixelError}");
    }

    private void DisableExpensivePostProcessing()
    {
        // Disable expensive effects via volume
        Volume[] volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
        foreach (Volume volume in volumes)
        {
            if (volume.profile != null)
            {
                // Keep only essential effects
                // This will be handled by volume weights
                volume.weight = 0.5f; // Reduce intensity
            }
        }

        Debug.Log("[PerformanceOptimizer] 📸 Post-Processing: MINIMAL");
    }

    private void DisableAllPostProcessing()
    {
        Volume[] volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
        foreach (Volume volume in volumes)
        {
            volume.enabled = false;
        }

        if (mainCamera != null)
        {
            var urpCameraData = mainCamera.GetUniversalAdditionalCameraData();
            if (urpCameraData != null)
            {
                urpCameraData.renderPostProcessing = false;
            }
        }

        Debug.Log("[PerformanceOptimizer] 📸 Post-Processing: ALL DISABLED");
    }

    #endregion

    #region Adaptive Quality

    private void StartAdaptiveQuality()
    {
        if (adaptiveQualityCoroutine != null)
        {
            StopCoroutine(adaptiveQualityCoroutine);
        }
        adaptiveQualityCoroutine = StartCoroutine(AdaptiveQualityRoutine());
    }

    private void StopAdaptiveQuality()
    {
        if (adaptiveQualityCoroutine != null)
        {
            StopCoroutine(adaptiveQualityCoroutine);
            adaptiveQualityCoroutine = null;
        }
    }

    private IEnumerator AdaptiveQualityRoutine()
    {
        while (enableAdaptiveQuality)
        {
            yield return new WaitForSeconds(2f);

            float avgFps = GetAverageFPS();
            float timeSinceChange = Time.time - lastQualityChangeTime;

            if (timeSinceChange < QUALITY_CHANGE_COOLDOWN)
                continue;

            // If FPS is too low, decrease quality
            if (avgFps < targetFps * 0.8f)
            {
                if (currentPreset == OptimizationPreset.Balanced)
                {
                    SetOptimizationPreset(OptimizationPreset.Performance);
                    lastQualityChangeTime = Time.time;
                }
                else if (currentPreset == OptimizationPreset.Performance)
                {
                    SetOptimizationPreset(OptimizationPreset.UltraPerformance);
                    lastQualityChangeTime = Time.time;
                }

                Debug.Log($"[PerformanceOptimizer] ⬇️ FPS too low ({avgFps:F1}), decreasing quality");
            }
            // If FPS is stable and high, can increase quality
            else if (avgFps > targetFps * 1.2f)
            {
                if (currentPreset == OptimizationPreset.UltraPerformance)
                {
                    SetOptimizationPreset(OptimizationPreset.Performance);
                    lastQualityChangeTime = Time.time;
                }
                else if (currentPreset == OptimizationPreset.Performance && preset == OptimizationPreset.Balanced)
                {
                    SetOptimizationPreset(OptimizationPreset.Balanced);
                    lastQualityChangeTime = Time.time;
                }

                Debug.Log($"[PerformanceOptimizer] ⬆️ FPS high ({avgFps:F1}), increasing quality");
            }
        }
    }

    #endregion

    void OnDestroy()
    {
        StopAdaptiveQuality();
    }
}
