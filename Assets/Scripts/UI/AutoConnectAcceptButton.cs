using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Автоматически подключает AcceptButton к WorldMapPlayerController
/// Поместите этот скрипт на кнопку AcceptButton в WorldMapScene
/// </summary>
[RequireComponent(typeof(Button))]
public class AutoConnectAcceptButton : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Искать игрока при старте?")]
    [SerializeField] private bool connectOnStart = true;

    [Tooltip("Повторять поиск если игрок не найден?")]
    [SerializeField] private bool retryIfNotFound = true;

    [Tooltip("Интервал повтора (секунды)")]
    [SerializeField] private float retryInterval = 0.5f;

    private Button button;
    private bool isConnected = false;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void Start()
    {
        if (connectOnStart)
        {
            // Задержка чтобы ForceSpawnCharacter успел создать персонажа
            Invoke(nameof(TryConnect), 0.5f);
        }
    }

    /// <summary>
    /// Попытка подключения к игроку
    /// </summary>
    [ContextMenu("Try Connect to Player")]
    public void TryConnect()
    {
        if (isConnected)
        {
            Debug.Log("[AutoConnectAcceptButton] ✅ Уже подключена");
            return;
        }

        WorldMapPlayerController player = FindObjectOfType<WorldMapPlayerController>();

        if (player != null)
        {
            // Используем рефлексию для подключения к приватному полю
            var field = typeof(WorldMapPlayerController).GetField("acceptButton",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(player, button);
                isConnected = true;
                Debug.Log("[AutoConnectAcceptButton] ✅ AcceptButton автоматически подключена к игроку");

                // ВАЖНО: Добавляем onClick listener ВРУЧНУЮ
                // т.к. WorldMapPlayerController.Start() выполнился когда acceptButton был null
                var onAcceptMethod = typeof(WorldMapPlayerController).GetMethod("OnAcceptButtonPressed",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (onAcceptMethod != null)
                {
                    button.onClick.AddListener(() =>
                    {
                        onAcceptMethod.Invoke(player, null);
                    });
                    Debug.Log("[AutoConnectAcceptButton] ✅ onClick listener добавлен");
                }
                else
                {
                    Debug.LogError("[AutoConnectAcceptButton] ❌ Метод OnAcceptButtonPressed не найден!");
                }

                // Скрываем кнопку (WorldMapPlayerController покажет при приближении)
                gameObject.SetActive(false);
                Debug.Log("[AutoConnectAcceptButton] ✅ Кнопка готова к использованию");
            }
            else
            {
                Debug.LogError("[AutoConnectAcceptButton] ❌ Поле 'acceptButton' не найдено в WorldMapPlayerController");
            }
        }
        else
        {
            Debug.LogWarning("[AutoConnectAcceptButton] ⚠️ WorldMapPlayerController не найден в сцене");

            if (retryIfNotFound)
            {
                Debug.Log($"[AutoConnectAcceptButton] 🔄 Повторная попытка через {retryInterval}с...");
                Invoke(nameof(TryConnect), retryInterval);
            }
        }
    }

    /// <summary>
    /// Проверка подключения
    /// </summary>
    [ContextMenu("Check Connection")]
    public void CheckConnection()
    {
        if (isConnected)
        {
            Debug.Log("[AutoConnectAcceptButton] ✅ Подключена");
        }
        else
        {
            Debug.LogWarning("[AutoConnectAcceptButton] ❌ НЕ подключена");
        }

        WorldMapPlayerController player = FindObjectOfType<WorldMapPlayerController>();
        if (player != null)
        {
            Debug.Log("[AutoConnectAcceptButton] ✅ WorldMapPlayerController найден в сцене");
        }
        else
        {
            Debug.LogWarning("[AutoConnectAcceptButton] ⚠️ WorldMapPlayerController НЕ найден в сцене");
        }
    }
}
