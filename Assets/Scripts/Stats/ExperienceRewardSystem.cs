using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Система награды опытом за убийство игроков в PvP
/// Автоматически добавляет опыт когда локальный игрок убивает другого игрока
/// Работает с LevelingSystem и синхронизируется через NetworkLevelingSync
/// </summary>
public class ExperienceRewardSystem : MonoBehaviour
{
    [Header("Награды за убийство")]
    [Tooltip("Базовый опыт за убийство игрока")]
    [SerializeField] private int baseKillExperience = 50;

    [Tooltip("Множитель за убийство игрока выше уровня")]
    [SerializeField] private float higherLevelMultiplier = 1.5f;

    [Tooltip("Опыт за убийство игрока ниже уровня (процент от базы)")]
    [Range(0f, 1f)]
    [SerializeField] private float lowerLevelPenalty = 0.5f;

    [Tooltip("Максимальная разница уровней для получения опыта")]
    [SerializeField] private int maxLevelDifference = 10;

    [Header("Dependencies")]
    private LevelingSystem levelingSystem;
    private CharacterStats characterStats;
    private SocketIOManager socketManager;
    private NetworkSyncManager networkSyncManager;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private string localPlayerSocketId;
    private bool isInitialized = false;

    void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Инициализация системы
    /// </summary>
    private void Initialize()
    {
        // Получаем компоненты
        levelingSystem = GetComponent<LevelingSystem>();
        characterStats = GetComponent<CharacterStats>();
        socketManager = SocketIOManager.Instance;
        networkSyncManager = NetworkSyncManager.Instance;

        if (levelingSystem == null)
        {
            Debug.LogError("[ExperienceReward] LevelingSystem не найден! Добавьте компонент на игрока.");
            return;
        }

        if (characterStats == null)
        {
            Debug.LogError("[ExperienceReward] CharacterStats не найден! Добавьте компонент на игрока.");
            return;
        }

        if (socketManager == null || !socketManager.IsConnected)
        {
            Debug.LogWarning("[ExperienceReward] SocketIOManager не подключен! Награды за убийства отключены.");
            return;
        }

        if (networkSyncManager == null)
        {
            Debug.LogWarning("[ExperienceReward] NetworkSyncManager не найден!");
            return;
        }

        // Получаем socketId локального игрока
        localPlayerSocketId = networkSyncManager.LocalPlayerSocketId;

        if (string.IsNullOrEmpty(localPlayerSocketId))
        {
            Debug.LogError("[ExperienceReward] LocalPlayerSocketId пустой!");
            return;
        }

        // Подписываемся на событие смерти игрока
        socketManager.On("player_died", OnPlayerKilled);

        isInitialized = true;

        if (showDebugLogs)
        {
            Debug.Log($"[ExperienceReward] ✅ Инициализирован! SocketId: {localPlayerSocketId}");
        }
    }

    /// <summary>
    /// Обработчик смерти игрока (от сервера)
    /// </summary>
    private void OnPlayerKilled(string jsonData)
    {
        if (!isInitialized) return;

        try
        {
            // Парсим данные о смерти
            var deathData = JsonConvert.DeserializeObject<PlayerDiedEvent>(jsonData);

            if (showDebugLogs)
            {
                Debug.Log($"[ExperienceReward] 📥 player_died: victim={deathData.socketId}, killer={deathData.killerId}");
            }

            // Проверяем, мы ли убийца
            if (string.IsNullOrEmpty(deathData.killerId))
            {
                if (showDebugLogs)
                {
                    Debug.Log("[ExperienceReward] Нет убийцы (самоубийство или PvE)");
                }
                return;
            }

            if (deathData.killerId != localPlayerSocketId)
            {
                // Убийца не мы
                return;
            }

            // ✅ МЫ УБИЛИ ИГРОКА!
            if (showDebugLogs)
            {
                Debug.Log($"[ExperienceReward] 🎯 Мы убили игрока {deathData.socketId}!");
            }

            // Рассчитываем награду
            int experienceReward = CalculateKillExperience(deathData);

            if (experienceReward > 0)
            {
                // Даём опыт
                levelingSystem.GainExperience(experienceReward);

                Debug.Log($"[ExperienceReward] 💰 +{experienceReward} опыта за убийство! (Всего: {levelingSystem.CurrentExperience})");

                // Показываем уведомление
                ShowKillRewardNotification(experienceReward);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ExperienceReward] ❌ Ошибка обработки player_died: {e.Message}\nJSON: {jsonData}");
        }
    }

