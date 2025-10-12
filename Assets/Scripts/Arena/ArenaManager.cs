using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Менеджер арены - управляет спавном персонажа и игровой логикой
/// ОБНОВЛЕНО: Использует PlayerController с агилити-бонусом к скорости
/// </summary>
public class ArenaManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Vector3 defaultSpawnPosition = new Vector3(0, 0, 0);

    [Header("Character Prefabs")]
    [SerializeField] private GameObject warriorPrefab;
    [SerializeField] private GameObject magePrefab;
    [SerializeField] private GameObject archerPrefab;
    [SerializeField] private GameObject roguePrefab;
    [SerializeField] private GameObject paladinPrefab;

    [Header("Camera")]
    [SerializeField] private Camera arenaCamera;

    [Header("Fog of War")]
    [Tooltip("Глобальные настройки Fog of War для всех персонажей")]
    [SerializeField] private FogOfWarSettings fogOfWarSettings;

    [Header("Multiplayer")]
    [SerializeField] private GameObject networkSyncManagerPrefab;
    [SerializeField] private Transform[] multiplayerSpawnPoints; // Спаун-поинты для разных игроков

    private GameObject spawnedCharacter;
    private bool isMultiplayer = false;

    void Start()
    {
        // Проверяем мультиплеер режим
        string roomId = PlayerPrefs.GetString("CurrentRoomId", "");
        isMultiplayer = !string.IsNullOrEmpty(roomId);

        if (isMultiplayer)
        {
            Debug.Log("[ArenaManager] 🌐 MULTIPLAYER MODE");
            SetupMultiplayer();
        }
        else
        {
            Debug.Log("[ArenaManager] 🎮 SINGLEPLAYER MODE");
            // Очищаем мультиплеер данные при запуске одиночной игры
            PlayerPrefs.DeleteKey("CurrentRoomId");
            PlayerPrefs.Save();
        }

        // Создаем UI для Action Points
        SetupActionPointsUI();

        SpawnSelectedCharacter();

        // Создаём UI для характеристик (нажми C во время игры)
        SetupCharacterStatsUI();

        // Создаём постоянный HUD с характеристиками
        SetupStatsHUD();

        // Создаём HP/MP бары с никнеймом
        SetupStatusBars();

        // Добавляем debug скрипт для отладки (нажми F9 во время игры)
        if (GetComponent<DebugPlayerStructure>() == null)
        {
            gameObject.AddComponent<DebugPlayerStructure>();
        }

        // Добавляем автоматическую настройку врагов
        if (GetComponent<EnemyAutoSetup>() == null)
        {
            EnemyAutoSetup enemyAutoSetup = gameObject.AddComponent<EnemyAutoSetup>();
            Debug.Log("✓ Добавлен EnemyAutoSetup (автоматическая настройка всех врагов)");
        }
    }

    /// <summary>
    /// Настроить мультиплеер
    /// </summary>
    private void SetupMultiplayer()
    {
        // Create NetworkSyncManager if not exists
        if (NetworkSyncManager.Instance == null)
        {
            if (networkSyncManagerPrefab != null)
            {
                Instantiate(networkSyncManagerPrefab);
                Debug.Log("[ArenaManager] ✅ NetworkSyncManager создан");
            }
            else
            {
                GameObject networkManager = new GameObject("NetworkSyncManager");
                networkManager.AddComponent<NetworkSyncManager>();
                Debug.Log("[ArenaManager] ✅ NetworkSyncManager создан динамически");
            }
        }

        // Verify WebSocket connection
        if (SocketIOManager.Instance == null)
        {
            Debug.LogError("[ArenaManager] ❌ SocketIOManager не найден! Multiplayer не будет работать");
        }
        else if (!SocketIOManager.Instance.IsConnected)
        {
            Debug.LogWarning("[ArenaManager] ⚠️ WebSocket не подключен. Connecting...");
            string token = PlayerPrefs.GetString("UserToken", "");
            SocketIOManager.Instance.Connect(token, (success) =>
            {
                if (success)
                {
                    Debug.Log("[ArenaManager] ✅ WebSocket (SocketIOManager) подключен");
                }
                else
                {
                    Debug.LogError("[ArenaManager] ❌ Не удалось подключиться к WebSocket");
                }
            });
        }
        else
        {
            Debug.Log("[ArenaManager] ✅ WebSocket (SocketIOManager) подключен");
        }
    }

    /// <summary>
    /// Спавн выбранного персонажа
    /// </summary>
    private void SpawnSelectedCharacter()
    {
        // Получаем выбранный класс из PlayerPrefs
        string selectedClass = PlayerPrefs.GetString("SelectedCharacterClass", "");

        if (string.IsNullOrEmpty(selectedClass))
        {
            Debug.LogError("Не выбран персонаж! Возврат к CharacterSelectionScene");
            SceneManager.LoadScene("CharacterSelectionScene");
            return;
        }

        // Получаем префаб персонажа
        GameObject characterPrefab = GetCharacterPrefab(selectedClass);

        if (characterPrefab == null)
        {
            Debug.LogError($"Префаб для класса {selectedClass} не найден!");
            return;
        }

        // Определяем точку спавна
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : defaultSpawnPosition;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        // Создаем контейнер для персонажа (родительский пустой объект)
        spawnedCharacter = new GameObject($"{selectedClass}Player");
        spawnedCharacter.transform.position = spawnPosition;
        spawnedCharacter.transform.rotation = spawnRotation;

        // Создаем модель персонажа как дочерний объект
        GameObject characterModel = Instantiate(characterPrefab, spawnedCharacter.transform);
        characterModel.name = $"{selectedClass}Model"; // Включаем имя класса для распознавания

        // ВАЖНО: Устанавливаем правильный Layer для персонажа и всех его детей
        int characterLayer = LayerMask.NameToLayer("Character");
        if (characterLayer == -1)
        {
            Debug.LogWarning("Layer 'Character' не найден! Используем Default");
            characterLayer = 0;
        }
        SetLayerRecursively(characterModel, characterLayer);
        Debug.Log($"✓ Layer установлен: {LayerMask.LayerToName(characterLayer)}");

        // ВАЖНО: Модель должна быть на земле (Y = 0)
        // Теперь Root Motion Y отключен в анимациях, поэтому смещение НЕ нужно
        characterModel.transform.localPosition = Vector3.zero;
        characterModel.transform.localRotation = Quaternion.identity;

        Debug.Log($"✓ Создан персонаж: {selectedClass}");
        Debug.Log($"  Родитель позиция: {spawnedCharacter.transform.position}");
        Debug.Log($"  Модель локальная позиция: {characterModel.transform.localPosition}");
        Debug.Log($"  Модель мировая позиция: {characterModel.transform.position}");

        // ВАЖНО: Animator остается на Model с нашими Mixamo анимациями
        Animator modelAnimator = characterModel.GetComponent<Animator>();
        if (modelAnimator != null)
        {
            modelAnimator.applyRootMotion = false; // Отключаем Root Motion
            Debug.Log($"✓ Animator настроен на Model (Root Motion: {modelAnimator.applyRootMotion})");
        }

        // Настраиваем компоненты персонажа
        SetupCharacterComponents();

        // Настраиваем камеру
        SetupCamera();
    }

    /// <summary>
    /// Настроить компоненты персонажа после спавна
    /// </summary>
    private void SetupCharacterComponents()
    {
        if (spawnedCharacter == null)
            return;

        // ВАЖНО: CharacterController должен быть на Model (дочернем объекте)
        // Удаляем CharacterController с родителя если есть
        CharacterController parentCC = spawnedCharacter.GetComponent<CharacterController>();
        if (parentCC != null)
        {
            DestroyImmediate(parentCC);
            Debug.Log("✓ Удален CharacterController с родителя");
        }

        // Находим Model (дочерний объект)
        Transform modelTransform = spawnedCharacter.transform.GetChild(0);
        if (modelTransform == null)
        {
            Debug.LogError("❌ Model не найден!");
            return;
        }
        Debug.Log($"✓ Найден Model: {modelTransform.name}");

        // Проверяем/настраиваем CharacterController на Model
        CharacterController charController = modelTransform.GetComponent<CharacterController>();
        if (charController == null)
        {
            charController = modelTransform.gameObject.AddComponent<CharacterController>();
            Debug.Log("✓ Добавлен CharacterController на Model");
        }

        // Правильные настройки для CharacterController (best practices для MMO/RPG)
        charController.height = 2.16f;
        charController.center = new Vector3(0, 0.05f, 0);
        charController.radius = 0.3f;

        // ВАЖНО: Best practices от Unity для CharacterController
        charController.skinWidth = 0.03f;         // ~10% от radius (0.3 * 0.1 = 0.03)
        charController.minMoveDistance = 0f;      // Unity рекомендует 0
        charController.slopeLimit = 45f;
        charController.stepOffset = 0.3f;

        Debug.Log($"✓ CharacterController настроен на Model: Center={charController.center}, Height={charController.height}");

        // Проверяем Animator на Model
        Animator animator = modelTransform.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            Debug.Log($"✓ Animator на Model: {animator.runtimeAnimatorController.name}");

            // Устанавливаем боевую стойку
            animator.SetBool("InBattle", true);
        }
        else
        {
            Debug.LogWarning("⚠ Animator не настроен на Model!");
        }

        // ВАЖНО: Сначала добавляем SPECIAL характеристики (другие системы зависят от них!)
        SetupStatsAndSystems(modelTransform);

        // Добавляем PlayerController с системой агилити-бонуса к скорости (ОБНОВЛЕНО)
        // ВАЖНО: Добавляем ПОСЛЕ CharacterStats чтобы PlayerController мог найти его в Start()
        // Ловкость персонажа будет влиять на скорость бега
        PlayerController playerController = modelTransform.GetComponent<PlayerController>();
        if (playerController == null)
        {
            playerController = modelTransform.gameObject.AddComponent<PlayerController>();
            Debug.Log("✓ Добавлен PlayerController (с агилити-бонусом к скорости)");
        }

        // Добавляем систему оружия
        SetupWeapons(modelTransform);

        // Добавляем систему атаки
        PlayerAttack playerAttack = modelTransform.GetComponent<PlayerAttack>();
        if (playerAttack == null)
        {
            playerAttack = modelTransform.gameObject.AddComponent<PlayerAttack>();
            Debug.Log("✓ Добавлен PlayerAttack");
        }

        // MULTIPLAYER: Добавляем синхронизацию боя
        if (isMultiplayer)
        {
            NetworkCombatSync combatSync = modelTransform.GetComponent<NetworkCombatSync>();
            if (combatSync == null)
            {
                combatSync = modelTransform.gameObject.AddComponent<NetworkCombatSync>();
                Debug.Log("✓ Добавлен NetworkCombatSync (мультиплеер)");
            }
        }

        // Добавляем систему таргетирования
        TargetSystem targetSystem = modelTransform.GetComponent<TargetSystem>();
        if (targetSystem == null)
        {
            targetSystem = modelTransform.gameObject.AddComponent<TargetSystem>();
            Debug.Log("✓ Добавлен TargetSystem");
        }

        // Добавляем индикатор цели (стрелка над врагом)
        TargetIndicator targetIndicator = modelTransform.GetComponent<TargetIndicator>();
        if (targetIndicator == null)
        {
            targetIndicator = modelTransform.gameObject.AddComponent<TargetIndicator>();
            Debug.Log("✓ Добавлен TargetIndicator");

            // Настраиваем индикатор
            SetupTargetIndicator(targetIndicator, targetSystem, modelTransform);
        }

        // Добавляем систему очков действия
        ActionPointsSystem actionPointsSystem = modelTransform.GetComponent<ActionPointsSystem>();
        if (actionPointsSystem == null)
        {
            actionPointsSystem = modelTransform.gameObject.AddComponent<ActionPointsSystem>();
            Debug.Log("✓ Добавлен ActionPointsSystem");
        }

        // Добавляем туман войны (Fog of War)
        FogOfWar fogOfWar = modelTransform.GetComponent<FogOfWar>();
        if (fogOfWar == null)
        {
            fogOfWar = modelTransform.gameObject.AddComponent<FogOfWar>();
            Debug.Log("✓ Добавлен FogOfWar");
        }

        // Применяем глобальные настройки FogOfWar если они заданы
        SetupFogOfWar(fogOfWar);

        // MULTIPLAYER: Регистрируем локального игрока в NetworkSyncManager
        if (isMultiplayer && NetworkSyncManager.Instance != null)
        {
            string selectedClass = PlayerPrefs.GetString("SelectedCharacterClass", "");
            NetworkSyncManager.Instance.SetLocalPlayer(modelTransform.gameObject, selectedClass);
            Debug.Log("[ArenaManager] ✅ Локальный игрок зарегистрирован в NetworkSyncManager");
        }
    }

    /// <summary>
    /// Настроить SPECIAL характеристики и зависимые системы
    /// </summary>
    private void SetupStatsAndSystems(Transform modelTransform)
    {
        // Получаем выбранный класс
        string selectedClass = PlayerPrefs.GetString("SelectedCharacterClass", "Warrior");

        // 1. CharacterStats (SPECIAL система)
        CharacterStats characterStats = modelTransform.GetComponent<CharacterStats>();
        if (characterStats == null)
        {
            characterStats = modelTransform.gameObject.AddComponent<CharacterStats>();
            Debug.Log("✓ Добавлен CharacterStats");
        }

        // Загружаем пресет класса
        ClassStatsPreset classPreset = Resources.Load<ClassStatsPreset>($"ClassStats/{selectedClass}Stats");
        if (classPreset != null)
        {
            // Используем рефлексию для установки пресета
            var presetField = typeof(CharacterStats).GetField("classPreset",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (presetField != null)
            {
                presetField.SetValue(characterStats, classPreset);
                Debug.Log($"✓ Применен пресет характеристик: {selectedClass}");
            }
        }
        else
        {
            Debug.LogWarning($"[ArenaManager] Пресет {selectedClass}Stats не найден в Resources/ClassStats/");
        }

        // Загружаем формулы расчета
        StatsFormulas formulas = Resources.Load<StatsFormulas>("StatsFormulas");
        if (formulas != null)
        {
            var formulasField = typeof(CharacterStats).GetField("formulas",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (formulasField != null)
            {
                formulasField.SetValue(characterStats, formulas);
                Debug.Log("✓ Применены формулы расчета характеристик");
            }
        }

        // 2. LevelingSystem (прокачка)
        LevelingSystem levelingSystem = modelTransform.GetComponent<LevelingSystem>();
        if (levelingSystem == null)
        {
            levelingSystem = modelTransform.gameObject.AddComponent<LevelingSystem>();
            Debug.Log("✓ Добавлен LevelingSystem");
        }

        // 3. HealthSystem (HP)
        HealthSystem healthSystem = modelTransform.GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            healthSystem = modelTransform.gameObject.AddComponent<HealthSystem>();
            Debug.Log("✓ Добавлен HealthSystem");
        }

        // 4. ManaSystem (MP)
        ManaSystem manaSystem = modelTransform.GetComponent<ManaSystem>();
        if (manaSystem == null)
        {
            manaSystem = modelTransform.gameObject.AddComponent<ManaSystem>();
            Debug.Log("✓ Добавлен ManaSystem");
        }
    }

    /// <summary>
    /// Настроить оружие для персонажа
    /// </summary>
    private void SetupWeapons(Transform modelTransform)
    {
        Debug.Log($"\n=== SetupWeapons для {modelTransform.name} ===");

        ClassWeaponManager weaponManager = modelTransform.GetComponent<ClassWeaponManager>();
        if (weaponManager == null)
        {
            Debug.Log("Добавляем ClassWeaponManager...");
            weaponManager = modelTransform.gameObject.AddComponent<ClassWeaponManager>();

            // Проверяем WeaponDatabase
            WeaponDatabase db = WeaponDatabase.Instance;
            if (db == null)
            {
                Debug.LogError("❌ WeaponDatabase не найдена! Создайте через Tools → Create Weapon Database");
            }
            else
            {
                Debug.Log("✓ WeaponDatabase найдена");
            }

            // Прикрепляем оружие
            weaponManager.AttachWeaponForClass();
            Debug.Log($"✓ Оружие добавлено для {modelTransform.name}");
        }
        else
        {
            Debug.Log("✓ ClassWeaponManager уже существует");
            // Переприкрепляем оружие на всякий случай
            weaponManager.AttachWeaponForClass();
        }
    }

    /// <summary>
    /// Получить префаб персонажа по классу
    /// </summary>
    private GameObject GetCharacterPrefab(string characterClass)
    {
        switch (characterClass)
        {
            case "Warrior":
                return warriorPrefab;
            case "Mage":
                return magePrefab;
            case "Archer":
                return archerPrefab;
            case "Rogue":
                return roguePrefab;
            case "Paladin":
                return paladinPrefab;
            default:
                Debug.LogWarning($"Неизвестный класс: {characterClass}");
                return null;
        }
    }

    /// <summary>
    /// Настроить камеру для следования за персонажем
    /// </summary>
    private void SetupCamera()
    {
        if (spawnedCharacter == null || arenaCamera == null)
        {
            Debug.LogError("⚠ SetupCamera: spawnedCharacter или arenaCamera = null!");
            return;
        }

        // Находим Model (дочерний объект)
        Transform modelTransform = spawnedCharacter.transform.GetChild(0);
        if (modelTransform == null)
        {
            Debug.LogError("❌ Model не найден для камеры!");
            return;
        }
        Debug.Log($"✓ Камера нацелена на: {modelTransform.name}");

        // ВАЖНО: Удаляем ВСЕ старые компоненты камеры
        CameraFollow[] oldFollows = arenaCamera.GetComponents<CameraFollow>();
        foreach (CameraFollow cf in oldFollows)
        {
            DestroyImmediate(cf);
            Debug.Log("✓ Удален старый CameraFollow");
        }

        // Удаляем все старые TPSCameraController
        TPSCameraController[] oldTPS = arenaCamera.GetComponents<TPSCameraController>();
        foreach (TPSCameraController tps in oldTPS)
        {
            DestroyImmediate(tps);
            Debug.Log("✓ Удален старый TPSCameraController");
        }

        // Добавляем новый TPS Camera Controller
        TPSCameraController newTpsCamera = arenaCamera.gameObject.AddComponent<TPSCameraController>();

        // ВАЖНО: Устанавливаем target на Model (а не на родителя!)
        newTpsCamera.SetTarget(modelTransform);

        Debug.Log($"✓ Настроена TPS камера, target = {modelTransform.name}");
    }

    /// <summary>
    /// Получить заспавненного персонажа
    /// </summary>
    public GameObject GetSpawnedCharacter()
    {
        return spawnedCharacter;
    }

    /// <summary>
    /// Получить вертикальное смещение модели для компенсации разных pivot точек Mixamo
    /// </summary>
    private float GetModelOffsetY(string characterClass)
    {
        // Смещения основаны на Bounds center Y из диагностики:
        // Warrior: 0.87, Mage: 0.87, Archer: 0.83, Rogue: 0.92, Paladin: 0.80
        // Используем среднее значение 0.86 как базовое, корректируем для каждого класса
        switch (characterClass)
        {
            case "Warrior":
                return 1.01f; // 0.87 центр → 1.01 компенсация
            case "Mage":
                return 1.01f; // 0.87 центр → 1.01 компенсация
            case "Archer":
                return 1.05f; // 0.83 центр → 1.05 компенсация (модель ниже, поднимаем больше)
            case "Rogue":
                return 0.96f; // 0.92 центр → 0.96 компенсация (модель выше, поднимаем меньше)
            case "Paladin":
                return 1.08f; // 0.80 центр → 1.08 компенсация (самый низкий, поднимаем больше всех)
            default:
                return 1.0f; // Дефолтное значение
        }
    }

    /// <summary>
    /// Установить Layer рекурсивно для всех детей
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    /// <summary>
    /// Создать UI для системы очков действия
    /// </summary>
    private void SetupActionPointsUI()
    {
        // Проверяем, уже создан ли UI
        ActionPointsUI existingUI = FindObjectOfType<ActionPointsUI>();
        if (existingUI != null)
        {
            Debug.Log("✓ ActionPointsUI уже существует");
            return;
        }

        // Находим или создаем Canvas
        UnityEngine.Canvas canvas = FindObjectOfType<UnityEngine.Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log("✓ Canvas создан");
        }

        // Создаем панель для Action Points внизу экрана
        GameObject apPanel = new GameObject("ActionPointsPanel");
        apPanel.transform.SetParent(canvas.transform, false);

        RectTransform apRect = apPanel.AddComponent<RectTransform>();
        apRect.anchorMin = new Vector2(0.5f, 0f); // Центр низа
        apRect.anchorMax = new Vector2(0.5f, 0f);
        apRect.pivot = new Vector2(0.5f, 0f);
        apRect.anchoredPosition = new Vector2(0, 50); // 50px от низа
        apRect.sizeDelta = new Vector2(500, 50);

        // Добавляем фон (опционально)
        UnityEngine.UI.Image bgImage = apPanel.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0, 0, 0, 0.3f); // Полупрозрачный черный

        // Создаем контейнер для шариков
        GameObject container = new GameObject("PointsContainer");
        container.transform.SetParent(apPanel.transform, false);

        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = new Vector2(450, 40);

        // Добавляем Horizontal Layout Group для автоматического расположения
        UnityEngine.UI.HorizontalLayoutGroup layout = container.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // Добавляем ActionPointsUI компонент
        ActionPointsUI apUI = apPanel.AddComponent<ActionPointsUI>();

        // Используем рефлексию чтобы установить приватные поля
        var pointsContainerField = typeof(ActionPointsUI).GetField("pointsContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (pointsContainerField != null)
        {
            pointsContainerField.SetValue(apUI, container.transform);
        }

        Debug.Log("✓ Action Points UI создан автоматически");
    }

    /// <summary>
    /// Вернуться в главное меню
    /// </summary>
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("GameScene");
    }

    /// <summary>
    /// Создать UI для отображения характеристик (нажми C)
    /// </summary>
    private void SetupCharacterStatsUI()
    {
        // Проверяем, уже создан ли UI
        CharacterStatsUI existingUI = FindObjectOfType<CharacterStatsUI>();
        if (existingUI != null)
        {
            Debug.Log("✓ CharacterStatsUI уже существует");
            return;
        }

        // Создаём новый GameObject для UI
        GameObject uiObj = new GameObject("CharacterStatsUI");
        CharacterStatsUI statsUI = uiObj.AddComponent<CharacterStatsUI>();

        Debug.Log("✓ CharacterStatsUI создан (Нажмите C для открытия)");
    }

    /// <summary>
    /// Создать постоянный HUD с характеристиками
    /// </summary>
    private void SetupStatsHUD()
    {
        // Проверяем, уже создан ли HUD
        SimpleStatsHUD existingHUD = FindObjectOfType<SimpleStatsHUD>();
        if (existingHUD != null)
        {
            Debug.Log("✓ SimpleStatsHUD уже существует");
            return;
        }

        // Создаём новый GameObject для HUD
        GameObject hudObj = new GameObject("SimpleStatsHUD");
        SimpleStatsHUD statsHUD = hudObj.AddComponent<SimpleStatsHUD>();

        Debug.Log("✓ SimpleStatsHUD создан (Нажмите H для переключения)");
    }

    /// <summary>
    /// Настроить индикатор цели (стрелка над врагом)
    /// </summary>
    private void SetupTargetIndicator(TargetIndicator indicator, TargetSystem targetSystem, Transform playerTransform)
    {
        if (indicator == null)
        {
            Debug.LogWarning("[ArenaManager] TargetIndicator не найден!");
            return;
        }

        // Загружаем префаб стрелки из Resources
        GameObject arrowPrefab = Resources.Load<GameObject>("Prefabs/UI/TargetArrow");

        if (arrowPrefab == null)
        {
            Debug.LogWarning("[ArenaManager] Префаб TargetArrow не найден! Убедитесь что он находится в Resources/Prefabs/UI/TargetArrow");
            return;
        }

        // Используем рефлексию для установки приватных полей
        var targetSystemField = typeof(TargetIndicator).GetField("targetSystem",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var playerTransformField = typeof(TargetIndicator).GetField("playerTransform",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var worldMarkerPrefabField = typeof(TargetIndicator).GetField("worldMarkerPrefab",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (targetSystemField != null)
            targetSystemField.SetValue(indicator, targetSystem);

        if (playerTransformField != null)
            playerTransformField.SetValue(indicator, playerTransform);

        if (worldMarkerPrefabField != null)
            worldMarkerPrefabField.SetValue(indicator, arrowPrefab);

        Debug.Log("✓ TargetIndicator настроен с префабом стрелки");
    }

    /// <summary>
    /// Настроить Fog of War с глобальными настройками
    /// </summary>
    private void SetupFogOfWar(FogOfWar fogOfWar)
    {
        if (fogOfWar == null)
        {
            Debug.LogWarning("[ArenaManager] FogOfWar компонент не найден!");
            return;
        }

        // Если есть глобальные настройки - применяем их
        if (fogOfWarSettings != null)
        {
            // Используем рефлексию для установки приватного поля globalSettings
            var globalSettingsField = typeof(FogOfWar).GetField("globalSettings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (globalSettingsField != null)
            {
                globalSettingsField.SetValue(fogOfWar, fogOfWarSettings);
                Debug.Log($"✓ FogOfWar настроен с глобальными настройками: {fogOfWarSettings.name}");
            }
            else
            {
                Debug.LogWarning("[ArenaManager] Не удалось применить глобальные настройки FogOfWar через рефлексию");
            }
        }
        else
        {
            Debug.LogWarning("[ArenaManager] FogOfWarSettings не установлен в ArenaManager. Используются локальные настройки персонажа.");
        }

        // ВАЖНО: Принудительно включаем ignoreHeight для поддержки высоких врагов
        var ignoreHeightField = typeof(FogOfWar).GetField("ignoreHeight",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (ignoreHeightField != null)
        {
            ignoreHeightField.SetValue(fogOfWar, true);
            Debug.Log("✓ FogOfWar: ignoreHeight = TRUE (враги видны на любой высоте)");
        }

        // Также устанавливаем большое значение maxHeightDifference на всякий случай
        var maxHeightDifferenceField = typeof(FogOfWar).GetField("maxHeightDifference",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (maxHeightDifferenceField != null)
        {
            maxHeightDifferenceField.SetValue(fogOfWar, 1000f);
            Debug.Log("✓ FogOfWar: maxHeightDifference = 1000м");
        }
    }

    /// <summary>
    /// Настройка HP/MP баров с никнеймом
    /// </summary>
    private void SetupStatusBars()
    {
        if (GetComponent<PlayerStatusBars>() == null)
        {
            gameObject.AddComponent<PlayerStatusBars>();
            Debug.Log("[ArenaManager] ✅ PlayerStatusBars добавлен");
        }
    }

    /// <summary>
    /// Перезапустить арену
    /// </summary>
    public void RestartArena()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
