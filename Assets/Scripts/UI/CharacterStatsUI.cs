using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Временное UI для отображения характеристик персонажа
/// Нажмите C для открытия/закрытия
/// </summary>
public class CharacterStatsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private Text statsText;

    private CharacterStats characterStats;
    private LevelingSystem levelingSystem;
    private HealthSystem healthSystem;
    private ManaSystem manaSystem;
    private ActionPointsSystem actionPointsSystem;
    private PlayerAttack playerAttack;

    private bool isVisible = false;

    void Start()
    {
        // Создаём UI если его нет
        if (statsPanel == null)
        {
            CreateStatsPanel();
        }

        // Не ищем сразу - персонаж генерируется динамически!

        // Скрываем панель по умолчанию
        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
        }
    }

    void Update()
    {
        // Если ещё не нашли системы - ищем
        if (characterStats == null)
        {
            FindPlayerSystems();
        }

        // Открыть/закрыть по нажатию C
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleStatsPanel();
        }

        // Обновляем текст если панель видима
        if (isVisible && statsText != null)
        {
            UpdateStatsText();
        }
    }

    /// <summary>
    /// Найти все системы персонажа
    /// </summary>
    private void FindPlayerSystems()
    {
        // Ищем через тег
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Пробуем найти по компонентам
            CharacterStats[] allStats = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
            if (allStats.Length > 0)
            {
                player = allStats[0].gameObject;
            }
            else
            {
                return; // Персонаж ещё не создан
            }
        }

        // Получаем все компоненты
        characterStats = player.GetComponentInChildren<CharacterStats>();
        levelingSystem = player.GetComponentInChildren<LevelingSystem>();
        healthSystem = player.GetComponentInChildren<HealthSystem>();
        manaSystem = player.GetComponentInChildren<ManaSystem>();
        actionPointsSystem = player.GetComponentInChildren<ActionPointsSystem>();
        playerAttack = player.GetComponentInChildren<PlayerAttack>();

        if (characterStats == null)
        {
            Debug.LogWarning("[CharacterStatsUI] CharacterStats не найден на игроке!");
        }
    }

    /// <summary>
    /// Переключить видимость панели
    /// </summary>
    private void ToggleStatsPanel()
    {
        isVisible = !isVisible;
        if (statsPanel != null)
        {
            statsPanel.SetActive(isVisible);
        }
    }

    /// <summary>
    /// Обновить текст характеристик
    /// </summary>
    private void UpdateStatsText()
    {
        if (characterStats == null || statsText == null)
            return;

        string text = "=== CHARACTER STATS ===\n\n";

        // Уровень и опыт
        if (levelingSystem != null)
        {
            text += $"<b>LEVEL:</b> {levelingSystem.CurrentLevel} / {levelingSystem.MaxLevel}\n";
            text += $"<b>EXP:</b> {levelingSystem.CurrentExperience} / {levelingSystem.GetExperienceForNextLevel()}\n";
            text += $"<b>Free Points:</b> {levelingSystem.AvailableStatPoints}\n\n";
        }

        // SPECIAL характеристики
        text += "<b>=== SPECIAL ===</b>\n";
        text += $"Strength:     {characterStats.strength}\n";
        text += $"Perception:   {characterStats.perception}\n";
        text += $"Endurance:    {characterStats.endurance}\n";
        text += $"Wisdom:       {characterStats.wisdom}\n";
        text += $"Intelligence: {characterStats.intelligence}\n";
        text += $"Agility:      {characterStats.agility}\n";
        text += $"Luck:         {characterStats.luck}\n\n";

        // Рассчитанные характеристики
        text += "<b>=== CALCULATED ===</b>\n";

        if (healthSystem != null)
        {
            text += $"HP: {healthSystem.CurrentHealth:F0} / {healthSystem.MaxHealth:F0}\n";
        }

        if (manaSystem != null)
        {
            text += $"MP: {manaSystem.CurrentMana:F0} / {manaSystem.MaxMana:F0}\n";
            text += $"MP Regen: {characterStats.ManaRegen:F1} /sec\n";
        }

        if (actionPointsSystem != null)
        {
            text += $"AP: {characterStats.MaxActionPoints:F1}\n";
            text += $"AP Regen: {characterStats.ActionPointsRegen:F2} /sec\n";
        }

        text += $"Vision: {characterStats.VisionRadius:F0}m\n";
        text += $"Crit: {characterStats.CritChance:F1}%\n\n";

        // Урон
        if (playerAttack != null && characterStats.Formulas != null)
        {
            text += "<b>=== DAMAGE ===</b>\n";
            float weaponDamage = 25f; // Примерный базовый урон
            float physDamage = characterStats.CalculatePhysicalDamage(weaponDamage);
            float magDamage = characterStats.CalculateMagicalDamage(weaponDamage);

            text += $"Physical: {physDamage:F0} ({weaponDamage} + {characterStats.strength} × {characterStats.Formulas.strengthDamageBonus})\n";
            text += $"Magical:  {magDamage:F0} ({weaponDamage} + {characterStats.intelligence} × {characterStats.Formulas.intelligenceDamageBonus})\n";
            text += $"Crit Multiplier: x{characterStats.Formulas.critDamageMultiplier}\n";
        }

        text += "\n<i>Press C to close</i>";

        statsText.text = text;
    }

    /// <summary>
    /// Создать панель статистики программно
    /// </summary>
    private void CreateStatsPanel()
    {
        // Находим или создаём Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // Создаём панель
        statsPanel = new GameObject("StatsPanel");
        statsPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = statsPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.02f, 0.5f);
        panelRect.anchorMax = new Vector2(0.02f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(400, 600);

        // Фон панели
        Image panelBg = statsPanel.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.8f);

        // Текстовое поле
        GameObject textObj = new GameObject("StatsText");
        textObj.transform.SetParent(statsPanel.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);

        statsText = textObj.AddComponent<Text>();

        // Используем LegacyRuntime.ttf вместо устаревшего Arial.ttf
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            Debug.LogWarning("[CharacterStatsUI] LegacyRuntime.ttf не найден, пробуем загрузить из Resources...");
            font = Resources.Load<Font>("Arial");
        }
        if (font != null)
        {
            statsText.font = font;
        }
        else
        {
            Debug.LogError("[CharacterStatsUI] Не удалось загрузить шрифт!");
        }

        statsText.fontSize = 14;
        statsText.color = Color.white;
        statsText.alignment = TextAnchor.UpperLeft;
        statsText.supportRichText = true;
        statsText.text = "Loading stats...";

        Debug.Log("[CharacterStatsUI] ✅ UI создан программно");
    }
}
