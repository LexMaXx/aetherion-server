using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Управление персонажем на карте мира
/// Движение, взаимодействие с локациями, UI подсказки
/// Стиль как в Mount & Blade: Bannerlord
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class WorldMapPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Скорость движения на карте")]
    [SerializeField] private float moveSpeed = 10f;

    [Tooltip("Скорость поворота")]
    [SerializeField] private float rotationSpeed = 720f;

    [Tooltip("Гравитация")]
    [SerializeField] private float gravity = 20f;

    [Header("Interaction")]
    [Tooltip("Клавиша взаимодействия с локацией")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Tooltip("Клавиша открытия меню")]
    [SerializeField] private KeyCode menuKey = KeyCode.Escape;

    [Tooltip("Кнопка взаимодействия для мобильных устройств")]
    [SerializeField] private UnityEngine.UI.Button mobileInteractionButton;

    [Tooltip("Кнопка подтверждения входа в локацию (AcceptButton)")]
    [SerializeField] private UnityEngine.UI.Button acceptButton;

    [Header("UI References")]
    [Tooltip("Текст подсказки взаимодействия")]
    [SerializeField] private TextMeshProUGUI interactionPromptText;

    [Tooltip("Панель информации о локации")]
    [SerializeField] private GameObject locationInfoPanel;

    [Tooltip("Название локации")]
    [SerializeField] private TextMeshProUGUI locationNameText;

    [Tooltip("Описание локации")]
    [SerializeField] private TextMeshProUGUI locationDescriptionText;

    [Tooltip("Уровень сложности")]
    [SerializeField] private TextMeshProUGUI locationLevelText;

    [Tooltip("Иконка локации")]
    [SerializeField] private Image locationIconImage;

    [Header("Visual")]
    [Tooltip("Модель персонажа")]
    [SerializeField] private GameObject characterModel;

    // Runtime переменные
    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private WorldMapLocationMarker currentNearMarker;
    private bool isPaused = false;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // Убеждаемся что у персонажа есть тег Player
        if (!CompareTag("Player"))
        {
            gameObject.tag = "Player";
            Debug.Log("[WorldMapPlayerController] Установлен тег 'Player'");
        }
    }

    void Start()
    {
        // Скрываем UI элементы
        if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(false);
        }

        if (locationInfoPanel != null)
        {
            locationInfoPanel.SetActive(false);
        }

        // Настраиваем мобильную кнопку взаимодействия
        if (mobileInteractionButton != null)
        {
            mobileInteractionButton.onClick.AddListener(OnMobileInteractionButtonPressed);
            mobileInteractionButton.gameObject.SetActive(false); // Скрыта по умолчанию
        }

        // Настраиваем кнопку AcceptButton
        if (acceptButton != null)
        {
            acceptButton.onClick.AddListener(OnAcceptButtonPressed);
            acceptButton.gameObject.SetActive(false); // Скрыта по умолчанию
            Debug.Log("[WorldMapPlayerController] ✅ AcceptButton подключена");
        }

        Debug.Log("[WorldMapPlayerController] ✅ Инициализирован");
    }

    void Update()
    {
        if (isPaused)
            return;

        HandleMovement();
        HandleInteraction();
        CheckNearestLocation();
    }

    /// <summary>
    /// Обработка движения персонажа
    /// </summary>
    private void HandleMovement()
    {
        if (characterController == null)
            return;

        // Получаем input (поддержка мобильного джойстика)
        float horizontal = 0f;
        float vertical = 0f;

        // Приоритет мобильному джойстику
        if (MobileInputManager.Instance != null && MobileInputManager.Instance.IsMobileDevice())
        {
            Vector2 joystickInput = MobileInputManager.Instance.GetMovementInput();
            horizontal = joystickInput.x;
            vertical = joystickInput.y;
        }
        else
        {
            // Клавиатура/геймпад
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");
        }

        // Направление движения относительно камеры
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        // Убираем Y компонент (движение только по плоскости)
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Вычисляем направление движения
        Vector3 desiredMoveDirection = (forward * vertical + right * horizontal).normalized;

        // Применяем движение
        if (desiredMoveDirection.magnitude > 0.1f)
        {
            moveDirection.x = desiredMoveDirection.x * moveSpeed;
            moveDirection.z = desiredMoveDirection.z * moveSpeed;

            // Поворачиваем персонажа в сторону движения
            Quaternion targetRotation = Quaternion.LookRotation(desiredMoveDirection);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            // Анимация бега (если есть Animator)
            if (characterModel != null)
            {
                Animator animator = characterModel.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetBool("IsRunning", true);
                    animator.SetFloat("Speed", desiredMoveDirection.magnitude);
                }
            }
        }
        else
        {
            moveDirection.x = 0;
            moveDirection.z = 0;

            // Анимация idle
            if (characterModel != null)
            {
                Animator animator = characterModel.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetBool("IsRunning", false);
                    animator.SetFloat("Speed", 0);
                }
            }
        }

        // Гравитация
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }
        else
        {
            moveDirection.y = -0.5f; // Прижимаем к земле
        }

        // Двигаем персонажа
        characterController.Move(moveDirection * Time.deltaTime);
    }

    /// <summary>
    /// Обработка взаимодействия с локациями
    /// </summary>
    private void HandleInteraction()
    {
        // Проверяем нажатие клавиши взаимодействия
        if (Input.GetKeyDown(interactionKey))
        {
            if (currentNearMarker != null && currentNearMarker.IsUnlocked())
            {
                // Входим в локацию
                currentNearMarker.TryEnterLocation();
            }
        }

        // Меню (ESC)
        if (Input.GetKeyDown(menuKey))
        {
            OpenMenu();
        }
    }

    /// <summary>
    /// Проверка ближайшей локации
    /// </summary>
    private void CheckNearestLocation()
    {
        if (WorldMapManager.Instance == null)
        {
            Debug.LogWarning("[WorldMapPlayerController] WorldMapManager.Instance == null!");
            return;
        }

        WorldMapLocationMarker nearestMarker = WorldMapManager.Instance.GetNearestMarker();

        // Если изменился ближайший маркер
        if (nearestMarker != currentNearMarker)
        {
            currentNearMarker = nearestMarker;

            if (currentNearMarker != null)
            {
                Debug.Log($"[WorldMapPlayerController] 🎯 Обнаружена ближайшая локация: {currentNearMarker.GetLocationData().locationName}");
                ShowLocationInfo(currentNearMarker.GetLocationData());
                ShowInteractionPrompt(true);
            }
            else
            {
                HideLocationInfo();
                ShowInteractionPrompt(false);
            }
        }
    }

    /// <summary>
    /// Показать информацию о локации
    /// </summary>
    private void ShowLocationInfo(LocationData location)
    {
        if (location == null)
            return;

        if (locationInfoPanel != null)
        {
            locationInfoPanel.SetActive(true);
        }

        if (locationNameText != null)
        {
            locationNameText.text = location.locationName;
        }

        if (locationDescriptionText != null)
        {
            locationDescriptionText.text = location.description;
        }

        if (locationLevelText != null)
        {
            locationLevelText.text = $"Сложность: {location.difficultyLevel} | Рекомендуемый уровень: {location.recommendedLevel}";
        }

        if (locationIconImage != null && location.locationIcon != null)
        {
            locationIconImage.sprite = location.locationIcon;
            locationIconImage.color = location.iconColor;
        }
    }

    /// <summary>
    /// Скрыть информацию о локации
    /// </summary>
    private void HideLocationInfo()
    {
        if (locationInfoPanel != null)
        {
            locationInfoPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Показать/скрыть подсказку взаимодействия
    /// </summary>
    private void ShowInteractionPrompt(bool show)
    {
        if (interactionPromptText == null)
            return;

        if (show && currentNearMarker != null)
        {
            string locationName = currentNearMarker.GetLocationData().locationName;
            bool isUnlocked = currentNearMarker.IsUnlocked();

            if (isUnlocked)
            {
                // Текст зависит от платформы
                bool isMobile = MobileInputManager.Instance != null && MobileInputManager.Instance.IsMobileDevice();
                if (isMobile)
                {
                    interactionPromptText.text = $"Нажмите кнопку чтобы войти в {locationName}";
                }
                else
                {
                    interactionPromptText.text = $"Нажмите [{interactionKey}] чтобы войти в {locationName}";
                }
                interactionPromptText.color = Color.green;
            }
            else
            {
                interactionPromptText.text = $"{locationName} - Заблокировано";
                interactionPromptText.color = Color.red;
            }

            interactionPromptText.gameObject.SetActive(true);

            // Показываем кнопки взаимодействия
            if (isUnlocked)
            {
                bool isMobile = MobileInputManager.Instance != null && MobileInputManager.Instance.IsMobileDevice();

                // Мобильная кнопка (только для мобильных)
                if (mobileInteractionButton != null)
                {
                    mobileInteractionButton.gameObject.SetActive(isMobile);
                }

                // AcceptButton (для всех платформ)
                Debug.Log($"[WorldMapPlayerController] 🔍 Проверка acceptButton: {(acceptButton != null ? "НЕ NULL" : "NULL")}");

                if (acceptButton != null)
                {
                    Debug.Log($"[WorldMapPlayerController] 🔘 Пытаюсь показать AcceptButton, текущий active: {acceptButton.gameObject.activeSelf}");
                    acceptButton.gameObject.SetActive(true);
                    Debug.Log($"[WorldMapPlayerController] 🔘 AcceptButton показана, новый active: {acceptButton.gameObject.activeSelf}");
                }
                else
                {
                    Debug.LogWarning("[WorldMapPlayerController] ⚠️ acceptButton == null! Кнопка не подключена!");
                    Debug.LogWarning("[WorldMapPlayerController] 💡 AutoConnectAcceptButton должен был подключить кнопку через reflection!");
                }
            }
        }
        else
        {
            interactionPromptText.gameObject.SetActive(false);

            // Скрываем все кнопки
            if (mobileInteractionButton != null)
            {
                mobileInteractionButton.gameObject.SetActive(false);
            }

            if (acceptButton != null)
            {
                acceptButton.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Обработка нажатия мобильной кнопки взаимодействия
    /// </summary>
    private void OnMobileInteractionButtonPressed()
    {
        if (currentNearMarker != null && currentNearMarker.IsUnlocked())
        {
            currentNearMarker.TryEnterLocation();
        }
    }

    /// <summary>
    /// Обработка нажатия кнопки AcceptButton
    /// ПУБЛИЧНЫЙ метод для подключения через Inspector
    /// </summary>
    public void OnAcceptButtonPressed()
    {
        Debug.Log("[WorldMapPlayerController] 🔘 AcceptButton нажата!");

        if (currentNearMarker != null && currentNearMarker.IsUnlocked())
        {
            Debug.Log($"[WorldMapPlayerController] ✅ Вход в локацию: {currentNearMarker.GetLocationData().locationName}");
            currentNearMarker.TryEnterLocation();
        }
        else if (currentNearMarker != null && !currentNearMarker.IsUnlocked())
        {
            Debug.LogWarning($"[WorldMapPlayerController] ⚠️ Локация заблокирована: {currentNearMarker.GetLocationData().locationName}");
        }
        else
        {
            Debug.LogWarning("[WorldMapPlayerController] ⚠️ Нет локации рядом");
        }
    }

    /// <summary>
    /// Открыть меню карты мира
    /// </summary>
    private void OpenMenu()
    {
        Debug.Log("[WorldMapPlayerController] Открытие меню (ESC)");

        // Можно добавить UI меню с опциями:
        // - Вернуться в последнюю локацию
        // - Настройки
        // - Выход

        // Временно: возврат в последнюю локацию
        if (WorldMapManager.Instance != null)
        {
            WorldMapManager.Instance.ReturnToLastLocation();
        }
    }

    /// <summary>
    /// Пауза/продолжить
    /// </summary>
    public void SetPaused(bool paused)
    {
        isPaused = paused;

        // Останавливаем анимацию
        if (characterModel != null)
        {
            Animator animator = characterModel.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = !paused;
            }
        }
    }

    /// <summary>
    /// Телепортировать персонажа в позицию
    /// </summary>
    public void TeleportTo(Vector3 position)
    {
        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = position;
            characterController.enabled = true;
        }
        else
        {
            transform.position = position;
        }

        Debug.Log($"[WorldMapPlayerController] Телепорт в {position}");
    }

    // Gizmos для отладки
    void OnDrawGizmos()
    {
        // Показываем направление движения
        if (Application.isPlaying && moveDirection.magnitude > 0.1f)
        {
            Gizmos.color = Color.blue;
            Vector3 horizontalMove = new Vector3(moveDirection.x, 0, moveDirection.z);
            Gizmos.DrawRay(transform.position + Vector3.up, horizontalMove.normalized * 2f);
        }
    }
}
