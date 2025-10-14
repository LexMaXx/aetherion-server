using UnityEngine;

/// <summary>
/// Система очков действия (Action Points)
/// - Максимум 10 очков
/// - Атака стоит 4 очка
/// - Восстановление только когда персонаж стоит на месте
/// - Полное восстановление за ~10 секунд
/// </summary>
public class ActionPointsSystem : MonoBehaviour
{
    [Header("Action Points Settings")]
    [SerializeField] private int maxActionPoints = 10;
    [SerializeField] private int attackCost = 4;
    [SerializeField] private float regenerationTime = 10f; // Время полного восстановления (секунды)

    private int currentActionPoints;
    private float currentActionPointsFloat; // Дробный счётчик для точного восстановления
    private float regenerationRate; // Очков в секунду
    private float regenerationTimer = 0f;
    private bool isRegenerating = false;

    // Ссылки на компоненты
    private MixamoPlayerController playerController;
    private ActionPointsUI actionPointsUI;
    private CharacterStats characterStats; // Интеграция с SPECIAL (Agility)
    private Animator animator; // Для проверки состояния анимации

    // Событие изменения очков (для UI)
    public delegate void ActionPointsChangedHandler(int current, int max);
    public event ActionPointsChangedHandler OnActionPointsChanged;

    void Start()
    {
        // Интеграция с CharacterStats (Agility → AP)
        characterStats = GetComponent<CharacterStats>();
        if (characterStats != null)
        {
            characterStats.OnStatsChanged += UpdateAPFromStats;
            UpdateAPFromStats();
            Debug.Log("[ActionPoints] ✅ Интеграция с CharacterStats активирована");
        }
        else
        {
            // Если нет CharacterStats - используем дефолтные значения
            currentActionPoints = maxActionPoints;
            currentActionPointsFloat = maxActionPoints;
            regenerationRate = maxActionPoints / regenerationTime;
        }

        // Получаем ссылки
        playerController = GetComponent<MixamoPlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("[ActionPoints] MixamoPlayerController не найден!");
        }

