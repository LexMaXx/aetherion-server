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

    public ActiveEffect(SkillEffect effect, Transform target)
    {
        this.effectData = effect;
        this.remainingDuration = effect.duration;
        this.nextTickTime = effect.tickInterval;
        this.target = target;

        // Создаём визуальный эффект если есть
        if (effect.particleEffectPrefab != null && target != null)
        {
            particleInstance = Object.Instantiate(effect.particleEffectPrefab, target.position, Quaternion.identity, target);
        }

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
    /// Удалить эффект
    /// </summary>
    public void Remove()
    {
        if (particleInstance != null)
        {
            Object.Destroy(particleInstance);
        }

        isStunned = false;
        isRooted = false;
        isSleeping = false;
        isSilenced = false;
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
}
