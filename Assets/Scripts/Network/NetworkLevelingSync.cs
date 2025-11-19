using UnityEngine;
using System.Collections;

/// <summary>
/// Синхронизация прокачки персонажа в реальном времени через WebSocket
/// Отправляет изменения уровня и статов другим игрокам мгновенно
/// Работает с LevelingSystem и CharacterStats в BattleScene для онлайн игры
/// </summary>
public class NetworkLevelingSync : MonoBehaviour
{
    [Header("Dependencies")]
    private LevelingSystem levelingSystem;
    private CharacterStats characterStats;
    private SocketIOManager socketManager;

    [Header("Settings")]
    [Tooltip("Автосохранение на сервер после прокачки")]
    [SerializeField] private bool autoSaveToServer = true;

    [Tooltip("Задержка перед сохранением (секунды)")]
    [SerializeField] private float saveDelay = 2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private Coroutine saveCoroutine;
    private string characterClass;
    private bool isInitialized = false;

    void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Инициализация компонента
    /// </summary>
    private void Initialize()
    {
        // Получаем зависимости
        levelingSystem = GetComponent<LevelingSystem>();
        characterStats = GetComponent<CharacterStats>();
        socketManager = SocketIOManager.Instance;

        if (levelingSystem == null)
        {
            Debug.LogError("[NetworkLevelingSync] LevelingSystem не найден! Добавьте компонент на игрока.");
            return;
        }

        if (characterStats == null)
        {
            Debug.LogError("[NetworkLevelingSync] CharacterStats не найден! Добавьте компонент на игрока.");
            return;
        }

        if (socketManager == null)
        {
            Debug.LogWarning("[NetworkLevelingSync] SocketIOManager не найден! Онлайн синхронизация отключена.");
            return;
        }

        // Получаем класс персонажа
        characterClass = characterStats.ClassName;

        // Подписываемся на события LevelingSystem
        levelingSystem.OnLevelUp += OnLevelUpHandler;
        levelingSystem.OnStatPointsChanged += OnStatPointsChangedHandler;

        // Подписываемся на события CharacterStats
        characterStats.OnStatsChanged += OnStatsChangedHandler;

        // Регистрируем обработчики сетевых событий
        RegisterNetworkHandlers();

        isInitialized = true;

        if (showDebugLogs)
        {
            Debug.Log($"[NetworkLevelingSync] ✅ Инициализирован для класса {characterClass}");
        }
    }

    /// <summary>
    /// Регистрация обработчиков сетевых событий от сервера
    /// </summary>
    private void RegisterNetworkHandlers()
    {
        if (socketManager == null) return;

        // Событие: другой игрок получил уровень
        socketManager.On("player_level_up", OnPlayerLevelUpReceived);

        // Событие: другой игрок повысил стат
        socketManager.On("player_stat_upgraded", OnPlayerStatUpgradedReceived);

        // Событие: полная синхронизация статов игрока
        socketManager.On("player_stats_sync", OnPlayerStatsSyncReceived);

        if (showDebugLogs)
        {
            Debug.Log("[NetworkLevelingSync] ✅ Сетевые обработчики зарегистрированы");
        }
    }

    // ═══════════════════════════════════════════════════════
    // ЛОКАЛЬНЫЕ СОБЫТИЯ (когда локальный игрок прокачивается)
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Обработчик получения уровня локальным игроком
    /// </summary>
    private void OnLevelUpHandler(int newLevel)
    {
        if (!isInitialized) return;

        if (showDebugLogs)
        {
            Debug.Log($"[NetworkLevelingSync] 🎉 Локальный игрок получил уровень {newLevel}!");
        }

        // Отправляем событие на сервер
        BroadcastLevelUp(newLevel);

        // Автосохранение на сервер
        if (autoSaveToServer)
        {
            ScheduleSaveToServer();
        }
    }

    /// <summary>
    /// Обработчик изменения доступных очков характеристик
    /// </summary>
    private void OnStatPointsChangedHandler(int newPoints)
    {
        if (!isInitialized) return;

        if (showDebugLogs)
        {
            Debug.Log($"[NetworkLevelingSync] 📊 Очки характеристик изменились: {newPoints}");
        }

        // При изменении очков также отправляем полную синхронизацию
        BroadcastFullStats();
    }

    /// <summary>
    /// Обработчик изменения статов (когда игрок повышает SPECIAL)
    /// </summary>
    private void OnStatsChangedHandler()
    {
        if (!isInitialized) return;

        if (showDebugLogs)
        {
            Debug.Log("[NetworkLevelingSync] 📈 Характеристики изменились!");
        }

        // Отправляем полную синхронизацию статов
        BroadcastFullStats();

        // Автосохранение на сервер
        if (autoSaveToServer)
        {
            ScheduleSaveToServer();
        }
    }

