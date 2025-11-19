using UnityEngine;

/// <summary>
/// Third Person Camera Controller - камера как в Lineage 2 M
/// Поддерживает вращение мышью и плавное следование за игроком
/// </summary>
public class TPSCameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Camera Distance & Height")]
    [SerializeField] private float distance = 6f;          // Дистанция от персонажа
    [SerializeField] private float height = 3f;            // Высота камеры над персонажем
    [SerializeField] private float targetHeight = 1.5f;    // На какую высоту персонажа смотреть

    [Header("Camera Rotation")]
    [SerializeField] private float rotationSpeed = 3f;     // Скорость вращения мышью
    [SerializeField] private float minVerticalAngle = 5f;  // Минимальный угол (вниз)
    [SerializeField] private float maxVerticalAngle = 80f; // Максимальный угол (вверх)

    [Header("Zoom")]
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 12f;
    [SerializeField] private float zoomSpeed = 2f;

    [Header("Collision Detection")]
    [SerializeField] private bool checkCollisions = true;
    [SerializeField] private float collisionRadius = 0.3f;
    [SerializeField] private LayerMask collisionLayers = -1; // Все слои

    [Header("Input")]
    [SerializeField] private bool invertY = false;
    [SerializeField] private KeyCode rotateButton = KeyCode.Mouse1; // ПКМ для вращения

    // Текущие углы камеры
    private float currentHorizontalAngle = 0f;
    private float currentVerticalAngle = 30f;

    // Целевая позиция
    private Vector3 currentVelocity = Vector3.zero;

    void Start()
    {
        // Инициализация углов камеры относительно текущего направления
        if (target != null)
        {
            Vector3 angles = transform.eulerAngles;
            currentHorizontalAngle = angles.y;
            currentVerticalAngle = angles.x;
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        HandleInput();
        UpdateCameraPosition();
    }

    /// <summary>
    /// Обработка ввода (вращение мышью и зум)
    /// </summary>
    private void HandleInput()
    {
        // Вращение камеры (ПКМ + движение мыши)
        if (Input.GetKey(rotateButton))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

            // Горизонтальное вращение (вокруг персонажа)
            currentHorizontalAngle += mouseX;

            // Вертикальное вращение (вверх/вниз)
            if (invertY)
                currentVerticalAngle += mouseY;
            else
                currentVerticalAngle -= mouseY;

            // Ограничиваем вертикальный угол
            currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);

            // Показываем курсор только когда не вращаем камеру
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Зум колесиком мыши
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    /// <summary>
    /// Обновление позиции и вращения камеры
    /// </summary>
    private void UpdateCameraPosition()
    {
        // Вычисляем целевую позицию камеры на основе углов
        Quaternion rotation = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0);
        Vector3 pivotPosition = target.position + Vector3.up * height;
        Vector3 offset = rotation * new Vector3(0, 0, -distance);
        Vector3 desiredPosition = pivotPosition + offset;

        // Проверка коллизий - камера не должна проходить сквозь стены и землю
        Vector3 finalPosition = desiredPosition;

        if (checkCollisions)
        {
            Vector3 direction = desiredPosition - pivotPosition;
            float targetDistance = direction.magnitude;

            // SphereCast от персонажа к желаемой позиции камеры
            RaycastHit hit;
            if (Physics.SphereCast(pivotPosition, collisionRadius, direction.normalized, out hit, targetDistance, collisionLayers))
            {
                // Если что-то мешает, приближаем камеру
                float safeDistance = hit.distance - collisionRadius;
                safeDistance = Mathf.Max(safeDistance, minDistance * 0.5f); // Минимальная дистанция
                finalPosition = pivotPosition + direction.normalized * safeDistance;
            }
        }

        // ВАЖНО: Мгновенная привязка к персонажу (без задержки)
        transform.position = finalPosition;

        // Камера смотрит на точку над персонажем (мгновенно)
        Vector3 lookAtPosition = target.position + Vector3.up * targetHeight;
        transform.rotation = Quaternion.LookRotation(lookAtPosition - transform.position);
    }

    /// <summary>
    /// Установить цель для камеры
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        if (target != null)
        {
            // Инициализируем углы на основе текущей позиции камеры
            Vector3 direction = transform.position - target.position;
            currentHorizontalAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            currentVerticalAngle = Mathf.Asin(direction.y / direction.magnitude) * Mathf.Rad2Deg;

            // ВАЖНО: Мгновенно устанавливаем камеру в правильную позицию
            UpdateCameraPosition();
        }
    }

    /// <summary>
    /// Получить горизонтальный угол камеры (для движения персонажа)
    /// </summary>
    public float GetHorizontalAngle()
    {
        return currentHorizontalAngle;
    }

    /// <summary>
    /// Получить направление камеры (forward без Y компоненты)
    /// </summary>
    public Vector3 GetCameraForward()
    {
        Vector3 forward = Quaternion.Euler(0, currentHorizontalAngle, 0) * Vector3.forward;
        return forward.normalized;
    }

    /// <summary>
    /// Получить направление камеры вправо
    /// </summary>
    public Vector3 GetCameraRight()
    {
        Vector3 right = Quaternion.Euler(0, currentHorizontalAngle, 0) * Vector3.right;
        return right.normalized;
    }

    void OnDrawGizmosSelected()
    {
        if (target == null)
            return;

        // Рисуем линию от камеры к цели
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target.position + Vector3.up * targetHeight);

        // Рисуем сферу в точке, на которую смотрит камера
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(target.position + Vector3.up * targetHeight, 0.2f);
    }
}
