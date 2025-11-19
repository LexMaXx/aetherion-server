using UnityEngine;

/// <summary>
/// Управление камерой через touch input для мобильных устройств
/// Поддерживает свайп для вращения камеры и pinch для зума
/// Улучшенная версия в стиле Lineage 2 Mobile
/// </summary>
public class TouchCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTarget; // Цель вращения камеры (обычно игрок)
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraHeight = 1.5f; // Высота точки фокуса от земли

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 0.3f; // Увеличена для более отзывчивого управления
    [SerializeField] private float minVerticalAngle = 10f; // Изменено: минимум 10 градусов (как в L2M)
    [SerializeField] private float maxVerticalAngle = 70f; // Изменено: максимум 70 градусов
    [SerializeField] private bool invertVertical = false;
    [SerializeField] private float rotationSmoothness = 5f; // Плавность вращения

    [Header("Zoom Settings")]
    [SerializeField] private bool enablePinchZoom = true;
    [SerializeField] private float minZoomDistance = 4f; // Минимальное приближение
    [SerializeField] private float maxZoomDistance = 12f; // Максимальное отдаление
    [SerializeField] private float zoomSpeed = 0.02f; // Скорость зума (меньше = плавнее)
    [SerializeField] private float zoomSmoothness = 10f; // Плавность зума

    [Header("Touch Areas")]
    [SerializeField] private bool useScreenSplit = true; // Разделение экрана на зоны
    [SerializeField] private float joystickAreaWidth = 0.35f; // 35% экрана для джойстика (слева)
    [SerializeField] private float skillBarAreaHeight = 0.25f; // 25% экрана для панели скиллов (снизу справа)

    [Header("Joystick Integration")]
    [SerializeField] private VirtualJoystick virtualJoystick; // Ссылка на джойстик для проверки состояния

    [Header("Collision Detection")]
    [SerializeField] private bool enableCollisionDetection = true; // Включить физическую коллизию
    [SerializeField] private float collisionCheckRadius = 0.3f; // Радиус сферы для проверки коллизий
    [SerializeField] private LayerMask collisionLayers = ~0; // Слои с которыми камера сталкивается (по умолчанию все)
    [SerializeField] private float collisionSmoothSpeed = 10f; // Скорость приближения/отдаления при коллизии

    // Camera state
    private float currentRotationX = 0f;
    private float currentRotationY = 35f; // Начальный угол (как в L2M)
    private float targetRotationX = 0f;
    private float targetRotationY = 35f;
    private float currentZoomDistance = 6f; // ИЗМЕНЕНО: Ближе к персонажу при старте (было 8f)
    private float targetZoomDistance = 6f; // ИЗМЕНЕНО: Ближе к персонажу при старте (было 8f)
    private float currentCollisionDistance = 8f; // Текущая дистанция с учетом коллизий

    // Touch tracking
    private Vector2 lastTouchPosition;
    private bool isTouching = false;
    private int cameraFingerID = -1; // ID пальца для камеры

    // Pinch zoom tracking
    private float lastPinchDistance = 0f;

    // Desktop fallback
    private bool isDesktop = false;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (cameraTarget == null)
        {
            // Пытаемся найти локального игрока
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                cameraTarget = player.transform;
            }
        }

        // Автоматически находим джойстик если не назначен
        if (virtualJoystick == null)
        {
            virtualJoystick = FindObjectOfType<VirtualJoystick>();
            if (virtualJoystick != null)
            {
                Debug.Log("[TouchCameraController] ✅ Автоматически найден VirtualJoystick");
            }
            else
            {
                Debug.LogWarning("[TouchCameraController] ⚠️ VirtualJoystick не найден! Pinch-to-zoom будет работать всегда.");
            }
        }

        // Определяем платформу
#if UNITY_EDITOR || UNITY_STANDALONE
        isDesktop = true;
#else
        isDesktop = false;
