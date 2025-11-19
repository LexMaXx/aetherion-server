using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// Автоматическая настройка врагов в сцене
/// </summary>
public class SetupEnemies : Editor
{
    [MenuItem("Tools/Enemy Setup/Setup All Enemies in Scene")]
    public static void SetupAllEnemiesInScene()
    {
        Debug.Log("[SetupEnemies] ========== НАЧАЛО ==========");

        // Находим все объекты с компонентом Enemy в текущей сцене
        Enemy[] enemies = FindObjectsOfType<Enemy>();

        if (enemies.Length == 0)
        {
            Debug.LogWarning("[SetupEnemies] В сцене не найдено объектов с компонентом Enemy!");
            Debug.Log("[SetupEnemies] 💡 Добавьте компонент Enemy на кубы-враги вручную:");
            Debug.Log("[SetupEnemies]    1. Выберите куб в Hierarchy");
            Debug.Log("[SetupEnemies]    2. Add Component → Enemy");
            return;
        }

        int setupCount = 0;
        int alreadySetupCount = 0;

        foreach (Enemy enemy in enemies)
        {
            GameObject enemyObj = enemy.gameObject;

            Debug.Log($"\n[SetupEnemies] Обработка: {enemyObj.name}");

            // 1. Проверяем/добавляем тег Enemy
            if (!enemyObj.CompareTag("Enemy"))
            {
                try
                {
                    enemyObj.tag = "Enemy";
                    Debug.Log($"  ✅ Установлен тег: Enemy");
                }
                catch
                {
                    Debug.LogError($"  ❌ Не удалось установить тег Enemy! Создайте тег в Project Settings → Tags and Layers");
                    continue;
                }
            }
            else
            {
                Debug.Log($"  ℹ️ Тег Enemy уже установлен");
            }

            // 2. Проверяем Layer
            if (enemyObj.layer == LayerMask.NameToLayer("Default"))
            {
                // Проверяем существует ли слой Enemy
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer != -1)
                {
                    enemyObj.layer = enemyLayer;
                    Debug.Log($"  ✅ Установлен Layer: Enemy");
                }
                else
                {
                    Debug.LogWarning($"  ⚠️ Layer 'Enemy' не найден. Создайте его в Project Settings → Tags and Layers");
                }
            }

            // 3. Проверяем Collider (нужен для клика)
            Collider collider = enemyObj.GetComponent<Collider>();
            if (collider == null)
            {
                // Добавляем BoxCollider
                BoxCollider boxCollider = enemyObj.AddComponent<BoxCollider>();
                Debug.Log($"  ✅ Добавлен BoxCollider");
            }
            else
            {
                Debug.Log($"  ℹ️ Collider уже есть: {collider.GetType().Name}");
            }

            // 4. Проверяем настройки Enemy компонента
            SerializedObject so = new SerializedObject(enemy);

            SerializedProperty enemyNameProp = so.FindProperty("enemyName");
            if (string.IsNullOrEmpty(enemyNameProp.stringValue) || enemyNameProp.stringValue == "Enemy")
            {
                enemyNameProp.stringValue = enemyObj.name;
                Debug.Log($"  ✅ Имя врага установлено: {enemyObj.name}");
            }

            SerializedProperty maxHealthProp = so.FindProperty("maxHealth");
            if (maxHealthProp.floatValue <= 0)
            {
                maxHealthProp.floatValue = 100f;
                Debug.Log($"  ✅ Здоровье установлено: 100");
            }

            so.ApplyModifiedProperties();

            setupCount++;
        }

        Debug.Log($"\n[SetupEnemies] ========== ЗАВЕРШЕНО ==========");
        Debug.Log($"[SetupEnemies] Обработано врагов: {setupCount}");
        Debug.Log($"[SetupEnemies] ✅ Все враги настроены!");
        Debug.Log($"[SetupEnemies] 💡 Теперь вы можете кликать по врагам и таргетить их!");

        // Помечаем сцену как изменённую
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Enemy Setup/Add Enemy Component to Selected")]
    public static void AddEnemyToSelected()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("[SetupEnemies] Не выбран ни один объект!");
            return;
        }

        int addedCount = 0;

        foreach (GameObject obj in Selection.gameObjects)
        {
            // Проверяем есть ли уже компонент
            if (obj.GetComponent<Enemy>() != null)
            {
                Debug.Log($"[SetupEnemies] {obj.name} уже имеет Enemy компонент");
                continue;
            }

            // Добавляем Enemy
            Enemy enemy = obj.AddComponent<Enemy>();

            // Устанавливаем тег
            try
            {
                obj.tag = "Enemy";
            }
            catch
            {
                Debug.LogError($"[SetupEnemies] Не удалось установить тег Enemy для {obj.name}");
            }

            // Добавляем Collider если нет
            if (obj.GetComponent<Collider>() == null)
            {
                obj.AddComponent<BoxCollider>();
            }

            // Настраиваем Enemy
            SerializedObject so = new SerializedObject(enemy);
            so.FindProperty("enemyName").stringValue = obj.name;
            so.FindProperty("maxHealth").floatValue = 100f;
            so.ApplyModifiedProperties();

            Debug.Log($"[SetupEnemies] ✅ Добавлен Enemy компонент на {obj.name}");
            addedCount++;
        }

        Debug.Log($"[SetupEnemies] Добавлено Enemy компонентов: {addedCount}");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}