    // ═══════════════════════════════════════════════════════
    // ОТПРАВКА ДАННЫХ НА СЕРВЕР (Broadcast)
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Отправить событие получения уровня всем игрокам в комнате
    /// </summary>
    private void BroadcastLevelUp(int newLevel)
    {
        if (socketManager == null || !socketManager.IsConnected) return;

        string json = JsonUtility.ToJson(new LevelUpEvent
        {
            newLevel = newLevel,
            characterClass = characterClass,
            availableStatPoints = levelingSystem.AvailableStatPoints
        });

        socketManager.Emit("player_level_up", json);

        if (showDebugLogs)
        {
            Debug.Log($"[NetworkLevelingSync] 📤 Отправлен level_up: {json}");
        }
    }

    /// <summary>
    /// Отправить полную синхронизацию статов и уровня
    /// </summary>
    private void BroadcastFullStats()
    {
        if (socketManager == null || !socketManager.IsConnected) return;

        string json = JsonUtility.ToJson(new PlayerStatsSync
        {
            level = levelingSystem.CurrentLevel,
            experience = levelingSystem.CurrentExperience,
            availableStatPoints = levelingSystem.AvailableStatPoints,
            characterClass = characterClass,
            stats = characterStats.ExportData()
        });

        socketManager.Emit("player_stats_sync", json);

        if (showDebugLogs)
        {
            Debug.Log($"[NetworkLevelingSync] 📤 Отправлена полная синхронизация статов: Level {levelingSystem.CurrentLevel}");
        }
    }

    // ═══════════════════════════════════════════════════════
    // ПОЛУЧЕНИЕ ДАННЫХ ОТ СЕРВЕРА (других игроков)
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Получено событие: другой игрок получил уровень
    /// </summary>
    private void OnPlayerLevelUpReceived(string data)
    {
        try
        {
            var levelUpData = JsonUtility.FromJson<PlayerLevelUpBroadcast>(data);

            if (showDebugLogs)
            {
                Debug.Log($"[NetworkLevelingSync] 📥 Игрок {levelUpData.username} ({levelUpData.characterClass}) получил уровень {levelUpData.newLevel}!");
            }

            // TODO: Показать уведомление в UI
            // ShowLevelUpNotification(levelUpData.username, levelUpData.newLevel);

            // TODO: Воспроизвести звук/эффект для других игроков
            // PlayLevelUpEffectForPlayer(levelUpData.socketId);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NetworkLevelingSync] Ошибка парсинга player_level_up: {e.Message}\nData: {data}");
        }
    }

