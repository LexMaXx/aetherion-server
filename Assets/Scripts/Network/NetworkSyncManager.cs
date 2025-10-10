using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Менеджер синхронизации мультиплеера
/// Управляет всеми сетевыми игроками, обрабатывает события от сервера
/// </summary>
public class NetworkSyncManager : MonoBehaviour
{
    public static NetworkSyncManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float positionSyncInterval = 0.1f; // 10 Hz
    [SerializeField] private bool syncEnabled = true;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Character Prefabs")]
    [SerializeField] private GameObject warriorPrefab;
    [SerializeField] private GameObject magePrefab;
    [SerializeField] private GameObject archerPrefab;
    [SerializeField] private GameObject roguePrefab;
    [SerializeField] private GameObject paladinPrefab;

    [Header("UI")]
    [SerializeField] private GameObject nameplatePrefab;

    // Network players
    private Dictionary<string, NetworkPlayer> networkPlayers = new Dictionary<string, NetworkPlayer>();

    // Local player reference
    private GameObject localPlayer;
    private string localPlayerClass;
    private float lastPositionSync = 0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Subscribe to WebSocket events
        SubscribeToNetworkEvents();
    }

    void Update()
    {
        if (!syncEnabled) return;

        // Send local player position to server
        if (Time.time - lastPositionSync > positionSyncInterval)
        {
            SyncLocalPlayerPosition();
            lastPositionSync = Time.time;
        }
    }

    /// <summary>
    /// Подписаться на сетевые события
    /// </summary>
    private void SubscribeToNetworkEvents()
    {
        if (WebSocketClient.Instance == null)
        {
            Debug.LogError("[NetworkSync] WebSocketClient не найден!");
            return;
        }

        // Player joined room
        WebSocketClient.Instance.On("player_joined", OnPlayerJoined);

        // Player left room
        WebSocketClient.Instance.On("player_left", OnPlayerLeft);

        // Position update
        WebSocketClient.Instance.On("position_update", OnPositionUpdate);

        // Player attacked
        WebSocketClient.Instance.On("player_attacked", OnPlayerAttacked);

        // Player used skill
        WebSocketClient.Instance.On("skill_used", OnSkillUsed);

        // Health update
        WebSocketClient.Instance.On("health_update", OnHealthUpdate);

        // Player died
        WebSocketClient.Instance.On("player_died", OnPlayerDied);

        // Player respawned
        WebSocketClient.Instance.On("player_respawned", OnPlayerRespawned);

        // Room state (full player list)
        WebSocketClient.Instance.On("room_state", OnRoomState);

        Debug.Log("[NetworkSync] ✅ Подписан на сетевые события");
    }

    /// <summary>
    /// Установить локального игрока
    /// </summary>
    public void SetLocalPlayer(GameObject player, string characterClass)
    {
        localPlayer = player;
        localPlayerClass = characterClass;
        Debug.Log($"[NetworkSync] Локальный игрок установлен: {characterClass}");
    }

    /// <summary>
    /// Синхронизировать позицию локального игрока
    /// </summary>
    private void SyncLocalPlayerPosition()
    {
        if (localPlayer == null || WebSocketClient.Instance == null || !WebSocketClient.Instance.IsConnected)
            return;

        // Get animation state
        string animState = GetLocalPlayerAnimationState();

        // Send to server
        WebSocketClient.Instance.UpdatePosition(
            localPlayer.transform.position,
            localPlayer.transform.rotation,
            animState
        );
    }

    /// <summary>
    /// Получить текущее состояние анимации локального игрока
    /// </summary>
    private string GetLocalPlayerAnimationState()
    {
        var animator = localPlayer.GetComponent<Animator>();
        if (animator == null) return "Idle";

        if (animator.GetBool("isDead")) return "Dead";
        if (animator.GetBool("isAttacking")) return "Attacking";
        if (animator.GetBool("isRunning")) return "Running";
        if (animator.GetBool("isWalking")) return "Walking";

        return "Idle";
    }

    // ===== NETWORK EVENT HANDLERS =====

    /// <summary>
    /// Обработать подключение нового игрока
    /// </summary>
    private void OnPlayerJoined(string jsonData)
    {
        var data = JsonUtility.FromJson<PlayerJoinedData>(jsonData);
        Debug.Log($"[NetworkSync] Игрок подключился: {data.username} ({data.characterClass})");

        // Don't create network player for ourselves
        if (data.socketId == WebSocketClient.Instance.SessionId)
        {
            Debug.Log("[NetworkSync] Это мы сами, пропускаем");
            return;
        }

        // Spawn network player
        SpawnNetworkPlayer(data.socketId, data.username, data.characterClass, data.position);
    }

    /// <summary>
    /// Обработать отключение игрока
    /// </summary>
    private void OnPlayerLeft(string jsonData)
    {
        var data = JsonUtility.FromJson<PlayerLeftData>(jsonData);
        Debug.Log($"[NetworkSync] Игрок отключился: {data.socketId}");

        RemoveNetworkPlayer(data.socketId);
    }

    /// <summary>
    /// Обработать обновление позиции
    /// </summary>
    private void OnPositionUpdate(string jsonData)
    {
        var data = JsonUtility.FromJson<PositionUpdateEventData>(jsonData);

        // Skip our own updates
        if (data.socketId == WebSocketClient.Instance.SessionId) return;

        if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
        {
            Vector3 pos = new Vector3(data.x, data.y, data.z);
            Quaternion rot = Quaternion.Euler(0, data.rotationY, 0);

            player.UpdatePosition(pos, rot);
            player.UpdateAnimation(data.animationState);
        }
    }

    /// <summary>
    /// Обработать атаку игрока
    /// </summary>
    private void OnPlayerAttacked(string jsonData)
    {
        var data = JsonUtility.FromJson<PlayerAttackedData>(jsonData);
        Debug.Log($"[NetworkSync] Атака: {data.attackerSocketId} -> {data.targetSocketId}, Урон: {data.damage}");

        // Play attack animation on attacker
        if (networkPlayers.TryGetValue(data.attackerSocketId, out NetworkPlayer attacker))
        {
            attacker.PlayAttackAnimation(data.attackType);
        }

        // Show damage on target
        if (data.targetSocketId == WebSocketClient.Instance.SessionId)
        {
            // We got hit!
            ApplyDamageToLocalPlayer(data.damage);
        }
        else if (networkPlayers.TryGetValue(data.targetSocketId, out NetworkPlayer target))
        {
            target.ShowDamage(data.damage);
        }
    }

    /// <summary>
    /// Обработать использование скилла
    /// </summary>
    private void OnSkillUsed(string jsonData)
    {
        var data = JsonUtility.FromJson<SkillUsedData>(jsonData);
        Debug.Log($"[NetworkSync] Скилл использован: {data.socketId}, Skill ID: {data.skillId}");

        if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
        {
            player.PlayAttackAnimation("skill");
        }

        // TODO: Spawn skill visual effects
    }

    /// <summary>
    /// Обработать обновление здоровья
    /// </summary>
    private void OnHealthUpdate(string jsonData)
    {
        var data = JsonUtility.FromJson<HealthUpdateEventData>(jsonData);

        if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
        {
            player.UpdateHealth(data.currentHP, data.maxHP, data.currentMP, data.maxMP);
        }
    }

    /// <summary>
    /// Обработать смерть игрока
    /// </summary>
    private void OnPlayerDied(string jsonData)
    {
        var data = JsonUtility.FromJson<PlayerDiedData>(jsonData);
        Debug.Log($"[NetworkSync] Игрок погиб: {data.deadPlayerSocketId}, Убийца: {data.killerSocketId}");

        if (data.deadPlayerSocketId == WebSocketClient.Instance.SessionId)
        {
            // We died!
            OnLocalPlayerDied();
        }
        else if (networkPlayers.TryGetValue(data.deadPlayerSocketId, out NetworkPlayer player))
        {
            player.UpdateHealth(0, player.MaxHP, player.CurrentMP, player.MaxMP);
        }
    }

    /// <summary>
    /// Обработать респавн игрока
    /// </summary>
    private void OnPlayerRespawned(string jsonData)
    {
        var data = JsonUtility.FromJson<PlayerRespawnedData>(jsonData);
        Debug.Log($"[NetworkSync] Игрок возродился: {data.socketId}");

        if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
        {
            Vector3 spawnPos = new Vector3(data.spawnX, data.spawnY, data.spawnZ);
            player.OnRespawn(spawnPos);
        }
    }

    /// <summary>
    /// Обработать полное состояние комнаты (список всех игроков)
    /// </summary>
    private void OnRoomState(string jsonData)
    {
        var data = JsonUtility.FromJson<RoomStateData>(jsonData);
        Debug.Log($"[NetworkSync] Получено состояние комнаты: {data.players.Length} игроков");

        // Spawn all existing players
        foreach (var playerData in data.players)
        {
            // Skip ourselves
            if (playerData.socketId == WebSocketClient.Instance.SessionId) continue;

            // Spawn if not already exists
            if (!networkPlayers.ContainsKey(playerData.socketId))
            {
                SpawnNetworkPlayer(playerData.socketId, playerData.username, playerData.characterClass, playerData.position);
            }
        }
    }

    // ===== NETWORK PLAYER MANAGEMENT =====

    /// <summary>
    /// Создать сетевого игрока
    /// </summary>
    private void SpawnNetworkPlayer(string socketId, string username, string characterClass, PositionData position)
    {
        GameObject prefab = GetCharacterPrefab(characterClass);
        if (prefab == null)
        {
            Debug.LogError($"[NetworkSync] Префаб для класса {characterClass} не найден!");
            return;
        }

        Vector3 spawnPos = new Vector3(position.x, position.y, position.z);
        GameObject playerObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        playerObj.name = $"NetworkPlayer_{username}";

        // Add NetworkPlayer component
        NetworkPlayer networkPlayer = playerObj.AddComponent<NetworkPlayer>();
        networkPlayer.socketId = socketId;
        networkPlayer.username = username;
        networkPlayer.characterClass = characterClass;

        // Set nameplate prefab
        if (nameplatePrefab != null)
        {
            // Assign via reflection or make it public
            var field = typeof(NetworkPlayer).GetField("nameplatePrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(networkPlayer, nameplatePrefab);
        }

        networkPlayers[socketId] = networkPlayer;

        Debug.Log($"[NetworkSync] ✅ Создан сетевой игрок: {username} ({characterClass})");
    }

    /// <summary>
    /// Удалить сетевого игрока
    /// </summary>
    private void RemoveNetworkPlayer(string socketId)
    {
        if (networkPlayers.TryGetValue(socketId, out NetworkPlayer player))
        {
            Destroy(player.gameObject);
            networkPlayers.Remove(socketId);
            Debug.Log($"[NetworkSync] Удален сетевой игрок: {socketId}");
        }
    }

    /// <summary>
    /// Получить префаб персонажа по классу
    /// </summary>
    private GameObject GetCharacterPrefab(string characterClass)
    {
        switch (characterClass)
        {
            case "Warrior": return warriorPrefab;
            case "Mage": return magePrefab;
            case "Archer": return archerPrefab;
            case "Rogue": return roguePrefab;
            case "Paladin": return paladinPrefab;
            default:
                Debug.LogWarning($"[NetworkSync] Неизвестный класс: {characterClass}");
                return warriorPrefab; // Fallback
        }
    }

    /// <summary>
    /// Применить урон к локальному игроку
    /// </summary>
    private void ApplyDamageToLocalPlayer(float damage)
    {
        if (localPlayer == null) return;

        var healthSystem = localPlayer.GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.TakeDamage((int)damage);
            Debug.Log($"[NetworkSync] Получили урон: {damage}");
        }
    }

    /// <summary>
    /// Обработать смерть локального игрока
    /// </summary>
    private void OnLocalPlayerDied()
    {
        Debug.Log("[NetworkSync] Мы погибли!");

        // TODO: Show death screen, respawn button
        // For now, auto-respawn after 5 seconds
        Invoke(nameof(RequestRespawn), 5f);
    }

    /// <summary>
    /// Запросить респавн
    /// </summary>
    private void RequestRespawn()
    {
        WebSocketClient.Instance.RequestRespawn();
    }

    /// <summary>
    /// Очистить всех сетевых игроков (при выходе из комнаты)
    /// </summary>
    public void ClearAllNetworkPlayers()
    {
        foreach (var player in networkPlayers.Values)
        {
            if (player != null)
            {
                Destroy(player.gameObject);
            }
        }
        networkPlayers.Clear();
        Debug.Log("[NetworkSync] Все сетевые игроки удалены");
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (WebSocketClient.Instance != null)
        {
            WebSocketClient.Instance.Off("player_joined");
            WebSocketClient.Instance.Off("player_left");
            WebSocketClient.Instance.Off("position_update");
            WebSocketClient.Instance.Off("player_attacked");
            WebSocketClient.Instance.Off("skill_used");
            WebSocketClient.Instance.Off("health_update");
            WebSocketClient.Instance.Off("player_died");
            WebSocketClient.Instance.Off("player_respawned");
            WebSocketClient.Instance.Off("room_state");
        }
    }
}

