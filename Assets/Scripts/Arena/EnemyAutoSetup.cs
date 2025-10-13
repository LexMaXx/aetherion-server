using UnityEngine;
using System.Collections;

/// <summary>
/// Автоматическая настройка всех врагов в сцене при запуске
/// Гарантирует что все враги имеют правильный тег и коллайдеры
/// </summary>
public class EnemyAutoSetup : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool setupOnStart = true;
    [SerializeField] private float delayBeforeSetup = 0.5f; // Задержка перед настройкой (чтобы все объекты успели загрузиться)

    void Start()
    {
        if (setupOnStart)
        {
            StartCoroutine(SetupEnemiesDelayed());
        }
    }

    /// <summary>
    /// Настроить всех врагов с задержкой
    /// </summary>
    private IEnumerator SetupEnemiesDelayed()
    {
        yield return new WaitForSeconds(delayBeforeSetup);

        Debug.Log("[EnemyAutoSetup] === Начало автоматической настройки врагов ===");

        SetupAllEnemies();

        Debug.Log("[EnemyAutoSetup] === Автоматическая настройка врагов завершена ===");
    }

    /// <summary>
    /// Настроить всех врагов в сцене
    /// </summary>
    public void SetupAllEnemies()
    {
        // Находим всех врагов с компонентом Enemy
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        if (allEnemies.Length == 0)
        {
            Debug.LogWarning("[EnemyAutoSetup] В сцене нет объектов с компонентом Enemy!");
            return;
        }

        Debug.Log($"[EnemyAutoSetup] Найдено врагов: {allEnemies.Length}");

        int fixedCount = 0;

        foreach (Enemy enemy in allEnemies)
        {
            if (enemy == null) continue;

            bool needsFix = false;

            // 1. Проверяем и устанавливаем тег "Enemy"
            if (!enemy.gameObject.CompareTag("Enemy"))
            {
                try
                {
                    enemy.gameObject.tag = "Enemy";
                    Debug.Log($"[EnemyAutoSetup] ✓ {enemy.gameObject.name}: установлен тег 'Enemy'");
                    needsFix = true;
                }
                catch (UnityException e)
                {
                    Debug.LogError($"[EnemyAutoSetup] ❌ Не удалось установить тег 'Enemy': {e.Message}");
                    Debug.LogError("[EnemyAutoSetup] Тег 'Enemy' не создан в проекте! Создайте его: Edit → Project Settings → Tags and Layers");
                }
            }

            // 2. Проверяем наличие Collider
            Collider collider = enemy.GetComponent<Collider>();
            if (collider == null)
            {
                // Добавляем BoxCollider
                BoxCollider box = enemy.gameObject.AddComponent<BoxCollider>();

                // Пробуем определить размер через Renderer
                Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    // Вычисляем bounds всех renderer'ов
                    Bounds combinedBounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        combinedBounds.Encapsulate(renderers[i].bounds);
                    }

                    // Устанавливаем размер коллайдера
                    box.center = enemy.transform.InverseTransformPoint(combinedBounds.center);
                    box.size = combinedBounds.size;
                }
                else
                {
                    // Дефолтный размер
                    box.size = new Vector3(1f, 2f, 1f);
                    box.center = new Vector3(0f, 1f, 0f);
                }

                Debug.Log($"[EnemyAutoSetup] ✓ {enemy.gameObject.name}: добавлен BoxCollider (size: {box.size})");
                needsFix = true;
            }
            else if (!collider.enabled)
            {
                // Включаем коллайдер если он отключен
                collider.enabled = true;
                Debug.Log($"[EnemyAutoSetup] ✓ {enemy.gameObject.name}: включен Collider");
                needsFix = true;
            }

            // 3. Проверяем что Renderer'ы включены
            Renderer[] enemyRenderers = enemy.GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in enemyRenderers)
            {
                if (!rend.enabled)
                {
                    rend.enabled = true;
                    Debug.Log($"[EnemyAutoSetup] ✓ {enemy.gameObject.name}: включен Renderer");
                    needsFix = true;
                }
            }

            if (needsFix)
            {
                fixedCount++;
            }
        }

        // Регистрируем всех врагов в FogOfWar
        FogOfWar fogOfWar = FindFirstObjectByType<FogOfWar>();
        if (fogOfWar != null)
        {
            fogOfWar.RefreshEnemies();
            Debug.Log("[EnemyAutoSetup] ✓ FogOfWar обновлен (все враги зарегистрированы)");
        }

        Debug.Log($"[EnemyAutoSetup] Исправлено врагов: {fixedCount}/{allEnemies.Length}");
    }
}
