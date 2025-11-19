using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Автоматически убирает черный или белый фон с иконок скиллов
/// Работает со всеми иконками в папке Assets/UI/Icons
/// </summary>
public class RemoveIconBackground : Editor
{
    /// <summary>
    /// Убрать черный И белый фон с иконок (меняет сами PNG файлы)
    /// </summary>
    [MenuItem("Tools/Aetherion/Remove Icon Backgrounds (Black & White)")]
    public static void RemoveBackgrounds()
    {
        if (!EditorUtility.DisplayDialog(
            "Удаление фона с иконок",
            "Этот инструмент изменит PNG файлы, удалив черный и белый фон.\n\n" +
            "Иконки должны иметь:\n" +
            "- Черный фон (RGB близко к 0,0,0)\n" +
            "- ИЛИ Белый фон (RGB близко к 255,255,255)\n\n" +
            "Рекомендуется сделать backup!\n\n" +
            "Продолжить?",
            "Да, удалить фон",
            "Отмена"))
        {
            return;
        }

        Debug.Log("[RemoveIconBackground] 🎨 Начинаю обработку иконок...");

        string[] iconGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/UI/Icons" });
        int processedCount = 0;
        int skippedCount = 0;

        foreach (string guid in iconGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (!path.EndsWith(".png") && !path.EndsWith(".jpg") && !path.EndsWith(".jpeg"))
            {
                continue;
            }

            try
            {
                // Сначала настраиваем Import Settings
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.isReadable = true; // Нужно для чтения пикселей
                    importer.textureType = TextureImporterType.Sprite;
                    importer.alphaIsTransparency = true;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }

                // Загружаем текстуру
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null)
                {
                    Debug.LogWarning($"[RemoveIconBackground] ⚠️ Не удалось загрузить: {Path.GetFileName(path)}");
                    skippedCount++;
                    continue;
                }

                // Проверяем что фон нужно убрать
                if (!NeedsBackgroundRemoval(texture))
                {
                    Debug.Log($"[RemoveIconBackground] ⏭️ Пропущена (фон уже прозрачный): {Path.GetFileName(path)}");
                    skippedCount++;
                    continue;
                }

                // Убираем фон
                Texture2D newTexture = RemoveBackground(texture);

                if (newTexture != null)
                {
                    // Сохраняем обратно в PNG
                    byte[] pngData = newTexture.EncodeToPNG();
                    string fullPath = Path.Combine(Application.dataPath, path.Replace("Assets/", ""));
                    File.WriteAllBytes(fullPath, pngData);

                    Debug.Log($"[RemoveIconBackground] ✅ {Path.GetFileName(path)}");
                    processedCount++;
                }
                else
                {
                    Debug.LogWarning($"[RemoveIconBackground] ⚠️ Не удалось обработать: {Path.GetFileName(path)}");
                    skippedCount++;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RemoveIconBackground] ❌ Ошибка: {Path.GetFileName(path)} - {e.Message}");
                skippedCount++;
            }
        }

        AssetDatabase.Refresh();

        Debug.Log($"[RemoveIconBackground] ✅ ГОТОВО!");
        Debug.Log($"  - Обработано: {processedCount}");
        Debug.Log($"  - Пропущено: {skippedCount}");
        Debug.Log($"  - Фон (черный/белый) удалён, иконки готовы!");
    }

    /// <summary>
    /// Проверка: нужно ли убирать фон у этой текстуры
    /// </summary>
    private static bool NeedsBackgroundRemoval(Texture2D texture)
    {
        Color[] pixels = texture.GetPixels();

        // Проверяем углы изображения - обычно там фон
        int width = texture.width;
        int height = texture.height;

        Color[] corners = new Color[]
        {
            pixels[0], // Левый верхний
            pixels[width - 1], // Правый верхний
            pixels[(height - 1) * width], // Левый нижний
            pixels[height * width - 1] // Правый нижний
        };

        // Если хотя бы один угол черный или белый - нужно обработать
        foreach (Color corner in corners)
        {
            if (IsBlack(corner) || IsWhite(corner))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Убрать черный и белый фон
    /// </summary>
    private static Texture2D RemoveBackground(Texture2D source)
    {
        Texture2D result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        Color[] pixels = source.GetPixels();

        float blackThreshold = 0.15f; // Порог для черного (0-1)
        float whiteThreshold = 0.85f; // Порог для белого (0-1)

        for (int i = 0; i < pixels.Length; i++)
        {
            Color pixel = pixels[i];

            // Проверяем: черный или белый пиксель?
            if (IsBlack(pixel, blackThreshold))
            {
                // Черный -> прозрачный
                pixels[i] = new Color(0, 0, 0, 0);
            }
            else if (IsWhite(pixel, whiteThreshold))
            {
                // Белый -> прозрачный
                pixels[i] = new Color(1, 1, 1, 0);
            }
            // Остальные пиксели (цветные) - оставляем как есть
        }

        result.SetPixels(pixels);
        result.Apply();

        return result;
    }

    /// <summary>
    /// Проверка: черный ли пиксель
    /// </summary>
    private static bool IsBlack(Color color, float threshold = 0.15f)
    {
        return color.r < threshold && color.g < threshold && color.b < threshold;
    }

    /// <summary>
    /// Проверка: белый ли пиксель
    /// </summary>
    private static bool IsWhite(Color color, float threshold = 0.85f)
    {
        return color.r > threshold && color.g > threshold && color.b > threshold;
    }

    /// <summary>
    /// Только настройки импорта (без изменения файлов)
    /// </summary>
    [MenuItem("Tools/Aetherion/Fix Icon Import Settings Only")]
    public static void FixImportSettings()
    {
        Debug.Log("[RemoveIconBackground] 🔧 Настраиваю Import Settings...");

        string[] iconGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/UI/Icons" });
        int count = 0;

        foreach (string guid in iconGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool changed = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                changed = true;
            }

            if (changed)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[RemoveIconBackground] ✅ Настроено {count} иконок");
    }
}
