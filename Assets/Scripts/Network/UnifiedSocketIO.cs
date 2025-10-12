using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

/// <summary>
/// Унифицированный Socket.IO клиент для Aetherion
/// Объединяет функционал всех предыдущих клиентов в один оптимизированный
/// Поддерживает real-time мультиплеер с автоматической синхронизацией
/// </summary>
public class UnifiedSocketIO : MonoBehaviour
{
    public static UnifiedSocketIO Instance { get; private set; }

    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "https://aetherion-server-gv5u.onrender.com";
    [SerializeField] private float heartbeatInterval = 20f;
    [SerializeField] private float pollInterval = 0.05f; // 20 Hz
    [SerializeField] private float reconnectDelay = 5f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    // Connection state
    private bool isConnected = false;
    private bool isConnecting = false;
    private bool shouldReconnect = true;
    private string sessionId = "";
    private string authToken = "";
    private string currentRoomId = "";
    private string myUsername = "";
    private string myUserId = "";

    // Heartbeat
    private float lastHeartbeat = 0f;

    // Event callbacks
    private Dictionary<string, Action<string>> eventCallbacks = new Dictionary<string, Action<string>>();

    // Socket.IO URLs
    private string HandshakeUrl => $"{serverUrl}/socket.io/?EIO=4&transport=polling";
    private string PollUrl => $"{serverUrl}/socket.io/?EIO=4&transport=polling&sid={sessionId}";

    // Coroutines
    private Coroutine listeningCoroutine;
    private Coroutine reconnectCoroutine;

    // Statistics
    private int messagesSent = 0;
    private int messagesReceived = 0;
    private float lastPingTime = 0f;
    private float currentPing = 0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            DebugLog("✅ UnifiedSocketIO initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Heartbeat
        if (isConnected && Time.time - lastHeartbeat > heartbeatInterval)
        {
            SendHeartbeat();
            lastHeartbeat = Time.time;
        }
    }

    void OnDestroy()
    {
        shouldReconnect = false;
        Disconnect();
    }

    /// <summary>
    /// Подключиться к серверу
    /// </summary>
    public void Connect(string token = "", Action<bool> onComplete = null)
    {
        if (isConnected || isConnecting)
        {
            DebugLog("⚠️ Уже подключен или подключается");
            onComplete?.Invoke(isConnected);
            return;
        }

        authToken = string.IsNullOrEmpty(token) ? PlayerPrefs.GetString("JWT", "") : token;
        myUsername = PlayerPrefs.GetString("Username", "Player");
        myUserId = PlayerPrefs.GetString("UserId", "");

        StartCoroutine(ConnectCoroutine(onComplete));
    }

