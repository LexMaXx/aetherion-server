using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// API клиент для связи с сервером на Render
/// Отправляет HTTP запросы к MongoDB через REST API
/// </summary>
public class ApiClient : MonoBehaviour
{
    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "https://aetherion-server-gv5u.onrender.com"; // URL твоего сервера на Render
    [SerializeField] private int timeout = 30; // Timeout в секундах (увеличен для Render cold start)

    [Header("API Endpoints")]
    private const string REGISTER_ENDPOINT = "/api/auth/register";
    private const string LOGIN_ENDPOINT = "/api/auth/login";
    private const string VERIFY_TOKEN_ENDPOINT = "/api/auth/verify";
    private const string GET_CHARACTERS_ENDPOINT = "/api/character";
    private const string CREATE_CHARACTER_ENDPOINT = "/api/character";
    private const string SELECT_CHARACTER_ENDPOINT = "/api/character/select";
    private const string CHARACTER_PROGRESS_ENDPOINT = "/api/character/progress";

    private static ApiClient instance;
    public static ApiClient Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("ApiClient");
                instance = go.AddComponent<ApiClient>();
                DontDestroyOnLoad(go);
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
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    public void Register(string username, string email, string password, Action<AuthResponse> onSuccess, Action<string> onError)
    {
        RegisterRequest request = new RegisterRequest
        {
            username = username,
            email = email,
            password = password
        };

        string json = JsonUtility.ToJson(request);
        StartCoroutine(PostRequest(REGISTER_ENDPOINT, json, onSuccess, onError));
    }

    /// <summary>
    /// Вход пользователя
    /// </summary>
    public void Login(string username, string password, Action<AuthResponse> onSuccess, Action<string> onError)
    {
        LoginRequest request = new LoginRequest
        {
            username = username,
            password = password
        };

        string json = JsonUtility.ToJson(request);
        StartCoroutine(PostRequest(LOGIN_ENDPOINT, json, onSuccess, onError));
    }

    /// <summary>
    /// Проверка токена
    /// </summary>
    public void VerifyToken(string token, Action<AuthResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(GetRequestWithToken(VERIFY_TOKEN_ENDPOINT, token, onSuccess, onError));
    }

