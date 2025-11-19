using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// Автоматическая настройка WorldMap - создаёт всё за один клик!
/// </summary>
public class AutoSetupWorldMap : EditorWindow
{
    [MenuItem("Aetherion/Auto Setup World Map (One Click!)")]
    public static void AutoSetup()
    {
        if (!EditorUtility.DisplayDialog("Автоматическая настройка WorldMap",
            "Этот скрипт автоматически создаст:\n\n" +
            "1. LocationData для BattleScene\n" +
            "2. LocationMarker prefab\n" +
            "3. Настроит WorldMapManager\n" +
            "4. Всё подключит автоматически\n\n" +
            "Продолжить?",
            "Да, создать!", "Отмена"))
        {
            return;
        }

        Debug.Log("[AutoSetupWorldMap] 🚀 Начинаю автоматическую настройку...");

        // 1. Создаём папки
        CreateFolders();

        // 2. Создаём LocationData
        LocationData battleLocation = CreateBattleLocationData();

        // 3. Создаём LocationMarker prefab
        GameObject markerPrefab = CreateLocationMarkerPrefab();

        // 4. Открываем WorldMapScene
        OpenWorldMapScene();

        // 5. Настраиваем WorldMapManager
        SetupWorldMapManager(battleLocation, markerPrefab);

        // 6. Добавляем BattleScene в Build Settings
        AddSceneToBuildSettings("Assets/Scenes/BattleScene.unity");

        Debug.Log("[AutoSetupWorldMap] ✅✅✅ ГОТОВО! Всё настроено!");
        EditorUtility.DisplayDialog("Готово!",
            "WorldMap полностью настроен!\n\n" +
            "Теперь:\n" +
            "1. Play Mode в WorldMapScene\n" +
            "2. Подойдите к зелёному кубу\n" +
            "3. Нажмите E\n" +
            "4. Загрузится BattleScene!\n\n" +
            "🎉 Всё работает!",
            "Отлично!");
    }

    private static void CreateFolders()
    {
        Debug.Log("[AutoSetupWorldMap] 📁 Создаю папки...");

        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");

        if (!AssetDatabase.IsValidFolder("Assets/Data/Locations"))
            AssetDatabase.CreateFolder("Assets/Data", "Locations");

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/WorldMap"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "WorldMap");

        AssetDatabase.Refresh();
        Debug.Log("[AutoSetupWorldMap] ✅ Папки созданы");
    }

    private static LocationData CreateBattleLocationData()
    {
        Debug.Log("[AutoSetupWorldMap] 📋 Создаю LocationData...");

        string path = "Assets/Data/Locations/BattleLocation.asset";

        // Проверяем существует ли уже
        LocationData existing = AssetDatabase.LoadAssetAtPath<LocationData>(path);
        if (existing != null)
        {
            Debug.Log("[AutoSetupWorldMap] ⚠️ BattleLocation уже существует, использую его");
            return existing;
        }

        // Создаём новый
        LocationData locationData = ScriptableObject.CreateInstance<LocationData>();

        locationData.locationName = "Боевая Арена";
        locationData.description = "Место для сражений и тренировок. Здесь вы можете проверить свои навыки в бою.";
        locationData.sceneName = "BattleScene";
        locationData.mapPosition = new Vector2(0.5f, 0.5f); // Центр карты
        locationData.iconColor = Color.green;
        locationData.unlockedByDefault = true; // ВАЖНО!
        locationData.requiredLevel = 1;
        locationData.difficultyLevel = 1;
        locationData.recommendedLevel = 1;
        locationData.locationType = LocationType.City;
        locationData.fastTravelEnabled = true;

        AssetDatabase.CreateAsset(locationData, path);
        AssetDatabase.SaveAssets();

        Debug.Log("[AutoSetupWorldMap] ✅ LocationData создан: " + path);
        return locationData;
    }

    private static GameObject CreateLocationMarkerPrefab()
    {
        Debug.Log("[AutoSetupWorldMap] 🎨 Создаю LocationMarker prefab...");

        string prefabPath = "Assets/Prefabs/WorldMap/LocationMarker_Battle.prefab";

        // Проверяем существует ли уже
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existingPrefab != null)
        {
            Debug.Log("[AutoSetupWorldMap] ⚠️ LocationMarker prefab уже существует, использую его");
            return existingPrefab;
        }

        // Создаём GameObject
        GameObject marker = new GameObject("LocationMarker_Battle");

        // Добавляем SphereCollider
        SphereCollider collider = marker.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 3f;

        // Добавляем WorldMapLocationMarker
        WorldMapLocationMarker markerComponent = marker.AddComponent<WorldMapLocationMarker>();

        // Создаём визуальную иконку (куб)
        GameObject icon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        icon.name = "Icon";
        icon.transform.SetParent(marker.transform);
        icon.transform.localPosition = Vector3.zero;
        icon.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        // Удаляем collider с куба (он не нужен, есть на родителе)
        DestroyImmediate(icon.GetComponent<BoxCollider>());

