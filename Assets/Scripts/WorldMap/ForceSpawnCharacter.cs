using UnityEngine;

/// <summary>
/// ТЕСТОВЫЙ СКРИПТ: Принудительный спавн персонажа на карте мира
/// Используйте если персонаж не появляется автоматически
/// </summary>
public class ForceSpawnCharacter : MonoBehaviour
{
    [Header("Test Settings")]
    [Tooltip("Какой класс заспавнить для теста (если не задан - берётся из GameProgressManager)")]
    [SerializeField] private string testClassName = "";

    [Tooltip("Позиция спавна по умолчанию (если нет сохранённой)")]
    [SerializeField] private Vector3 defaultSpawnPosition = new Vector3(250, 1, 250);

    [Tooltip("Спавнить при старте сцены")]
    [SerializeField] private bool spawnOnStart = true;

    void Start()
    {
        if (spawnOnStart)
        {
            ForceSpawn();
        }
    }

    /// <summary>
    /// Принудительный спавн персонажа (для тестирования)
    /// </summary>
    [ContextMenu("Force Spawn Character")]
    public void ForceSpawn()
    {
        Debug.Log("=== НАЧАЛО ДИАГНОСТИКИ СПАВНА ===");

        // Определяем класс персонажа
        string className = testClassName;
        Debug.Log($"[ForceSpawnCharacter] 🔍 testClassName (Inspector): '{testClassName}'");

        // Если не задан - берём из GameProgressManager или PlayerPrefs
        if (string.IsNullOrEmpty(className))
        {
            Debug.Log($"[ForceSpawnCharacter] testClassName пустой, ищу в сохранениях...");

            // Сначала пробуем из GameProgressManager
            if (GameProgressManager.Instance != null)
            {
                GameObject savedPrefab = GameProgressManager.Instance.GetSelectedCharacterPrefab();
                if (savedPrefab != null)
                {
                    // Извлекаем имя класса из имени префаба: "WarriorModel" → "Warrior"
                    className = savedPrefab.name.Replace("Model", "");
                    Debug.Log($"[ForceSpawnCharacter] ✅ Класс из GameProgressManager: '{className}' (префаб: {savedPrefab.name})");
                }
                else
                {
                    Debug.LogWarning($"[ForceSpawnCharacter] ⚠️ GameProgressManager.GetSelectedCharacterPrefab() вернул null");
                }
            }
            else
            {
                Debug.LogWarning($"[ForceSpawnCharacter] ⚠️ GameProgressManager.Instance == null");
            }

            // Если не нашли - пробуем из PlayerPrefs
            if (string.IsNullOrEmpty(className))
            {
                className = PlayerPrefs.GetString("SelectedCharacterClass", "");
                if (!string.IsNullOrEmpty(className))
                {
                    Debug.Log($"[ForceSpawnCharacter] ✅ Класс из PlayerPrefs: '{className}'");
                }
                else
                {
                    Debug.LogWarning($"[ForceSpawnCharacter] ⚠️ PlayerPrefs.SelectedCharacterClass пустой");
                }
            }

            // Если всё равно пусто - используем Warrior
            if (string.IsNullOrEmpty(className))
            {
                className = "Warrior";
                Debug.LogWarning($"[ForceSpawnCharacter] ⚠️ Класс не найден нигде, использую по умолчанию: {className}");
            }
        }
        else
        {
            Debug.Log($"[ForceSpawnCharacter] ✅ Использую testClassName из Inspector: '{className}'");
        }

        Debug.Log($"[ForceSpawnCharacter] 🚀 ПРИНУДИТЕЛЬНЫЙ СПАВН: {className}");

        // 1. Проверка префаба
        string prefabPath = $"Characters/{className}Model";
        GameObject characterPrefab = Resources.Load<GameObject>(prefabPath);

        if (characterPrefab == null)
        {
            Debug.LogError($"[ForceSpawnCharacter] ❌ Префаб не найден: Resources/{prefabPath}");
            Debug.LogError($"[ForceSpawnCharacter] 💡 Проверьте что префаб существует в Assets/Resources/Characters/{className}Model.prefab");
            return;
        }

        Debug.Log($"[ForceSpawnCharacter] ✅ Префаб найден: {characterPrefab.name}");

        // 2. Удаление старого персонажа
        GameObject[] oldPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject oldPlayer in oldPlayers)
        {
            Debug.Log($"[ForceSpawnCharacter] 🗑️ Удаляю старого Player: {oldPlayer.name}");
            Destroy(oldPlayer);
        }

        // 3. Определение позиции спавна
        Vector3 spawnPosition = GetSpawnPosition();
        Debug.Log($"[ForceSpawnCharacter] 📍 Позиция спавна: {spawnPosition}");

        // 4. Спавн нового персонажа
        GameObject player = Instantiate(characterPrefab, spawnPosition, Quaternion.identity);
        player.tag = "Player";
        player.name = $"Player (FORCE SPAWNED) - {className}Model";

