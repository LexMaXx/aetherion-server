using UnityEngine;

/// <summary>
/// Настройки системы логирования
/// Позволяет включить/выключить логи в билде через Inspector
///
/// Создание: Assets → Create → Aetherion → Debug Logger Settings
/// </summary>
[CreateAssetMenu(fileName = "DebugLoggerSettings", menuName = "Aetherion/Debug Logger Settings")]
public class DebugLoggerSettings : ScriptableObject
{
    [Header("Основные настройки")]
    [Tooltip("Показывать логи в билде? (false = скрыть все Debug.Log в релизе)")]
    public bool showLogsInBuild = false;

    [Tooltip("Показывать предупреждения в билде?")]
    public bool showWarningsInBuild = false;

    [Tooltip("Показывать ошибки в билде? (рекомендуется TRUE)")]
    public bool showErrorsInBuild = true;

    [Header("Фильтры по категориям")]
    [Tooltip("Показывать сетевые логи [NetworkSync], [SocketIO]")]
    public bool showNetworkLogs = true;

    [Tooltip("Показывать логи боевой системы [SkillExecutor], [Combat]")]
    public bool showCombatLogs = true;

    [Tooltip("Показывать логи UI [SimpleStatsUI], [CharacterStatsPanel]")]
    public bool showUILogs = true;

    [Tooltip("Показывать логи AI [SkeletonAI], [EnemyAI]")]
    public bool showAILogs = true;

    [Tooltip("Показывать логи систем [HealthSystem], [LevelingSystem]")]
    public bool showSystemLogs = true;

    [Header("Дополнительно")]
    [Tooltip("Включить stack trace для ошибок")]
    public bool enableStackTrace = true;

    /// <summary>
    /// Применить настройки логирования
    /// </summary>
    public void Apply()
    {
        DebugLogger.SetShowLogsInBuild(showLogsInBuild);

        // Настраиваем уровень логирования Unity
        if (enableStackTrace)
        {
            Debug.unityLogger.logEnabled = true;
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
            Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.ScriptOnly);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        }
        else
        {
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        }

        Debug.Log($"[DebugLoggerSettings] Настройки применены: showLogsInBuild={showLogsInBuild}");
    }

    /// <summary>
    /// Проверить должен ли показываться лог определённой категории
    /// </summary>
    public bool ShouldLog(string message)
    {
        if (!showLogsInBuild) return false;

        // Фильтруем по категориям
        if (message.Contains("[NetworkSync]") || message.Contains("[SocketIO]"))
            return showNetworkLogs;

        if (message.Contains("[SkillExecutor]") || message.Contains("[Combat]"))
            return showCombatLogs;

        if (message.Contains("[SimpleStatsUI]") || message.Contains("[CharacterStatsPanel]"))
            return showUILogs;

        if (message.Contains("[SkeletonAI]") || message.Contains("[EnemyAI]"))
            return showAILogs;

        if (message.Contains("[HealthSystem]") || message.Contains("[LevelingSystem]"))
            return showSystemLogs;

        // По умолчанию показываем
        return true;
    }

    void OnEnable()
    {
        Apply();
    }
}
