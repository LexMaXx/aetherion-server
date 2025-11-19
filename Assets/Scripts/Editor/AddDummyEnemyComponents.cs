using UnityEngine;
using UnityEditor;

/// <summary>
/// Утилита для добавления компонента DummyEnemy ко всем врагам в сцене
/// </summary>
public class AddDummyEnemyComponents : EditorWindow
{
    [MenuItem("Aetherion/Fix Dummy Enemies (Add Components)")]
    public static void FixDummyEnemies()
    {
        // Находим все объекты с именем содержащим "DummyEnemy"
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        int fixedCount = 0;

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("DummyEnemy"))
            {
                // Проверяем есть ли уже компонент
                DummyEnemy existingComponent = obj.GetComponent<DummyEnemy>();
                if (existingComponent == null)
                {
                    // Добавляем компонент
                    DummyEnemy dummy = obj.AddComponent<DummyEnemy>();

                    // Помечаем объект как изменённый
                    EditorUtility.SetDirty(obj);

                    fixedCount++;
                    Debug.Log($"[FixDummyEnemies] ✅ Добавлен DummyEnemy к {obj.name}");
                }
                else
                {
                    Debug.Log($"[FixDummyEnemies] ⏭️ {obj.name} уже имеет компонент DummyEnemy");
                }
            }
        }

        if (fixedCount > 0)
        {
            Debug.Log($"[FixDummyEnemies] 🎉 Исправлено {fixedCount} врагов!");
            EditorUtility.DisplayDialog(
                "Готово!",
                $"Добавлен компонент DummyEnemy к {fixedCount} объектам!\n\nТеперь нажмите Play и тестируйте атаку (Space).",
                "OK"
            );
        }
        else
        {
            Debug.LogWarning("[FixDummyEnemies] ⚠️ Не найдено объектов для исправления!");
            EditorUtility.DisplayDialog(
                "Ничего не найдено",
                "Не найдено объектов DummyEnemy в текущей сцене.\n\nУбедитесь что:\n1. Открыта правильная сцена\n2. Есть объекты с именем 'DummyEnemy'",
                "OK"
            );
        }
    }
}
