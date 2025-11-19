using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет отложенным спавном игроков для синхронизации
/// Игроки получают позиции спавна, но остаются невидимыми до game_start
/// </summary>
public class DelayedSpawnManager : MonoBehaviour
{
    public static DelayedSpawnManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool enableDelayedSpawn = true;

    // Список локальных игроков ожидающих синхронного спавна
    private List<DelayedSpawnData> pendingLocalSpawns = new List<DelayedSpawnData>();

    // Список сетевых игроков ожидающих синхронного спавна
    private Dictionary<string, DelayedNetworkSpawnData> pendingNetworkSpawns = new Dictionary<string, DelayedNetworkSpawnData>();

    private bool isWaitingForGameStart = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[DelayedSpawn] Дубликат DelayedSpawnManager, уничтожаем...");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[DelayedSpawn] ✅ DelayedSpawnManager инициализирован");
    }

    /// <summary>
    /// Зарегистрировать локального игрока для отложенного спавна
    /// </summary>
    public void RegisterLocalPlayerSpawn(GameObject playerPrefab, Vector3 spawnPosition, Quaternion spawnRotation, string characterClass)
    {
        if (!enableDelayedSpawn)
        {
            Debug.Log("[DelayedSpawn] ⚠️ Отложенный спавн отключен, пропускаем регистрацию");
            return;
        }

        DelayedSpawnData data = new DelayedSpawnData
        {
            playerPrefab = playerPrefab,
            spawnPosition = spawnPosition,
            spawnRotation = spawnRotation,
            characterClass = characterClass
        };

        pendingLocalSpawns.Add(data);
        isWaitingForGameStart = true;

        Debug.Log($"[DelayedSpawn] 📝 Локальный игрок ({characterClass}) зарегистрирован для отложенного спавна в позиции {spawnPosition}");
        Debug.Log($"[DelayedSpawn] ⏳ Ожидаем game_start события от сервера...");
    }

    /// <summary>
    /// Зарегистрировать сетевого игрока для отложенного спавна
    /// </summary>
    public void RegisterNetworkPlayerSpawn(string socketId, GameObject playerPrefab, Vector3 spawnPosition, string username, string characterClass, CharacterStats stats)
    {
        if (!enableDelayedSpawn)
        {
            Debug.Log("[DelayedSpawn] ⚠️ Отложенный спавн отключен, пропускаем регистрацию");
            return;
        }

        DelayedNetworkSpawnData data = new DelayedNetworkSpawnData
        {
            socketId = socketId,
            playerPrefab = playerPrefab,
            spawnPosition = spawnPosition,
            username = username,
            characterClass = characterClass,
            stats = stats
        };

        pendingNetworkSpawns[socketId] = data;

        Debug.Log($"[DelayedSpawn] 📝 Сетевой игрок {username} ({characterClass}) зарегистрирован для отложенного спавна");
    }

    /// <summary>
    /// Обработать событие game_start - заспавнить всех ожидающих игроков
    /// </summary>
    public void OnGameStartReceived()
    {
        Debug.Log($"[DelayedSpawn] 🎮 game_start получен! Спавним всех игроков синхронно...");
        Debug.Log($"[DelayedSpawn] 📊 Локальных игроков: {pendingLocalSpawns.Count}");
        Debug.Log($"[DelayedSpawn] 📊 Сетевых игроков: {pendingNetworkSpawns.Count}");

        // 1. Спавним локального игрока
        foreach (var data in pendingLocalSpawns)
        {
            SpawnLocalPlayer(data);
        }

        // 2. Спавним сетевых игроков
        foreach (var kvp in pendingNetworkSpawns)
        {
            SpawnNetworkPlayer(kvp.Value);
        }

        // 3. Очищаем списки
        pendingLocalSpawns.Clear();
        pendingNetworkSpawns.Clear();
        isWaitingForGameStart = false;

        Debug.Log($"[DelayedSpawn] ✅ Все игроки заспавнены синхронно!");
    }

    /// <summary>
    /// Заспавнить локального игрока
    /// </summary>
    private void SpawnLocalPlayer(DelayedSpawnData data)
    {
        Debug.Log($"[DelayedSpawn] 🎯 Спавним локального игрока ({data.characterClass}) в {data.spawnPosition}");

        GameObject player = Instantiate(data.playerPrefab, data.spawnPosition, data.spawnRotation);
        player.name = $"LocalPlayer_{data.characterClass}";

        Debug.Log($"[DelayedSpawn] ✅ Локальный игрок заспавнен: {player.name}");
    }

    /// <summary>
    /// Заспавнить сетевого игрока
    /// </summary>
    private void SpawnNetworkPlayer(DelayedNetworkSpawnData data)
    {
        Debug.Log($"[DelayedSpawn] 🌐 Спавним сетевого игрока {data.username} ({data.characterClass}) в {data.spawnPosition}");

        GameObject networkPlayerObj = Instantiate(data.playerPrefab, data.spawnPosition, Quaternion.identity);
        networkPlayerObj.name = $"NetworkPlayer_{data.username}";

        // Настройка NetworkPlayer компонента
        NetworkPlayer networkPlayer = networkPlayerObj.GetComponent<NetworkPlayer>();
        if (networkPlayer != null)
        {
            networkPlayer.socketId = data.socketId;
            networkPlayer.username = data.username;
            networkPlayer.characterClass = data.characterClass;
        }

        // Настройка CharacterStats
        CharacterStats characterStats = networkPlayerObj.GetComponent<CharacterStats>();
        if (characterStats != null && data.stats != null)
        {
            characterStats.strength = data.stats.strength;
            characterStats.perception = data.stats.perception;
            characterStats.endurance = data.stats.endurance;
            characterStats.wisdom = data.stats.wisdom;
            characterStats.intelligence = data.stats.intelligence;
            characterStats.agility = data.stats.agility;
            characterStats.luck = data.stats.luck;
        }

        Debug.Log($"[DelayedSpawn] ✅ Сетевой игрок заспавнен: {data.username}");
    }

    /// <summary>
    /// Проверить ожидает ли система game_start
    /// </summary>
    public bool IsWaitingForGameStart()
    {
        return isWaitingForGameStart;
    }

    /// <summary>
    /// Очистить все ожидающие спавны (на случай отмены матчмейкинга)
    /// </summary>
    public void ClearAllPendingSpawns()
    {
        Debug.Log($"[DelayedSpawn] 🧹 Очищаем все ожидающие спавны...");
        pendingLocalSpawns.Clear();
        pendingNetworkSpawns.Clear();
        isWaitingForGameStart = false;
    }

    /// <summary>
    /// Включить/выключить систему отложенного спавна
    /// </summary>
    public void SetDelayedSpawnEnabled(bool enabled)
    {
        enableDelayedSpawn = enabled;
        Debug.Log($"[DelayedSpawn] Отложенный спавн: {(enabled ? "Включен" : "Отключен")}");
    }
}

/// <summary>
/// Данные для отложенного спавна локального игрока
/// </summary>
[System.Serializable]
public class DelayedSpawnData
{
    public GameObject playerPrefab;
    public Vector3 spawnPosition;
    public Quaternion spawnRotation;
    public string characterClass;
}

/// <summary>
/// Данные для отложенного спавна сетевого игрока
/// </summary>
[System.Serializable]
public class DelayedNetworkSpawnData
{
    public string socketId;
    public GameObject playerPrefab;
    public Vector3 spawnPosition;
    public string username;
    public string characterClass;
    public CharacterStats stats;
}
