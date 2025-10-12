using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Быстрый старт для тестирования - пропускает LoginScene
/// Добавьте этот скрипт на любой GameObject в любой сцене
/// </summary>
public class QuickStart : MonoBehaviour
{
    [Header("Quick Start Settings")]
    [SerializeField] private bool enableQuickStart = false; // ОТКЛЮЧЕН для production
    [SerializeField] private KeyCode quickStartKey = KeyCode.F5;

    [Header("Test Account")]
    [SerializeField] private string testToken = "test_token_12345";
    [SerializeField] private string testUsername = "TestPlayer";

    [Header("Scene to Load")]
    [SerializeField] private string targetScene = "CharacterSelectionScene";

    void Update()
    {
        if (!enableQuickStart)
            return;

        // F5 - быстрый переход к CharacterSelectionScene
        if (Input.GetKeyDown(quickStartKey))
        {
            QuickStartToCharacterSelection();
        }
    }

    void Start()
    {
        // Автоматический быстрый старт если находимся в IntroScene или LoginScene
        string currentScene = SceneManager.GetActiveScene().name;

        if (enableQuickStart && (currentScene == "IntroScene" || currentScene == "LoginScene"))
        {
            Debug.Log("[QuickStart] Автоматический переход к CharacterSelectionScene через 1 секунду...");
            Invoke(nameof(QuickStartToCharacterSelection), 1f);
        }
    }

    /// <summary>
    /// Быстрый переход к выбору персонажа
    /// </summary>
    public void QuickStartToCharacterSelection()
    {
        Debug.Log("[QuickStart] Установка тестового токена и переход к CharacterSelectionScene");

        // Устанавливаем тестовый токен
        PlayerPrefs.SetString("UserToken", testToken);
        PlayerPrefs.SetString("Username", testUsername);
        PlayerPrefs.Save();

        // Загружаем сцену выбора персонажа
        SceneManager.LoadScene(targetScene);
    }

    /// <summary>
    /// Быстрый переход к арене с выбранным классом
    /// </summary>
    public void QuickStartToArena(string characterClass)
    {
        Debug.Log($"[QuickStart] Быстрый переход к арене с классом: {characterClass}");

        // Устанавливаем данные персонажа
        PlayerPrefs.SetString("UserToken", testToken);
        PlayerPrefs.SetString("SelectedCharacterClass", characterClass);
        PlayerPrefs.SetString("SelectedCharacterId", $"test_{characterClass}_id");
        PlayerPrefs.Save();

        // Загружаем арену
        SceneManager.LoadScene("ArenaScene");
    }

    /// <summary>
    /// Очистить все PlayerPrefs
    /// </summary>
    public void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[QuickStart] Все PlayerPrefs очищены!");
    }

    void OnGUI()
    {
        if (!enableQuickStart)
            return;

        // GUI для быстрого доступа
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 12;
        style.normal.textColor = Color.yellow;
        style.alignment = TextAnchor.MiddleLeft;

        GUI.Box(new Rect(10, 10, 300, 150), "", style);

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 14;
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontStyle = FontStyle.Bold;

        GUI.Label(new Rect(20, 15, 280, 25), "QUICK START MODE", labelStyle);

        labelStyle.fontSize = 11;
        labelStyle.fontStyle = FontStyle.Normal;

        GUI.Label(new Rect(20, 40, 280, 20), $"F5 - {targetScene}", labelStyle);
        GUI.Label(new Rect(20, 60, 280, 20), "F6 - Arena (Warrior)", labelStyle);
        GUI.Label(new Rect(20, 80, 280, 20), "F7 - Arena (Mage)", labelStyle);
        GUI.Label(new Rect(20, 100, 280, 20), "F8 - Clear PlayerPrefs", labelStyle);

        // Дополнительные кнопки
        if (Input.GetKeyDown(KeyCode.F6))
        {
            QuickStartToArena("Warrior");
        }

        if (Input.GetKeyDown(KeyCode.F7))
        {
            QuickStartToArena("Mage");
        }

        if (Input.GetKeyDown(KeyCode.F8))
        {
            ClearPlayerPrefs();
        }
    }

    /// <summary>
    /// Показать текущие PlayerPrefs в консоли
    /// </summary>
    [ContextMenu("Show PlayerPrefs")]
    public void ShowPlayerPrefs()
    {
        Debug.Log("=== CURRENT PLAYERPREFS ===");
        Debug.Log($"UserToken: {PlayerPrefs.GetString("UserToken", "NOT SET")}");
        Debug.Log($"Username: {PlayerPrefs.GetString("Username", "NOT SET")}");
        Debug.Log($"SelectedCharacterId: {PlayerPrefs.GetString("SelectedCharacterId", "NOT SET")}");
        Debug.Log($"SelectedCharacterClass: {PlayerPrefs.GetString("SelectedCharacterClass", "NOT SET")}");
        Debug.Log("===========================");
    }
}
