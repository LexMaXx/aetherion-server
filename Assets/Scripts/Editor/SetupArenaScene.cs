using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Автоматическая настройка Arena Scene для работы с 5 скиллами
/// Аналогично SetupSkillTestScene
/// </summary>
public class SetupArenaScene : EditorWindow
{
    [MenuItem("Tools/Arena/Setup Arena Scene (5 Skills)")]
    public static void SetupArena()
    {
        Debug.Log("🏟️ ========== НАСТРОЙКА ARENA SCENE ==========");

        // Открываем Arena сцену
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/ArenaScene.unity");

        // ═══════════════════════════════════════════════════════════
        // ЧАСТЬ 1: НАСТРОЙКА SKILL BAR UI (5 СЛОТОВ)
        // ═══════════════════════════════════════════════════════════

        Debug.Log("\n[1/3] Настройка Skill Bar UI...");

        GameObject skillBarObj = GameObject.Find("SkillBar");
        if (skillBarObj == null)
        {
            skillBarObj = GameObject.Find("Canvas/SkillBar");
        }
        if (skillBarObj == null)
        {
            skillBarObj = GameObject.Find("UI/SkillBar");
        }

        if (skillBarObj == null)
        {
            Debug.LogError("❌ SkillBar не найден в сцене!");
            Debug.LogError("Создайте объект Canvas → SkillBar вручную");
            return;
        }

        Debug.Log($"✅ Найден SkillBar: {skillBarObj.name}");

        // Проверяем сколько слотов уже есть
        Transform[] existingSlots = new Transform[5];
        int existingCount = 0;

        for (int i = 0; i < 5; i++)
        {
            Transform slot = skillBarObj.transform.Find($"SkillSlot_{i}");
            if (slot != null)
            {
                existingSlots[i] = slot;
                existingCount++;
            }
        }

        Debug.Log($"Найдено существующих слотов: {existingCount}/5");

        // Создаём недостающие слоты
        if (existingCount < 5)
        {
            Debug.Log($"Создаю недостающие слоты ({5 - existingCount})...");

            // Если есть хотя бы один слот - используем его как шаблон
            Transform templateSlot = existingSlots[0];
            if (templateSlot == null)
            {
                // Создаём слоты с нуля
                for (int i = 0; i < 5; i++)
                {
                    if (existingSlots[i] == null)
                    {
                        GameObject newSlot = CreateSkillSlot(skillBarObj.transform, i);
                        existingSlots[i] = newSlot.transform;
                        Debug.Log($"  ✅ Создан SkillSlot_{i} (новый)");
                    }
                }
            }
            else
            {
                // Дублируем существующий слот
                for (int i = 0; i < 5; i++)
                {
                    if (existingSlots[i] == null)
                    {
                        GameObject newSlot = Instantiate(templateSlot.gameObject, skillBarObj.transform);
                        newSlot.name = $"SkillSlot_{i}";

                        // Позиционируем слот (горизонтально)
                        RectTransform rt = newSlot.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            rt.anchoredPosition = new Vector2(i * 70, 0); // 70 пикселей между слотами
                        }

                        existingSlots[i] = newSlot.transform;
                        Debug.Log($"  ✅ Создан SkillSlot_{i} (из шаблона)");
                    }
                }
            }
        }
        else
        {
            Debug.Log("✅ Все 5 слотов уже существуют");
        }

        // Настраиваем SkillBarUI компонент
        SkillBarUI skillBarUI = skillBarObj.GetComponent<SkillBarUI>();
        if (skillBarUI == null)
        {
            skillBarUI = skillBarObj.AddComponent<SkillBarUI>();
            Debug.Log("✅ Добавлен компонент SkillBarUI");
        }

        // Назначаем слоты в массив
        SerializedObject so = new SerializedObject(skillBarUI);
        SerializedProperty skillSlotsProp = so.FindProperty("skillSlots");
        skillSlotsProp.arraySize = 5;

        for (int i = 0; i < 5; i++)
        {
            SerializedProperty element = skillSlotsProp.GetArrayElementAtIndex(i);
            element.objectReferenceValue = existingSlots[i].GetComponent<Image>();

            if (element.objectReferenceValue == null)
            {
                Debug.LogWarning($"⚠️ SkillSlot_{i} не имеет Image компонента!");
            }
        }

        so.ApplyModifiedProperties();
        Debug.Log("✅ Слоты назначены в SkillBarUI.skillSlots[]");

        // ═══════════════════════════════════════════════════════════
        // ЧАСТЬ 2: НАСТРОЙКА ИГРОКА (КОМПОНЕНТЫ)
        // ═══════════════════════════════════════════════════════════

        Debug.Log("\n[2/3] Настройка компонентов игрока...");

        // Ищем игрока в сцене
        GameObject player = FindPlayerInScene();

        if (player == null)
        {
            Debug.LogError("❌ Игрок не найден в сцене!");
            Debug.LogError("Убедитесь что в Arena Scene есть объект с тегом 'Player'");
            Debug.LogError("Или объект с именем содержащим 'Player', 'Warrior', 'Mage', 'Archer', 'Paladin', 'Rogue'");
            return;
        }

