using UnityEngine;

/// <summary>
/// Активный эффект на персонаже (баф/дебаф/контроль)
/// </summary>
public class ActiveEffect
{
    public SkillEffect effectData;
    public float remainingDuration;
    public float nextTickTime;
    public GameObject particleInstance;
    public Transform target;

    // Для контроля
    public bool isStunned = false;
    public bool isRooted = false;
    public bool isSleeping = false;
    public bool isSilenced = false;

    // Особые эффекты
    public bool isInvulnerable = false;
    public float shieldAmount = 0f; // Щит (поглощает урон)

    public ActiveEffect(SkillEffect effect, Transform target)
    {
        this.effectData = effect;
        this.remainingDuration = effect.duration;
        this.nextTickTime = effect.tickInterval;
        this.target = target;

        // Создаём визуальный эффект если есть
        // КРИТИЧЕСКОЕ: Создаём эффекты для врагов (Enemy) и локального игрока, НО НЕ для NetworkPlayer
        Debug.Log($"[ActiveEffect] 🔍 Проверка эффекта {effect.effectType} для {target.name}");
        Debug.Log($"[ActiveEffect] 🔍 particleEffectPrefab: {(effect.particleEffectPrefab != null ? effect.particleEffectPrefab.name : "NULL")}");

        if (effect.particleEffectPrefab != null && target != null)
        {
            NetworkPlayer networkPlayer = target.GetComponent<NetworkPlayer>();
            Enemy enemy = target.GetComponent<Enemy>();

            Debug.Log($"[ActiveEffect] 🔍 Enemy: {enemy != null}, NetworkPlayer: {networkPlayer != null}");

            // Создаём эффект если это:
            // 1. Враг (Enemy) - всегда показываем эффекты на врагах
            // 2. Локальный игрок (нет NetworkPlayer компонента)
            // НЕ создаём для NetworkPlayer (другие игроки)
            if (enemy != null || networkPlayer == null)
            {
                particleInstance = Object.Instantiate(effect.particleEffectPrefab, target.position, Quaternion.identity, target);
                Debug.Log($"[ActiveEffect] ✨ Визуальный эффект создан на {target.name}: {effect.particleEffectPrefab.name}, position: {target.position}");

                // СИНХРОНИЗАЦИЯ: Отправляем визуальный эффект на сервер (если это локальный игрок)
                if (networkPlayer == null && SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
                {
                    string effectName = effect.particleEffectPrefab.name;
                    string effectType = GetEffectTypeString(effect.effectType);

                    SocketIOManager.Instance.SendVisualEffect(
                        effectType, // тип эффекта ("buff", "debuff", "burn", "poison" и т.д.)
                        effectName, // название prefab
                        target.position, // позиция
                        Quaternion.identity, // ротация
                        "", // TODO: можно привязать к socketId если нужно
                        effect.duration // длительность эффекта
                    );
                    Debug.Log($"[ActiveEffect] ✨ Визуальный эффект {effectType} отправлен на сервер: {effectName}");
                }
            }
            else
            {
                Debug.Log($"[ActiveEffect] ⏭️ NetworkPlayer detected - пропускаем визуальный эффект {effect.effectType}");
            }
        }
        else
        {
            Debug.Log($"[ActiveEffect] ⚠️ Нет particleEffectPrefab или target для эффекта {effect.effectType}");
        }

        // Применяем эффект
        ApplyEffect();

        // Устанавливаем флаги контроля
        switch (effect.effectType)
        {
            case EffectType.Stun:
                isStunned = true;
                break;
            case EffectType.Root:
                isRooted = true;
                break;
            case EffectType.Sleep:
                isSleeping = true;
                break;
            case EffectType.Silence:
                isSilenced = true;
                break;
            case EffectType.Invulnerability:
                isInvulnerable = true;
                Debug.Log("[ActiveEffect] 🛡️ НЕУЯЗВИМОСТЬ активирована!");
                break;
        }
    }

    /// <summary>
    /// Обновление эффекта (вызывается каждый кадр)
    /// </summary>
    public bool Update(float deltaTime)
    {
        remainingDuration -= deltaTime;
        nextTickTime -= deltaTime;

        // Тиковый урон/лечение
        if (nextTickTime <= 0f && effectData.damageOrHealPerTick != 0f)
        {
            ApplyTick();
            nextTickTime = effectData.tickInterval;
        }

        // Эффект закончился
        if (remainingDuration <= 0f)
        {
            Remove();
            return true; // Удалить из списка
        }

        return false;
    }

    /// <summary>
    /// Применить тиковый урон/лечение
    /// </summary>
    private void ApplyTick()
    {
        if (target == null) return;

        // Урон
        if (effectData.damageOrHealPerTick < 0f)
        {
            HealthSystem healthSystem = target.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                healthSystem.TakeDamage(-effectData.damageOrHealPerTick);
            }
        }
        // Лечение
        else if (effectData.damageOrHealPerTick > 0f)
        {
            HealthSystem healthSystem = target.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                healthSystem.Heal(effectData.damageOrHealPerTick);
            }
        }
    }

    /// <summary>
    /// Применить эффект (при активации)
    /// </summary>
    private void ApplyEffect()
    {
        if (target == null) return;

        CharacterStats stats = target.GetComponent<CharacterStats>();
        if (stats == null) return;

        float bonusValue = (effectData.power / 100f); // power в процентах (25 = 25%)

        switch (effectData.effectType)
        {
            case EffectType.IncreaseAttack:
                float attackBonus = stats.physicalDamage * bonusValue;
                stats.ModifyPhysicalDamage(attackBonus);
                Debug.Log($"[ActiveEffect] ⚔️ Атака увеличена на {effectData.power}% (+{attackBonus:F0})");
                break;

            case EffectType.IncreaseDefense:
                float defenseBonus = stats.physicalDefense * bonusValue;
                stats.ModifyPhysicalDefense(defenseBonus);
                Debug.Log($"[ActiveEffect] 🛡️ Защита увеличена на {effectData.power}% (+{defenseBonus:F0})");
                break;

            case EffectType.IncreaseSpeed:
                float speedBonus = stats.movementSpeed * bonusValue;
                stats.ModifyMovementSpeed(speedBonus);
                Debug.Log($"[ActiveEffect] 🏃 Скорость увеличена на {effectData.power}% (+{speedBonus:F1})");
                break;

            case EffectType.IncreasePerception:
                int perceptionBonus = Mathf.RoundToInt(effectData.power); // power = прямое значение (не процент)
                stats.ModifyPerception(perceptionBonus);
                Debug.Log($"[ActiveEffect] 👁️ Восприятие увеличено на {perceptionBonus}");
                break;

            case EffectType.DecreaseAttack:
                float attackPenalty = -stats.physicalDamage * bonusValue;
                stats.ModifyPhysicalDamage(attackPenalty);
                Debug.Log($"[ActiveEffect] ⚔️ Атака уменьшена на {effectData.power}% ({attackPenalty:F0})");
                break;

            case EffectType.DecreaseDefense:
                float defensePenalty = -stats.physicalDefense * bonusValue;
                stats.ModifyPhysicalDefense(defensePenalty);
                Debug.Log($"[ActiveEffect] 🛡️ Защита уменьшена на {effectData.power}% ({defensePenalty:F0})");
                break;

            case EffectType.DecreaseSpeed:
                float speedPenalty = -stats.movementSpeed * bonusValue;
                stats.ModifyMovementSpeed(speedPenalty);
                Debug.Log($"[ActiveEffect] 🏃 Скорость уменьшена на {effectData.power}% ({speedPenalty:F1})");
                break;

            case EffectType.Shield:
                shieldAmount = effectData.power; // power = количество HP щита
                Debug.Log($"[ActiveEffect] 🛡️ Щит активирован: {shieldAmount:F0} HP");
                break;
        }
    }

    /// <summary>
    /// Удалить эффект
    /// </summary>
    public void Remove()
    {
        // Снимаем модификаторы статов
        RemoveEffect();

        if (particleInstance != null)
        {
            Object.Destroy(particleInstance);
        }

        isStunned = false;
        isRooted = false;
        isSleeping = false;
        isSilenced = false;
        isInvulnerable = false;
    }

    /// <summary>
    /// Снять эффект (при завершении)
    /// </summary>
    private void RemoveEffect()
    {
        if (target == null) return;

        CharacterStats stats = target.GetComponent<CharacterStats>();
        if (stats == null) return;

        float bonusValue = (effectData.power / 100f);

        switch (effectData.effectType)
        {
            case EffectType.IncreaseAttack:
                float attackBonus = stats.physicalDamage * bonusValue;
                stats.ModifyPhysicalDamage(-attackBonus); // Убираем бонус
                Debug.Log($"[ActiveEffect] ⚔️ Бонус атаки снят (-{attackBonus:F0})");
                break;

            case EffectType.IncreaseDefense:
                float defenseBonus = stats.physicalDefense * bonusValue;
                stats.ModifyPhysicalDefense(-defenseBonus);
                Debug.Log($"[ActiveEffect] 🛡️ Бонус защиты снят (-{defenseBonus:F0})");
                break;

            case EffectType.IncreaseSpeed:
                float speedBonus = stats.movementSpeed * bonusValue;
                stats.ModifyMovementSpeed(-speedBonus);
                Debug.Log($"[ActiveEffect] 🏃 Бонус скорости снят (-{speedBonus:F1})");
                break;

            case EffectType.IncreasePerception:
                int perceptionBonus = Mathf.RoundToInt(effectData.power);
                stats.ModifyPerception(-perceptionBonus); // Убираем бонус
                Debug.Log($"[ActiveEffect] 👁️ Бонус восприятия снят (-{perceptionBonus})");
                break;

            case EffectType.DecreaseAttack:
                float attackPenalty = -stats.physicalDamage * bonusValue;
                stats.ModifyPhysicalDamage(-attackPenalty); // Убираем пенальти
                Debug.Log($"[ActiveEffect] ⚔️ Пенальти атаки снят (+{-attackPenalty:F0})");
                break;

            case EffectType.DecreaseDefense:
                float defensePenalty = -stats.physicalDefense * bonusValue;
                stats.ModifyPhysicalDefense(-defensePenalty);
                Debug.Log($"[ActiveEffect] 🛡️ Пенальти защиты снят (+{-defensePenalty:F0})");
                break;

            case EffectType.DecreaseSpeed:
                float speedPenalty = -stats.movementSpeed * bonusValue;
                stats.ModifyMovementSpeed(-speedPenalty);
                Debug.Log($"[ActiveEffect] 🏃 Пенальти скорости снят (+{-speedPenalty:F1})");
                break;

            case EffectType.Shield:
                shieldAmount = 0f; // Щит удаляется
                Debug.Log($"[ActiveEffect] 🛡️ Щит снят");
                break;
        }
    }

    /// <summary>
    /// Поглотить урон щитом (вызывается из HealthSystem)
    /// Возвращает остаток урона после поглощения
    /// </summary>
    public float AbsorbDamage(float damage)
    {
        if (shieldAmount <= 0f) return damage;

        if (damage <= shieldAmount)
        {
            // Щит поглощает весь урон
            shieldAmount -= damage;
            Debug.Log($"[ActiveEffect] 🛡️ Щит поглотил {damage:F0} урона (осталось: {shieldAmount:F0})");
            return 0f;
        }
        else
        {
            // Щит частично поглощает урон
            float remainder = damage - shieldAmount;
            Debug.Log($"[ActiveEffect] 🛡️ Щит разрушен! Поглощено {shieldAmount:F0}, осталось урона: {remainder:F0}");
            shieldAmount = 0f;
            remainingDuration = 0f; // Щит разрушен - эффект завершается
            return remainder;
        }
    }

    /// <summary>
    /// Прервать сон при получении урона
    /// </summary>
    public void OnDamageTaken()
    {
        if (effectData.effectType == EffectType.Sleep)
        {
            remainingDuration = 0f; // Прерываем сон
            Debug.Log("[ActiveEffect] Сон прерван при получении урона");
        }
    }

    /// <summary>
    /// Получить строковое представление типа эффекта для сервера
    /// </summary>
    private string GetEffectTypeString(EffectType effectType)
    {
        switch (effectType)
        {
            case EffectType.Burn:
            case EffectType.Poison:
            case EffectType.Bleed:
                return "dot"; // Damage Over Time

            case EffectType.IncreaseAttack:
            case EffectType.IncreaseDefense:
            case EffectType.IncreaseSpeed:
            case EffectType.IncreaseHPRegen:
            case EffectType.IncreaseMPRegen:
            case EffectType.HealOverTime:
            case EffectType.Shield:
            case EffectType.Invulnerability:
                return "buff";

            case EffectType.DecreaseAttack:
            case EffectType.DecreaseDefense:
            case EffectType.DecreaseSpeed:
            case EffectType.Stun:
            case EffectType.Root:
            case EffectType.Silence:
            case EffectType.Sleep:
            case EffectType.Fear:
            case EffectType.Taunt:
                return "debuff";

            default:
                return "effect";
        }
    }
}