        // Настраиваем материал
        Renderer iconRenderer = icon.GetComponent<Renderer>();
        if (iconRenderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.green;
            iconRenderer.material = mat;
        }

        // Создаём эффект подсветки
        GameObject highlight = new GameObject("Highlight");
        highlight.transform.SetParent(marker.transform);
        highlight.transform.localPosition = Vector3.zero;

        GameObject highlightSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        highlightSphere.transform.SetParent(highlight.transform);
        highlightSphere.transform.localPosition = Vector3.zero;
        highlightSphere.transform.localScale = Vector3.one * 1.5f;

        // Удаляем collider
        DestroyImmediate(highlightSphere.GetComponent<SphereCollider>());

        // Полупрозрачный материал
        Renderer highlightRenderer = highlightSphere.GetComponent<Renderer>();
        if (highlightRenderer != null)
        {
            Material highlightMat = new Material(Shader.Find("Standard"));
            highlightMat.color = new Color(0, 1, 0, 0.3f);
            highlightMat.SetFloat("_Mode", 3); // Transparent
            highlightMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            highlightMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            highlightMat.SetInt("_ZWrite", 0);
            highlightMat.DisableKeyword("_ALPHATEST_ON");
            highlightMat.EnableKeyword("_ALPHABLEND_ON");
            highlightMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            highlightMat.renderQueue = 3000;
            highlightRenderer.material = highlightMat;
        }

        highlight.SetActive(false);

        // Настраиваем компонент через рефлексию
        var iconObjectField = typeof(WorldMapLocationMarker).GetField("iconObject",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (iconObjectField != null)
            iconObjectField.SetValue(markerComponent, icon);

        var highlightEffectField = typeof(WorldMapLocationMarker).GetField("highlightEffect",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (highlightEffectField != null)
            highlightEffectField.SetValue(markerComponent, highlight);

        // Сохраняем как prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(marker, prefabPath);

        // Удаляем временный объект
        DestroyImmediate(marker);

        Debug.Log("[AutoSetupWorldMap] ✅ LocationMarker prefab создан: " + prefabPath);
        return prefab;
    }

    private static void OpenWorldMapScene()
    {
        Debug.Log("[AutoSetupWorldMap] 🗺️ Открываю WorldMapScene...");

        string scenePath = "Assets/Scenes/WorldMapScene.unity";

        if (!File.Exists(scenePath))
        {
            Debug.LogError("[AutoSetupWorldMap] ❌ WorldMapScene не найдена по пути: " + scenePath);
            EditorUtility.DisplayDialog("Ошибка",
                "WorldMapScene не найдена!\n\nОжидаемый путь: Assets/Scenes/WorldMapScene.unity\n\n" +
                "Создайте сцену вручную или укажите правильный путь.",
                "OK");
            return;
        }

        EditorSceneManager.OpenScene(scenePath);
        Debug.Log("[AutoSetupWorldMap] ✅ WorldMapScene открыта");
    }

    private static void SetupWorldMapManager(LocationData locationData, GameObject markerPrefab)
    {
        Debug.Log("[AutoSetupWorldMap] ⚙️ Настраиваю WorldMapManager...");

        WorldMapManager manager = FindObjectOfType<WorldMapManager>();

        if (manager == null)
        {
            Debug.LogError("[AutoSetupWorldMap] ❌ WorldMapManager не найден в сцене!");
            EditorUtility.DisplayDialog("Ошибка",
                "WorldMapManager не найден в WorldMapScene!\n\n" +
                "Создайте GameObject с компонентом WorldMapManager.",
                "OK");
            return;
        }

        // Используем рефлексию для установки приватных полей
        var managerType = typeof(WorldMapManager);

        // Устанавливаем allLocations
        var allLocationsField = managerType.GetField("allLocations",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (allLocationsField != null)
        {
            var locationsList = new System.Collections.Generic.List<LocationData> { locationData };
            allLocationsField.SetValue(manager, locationsList);
            Debug.Log("[AutoSetupWorldMap] ✅ LocationData добавлен в WorldMapManager");
        }

        // Устанавливаем locationMarkerPrefab
        var markerPrefabField = managerType.GetField("locationMarkerPrefab",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (markerPrefabField != null)
        {
            markerPrefabField.SetValue(manager, markerPrefab);
            Debug.Log("[AutoSetupWorldMap] ✅ LocationMarker prefab назначен");
        }

        // Сохраняем изменения
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[AutoSetupWorldMap] ✅ WorldMapManager настроен и сохранён");
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        Debug.Log("[AutoSetupWorldMap] 🔧 Добавляю сцену в Build Settings...");

        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        // Проверяем есть ли уже
        bool alreadyExists = false;
        foreach (var scene in scenes)
        {
            if (scene.path == scenePath)
            {
                alreadyExists = true;
                break;
            }
        }

        if (!alreadyExists)
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[AutoSetupWorldMap] ✅ BattleScene добавлена в Build Settings");
        }
        else
        {
            Debug.Log("[AutoSetupWorldMap] ⚠️ BattleScene уже в Build Settings");
        }
    }
}
