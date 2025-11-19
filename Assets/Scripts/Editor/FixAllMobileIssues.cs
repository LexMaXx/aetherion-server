using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// ИСПРАВЛЯЕТ ВСЁ ОДНОЙ КНОПКОЙ:
/// 1. Canvas Sorting Order
/// 2. Raycast Target на Images
/// 3. EventSystem
/// </summary>
public class FixAllMobileIssues : EditorWindow
{
    [MenuItem("Tools/Mobile/🔥 FIX ALL MOBILE ISSUES NOW! 🔥")]
    public static void ShowWindow()
    {
        FixEverything();
    }

    static void FixEverything()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Нужен Play Mode!",
                "⚠️ Запустите игру и зайдите в арену!\n\nПотом нажмите эту кнопку снова.",
                "OK"
            );
            return;
        }

        Debug.Log("🔥 ===== ИСПРАВЛЕНИЕ ВСЕХ ПРОБЛЕМ ===== 🔥\n");

        int totalFixed = 0;

        // 1. Исправляем Canvas Sorting Order
        totalFixed += FixCanvasSortingOrder();

        // 2. Исправляем Raycast Target
        totalFixed += FixRaycastTargets();

        // 3. Проверяем EventSystem
        CheckEventSystem();

        Debug.Log($"\n🔥 ===== ИСПРАВЛЕНО: {totalFixed} проблем =====\n");

        EditorUtility.DisplayDialog(
            "✅ ВСЁ ИСПРАВЛЕНО!",
            $"✅ Исправлено проблем: {totalFixed}\n\n" +
            "Теперь:\n" +
            "1. Кликните на скилл\n" +
            "2. Свайпните правой частью экрана\n\n" +
            "Должно работать!",
            "OK"
        );
    }

    static int FixCanvasSortingOrder()
    {
        Debug.Log("\n📋 1. ИСПРАВЛЕНИЕ CANVAS SORTING ORDER\n");

        Canvas[] allCanvases = GameObject.FindObjectsOfType<Canvas>();
        int fixedCount = 0;

        foreach (Canvas canvas in allCanvases)
        {
            string canvasName = canvas.gameObject.name;
            int oldOrder = canvas.sortingOrder;
            int newOrder = oldOrder;

            if (canvasName.Contains("MobileControls") || canvasName.Contains("Joystick"))
            {
                newOrder = 5; // Джойстик
            }
            else if (canvasName.Contains("SkillBar") || canvasName.Contains("Skill"))
            {
                newOrder = 10; // Скиллы ВЫШЕ джойстика
            }
            else if (canvasName.Contains("StatusBar") || canvasName.Contains("HP") || canvasName.Contains("MP"))
            {
                newOrder = 15; // Бары ВЫШЕ всего
            }

            if (oldOrder != newOrder)
            {
                canvas.sortingOrder = newOrder;
                Debug.Log($"   ✅ {canvasName}: {oldOrder} → {newOrder}");
                fixedCount++;
            }
            else
            {
                Debug.Log($"   ✓ {canvasName}: {oldOrder} (OK)");
            }
        }

        Debug.Log($"\n   Исправлено Canvas: {fixedCount}\n");
        return fixedCount;
    }

    static int FixRaycastTargets()
    {
        Debug.Log("\n📋 2. ИСПРАВЛЕНИЕ RAYCAST TARGET\n");

        SkillSlotBar[] allSlots = GameObject.FindObjectsOfType<SkillSlotBar>();
        int fixedCount = 0;

        foreach (SkillSlotBar slot in allSlots)
        {
            Image[] images = slot.GetComponentsInChildren<Image>();

            foreach (Image img in images)
            {
                if (!img.raycastTarget)
                {
                    img.raycastTarget = true;
                    Debug.Log($"   ✅ Raycast ON: {slot.gameObject.name}/{img.gameObject.name}");
                    fixedCount++;
                }
            }
        }

        Debug.Log($"\n   Исправлено Image: {fixedCount}\n");
        return fixedCount;
    }

    static void CheckEventSystem()
    {
        Debug.Log("\n📋 3. ПРОВЕРКА EVENTSYSTEM\n");

        UnityEngine.EventSystems.EventSystem eventSystem = GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();

        if (eventSystem == null)
        {
            Debug.LogError("   ❌ EventSystem не найден! Клики не будут работать!");

            EditorUtility.DisplayDialog(
                "⚠️ EventSystem не найден!",
                "❌ В сцене нет EventSystem!\n\n" +
                "Добавьте его:\n" +
                "Hierarchy → Right Click → UI → Event System",
                "OK"
            );
        }
        else
        {
            Debug.Log($"   ✅ EventSystem найден: {eventSystem.gameObject.name}");
        }
    }
}
