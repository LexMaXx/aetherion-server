using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Управляет всем UI в BattleScene
/// Включает: HP/MP/AP бары, скиллы, джойстик, кнопка атаки
/// </summary>
public class BattleSceneUIManager : MonoBehaviour
{
    [Header("Main Canvas")]
    public Canvas mainCanvas;

    [Header("Player Stats Panel (Top Left)")]
    public RectTransform statsPanel;
    public Image hpFillImage;
    public Image mpFillImage;
    public Image apFillImage;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI mpText;
    public TextMeshProUGUI apText;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI levelText;

    [Header("Skill Bar Panel (Bottom Center)")]
    public RectTransform skillBarPanel;
    public BattleSkillSlot[] skillSlots = new BattleSkillSlot[5];

    [Header("Action Points Panel (Below Skill Bar)")]
    public ActionPointsUI actionPointsUI;

    [Header("Mobile Controls (Bottom)")]
    public RectTransform mobileControlsPanel;
    public RectTransform joystickPanel;
    public Image joystickBackground;
    public Image joystickHandle;
    public Button attackButton;
    public Image attackButtonImage;
    public TextMeshProUGUI attackCooldownText;
    public Button partyButton; // Кнопка "Party" для приглашения в группу

    [Header("Target Panel (Top Center)")]
    public RectTransform targetPanel;
    public Image targetHpFillImage;
    public TextMeshProUGUI targetNameText;
    public TextMeshProUGUI targetHpText;

    [Header("Components")]
    private HealthSystem healthSystem;
    private ManaSystem manaSystem;
    private ActionPointSystem actionPointSystem; // Новая система (для бара)
    private ActionPointsSystem actionPointsSystem; // Старая система (для шариков)
    private SkillExecutor skillExecutor;
    private TargetSystem targetSystem;
    private BattleMobileInput mobileInput; // Связь джойстика с PlayerController

    [Header("Settings")]
    public bool showMobileControls = true;
    public bool enableDebugLogs = true;

    private void Awake()
    {
        // Скрываем target panel по умолчанию
        if (targetPanel != null)
            targetPanel.gameObject.SetActive(false);

        // Показываем/скрываем мобильные контролы
        if (mobileControlsPanel != null)
            mobileControlsPanel.gameObject.SetActive(showMobileControls);
    }

    /// <summary>
    /// Инициализация UI для локального игрока
    /// Вызывается из BattleSceneManager после спавна персонажа
    /// </summary>
    public void Initialize(GameObject player)
    {
        if (player == null)
        {
            Debug.LogError("[BattleSceneUI] ❌ Player is null!");
            return;
        }

        Log("Initializing UI for player: " + player.name);

        // Получаем компоненты с игрока
        healthSystem = player.GetComponentInChildren<HealthSystem>();
        manaSystem = player.GetComponentInChildren<ManaSystem>();
        actionPointSystem = player.GetComponentInChildren<ActionPointSystem>();
        actionPointsSystem = player.GetComponentInChildren<ActionPointsSystem>();
        skillExecutor = player.GetComponent<SkillExecutor>();
        targetSystem = player.GetComponent<TargetSystem>();

        if (healthSystem == null)
        {
            Debug.LogWarning("[BattleSceneUI] ⚠️ HealthSystem not found on player!");
        }

        if (manaSystem == null)
        {
            Debug.LogWarning("[BattleSceneUI] ⚠️ ManaSystem not found on player!");
        }

        if (actionPointSystem == null)
        {
            // Создаем новую систему если нет (для AP бара)
            actionPointSystem = player.AddComponent<ActionPointSystem>();
            Log("✅ Created ActionPointSystem (bar)");
        }

        if (actionPointsSystem == null)
        {
            // Создаем старую систему для шариков если нет
            actionPointsSystem = player.AddComponent<ActionPointsSystem>();
            Log("✅ Created ActionPointsSystem (balls)");
        }

        if (skillExecutor == null)
        {
            Debug.LogWarning("[BattleSceneUI] ⚠️ SkillExecutor not found on player!");
        }

        if (targetSystem == null)
        {
            Debug.LogWarning("[BattleSceneUI] ⚠️ TargetSystem not found on player!");
        }
        else
        {
            // Подписываемся на изменение цели
            targetSystem.OnTargetChanged += OnTargetChanged;
        }

        // Инициализируем skill slots
        InitializeSkillSlots();

        // КРИТИЧЕСКОЕ: Инициализируем мобильный джойстик ПЕРЕД ActionPointsUI
        // ActionPointsUI может вызвать ошибку и прервать Initialize()
        InitializeMobileInput(player);

        // Инициализируем кнопку атаки
        InitializeAttackButton(player);

        // Инициализируем Action Points UI (шарики) - ПОСЛЕ джойстика!
        InitializeActionPointsUI();

        // Устанавливаем имя и уровень игрока
        SetupPlayerInfo(player);

        // Первичное обновление UI
        UpdateAllUI();

        Log("✅ BattleSceneUI initialized");
    }

