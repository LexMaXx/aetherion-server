using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Принудительно убирает белый и черный фон со ВСЕХ иконок
/// Без проверок - просто делает все белые/черные пиксели прозрачными
/// </summary>
public class ForceRemoveIconBackground : Editor
{
    [MenuItem("Tools/Aetherion/FORCE Remove All Backgrounds")]
    public static void ForceRemoveAll()
    {
        if (!EditorUtility.DisplayDialog(
            "⚠️ ПРИНУДИТЕЛЬНОЕ удаление фона",
            "Этот инструмент обработает ВСЕ иконки в Assets/UI/Icons\n\n" +
            "Все белые и черные пиксели станут прозрачными!\n\n" +
            "ВАЖНО: Сделай backup перед использованием!\n\n" +
            "Продолжить?",
            "ДА, удалить весь фон",
            "Отмена"))
        {
            return;
        }

        Debug.Log("[ForceRemoveBackground] ⚡ ПРИНУДИТЕЛЬНАЯ обработка всех иконок...");

        string[] iconGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/UI/Icons" });
        int processedCount = 0;

        foreach (string guid in iconGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Только PNG файлы
            if (!path.EndsWith(".png"))
            {
                Debug.Log($"[ForceRemoveBackground] ⏭️ Пропущен (не PNG): {Path.GetFileName(path)}");
                continue;
            }

            try
            {
                Debug.Log($"[ForceRemoveBackground] 🔄 Обрабатываю: {path}");

                // 1. Настраиваем импорт для чтения
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.isReadable = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.alphaIsTransparency = true;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }

                // 2. Загружаем текстуру
                Texture2D originalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (originalTexture == null)
                {
                    Debug.LogError($"[ForceRemoveBackground] ❌ Не удалось загрузить: {path}");
                    continue;
                }

                int width = originalTexture.width;
                int height = originalTexture.height;

                Debug.Log($"[ForceRemoveBackground]   Размер: {width}x{height}");

                // 3. Создаём новую текстуру
                Texture2D newTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Color[] pixels = originalTexture.GetPixels();

                int removedPixels = 0;

                // 4. Обрабатываем каждый пиксель
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color pixel = pixels[i];

                    // Проверяем: белый или черный?
                    bool isWhite = pixel.r > 0.9f && pixel.g > 0.9f && pixel.b > 0.9f;
                    bool isBlack = pixel.r < 0.1f && pixel.g < 0.1f && pixel.b < 0.1f;

                    if (isWhite || isBlack)
                    {
                        // Делаем прозрачным
                        pixels[i] = new Color(pixel.r, pixel.g, pixel.b, 0f);
                        removedPixels++;
                    }
                }

                Debug.Log($"[ForceRemoveBackground]   Удалено пикселей: {removedPixels} из {pixels.Length}");

                // 5. Применяем изменения
                newTexture.SetPixels(pixels);
                newTexture.Apply();

                // 6. Сохраняем PNG
                byte[] pngData = newTexture.EncodeToPNG();
                string fullPath = Path.Combine(Application.dataPath, path.Replace("Assets/", ""));

                File.WriteAllBytes(fullPath, pngData);

                Debug.Log($"[ForceRemoveBackground] ✅ ГОТОВО: {Path.GetFileName(path)}");
                processedCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ForceRemoveBackground] ❌ ОШИБКА при обработке {path}:\n{e.Message}\n{e.StackTrace}");
            }
        }

        // 7. Обновляем базу данных
        AssetDatabase.Refresh();

        Debug.Log($"[ForceRemoveBackground] ✅✅✅ ВСЁ ГОТОВО! Обработано: {processedCount} иконок");
        Debug.Log("[ForceRemoveBackground] Белый и черный фон удалён. Проверь иконки!");
    }
}
