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

    // Трансформация (для Paladin)
    private GameObject transformationInstance;
    private GameObject originalModel;
    public bool isTransformed = false; // PUBLIC для NetworkSyncManager
    private float transformationHPBonus = 0f; // Сохраняем бонус HP для удаления

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
    /// Скилл трансформации (Paladin - медведь)
    /// </summary>
    private void ExecuteTransformationSkill(SkillData skill)
    {
        Debug.Log($"[SkillManager] 🔍 ExecuteTransformationSkill вызван для {skill.skillName}");
        Debug.Log($"[SkillManager] 🔍 transformationModel = {(skill.transformationModel != null ? skill.transformationModel.name : "NULL")}");

        if (skill.transformationModel == null)
        {
            Debug.LogError("[SkillManager] ❌ НЕТ МОДЕЛИ ДЛЯ ТРАНСФОРМАЦИИ! Проверь Paladin_BearForm.asset в инспекторе!");
            return;
        }

        // Проверка isTransformed теперь в UseSkill() ДО траты маны

        Debug.Log("[SkillManager] 🔍 Отключаю SkinnedMeshRenderer для скрытия оригинальной модели...");

        // КРИТИЧЕСКОЕ: Отключаем ВСЕ SkinnedMeshRenderer (модель + одежда), но НЕ GameObject
        SkinnedMeshRenderer[] originalRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer smr in originalRenderers)
        {
            smr.enabled = false; // Отключаем только рендерер, GameObject остаётся active (оружие остаётся)
            Debug.Log($"[SkillManager] ✅ Отключён рендерер: {smr.gameObject.name}");
        }

        if (originalRenderers.Length > 0)
        {
            originalModel = originalRenderers[0].gameObject;
        }
        else
        {
            Debug.LogWarning("[SkillManager] ⚠️ SkinnedMeshRenderer не найден - оригинальная модель не скрыта");
        }

        Debug.Log($"[SkillManager] 🔍 Создаю трансформацию на позиции {transform.position}...");

        // Создаём модель трансформации
        transformationInstance = Instantiate(skill.transformationModel, transform.position, transform.rotation, transform);
        isTransformed = true;

        // КРИТИЧЕСКОЕ: Сбрасываем локальную позицию/поворот в zero (чтобы модель была ровно в центре родителя)
        transformationInstance.transform.localPosition = Vector3.zero;
        transformationInstance.transform.localRotation = Quaternion.identity;

        Debug.Log($"[SkillManager] ✅ Трансформация создана: {transformationInstance.name} (localPos сброшен в zero)");
        Debug.Log($"[SkillManager] 🔍 ДИАГНОСТИКА: Parent worldPos={transform.position}, Bear localPos={transformationInstance.transform.localPosition}, Bear worldPos={transformationInstance.transform.position}");

        // КРИТИЧЕСКОЕ: Отключаем ВСЕ коллайдеры медведя (они конфликтуют с CharacterController игрока)
        // Медведь - это визуальная "шкура" (child GameObject), физика остаётся на родителе (игроке)
        Collider[] bearColliders = transformationInstance.GetComponentsInChildren<Collider>();
        foreach (Collider col in bearColliders)
        {
            col.enabled = false;
            Debug.Log($"[SkillManager] 🔧 Отключён коллайдер медведя: {col.GetType().Name}");
        }

        // КРИТИЧЕСКОЕ: Сбрасываем scale GameObject "input" (scale=100 создаёт offset)
        Transform inputTransform = transformationInstance.transform.Find("input");
        if (inputTransform != null)
        {
            inputTransform.localScale = Vector3.one; // Сбрасываем scale в (1, 1, 1)
            Debug.Log($"[SkillManager] 🔧 Scale 'input' GameObject сброшен в (1,1,1) (offset fix)");
        }

        // КРИТИЧЕСКОЕ: Используем РОДНОЙ аниматор медведя (совместим с его скелетом)
        Animator bearAnimator = transformationInstance.GetComponentInChildren<Animator>();
        if (bearAnimator != null)
        {
            // Оставляем аниматор медведя как есть (не заменяем!)
            // Переключаемся на его использование
            animator = bearAnimator;

            // Устанавливаем боевую стойку
            if (HasAnimatorParameter(animator, "InBattle"))
            {
                animator.SetBool("InBattle", true);
            }

            Debug.Log($"[SkillManager] 🔧 Используем родной аниматор медведя");
        }

        // КРИТИЧЕСКОЕ: Скрываем оружие во время трансформации (медведь безоружный)
        WeaponAttachment weaponAttachment = GetComponent<WeaponAttachment>();
        if (weaponAttachment != null)
        {
            weaponAttachment.DetachWeapon();
            Debug.Log($"[SkillManager] 🔧 Оружие скрыто (медведь безоружный)");
        }

        // Применяем бонусы
        if (healthSystem != null && skill.hpBonusPercent > 0f)
        {
            transformationHPBonus = healthSystem.MaxHealth * (skill.hpBonusPercent / 100f);
            healthSystem.AddTemporaryMaxHealth(transformationHPBonus);
            Debug.Log($"[SkillManager] ✅ Бонус HP: +{transformationHPBonus:F0} ({skill.hpBonusPercent}%)");
        }
        else
        {
            Debug.LogWarning($"[SkillManager] ⚠️ HealthSystem={healthSystem != null}, HPBonus={skill.hpBonusPercent}%");
        }

        // TODO: Бонус к атаке (сохраняем в переменную для PlayerAttack)
        Debug.Log($"[SkillManager] 🔍 Бонус физ. урона: {skill.physicalDamageBonusPercent}%");

        // Автоматически отключаем через время
        Invoke(nameof(EndTransformation), skill.transformationDuration);

        Debug.Log($"[SkillManager] 🐻 ✅ ТРАНСФОРМАЦИЯ АКТИВИРОВАНА на {skill.transformationDuration}с!");
    }

    /// <summary>
    /// Завершить трансформацию
    /// </summary>
    private void EndTransformation()
    {
        if (!isTransformed) return;

        if (transformationInstance != null)
        {
            Destroy(transformationInstance);
        }

        // Включаем все SkinnedMeshRenderer обратно
        SkinnedMeshRenderer[] originalRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true); // includeInactive=true
        foreach (SkinnedMeshRenderer smr in originalRenderers)
        {
            smr.enabled = true;
            Debug.Log($"[SkillManager] ✅ Включён рендерер: {smr.gameObject.name}");
        }

        // Убираем бонус HP
        if (healthSystem != null && transformationHPBonus > 0f)
        {
            healthSystem.RemoveTemporaryMaxHealth(transformationHPBonus);
            transformationHPBonus = 0f;
        }

        // КРИТИЧЕСКОЕ: Восстанавливаем оружие после трансформации
        WeaponAttachment weaponAttachment = GetComponent<WeaponAttachment>();
        if (weaponAttachment != null)
        {
            weaponAttachment.AttachWeapon();
            Debug.Log($"[SkillManager] ✅ Оружие восстановлено");
        }

        isTransformed = false;

        // НОВОЕ: Отправляем на сервер окончание трансформации
        if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
        {
            SocketIOManager.Instance.SendTransformationEnd();
        }

        Debug.Log("[SkillManager] 🐻 Трансформация завершена");
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
        if (transformationInstance != null)
        {
            Destroy(transformationInstance);
        }
    }

    /// <summary>
    /// Проверить есть ли параметр в Animator
    /// </summary>
    private bool HasAnimatorParameter(Animator anim, string paramName)
    {
        if (anim == null) return false;

        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }
}
