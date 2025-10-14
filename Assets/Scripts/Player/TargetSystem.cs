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

    [Header("Input Settings")]
    [Tooltip("Кнопка для переключения на следующую цель")]
    [SerializeField] private KeyCode nextTargetKey = KeyCode.Tab;

    [Tooltip("Кнопка для сброса цели")]
    [SerializeField] private KeyCode clearTargetKey = KeyCode.Escape;

    // Текущая цель
    private Enemy currentTarget = null;

    // Список всех врагов в зоне видимости
    private List<Enemy> enemiesInRange = new List<Enemy>();

    // Fog of War для проверки видимости
    private FogOfWar fogOfWar;

    // Событие смены цели
    public delegate void TargetChangeHandler(Enemy newTarget);
    public event TargetChangeHandler OnTargetChanged;

    void Start()
    {
        // Находим FogOfWar компонент
        fogOfWar = GetComponent<FogOfWar>();
        if (fogOfWar == null)
        {
            Debug.LogWarning("[TargetSystem] FogOfWar компонент не найден!");
        }
    }

    void Update()
    {
        // Обновляем список врагов в радиусе
        UpdateEnemiesInRange();

        // Обработка input
        HandleTargetInput();

        // Обработка клика по врагу
        HandleMouseClick();

        // Автотаргет если нет цели
        if (currentTarget == null || !currentTarget.IsAlive())
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

        // Проверяем видимость цели через FogOfWar
        if (currentTarget != null && fogOfWar != null)
        {
            if (!fogOfWar.IsEnemyVisible(currentTarget))
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
    /// Обработка клика мышкой по врагу
    /// </summary>
    private void HandleMouseClick()
    {
        // Левая кнопка мыши
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Кастуем луч на слой Enemy
            if (Physics.Raycast(ray, out hit, maxTargetRange))
            {
                // Проверяем попал ли луч во врага
                Enemy enemy = hit.collider.GetComponent<Enemy>();
                if (enemy == null)
                {
                    // Попробуем найти Enemy в родителе
                    enemy = hit.collider.GetComponentInParent<Enemy>();
                }

                if (enemy != null && enemy.IsAlive())
                {
                    // Проверяем видимость через FogOfWar
                    if (fogOfWar != null && !fogOfWar.IsEnemyVisible(enemy))
                    {
                        Debug.Log("[TargetSystem] Враг невидим (туман/стена)");
                        return;
                    }

                    Debug.Log($"[TargetSystem] Клик по врагу: {enemy.GetEnemyName()}");
                    SetTarget(enemy);
                }
            }
        }
    }

    /// <summary>
    /// Обновить список врагов в радиусе
    /// </summary>
    private void UpdateEnemiesInRange()
    {
        enemiesInRange.Clear();

        // Находим всех врагов с тегом "Enemy"
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemyObj in allEnemies)
        {
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy == null || !enemy.IsAlive())
                continue;

            // ИСПРАВЛЕНО: Проверяем расстояние только в горизонтальной плоскости (X,Z)
            // Игнорируем высоту (Y) чтобы высокие враги не считались далёкими
            Vector3 playerPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 enemyPosFlat = new Vector3(enemy.transform.position.x, 0, enemy.transform.position.z);
            float distance = Vector3.Distance(playerPosFlat, enemyPosFlat);

            // Проверяем дистанцию
            if (distance <= maxTargetRange)
            {
                // Проверяем угол (находится ли враг в поле зрения)
                // Также используем плоскую позицию для угла
                Vector3 directionToEnemy = (enemyPosFlat - playerPosFlat).normalized;
                float angle = Vector3.Angle(new Vector3(transform.forward.x, 0, transform.forward.z).normalized, directionToEnemy);

                if (angle <= targetFieldOfView / 2f)
                {
                    // Проверяем видимость через FogOfWar (туман + стены)
                    if (fogOfWar != null && !fogOfWar.IsEnemyVisible(enemy))
                        continue;

                    enemiesInRange.Add(enemy);
                }
            }
        }

        // Сортируем по дистанции (ближайшие первыми) - также используем плоское расстояние
        enemiesInRange = enemiesInRange.OrderBy(e =>
        {
            Vector3 playerPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 enemyPosFlat = new Vector3(e.transform.position.x, 0, e.transform.position.z);
            return Vector3.Distance(playerPosFlat, enemyPosFlat);
        }).ToList();
    }

    /// <summary>
    /// Автоматический таргет на ближайшего врага
    /// </summary>
    private void AutoTarget()
    {
        if (enemiesInRange.Count > 0)
        {
            SetTarget(enemiesInRange[0]);
        }
    }

    /// <summary>
    /// Переключиться на следующую цель
    /// </summary>
    public void SwitchToNextTarget()
    {
        if (enemiesInRange.Count == 0)
        {
            Debug.Log("[TargetSystem] Нет врагов в радиусе");
            return;
        }

        if (currentTarget == null)
        {
            // Если нет цели - выбираем первого
            SetTarget(enemiesInRange[0]);
        }
        else
        {
            // Находим индекс текущей цели
            int currentIndex = enemiesInRange.IndexOf(currentTarget);

            if (currentIndex == -1)
            {
                // Текущая цель не в списке - выбираем первого
                SetTarget(enemiesInRange[0]);
            }
            else
            {
                // Переключаемся на следующего (циклично)
                int nextIndex = (currentIndex + 1) % enemiesInRange.Count;
                SetTarget(enemiesInRange[nextIndex]);
            }
        }
    }

    /// <summary>
    /// Установить цель
    /// </summary>
    public void SetTarget(Enemy enemy)
    {
        if (currentTarget == enemy)
            return;

        // СКРЫВАЕМ никнейм старой цели (если это NetworkPlayer)
        if (currentTarget != null)
        {
            currentTarget.OnDeath -= OnCurrentTargetDeath;

            // Проверяем является ли старая цель NetworkPlayer
            NetworkPlayer oldNetworkPlayer = currentTarget.GetComponent<NetworkPlayer>();
            if (oldNetworkPlayer != null)
            {
                oldNetworkPlayer.HideNameplate();
            }
        }

        currentTarget = enemy;

        // ПОКАЗЫВАЕМ никнейм новой цели (если это NetworkPlayer)
        if (currentTarget != null)
        {
            currentTarget.OnDeath += OnCurrentTargetDeath;
            Debug.Log($"[TargetSystem] Новая цель: {currentTarget.GetEnemyName()}");

            // Проверяем является ли новая цель NetworkPlayer
            NetworkPlayer newNetworkPlayer = currentTarget.GetComponent<NetworkPlayer>();
            if (newNetworkPlayer != null)
            {
                // ВАЖНО: Показываем nameplate ТОЛЬКО если враг виден (не в тумане войны и не за стеной)
                if (fogOfWar != null && fogOfWar.IsEnemyVisible(currentTarget))
                {
                    newNetworkPlayer.ShowNameplate();
                    Debug.Log($"[TargetSystem] ✅ Никнейм {newNetworkPlayer.username} показан (враг виден)");
                }
                else
                {
                    Debug.Log($"[TargetSystem] ❌ Никнейм {newNetworkPlayer.username} НЕ показан (враг в тумане/за стеной)");
                }
            }
        }

        // Вызываем событие смены цели
        OnTargetChanged?.Invoke(currentTarget);
    }

    /// <summary>
    /// Сбросить цель
    /// </summary>
    public void ClearTarget()
    {
        if (currentTarget != null)
        {
            currentTarget.OnDeath -= OnCurrentTargetDeath;

            // СКРЫВАЕМ никнейм при сбросе цели (если это NetworkPlayer)
            NetworkPlayer networkPlayer = currentTarget.GetComponent<NetworkPlayer>();
            if (networkPlayer != null)
            {
                networkPlayer.HideNameplate();
            }

            Debug.Log("[TargetSystem] Цель сброшена");
        }

        currentTarget = null;
        OnTargetChanged?.Invoke(null);
    }

    /// <summary>
    /// Обработчик смерти текущей цели
    /// </summary>
    private void OnCurrentTargetDeath(Enemy deadEnemy)
    {
        Debug.Log($"[TargetSystem] Цель {deadEnemy.GetEnemyName()} убита. Автотаргет...");
        ClearTarget();
        AutoTarget();
    }

    /// <summary>
    /// Получить текущую цель
    /// </summary>
    public Enemy GetCurrentTarget()
    {
        return currentTarget;
    }

    /// <summary>
    /// Есть ли цель?
    /// </summary>
    public bool HasTarget()
    {
        return currentTarget != null && currentTarget.IsAlive();
    }

    /// <summary>
    /// Получить количество врагов в радиусе
    /// </summary>
    public int GetEnemyCount()
    {
        return enemiesInRange.Count;
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
