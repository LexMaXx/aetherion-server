using UnityEngine;

/// <summary>
/// Автоматическая синхронизация HP/MP с PartyManager
/// Прикрепляется к локальному игроку
/// Отслеживает изменения HealthSystem и ManaSystem и отправляет обновления членам группы
/// </summary>
public class PartyStatsSync : MonoBehaviour
{
    [Header("Components")]
    private HealthSystem healthSystem;
    private ManaSystem manaSystem;

    [Header("Sync Settings")]
    [SerializeField] private float syncInterval = 0.5f; // Синхронизировать каждые 0.5 секунд
    private float syncTimer = 0f;

    // Кэш предыдущих значений для проверки изменений
    private float lastHealth = -1f;
    private float lastMana = -1f;
    private float lastMaxHealth = -1f;
    private float lastMaxMana = -1f;

    void Start()
    {
        // Находим компоненты
        healthSystem = GetComponentInChildren<HealthSystem>();
        manaSystem = GetComponentInChildren<ManaSystem>();

        if (healthSystem == null)
        {
            Debug.LogWarning("[PartyStatsSync] ⚠️ HealthSystem не найден!");
        }

        if (manaSystem == null)
        {
            Debug.LogWarning("[PartyStatsSync] ⚠️ ManaSystem не найден!");
        }

        // Подписываемся на события изменения HP/MP для мгновенной синхронизации
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged += OnHealthChanged;
        }

        if (manaSystem != null)
        {
            manaSystem.OnManaChanged += OnManaChanged;
        }

        Debug.Log("[PartyStatsSync] ✅ Инициализирован");
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= OnHealthChanged;
        }

        if (manaSystem != null)
        {
            manaSystem.OnManaChanged -= OnManaChanged;
        }
    }

    void Update()
    {
        // Периодическая синхронизация (на случай если события не сработали)
        syncTimer += Time.deltaTime;
        if (syncTimer >= syncInterval)
        {
            syncTimer = 0f;
            SyncStatsIfChanged();
        }
    }

    /// <summary>
    /// Событие изменения HP (мгновенная синхронизация)
    /// </summary>
    private void OnHealthChanged(float currentHealth, float maxHealth)
    {
        // Синхронизируем сразу при изменении HP
        SyncStats();
    }

    /// <summary>
    /// Событие изменения MP (мгновенная синхронизация)
    /// </summary>
    private void OnManaChanged(float currentMana, float maxMana)
    {
        // Синхронизируем сразу при изменении MP
        SyncStats();
    }

    /// <summary>
    /// Синхронизировать статы только если они изменились
    /// </summary>
    private void SyncStatsIfChanged()
    {
        if (PartyManager.Instance == null || !PartyManager.Instance.IsInParty)
        {
            return;
        }

        if (healthSystem == null || manaSystem == null)
        {
            return;
        }

        float currentHealth = healthSystem.CurrentHealth;
        float maxHealth = healthSystem.MaxHealth;
        float currentMana = manaSystem.CurrentMana;
        float maxMana = manaSystem.MaxMana;

        // Проверяем изменения
        bool changed = false;
        if (Mathf.Abs(currentHealth - lastHealth) > 0.1f) changed = true;
        if (Mathf.Abs(maxHealth - lastMaxHealth) > 0.1f) changed = true;
        if (Mathf.Abs(currentMana - lastMana) > 0.1f) changed = true;
        if (Mathf.Abs(maxMana - lastMaxMana) > 0.1f) changed = true;

        if (changed)
        {
            SyncStats();
        }
    }

    /// <summary>
    /// Синхронизировать статы с группой
    /// </summary>
    private void SyncStats()
    {
        if (PartyManager.Instance == null || !PartyManager.Instance.IsInParty)
        {
            return;
        }

        if (healthSystem == null || manaSystem == null)
        {
            return;
        }

        float currentHealth = healthSystem.CurrentHealth;
        float maxHealth = healthSystem.MaxHealth;
        float currentMana = manaSystem.CurrentMana;
        float maxMana = manaSystem.MaxMana;

        // Обновляем кэш
        lastHealth = currentHealth;
        lastMaxHealth = maxHealth;
        lastMana = currentMana;
        lastMaxMana = maxMana;

        // Отправляем обновление через PartyManager
        PartyManager.Instance.UpdateMyStats(currentHealth, currentMana, maxHealth, maxMana);

        Debug.Log($"[PartyStatsSync] 📊 Синхронизированы статы: HP {currentHealth}/{maxHealth}, MP {currentMana}/{maxMana}");
    }

    /// <summary>
    /// Принудительная синхронизация (вызывается извне при необходимости)
    /// </summary>
    public void ForceSyncStats()
    {
        SyncStats();
    }
}
