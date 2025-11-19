using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Полноценная панель прокачки персонажа в MMO стиле
/// Показывает уровень, опыт, характеристики SPECIAL с кнопками +
/// Открывается клавишей P (Player Stats)
/// </summary>
public class CharacterStatsPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton;

    [Header("Header")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text classNameText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text availablePointsText;

    [Header("Experience Bar")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private Text expText;

    [Header("SPECIAL Stats")]
    [SerializeField] private StatRow strengthRow;
    [SerializeField] private StatRow perceptionRow;
    [SerializeField] private StatRow enduranceRow;
    [SerializeField] private StatRow wisdomRow;
    [SerializeField] private StatRow intelligenceRow;
    [SerializeField] private StatRow agilityRow;
    [SerializeField] private StatRow luckRow;

    [Header("Calculated Stats Display")]
    [SerializeField] private Text calculatedStatsText;

    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.P;
    [SerializeField] private Color availablePointsColor = Color.yellow;
    [SerializeField] private Color noPointsColor = Color.gray;

    // Системы
    private CharacterStats characterStats;
    private LevelingSystem levelingSystem;
    private HealthSystem healthSystem;
    private ManaSystem manaSystem;
    private ActionPointsSystem actionPointsSystem;

    private bool isVisible = false;
    private List<StatRow> allStatRows;

    void Start()
    {
        Debug.Log($"[CharacterStatsPanel] Start() - panel is null: {panel == null}");
        Debug.Log($"[CharacterStatsPanel] GameObject name: {gameObject.name}");

        // Если UI не создан - создаём программно
        if (panel == null)
        {
            Debug.LogWarning("[CharacterStatsPanel] Panel field is NULL! UI was not set up properly. Please use Tools → Aetherion → Create Character Stats Panel");

            // ФИКС: Если panel == null, значит поле не заполнено в Inspector
            // Пробуем присвоить сам GameObject как panel
            panel = gameObject;
            Debug.Log($"[CharacterStatsPanel] Auto-assigned panel to self: {panel.name}");
        }

        // Скрываем панель
        if (panel != null)
        {
            panel.SetActive(false);
        }

        // Собираем все StatRow в список
        allStatRows = new List<StatRow>
        {
            strengthRow, perceptionRow, enduranceRow, wisdomRow,
            intelligenceRow, agilityRow, luckRow
        };

        // Настраиваем кнопки
        SetupStatButtons();

        // Кнопка закрытия
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }
    }

    void Update()
    {
        // КРИТИЧЕСКИ ВАЖНО: Ищем системы если не нашли ИЛИ нашли не того игрока!
        if (characterStats == null || levelingSystem == null)
        {
            FindPlayerSystems();
        }
        else
        {
            // Проверяем что это ЛОКАЛЬНЫЙ игрок, а не сетевой!
            // Если у текущего characterStats есть NetworkPlayerEntity - это СЕТЕВОЙ игрок!
            NetworkPlayerEntity networkPlayer = characterStats.GetComponent<NetworkPlayerEntity>();
            if (networkPlayer == null)
            {
                networkPlayer = characterStats.GetComponentInParent<NetworkPlayerEntity>();
            }

            if (networkPlayer != null)
            {
                // ЭТО СЕТЕВОЙ ИГРОК! Нужно искать локального!
                Debug.LogWarning($"[CharacterStatsPanel] ❌ Найден сетевой игрок {characterStats.gameObject.name}, ищем локального...");
                characterStats = null; // Сбрасываем чтобы найти правильного
                levelingSystem = null;
            }
        }

        // DEBUG: Проверяем KeyCode.P напрямую
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log($"[CharacterStatsPanel] KeyCode.P detected! toggleKey={toggleKey}, panel={panel}");
        }

        // Переключение видимости
        if (Input.GetKeyDown(toggleKey))
        {
            Debug.Log($"[CharacterStatsPanel] Toggle key '{toggleKey}' pressed! panel={panel}, isVisible={isVisible}");
            Toggle();
        }

        // Обновляем UI если видим
        if (isVisible && characterStats != null)
        {
            UpdateUI();
        }
    }

    /// <summary>
    /// Найти все системы игрока
    /// </summary>
    private void FindPlayerSystems()
    {
        UnsubscribeFromSystems();
        characterStats = null;
        levelingSystem = null;
        healthSystem = null;
        manaSystem = null;
        actionPointsSystem = null;

        // КРИТИЧЕСКИ ВАЖНО: Ищем ТОЛЬКО локального игрока, не сетевых!
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("[CharacterStatsPanel] Player tag не найден, ищем через LocalPlayerEntity...");

            // Пытаемся найти локального игрока через LocalPlayerEntity
            LocalPlayerEntity localPlayer = FindObjectsByType<LocalPlayerEntity>(FindObjectsSortMode.None).FirstOrDefault();
            if (localPlayer != null)
            {
                player = localPlayer.gameObject;
                Debug.Log($"[CharacterStatsPanel] ✅ Найден локальный игрок через LocalPlayerEntity: {player.name}");
            }
            else
            {
                // Fallback: ищем игрока который НЕ NetworkPlayerEntity
                CharacterStats[] allStats = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
                foreach (CharacterStats stats in allStats)
                {
                    // Пропускаем сетевых игроков
                    if (stats.GetComponent<NetworkPlayerEntity>() == null && stats.GetComponentInParent<NetworkPlayerEntity>() == null)
                    {
                        player = stats.gameObject;
                        Debug.Log($"[CharacterStatsPanel] ✅ Найден локальный игрок (не сетевой): {player.name}");
                        break;
                    }
                }

                if (player == null)
                {
                    Debug.LogError("[CharacterStatsPanel] ❌ Локальный игрок не найден!");
                    return;
                }
            }
        }

        characterStats = player.GetComponentInChildren<CharacterStats>();
        levelingSystem = player.GetComponentInChildren<LevelingSystem>();
        healthSystem = player.GetComponentInChildren<HealthSystem>();
        manaSystem = player.GetComponentInChildren<ManaSystem>();
        actionPointsSystem = player.GetComponentInChildren<ActionPointsSystem>();

        // Если LevelingSystem не найден через GetComponentInChildren, ищем его на том же объекте что CharacterStats
        if (levelingSystem == null && characterStats != null)
        {
            Debug.LogWarning($"[CharacterStatsPanel] LevelingSystem не найден через GetComponentInChildren, пробуем найти на {characterStats.gameObject.name}");
            levelingSystem = characterStats.gameObject.GetComponent<LevelingSystem>();

            // Если всё ещё null - добавляем динамически
            if (levelingSystem == null)
            {
                Debug.LogWarning($"[CharacterStatsPanel] LevelingSystem отсутствует! Добавляем динамически на {characterStats.gameObject.name}");
                levelingSystem = characterStats.gameObject.AddComponent<LevelingSystem>();
            }
        }

        if (characterStats != null)
        {
            Debug.Log($"[CharacterStatsPanel] ✅ CharacterStats найден на: {characterStats.gameObject.name}");

            if (levelingSystem != null)
            {
                Debug.Log($"[CharacterStatsPanel] ✅ LevelingSystem найден на: {levelingSystem.gameObject.name}");
                Debug.Log($"[CharacterStatsPanel] 📊 Данные: Level {levelingSystem.CurrentLevel}/{levelingSystem.MaxLevel}, XP {levelingSystem.CurrentExperience}, Points {levelingSystem.AvailableStatPoints}");
            }
            else
            {
                Debug.LogError("[CharacterStatsPanel] ❌ LevelingSystem НЕ НАЙДЕН!");
            }

            if (healthSystem != null)
            {
                Debug.Log($"[CharacterStatsPanel] ✅ HealthSystem найден");
            }

            if (manaSystem != null)
            {
                Debug.Log($"[CharacterStatsPanel] ✅ ManaSystem найден");
            }

            if (actionPointsSystem != null)
            {
                Debug.Log($"[CharacterStatsPanel] ✅ ActionPointsSystem найден");
            }

            SubscribeToSystems();
        }
        else
        {
            Debug.LogWarning("[CharacterStatsPanel] ❌ CharacterStats не найден!");
        }

        if (levelingSystem == null)
        {
            Debug.LogWarning("[CharacterStatsPanel] LevelingSystem не найден!");
        }
    }

    /// <summary>
    /// Настроить кнопки прокачки характеристик
    /// </summary>
    private void SetupStatButtons()
    {
        if (strengthRow != null) strengthRow.button.onClick.AddListener(() => OnStatButtonClick("strength"));
        if (perceptionRow != null) perceptionRow.button.onClick.AddListener(() => OnStatButtonClick("perception"));
        if (enduranceRow != null) enduranceRow.button.onClick.AddListener(() => OnStatButtonClick("endurance"));
        if (wisdomRow != null) wisdomRow.button.onClick.AddListener(() => OnStatButtonClick("wisdom"));
        if (intelligenceRow != null) intelligenceRow.button.onClick.AddListener(() => OnStatButtonClick("intelligence"));
        if (agilityRow != null) agilityRow.button.onClick.AddListener(() => OnStatButtonClick("agility"));
        if (luckRow != null) luckRow.button.onClick.AddListener(() => OnStatButtonClick("luck"));
    }

    /// <summary>
    /// Обработка нажатия на кнопку +
    /// </summary>
    private void OnStatButtonClick(string statName)
    {
        if (levelingSystem == null)
        {
            Debug.LogError("[CharacterStatsPanel] LevelingSystem не найден!");
            return;
        }

        if (levelingSystem.AvailableStatPoints <= 0)
        {
            Debug.LogWarning("[CharacterStatsPanel] Нет свободных очков!");
            // TODO: Показать UI уведомление
            return;
        }

        bool success = levelingSystem.SpendStatPoint(statName);
        if (success)
        {
            Debug.Log($"[CharacterStatsPanel] ✅ Прокачана характеристика: {statName}");
            UpdateUI(); // Обновляем сразу

            // TODO: Звуковой эффект прокачки
            // TODO: Визуальный эффект на кнопке
        }
    }

    /// <summary>
    /// Обработчик события изменения статов
    /// </summary>
    private void OnStatsChangedHandler()
    {
        Debug.Log("[CharacterStatsPanel] OnStatsChanged получен - обновляем UI");
        UpdateUI();
    }

    private void OnExperienceGainedHandler(int _)
    {
        if (isVisible)
        {
            UpdateUI();
        }
    }

    /// <summary>
    /// Обработчик события получения уровня
    /// </summary>
    private void OnLevelUpHandler(int newLevel)
    {
        Debug.Log($"[CharacterStatsPanel] OnLevelUp получен - новый уровень {newLevel}");
        UpdateUI();
    }

    /// <summary>
    /// Обработчик события изменения доступных очков
    /// </summary>
    private void OnStatPointsChangedHandler(int newPoints)
    {
        Debug.Log($"[CharacterStatsPanel] OnStatPointsChanged получен - новые очки {newPoints}");
        UpdateUI();
    }

    /// <summary>
    /// Принудительно обновить UI (вызывается извне после загрузки данных с сервера)
    /// </summary>
    public void ForceRefresh()
    {
        Debug.Log("[CharacterStatsPanel] ForceRefresh вызван - обновляем UI");
        UpdateUI();
    }

    /// <summary>
    /// Обновить весь UI
    /// </summary>
    private void UpdateUI()
    {
        if (characterStats == null || levelingSystem == null)
        {
            Debug.LogWarning($"[CharacterStatsPanel] UpdateUI пропущен: characterStats={characterStats != null}, levelingSystem={levelingSystem != null}");
            return;
        }

        Debug.Log($"[CharacterStatsPanel] UpdateUI: Level={levelingSystem.CurrentLevel}, Points={levelingSystem.AvailableStatPoints}");

        // Заголовок
        if (titleText != null)
        {
            titleText.text = "CHARACTER";
        }

        if (classNameText != null)
        {
            classNameText.text = characterStats.ClassName.ToUpper();
        }

        // Уровень и очки
        if (levelText != null)
        {
            levelText.text = $"LEVEL {levelingSystem.CurrentLevel} / {levelingSystem.MaxLevel}";
            Debug.Log($"[CharacterStatsPanel] levelText обновлен: {levelText.text}");
        }
        else
        {
            Debug.LogWarning("[CharacterStatsPanel] levelText is NULL!");
        }

        if (availablePointsText != null)
        {
            int points = levelingSystem.AvailableStatPoints;
            availablePointsText.text = $"AVAILABLE POINTS: {points}";
            availablePointsText.color = points > 0 ? availablePointsColor : noPointsColor;
            Debug.Log($"[CharacterStatsPanel] availablePointsText обновлен: {points} points");
        }
        else
        {
            Debug.LogWarning("[CharacterStatsPanel] availablePointsText is NULL!");
        }

        // Полоса опыта
        if (expSlider != null && levelingSystem.CurrentLevel < levelingSystem.MaxLevel)
        {
            float progress = levelingSystem.GetLevelProgress();
            expSlider.value = progress;

            if (expText != null)
            {
                int current = levelingSystem.CurrentExperience;
                int needed = levelingSystem.GetExperienceForNextLevel();
                expText.text = $"EXP: {current} / {needed} ({progress * 100f:F0}%)";
            }
        }
        else if (expSlider != null)
        {
            expSlider.value = 1f;
            if (expText != null)
            {
                expText.text = "MAX LEVEL REACHED";
            }
        }

        // SPECIAL характеристики
        UpdateStatRow(strengthRow, "Strength", characterStats.strength, "Physical damage");
        UpdateStatRow(perceptionRow, "Perception", characterStats.perception, "Vision radius");
        UpdateStatRow(enduranceRow, "Endurance", characterStats.endurance, "Health points");
        UpdateStatRow(wisdomRow, "Wisdom", characterStats.wisdom, "Mana & regen");
        UpdateStatRow(intelligenceRow, "Intelligence", characterStats.intelligence, "Magical damage");
        UpdateStatRow(agilityRow, "Agility", characterStats.agility, "Action points");
        UpdateStatRow(luckRow, "Luck", characterStats.luck, "Critical chance");

        // Вычисленные характеристики
        UpdateCalculatedStats();

        // Активность кнопок +
        bool hasPoints = levelingSystem.AvailableStatPoints > 0;
        foreach (var row in allStatRows)
        {
            if (row != null && row.button != null)
            {
                // Кнопка активна если есть очки И стат не максимальный (10)
                int statValue = characterStats.GetStat(GetStatNameFromRow(row));
                row.button.interactable = hasPoints && statValue < 10;
            }
        }
    }

    /// <summary>
    /// Обновить строку характеристики
    /// </summary>
    private void UpdateStatRow(StatRow row, string statName, int value, string description)
    {
        if (row == null) return;

        if (row.nameText != null)
        {
            row.nameText.text = statName;
        }

        if (row.valueText != null)
        {
            row.valueText.text = $"{value} / 10";
        }

        if (row.descriptionText != null)
        {
            row.descriptionText.text = description;
        }
    }

    /// <summary>
    /// Получить имя характеристики из StatRow
    /// </summary>
    private string GetStatNameFromRow(StatRow row)
    {
        if (row == strengthRow) return "strength";
        if (row == perceptionRow) return "perception";
        if (row == enduranceRow) return "endurance";
        if (row == wisdomRow) return "wisdom";
        if (row == intelligenceRow) return "intelligence";
        if (row == agilityRow) return "agility";
        if (row == luckRow) return "luck";
        return "";
    }

    /// <summary>
    /// Обновить вычисленные характеристики
    /// </summary>
    private void UpdateCalculatedStats()
    {
        if (calculatedStatsText == null || characterStats == null)
            return;

        string text = "<b>=== CALCULATED STATS ===</b>\n\n";

        if (healthSystem != null)
        {
            text += $"<b>Health:</b> {healthSystem.MaxHealth:F0}\n";
        }

        if (manaSystem != null)
        {
            text += $"<b>Mana:</b> {manaSystem.MaxMana:F0}\n";
            text += $"<b>Mana Regen:</b> {characterStats.ManaRegen:F1} /sec\n";
        }

        if (actionPointsSystem != null)
        {
            text += $"<b>Action Points:</b> {characterStats.MaxActionPoints:F0}\n";
            text += $"<b>AP Regen:</b> {characterStats.ActionPointsRegen:F2} /sec\n";
        }

        text += $"<b>Vision Radius:</b> {characterStats.VisionRadius:F0}m\n";
        text += $"<b>Crit Chance:</b> {characterStats.CritChance:F1}%\n";

        calculatedStatsText.text = text;
    }

    /// <summary>
    /// Показать панель
    /// </summary>
    public void Show()
    {
        Debug.Log($"[CharacterStatsPanel] Show() called - panel={panel}, characterStats={characterStats != null}, levelingSystem={levelingSystem != null}");

        // Принудительно ищем системы если их нет
        if (characterStats == null || levelingSystem == null)
        {
            Debug.LogWarning("[CharacterStatsPanel] Системы не найдены, ищем принудительно...");
            FindPlayerSystems();
        }

        if (panel != null)
        {
            panel.SetActive(true);
            isVisible = true;
            UpdateUI();
        }
    }

    /// <summary>
    /// Скрыть панель
    /// </summary>
    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
            isVisible = false;
        }
    }

    /// <summary>
    /// Переключить видимость
    /// </summary>
    public void Toggle()
    {
        Debug.Log($"[CharacterStatsPanel] Toggle() called - isVisible={isVisible}, panel={panel}");

        if (panel == null)
        {
            Debug.LogError("[CharacterStatsPanel] Cannot toggle - panel is NULL!");
            return;
        }

        if (isVisible)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    /// <summary>
    /// Создать панель программно (если не настроена в редакторе)
    /// </summary>
    private void CreateStatsPanel()
    {
        Debug.Log("[CharacterStatsPanel] UI будет создан вручную в Unity Editor");
        // TODO: Программное создание UI (очень много кода, лучше делать в редакторе)
    }

    /// <summary>
    /// Отписываемся от событий при уничтожении
    /// </summary>
    void OnDestroy()
    {
        UnsubscribeFromSystems();
        Debug.Log("[CharacterStatsPanel] Отписка от событий выполнена");
    }

    private void SubscribeToSystems()
    {
        if (characterStats != null)
        {
            characterStats.OnStatsChanged -= OnStatsChangedHandler;
            characterStats.OnStatsChanged += OnStatsChangedHandler;
        }

        if (levelingSystem != null)
        {
            levelingSystem.OnLevelUp -= OnLevelUpHandler;
            levelingSystem.OnStatPointsChanged -= OnStatPointsChangedHandler;
            levelingSystem.OnExperienceGained -= OnExperienceGainedHandler;

            levelingSystem.OnLevelUp += OnLevelUpHandler;
            levelingSystem.OnStatPointsChanged += OnStatPointsChangedHandler;
            levelingSystem.OnExperienceGained += OnExperienceGainedHandler;
        }
    }

    private void UnsubscribeFromSystems()
    {
        if (characterStats != null)
        {
            characterStats.OnStatsChanged -= OnStatsChangedHandler;
        }

        if (levelingSystem != null)
        {
            levelingSystem.OnLevelUp -= OnLevelUpHandler;
            levelingSystem.OnStatPointsChanged -= OnStatPointsChangedHandler;
            levelingSystem.OnExperienceGained -= OnExperienceGainedHandler;
        }
    }
}

/// <summary>
/// Строка характеристики (для инспектора)
/// </summary>
[System.Serializable]
public class StatRow
{
    public Text nameText;
    public Text valueText;
    public Text descriptionText;
    public Button button;
}
