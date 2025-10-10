using UnityEngine;
using UnityEditor;

/// <summary>
/// Простой инструмент для исправления тегов врагов
/// </summary>
public class FixEnemyTag : MonoBehaviour
{
    [MenuItem("Tools/Enemy Setup/Fix Enemy Tags (Simple)")]
    public static void FixEnemyTags()
    {
        Debug.Log("=== 🔧 ИСПРАВЛЕНИЕ ТЕГОВ ВРАГОВ ===\n");

        // Находим всех врагов
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();

        if (allEnemies.Length == 0)
        {
            Debug.LogWarning("[FixEnemyTag] В сцене нет объектов с компонентом Enemy!");
            return;
        }

        Debug.Log($"[FixEnemyTag] Найдено врагов: {allEnemies.Length}\n");

        int fixedCount = 0;

        foreach (Enemy enemy in allEnemies)
        {
            // Проверяем и исправляем тег
            if (!enemy.gameObject.CompareTag("Enemy"))
            {
                try
                {
                    enemy.gameObject.tag = "Enemy";
                    Debug.Log($"[FixEnemyTag] ✓ {enemy.gameObject.name}: установлен тег 'Enemy'");
                    fixedCount++;
                }
                catch (UnityException e)
                {
                    Debug.LogError($"[FixEnemyTag] ❌ Не удалось установить тег 'Enemy': {e.Message}");
                    Debug.LogError("[FixEnemyTag] Возможно тег 'Enemy' не создан в проекте!");
                    Debug.LogError("[FixEnemyTag] Создайте тег вручную: Edit → Project Settings → Tags and Layers");
                    break;
                }
            }
            else
            {
                Debug.Log($"[FixEnemyTag] ✓ {enemy.gameObject.name}: тег уже установлен");
            }

            // Проверяем и добавляем коллайдер
            Collider collider = enemy.GetComponent<Collider>();
            if (collider == null)
            {
                BoxCollider box = enemy.gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(1f, 2f, 1f);
                box.center = new Vector3(0f, 1f, 0f);
                Debug.Log($"[FixEnemyTag] ✓ {enemy.gameObject.name}: добавлен BoxCollider");
                fixedCount++;
            }
            else if (!collider.enabled)
            {
                collider.enabled = true;
                Debug.Log($"[FixEnemyTag] ✓ {enemy.gameObject.name}: включен Collider");
                fixedCount++;
            }
        }

        Debug.Log($"\n=== ИСПРАВЛЕНО: {fixedCount} ===");
    }

    [MenuItem("Tools/Enemy Setup/Create Enemy Tag Manually")]
    public static void ShowTagInstructions()
    {
        string message = @"
=== КАК СОЗДАТЬ ТЕГ 'Enemy' ВРУЧНУЮ ===

1. В Unity Editor откройте: Edit → Project Settings
2. Слева выберите: Tags and Layers
3. Раскройте секцию: Tags
4. Найдите пустой слот (например: Tag 1)
5. Введите: Enemy
6. Закройте окно Project Settings

После этого запустите: Tools → Enemy Setup → Fix Enemy Tags (Simple)
        ";

        Debug.Log(message);
        EditorUtility.DisplayDialog("Создание тега Enemy", message, "OK");
    }
}
