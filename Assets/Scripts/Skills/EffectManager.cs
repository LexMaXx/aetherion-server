using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Управляет активными эффектами на персонаже (DoT, CC, Buffs, Debuffs)
/// Обрабатывает тики урона/лечения, блокировку действий, модификаторы статов
/// </summary>
public class EffectManager : MonoBehaviour
{
    [Header("═══════════════════════════════════════════")]
    [Header("АКТИВНЫЕ ЭФФЕКТЫ")]
    [Header("═══════════════════════════════════════════")]

    [Tooltip("Список активных эффектов на персонаже")]
    [SerializeField] private List<ActiveEffect> activeEffects = new List<ActiveEffect>();

    [Header("═══════════════════════════════════════════")]
    [Header("КОМПОНЕНТЫ")]
    [Header("═══════════════════════════════════════════")]

    private HealthSystem healthSystem;
    private CharacterStats characterStats;
    private PlayerController playerController; // Для блокировки движения (NetworkPlayer)
    private SimplePlayerController simplePlayerController; // Для тестовой сцены
    private Animator animator;

    [Header("═══════════════════════════════════════════")]
    [Header("DEBUG")]
    [Header("═══════════════════════════════════════════")]

    [SerializeField] private bool showDebugLogs = true;

    // Счётчик для генерации уникальных ID эффектов
    private int effectIdCounter = 0;

    // ════════════════════════════════════════════════════════════
    // ИНИЦИАЛИЗАЦИЯ
    // ════════════════════════════════════════════════════════════

    void Start()
    {
        healthSystem = GetComponent<HealthSystem>();
        characterStats = GetComponent<CharacterStats>();
        playerController = GetComponent<PlayerController>();
        simplePlayerController = GetComponent<SimplePlayerController>(); // Для тестовой сцены
        animator = GetComponentInChildren<Animator>();

        Log("✅ EffectManager инициализирован");
    }

    void Update()
    {
        UpdateEffects();
    }

    // ════════════════════════════════════════════════════════════
    // ПРИМЕНЕНИЕ ЭФФЕКТОВ
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Применить эффект к персонажу
    /// </summary>
    public void ApplyEffect(EffectConfig config, CharacterStats casterStats, string casterSocketId = "")
    {
        if (config == null)
        {
            Log("❌ EffectConfig is NULL!");
            return;
        }

        Debug.Log($"[EffectManager] 🎯 ApplyEffect вызван: {config.effectType} на {gameObject.name}, duration={config.duration}s");

        // ═══════════════════════════════════════════════════════
        // ПРОВЕРКА: Может ли эффект быть применён
        // ═══════════════════════════════════════════════════════

        // Проверка на иммунитет (Invulnerability блокирует дебафы)
        if (HasInvulnerability() && config.IsDebuff())
        {
            Log($"🛡️ Иммунитет! Эффект {config.effectType} заблокирован");
            return;
        }

        // ═══════════════════════════════════════════════════════
        // ПРОВЕРКА: Стаки
        // ═══════════════════════════════════════════════════════

        if (!config.canStack)
        {
            // Эффект не стакается - проверяем есть ли уже такой
            ActiveEffect existing = activeEffects.FirstOrDefault(e => e.config.effectType == config.effectType);

            if (existing != null)
            {
                // Обновляем длительность
                existing.remainingDuration = config.duration;
                Log($"🔄 Обновлён эффект {config.effectType}, новая длительность: {config.duration:F1}с");
                return;
            }
        }
        else
        {
            // Эффект стакается - проверяем не превышен ли лимит
            int currentStacks = activeEffects.Count(e => e.config.effectType == config.effectType);

            if (currentStacks >= config.maxStacks)
            {
                Log($"⚠️ Достигнут максимум стаков {config.effectType}: {currentStacks}/{config.maxStacks}");
                return;
            }
        }

        // ═══════════════════════════════════════════════════════
        // СОЗДАНИЕ АКТИВНОГО ЭФФЕКТА
        // ═══════════════════════════════════════════════════════

        ActiveEffect effect = new ActiveEffect
        {
            effectId = GenerateEffectId(),
            config = config,
            remainingDuration = config.duration,
            nextTickTime = config.tickInterval,
            casterStats = casterStats,
            casterSocketId = casterSocketId,
            stackCount = 1,
            visualEffect = null
        };

        activeEffects.Add(effect);

        // ═══════════════════════════════════════════════════════
        // ПРИМЕНЕНИЕ МГНОВЕННЫХ ЭФФЕКТОВ
        // ═══════════════════════════════════════════════════════

        ApplyImmediateEffect(effect);

        // ═══════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ═══════════════════════════════════════════════════════

        if (config.particleEffectPrefab != null)
        {
            effect.visualEffect = Instantiate(config.particleEffectPrefab, transform);
            effect.visualEffect.transform.localPosition = Vector3.up * 1.5f; // Над головой
        }

        // Звук применения
        if (config.applySound != null)
        {
            AudioSource.PlayClipAtPoint(config.applySound, transform.position, config.soundVolume);
        }

        // ═══════════════════════════════════════════════════════
        // СЕТЕВАЯ СИНХРОНИЗАЦИЯ
        // ═══════════════════════════════════════════════════════

        if (config.syncWithServer)
        {
            SendEffectToServer(effect);
        }

        Log($"✨ Применён эффект: {config.effectType} ({config.duration}с)");
    }