// ===== EVENT DATA CLASSES =====

[Serializable]
public class PlayerJoinedData
{
    public string socketId;
    public string username;
    public string characterClass;
    public PositionData position;
}

[Serializable]
public class PlayerLeftData
{
    public string socketId;
}

[Serializable]
public class PositionUpdateEventData
{
    public string socketId;
    public float x;
    public float y;
    public float z;
    public float rotationY;
    public string animationState;
}

[Serializable]
public class PlayerAttackedData
{
    public string attackerSocketId;
    public string targetSocketId;
    public float damage;
    public string attackType;
}

[Serializable]
public class SkillUsedData
{
    public string socketId;
    public int skillId;
    public string targetSocketId;
    public float targetX;
    public float targetY;
    public float targetZ;
}

[Serializable]
public class HealthUpdateEventData
{
    public string socketId;
    public int currentHP;
    public int maxHP;
    public int currentMP;
    public int maxMP;
}

[Serializable]
public class PlayerDiedData
{
    public string deadPlayerSocketId;
    public string killerSocketId;
}

[Serializable]
public class PlayerRespawnedData
{
    public string socketId;
    public float spawnX;
    public float spawnY;
    public float spawnZ;
}

[Serializable]
public class RoomStateData
{
    public RoomPlayerData[] players;
}

[Serializable]
public class RoomPlayerData
{
    public string socketId;
    public string username;
    public string characterClass;
    public PositionData position;
    public HealthData health;
    public HealthData mana;
}

[Serializable]
public class PositionData
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class HealthData
{
    public int current;
    public int max;
}
