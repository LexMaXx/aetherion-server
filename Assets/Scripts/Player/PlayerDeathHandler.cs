using UnityEngine;
using System.Collections;

/// <summary>
/// Обрабатывает смерть и респавн локального игрока
/// Блокирует движение и атаки, показывает анимацию смерти
/// </summary>
public class PlayerDeathHandler : MonoBehaviour
{
    [Header("Dependencies")]
    private HealthSystem healthSystem;
    private PlayerController playerController;
    private PlayerAttackNew playerAttack;
    private SkillExecutor skillExecutor;
    private Animator animator;
    private CharacterController characterController;
    private Rigidbody rb;
    private TargetableEntity targetableEntity;

    [Header("Death State")]
    private bool isDead = false;
    private float deathTime = 0f;
    private float respawnDelay = 10f; // 10 секунд до респавна

    [Header("Network")]
    [SerializeField] private bool isLocalPlayer = false; // Только локальный игрок отправляет события на сервер

    [Header("Respawn")]
    private Coroutine respawnCoroutine;

    void Awake()
    {
        // ИСПРАВЛЕНО: Все компоненты могут быть как на текущем объекте, так и на Model (дочернем)
        // Используем GetComponentInChildren для поиска везде
        healthSystem = GetComponentInChildren<HealthSystem>();
        playerController = GetComponentInChildren<PlayerController>();
        playerAttack = GetComponentInChildren<PlayerAttackNew>();
        skillExecutor = GetComponentInChildren<SkillExecutor>();
        animator = GetComponentInChildren<Animator>();
        characterController = GetComponentInChildren<CharacterController>();
        rb = GetComponentInChildren<Rigidbody>();
        targetableEntity = GetComponentInChildren<TargetableEntity>();
        if (targetableEntity == null)
        {
            targetableEntity = GetComponent<TargetableEntity>();
        }

        Debug.Log($"[PlayerDeathHandler] Найдены компоненты:");
        Debug.Log($"  HealthSystem: {healthSystem != null}");
        Debug.Log($"  PlayerController: {playerController != null}");
        Debug.Log($"  PlayerAttackNew: {playerAttack != null}");
        Debug.Log($"  SkillExecutor: {skillExecutor != null}");
        Debug.Log($"  CharacterController: {characterController != null}");
        Debug.Log($"  TargetableEntity: {targetableEntity != null}");
    }

    void Start()
    {
        // Подписываемся на событие смерти
        if (healthSystem != null)
        {
            healthSystem.OnDeath += OnPlayerDied;
        }
    }

    void Update()
    {
        // УБРАНО: Блокировка в Update() не нужна, т.к. компоненты отключаются сразу в OnPlayerDied()
        // Оставляем Update() пустым для возможных будущих проверок
    }

    /// <summary>
    /// Установить, является ли этот игрок локальным (true) или сетевым (false)
    /// Только локальный игрок отправляет события смерти на сервер
    /// </summary>
    public void SetLocalPlayer(bool isLocal)
    {
        isLocalPlayer = isLocal;
        Debug.Log($"[PlayerDeathHandler] 🏷️ isLocalPlayer установлен = {isLocal} для {gameObject.name}");
    }

    /// <summary>
    /// Вызывается когда игрок умирает
    /// </summary>
    private void OnPlayerDied()
    {
        if (isDead) return; // Уже мертв

        isDead = true;
        deathTime = Time.time;

        Debug.Log("[PlayerDeathHandler] ☠️ Игрок погиб! Ожидание респавна...");

        // КРИТИЧЕСКИ ВАЖНО: Отменяем трансформацию Bear Form если она активна
        SimpleTransformation transformation = GetComponent<SimpleTransformation>();
        if (transformation != null && transformation.IsTransformed())
        {
            transformation.RevertToOriginal();
            Debug.Log("[PlayerDeathHandler] 🐻 Трансформация Bear Form отменена при смерти");
        }

        // КРИТИЧЕСКИ ВАЖНО: Уничтожаем активного миньона (скелет) если он есть
        SkillExecutor skillExecutorComp = GetComponent<SkillExecutor>();
        if (skillExecutorComp != null)
        {
            skillExecutorComp.CleanupActiveMinion();
            Debug.Log("[PlayerDeathHandler] 💀 Активный миньон уничтожен при смерти владельца");
        }

        // КРИТИЧЕСКИ ВАЖНО: Отключаем движение СРАЗУ (не даем игроку управлять персонажем)
        // ИСПРАВЛЕНИЕ: Компоненты могут быть уже отключены для NetworkPlayer, проверяем isLocalPlayer
        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log($"[PlayerDeathHandler] 🚫 PlayerController отключён (isLocalPlayer={isLocalPlayer})");
        }
        if (playerAttack != null)
        {
            playerAttack.enabled = false;
            Debug.Log($"[PlayerDeathHandler] 🚫 PlayerAttack отключён (isLocalPlayer={isLocalPlayer})");
        }
        if (skillExecutor != null)
        {
            skillExecutor.enabled = false;
            Debug.Log($"[PlayerDeathHandler] 🚫 SkillExecutor отключён (isLocalPlayer={isLocalPlayer})");
        }

