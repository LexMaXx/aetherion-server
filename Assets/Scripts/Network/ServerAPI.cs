using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

/// <summary>
/// API для связи с Node.js сервером (заглушка для будущей онлайн игры)
/// TODO: Заменить на реальные API endpoints когда сервер будет готов
/// </summary>
public partial class ServerAPI : MonoBehaviour
{
    [Header("Server Settings")]
    [SerializeField] private string serverURL = "http://localhost:3000/api"; // URL Node.js сервера
    [SerializeField] private bool useLocalStorage = true; // Временно используем локальное хранилище

    private static ServerAPI instance;
    public static ServerAPI Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("ServerAPI");
                instance = obj.AddComponent<ServerAPI>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // ============ СОХРАНЕНИЕ ПЕРСОНАЖА ============

    /// <summary>
    /// Сохранить персонажа на сервер
    /// </summary>
    public void SaveCharacter(string characterClass, CharacterStatsData stats, LevelingData leveling, Action<bool> onComplete = null)
    {
        if (useLocalStorage)
        {
            // Временно: сохраняем локально
            SaveCharacterLocal(characterClass, stats, leveling);
            onComplete?.Invoke(true);
            Debug.Log("[ServerAPI] ✅ Персонаж сохранен локально (PlayerPrefs)");
        }
        else
        {
            // TODO: Отправить на сервер
            StartCoroutine(SaveCharacterToServer(characterClass, stats, leveling, onComplete));
        }
    }

    /// <summary>
    /// Загрузить персонажа с сервера
    /// </summary>
    public void LoadCharacter(string characterClass, Action<CharacterStatsData, LevelingData, bool> onComplete)
    {
        if (useLocalStorage)
        {
            // Временно: загружаем локально
            var (stats, leveling) = LoadCharacterLocal(characterClass);
            bool success = stats != null && leveling != null;
            onComplete?.Invoke(stats, leveling, success);
            Debug.Log($"[ServerAPI] Персонаж загружен локально: {(success ? "✅" : "❌")}");
        }
        else
        {
            // TODO: Загрузить с сервера
            StartCoroutine(LoadCharacterFromServer(characterClass, onComplete));
        }
    }

    // ============ ЛОКАЛЬНОЕ ХРАНИЛИЩЕ (ЗАГЛУШКА) ============

    private void SaveCharacterLocal(string characterClass, CharacterStatsData stats, LevelingData leveling)
    {
        string key = $"Character_{characterClass}";

        // Сохраняем характеристики
        PlayerPrefs.SetInt($"{key}_Strength", stats.strength);
        PlayerPrefs.SetInt($"{key}_Perception", stats.perception);
        PlayerPrefs.SetInt($"{key}_Endurance", stats.endurance);
        PlayerPrefs.SetInt($"{key}_Wisdom", stats.wisdom);
        PlayerPrefs.SetInt($"{key}_Intelligence", stats.intelligence);
        PlayerPrefs.SetInt($"{key}_Agility", stats.agility);
        PlayerPrefs.SetInt($"{key}_Luck", stats.luck);

        // Сохраняем прокачку
        PlayerPrefs.SetInt($"{key}_Level", leveling.level);
        PlayerPrefs.SetInt($"{key}_Experience", leveling.experience);
        PlayerPrefs.SetInt($"{key}_StatPoints", leveling.availableStatPoints);

        PlayerPrefs.Save();
    }

    private (CharacterStatsData, LevelingData) LoadCharacterLocal(string characterClass)
    {
        string key = $"Character_{characterClass}";

        // Проверяем есть ли сохранение
        if (!PlayerPrefs.HasKey($"{key}_Level"))
        {
            return (null, null);
        }

        // Загружаем характеристики
        CharacterStatsData stats = new CharacterStatsData
        {
            strength = PlayerPrefs.GetInt($"{key}_Strength", 1),
            perception = PlayerPrefs.GetInt($"{key}_Perception", 1),
            endurance = PlayerPrefs.GetInt($"{key}_Endurance", 1),
            wisdom = PlayerPrefs.GetInt($"{key}_Wisdom", 1),
            intelligence = PlayerPrefs.GetInt($"{key}_Intelligence", 1),
            agility = PlayerPrefs.GetInt($"{key}_Agility", 1),
            luck = PlayerPrefs.GetInt($"{key}_Luck", 1)
        };

        // Загружаем прокачку
        LevelingData leveling = new LevelingData
        {
            level = PlayerPrefs.GetInt($"{key}_Level", 1),
            experience = PlayerPrefs.GetInt($"{key}_Experience", 0),
            availableStatPoints = PlayerPrefs.GetInt($"{key}_StatPoints", 0)
        };

        return (stats, leveling);
    }

    // ============ NODE.JS SERVER API (ДЛЯ БУДУЩЕГО) ============

    private IEnumerator SaveCharacterToServer(string characterClass, CharacterStatsData stats, LevelingData leveling, Action<bool> onComplete)
    {
        // Создаем JSON для отправки
        var saveData = new
        {
            characterClass = characterClass,
            stats = stats,
            leveling = leveling,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        string json = JsonUtility.ToJson(saveData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        // Отправляем POST запрос
        UnityWebRequest request = new UnityWebRequest($"{serverURL}/character/save", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;

        if (success)
        {
            Debug.Log("[ServerAPI] ✅ Персонаж сохранен на сервер");
        }
        else
        {
            Debug.LogError($"[ServerAPI] ❌ Ошибка сохранения: {request.error}");
        }

        onComplete?.Invoke(success);
    }

    private IEnumerator LoadCharacterFromServer(string characterClass, Action<CharacterStatsData, LevelingData, bool> onComplete)
    {
        // Отправляем GET запрос
        UnityWebRequest request = UnityWebRequest.Get($"{serverURL}/character/load?class={characterClass}");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Парсим JSON ответ
            string json = request.downloadHandler.text;
            CharacterSaveData saveData = JsonUtility.FromJson<CharacterSaveData>(json);

            Debug.Log("[ServerAPI] ✅ Персонаж загружен с сервера");
            onComplete?.Invoke(saveData.stats, saveData.leveling, true);
        }
        else
        {
            Debug.LogError($"[ServerAPI] ❌ Ошибка загрузки: {request.error}");
            onComplete?.Invoke(null, null, false);
        }
    }

    // Структура для сериализации
    [System.Serializable]
    private class CharacterSaveData
    {
        public string characterClass;
        public CharacterStatsData stats;
        public LevelingData leveling;
        public string timestamp;
    }
}
