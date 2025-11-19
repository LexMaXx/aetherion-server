using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor скрипт для автоматического добавления SkillButton ко всем skill slots в ArenaScene
/// </summary>
public class AddSkillButtons : EditorWindow
{
    [MenuItem("Aetherion/Add SkillButtons to Arena")]
    public static void ShowWindow()
    {
        GetWindow<AddSkillButtons>("Add Skill Buttons");
    }

    void OnGUI()
    {
        GUILayout.Label("Add SkillButton Components", EditorStyles.boldLabel);
        GUILayout.Label("Автоматически добавляет SkillButton ко всем skill slots", EditorStyles.helpBox);

        EditorGUILayout.Space(20);

        if (GUILayout.Button("🔧 Добавить SkillButton ко всем слотам", GUILayout.Height(50)))
        {
            AddSkillButtonsToSlots();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("🗑️ Удалить все SkillButton компоненты", GUILayout.Height(40)))
        {
            RemoveSkillButtonsFromSlots();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("🔍 Показать все skill slots в сцене", GUILayout.Height(40)))
        {
            ShowAllSkillSlots();
        }

        EditorGUILayout.Space(20);

        EditorGUILayout.HelpBox("ИНСТРУКЦИЯ:\n\n1. Открой ArenaScene\n2. Нажми 'Добавить SkillButton ко всем слотам'\n3. Проверь Unity Console для деталей\n4. Сохрани сцену (Ctrl+S)", MessageType.Info);
    }

    /// <summary>
    /// Добавить SkillButton ко всем skill slots в сцене
    /// </summary>
    private void AddSkillButtonsToSlots()
    {
        if (!Application.isPlaying && !EditorApplication.isPlaying)
        {
            Debug.Log("=== ДОБАВЛЕНИЕ SKILLBUTTON КО ВСЕМ СЛОТАМ ===");

            // Ищем все SkillSlotBar в сцене (старая система)
            SkillSlotBar[] skillSlotBars = FindObjectsOfType<SkillSlotBar>(true);
            Debug.Log($"[AddSkillButtons] Найдено {skillSlotBars.Length} SkillSlotBar компонентов");

            int addedCount = 0;
            int skippedCount = 0;

            foreach (var slotBar in skillSlotBars)
            {
                // Проверяем есть ли уже SkillButton
                SkillButton existingButton = slotBar.GetComponent<SkillButton>();
                if (existingButton != null)
                {
                    Debug.Log($"[AddSkillButtons] ⏭️ Пропуск {slotBar.gameObject.name} - уже есть SkillButton");
                    skippedCount++;
                    continue;
                }

                // Добавляем SkillButton
                SkillButton skillButton = slotBar.gameObject.AddComponent<SkillButton>();

                // Автоматически назначаем ссылки на UI компоненты
                AutoAssignReferences(skillButton, slotBar.gameObject);

                Debug.Log($"[AddSkillButtons] ✅ Добавлен SkillButton к {slotBar.gameObject.name}");
                addedCount++;

                // Отмечаем объект как изменённый
                EditorUtility.SetDirty(slotBar.gameObject);
            }

            Debug.Log($"=== ЗАВЕРШЕНО ===");
            Debug.Log($"Добавлено: {addedCount}");
            Debug.Log($"Пропущено (уже есть): {skippedCount}");

            if (addedCount > 0)
            {
                EditorUtility.DisplayDialog("Готово!",
                    $"SkillButton добавлен к {addedCount} слотам!\n\nНе забудь сохранить сцену (Ctrl+S)",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Готово!",
                    "Все слоты уже имеют SkillButton компоненты.",
                    "OK");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Ошибка",
                "Выйди из Play Mode перед использованием этого инструмента!",
                "OK");
        }
    }

