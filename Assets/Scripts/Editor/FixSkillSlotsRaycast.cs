using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Исправляет Raycast Target на всех SkillSlot Icon Image
/// </summary>
public class FixSkillSlotsRaycast : EditorWindow
{
    [MenuItem("Tools/Mobile/Fix Skill Slots Raycast Target")]
    public static void ShowWindow()
    {
        FixAllRaycastTargets();
    }

    static void FixAllRaycastTargets()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Нужен Play Mode!",
                "⚠️ Запустите игру и зайдите в арену!",
                "OK"
            );
            return;
        }

        // Находим все SkillSlotBar
        SkillSlotBar[] allSlots = GameObject.FindObjectsOfType<SkillSlotBar>();

        if (allSlots.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Не найдено",
                "❌ SkillSlotBar не найдены!",
                "OK"
            );
            return;
        }

        Debug.Log($"🔍 Найдено SkillSlotBar: {allSlots.Length}\n");

        int fixedCount = 0;

        foreach (SkillSlotBar slot in allSlots)
        {
            // Находим все Image в слоте
            Image[] images = slot.GetComponentsInChildren<Image>();

            foreach (Image img in images)
            {
                if (!img.raycastTarget)
                {
                    img.raycastTarget = true;
                    Debug.Log($"✅ Включен Raycast Target: {slot.gameObject.name}/{img.gameObject.name}");
                    fixedCount++;
                }
            }
        }

        string message = $"✅ Исправлено Image: {fixedCount}\n" +
                        $"Проверено слотов: {allSlots.Length}\n\n" +
                        "Теперь клики должны работать!";

        EditorUtility.DisplayDialog("Готово!", message, "OK");

        Debug.Log($"\n✅ ИСПРАВЛЕНИЕ ЗАВЕРШЕНО: {fixedCount} Image\n");
    }
}
