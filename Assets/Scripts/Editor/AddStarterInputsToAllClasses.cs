using UnityEngine;
using UnityEditor;
using StarterAssets;

/// <summary>
/// Добавляет StarterAssetsInputs на все префабы классов персонажей
/// Menu: Aetherion/Setup/Add StarterAssetsInputs to All Classes
/// </summary>
public class AddStarterInputsToAllClasses : Editor
{
    [MenuItem("Aetherion/Setup/Add StarterAssetsInputs to All Classes")]
    public static void AddToAllClasses()
    {
        Debug.Log("=== Добавление StarterAssetsInputs на все префабы классов ===");

        // Пути к префабам классов (используются в BattleSceneManager)
        string[] prefabPaths = new string[]
        {
            "Assets/Resources/Characters/PaladinModel.prefab",
            "Assets/Resources/Characters/ArcherModel.prefab",
            "Assets/Resources/Characters/MageModel.prefab",
            "Assets/Resources/Characters/RogueModel.prefab"
        };

        int successCount = 0;
        int skippedCount = 0;
        int errorCount = 0;

        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogWarning($"⚠️ Префаб не найден: {path}");
                errorCount++;
                continue;
            }

            // Проверяем есть ли уже StarterAssetsInputs
            StarterAssetsInputs existingInputs = prefab.GetComponent<StarterAssetsInputs>();

            if (existingInputs != null)
            {
                Debug.Log($"✅ {prefab.name} - StarterAssetsInputs уже существует, пропускаем");
                skippedCount++;
                continue;
            }

            // Открываем префаб для редактирования
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);

            // Добавляем StarterAssetsInputs
            StarterAssetsInputs inputs = prefabInstance.AddComponent<StarterAssetsInputs>();

            if (inputs != null)
            {
                // Сохраняем изменения в префабе
                PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabInstance);

                Debug.Log($"✅ {prefab.name} - StarterAssetsInputs ДОБАВЛЕН!");
                successCount++;
            }
            else
            {
                PrefabUtility.UnloadPrefabContents(prefabInstance);
                Debug.LogError($"❌ {prefab.name} - Ошибка добавления StarterAssetsInputs!");
                errorCount++;
            }
        }

        // Итоги
        Debug.Log("=== РЕЗУЛЬТАТЫ ===");
        Debug.Log($"✅ Успешно добавлено: {successCount}");
        Debug.Log($"⏭️ Пропущено (уже есть): {skippedCount}");
        Debug.Log($"❌ Ошибки: {errorCount}");
        Debug.Log("===================");

        if (successCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("💾 Префабы сохранены!");
        }

        // Показываем финальное сообщение
        if (errorCount == 0)
        {
            EditorUtility.DisplayDialog(
                "Готово!",
                $"StarterAssetsInputs добавлен на {successCount} префаб(ов).\n\n" +
                $"Теперь джойстик будет работать автоматически с любым классом!",
                "OK"
            );
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Завершено с ошибками",
                $"Успешно: {successCount}\n" +
                $"Ошибки: {errorCount}\n\n" +
                $"Проверьте Console для деталей.",
                "OK"
            );
        }
    }
}
