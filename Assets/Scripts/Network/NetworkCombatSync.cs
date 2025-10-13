using UnityEngine;

/// <summary>
/// Синхронизирует боевые действия локального игрока с сервером
/// Перехватывает события атаки и отправляет их через WebSocket
/// </summary>
[RequireComponent(typeof(PlayerAttack))]
public class NetworkCombatSync : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool enableSync = true;

    private PlayerAttack playerAttack;
    private HealthSystem healthSystem;
    private ManaSystem manaSystem;
    private bool isMultiplayer = false;

    // Cooldown для отправки HP/MP (не отправляем каждый кадр)
    private float lastHealthSync = 0f;
    private float healthSyncInterval = 0.5f; // Раз в 0.5 секунды

    void Start()
    {
        // Проверяем мультиплеер режим
        string roomId = PlayerPrefs.GetString("CurrentRoomId", "");
        isMultiplayer = !string.IsNullOrEmpty(roomId);

        if (!isMultiplayer)
        {
            Debug.Log("[NetworkCombatSync] Одиночная игра, синхронизация отключена");
            enabled = false;
            return;
        }

        playerAttack = GetComponent<PlayerAttack>();
        healthSystem = GetComponent<HealthSystem>();
        manaSystem = GetComponent<ManaSystem>();

        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged += OnHealthChanged;
            healthSystem.OnDeath += OnPlayerDeath;
        }

        if (manaSystem != null)
        {
            manaSystem.OnManaChanged += OnManaChanged;
        }

        Debug.Log("[NetworkCombatSync] ✅ Боевая синхронизация активирована");
    }

    void Update()
    {
        if (!enableSync || !isMultiplayer) return;

        // Периодически синхронизируем HP/MP
        if (Time.time - lastHealthSync > healthSyncInterval)
        {
            SyncHealth();
            lastHealthSync = Time.time;
        }
    }

    /// <summary>
    /// Отправить атаку на сервер
    /// ВАЖНО: Этот метод должен вызываться из PlayerAttack после нанесения урона
    /// </summary>
    public void SendAttack(GameObject target, float damage, string attackType)
    {
        if (!enableSync || !isMultiplayer || SocketIOManager.Instance == null)
        {
            return;
        }

        if (!SocketIOManager.Instance.IsConnected)
        {
            return;
        }

        if (target == null)
        {
            Debug.LogWarning("[NetworkCombatSync] ❌ SendAttack - target is NULL!");
            return;
        }

        // Получаем socketId цели (если это NetworkPlayer)
        string targetId = "";
        string targetType = "enemy"; // По умолчанию атакуем врага

        NetworkPlayer networkTarget = target.GetComponent<NetworkPlayer>();
        if (networkTarget != null)
        {
            // Это другой игрок!
            targetId = networkTarget.socketId;
            targetType = "player";
            Debug.Log($"[NetworkCombatSync] Атака на игрока: {networkTarget.username} (Socket: {targetId})");
        }
        else
        {
            // Это NPC враг
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null)
            {
                targetId = enemy.GetEnemyId(); // Получаем ID врага
                Debug.Log($"[NetworkCombatSync] Атака на врага: {enemy.GetEnemyName()} (ID: {targetId})");
            }
        }

        if (string.IsNullOrEmpty(targetId))
        {
            Debug.LogWarning($"[NetworkCombatSync] ❌ SendAttack - не удалось определить ID цели! Target: {target.name}, Has Enemy: {target.GetComponent<Enemy>() != null}, Has NetworkPlayer: {target.GetComponent<NetworkPlayer>() != null}");
            return;
        }

        // Отправляем атаку на сервер (сервер сам рассчитает урон на основе SPECIAL статов)
        Vector3 attackPosition = transform.position;
        Vector3 targetPosition = target.transform.position;
        Vector3 attackDirection = (targetPosition - attackPosition).normalized;

        SocketIOManager.Instance.SendPlayerAttack(targetType, targetId, damage, attackType, attackPosition, attackDirection, targetPosition);
        Debug.Log($"[NetworkCombatSync] ✅ Атака отправлена на сервер: {attackType} → {targetType} (ID: {targetId})");
    }

    /// <summary>
    /// Отправить использование скилла на сервер
    /// </summary>
    public void SendSkill(int skillId, GameObject target, Vector3 targetPosition)
    {
        if (!enableSync || !isMultiplayer || SocketIOManager.Instance == null)
        {
            Debug.Log("[NetworkCombatSync] SendSkill пропущен (не мультиплеер или нет подключения)");
            return;
        }

        if (!SocketIOManager.Instance.IsConnected)
        {
            Debug.LogWarning("[NetworkCombatSync] SendSkill пропущен - нет подключения к серверу");
            return;
        }

        string targetSocketId = "";
        NetworkPlayer networkTarget = target?.GetComponent<NetworkPlayer>();
        if (networkTarget != null)
        {
            targetSocketId = networkTarget.socketId;
            Debug.Log($"[NetworkCombatSync] Использование скилла на игрока: {networkTarget.username}");
        }

        SocketIOManager.Instance.SendPlayerSkill(skillId, targetSocketId, targetPosition);
        Debug.Log($"[NetworkCombatSync] ✅ Скилл отправлен на сервер: ID={skillId}");
    }

    /// <summary>
    /// Синхронизировать здоровье/ману с сервером
    /// </summary>
    private void SyncHealth()
    {
        if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
            return;

        float currentHP = healthSystem != null ? healthSystem.CurrentHealth : 0;
        float maxHP = healthSystem != null ? healthSystem.MaxHealth : 100;
        float currentMP = manaSystem != null ? manaSystem.CurrentMana : 0;
        float maxMP = manaSystem != null ? manaSystem.MaxMana : 100;

        // Определяем кто атаковал (пока не знаем, отправим пустую строку)
        string lastAttackerId = "";

        // Если HP изменилось - отправляем информацию о получении урона
        if (currentHP < maxHP)
        {
            float damage = maxHP - currentHP;
            SocketIOManager.Instance.SendPlayerDamaged(damage, currentHP, maxHP, lastAttackerId);
        }
    }

    /// <summary>
    /// Обработчик изменения здоровья
    /// </summary>
    private void OnHealthChanged(float current, float max)
    {
        // Синхронизируем немедленно при получении урона
        SyncHealth();
    }

    /// <summary>
    /// Обработчик изменения маны
    /// </summary>
    private void OnManaChanged(float current, float max)
    {
        // Синхронизируем немедленно при использовании маны
        SyncHealth();
    }

    /// <summary>
    /// Обработчик смерти игрока
    /// </summary>
    private void OnPlayerDeath()
    {
        Debug.Log("[NetworkCombatSync] 💀 Игрок погиб");

        // Сервер уже знает о нашей смерти через health update
        // Здесь можем показать UI смерти
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= OnHealthChanged;
            healthSystem.OnDeath -= OnPlayerDeath;
        }

        if (manaSystem != null)
        {
            manaSystem.OnManaChanged -= OnManaChanged;
        }
    }
}
