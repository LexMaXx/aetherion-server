using UnityEngine;
using System;
using System.Collections.Generic;
using SocketIOClient;
using System.Text.Json;

/// <summary>
/// Socket.IO менеджер для реального мультиплеера
/// Использует правильную Socket.IO библиотеку
/// </summary>
public class SocketIOManager : MonoBehaviour
{
    public static SocketIOManager Instance { get; private set; }

    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "https://aetherion-server-gv5u.onrender.com";

    // Socket.IO клиент
    private SocketIO socket;

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
    /// Подключиться к серверу
    /// </summary>
    public void Connect(string token, Action<bool> onComplete = null)
    {
        if (isConnected || socket != null)
        {
            Debug.LogWarning("[SocketIO] Уже подключен или подключается");
            onComplete?.Invoke(false);
            return;
        }

        authToken = token;

        try
        {
            // Создаём Socket.IO клиент
            var uri = new Uri(serverUrl);
            socket = new SocketIO(uri, new SocketIOOptions
            {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                ExtraHeaders = new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {token}" }
                }
            });

            // Подписываемся на события подключения
            socket.OnConnected += (sender, e) =>
            {
                isConnected = true;
                sessionId = socket.Id;
                Debug.Log($"[SocketIO] ✅ Подключено! Socket ID: {sessionId}");
                onComplete?.Invoke(true);
            };

            socket.OnDisconnected += (sender, e) =>
            {
                isConnected = false;
                Debug.Log("[SocketIO] ❌ Отключено от сервера");
            };

            socket.OnError += (sender, e) =>
            {
                Debug.LogError($"[SocketIO] ❌ Ошибка: {e}");
            };

            socket.OnReconnectAttempt += (sender, e) =>
            {
                Debug.Log($"[SocketIO] 🔄 Попытка переподключения #{e}");
            };

            // Подключаемся
            Debug.Log($"[SocketIO] Подключение к {serverUrl}...");
            socket.ConnectAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"[SocketIO] ❌ Ошибка подключения: {task.Exception?.Message}");
                    onComplete?.Invoke(false);
                }
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SocketIO] ❌ Исключение при подключении: {ex.Message}");
            onComplete?.Invoke(false);
        }
    }

    /// <summary>
    /// Отключиться от сервера
    /// </summary>
    public void Disconnect()
    {
        if (socket != null)
        {
            socket.DisconnectAsync();
            socket.Dispose();
            socket = null;
        }

        isConnected = false;
        sessionId = "";
        currentRoomId = "";
        Debug.Log("[SocketIO] Отключение завершено");
    }

    /// <summary>
    /// Отправить событие на сервер
    /// </summary>
    public void EmitEvent(string eventName, object data)
    {
        if (!isConnected || socket == null)
        {
            Debug.LogWarning($"[SocketIO] Не подключен! Событие '{eventName}' не отправлено");
            return;
        }

        try
        {
            socket.EmitAsync(eventName, data);
            Debug.Log($"[SocketIO] ✉️ Отправлено: {eventName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SocketIO] ❌ Ошибка отправки '{eventName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Подписаться на событие от сервера
    /// </summary>
    public void On(string eventName, Action<JsonElement> callback)
    {
        if (socket == null)
        {
            Debug.LogError("[SocketIO] Socket не инициализирован!");
            return;
        }

        socket.On(eventName, response =>
        {
            try
            {
                var data = response.GetValue<JsonElement>();
                Debug.Log($"[SocketIO] 📨 Получено: {eventName}");
                callback?.Invoke(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SocketIO] ❌ Ошибка обработки '{eventName}': {ex.Message}");
            }
        });

        Debug.Log($"[SocketIO] 📡 Подписка на событие: {eventName}");
    }

    /// <summary>
    /// Отписаться от события
    /// </summary>
    public void Off(string eventName)
    {
        if (socket != null)
        {
            socket.Off(eventName);
            Debug.Log($"[SocketIO] ❌ Отписка от события: {eventName}");
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
            Debug.LogError("[SocketIO] Не подключен к серверу!");
            onComplete?.Invoke(false);
            return;
        }

        currentRoomId = roomId;

        var data = new
        {
            roomId = roomId,
            characterClass = characterClass
        };

        Debug.Log($"[SocketIO] 🚪 Вход в комнату: {roomId} (класс: {characterClass})");

        // Подписываемся на ответ сервера
        On("room_state", (response) =>
        {
            Debug.Log($"[SocketIO] ✅ Получено состояние комнаты!");
            onComplete?.Invoke(true);
        });

        On("error", (response) =>
        {
            var error = response.ToString();
            Debug.LogError($"[SocketIO] ❌ Ошибка входа в комнату: {error}");
            onComplete?.Invoke(false);
        });

        // Отправляем событие
        EmitEvent("join_room", data);
    }

    /// <summary>
    /// Обновить позицию игрока
    /// </summary>
    public void UpdatePosition(Vector3 position, Quaternion rotation, string animationState)
    {
        if (!isConnected || string.IsNullOrEmpty(currentRoomId))
        {
            return;
        }

        var data = new
        {
            x = position.x,
            y = position.y,
            z = position.z,
            rotationY = rotation.eulerAngles.y,
            animationState = animationState
        };

        EmitEvent("update_position", data);
    }

    /// <summary>
    /// Отправить атаку
    /// </summary>
    public void SendAttack(string targetSocketId, float damage, string attackType)
    {
        if (!isConnected) return;

        var data = new
        {
            targetSocketId = targetSocketId,
            damage = damage,
            attackType = attackType
        };

        EmitEvent("player_attack", data);
        Debug.Log($"[SocketIO] ⚔️ Атака отправлена: {damage} урона");
    }

    /// <summary>
    /// Использовать скилл
    /// </summary>
    public void SendSkill(int skillId, string targetSocketId, Vector3 targetPosition)
    {
        if (!isConnected) return;

        var data = new
        {
            skillId = skillId,
            targetSocketId = targetSocketId,
            targetX = targetPosition.x,
            targetY = targetPosition.y,
            targetZ = targetPosition.z
        };

        EmitEvent("player_skill", data);
        Debug.Log($"[SocketIO] 🔮 Скилл {skillId} использован");
    }

    /// <summary>
    /// Обновить здоровье/ману
    /// </summary>
    public void UpdateHealth(int currentHP, int maxHP, int currentMP, int maxMP)
    {
        if (!isConnected) return;

        var data = new
        {
            currentHP = currentHP,
            maxHP = maxHP,
            currentMP = currentMP,
            maxMP = maxMP
        };

        EmitEvent("update_health", data);
    }

    /// <summary>
    /// Запросить респавн
    /// </summary>
    public void RequestRespawn()
    {
        if (!isConnected) return;
        EmitEvent("player_respawn", new { });
        Debug.Log("[SocketIO] 💀 Запрос респавна");
    }

    /// <summary>
    /// Начать прослушивание событий
    /// </summary>
    public void StartListening()
    {
        Debug.Log("[SocketIO] 📡 Listening started");
    }

    // Properties
    public bool IsConnected => isConnected;
    public string CurrentRoomId => currentRoomId;
    public string SessionId => sessionId;
}
