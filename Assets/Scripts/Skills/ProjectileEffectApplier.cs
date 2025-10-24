using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Компонент для применения новых EffectConfig эффектов к старым Projectile снарядам
/// Добавляется динамически к снаряду для поддержки стана, root и других новых эффектов
/// </summary>
public class ProjectileEffectApplier : MonoBehaviour
{
    private List<EffectConfig> effects;
    private CharacterStats casterStats;
    private bool effectsApplied = false;

    /// <summary>
    /// Инициализация с эффектами из SkillConfig
    /// </summary>
    public void Initialize(List<EffectConfig> effectConfigs, CharacterStats stats)
    {
        effects = effectConfigs;
        casterStats = stats;

        Debug.Log($"[ProjectileEffectApplier] Initialized with {effects?.Count ?? 0} effects");
    }

    void Update()
    {
        // Проверяем, был ли снаряд уничтожен или попал в цель
        if (effectsApplied)
        {
            return;
        }

        // Получаем компонент Projectile
        Projectile projectile = GetComponent<Projectile>();
        if (projectile == null)
        {
            Debug.LogWarning("[ProjectileEffectApplier] No Projectile component found!");
            Destroy(this);
            return;
        }

        // Снаряд уничтожается при попадании, так что если мы всё ещё существуем - эффекты не применены
        // Мы подпишемся на коллизию через OnTriggerEnter на этом же объекте
    }

    /// <summary>
    /// Ловим коллизию раньше чем Projectile, чтобы применить эффекты
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (effectsApplied)
        {
            return;
        }

        // Проверяем что это враг
        NetworkPlayer networkTarget = other.GetComponent<NetworkPlayer>();
        Enemy enemy = other.GetComponent<Enemy>();
        DummyEnemy dummy = other.GetComponent<DummyEnemy>();

        if (networkTarget == null && enemy == null && dummy == null)
        {
            // Не враг - игнорируем
            return;
        }

        // Применяем эффекты через EffectManager
        ApplyEffectsToTarget(other.transform);
        effectsApplied = true;
    }

    /// <summary>
    /// Публичный метод для применения эффектов к цели (вызывается из Projectile.HitTarget)
    /// </summary>
    public void ApplyEffects(Transform targetTransform)
    {
        if (effectsApplied)
        {
            Debug.Log("[ProjectileEffectApplier] ⏭️ Effects already applied, skipping");
            return;
        }

        ApplyEffectsToTarget(targetTransform);
        effectsApplied = true;
    }

    /// <summary>
    /// Применить эффекты к цели (внутренний метод)
    /// </summary>
    private void ApplyEffectsToTarget(Transform targetTransform)
    {
        if (effects == null || effects.Count == 0)
        {
            Debug.Log("[ProjectileEffectApplier] No effects to apply");
            return;
        }

        // Получаем EffectManager цели
        EffectManager targetEffectManager = targetTransform.GetComponent<EffectManager>();
        if (targetEffectManager == null)
        {
            Debug.LogWarning($"[ProjectileEffectApplier] Target {targetTransform.name} has no EffectManager! Adding one...");
            targetEffectManager = targetTransform.gameObject.AddComponent<EffectManager>();
        }

        // Применяем все эффекты
        foreach (var effectConfig in effects)
        {
            targetEffectManager.ApplyEffect(effectConfig, casterStats);
            Debug.Log($"[ProjectileEffectApplier] ✨ Applied effect {effectConfig.effectType} to {targetTransform.name}");
        }

        Debug.Log($"[ProjectileEffectApplier] ✅ Applied {effects.Count} effects to {targetTransform.name}");
    }
}
