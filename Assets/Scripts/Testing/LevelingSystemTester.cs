using UnityEngine;

/// <summary>
/// Тестовый скрипт для системы прокачки
/// Добавьте на любой GameObject в сцене для быстрого тестирования
/// </summary>
public class LevelingSystemTester : MonoBehaviour
{
    [Header("Test Keys")]
    [SerializeField] private KeyCode giveExpKey = KeyCode.E;
    [SerializeField] private KeyCode levelUpKey = KeyCode.L;
    [SerializeField] private KeyCode addStatPointKey = KeyCode.K;
    [SerializeField] private KeyCode showStatsKey = KeyCode.P;
    [SerializeField] private KeyCode resetProgressKey = KeyCode.R;

    [Header("Test Settings")]
    [SerializeField] private int expToGive = 100;
    [SerializeField] private bool showDebugUI = true; // Включить/выключить UI меню (ВОССТАНОВЛЕНО)

    private LevelingSystem levelingSystem;
    private CharacterStats characterStats;
    private CharacterStatsPanel statsPanel;
    private NetworkLevelingSync networkSync;

    void Start()
    {
        Debug.Log("[LevelingSystemTester] 🧪 Тестовый скрипт активирован!");
        Debug.Log($"[LevelingSystemTester] Клавиши управления:");
        Debug.Log($"  - {giveExpKey}: Дать {expToGive} опыта");
        Debug.Log($"  - {levelUpKey}: Получить уровень (весь опыт)");
        Debug.Log($"  - {addStatPointKey}: Добавить очко характеристики");
        Debug.Log($"  - {showStatsKey}: Показать/скрыть панель статов");
        Debug.Log($"  - {resetProgressKey}: СБРОС ПРОГРЕССА (Level 1, 0 XP)");

        FindSystems();
    }

    void Update()
    {
        if (levelingSystem == null || characterStats == null)
            FindSystems();

        if (levelingSystem != null && Input.GetKeyDown(giveExpKey)) TestGiveExp();
        if (levelingSystem != null && Input.GetKeyDown(levelUpKey)) TestLevelUp();
        if (levelingSystem != null && Input.GetKeyDown(addStatPointKey)) TestAddStatPoint();
        if (Input.GetKeyDown(showStatsKey)) TestToggleStatsPanel();
        if (levelingSystem != null && Input.GetKeyDown(resetProgressKey)) TestResetProgress();
    }

    private void FindSystems()
    {
#if UNITY_EDITOR
        LocalPlayerEntity localPlayer = GameObject.FindObjectOfType<LocalPlayerEntity>();
#else
        LocalPlayerEntity localPlayer = FindFirstObjectByType<LocalPlayerEntity>();
#endif

        if (localPlayer != null)
        {
            levelingSystem = localPlayer.GetComponentInChildren<LevelingSystem>();
            characterStats = localPlayer.GetComponentInChildren<CharacterStats>();
            networkSync = localPlayer.GetComponentInChildren<NetworkLevelingSync>();
        }

        if (levelingSystem == null)
        {
#if UNITY_EDITOR
            levelingSystem = GameObject.FindObjectOfType<LevelingSystem>();
#else
            levelingSystem = FindFirstObjectByType<LevelingSystem>();
#endif
        }

        if (characterStats == null)
        {
#if UNITY_EDITOR
            characterStats = GameObject.FindObjectOfType<CharacterStats>();
#else
            characterStats = FindFirstObjectByType<CharacterStats>();
#endif
        }

        if (networkSync == null)
        {
#if UNITY_EDITOR
            networkSync = GameObject.FindObjectOfType<NetworkLevelingSync>();
#else
            networkSync = FindFirstObjectByType<NetworkLevelingSync>();
#endif
        }

        if (statsPanel == null)
        {
#if UNITY_EDITOR
            statsPanel = GameObject.FindObjectOfType<CharacterStatsPanel>();
#else
            statsPanel = FindFirstObjectByType<CharacterStatsPanel>();
#endif
        }
    }

    [ContextMenu("Test: Give EXP")]
    private void TestGiveExp()
    {
        if (levelingSystem == null) return;

        int currentExp = levelingSystem.CurrentExperience;
        int currentLevel = levelingSystem.CurrentLevel;

        levelingSystem.GainExperience(expToGive);

        Debug.Log($"[LevelingSystemTester] 💰 Дано {expToGive} опыта! Было: {currentExp}, Стало: {levelingSystem.CurrentExperience}");

        if (levelingSystem.CurrentLevel > currentLevel)
            Debug.Log($"[LevelingSystemTester] 🎉 LEVEL UP! {currentLevel} → {levelingSystem.CurrentLevel}");
    }

