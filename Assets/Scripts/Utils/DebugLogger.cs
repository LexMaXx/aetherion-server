using UnityEngine;

/// <summary>
/// Централизованная система логирования с автоматическим отключением в релизе
/// Использовать вместо Debug.Log() для автоматического скрытия логов в билде
///
/// Использование:
/// DebugLogger.Log("Обычное сообщение");
/// DebugLogger.LogWarning("Предупреждение");
/// DebugLogger.LogError("Ошибка - всегда показывается!");
/// </summary>
public static class DebugLogger
{
    // Показывать логи в билде? (можно изменить в Inspector через DebugLoggerSettings)
    private static bool showLogsInBuild = false;

    /// <summary>
    /// Обычный лог (скрывается в релизе)
    /// </summary>
    public static void Log(string message)
    {
        #if UNITY_EDITOR
        Debug.Log(message);
        #else
        if (showLogsInBuild)
        {
            Debug.Log(message);
        }
        #endif
    }

    /// <summary>
    /// Лог с контекстом (скрывается в релизе)
    /// </summary>
    public static void Log(string message, Object context)
    {
        #if UNITY_EDITOR
        Debug.Log(message, context);
        #else
        if (showLogsInBuild)
        {
            Debug.Log(message, context);
        }
        #endif
    }

    /// <summary>
    /// Предупреждение (скрывается в релизе)
    /// </summary>
    public static void LogWarning(string message)
    {
        #if UNITY_EDITOR
        Debug.LogWarning(message);
        #else
        if (showLogsInBuild)
        {
            Debug.LogWarning(message);
        }
        #endif
    }

    /// <summary>
    /// Предупреждение с контекстом (скрывается в релизе)
    /// </summary>
    public static void LogWarning(string message, Object context)
    {
        #if UNITY_EDITOR
        Debug.LogWarning(message, context);
        #else
        if (showLogsInBuild)
        {
            Debug.LogWarning(message, context);
        }
        #endif
    }

    /// <summary>
    /// ОШИБКА - ВСЕГДА ПОКАЗЫВАЕТСЯ (даже в релизе!)
    /// </summary>
    public static void LogError(string message)
    {
        Debug.LogError(message);
    }

    /// <summary>
    /// ОШИБКА с контекстом - ВСЕГДА ПОКАЗЫВАЕТСЯ (даже в релизе!)
    /// </summary>
    public static void LogError(string message, Object context)
    {
        Debug.LogError(message, context);
    }

    /// <summary>
    /// Установить показ логов в билде (может быть изменено через настройки)
    /// </summary>
    public static void SetShowLogsInBuild(bool show)
    {
        showLogsInBuild = show;
    }

    /// <summary>
    /// Получить текущий режим логирования
    /// </summary>
    public static bool IsLoggingEnabled()
    {
        #if UNITY_EDITOR
        return true;
        #else
        return showLogsInBuild;
        #endif
    }
}
