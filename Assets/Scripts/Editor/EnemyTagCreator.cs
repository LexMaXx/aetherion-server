using UnityEngine;
using UnityEditor;

/// <summary>
/// Автоматически создаёт тег "Enemy" при загрузке редактора
/// </summary>
[InitializeOnLoad]
public class EnemyTagCreator
{
    static EnemyTagCreator()
    {
        // Проверяем существует ли тег "Enemy"
        if (!TagExists("Enemy"))
        {
            Debug.Log("[EnemyTagCreator] Тег 'Enemy' не найден. Создаём...");
            CreateTag("Enemy");
        }
        else
        {
            Debug.Log("[EnemyTagCreator] ✓ Тег 'Enemy' существует");
        }
    }

    /// <summary>
    /// Проверить существует ли тег
    /// </summary>
    private static bool TagExists(string tagName)
    {
        try
        {
            GameObject.FindGameObjectWithTag(tagName);
            return true;
        }
        catch
        {
            // Открываем TagManager для проверки
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(tagName))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Создать тег
    /// </summary>
    private static void CreateTag(string tagName)
    {
        try
        {
            // Открываем TagManager
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            // Проверяем что тега ещё нет
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(tagName))
                {
                    Debug.Log($"[EnemyTagCreator] Тег '{tagName}' уже существует");
                    return;
                }
            }

            // Добавляем новый тег
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            newTag.stringValue = tagName;
            tagManager.ApplyModifiedProperties();

            Debug.Log($"[EnemyTagCreator] ✅ Тег '{tagName}' создан!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnemyTagCreator] ❌ Не удалось создать тег: {e.Message}");
        }
    }

    /// <summary>
    /// Создать слой (Layer)
    /// </summary>
    private static void CreateLayer(string layerName)
    {
        try
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");

            // Ищем пустой слот
            for (int i = 8; i < layersProp.arraySize; i++) // 0-7 зарезервированы
            {
                SerializedProperty layer = layersProp.GetArrayElementAtIndex(i);

                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"[EnemyTagCreator] ✅ Layer '{layerName}' создан на позиции {i}!");
                    return;
                }
                else if (layer.stringValue == layerName)
                {
                    Debug.Log($"[EnemyTagCreator] Layer '{layerName}' уже существует на позиции {i}");
                    return;
                }
            }

            Debug.LogWarning($"[EnemyTagCreator] ⚠️ Нет свободных слотов для создания Layer '{layerName}'");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnemyTagCreator] ❌ Не удалось создать Layer: {e.Message}");
        }
    }

    [MenuItem("Tools/Enemy Setup/Force Create Enemy Tag")]
    public static void ForceCreateEnemyTag()
    {
        CreateTag("Enemy");
    }

    [MenuItem("Tools/Enemy Setup/Force Create Enemy Layer")]
    public static void ForceCreateEnemyLayer()
    {
        CreateLayer("Enemy");
    }
}
