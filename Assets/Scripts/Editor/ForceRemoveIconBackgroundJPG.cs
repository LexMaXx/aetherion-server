using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Принудительно убирает белый и черный фон со ВСЕХ иконок
/// Поддерживает PNG и JPG
/// </summary>
public class ForceRemoveIconBackgroundJPG : Editor
{
    [MenuItem("Tools/Aetherion/Remove Backgrounds (PNG + JPG)")]
    public static void RemoveAllBackgrounds()
    {
        if (!EditorUtility.DisplayDialog(
            "⚠️ Удаление фона с иконок",
            "Этот инструмент обработает ВСЕ иконки в Assets/UI/Icons\n\n" +
            "• Белые и черные пиксели → прозрачные\n" +
            "• JPG файлы → конвертирует в PNG\n\n" +
            "Продолжить?",
            "ДА",
            "Отмена"))
        {
            return;
        }

        Debug.Log("[RemoveBackgrounds] ⚡ Начинаю обработку...");

        string[] iconGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/UI/Icons" });
        int count = 0;

        foreach (string guid in iconGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"[RemoveBackgrounds] Найден файл: {path}");

            bool isPng = path.EndsWith(".png");
            bool isJpg = path.EndsWith(".jpg") || path.EndsWith(".jpeg");

            if (!isPng && !isJpg) continue;

            try
            {
                // Настраиваем импорт
                TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp != null)
                {
                    imp.isReadable = true;
                    imp.textureCompression = TextureImporterCompression.Uncompressed;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }

                // Загружаем
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null) continue;

                // Обрабатываем
                Texture2D newTex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
                Color[] pixels = tex.GetPixels();
                int removed = 0;

                for (int i = 0; i < pixels.Length; i++)
                {
                    Color p = pixels[i];
                    bool white = p.r > 0.85f && p.g > 0.85f && p.b > 0.85f;
                    bool black = p.r < 0.15f && p.g < 0.15f && p.b < 0.15f;

                    if (white || black)
                    {
                        pixels[i] = new Color(0, 0, 0, 0);
                        removed++;
                    }
                }

                newTex.SetPixels(pixels);
                newTex.Apply();

                // Сохраняем как PNG
                byte[] png = newTex.EncodeToPNG();
                string outPath = isJpg ? path.Replace(".jpg", ".png").Replace(".jpeg", ".png") : path;
                string fullPath = Path.Combine(Application.dataPath, outPath.Replace("Assets/", ""));

                File.WriteAllBytes(fullPath, png);

                // Удаляем JPG если конвертировали
                if (isJpg && outPath != path)
                {
                    File.Delete(Path.Combine(Application.dataPath, path.Replace("Assets/", "")));
                }

                Debug.Log($"[RemoveBackgrounds] ✅ {Path.GetFileName(outPath)} - удалено {removed} пикселей");
                count++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RemoveBackgrounds] ❌ {path}: {e.Message}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[RemoveBackgrounds] ✅ ГОТОВО! Обработано: {count}");
    }
}
