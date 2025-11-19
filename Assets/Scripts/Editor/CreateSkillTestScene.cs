using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility для создания тестовой сцены для проверки атак и скиллов
/// </summary>
public class CreateSkillTestScene : EditorWindow
{
    private string sceneName = "SkillTestScene";
    private int dummyEnemyCount = 5;
    private float enemySpacing = 3f;

    [MenuItem("Aetherion/Create Skill Test Scene")]
    public static void ShowWindow()
    {
        GetWindow<CreateSkillTestScene>("Skill Test Scene Creator");
    }

    void OnGUI()
    {
        GUILayout.Label("Создание тестовой сцены", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        sceneName = EditorGUILayout.TextField("Название сцены:", sceneName);
        dummyEnemyCount = EditorGUILayout.IntSlider("Количество врагов:", dummyEnemyCount, 1, 10);
        enemySpacing = EditorGUILayout.Slider("Расстояние между врагами:", enemySpacing, 2f, 5f);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Создать тестовую сцену", GUILayout.Height(40)))
        {
            CreateTestScene();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Эта утилита создаст:\n" +
            "• Новую сцену для тестирования\n" +
            "• Арену с землей\n" +
            "• Точку спавна игрока\n" +
            "• Dummy-врагов для тестирования атак\n" +
            "• Камеру и освещение\n" +
            "• UI для тестирования",
            MessageType.Info
        );
    }

    void CreateTestScene()
    {
        // Создаём новую сцену
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ═══════════════════════════════════════════
        // 1. ЗЕМЛЯ (ARENA FLOOR)
        // ═══════════════════════════════════════════
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "ArenaFloor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(5f, 1f, 5f); // 50x50 метров

        // Материал для земли
        Material floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        floorMaterial.color = new Color(0.3f, 0.3f, 0.3f);
        floor.GetComponent<Renderer>().material = floorMaterial;

        Debug.Log("[SkillTestScene] ✅ Создана арена");

        // ═══════════════════════════════════════════
        // 2. ТОЧКА СПАВНА ИГРОКА
        // ═══════════════════════════════════════════
        GameObject playerSpawn = new GameObject("PlayerSpawnPoint");
        playerSpawn.transform.position = new Vector3(0f, 0.1f, -10f);
        playerSpawn.tag = "Respawn"; // Unity стандартный тег для спавна

        // Добавляем визуальный маркер
        GameObject spawnMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spawnMarker.name = "SpawnMarker";
        spawnMarker.transform.SetParent(playerSpawn.transform);
        spawnMarker.transform.localPosition = Vector3.zero;
        spawnMarker.transform.localScale = new Vector3(1f, 0.01f, 1f);
        Material spawnMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        spawnMaterial.color = Color.green;
        spawnMarker.GetComponent<Renderer>().material = spawnMaterial;
        DestroyImmediate(spawnMarker.GetComponent<Collider>()); // Убираем коллайдер

        Debug.Log("[SkillTestScene] ✅ Создан spawn point игрока");

        // ═══════════════════════════════════════════
        // 3. DUMMY ВРАГИ
        // ═══════════════════════════════════════════
        GameObject enemiesParent = new GameObject("DummyEnemies");
        enemiesParent.transform.position = Vector3.zero;

        float startX = -(dummyEnemyCount - 1) * enemySpacing * 0.5f;

        for (int i = 0; i < dummyEnemyCount; i++)
        {
            GameObject dummy = CreateDummyEnemy(i + 1);
            dummy.transform.SetParent(enemiesParent.transform);
            dummy.transform.position = new Vector3(startX + i * enemySpacing, 0.1f, 5f);
        }

        Debug.Log($"[SkillTestScene] ✅ Создано {dummyEnemyCount} dummy врагов");

        // ═══════════════════════════════════════════
        // 4. КАМЕРА (настраиваем существующую Main Camera)
        // ═══════════════════════════════════════════
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.transform.position = new Vector3(0f, 15f, -15f);
            mainCamera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            mainCamera.farClipPlane = 100f;

            Debug.Log("[SkillTestScene] ✅ Настроена камера");
        }

        // ═══════════════════════════════════════════
        // 5. ОСВЕЩЕНИЕ (настраиваем существующий Directional Light)
        // ═══════════════════════════════════════════
        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                light.intensity = 1.5f;
                light.shadows = LightShadows.Soft;
                Debug.Log("[SkillTestScene] ✅ Настроено освещение");
                break;
            }
        }

        // ═══════════════════════════════════════════
        // 6. UI ДЛЯ ТЕСТИРОВАНИЯ
        // ═══════════════════════════════════════════
        CreateTestUI();

        // ═══════════════════════════════════════════
        // СОХРАНЕНИЕ СЦЕНЫ
        // ═══════════════════════════════════════════
        string scenePath = $"Assets/Scenes/{sceneName}.unity";
        bool saved = EditorSceneManager.SaveScene(newScene, scenePath);

        if (saved)
        {
            Debug.Log($"[SkillTestScene] ✅ Сцена сохранена: {scenePath}");
            EditorUtility.DisplayDialog(
                "Тестовая сцена создана",
                $"Сцена '{sceneName}' успешно создана!\n\n" +
                $"Путь: {scenePath}\n\n" +
                $"Создано:\n" +
                $"• Арена 50x50м\n" +
                $"• Точка спавна игрока\n" +
                $"• {dummyEnemyCount} dummy врагов\n" +
                $"• Камера и освещение\n" +
                $"• UI для тестирования",
                "OK"
            );
        }
        else
        {
            Debug.LogError("[SkillTestScene] ❌ Не удалось сохранить сцену!");
            EditorUtility.DisplayDialog("Ошибка", "Не удалось сохранить сцену!", "OK");
        }
    }

    /// <summary>
    /// Создать dummy врага (простой столбик)
    /// </summary>
    GameObject CreateDummyEnemy(int index)
    {
        GameObject dummy = new GameObject($"DummyEnemy_{index}");

        // Визуальный столбик
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "Visual";
        visual.transform.SetParent(dummy.transform);
        visual.transform.localPosition = new Vector3(0f, 1f, 0f);
        visual.transform.localScale = new Vector3(1f, 1f, 1f);

        // Материал
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = new Color(0.8f, 0.2f, 0.2f); // Красный
        visual.GetComponent<Renderer>().material = material;

        // Capsule Collider для попаданий
        CapsuleCollider collider = dummy.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1f, 0f);
        collider.radius = 0.5f;
        collider.height = 2f;

        // Тег Enemy
        dummy.tag = "Enemy";
        dummy.layer = LayerMask.NameToLayer("Default");

        return dummy;
    }

    /// <summary>
    /// Создать UI для тестирования
    /// </summary>
    void CreateTestUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("TestUI");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Panel для инструкций
        GameObject panel = new GameObject("InstructionsPanel");
        panel.transform.SetParent(canvasObj.transform);
        UnityEngine.UI.Image panelImage = panel.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.7f);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(10f, -10f);
        panelRect.sizeDelta = new Vector2(400f, 200f);

        // Текст инструкций
        GameObject textObj = new GameObject("InstructionsText");
        textObj.transform.SetParent(panel.transform);
        UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = "SKILL TEST SCENE\n\n" +
                    "1. Перетащите префаб игрока на PlayerSpawnPoint\n" +
                    "2. Назначьте BasicAttackConfig в PlayerAttack\n" +
                    "3. Нажмите Play\n" +
                    "4. Атакуйте dummy врагов для теста\n\n" +
                    "Dummy враги: простые столбики с HP\n" +
                    "Используйте Console для логов урона";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 10f);
        textRect.offsetMax = new Vector2(-10f, -10f);

        Debug.Log("[SkillTestScene] ✅ Создан UI для тестирования");
    }
}
