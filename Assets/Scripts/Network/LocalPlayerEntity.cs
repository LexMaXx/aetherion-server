using UnityEngine;
using System.Globalization;

/// <summary>
/// Компонент для локального игрока в мультиплеере
/// Отправляет урон на сервер (аналогично NetworkPlayerEntity)
/// </summary>
public class LocalPlayerEntity : TargetableEntity
{
    private new HealthSystem healthSystem; // new keyword - скрываем поле базового класса

    [Header("Multiplayer Settings")]
    [Tooltip("Socket ID локального игрока (устанавливается NetworkSyncManager)")]
    private string localPlayerSocketId;

    protected override void Start()
    {
        // Получаем HealthSystem компонент
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            Debug.LogError("[LocalPlayerEntity] ❌ HealthSystem компонент не найден!");
            return;
        }

        // Пытаемся получить socketId (может быть пустым если соединение ещё не установлено)
        TrySetSocketId();

        // Вызываем base.Start()
        base.Start();

        // Синхронизируем HP из HealthSystem
        currentHealth = healthSystem.CurrentHealth;
        maxHealth = healthSystem.MaxHealth;

        Debug.Log($"[LocalPlayerEntity] 🎯 {entityName} инициализирован (ID: {localPlayerSocketId}, HP: {currentHealth}/{maxHealth})");

