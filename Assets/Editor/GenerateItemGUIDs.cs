using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor утилита для генерации GUID для всех предметов
/// </summary>
public class GenerateItemGUIDs : EditorWindow
{
    [MenuItem("Tools/Inventory/Generate All Item GUIDs")]
    static void GenerateAllItemGUIDs()
    {
        // Загружаем все ItemData из Resources/Data/Items
        ItemData[] allItems = Resources.LoadAll<ItemData>("Data/Items");

        if (allItems.Length == 0)
        {
            Debug.LogWarning("[GenerateItemGUIDs] ⚠️ Предметы не найдены в Resources/Data/Items!");
            return;
        }

        Debug.Log($"[GenerateItemGUIDs] 🔍 Найдено {allItems.Length} предметов. Генерируем GUID...");

        int generatedCount = 0;
        int alreadyHadGuid = 0;

        foreach (ItemData item in allItems)
        {
            // Проверяем текущий GUID (через reflection, чтобы не триггерить auto-generation)
            SerializedObject so = new SerializedObject(item);
            SerializedProperty itemIdProp = so.FindProperty("itemId");

            if (itemIdProp != null)
            {
                string currentGuid = itemIdProp.stringValue;

                if (string.IsNullOrEmpty(currentGuid))
                {
                    // Триггерим автогенерацию через property
                    string newGuid = item.ItemId;
                    Debug.Log($"[GenerateItemGUIDs] ✅ {item.itemName} → {newGuid}");
                    generatedCount++;

                    // Помечаем как грязный для сохранения
                    EditorUtility.SetDirty(item);
                }
                else
                {
                    Debug.Log($"[GenerateItemGUIDs] ℹ️ {item.itemName} уже имеет GUID: {currentGuid}");
                    alreadyHadGuid++;
                }
            }
            else
            {
                Debug.LogError($"[GenerateItemGUIDs] ❌ {item.itemName} не имеет поля itemId! Обновите ItemData.cs");
            }
        }

        // Сохраняем все изменения
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[GenerateItemGUIDs] 🎉 ГОТОВО!");
        Debug.Log($"  - Всего предметов: {allItems.Length}");
        Debug.Log($"  - Сгенерировано новых GUID: {generatedCount}");
        Debug.Log($"  - Уже имели GUID: {alreadyHadGuid}");
    }

    [MenuItem("Tools/Inventory/Show All Item GUIDs")]
    static void ShowAllItemGUIDs()
    {
        ItemData[] allItems = Resources.LoadAll<ItemData>("Data/Items");

        if (allItems.Length == 0)
        {
            Debug.LogWarning("[GenerateItemGUIDs] ⚠️ Предметы не найдены в Resources/Data/Items!");
            return;
        }

        Debug.Log($"[GenerateItemGUIDs] 📋 СПИСОК ВСЕХ ПРЕДМЕТОВ И ИХ GUID:\n");

        foreach (ItemData item in allItems)
        {
            Debug.Log($"  {item.itemName,-30} → {item.ItemId}");
        }

        Debug.Log($"\nВсего: {allItems.Length} предметов");
    }

    [MenuItem("Tools/Inventory/Verify Item Database")]
    static void VerifyItemDatabase()
    {
        ItemData[] allItems = Resources.LoadAll<ItemData>("Data/Items");

        if (allItems.Length == 0)
        {
            Debug.LogWarning("[GenerateItemGUIDs] ⚠️ Предметы не найдены в Resources/Data/Items!");
            return;
        }

        Debug.Log($"[GenerateItemGUIDs] 🔍 ПРОВЕРКА БАЗЫ ДАННЫХ ПРЕДМЕТОВ:\n");

        int errors = 0;
        int warnings = 0;

        foreach (ItemData item in allItems)
        {
            // Проверка 1: GUID не пустой
            string guid = item.ItemId;
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"  ❌ {item.itemName}: GUID пустой!");
                errors++;
            }

            // Проверка 2: itemName не пустой
            if (string.IsNullOrEmpty(item.itemName))
            {
                Debug.LogError($"  ❌ Предмет с GUID {guid}: itemName пустой!");
                errors++;
            }

            // Проверка 3: icon назначен
            if (item.icon == null)
            {
                Debug.LogWarning($"  ⚠️ {item.itemName}: icon не назначен!");
                warnings++;
            }
        }

        Debug.Log($"\n[GenerateItemGUIDs] РЕЗУЛЬТАТ:");
        Debug.Log($"  - Всего предметов: {allItems.Length}");
        Debug.Log($"  - Ошибок: {errors}");
        Debug.Log($"  - Предупреждений: {warnings}");

        if (errors == 0 && warnings == 0)
        {
            Debug.Log("  ✅ База данных в порядке!");
        }
    }
}
