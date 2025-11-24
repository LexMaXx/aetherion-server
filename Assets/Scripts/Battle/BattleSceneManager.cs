using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// НОВАЯ СИСТЕМА: Менеджер боевой сцены
/// Загружает персонажей напрямую из Resources/Characters с компонентами из префабов
/// Первый игрок создаёт комнату, остальные подключаются к нему
/// </summary>
public class BattleSceneManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints; // 0-19 точек для 20 игроков
    [SerializeField] private Vector3 defaultSpawnPosition = new Vector3(0, 0, 0);

    [Header("Camera")]
    [SerializeField] private Camera battleCamera;

    [Header("Fog of War")]
    [Tooltip("Глобальные настройки Fog of War для всех персонажей")]
    [SerializeField] private FogOfWarSettings fogOfWarSettings;

    [Header("UI")]
    [SerializeField] private BattleSceneUIManager uiManager;
    [Tooltip("Опционально: Prefab для SimpleStatsUI (если не задан - создаётся программно)")]
    [SerializeField] private GameObject simpleStatsUIPrefab;

    [Header("Multiplayer")]
    [SerializeField] private GameObject networkSyncManagerPrefab;

    private GameObject spawnedLocalPlayer;
    private bool isMultiplayer = false;
    private bool isRoomHost = false; // Первый игрок = хост комнаты
    private int assignedSpawnIndex = -1;
    private string roomId = "";

    // КРИТИЧЕСКИЕ ФЛАГИ для синхронизации (как в ArenaManager):
    private bool spawnIndexReceived = false; // Флаг получения spawnIndex от сервера
    private bool gameStarted = false; // Флаг старта игры (после game_start события)

    /// <summary>
    /// Singleton instance
    /// </summary>
    public static BattleSceneManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnDestroy()
    {
        // При выходе из BattleScene - покидаем комнату
        LeaveRoom();
    }

    void OnApplicationQuit()
    {
        // При закрытии приложения - покидаем комнату
        LeaveRoom();
    }

    void Start()
    {
        Debug.Log("[BattleSceneManager] 🎮 НОВАЯ СИСТЕМА загрузки персонажей");

        // КРИТИЧЕСКОЕ: Загружаем spawn points ПЕРЕД всем остальным
        LoadSpawnPoints();

        // КРИТИЧЕСКИ ВАЖНО: Передаем spawn points в NetworkSyncManager СРАЗУ!
        // Это нужно делать ДО SetupMultiplayer(), потому что NetworkSyncManager может уже существовать в сцене
        // ИСПРАВЛЕНИЕ: Делаем это ВСЕГДА (даже если NetworkSyncManager уже имеет spawn points)
        // Потому что при переходе WorldMap → BattleScene спавн поинты могут потеряться
        if (NetworkSyncManager.Instance != null)
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Debug.Log($"[BattleSceneManager] 🔄 РАННЯЯ ПЕРЕДАЧА: Передаём {spawnPoints.Length} spawn points в NetworkSyncManager (ПЕРЕЗАПИСЬ старых!)");
                NetworkSyncManager.Instance.SetSpawnPoints(spawnPoints);
            }
            else
            {
                Debug.LogError("[BattleSceneManager] ❌ spawnPoints пустой после LoadSpawnPoints()!");
            }
        }
        else
        {
            Debug.LogWarning("[BattleSceneManager] ⚠️ NetworkSyncManager не найден в Start() - spawn points будут переданы позже в SetupMultiplayer()");
        }

        // Проверяем режим игры
        CheckGameMode();

        // Настраиваем мультиплеер если нужно
        if (isMultiplayer)
        {
            SetupMultiplayer();

            // ИЗМЕНЕНО: INSTANT SPAWN - спавним игрока сразу (нет лобби, нет ожидания)
            Debug.Log("[BattleSceneManager] 🚀 INSTANT SPAWN - спавним игрока сразу!");

            // Скрываем MatchmakingUI (если есть) - он больше не нужен
            MatchmakingUI matchmakingUI = FindObjectOfType<MatchmakingUI>();
            if (matchmakingUI != null)
            {
                matchmakingUI.HideLobby();
            }

            // Спавним игрока сразу
            SpawnLocalPlayer();
        }
        else
        {
            // Одиночная игра - спавним сразу
            SpawnLocalPlayer();
        }
    }

    /// <summary>
    /// Загрузить spawn points автоматически (КРИТИЧЕСКОЕ для мультиплеера!)
    /// </summary>
    private void LoadSpawnPoints()
    {
        Debug.Log("[BattleSceneManager] 🔍 === ЗАГРУЗКА SPAWN POINTS ===");

        // Если уже назначены в Inspector - отлично!
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Debug.Log($"[BattleSceneManager] ✅ Spawn points уже назначены в Inspector: {spawnPoints.Length} точек");

            // Валидация: проверяем что все точки не null
            int validPoints = 0;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    validPoints++;
                    Debug.Log($"[BattleSceneManager]   ✅ SpawnPoint[{i}]: {spawnPoints[i].name} at {spawnPoints[i].position}");
                }
                else
                {
                    Debug.LogError($"[BattleSceneManager]   ❌ SpawnPoint[{i}] is NULL!");
                }
            }
            Debug.Log($"[BattleSceneManager] ✅ Валидация: {validPoints}/{spawnPoints.Length} точек корректны");
            return;
        }

        // Автоматическая загрузка из сцены
        Debug.Log("[BattleSceneManager] 🔧 Spawn points НЕ назначены - загружаем автоматически...");

        // Ищем контейнер SpawnPoints или MultiplayerSpawnPoints
        GameObject spawnPointsContainer = GameObject.Find("SpawnPoints");

        if (spawnPointsContainer == null)
        {
            Debug.LogWarning("[BattleSceneManager] ⚠️ 'SpawnPoints' не найден, ищем 'MultiplayerSpawnPoints'...");
            spawnPointsContainer = GameObject.Find("MultiplayerSpawnPoints");
        }

        if (spawnPointsContainer == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ GameObject 'SpawnPoints' или 'MultiplayerSpawnPoints' НЕ НАЙДЕН в сцене!");
            Debug.LogError("[BattleSceneManager] ❌ Создайте контейнер с 20 дочерними точками!");
            return;
        }

        Debug.Log($"[BattleSceneManager] ✅ Найден контейнер '{spawnPointsContainer.name}'");

        int childCount = spawnPointsContainer.transform.childCount;
        Debug.Log($"[BattleSceneManager] ✅ Контейнер '{spawnPointsContainer.name}' содержит {childCount} точек спавна");

        if (childCount == 0)
        {
            Debug.LogError("[BattleSceneManager] ❌ Контейнер 'SpawnPoints' пустой! Добавьте точки спавна!");
            return;
        }

        // Загружаем все дочерние точки
        spawnPoints = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
        {
            spawnPoints[i] = spawnPointsContainer.transform.GetChild(i);
            Debug.Log($"[BattleSceneManager]   [{i}] {spawnPoints[i].name} at {spawnPoints[i].position}");
        }

        Debug.Log($"[BattleSceneManager] ✅✅✅ Автоматически загружено {spawnPoints.Length} spawn points!");

        // КРИТИЧЕСКОЕ ЛОГИРОВАНИЕ: Выводим ПЕРВЫЕ 5 ТОЧЕК КРАСНЫМ для visibility в билде!
        Debug.LogError($"[BattleSceneManager] 🔥🔥🔥 CRITICAL DEBUG - SPAWN POINTS COORDINATES:");
        for (int i = 0; i < Mathf.Min(5, spawnPoints.Length); i++)
        {
            if (spawnPoints[i] != null)
            {
                Debug.LogError($"[BattleSceneManager] 🔥 [{i}] {spawnPoints[i].name} = ({spawnPoints[i].position.x:F2}, {spawnPoints[i].position.y:F2}, {spawnPoints[i].position.z:F2})");
            }
            else
            {
                Debug.LogError($"[BattleSceneManager] 🔥 [{i}] NULL!");
            }
        }
    }

    /// <summary>
    /// Проверить режим игры (одиночный или мультиплеер)
    /// </summary>
    private void CheckGameMode()
    {
        // Проверяем есть ли roomId в PlayerPrefs
        roomId = PlayerPrefs.GetString("CurrentRoomId", "");

        // ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА: Проверяем подключен ли SocketIO
        bool isSocketConnected = SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected;

        // КРИТИЧЕСКОЕ ИЗМЕНЕНИЕ: Проверяем наличие SocketIOManager (не только подключение)
        // Если SocketIOManager существует - включаем мультиплеер режим для синхронизации
        bool hasSocketManager = SocketIOManager.Instance != null;

        // Включаем мультиплеер если:
        // 1. Есть roomId (явно указана комната)
        // 2. SocketIO уже подключен
        // 3. SocketIOManager существует (будем подключаться для синхронизации инвентаря)
        isMultiplayer = !string.IsNullOrEmpty(roomId) || isSocketConnected || hasSocketManager;

        Debug.Log($"[BattleSceneManager] 🔍 ДИАГНОСТИКА РЕЖИМА:");
        Debug.Log($"  roomId: '{roomId}' (пустой: {string.IsNullOrEmpty(roomId)})");
        Debug.Log($"  SocketIOManager существует: {hasSocketManager}");
        Debug.Log($"  SocketIO подключен: {isSocketConnected}");
        Debug.Log($"  isMultiplayer: {isMultiplayer}");

        if (isMultiplayer)
        {
            Debug.Log($"[BattleSceneManager] 🌐 MULTIPLAYER MODE (Room: {roomId})");

            // Проверяем кто мы - хост или клиент
            // Хост = тот кто первый создал комнату
            isRoomHost = PlayerPrefs.GetInt("IsRoomHost", 0) == 1;

            if (isRoomHost)
            {
                Debug.Log("[BattleSceneManager] 👑 Вы ХОСТ комнаты (первый игрок)");
            }
            else
            {
                Debug.Log("[BattleSceneManager] 🎮 Вы присоединились к комнате");
            }
        }
        else
        {
            Debug.Log("[BattleSceneManager] 🎮 SINGLEPLAYER MODE (нет SocketIOManager)");
            // Очищаем старые данные мультиплеера
            PlayerPrefs.DeleteKey("CurrentRoomId");
            PlayerPrefs.DeleteKey("IsRoomHost");
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Настроить мультиплеер (NetworkSyncManager)
    /// </summary>
    private void SetupMultiplayer()
    {
        // Создаём NetworkSyncManager если его нет
        if (NetworkSyncManager.Instance == null && networkSyncManagerPrefab != null)
        {
            GameObject networkManager = Instantiate(networkSyncManagerPrefab);
            networkManager.name = "NetworkSyncManager";
            DontDestroyOnLoad(networkManager);
            Debug.Log("[BattleSceneManager] ✅ NetworkSyncManager создан");
        }

        // КРИТИЧЕСКОЕ: Передаём spawn points СРАЗУ после создания NetworkSyncManager!
        // Это должно быть ДО любых событий от сервера!
        if (NetworkSyncManager.Instance != null)
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Debug.Log($"[BattleSceneManager] 🔄🔄🔄 РАННЕЕ НАЗНАЧЕНИЕ: Передаём {spawnPoints.Length} spawn points в NetworkSyncManager");
                NetworkSyncManager.Instance.SetSpawnPoints(spawnPoints);
            }
            else
            {
                Debug.LogError("[BattleSceneManager] ❌ spawnPoints пустой в SetupMultiplayer()!");
            }
        }

        // КРИТИЧЕСКОЕ: Автоматически подключаемся к Socket.IO если ещё не подключены!
        if (SocketIOManager.Instance != null && !SocketIOManager.Instance.IsConnected)
        {
            Debug.Log("[BattleSceneManager] 🔌 Socket.IO не подключен - подключаемся автоматически...");
            string token = PlayerPrefs.GetString("UserToken", "");

            SocketIOManager.Instance.Connect(token, (success) =>
            {
                if (success)
                {
                    Debug.Log("[BattleSceneManager] ✅ Socket.IO подключен успешно!");

                    // После подключения - присоединяемся к комнате
                    ConnectToRoom();
                }
                else
                {
                    Debug.LogError("[BattleSceneManager] ❌ Не удалось подключиться к Socket.IO!");
                }
            });
        }
        else if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
        {
            Debug.Log("[BattleSceneManager] ✅ Socket.IO уже подключен");
            ConnectToRoom();
        }
        else
        {
            Debug.LogError("[BattleSceneManager] ❌ SocketIOManager не найден!");
        }
    }

    /// <summary>
    /// Подключиться к комнате и подписаться на события
    /// </summary>
    private void ConnectToRoom()
    {
        if (SocketIOManager.Instance == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ SocketIOManager is null в ConnectToRoom()!");
            return;
        }

        // Слушаем назначение точки спавна от сервера
        SocketIOManager.Instance.On("assign_spawn_index", OnSpawnIndexReceived);

        // Если roomId пустой - создаём глобальную MMO комнату
        if (string.IsNullOrEmpty(roomId))
        {
            roomId = "aetherion-global-world";
            Debug.Log($"[BattleSceneManager] 🌍 Используем глобальную MMO комнату: {roomId}");
        }

        // Присоединяемся к комнате
        string characterClass = PlayerPrefs.GetString("SelectedCharacterClass", "Warrior");
        Debug.Log($"[BattleSceneManager] 🚪 Присоединяемся к комнате {roomId} как {characterClass}...");

        SocketIOManager.Instance.JoinRoom(roomId, characterClass, (joinSuccess) =>
        {
            if (joinSuccess)
            {
                Debug.Log($"[BattleSceneManager] ✅ Присоединились к комнате {roomId}!");

                // КРИТИЧЕСКОЕ: Загружаем инвентарь ПОСЛЕ присоединения к комнате
                // Только сейчас сервер знает о нас в activePlayers
                Debug.Log("[BattleSceneManager] 📦 ЗАДЕРЖАННАЯ ЗАГРУЗКА: Загружаем инвентарь после присоединения к комнате...");
                LoadInventoryFromServerDelayed();
            }
            else
            {
                Debug.LogError($"[BattleSceneManager] ❌ Не удалось присоединиться к комнате {roomId}!");
            }
        });

        // Запрашиваем точку спавна (формируем JSON вручную)
        string requestData = $"{{\"roomId\":\"{roomId}\"}}";
        SocketIOManager.Instance.Emit("request_spawn_index", requestData);
        Debug.Log("[BattleSceneManager] ⏳ Запросили spawn index у сервера...");
    }

    /// <summary>
    /// Callback: Получен spawn index от сервера
    /// </summary>
    private void OnSpawnIndexReceived(string data)
    {
        try
        {
            var json = JsonUtility.FromJson<SpawnIndexData>(data);
            assignedSpawnIndex = json.spawnIndex;

            Debug.Log($"[BattleSceneManager] ✅ Получен spawn index: {assignedSpawnIndex}");

            // Теперь можем спавнить персонажа
            SpawnLocalPlayer();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BattleSceneManager] ❌ Ошибка парсинга spawn index: {e.Message}");
        }
    }

    /// <summary>
    /// Проверить началась ли игра (для синхронизации с ArenaManager API)
    /// </summary>
    public bool IsGameStarted()
    {
        return gameStarted;
    }

    /// <summary>
    /// Установить индекс точки спавна от сервера (для мультиплеера)
    /// КРИТИЧЕСКОЕ: Как в ArenaManager - только устанавливаем флаг, спавн будет при game_start
    /// </summary>
    public void SetSpawnIndex(int spawnIndex)
    {
        assignedSpawnIndex = spawnIndex;
        spawnIndexReceived = true;
        Debug.Log($"[BattleSceneManager] 🎯 Сервер назначил точку спавна: #{spawnIndex}");
        Debug.Log("[BattleSceneManager] ⏳ Ждем game_start для спавна...");
    }

    /// <summary>
    /// Спавн локального игрока СЕЙЧАС (вызывается из NetworkSyncManager.OnGameStart)
    /// КРИТИЧЕСКОЕ: Проверяем что spawnIndex получен перед спавном (как в ArenaManager)
    /// </summary>
    public void SpawnLocalPlayerNow()
    {
        Debug.Log("[BattleSceneManager] 🎮 SpawnLocalPlayerNow() вызван из NetworkSyncManager!");

        // КРИТИЧЕСКОЕ: Устанавливаем флаг game started
        gameStarted = true;

        // Проверяем что spawnIndex получен (как в ArenaManager)
        if (isMultiplayer && !spawnIndexReceived)
        {
            Debug.LogError("[BattleSceneManager] ❌ game_start получен, но spawnIndex не назначен!");
            Debug.LogError("[BattleSceneManager] ❌ Невозможно заспавнить игрока без spawnIndex!");
            return;
        }

        // Проверяем что не заспавнен уже
        if (spawnedLocalPlayer != null)
        {
            Debug.LogWarning("[BattleSceneManager] ⚠️ Игрок уже заспавнен!");
            return;
        }

        Debug.Log($"[BattleSceneManager] ✅ Все условия выполнены! spawnIndex={assignedSpawnIndex}");
        SpawnLocalPlayer();
    }

    /// <summary>
    /// Спавн локального игрока
    /// НОВАЯ СИСТЕМА: Загружаем префаб напрямую из Resources/Characters
    /// </summary>
    private void SpawnLocalPlayer()
    {
        // Получаем выбранный класс из PlayerPrefs
        string selectedClass = PlayerPrefs.GetString("SelectedCharacterClass", "");

        if (string.IsNullOrEmpty(selectedClass))
        {
            // FALLBACK: Если класс не выбран - используем Warrior по умолчанию
            selectedClass = "Warrior";
            PlayerPrefs.SetString("SelectedCharacterClass", selectedClass);
            PlayerPrefs.Save();

            Debug.LogWarning("[BattleSceneManager] ⚠️ Класс персонажа не был выбран!");
            Debug.Log($"[BattleSceneManager] 🎯 Использую класс по умолчанию: {selectedClass}");
        }
        else
        {
            Debug.Log($"[BattleSceneManager] 🎯 Загрузка персонажа: {selectedClass}");
        }

        // КРИТИЧЕСКОЕ: Загружаем префаб напрямую из Resources/Characters
        // Эти префабы УЖЕ содержат все нужные компоненты!
        string prefabPath = $"Characters/{selectedClass}Model";
        GameObject characterPrefab = Resources.Load<GameObject>(prefabPath);

        if (characterPrefab == null)
        {
            Debug.LogError($"[BattleSceneManager] ❌ Префаб не найден: Resources/{prefabPath}.prefab");
            return;
        }

        Debug.Log($"[BattleSceneManager] ✅ Префаб загружен: {prefabPath}");

        // Определяем точку спавна
        Vector3 spawnPosition;
        Quaternion spawnRotation;

        Debug.Log($"[BattleSceneManager] 🔍 === ОПРЕДЕЛЕНИЕ ТОЧКИ СПАВНА ===");
        Debug.Log($"[BattleSceneManager] 🔍 isMultiplayer: {isMultiplayer}");
        Debug.Log($"[BattleSceneManager] 🔍 assignedSpawnIndex: {assignedSpawnIndex}");
        Debug.Log($"[BattleSceneManager] 🔍 spawnPoints: {(spawnPoints != null ? $"{spawnPoints.Length} точек" : "NULL")}");

        // ИЗМЕНЕНО: Для instant spawn - если индекс не назначен, используем случайную точку
        if (isMultiplayer && spawnPoints != null && spawnPoints.Length > 0 && assignedSpawnIndex >= 0 && assignedSpawnIndex < spawnPoints.Length)
        {
            // MULTIPLAYER + НАЗНАЧЕН ИНДЕКС: Используем точку от сервера
            Transform spawnTransform = spawnPoints[assignedSpawnIndex];
            if (spawnTransform != null)
            {
                spawnPosition = spawnTransform.position;
                spawnRotation = spawnTransform.rotation;
                Debug.Log($"[BattleSceneManager] ✅ MULTIPLAYER (индекс от сервера): spawn point [{assignedSpawnIndex}]: {spawnTransform.name}");
                Debug.LogError($"[BattleSceneManager] 🔥 Position: ({spawnPosition.x:F2}, {spawnPosition.y:F2}, {spawnPosition.z:F2})");
            }
            else
            {
                Debug.LogError($"[BattleSceneManager] ❌ Spawn point [{assignedSpawnIndex}] is NULL!");
                spawnPosition = defaultSpawnPosition;
                spawnRotation = Quaternion.identity;
            }
        }
        else if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // INSTANT SPAWN или SINGLEPLAYER: Используем случайную точку (индекс ещё не получен)
            Transform spawnTransform = GetRandomSpawnPoint();
            if (spawnTransform != null)
            {
                spawnPosition = spawnTransform.position;
                spawnRotation = spawnTransform.rotation;
                Debug.Log($"[BattleSceneManager] 🎯 INSTANT SPAWN: случайный spawn point '{spawnTransform.name}'");
                Debug.LogError($"[BattleSceneManager] 🔥 Position: ({spawnPosition.x:F2}, {spawnPosition.y:F2}, {spawnPosition.z:F2})");
            }
            else
            {
                Debug.LogError($"[BattleSceneManager] ❌ Random spawn point is NULL!");
                spawnPosition = defaultSpawnPosition;
                spawnRotation = Quaternion.identity;
            }
        }
        else
        {
            // Fallback - дефолтная позиция
            Debug.LogWarning($"[BattleSceneManager] ⚠️⚠️⚠️ FALLBACK TO DEFAULT SPAWN POSITION!");

            if (isMultiplayer && assignedSpawnIndex < 0)
            {
                Debug.LogError($"[BattleSceneManager] ❌ assignedSpawnIndex не назначен сервером! ({assignedSpawnIndex})");
            }
            else if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError($"[BattleSceneManager] ❌ spawnPoints пустой или NULL!");
            }

            spawnPosition = defaultSpawnPosition;
            spawnRotation = Quaternion.identity;
            Debug.Log($"[BattleSceneManager] ⚠️ Используем default spawn position: {spawnPosition}");
        }

        // НОВАЯ СИСТЕМА: Спавним префаб НАПРЯМУЮ!
        // Префаб УЖЕ содержит все компоненты: CharacterController, PlayerController, TargetSystem
        spawnedLocalPlayer = Instantiate(characterPrefab, spawnPosition, spawnRotation);
        spawnedLocalPlayer.name = $"{selectedClass}Player";

        // КРИТИЧЕСКИ ВАЖНО: Устанавливаем тег "Player" для поиска UI скриптами
        spawnedLocalPlayer.tag = "Player";

        Debug.Log($"[BattleSceneManager] ✅ Персонаж создан: {spawnedLocalPlayer.name}");
        Debug.Log($"[BattleSceneManager] 📦 Компоненты из префаба загружены автоматически");

        // КРИТИЧЕСКОЕ: Добавляем StarterAssetsInputs для мобильного джойстика
        SetupMobileInput();

        // Устанавливаем Layer
        int characterLayer = LayerMask.NameToLayer("Character");
        if (characterLayer == -1)
        {
            Debug.LogWarning("[BattleSceneManager] ⚠️ Layer 'Character' не найден, используем Default");
            characterLayer = 0;
        }
        SetLayerRecursively(spawnedLocalPlayer, characterLayer);

        // Настроить Animator (отключаем Root Motion, как в Arena)
        SetupAnimator();

        // Настраиваем оружие персонажа (ВАЖНО!)
        SetupWeaponSystem();

        // НОВОЕ: Добавляем LevelingSystem для системы прокачки
        SetupLevelingSystem();

        // КРИТИЧЕСКИ ВАЖНО: Принудительно инициализируем HealthSystem ПЕРЕД LocalPlayerEntity!
        // LocalPlayerEntity.Start() требует что HealthSystem уже инициализирован
        // Unity может вызвать Start() в непредсказуемом порядке, поэтому делаем это вручную
        EnsureHealthSystemInitialized();

        // Добавляем TargetableEntity на локального игрока ПОСЛЕ HealthSystem (БЕЗ Enemy!)
        SetupTargetableEntity();

        // Настраиваем Fog of War (система видимости)
        SetupFogOfWar();

        // Настраиваем камеру
        SetupCamera();

        // Настраиваем UI
        SetupUI();

        // ⭐ НОВОЕ: Добавляем SimpleStatsUI для прокачки (автоматически)
        SetupStatsUI();

        // Регистрируем в мультиплеере если нужно
        if (isMultiplayer && NetworkSyncManager.Instance != null)
        {
            // КРИТИЧЕСКОЕ: Передаём spawn points в NetworkSyncManager для синхронизации
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Debug.Log($"[BattleSceneManager] 🔄 Передаём {spawnPoints.Length} spawn points в NetworkSyncManager");
                NetworkSyncManager.Instance.SetSpawnPoints(spawnPoints);
            }
            else
            {
                Debug.LogError("[BattleSceneManager] ❌ spawnPoints пустой! NetworkSyncManager не сможет спавнить других игроков!");
            }

            NetworkSyncManager.Instance.SetLocalPlayer(spawnedLocalPlayer, selectedClass);
            Debug.Log("[BattleSceneManager] ✅ Игрок зарегистрирован в NetworkSyncManager");
        }

        // КРИТИЧЕСКИ ВАЖНО: Отправляем HP и stats на сервер для синхронизации!
        // Делаем это ВСЕГДА (даже в singleplayer для отладки)
        // Задержка нужна чтобы HealthSystem и CharacterStats успели инициализироваться
        if (isMultiplayer)
        {
            Debug.Log("[BattleSceneManager] 🌐 Multiplayer режим - будем отправлять stats на сервер");
            StartCoroutine(SendPlayerStatsToServerDelayed());
        }
        else
        {
            Debug.Log("[BattleSceneManager] 🎮 Singleplayer режим - stats НЕ отправляются");
        }

        // Добавляем PlayerDeathHandler для обработки смерти и респавна
        PlayerDeathHandler deathHandler = spawnedLocalPlayer.AddComponent<PlayerDeathHandler>();
        deathHandler.SetLocalPlayer(true); // Локальный игрок ДОЛЖЕН отправлять события смерти на сервер
        Debug.Log("[BattleSceneManager] ✅ PlayerDeathHandler добавлен для обработки смерти (isLocalPlayer=true)");

        // КРИТИЧЕСКИ ВАЖНО: Регистрируем персонажа в GameProgressManager для WorldMapScene
        RegisterCharacterInProgressManager(selectedClass);

        // ИСПРАВЛЕНО: НЕ загружаем инвентарь здесь!
        // Инвентарь загружается ПОСЛЕ JoinRoom callback в LoadInventoryFromServerDelayed()
        // Иначе сервер не знает о игроке в activePlayers → "Player not found"

        Debug.Log("[BattleSceneManager] ✅✅✅ ГОТОВО! Персонаж в игре!");
    }

    /// <summary>
    /// Загрузить инвентарь персонажа с сервера (из MongoDB)
    /// Вызывается после спавна персонажа
    /// </summary>
    private void LoadInventoryFromServer()
    {
        if (!isMultiplayer)
        {
            Debug.Log("[BattleSceneManager] 🎮 Singleplayer режим - инвентарь не загружается с сервера");
            return;
        }

        // Проверяем новый MongoInventoryManager
        if (AetherionMMO.Inventory.MongoInventoryManager.Instance != null)
        {
            Debug.Log("[BattleSceneManager] 📦 Загружаем MMO инвентарь с сервера...");
            AetherionMMO.Inventory.MongoInventoryManager.Instance.LoadInventoryFromServer();
        }
        // Fallback на старый InventoryManager
        else if (InventoryManager.Instance != null)
        {
            Debug.Log("[BattleSceneManager] 📦 Загружаем старый инвентарь с сервера...");
            InventoryManager.Instance.LoadInventoryFromServer();
        }
        else
        {
            Debug.LogWarning("[BattleSceneManager] ⚠️ Inventory Manager не найден, инвентарь не будет загружен");
        }
    }

    /// <summary>
    /// Загрузить инвентарь с задержкой (после присоединения к комнате)
    /// КРИТИЧЕСКОЕ: Вызывается ПОСЛЕ JoinRoom callback, когда сервер уже знает о нас
    /// </summary>
    private void LoadInventoryFromServerDelayed()
    {
        StartCoroutine(LoadInventoryWithRetry());
    }

    /// <summary>
    /// Корутина для загрузки инвентаря с повторными попытками
    /// </summary>
    private System.Collections.IEnumerator LoadInventoryWithRetry()
    {
        int maxRetries = 10;
        float retryDelay = 0.5f;

        for (int i = 0; i < maxRetries; i++)
        {
            // Проверяем новый MongoInventoryManager
            if (AetherionMMO.Inventory.MongoInventoryManager.Instance != null)
            {
                if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
                {
                    // НЕ вызываем LoadInventoryFromServer() напрямую!
                    // MongoInventoryManager сам загрузит инвентарь в своём Start() методе
                    // после того как LoadCharacterClass() установит characterClass
                    Debug.Log($"[BattleSceneManager] ✅ MongoInventoryManager обнаружен, он сам загрузит инвентарь");
                    yield break;
                }
                else
                {
                    Debug.LogWarning($"[BattleSceneManager] ⚠️ Socket.IO не подключен, ждём... (попытка {i + 1}/{maxRetries})");
                }
            }
            // Fallback на старый InventoryManager (если есть)
            else if (InventoryManager.Instance != null)
            {
                if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
                {
                    Debug.Log($"[BattleSceneManager] 📦 Загружаем старый инвентарь (попытка {i + 1})");
                    InventoryManager.Instance.LoadInventoryFromServer();
                    yield break;
                }
            }
            else
            {
                Debug.LogWarning($"[BattleSceneManager] ⚠️ Inventory Manager не найден, ждём... (попытка {i + 1}/{maxRetries})");
            }

            yield return new WaitForSeconds(retryDelay);
        }

        Debug.LogError("[BattleSceneManager] ❌ Не удалось загрузить инвентарь после всех попыток!");
    }

    /// <summary>
    /// Зарегистрировать персонажа в GameProgressManager для использования на карте мира
    /// ВАЖНО: Вызывается при спавне персонажа в BattleScene
    /// </summary>
    private void RegisterCharacterInProgressManager(string selectedClass)
    {
        if (GameProgressManager.Instance == null)
        {
            Debug.LogWarning("[BattleSceneManager] ⚠️ GameProgressManager не найден - персонаж не будет сохранён для карты мира");
            return;
        }

        // Формируем имя префаба персонажа: "Warrior" → "WarriorModel"
        string prefabName = $"{selectedClass}Model";

        // Сохраняем в GameProgressManager
        GameProgressManager.Instance.SetSelectedCharacter(prefabName);

        Debug.Log($"[BattleSceneManager] ✅ Персонаж зарегистрирован в GameProgressManager: {prefabName}");
        Debug.Log($"[BattleSceneManager] 🗺️ Этот персонаж появится на карте мира при переходе в WorldMapScene");
    }

    /// <summary>
    /// Настроить систему оружия (ClassWeaponManager)
    /// КОПИЯ из ArenaManager
    /// </summary>
    private void SetupAnimator()
    {
        if (spawnedLocalPlayer == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ SetupAnimator: персонаж не найден!");
            return;
        }

        Transform modelTransform = spawnedLocalPlayer.transform.childCount > 0 ? spawnedLocalPlayer.transform.GetChild(0) : null;
        if (modelTransform == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ Model не найден для Animator!");
            return;
        }

        Animator modelAnimator = modelTransform.GetComponent<Animator>();
        if (modelAnimator == null)
        {
            Debug.LogWarning("[BattleSceneManager] ⚠️ Animator не найден на модели!");
            return;
        }

        modelAnimator.applyRootMotion = false;
        Debug.Log($"[BattleSceneManager] ✅ Animator найден на {modelTransform.name}. Root Motion отключён.");
    }

    /// <summary>
    /// Настроить систему оружия (ClassWeaponManager)
    /// КОПИЯ из ArenaManager
    /// </summary>
    private void SetupWeaponSystem()
    {
        if (spawnedLocalPlayer == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ SetupWeaponSystem: персонаж не найден!");
            return;
        }

        // Находим Model (дочерний объект)
        Transform modelTransform = spawnedLocalPlayer.transform.GetChild(0);
        if (modelTransform == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ Model не найден для оружия!");
            return;
        }

        Debug.Log($"[BattleSceneManager] === SetupWeapons для {modelTransform.name} ===");

        // КРИТИЧЕСКИ ВАЖНО: Получаем выбранный класс из PlayerPrefs
        string selectedClass = PlayerPrefs.GetString("SelectedCharacterClass", "");
        CharacterClass characterClass = CharacterClass.Warrior; // Default

        // Преобразуем строку в enum
        if (!string.IsNullOrEmpty(selectedClass))
        {
            if (System.Enum.TryParse(selectedClass, out CharacterClass parsedClass))
            {
                characterClass = parsedClass;
                Debug.Log($"[BattleSceneManager] ✅ Класс из PlayerPrefs: {characterClass}");
            }
            else
            {
                Debug.LogWarning($"[BattleSceneManager] ⚠️ Не удалось распарсить класс '{selectedClass}', использую Warrior");
            }
        }

        // Проверяем есть ли уже ClassWeaponManager на префабе
        ClassWeaponManager weaponManager = modelTransform.GetComponent<ClassWeaponManager>();
        if (weaponManager == null)
        {
            Debug.Log("[BattleSceneManager] Добавляем ClassWeaponManager...");
            weaponManager = modelTransform.gameObject.AddComponent<ClassWeaponManager>();

            // Проверяем WeaponDatabase
            WeaponDatabase db = WeaponDatabase.Instance;
            if (db == null)
            {
                Debug.LogError("[BattleSceneManager] ❌ WeaponDatabase не найдена! Создайте через Tools → Create Weapon Database");
            }
            else
            {
                Debug.Log("[BattleSceneManager] ✓ WeaponDatabase найдена");
            }

            // КРИТИЧЕСКИ ВАЖНО: Устанавливаем класс вручную ПЕРЕД AttachWeaponForClass()
            weaponManager.SetCharacterClass(characterClass);

            // Прикрепляем оружие
            weaponManager.AttachWeaponForClass();
            Debug.Log($"[BattleSceneManager] ✓ Оружие добавлено для {modelTransform.name} (класс: {characterClass})");
        }
        else
        {
            Debug.Log("[BattleSceneManager] ✓ ClassWeaponManager уже существует на префабе");

            // КРИТИЧЕСКИ ВАЖНО: Устанавливаем класс вручную ПЕРЕД AttachWeaponForClass()
            weaponManager.SetCharacterClass(characterClass);

            // Переприкрепляем оружие на всякий случай
            weaponManager.AttachWeaponForClass();
        }
    }

    /// <summary>
    /// Добавить TargetableEntity на локального игрока (НЕ Enemy!)
    /// КРИТИЧЕСКОЕ: Enemy добавляется только на ДРУГИХ игроков для FogOfWar
    /// </summary>
    private void SetupTargetableEntity()
    {
        if (spawnedLocalPlayer == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ SetupTargetableEntity: персонаж не найден!");
            return;
        }

        // Находим Model (дочерний объект)
        Transform modelTransform = spawnedLocalPlayer.transform.GetChild(0);
        if (modelTransform == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ Model не найден для TargetableEntity!");
            return;
        }

        Debug.Log($"[BattleSceneManager] === SetupTargetableEntity для {modelTransform.name} ===");

        // В multiplayer режиме используем LocalPlayerEntity (отправляет урон на сервер)
        // В singleplayer используем обычный TargetableEntity
        TargetableEntity playerEntity = modelTransform.GetComponent<TargetableEntity>();

        if (isMultiplayer)
        {
            // MULTIPLAYER: Используем LocalPlayerEntity для отправки урона на сервер
            LocalPlayerEntity localEntity = modelTransform.GetComponent<LocalPlayerEntity>();
            if (localEntity == null)
            {
                localEntity = modelTransform.gameObject.AddComponent<LocalPlayerEntity>();
                Debug.Log("[BattleSceneManager] ✓ Добавлен LocalPlayerEntity (multiplayer)");
            }
            playerEntity = localEntity; // LocalPlayerEntity наследует TargetableEntity
        }
        else
        {
            // SINGLEPLAYER: Используем обычный TargetableEntity
            if (playerEntity == null)
            {
                playerEntity = modelTransform.gameObject.AddComponent<TargetableEntity>();
                Debug.Log("[BattleSceneManager] ✓ Добавлен TargetableEntity (singleplayer)");
            }
        }

        // Получаем класс персонажа
        string selectedClass = PlayerPrefs.GetString("SelectedCharacterClass", "Unknown");

        // Настраиваем TargetableEntity
        playerEntity.SetEntityName(selectedClass);
        playerEntity.SetFaction(Faction.Player); // ВАЖНО! Faction.Player = нельзя таргетить себя

        // В multiplayer используем socketId, в singleplayer - userToken
        string ownerId;
        if (isMultiplayer && NetworkSyncManager.Instance != null)
        {
            ownerId = NetworkSyncManager.Instance.LocalPlayerSocketId;
            Debug.Log($"[BattleSceneManager] 🌐 Multiplayer: ownerId = socketId ({ownerId})");
        }
        else
        {
            ownerId = PlayerPrefs.GetString("UserToken", "local_player");
            Debug.Log($"[BattleSceneManager] 🎮 Singleplayer: ownerId = userToken ({ownerId})");
        }

        playerEntity.SetOwnerId(ownerId);

        Debug.Log($"[BattleSceneManager] ✅ TargetableEntity настроен: {selectedClass} (Faction: Player, ID: {ownerId})");

        // КРИТИЧЕСКИ ВАЖНО: Добавляем EffectManager для статус-эффектов (Stun, DoT, Buffs)
        EffectManager effectManager = modelTransform.GetComponent<EffectManager>();
        if (effectManager == null)
        {
            effectManager = modelTransform.gameObject.AddComponent<EffectManager>();
            Debug.Log("[BattleSceneManager] ✅ Добавлен EffectManager для локального игрока (для Stun, Root, DoT, Buffs)");
        }
        else
        {
            Debug.Log("[BattleSceneManager] ✓ EffectManager уже существует на префабе");
        }

        // КРИТИЧЕСКОЕ: НЕ добавляем Enemy компонент на локального игрока!
        // Enemy добавляется ТОЛЬКО на NetworkPlayer (удаленных игроков) для FogOfWar
        Debug.Log("[BattleSceneManager] ⚠️ Enemy НЕ добавлен на локального игрока (чтобы не скрывать себя в FogOfWar)");

        // ДИАГНОСТИКА: Проверяем что Enemy компонент ОТСУТСТВУЕТ на локальном игроке
        CheckAndRemoveEnemyComponent();
    }

    /// <summary>
    /// Настроить систему прокачки (LevelingSystem)
    /// Добавляется автоматически при создании персонажа
    /// </summary>
    private void SetupLevelingSystem()
    {
        if (spawnedLocalPlayer == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ SetupLevelingSystem: персонаж не найден!");
            return;
        }

        // Находим Model (дочерний объект)
        Transform modelTransform = spawnedLocalPlayer.transform.GetChild(0);
        if (modelTransform == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ Model не найден для LevelingSystem!");
            return;
        }

        Debug.Log($"[BattleSceneManager] === SetupLevelingSystem для {modelTransform.name} ===");

        // Проверяем есть ли CharacterStats (обязательно для LevelingSystem)
        CharacterStats characterStats = modelTransform.GetComponent<CharacterStats>();

        // Если не на Model, ищем на родительском объекте или в детях
        if (characterStats == null)
        {
            Debug.LogWarning($"[BattleSceneManager] CharacterStats не на Model, ищем в родительском объекте...");
            characterStats = spawnedLocalPlayer.GetComponent<CharacterStats>();
        }

        if (characterStats == null)
        {
            Debug.LogWarning($"[BattleSceneManager] CharacterStats не на родителе, ищем в детях...");
            characterStats = spawnedLocalPlayer.GetComponentInChildren<CharacterStats>();
        }

        if (characterStats == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ CharacterStats не найден нигде! LevelingSystem требует CharacterStats.");
            Debug.LogError($"[BattleSceneManager] Проверьте префаб {spawnedLocalPlayer.name} - должен иметь CharacterStats компонент!");
            return;
        }

        Debug.Log($"[BattleSceneManager] ✓ CharacterStats найден на: {characterStats.gameObject.name}");

        // Проверяем есть ли уже LevelingSystem (на том же объекте где CharacterStats)
        LevelingSystem levelingSystem = characterStats.GetComponent<LevelingSystem>();
        if (levelingSystem == null)
        {
            // Добавляем LevelingSystem на тот же GameObject где CharacterStats
            levelingSystem = characterStats.gameObject.AddComponent<LevelingSystem>();
            Debug.Log($"[BattleSceneManager] ⭐ ДОБАВЛЕН LevelingSystem на {characterStats.gameObject.name}");
        }
        else
        {
            Debug.Log($"[BattleSceneManager] ✓ LevelingSystem уже существует на {characterStats.gameObject.name}");
            Debug.Log($"[BattleSceneManager] ✓ Текущий Level (ДО загрузки): {levelingSystem.CurrentLevel}, XP: {levelingSystem.CurrentExperience}, Points: {levelingSystem.AvailableStatPoints}");
        }

        // Даже если LevelingSystem существует, нужно добавить NetworkLevelingSync и ExperienceRewardSystem для онлайна
        if (isMultiplayer)
        {
            NetworkLevelingSync networkLevelingSync = characterStats.GetComponent<NetworkLevelingSync>();
            if (networkLevelingSync == null)
            {
                networkLevelingSync = characterStats.gameObject.AddComponent<NetworkLevelingSync>();
                Debug.Log($"[BattleSceneManager] ⭐ Добавлен NetworkLevelingSync на {characterStats.gameObject.name}");
            }

            ExperienceRewardSystem experienceReward = characterStats.GetComponent<ExperienceRewardSystem>();
            if (experienceReward == null)
            {
                experienceReward = characterStats.gameObject.AddComponent<ExperienceRewardSystem>();
                Debug.Log($"[BattleSceneManager] ⭐ Добавлен ExperienceRewardSystem на {characterStats.gameObject.name}");
            }
        }

        // Загружаем прогресс прокачки с сервера/PlayerPrefs
        string token = PlayerPrefs.GetString("UserToken", "");
        string characterId = PlayerPrefs.GetString("SelectedCharacterId", "");
        string selectedClass = PlayerPrefs.GetString("SelectedCharacterClass", "");

        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(characterId))
        {
            Debug.Log($"[BattleSceneManager] 📥 Загружаем прогресс из MongoDB для персонажа {selectedClass} (ID: {characterId})");
            ApiClient.Instance.LoadCharacterProgress(token, characterId,
                (response) =>
                {
                    if (response != null && response.success)
                    {
                        if (response.leveling != null)
                        {
                            Debug.Log($"[BattleSceneManager] ✅ Прогресс загружен: Level {response.leveling.level}, XP {response.leveling.experience}, Points {response.leveling.availableStatPoints}");
                            levelingSystem.ImportData(response.leveling);
                        }

                        if (response.stats != null)
                        {
                            characterStats.ImportData(response.stats);
                            Debug.Log($"[BattleSceneManager] ✅ Статы обновлены по данным сервера");
                        }

                        CharacterStatsPanel panel = FindFirstObjectByType<CharacterStatsPanel>(FindObjectsInactive.Include);
                        if (panel != null)
                        {
                            panel.ForceRefresh();
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[BattleSceneManager] ⚠️ Не удалось загрузить прогресс персонажа: {response?.message}");
                    }
                },
                (error) =>
                {
                    Debug.LogError($"[BattleSceneManager] ❌ Ошибка загрузки прогресса: {error}");
                });
        }
        else
        {
            Debug.LogWarning("[BattleSceneManager] ⚠️ Отсутствует token или выбранный персонаж, прогресс не загружен");
        }

        Debug.Log($"[BattleSceneManager] ✅ LevelingSystem настроен (Level: {levelingSystem.CurrentLevel}, MaxLevel: {levelingSystem.MaxLevel})");

        // ⭐ НОВОЕ: Добавляем NetworkLevelingSync для онлайн синхронизации прокачки
        if (isMultiplayer)
        {
            NetworkLevelingSync networkLevelingSync = characterStats.GetComponent<NetworkLevelingSync>();
            if (networkLevelingSync == null)
            {
                networkLevelingSync = characterStats.gameObject.AddComponent<NetworkLevelingSync>();
                Debug.Log($"[BattleSceneManager] ⭐ Добавлен NetworkLevelingSync на {characterStats.gameObject.name} для онлайн синхронизации прокачки");
            }
            else
            {
                Debug.Log("[BattleSceneManager] ✓ NetworkLevelingSync уже существует на персонаже");
            }

            // ⭐ НОВОЕ: Добавляем ExperienceRewardSystem для получения опыта за убийства в PvP
            ExperienceRewardSystem experienceReward = characterStats.GetComponent<ExperienceRewardSystem>();
            if (experienceReward == null)
            {
                experienceReward = characterStats.gameObject.AddComponent<ExperienceRewardSystem>();
                Debug.Log($"[BattleSceneManager] ⭐ Добавлен ExperienceRewardSystem на {characterStats.gameObject.name} для PvP наград");
            }
            else
            {
                Debug.Log("[BattleSceneManager] ✓ ExperienceRewardSystem уже существует на персонаже");
            }
        }
        else
        {
            Debug.Log("[BattleSceneManager] ℹ️ Singleplayer режим - NetworkLevelingSync и ExperienceRewardSystem не требуются");
        }
    }

    /// <summary>
    /// Настроить Fog of War (систему видимости)
    /// КОПИЯ из ArenaManager
    /// </summary>
    private void SetupFogOfWar()
    {
        if (spawnedLocalPlayer == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ SetupFogOfWar: персонаж не найден!");
            return;
        }

        // Находим Model (дочерний объект)
        Transform modelTransform = spawnedLocalPlayer.transform.GetChild(0);
        if (modelTransform == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ Model не найден для FogOfWar!");
            return;
        }

        Debug.Log($"[BattleSceneManager] === SetupFogOfWar для {modelTransform.name} ===");

        // Добавляем FogOfWar компонент
        FogOfWar fogOfWar = modelTransform.GetComponent<FogOfWar>();
        if (fogOfWar == null)
        {
            fogOfWar = modelTransform.gameObject.AddComponent<FogOfWar>();
            Debug.Log("[BattleSceneManager] ✓ Добавлен FogOfWar");
        }

        // Применяем глобальные настройки если есть
        if (fogOfWarSettings != null)
        {
            // Используем рефлексию для установки приватного поля globalSettings
            var globalSettingsField = typeof(FogOfWar).GetField("globalSettings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (globalSettingsField != null)
            {
                globalSettingsField.SetValue(fogOfWar, fogOfWarSettings);
                Debug.Log($"[BattleSceneManager] ✓ FogOfWar настроен с глобальными настройками: {fogOfWarSettings.name}");
            }
            else
            {
                Debug.LogWarning("[BattleSceneManager] Не удалось применить глобальные настройки FogOfWar через рефлексию");
            }
        }
        else
        {
            Debug.LogWarning("[BattleSceneManager] FogOfWarSettings не установлен. Используются локальные настройки.");
        }

        // ВАЖНО: Принудительно включаем ignoreHeight для поддержки высоких врагов
        var ignoreHeightField = typeof(FogOfWar).GetField("ignoreHeight",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (ignoreHeightField != null)
        {
            ignoreHeightField.SetValue(fogOfWar, true);
            Debug.Log("[BattleSceneManager] ✓ FogOfWar: ignoreHeight = TRUE (враги видны на любой высоте)");
        }

        // Устанавливаем большое значение maxHeightDifference
        var maxHeightDifferenceField = typeof(FogOfWar).GetField("maxHeightDifference",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (maxHeightDifferenceField != null)
        {
            maxHeightDifferenceField.SetValue(fogOfWar, 1000f);
            Debug.Log("[BattleSceneManager] ✓ FogOfWar: maxHeightDifference = 1000м");
        }

        // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Отключаем FogOfWar для NetworkPlayer в PvP арене!
        // В BattleScene все игроки должны быть ВСЕГДА ВИДНЫ (как в CS:GO/Valorant)
        var disableFogForNetworkPlayersField = typeof(FogOfWar).GetField("disableFogForNetworkPlayers",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (disableFogForNetworkPlayersField != null)
        {
            disableFogForNetworkPlayersField.SetValue(fogOfWar, true);
            Debug.LogError("[BattleSceneManager] ✅✅✅ FogOfWar: disableFogForNetworkPlayers = TRUE (PvP Arena Mode - все игроки всегда видны!)");
        }
        else
        {
            Debug.LogError("[BattleSceneManager] ❌ Не удалось установить disableFogForNetworkPlayers! Обновите FogOfWar.cs");
        }
    }

    /// <summary>
    /// Настроить камеру для следования за игроком
    /// КОПИЯ из ArenaManager - безупречная система!
    /// </summary>
    private void SetupCamera()
    {
        if (spawnedLocalPlayer == null || battleCamera == null)
        {
            Debug.LogError("[BattleSceneManager] ⚠️ SetupCamera: spawnedLocalPlayer или battleCamera = null!");
            return;
        }

        // Находим Model (дочерний объект персонажа)
        Transform modelTransform = spawnedLocalPlayer.transform.GetChild(0);
        if (modelTransform == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ Model не найден для камеры!");
            return;
        }
        Debug.Log($"[BattleSceneManager] ✓ Камера нацелена на: {modelTransform.name}");

        // ВАЖНО: Удаляем ВСЕ старые компоненты камеры
        CameraFollow[] oldFollows = battleCamera.GetComponents<CameraFollow>();
        foreach (CameraFollow cf in oldFollows)
        {
            Destroy(cf);
            Debug.Log("[BattleSceneManager] ✓ Удален старый CameraFollow");
        }

        // Удаляем все старые TPSCameraController (десктопная версия)
        TPSCameraController[] oldTPS = battleCamera.GetComponents<TPSCameraController>();
        foreach (TPSCameraController tps in oldTPS)
        {
            Destroy(tps);
            Debug.Log("[BattleSceneManager] ✓ Удален старый TPSCameraController (desktop)");
        }

        // Удаляем старые TouchCameraController если есть
        TouchCameraController[] oldTouch = battleCamera.GetComponents<TouchCameraController>();
        foreach (TouchCameraController tc in oldTouch)
        {
            Destroy(tc);
            Debug.Log("[BattleSceneManager] ✓ Удален старый TouchCameraController");
        }

        // НОВОЕ: Добавляем TouchCameraController (поддерживает и десктоп, и мобильные)
        TouchCameraController touchCamera = battleCamera.gameObject.AddComponent<TouchCameraController>();

        // ВАЖНО: Устанавливаем target на Model (а не на родителя!)
        touchCamera.SetTarget(modelTransform);

        // НОВОЕ: Находим VirtualJoystick и передаём в камеру для проверки состояния при zoom
        VirtualJoystick joystick = FindObjectOfType<VirtualJoystick>();
        if (joystick != null)
        {
            touchCamera.SetVirtualJoystick(joystick);
            Debug.Log($"[BattleSceneManager] ✅ VirtualJoystick передан в камеру для проверки pinch-to-zoom");
        }
        else
        {
            Debug.LogWarning($"[BattleSceneManager] ⚠️ VirtualJoystick не найден! Pinch-to-zoom будет работать всегда.");
        }

        Debug.Log($"[BattleSceneManager] ✅ Настроена Touch камера (desktop + mobile), target = {modelTransform.name}");
    }

    /// <summary>
    /// Настроить UI для локального игрока
    /// </summary>
    private void SetupUI()
    {
        Debug.Log("[BattleSceneManager] 🎨 === SetupUI вызван ===");

        if (spawnedLocalPlayer == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ SetupUI: персонаж не найден!");
            return;
        }

        Debug.Log($"[BattleSceneManager] ✅ Персонаж для UI: {spawnedLocalPlayer.name}");

        if (uiManager == null)
        {
            Debug.LogError("[BattleSceneManager] ❌❌❌ BattleSceneUIManager == NULL! UI НЕ БУДЕТ работать!");
            Debug.LogError("[BattleSceneManager] 💡 Открой BattleScene → Найди BattleSceneManager в Hierarchy → Inspector → назначь UI Manager!");
            return;
        }

        Debug.Log($"[BattleSceneManager] ✅ UIManager найден: {uiManager.gameObject.name}");
        Debug.Log($"[BattleSceneManager] 🎯 Вызываем uiManager.Initialize({spawnedLocalPlayer.name})...");

        // Инициализируем UI с игроком
        uiManager.Initialize(spawnedLocalPlayer);

        Debug.Log("[BattleSceneManager] ✅ UI инициализирован");
    }

    /// <summary>
    /// Настроить компоненты для мобильного input (джойстик)
    /// КРИТИЧЕСКОЕ: StarterAssetsInputs нужен для работы джойстика!
    /// </summary>
    private void SetupMobileInput()
    {
        if (spawnedLocalPlayer == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ SetupMobileInput: персонаж не найден!");
            return;
        }

        Debug.Log($"[BattleSceneManager] 🎮 === Настройка мобильного input для {spawnedLocalPlayer.name} ===");

        // Проверяем есть ли StarterAssetsInputs
        StarterAssets.StarterAssetsInputs starterInputs = spawnedLocalPlayer.GetComponent<StarterAssets.StarterAssetsInputs>();

        if (starterInputs == null)
        {
            Debug.Log($"[BattleSceneManager] ⚠️ StarterAssetsInputs НЕ НАЙДЕН на {spawnedLocalPlayer.name}");
            Debug.Log($"[BattleSceneManager] 🔧 Добавляем StarterAssetsInputs...");

            // АВТОМАТИЧЕСКИ ДОБАВЛЯЕМ!
            starterInputs = spawnedLocalPlayer.AddComponent<StarterAssets.StarterAssetsInputs>();

            if (starterInputs != null)
            {
                Debug.Log($"[BattleSceneManager] ✅✅✅ StarterAssetsInputs УСПЕШНО добавлен на {spawnedLocalPlayer.name}!");
                Debug.Log($"[BattleSceneManager] 📋 Проверка: starterInputs != null = {starterInputs != null}");
            }
            else
            {
                Debug.LogError($"[BattleSceneManager] ❌❌❌ ОШИБКА! StarterAssetsInputs НЕ УДАЛОСЬ добавить!");
            }
        }
        else
        {
            Debug.Log($"[BattleSceneManager] ✅ StarterAssetsInputs уже существует на {spawnedLocalPlayer.name}");
        }

        // Финальная проверка
        var finalCheck = spawnedLocalPlayer.GetComponent<StarterAssets.StarterAssetsInputs>();
        if (finalCheck != null)
        {
            Debug.Log($"[BattleSceneManager] ✅ ФИНАЛЬНАЯ ПРОВЕРКА: StarterAssetsInputs ТОЧНО есть на {spawnedLocalPlayer.name}");
        }
        else
        {
            Debug.LogError($"[BattleSceneManager] ❌ ФИНАЛЬНАЯ ПРОВЕРКА: StarterAssetsInputs ОТСУТСТВУЕТ на {spawnedLocalPlayer.name}!");
        }

        // Проверяем PlayerController (должен быть на префабе)
        PlayerController pc = spawnedLocalPlayer.GetComponent<PlayerController>();
        if (pc == null)
        {
            Debug.LogWarning("[BattleSceneManager] ⚠️ PlayerController не найден! Джойстик не будет работать без PlayerController!");
        }
        else
        {
            Debug.Log("[BattleSceneManager] ✅ PlayerController найден - готов принимать input от джойстика");
        }

        Debug.Log("[BattleSceneManager] ✅ Мобильный input настроен! Джойстик будет работать автоматически.");
    }

    /// <summary>
    /// Установить Layer рекурсивно для объекта и всех детей
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    /// <summary>
    /// Покинуть комнату при выходе из сцены
    /// ВАЖНО: Удаляет комнату если мы были последним игроком
    /// </summary>
    private void LeaveRoom()
    {
        if (!isMultiplayer || string.IsNullOrEmpty(roomId))
        {
            return; // Не в мультиплеере
        }

        Debug.Log($"[BattleSceneManager] 🚪 Покидаем комнату: {roomId}");

        // Отключаемся от сервера и покидаем комнату
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.LeaveRoom((success) =>
            {
                if (success)
                {
                    Debug.Log("[BattleSceneManager] ✅ Успешно покинули комнату на сервере");
                }
                else
                {
                    Debug.LogWarning("[BattleSceneManager] ⚠️ Ошибка при выходе из комнаты");
                }
            });
        }

        // Очищаем PlayerPrefs
        PlayerPrefs.DeleteKey("CurrentRoomId");
        PlayerPrefs.DeleteKey("IsRoomHost");
        PlayerPrefs.Save();

        Debug.Log("[BattleSceneManager] ✅ Комната покинута, данные очищены");
    }

    // УДАЛЕНО: OnMatchmakingMatchStart больше не нужен (instant spawn)
    // При instant spawn игрок спавнится сразу в Start(), нет ожидания таймера

    /// <summary>
    /// Отправить HP и stats на сервер для синхронизации (с задержкой)
    /// КРИТИЧЕСКИ ВАЖНО: Ждём пока CharacterStats РЕАЛЬНО рассчитает HP!
    /// </summary>
    private System.Collections.IEnumerator SendPlayerStatsToServerDelayed()
    {
        Debug.Log("[BattleSceneManager] ⏳ Ожидание инициализации CharacterStats и HealthSystem...");

        CharacterStats characterStats = spawnedLocalPlayer.GetComponent<CharacterStats>();
        HealthSystem healthSystem = spawnedLocalPlayer.GetComponent<HealthSystem>();

        if (characterStats == null || healthSystem == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ CharacterStats или HealthSystem не найдены!");
            yield break;
        }

        // КРИТИЧЕСКИ ВАЖНО: Ждём пока CharacterStats.MaxHealth > 100 (т.е. рассчитан!)
        // Базовое HP = 1000, если меньше = CharacterStats.Start() ещё не выполнился
        int maxAttempts = 50; // 50 * 0.02s = 1 секунда максимум
        int attempts = 0;

        while (characterStats.MaxHealth < 1000f && attempts < maxAttempts)
        {
            yield return new WaitForSeconds(0.02f); // 20ms между проверками
            attempts++;
        }

        if (attempts >= maxAttempts)
        {
            Debug.LogError($"[BattleSceneManager] ⚠️ CharacterStats.MaxHealth всё ещё {characterStats.MaxHealth} после {maxAttempts} попыток!");
            Debug.LogError("[BattleSceneManager] ⚠️ Отправляем stats но HP может быть некорректным!");
        }
        else
        {
            Debug.Log($"[BattleSceneManager] ✅ CharacterStats инициализирован за {attempts * 20}ms. MaxHealth: {characterStats.MaxHealth}");
        }

        // Дополнительно ждём что HealthSystem получил HP из CharacterStats
        yield return new WaitForSeconds(0.05f);

        // Теперь отправляем stats - HP уже рассчитан!
        SendPlayerStatsToServer();

        Debug.Log($"[BattleSceneManager] ⏰ Stats отправлены после {(attempts * 20) + 50}ms ожидания");
    }

    /// <summary>
    /// Отправить HP и stats на сервер для синхронизации
    /// Вызывается из корутины SendPlayerStatsToServerDelayed()
    /// </summary>
    private void SendPlayerStatsToServer()
    {
        if (spawnedLocalPlayer == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ spawnedLocalPlayer == null, не могу отправить stats!");
            return;
        }

        // Получаем компоненты
        HealthSystem healthSystem = spawnedLocalPlayer.GetComponent<HealthSystem>();
        CharacterStats characterStats = spawnedLocalPlayer.GetComponent<CharacterStats>();

        if (healthSystem == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ HealthSystem не найден на локальном игроке!");
            return;
        }

        if (characterStats == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ CharacterStats не найден на локальном игроке!");
            return;
        }

        // Собираем данные
        float maxHealth = healthSystem.MaxHealth;
        float currentHealth = healthSystem.CurrentHealth;

        // ПРОВЕРКА: Если MaxHealth меньше 1000 = что-то пошло не так!
        if (maxHealth < 1000f)
        {
            Debug.LogError($"[BattleSceneManager] ⚠️⚠️⚠️ ВНИМАНИЕ! MaxHealth = {maxHealth} (меньше 1000!)");
            Debug.LogError($"[BattleSceneManager] CharacterStats.MaxHealth = {characterStats.MaxHealth}");
            Debug.LogError($"[BattleSceneManager] CharacterStats.Endurance = {characterStats.endurance}");
            Debug.LogError("[BattleSceneManager] Возможно StatsFormulas.asset ещё не обновлён!");
            Debug.LogError("[BattleSceneManager] Запусти: Tools → Update Stats Formulas (HP x10)");
        }

        // КРИТИЧЕСКИ ВАЖНО: Используем serializable классы вместо anonymous types!
        // JsonUtility.ToJson() возвращает "{}" для anonymous types
        PlayerStatsData stats = new PlayerStatsData
        {
            strength = characterStats.strength,
            perception = characterStats.perception,
            endurance = characterStats.endurance,
            wisdom = characterStats.wisdom,
            intelligence = characterStats.intelligence,
            agility = characterStats.agility,
            luck = characterStats.luck
        };

        // Формируем JSON
        UpdatePlayerStatsData data = new UpdatePlayerStatsData
        {
            maxHealth = maxHealth,
            currentHealth = currentHealth,
            stats = stats
        };

        string json = JsonUtility.ToJson(data);

        Debug.Log($"[BattleSceneManager] 📊 Отправка stats на сервер:");
        Debug.Log($"  MaxHealth: {maxHealth}");
        Debug.Log($"  CurrentHealth: {currentHealth}");
        Debug.Log($"  Endurance: {characterStats.endurance}");
        Debug.Log($"  JSON: {json}");

        // Отправляем на сервер через SocketIOManager
        if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
        {
            SocketIOManager.Instance.Emit("update_player_stats", json);
            Debug.Log("[BattleSceneManager] ✅ Stats отправлены на сервер!");
        }
        else
        {
            Debug.LogWarning("[BattleSceneManager] ⚠️ SocketIOManager не подключен, stats не отправлены");
        }
    }

    /// <summary>
    /// Вспомогательный класс для парсинга spawn index
    /// </summary>
    [System.Serializable]
    private class SpawnIndexData
    {
        public int spawnIndex;
    }

    /// <summary>
    /// Serializable класс для отправки stats на сервер
    /// КРИТИЧЕСКИ ВАЖНО: JsonUtility.ToJson() НЕ работает с anonymous types!
    /// </summary>
    [System.Serializable]
    private class PlayerStatsData
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
    /// Serializable класс для отправки HP и stats на сервер
    /// </summary>
    [System.Serializable]
    private class UpdatePlayerStatsData
    {
        public float maxHealth;
        public float currentHealth;
        public PlayerStatsData stats;
    }

    /// <summary>
    /// КРИТИЧЕСКАЯ ДИАГНОСТИКА: Проверить и удалить Enemy компонент с локального игрока!
    /// Enemy имеет собственную HP систему (maxHealth=200, currentHealth) которая КОНФЛИКТУЕТ с HealthSystem!
    /// Это может быть причиной преждевременной смерти (Enemy.maxHealth=200 vs HealthSystem.MaxHealth=300)
    /// </summary>
    private void CheckAndRemoveEnemyComponent()
    {
        if (spawnedLocalPlayer == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ CheckAndRemoveEnemyComponent: spawnedLocalPlayer == null!");
            return;
        }

        Debug.Log($"[BattleSceneManager] 🔍 === ДИАГНОСТИКА ENEMY КОМПОНЕНТА ===");

        // Проверяем Root
        Enemy enemyRoot = spawnedLocalPlayer.GetComponent<Enemy>();
        if (enemyRoot != null)
        {
            Debug.LogError($"[BattleSceneManager] ❌❌❌ КРИТИЧЕСКАЯ ОШИБКА: Локальный игрок имеет Enemy компонент на ROOT!");
            Debug.LogError($"[BattleSceneManager] ❌ Enemy.maxHealth = {enemyRoot.GetMaxHealth()}");
            Debug.LogError($"[BattleSceneManager] ❌ Это КОНФЛИКТУЕТ с HealthSystem.MaxHealth!");
            Debug.LogError($"[BattleSceneManager] ❌ Удаляем Enemy компонент...");
            Destroy(enemyRoot);
            Debug.Log($"[BattleSceneManager] ✅ Enemy компонент удален с Root!");
        }
        else
        {
            Debug.Log($"[BattleSceneManager] ✅ Root: OK (нет Enemy компонента)");
        }

        // Проверяем Model (дочерний объект)
        if (spawnedLocalPlayer.transform.childCount > 0)
        {
            Transform modelTransform = spawnedLocalPlayer.transform.GetChild(0);
            Enemy enemyModel = modelTransform.GetComponent<Enemy>();

            if (enemyModel != null)
            {
                Debug.LogError($"[BattleSceneManager] ❌❌❌ КРИТИЧЕСКАЯ ОШИБКА: Локальный игрок имеет Enemy компонент на MODEL!");
                Debug.LogError($"[BattleSceneManager] ❌ Enemy.maxHealth = {enemyModel.GetMaxHealth()}");
                Debug.LogError($"[BattleSceneManager] ❌ Это КОНФЛИКТУЕТ с HealthSystem.MaxHealth!");
                Debug.LogError($"[BattleSceneManager] ❌ Удаляем Enemy компонент...");
                Destroy(enemyModel);
                Debug.Log($"[BattleSceneManager] ✅ Enemy компонент удален с Model!");
            }
            else
            {
                Debug.Log($"[BattleSceneManager] ✅ Model: OK (нет Enemy компонента)");
            }
        }

    }

    /// <summary>
    /// Принудительно инициализировать HealthSystem ПОСЛЕ CharacterStats
    /// Решает проблему Start() порядка выполнения в Unity
    /// </summary>
    private void EnsureHealthSystemInitialized()
    {
        if (spawnedLocalPlayer == null) return;

        Debug.Log("[BattleSceneManager] === Проверка HealthSystem ===");

        HealthSystem healthSystem = spawnedLocalPlayer.GetComponentInChildren<HealthSystem>();
        CharacterStats characterStats = spawnedLocalPlayer.GetComponentInChildren<CharacterStats>();

        if (healthSystem == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ HealthSystem не найден!");
            return;
        }

        if (characterStats == null)
        {
            Debug.LogError("[BattleSceneManager] ❌ CharacterStats не найден!");
            return;
        }

        // КРИТИЧНО: Если CurrentHealth = 0, принудительно инициализируем
        if (healthSystem.CurrentHealth <= 0)
        {
            Debug.LogWarning($"[BattleSceneManager] ⚠️ CurrentHealth = 0! Принудительная инициализация...");
            Debug.Log($"[BattleSceneManager]   MaxHealth из CharacterStats: {characterStats.MaxHealth}");

            // Устанавливаем MaxHealth и полностью восстанавливаем HP
            healthSystem.SetMaxHealth(characterStats.MaxHealth);
            healthSystem.Revive(1.0f); // Полное HP

            Debug.Log($"[BattleSceneManager] ✅ HealthSystem принудительно инициализирован: {healthSystem.CurrentHealth}/{healthSystem.MaxHealth} HP");
        }
        else
        {
            Debug.Log($"[BattleSceneManager] ✅ HealthSystem корректен: {healthSystem.CurrentHealth}/{healthSystem.MaxHealth} HP");
        }
    }

    /// <summary>
    /// ⭐ НОВОЕ: Настроить UI прокачки (SimpleStatsUI)
    /// Создаётся автоматически при спавне игрока
    /// </summary>
    private void SetupStatsUI()
    {
        // Проверяем есть ли уже SimpleStatsUI в сцене
        SimpleStatsUI existingUI = FindFirstObjectByType<SimpleStatsUI>();
        if (existingUI != null)
        {
            Debug.Log("[BattleSceneManager] ✓ SimpleStatsUI уже существует в сцене");
            return;
        }

        // Создаём GameObject для UI
        GameObject statsUIObj = new GameObject("SimpleStatsUI");
        SimpleStatsUI statsUI = statsUIObj.AddComponent<SimpleStatsUI>();

        // ВАЖНО: Если задан prefab - привязываем его к компоненту
        if (simpleStatsUIPrefab != null)
        {
            // Используем рефлексию чтобы установить private поле statsPanelPrefab
            var fieldInfo = typeof(SimpleStatsUI).GetField("statsPanelPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fieldInfo != null)
            {
                fieldInfo.SetValue(statsUI, simpleStatsUIPrefab);
                Debug.Log("[BattleSceneManager] ✅ SimpleStatsUI создан с КАСТОМНЫМ prefab!");
            }
            else
            {
                Debug.LogWarning("[BattleSceneManager] ⚠️ Не удалось установить prefab через рефлексию");
            }
        }
        else
        {
            Debug.Log("[BattleSceneManager] ⭐ SimpleStatsUI создан! Нажмите P для открытия панели статов (программный UI)");
        }
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        List<Transform> validPoints = new List<Transform>();
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
            {
                validPoints.Add(point);
            }
        }

        if (validPoints.Count == 0)
            return null;

        return validPoints[Random.Range(0, validPoints.Count)];
    }

    public bool TryGetRandomSpawnPoint(out Vector3 position, out Quaternion rotation)
    {
        Transform spawnTransform = GetRandomSpawnPoint();
        if (spawnTransform != null)
        {
            position = spawnTransform.position;
            rotation = spawnTransform.rotation;
            return true;
        }

        position = defaultSpawnPosition;
        rotation = Quaternion.identity;
        return false;
    }
}
