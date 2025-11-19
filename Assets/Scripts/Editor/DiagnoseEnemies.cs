using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Инструмент диагностики врагов - проверяет почему не все враги работают
/// </summary>
public class DiagnoseEnemies : MonoBehaviour
{
    [MenuItem("Tools/Enemy Setup/Diagnose All Enemies")]
    public static void DiagnoseAllEnemies()
    {
        Debug.Log("=== 🔍 ДИАГНОСТИКА ВРАГОВ ===\n");

        // Находим все объекты с компонентом Enemy
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();

        if (allEnemies.Length == 0)
        {
            Debug.LogError("[Diagnose] ❌ В сцене нет объектов с компонентом Enemy!");
            return;
        }

        Debug.Log($"[Diagnose] Найдено Enemy компонентов: {allEnemies.Length}\n");

        // Проверяем FogOfWar
        FogOfWar fogOfWar = FindObjectOfType<FogOfWar>();
        if (fogOfWar == null)
        {
            Debug.LogWarning("[Diagnose] ⚠️ FogOfWar не найден в сцене!");
        }
        else
        {
            Debug.Log("[Diagnose] ✅ FogOfWar найден");
        }

        // Проверяем TargetSystem
        TargetSystem targetSystem = FindObjectOfType<TargetSystem>();
        if (targetSystem == null)
        {
            Debug.LogWarning("[Diagnose] ⚠️ TargetSystem не найден в сцене!");
        }
        else
        {
            Debug.Log("[Diagnose] ✅ TargetSystem найден");
        }

        // Проверяем TargetIndicator
        TargetIndicator targetIndicator = FindObjectOfType<TargetIndicator>();
        if (targetIndicator == null)
        {
            Debug.LogWarning("[Diagnose] ⚠️ TargetIndicator не найден!");
        }
        else
        {
            Debug.Log("[Diagnose] ✅ TargetIndicator найден");

            // Проверяем есть ли у него префаб стрелки
            var worldMarkerPrefabField = typeof(TargetIndicator).GetField("worldMarkerPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (worldMarkerPrefabField != null)
            {
                GameObject prefab = worldMarkerPrefabField.GetValue(targetIndicator) as GameObject;
                if (prefab == null)
                {
                    Debug.LogWarning("[Diagnose] ⚠️ У TargetIndicator нет префаба стрелки (worldMarkerPrefab)!");
                }
                else
                {
                    Debug.Log($"[Diagnose] ✅ Префаб стрелки назначен: {prefab.name}");
                }
            }
        }

        Debug.Log("\n--- Проверка каждого врага ---\n");

        // Проверяем каждого врага
        for (int i = 0; i < allEnemies.Length; i++)
        {
            Enemy enemy = allEnemies[i];
            Debug.Log($"\n[Враг #{i + 1}] {enemy.gameObject.name}:");

            // 1. Проверка тега
            if (enemy.gameObject.CompareTag("Enemy"))
            {
                Debug.Log("  ✅ Тег: Enemy");
            }
            else
            {
                Debug.LogError($"  ❌ Тег: {enemy.gameObject.tag} (должен быть 'Enemy')");
            }

            // 2. Проверка коллайдера
            Collider collider = enemy.GetComponent<Collider>();
            if (collider != null)
            {
                Debug.Log($"  ✅ Коллайдер: {collider.GetType().Name} (enabled: {collider.enabled})");
            }
            else
            {
                Debug.LogError("  ❌ Коллайдер: НЕТ (нужен для клика мышью!)");
            }

            // 3. Проверка Renderer
            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                int enabledCount = renderers.Count(r => r.enabled);
                Debug.Log($"  ✅ Renderer'ы: {renderers.Length} (включено: {enabledCount})");
            }
            else
            {
                Debug.LogWarning("  ⚠️ Renderer'ы: НЕТ (враг будет невидим)");
            }

            // 4. Проверка имени и здоровья
            Debug.Log($"  • Имя: {enemy.GetEnemyName()}");
            Debug.Log($"  • Здоровье: {enemy.GetCurrentHealth()}/{enemy.GetMaxHealth()}");
            Debug.Log($"  • Жив: {(enemy.IsAlive() ? "Да" : "Нет")}");

            // 5. Проверка позиции
            Debug.Log($"  • Позиция: {enemy.transform.position}");

            // 6. Проверка родителя
            if (enemy.transform.parent != null)
            {
                Debug.Log($"  • Родитель: {enemy.transform.parent.name}");
            }
            else
            {
                Debug.Log("  • Родитель: нет (root)");
            }

            // 7. Проверка регистрации в FogOfWar
            if (fogOfWar != null)
            {
                var allEnemiesField = typeof(FogOfWar).GetField("allEnemies",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (allEnemiesField != null)
                {
                    var enemyList = allEnemiesField.GetValue(fogOfWar) as System.Collections.Generic.List<Enemy>;
                    if (enemyList != null && enemyList.Contains(enemy))
                    {
                        Debug.Log("  ✅ Зарегистрирован в FogOfWar");
                    }
                    else
                    {
                        Debug.LogError("  ❌ НЕ зарегистрирован в FogOfWar! (Враг добавлен после запуска игры?)");
                    }
                }
            }
        }

        Debug.Log("\n=== ДИАГНОСТИКА ЗАВЕРШЕНА ===");
    }

    [MenuItem("Tools/Enemy Setup/Fix All Enemy Issues")]
    public static void FixAllEnemyIssues()
    {
        Debug.Log("=== 🔧 ИСПРАВЛЕНИЕ ПРОБЛЕМ ВРАГОВ ===\n");

        Enemy[] allEnemies = FindObjectsOfType<Enemy>();

        if (allEnemies.Length == 0)
        {
            Debug.LogError("[Fix] ❌ В сцене нет объектов с компонентом Enemy!");
            return;
        }

        int fixedCount = 0;

        foreach (Enemy enemy in allEnemies)
        {
            bool needsFix = false;

            // 1. Исправляем тег
            if (!enemy.gameObject.CompareTag("Enemy"))
            {
                enemy.gameObject.tag = "Enemy";
                Debug.Log($"[Fix] ✓ {enemy.gameObject.name}: установлен тег 'Enemy'");
                needsFix = true;
            }

            // 2. Добавляем коллайдер если его нет
            Collider collider = enemy.GetComponent<Collider>();
            if (collider == null)
            {
                BoxCollider box = enemy.gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(1f, 2f, 1f);
                box.center = new Vector3(0f, 1f, 0f);
                Debug.Log($"[Fix] ✓ {enemy.gameObject.name}: добавлен BoxCollider");
                needsFix = true;
            }
            else if (!collider.enabled)
            {
                collider.enabled = true;
                Debug.Log($"[Fix] ✓ {enemy.gameObject.name}: включен Collider");
                needsFix = true;
            }

            // 3. Проверяем Renderer
            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                foreach (Renderer rend in renderers)
                {
                    if (!rend.enabled)
                    {
                        rend.enabled = true;
                        Debug.Log($"[Fix] ✓ {enemy.gameObject.name}: включен Renderer");
                        needsFix = true;
                    }
                }
            }

            // 4. Регистрируем в FogOfWar
            FogOfWar fogOfWar = FindObjectOfType<FogOfWar>();
            if (fogOfWar != null)
            {
                var allEnemiesField = typeof(FogOfWar).GetField("allEnemies",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (allEnemiesField != null)
                {
                    var enemyList = allEnemiesField.GetValue(fogOfWar) as System.Collections.Generic.List<Enemy>;
                    if (enemyList != null && !enemyList.Contains(enemy))
                    {
                        enemyList.Add(enemy);
                        Debug.Log($"[Fix] ✓ {enemy.gameObject.name}: зарегистрирован в FogOfWar");
                        needsFix = true;
                    }
                }
            }

            if (needsFix)
            {
                fixedCount++;
            }
        }

        // Обновляем FogOfWar
        FogOfWar fog = FindObjectOfType<FogOfWar>();
        if (fog != null)
        {
            fog.RefreshEnemies();
            Debug.Log("\n[Fix] ✓ FogOfWar обновлен");
        }

        Debug.Log($"\n=== ИСПРАВЛЕНО ВРАГОВ: {fixedCount}/{allEnemies.Length} ===");

        if (fixedCount > 0)
        {
            Debug.Log("⚠️ ВАЖНО: Эти исправления работают только в Play Mode!");
            Debug.Log("После выхода из Play Mode изменения НЕ сохранятся.");
            Debug.Log("Чтобы сохранить - нужно настроить врагов в режиме редактирования.");
        }
    }
}