        Debug.Log($"✅ Найден игрок: {player.name}");

        // Проверяем/добавляем необходимые компоненты
        EnsureComponent<SkillExecutor>(player, "SkillExecutor");
        EnsureComponent<EffectManager>(player, "EffectManager");

        PlayerAttackNew attackNew = EnsureComponent<PlayerAttackNew>(player, "PlayerAttackNew");

        // Включаем PlayerAttackNew если он был отключен
        SerializedObject playerAttackSO = new SerializedObject(attackNew);
        playerAttackSO.Update();
        // Компонент включается автоматически при добавлении
        playerAttackSO.ApplyModifiedProperties();

        // Проверяем SkillManager
        SkillManager skillManager = EnsureComponent<SkillManager>(player, "SkillManager");

        Debug.Log("✅ Все необходимые компоненты добавлены/проверены");

        // ═══════════════════════════════════════════════════════════
        // ЧАСТЬ 3: ПРОВЕРКА ATTACK CONFIG
        // ═══════════════════════════════════════════════════════════

        Debug.Log("\n[3/3] Проверка Attack Config...");

        // Определяем класс персонажа по имени объекта
        string characterClass = DetermineCharacterClass(player.name);
        Debug.Log($"Определён класс: {characterClass}");

        // Путь к BasicAttackConfig
        string configPath = $"skill old/BasicAttackConfig_{characterClass}";
        BasicAttackConfig attackConfig = Resources.Load<BasicAttackConfig>(configPath);

        if (attackConfig != null)
        {
            Debug.Log($"✅ BasicAttackConfig найден: {configPath}");

            // Назначаем в PlayerAttackNew (если поле есть)
            SerializedObject attackNewSO = new SerializedObject(attackNew);
            SerializedProperty attackConfigProp = attackNewSO.FindProperty("attackConfig");
            if (attackConfigProp != null)
            {
                attackConfigProp.objectReferenceValue = attackConfig;
                attackNewSO.ApplyModifiedProperties();
                Debug.Log("✅ Attack Config назначен в PlayerAttackNew");
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ BasicAttackConfig не найден по пути: Resources/{configPath}");
            Debug.LogWarning("Назначьте Attack Config вручную в Inspector");
        }

        // ═══════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ СЦЕНЫ
        // ═══════════════════════════════════════════════════════════

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("\n✅ ========== ARENA SCENE НАСТРОЕНА! ==========");
        Debug.Log("📋 Что было сделано:");
        Debug.Log("  ✅ Skill Bar: 5 слотов (SkillSlot_0 до SkillSlot_4)");
        Debug.Log("  ✅ Компоненты игрока: SkillExecutor, EffectManager, PlayerAttackNew");
        Debug.Log("  ✅ PlayerAttackNew включен и готов к работе");
        Debug.Log($"  ✅ Игрок: {player.name}");
        Debug.Log("\n📋 Следующие шаги:");
        Debug.Log("  1. Зайдите в CharacterSelection сцену");
        Debug.Log("  2. Выберите класс и скиллы");
        Debug.Log("  3. Вернитесь в Arena - скиллы загрузятся автоматически");
        Debug.Log("  4. Нажмите клавиши 1-5 для использования скиллов");
        Debug.Log("════════════════════════════════════════════════════════════");

        // Выделяем SkillBar для удобства
        Selection.activeGameObject = skillBarObj;
        EditorGUIUtility.PingObject(skillBarObj);
    }

