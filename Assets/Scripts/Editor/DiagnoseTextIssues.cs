using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Диагностика проблем с текстом - показывает реальное состояние всех TextMeshPro элементов
/// </summary>
public class DiagnoseTextIssues : EditorWindow
{
    private Vector2 scrollPos;

    [MenuItem("Tools/Diagnose Text Issues")]
    static void ShowWindow()
    {
        DiagnoseTextIssues window = GetWindow<DiagnoseTextIssues>("Text Diagnosis");
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Text Issues Diagnosis", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Analyze Current Scene", GUILayout.Height(40)))
        {
            AnalyzeScene();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Fix All Issues", GUILayout.Height(40)))
        {
            FixAllIssues();
        }
    }

    void AnalyzeScene()
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("📊 ДИАГНОСТИКА ТЕКСТА В СЦЕНЕ");
        Debug.Log("═══════════════════════════════════════════");

        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        Debug.Log($"Всего текстовых элементов: {allTexts.Length}");

        int issuesFound = 0;

        foreach (var tmp in allTexts)
        {
            string path = GetPath(tmp.transform);
            bool hasIssue = false;
            string issues = "";

            // Проверка 1: Шрифт
            if (tmp.font == null)
            {
                issues += "❌ НЕТ ШРИФТА! ";
                hasIssue = true;
            }
            else if (tmp.font.name.Contains("Cinzel"))
            {
                issues += "⚠️ Cinzel (нет кириллицы) ";
                hasIssue = true;
            }
            else
            {
                issues += $"✅ Шрифт: {tmp.font.name} ";
            }

            // Проверка 2: Цвет и прозрачность
            if (tmp.color.a < 0.01f)
            {
                issues += "❌ ПРОЗРАЧНЫЙ (alpha=0)! ";
                hasIssue = true;
            }
            else if (tmp.color.a < 0.5f)
            {
                issues += $"⚠️ Полупрозрачный (alpha={tmp.color.a:F2}) ";
                hasIssue = true;
            }

            // Проверка 3: Размер текста
            if (tmp.fontSize < 1f)
            {
                issues += "❌ РАЗМЕР = 0! ";
                hasIssue = true;
            }
            else if (tmp.fontSize < 10f)
            {
                issues += $"⚠️ Очень маленький ({tmp.fontSize}) ";
            }

            // Проверка 4: Текст
            if (string.IsNullOrEmpty(tmp.text))
            {
                issues += "⚠️ Пустой текст ";
            }
            else
            {
                string preview = tmp.text.Length > 30 ? tmp.text.Substring(0, 30) + "..." : tmp.text;
                issues += $"| Текст: \"{preview}\"";
            }

            // Проверка 5: GameObject активен
            if (!tmp.gameObject.activeInHierarchy)
            {
                issues += "⚠️ ОТКЛЮЧЕН ";
            }

            // Проверка 6: Material
            if (tmp.fontMaterial != null && tmp.fontMaterial.shader == null)
            {
                issues += "❌ МАТЕРИАЛ БЕЗ ШЕЙДЕРА! ";
                hasIssue = true;
            }

            // Проверка 7: Canvas
            Canvas canvas = tmp.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                issues += "❌ НЕТ CANVAS! ";
                hasIssue = true;
            }
            else if (!canvas.enabled)
            {
                issues += "❌ CANVAS ОТКЛЮЧЕН! ";
                hasIssue = true;
            }

            if (hasIssue)
            {
                Debug.LogError($"[ПРОБЛЕМА] {path}\n{issues}", tmp.gameObject);
                issuesFound++;
            }
            else if (issues.Contains("⚠️"))
            {
                Debug.LogWarning($"[WARNING] {path}\n{issues}", tmp.gameObject);
            }
            else
            {
                Debug.Log($"[OK] {path}\n{issues}");
            }
        }

        Debug.Log("═══════════════════════════════════════════");
        if (issuesFound > 0)
        {
            Debug.LogError($"❌ НАЙДЕНО {issuesFound} ПРОБЛЕМ!");
        }
        else
        {
            Debug.Log("✅ Все тексты в порядке!");
        }
        Debug.Log("═══════════════════════════════════════════");
    }

    void FixAllIssues()
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("🔧 АВТОИСПРАВЛЕНИЕ ПРОБЛЕМ");
        Debug.Log("═══════════════════════════════════════════");

        // Загружаем LiberationSans SDF
        TMP_FontAsset liberationSans = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (liberationSans == null)
        {
            Debug.LogError("❌ LiberationSans SDF не найден!");
            return;
        }

        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        int fixedCount = 0;

        foreach (var tmp in allTexts)
        {
            bool wasFixed = false;

            // Исправление 1: Шрифт
            if (tmp.font == null || tmp.font.name.Contains("Cinzel"))
            {
                Undo.RecordObject(tmp, "Fix Font");
                tmp.font = liberationSans;
                wasFixed = true;
                Debug.Log($"✅ Исправлен шрифт: {GetPath(tmp.transform)}");
            }

            // Исправление 2: Прозрачность
            if (tmp.color.a < 0.01f)
            {
                Undo.RecordObject(tmp, "Fix Alpha");
                tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 1f);
                wasFixed = true;
                Debug.Log($"✅ Исправлена прозрачность: {GetPath(tmp.transform)}");
            }

            // Исправление 3: Размер
            if (tmp.fontSize < 1f)
            {
                Undo.RecordObject(tmp, "Fix Font Size");
                tmp.fontSize = 14f;
                wasFixed = true;
                Debug.Log($"✅ Исправлен размер: {GetPath(tmp.transform)}");
            }

            // Исправление 4: Материал с ошибкой
            if (tmp.fontMaterial != null && tmp.fontMaterial.shader == null)
            {
                Undo.RecordObject(tmp, "Fix Material");
                tmp.fontMaterial = null; // Используем стандартный
                wasFixed = true;
                Debug.Log($"✅ Удалён поломанный материал: {GetPath(tmp.transform)}");
            }

            if (wasFixed)
            {
                EditorUtility.SetDirty(tmp);
                fixedCount++;
            }
        }

        Debug.Log("═══════════════════════════════════════════");
        Debug.Log($"✅ ИСПРАВЛЕНО {fixedCount} элементов!");
        Debug.Log("═══════════════════════════════════════════");

        // Маркируем сцену как изменённую
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        EditorUtility.DisplayDialog(
            "Готово!",
            $"✅ Исправлено {fixedCount} текстовых элементов\n\n" +
            "Проверьте Unity Console для деталей.\n\n" +
            "Сохраните сцену (Ctrl+S)",
            "OK"
        );
    }

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
