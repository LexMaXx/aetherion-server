using UnityEngine;

/// <summary>
/// Автоматически создаёт необходимые сетевые компоненты при старте игры
/// Добавь этот скрипт на любой GameObject в первой сцене (например MainMenu)
/// </summary>
public class NetworkInitializer : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool autoCreateNetworkManagers = true;

    void Awake()
    {
        if (!autoCreateNetworkManagers) return;

        // Создаём SimpleWebSocketClient если его нет (работает БЕЗ внешних зависимостей)
        if (SimpleWebSocketClient.Instance == null)
        {
            GameObject wsClient = new GameObject("SimpleWebSocketClient");
            wsClient.AddComponent<SimpleWebSocketClient>();
            DontDestroyOnLoad(wsClient);
            Debug.Log("[NetworkInitializer] ✅ Создан SimpleWebSocketClient");
        }

        // Создаём RoomManager если его нет
        if (RoomManager.Instance == null)
        {
            GameObject roomMgr = new GameObject("RoomManager");
            roomMgr.AddComponent<RoomManager>();
            DontDestroyOnLoad(roomMgr);
            Debug.Log("[NetworkInitializer] ✅ Создан RoomManager");
        }

        // Создаём ApiClient если его нет
        if (ApiClient.Instance == null)
        {
            GameObject apiClient = new GameObject("ApiClient");
            apiClient.AddComponent<ApiClient>();
            DontDestroyOnLoad(apiClient);
            Debug.Log("[NetworkInitializer] ✅ Создан ApiClient");
        }

        Debug.Log("[NetworkInitializer] 🚀 Все сетевые менеджеры готовы!");
    }
}
