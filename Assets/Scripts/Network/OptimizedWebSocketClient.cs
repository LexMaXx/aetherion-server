using UnityEngine;
using System;
using System.Collections.Generic;
using SocketIOClient;
using System.Text.Json;

/// <summary>
/// ОПТИМИЗИРОВАННЫЙ WebSocket клиент для реального времени
///
/// КЛЮЧЕВЫЕ ОПТИМИЗАЦИИ:
/// - Delta Compression: отправляем только изменения
/// - Adaptive Update Rate: динамически меняем частоту обновлений
/// - Batching: группируем несколько обновлений в один пакет
/// - Priority System: критичные события отправляются немедленно
/// - Dead Reckoning: предсказание движения между обновлениями
/// </summary>
public class OptimizedWebSocketClient : MonoBehaviour
{
    public static OptimizedWebSocketClient Instance { get; private set; }

    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "https://aetherion-server-gv5u.onrender.com";

    [Header("Network Optimization")]
    [Tooltip("Базовая частота обновлений позиции (Гц)")]
    [SerializeField] private float positionUpdateRate = 20f; // 20 Hz = 50ms

    [Tooltip("Частота обновлений для неподвижных объектов (Гц)")]
    [SerializeField] private float idleUpdateRate = 2f; // 2 Hz = 500ms

    [Tooltip("Минимальная дистанция для отправки обновления позиции")]
    [SerializeField] private float minPositionDelta = 0.01f; // 1 см

    [Tooltip("Минимальный угол для отправки обновления ротации")]
    [SerializeField] private float minRotationDelta = 1f; // 1 градус

    [Tooltip("Максимальный размер батча")]
    [SerializeField] private int maxBatchSize = 10;

    [Tooltip("Задержка перед отправкой батча (мс)")]
    [SerializeField] private float batchDelay = 50f; // 50ms

    // Socket.IO клиент
    private SocketIO socket;

    // Connection state
    private bool isConnected = false;
    private string sessionId = "";
    private string authToken = "";
    private string currentRoomId = "";

    // Delta compression - последнее отправленное состояние
    private Vector3 lastSentPosition;
    private Quaternion lastSentRotation;
    private string lastSentAnimation = "";
    private float lastPositionUpdateTime = 0f;

    // Velocity tracking для Dead Reckoning
    private Vector3 lastVelocity = Vector3.zero;
    private float lastAngularVelocity = 0f;

    // Batching
    private List<object> pendingUpdates = new List<object>();
    private float lastBatchSendTime = 0f;