    /// <summary>
    /// Корутина подключения
    /// </summary>
    private IEnumerator ConnectCoroutine(Action<bool> onComplete)
    {
        isConnecting = true;
        DebugLog($"🔌 Подключение к {serverUrl}...");

        // Socket.io handshake
        UnityWebRequest request = UnityWebRequest.Get(HandshakeUrl);
        if (!string.IsNullOrEmpty(authToken))
        {
            request.SetRequestHeader("Authorization", $"Bearer {authToken}");
        }

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler.text;

            // Socket.io response format: 0{"sid":"xxxxx","upgrades":["websocket"],"pingInterval":25000,"pingTimeout":60000}
            int sidStart = response.IndexOf("\"sid\":\"") + 7;
            int sidEnd = response.IndexOf("\"", sidStart);

            if (sidStart > 6 && sidEnd > sidStart)
            {
                sessionId = response.Substring(sidStart, sidEnd - sidStart);
                isConnected = true;
                isConnecting = false;
                lastHeartbeat = Time.time;

                DebugLog($"✅ Подключено! Session ID: {sessionId}");

                // ТЕСТ: Отправим ping для проверки связи
                StartCoroutine(TestPing());

                onComplete?.Invoke(true);

                // Start listening for events
                listeningCoroutine = StartCoroutine(ListenCoroutine());
            }
            else
            {
                Debug.LogError("[SocketIO] ❌ Не удалось получить session ID");
                isConnecting = false;
                onComplete?.Invoke(false);

                // Try reconnect
                if (shouldReconnect)
                {
                    TryReconnect();
                }
            }
        }
        else
        {
            Debug.LogError($"[SocketIO] ❌ Ошибка подключения: {request.error}");
            isConnecting = false;
            onComplete?.Invoke(false);

            // Try reconnect
            if (shouldReconnect)
            {
                TryReconnect();
            }
        }
    }

    /// <summary>
    /// Попытка переподключения
    /// </summary>
    private void TryReconnect()
    {
        if (reconnectCoroutine != null)
        {
            StopCoroutine(reconnectCoroutine);
        }
        reconnectCoroutine = StartCoroutine(ReconnectCoroutine());
    }

    private IEnumerator ReconnectCoroutine()
    {
        DebugLog($"🔄 Переподключение через {reconnectDelay} сек...");
        yield return new WaitForSeconds(reconnectDelay);

        if (!isConnected && shouldReconnect)
        {
            Connect();
        }
    }

    /// <summary>
    /// Отключиться от сервера
    /// </summary>
    public void Disconnect()
    {
        if (!isConnected) return;

        DebugLog("🔌 Отключение...");

        // Stop listening
        if (listeningCoroutine != null)
        {
            StopCoroutine(listeningCoroutine);
            listeningCoroutine = null;
        }

        // Stop reconnecting
        if (reconnectCoroutine != null)
        {
            StopCoroutine(reconnectCoroutine);
            reconnectCoroutine = null;
        }

        isConnected = false;
        sessionId = "";
        currentRoomId = "";
    }

    /// <summary>
    /// Отправить событие на сервер (Socket.IO format)
    /// </summary>
    public void Emit(string eventName, string jsonData)
    {
        if (!isConnected)
        {
            Debug.LogWarning($"[SocketIO] ⚠️ Не подключен! Событие '{eventName}' не отправлено");
            return;
        }

        StartCoroutine(EmitCoroutine(eventName, jsonData));
    }

    /// <summary>
    /// Корутина отправки события
    /// </summary>
    private IEnumerator EmitCoroutine(string eventName, string jsonData)
    {
        // Socket.io message format: 42["event_name",{data}]
        string message = $"42[\"{eventName}\",{jsonData}]";

        UnityWebRequest request = new UnityWebRequest(PollUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(message);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "text/plain");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            messagesSent++;
            DebugLog($"📤 Отправлено: {eventName}");
        }
        else
        {
            // Ignore 400 errors - Socket.IO sometimes returns them even on success
            if (request.responseCode != 400)
            {
                Debug.LogWarning($"[SocketIO] ⚠️ Ошибка отправки '{eventName}': {request.error}");
            }
            else
            {
                messagesSent++;
            }
        }
    }

    /// <summary>
    /// Подписаться на событие от сервера
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
        DebugLog($"📡 Подписка на событие: {eventName}");
    }

    /// <summary>
    /// Отписаться от события
    /// </summary>
    public void Off(string eventName)
    {
        if (eventCallbacks.ContainsKey(eventName))
        {
            eventCallbacks.Remove(eventName);
            DebugLog($"📡 Отписка от события: {eventName}");
        }
    }

    /// <summary>
    /// Отправить heartbeat (ping)
    /// </summary>
    private void SendHeartbeat()
    {
        if (!isConnected) return;
        lastPingTime = Time.time;
        StartCoroutine(HeartbeatCoroutine());
    }

    private IEnumerator HeartbeatCoroutine()
    {
        UnityWebRequest request = new UnityWebRequest(PollUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes("2"); // Socket.io ping message
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            currentPing = (Time.time - lastPingTime) * 1000f; // Convert to ms
            DebugLog($"💓 Heartbeat OK (Ping: {currentPing:F0}ms)");
        }
    }

    /// <summary>
    /// ТЕСТ: Отправить ping сразу после подключения
    /// </summary>
    private IEnumerator TestPing()
    {
        yield return new WaitForSeconds(0.5f);
        Debug.Log("[SocketIO] 🧪 TEST: Отправляем ping событие...");
        Emit("ping", "{}");
    }

    /// <summary>
    /// Корутина прослушивания сообщений от сервера
    /// </summary>
    private IEnumerator ListenCoroutine()
    {
        DebugLog("👂 Начинаем прослушивание событий...");

        while (isConnected)
        {
            UnityWebRequest request = UnityWebRequest.Get(PollUrl);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;

                if (!string.IsNullOrEmpty(response) && response.Length > 2)
                {
                    ParseSocketIOMessages(response);
                }
            }
            else if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError("[SocketIO] ❌ Потеря соединения");
                isConnected = false;

                if (shouldReconnect)
                {
                    TryReconnect();
                }
                yield break;
            }

            yield return new WaitForSeconds(pollInterval);
        }

        DebugLog("👂 Прослушивание остановлено");
    }

    /// <summary>
    /// Парсинг Socket.IO сообщений
    /// </summary>
    private void ParseSocketIOMessages(string response)
    {
        // ДЛЯ ОТЛАДКИ: Логируем ВСЕ сообщения от сервера
        if (response.Length > 2)
        {
            Debug.Log($"[SocketIO] 🔍 RAW MESSAGE: {response}");
        }

        if (response.StartsWith("42["))
        {
            try
            {
                int eventStart = response.IndexOf("[\"") + 2;
                int eventEnd = response.IndexOf("\",", eventStart);

                if (eventEnd > eventStart)
                {
                    string eventName = response.Substring(eventStart, eventEnd - eventStart);
                    int dataStart = eventEnd + 2;
                    int dataEnd = response.LastIndexOf(']');

                    if (dataEnd > dataStart)
                    {
                        string jsonData = response.Substring(dataStart, dataEnd - dataStart);
                        messagesReceived++;
                        DebugLog($"📨 Получено событие: {eventName}");
                        Debug.Log($"[SocketIO] 📦 Данные: {jsonData.Substring(0, Math.Min(200, jsonData.Length))}...");

                        if (eventCallbacks.ContainsKey(eventName))
                        {
                            Debug.Log($"[SocketIO] ✅ Вызываем callback для '{eventName}'");
                            eventCallbacks[eventName]?.Invoke(jsonData);
                        }
                        else
                        {
                            Debug.LogWarning($"[SocketIO] ⚠️ Нет подписчиков на событие '{eventName}'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SocketIO] ⚠️ Ошибка парсинга сообщения: {ex.Message}");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // GAME-SPECIFIC METHODS
    // ═══════════════════════════════════════════════════════════════

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
        bool responseReceived = false;

        // Subscribe to room_players event BEFORE joining
        On("room_players", (data) =>
        {
            if (!responseReceived)
            {
                responseReceived = true;
                DebugLog($"✅ Получен список игроков в комнате");
                onComplete?.Invoke(true);
            }
        });

        // Send join_room event
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

        // Fallback timeout - если через 3 сек нет ответа, считаем успешным
        StartCoroutine(JoinRoomTimeout(3f, () =>
        {
            if (!responseReceived)
            {
                responseReceived = true;
                Debug.LogWarning("[SocketIO] ⏰ Таймаут ожидания room_players, считаем вход успешным");
                onComplete?.Invoke(true);
            }
        }));
    }

    /// <summary>
    /// Запросить список игроков в текущей комнате (для повторной синхронизации)
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

    private IEnumerator JoinRoomTimeout(float delay, Action callback)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }

    /// <summary>
    /// Обновить позицию и движение игрока (оптимизированный метод)
    /// </summary>
    public void UpdatePosition(Vector3 position, Quaternion rotation, Vector3 velocity, bool isGrounded)
    {
        if (!isConnected || string.IsNullOrEmpty(currentRoomId)) return;

        var data = new PlayerUpdateRequest
        {
            position = new SerializableVector3(position),
            rotation = new SerializableVector3(rotation.eulerAngles),
            velocity = new SerializableVector3(velocity),
            isGrounded = isGrounded
        };

        Emit("player_update", JsonUtility.ToJson(data));
    }

    /// <summary>
    /// Отправить анимацию игрока
    /// </summary>
    public void SendAnimation(string animation, float speed = 1.0f)
    {
        if (!isConnected || string.IsNullOrEmpty(currentRoomId)) return;

        var data = new AnimationRequest
        {
            animation = animation,
            speed = speed
        };

        Emit("player_animation", JsonUtility.ToJson(data));
    }

    /// <summary>
    /// Отправить атаку
    /// </summary>
    public void SendAttack(string targetType, string targetId, float damage, string attackType = "melee", int skillId = 0)
    {
        if (!isConnected || string.IsNullOrEmpty(currentRoomId)) return;

        var data = new AttackRequest
        {
            attackType = attackType,
            targetType = targetType,
            targetId = targetId,
            damage = damage,
            skillId = skillId
        };

        Emit("player_attack", JsonUtility.ToJson(data));
        DebugLog($"⚔️ Атака: {targetType} {targetId}, урон {damage}");
    }

    /// <summary>
    /// Отправить получение урона
    /// </summary>
    public void SendDamage(float damage, float currentHealth, string attackerId)
    {
        if (!isConnected || string.IsNullOrEmpty(currentRoomId)) return;

        var data = new DamageRequest
        {
            damage = damage,
            currentHealth = currentHealth,
            attackerId = attackerId
        };

        Emit("player_damaged", JsonUtility.ToJson(data));
        DebugLog($"💔 Урон: {damage}, HP: {currentHealth}");
    }

    /// <summary>
    /// Отправить респавн
    /// </summary>
    public void SendRespawn(Vector3 position)
    {
        if (!isConnected || string.IsNullOrEmpty(currentRoomId)) return;

        var data = new RespawnRequest
        {
            position = new SerializableVector3(position)
        };

        Emit("player_respawn", JsonUtility.ToJson(data));
        DebugLog("🔄 Респавн");
    }

    /// <summary>
    /// Отправить использование скилла
    /// </summary>
    public void SendSkill(int skillId, string targetSocketId, Vector3 targetPosition)
    {
        if (!isConnected || string.IsNullOrEmpty(currentRoomId)) return;

        var data = new
        {
            skillId = skillId,
            targetSocketId = targetSocketId,
            targetX = targetPosition.x,
            targetY = targetPosition.y,
            targetZ = targetPosition.z
        };

        Emit("player_skill", JsonUtility.ToJson(data));
        DebugLog($"🔮 Скилл {skillId}");
    }

    /// <summary>
    /// Обновить здоровье/ману
    /// </summary>
    public void UpdateHealth(int currentHP, int maxHP, int currentMP, int maxMP)
    {
        if (!isConnected || string.IsNullOrEmpty(currentRoomId)) return;

        var data = new
        {
            currentHP = currentHP,
            maxHP = maxHP,
            currentMP = currentMP,
            maxMP = maxMP
        };

        Emit("update_health", JsonUtility.ToJson(data));
    }

    /// <summary>
    /// Враг получил урон
    /// </summary>
    public void SendEnemyDamaged(string enemyId, float damage, float currentHealth)
    {
        if (!isConnected || string.IsNullOrEmpty(currentRoomId)) return;

        var data = new EnemyDamagedRequest
        {
            roomId = currentRoomId,
            enemyId = enemyId,
            damage = damage,
            currentHealth = currentHealth
        };

        Emit("enemy_damaged", JsonUtility.ToJson(data));
    }

    /// <summary>
    /// Враг убит
    /// </summary>
    public void SendEnemyKilled(string enemyId, Vector3 position)
    {
        if (!isConnected || string.IsNullOrEmpty(currentRoomId)) return;

        var data = new EnemyKilledRequest
        {
            roomId = currentRoomId,
            enemyId = enemyId,
            position = new SerializableVector3(position)
        };

        Emit("enemy_killed", JsonUtility.ToJson(data));
    }

    /// <summary>
    /// Вывести статистику
    /// </summary>
    public void LogStats()
    {
        Debug.Log($"[SocketIO] 📊 Статистика:\n" +
                  $"  Подключено: {isConnected}\n" +
                  $"  Session ID: {sessionId}\n" +
                  $"  Комната: {currentRoomId}\n" +
                  $"  Отправлено сообщений: {messagesSent}\n" +
                  $"  Получено сообщений: {messagesReceived}\n" +
                  $"  Пинг: {currentPing:F0}ms\n" +
                  $"  Пользователь: {myUsername}");
    }

    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[SocketIO] {message}");
        }
    }

    // Properties
    public bool IsConnected => isConnected;
    public string CurrentRoomId => currentRoomId;
    public string SessionId => sessionId;
    public string Username => myUsername;
    public string UserId => myUserId;
    public float Ping => currentPing;
    public int MessagesSent => messagesSent;
    public int MessagesReceived => messagesReceived;
}

// ═══════════════════════════════════════════════════════════════
// DATA CLASSES FOR NETWORK COMMUNICATION
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Serializable Vector3 for network transmission
/// </summary>
[Serializable]
public class SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3() { }

    public SerializableVector3(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    public SerializableVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

/// <summary>
/// Player position/movement update request
/// </summary>
[Serializable]
public class PlayerUpdateRequest
{
    public SerializableVector3 position;
    public SerializableVector3 rotation;
    public SerializableVector3 velocity;
    public bool isGrounded;
}

/// <summary>
/// Animation update request
/// </summary>
[Serializable]
public class AnimationRequest
{
    public string animation;
    public float speed;
}

/// <summary>
/// Attack request
/// </summary>
[Serializable]
public class AttackRequest
{
    public string attackType;
    public string targetType;
    public string targetId;
    public float damage;
    public int skillId;
}

/// <summary>
/// Damage received request
/// </summary>
[Serializable]
public class DamageRequest
{
    public float damage;
    public float currentHealth;
    public string attackerId;
}

/// <summary>
/// Respawn request
/// </summary>
[Serializable]
public class RespawnRequest
{
    public SerializableVector3 position;
}

/// <summary>
/// Enemy damaged request
/// </summary>
[Serializable]
public class EnemyDamagedRequest
{
    public string roomId;
    public string enemyId;
    public float damage;
    public float currentHealth;
}

/// <summary>
/// Enemy killed request
/// </summary>
[Serializable]
public class EnemyKilledRequest
{
    public string roomId;
    public string enemyId;
    public SerializableVector3 position;
}

/// <summary>
/// Join room request
/// </summary>
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

/// <summary>
/// Create room request
/// </summary>
[Serializable]
public class CreateRoomRequest
{
    public string roomName;
    public int maxPlayers;
    public bool isPrivate;
    public string password;
    public string characterClass;
    public string username;
    public int level;
}

/// <summary>
/// Room info
/// </summary>
[Serializable]
public class RoomInfo
{
    public string roomId;
    public string roomName;
    public int currentPlayers;
    public int maxPlayers;
    public bool canJoin;
    public string status;
    public bool isHost;
}

/// <summary>
/// Create room response
/// </summary>
[Serializable]
public class CreateRoomResponse
{
    public bool success;
    public string message;
    public RoomInfo room;
}

/// <summary>
/// Join room response
/// </summary>
[Serializable]
public class JoinRoomResponse
{
    public bool success;
    public string message;
    public RoomInfo room;
}

/// <summary>
/// Room list response
/// </summary>
[Serializable]
public class RoomListResponse
{
    public bool success;
    public RoomInfo[] rooms;
    public int total;
}
