using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Система чата с вкладками (All Chat / Party Chat)
/// Поддержка: общий чат (белый) и командный чат (зеленый)
/// Интеграция с всплывающими сообщениями над головами игроков
/// </summary>
public class ChatManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject chatPanel; // Весь ChatPanel GameObject (с этим компонентом)
    [SerializeField] private CanvasGroup chatCanvasGroup; // CanvasGroup для показа/скрытия UI
    [SerializeField] private TMP_InputField messageInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button openChatButton; // Кнопка для открытия чата (для Android/Mobile)
    [SerializeField] private ScrollRect chatScrollRect;
    [SerializeField] private Transform chatContentContainer;
    [SerializeField] private GameObject chatMessagePrefab;

    [Header("Chat Tabs")]
    [SerializeField] private Button allChatTab;
    [SerializeField] private Button partyChatTab;
    [SerializeField] private GameObject allChatTabHighlight;
    [SerializeField] private GameObject partyChatTabHighlight;

    [Header("Settings")]
    [SerializeField] private int maxMessagesInChat = 50;
    [SerializeField] private float messageDisplayTime = 5f; // Время показа сообщения над головой
    [SerializeField] private float autoHideDelay = 5f; // Время до автоматического скрытия чата после получения сообщения
    [SerializeField] private Color allChatColor = Color.white;
    [SerializeField] private Color partyChatColor = Color.green;

    // Singleton
    public static ChatManager Instance { get; private set; }

    // Chat state
    private ChatChannel currentChannel = ChatChannel.All;
    private List<ChatMessage> allMessages = new List<ChatMessage>();
    private List<ChatMessage> partyMessages = new List<ChatMessage>();
    private Queue<GameObject> chatMessageObjects = new Queue<GameObject>();

    // Chat bubble reference
    private ChatBubbleManager chatBubbleManager;

    // Auto-hide state
    private float lastMessageTime;
    private bool autoHideEnabled = true;

    public enum ChatChannel
    {
        All,
        Party
    }

    [System.Serializable]
    public class ChatMessage
    {
        public string username;
        public string message;
        public string channel; // Изменено с ChatChannel на string для совместимости с JSON
        public string socketId;
        public long timestamp;

        // Вспомогательное свойство для получения канала как enum
        public ChatChannel GetChannel()
        {
            if (channel == "party")
                return ChatChannel.Party;
            return ChatChannel.All;
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Добавляем CanvasGroup если его нет
        if (chatCanvasGroup == null && chatPanel != null)
        {
            chatCanvasGroup = chatPanel.GetComponent<CanvasGroup>();
            if (chatCanvasGroup == null)
            {
                chatCanvasGroup = chatPanel.AddComponent<CanvasGroup>();
                Debug.Log("[ChatManager] ✅ CanvasGroup добавлен к ChatPanel");
            }
        }

        // Проверка ссылок
        Debug.Log($"[ChatManager] 🔍 Проверка ссылок:");
        Debug.Log($"[ChatManager]   chatPanel: {(chatPanel != null ? "✅" : "❌ NULL")}");
        Debug.Log($"[ChatManager]   chatCanvasGroup: {(chatCanvasGroup != null ? "✅" : "❌ NULL")}");
        Debug.Log($"[ChatManager]   messageInputField: {(messageInputField != null ? "✅" : "❌ NULL")}");
        Debug.Log($"[ChatManager]   sendButton: {(sendButton != null ? "✅" : "❌ NULL")}");
        Debug.Log($"[ChatManager]   openChatButton: {(openChatButton != null ? "✅" : "❌ NULL (опционально для Mobile)")}");
        Debug.Log($"[ChatManager]   chatPanel активен: {(chatPanel != null ? chatPanel.activeSelf.ToString() : "N/A")}");

        // Инициализация UI
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(SendMessage);
        }

        if (openChatButton != null)
        {
            openChatButton.onClick.AddListener(OnOpenChatButtonClicked);
            Debug.Log("[ChatManager] ✅ OpenChatButton подключен");
        }

        if (allChatTab != null)
        {
            allChatTab.onClick.AddListener(() => SwitchChannel(ChatChannel.All));
        }

        if (partyChatTab != null)
        {
            partyChatTab.onClick.AddListener(() => SwitchChannel(ChatChannel.Party));
        }

        if (messageInputField != null)
        {
            messageInputField.onSubmit.AddListener((text) => SendMessage());
        }

        // Инициализация ChatBubbleManager
        chatBubbleManager = FindFirstObjectByType<ChatBubbleManager>();
        if (chatBubbleManager == null)
        {
            Debug.LogWarning("[ChatManager] ChatBubbleManager не найден! Всплывающие сообщения не будут работать.");
        }

        // Регистрация обработчиков сетевых событий
        RegisterNetworkEvents();

        // Устанавливаем начальный канал
        SwitchChannel(ChatChannel.All);

        // Чат уже скрыт по умолчанию (alpha = 0 при создании через ChatSystemSetup)
        // Проверяем текущее состояние
        bool isChatVisible = chatCanvasGroup != null && chatCanvasGroup.alpha > 0.5f;
        Debug.Log($"[ChatManager] ✅ Инициализирован (чат {(isChatVisible ? "ВИДЕН" : "СКРЫТ")})");
    }

    void Update()
    {
        // Открытие/закрытие чата по Enter (только на PC)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Debug.Log($"[ChatManager] ⌨️ Enter нажат! messageInputField null: {messageInputField == null}, focused: {(messageInputField != null ? messageInputField.isFocused.ToString() : "N/A")}");

            if (messageInputField != null && !messageInputField.isFocused)
            {
                OpenChatManually();
            }
        }

        // Закрытие чата по Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (messageInputField != null && messageInputField.isFocused)
            {
                messageInputField.DeactivateInputField();
                // Скрываем чат после закрытия
                ToggleChatPanel(false);
                autoHideEnabled = true; // Включаем обратно автоскрытие
            }
        }

        // Автоматическое скрытие чата после получения сообщения
        bool isChatVisible = chatCanvasGroup != null ? chatCanvasGroup.alpha > 0.5f : (chatPanel != null && chatPanel.activeSelf);
        if (autoHideEnabled && isChatVisible && Time.time - lastMessageTime > autoHideDelay)
        {
            Debug.Log("[ChatManager] ⏰ Автоскрытие чата (время вышло)");
            ToggleChatPanel(false);
        }
    }

    /// <summary>
    /// Регистрация обработчиков сетевых событий чата
    /// </summary>
    private void RegisterNetworkEvents()
    {
        if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
        {
            SocketIOManager.Instance.On("chat_message", OnChatMessageReceived);
            Debug.Log("[ChatManager] 🌐 Обработчик chat_message зарегистрирован");
        }
        else
        {
            if (SocketIOManager.Instance == null)
            {
                Debug.LogWarning("[ChatManager] ⚠️ SocketIOManager не найден! Регистрация отложена.");
            }
            else
            {
                Debug.LogWarning("[ChatManager] ⚠️ SocketIOManager не подключен! Регистрация отложена.");
            }
            Invoke(nameof(RegisterNetworkEvents), 1f);
        }
    }

    /// <summary>
    /// Переключение канала чата (All / Party)
    /// </summary>
    public void SwitchChannel(ChatChannel channel)
    {
        currentChannel = channel;

        // Обновляем подсветку вкладок
        if (allChatTabHighlight != null)
        {
            allChatTabHighlight.SetActive(channel == ChatChannel.All);
        }

        if (partyChatTabHighlight != null)
        {
            partyChatTabHighlight.SetActive(channel == ChatChannel.Party);
        }

        // Обновляем отображение сообщений
        RefreshChatDisplay();

        Debug.Log($"[ChatManager] 📢 Переключено на канал: {channel}");
    }

    /// <summary>
    /// Отправка сообщения в чат
    /// </summary>
    public void SendMessage()
    {
        if (messageInputField == null || string.IsNullOrWhiteSpace(messageInputField.text))
        {
            return;
        }

        string message = messageInputField.text.Trim();
        messageInputField.text = "";

        // Проверка подключения
        if (SocketIOManager.Instance == null)
        {
            Debug.LogWarning("[ChatManager] ⚠️ SocketIOManager не найден!");
            AddSystemMessage("Ошибка: SocketIOManager не найден. Перезапустите игру.");
            return;
        }

        if (!SocketIOManager.Instance.IsConnected)
        {
            Debug.LogWarning("[ChatManager] ⚠️ Не подключен к серверу! Подключитесь к серверу перед отправкой сообщений.");
            AddSystemMessage("Ошибка: не подключен к серверу. Подключитесь к серверу в главном меню.");
            return;
        }

        // Проверка для Party чата - нужно быть в группе
        if (currentChannel == ChatChannel.Party)
        {
            if (PartyManager.Instance == null || !PartyManager.Instance.IsInParty)
            {
                AddSystemMessage("Ошибка: вы не состоите в группе");
                return;
            }
        }

        // Получаем имя игрока из PlayerPrefs (сохраняется при логине)
        string username = PlayerPrefs.GetString("SavedUsername", "Unknown");

        // Отправляем сообщение на сервер
        var data = new
        {
            message = message,
            channel = currentChannel.ToString().ToLower(),
            username = username
        };

        string json = JsonConvert.SerializeObject(data);
        SocketIOManager.Instance.Emit("chat_message", json);

        Debug.Log($"[ChatManager] 📤 Отправлено сообщение [{currentChannel}]: {message}");

        // Деактивируем поле ввода и скрываем чат
        if (messageInputField != null)
        {
            messageInputField.DeactivateInputField();
        }

        // Скрываем чат после отправки и включаем автоскрытие
        ToggleChatPanel(false);
        autoHideEnabled = true;
    }

    /// <summary>
    /// Обработка получения сообщения из сети
    /// </summary>
    private void OnChatMessageReceived(string jsonData)
    {
        try
        {
            Debug.Log($"[ChatManager] 📥 Получено сообщение: {jsonData}");

            var data = JsonConvert.DeserializeObject<ChatMessage>(jsonData);

            if (data == null)
            {
                Debug.LogWarning("[ChatManager] ⚠️ Не удалось десериализовать сообщение");
                return;
            }

            // Получаем канал как enum
            ChatChannel messageChannel = data.GetChannel();

            // Добавляем сообщение в соответствующий список
            if (messageChannel == ChatChannel.All)
            {
                allMessages.Add(data);
            }
            else if (messageChannel == ChatChannel.Party)
            {
                partyMessages.Add(data);
            }

            // Обновляем отображение если мы на нужном канале
            if (messageChannel == currentChannel)
            {
                AddMessageToUI(data);
            }

            // Показываем сообщение над головой игрока
            ShowChatBubble(data);

            // Показываем чат при получении нового сообщения (если он скрыт)
            bool isChatVisible = chatCanvasGroup != null ? chatCanvasGroup.alpha > 0.5f : (chatPanel != null && chatPanel.activeSelf);
            if (!isChatVisible)
            {
                Debug.Log("[ChatManager] 📬 Новое сообщение - показываем чат");
                ToggleChatPanel(true);
                autoHideEnabled = true; // Включаем автоскрытие
            }

            // Обновляем время последнего сообщения для автоскрытия
            lastMessageTime = Time.time;

            Debug.Log($"[ChatManager] ✅ Сообщение добавлено [{data.channel}]: {data.username}: {data.message}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ChatManager] ❌ Ошибка обработки сообщения: {e.Message}");
        }
    }

    /// <summary>
    /// Добавление сообщения в UI
    /// </summary>
    private void AddMessageToUI(ChatMessage message)
    {
        if (chatMessagePrefab == null || chatContentContainer == null)
        {
            Debug.LogWarning("[ChatManager] ⚠️ chatMessagePrefab или chatContentContainer не назначены");
            return;
        }

        // Создаем новый объект сообщения
        GameObject messageObj = Instantiate(chatMessagePrefab, chatContentContainer);

        // Настраиваем текст
        TMP_Text messageText = messageObj.GetComponentInChildren<TMP_Text>();
        if (messageText != null)
        {
            ChatChannel messageChannel = message.GetChannel();
            Color messageColor = messageChannel == ChatChannel.Party ? partyChatColor : allChatColor;
            string channelPrefix = messageChannel == ChatChannel.Party ? "[Party] " : "";
            messageText.text = $"{channelPrefix}<color=#{ColorUtility.ToHtmlStringRGB(messageColor)}>{message.username}</color>: {message.message}";
            messageText.color = allChatColor; // Базовый цвет для остального текста
        }

        // Добавляем в очередь для отслеживания
        chatMessageObjects.Enqueue(messageObj);

        // Удаляем старые сообщения если превышен лимит
        while (chatMessageObjects.Count > maxMessagesInChat)
        {
            GameObject oldMessage = chatMessageObjects.Dequeue();
            Destroy(oldMessage);
        }

        // Прокручиваем вниз
        Canvas.ForceUpdateCanvases();
        if (chatScrollRect != null)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// Показать всплывающее сообщение над головой игрока
    /// </summary>
    private void ShowChatBubble(ChatMessage message)
    {
        if (chatBubbleManager == null)
        {
            return;
        }

        // Находим игрока по socketId
        Transform playerTransform = null;

        // Проверяем локального игрока
        if (SocketIOManager.Instance != null && message.socketId == SocketIOManager.Instance.GetSocketId())
        {
            // Это наше сообщение - находим локального игрока
            GameObject localPlayer = GameObject.FindGameObjectWithTag("Player");
            if (localPlayer != null)
            {
                playerTransform = localPlayer.transform;
            }
        }
        else
        {
            // Ищем NetworkPlayer по socketId
            NetworkPlayer[] networkPlayers = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            foreach (NetworkPlayer np in networkPlayers)
            {
                if (np.socketId == message.socketId)
                {
                    playerTransform = np.transform;
                    break;
                }
            }
        }

        if (playerTransform != null)
        {
            chatBubbleManager.ShowChatBubble(playerTransform, message.message, messageDisplayTime);
        }
        else
        {
            Debug.LogWarning($"[ChatManager] ⚠️ Не найден игрок для socketId: {message.socketId}");
        }
    }

    /// <summary>
    /// Добавить системное сообщение (локально)
    /// </summary>
    public void AddSystemMessage(string message)
    {
        if (chatMessagePrefab == null || chatContentContainer == null)
        {
            return;
        }

        GameObject messageObj = Instantiate(chatMessagePrefab, chatContentContainer);
        TMP_Text messageText = messageObj.GetComponentInChildren<TMP_Text>();
        if (messageText != null)
        {
            messageText.text = $"<color=yellow>[System]</color> {message}";
            messageText.color = Color.yellow;
        }

        chatMessageObjects.Enqueue(messageObj);

        while (chatMessageObjects.Count > maxMessagesInChat)
        {
            GameObject oldMessage = chatMessageObjects.Dequeue();
            Destroy(oldMessage);
        }

        Canvas.ForceUpdateCanvases();
        if (chatScrollRect != null)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// Обновить отображение чата (при переключении вкладок)
    /// </summary>
    private void RefreshChatDisplay()
    {
        // Очищаем текущие сообщения
        foreach (GameObject obj in chatMessageObjects)
        {
            Destroy(obj);
        }
        chatMessageObjects.Clear();

        // Получаем сообщения нужного канала
        List<ChatMessage> messages = currentChannel == ChatChannel.All ? allMessages : partyMessages;

        // Отображаем последние N сообщений
        int startIndex = Mathf.Max(0, messages.Count - maxMessagesInChat);
        for (int i = startIndex; i < messages.Count; i++)
        {
            AddMessageToUI(messages[i]);
        }
    }

    /// <summary>
    /// Открыть чат вручную (по кнопке или Enter)
    /// </summary>
    private void OpenChatManually()
    {
        Debug.Log("[ChatManager] 📂 Открываем чат принудительно...");
        // Открываем чат и фокусируемся на поле ввода
        autoHideEnabled = false; // Отключаем автоскрытие при ручном открытии
        ToggleChatPanel(true);
        if (messageInputField != null)
        {
            messageInputField.ActivateInputField();
        }
    }

    /// <summary>
    /// Обработчик нажатия на кнопку открытия чата (для Mobile/Android)
    /// </summary>
    public void OnOpenChatButtonClicked()
    {
        Debug.Log("[ChatManager] 📱 Кнопка открытия чата нажата (Mobile)");
        OpenChatManually();
    }

    /// <summary>
    /// Показать/скрыть панель чата (используя CanvasGroup вместо SetActive)
    /// </summary>
    public void ToggleChatPanel(bool show)
    {
        Debug.Log($"[ChatManager] 🔄 ToggleChatPanel({show}) вызван. chatCanvasGroup: {(chatCanvasGroup != null ? "существует" : "NULL")}");

        if (chatCanvasGroup != null)
        {
            chatCanvasGroup.alpha = show ? 1f : 0f; // Прозрачность (0 = невидимо, 1 = видимо)
            chatCanvasGroup.interactable = show; // Можно ли взаимодействовать
            chatCanvasGroup.blocksRaycasts = show; // Блокирует ли клики мыши
            Debug.Log($"[ChatManager] ✅ CanvasGroup.alpha = {chatCanvasGroup.alpha}, visible = {show}");
        }
        else if (chatPanel != null)
        {
            // Fallback: используем SetActive если CanvasGroup нет
            chatPanel.SetActive(show);
            Debug.Log($"[ChatManager] ⚠️ Используем SetActive (нет CanvasGroup). Текущее состояние: {chatPanel.activeSelf}");
        }
        else
        {
            Debug.LogError("[ChatManager] ❌ chatPanel и chatCanvasGroup == NULL! Ссылки не назначены!");
        }
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        // Примечание: SocketIOManager не имеет метода Off, используем On с null
        // или просто не отписываемся (SocketIOManager - singleton, живет всю сессию)
    }
}
