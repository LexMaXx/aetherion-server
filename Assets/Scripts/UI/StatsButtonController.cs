using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Контроллер кнопки для открытия панели статистики
/// Привязывается к UI кнопке в Inspector
/// </summary>
public class StatsButtonController : MonoBehaviour
{
    [Header("Выберите какую панель использовать")]
    [Tooltip("SimpleStatsUI - создаётся автоматически программно")]
    [SerializeField] private bool useSimpleStatsUI = true;

    [Header("Ссылки (опционально)")]
    [Tooltip("Если не заполнено - будет искать автоматически")]
    [SerializeField] private SimpleStatsUI simpleStatsUI;

    [Tooltip("Если не заполнено - будет искать автоматически")]
    [SerializeField] private CharacterStatsPanel characterStatsPanel;

    private Button button;

    void Start()
    {
        Debug.Log("[StatsButtonController] ==================== START ====================");
        Debug.Log($"[StatsButtonController] Инициализация на объекте: {gameObject.name}");
        Debug.Log($"[StatsButtonController] useSimpleStatsUI = {useSimpleStatsUI}");

        // Получаем компонент Button
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"[StatsButtonController] ❌ На объекте {gameObject.name} нет компонента Button!");
            return;
        }

        Debug.Log($"[StatsButtonController] ✅ Найден компонент Button на {gameObject.name}");

        // Добавляем обработчик нажатия
        button.onClick.AddListener(OnButtonClick);
        Debug.Log("[StatsButtonController] ✅ Обработчик нажатия добавлен");

        // Ищем панель статистики
        FindStatsPanel();
    }

    /// <summary>
    /// Найти панель статистики
    /// </summary>
    private void FindStatsPanel()
    {
        if (useSimpleStatsUI)
        {
            // Используем SimpleStatsUI
            if (simpleStatsUI == null)
            {
                Debug.Log("[StatsButtonController] 🔍 Ищем SimpleStatsUI...");
                simpleStatsUI = FindFirstObjectByType<SimpleStatsUI>();

                if (simpleStatsUI == null)
                {
                    Debug.LogWarning("[StatsButtonController] ⚠️ SimpleStatsUI не найден в сцене!");
                    Debug.LogWarning("[StatsButtonController] 💡 РЕШЕНИЕ: Добавь SimpleStatsUI компонент на любой GameObject в сцене");
                    Debug.LogWarning("[StatsButtonController] 💡 Он создаст панель автоматически при запуске игры");
                }
                else
                {
                    Debug.Log($"[StatsButtonController] ✅ SimpleStatsUI найден на: {simpleStatsUI.gameObject.name}");
                }
            }
            else
            {
                Debug.Log($"[StatsButtonController] ✅ SimpleStatsUI уже привязан: {simpleStatsUI.gameObject.name}");
            }
        }
        else
        {
            // Используем CharacterStatsPanel
            if (characterStatsPanel == null)
            {
                Debug.Log("[StatsButtonController] 🔍 Ищем CharacterStatsPanel...");
                characterStatsPanel = FindFirstObjectByType<CharacterStatsPanel>();

                if (characterStatsPanel == null)
                {
                    Debug.LogWarning("[StatsButtonController] ⚠️ CharacterStatsPanel не найден в сцене!");
                    Debug.LogWarning("[StatsButtonController] 💡 РЕШЕНИЕ: Используй Tools → Aetherion → Create Character Stats Panel");
                }
                else
                {
                    Debug.Log($"[StatsButtonController] ✅ CharacterStatsPanel найден на: {characterStatsPanel.gameObject.name}");
                }
            }
            else
            {
                Debug.Log($"[StatsButtonController] ✅ CharacterStatsPanel уже привязан: {characterStatsPanel.gameObject.name}");
            }
        }
    }

    /// <summary>
    /// Обработчик нажатия на кнопку
    /// </summary>
    private void OnButtonClick()
    {
        Debug.Log($"[StatsButtonController] 🖱️ Кнопка {gameObject.name} нажата!");

        if (useSimpleStatsUI)
        {
            // Открываем SimpleStatsUI
            if (simpleStatsUI != null)
            {
                Debug.Log("[StatsButtonController] Открываем SimpleStatsUI...");
                simpleStatsUI.Toggle();
            }
            else
            {
                Debug.LogError("[StatsButtonController] ❌ SimpleStatsUI не найден! Панель не откроется.");
                Debug.LogError("[StatsButtonController] 💡 Добавь SimpleStatsUI компонент в сцену!");
            }
        }
        else
        {
            // Открываем CharacterStatsPanel
            if (characterStatsPanel != null)
            {
                Debug.Log("[StatsButtonController] Открываем CharacterStatsPanel...");
                characterStatsPanel.Toggle();
            }
            else
            {
                Debug.LogError("[StatsButtonController] ❌ CharacterStatsPanel не найден! Панель не откроется.");
                Debug.LogError("[StatsButtonController] 💡 Создай панель через Tools → Aetherion → Create Character Stats Panel");
            }
        }
    }

    /// <summary>
    /// Публичный метод для ручного открытия (можно вызвать из Inspector)
    /// </summary>
    public void OpenStatsPanel()
    {
        OnButtonClick();
    }
}
