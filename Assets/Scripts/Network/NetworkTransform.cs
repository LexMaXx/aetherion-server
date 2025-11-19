using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Компонент для плавной синхронизации трансформа по сети
///
/// ТЕХНОЛОГИИ:
/// - Linear Interpolation: плавное движение между обновлениями
/// - Dead Reckoning: предсказание движения на основе velocity
/// - Extrapolation: продолжение движения при потере пакетов
/// - Jitter Buffer: буфер для сглаживания нестабильного пинга
/// - Snap Threshold: телепортация при больших рассинхронизациях
/// </summary>
public class NetworkTransform : MonoBehaviour
{
    [Header("Interpolation Settings (Dota 2 Style - 60Hz)")]
    [Tooltip("Время интерполяции (мс). Уменьшено для реал-тайм отклика")]
    [SerializeField] private float interpolationDelay = 0f; // Нет задержки - мгновенный отклик

    [Tooltip("Скорость интерполяции позиции через SmoothDamp (меньше = быстрее)")]
    [SerializeField] private float positionSmoothTime = 0.05f; // УСКОРЕНО: 0.05 вместо 0.1 для 60Hz синхронизации

    [Tooltip("Скорость интерполяции ротации через Slerp")]
    [SerializeField] private float rotationLerpSpeed = 20f; // УСКОРЕНО: 20 вместо 15 для более быстрой реакции

    [Header("Prediction Settings")]
    [Tooltip("Включить Dead Reckoning (предсказание)")]
    [SerializeField] private bool enablePrediction = true;

    [Tooltip("Максимальное время предсказания без обновлений (секунды)")]
    [SerializeField] private float maxPredictionTime = 1f;

    [Header("Snap Settings")]
    [Tooltip("Дистанция для телепортации вместо интерполяции")]
    [SerializeField] private float snapThreshold = 5f; // 5м - только для критических рассинхронов (телепорт, respawn)

    [Header("Ground Snapping (предотвращение проваливания)")]
    [Tooltip("Включить автоматическое прилипание к земле")]
    [SerializeField] private bool enableGroundSnapping = true;

    [Tooltip("Максимальная дистанция raycast вниз для поиска земли")]
    [SerializeField] private float groundCheckDistance = 2f;

    [Tooltip("Слои для проверки земли")]
    [SerializeField] private LayerMask groundLayers = ~0; // По умолчанию все слои

    [Tooltip("Вертикальный offset от земли (м)")]
    [SerializeField] private float groundOffset = 1.08f; // Половина высоты CapsuleCollider (center.y)

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // State Buffer для интерполяции (убрали - используем прямую интерполяцию)
    // private Queue<NetworkState> stateBuffer = new Queue<NetworkState>();

    // Current state
    private Vector3 currentPosition;
    private Quaternion currentRotation;
    private Vector3 currentVelocity;
    private Vector3 velocityRef; // Для SmoothDamp

    // Target state (для интерполяции)
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 targetVelocity;

    // Prediction
    private float lastUpdateTime;
    private bool isExtrapolating = false;
    private float timeSinceLastUpdate = 0f;

    // Statistics
    private float totalLatency = 0f;
    private int updateCount = 0;

    void Start()
    {
        currentPosition = transform.position;
        currentRotation = transform.rotation;
        targetPosition = currentPosition;
        targetRotation = currentRotation;
        lastUpdateTime = Time.time;
    }

    void Update()
    {
        timeSinceLastUpdate = Time.time - lastUpdateTime;

        // МЕТОД 1: Vector3.SmoothDamp (как в Dota 2, очень плавно)
        // Плавно двигаемся к целевой позиции
        currentPosition = Vector3.SmoothDamp(
            currentPosition,
            targetPosition,
            ref velocityRef,
            positionSmoothTime,
            Mathf.Infinity,
            Time.deltaTime
        );

        // МЕТОД 2: Quaternion.Slerp для плавной ротации
        currentRotation = Quaternion.Slerp(
            currentRotation,
            targetRotation,
            rotationLerpSpeed * Time.deltaTime
        );

        // ИСПРАВЛЕНИЕ: Применяем Ground Snapping ПЕРЕД установкой позиции!
        // Это предотвращает проваливание под террейн
        // НО используем обычный transform.position (не MovePosition) чтобы избежать рассинхрона
        if (enableGroundSnapping)
        {
            currentPosition = SnapToGround(currentPosition);
        }

        // Применяем к transform
        transform.position = currentPosition;
        transform.rotation = currentRotation;

        // Если долго не было обновлений - используем предсказание
        if (enablePrediction && timeSinceLastUpdate > 0.1f)
        {
            ExtrapolatePosition();
        }

        // Debug визуализация
        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
    }

    /// <summary>
    /// Получить обновление позиции от сервера (улучшенная версия для L2M)
    /// </summary>
    public void ReceivePositionUpdate(Vector3 position, Quaternion rotation, Vector3 velocity, float timestamp)
    {
        // Обновляем статистику
        float latency = Time.time - timestamp;
        totalLatency += latency;
        updateCount++;

        lastUpdateTime = Time.time;
        isExtrapolating = false;

        // Сохраняем velocity для предсказания
        targetVelocity = velocity;
        currentVelocity = velocity;

        // Проверяем нужна ли телепортация (snap)
        float distance = Vector3.Distance(currentPosition, position);

        if (distance > snapThreshold)
        {
            // Слишком далеко - телепортируемся мгновенно
            transform.position = position;
            transform.rotation = rotation;
            currentPosition = position;
            currentRotation = rotation;
            targetPosition = position;
            targetRotation = rotation;
            velocityRef = Vector3.zero; // Сбрасываем SmoothDamp velocity

            if (showDebugInfo)
            {
                Debug.Log($"[NetworkTransform] Snap! Distance: {distance:F2}m");
            }
        }
        else
        {
            // Плавная интерполяция - просто устанавливаем целевые значения
            // SmoothDamp в Update() сам плавно доведет туда
            targetPosition = position;
            targetRotation = rotation;
        }
    }

