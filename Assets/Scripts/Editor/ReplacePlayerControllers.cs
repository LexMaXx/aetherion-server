using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Заменяет MixamoPlayerController на PlayerController во всех префабах персонажей
/// </summary>
public class ReplacePlayerControllers : Editor
{
    [MenuItem("Tools/Character Setup/Replace with PlayerController (Agility Speed)")]
    public static void ReplaceControllers()
    {
        Debug.Log("[ReplacePlayerControllers] ========== НАЧАЛО ==========");

        // Находим все префабы персонажей
        string[] prefabPaths = new string[]
        {
            "Assets/UI/Prefabs/WarriorModel.prefab",
            "Assets/UI/Prefabs/MageModel.prefab",
            "Assets/UI/Prefabs/ArcherModel.prefab",
            "Assets/UI/Prefabs/RogueModel.prefab",
            "Assets/UI/Prefabs/PaladinModel.prefab"
        };

        int replaced = 0;
        int added = 0;

        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[ReplacePlayerControllers] ⚠️ Префаб не найден: {path}");
                continue;
            }

            Debug.Log($"\n[ReplacePlayerControllers] Обработка: {prefab.name}");

            // Открываем префаб для редактирования
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);

            bool modified = false;

            // Ищем MixamoPlayerController
            MixamoPlayerController oldController = prefabContents.GetComponent<MixamoPlayerController>();
            if (oldController != null)
            {
                Debug.Log($"  ❌ Найден MixamoPlayerController - удаляю");
                DestroyImmediate(oldController, true);
                modified = true;
                replaced++;
            }

            // Проверяем есть ли уже PlayerController
            PlayerController playerController = prefabContents.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.Log($"  ✅ Добавляю PlayerController");
                playerController = prefabContents.AddComponent<PlayerController>();

                // Устанавливаем стандартные значения через SerializedObject
                SerializedObject so = new SerializedObject(playerController);
                so.FindProperty("walkSpeed").floatValue = 3f;
                so.FindProperty("runSpeed").floatValue = 6f;
                so.FindProperty("rotationSpeed").floatValue = 10f;
                so.FindProperty("gravity").floatValue = 30f;
                so.ApplyModifiedProperties();

                modified = true;
                added++;
            }
            else
            {
                Debug.Log($"  ℹ️ PlayerController уже есть - пропускаю");
            }

            // Сохраняем изменения
            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                Debug.Log($"  💾 Префаб сохранён: {prefab.name}");
            }

            PrefabUtility.UnloadPrefabContents(prefabContents);
        }

        Debug.Log($"\n[ReplacePlayerControllers] ========== ЗАВЕРШЕНО ==========");
        Debug.Log($"  Удалено MixamoPlayerController: {replaced}");
        Debug.Log($"  Добавлено PlayerController: {added}");
        Debug.Log($"  ✅ Теперь все персонажи используют PlayerController с агилити-бонусом!");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
