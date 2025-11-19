using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI для приглашений в группу
/// Появляется когда другой игрок приглашает в группу
/// Показывает:
/// - Имя приглашающего, класс, уровень
/// - Кнопки "Принять" и "Отклонить"
/// </summary>
public class PartyInviteUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject invitePanel; // Панель приглашения
    [SerializeField] private TextMeshProUGUI inviterNameText; // "Player приглашает вас в группу"
    [SerializeField] private TextMeshProUGUI inviterDetailsText; // "Воин, Ур. 5"
    [SerializeField] private Button acceptButton; // Кнопка "Принять"
    [SerializeField] private Button declineButton; // Кнопка "Отклонить"

    [Header("Settings")]
    [SerializeField] private float autoDeclineDelay = 30f; // Автоматический отказ через 30 секунд

    // Текущее приглашение
    private PartyInvite currentInvite = null;
    private float inviteTimer = 0f;
    private bool isWaitingForResponse = false;

    void Start()
    {
        Debug.Log("[PartyInviteUI] 🚀 Start() вызван");
        SetupUI();
        SubscribeToEvents();

        // По умолчанию панель скрыта
        HideInvite();
        Debug.Log("[PartyInviteUI] ✅ Инициализация завершена");
    }

    void Update()
    {
        // Автоотказ по таймеру
        if (isWaitingForResponse)
        {
            inviteTimer -= Time.deltaTime;
            if (inviteTimer <= 0)
            {
                Debug.Log("[PartyInviteUI] ⏰ Время вышло, автоматический отказ");
                DeclineInvite();
            }
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
        if (acceptButton != null)
        {
            acceptButton.onClick.AddListener(AcceptInvite);
        }

        if (declineButton != null)
        {
            declineButton.onClick.AddListener(DeclineInvite);
        }
    }

    /// <summary>
    /// Подписаться на события PartyManager
    /// </summary>
    private void SubscribeToEvents()
    {
        Debug.Log($"[PartyInviteUI] 🔌 Подписываемся на события... PartyManager.Instance={PartyManager.Instance != null}");

        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.OnInviteReceived += OnInviteReceived;
            Debug.Log("[PartyInviteUI] ✅ Подписались на OnInviteReceived");
        }
        else
        {
            Debug.LogError("[PartyInviteUI] ❌ PartyManager.Instance is null! Не можем подписаться на события.");
        }
    }

    /// <summary>
    /// Отписаться от событий
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.OnInviteReceived -= OnInviteReceived;
        }
    }

    // ═══════════════════════════════════════════
    // PUBLIC API
    // ═══════════════════════════════════════════

    /// <summary>
    /// Показать приглашение
    /// </summary>
    public void ShowInvite(PartyInvite invite)
    {
        if (invite == null)
        {
            Debug.LogError("[PartyInviteUI] ❌ invite is null!");
            return;
        }

        currentInvite = invite;
        isWaitingForResponse = true;
        inviteTimer = autoDeclineDelay;

        // Обновляем текст
        if (inviterNameText != null)
        {
            inviterNameText.text = $"{invite.inviterUsername} приглашает вас в группу";
        }

        if (inviterDetailsText != null)
        {
            inviterDetailsText.text = $"{invite.inviterClass}, Ур. {invite.inviterLevel}";
        }

        // Показываем панель
        if (invitePanel != null)
        {
            invitePanel.SetActive(true);
        }

        Debug.Log($"[PartyInviteUI] 📨 Показано приглашение от {invite.inviterUsername}");
    }

    /// <summary>
    /// Скрыть приглашение
    /// </summary>
    public void HideInvite()
    {
        if (invitePanel != null)
        {
            invitePanel.SetActive(false);
        }

        currentInvite = null;
        isWaitingForResponse = false;
        inviteTimer = 0f;
    }

    /// <summary>
    /// Принять приглашение
    /// </summary>
    public void AcceptInvite()
    {
        if (currentInvite == null)
        {
            Debug.LogWarning("[PartyInviteUI] ❌ Нет активного приглашения!");
            return;
        }

        Debug.Log($"[PartyInviteUI] ✅ Принимаем приглашение от {currentInvite.inviterUsername}");

        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.AcceptInvite(currentInvite.partyId, currentInvite.inviterSocketId);
        }

        HideInvite();
    }

    /// <summary>
    /// Отклонить приглашение
    /// </summary>
    public void DeclineInvite()
    {
        if (currentInvite == null)
        {
            Debug.LogWarning("[PartyInviteUI] ❌ Нет активного приглашения!");
            return;
        }

        Debug.Log($"[PartyInviteUI] ❌ Отклоняем приглашение от {currentInvite.inviterUsername}");

        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.DeclineInvite(currentInvite.partyId, currentInvite.inviterSocketId);
        }

        HideInvite();
    }

    // ═══════════════════════════════════════════
    // EVENT HANDLERS
    // ═══════════════════════════════════════════

    private void OnInviteReceived(PartyInvite invite)
    {
        Debug.Log($"[PartyInviteUI] 📬 📬 📬 OnInviteReceived ВЫЗВАН! Приглашение от {invite.inviterUsername}");

        // Если уже в группе - автоматически отклоняем
        if (PartyManager.Instance != null && PartyManager.Instance.IsInParty)
        {
            Debug.Log("[PartyInviteUI] ⚠️ Уже в группе, автоматический отказ");
            PartyManager.Instance.DeclineInvite(invite.partyId, invite.inviterSocketId);
            return;
        }

        // Показываем приглашение
        Debug.Log("[PartyInviteUI] 🎯 Вызываем ShowInvite()...");
        ShowInvite(invite);
    }
}
