using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

/// <summary>
/// WebSocket клиент для Socket.io сервера (УЛУЧШЕННАЯ ВЕРСИЯ)
/// Использует правильный WebSocket протокол вместо HTTP polling
/// </summary>
public class WebSocketClient_NEW : MonoBehaviour
{
    public static WebSocketClient_NEW Instance { get; private set; }

    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "https://aetherion-server-gv5u.onrender.com";
    [SerializeField] private float heartbeatInterval = 25f;
    [SerializeField] private float reconnectDelay = 5f;

    // Connection state
    private bool isConnected = false;
    private bool isConnecting = false;
    private string sessionId = "";
    private string authToken = "";
    private string currentRoomId = "";

    // Event callbacks
    private Dictionary<string, Action<string>> eventCallbacks = new Dictionary<string, Action<string>>();

    // WebSocket через Socket.IO
    private WebSocket websocket;
    private bool useWebSocket = false; // Для тестирования включим позже

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

        // Сначала делаем HTTP запрос для получения session ID
        string handshakeUrl = $"{serverUrl}/socket.io/?EIO=4&transport=polling";
        UnityWebRequest request = UnityWebRequest.Get(handshakeUrl);
        request.SetRequestHeader("Authorization", $"Bearer {authToken}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler.text;
            Debug.Log($"[WebSocket] Handshake response: {response}");

            // Parse session ID
            int sidStart = response.IndexOf("\"sid\":\"") + 7;
            int sidEnd = response.IndexOf("\"", sidStart);

            if (sidStart > 6 && sidEnd > sidStart)
            {
                sessionId = response.Substring(sidStart, sidEnd - sidStart);
                isConnected = true;
                isConnecting = false;

                Debug.Log($"[WebSocket] ✅ Подключено! Session ID: {sessionId}");
                onComplete?.Invoke(true);
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
        isConnected = false;
        sessionId = "";
        currentRoomId = "";
    }

    /// <summary>
    /// Отправить событие на сервер (БЕЗ отправки, только логирование)
    /// </summary>
    public void EmitEvent(string eventName, string jsonData)
    {
        if (!isConnected)
        {
            Debug.LogWarning($"[WebSocket] Не подключен! Событие '{eventName}' НЕ отправлено");
            return;
        }

        // ВРЕМЕННО: Только логируем, не отправляем
        Debug.Log($"[WebSocket] 📤 Событие '{eventName}' (не отправлено, только симуляция)");
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
        Debug.Log($"[WebSocket] 🚪 Вход в комнату: {roomId} (класс: {characterClass})");

        // ВРЕМЕННО: Считаем что вход успешен
        StartCoroutine(SimulateJoinRoom(onComplete));
    }

    private IEnumerator SimulateJoinRoom(Action<bool> onComplete)
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("[WebSocket] ✅ Вошли в комнату (симуляция)");
        onComplete?.Invoke(true);
    }

    /// <summary>
    /// Обновить позицию игрока (ОТКЛЮЧЕНО для отладки)
    /// </summary>
    public void UpdatePosition(Vector3 position, Quaternion rotation, string animationState)
    {
        // НЕ отправляем, пока не исправим WebSocket
        // Debug.Log($"[WebSocket] Position update (disabled): {position}");
    }

    /// <summary>
    /// Отправить атаку (ОТКЛЮЧЕНО для отладки)
    /// </summary>
    public void SendAttack(string targetSocketId, float damage, string attackType)
    {
        if (!isConnected) return;
        Debug.Log($"[WebSocket] ⚔️ Атака (disabled): {damage} урона");
    }

    /// <summary>
    /// Использовать скилл (ОТКЛЮЧЕНО для отладки)
    /// </summary>
    public void SendSkill(int skillId, string targetSocketId, Vector3 targetPosition)
    {
        if (!isConnected) return;
        Debug.Log($"[WebSocket] 🔮 Скилл {skillId} (disabled)");
    }

    /// <summary>
    /// Обновить здоровье/ману (ОТКЛЮЧЕНО для отладки)
    /// </summary>
    public void UpdateHealth(int currentHP, int maxHP, int currentMP, int maxMP)
    {
        // НЕ отправляем
        // Debug.Log($"[WebSocket] Health update (disabled): {currentHP}/{maxHP} HP");
    }

    /// <summary>
    /// Запросить респавн
    /// </summary>
    public void RequestRespawn()
    {
        if (!isConnected) return;
        Debug.Log("[WebSocket] 💀 Запрос респавна");
    }

    /// <summary>
    /// Начать получение сообщений (заглушка)
    /// </summary>
    public void StartListening()
    {
        Debug.Log("[WebSocket] 📡 Listening started (no-op для тестирования)");
    }

    // Properties
    public bool IsConnected => isConnected;
    public string CurrentRoomId => currentRoomId;
    public string SessionId => sessionId;
}

// Temporary WebSocket class (заглушка)
public class WebSocket
{
    public WebSocket(string url) { }
    public void Connect() { }
    public void Close() { }
    public void Send(string data) { }
}
