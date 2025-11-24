using UnityEngine;

/// <summary>
/// Диагностический скрипт для проверки компонентов игрока
/// Добавьте на любой GameObject в сцене и нажмите клавишу D для диагностики
/// </summary>
public class DiagnosePlayerComponents : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            DiagnosePlayer();
        }
    }

    [ContextMenu("Diagnose Player Components")]
    public void DiagnosePlayer()
    {
        Debug.Log("========================================");
        Debug.Log("🔍 ДИАГНОСТИКА КОМПОНЕНТОВ ИГРОКА");
        Debug.Log("========================================");

        // Поиск игрока через тег
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("❌ Игрок с тегом 'Player' не найден!");
            return;
        }

        Debug.Log($"✅ Игрок найден: {player.name}");
        Debug.Log($"   Тег: {player.tag}");
        Debug.Log($"   Позиция: {player.transform.position}");
        Debug.Log("");

        // Проверка CharacterStats
        Debug.Log("--- CharacterStats ---");
        CharacterStats stats = player.GetComponent<CharacterStats>();
        if (stats != null)
        {
            Debug.Log($"✅ CharacterStats на родительском объекте ({player.name})");
            Debug.Log($"   STR:{stats.strength} PER:{stats.perception} END:{stats.endurance} WIS:{stats.wisdom}");
            Debug.Log($"   INT:{stats.intelligence} AGI:{stats.agility} LCK:{stats.luck}");
        }
        else
        {
            Debug.LogWarning($"⚠️ CharacterStats НЕ найден на {player.name}");
            stats = player.GetComponentInChildren<CharacterStats>();
            if (stats != null)
            {
                Debug.Log($"✅ CharacterStats найден в детях ({stats.gameObject.name})");
                Debug.Log($"   STR:{stats.strength} PER:{stats.perception} END:{stats.endurance} WIS:{stats.wisdom}");
                Debug.Log($"   INT:{stats.intelligence} AGI:{stats.agility} LCK:{stats.luck}");
            }
            else
            {
                Debug.LogError("❌ CharacterStats НЕ НАЙДЕН НИГДЕ!");
            }
        }
        Debug.Log("");

        // Проверка LevelingSystem
        Debug.Log("--- LevelingSystem ---");
        LevelingSystem leveling = player.GetComponent<LevelingSystem>();
        if (leveling != null)
        {
            Debug.Log($"✅ LevelingSystem на родительском объекте ({player.name})");
            Debug.Log($"   Level: {leveling.CurrentLevel}");
            Debug.Log($"   Experience: {leveling.CurrentExperience}");
            Debug.Log($"   Available Points: {leveling.AvailableStatPoints}");
        }
        else
        {
            Debug.LogWarning($"⚠️ LevelingSystem НЕ найден на {player.name}");
            leveling = player.GetComponentInChildren<LevelingSystem>();
            if (leveling != null)
            {
                Debug.Log($"✅ LevelingSystem найден в детях ({leveling.gameObject.name})");
                Debug.Log($"   Level: {leveling.CurrentLevel}");
                Debug.Log($"   Experience: {leveling.CurrentExperience}");
                Debug.Log($"   Available Points: {leveling.AvailableStatPoints}");
            }
            else
            {
                Debug.LogError("❌ LevelingSystem НЕ НАЙДЕН НИГДЕ!");
                Debug.LogError("   БЕЗ LevelingSystem ИГРОК НЕ МОЖЕТ ПОЛУЧАТЬ ОПЫТ!");
            }
        }
        Debug.Log("");

        // Проверка MongoInventoryManager
        Debug.Log("--- MongoInventoryManager ---");
        if (AetherionMMO.Inventory.MongoInventoryManager.Instance != null)
        {
            Debug.Log("✅ MongoInventoryManager.Instance существует");
            Debug.Log($"   Gold: {AetherionMMO.Inventory.MongoInventoryManager.Instance.CurrentGold}");
        }
        else
        {
            Debug.LogError("❌ MongoInventoryManager.Instance НЕ НАЙДЕН!");
            Debug.LogError("   БЕЗ MongoInventoryManager ИГРОК НЕ МОЖЕТ ПОЛУЧАТЬ ЗОЛОТО!");
        }
        Debug.Log("");

        // Проверка NetworkLevelingSync
        Debug.Log("--- NetworkLevelingSync ---");
        NetworkLevelingSync networkSync = player.GetComponentInChildren<NetworkLevelingSync>();
        if (networkSync != null)
        {
            Debug.Log($"✅ NetworkLevelingSync найден на {networkSync.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ NetworkLevelingSync не найден (нормально для singleplayer)");
        }
        Debug.Log("");

        // Проверка врагов
        Debug.Log("--- Враги в сцене ---");
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Debug.Log($"Всего врагов с компонентом Enemy: {enemies.Length}");

        int withRewardSystem = 0;
        int withoutRewardSystem = 0;

        foreach (Enemy enemy in enemies)
        {
            EnemyRewardSystem reward = enemy.GetComponent<EnemyRewardSystem>();
            if (reward != null)
                withRewardSystem++;
            else
                withoutRewardSystem++;
        }

        Debug.Log($"✅ С EnemyRewardSystem: {withRewardSystem}");
        if (withoutRewardSystem > 0)
        {
            Debug.LogWarning($"⚠️ БЕЗ EnemyRewardSystem: {withoutRewardSystem}");
            Debug.LogWarning($"   Эти враги НЕ БУДУТ давать награды!");
        }

        Debug.Log("========================================");
        Debug.Log("🔍 ДИАГНОСТИКА ЗАВЕРШЕНА");
        Debug.Log("========================================");
    }
}
