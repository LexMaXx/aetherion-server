using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Исправляет Canvas в Arena Scene БЕЗ Play Mode
/// Открывает сцену, исправляет, сохраняет
/// </summary>
public class FixArenaSceneCanvas
{
    [MenuItem("Tools/Mobile/Fix Arena Scene Canvas (NO PLAY MODE)")]
    public static void FixArenaCanvas()
    {
        // Открываем Arena Scene
        Scene arenaScene = EditorSceneManager.OpenScene("Assets/Scenes/ArenaScene.unity", OpenSceneMode.Single);

        if (!arenaScene.IsValid())
        {
            EditorUtility.DisplayDialog("Ошибка", "❌ Не удалось открыть ArenaScene.unity!", "OK");
            return;
        }

        Debug.Log("🔍 Arena Scene открыта, ищу Canvas'ы...\n");

        // Находим все Canvas в открытой сцене
        Canvas[] allCanvases = GameObject.FindObjectsOfType<Canvas>();

        if (allCanvases.Length == 0)
        {
            EditorUtility.DisplayDialog("Не найдено", "❌ Canvas не найдены в Arena Scene!", "OK");
            return;
        }

        Debug.Log($"🔍 Найдено Canvas'ов: {allCanvases.Length}\n");

        int fixedCount = 0;
        bool sceneModified = false;

        foreach (Canvas canvas in allCanvases)
        {
            string canvasName = canvas.gameObject.name;
            int oldOrder = canvas.sortingOrder;
            int newOrder = oldOrder;

            // Определяем правильный Sorting Order
            if (canvasName.Contains("MobileControls") || canvasName.Contains("Joystick"))
            {
                newOrder = 5; // Джойстик - НИЖЕ
            }
            else if (canvasName.Contains("SkillBar") || canvasName.Contains("Skill"))
            {
                newOrder = 10; // Скиллы - ВЫШЕ джойстика
            }
            else if (canvasName.Contains("StatusBar") || canvasName.Contains("HP") || canvasName.Contains("MP"))
            {
                newOrder = 15; // HP/MP бары - ЕЩЁ ВЫШЕ
            }
            else if (canvasName == "Canvas") // Главный Canvas
            {
                newOrder = 0; // Базовый уровень
            }

            if (oldOrder != newOrder)
            {
                Undo.RecordObject(canvas, "Fix Canvas Sorting Order");
                canvas.sortingOrder = newOrder;
                EditorUtility.SetDirty(canvas);
                sceneModified = true;

                Debug.Log($"✅ {canvasName}: Sorting Order {oldOrder} → {newOrder}");
                fixedCount++;
            }
            else
            {
                Debug.Log($"✓ {canvasName}: Sorting Order {oldOrder} (не требует изменений)");
            }
        }

        if (sceneModified)
        {
            // Сохраняем сцену
            EditorSceneManager.SaveScene(arenaScene);
            Debug.Log($"\n✅ СЦЕНА СОХРАНЕНА: {fixedCount} Canvas'ов исправлено!\n");

            EditorUtility.DisplayDialog(
                "Готово!",
                $"✅ Исправлено Canvas'ов: {fixedCount}\n\n" +
                "Arena Scene сохранена!\n\n" +
                "Теперь запустите игру и проверьте кнопки скиллов.",
                "OK"
            );
        }
        else
        {
            Debug.Log("✓ Все Canvas уже имеют правильный Sorting Order\n");

            EditorUtility.DisplayDialog(
                "Не требуется",
                "✓ Все Canvas уже правильно настроены!\n\n" +
                "Sorting Order не изменён.",
                "OK"
            );
        }
    }

    [MenuItem("Tools/Mobile/Show All Canvas Info")]
    public static void ShowCanvasInfo()
    {
        // Открываем Arena Scene
        Scene arenaScene = EditorSceneManager.OpenScene("Assets/Scenes/ArenaScene.unity", OpenSceneMode.Single);

        Canvas[] allCanvases = GameObject.FindObjectsOfType<Canvas>();

        Debug.Log($"📋 ===== CANVAS INFO =====\n");
        Debug.Log($"Найдено Canvas'ов: {allCanvases.Length}\n");

        foreach (Canvas canvas in allCanvases)
        {
            string path = GetGameObjectPath(canvas.gameObject);
            Debug.Log($"Canvas: {canvas.gameObject.name}\n" +
                     $"  Path: {path}\n" +
                     $"  Sorting Order: {canvas.sortingOrder}\n" +
                     $"  Render Mode: {canvas.renderMode}\n");
        }

        Debug.Log($"===== END =====\n");

        string info = $"Всего Canvas: {allCanvases.Length}\n\nДетали в Console";
        EditorUtility.DisplayDialog("Canvas Info", info, "OK");
    }

    static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}
