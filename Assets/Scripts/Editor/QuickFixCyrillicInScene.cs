using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Быстрое исправление кириллицы в текущей сцене
/// Заменяет все шрифты на LiberationSans SDF с поддержкой русских букв
/// </summary>
public class QuickFixCyrillicInScene : EditorWindow
{
    [MenuItem("Tools/Quick Fix Cyrillic in Current Scene")]
    static void QuickFix()
    {
        // Загружаем LiberationSans SDF
        TMP_FontAsset liberationSans = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (liberationSans == null)
        {
            Debug.LogError("❌ Не удалось загрузить LiberationSans SDF!");
            EditorUtility.DisplayDialog(
                "Ошибка",
                "LiberationSans SDF не найден!\nПроверьте что TextMeshPro импортирован.",
                "OK"
            );
            return;
        }

        Debug.Log($"✅ LiberationSans SDF загружен: {liberationSans.name}");

        // Находим все TextMeshProUGUI в сцене
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        int fixedCount = 0;

        foreach (var tmp in allTexts)
        {
            if (tmp.font == null || tmp.font.name.Contains("Cinzel") || !tmp.font.name.Contains("Liberation"))
            {
                Undo.RecordObject(tmp, "Fix Cyrillic Font");
                tmp.font = liberationSans;
                EditorUtility.SetDirty(tmp);
                fixedCount++;
                Debug.Log($"✅ Шрифт заменён: {tmp.gameObject.name} ({GetPath(tmp.transform)})");
            }
        }

        if (fixedCount > 0)
        {
            Debug.Log($"═══════════════════════════════════════════");
            Debug.Log($"✅ ГОТОВО! Исправлено {fixedCount} из {allTexts.Length} текстовых элементов");
            Debug.Log($"Сцена: {UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name}");
            Debug.Log($"═══════════════════════════════════════════");

            // Показываем диалог
            EditorUtility.DisplayDialog(
                "Готово!",
                $"✅ Исправлено {fixedCount} текстовых элементов\n\n" +
                $"Теперь кириллица должна отображаться!\n\n" +
                $"Сохраните сцену (Ctrl+S)",
                "OK"
            );

            // Маркируем сцену как изменённую
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );
        }
        else
        {
            Debug.Log($"✅ Все {allTexts.Length} текстов уже используют правильный шрифт!");
            EditorUtility.DisplayDialog(
                "Готово",
                $"Все текстовые элементы уже используют LiberationSans SDF.\n\n" +
                $"Кириллица должна работать!",
                "OK"
            );
        }
    }

    // Получить полный путь к GameObject
    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
