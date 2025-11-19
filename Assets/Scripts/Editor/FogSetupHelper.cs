using UnityEngine;
using UnityEditor;
using Tenkoku.Effects;

/// <summary>
/// Помощник для быстрой настройки тумана в сцене
/// </summary>
public class FogSetupHelper : EditorWindow
{
    // Unity Fog Settings
    private bool unityFogEnabled = true;
    private FogMode fogMode = FogMode.Exponential;
    private float fogDensity = 0.02f;
    private float fogStart = 10f;
    private float fogEnd = 100f;
    private Color fogColor = new Color(0.5f, 0.5f, 0.5f);

    // TENKOKU Fog Settings
    private bool tenkokuFogEnabled = false;
    private float height = 185f;
    private float heightDensity = 0.00325f;
    private bool useRadialDistance = true;
    private bool fogHorizon = false;
    private bool fogSkybox = true;

    // Heat Distortion
    private float heatSpd = 4f;
    private float heatScale = 2f;
    private float heatDistance = 0.01f;

    // Presets
    private enum FogPreset
    {
        Custom,
        LightMorningHaze,
        DenseForestFog,
        MysticalPurple,
        DesertHeat,
        NightFog
    }
    private FogPreset currentPreset = FogPreset.Custom;

    [MenuItem("Aetherion/Fog Setup Helper")]
    public static void ShowWindow()
    {
        FogSetupHelper window = GetWindow<FogSetupHelper>("Fog Setup");
        window.minSize = new Vector2(400, 600);
        window.LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        unityFogEnabled = RenderSettings.fog;
        fogMode = RenderSettings.fogMode;
        fogDensity = RenderSettings.fogDensity;
        fogStart = RenderSettings.fogStartDistance;
        fogEnd = RenderSettings.fogEndDistance;
        fogColor = RenderSettings.fogColor;

        // Load TENKOKU settings if exists
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            TenkokuSkyFog tenkokuFog = mainCam.GetComponent<TenkokuSkyFog>();
            if (tenkokuFog != null)
            {
                tenkokuFogEnabled = tenkokuFog.enabled;
                height = tenkokuFog.height;
                heightDensity = tenkokuFog.heightDensity;
                useRadialDistance = tenkokuFog.useRadialDistance;
                fogHorizon = tenkokuFog.fogHorizon;
                fogSkybox = tenkokuFog.fogSkybox;
                heatSpd = tenkokuFog.heatSpd;
                heatScale = tenkokuFog.heatScale;
                heatDistance = tenkokuFog.heatDistance;
            }
        }
    }

    void OnGUI()
    {
        GUILayout.Label("Fog Setup Helper", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // Presets
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Пресеты тумана", EditorStyles.boldLabel);
        FogPreset newPreset = (FogPreset)EditorGUILayout.EnumPopup("Выбрать пресет:", currentPreset);
        if (newPreset != currentPreset)
        {
            currentPreset = newPreset;
            ApplyPreset(currentPreset);
        }

        if (GUILayout.Button("Применить выбранный пресет"))
        {
            ApplyPreset(currentPreset);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Unity Built-in Fog
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Unity Built-in Fog", EditorStyles.boldLabel);
        unityFogEnabled = EditorGUILayout.Toggle("Включить туман", unityFogEnabled);

        if (unityFogEnabled)
        {
            fogMode = (FogMode)EditorGUILayout.EnumPopup("Режим тумана:", fogMode);

            if (fogMode == FogMode.Linear)
            {
                fogStart = EditorGUILayout.Slider("Start Distance", fogStart, 0f, 500f);
                fogEnd = EditorGUILayout.Slider("End Distance", fogEnd, fogStart + 1f, 1000f);
            }
            else
            {
                fogDensity = EditorGUILayout.Slider("Density", fogDensity, 0f, 0.1f);
            }

            fogColor = EditorGUILayout.ColorField("Цвет тумана", fogColor);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // TENKOKU Sky Fog
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("TENKOKU Sky Fog (Advanced)", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "TENKOKU Fog - продвинутый туман с высотой и эффектами.\n" +
            "Требует TenkokuSkyFog компонент на Main Camera.",
            MessageType.Info);

        tenkokuFogEnabled = EditorGUILayout.Toggle("Включить TENKOKU Fog", tenkokuFogEnabled);

        if (tenkokuFogEnabled)
        {
            EditorGUILayout.Space(5);
            GUILayout.Label("Основные настройки:", EditorStyles.miniBoldLabel);
            height = EditorGUILayout.Slider("Height (высота)", height, 0f, 500f);
            heightDensity = EditorGUILayout.Slider("Height Density", heightDensity, 0.00001f, 0.1f);
            useRadialDistance = EditorGUILayout.Toggle("Use Radial Distance", useRadialDistance);
            fogHorizon = EditorGUILayout.Toggle("Fog Horizon", fogHorizon);
            fogSkybox = EditorGUILayout.Toggle("Fog Skybox", fogSkybox);

            EditorGUILayout.Space(5);
            GUILayout.Label("Heat Distortion:", EditorStyles.miniBoldLabel);
            heatSpd = EditorGUILayout.Slider("Heat Speed", heatSpd, 0f, 10f);
            heatScale = EditorGUILayout.Slider("Heat Scale", heatScale, 0f, 10f);
            heatDistance = EditorGUILayout.Slider("Heat Distance", heatDistance, 0f, 0.1f);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(20);

        // Apply Button
        if (GUILayout.Button("Применить настройки", GUILayout.Height(40)))
        {
            ApplySettings();
        }

        EditorGUILayout.Space(5);

        // Quick Actions
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Сбросить все"))
        {
            ResetSettings();
        }
        if (GUILayout.Button("Отключить весь туман"))
        {
            DisableAllFog();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Performance Info
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Информация:", EditorStyles.miniBoldLabel);
        EditorGUILayout.HelpBox(
            "Unity Fog: Лёгкий, подходит для мобильных\n" +
            "TENKOKU Fog: Высокое качество, требует больше ресурсов\n" +
            "Для Android рекомендуется только Unity Fog",
            MessageType.None);
        EditorGUILayout.EndVertical();
    }

    private void ApplyPreset(FogPreset preset)
    {
        switch (preset)
        {
            case FogPreset.LightMorningHaze:
                // Лёгкая утренняя дымка
                unityFogEnabled = true;
                fogMode = FogMode.Exponential;
                fogDensity = 0.01f;
                fogColor = new Color(0.78f, 0.84f, 0.88f); // #C8D5E0
                tenkokuFogEnabled = true;
                height = 150f;
                heightDensity = 0.002f;
                fogSkybox = true;
                fogHorizon = false;
                Debug.Log("[FogSetup] Применён пресет: Лёгкая утренняя дымка");
                break;

            case FogPreset.DenseForestFog:
                // Густой лесной туман
                unityFogEnabled = true;
                fogMode = FogMode.ExponentialSquared;
                fogDensity = 0.03f;
                fogColor = new Color(0.54f, 0.61f, 0.63f); // #8A9BA0
                tenkokuFogEnabled = true;
                height = 100f;
                heightDensity = 0.008f;
                fogSkybox = true;
                fogHorizon = true;
                Debug.Log("[FogSetup] Применён пресет: Густой лесной туман");
                break;

            case FogPreset.MysticalPurple:
                // Мистический фиолетовый туман
                unityFogEnabled = true;
                fogMode = FogMode.Exponential;
                fogDensity = 0.025f;
                fogColor = new Color(0.42f, 0.35f, 0.55f); // #6A5A8C
                tenkokuFogEnabled = true;
                height = 200f;
                heightDensity = 0.005f;
                fogSkybox = true;
                fogHorizon = true;
                Debug.Log("[FogSetup] Применён пресет: Мистический фиолетовый туман");
                break;

            case FogPreset.DesertHeat:
                // Пустынная жара
                unityFogEnabled = true;
                fogMode = FogMode.Linear;
                fogStart = 20f;
                fogEnd = 300f;
                fogColor = new Color(1f, 0.91f, 0.75f); // #FFE8C0
                tenkokuFogEnabled = true;
                height = 300f;
                heightDensity = 0.001f;
                heatSpd = 8f;
                heatScale = 4f;
                heatDistance = 0.02f;
                Debug.Log("[FogSetup] Применён пресет: Пустынная жара");
                break;

            case FogPreset.NightFog:
                // Ночной туман
                unityFogEnabled = true;
                fogMode = FogMode.ExponentialSquared;
                fogDensity = 0.02f;
                fogColor = new Color(0.1f, 0.15f, 0.19f); // #1A2530
                tenkokuFogEnabled = true;
                height = 120f;
                heightDensity = 0.006f;
                fogSkybox = true;
                fogHorizon = true;
                Debug.Log("[FogSetup] Применён пресет: Ночной туман");
                break;
        }

        ApplySettings();
    }

    private void ApplySettings()
    {
        // Apply Unity Fog
        RenderSettings.fog = unityFogEnabled;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;
        RenderSettings.fogColor = fogColor;

        Debug.Log($"[FogSetup] Unity Fog применён: Mode={fogMode}, Density={fogDensity:F4}, Color={fogColor}");

        // Apply TENKOKU Fog
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            TenkokuSkyFog tenkokuFog = mainCam.GetComponent<TenkokuSkyFog>();

            if (tenkokuFogEnabled)
            {
                if (tenkokuFog == null)
                {
                    tenkokuFog = mainCam.gameObject.AddComponent<TenkokuSkyFog>();
                    Debug.Log("[FogSetup] TenkokuSkyFog компонент добавлен на Main Camera");
                }

                tenkokuFog.enabled = true;
                tenkokuFog.height = height;
                tenkokuFog.heightDensity = heightDensity;
                tenkokuFog.useRadialDistance = useRadialDistance;
                tenkokuFog.fogHorizon = fogHorizon;
                tenkokuFog.fogSkybox = fogSkybox;
                tenkokuFog.fogColor = fogColor;
                tenkokuFog.heatSpd = heatSpd;
                tenkokuFog.heatScale = heatScale;
                tenkokuFog.heatDistance = heatDistance;

                Debug.Log($"[FogSetup] TENKOKU Fog применён: Height={height}, Density={heightDensity:F5}");
            }
            else if (tenkokuFog != null)
            {
                tenkokuFog.enabled = false;
                Debug.Log("[FogSetup] TENKOKU Fog отключён");
            }
        }
        else
        {
            Debug.LogWarning("[FogSetup] Main Camera не найдена! TENKOKU Fog не может быть настроен.");
        }

        EditorUtility.DisplayDialog("Fog Setup", "Настройки тумана успешно применены!", "OK");
    }

    private void ResetSettings()
    {
        if (EditorUtility.DisplayDialog("Сброс настроек", "Сбросить все настройки тумана к значениям по умолчанию?", "Да", "Отмена"))
        {
            unityFogEnabled = false;
            fogMode = FogMode.Exponential;
            fogDensity = 0.02f;
            fogStart = 10f;
            fogEnd = 100f;
            fogColor = new Color(0.5f, 0.5f, 0.5f);

            tenkokuFogEnabled = false;
            height = 185f;
            heightDensity = 0.00325f;
            useRadialDistance = true;
            fogHorizon = false;
            fogSkybox = true;

            currentPreset = FogPreset.Custom;

            ApplySettings();
            Debug.Log("[FogSetup] Настройки тумана сброшены");
        }
    }

    private void DisableAllFog()
    {
        if (EditorUtility.DisplayDialog("Отключить туман", "Полностью отключить весь туман в сцене?", "Да", "Отмена"))
        {
            RenderSettings.fog = false;

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                TenkokuSkyFog tenkokuFog = mainCam.GetComponent<TenkokuSkyFog>();
                if (tenkokuFog != null)
                {
                    tenkokuFog.enabled = false;
                }
            }

            unityFogEnabled = false;
            tenkokuFogEnabled = false;

            Debug.Log("[FogSetup] Весь туман отключён");
            EditorUtility.DisplayDialog("Fog Setup", "Весь туман отключён", "OK");
        }
    }
}
