using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor скрипт для проверки Character префабов на наличие Enemy компонента
/// КРИТИЧЕСКОЕ: Enemy компонент НЕ должен быть на локальных игроках!
/// Enemy имеет собственную HP систему (maxHealth, currentHealth) которая конфликтует с HealthSystem!
/// </summary>
public class CheckEnemyComponentOnCharacters : EditorWindow
{
    [MenuItem("Tools/Check Enemy Component on Characters")]
    public static void CheckCharacters()
    {
        Debug.Log("[CheckEnemy] ========== ПРОВЕРКА CHARACTER ПРЕФАБОВ ==========");

        // Загружаем все Character префабы из Resources/Characters
        GameObject[] characterPrefabs = Resources.LoadAll<GameObject>("Characters");

        if (characterPrefabs.Length == 0)
        {
            Debug.LogError("[CheckEnemy] ❌ Не найдено Character префабов в Resources/Characters!");
            return;
        }

        Debug.Log($"[CheckEnemy] Найдено {characterPrefabs.Length} Character префабов");

        bool foundEnemyComponents = false;

        foreach (GameObject prefab in characterPrefabs)
        {
            Debug.Log($"\n[CheckEnemy] === Проверка {prefab.name} ===");

            // Проверяем Enemy компонент на Root
            Enemy enemyRoot = prefab.GetComponent<Enemy>();
            if (enemyRoot != null)
            {
                Debug.LogError($"[CheckEnemy] ❌❌❌ КРИТИЧЕСКАЯ ОШИБКА: {prefab.name} имеет Enemy компонент на ROOT!", prefab);
                Debug.LogError($"[CheckEnemy] ❌ Enemy.maxHealth = {enemyRoot.GetMaxHealth()} конфликтует с HealthSystem!");
                Debug.LogError($"[CheckEnemy] ❌ РЕШЕНИЕ: Удалите Enemy компонент из {prefab.name}!");
                foundEnemyComponents = true;
            }
            else
            {
                Debug.Log($"[CheckEnemy] ✅ {prefab.name} Root: OK (нет Enemy)");
            }

            // Проверяем Model (дочерний объект)
            if (prefab.transform.childCount > 0)
            {
                Transform modelTransform = prefab.transform.GetChild(0);
                Enemy enemyModel = modelTransform.GetComponent<Enemy>();

                if (enemyModel != null)
                {
                    Debug.LogError($"[CheckEnemy] ❌❌❌ КРИТИЧЕСКАЯ ОШИБКА: {prefab.name}/Model имеет Enemy компонент!", modelTransform.gameObject);
                    Debug.LogError($"[CheckEnemy] ❌ Enemy.maxHealth = {enemyModel.GetMaxHealth()} конфликтует с HealthSystem!");
                    Debug.LogError($"[CheckEnemy] ❌ РЕШЕНИЕ: Удалите Enemy компонент из {prefab.name}/Model!");
                    foundEnemyComponents = true;
                }
                else
                {
                    Debug.Log($"[CheckEnemy] ✅ {prefab.name}/Model: OK (нет Enemy)");
                }
            }

            // Проверяем HealthSystem
            HealthSystem healthSystem = prefab.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                Debug.Log($"[CheckEnemy] ✅ {prefab.name} имеет HealthSystem (MaxHP: {healthSystem.MaxHealth})");
            }
            else
            {
                Debug.LogWarning($"[CheckEnemy] ⚠️ {prefab.name} НЕ имеет HealthSystem!");
            }

            // Проверяем CharacterStats
            CharacterStats characterStats = prefab.GetComponent<CharacterStats>();
            if (characterStats != null)
            {
                Debug.Log($"[CheckEnemy] ✅ {prefab.name} имеет CharacterStats (Endurance: {characterStats.endurance})");
            }
            else
            {
                Debug.LogWarning($"[CheckEnemy] ⚠️ {prefab.name} НЕ имеет CharacterStats!");
            }
        }

        Debug.Log("\n[CheckEnemy] ========== ПРОВЕРКА ЗАВЕРШЕНА ==========");

        if (foundEnemyComponents)
        {
            Debug.LogError("[CheckEnemy] ❌ НАЙДЕНЫ КОНФЛИКТЫ! Enemy компонент должен быть ТОЛЬКО на врагах, НЕ на игроках!");
            EditorUtility.DisplayDialog(
                "Enemy Component Found!",
                "КРИТИЧЕСКАЯ ОШИБКА: Найдены Enemy компоненты на Character префабах!\n\n" +
                "Enemy имеет собственную HP систему которая конфликтует с HealthSystem!\n\n" +
                "Проверьте Console для деталей.",
                "OK"
            );
        }
        else
        {
            Debug.Log("[CheckEnemy] ✅✅✅ ВСЕ ПРЕФАБЫ В ПОРЯДКЕ! Enemy компоненты не найдены.");
            EditorUtility.DisplayDialog(
                "Check Complete",
                "✅ Все Character префабы в порядке!\n\nEnemy компоненты не найдены.",
                "OK"
            );
        }
    }
}
