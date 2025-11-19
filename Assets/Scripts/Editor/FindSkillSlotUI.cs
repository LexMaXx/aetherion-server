using UnityEngine;
using UnityEditor;

/// <summary>
/// Поиск всех SkillSlotUI объектов в сцене
/// </summary>
public class FindSkillSlotUI : EditorWindow
{
    [MenuItem("Tools/Mobile/Find SkillSlotUI Objects")]
    public static void ShowWindow()
    {
        FindAllSkillSlots();
    }

    static void FindAllSkillSlots()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Нужен Play Mode!",
                "⚠️ Запустите игру и зайдите в арену!\n\nSkillSlotUI создаются динамически.",
                "OK"
            );
            return;
        }

        // Находим все SkillSlotUI
        SkillSlotUI[] allSlots = GameObject.FindObjectsOfType<SkillSlotUI>();

        if (allSlots.Length == 0)
        {
            Debug.LogError("❌ НЕ НАЙДЕНО ни одного SkillSlotUI в сцене!");
            EditorUtility.DisplayDialog(
                "Не найдено",
                "❌ SkillSlotUI объекты не найдены!\n\nВы зашли в арену?",
                "OK"
            );
            return;
        }

        Debug.Log($"🔍 ====== Найдено SkillSlotUI объектов: {allSlots.Length} ======\n");

        foreach (SkillSlotUI slot in allSlots)
        {
            UnityEngine.UI.Button button = slot.GetComponent<UnityEngine.UI.Button>();
            string buttonStatus = button != null ? "✅ Button ЕСТЬ" : "❌ Button НЕТ";

            string path = GetGameObjectPath(slot.gameObject);

            Debug.Log($"  📍 {slot.gameObject.name}\n" +
                     $"     Путь: {path}\n" +
                     $"     Статус: {buttonStatus}\n",
                     slot.gameObject);

            // Автоматически выделяем первый объект
            if (slot == allSlots[0])
            {
                Selection.activeGameObject = slot.gameObject;
                EditorGUIUtility.PingObject(slot.gameObject);
            }
        }

        Debug.Log($"====== Конец списка ======\n");

        string message = $"✅ Найдено объектов: {allSlots.Length}\n\n" +
                        "Проверьте Console - там полный список с кликабельными ссылками.\n\n" +
                        $"Первый объект выделен в Hierarchy: {allSlots[0].gameObject.name}";

        EditorUtility.DisplayDialog("Результат поиска", message, "OK");
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