        // КРИТИЧЕСКИ ВАЖНО: Отключаем CharacterController НЕМЕДЛЕННО чтобы персонаж не мог двигаться
        if (characterController != null)
        {
            characterController.enabled = false;
            Debug.Log("[PlayerDeathHandler] 🎮 CharacterController отключен СРАЗУ (предотвращение движения)");
        }

        // КРИТИЧЕСКИ ВАЖНО: Опускаем персонажа на землю для анимации смерти
        // ИСПРАВЛЕНО: Опускаем ВСЕХ (и локальных и NetworkPlayer)!
        // NetworkPlayer тоже нужно опустить, чтобы анимация смерти была на земле
        DropToGround();

        // ИСПРАВЛЕНО: НЕ используем Rigidbody для падения
        // Просто оставляем персонажа в текущей позиции без физики
        // Это предотвращает проблемы с позицией после респавна
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            Debug.Log("[PlayerDeathHandler] 🎮 Rigidbody остается kinematic (без физики падения)");
        }

        // Проигрываем анимацию смерти
        PlayDeathAnimation();

        // В мультиплеере сервер управляет респавном
        // В singleplayer запускаем локальный респавн
        if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
        {
            // Single player - респавн локально
            if (respawnCoroutine != null) StopCoroutine(respawnCoroutine);
            respawnCoroutine = StartCoroutine(RespawnAfterDelay(respawnDelay));
        }
        else
        {
            // Multiplayer - отправляем смерть на сервер
            SendDeathToServer();

            // Запускаем локальный респавн (сервер подтвердит)
            if (respawnCoroutine != null) StopCoroutine(respawnCoroutine);
            respawnCoroutine = StartCoroutine(RespawnAfterDelay(respawnDelay));

            Debug.Log("[PlayerDeathHandler] 🌐 Смерть отправлена на сервер, запущен таймер респавна 10 сек...");
        }
    }

    /// <summary>
    /// Отправить событие смерти на сервер
    /// </summary>
    private void SendDeathToServer()
    {
        // КРИТИЧНО: NetworkPlayer НЕ должен отправлять события смерти (только получать)
        if (!isLocalPlayer)
        {
            Debug.Log($"[PlayerDeathHandler] ⏭️ Пропущена отправка player_died (это NetworkPlayer, не локальный игрок)");
            return;
        }

        if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
        {
            Debug.LogWarning("[PlayerDeathHandler] ⚠️ SocketIOManager не подключен!");
            return;
        }

        // Отправляем пустой объект - сервер использует socket.id из WebSocket подключения
        // Можно добавить дополнительную информацию (убийца, причина смерти и т.д.)
        string deathData = "{}";
        SocketIOManager.Instance.Emit("player_died", deathData);

        Debug.Log($"[PlayerDeathHandler] 📤 Отправлено player_died на сервер");
    }

    /// <summary>
    /// Проиграть анимацию смерти
    /// </summary>
    private void PlayDeathAnimation()
    {
        if (animator == null)
        {
            Debug.LogWarning("[PlayerDeathHandler] ⚠️ Animator не найден!");
            return;
        }

        // Проверяем есть ли параметр isDead
        if (HasAnimatorParameter(animator, "isDead"))
        {
            animator.SetBool("isDead", true);
            Debug.Log("[PlayerDeathHandler] 🎬 Анимация смерти запущена (isDead=true)");
        }
        else if (HasAnimatorParameter(animator, "Death"))
        {
            animator.SetTrigger("Death");
            Debug.Log("[PlayerDeathHandler] 🎬 Анимация смерти запущена (Death trigger)");
        }
        else
        {
            Debug.LogWarning("[PlayerDeathHandler] ⚠️ Параметры isDead/Death не найдены в Animator!");
        }

        // Останавливаем движение
        animator.SetBool("IsMoving", false);
        animator.SetFloat("MoveX", 0);
        animator.SetFloat("MoveY", 0);
    }

    /// <summary>
    /// Корутина для респавна с задержкой
    /// </summary>
    private IEnumerator RespawnAfterDelay(float delay)
    {
        Debug.Log($"[PlayerDeathHandler] ⏳ Респавн через {delay} секунд...");
        yield return new WaitForSeconds(delay);

        // Проверяем режим игры
        bool isMultiplayer = SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected;

        if (isMultiplayer)
        {
            // MULTIPLAYER: Запрашиваем респавн у сервера
            RequestRespawnFromServer();
        }
        else
        {
            // SINGLEPLAYER: Респавним локально через ArenaManager
            if (ArenaManager.Instance != null)
            {
                ArenaManager.Instance.RespawnPlayer();
            }
            else if (BattleSceneManager.Instance != null)
            {
                Vector3 respawnPosition;
                Quaternion respawnRotation;
                if (BattleSceneManager.Instance.TryGetRandomSpawnPoint(out respawnPosition, out respawnRotation))
                {
                    Respawn(respawnPosition);
                    transform.rotation = respawnRotation;
                }
                else
                {
                    Debug.LogWarning("[PlayerDeathHandler] ⚠️ Не удалось получить spawn point из BattleSceneManager, используем текущую позицию");
                    Respawn(transform.position);
                }
            }
            else
            {
                // Fallback - просто восстанавливаем HP на текущей позиции
                Debug.LogWarning("[PlayerDeathHandler] ⚠️ ArenaManager не найден! Используем fallback респавн на текущей позиции");
                Respawn(transform.position);
            }
        }
    }

    /// <summary>
    /// Запросить респавн у сервера (multiplayer)
    /// </summary>
    private void RequestRespawnFromServer()
    {
        if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
        {
            Debug.LogError("[PlayerDeathHandler] ❌ SocketIOManager не подключен!");
            return;
        }

        // Получаем наш socketId
        string mySocketId = "";
        if (NetworkSyncManager.Instance != null)
        {
            mySocketId = NetworkSyncManager.Instance.LocalPlayerSocketId;
        }

        if (string.IsNullOrEmpty(mySocketId))
        {
            Debug.LogError("[PlayerDeathHandler] ❌ Не удалось получить LocalPlayerSocketId!");
            return;
        }

        // Отправляем запрос на респавн
        string requestData = $"{{\\\"socketId\\\":\\\"{mySocketId}\\\"}}";
        SocketIOManager.Instance.Emit("request_respawn", requestData);

        Debug.Log($"[PlayerDeathHandler] 📤 Запрос на респавн отправлен: {requestData}");
    }

    /// <summary>
    /// Воскресить игрока (вызывается из NetworkSyncManager или ArenaManager)
    /// </summary>
    public void Respawn(Vector3 position)
    {
        Debug.Log($"[PlayerDeathHandler] ⚕️ Респавн игрока на позиции {position}");

        // Отменяем корутину респавна если она работает
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }

        // Сбрасываем состояние смерти
        isDead = false;

        // КРИТИЧЕСКИ ВАЖНО: Порядок восстановления HP!
        // 1. Сначала восстанавливаем HealthSystem (источник истины для HP)
        if (healthSystem != null)
        {
            healthSystem.Revive(1f); // 100% HP
            Debug.Log($"[PlayerDeathHandler] 💚 HealthSystem восстановлен: {healthSystem.CurrentHealth}/{healthSystem.MaxHealth}");
        }

        // 2. Затем синхронизируем TargetableEntity с HealthSystem
        TargetableEntity targetable = targetableEntity;
        if (targetable == null)
        {
            targetable = GetComponentInChildren<TargetableEntity>();
            if (targetable != null)
            {
                targetableEntity = targetable;
            }
        }

        if (targetable != null && healthSystem != null)
        {
            // Принудительно синхронизируем HP из HealthSystem
            targetable.Revive(1f);

            // ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА: убеждаемся что HP совпадают
            float healthSystemHP = healthSystem.CurrentHealth;
            float targetableHP = targetable.GetCurrentHealth();

            if (Mathf.Abs(healthSystemHP - targetableHP) > 0.1f)
            {
                Debug.LogError($"[PlayerDeathHandler] ❌ РАССИНХРОНИЗАЦИЯ HP! HealthSystem={healthSystemHP}, TargetableEntity={targetableHP}");
                // Принудительно выравниваем
                targetable.SetHealth(healthSystemHP, healthSystem.MaxHealth);
            }

            Debug.Log($"[PlayerDeathHandler] ✅ TargetableEntity восстановлен: isAlive={targetable.IsAlive()}, HP={targetableHP}/{healthSystem.MaxHealth}");
        }

        // Телепортируем на точку респавна
        // ИСПРАВЛЕНО: Используем позицию напрямую без добавления высоты
        // CharacterController автоматически поставит персонажа на землю
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            transform.position = position;
            cc.enabled = true;
            Debug.Log($"[PlayerDeathHandler] 📍 Телепорт CharacterController на {position}");
        }
        else
        {
            transform.position = position;
            Debug.Log($"[PlayerDeathHandler] 📍 Телепорт без CharacterController на {position}");
        }

        // Включаем CharacterController обратно
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // КРИТИЧЕСКИ ВАЖНО: Отключаем Rigidbody обратно (CharacterController теперь контролирует движение)
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log("[PlayerDeathHandler] 🎮 Rigidbody отключен (CharacterController теперь активен)");
        }

        // КРИТИЧЕСКИ ВАЖНО: Включаем управление обратно ТОЛЬКО для локального игрока!
        // NetworkPlayer НЕ должен включать PlayerController/PlayerAttack (они управляются сервером)
        if (isLocalPlayer)
        {
            // Локальный игрок - включаем все компоненты управления
            if (playerController != null) playerController.enabled = true;
            if (playerAttack != null) playerAttack.enabled = true;
            if (skillExecutor != null) skillExecutor.enabled = true;
            Debug.Log("[PlayerDeathHandler] ✅ Компоненты управления ВКЛЮЧЕНЫ (локальный игрок)");
        }
        else
        {
            // NetworkPlayer - НЕ включаем локальные компоненты (они должны оставаться отключенными)
            Debug.Log("[PlayerDeathHandler] ⏭️ Компоненты управления НЕ включены (NetworkPlayer - управляется сервером)");
        }

        // Сбрасываем анимацию смерти
        if (animator != null)
        {
            // КРИТИЧЕСКИ ВАЖНО: Сбрасываем все состояния Animator
            if (HasAnimatorParameter(animator, "isDead"))
            {
                animator.SetBool("isDead", false);
                Debug.Log("[PlayerDeathHandler] 🎬 Animator: isDead = false");
            }

            // Сбрасываем все параметры движения
            if (HasAnimatorParameter(animator, "IsMoving"))
                animator.SetBool("IsMoving", false);

            if (HasAnimatorParameter(animator, "MoveX"))
                animator.SetFloat("MoveX", 0);

            if (HasAnimatorParameter(animator, "MoveY"))
                animator.SetFloat("MoveY", 0);

            if (HasAnimatorParameter(animator, "Speed"))
                animator.SetFloat("Speed", 0);

            // Сбрасываем все триггеры (Attack, Death и т.д.)
            if (HasAnimatorParameter(animator, "Attack"))
                animator.ResetTrigger("Attack");

            if (HasAnimatorParameter(animator, "Death"))
                animator.ResetTrigger("Death");

            // КРИТИЧЕСКИ ВАЖНО: Принудительно воспроизводим Idle
            animator.Play("Idle", 0, 0f);

            Debug.Log("[PlayerDeathHandler] 🎬 Animator полностью сброшен в Idle");
        }

        // КРИТИЧЕСКИ ВАЖНО: Принудительно отправляем анимацию Idle другим игрокам!
        // Без этого другие игроки продолжат видеть анимацию Dead
        // ВАЖНО: Только локальный игрок отправляет анимацию на сервер
        if (isLocalPlayer && SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
        {
            SocketIOManager.Instance.UpdateAnimation("Idle", 1.0f);
            Debug.Log("[PlayerDeathHandler] 📡 Отправлена анимация Idle после респавна");
        }

        // КРИТИЧЕСКИ ВАЖНО: Отправляем обновленные HP на сервер после респавна!
        // Это гарантирует что сервер знает правильный maxHealth
        SendStatsToServer();

        Debug.Log("[PlayerDeathHandler] ✅ Игрок воскрешен!");
    }

    /// <summary>
    /// Отправить HP и stats на сервер для синхронизации
    /// Вызывается после респавна чтобы синхронизировать HP с сервером
    /// </summary>
    private void SendStatsToServer()
    {
        // КРИТИЧНО: NetworkPlayer НЕ должен отправлять stats на сервер
        if (!isLocalPlayer)
        {
            Debug.Log("[PlayerDeathHandler] ⏭️ Пропущена отправка stats (это NetworkPlayer, не локальный игрок)");
            return;
        }

        if (healthSystem == null)
        {
            Debug.LogError("[PlayerDeathHandler] ❌ HealthSystem == null, не могу отправить stats!");
            return;
        }

        CharacterStats characterStats = GetComponent<CharacterStats>();
        if (characterStats == null)
        {
            Debug.LogError("[PlayerDeathHandler] ❌ CharacterStats не найден!");
            return;
        }

        // Собираем данные
        float maxHealth = healthSystem.MaxHealth;
        float currentHealth = healthSystem.CurrentHealth;

        // КРИТИЧЕСКИ ВАЖНО: Используем serializable классы вместо anonymous types!
        // JsonUtility.ToJson() возвращает "{}" для anonymous types
        PlayerStatsData stats = new PlayerStatsData
        {
            strength = characterStats.strength,
            perception = characterStats.perception,
            endurance = characterStats.endurance,
            wisdom = characterStats.wisdom,
            intelligence = characterStats.intelligence,
            agility = characterStats.agility,
            luck = characterStats.luck
        };

        // Формируем JSON
        UpdatePlayerStatsData data = new UpdatePlayerStatsData
        {
            maxHealth = maxHealth,
            currentHealth = currentHealth,
            stats = stats
        };

        string json = JsonUtility.ToJson(data);

        Debug.Log($"[PlayerDeathHandler] 📊 Отправка stats после респавна:");
        Debug.Log($"  MaxHealth: {maxHealth}");
        Debug.Log($"  CurrentHealth: {currentHealth}");
        Debug.Log($"  Endurance: {characterStats.endurance}");

        // Отправляем на сервер через SocketIOManager
        if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
        {
            SocketIOManager.Instance.Emit("update_player_stats", json);
            Debug.Log("[PlayerDeathHandler] ✅ Stats отправлены на сервер после респавна!");
        }
        else
        {
            Debug.LogWarning("[PlayerDeathHandler] ⚠️ SocketIOManager не подключен, stats не отправлены");
        }
    }

    /// <summary>
    /// Проверить есть ли параметр в Animator
    /// </summary>
    private bool HasAnimatorParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    /// <summary>
    /// Публичные геттеры
    /// </summary>
    public bool IsDead => isDead;
    public float TimeSinceDeath => isDead ? Time.time - deathTime : 0f;
    public float RespawnTimeLeft => isDead ? Mathf.Max(0f, respawnDelay - TimeSinceDeath) : 0f;

    void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDeath -= OnPlayerDied;
        }

        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
        }
    }

    /// <summary>
    /// Serializable класс для отправки stats на сервер
    /// КРИТИЧЕСКИ ВАЖНО: JsonUtility.ToJson() НЕ работает с anonymous types!
    /// </summary>
    [System.Serializable]
    private class PlayerStatsData
    {
        public int strength;
        public int perception;
        public int endurance;
        public int wisdom;
        public int intelligence;
        public int agility;
        public int luck;
    }

    /// <summary>
    /// Serializable класс для отправки HP и stats на сервер
    /// </summary>
    [System.Serializable]
    private class UpdatePlayerStatsData
    {
        public float maxHealth;
        public float currentHealth;
        public PlayerStatsData stats;
    }

    /// <summary>
    /// Опустить персонажа на землю (для анимации смерти на холмах)
    /// Использует raycast вниз для поиска земли
    /// </summary>
    private void DropToGround()
    {
        // Raycast вниз от центра персонажа
        Vector3 rayStart = transform.position + Vector3.up * 0.5f; // Начинаем с центра персонажа
        RaycastHit hit;

        // Кастуем луч вниз на расстояние 10 метров (достаточно для большинства холмов)
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f, LayerMask.GetMask("Default", "Terrain", "Ground")))
        {
            // Земля найдена! Опускаем персонажа на землю
            Vector3 groundPosition = hit.point;

            // Небольшой offset чтобы персонаж был точно на земле (0.05м)
            groundPosition.y += 0.05f;

            transform.position = groundPosition;

            Debug.Log($"[PlayerDeathHandler] 🌍 Персонаж опущен на землю: {groundPosition} (raycast hit: {hit.collider.name})");
        }
        else
        {
            // Земля не найдена в радиусе 10м - просто опускаем на 1 метр вниз
            Vector3 fallbackPosition = transform.position;
            fallbackPosition.y -= 1f;
            transform.position = fallbackPosition;

            Debug.LogWarning($"[PlayerDeathHandler] ⚠️ Земля не найдена raycast! Опускаем на 1м вниз: {fallbackPosition}");
        }
    }
}