        // Подписываемся на события HealthSystem
        SubscribeToHealthSystemEvents();
    }

    /// <summary>
    /// Подписаться на события HealthSystem для синхронизации HP с TargetableEntity
    /// </summary>
    private void SubscribeToHealthSystemEvents()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged += OnHealthSystemChanged;
            Debug.Log($"[LocalPlayerEntity] ✅ Подписан на HealthSystem.OnHealthChanged");
        }
    }

    /// <summary>
    /// Обработчик события изменения HP из HealthSystem
    /// </summary>
    private void OnHealthSystemChanged(float newHealth, float newMaxHealth)
    {
        // Синхронизируем HP с TargetableEntity
        currentHealth = newHealth;
        maxHealth = newMaxHealth;

        // Вызываем событие OnHealthChanged для обновления UI
        InvokeHealthChanged(currentHealth, maxHealth);

        Debug.Log($"[LocalPlayerEntity] 💚 {entityName} HP обновлено: {currentHealth:F0}/{maxHealth:F0}");

        // Проверяем смерть
        if (currentHealth <= 0f && isAlive)
        {
            isAlive = false;
            Debug.Log($"[LocalPlayerEntity] 💀 {entityName} погиб!");
        }
    }

    void Update()
    {
        // Пытаемся получить socketId если он ещё не установлен
        if (string.IsNullOrEmpty(localPlayerSocketId))
        {
            TrySetSocketId();
        }

        // Постоянно синхронизируем HP из HealthSystem (на случай если событие не сработало)
        if (healthSystem != null)
        {
            if (currentHealth != healthSystem.CurrentHealth || maxHealth != healthSystem.MaxHealth)
            {
                currentHealth = healthSystem.CurrentHealth;
                maxHealth = healthSystem.MaxHealth;
                InvokeHealthChanged(currentHealth, maxHealth);
            }
        }
    }

    /// <summary>
    /// Попытаться получить socketId из NetworkSyncManager
    /// </summary>
    private void TrySetSocketId()
    {
        if (NetworkSyncManager.Instance != null)
        {
            string newSocketId = NetworkSyncManager.Instance.LocalPlayerSocketId;

            if (!string.IsNullOrEmpty(newSocketId))
            {
                localPlayerSocketId = newSocketId;
                SetOwnerId(localPlayerSocketId);
                Debug.Log($"[LocalPlayerEntity] ✅ Socket ID установлен: {localPlayerSocketId}");
            }
            else
            {
                // Socket ID ещё не готов (обычно при первом запуске до получения 'ready' события)
                // Это нормально - будет установлен позже в Update или при первом TakeDamage
            }
        }
        else
        {
            Debug.LogWarning("[LocalPlayerEntity] ⚠️ NetworkSyncManager не найден");
        }
    }

    /// <summary>
    /// Переопределяем IsLocalPlayer - LocalPlayerEntity ВСЕГДА локальный игрок
    /// </summary>
    protected override bool IsLocalPlayer()
    {
        return true; // Это ВСЕГДА локальный игрок
    }

    /// <summary>
    /// Переопределяем TakeDamage - для локального игрока урон отправляется на сервер!
    /// </summary>
    public override void TakeDamage(float damage, TargetableEntity attacker = null)
    {
        // ВАЖНО: Для локального игрока урон должен идти через сервер в multiplayer!
        Debug.Log($"[LocalPlayerEntity] 🎯 {entityName} получил {damage} урона от {attacker?.GetEntityName() ?? "Unknown"}");

        // Получаем ID атакующего
        string attackerId = "";
        if (attacker != null)
        {
            attackerId = attacker.GetOwnerId();
            Debug.Log($"[LocalPlayerEntity] 👊 Атакующий ID: {attackerId}");
        }

        // Отправляем урон на сервер (если multiplayer)
        bool isMultiplayer = SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected;

        if (isMultiplayer)
        {
            // Попытка получить socketId если он пустой
            if (string.IsNullOrEmpty(localPlayerSocketId))
            {
                TrySetSocketId();
            }

            if (!string.IsNullOrEmpty(localPlayerSocketId))
            {
                // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ для Life Steal:
                // Применяем урон ЛОКАЛЬНО для мгновенного feedback,
                // затем отправляем на сервер для синхронизации
                base.TakeDamage(damage, attacker);
                Debug.Log($"[LocalPlayerEntity] 💥 Урон применён локально: {damage} (для Life Steal и feedback)");

                // КРИТИЧЕСКИ ВАЖНО: Используем InvariantCulture для точки вместо запятой!
                // JSON требует точку: 53.2 а не 53,2
                string damageStr = damage.ToString("F1", CultureInfo.InvariantCulture);

                // Формируем JSON вручную
                string damageData = $"{{\"targetSocketId\":\"{localPlayerSocketId}\",\"damage\":{damageStr},\"attackerId\":\"{attackerId}\"}}";
                SocketIOManager.Instance.Emit("player_damage", damageData);
                Debug.Log($"[LocalPlayerEntity] 📤 Отправлен урон на сервер для синхронизации: {damageData}");

                // Сервер может отправить player_damaged для корректировки HP
                // Это позволит:
                // 1. Life Steal работать мгновенно
                // 2. Сервер корректировать HP если нужно (anti-cheat)
            }
            else
            {
                // socketId всё ещё не доступен (соединение не установлено)
                // Применяем урон локально и НЕ логируем ошибку (это нормально в начале)
                base.TakeDamage(damage, attacker);
                Debug.Log($"[LocalPlayerEntity] 📶 Соединение не установлено, урон применён локально");
            }
        }
        else
        {
            // Singleplayer режим - урон локально
            Debug.Log("[LocalPlayerEntity] 🎮 Singleplayer режим - урон локально");
            base.TakeDamage(damage, attacker);
        }
    }

    /// <summary>
    /// Переопределяем Heal - для локального игрока хил обрабатывается на сервере
    /// </summary>
    public override void Heal(float amount, TargetableEntity healer = null)
    {
        Debug.Log($"[LocalPlayerEntity] {entityName} восстановил {amount} HP");

        // TODO: Отправить хил на сервер в multiplayer режиме
        bool isMultiplayer = SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected;

        // if (isMultiplayer)
        // {
        //     SocketIOManager.Instance.Emit("player_heal", ...);
        // }

        // Для теста можем локально обновить HP
        if (Application.isEditor || !isMultiplayer)
        {
            base.Heal(amount, healer);
        }
    }

    new void OnDestroy() // new keyword - скрываем метод базового класса
    {
        // Вызываем базовый OnDestroy
        base.OnDestroy();

        // Отписываемся от событий
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= OnHealthSystemChanged;
        }
    }
}
