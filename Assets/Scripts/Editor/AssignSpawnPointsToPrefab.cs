using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Автоматически назначает Spawn Points из сцены в NetworkSyncManager.prefab
/// </summary>
public class AssignSpawnPointsToPrefab : EditorWindow
{
    private string prefabPath = "Assets/Prefabs/Network/NetworkSyncManager.prefab";
    private string spawnPointsParentName = "SpawnPoints";
    private int expectedCount = 20;

    [MenuItem("Tools/Aetherion/Assign Spawn Points to Prefab")]
    static void ShowWindow()
    {
        GetWindow<AssignSpawnPointsToPrefab>("Assign Spawn Points");
    }

    void OnGUI()
    {
        GUILayout.Label("Автоматическое назначение Spawn Points", EditorStyles.boldLabel);
        GUILayout.Space(10);

        prefabPath = EditorGUILayout.TextField("Путь к префабу:", prefabPath);
        spawnPointsParentName = EditorGUILayout.TextField("Имя родителя точек:", spawnPointsParentName);
        expectedCount = EditorGUILayout.IntField("Ожидаемое количество:", expectedCount);

        GUILayout.Space(10);

        if (GUILayout.Button("🚀 Назначить Spawn Points в Prefab", GUILayout.Height(40)))
        {
            AssignSpawnPoints();
        }

        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Этот инструмент автоматически назначит Spawn Points из текущей сцены " +
            "в префаб NetworkSyncManager.\n\n" +
            "Убедитесь что:\n" +
            "• Вы в сцене BattleScene\n" +
            "• Объект SpawnPoints существует в Hierarchy\n" +
            "• Внутри него есть 20 точек: SpawnPoint_00...19",
            MessageType.Info
        );

        GUILayout.Space(10);

