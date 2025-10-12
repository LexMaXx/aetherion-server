using UnityEngine;
using System;
using System.Collections.Generic;
using SocketIOClient;
using Newtonsoft.Json;

/// <summary>
/// Socket.IO Manager using SocketIOUnity library
/// Заменяет UnifiedSocketIO для работы с WebSocket transport
/// </summary>
public class SocketIOManager : MonoBehaviour
{
    public static SocketIOManager Instance { get; private set; }

    [Header("Connection")]
    [SerializeField] private string serverUrl = "https://aetherion-server-gv5u.onrender.com";
    [SerializeField] private bool debugMode = true;

    // Socket.IO client
    private SocketIOUnity socket;
    private bool isConnected = false;
    private string currentRoomId = "";
    private string myUsername = "";
    private string myUserId = "";

    // Event callbacks
    private Dictionary<string, Action<string>> eventCallbacks = new Dictionary<string, Action<string>>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure UnityMainThreadDispatcher exists
        if (!UnityMainThreadDispatcher.Exists())
        {
            UnityMainThreadDispatcher.Instance();
            DebugLog("✅ UnityMainThreadDispatcher created");
        }

        DebugLog("✅ SocketIOManager initialized");
    }

    void OnDestroy()
    {
        if (socket != null)
        {
            socket.Disconnect();
            socket.Dispose();
        }
    }

    /// <summary>
    /// Подключиться к серверу
    /// </summary>
    public void Connect(string token, Action<bool> onComplete = null)
    {
        if (isConnected)
        {
            DebugLog("⚠️ Уже подключен!");
            onComplete?.Invoke(true);
            return;
        }

        DebugLog($"🔌 Подключение к {serverUrl}...");

        // Создаём Socket.IO клиент
        var uri = new Uri(serverUrl);
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
            EIO = 4,
            Query = new Dictionary<string, string>
            {
                { "token", token }
            }
        });

        // Событие подключения
        socket.OnConnected += (sender, e) =>
        {
            isConnected = true;
            DebugLog($"✅ Подключено! Socket ID: {socket.Id}");
            onComplete?.Invoke(true);
        };

        // Событие отключения
        socket.OnDisconnected += (sender, e) =>
        {
            isConnected = false;
            DebugLog("❌ Отключено от сервера");
        };

        // Событие ошибки
        socket.OnError += (sender, e) =>
        {
            Debug.LogError($"[SocketIO] ❌ Ошибка: {e}");
        };

        // Подключаемся
        socket.Connect();
    }

    /// <summary>
    /// Отключиться от сервера
    /// </summary>
    public void Disconnect()
    {
        if (socket != null && isConnected)
        {
            socket.Disconnect();
            isConnected = false;
            DebugLog("🔌 Отключились от сервера");
        }
    }

    /// <summary>
    /// Подписаться на событие
    /// </summary>
    public void On(string eventName, Action<string> callback)
    {
        if (socket == null)
        {
            Debug.LogError("[SocketIO] Socket не инициализирован!");
            return;
        }

        // Сохраняем callback
        eventCallbacks[eventName] = callback;

        // Подписываемся на событие Socket.IO
        socket.On(eventName, (response) =>
        {
            string jsonData = response.GetValue<string>();
            DebugLog($"📨 Событие '{eventName}': {jsonData.Substring(0, Math.Min(100, jsonData.Length))}...");

            // Вызываем callback в главном потоке Unity
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (eventCallbacks.ContainsKey(eventName))
                {
                    eventCallbacks[eventName]?.Invoke(jsonData);
                }
            });
        });

        DebugLog($"📡 Подписка на событие: {eventName}");
    }

    /// <summary>
    /// Отправить событие
    /// </summary>
    public void Emit(string eventName, string jsonData)
    {
        if (socket == null || !isConnected)
        {
            Debug.LogWarning($"[SocketIO] ⚠️ Не подключен! Событие '{eventName}' не отправлено");
            return;
        }

        socket.Emit(eventName, jsonData);
        DebugLog($"📤 Отправлено: {eventName}");
    }

    /// <summary>
    /// Войти в комнату
    /// </summary>
    public void JoinRoom(string roomId, string characterClass, Action<bool> onComplete = null)
    {
        if (!isConnected)
        {
            Debug.LogError("[SocketIO] ❌ Не подключен к серверу!");
            onComplete?.Invoke(false);
            return;
        }

        currentRoomId = roomId;
        myUsername = PlayerPrefs.GetString("Username", "Player");
        myUserId = PlayerPrefs.GetString("UserId", "");

        // Подписываемся на room_players перед входом
        bool responseReceived = false;
        On("room_players", (data) =>
        {
            if (!responseReceived)
            {
                responseReceived = true;
                DebugLog("✅ Получен список игроков в комнате");
                onComplete?.Invoke(true);
            }
        });

        // Отправляем join_room
        var joinData = new JoinRoomRequest
        {
            roomId = roomId,
            username = myUsername,
            characterClass = characterClass,
            userId = myUserId,
            password = "",
            level = PlayerPrefs.GetInt("Level", 1)
        };

        string json = JsonUtility.ToJson(joinData);
        Emit("join_room", json);

        DebugLog($"🚪 Вход в комнату: {roomId} как {characterClass}");

        // Таймаут на случай, если сервер не ответит
        StartCoroutine(JoinRoomTimeout(3f, () =>
        {
            if (!responseReceived)
            {
                responseReceived = true;
                Debug.LogWarning("[SocketIO] ⏰ Таймаут ожидания room_players");
                onComplete?.Invoke(true);
            }
        }));
    }

    private System.Collections.IEnumerator JoinRoomTimeout(float delay, Action callback)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }

    /// <summary>
    /// Запросить список игроков в комнате
    /// </summary>
    public void RequestRoomPlayers()
    {
        if (!isConnected || string.IsNullOrEmpty(currentRoomId))
        {
            Debug.LogWarning("[SocketIO] ⚠️ Не подключен к комнате!");
            return;
        }

        var data = new { roomId = currentRoomId };
        Emit("get_room_players", JsonUtility.ToJson(data));
        DebugLog($"🔄 Запрос списка игроков для комнаты {currentRoomId}");
    }

    /// <summary>
    /// Обновить позицию игрока
    /// </summary>
    public void UpdatePosition(Vector3 position, Quaternion rotation, Vector3 velocity, bool isGrounded)
    {
        if (!isConnected) return;

        var data = new
        {
            position = new { x = position.x, y = position.y, z = position.z },
            rotation = new { x = rotation.x, y = rotation.y, z = rotation.z, w = rotation.w },
            velocity = new { x = velocity.x, y = velocity.y, z = velocity.z },
            isGrounded = isGrounded
        };

        string json = JsonConvert.SerializeObject(data);
        Emit("player_update", json);
    }

    public bool IsConnected => isConnected;
    public string CurrentRoomId => currentRoomId;
    public string MyUsername => myUsername;

    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[SocketIO] {message}");
        }
    }

    // Data classes
    [Serializable]
    public class JoinRoomRequest
    {
        public string roomId;
        public string username;
        public string characterClass;
        public string userId;
        public string password;
        public int level;
    }
}
