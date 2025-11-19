using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет Skill Bar в Arena Scene (5 иконок внизу справа)
/// Загружает экипированные скиллы и обрабатывает хоткеи (1, 2, 3, 4, 5)
/// </summary>
public class SkillBarUI : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("База данных скиллов")]
    [SerializeField] private SkillDatabase skillDatabase;

    // DUAL SYSTEM: Поддержка старой (SkillSlotBar) и новой (SkillButton) системы
    private SkillSlotBar[] skillSlots; // OLD SYSTEM
    private SkillButton[] skillButtons; // NEW SYSTEM (приоритет)
    private List<int> equippedSkillIds = new List<int>();

    void Awake()
    {
        // ПРИОРИТЕТ 1: Ищем новую систему (SkillButton)
        skillButtons = GetComponentsInChildren<SkillButton>();

        if (skillButtons.Length > 0)
        {
            Debug.Log($"[SkillBarUI] ✅ NEW SYSTEM: Найдено {skillButtons.Length} SkillButton слотов");

            if (skillButtons.Length != 5)
            {
                Debug.LogWarning($"[SkillBarUI] ⚠️ Должно быть ровно 5 слотов! Найдено: {skillButtons.Length}");
            }
        }
        else
        {
            // FALLBACK: Используем старую систему (SkillSlotBar)
            skillSlots = GetComponentsInChildren<SkillSlotBar>();

            if (skillSlots.Length != 5)
            {
                Debug.LogError($"[SkillBarUI] Должно быть ровно 5 слотов! Найдено: {skillSlots.Length}");
            }
            else
            {
                Debug.Log("[SkillBarUI] ✅ OLD SYSTEM: Найдено 5 SkillSlotBar слотов");
            }
        }
    }

    void Start()
    {
        // Загружаем SkillDatabase из Resources если не назначена
        if (skillDatabase == null)
        {
            skillDatabase = Resources.Load<SkillDatabase>("SkillDatabase");

            if (skillDatabase == null)
            {
                Debug.LogError("[SkillBarUI] ❌ SkillDatabase не найдена!");
                return;
            }
            else
            {
                Debug.Log("[SkillBarUI] ✅ SkillDatabase загружена из Resources");
            }
        }

        // Загружаем экипированные скиллы
        LoadEquippedSkills();
    }

    /// <summary>
    /// Загрузить экипированные скиллы из PlayerPrefs
    /// (сохраняются в Character Selection Scene)
    /// NEW SYSTEM: Использует SkillConfig напрямую из Resources/Skills/
    /// </summary>
    private void LoadEquippedSkills()
    {
        // Получаем ID экипированных скиллов из PlayerPrefs
        string equippedSkillsJson = PlayerPrefs.GetString("EquippedSkills", "");

        if (string.IsNullOrEmpty(equippedSkillsJson))
        {
            Debug.LogWarning("[SkillBarUI] ⚠️ Нет сохранённых скиллов. Используем тестовые скиллы.");
            LoadTestSkills();
            return;
        }

        try
        {
            // Парсим JSON
            EquippedSkillsData data = JsonUtility.FromJson<EquippedSkillsData>(equippedSkillsJson);
            equippedSkillIds = data.skillIds;

            Debug.Log($"[SkillBarUI] Загружено {equippedSkillIds.Count} экипированных скиллов: [{string.Join(", ", equippedSkillIds)}]");

            // NEW SYSTEM: Загружаем SkillConfig напрямую из Resources/Skills/ по ID
            // DUAL SYSTEM: Используем либо SkillButton, либо SkillSlotBar
            if (skillButtons != null && skillButtons.Length > 0)
            {
                // NEW SYSTEM: SkillButton - используем SetSkillConfig напрямую!
                for (int i = 0; i < skillButtons.Length && i < equippedSkillIds.Count; i++)
                {
                    SkillConfig skillConfig = SkillConfigLoader.LoadSkillById(equippedSkillIds[i]);

                    if (skillConfig != null)
                    {
                        // ИСПРАВЛЕНО: Используем SetSkillConfig вместо SetSkill
                        skillButtons[i].SetSkillConfig(skillConfig);
                        Debug.Log($"[SkillBarUI] ✅ SkillButton {i + 1}: {skillConfig.skillName} (ID: {skillConfig.skillId})");
                    }
                    else
                    {
                        Debug.LogWarning($"[SkillBarUI] ⚠️ Скилл с ID {equippedSkillIds[i]} не найден в Resources/Skills/!");
                    }
                }
            }
            else if (skillSlots != null && skillSlots.Length > 0)
            {
                // OLD SYSTEM: SkillSlotBar
                for (int i = 0; i < skillSlots.Length && i < equippedSkillIds.Count; i++)
                {
                    SkillConfig skillConfig = SkillConfigLoader.LoadSkillById(equippedSkillIds[i]);

                    if (skillConfig != null)
                    {
                        SkillData skillData = SkillDataConverter.ConvertToSkillData(skillConfig);

                        if (skillData != null)
                        {
                            skillSlots[i].SetSkill(skillData);
                            Debug.Log($"[SkillBarUI] ✅ SkillSlotBar {i + 1}: {skillConfig.skillName} (ID: {skillConfig.skillId})");
                        }
                        else
                        {
                            Debug.LogWarning($"[SkillBarUI] ⚠️ Не удалось конвертировать SkillConfig ID {equippedSkillIds[i]} в SkillData!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[SkillBarUI] ⚠️ Скилл с ID {equippedSkillIds[i]} не найден в Resources/Skills/!");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SkillBarUI] ❌ Ошибка парсинга скиллов: {e.Message}");
            LoadTestSkills();
        }
    }

    /// <summary>
    /// Загрузить тестовые скиллы (для отладки)
    /// </summary>
    private void LoadTestSkills()
    {
        // Получаем класс локального игрока
        PlayerController player = FindObjectOfType<PlayerController>();
        CharacterClass playerClass = CharacterClass.Warrior; // По умолчанию

        if (player != null)
        {
            CharacterStats stats = player.GetComponent<CharacterStats>();
            if (stats != null)
            {
                // Парсим className (string) в CharacterClass (enum)
                string className = stats.ClassName;
                if (System.Enum.TryParse(className, out CharacterClass parsedClass))
                {
                    playerClass = parsedClass;
                    Debug.Log($"[SkillBarUI] 🔍 Определён класс игрока: {className} → {playerClass}");
                }
                else
                {
                    Debug.LogWarning($"[SkillBarUI] ⚠️ Не удалось распарсить класс '{className}', используется Warrior");
                }
            }
        }

        Debug.Log($"[SkillBarUI] 🧪 Загружаю тестовые скиллы для {playerClass}...");

        // Получаем все 5 скиллов класса игрока
        List<SkillData> classSkills = skillDatabase.GetSkillsForClass(playerClass);

        if (classSkills == null || classSkills.Count == 0)
        {
            Debug.LogError($"[SkillBarUI] ❌ Нет скиллов для класса {playerClass}!");
            return;
        }

        // DUAL SYSTEM: SkillButton или SkillSlotBar
        if (skillButtons != null && skillButtons.Length > 0)
        {
            // NEW SYSTEM: SkillButton
            for (int i = 0; i < skillButtons.Length && i < classSkills.Count; i++)
            {
                skillButtons[i].SetSkill(classSkills[i]);
                Debug.Log($"[SkillBarUI] ✅ Тестовый SkillButton {i + 1}: {classSkills[i].skillName} (ID: {classSkills[i].skillId})");
            }
        }
        else if (skillSlots != null && skillSlots.Length > 0)
        {
            // OLD SYSTEM: SkillSlotBar
            for (int i = 0; i < skillSlots.Length && i < classSkills.Count; i++)
            {
                skillSlots[i].SetSkill(classSkills[i]);
                Debug.Log($"[SkillBarUI] ✅ Тестовый SkillSlotBar {i + 1}: {classSkills[i].skillName} (ID: {classSkills[i].skillId})");
            }
        }
    }

    void Update()
    {
        // ОТКЛЮЧЕНО: Обработка хоткеев теперь в PlayerAttack.cs
        // Клавиши 1/2/3 теперь ТОЛЬКО выбирают скилл
        // ПКМ использует выбранный скилл
        // Это избегает двойного срабатывания

        // Старый код (закомментирован):
        // if (Input.GetKeyDown(KeyCode.Alpha1)) { UseSkill(0); }
        // else if (Input.GetKeyDown(KeyCode.Alpha2)) { UseSkill(1); }
        // else if (Input.GetKeyDown(KeyCode.Alpha3)) { UseSkill(2); }
    }

    /// <summary>
    /// Использовать скилл по индексу слота (0, 1, 2)
    /// </summary>
    public void UseSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= skillSlots.Length)
        {
            Debug.LogError($"[SkillBarUI] Неверный индекс слота: {slotIndex}");
            return;
        }

        SkillSlotBar slot = skillSlots[slotIndex];
        SkillData skill = slot.GetSkill();

        if (skill == null)
        {
            Debug.LogWarning($"[SkillBarUI] Слот {slotIndex + 1} пустой!");
            return;
        }

        if (slot.IsOnCooldown())
        {
            Debug.LogWarning($"[SkillBarUI] Скилл '{skill.skillName}' на кулдауне! Осталось: {slot.GetCooldownRemaining():F1}с");
            return;
        }

        Debug.Log($"[SkillBarUI] ⚡ Попытка использовать скилл: '{skill.skillName}' (Урон: {skill.baseDamageOrHeal}, Мана: {skill.manaCost})");

        // Применяем скилл (SkillManager сам проверит ману и запустит кулдаун)
        bool success = ApplySkill(skill);

        // Запускаем кулдаун UI только если скилл успешно применён
        if (success)
        {
            slot.StartCooldown(skill.cooldown);
        }
    }

    /// <summary>
    /// Применить эффект скилла
    /// ИЗМЕНЕНО: Использует SkillManager для полной поддержки всех типов скиллов
    /// </summary>
    private bool ApplySkill(SkillData skill)
    {
        // Находим игрока по тегу
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning("[SkillBarUI] Игрок не найден!");
            return false;
        }

        Debug.Log($"[SkillBarUI] 🔍 Player найден: {playerObj.name}, ищу SkillManager в детях...");

        // Ищем SkillManager в детях (на Model)
        SkillManager skillManager = playerObj.GetComponentInChildren<SkillManager>(true); // includeInactive = true

        if (skillManager == null)
        {
            // Дополнительная диагностика
            Debug.LogWarning("[SkillBarUI] ⚠️ SkillManager не найден GetComponentInChildren! Ищу везде...");

            // Поиск во всех дочерних объектах
            SkillManager[] allManagers = FindObjectsOfType<SkillManager>(true);
            Debug.Log($"[SkillBarUI] Найдено SkillManager в сцене: {allManagers.Length}");

            foreach (SkillManager sm in allManagers)
            {
                Debug.Log($"  - SkillManager на объекте: {sm.gameObject.name} (parent: {sm.transform.parent?.name ?? "NULL"})");
                // Проверяем что это SkillManager нашего игрока
                if (sm.transform.IsChildOf(playerObj.transform) || sm.gameObject == playerObj)
                {
                    Debug.Log($"    ✅ Это наш SkillManager! Использую его.");
                    skillManager = sm;
                    break;
                }
            }

            if (skillManager == null && allManagers.Length > 0)
            {
                // FALLBACK: Если не нашли по родителю, берём первый попавшийся (для singleplayer)
                Debug.LogWarning("[SkillBarUI] ⚠️ Не могу определить SkillManager по иерархии, использую первый найденный");
                skillManager = allManagers[0];
            }
        }

        if (skillManager != null)
        {
            Debug.Log($"[SkillBarUI] ✅ Используется SkillManager (на {skillManager.gameObject.name}) для применения скилла");

            // Получаем цель (если требуется)
            Transform target = null;
            if (skill.requiresTarget)
            {
                TargetSystem targetSystem = playerObj.GetComponentInChildren<TargetSystem>();
                if (targetSystem != null)
                {
                    TargetableEntity currentTarget = targetSystem.GetCurrentTarget();
                    if (currentTarget != null)
                    {
                        target = currentTarget.transform;
                    }
                }
            }

            // Находим индекс скилла по skillId
            int skillIndex = -1;
            for (int i = 0; i < skillManager.equippedSkills.Count; i++)
            {
                if (skillManager.equippedSkills[i] != null && skillManager.equippedSkills[i].skillId == skill.skillId)
                {
                    skillIndex = i;
                    break;
                }
            }

            if (skillIndex < 0)
            {
                Debug.LogWarning($"[SkillBarUI] ❌ Скилл ID {skill.skillId} не найден в equippedSkills!");
                return false;
            }

            // Используем скилл через SkillManager (по индексу)
            bool success = skillManager.UseSkill(skillIndex, target);
            if (success)
            {
                Debug.Log($"[SkillBarUI] ✅ Скилл '{skill.skillName}' применён через SkillManager");
            }
            else
            {
                Debug.LogWarning($"[SkillBarUI] ❌ Не удалось применить скилл '{skill.skillName}' через SkillManager");
            }
            return success;
        }

        // FALLBACK: Старая система (если нет SkillManager)
        Debug.LogWarning("[SkillBarUI] SkillManager не найден! Используется старая система (только урон)");

        PlayerController player = playerObj.GetComponentInChildren<PlayerController>();
        if (player == null)
        {
            Debug.LogWarning("[SkillBarUI] PlayerController не найден!");
            return false;
        }

        // Получаем систему таргетинга
        TargetSystem targetSystem2 = playerObj.GetComponentInChildren<TargetSystem>();
        if (targetSystem2 == null)
        {
            Debug.LogWarning("[SkillBarUI] TargetSystem не найден!");
            return false;
        }

        // Получаем текущую цель
        TargetableEntity currentTarget2 = targetSystem2.GetCurrentTarget();

        if (currentTarget2 == null)
        {
            Debug.LogWarning($"[SkillBarUI] Нет цели для скилла '{skill.skillName}'!");
            return false;
        }

        GameObject target2 = currentTarget2.gameObject;

        // Проверяем дистанцию
        float distance = Vector3.Distance(player.transform.position, target2.transform.position);
        if (distance > skill.castRange)
        {
            Debug.LogWarning($"[SkillBarUI] Цель слишком далеко! Дистанция: {distance:F1}м, Макс: {skill.castRange}м");
            return false;
        }

        Debug.Log($"[SkillBarUI] Применяю скилл '{skill.skillName}' к цели '{target2.name}' (дистанция: {distance:F1}м)");

        // Получаем статы игрока для расчёта урона
        CharacterStats playerStats = player.GetComponent<CharacterStats>();
        float damage = skill.CalculateDamage(playerStats);

        // Наносим урон цели (используем Enemy напрямую, т.к. currentTarget уже типа Enemy)
        currentTarget2.TakeDamage(damage);
        Debug.Log($"[SkillBarUI] ✅ Нанесён урон: {damage:F0}");

        // Спавним визуальный эффект (если есть)
        if (skill.visualEffectPrefab != null)
        {
            Vector3 spawnPosition = target2.transform.position;
            GameObject effectInstance = Instantiate(skill.visualEffectPrefab, spawnPosition, Quaternion.identity);

            // Автоматически удаляем через 5 секунд
            Destroy(effectInstance, 5f);

            Debug.Log($"[SkillBarUI] ✅ Создан визуальный эффект '{skill.skillName}'");
        }

        // Спавним снаряд (если есть)
        if (skill.projectilePrefab != null)
        {
            Vector3 spawnPosition = player.transform.position + player.transform.forward * 1f + Vector3.up * 1.5f;
            GameObject projectile = Instantiate(skill.projectilePrefab, spawnPosition, Quaternion.identity);

            // TODO: Добавить скрипт движения снаряда к цели
            // Пока просто направим его на цель
            Vector3 direction = (target2.transform.position - spawnPosition).normalized;
            projectile.transform.forward = direction;

            Debug.Log($"[SkillBarUI] ✅ Создан снаряд '{skill.skillName}'");
        }

        // Воспроизводим звук каста
        if (skill.castSound != null)
        {
            AudioSource.PlayClipAtPoint(skill.castSound, player.transform.position);
            Debug.Log($"[SkillBarUI] ✅ Воспроизведён звук каста");
        }

        // Воспроизводим звук удара (с задержкой, если есть снаряд)
        if (skill.impactSound != null)
        {
            float delay = skill.projectilePrefab != null ? 0.5f : 0f;
            StartCoroutine(PlayImpactSoundDelayed(skill.impactSound, target2.transform.position, delay));
        }

        return true; // Fallback система успешно применила скилл
    }

    /// <summary>
    /// Воспроизвести звук удара с задержкой
    /// </summary>
    private System.Collections.IEnumerator PlayImpactSoundDelayed(AudioClip sound, Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);
        AudioSource.PlayClipAtPoint(sound, position);
        Debug.Log("[SkillBarUI] ✅ Воспроизведён звук удара");
    }

    /// <summary>
    /// Получить слот по индексу (OLD SYSTEM)
    /// </summary>
    public SkillSlotBar GetSlot(int index)
    {
        if (skillSlots != null && index >= 0 && index < skillSlots.Length)
        {
            return skillSlots[index];
        }
        return null;
    }

    /// <summary>
    /// Получить кнопку скилла по индексу (NEW SYSTEM)
    /// </summary>
    public SkillButton GetSkillButton(int index)
    {
        if (skillButtons != null && index >= 0 && index < skillButtons.Length)
        {
            return skillButtons[index];
        }
        return null;
    }
}

/// <summary>
/// Структура для сохранения экипированных скиллов в JSON
/// </summary>
[System.Serializable]
public class EquippedSkillsData
{
    public List<int> skillIds;
}