    /// <summary>
    /// Применить эффект из сети (для мультиплеера)
    /// Вызывается когда получаем эффект от другого игрока через NetworkSyncManager
    /// ВАЖНО: Применяет как визуальные эффекты, так и модификаторы статов!
    /// </summary>
    public void ApplyEffectVisual(EffectConfig config, float duration)
    {
        ActiveEffect effect = new ActiveEffect
        {
            effectId = GenerateEffectId(),
            config = config,
            remainingDuration = duration,
            nextTickTime = config.tickInterval,
            stackCount = 1,
            visualEffect = null
        };

        activeEffects.Add(effect);

        // ═══════════════════════════════════════════════════════
        // КРИТИЧЕСКИ ВАЖНО: Применяем модификаторы статов!
        // ═══════════════════════════════════════════════════════
        // Без этого вызова эффекты работают только визуально,
        // но не влияют на статы (Agility, Perception, и т.д.)
        ApplyImmediateEffect(effect);

        // ═══════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ═══════════════════════════════════════════════════════
        if (config.particleEffectPrefab != null)
        {
            effect.visualEffect = Instantiate(config.particleEffectPrefab, transform);
            effect.visualEffect.transform.localPosition = Vector3.up * 1.5f;
        }

        Log($"✨ Применён сетевой эффект: {config.effectType} (визуал + статы)");
    }

