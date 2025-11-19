using UnityEngine;

/// <summary>
/// Камера следует за целью с плавным движением + ФИЗИЧЕСКАЯ КОЛЛИЗИЯ СО СТЕНАМИ
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Offset Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 5, -8);
    [SerializeField] private float height = 5f;
    [SerializeField] private float distance = 8f;

    [Header("Smoothing")]
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private float rotationSmoothSpeed = 5f;

    [Header("Camera Angle")]
    [SerializeField] private float lookAngle = 15f; // Угол наклона камеры вниз

    [Header("Collision Settings")]
    [SerializeField] private bool enableCollision = true; // Включить физическую коллизию
    [SerializeField] private float collisionRadius = 0.3f; // Радиус сферы для проверки коллизий
    [SerializeField] private float minDistance = 1f; // Минимальное расстояние от игрока
    [SerializeField] private LayerMask collisionLayers = ~0; // Слои с которыми камера сталкивается (по умолчанию все)
    [SerializeField] private float collisionSmoothSpeed = 10f; // Скорость приближения/отдаления при коллизии

    private float currentDistance; // Текущая дистанция (может меняться при коллизии)

    void Start()
    {
        currentDistance = distance; // Инициализируем текущую дистанцию
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        FollowTarget();
    }

    /// <summary>
    /// Следовать за целью С ФИЗИЧЕСКОЙ КОЛЛИЗИЕЙ
    /// </summary>
    private void FollowTarget()
    {
        // 1. Вычисляем направление от игрока к желаемой позиции камеры
        Vector3 direction = -transform.forward; // Камера смотрит вперед, значит offset - назад
        if (offset.magnitude > 0.01f)
        {
            direction = offset.normalized;
        }

        // 2. Точка старта - позиция игрока (немного выше для лучшего обзора)
        Vector3 startPosition = target.position + Vector3.up * height;

        // 3. ФИЗИЧЕСКАЯ ПРОВЕРКА: SphereCast от игрока к желаемой позиции камеры
        float targetDistance = distance; // Желаемая дистанция

        if (enableCollision)
        {
            // SphereCast: проверяем есть ли препятствия между игроком и камерой
            RaycastHit hit;
            if (Physics.SphereCast(startPosition, collisionRadius, direction, out hit, distance, collisionLayers))
            {
                // КОЛЛИЗИЯ! Стена/препятствие найдено!
                // Приближаем камеру к игроку (останавливаемся перед препятствием)
                targetDistance = Mathf.Clamp(hit.distance - collisionRadius, minDistance, distance);

                // Debug: показываем что камера упёрлась в препятствие
                Debug.DrawLine(startPosition, hit.point, Color.red, 0.1f);
            }
            else
            {
                // Нет коллизии - возвращаемся к нормальной дистанции
                targetDistance = distance;
                Debug.DrawLine(startPosition, startPosition + direction * distance, Color.green, 0.1f);
            }
        }
        else
        {
            // Коллизия выключена - используем обычную дистанцию
            targetDistance = distance;
        }

        // 4. Плавно меняем текущую дистанцию (чтобы камера не прыгала)
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, collisionSmoothSpeed * Time.deltaTime);

        // 5. Вычисляем финальную позицию камеры с учетом коллизии
        Vector3 desiredPosition = startPosition + direction * currentDistance;

        // 6. Плавно движемся к желаемой позиции
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // 7. Камера смотрит на цель с наклоном
        Vector3 lookTarget = target.position + Vector3.up * 1.5f; // Смотрим чуть выше цели
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Установить цель для камеры
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        if (target != null)
        {
            // Мгновенно перемещаем камеру к новой цели (без плавности)
            Vector3 desiredPosition = target.position + offset;
            desiredPosition.y = target.position.y + height;
            transform.position = desiredPosition;

            // Сразу смотрим на цель
            Vector3 lookTarget = target.position + Vector3.up * 1.5f;
            transform.rotation = Quaternion.LookRotation(lookTarget - transform.position);
        }
    }

    /// <summary>
    /// Установить offset камеры
    /// </summary>
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }

    /// <summary>
    /// Установить высоту камеры
    /// </summary>
    public void SetHeight(float newHeight)
    {
        height = newHeight;
        offset.y = newHeight;
    }

    /// <summary>
    /// Установить дистанцию камеры
    /// </summary>
    public void SetDistance(float newDistance)
    {
        distance = newDistance;
        offset.z = -newDistance;
    }

    /// <summary>
    /// Включить/выключить физическую коллизию
    /// </summary>
    public void SetCollisionEnabled(bool enabled)
    {
        enableCollision = enabled;
    }

    /// <summary>
    /// Визуализация для отладки (показывает сферу коллизии в редакторе)
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (target == null || !enableCollision)
            return;

        // Показываем сферу коллизии
        Vector3 direction = offset.normalized;
        Vector3 startPosition = target.position + Vector3.up * height;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(startPosition, collisionRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(startPosition + direction * currentDistance, collisionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(startPosition, startPosition + direction * distance);
    }
}
