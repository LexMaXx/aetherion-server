using UnityEngine;

/// <summary>
/// Камера следует за целью с плавным движением
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

    void LateUpdate()
    {
        if (target == null)
            return;

        FollowTarget();
    }

    /// <summary>
    /// Следовать за целью
    /// </summary>
    private void FollowTarget()
    {
        // Вычисляем желаемую позицию камеры
        Vector3 desiredPosition = target.position + offset;
        desiredPosition.y = target.position.y + height;

        // Плавно движемся к желаемой позиции
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // Камера смотрит на цель с наклоном
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
}