    /// <summary>
    /// Применить мгновенные эффекты (модификаторы статов)
    /// </summary>
    private void ApplyImmediateEffect(ActiveEffect effect)
    {
        EffectConfig config = effect.config;

        // TODO: Реализовать модификаторы статов
        switch (config.effectType)
        {
            case EffectType.IncreaseAttack:
                if (characterStats != null)
                {
                    characterStats.AddAttackModifier(config.power);
                }
                Log($"⚔️ +{config.power}% к атаке");
                break;

            case EffectType.IncreaseDefense:
                if (healthSystem != null)
                {
                    healthSystem.AddDamageReduction(config.power);
                }
                Log($"🛡️ +{config.power}% к защите (снижение урона)");
                break;

            case EffectType.IncreaseSpeed:
                if (simplePlayerController != null)
                {
                    simplePlayerController.AddSpeedModifier(config.power);
                }
                else if (playerController != null)
                {
                    // TODO: Добавить метод в PlayerController для мультиплеера
                    // playerController.AddSpeedModifier(config.power);
                }
                Log($"🏃 +{config.power}% к скорости");
                break;

            case EffectType.DecreaseSpeed:
                if (simplePlayerController != null)
                {
                    simplePlayerController.AddSpeedModifier(-config.power);
                }
                else if (playerController != null)
                {
                    // TODO: Добавить метод в PlayerController для мультиплеера
                    // playerController.AddSpeedModifier(-config.power);
                }
                Log($"🐌 -{config.power}% к скорости (замедление)");
                break;

            case EffectType.IncreasePerception:
                if (characterStats != null)
                {
                    int perceptionBonus = Mathf.RoundToInt(config.power);
                    characterStats.ModifyPerception(perceptionBonus);
                }
                Log($"👁️ +{config.power} к восприятию");
                break;

            case EffectType.DecreasePerception:
                if (characterStats != null)
                {
                    // Устанавливаем perception в 1 (проклятие)
                    characterStats.SetPerception(1);
                }
                Log($"👁️🔻 Perception снижен до 1 (проклятие)");
                break;

            case EffectType.DecreaseAgility:
                if (characterStats != null)
                {
                    // Уменьшаем Agility на config.power, минимум до 1
                    int agilityPenalty = -Mathf.RoundToInt(config.power);
                    characterStats.ModifyAgility(agilityPenalty);
                }
                Log($"🏃🔻 -{config.power} к ловкости (минимум 1)");
                break;

            case EffectType.IncreaseAgility:
                if (characterStats != null)
                {
                    // Увеличиваем Agility на config.power
                    int agilityBonus = Mathf.RoundToInt(config.power);
                    characterStats.ModifyAgility(agilityBonus);
                }
                Log($"🏃⚡ +{config.power} к ловкости");
                break;

            case EffectType.IncreaseCritDamage:
                Log($"💥 DEBUG: IncreaseCritDamage вызван! power={config.power}, characterStats={characterStats != null}");
                if (characterStats != null)
                {
                    characterStats.AddCritDamageModifier(config.power);
                    Log($"💥 DEBUG: После AddCritDamageModifier, модификатор сейчас = {characterStats.CritDamageModifier}%");
                }
                Log($"💥 +{config.power}% к критическому урону");
                break;

            case EffectType.Shield:
                // healthSystem.AddShield(config.power);
                Log($"🛡️ Щит на {config.power} HP");
                break;

            case EffectType.Invulnerability:
                Log($"🛡️⚡ НЕУЯЗВИМОСТЬ активирована! Длительность: {config.duration}с (Divine Protection)");
                break;

            case EffectType.Stun:
                Log($"😵 Оглушение!");
                break;

            case EffectType.Root:
                Log($"🌿 Корни!");
                break;

            case EffectType.Sleep:
                Log($"😴 Сон!");
                break;

            case EffectType.Silence:
                Log($"🔇 Молчание!");
                break;

            case EffectType.Fear:
                Log($"😱 Страх!");
                break;

            case EffectType.SummonMinion:
                // Призыв миньона будет обрабатываться в SkillExecutor
                Log($"💀 Призыв миньона (урон: {config.power})");
                break;
        }
    }

    /// <summary>
    /// Удалить эффект
    /// </summary>
    public void RemoveEffect(int effectId)
    {
        ActiveEffect effect = activeEffects.FirstOrDefault(e => e.effectId == effectId);

        if (effect == null)
        {
            Log($"⚠️ Эффект с ID {effectId} не найден");
            return;
        }

        RemoveEffect(effect);
    }

    /// <summary>
    /// Удалить эффект (внутренний метод)
    /// </summary>
    private void RemoveEffect(ActiveEffect effect)
    {
        // Снять модификаторы
        RemoveImmediateEffect(effect);

        // Удалить визуал
        if (effect.visualEffect != null)
        {
            Destroy(effect.visualEffect);
        }

        // Звук окончания
        if (effect.config.removeSound != null)
        {
            AudioSource.PlayClipAtPoint(effect.config.removeSound, transform.position, effect.config.soundVolume);
        }

        // Удалить из списка
        activeEffects.Remove(effect);

        Log($"🔚 Снят эффект: {effect.config.effectType}");
    }

