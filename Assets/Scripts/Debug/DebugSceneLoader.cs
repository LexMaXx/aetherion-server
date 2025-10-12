using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Дебаг скрипт для быстрого тестирования сцен без прохождения авторизации
/// </summary>
public class DebugSceneLoader : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugMode = false; // ОТКЛЮЧЕН для production

    [Header("Test Character Settings")]
    [SerializeField] private string testCharacterClass = "Warrior";
    [SerializeField] private string testUsername = "TestPlayer";
    [SerializeField] private string testCharacterId = "test_character_id";

    [Header("Keyboard Shortcuts")]
    [SerializeField] private KeyCode loadArenaKey = KeyCode.F1;
    [SerializeField] private KeyCode loadGameSceneKey = KeyCode.F2;
    [SerializeField] private KeyCode loadCharacterSelectionKey = KeyCode.F3;

    void Update()
    {
        if (!enableDebugMode)
            return;

        // F1 - быстрая загрузка Arena с тестовым персонажем
        if (Input.GetKeyDown(loadArenaKey))
        {
            LoadArenaWithTestCharacter();
        }

        // F2 - быстрая загрузка GameScene с тестовым персонажем
        if (Input.GetKeyDown(loadGameSceneKey))
        {
            LoadGameSceneWithTestCharacter();
        }

        // F3 - быстрая загрузка CharacterSelection с тестовым токеном
        if (Input.GetKeyDown(loadCharacterSelectionKey))
        {
            LoadCharacterSelectionWithTestToken();
        }
    }

    /// <summary>
    /// Загрузить арену с тестовым персонажем
    /// </summary>
    public void LoadArenaWithTestCharacter()
    {
        SetupTestCharacter();
        Debug.Log($"[DEBUG] Загрузка Arena с персонажем: {testCharacterClass}");
        SceneManager.LoadScene("ArenaScene");
    }

    /// <summary>
    /// Загрузить GameScene с тестовым персонажем
    /// </summary>
    public void LoadGameSceneWithTestCharacter()
    {
        SetupTestCharacter();
        Debug.Log($"[DEBUG] Загрузка GameScene с персонажем: {testCharacterClass}");
        SceneManager.LoadScene("GameScene");
    }

    /// <summary>
    /// Загрузить CharacterSelection с тестовым токеном
    /// </summary>
    public void LoadCharacterSelectionWithTestToken()
    {
        // Устанавливаем тестовый токен
        PlayerPrefs.SetString("UserToken", "test_token_for_debugging");
        PlayerPrefs.Save();

        Debug.Log("[DEBUG] Загрузка CharacterSelectionScene с тестовым токеном");
        Debug.LogWarning("[DEBUG] ВНИМАНИЕ: Запросы к серверу не будут работать без реального токена!");

        SceneManager.LoadScene("CharacterSelectionScene");
    }

    /// <summary>
    /// Настроить тестового персонажа в PlayerPrefs
    /// </summary>
    private void SetupTestCharacter()
    {
        PlayerPrefs.SetString("SelectedCharacterId", testCharacterId);
        PlayerPrefs.SetString("SelectedCharacterClass", testCharacterClass);
        PlayerPrefs.SetString("UserToken", "test_token");
        PlayerPrefs.Save();

        Debug.Log($"[DEBUG] Установлены тестовые данные персонажа:");
        Debug.Log($"  - Class: {testCharacterClass}");
        Debug.Log($"  - Username: {testUsername}");
        Debug.Log($"  - CharacterId: {testCharacterId}");
    }

    /// <summary>
    /// Очистить все PlayerPrefs (сброс)
    /// </summary>
    public void ClearAllPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[DEBUG] Все PlayerPrefs очищены!");
    }

    // Unity Editor Gizmo для визуализации
    void OnGUI()
    {
        if (!enableDebugMode)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = Color.yellow;

        GUI.Label(new Rect(10, 10, 400, 100),
            "DEBUG MODE:\n" +
            $"F1 - Load Arena ({testCharacterClass})\n" +
            $"F2 - Load GameScene ({testCharacterClass})\n" +
            "F3 - Load CharacterSelection",
            style
        );
    }
}
