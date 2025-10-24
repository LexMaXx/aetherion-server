using UnityEngine;

/// <summary>
/// Система атаки персонажа - клик мышкой для атаки
/// Блокирует движение пока проигрывается анимация атаки
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Header("⚔️ BASIC ATTACK CONFIG")]
    [Tooltip("Конфигурация базовой атаки (ScriptableObject). Если назначена - приоритет над ручными настройками.")]
    [SerializeField] private BasicAttackConfig attackConfig;

    [Header("Attack Settings (Legacy - используется если attackConfig = null)")]
    [SerializeField] private float attackCooldown = 1.0f; // Кулдаун между атаками
    [SerializeField] private float attackRange = 3.0f; // Дальность атаки
    [SerializeField] private float attackDamage = 25f; // Урон атаки
    [SerializeField] private float optimalAttackDistance = 1.8f; // Оптимальная дистанция для удара
    [SerializeField] private float attackRotationOffset = 45f; // Поворот во время атаки (градусы)

    // Public properties для NetworkCombatSync
    public float BaseDamage => GetBaseDamage();
    public bool IsRangedAttack => GetIsRangedAttack();

    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab; // Префаб снаряда (стрела, шар, осколки)
    [SerializeField] private bool isRangedAttack = false; // Дальняя атака? (лучник, маг, разбойник)
    [SerializeField] private float projectileSpeed = 20f; // Скорость снаряда
    [SerializeField] private Transform weaponTipTransform; // Точка спавна снарядов (кончик оружия)
    [SerializeField] private float manaCostPerShot = 10f; // Стоимость маны за выстрел (для магов и разбойников)

    [Header("Animation Settings")]
    [SerializeField] private float attackAnimationSpeed = 1.0f; // Скорость анимации атаки (1.0 = нормально, 3.0 = в 3 раза быстрее)
    [SerializeField][Range(0f, 1f)] private float attackHitTiming = 0.8f; // Момент выстрела/удара (0.0 = начало, 0.5 = середина, 1.0 = конец)

    private Animator animator;
    private CharacterController characterController;
    private MixamoPlayerController playerController;
    private TargetSystem targetSystem;
    private ActionPointsSystem actionPointsSystem;
    private ClassWeaponManager weaponManager;
    private CharacterStats characterStats; // Интеграция с SPECIAL (Strength/Intelligence/Luck)
    private ManaSystem manaSystem; // Система маны для дальних атак
    private SkillManager skillManager; // Менеджер скиллов
    private float lastAttackTime = 0f;
    private bool isAttacking = false;
    private Vector3 positionBeforeAttack;
    private Quaternion rotationBeforeAttack;

    // Система использования скиллов
    private int selectedSkillIndex = -1; // -1 = не выбран, 0-2 = индекс скилла

    // Текущая цель атаки (для Animation Event)
    private Enemy currentAttackTarget = null;

    // Rotation lock для удержания поворота к врагу
    private Quaternion lockedRotation;
    private bool isRotationLocked = false;

    // Хеш состояния атаки в аниматоре
    private int attackStateHash;
    private const string ATTACK_STATE_NAME = "WarriorAttack"; // Имя состояния атаки

    // ============================================================
    // HELPER METHODS: Получение значений из BasicAttackConfig или legacy полей
    // ============================================================

    private float GetBaseDamage()
    {
        return attackConfig != null ? attackConfig.baseDamage : attackDamage;
    }

    private bool GetIsRangedAttack()
    {
        return attackConfig != null ? (attackConfig.attackType == AttackType.Ranged) : isRangedAttack;
    }

    private float GetAttackRange()
    {
        return attackConfig != null ? attackConfig.attackRange : attackRange;
    }

    private float GetAttackCooldown()
    {
        return attackConfig != null ? attackConfig.attackCooldown : attackCooldown;
    }

    private GameObject GetProjectilePrefab()
    {
        return attackConfig != null ? attackConfig.projectilePrefab : projectilePrefab;
    }

    private float GetProjectileSpeed()
    {
        return attackConfig != null ? attackConfig.projectileSpeed : projectileSpeed;
    }

    private string GetAnimationTrigger()
    {
        return attackConfig != null ? attackConfig.animationTrigger : "Attack";
    }

    private float GetAnimationSpeed()
    {
        return attackConfig != null ? attackConfig.animationSpeed : attackAnimationSpeed;
    }

    private float GetManaCostPerAttack()
    {
        return attackConfig != null ? attackConfig.manaCostPerAttack : manaCostPerShot;
    }

    void Start()
    {
        Debug.Log($"[PlayerAttack] ========== START вызван для {gameObject.name} ==========");

        // ПРИОРИТЕТ: BasicAttackConfig
        if (attackConfig != null)
        {
            Debug.Log($"[PlayerAttack] ✅ Используется BasicAttackConfig: {attackConfig.name}");
            Debug.Log($"[PlayerAttack] 📊 Config: Damage={attackConfig.baseDamage}, Range={attackConfig.attackRange}m, Type={attackConfig.attackType}");

            // Валидация конфига
            string validationError;
            if (!attackConfig.Validate(out validationError))
            {
                Debug.LogError($"[PlayerAttack] ❌ ОШИБКА ВАЛИДАЦИИ BasicAttackConfig: {validationError}");
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerAttack] ⚠️ BasicAttackConfig НЕ НАЗНАЧЕН! Используются legacy настройки.");
        }

        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        playerController = GetComponent<MixamoPlayerController>();
        targetSystem = GetComponent<TargetSystem>();
        actionPointsSystem = GetComponent<ActionPointsSystem>();
        weaponManager = GetComponent<ClassWeaponManager>();
        characterStats = GetComponent<CharacterStats>();
        manaSystem = GetComponent<ManaSystem>();
        skillManager = GetComponent<SkillManager>();

        if (characterStats != null)
        {
            Debug.Log($"[PlayerAttack] ✅ Интеграция с CharacterStats активирована (Класс: {characterStats.ClassName})");

            // Проверяем тип атаки
            if (GetIsRangedAttack())
            {
                bool isMagicalAttack = (characterStats.ClassName == "Mage" || characterStats.ClassName == "Rogue");
                if (isMagicalAttack && manaSystem != null)
                {
                    string classType = characterStats.ClassName == "Mage" ? "Маг" : "Некромант (Rogue)";
                    Debug.Log($"[PlayerAttack] 🔮 МАГИЧЕСКАЯ дальняя атака ({classType}) - тратит {GetManaCostPerAttack()} маны за выстрел");
                }
                else
                {
                    Debug.Log($"[PlayerAttack] 🏹 ФИЗИЧЕСКАЯ дальняя атака (Лучник) - НЕ тратит ману (только стрелы)");
                }
            }
            else
            {
                Debug.Log($"[PlayerAttack] ⚔️ БЛИЖНЯЯ атака - НЕ тратит ману");
            }
        }

        if (animator == null)
        {
            Debug.LogError("[PlayerAttack] Animator не найден!");
            enabled = false;
            return;
        }

        if (targetSystem == null)
        {
            Debug.LogWarning("[PlayerAttack] TargetSystem не найден! Добавьте компонент TargetSystem.");
        }

        if (actionPointsSystem == null)
        {
            Debug.LogWarning("[PlayerAttack] ActionPointsSystem не найден! Атаки будут без ограничений AP.");
        }

        if (weaponManager == null)
        {
            Debug.LogWarning("[PlayerAttack] ClassWeaponManager не найден! Эффект свечения оружия не будет работать.");
        }

        // Определяем хеш состояния атаки для оптимизации
        attackStateHash = Animator.StringToHash("Base Layer.WarriorAttack");

        // АВТОНАСТРОЙКА дистанций для разных классов
        Debug.Log($"[PlayerAttack] Вызываем ConfigureClassSettings()...");
        ConfigureClassSettings();

        // АВТОПОИСК точки оружия для спавна снарядов
        Debug.Log($"[PlayerAttack] Вызываем FindWeaponTip()...");
        FindWeaponTip();

        // ПРИМЕНЯЕМ скорость анимации к Animator
        Debug.Log($"[PlayerAttack] Вызываем ApplyAnimationSpeed()...");
        ApplyAnimationSpeed();

        Debug.Log($"[PlayerAttack] ========== START завершён для {gameObject.name} ==========");
    }

    /// <summary>
    /// Автоматическая настройка параметров атаки для разных классов
    /// </summary>
    private void ConfigureClassSettings()
    {
        Debug.Log($"[PlayerAttack] ConfigureClassSettings вызван для {gameObject.name}");

        // Пытаемся загрузить настройки из ScriptableObject
        CharacterAttackSettings settingsAsset = Resources.Load<CharacterAttackSettings>("CharacterAttackSettings");

        if (settingsAsset == null)
        {
            Debug.LogWarning($"[PlayerAttack] ⚠️ CharacterAttackSettings.asset НЕ НАЙДЕН в Resources!");
            Debug.LogWarning($"[PlayerAttack] Создайте его: Tools → Create Attack Settings Asset");
            Debug.LogWarning($"[PlayerAttack] Переместите в Assets/Resources/CharacterAttackSettings.asset");
        }
        else
        {
            Debug.Log($"[PlayerAttack] ✅ CharacterAttackSettings.asset загружен");

            // Определяем класс персонажа
            string className = PlayerPrefs.GetString("SelectedCharacterClass", "");
            Debug.Log($"[PlayerAttack] PlayerPrefs SelectedCharacterClass = '{className}'");

            if (!string.IsNullOrEmpty(className))
            {
                ClassAttackConfig config = settingsAsset.GetConfigForClass(className);
                Debug.Log($"[PlayerAttack] 🔍 Config из asset: Speed={config.attackAnimationSpeed}, Timing={config.attackHitTiming}, Damage={config.attackDamage}");
                ApplyConfig(config);
                Debug.Log($"[PlayerAttack] ✅ Загружены настройки из ScriptableObject для {className}");
                return;
            }
            else
            {
                Debug.LogWarning($"[PlayerAttack] ⚠️ SelectedCharacterClass пуст в PlayerPrefs!");
            }
        }

        // Fallback: настройки по имени объекта (если ScriptableObject не найден)
        Debug.Log($"[PlayerAttack] Используем fallback настройки по имени объекта");
        string objectName = gameObject.name.ToLower();

        if (objectName.Contains("warrior"))
        {
            // Воин: ближний бой, меч
            attackRange = 3.0f;
            optimalAttackDistance = 1.8f;
            attackRotationOffset = 45f;
            attackDamage = 30f;
            Debug.Log("[PlayerAttack] ⚔️ Warrior: Range=3m, Damage=30");
        }
        else if (objectName.Contains("archer"))
        {
            // Лучник: дальний бой, лук
            attackRange = 50f;
            optimalAttackDistance = 10f;
            attackRotationOffset = 0f;
            attackDamage = 35f;
            isRangedAttack = true;
            projectileSpeed = 30f;
            LoadProjectilePrefab("ArrowProjectile");
            Debug.Log("[PlayerAttack] 🏹 Archer: Range=50m, Damage=35");
        }
        else if (objectName.Contains("mage"))
        {
            // Маг: средняя дистанция, магия
            attackRange = 20f;
            optimalAttackDistance = 5f;
            attackRotationOffset = 0f;
            attackDamage = 40f;
            isRangedAttack = true;
            projectileSpeed = 20f;
            attackAnimationSpeed = 3.0f; // Анимация атаки в 3 раза быстрее!
            LoadProjectilePrefab("CelestialBallProjectile");
            Debug.Log("[PlayerAttack] 🔮 Mage: Range=20m, Damage=40, AnimSpeed=3x");
        }
        else if (objectName.Contains("rogue"))
        {
            // Разбойник: средняя дистанция, похищение души
            attackRange = 20f;
            optimalAttackDistance = 5f;
            attackRotationOffset = 0f; // ИСПРАВЛЕНО: было -30f, теперь 0f (смотрит прямо на цель)
            attackDamage = 50f; // Высокий урон за похищение души
            isRangedAttack = true;
            projectileSpeed = 15f;
            LoadProjectilePrefab("SoulShardsProjectile");
            Debug.Log("[PlayerAttack] 💀 Rogue: Range=20m, Damage=50");
        }
        else if (objectName.Contains("paladin"))
        {
            // Паладин: ближний бой, меч
            attackRange = 3.0f;
            optimalAttackDistance = 1.8f;
            attackRotationOffset = 45f;
            attackDamage = 25f;
            Debug.Log("[PlayerAttack] 🛡️ Paladin: Range=3m, Damage=25");
        }
    }

    /// <summary>
    /// Применить конфигурацию из ScriptableObject
    /// </summary>
    private void ApplyConfig(ClassAttackConfig config)
    {
        attackAnimationSpeed = config.attackAnimationSpeed;
        attackHitTiming = config.attackHitTiming;
        attackDamage = config.attackDamage;
        attackRange = config.attackRange;
        attackCooldown = config.attackCooldown;
        isRangedAttack = config.isRangedAttack;
        projectileSpeed = config.projectileSpeed;
        attackRotationOffset = config.attackRotationOffset;

        if (isRangedAttack && !string.IsNullOrEmpty(config.projectilePrefabName))
        {
            LoadProjectilePrefab(config.projectilePrefabName);
        }

        Debug.Log($"[PlayerAttack] Config applied: Speed={attackAnimationSpeed}x, Damage={attackDamage}, Range={attackRange}");
    }

    /// <summary>
    /// Загрузить префаб снаряда из Resources или Assets
    /// </summary>
    private void LoadProjectilePrefab(string prefabName)
    {
        // Сначала пытаемся загрузить из Resources
        projectilePrefab = Resources.Load<GameObject>($"Projectiles/{prefabName}");

        if (projectilePrefab == null)
        {
            // Если не нашли в Resources, пытаемся найти в Assets через AssetDatabase (только в Editor)
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{prefabName} t:Prefab");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                projectilePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Debug.Log($"[PlayerAttack] ✅ Префаб загружен из Assets: {path}");
            }
            else
            {
                Debug.LogWarning($"[PlayerAttack] ⚠️ Префаб {prefabName} не найден! Создайте префабы через Tools → Projectiles");
            }
#else
            Debug.LogWarning($"[PlayerAttack] ⚠️ Префаб {prefabName} не найден в Resources/Projectiles/");
#endif
        }
        else
        {
            Debug.Log($"[PlayerAttack] ✅ Префаб загружен из Resources: {prefabName}");
        }
    }

    /// <summary>
    /// Найти точку оружия (кончик лука/посоха) для спавна снарядов
    /// </summary>
    private void FindWeaponTip()
    {
        // Если уже назначена вручную - пропускаем
        if (weaponTipTransform != null)
        {
            Debug.Log($"[PlayerAttack] ✅ Точка оружия уже назначена: {weaponTipTransform.name}");
            return;
        }

        // Пытаемся найти в иерархии по имени
        // Общие имена для точек оружия в Mixamo моделях:
        string[] weaponTipNames = new string[]
        {
            "WeaponTip",           // Если создали вручную
            "Weapon_Tip",
            "RightHandIndex3",     // Кончик указательного пальца правой руки
            "mixamorig:RightHandIndex3",
            "RightHandIndex4",     // Самый кончик пальца
            "mixamorig:RightHandIndex4",
            "RightHand",           // Правая рука (запасной вариант)
            "mixamorig:RightHand"
        };

        Transform[] allTransforms = GetComponentsInChildren<Transform>();
        foreach (string tipName in weaponTipNames)
        {
            foreach (Transform t in allTransforms)
            {
                if (t.name.Contains(tipName))
                {
                    weaponTipTransform = t;
                    Debug.Log($"[PlayerAttack] ✅ Автопоиск: Найдена точка оружия '{t.name}'");
                    return;
                }
            }
        }

        // Если не нашли - используем позицию персонажа + смещение вперёд
        Debug.LogWarning("[PlayerAttack] ⚠️ Точка оружия не найдена! Снаряды будут лететь от персонажа. Создайте GameObject 'WeaponTip' на кончике оружия.");
    }

    /// <summary>
    /// Применить скорость анимации к Animator
    /// </summary>
    private void ApplyAnimationSpeed()
    {
        if (animator == null)
        {
            Debug.LogError($"[PlayerAttack] ❌ Animator is NULL for {gameObject.name}!");
            return;
        }

        Debug.Log($"[PlayerAttack] 🎯 ApplyAnimationSpeed called for {gameObject.name}: speed={attackAnimationSpeed}x");

        // РЕШЕНИЕ 1: Используем animator.speed напрямую (ПРОСТОЕ, РАБОТАЕТ ВСЕГДА)
        // Влияет на все анимации, но для атаки это приемлемо
        animator.speed = attackAnimationSpeed;
        Debug.Log($"✅ [PlayerAttack] Animator.speed установлен: {animator.speed}x");

        // РЕШЕНИЕ 2: Дополнительно проверяем параметр AttackSpeed
        if (animator.parameters != null)
        {
            bool hasAttackSpeedParam = false;
            foreach (var param in animator.parameters)
            {
                if (param.name == "AttackSpeed")
                {
                    hasAttackSpeedParam = true;
                    animator.SetFloat("AttackSpeed", attackAnimationSpeed);
                    Debug.Log($"✅ [PlayerAttack] Параметр AttackSpeed также установлен: {attackAnimationSpeed}x");
                    break;
                }
            }

            if (!hasAttackSpeedParam)
            {
                Debug.LogWarning($"⚠ [PlayerAttack] Параметр 'AttackSpeed' НЕ найден в Animator для {gameObject.name}");
                Debug.LogWarning("   Используется animator.speed вместо этого");
            }
        }
    }


    void Update()
    {
        // БЛОКИРОВКА ROTATION: принудительно удерживаем поворот к врагу
        if (isRotationLocked)
        {
            transform.rotation = lockedRotation;
        }

        // Проверяем состояние анимации атаки
        CheckAttackState();

        // === СИСТЕМА УПРАВЛЕНИЯ СКИЛЛАМИ ===

        // Клавиши 1/2/3/4/5 - ПРЯМОЕ использование скиллов (БЕЗ ПКМ)
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            UseSkillDirectly(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            UseSkillDirectly(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            UseSkillDirectly(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            UseSkillDirectly(3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
        {
            UseSkillDirectly(4);
        }

        // ЛКМ - обычная атака
        if (Input.GetMouseButtonDown(0))
        {
            TryAttack();
        }
    }

    /// <summary>
    /// Проверить состояние анимации атаки
    /// </summary>
    private void CheckAttackState()
    {
        if (animator == null)
            return;

        // Если не атакуем - ничего не проверяем
        if (!isAttacking)
            return;

        // Проверяем Base Layer (0)
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // Проверяем несколько вариантов состояния атаки
        bool isPlayingAttack = stateInfo.IsTag("Attack") ||
                              stateInfo.IsName("Attack") ||
                              stateInfo.IsName("WarriorAttack") ||
                              stateInfo.IsName("MageAttack") ||
                              stateInfo.IsName("ArcherAttack") ||
                              stateInfo.IsName("RogueAttack");

        // ФИКСИРОВАННАЯ ЗАДЕРЖКА: 1 секунда для гарантии проигрывания анимации
        float timeSinceAttack = Time.time - lastAttackTime;
        float minAttackDuration = 1.0f; // ВСЕГДА 1 секунда, независимо от скорости

        // Прошла 1 секунда с начала атаки?
        if (timeSinceAttack >= minAttackDuration)
        {
            isAttacking = false;

            // ВКЛЮЧАЕМ ОБРАТНО CharacterController - позволяем движение
            if (characterController != null && !characterController.enabled)
            {
                characterController.enabled = true;
                Debug.Log("[PlayerAttack] ✅ CharacterController ВКЛЮЧЕН - можно двигаться");
            }

            // РАЗБЛОКИРУЕМ rotation - можно поворачиваться
            isRotationLocked = false;
            Debug.Log("[PlayerAttack] 🔓 Rotation разблокирован");

            // ОТКЛЮЧАЕМ ЭФФЕКТ СВЕЧЕНИЯ (аура исчезает)
            if (weaponManager != null)
            {
                weaponManager.DeactivateWeaponGlow();
            }

            Debug.Log($"[PlayerAttack] ⏱️ Атака завершена после {timeSinceAttack:F2}s (минимум {minAttackDuration}s) - движение разблокировано");
        }
    }

    /// <summary>
    /// Попытка атаковать
    /// </summary>
    private void TryAttack()
    {
        // ПРОВЕРЯЕМ наличие цели
        if (targetSystem == null || !targetSystem.HasTarget())
        {
            Debug.Log("[PlayerAttack] Нет цели для атаки");
            return;
        }

        Enemy target = targetSystem.GetCurrentTarget();
        if (target == null || !target.IsAlive())
        {
            Debug.Log("[PlayerAttack] Цель мертва или недоступна");
            return;
        }

        // ПРОВЕРЯЕМ дистанцию до цели
        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        float currentAttackRange = GetAttackRange();
        if (distanceToTarget > currentAttackRange)
        {
            Debug.Log($"[PlayerAttack] Цель слишком далеко: {distanceToTarget:F1}m (макс: {currentAttackRange}m)");
            return;
        }

        // ПРОВЕРЯЕМ наличие очков действия
        if (actionPointsSystem != null && !actionPointsSystem.HasEnoughPointsForAttack())
        {
            Debug.Log($"[PlayerAttack] ❌ Недостаточно очков действия для атаки! Нужно {actionPointsSystem.GetAttackCost()}, доступно {actionPointsSystem.GetCurrentPoints()}");
            return;
        }

        // ПРОВЕРЯЕМ наличие маны для МАГИЧЕСКИХ дальних атак (Mage и Rogue/Некромант)
        // Лучник стреляет физическими стрелами без затрат маны
        bool isMagicalAttack = GetIsRangedAttack() && characterStats != null &&
                               (characterStats.ClassName == "Mage" || characterStats.ClassName == "Rogue");
        float manaCost = GetManaCostPerAttack();
        if (isMagicalAttack && manaSystem != null)
        {
            if (manaSystem.CurrentMana < manaCost)
            {
                Debug.Log($"[PlayerAttack] ❌ Недостаточно маны для магической атаки! Нужно {manaCost}, доступно {manaSystem.CurrentMana:F0}");
                return;
            }
        }

        // Проверяем кулдаун
        float currentCooldown = GetAttackCooldown();
        if (Time.time - lastAttackTime < currentCooldown)
        {
            Debug.Log("[PlayerAttack] Кулдаун атаки еще не закончился");
            return;
        }

        // Проверяем что не атакуем сейчас
        if (isAttacking)
        {
            Debug.Log("[PlayerAttack] Уже атакуем");
            return;
        }

        // ТРАТИМ очки действия перед атакой
        if (actionPointsSystem != null)
        {
            if (!actionPointsSystem.TrySpendActionPoints(actionPointsSystem.GetAttackCost()))
            {
                Debug.LogWarning("[PlayerAttack] Не удалось потратить очки действия!");
                return;
            }
        }

        // ТРАТИМ ману для МАГИЧЕСКИХ дальних атак (используем уже вычисленное значение isMagicalAttack и manaCost)
        if (isMagicalAttack && manaSystem != null)
        {
            if (!manaSystem.SpendMana(manaCost))
            {
                Debug.LogWarning("[PlayerAttack] Не удалось потратить ману!");
                return;
            }
            Debug.Log($"[PlayerAttack] 💧 Потрачено {manaCost} маны. Осталось: {manaSystem.CurrentMana:F0}/{manaSystem.MaxMana:F0}");
        }

        // Выполняем атаку
        PerformAttack(target);
    }

    /// <summary>
    /// Выполнить атаку
    /// </summary>
    private void PerformAttack(Enemy target)
    {
        // 0. АКТИВИРУЕМ ЭФФЕКТ СВЕЧЕНИЯ ОРУЖИЯ (Lineage 2 style - синяя аура)
        if (weaponManager != null)
        {
            weaponManager.ActivateWeaponGlow();
        }

        // 1. ПОВОРАЧИВАЕМ персонажа к цели (СНАЧАЛА поворот!)
        Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
        directionToTarget.y = 0; // Игнорируем вертикальную составляющую

        if (directionToTarget.magnitude > 0.01f) // Проверка на нулевой вектор
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            // ДОБАВЛЯЕМ поворот для компенсации анимации (если атака бьет влево/вправо)
            targetRotation *= Quaternion.Euler(0, attackRotationOffset, 0);

            transform.rotation = targetRotation;

            // БЛОКИРУЕМ rotation на 1 секунду
            lockedRotation = targetRotation;
            isRotationLocked = true;

            Debug.Log($"[PlayerAttack] 🔒 Персонаж повернут к цели: {target.GetEnemyName()} (смещение {attackRotationOffset}°) - rotation заблокирован");
        }

        // 2. ОТКЛЮЧАЕМ CharacterController чтобы полностью остановить движение
        if (characterController != null)
        {
            characterController.enabled = false;
            Debug.Log("[PlayerAttack] CharacterController ВЫКЛЮЧЕН - персонаж полностью остановлен");
        }

        // 3. СОХРАНЯЕМ цель для атаки
        currentAttackTarget = target;
        Debug.Log($"[PlayerAttack] Атака на {target.GetEnemyName()}! Персонаж остановлен.");

        // 4. Запускаем анимацию атаки
        // Если трансформирован (медведь) - используем SimpleTransformation
        SimpleTransformation transformation = GetComponent<SimpleTransformation>();
        if (transformation != null && transformation.IsTransformed())
        {
            transformation.SetAnimatorTrigger("Attack");
            Debug.Log($"[PlayerAttack] ⚡ Анимация атаки запущена на медведя через SimpleTransformation");
        }
        else if (animator != null)
        {
            animator.SetTrigger("Attack");
            // animator.speed уже установлен в ApplyAnimationSpeed() в Start()
            Debug.Log($"[PlayerAttack] Анимация атаки запущена (animator.speed = {animator.speed}x)");
        }

        // 5. Запускаем автоматическое отслеживание конца анимации
        StartCoroutine(WaitForAttackAnimationEnd());

        // 6. Обновляем время последней атаки
        lastAttackTime = Time.time;

        // 7. Флаг что атакуем (будет сброшен автоматически через CheckAttackState)
        isAttacking = true;
    }

    /// <summary>
    /// Корутина: ждёт 1 секунду и наносит урон/создаёт снаряд
    /// </summary>
    private System.Collections.IEnumerator WaitForAttackAnimationEnd()
    {
        // ПРОСТОЕ РЕШЕНИЕ: Ждём ровно 1 секунду
        float waitTime = 1.0f;

        Debug.Log($"[PlayerAttack] ⏱️ Ждём {waitTime}s до создания снаряда/урона...");

        yield return new WaitForSeconds(waitTime);

        Debug.Log($"[PlayerAttack] ✅ Прошла {waitTime}s - создаём снаряд/урон");

        // Наносим урон или создаём снаряд
        PerformDamageOrSpawnProjectile();
    }

    /// <summary>
    /// Нанести урон или создать снаряд (вызывается автоматически в конце анимации)
    /// </summary>
    private void PerformDamageOrSpawnProjectile()
    {
        // Проверяем что цель ещё существует и жива
        if (currentAttackTarget == null || !currentAttackTarget.IsAlive())
        {
            Debug.LogWarning("[PlayerAttack] ⚠️ Цель умерла или исчезла до момента удара!");
            currentAttackTarget = null;
            return;
        }

        // Рассчитываем финальный урон с учетом характеристик (Strength/Intelligence/Luck)
        float finalDamage = CalculateFinalDamage();

        // Дальняя атака - создаём снаряд
        GameObject currentProjectilePrefab = GetProjectilePrefab();
        if (GetIsRangedAttack() && currentProjectilePrefab != null)
        {
            SpawnProjectile(currentAttackTarget, finalDamage);
            Debug.Log($"[PlayerAttack] 🎯 Снаряд создан для {currentAttackTarget.GetEnemyName()}");
        }
        // Ближняя атака - наносим урон сразу
        else
        {
            // ВАЖНО: В мультиплеере НЕ наносим урон NetworkPlayer локально!
            // Сервер рассчитает урон и отправит событие enemy_damaged_by_server
            NetworkPlayer networkTarget = currentAttackTarget.GetComponent<NetworkPlayer>();
            if (networkTarget == null)
            {
                // Это обычный NPC враг - наносим урон локально
                currentAttackTarget.TakeDamage(finalDamage);
                Debug.Log($"[PlayerAttack] ⚔️ Урон {finalDamage:F0} нанесён NPC {currentAttackTarget.GetEnemyName()}");
            }
            else
            {
                // Это NetworkPlayer - урон нанесёт сервер
                Debug.Log($"[PlayerAttack] 🌐 Атака на NetworkPlayer {networkTarget.username} - ждём ответа сервера");
            }
        }

        // МУЛЬТИПЛЕЕР: Отправляем информацию об атаке на сервер
        NetworkCombatSync combatSync = GetComponent<NetworkCombatSync>();
        if (combatSync != null)
        {
            string attackType = GetIsRangedAttack() ? "ranged" : "melee";
            combatSync.SendAttack(currentAttackTarget.gameObject, finalDamage, attackType);
        }

        // Очищаем цель после использования
        currentAttackTarget = null;
    }

    /// <summary>
    /// Расчет финального урона с учетом SPECIAL характеристик
    /// </summary>
    private float CalculateFinalDamage()
    {
        // ПРИОРИТЕТ: используем BasicAttackConfig для расчета урона
        float baseDamage = GetBaseDamage();
        float finalDamage = baseDamage;

        if (characterStats != null)
        {
            // Если используется BasicAttackConfig - используем его метод расчета
            if (attackConfig != null)
            {
                finalDamage = attackConfig.CalculateDamage(characterStats);
                Debug.Log($"[PlayerAttack] 💥 Урон рассчитан через BasicAttackConfig: {finalDamage:F0}");
            }
            // Иначе legacy расчет
            else
            {
                // Физический урон (ближний бой) - Strength
                if (!GetIsRangedAttack())
                {
                    finalDamage = characterStats.CalculatePhysicalDamage(baseDamage);
                }
                // Магический урон (дальний бой) - Intelligence
                else
                {
                    finalDamage = characterStats.CalculateMagicalDamage(baseDamage);
                }
            }

            // Проверка критического удара (Luck)
            bool isCrit = characterStats.RollCriticalHit();
            if (isCrit)
            {
                finalDamage = characterStats.ApplyCriticalDamage(finalDamage);
                Debug.Log($"[PlayerAttack] ★ КРИТИЧЕСКИЙ УДАР! Урон x{characterStats.Formulas.critDamageMultiplier}");
            }
        }

        return finalDamage;
    }

    /// <summary>
    /// Создать снаряд (стрела, огненный шар, осколки души, celestial ball)
    /// Теперь вызывается из Animation Event OnAttackHit()
    /// </summary>
    private void SpawnProjectile(Enemy target, float damage)
    {
        // Точка спавна снаряда
        Vector3 spawnPosition;

        if (weaponTipTransform != null)
        {
            // ВАРИАНТ 1: Если есть точка оружия - используем её
            spawnPosition = weaponTipTransform.position;
            Debug.Log($"[PlayerAttack] 🎯 Спавн от оружия: {weaponTipTransform.name} at {spawnPosition}");
        }
        else
        {
            // ВАРИАНТ 2: Если нет точки оружия - используем позицию персонажа + смещение
            spawnPosition = transform.position + transform.forward * 0.5f + Vector3.up * 1.2f;
            Debug.Log($"[PlayerAttack] 🎯 Спавн от персонажа (нет WeaponTip): {spawnPosition}");
        }

        // Направление к цели (к центру врага)
        Vector3 targetPosition = target.transform.position + Vector3.up * 1.0f; // Центр врага
        Vector3 direction = (targetPosition - spawnPosition).normalized;

        // Создаем снаряд
        GameObject prefabToSpawn = GetProjectilePrefab();
        GameObject projectileObj = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

        // Проверяем тип снаряда и инициализируем соответствующий компонент
        // Проверяем CelestialProjectile, ArrowProjectile, затем старый Projectile
        CelestialProjectile celestialProjectile = projectileObj.GetComponent<CelestialProjectile>();
        ArrowProjectile arrowProjectile = projectileObj.GetComponent<ArrowProjectile>();

        if (celestialProjectile != null)
        {
            // Мага - Celestial Ball с автонаведением
            celestialProjectile.Initialize(target.transform, damage, direction, this.gameObject);
            Debug.Log($"[PlayerAttack] ✨ CelestialProjectile создан: {projectilePrefab.name} → {target.GetEnemyName()} (Урон: {damage:F0}, Homing: ON)");
        }
        else if (arrowProjectile != null)
        {
            // Лучник - Arrow с автонаведением
            arrowProjectile.Initialize(target.transform, damage, direction, this.gameObject);
            Debug.Log($"[PlayerAttack] 🏹 ArrowProjectile создан: {projectilePrefab.name} → {target.GetEnemyName()} (Урон: {damage:F0}, Homing: ON)");
        }
        else
        {
            // Старый базовый снаряд (Projectile)
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                // ВАЖНО: Передаём this.gameObject как owner чтобы снаряд не попадал в своего владельца
                projectile.Initialize(target.transform, damage, direction, this.gameObject);
                Debug.Log($"[PlayerAttack] ✅ Projectile создан: {projectilePrefab.name} → {target.GetEnemyName()} (Урон: {damage:F0})");
            }
            else
            {
                Debug.LogError("[PlayerAttack] ❌ У префаба снаряда нет компонента Projectile, CelestialProjectile или ArrowProjectile!");
            }
        }

        // КРИТИЧЕСКОЕ: Отправляем снаряд на сервер для синхронизации с другими игроками
        SendProjectileToServer(spawnPosition, direction, target);
    }

    /// <summary>
    /// Отправить информацию о снаряде на сервер для синхронизации
    /// </summary>
    private void SendProjectileToServer(Vector3 spawnPosition, Vector3 direction, Enemy target)
    {
        // Проверяем что мы в мультиплеере
        if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
        {
            Debug.Log("[PlayerAttack] Не в мультиплеере - пропускаем отправку снаряда на сервер");
            return;
        }

        // Получаем targetSocketId (если цель - другой игрок)
        string targetSocketId = "";
        if (target != null)
        {
            NetworkPlayer networkTarget = target.GetComponent<NetworkPlayer>();
            if (networkTarget != null)
            {
                targetSocketId = networkTarget.socketId;
            }
        }

        // Отправляем на сервер через событие attack с дополнительной информацией
        // Используем skillId = 0 для обычной атаки
        SocketIOManager.Instance.SendProjectileSpawned(0, spawnPosition, direction, targetSocketId);

        Debug.Log($"[PlayerAttack] 🚀 Снаряд отправлен на сервер: pos={spawnPosition}, dir={direction}, target={target?.GetEnemyName() ?? "None"}");
    }

    /// <summary>
    /// Проверить, атакует ли персонаж сейчас
    /// </summary>
    public bool IsAttacking()
    {
        return isAttacking;
    }

    /// <summary>
    /// Отрисовка гизмо для дальности атаки
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Красная сфера - максимальная дальность атаки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Зеленая сфера - оптимальная дистанция для удара
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, optimalAttackDistance);

        // Если есть цель - показываем линию
        if (targetSystem != null && targetSystem.HasTarget())
        {
            Enemy target = targetSystem.GetCurrentTarget();
            if (target != null)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);

                // Цвет линии зависит от дистанции
                if (distance <= optimalAttackDistance)
                {
                    Gizmos.color = Color.green; // Оптимально
                }
                else if (distance <= attackRange)
                {
                    Gizmos.color = Color.yellow; // В радиусе, но далеко
                }
                else
                {
                    Gizmos.color = Color.red; // Слишком далеко
                }

                Gizmos.DrawLine(transform.position, target.transform.position);

                // Показываем дистанцию
#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    (transform.position + target.transform.position) / 2f,
                    $"{distance:F2}m"
                );
