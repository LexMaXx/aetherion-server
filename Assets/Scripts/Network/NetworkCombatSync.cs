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
        if (!enableSync || !isMultiplayer || WebSocketClient.Instance == null)
            return;

        // Получаем socketId цели (если это NetworkPlayer)
        string targetSocketId = "";
        NetworkPlayer networkTarget = target.GetComponent<NetworkPlayer>();
        if (networkTarget != null)
        {
            targetSocketId = networkTarget.socketId;
        }

        // Отправляем на сервер
        WebSocketClient.Instance.SendAttack(targetSocketId, damage, attackType);

        Debug.Log($"[NetworkCombatSync] ⚔️ Атака отправлена на сервер: {damage} урона, тип: {attackType}");
    }

    /// <summary>
    /// Отправить использование скилла на сервер
    /// </summary>
    public void SendSkill(int skillId, GameObject target, Vector3 targetPosition)
    {
        if (!enableSync || !isMultiplayer || WebSocketClient.Instance == null)
            return;

        string targetSocketId = "";
        NetworkPlayer networkTarget = target?.GetComponent<NetworkPlayer>();
        if (networkTarget != null)
        {
            targetSocketId = networkTarget.socketId;
        }

        WebSocketClient.Instance.SendSkill(skillId, targetSocketId, targetPosition);

        Debug.Log($"[NetworkCombatSync] 🔮 Скилл {skillId} отправлен на сервер");
    }

    /// <summary>
    /// Синхронизировать здоровье/ману с сервером
    /// </summary>
    private void SyncHealth()
    {
        if (WebSocketClient.Instance == null || !WebSocketClient.Instance.IsConnected)
            return;

        int currentHP = healthSystem != null ? healthSystem.CurrentHealth : 0;
        int maxHP = healthSystem != null ? healthSystem.MaxHealth : 100;
        int currentMP = manaSystem != null ? manaSystem.CurrentMana : 0;
        int maxMP = manaSystem != null ? manaSystem.MaxMana : 100;

        WebSocketClient.Instance.UpdateHealth(currentHP, maxHP, currentMP, maxMP);
    }

    /// <summary>
    /// Обработчик изменения здоровья
    /// </summary>
    private void OnHealthChanged(int current, int max)
    {
        // Синхронизируем немедленно при получении урона
        SyncHealth();
    }

    /// <summary>
    /// Обработчик изменения маны
    /// </summary>
    private void OnManaChanged(int current, int max)
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
