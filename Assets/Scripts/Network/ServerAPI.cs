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
    [SerializeField] private string serverURL = "https://aetherion-server.onrender.com/api"; // URL Node.js сервера на Render
    [SerializeField] private bool useLocalStorage = false; // true = PlayerPrefs, false = MongoDB через Render

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
        // Получаем JWT токен
        string token = PlayerPrefs.GetString("UserToken", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[ServerAPI] ❌ Токен не найден! Невозможно сохранить на сервер.");
            onComplete?.Invoke(false);
            yield break;
        }

        // Создаем JSON для отправки
        var saveData = new
        {
            characterClass = characterClass,
            stats = stats,
            leveling = leveling,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        string json = JsonUtility.ToJson(saveData);
        Debug.Log($"[ServerAPI] 📤 Отправка на сервер: {json}");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        // Отправляем POST запрос на правильный endpoint
        UnityWebRequest request = new UnityWebRequest($"{serverURL}/character/save-progress", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        Debug.Log($"[ServerAPI] 📡 POST {serverURL}/character/save-progress");
        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;

        if (success)
        {
            Debug.Log($"[ServerAPI] ✅ Персонаж сохранен на сервер! Response: {request.downloadHandler.text}");
        }
        else
        {
            Debug.LogError($"[ServerAPI] ❌ Ошибка сохранения: {request.error}");
            Debug.LogError($"[ServerAPI] Response: {request.downloadHandler.text}");
        }

        onComplete?.Invoke(success);
    }

    private IEnumerator LoadCharacterFromServer(string characterClass, Action<CharacterStatsData, LevelingData, bool> onComplete)
    {
        // Получаем JWT токен
        string token = PlayerPrefs.GetString("UserToken", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[ServerAPI] ❌ Токен не найден! Невозможно загрузить с сервера.");
            onComplete?.Invoke(null, null, false);
            yield break;
        }

        // Отправляем GET запрос на правильный endpoint
        UnityWebRequest request = UnityWebRequest.Get($"{serverURL}/character/load-progress?characterClass={characterClass}");
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        Debug.Log($"[ServerAPI] 📡 GET {serverURL}/character/load-progress?characterClass={characterClass}");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Парсим JSON ответ
            string json = request.downloadHandler.text;
            Debug.Log($"[ServerAPI] 📥 Получен ответ: {json}");

            ServerLoadResponse response = JsonUtility.FromJson<ServerLoadResponse>(json);

            if (response != null && response.success)
            {
                Debug.Log($"[ServerAPI] ✅ Персонаж загружен с сервера: Level {response.leveling.level}, XP {response.leveling.experience}, Points {response.leveling.availableStatPoints}");
                onComplete?.Invoke(response.stats, response.leveling, true);
            }
            else
            {
                Debug.LogError("[ServerAPI] ❌ Неверный формат ответа сервера");
                onComplete?.Invoke(null, null, false);
            }
        }
        else
        {
            Debug.LogError($"[ServerAPI] ❌ Ошибка загрузки: {request.error}");
            Debug.LogError($"[ServerAPI] Response: {request.downloadHandler.text}");
            onComplete?.Invoke(null, null, false);
        }
    }

    // Структура для сериализации (отправка на сервер)
    [System.Serializable]
    private class CharacterSaveData
    {
        public string characterClass;
        public CharacterStatsData stats;
        public LevelingData leveling;
        public string timestamp;
    }

    // Структура ответа сервера (загрузка с сервера)
    [System.Serializable]
    private class ServerLoadResponse
    {
        public bool success;
        public CharacterStatsData stats;
        public LevelingData leveling;
    }
}
