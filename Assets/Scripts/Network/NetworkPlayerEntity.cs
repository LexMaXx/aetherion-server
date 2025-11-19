using UnityEngine;
using System.Globalization;

/// <summary>
/// Компонент для NetworkPlayer (удаленные игроки в мультиплеере)
/// Делает их таргетабельными как враги (PvP) или союзники (PvE)
/// </summary>
[RequireComponent(typeof(NetworkPlayer))]
public class NetworkPlayerEntity : TargetableEntity
{
    private NetworkPlayer networkPlayer;
    private new HealthSystem healthSystem; // new keyword - скрываем поле базового класса

    [Header("PvP Settings")]
    [Tooltip("PvP режим: другие игроки = враги (можно атаковать)")]
    [SerializeField] private bool isPvPMode = true;

    void Awake()
    {
        // КРИТИЧЕСКИ ВАЖНО: Используем Awake() вместо Start()!
        // Awake() вызывается ДО Start() у всех компонентов
        // Это гарантирует что мы установим entityName и faction ДО вызова TargetableEntity.Start()

        // Получаем NetworkPlayer компонент
        networkPlayer = GetComponent<NetworkPlayer>();

        if (networkPlayer == null)
        {
            Debug.LogError("[NetworkPlayerEntity] ❌ NetworkPlayer компонент не найден в Awake()!");
            return;
        }

        // Получаем HealthSystem компонент
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            Debug.LogError("[NetworkPlayerEntity] ❌ HealthSystem компонент не найден в Awake()!");
            return;
        }

        // КРИТИЧЕСКИ ВАЖНО: Устанавливаем entityName, ownerId, faction ЗДЕСЬ в Awake()!
        // Это гарантирует что TargetableEntity.Start() увидит правильные значения
        entityName = networkPlayer.username;
        ownerId = networkPlayer.socketId;

        Debug.Log($"[NetworkPlayerEntity] 🔍 Awake(): username='{networkPlayer.username}', socketId='{networkPlayer.socketId}'");

        // Устанавливаем фракцию в зависимости от режима
        if (isPvPMode)
        {
            faction = Faction.OtherPlayer; // В PvP = враг
        }
        else
        {
            faction = Faction.Ally; // В PvE = союзник
        }

