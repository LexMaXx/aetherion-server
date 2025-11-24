using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor скрипт для исправления MageModel.prefab
/// Добавляет недостающий компонент CharacterStats
/// </summary>
public class FixMagePrefab : EditorWindow
{
    [MenuItem("Tools/Aetherion/Fix Mage Prefab (Add CharacterStats)")]
    public static void ShowWindow()
    {
        if (EditorUtility.DisplayDialog(
            "Fix Mage Prefab",
            "Этот скрипт добавит компонент CharacterStats к MageModel.prefab.\n\n" +
            "ПРОБЛЕМА: Без CharacterStats маг не может получать опыт и золото!\n\n" +
            "Продолжить?",
            "Да, исправить",
            "Отмена"
        ))
        {
            FixMage();
        }
    }

    private static void FixMage()
    {
        // Загружаем префаб
        string prefabPath = "Assets/Resources/Characters/MageModel.prefab";
        GameObject magePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (magePrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Ошибка",
                $"Префаб не найден: {prefabPath}",
                "OK"
            );
            return;
        }

        // Открываем префаб для редактирования
        string assetPath = AssetDatabase.GetAssetPath(magePrefab);
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(assetPath);

        if (prefabInstance == null)
        {
            EditorUtility.DisplayDialog(
                "Ошибка",
                "Не удалось открыть префаб для редактирования",
                "OK"
            );
            return;
        }

        // Проверяем есть ли уже CharacterStats
        CharacterStats existingStats = prefabInstance.GetComponent<CharacterStats>();
        if (existingStats != null)
        {
            EditorUtility.DisplayDialog(
                "Информация",
                "CharacterStats уже существует на MageModel.prefab!",
                "OK"
            );
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            return;
        }

        // Добавляем CharacterStats
        CharacterStats characterStats = prefabInstance.AddComponent<CharacterStats>();
        Debug.Log("[FixMagePrefab] ✅ CharacterStats добавлен к MageModel");

        // Загружаем ClassStatsPreset для мага
        ClassStatsPreset magePreset = Resources.Load<ClassStatsPreset>("ClassStats/MageStats");
        if (magePreset != null)
        {
            SerializedObject serializedStats = new SerializedObject(characterStats);
            serializedStats.FindProperty("classPreset").objectReferenceValue = magePreset;
            serializedStats.ApplyModifiedProperties();
            Debug.Log("[FixMagePrefab] ✅ MageStats preset установлен");
        }
        else
        {
            Debug.LogWarning("[FixMagePrefab] ⚠️ MageStats preset не найден в Resources/ClassStats/");
        }

        // Загружаем StatsFormulas
        StatsFormulas formulas = Resources.Load<StatsFormulas>("StatsFormulas");
        if (formulas != null)
        {
            SerializedObject serializedStats = new SerializedObject(characterStats);
            serializedStats.FindProperty("formulas").objectReferenceValue = formulas;
            serializedStats.ApplyModifiedProperties();
            Debug.Log("[FixMagePrefab] ✅ StatsFormulas установлены");
        }
        else
        {
            Debug.LogWarning("[FixMagePrefab] ⚠️ StatsFormulas не найдены в Resources/");
        }

        // Сохраняем изменения в префабе
        PrefabUtility.SaveAsPrefabAsset(prefabInstance, assetPath);
        PrefabUtility.UnloadPrefabContents(prefabInstance);

        Debug.Log("[FixMagePrefab] ✅ MageModel.prefab сохранён");

        EditorUtility.DisplayDialog(
            "Готово!",
            "CharacterStats успешно добавлен к MageModel.prefab!\n\n" +
            "Теперь маг сможет получать опыт и золото.\n\n" +
            "Изменения уже сохранены.",
            "OK"
        );

        // Обновляем AssetDatabase
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
