using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI для отображения членов группы
/// Показывает:
/// - Имя, класс, уровень каждого члена
/// - HP и MP (синхронизируются в реальном времени)
/// - Кнопка "Покинуть группу" для каждого члена (только себя можно кикнуть)
/// </summary>
public class PartyUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject partyPanel; // Весь UI группы
    [SerializeField] private Transform membersContainer; // Контейнер для PartyMemberSlot
    [SerializeField] private GameObject memberSlotPrefab; // Префаб PartyMemberSlot
    [SerializeField] private TextMeshProUGUI partyTitleText; // "Группа (2/5)"
    [SerializeField] private Button closeButton; // Кнопка закрытия панели

    [Header("Settings")]
    [SerializeField] private Color localPlayerColor = Color.cyan; // Цвет для своего имени
    [SerializeField] private Color remotePlayerColor = Color.white; // Цвет для других игроков

    // Хранилище слотов
    private Dictionary<string, PartyMemberSlot> memberSlots = new Dictionary<string, PartyMemberSlot>();

    void Start()
    {
        SetupUI();
        SubscribeToEvents();

        // По умолчанию панель скрыта
        HidePanel();
    }

    void Update()
    {
        // Hotkey "P" для переключения панели группы
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePanel();
        }
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
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
        }
    }

    /// <summary>
    /// Подписаться на события PartyManager
    /// </summary>
    private void SubscribeToEvents()
    {
        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.OnPartyCreated += OnPartyCreated;
            PartyManager.Instance.OnPartyJoined += OnPartyJoined;
            PartyManager.Instance.OnMemberJoined += OnMemberJoined;
            PartyManager.Instance.OnMemberLeft += OnMemberLeft;
            PartyManager.Instance.OnPartyLeft += OnPartyLeft;
            PartyManager.Instance.OnMemberStatsUpdated += OnMemberStatsUpdated;
        }
    }

    /// <summary>
    /// Отписаться от событий
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.OnPartyCreated -= OnPartyCreated;
            PartyManager.Instance.OnPartyJoined -= OnPartyJoined;
            PartyManager.Instance.OnMemberJoined -= OnMemberJoined;
            PartyManager.Instance.OnMemberLeft -= OnMemberLeft;
            PartyManager.Instance.OnPartyLeft -= OnPartyLeft;
            PartyManager.Instance.OnMemberStatsUpdated -= OnMemberStatsUpdated;
        }
    }

    // ═══════════════════════════════════════════
    // PUBLIC API
    // ═══════════════════════════════════════════

    /// <summary>
    /// Показать панель группы
    /// </summary>
    public void ShowPanel()
    {
        if (partyPanel != null)
        {
            partyPanel.SetActive(true);
        }

        RefreshUI();
    }

    /// <summary>
    /// Скрыть панель группы
    /// </summary>
    public void HidePanel()
    {
        if (partyPanel != null)
        {
            partyPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Переключить видимость панели
    /// </summary>
    public void TogglePanel()
    {
        if (partyPanel != null)
        {
            if (partyPanel.activeSelf)
            {
                HidePanel();
            }
            else
            {
                ShowPanel();
            }
        }
    }

    /// <summary>
    /// Обновить весь UI группы
    /// </summary>
    public void RefreshUI()
    {
        if (PartyManager.Instance == null || !PartyManager.Instance.IsInParty)
        {
            HidePanel();
            return;
        }

        // Обновляем заголовок
        UpdateTitle();

        // Удаляем старые слоты
        ClearSlots();

        // Создаём слоты для всех членов
        List<PartyMember> members = PartyManager.Instance.PartyMembers;
        string mySocketId = SocketIOManager.Instance != null ? SocketIOManager.Instance.SocketId : "";

        foreach (var member in members)
        {
            CreateMemberSlot(member, member.socketId == mySocketId);
        }

        Debug.Log($"[PartyUI] 🔄 UI обновлён, членов: {members.Count}");
    }

    // ═══════════════════════════════════════════
    // PRIVATE METHODS
    // ═══════════════════════════════════════════

    /// <summary>
    /// Обновить заголовок "Группа (2/5)"
    /// </summary>
    private void UpdateTitle()
    {
        if (partyTitleText != null && PartyManager.Instance != null)
        {
            int current = PartyManager.Instance.PartySize;
            int max = PartyManager.Instance.MaxPartySize;
            partyTitleText.text = $"Группа ({current}/{max})";
        }
    }

    /// <summary>
    /// Создать слот члена группы
    /// </summary>
    private void CreateMemberSlot(PartyMember member, bool isLocalPlayer)
    {
        if (memberSlotPrefab == null || membersContainer == null)
        {
            Debug.LogError("[PartyUI] ❌ memberSlotPrefab или membersContainer не назначены!");
            return;
        }

        GameObject slotGO = Instantiate(memberSlotPrefab, membersContainer);
        PartyMemberSlot slot = slotGO.GetComponent<PartyMemberSlot>();

        if (slot != null)
        {
            slot.Initialize(member, isLocalPlayer, OnLeaveButtonClicked);
            memberSlots[member.socketId] = slot;

            Debug.Log($"[PartyUI] ✅ Создан слот для {member.username}");
        }
    }

    /// <summary>
    /// Удалить все слоты
    /// </summary>
    private void ClearSlots()
    {
        foreach (var slot in memberSlots.Values)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }

        memberSlots.Clear();
    }

    /// <summary>
    /// Callback кнопки "Покинуть группу"
    /// </summary>
    private void OnLeaveButtonClicked(string socketId)
    {
        Debug.Log($"[PartyUI] 👋 Нажата кнопка выхода для {socketId}");

        // Можно покинуть только себя
        string mySocketId = SocketIOManager.Instance != null ? SocketIOManager.Instance.SocketId : "";
        if (socketId == mySocketId)
        {
            if (PartyManager.Instance != null)
            {
                PartyManager.Instance.LeaveParty();
            }
        }
    }

    // ═══════════════════════════════════════════
    // EVENT HANDLERS
    // ═══════════════════════════════════════════

    private void OnPartyCreated(string partyId)
    {
        Debug.Log($"[PartyUI] 🎉 Группа создана: {partyId}");
        ShowPanel();
    }

    private void OnPartyJoined(string partyId)
    {
        Debug.Log($"[PartyUI] 🎊 Вступили в группу: {partyId}");
        ShowPanel();
    }

    private void OnMemberJoined(PartyMember member)
    {
        Debug.Log($"[PartyUI] ➕ Новый член: {member.username}");
        RefreshUI();
    }

    private void OnMemberLeft(string socketId)
    {
        Debug.Log($"[PartyUI] ➖ Член вышел: {socketId}");
        RefreshUI();
    }

    private void OnPartyLeft()
    {
        Debug.Log("[PartyUI] 👋 Вышли из группы");
        HidePanel();
        ClearSlots();
    }

    private void OnMemberStatsUpdated(string socketId, PartyMemberStats stats)
    {
        Debug.Log($"[PartyUI] 📊 Обновление статов: {socketId}");

        // Обновляем слот
        if (memberSlots.ContainsKey(socketId))
        {
            PartyMemberSlot slot = memberSlots[socketId];
            if (slot != null)
            {
                slot.UpdateStats(stats.health, stats.mana, stats.maxHealth, stats.maxMana);
            }
        }
    }
}
