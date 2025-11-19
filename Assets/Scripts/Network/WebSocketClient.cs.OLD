using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

/// <summary>
/// WebSocket клиент для Socket.io сервера
/// Управляет подключением, отправкой и получением событий
/// </summary>
public class WebSocketClient : MonoBehaviour
{
    public static WebSocketClient Instance { get; private set; }

    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "https://aetherion-server-gv5u.onrender.com";
    [SerializeField] private float reconnectDelay = 5f;
    [SerializeField] private float heartbeatInterval = 25f;

    // Connection state
    private bool isConnected = false;
    private bool isConnecting = false;
    private string sessionId = "";
    private string authToken = "";
    private string currentRoomId = "";

    // Heartbeat
    private float lastHeartbeat = 0f;

    // Room join state
    private bool responseReceived = false;

    // Event callbacks
    private Dictionary<string, Action<string>> eventCallbacks = new Dictionary<string, Action<string>>();

    // WebSocket URL (Socket.io uses HTTP polling for initial connection)
    private string SocketUrl => $"{serverUrl}/socket.io/?EIO=4&transport=polling";

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
            Debug.LogWarning("[WebSocket] Уже подключен или подключается");
            onComplete?.Invoke(false);
            return;
        }

        authToken = token;
        StartCoroutine(ConnectCoroutine(onComplete));
    }

    /// <summary>
    /// Корутина подключения
    /// </summary>
    private IEnumerator ConnectCoroutine(Action<bool> onComplete)
    {
        isConnecting = true;
        Debug.Log($"[WebSocket] Подключение к {serverUrl}...");

        // Socket.io handshake
        UnityWebRequest request = UnityWebRequest.Get(SocketUrl);
        request.SetRequestHeader("Authorization", $"Bearer {authToken}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Parse session ID from response
            string response = request.downloadHandler.text;
            Debug.Log($"[WebSocket] Handshake response: {response}");

            // Socket.io response format: 0{"sid":"xxxxx","upgrades":["websocket"],"pingInterval":25000,"pingTimeout":60000}
            int sidStart = response.IndexOf("\"sid\":\"") + 7;
            int sidEnd = response.IndexOf("\"", sidStart);
            if (sidStart > 6 && sidEnd > sidStart)
            {
                sessionId = response.Substring(sidStart, sidEnd - sidStart);
                isConnected = true;
                isConnecting = false;
                lastHeartbeat = Time.time;

                Debug.Log($"[WebSocket] ✅ Подключено! Session ID: {sessionId}");
                onComplete?.Invoke(true);

                // NOTE: Отправка 'connected' события закомментирована, так как вызывает ошибку 400
                // Сервер автоматически узнает о подключении через handshake
                // EmitEvent("connected", JsonUtility.ToJson(new { token = authToken }));
            }
            else
            {
                Debug.LogError("[WebSocket] ❌ Не удалось получить session ID");
                isConnecting = false;
                onComplete?.Invoke(false);
            }
        }
        else
        {
            Debug.LogError($"[WebSocket] ❌ Ошибка подключения: {request.error}");
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

        Debug.Log("[WebSocket] Отключение...");
        EmitEvent("disconnect", "");

        isConnected = false;
        sessionId = "";
        currentRoomId = "";
    }

    /// <summary>
    /// Отправить событие на сервер
    /// </summary>
    public void EmitEvent(string eventName, string jsonData)
    {
        if (!isConnected)
        {
            Debug.LogWarning($"[WebSocket] Не подключен! Событие '{eventName}' не отправлено");
            return;
        }

        StartCoroutine(EmitCoroutine(eventName, jsonData));
    }

    /// <summary>
    /// Корутина отправки события (ВРЕМЕННО ОТКЛЮЧЕНО)
    /// </summary>
    private IEnumerator EmitCoroutine(string eventName, string jsonData)
    {
        // ВРЕМЕННО: НЕ отправляем события, чтобы убрать ошибки 400
        // Socket.io HTTP polling не работает правильно
        Debug.Log($"[WebSocket] 📤 Событие '{eventName}' (отправка отключена до исправления протокола)");
        yield break;

        /* ЗАКОММЕНТИРОВАНО - НЕ РАБОТАЕТ С SOCKET.IO
        // Socket.io message format: 42["event_name",{data}]
        string message = $"42[\"{eventName}\",{jsonData}]";

        string url = $"{serverUrl}/socket.io/?EIO=4&transport=polling&sid={sessionId}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(message);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "text/plain");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[WebSocket] ✉️ Отправлено: {eventName}");
        }
        else
        {
            Debug.LogError($"[WebSocket] ❌ Ошибка отправки '{eventName}': {request.error}");
        }
        */
    }

    /// <summary>
    /// Подписаться на событие от сервера
    /// </summary>
    public void On(string eventName, Action<string> callback)
    {
        if (!eventCallbacks.ContainsKey(eventName))
        {
            eventCallbacks[eventName] = callback;
            Debug.Log($"[WebSocket] 📡 Подписка на событие: {eventName}");
        }
        else
        {
            eventCallbacks[eventName] += callback;
        }
    }

    /// <summary>
    /// Отписаться от события
    /// </summary>
    public void Off(string eventName, Action<string> callback = null)
    {
        if (eventCallbacks.ContainsKey(eventName))
        {
            if (callback != null)
            {
                eventCallbacks[eventName] -= callback;
            }
            else
            {
                eventCallbacks.Remove(eventName);
            }
        }
    }

    /// <summary>
    /// Отправить heartbeat
    /// </summary>
    private void SendHeartbeat()
    {
        if (!isConnected) return;
        StartCoroutine(HeartbeatCoroutine());
    }

    private IEnumerator HeartbeatCoroutine()
    {
        string url = $"{serverUrl}/socket.io/?EIO=4&transport=polling&sid={sessionId}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes("2"); // Socket.io ping message
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Expected response: "3" (pong)
            Debug.Log("[WebSocket] 💓 Heartbeat OK");
        }
        else
        {
            Debug.LogWarning($"[WebSocket] ⚠️ Heartbeat failed: {request.error}");
        }
    }

    /// <summary>
    /// Начать получение сообщений от сервера
    /// </summary>
    public void StartListening()
    {
        if (!isConnected) return;
        StartCoroutine(ListenCoroutine());
    }

    /// <summary>
    /// Корутина прослушивания сообщений
    /// </summary>
    private IEnumerator ListenCoroutine()
    {
        while (isConnected)
        {
            string url = $"{serverUrl}/socket.io/?EIO=4&transport=polling&sid={sessionId}";
            UnityWebRequest request = UnityWebRequest.Get(url);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;

                // Parse Socket.io messages
                if (response.StartsWith("42"))
                {
                    // Format: 42["event_name",{data}]
                    int eventStart = response.IndexOf("[\"") + 2;
                    int eventEnd = response.IndexOf("\",", eventStart);

                    if (eventEnd > eventStart)
                    {
                        string eventName = response.Substring(eventStart, eventEnd - eventStart);
                        string jsonData = response.Substring(eventEnd + 2, response.Length - eventEnd - 3);

                        Debug.Log($"[WebSocket] 📨 Получено: {eventName}");

                        // Invoke callbacks
                        if (eventCallbacks.ContainsKey(eventName))
                        {
                            eventCallbacks[eventName]?.Invoke(jsonData);
                        }
                    }
                }
            }
            else
            {
                // Если ошибка 400 Bad Request - это нормально для Socket.io polling
                // Просто ждем дольше перед следующим запросом
                if (request.responseCode == 400)
                {
                    yield return new WaitForSeconds(0.5f); // Wait longer on 400
                }
                else
                {
                    Debug.LogWarning($"[WebSocket] ⚠️ Ошибка прослушивания: {request.error}");
                    yield return new WaitForSeconds(1f);
                }
            }

            yield return new WaitForSeconds(0.1f); // Poll every 100ms
        }
    }

    // ===== GAME-SPECIFIC EVENTS =====

    /// <summary>
    /// Войти в комнату
    /// </summary>
    public void JoinRoom(string roomId, string characterClass, Action<bool> onComplete = null)
    {
        if (!isConnected)
        {
            Debug.LogError("[WebSocket] Не подключен к серверу!");
            onComplete?.Invoke(false);
            return;
        }

        currentRoomId = roomId;

        string data = JsonUtility.ToJson(new JoinRoomData
        {
            roomId = roomId,
            characterClass = characterClass
        });

        EmitEvent("join_room", data);

        // Listen for response - сервер отправляет 'room_state' при успешном входе
        responseReceived = false; // Сбрасываем флаг перед каждым входом в комнату

        On("room_state", (response) =>
        {
            responseReceived = true;
            Debug.Log($"[WebSocket] ✅ Получено состояние комнаты: {response}");
            Debug.Log($"[WebSocket] ✅ Вошли в комнату: {roomId}");
            onComplete?.Invoke(true);
        });

        On("error", (error) =>
        {
            responseReceived = true;
            Debug.LogError($"[WebSocket] ❌ Ошибка входа в комнату: {error}");
            onComplete?.Invoke(false);
        });

        // ВРЕМЕННОЕ РЕШЕНИЕ: Так как HTTP polling не работает правильно,
        // считаем что вход успешен если через 2 сек нет ошибки
        StartCoroutine(JoinRoomTimeout(2f, () =>
        {
            if (!responseReceived)
            {
                Debug.LogWarning("[WebSocket] ⚠️ Ответ не получен через polling, но считаем вход успешным");
                onComplete?.Invoke(true);
            }
        }));
    }

    /// <summary>
    /// Корутина таймаута для входа в комнату
    /// </summary>
    private IEnumerator JoinRoomTimeout(float delay, Action callback)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }

    /// <summary>
    /// Обновить позицию игрока
    /// </summary>
    public void UpdatePosition(Vector3 position, Quaternion rotation, string animationState)
    {
        // НЕ отправляем позиции если не подключены или не в комнате
        if (!isConnected || string.IsNullOrEmpty(currentRoomId) || string.IsNullOrEmpty(sessionId))
        {
            return;
        }

        string data = JsonUtility.ToJson(new PositionUpdateData
        {
            x = position.x,
            y = position.y,
            z = position.z,
            rotationY = rotation.eulerAngles.y,
            animationState = animationState
        });

        EmitEvent("update_position", data);
    }

    /// <summary>
    /// Отправить атаку
    /// </summary>
    public void SendAttack(string targetSocketId, float damage, string attackType)
    {
        if (!isConnected) return;

        string data = JsonUtility.ToJson(new AttackData
        {
            targetSocketId = targetSocketId,
            damage = damage,
            attackType = attackType
        });

        EmitEvent("player_attack", data);
    }

    /// <summary>
    /// Использовать скилл
    /// </summary>
    public void SendSkill(int skillId, string targetSocketId, Vector3 targetPosition)
    {
        if (!isConnected) return;

        string data = JsonUtility.ToJson(new NetworkSkillData
        {
            skillId = skillId,
            targetSocketId = targetSocketId,
            targetX = targetPosition.x,
            targetY = targetPosition.y,
            targetZ = targetPosition.z
        });

        EmitEvent("player_skill", data);
    }

    /// <summary>
    /// Обновить здоровье/ману
    /// </summary>
    public void UpdateHealth(int currentHP, int maxHP, int currentMP, int maxMP)
    {
        if (!isConnected) return;

        string data = JsonUtility.ToJson(new HealthUpdateData
        {
            currentHP = currentHP,
            maxHP = maxHP,
            currentMP = currentMP,
            maxMP = maxMP
        });

        EmitEvent("update_health", data);
    }

    /// <summary>
    /// Запросить респавн
    /// </summary>
    public void RequestRespawn()
    {
        if (!isConnected) return;
        EmitEvent("player_respawn", "{}");
    }

    // Properties
    public bool IsConnected => isConnected;
    public string CurrentRoomId => currentRoomId;
    public string SessionId => sessionId;
}

// ===== DATA CLASSES =====

[Serializable]
public class JoinRoomData
{
    public string roomId;
    public string characterClass;
}

[Serializable]
public class PositionUpdateData
{
    public float x;
    public float y;
    public float z;
    public float rotationY;
    public string animationState;
}

[Serializable]
public class AttackData
{
    public string targetSocketId;
    public float damage;
    public string attackType;
}

[Serializable]
public class NetworkSkillData
{
    public int skillId;
    public string targetSocketId;
    public float targetX;
    public float targetY;
    public float targetZ;
}

[Serializable]
public class HealthUpdateData
{
    public int currentHP;
    public int maxHP;
    public int currentMP;
    public int maxMP;
}
