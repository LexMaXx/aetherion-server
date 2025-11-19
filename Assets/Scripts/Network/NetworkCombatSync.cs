using UnityEngine;

/// <summary>
/// Синхронизирует боевые действия локального игрока с сервером
/// Перехватывает события атаки и отправляет их через WebSocket
/// ОБНОВЛЕНО: Поддерживает PlayerAttack (старый) И PlayerAttackNew (новый)
/// </summary>
public class NetworkCombatSync : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool enableSync = true;

    // ОБНОВЛЕНО: Поддерживаем обе системы атаки
    private PlayerAttack playerAttack;           // Старая система (legacy)
    private PlayerAttackNew playerAttackNew;     // НОВАЯ система с BasicAttackConfig
    private HealthSystem healthSystem;
    private ManaSystem manaSystem;
    private bool isMultiplayer = false;

    // Cooldown для отправки HP/MP (не отправляем каждый кадр)
    private float lastHealthSync = 0f;
    private float healthSyncInterval = 0.5f; // Раз в 0.5 секунды

    // Отслеживание предыдущего HP для определения урона/регенерации
    private float previousHP = 0f;

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

        // ОБНОВЛЕНО: Находим обе системы атаки
        playerAttack = GetComponent<PlayerAttack>();       // Старая (может быть null)
        playerAttackNew = GetComponent<PlayerAttackNew>(); // НОВАЯ (может быть null)

        // Проверяем какая система используется
        if (playerAttackNew != null)
        {
            Debug.Log("[NetworkCombatSync] ✅ Обнаружена НОВАЯ система атаки (PlayerAttackNew)");
        }
        else if (playerAttack != null)
        {
            Debug.Log("[NetworkCombatSync] ⚠️ Обнаружена СТАРАЯ система атаки (PlayerAttack)");
        }
        else
        {
            Debug.LogWarning("[NetworkCombatSync] ❌ НЕТ системы атаки! Ни PlayerAttack, ни PlayerAttackNew не найдены!");
        }

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

        // Сохраняем начальное HP
        if (healthSystem != null)
        {
            previousHP = healthSystem.CurrentHealth;
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
    /// ИЗМЕНЕНО: Теперь отправляет SPECIAL stats вместо pre-calculated damage
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

        // Получаем SPECIAL stats для отправки на сервер
        CharacterStats characterStats = GetComponent<CharacterStats>();
        PlayerAttack playerAttack = GetComponent<PlayerAttack>();

        if (characterStats == null)
        {
            Debug.LogError("[NetworkCombatSync] ❌ CharacterStats не найден! Не могу отправить статы на сервер.");
            return;
        }

        // ОБНОВЛЕНО: Получаем базовый урон из НОВОЙ или старой системы
        float baseDamage = 0f;

        if (playerAttackNew != null && playerAttackNew.attackConfig != null)
        {
            // НОВАЯ система - берём из BasicAttackConfig
            baseDamage = playerAttackNew.attackConfig.baseDamage;
            Debug.Log($"[NetworkCombatSync] ✅ Используем НОВУЮ систему (PlayerAttackNew), базовый урон: {baseDamage}");
        }
        else if (playerAttack != null)
        {
            // СТАРАЯ система - берём из PlayerAttack
            baseDamage = playerAttack.BaseDamage;
            Debug.Log($"[NetworkCombatSync] ⚠️ Используем СТАРУЮ систему (PlayerAttack), базовый урон: {baseDamage}");
        }
        else
        {
            Debug.LogError("[NetworkCombatSync] ❌ НЕТ системы атаки! Ни PlayerAttackNew, ни PlayerAttack не найдены!");
            return;
        }

        // Отправляем атаку на сервер с SPECIAL stats (сервер сам рассчитает урон и криты!)
        Vector3 attackPosition = transform.position;
        Vector3 targetPosition = target.transform.position;
        Vector3 attackDirection = (targetPosition - attackPosition).normalized;

        SocketIOManager.Instance.SendPlayerAttack(
            targetType,
            targetId,
            characterStats.strength,      // SPECIAL: Strength для физ. урона
            characterStats.intelligence,  // SPECIAL: Intelligence для маг. урона
            characterStats.luck,          // SPECIAL: Luck для критов
            baseDamage,                   // Базовый урон оружия (БЕЗ бонусов)
            attackType,
            attackPosition,
            attackDirection,
            targetPosition
        );

        Debug.Log($"[NetworkCombatSync] ✅ Атака отправлена на сервер: {attackType} → {targetType} (ID: {targetId})");
        Debug.Log($"[NetworkCombatSync] 📊 SPECIAL: STR={characterStats.strength}, INT={characterStats.intelligence}, LUCK={characterStats.luck}, Base Damage={baseDamage}");
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

        // ИСПРАВЛЕНО: Отправляем ТОЛЬКО если HP УМЕНЬШИЛОСЬ (урон), НЕ отправляем при регенерации!
        if (currentHP < previousHP)
        {
            float damage = previousHP - currentHP;
            SocketIOManager.Instance.SendPlayerDamaged(damage, currentHP, maxHP, lastAttackerId);
            Debug.Log($"[NetworkCombatSync] 💔 Урон: -{damage:F1} HP, осталось {currentHP:F1}/{maxHP:F1}");
        }

        // Обновляем previousHP для следующей проверки
        previousHP = currentHP;
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
