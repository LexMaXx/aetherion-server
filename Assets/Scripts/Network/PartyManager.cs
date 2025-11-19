using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Менеджер группы игроков (Party System)
/// Максимум 5 игроков в группе
/// Функции:
/// - Приглашение игроков в группу
/// - Принятие/отклонение приглашений
/// - Синхронизация HP/MP всех членов группы
/// - Выход из группы
/// - Проверка союзников (для запрета атаки)
/// </summary>
public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance { get; private set; }

    [Header("Party Settings")]
    [SerializeField] private int maxPartyMembers = 5;

    [Header("Current Party")]
    private string currentPartyId = "";
    private bool isInParty = false;
    private bool isPartyLeader = false;
    private List<PartyMember> partyMembers = new List<PartyMember>();
    private PartyInvite lastReceivedInvite = null; // Кэш последнего приглашения для восстановления данных

    // Events
    public event Action<string> OnPartyCreated; // partyId
    public event Action<string> OnPartyJoined; // partyId - when local player joins party
    public event Action<PartyMember> OnMemberJoined; // new member
    public event Action<string> OnMemberLeft; // member socketId
    public event Action OnPartyLeft; // local player left party
    public event Action<string, PartyMemberStats> OnMemberStatsUpdated; // socketId, stats
    public event Action<PartyInvite> OnInviteReceived; // invite data
    public event Action<string> OnInviteDeclined; // declined username
    public event Action<string> OnPartyError; // error message

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

    void Start()
    {
        SubscribeToSocketEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromSocketEvents();
    }

    /// <summary>
    /// Подписаться на WebSocket события
    /// </summary>
    private void SubscribeToSocketEvents()
    {
        if (SocketIOManager.Instance != null)
        {
            Debug.Log("[PartyManager] 🔌 Подписываемся на Party System события...");
            SocketIOManager.Instance.On("party_invite_received", OnPartyInviteReceived);
            SocketIOManager.Instance.On("party_invite_sent", OnPartyInviteSent);
            SocketIOManager.Instance.On("party_member_joined", OnPartyMemberJoined);
            SocketIOManager.Instance.On("party_joined", OnPartyJoinedReceived);
            SocketIOManager.Instance.On("party_invite_declined", OnPartyInviteDeclined);
            SocketIOManager.Instance.On("party_member_left", OnPartyMemberLeft);
            SocketIOManager.Instance.On("party_left", OnPartyLeftConfirmed);
            SocketIOManager.Instance.On("party_member_stats_updated", OnPartyMemberStatsUpdated);
            SocketIOManager.Instance.On("party_synced", OnPartySynced);
            SocketIOManager.Instance.On("party_error", OnPartyErrorReceived);
            Debug.Log("[PartyManager] ✅ Подписка на 10 Party System событий завершена");
        }
        else
        {
            Debug.LogError("[PartyManager] ❌ SocketIOManager.Instance is null! Не могу подписаться на события.");
        }
    }

    /// <summary>
    /// Отписаться от WebSocket событий
    /// </summary>
    private void UnsubscribeFromSocketEvents()
    {
        // NOTE: SocketIOManager не имеет метода Off(), события очищаются автоматически
        // при уничтожении объекта или отключении сокета
    }

    // ═══════════════════════════════════════════
    // PUBLIC API
    // ═══════════════════════════════════════════

    /// <summary>
    /// Пригласить игрока в группу (по targetSocketId)
    /// </summary>
    public void InvitePlayer(string targetSocketId)
    {
        if (string.IsNullOrEmpty(targetSocketId))
        {
            Debug.LogError("[PartyManager] ❌ targetSocketId is null or empty!");
            return;
        }

        // Если не в группе - создаём новую
        if (!isInParty)
        {
            CreateParty();
        }

        // Проверяем лимит игроков
        if (partyMembers.Count >= maxPartyMembers)
        {
            Debug.LogWarning("[PartyManager] ❌ Группа полная (макс. 5 игроков)!");
            OnPartyError?.Invoke("Группа полная (максимум 5 игроков)");
            return;
        }

        string mySocketId = SocketIOManager.Instance != null ? SocketIOManager.Instance.SocketId : "";
        string myUsername = PlayerPrefs.GetString("Username", "Player");
        string myClass = PlayerPrefs.GetString("SelectedCharacterClass", "Warrior");
        int myLevel = PlayerPrefs.GetInt("PlayerLevel", 1);

        Debug.Log($"[PartyManager] 📨 Приглашаем игрока {targetSocketId} в группу {currentPartyId} (инвайтер {myUsername}/{mySocketId})");

        PartyInviteData data = new PartyInviteData
        {
            targetSocketId = targetSocketId,
            partyId = currentPartyId,
            inviterSocketId = mySocketId,
            inviterUsername = myUsername,
            inviterClass = myClass,
            inviterLevel = myLevel
        };

        string json = JsonUtility.ToJson(data);
        Debug.Log($"[PartyManager] 📦 JSON размер: {json.Length} байт, содержимое: {json}");
        SocketIOManager.Instance.Emit("party_invite", json);
    }

    /// <summary>
    /// Принять приглашение в группу
    /// </summary>
    public void AcceptInvite(string partyId, string inviterSocketId)
    {
        Debug.Log($"[PartyManager] ✅ Принимаем приглашение в группу {partyId}");

        PartyAcceptData data = new PartyAcceptData
        {
            partyId = partyId,
            inviterSocketId = inviterSocketId
        };

        string json = JsonUtility.ToJson(data);
        SocketIOManager.Instance.Emit("party_accept", json);

        // Устанавливаем локальное состояние
        currentPartyId = partyId;
        isInParty = true;
        isPartyLeader = false;
    }

    /// <summary>
    /// Отклонить приглашение в группу
    /// </summary>
    public void DeclineInvite(string partyId, string inviterSocketId)
    {
        Debug.Log($"[PartyManager] ❌ Отклоняем приглашение в группу {partyId}");

        PartyDeclineData data = new PartyDeclineData
        {
            partyId = partyId,
            inviterSocketId = inviterSocketId
        };

        string json = JsonUtility.ToJson(data);
        SocketIOManager.Instance.Emit("party_decline", json);
    }

    /// <summary>
    /// Выйти из группы
    /// </summary>
    public void LeaveParty()
    {
        if (!isInParty)
        {
            Debug.LogWarning("[PartyManager] ❌ Вы не в группе!");
            return;
        }

        Debug.Log($"[PartyManager] 👋 Выходим из группы {currentPartyId}");

        PartyLeaveData data = new PartyLeaveData
        {
            partyId = currentPartyId,
            memberSocketIds = partyMembers.Select(m => m.socketId).ToArray()
        };

        string json = JsonUtility.ToJson(data);
        SocketIOManager.Instance.Emit("party_leave", json);

        // Очищаем локальное состояние
        ClearParty();
    }

    /// <summary>
    /// Обновить свои статы (HP/MP) для членов группы
    /// </summary>
    public void UpdateMyStats(float health, float mana, float maxHealth, float maxMana)
    {
        if (!isInParty)
        {
            return;
        }

        string mySocketId = SocketIOManager.Instance != null ? SocketIOManager.Instance.SocketId : "";

        PartyStatsUpdateData data = new PartyStatsUpdateData
        {
            partyId = currentPartyId,
            memberSocketIds = partyMembers.Select(m => m.socketId).ToArray(),
            health = health,
            mana = mana,
            maxHealth = maxHealth,
            maxMana = maxMana
        };

        string json = JsonUtility.ToJson(data);
        SocketIOManager.Instance.Emit("party_stats_update", json);

        if (!string.IsNullOrEmpty(mySocketId))
        {
            UpdateCachedMemberStats(mySocketId, health, mana, maxHealth, maxMana);
        }
    }

    /// <summary>
    /// Проверить является ли игрок союзником (членом группы)
    /// </summary>
    public bool IsAlly(string socketId)
    {
        if (!isInParty || string.IsNullOrEmpty(socketId))
        {
            return false;
        }

        return partyMembers.Any(m => m.socketId == socketId);
    }

    /// <summary>
    /// Проверка союзника по TargetableEntity (удобно для скиллов/снарядов)
    /// </summary>
    public bool IsAlly(TargetableEntity entity)
    {
        if (entity == null || !isInParty)
        {
            return false;
        }

        string socketId = entity.GetOwnerId();
        if (string.IsNullOrEmpty(socketId))
        {
            NetworkPlayer networkPlayer = entity.GetComponent<NetworkPlayer>();
            if (networkPlayer == null)
            {
                networkPlayer = entity.GetComponentInParent<NetworkPlayer>();
            }

            if (networkPlayer != null)
            {
                socketId = networkPlayer.socketId;
                entity.SetOwnerId(socketId);
            }
        }

        return IsAlly(socketId);
    }

    /// <summary>
    /// Получить список socketId всех членов группы
    /// </summary>
    public List<string> GetPartyMemberSocketIds()
    {
        return partyMembers.Select(m => m.socketId).ToList();
    }

    /// <summary>
    /// Получить данные члена группы по socketId
    /// </summary>
    public PartyMember GetMember(string socketId)
    {
        return partyMembers.FirstOrDefault(m => m.socketId == socketId);
    }

    // ═══════════════════════════════════════════
    // PRIVATE METHODS
    // ═══════════════════════════════════════════

    /// <summary>
    /// Создать новую группу (локальный игрок становится лидером)
    /// </summary>
    private void CreateParty()
    {
        currentPartyId = System.Guid.NewGuid().ToString();
        isInParty = true;
        isPartyLeader = true;

        // Добавляем себя в список членов
        string mySocketId = SocketIOManager.Instance.SocketId;
        string myUsername = PlayerPrefs.GetString("Username", "Player");
        string myClass = PlayerPrefs.GetString("SelectedCharacterClass", "Warrior");
        int myLevel = 1; // TODO: Get from leveling system

        PartyMember me = new PartyMember
        {
            socketId = mySocketId,
            username = myUsername,
            characterClass = myClass,
            level = myLevel,
            health = 100,
            mana = 100,
            maxHealth = 100,
            maxMana = 100
        };

        partyMembers.Add(me);

        Debug.Log($"[PartyManager] ✅ Создана группа: {currentPartyId} (лидер: {myUsername})");

        OnPartyCreated?.Invoke(currentPartyId);
    }

    /// <summary>
    /// Очистить данные группы
    /// </summary>
    private void ClearParty()
    {
        currentPartyId = "";
        isInParty = false;
        isPartyLeader = false;
        partyMembers.Clear();

        Debug.Log("[PartyManager] 🧹 Данные группы очищены");
    }

    // ═══════════════════════════════════════════
    // SOCKET EVENT HANDLERS
    // ═══════════════════════════════════════════

    private void OnPartyInviteReceived(string jsonData)
    {
        Debug.Log($"[PartyManager] 📨 Получено приглашение в группу (RAW): {jsonData}");
        Debug.Log($"[PartyManager] 🔍 Первый символ: '{(jsonData.Length > 0 ? jsonData[0].ToString() : "EMPTY")}'");

        // ВАЖНО: Проверяем, не является ли jsonData escaped JSON строкой (начинается с кавычки)
        // Сервер иногда отправляет: "{\"partyId\":...}" вместо {"partyId":...}
        if (jsonData.StartsWith("\"") && jsonData.EndsWith("\""))
        {
            // Убираем внешние кавычки и декодируем escaped символы
            jsonData = System.Text.RegularExpressions.Regex.Unescape(jsonData.Substring(1, jsonData.Length - 2));
            Debug.Log($"[PartyManager] 🔧 Unescaped JSON: {jsonData}");
        }

        var invite = JsonUtility.FromJson<PartyInvite>(jsonData);
        lastReceivedInvite = invite;
        Debug.Log($"[PartyManager] 🔔 Вызываем событие OnInviteReceived (подписчиков: {OnInviteReceived?.GetInvocationList()?.Length ?? 0})");
        OnInviteReceived?.Invoke(invite);
        Debug.Log($"[PartyManager] ✅ Событие OnInviteReceived вызвано");
    }

    private void OnPartyInviteSent(string jsonData)
    {
        Debug.Log($"[PartyManager] ✅ Приглашение отправлено: {jsonData}");
    }

    private void OnPartyMemberJoined(string jsonData)
    {
        Debug.Log($"[PartyManager] 🎉 Новый член группы (RAW): {jsonData}");

        // ВАЖНО: Unescaping как в OnPartyInviteReceived
        if (jsonData.StartsWith("\"") && jsonData.EndsWith("\""))
        {
            jsonData = Regex.Unescape(jsonData.Substring(1, jsonData.Length - 2));
            Debug.Log($"[PartyManager] 🔧 Unescaped JSON: {jsonData}");
        }

        var memberData = JsonUtility.FromJson<PartyMemberJoinedData>(jsonData);
        Debug.Log($"[PartyManager] 👤 Добавляем {memberData.memberUsername} ({memberData.memberClass}, Ур.{memberData.memberLevel})");

        PartyMember newMember = new PartyMember
        {
            socketId = memberData.memberSocketId,
            username = memberData.memberUsername,
            characterClass = memberData.memberClass,
            level = memberData.memberLevel,
            health = 100,
            mana = 100,
            maxHealth = 100,
            maxMana = 100
        };

        partyMembers.Add(newMember);
        OnMemberJoined?.Invoke(newMember);
        Debug.Log($"[PartyManager] ✅ Член группы добавлен! Всего в группе: {partyMembers.Count}");
    }

    private void OnPartyJoinedReceived(string jsonData)
    {
        Debug.Log($"[PartyManager] ✅ Вступили в группу (RAW): {jsonData}");

        // ВАЖНО: Unescaping как в OnPartyInviteReceived
        if (jsonData.StartsWith("\"") && jsonData.EndsWith("\""))
        {
            jsonData = Regex.Unescape(jsonData.Substring(1, jsonData.Length - 2));
            Debug.Log($"[PartyManager] 🔧 Unescaped JSON: {jsonData}");
        }

        var data = JsonUtility.FromJson<PartyJoinedData>(jsonData);
        partyMembers.Clear(); // гарантируем чистое состояние
        currentPartyId = data.partyId;
        isInParty = true;
        isPartyLeader = false;

        Debug.Log($"[PartyManager] 🎊 Вступили в группу {currentPartyId}! isPartyLeader={isPartyLeader}");

        // ВАЖНО: Добавляем ЛИДЕРА группы (инвайтера) в список членов
        string leaderSocketId = data.leaderSocketId;
        string leaderUsername = data.leaderUsername;
        string leaderClass = data.leaderClass;
        int leaderLevel = data.leaderLevel;

        // Фолбэк на данные из последнего приглашения, если сервер не прислал лидера
        if (string.IsNullOrEmpty(leaderSocketId) && lastReceivedInvite != null && lastReceivedInvite.partyId == data.partyId)
        {
            leaderSocketId = lastReceivedInvite.inviterSocketId;
            leaderUsername = lastReceivedInvite.inviterUsername;
            leaderClass = lastReceivedInvite.inviterClass;
            leaderLevel = lastReceivedInvite.inviterLevel;
            Debug.Log("[PartyManager] 🩹 Лидер не пришёл с сервера, используем данные из приглашения");
        }

        if (!string.IsNullOrEmpty(leaderSocketId))
        {
            PartyMember leader = new PartyMember
            {
                socketId = leaderSocketId,
                username = string.IsNullOrEmpty(leaderUsername) ? "Leader" : leaderUsername,
                characterClass = string.IsNullOrEmpty(leaderClass) ? "Unknown" : leaderClass,
                level = leaderLevel > 0 ? leaderLevel : 1,
                health = 100,
                mana = 100,
                maxHealth = 100,
                maxMana = 100
            };

            partyMembers.Add(leader);
            Debug.Log($"[PartyManager] 👑 Добавили лидера: {leader.username} ({leader.characterClass}, Ур.{leader.level})");
        }
        else
        {
            Debug.LogWarning("[PartyManager] ⚠️ Лидер группы не определён даже после фолбэка!");
        }

        // Добавляем себя в список членов
        string mySocketId = SocketIOManager.Instance.SocketId;
        string myUsername = PlayerPrefs.GetString("Username", "Player");
        string myClass = PlayerPrefs.GetString("SelectedCharacterClass", "Warrior");
        int myLevel = 1;

        PartyMember me = new PartyMember
        {
            socketId = mySocketId,
            username = myUsername,
            characterClass = myClass,
            level = myLevel,
            health = 100,
            mana = 100,
            maxHealth = 100,
            maxMana = 100
        };

        partyMembers.Add(me);
        Debug.Log($"[PartyManager] ✅ Добавили себя в группу. Всего членов: {partyMembers.Count}");

        lastReceivedInvite = null;

        // Вызываем публичное событие для UI (например, PartyUI)
        OnPartyJoined?.Invoke(currentPartyId);
    }

    private void OnPartyInviteDeclined(string jsonData)
    {
        Debug.Log($"[PartyManager] ❌ Приглашение отклонено: {jsonData}");

        var data = JsonUtility.FromJson<PartyInviteDeclinedData>(jsonData);
        OnInviteDeclined?.Invoke(data.declinedUsername);
    }

    private void OnPartyMemberLeft(string jsonData)
    {
        Debug.Log($"[PartyManager] 👋 Член группы вышел: {jsonData}");

        var data = JsonUtility.FromJson<PartyMemberLeftData>(jsonData);

        // Удаляем члена из списка
        partyMembers.RemoveAll(m => m.socketId == data.leftSocketId);

        OnMemberLeft?.Invoke(data.leftSocketId);

        // Если группа пустая - очищаем
        if (partyMembers.Count == 0)
        {
            ClearParty();
        }
    }

    private void OnPartyLeftConfirmed(string jsonData)
    {
        Debug.Log($"[PartyManager] ✅ Вышли из группы: {jsonData}");

        ClearParty();
        OnPartyLeft?.Invoke();
    }

    private void OnPartyMemberStatsUpdated(string jsonData)
    {
        Debug.Log($"[PartyManager] 📊 Обновление статов члена группы: {jsonData}");

        var data = JsonUtility.FromJson<PartyMemberStatsData>(jsonData);

        UpdateCachedMemberStats(
            data.memberSocketId,
            data.health,
            data.mana,
            data.maxHealth,
            data.maxMana);
    }

    private void OnPartySynced(string jsonData)
    {
        Debug.Log($"[PartyManager] 🔄 Синхронизация группы: {jsonData}");
        // TODO: Implement if needed
    }

    private void OnPartyErrorReceived(string jsonData)
    {
        Debug.LogError($"[PartyManager] ❌ Ошибка группы: {jsonData}");

        string message = jsonData;

        if (!string.IsNullOrEmpty(jsonData) && jsonData.TrimStart().StartsWith("{"))
        {
            try
            {
                var data = JsonUtility.FromJson<PartyErrorData>(jsonData);
                if (data != null && !string.IsNullOrEmpty(data.message))
                {
                    message = data.message;
                }
            }
            catch
            {
                // Игнорируем и используем исходный текст
            }
        }

        OnPartyError?.Invoke(message);
    }

    private void UpdateCachedMemberStats(string socketId, float health, float mana, float maxHealth, float maxMana)
    {
        if (string.IsNullOrEmpty(socketId))
        {
            return;
        }

        var member = partyMembers.FirstOrDefault(m => m.socketId == socketId);
        if (member == null)
        {
            Debug.LogWarning($"[PartyManager] ⚠️ Не найден член группы с socketId={socketId} для обновления статов");
            return;
        }

        member.health = health;
        member.mana = mana;
        member.maxHealth = maxHealth;
        member.maxMana = maxMana;

        PartyMemberStats stats = new PartyMemberStats
        {
            health = health,
            mana = mana,
            maxHealth = maxHealth,
            maxMana = maxMana
        };

        OnMemberStatsUpdated?.Invoke(socketId, stats);
    }

    // Public getters
    public bool IsInParty => isInParty;
    public bool IsPartyLeader => isPartyLeader;
    public string CurrentPartyId => currentPartyId;
    public List<PartyMember> PartyMembers => new List<PartyMember>(partyMembers);
    public int PartySize => partyMembers.Count;
    public int MaxPartySize => maxPartyMembers;
}

