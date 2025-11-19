using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
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
    [SerializeField] private Transform[] multiplayerSpawnPoints; // Спаун-поинты для разных игроков (0-19)

    private GameObject spawnedCharacter;
    private bool isMultiplayer = false;
    private int assignedSpawnIndex = -1; // Индекс точки спавна от сервера (-1 = не назначен)
    private bool spawnIndexReceived = false; // Флаг получения spawnIndex от сервера
    private bool gameStarted = false; // LOBBY SYSTEM: Флаг старта игры

    /// <summary>
    /// Проверить началась ли игра (прошел ли countdown)
    /// </summary>
    public bool IsGameStarted()
    {
        return gameStarted;
    }

    /// <summary>
    /// Получить массив точек спавна для мультиплеера
    /// </summary>
    public Transform[] MultiplayerSpawnPoints
    {
        get { return multiplayerSpawnPoints; }
    }

    void Start()
    {
        // Проверяем мультиплеер режим
        string roomId = PlayerPrefs.GetString("CurrentRoomId", "");
        isMultiplayer = !string.IsNullOrEmpty(roomId);

        if (isMultiplayer)
        {
            Debug.Log("[ArenaManager] 🌐 MULTIPLAYER MODE");
            SetupMultiplayer();

            // КРИТИЧЕСКОЕ: НЕ спавним сразу в мультиплеере!
            // Ждем spawnIndex от сервера
            Debug.Log("[ArenaManager] ⏳ Ожидаем spawnIndex от сервера...");
        }
        else
        {
            Debug.Log("[ArenaManager] 🎮 SINGLEPLAYER MODE");
            // Очищаем мультиплеер данные при запуске одиночной игры
            PlayerPrefs.DeleteKey("CurrentRoomId");
            PlayerPrefs.Save();

            // Singleplayer - спавним сразу
            SpawnSelectedCharacter();
        }

        // ═══════════════════════════════════════════════════════
        // Action Points UI удален (устаревшая система)
        // Новая система использует только Mana
        // ═══════════════════════════════════════════════════════

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

        // 🔥 АВТОМАТИЧЕСКОЕ ИСПРАВЛЕНИЕ Canvas sorting order (НЕ НУЖНО больше жать "Fix All Issues"!)
        if (GetComponent<AutoFixCanvasOnStart>() == null)
        {
            AutoFixCanvasOnStart autoFix = gameObject.AddComponent<AutoFixCanvasOnStart>();
            Debug.Log("✓ Добавлен AutoFixCanvasOnStart (автоматическое исправление UI)");
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
        Vector3 spawnPosition;
        Quaternion spawnRotation;

        // MULTIPLAYER: Используем точку спавна по индексу от сервера
        if (isMultiplayer && assignedSpawnIndex >= 0 && multiplayerSpawnPoints != null && assignedSpawnIndex < multiplayerSpawnPoints.Length)
        {
            Transform spawnTransform = multiplayerSpawnPoints[assignedSpawnIndex];
            spawnPosition = spawnTransform.position;
            spawnRotation = spawnTransform.rotation;
            Debug.Log($"[ArenaManager] 🎯 Использую мультиплеер точку спавна #{assignedSpawnIndex}: {spawnPosition}");
        }
        else
        {
            // Singleplayer или fallback
            spawnPosition = spawnPoint != null ? spawnPoint.position : defaultSpawnPosition;
            spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

            if (isMultiplayer)
            {
                Debug.LogWarning($"[ArenaManager] ⚠️ Индекс спавна не назначен (assignedSpawnIndex={assignedSpawnIndex}), используем дефолтный spawn point");
            }
        }

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
    /// НОВАЯ ВЕРСИЯ: Правильный порядок с PlayerAttackNew, SkillExecutor, ActionPoints
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

        // ═══════════════════════════════════════════════════════════════════
        // ШАГ 1: CharacterController (физика движения)
        // ═══════════════════════════════════════════════════════════════════
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
        charController.skinWidth = 0.03f;         // ~10% от radius
        charController.minMoveDistance = 0f;      // Unity рекомендует 0
        charController.slopeLimit = 45f;
        charController.stepOffset = 0.3f;

        Debug.Log($"✓ CharacterController настроен: Center={charController.center}, Height={charController.height}");

        // Проверяем Animator на Model
        Animator animator = modelTransform.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            Debug.Log($"✓ Animator на Model: {animator.runtimeAnimatorController.name}");
            animator.SetBool("InBattle", true); // Устанавливаем боевую стойку
        }
        else
        {
            Debug.LogWarning("⚠ Animator не настроен на Model!");
        }

        // ═══════════════════════════════════════════════════════════════════
        // ШАГ 2: PlayerDeathHandler (КРИТИЧЕСКИ ВАЖНО - на родительском объекте!)
        // ═══════════════════════════════════════════════════════════════════
        PlayerDeathHandler deathHandler = spawnedCharacter.GetComponent<PlayerDeathHandler>();
        if (deathHandler == null)
        {
            deathHandler = spawnedCharacter.AddComponent<PlayerDeathHandler>();
            Debug.Log("✓ Добавлен PlayerDeathHandler на родительский объект");
        }
        deathHandler.SetLocalPlayer(true); // Локальный игрок ДОЛЖЕН отправлять события смерти на сервер
        Debug.Log("✓ PlayerDeathHandler.SetLocalPlayer(true) для локального игрока");

        // ═══════════════════════════════════════════════════════════════════
        // ШАГ 3-6: SPECIAL характеристики + HP/Mana/ActionPoints
        // КРИТИЧЕСКИ ВАЖНО: Эти системы должны быть первыми!
        // ═══════════════════════════════════════════════════════════════════
        SetupStatsAndSystems(modelTransform);

        // ═══════════════════════════════════════════════════════════════════
        // ШАГ 7: PlayerController (движение, зависит от Agility)
        // ═══════════════════════════════════════════════════════════════════
        PlayerController playerController = modelTransform.GetComponent<PlayerController>();
        if (playerController == null)
        {
            playerController = modelTransform.gameObject.AddComponent<PlayerController>();
            Debug.Log("✓ Добавлен PlayerController (зависит от Agility)");
        }

        // ═══════════════════════════════════════════════════════════════════
        // ШАГ 8: Мобильные контролы (Starter Assets Input System)
        // ═══════════════════════════════════════════════════════════════════
        SetupMobileInputSystem(modelTransform);

        // ═══════════════════════════════════════════════════════════════════
        // ШАГ 9: Система оружия (ClassWeaponManager)
        // ═══════════════════════════════════════════════════════════════════
        SetupWeapons(modelTransform);

        // ═══════════════════════════════════════════════════════════════════
        // ШАГ 10: НОВАЯ СИСТЕМА АТАКИ (PlayerAttackNew + BasicAttackConfig)
        // ═══════════════════════════════════════════════════════════════════
        SetupPlayerAttackNew(modelTransform);

        // ═══════════════════════════════════════════════════════════════════
        // ШАГ 11: Enemy.cs (ВАЖНО! Маркер для таргетинга в PvP)
        // ═══════════════════════════════════════════════════════════════════
        Enemy enemyComponent = modelTransform.GetComponent<Enemy>();
        if (enemyComponent == null)
        {
            enemyComponent = modelTransform.gameObject.AddComponent<Enemy>();
            Debug.Log("✓ Добавлен Enemy.cs (для PvP таргетинга)");
        }

        // ═══════════════════════════════════════════════════════════════════
        // ШАГ 11: NetworkCombatSync (мультиплеер синхронизация)
        // ═══════════════════════════════════════════════════════════════════
        if (isMultiplayer)
        {
            NetworkCombatSync combatSync = modelTransform.GetComponent<NetworkCombatSync>();
            if (combatSync == null)
            {
                combatSync = modelTransform.gameObject.AddComponent<NetworkCombatSync>();
                Debug.Log("✓ Добавлен NetworkCombatSync (мультиплеер)");
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // ШАГ 12-13: Таргетинг (TargetSystem + TargetIndicator)
        // ═══════════════════════════════════════════════════════════════════
        TargetSystem targetSystem = modelTransform.GetComponent<TargetSystem>();
        if (targetSystem == null)
        {
            targetSystem = modelTransform.gameObject.AddComponent<TargetSystem>();
            Debug.Log("✓ Добавлен TargetSystem");
        }

        TargetIndicator targetIndicator = modelTransform.GetComponent<TargetIndicator>();
        if (targetIndicator == null)
        {
            targetIndicator = modelTransform.gameObject.AddComponent<TargetIndicator>();
            Debug.Log("✓ Добавлен TargetIndicator");
            SetupTargetIndicator(targetIndicator, targetSystem, modelTransform);
        }

        // ═══════════════════════════════════════════════════════════════════
        // ШАГ 14: EffectManager (система эффектов: Root, Stun, Slow, DoT)
        // ═══════════════════════════════════════════════════════════════════
        EffectManager effectManager = modelTransform.GetComponent<EffectManager>();
        if (effectManager == null)
        {
            effectManager = modelTransform.gameObject.AddComponent<EffectManager>();
            Debug.Log("✓ Добавлен EffectManager");
        }

        // ═══════════════════════════════════════════════════════════════════
        // ШАГ 15: SkillExecutor (ТОЛЬКО ОН! Без SkillManager)
        // ═══════════════════════════════════════════════════════════════════
        SkillExecutor skillExecutor = modelTransform.GetComponent<SkillExecutor>();
        if (skillExecutor == null)
        {
            skillExecutor = modelTransform.gameObject.AddComponent<SkillExecutor>();
            Debug.Log("✓ Добавлен SkillExecutor (НОВАЯ СИСТЕМА без SkillManager)");
        }

        // ═══════════════════════════════════════════════════════════════════
        // ШАГ 16: Загрузка скиллов НАПРЯМУЮ в SkillExecutor
        // ═══════════════════════════════════════════════════════════════════
        string selectedClass = PlayerPrefs.GetString("SelectedCharacterClass", "Warrior");
        Debug.Log($"[ArenaManager] 🔄 Загрузка скиллов для класса {selectedClass} НАПРЯМУЮ в SkillExecutor...");
        LoadSkillsToExecutor(skillExecutor, selectedClass);

        // ═══════════════════════════════════════════════════════════════════
        // ШАГ 17: Fog of War (система обзора, зависит от Perception)
        // ═══════════════════════════════════════════════════════════════════
        FogOfWar fogOfWar = modelTransform.GetComponent<FogOfWar>();
        if (fogOfWar == null)
        {
            fogOfWar = modelTransform.gameObject.AddComponent<FogOfWar>();
            Debug.Log("✓ Добавлен FogOfWar");
        }
        SetupFogOfWar(fogOfWar);

        // ═══════════════════════════════════════════════════════════════════
        // ШАГ 18: Регистрация в NetworkSyncManager (мультиплеер)
        // ═══════════════════════════════════════════════════════════════════
        if (isMultiplayer && NetworkSyncManager.Instance != null)
        {
            NetworkSyncManager.Instance.SetLocalPlayer(modelTransform.gameObject, selectedClass);
            Debug.Log("[ArenaManager] ✅ Локальный игрок зарегистрирован в NetworkSyncManager");

            // Отправляем начальную позицию на сервер
            if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
            {
                Vector3 initialPosition = spawnedCharacter.transform.position;
                Quaternion initialRotation = spawnedCharacter.transform.rotation;
                SocketIOManager.Instance.UpdatePosition(initialPosition, initialRotation, Vector3.zero, true);
                Debug.Log($"[ArenaManager] ✅ Начальная позиция отправлена на сервер: {initialPosition}");
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // ШАГ 19: Nameplate (никнейм над головой)
        // ═══════════════════════════════════════════════════════════════════
        AddLocalPlayerNameplate(modelTransform);

        Debug.Log("[ArenaManager] ✅✅✅ ВСЕ КОМПОНЕНТЫ НАСТРОЕНЫ В ПРАВИЛЬНОМ ПОРЯДКЕ!");
    }

    /// <summary>
    /// Добавить никнейм над головой локального игрока (зеленый)
    /// </summary>
    private void AddLocalPlayerNameplate(Transform playerTransform)
    {
        string username = PlayerPrefs.GetString("Username", "Player");

        Nameplate nameplate = playerTransform.gameObject.AddComponent<Nameplate>();
        nameplate.Initialize(playerTransform, username, true); // true = зеленый (свой)

        Debug.Log($"[ArenaManager] ✅ Никнейм '{username}' добавлен над головой (зеленый)");
    }

    /// <summary>
    /// Настроить SPECIAL характеристики и зависимые системы
    /// ОБНОВЛЕНО: Добавлен ActionPointsSystem (зависит от Agility)
    /// </summary>
    private void SetupStatsAndSystems(Transform modelTransform)
    {
        // Получаем выбранный класс
        string selectedClass = PlayerPrefs.GetString("SelectedCharacterClass", "Warrior");

        // ═══════════════════════════════════════════════════════════════════
        // 1. CharacterStats (SPECIAL система) - ПЕРВЫМ!
        // ═══════════════════════════════════════════════════════════════════
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

        // ВАЖНО: Принудительно вызываем RecalculateStats() СРАЗУ
        // Это нужно потому что HealthSystem/ManaSystem/ActionPointsSystem могут вызвать Start() раньше CharacterStats
        if (classPreset != null && formulas != null)
        {
            // Применяем характеристики класса
            characterStats.strength = classPreset.strength;
            characterStats.perception = classPreset.perception;
            characterStats.endurance = classPreset.endurance;
            characterStats.wisdom = classPreset.wisdom;
            characterStats.intelligence = classPreset.intelligence;
            characterStats.agility = classPreset.agility;
            characterStats.luck = classPreset.luck;

            // Рассчитываем все производные характеристики
            characterStats.RecalculateStats();
            Debug.Log("✓ CharacterStats инициализированы НЕМЕДЛЕННО (до Start())");
            Debug.Log($"📊 SPECIAL: STR={characterStats.strength}, PER={characterStats.perception}, END={characterStats.endurance}, " +
                      $"WIS={characterStats.wisdom}, INT={characterStats.intelligence}, AGI={characterStats.agility}, LUCK={characterStats.luck}");
            Debug.Log($"📊 Derived: HP={characterStats.MaxHealth:F0}, Mana={characterStats.MaxMana:F0}, AP={characterStats.MaxActionPoints:F0}");
        }

        // ═══════════════════════════════════════════════════════════════════
        // 2. HealthSystem (зависит от Endurance)
        // ═══════════════════════════════════════════════════════════════════
        HealthSystem healthSystem = modelTransform.GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            healthSystem = modelTransform.gameObject.AddComponent<HealthSystem>();
            Debug.Log("✓ Добавлен HealthSystem");
        }

        // ═══════════════════════════════════════════════════════════════════
        // 3. ManaSystem (зависит от Wisdom)
        // ═══════════════════════════════════════════════════════════════════
        ManaSystem manaSystem = modelTransform.GetComponent<ManaSystem>();
        if (manaSystem == null)
        {
            manaSystem = modelTransform.gameObject.AddComponent<ManaSystem>();
            Debug.Log("✓ Добавлен ManaSystem");
        }

        // ═══════════════════════════════════════════════════════════════════
        // 4. ActionPointsSystem (зависит от Agility) - ВОЗВРАЩЁН!
        // ═══════════════════════════════════════════════════════════════════
        ActionPointsSystem actionPointsSystem = modelTransform.GetComponent<ActionPointsSystem>();
        if (actionPointsSystem == null)
        {
            actionPointsSystem = modelTransform.gameObject.AddComponent<ActionPointsSystem>();
            Debug.Log("✓ Добавлен ActionPointsSystem (зависит от Agility)");
        }

        // ═══════════════════════════════════════════════════════════════════
        // 5. LevelingSystem (прокачка)
        // ═══════════════════════════════════════════════════════════════════
        LevelingSystem levelingSystem = modelTransform.GetComponent<LevelingSystem>();
        if (levelingSystem == null)
        {
            levelingSystem = modelTransform.gameObject.AddComponent<LevelingSystem>();
            Debug.Log("✓ Добавлен LevelingSystem");
        }

        // ═══════════════════════════════════════════════════════════════════
        // 6. NetworkLevelingSync (синхронизация прокачки через сеть)
        // ═══════════════════════════════════════════════════════════════════
        NetworkLevelingSync networkLevelingSync = modelTransform.GetComponent<NetworkLevelingSync>();
        if (networkLevelingSync == null)
        {
            networkLevelingSync = modelTransform.gameObject.AddComponent<NetworkLevelingSync>();
            Debug.Log("✓ Добавлен NetworkLevelingSync (синхронизация прокачки с сервером)");
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
    /// Получить префаб персонажа по классу (АВТОЗАГРУЗКА из Resources/Characters/)
    /// </summary>
    private GameObject GetCharacterPrefab(string characterClass)
    {
        // КРИТИЧЕСКОЕ: Загружаем префабы из Resources/Characters/ автоматически
        // Формат: Resources/Characters/{ClassName}Model.prefab
        string prefabPath = $"Characters/{characterClass}Model";
        GameObject prefab = Resources.Load<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"[ArenaManager] ❌ Префаб не найден: Resources/{prefabPath}.prefab");
            Debug.LogError($"[ArenaManager] Убедитесь что префаб {characterClass}Model.prefab находится в Assets/Resources/Characters/");
        }
        else
        {
            Debug.Log($"[ArenaManager] ✅ Префаб загружен: {prefabPath}");
        }

        return prefab;
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

        // Удаляем все старые TPSCameraController (десктопная версия)
        TPSCameraController[] oldTPS = arenaCamera.GetComponents<TPSCameraController>();
        foreach (TPSCameraController tps in oldTPS)
        {
            DestroyImmediate(tps);
            Debug.Log("✓ Удален старый TPSCameraController (desktop)");
        }

        // Удаляем старые TouchCameraController если есть
        TouchCameraController[] oldTouch = arenaCamera.GetComponents<TouchCameraController>();
        foreach (TouchCameraController tc in oldTouch)
        {
            DestroyImmediate(tc);
            Debug.Log("✓ Удален старый TouchCameraController");
        }

        // НОВОЕ: Добавляем TouchCameraController (поддерживает и десктоп, и мобильные)
        TouchCameraController touchCamera = arenaCamera.gameObject.AddComponent<TouchCameraController>();

        // ВАЖНО: Устанавливаем target на Model (а не на родителя!)
        touchCamera.SetTarget(modelTransform);

        Debug.Log($"✓ Настроена Touch камера (desktop + mobile), target = {modelTransform.name}");
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
        ActionPointsUI existingUI = FindFirstObjectByType<ActionPointsUI>();
        if (existingUI != null)
        {
            Debug.Log("✓ ActionPointsUI уже существует");
            return;
        }

        // Находим или создаем Canvas
        UnityEngine.Canvas canvas = FindFirstObjectByType<UnityEngine.Canvas>();
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
        CharacterStatsUI existingUI = FindFirstObjectByType<CharacterStatsUI>();
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
        SimpleStatsHUD existingHUD = FindFirstObjectByType<SimpleStatsHUD>();
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
        // PlayerHUD - HP/MP/Class Icon в верхнем левом углу (ВСЕГДА ВИДНЫ!)
        if (GetComponent<PlayerHUD>() == null)
        {
            gameObject.AddComponent<PlayerHUD>();
            Debug.Log("[ArenaManager] ✅ PlayerHUD добавлен (верхний левый угол - ВСЕГДА ВИДНЫ)");
        }

        // УДАЛЕНО: PlayerStatusBars (HP/MP бары над головой) - заменены на PlayerHUD в углу
    }

    /// <summary>
    /// Перезапустить арену
    /// </summary>
    public void RestartArena()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Установить индекс точки спавна от сервера (для мультиплеера)
    /// </summary>
    public void SetSpawnIndex(int spawnIndex)
    {
        assignedSpawnIndex = spawnIndex;
        spawnIndexReceived = true;
        Debug.Log($"[ArenaManager] 🎯 Сервер назначил точку спавна: #{spawnIndex}");

        // LOBBY SYSTEM: НЕ СПАВНИМ ДО game_start!
        // Просто сохраняем spawnIndex, спавн произойдет при OnGameStarted()
        Debug.Log("[ArenaManager] ⏳ Ждем game_start для спавна...");
    }

    // ===== LOBBY SYSTEM CALLBACKS =====

    private GameObject lobbyUI;
    private UnityEngine.UI.Text countdownText;
    private System.Collections.IEnumerator lobbyCountdownCoroutine;

    /// <summary>
    /// Callback: Лобби создано, начинается 20-секундное ожидание
    /// </summary>
    public void OnLobbyStarted(int waitTimeMs)
    {
        Debug.Log($"[ArenaManager] 🏁 LOBBY STARTED! Ожидание {waitTimeMs}ms (только countdown 3-2-1)");

        // Создаем UI для лобби (только countdown, без текста ожидания)
        CreateLobbyUI();

        // КРИТИЧЕСКОЕ: Запускаем локальный таймер для FALLBACK случаев
        // (когда сервер не отправляет game_countdown события)
        if (lobbyCountdownCoroutine != null)
        {
            StopCoroutine(lobbyCountdownCoroutine);
        }
        lobbyCountdownCoroutine = LobbyCountdownTimer(waitTimeMs / 1000f);
        StartCoroutine(lobbyCountdownCoroutine);
    }

    /// <summary>
    /// Callback: Countdown (3, 2, 1...)
    /// </summary>
    public void OnCountdown(int countdown)
    {
        Debug.Log($"[ArenaManager] ⏱️ COUNTDOWN: {countdown}");

        // Показываем большой countdown
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = countdown.ToString();
        }
    }

    /// <summary>
    /// Создать UI для лобби (только countdown 3-2-1, без текста ожидания)
    /// </summary>
    private void CreateLobbyUI()
    {
        // Находим или создаем Canvas
        UnityEngine.Canvas canvas = FindFirstObjectByType<UnityEngine.Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // Создаем панель лобби
        lobbyUI = new GameObject("LobbyUI");
        lobbyUI.transform.SetParent(canvas.transform, false);

        // ТОЛЬКО Countdown Text (по центру экрана) - ЗОЛОТОЙ, БОЛЬШОЙ
        GameObject countdownObj = new GameObject("Countdown");
        countdownObj.transform.SetParent(lobbyUI.transform, false);

        RectTransform countdownRect = countdownObj.AddComponent<RectTransform>();
        countdownRect.anchorMin = new Vector2(0.5f, 0.5f);
        countdownRect.anchorMax = new Vector2(0.5f, 0.5f);
        countdownRect.pivot = new Vector2(0.5f, 0.5f);
        countdownRect.anchoredPosition = Vector2.zero;
        countdownRect.sizeDelta = new Vector2(400, 200);

        countdownText = countdownObj.AddComponent<UnityEngine.UI.Text>();
        countdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        countdownText.fontSize = 150; // ОГРОМНЫЙ текст
        countdownText.alignment = TextAnchor.MiddleCenter;
        countdownText.color = new Color(0.83f, 0.68f, 0.21f); // ЗОЛОТОЙ цвет (RGB: 212, 175, 55)
        countdownText.text = "3";
        countdownText.gameObject.SetActive(false);

        // Добавляем большую тень
        UnityEngine.UI.Shadow countdownShadow = countdownObj.AddComponent<UnityEngine.UI.Shadow>();
        countdownShadow.effectColor = Color.black;
        countdownShadow.effectDistance = new Vector2(5, -5);

        Debug.Log("[ArenaManager] ✅ Lobby UI создан (только countdown 3-2-1)");
    }


    /// <summary>
    /// Локальный таймер countdown (FALLBACK для случаев когда сервер не отправляет события)
    /// </summary>
    private System.Collections.IEnumerator LobbyCountdownTimer(float waitTimeSeconds)
    {
        Debug.Log($"[ArenaManager] ⏱️ Локальный countdown таймер запущен: {waitTimeSeconds}с");

        // Ждем основное время (waitTimeSeconds - 3 секунды на countdown)
        float countdownStartTime = Mathf.Max(0f, waitTimeSeconds - 3f);
        if (countdownStartTime > 0f)
        {
            Debug.Log($"[ArenaManager] ⏳ Ожидание {countdownStartTime}с до начала countdown...");
            yield return new WaitForSeconds(countdownStartTime);
        }

        // Countdown 3-2-1
        for (int i = 3; i >= 1; i--)
        {
            Debug.Log($"[ArenaManager] ⏱️ COUNTDOWN: {i}");
            OnCountdown(i);
            yield return new WaitForSeconds(1f);
        }

        // GO!
        Debug.Log("[ArenaManager] 🚀 GO! Запускаем игру...");
        OnGameStarted();
    }

    /// <summary>
    /// Callback: Игра началась - СПАВНИМ ВСЕХ ОДНОВРЕМЕННО!
    /// </summary>
    public void OnGameStarted()
    {
        Debug.Log($"[ArenaManager] 🎮 GAME START! Спавним персонажа...");
        gameStarted = true;

        // Останавливаем countdown таймер если он еще работает
        if (lobbyCountdownCoroutine != null)
        {
            StopCoroutine(lobbyCountdownCoroutine);
            lobbyCountdownCoroutine = null;
        }

        // Скрываем countdown и удаляем Lobby UI
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
            Debug.Log("[ArenaManager] ✅ Countdown text скрыт");
        }

        if (lobbyUI != null)
        {
            Destroy(lobbyUI);
            lobbyUI = null;
            countdownText = null;
            Debug.Log("[ArenaManager] ✅ Lobby UI удален");
        }

        // Спавним персонажа
        if (isMultiplayer && spawnedCharacter == null && spawnIndexReceived)
        {
            Debug.Log("[ArenaManager] ✅ Спавним персонажа при game_start");
            SpawnSelectedCharacter();
        }
        else if (!spawnIndexReceived)
        {
            Debug.LogError("[ArenaManager] ❌ game_start получен, но spawnIndex не назначен!");
        }
    }

    /// <summary>
    /// Респавн игрока после смерти на СЛУЧАЙНОЙ точке спавна
    /// ВАЖНО: Для мультиплеера синхронизация через сервер (пока временный fallback)
    /// </summary>
    public void RespawnPlayer()
    {
        if (spawnedCharacter == null)
        {
            Debug.LogError("[ArenaManager] ❌ Невозможно респавнить - персонаж не существует!");
            return;
        }

        // Выбираем СЛУЧАЙНУЮ точку спавна
        Vector3 respawnPosition;
        Quaternion respawnRotation;
        ChooseRandomSpawnPoint(out respawnPosition, out respawnRotation);

        // Телепортируем игрока на точку респавна
        spawnedCharacter.transform.position = respawnPosition;
        spawnedCharacter.transform.rotation = respawnRotation;

        // Находим Model (дочерний объект)
        Transform modelTransform = spawnedCharacter.transform.GetChild(0);
        if (modelTransform == null)
        {
            Debug.LogError("[ArenaManager] ❌ Model не найден при респавне!");
            return;
        }

        // Восстанавливаем HP/MP через HealthSystem и ManaSystem
        HealthSystem healthSystem = modelTransform.GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.Revive(1f); // 100% HP
        }

        ManaSystem manaSystem = modelTransform.GetComponent<ManaSystem>();
        if (manaSystem != null)
        {
            manaSystem.RestoreMana(manaSystem.MaxMana); // Полная мана
        }

        // Сбрасываем анимацию на Idle
        Animator animator = modelTransform.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("InBattle", true);
            // Сбрасываем все триггеры
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Die");
        }

        // Отправляем респавн на сервер (синхронизация позиции)
        if (isMultiplayer && SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
        {
            SocketIOManager.Instance.UpdatePosition(respawnPosition, respawnRotation, Vector3.zero, true);
            Debug.Log("[ArenaManager] ✅ Респавн отправлен на сервер (синхронизация позиции)");
            Debug.LogWarning("[ArenaManager] ⚠️ TODO: Сервер должен выбирать точку спавна для 100% синхронизации!");
        }

        Debug.Log($"[ArenaManager] ✅ Игрок респавнился на случайной точке: {respawnPosition}");
    }

    /// <summary>
    /// Выбрать случайную точку спавна
    /// </summary>
    private void ChooseRandomSpawnPoint(out Vector3 position, out Quaternion rotation)
    {
        // MULTIPLAYER: Используем массив multiplayerSpawnPoints (если есть)
        if (multiplayerSpawnPoints != null && multiplayerSpawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, multiplayerSpawnPoints.Length);
            Transform randomSpawnPoint = multiplayerSpawnPoints[randomIndex];
            position = randomSpawnPoint.position;
            rotation = randomSpawnPoint.rotation;
            Debug.Log($"[ArenaManager] 🎲 Выбрана случайная точка спавна #{randomIndex}: {position}");
            return;
        }

        // FALLBACK: Используем дефолтную точку если multiplayerSpawnPoints не назначены
        if (spawnPoint != null)
        {
            position = spawnPoint.position;
            rotation = spawnPoint.rotation;
            Debug.LogWarning("[ArenaManager] ⚠️ multiplayerSpawnPoints не назначены, используем дефолтный spawnPoint");
        }
        else
        {
            position = defaultSpawnPosition;
            rotation = Quaternion.identity;
            Debug.LogWarning("[ArenaManager] ⚠️ Нет точек спавна, используем defaultSpawnPosition");
        }
    }

    /// <summary>
    /// Получить singleton instance
    /// </summary>
    private static ArenaManager instance;
    public static ArenaManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<ArenaManager>();
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Загрузить все доступные скиллы класса из SkillDatabase
    /// </summary>
    // ═══════════════════════════════════════════════════════════════════
    // СТАРЫЕ МЕТОДЫ УДАЛЕНЫ - используется LoadSkillsToExecutor() напрямую
    // ═══════════════════════════════════════════════════════════════════
    [System.Serializable]
    private class EquippedSkillsData
    {
        public List<int> skillIds;
    }

    // LoadAllSkillsToManager УДАЛЁН - используется LoadSkillsToExecutor() напрямую

    /// <summary>
    /// НОВАЯ СИСТЕМА: Настроить PlayerAttackNew с BasicAttackConfig
    /// </summary>
    private void SetupPlayerAttackNew(Transform modelTransform)
    {
        string selectedClass = PlayerPrefs.GetString("SelectedCharacterClass", "Warrior");

        // Добавляем PlayerAttackNew
        PlayerAttackNew playerAttackNew = modelTransform.GetComponent<PlayerAttackNew>();
        if (playerAttackNew == null)
        {
            playerAttackNew = modelTransform.gameObject.AddComponent<PlayerAttackNew>();
            Debug.Log("✓ Добавлен PlayerAttackNew (НОВАЯ СИСТЕМА)");
        }

        // Загружаем BasicAttackConfig для выбранного класса
        BasicAttackConfig attackConfig = Resources.Load<BasicAttackConfig>($"BasicAttacks/BasicAttackConfig_{selectedClass}");

        if (attackConfig != null)
        {
            // Устанавливаем конфиг через публичное поле
            playerAttackNew.attackConfig = attackConfig;
            Debug.Log($"✓ BasicAttackConfig назначен: {attackConfig.name}");
            Debug.Log($"  → Базовый урон: {attackConfig.baseDamage}");
            Debug.Log($"  → Дистанция: {attackConfig.attackRange}m");
            Debug.Log($"  → Тип атаки: {attackConfig.attackType}");
        }
        else
        {
            Debug.LogError($"❌ BasicAttackConfig_{selectedClass} НЕ НАЙДЕН в Resources/BasicAttacks/!");
            Debug.LogError("   Проверь что файл существует: Assets/Resources/BasicAttacks/BasicAttackConfig_{класс}.asset");
        }
    }

    /// <summary>
    /// НОВАЯ СИСТЕМА: Загрузить скиллы НАПРЯМУЮ в SkillExecutor (без SkillManager)
    /// </summary>
    private void LoadSkillsToExecutor(SkillExecutor skillExecutor, string characterClass)
    {
        if (skillExecutor == null)
        {
            Debug.LogError("[ArenaManager] ❌ SkillExecutor is NULL!");
            return;
        }

        Debug.Log($"[ArenaManager] 📚 Загрузка скиллов для класса: {characterClass} НАПРЯМУЮ в SkillExecutor");

        // Очищаем старые скиллы
        skillExecutor.ClearAllSkills();

        // ПРИОРИТЕТ 1: Загружаем из PlayerPrefs (порядок из Character Selection)
        string equippedSkillsJson = PlayerPrefs.GetString("EquippedSkills", "");

        if (!string.IsNullOrEmpty(equippedSkillsJson))
        {
            try
            {
                // Парсим JSON с порядком скиллов
                EquippedSkillsData data = JsonUtility.FromJson<EquippedSkillsData>(equippedSkillsJson);
                List<int> skillIds = data.skillIds;

                Debug.Log($"[ArenaManager] ✅ Загружено {skillIds.Count} ID скиллов из PlayerPrefs: [{string.Join(", ", skillIds)}]");

                // Загружаем ВСЕ SkillConfig из Resources/Skills/
                SkillConfig[] allSkillConfigs = Resources.LoadAll<SkillConfig>("Skills");
                Debug.Log($"[ArenaManager] 📚 Найдено {allSkillConfigs.Length} SkillConfig в Resources/Skills/");

                // Создаём словарь для быстрого поиска по ID
                Dictionary<int, SkillConfig> skillConfigMap = new Dictionary<int, SkillConfig>();
                foreach (SkillConfig config in allSkillConfigs)
                {
                    if (config != null)
                    {
                        skillConfigMap[config.skillId] = config;
                    }
                }

                // Загружаем скиллы в правильном порядке
                for (int slotIndex = 0; slotIndex < skillIds.Count && slotIndex < 5; slotIndex++)
                {
                    int skillId = skillIds[slotIndex];

                    if (skillConfigMap.ContainsKey(skillId))
                    {
                        SkillConfig skillConfig = skillConfigMap[skillId];
                        int slotNumber = slotIndex + 1; // 1-5
                        skillExecutor.SetSkill(slotNumber, skillConfig);
                        Debug.Log($"[ArenaManager] ✅ Слот {slotNumber}: {skillConfig.skillName} (ID: {skillId})");
                    }
                    else
                    {
                        Debug.LogError($"[ArenaManager] ❌ Скилл с ID {skillId} не найден в Resources/Skills/!");
                    }
                }

                Debug.Log($"[ArenaManager] ✅✅✅ Все скиллы загружены в SkillExecutor в ПРАВИЛЬНОМ ПОРЯДКЕ!");
                return;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ArenaManager] ⚠️ Ошибка загрузки скиллов из PlayerPrefs: {e.Message}");
                Debug.LogWarning($"[ArenaManager] ⚠️ Использую запасной вариант");
            }
        }

        // ЗАПАСНОЙ ВАРИАНТ: Загружаем все скиллы класса по префиксу
        Debug.LogWarning($"[ArenaManager] ⚠️ PlayerPrefs 'EquippedSkills' пуст! Загружаю ВСЕ скиллы класса {characterClass}");

        string skillPrefix = $"{characterClass}_";
        SkillConfig[] allSkills = Resources.LoadAll<SkillConfig>("Skills");

        List<SkillConfig> classSkills = new List<SkillConfig>();
        foreach (SkillConfig skill in allSkills)
        {
            if (skill.name.StartsWith(skillPrefix))
            {
                classSkills.Add(skill);
            }
        }

        if (classSkills.Count == 0)
        {
            Debug.LogError($"[ArenaManager] ❌ Не найдено скиллов для класса {characterClass}!");
            return;
        }

        // Сортируем по skillId
        classSkills.Sort((a, b) => a.skillId.CompareTo(b.skillId));

        // Загружаем первые 5
        for (int i = 0; i < Mathf.Min(5, classSkills.Count); i++)
        {
            int slotNumber = i + 1; // 1-5
            skillExecutor.SetSkill(slotNumber, classSkills[i]);
            Debug.Log($"[ArenaManager] ✅ Слот {slotNumber}: {classSkills[i].skillName} (ЗАПАСНОЙ ВАРИАНТ)");
        }

        Debug.Log($"[ArenaManager] ✅ Загружено {Mathf.Min(5, classSkills.Count)} скиллов (запасной вариант)");
    }

    /// <summary>
    /// Настроить систему мобильного ввода (Starter Assets Input System)
    /// </summary>
    private void SetupMobileInputSystem(Transform playerTransform)
    {
        // 1. Добавляем StarterAssetsInputs (хранит move/look/jump/sprint)
        StarterAssets.StarterAssetsInputs starterInputs = playerTransform.GetComponent<StarterAssets.StarterAssetsInputs>();
        if (starterInputs == null)
        {
            starterInputs = playerTransform.gameObject.AddComponent<StarterAssets.StarterAssetsInputs>();
            Debug.Log("✓ Добавлен StarterAssetsInputs (мобильные + десктоп контролы)");
        }

        // 2. Добавляем PlayerInput (New Input System)
        UnityEngine.InputSystem.PlayerInput playerInput = playerTransform.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput == null)
        {
            playerInput = playerTransform.gameObject.AddComponent<UnityEngine.InputSystem.PlayerInput>();
            Debug.Log("✓ Добавлен PlayerInput (New Input System)");

            // Загружаем Input Actions из Resources (БЕЗ расширения .inputactions)
            UnityEngine.InputSystem.InputActionAsset inputActions = UnityEngine.Resources.Load<UnityEngine.InputSystem.InputActionAsset>("InputSystem/StarterAssets");

            if (inputActions == null)
            {
                // Пытаемся найти в корне Resources
                inputActions = UnityEngine.Resources.Load<UnityEngine.InputSystem.InputActionAsset>("StarterAssets");
            }

            if (inputActions != null)
            {
                playerInput.actions = inputActions;
                playerInput.defaultActionMap = "Player";
                playerInput.notificationBehavior = UnityEngine.InputSystem.PlayerNotifications.InvokeCSharpEvents;
                Debug.Log("✓ Input Actions загружены: StarterAssets.inputactions");
            }
            else
            {
                Debug.LogWarning("⚠️ StarterAssets.inputactions не найден в Resources! Мобильные контролы могут не работать.");
                Debug.LogWarning("   Проверьте путь: Assets/StarterAssets/InputSystem/StarterAssets.inputactions");
            }
        }

        Debug.Log("[ArenaManager] ✅ Система мобильного ввода настроена (Starter Assets + PlayerInput)");
    }
}