#endif
            }
        }
    }

    // ============================================================
    // ANIMATION EVENT METHODS
    // ============================================================

    /// <summary>
    /// Вызывается из Animation Event в момент удара/выстрела
    /// Добавьте этот метод как Event в анимацию атаки!
    /// </summary>
    public void OnAttackHit()
    {
        Debug.Log("[PlayerAttack] 💥 Animation Event: OnAttackHit вызван!");

        // Проверяем что цель ещё существует и жива
        if (currentAttackTarget == null || !currentAttackTarget.IsAlive())
        {
            Debug.LogWarning("[PlayerAttack] ⚠️ Цель умерла или исчезла до момента удара!");
            currentAttackTarget = null;
            return;
        }

        // Рассчитываем финальный урон с учетом характеристик (Strength/Intelligence/Luck)
        float finalDamage = CalculateFinalDamage();

        // Дальняя атака - создаём снаряд
        GameObject currentProjectilePrefab = GetProjectilePrefab();
        if (GetIsRangedAttack() && currentProjectilePrefab != null)
        {
            SpawnProjectile(currentAttackTarget, finalDamage);
            Debug.Log($"[PlayerAttack] 🎯 Снаряд создан для {currentAttackTarget.GetEnemyName()}");
        }
        // Ближняя атака - наносим урон сразу
        else
        {
            // ВАЖНО: В мультиплеере НЕ наносим урон NetworkPlayer локально!
            // Сервер рассчитает урон и отправит событие enemy_damaged_by_server
            NetworkPlayer networkTarget = currentAttackTarget.GetComponent<NetworkPlayer>();
            if (networkTarget == null)
            {
                // Это обычный NPC враг - наносим урон локально
                currentAttackTarget.TakeDamage(finalDamage);
                Debug.Log($"[PlayerAttack] ⚔️ Урон {finalDamage:F0} нанесён NPC {currentAttackTarget.GetEnemyName()}");
            }
            else
            {
                // Это NetworkPlayer - урон нанесёт сервер
                Debug.Log($"[PlayerAttack] 🌐 Атака на NetworkPlayer {networkTarget.username} - ждём ответа сервера");
            }
        }

        // МУЛЬТИПЛЕЕР: Отправляем информацию об атаке на сервер
        NetworkCombatSync combatSync = GetComponent<NetworkCombatSync>();
        if (combatSync != null)
        {
            string attackType = GetIsRangedAttack() ? "ranged" : "melee";
            combatSync.SendAttack(currentAttackTarget.gameObject, finalDamage, attackType);
        }

        // Очищаем цель после использования
        currentAttackTarget = null;
    }

    /// <summary>
    /// Альтернативный метод - вызывается в конце анимации атаки
    /// Используйте OnAttackEnd если хотите действие в КОНЦЕ анимации
    /// </summary>
    public void OnAttackEnd()
    {
        Debug.Log("[PlayerAttack] Animation Event: OnAttackEnd вызван!");
        // Можно использовать для эффектов окончания атаки
    }

    // ============================================================
    // СИСТЕМА ИСПОЛЬЗОВАНИЯ СКИЛЛОВ
    // ============================================================

    /// <summary>
    /// Выбрать скилл по индексу (0-2)
    /// </summary>
    private void SelectSkill(int skillIndex)
    {
        if (skillManager == null)
        {
            Debug.LogWarning("[PlayerAttack] ❌ SkillManager не найден! Не могу выбрать скилл.");
            return;
        }

        if (skillIndex < 0 || skillIndex >= skillManager.equippedSkills.Count)
        {
            Debug.LogWarning($"[PlayerAttack] ❌ Некорректный индекс скилла: {skillIndex}");
            return;
        }

        // Если скилл уже выбран - снимаем выбор
        if (selectedSkillIndex == skillIndex)
        {
            selectedSkillIndex = -1;
            Debug.Log($"[PlayerAttack] 🔘 Скилл {skillIndex + 1} ОТМЕНЁН");
        }
        else
        {
            selectedSkillIndex = skillIndex;
            SkillConfig skill = skillManager.equippedSkills[skillIndex];
            Debug.Log($"[PlayerAttack] ✅ ВЫБРАН скилл {skillIndex + 1}: {skill.skillName}");
            Debug.Log($"[PlayerAttack] 💡 Теперь нажмите ПКМ для использования или 1/2/3 для отмены");
        }
    }

    /// <summary>
    /// ПРЯМОЕ использование скилла по индексу (БЕЗ ПКМ)
    /// </summary>
    private void UseSkillDirectly(int skillIndex)
    {
        if (skillManager == null)
        {
            Debug.LogWarning("[PlayerAttack] ❌ SkillManager не найден!");
            return;
        }

        if (skillIndex < 0 || skillIndex >= skillManager.equippedSkills.Count)
        {
            Debug.LogWarning($"[PlayerAttack] ❌ Некорректный индекс скилла: {skillIndex}");
            return;
        }

        SkillConfig skill = skillManager.equippedSkills[skillIndex];
        Debug.Log($"[PlayerAttack] 🎯 Прямое использование скилла {skillIndex + 1}: {skill.skillName}");

        // Определяем цель в зависимости от типа скилла
        Transform target = null;

        // Для скиллов требующих цель - используем текущую цель из TargetSystem
        if (skill.targetType == SkillTargetType.Enemy)
        {
            if (targetSystem != null && targetSystem.HasTarget())
            {
                target = targetSystem.GetCurrentTarget()?.transform;
            }
            else
            {
                Debug.LogWarning($"[PlayerAttack] ❌ Скилл {skill.skillName} требует цель, но цель не выбрана!");
                return;
            }
        }
        // Для скиллов на себя - цель = сам персонаж
        else if (skill.targetType == SkillTargetType.Self)
        {
            target = transform;
        }
        // Для остальных типов (NoTarget, GroundTarget, Directional) - null (обработается в SkillManager)

        // Используем скилл
        bool success = skillManager.UseSkill(skillIndex, target);

        if (success)
        {
            Debug.Log($"[PlayerAttack] ⚡ Скилл {skill.skillName} УСПЕШНО использован!");
        }
        else
        {
            Debug.LogWarning($"[PlayerAttack] ❌ Не удалось использовать скилл {skill.skillName}");
        }
    }

    /// <summary>
    /// Попытка использовать выбранный скилл (СТАРАЯ СИСТЕМА - DEPRECATED)
    /// </summary>
    private void TryUseSelectedSkill()
    {
        if (skillManager == null)
        {
            Debug.LogWarning("[PlayerAttack] ❌ SkillManager не найден!");
            return;
        }

        if (selectedSkillIndex < 0)
        {
            Debug.Log("[PlayerAttack] 💡 Сначала выберите скилл клавишами 1/2/3, затем нажмите ПКМ");
            return;
        }

        if (selectedSkillIndex >= skillManager.equippedSkills.Count)
        {
            Debug.LogWarning($"[PlayerAttack] ❌ Некорректный индекс выбранного скилла: {selectedSkillIndex}");
            selectedSkillIndex = -1;
            return;
        }

        SkillConfig skill = skillManager.equippedSkills[selectedSkillIndex];
        Debug.Log($"[PlayerAttack] 🎯 Попытка использовать скилл: {skill.skillName}");

        // Определяем цель в зависимости от типа скилла
        Transform target = null;

        // Для скиллов требующих цель - используем текущую цель из TargetSystem
        if (skill.targetType == SkillTargetType.Enemy)
        {
            if (targetSystem != null && targetSystem.HasTarget())
            {
                target = targetSystem.GetCurrentTarget()?.transform;
            }
            else
            {
                Debug.LogWarning($"[PlayerAttack] ❌ Скилл {skill.skillName} требует цель, но цель не выбрана!");
                return;
            }
        }
        // Для скиллов на себя - цель = сам персонаж
        else if (skill.targetType == SkillTargetType.Self)
        {
            target = transform;
        }
        // Для остальных типов (NoTarget, GroundTarget, Directional) - null (обработается в SkillManager)

        // Используем скилл
        bool success = skillManager.UseSkill(selectedSkillIndex, target);

        if (success)
        {
            Debug.Log($"[PlayerAttack] ⚡ Скилл {skill.skillName} УСПЕШНО использован!");
            // Снимаем выбор после использования
            selectedSkillIndex = -1;
        }
        else
        {
            Debug.LogWarning($"[PlayerAttack] ❌ Не удалось использовать скилл {skill.skillName} (проверьте кулдаун/ману/дистанцию)");
            // Оставляем скилл выбранным для повторной попытки
        }
    }

    /// <summary>
    /// Получить индекс выбранного скилла (-1 если не выбран)
    /// </summary>
    public int GetSelectedSkillIndex()
    {
        return selectedSkillIndex;
    }
}
