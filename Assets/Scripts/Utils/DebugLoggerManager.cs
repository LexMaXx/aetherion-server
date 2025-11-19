using UnityEngine;

/// <summary>
/// Менеджер логирования - автоматически отключает логи в релизе
/// Добавьте на любой GameObject в первой загружаемой сцене
///
/// ВАЖНО: Автоматически запускается при старте игры!
/// </summary>
public class DebugLoggerManager : MonoBehaviour
{
    [Header("Настройки логирования")]
    [Tooltip("Ссылка на ScriptableObject с настройками (опционально)")]
    [SerializeField] private DebugLoggerSettings settings;

    [Header("Быстрые настройки (если settings == null)")]
    [Tooltip("Показывать Debug.Log в билде?")]
    [SerializeField] private bool showLogsInBuild = false;

    [Tooltip("Показывать предупреждения в билде?")]
    [SerializeField] private bool showWarningsInBuild = false;

    [Tooltip("Полностью отключить Debug.unityLogger?")]
    [SerializeField] private bool disableUnityLogger = true;

    void Awake()
    {
        // Применяем настройки при старте
        ApplySettings();
    }

    /// <summary>
    /// Применить настройки логирования
    /// </summary>
    private void ApplySettings()
    {
        #if UNITY_EDITOR
        // В редакторе всегда показываем все логи
        Debug.unityLogger.logEnabled = true;
        Debug.Log("[DebugLoggerManager] 🔧 EDITOR MODE - Все логи включены");
        return;
        #endif

        // В билде применяем настройки
        if (settings != null)
        {
            // Используем ScriptableObject настройки
            settings.Apply();
            Debug.Log($"[DebugLoggerManager] ⚙️ Применены настройки из ScriptableObject: showLogsInBuild={settings.showLogsInBuild}");
        }
        else
        {
            // Используем локальные настройки
            DebugLogger.SetShowLogsInBuild(showLogsInBuild);

            if (disableUnityLogger)
            {
                // ПОЛНОЕ отключение Unity Logger (самый агрессивный вариант)
                Debug.unityLogger.logEnabled = false;
                Debug.Log("[DebugLoggerManager] 🚫 Unity Logger ПОЛНОСТЬЮ ОТКЛЮЧЕН в билде");
            }
            else
            {
                // Отключаем только обычные логи
                Debug.unityLogger.filterLogType = showWarningsInBuild ? LogType.Warning : LogType.Error;
                Debug.Log($"[DebugLoggerManager] ⚙️ Логи в билде: logs={showLogsInBuild}, warnings={showWarningsInBuild}");
            }

            // Отключаем Stack Trace для производительности
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
        }
    }

    /// <summary>
    /// Переключить логи в рантайме (для отладки)
    /// </summary>
    [ContextMenu("Toggle Logs")]
    public void ToggleLogs()
    {
        Debug.unityLogger.logEnabled = !Debug.unityLogger.logEnabled;
        Debug.Log($"[DebugLoggerManager] 🔄 Логи: {(Debug.unityLogger.logEnabled ? "ВКЛЮЧЕНЫ" : "ВЫКЛЮЧЕНЫ")}");
    }

    /// <summary>
    /// Показать информацию о текущих настройках
    /// </summary>
    [ContextMenu("Show Info")]
    public void ShowInfo()
    {
        Debug.Log("=== DEBUG LOGGER INFO ===");
        Debug.Log($"Unity Logger Enabled: {Debug.unityLogger.logEnabled}");
        Debug.Log($"Filter Log Type: {Debug.unityLogger.filterLogType}");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Is Editor: {Application.isEditor}");
        Debug.Log($"Is Development Build: {Debug.isDebugBuild}");
        Debug.Log($"DebugLogger Enabled: {DebugLogger.IsLoggingEnabled()}");
        Debug.Log("========================");
    }
}
