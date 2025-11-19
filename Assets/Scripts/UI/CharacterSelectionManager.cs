using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Менеджер выбора персонажа - управляет UI и загрузкой персонажей
/// </summary>
public class CharacterSelectionManager : MonoBehaviour
{
    [Header("UI References - Left Panel")]
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private RawImage characterViewport;

    [Header("UI References - Class Icons")]
    [SerializeField] private Button warriorIcon;
    [SerializeField] private Button mageIcon;
    [SerializeField] private Button archerIcon;
    [SerializeField] private Button rogueIcon;
    [SerializeField] private Button paladinIcon;

    [Header("UI References - Description Panel")]
    [SerializeField] private GameObject classDescriptionPanel;
    [SerializeField] private TextMeshProUGUI descriptionTitle;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI classStatsText;

    [Header("UI References - Progress/New Panels")]
    [SerializeField] private GameObject progressPanel;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private GameObject newCharacterPanel;

    [Header("UI References - Bottom Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button settingsButton;

    [Header("3D Character Models")]
    [SerializeField] private GameObject warriorModel;
    [SerializeField] private GameObject mageModel;
    [SerializeField] private GameObject archerModel;
    [SerializeField] private GameObject rogueModel;
    [SerializeField] private GameObject paladinModel;
    [SerializeField] private float rotationSpeed = 100f;

    [Header("Settings")]
    [SerializeField] private string loadingSceneName = "LoadingScene";
    [SerializeField] private string gameSceneName = "GameScene";

    private GameObject currentCharacterModel;

    // Текущий выбранный класс
    private CharacterClass selectedClass = CharacterClass.Warrior;
    private Dictionary<CharacterClass, CharacterInfo> charactersData = new Dictionary<CharacterClass, CharacterInfo>();
    private bool isLoading = false;

    // Публичный геттер для выбранного класса
    public CharacterClass GetSelectedClass()
    {
        return selectedClass;
    }

    // Описания классов
    private Dictionary<CharacterClass, ClassDescription> classDescriptions = new Dictionary<CharacterClass, ClassDescription>();

    void Start()
    {
        InitializeClassDescriptions();
        SetupButtonListeners();
        LoadCharacters();
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
    /// Инициализация описаний классов
    /// </summary>
    private void InitializeClassDescriptions()
    {
        classDescriptions[CharacterClass.Warrior] = new ClassDescription
        {
            title = "ВОИН",
            description = "Мастер ближнего боя. Высокая защита и урон мечом.",
            stats = "Сила: 15 | Ловкость: 8 | Интеллект: 5"
        };

        classDescriptions[CharacterClass.Mage] = new ClassDescription
        {
            title = "МАГ",
            description = "Повелитель магии стихий. Разрушительные заклинания.",
            stats = "Сила: 5 | Ловкость: 7 | Интеллект: 18"
        };

        classDescriptions[CharacterClass.Archer] = new ClassDescription
        {
            title = "ЛУЧНИК",
            description = "Снайпер дальнего боя. Точность и скорость.",
            stats = "Сила: 8 | Ловкость: 16 | Интеллект: 7"
        };

        classDescriptions[CharacterClass.Rogue] = new ClassDescription
        {
            title = "РАЗБОЙНИК",
            description = "Мастер скрытности. Критические удары из тени.",
            stats = "Сила: 10 | Ловкость: 15 | Интеллект: 8"
        };

        classDescriptions[CharacterClass.Paladin] = new ClassDescription
        {
            title = "ПАЛАДИН",
            description = "Святой воин. Защита союзников и исцеление.",
            stats = "Сила: 12 | Ловкость: 8 | Интеллект: 10"
        };
    }

    /// <summary>
    /// Настройка обработчиков кнопок
    /// </summary>
    private void SetupButtonListeners()
    {
        Debug.Log("[SetupButtonListeners] Настройка кнопок...");

        // Иконки классов
        if (warriorIcon != null)
        {
            warriorIcon.onClick.AddListener(() => SelectClass(CharacterClass.Warrior));
            Debug.Log("[SetupButtonListeners] Warrior кнопка подключена");
        }
        else Debug.LogError("[SetupButtonListeners] warriorIcon = NULL!");

        if (mageIcon != null)
        {
            mageIcon.onClick.AddListener(() => SelectClass(CharacterClass.Mage));
            Debug.Log("[SetupButtonListeners] Mage кнопка подключена");
        }
        else Debug.LogError("[SetupButtonListeners] mageIcon = NULL!");

        if (archerIcon != null)
        {
            archerIcon.onClick.AddListener(() => SelectClass(CharacterClass.Archer));
            Debug.Log("[SetupButtonListeners] Archer кнопка подключена");
        }
        else Debug.LogError("[SetupButtonListeners] archerIcon = NULL!");

        if (rogueIcon != null)
        {
            rogueIcon.onClick.AddListener(() => SelectClass(CharacterClass.Rogue));
            Debug.Log("[SetupButtonListeners] Rogue кнопка подключена");
        }
        else Debug.LogError("[SetupButtonListeners] rogueIcon = NULL!");

        if (paladinIcon != null)
        {
            paladinIcon.onClick.AddListener(() => SelectClass(CharacterClass.Paladin));
            Debug.Log("[SetupButtonListeners] Paladin кнопка подключена");
        }
        else Debug.LogError("[SetupButtonListeners] paladinIcon = NULL!");

        // Нижние кнопки
        playButton?.onClick.AddListener(OnPlayButtonClick);
        backButton?.onClick.AddListener(OnBackButtonClick);
        settingsButton?.onClick.AddListener(OnSettingsButtonClick);
    }

    /// <summary>
    /// Загрузить список персонажей с сервера
    /// </summary>
    private void LoadCharacters()
    {
        string token = PlayerPrefs.GetString("UserToken", "");

        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("Нет токена! Возвращаемся к логину.");
            SceneManager.LoadScene("LoginScene");
            return;
        }

        isLoading = true;

        ApiClient.Instance.GetCharacters(token,
            onSuccess: (response) =>
            {
                isLoading = false;

                if (response.success && response.characters != null)
                {
                    // Сохраняем данные персонажей
                    charactersData.Clear();
                    foreach (var character in response.characters)
                    {
                        if (System.Enum.TryParse(character.characterClass, true, out CharacterClass characterClass))
                        {
                            charactersData[characterClass] = character;
                        }
                    }

                    Debug.Log($"Загружено {charactersData.Count} персонажей");
                }

                // Если есть персонажи - показываем первого из них
                // Если нет - показываем Warrior для создания
                CharacterClass classToShow = CharacterClass.Warrior;

                if (charactersData.Count > 0)
                {
                    // Показываем первого существующего персонажа
                    classToShow = charactersData.Keys.First();
                    Debug.Log($"Найден существующий персонаж: {classToShow}");
                }
                else
                {
                    Debug.Log("Персонажей нет, показываем Warrior для создания");
                }

                SelectClass(classToShow);
            },
            onError: (error) =>
            {
                isLoading = false;
                Debug.LogError($"Ошибка загрузки персонажей: {error}");
                // Показываем первого персонажа даже при ошибке
                SelectClass(CharacterClass.Warrior);
            }
        );
    }

    /// <summary>
    /// Выбрать класс персонажа
    /// </summary>
    private void SelectClass(CharacterClass characterClass)
    {
        selectedClass = characterClass;

        // Уведомляем SkillSelectionManager о смене класса
        SkillSelectionManager skillManager = FindObjectOfType<SkillSelectionManager>();
        if (skillManager != null)
        {
            Debug.Log($"[CharacterSelectionManager] Уведомляю SkillSelectionManager о смене класса на {characterClass}");
            skillManager.LoadSkillsForClass(characterClass);
        }
        else
        {
            Debug.LogWarning("[CharacterSelectionManager] SkillSelectionManager не найден в сцене!");
        }

        // Обновляем название класса
        if (characterNameText != null)
        {
            characterNameText.text = classDescriptions[characterClass].title;
        }

        // Обновляем описание класса
        if (descriptionTitle != null)
        {
            descriptionTitle.text = classDescriptions[characterClass].title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = classDescriptions[characterClass].description;
        }

        if (classStatsText != null)
        {
            classStatsText.text = classDescriptions[characterClass].stats;
        }

        // Проверяем существует ли персонаж этого класса
        bool characterExists = charactersData.ContainsKey(characterClass);

        // Показываем соответствующую панель
        if (progressPanel != null)
        {
            progressPanel.SetActive(characterExists);
        }

        if (newCharacterPanel != null)
        {
            newCharacterPanel.SetActive(!characterExists);
        }

        // Если персонаж существует - показываем его данные
        if (characterExists)
        {
            CharacterInfo charData = charactersData[characterClass];

            if (levelText != null)
            {
                levelText.text = $"Уровень: {charData.level}";
            }

            if (goldText != null)
            {
                goldText.text = $"Золото: {charData.gold}";
            }
        }

        // Показать 3D модель персонажа
        ShowCharacterModel(characterClass);
        Debug.Log($"Выбран класс: {characterClass}");
    }

    /// <summary>
    /// Нажатие на кнопку "Начать игру"
    /// </summary>
    private void OnPlayButtonClick()
    {
        if (isLoading) return;

        string token = PlayerPrefs.GetString("UserToken", "");

        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("Нет токена!");
            return;
        }

        isLoading = true;

        // Отправляем запрос на выбор/создание персонажа
        ApiClient.Instance.SelectOrCreateCharacter(token, selectedClass,
            onSuccess: (response) =>
            {
                isLoading = false;

                if (response.success && response.character != null)
                {
                    // Сохраняем ID выбранного персонажа
                    PlayerPrefs.SetString("SelectedCharacterId", response.character.id);
                    PlayerPrefs.SetString("SelectedCharacterClass", response.character.characterClass);

                    // Сохраняем целевую сцену для LoadingScreen
                    PlayerPrefs.SetString("TargetScene", gameSceneName);

                    // Сохраняем экипированные скиллы
                    SkillSelectionManager skillManager = FindObjectOfType<SkillSelectionManager>();
                    if (skillManager != null)
                    {
                        skillManager.SaveEquippedSkillsToPlayerPrefs();
                    }
                    else
                    {
                        Debug.LogWarning("[CharacterSelectionManager] SkillSelectionManager не найден! Скиллы не сохранены.");
                    }

                    PlayerPrefs.Save();

                    Debug.Log($"Персонаж выбран: {response.character.characterClass}, Level {response.character.level}");

                    // Загружаем сцену загрузки
                    SceneManager.LoadScene(loadingSceneName);
                }
                else
                {
                    Debug.LogError($"Ошибка выбора персонажа: {response.message}");
                }
            },
            onError: (error) =>
            {
                isLoading = false;
                Debug.LogError($"Ошибка выбора персонажа: {error}");

                // Показываем понятное сообщение пользователю
                string userMessage = error;

                // Если это персонаж который уже существует
                bool characterExists = charactersData.ContainsKey(selectedClass);
                if (characterExists)
                {
                    userMessage = $"Не удалось загрузить персонажа {selectedClass}. Попробуйте еще раз или выберите другой класс.";
                }
                else
                {
                    userMessage = $"Не удалось создать персонажа {selectedClass}. Возможно этот класс уже создан. Обновите страницу.";
                }

                // TODO: Показать UI сообщение об ошибке
                Debug.LogWarning($"Сообщение пользователю: {userMessage}");
            }
        );
    }

    /// <summary>
    /// Нажатие на кнопку "Назад"
    /// </summary>
    private void OnBackButtonClick()
    {
        PlayerPrefs.DeleteKey("UserToken");
        PlayerPrefs.Save();
        SceneManager.LoadScene("LoginScene");
    }

    /// <summary>
    /// Нажатие на кнопку "Настройки"
    /// </summary>
    private void OnSettingsButtonClick()
    {
        Debug.Log("Настройки - пока не реализовано");
        // TODO: Открыть панель настроек
    }

    /// <summary>
    /// Показать 3D модель выбранного персонажа
    /// </summary>
    private void ShowCharacterModel(CharacterClass characterClass)
    {
        Debug.Log($"[ShowCharacterModel] Выбран класс: {characterClass}");

        // Скрываем текущую модель
        if (currentCharacterModel != null)
        {
            Debug.Log($"[ShowCharacterModel] Скрываем текущую модель: {currentCharacterModel.name}");
            currentCharacterModel.SetActive(false);
        }

        // Показываем нужную модель
        switch (characterClass)
        {
            case CharacterClass.Warrior:
                currentCharacterModel = warriorModel;
                break;
            case CharacterClass.Mage:
                currentCharacterModel = mageModel;
                break;
            case CharacterClass.Archer:
                currentCharacterModel = archerModel;
                break;
            case CharacterClass.Rogue:
                currentCharacterModel = rogueModel;
                break;
            case CharacterClass.Paladin:
                currentCharacterModel = paladinModel;
                break;
        }

        if (currentCharacterModel != null)
        {
            Debug.Log($"[ShowCharacterModel] Активируем модель: {currentCharacterModel.name}");
            currentCharacterModel.SetActive(true);

            // Устанавливаем боевую стойку по умолчанию
            Animator animator = currentCharacterModel.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("InBattle", true);
                Debug.Log($"[ShowCharacterModel] Установлена боевая стойка для {currentCharacterModel.name}");
            }

            // Добавляем оружие если еще не добавлено
            ClassWeaponManager weaponManager = currentCharacterModel.GetComponent<ClassWeaponManager>();
            if (weaponManager == null)
            {
                weaponManager = currentCharacterModel.AddComponent<ClassWeaponManager>();
                weaponManager.AttachWeaponForClass();
                Debug.Log($"[ShowCharacterModel] Добавлено оружие для {currentCharacterModel.name}");
            }
        }
        else
        {
            Debug.LogError($"[ShowCharacterModel] МОДЕЛЬ НЕ НАЗНАЧЕНА для класса {characterClass}!");
        }
    }

    /// <summary>
    /// Вращение персонажа при зажатой ЛКМ
    /// </summary>
    void Update()
    {
        if (currentCharacterModel != null && Input.GetMouseButton(0))
        {
            float rotationX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            currentCharacterModel.transform.Rotate(Vector3.up, -rotationX, Space.World);
        }
    }
}

/// <summary>
/// Описание класса для UI
/// </summary>
[System.Serializable]
public class ClassDescription
{
    public string title;
    public string description;
    public string stats;
}
