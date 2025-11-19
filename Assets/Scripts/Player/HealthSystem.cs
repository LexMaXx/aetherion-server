using UnityEngine;
using System;
using System.Linq;

/// <summary>
/// Система здоровья персонажа
/// Интегрируется с CharacterStats (Endurance → MaxHP)
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float maxHealth = 1000f;

    [Header("Health Regeneration")]
    [SerializeField] private float healthRegenRate = 0.2f; // HP/сек когда стоит на месте (ЗАМЕДЛЕНО в 10 раз)

    [Header("Damage Reduction")]
    [SerializeField] private float damageReduction = 0f; // Снижение урона в процентах (0-100)

    // Интеграция с CharacterStats
    private CharacterStats characterStats;

    // Регенерация
    private bool isRegenerating = false;

    // События
    public event Action<float, float> OnHealthChanged; // (current, max)
    public event Action OnDeath;

    // Геттеры
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercent => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsAlive => currentHealth > 0;

    void Start()
    {
        // Интеграция с CharacterStats (Endurance → HP)
        characterStats = GetComponent<CharacterStats>();
        if (characterStats != null)
        {
            characterStats.OnStatsChanged += UpdateHealthFromStats;
            UpdateHealthFromStats();
            Debug.Log("[HealthSystem] ✅ Интеграция с CharacterStats активирована");
        }
        else
        {
            // Если нет CharacterStats - используем дефолтные значения
            currentHealth = maxHealth;
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Update()
    {
        // Регенерация HP только когда стоит на месте
        bool isStanding = IsPlayerStanding();

        if (isStanding && currentHealth < maxHealth && IsAlive)
        {
            if (!isRegenerating)
            {
                isRegenerating = true;
                Debug.Log("[HealthSystem] 🔄 Начало восстановления HP (персонаж стоит)");
            }

            currentHealth += healthRegenRate * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth, maxHealth);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Полностью восстановлено
            if (currentHealth >= maxHealth)
            {
                currentHealth = maxHealth;
                if (isRegenerating)
                {
                    isRegenerating = false;
                    Debug.Log("[HealthSystem] ✅ HP полностью восстановлено!");
                }
            }
        }
        else if (!isStanding && isRegenerating)
        {
            // Остановили регенерацию при движении
            isRegenerating = false;
            Debug.Log("[HealthSystem] ⏸️ Остановка восстановления HP (персонаж движется)");
        }
    }

    /// <summary>
    /// Проверка, стоит ли игрок на месте
    /// </summary>
    private bool IsPlayerStanding()
    {
        // Проверяем ввод движения - самый надёжный метод
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool hasMovementInput = Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f;

        // Если есть ввод - персонаж движется
        return !hasMovementInput;
    }

    /// <summary>
    /// Обновить HP на основе Endurance
    /// </summary>
    private void UpdateHealthFromStats()
    {
        if (characterStats == null) return;

        float oldMaxHealth = maxHealth;
        maxHealth = characterStats.MaxHealth;

        // Восстанавливаем HP пропорционально, но при первой инициализации даём полное HP
        if (oldMaxHealth > 0 && currentHealth > 0)
        {
            // Пропорциональное восстановление при изменении maxHealth
            float healthPercent = currentHealth / oldMaxHealth;
            currentHealth = maxHealth * healthPercent;
        }
        else
        {
            // Первая инициализация - даём полное HP
            currentHealth = maxHealth;
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"[HealthSystem] Обновлено из Stats: {currentHealth:F0}/{maxHealth:F0} HP (Endurance: {characterStats.endurance})");
    }

    /// <summary>
    /// Получить урон
    /// </summary>
    public void TakeDamage(float damage)
{
    if (!IsAlive) return;

    // Проверяем неуязвимость (Divine Protection и другие эффекты)
    EffectManager effectManager = GetComponent<EffectManager>();
    if (effectManager != null && effectManager.HasInvulnerability())
    {
        Debug.Log($"[HealthSystem] 🛡️ НЕУЯЗВИМОСТЬ! Урон {damage:F0} заблокирован (Divine Protection или аналог)");
        Debug.Log($"[HealthSystem] 🛡️ Активные эффекты: {string.Join(", ", effectManager.GetAllEffects().Select(e => e.config.effectType.ToString()))}");
        return;
    }


    // Применяем снижение урона
    float originalDamage = damage;
        if (damageReduction > 0)
        {
            float reduction = damage * (damageReduction / 100f);
            damage -= reduction;
            Debug.Log($"[HealthSystem] 🛡️ Снижение урона: {originalDamage:F1} → {damage:F1} (-{reduction:F1}, {damageReduction}%)");
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"[HealthSystem] -{damage:F0} HP. Осталось: {currentHealth:F0}/{maxHealth:F0}");

        // ═══════════════════════════════════════════════════════════
        // КРИТИЧЕСКИ ВАЖНО: Показываем всплывающие цифры урона!
        // ═══════════════════════════════════════════════════════════
        if (DamageNumberManager.Instance != null)
        {
            Vector3 damagePosition = transform.position + Vector3.up * 2f; // Над головой персонажа
            DamageNumberManager.Instance.ShowDamage(damagePosition, damage, false, false);
            Debug.Log($"[HealthSystem] 💥 Всплывающая цифра урона показана: {damage:F0}");
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Восстановить здоровье
    /// </summary>
    public void Heal(float amount)
    {
        if (!IsAlive) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        Debug.Log($"[HealthSystem] +{amount:F0} HP. Текущее: {currentHealth:F0}/{maxHealth:F0}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Принудительно установить текущее HP (для синхронизации и респавна)
    /// </summary>
    public void SetHealth(float health)
    {
        bool wasAlive = IsAlive;
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
        Debug.Log($"[HealthSystem] 🔧 HP принудительно установлено: {currentHealth:F0}/{maxHealth:F0}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // КРИТИЧЕСКИ ВАЖНО: Проверяем смерть после установки HP от сервера
        if (wasAlive && currentHealth <= 0)
        {
            Debug.Log("[HealthSystem] ☠️ Смерть обнаружена после SetHealth от сервера!");
            Die();
        }
    }

    /// <summary>
    /// Принудительно установить максимальное HP (для синхронизации)
    /// </summary>
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = Mathf.Max(0f, newMaxHealth);
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Не превышать новый maxHealth
        Debug.Log($"[HealthSystem] 🔧 MaxHealth установлено: {maxHealth:F0}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Смерть персонажа
    /// </summary>
    private void Die()
    {
        Debug.Log("[HealthSystem] ☠️ Персонаж погиб!");
        OnDeath?.Invoke();

        // ВАЖНО: PlayerDeathHandler или ArenaManager обработают респавн
    }

    /// <summary>
    /// Воскресить персонажа
    /// </summary>
    public void Revive(float healthPercent = 1f)
    {
        currentHealth = maxHealth * healthPercent;
        Debug.Log($"[HealthSystem] ⚕️ Персонаж воскрешен с {currentHealth:F0}/{maxHealth:F0} HP");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Временно увеличить максимальное здоровье (для трансформаций/баффов)
    /// </summary>
    public void AddTemporaryMaxHealth(float amount)
    {
        maxHealth += amount;
        currentHealth += amount; // Также увеличиваем текущее HP
        Debug.Log($"[HealthSystem] +{amount:F0} Временное Max HP. Текущее: {currentHealth:F0}/{maxHealth:F0}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Убрать временное максимальное здоровье (окончание баффа)
    /// </summary>
    public void RemoveTemporaryMaxHealth(float amount)
    {
        maxHealth -= amount;
        maxHealth = Mathf.Max(maxHealth, 1f); // Минимум 1 HP
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Текущее HP не может превышать максимум
        Debug.Log($"[HealthSystem] -{amount:F0} Временное Max HP. Текущее: {currentHealth:F0}/{maxHealth:F0}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Добавить снижение урона (для защитных баффов)
    /// </summary>
    public void AddDamageReduction(float percent)
    {
        damageReduction += percent;
        damageReduction = Mathf.Clamp(damageReduction, 0f, 100f); // Максимум 100%
        Debug.Log($"[HealthSystem] 🛡️ Снижение урона: {(percent > 0 ? "+" : "")}{percent}% (итого: {damageReduction}%)");
    }

    /// <summary>
    /// Убрать снижение урона (окончание защитного баффа)
    /// </summary>
    public void RemoveDamageReduction(float percent)
    {
        damageReduction -= percent;
        damageReduction = Mathf.Max(damageReduction, 0f); // Минимум 0%
        Debug.Log($"[HealthSystem] 🛡️ Снижение урона: -{percent}% (итого: {damageReduction}%)");
    }

    private void OnDestroy()
    {
        if (characterStats != null)
        {
            characterStats.OnStatsChanged -= UpdateHealthFromStats;
        }
    }

    // Для тестирования в редакторе
    [ContextMenu("Test: Take 20 Damage")]
    private void TestTakeDamage()
    {
        TakeDamage(20f);
    }

    [ContextMenu("Test: Heal 30 HP")]
    private void TestHeal()
    {
        Heal(30f);
    }
}
