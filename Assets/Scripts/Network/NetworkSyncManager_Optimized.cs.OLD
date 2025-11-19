using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text.Json;

/// <summary>
/// ОПТИМИЗИРОВАННЫЙ Менеджер синхронизации мультиплеера
/// Управляет всеми сетевыми игроками, обрабатывает события от сервера
///
/// ОПТИМИЗАЦИИ:
/// - Использует OptimizedWebSocketClient для экономии трафика
/// - Передаёт velocity для Dead Reckoning
/// - Обрабатывает JsonElement вместо string
/// - Адаптивная частота обновлений
/// </summary>
public class NetworkSyncManager_Optimized : MonoBehaviour
{
    public static NetworkSyncManager_Optimized Instance { get; private set; }

    [Header("Settings")]
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

    // Velocity tracking для Dead Reckoning
    private Vector3 lastPosition;
    private float lastPositionTime;

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
        // Проверяем, находимся ли мы в мультиплеер режиме
        string roomId = PlayerPrefs.GetString("CurrentRoomId", "");
        if (string.IsNullOrEmpty(roomId))
        {
            Debug.Log("[NetworkSync] Не в мультиплеере, отключаем синхронизацию");
            enabled = false;
            return;
        }

        // Subscribe to WebSocket events
        SubscribeToNetworkEvents();
    }

    void Update()
    {
        if (!syncEnabled) return;

        // Проверяем подключение перед отправкой
        if (OptimizedWebSocketClient.Instance == null || !OptimizedWebSocketClient.Instance.IsConnected)
        {
            return;
        }

        // Отправка позиции теперь управляется OptimizedWebSocketClient
        // Он сам решает когда отправлять на основе delta compression
        SyncLocalPlayerPosition();
    }

    /// <summary>
    /// Подписаться на сетевые события
    /// </summary>
    private void SubscribeToNetworkEvents()
    {
        if (OptimizedWebSocketClient.Instance == null)
        {
            Debug.LogError("[NetworkSync] OptimizedWebSocketClient не найден!");
            return;
        }

        // Player joined room
        OptimizedWebSocketClient.Instance.On("player_joined", OnPlayerJoined);

        // Player left room
        OptimizedWebSocketClient.Instance.On("player_left", OnPlayerLeft);

        // Position update
        OptimizedWebSocketClient.Instance.On("position_update", OnPositionUpdate);

        // Player attacked
        OptimizedWebSocketClient.Instance.On("player_attacked", OnPlayerAttacked);

        // Player used skill
        OptimizedWebSocketClient.Instance.On("skill_used", OnSkillUsed);

        // Health update
        OptimizedWebSocketClient.Instance.On("health_update", OnHealthUpdate);

        // Player died
        OptimizedWebSocketClient.Instance.On("player_died", OnPlayerDied);

        // Player respawned
        OptimizedWebSocketClient.Instance.On("player_respawned", OnPlayerRespawned);

        // Room state (full player list)
        OptimizedWebSocketClient.Instance.On("room_state", OnRoomState);

        Debug.Log("[NetworkSync] ✅ Подписан на сетевые события");
    }

    /// <summary>
    /// Установить локального игрока
    /// </summary>
    public void SetLocalPlayer(GameObject player, string characterClass)
    {
        localPlayer = player;
        localPlayerClass = characterClass;
        lastPosition = player.transform.position;
        lastPositionTime = Time.time;
        Debug.Log($"[NetworkSync] Локальный игрок установлен: {characterClass}");
    }

    /// <summary>
    /// Синхронизировать позицию локального игрока
    /// </summary>
    private void SyncLocalPlayerPosition()
    {
        if (localPlayer == null || OptimizedWebSocketClient.Instance == null || !OptimizedWebSocketClient.Instance.IsConnected)
            return;

        // Get animation state
        string animState = GetLocalPlayerAnimationState();

        // Вычисляем velocity для Dead Reckoning
        Vector3 velocity = Vector3.zero;
        float deltaTime = Time.time - lastPositionTime;

        if (deltaTime > 0)
        {
            velocity = (localPlayer.transform.position - lastPosition) / deltaTime;
        }

        // Или получаем velocity из компонентов
        var rigidbody = localPlayer.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            velocity = rigidbody.linearVelocity;
        }
        else
        {
            var controller = localPlayer.GetComponent<CharacterController>();
            if (controller != null)
            {
                velocity = controller.velocity;
            }
        }

        // Send to server с velocity для Dead Reckoning
        OptimizedWebSocketClient.Instance.UpdatePosition(
            localPlayer.transform.position,
            localPlayer.transform.rotation,
            animState,
            velocity
        );

        // Сохраняем для следующего кадра
        lastPosition = localPlayer.transform.position;
        lastPositionTime = Time.time;
    }

    /// <summary>
    /// Получить текущее состояние анимации локального игрока
    /// </summary>
    private string GetLocalPlayerAnimationState()
    {
        var animator = localPlayer.GetComponent<Animator>();
        if (animator == null) return "Idle";

        try
        {
            if (animator.GetBool("isDead")) return "Dead";
            if (animator.GetBool("isAttacking")) return "Attacking";
            if (animator.GetBool("isRunning")) return "Running";
            if (animator.GetBool("isWalking")) return "Walking";
        }
        catch
        {
            // Параметры анимации не существуют - это нормально
        }

        return "Idle";
    }

    // ===== NETWORK EVENT HANDLERS =====

    /// <summary>
    /// Обработать подключение нового игрока
    /// </summary>
    private void OnPlayerJoined(JsonElement jsonData)
    {
        try
        {
            string socketId = jsonData.GetProperty("socketId").GetString();
            string username = jsonData.GetProperty("username").GetString();
            string characterClass = jsonData.GetProperty("characterClass").GetString();

            var positionElement = jsonData.GetProperty("position");
            float x = positionElement.GetProperty("x").GetSingle();
            float y = positionElement.GetProperty("y").GetSingle();
            float z = positionElement.GetProperty("z").GetSingle();

            Debug.Log($"[NetworkSync] Игрок подключился: {username} ({characterClass})");

            // Don't create network player for ourselves
            if (socketId == OptimizedWebSocketClient.Instance.SessionId)
            {
                Debug.Log("[NetworkSync] Это мы сами, пропускаем");
                return;
            }

            // Spawn network player
            SpawnNetworkPlayer(socketId, username, characterClass, new Vector3(x, y, z));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] Ошибка обработки player_joined: {ex.Message}");
        }
    }

    /// <summary>
    /// Обработать отключение игрока
    /// </summary>
    private void OnPlayerLeft(JsonElement jsonData)
    {
        try
        {
            string socketId = jsonData.GetProperty("socketId").GetString();
            Debug.Log($"[NetworkSync] Игрок отключился: {socketId}");

            RemoveNetworkPlayer(socketId);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] Ошибка обработки player_left: {ex.Message}");
        }
    }

    /// <summary>
    /// Обработать обновление позиции
    /// </summary>
    private void OnPositionUpdate(JsonElement jsonData)
    {
        try
        {
            string socketId = jsonData.GetProperty("socketId").GetString();

            // Skip our own updates
            if (socketId == OptimizedWebSocketClient.Instance.SessionId) return;

            if (networkPlayers.TryGetValue(socketId, out NetworkPlayer player))
            {
                // Парсим позицию (может быть null из-за delta compression)
                float x = jsonData.TryGetProperty("x", out var xProp) ? xProp.GetSingle() : player.transform.position.x;
                float y = jsonData.TryGetProperty("y", out var yProp) ? yProp.GetSingle() : player.transform.position.y;
                float z = jsonData.TryGetProperty("z", out var zProp) ? zProp.GetSingle() : player.transform.position.z;

                Vector3 pos = new Vector3(x, y, z);

                // Парсим ротацию
                float rotY = jsonData.TryGetProperty("rotY", out var rotProp) ? rotProp.GetSingle() : player.transform.rotation.eulerAngles.y;
                Quaternion rot = Quaternion.Euler(0, rotY, 0);

                // Парсим velocity для Dead Reckoning
                Vector3 velocity = Vector3.zero;
                if (jsonData.TryGetProperty("vx", out var vxProp))
                {
                    velocity.x = vxProp.GetSingle();
                    velocity.y = jsonData.TryGetProperty("vy", out var vyProp) ? vyProp.GetSingle() : 0f;
                    velocity.z = jsonData.TryGetProperty("vz", out var vzProp) ? vzProp.GetSingle() : 0f;
                }

                // Timestamp для интерполяции
                float timestamp = jsonData.TryGetProperty("t", out var tProp) ? tProp.GetSingle() : Time.time;

                // Обновляем позицию с velocity
                player.UpdatePosition(pos, rot, velocity, timestamp);

                // Обновляем анимацию если изменилась
                if (jsonData.TryGetProperty("anim", out var animProp))
                {
                    player.UpdateAnimation(animProp.GetString());
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] Ошибка обработки position_update: {ex.Message}");
        }
    }

    /// <summary>
    /// Обработать атаку игрока
    /// </summary>
    private void OnPlayerAttacked(JsonElement jsonData)
    {
        try
        {
            string attackerSocketId = jsonData.GetProperty("attackerSocketId").GetString();
            string targetSocketId = jsonData.GetProperty("targetSocketId").GetString();
            float damage = jsonData.GetProperty("damage").GetSingle();
            string attackType = jsonData.GetProperty("attackType").GetString();

            Debug.Log($"[NetworkSync] Атака: {attackerSocketId} -> {targetSocketId}, Урон: {damage}");

            // Play attack animation on attacker
            if (networkPlayers.TryGetValue(attackerSocketId, out NetworkPlayer attacker))
            {
                attacker.PlayAttackAnimation(attackType);
            }

            // Show damage on target
            if (targetSocketId == OptimizedWebSocketClient.Instance.SessionId)
            {
                // We got hit!
                ApplyDamageToLocalPlayer(damage);
            }
            else if (networkPlayers.TryGetValue(targetSocketId, out NetworkPlayer target))
            {
                target.ShowDamage(damage);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] Ошибка обработки player_attacked: {ex.Message}");
        }
    }

    /// <summary>
    /// Обработать использование скилла
    /// </summary>
    private void OnSkillUsed(JsonElement jsonData)
    {
        try
        {
            string socketId = jsonData.GetProperty("socketId").GetString();
            int skillId = jsonData.GetProperty("skillId").GetInt32();

            Debug.Log($"[NetworkSync] Скилл использован: {socketId}, Skill ID: {skillId}");

            if (networkPlayers.TryGetValue(socketId, out NetworkPlayer player))
            {
                player.PlayAttackAnimation("skill");
            }

            // TODO: Spawn skill visual effects
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] Ошибка обработки skill_used: {ex.Message}");
        }
    }

    /// <summary>
    /// Обработать обновление здоровья
    /// </summary>
    private void OnHealthUpdate(JsonElement jsonData)
    {
        try
        {
            string socketId = jsonData.GetProperty("socketId").GetString();
            int currentHP = jsonData.GetProperty("currentHP").GetInt32();
            int maxHP = jsonData.GetProperty("maxHP").GetInt32();
            int currentMP = jsonData.GetProperty("currentMP").GetInt32();
            int maxMP = jsonData.GetProperty("maxMP").GetInt32();

            if (networkPlayers.TryGetValue(socketId, out NetworkPlayer player))
            {
                player.UpdateHealth(currentHP, maxHP, currentMP, maxMP);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] Ошибка обработки health_update: {ex.Message}");
        }
    }

    /// <summary>
    /// Обработать смерть игрока
    /// </summary>
    private void OnPlayerDied(JsonElement jsonData)
    {
        try
        {
            string deadPlayerSocketId = jsonData.GetProperty("deadPlayerSocketId").GetString();
            string killerSocketId = jsonData.GetProperty("killerSocketId").GetString();

            Debug.Log($"[NetworkSync] Игрок погиб: {deadPlayerSocketId}, Убийца: {killerSocketId}");

            if (deadPlayerSocketId == OptimizedWebSocketClient.Instance.SessionId)
            {
                // We died!
                OnLocalPlayerDied();
            }
            else if (networkPlayers.TryGetValue(deadPlayerSocketId, out NetworkPlayer player))
            {
                player.UpdateHealth(0, player.MaxHP, player.CurrentMP, player.MaxMP);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] Ошибка обработки player_died: {ex.Message}");
        }
    }

    /// <summary>
    /// Обработать респавн игрока
    /// </summary>
    private void OnPlayerRespawned(JsonElement jsonData)
    {
        try
        {
            string socketId = jsonData.GetProperty("socketId").GetString();
            float spawnX = jsonData.GetProperty("spawnX").GetSingle();
            float spawnY = jsonData.GetProperty("spawnY").GetSingle();
            float spawnZ = jsonData.GetProperty("spawnZ").GetSingle();

            Debug.Log($"[NetworkSync] Игрок возродился: {socketId}");

            if (networkPlayers.TryGetValue(socketId, out NetworkPlayer player))
            {
                Vector3 spawnPos = new Vector3(spawnX, spawnY, spawnZ);
                player.OnRespawn(spawnPos);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] Ошибка обработки player_respawned: {ex.Message}");
        }
    }

    /// <summary>
    /// Обработать полное состояние комнаты (список всех игроков)
    /// </summary>
    private void OnRoomState(JsonElement jsonData)
    {
        try
        {
            var playersArray = jsonData.GetProperty("players");
            int playerCount = playersArray.GetArrayLength();

            Debug.Log($"[NetworkSync] Получено состояние комнаты: {playerCount} игроков");

            // Spawn all existing players
            foreach (var playerElement in playersArray.EnumerateArray())
            {
                string socketId = playerElement.GetProperty("socketId").GetString();
                string username = playerElement.GetProperty("username").GetString();
                string characterClass = playerElement.GetProperty("characterClass").GetString();

                var positionElement = playerElement.GetProperty("position");
                float x = positionElement.GetProperty("x").GetSingle();
                float y = positionElement.GetProperty("y").GetSingle();
                float z = positionElement.GetProperty("z").GetSingle();

                // Skip ourselves
                if (socketId == OptimizedWebSocketClient.Instance.SessionId) continue;

                // Spawn if not already exists
                if (!networkPlayers.ContainsKey(socketId))
                {
                    SpawnNetworkPlayer(socketId, username, characterClass, new Vector3(x, y, z));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] Ошибка обработки room_state: {ex.Message}");
        }
    }

    // ===== NETWORK PLAYER MANAGEMENT =====

    /// <summary>
    /// Создать сетевого игрока
    /// </summary>
    private void SpawnNetworkPlayer(string socketId, string username, string characterClass, Vector3 spawnPos)
    {
        GameObject prefab = GetCharacterPrefab(characterClass);
        if (prefab == null)
        {
            Debug.LogError($"[NetworkSync] Префаб для класса {characterClass} не найден!");
            return;
        }

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
        OptimizedWebSocketClient.Instance.RequestRespawn();
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
        if (OptimizedWebSocketClient.Instance != null)
        {
            OptimizedWebSocketClient.Instance.Off("player_joined");
            OptimizedWebSocketClient.Instance.Off("player_left");
            OptimizedWebSocketClient.Instance.Off("position_update");
            OptimizedWebSocketClient.Instance.Off("player_attacked");
            OptimizedWebSocketClient.Instance.Off("skill_used");
            OptimizedWebSocketClient.Instance.Off("health_update");
            OptimizedWebSocketClient.Instance.Off("player_died");
            OptimizedWebSocketClient.Instance.Off("player_respawned");
            OptimizedWebSocketClient.Instance.Off("room_state");
        }
    }
}