    // УДАЛЕНО: InterpolateState() - теперь используем прямую SmoothDamp интерполяцию в Update()

    /// <summary>
    /// Экстраполяция (предсказание) позиции при потере пакетов
    /// Dead Reckoning - продолжаем движение по последнему известному направлению
    /// </summary>
    private void ExtrapolatePosition()
    {
        // Не предсказываем слишком долго
        if (timeSinceLastUpdate > maxPredictionTime)
        {
            isExtrapolating = false;
            currentVelocity = Vector3.zero; // Останавливаем предсказание
            return;
        }

        // Используем velocity для предсказания (если игрок двигался)
        if (currentVelocity.sqrMagnitude > 0.01f)
        {
            isExtrapolating = true;

            // Предсказываем позицию на основе скорости
            // Продолжаем движение по последнему известному направлению
            targetPosition += currentVelocity * Time.deltaTime;

            if (showDebugInfo)
            {
                Debug.Log($"[NetworkTransform] Extrapolating... velocity: {currentVelocity.magnitude:F2}m/s");
            }
        }
        else
        {
            isExtrapolating = false;
        }
    }

    /// <summary>
    /// Получить среднюю задержку
    /// </summary>
    public float GetAverageLatency()
    {
        if (updateCount == 0) return 0f;
        return (totalLatency / updateCount) * 1000f; // В миллисекундах
    }

    /// <summary>
    /// Получить текущую velocity
    /// </summary>
    public Vector3 GetVelocity()
    {
        return currentVelocity;
    }

    /// <summary>
    /// Проверка активности экстраполяции
    /// </summary>
    public bool IsExtrapolating()
    {
        return isExtrapolating;
    }

    /// <summary>
    /// Получить расстояние до целевой позиции
    /// </summary>
    public float GetDistanceToTarget()
    {
        return Vector3.Distance(currentPosition, targetPosition);
    }

    /// <summary>
    /// Debug визуализация
    /// </summary>
    private void DrawDebugInfo()
    {
        // Рисуем текущую позицию (зеленый)
        Debug.DrawRay(transform.position, Vector3.up * 2f, Color.green);

        // Рисуем целевую позицию (желтый)
        Debug.DrawRay(targetPosition, Vector3.up * 2f, Color.yellow);

        // Рисуем velocity (синий)
        if (currentVelocity.sqrMagnitude > 0.01f)
        {
            Debug.DrawRay(transform.position, currentVelocity, Color.blue);
        }

        // Рисуем линию между текущей и целевой позицией
        Debug.DrawLine(transform.position, targetPosition, isExtrapolating ? Color.red : Color.white);
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, Screen.height - 180, 350, 170));
        GUILayout.Box($"NetworkTransform Debug (L2M Style)\n" +
                      $"Distance to Target: {GetDistanceToTarget():F3}m\n" +
                      $"Avg Latency: {GetAverageLatency():F1}ms\n" +
                      $"Velocity: {currentVelocity.magnitude:F2}m/s\n" +
                      $"Extrapolating: {isExtrapolating}\n" +
                      $"Time Since Update: {timeSinceLastUpdate:F3}s\n" +
                      $"Updates: {updateCount}");
        GUILayout.EndArea();
    }

    /// <summary>
    /// Сброс состояния
    /// </summary>
    public void ResetState()
    {
        currentPosition = transform.position;
        currentRotation = transform.rotation;
        targetPosition = currentPosition;
        targetRotation = currentRotation;
        currentVelocity = Vector3.zero;
        targetVelocity = Vector3.zero;
        velocityRef = Vector3.zero;
        lastUpdateTime = Time.time;
        timeSinceLastUpdate = 0f;
        isExtrapolating = false;
        totalLatency = 0f;
        updateCount = 0;

        Debug.Log("[NetworkTransform] State reset");
    }

    /// <summary>
    /// Прилипание к земле для предотвращения проваливания
    /// Использует raycast вниз для поиска ближайшей поверхности
    /// </summary>
    private Vector3 SnapToGround(Vector3 position)
    {
        // Raycast вниз от позиции персонажа
        Vector3 rayStart = position + Vector3.up * 0.5f; // Начинаем с центра capsule
        RaycastHit hit;

        // Кастуем луч вниз
        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            // Земля найдена! Корректируем Y позицию
            Vector3 groundPosition = position;
            groundPosition.y = hit.point.y + groundOffset;

            // Debug визуализация (только если включен showDebugInfo)
            if (showDebugInfo)
            {
                Debug.DrawLine(rayStart, hit.point, Color.green, 0.1f);
            }

            return groundPosition;
        }
        else
        {
            // Земля не найдена - возвращаем исходную позицию без изменений
            if (showDebugInfo)
            {
                Debug.DrawRay(rayStart, Vector3.down * groundCheckDistance, Color.red, 0.1f);
            }
            return position;
        }
    }
}
