using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

/// <summary>
/// Менеджер синхронизации мультиплеера
/// Управляет всеми сетевыми игроками, обрабатывает события от сервера
/// </summary>
public class NetworkSyncManager : MonoBehaviour
{
    public static NetworkSyncManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float positionSyncInterval = 0.05f; // 20 Hz для плавного движения (как в MMO)
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
    private string lastAnimationState = "Idle";

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

        // Subscribe to WebSocket events FIRST
        SubscribeToNetworkEvents();

        // ВАЖНО: Запрашиваем список игроков в комнате ПОСЛЕ подписки
        // Потому что мы могли пропустить событие room_players если оно пришло до загрузки ArenaScene
        Debug.Log("[NetworkSync] 🔄 Запрашиваем список игроков в комнате...");
        if (SocketIOManager.Instance != null)
        {
            SocketIOManager.Instance.RequestRoomPlayers();
        }
    }


    void Update()
    {
        if (!syncEnabled)
            return;

        // Проверяем подключение перед отправкой
        if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
            return;

        // Send local player position to server
        if (Time.time - lastPositionSync > positionSyncInterval)
        {
            SyncLocalPlayerPosition();
            SyncLocalPlayerAnimation();  // ВАЖНО: Синхронизируем анимацию
            lastPositionSync = Time.time;
        }
    }

    /// <summary>
    /// Подписаться на сетевые события
    /// </summary>
    private void SubscribeToNetworkEvents()
    {
        if (SocketIOManager.Instance == null)
        {
            Debug.LogError("[NetworkSync] SocketIOManager не найден!");
            return;
        }

        // Room players list (when we join)
        SocketIOManager.Instance.On("room_players", OnRoomPlayers);
        SocketIOManager.Instance.On("player_joined", OnPlayerJoined);
        SocketIOManager.Instance.On("player_left", OnPlayerLeft);
        SocketIOManager.Instance.On("player_moved", OnPlayerMoved);
        SocketIOManager.Instance.On("player_animation_changed", OnAnimationChanged); // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: теперь совпадает с сервером!
        SocketIOManager.Instance.On("player_attacked", OnPlayerAttacked);
        SocketIOManager.Instance.On("player_health_changed", OnHealthChanged);
        SocketIOManager.Instance.On("player_died", OnPlayerDied);
        SocketIOManager.Instance.On("player_respawned", OnPlayerRespawned);

        // Enemy events
        SocketIOManager.Instance.On("enemy_health_changed", OnEnemyHealthChanged);
        SocketIOManager.Instance.On("enemy_damaged_by_server", OnEnemyDamagedByServer);
        SocketIOManager.Instance.On("enemy_died", OnEnemyDied);
        SocketIOManager.Instance.On("enemy_respawned", OnEnemyRespawned);

        Debug.Log("[NetworkSync] ✅ Подписан на сетевые события");
    }

    /// <summary>
    /// Установить локального игрока
    /// </summary>
    public void SetLocalPlayer(GameObject player, string characterClass)
    {
        localPlayer = player;
        localPlayerClass = characterClass;
        Debug.Log($"[NetworkSync] ✅ Локальный игрок установлен: {characterClass}");
        Debug.Log($"[NetworkSync] 🔍 localPlayer: {(localPlayer != null ? localPlayer.name : "NULL")}");
        Debug.Log($"[NetworkSync] 🔍 SocketIOManager.Instance: {(SocketIOManager.Instance != null ? "EXISTS" : "NULL")}");
        Debug.Log($"[NetworkSync] 🔍 SocketIOManager.IsConnected: {(SocketIOManager.Instance != null ? SocketIOManager.Instance.IsConnected.ToString() : "N/A")}");
        Debug.Log($"[NetworkSync] 🔍 syncEnabled: {syncEnabled}");
    }

    /// <summary>
    /// Синхронизировать позицию локального игрока
    /// </summary>
    private void SyncLocalPlayerPosition()
    {
        if (localPlayer == null || SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
            return;

        // Get velocity и позицию
        Vector3 velocity = Vector3.zero;
        bool isGrounded = true;
        Vector3 position = localPlayer.transform.position;
        Quaternion rotation = localPlayer.transform.rotation;

        var controller = localPlayer.GetComponent<CharacterController>();
        if (controller != null)
        {
            velocity = controller.velocity;
            isGrounded = controller.isGrounded;
        }
        else
        {
            var rigidbody = localPlayer.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                velocity = rigidbody.linearVelocity;
            }
        }

        // ДИАГНОСТИКА: Логируем каждую 60-ю отправку (1 раз в секунду при 20Hz)
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[NetworkSync] 📤 Отправка позиции: pos=({position.x:F1}, {position.y:F1}, {position.z:F1}), vel=({velocity.x:F1}, {velocity.y:F1}, {velocity.z:F1}), rot={rotation.eulerAngles.y:F0}°");
        }

        // Send to server
        SocketIOManager.Instance.UpdatePosition(position, rotation, velocity, isGrounded);
    }

    /// <summary>
    /// Синхронизировать анимацию локального игрока
    /// </summary>
    private void SyncLocalPlayerAnimation()
    {
        if (localPlayer == null)
        {
            Debug.LogWarning("[NetworkSync] ⚠️ SyncLocalPlayerAnimation: localPlayer == NULL!");
            return;
        }

        if (SocketIOManager.Instance == null)
        {
            Debug.LogWarning("[NetworkSync] ⚠️ SyncLocalPlayerAnimation: SocketIOManager.Instance == NULL!");
            return;
        }

        if (!SocketIOManager.Instance.IsConnected)
        {
            // Не спамим если не подключены
            return;
        }

        string currentState = GetLocalPlayerAnimationState();

        // ВАЖНО: Отправляем анимацию КАЖДЫЙ раз для real-time синхронизации
        // Сервер сам решит нужно ли рассылать всем (если изменилась)
        bool stateChanged = (currentState != lastAnimationState);

        if (stateChanged)
        {
            Debug.Log($"[NetworkSync] 🎬 Анимация изменилась: {lastAnimationState} → {currentState}");
            lastAnimationState = currentState;
        }

        // ДИАГНОСТИКА: Логируем КАЖДУЮ отправку анимации
        Debug.Log($"[NetworkSync] 📤 Отправка анимации на сервер: {currentState} (changed={stateChanged})");

        SocketIOManager.Instance.UpdateAnimation(currentState);
    }

    /// <summary>
    /// Получить текущее состояние анимации локального игрока
    /// </summary>
    private string GetLocalPlayerAnimationState()
    {
        if (localPlayer == null) return "Idle";

        // ВАЖНО: Animator может быть на самом объекте или в дочернем Model
        var animator = localPlayer.GetComponent<Animator>();
        if (animator == null)
        {
            animator = localPlayer.GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            Debug.LogWarning("[NetworkSync] ⚠️ Animator не найден для локального игрока!");
            return "Idle";
        }

        // ВАЖНО: PlayerController использует Blend Tree с MoveX/MoveY/IsMoving
        // А не простые bool параметры isWalking/isRunning

        // Проверяем на атаку (триггер)
        if (HasParameter(animator, "isAttacking") && animator.GetBool("isAttacking"))
            return "Attacking";

        if (HasParameter(animator, "isDead") && animator.GetBool("isDead"))
            return "Dead";

        // PlayerController использует IsMoving (bool) и MoveY (float)
        bool isMoving = HasParameter(animator, "IsMoving") && animator.GetBool("IsMoving");

        if (isMoving)
        {
            // MoveY определяет скорость: 0.5 = Walking, 1.0 = Running
            if (HasParameter(animator, "MoveY"))
            {
                float moveY = animator.GetFloat("MoveY");

                // ДИАГНОСТИКА: Логируем параметры каждую секунду
                if (Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[NetworkSync] 🎭 Animator parameters: IsMoving={isMoving}, MoveY={moveY:F2}");
                }

                // MoveY > 0.7 = Running, иначе Walking
                return moveY > 0.7f ? "Running" : "Walking";
            }
            else
            {
                // Fallback: если нет MoveY, считаем что Walking
                return "Walking";
            }
        }

        return "Idle";
    }

    /// <summary>
    /// Проверить есть ли параметр в Animator
    /// </summary>
    private bool HasParameter(Animator animator, string paramName)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    // ===== NETWORK EVENT HANDLERS =====

    /// <summary>
    /// Обработать список игроков в комнате (когда мы входим)
    /// </summary>
    private void OnRoomPlayers(string jsonData)
    {
        Debug.Log($"[NetworkSync] 📦 Получен список игроков в комнате. JSON: {jsonData}");

        try
        {
            var data = JsonConvert.DeserializeObject<RoomPlayersResponse>(jsonData);

            if (data == null || data.players == null)
            {
                Debug.LogError("[NetworkSync] ❌ Failed to parse RoomPlayersResponse");
                return;
            }

            Debug.Log($"[NetworkSync] В комнате {data.players.Length} игроков");
            Debug.Log($"[NetworkSync] Мой socketId: {data.yourSocketId}");

            // Spawn all existing players
            foreach (var playerData in data.players)
            {
                Debug.Log($"[NetworkSync] Игрок: {playerData.username} (socketId: {playerData.socketId}, class: {playerData.characterClass})");

                // Skip ourselves
                if (playerData.socketId == data.yourSocketId)
                {
                    Debug.Log($"[NetworkSync] ⏭️ Это мы сами, пропускаем");
                    continue;
                }

                // Spawn if not already exists
                if (!networkPlayers.ContainsKey(playerData.socketId))
                {
                    Debug.Log($"[NetworkSync] 🎭 Spawning network player: {playerData.username}");
                    Vector3 pos = new Vector3(playerData.position.x, playerData.position.y, playerData.position.z);
                    SpawnNetworkPlayer(playerData.socketId, playerData.username, playerData.characterClass, pos);
                }
            }

            Debug.Log($"[NetworkSync] 📊 Всего сетевых игроков: {networkPlayers.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌ Error in OnRoomPlayers: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Обработать подключение нового игрока
    /// </summary>
    private void OnPlayerJoined(string jsonData)
    {
        var data = JsonConvert.DeserializeObject<PlayerJoinedEvent>(jsonData);
        Debug.Log($"[NetworkSync] Игрок подключился: {data.username} ({data.characterClass})");

        // Don't create network player for ourselves
        // SocketIOManager doesn't have SessionId, so we compare with our socket ID from room_players
        // For now, skip this check - room_players already filters us out

        // Spawn network player
        Vector3 pos = new Vector3(data.position.x, data.position.y, data.position.z);
        SpawnNetworkPlayer(data.socketId, data.username, data.characterClass, pos);
    }

    /// <summary>
    /// Обработать отключение игрока
    /// </summary>
    private void OnPlayerLeft(string jsonData)
    {
        var data = JsonConvert.DeserializeObject<PlayerLeftEvent>(jsonData);
        Debug.Log($"[NetworkSync] Игрок отключился: {data.username} ({data.socketId})");

        RemoveNetworkPlayer(data.socketId);
    }

    /// <summary>
    /// Обработать обновление позиции
    /// </summary>
    private void OnPlayerMoved(string jsonData)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<PlayerMovedEvent>(jsonData);

            if (data == null || string.IsNullOrEmpty(data.socketId))
                return;

            if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
            {
                // Проверяем что объект не уничтожен
                if (player == null || player.gameObject == null)
                {
                    networkPlayers.Remove(data.socketId);
                    return;
                }

                Vector3 pos = new Vector3(data.position.x, data.position.y, data.position.z);
                Quaternion rot = Quaternion.Euler(data.rotation.x, data.rotation.y, data.rotation.z);

                // ВАЖНО: velocity может быть null если сервер не отправил
                Vector3 vel = Vector3.zero;
                if (data.velocity != null)
                {
                    vel = new Vector3(data.velocity.x, data.velocity.y, data.velocity.z);
                }

                float timestamp = data.timestamp > 0 ? (data.timestamp / 1000f) : Time.time;

                player.UpdatePosition(pos, rot, vel, timestamp);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌ Error in OnPlayerMoved: {ex.Message}");
        }
    }

    /// <summary>
    /// Обработать изменение анимации
    /// </summary>
    private void OnAnimationChanged(string jsonData)
    {
        try
        {
            Debug.Log($"[NetworkSync] 📥 RAW animation data: {jsonData}");

            var data = JsonConvert.DeserializeObject<AnimationChangedEvent>(jsonData);

            if (data == null)
            {
                Debug.LogError($"[NetworkSync] ❌ Failed to deserialize animation data!");
                return;
            }

            Debug.Log($"[NetworkSync] 📥 Получена анимация от сервера: socketId={data.socketId}, animation={data.animation}");

            // Skip our own updates - server should not send us our own animation

            if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
            {
                // ВАЖНО: Проверяем что объект не уничтожен
                if (player == null || player.gameObject == null)
                {
                    Debug.LogWarning($"[NetworkSync] ⚠️ Player {data.socketId} is destroyed (animation), removing from dictionary");
                    networkPlayers.Remove(data.socketId);
                    return;
                }

                player.UpdateAnimation(data.animation);
            }
            else
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ Получена анимация для несуществующего игрока: {data.socketId}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌ Error in OnAnimationChanged: {ex.Message}\nJSON: {jsonData}");
        }
    }

    /// <summary>
    /// Обработать атаку игрока
    /// </summary>
    private void OnPlayerAttacked(string jsonData)
    {
        var data = JsonUtility.FromJson<PlayerAttackedEvent>(jsonData);
        Debug.Log($"[NetworkSync] ⚔️ Атака: {data.socketId}, тип: {data.attackType}, цель: {data.targetType} {data.targetId}");

        // Play attack animation on attacker (if it's a network player)
        if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer attacker))
        {
            attacker.PlayAttackAnimation(data.attackType);
        }

        // If target is a player and it's us, apply damage
        // Note: We need to track our socket ID from room_players event
        // For now, server handles damage logic
    }

    /// <summary>
    /// Обработать обновление здоровья игрока (SERVER AUTHORITY)
    /// </summary>
    private void OnHealthChanged(string jsonData)
    {
        var data = JsonUtility.FromJson<HealthChangedEvent>(jsonData);
        string critText = data.isCritical ? " 💥 КРИТИЧЕСКИЙ УДАР!" : "";
        Debug.Log($"[NetworkSync] 💔 Здоровье игрока {data.socketId}: {data.currentHealth}/{data.maxHealth}{critText}");

        if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
        {
            player.ShowDamage(data.damage);
            // TODO: Update health bar
        }

        // TODO: Если это мы получили урон, применить к локальному игроку
        // Сервер должен отправить специальное событие для локального игрока
    }

    /// <summary>
    /// Обработать смерть игрока
    /// </summary>
    private void OnPlayerDied(string jsonData)
    {
        var data = JsonUtility.FromJson<PlayerDiedEvent>(jsonData);
        Debug.Log($"[NetworkSync] ☠️ Игрок погиб: {data.socketId}, Убийца: {data.killerId}");

        // Check if it's a network player
        if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
        {
            // Play death animation
            player.UpdateAnimation("Dead");
        }
        // If not in networkPlayers, it might be us - handle in HealthSystem
    }

    /// <summary>
    /// Обработать респавн игрока
    /// </summary>
    private void OnPlayerRespawned(string jsonData)
    {
        var data = JsonUtility.FromJson<PlayerRespawnedEvent>(jsonData);
        Debug.Log($"[NetworkSync] 🔄 Игрок возродился: {data.socketId}");

        if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
        {
            Vector3 spawnPos = new Vector3(data.position.x, data.position.y, data.position.z);
            player.OnRespawn(spawnPos);
        }
    }

    /// <summary>
    /// Обработать изменение здоровья врага
    /// </summary>
    private void OnEnemyHealthChanged(string jsonData)
    {
        var data = JsonUtility.FromJson<EnemyHealthChangedEvent>(jsonData);
        Debug.Log($"[NetworkSync] 🐺 Враг {data.enemyId} получил урон: {data.damage}, здоровье: {data.currentHealth}");

        // TODO: Find enemy by ID and update its health
        // This will be implemented when we have enemy manager
    }

    /// <summary>
    /// Обработать урон врага от сервера (SERVER AUTHORITY)
    /// Сервер рассчитал урон на основе SPECIAL статов атакующего
    /// </summary>
    private void OnEnemyDamagedByServer(string jsonData)
    {
        var data = JsonUtility.FromJson<EnemyDamagedByServerEvent>(jsonData);
        Debug.Log($"[NetworkSync] 🎯 Сервер нанёс урон врагу {data.enemyId}: {data.damage} урона{(data.isCritical ? " (КРИТ!)" : "")}");

        // Найти врага по ID и применить урон
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemyObj in enemies)
        {
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy != null && enemy.GetEnemyId() == data.enemyId)
            {
                // Применяем урон к врагу
                enemy.TakeDamage(data.damage);
                Debug.Log($"[NetworkSync] ✅ Применён серверный урон к {enemy.GetEnemyName()}: {data.damage}{(data.isCritical ? " КРИТИЧЕСКИЙ" : "")}");
                return;
            }
        }

        Debug.LogWarning($"[NetworkSync] ⚠️ Враг {data.enemyId} не найден для применения серверного урона");
    }

    /// <summary>
    /// Обработать смерть врага
    /// </summary>
    private void OnEnemyDied(string jsonData)
    {
        var data = JsonUtility.FromJson<EnemyDiedEvent>(jsonData);
        Debug.Log($"[NetworkSync] 💀 Враг {data.enemyId} убит игроком {data.killerUsername}");

        // TODO: Find enemy by ID and play death animation
        // This will be implemented when we have enemy manager
    }

    /// <summary>
    /// Обработать респавн врага
    /// </summary>
    private void OnEnemyRespawned(string jsonData)
    {
        var data = JsonUtility.FromJson<EnemyRespawnedEvent>(jsonData);
        Debug.Log($"[NetworkSync] 🔄 Враг {data.enemyId} ({data.enemyType}) возродился");

        // TODO: Respawn enemy at position
        // This will be implemented when we have enemy manager
    }


    // ===== NETWORK PLAYER MANAGEMENT =====

    /// <summary>
    /// Создать сетевого игрока
    /// </summary>
    private void SpawnNetworkPlayer(string socketId, string username, string characterClass, Vector3 position)
    {
        GameObject prefab = GetCharacterPrefab(characterClass);
        if (prefab == null)
        {
            Debug.LogError($"[NetworkSync] Префаб для класса {characterClass} не найден!");
            return;
        }

        GameObject playerObj = Instantiate(prefab, position, Quaternion.identity);
        playerObj.name = $"NetworkPlayer_{username}";
        playerObj.layer = LayerMask.NameToLayer("Character");

        // ВАЖНО: Найти модель внутри префаба
        Transform modelTransform = playerObj.transform.Find("Model") ?? playerObj.transform;

        // ВАЖНО: Отключить ненужные компоненты для сетевого игрока
        // Ищем PlayerController на ВСЕХ уровнях (root, model, children)
        PlayerController[] allPlayerControllers = playerObj.GetComponentsInChildren<PlayerController>(true);
        foreach (var pc in allPlayerControllers)
        {
            pc.enabled = false;
            Debug.Log($"[NetworkSync] ✅ Отключен PlayerController на {pc.gameObject.name} для {username}");
        }

        // Отключаем PlayerAttack чтобы NetworkPlayer не атаковал локально
        PlayerAttack[] allPlayerAttacks = playerObj.GetComponentsInChildren<PlayerAttack>(true);
        foreach (var pa in allPlayerAttacks)
        {
            pa.enabled = false;
            Debug.Log($"[NetworkSync] ✅ Отключен PlayerAttack на {pa.gameObject.name} для {username}");
        }

        // Отключаем TargetSystem чтобы NetworkPlayer не таргетил
        TargetSystem[] allTargetSystems = playerObj.GetComponentsInChildren<TargetSystem>(true);
        foreach (var ts in allTargetSystems)
        {
            ts.enabled = false;
            Debug.Log($"[NetworkSync] ✅ Отключен TargetSystem на {ts.gameObject.name} для {username}");
        }

        // Отключаем локальные input компоненты
        var cameraController = playerObj.GetComponentInChildren<Camera>();
        if (cameraController != null)
        {
            cameraController.gameObject.SetActive(false);
            Debug.Log($"[NetworkSync] ✅ Отключена камера для {username}");
        }

        // Отключаем CharacterController (NetworkPlayer управляется через NetworkTransform)
        CharacterController[] allCharControllers = playerObj.GetComponentsInChildren<CharacterController>(true);
        foreach (var cc in allCharControllers)
        {
            cc.enabled = false;
            Debug.Log($"[NetworkSync] ✅ Отключен CharacterController на {cc.gameObject.name} для {username}");
        }

        // ВАЖНО: Настроить оружие из WeaponDatabase
        SetupNetworkPlayerWeapon(modelTransform, characterClass);

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

        // ВАЖНО: Добавляем компонент Enemy для системы таргетинга и тумана войны
        SetupNetworkPlayerAsEnemy(playerObj, username);

        networkPlayers[socketId] = networkPlayer;

        Debug.Log($"[NetworkSync] ✅ Создан сетевой игрок: {username} ({characterClass}) - враг для таргетинга");
    }

    /// <summary>
    /// Настроить оружие для сетевого игрока
    /// </summary>
    private void SetupNetworkPlayerWeapon(Transform modelTransform, string characterClass)
    {
        // Найти или добавить ClassWeaponManager
        var weaponManager = modelTransform.GetComponent<ClassWeaponManager>();
        if (weaponManager == null)
        {
            weaponManager = modelTransform.gameObject.AddComponent<ClassWeaponManager>();
            Debug.Log($"[NetworkSync] Добавлен ClassWeaponManager для {characterClass}");
        }

        // ВАЖНО: Устанавливаем класс персонажа вручную, чтобы не было миллионов логов
        var characterClassEnum = (CharacterClass)System.Enum.Parse(typeof(CharacterClass), characterClass);
        weaponManager.SetCharacterClass(characterClassEnum);
        Debug.Log($"[NetworkSync] Установлен класс {characterClass} для сетевого игрока");

        // Загрузить WeaponDatabase
        var weaponDatabase = Resources.Load<WeaponDatabase>("WeaponDatabase");
        if (weaponDatabase != null)
        {
            // Вызываем метод через рефлексию или делаем публичным
            var method = typeof(ClassWeaponManager).GetMethod("AttachWeaponForClass", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(weaponManager, null);
                Debug.Log($"[NetworkSync] ✅ Оружие прикреплено для {characterClass}");
            }
            else
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ Не найден метод AttachWeaponForClass");
            }
        }
        else
        {
            Debug.LogWarning($"[NetworkSync] ⚠️ WeaponDatabase не найдена в Resources");
        }
    }

    /// <summary>
    /// Настроить сетевого игрока как Enemy (для таргетинга и тумана войны)
    /// </summary>
    private void SetupNetworkPlayerAsEnemy(GameObject playerObj, string username)
    {
        // Добавляем компонент Enemy
        Enemy enemyComponent = playerObj.AddComponent<Enemy>();

        // Используем reflection для установки приватных полей
        var enemyNameField = typeof(Enemy).GetField("enemyName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (enemyNameField != null)
        {
            enemyNameField.SetValue(enemyComponent, username);
        }

        var maxHealthField = typeof(Enemy).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (maxHealthField != null)
        {
            maxHealthField.SetValue(enemyComponent, 10000f); // ОЧЕНЬ ВЫСОКОЕ HP - сетевые игроки бессмертные
        }

        // ВАЖНО: Также установить currentHealth чтобы избежать отрицательного HP
        var currentHealthField = typeof(Enemy).GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (currentHealthField != null)
        {
            currentHealthField.SetValue(enemyComponent, 10000f); // Инициализируем currentHealth = maxHealth
            Debug.Log($"[NetworkSync] ✅ currentHealth установлено: 10000");
        }

        // ВАЖНО: Отключаем Enemy компонент чтобы он не вызывал TakeDamage/Die
        // Оставляем только для таргетинга и регистрации в FogOfWar
        enemyComponent.enabled = false;
        Debug.Log($"[NetworkSync] ⚠️ Enemy компонент ОТКЛЮЧЁН (только для таргетинга)");

        // ВАЖНО: Установить тег "Enemy" для системы таргетинга
        if (!playerObj.CompareTag("Enemy"))
        {
            try
            {
                playerObj.tag = "Enemy";
                Debug.Log($"[NetworkSync] ✅ Установлен тег 'Enemy' для {username}");
            }
            catch (UnityException ex)
            {
                Debug.LogError($"[NetworkSync] ❌ Не удалось установить тег 'Enemy': {ex.Message}. Создайте тег 'Enemy' в Project Settings → Tags and Layers!");
            }
        }

        // Зарегистрировать в FogOfWar системе локального игрока
        if (localPlayer != null)
        {
            FogOfWar fogOfWar = localPlayer.GetComponent<FogOfWar>();
            if (fogOfWar != null)
            {
                fogOfWar.RegisterEnemy(enemyComponent);
                Debug.Log($"[NetworkSync] ✅ Сетевой игрок {username} зарегистрирован в FogOfWar");
            }
        }

        Debug.Log($"[NetworkSync] ✅ Сетевой игрок {username} настроен как Enemy (можно таргетить)");
    }

    /// <summary>
    /// Удалить сетевого игрока
    /// </summary>
    private void RemoveNetworkPlayer(string socketId)
    {
        if (networkPlayers.TryGetValue(socketId, out NetworkPlayer player))
        {
            // Отрегистрируем из FogOfWar перед удалением
            Enemy enemyComponent = player.GetComponent<Enemy>();
            if (enemyComponent != null && localPlayer != null)
            {
                FogOfWar fogOfWar = localPlayer.GetComponent<FogOfWar>();
                if (fogOfWar != null)
                {
                    fogOfWar.UnregisterEnemy(enemyComponent);
                }
            }

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
        if (localPlayer != null && spawnPoints != null && spawnPoints.Length > 0)
        {
            // Choose random spawn point
            Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;

            // TODO: Add SendRespawn method to SocketIOManager
            // SocketIOManager.Instance.SendRespawn(spawnPos);
            Debug.LogWarning("[NetworkSync] SendRespawn not yet implemented in SocketIOManager");
        }
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
        // Note: SocketIOManager handles event cleanup internally
        // We don't need to manually unsubscribe
        Debug.Log("[NetworkSync] NetworkSyncManager destroyed");
    }
}

// ===== EVENT DATA CLASSES (matching multiplayer.js server) =====

/// <summary>
/// Response when joining a room (room_players event)
/// </summary>
[Serializable]
public class RoomPlayersResponse
{
    public RoomPlayerInfo[] players;
    public string yourSocketId;
}

[Serializable]
public class RoomPlayerInfo
{
    public string socketId;
    public string username;
    public string characterClass;
    public Vector3Data position;
    public Vector3Data rotation;
    public string animation;
    public float health;
    public float maxHealth;
}

/// <summary>
/// Player joined event
/// </summary>
[Serializable]
public class PlayerJoinedEvent
{
    public string socketId;
    public string username;
    public string characterClass;
    public Vector3Data position;
    public Vector3Data rotation;
}

/// <summary>
/// Player left event
/// </summary>
[Serializable]
public class PlayerLeftEvent
{
    public string socketId;
    public string username;
}

/// <summary>
/// Player moved event
/// </summary>
[Serializable]
public class PlayerMovedEvent
{
    public string socketId;
    public Vector3Data position;
    public Vector3Data rotation;
    public Vector3Data velocity;
    public bool isGrounded;
    public long timestamp;
}

/// <summary>
/// Animation changed event
/// </summary>
[Serializable]
public class AnimationChangedEvent
{
    public string socketId;
    public string animation;
    public float speed;
    public long timestamp;
}

/// <summary>
/// Player attacked event
/// </summary>
[Serializable]
public class PlayerAttackedEvent
{
    public string socketId;
    public string attackType;
    public string targetType;
    public string targetId;
    public float damage;
    public Vector3Data position;
    public Vector3Data direction;
    public int skillId;
    public long timestamp;
}

/// <summary>
/// Player health changed event
/// </summary>
[Serializable]
public class HealthChangedEvent
{
    public string socketId;
    public float damage;
    public float currentHealth;
    public float maxHealth;
    public string attackerId;
    public bool isCritical;
    public long timestamp;
}

/// <summary>
/// Player died event
/// </summary>
[Serializable]
public class PlayerDiedEvent
{
    public string socketId;
    public string killerId;
    public long timestamp;
}

/// <summary>
/// Player respawned event
/// </summary>
[Serializable]
public class PlayerRespawnedEvent
{
    public string socketId;
    public Vector3Data position;
    public float health;
    public long timestamp;
}

/// <summary>
/// Enemy health changed event
/// </summary>
[Serializable]
public class EnemyHealthChangedEvent
{
    public string enemyId;
    public float damage;
    public float currentHealth;
    public string attackerId;
    public long timestamp;
}

/// <summary>
/// Enemy damaged by server event (SERVER AUTHORITY)
/// </summary>
[Serializable]
public class EnemyDamagedByServerEvent
{
    public string enemyId;
    public float damage;
    public string attackerId;
    public string attackerUsername;
    public bool isCritical;
    public long timestamp;
}

/// <summary>
/// Enemy died event
/// </summary>
[Serializable]
public class EnemyDiedEvent
{
    public string enemyId;
    public string killerId;
    public string killerUsername;
    public Vector3Data position;
    public long timestamp;
}

/// <summary>
/// Enemy respawned event
/// </summary>
[Serializable]
public class EnemyRespawnedEvent
{
    public string enemyId;
    public string enemyType;
    public Vector3Data position;
    public float health;
    public long timestamp;
}

/// <summary>
/// Vector3 serializable for JSON
/// </summary>
[Serializable]
public class Vector3Data
{
    public float x;
    public float y;
    public float z;
}
