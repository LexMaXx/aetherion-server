using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.Events;

/// <summary>
/// Автоматическая настройка Button компонентов для SkillSlot'ов
/// </summary>
public class AutoSetupSkillButtons : EditorWindow
{
    [MenuItem("Tools/Mobile/Setup Skill Buttons")]
    public static void ShowWindow()
    {
        GetWindow<AutoSetupSkillButtons>("Skill Buttons Setup");
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Этот инструмент автоматически настроит Button компоненты\n" +
            "на всех SkillSlot'ах для активации скиллов кликом/тапом",
            MessageType.Info
        );

        GUILayout.Space(10);

        if (GUILayout.Button("✅ Настроить все Button'ы", GUILayout.Height(50)))
        {
            SetupAllButtons();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("🔍 Показать SkillSlot'ы в сцене", GUILayout.Height(30)))
        {
            ShowAllSkillSlots();
        }
    }

    static void SetupAllButtons()
    {
        // ВАЖНО: Работает только в режиме Play!
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Нужен Play Mode",
                "⚠️ Запустите игру (Play Mode) и зайдите в арену!\n\n" +
                "SkillSlot'ы создаются динамически при входе в игру.",
                "OK"
            );
            return;
        }

        // Находим все SkillSlotUI
        SkillSlotUI[] allSlots = GameObject.FindObjectsOfType<SkillSlotUI>();

        if (allSlots.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Не найдено",
                "❌ SkillSlot'ы не найдены!\n\n" +
                "Убедитесь что вы зашли в арену и UI видно на экране.",
                "OK"
            );
            return;
        }

        int successCount = 0;

        foreach (SkillSlotUI slot in allSlots)
        {
            // Получаем или добавляем Button
            Button button = slot.GetComponent<Button>();
            if (button == null)
            {
                button = slot.gameObject.AddComponent<Button>();
            }

            // Настраиваем параметры
            button.transition = Selectable.Transition.None;
            button.interactable = true;

            // Очищаем onClick
            button.onClick.RemoveAllListeners();

            // Добавляем listener ПРОГРАММНО (работает в Runtime)
            button.onClick.AddListener(() => slot.ClickSkill());

            successCount++;
            Debug.Log($"[AutoSetup] ✅ Button настроен: {slot.gameObject.name} → ClickSkill()");
        }

        EditorUtility.DisplayDialog(
            "Готово!",
            $"✅ Настроено Button'ов: {successCount}\n\n" +
            "Попробуйте кликнуть на иконку скилла!",
            "OK"
        );
    }

    static void ShowAllSkillSlots()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Нужен Play Mode",
                "Запустите игру чтобы увидеть SkillSlot'ы",
                "OK"
            );
            return;
        }

        SkillSlotUI[] allSlots = GameObject.FindObjectsOfType<SkillSlotUI>();

        if (allSlots.Length == 0)
        {
            Debug.LogWarning("❌ SkillSlot'ы не найдены");
            return;
        }

        Debug.Log($"🔍 Найдено SkillSlot'ов: {allSlots.Length}\n");

        foreach (SkillSlotUI slot in allSlots)
        {
            Button button = slot.GetComponent<Button>();
            string status = button != null ? "✅ Button есть" : "❌ Button нет";

            Debug.Log($"  • {slot.gameObject.name} - {status}", slot.gameObject);
        }
    }
}
