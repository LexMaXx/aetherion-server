using UnityEngine;
using UnityEditor;

/// <summary>
/// Проверяет и добавляет необходимые компоненты ко всем персонажам
/// </summary>
public class FixCharacterControllers : Editor
{
    [MenuItem("Tools/Aetherion/Fix Character Controllers")]
    public static void FixControllers()
    {
        Debug.Log("=== [FixCharacterControllers] Проверка и исправление персонажей ===");

        string[] prefabPaths = new string[]
        {
            "Assets/UI/Prefabs/WarriorModel.prefab",
            "Assets/UI/Prefabs/MageModel.prefab",
            "Assets/UI/Prefabs/ArcherModel.prefab",
            "Assets/UI/Prefabs/RogueModel.prefab",
            "Assets/UI/Prefabs/PaladinModel.prefab"
        };

        int fixedCount = 0;

        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogWarning($"[FixCharacterControllers] ⚠️ Не найден: {path}");
                continue;
            }

            Debug.Log($"[FixCharacterControllers] Проверяю: {prefab.name}");

            bool wasFixed = false;

            // Проверяем PlayerController
            PlayerController playerController = prefab.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogWarning($"[FixCharacterControllers] ⚠️ {prefab.name}: НЕТ PlayerController! Добавляю...");

                // Добавляем компонент через PrefabUtility
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

                PlayerController newController = instance.AddComponent<PlayerController>();

                // Настраиваем стандартные значения через SerializedObject
                SerializedObject so = new SerializedObject(newController);
                so.FindProperty("walkSpeed").floatValue = 3f;
                so.FindProperty("runSpeed").floatValue = 6f;
                so.FindProperty("rotationSpeed").floatValue = 10f;
                so.FindProperty("gravity").floatValue = 30f;
                so.ApplyModifiedProperties();

                // Сохраняем изменения в префаб
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                DestroyImmediate(instance);

                Debug.Log($"[FixCharacterControllers] ✅ {prefab.name}: PlayerController добавлен!");
                wasFixed = true;
                fixedCount++;
            }
            else
            {
                Debug.Log($"[FixCharacterControllers] ✅ {prefab.name}: PlayerController есть");
            }

            // Проверяем CharacterController (Unity компонент)
            CharacterController charController = prefab.GetComponent<CharacterController>();
            if (charController == null)
            {
                Debug.LogWarning($"[FixCharacterControllers] ⚠️ {prefab.name}: НЕТ CharacterController! Добавляю...");

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                CharacterController newCharController = instance.AddComponent<CharacterController>();

                // Настраиваем стандартные значения
                newCharController.height = 2f;
                newCharController.radius = 0.5f;
                newCharController.center = new Vector3(0, 1f, 0);

                PrefabUtility.SaveAsPrefabAsset(instance, path);
                DestroyImmediate(instance);

                Debug.Log($"[FixCharacterControllers] ✅ {prefab.name}: CharacterController добавлен!");
                wasFixed = true;
                fixedCount++;
            }

            // Проверяем Animator
            Animator animator = prefab.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"[FixCharacterControllers] ⚠️ {prefab.name}: НЕТ Animator!");
            }
            else
            {
                Debug.Log($"[FixCharacterControllers] ✅ {prefab.name}: Animator есть");
            }

            if (wasFixed)
            {
                EditorUtility.SetDirty(prefab);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"=== [FixCharacterControllers] ГОТОВО! Исправлено: {fixedCount} ===");

        // После исправления снова проверяем скорости
        if (fixedCount > 0)
        {
            Debug.Log("[FixCharacterControllers] Проверяю скорости после исправления...");
            CheckCharacterSpeeds.CheckSpeeds();
        }
    }
}
