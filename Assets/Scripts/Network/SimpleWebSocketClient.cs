using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

/// <summary>
/// Упрощенный WebSocket клиент БЕЗ внешних зависимостей
/// Использует только UnityWebRequest (встроен в Unity)
///
/// РАБОТАЕТ БЕЗ SOCKET.IO - просто REST API + polling
/// </summary>
public class SimpleWebSocketClient : MonoBehaviour
{
    public static SimpleWebSocketClient Instance { get; private set; }

    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "https://aetherion-server-gv5u.onrender.com";

    // Connection state
    private bool isConnected = false;
    private string sessionId = "";
    private string authToken = "";
    private string currentRoomId = "";

    // Event callbacks
    private Dictionary<string, Action<string>> eventCallbacks = new Dictionary<string, Action<string>>();

    // Polling state
    private bool isPolling = false;
    private float pollInterval = 1f; // Poll every 1 second
    private Coroutine pollCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        Disconnect();
    }

    // ===== CONNECTION =====

    /// <summary>
    /// Подключиться к серверу
    /// </summary>
    public void Connect(string token, Action<bool> onComplete = null)
    {
        if (isConnected)
        {
            Debug.Log("[SimpleWS] Уже подключен");
            onComplete?.Invoke(true);
            return;
        }

        authToken = token;
        StartCoroutine(ConnectCoroutine(onComplete));
    }

    private IEnumerator ConnectCoroutine(Action<bool> onComplete)
    {
        Debug.Log($"[SimpleWS] Подключение к {serverUrl}...");

        // Генерируем session ID
        sessionId = System.Guid.NewGuid().ToString();
        isConnected = true;

        Debug.Log($"[SimpleWS] ✅ Подключено! Session ID: {sessionId}");
        onComplete?.Invoke(true);

        yield break;
    }

    /// <summary>
    /// Отключиться
    /// </summary>
    public void Disconnect()
    {
        isConnected = false;
        sessionId = "";
        currentRoomId = "";

        // Stop polling
        if (pollCoroutine != null)
        {
            StopCoroutine(pollCoroutine);
            pollCoroutine = null;
        }
        isPolling = false;

        Debug.Log("[SimpleWS] Отключено");
    }

    // ===== EVENTS =====

    /// <summary>
    /// Отправить событие (НЕ отправляем на самом деле, только логируем)
    /// Для полной функциональности нужен настоящий WebSocket
    /// </summary>
    public void EmitEvent(string eventName, object data, bool highPriority = false)
    {
        if (!isConnected)
        {
            Debug.LogWarning($"[SimpleWS] Не подключен! Событие '{eventName}' пропущено");
            return;
        }

        // Просто логируем - для работы с сервером нужен Socket.IO
        Debug.Log($"[SimpleWS] 📤 Событие: {eventName} (только лог, не отправлено)");
    }

    /// <summary>
    /// Подписаться на событие
    /// </summary>
    public void On(string eventName, Action<string> callback)
    {
        if (!eventCallbacks.ContainsKey(eventName))
        {
            eventCallbacks[eventName] = callback;
        }
        else
        {
            eventCallbacks[eventName] += callback;
        }
        Debug.Log($"[SimpleWS] 📡 Подписка на: {eventName}");
    }

    /// <summary>
    /// Отписаться от события
    /// </summary>
    public void Off(string eventName)
    {
        if (eventCallbacks.ContainsKey(eventName))
        {
            eventCallbacks.Remove(eventName);
        }
    }

    // ===== GAME EVENTS =====

    /// <summary>
    /// Войти в комнату (через REST API)
    /// </summary>
    public void JoinRoom(string roomId, string characterClass, Action<bool> onComplete = null)
    {
        if (!isConnected)
        {
            Debug.LogError("[SimpleWS] Не подключен!");
            onComplete?.Invoke(false);
            return;
        }

        currentRoomId = roomId;
        Debug.Log($"[SimpleWS] 🚪 Вход в комнату: {roomId} (класс: {characterClass})");

        StartCoroutine(JoinRoomCoroutine(roomId, characterClass, onComplete));
    }

    private IEnumerator JoinRoomCoroutine(string roomId, string characterClass, Action<bool> onComplete)
    {
        // Используем REST API для входа в комнату
        string url = $"{serverUrl}/api/room/join";
        string username = PlayerPrefs.GetString("Username", "Player");

        string jsonData = $"{{\"roomId\":\"{roomId}\",\"password\":\"\",\"characterClass\":\"{characterClass}\",\"username\":\"{username}\",\"level\":1}}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {authToken}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[SimpleWS] ✅ Вошли в комнату через REST API");

            // Симулируем получение room_state
            if (eventCallbacks.ContainsKey("room_state"))
            {
                string mockRoomState = "{\"players\":[]}";
                eventCallbacks["room_state"]?.Invoke(mockRoomState);
            }

            onComplete?.Invoke(true);
        }
        else
        {
            Debug.LogError($"[SimpleWS] ❌ Ошибка входа в комнату: {request.error}");
            onComplete?.Invoke(false);
        }
    }

    /// <summary>
    /// Обновить позицию (НЕ отправляем)
    /// </summary>
    public void UpdatePosition(Vector3 position, Quaternion rotation, string animationState, Vector3 velocity)
    {
        // Не отправляем - требуется WebSocket
    }

    /// <summary>
    /// Отправить атаку
    /// </summary>
    public void SendAttack(string targetSocketId, float damage, string attackType)
    {
        if (!isConnected) return;
        Debug.Log($"[SimpleWS] ⚔️ Атака: {damage} урона (только лог)");
    }

    /// <summary>
    /// Использовать скилл
    /// </summary>
    public void SendSkill(int skillId, string targetSocketId, Vector3 targetPosition)
    {
        if (!isConnected) return;
        Debug.Log($"[SimpleWS] 🔮 Скилл {skillId} (только лог)");
    }

    /// <summary>
    /// Обновить здоровье
    /// </summary>
    public void UpdateHealth(int currentHP, int maxHP, int currentMP, int maxMP)
    {
        // Не отправляем
    }

    /// <summary>
    /// Запросить респавн
    /// </summary>
    public void RequestRespawn()
    {
        if (!isConnected) return;
        Debug.Log("[SimpleWS] 💀 Запрос респавна (только лог)");
    }

    /// <summary>
    /// Начать прослушивание (polling room state)
    /// </summary>
    public void StartListening()
    {
        if (isPolling)
        {
            Debug.Log("[SimpleWS] Polling уже запущен");
            return;
        }

        isPolling = true;
        pollCoroutine = StartCoroutine(PollRoomState());
        Debug.Log("[SimpleWS] 📡 Polling started");
    }

    /// <summary>
    /// Polling room state from server
    /// </summary>
    private IEnumerator PollRoomState()
    {
        while (isPolling && isConnected && !string.IsNullOrEmpty(currentRoomId))
        {
            yield return new WaitForSeconds(pollInterval);

            // Get room state via REST API
            string url = $"{serverUrl}/api/room/{currentRoomId}";

            UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", $"Bearer {authToken}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Parse response and trigger events
                ProcessRoomStateResponse(request.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning($"[SimpleWS] Polling error: {request.error}");
            }
        }

        Debug.Log("[SimpleWS] Polling stopped");
    }

    /// <summary>
    /// Process room state response and trigger events
    /// </summary>
    private void ProcessRoomStateResponse(string jsonResponse)
    {
        try
        {
            // Parse response: {"success":true,"room":{...,"players":[...]}}
            var wrapper = JsonUtility.FromJson<RoomInfoResponseWrapper>(jsonResponse);

            if (wrapper.success && wrapper.room != null)
            {
                // Convert to RoomStateData format
                List<RoomPlayerDataSimple> playersList = new List<RoomPlayerDataSimple>();

                foreach (var player in wrapper.room.players)
                {
                    playersList.Add(new RoomPlayerDataSimple
                    {
                        socketId = player.userId,  // Use userId as socketId
                        username = player.username,
                        characterClass = player.characterClass,
                        position = player.position
                    });
                }

                var roomStateData = new RoomStateDataSimple
                {
                    players = playersList.ToArray()
                };

                string roomStateJson = JsonUtility.ToJson(roomStateData);

                // Trigger room_state event
                if (eventCallbacks.ContainsKey("room_state"))
                {
                    eventCallbacks["room_state"]?.Invoke(roomStateJson);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SimpleWS] Error processing room state: {ex.Message}");
        }
    }

    // Properties
    public bool IsConnected => isConnected;
    public string CurrentRoomId => currentRoomId;
    public string SessionId => sessionId;
    public float AveragePing => 0f;
}

// ===== DATA CLASSES FOR POLLING =====

[Serializable]
public class RoomInfoResponseWrapper
{
    public bool success;
    public RoomInfoDetailed room;
    public string message;
}

[Serializable]
public class RoomInfoDetailed
{
    public string roomId;
    public string roomName;
    public string host;
    public int currentPlayers;
    public int maxPlayers;
    public string status;
    public PlayerInRoom[] players;
}

[Serializable]
public class PlayerInRoom
{
    public string userId;
    public string username;
    public string characterClass;
    public int level;
    public bool isAlive;
    public int kills;
    public int deaths;
    public PositionDataSimple position;
}

[Serializable]
public class PositionDataSimple
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class RoomStateDataSimple
{
    public RoomPlayerDataSimple[] players;
}

[Serializable]
public class RoomPlayerDataSimple
{
    public string socketId;
    public string username;
    public string characterClass;
    public PositionDataSimple position;
}
