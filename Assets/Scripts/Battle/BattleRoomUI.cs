using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI для отображения игроков в комнате BattleScene
/// Показывает список подключенных игроков
/// </summary>
public class BattleRoomUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject playersPanel; // Панель со списком игроков
    [SerializeField] private TextMeshProUGUI roomInfoText; // Информация о комнате
    [SerializeField] private TextMeshProUGUI playersListText; // Список игроков
    [SerializeField] private TextMeshProUGUI statusText; // Статус (ожидание/готовы)

    [Header("Settings")]
    [SerializeField] private bool showPanelOnStart = true;
    [SerializeField] private float hideDelay = 5f; // Скрыть панель через 5 секунд

    private List<string> connectedPlayers = new List<string>();
    private string roomId = "";
    private bool isRoomHost = false;
    private float showTimer = 0f;

    /// <summary>
    /// Singleton instance
    /// </summary>
    public static BattleRoomUI Instance { get; private set; }

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
        // Получаем информацию о комнате
        roomId = PlayerPrefs.GetString("CurrentRoomId", "");
        isRoomHost = PlayerPrefs.GetInt("IsRoomHost", 0) == 1;
        string username = PlayerPrefs.GetString("Username", "Player");

        // Добавляем себя в список
        connectedPlayers.Add(username);

        // Обновляем UI
        UpdateUI();

        // Показываем панель если нужно
        if (playersPanel != null)
        {
            playersPanel.SetActive(showPanelOnStart);
            if (showPanelOnStart)
            {
                showTimer = hideDelay;
            }
        }

        // Подписываемся на события мультиплеера
        if (!string.IsNullOrEmpty(roomId))
        {
            SubscribeToMultiplayerEvents();
        }
    }

    void Update()
    {
        // Автоматическое скрытие панели
        if (showTimer > 0f)
        {
            showTimer -= Time.deltaTime;
            if (showTimer <= 0f && playersPanel != null)
            {
                playersPanel.SetActive(false);
            }
        }

        // Показать/скрыть панель по нажатию Tab
        if (Input.GetKeyDown(KeyCode.Tab) && playersPanel != null)
        {
            bool newState = !playersPanel.activeSelf;
            playersPanel.SetActive(newState);

            if (newState)
            {
                UpdateUI(); // Обновляем при показе
            }
        }
    }

    /// <summary>
    /// Подписаться на события мультиплеера
    /// </summary>
    private void SubscribeToMultiplayerEvents()
    {
        if (SocketIOManager.Instance != null)
        {
            // Слушаем подключение новых игроков
            SocketIOManager.Instance.On("player_joined", OnPlayerJoined);
            SocketIOManager.Instance.On("player_left", OnPlayerLeft);
            SocketIOManager.Instance.On("room_players_list", OnRoomPlayersList);

            Debug.Log("[BattleRoomUI] ✅ Подписались на события мультиплеера");

            // Запрашиваем текущий список игроков
            string requestData = $"{{\"roomId\":\"{roomId}\"}}";
            SocketIOManager.Instance.Emit("get_room_players", requestData);
        }
    }

    /// <summary>
    /// Callback: Новый игрок присоединился
    /// </summary>
    private void OnPlayerJoined(string data)
    {
        try
        {
            var json = JsonUtility.FromJson<PlayerEventData>(data);
            string playerName = json.username ?? json.playerId ?? "Unknown";

            if (!connectedPlayers.Contains(playerName))
            {
                connectedPlayers.Add(playerName);
                Debug.Log($"[BattleRoomUI] ✅ Игрок присоединился: {playerName}");
                UpdateUI();

                // Показываем панель на 3 секунды
                if (playersPanel != null)
                {
                    playersPanel.SetActive(true);
                    showTimer = 3f;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BattleRoomUI] Ошибка парсинга player_joined: {e.Message}");
        }
    }

    /// <summary>
    /// Callback: Игрок вышел
    /// </summary>
    private void OnPlayerLeft(string data)
    {
        try
        {
            var json = JsonUtility.FromJson<PlayerEventData>(data);
            string playerName = json.username ?? json.playerId ?? "Unknown";

            if (connectedPlayers.Contains(playerName))
            {
                connectedPlayers.Remove(playerName);
                Debug.Log($"[BattleRoomUI] ❌ Игрок вышел: {playerName}");
                UpdateUI();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BattleRoomUI] Ошибка парсинга player_left: {e.Message}");
        }
    }

    /// <summary>
    /// Callback: Получен список игроков в комнате
    /// </summary>
    private void OnRoomPlayersList(string data)
    {
        try
        {
            var json = JsonUtility.FromJson<RoomPlayersData>(data);

            if (json.players != null && json.players.Length > 0)
            {
                connectedPlayers.Clear();
                foreach (var playerName in json.players)
                {
                    if (!string.IsNullOrEmpty(playerName))
                    {
                        connectedPlayers.Add(playerName);
                    }
                }

                Debug.Log($"[BattleRoomUI] ✅ Получен список игроков: {connectedPlayers.Count}");
                UpdateUI();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BattleRoomUI] Ошибка парсинга room_players_list: {e.Message}");
        }
    }

    /// <summary>
    /// Обновить UI
    /// </summary>
    private void UpdateUI()
    {
        // Информация о комнате
        if (roomInfoText != null)
        {
            string hostStatus = isRoomHost ? "👑 ВЫ ХОСТ" : "🎮 КЛИЕНТ";
            roomInfoText.text = $"Комната: {roomId}\n{hostStatus}";
        }

        // Список игроков
        if (playersListText != null)
        {
            string playersList = $"Игроки в комнате ({connectedPlayers.Count}):\n";
            for (int i = 0; i < connectedPlayers.Count; i++)
            {
                string prefix = i == 0 && isRoomHost ? "👑 " : "🎮 ";
                playersList += $"{prefix}{connectedPlayers[i]}\n";
            }
            playersListText.text = playersList;
        }

        // Статус
        if (statusText != null)
        {
            if (connectedPlayers.Count >= 2)
            {
                statusText.text = "✅ ГОТОВЫ К БОЮ!";
                statusText.color = Color.green;
            }
            else
            {
                statusText.text = "⏳ Ожидание игроков...";
                statusText.color = Color.yellow;
            }
        }
    }

    /// <summary>
    /// Показать панель игроков
    /// </summary>
    public void ShowPlayersPanel()
    {
        if (playersPanel != null)
        {
            playersPanel.SetActive(true);
            UpdateUI();
        }
    }

    /// <summary>
    /// Скрыть панель игроков
    /// </summary>
    public void HidePlayersPanel()
    {
        if (playersPanel != null)
        {
            playersPanel.SetActive(false);
        }
    }

    // Вспомогательные классы для JSON
    [System.Serializable]
    private class PlayerEventData
    {
        public string playerId;
        public string username;
    }

    [System.Serializable]
    private class RoomPlayersData
    {
        public string[] players;
    }
}
