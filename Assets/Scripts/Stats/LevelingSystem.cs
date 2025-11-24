using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Система прокачки персонажа (уровни и распределение очков)
/// Макс уровень: 20, за каждый уровень +1 очко характеристики
/// </summary>
public class LevelingSystem : MonoBehaviour
{
    [Header("Level Settings")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int maxLevel = 20;
    [SerializeField] private int currentExperience = 0;

    [Header("Stat Points")]
    [SerializeField] private int availableStatPoints = 0; // Свободные очки для распределения
    [SerializeField] private int statPointsPerLevel = 1;  // Сколько очков за уровень

    [Header("Experience Curve")]
    [Tooltip("Базовый опыт для 2 уровня")]
    [SerializeField] private int baseExpForLevel2 = 100;
    [Tooltip("Множитель роста опыта (каждый уровень требует больше)")]
    [SerializeField] private float expGrowthMultiplier = 1.5f;

    [Header("Auto Save")]
    [Tooltip("Задержка перед автосохранением (секунды)")]
    [SerializeField] private float autoSaveDelay = 3f;

    // Ссылка на CharacterStats
    private CharacterStats characterStats;

    // Автосохранение
    private Coroutine saveCoroutine;

    // События
    public event Action<int> OnLevelUp;           // Новый уровень
    public event Action<int> OnExperienceGained;  // Получен опыт
    public event Action<int> OnStatPointsChanged; // Изменились свободные очки

    // Геттеры
    public int CurrentLevel => currentLevel;
    public int MaxLevel => maxLevel;
    public int CurrentExperience => currentExperience;
    public int AvailableStatPoints => availableStatPoints;

    void Start()
    {
        characterStats = GetComponent<CharacterStats>();
        if (characterStats == null)
        {
            Debug.LogError("[LevelingSystem] CharacterStats не найден!");
        }
    }

    /// <summary>
    /// Получить опыт
    /// </summary>
    public void GainExperience(int amount)
    {
        if (currentLevel >= maxLevel)
        {
            Debug.Log("[LevelingSystem] Достигнут максимальный уровень!");
            return;
        }

        currentExperience += amount;
        OnExperienceGained?.Invoke(amount);

        Debug.Log($"[LevelingSystem] +{amount} опыта. Всего: {currentExperience}/{GetExperienceForNextLevel()}");

        // Проверяем повышение уровня
        CheckLevelUp();
    }

    /// <summary>
    /// Проверка и повышение уровня
    /// </summary>
    private void CheckLevelUp()
    {
        while (currentExperience >= GetExperienceForNextLevel() && currentLevel < maxLevel)
        {
            LevelUp();
        }
    }

    /// <summary>
    /// Повысить уровень
    /// </summary>
    private void LevelUp()
    {
        currentLevel++;
        availableStatPoints += statPointsPerLevel;

        Debug.Log($"[LevelingSystem] ★ УРОВЕНЬ ПОВЫШЕН: {currentLevel}! Свободных очков: {availableStatPoints}");

        OnLevelUp?.Invoke(currentLevel);
        OnStatPointsChanged?.Invoke(availableStatPoints);

        // Сохраняем на сервер (NetworkLevelingSync сделает это с задержкой в multiplayer)
        // Но в singleplayer NetworkLevelingSync отсутствует, поэтому вызываем напрямую
        ScheduleSaveToServer();
    }

    /// <summary>
    /// Потратить очко характеристики на прокачку
    /// </summary>
    public bool SpendStatPoint(string statName)
    {
        if (availableStatPoints <= 0)
        {
            Debug.LogWarning("[LevelingSystem] Нет свободных очков характеристик!");
            return false;
        }

        if (characterStats == null)
        {
            Debug.LogError("[LevelingSystem] CharacterStats не найден!");
            return false;
        }

        // Пытаемся увеличить характеристику
        bool success = characterStats.Increasestat(statName);
        if (success)
        {
            availableStatPoints--;
            OnStatPointsChanged?.Invoke(availableStatPoints);
            Debug.Log($"[LevelingSystem] Прокачана характеристика {statName}. Осталось очков: {availableStatPoints}");

            // Сохраняем на сервер (NetworkLevelingSync сделает это с задержкой в multiplayer)
            // Но в singleplayer NetworkLevelingSync отсутствует, поэтому вызываем напрямую
            ScheduleSaveToServer();

            return true;
        }

        return false;
    }

    /// <summary>
    /// Расчет опыта для следующего уровня
    /// </summary>
    public int GetExperienceForNextLevel()
    {
        if (currentLevel >= maxLevel)
            return int.MaxValue;

        // Формула: baseExp * (multiplier ^ (level - 1))
        // Уровень 2: 100 * 1.5^0 = 100
        // Уровень 3: 100 * 1.5^1 = 150
        // Уровень 4: 100 * 1.5^2 = 225
        // ...
        // Уровень 20: 100 * 1.5^18 ≈ 147,745
        return Mathf.RoundToInt(baseExpForLevel2 * Mathf.Pow(expGrowthMultiplier, currentLevel - 1));
    }

    /// <summary>
    /// Процент прогресса до следующего уровня
    /// </summary>
    public float GetLevelProgress()
    {
        if (currentLevel >= maxLevel)
            return 1f;

        int expForNext = GetExperienceForNextLevel();
        int expForCurrent = currentLevel > 1 ? GetExperienceForLevel(currentLevel) : 0;
        int expInCurrentLevel = currentExperience - expForCurrent;
        int expNeededForLevel = expForNext - expForCurrent;

        return (float)expInCurrentLevel / expNeededForLevel;
    }

    /// <summary>
    /// Получить опыт необходимый для конкретного уровня
    /// </summary>
    private int GetExperienceForLevel(int level)
    {
        if (level <= 1) return 0;
        return Mathf.RoundToInt(baseExpForLevel2 * Mathf.Pow(expGrowthMultiplier, level - 2));
    }

    /// <summary>
    /// Установить уровень (для тестирования или загрузки с сервера)
    /// </summary>
    public void SetLevel(int level, int experience, int statPoints)
    {
        currentLevel = Mathf.Clamp(level, 1, maxLevel);
        currentExperience = Mathf.Max(0, experience);
        availableStatPoints = Mathf.Max(0, statPoints);

        Debug.Log($"[LevelingSystem] Установлен уровень {currentLevel}, опыт {currentExperience}, свободных очков {availableStatPoints}");
    }

    /// <summary>
    /// Получить данные для сохранения на сервер
    /// </summary>
    public LevelingData GetLevelingData()
    {
        return new LevelingData
        {
            level = currentLevel,
            experience = currentExperience,
            availableStatPoints = availableStatPoints
        };
    }

    /// <summary>
    /// Загрузить данные с сервера
    /// </summary>
    public void LoadLevelingData(LevelingData data)
    {
        SetLevel(data.level, data.experience, data.availableStatPoints);
    }

    // Для отладки в Inspector
    private void OnValidate()
    {
        currentLevel = Mathf.Clamp(currentLevel, 1, maxLevel);
        currentExperience = Mathf.Max(0, currentExperience);
        availableStatPoints = Mathf.Max(0, availableStatPoints);
    }

    // Для тестирования в редакторе
    [ContextMenu("Test: Gain 50 EXP")]
    private void TestGainExp()
    {
        GainExperience(50);
    }

    [ContextMenu("Test: Level Up")]
    private void TestLevelUp()
    {
        int expNeeded = GetExperienceForNextLevel() - currentExperience;
        GainExperience(expNeeded);
    }

    [ContextMenu("Test: Add Stat Point")]
    private void TestAddStatPoint()
    {
        availableStatPoints++;
        Debug.Log($"[Test] Добавлено очко. Всего: {availableStatPoints}");
    }

    // ═══════════════════════════════════════════════════════
    // SERVER SYNC - Методы для синхронизации с сервером
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Экспортировать данные прокачки для отправки на сервер
    /// </summary>
    public LevelingData ExportData()
    {
        return new LevelingData
        {
            level = currentLevel,
            experience = currentExperience,
            availableStatPoints = availableStatPoints
        };
    }

    /// <summary>
    /// Импортировать данные прокачки с сервера
    /// </summary>
    public void ImportData(LevelingData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[LevelingSystem] ImportData: data is null!");
            return;
        }

        currentLevel = data.level;
        currentExperience = data.experience;
        availableStatPoints = data.availableStatPoints;

        Debug.Log($"[LevelingSystem] ✅ Данные импортированы: Level {currentLevel}, XP {currentExperience}, Points {availableStatPoints}");

        // Вызываем события для обновления UI
        OnLevelUp?.Invoke(currentLevel);
        OnExperienceGained?.Invoke(currentExperience);
        OnStatPointsChanged?.Invoke(availableStatPoints);
    }

