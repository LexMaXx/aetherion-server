using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Исправляет все шрифты в проекте для поддержки кириллицы
/// Заменяет Cinzel Decorative (не поддерживает кириллицу) на LiberationSans SDF
/// </summary>
public class FixCyrillicFonts : EditorWindow
{
    private TMP_FontAsset fallbackFont;
    private int fixedCount = 0;

    [MenuItem("Tools/Fix Cyrillic Fonts")]
    static void ShowWindow()
    {
        FixCyrillicFonts window = GetWindow<FixCyrillicFonts>("Fix Cyrillic Fonts");
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Fix Cyrillic Fonts", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Этот инструмент заменит все шрифты в сценах на LiberationSans SDF,\n" +
            "который поддерживает кириллицу.\n\n" +
            "Cinzel Decorative НЕ поддерживает русские буквы!",
            MessageType.Info
        );

        GUILayout.Space(10);

        fallbackFont = EditorGUILayout.ObjectField(
            "Fallback Font (с кириллицей):",
            fallbackFont,
            typeof(TMP_FontAsset),
            false
        ) as TMP_FontAsset;

        GUILayout.Space(10);

        if (GUILayout.Button("Auto-Load LiberationSans SDF", GUILayout.Height(30)))
        {
            LoadDefaultFont();
        }

        GUILayout.Space(10);

        EditorGUI.BeginDisabledGroup(fallbackFont == null);
        if (GUILayout.Button("Fix All Fonts in Current Scene", GUILayout.Height(40)))
        {
            FixFontsInCurrentScene();
        }

        if (GUILayout.Button("Fix All Fonts in All Scenes", GUILayout.Height(40)))
        {
            FixFontsInAllScenes();
        }
        EditorGUI.EndDisabledGroup();

        if (fallbackFont == null)
        {
            EditorGUILayout.HelpBox(
                "Нажмите 'Auto-Load LiberationSans SDF' для загрузки шрифта с кириллицей",
                MessageType.Warning
            );
        }

        GUILayout.Space(10);

        if (fixedCount > 0)
        {
            EditorGUILayout.HelpBox(
                $"✅ Исправлено шрифтов: {fixedCount}",
                MessageType.Info
            );
        }
    }

    void LoadDefaultFont()
    {
        // Загружаем LiberationSans SDF из ресурсов TextMeshPro
        fallbackFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (fallbackFont != null)
        {
            Debug.Log("✅ LiberationSans SDF загружен успешно!");
        }
        else
        {
            Debug.LogError("❌ Не удалось загрузить LiberationSans SDF! Проверьте что TextMeshPro импортирован.");

            // Попробуем найти через AssetDatabase
            string[] guids = AssetDatabase.FindAssets("LiberationSans t:TMP_FontAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                fallbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                Debug.Log($"✅ Найден через AssetDatabase: {path}");
            }
        }
    }

    void FixFontsInCurrentScene()
    {
        fixedCount = 0;

        // Находим все TextMeshProUGUI в текущей сцене
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>(true);

        foreach (var tmp in allTexts)
        {
            if (ShouldReplaceFont(tmp))
            {
                Undo.RecordObject(tmp, "Fix Font");
                tmp.font = fallbackFont;
                EditorUtility.SetDirty(tmp);
                fixedCount++;
                Debug.Log($"✅ Шрифт заменён: {tmp.gameObject.name}");
            }
        }

        Debug.Log($"═══════════════════════════════════════════");
        Debug.Log($"✅ Исправлено {fixedCount} текстовых элементов");
        Debug.Log($"═══════════════════════════════════════════");

        // Сохраняем сцену
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );
    }

    void FixFontsInAllScenes()
    {
        if (!EditorUtility.DisplayDialog(
            "Fix All Scenes",
            "Это заменит шрифты во ВСЕХ сценах проекта.\nПродолжить?",
            "Да",
            "Отмена"
        ))
        {
            return;
        }

        fixedCount = 0;

        // Сохраняем текущую сцену
        UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // Находим все сцены
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");

        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);

            // ПРОПУСКАЕМ сцены из Packages (их нельзя открывать)
            if (scenePath.StartsWith("Packages/"))
            {
                Debug.LogWarning($"⚠️ Пропускаем сцену из пакета: {scenePath}");
                continue;
            }

            // ПРОПУСКАЕМ сцены из Library
            if (scenePath.StartsWith("Library/"))
            {
                continue;
            }

            Debug.Log($"Открываем сцену: {scenePath}");

            // Открываем сцену
            try
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Не удалось открыть сцену {scenePath}: {e.Message}");
                continue;
            }

            // Исправляем шрифты
            TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>(true);

            foreach (var tmp in allTexts)
            {
                if (ShouldReplaceFont(tmp))
                {
                    Undo.RecordObject(tmp, "Fix Font");
                    tmp.font = fallbackFont;
                    EditorUtility.SetDirty(tmp);
                    fixedCount++;
                }
            }

            // Сохраняем сцену
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );
        }

        Debug.Log($"═══════════════════════════════════════════");
        Debug.Log($"✅ Исправлено {fixedCount} текстовых элементов во всех сценах!");
        Debug.Log($"═══════════════════════════════════════════");
    }

    bool ShouldReplaceFont(TextMeshProUGUI tmp)
    {
        if (tmp.font == null)
        {
            return true; // Нет шрифта - заменяем
        }

        string fontName = tmp.font.name.ToLower();

        // Заменяем если это Cinzel Decorative (не поддерживает кириллицу)
        if (fontName.Contains("cinzel"))
        {
            return true;
        }

        // Заменяем если это не LiberationSans
        if (!fontName.Contains("liberation"))
        {
            Debug.LogWarning($"⚠️ Неизвестный шрифт '{tmp.font.name}' на {tmp.gameObject.name}. Может не поддерживать кириллицу.");
            return true;
        }

        return false;
    }
}
