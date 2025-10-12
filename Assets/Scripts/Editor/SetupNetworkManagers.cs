using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor утилита для автоматической настройки сетевых менеджеров
/// </summary>
public class SetupNetworkManagers : EditorWindow
{
    [MenuItem("Aetherion/Setup/Auto Setup Network Managers")]
    public static void SetupManagers()
    {
        // Находим IntroScene или первую доступную сцену
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene IntroScene");

        if (sceneGuids.Length == 0)
        {
            // Если IntroScene не найдена, ищем любую сцену
            sceneGuids = AssetDatabase.FindAssets("t:Scene");
        }

        if (sceneGuids.Length == 0)
        {
            Debug.LogError("[Setup] Не найдено ни одной сцены!");
            return;
        }

        // Открываем первую найденную сцену
        string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[0]);
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        Debug.Log($"[Setup] Открыта сцена: {scene.name}");

        // Проверяем есть ли уже NetworkInitializer
        NetworkInitializer existing = FindObjectOfType<NetworkInitializer>();
        if (existing != null)
        {
            Debug.Log("[Setup] ✅ NetworkInitializer уже существует!");
            return;
        }

        // Создаём GameObject с NetworkInitializer
        GameObject networkInit = new GameObject("NetworkInitializer");
        networkInit.AddComponent<NetworkInitializer>();

        // Помечаем сцену как изменённую
        EditorSceneManager.MarkSceneDirty(scene);

        // Сохраняем сцену
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[Setup] ✅ NetworkInitializer добавлен в сцену!");
        Debug.Log("[Setup] ✅ Сцена сохранена!");

        // Показываем сообщение
        EditorUtility.DisplayDialog(
            "Setup Complete",
            "NetworkInitializer успешно добавлен!\n\nТеперь запусти игру (Play) и всё будет работать!",
            "OK"
        );
    }

    [MenuItem("Aetherion/Setup/Check Network Setup")]
    public static void CheckSetup()
    {
        bool hasInitializer = FindObjectOfType<NetworkInitializer>() != null;
        bool hasOptimizedWS = FindObjectOfType<OptimizedWebSocketClient>() != null;
        bool hasRoomManager = FindObjectOfType<RoomManager>() != null;

        string message = "=== Network Setup Status ===\n\n";
        message += hasInitializer ? "✅ NetworkInitializer\n" : "❌ NetworkInitializer НЕ найден\n";
        message += hasOptimizedWS ? "✅ OptimizedWebSocketClient\n" : "⚠️ OptimizedWebSocketClient (создастся автоматически)\n";
        message += hasRoomManager ? "✅ RoomManager\n" : "⚠️ RoomManager (создастся автоматически)\n";

        if (!hasInitializer)
        {
            message += "\n⚠️ ВНИМАНИЕ: NetworkInitializer не найден!\n";
            message += "Нажми 'Auto Setup Network Managers' в меню Aetherion → Setup";
        }
        else
        {
            message += "\n✅ Всё готово к запуску!";
        }

        EditorUtility.DisplayDialog("Network Setup Check", message, "OK");
    }
}