    /// <summary>
    /// Снять модификаторы эффекта
    /// </summary>
    private void RemoveImmediateEffect(ActiveEffect effect)
    {
        EffectConfig config = effect.config;

        // TODO: Снять модификаторы
        switch (config.effectType)
        {
            case EffectType.IncreaseAttack:
                if (characterStats != null)
                {
                    characterStats.RemoveAttackModifier(config.power);
                }
                break;

            case EffectType.IncreaseDefense:
                if (healthSystem != null)
                {
                    healthSystem.RemoveDamageReduction(config.power);
                }
                break;

            case EffectType.IncreaseSpeed:
                if (simplePlayerController != null)
                {
                    simplePlayerController.RemoveSpeedModifier(config.power);
                }
                else if (playerController != null)
                {
                    // TODO: Добавить метод в PlayerController для мультиплеера
                    // playerController.RemoveSpeedModifier(config.power);
                }
                break;

            case EffectType.DecreaseSpeed:
                if (simplePlayerController != null)
                {
                    simplePlayerController.RemoveSpeedModifier(-config.power);
                }
                else if (playerController != null)
                {
                    // TODO: Добавить метод в PlayerController для мультиплеера
                    // playerController.RemoveSpeedModifier(-config.power);
                }
                break;

            case EffectType.IncreasePerception:
                if (characterStats != null)
                {
                    int perceptionBonus = Mathf.RoundToInt(config.power);
                    characterStats.ModifyPerception(-perceptionBonus); // Убираем бонус
                }
                break;

            case EffectType.DecreasePerception:
                if (characterStats != null)
                {
                    // Восстанавливаем оригинальный perception
                    characterStats.RestorePerception();
                }
                break;

            case EffectType.DecreaseAgility:
                if (characterStats != null)
                {
                    // Восстанавливаем Agility (убираем пенальти)
                    int agilityPenalty = -Mathf.RoundToInt(config.power);
                    characterStats.ModifyAgility(-agilityPenalty); // Обратная операция
                }
                break;

            case EffectType.IncreaseAgility:
                if (characterStats != null)
                {
                    // Убираем бонус Agility
                    int agilityBonus = Mathf.RoundToInt(config.power);
                    characterStats.ModifyAgility(-agilityBonus);
                }
                break;

            case EffectType.IncreaseCritDamage:
                if (characterStats != null)
                {
                    characterStats.RemoveCritDamageModifier(config.power);
                }
                break;
        }
    }

    // ════════════════════════════════════════════════════════════
    // ОБНОВЛЕНИЕ ЭФФЕКТОВ
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Обновление эффектов каждый кадр
    /// </summary>
    private void UpdateEffects()
    {
        if (activeEffects.Count == 0) return;

        List<ActiveEffect> expiredEffects = new List<ActiveEffect>();

        foreach (ActiveEffect effect in activeEffects)
        {
            // Уменьшаем оставшееся время
            effect.remainingDuration -= Time.deltaTime;

            // Проверка истекло ли время
            if (effect.remainingDuration <= 0f)
            {
                expiredEffects.Add(effect);
                continue;
            }

            // ═══════════════════════════════════════════════════════
            // ТИКИ УРОНА/ЛЕЧЕНИЯ
            // ═══════════════════════════════════════════════════════

            if (effect.config.IsDamageOverTime() || effect.config.IsHealOverTime())
            {
                effect.nextTickTime -= Time.deltaTime;

                if (effect.nextTickTime <= 0f)
                {
                    // Время для тика
                    ProcessEffectTick(effect);

                    // Сброс таймера
                    effect.nextTickTime = effect.config.tickInterval;
                }
            }
        }

        // Удаляем истёкшие эффекты
        foreach (ActiveEffect effect in expiredEffects)
        {
            RemoveEffect(effect);
        }
    }

