using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

/// <summary>
/// Менеджер комнат - управляет созданием, поиском и входом в комнаты
/// REST API для управления комнатами, WebSocket для реального времени
/// </summary>
public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "https://aetherion-server-gv5u.onrender.com";

    [Header("Current Room")]
    private string currentRoomId = "";
    private bool isInRoom = false;
    private RoomInfo currentRoom;

    // Events
    public event Action<RoomInfo> OnRoomCreated;
    public event Action<RoomInfo> OnRoomJoined;
    public event Action<string> OnRoomError;
    public event Action OnRoomLeft;

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

    // ===== REST API METHODS =====

    /// <summary>
    /// Получить список доступных комнат
    /// </summary>
    public void GetAvailableRooms(Action<RoomListResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(GetAvailableRoomsCoroutine(onSuccess, onError));
    }

    private IEnumerator GetAvailableRoomsCoroutine(Action<RoomListResponse> onSuccess, Action<string> onError)
    {
        string token = PlayerPrefs.GetString("UserToken", "");
        string url = $"{serverUrl}/api/room/list";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            RoomListResponse response = JsonUtility.FromJson<RoomListResponse>(json);

            Debug.Log($"[RoomManager] Получено комнат: {response.rooms.Length}");
            onSuccess?.Invoke(response);
        }
        else
        {
            Debug.LogError($"[RoomManager] Ошибка получения списка комнат: {request.error}");
            onError?.Invoke(request.error);
        }
    }

    /// <summary>
    /// Создать новую комнату
    /// </summary>
    public void CreateRoom(string roomName, int maxPlayers, bool isPrivate, string password, Action<RoomInfo> onSuccess, Action<string> onError)
    {
        StartCoroutine(CreateRoomCoroutine(roomName, maxPlayers, isPrivate, password, onSuccess, onError));
    }

    private IEnumerator CreateRoomCoroutine(string roomName, int maxPlayers, bool isPrivate, string password, Action<RoomInfo> onSuccess, Action<string> onError)
    {
        string token = PlayerPrefs.GetString("UserToken", "");
        string url = $"{serverUrl}/api/room/create";

        // Получаем данные персонажа из PlayerPrefs
        string characterClass = PlayerPrefs.GetString("SelectedCharacterClass", "Warrior");
        string username = PlayerPrefs.GetString("Username", "Player");
        // Level можно получить из характеристик персонажа, пока используем 1
        int level = 1;

        CreateRoomRequest requestData = new CreateRoomRequest
        {
            roomName = roomName,
            maxPlayers = maxPlayers,
            isPrivate = isPrivate,
            password = password,
            characterClass = characterClass,
            username = username,
            level = level
        };

        string json = JsonUtility.ToJson(requestData);
        Debug.Log($"[RoomManager] Create room request: {json}");

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseJson = request.downloadHandler.text;
            CreateRoomResponse response = JsonUtility.FromJson<CreateRoomResponse>(responseJson);

            if (response.success)
            {
                currentRoom = response.room;
                currentRoomId = response.room.roomId;
                isInRoom = true;

                // ВАЖНО: Сохраняем CurrentRoomId СРАЗУ для NetworkSyncManager
                PlayerPrefs.SetString("CurrentRoomId", currentRoomId);
                PlayerPrefs.Save();

                Debug.Log($"[RoomManager] ✅ Комната создана: {response.room.roomName} (ID: {currentRoomId})");
                OnRoomCreated?.Invoke(response.room);
                onSuccess?.Invoke(response.room);
            }
            else
            {
                Debug.LogError($"[RoomManager] ❌ Ошибка создания комнаты: {response.message}");
                OnRoomError?.Invoke(response.message);
                onError?.Invoke(response.message);
            }
        }
        else
        {
            // Логируем тело ответа даже при ошибке для дебага
            string errorBody = request.downloadHandler != null ? request.downloadHandler.text : "No response body";
            Debug.LogError($"[RoomManager] ❌ HTTP ошибка: {request.error}");
            Debug.LogError($"[RoomManager] Error response body: {errorBody}");
            onError?.Invoke(request.error);
        }
    }

    /// <summary>
    /// Присоединиться к существующей комнате
    /// </summary>
    public void JoinRoom(string roomId, string password, Action<RoomInfo> onSuccess, Action<string> onError)
    {
        StartCoroutine(JoinRoomCoroutine(roomId, password, onSuccess, onError));
    }

    private IEnumerator JoinRoomCoroutine(string roomId, string password, Action<RoomInfo> onSuccess, Action<string> onError)
    {
        string token = PlayerPrefs.GetString("UserToken", "");
        string url = $"{serverUrl}/api/room/join";

        // Получаем данные персонажа из PlayerPrefs
        string characterClass = PlayerPrefs.GetString("SelectedCharacterClass", "Warrior");
        string username = PlayerPrefs.GetString("Username", "Player");
        int level = 1;

        JoinRoomRequest requestData = new JoinRoomRequest
        {
            roomId = roomId,
            password = password,
            characterClass = characterClass,
            username = username,
            level = level
        };

        string json = JsonUtility.ToJson(requestData);
        Debug.Log($"[RoomManager] Join room request: {json}");

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseJson = request.downloadHandler.text;
            JoinRoomResponse response = JsonUtility.FromJson<JoinRoomResponse>(responseJson);

            if (response.success)
            {
                currentRoom = response.room;
                currentRoomId = response.room.roomId;
                isInRoom = true;

                // ВАЖНО: Сохраняем CurrentRoomId СРАЗУ для NetworkSyncManager
                PlayerPrefs.SetString("CurrentRoomId", currentRoomId);
                PlayerPrefs.Save();

                Debug.Log($"[RoomManager] ✅ Вошли в комнату: {response.room.roomName}");
                OnRoomJoined?.Invoke(response.room);
                onSuccess?.Invoke(response.room);
            }
            else
            {
                Debug.LogError($"[RoomManager] ❌ Ошибка входа в комнату: {response.message}");
                OnRoomError?.Invoke(response.message);
                onError?.Invoke(response.message);
            }
        }
        else
        {
            // Логируем тело ответа даже при ошибке для дебага
            string errorBody = request.downloadHandler != null ? request.downloadHandler.text : "No response body";
            Debug.LogError($"[RoomManager] ❌ HTTP ошибка: {request.error}");
            Debug.LogError($"[RoomManager] Error response body: {errorBody}");
            onError?.Invoke(request.error);
        }
    }

    /// <summary>
    /// Выйти из комнаты
    /// </summary>
    public void LeaveRoom(Action<bool> onComplete = null)
    {
        if (!isInRoom)
        {
            Debug.LogWarning("[RoomManager] Вы не в комнате!");
            onComplete?.Invoke(false);
            return;
        }

        StartCoroutine(LeaveRoomCoroutine(onComplete));
    }

    private IEnumerator LeaveRoomCoroutine(Action<bool> onComplete)
    {
        string token = PlayerPrefs.GetString("UserToken", "");
        string url = $"{serverUrl}/api/room/leave";

        LeaveRoomRequest requestData = new LeaveRoomRequest
        {
            roomId = currentRoomId
        };

        string json = JsonUtility.ToJson(requestData);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[RoomManager] ✅ Вышли из комнаты");
            isInRoom = false;
            currentRoomId = "";
            currentRoom = null;

            // Очищаем PlayerPrefs
            PlayerPrefs.DeleteKey("CurrentRoomId");
            PlayerPrefs.Save();

            OnRoomLeft?.Invoke();
            onComplete?.Invoke(true);
        }
        else
        {
            Debug.LogError($"[RoomManager] ❌ Ошибка выхода из комнаты: {request.error}");
            onComplete?.Invoke(false);
        }
    }

    /// <summary>
    /// Получить информацию о комнате
    /// </summary>
    public void GetRoomInfo(string roomId, Action<RoomInfo> onSuccess, Action<string> onError)
    {
        StartCoroutine(GetRoomInfoCoroutine(roomId, onSuccess, onError));
    }

    private IEnumerator GetRoomInfoCoroutine(string roomId, Action<RoomInfo> onSuccess, Action<string> onError)
    {
        string token = PlayerPrefs.GetString("UserToken", "");
        string url = $"{serverUrl}/api/room/{roomId}";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            RoomInfoResponse response = JsonUtility.FromJson<RoomInfoResponse>(json);

            if (response.success)
            {
                Debug.Log($"[RoomManager] Информация о комнате получена: {response.room.roomName}");
                onSuccess?.Invoke(response.room);
            }
            else
            {
                Debug.LogError($"[RoomManager] Ошибка: {response.message}");
                onError?.Invoke(response.message);
            }
        }
        else
        {
            Debug.LogError($"[RoomManager] HTTP ошибка: {request.error}");
            onError?.Invoke(request.error);
        }
    }

    // ===== MULTIPLAYER FLOW =====

    /// <summary>
    /// Создать комнату и подключиться через WebSocket
    /// </summary>
    public void CreateAndJoinRoom(string roomName, Action<bool> onComplete)
    {
        string characterClass = PlayerPrefs.GetString("SelectedCharacterClass", "Warrior");

        CreateRoom(roomName, 20, false, "",
            onSuccess: (room) =>
            {
                Debug.Log("[RoomManager] Комната создана, подключаемся через WebSocket...");
                ConnectToWebSocket(room.roomId, characterClass, onComplete);
            },
            onError: (error) =>
            {
                Debug.LogError($"[RoomManager] Не удалось создать комнату: {error}");
                onComplete?.Invoke(false);
            }
        );
    }

    /// <summary>
    /// Присоединиться к комнате и подключиться через WebSocket
    /// </summary>
    public void JoinAndConnectRoom(string roomId, Action<bool> onComplete)
    {
        string characterClass = PlayerPrefs.GetString("SelectedCharacterClass", "Warrior");

        JoinRoom(roomId, "",
            onSuccess: (room) =>
            {
                Debug.Log("[RoomManager] Вошли в комнату, подключаемся через WebSocket...");
                ConnectToWebSocket(room.roomId, characterClass, onComplete);
            },
            onError: (error) =>
            {
                Debug.LogError($"[RoomManager] Не удалось войти в комнату: {error}");
                onComplete?.Invoke(false);
            }
        );
    }

    /// <summary>
    /// Подключиться к WebSocket и войти в комнату
    /// </summary>
    private void ConnectToWebSocket(string roomId, string characterClass, Action<bool> onComplete)
    {
        string token = PlayerPrefs.GetString("UserToken", "");

        // Connect to SocketIOClient
        SocketIOClient.Instance.Connect(token, (success) =>
        {
            if (success)
            {
                Debug.Log("[RoomManager] 🚀 Socket.IO подключен, входим в комнату...");

                // Join room via Socket.IO
                SocketIOClient.Instance.JoinRoom(roomId, characterClass, (joined) =>
                {
                    if (joined)
                    {
                        Debug.Log("[RoomManager] ✅ Полностью подключены к игре!");

                        // Save room ID for scene transition
                        PlayerPrefs.SetString("CurrentRoomId", roomId);
                        PlayerPrefs.Save();

                        onComplete?.Invoke(true);
                    }
                    else
                    {
                        Debug.LogError("[RoomManager] ❌ Не удалось войти в комнату через Socket.IO");
                        onComplete?.Invoke(false);
                    }
                });
            }
            else
            {
                Debug.LogError("[RoomManager] ❌ Не удалось подключиться к Socket.IO");
                onComplete?.Invoke(false);
            }
        });
    }

    /// <summary>
    /// Загрузить арену после подключения
    /// </summary>
    public void LoadArenaScene()
    {
        if (!isInRoom)
        {
            Debug.LogError("[RoomManager] Не в комнате! Невозможно загрузить арену");
            return;
        }

        Debug.Log("[RoomManager] Загрузка ArenaScene...");

        // Set target scene for LoadingScreen
        PlayerPrefs.SetString("TargetScene", "ArenaScene");
        PlayerPrefs.Save();

        SceneManager.LoadScene("LoadingScene");
    }

    // Public getters
    public bool IsInRoom => isInRoom;
    public string CurrentRoomId => currentRoomId;
    public RoomInfo CurrentRoom => currentRoom;
}

