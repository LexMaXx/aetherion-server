using UnityEngine;
using UnityEditor;

/// <summary>
/// Создаёт префаб стрелки для индикатора цели
/// </summary>
public class CreateTargetArrow : Editor
{
    [MenuItem("Tools/Target System/Create Target Arrow Prefab (Sprite)")]
    public static void CreateArrowPrefabWithSprite()
    {
        Debug.Log("[CreateTargetArrow] Создание префаба стрелки с Sprite Renderer...");

        // Создаём главный GameObject
        GameObject arrowObj = new GameObject("TargetArrow");

        // Создаём дочерний объект для спрайта
        GameObject spriteObj = new GameObject("ArrowSprite");
        spriteObj.transform.SetParent(arrowObj.transform);
        spriteObj.transform.localPosition = Vector3.zero;

        // Добавляем SpriteRenderer
        SpriteRenderer spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();

        // ВАЖНО: Ищем PNG стрелку в проекте автоматически
        string[] guids = AssetDatabase.FindAssets("t:Texture2D arrow", new[] { "Assets" });
        if (guids.Length == 0)
        {
            // Пробуем найти ChatGPT_Image
            guids = AssetDatabase.FindAssets("t:Texture2D ChatGPT", new[] { "Assets" });
        }

        Sprite arrowSprite = null;
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (texture != null)
            {
                // Настраиваем текстуру как Sprite если это еще не сделано
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }

                arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Debug.Log($"[CreateTargetArrow] ✅ Найдена стрелка: {path}");
            }
        }

        if (arrowSprite != null)
        {
            spriteRenderer.sprite = arrowSprite;
            Debug.Log($"[CreateTargetArrow] ✅ Спрайт установлен: {arrowSprite.name}");
        }
        else
        {
            Debug.LogWarning("[CreateTargetArrow] ⚠️ PNG стрелка не найдена автоматически!");
            Debug.LogWarning("[CreateTargetArrow] 💡 Вручную назначьте Sprite в префабе:");
            Debug.LogWarning("[CreateTargetArrow]    TargetArrow → ArrowSprite → Sprite Renderer → Sprite");
        }

        // Настраиваем SpriteRenderer
        spriteRenderer.color = new Color(1f, 0.2f, 0.2f, 1f); // Красный цвет
        spriteRenderer.sortingOrder = 1000; // Поверх всего

        // Поворачиваем спрайт вниз (стрелка указывает на врага)
        spriteObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
        spriteObj.transform.localScale = Vector3.one * 0.5f; // Размер стрелки

        // Добавляем Billboard эффект на главный объект (вращает к камере)
        Billboard billboard = arrowObj.AddComponent<Billboard>();

        // Добавляем анимацию подпрыгивания на главный объект
        TargetArrowAnimation anim = arrowObj.AddComponent<TargetArrowAnimation>();

        // Сохраняем как префаб
        string prefabPath = "Assets/Prefabs/UI/TargetArrow.prefab";
        System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");

        PrefabUtility.SaveAsPrefabAsset(arrowObj, prefabPath);

        Debug.Log($"[CreateTargetArrow] ✅ Префаб создан: {prefabPath}");
        Debug.Log($"[CreateTargetArrow] 📋 Структура:");
        Debug.Log($"[CreateTargetArrow]   TargetArrow (Billboard + Animation)");
        Debug.Log($"[CreateTargetArrow]     └─ ArrowSprite (SpriteRenderer)");

        // Удаляем временный объект из сцены
        DestroyImmediate(arrowObj);

        AssetDatabase.Refresh();

        // Выделяем префаб в Project
        Object prefab = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
    }
}
