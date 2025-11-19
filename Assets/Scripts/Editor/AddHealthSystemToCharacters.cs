using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// КРИТИЧЕСКОЕ: Добавляет HealthSystem компонент на все префабы персонажей
/// Это необходимо для корректной работы системы урона в мультиплеере!
/// </summary>
public class AddHealthSystemToCharacters : EditorWindow
{
    [MenuItem("Tools/FIX: Add HealthSystem to All Characters")]
    public static void AddHealthSystemToAllCharacterPrefabs()
    {
        Debug.Log("=== НАЧАЛО: Добавление HealthSystem на все префабы персонажей ===");

        // Список всех префабов персонажей
        string[] characterPrefabPaths = new string[]
        {
            "Assets/Resources/Characters/WarriorModel.prefab",
            "Assets/Resources/Characters/MageModel.prefab",
            "Assets/Resources/Characters/ArcherModel.prefab",
            "Assets/Resources/Characters/RogueModel.prefab",
            "Assets/Resources/Characters/PaladinModel.prefab"
        };

        int successCount = 0;
        int failedCount = 0;
        List<string> failedPrefabs = new List<string>();

        foreach (string prefabPath in characterPrefabPaths)
        {
            Debug.Log($"\n[Processing] {prefabPath}...");

            // Загружаем префаб
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                Debug.LogWarning($"❌ Префаб не найден: {prefabPath}");
                failedCount++;
                failedPrefabs.Add(prefabPath);
                continue;
            }

            // Открываем префаб для редактирования
            string prefabAssetPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabAssetPath);

            if (prefabInstance == null)
            {
                Debug.LogWarning($"❌ Не удалось загрузить содержимое префаба: {prefabPath}");
                failedCount++;
                failedPrefabs.Add(prefabPath);
                continue;
            }

            bool modified = false;

            // 1. КРИТИЧЕСКОЕ: Добавляем HealthSystem если его нет
            HealthSystem healthSystem = prefabInstance.GetComponent<HealthSystem>();
            if (healthSystem == null)
            {
                healthSystem = prefabInstance.AddComponent<HealthSystem>();
                Debug.Log($"✅ Добавлен HealthSystem на {prefabInstance.name}");
                modified = true;
            }
            else
            {
                Debug.Log($"ℹ️ HealthSystem уже существует на {prefabInstance.name}");
            }

            // 2. Проверяем CharacterStats (должен быть, т.к. HealthSystem интегрируется с ним)
            CharacterStats characterStats = prefabInstance.GetComponent<CharacterStats>();
            if (characterStats == null)
            {
                Debug.LogWarning($"⚠️ CharacterStats НЕ НАЙДЕН на {prefabInstance.name}! HealthSystem не будет работать корректно!");
                Debug.LogWarning($"⚠️ Добавьте CharacterStats через Tools → Add CharacterStats или вручную");
            }
            else
            {
                Debug.Log($"✅ CharacterStats найден на {prefabInstance.name}");
            }

            // 3. Проверяем EffectManager (нужен для неуязвимости и других эффектов)
            EffectManager effectManager = prefabInstance.GetComponent<EffectManager>();
            if (effectManager == null)
            {
                effectManager = prefabInstance.AddComponent<EffectManager>();
                Debug.Log($"✅ Добавлен EffectManager на {prefabInstance.name}");
                modified = true;
            }
            else
            {
                Debug.Log($"ℹ️ EffectManager уже существует на {prefabInstance.name}");
            }

            // 4. Проверяем NetworkPlayer (ТОЛЬКО на NetworkPlayer префабах, НЕ на локальных!)
            // Для локальных персонажей НЕ добавляем NetworkPlayer!
            NetworkPlayer networkPlayer = prefabInstance.GetComponent<NetworkPlayer>();
            if (networkPlayer != null)
            {
                // Это NetworkPlayer префаб - добавляем NetworkPlayerEntity
                NetworkPlayerEntity networkPlayerEntity = prefabInstance.GetComponent<NetworkPlayerEntity>();
                if (networkPlayerEntity == null)
                {
                    networkPlayerEntity = prefabInstance.AddComponent<NetworkPlayerEntity>();
                    Debug.Log($"✅ Добавлен NetworkPlayerEntity на {prefabInstance.name} (это NetworkPlayer)");
                    modified = true;
                }
                else
                {
                    Debug.Log($"ℹ️ NetworkPlayerEntity уже существует на {prefabInstance.name}");
                }
            }
            else
            {
                // Это локальный игрок - NetworkPlayerEntity НЕ нужен
                Debug.Log($"ℹ️ Это локальный персонаж, NetworkPlayerEntity НЕ добавляется");
            }

            // Сохраняем изменения если они были
            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabAssetPath);
                Debug.Log($"💾 Префаб сохранен: {prefabPath}");
                successCount++;
            }
            else
            {
                Debug.Log($"ℹ️ Изменения не требуются для {prefabPath}");
                successCount++;
            }

            // Выгружаем префаб из памяти
            PrefabUtility.UnloadPrefabContents(prefabInstance);
        }

        Debug.Log($"\n=== ЗАВЕРШЕНО ===");
        Debug.Log($"✅ Успешно обработано: {successCount}/{characterPrefabPaths.Length}");
        if (failedCount > 0)
        {
            Debug.LogWarning($"❌ Не удалось обработать: {failedCount}/{characterPrefabPaths.Length}");
            foreach (string failed in failedPrefabs)
            {
                Debug.LogWarning($"   - {failed}");
            }
        }

        // Обновляем Asset Database
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Добавление HealthSystem",
            $"Обработано: {successCount}/{characterPrefabPaths.Length} префабов\n" +
            $"Неудачно: {failedCount}\n\n" +
            $"Проверьте Console для подробностей.",
            "OK");
    }
}
