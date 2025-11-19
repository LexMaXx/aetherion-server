using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

/// <summary>
/// Автоматическая настройка NetworkManagers в GameScene
/// Tools → Aetherion → Setup Multiplayer Managers
/// </summary>
public class SetupMultiplayerManagers : EditorWindow
{
    private string serverUrl = "https://aetherion-server-gv5u.onrender.com";

    [MenuItem("Tools/Aetherion/Setup Multiplayer Managers")]
    public static void ShowWindow()
    {
        GetWindow<SetupMultiplayerManagers>("Setup Multiplayer");
    }

    void OnGUI()
    {
        GUILayout.Label("Setup Multiplayer Managers", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Этот скрипт автоматически создаст и настроит NetworkManagers в GameScene.\n\n" +
            "1. WebSocketClient - для WebSocket соединения\n" +
            "2. RoomManager - для управления комнатами\n\n" +
            "Оба будут иметь DontDestroyOnLoad.",
            MessageType.Info
        );

        GUILayout.Space(10);

        // Server URL field
        GUILayout.Label("Server URL:");
        serverUrl = EditorGUILayout.TextField(serverUrl);

        GUILayout.Space(10);

        // Current scene info
        Scene currentScene = SceneManager.GetActiveScene();
        EditorGUILayout.LabelField("Current Scene:", currentScene.name);

        GUILayout.Space(10);

        // Setup button
        if (GUILayout.Button("Setup NetworkManagers", GUILayout.Height(40)))
        {
            SetupManagers();
        }

        GUILayout.Space(10);

        // Remove button
        if (GUILayout.Button("Remove NetworkManagers", GUILayout.Height(30)))
        {
            RemoveManagers();
        }
    }

    private void SetupManagers()
    {
        // Check if we're in GameScene
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.name != "GameScene" && currentScene.name != "MainScene")
        {
            if (!EditorUtility.DisplayDialog(
                "Warning",
                $"You are not in GameScene/MainScene (current: {currentScene.name}).\n\nNetworkManagers should be created in GameScene where the BattleButton is.\n\nContinue anyway?",
                "Yes, Continue",
                "Cancel"))
            {
                return;
            }
        }

        // Create parent object
        GameObject networkManagers = GameObject.Find("NetworkManagers");
        if (networkManagers == null)
        {
            networkManagers = new GameObject("NetworkManagers");
            Debug.Log("✓ Created NetworkManagers parent object");
        }

        // Setup WebSocketClient
        WebSocketClient wsClient = networkManagers.GetComponent<WebSocketClient>();
        if (wsClient == null)
        {
            wsClient = networkManagers.AddComponent<WebSocketClient>();
            Debug.Log("✓ Added WebSocketClient component");
        }

        // Set server URL via reflection (since it's private serialized field)
        SerializedObject wsClientSO = new SerializedObject(wsClient);
        SerializedProperty serverUrlProp = wsClientSO.FindProperty("serverUrl");
        if (serverUrlProp != null)
        {
            serverUrlProp.stringValue = serverUrl;
            wsClientSO.ApplyModifiedProperties();
            Debug.Log($"✓ Set WebSocketClient server URL: {serverUrl}");
        }

        // Setup RoomManager
        RoomManager roomManager = networkManagers.GetComponent<RoomManager>();
        if (roomManager == null)
        {
            roomManager = networkManagers.AddComponent<RoomManager>();
            Debug.Log("✓ Added RoomManager component");
        }

        // Set server URL for RoomManager
        SerializedObject roomManagerSO = new SerializedObject(roomManager);
        SerializedProperty roomServerUrlProp = roomManagerSO.FindProperty("serverUrl");
        if (roomServerUrlProp != null)
        {
            roomServerUrlProp.stringValue = serverUrl;
            roomManagerSO.ApplyModifiedProperties();
            Debug.Log($"✓ Set RoomManager server URL: {serverUrl}");
        }

        // Mark scene as dirty
        EditorUtility.SetDirty(networkManagers);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(currentScene);

        EditorUtility.DisplayDialog(
            "Success!",
            "NetworkManagers успешно созданы и настроены!\n\n" +
            "- WebSocketClient\n" +
            "- RoomManager\n\n" +
            $"Server URL: {serverUrl}\n\n" +
            "Не забудьте сохранить сцену (Ctrl+S)!",
            "OK"
        );

        Debug.Log("========================================");
        Debug.Log("✅ MULTIPLAYER SETUP COMPLETE!");
        Debug.Log($"Server URL: {serverUrl}");
        Debug.Log("Теперь сохраните сцену (Ctrl+S)");
        Debug.Log("========================================");
    }

    private void RemoveManagers()
    {
        GameObject networkManagers = GameObject.Find("NetworkManagers");
        if (networkManagers != null)
        {
            if (EditorUtility.DisplayDialog(
                "Confirm Removal",
                "Вы уверены что хотите удалить NetworkManagers?",
                "Yes, Remove",
                "Cancel"))
            {
                DestroyImmediate(networkManagers);
                Debug.Log("✓ NetworkManagers removed");

                Scene currentScene = SceneManager.GetActiveScene();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(currentScene);

                EditorUtility.DisplayDialog("Removed", "NetworkManagers удалены", "OK");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Not Found", "NetworkManagers не найдены в сцене", "OK");
        }
    }
}