        Debug.Log($"[NetworkPlayerEntity] 🔍 Awake(): entityName='{entityName}', faction={faction}");
    }

    protected override void Start()
    {
        // Проверяем что компоненты получены в Awake()
        if (networkPlayer == null || healthSystem == null)
        {
            Debug.LogError("[NetworkPlayerEntity] ❌ Компоненты не инициализированы в Awake()!");
            return;
        }

        // Вызываем base.Start() который использует entityName и faction установленные в Awake()
        base.Start();

        // Синхронизируем HP из HealthSystem
        currentHealth = healthSystem.CurrentHealth;
        maxHealth = healthSystem.MaxHealth;

        Debug.Log($"[NetworkPlayerEntity] 🎯 {entityName} инициализирован (ID: {ownerId}, Faction: {faction}, HP: {currentHealth}/{maxHealth})");

        // Подписываемся на события HealthSystem
        SubscribeToHealthSystemEvents();

        Debug.Log($"[NetworkPlayerEntity] ✅ Полная инициализация завершена для {entityName}");
    }

    /// <summary>
    /// Подписаться на события HealthSystem для синхронизации HP с TargetableEntity
    /// </summary>
    private void SubscribeToHealthSystemEvents()
    {
        if (healthSystem != null)
        {
            // Подписываемся на событие изменения HP
            healthSystem.OnHealthChanged += OnHealthSystemChanged;
            Debug.Log($"[NetworkPlayerEntity] ✅ Подписан на HealthSystem.OnHealthChanged для {entityName}");
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

        // Вызываем событие OnHealthChanged для обновления UI (TargetPanel)
        InvokeHealthChanged(currentHealth, maxHealth);

        Debug.Log($"[NetworkPlayerEntity] 💚 {entityName} HP обновлено: {currentHealth:F0}/{maxHealth:F0}");

        // Проверяем смерть
        if (currentHealth <= 0f && isAlive)
        {
            isAlive = false;
            Debug.Log($"[NetworkPlayerEntity] 💀 {entityName} погиб!");
        }
    }

    void Update()
    {
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
    /// Переопределяем IsLocalPlayer - NetworkPlayerEntity НИКОГДА не локальный игрок
    /// </summary>
    protected override bool IsLocalPlayer()
    {
        return false; // Это ВСЕГДА удаленный игрок
    }

    /// <summary>
    /// Переопределяем TakeDamage - для NetworkPlayer урон обрабатывается на сервере
    /// </summary>
    public override void TakeDamage(float damage, TargetableEntity attacker = null)
    {
        // ВАЖНО: Для NetworkPlayer урон должен идти через сервер!
        Debug.Log($"[NetworkPlayerEntity] 🎯 {entityName} получил {damage} урона от {attacker?.GetEntityName() ?? "Unknown"}");

        // Получаем ID атакующего
        string attackerId = "";
        if (attacker != null)
        {
            attackerId = attacker.GetOwnerId();
            Debug.Log($"[NetworkPlayerEntity] 👊 Атакующий ID из TargetableEntity: {attackerId}");
        }

        // Если attackerId пустой (attacker = локальный игрок), получаем из NetworkSyncManager
        if (string.IsNullOrEmpty(attackerId) && NetworkSyncManager.Instance != null)
        {
            attackerId = NetworkSyncManager.Instance.LocalPlayerSocketId;
            Debug.Log($"[NetworkPlayerEntity] 👊 Атакующий ID из NetworkSyncManager (локальный игрок): {attackerId}");
        }

        // Отправляем урон на сервер
        if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
        {
            // КРИТИЧЕСКИ ВАЖНО: Используем InvariantCulture для точки вместо запятой!
            // JSON требует точку: 53.2 а не 53,2
            string damageStr = damage.ToString("F1", CultureInfo.InvariantCulture);

            // Формируем JSON вручную (т.к. Emit требует string)
            string damageData = $"{{\"targetSocketId\":\"{ownerId}\",\"damage\":{damageStr},\"attackerId\":\"{attackerId}\"}}";
            SocketIOManager.Instance.Emit("player_damage", damageData);
            Debug.Log($"[NetworkPlayerEntity] 📤 ОТПРАВЛЕН УРОН НА СЕРВЕР: {damageData}");
            Debug.Log($"[NetworkPlayerEntity] 🎯 Target: {entityName} (socketId: {ownerId})");
            Debug.Log($"[NetworkPlayerEntity] 👊 Attacker ID: {attackerId}");
            Debug.Log($"[NetworkPlayerEntity] 💥 Damage: {damage}");
        }
        else
        {
            Debug.LogError("[NetworkPlayerEntity] ❌ SocketIOManager не найден! Урон НЕ отправлен на сервер!");

            // Fallback: Локальный урон только в редакторе для тестирования
            if (Application.isEditor)
            {
                Debug.LogWarning("[NetworkPlayerEntity] ⚠️ FALLBACK: Применяем урон локально (только для теста в Editor)");
                base.TakeDamage(damage, attacker);
            }
        }
    }

    /// <summary>
    /// Переопределяем Heal - для NetworkPlayer хил обрабатывается на сервере
    /// </summary>
    public override void Heal(float amount, TargetableEntity healer = null)
    {
        // ВАЖНО: Для NetworkPlayer хил должен идти через сервер!

        Debug.Log($"[NetworkPlayerEntity] {entityName} восстановил {amount} HP (отправляем на сервер)");

        // TODO: Отправить хил на сервер
        // SocketIOManager.Instance.Emit("player_heal", new { targetId = ownerId, amount = amount });

        // Для теста можем локально обновить HP (НЕ для прода!)
        if (Application.isEditor)
        {
            base.Heal(amount, healer);
        }
    }

    /// <summary>
    /// Переключить режим PvP/PvE
    /// </summary>
    public void SetPvPMode(bool pvp)
    {
        isPvPMode = pvp;

        if (isPvPMode)
        {
            faction = Faction.OtherPlayer; // Враг
        }
        else
        {
            faction = Faction.Ally; // Союзник
        }

        Debug.Log($"[NetworkPlayerEntity] {entityName} фракция изменена на {faction}");
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
