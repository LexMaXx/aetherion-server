using UnityEngine;

/// <summary>
/// Автоматически создаёт необходимые сетевые компоненты при старте ЛЮБОЙ сцены
/// Использует [RuntimeInitializeOnLoadMethod] для автоматического запуска
/// НЕ ТРЕБУЕТ добавления GameObject в сцену - работает полностью автоматически!
/// </summary>
public class NetworkInitializer : MonoBehaviour
{
    /// <summary>
    /// КРИТИЧНО: Автоматически вызывается Unity ДО загрузки ЛЮБОЙ первой сцены
    /// Гарантирует что SocketIOManager существует даже если запускаешь BattleScene напрямую из редактора
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        Debug.LogError("🔥🔥🔥 [NetworkInitializer] АВТОМАТИЧЕСКАЯ ИНИЦИАЛИЗАЦИЯ ЗАПУЩЕНА!");
        Debug.LogError("[NetworkInitializer] Эта функция вызывается ДО загрузки сцены");
        Debug.LogError($"[NetworkInitializer] SocketIOManager.Instance до инициализации: {(SocketIOManager.Instance != null ? "EXISTS" : "NULL")}");

        InitializeNetworkManagers();

        Debug.LogError($"[NetworkInitializer] SocketIOManager.Instance после инициализации: {(SocketIOManager.Instance != null ? "EXISTS" : "NULL")}");
    }

    /// <summary>
    /// Создаёт все необходимые сетевые менеджеры
    /// </summary>
    private static void InitializeNetworkManagers()
    {
        Debug.LogError("🔥 [NetworkInitializer] InitializeNetworkManagers() ВЫЗВАН");

        // КРИТИЧНО: Уничтожаем старый SocketIOManager если он есть!
        // Это нужно потому что DontDestroyOnLoad сохраняет объекты между запусками в редакторе
        if (SocketIOManager.Instance != null)
        {
            Debug.LogError("🔥 [NetworkInitializer] ⚠️ НАЙДЕН СТАРЫЙ SocketIOManager! УНИЧТОЖАЮ...");

            // Отключаемся от старого сервера
            if (SocketIOManager.Instance.IsConnected)
            {
                Debug.LogError("🔥 [NetworkInitializer] Старый SocketIOManager был подключён, отключаюсь...");
                SocketIOManager.Instance.Disconnect();
            }

            // Уничтожаем GameObject
            Object.Destroy(SocketIOManager.Instance.gameObject);
            Debug.LogError("🔥 [NetworkInitializer] ✅ Старый SocketIOManager уничтожен");
        }
        else
        {
            Debug.LogError("🔥 [NetworkInitializer] Старого SocketIOManager НЕТ, создаю новый");
        }

        // Создаём НОВЫЙ SocketIOManager с правильным URL
        Debug.LogError("🔥 [NetworkInitializer] Создаю GameObject 'SocketIOManager'...");
        GameObject wsClient = new GameObject("SocketIOManager");

        Debug.LogError("🔥 [NetworkInitializer] Добавляю компонент SocketIOManager...");
        var component = wsClient.AddComponent<SocketIOManager>();

        Debug.LogError($"🔥 [NetworkInitializer] Компонент добавлен: {(component != null ? "SUCCESS" : "FAILED")}");

        DontDestroyOnLoad(wsClient);
        Debug.LogError("🔥 [NetworkInitializer] ✅ Создан НОВЫЙ SocketIOManager (WebSocket)");
        Debug.LogError("🔥 [NetworkInitializer] URL должен быть: https://aetherion-server-gv5u.onrender.com");

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

        Debug.LogError("🔥 [NetworkInitializer] 🚀 Все сетевые менеджеры готовы!");
    }

    // СТАРЫЙ КОД: Оставлен для обратной совместимости (если NetworkInitializer вручную добавлен в сцену)
    [Header("Auto Setup (Legacy - не требуется с RuntimeInitializeOnLoadMethod)")]
    [SerializeField] private bool autoCreateNetworkManagers = true;

    void Awake()
    {
        if (!autoCreateNetworkManagers) return;

        Debug.Log("[NetworkInitializer] Awake вызван (legacy mode)");
        // RuntimeInitializeOnLoadMethod уже создал менеджеры, но вызовем ещё раз на всякий случай
        InitializeNetworkManagers();
    }
}
