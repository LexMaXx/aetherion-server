using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Менеджер синхронизации игроков на WorldMap
/// Отображает других игроков на глобальной карте в реальном времени
/// </summary>
public class WorldMapNetworkManager : MonoBehaviour
{
    public static WorldMapNetworkManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float positionSyncInterval = 0.2f; // 5 Hz для WorldMap (не нужна высокая частота)
    [SerializeField] private GameObject networkPlayerPrefab; // Простой prefab для отображения других игроков

    [Header("Character Prefabs (simplified for WorldMap)")]
    [SerializeField] private GameObject warriorPrefab;
    [SerializeField] private GameObject magePrefab;
    [SerializeField] private GameObject archerPrefab;
    [SerializeField] private GameObject roguePrefab;
    [SerializeField] private GameObject paladinPrefab;

    // Network players на WorldMap
    private Dictionary<string, GameObject> networkPlayers = new Dictionary<string, GameObject>();

    // Local player
    private GameObject localPlayer;
    private float lastPositionSync = 0f;
    private Vector3 lastSentPosition = Vector3.zero;
    private const float positionThreshold = 0.1f; // 10см для WorldMap (не нужна точность как в бою)

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

    void Start()
    {
        Debug.Log("[WorldMapNetwork] Инициализация синхронизации WorldMap");

        // Проверяем подключение к серверу
        if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
        {
            Debug.LogWarning("[WorldMapNetwork] Не подключен к серверу, синхронизация отключена");
            enabled = false;
            return;
        }

        // Находим локального игрока (тег Player)
        localPlayer = GameObject.FindGameObjectWithTag("Player");
        if (localPlayer == null)
        {
            Debug.LogWarning("[WorldMapNetwork] Локальный игрок не найден");
        }

        // Подписываемся на события
        SubscribeToNetworkEvents();

        // Уведомляем сервер что мы на WorldMap
        NotifyServerWorldMapJoin();
    }

