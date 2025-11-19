using UnityEngine;

/// <summary>
/// Быстрая настройка для тестирования инвентаря
/// Устанавливает необходимые PlayerPrefs значения для работы системы
/// </summary>
public class QuickTestSetup : MonoBehaviour
{
    [Header("Test User Settings")]
    [Tooltip("Имя пользователя для тестирования")]
    public string testUsername = "TestUser";

    [Tooltip("JWT токен (оставьте пустым для автогенерации)")]
    public string testJWTToken = "";

    [Tooltip("Класс персонажа")]
    public string testCharacterClass = "Warrior";

    [Header("Auto Setup")]
    [Tooltip("Автоматически настраивать при старте")]
    public bool autoSetupOnStart = true;

    [Tooltip("Показывать UI с инструкциями")]
    public bool showInstructionsUI = true;

    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupTestEnvironment();
        }
    }

    /// <summary>
    /// Настраивает тестовое окружение
    /// </summary>
    public void SetupTestEnvironment()
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("[QuickTestSetup] 🔧 НАСТРОЙКА ТЕСТОВОГО ОКРУЖЕНИЯ");
        Debug.Log("═══════════════════════════════════════════");

        // 1. Устанавливаем username
        if (!string.IsNullOrEmpty(testUsername))
        {
            PlayerPrefs.SetString("Username", testUsername);
            Debug.Log($"✅ Username установлен: {testUsername}");
        }

        // 2. Устанавливаем класс персонажа
        if (!string.IsNullOrEmpty(testCharacterClass))
        {
            PlayerPrefs.SetString("SelectedCharacterClass", testCharacterClass);
            Debug.Log($"✅ Character Class установлен: {testCharacterClass}");
        }

        // 3. Генерируем или устанавливаем JWT токен
        if (string.IsNullOrEmpty(testJWTToken))
        {
            // Генерируем фейковый JWT токен для тестирования
            // ВНИМАНИЕ: Это НЕ настоящий JWT, только для локального тестирования!
            testJWTToken = GenerateFakeJWT(testUsername);
            Debug.LogWarning("⚠️ Сгенерирован ФЕЙКОВЫЙ JWT токен для тестирования");
            Debug.LogWarning("⚠️ Для реального подключения пройдите через LoginScene!");
        }

        PlayerPrefs.SetString("UserToken", testJWTToken);
        Debug.Log($"✅ JWT Token установлен (длина: {testJWTToken.Length} символов)");

        // 4. Сохраняем
        PlayerPrefs.Save();

        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("✅✅✅ НАСТРОЙКА ЗАВЕРШЕНА");
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("СЛЕДУЮЩИЕ ШАГИ:");
        Debug.Log("1. Для РЕАЛЬНОГО тестирования: Пройдите через LoginScene");
        Debug.Log("2. Для ЛОКАЛЬНОГО тестирования: Используйте ImprovedInventoryTester");
        Debug.Log("3. Нажмите F8 для проверки системы");
        Debug.Log("═══════════════════════════════════════════");
    }

    /// <summary>
    /// Генерирует фейковый JWT токен (только для локального тестирования UI)
    /// </summary>
    private string GenerateFakeJWT(string username)
    {
        // Формат JWT: header.payload.signature
        // Это НЕ настоящий JWT, только для тестирования UI!
        string header = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
        string payload = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{{\"user\":{{\"username\":\"{username}\"}}}}"));
        string signature = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("fake_signature_for_testing"));

        return $"{header}.{payload}.{signature}";
    }

    /// <summary>
    /// Очищает все тестовые данные
    /// </summary>
    public void ClearTestEnvironment()
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("[QuickTestSetup] 🧹 ОЧИСТКА ТЕСТОВЫХ ДАННЫХ");
        Debug.Log("═══════════════════════════════════════════");

        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.DeleteKey("SelectedCharacterClass");
        PlayerPrefs.DeleteKey("UserToken");
        PlayerPrefs.Save();

        Debug.Log("✅ Все тестовые данные очищены");
        Debug.Log("═══════════════════════════════════════════");
    }

    /// <summary>
    /// Показывает текущие настройки
    /// </summary>
    public void ShowCurrentSettings()
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("[QuickTestSetup] 📋 ТЕКУЩИЕ НАСТРОЙКИ:");
        Debug.Log("═══════════════════════════════════════════");

        string username = PlayerPrefs.GetString("Username", "НЕ УСТАНОВЛЕН");
        string characterClass = PlayerPrefs.GetString("SelectedCharacterClass", "НЕ УСТАНОВЛЕН");
        string token = PlayerPrefs.GetString("UserToken", "");

        Debug.Log($"Username: {username}");
        Debug.Log($"Character Class: {characterClass}");
        Debug.Log($"JWT Token: {(string.IsNullOrEmpty(token) ? "НЕ УСТАНОВЛЕН" : $"Установлен (длина: {token.Length})")}");

        Debug.Log("═══════════════════════════════════════════");
    }

    void Update()
    {
        // Shift + F8 - повторная настройка
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F8))
        {
            SetupTestEnvironment();
        }

        // Shift + F9 - показать текущие настройки
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F9))
        {
            ShowCurrentSettings();
        }

        // Shift + F10 - очистить настройки
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F10))
        {
            ClearTestEnvironment();
        }
    }

    void OnGUI()
    {
        if (!showInstructionsUI)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 12;
        style.normal.textColor = Color.cyan;

        // Проверяем готовность
        bool hasUsername = !string.IsNullOrEmpty(PlayerPrefs.GetString("Username", ""));
        bool hasClass = !string.IsNullOrEmpty(PlayerPrefs.GetString("SelectedCharacterClass", ""));
        bool hasToken = !string.IsNullOrEmpty(PlayerPrefs.GetString("UserToken", ""));

        string status = (hasUsername && hasClass && hasToken)
            ? "✅ ТЕСТОВОЕ ОКРУЖЕНИЕ ГОТОВО"
            : "⚠️ ТРЕБУЕТСЯ НАСТРОЙКА";

        GUI.Label(new Rect(10, Screen.height - 150, 500, 150),
            $"QUICK TEST SETUP:\n\n" +
            $"Status: {status}\n" +
            $"Username: {(hasUsername ? "✅" : "❌")}\n" +
            $"Class: {(hasClass ? "✅" : "❌")}\n" +
            $"Token: {(hasToken ? "✅" : "❌")}\n\n" +
            $"Shift+F8 - Настроить\n" +
            $"Shift+F9 - Показать настройки\n" +
            $"Shift+F10 - Очистить",
            style
        );
    }
}