        if (GUILayout.Button("📋 Проверить текущую сцену", GUILayout.Height(30)))
        {
            CheckCurrentScene();
        }
    }

    void AssignSpawnPoints()
    {
        // Шаг 1: Проверяем что мы в правильной сцене
        if (string.IsNullOrEmpty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name))
        {
            EditorUtility.DisplayDialog("Ошибка",
                "Сцена не загружена! Откройте BattleScene сначала.", "OK");
            return;
        }

        Debug.Log("[AssignSpawnPoints] 🔍 Шаг 1: Ищем SpawnPoints в сцене...");

        // Шаг 2: Находим родительский объект SpawnPoints
        GameObject spawnPointsParent = GameObject.Find(spawnPointsParentName);
        if (spawnPointsParent == null)
        {
            EditorUtility.DisplayDialog("Ошибка",
                $"Объект '{spawnPointsParentName}' не найден в сцене!\n\n" +
                "Создайте его сначала:\n" +
                "Tools → Aetherion → Create Spawn Points", "OK");
            return;
        }

        Debug.Log($"[AssignSpawnPoints] ✅ Найден объект: {spawnPointsParent.name}");

        // Шаг 3: Собираем все дочерние SpawnPoints
        Transform[] spawnPoints = new Transform[expectedCount];
        int foundCount = 0;

        for (int i = 0; i < expectedCount; i++)
        {
            string spawnPointName = $"SpawnPoint_{i:D2}";
            Transform spawnPoint = spawnPointsParent.transform.Find(spawnPointName);

            if (spawnPoint == null)
            {
                Debug.LogError($"[AssignSpawnPoints] ❌ {spawnPointName} не найден!");
                EditorUtility.DisplayDialog("Ошибка",
                    $"{spawnPointName} не найден в объекте '{spawnPointsParentName}'!\n\n" +
                    $"Найдено только {foundCount}/{expectedCount} точек.\n" +
                    "Создайте недостающие точки.", "OK");
                return;
            }

            spawnPoints[i] = spawnPoint;
            foundCount++;
        }

        Debug.Log($"[AssignSpawnPoints] ✅ Найдено {foundCount}/{expectedCount} точек спавна");

        // Шаг 4: Загружаем префаб NetworkSyncManager
        Debug.Log($"[AssignSpawnPoints] 🔍 Шаг 2: Загружаем префаб из {prefabPath}...");

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Ошибка",
                $"Префаб не найден: {prefabPath}\n\n" +
                "Создайте префаб NetworkSyncManager сначала!", "OK");
            return;
        }

        Debug.Log($"[AssignSpawnPoints] ✅ Префаб загружен: {prefab.name}");

        // Шаг 5: Получаем компонент NetworkSyncManager
        NetworkSyncManager syncManager = prefab.GetComponent<NetworkSyncManager>();
        if (syncManager == null)
        {
            EditorUtility.DisplayDialog("Ошибка",
                "На префабе нет компонента NetworkSyncManager!\n\n" +
                "Добавьте компонент на префаб.", "OK");
            return;
        }

        Debug.Log("[AssignSpawnPoints] ✅ Найден компонент NetworkSyncManager");

        // Шаг 6: Используем SerializedObject для изменения префаба
        Debug.Log("[AssignSpawnPoints] 🔍 Шаг 3: Назначаем Spawn Points в префаб...");

        SerializedObject serializedPrefab = new SerializedObject(syncManager);
        SerializedProperty spawnPointsProperty = serializedPrefab.FindProperty("spawnPoints");

        if (spawnPointsProperty == null)
        {
            EditorUtility.DisplayDialog("Ошибка",
                "Поле 'spawnPoints' не найдено в NetworkSyncManager!\n\n" +
                "Проверьте что в скрипте есть:\n" +
                "[SerializeField] private Transform[] spawnPoints;", "OK");
            return;
        }

        // Устанавливаем размер массива
        spawnPointsProperty.arraySize = expectedCount;
        Debug.Log($"[AssignSpawnPoints] ✅ Размер массива установлен: {expectedCount}");

        // Назначаем каждый SpawnPoint
        for (int i = 0; i < expectedCount; i++)
        {
            spawnPointsProperty.GetArrayElementAtIndex(i).objectReferenceValue = spawnPoints[i];
            Debug.Log($"[AssignSpawnPoints]   • Element {i} = {spawnPoints[i].name}");
        }

        // Применяем изменения
        serializedPrefab.ApplyModifiedProperties();
        Debug.Log("[AssignSpawnPoints] ✅ Изменения применены к SerializedObject");

        // Шаг 7: Сохраняем префаб
        PrefabUtility.SavePrefabAsset(prefab);
        Debug.Log("[AssignSpawnPoints] ✅ Префаб сохранён");

        // Шаг 8: Также назначаем в BattleSceneManager (если есть)
        AssignToBattleSceneManager(spawnPoints);

        // Финальное сообщение
        Debug.Log("[AssignSpawnPoints] 🎉 ВСЁ ГОТОВО!");

        EditorUtility.DisplayDialog("Успех!",
            $"✅ Все {expectedCount} Spawn Points назначены!\n\n" +
            "Назначено в:\n" +
            $"• {prefabPath}\n" +
            "• BattleSceneManager (в сцене)\n\n" +
            "Откройте префаб чтобы убедиться.", "OK");
    }

    void AssignToBattleSceneManager(Transform[] spawnPoints)
    {
        Debug.Log("[AssignSpawnPoints] 🔍 Шаг 4: Назначаем в BattleSceneManager...");

        BattleSceneManager battleManager = FindObjectOfType<BattleSceneManager>();
        if (battleManager == null)
        {
            Debug.LogWarning("[AssignSpawnPoints] ⚠️ BattleSceneManager не найден в сцене");
            return;
        }

        SerializedObject serializedManager = new SerializedObject(battleManager);
        SerializedProperty spawnPointsProperty = serializedManager.FindProperty("spawnPoints");

        if (spawnPointsProperty == null)
        {
            Debug.LogWarning("[AssignSpawnPoints] ⚠️ Поле 'spawnPoints' не найдено в BattleSceneManager");
            return;
        }

        spawnPointsProperty.arraySize = spawnPoints.Length;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            spawnPointsProperty.GetArrayElementAtIndex(i).objectReferenceValue = spawnPoints[i];
        }

        serializedManager.ApplyModifiedProperties();

        Debug.Log($"[AssignSpawnPoints] ✅ Назначено {spawnPoints.Length} точек в BattleSceneManager");
    }

    void CheckCurrentScene()
    {
        var sceneName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;

        if (string.IsNullOrEmpty(sceneName))
        {
            EditorUtility.DisplayDialog("Проверка сцены",
                "❌ Сцена не загружена!", "OK");
            return;
        }

        // Проверяем SpawnPoints
        GameObject spawnPointsParent = GameObject.Find(spawnPointsParentName);
        if (spawnPointsParent == null)
        {
            EditorUtility.DisplayDialog("Проверка сцены",
                $"❌ Объект '{spawnPointsParentName}' не найден!\n\n" +
                "Создайте его:\n" +
                "Tools → Aetherion → Create Spawn Points", "OK");
            return;
        }

        // Считаем дочерние точки
        int childCount = spawnPointsParent.transform.childCount;
        int validCount = 0;

        for (int i = 0; i < expectedCount; i++)
        {
            string spawnPointName = $"SpawnPoint_{i:D2}";
            Transform spawnPoint = spawnPointsParent.transform.Find(spawnPointName);
            if (spawnPoint != null)
                validCount++;
        }

        // Проверяем BattleSceneManager
        BattleSceneManager battleManager = FindObjectOfType<BattleSceneManager>();
        string battleManagerStatus = battleManager != null ? "✅ Найден" : "❌ Не найден";

        // Проверяем префаб
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        string prefabStatus = prefab != null ? "✅ Найден" : "❌ Не найден";

        EditorUtility.DisplayDialog("Проверка сцены",
            $"Сцена: {sceneName}\n\n" +
            $"SpawnPoints: {(spawnPointsParent != null ? "✅ Найден" : "❌ Не найден")}\n" +
            $"Дочерних объектов: {childCount}\n" +
            $"Правильных точек (00-19): {validCount}/{expectedCount}\n\n" +
            $"BattleSceneManager: {battleManagerStatus}\n" +
            $"NetworkSyncManager.prefab: {prefabStatus}\n\n" +
            (validCount == expectedCount && battleManager != null && prefab != null ?
                "✅ Всё готово для назначения!" :
                "⚠️ Не все компоненты готовы"),
            "OK");
    }
}
