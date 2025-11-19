using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Кнопка для перехода на карту мира из BattleScene
/// Автоматически сохраняет текущий класс персонажа перед переходом
/// </summary>
[RequireComponent(typeof(Button))]
public class WorldMapButton : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Имя сцены карты мира")]
    [SerializeField] private string worldMapSceneName = "WorldMapScene";

    [Tooltip("Использовать SceneTransitionManager для плавного перехода")]
    [SerializeField] private bool useTransitionManager = true;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void Start()
    {
        // Подключаем обработчик кнопки
        if (button != null)
        {
            button.onClick.AddListener(OnWorldMapButtonClick);
            Debug.Log("[WorldMapButton] ✅ Кнопка карты мира инициализирована");
        }
        else
        {
            Debug.LogError("[WorldMapButton] ❌ Button компонент не найден!");
        }
    }

    /// <summary>
    /// Обработчик нажатия кнопки карты мира
    /// </summary>
    private void OnWorldMapButtonClick()
    {
        Debug.Log("[WorldMapButton] 🗺️ Переход на карту мира...");

        // Получаем выбранный класс из PlayerPrefs
        string selectedClass = PlayerPrefs.GetString("SelectedCharacterClass", "");

        if (string.IsNullOrEmpty(selectedClass))
        {
            Debug.LogError("[WorldMapButton] ❌ Класс персонажа не выбран!");
            Debug.LogError("[WorldMapButton] 💡 Сначала выберите класс персонажа через Character Selection");
            return;
        }

        Debug.Log($"[WorldMapButton] 📋 Текущий класс: {selectedClass}");

        // КРИТИЧЕСКИ ВАЖНО: Регистрируем персонажа в GameProgressManager
        RegisterCurrentCharacter(selectedClass);

        // Сохраняем последнюю локацию
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.SetLastLocation("BattleScene");
            Debug.Log("[WorldMapButton] 💾 BattleScene сохранён как последняя локация");
        }

        // Переходим на карту мира
        LoadWorldMapScene();
    }

    /// <summary>
    /// Зарегистрировать текущего персонажа в GameProgressManager
    /// </summary>
    private void RegisterCurrentCharacter(string selectedClass)
    {
        if (GameProgressManager.Instance == null)
        {
            Debug.LogWarning("[WorldMapButton] ⚠️ GameProgressManager не найден - персонаж не будет сохранён");
            return;
        }

        // Формируем имя префаба: "Warrior" → "WarriorModel"
        string prefabName = $"{selectedClass}Model";

        // Сохраняем в GameProgressManager
        GameProgressManager.Instance.SetSelectedCharacter(prefabName);

        Debug.Log($"[WorldMapButton] ✅ Персонаж зарегистрирован: {prefabName}");
        Debug.Log($"[WorldMapButton] 🗺️ Этот персонаж появится на карте мира");
    }

    /// <summary>
    /// Загрузить сцену карты мира
    /// </summary>
    private void LoadWorldMapScene()
    {
        if (useTransitionManager && SceneTransitionManager.Instance != null)
        {
            // Плавный переход с fade-эффектом
            SceneTransitionManager.Instance.LoadScene(worldMapSceneName);
            Debug.Log($"[WorldMapButton] ✨ Плавный переход в {worldMapSceneName}");
        }
        else
        {
            // Прямая загрузка
            UnityEngine.SceneManagement.SceneManager.LoadScene(worldMapSceneName);
            Debug.Log($"[WorldMapButton] ⚡ Прямой переход в {worldMapSceneName}");
        }
    }

    /// <summary>
    /// Публичный метод для вызова из UI кнопки (альтернатива onClick)
    /// </summary>
    public void GoToWorldMap()
    {
        OnWorldMapButtonClick();
    }
}
