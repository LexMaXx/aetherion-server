using UnityEngine;

/// <summary>
/// Базовый класс для всех существ, которых можно таргетить
/// Заменяет старую систему Enemy, поддерживает фракции
/// </summary>
public class TargetableEntity : MonoBehaviour
{
    [Header("Entity Info")]
    [SerializeField] protected string entityName = "Unknown";
    [SerializeField] protected Faction faction = Faction.Enemy;

    [Header("Health")]
    [SerializeField] protected float maxHealth = 1000f;  // Fallback если нет HealthSystem
    protected float currentHealth;
    protected bool isAlive = true;

    // Ссылка на HealthSystem (если есть, используется вместо собственных полей)
    protected HealthSystem healthSystem;

    [Header("Targetable Settings")]
    [Tooltip("Можно ли выбрать эту сущность как цель?")]
    [SerializeField] protected bool isTargetable = true;

    [Tooltip("ID игрока (для мультиплеера, чтобы не атаковать самого себя)")]
    protected string ownerId = ""; // Пустая строка = NPC/моб

    // Последний атакующий (нужен для опыта/агро)
    protected TargetableEntity lastAttacker;
    protected string lastAttackerOwnerId = "";

    // События
    public delegate void DeathHandler(TargetableEntity deadEntity);
    public event DeathHandler OnDeath;

    public delegate void HealthChangedHandler(float currentHP, float maxHP);
    public event HealthChangedHandler OnHealthChanged;

    protected virtual void Start()
    {
        if (string.IsNullOrEmpty(entityName) || entityName == "Unknown")
        {
            entityName = gameObject.name.Replace("(Clone)", "").Trim();

            LevelingSystem levelSystem = GetComponentInParent<LevelingSystem>();
            if (levelSystem == null)
            {
                levelSystem = GetComponent<LevelingSystem>();
            }

            if (levelSystem != null)
            {
                entityName = $"{entityName} Lv{levelSystem.CurrentLevel}";
            }
        }
        // Проверяем есть ли HealthSystem
        healthSystem = GetComponentInParent<HealthSystem>();

        if (healthSystem != null)
        {
            // Используем HP из HealthSystem
            maxHealth = healthSystem.MaxHealth;
            currentHealth = healthSystem.CurrentHealth;
            isAlive = true;

            Debug.Log($"[TargetableEntity] {entityName} использует HealthSystem: {currentHealth:F0}/{maxHealth:F0}");

            // Подписываемся на события HealthSystem
            healthSystem.OnHealthChanged += OnHealthSystemChanged;
            healthSystem.OnDeath += OnHealthSystemDeath;
        }
        else
        {
            // Fallback - используем собственные поля
            currentHealth = maxHealth;
            isAlive = true;

            Debug.Log($"[TargetableEntity] {entityName} использует собственную HP систему: {currentHealth:F0}/{maxHealth:F0}");
        }

        // Если это локальный игрок, устанавливаем фракцию Player
        if (IsLocalPlayer())
        {
            faction = Faction.Player;
            ownerId = GetLocalPlayerId();
            Debug.Log($"[TargetableEntity] {entityName} = локальный игрок (Faction.Player)");
        }
    }

