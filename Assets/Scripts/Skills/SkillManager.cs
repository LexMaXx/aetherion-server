using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Менеджер скиллов персонажа (загружается в Arena)
/// Управляет активными скиллами, кулдаунами и эффектами
/// </summary>
public class SkillManager : MonoBehaviour
{
    [Header("Активные скиллы (загружаются из Character Selection)")]
    public List<SkillData> equippedSkills = new List<SkillData>(3); // Максимум 3 скилла

    [Header("Все доступные скиллы класса")]
    public List<SkillData> allAvailableSkills = new List<SkillData>(6); // До 6 скиллов на класс

    [Header("Компоненты")]
    private CharacterStats characterStats;
    private ManaSystem manaSystem;
    private HealthSystem healthSystem;
    private Animator animator;

    // Кулдауны скиллов (skillId -> оставшееся время)
    private Dictionary<int, float> skillCooldowns = new Dictionary<int, float>();

    // Активные эффекты на персонаже
    private List<ActiveEffect> activeEffects = new List<ActiveEffect>();

    // События
    public delegate void SkillUsedHandler(SkillData skill);
    public event SkillUsedHandler OnSkillUsed;

    public delegate void EffectAppliedHandler(SkillEffect effect);
    public event EffectAppliedHandler OnEffectApplied;

    // Призванные существа (для Rogue)
    private List<GameObject> summonedCreatures = new List<GameObject>();

    // Трансформация (для Paladin) - MESH SWAPPING APPROACH
    public bool isTransformed = false; // PUBLIC для NetworkSyncManager
    private float transformationHPBonus = 0f; // Сохраняем бонус HP для удаления

    // Сохранение оригинального mesh и материалов для восстановления
    private SkinnedMeshRenderer playerRenderer;
    private Mesh originalMesh;
    private Material[] originalMaterials;
    private Transform[] originalBones; // КРИТИЧЕСКОЕ: Сохраняем bones игрока!

    void Start()
    {
        characterStats = GetComponent<CharacterStats>();
        manaSystem = GetComponent<ManaSystem>();
        healthSystem = GetComponent<HealthSystem>();
        animator = GetComponent<Animator>();

        Debug.Log($"[SkillManager] Инициализирован. Экипировано скиллов: {equippedSkills.Count}");
    }

    void Update()
    {
        // Обновляем кулдауны
        UpdateCooldowns();

        // Обновляем активные эффекты
        UpdateActiveEffects();
    }

    /// <summary>
    /// Загрузить скиллы из Character Selection (вызывается при загрузке персонажа)
    /// </summary>
    public void LoadEquippedSkills(List<int> skillIds)
    {
        equippedSkills.Clear();

        foreach (int skillId in skillIds)
        {
            SkillData skill = GetSkillById(skillId);
            if (skill != null)
            {
                equippedSkills.Add(skill);
                Debug.Log($"[SkillManager] Загружен скилл: {skill.skillName}");
            }
        }

        Debug.Log($"[SkillManager] ✅ Загружено {equippedSkills.Count} скиллов");
    }

    /// <summary>
    /// Использовать скилл по индексу (0-2)
    /// </summary>
    public bool UseSkill(int skillIndex, Transform target = null)
    {
        if (skillIndex < 0 || skillIndex >= equippedSkills.Count)
        {
            Debug.LogWarning($"[SkillManager] Некорректный индекс скилла: {skillIndex}");
            return false;
        }

        SkillData skill = equippedSkills[skillIndex];
        return UseSkill(skill, target);
    }

