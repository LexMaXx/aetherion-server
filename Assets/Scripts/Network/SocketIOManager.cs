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
        Debug.LogWarning("[SocketIO] ⚠️ OnDestroy вызван! SocketIOManager уничтожается!");

        if (socket != null)
        {
            Debug.LogWarning("[SocketIO] Отключаем socket перед уничтожением...");
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
            EIO = SocketIOClient.EngineIO.V4, // Engine.IO version 4
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

            // ВАЖНО: Вызываем callback в главном потоке Unity!
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                DebugLog($"✅ Callback вызван в главном потоке, isConnected = {isConnected}");
                onComplete?.Invoke(true);
            });
        };

        // Событие отключения
        socket.OnDisconnected += (sender, e) =>
        {
            isConnected = false;
            Debug.LogError($"[SocketIO] ❌ Отключено от сервера! Причина: {e}");

            // Логируем дополнительную информацию
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                Debug.LogError($"[SocketIO] Отключение произошло в комнате: {currentRoomId}");
                Debug.LogError($"[SocketIO] Username: {myUsername}");
            });
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
            try
            {
                // ВАЖНО: SocketIOResponse содержит массив параметров
                // Берем первый параметр (индекс 0) и сериализуем его
                string jsonData;

                if (response.Count > 0)
                {
                    // GetValue(0) получает первый аргумент события
                    var firstArg = response.GetValue(0);
                    jsonData = JsonConvert.SerializeObject(firstArg);
                }
                else
                {
                    jsonData = "{}";
                }

                DebugLog($"📨 Событие '{eventName}': {jsonData.Substring(0, Math.Min(100, jsonData.Length))}...");

                // Вызываем callback в главном потоке Unity
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (eventCallbacks.ContainsKey(eventName))
                    {
                        eventCallbacks[eventName]?.Invoke(jsonData);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SocketIO] ❌ Ошибка обработки события '{eventName}': {ex.Message}");
                Debug.LogError($"   Stack: {ex.StackTrace}");
            }
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

        // ВАЖНО: SocketIOUnity требует объект, а не строку!
        // Парсим JSON в Dictionary<string, object> для совместимости с Socket.IO
        try
        {
            DebugLog($"📤 Попытка отправить: {eventName}");
            DebugLog($"   JSON: {jsonData}");

            // Используем Dictionary<string, object> вместо JObject
            var dataObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
            DebugLog($"   Десериализовано: {dataObject?.GetType().Name ?? "null"}");
            if (dataObject != null)
            {
                DebugLog($"   Ключи: {string.Join(", ", dataObject.Keys)}");
            }

            socket.Emit(eventName, dataObject);
            DebugLog($"✅ Успешно отправлено: {eventName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SocketIO] ❌ Ошибка при отправке '{eventName}': {ex.Message}");
            Debug.LogError($"   Stack: {ex.StackTrace}");
        }
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
                DebugLog($"🔍 RAW room_players JSON (first 500 chars): {data.Substring(0, Math.Min(500, data.Length))}");

                // Попробуем распарсить и показать структуру
                try
                {
                    var parsed = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
                    if (parsed != null)
                    {
                        DebugLog($"🔍 room_players keys: {string.Join(", ", parsed.Keys)}");
                        foreach (var key in parsed.Keys)
                        {
                            var value = parsed[key];
                            if (value != null)
                            {
                                DebugLog($"   {key}: {value.GetType().Name}");
                                if (key == "players" && value is Newtonsoft.Json.Linq.JArray)
                                {
                                    var players = value as Newtonsoft.Json.Linq.JArray;
                                    DebugLog($"   players count: {players?.Count ?? 0}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SocketIO] ❌ Ошибка парсинга room_players: {ex.Message}");
                }

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
        DebugLog($"🚪 Вход в комнату: {roomId} как {characterClass}");
        DebugLog($"   isConnected: {isConnected}");
        DebugLog($"   socket is null: {socket == null}");
        DebugLog($"   JSON для отправки: {json}");
        DebugLog($"📞 Вызываем Emit('join_room')...");

        Emit("join_room", json);

        DebugLog($"✅ Emit('join_room') вызван (метод завершился)");

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

        // ВАЖНО: Преобразуем Quaternion в Euler angles (градусы) для сервера
        Vector3 eulerRotation = rotation.eulerAngles;

        var data = new
        {
            position = new { x = position.x, y = position.y, z = position.z },
            rotation = new { x = eulerRotation.x, y = eulerRotation.y, z = eulerRotation.z },
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
