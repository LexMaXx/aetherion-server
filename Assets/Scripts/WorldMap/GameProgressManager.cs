using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управление прогрессом игры
/// Singleton, сохраняется между сценами (DontDestroyOnLoad)
/// Хранит информацию о:
/// - Открытых локациях
/// - Посещённых локациях
/// - Текущей/последней локации игрока
/// - Прогрессе квестов (расширяется при необходимости)
/// </summary>
public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    [Header("Save Settings")]
    [Tooltip("Автоматически сохранять при изменении прогресса?")]
    [SerializeField] private bool autoSave = true;

    [Tooltip("Ключ для сохранения в PlayerPrefs")]
    [SerializeField] private string saveKey = "GameProgress";

    // Прогресс игры
    private GameProgressData progressData;

    void Awake()
    {
        // Singleton с DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
            Debug.Log("[GameProgressManager] ✅ Инициализирован и загружен прогресс");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Location Management

    /// <summary>
    /// Разблокировать локацию
    /// </summary>
    public void UnlockLocation(string sceneName)
    {
        if (!progressData.unlockedLocations.Contains(sceneName))
        {
            progressData.unlockedLocations.Add(sceneName);
            Debug.Log($"[GameProgressManager] 🔓 Локация '{sceneName}' разблокирована");

            if (autoSave)
                SaveProgress();
        }
    }

    /// <summary>
    /// Проверить разблокирована ли локация
    /// </summary>
    public bool IsLocationUnlocked(string sceneName)
    {
        return progressData.unlockedLocations.Contains(sceneName);
    }

    /// <summary>
    /// Отметить локацию как посещённую
    /// </summary>
    public void MarkLocationAsVisited(string sceneName)
    {
        if (!progressData.visitedLocations.Contains(sceneName))
        {
            progressData.visitedLocations.Add(sceneName);
            Debug.Log($"[GameProgressManager] 📍 Локация '{sceneName}' посещена");

            if (autoSave)
                SaveProgress();
        }
    }

    /// <summary>
    /// Проверить была ли посещена локация
    /// </summary>
    public bool IsLocationVisited(string sceneName)
    {
        return progressData.visitedLocations.Contains(sceneName);
    }

    /// <summary>
    /// Установить последнюю локацию игрока
    /// </summary>
    public void SetLastLocation(string sceneName)
    {
        progressData.lastLocation = sceneName;
        Debug.Log($"[GameProgressManager] 💾 Последняя локация: {sceneName}");

        if (autoSave)
            SaveProgress();
    }

    /// <summary>
    /// Получить последнюю локацию игрока
    /// </summary>
    public string GetLastLocation()
    {
        return progressData.lastLocation;
    }

    /// <summary>
    /// Установить целевую локацию (куда игрок хочет пойти)
    /// </summary>
    public void SetTargetLocation(string sceneName)
    {
        progressData.targetLocation = sceneName;
        Debug.Log($"[GameProgressManager] 🎯 Целевая локация: {sceneName}");
    }

    /// <summary>
    /// Получить целевую локацию
    /// </summary>
    public string GetTargetLocation()
    {
        return progressData.targetLocation;
    }

    #endregion

    #region Quest Management (опционально)

    /// <summary>
    /// Завершить квест
    /// </summary>
    public void CompleteQuest(string questId)
    {
        if (!progressData.completedQuests.Contains(questId))
        {
            progressData.completedQuests.Add(questId);
            Debug.Log($"[GameProgressManager] ✅ Квест '{questId}' завершён");

            if (autoSave)
                SaveProgress();
        }
    }

    /// <summary>
    /// Проверить завершён ли квест
    /// </summary>
    public bool IsQuestCompleted(string questId)
    {
        return progressData.completedQuests.Contains(questId);
    }

    #endregion

    #region Save/Load

    /// <summary>
    /// Сохранить прогресс в PlayerPrefs
    /// </summary>
    public void SaveProgress()
    {
        string json = JsonUtility.ToJson(progressData, true);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();

        Debug.Log($"[GameProgressManager] 💾 Прогресс сохранён: {progressData.unlockedLocations.Count} локаций разблокировано");
    }

    /// <summary>
    /// Загрузить прогресс из PlayerPrefs
    /// </summary>
    public void LoadProgress()
    {
        if (PlayerPrefs.HasKey(saveKey))
        {
            string json = PlayerPrefs.GetString(saveKey);
            progressData = JsonUtility.FromJson<GameProgressData>(json);
            Debug.Log($"[GameProgressManager] 📂 Прогресс загружен: {progressData.unlockedLocations.Count} локаций разблокировано");
        }
        else
        {
            // Новая игра
            progressData = new GameProgressData();

            // Разблокируем стартовую локацию
            UnlockLocation("BattleScene");
            SetLastLocation("BattleScene");

            Debug.Log("[GameProgressManager] 🆕 Создан новый прогресс игры");
        }
    }

    /// <summary>
    /// Сбросить прогресс (новая игра)
    /// </summary>
    public void ResetProgress()
    {
        progressData = new GameProgressData();
        UnlockLocation("BattleScene");
        SetLastLocation("BattleScene");
        SaveProgress();

        Debug.Log("[GameProgressManager] 🔄 Прогресс сброшен");
    }

    #endregion

    #region Character Management

    /// <summary>
    /// Установить выбранного персонажа (по имени префаба)
    /// </summary>
    public void SetSelectedCharacter(string prefabName)
    {
        if (!string.IsNullOrEmpty(prefabName))
        {
            progressData.selectedCharacterPrefabPath = prefabName;

            Debug.Log($"[GameProgressManager] 🧙 Персонаж установлен: {prefabName}");

            if (autoSave)
                SaveProgress();
        }
    }

    /// <summary>
    /// Установить выбранного персонажа (из GameObject - для удобства)
    /// </summary>
    public void SetSelectedCharacter(GameObject characterPrefab)
    {
        if (characterPrefab != null)
        {
            SetSelectedCharacter(characterPrefab.name);
        }
    }

    /// <summary>
    /// Получить префаб выбранного персонажа
    /// </summary>
    public GameObject GetSelectedCharacterPrefab()
    {
        if (string.IsNullOrEmpty(progressData.selectedCharacterPrefabPath))
        {
            Debug.LogWarning("[GameProgressManager] Персонаж не выбран!");
            return null;
        }

        // Пробуем загрузить из Resources/Characters/
        GameObject prefab = Resources.Load<GameObject>("Characters/" + progressData.selectedCharacterPrefabPath);

        // Если не нашли, пробуем просто из Resources
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>(progressData.selectedCharacterPrefabPath);
        }

        if (prefab == null)
        {
            Debug.LogError($"[GameProgressManager] ❌ Не удалось загрузить персонажа: {progressData.selectedCharacterPrefabPath}\n" +
                          $"Убедитесь что префаб находится в Assets/Resources/Characters/");
        }
        else
        {
            Debug.Log($"[GameProgressManager] ✅ Загружен персонаж: {prefab.name}");
        }

        return prefab;
    }

    #endregion

    #region Debug

    [ContextMenu("Print Progress")]
    public void PrintProgress()
    {
        Debug.Log("=== GAME PROGRESS ===");
        Debug.Log($"Last Location: {progressData.lastLocation}");
        Debug.Log($"Target Location: {progressData.targetLocation}");
        Debug.Log($"Unlocked Locations ({progressData.unlockedLocations.Count}):");
        foreach (string loc in progressData.unlockedLocations)
        {
            Debug.Log($"  - {loc}");
        }
        Debug.Log($"Visited Locations ({progressData.visitedLocations.Count}):");
        foreach (string loc in progressData.visitedLocations)
        {
            Debug.Log($"  - {loc}");
        }
        Debug.Log($"Completed Quests ({progressData.completedQuests.Count}):");
        foreach (string quest in progressData.completedQuests)
        {
            Debug.Log($"  - {quest}");
        }
    }

    [ContextMenu("Reset Progress")]
    public void ResetProgressDebug()
    {
        ResetProgress();
    }

    #endregion
}

/// <summary>
/// Сериализуемая структура данных прогресса
/// </summary>
[System.Serializable]
public class GameProgressData
{
    // Локации
    public List<string> unlockedLocations = new List<string>();
    public List<string> visitedLocations = new List<string>();
    public string lastLocation = "BattleScene";
    public string targetLocation = "";

    // Квесты
    public List<string> completedQuests = new List<string>();

    // Персонаж
    public string selectedCharacterPrefabPath = ""; // Путь к префабу выбранного персонажа

    // Можно расширить:
    // public int playerLevel = 1;
    // public int gold = 0;
    // public List<string> inventory = new List<string>();
    // и т.д.
}