    /// <summary>
    /// Получено событие: другой игрок повысил стат
    /// </summary>
    private void OnPlayerStatUpgradedReceived(string data)
    {
        try
        {
            var statUpgradeData = JsonUtility.FromJson<PlayerStatUpgradeBroadcast>(data);

            if (showDebugLogs)
            {
                Debug.Log($"[NetworkLevelingSync] 📥 Игрок {statUpgradeData.username} повысил {statUpgradeData.statName} до {statUpgradeData.newValue}");
            }

            // Обновляем статы у NetworkPlayerEntity (если это сетевой игрок)
            UpdateNetworkPlayerStat(statUpgradeData.socketId, statUpgradeData.statName, statUpgradeData.newValue);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NetworkLevelingSync] Ошибка парсинга player_stat_upgraded: {e.Message}\nData: {data}");
        }
    }

    /// <summary>
    /// Получена полная синхронизация статов игрока
    /// </summary>
    private void OnPlayerStatsSyncReceived(string data)
    {
        try
        {
            var syncData = JsonUtility.FromJson<PlayerStatsSyncBroadcast>(data);

            if (showDebugLogs)
            {
                Debug.Log($"[NetworkLevelingSync] 📥 Синхронизация статов игрока {syncData.username}: Level {syncData.level}, Points {syncData.availableStatPoints}");
            }

            // Обновляем статы у NetworkPlayerEntity
            UpdateNetworkPlayerFullStats(syncData.socketId, syncData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NetworkLevelingSync] Ошибка парсинга player_stats_sync: {e.Message}\nData: {data}");
        }
    }

    /// <summary>
    /// Обновить один стат у сетевого игрока
    /// </summary>
    private void UpdateNetworkPlayerStat(string socketId, string statName, int newValue)
    {
        // Находим сетевого игрока по socketId
        NetworkPlayerEntity[] networkPlayers = FindObjectsByType<NetworkPlayerEntity>(FindObjectsSortMode.None);

        foreach (var networkPlayer in networkPlayers)
        {
            // Получаем NetworkPlayer компонент для доступа к socketId
            NetworkPlayer np = networkPlayer.GetComponent<NetworkPlayer>();
            if (np != null && np.socketId == socketId)
            {
                CharacterStats stats = networkPlayer.GetComponent<CharacterStats>();
                if (stats != null)
                {
                    // Обновляем стат через рефлексию (или через switch-case)
                    UpdateStatValue(stats, statName, newValue);
                    stats.RecalculateStats();

                    if (showDebugLogs)
                    {
                        Debug.Log($"[NetworkLevelingSync] ✅ Обновлен стат {statName}={newValue} для {np.username}");
                    }
                }
                break;
            }
        }
    }

    /// <summary>
    /// Обновить все статы у сетевого игрока
    /// </summary>
    private void UpdateNetworkPlayerFullStats(string socketId, PlayerStatsSyncBroadcast syncData)
    {
        NetworkPlayerEntity[] networkPlayers = FindObjectsByType<NetworkPlayerEntity>(FindObjectsSortMode.None);

        foreach (var networkPlayer in networkPlayers)
        {
            // Получаем NetworkPlayer компонент для доступа к socketId
            NetworkPlayer np = networkPlayer.GetComponent<NetworkPlayer>();
            if (np != null && np.socketId == socketId)
            {
                CharacterStats stats = networkPlayer.GetComponent<CharacterStats>();
                if (stats != null)
                {
                    // Импортируем все статы
                    stats.ImportData(syncData.stats);

                    if (showDebugLogs)
                    {
                        Debug.Log($"[NetworkLevelingSync] ✅ Полная синхронизация статов для {np.username}");
                    }
                }
                break;
            }
        }
    }

    /// <summary>
    /// Обновить значение характеристики
    /// </summary>
    private void UpdateStatValue(CharacterStats stats, string statName, int value)
    {
        // Используем рефлексию для установки значения
        var field = typeof(CharacterStats).GetField(statName.ToLower(),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(stats, value);
        }
        else
        {
            Debug.LogWarning($"[NetworkLevelingSync] Не найдено поле {statName} в CharacterStats");
        }
    }

    // ═══════════════════════════════════════════════════════
    // АВТОСОХРАНЕНИЕ НА СЕРВЕР
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Запланировать сохранение на сервер с задержкой
    /// </summary>
    private void ScheduleSaveToServer()
    {
        // Отменяем предыдущее сохранение если оно есть
        if (saveCoroutine != null)
        {
            StopCoroutine(saveCoroutine);
        }

        // Запускаем новое сохранение с задержкой
        saveCoroutine = StartCoroutine(SaveToServerDelayed());
    }

    /// <summary>
    /// Сохранение на сервер с задержкой (чтобы не спамить запросы)
    /// </summary>
    private IEnumerator SaveToServerDelayed()
    {
        yield return new WaitForSeconds(saveDelay);

        if (showDebugLogs)
        {
            Debug.Log("[NetworkLevelingSync] 💾 Автосохранение на сервер...");
        }

        levelingSystem.SaveToServer();
    }

    // ═══════════════════════════════════════════════════════
    // ПУБЛИЧНЫЕ МЕТОДЫ
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Принудительно отправить полную синхронизацию
    /// </summary>
    public void ForceBroadcastStats()
    {
        BroadcastFullStats();
    }

    /// <summary>
    /// Принудительно сохранить на сервер немедленно
    /// </summary>
    public void ForceSaveToServer()
    {
        if (levelingSystem != null)
        {
            levelingSystem.SaveToServer();
        }
    }

    // ═══════════════════════════════════════════════════════
    // CLEANUP
    // ═══════════════════════════════════════════════════════

    void OnDestroy()
    {
        // Отписываемся от событий
        if (levelingSystem != null)
        {
            levelingSystem.OnLevelUp -= OnLevelUpHandler;
            levelingSystem.OnStatPointsChanged -= OnStatPointsChangedHandler;
        }

        if (characterStats != null)
        {
            characterStats.OnStatsChanged -= OnStatsChangedHandler;
        }

        // ПРИМЕЧАНИЕ: SocketIOManager не имеет метода Off для отписки
        // События автоматически очистятся при уничтожении сокета
        // Если нужна явная отписка - нужно добавить метод Off в SocketIOManager

        if (showDebugLogs)
        {
            Debug.Log("[NetworkLevelingSync] 🔌 Отписка от событий выполнена");
        }
    }
}

// ═══════════════════════════════════════════════════════
// DATA STRUCTURES (JSON для WebSocket)
// ═══════════════════════════════════════════════════════

/// <summary>
/// Событие получения уровня (отправка)
/// </summary>
[System.Serializable]
public class LevelUpEvent
{
    public int newLevel;
    public string characterClass;
    public int availableStatPoints;
}

/// <summary>
/// Полная синхронизация статов (отправка)
/// </summary>
[System.Serializable]
public class PlayerStatsSync
{
    public int level;
    public int experience;
    public int availableStatPoints;
    public string characterClass;
    public CharacterStatsData stats;
}

/// <summary>
/// Событие получения уровня другим игроком (получение от сервера)
/// </summary>
[System.Serializable]
public class PlayerLevelUpBroadcast
{
    public string socketId;
    public string username;
    public string characterClass;
    public int newLevel;
    public int availableStatPoints;
}

/// <summary>
/// Событие повышения стата другим игроком (получение от сервера)
/// </summary>
[System.Serializable]
public class PlayerStatUpgradeBroadcast
{
    public string socketId;
    public string username;
    public string statName;      // "strength", "endurance", etc.
    public int newValue;         // Новое значение характеристики
}

/// <summary>
/// Полная синхронизация статов другого игрока (получение от сервера)
/// </summary>
[System.Serializable]
public class PlayerStatsSyncBroadcast
{
    public string socketId;
    public string username;
    public int level;
    public int availableStatPoints;
    public CharacterStatsData stats;
}
