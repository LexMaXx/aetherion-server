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

    // Трансформация (для Paladin) - SIMPLE TRANSFORMATION APPROACH
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
        if (skill == null)
        {
            Debug.LogError("[SkillManager] ❌ Skill is NULL!");
            return false;
        }

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

        // Проигрываем анимацию с настройками из SkillData
        PlaySkillAnimation(skill);

        // НОВОЕ: Выполняем движение если включено
        if (skill.enableMovement)
        {
            PerformSkillMovement(skill, target);
        }

        // Блокируем движение если требуется
        if (skill.blockMovementDuringCast)
        {
            BlockMovement(skill);
        }

        // Проигрываем звук каста
        if (skill.castSound != null)
        {
            AudioSource.PlayClipAtPoint(skill.castSound, transform.position);
        }

        // НОВОЕ: Отправляем скилл на сервер для синхронизации в мультиплеере
        SendSkillToServer(skill, target);

        // Выполняем скилл локально (с задержкой castTime если есть)
        if (skill.castTime > 0f)
        {
            StartCoroutine(ExecuteSkillAfterCastTime(skill, target));
        }
        else
        {
            ExecuteSkill(skill, target);
        }

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

            // СПЕЦИАЛЬНАЯ ОБРАБОТКА ДЛЯ ICE NOVA (skillId 202) - создаём радиальные снаряды
            if (skill.skillId == 202 && skill.projectilePrefab != null)
            {
                SpawnIceNovaShards(skill, center, damage);
            }
            // Обычный визуальный эффект для других AOE скилов
            else if (skill.visualEffectPrefab != null)
            {
                GameObject effectObj = Instantiate(skill.visualEffectPrefab, center, Quaternion.identity);
                Debug.Log($"[SkillManager] ✨ AOE визуальный эффект создан в центре: {skill.visualEffectPrefab.name}");

                // СИНХРОНИЗАЦИЯ: Отправляем AOE визуальный эффект на сервер
                if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
                {
                    string effectName = skill.visualEffectPrefab.name;
                    SocketIOManager.Instance.SendVisualEffect(
                        "aoe", // тип эффекта
                        effectName, // название prefab
                        center, // позиция центра AOE
                        Quaternion.identity, // ротация
                        "", // не привязан к игроку
                        0f // длительность автоматически
                    );
                    Debug.Log($"[SkillManager] ✨ AOE эффект отправлен на сервер: {effectName}");
                }
            }

            Collider[] hits = Physics.OverlapSphere(center, skill.aoeRadius);

            int hitCount = 0;
            foreach (Collider hit in hits)
            {
                if (hitCount >= skill.maxTargets) break;

                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);

                    // ВАЖНО: Применяем все эффекты из SkillData к врагу!
                    if (skill.effects != null && skill.effects.Count > 0)
                    {
                        foreach (SkillEffect effect in skill.effects)
                        {
                            ApplyEffect(effect, enemy.transform);
                        }
                        Debug.Log($"[SkillManager] ✅ Применено {skill.effects.Count} эффектов к {enemy.GetEnemyName()}");
                    }

                    // Визуальный эффект попадания на враге
                    if (skill.projectileHitEffectPrefab != null)
                    {
                        Instantiate(skill.projectileHitEffectPrefab, enemy.transform.position, Quaternion.identity);
                    }

                    hitCount++;
                }
            }

            Debug.Log($"[SkillManager] 💥 AOE урон: {damage} по {hitCount} целям");
        }
        // Одиночный урон
        else if (target != null)
        {
            // ВАЖНО: Если есть снаряд - НЕ наносим урон напрямую! Снаряд нанесёт урон при попадании
            if (skill.projectilePrefab != null)
            {
                Debug.Log($"[SkillManager] 🔍 Префаб снаряда найден для {skill.skillName}: {skill.projectilePrefab.name}");

                // Создаём снаряд (только для локального игрока)
                NetworkPlayer networkPlayer = GetComponent<NetworkPlayer>();
                if (networkPlayer == null)
                {
                    Debug.Log($"[SkillManager] ✅ Локальный игрок - создаём снаряд");
                    SpawnProjectile(skill, target, damage);
                }
                else
                {
                    Debug.Log($"[SkillManager] ⏭️ NetworkPlayer - пропускаем создание снаряда");
                }
            }
            // Нет снаряда - урон напрямую (мгновенный)
            else
            {
                Enemy enemy = target.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);

                    // Визуальный эффект
                    if (skill.visualEffectPrefab != null)
                    {
                        GameObject effectObj = Instantiate(skill.visualEffectPrefab, target.position, Quaternion.identity);

                        // СИНХРОНИЗАЦИЯ: Отправляем визуальный эффект на сервер
                        if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
                        {
                            string effectName = skill.visualEffectPrefab.name;
                            SocketIOManager.Instance.SendVisualEffect(
                                "skill_hit", // тип эффекта
                                effectName, // название prefab
                                target.position, // позиция
                                Quaternion.identity, // ротация
                                "", // не привязан к игроку
                                0f // длительность автоматически
                            );
                            Debug.Log($"[SkillManager] ✨ Визуальный эффект скилла отправлен на сервер: {effectName}");
                        }
                    }

                    Debug.Log($"[SkillManager] 💥 Урон (мгновенный): {damage}");
                }
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
                GameObject effectObj = Instantiate(skill.visualEffectPrefab, healTarget.position, Quaternion.identity);

                // СИНХРОНИЗАЦИЯ: Отправляем визуальный эффект на сервер
                if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
                {
                    string effectName = skill.visualEffectPrefab.name;
                    SocketIOManager.Instance.SendVisualEffect(
                        "heal", // тип эффекта
                        effectName, // название prefab
                        healTarget.position, // позиция
                        Quaternion.identity, // ротация
                        "", // не привязан к игроку (можно добавить привязку)
                        0f // длительность автоматически
                    );
                    Debug.Log($"[SkillManager] ✨ Визуальный эффект лечения отправлен на сервер: {effectName}");
                }
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

        // СНАРЯД (для Hammer of Justice и других projectile-based CC скиллов)
        if (skill.projectilePrefab != null && target != null)
        {
            // Проверяем это NetworkPlayer (враг)?
            NetworkPlayer networkPlayer = GetComponent<NetworkPlayer>();
            if (networkPlayer == null)
            {
                // Создаём снаряд только для локального игрока
                float damage = skill.CalculateDamage(characterStats);
                SpawnProjectile(skill, target, damage);
                Debug.Log($"[SkillManager] ⚔️ Снаряд создан для {skill.skillName}, эффектов: {skill.effects.Count}");
            }
            else
            {
                Debug.Log($"[SkillManager] ⏭️ NetworkPlayer detected - пропускаем снаряд для {skill.skillName}");
            }

            // КРИТИЧЕСКОЕ: Если есть снаряд - НЕ применяем эффекты сразу!
            // Снаряд применит эффекты при попадании через Projectile.ApplyEffects()
            Debug.Log($"[SkillManager] ⏭️ Эффекты будут применены снарядом при попадании");
        }
        // Без снаряда - применяем эффекты сразу (баффы на себя, дебаффы в области и т.д.)
        else
        {
            // Визуальный эффект (для скиллов без снарядов)
            if (skill.visualEffectPrefab != null)
            {
                Instantiate(skill.visualEffectPrefab, effectTarget.position, Quaternion.identity);
            }

            // Применяем все эффекты скилла
            foreach (SkillEffect effect in skill.effects)
            {
                ApplyEffect(effect, effectTarget);
            }

            Debug.Log($"[SkillManager] ✨ Применено эффектов: {skill.effects.Count}");
        }
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
    /// Скилл трансформации (Paladin - медведь) - SIMPLE TRANSFORMATION
    /// Создаём модель медведя как child объект, скрываем паладина
    /// </summary>
    private void ExecuteTransformationSkill(SkillData skill)
    {
        Debug.Log($"[SkillManager] 🔍 ExecuteTransformationSkill (SIMPLE TRANSFORMATION) вызван для {skill.skillName}");

        if (skill.transformationModel == null)
        {
            Debug.LogError("[SkillManager] ❌ НЕТ МОДЕЛИ ДЛЯ ТРАНСФОРМАЦИИ! Проверь Paladin_BearForm.asset в инспекторе!");
            return;
        }

        // ВИЗУАЛЬНЫЙ ЭФФЕКТ ТРАНСФОРМАЦИИ (дым/магия)
        if (skill.visualEffectPrefab != null)
        {
            GameObject effectObj = Instantiate(skill.visualEffectPrefab, transform.position, Quaternion.identity);
            Debug.Log($"[SkillManager] ✨ Эффект трансформации проигран: {skill.visualEffectPrefab.name}");

            // СИНХРОНИЗАЦИЯ: Отправляем визуальный эффект трансформации на сервер
            if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
            {
                string effectName = skill.visualEffectPrefab.name;
                SocketIOManager.Instance.SendVisualEffect(
                    "transformation", // тип эффекта
                    effectName, // название prefab
                    transform.position, // позиция
                    Quaternion.identity, // ротация
                    "", // не привязан к игроку (можно привязать если нужно)
                    0f // длительность автоматически
                );
                Debug.Log($"[SkillManager] ✨ Эффект трансформации отправлен на сервер: {effectName}");
            }
        }

        // Получаем или добавляем SimpleTransformation компонент
        SimpleTransformation simpleTransformation = GetComponent<SimpleTransformation>();
        if (simpleTransformation == null)
        {
            simpleTransformation = gameObject.AddComponent<SimpleTransformation>();
            Debug.Log($"[SkillManager] ➕ Добавлен SimpleTransformation компонент");
        }

        // Выполняем трансформацию (передаём аниматор паладина явно)
        bool success = simpleTransformation.TransformTo(skill.transformationModel, animator);
        if (!success)
        {
            Debug.LogError("[SkillManager] ❌ Трансформация не удалась!");
            return;
        }

        isTransformed = true;

        Debug.Log($"[SkillManager] 🐻 ✅ Трансформация завершена - паладин превратился в медведя!");

        // Применяем бонусы
        if (healthSystem != null && skill.hpBonusPercent > 0f)
        {
            transformationHPBonus = healthSystem.MaxHealth * (skill.hpBonusPercent / 100f);
            healthSystem.AddTemporaryMaxHealth(transformationHPBonus);
            Debug.Log($"[SkillManager] ✅ Бонус HP: +{transformationHPBonus:F0} ({skill.hpBonusPercent}%)");
        }

        // Автоматически отключаем через время
        Invoke(nameof(EndTransformation), skill.transformationDuration);

        Debug.Log($"[SkillManager] 🐻 ✅ ТРАНСФОРМАЦИЯ АКТИВИРОВАНА (SIMPLE TRANSFORMATION) на {skill.transformationDuration}с!");
    }

    /// <summary>
    /// Завершить трансформацию - SIMPLE TRANSFORMATION
    /// </summary>
    private void EndTransformation()
    {
        if (!isTransformed) return;

        // Получаем SimpleTransformation компонент
        SimpleTransformation simpleTransformation = GetComponent<SimpleTransformation>();
        if (simpleTransformation != null)
        {
            // Возвращаем паладина (удаляем медведя, показываем паладина)
            simpleTransformation.RevertToOriginal();
            Debug.Log($"[SkillManager] ✅ Паладин восстановлен");
        }

        // Убираем бонус HP
        if (healthSystem != null && transformationHPBonus > 0f)
        {
            healthSystem.RemoveTemporaryMaxHealth(transformationHPBonus);
            transformationHPBonus = 0f;
        }

        isTransformed = false;

        // Отправляем на сервер окончание трансформации
        if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
        {
            SocketIOManager.Instance.SendTransformationEnd();
        }

        Debug.Log("[SkillManager] 🐻 Трансформация завершена (SIMPLE TRANSFORMATION)");
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
        if (skillCooldowns.Count == 0) return;

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
    /// Проверка неуязвимости
    /// </summary>
    public bool IsInvulnerable()
    {
        foreach (ActiveEffect effect in activeEffects)
        {
            if (effect.isInvulnerable)
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
    /// ОБНОВЛЕНО: Теперь передает skillType, анимацию и визуальные эффекты
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

        // Отправляем на сервер с информацией об анимации
        Vector3 targetPos = target != null ? target.position : transform.position;
        SocketIOManager.Instance.SendPlayerSkillWithAnimation(
            skill.skillId,
            targetSocketId,
            targetPos,
            skillType,
            skill.animationTrigger,
            skill.animationSpeed,
            skill.castTime
        );

        Debug.Log($"[SkillManager] 📡 Скилл {skill.skillName} (ID:{skill.skillId}, тип:{skillType}, анимация:{skill.animationTrigger}, castTime:{skill.castTime}с) отправлен на сервер");
    }

    /// <summary>
    /// Отправить создание снаряда на сервер для синхронизации
    /// </summary>
    private void SendProjectileToServer(int skillId, Vector3 spawnPosition, Vector3 direction, Transform target)
    {
        // Проверяем что мы в мультиплеере
        if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
        {
            Debug.Log("[SkillManager] Не в мультиплеере - пропускаем отправку снаряда на сервер");
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

        // Отправляем на сервер
        SocketIOManager.Instance.SendProjectileSpawned(skillId, spawnPosition, direction, targetSocketId);

        Debug.Log($"[SkillManager] 🚀 Снаряд отправлен на сервер: skillId={skillId}, pos={spawnPosition}, dir={direction}");
    }

    /// <summary>
    /// Создать ледяные осколки для Ice Nova (радиально во все стороны)
    /// ВАЖНО: Только для локального игрока! Не для NetworkPlayer!
    /// </summary>
    private void SpawnIceNovaShards(SkillData skill, Vector3 center, float damage)
    {
        // КРИТИЧЕСКОЕ: Проверяем это NetworkPlayer (враг)?
        NetworkPlayer networkPlayer = GetComponent<NetworkPlayer>();
        if (networkPlayer != null)
        {
            Debug.Log($"[SkillManager] ⏭️ NetworkPlayer detected - пропускаем визуальные эффекты Ice Nova");
            return;
        }

        int shardCount = 12;
        float spawnHeight = 1f;
        float angleStep = 360f / shardCount;

        Vector3 spawnPosition = center + Vector3.up * spawnHeight;

        for (int i = 0; i < shardCount; i++)
        {
            // Вычисляем угол для этого осколка
            float angle = i * angleStep;

            // Добавляем случайность
            angle += Random.Range(-angleStep * 0.2f, angleStep * 0.2f);

            // Направление
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            // Создаем осколок
            GameObject shard = Instantiate(skill.projectilePrefab, spawnPosition, Quaternion.LookRotation(direction));

            // Настраиваем Projectile
            Projectile projectile = shard.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(null, damage, direction, gameObject, skill.effects);
            }

            // СИНХРОНИЗАЦИЯ: Отправляем каждый осколок на сервер для мультиплеера
            if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
            {
                SocketIOManager.Instance.SendProjectileSpawned(
                    skill.skillId, // 202 для Ice Nova
                    spawnPosition,
                    direction,
                    "" // targetSocketId - осколки не наводятся
                );
                Debug.Log($"[SkillManager] 📡 Ice Nova shard {i + 1} sent to server: angle={angle:F1}°, dir={direction}");
            }
        }

        Debug.Log($"[SkillManager] ❄️ Ice Nova: Spawned {shardCount} ice shards!");
    }

    /// <summary>
    /// Создать снаряд (НОВОЕ - centralized method)
    /// </summary>
    private void SpawnProjectile(SkillData skill, Transform target, float damage)
    {
        // DEBUG: Проверяем что префаб загружен
        if (skill.projectilePrefab == null)
        {
            Debug.LogError($"[SkillManager] ❌ projectilePrefab == NULL для скилла {skill.skillName}!");
            return;
        }

        Debug.Log($"[SkillManager] 📦 Создаём снаряд из префаба: {skill.projectilePrefab.name}");

        GameObject projectile = Instantiate(skill.projectilePrefab, transform.position + Vector3.up, Quaternion.identity);

        Debug.Log($"[SkillManager] ✅ Снаряд создан в сцене: {projectile.name}");

        Projectile proj = projectile.GetComponent<Projectile>();

        if (proj != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            Vector3 spawnPosition = transform.position + Vector3.up;

            // НОВОЕ: Используем InitializeFromSkill для применения всех настроек из SkillData
            proj.InitializeFromSkill(skill, target, direction, gameObject);

            Debug.Log($"[SkillManager] 🚀 Снаряд инициализирован: {skill.projectilePrefab.name}, урон: {damage}, скорость: {skill.projectileSpeed}, homing: {skill.projectileHoming}");

            // КРИТИЧЕСКОЕ: Отправляем на сервер для синхронизации с другими игроками
            SendProjectileToServer(skill.skillId, spawnPosition, direction, target);
        }
        else
        {
            Debug.LogError($"[SkillManager] ❌ У префаба {projectile.name} нет компонента Projectile!");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // СИСТЕМА АНИМАЦИЙ СКИЛЛОВ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Проиграть анимацию скилла с настройками скорости
    /// ОБНОВЛЕНО: Всегда играет "Attack" анимацию (как у мага) перед любым скиллом!
    /// </summary>
    private void PlaySkillAnimation(SkillData skill)
    {
        if (animator == null)
        {
            Debug.LogWarning("[SkillManager] ⚠️ Animator отсутствует!");
            return;
        }

        // КРИТИЧЕСКОЕ ИЗМЕНЕНИЕ: Всегда используем "Attack" как анимацию каста!
        // Это стандартная анимация каста магии/скилла для ВСЕХ классов
        string castAnimationTrigger = "Attack";

        // Если у скилла задан свой триггер - используем его, иначе "Attack"
        if (!string.IsNullOrEmpty(skill.animationTrigger))
        {
            castAnimationTrigger = skill.animationTrigger;
        }

        // Устанавливаем скорость анимации
        float previousSpeed = animator.speed;
        animator.speed = skill.animationSpeed;

        // Запускаем триггер анимации каста
        animator.SetTrigger(castAnimationTrigger);

        Debug.Log($"[SkillManager] 🎬 Анимация КАСТА: триггер='{castAnimationTrigger}', скорость={skill.animationSpeed}x");

        // Восстанавливаем скорость анимации через castTime или небольшую задержку
        float resetDelay = skill.castTime > 0f ? skill.castTime : 1f;
        StartCoroutine(ResetAnimationSpeed(previousSpeed, resetDelay));
    }

    /// <summary>
    /// Восстановить скорость анимации после каста
    /// </summary>
    private System.Collections.IEnumerator ResetAnimationSpeed(float originalSpeed, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (animator != null)
        {
            animator.speed = originalSpeed;
            Debug.Log($"[SkillManager] ⏱️ Скорость анимации восстановлена: {originalSpeed}x");
        }
    }

    /// <summary>
    /// Выполнить скилл после времени каста
    /// </summary>
    private System.Collections.IEnumerator ExecuteSkillAfterCastTime(SkillData skill, Transform target)
    {
        Debug.Log($"[SkillManager] ⏳ Каст скилла {skill.skillName}... Ждём {skill.castTime}с");

        yield return new WaitForSeconds(skill.castTime);

        Debug.Log($"[SkillManager] ✅ Каст завершён! Выполняем скилл {skill.skillName}");
        ExecuteSkill(skill, target);
    }

    /// <summary>
    /// Блокировать движение персонажа во время каста
    /// </summary>
    private void BlockMovement(SkillData skill)
    {
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            StartCoroutine(BlockMovementCoroutine(controller, skill.movementBlockDuration > 0f ? skill.movementBlockDuration : skill.castTime));
        }
    }

    /// <summary>
    /// Корутина блокировки движения
    /// </summary>
    private System.Collections.IEnumerator BlockMovementCoroutine(CharacterController controller, float duration)
    {
        // Если duration == 0, используем castTime или дефолтное значение
        if (duration <= 0f)
        {
            duration = 0.5f; // Минимальная блокировка
        }

        Debug.Log($"[SkillManager] 🔒 Движение заблокировано на {duration}с");

        // Сохраняем состояние контроллера
        bool wasEnabled = controller.enabled;

        // Отключаем контроллер
        controller.enabled = false;

        // Ждём
        yield return new WaitForSeconds(duration);

        // Восстанавливаем состояние
        controller.enabled = wasEnabled;

        Debug.Log($"[SkillManager] 🔓 Движение разблокировано");
    }

    // ═══════════════════════════════════════════════════════════════
    // СИСТЕМА ДВИЖЕНИЯ СКИЛЛОВ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Выполнить движение персонажа при использовании скилла
    /// </summary>
    private void PerformSkillMovement(SkillData skill, Transform target)
    {
        if (!skill.enableMovement || skill.movementType == MovementType.None)
        {
            return;
        }

        // Проигрываем анимацию движения если указана
        if (!string.IsNullOrEmpty(skill.movementAnimationTrigger) && animator != null)
        {
            animator.SetTrigger(skill.movementAnimationTrigger);
            Debug.Log($"[SkillManager] 🏃 Анимация движения: {skill.movementAnimationTrigger}");
        }

        // Определяем целевую позицию
        Vector3 targetPosition = CalculateMovementTarget(skill, target);

        // Выполняем движение в зависимости от типа
        switch (skill.movementType)
        {
            case MovementType.Teleport:
            case MovementType.Blink:
                // Мгновенная телепортация
                TeleportToPosition(targetPosition);
                break;

            case MovementType.Dash:
            case MovementType.Charge:
            case MovementType.Roll:
            case MovementType.Leap:
                // Плавное перемещение
                StartCoroutine(MoveToPosition(targetPosition, skill.movementSpeed));
                break;
        }
    }

    /// <summary>
    /// Вычислить целевую позицию для движения
    /// </summary>
    private Vector3 CalculateMovementTarget(SkillData skill, Transform target)
    {
        Vector3 direction = Vector3.zero;

        switch (skill.movementDirection)
        {
            case MovementDirection.Forward:
                direction = transform.forward;
                break;

            case MovementDirection.Backward:
                direction = -transform.forward;
                break;

            case MovementDirection.ToTarget:
                if (target != null)
                {
                    direction = (target.position - transform.position).normalized;
                }
                else
                {
                    direction = transform.forward;
                }
                break;

            case MovementDirection.AwayFromTarget:
                if (target != null)
                {
                    direction = (transform.position - target.position).normalized;
                }
                else
                {
                    direction = -transform.forward;
                }
                break;

            case MovementDirection.MouseDirection:
                direction = transform.forward;
                break;
        }

        return transform.position + direction * skill.movementDistance;
    }

    /// <summary>
    /// Мгновенная телепортация
    /// </summary>
    private void TeleportToPosition(Vector3 targetPosition)
    {
        CharacterController controller = GetComponent<CharacterController>();

        if (controller != null)
        {
            controller.enabled = false;
            transform.position = targetPosition;
            controller.enabled = true;

            Debug.Log($"[SkillManager] ✨ Телепорт в позицию: {targetPosition}");
        }
        else
        {
            transform.position = targetPosition;
        }
    }

    /// <summary>
    /// Плавное перемещение к позиции
    /// </summary>
    private System.Collections.IEnumerator MoveToPosition(Vector3 targetPosition, float speed)
    {
        CharacterController controller = GetComponent<CharacterController>();
        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float duration = distance / speed;
        float elapsed = 0f;

        Debug.Log($"[SkillManager] 🏃 Начинаем движение, скорость: {speed}м/с, дистанция: {distance:F1}м");

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, t);

            if (controller != null && controller.enabled)
            {
                Vector3 movement = newPosition - transform.position;
                controller.Move(movement);
            }
            else
            {
                transform.position = newPosition;
            }

            yield return null;
        }

        if (controller != null && controller.enabled)
        {
            Vector3 finalMovement = targetPosition - transform.position;
            controller.Move(finalMovement);
        }
        else
        {
            transform.position = targetPosition;
        }

        Debug.Log($"[SkillManager] ✅ Движение завершено");
    }

    void OnDestroy()
    {
        // КРИТИЧЕСКОЕ: Отменяем все Invoke (EndTransformation и другие)
        CancelInvoke();

        ClearSummons();
    }
}