    // Statistics
    private int packetsSent = 0;
    private int bytesSaved = 0;
    private float averagePing = 0f;

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
        // Автоматически отправляем батч если прошло достаточно времени
        if (pendingUpdates.Count > 0 && Time.time - lastBatchSendTime > batchDelay / 1000f)
        {
            FlushBatch();
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
        if (isConnected || socket != null)
        {
            Debug.LogWarning("[OptimizedWS] Уже подключен");
            onComplete?.Invoke(false);
            return;
        }

        authToken = token;

        try
        {
            var uri = new Uri(serverUrl);
            socket = new SocketIO(uri, new SocketIOOptions
            {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                Reconnection = true,
                ReconnectionAttempts = 5,
                ReconnectionDelay = 1000,
                ExtraHeaders = new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {token}" }
                }
            });

            // События подключения
            socket.OnConnected += (sender, e) =>
            {
                isConnected = true;
                sessionId = socket.Id;
                Debug.Log($"[OptimizedWS] ✅ Подключено! Socket ID: {sessionId}");

                // Начинаем мониторинг пинга
                StartPingMonitoring();

                onComplete?.Invoke(true);
            };

            socket.OnDisconnected += (sender, e) =>
            {
                isConnected = false;
                Debug.Log("[OptimizedWS] ❌ Отключено");
            };

            socket.OnError += (sender, e) =>
            {
                Debug.LogError($"[OptimizedWS] ❌ Ошибка: {e}");
            };

            socket.OnReconnectAttempt += (sender, e) =>
            {
                Debug.Log($"[OptimizedWS] 🔄 Переподключение #{e}");
            };

            // Подключаемся
            Debug.Log($"[OptimizedWS] Подключение к {serverUrl}...");
            socket.ConnectAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"[OptimizedWS] ❌ Ошибка: {task.Exception?.Message}");
                    onComplete?.Invoke(false);
                }
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OptimizedWS] ❌ Исключение: {ex.Message}");
            onComplete?.Invoke(false);
        }
    }

    /// <summary>
    /// Отключиться
    /// </summary>
    public void Disconnect()
    {
        if (socket != null)
        {
            FlushBatch(); // Отправляем оставшиеся данные
            socket.DisconnectAsync();
            socket.Dispose();
            socket = null;
        }

        isConnected = false;
        sessionId = "";
        currentRoomId = "";
        Debug.Log("[OptimizedWS] Отключение завершено");
    }

    // ===== EVENTS =====

    /// <summary>
    /// Отправить событие (с приоритетом)
    /// </summary>
    public void EmitEvent(string eventName, object data, bool highPriority = false)
    {
        if (!isConnected || socket == null)
        {
            Debug.LogWarning($"[OptimizedWS] Не подключен! Событие '{eventName}' пропущено");
            return;
        }

        try
        {
            if (highPriority)
            {
                // Высокоприоритетные события отправляем немедленно
                socket.EmitAsync(eventName, data);
                packetsSent++;
            }
            else
            {
                // Низкоприоритетные добавляем в батч
                AddToBatch(new { eventName, data });
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OptimizedWS] ❌ Ошибка отправки '{eventName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Подписаться на событие
    /// </summary>
    public void On(string eventName, Action<JsonElement> callback)
    {
        if (socket == null)
        {
            Debug.LogError("[OptimizedWS] Socket не инициализирован!");
            return;
        }

        socket.On(eventName, response =>
        {
            try
            {
                var data = response.GetValue<JsonElement>();
                callback?.Invoke(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OptimizedWS] ❌ Ошибка обработки '{eventName}': {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Отписаться от события
    /// </summary>
    public void Off(string eventName)
    {
        socket?.Off(eventName);
    }

    // ===== BATCHING =====

    /// <summary>
    /// Добавить событие в батч
    /// </summary>
    private void AddToBatch(object update)
    {
        pendingUpdates.Add(update);

        // Если батч полон - отправляем немедленно
        if (pendingUpdates.Count >= maxBatchSize)
        {
            FlushBatch();
        }
    }

    /// <summary>
    /// Отправить батч на сервер
    /// </summary>
    private void FlushBatch()
    {
        if (pendingUpdates.Count == 0) return;

        try
        {
            // Отправляем все события одним пакетом
            socket.EmitAsync("batch_update", new { updates = pendingUpdates });

            packetsSent++;
            lastBatchSendTime = Time.time;

            // Очищаем батч
            pendingUpdates.Clear();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OptimizedWS] ❌ Ошибка отправки батча: {ex.Message}");
        }
    }

    // ===== OPTIMIZED GAME EVENTS =====

    /// <summary>
    /// Войти в комнату (ВЫСОКИЙ ПРИОРИТЕТ)
    /// </summary>
    public void JoinRoom(string roomId, string characterClass, Action<bool> onComplete = null)
    {
        if (!isConnected)
        {
            Debug.LogError("[OptimizedWS] Не подключен!");
            onComplete?.Invoke(false);
            return;
        }

        currentRoomId = roomId;

        var data = new
        {
            roomId = roomId,
            characterClass = characterClass
        };

        Debug.Log($"[OptimizedWS] 🚪 Вход в комнату: {roomId}");

        // Подписываемся на ответ
        On("room_state", (response) =>
        {
            Debug.Log("[OptimizedWS] ✅ Получено состояние комнаты");
            onComplete?.Invoke(true);
        });

        On("error", (response) =>
        {
            Debug.LogError($"[OptimizedWS] ❌ Ошибка: {response}");
            onComplete?.Invoke(false);
        });

        // Отправляем с высоким приоритетом
        EmitEvent("join_room", data, highPriority: true);
    }

    /// <summary>
    /// ОПТИМИЗИРОВАННОЕ обновление позиции с Delta Compression
    /// </summary>
    public void UpdatePosition(Vector3 position, Quaternion rotation, string animationState, Vector3 velocity)
    {
        if (!isConnected || string.IsNullOrEmpty(currentRoomId))
            return;

        float currentTime = Time.time;

        // Адаптивная частота обновлений
        bool isMoving = velocity.sqrMagnitude > 0.01f;
        float updateInterval = isMoving ? (1f / positionUpdateRate) : (1f / idleUpdateRate);

        // Проверяем нужно ли отправлять обновление
        if (currentTime - lastPositionUpdateTime < updateInterval)
            return;

        // Delta Compression - отправляем только если есть изменения
        float positionDelta = Vector3.Distance(position, lastSentPosition);
        float rotationDelta = Quaternion.Angle(rotation, lastSentRotation);
        bool animationChanged = animationState != lastSentAnimation;

        if (positionDelta < minPositionDelta &&
            rotationDelta < minRotationDelta &&
            !animationChanged)
        {
            bytesSaved++; // Статистика экономии
            return;
        }

        // Вычисляем скорости для Dead Reckoning
        float deltaTime = currentTime - lastPositionUpdateTime;
        if (deltaTime > 0)
        {
            lastVelocity = (position - lastSentPosition) / deltaTime;
            lastAngularVelocity = rotationDelta / deltaTime;
        }

        // Создаем компактный пакет данных
        var data = new
        {
            // Позиция (только если изменилась)
            x = positionDelta >= minPositionDelta ? position.x : (float?)null,
            y = positionDelta >= minPositionDelta ? position.y : (float?)null,
            z = positionDelta >= minPositionDelta ? position.z : (float?)null,

            // Ротация (только если изменилась)
            rotY = rotationDelta >= minRotationDelta ? rotation.eulerAngles.y : (float?)null,

            // Анимация (только если изменилась)
            anim = animationChanged ? animationState : null,

            // Velocity для Dead Reckoning на других клиентах
            vx = isMoving ? velocity.x : (float?)null,
            vy = isMoving ? velocity.y : (float?)null,
            vz = isMoving ? velocity.z : (float?)null,

            // Timestamp для синхронизации
            t = currentTime
        };

        // Отправляем обновление (низкий приоритет - идёт в батч)
        EmitEvent("update_position", data, highPriority: false);

        // Сохраняем последнее состояние
        lastSentPosition = position;
        lastSentRotation = rotation;
        lastSentAnimation = animationState;
        lastPositionUpdateTime = currentTime;
    }

    /// <summary>
    /// Отправить атаку (ВЫСОКИЙ ПРИОРИТЕТ)
    /// </summary>
    public void SendAttack(string targetSocketId, float damage, string attackType)
    {
        if (!isConnected) return;

        var data = new
        {
            targetSocketId = targetSocketId,
            damage = damage,
            attackType = attackType,
            timestamp = Time.time
        };

        EmitEvent("player_attack", data, highPriority: true);
        Debug.Log($"[OptimizedWS] ⚔️ Атака: {damage} урона");
    }

    /// <summary>
    /// Использовать скилл (ВЫСОКИЙ ПРИОРИТЕТ)
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
            targetZ = targetPosition.z,
            timestamp = Time.time
        };

        EmitEvent("player_skill", data, highPriority: true);
        Debug.Log($"[OptimizedWS] 🔮 Скилл {skillId}");
    }

    /// <summary>
    /// Обновить здоровье (СРЕДНИЙ ПРИОРИТЕТ)
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

        // Среднего приоритета - отправляем в батче
        EmitEvent("update_health", data, highPriority: false);
    }

    /// <summary>
    /// Запросить респавн (ВЫСОКИЙ ПРИОРИТЕТ)
    /// </summary>
    public void RequestRespawn()
    {
        if (!isConnected) return;

        EmitEvent("player_respawn", new { }, highPriority: true);
        Debug.Log("[OptimizedWS] 💀 Запрос респавна");
    }

    /// <summary>
    /// Начать прослушивание
    /// </summary>
    public void StartListening()
    {
        Debug.Log("[OptimizedWS] 📡 Listening started");
    }

    // ===== PING MONITORING =====

    /// <summary>
    /// Начать мониторинг пинга
    /// </summary>
    private void StartPingMonitoring()
    {
        InvokeRepeating(nameof(MeasurePing), 1f, 5f); // Каждые 5 секунд
    }

    /// <summary>
    /// Измерить пинг
    /// </summary>
    private void MeasurePing()
    {
        if (!isConnected) return;

        float sendTime = Time.time;

        socket.EmitAsync("ping", new { sendTime = sendTime });

        On("pong", (response) =>
        {
            float receiveTime = Time.time;
            averagePing = (receiveTime - sendTime) * 1000f; // В миллисекундах

            // Адаптируем частоту обновлений на основе пинга
            AdaptUpdateRate();
        });
    }

    /// <summary>
    /// Адаптировать частоту обновлений на основе пинга
    /// </summary>
    private void AdaptUpdateRate()
    {
        if (averagePing < 50f)
        {
            // Отличный пинг - увеличиваем частоту
            positionUpdateRate = 30f;
        }
        else if (averagePing < 100f)
        {
            // Хороший пинг - стандартная частота
            positionUpdateRate = 20f;
        }
        else if (averagePing < 200f)
        {
            // Средний пинг - снижаем частоту
            positionUpdateRate = 15f;
        }
        else
        {
            // Плохой пинг - минимальная частота
            positionUpdateRate = 10f;
        }
    }

    // ===== STATISTICS =====

    /// <summary>
    /// Получить статистику сети
    /// </summary>
    public NetworkStats GetNetworkStats()
    {
        return new NetworkStats
        {
            packetsSent = packetsSent,
            bytesSaved = bytesSaved,
            averagePing = averagePing,
            updateRate = positionUpdateRate,
            pendingBatchSize = pendingUpdates.Count
        };
    }

    /// <summary>
    /// Вывести статистику в консоль
    /// </summary>
    public void LogStats()
    {
        var stats = GetNetworkStats();
        Debug.Log($"[OptimizedWS] 📊 Stats:\n" +
                  $"  Packets Sent: {stats.packetsSent}\n" +
                  $"  Bytes Saved: {stats.bytesSaved}\n" +
                  $"  Avg Ping: {stats.averagePing:F1}ms\n" +
                  $"  Update Rate: {stats.updateRate}Hz\n" +
                  $"  Pending Batch: {stats.pendingBatchSize}");
    }

    // Properties
    public bool IsConnected => isConnected;
    public string CurrentRoomId => currentRoomId;
    public string SessionId => sessionId;
    public float AveragePing => averagePing;
}

/// <summary>
/// Статистика сети
/// </summary>
[Serializable]
public struct NetworkStats
{
    public int packetsSent;
    public int bytesSaved;
    public float averagePing;
    public float updateRate;
    public int pendingBatchSize;
}
