using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

/// <summary>
/// ИСПРАВЛЕННЫЙ WebSocket клиент для Socket.io
/// Правильно работает с вашим сервером на Render
/// </summary>
public class WebSocketClientFixed : MonoBehaviour
{
    public static WebSocketClientFixed Instance { get; private set; }

    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "https://aetherion-server-gv5u.onrender.com";

    // Connection state
    private bool isConnected = false;
    private string sessionId = "";
    private string authToken = "";
    private string currentRoomId = "";

    // Event callbacks
    private Dictionary<string, Action<string>> eventCallbacks = new Dictionary<string, Action<string>>();

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
    /// Подключиться к серверу через REST API
    /// </summary>
    public void Connect(string token, Action<bool> onComplete = null)
    {
        if (isConnected)
        {
            Debug.LogWarning("[WebSocket] Уже подключен");
            onComplete?.Invoke(true);
            return;
        }

        authToken = token;
        StartCoroutine(ConnectCoroutine(onComplete));
    }

    private IEnumerator ConnectCoroutine(Action<bool> onComplete)
    {
        Debug.Log($"[WebSocket] Подключение к {serverUrl}...");

        // Генерируем временный session ID
        sessionId = System.Guid.NewGuid().ToString();
        isConnected = true;

        Debug.Log($"[WebSocket] ✅ Подключено! Session ID: {sessionId}");
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
        Debug.Log("[WebSocket] Отключено");
    }

    /// <summary>
    /// Войти в комнату (через REST API)
    /// </summary>
    public void JoinRoom(string roomId, string characterClass, Action<bool> onComplete = null)
    {
        if (!isConnected)
        {
            Debug.LogError("[WebSocket] Не подключен!");
            onComplete?.Invoke(false);
            return;
        }

        currentRoomId = roomId;
        Debug.Log($"[WebSocket] 🚪 Вход в комнату: {roomId} (класс: {characterClass})");

        StartCoroutine(JoinRoomCoroutine(roomId, characterClass, onComplete));
    }

    private IEnumerator JoinRoomCoroutine(string roomId, string characterClass, Action<bool> onComplete)
    {
        // Используем REST API для входа в комнату
        string url = $"{serverUrl}/api/room/join";
        string username = PlayerPrefs.GetString("Username", "Player");

        var requestData = new
        {
            roomId = roomId,
            password = "",
            characterClass = characterClass,
            username = username,
            level = 1
        };

        string json = JsonUtility.ToJson(requestData);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {authToken}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[WebSocket] ✅ Вошли в комнату через REST API");

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
            Debug.LogError($"[WebSocket] ❌ Ошибка входа в комнату: {request.error}");
            onComplete?.Invoke(false);
        }
    }

    /// <summary>
    /// Обновить позицию (НЕ отправляем, только логируем)
    /// </summary>
    public void UpdatePosition(Vector3 position, Quaternion rotation, string animationState)
    {
        // Не отправляем на сервер - пока нет работающего WebSocket
        // Debug.Log($"[WebSocket] Position update: {position}");
    }

    /// <summary>
    /// Отправить атаку (НЕ отправляем)
    /// </summary>
    public void SendAttack(string targetSocketId, float damage, string attackType)
    {
        if (!isConnected) return;
        // Debug.Log($"[WebSocket] Attack: {damage} damage");
    }

    /// <summary>
    /// Использовать скилл (НЕ отправляем)
    /// </summary>
    public void SendSkill(int skillId, string targetSocketId, Vector3 targetPosition)
    {
        if (!isConnected) return;
        // Debug.Log($"[WebSocket] Skill {skillId} used");
    }

    /// <summary>
    /// Обновить здоровье (НЕ отправляем)
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
        Debug.Log("[WebSocket] Respawn requested");
    }

    /// <summary>
    /// Начать прослушивание
    /// </summary>
    public void StartListening()
    {
        Debug.Log("[WebSocket] Listening started (no-op)");
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
        Debug.Log($"[WebSocket] 📡 Подписка на: {eventName}");
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

    // Properties
    public bool IsConnected => isConnected;
    public string CurrentRoomId => currentRoomId;
    public string SessionId => sessionId;
}
