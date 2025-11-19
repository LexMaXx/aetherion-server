using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Система таргетирования врагов
/// Автоматический выбор ближайшего врага + переключение между целями
/// </summary>
public class TargetSystem : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Максимальная дистанция для автотаргета")]
    [SerializeField] private float maxTargetRange = 50f;

    [Tooltip("Угол обзора для автотаргета (180 = перед игроком)")]
    [SerializeField] private float targetFieldOfView = 180f;

    [Header("Auto-Target Settings")]
    [Tooltip("Автоматически таргетить врагов когда они входят в зону видимости")]
    [SerializeField] private bool enableAutoTargetOnEnemyVisible = true;

    [Tooltip("Автотаргет только на врагов (не союзников)")]
    [SerializeField] private bool autoTargetOnlyEnemies = true;

    [Tooltip("Приоритет врагов при автотаргете (враги > союзники)")]
    [SerializeField] private bool prioritizeEnemies = true;

    [Header("Input Settings")]
    [Tooltip("Кнопка для переключения на следующую цель")]
    [SerializeField] private KeyCode nextTargetKey = KeyCode.Tab;

    [Tooltip("Кнопка для сброса цели")]
    [SerializeField] private KeyCode clearTargetKey = KeyCode.Escape;

    // Текущая цель (НОВАЯ СИСТЕМА: TargetableEntity вместо Enemy)
    private TargetableEntity currentTarget = null;

    // Список всех существ в зоне видимости (враги + союзники + другие игроки)
    private List<TargetableEntity> entitiesInRange = new List<TargetableEntity>();

    // Предыдущий список для отслеживания новых врагов
    private List<TargetableEntity> previousEntitiesInRange = new List<TargetableEntity>();

    // Fog of War для проверки видимости
    private FogOfWar fogOfWar;

    // Локальная сущность игрока (для проверки "не атаковать самого себя")
    private TargetableEntity localPlayerEntity = null;

    // Событие смены цели
    public delegate void TargetChangeHandler(TargetableEntity newTarget);
    public event TargetChangeHandler OnTargetChanged;

    void Start()
    {
        // Находим FogOfWar компонент
        fogOfWar = GetComponent<FogOfWar>();
        if (fogOfWar == null)
        {
            Debug.LogWarning("[TargetSystem] FogOfWar компонент не найден!");
        }

        // НОВОЕ: Находим TargetableEntity локального игрока
        localPlayerEntity = GetComponent<TargetableEntity>();
        if (localPlayerEntity == null)
        {
            // Пробуем найти в родителе
            localPlayerEntity = GetComponentInParent<TargetableEntity>();
        }

        if (localPlayerEntity == null)
        {
            // На некоторых префабах TargetableEntity висит на дочернем объекте (модель)
            localPlayerEntity = GetComponentInChildren<TargetableEntity>();
        }

        if (localPlayerEntity != null)
        {
            Debug.Log($"[TargetSystem] ✅ Локальный игрок: {localPlayerEntity.GetEntityName()} (Faction: {localPlayerEntity.GetFaction()})");
        }
        else
        {
            Debug.LogWarning("[TargetSystem] ⚠️ TargetableEntity не найден на локальном игроке!");
        }
    }

    void Update()
    {
        // Обновляем список существ в радиусе
        UpdateEntitiesInRange();

        // Обработка input
        HandleTargetInput();

        // Обработка клика/касания (универсальная для десктопа и мобилки)
        HandlePointerInput();

        // Автотаргет если нет цели
        if (currentTarget == null || !currentTarget.IsEntityAlive())
        {
            AutoTarget();
        }

        // Проверяем дистанцию до цели (только горизонтальная плоскость X,Z)
        if (currentTarget != null)
        {
            Vector3 playerPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 targetPosFlat = new Vector3(currentTarget.transform.position.x, 0, currentTarget.transform.position.z);
            float distance = Vector3.Distance(playerPosFlat, targetPosFlat);

            if (distance > maxTargetRange)
            {
                Debug.Log("[TargetSystem] Цель вышла за пределы дистанции");
                ClearTarget();
            }
        }

        // Проверяем видимость цели через FogOfWar (используем Enemy компонент если есть)
        if (currentTarget != null && fogOfWar != null)
        {
            Enemy enemyComponent = currentTarget.GetComponent<Enemy>();
            if (enemyComponent != null && !fogOfWar.IsEnemyVisible(enemyComponent))
            {
                Debug.Log("[TargetSystem] Цель стала невидимой (туман/стена)");
                ClearTarget();
            }
        }
    }

    /// <summary>
    /// Обработка ввода для переключения целей
    /// </summary>
    private void HandleTargetInput()
    {
        // Tab - следующая цель
        if (Input.GetKeyDown(nextTargetKey))
        {
            SwitchToNextTarget();
        }

        // Escape - сбросить цель
        if (Input.GetKeyDown(clearTargetKey))
        {
            ClearTarget();
        }
    }

    /// <summary>
    /// Обработка клика/касания по врагу (универсально для десктопа и мобилки)
    /// </summary>
    private void HandlePointerInput()
    {
        Vector2? screenPosition = null;
        bool shouldCheckTarget = false;

        // ДЕСКТОП: Левая кнопка мыши
        if (Input.GetMouseButtonDown(0))
        {
            // Игнорируем если мышь над UI
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            screenPosition = Input.mousePosition;
            shouldCheckTarget = true;
        }
        // МОБИЛКА: Touch
        else if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // Только на начало касания
            if (touch.phase == TouchPhase.Began)
            {
                // Игнорируем если касание над UI (джойстик, кнопки скиллов)
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    return;
                }

                screenPosition = touch.position;
                shouldCheckTarget = true;
            }
        }

        // Проверяем попадание по врагу
        if (shouldCheckTarget && screenPosition.HasValue)
        {
            TrySelectEnemyAtPosition(screenPosition.Value);
        }
    }

    /// <summary>
    /// Попытка выбрать существо в указанной позиции экрана
    /// НОВАЯ СИСТЕМА: поддержка TargetableEntity и фракций
    /// </summary>
    private void TrySelectEnemyAtPosition(Vector2 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        // Кастуем луч на максимальную дистанцию таргета
        if (Physics.Raycast(ray, out hit, maxTargetRange))
        {
            // НОВОЕ: Ищем TargetableEntity вместо Enemy
            TargetableEntity entity = hit.collider.GetComponent<TargetableEntity>();
            if (entity == null)
            {
                // Попробуем найти в родителе
                entity = hit.collider.GetComponentInParent<TargetableEntity>();
            }

            if (entity != null && entity.IsEntityAlive())
            {
                // КРИТИЧЕСКОЕ: Не таргетим самого себя!
                if (IsLocalPlayerEntity(entity))
                {
                    Debug.LogWarning($"[TargetSystem] ⛔ Нельзя таргетить самого себя! Clicked: {entity.GetEntityName()}, LocalPlayer: {(localPlayerEntity != null ? localPlayerEntity.GetEntityName() : "NULL")}");
                    return;
                }

                // Дополнительная проверка по GameObject
                if (entity.gameObject == gameObject || entity.transform == transform)
                {
                    Debug.LogWarning("[TargetSystem] ⛔ Попытка таргетить самого себя (по GameObject/Transform)!");
                    return;
                }

                // Проверяем можно ли таргетить эту сущность
                if (!entity.IsTargetable())
                {
                    Debug.Log($"[TargetSystem] ⛔ {entity.GetEntityName()} нельзя таргетить");
                    return;
                }

                // Проверяем видимость через FogOfWar (если есть Enemy компонент)
                Enemy enemyComponent = entity.GetComponent<Enemy>();
                if (enemyComponent != null && fogOfWar != null && !fogOfWar.IsEnemyVisible(enemyComponent))
                {
                    Debug.Log("[TargetSystem] Существо невидимо (туман/стена)");
                    return;
                }

                Debug.Log($"[TargetSystem] 🎯 Таргет выбран: {entity.GetEntityName()} (Faction: {entity.GetFaction()})");
                SetTarget(entity);

                // Тактильная обратная связь на мобилке
#if UNITY_ANDROID || UNITY_IOS
                Handheld.Vibrate();
#endif
            }
        }
    }

    /// <summary>
    /// Обновить список существ в радиусе
    /// НОВАЯ СИСТЕМА: TargetableEntity + фракции + НЕ таргетим себя!
    /// AUTO-TARGET: Автоматически таргетит новых врагов входящих в зону видимости
    /// </summary>
    private void UpdateEntitiesInRange()
    {
        entitiesInRange.Clear();

        // Находим ВСЕХ существ с компонентом TargetableEntity
        TargetableEntity[] allEntities = FindObjectsOfType<TargetableEntity>();

        foreach (TargetableEntity entity in allEntities)
        {
            if (entity == null || !entity.IsEntityAlive())
                continue;

            // КРИТИЧЕСКОЕ: НЕ добавляем самого себя в список!
            if (IsLocalPlayerEntity(entity))
                continue;

            // Проверяем можно ли таргетить
            if (!entity.IsTargetable())
                continue;

            // ИСПРАВЛЕНО: Проверяем расстояние только в горизонтальной плоскости (X,Z)
            // Игнорируем высоту (Y) чтобы высокие существа не считались далёкими
            Vector3 playerPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 entityPosFlat = new Vector3(entity.transform.position.x, 0, entity.transform.position.z);
            float distance = Vector3.Distance(playerPosFlat, entityPosFlat);

            // Проверяем дистанцию
            if (distance <= maxTargetRange)
            {
                // Проверяем угол (находится ли существо в поле зрения)
                // Также используем плоскую позицию для угла
                Vector3 directionToEntity = (entityPosFlat - playerPosFlat).normalized;
                float angle = Vector3.Angle(new Vector3(transform.forward.x, 0, transform.forward.z).normalized, directionToEntity);

                if (angle <= targetFieldOfView / 2f)
                {
                    // Проверяем видимость через FogOfWar (туман + стены) - если есть Enemy компонент
                    Enemy enemyComponent = entity.GetComponent<Enemy>();
                    if (enemyComponent != null && fogOfWar != null && !fogOfWar.IsEnemyVisible(enemyComponent))
                        continue;

                    entitiesInRange.Add(entity);
                }
            }
        }

        // Сортируем по дистанции (ближайшие первыми) - также используем плоское расстояние
        entitiesInRange = entitiesInRange.OrderBy(e =>
        {
            Vector3 playerPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 entityPosFlat = new Vector3(e.transform.position.x, 0, e.transform.position.z);
            return Vector3.Distance(playerPosFlat, entityPosFlat);
        }).ToList();

        // AUTO-TARGET: Проверяем новых врагов, входящих в зону видимости
        if (enableAutoTargetOnEnemyVisible)
        {
            CheckForNewEnemies();
        }

        // Сохраняем текущий список для следующего кадра
        previousEntitiesInRange = new List<TargetableEntity>(entitiesInRange);
    }

    /// <summary>
    /// Проверить появились ли новые враги в зоне видимости и автоматически таргетить их
    /// </summary>
    private void CheckForNewEnemies()
    {
        // Находим новых существ (которых не было в предыдущем кадре)
        List<TargetableEntity> newEntities = new List<TargetableEntity>();

        foreach (TargetableEntity entity in entitiesInRange)
        {
            if (!previousEntitiesInRange.Contains(entity))
            {
                newEntities.Add(entity);
            }
        }

        // Если нет новых существ - ничего не делаем
        if (newEntities.Count == 0)
            return;

        // Фильтруем по настройкам
        List<TargetableEntity> validTargets = new List<TargetableEntity>();

        foreach (TargetableEntity entity in newEntities)
        {
            bool isEnemy = entity.GetFaction() == Faction.Enemy || entity.GetFaction() == Faction.OtherPlayer;
            bool isAlly = entity.GetFaction() == Faction.Ally;

            // Если включён режим "только враги" - пропускаем союзников
            if (autoTargetOnlyEnemies && !isEnemy)
                continue;

            validTargets.Add(entity);
        }

        // Если нет валидных целей - выходим
        if (validTargets.Count == 0)
            return;

        // Сортируем по приоритету: враги > союзники (если включён prioritizeEnemies)
        if (prioritizeEnemies)
        {
            validTargets = validTargets.OrderByDescending(e =>
            {
                Faction faction = e.GetFaction();
                if (faction == Faction.Enemy || faction == Faction.OtherPlayer)
                    return 2; // Враги - высший приоритет
                else if (faction == Faction.Ally)
                    return 1; // Союзники - средний приоритет
                else
                    return 0; // Нейтральные - низший приоритет
            }).ToList();
        }

        // Выбираем первую валидную цель (ближайшая после сортировки по дистанции)
        TargetableEntity newTarget = validTargets[0];

        // Автотаргет только если:
        // 1. Нет текущей цели
        // 2. ИЛИ текущая цель мертва
        // 3. ИЛИ новая цель - враг, а текущая - союзник (приоритет врагов)
        bool shouldAutoTarget = false;

        if (currentTarget == null || !currentTarget.IsEntityAlive())
        {
            shouldAutoTarget = true;
        }
        else if (prioritizeEnemies && currentTarget != null)
        {
            bool currentIsEnemy = currentTarget.GetFaction() == Faction.Enemy || currentTarget.GetFaction() == Faction.OtherPlayer;
            bool newIsEnemy = newTarget.GetFaction() == Faction.Enemy || newTarget.GetFaction() == Faction.OtherPlayer;

            // Переключаемся на врага если текущая цель - союзник
            if (newIsEnemy && !currentIsEnemy)
            {
                shouldAutoTarget = true;
            }
        }

        if (shouldAutoTarget)
        {
            Debug.Log($"[TargetSystem] ⚡ AUTO-TARGET: Новый враг вошёл в зону видимости: {newTarget.GetEntityName()} (Faction: {newTarget.GetFaction()})");
            SetTarget(newTarget);
        }
    }

    /// <summary>
    /// Автоматический таргет на ближайшего существа
    /// </summary>
    private void AutoTarget()
    {
        if (entitiesInRange.Count > 0)
        {
            SetTarget(entitiesInRange[0]);
        }
    }

    /// <summary>
    /// Переключиться на следующую цель
    /// </summary>
    public void SwitchToNextTarget()
    {
        if (entitiesInRange.Count == 0)
        {
            Debug.Log("[TargetSystem] Нет существ в радиусе");
            return;
        }

        if (currentTarget == null)
        {
            // Если нет цели - выбираем первого
            SetTarget(entitiesInRange[0]);
        }
        else
        {
            // Находим индекс текущей цели
            int currentIndex = entitiesInRange.IndexOf(currentTarget);

            if (currentIndex == -1)
            {
                // Текущая цель не в списке - выбираем первого
                SetTarget(entitiesInRange[0]);
            }
            else
            {
                // Переключаемся на следующего (циклично)
                int nextIndex = (currentIndex + 1) % entitiesInRange.Count;
                SetTarget(entitiesInRange[nextIndex]);
            }
        }
    }

    /// <summary>
    /// Установить цель
    /// НОВАЯ СИСТЕМА: TargetableEntity + проверка фракций
    /// </summary>
    public void SetTarget(TargetableEntity entity)
    {
        if (currentTarget == entity)
            return;

        // КРИТИЧЕСКОЕ: Не таргетим самого себя!
        if (IsLocalPlayerEntity(entity))
        {
            Debug.Log("[TargetSystem] ⛔ Нельзя установить самого себя как цель!");
            return;
        }

        // СКРЫВАЕМ никнейм старой цели
        if (currentTarget != null)
        {
            currentTarget.OnDeath -= OnCurrentTargetDeath;

            // Скрываем Nameplate (работает для NetworkPlayer и обычных врагов)
            Nameplate nameplate = currentTarget.GetComponent<Nameplate>();
            if (nameplate != null)
            {
                nameplate.Hide();
                Debug.Log($"[TargetSystem] Скрыт никнейм старой цели: {currentTarget.GetEntityName()}");
            }
        }

        currentTarget = entity;

        // ПОКАЗЫВАЕМ никнейм новой цели
        if (currentTarget != null)
        {
            currentTarget.OnDeath += OnCurrentTargetDeath;

            // Логируем с цветом фракции
            Color factionColor = currentTarget.GetFactionColor();
            Debug.Log($"[TargetSystem] 🎯 Новая цель: <color=#{ColorUtility.ToHtmlStringRGB(factionColor)}>{currentTarget.GetEntityName()}</color> (Faction: {currentTarget.GetFaction()})");

            // Показываем Nameplate (работает для NetworkPlayer и обычных врагов)
            Nameplate nameplate = currentTarget.GetComponent<Nameplate>();
            if (nameplate != null)
            {
                nameplate.Show();
                Debug.Log($"[TargetSystem] Показан никнейм новой цели: {currentTarget.GetEntityName()}");
            }
        }

        // Вызываем событие смены цели
        OnTargetChanged?.Invoke(currentTarget);
    }

    /// <summary>
    /// Проверяет принадлежит ли TargetableEntity локальному игроку (любой объект его иерархии / ownerId)
    /// </summary>
    private bool IsLocalPlayerEntity(TargetableEntity entity)
    {
        if (entity == null || localPlayerEntity == null)
            return false;

        if (entity == localPlayerEntity)
            return true;

        string localOwnerId = localPlayerEntity.GetOwnerId();
        if (!string.IsNullOrEmpty(localOwnerId) && localOwnerId == entity.GetOwnerId())
            return true;

        // Проверяем что оба относятся к одному PlayerController (например, модель ребёнок)
        PlayerController localController = localPlayerEntity.GetComponentInParent<PlayerController>();
        PlayerController entityController = entity.GetComponentInParent<PlayerController>();
        if (localController != null && entityController != null && localController == entityController)
            return true;

        return false;
    }

    /// <summary>
    /// Сбросить цель
    /// </summary>
    public void ClearTarget()
    {
        if (currentTarget != null)
        {
            currentTarget.OnDeath -= OnCurrentTargetDeath;

            // Скрываем Nameplate при сбросе таргета
            Nameplate nameplate = currentTarget.GetComponent<Nameplate>();
            if (nameplate != null)
            {
                nameplate.Hide();
                Debug.Log($"[TargetSystem] Скрыт никнейм при сбросе таргета: {currentTarget.GetEntityName()}");
            }

            Debug.Log("[TargetSystem] Цель сброшена");
        }

        currentTarget = null;
        OnTargetChanged?.Invoke(null);
    }

    /// <summary>
    /// Обработчик смерти текущей цели
    /// </summary>
    private void OnCurrentTargetDeath(TargetableEntity deadEntity)
    {
        Debug.Log($"[TargetSystem] Цель {deadEntity.GetEntityName()} убита. Автотаргет...");
        ClearTarget();
        AutoTarget();
    }

    /// <summary>
    /// Получить текущую цель
    /// </summary>
    public TargetableEntity GetCurrentTarget()
    {
        return currentTarget;
    }

    /// <summary>
    /// Есть ли цель?
    /// </summary>
    public bool HasTarget()
    {
        return currentTarget != null && currentTarget.IsEntityAlive();
    }

    /// <summary>
    /// Получить количество существ в радиусе
    /// </summary>
    public int GetEnemyCount()
    {
        return entitiesInRange.Count;
    }

    /// <summary>
    /// Получить количество врагов в радиусе (только враждебные)
    /// </summary>
    public int GetHostileCount()
    {
        return entitiesInRange.Count(e => e.GetFaction() == Faction.Enemy || e.GetFaction() == Faction.OtherPlayer);
    }

    /// <summary>
    /// Получить количество союзников в радиусе
    /// </summary>
    public int GetAllyCount()
    {
        return entitiesInRange.Count(e => e.GetFaction() == Faction.Ally);
    }

    /// <summary>
    /// Визуализация в редакторе
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Радиус таргета
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxTargetRange);

        // Линия к текущей цели
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }

        // Конус поля зрения
        if (Application.isPlaying)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Vector3 leftBoundary = Quaternion.Euler(0, -targetFieldOfView / 2f, 0) * transform.forward * maxTargetRange;
            Vector3 rightBoundary = Quaternion.Euler(0, targetFieldOfView / 2f, 0) * transform.forward * maxTargetRange;

            Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        }
    }
}
