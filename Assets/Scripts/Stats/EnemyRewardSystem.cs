using UnityEngine;

/// <summary>
/// Выдаёт опыт И золото за убийство врагов (Enemy компонент)
/// Работает с Enemy.cs (орки, боссы и т.д.)
/// ПОДДЕРЖКА: Подписывается на Enemy.OnDeath И HealthSystem.OnDeath
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyRewardSystem : MonoBehaviour
{
    [Header("Experience Reward")]
    [Tooltip("Базовое количество опыта за убийство этого врага")]
    [SerializeField] private int baseExperience = 50;

    [Tooltip("Уровень врага для расчёта бонусов/штрафов")]
    [SerializeField] private int enemyLevel = 1;

    [Tooltip("Бонус к опыту за каждый уровень врага выше игрока")]
    [SerializeField] private float higherLevelBonusPerLevel = 0.2f;

    [Tooltip("Минимальная доля опыта при убийстве врага значительно ниже уровнем")]
    [Range(0.1f, 1f)]
    [SerializeField] private float lowerLevelPenalty = 0.4f;

    [Header("Gold Reward")]
    [Tooltip("Базовое количество золота за убийство этого врага")]
    [SerializeField] private int baseGold = 25;

    [Tooltip("Минимальное золото (всегда минимум это значение)")]
    [SerializeField] private int minGold = 20;

    [Tooltip("Максимальное золото (случайный диапазон от minGold до maxGold)")]
    [SerializeField] private int maxGold = 40;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private Enemy enemy;
    private HealthSystem healthSystem;
    private bool rewardsGiven = false; // Флаг чтобы награды выдавались только один раз

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        if (enemy == null)
        {
            Debug.LogError("[EnemyReward] Enemy не найден! Компонент отключён.");
            enabled = false;
            return;
        }

        // Проверяем наличие HealthSystem (скелеты и другие враги могут его иметь)
        healthSystem = GetComponent<HealthSystem>();

        // ДИАГНОСТИКА: Логируем информацию о враге
        Debug.Log($"[EnemyReward] 🔍 Awake для {gameObject.name}, Enemy ID: {enemy.GetEnemyId()}");
        if (healthSystem != null)
        {
            Debug.Log($"[EnemyReward] 🔍 HealthSystem найден - будет подписка на оба события смерти");
        }
    }

    private void OnEnable()
    {
        rewardsGiven = false;

        if (enemy != null)
        {
            enemy.OnDeath += OnEnemyDeath;
            Log($"✅ Подписан на Enemy.OnDeath врага {enemy.GetEnemyName()}");
        }

        // КРИТИЧЕСКИ ВАЖНО: Подписываемся ТАКЖЕ на HealthSystem.OnDeath
        // Скелеты и другие враги используют HealthSystem для отслеживания HP
        if (healthSystem != null)
        {
            healthSystem.OnDeath += OnHealthSystemDeath;
            Log($"✅ Подписан на HealthSystem.OnDeath врага {enemy.GetEnemyName()}");
        }
    }

    private void OnDisable()
    {
        if (enemy != null)
        {
            enemy.OnDeath -= OnEnemyDeath;
        }

        if (healthSystem != null)
        {
            healthSystem.OnDeath -= OnHealthSystemDeath;
        }
    }

    /// <summary>
    /// Обработчик смерти врага через Enemy.OnDeath
    /// </summary>
    private void OnEnemyDeath(Enemy deadEnemy)
    {
        if (deadEnemy == null || rewardsGiven)
            return;

        Log($"💀 Враг {deadEnemy.GetEnemyName()} погиб (Enemy.OnDeath)! Выдаём награды...");
        GiveRewards();
    }

    /// <summary>
    /// Обработчик смерти врага через HealthSystem.OnDeath
    /// </summary>
    private void OnHealthSystemDeath()
    {
        if (rewardsGiven)
            return;

        Log($"💀 Враг {enemy.GetEnemyName()} погиб (HealthSystem.OnDeath)! Выдаём награды...");
        GiveRewards();
    }

    /// <summary>
    /// Выдать награды игроку (опыт + золото)
    /// </summary>
    private void GiveRewards()
    {
        if (rewardsGiven)
        {
            Log("⚠️ Награды уже выданы, пропускаем повторную выдачу");
            return;
        }

        rewardsGiven = true;

        // Получаем локального игрока (того кто убил врага)
        GameObject player = FindLocalPlayer();
        if (player == null)
        {
            Log("❌ Локальный игрок не найден - награды не выданы");
            return;
        }

        // Выдаём опыт
        GiveExperienceReward(player);

        // Выдаём золото
        GiveGoldReward(player);
    }

    /// <summary>
    /// Найти локального игрока
    /// </summary>
    private GameObject FindLocalPlayer()
    {
        Log("🔍 Поиск локального игрока...");

        // Пробуем найти через тег
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Log($"✅ Игрок найден через тег 'Player': {player.name}");
            return player;
        }
        Log("⚠️ Игрок не найден через тег 'Player'");

        // Пробуем найти по имени (WarriorPlayer, MagePlayer и т.д.)
        player = GameObject.Find("WarriorPlayer");
        if (player != null)
        {
            Log($"✅ Игрок найден по имени 'WarriorPlayer'");
            return player;
        }

        player = GameObject.Find("MagePlayer");
        if (player != null)
        {
            Log($"✅ Игрок найден по имени 'MagePlayer'");
            return player;
        }

        player = GameObject.Find("ArcherPlayer");
        if (player != null)
        {
            Log($"✅ Игрок найден по имени 'ArcherPlayer'");
            return player;
        }

        // Пробуем найти через LocalPlayerEntity
        var localPlayerEntity = FindFirstObjectByType<LocalPlayerEntity>();
        if (localPlayerEntity != null)
        {
            Log($"✅ Игрок найден через LocalPlayerEntity: {localPlayerEntity.gameObject.name}");
            return localPlayerEntity.gameObject;
        }

        Log("❌ Игрок не найден ни одним способом!");
        return null;
    }

    /// <summary>
    /// Выдать опыт игроку
    /// </summary>
    private void GiveExperienceReward(GameObject player)
    {
        Log($"🎯 Попытка выдать опыт игроку {player.name}...");

        // Ищем LevelingSystem на всех уровнях
        LevelingSystem leveling = player.GetComponent<LevelingSystem>();
        if (leveling != null)
        {
            Log($"✅ LevelingSystem найден через GetComponent");
        }
        else
        {
            leveling = player.GetComponentInChildren<LevelingSystem>();
            if (leveling != null)
                Log($"✅ LevelingSystem найден через GetComponentInChildren");
        }

        if (leveling == null)
        {
            leveling = player.GetComponentInParent<LevelingSystem>();
            if (leveling != null)
                Log($"✅ LevelingSystem найден через GetComponentInParent");
        }

        if (leveling == null)
        {
            leveling = player.transform.root.GetComponentInChildren<LevelingSystem>();
            if (leveling != null)
                Log($"✅ LevelingSystem найден через root.GetComponentInChildren");
        }

        if (leveling == null)
        {
            Log($"❌ LevelingSystem не найден на игроке {player.name} - опыт не выдан");
            return;
        }

        int currentLevel = leveling.CurrentLevel;
        int currentExp = leveling.CurrentExperience;
        int experienceReward = CalculateExperience(currentLevel);

        Log($"📊 Текущий уровень игрока: {currentLevel}, текущий опыт: {currentExp}");
        Log($"💎 Рассчитанная награда: {experienceReward} XP");

        if (experienceReward <= 0)
        {
            Log($"⚠️ Награда опыта <= 0, пропускаем");
            return;
        }

        leveling.GainExperience(experienceReward);
        Log($"✅ ОПЫТ ВЫДАН! +{experienceReward} XP игроку {player.name} за {enemy.GetEnemyName()} (lvl {enemyLevel})");
        Log($"📈 Новый опыт игрока: {leveling.CurrentExperience} (было: {currentExp})");
    }

    /// <summary>
    /// Выдать золото игроку
    /// </summary>
    private void GiveGoldReward(GameObject player)
    {
        Log($"💰 Попытка выдать золото игроку {player.name}...");

        // Ищем MongoInventoryManager через Singleton
        var inventoryManager = AetherionMMO.Inventory.MongoInventoryManager.Instance;

        if (inventoryManager != null)
        {
            Log($"✅ MongoInventoryManager найден через Singleton");
        }
        else
        {
            Log($"⚠️ MongoInventoryManager.Instance == null, ищем на игроке...");

            // Пробуем найти на игроке
            inventoryManager = player.GetComponent<AetherionMMO.Inventory.MongoInventoryManager>();
            if (inventoryManager != null)
                Log($"✅ MongoInventoryManager найден через GetComponent");

            if (inventoryManager == null)
            {
                inventoryManager = player.GetComponentInChildren<AetherionMMO.Inventory.MongoInventoryManager>();
                if (inventoryManager != null)
                    Log($"✅ MongoInventoryManager найден через GetComponentInChildren");
            }

            if (inventoryManager == null)
            {
                inventoryManager = player.GetComponentInParent<AetherionMMO.Inventory.MongoInventoryManager>();
                if (inventoryManager != null)
                    Log($"✅ MongoInventoryManager найден через GetComponentInParent");
            }

            if (inventoryManager == null)
            {
                inventoryManager = player.transform.root.GetComponentInChildren<AetherionMMO.Inventory.MongoInventoryManager>();
                if (inventoryManager != null)
                    Log($"✅ MongoInventoryManager найден через root.GetComponentInChildren");
            }
        }

        if (inventoryManager == null)
        {
            Log($"❌ MongoInventoryManager не найден - золото не выдано");
            return;
        }

        int currentGold = inventoryManager.GetGold();
        Log($"📊 Текущее золото игрока: {currentGold}");

        // Рассчитываем случайное количество золота
        int goldReward = Random.Range(minGold, maxGold + 1);
        goldReward = Mathf.Max(goldReward, minGold); // Минимум minGold

        Log($"💎 Рассчитанная награда: {goldReward} GOLD (диапазон: {minGold}-{maxGold})");

        // Даём золото
        inventoryManager.AddGold(goldReward);
        Log($"✅ ЗОЛОТО ВЫДАНО! +{goldReward} GOLD игроку {player.name} за {enemy.GetEnemyName()}");
        Log($"💰 Новое золото игрока: {inventoryManager.GetGold()} (было: {currentGold})");
    }

    /// <summary>
    /// Рассчитать опыт с учётом уровня
    /// </summary>
    private int CalculateExperience(int killerLevel)
    {
        float reward = Mathf.Max(1, baseExperience);
        int levelDifference = enemyLevel - Mathf.Max(1, killerLevel);

        if (levelDifference > 0)
        {
            // Враг выше уровнем - бонус
            reward *= 1f + levelDifference * Mathf.Max(0f, higherLevelBonusPerLevel);
            Log($"🔥 Бонус за убийство врага выше уровня: x{1f + levelDifference * higherLevelBonusPerLevel:F2}");
        }
        else if (levelDifference < 0)
        {
            // Враг ниже уровнем - штраф
            float penalty = 1f + levelDifference * 0.1f; // levelDifference отрицательный
            reward *= Mathf.Max(lowerLevelPenalty, penalty);
            Log($"⚠️ Штраф за убийство врага ниже уровня: x{Mathf.Max(lowerLevelPenalty, penalty):F2}");
        }

        return Mathf.Max(1, Mathf.RoundToInt(reward));
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[EnemyReward] {message}");
        }
    }
}
