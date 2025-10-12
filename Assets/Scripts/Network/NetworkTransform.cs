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
    [Header("Interpolation Settings")]
    [Tooltip("Время интерполяции (мс). Рекомендуется 100-200мс")]
    [SerializeField] private float interpolationDelay = 100f; // 100ms

    [Tooltip("Скорость интерполяции позиции")]
    [SerializeField] private float positionLerpSpeed = 10f;

    [Tooltip("Скорость интерполяции ротации")]
    [SerializeField] private float rotationLerpSpeed = 15f;

    [Header("Prediction Settings")]
    [Tooltip("Включить Dead Reckoning (предсказание)")]
    [SerializeField] private bool enablePrediction = true;

    [Tooltip("Максимальное время предсказания без обновлений (секунды)")]
    [SerializeField] private float maxPredictionTime = 1f;

    [Header("Snap Settings")]
    [Tooltip("Дистанция для телепортации вместо интерполяции")]
    [SerializeField] private float snapThreshold = 5f;

    [Tooltip("Угол для мгновенной ротации")]
    [SerializeField] private float snapRotationThreshold = 90f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // State Buffer для интерполяции
    private struct NetworkState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public float timestamp;
    }

    private Queue<NetworkState> stateBuffer = new Queue<NetworkState>();
    private const int MAX_BUFFER_SIZE = 32;

    // Current state
    private Vector3 currentPosition;
    private Quaternion currentRotation;
    private Vector3 currentVelocity;

    // Target state (для интерполяции)
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    // Prediction
    private float lastUpdateTime;
    private bool isExtrapolating = false;

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
        // Интерполяция между состояниями
        InterpolateState();

        // Если долго не было обновлений - используем предсказание
        if (enablePrediction && Time.time - lastUpdateTime > interpolationDelay / 1000f)
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
    /// Получить обновление позиции от сервера
    /// </summary>
    public void ReceivePositionUpdate(Vector3 position, Quaternion rotation, Vector3 velocity, float timestamp)
    {
        // Создаем новое состояние
        NetworkState newState = new NetworkState
        {
            position = position,
            rotation = rotation,
            velocity = velocity,
            timestamp = timestamp
        };

        // Добавляем в буфер
        stateBuffer.Enqueue(newState);

        // Ограничиваем размер буфера
        while (stateBuffer.Count > MAX_BUFFER_SIZE)
        {
            stateBuffer.Dequeue();
        }

        // Обновляем статистику
        float latency = Time.time - timestamp;
        totalLatency += latency;
        updateCount++;

        lastUpdateTime = Time.time;
        isExtrapolating = false;

        // Проверяем нужна ли телепортация
        float distance = Vector3.Distance(transform.position, position);
        if (distance > snapThreshold)
        {
            // Слишком далеко - телепортируемся
            transform.position = position;
            transform.rotation = rotation;
            currentPosition = position;
            currentRotation = rotation;
            targetPosition = position;
            targetRotation = rotation;

            Debug.LogWarning($"[NetworkTransform] Snap! Distance: {distance:F2}m");
        }
    }

    /// <summary>
    /// Интерполяция между состояниями из буфера
    /// </summary>
    private void InterpolateState()
    {
        // Если буфер пуст - ничего не делаем
        if (stateBuffer.Count == 0)
            return;

        // Вычисляем время для интерполяции
        float renderTime = Time.time - (interpolationDelay / 1000f);

        // Находим два состояния для интерполяции
        NetworkState from = default;
        NetworkState to = default;
        bool foundStates = false;

        NetworkState[] states = stateBuffer.ToArray();

        for (int i = 0; i < states.Length - 1; i++)
        {
            if (states[i].timestamp <= renderTime && renderTime <= states[i + 1].timestamp)
            {
                from = states[i];
                to = states[i + 1];
                foundStates = true;
                break;
            }
        }

        if (!foundStates)
        {
            // Используем последнее состояние
            if (states.Length > 0)
            {
                to = states[states.Length - 1];
                from = to;
                foundStates = true;
            }
        }

        if (foundStates)
        {
            // Вычисляем коэффициент интерполяции
            float t = 0f;
            if (to.timestamp != from.timestamp)
            {
                t = (renderTime - from.timestamp) / (to.timestamp - from.timestamp);
                t = Mathf.Clamp01(t);
            }

            // Интерполируем
            targetPosition = Vector3.Lerp(from.position, to.position, t);
            targetRotation = Quaternion.Slerp(from.rotation, to.rotation, t);
            currentVelocity = Vector3.Lerp(from.velocity, to.velocity, t);

            // Применяем плавное движение к трансформу
            currentPosition = Vector3.Lerp(currentPosition, targetPosition, positionLerpSpeed * Time.deltaTime);
            currentRotation = Quaternion.Slerp(currentRotation, targetRotation, rotationLerpSpeed * Time.deltaTime);

            transform.position = currentPosition;
            transform.rotation = currentRotation;
        }

        // Очищаем старые состояния из буфера
        while (stateBuffer.Count > 0 && stateBuffer.Peek().timestamp < renderTime - 1f)
        {
            stateBuffer.Dequeue();
        }
    }

    /// <summary>
    /// Экстраполяция (предсказание) позиции при потере пакетов
    /// Dead Reckoning
    /// </summary>
    private void ExtrapolatePosition()
    {
        float timeSinceLastUpdate = Time.time - lastUpdateTime;

        // Не предсказываем слишком долго
        if (timeSinceLastUpdate > maxPredictionTime)
        {
            isExtrapolating = false;
            return;
        }

        // Используем velocity для предсказания
        if (currentVelocity.sqrMagnitude > 0.01f)
        {
            isExtrapolating = true;

            // Предсказываем позицию на основе скорости
            Vector3 predictedPosition = currentPosition + currentVelocity * Time.deltaTime;

            // Плавно двигаемся к предсказанной позиции
            currentPosition = Vector3.Lerp(currentPosition, predictedPosition, positionLerpSpeed * Time.deltaTime * 0.5f);
            transform.position = currentPosition;
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
    /// Размер буфера состояний
    /// </summary>
    public int GetBufferSize()
    {
        return stateBuffer.Count;
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

        GUILayout.BeginArea(new Rect(10, Screen.height - 150, 300, 140));
        GUILayout.Box($"Network Transform Debug\n" +
                      $"Buffer Size: {stateBuffer.Count}\n" +
                      $"Avg Latency: {GetAverageLatency():F1}ms\n" +
                      $"Velocity: {currentVelocity.magnitude:F2}m/s\n" +
                      $"Extrapolating: {isExtrapolating}\n" +
                      $"Updates: {updateCount}");
        GUILayout.EndArea();
    }

    /// <summary>
    /// Сброс состояния
    /// </summary>
    public void ResetState()
    {
        stateBuffer.Clear();
        currentPosition = transform.position;
        currentRotation = transform.rotation;
        targetPosition = currentPosition;
        targetRotation = currentRotation;
        currentVelocity = Vector3.zero;
        lastUpdateTime = Time.time;
        isExtrapolating = false;
        totalLatency = 0f;
        updateCount = 0;
    }
}