    /// <summary>
    /// Использовать скилл
    /// </summary>
    public bool UseSkill(SkillData skill, Transform target = null)
    {
        Debug.Log($"[SkillManager] 🔍 UseSkill вызван: skill={skill?.skillName ?? "NULL"}");

        if (skill == null)
        {
            Debug.LogError("[SkillManager] ❌ Skill is NULL!");
            return false;
        }

        Debug.Log($"[SkillManager] 🔍 Скилл: {skill.skillName}, Тип: {skill.skillType}, Мана: {skill.manaCost}");

        // Проверка контроля (стан, молчание и т.д.)
        if (IsUnderCrowdControl())
        {
            Debug.Log("[SkillManager] ❌ Не могу использовать скилл - персонаж под контролем!");
            return false;
        }

        // КРИТИЧЕСКОЕ: Проверка трансформации ДО траты маны
        if (skill.skillType == SkillType.Transformation && isTransformed)
        {
            Debug.Log("[SkillManager] ❌ Уже трансформирован! Нельзя использовать скилл трансформации.");
            return false;
        }

        // Проверка возможности использования
        float currentCooldown = GetCooldown(skill.skillId);
        Debug.Log($"[SkillManager] 🔍 Кулдаун скилла {skill.skillId}: {currentCooldown:F1}с");

        if (!skill.CanUse(characterStats, manaSystem, currentCooldown))
        {
            if (manaSystem != null)
            {
                Debug.Log($"[SkillManager] 🔍 Текущая мана: {manaSystem.CurrentMana}/{manaSystem.MaxMana}, Требуется: {skill.manaCost}");
            }
            Debug.Log($"[SkillManager] ❌ Скилл {skill.skillName} недоступен (кулдаун: {currentCooldown:F1}с)");
            return false;
        }

        // Проверка цели
        if (skill.requiresTarget && target == null)
        {
            Debug.Log($"[SkillManager] ❌ Скилл {skill.skillName} требует цель!");
            return false;
        }

        // Проверка дальности
        if (target != null && Vector3.Distance(transform.position, target.position) > skill.castRange)
        {
            Debug.Log($"[SkillManager] ❌ Цель слишком далеко! ({Vector3.Distance(transform.position, target.position):F1}м > {skill.castRange}м)");
            return false;
        }

        // Тратим ману
        if (manaSystem != null)
        {
            manaSystem.SpendMana(skill.manaCost);
        }

        // Запускаем кулдаун
        skillCooldowns[skill.skillId] = skill.cooldown;

        // Проигрываем анимацию
        if (animator != null && !string.IsNullOrEmpty(skill.animationTrigger))
        {
            animator.SetTrigger(skill.animationTrigger);
        }

        // Проигрываем звук каста
        if (skill.castSound != null)
        {
            AudioSource.PlayClipAtPoint(skill.castSound, transform.position);
        }

        // НОВОЕ: Отправляем скилл на сервер для синхронизации в мультиплеере
        SendSkillToServer(skill, target);

        // Выполняем скилл локально
        ExecuteSkill(skill, target);

        // Событие
        OnSkillUsed?.Invoke(skill);

        Debug.Log($"[SkillManager] ⚡ Использован скилл: {skill.skillName}");
        return true;
    }

    /// <summary>
    /// Выполнить скилл
    /// </summary>
    private void ExecuteSkill(SkillData skill, Transform target)
    {
        switch (skill.skillType)
        {
            case SkillType.Damage:
                ExecuteDamageSkill(skill, target);
                break;

            case SkillType.Heal:
                ExecuteHealSkill(skill, target);
                break;

            case SkillType.Buff:
            case SkillType.Debuff:
            case SkillType.CrowdControl:
                ExecuteEffectSkill(skill, target);
                break;

            case SkillType.Summon:
                ExecuteSummonSkill(skill);
                break;

            case SkillType.Transformation:
                ExecuteTransformationSkill(skill);
                break;

            case SkillType.Ressurect:
                ExecuteRessurectSkill(skill, target);
                break;
        }
    }

    /// <summary>
    /// Скилл урона
    /// </summary>
    private void ExecuteDamageSkill(SkillData skill, Transform target)
    {
        float damage = skill.CalculateDamage(characterStats);

        // AOE урон
        if (skill.aoeRadius > 0f)
        {
            Vector3 center = target != null ? target.position : transform.position;
            Collider[] hits = Physics.OverlapSphere(center, skill.aoeRadius);

            int hitCount = 0;
            foreach (Collider hit in hits)
            {
                if (hitCount >= skill.maxTargets) break;

                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    hitCount++;

                    // Визуальный эффект
                    if (skill.visualEffectPrefab != null)
                    {
                        Instantiate(skill.visualEffectPrefab, hit.transform.position, Quaternion.identity);
                    }
                }
            }

            Debug.Log($"[SkillManager] 💥 AOE урон: {damage} по {hitCount} целям");
        }
        // Одиночный урон
        else if (target != null)
        {
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);

                // Визуальный эффект
                if (skill.visualEffectPrefab != null)
                {
                    Instantiate(skill.visualEffectPrefab, target.position, Quaternion.identity);
                }

