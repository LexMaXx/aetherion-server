#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// Автоматическая настройка BattleScene
/// Добавляет BattleSceneManager и создает spawn points
/// </summary>
public class SetupBattleScene : EditorWindow
{
    [MenuItem("Aetherion/Setup BattleScene")]
    public static void SetupScene()
    {
        Debug.Log("[SetupBattleScene] Начинаем настройку BattleScene...");

        // Открываем BattleScene
        string scenePath = "Assets/Scenes/BattleScene.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // 1. Проверяем есть ли уже BattleSceneManager
        BattleSceneManager existingManager = GameObject.FindObjectOfType<BattleSceneManager>();

        if (existingManager != null)
        {
            Debug.Log("[SetupBattleScene] ✅ BattleSceneManager уже существует");
        }
        else
        {
            // Создаём новый GameObject с BattleSceneManager
            GameObject managerObj = new GameObject("BattleSceneManager");
            BattleSceneManager manager = managerObj.AddComponent<BattleSceneManager>();

            Debug.Log("[SetupBattleScene] ✅ Создан BattleSceneManager");

            // Настраиваем компоненты через SerializedObject
            SerializedObject so = new SerializedObject(manager);

            // Ищем Main Camera
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                SerializedProperty cameraProp = so.FindProperty("battleCamera");
                if (cameraProp != null)
                {
                    cameraProp.objectReferenceValue = mainCamera;
                    Debug.Log("[SetupBattleScene] ✅ Назначена Main Camera");
                }
            }

            so.ApplyModifiedProperties();
        }

        // 2. Создаём PlayerSpawnPoint если его нет
        GameObject spawnPoint = GameObject.Find("PlayerSpawnPoint");
        if (spawnPoint == null)
        {
            spawnPoint = new GameObject("PlayerSpawnPoint");
            spawnPoint.transform.position = Vector3.zero;
            spawnPoint.transform.rotation = Quaternion.identity;

            Debug.Log("[SetupBattleScene] ✅ Создан PlayerSpawnPoint");
        }

        // 3. Создаём массив spawn points для мультиплеера
        GameObject spawnPointsParent = GameObject.Find("MultiplayerSpawnPoints");
        if (spawnPointsParent == null)
        {
            spawnPointsParent = new GameObject("MultiplayerSpawnPoints");

            // Создаём 10 точек спавна по кругу
            int spawnPointCount = 10;
            float radius = 10f;

            for (int i = 0; i < spawnPointCount; i++)
            {
                GameObject sp = new GameObject($"SpawnPoint_{i}");
                sp.transform.parent = spawnPointsParent.transform;

                // Располагаем по кругу
                float angle = (360f / spawnPointCount) * i;
                float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
                float z = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

                sp.transform.position = new Vector3(x, 0, z);
                sp.transform.rotation = Quaternion.Euler(0, -angle, 0);
            }

            Debug.Log($"[SetupBattleScene] ✅ Создано {spawnPointCount} spawn points");
        }

        // 4. Назначаем spawn points в BattleSceneManager
        BattleSceneManager battleManager = GameObject.FindObjectOfType<BattleSceneManager>();
        if (battleManager != null && spawnPointsParent != null)
        {
            SerializedObject so = new SerializedObject(battleManager);
            SerializedProperty spawnPointsProp = so.FindProperty("spawnPoints");

            if (spawnPointsProp != null && spawnPointsProp.isArray)
            {
                // Очищаем массив
                spawnPointsProp.ClearArray();

                // Добавляем все spawn points
                Transform[] spawnTransforms = spawnPointsParent.GetComponentsInChildren<Transform>();

                int count = 0;
                foreach (Transform t in spawnTransforms)
                {
                    if (t != spawnPointsParent.transform) // Пропускаем родителя
                    {
                        spawnPointsProp.InsertArrayElementAtIndex(count);
                        spawnPointsProp.GetArrayElementAtIndex(count).objectReferenceValue = t;
                        count++;
                    }
                }

                so.ApplyModifiedProperties();
                Debug.Log($"[SetupBattleScene] ✅ Назначено {count} spawn points в BattleSceneManager");
            }
        }

        // 5. Удаляем TestPlayer если он есть
        GameObject testPlayer = GameObject.Find("TestPlayer");
        if (testPlayer != null)
        {
            GameObject.DestroyImmediate(testPlayer);
            Debug.Log("[SetupBattleScene] ✅ Удалён TestPlayer");
        }

        // Сохраняем сцену
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SetupBattleScene] ✅✅✅ BattleScene настроена!");

        EditorUtility.DisplayDialog(
            "Готово!",
            "BattleScene настроена успешно!\n\n" +
            "✅ BattleSceneManager добавлен\n" +
            "✅ Main Camera назначена\n" +
            "✅ Spawn points созданы (10 шт)\n" +
            "✅ TestPlayer удалён\n\n" +
            "Теперь можно тестировать загрузку персонажа!",
            "OK"
        );
    }
}
#endif
