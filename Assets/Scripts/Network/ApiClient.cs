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
    private const string SELECT_CHARACTER_ENDPOINT = "/api/character/select";

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
    /// Выбрать/создать персонажа по классу
    /// </summary>
    public void SelectOrCreateCharacter(string token, CharacterClass characterClass, Action<CharacterResponse> onSuccess, Action<string> onError)
    {
        // Отправляем только класс персонажа
        // Сервер использует userId + characterClass для уникальности
        SelectCharacterRequest request = new SelectCharacterRequest
        {
            characterClass = characterClass.ToString()
        };

        string json = JsonUtility.ToJson(request);
        Debug.Log($"Отправляем запрос для класса: {characterClass}");
        StartCoroutine(PostRequestWithToken(SELECT_CHARACTER_ENDPOINT, token, json, onSuccess, onError));
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
