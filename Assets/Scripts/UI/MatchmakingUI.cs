using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// UI для лобби матчмейкинга
/// Показывает:
/// - Статус поиска/ожидания
/// - Количество игроков в комнате
/// - Таймер обратного отсчета (20 секунд)
/// - Кнопку отмены
///
/// Должен быть в BattleScene и активироваться когда игрок в режиме ожидания
/// </summary>
public class MatchmakingUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject lobbyPanel; // Весь UI лобби
    [SerializeField] private TextMeshProUGUI statusText; // "Ожидание игроков..."
    [SerializeField] private TextMeshProUGUI playerCountText; // "Игроков: 1/20"
    [SerializeField] private TextMeshProUGUI timerText; // "Матч начнётся через: 20"
    [SerializeField] private GameObject timerPanel; // Панель таймера (скрыта пока нет 2го игрока)
    [SerializeField] private Button cancelButton; // Кнопка "Отмена"
    [SerializeField] private Image timerFillImage; // Заполнение таймера (опционально)

    [Header("Settings")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private Color waitingColor = Color.yellow;
    [SerializeField] private Color countdownColor = Color.green;

    private bool isInLobby = false;

    void Start()
    {
        SetupUI();
        SubscribeToEvents();

        // По умолчанию лобби скрыто
        HideLobby();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// Настройка UI элементов
    /// </summary>
    private void SetupUI()
    {
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelButtonClick);
        }
    }

    /// <summary>
    /// Подписаться на события MatchmakingManager
    /// </summary>
    private void SubscribeToEvents()
    {
        if (MatchmakingManager.Instance != null)
        {
            MatchmakingManager.Instance.OnRoomFound += OnRoomFound;
            MatchmakingManager.Instance.OnPlayerCountChanged += OnPlayerCountChanged;
            // УДАЛЕНО: OnCountdownTick (instant spawn, нет таймера)
            MatchmakingManager.Instance.OnMatchStart += OnMatchStart;
            MatchmakingManager.Instance.OnMatchmakingError += OnMatchmakingError;
        }
    }

    /// <summary>
    /// Отписаться от событий
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (MatchmakingManager.Instance != null)
        {
            MatchmakingManager.Instance.OnRoomFound -= OnRoomFound;
            MatchmakingManager.Instance.OnPlayerCountChanged -= OnPlayerCountChanged;
            // УДАЛЕНО: OnCountdownTick (instant spawn, нет таймера)
            MatchmakingManager.Instance.OnMatchStart -= OnMatchStart;
            MatchmakingManager.Instance.OnMatchmakingError -= OnMatchmakingError;
        }
    }

    /// <summary>
    /// Показать лобби ожидания
    /// </summary>
    public void ShowLobby()
    {
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
        }

        isInLobby = true;

        // Начальное состояние
        UpdateStatusText("Поиск игроков...", waitingColor);
        UpdatePlayerCount(1, 50); // ИЗМЕНЕНО: лимит 50 игроков
        HideTimer();

        Debug.Log("[MatchmakingUI] 📺 Лобби показано");
    }

    /// <summary>
    /// Скрыть лобби
    /// </summary>
    public void HideLobby()
    {
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
        }

        isInLobby = false;

        Debug.Log("[MatchmakingUI] 📺 Лобби скрыто");
    }

    /// <summary>
    /// Событие: комната найдена/создана
    /// </summary>
    private void OnRoomFound(RoomInfo roomInfo)
    {
        Debug.Log($"[MatchmakingUI] Комната найдена: {roomInfo.roomName}");

        ShowLobby();

        if (roomInfo.isHost)
        {
            UpdateStatusText("Ожидание игроков...", waitingColor);
        }
        else
        {
            UpdateStatusText("Подключились к игре!", countdownColor);
        }

        UpdatePlayerCount(roomInfo.currentPlayers, roomInfo.maxPlayers);
    }

    /// <summary>
    /// Событие: количество игроков изменилось
    /// </summary>
    private void OnPlayerCountChanged(int playerCount)
    {
        Debug.Log($"[MatchmakingUI] Игроков в комнате: {playerCount}");

        UpdatePlayerCount(playerCount, 50); // ИЗМЕНЕНО: лимит 50 игроков

        // УДАЛЕНО: Логика "второй игрок" (instant spawn, нет ожидания)
    }

    // УДАЛЕНО: OnCountdownTick метод больше не нужен (instant spawn, нет таймера)
    // Таймер обратного отсчета удалён из системы

    /// <summary>
    /// Событие: матч начинается
    /// </summary>
    private void OnMatchStart()
    {
        Debug.Log("[MatchmakingUI] 🎮 Матч начинается!");

        UpdateStatusText("Начинаем бой!", Color.green);

        // Скрываем лобби через 1 секунду
        Invoke(nameof(HideLobby), 1f);
    }

    /// <summary>
    /// Событие: ошибка матчмейкинга
    /// </summary>
    private void OnMatchmakingError(string error)
    {
        Debug.LogError($"[MatchmakingUI] Ошибка: {error}");

        UpdateStatusText($"Ошибка: {error}", Color.red);

        // Возвращаемся в GameScene через 3 секунды
        Invoke(nameof(ReturnToGameScene), 3f);
    }

    /// <summary>
    /// Обновить текст статуса
    /// </summary>
    private void UpdateStatusText(string text, Color color)
    {
        if (statusText != null)
        {
            statusText.text = text;
            statusText.color = color;
        }
    }

    /// <summary>
    /// Обновить количество игроков
    /// </summary>
    private void UpdatePlayerCount(int current, int max)
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"Игроков: {current}/{max}";
        }
    }

    /// <summary>
    /// Показать таймер
    /// </summary>
    private void ShowTimer(float timeRemaining)
    {
        if (timerPanel != null && !timerPanel.activeSelf)
        {
            timerPanel.SetActive(true);
        }

        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(timeRemaining);
            timerText.text = $"Матч начнётся через: {seconds}";
        }
    }

    /// <summary>
    /// Скрыть таймер
    /// </summary>
    private void HideTimer()
    {
        if (timerPanel != null)
        {
            timerPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Нажатие на кнопку "Отмена"
    /// </summary>
    private void OnCancelButtonClick()
    {
        Debug.Log("[MatchmakingUI] Отменяем матчмейкинг...");

        if (MatchmakingManager.Instance != null)
        {
            MatchmakingManager.Instance.CancelMatchmaking();
        }

        ReturnToGameScene();
    }

    /// <summary>
    /// Вернуться в GameScene
    /// </summary>
    private void ReturnToGameScene()
    {
        Debug.Log("[MatchmakingUI] Возвращаемся в GameScene...");

        HideLobby();
        SceneManager.LoadScene(gameSceneName);
    }

    // Public methods для внешнего управления
    public bool IsInLobby => isInLobby;
}
