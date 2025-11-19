using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Менеджер матчмейкинга - автоматический поиск или создание комнаты
/// НОВАЯ МЕХАНИКА (INSTANT SPAWN):
/// 1. Игрок нажимает BattleButton
/// 2. Ищем открытую комнату со статусом "in_progress" (активная игра)
/// 3. Если найдена и есть место (< 50 игроков) - присоединяемся
/// 4. Если не найдена или заполнена - создаем новую комнату
/// 5. Игрок СРАЗУ спавнится в мире (нет ожидания, нет таймера)
/// 6. Другие игроки могут присоединяться в любой момент (drop-in/drop-out)
/// 7. Максимум 50 игроков в одной комнате
/// </summary>
public class MatchmakingManager : MonoBehaviour
{
    public static MatchmakingManager Instance { get; private set; }

    [Header("Matchmaking Settings")]
    [SerializeField] private int maxPlayersPerRoom = 50; // ИЗМЕНЕНО: увеличен лимит до 50

    [Header("Room Status")]
    private string currentRoomId = "";
    private bool isSearching = false;
    private bool isInMatchmaking = false;

    // УДАЛЕНО: Таймер больше не нужен (instant spawn)
    // private float countdownTimer = 0f;
    // private bool isCountdownActive = false;

    // События
    public event Action<RoomInfo> OnRoomFound; // Нашли или создали комнату
    public event Action<int> OnPlayerCountChanged; // Количество игроков изменилось
    // УДАЛЕНО: public event Action<float> OnCountdownTick; // Таймер больше не нужен
    public event Action OnMatchStart; // Матч начинается (instant spawn)
    public event Action<string> OnMatchmakingError; // Ошибка матчмейкинга

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

    // УДАЛЕНО: Update метод больше не нужен (нет таймера)

    /// <summary>
    /// ГЛАВНАЯ ФУНКЦИЯ: Найти или создать комнату
    /// </summary>
    public void FindOrCreateMatch(Action<bool> onComplete)
    {
        if (isSearching)
        {
            Debug.LogWarning("[MatchmakingManager] Уже ищем матч!");
            onComplete?.Invoke(false);
            return;
        }

        isSearching = true;
        isInMatchmaking = true;

        Debug.Log("[MatchmakingManager] 🔍 Начинаем поиск матча...");

        // Шаг 1: Получаем список доступных комнат
        RoomManager.Instance.GetAvailableRooms(
            onSuccess: (response) =>
            {
                Debug.Log($"[MatchmakingManager] Получено комнат: {response.rooms.Length}");

                // Ищем комнату со статусом "waiting" (ожидает игроков)
                RoomInfo availableRoom = FindWaitingRoom(response.rooms);

                if (availableRoom != null)
                {
                    // Нашли открытую комнату - присоединяемся
                    Debug.Log($"[MatchmakingManager] ✅ Найдена открытая комната: {availableRoom.roomName}");
                    JoinExistingRoom(availableRoom.roomId, onComplete);
                }
                else
                {
                    // Не нашли - создаем новую
                    Debug.Log("[MatchmakingManager] ❌ Открытых комнат нет, создаём новую");
                    CreateNewRoom(onComplete);
                }
            },
            onError: (error) =>
            {
                Debug.LogError($"[MatchmakingManager] ❌ Ошибка получения списка комнат: {error}");
                // При ошибке просто создаем новую комнату
                CreateNewRoom(onComplete);
            }
        );
    }

    /// <summary>
    /// Найти комнату в статусе "in_progress" с свободными местами (ИЗМЕНЕНО для instant spawn)
    /// </summary>
    private RoomInfo FindWaitingRoom(RoomInfo[] rooms)
    {
        foreach (var room in rooms)
        {
            // ИЗМЕНЕНО: Ищем комнату в статусе "in_progress" (игра уже идёт)
            // Теперь можно присоединяться к активным играм (drop-in)
            // - Статус "in_progress" (игра активна)
            // - Есть свободные места (< 50 игроков)
            // - Можно присоединиться (canJoin = true)
            if (room.status == "in_progress" && room.canJoin && room.currentPlayers < maxPlayersPerRoom)
            {
                Debug.Log($"[MatchmakingManager] Найдена активная комната: {room.roomName} ({room.currentPlayers}/{room.maxPlayers})");
                return room;
            }
        }

        Debug.Log("[MatchmakingManager] Активных комнат с свободными местами не найдено");
        return null;
    }

