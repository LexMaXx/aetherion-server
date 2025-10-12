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
            var dispatcher = UnityMainThreadDispatcher.Instance();
            if (dispatcher != null)
            {
                dispatcher.Enqueue(() =>
                {
                    DebugLog($"✅ Callback вызван в главном потоке, isConnected = {isConnected}");
                    onComplete?.Invoke(true);
                });
            }
            else
            {
                Debug.LogError("[SocketIO] ❌ UnityMainThreadDispatcher is null in OnConnected!");
                onComplete?.Invoke(false);
            }
        };

        // Событие отключения
        socket.OnDisconnected += (sender, e) =>
        {
            isConnected = false;
            Debug.LogError($"[SocketIO] ❌ Отключено от сервера! Причина: {e}");

            // Логируем дополнительную информацию
            var dispatcher = UnityMainThreadDispatcher.Instance();
            if (dispatcher != null)
            {
                dispatcher.Enqueue(() =>
                {
                    Debug.LogError($"[SocketIO] Отключение произошло в комнате: {currentRoomId}");
                    Debug.LogError($"[SocketIO] Username: {myUsername}");
                });
            }
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
                // Используем GetValue() без параметров, затем преобразуем ToString()
                string jsonData;

                if (response.Count > 0)
                {
                    // ПРАВИЛЬНЫЙ СПОСОБ: GetValue() возвращает JsonElement, используем GetRawText()
                    var firstArg = response.GetValue();

                    // Логируем тип для отладки
                    DebugLog($"🔍 Event '{eventName}' firstArg type: {firstArg.GetType().FullName}");

                    // Проверяем, является ли это JsonElement
                    if (firstArg.GetType().Name == "JsonElement")
                    {
                        // Получаем сырой JSON текст из JsonElement
                        var jsonElement = (System.Text.Json.JsonElement)firstArg;
                        jsonData = jsonElement.GetRawText();
                    }
                    else
                    {
                        // Fallback: сериализуем через Newtonsoft
                        jsonData = JsonConvert.SerializeObject(firstArg);
                    }
                }
                else
                {
                    jsonData = "{}";
                }

                DebugLog($"📨 Событие '{eventName}': {jsonData.Substring(0, Math.Min(100, jsonData.Length))}...");

                // Вызываем callback в главном потоке Unity
                var dispatcher = UnityMainThreadDispatcher.Instance();
                if (dispatcher != null)
                {
                    dispatcher.Enqueue(() =>
                    {
                        if (eventCallbacks.ContainsKey(eventName))
                        {
                            eventCallbacks[eventName]?.Invoke(jsonData);
                        }
                    });
                }
                else
                {
                    Debug.LogError($"[SocketIO] ❌ UnityMainThreadDispatcher is null! Event '{eventName}' cannot be processed.");
                }
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

        // ВАЖНО: SocketIOUnity v1.1.5 лучше работает с обычными строками!
        // Сервер Node.js сам распарсит JSON
        try
        {
            DebugLog($"📤 Попытка отправить: {eventName}");
            DebugLog($"   JSON: {jsonData}");

            // ПРАВИЛЬНО: Отправляем JSON как строку - сервер распарсит
            socket.Emit(eventName, jsonData);
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

        // ПРИМЕЧАНИЕ: room_players событие обрабатывается через NetworkSyncManager
        // который подписывается в ArenaScene.Start()
        // Вызываем onComplete сразу, не ждём room_players
        onComplete?.Invoke(true);
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
        string json = JsonConvert.SerializeObject(data);
        DebugLog($"🔄 Запрос списка игроков для комнаты {currentRoomId}");
        DebugLog($"   JSON: {json}");
        Emit("get_room_players", json);
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