                Debug.Log($"[SkillManager] 💥 Урон: {damage}");
            }
        }

        // Снаряд
        if (skill.projectilePrefab != null && target != null)
        {
            GameObject projectile = Instantiate(skill.projectilePrefab, transform.position + Vector3.up, Quaternion.identity);
            Projectile proj = projectile.GetComponent<Projectile>();
            if (proj != null)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                proj.Initialize(target, damage, direction);
            }
        }
    }

    /// <summary>
    /// Скилл лечения
    /// </summary>
    private void ExecuteHealSkill(SkillData skill, Transform target)
    {
        float healAmount = skill.CalculateDamage(characterStats);

        // Лечим цель
        Transform healTarget = target != null ? target : transform;
        HealthSystem targetHealth = healTarget.GetComponent<HealthSystem>();

        if (targetHealth != null)
        {
            targetHealth.Heal(healAmount);

            // Визуальный эффект
            if (skill.visualEffectPrefab != null)
            {
                Instantiate(skill.visualEffectPrefab, healTarget.position, Quaternion.identity);
            }

            Debug.Log($"[SkillManager] 💚 Лечение: {healAmount} HP");
        }
    }

    /// <summary>
    /// Скилл с эффектами (баф/дебаф/контроль)
    /// </summary>
    private void ExecuteEffectSkill(SkillData skill, Transform target)
    {
        Transform effectTarget = target != null ? target : transform;

        // Применяем все эффекты скилла
        foreach (SkillEffect effect in skill.effects)
        {
            ApplyEffect(effect, effectTarget);
        }

        Debug.Log($"[SkillManager] ✨ Применено эффектов: {skill.effects.Count}");
    }

    /// <summary>
    /// Скилл призыва (Rogue - скелеты)
    /// </summary>
    private void ExecuteSummonSkill(SkillData skill)
    {
        if (skill.summonPrefab == null)
        {
            Debug.LogWarning("[SkillManager] Нет префаба для призыва!");
            return;
        }

        // Очищаем старых призванных
        ClearSummons();

        // Призываем существ
        for (int i = 0; i < skill.summonCount; i++)
        {
            Vector3 spawnPos = transform.position + transform.forward * 2f + transform.right * (i - skill.summonCount / 2) * 1.5f;
            GameObject summon = Instantiate(skill.summonPrefab, spawnPos, Quaternion.identity);

            // Настраиваем призванное существо
            SummonedCreature creature = summon.GetComponent<SummonedCreature>();
            if (creature == null)
            {
                creature = summon.AddComponent<SummonedCreature>();
            }
            creature.Initialize(transform, skill.summonDuration);

            summonedCreatures.Add(summon);
        }

        Debug.Log($"[SkillManager] 👻 Призвано существ: {skill.summonCount}");
    }

    /// <summary>
    /// Скилл трансформации (Paladin - медведь) - CHILD GAMEOBJECT APPROACH
    /// Создаём медведя как child GameObject и прячем модель игрока
    /// </summary>
    private void ExecuteTransformationSkill(SkillData skill)
    {
        Debug.Log($"[SkillManager] 🔍 ExecuteTransformationSkill (CHILD GAMEOBJECT) вызван для {skill.skillName}");

        if (skill.transformationModel == null)
        {
            Debug.LogError("[SkillManager] ❌ НЕТ МОДЕЛИ ДЛЯ ТРАНСФОРМАЦИИ! Проверь Paladin_BearForm.asset в инспекторе!");
            return;
        }

        // Находим SkinnedMeshRenderer игрока и прячем его
        playerRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (playerRenderer == null)
        {
            Debug.LogError("[SkillManager] ❌ SkinnedMeshRenderer не найден!");
            return;
        }

        // НОВЫЙ ПОДХОД: Прячем модель игрока
        playerRenderer.gameObject.SetActive(false);
        Debug.Log($"[SkillManager] 👻 Модель игрока скрыта");

        // Создаём медведя как child объект
        GameObject bearInstance = Instantiate(skill.transformationModel, transform);
        bearInstance.transform.localPosition = Vector3.zero;
        bearInstance.transform.localRotation = Quaternion.identity;

        // КРИТИЧЕСКОЕ: Подключаем аниматор медведя к PlayerController
        Animator bearAnimator = bearInstance.GetComponentInChildren<Animator>();
        if (bearAnimator != null && animator != null)
        {
            // Заменяем animator игрока на animator медведя
            Animator originalAnimator = animator; // Сохраняем оригинальный аниматор
            animator = bearAnimator; // Теперь PlayerController будет управлять аниматором медведя!

            // Сохраняем оригинальный аниматор для восстановления
            originalBones = new Transform[] { bearInstance.transform, originalAnimator.transform };

            Debug.Log($"[SkillManager] 🎬 Аниматор медведя подключен к PlayerController");
        }
        else
        {
            // Если нет аниматора, просто сохраняем ссылку
            originalBones = new Transform[] { bearInstance.transform };
            Debug.LogWarning($"[SkillManager] ⚠️ У медведя нет Animator компонента!");
        }

        // КРИТИЧЕСКОЕ: Добавляем ClassWeaponManager к медведю и привязываем оружие паладина
        ClassWeaponManager bearWeaponManager = bearInstance.GetComponent<ClassWeaponManager>();
        if (bearWeaponManager == null)
        {
            bearWeaponManager = bearInstance.AddComponent<ClassWeaponManager>();
            Debug.Log($"[SkillManager] 🔧 ClassWeaponManager добавлен к медведю");
        }

        // Устанавливаем класс вручную (медведь = паладин с оружием)
        bearWeaponManager.SetCharacterClass(CharacterClass.Paladin);

        // Привязываем оружие к костям медведя
        bearWeaponManager.AttachWeaponForClass();
        Debug.Log($"[SkillManager] ⚔️ Оружие паладина привязано к медведю");

        originalMesh = null; // Не используем mesh swapping
        originalMaterials = null;

        isTransformed = true;

        Debug.Log($"[SkillManager] 🐻 ✅ Медведь создан как child GameObject");

        // ОРУЖИЕ: Оружие игрока скрыто вместе с моделью, а у медведя своё оружие (через ClassWeaponManager выше)

        // Применяем бонусы
        if (healthSystem != null && skill.hpBonusPercent > 0f)
        {
            transformationHPBonus = healthSystem.MaxHealth * (skill.hpBonusPercent / 100f);
            healthSystem.AddTemporaryMaxHealth(transformationHPBonus);
            Debug.Log($"[SkillManager] ✅ Бонус HP: +{transformationHPBonus:F0} ({skill.hpBonusPercent}%)");
        }

        // Автоматически отключаем через время
        Invoke(nameof(EndTransformation), skill.transformationDuration);

        Debug.Log($"[SkillManager] 🐻 ✅ ТРАНСФОРМАЦИЯ АКТИВИРОВАНА (CHILD GAMEOBJECT) на {skill.transformationDuration}с!");
    }

    /// <summary>
    /// Завершить трансформацию - CHILD GAMEOBJECT
    /// </summary>
    private void EndTransformation()
    {
        if (!isTransformed) return;

        // Восстанавливаем оригинальный аниматор
        if (originalBones != null && originalBones.Length > 1 && originalBones[1] != null)
        {
            Animator originalAnimator = originalBones[1].GetComponent<Animator>();
            if (originalAnimator != null)
            {
                animator = originalAnimator; // Восстанавливаем аниматор игрока
                Debug.Log($"[SkillManager] 🎬 Аниматор игрока восстановлен");
            }
        }

        // Удаляем медведя (child GameObject)
        if (originalBones != null && originalBones.Length > 0 && originalBones[0] != null)
        {
            GameObject bearInstance = originalBones[0].gameObject;
            Destroy(bearInstance);
            Debug.Log($"[SkillManager] ✅ Медведь удалён");
        }

        // Показываем модель игрока обратно
        if (playerRenderer != null)
        {
            playerRenderer.gameObject.SetActive(true);
            Debug.Log($"[SkillManager] ✅ Модель игрока восстановлена");
        }

        // Убираем бонус HP
        if (healthSystem != null && transformationHPBonus > 0f)
        {
            healthSystem.RemoveTemporaryMaxHealth(transformationHPBonus);
            transformationHPBonus = 0f;
        }

        // ОРУЖИЕ: Оружие игрока скрыто вместе с его моделью, при восстановлении модели оно автоматически появится
        // ClassWeaponManager медведя удалится вместе с GameObject медведя (Destroy выше)

        // Очищаем ссылки
        playerRenderer = null;
        originalMesh = null;
        originalMaterials = null;
        originalBones = null;

        isTransformed = false;

        // Отправляем на сервер окончание трансформации
        if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
        {
            SocketIOManager.Instance.SendTransformationEnd();
        }

        Debug.Log("[SkillManager] 🐻 Трансформация завершена (CHILD GAMEOBJECT)");
    }

    /// <summary>
    /// Скилл воскрешения
    /// </summary>
    private void ExecuteRessurectSkill(SkillData skill, Transform target)
    {
        // TODO: Реализовать воскрешение
        Debug.Log($"[SkillManager] ⚰️ Воскрешение (не реализовано)");
    }

    /// <summary>
    /// Применить эффект к цели
    /// </summary>
    private void ApplyEffect(SkillEffect effect, Transform target)
    {
        if (target == null) return;

        // Для противника
        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy != null)
        {
            SkillManager enemySkillManager = enemy.GetComponent<SkillManager>();
            if (enemySkillManager == null)
            {
                enemySkillManager = enemy.gameObject.AddComponent<SkillManager>();
            }
            enemySkillManager.AddEffect(effect, target);
        }
        // Для себя/союзника
        else
        {
            AddEffect(effect, target);
        }

        OnEffectApplied?.Invoke(effect);
    }

    /// <summary>
    /// Добавить эффект
    /// </summary>
    public void AddEffect(SkillEffect effect, Transform target)
    {
        ActiveEffect activeEffect = new ActiveEffect(effect, target);
        activeEffects.Add(activeEffect);

        Debug.Log($"[SkillManager] ✨ Добавлен эффект: {effect.effectType} на {effect.duration}с");
    }

    /// <summary>
    /// Обновить кулдауны (ИСПРАВЛЕНО: используем список ключей)
    /// </summary>
    private void UpdateCooldowns()
    {
        // ИСПРАВЛЕНИЕ: Создаём временный список ключей для безопасной итерации
        // Нужно потому что UseSkill() может добавлять новые ключи во время Update()
        if (skillCooldowns.Count == 0) return;

        // Копируем ключи в список (это безопасно)
        var keys = new List<int>(skillCooldowns.Keys);

        foreach (int skillId in keys)
        {
            float cooldown = skillCooldowns[skillId];

            if (cooldown > 0f)
            {
                skillCooldowns[skillId] = Mathf.Max(0f, cooldown - Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// Обновить активные эффекты
    /// </summary>
    private void UpdateActiveEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            bool shouldRemove = activeEffects[i].Update(Time.deltaTime);
            if (shouldRemove)
            {
                activeEffects.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Получить кулдаун скилла
    /// </summary>
    public float GetCooldown(int skillId)
    {
        return skillCooldowns.ContainsKey(skillId) ? skillCooldowns[skillId] : 0f;
    }

    /// <summary>
    /// Проверка контроля
    /// </summary>
    public bool IsUnderCrowdControl()
    {
        foreach (ActiveEffect effect in activeEffects)
        {
            if (effect.isStunned || effect.isSilenced)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Проверка обездвиживания
    /// </summary>
    public bool IsRooted()
    {
        foreach (ActiveEffect effect in activeEffects)
        {
            if (effect.isRooted || effect.isStunned)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Найти скилл по ID
    /// </summary>
    private SkillData GetSkillById(int skillId)
    {
        foreach (SkillData skill in allAvailableSkills)
        {
            if (skill.skillId == skillId)
            {
                return skill;
            }
        }
        return null;
    }

    /// <summary>
    /// Очистить призванных существ
    /// </summary>
    private void ClearSummons()
    {
        foreach (GameObject summon in summonedCreatures)
        {
            if (summon != null)
            {
                Destroy(summon);
            }
        }
        summonedCreatures.Clear();
    }

    /// <summary>
    /// Отправить скилл на сервер для синхронизации
    /// ОБНОВЛЕНО: Теперь передает skillType для корректной обработки трансформации
    /// </summary>
    private void SendSkillToServer(SkillData skill, Transform target)
    {
        // Проверяем что мы в мультиплеере
        if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
        {
            Debug.Log("[SkillManager] Не в мультиплеере - пропускаем отправку на сервер");
            return;
        }

        // Получаем target socketId (если цель - другой игрок)
        string targetSocketId = "";
        if (target != null)
        {
            NetworkPlayer networkTarget = target.GetComponent<NetworkPlayer>();
            if (networkTarget != null)
            {
                targetSocketId = networkTarget.socketId;
            }
        }

        // ВАЖНО: Передаем тип скилла для правильной обработки на сервере
        string skillType = skill.skillType.ToString(); // "Transformation", "Damage", "Heal" и т.д.

        // Отправляем на сервер
        Vector3 targetPos = target != null ? target.position : transform.position;
        SocketIOManager.Instance.SendPlayerSkill(skill.skillId, targetSocketId, targetPos, skillType);

        Debug.Log($"[SkillManager] 📡 Скилл {skill.skillName} (ID:{skill.skillId}, тип:{skillType}) отправлен на сервер");
    }

    void OnDestroy()
    {
        // КРИТИЧЕСКОЕ: Отменяем все Invoke (EndTransformation и другие)
        CancelInvoke();

        ClearSummons();
    }
}
