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

    /// <summary>
    /// Публичный доступ к socketId локального игрока для отправки урона
    /// </summary>
    public string LocalPlayerSocketId => localPlayerSocketId;

    /// <summary>
    /// Публичный доступ к точкам спавна для респавна
    /// </summary>
    public Transform[] SpawnPoints => spawnPoints;

    [Header("Settings (Mobile Optimized)")]
    [Tooltip("Интервал синхронизации позиций (секунды). 0.0167 = 60Hz, 0.033 = 30Hz, 0.05 = 20Hz, 0.1 = 10Hz")]
    [SerializeField] private float positionSyncInterval = 0.05f; // 20 Hz - Оптимально для мобильных устройств (баланс производительность/плавность)
    [SerializeField] private bool syncEnabled = true;
    [SerializeField] private bool alwaysSendPosition = false; // Отправлять только при движении (экономия трафика и CPU)

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

    // Network minions (скелеты, петы и т.д.)
    private Dictionary<string, GameObject> networkMinions = new Dictionary<string, GameObject>();

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

    // КРИТИЧЕСКОЕ: Защита от высокой скорости при спавне
    private bool justSpawned = false; // Флаг что игрок только что заспавнился
    private float spawnTime = 0f; // Время спавна
    private const float spawnProtectionTime = 2.0f; // 2 секунды защиты после спавна (увеличено из-за серверной коррекции)

    // КРИТИЧЕСКОЕ: Защита от повторного game_start
    private bool gameStarted = false;

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
        Debug.LogWarning("[NetworkSync] 🚀🚀🚀 START() ВЫЗВАН!");

        // Проверяем, находимся ли мы в мультиплеер режиме
        string roomId = PlayerPrefs.GetString("CurrentRoomId", "");
        Debug.LogWarning($"[NetworkSync] 🔍 CurrentRoomId: '{roomId}'");

        if (string.IsNullOrEmpty(roomId))
        {
            Debug.Log("[NetworkSync] Не в мультиплеере, отключаем синхронизацию");
            enabled = false;
            return;
        }

        // КРИТИЧЕСКОЕ: Получить socketId из SocketIOManager СРАЗУ!
        if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
        {
            localPlayerSocketId = SocketIOManager.Instance.GetSocketId();
            Debug.LogError($"[NetworkSync] ✅✅✅ localPlayerSocketId установлен из SocketIOManager: {localPlayerSocketId}");
        }
        else
        {
            Debug.LogError("[NetworkSync] ⚠️⚠️⚠️ SocketIOManager не подключен! localPlayerSocketId будет пустым!");
        }

        Debug.LogWarning("[NetworkSync] 🔍 Проверяем spawnPoints массив...");
        Debug.LogWarning($"[NetworkSync] 🔍 spawnPoints == null? {spawnPoints == null}");
        Debug.LogWarning($"[NetworkSync] 🔍 spawnPoints.Length: {(spawnPoints != null ? spawnPoints.Length : -1)}");

        // КРИТИЧЕСКОЕ: Автоматически находим spawn points если не назначены
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[NetworkSync] ⚠️ spawnPoints пустой! Ищем контейнер 'SpawnPoints' в сцене...");

            GameObject spawnPointsContainer = GameObject.Find("SpawnPoints");

            if (spawnPointsContainer != null)
            {
                Debug.LogWarning($"[NetworkSync] ✅ Найден контейнер 'SpawnPoints' с {spawnPointsContainer.transform.childCount} детьми!");

                spawnPoints = new Transform[spawnPointsContainer.transform.childCount];
                for (int i = 0; i < spawnPointsContainer.transform.childCount; i++)
                {
                    spawnPoints[i] = spawnPointsContainer.transform.GetChild(i);
                    if (i < 3) // Логируем первые 3 для диагностики
                    {
                        Debug.LogWarning($"[NetworkSync]   [{i}] {spawnPoints[i].name} at {spawnPoints[i].position}");
                    }
                }
                Debug.LogWarning($"[NetworkSync] ✅✅✅ Автоматически загружено {spawnPoints.Length} spawn points!");
            }
            else
            {
                Debug.LogError("[NetworkSync] ❌❌❌ SpawnPoints контейнер НЕ НАЙДЕН в сцене!");
                Debug.LogError("[NetworkSync] ❌ GameObject.Find('SpawnPoints') вернул NULL!");
            }
        }
        else
        {
            Debug.LogWarning($"[NetworkSync] ✅ spawnPoints УЖЕ назначен: {spawnPoints.Length} точек");
        }

        // Subscribe to WebSocket events FIRST
        SubscribeToNetworkEvents();

        // ВАЖНО: Запрашиваем список игроков в комнате ПОСЛЕ подписки
        // Потому что мы могли пропустить событие room_players если оно пришло до загрузки ArenaScene
        Debug.LogError("[NetworkSync] 🔄🔄🔄 ДИАГНОСТИКА: Запрашиваем список игроков в комнате...");
        if (SocketIOManager.Instance == null)
        {
            Debug.LogError("[NetworkSync] ❌ SocketIOManager.Instance == NULL!");
        }
        else
        {
            Debug.LogError($"[NetworkSync] ✅ SocketIOManager exists, calling RequestRoomPlayers()...");
            SocketIOManager.Instance.RequestRoomPlayers();
            Debug.LogError($"[NetworkSync] ✅ RequestRoomPlayers() вызван!");
        }
    }


    void Update()
    {
        if (!syncEnabled)
            return;

        // КРИТИЧЕСКОЕ: Проверяем что локальный игрок установлен
        if (localPlayer == null)
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
        SocketIOManager.Instance.On("effect_applied", OnEffectApplied); // НОВОЕ: Синхронизация статус-эффектов (Stun, Root, Buffs, Debuffs)

        // КРИТИЧЕСКОЕ: Регистрация обработчика minion_summoned
        Debug.LogError("[NetworkSync] 🔥🔥🔥 РЕГИСТРАЦИЯ ОБРАБОТЧИКА minion_summoned");
        SocketIOManager.Instance.On("minion_summoned", OnMinionSummoned); // НОВОЕ: Синхронизация призыва миньонов (Skeleton, и т.д.)
        Debug.LogError("[NetworkSync] ✅ Обработчик minion_summoned ЗАРЕГИСТРИРОВАН!");

        SocketIOManager.Instance.On("minion_animation", OnMinionAnimation); // НОВОЕ: Синхронизация анимаций миньонов
        SocketIOManager.Instance.On("minion_destroyed", OnMinionDestroyed); // НОВОЕ: Синхронизация уничтожения миньонов

        SocketIOManager.Instance.On("player_transformed", OnPlayerTransformed); // НОВОЕ: Синхронизация трансформации
        SocketIOManager.Instance.On("player_transformation_ended", OnPlayerTransformationEnded); // НОВОЕ: Окончание трансформации
        SocketIOManager.Instance.On("player_health_changed", OnHealthChanged);
        SocketIOManager.Instance.On("player_damaged", OnPlayerDamaged); // НОВОЕ: Синхронизация урона через сервер (PvP)
        SocketIOManager.Instance.On("player_healed", OnPlayerHealed); // НОВОЕ: Синхронизация лечения через сервер
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
        SocketIOManager.Instance.On("match_start", OnMatchStart); // НОВОЕ: Событие от клиента для начала матча

        Debug.Log("[NetworkSync] ✅ Подписан на сетевые события");
        Debug.Log("[NetworkSync] 🔍 ДИАГНОСТИКА: Подписка на 'game_start' и 'match_start' зарегистрирована!");
    }

    /// <summary>
    /// Установить spawn points извне (из BattleSceneManager или ArenaManager)
    /// КРИТИЧЕСКОЕ: Должно быть вызвано ДО SetLocalPlayer для корректной синхронизации
    /// </summary>
    public void SetSpawnPoints(Transform[] points)
    {
        if (points == null || points.Length == 0)
        {
            Debug.LogError("[NetworkSync] ❌ SetSpawnPoints: массив пустой или NULL!");
            return;
        }

        spawnPoints = points;
        Debug.Log($"[NetworkSync] ✅✅✅ Spawn points установлены извне: {spawnPoints.Length} точек");

        // Валидация
        int validCount = 0;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                validCount++;
                Debug.Log($"[NetworkSync]   [{i}] {spawnPoints[i].name} at {spawnPoints[i].position}");
            }
            else
            {
                Debug.LogError($"[NetworkSync]   [{i}] NULL!");
            }
        }
        Debug.Log($"[NetworkSync] ✅ Валидация: {validCount}/{spawnPoints.Length} точек корректны");
    }

    /// <summary>
    /// Установить локального игрока
    /// </summary>
    public void SetLocalPlayer(GameObject player, string characterClass)
    {
        localPlayer = player;
        localPlayerClass = characterClass;

        // КРИТИЧЕСКОЕ: Устанавливаем флаг спавна для защиты от высокой скорости
        justSpawned = true;
        spawnTime = Time.time;
        lastSentPosition = player.transform.position; // Инициализируем с позицией спавна
        lastSentRotation = player.transform.rotation;

        Debug.Log($"[NetworkSync] ✅ Локальный игрок установлен: {characterClass}");
        Debug.Log($"[NetworkSync] 🛡️ Защита от высокой скорости активна на {spawnProtectionTime}с");
        Debug.Log($"[NetworkSync] 📍 Начальная позиция: {lastSentPosition}");
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
        // ПРИМЕЧАНИЕ: localPlayer и SocketIOManager уже проверены в Update()

        // КРИТИЧЕСКОЕ: Защита от высокой скорости сразу после спавна
        if (justSpawned)
        {
            float timeSinceSpawn = Time.time - spawnTime;

            if (timeSinceSpawn < spawnProtectionTime)
            {
                // Пропускаем синхронизацию в первые 2 секунды после спавна
                // Это предотвращает отправку огромной скорости из-за телепорта в spawn point
                if (Time.frameCount % 30 == 0) // Логируем каждые 0.5 секунды
                {
                    Debug.LogWarning($"[NetworkSync] 🛡️🛡️🛡️ ЗАЩИТА ОТ СПАВНА АКТИВНА ({timeSinceSpawn:F2}s/{spawnProtectionTime}s) - НЕ ОТПРАВЛЯЕМ ПОЗИЦИЮ!");
                }
                return; // КРИТИЧЕСКОЕ: Выходим без отправки данных!
            }
            else
            {
                // Время защиты истекло
                justSpawned = false;
                Debug.LogWarning($"[NetworkSync] ✅✅✅ ЗАЩИТА ОТ СПАВНА ДЕАКТИВИРОВАНА ({timeSinceSpawn:F2}s) - НАЧИНАЕМ СИНХРОНИЗАЦИЮ!");
            }
        }

        // Get velocity и позицию
        Vector3 velocity = Vector3.zero;
        bool isGrounded = true;
        Vector3 position = localPlayer.transform.position;
        Quaternion rotation = localPlayer.transform.rotation;

        var controller = localPlayer.GetComponent<CharacterController>();
        if (controller != null)
        {
            // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Берём ТОЛЬКО горизонтальную скорость (XZ plane)
            // Исключаем Y компонент (гравитация/прыжки), т.к. сервер проверяет ГОРИЗОНТАЛЬНУЮ скорость
            // CharacterController.velocity включает гравитацию, что может давать 420 m/s при падении
            Vector3 fullVelocity = controller.velocity;
            velocity = new Vector3(fullVelocity.x, 0f, fullVelocity.z); // Убираем Y компонент
            isGrounded = controller.isGrounded;
        }
        else
        {
            var rigidbody = localPlayer.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                // Также убираем Y компонент для Rigidbody
                Vector3 fullVelocity = rigidbody.linearVelocity;
                velocity = new Vector3(fullVelocity.x, 0f, fullVelocity.z);
            }
        }

        // DOTA 2 STYLE: ВСЕГДА отправляем позицию для максимальной синхронизации!
        // Убрали пороги движения - отправляем каждый кадр согласно positionSyncInterval

        if (alwaysSendPosition)
        {
            // Режим DOTA 2: Постоянная синхронизация (60Hz)
            SocketIOManager.Instance.UpdatePosition(position, rotation, velocity, isGrounded);

            // ДИАГНОСТИКА: Логируем каждую 60-ю отправку (1 раз в секунду при 60Hz)
            if (Time.frameCount % 60 == 0)
            {
                float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;
                Debug.Log($"[NetworkSync] 📤 60Hz SYNC: pos=({position.x:F2}, {position.y:F2}, {position.z:F2}), vel=({velocity.x:F2}, 0.0, {velocity.z:F2}), speed={horizontalSpeed:F2}m/s, rot={rotation.eulerAngles.y:F0}°");
            }
        }
        else
        {
            // Режим оптимизации: отправляем только при изменении (СТАРАЯ ЛОГИКА)
            float positionDelta = Vector3.Distance(position, lastSentPosition);
            float rotationDelta = Quaternion.Angle(rotation, lastSentRotation);
            bool isMoving = velocity.sqrMagnitude > 0.01f;

            if (isMoving || positionDelta > positionThreshold || rotationDelta > rotationThreshold)
            {
                lastSentPosition = position;
                lastSentRotation = rotation;
                SocketIOManager.Instance.UpdatePosition(position, rotation, velocity, isGrounded);
            }
        }
    }

    /// <summary>
    /// Синхронизировать анимацию локального игрока
    /// </summary>
    private void SyncLocalPlayerAnimation()
    {
        // ПРИМЕЧАНИЕ: localPlayer и SocketIOManager уже проверены в Update()
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
        Debug.LogError($"[NetworkSync] 📦📦📦 ROOM_PLAYERS СОБЫТИЕ ПОЛУЧЕНО!!! JSON: {jsonData}");

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
            Debug.Log($"[NetworkSync] 🎮 Статус игры от сервера: gameStarted={data.gameStarted}");

            // КРИТИЧЕСКОЕ: Сохраняем наш socketId для проверки получения урона
            localPlayerSocketId = data.yourSocketId;

            // КРИТИЧЕСКОЕ: Устанавливаем индекс точки спавна в Manager (ArenaManager или BattleSceneManager)
            if (ArenaManager.Instance != null)
            {
                ArenaManager.Instance.SetSpawnIndex(data.yourSpawnIndex);
                Debug.Log($"[NetworkSync] ✅ Индекс точки спавна установлен в ArenaManager: {data.yourSpawnIndex}");
            }
            else if (BattleSceneManager.Instance != null)
            {
                BattleSceneManager.Instance.SetSpawnIndex(data.yourSpawnIndex);
                Debug.Log($"[NetworkSync] ✅ Индекс точки спавна установлен в BattleSceneManager: {data.yourSpawnIndex}");
            }
            else
            {
                Debug.LogWarning("[NetworkSync] ⚠️ НИ ArenaManager НИ BattleSceneManager не найдены! Не могу установить spawnIndex");
            }

            // КРИТИЧНО MMO FIX: Проверяем gameStarted от СЕРВЕРА, не только локально!
            // В MMO режиме игра ВСЕГДА идёт (persistent world), сервер сообщает через gameStarted флаг
            bool localGameStarted = ArenaManager.Instance != null && ArenaManager.Instance.IsGameStarted();
            bool gameAlreadyStarted = data.gameStarted || localGameStarted;

            Debug.Log($"[NetworkSync] 🔍 Game status check: server.gameStarted={data.gameStarted}, local.IsGameStarted={localGameStarted}, final={gameAlreadyStarted}");

            if (gameAlreadyStarted)
            {
                Debug.Log($"[NetworkSync] 🎮 Игра УЖЕ ИДЕТ ({data.players.Length} игроков)! Спавним локального игрока сразу (JOIN EXISTING GAME)");

                // ВАЖНО: Отложенный спавн через корутину, чтобы ArenaManager.Start() успел выполниться
                StartCoroutine(SpawnLocalPlayerDelayed());
            }
            else
            {
                Debug.Log($"[NetworkSync] ⏳ Игра ещё НЕ началась (лобби или ожидание), НЕ спавним себя, ждем game_start");

                // ИЗМЕНЕНО: Instant spawn - просто обновляем счётчик игроков
                if (MatchmakingManager.Instance != null)
                {
                    Debug.Log($"[NetworkSync] 📣 Уведомляем MatchmakingManager о количестве игроков: {data.players.Length}");
                    // Используем UpdatePlayerCount вместо удалённого OnSecondPlayerJoined
                    MatchmakingManager.Instance.UpdatePlayerCount(data.players.Length);
                }
                else
                {
                    // FALLBACK: Если 2+ игроков и лобби еще не запущено - запускаем его сами!
                    if (data.players.Length >= 2 && ArenaManager.Instance != null)
                    {
                        var lobbyUI = GameObject.Find("LobbyUI");
                        if (lobbyUI == null)
                        {
                            Debug.Log($"[NetworkSync] 🏁 FALLBACK: Запускаем лобби (игроков в комнате: {data.players.Length})");
                            ArenaManager.Instance.OnLobbyStarted(17000); // 17 секунд как вы описали
                        }
                        else
                        {
                            Debug.Log($"[NetworkSync] ⏭️ LobbyUI уже существует");
                        }
                    }
                }
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

        // MMO MODE: Если игра УЖЕ началась (gameStarted == true), спавним игрока СРАЗУ!
        if (gameStarted)
        {
            Debug.LogError($"[NetworkSync] 🌍 MMO MODE: Игра уже идёт! Спавним {data.username} СРАЗУ (не ждём game_start)");

            // Используем позицию из data если есть, иначе spawn point
            Vector3 spawnPos = Vector3.zero;
            if (data.position != null && (data.position.x != 0 || data.position.y != 0 || data.position.z != 0))
            {
                spawnPos = new Vector3(data.position.x, data.position.y, data.position.z);
                Debug.Log($"[NetworkSync] 📍 Используем позицию от сервера: {spawnPos}");
            }
            else if (ArenaManager.Instance != null && ArenaManager.Instance.MultiplayerSpawnPoints != null)
            {
                int spawnIndex = data.spawnIndex;
                if (spawnIndex >= 0 && spawnIndex < ArenaManager.Instance.MultiplayerSpawnPoints.Length)
                {
                    spawnPos = ArenaManager.Instance.MultiplayerSpawnPoints[spawnIndex].position;
                    Debug.Log($"[NetworkSync] 📍 Используем spawn point #{spawnIndex}: {spawnPos}");
                }
                else
                {
                    Debug.LogWarning($"[NetworkSync] ⚠️ Invalid spawnIndex {spawnIndex}, используем spawn point #0");
                    spawnPos = ArenaManager.Instance.MultiplayerSpawnPoints[0].position;
                }
            }
            else
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ ArenaManager или SpawnPoints == null, используем (0,0,0)");
            }

            // Спавним сетевого игрока
            SpawnNetworkPlayer(
                playerInfo.socketId,
                playerInfo.username,
                playerInfo.characterClass,
                spawnPos,
                playerInfo.stats
            );

            // Удаляем из pending (уже заспавнен)
            pendingPlayers.Remove(data.socketId);
            Debug.LogError($"[NetworkSync] ✅ MMO MODE: {data.username} заспавнен СРАЗУ при player_joined!");
        }

        // КРИТИЧЕСКОЕ: Уведомляем MatchmakingManager о присоединении игрока
        // ВАЖНО: +1 для локального игрока (мы сами), который НЕ в networkPlayers!
        int totalPlayers = networkPlayers.Count + pendingPlayers.Count + 1;
        Debug.Log($"[NetworkSync] 👥 Всего игроков в комнате: {totalPlayers} (network={networkPlayers.Count}, pending={pendingPlayers.Count}, local=1)");

        // ИЗМЕНЕНО: Instant spawn - просто обновляем счётчик игроков
        if (MatchmakingManager.Instance != null)
        {
            Debug.Log($"[NetworkSync] 📣 Уведомляем MatchmakingManager о присоединении игрока (всего: {totalPlayers})");
            // Используем UpdatePlayerCount вместо удалённого OnSecondPlayerJoined
            MatchmakingManager.Instance.UpdatePlayerCount(totalPlayers);
        }
        else
        {
            // FALLBACK: Используем старую систему через ArenaManager
            if (totalPlayers >= 2 && ArenaManager.Instance != null)
            {
                var lobbyUI = GameObject.Find("LobbyUI");
                if (lobbyUI == null)
                {
                    Debug.Log($"[NetworkSync] 🏁 FALLBACK: Запускаем лобби через ArenaManager (всего игроков: {totalPlayers})");
                    ArenaManager.Instance.OnLobbyStarted(20000); // 20 секунд
                }
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
            // ДИАГНОСТИКА: Логируем ВСЕ player_moved события для отладки
            // Debug.Log($"[NetworkSync] 📥 RAW position data: {jsonData}"); // ОТКЛЮЧЕНО: слишком много спама

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

            // FALLBACK: Если game_start не пришёл, но игрок шлёт данные - спавним его!
            // Это ВРЕМЕННОЕ решение на случай если game_start событие не работает
            if (!networkPlayers.ContainsKey(data.socketId) && pendingPlayers.ContainsKey(data.socketId))
            {
                Debug.LogWarning($"[NetworkSync] 🆘 FALLBACK SPAWN TRIGGERED!");
                Debug.LogWarning($"[NetworkSync] 🆘 Спавним игрока {data.socketId} по player_moved (game_start не пришёл!)");
                Debug.LogWarning($"[NetworkSync] 🆘 Всего pending игроков: {pendingPlayers.Count}, сетевых игроков: {networkPlayers.Count}");

                RoomPlayerInfo playerInfo = pendingPlayers[data.socketId];

                // Используем spawnIndex из данных игрока (если есть)
                Vector3 spawnPos = Vector3.zero;
                if (ArenaManager.Instance != null && ArenaManager.Instance.MultiplayerSpawnPoints != null)
                {
                    int spawnIndex = playerInfo.spawnIndex;
                    if (spawnIndex >= 0 && spawnIndex < ArenaManager.Instance.MultiplayerSpawnPoints.Length)
                    {
                        spawnPos = ArenaManager.Instance.MultiplayerSpawnPoints[spawnIndex].position;
                        Debug.Log($"[NetworkSync] 📍 Используем spawn point #{spawnIndex}: {spawnPos}");
                    }
                    else
                    {
                        Debug.LogWarning($"[NetworkSync] ⚠️ Некорректный spawnIndex {spawnIndex}, используем (0,0,0)");
                    }
                }

                SpawnNetworkPlayer(data.socketId, playerInfo.username, playerInfo.characterClass, spawnPos, playerInfo.stats);
                pendingPlayers.Remove(data.socketId);
            }

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
            // Debug.Log($"[NetworkSync] 📥 RAW animation data: {jsonData}"); // ОТКЛЮЧЕНО: слишком много спама

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

            // Fallback: гарантируем что знаем свой socketId
            if (string.IsNullOrEmpty(localPlayerSocketId) && SocketIOManager.Instance != null)
            {
                localPlayerSocketId = SocketIOManager.Instance.GetSocketId();
                Debug.LogWarning($"[NetworkSync] ⚠️ localPlayerSocketId был пуст! Обновлён из SocketIOManager: {localPlayerSocketId}");
            }

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
                    // Это скилл - пробуем загрузить из SkillConfig (приоритет) или SkillDatabase (fallback)
                    SkillConfig[] allSkills = Resources.LoadAll<SkillConfig>("Skills");
                    SkillConfig skillConfig = null;

                    foreach (SkillConfig s in allSkills)
                    {
                        if (s.skillId == data.skillId)
                        {
                            skillConfig = s;
                            break;
                        }
                    }

                    if (skillConfig != null && skillConfig.projectilePrefab != null)
                    {
                        projectilePrefab = skillConfig.projectilePrefab;
                        projectileName = skillConfig.skillName;
                        Debug.Log($"[NetworkSync] 📦 Скилл загружен из SkillConfig: {projectileName}");
                    }
                    else
                    {
                        // Fallback: SkillDatabase
                        SkillDatabase db = SkillDatabase.Instance;
                        if (db != null)
                        {
                            SkillData skill = db.GetSkillById(data.skillId);
                            if (skill != null)
                            {
                                projectilePrefab = skill.projectilePrefab;
                                projectileName = skill.skillName;
                                Debug.Log($"[NetworkSync] 📦 Скилл загружен из SkillDatabase: {projectileName}");
                            }
                        }

                        if (projectilePrefab == null)
                        {
                            Debug.LogWarning($"[NetworkSync] ⚠️ Скилл с ID {data.skillId} не найден ни в SkillConfig, ни в SkillDatabase");
                            return;
                        }
                    }
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

            if (string.IsNullOrEmpty(localPlayerSocketId) && SocketIOManager.Instance != null)
            {
                localPlayerSocketId = SocketIOManager.Instance.GetSocketId();
                Debug.LogWarning($"[NetworkSync] ⚠️ localPlayerSocketId был пуст! Обновлён из SocketIOManager: {localPlayerSocketId}");
            }

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
            Debug.Log($"[NetworkSync] 🔍 ОТЛАДКА targetSocketId:");
            Debug.Log($"[NetworkSync] 🔍 data.targetSocketId: '{data.targetSocketId}' (длина: {data.targetSocketId?.Length ?? 0})");
            Debug.Log($"[NetworkSync] 🔍 localPlayerSocketId: '{localPlayerSocketId}' (длина: {localPlayerSocketId?.Length ?? 0})");
            Debug.Log($"[NetworkSync] 🔍 networkPlayers count: {networkPlayers.Count}");

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
                    Debug.LogWarning($"[NetworkSync] 🔍 Доступные networkPlayers:");
                    foreach (var kvp in networkPlayers)
                    {
                        Debug.LogWarning($"[NetworkSync]    - socketId: '{kvp.Key}', username: {kvp.Value.username}");
                    }
                }
            }
            else
            {
                Debug.Log($"[NetworkSync] 🔍 targetSocketId пустой - эффект в мировых координатах");
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
                effectObj = Instantiate(effectPrefab, effectPosition, effectRotation, effectParent);

                // Устанавливаем локальную позицию и ротацию для правильной привязки
                effectObj.transform.localPosition = Vector3.up * 1f; // 1 метр над головой
                effectObj.transform.localRotation = effectRotation;

                Debug.Log($"[NetworkSync] ✨ Эффект создан как child объект игрока, localPos=(0,1,0), localRot=(90,0,0)");
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
    /// Обработать применение статус-эффекта (НОВОЕ - для синхронизации Stun, Root, Buffs, Debuffs)
    /// </summary>
    private void OnEffectApplied(string jsonData)
    {
        Debug.Log($"[NetworkSync] ✨ RAW effect_applied JSON: {jsonData}");

        try
        {
            var data = JsonConvert.DeserializeObject<EffectAppliedEvent>(jsonData);
            Debug.Log($"[NetworkSync] ✨ Эффект получен: caster={data.socketId}, target={data.targetSocketId}, type={data.effectType}, duration={data.duration}");

            // Определяем кто цель эффекта
            GameObject targetObject = null;
            string targetName = "";

            if (string.IsNullOrEmpty(data.targetSocketId))
            {
                // Пустая строка = эффект на кастера (самого себя)
                Debug.Log($"[NetworkSync] 🎯 Цель эффекта: кастер (socketId={data.socketId})");

                if (data.socketId == localPlayerSocketId)
                {
                    // Это наш локальный игрок
                    targetObject = localPlayer;
                    targetName = "Local Player (self)";
                }
                else if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer casterPlayer))
                {
                    // Это сетевой игрок
                    targetObject = casterPlayer.gameObject;
                    targetName = casterPlayer.username + " (self)";
                }
            }
            else
            {
                // Эффект на другого игрока
                Debug.Log($"[NetworkSync] 🎯 Цель эффекта: другой игрок (targetSocketId={data.targetSocketId})");

                if (data.targetSocketId == localPlayerSocketId)
                {
                    // Эффект на нас!
                    targetObject = localPlayer;
                    targetName = "Local Player";
                }
                else if (networkPlayers.TryGetValue(data.targetSocketId, out NetworkPlayer targetPlayer))
                {
                    // Эффект на другого сетевого игрока
                    targetObject = targetPlayer.gameObject;
                    targetName = targetPlayer.username;
                }
            }

            if (targetObject == null)
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ Цель эффекта не найдена! targetSocketId={data.targetSocketId}");
                return;
            }

            Debug.Log($"[NetworkSync] ✨ Применяем эффект {data.effectType} к {targetName}");

            // Получаем EffectManager цели
            EffectManager effectManager = targetObject.GetComponent<EffectManager>();
            if (effectManager == null)
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ У {targetName} нет EffectManager!");
                return;
            }

            // Создаём временный EffectConfig из данных события
            // EffectConfig - это обычный класс (не ScriptableObject), создаём через new
            EffectConfig tempConfig = new EffectConfig();

            // Парсим EffectType из строки
            if (System.Enum.TryParse<EffectType>(data.effectType, out EffectType effectType))
            {
                tempConfig.effectType = effectType;
            }
            else
            {
                Debug.LogError($"[NetworkSync] ❌ Неизвестный тип эффекта: {data.effectType}");
                return;
            }

            tempConfig.duration = data.duration;
            tempConfig.power = data.power;
            tempConfig.tickInterval = data.tickInterval;
            tempConfig.syncWithServer = false; // НЕ отправляем обратно на сервер!

            // Загружаем prefab частиц если указан
            if (!string.IsNullOrEmpty(data.particleEffectPrefabName))
            {
                GameObject particlePrefab = TryLoadEffectPrefab(data.particleEffectPrefabName);
                if (particlePrefab != null)
                {
                    tempConfig.particleEffectPrefab = particlePrefab;
                }
            }

            // Применяем эффект (только визуально, урон/лечение идёт на сервере)
            effectManager.ApplyEffectVisual(tempConfig, data.duration);

            Debug.Log($"[NetworkSync] ✅ Эффект {data.effectType} применён к {targetName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌ Ошибка в OnEffectApplied: {ex.Message}\nJSON: {jsonData}");
        }
    }

    /// <summary>
    /// Обработать призыв миньона (НОВОЕ - для синхронизации Skeleton, и т.д.)
    /// </summary>
    private void OnMinionSummoned(string jsonData)
    {
        Debug.Log($"[NetworkSync] ═══════════════════════════════════════════");
        Debug.Log($"[NetworkSync] 💀 ПОЛУЧЕНО СОБЫТИЕ: minion_summoned");
        Debug.Log($"[NetworkSync] 📥 RAW JSON: {jsonData}");

        try
        {
            var data = JsonConvert.DeserializeObject<MinionSummonedEvent>(jsonData);

            Debug.Log($"[NetworkSync] ✅ JSON распарсен успешно");
            Debug.Log($"[NetworkSync] 📊 minionType={data.minionType}, owner={data.ownerSocketId}");
            Debug.Log($"[NetworkSync] 📍 Позиция: ({data.positionX}, {data.positionY}, {data.positionZ})");
            Debug.Log($"[NetworkSync] ⚔️ Урон: {data.damage}, Duration: {data.duration}s");

            // КРИТИЧЕСКАЯ ПРОВЕРКА: Пропускаем если это НАШ миньон!
            // Локальный игрок уже создал скелета в SkillExecutor
            Debug.LogError($"[NetworkSync] - ПРОВЕРКА: ownerSocketId='{data.ownerSocketId}' vs localPlayerSocketId='{localPlayerSocketId}'");
            Debug.LogError($"[NetworkSync] - РАВНЫЙ? {data.ownerSocketId == localPlayerSocketId}");

            if (data.ownerSocketId == localPlayerSocketId)
            {
                Debug.LogError($"[NetworkSync] ⏭️ Пропускаем - это НАШ скелет (уже создан локально в SkillExecutor)");
                return;
            }

            Debug.Log($"[NetworkSync] ✅ Это скелет ДРУГОГО игрока - создаём Network Skeleton");

            // Загружаем префаб миньона
            GameObject minionPrefab = Resources.Load<GameObject>($"Minions/{CapitalizeFirst(data.minionType)}");
            if (minionPrefab == null)
            {
                Debug.LogError($"[NetworkSync] ❌ Префаб миньона не найден: Minions/{data.minionType}");
                return;
            }

            Debug.Log($"[NetworkSync] 🔍 Загружен префаб: {minionPrefab.name}");
            Debug.Log($"[NetworkSync] 🔍 Префаб тип: {minionPrefab.GetType().Name}");
            Debug.Log($"[NetworkSync] 🔍 Количество дочерних объектов: {minionPrefab.transform.childCount}");
            Debug.Log($"[NetworkSync] 🔍 Renderer в префабе: {minionPrefab.GetComponentsInChildren<Renderer>(true).Length}");

            // Создаём позицию и ротацию
            Vector3 spawnPosition = new Vector3(data.positionX, data.positionY, data.positionZ);
            Quaternion spawnRotation = Quaternion.Euler(0, data.rotationY, 0);

            // Спавним миньона
            GameObject minion = Instantiate(minionPrefab, spawnPosition, spawnRotation);
            minion.name = $"{data.minionType} (Network - Owner: {data.ownerSocketId})";

            Debug.Log($"[NetworkSync] 🔍 ПОСЛЕ Instantiate:");
            Debug.Log($"[NetworkSync] 🔍 Minion name: {minion.name}");
            Debug.Log($"[NetworkSync] 🔍 Количество дочерних объектов: {minion.transform.childCount}");
            Debug.Log($"[NetworkSync] 🔍 Позиция: {minion.transform.position}");

            // ВАЖНО: Устанавливаем правильный layer для видимости
            // Проверяем что все renderer'ы включены
            SetLayerRecursively(minion, LayerMask.NameToLayer("Default"));

            Renderer[] renderers = minion.GetComponentsInChildren<Renderer>();
            Debug.Log($"[NetworkSync] 🎨 Skeleton renderers: {renderers.Length}");
            foreach (Renderer r in renderers)
            {
                r.enabled = true;
                Debug.Log($"[NetworkSync] 🎨 Renderer: {r.name}, enabled: {r.enabled}, layer: {LayerMask.LayerToName(r.gameObject.layer)}");
            }

            // КРИТИЧЕСКОЕ: Удаляем НЕНУЖНЫЕ компоненты (PlayerController, StarterAssetsInputs)
            // Эти компоненты есть в Skeleton.prefab по ошибке - они для игрока, не для AI!
            Component[] allComponents = minion.GetComponents<Component>();
            foreach (Component comp in allComponents)
            {
                string typeName = comp.GetType().Name;
                if (typeName == "PlayerController" ||
                    typeName == "StarterAssetsInputs" ||
                    typeName == "SimplePlayerController")
                {
                    Destroy(comp);
                    Debug.Log($"[NetworkSync] 🗑️ Удалён {typeName} из миньона (это для игрока!)");
                }
            }

            // КРИТИЧЕСКОЕ: Настраиваем Animator для миньона
            // Копируем Animator с ПРЕФАБА (не с FBX!)
            Animator prefabAnimator = minionPrefab.GetComponentInChildren<Animator>();
            Animator minionAnimator = minion.GetComponentInChildren<Animator>();

            if (minionAnimator != null)
            {
                minionAnimator.enabled = true;
                Debug.Log($"[NetworkSync] 🎬 Animator найден: {minionAnimator.name}");

                // Копируем настройки с ПРЕФАБА
                if (prefabAnimator != null && prefabAnimator.runtimeAnimatorController != null)
                {
                    minionAnimator.runtimeAnimatorController = prefabAnimator.runtimeAnimatorController;
                    minionAnimator.avatar = prefabAnimator.avatar;
                    minionAnimator.applyRootMotion = prefabAnimator.applyRootMotion;

                    // ВАЖНО: Устанавливаем правильные режимы работы Animator
                    minionAnimator.updateMode = AnimatorUpdateMode.Normal;
                    minionAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                    Debug.Log($"[NetworkSync] ✅ AnimatorController скопирован с префаба: {minionAnimator.runtimeAnimatorController.name}");
                    Debug.Log($"[NetworkSync] 🎬 UpdateMode: {minionAnimator.updateMode}, CullingMode: {minionAnimator.cullingMode}");
                }
                else
                {
                    // Fallback: загружаем RogueAnimator вручную
                    Debug.LogWarning($"[NetworkSync] ⚠️ У префаба нет AnimatorController, загружаем вручную");

                    RuntimeAnimatorController rogueController = null;

#if UNITY_EDITOR
                    // В Editor mode используем AssetDatabase
                    rogueController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Controllers/RogueAnimator.controller");
                    if (rogueController != null)
                    {
                        Debug.Log($"[NetworkSync] ✅ Загружен RogueAnimator из AssetDatabase");
                    }
#endif

                    // Fallback: пробуем Resources
                    if (rogueController == null)
                    {
                        rogueController = Resources.Load<RuntimeAnimatorController>("Animations/Controllers/RogueAnimator");
                    }
                    if (rogueController == null)
                    {
                        rogueController = Resources.Load<RuntimeAnimatorController>("RogueAnimator");
                    }

                    if (rogueController != null)
                    {
                        minionAnimator.runtimeAnimatorController = rogueController;
                        minionAnimator.applyRootMotion = false;
                        minionAnimator.updateMode = AnimatorUpdateMode.Normal;
                        minionAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                        Debug.Log($"[NetworkSync] ✅ AnimatorController назначен вручную: {rogueController.name}");
                    }
                    else
                    {
                        Debug.LogError($"[NetworkSync] ❌ RogueAnimator не найден ни в префабе, ни в AssetDatabase, ни в Resources!");
                    }
                }

                // КРИТИЧЕСКОЕ: Устанавливаем InBattle = true для скелета (боевая стойка)
                if (minionAnimator.runtimeAnimatorController != null)
                {
                    minionAnimator.SetBool("InBattle", true);
                    Debug.Log($"[NetworkSync] ⚔️ InBattle = true установлен для сетевого скелета");
                }
            }
            else
            {
                Debug.LogError($"[NetworkSync] ❌ Animator НЕ найден в миньоне!");
            }

            // Настраиваем NavMeshAgent (если нет в префабе)
            UnityEngine.AI.NavMeshAgent navAgent = minion.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent == null)
            {
                navAgent = minion.AddComponent<UnityEngine.AI.NavMeshAgent>();
                navAgent.speed = 2.625f;  // Было: 5.25f (-50%)
                navAgent.acceleration = 8f;
                navAgent.angularSpeed = 120f;
                navAgent.stoppingDistance = 1.5f;
                navAgent.radius = 0.5f;
                navAgent.height = 2f;
                Debug.Log($"[NetworkSync] ✅ NavMeshAgent добавлен");
            }

            // Настраиваем CapsuleCollider (если нет в префабе)
            CapsuleCollider collider = minion.GetComponent<CapsuleCollider>();
            if (collider == null)
            {
                collider = minion.AddComponent<CapsuleCollider>();
                collider.center = new Vector3(0, 1f, 0);
                collider.radius = 0.5f;
                collider.height = 2f;
                collider.direction = 1; // Y-axis
                Debug.Log($"[NetworkSync] ✅ CapsuleCollider добавлен");
            }

            // Находим владельца (NetworkPlayer)
            GameObject ownerObject = null;
            NetworkPlayer ownerPlayer = null;
            if (networkPlayers.TryGetValue(data.ownerSocketId, out ownerPlayer))
            {
                ownerObject = ownerPlayer.gameObject;
            }

            // Настраиваем AI миньона
            SkeletonAI skeletonAI = minion.GetComponent<SkeletonAI>();
            if (skeletonAI == null)
            {
                skeletonAI = minion.AddComponent<SkeletonAI>();
            }

            // Инициализируем AI (без CharacterStats владельца т.к. это удалённый игрок)
            skeletonAI.Initialize(
                ownerObject != null ? ownerObject : minion,  // owner
                null,                                         // ownerStats (нет для удалённых игроков)
                data.damage,                                  // baseDamage
                data.intelligenceScaling,                     // intelligenceScaling
                data.duration,                                // lifetime
                data.ownerSocketId                            // ownerSocketId
            );

            // Добавляем SkeletonEntity
            SkeletonEntity skeletonEntity = minion.GetComponent<SkeletonEntity>();
            if (skeletonEntity == null)
            {
                skeletonEntity = minion.AddComponent<SkeletonEntity>();
            }
            skeletonEntity.SetOwner(data.ownerSocketId, ownerPlayer != null ? ownerPlayer.username : "Unknown");

            // КРИТИЧЕСКИ ВАЖНО: Добавляем Enemy компонент для интеграции с FogOfWar
            // Enemy компонент автоматически регистрируется в FogOfWar в своём Start()
            Enemy enemyComponent = minion.GetComponent<Enemy>();
            if (enemyComponent == null)
            {
                enemyComponent = minion.AddComponent<Enemy>();
                Debug.Log($"[NetworkSync] ✅ Enemy компонент добавлен на сетевого скелета для FogOfWar");
            }

            // Генерируем уникальный ID миньона и регистрируем в словаре для синхронизации анимаций
            // ВАЖНО: Используем простой формат ownerSocketId_minionType т.к. у каждого игрока может быть только 1 скелет
            string minionId = $"{data.ownerSocketId}_{data.minionType}";
            networkMinions[minionId] = minion;
            Debug.Log($"[NetworkSync] 📝 Миньон зарегистрирован: {minionId}");

            Debug.Log($"[NetworkSync] ✅✅✅ МИНЬОН {data.minionType.ToUpper()} СОЗДАН!");
            Debug.Log($"[NetworkSync] 👤 Владелец: {(ownerPlayer != null ? ownerPlayer.username : data.ownerSocketId)}");
            Debug.Log($"[NetworkSync] ═══════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌❌❌ ОШИБКА в OnMinionSummoned: {ex.Message}");
            Debug.LogError($"[NetworkSync] JSON: {jsonData}");
            Debug.LogError($"[NetworkSync] ═══════════════════════════════════════════");
        }
    }

    /// <summary>
    /// Обработать анимацию миньона (НОВОЕ - для синхронизации анимаций Skeleton и т.д.)
    /// </summary>
    private void OnMinionAnimation(string jsonData)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<MinionAnimationEvent>(jsonData);

            // ИСПРАВЛЕНО: НЕ пропускаем анимации для своего миньона!
            // ПРИЧИНА: Анимации могут не играть локально из-за проблем с Animator
            // Теперь применяем ВСЕ анимации независимо от владельца для диагностики
            Debug.Log($"[NetworkSync] 💀 Получена анимация миньона: {data.minionId}, владелец: {data.ownerSocketId}, анимация: {data.animation}");

            string minionOwnership = (data.ownerSocketId == localPlayerSocketId) ? "наш" : "чужой";
            Debug.Log($"[NetworkSync] 💀 Это {minionOwnership} миньон");

            // ВАЖНО: Сначала проверяем локального скелета (у нас в SkillExecutor)
            // Если это НАШ миньон, применяем анимацию к нашему локальному скелету
            if (data.ownerSocketId == localPlayerSocketId)
            {
                // Ищем локального скелета через SkillExecutor
                SkillExecutor skillExecutor = FindFirstObjectByType<SkillExecutor>();
                if (skillExecutor != null)
                {
                    // Пытаемся получить активного миньона из SkillExecutor
                    // Добавим публичный метод GetActiveMinion() в SkillExecutor
                    GameObject localMinion = skillExecutor.GetActiveMinion();
                    if (localMinion != null)
                    {
                        Animator minionAnimator = localMinion.GetComponentInChildren<Animator>();
                        if (minionAnimator != null)
                        {
                            ApplyMinionAnimation(minionAnimator, data.animation, data.minionId);
                            Debug.Log($"[NetworkSync] ✅ Анимация применена к ЛОКАЛЬНОМУ миньону через SkillExecutor");
                            return; // Анимация применена к локальному миньону
                        }
                        else
                        {
                            Debug.LogError($"[NetworkSync] ❌ Animator не найден у локального миньона!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[NetworkSync] ⚠️ Локальный миньон не найден в SkillExecutor (возможно уже уничтожен)");
                    }
                }
                else
                {
                    Debug.LogWarning($"[NetworkSync] ⚠️ SkillExecutor не найден!");
                }
            }

            // Ищем миньона в словаре Network миньонов (чужие скелеты)
            if (networkMinions.TryGetValue(data.minionId, out GameObject minion))
            {
                if (minion == null)
                {
                    // Миньон был уничтожен - удаляем из словаря
                    networkMinions.Remove(data.minionId);
                    return;
                }

                // Применяем анимацию
                Animator minionAnimator = minion.GetComponentInChildren<Animator>();
                if (minionAnimator != null)
                {
                    ApplyMinionAnimation(minionAnimator, data.animation, data.minionId);
                }
                else
                {
                    Debug.LogWarning($"[NetworkSync] ⚠️ Animator не найден у сетевого миньона {data.minionId}");
                }
            }
            else
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ Сетевой миньон {data.minionId} не найден в словаре (возможно ещё не создан или уже уничтожен)");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌ Ошибка в OnMinionAnimation: {ex.Message}");
        }
    }

    /// <summary>
    /// НОВЫЙ МЕТОД: Применить анимацию к миньону (централизованная логика)
    /// </summary>
    private void ApplyMinionAnimation(Animator minionAnimator, string animation, string minionId)
    {
        Debug.Log($"[NetworkSync] 🎬 Применяем анимацию '{animation}' к миньону {minionId}");
        Debug.Log($"[NetworkSync] 🎬 Animator.enabled = {minionAnimator.enabled}");
        Debug.Log($"[NetworkSync] 🎬 Animator.runtimeAnimatorController = {(minionAnimator.runtimeAnimatorController != null ? minionAnimator.runtimeAnimatorController.name : "NULL")}");

        // Проверяем что Animator включен и имеет controller
        if (!minionAnimator.enabled)
        {
            Debug.LogError($"[NetworkSync] ❌ Animator отключен для миньона {minionId}! Включаем...");
            minionAnimator.enabled = true;
        }

        if (minionAnimator.runtimeAnimatorController == null)
        {
            Debug.LogError($"[NetworkSync] ❌ Animator Controller не назначен для миньона {minionId}!");
            return;
        }

        // Применяем анимацию
        switch (animation)
        {
            case "Walking":
                minionAnimator.SetBool("IsMoving", true);
                Debug.Log($"[NetworkSync] 💀 Миньон {minionId}: IsMoving = true");
                Debug.Log($"[NetworkSync] 🔍 Проверка: IsMoving = {minionAnimator.GetBool("IsMoving")}");
                break;
            case "Idle":
                minionAnimator.SetBool("IsMoving", false);
                Debug.Log($"[NetworkSync] 💀 Миньон {minionId}: IsMoving = false");
                Debug.Log($"[NetworkSync] 🔍 Проверка: IsMoving = {minionAnimator.GetBool("IsMoving")}");
                break;
            case "Attack":
                minionAnimator.SetTrigger("Attack");
                Debug.Log($"[NetworkSync] 💀 Миньон {minionId}: Attack trigger");
                break;
            default:
                Debug.LogWarning($"[NetworkSync] ⚠️ Неизвестная анимация: {animation}");
                break;
        }
    }

    /// <summary>
    /// Обработать уничтожение миньона (НОВОЕ - для синхронизации смерти Skeleton и т.д.)
    /// </summary>
    private void OnMinionDestroyed(string jsonData)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<MinionDestroyedEvent>(jsonData);

            // Пропускаем если это наш миньон (уже уничтожен локально)
            if (data.ownerSocketId == localPlayerSocketId)
            {
                return;
            }

            // Ищем миньона в словаре по ID
            if (networkMinions.TryGetValue(data.minionId, out GameObject minion))
            {
                Debug.Log($"[NetworkSync] 💀 Уничтожение миньона: {data.minionId}");

                // Уничтожаем GameObject
                if (minion != null)
                {
                    Destroy(minion);
                }

                // Удаляем из словаря
                networkMinions.Remove(data.minionId);

                Debug.Log($"[NetworkSync] ✅ Миньон {data.minionId} успешно уничтожен и удалён из словаря");
            }
            else
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ Миньон {data.minionId} не найден в словаре (возможно уже уничтожен)");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌ Ошибка в OnMinionDestroyed: {ex.Message}");
        }
    }

    /// <summary>
    /// Преобразовать первую букву строки в заглавную
    /// </summary>
    private string CapitalizeFirst(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return char.ToUpper(str[0]) + str.Substring(1).ToLower();
    }

    /// <summary>
    /// Рекурсивно устанавливает layer для GameObject и всех его детей
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null)
            return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    /// <summary>
    /// Обработать трансформацию игрока (НОВОЕ)
    /// </summary>
    private void OnPlayerTransformed(string jsonData)
    {
        Debug.Log($"[NetworkSync] ═══════════════════════════════════════════");
        Debug.Log($"[NetworkSync] 🐻 ПОЛУЧЕНО СОБЫТИЕ: player_transformed");
        Debug.Log($"[NetworkSync] 📥 RAW JSON: {jsonData}");

        try
        {
            var data = JsonUtility.FromJson<PlayerTransformedEvent>(jsonData);
            Debug.Log($"[NetworkSync] ✅ JSON распарсен успешно");
            Debug.Log($"[NetworkSync] 📊 socketId={data.socketId}, skillId={data.skillId}");

            // Skip if it's our own transformation (we already did it locally)
            if (data.socketId == localPlayerSocketId)
            {
                Debug.Log($"[NetworkSync] ⏭️ Это наша собственная трансформация, пропускаем");
                Debug.Log($"[NetworkSync] ═══════════════════════════════════════════");
                return;
            }

            // Find the network player who transformed
            if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
            {
                Debug.Log($"[NetworkSync] 🎯 Найден сетевой игрок: {player.username}");
                Debug.Log($"[NetworkSync] 🐻 Применяем трансформацию к сетевому игроку...");

                // Apply transformation визуально к сетевому игроку
                player.ApplyTransformation(data.skillId);

                Debug.Log($"[NetworkSync] ✅✅✅ ТРАНСФОРМАЦИЯ ПРИМЕНЕНА К {player.username.ToUpper()}!");
                Debug.Log($"[NetworkSync] ═══════════════════════════════════════════");
            }
            else
            {
                Debug.LogWarning($"[NetworkSync] ⚠️⚠️⚠️ Network player {data.socketId} НЕ НАЙДЕН!");
                Debug.LogWarning($"[NetworkSync] Доступные игроки: {string.Join(", ", networkPlayers.Keys)}");
                Debug.Log($"[NetworkSync] ═══════════════════════════════════════════");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌❌❌ ОШИБКА в OnPlayerTransformed: {ex.Message}");
            Debug.LogError($"[NetworkSync] JSON: {jsonData}");
            Debug.LogError($"[NetworkSync] ═══════════════════════════════════════════");
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
    /// Обработать урон игрока (PvP) - НОВОЕ для синхронизации урона через сервер
    /// </summary>
    private void OnPlayerDamaged(string jsonData)
    {
        Debug.Log($"[NetworkSync] 💥 player_damaged event received: {jsonData}");

        try
        {
            var data = JsonConvert.DeserializeObject<PlayerDamagedEvent>(jsonData);

            if (data == null)
            {
                Debug.LogError("[NetworkSync] ❌ Failed to parse player_damaged event");
                return;
            }

            // КРИТИЧЕСКОЕ: Fallback если localPlayerSocketId пустой
            if (string.IsNullOrEmpty(localPlayerSocketId) && SocketIOManager.Instance != null)
            {
                localPlayerSocketId = SocketIOManager.Instance.GetSocketId();
                Debug.LogError($"[NetworkSync] ⚠️⚠️⚠️ localPlayerSocketId был пустой! Получен из SocketIOManager: {localPlayerSocketId}");
            }

            Debug.Log($"[NetworkSync] 🎯 Атакующий: {data.attackerName}, Цель: {data.targetSocketId}, Урон: {data.damage}");
            Debug.Log($"[NetworkSync] 💚 Target HP: {data.currentHealth}/{data.maxHealth}");
            Debug.LogError($"[NetworkSync] 🔍 ПРОВЕРКА: targetSocketId='{data.targetSocketId}' vs localPlayerSocketId='{localPlayerSocketId}'");
            Debug.LogError($"[NetworkSync] 🔍 РАВНЫ? {data.targetSocketId == localPlayerSocketId}");

            // КРИТИЧЕСКОЕ: Проверяем это МЫ или сетевой игрок получил урон
            if (data.targetSocketId == localPlayerSocketId)
            {
                // ЭТО МЫ ПОЛУЧИЛИ УРОН!
                Debug.LogError($"[NetworkSync] 💔💔💔 МЫ получили {data.damage} урона от {data.attackerName}!");
                Debug.LogError($"[NetworkSync] 💚 Наше HP: {data.currentHealth}/{data.maxHealth}");

                // ИСПРАВЛЕНО: Устанавливаем HP с сервера (НЕ применяем урон ещё раз!)
                // Сервер уже вычислил новое HP, мы просто синхронизируем его
                if (localPlayer != null)
                {
                    HealthSystem localHealth = localPlayer.GetComponent<HealthSystem>();
                    if (localHealth != null)
                    {
                        // ✅ ПРАВИЛЬНО: Устанавливаем HP с сервера (не дублируем урон!)
                        localHealth.SetHealth(data.currentHealth);
                        Debug.Log($"[NetworkSync] ✅ HP синхронизирован с сервера! HP: {data.currentHealth}/{data.maxHealth}");
                        Debug.Log($"[NetworkSync] 💔 Получен урон {data.damage} от {data.attackerName}");

                        // Показываем цифры урона над локальным игроком
                        if (DamageNumberManager.Instance != null)
                        {
                            Vector3 damagePos = localPlayer.transform.position + Vector3.up * 2f;
                            DamageNumberManager.Instance.ShowDamage(damagePos, data.damage, false, false);
                        }
                    }
                    else
                    {
                        Debug.LogError("[NetworkSync] ❌ HealthSystem НЕ НАЙДЕН на localPlayer!");
                    }
                }
                else
                {
                    Debug.LogError("[NetworkSync] ❌ localPlayer == NULL! Ищем через FindObjectOfType...");

                    // Fallback: поиск через FindObjectOfType
                    HealthSystem localHealth = GameObject.FindFirstObjectByType<HealthSystem>();
                    if (localHealth != null)
                    {
                        // ✅ ПРАВИЛЬНО: Устанавливаем HP с сервера
                        localHealth.SetHealth(data.currentHealth);
                        Debug.Log($"[NetworkSync] ✅ HP синхронизирован через FindObjectOfType! HP: {data.currentHealth}/{data.maxHealth}");

                        if (DamageNumberManager.Instance != null)
                        {
                            Vector3 damagePos = localHealth.transform.position + Vector3.up * 2f;
                            DamageNumberManager.Instance.ShowDamage(damagePos, data.damage, false, false);
                        }
                    }
                    else
                    {
                        Debug.LogError("[NetworkSync] ❌ HealthSystem НЕ НАЙДЕН НИГДЕ!");
                    }
                }
            }
            else if (networkPlayers.TryGetValue(data.targetSocketId, out NetworkPlayer targetPlayer))
            {
                // Это сетевой игрок получил урон
                Debug.Log($"[NetworkSync] 🌐 Сетевой игрок {targetPlayer.username} получил {data.damage} урона");

                // ИСПРАВЛЕНО: Синхронизируем HP с сервера (не дублируем урон!)
                HealthSystem targetHealth = targetPlayer.GetComponent<HealthSystem>();
                if (targetHealth != null)
                {
                    // ✅ ПРАВИЛЬНО: Устанавливаем HP с сервера
                    targetHealth.SetHealth(data.currentHealth);
                    Debug.Log($"[NetworkSync] ✅ HP синхронизирован для {targetPlayer.username}: {data.currentHealth}/{data.maxHealth}");
                }
                else
                {
                    Debug.LogWarning($"[NetworkSync] ⚠️ HealthSystem не найден для {targetPlayer.username}");
                }

                // Показываем визуальный эффект урона
                targetPlayer.ShowDamage(data.damage);
            }
            else
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ Игрок {data.targetSocketId} не найден в networkPlayers");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌ Error in OnPlayerDamaged: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Обработчик события лечения игрока с сервера
    /// Синхронизирует лечение между всеми игроками
    /// </summary>
    private void OnPlayerHealed(string jsonData)
    {
        Debug.Log($"[NetworkSync] 💚 player_healed event received: {jsonData}");

        try
        {
            var data = JsonConvert.DeserializeObject<PlayerHealedEvent>(jsonData);

            if (data == null)
            {
                Debug.LogError("[NetworkSync] ❌ Failed to parse player_healed event");
                return;
            }

            // КРИТИЧЕСКОЕ: Fallback если localPlayerSocketId пустой
            if (string.IsNullOrEmpty(localPlayerSocketId) && SocketIOManager.Instance != null)
            {
                localPlayerSocketId = SocketIOManager.Instance.GetSocketId();
                Debug.LogError($"[NetworkSync] ⚠️⚠️⚠️ localPlayerSocketId был пустой! Получен из SocketIOManager: {localPlayerSocketId}");
            }

            Debug.Log($"[NetworkSync] 🎯 Целитель: {data.healerName}, Цель: {data.targetSocketId}, Лечение: +{data.healAmount}");
            Debug.Log($"[NetworkSync] 💚 Target HP после лечения: {data.currentHealth}/{data.maxHealth}");
            Debug.Log($"[NetworkSync] 🔍 ПРОВЕРКА: targetSocketId='{data.targetSocketId}' vs localPlayerSocketId='{localPlayerSocketId}'");

            // КРИТИЧЕСКОЕ: Проверяем это МЫ или сетевой игрок получил лечение
            if (data.targetSocketId == localPlayerSocketId)
            {
                // ЭТО МЫ ПОЛУЧИЛИ ЛЕЧЕНИЕ!
                Debug.Log($"[NetworkSync] 💚💚💚 МЫ получили {data.healAmount} лечения от {data.healerName}!");
                Debug.Log($"[NetworkSync] 💚 Наше HP: {data.currentHealth}/{data.maxHealth}");

                // Устанавливаем HP с сервера (сервер уже вычислил новое HP)
                if (localPlayer != null)
                {
                    HealthSystem localHealth = localPlayer.GetComponent<HealthSystem>();
                    if (localHealth != null)
                    {
                        // ✅ ПРАВИЛЬНО: Устанавливаем HP с сервера
                        localHealth.SetHealth(data.currentHealth);
                        Debug.Log($"[NetworkSync] ✅ HP синхронизирован с сервера после лечения! HP: {data.currentHealth}/{data.maxHealth}");
                        Debug.Log($"[NetworkSync] 💚 Получено лечение +{data.healAmount} от {data.healerName}");

                        // Показываем визуальный эффект лечения (зеленые цифры)
                        if (DamageNumberManager.Instance != null)
                        {
                            Vector3 healPos = localPlayer.transform.position + Vector3.up * 2f;
                            DamageNumberManager.Instance.ShowDamage(healPos, data.healAmount, false, true); // isHeal = true
                        }
                    }
                    else
                    {
                        Debug.LogError("[NetworkSync] ❌ HealthSystem НЕ НАЙДЕН на localPlayer!");
                    }
                }
                else
                {
                    Debug.LogError("[NetworkSync] ❌ localPlayer == NULL! Ищем через FindObjectOfType...");

                    // Fallback: поиск через FindObjectOfType
                    HealthSystem localHealth = GameObject.FindFirstObjectByType<HealthSystem>();
                    if (localHealth != null)
                    {
                        // ✅ ПРАВИЛЬНО: Устанавливаем HP с сервера
                        localHealth.SetHealth(data.currentHealth);
                        Debug.Log($"[NetworkSync] ✅ HP синхронизирован через FindObjectOfType! HP: {data.currentHealth}/{data.maxHealth}");
                    }
                    else
                    {
                        Debug.LogError("[NetworkSync] ❌ HealthSystem НЕ НАЙДЕН НИГДЕ!");
                    }
                }
            }
            else if (networkPlayers.TryGetValue(data.targetSocketId, out NetworkPlayer targetPlayer))
            {
                // Это сетевой игрок получил лечение
                Debug.Log($"[NetworkSync] 🌐 Сетевой игрок {targetPlayer.username} получил {data.healAmount} лечения");

                // Синхронизируем HP с сервера
                HealthSystem targetHealth = targetPlayer.GetComponent<HealthSystem>();
                if (targetHealth != null)
                {
                    // ✅ ПРАВИЛЬНО: Устанавливаем HP с сервера
                    targetHealth.SetHealth(data.currentHealth);
                    Debug.Log($"[NetworkSync] ✅ HP синхронизирован для {targetPlayer.username}: {data.currentHealth}/{data.maxHealth}");
                }
                else
                {
                    Debug.LogWarning($"[NetworkSync] ⚠️ HealthSystem не найден для {targetPlayer.username}");
                }

                // Показываем визуальный эффект лечения (зеленые цифры)
                if (DamageNumberManager.Instance != null)
                {
                    Vector3 healPos = targetPlayer.transform.position + Vector3.up * 2f;
                    DamageNumberManager.Instance.ShowDamage(healPos, data.healAmount, false, true); // isHeal = true
                }
            }
            else
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ Игрок {data.targetSocketId} не найден в networkPlayers");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌ Error in OnPlayerHealed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Обработать смерть игрока
    /// </summary>
        private void OnPlayerDied(string jsonData)
    {
        Debug.Log($"[NetworkSync] 🔴 RAW player_died JSON: {jsonData}");

        try
        {
            var data = JsonConvert.DeserializeObject<PlayerDiedEvent>(jsonData);
            Debug.Log($"[NetworkSync] ☠️ Игрок погиб: {data.socketId}, Убийца: {data.killerId}, Респавн через: {data.respawnTime/1000}с");

            // Проверяем это мы или сетевой игрок
            if (data.socketId == localPlayerSocketId)
            {
                // ЭТО МЫ УМЕРЛИ!
                Debug.Log("[NetworkSync] 💀 МЫ ПОГИБЛИ! Вызываем HealthSystem.SetHealth(0) для активации PlayerDeathHandler...");

                // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Вызываем HealthSystem.SetHealth(0)
                // Это триггернет OnDeath событие, которое вызовет PlayerDeathHandler.OnPlayerDied()
                // PlayerDeathHandler правильно блокирует управление, анимацию, и запускает респавн
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    if (localPlayer != null)
                    {
                        HealthSystem healthSystem = localPlayer.GetComponent<HealthSystem>();
                        if (healthSystem != null)
                        {
                            // Устанавливаем HP = 0, это вызовет Die() → OnDeath → PlayerDeathHandler
                            healthSystem.SetHealth(0f);
                            Debug.Log("[NetworkSync] ✅ HealthSystem.SetHealth(0) вызван - PlayerDeathHandler обработает смерть!");
                        }
                        else
                        {
                            Debug.LogError("[NetworkSync] ❌ HealthSystem не найден на локальном игроке!");

                            // FALLBACK: Пробуем вызвать PlayerDeathHandler напрямую (не рекомендуется)
                            PlayerDeathHandler deathHandler = localPlayer.GetComponent<PlayerDeathHandler>();
                            if (deathHandler != null)
                            {
                                Debug.LogWarning("[NetworkSync] ⚠️ FALLBACK: Вызываем Respawn через Reflection");
                                // Вызываем приватный метод OnPlayerDied через Reflection
                                var method = typeof(PlayerDeathHandler).GetMethod("OnPlayerDied",
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (method != null)
                                {
                                    method.Invoke(deathHandler, null);
                                }
                            }
                        }
                    }
                });
            }
            else if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
            {
                // КРИТИЧЕСКИ ВАЖНО: Это сетевой игрок - устанавливаем HP = 0
                // HealthSystem вызовет событие OnDeath, которое запустит PlayerDeathHandler
                Debug.Log($"[NetworkSync] ☠️ Обрабатываем смерть для {player.username}");

                // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Уничтожаем миньонов погибшего игрока!
                // Ищем всех миньонов принадлежащих этому игроку
                List<string> minionsToRemove = new List<string>();
                foreach (var kvp in networkMinions)
                {
                    // minionId имеет формат: "ownerSocketId_minionType"
                    if (kvp.Key.StartsWith(data.socketId + "_"))
                    {
                        minionsToRemove.Add(kvp.Key);
                        Debug.Log($"[NetworkSync] 💀 Помечаем миньона {kvp.Key} для удаления (владелец погиб)");
                    }
                }

                // Уничтожаем найденных миньонов
                foreach (string minionId in minionsToRemove)
                {
                    if (networkMinions.TryGetValue(minionId, out GameObject minion))
                    {
                        if (minion != null)
                        {
                            Debug.Log($"[NetworkSync] 🗑️ Уничтожаем миньона {minionId} (владелец погиб)");
                            Destroy(minion);
                        }
                        networkMinions.Remove(minionId);
                    }
                }

                if (minionsToRemove.Count > 0)
                {
                    Debug.Log($"[NetworkSync] ✅ Уничтожено {minionsToRemove.Count} миньонов игрока {player.username}");
                }

                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    if (player != null)
                    {
                        // Получаем HealthSystem и устанавливаем HP = 0
                        HealthSystem healthSystem = player.GetComponent<HealthSystem>();
                        if (healthSystem != null)
                        {
                            healthSystem.SetHealth(0f);
                            Debug.Log($"[NetworkSync] ✅ HealthSystem.SetHealth(0) вызван для {player.username} - PlayerDeathHandler обработает смерть");
                        }
                        else
                        {
                            Debug.LogWarning($"[NetworkSync] ⚠️ HealthSystem не найден для {player.username}!");
                        }
                    }
                });
            }
            else
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ NetworkPlayer с socketId={data.socketId} не найден!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌ Error in OnPlayerDied: {ex.Message}");
        }
    }

    /// <summary>
    /// Обработать респавн игрока
    /// </summary>
        private void OnPlayerRespawned(string jsonData)
    {
        Debug.Log($"[NetworkSync] 🔵 RAW player_respawned JSON: {jsonData}");

        try
        {
            var data = JsonConvert.DeserializeObject<PlayerRespawnedEvent>(jsonData);
            Debug.Log($"[NetworkSync] 🔄 Игрок возродился: {data.socketId} на точке спавна #{data.spawnIndex}");

            // Получаем позицию из spawnIndex
            Vector3 spawnPos = Vector3.zero;

            // КРИТИЧНО: Если spawnPoints не назначены, пытаемся найти контейнер SpawnPoints в сцене
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[NetworkSync] ⚠️ spawnPoints не назначены! Ищем контейнер SpawnPoints...");

                // Пытаемся найти "SpawnPoints" или "MultiplayerSpawnPoints"
                GameObject spawnPointsContainer = GameObject.Find("SpawnPoints");
                if (spawnPointsContainer == null)
                {
                    Debug.LogWarning("[NetworkSync] ⚠️ 'SpawnPoints' не найден, ищем 'MultiplayerSpawnPoints'...");
                    spawnPointsContainer = GameObject.Find("MultiplayerSpawnPoints");
                }

                if (spawnPointsContainer != null && spawnPointsContainer.transform.childCount > 0)
                {
                    spawnPoints = new Transform[spawnPointsContainer.transform.childCount];
                    for (int i = 0; i < spawnPointsContainer.transform.childCount; i++)
                    {
                        spawnPoints[i] = spawnPointsContainer.transform.GetChild(i);
                    }
                    Debug.Log($"[NetworkSync] ✅ Найдено {spawnPoints.Length} spawn points в контейнере '{spawnPointsContainer.name}'!");
                }
                else
                {
                    Debug.LogError("[NetworkSync] ❌ Контейнер SpawnPoints/MultiplayerSpawnPoints не найден в сцене!");
                }
            }

            if (spawnPoints != null && data.spawnIndex >= 0 && data.spawnIndex < spawnPoints.Length)
            {
                spawnPos = spawnPoints[data.spawnIndex].position;
                Debug.Log($"[NetworkSync] 📍 Точка спавна #{data.spawnIndex}: {spawnPos}");
            }
            else
            {
                Debug.LogError($"[NetworkSync] ❌ Некорректный spawnIndex: {data.spawnIndex} (доступно: {spawnPoints?.Length ?? 0})");
                // Fallback - используем первую точку спавна
                if (spawnPoints != null && spawnPoints.Length > 0)
                {
                    spawnPos = spawnPoints[0].position;
                    Debug.LogWarning($"[NetworkSync] ⚠️ Используем fallback точку спавна #0: {spawnPos}");
                }
                else
                {
                    // Последний fallback - используем текущую позицию игрока
                    if (localPlayer != null)
                    {
                        spawnPos = localPlayer.transform.position;
                        Debug.LogError($"[NetworkSync] ❌❌ КРИТИЧЕСКАЯ ОШИБКА: spawnPoints не найдены! Используем текущую позицию: {spawnPos}");
                    }
                }
            }

            // Проверяем это мы или сетевой игрок
            if (data.socketId == localPlayerSocketId)
            {
                // ЭТО МЫ ВОСКРЕСЛИ!
                Debug.Log($"[NetworkSync] ⚕️ МЫ ВОСКРЕСЛИ на позиции {spawnPos}!");

                // Находим PlayerDeathHandler и респавним
                PlayerDeathHandler deathHandler = localPlayer?.GetComponent<PlayerDeathHandler>();
                if (deathHandler != null)
                {
                    deathHandler.Respawn(spawnPos);
                }
                else
                {
                    Debug.LogWarning("[NetworkSync] ⚠️ PlayerDeathHandler не найден! Используем fallback респавн");

                    // Fallback - просто телепортируем
                    CharacterController cc = localPlayer?.GetComponent<CharacterController>();
                    if (cc != null)
                    {
                        cc.enabled = false;
                        localPlayer.transform.position = spawnPos;
                        cc.enabled = true;
                    }
                }

                // Восстанавливаем HP локально
                HealthSystem healthSystem = localPlayer?.GetComponent<HealthSystem>();
                if (healthSystem != null)
                {
                    healthSystem.Revive(1f); // 100% HP
                    Debug.Log($"[NetworkSync] 💚 HP восстановлено: {healthSystem.CurrentHealth}/{healthSystem.MaxHealth}");
                }

                // КРИТИЧЕСКИ ВАЖНО: Сбрасываем lastAnimationState чтобы Idle отправился заново
                lastAnimationState = "";
                Debug.Log("[NetworkSync] 🔄 lastAnimationState сброшен для пересылки анимации");
            }
            else if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
            {
                // Это сетевой игрок - респавним его
                player.OnRespawn(spawnPos);
                Debug.Log($"[NetworkSync] ⚕️ {player.username} воскрешен на {spawnPos}!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌ Error in OnPlayerRespawned: {ex.Message}");
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
        Debug.Log($"[NetworkSync] 📥 RAW lobby_created JSON: {jsonData}");

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
        Debug.Log($"[NetworkSync] 📥 RAW game_countdown JSON: {jsonData}");

        var data = JsonUtility.FromJson<GameCountdownEvent>(jsonData);
        Debug.Log($"[NetworkSync] ⏱️ COUNTDOWN: {data.count}");

        // Показываем countdown UI
        if (ArenaManager.Instance != null)
        {
            ArenaManager.Instance.OnCountdown(data.count);
        }
    }

    /// <summary>
    /// Убедиться что spawn points загружены (ленивая инициализация)
    /// </summary>
    private void EnsureSpawnPointsLoaded()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Debug.Log($"[NetworkSync] ✅ Spawn points уже загружены: {spawnPoints.Length} точек");
            return; // Уже загружены
        }

        Debug.Log("[NetworkSync] 🔍 Ищем spawn points в сцене...");

        // ВРЕМЕННОЕ РЕШЕНИЕ: Используем ArenaManager если доступен
        if (ArenaManager.Instance != null)
        {
            Debug.Log("[NetworkSync] 🔍 ArenaManager найден, проверяем его spawn points...");
            // ArenaManager имеет публичное поле spawnPoints
            var arenaSpawnPoints = ArenaManager.Instance.GetType().GetField("spawnPoints",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (arenaSpawnPoints != null)
            {
                var points = arenaSpawnPoints.GetValue(ArenaManager.Instance) as Transform[];
                if (points != null && points.Length > 0)
                {
                    spawnPoints = points;
                    Debug.Log($"[NetworkSync] ✅ Получено {spawnPoints.Length} spawn points из ArenaManager!");
                    return;
                }
            }
        }

        // Ищем по разным возможным именам
        string[] possibleNames = { "SpawnPoints", "Spawn Points", "PlayerSpawnPoints", "RespawnPoints", "Respawn" };

        foreach (string name in possibleNames)
        {
            Debug.Log($"[NetworkSync] 🔍 Ищем GameObject: '{name}'...");
            GameObject spawnPointsContainer = GameObject.Find(name);
            if (spawnPointsContainer != null)
            {
                Debug.Log($"[NetworkSync] ✅ Найден '{name}' с {spawnPointsContainer.transform.childCount} детьми");
                spawnPoints = new Transform[spawnPointsContainer.transform.childCount];
                for (int i = 0; i < spawnPointsContainer.transform.childCount; i++)
                {
                    spawnPoints[i] = spawnPointsContainer.transform.GetChild(i);
                }
                Debug.Log($"[NetworkSync] ✅ Загружено {spawnPoints.Length} spawn points из '{name}'");
                return;
            }
        }

        Debug.LogError("[NetworkSync] ❌ Spawn points не найдены! Используем дефолтную позицию (0,0,0)");
        Debug.LogError("Создайте GameObject 'SpawnPoints' с дочерними точками спавна в BattleScene");
    }

    /// <summary>
    /// Обработать старт игры - СПАВНИМ ВСЕХ ОДНОВРЕМЕННО!
    /// </summary>
    private void OnGameStart(string jsonData)
    {
        Debug.Log($"[NetworkSync] 🎮 GAME START EVENT RECEIVED!");
        Debug.Log($"[NetworkSync] 🎮 JSON Length: {jsonData?.Length ?? 0}");
        Debug.Log($"[NetworkSync] 🎮 RAW JSON: {jsonData}");

        // Socket.IO иногда присылает строку в виде \"{...}\" (дополнительные кавычки и экранирование)
        string normalizedJson = NormalizeSocketJson(jsonData);
        if (!ReferenceEquals(normalizedJson, jsonData))
        {
            Debug.Log($"[NetworkSync] 🎮 Normalized JSON: {normalizedJson}");
            jsonData = normalizedJson;
        }

        // КРИТИЧЕСКОЕ: Защита от повторного вызова game_start
        if (gameStarted)
        {
            Debug.LogWarning("[NetworkSync] ⚠️⚠️⚠️ game_start уже обработан, игнорируем повторный вызов!");
            return;
        }
        gameStarted = true;
        Debug.Log("[NetworkSync] ✅ Первый вызов game_start, продолжаем обработку...");

        try
        {
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.LogError("[NetworkSync] ❌ game_start JSON is NULL or EMPTY!");
                return;
            }

            var data = JsonUtility.FromJson<GameStartEvent>(jsonData);

            if (data == null)
            {
                Debug.LogError("[NetworkSync] ❌ Failed to deserialize game_start JSON!");
                return;
            }

            if (data.players == null)
            {
                Debug.LogError("[NetworkSync] ❌ game_start data.players is NULL!");
                return;
            }

            Debug.Log($"[NetworkSync] 🎮 === GAME_START: СИНХРОННЫЙ СПАВН ===");
            Debug.Log($"[NetworkSync] 🎮 Получено {data.players.Length} игроков для синхронного спавна");
            Debug.Log($"[NetworkSync] 📊 Pending игроков: {pendingPlayers.Count}");
            Debug.Log($"[NetworkSync] 📊 Сетевых игроков уже заспавнено: {networkPlayers.Count}");
            Debug.Log($"[NetworkSync] 🎯 СИНХРОННЫЙ СПАВН: Сначала спавним ВСЕХ сетевых игроков, ПОТОМ локального!");

            // Логируем всех игроков из game_start
            Debug.Log($"[NetworkSync] 📋 Список игроков от сервера:");
            foreach (var p in data.players)
            {
                string posStr = (p.spawnPosition != null) ?
                    $"({p.spawnPosition.x:F2}, {p.spawnPosition.y:F2}, {p.spawnPosition.z:F2})" :
                    "NULL";
                Debug.LogError($"[NetworkSync] 🔥 {p.username}: socketId={p.socketId}, spawnIndex={p.spawnIndex}, spawnPosition={posStr}");
            }

            // ДИАГНОСТИКА: Проверяем pending игроков ПЕРЕД спавном
            Debug.LogError($"[NetworkSync] 🔍 PENDING PLAYERS COUNT: {pendingPlayers.Count}");
            Debug.LogError($"[NetworkSync] 🔍 LOCAL SOCKET ID: {localPlayerSocketId}");
            foreach (var kvp in pendingPlayers)
            {
                Debug.LogError($"[NetworkSync] 🔍 Pending: {kvp.Value.username} (socketId={kvp.Key})");
            }

            // ИЗМЕНЕНИЕ: Сначала спавним ВСЕ сетевых игроков из pending
            foreach (var playerData in data.players)
            {
                Debug.Log($"[NetworkSync] Игрок в game_start: {playerData.username} (socketId: {playerData.socketId}, spawnIndex: {playerData.spawnIndex})");

                // КРИТИЧЕСКОЕ: Пропускаем СЕБЯ (localPlayerSocketId)
                if (playerData.socketId == localPlayerSocketId)
                {
                    Debug.Log($"[NetworkSync] ⏭️ Пропускаем локального игрока {playerData.username} (это мы!)");
                    continue;
                }

                // Если игрок в pending - спавним его СЕЙЧАС с реальной позицией
                if (pendingPlayers.TryGetValue(playerData.socketId, out RoomPlayerInfo playerInfo))
                {
                    Debug.Log($"[NetworkSync] 🎬 Спавним pending игрока {playerInfo.username} при game_start");

                    // ✅ НОВАЯ СИСТЕМА: Используем координаты ОТ СЕРВЕРА!
                    // Сервер отправляет реальные координаты в spawnPosition
                    Vector3 spawnPos = Vector3.zero;

                    if (playerData.spawnPosition != null)
                    {
                        // КРИТИЧЕСКОЕ: Используем координаты ОТ СЕРВЕРА (единые для всех клиентов!)
                        spawnPos = new Vector3(
                            playerData.spawnPosition.x,
                            playerData.spawnPosition.y,
                            playerData.spawnPosition.z
                        );

                        Debug.Log($"[NetworkSync] ✅✅✅ SERVER SPAWN для {playerInfo.username}:");
                        Debug.LogError($"[NetworkSync] 🔥🔥🔥 SPAWN FROM SERVER: {playerInfo.username}");
                        Debug.LogError($"[NetworkSync] 🔥 SpawnIndex: {playerData.spawnIndex}");
                        Debug.LogError($"[NetworkSync] 🔥 Server Position: ({spawnPos.x:F2}, {spawnPos.y:F2}, {spawnPos.z:F2})");
                    }
                    else
                    {
                        // FALLBACK: Если сервер не отправил координаты - используем ЛОКАЛЬНЫЕ spawn points!
                        Debug.LogWarning($"[NetworkSync] ⚠️ spawnPosition от сервера NULL! FALLBACK на локальные spawn points...");

                        // КРИТИЧЕСКОЕ: Используем локальные spawn points по индексу
                        if (spawnPoints != null && playerData.spawnIndex >= 0 && playerData.spawnIndex < spawnPoints.Length)
                        {
                            spawnPos = spawnPoints[playerData.spawnIndex].position;
                            Debug.LogError($"[NetworkSync] 🔥 FALLBACK: Используем локальный spawn point [{playerData.spawnIndex}]: {spawnPoints[playerData.spawnIndex].name}");
                            Debug.LogError($"[NetworkSync] 🔥 Position: ({spawnPos.x:F2}, {spawnPos.y:F2}, {spawnPos.z:F2})");
                        }
                        else
                        {
                            // Крайний fallback - круговое расположение (если spawn points не загружены)
                            Debug.LogError($"[NetworkSync] ❌ Локальные spawn points НЕ ЗАГРУЖЕНЫ! Используем круговое расположение...");
                            Debug.LogError($"[NetworkSync] ❌ spawnPoints: {(spawnPoints == null ? "NULL" : spawnPoints.Length.ToString())}");
                            float angle = (playerData.spawnIndex / 20f) * 360f;
                            float radius = 10f;
                            spawnPos = new Vector3(
                                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                                0f,
                                Mathf.Sin(angle * Mathf.Deg2Rad) * radius
                            );
                            Debug.LogError($"[NetworkSync] ⚠️ КРУГОВАЯ позиция [{playerData.spawnIndex}]: {spawnPos}");
                        }
                    }

                    SpawnNetworkPlayer(playerData.socketId, playerInfo.username, playerInfo.characterClass, spawnPos, playerInfo.stats);
                    pendingPlayers.Remove(playerData.socketId); // Удаляем из pending после спавна
                }
            }

            // КРИТИЧЕСКОЕ: Теперь спавним локального игрока ПОСЛЕДНИМ (после всех остальных)
            // Это гарантирует что все игроки появляются В ОДИН КАДР!
            Debug.Log($"[NetworkSync] 👤 Все сетевые игроки заспавнены, теперь спавним локального игрока...");

            // ДИАГНОСТИКА: Проверяем что есть в сцене
            Debug.Log($"[NetworkSync] 🔍 ArenaManager.Instance = {(ArenaManager.Instance != null ? "EXISTS" : "NULL")}");
            Debug.Log($"[NetworkSync] 🔍 BattleSceneManager.Instance = {(BattleSceneManager.Instance != null ? "EXISTS" : "NULL")}");

            if (ArenaManager.Instance != null)
            {
                Debug.Log("[NetworkSync] ✅ Используем ArenaManager для спавна");
                ArenaManager.Instance.OnGameStarted();
            }
            else if (BattleSceneManager.Instance != null)
            {
                Debug.Log("[NetworkSync] ✅ Используем BattleSceneManager для спавна");
                BattleSceneManager.Instance.SpawnLocalPlayerNow();
            }
            else
            {
                Debug.LogError("[NetworkSync] ❌ НИ ArenaManager НИ BattleSceneManager не найдены в сцене!");
                Debug.LogError("[NetworkSync] ❌ Локальный игрок НЕ БУДЕТ заспавнен!");
            }

            Debug.Log($"[NetworkSync] ✅ Game started! Всего сетевых игроков: {networkPlayers.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSync] ❌ Error in OnGameStart: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Socket.IO events могут приходить как экранированная строка (\"{...}\"). Убираем лишнюю оболочку.
    /// </summary>
    private string NormalizeSocketJson(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
            return jsonData;

        string trimmed = jsonData.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '\"' && trimmed[trimmed.Length - 1] == '\"')
        {
            trimmed = trimmed.Substring(1, trimmed.Length - 2);
            trimmed = trimmed.Replace("\\\"", "\"");
            trimmed = trimmed.Replace("\\\\", "\\");
            return trimmed;
        }

        return jsonData;
    }

    /// <summary>
    /// Обработать match_start - событие от клиента когда таймер закончился
    /// Сервер должен закрыть комнату и отправить game_start всем игрокам
    /// </summary>
    private void OnMatchStart(string jsonData)
    {
        Debug.Log($"[NetworkSync] 🎮 MATCH_START EVENT RECEIVED!");
        Debug.Log($"[NetworkSync] JSON: {jsonData}");

        // Это событие отправляется клиентом на сервер
        // Сервер должен ответить событием game_start с полными данными
        // Здесь мы просто логируем для диагностики
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

        // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Включаем ВСЕ Renderer'ы для видимости!
        Renderer[] renderers = playerObj.GetComponentsInChildren<Renderer>();
        Debug.Log($"[NetworkSync] 🎨 Найдено Renderer'ов для {username}: {renderers.Length}");
        int enabledCount = 0;
        foreach (Renderer r in renderers)
        {
            if (!r.enabled)
            {
                Debug.LogWarning($"[NetworkSync]   ❌ {r.name}: DISABLED! Включаем...");
                r.enabled = true;
            }
            else
            {
                enabledCount++;
                Debug.Log($"[NetworkSync]   ✅ {r.name}: enabled=true");
            }
        }
        if (enabledCount == 0 && renderers.Length > 0)
        {
            Debug.LogError($"[NetworkSync] ⚠️ ВСЕ Renderer'ы были выключены для {username}! Игрок был НЕВИДИМ!");
        }
        else
        {
            Debug.Log($"[NetworkSync] ✅ Включено {renderers.Length} Renderer'ов для {username} - игрок ВИДИМ!");
        }

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

        // КРИТИЧЕСКОЕ: УДАЛИТЬ PlayerController для сетевого игрока (не просто отключить!)
        // Ищем PlayerController на ВСЕХ уровнях (root, model, children)
        PlayerController[] allPlayerControllers = playerObj.GetComponentsInChildren<PlayerController>(true);
        foreach (var pc in allPlayerControllers)
        {
            Destroy(pc); // УДАЛЯЕМ компонент полностью!
            Debug.Log($"[NetworkSync] ✅ УДАЛЁН PlayerController на {pc.gameObject.name} для {username}");
        }

        // УДАЛЯЕМ PlayerAttack (старая система) чтобы NetworkPlayer не атаковал локально
        PlayerAttack[] allPlayerAttacks = playerObj.GetComponentsInChildren<PlayerAttack>(true);
        foreach (var pa in allPlayerAttacks)
        {
            Destroy(pa);
            Debug.Log($"[NetworkSync] ✅ УДАЛЁН PlayerAttack (старый) на {pa.gameObject.name} для {username}");
        }

        // УДАЛЯЕМ PlayerAttackNew (новая система) чтобы NetworkPlayer не атаковал локально
        PlayerAttackNew[] allPlayerAttacksNew = playerObj.GetComponentsInChildren<PlayerAttackNew>(true);
        foreach (var pan in allPlayerAttacksNew)
        {
            Destroy(pan);
            Debug.Log($"[NetworkSync] ✅ УДАЛЁН PlayerAttackNew на {pan.gameObject.name} для {username}");
        }

        // УДАЛЯЕМ TargetSystem чтобы NetworkPlayer не таргетил
        TargetSystem[] allTargetSystems = playerObj.GetComponentsInChildren<TargetSystem>(true);
        foreach (var ts in allTargetSystems)
        {
            Destroy(ts);
            Debug.Log($"[NetworkSync] ✅ УДАЛЁН TargetSystem на {ts.gameObject.name} для {username}");
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

        // КРИТИЧЕСКИ ВАЖНО: Добавляем HealthSystem ПЕРЕД NetworkPlayerEntity!
        // NetworkPlayerEntity.Start() требует HealthSystem компонент
        HealthSystem healthSystem = playerObj.AddComponent<HealthSystem>();
        // HealthSystem автоматически инициализируется в Start() с maxHealth = 100
        // Если нужны другие значения HP - они должны быть в CharacterStats компоненте
        Debug.Log($"[NetworkSync] ✅ Добавлен HealthSystem для {username} (будет инициализирован в Start)");

        // EffectManager добавляется автоматически в NetworkPlayer.Awake()
        // НЕ добавляем здесь чтобы избежать дублирования
        Debug.Log($"[NetworkSync] ℹ️ EffectManager будет добавлен NetworkPlayer.Awake() для {username}");

        // КРИТИЧЕСКИ ВАЖНО: Удаляем базовый TargetableEntity из префаба (если есть)
        // NetworkPlayerEntity extends TargetableEntity, поэтому базовый компонент конфликтует!
        // GetComponent<TargetableEntity>() вернёт ПЕРВЫЙ найденный (базовый), а не NetworkPlayerEntity!
        TargetableEntity[] allTargetableEntities = playerObj.GetComponentsInChildren<TargetableEntity>(true);
        foreach (var te in allTargetableEntities)
        {
            // Удаляем ТОЛЬКО базовый TargetableEntity (не NetworkPlayerEntity!)
            if (te.GetType() == typeof(TargetableEntity))
            {
                Destroy(te);
                Debug.Log($"[NetworkSync] ✅ УДАЛЁН базовый TargetableEntity на {te.gameObject.name} для {username}");
            }
        }

        // КРИТИЧЕСКИ ВАЖНО: Добавляем NetworkPlayerEntity (НЕ Enemy!)
        // NetworkPlayerEntity УЖЕ extends TargetableEntity, поэтому Enemy НЕ НУЖЕН
        // Enemy конфликтует с NetworkPlayerEntity при GetComponent<TargetableEntity>()
        NetworkPlayerEntity netEntity = playerObj.AddComponent<NetworkPlayerEntity>();
        Debug.Log($"[NetworkSync] ✅ Добавлен NetworkPlayerEntity для {username} (Faction: OtherPlayer)");

        // ВАЖНО: Устанавливаем тег "Enemy" для системы таргетинга
        // (без добавления Enemy компонента!)
        if (!playerObj.CompareTag("Enemy"))
        {
            try
            {
                playerObj.tag = "Enemy";
                Debug.Log($"[NetworkSync] ✅ Тег 'Enemy' установлен для {username}");
            }
            catch (UnityException e)
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ Не удалось установить тег 'Enemy': {e.Message}");
            }
        }

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

        // КРИТИЧЕСКОЕ: Зарегистрировать в FogOfWar системе локального игрока
        // Если localPlayer уже заспавнен - берем FogOfWar от него
        // Если еще нет (NetworkPlayers спавнятся раньше) - ищем любой FogOfWar в сцене
        FogOfWar fogOfWar = null;
        if (localPlayer != null)
        {
            fogOfWar = localPlayer.GetComponent<FogOfWar>();
            Debug.Log($"[NetworkSync] 🔍 localPlayer найден, получаем FogOfWar от него");
        }
        else
        {
            // ВАЖНО: localPlayer еще не заспавнен, ищем FogOfWar в сцене
            fogOfWar = FindAnyObjectByType<FogOfWar>();
            if (fogOfWar != null)
            {
                Debug.Log($"[NetworkSync] 🔍 localPlayer еще не готов, найден FogOfWar в сцене: {fogOfWar.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[NetworkSync] ⚠️ FogOfWar не найден! NetworkPlayer {username} НЕ будет зарегистрирован!");
                Debug.LogWarning($"[NetworkSync] ⚠️ Игрок может быть НЕВИДИМ из-за FogOfWar!");
            }
        }

        if (fogOfWar != null)
        {
            fogOfWar.RegisterEnemy(enemyComponent);
            Debug.Log($"[NetworkSync] ✅✅✅ Сетевой игрок {username} зарегистрирован в FogOfWar! Теперь виден в Fog!");
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
                return "Ethereal_Skull_1020210937_texture"; // ИСПРАВЛЕНО: используем правильный префаб из BasicAttackConfig_Rogue
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
    
    /// <summary>
    /// Проверить есть ли параметр в Animator
    /// </summary>
    private bool HasAnimatorParameter(Animator anim, string paramName)
    {
        if (anim == null) return false;

        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
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
    public bool gameStarted; // КРИТИЧНО: Флаг от сервера что игра УЖЕ идёт (MMO режим)
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
    public Vector3Data spawnPosition; // ✅ НОВОЕ: Реальные координаты spawn point от сервера!
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
    public string socketId;        // Кто умер
    public string killerId;        // Кто убил (может быть null)
    public long timestamp;         // Время смерти
    public float respawnTime;      // Время до респавна (мс)
    public int victimLevel;        // Уровень жертвы (для расчёта опыта в ExperienceRewardSystem)
}

/// <summary>
/// Player damaged event (PvP) - НОВОЕ для синхронизации урона через сервер
/// </summary>
[Serializable]
public class PlayerDamagedEvent
{
    public string targetSocketId;
    public string attackerSocketId;
    public string attackerName;
    public float damage;
    public float currentHealth;
    public float maxHealth;
    public long timestamp;
}

/// <summary>
/// Player healed event - для синхронизации лечения через сервер
/// </summary>
[Serializable]
public class PlayerHealedEvent
{
    public string targetSocketId;
    public string healerSocketId;
    public string healerName;
    public float healAmount;
    public float currentHealth;
    public float maxHealth;
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
    public int spawnIndex;
    public float maxHealth;
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
    public Vector3Data spawnPosition; // ✅ НОВОЕ: Реальные координаты spawn point от сервера!
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

/// <summary>
/// Effect applied event (НОВОЕ - для синхронизации статус-эффектов: Stun, Root, Buffs, Debuffs)
/// </summary>
[Serializable]
public class EffectAppliedEvent
{
    public string socketId; // Кто применил эффект (кастер)
    public string casterUsername; // Имя кастера
    public string targetSocketId; // На кого применён эффект (пустая строка = на себя)
    public string effectType; // Тип эффекта (Stun, Root, IncreaseAttack и т.д.)
    public float duration; // Длительность эффекта в секундах
    public float power; // Сила эффекта (процент для баффов/дебаффов, урон для DoT)
    public float tickInterval; // Интервал тика для DoT/HoT
    public string particleEffectPrefabName; // Название prefab'а частиц (если есть)
    public long timestamp;
}

/// <summary>
/// Minion summoned event (НОВОЕ - для синхронизации призыва миньонов: Skeleton, и т.д.)
/// </summary>
[Serializable]
public class MinionSummonedEvent
{
    public string ownerSocketId; // Socket ID владельца (некроманта)
    public string minionType; // Тип миньона ("skeleton", "demon", и т.д.)
    public float positionX; // X координата спавна
    public float positionY; // Y координата спавна
    public float positionZ; // Z координата спавна
    public float rotationY; // Y ротация (поворот)
    public float duration; // Длительность существования миньона (секунды)
    public float damage; // Базовый урон миньона
    public float intelligenceScaling; // Скейлинг урона от Intelligence владельца
    public long timestamp;
}

/// <summary>
/// Minion animation event (НОВОЕ - для синхронизации анимаций миньонов)
/// </summary>
[Serializable]
public class MinionAnimationEvent
{
    public string minionId; // Уникальный ID миньона
    public string ownerSocketId; // Socket ID владельца
    public string animation; // Название анимации ("Idle", "Walking", "Attack")
    public float speed; // Скорость анимации (обычно 1.0)
}

/// <summary>
/// Событие уничтожения миньона (для десериализации JSON от сервера)
/// </summary>
[Serializable]
public class MinionDestroyedEvent
{
    public string minionId; // Уникальный ID миньона
    public string ownerSocketId; // Socket ID владельца
}