        Debug.Log($"[ForceSpawnCharacter] ✅ Персонаж заспавнен: {player.name} at {spawnPosition}");

        // 4. Добавление компонентов
        if (player.GetComponent<WorldMapPlayerController>() == null)
        {
            player.AddComponent<WorldMapPlayerController>();
            Debug.Log($"[ForceSpawnCharacter] ✅ WorldMapPlayerController добавлен");
        }

        if (player.GetComponent<CharacterController>() == null)
        {
            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = new Vector3(0, 1, 0);
            Debug.Log($"[ForceSpawnCharacter] ✅ CharacterController добавлен");
        }

        // 5. Регистрация в GameProgressManager
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.SetSelectedCharacter($"{className}Model");
            Debug.Log($"[ForceSpawnCharacter] ✅ Персонаж зарегистрирован в GameProgressManager");
        }

        // 6. Уведомляем WorldMapManager о новом персонаже
        if (WorldMapManager.Instance != null)
        {
            // Используем рефлексию чтобы установить playerCharacter
            var field = typeof(WorldMapManager).GetField("playerCharacter",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(WorldMapManager.Instance, player);
                Debug.Log($"[ForceSpawnCharacter] ✅ WorldMapManager уведомлён о персонаже");

                // Принудительная настройка камеры
                var setupCameraMethod = typeof(WorldMapManager).GetMethod("SetupCamera",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (setupCameraMethod != null)
                {
                    setupCameraMethod.Invoke(WorldMapManager.Instance, null);
                    Debug.Log($"[ForceSpawnCharacter] ✅ Камера настроена");
                }
            }
        }
        else
        {
            Debug.LogWarning("[ForceSpawnCharacter] ⚠️ WorldMapManager не найден - камера не будет следить за персонажем");
        }

        Debug.Log($"[ForceSpawnCharacter] 🎉 ГОТОВО! Персонаж появился на карте мира!");
    }

    /// <summary>
    /// Получить позицию спавна (сохранённую или дефолтную)
    /// </summary>
    private Vector3 GetSpawnPosition()
    {
        Vector3 position;

        // Проверяем сохранённую позицию в PlayerPrefs
        if (PlayerPrefs.HasKey("WorldMap_PlayerX"))
        {
            float x = PlayerPrefs.GetFloat("WorldMap_PlayerX");
            float z = PlayerPrefs.GetFloat("WorldMap_PlayerZ");

            position = new Vector3(x, 0, z);
            Debug.Log($"[ForceSpawnCharacter] ✅ Загружена сохранённая позиция (X, Z): ({x}, {z})");
        }
        else
        {
            Debug.Log($"[ForceSpawnCharacter] 📍 Сохранённой позиции нет, используем дефолтную: {defaultSpawnPosition}");
            position = defaultSpawnPosition;
        }

        // ВАЖНО: Определяем Y координату по высоте terrain
        float terrainHeight = GetTerrainHeight(position.x, position.z);
        position.y = terrainHeight + 0.5f; // +0.5 чтобы персонаж был чуть выше terrain

        Debug.Log($"[ForceSpawnCharacter] 📍 Финальная позиция спавна: {position} (terrain height: {terrainHeight})");
        return position;
    }

    /// <summary>
    /// Получить высоту terrain в точке (x, z)
    /// </summary>
    private float GetTerrainHeight(float x, float z)
    {
        // Ищем terrain в сцене
        Terrain terrain = Terrain.activeTerrain;

        if (terrain != null)
        {
            float height = terrain.SampleHeight(new Vector3(x, 0, z));
            Debug.Log($"[ForceSpawnCharacter] 🏔️ Высота terrain в ({x}, {z}): {height}");
            return height;
        }
        else
        {
            Debug.LogWarning($"[ForceSpawnCharacter] ⚠️ Terrain не найден! Используем Y = 1");
            return 1f; // Дефолтная высота если нет terrain
        }
    }

    /// <summary>
    /// Тест всех классов по очереди
    /// </summary>
    [ContextMenu("Test All Classes")]
    public void TestAllClasses()
    {
        string[] classes = { "Warrior", "Mage", "Archer", "Rogue", "Paladin" };

        Debug.Log("[ForceSpawnCharacter] 🔍 Проверка всех классов...");

        foreach (string className in classes)
        {
            string prefabPath = $"Characters/{className}Model";
            GameObject prefab = Resources.Load<GameObject>(prefabPath);

            if (prefab != null)
            {
                Debug.Log($"[ForceSpawnCharacter]   ✅ {className} - OK");
            }
            else
            {
                Debug.LogError($"[ForceSpawnCharacter]   ❌ {className} - НЕ НАЙДЕН!");
            }
        }

        Debug.Log("[ForceSpawnCharacter] ✅ Проверка завершена");
    }
}
