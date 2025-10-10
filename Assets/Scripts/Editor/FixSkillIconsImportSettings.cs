using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Автоматически убирает черный фон с иконок скиллов и настраивает их для UI
/// Запуск: Tools → Aetherion → Fix Skill Icons (Remove Background)
/// </summary>
public class FixSkillIconsImportSettings : Editor
{
    [MenuItem("Tools/Aetherion/Fix Skill Icons (Remove Background)")]
    public static void FixAllSkillIcons()
    {
        Debug.Log("[FixSkillIcons] Начинаю обработку иконок скиллов...");

        // Ищем все иконки в папке UI/Icons
        string[] iconGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/UI/Icons" });

        int processedCount = 0;
        int errorCount = 0;

        foreach (string guid in iconGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Пропускаем файлы которые не PNG/JPG
            if (!path.EndsWith(".png") && !path.EndsWith(".jpg") && !path.EndsWith(".jpeg"))
            {
                continue;
            }

            Debug.Log($"[FixSkillIcons] Обрабатываю: {path}");

            try
            {
                // Получаем TextureImporter
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null)
                {
                    Debug.LogError($"[FixSkillIcons] Не удалось получить TextureImporter для {path}");
                    errorCount++;
                    continue;
                }

                // Настраиваем импорт для UI Sprite
                bool changed = false;

                // 1. Тип текстуры - Sprite (2D and UI)
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }

                // 2. Sprite Mode - Single
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }

                // 3. Включаем Read/Write (нужно для обработки пикселей)
                if (!importer.isReadable)
                {
                    importer.isReadable = true;
                    changed = true;
                }

                // 4. Включаем Alpha Is Transparency (ключевая настройка!)
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }

                // 5. Alpha Source - From Input Texture Alpha
                if (importer.alphaSource != TextureImporterAlphaSource.FromInput)
                {
                    importer.alphaSource = TextureImporterAlphaSource.FromInput;
                    changed = true;
                }

                // 6. Формат сжатия - RGBA 32 bit для лучшего качества
                TextureImporterPlatformSettings platformSettings = importer.GetDefaultPlatformTextureSettings();
                if (platformSettings.format != TextureImporterFormat.RGBA32)
                {
                    platformSettings.format = TextureImporterFormat.RGBA32;
                    platformSettings.overridden = true;
                    importer.SetPlatformTextureSettings(platformSettings);
                    changed = true;
                }

                // 7. Filter Mode - Bilinear для сглаживания
                if (importer.filterMode != FilterMode.Bilinear)
                {
                    importer.filterMode = FilterMode.Bilinear;
                    changed = true;
                }

                // 8. Max Size - 256 (иконки небольшие)
                if (importer.maxTextureSize != 256)
                {
                    importer.maxTextureSize = 256;
                    changed = true;
                }

                if (changed)
                {
                    // Сохраняем изменения
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    Debug.Log($"[FixSkillIcons] ✅ Обработана: {Path.GetFileName(path)}");
                    processedCount++;
                }
                else
                {
                    Debug.Log($"[FixSkillIcons] ⏭️ Пропущена (уже настроена): {Path.GetFileName(path)}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FixSkillIcons] ❌ Ошибка при обработке {path}: {e.Message}");
                errorCount++;
            }
        }

        // Обновляем AssetDatabase
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[FixSkillIcons] ✅ ГОТОВО! Обработано: {processedCount}, Ошибок: {errorCount}");
        Debug.Log($"[FixSkillIcons] Теперь черный фон должен стать прозрачным!");
    }

    /// <summary>
    /// Дополнительная обработка - убирает черный фон программно (если Import Settings не помогли)
    /// Внимание: это изменит сами PNG файлы!
    /// </summary>
    [MenuItem("Tools/Aetherion/Fix Skill Icons (Force Remove Black Background)")]
    public static void ForceRemoveBlackBackground()
    {
        if (!EditorUtility.DisplayDialog(
            "Внимание!",
            "Этот инструмент изменит сами PNG файлы, заменив черные пиксели на прозрачные.\n\n" +
            "Рекомендуется сначала сделать backup иконок!\n\n" +
            "Продолжить?",
            "Да, продолжить",
            "Отмена"))
        {
            return;
        }

        Debug.Log("[ForceRemoveBlack] Начинаю принудительное удаление черного фона...");

        string[] iconGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/UI/Icons" });
        int processedCount = 0;

        foreach (string guid in iconGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (!path.EndsWith(".png"))
            {
                continue; // Работаем только с PNG
            }

            try
            {
                // Загружаем текстуру
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null) continue;

                // Создаем новую текстуру с альфа-каналом
                Texture2D newTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);

                // Копируем пиксели, заменяя черные на прозрачные
                Color[] pixels = texture.GetPixels();
                float threshold = 0.1f; // Порог для определения "черного" цвета

                for (int i = 0; i < pixels.Length; i++)
                {
                    Color pixel = pixels[i];

                    // Если пиксель черный или почти черный
                    if (pixel.r < threshold && pixel.g < threshold && pixel.b < threshold)
                    {
                        pixels[i] = new Color(0, 0, 0, 0); // Делаем прозрачным
                    }
                }

                newTexture.SetPixels(pixels);
                newTexture.Apply();

                // Сохраняем обратно в PNG
                byte[] pngData = newTexture.EncodeToPNG();
                string fullPath = Path.Combine(Application.dataPath, path.Replace("Assets/", ""));
                File.WriteAllBytes(fullPath, pngData);

                Debug.Log($"[ForceRemoveBlack] ✅ Обработана: {Path.GetFileName(path)}");
                processedCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ForceRemoveBlack] ❌ Ошибка: {path} - {e.Message}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[ForceRemoveBlack] ✅ ГОТОВО! Обработано: {processedCount} иконок");
    }
}
