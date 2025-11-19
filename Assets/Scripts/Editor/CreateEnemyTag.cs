using UnityEngine;
using UnityEditor;

/// <summary>
/// Создаёт тег Enemy в проекте
/// </summary>
public class CreateEnemyTag : Editor
{
    [MenuItem("Tools/Enemy Setup/Create Enemy Tag")]
    public static void CreateTag()
    {
        Debug.Log("[CreateEnemyTag] Создание тега Enemy...");

        // Открываем TagManager
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        // Проверяем существует ли тег
        bool tagExists = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty tag = tagsProp.GetArrayElementAtIndex(i);
            if (tag.stringValue == "Enemy")
            {
                tagExists = true;
                break;
            }
        }

        if (tagExists)
        {
            Debug.Log("[CreateEnemyTag] ℹ️ Тег Enemy уже существует");
        }
        else
        {
            // Добавляем новый тег
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            newTag.stringValue = "Enemy";
            tagManager.ApplyModifiedProperties();

            Debug.Log("[CreateEnemyTag] ✅ Тег Enemy создан!");
        }

        // Проверяем Layer Enemy
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        bool layerExists = false;
        int emptyLayerIndex = -1;

        for (int i = 8; i < layersProp.arraySize; i++) // Начинаем с 8 (первые 8 зарезервированы Unity)
        {
            SerializedProperty layer = layersProp.GetArrayElementAtIndex(i);
            if (layer.stringValue == "Enemy")
            {
                layerExists = true;
                break;
            }
            if (string.IsNullOrEmpty(layer.stringValue) && emptyLayerIndex == -1)
            {
                emptyLayerIndex = i;
            }
        }

        if (layerExists)
        {
            Debug.Log("[CreateEnemyTag] ℹ️ Layer Enemy уже существует");
        }
        else if (emptyLayerIndex != -1)
        {
            // Добавляем новый Layer
            SerializedProperty newLayer = layersProp.GetArrayElementAtIndex(emptyLayerIndex);
            newLayer.stringValue = "Enemy";
            tagManager.ApplyModifiedProperties();

            Debug.Log($"[CreateEnemyTag] ✅ Layer Enemy создан (Layer {emptyLayerIndex})!");
        }
        else
        {
            Debug.LogWarning("[CreateEnemyTag] ⚠️ Нет свободных слотов для Layer Enemy");
        }

        Debug.Log("[CreateEnemyTag] ✅ Настройка завершена!");
    }
}