    /// <summary>
    /// Автоматически назначить ссылки на UI компоненты
    /// </summary>
    private void AutoAssignReferences(SkillButton skillButton, GameObject slotObject)
    {
        // Получаем SerializedObject для редактирования через Editor API
        SerializedObject serializedButton = new SerializedObject(skillButton);

        // Ищем компоненты в детях
        Image[] images = slotObject.GetComponentsInChildren<Image>(true);
        TextMeshProUGUI[] texts = slotObject.GetComponentsInChildren<TextMeshProUGUI>(true);

        // Назначаем iconImage (первый Image который НЕ cooldown)
        foreach (var img in images)
        {
            if (!img.gameObject.name.ToLower().Contains("cooldown") &&
                !img.gameObject.name.ToLower().Contains("overlay"))
            {
                serializedButton.FindProperty("iconImage").objectReferenceValue = img;
                Debug.Log($"  → iconImage: {img.gameObject.name}");
                break;
            }
        }

        // Назначаем cooldownOverlay
        foreach (var img in images)
        {
            if (img.gameObject.name.ToLower().Contains("cooldown") ||
                img.gameObject.name.ToLower().Contains("overlay"))
            {
                serializedButton.FindProperty("cooldownOverlay").objectReferenceValue = img;
                Debug.Log($"  → cooldownOverlay: {img.gameObject.name}");
                break;
            }
        }

        // Назначаем cooldownText
        foreach (var txt in texts)
        {
            if (txt.gameObject.name.ToLower().Contains("cooldown"))
            {
                serializedButton.FindProperty("cooldownText").objectReferenceValue = txt;
                Debug.Log($"  → cooldownText: {txt.gameObject.name}");
                break;
            }
        }

        // Назначаем hotkeyText
        foreach (var txt in texts)
        {
            if (txt.gameObject.name.ToLower().Contains("hotkey") ||
                txt.gameObject.name.ToLower().Contains("key"))
            {
                serializedButton.FindProperty("hotkeyText").objectReferenceValue = txt;
                Debug.Log($"  → hotkeyText: {txt.gameObject.name}");
                break;
            }
        }

        // Определяем skillIndex из имени объекта
        string objectName = slotObject.name;
        int skillIndex = 0;

        // Пытаемся извлечь индекс из имени (например "SkillSlot_1" → 0)
        if (objectName.Contains("_"))
        {
            string[] parts = objectName.Split('_');
            if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out int index))
            {
                skillIndex = index - 1; // Индексы начинаются с 0
                Debug.Log($"  → skillIndex: {skillIndex} (из имени '{objectName}')");
            }
        }

        serializedButton.FindProperty("skillIndex").intValue = skillIndex;

        // Применяем изменения
        serializedButton.ApplyModifiedProperties();
    }

    /// <summary>
    /// Удалить все SkillButton компоненты из сцены
    /// </summary>
    private void RemoveSkillButtonsFromSlots()
    {
        if (!Application.isPlaying && !EditorApplication.isPlaying)
        {
            bool confirmed = EditorUtility.DisplayDialog("Подтверждение",
                "Удалить ВСЕ SkillButton компоненты из сцены?",
                "Да, удалить",
                "Отмена");

            if (!confirmed) return;

            Debug.Log("=== УДАЛЕНИЕ ВСЕХ SKILLBUTTON ===");

            SkillButton[] skillButtons = FindObjectsOfType<SkillButton>(true);
            int removedCount = skillButtons.Length;

            foreach (var btn in skillButtons)
            {
                Debug.Log($"[AddSkillButtons] 🗑️ Удалён SkillButton с {btn.gameObject.name}");
                DestroyImmediate(btn);
            }

            Debug.Log($"=== ЗАВЕРШЕНО: Удалено {removedCount} компонентов ===");

            EditorUtility.DisplayDialog("Готово!",
                $"Удалено {removedCount} SkillButton компонентов.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Ошибка",
                "Выйди из Play Mode перед использованием этого инструмента!",
                "OK");
        }
    }

    /// <summary>
    /// Показать все skill slots в сцене (дебаг)
    /// </summary>
    private void ShowAllSkillSlots()
    {
        Debug.Log("=== ВСЕ SKILL SLOTS В СЦЕНЕ ===");

        SkillSlotBar[] skillSlotBars = FindObjectsOfType<SkillSlotBar>(true);
        Debug.Log($"SkillSlotBar компонентов: {skillSlotBars.Length}");

        foreach (var slot in skillSlotBars)
        {
            SkillButton btn = slot.GetComponent<SkillButton>();
            string status = btn != null ? "✅ Есть SkillButton" : "❌ НЕТ SkillButton";
            Debug.Log($"  → {slot.gameObject.name} - {status}");
        }

        SkillButton[] skillButtons = FindObjectsOfType<SkillButton>(true);
        Debug.Log($"\nSkillButton компонентов: {skillButtons.Length}");

        foreach (var btn in skillButtons)
        {
            Debug.Log($"  → {btn.gameObject.name}");
        }

        Debug.Log("=================================");

        EditorUtility.DisplayDialog("Дебаг",
            $"Найдено:\n{skillSlotBars.Length} SkillSlotBar\n{skillButtons.Length} SkillButton\n\nСмотри Unity Console для деталей.",
            "OK");
    }
}
