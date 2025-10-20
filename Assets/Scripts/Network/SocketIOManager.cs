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
                        if (eventCallbacks.ContainsKey(eventName))
                        {
                            try
                            {
                                eventCallbacks[eventName]?.Invoke(jsonData);
                            }
                            catch (Exception callbackEx)
                            {
                                Debug.LogError($"[SocketIO] ❌ Исключение в callback '{eventName}': {callbackEx.Message}");
                            }
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
            socket.Emit(eventName, jsonData);
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

        currentRoomId = roomId;
        myUsername = PlayerPrefs.GetString("Username", "Player");
        myUserId = PlayerPrefs.GetString("UserId", "");

        Debug.Log($"[SocketIO] 🔍 ДИАГНОСТИКА JoinRoom:");
        Debug.Log($"  - roomId: {roomId}");
        Debug.Log($"  - characterClass (параметр): '{characterClass}'");
        Debug.Log($"  - username: {myUsername}");
        Debug.Log($"  - userId: {myUserId}");

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

        Emit("player_animation", json);

        Debug.Log($"[SocketIO] ✅ Анимация отправлена на сервер");
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

    public bool IsConnected => isConnected;
    public string CurrentRoomId => currentRoomId;
    public string MyUsername => myUsername;
    public string SocketId => socket?.Id ?? "";

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