#endif

        // Инициализируем начальное положение камеры
        if (cameraTarget != null)
        {
            float distance = Vector3.Distance(mainCamera.transform.position, cameraTarget.position);
            currentZoomDistance = Mathf.Clamp(distance, minZoomDistance, maxZoomDistance);
            targetZoomDistance = currentZoomDistance;
            // Принудительно ставим камеру в корректную позицию сразу
            UpdateCameraPosition();
        }

        // Инициализируем углы
        targetRotationX = currentRotationX;
        targetRotationY = currentRotationY;

        Debug.Log($"[TouchCameraController] Инициализация. Платформа: {(isDesktop ? "Desktop" : "Mobile")}, Zoom: {currentZoomDistance}");
    }

    void LateUpdate()
    {
        if (cameraTarget == null) return;

        // Обрабатываем ввод
        if (isDesktop)
        {
            HandleMouseInput();
        }
        else
        {
            HandleTouchInput();
        }

        // Плавно интерполируем углы и зум
        SmoothCameraMovement();

        // Обновляем позицию камеры
        UpdateCameraPosition();
    }

    /// <summary>
    /// Плавная интерполяция камеры
    /// </summary>
    private void SmoothCameraMovement()
    {
        // Плавное вращение
        currentRotationX = Mathf.Lerp(currentRotationX, targetRotationX, Time.deltaTime * rotationSmoothness);
        currentRotationY = Mathf.Lerp(currentRotationY, targetRotationY, Time.deltaTime * rotationSmoothness);

        // Плавный зум
        currentZoomDistance = Mathf.Lerp(currentZoomDistance, targetZoomDistance, Time.deltaTime * zoomSmoothness);
    }

    /// <summary>
    /// Обработка touch input (улучшенная версия в стиле Lineage 2 Mobile)
    /// </summary>
    private void HandleTouchInput()
    {
        int touchCount = Input.touchCount;

        // Нет касаний - сброс состояния
        if (touchCount == 0)
        {
            isTouching = false;
            cameraFingerID = -1;
            lastPinchDistance = 0f;
            return;
        }

        // Одно касание - вращение камеры
        if (touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            // КРИТИЧНО: Игнорируем касания UI элементов (джойстик, кнопки скиллов)
            if (IsTouchOverUI(touch.fingerId))
            {
                isTouching = false;
                cameraFingerID = -1;
                return;
            }

            // Проверяем находится ли касание в ЗОНЕ КАМЕРЫ (не джойстик, не скилл-бар)
            if (useScreenSplit && !IsTouchInCameraZone(touch.position))
            {
                isTouching = false;
                cameraFingerID = -1;
                return;
            }

            // Обрабатываем фазы касания
            if (touch.phase == TouchPhase.Began)
            {
                isTouching = true;
                cameraFingerID = touch.fingerId;
                lastTouchPosition = touch.position;
                Debug.Log($"[TouchCamera] Camera rotation started (Finger: {touch.fingerId})");
            }
            else if (touch.phase == TouchPhase.Moved && isTouching && touch.fingerId == cameraFingerID)
            {
                Vector2 delta = touch.position - lastTouchPosition;

                // Применяем вращение к target (плавная интерполяция)
                float rotationX = delta.x * rotationSpeed;
                float rotationY = delta.y * rotationSpeed;

                if (invertVertical)
                {
                    rotationY = -rotationY;
                }

                targetRotationX += rotationX;
                targetRotationY -= rotationY;

                // Ограничиваем вертикальный угол
                targetRotationY = Mathf.Clamp(targetRotationY, minVerticalAngle, maxVerticalAngle);

                lastTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                if (touch.fingerId == cameraFingerID)
                {
                    isTouching = false;
                    cameraFingerID = -1;
                    Debug.Log("[TouchCamera] Camera rotation ended");
                }
            }
        }
        // Два касания - ПРИОРИТЕТ вращению камеры, потом pinch zoom
        else if (touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            // Проверяем сколько пальцев на UI
            bool touch1OnUI = IsTouchOverUI(touch1.fingerId);
            bool touch2OnUI = IsTouchOverUI(touch2.fingerId);

            // ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА: Находятся ли пальцы в зоне джойстика (левая часть экрана)
            bool touch1InJoystickZone = IsTouchInJoystickZone(touch1.position);
            bool touch2InJoystickZone = IsTouchInJoystickZone(touch2.position);

            // СЛУЧАЙ 1 (ПРИОРИТЕТНЫЙ): Хотя бы ОДИН палец на джойстике/UI → ТОЛЬКО вращение камеры (БЕЗ ЗУМА!)
            // Это исправляет проблему когда во время бега камера зумится вместо вращения
            if (touch1OnUI || touch2OnUI || touch1InJoystickZone || touch2InJoystickZone)
            {
                // Определяем какой палец для камеры (не на джойстике)
                Touch cameraTouch;
                if (touch1InJoystickZone || touch1OnUI)
                {
                    cameraTouch = touch2;
                }
                else
                {
                    cameraTouch = touch1;
                }

                // Проверяем что касание в зоне камеры
                if (useScreenSplit && !IsTouchInCameraZone(cameraTouch.position))
                {
                    isTouching = false;
                    cameraFingerID = -1;
                    lastPinchDistance = 0f; // КРИТИЧНО: Отключаем zoom!
                    return;
                }

                // ОТКЛЮЧАЕМ ZOOM полностью (сбрасываем lastPinchDistance)
                lastPinchDistance = 0f;

                // Обрабатываем как вращение камеры
                if (cameraTouch.phase == TouchPhase.Began)
                {
                    isTouching = true;
                    cameraFingerID = cameraTouch.fingerId;
                    lastTouchPosition = cameraTouch.position;
                    Debug.Log($"[TouchCamera] 🎮 Camera rotation (2-finger mode, joystick active) started (Finger: {cameraTouch.fingerId})");
                }
                else if (cameraTouch.phase == TouchPhase.Moved && cameraTouch.fingerId == cameraFingerID)
                {
                    Vector2 delta = cameraTouch.position - lastTouchPosition;

                    float rotationX = delta.x * rotationSpeed;
                    float rotationY = delta.y * rotationSpeed;

                    if (invertVertical)
                    {
                        rotationY = -rotationY;
                    }

                    targetRotationX += rotationX;
                    targetRotationY -= rotationY;
                    targetRotationY = Mathf.Clamp(targetRotationY, minVerticalAngle, maxVerticalAngle);

                    lastTouchPosition = cameraTouch.position;
                }
                else if (cameraTouch.phase == TouchPhase.Ended || cameraTouch.phase == TouchPhase.Canceled)
                {
                    if (cameraTouch.fingerId == cameraFingerID)
                    {
                        isTouching = false;
                        cameraFingerID = -1;
                        Debug.Log("[TouchCamera] Camera rotation (2-finger mode) ended");
                    }
                }
            }
            // СЛУЧАЙ 2: Оба пальца НЕ на UI и НЕ в зоне джойстика → Pinch-to-Zoom
            // НО ТОЛЬКО ЕСЛИ ДЖОЙСТИК НЕ НАЖАТ (стоим на месте)!
            else if (!touch1OnUI && !touch2OnUI && !touch1InJoystickZone && !touch2InJoystickZone && enablePinchZoom)
            {
                // КРИТИЧЕСКАЯ ПРОВЕРКА: Джойстик нажат?
                bool isJoystickActive = virtualJoystick != null && virtualJoystick.IsPressed;

                if (isJoystickActive)
                {
                    // Джойстик активен = игрок бежит → НЕ ЗУМИМ, только вращение камеры
                    lastPinchDistance = 0f; // Сбрасываем состояние зума

                    // Обрабатываем как вращение камеры (берём первый палец)
                    Touch cameraTouch = touch1;

                    if (cameraTouch.phase == TouchPhase.Began)
                    {
                        isTouching = true;
                        cameraFingerID = cameraTouch.fingerId;
                        lastTouchPosition = cameraTouch.position;
                        Debug.Log($"[TouchCamera] 🎮 Camera rotation (joystick active, ignoring zoom)");
                    }
                    else if (cameraTouch.phase == TouchPhase.Moved && cameraTouch.fingerId == cameraFingerID)
                    {
                        Vector2 delta = cameraTouch.position - lastTouchPosition;

                        float rotationX = delta.x * rotationSpeed;
                        float rotationY = delta.y * rotationSpeed;

                        if (invertVertical)
                        {
                            rotationY = -rotationY;
                        }

                        targetRotationX += rotationX;
                        targetRotationY -= rotationY;
                        targetRotationY = Mathf.Clamp(targetRotationY, minVerticalAngle, maxVerticalAngle);

                        lastTouchPosition = cameraTouch.position;
                    }
                }
                else
                {
                    // Джойстик НЕ активен = стоим на месте → МОЖНО ЗУМИТЬ!
                    float currentPinchDistance = Vector2.Distance(touch1.position, touch2.position);

                    if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
                    {
                        lastPinchDistance = currentPinchDistance;
                        Debug.Log("[TouchCamera] 🔍 Pinch zoom started (standing still)");
                    }
                    else if ((touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved) && lastPinchDistance > 0f)
                    {
                        // Рассчитываем изменение расстояния между пальцами
                        float pinchDelta = currentPinchDistance - lastPinchDistance;

                        // Применяем зум (отрицательный для интуитивного управления)
                        targetZoomDistance -= pinchDelta * zoomSpeed;
                        targetZoomDistance = Mathf.Clamp(targetZoomDistance, minZoomDistance, maxZoomDistance);

                        lastPinchDistance = currentPinchDistance;

                        Debug.Log($"[TouchCamera] 🔍 Zooming: {targetZoomDistance:F1}m (joystick not pressed)");
                    }
                    else if (touch1.phase == TouchPhase.Ended || touch2.phase == TouchPhase.Ended)
                    {
                        lastPinchDistance = 0f;
                        Debug.Log($"[TouchCamera] Pinch zoom ended. Final zoom: {targetZoomDistance:F1}");
                    }
                }
            }
            // СЛУЧАЙ 3: Оба пальца на UI → Игнорируем
            else
            {
                isTouching = false;
                cameraFingerID = -1;
                lastPinchDistance = 0f;
            }
        }
        // Три и более касаний - игнорируем
        else
        {
            isTouching = false;
            cameraFingerID = -1;
            lastPinchDistance = 0f;
        }
    }

    /// <summary>
    /// Обновить позицию камеры на основе текущих параметров С ФИЗИЧЕСКОЙ КОЛЛИЗИЕЙ
    /// </summary>
    private void UpdateCameraPosition()
    {
        // Рассчитываем rotation на основе углов
        Quaternion rotation = Quaternion.Euler(currentRotationY, currentRotationX, 0f);

        // Рассчитываем позицию камеры
        Vector3 direction = rotation * Vector3.back;
        Vector3 focusPoint = cameraTarget.position + Vector3.up * cameraHeight;

        // ФИЗИЧЕСКАЯ ПРОВЕРКА: SphereCast от игрока к желаемой позиции камеры
        float finalDistance = currentZoomDistance;

        if (enableCollisionDetection)
        {
            // SphereCast: проверяем есть ли препятствия между игроком и камерой
            RaycastHit hit;
            if (Physics.SphereCast(focusPoint, collisionCheckRadius, direction, out hit, currentZoomDistance, collisionLayers))
            {
                // КОЛЛИЗИЯ! Стена/препятствие найдено!
                // Приближаем камеру к игроку (останавливаемся перед препятствием)
                float safeDistance = hit.distance - collisionCheckRadius;
                finalDistance = Mathf.Max(safeDistance, minZoomDistance * 0.5f); // Минимальная дистанция

                // Debug: показываем что камера упёрлась в препятствие
                Debug.DrawLine(focusPoint, hit.point, Color.red, 0.1f);
            }
            else
            {
                // Нет коллизии - используем обычную дистанцию
                finalDistance = currentZoomDistance;
                Debug.DrawLine(focusPoint, focusPoint + direction * currentZoomDistance, Color.green, 0.1f);
            }

            // Плавно меняем текущую дистанцию (чтобы камера не прыгала)
            currentCollisionDistance = Mathf.Lerp(currentCollisionDistance, finalDistance, collisionSmoothSpeed * Time.deltaTime);
        }
        else
        {
            // Коллизия выключена - используем обычную дистанцию
            currentCollisionDistance = currentZoomDistance;
        }

        // Финальная позиция с учетом коллизий
        Vector3 targetPosition = focusPoint + direction * currentCollisionDistance;

        // Устанавливаем позицию и направление камеры
        mainCamera.transform.position = targetPosition;
        mainCamera.transform.LookAt(focusPoint);
    }

    /// <summary>
    /// Проверить находится ли касание в зоне джойстика (левая часть экрана)
    /// </summary>
    private bool IsTouchInJoystickZone(Vector2 touchPosition)
    {
        float joystickZoneWidth = Screen.width * joystickAreaWidth;
        return touchPosition.x < joystickZoneWidth;
    }

    /// <summary>
    /// Проверить находится ли касание в зоне камеры (не джойстик, не скилл-бар)
    /// </summary>
    private bool IsTouchInCameraZone(Vector2 touchPosition)
    {
        // Рассчитываем границы зон
        float joystickZoneWidth = Screen.width * joystickAreaWidth; // Левая зона для джойстика
        float skillBarZoneHeight = Screen.height * skillBarAreaHeight; // Нижняя правая зона для скиллов

        // Джойстик слева
        if (touchPosition.x < joystickZoneWidth)
        {
            return false; // Касание в зоне джойстика
        }

        // Скилл-бар справа снизу
        if (touchPosition.x > joystickZoneWidth && touchPosition.y < skillBarZoneHeight)
        {
            return false; // Касание в зоне скилл-бара
        }

        // Остальная часть экрана - зона камеры
        return true;
    }

    /// <summary>
    /// Проверить находится ли касание над UI элементом
    /// </summary>
    private bool IsTouchOverUI(int fingerID)
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
            return false;

        return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(fingerID);
    }

    /// <summary>
    /// Установить цель для камеры
    /// </summary>
    public void SetTarget(Transform target)
    {
        cameraTarget = target;

        if (target != null && mainCamera != null)
        {
            currentZoomDistance = Vector3.Distance(mainCamera.transform.position, target.position);
        }
        else if (mainCamera == null)
        {
            Debug.LogWarning("[TouchCameraController] mainCamera = null! Ищу Camera.main...");
            mainCamera = Camera.main;

            if (mainCamera != null && target != null)
            {
                currentZoomDistance = Vector3.Distance(mainCamera.transform.position, target.position);
            }
        }
    }

    /// <summary>
    /// Установить ссылку на виртуальный джойстик (для проверки состояния при zoom)
    /// </summary>
    public void SetVirtualJoystick(VirtualJoystick joystick)
    {
        virtualJoystick = joystick;
        Debug.Log($"[TouchCameraController] ✅ VirtualJoystick установлен: {joystick != null}");
    }

    /// <summary>
    /// Установить zoom distance
    /// </summary>
    public void SetZoomDistance(float distance)
    {
        currentZoomDistance = Mathf.Clamp(distance, minZoomDistance, maxZoomDistance);
    }

    /// <summary>
    /// Сбросить camera rotation к значениям по умолчанию
    /// </summary>
    public void ResetRotation()
    {
        currentRotationX = 0f;
        currentRotationY = 20f;
    }

    /// <summary>
    /// Обработка мыши в редакторе/десктопе (для тестирования)
    /// </summary>
    private void HandleMouseInput()
    {
        // ПКМ зажата - вращение камеры
        if (Input.GetMouseButton(1))
        {
            // Игнорируем если мышь над UI
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            // Применяем вращение к target (плавная интерполяция)
            float rotationX = mouseDelta.x * rotationSpeed * 100f;
            float rotationY = mouseDelta.y * rotationSpeed * 100f;

            if (invertVertical)
            {
                rotationY = -rotationY;
            }

            targetRotationX += rotationX;
            targetRotationY -= rotationY;

            // Ограничиваем вертикальный угол
            targetRotationY = Mathf.Clamp(targetRotationY, minVerticalAngle, maxVerticalAngle);
        }

        // Колёсико мыши - zoom
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            targetZoomDistance -= scrollDelta * 5f;
            targetZoomDistance = Mathf.Clamp(targetZoomDistance, minZoomDistance, maxZoomDistance);
        }
    }

    // Визуальный дебаг в редакторе
    void OnDrawGizmos()
    {
        if (cameraTarget == null || mainCamera == null) return;

        // Рисуем линию от камеры к цели
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(mainCamera.transform.position, cameraTarget.position);

        // Рисуем сферу на точке фокуса
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(cameraTarget.position + Vector3.up * cameraHeight, 0.3f);

        // НОВОЕ: Визуализация физической коллизии
        if (enableCollisionDetection)
        {
            Vector3 focusPoint = cameraTarget.position + Vector3.up * cameraHeight;
            Quaternion rotation = Quaternion.Euler(currentRotationY, currentRotationX, 0f);
            Vector3 direction = rotation * Vector3.back;

            // Сфера в точке игрока (начало проверки)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(focusPoint, collisionCheckRadius);

            // Сфера в текущей позиции камеры
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(focusPoint + direction * currentCollisionDistance, collisionCheckRadius);

            // Линия максимальной дистанции
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(focusPoint, focusPoint + direction * maxZoomDistance);
        }
    }
}