    /// <summary>
    /// Рассчитать опыт за убийство
    /// </summary>
    private int CalculateKillExperience(PlayerDiedEvent deathData)
    {
        // Базовый опыт
        float experience = baseKillExperience;

        // Если сервер передал уровень жертвы - учитываем разницу уровней
        if (deathData.victimLevel > 0)
        {
            int myLevel = levelingSystem.CurrentLevel;
            int victimLevel = deathData.victimLevel;
            int levelDifference = victimLevel - myLevel;

            if (showDebugLogs)
            {
                Debug.Log($"[ExperienceReward] Мой уровень: {myLevel}, Уровень жертвы: {victimLevel}, Разница: {levelDifference}");
            }

            // Слишком большая разница - не даём опыт
            if (Mathf.Abs(levelDifference) > maxLevelDifference)
            {
                Debug.LogWarning($"[ExperienceReward] ⚠️ Разница уровней слишком большая ({levelDifference}). Опыт не получен.");
                return 0;
            }

            // Жертва выше уровнем - бонус
            if (levelDifference > 0)
            {
                experience *= (1f + (levelDifference * (higherLevelMultiplier - 1f) / 10f));

                if (showDebugLogs)
                {
                    Debug.Log($"[ExperienceReward] 🔥 Бонус за убийство игрока выше уровня! x{1f + (levelDifference * (higherLevelMultiplier - 1f) / 10f):F2}");
                }
            }
            // Жертва ниже уровнем - штраф
            else if (levelDifference < 0)
            {
                float penalty = 1f - (Mathf.Abs(levelDifference) * (1f - lowerLevelPenalty) / 10f);
                penalty = Mathf.Max(penalty, lowerLevelPenalty); // Минимум 50% от базы
                experience *= penalty;

                if (showDebugLogs)
                {
                    Debug.Log($"[ExperienceReward] ⚠️ Штраф за убийство игрока ниже уровня: x{penalty:F2}");
                }
            }
        }

        return Mathf.RoundToInt(experience);
    }

    /// <summary>
    /// Показать уведомление о награде
    /// TODO: Создать UI для уведомлений
    /// </summary>
    private void ShowKillRewardNotification(int experience)
    {
        // ВРЕМЕННО: Просто логируем
        Debug.Log($"[ExperienceReward] 🎉 УБИЙСТВО! +{experience} ОПЫТА");

        // TODO: Добавить UI уведомление
        // NotificationManager.Instance.ShowKillReward(experience);
    }

    /// <summary>
    /// Очистка при уничтожении
    /// </summary>
    void OnDestroy()
    {
        // Отписываемся от событий
        // ПРИМЕЧАНИЕ: SocketIOManager не имеет метода Off
        // События очистятся автоматически

        if (showDebugLogs)
        {
            Debug.Log("[ExperienceReward] 🔌 Компонент уничтожен");
        }
    }

    // ═══════════════════════════════════════════════════════
    // ТЕСТИРОВАНИЕ В РЕДАКТОРЕ
    // ═══════════════════════════════════════════════════════

    [ContextMenu("Test: Simulate Kill (Same Level)")]
    private void TestKillSameLevel()
    {
        if (levelingSystem == null)
        {
            Debug.LogError("LevelingSystem не найден!");
            return;
        }

        int reward = baseKillExperience;
        levelingSystem.GainExperience(reward);
        Debug.Log($"[TEST] Симуляция убийства игрока того же уровня: +{reward} опыта");
    }

    [ContextMenu("Test: Simulate Kill (Higher Level +3)")]
    private void TestKillHigherLevel()
    {
        if (levelingSystem == null)
        {
            Debug.LogError("LevelingSystem не найден!");
            return;
        }

        // Симуляция убийства игрока на 3 уровня выше
        float multiplier = 1f + (3 * (higherLevelMultiplier - 1f) / 10f);
        int reward = Mathf.RoundToInt(baseKillExperience * multiplier);

        levelingSystem.GainExperience(reward);
        Debug.Log($"[TEST] Симуляция убийства игрока +3 уровня: +{reward} опыта (x{multiplier:F2})");
    }

    [ContextMenu("Test: Simulate Kill (Lower Level -2)")]
    private void TestKillLowerLevel()
    {
        if (levelingSystem == null)
        {
            Debug.LogError("LevelingSystem не найден!");
            return;
        }

        // Симуляция убийства игрока на 2 уровня ниже
        float penalty = 1f - (2 * (1f - lowerLevelPenalty) / 10f);
        penalty = Mathf.Max(penalty, lowerLevelPenalty);
        int reward = Mathf.RoundToInt(baseKillExperience * penalty);

        levelingSystem.GainExperience(reward);
        Debug.Log($"[TEST] Симуляция убийства игрока -2 уровня: +{reward} опыта (x{penalty:F2})");
    }
}

// ПРИМЕЧАНИЕ: Класс PlayerDiedEvent определён в NetworkSyncManager.cs
// Используем существующий класс вместо создания дубликата