// ===== DATA CLASSES =====

[Serializable]
public class CreateRoomRequest
{
    public string roomName;
    public int maxPlayers;
    public bool isPrivate;
    public string password;
    public string characterClass;  // Класс персонажа
    public string username;         // Имя пользователя
    public int level;               // Уровень персонажа
}

[Serializable]
public class CreateRoomResponse
{
    public bool success;
    public string message;
    public RoomInfo room;
}

[Serializable]
public class JoinRoomRequest
{
    public string roomId;
    public string password;
    public string characterClass;  // Класс персонажа
    public string username;         // Имя пользователя
    public int level;               // Уровень персонажа
}

[Serializable]
public class JoinRoomResponse
{
    public bool success;
    public string message;
    public RoomInfo room;
}

[Serializable]
public class LeaveRoomRequest
{
    public string roomId;
}

[Serializable]
public class RoomInfoResponse
{
    public bool success;
    public RoomInfo room;
    public string message;
}

[Serializable]
public class RoomListResponse
{
    public bool success;
    public RoomInfo[] rooms;
}

[Serializable]
public class RoomInfo
{
    public string roomId;
    public string roomName;
    public string hostUserId;
    public int maxPlayers;
    public int currentPlayers;
    public bool isPrivate;
    public bool isActive;
    public string status;
    public string createdAt;
}