    private void SetupPlayerInfo(GameObject player)
    {
        // Получаем имя игрока (используем название класса)
        string playerName = "Player";

        CharacterStats stats = player.GetComponent<CharacterStats>();
        if (stats != null)
        {
            playerName = stats.ClassName;
        }

        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }

        // Уровень пока не используется (в CharacterStats нет поля level)
        if (levelText != null)
        {
            levelText.text = "Lv 1"; // Временно статичное значение
        }
    }

    private void InitializeSkillSlots()
    {
        if (skillExecutor == null)
        {
            Log("⚠️ Cannot initialize skill slots - SkillExecutor is null");
            return;
        }

        for (int i = 0; i < skillSlots.Length; i++)
        {
            if (skillSlots[i] != null)
            {
                int slotIndex = i;
                skillSlots[i].Initialize(slotIndex, skillExecutor);
                Log($"✅ Skill slot {i} initialized");
            }
        }
    }

    private void InitializeActionPointsUI()
    {
        if (actionPointsUI == null)
        {
            Log("⚠️ ActionPointsUI not assigned in UI Manager");
            return;
        }

        if (actionPointsSystem == null)
        {
            Log("⚠️ ActionPointsSystem not found on player");
            return;
        }

        // Инициализируем UI с системой
        actionPointsUI.Initialize(actionPointsSystem);
        Log("✅ Action Points UI initialized (10 balls)");
    }

    /// <summary>
    /// Инициализация мобильного джойстика
    /// </summary>
    private void InitializeMobileInput(GameObject player)
    {
        Debug.Log("[BattleSceneUI] 🔧 InitializeMobileInput started...");

        if (joystickPanel == null)
        {
            Debug.LogError("[BattleSceneUI] ❌ Joystick panel not found!");
            return;
        }

        Debug.Log($"[BattleSceneUI] ✅ Joystick panel found: {joystickPanel.name}");

        // ИСПРАВЛЕНИЕ: VirtualJoystick может быть на самом joystickPanel или на дочернем объекте
        VirtualJoystick virtualJoystick = joystickPanel.GetComponent<VirtualJoystick>();

        // Если не нашли на joystickPanel, ищем в дочерних объектах
        if (virtualJoystick == null)
        {
            virtualJoystick = joystickPanel.GetComponentInChildren<VirtualJoystick>();
            Debug.Log($"[BattleSceneUI] ℹ️ Поиск VirtualJoystick в дочерних объектах...");
        }

        if (virtualJoystick == null)
        {
            Debug.LogError("[BattleSceneUI] ❌ VirtualJoystick component not found! Проверь что VirtualJoystick добавлен на Joystick GameObject");
            Debug.LogError($"[BattleSceneUI] ❌ Проверял: {joystickPanel.name} и его дочерние объекты");
            return;
        }

        Debug.Log($"[BattleSceneUI] ✅ VirtualJoystick найден на: {virtualJoystick.gameObject.name}");

        // Создаём BattleMobileInput если нет
        mobileInput = gameObject.GetComponent<BattleMobileInput>();
        if (mobileInput == null)
        {
            mobileInput = gameObject.AddComponent<BattleMobileInput>();
            Debug.Log("[BattleSceneUI] ✅ BattleMobileInput создан");
        }
        else
        {
            Debug.Log("[BattleSceneUI] ✅ BattleMobileInput уже существует");
        }

        Debug.Log($"[BattleSceneUI] 🎯 Вызываем mobileInput.Initialize() для {player.name}...");

        // Инициализируем связь джойстика с игроком
        mobileInput.Initialize(virtualJoystick, player);

        Debug.Log("[BattleSceneUI] ✅ Mobile joystick initialized and linked to player");
    }

    /// <summary>
    /// Инициализация кнопки атаки
    /// </summary>
    private void InitializeAttackButton(GameObject player)
    {
        if (attackButton == null)
        {
            Log("⚠️ Attack button not found");
            return;
        }

        // Получаем PlayerAttackNew компонент
        PlayerAttackNew playerAttack = player.GetComponent<PlayerAttackNew>();
        if (playerAttack == null)
        {
            Log("❌ PlayerAttackNew component not found on player!");
            return;
        }

        // Привязываем кнопку к методу атаки
        attackButton.onClick.RemoveAllListeners(); // Удаляем старые слушатели
        attackButton.onClick.AddListener(() => {
            // Вызываем TryAttack через рефлексию (метод private)
            var method = typeof(PlayerAttackNew).GetMethod("TryAttack",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(playerAttack, null);
                Log("🗡️ Attack button pressed");
            }
        });

        Log("✅ Attack button initialized and linked to PlayerAttackNew");

        // Инициализируем кнопку Party
        InitializePartyButton();
    }

    /// <summary>
    /// Инициализация кнопки Party
    /// </summary>
    private void InitializePartyButton()
    {
        if (partyButton == null)
        {
            Debug.LogWarning("[BattleSceneUI] ⚠️ Party button not assigned!");
            return;
        }

        partyButton.onClick.RemoveAllListeners();
        partyButton.onClick.AddListener(OnPartyButtonClicked);

        Log("✅ Party button initialized");
    }

    /// <summary>
    /// Обработка нажатия кнопки Party
    /// Приглашает выбранного в таргет игрока в группу
    /// </summary>
    private void OnPartyButtonClicked()
    {
        Log("🎉 Party button clicked");

        // Проверяем что PartyManager существует
        if (PartyManager.Instance == null)
        {
            Debug.LogError("[BattleSceneUI] ❌ PartyManager not found!");
            return;
        }

        // Проверяем что TargetSystem существует и есть таргет
        if (targetSystem == null)
        {
            Debug.LogWarning("[BattleSceneUI] ⚠️ TargetSystem not found!");
            return;
        }

        TargetableEntity currentTarget = targetSystem.GetCurrentTarget();
        if (currentTarget == null)
        {
            Debug.LogWarning("[BattleSceneUI] ⚠️ Нет выбранной цели! Выберите игрока для приглашения в группу.");
            return;
        }

        // Проверяем что это игрок (не моб)
        NetworkPlayer networkPlayer = currentTarget.GetComponent<NetworkPlayer>();
        if (networkPlayer == null)
        {
            Debug.LogWarning("[BattleSceneUI] ⚠️ Цель не является игроком!");
            return;
        }

        // Получаем socketId игрока
        string targetSocketId = networkPlayer.socketId;
        if (string.IsNullOrEmpty(targetSocketId))
        {
            Debug.LogWarning($"[BattleSceneUI] ⚠️ У цели {networkPlayer.username} нет socketId!");
            return;
        }

        // Приглашаем в группу
        Debug.Log($"[BattleSceneUI] 📨 Приглашаем игрока {networkPlayer.username} (socketId: {targetSocketId}) в группу");
        PartyManager.Instance.InvitePlayer(targetSocketId);
    }

    private void Update()
    {
        UpdateAllUI();
    }

    private void UpdateAllUI()
    {
        UpdateHealthBar();
        UpdateManaBar();
        UpdateActionPointsBar();
        UpdateSkillSlots();
    }

    private void UpdateHealthBar()
    {
        if (healthSystem == null || hpFillImage == null) return;

        float hpPercent = healthSystem.CurrentHealth / healthSystem.MaxHealth;
        hpFillImage.fillAmount = hpPercent;

        if (hpText != null)
        {
            hpText.text = $"{Mathf.RoundToInt(healthSystem.CurrentHealth)}/{Mathf.RoundToInt(healthSystem.MaxHealth)}";
        }
    }

    private void UpdateManaBar()
    {
        if (manaSystem == null || mpFillImage == null) return;

        float mpPercent = manaSystem.CurrentMana / manaSystem.MaxMana;
        mpFillImage.fillAmount = mpPercent;

        if (mpText != null)
        {
            mpText.text = $"{Mathf.RoundToInt(manaSystem.CurrentMana)}/{Mathf.RoundToInt(manaSystem.MaxMana)}";
        }
    }

    private void UpdateActionPointsBar()
    {
        if (actionPointSystem == null || apFillImage == null) return;

        float apPercent = actionPointSystem.CurrentActionPoints / actionPointSystem.MaxActionPoints;
        apFillImage.fillAmount = apPercent;

        if (apText != null)
        {
            apText.text = $"{Mathf.RoundToInt(actionPointSystem.CurrentActionPoints)}/{Mathf.RoundToInt(actionPointSystem.MaxActionPoints)}";
        }
    }

    private void UpdateSkillSlots()
    {
        for (int i = 0; i < skillSlots.Length; i++)
        {
            if (skillSlots[i] != null)
            {
                skillSlots[i].UpdateUI();
            }
        }
    }

    private void OnTargetChanged(TargetableEntity newTarget)
    {
        if (newTarget == null)
        {
            // Скрываем target panel
            if (targetPanel != null)
                targetPanel.gameObject.SetActive(false);
        }
        else
        {
            // Показываем target panel
            if (targetPanel != null)
                targetPanel.gameObject.SetActive(true);

            // Обновляем имя цели
            if (targetNameText != null)
            {
                targetNameText.text = newTarget.GetEntityName();
            }

            // Подписываемся на изменение HP цели
            newTarget.OnHealthChanged -= OnTargetHealthChanged;
            newTarget.OnHealthChanged += OnTargetHealthChanged;

            // Обновляем HP сразу
            OnTargetHealthChanged(newTarget.GetCurrentHealth(), newTarget.GetMaxHealth());
        }
    }

    private void OnTargetHealthChanged(float current, float max)
    {
        if (targetHpFillImage != null)
        {
            targetHpFillImage.fillAmount = current / max;
        }

        if (targetHpText != null)
        {
            targetHpText.text = $"{Mathf.RoundToInt(current)}/{Mathf.RoundToInt(max)}";
        }
    }

    private void OnDestroy()
    {
        // Отписываемся от событий
        if (targetSystem != null)
        {
            targetSystem.OnTargetChanged -= OnTargetChanged;
        }
    }

    /// <summary>
    /// Показать/скрыть мобильные контролы
    /// </summary>
    public void SetMobileControlsVisible(bool visible)
    {
        showMobileControls = visible;
        if (mobileControlsPanel != null)
        {
            mobileControlsPanel.gameObject.SetActive(visible);
        }
    }

    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[BattleSceneUI] {message}");
        }
    }
}
