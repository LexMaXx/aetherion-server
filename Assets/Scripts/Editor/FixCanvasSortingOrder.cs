using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Исправляет Sorting Order всех Canvas в Arena Scene
/// SkillBar должен быть ВЫШЕ MobileControlsCanvas!
/// </summary>
public class FixCanvasSortingOrder : EditorWindow
{
    [MenuItem("Tools/Mobile/Fix Canvas Sorting Order")]
    public static void ShowWindow()
    {
        FixAllCanvasSortingOrder();
    }

    static void FixAllCanvasSortingOrder()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Нужен Play Mode!",
                "⚠️ Запустите игру (Play Mode) и зайдите в арену!\n\n" +
                "Canvas'ы создаются динамически.",
                "OK"
            );
            return;
        }

        // Находим все Canvas
        Canvas[] allCanvases = GameObject.FindObjectsOfType<Canvas>();

        if (allCanvases.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Не найдено",
                "❌ Canvas не найдены!\n\nВы зашли в арену?",
                "OK"
            );
            return;
        }

        Debug.Log($"🔍 Найдено Canvas'ов: {allCanvases.Length}\n");

        int fixedCount = 0;

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
                newOrder = 10; // Скиллы - ВЫШЕ
            }
            else if (canvasName.Contains("StatusBar") || canvasName.Contains("HP") || canvasName.Contains("MP"))
            {
                newOrder = 15; // HP/MP бары - ЕЩЁ ВЫШЕ
            }
            else
            {
                newOrder = 0; // Остальные - базовый уровень
            }

            if (oldOrder != newOrder)
            {
                canvas.sortingOrder = newOrder;
                Debug.Log($"✅ {canvasName}: {oldOrder} → {newOrder}");
                fixedCount++;
            }
            else
            {
                Debug.Log($"✓ {canvasName}: {oldOrder} (не требует изменений)");
            }
        }

        string message = $"✅ Исправлено Canvas'ов: {fixedCount}\n" +
                        $"Всего проверено: {allCanvases.Length}\n\n" +
                        "Теперь кнопки скиллов должны работать!";

        EditorUtility.DisplayDialog("Готово!", message, "OK");

        Debug.Log($"\n✅ ИСПРАВЛЕНИЕ ЗАВЕРШЕНО: {fixedCount}/{allCanvases.Length}\n");
    }
}