    [ContextMenu("Test: Level Up")]
    private void TestLevelUp()
    {
        if (levelingSystem == null) return;

        if (levelingSystem.CurrentLevel >= levelingSystem.MaxLevel)
        {
            Debug.LogWarning("[LevelingSystemTester] ⚠️ Достигнут максимальный уровень!");
            return;
        }

        int expNeeded = levelingSystem.GetExperienceForNextLevel() - levelingSystem.CurrentExperience;
        int oldLevel = levelingSystem.CurrentLevel;

        levelingSystem.GainExperience(expNeeded);
        Debug.Log($"[LevelingSystemTester] 🎉 LEVEL UP! {oldLevel} → {levelingSystem.CurrentLevel}");
    }

    [ContextMenu("Test: Add Stat Point")]
    private void TestAddStatPoint()
    {
        if (levelingSystem == null) return;

        int currentPoints = levelingSystem.AvailableStatPoints;
        var field = typeof(LevelingSystem).GetField("availableStatPoints",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(levelingSystem, currentPoints + 1);
            Debug.Log($"[LevelingSystemTester] ✨ Добавлено очко! Было: {currentPoints}, Стало: {levelingSystem.AvailableStatPoints}");
        }
        else
        {
            Debug.LogError("[LevelingSystemTester] ❌ Не удалось найти поле availableStatPoints!");
        }
    }

    [ContextMenu("Test: Toggle Stats Panel")]
    private void TestToggleStatsPanel()
    {
        if (statsPanel == null)
            statsPanel = FindFirstObjectByType<CharacterStatsPanel>();

        if (statsPanel != null)
        {
            statsPanel.Toggle();
            Debug.Log("[LevelingSystemTester] 📊 Панель статов переключена");
        }
        else
        {
            Debug.LogError("[LevelingSystemTester] ❌ CharacterStatsPanel не найден!");
        }
    }

    [ContextMenu("Test: RESET PROGRESS")]
    private void TestResetProgress()
    {
        if (levelingSystem == null || characterStats == null)
        {
            Debug.LogError("[LevelingSystemTester] ❌ Системы не найдены!");
            return;
        }

        levelingSystem.SetLevel(1, 0, 0);

        var statsData = new CharacterStatsData
        {
            strength = 5,
            perception = 5,
            endurance = 5,
            wisdom = 5,
            intelligence = 5,
            agility = 5,
            luck = 5
        };
        characterStats.ImportData(statsData);

        if (networkSync != null)
            networkSync.ForceSaveToServer();

        Debug.Log("[LevelingSystemTester] ✅ ПРОГРЕСС СБРОШЕН!");
    }

    void OnGUI()
    {
        // Если showDebugUI выключен - не показываем меню
        if (!showDebugUI) return;

        GUI.Box(new Rect(10, 10, 300, 140), "LEVELING SYSTEM TESTER");
        float y = 35;

        if (levelingSystem == null)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(20, y, 280, 20), "❌ SYSTEM NOT FOUND!");
            y += 20;
            GUI.color = Color.white;

            GUI.Label(new Rect(20, y, 280, 20), $"{showStatsKey}: Toggle Stats Panel");
            y += 20;
        }
        else
        {
            GUI.color = Color.green;
            GUI.Label(new Rect(20, y, 280, 20),
                $"✅ Level: {levelingSystem.CurrentLevel} | Points: {levelingSystem.AvailableStatPoints}");
            y += 20;
            GUI.color = Color.white;

            GUI.Label(new Rect(20, y, 280, 20), $"{giveExpKey}: Give {expToGive} EXP"); y += 20;
            GUI.Label(new Rect(20, y, 280, 20), $"{levelUpKey}: Level Up"); y += 20;
            GUI.Label(new Rect(20, y, 280, 20), $"{addStatPointKey}: Add Stat Point"); y += 20;
            GUI.Label(new Rect(20, y, 280, 20), $"{showStatsKey}: Toggle Stats Panel"); y += 20;

            GUI.color = Color.yellow;
            GUI.Label(new Rect(20, y, 280, 20), $"{resetProgressKey}: RESET PROGRESS"); y += 20;
            GUI.color = Color.white;
        }
    }
}
