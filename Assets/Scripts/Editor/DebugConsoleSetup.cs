using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to quickly add DebugConsole to current scene
/// </summary>
public class DebugConsoleSetup : MonoBehaviour
{
    [MenuItem("Tools/Debug/Add Debug Console to Scene")]
    static void AddDebugConsoleToScene()
    {
        // Check if already exists
        DebugConsole existing = FindObjectOfType<DebugConsole>();
        if (existing != null)
        {
            Debug.LogWarning("[DebugConsoleSetup] DebugConsole already exists in scene!");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // Create GameObject
        GameObject consoleObj = new GameObject("DebugConsole");
        DebugConsole console = consoleObj.AddComponent<DebugConsole>();

        // Add test script
        consoleObj.AddComponent<DebugConsoleTest>();

        // Select it
        Selection.activeGameObject = consoleObj;

        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("[DebugConsoleSetup] ✅ DebugConsole добавлен в сцену!");
        Debug.Log("[DebugConsoleSetup] 📋 Нажмите Play и затем:");
        Debug.Log("[DebugConsoleSetup]    - F12 для открытия консоли");
        Debug.Log("[DebugConsoleSetup]    - F11 для открытия консоли");
        Debug.Log("[DebugConsoleSetup]    - ` (backtick) для открытия консоли");
        Debug.Log("[DebugConsoleSetup]    - H для помощи");
        Debug.Log("═══════════════════════════════════════════");
    }

    [MenuItem("Tools/Debug/Create DebugConsole Prefab")]
    static void CreateDebugConsolePrefab()
    {
        // Create prefab folder if not exists
        string folderPath = "Assets/Prefabs/Debug";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Debug");
        }

        // Create GameObject
        GameObject consoleObj = new GameObject("DebugConsole");
        DebugConsole console = consoleObj.AddComponent<DebugConsole>();

        // Save as prefab
        string prefabPath = folderPath + "/DebugConsole.prefab";
        PrefabUtility.SaveAsPrefabAsset(consoleObj, prefabPath);

        // Destroy temp object
        DestroyImmediate(consoleObj);

        Debug.Log($"[DebugConsoleSetup] ✅ Prefab created at {prefabPath}");

        // Select the prefab
        Object prefab = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);
        Selection.activeObject = prefab;
    }

    [MenuItem("Tools/Debug/Remove Debug Console from Scene")]
    static void RemoveDebugConsoleFromScene()
    {
        DebugConsole existing = FindObjectOfType<DebugConsole>();
        if (existing == null)
        {
            Debug.LogWarning("[DebugConsoleSetup] No DebugConsole found in scene!");
            return;
        }

        DestroyImmediate(existing.gameObject);
        Debug.Log("[DebugConsoleSetup] ✅ DebugConsole removed from scene");
    }
}
