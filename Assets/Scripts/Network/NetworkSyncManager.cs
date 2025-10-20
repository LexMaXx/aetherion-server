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
    [Tooltip("Интервал синхронизации позиций (секунды). 0.0167 = 60Hz, 0.033 = 30Hz, 0.05 = 20Hz")]
    [SerializeField] private float positionSyncInterval = 0.05f; // 20 Hz - ОПТИМАЛЬНО для баланса точности/трафика (было 60Hz = 0.0167)
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

    // Player data cache (для игроков которые еще не заспавнились)
    private Dictionary<string, RoomPlayerInfo> pendingPlayers = new Dictionary<string, RoomPlayerInfo>();

    // Local player reference
    private GameObject localPlayer;
    private string localPlayerClass;
    private string localPlayerSocketId; // КРИТИЧЕСКОЕ: Наш socketId для проверки урона
    private float lastPositionSync = 0f;
    private string lastAnimationState = "Idle";
    private Vector3 lastSentPosition = Vector3.zero; // Для проверки изменения позиции
    private Quaternion lastSentRotation = Quaternion.identity; // Для проверки изменения ротации
    private const float positionThreshold = 0.01f; // Минимальное изменение позиции для отправки (1см)
    private const float rotationThreshold = 1f; // Минимальное изменение ротации для отправки (1 градус)

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
        SocketIOManager.Instance.On("player_used_skill", OnPlayerSkillUsed); // НОВОЕ: Синхронизация скиллов (ИСПРАВЛЕНО: было player_skill_used)
        SocketIOManager.Instance.On("projectile_spawned", OnProjectileSpawned); // НОВОЕ: Синхронизация снарядов (Fireball, Lightning и т.д.)
        SocketIOManager.Instance.On("visual_effect_spawned", OnVisualEffectSpawned); // НОВОЕ: Синхронизация визуальных эффектов (взрывы, ауры, горение и т.д.)
        SocketIOManager.Instance.On("player_transformed", OnPlayerTransformed); // НОВОЕ: Синхронизация трансформации
        SocketIOManager.Instance.On("player_transformation_ended", OnPlayerTransformationEnded); // НОВОЕ: Окончание трансформации
        SocketIOManager.Instance.On("player_health_changed", OnHealthChanged);
        SocketIOManager.Instance.On("player_died", OnPlayerDied);
        SocketIOManager.Instance.On("player_respawned", OnPlayerRespawned);

        // Enemy events
        SocketIOManager.Instance.On("enemy_health_changed", OnEnemyHealthChanged);
        SocketIOManager.Instance.On("enemy_damaged_by_server", OnEnemyDamagedByServer);
        SocketIOManager.Instance.On("enemy_died", OnEnemyDied);
        SocketIOManager.Instance.On("enemy_respawned", OnEnemyRespawned);

        // LOBBY SYSTEM EVENTS (10-second wait + countdown)
        SocketIOManager.Instance.On("lobby_created", OnLobbyCreated);
        SocketIOManager.Instance.On("game_countdown", OnGameCountdown);
        SocketIOManager.Instance.On("game_start", OnGameStart);

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
    /// ОПТИМИЗИРОВАНО: отправляет ТОЛЬКО при изменении позиции/ротации
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

        // ОПТИМИЗАЦИЯ: Проверяем изменилась ли позиция/ротация достаточно сильно
        float positionDelta = Vector3.Distance(position, lastSentPosition);
        float rotationDelta = Quaternion.Angle(rotation, lastSentRotation);
        bool isMoving = velocity.sqrMagnitude > 0.01f; // Игрок движется?

        // Отправляем ТОЛЬКО если:
        // 1. Игрок движется (velocity > 0)
        // 2. ИЛИ позиция изменилась больше чем на порог (0.01m = 1см)
        // 3. ИЛИ ротация изменилась больше чем на порог (1 градус)
        if (isMoving || positionDelta > positionThreshold || rotationDelta > rotationThreshold)
        {
            // Сохраняем последнюю отправленную позицию
            lastSentPosition = position;
            lastSentRotation = rotation;

            // Send to server
            SocketIOManager.Instance.UpdatePosition(position, rotation, velocity, isGrounded);

            // ДИАГНОСТИКА: Логируем каждую 20-ю отправку (1 раз в секунду при 20Hz)
            if (Time.frameCount % 20 == 0)
            {
                Debug.Log($"[NetworkSync] 📤 Отправка позиции: pos=({position.x:F1}, {position.y:F1}, {position.z:F1}), vel=({velocity.x:F1}, {velocity.y:F1}, {velocity.z:F1}), rot={rotation.eulerAngles.y:F0}°");
            }
        }
        // Если игрок стоит на месте - НЕ отправляем (экономим трафик)
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

        // ОПТИМИЗАЦИЯ: Отправляем анимацию ТОЛЬКО когда она ИЗМЕНИЛАСЬ!
        if (currentState != lastAnimationState)
        {
            Debug.Log($"[NetworkSync] 🎬 Анимация изменилась: {lastAnimationState} → {currentState}");
            lastAnimationState = currentState;

            // Отправляем ТОЛЬКО при изменении
            SocketIOManager.Instance.UpdateAnimation(currentState);
        }
        // Если анимация не изменилась - НЕ отправляем (экономим трафик)
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
            Debug.Log($"[NetworkSync] 🎯 Мой spawnIndex от сервера: {data.yourSpawnIndex}");

            // КРИТИЧЕСКОЕ: Сохраняем наш socketId для проверки получения урона
            localPlayerSocketId = data.yourSocketId;

            // КРИТИЧЕСКОЕ: Устанавливаем индекс точки спавна в ArenaManager
            if (ArenaManager.Instance != null)
            {
                ArenaManager.Instance.SetSpawnIndex(data.yourSpawnIndex);
                Debug.Log($"[NetworkSync] ✅ Индекс точки спавна установлен в ArenaManager: {data.yourSpawnIndex}");
            }
            else
            {
                Debug.LogWarning("[NetworkSync] ⚠️ ArenaManager.Instance == null! Не могу установить spawnIndex");
            }

            // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Проверяем РЕАЛЬНО ли игра УЖЕ НАЧАЛАСЬ!
            // Игра началась = 2+ игроков И countdown УЖЕ прошел (gameStarted == true в ArenaManager)
            // Игра НЕ началась = лобби запущено или ждем игроков
            bool gameAlreadyStarted = data.players.Length >= 2 &&
                                     ArenaManager.Instance != null &&
                                     ArenaManager.Instance.IsGameStarted();

            if (gameAlreadyStarted)
            {
                Debug.Log($"[NetworkSync] 🎮 Игра УЖЕ ИДЕТ ({data.players.Length} игроков)! Спавним локального игрока сразу (JOIN EXISTING GAME)");

                // ВАЖНО: Отложенный спавн через корутину, чтобы ArenaManager.Start() успел выполниться
                StartCoroutine(SpawnLocalPlayerDelayed());
            }
            else
            {
                Debug.Log($"[NetworkSync] ⏳ Игра ещё НЕ началась (лобби или ожидание), НЕ спавним себя, ждем game_start");
            }

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

                if (gameAlreadyStarted)
                {
                    // Игра уже началась - СПАВНИМ СРАЗУ!
                    Debug.Log($"[NetworkSync] 🎬 Спавним существующего игрока {playerData.username} сразу (игра началась)");

                    // Используем spawn point по индексу
                    Vector3 spawnPos = Vector3.zero;
                    if (spawnPoints != null && playerData.spawnIndex >= 0 && playerData.spawnIndex < spawnPoints.Length)
                    {
                        spawnPos = spawnPoints[playerData.spawnIndex].position;
                    }
                    else
                    {
                        spawnPos = new Vector3(playerData.position.x, playerData.position.y, playerData.position.z);
                    }

                    SpawnNetworkPlayer(playerData.socketId, playerData.username, playerData.characterClass, spawnPos, playerData.stats);
                }
                else
                {
                    // Игра ещё не началась - добавляем в pending (ждем game_start)
                    pendingPlayers[playerData.socketId] = playerData;
                    Debug.Log($"[NetworkSync] ⏳ Игрок {playerData.username} добавлен в pending, заспавнится при game_start");
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
        Debug.Log($"[NetworkSync] 📥 RAW player_joined JSON: {jsonData}");

        var data = JsonConvert.DeserializeObject<PlayerJoinedEvent>(jsonData);
        Debug.Log($"[NetworkSync] Игрок подключился: {data.username} ({data.characterClass}), socketId={data.socketId}");

        // Don't create network player for ourselves
        // SocketIOManager doesn't have SessionId, so we compare with our socket ID from room_players
        // For now, skip this check - room_players already filters us out

        // КРИТИЧЕСКОЕ: НЕ спавним сразу - ждем первого player_moved с реальной позицией
        // Сохраняем данные в pending (как в CS:GO/Dota)
        RoomPlayerInfo playerInfo = new RoomPlayerInfo
        {
            socketId = data.socketId,
            username = data.username,
            characterClass = data.characterClass,
            spawnIndex = data.spawnIndex,
            position = new Vector3Data { x = 0, y = 0, z = 0 }, // Позиция пока неизвестна
            stats = data.stats // КРИТИЧЕСКОЕ: Сохраняем SPECIAL характеристики!
        };

        pendingPlayers[data.socketId] = playerInfo;
        Debug.Log($"[NetworkSync] ⏳ Игрок {data.username} добавлен в pending по ключу socketId={data.socketId} (STR={data.stats?.strength ?? 5}), ждем game_start...");

        // КРИТИЧЕСКОЕ: Запускаем лобби для второго+ игрока, НО НЕ СПАВНИМ ЕГО СРАЗУ!
        // Сервер должен отправлять lobby_created, но если не отправил - запускаем сами
        // ВАЖНО: +1 для локального игрока (мы сами), который НЕ в networkPlayers!
        int totalPlayers = networkPlayers.Count + pendingPlayers.Count + 1;
        Debug.Log($"[NetworkSync] 👥 Всего игроков в комнате: {totalPlayers} (network={networkPlayers.Count}, pending={pendingPlayers.Count}, local=1)");

        if (totalPlayers >= 2 && ArenaManager.Instance != null)
        {
            // Проверяем, уже запущено ли лобби
            var lobbyUI = GameObject.Find("LobbyUI");
            if (lobbyUI == null)
            {
                Debug.Log($"[NetworkSync] 🏁 FALLBACK: Запускаем лобби локально (всего игроков: {totalPlayers})");
                ArenaManager.Instance.OnLobbyStarted(20000); // 20 секунд
            }
            else
            {
                Debug.Log($"[NetworkSync] ⏭️ LobbyUI уже существует, не запускаем повторно");
            }
        }
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
            // ДИАГНОСТИКА: Логируем ВСЕ player_moved события (ОТКЛЮЧЕНО для производительности)
            // Debug.Log($"[NetworkSync] 📥 RAW position data: {jsonData}");

            var data = JsonConvert.DeserializeObject<PlayerMovedEvent>(jsonData);

            if (data == null || string.IsNullOrEmpty(data.socketId))
                return;

            Vector3 pos = new Vector3(data.position.x, data.position.y, data.position.z);
            Quaternion rot = Quaternion.Euler(data.rotation.x, data.rotation.y, data.rotation.z);
            Vector3 vel = Vector3.zero;
            if (data.velocity != null)
            {
                vel = new Vector3(data.velocity.x, data.velocity.y, data.velocity.z);
            }

            // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: НЕ СПАВНИМ pending игроков по player_moved!
            // Они должны заспавниться ТОЛЬКО по событию game_start (после лобби и countdown)
            // Раньше здесь был код который спавнил при первом player_moved - это НЕПРАВИЛЬНО!

            if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
            {
                // Проверяем что объект не уничтожен
                if (player == null || player.gameObject == null)
                {
                    networkPlayers.Remove(data.socketId);
                    return;
                }

                float timestamp = data.timestamp > 0 ? (data.timestamp / 1000f) : Time.time;
                player.UpdatePosition(pos, rot, vel, timestamp);
            }
            else
            {
                // Игрок не найден и не в pending - это странно, но может произойти
                Debug.LogWarning($"[NetworkSync] ⚠️ player_moved для неизвестного игрока {data.socketId}");
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
        Debug.Log($"[NetworkSync] ⚔️ RAW player_attacked JSON: {jsonData}");

        try
        {
            var data = JsonUtility.FromJson<PlayerAttackedEvent>(jsonData);
            Debug.Log($"[NetworkSync] ⚔️ Атака получена: socketId={data.socketId}, attackType={data.attackType}, targetType={data.targetType}, targetId={data.targetId}");

            // Play attack animation on attacker (if it's a network player)
            if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer attacker))
            {
                Debug.Log($"[NetworkSync] ⚔️ Проигрываем анимацию атаки для {attacker.username}, тип: {data.attackType}");
                attacker.PlayAttackAnimation(data.attackType);
                Debug.Log($"[NetworkSync] ✅ Анимация атаки применена для {attacker.username}");
            }
            else
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ Атакующий игрок {data.socketId} НЕ НАЙДЕН в networkPlayers! Всего игроков: {networkPlayers.Count}");
                foreach (var kvp in networkPlayers)
                {
                    Debug.Log($"[NetworkSync]    - {kvp.Key}: {kvp.Value.username}");
                }
            }

            // If target is a player and it's us, apply damage
            // Note: We need to track our socket ID from room_players event
            // For now, server handles damage logic
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌ Ошибка в OnPlayerAttacked: {ex.Message}\nJSON: {jsonData}");
        }
    }

    /// <summary>
    /// Обработать использование скилла игроком (ПЕРЕРАБОТАНО)
    /// Теперь показывает только визуальные эффекты (анимация + снаряды)
    /// Урон/логика обрабатывается через отдельные события (player_damaged и т.д.)
    /// </summary>
    private void OnPlayerSkillUsed(string jsonData)
    {
        Debug.Log($"[NetworkSync] ⚡ RAW player_used_skill JSON: {jsonData}");

        try
        {
            var data = JsonConvert.DeserializeObject<PlayerSkillUsedEvent>(jsonData);
            Debug.Log($"[NetworkSync] ⚡ Скилл получен: socketId={data.socketId}, skillId={data.skillId}, animationTrigger={data.animationTrigger}");

            // Skip if it's our own skill (we already executed it locally)
            if (data.socketId == localPlayerSocketId)
            {
                Debug.Log($"[NetworkSync] ⏭️ Это наш собственный скилл, пропускаем");
                return;
            }

            // Find the network player who used the skill
            if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
            {
                Debug.Log($"[NetworkSync] ⚡ Показываем визуальные эффекты скилла {data.skillId} для {player.username}");

                // Get the skill from SkillDatabase
                SkillDatabase db = SkillDatabase.Instance;
                if (db == null)
                {
                    Debug.LogError($"[NetworkSync] ❌ SkillDatabase.Instance == null!");
                    return;
                }

                SkillData skill = db.GetSkillById(data.skillId);
                if (skill == null)
                {
                    Debug.LogWarning($"[NetworkSync] ⚠️ Скилл с ID {data.skillId} не найден в SkillDatabase");
                    return;
                }

                // 1. ПРОИГРЫВАЕМ АНИМАЦИЮ КАСТА
                Animator animator = player.GetComponentInChildren<Animator>();
                if (animator != null && !string.IsNullOrEmpty(data.animationTrigger))
                {
                    animator.SetTrigger(data.animationTrigger);
                    if (data.animationSpeed > 0)
                    {
                        animator.speed = data.animationSpeed;
                    }
                    Debug.Log($"[NetworkSync] 🎬 Анимация '{data.animationTrigger}' запущена для {player.username}");
                }

                // 2. СОЗДАЁМ СНАРЯД (если есть)
                if (skill.projectilePrefab != null)
                {
                    // Определяем целевую позицию
                    Vector3 targetPosition = data.targetPosition != null
                        ? new Vector3(data.targetPosition.x, data.targetPosition.y, data.targetPosition.z)
                        : player.transform.position + player.transform.forward * 10f;

                    // Запускаем корутину для создания снаряда после анимации
                    player.StartCoroutine(SpawnSkillProjectile(player, skill, targetPosition, data.castTime));
                }

                // 3. ВИЗУАЛЬНЫЙ ЭФФЕКТ КАСТА (если есть)
                if (skill.visualEffectPrefab != null)
                {
                    Instantiate(skill.visualEffectPrefab, player.transform.position, Quaternion.identity);
                    Debug.Log($"[NetworkSync] ✨ Визуальный эффект создан для {skill.skillName}");
                }

                // 4. ЗВУК КАСТА (если есть)
                if (skill.castSound != null)
                {
                    AudioSource.PlayClipAtPoint(skill.castSound, player.transform.position);
                }

                Debug.Log($"[NetworkSync] ✅ Визуальные эффекты скилла {skill.skillName} показаны для {player.username}");
            }
            else
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ Network player {data.socketId} не найден для применения скилла");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌ Ошибка в OnPlayerSkillUsed: {ex.Message}\nJSON: {jsonData}");
        }
    }

    /// <summary>
    /// Создать снаряд для скилла с задержкой (для анимации каста)
    /// </summary>
    private System.Collections.IEnumerator SpawnSkillProjectile(NetworkPlayer player, SkillData skill, Vector3 targetPosition, float delay)
    {
        // Ждём завершения анимации каста
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        // Проверяем что игрок ещё жив
        if (player == null || player.gameObject == null)
        {
            Debug.LogWarning($"[NetworkSync] ⚠️ NetworkPlayer уничтожен до создания снаряда");
            yield break;
        }

        // Создаём снаряд в позиции игрока
        Vector3 spawnPos = player.transform.position + Vector3.up * 1.5f + player.transform.forward * 0.5f;
        Vector3 direction = (targetPosition - spawnPos).normalized;

        GameObject projectileObj = Instantiate(skill.projectilePrefab, spawnPos, Quaternion.LookRotation(direction));

        // Настраиваем снаряд
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            // ВАЖНО: Для сетевого игрока снаряд ЧИСТО ВИЗУАЛЬНЫЙ (урон = 0, owner = NetworkPlayer)
            projectile.Initialize(null, 0f, direction, player.gameObject);
            Debug.Log($"[NetworkSync] 🚀 Снаряд {skill.projectilePrefab.name} создан для {player.username}");
        }
        else
        {
            Debug.LogWarning($"[NetworkSync] ⚠️ У префаба {skill.projectilePrefab.name} нет компонента Projectile!");
        }
    }

    /// <summary>
    /// Обработать создание снаряда (НОВОЕ - для синхронизации Fireball, Lightning и т.д.)
    /// </summary>
    private void OnProjectileSpawned(string jsonData)
    {
        Debug.Log($"[NetworkSync] 🚀 RAW projectile_spawned JSON: {jsonData}");

        try
        {
            var data = JsonConvert.DeserializeObject<ProjectileSpawnedEvent>(jsonData);
            Debug.Log($"[NetworkSync] 🚀 Снаряд получен: socketId={data.socketId}, skillId={data.skillId}");

            // Skip if it's our own projectile (we already created it locally)
            if (data.socketId == localPlayerSocketId)
            {
                Debug.Log($"[NetworkSync] ⏭️ Это наш собственный снаряд, пропускаем");
                return;
            }

            // Find the network player who spawned the projectile
            if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
            {
                Debug.Log($"[NetworkSync] 🚀 Создаём снаряд для {player.username}");

                // Определяем префаб снаряда
                GameObject projectilePrefab = null;
                string projectileName = "";

                if (data.skillId == 0)
                {
                    // skillId = 0 означает обычную атаку (не скилл)
                    // Определяем префаб по классу персонажа
                    string className = player.characterClass;
                    projectileName = GetProjectilePrefabNameByClass(className);

                    if (!string.IsNullOrEmpty(projectileName))
                    {
                        projectilePrefab = Resources.Load<GameObject>($"Projectiles/{projectileName}");
                        Debug.Log($"[NetworkSync] 📦 Обычная атака {className}: загружаем {projectileName}");
                    }
                }
                else
                {
                    // Это скилл - загружаем из SkillDatabase
                    SkillDatabase db = SkillDatabase.Instance;
                    if (db == null)
                    {
                        Debug.LogError($"[NetworkSync] ❌ SkillDatabase.Instance == null!");
                        return;
                    }

                    SkillData skill = db.GetSkillById(data.skillId);
                    if (skill == null)
                    {
                        Debug.LogWarning($"[NetworkSync] ⚠️ Скилл с ID {data.skillId} не найден в SkillDatabase");
                        return;
                    }

                    projectilePrefab = skill.projectilePrefab;
                    projectileName = skill.skillName;
                }

                if (projectilePrefab == null)
                {
                    Debug.LogWarning($"[NetworkSync] ⚠️ Префаб снаряда не найден для {projectileName}");
                    return;
                }

                // Создаём снаряд в позиции от сервера
                Vector3 spawnPos = new Vector3(data.spawnPosition.x, data.spawnPosition.y, data.spawnPosition.z);
                Vector3 direction = new Vector3(data.direction.x, data.direction.y, data.direction.z).normalized;

                GameObject projectileObj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));

                // Определяем цель (если есть targetSocketId)
                Transform target = null;
                if (!string.IsNullOrEmpty(data.targetSocketId))
                {
                    if (networkPlayers.TryGetValue(data.targetSocketId, out NetworkPlayer targetPlayer))
                    {
                        target = targetPlayer.transform;
                    }
                    else if (data.targetSocketId == localPlayerSocketId && localPlayer != null)
                    {
                        target = localPlayer.transform;
                    }
                }

                // Настраиваем снаряд (проверяем CelestialProjectile, ArrowProjectile, затем Projectile)
                CelestialProjectile celestialProjectile = projectileObj.GetComponent<CelestialProjectile>();
                ArrowProjectile arrowProjectile = projectileObj.GetComponent<ArrowProjectile>();

                if (celestialProjectile != null)
                {
                    // ВАЖНО: isVisualOnly = true для сетевых снарядов
                    celestialProjectile.Initialize(target, 0f, direction, player.gameObject, null, isVisualOnly: true);
                    Debug.Log($"[NetworkSync] ✅ CelestialProjectile создан для {player.username} (визуальный режим)");
                }
                else if (arrowProjectile != null)
                {
                    // ВАЖНО: isVisualOnly = true для сетевых снарядов
                    arrowProjectile.Initialize(target, 0f, direction, player.gameObject, null, isVisualOnly: true);
                    Debug.Log($"[NetworkSync] ✅ ArrowProjectile создан для {player.username} (визуальный режим)");
                }
                else
                {
                    Projectile projectile = projectileObj.GetComponent<Projectile>();
                    if (projectile != null)
                    {
                        // ВАЖНО: Для сетевого снаряда урон = 0 (визуальный)
                        projectile.Initialize(target, 0f, direction, player.gameObject);
                        Debug.Log($"[NetworkSync] ✅ Снаряд {projectilePrefab.name} создан для {player.username}");
                    }
                    else
                    {
                        Debug.LogWarning($"[NetworkSync] ⚠️ У префаба {projectilePrefab.name} нет компонента Projectile, CelestialProjectile или ArrowProjectile!");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ Network player {data.socketId} не найден для создания снаряда");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌ Ошибка в OnProjectileSpawned: {ex.Message}\nJSON: {jsonData}");
        }
    }

    /// <summary>
    /// Обработать создание визуального эффекта (НОВОЕ - для синхронизации взрывов, аур, горения и т.д.)
    /// </summary>
    private void OnVisualEffectSpawned(string jsonData)
    {
        Debug.Log($"[NetworkSync] ✨ RAW visual_effect_spawned JSON: {jsonData}");

        try
        {
            var data = JsonConvert.DeserializeObject<VisualEffectSpawnedEvent>(jsonData);
            Debug.Log($"[NetworkSync] ✨ Визуальный эффект получен: type={data.effectType}, prefab={data.effectPrefabName}, targetSocketId={data.targetSocketId}");

            // Skip if it's our own effect (we already created it locally)
            if (data.socketId == localPlayerSocketId)
            {
                Debug.Log($"[NetworkSync] ⏭️ Это наш собственный эффект, пропускаем");
                return;
            }

            // Определяем позицию для эффекта
            Vector3 effectPosition = new Vector3(data.position.x, data.position.y, data.position.z);
            Quaternion effectRotation = Quaternion.Euler(data.rotation.x, data.rotation.y, data.rotation.z);
            Transform effectParent = null;

            // Если эффект привязан к игроку - найти этого игрока
            if (!string.IsNullOrEmpty(data.targetSocketId))
            {
                // Проверяем это мы или сетевой игрок
                if (data.targetSocketId == localPlayerSocketId && localPlayer != null)
                {
                    effectParent = localPlayer.transform;
                    Debug.Log($"[NetworkSync] ✨ Эффект привязан к ЛОКАЛЬНОМУ игроку");
                }
                else if (networkPlayers.TryGetValue(data.targetSocketId, out NetworkPlayer targetPlayer))
                {
                    effectParent = targetPlayer.transform;
                    Debug.Log($"[NetworkSync] ✨ Эффект привязан к сетевому игроку {targetPlayer.username}");
                }
                else
                {
                    Debug.LogWarning($"[NetworkSync] ⚠️ Целевой игрок {data.targetSocketId} не найден для эффекта");
                }
            }

            // Пытаемся загрузить prefab эффекта из Resources
            GameObject effectPrefab = TryLoadEffectPrefab(data.effectPrefabName);
            if (effectPrefab == null)
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ Prefab эффекта '{data.effectPrefabName}' не найден!");
                return;
            }

            // Создаём эффект
            GameObject effectObj = null;
            if (effectParent != null)
            {
                // Привязываем к игроку (для аур, баффов)
                effectObj = Instantiate(effectPrefab, effectParent.position, effectRotation, effectParent);
                Debug.Log($"[NetworkSync] ✨ Эффект создан как child объект игрока");
            }
            else
            {
                // Создаём в мире (для взрывов, hit effects)
                effectObj = Instantiate(effectPrefab, effectPosition, effectRotation);
                Debug.Log($"[NetworkSync] ✨ Эффект создан в мировых координатах");
            }

            // Если указана длительность - уничтожаем через указанное время
            if (data.duration > 0f)
            {
                Destroy(effectObj, data.duration);
                Debug.Log($"[NetworkSync] ⏱️ Эффект будет уничтожен через {data.duration}с");
            }
            // Иначе пусть ParticleSystem сам уничтожится автоматически
            else
            {
                // Проверяем есть ли ParticleSystem и добавляем AutoDestroy компонент
                ParticleSystem ps = effectObj.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    float psLifetime = ps.main.duration + ps.main.startLifetime.constantMax;
                    Destroy(effectObj, psLifetime + 0.5f);
                    Debug.Log($"[NetworkSync] ⏱️ Эффект (ParticleSystem) будет уничтожен через {psLifetime:F1}с");
                }
            }

            Debug.Log($"[NetworkSync] ✅ Визуальный эффект создан: {data.effectPrefabName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌ Ошибка в OnVisualEffectSpawned: {ex.Message}\nJSON: {jsonData}");
        }
    }

    /// <summary>
    /// Попытаться загрузить prefab эффекта из Resources
    /// Ищет в папках: Effects/, Prefabs/Effects/, VFX/, Particles/
    /// </summary>
    private GameObject TryLoadEffectPrefab(string prefabName)
    {
        // Убираем расширения если есть
        prefabName = prefabName.Replace(".prefab", "");

        // Список возможных путей для поиска
        string[] possiblePaths = new string[]
        {
            $"Effects/{prefabName}",
            $"Prefabs/Effects/{prefabName}",
            $"VFX/{prefabName}",
            $"Particles/{prefabName}",
            prefabName // На случай если указан полный путь
        };

        foreach (string path in possiblePaths)
        {
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                Debug.Log($"[NetworkSync] ✅ Prefab найден: Resources/{path}");
                return prefab;
            }
        }

        Debug.LogWarning($"[NetworkSync] ⚠️ Prefab '{prefabName}' не найден ни в одной из папок Resources!");
        return null;
    }

    /// <summary>
    /// Обработать трансформацию игрока (НОВОЕ)
    /// </summary>
    private void OnPlayerTransformed(string jsonData)
    {
        Debug.Log($"[NetworkSync] 🐻 RAW player_transformed JSON: {jsonData}");

        try
        {
            var data = JsonUtility.FromJson<PlayerTransformedEvent>(jsonData);
            Debug.Log($"[NetworkSync] 🐻 Трансформация получена: socketId={data.socketId}, skillId={data.skillId}");

            // Skip if it's our own transformation (we already did it locally)
            if (data.socketId == localPlayerSocketId)
            {
                Debug.Log($"[NetworkSync] ⏭️ Это наша собственная трансформация, пропускаем");
                return;
            }

            // Find the network player who transformed
            if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
            {
                Debug.Log($"[NetworkSync] 🐻 Применяем трансформацию для {player.username}");

                // Apply transformation визуально к сетевому игроку
                player.ApplyTransformation(data.skillId);

                Debug.Log($"[NetworkSync] ✅ Трансформация применена к {player.username}");
            }
            else
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ Network player {data.socketId} не найден для трансформации");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌ Ошибка в OnPlayerTransformed: {ex.Message}\nJSON: {jsonData}");
        }
    }

    /// <summary>
    /// Обработать окончание трансформации игрока (НОВОЕ)
    /// </summary>
    private void OnPlayerTransformationEnded(string jsonData)
    {
        Debug.Log($"[NetworkSync] 🔄 RAW player_transformation_ended JSON: {jsonData}");

        try
        {
            var data = JsonUtility.FromJson<PlayerTransformationEndedEvent>(jsonData);
            Debug.Log($"[NetworkSync] 🔄 Окончание трансформации: socketId={data.socketId}");

            // Skip if it's our own transformation end
            if (data.socketId == localPlayerSocketId)
            {
                Debug.Log($"[NetworkSync] ⏭️ Это наша собственная трансформация, пропускаем");
                return;
            }

            // Find the network player
            if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
            {
                Debug.Log($"[NetworkSync] 🔄 Завершаем трансформацию для {player.username}");

                // End transformation визуально
                player.EndTransformation();

                Debug.Log($"[NetworkSync] ✅ Трансформация завершена для {player.username}");
            }
            else
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ Network player {data.socketId} не найден");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌ Ошибка в OnPlayerTransformationEnded: {ex.Message}\nJSON: {jsonData}");
        }
    }

    /// <summary>
    /// Обработать обновление здоровья игрока (SERVER AUTHORITY)
    /// </summary>
    private void OnHealthChanged(string jsonData)
    {
        var data = JsonUtility.FromJson<HealthChangedEvent>(jsonData);
        string critText = data.isCritical ? " 💥 КРИТИЧЕСКИЙ УДАР!" : "";
        Debug.Log($"[NetworkSync] 💔 Здоровье игрока {data.socketId}: {data.currentHealth}/{data.maxHealth}{critText}");

        // КРИТИЧЕСКОЕ: Проверяем это МЫ или сетевой игрок
        if (data.socketId == localPlayerSocketId)
        {
            // ЭТО МЫ ПОЛУЧИЛИ УРОН! Применяем через HealthSystem
            Debug.Log($"[NetworkSync] 💔 МЫ получили урон {data.damage}! HP: {data.currentHealth}/{data.maxHealth}");
            ApplyDamageToLocalPlayer(data.damage);
        }
        else if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
        {
            // Это сетевой игрок - обновляем его HP
            player.UpdateHealth((int)data.currentHealth, (int)data.maxHealth, player.CurrentMP, player.MaxMP);
            player.ShowDamage(data.damage);

            Debug.Log($"[NetworkSync] ✅ HP обновлён для {player.username}: {data.currentHealth}/{data.maxHealth}");
        }
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

    // ===== LOBBY SYSTEM EVENT HANDLERS =====

    /// <summary>
    /// Обработать создание лобби (10 секунд ожидание)
    /// </summary>
    private void OnLobbyCreated(string jsonData)
    {
        var data = JsonUtility.FromJson<LobbyCreatedEvent>(jsonData);
        Debug.Log($"[NetworkSync] 🏁 LOBBY CREATED! Ожидание {data.waitTime}ms перед стартом");

        // НЕ СПАВНИМ игрока сейчас! Ждем game_start
        // Можно показать UI с таймером через ArenaManager
        if (ArenaManager.Instance != null)
        {
            ArenaManager.Instance.OnLobbyStarted(data.waitTime);
        }
    }

    /// <summary>
    /// Обработать countdown (3, 2, 1...)
    /// </summary>
    private void OnGameCountdown(string jsonData)
    {
        var data = JsonUtility.FromJson<GameCountdownEvent>(jsonData);
        Debug.Log($"[NetworkSync] ⏱️ COUNTDOWN: {data.count}");

        // Показываем countdown UI
        if (ArenaManager.Instance != null)
        {
            ArenaManager.Instance.OnCountdown(data.count);
        }
    }

    /// <summary>
    /// Обработать старт игры - СПАВНИМ ВСЕХ ОДНОВРЕМЕННО!
    /// </summary>
    private void OnGameStart(string jsonData)
    {
        Debug.Log($"[NetworkSync] 🎮 GAME START! JSON: {jsonData}");

        try
        {
            var data = JsonUtility.FromJson<GameStartEvent>(jsonData);

            Debug.Log($"[NetworkSync] 🎮 Получено {data.players.Length} игроков для синхронного спавна");

            // КРИТИЧЕСКОЕ: Говорим ArenaManager СПАВНИТЬ локального игрока СЕЙЧАС!
            if (ArenaManager.Instance != null)
            {
                ArenaManager.Instance.OnGameStarted();
            }

            // Спавним всех сетевых игроков из pending (если есть)
            foreach (var playerData in data.players)
            {
                Debug.Log($"[NetworkSync] Игрок в game_start: {playerData.username} (socketId: {playerData.socketId}, spawnIndex: {playerData.spawnIndex})");

                // Skip ourselves - мы заспавнимся через ArenaManager
                // (мы не знаем свой socketId здесь, но это не страшно - pending не содержит нас)

                // Если игрок в pending - спавним его СЕЙЧАС с реальной позицией
                if (pendingPlayers.TryGetValue(playerData.socketId, out RoomPlayerInfo playerInfo))
                {
                    Debug.Log($"[NetworkSync] 🎬 Спавним pending игрока {playerInfo.username} при game_start");

                    // КРИТИЧЕСКОЕ: Используем spawn point по индексу от сервера
                    Vector3 spawnPos = Vector3.zero;
                    if (spawnPoints != null && playerData.spawnIndex >= 0 && playerData.spawnIndex < spawnPoints.Length)
                    {
                        spawnPos = spawnPoints[playerData.spawnIndex].position;
                        Debug.Log($"[NetworkSync] 📍 Spawn position для {playerInfo.username}: {spawnPos} (index {playerData.spawnIndex})");
                    }
                    else
                    {
                        Debug.LogWarning($"[NetworkSync] ⚠️ Некорректный spawnIndex {playerData.spawnIndex} для {playerInfo.username}, используем (0,0,0)");
                    }

                    SpawnNetworkPlayer(playerData.socketId, playerInfo.username, playerInfo.characterClass, spawnPos, playerInfo.stats);
                    pendingPlayers.Remove(playerData.socketId); // Удаляем из pending после спавна
                }
            }

            Debug.Log($"[NetworkSync] ✅ Game started! Всего сетевых игроков: {networkPlayers.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌ Error in OnGameStart: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // ===== NETWORK PLAYER MANAGEMENT =====

    /// <summary>
    /// Создать сетевого игрока
    /// </summary>
    private void SpawnNetworkPlayer(string socketId, string username, string characterClass, Vector3 position, SpecialStatsData stats = null)
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

        // КРИТИЧЕСКОЕ: Применяем SPECIAL stats от сервера СРАЗУ после спавна!
        if (stats != null)
        {
            CharacterStats characterStats = playerObj.GetComponent<CharacterStats>();
            if (characterStats != null)
            {
                characterStats.strength = stats.strength;
                characterStats.perception = stats.perception;
                characterStats.endurance = stats.endurance;
                characterStats.wisdom = stats.wisdom;
                characterStats.intelligence = stats.intelligence;
                characterStats.agility = stats.agility;
                characterStats.luck = stats.luck;

                characterStats.RecalculateStats(); // Пересчитываем все характеристики

                Debug.Log($"[NetworkSync] 📊 SPECIAL stats применены для {username}: S:{stats.strength} P:{stats.perception} E:{stats.endurance} W:{stats.wisdom} I:{stats.intelligence} A:{stats.agility} L:{stats.luck}");
            }
            else
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ CharacterStats не найден на {username}!");
            }
        }
        else
        {
            Debug.LogWarning($"[NetworkSync] ⚠️ Stats == null для {username}, используются дефолтные характеристики!");
        }

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

        // Добавляем красный никнейм над головой врага
        Nameplate nameplate = playerObj.AddComponent<Nameplate>();
        nameplate.Initialize(playerObj.transform, username, false); // false = красный (враг)

        networkPlayers[socketId] = networkPlayer;

        Debug.Log($"[NetworkSync] ✅ Создан сетевой игрок: {username} ({characterClass}) - враг для таргетинга с красным никнеймом");
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
    /// Получить префаб персонажа по классу (АВТОЗАГРУЗКА из Resources/Characters/)
    /// </summary>
    private GameObject GetCharacterPrefab(string characterClass)
    {
        // КРИТИЧЕСКОЕ: Загружаем префабы из Resources/Characters/ автоматически
        // Формат: Resources/Characters/{ClassName}Model.prefab
        string prefabPath = $"Characters/{characterClass}Model";
        GameObject prefab = Resources.Load<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"[NetworkSync] ❌ Префаб не найден: Resources/{prefabPath}.prefab");
            Debug.LogError($"[NetworkSync] Убедитесь что префаб {characterClass}Model.prefab находится в Assets/Resources/Characters/");

            // Fallback на Warrior если не найден
            prefab = Resources.Load<GameObject>("Characters/WarriorModel");
            if (prefab != null)
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ Используется Warrior как fallback для класса {characterClass}");
            }
        }
        else
        {
            Debug.Log($"[NetworkSync] ✅ Префаб загружен: {prefabPath}");
        }

        return prefab;
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

    /// <summary>
    /// Отложенный спавн локального игрока (даёт время ArenaManager.Start() выполниться)
    /// </summary>
    private System.Collections.IEnumerator SpawnLocalPlayerDelayed()
    {
        Debug.Log("[NetworkSync] ⏰ Отложенный спавн через 0.5 секунды...");

        // Ждём 0.5 секунды чтобы ArenaManager.Start() выполнился
        yield return new WaitForSeconds(0.5f);

        Debug.Log("[NetworkSync] ✅ Задержка истекла, спавним локального игрока");

        if (ArenaManager.Instance != null)
        {
            ArenaManager.Instance.OnGameStarted();
        }
        else
        {
            Debug.LogError("[NetworkSync] ❌ ArenaManager.Instance ВСЁЩЁ null после задержки!");
        }
    }

    /// <summary>
    /// ПУБЛИЧНЫЙ метод: Спавнить ВСЕХ pending игроков (вызывается из ArenaManager при FALLBACK countdown)
    /// </summary>
    public void SpawnAllPendingPlayers()
    {
        Debug.Log($"[NetworkSync] 🎬 Спавним ВСЕ pending игроки ({pendingPlayers.Count} игроков)...");

        // Создаем копию словаря чтобы избежать ошибки модификации во время итерации
        var pendingPlayersCopy = new Dictionary<string, RoomPlayerInfo>(pendingPlayers);

        foreach (var kvp in pendingPlayersCopy)
        {
            string socketId = kvp.Key;
            RoomPlayerInfo playerInfo = kvp.Value;

            Debug.Log($"[NetworkSync] 🎬 Спавним pending игрока {playerInfo.username} (spawnIndex={playerInfo.spawnIndex})");

            // КРИТИЧЕСКОЕ: Используем spawn point по индексу от сервера
            Vector3 spawnPos = Vector3.zero;
            if (spawnPoints != null && playerInfo.spawnIndex >= 0 && playerInfo.spawnIndex < spawnPoints.Length)
            {
                spawnPos = spawnPoints[playerInfo.spawnIndex].position;
                Debug.Log($"[NetworkSync] 📍 Spawn position для {playerInfo.username}: {spawnPos} (index {playerInfo.spawnIndex})");
            }
            else
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ Некорректный spawnIndex {playerInfo.spawnIndex} для {playerInfo.username}, используем (0,0,0)");
            }

            SpawnNetworkPlayer(socketId, playerInfo.username, playerInfo.characterClass, spawnPos, playerInfo.stats);
            pendingPlayers.Remove(socketId); // Удаляем из pending после спавна
        }

        Debug.Log($"[NetworkSync] ✅ Все pending игроки заспавнены! Теперь сетевых игроков: {networkPlayers.Count}");
    }

    /// <summary>
    /// Получить имя префаба снаряда по классу персонажа (для обычных атак)
    /// </summary>
    private string GetProjectilePrefabNameByClass(string className)
    {
        switch (className)
        {
            case "Archer":
                return "ArrowProjectile";
            case "Mage":
                return "CelestialBallProjectile";
            case "Rogue":
                return "SoulShardsProjectile";
            default:
                return null; // Воин и Паладин - ближний бой, снарядов нет
        }
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
    public int yourSpawnIndex; // ВАЖНО: Индекс точки спавна от сервера
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
    public int spawnIndex; // ВАЖНО: Индекс точки спавна игрока
    public SpecialStatsData stats; // КРИТИЧЕСКОЕ: SPECIAL характеристики персонажа
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
    public int spawnIndex; // ВАЖНО: Индекс точки спавна нового игрока
    public SpecialStatsData stats; // КРИТИЧЕСКОЕ: SPECIAL характеристики персонажа
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
/// Player skill used event
/// </summary>
[Serializable]
public class PlayerSkillUsedEvent
{
    public string socketId;
    public string username;
    public string characterClass;
    public int skillId;
    public string targetSocketId;
    public Vector3Data targetPosition;
    public long timestamp;
    public string skillType; // НОВОЕ: "Damage", "Heal", "Transformation" и т.д.
    public string animationTrigger; // НОВОЕ: триггер анимации ("Cast", "Attack" и т.д.)
    public float animationSpeed; // НОВОЕ: скорость анимации (default: 1.0)
    public float castTime; // НОВОЕ: время каста для задержки создания снаряда
}

/// <summary>
/// Projectile spawned event (НОВОЕ - для синхронизации снарядов)
/// </summary>
[Serializable]
public class ProjectileSpawnedEvent
{
    public string socketId;
    public int skillId;
    public Vector3Data spawnPosition;
    public Vector3Data direction;
    public string targetSocketId;
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

// ===== LOBBY SYSTEM EVENT DATA CLASSES =====

/// <summary>
/// Lobby created event (10 секунд ожидание)
/// </summary>
[Serializable]
public class LobbyCreatedEvent
{
    public int waitTime; // Время ожидания в миллисекундах (10000ms = 10s)
    public long timestamp;
}

/// <summary>
/// Game countdown event (3, 2, 1...)
/// </summary>
[Serializable]
public class GameCountdownEvent
{
    public int count; // 3, 2, 1 (сервер отправляет count, а не countdown)
    public long timestamp;
}

/// <summary>
/// Game start event - все спавнятся одновременно!
/// </summary>
[Serializable]
public class GameStartEvent
{
    public GameStartPlayerInfo[] players; // Все игроки в комнате
    public long timestamp;
}

/// <summary>
/// Player info в game_start event
/// </summary>
[Serializable]
public class GameStartPlayerInfo
{
    public string socketId;
    public string username;
    public string characterClass;
    public int spawnIndex;
}

/// <summary>
/// SPECIAL stats от сервера (S.P.E.C.I.A.L система)
/// </summary>
[Serializable]
public class SpecialStatsData
{
    public int strength;
    public int perception;
    public int endurance;
    public int wisdom;
    public int intelligence;
    public int agility;
    public int luck;
}

/// <summary>
/// Player transformed event (НОВОЕ)
/// </summary>
[Serializable]
public class PlayerTransformedEvent
{
    public string socketId;
    public string username;
    public int skillId; // ID скилла трансформации (301 = Bear Form)
    public long timestamp;
}

/// <summary>
/// Player transformation ended event (НОВОЕ)
/// </summary>
[Serializable]
public class PlayerTransformationEndedEvent
{
    public string socketId;
    public string username;
    public long timestamp;
}

/// <summary>
/// Visual effect spawned event (НОВОЕ - для синхронизации визуальных эффектов)
/// </summary>
[Serializable]
public class VisualEffectSpawnedEvent
{
    public string socketId; // Кто создал эффект
    public string effectType; // "explosion", "aura", "burn", "poison" и т.д.
    public string effectPrefabName; // Название prefab эффекта
    public Vector3Data position;
    public Vector3Data rotation;
    public string targetSocketId; // Если эффект привязан к игроку (пустая строка = world space)
    public float duration; // Длительность эффекта (0 = автоматически)
    public long timestamp;
}