    /// <summary>
    /// Обработать тик эффекта (урон или лечение)
    /// </summary>
    private void ProcessEffectTick(ActiveEffect effect)
    {
        float tickValue = effect.config.CalculateTickDamage(effect.casterStats);

        if (effect.config.IsDamageOverTime())
        {
            // Урон во времени
            if (healthSystem != null)
            {
                healthSystem.TakeDamage(tickValue);
                Log($"💀 DoT тик: -{tickValue:F0} HP от {effect.config.effectType}");
            }

            // Sleep сбрасывается при получении урона
            if (effect.config.effectType == EffectType.Sleep)
            {
                ActiveEffect sleepEffect = activeEffects.FirstOrDefault(e => e.config.effectType == EffectType.Sleep);
                if (sleepEffect != null)
                {
                    RemoveEffect(sleepEffect);
                    Log("😴 Сон прерван уроном!");
                }
            }
        }
        else if (effect.config.IsHealOverTime())
        {
            // Лечение во времени
            if (healthSystem != null)
            {
                healthSystem.Heal(tickValue);
                Log($"💚 HoT тик: +{tickValue:F0} HP от {effect.config.effectType}");
            }
        }
    }

    // ════════════════════════════════════════════════════════════
    // ПРОВЕРКИ
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Находится ли персонаж под контролем (Stun, Sleep, Fear)
    /// </summary>
    public bool IsUnderCrowdControl()
    {
        return activeEffects.Any(e => e.config.IsCrowdControl());
    }

    /// <summary>
    /// Может ли персонаж двигаться
    /// </summary>
    public bool CanMove()
    {
        return !activeEffects.Any(e => e.config.BlocksMovement());
    }

    /// <summary>
    /// Может ли персонаж атаковать
    /// </summary>
    public bool CanAttack()
    {
        return !activeEffects.Any(e => e.config.BlocksAttacks());
    }

    /// <summary>
    /// Может ли персонаж использовать скиллы
    /// </summary>
    public bool CanUseSkills()
    {
        return !activeEffects.Any(e => e.config.BlocksSkills());
    }

    /// <summary>
    /// Есть ли неуязвимость
    /// </summary>
    public bool HasInvulnerability()
    {
        return activeEffects.Any(e => e.config.effectType == EffectType.Invulnerability);
    }

    /// <summary>
    /// Есть ли невидимость
    /// </summary>
    public bool HasInvisibility()
    {
        return activeEffects.Any(e => e.config.effectType == EffectType.Invisibility);
    }

    /// <summary>
    /// Получить активный эффект по типу
    /// </summary>
    public ActiveEffect GetEffect(EffectType type)
    {
        return activeEffects.FirstOrDefault(e => e.config.effectType == type);
    }

    /// <summary>
    /// Получить все активные эффекты
    /// </summary>
    public List<ActiveEffect> GetAllEffects()
    {
        return new List<ActiveEffect>(activeEffects);
    }

    /// <summary>
    /// Снять все эффекты (Dispel, Cleanse)
    /// </summary>
    public void DispelAllEffects()
    {
        List<ActiveEffect> dispellable = activeEffects.Where(e => e.config.canBeDispelled).ToList();

        foreach (ActiveEffect effect in dispellable)
        {
            RemoveEffect(effect);
        }

        Log($"✨ Снято {dispellable.Count} эффектов (Dispel)");
    }

    /// <summary>
    /// Снять эффекты определённого типа
    /// </summary>
    public void DispelEffectType(EffectType type)
    {
        List<ActiveEffect> toRemove = activeEffects.Where(e => e.config.effectType == type && e.config.canBeDispelled).ToList();

        foreach (ActiveEffect effect in toRemove)
        {
            RemoveEffect(effect);
        }
    }

