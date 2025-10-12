using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Менеджер аутентификации - управляет регистрацией и логином
/// Связывает UI с валидацией и API клиентом
/// </summary>
public class AuthenticationManager : MonoBehaviour
{
    [Header("Register UI Input Fields")]
    [SerializeField] private TMP_InputField registerUsernameField;
    [SerializeField] private TMP_InputField registerEmailField;
    [SerializeField] private TMP_InputField registerPasswordField;

    [Header("Login UI Input Fields")]
    [SerializeField] private TMP_InputField loginUsernameField;
    [SerializeField] private TMP_InputField loginPasswordField;

    [Header("Buttons")]
    [SerializeField] private Button registerButton;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button switchToLoginButton; // Переключение на форму логина
    [SerializeField] private Button switchToRegisterButton; // Переключение на форму регистрации

    [Header("Panels")]
    [SerializeField] private GameObject registerPanel; // Панель регистрации
    [SerializeField] private GameObject loginPanel; // Панель логина

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI feedbackText; // Текст с сообщениями об ошибках/успехе
    [SerializeField] private GameObject loadingIndicator; // Индикатор загрузки

    [Header("Scene Settings")]
    [SerializeField] private string characterSelectionSceneName = "CharacterSelectionScene"; // Сцена выбора персонажа после входа

    private bool isProcessing = false;

    void Start()
    {
        // Подписка на события кнопок
        registerButton?.onClick.AddListener(OnRegisterClick);
        loginButton?.onClick.AddListener(OnLoginClick);
        switchToLoginButton?.onClick.AddListener(() => SwitchPanel(false));
        switchToRegisterButton?.onClick.AddListener(() => SwitchPanel(true));

        // Показываем панель регистрации по умолчанию
        SwitchPanel(true);

        // Скрываем индикатор загрузки
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        // Очищаем feedback текст
        SetFeedback("", Color.white);
    }

    /// <summary>
    /// Переключение между панелями регистрации и логина
    /// </summary>
    private void SwitchPanel(bool showRegister)
    {
        if (registerPanel != null)
            registerPanel.SetActive(showRegister);

        if (loginPanel != null)
            loginPanel.SetActive(!showRegister);

        // Очищаем поля и feedback
        ClearFields();
        SetFeedback("", Color.white);
    }

    /// <summary>
    /// Обработка нажатия кнопки регистрации
    /// </summary>
    private void OnRegisterClick()
    {
        if (isProcessing) return;

        string username = registerUsernameField.text.Trim();
        string email = registerEmailField.text.Trim();
        string password = registerPasswordField.text;

        // Валидация на клиенте
        var validationResult = InputValidator.ValidateRegistration(username, email, password);

        if (!validationResult.isValid)
        {
            SetFeedback(validationResult.message, Color.red);
            return;
        }

        // Отправка на сервер
        StartRegistration(username, email, password);
    }

    /// <summary>
    /// Обработка нажатия кнопки логина
    /// </summary>
    private void OnLoginClick()
    {
        if (isProcessing) return;

        string username = loginUsernameField.text.Trim();
        string password = loginPasswordField.text;

        // Валидация на клиенте
        var validationResult = InputValidator.ValidateLogin(username, password);

        if (!validationResult.isValid)
        {
            SetFeedback(validationResult.message, Color.red);
            return;
        }

        // Отправка на сервер
        StartLogin(username, password);
    }

    /// <summary>
    /// Начать процесс регистрации
    /// </summary>
    private void StartRegistration(string username, string email, string password)
    {
        SetProcessing(true);
        SetFeedback("Регистрация...", new Color(0.83f, 0.68f, 0.23f)); // Золотой цвет

        ApiClient.Instance.Register(username, email, password,
            onSuccess: (response) =>
            {
                SetProcessing(false);

                if (response.success)
                {
                    SetFeedback("Регистрация успешна! Вход в игру...", Color.green);
                    SaveUserData(response.token, username); // Сохраняем токен И username
                    LoadGameScene();
                }
                else
                {
                    SetFeedback(response.message, Color.red);
                }
            },
            onError: (error) =>
            {
                SetProcessing(false);
                SetFeedback($"Ошибка: {error}", Color.red);
            }
        );
    }

    /// <summary>
    /// Начать процесс логина
    /// </summary>
    private void StartLogin(string username, string password)
    {
        SetProcessing(true);
        SetFeedback("Вход...", new Color(0.83f, 0.68f, 0.23f)); // Золотой цвет

        ApiClient.Instance.Login(username, password,
            onSuccess: (response) =>
            {
                SetProcessing(false);

                if (response.success)
                {
                    SetFeedback("Вход успешен! Загрузка...", Color.green);
                    SaveUserData(response.token, username); // Сохраняем токен И username
                    LoadGameScene();
                }
                else
                {
                    SetFeedback(response.message, Color.red);
                }
            },
            onError: (error) =>
            {
                SetProcessing(false);
                SetFeedback($"Ошибка: {error}", Color.red);
            }
        );
    }

    /// <summary>
    /// Сохранить данные пользователя (токен и username)
    /// </summary>
    private void SaveUserData(string token, string username)
    {
        PlayerPrefs.SetString("UserToken", token);
        PlayerPrefs.SetString("Username", username); // ВАЖНО: Сохраняем username!
        PlayerPrefs.Save();
        Debug.Log($"[Auth] ✅ Данные сохранены: Username={username}, Token={token.Substring(0, 10)}...");
    }

    /// <summary>
    /// Загрузить сцену выбора персонажа
    /// </summary>
    private void LoadGameScene()
    {
        Invoke(nameof(LoadScene), 1.5f); // Задержка для показа сообщения
    }

    private void LoadScene()
    {
        SceneManager.LoadScene(characterSelectionSceneName);
    }

    /// <summary>
    /// Установить состояние обработки
    /// </summary>
    private void SetProcessing(bool processing)
    {
        isProcessing = processing;

        if (registerButton != null)
            registerButton.interactable = !processing;

        if (loginButton != null)
            loginButton.interactable = !processing;

        if (loadingIndicator != null)
            loadingIndicator.SetActive(processing);
    }

    /// <summary>
    /// Показать сообщение пользователю
    /// </summary>
    private void SetFeedback(string message, Color color)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = color;
        }
    }

    /// <summary>
    /// Очистить поля ввода
    /// </summary>
    private void ClearFields()
    {
        if (registerUsernameField != null) registerUsernameField.text = "";
        if (registerEmailField != null) registerEmailField.text = "";
        if (registerPasswordField != null) registerPasswordField.text = "";
        if (loginUsernameField != null) loginUsernameField.text = "";
        if (loginPasswordField != null) loginPasswordField.text = "";
    }
}