    /// <summary>
    /// Запланировать сохранение на сервер с задержкой (избегаем спама)
    /// </summary>
    private void ScheduleSaveToServer()
    {
        // Проверяем наличие NetworkLevelingSync - в multiplayer он сам сохраняет
        NetworkLevelingSync networkSync = GetComponent<NetworkLevelingSync>();
        if (networkSync != null)
        {
            // В multiplayer NetworkLevelingSync сам вызовет SaveToServer через свою задержку
            Debug.Log("[LevelingSystem] NetworkLevelingSync обнаружен - пропускаем дублирование сохранения");
            return;
        }

        // В singleplayer NetworkLevelingSync отсутствует - сохраняем сами
        if (saveCoroutine != null)
        {
            StopCoroutine(saveCoroutine);
        }
        saveCoroutine = StartCoroutine(SaveToServerDelayed());
    }

    /// <summary>
    /// Корутина для отложенного сохранения
    /// </summary>
    private IEnumerator SaveToServerDelayed()
    {
        yield return new WaitForSeconds(autoSaveDelay);
        Debug.Log("[LevelingSystem] ⏰ Автосохранение на сервер через задержку...");
        SaveToServer();
    }

    /// <summary>
    /// Сохранить данные на сервер
    /// </summary>
    public void SaveToServer()
    {
        CharacterStats characterStats = GetComponent<CharacterStats>();
        if (characterStats == null)
        {
            Debug.LogError("[LevelingSystem] CharacterStats не найден!");
            return;
        }

        LevelingData levelingData = ExportData();
        CharacterStatsData statsData = characterStats.ExportData();

        string token = PlayerPrefs.GetString("UserToken", "");
        string characterId = PlayerPrefs.GetString("SelectedCharacterId", "");

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(characterId))
        {
            Debug.LogWarning("[LevelingSystem] Невозможно сохранить прогресс: отсутствует токен или ID персонажа");
            return;
        }

        ApiClient.Instance.SaveCharacterProgress(token, characterId, statsData, levelingData,
            (response) =>
            {
                if (response != null && response.success)
                {
                    Debug.Log("[LevelingSystem] ✅ Прогресс персонажа сохранен в MongoDB");
                }
                else
                {
                    Debug.LogWarning($"[LevelingSystem] ⚠️ Не удалось сохранить прогресс: {response?.message}");
                }
            },
            (error) =>
            {
                Debug.LogError($"[LevelingSystem] ❌ Ошибка сохранения прогресса: {error}");
            });
    }
}

/// <summary>
/// Данные прокачки для сериализации (отправка на сервер)
/// </summary>
[System.Serializable]
public class LevelingData
{
    public int level;
    public int experience;
    public int availableStatPoints;
}
