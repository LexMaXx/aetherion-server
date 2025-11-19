using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Editor скрипт для автоматической настройки Button компонентов на всех SkillSlot'ах
/// </summary>
public class SetupSkillSlotsButtons : EditorWindow
{
    [MenuItem("Tools/Setup Skill Slots Buttons")]
    public static void ShowWindow()
    {
        GetWindow<SetupSkillSlotsButtons>("Setup Skill Buttons");
    }

    void OnGUI()
    {
        GUILayout.Label("Автоматическая настройка Button для скиллов", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("Этот скрипт найдёт все SkillSlotUI компоненты\nи настроит Button.onClick для активации скиллов");
        GUILayout.Space(10);

        if (GUILayout.Button("✅ Настроить все Button'ы", GUILayout.Height(40)))
        {
            SetupAllSkillSlots();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("🔍 Найти SkillSlot'ы в сцене", GUILayout.Height(30)))
        {
            FindSkillSlots();
        }
    }

    static void SetupAllSkillSlots()
    {
        // Находим все SkillSlotUI в сцене
        SkillSlotUI[] allSlots = GameObject.FindObjectsOfType<SkillSlotUI>();

        if (allSlots.Length == 0)
        {
            EditorUtility.DisplayDialog("Ошибка", "Не найдено ни одного SkillSlotUI в сцене!\n\nЗапустите игру и попробуйте снова.", "OK");
            return;
        }

        int setupCount = 0;
        int skippedCount = 0;

        foreach (SkillSlotUI slot in allSlots)
        {
            // Получаем или добавляем Button
            Button button = slot.GetComponent<Button>();
            if (button == null)
            {
                button = slot.gameObject.AddComponent<Button>();
                Debug.Log($"[Setup] ✅ Добавлен Button к {slot.gameObject.name}");
            }

            // Настраиваем Button
            button.transition = Selectable.Transition.None;
            button.interactable = true;

            // Очищаем старые подписки
            button.onClick.RemoveAllListeners();

            // Добавляем новую подписку на ClickSkill()
            UnityEngine.Events.UnityAction action = new UnityEngine.Events.UnityAction(slot.ClickSkill);
            button.onClick.AddPersistentListener(action);

            // Помечаем объект как изменённый
            EditorUtility.SetDirty(button);
            EditorUtility.SetDirty(slot);

            setupCount++;
            Debug.Log($"[Setup] ✅ Button настроен: {slot.gameObject.name} → ClickSkill()");
        }

        // Сохраняем изменения
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        EditorUtility.DisplayDialog(
            "Готово!",
            $"✅ Настроено Button'ов: {setupCount}\n" +
            $"Пропущено: {skippedCount}\n\n" +
            "Теперь клики по скиллам будут работать!",
            "OK"
        );
    }

    static void FindSkillSlots()
    {
        SkillSlotUI[] allSlots = GameObject.FindObjectsOfType<SkillSlotUI>();

        if (allSlots.Length == 0)
        {
            Debug.LogWarning("❌ Не найдено ни одного SkillSlotUI в сцене! Запустите игру и попробуйте снова.");
            EditorUtility.DisplayDialog("Не найдено", "SkillSlotUI не найдены в сцене.\n\nВозможно нужно запустить игру?", "OK");
            return;
        }

        Debug.Log($"🔍 Найдено SkillSlotUI компонентов: {allSlots.Length}");

        foreach (SkillSlotUI slot in allSlots)
        {
            Button button = slot.GetComponent<Button>();
            string buttonStatus = button != null ? "✅ есть Button" : "❌ нет Button";

            Debug.Log($"  • {slot.gameObject.name} - {buttonStatus}");
        }

        EditorUtility.DisplayDialog(
            "Результат поиска",
            $"Найдено SkillSlotUI: {allSlots.Length}\n\nДетали в Console",
            "OK"
        );
    }
}

/// <summary>
/// Расширение для добавления persistent listener программно
/// </summary>
public static class UnityEventExtensions
{
    public static void AddPersistentListener(this UnityEngine.UI.Button.ButtonClickedEvent unityEvent, UnityEngine.Events.UnityAction call)
    {
        // Используем Reflection для добавления persistent listener
        var targetMethod = call.Method;
        var target = call.Target as UnityEngine.Object;

        if (target == null)
        {
            Debug.LogError("Target is null!");
            return;
        }

        // Создаём PersistentCall через SerializedObject
        var serializedObject = new SerializedObject(target);
        var persistentCallsProperty = serializedObject.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");

        // Добавляем новый элемент
        persistentCallsProperty.arraySize++;
        var lastCall = persistentCallsProperty.GetArrayElementAtIndex(persistentCallsProperty.arraySize - 1);

        lastCall.FindPropertyRelative("m_Target").objectReferenceValue = target;
        lastCall.FindPropertyRelative("m_MethodName").stringValue = targetMethod.Name;
        lastCall.FindPropertyRelative("m_Mode").enumValueIndex = 1; // EventDefined
        lastCall.FindPropertyRelative("m_CallState").enumValueIndex = 2; // RuntimeOnly

        serializedObject.ApplyModifiedProperties();
    }
}
