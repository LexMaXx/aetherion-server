using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

/// <summary>
/// Socket.IO клиент для Unity с поддержкой всех мультиплеер событий
/// Подключается к серверу на Render.com и синхронизирует игроков
/// </summary>
public class SocketIOClient : MonoBehaviour
{
    public static SocketIOClient Instance { get; private set; }

    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "https://aetherion-server-gv5u.onrender.com";
    [SerializeField] private float heartbeatInterval = 20f;
    [SerializeField] private float pollInterval = 0.05f; // 20 Hz

    // Connection state
    private bool isConnected = false;
    private bool isConnecting = false;
    private string sessionId = "";
    private string authToken = "";
    private string currentRoomId = "";
    private string myUsername = "";

    // Heartbeat
    private float lastHeartbeat = 0f;

    // Event callbacks
    private Dictionary<string, Action<string>> eventCallbacks = new Dictionary<string, Action<string>>();

    // Socket.IO URLs
    private string HandshakeUrl => $"{serverUrl}/socket.io/?EIO=4&transport=polling";
    private string PollUrl => $"{serverUrl}/socket.io/?EIO=4&transport=polling&sid={sessionId}";

    // Listening coroutine
    private Coroutine listeningCoroutine;

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

    void Update()
    {
        // Heartbeat
        if (isConnected && Time.time - lastHeartbeat > heartbeatInterval)
        {
            SendHeartbeat();
            lastHeartbeat = Time.time;
        }
    }

    /// <summary>
    /// Подключиться к серверу
    /// </summary>
    public void Connect(string token, Action<bool> onComplete = null)
    {
        if (isConnected || isConnecting)
        {
            Debug.LogWarning("[SocketIO] Уже подключен или подключается");
            onComplete?.Invoke(false);
            return;
        }

        authToken = token;
        myUsername = PlayerPrefs.GetString("Username", "Player");
        StartCoroutine(ConnectCoroutine(onComplete));
    }

    /// <summary>
    /// Корутина подключения
    /// </summary>
    private IEnumerator ConnectCoroutine(Action<bool> onComplete)
    {
        isConnecting = true;
        Debug.Log($"[SocketIO] 🔌 Подключение к {serverUrl}...");

        // Socket.io handshake
        UnityWebRequest request = UnityWebRequest.Get(HandshakeUrl);
        request.SetRequestHeader("Authorization", $"Bearer {authToken}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Parse session ID from response
            string response = request.downloadHandler.text;
            Debug.Log($"[SocketIO] Handshake response: {response}");

            // Socket.io response format: 0{"sid":"xxxxx","upgrades":["websocket"],"pingInterval":25000,"pingTimeout":60000}
            int sidStart = response.IndexOf("\"sid\":\"") + 7;
            int sidEnd = response.IndexOf("\"", sidStart);
            if (sidStart > 6 && sidEnd > sidStart)
            {
                sessionId = response.Substring(sidStart, sidEnd - sidStart);
                isConnected = true;
                isConnecting = false;
                lastHeartbeat = Time.time;

                Debug.Log($"[SocketIO] ✅ Подключено! Session ID: {sessionId}");
                onComplete?.Invoke(true);

                // Start listening for events
                listeningCoroutine = StartCoroutine(ListenCoroutine());
            }
            else
            {
                Debug.LogError("[SocketIO] ❌ Не удалось получить session ID");
                isConnecting = false;
                onComplete?.Invoke(false);
            }
        }
        else
        {
            Debug.LogError($"[SocketIO] ❌ Ошибка подключения: {request.error}");
            isConnecting = false;
            onComplete?.Invoke(false);
        }
    }

