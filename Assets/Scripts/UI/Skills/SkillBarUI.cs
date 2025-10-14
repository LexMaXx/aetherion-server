using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет Skill Bar в Arena Scene (3 иконки внизу справа)
/// Загружает экипированные скиллы и обрабатывает хоткеи (1, 2, 3)
/// </summary>
public class SkillBarUI : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("База данных скиллов")]
    [SerializeField] private SkillDatabase skillDatabase;

    private SkillSlotBar[] skillSlots;
    private List<int> equippedSkillIds = new List<int>();

    void Awake()
    {
        // Находим все слоты
        skillSlots = GetComponentsInChildren<SkillSlotBar>();

        if (skillSlots.Length != 3)
        {
            Debug.LogError($"[SkillBarUI] Должно быть ровно 3 слота! Найдено: {skillSlots.Length}");
        }
        else
        {
            Debug.Log("[SkillBarUI] ✅ Найдено 3 слота скиллов");
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

            // Устанавливаем скиллы в слоты
            for (int i = 0; i < skillSlots.Length && i < equippedSkillIds.Count; i++)
            {
                SkillData skill = skillDatabase.GetSkillById(equippedSkillIds[i]);

                if (skill != null)
                {
                    skillSlots[i].SetSkill(skill);
                    Debug.Log($"[SkillBarUI] Слот {i + 1}: {skill.skillName}");
                }
                else
                {
                    Debug.LogWarning($"[SkillBarUI] ⚠️ Скилл с ID {equippedSkillIds[i]} не найден!");
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
        Debug.Log("[SkillBarUI] Загружаю тестовые скиллы для Warrior...");

        // Получаем первые 3 скилла воина
        List<SkillData> warriorSkills = skillDatabase.GetSkillsForClass(CharacterClass.Warrior);

        for (int i = 0; i < skillSlots.Length && i < warriorSkills.Count; i++)
        {
            skillSlots[i].SetSkill(warriorSkills[i]);
            Debug.Log($"[SkillBarUI] Тестовый слот {i + 1}: {warriorSkills[i].skillName}");
        }
    }

    void Update()
    {
        // Обработка хоткеев (клавиши 1, 2, 3)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            UseSkill(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            UseSkill(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            UseSkill(2);
        }
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

        // Проверяем ману
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            ManaSystem manaSystem = player.GetComponent<ManaSystem>();
            if (manaSystem != null)
            {
                if (!manaSystem.HasEnoughMana(skill.manaCost))
                {
                    Debug.LogWarning($"[SkillBarUI] Недостаточно маны для '{skill.skillName}'! Требуется: {skill.manaCost}");
                    return;
                }

                // Используем ману
                if (!manaSystem.SpendMana(skill.manaCost))
                {
                    Debug.LogWarning($"[SkillBarUI] Не удалось потратить ману для '{skill.skillName}'!");
                    return;
                }
            }
        }

        Debug.Log($"[SkillBarUI] ✅ Использован скилл: '{skill.skillName}' (Урон: {skill.baseDamageOrHeal}, Мана: {skill.manaCost})");

        // Применяем скилл
        ApplySkill(skill);

        // Запускаем кулдаун
        slot.StartCooldown(skill.cooldown);
    }

    /// <summary>
    /// Применить эффект скилла
    /// ИЗМЕНЕНО: Использует SkillManager для полной поддержки всех типов скиллов
    /// </summary>
    private void ApplySkill(SkillData skill)
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null)
        {
            Debug.LogWarning("[SkillBarUI] PlayerController не найден!");
            return;
        }

        // НОВОЕ: Используем SkillManager если есть
        SkillManager skillManager = player.GetComponent<SkillManager>();
        if (skillManager != null)
        {
            Debug.Log("[SkillBarUI] Используется SkillManager для применения скилла");

            // Получаем цель (если требуется)
            Transform target = null;
            if (skill.requiresTarget)
            {
                TargetSystem targetSystem = player.GetComponent<TargetSystem>();
                if (targetSystem != null)
                {
                    Enemy currentTarget = targetSystem.GetCurrentTarget();
                    if (currentTarget != null)
                    {
                        target = currentTarget.transform;
                    }
                }
            }

            // Используем скилл через SkillManager
            bool success = skillManager.UseSkill(skill, target);
            if (success)
            {
                Debug.Log($"[SkillBarUI] ✅ Скилл '{skill.skillName}' применён через SkillManager");
            }
            else
            {
                Debug.LogWarning($"[SkillBarUI] ❌ Не удалось применить скилл '{skill.skillName}' через SkillManager");
            }
            return;
        }

        // FALLBACK: Старая система (если нет SkillManager)
        Debug.LogWarning("[SkillBarUI] SkillManager не найден! Используется старая система (только урон)");

        // Получаем систему таргетинга
        TargetSystem targetSystem2 = player.GetComponent<TargetSystem>();
        if (targetSystem2 == null)
        {
            Debug.LogWarning("[SkillBarUI] TargetSystem не найден!");
            return;
        }

        // Получаем текущую цель
        Enemy currentTarget2 = targetSystem2.GetCurrentTarget();

        if (currentTarget2 == null)
        {
            Debug.LogWarning($"[SkillBarUI] Нет цели для скилла '{skill.skillName}'!");
            return;
        }

        GameObject target2 = currentTarget2.gameObject;

        // Проверяем дистанцию
        float distance = Vector3.Distance(player.transform.position, target2.transform.position);
        if (distance > skill.castRange)
        {
            Debug.LogWarning($"[SkillBarUI] Цель слишком далеко! Дистанция: {distance:F1}м, Макс: {skill.castRange}м");
            return;
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
    /// Получить слот по индексу
    /// </summary>
    public SkillSlotBar GetSlot(int index)
    {
        if (index >= 0 && index < skillSlots.Length)
        {
            return skillSlots[index];
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