        // Получаем Animator для проверки анимаций
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("[ActionPoints] Animator не найден!");
        }

        // Находим UI
        actionPointsUI = FindFirstObjectByType<ActionPointsUI>();
        if (actionPointsUI != null)
        {
            actionPointsUI.Initialize(this);
        }

        Debug.Log($"[ActionPoints] Система инициализирована: {currentActionPoints}/{maxActionPoints} AP");
        Debug.Log($"[ActionPoints] Скорость восстановления: {regenerationRate:F2} AP/сек");

        // Первое обновление UI
        OnActionPointsChanged?.Invoke(currentActionPoints, maxActionPoints);
    }

    /// <summary>
    /// Обновить AP на основе Agility
    /// </summary>
    private void UpdateAPFromStats()
    {
        if (characterStats == null) return;

        maxActionPoints = Mathf.RoundToInt(characterStats.MaxActionPoints);
        regenerationRate = characterStats.ActionPointsRegen;

        // Восстанавливаем AP пропорционально
        currentActionPoints = Mathf.Min(currentActionPoints, maxActionPoints);
        currentActionPointsFloat = currentActionPoints;

        OnActionPointsChanged?.Invoke(currentActionPoints, maxActionPoints);
        Debug.Log($"[ActionPoints] Обновлено из Stats: Max={maxActionPoints} AP, Regen={regenerationRate:F2} AP/сек (Agility: {characterStats.agility})");
    }

    private void OnDestroy()
    {
        if (characterStats != null)
        {
            characterStats.OnStatsChanged -= UpdateAPFromStats;
        }
    }

    void Update()
    {
        // Проверяем, стоит ли персонаж на месте
        bool isStanding = IsPlayerStanding();

        // ВОССТАНОВЛЕНИЕ: только когда стоит на месте
        if (isStanding && currentActionPoints < maxActionPoints)
        {
            if (!isRegenerating)
            {
                isRegenerating = true;
                regenerationTimer = 0f;
                Debug.Log("[ActionPoints] 🔄 Начало восстановления AP (персонаж стоит на месте - не двигается и не атакует)");
            }

            regenerationTimer += Time.deltaTime;

            // Добавляем очки к float счётчику
            currentActionPointsFloat += regenerationRate * Time.deltaTime;
            currentActionPointsFloat = Mathf.Min(currentActionPointsFloat, maxActionPoints);

            // Обновляем целочисленный счётчик
            int oldPoints = currentActionPoints;
            currentActionPoints = Mathf.FloorToInt(currentActionPointsFloat);

            // Обновляем UI только если изменилось целое число очков
            if (currentActionPoints != oldPoints)
            {
                Debug.Log($"[ActionPoints] ⬆️ Восстановлено: {currentActionPoints}/{maxActionPoints} AP (float: {currentActionPointsFloat:F2}, timer: {regenerationTimer:F1}s)");
                OnActionPointsChanged?.Invoke(currentActionPoints, maxActionPoints);
            }

            // Полностью восстановлено
            if (currentActionPoints >= maxActionPoints)
            {
                currentActionPoints = maxActionPoints;
                currentActionPointsFloat = maxActionPoints;
                isRegenerating = false;
                Debug.Log($"[ActionPoints] ✅ AP полностью восстановлены за {regenerationTimer:F1} секунд!");
            }
        }
        else if (!isStanding && isRegenerating)
        {
            // Персонаж начал двигаться/атаковать - останавливаем восстановление
            isRegenerating = false;
            Debug.Log("[ActionPoints] ⏸️ Восстановление остановлено (персонаж двигается или атакует)");
        }
    }

    /// <summary>
    /// Проверяет, стоит ли персонаж на месте (НЕ бегает, НЕ ходит, НЕ атакует)
    /// </summary>
    private bool IsPlayerStanding()
    {
        // ПРОВЕРКА 1: Проверяем ввод клавиш движения
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool hasMovementInput = Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f;

        if (hasMovementInput)
        {
            return false; // Игрок нажимает клавиши движения
        }

        // ПРОВЕРКА 2: Проверяем состояние аниматора
        if (animator != null)
        {
            // Проверяем параметр isMoving (используется в MixamoPlayerController)
            bool isMoving = animator.GetBool("isMoving");
            if (isMoving)
            {
                return false; // Персонаж двигается
            }

            // Проверяем параметр moveY (скорость движения)
            float moveY = animator.GetFloat("moveY");
            if (Mathf.Abs(moveY) > 0.01f)
            {
                return false; // Персонаж движется
            }

            // КРИТИЧЕСКОЕ: Проверяем, проигрывается ли анимация атаки
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool isAttacking = stateInfo.IsTag("Attack") ||
                              stateInfo.IsName("Attack") ||
                              stateInfo.IsName("attack") ||
                              stateInfo.normalizedTime < 1.0f && (
                                  stateInfo.IsName("Sword And Shield Slash") ||
                                  stateInfo.IsName("Standing Melee Attack Horizontal") ||
                                  stateInfo.IsName("Unarmed Melee Attack")
                              );

            if (isAttacking)
            {
                return false; // Персонаж атакует
            }
        }

        // ПРОВЕРКА 3: Проверяем скорость через CharacterController
        if (playerController != null)
        {
            // MixamoPlayerController использует CharacterController
            CharacterController charController = GetComponent<CharacterController>();
            if (charController != null && charController.velocity.magnitude > 0.1f)
            {
                return false; // Персонаж физически движется
            }
        }

        // ВСЕ ПРОВЕРКИ ПРОЙДЕНЫ: персонаж стоит на месте
        return true;
    }

    /// <summary>
    /// Попытка потратить очки на атаку
    /// </summary>
    public bool TrySpendActionPoints(int cost)
    {
        if (currentActionPoints >= cost)
        {
            currentActionPoints -= cost;
            currentActionPointsFloat = currentActionPoints; // Синхронизируем float счётчик
            Debug.Log($"[ActionPoints] 💸 Потрачено {cost} AP. Осталось: {currentActionPoints}/{maxActionPoints}");

            // Останавливаем восстановление при трате
            isRegenerating = false;
            regenerationTimer = 0f;

            OnActionPointsChanged?.Invoke(currentActionPoints, maxActionPoints);
            return true;
        }
        else
        {
            Debug.Log($"[ActionPoints] ❌ Недостаточно AP! Нужно {cost}, доступно {currentActionPoints}");
            return false;
        }
    }

    /// <summary>
    /// Проверка наличия достаточного количества очков для атаки
    /// </summary>
    public bool HasEnoughPointsForAttack()
    {
        return currentActionPoints >= attackCost;
    }

    /// <summary>
    /// Получить текущее количество очков
    /// </summary>
    public int GetCurrentPoints()
    {
        return currentActionPoints;
    }

    /// <summary>
    /// Получить максимальное количество очков
    /// </summary>
    public int GetMaxPoints()
    {
        return maxActionPoints;
    }

    /// <summary>
    /// Получить стоимость атаки
    /// </summary>
    public int GetAttackCost()
    {
        return attackCost;
    }

    /// <summary>
    /// Восстанавливается ли сейчас
    /// </summary>
    public bool IsRegenerating()
    {
        return isRegenerating;
    }
}