    /// <summary>
    /// Отключиться от сервера
    /// </summary>
    public void Disconnect()
    {
        if (!isConnected) return;

        Debug.Log("[SocketIO] 🔌 Отключение...");

        // Stop listening
        if (listeningCoroutine != null)
        {
            StopCoroutine(listeningCoroutine);
            listeningCoroutine = null;
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
            Debug.Log($"[SocketIO] 📤 Отправлено: {eventName}");
        }
        else
        {
            // Ignore 400 errors - Socket.IO sometimes returns them even on success
            if (request.responseCode != 400)
            {
                Debug.LogWarning($"[SocketIO] ⚠️ Ошибка отправки '{eventName}': {request.error}");
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
        Debug.Log($"[SocketIO] 📡 Подписка на событие: {eventName}");
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

    /// <summary>
    /// Отправить heartbeat (ping)
    /// </summary>
    private void SendHeartbeat()
    {
        if (!isConnected) return;
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
            Debug.Log("[SocketIO] 💓 Heartbeat OK");
        }
    }

    /// <summary>
    /// Корутина прослушивания сообщений от сервера
    /// </summary>
    private IEnumerator ListenCoroutine()
    {
        Debug.Log("[SocketIO] 👂 Начинаем прослушивание событий...");

        while (isConnected)
        {
            UnityWebRequest request = UnityWebRequest.Get(PollUrl);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;

                // Parse Socket.io messages
                if (!string.IsNullOrEmpty(response) && response.Length > 2)
                {
                    ParseSocketIOMessages(response);
                }
            }

            yield return new WaitForSeconds(pollInterval);
        }

        Debug.Log("[SocketIO] 👂 Прослушивание остановлено");
    }

    /// <summary>
    /// Парсинг Socket.IO сообщений
    /// </summary>
    private void ParseSocketIOMessages(string response)
    {
        // Socket.IO может отправить несколько сообщений в одном ответе
        // Format: 42["event_name",{data}]

        if (response.StartsWith("42["))
        {
            try
            {
                // Extract event name and data
                int eventStart = response.IndexOf("[\"") + 2;
                int eventEnd = response.IndexOf("\",", eventStart);

                if (eventEnd > eventStart)
                {
                    string eventName = response.Substring(eventStart, eventEnd - eventStart);

                    // Find JSON data (everything between the comma and the final ])
                    int dataStart = eventEnd + 2;
                    int dataEnd = response.LastIndexOf(']');

                    if (dataEnd > dataStart)
                    {
                        string jsonData = response.Substring(dataStart, dataEnd - dataStart);

                        Debug.Log($"[SocketIO] 📨 Получено событие: {eventName}");

                        // Invoke callbacks
                        if (eventCallbacks.ContainsKey(eventName))
                        {
                            eventCallbacks[eventName]?.Invoke(jsonData);
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

    // ===== GAME-SPECIFIC METHODS =====

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

        // Subscribe to room_players event BEFORE joining
        On("room_players", (data) =>
        {
            Debug.Log($"[SocketIO] ✅ Получен список игроков в комнате: {data}");
            onComplete?.Invoke(true);
        });

        // Send join_room event
        var joinData = new JoinRoomRequest
        {
            roomId = roomId,
            username = myUsername,
            characterClass = characterClass,
            userId = PlayerPrefs.GetString("UserId", "")
        };

        string json = JsonUtility.ToJson(joinData);
        Emit("join_room", json);

        Debug.Log($"[SocketIO] 🚪 Вход в комнату: {roomId} как {characterClass}");
    }

    /// <summary>
    /// Обновить позицию и движение игрока
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
        Debug.Log($"[SocketIO] ⚔️ Атака отправлена: {targetType} {targetId}, урон {damage}");
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
        Debug.Log($"[SocketIO] 💔 Отправлен урон: {damage}, здоровье: {currentHealth}");
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
        Debug.Log($"[SocketIO] 🔄 Респавн отправлен");
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

    // Properties
    public bool IsConnected => isConnected;
    public string CurrentRoomId => currentRoomId;
    public string SessionId => sessionId;
    public string Username => myUsername;
}

// ===== REQUEST DATA CLASSES =====

[Serializable]
public class JoinRoomRequest
{
    public string roomId;
    public string username;
    public string characterClass;
    public string userId;
}

[Serializable]
public class PlayerUpdateRequest
{
    public SerializableVector3 position;
    public SerializableVector3 rotation;
    public SerializableVector3 velocity;
    public bool isGrounded;
}

[Serializable]
public class AnimationRequest
{
    public string animation;
    public float speed;
}

[Serializable]
public class AttackRequest
{
    public string attackType;
    public string targetType;
    public string targetId;
    public float damage;
    public int skillId;
}

[Serializable]
public class DamageRequest
{
    public float damage;
    public float currentHealth;
    public string attackerId;
}

[Serializable]
public class RespawnRequest
{
    public SerializableVector3 position;
}

[Serializable]
public class EnemyDamagedRequest
{
    public string roomId;
    public string enemyId;
    public float damage;
    public float currentHealth;
}

[Serializable]
public class EnemyKilledRequest
{
    public string roomId;
    public string enemyId;
    public SerializableVector3 position;
}

[Serializable]
public class SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}
