using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Менеджер игровой сцены - главное меню игры
/// Показывает выбранного персонажа и кнопку "В бой"
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image classIcon; // Иконка класса
    [SerializeField] private TextMeshProUGUI nicknameText; // Никнейм игрока
    [SerializeField] private TextMeshProUGUI levelText; // Уровень персонажа
    [SerializeField] private Button battleButton; // Кнопка "В бой"

    [Header("Class Icons")]
    [SerializeField] private Sprite warriorIcon;
    [SerializeField] private Sprite mageIcon;
    [SerializeField] private Sprite archerIcon;
    [SerializeField] private Sprite rogueIcon;
    [SerializeField] private Sprite paladinIcon;

    [Header("Settings")]
    [SerializeField] private string loadingSceneName = "LoadingScene";
    [SerializeField] private string arenaSceneName = "BattleScene"; // НОВАЯ СИСТЕМА: Теперь используем BattleScene

    private CharacterInfo currentCharacter;
    private string username;

    void Start()
    {
        SetupUI();
        LoadCharacterInfo();
        StartMusic();
    }

    /// <summary>
    /// Запустить фоновую музыку
    /// </summary>
    private void StartMusic()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMenuMusic();
        }
    }

    /// <summary>
    /// Настройка UI элементов
    /// </summary>
    private void SetupUI()
    {
        if (battleButton != null)
        {
            battleButton.onClick.AddListener(OnBattleButtonClick);
        }
    }

    /// <summary>
    /// Загрузить информацию о выбранном персонаже
    /// </summary>
    private void LoadCharacterInfo()
    {
        string token = PlayerPrefs.GetString("UserToken", "");
        string characterId = PlayerPrefs.GetString("SelectedCharacterId", "");
        string characterClass = PlayerPrefs.GetString("SelectedCharacterClass", "");

        Debug.Log($"[GameScene] LoadCharacterInfo: token={token}, characterId={characterId}, characterClass={characterClass}");

        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[GameScene] Нет токена! Возврат к LoginScene");
            SceneManager.LoadScene("LoginScene");
            return;
        }

        if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(characterClass))
        {
            Debug.LogError("[GameScene] Нет выбранного персонажа! Возврат к CharacterSelectionScene");
            SceneManager.LoadScene("CharacterSelectionScene");
            return;
        }

        // Сначала получаем информацию о пользователе
        ApiClient.Instance.VerifyToken(token,
            onSuccess: (response) =>
            {
                if (response.success && response.user != null)
                {
                    username = response.user.username;
                    Debug.Log($"[GameScene] Токен валидный, username: {username}");

                    // Теперь загружаем персонажа
                    LoadCharacter(token, characterClass);
                }
                else
                {
                    Debug.LogError($"[GameScene] Токен невалидный! Response: {response.message}");
                    SceneManager.LoadScene("LoginScene");
                }
            },
            onError: (error) =>
            {
                Debug.LogError($"[GameScene] Ошибка проверки токена: {error}");
                SceneManager.LoadScene("LoginScene");
            }
        );
    }

    /// <summary>
    /// Загрузить данные персонажа
    /// </summary>
    private void LoadCharacter(string token, string characterClass)
    {
        Debug.Log($"[GameScene] LoadCharacter: class={characterClass}");

        ApiClient.Instance.GetCharacters(token,
            (response) =>
            {
                Debug.Log($"[GameScene] GetCharacters response: success={response.success}, characters count={response.characters?.Length ?? 0}");

                if (response.success && response.characters != null)
                {
                    // Находим нужного персонажа
                    foreach (var character in response.characters)
                    {
                        Debug.Log($"[GameScene] Проверяем персонажа: {character.characterClass}");
                        if (character.characterClass == characterClass)
                        {
                            currentCharacter = character;
                            Debug.Log($"[GameScene] ✅ Персонаж найден: {characterClass}, Level {character.level}");
                            DisplayCharacterInfo();
                            return;
                        }
                    }

                    Debug.LogError($"[GameScene] ❌ Персонаж класса {characterClass} не найден!");
                    SceneManager.LoadScene("CharacterSelectionScene");
                }
                else
                {
                    Debug.LogError($"[GameScene] ❌ Ошибка загрузки персонажей! success={response.success}");
                    SceneManager.LoadScene("CharacterSelectionScene");
                }
            },
            (error) =>
            {
                Debug.LogError($"[GameScene] ❌ Ошибка загрузки персонажей: {error}");
                SceneManager.LoadScene("CharacterSelectionScene");
            }
        );
    }

    /// <summary>
    /// Отобразить информацию о персонаже
    /// </summary>
    private void DisplayCharacterInfo()
    {
        if (currentCharacter == null) return;

        // Устанавливаем иконку класса
        if (classIcon != null)
        {
            classIcon.sprite = GetClassIcon(currentCharacter.characterClass);
        }

        // Устанавливаем никнейм
        if (nicknameText != null)
        {
            nicknameText.text = username;
        }

        // Устанавливаем уровень
        if (levelText != null)
        {
            levelText.text = $"Уровень: {currentCharacter.level}";
        }

        Debug.Log($"GameScene загружена: {username} - {currentCharacter.characterClass} (Level {currentCharacter.level})");
    }

    /// <summary>
    /// Получить иконку класса
    /// </summary>
    private Sprite GetClassIcon(string characterClass)
    {
        switch (characterClass)
        {
            case "Warrior":
                return warriorIcon;
            case "Mage":
                return mageIcon;
            case "Archer":
                return archerIcon;
            case "Rogue":
                return rogueIcon;
            case "Paladin":
                return paladinIcon;
            default:
                Debug.LogWarning($"Неизвестный класс: {characterClass}");
                return null;
        }
    }

    /// <summary>
    /// Нажатие на кнопку "В бой"
    /// НОВАЯ СИСТЕМА: Использует MatchmakingManager для автоматического поиска/создания комнаты
    /// </summary>
    private void OnBattleButtonClick()
    {
        Debug.Log("[GameScene] Начинаем матчмейкинг!");

        // Disable button to prevent double-click
        if (battleButton != null)
        {
            battleButton.interactable = false;
        }

        // Проверяем наличие MatchmakingManager
        if (MatchmakingManager.Instance == null)
        {
            Debug.LogWarning("[GameScene] MatchmakingManager не найден, создаём...");
            GameObject matchmakingGO = new GameObject("MatchmakingManager");
            matchmakingGO.AddComponent<MatchmakingManager>();
        }

        // Запускаем автоматический поиск или создание комнаты
        MatchmakingManager.Instance.FindOrCreateMatch((success) =>
        {
            if (success)
            {
                Debug.Log("[GameScene] ✅ Матчмейкинг успешен! Загружаем BattleScene...");
                LoadArena();
            }
            else
            {
                Debug.LogError("[GameScene] ❌ Ошибка матчмейкинга");
                ReEnableBattleButton();
            }
        });
    }

    /// <summary>
    /// Загрузить арену
    /// </summary>
    private void LoadArena()
    {
        Debug.Log("[GameScene] Загрузка арены...");

        // Сохраняем целевую сцену для LoadingScreen
        PlayerPrefs.SetString("TargetScene", arenaSceneName);
        PlayerPrefs.Save();

        // Загружаем LoadingScene
        SceneManager.LoadScene(loadingSceneName);
    }

    /// <summary>
    /// Включить кнопку обратно (при ошибке)
    /// </summary>
    private void ReEnableBattleButton()
    {
        if (battleButton != null)
        {
            battleButton.interactable = true;
        }
    }

    /// <summary>
    /// Выход из аккаунта
    /// </summary>
    public void Logout()
    {
        PlayerPrefs.DeleteKey("UserToken");
        PlayerPrefs.DeleteKey("SelectedCharacterId");
        PlayerPrefs.DeleteKey("SelectedCharacterClass");
        PlayerPrefs.Save();
        SceneManager.LoadScene("LoginScene");
    }
}
