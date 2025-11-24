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
    private string mySocketId = ""; // КРИТИЧЕСКОЕ: Наш socketId для NetworkSyncManager

    // Event callbacks
    private Dictionary<string, Action<string>> eventCallbacks = new Dictionary<string, Action<string>>();

    // Pending callbacks for inventory operations
    private Action<string> pendingInventoryLoadCallback = null;
    private Action<bool> pendingInventorySyncCallback = null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ВАЖНО: Разрешаем работу в фоновом режиме для тестирования мультиплеера
        Application.runInBackground = true;
        Debug.Log("[SocketIO] ✅ Фоновый режим ВКЛЮЧЁН - окно не будет зависать при потере фокуса");

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
        Debug.LogError($"🔥🔥🔥 [SocketIO] КРИТИЧНО: Подключение к URL: {serverUrl}");
        Debug.LogError($"🔥 [SocketIO] Это должен быть: https://aetherion-server-gv5u.onrender.com");

        // Создаём Socket.IO клиент
        var uri = new Uri(serverUrl);
        Debug.LogError($"🔥 [SocketIO] URI после парсинга: {uri.ToString()}");
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
            mySocketId = socket.Id; // КРИТИЧЕСКОЕ: Сохраняем socketId для NetworkSyncManager!
            DebugLog($"✅ Подключено! Socket ID: {socket.Id}");
            Debug.LogError($"🔥🔥🔥 [SocketIO] ПОДКЛЮЧЁН! Socket ID: {socket.Id}");
            Debug.LogError($"🔥 [SocketIO] Server URL: {serverUrl}");
            Debug.LogError($"🔥 [SocketIO] ЭТОТ ЛОГ ДОЛЖЕН ПОЯВИТЬСЯ В RENDER DASHBOARD!");

            // ВАЖНО: Вызываем callback в главном потоке Unity!
            var dispatcher = UnityMainThreadDispatcher.Instance();
            if (dispatcher != null)
            {
                dispatcher.Enqueue(() =>
                {
                    DebugLog($"✅ Callback вызван в главном потоке, isConnected = {isConnected}");
                    DebugLog($"✅ mySocketId сохранен: {mySocketId}");
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
            // ДИАГНОСТИКА: Лог для minion_summoned и party events
            if (eventName == "minion_summoned" || eventName.StartsWith("party_"))
            {
                Debug.LogError($"[SocketIO] 🔥🔥🔥 EVENT '{eventName}' RECEIVED! response.Count={response.Count}");
            }

            try
            {
                string jsonData;

                if (response.Count > 0)
                {
                    var firstArg = response.GetValue();

                    if (firstArg.GetType().Name == "JsonElement")
                    {
                        var jsonElement = (System.Text.Json.JsonElement)firstArg;
                        jsonData = jsonElement.GetRawText();
                    }
                    else
                    {
                        jsonData = JsonConvert.SerializeObject(firstArg);
                    }
                }
                else
                {
                    jsonData = "{}";
                }

                var dispatcher = UnityMainThreadDispatcher.Instance();
                if (dispatcher != null)
                {
                    dispatcher.Enqueue(() =>
                    {
                        try
                        {
                            // ДИАГНОСТИКА: Лог перед вызовом callback
                            if (eventName == "minion_summoned" || eventName.StartsWith("party_"))
                            {
                                Debug.LogError($"[SocketIO] 🔥 Dispatching to main thread, eventCallbacks.ContainsKey('{eventName}')={eventCallbacks.ContainsKey(eventName)}");
                                Debug.LogError($"[SocketIO] 🔥 jsonData: {jsonData}");
                            }

                            if (eventCallbacks.ContainsKey(eventName))
                            {
                                eventCallbacks[eventName]?.Invoke(jsonData);
                            }
                            else if (eventName.StartsWith("party_"))
                            {
                                Debug.LogError($"[SocketIO] ❌ Event '{eventName}' received but NO CALLBACK registered!");
                            }
                        }
                        catch (Exception callbackEx)
                        {
                            Debug.LogError($"[SocketIO] ❌ Исключение в callback '{eventName}': {callbackEx.Message}\n{callbackEx.StackTrace}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                // КРИТИЧНО: Не вызываем Debug.LogError из фонового потока!
                // Отправляем в главный поток
                try
                {
                    var dispatcher = UnityMainThreadDispatcher.Instance();
                    dispatcher?.Enqueue(() =>
                    {
                        Debug.LogError($"[SocketIO] ❌ Ошибка парсинга события '{eventName}': {ex.Message}");
                    });
                }
                catch
                {
                    // Если даже это не работает - игнорируем
                }
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

        // Отправляем JSON как строку - сервер распарсит
        try
        {
            Debug.Log($"[SocketIO] 📤 Emit('{eventName}', {jsonData?.Length ?? 0} bytes)");
            socket.Emit(eventName, jsonData);
            Debug.Log($"[SocketIO] ✅ Событие '{eventName}' отправлено успешно!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SocketIO] ❌ Ошибка при отправке '{eventName}': {ex.Message}");
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

        // КРИТИЧНО MMO MODE: Всегда используем глобальную комнату!
        const string GLOBAL_ROOM_ID = "aetherion-global-world";
        if (roomId != GLOBAL_ROOM_ID)
        {
            Debug.LogError($"[SocketIO] 🌍 MMO MODE: Переопределяю roomId '{roomId}' → '{GLOBAL_ROOM_ID}'");
            roomId = GLOBAL_ROOM_ID;
        }

        currentRoomId = roomId;
        myUsername = PlayerPrefs.GetString("Username", "Player");

        // КРИТИЧЕСКИ ВАЖНО: Используем SelectedCharacterId (MongoDB ObjectId) вместо UserId (UUID)
        // SelectedCharacterId сохраняется в CharacterSelectionManager.SelectOrCreateCharacter() после логина
        myUserId = PlayerPrefs.GetString("SelectedCharacterId", "");

        // КРИТИЧНО: userId не должен быть пустым для MMO режима
        if (string.IsNullOrEmpty(myUserId))
        {
            Debug.LogError($"[SocketIO] ❌ SelectedCharacterId не найден в PlayerPrefs!");
            Debug.LogError($"[SocketIO] ❌ Игрок должен сначала выбрать персонажа через CharacterSelectionManager!");
            Debug.LogError($"[SocketIO] ❌ Генерируем временный UUID (НЕ будет работать с MongoDB!)");
            myUserId = System.Guid.NewGuid().ToString();
        }
        else
        {
            Debug.Log($"[SocketIO] ✅ SelectedCharacterId (MongoDB ObjectId) загружен из PlayerPrefs: {myUserId}");
        }

        Debug.LogError($"[SocketIO] 🔍🔍🔍 ДИАГНОСТИКА JoinRoom:");
        Debug.LogError($"  - roomId: {roomId}");
        Debug.LogError($"  - characterClass (параметр): '{characterClass}'");
        Debug.LogError($"  - username: {myUsername}");
        Debug.LogError($"  - userId: {myUserId}");

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
        Debug.Log($"[SocketIO] 🔍 JSON для join_room: {json}");
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
        Debug.LogError($"[SocketIO] 🔍🔍🔍 RequestRoomPlayers() ВЫЗВАН! isConnected={isConnected}, currentRoomId='{currentRoomId}'");

        if (!isConnected || string.IsNullOrEmpty(currentRoomId))
        {
            Debug.LogError($"[SocketIO] ❌❌❌ НЕ ПРОХОДИТ ПРОВЕРКА! isConnected={isConnected}, currentRoomId empty={string.IsNullOrEmpty(currentRoomId)}");
            Debug.LogWarning("[SocketIO] ⚠️ Не подключен к комнате!");
            return;
        }

        var data = new { roomId = currentRoomId };
        string json = JsonConvert.SerializeObject(data);
        Debug.LogError($"[SocketIO] 🔄🔄🔄 Запрос списка игроков для комнаты {currentRoomId}");
        Debug.LogError($"[SocketIO]    JSON: {json}");
        Debug.LogError($"[SocketIO] 📤📤📤 Отправляю событие 'get_room_players'");
        Emit("get_room_players", json);
        Debug.LogError($"[SocketIO] ✅✅✅ get_room_players отправлен!");
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
            socketId = socket.Id, // ВАЖНО: Добавляем socketId чтобы сервер знал кто отправил
            position = new { x = position.x, y = position.y, z = position.z },
            rotation = new { x = eulerRotation.x, y = eulerRotation.y, z = eulerRotation.z },
            velocity = new { x = velocity.x, y = velocity.y, z = velocity.z },
            isGrounded = isGrounded,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() // Timestamp для Dead Reckoning
        };

        string json = JsonConvert.SerializeObject(data);
        Emit("player_update", json);
    }

    /// <summary>
    /// Обновить анимацию игрока
    /// </summary>
    public void UpdateAnimation(string animationState, float speed = 1f)
    {
        if (!isConnected)
        {
            Debug.LogWarning($"[SocketIO] ⚠️ UpdateAnimation: Не подключен к серверу! animation={animationState}");
            return;
        }

        var data = new
        {
            socketId = socket.Id, // ВАЖНО: Добавляем socketId чтобы сервер знал кто отправил
            animation = animationState,
            speed = speed
        };

        string json = JsonConvert.SerializeObject(data);

        // ДИАГНОСТИКА: Логируем каждую отправку анимации
        Debug.Log($"[SocketIO] 📤 Отправка анимации: animation={animationState}, speed={speed}, socketId={socket.Id}");
        Debug.Log($"[SocketIO] 📤 JSON: {json}");

        // ВРЕМЕННОЕ РЕШЕНИЕ: Отправляем ОБОИМИ событиями для совместимости
        // Новые серверы слушают update_animation, старые могут слушать player_animation
        Emit("update_animation", json);
        Emit("player_animation", json); // Для обратной совместимости

        Debug.Log($"[SocketIO] ✅ Анимация отправлена на сервер");
    }

    /// <summary>
    /// Отправить анимацию миньона на сервер (для синхронизации Skeleton и других миньонов)
    /// </summary>
    public void SendMinionAnimation(string minionId, string animationState, float speed = 1f)
    {
        if (!isConnected)
        {
            return; // Тихо пропускаем если не подключены
        }

        var data = new
        {
            minionId = minionId,           // Уникальный ID миньона (ownerSocketId_minionType)
            ownerSocketId = socket.Id,     // Socket ID владельца
            animation = animationState,    // "Idle", "Walking", "Attack"
            speed = speed
        };

        string json = JsonConvert.SerializeObject(data);
        Emit("minion_animation", json);

        // Логируем только в режиме отладки
        if (debugMode)
        {
            Debug.Log($"[SocketIO] 💀 Отправка анимации миньона: id={minionId}, anim={animationState}");
        }
    }

    /// <summary>
    /// Отправить уничтожение миньона на сервер (для удаления из словаря других игроков)
    /// </summary>
    public void SendMinionDestroyed(string minionId)
    {
        if (!isConnected)
        {
            return; // Тихо пропускаем если не подключены
        }

        var data = new
        {
            minionId = minionId,
            ownerSocketId = socket.Id
        };

        string json = JsonConvert.SerializeObject(data);
        Emit("minion_destroyed", json);

        if (debugMode)
        {
            Debug.Log($"[SocketIO] 💀 Отправка minion_destroyed: {minionId}");
        }
    }

    /// <summary>
    /// Отправить атаку на сервер (для мультиплеера)
    /// ИЗМЕНЕНО: Теперь отправляет SPECIAL stats для расчёта урона на сервере
    /// Сервер сам рассчитает урон (damage = baseDamage + (strength/intelligence * 5)) и криты (luck)
    /// </summary>
    public void SendPlayerAttack(string targetType, string targetId, int strength, int intelligence, int luck, float baseDamage, string attackType, Vector3 position, Vector3 direction, Vector3 targetPosition)
    {
        if (!isConnected)
        {
            DebugLog("⚠️ SendPlayerAttack: Не подключен к серверу");
            return;
        }

        var data = new
        {
            targetType = targetType,  // "player" or "enemy"
            targetId = targetId,      // socketId (для игрока) или enemyId (для врага)
            attackType = attackType,  // "melee", "ranged", "magic"
            // SPECIAL stats для расчёта урона на сервере
            strength = strength,      // Для физического урона (melee)
            intelligence = intelligence, // Для магического урона (ranged)
            luck = luck,             // Для критических ударов
            baseDamage = baseDamage, // Базовый урон оружия (БЕЗ бонусов от статов)
            position = new { x = position.x, y = position.y, z = position.z },
            direction = new { x = direction.x, y = direction.y, z = direction.z },
            targetPosition = new { x = targetPosition.x, y = targetPosition.y, z = targetPosition.z }
        };

        string json = JsonConvert.SerializeObject(data);
        Debug.Log($"[SocketIO] ⚔️ Отправка атаки на сервер: {attackType} на {targetType} (ID: {targetId})");
        Debug.Log($"[SocketIO] 📊 SPECIAL stats: STR={strength}, INT={intelligence}, LUCK={luck}, Base Damage={baseDamage}");
        Debug.Log($"[SocketIO] ⚔️ JSON атаки: {json}");
        Debug.Log($"[SocketIO] 🎲 Сервер рассчитает финальный урон и крит на основе SPECIAL статов");
        Emit("player_attack", json);
        Debug.Log($"[SocketIO] ✅ player_attack отправлен");
    }

    /// <summary>
    /// Отправить использование скилла на сервер (для мультиплеера)
    /// ОБНОВЛЕНО: Добавлен параметр skillType для корректной обработки трансформации
    /// </summary>
    public void SendPlayerSkill(int skillId, string targetSocketId, Vector3 targetPosition, string skillType = "")
    {
        if (!isConnected)
        {
            DebugLog("⚠️ SendPlayerSkill: Не подключен к серверу");
            return;
        }

        var data = new
        {
            skillId = skillId,
            targetSocketId = targetSocketId,
            targetPosition = new { x = targetPosition.x, y = targetPosition.y, z = targetPosition.z },
            skillType = skillType // ВАЖНО: "Transformation" для трансформации медведя
        };

        string json = JsonConvert.SerializeObject(data);
        DebugLog($"⚡ Отправка скилла: ID={skillId}, тип={skillType}, target={targetSocketId}");
        Emit("player_skill", json);
    }

    /// <summary>
    /// Отправить использование скилла с анимацией на сервер (улучшенная версия)
    /// НОВОЕ: Передает информацию об анимации для синхронизации
    /// </summary>
    public void SendPlayerSkillWithAnimation(int skillId, string targetSocketId, Vector3 targetPosition, string skillType, string animationTrigger, float animationSpeed, float castTime)
    {
        if (!isConnected)
        {
            DebugLog("⚠️ SendPlayerSkillWithAnimation: Не подключен к серверу");
            return;
        }

        var data = new
        {
            skillId = skillId,
            targetSocketId = targetSocketId,
            targetPosition = new { x = targetPosition.x, y = targetPosition.y, z = targetPosition.z },
            skillType = skillType,
            animationTrigger = animationTrigger,
            animationSpeed = animationSpeed,
            castTime = castTime
        };

        string json = JsonConvert.SerializeObject(data);
        DebugLog($"⚡ Отправка скилла с анимацией: ID={skillId}, тип={skillType}, анимация={animationTrigger}@{animationSpeed}x, castTime={castTime}с");
        Emit("player_skill", json); // Используем тот же событие, сервер будет обрабатывать доп. поля
    }

    /// <summary>
    /// Отправить создание снаряда на сервер (НОВОЕ)
    /// </summary>
    public void SendProjectileSpawned(int skillId, Vector3 spawnPosition, Vector3 direction, string targetSocketId)
    {
        if (!isConnected)
        {
            DebugLog("⚠️ SendProjectileSpawned: Не подключен к серверу");
            return;
        }

        var data = new
        {
            skillId = skillId,
            spawnPosition = new { x = spawnPosition.x, y = spawnPosition.y, z = spawnPosition.z },
            direction = new { x = direction.x, y = direction.y, z = direction.z },
            targetSocketId = targetSocketId
        };

        string json = JsonConvert.SerializeObject(data);
        DebugLog($"🚀 Отправка снаряда: skillId={skillId}, pos=({spawnPosition.x:F1}, {spawnPosition.y:F1}, {spawnPosition.z:F1}), dir=({direction.x:F2}, {direction.y:F2}, {direction.z:F2})");
        Emit("projectile_spawned", json);
    }

    /// <summary>
    /// Отправить создание визуального эффекта на сервер (НОВОЕ - для синхронизации эффектов попадания)
    /// </summary>
    public void SendVisualEffectSpawned(string effectType, string effectPrefabName, Vector3 position, Quaternion rotation, string targetSocketId, float duration)
    {
        if (!isConnected)
        {
            DebugLog("⚠️ SendVisualEffectSpawned: Не подключен к серверу");
            return;
        }

        var data = new
        {
            effectType = effectType,
            effectPrefabName = effectPrefabName,
            position = new { x = position.x, y = position.y, z = position.z },
            rotation = new { x = rotation.x, y = rotation.y, z = rotation.z, w = rotation.w },
            targetSocketId = targetSocketId,
            duration = duration
        };

        string json = JsonConvert.SerializeObject(data);
        DebugLog($"✨ Отправка визуального эффекта: {effectType} ({effectPrefabName}) at ({position.x:F1}, {position.y:F1}, {position.z:F1})");
        Emit("visual_effect_spawned", json);
    }

    /// <summary>
    /// Отправить окончание трансформации на сервер
    /// </summary>
    public void SendTransformationEnd()
    {
        if (!isConnected)
        {
            DebugLog("⚠️ SendTransformationEnd: Не подключен к серверу");
            return;
        }

        DebugLog("🔄 Отправка окончания трансформации");
        Emit("player_transformation_end", "{}");
    }

    /// <summary>
    /// Отправить визуальный эффект на сервер (НОВОЕ - для синхронизации визуальных эффектов)
    /// Используется для: взрывы, ауры, горение, яд, баффы и т.д.
    /// </summary>
    public void SendVisualEffect(string effectType, string effectPrefabName, Vector3 position, Quaternion rotation, string targetSocketId = "", float duration = 0f, Transform parentTransform = null)
    {
        if (!isConnected)
        {
            DebugLog("⚠️ SendVisualEffect: Не подключен к серверу");
            return;
        }

        var data = new
        {
            effectType = effectType, // "explosion", "aura", "burn", "poison", "buff", "debuff" и т.д.
            effectPrefabName = effectPrefabName, // Название prefab эффекта (для поиска в Resources)
            position = new { x = position.x, y = position.y, z = position.z },
            rotation = new { x = rotation.eulerAngles.x, y = rotation.eulerAngles.y, z = rotation.eulerAngles.z },
            targetSocketId = targetSocketId, // Если эффект привязан к игроку (пустая строка = world space)
            duration = duration // Длительность эффекта (0 = мгновенный/particle system автоматически)
        };

        string json = JsonConvert.SerializeObject(data);
        Debug.Log($"[SocketIO] 🔍 ОТЛАДКА SendVisualEffect:");
        Debug.Log($"[SocketIO] 🔍 effectType: '{effectType}'");
        Debug.Log($"[SocketIO] 🔍 effectPrefabName: '{effectPrefabName}'");
        Debug.Log($"[SocketIO] 🔍 targetSocketId: '{targetSocketId}'");
        Debug.Log($"[SocketIO] 🔍 JSON для отправки: {json}");
        DebugLog($"✨ Отправка визуального эффекта: type={effectType}, prefab={effectPrefabName}, pos=({position.x:F1}, {position.y:F1}, {position.z:F1})");
        Emit("visual_effect_spawned", json);
    }

    /// <summary>
    /// Отправить получение урона на сервер (для мультиплеера)
    /// </summary>
    public void SendPlayerDamaged(float damage, float currentHealth, float maxHealth, string attackerId)
    {
        if (!isConnected)
        {
            DebugLog("⚠️ SendPlayerDamaged: Не подключен к серверу");
            return;
        }

        var data = new
        {
            damage = damage,
            currentHealth = currentHealth,
            maxHealth = maxHealth,
            attackerId = attackerId
        };

        string json = JsonConvert.SerializeObject(data);
        DebugLog($"💔 Отправка получения урона: -{damage:F1} HP, осталось {currentHealth:F1}/{maxHealth:F1}");
        Emit("player_damaged", json);
    }

    /// <summary>
    /// Отправить информацию о респавне на сервер
    /// </summary>
    public void SendPlayerRespawn(Vector3 position)
    {
        if (!isConnected)
        {
            DebugLog("⚠️ SendPlayerRespawn: Не подключен к серверу");
            return;
        }

        var data = new
        {
            position = new { x = position.x, y = position.y, z = position.z }
        };

        string json = JsonConvert.SerializeObject(data);
        DebugLog($"♻️ Отправка респавна на позиции ({position.x:F1}, {position.y:F1}, {position.z:F1})");
        Emit("player_respawn", json);
    }

    /// <summary>
    /// Отправить лечение на сервер (для мультиплеера)
    /// Используется для синхронизации скиллов лечения (Battle Heal, Lay on Hands и т.д.)
    /// </summary>
    public void SendPlayerHealed(string targetSocketId, float healAmount, float currentHealth, float maxHealth, string healerSocketId = "")
    {
        if (!isConnected)
        {
            DebugLog("⚠️ SendPlayerHealed: Не подключен к серверу");
            return;
        }

        var data = new
        {
            targetSocketId = targetSocketId,
            healAmount = healAmount,
            currentHealth = currentHealth,
            maxHealth = maxHealth,
            healerSocketId = string.IsNullOrEmpty(healerSocketId) ? mySocketId : healerSocketId
        };

        string json = JsonConvert.SerializeObject(data);
        DebugLog($"💚 Отправка лечения: +{healAmount:F1} HP → {targetSocketId}, HP: {currentHealth:F1}/{maxHealth:F1}");
        Emit("player_healed", json);
    }

    // ══════════════════════════════════════════════════════════════
    // НОВЫЕ МЕТОДЫ ДЛЯ УЛУЧШЕННОЙ СИНХРОНИЗАЦИИ СКИЛЛОВ
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Отправить урон от скилла на сервер (НОВОЕ)
    /// Сервер валидирует урон и применяет к цели
    /// </summary>
    public void SendSkillDamage(int skillId, string targetSocketId, float damage, List<SkillEffect> effects)
    {
        if (!isConnected)
        {
            DebugLog("⚠️ SendSkillDamage: Не подключен к серверу");
            return;
        }

        // Сериализуем эффекты
        var effectsData = new List<object>();
        if (effects != null)
        {
            foreach (var effect in effects)
            {
                effectsData.Add(new
                {
                    type = effect.effectType.ToString(),
                    duration = effect.duration,
                    power = effect.power,
                    damageOrHealPerTick = effect.damageOrHealPerTick,
                    tickInterval = effect.tickInterval,
                    canStack = effect.canStack,
                    maxStacks = effect.maxStacks,
                    syncWithServer = effect.syncWithServer
                });
            }
        }

        var data = new
        {
            skillId = skillId,
            targetSocketId = targetSocketId,
            damage = damage,
            effects = effectsData
        };

        string json = JsonConvert.SerializeObject(data);
        DebugLog($"💥 Отправка урона скилла: ID={skillId}, урон={damage:F1}, цель={targetSocketId}, эффектов={effectsData.Count}");
        Emit("skill_damage", json);
    }

    /// <summary>
    /// Отправить применение эффекта (баффа/дебаффа) на сервер (НОВОЕ)
    /// </summary>
    public void SendEffectApplied(SkillEffect effect, string targetSocketId = null)
    {
        if (!isConnected)
        {
            DebugLog("⚠️ SendEffectApplied: Не подключен к серверу");
            return;
        }

        // Отправляем только если эффект должен синхронизироваться
        if (!effect.syncWithServer)
        {
            DebugLog($"⏭️ Эффект {effect.effectType} не требует синхронизации");
            return;
        }

        var data = new
        {
            targetSocketId = targetSocketId ?? "", // Пустая строка = на себя
            effectType = effect.effectType.ToString(),
            duration = effect.duration,
            power = effect.power,
            damageOrHealPerTick = effect.damageOrHealPerTick,
            tickInterval = effect.tickInterval
        };

        string json = JsonConvert.SerializeObject(data);
        DebugLog($"✨ Отправка эффекта: {effect.effectType}, цель={targetSocketId ?? "self"}, duration={effect.duration}с");
        Emit("effect_applied", json);
    }

    /// <summary>
    /// Отправить применение эффекта на сервер (EffectConfig)
    /// NEW: Для EffectManager с EffectConfig
    /// </summary>
    public void SendEffectApplied(EffectConfig effect, string targetSocketId = null)
    {
        if (!isConnected)
        {
            DebugLog("⚠️ SendEffectApplied: Не подключен к серверу");
            return;
        }

        // Отправляем только если эффект должен синхронизироваться
        if (!effect.syncWithServer)
        {
            DebugLog($"⏭️ Эффект {effect.effectType} не требует синхронизации (syncWithServer=false)");
            return;
        }

        // Получить имя prefab'а частиц (если есть)
        string particlePrefabName = "";
        if (effect.particleEffectPrefab != null)
        {
            particlePrefabName = effect.particleEffectPrefab.name;
        }

        var data = new
        {
            targetSocketId = targetSocketId ?? "", // Пустая строка = на себя
            effectType = effect.effectType.ToString(),
            duration = effect.duration,
            power = effect.power,
            tickInterval = effect.tickInterval,
            particleEffectPrefabName = particlePrefabName
        };

        string json = JsonConvert.SerializeObject(data);
        DebugLog($"✨ Отправка эффекта (EffectConfig): {effect.effectType}, цель={targetSocketId ?? "self"}, duration={effect.duration}с, power={effect.power}, particles={particlePrefabName}");
        Emit("effect_applied", json);
    }

    /// <summary>
    /// Отправить начало игры на сервер (FALLBACK countdown завершился)
    /// Сервер должен разослать game_start всем игрокам в комнате
    /// </summary>
    public void SendGameStart()
    {
        if (!isConnected || string.IsNullOrEmpty(currentRoomId))
        {
            DebugLog("⚠️ SendGameStart: Не подключен к комнате");
            return;
        }

        var data = new
        {
            roomId = currentRoomId
        };

        string json = JsonConvert.SerializeObject(data);
        DebugLog($"🚀 Отправка start_game для комнаты {currentRoomId}");
        Emit("start_game", json);
    }

    // ══════════════════════════════════════════════════════════════
    // INVENTORY SYNC METHODS
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Синхронизировать инвентарь с сервером (сохранить в MongoDB)
    /// </summary>
    public void SyncInventory(string characterClass, string inventoryJson, Action<bool> onComplete = null)
    {
        if (!isConnected)
        {
            Debug.LogWarning("[SocketIO] ⚠️ SyncInventory: Не подключен к серверу!");
            onComplete?.Invoke(false);
            return;
        }

        var data = new
        {
            characterClass = characterClass,
            inventoryData = inventoryJson
        };

        string json = JsonConvert.SerializeObject(data);
        Debug.Log($"[SocketIO] 📦 Синхронизация инвентаря: {characterClass}");

        // Сохраняем callback для использования при получении ответа
        pendingInventorySyncCallback = onComplete;

        // Подписываемся на ответ от сервера ОДИН раз (если ещё не подписаны)
        if (!eventCallbacks.ContainsKey("inventory_synced"))
        {
            On("inventory_synced", OnInventorySynced);
        }

        Emit("inventory_sync", json);
    }

    /// <summary>
    /// Обработчик события inventory_synced
    /// </summary>
    private void OnInventorySynced(string response)
    {
        try
        {
            var syncResponse = JsonConvert.DeserializeObject<InventorySyncResponse>(response);
            Debug.Log($"[SocketIO] ✅ Инвентарь синхронизирован: success={syncResponse.success}");
            pendingInventorySyncCallback?.Invoke(syncResponse.success);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SocketIO] ❌ Ошибка парсинга inventory_synced: {ex.Message}");
            pendingInventorySyncCallback?.Invoke(false);
        }
        finally
        {
            pendingInventorySyncCallback = null;
        }
    }

    /// <summary>
    /// Загрузить инвентарь с сервера (из MongoDB)
    /// </summary>
    public void LoadInventory(string characterClass, Action<string> onComplete = null)
    {
        if (!isConnected)
        {
            Debug.LogWarning("[SocketIO] ⚠️ LoadInventory: Не подключен к серверу!");
            onComplete?.Invoke(null);
            return;
        }

        var data = new
        {
            characterClass = characterClass
        };

        string json = JsonConvert.SerializeObject(data);
        Debug.Log($"[SocketIO] 📥 Запрос инвентаря для {characterClass}");

        // Сохраняем callback для использования при получении ответа
        pendingInventoryLoadCallback = onComplete;

        // Подписываемся на ответ от сервера ОДИН раз (если ещё не подписаны)
        if (!eventCallbacks.ContainsKey("inventory_loaded"))
        {
            On("inventory_loaded", OnInventoryLoaded);
        }

        Emit("load_inventory", json);
    }

    /// <summary>
    /// Обработчик события inventory_loaded
    /// </summary>
    private void OnInventoryLoaded(string response)
    {
        try
        {
            var loadResponse = JsonConvert.DeserializeObject<InventoryLoadResponse>(response);

            if (loadResponse.success)
            {
                Debug.Log($"[SocketIO] ✅ Инвентарь загружен: {loadResponse.inventoryJson?.Length ?? 0} символов");
                pendingInventoryLoadCallback?.Invoke(loadResponse.inventoryJson);
            }
            else
            {
                Debug.LogError($"[SocketIO] ❌ Ошибка загрузки инвентаря: {loadResponse.error}");
                pendingInventoryLoadCallback?.Invoke(null);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SocketIO] ❌ Ошибка парсинга inventory_loaded: {ex.Message}");
            pendingInventoryLoadCallback?.Invoke(null);
        }
        finally
        {
            pendingInventoryLoadCallback = null;
        }
    }

    public bool IsConnected => isConnected;
    public string CurrentRoomId => currentRoomId;
    public string MyUsername => myUsername;
    public string SocketId => mySocketId; // КРИТИЧЕСКОЕ: Используем сохраненный socketId

    /// <summary>
    /// Получить socketId (для NetworkSyncManager)
    /// </summary>
    public string GetSocketId() => mySocketId;

    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[SocketIO] {message}");
        }
    }

    // ════════════════════════════════════════════════════════════
    // НОВАЯ СИСТЕМА СКИЛЛОВ (SkillConfig)
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Отправить использование скилла на сервер (новая система)
    /// </summary>
    public void SendSkillCast(int skillId, string targetSocketId, Vector3 targetPosition)
    {
        var data = new
        {
            skillId = skillId,
            targetSocketId = targetSocketId,
            targetPosition = new { x = targetPosition.x, y = targetPosition.y, z = targetPosition.z },
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        Emit("player_skill_cast", JsonConvert.SerializeObject(data));
        DebugLog($"📡 SendSkillCast: skillId={skillId}, target={targetSocketId}, pos={targetPosition}");
    }

    /// <summary>
    /// Отправить применение эффекта на сервер
    /// </summary>
    public void SendEffectApplied(int skillId, int effectIndex, string targetSocketId, float duration, EffectType effectType)
    {
        var data = new
        {
            skillId = skillId,
            effectIndex = effectIndex,
            targetSocketId = targetSocketId,
            duration = duration,
            effectType = effectType.ToString()
        };

        Emit("effect_applied", JsonConvert.SerializeObject(data));
        DebugLog($"📡 SendEffectApplied: effect={effectType}, target={targetSocketId}, duration={duration}");
    }

    /// <summary>
    /// Отправить снятие эффекта на сервер
    /// </summary>
    public void SendEffectRemoved(int effectId, string targetSocketId, EffectType effectType)
    {
        var data = new
        {
            effectId = effectId,
            targetSocketId = targetSocketId,
            effectType = effectType.ToString()
        };

        Emit("effect_removed", JsonConvert.SerializeObject(data));
        DebugLog($"📡 SendEffectRemoved: effect={effectType}, target={targetSocketId}");
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

    [Serializable]
    public class InventorySyncResponse
    {
        public bool success;
        public string error;
        public long timestamp;
    }

    [Serializable]
    public class InventoryLoadResponse
    {
        public bool success;
        public string inventoryJson;
        public string error;
        public long timestamp;
    }

    // ═══════════════════════════════════════════
    // CUSTOM EVENT EMITTER FOR MMO INVENTORY
    // ═══════════════════════════════════════════

    /// <summary>
    /// Отправить кастомное событие на сервер с callback
    /// Используется для MMO Inventory системы
    /// </summary>
    public void EmitCustomEvent(string eventName, string jsonData, Action<string> callback)
    {
        if (!isConnected || socket == null)
        {
            Debug.LogWarning($"[SocketIO] ⚠️ Не подключен к серверу. Событие {eventName} не отправлено.");
            callback?.Invoke("{\"success\":false,\"message\":\"Not connected to server\"}");
            return;
        }

        DebugLog($"📤 Отправка кастомного события: {eventName}");
        DebugLog($"📦 Данные: {jsonData}");

        // Отправляем событие
        socket.Emit(eventName, jsonData);
        DebugLog($"✅ Событие {eventName} отправлено на сервер");

        // Подписываемся на ответ
        if (callback != null)
        {
            string responseEvent = "mmo_inventory_response"; // Все MMO события отвечают на один эндпоинт
            DebugLog($"👂 Подписываемся на ответ: {responseEvent}");

            // Создаём флаг для одноразового выполнения
            bool hasResponded = false;

            // Создаём обработчик ответа
            Action<SocketIOResponse> responseHandler = (response) =>
            {
                DebugLog($"🔔 Получен ЛЮБОЙ ответ от сервера на событие {responseEvent}");

                // Защита от множественных вызовов
                if (hasResponded)
                {
                    DebugLog($"⚠️ Ответ уже обработан, пропускаем");
                    return;
                }
                hasResponded = true;

                try
                {
                    // Пробуем получить как string
                    string responseData = null;
                    try
                    {
                        responseData = response.GetValue<string>();
                        DebugLog($"📥 Получен ответ (string) на {eventName}: {responseData}");
                    }
                    catch
                    {
                        // Если не string, пробуем как JSON объект
                        try
                        {
                            var jsonObj = response.GetValue();
                            responseData = jsonObj.ToString();
                            DebugLog($"📥 Получен ответ (object) на {eventName}: {responseData}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[SocketIO] ❌ Не удалось получить данные ответа: {ex.Message}");
                            responseData = "{\"success\":false,\"message\":\"Invalid response format\"}";
                        }
                    }

                    // Вызываем callback в главном потоке Unity
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        callback(responseData);
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SocketIO] ❌ Ошибка обработки ответа {eventName}: {e.Message}");
                    Debug.LogError($"[SocketIO] Stack trace: {e.StackTrace}");

                    // Вызываем callback с ошибкой в главном потоке
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        callback("{\"success\":false,\"message\":\"Parse error\"}");
                    });
                }
            };

            // Подписываемся на ответ
            socket.On(responseEvent, responseHandler);

            // Примечание: socket.Off() в SocketIOUnity принимает только 1 аргумент (имя события)
            // Поэтому используем флаг hasResponded для защиты от повторных вызовов
        }
    }

    /// <summary>
    /// Отправить кастомное событие без callback
    /// </summary>
    public void EmitCustomEvent(string eventName, string jsonData)
    {
        EmitCustomEvent(eventName, jsonData, null);
    }
}