    // ════════════════════════════════════════════════════════════
    // СЕТЕВАЯ СИНХРОНИЗАЦИЯ
    // ════════════════════════════════════════════════════════════

    private void SendEffectToServer(ActiveEffect effect)
    {
        if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
        {
            Log($"⚠️ SocketIOManager не подключен, эффект {effect.config.effectType} НЕ отправлен");
            return;
        }

        // Определяем targetSocketId (на кого применяется эффект)
        string targetSocketId = "";

        // Если это наш локальный игрок - targetSocketId = "" (пустая строка = мы сами)
        // Если это NetworkPlayer - берём его socketId
        NetworkPlayer networkPlayer = GetComponent<NetworkPlayer>();
        if (networkPlayer != null)
        {
            targetSocketId = networkPlayer.socketId;
        }

        // Отправляем эффект на сервер
        SocketIOManager.Instance.SendEffectApplied(effect.config, targetSocketId);

        Log($"📡 Эффект {effect.config.effectType} отправлен на сервер (target={targetSocketId})");
    }

    // ════════════════════════════════════════════════════════════
    // УТИЛИТЫ
    // ════════════════════════════════════════════════════════════

    private int GenerateEffectId()
    {
        return ++effectIdCounter;
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[EffectManager] {message}");
        }
    }

    // ════════════════════════════════════════════════════════════
    // DEBUG UI
    // ════════════════════════════════════════════════════════════

    void OnGUI()
    {
        if (!showDebugLogs) return;

        // Показываем активные эффекты в левом верхнем углу
        GUILayout.BeginArea(new Rect(10, 200, 300, 400));
        GUILayout.Label($"=== Активные эффекты ({activeEffects.Count}) ===");

        foreach (ActiveEffect effect in activeEffects)
        {
            string color = effect.config.IsBuff() ? "green" : "red";
            GUILayout.Label($"<color={color}>{effect.config.effectType} ({effect.remainingDuration:F1}с)</color>");
        }

        GUILayout.EndArea();
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // МЕТОДЫ ДЛЯ ОБРАТНОЙ СОВМЕСТИМОСТИ
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Проверить активен ли Root/Stun эффект (блокирует движение)
    /// Используется в ThirdPersonController и SkillExecutor
    /// </summary>
    public bool IsRooted()
    {
        return !CanMove();
    }

    /// <summary>
    /// Добавить эффект на персонажа (упрощённая версия ApplyEffect)
    /// Используется в SkillManager для обратной совместимости
    /// </summary>
    public void AddEffect(EffectConfig config, Transform caster)
    {
        CharacterStats casterStats = caster != null ? caster.GetComponent<CharacterStats>() : null;
        ApplyEffect(config, casterStats, "");
    }

    /// <summary>
    /// Получить список активных эффектов (для UI отображения)
    /// </summary>
    public List<ActiveEffect> GetActiveEffects()
    {
        return new List<ActiveEffect>(activeEffects);
    }
}

// ════════════════════════════════════════════════════════════
// КЛАСС: АКТИВНЫЙ ЭФФЕКТ
// ════════════════════════════════════════════════════════════

/// <summary>
/// Активный эффект на персонаже (инстанс EffectConfig)
/// </summary>
[System.Serializable]
public class ActiveEffect
{
    [Tooltip("Уникальный ID эффекта (для удаления)")]
    public int effectId;

    [Tooltip("Конфигурация эффекта")]
    public EffectConfig config;

    [Tooltip("Оставшееся время действия")]
    public float remainingDuration;

    [Tooltip("Время до следующего тика (для DoT/HoT)")]
    public float nextTickTime;

    [Tooltip("Статы кастера (для расчёта DoT/HoT)")]
    public CharacterStats casterStats;

    [Tooltip("SocketId кастера (для сетевой синхронизации)")]
    public string casterSocketId;

    [Tooltip("Количество стаков")]
    public int stackCount;

    [Tooltip("Визуальный эффект (частицы)")]
    public GameObject visualEffect;
}