    /// <summary>
    /// Присоединиться к существующей комнате
    /// </summary>
    private void JoinExistingRoom(string roomId, Action<bool> onComplete)
    {
        Debug.Log($"[MatchmakingManager] Присоединяемся к комнате: {roomId}");

        RoomManager.Instance.JoinAndConnectRoom(roomId, (success) =>
        {
            if (success)
            {
                currentRoomId = roomId;
                isSearching = false;

                Debug.Log("[MatchmakingManager] ✅ Успешно присоединились к комнате!");

                // Получаем информацию о комнате
                RoomManager.Instance.GetRoomInfo(roomId,
                    onSuccess: (roomInfo) =>
                    {
                        OnRoomFound?.Invoke(roomInfo);
                        OnPlayerCountChanged?.Invoke(roomInfo.currentPlayers);

                        // ИЗМЕНЕНО: Instant spawn - сразу начинаем играть (нет ожидания)
                        Debug.Log($"[MatchmakingManager] Игроков в комнате: {roomInfo.currentPlayers}");

                        // Сразу вызываем OnMatchStart (нет таймера)
                        OnMatchStart?.Invoke();

                        onComplete?.Invoke(true);
                    },
                    onError: (error) =>
                    {
                        Debug.LogError($"[MatchmakingManager] Ошибка получения информации о комнате: {error}");
                        onComplete?.Invoke(true); // Всё равно успешно присоединились
                    }
                );
            }
            else
            {
                Debug.LogError("[MatchmakingManager] ❌ Не удалось присоединиться к комнате");
                isSearching = false;

                // Пробуем создать новую комнату
                CreateNewRoom(onComplete);
            }
        });
    }

    /// <summary>
    /// Создать новую комнату
    /// </summary>
    private void CreateNewRoom(Action<bool> onComplete)
    {
        string username = PlayerPrefs.GetString("Username", "Player");
        string roomName = $"{username}'s Battle";

        Debug.Log($"[MatchmakingManager] Создание новой комнаты: {roomName}");

        RoomManager.Instance.CreateAndJoinRoom(roomName, (success) =>
        {
            if (success)
            {
                currentRoomId = RoomManager.Instance.CurrentRoomId;
                isSearching = false;

                Debug.Log($"[MatchmakingManager] ✅ Комната создана: {currentRoomId}");

                // ИЗМЕНЕНО: Instant spawn - сразу начинаем играть (нет ожидания)
                RoomInfo roomInfo = RoomManager.Instance.CurrentRoom;
                OnRoomFound?.Invoke(roomInfo);
                OnPlayerCountChanged?.Invoke(1);

                // Сразу вызываем OnMatchStart (нет таймера, нет ожидания второго игрока)
                Debug.Log("[MatchmakingManager] 🚀 Начинаем игру сразу (instant spawn)!");
                OnMatchStart?.Invoke();

                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError("[MatchmakingManager] ❌ Не удалось создать комнату");
                isSearching = false;
                isInMatchmaking = false;
                OnMatchmakingError?.Invoke("Не удалось создать комнату");
                onComplete?.Invoke(false);
            }
        });
    }

    // УДАЛЕНО: Методы таймера больше не нужны (instant spawn)
    // - OnSecondPlayerJoined
    // - StartCountdown
    // - StartMatch
    // Теперь игроки спавнятся сразу при подключении

    // УДАЛЕНО: GameStartFallback больше не нужен
    // При instant spawn игрок сразу получает game_start от сервера при join_room

    /// <summary>
    /// Отменить поиск матча
    /// </summary>
    public void CancelMatchmaking()
    {
        if (!isInMatchmaking)
        {
            Debug.LogWarning("[MatchmakingManager] Не в процессе матчмейкинга!");
            return;
        }

        Debug.Log("[MatchmakingManager] Отменяем поиск матча...");

        isSearching = false;
        isInMatchmaking = false;
        // УДАЛЕНО: isCountdownActive и countdownTimer больше не используются

        // Если уже в комнате - выходим
        if (!string.IsNullOrEmpty(currentRoomId))
        {
            RoomManager.Instance.LeaveRoom();
            currentRoomId = "";
        }
    }

    /// <summary>
    /// Выйти из матча и вернуться в GameScene
    /// </summary>
    public void LeaveMatch()
    {
        Debug.Log("[MatchmakingManager] Выходим из матча...");

        isInMatchmaking = false;
        // УДАЛЕНО: isCountdownActive и countdownTimer больше не используются

        if (!string.IsNullOrEmpty(currentRoomId))
        {
            RoomManager.Instance.LeaveRoom();
            currentRoomId = "";
        }

        // Следующий игрок, который нажмёт BattleButton, создаст новую комнату
    }

    /// <summary>
    /// Публичный метод для обновления счётчика игроков (вызывается из NetworkSyncManager)
    /// </summary>
    public void UpdatePlayerCount(int playerCount)
    {
        Debug.Log($"[MatchmakingManager] Обновление счётчика игроков: {playerCount}");
        OnPlayerCountChanged?.Invoke(playerCount);
    }

    // Public getters
    public bool IsInMatchmaking => isInMatchmaking;
    // УДАЛЕНО: IsCountdownActive и CountdownTimer больше не нужны
    public string CurrentRoomId => currentRoomId;
}
