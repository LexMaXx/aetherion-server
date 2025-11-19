using UnityEngine;
using UnityEditor;

/// <summary>
/// Проверяет и настраивает скорость движения всех персонажей
/// </summary>
public class CheckCharacterSpeeds : Editor
{
    [MenuItem("Tools/Aetherion/Check Character Speeds")]
    public static void CheckSpeeds()
    {
        Debug.Log("=== [CheckCharacterSpeeds] Проверка скорости персонажей ===");

        string[] prefabPaths = new string[]
        {
            "Assets/UI/Prefabs/WarriorModel.prefab",
            "Assets/UI/Prefabs/MageModel.prefab",
            "Assets/UI/Prefabs/ArcherModel.prefab",
            "Assets/UI/Prefabs/RogueModel.prefab",
            "Assets/UI/Prefabs/PaladinModel.prefab"
        };

        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogWarning($"[CheckCharacterSpeeds] ⚠️ Не найден: {path}");
                continue;
            }

            PlayerController controller = prefab.GetComponent<PlayerController>();

            if (controller == null)
            {
                Debug.LogWarning($"[CheckCharacterSpeeds] ⚠️ {prefab.name}: нет PlayerController");
                continue;
            }

            // Получаем значения через SerializedObject
            SerializedObject so = new SerializedObject(controller);

            SerializedProperty walkSpeedProp = so.FindProperty("walkSpeed");
            SerializedProperty runSpeedProp = so.FindProperty("runSpeed");
            SerializedProperty rotationSpeedProp = so.FindProperty("rotationSpeed");

            float walkSpeed = walkSpeedProp != null ? walkSpeedProp.floatValue : 0f;
            float runSpeed = runSpeedProp != null ? runSpeedProp.floatValue : 0f;
            float rotationSpeed = rotationSpeedProp != null ? rotationSpeedProp.floatValue : 0f;

            Debug.Log($"[CheckCharacterSpeeds] {prefab.name}:");
            Debug.Log($"  - Walk Speed: {walkSpeed}");
            Debug.Log($"  - Run Speed: {runSpeed}");
            Debug.Log($"  - Rotation Speed: {rotationSpeed}");
        }

        Debug.Log("=== [CheckCharacterSpeeds] Проверка завершена ===");
    }

    [MenuItem("Tools/Aetherion/Set All Character Speeds")]
    public static void SetAllSpeeds()
    {
        float walkSpeed = 3f;
        float runSpeed = 6f;
        float rotationSpeed = 10f;

        if (!EditorUtility.DisplayDialog(
            "Установка скорости персонажей",
            $"Установить для ВСЕХ персонажей:\n\n" +
            $"• Скорость ходьбы: {walkSpeed}\n" +
            $"• Скорость бега: {runSpeed}\n" +
            $"• Скорость поворота: {rotationSpeed}\n\n" +
            $"Продолжить?",
            "Да",
            "Отмена"))
        {
            return;
        }

        Debug.Log("[SetAllSpeeds] Устанавливаю одинаковую скорость для всех персонажей...");

        string[] prefabPaths = new string[]
        {
            "Assets/UI/Prefabs/WarriorModel.prefab",
            "Assets/UI/Prefabs/MageModel.prefab",
            "Assets/UI/Prefabs/ArcherModel.prefab",
            "Assets/UI/Prefabs/RogueModel.prefab",
            "Assets/UI/Prefabs/PaladinModel.prefab"
        };

        int updatedCount = 0;

        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            PlayerController controller = prefab.GetComponent<PlayerController>();
            if (controller == null) continue;

            SerializedObject so = new SerializedObject(controller);

            SerializedProperty walkSpeedProp = so.FindProperty("walkSpeed");
            SerializedProperty runSpeedProp = so.FindProperty("runSpeed");
            SerializedProperty rotationSpeedProp = so.FindProperty("rotationSpeed");

            if (walkSpeedProp != null) walkSpeedProp.floatValue = walkSpeed;
            if (runSpeedProp != null) runSpeedProp.floatValue = runSpeed;
            if (rotationSpeedProp != null) rotationSpeedProp.floatValue = rotationSpeed;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);

            Debug.Log($"[SetAllSpeeds] ✅ {prefab.name}: Walk={walkSpeed}, Run={runSpeed}");
            updatedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SetAllSpeeds] ✅ ГОТОВО! Обновлено персонажей: {updatedCount}");
    }

    [MenuItem("Tools/Aetherion/Set Custom Character Speeds")]
    public static void SetCustomSpeeds()
    {
        // Открываем окно для ввода кастомных значений
        CustomSpeedWindow.ShowWindow();
    }
}

/// <summary>
/// Окно для ввода кастомных значений скорости
/// </summary>
public class CustomSpeedWindow : EditorWindow
{
    private float walkSpeed = 3f;
    private float runSpeed = 6f;
    private float rotationSpeed = 10f;

    public static void ShowWindow()
    {
        GetWindow<CustomSpeedWindow>("Настройка скорости");
    }

    void OnGUI()
    {
        GUILayout.Label("Настройка скорости персонажей", EditorStyles.boldLabel);

        GUILayout.Space(10);

        walkSpeed = EditorGUILayout.FloatField("Скорость ходьбы:", walkSpeed);
        runSpeed = EditorGUILayout.FloatField("Скорость бега:", runSpeed);
        rotationSpeed = EditorGUILayout.FloatField("Скорость поворота:", rotationSpeed);

        GUILayout.Space(10);

        if (GUILayout.Button("Применить ко всем персонажам"))
        {
            ApplySpeeds();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Проверить текущие значения"))
        {
            CheckCharacterSpeeds.CheckSpeeds();
        }
    }

    private void ApplySpeeds()
    {
        Debug.Log($"[CustomSpeeds] Применяю: Walk={walkSpeed}, Run={runSpeed}, Rotation={rotationSpeed}");

        string[] prefabPaths = new string[]
        {
            "Assets/UI/Prefabs/WarriorModel.prefab",
            "Assets/UI/Prefabs/MageModel.prefab",
            "Assets/UI/Prefabs/ArcherModel.prefab",
            "Assets/UI/Prefabs/RogueModel.prefab",
            "Assets/UI/Prefabs/PaladinModel.prefab"
        };

        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            PlayerController controller = prefab.GetComponent<PlayerController>();
            if (controller == null) continue;

            SerializedObject so = new SerializedObject(controller);

            so.FindProperty("walkSpeed").floatValue = walkSpeed;
            so.FindProperty("runSpeed").floatValue = runSpeed;
            so.FindProperty("rotationSpeed").floatValue = rotationSpeed;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);

            Debug.Log($"[CustomSpeeds] ✅ {prefab.name}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CustomSpeeds] ✅ ГОТОВО!");
    }
}