    // ═══════════════════════════════════════════════════════════════
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Ищет игрока в сцене по тегу или имени
    /// </summary>
    private static GameObject FindPlayerInScene()
    {
        // Сначала ищем по тегу Player
        try
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) return player;
        }
        catch
        {
            // Тег Player может не существовать
        }

        // Ищем по имени (модели классов)
        string[] possibleNames = { "Player", "WarriorModel", "MageModel", "ArcherModel", "PaladinModel", "RogueModel" };

        foreach (string name in possibleNames)
        {
            GameObject player = GameObject.Find(name);
            if (player != null) return player;
        }

        // Ищем объекты содержащие эти слова в имени
        // Используем FindObjectsOfType с Transform вместо GameObject
        Transform[] allTransforms = Object.FindObjectsOfType<Transform>();
        foreach (Transform t in allTransforms)
        {
            foreach (string keyword in possibleNames)
            {
                if (t.name.Contains(keyword))
                {
                    return t.gameObject;
                }
            }
        }

        // Последняя попытка - ищем объект с CharacterController
        CharacterController[] controllers = Object.FindObjectsOfType<CharacterController>();
        if (controllers.Length > 0)
        {
            Debug.Log($"Найден объект с CharacterController: {controllers[0].gameObject.name}");
            return controllers[0].gameObject;
        }

        return null;
    }

    /// <summary>
    /// Определяет класс персонажа по имени объекта
    /// </summary>
    private static string DetermineCharacterClass(string objectName)
    {
        if (objectName.Contains("Warrior")) return "Warrior";
        if (objectName.Contains("Mage")) return "Mage";
        if (objectName.Contains("Archer")) return "Archer";
        if (objectName.Contains("Paladin")) return "Paladin";
        if (objectName.Contains("Rogue")) return "Rogue";

        // По умолчанию
        return "Warrior";
    }

    /// <summary>
    /// Проверяет наличие компонента и добавляет если нет
    /// </summary>
    private static T EnsureComponent<T>(GameObject obj, string componentName) where T : Component
    {
        T component = obj.GetComponent<T>();
        if (component == null)
        {
            component = obj.AddComponent<T>();
            Debug.Log($"  ✅ Добавлен компонент: {componentName}");
        }
        else
        {
            Debug.Log($"  ℹ️ Компонент уже есть: {componentName}");
        }
        return component;
    }

    /// <summary>
    /// Создаёт UI слот для скилла с нуля
    /// </summary>
    private static GameObject CreateSkillSlot(Transform parent, int index)
    {
        GameObject slot = new GameObject($"SkillSlot_{index}");
        slot.transform.SetParent(parent);

        // RectTransform
        RectTransform rt = slot.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60, 60);
        rt.anchoredPosition = new Vector2(index * 70, 0);

        // Image (фон слота)
        Image bg = slot.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Тёмно-серый полупрозрачный

        // Создаём иконку скилла (дочерний объект)
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slot.transform);

        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.sizeDelta = Vector2.zero;
        iconRt.anchoredPosition = Vector2.zero;

        Image icon = iconObj.AddComponent<Image>();
        icon.color = Color.white;

        // Создаём текст хоткея
        GameObject hotkeyObj = new GameObject("Hotkey");
        hotkeyObj.transform.SetParent(slot.transform);

        RectTransform hotkeyRt = hotkeyObj.AddComponent<RectTransform>();
        hotkeyRt.anchorMin = new Vector2(0, 0);
        hotkeyRt.anchorMax = new Vector2(1, 0);
        hotkeyRt.pivot = new Vector2(0.5f, 0);
        hotkeyRt.sizeDelta = new Vector2(0, 20);
        hotkeyRt.anchoredPosition = new Vector2(0, 2);

        TextMeshProUGUI hotkeyText = hotkeyObj.AddComponent<TextMeshProUGUI>();
        hotkeyText.text = (index + 1).ToString();
        hotkeyText.fontSize = 14;
        hotkeyText.alignment = TextAlignmentOptions.Center;
        hotkeyText.color = Color.white;

        return slot;
    }

    // ═══════════════════════════════════════════════════════════════
    // ДОПОЛНИТЕЛЬНАЯ УТИЛИТА: ПРОВЕРКА НАСТРОЙКИ
    // ═══════════════════════════════════════════════════════════════

    [MenuItem("Tools/Arena/Check Arena Setup")]
    public static void CheckArenaSetup()
    {
        Debug.Log("🔍 ========== ПРОВЕРКА ARENA SETUP ==========");

        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "ArenaScene")
        {
            Debug.LogWarning("⚠️ Текущая сцена не ArenaScene!");
            Debug.LogWarning($"Текущая сцена: {scene.name}");
        }

        // Проверка Skill Bar
        GameObject skillBar = GameObject.Find("SkillBar");
        if (skillBar == null) skillBar = GameObject.Find("Canvas/SkillBar");
        if (skillBar == null) skillBar = GameObject.Find("UI/SkillBar");

        if (skillBar == null)
        {
            Debug.LogError("❌ SkillBar не найден!");
        }
        else
        {
            Debug.Log($"✅ SkillBar найден: {skillBar.name}");

            int slotCount = 0;
            for (int i = 0; i < 5; i++)
            {
                if (skillBar.transform.Find($"SkillSlot_{i}") != null)
                {
                    slotCount++;
                }
            }
            Debug.Log($"Слотов найдено: {slotCount}/5");

            if (slotCount < 5)
            {
                Debug.LogWarning($"⚠️ Недостаточно слотов! Запустите Tools → Arena → Setup Arena Scene");
            }
        }

        // Проверка игрока
        GameObject player = FindPlayerInScene();
        if (player == null)
        {
            Debug.LogError("❌ Игрок не найден!");
        }
        else
        {
            Debug.Log($"✅ Игрок найден: {player.name}");

            // Проверка компонентов
            CheckComponent<SkillExecutor>(player, "SkillExecutor");
            CheckComponent<EffectManager>(player, "EffectManager");
            CheckComponent<PlayerAttackNew>(player, "PlayerAttackNew");
            CheckComponent<SkillManager>(player, "SkillManager");
        }

        Debug.Log("════════════════════════════════════════════════════════════");
    }

    private static void CheckComponent<T>(GameObject obj, string name) where T : Component
    {
        if (obj.GetComponent<T>() != null)
        {
            Debug.Log($"  ✅ {name}");
        }
        else
        {
            Debug.LogError($"  ❌ {name} - НЕ НАЙДЕН!");
        }
    }
}