    void Update()
    {
        if (localPlayer == null || SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
            return;

        // Синхронизируем позицию локального игрока
        if (Time.time - lastPositionSync > positionSyncInterval)
        {
            SyncLocalPlayerPosition();
            lastPositionSync = Time.time;
        }
    }

    void OnDestroy()
    {
        // Уведомляем сервер что покинули WorldMap
        NotifyServerWorldMapLeave();

        // Удаляем всех сетевых игроков
        foreach (var player in networkPlayers.Values)
        {
            if (player != null)
                Destroy(player);
        }
        networkPlayers.Clear();
    }

    /// <summary>
    /// Подписка на сетевые события WorldMap
    /// </summary>
    private void SubscribeToNetworkEvents()
    {
        if (SocketIOManager.Instance == null)
            return;

        SocketIOManager.Instance.On("world_map_players_list", OnWorldMapPlayersList);
        SocketIOManager.Instance.On("world_map_player_joined", OnWorldMapPlayerJoined);
        SocketIOManager.Instance.On("world_map_player_left", OnWorldMapPlayerLeft);
        SocketIOManager.Instance.On("world_map_player_moved", OnWorldMapPlayerMoved);

        Debug.Log("[WorldMapNetwork] Подписались на события WorldMap");
    }

    /// <summary>
    /// Уведомить сервер о входе на WorldMap
    /// </summary>
    private void NotifyServerWorldMapJoin()
    {
        if (SocketIOManager.Instance == null || localPlayer == null)
            return;

        var data = new
        {
            position = new { x = localPlayer.transform.position.x, y = localPlayer.transform.position.y, z = localPlayer.transform.position.z },
            characterClass = PlayerPrefs.GetString("SelectedCharacterClass", "Warrior"),
            username = PlayerPrefs.GetString("Username", "Player")
        };

        string json = JsonConvert.SerializeObject(data);
        SocketIOManager.Instance.Emit("world_map_join", json);

        Debug.Log($"[WorldMapNetwork] Отправлено world_map_join: {json}");
    }

    /// <summary>
    /// Уведомить сервер о выходе с WorldMap
    /// </summary>
    private void NotifyServerWorldMapLeave()
    {
        if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
            return;

        SocketIOManager.Instance.Emit("world_map_leave", "{}");
        Debug.Log("[WorldMapNetwork] Отправлено world_map_leave");
    }

    /// <summary>
    /// Синхронизация позиции локального игрока
    /// </summary>
    private void SyncLocalPlayerPosition()
    {
        if (localPlayer == null)
            return;

        Vector3 currentPosition = localPlayer.transform.position;

        // Отправляем только если позиция изменилась достаточно
        if (Vector3.Distance(currentPosition, lastSentPosition) < positionThreshold)
            return;

        var data = new
        {
            position = new { x = currentPosition.x, y = currentPosition.y, z = currentPosition.z },
            rotation = new { x = localPlayer.transform.rotation.eulerAngles.x, y = localPlayer.transform.rotation.eulerAngles.y, z = localPlayer.transform.rotation.eulerAngles.z }
        };

        string json = JsonConvert.SerializeObject(data);
        SocketIOManager.Instance.Emit("world_map_position_update", json);

        lastSentPosition = currentPosition;
    }

    /// <summary>
    /// Получен список игроков на WorldMap
    /// </summary>
    private void OnWorldMapPlayersList(string jsonData)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<WorldMapPlayersListResponse>(jsonData);
            Debug.Log($"[WorldMapNetwork] Получен список игроков на WorldMap: {response.players.Length}");

            string mySocketId = SocketIOManager.Instance.GetSocketId();

            foreach (var playerData in response.players)
            {
                // Пропускаем себя
                if (playerData.socketId == mySocketId)
                    continue;

                SpawnNetworkPlayer(playerData);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WorldMapNetwork] Ошибка парсинга world_map_players_list: {e.Message}");
        }
    }

    /// <summary>
    /// Игрок присоединился к WorldMap
    /// </summary>
    private void OnWorldMapPlayerJoined(string jsonData)
    {
        try
        {
            var playerData = JsonConvert.DeserializeObject<WorldMapPlayerData>(jsonData);
            Debug.Log($"[WorldMapNetwork] Игрок {playerData.username} присоединился к WorldMap");

            string mySocketId = SocketIOManager.Instance.GetSocketId();
            if (playerData.socketId == mySocketId)
                return; // Это мы сами

            SpawnNetworkPlayer(playerData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WorldMapNetwork] Ошибка обработки world_map_player_joined: {e.Message}");
        }
    }

    /// <summary>
    /// Игрок покинул WorldMap
    /// </summary>
    private void OnWorldMapPlayerLeft(string jsonData)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
            string socketId = data["socketId"];

            if (networkPlayers.ContainsKey(socketId))
            {
                Debug.Log($"[WorldMapNetwork] Игрок {socketId} покинул WorldMap");
                Destroy(networkPlayers[socketId]);
                networkPlayers.Remove(socketId);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WorldMapNetwork] Ошибка обработки world_map_player_left: {e.Message}");
        }
    }

    /// <summary>
    /// Игрок переместился на WorldMap
    /// </summary>
    private void OnWorldMapPlayerMoved(string jsonData)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<WorldMapPlayerMovedData>(jsonData);

            if (networkPlayers.ContainsKey(data.socketId))
            {
                GameObject player = networkPlayers[data.socketId];
                Vector3 targetPosition = new Vector3(data.position.x, data.position.y, data.position.z);

                // Плавное перемещение
                player.transform.position = Vector3.Lerp(player.transform.position, targetPosition, Time.deltaTime * 10f);

                // Ротация
                if (data.rotation != null)
                {
                    Quaternion targetRotation = Quaternion.Euler(data.rotation.x, data.rotation.y, data.rotation.z);
                    player.transform.rotation = Quaternion.Lerp(player.transform.rotation, targetRotation, Time.deltaTime * 10f);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WorldMapNetwork] Ошибка обработки world_map_player_moved: {e.Message}");
        }
    }

    /// <summary>
    /// Спавн сетевого игрока на WorldMap
    /// </summary>
    private void SpawnNetworkPlayer(WorldMapPlayerData playerData)
    {
        if (networkPlayers.ContainsKey(playerData.socketId))
        {
            Debug.LogWarning($"[WorldMapNetwork] Игрок {playerData.socketId} уже существует");
            return;
        }

        // Получаем prefab по классу
        GameObject prefab = GetPrefabForClass(playerData.characterClass);
        if (prefab == null)
        {
            Debug.LogWarning($"[WorldMapNetwork] Prefab не найден для класса {playerData.characterClass}");
            return;
        }

        // Спавним игрока
        Vector3 spawnPosition = new Vector3(playerData.position.x, playerData.position.y, playerData.position.z);
        GameObject networkPlayer = Instantiate(prefab, spawnPosition, Quaternion.identity);
        networkPlayer.name = $"NetworkPlayer_{playerData.username}";

        // Отключаем ненужные компоненты для WorldMap
        DisableUnnecessaryComponents(networkPlayer);

        // Добавляем nameplate (опционально)
        AddNameplate(networkPlayer, playerData.username, playerData.characterClass);

        networkPlayers[playerData.socketId] = networkPlayer;

        Debug.Log($"[WorldMapNetwork] Заспавнен игрок {playerData.username} ({playerData.characterClass}) на позиции {spawnPosition}");
    }

    /// <summary>
    /// Получить prefab по классу персонажа
    /// </summary>
    private GameObject GetPrefabForClass(string characterClass)
    {
        switch (characterClass)
        {
            case "Warrior": return warriorPrefab;
            case "Mage": return magePrefab;
            case "Archer": return archerPrefab;
            case "Rogue": return roguePrefab;
            case "Paladin": return paladinPrefab;
            default: return warriorPrefab; // Fallback
        }
    }

    /// <summary>
    /// Отключить ненужные компоненты для WorldMap (боевые скрипты и т.д.)
    /// </summary>
    private void DisableUnnecessaryComponents(GameObject player)
    {
        // Отключаем боевые компоненты
        var skillManager = player.GetComponent<SkillManager>();
        if (skillManager != null) skillManager.enabled = false;

        // CharacterController нужен только для локального игрока
        var characterController = player.GetComponent<CharacterController>();
        if (characterController != null) characterController.enabled = false;

        // Отключаем ввод для сетевых игроков
        var playerController = player.GetComponent<PlayerController>();
        if (playerController != null) playerController.enabled = false;

        // Оставляем только визуализацию и анимацию
        Debug.Log($"[WorldMapNetwork] Отключены боевые компоненты для {player.name}");
    }

    /// <summary>
    /// Добавить nameplate над игроком
    /// </summary>
    private void AddNameplate(GameObject player, string username, string characterClass)
    {
        // TODO: Добавить 3D текст над персонажем
        // Например через TextMeshPro WorldSpace
    }

    // ===== DATA CLASSES =====

    [System.Serializable]
    private class WorldMapPlayersListResponse
    {
        public WorldMapPlayerData[] players;
    }

    [System.Serializable]
    private class WorldMapPlayerData
    {
        public string socketId;
        public string username;
        public string characterClass;
        public Position position;
        public Rotation rotation;
    }

    [System.Serializable]
    private class WorldMapPlayerMovedData
    {
        public string socketId;
        public Position position;
        public Rotation rotation;
    }

    [System.Serializable]
    private class Position
    {
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    private class Rotation
    {
        public float x;
        public float y;
        public float z;
    }
}