    /// <summary>
    /// POST запрос
    /// </summary>
    private IEnumerator PostRequest(string endpoint, string jsonData, Action<AuthResponse> onSuccess, Action<string> onError)
    {
        string url = serverUrl + endpoint;

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = timeout;

            Debug.Log($"Отправка запроса на {url}");
            Debug.Log($"Данные: {jsonData}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"Ответ сервера: {responseText}");

                try
                {
                    AuthResponse response = JsonUtility.FromJson<AuthResponse>(responseText);
                    onSuccess?.Invoke(response);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Ошибка парсинга JSON: {e.Message}");
                    onError?.Invoke("Ошибка обработки ответа сервера");
                }
            }
            else
            {
                string errorMessage = $"Ошибка: {request.error}";
                Debug.LogError(errorMessage);

                // Пытаемся получить сообщение об ошибке от сервера
                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    try
                    {
                        AuthResponse errorResponse = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                        errorMessage = errorResponse.message;
                    }
                    catch
                    {
                        errorMessage = request.downloadHandler.text;
                    }
                }

                onError?.Invoke(errorMessage);
            }
        }
    }

    /// <summary>
    /// GET запрос с токеном
    /// </summary>
    private IEnumerator GetRequestWithToken(string endpoint, string token, Action<AuthResponse> onSuccess, Action<string> onError)
    {
        string url = serverUrl + endpoint;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", $"Bearer {token}");
            request.timeout = timeout;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;

                try
                {
                    AuthResponse response = JsonUtility.FromJson<AuthResponse>(responseText);
                    onSuccess?.Invoke(response);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Ошибка парсинга JSON: {e.Message}");
                    onError?.Invoke("Ошибка обработки ответа сервера");
                }
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }
    }

    /// <summary>
    /// Получить список персонажей игрока
    /// </summary>
    public void GetCharacters(string token, Action<CharactersListResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(GetRequestWithTokenCharacters(GET_CHARACTERS_ENDPOINT, token, onSuccess, onError));
    }

    /// <summary>
    /// Создать нового персонажа
    /// </summary>
    public void CreateCharacter(string token, CharacterClass characterClass, Action<CharacterResponse> onSuccess, Action<string> onError)
    {
        // Формируем JSON вручную, так как "class" - зарезервированное слово в C#
        string className = characterClass.ToString();
        string json = $"{{\"name\":\"{className}\",\"class\":\"{className}\"}}";

        Debug.Log($"Создание персонажа класса: {characterClass}");
        Debug.Log($"JSON: {json}");
        StartCoroutine(PostRequestWithToken(CREATE_CHARACTER_ENDPOINT, token, json, onSuccess, onError));
    }

    /// <summary>
    /// Выбрать существующего персонажа по ID
    /// </summary>
    public void SelectCharacter(string token, string characterId, Action<CharacterResponse> onSuccess, Action<string> onError)
    {
        SelectCharacterRequest request = new SelectCharacterRequest
        {
            characterId = characterId
        };

        string json = JsonUtility.ToJson(request);
        Debug.Log($"Выбор персонажа ID: {characterId}");
        StartCoroutine(PostRequestWithToken(SELECT_CHARACTER_ENDPOINT, token, json, onSuccess, onError));
    }

    /// <summary>
    /// Выбрать/создать персонажа по классу (умная логика)
    /// Сначала проверяет список персонажей, если есть - выбирает, если нет - создает
    /// </summary>
    public void SelectOrCreateCharacter(string token, CharacterClass characterClass, Action<CharacterResponse> onSuccess, Action<string> onError)
    {
        Debug.Log($"SelectOrCreateCharacter для класса: {characterClass}");

        // Сначала получаем список всех персонажей
        GetCharacters(token,
            (response) =>
            {
                if (response.success && response.characters != null)
                {
                    // Ищем персонажа нужного класса
                    CharacterInfo existingChar = null;
                    foreach (var character in response.characters)
                    {
                        if (string.Equals(character.characterClass, characterClass.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            existingChar = character;
                            break;
                        }
                    }

                    if (existingChar != null)
                    {
                        // Персонаж существует - выбираем его
                        Debug.Log($"Персонаж {characterClass} найден (ID: {existingChar.id}), выбираем его");
                        SelectCharacter(token, existingChar.id, onSuccess, onError);
                    }
                    else
                    {
                        // Персонажа нет - создаем нового
                        Debug.Log($"Персонаж {characterClass} не найден, создаем нового");
                        CreateCharacter(token, characterClass, onSuccess, onError);
                    }
                }
                else
                {
                    // Нет персонажей - создаем первого
                    Debug.Log($"Нет персонажей, создаем первого: {characterClass}");
                    CreateCharacter(token, characterClass, onSuccess, onError);
                }
            },
            (error) =>
            {
                Debug.LogError($"Ошибка получения списка персонажей: {error}");
                onError?.Invoke(error);
            }
        );
    }

    public void SaveCharacterProgress(string token, string characterId, CharacterStatsData stats, LevelingData leveling, Action<CharacterProgressResponse> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(characterId))
        {
            onError?.Invoke("Token или characterId отсутствует");
            return;
        }

        CharacterProgressRequest request = new CharacterProgressRequest
        {
            characterId = characterId,
            stats = stats,
            leveling = leveling
        };

        string json = JsonUtility.ToJson(request);
        StartCoroutine(PostCharacterProgressRequest(CHARACTER_PROGRESS_ENDPOINT, token, json, onSuccess, onError));
    }

    public void LoadCharacterProgress(string token, string characterId, Action<CharacterProgressResponse> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(characterId))
        {
            onError?.Invoke("Token или characterId отсутствует");
            return;
        }

        string endpoint = $"{CHARACTER_PROGRESS_ENDPOINT}?characterId={UnityWebRequest.EscapeURL(characterId)}";
        StartCoroutine(GetCharacterProgressRequest(endpoint, token, onSuccess, onError));
    }

    private IEnumerator PostCharacterProgressRequest(string endpoint, string token, string jsonData, Action<CharacterProgressResponse> onSuccess, Action<string> onError)
    {
        string url = serverUrl + endpoint;

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {token}");
            request.timeout = timeout;

            Debug.Log($"[ApiClient] 📤 Сохранение прогресса персонажа: {url}");
            Debug.Log($"[ApiClient] Payload: {jsonData}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"[ApiClient] ✅ Прогресс сохранен: {responseText}");
                CharacterProgressResponse response = JsonUtility.FromJson<CharacterProgressResponse>(responseText);
                onSuccess?.Invoke(response);
            }
            else
            {
                Debug.LogError($"[ApiClient] ❌ Ошибка сохранения прогресса: {request.error}");
                onError?.Invoke(request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text)
                    ? request.downloadHandler.text
                    : request.error);
            }
        }
    }

    private IEnumerator GetCharacterProgressRequest(string endpoint, string token, Action<CharacterProgressResponse> onSuccess, Action<string> onError)
    {
        string url = serverUrl + endpoint;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", $"Bearer {token}");
            request.timeout = timeout;

            Debug.Log($"[ApiClient] 📥 Запрос прогресса персонажа: {url}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"[ApiClient] ✅ Прогресс получен: {responseText}");
                CharacterProgressResponse response = JsonUtility.FromJson<CharacterProgressResponse>(responseText);
                onSuccess?.Invoke(response);
            }
            else
            {
                Debug.LogError($"[ApiClient] ❌ Ошибка получения прогресса: {request.error}");
                onError?.Invoke(request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text)
                    ? request.downloadHandler.text
                    : request.error);
            }
        }
    }

    /// <summary>
    /// GET запрос с токеном для списка персонажей
    /// </summary>
    private IEnumerator GetRequestWithTokenCharacters(string endpoint, string token, Action<CharactersListResponse> onSuccess, Action<string> onError)
    {
        string url = serverUrl + endpoint;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", $"Bearer {token}");
            request.timeout = timeout;

            Debug.Log($"Запрос списка персонажей: {url}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"Список персонажей получен: {responseText}");

                try
                {
                    CharactersListResponse response = JsonUtility.FromJson<CharactersListResponse>(responseText);
                    onSuccess?.Invoke(response);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Ошибка парсинга JSON: {e.Message}");
                    onError?.Invoke("Ошибка обработки ответа сервера");
                }
            }
            else
            {
                Debug.LogError($"Ошибка получения персонажей: {request.error}");
                onError?.Invoke(request.error);
            }
        }
    }

    private CharacterStatsData GetDefaultStats(CharacterClass characterClass)
    {
        switch (characterClass)
        {
            case CharacterClass.Warrior:
                return new CharacterStatsData { strength = 15, perception = 8, endurance = 12, wisdom = 6, intelligence = 5, agility = 8, luck = 6 };
            case CharacterClass.Mage:
                return new CharacterStatsData { strength = 5, perception = 8, endurance = 6, wisdom = 18, intelligence = 18, agility = 7, luck = 10 };
            case CharacterClass.Archer:
                return new CharacterStatsData { strength = 8, perception = 16, endurance = 8, wisdom = 7, intelligence = 7, agility = 15, luck = 10 };
            case CharacterClass.Rogue:
                return new CharacterStatsData { strength = 10, perception = 12, endurance = 9, wisdom = 8, intelligence = 8, agility = 15, luck = 12 };
            case CharacterClass.Paladin:
                return new CharacterStatsData { strength = 14, perception = 9, endurance = 14, wisdom = 12, intelligence = 10, agility = 9, luck = 8 };
            default:
                return new CharacterStatsData { strength = 10, perception = 10, endurance = 10, wisdom = 10, intelligence = 10, agility = 10, luck = 10 };
        }
    }

    /// <summary>
    /// POST запрос с токеном для выбора персонажа
    /// </summary>
    private IEnumerator PostRequestWithToken(string endpoint, string token, string jsonData, Action<CharacterResponse> onSuccess, Action<string> onError)
    {
        string url = serverUrl + endpoint;

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {token}");
            request.timeout = timeout;

            Debug.Log($"Выбор персонажа: {url}");
            Debug.Log($"Данные: {jsonData}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"Ответ сервера: {responseText}");

                try
                {
                    CharacterResponse response = JsonUtility.FromJson<CharacterResponse>(responseText);
                    onSuccess?.Invoke(response);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Ошибка парсинга JSON: {e.Message}");
                    onError?.Invoke("Ошибка обработки ответа сервера");
                }
            }
            else
            {
                string errorMessage = $"Ошибка: {request.error}";
                Debug.LogError(errorMessage);

                // Логируем полный ответ сервера для отладки
                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    Debug.LogError($"Ответ сервера при ошибке: {request.downloadHandler.text}");

                    try
                    {
                        CharacterResponse errorResponse = JsonUtility.FromJson<CharacterResponse>(request.downloadHandler.text);
                        errorMessage = errorResponse.message;
                    }
                    catch
                    {
                        errorMessage = request.downloadHandler.text;
                    }
                }
                else
                {
                    Debug.LogError("Сервер не вернул ответ (пустое тело)");
                }

                // Также логируем HTTP код ответа
                Debug.LogError($"HTTP код: {request.responseCode}");

                onError?.Invoke(errorMessage);
            }
        }
    }

    /// <summary>
    /// Установить URL сервера
    /// </summary>
    public void SetServerUrl(string url)
    {
        serverUrl = url;
        Debug.Log($"Server URL установлен: {serverUrl}");
    }

    /// <summary>
    /// Получить текущий URL сервера
    /// </summary>
    public string GetServerUrl()
    {
        return serverUrl;
    }
}
