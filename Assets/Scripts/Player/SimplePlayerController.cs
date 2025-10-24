using UnityEngine;

/// <summary>
/// Простой контроллер для тестирования скиллов в SkillTestScene
/// </summary>
public class SimplePlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    private float speedModifier = 0f; // Модификатор скорости в процентах (может быть + или -)

    [Header("Components")]
    private CharacterController characterController;
    private SkillExecutor skillExecutor;
    private EffectManager effectManager;
    private Animator animator;

    [Header("Target")]
    private Transform currentTarget;
    private GameObject[] enemies;

    void Start()
    {
        // Получаем компоненты
        characterController = GetComponent<CharacterController>();
        skillExecutor = GetComponent<SkillExecutor>();
        effectManager = GetComponent<EffectManager>();
        animator = GetComponent<Animator>();

        // Находим всех врагов в сцене
        enemies = GameObject.FindGameObjectsWithTag("Enemy");

        Debug.Log($"[SimplePlayerController] ✅ Инициализация завершена. Найдено врагов: {enemies.Length}");
        Debug.Log("[SimplePlayerController] 🎮 Управление:");
        Debug.Log("  WASD - Движение");
        Debug.Log("  ЛКМ - Выбрать ближайшего врага");
        Debug.Log("  1/2/3 - Использовать скиллы");
    }

    void Update()
    {
        // Проверка контроля (стан, сон и т.д.)
        if (effectManager != null && effectManager.IsUnderCrowdControl())
        {
            Debug.Log("[SimplePlayerController] ⛔ Заблокирован CC эффектом!");
            return;
        }

        // Движение
        HandleMovement();

        // Выбор цели
        HandleTargeting();

        // Использование скиллов
        HandleSkills();

        // Debug информация
        if (Input.GetKeyDown(KeyCode.H))
        {
            PrintHelp();
        }
    }

    void HandleMovement()
    {
        if (characterController == null) return;
        if (effectManager != null && !effectManager.CanMove())
        {
            return; // Заблокирован (Root, Stun, etc.)
        }

        // Получаем ввод
        float horizontal = Input.GetAxis("Horizontal"); // A/D
        float vertical = Input.GetAxis("Vertical");     // W/S

        // Направление движения
        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

        if (moveDirection.magnitude >= 0.1f)
        {
            // Поворот в сторону движения
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Движение с учётом модификаторов скорости
            float currentSpeed = moveSpeed * (1f + speedModifier / 100f);
            Vector3 move = moveDirection * currentSpeed * Time.deltaTime;
            move.y = -9.81f * Time.deltaTime; // Гравитация
            if (characterController.enabled) characterController.Move(move);

            // Анимация бега
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool("IsMoving", true);
            }
        }
        else
        {
            // Гравитация когда стоим
            Vector3 gravity = new Vector3(0, -9.81f * Time.deltaTime, 0);
            if (characterController.enabled) characterController.Move(gravity);

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool("IsMoving", false);
            }
        }
    }

    void HandleTargeting()
    {
        // ЛКМ - выбрать ближайшего врага
        if (Input.GetMouseButtonDown(0))
        {
            // Ищем ближайшего врага
            GameObject closestEnemy = null;
            float closestDistance = float.MaxValue;

            foreach (GameObject enemy in enemies)
            {
                if (enemy == null) continue;

                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }

            if (closestEnemy != null)
            {
                currentTarget = closestEnemy.transform;
                Debug.Log($"[SimplePlayerController] 🎯 Цель: {closestEnemy.name} (дистанция: {closestDistance:F1}м)");

                // Поворачиваемся к цели
                Vector3 direction = (currentTarget.position - transform.position).normalized;
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }
            else
            {
                Debug.LogWarning("[SimplePlayerController] ❌ Враги не найдены!");
            }
        }
    }

    void HandleSkills()
    {
        if (skillExecutor == null) return;
        if (effectManager != null && !effectManager.CanUseSkills())
        {
            Debug.Log("[SimplePlayerController] ⛔ Не могу использовать скиллы (Silence, Stun, etc.)");
            return;
        }

        // Клавиши 1/2/3/4/5 для скиллов
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            UseSkill(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            UseSkill(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            UseSkill(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            UseSkill(3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            UseSkill(4);
        }
    }

    void UseSkill(int slotIndex)
    {
        if (skillExecutor == null) return;

        Debug.Log($"[SimplePlayerController] 🔥 Попытка использовать скилл в слоте {slotIndex}");

        // Получаем скилл из слота
        SkillConfig skill = skillExecutor.GetEquippedSkill(slotIndex);

        if (skill == null)
        {
            Debug.LogWarning($"[SimplePlayerController] ❌ Слот {slotIndex} пуст!");
            return;
        }

        // Ground Target скиллы (Teleport, Meteor) - клик ПКМ на землю
        if (skill.targetType == SkillTargetType.Ground)
        {
            Debug.Log($"[SimplePlayerController] 📍 Ground target скилл. Нажмите ПКМ на землю для выбора позиции.");

            // Для тестирования - телепорт на 5 метров вперёд
            Vector3 groundTarget = transform.position + transform.forward * 5f;
            bool success = skillExecutor.UseSkill(slotIndex, null, groundTarget);

            if (!success)
            {
                Debug.LogWarning($"[SimplePlayerController] ❌ Не удалось использовать скилл {slotIndex}");
            }
            return;
        }

        // Для скиллов которые не требуют цель (AOE вокруг себя), передаём null
        Transform targetToUse = skill.requiresTarget ? currentTarget : null;

        // Используем скилл
        bool success2 = skillExecutor.UseSkill(slotIndex, targetToUse, null);

        if (!success2)
        {
            Debug.LogWarning($"[SimplePlayerController] ❌ Не удалось использовать скилл {slotIndex}");

            if (skill.requiresTarget && currentTarget == null)
            {
                Debug.LogWarning("  → Цель не выбрана! Нажмите ЛКМ чтобы выбрать врага");
            }
        }
    }

    void PrintHelp()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("🎮 УПРАВЛЕНИЕ В ТЕСТОВОЙ СЦЕНЕ:");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("  WASD - Движение");
        Debug.Log("  ЛКМ - Выбрать ближайшего врага");
        Debug.Log("  1 - Fireball (требует цель)");
        Debug.Log("  2 - Ice Nova (AOE вокруг себя)");
        Debug.Log("  3 - Lightning Storm (AOE + Chain Lightning)");
        Debug.Log("  4 - Teleport (телепорт вперёд на 5м)");
        Debug.Log("  5 - Meteor (ground target, cast 2 сек)");
        Debug.Log("  H - Показать эту справку");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"📊 СТАТУС:");
        Debug.Log($"  Текущая цель: {(currentTarget != null ? currentTarget.name : "НЕТ")}");
        Debug.Log($"  Врагов в сцене: {enemies.Length}");
        Debug.Log($"  SkillExecutor: {(skillExecutor != null ? "✅" : "❌")}");
        Debug.Log($"  EffectManager: {(effectManager != null ? "✅" : "❌")}");
        Debug.Log("═══════════════════════════════════════════════════════");
    }

    // ═══════════════════════════════════════════════════════════════
    // ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ ЭФФЕКТОВ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Добавить модификатор скорости (в процентах)
    /// </summary>
    public void AddSpeedModifier(float percentModifier)
    {
        speedModifier += percentModifier;
        Debug.Log($"[SimplePlayerController] 🏃 Скорость изменена: {(percentModifier > 0 ? "+" : "")}{percentModifier}% (итого: {speedModifier}%)");
    }

    /// <summary>
    /// Убрать модификатор скорости
    /// </summary>
    public void RemoveSpeedModifier(float percentModifier)
    {
        speedModifier -= percentModifier;
        Debug.Log($"[SimplePlayerController] 🏃 Модификатор скорости снят: {percentModifier}% (итого: {speedModifier}%)");
    }

    /// <summary>
    /// Сбросить все модификаторы скорости
    /// </summary>
    public void ResetSpeedModifiers()
    {
        speedModifier = 0f;
        Debug.Log("[SimplePlayerController] 🏃 Все модификаторы скорости сброшены");
    }

    // Визуализация текущей цели
    void OnDrawGizmos()
    {
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up, currentTarget.position + Vector3.up);
            Gizmos.DrawWireSphere(currentTarget.position + Vector3.up, 1f);
        }
    }
}