// ═══════════════════════════════════════════
// DATA CLASSES
// ═══════════════════════════════════════════

[Serializable]
public class PartyMember
{
    public string socketId;
    public string username;
    public string characterClass;
    public int level;
    public float health;
    public float mana;
    public float maxHealth;
    public float maxMana;
}

[Serializable]
public class PartyMemberStats
{
    public float health;
    public float mana;
    public float maxHealth;
    public float maxMana;
}

[Serializable]
public class PartyInvite
{
    public string partyId;
    public string inviterSocketId;
    public string inviterUsername;
    public string inviterClass;
    public int inviterLevel;
    public long timestamp;
}

[Serializable]
public class PartyMemberJoinedData
{
    public string partyId;
    public string memberSocketId;
    public string memberUsername;
    public string memberClass;
    public int memberLevel;
    public long timestamp;
}

[Serializable]
public class PartyJoinedData
{
    public string partyId;
    public string leaderSocketId;
    public string leaderUsername;
    public string leaderClass;
    public int leaderLevel;
    public long timestamp;
}

[Serializable]
public class PartyInviteDeclinedData
{
    public string partyId;
    public string declinedUsername;
    public long timestamp;
}

[Serializable]
public class PartyMemberLeftData
{
    public string partyId;
    public string leftSocketId;
    public string leftUsername;
    public long timestamp;
}

[Serializable]
public class PartyMemberStatsData
{
    public string partyId;
    public string memberSocketId;
    public string memberUsername;
    public float health;
    public float mana;
    public float maxHealth;
    public float maxMana;
    public long timestamp;
}

[Serializable]
public class PartyErrorData
{
    public string message;
}

// ═══════════════════════════════════════════
// OUTGOING DATA CLASSES (Client → Server)
// ═══════════════════════════════════════════

[Serializable]
public class PartyInviteData
{
    public string targetSocketId;
    public string partyId;
    public string inviterSocketId;
    public string inviterUsername;
    public string inviterClass;
    public int inviterLevel;
}

[Serializable]
public class PartyAcceptData
{
    public string partyId;
    public string inviterSocketId;
}

[Serializable]
public class PartyDeclineData
{
    public string partyId;
    public string inviterSocketId;
}

[Serializable]
public class PartyLeaveData
{
    public string partyId;
    public string[] memberSocketIds;
}

[Serializable]
public class PartyStatsUpdateData
{
    public string partyId;
    public string[] memberSocketIds;
    public float health;
    public float mana;
    public float maxHealth;
    public float maxMana;
}