    protected virtual void OnDestroy()
    {
        // Отписываемся от событий HealthSystem
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= OnHealthSystemChanged;
            healthSystem.OnDeath -= OnHealthSystemDeath;
        }
    }

    /// <summary>
    /// Обработчик изменения HP в HealthSystem
    /// </summary>
    private void OnHealthSystemChanged(float newHealth, float newMaxHealth)
    {
        currentHealth = newHealth;
        maxHealth = newMaxHealth;

        // Вызываем свое событие OnHealthChanged для UI и т.д.
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Обработчик смерти из HealthSystem
    /// </summary>
    private void OnHealthSystemDeath()
    {
        if (!isAlive) return;

        isAlive = false;
        Debug.Log($"[TargetableEntity] 💀 {entityName} погиб (через HealthSystem)!");

        // Вызываем свое событие OnDeath
        OnDeath?.Invoke(this);
    }

    /// <summary>
    /// Получить урон
    /// </summary>
    public virtual void TakeDamage(float damage, TargetableEntity attacker = null)
    {
        if (!isAlive) return;

        RecordAttacker(attacker);

        if (healthSystem != null)
        {
            // Используем HealthSystem
            healthSystem.TakeDamage(damage);
            // HealthSystem автоматически вызовет OnHealthSystemChanged и OnHealthSystemDeath
            Debug.Log($"[TargetableEntity] {entityName} получил {damage:F1} урона через HealthSystem");
        }
        else
        {
            // Fallback - собственная HP система
            currentHealth -= damage;
            currentHealth = Mathf.Max(0f, currentHealth);

            Debug.Log($"[TargetableEntity] {entityName} получил {damage:F1} урона. HP: {currentHealth:F0}/{maxHealth:F0}");

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }
    }

    /// <summary>
    /// Восстановить здоровье
    /// </summary>
    public virtual void Heal(float amount, TargetableEntity healer = null)
    {
        if (!isAlive) return;

        if (healthSystem != null)
        {
            // Используем HealthSystem
            healthSystem.Heal(amount);
            Debug.Log($"[TargetableEntity] {entityName} восстановил {amount:F1} HP через HealthSystem");
        }
        else
        {
            // Fallback - собственная HP система
            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);

            Debug.Log($"[TargetableEntity] {entityName} восстановил {amount:F1} HP. HP: {currentHealth:F0}/{maxHealth:F0}");

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    /// <summary>
    /// Protected метод для вызова события изменения здоровья из дочерних классов
    /// </summary>
    protected void InvokeHealthChanged(float current, float max)
    {
        OnHealthChanged?.Invoke(current, max);
    }

    /// <summary>
    /// Смерть существа
    /// </summary>
    protected virtual void Die()
    {
        if (!isAlive) return;

        isAlive = false;
        Debug.Log($"[TargetableEntity] 💀 {entityName} погиб!");

        OnDeath?.Invoke(this);
    }

    /// <summary>
    /// Запомнить последнего атакующего
    /// </summary>
    protected void RecordAttacker(TargetableEntity attacker)
    {
        if (attacker == null || attacker == this)
            return;

        lastAttacker = attacker;
        lastAttackerOwnerId = attacker.GetOwnerId();
    }

    /// <summary>
    /// Воскресить существо (для респавна)
    /// </summary>
    public virtual void Revive(float healthPercent = 1f)
    {
        // КРИТИЧЕСКИ ВАЖНО: НЕ проверяем isAlive!
        // Revive() должен ВСЕГДА восстанавливать HP, даже если isAlive=true
        // Это исправляет баг с рассинхронизацией HP после респавна

        isAlive = true;
        lastAttacker = null;
        lastAttackerOwnerId = "";

        if (healthSystem != null)
        {
            // Используем HealthSystem
            float targetHealth = healthSystem.MaxHealth * healthPercent;
            healthSystem.SetHealth(targetHealth);

            // Обновляем локальные поля
            maxHealth = healthSystem.MaxHealth;
            currentHealth = healthSystem.CurrentHealth;

            Debug.Log($"[TargetableEntity] ⚕️ {entityName} воскрешен через HealthSystem! HP: {currentHealth:F0}/{maxHealth:F0}");
        }
        else
        {
            // Fallback - собственная HP система
            currentHealth = maxHealth * healthPercent;

            Debug.Log($"[TargetableEntity] ⚕️ {entityName} воскрешен! HP: {currentHealth:F0}/{maxHealth:F0}");

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    /// <summary>
    /// Принудительно установить HP (для синхронизации)
    /// </summary>
    public virtual void SetHealth(float health, float max)
    {
        if (healthSystem != null)
        {
            // Используем HealthSystem
            healthSystem.SetMaxHealth(max);
            healthSystem.SetHealth(health);

            // Обновляем локальные поля
            maxHealth = healthSystem.MaxHealth;
            currentHealth = healthSystem.CurrentHealth;

            Debug.Log($"[TargetableEntity] 🔧 {entityName} HP установлено через HealthSystem: {currentHealth:F0}/{maxHealth:F0}");
        }
        else
        {
            // Fallback - собственная HP система
            currentHealth = health;
            maxHealth = max;

            Debug.Log($"[TargetableEntity] 🔧 {entityName} HP принудительно установлено: {currentHealth:F0}/{maxHealth:F0}");

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    /// <summary>
    /// Проверить является ли это локальным игроком
    /// </summary>
    protected virtual bool IsLocalPlayer()
    {
        // Проверяем есть ли PlayerController на этом объекте или родителе
        PlayerController pc = GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            // Дополнительно проверяем что это НЕ NetworkPlayer (удаленный игрок)
            NetworkPlayer np = GetComponentInParent<NetworkPlayer>();
            return np == null; // Если нет NetworkPlayer = локальный игрок
        }

        return false;
    }

    /// <summary>
    /// Получить ID локального игрока
    /// </summary>
    protected virtual string GetLocalPlayerId()
    {
        // Используем UserToken как уникальный ID игрока
        return PlayerPrefs.GetString("UserToken", "local_player");
    }

    // ═══════════════════════════════════════════════════════════
    // ПУБЛИЧНЫЕ ГЕТТЕРЫ/СЕТТЕРЫ
    // ═══════════════════════════════════════════════════════════

    public string GetEntityName() => entityName;
    public void SetEntityName(string name) => entityName = name;

    public Faction GetFaction() => faction;
    public void SetFaction(Faction newFaction) => faction = newFaction;

    public float GetCurrentHealth()
    {
        // Если есть HealthSystem - используем его
        if (healthSystem != null)
        {
            return healthSystem.CurrentHealth;
        }
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        // Если есть HealthSystem - используем его
        if (healthSystem != null)
        {
            return healthSystem.MaxHealth;
        }
        return maxHealth;
    }
    public bool IsEntityAlive() => isAlive;
    public bool IsTargetable() => isTargetable && isAlive;

    public string GetOwnerId() => ownerId;
    public void SetOwnerId(string id) => ownerId = id;
    public TargetableEntity GetLastAttacker() => lastAttacker;
    public string GetLastAttackerOwnerId() => lastAttackerOwnerId;

    /// <summary>
    /// Проверить можно ли атаковать эту цель с точки зрения атакующего
    /// </summary>
    public bool CanBeAttackedBy(TargetableEntity attacker)
    {
        if (!IsTargetable()) return false;

        // ВАЖНО: Нельзя атаковать самого себя!
        if (attacker != null && attacker.GetOwnerId() == this.GetOwnerId() && !string.IsNullOrEmpty(this.GetOwnerId()))
        {
            return false;
        }

        // Проверяем фракции
        if (attacker != null)
        {
            return FactionHelper.CanAttack(attacker.GetFaction(), this.GetFaction());
        }

        return true;
    }

    /// <summary>
    /// Проверить можно ли хилить/баффать эту цель с точки зрения кастера
    /// </summary>
    public bool CanBeSupportedBy(TargetableEntity caster)
    {
        if (!IsTargetable()) return false;

        // Проверяем фракции
        if (caster != null)
        {
            return FactionHelper.CanSupport(caster.GetFaction(), this.GetFaction());
        }

        return false;
    }

    /// <summary>
    /// Получить цвет фракции (для UI)
    /// </summary>
    public Color GetFactionColor()
    {
        return FactionHelper.GetFactionColor(faction);
    }

    // ═══════════════════════════════════════════════════════════
    // ОБРАТНАЯ СОВМЕСТИМОСТЬ С ENEMY.CS
    // ═══════════════════════════════════════════════════════════

    // Методы для обратной совместимости со старым кодом
    public string GetEnemyName() => GetEntityName();
    public bool IsAlive() => IsEntityAlive();

    /// <summary>
    /// Получить процент здоровья (0.0 - 1.0) для UI
    /// </summary>
    public float GetHealthPercent()
    {
        if (maxHealth <= 0f) return 0f;
        return currentHealth / maxHealth;
    }
}
