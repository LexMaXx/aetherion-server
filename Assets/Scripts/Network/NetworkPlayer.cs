using UnityEngine;
using TMPro;

/// <summary>
/// Представляет удаленного игрока в мультиплеере
/// Синхронизирует позицию, анимацию, здоровье
/// </summary>
public class NetworkPlayer : MonoBehaviour
{
    [Header("Network Info")]
    public string socketId;
    public string username;
    public string characterClass;

    [Header("Components")]
    private Animator animator;
    private CharacterController characterController;
    private NetworkTransform networkTransform;

    [Header("Health (for health bar only)")]
    private int currentHP = 100;
    private int maxHP = 100;

    [Header("Sync Settings")]
    [SerializeField] private float positionLerpSpeed = 30f; // Увеличено для 60Hz PvP (было 10 → 20 → 30)
    [SerializeField] private float rotationLerpSpeed = 40f; // Увеличено для 60Hz PvP (было 10 → 20 → 40)

    // Target state (received from server)
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private string currentAnimationState = "Idle";

    // Health
    private int currentMP = 100;
    private int maxMP = 100;

    // Interpolation
    private bool hasReceivedFirstUpdate = false;

    void Awake()
    {
        // ВАЖНО: Animator находится на дочернем объекте "Model"
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"[NetworkPlayer] ⚠️ Animator не найден для {gameObject.name}!");
        }

        characterController = GetComponent<CharacterController>();

        // ВАЖНО: Отключаем CharacterController для сетевых игроков
        // Иначе он будет применять гравитацию и коллизии, которые конфликтуют с сетевой позицией
        if (characterController != null)
        {
            characterController.enabled = false;
            Debug.Log("[NetworkPlayer] ✅ CharacterController отключён для сетевого игрока");
        }

        // Добавляем или получаем NetworkTransform для плавной синхронизации
        networkTransform = GetComponent<NetworkTransform>();
        if (networkTransform == null)
        {
            networkTransform = gameObject.AddComponent<NetworkTransform>();
        }

        // Disable local player components for network players
        var playerController = GetComponent<PlayerController>();
        if (playerController != null) playerController.enabled = false;

        var playerAttack = GetComponent<PlayerAttack>();
        if (playerAttack != null) playerAttack.enabled = false;

        var targetSystem = GetComponent<TargetSystem>();
        if (targetSystem != null) targetSystem.enabled = false;

        // But enable health/stats for damage visualization
        // Keep CharacterStatsData, HealthSystem, etc enabled
    }

    void Start()
    {
        // ВАЖНО: Устанавливаем боевую стойку для NetworkPlayer (InBattle = true)
        if (animator != null)
        {
            // Проверяем есть ли параметр InBattle
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "InBattle")
                {
                    animator.SetBool("InBattle", true);
                    Debug.Log($"[NetworkPlayer] ✅ Боевая стойка установлена для {username}");
                    break;
                }
            }
        }
    }

    void Update()
    {
        if (!hasReceivedFirstUpdate) return;

        // ВАЖНО: Если NetworkTransform существует, он управляет позицией
        // Иначе управляем позицией здесь
        if (networkTransform == null)
        {
            // Smooth position interpolation
            // CharacterController отключён, используем прямую установку transform.position
            transform.position = Vector3.Lerp(transform.position, targetPosition, positionLerpSpeed * Time.deltaTime);

            // Smooth rotation interpolation
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);
        }

    }

    // УДАЛЕНО: Старая система nameplate - заменена на EnemyNameplate.cs

    /// <summary>
    /// Обновить позицию от сервера (с поддержкой velocity для Dead Reckoning)
    /// </summary>
    public void UpdatePosition(Vector3 position, Quaternion rotation, Vector3 velocity = default, float timestamp = 0f)
    {
        if (timestamp == 0f)
        {
            timestamp = Time.time;
        }

        // ДИАГНОСТИКА: Логируем обновление позиции
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[NetworkPlayer] 🔧 UpdatePosition для {username}: current=({transform.position.x:F2}, {transform.position.y:F2}, {transform.position.z:F2}), target=({position.x:F2}, {position.y:F2}, {position.z:F2}), distance={Vector3.Distance(transform.position, position):F2}m");
        }

        // Используем NetworkTransform для плавной синхронизации
        if (networkTransform != null)
        {
            networkTransform.ReceivePositionUpdate(position, rotation, velocity, timestamp);
        }
        else
        {
            // Fallback к старому методу если NetworkTransform отсутствует
            targetPosition = position;
            targetRotation = rotation;

            if (!hasReceivedFirstUpdate)
            {
                // First update - teleport
                transform.position = position;
                transform.rotation = rotation;
                hasReceivedFirstUpdate = true;
                Debug.Log($"[NetworkPlayer] 🎯 Первая позиция для {username}: ({position.x:F2}, {position.y:F2}, {position.z:F2})");
            }
        }

        hasReceivedFirstUpdate = true;
    }

    /// <summary>
    /// Обновить анимацию от сервера
    /// </summary>
    public void UpdateAnimation(string animationState)
    {
        // ДИАГНОСТИКА: Логируем ВСЕ попытки обновления анимации (ВСЕГДА, не только каждую секунду!)
        Debug.Log($"[NetworkPlayer] 🔄 UpdateAnimation вызван для {username}: текущее={currentAnimationState}, новое={animationState}");

        if (animator == null)
        {
            Debug.LogError($"[NetworkPlayer] ❌ КРИТИЧЕСКАЯ ОШИБКА: Animator is null для {username}!");
            Debug.LogError($"[NetworkPlayer] 🔍 Пытаюсь найти Animator в дочерних объектах...");

            // Пытаемся найти Animator снова
            animator = GetComponentInChildren<Animator>();

            if (animator == null)
            {
                Debug.LogError($"[NetworkPlayer] ❌ Animator НЕ НАЙДЕН даже в дочерних объектах!");
                return;
            }
            else
            {
                Debug.Log($"[NetworkPlayer] ✅ Animator найден в дочернем объекте: {animator.gameObject.name}");
            }
        }

        // ВАЖНО: ВСЕГДА обновляем анимацию для реал-тайм синхронизации
        bool stateChanged = (currentAnimationState != animationState);

        if (stateChanged)
        {
            Debug.Log($"[NetworkPlayer] 🎬 Анимация для {username}: {currentAnimationState} → {animationState}");
            currentAnimationState = animationState;
        }
        else
        {
            Debug.Log($"[NetworkPlayer] 🔄 Повторное применение анимации {animationState} для {username}");
        }

        // ВАЖНО: PlayerController использует Blend Tree систему
        // IsMoving (bool), MoveX (float), MoveY (float)
        // Не используем isWalking/isRunning, потому что они отсутствуют

        // Set new state
        Debug.Log($"[NetworkPlayer] 🎭 Применяю анимацию '{animationState}' для {username}");

        switch (animationState)
        {
            case "Idle":
                Debug.Log($"[NetworkPlayer] ➡️ Idle: IsMoving=false, MoveX=0, MoveY=0, speed=1.0");
                animator.SetBool("IsMoving", false);
                animator.SetFloat("MoveX", 0);
                animator.SetFloat("MoveY", 0);
                animator.speed = 1.0f;
                break;

            case "Walking":
                Debug.Log($"[NetworkPlayer] ➡️ Walking: IsMoving=true, MoveX=0, MoveY=0.5, speed=0.5");
                animator.SetBool("IsMoving", true);
                animator.SetFloat("MoveX", 0);
                animator.SetFloat("MoveY", 0.5f); // 0.5 = Slow Run (ходьба)
                animator.speed = 0.5f; // Замедленная анимация для ходьбы
                break;

            case "Running":
                Debug.Log($"[NetworkPlayer] ➡️ Running: IsMoving=true, MoveX=0, MoveY=1.0, speed=1.0");
                animator.SetBool("IsMoving", true);
                animator.SetFloat("MoveX", 0);
                animator.SetFloat("MoveY", 1.0f); // 1.0 = Sprint (бег)
                animator.speed = 1.0f; // Нормальная скорость анимации
                break;

            case "Attacking":
                Debug.Log($"[NetworkPlayer] ➡️ Attacking: Trigger=Attack");
                animator.SetTrigger("Attack");
                // Не меняем IsMoving - атака может быть во время движения
                break;

            case "Dead":
                Debug.Log($"[NetworkPlayer] ➡️ Dead: isDead=true, IsMoving=false");
                if (HasAnimatorParameter(animator, "isDead"))
                {
                    animator.SetBool("isDead", true);
                }
                animator.SetBool("IsMoving", false);
                break;

            case "Casting":
                Debug.Log($"[NetworkPlayer] ➡️ Casting: Trigger=Cast");
                animator.SetTrigger("Cast");
                break;

            default:
                Debug.LogWarning($"[NetworkPlayer] ⚠️ Неизвестное состояние анимации: '{animationState}' для {username}");
                break;
        }

        // Логируем итоговое состояние параметров аниматора
        Debug.Log($"[NetworkPlayer] 📊 Состояние Animator для {username}: IsMoving={animator.GetBool("IsMoving")}, MoveY={animator.GetFloat("MoveY"):F2}, speed={animator.speed:F2}");
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
    /// Обновить здоровье от сервера
    /// </summary>
    public void UpdateHealth(int hp, int maxHp, int mp, int maxMp)
    {
        currentHP = hp;
        maxHP = maxHp;
        currentMP = mp;
        maxMP = maxMp;

        // УДАЛЕНО: UpdateHealthBar() - теперь EnemyNameplate сам обновляет HP бар через LateUpdate

        // Check death
        if (currentHP <= 0)
        {
            OnDeath();
        }
    }

    // УДАЛЕНО: UpdateHealthBar() - теперь управляется через EnemyNameplate.cs

    /// <summary>
    /// Обработать смерть
    /// </summary>
    private void OnDeath()
    {
        UpdateAnimation("Dead");
        Debug.Log($"[NetworkPlayer] {username} погиб!");

        // Disable collider so players can walk through corpse
        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
    }

    /// <summary>
    /// Обработать респавн
    /// </summary>
    public void OnRespawn(Vector3 spawnPosition)
    {
        currentHP = maxHP;
        currentMP = maxMP;

        UpdatePosition(spawnPosition, Quaternion.identity);
        UpdateAnimation("Idle");

        // УДАЛЕНО: UpdateHealthBar() - теперь EnemyNameplate сам обновляет HP бар

        // Re-enable collider
        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = true;

        Debug.Log($"[NetworkPlayer] {username} возродился!");
    }

    /// <summary>
    /// Воспроизвести анимацию атаки С ЭФФЕКТАМИ
    /// </summary>
    public void PlayAttackAnimation(string attackType)
    {
        if (animator == null) return;

        Debug.Log($"[NetworkPlayer] 🎬 PlayAttackAnimation для {username}: тип={attackType}");

        // ЭФФЕКТ 1: СВЕЧЕНИЕ ОРУЖИЯ (через ClassWeaponManager)
        ClassWeaponManager weaponManager = GetComponentInChildren<ClassWeaponManager>();
        if (weaponManager != null)
        {
            Debug.Log($"[NetworkPlayer] ✨ Активирую свечение оружия для {username}");
            weaponManager.ActivateWeaponGlow();

            // Автоотключение через 1 секунду
            StartCoroutine(DeactivateWeaponGlowAfterDelay(weaponManager, 1.0f));
        }
        else
        {
            Debug.LogWarning($"[NetworkPlayer] ⚠️ ClassWeaponManager не найден для {username} - свечение оружия не работает");
        }

        // ЭФФЕКТ 2: АНИМАЦИЯ АТАКИ
        switch (attackType)
        {
            case "melee":
                Debug.Log($"[NetworkPlayer] ⚔️ Ближняя атака: Trigger=Attack");
                animator.SetTrigger("Attack");
                break;

            case "ranged":
                Debug.Log($"[NetworkPlayer] 🏹 Дальняя атака: Trigger=RangedAttack (+ снаряд)");
                if (HasAnimatorParameter(animator, "RangedAttack"))
                {
                    animator.SetTrigger("RangedAttack");
                }
                else
                {
                    // Fallback если нет RangedAttack
                    animator.SetTrigger("Attack");
                }

                // ЭФФЕКТ 3: СОЗДАНИЕ СНАРЯДА для дальних атак
                StartCoroutine(SpawnProjectileAfterDelay(attackType, 0.3f));
                break;

            case "skill":
            case "magic":
                Debug.Log($"[NetworkPlayer] 🔮 Магия/Скилл: Trigger=Cast");
                animator.SetTrigger("Cast");
                break;
        }
    }

    /// <summary>
    /// Корутина: отключить свечение оружия через задержку
    /// </summary>
    private System.Collections.IEnumerator DeactivateWeaponGlowAfterDelay(ClassWeaponManager weaponManager, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (weaponManager != null)
        {
            weaponManager.DeactivateWeaponGlow();
            Debug.Log($"[NetworkPlayer] 💤 Свечение оружия отключено для {username}");
        }
    }

    /// <summary>
    /// Корутина: создать снаряд через задержку (для дальних атак)
    /// </summary>
    private System.Collections.IEnumerator SpawnProjectileAfterDelay(string attackType, float delay)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log($"[NetworkPlayer] 🚀 Создание снаряда для {username}, класс={characterClass}");

        // Определяем префаб снаряда по классу персонажа
        string projectileName = GetProjectilePrefabName(characterClass);

        if (string.IsNullOrEmpty(projectileName))
        {
            Debug.LogWarning($"[NetworkPlayer] ⚠️ Нет снаряда для класса {characterClass}");
            yield break;
        }

        // Загружаем префаб снаряда
        GameObject projectilePrefab = Resources.Load<GameObject>($"Projectiles/{projectileName}");

        if (projectilePrefab == null)
        {
            // Пытаемся найти в Assets/Prefabs/Projectiles/
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{projectileName} t:Prefab");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                projectilePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Debug.Log($"[NetworkPlayer] ✅ Префаб загружен из Assets: {path}");
            }
#endif
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[NetworkPlayer] ⚠️ Префаб снаряда '{projectileName}' не найден!");
            yield break;
        }

        // Находим точку спавна снаряда (правая рука или WeaponTip)
        Transform spawnPoint = FindWeaponTip();
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position + transform.forward * 0.5f + Vector3.up * 1.5f;

        // Направление полета (вперёд от игрока)
        Vector3 direction = transform.forward;

        // Создаём снаряд
        GameObject projectileObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.LookRotation(direction));

        // Настраиваем снаряд (если есть компонент Projectile)
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            // Для NetworkPlayer снаряд чисто визуальный (урон обрабатывает сервер)
            // Цель = null, урон = 0, просто летит вперёд
            // owner = this.gameObject чтобы снаряд не попадал в своего владельца
            projectile.Initialize(null, 0f, direction, this.gameObject);
            Debug.Log($"[NetworkPlayer] ✅ Снаряд создан: {projectileName} для {username}");
        }
    }

    /// <summary>
    /// Получить имя префаба снаряда по классу персонажа
    /// </summary>
    private string GetProjectilePrefabName(string className)
    {
        switch (className)
        {
            case "Archer":
                return "ArrowProjectile";
            case "Mage":
                return "FireballProjectile";
            case "Rogue":
                return "SoulShardsProjectile";
            default:
                return null; // Воин и Паладин - ближний бой, снарядов нет
        }
    }

    /// <summary>
    /// Найти точку оружия для спавна снарядов
    /// </summary>
    private Transform FindWeaponTip()
    {
        // Пытаемся найти WeaponTip
        Transform weaponTip = transform.Find("WeaponTip");
        if (weaponTip != null) return weaponTip;

        // Ищем в дочерних объектах
        string[] weaponTipNames = new string[]
        {
            "WeaponTip",
            "Weapon_Tip",
            "RightHandIndex3",
            "mixamorig:RightHandIndex3",
            "RightHand",
            "mixamorig:RightHand"
        };

        Transform[] allTransforms = GetComponentsInChildren<Transform>();
        foreach (string tipName in weaponTipNames)
        {
            foreach (Transform t in allTransforms)
            {
                if (t.name.Contains(tipName))
                {
                    Debug.Log($"[NetworkPlayer] ✅ Найдена точка оружия: {t.name}");
                    return t;
                }
            }
        }

        Debug.LogWarning($"[NetworkPlayer] ⚠️ Точка оружия не найдена для {username}");
        return null;
    }

    /// <summary>
    /// Показать урон (визуальный эффект + взрыв)
    /// ИЗМЕНЕНО: Отключено мигание красным по запросу пользователя
    /// </summary>
    public void ShowDamage(float damage)
    {
        Debug.Log($"[NetworkPlayer] {username} получил {damage} урона!");

        // ЭФФЕКТ 1: Мигание красным (ОТКЛЮЧЕНО)
        // StartCoroutine(FlashRed());

        // ЭФФЕКТ 2: Hit effect (включён обратно с оптимизацией - не зацикленный)
        SpawnHitEffect();

        // TODO: Всплывающие цифры урона (DamagePopup)
    }

    /// <summary>
    /// Создать эффект попадания (взрыв)
    /// </summary>
    private void SpawnHitEffect()
    {
        // Загружаем префаб эффекта попадания
        GameObject hitEffectPrefab = Resources.Load<GameObject>("Effects/HitEffect");

        if (hitEffectPrefab == null)
        {
            Debug.LogWarning("[NetworkPlayer] ⚠️ HitEffect префаб не найден в Resources/Effects/");
            return;
        }

        // Позиция эффекта = центр персонажа (торс)
        Vector3 hitPosition = transform.position + Vector3.up * 1.0f;

        // Создаём эффект
        GameObject hitEffectObj = Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);

        Debug.Log($"[NetworkPlayer] 💥 Эффект попадания создан для {username} в позиции {hitPosition}");

        // Автоуничтожение через 2 секунды
        Destroy(hitEffectObj, 2.0f);
    }

    /// <summary>
    /// Мигание красным при получении урона
    /// ИСПРАВЛЕНО: Сохраняем ссылку на material instance чтобы избежать утечки памяти
    /// </summary>
    private System.Collections.IEnumerator FlashRed()
    {
        // КРИТИЧЕСКОЕ: Находим ВСЕ SkinnedMeshRenderer (может быть тело + одежда + оружие)
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning($"[NetworkPlayer] ⚠️ SkinnedMeshRenderer не найден для {username}!");
            yield break;
        }

        Debug.Log($"[NetworkPlayer] 💥 FlashRed для {username}: найдено {renderers.Length} mesh'ей");

        // Сохраняем оригинальные цвета ВСЕХ mesh'ей
        Material[] materialInstances = new Material[renderers.Length];
        Color[] originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            materialInstances[i] = renderers[i].material; // Создаём instance один раз
            originalColors[i] = materialInstances[i].color;

            // Красим в красный
            materialInstances[i].color = Color.red;
        }

        yield return new WaitForSeconds(0.1f);

        // Возвращаем оригинальные цвета
        for (int i = 0; i < materialInstances.Length; i++)
        {
            if (materialInstances[i] != null)
            {
                materialInstances[i].color = originalColors[i];
            }
        }

        Debug.Log($"[NetworkPlayer] ✅ FlashRed завершён для {username}");
    }

    // УДАЛЕНО: GetNameplate(), ShowNameplate(), HideNameplate(), OnDestroy() - заменено на EnemyNameplate.cs

    // ===== ТРАНСФОРМАЦИЯ (НОВОЕ) =====

    private GameObject transformationInstance; // Модель трансформации (медведь и т.д.)
    private GameObject originalModel; // Оригинальная модель персонажа

    /// <summary>
    /// Применить трансформацию к сетевому игроку (НОВОЕ)
    /// </summary>
    public void ApplyTransformation(int skillId)
    {
        Debug.Log($"[NetworkPlayer] 🐻 ApplyTransformation вызван для {username}, skillId={skillId}");

        // Получаем скилл из SkillDatabase
        SkillDatabase db = SkillDatabase.Instance;
        if (db == null)
        {
            Debug.LogError("[NetworkPlayer] ❌ SkillDatabase.Instance == null!");
            return;
        }

        SkillData skill = db.GetSkillById(skillId);
        if (skill == null)
        {
            Debug.LogError($"[NetworkPlayer] ❌ Скилл с ID {skillId} не найден!");
            return;
        }

        if (skill.transformationModel == null)
        {
            Debug.LogError($"[NetworkPlayer] ❌ У скилла {skill.skillName} нет модели трансформации!");
            return;
        }

        Debug.Log($"[NetworkPlayer] 🔍 Скилл найден: {skill.skillName}, модель: {skill.transformationModel.name}");

        // Скрываем оригинальную модель
        originalModel = GetComponentInChildren<SkinnedMeshRenderer>()?.gameObject;
        if (originalModel != null)
        {
            originalModel.SetActive(false);
            Debug.Log($"[NetworkPlayer] ✅ Оригинальная модель скрыта: {originalModel.name}");
        }
        else
        {
            Debug.LogWarning($"[NetworkPlayer] ⚠️ SkinnedMeshRenderer не найден для {username}");
        }

        // Создаём модель трансформации
        transformationInstance = Instantiate(skill.transformationModel, transform.position, transform.rotation, transform);
        Debug.Log($"[NetworkPlayer] ✅ Трансформация создана: {transformationInstance.name} для {username}");

        // ВАЖНО: Обновляем ссылку на Animator (теперь используем animator из модели трансформации)
        Animator newAnimator = transformationInstance.GetComponentInChildren<Animator>();
        if (newAnimator != null)
        {
            animator = newAnimator;
            Debug.Log($"[NetworkPlayer] ✅ Animator обновлён на трансформацию для {username}");

            // Устанавливаем боевую стойку
            if (HasAnimatorParameter(animator, "InBattle"))
            {
                animator.SetBool("InBattle", true);
            }
        }
        else
        {
            Debug.LogWarning($"[NetworkPlayer] ⚠️ Animator не найден на модели трансформации для {username}");
        }

        Debug.Log($"[NetworkPlayer] 🐻 ✅ Трансформация применена к {username}!");
    }

    /// <summary>
    /// Завершить трансформацию сетевого игрока (НОВОЕ)
    /// </summary>
    public void EndTransformation()
    {
        Debug.Log($"[NetworkPlayer] 🔄 EndTransformation вызван для {username}");

        if (transformationInstance != null)
        {
            Destroy(transformationInstance);
            Debug.Log($"[NetworkPlayer] ✅ Модель трансформации удалена для {username}");
        }

        if (originalModel != null)
        {
            originalModel.SetActive(true);
            Debug.Log($"[NetworkPlayer] ✅ Оригинальная модель восстановлена для {username}");

            // ВАЖНО: Возвращаем ссылку на оригинальный Animator
            Animator originalAnimator = originalModel.GetComponentInChildren<Animator>();
            if (originalAnimator != null)
            {
                animator = originalAnimator;
                Debug.Log($"[NetworkPlayer] ✅ Animator восстановлён на оригинальный для {username}");

                // Устанавливаем боевую стойку
                if (HasAnimatorParameter(animator, "InBattle"))
                {
                    animator.SetBool("InBattle", true);
                }
            }
        }

        transformationInstance = null;
        originalModel = null;

        Debug.Log($"[NetworkPlayer] 🔄 ✅ Трансформация завершена для {username}!");
    }

    // Public getters
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public int CurrentMP => currentMP;
    public int MaxMP => maxMP;
    public bool IsAlive => currentHP > 0;
}
