using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Кнопка скилла для Arena Scene (аналогично AttackButton)
/// Гарантированная работа кликов/тапов с визуальной обратной связью и cooldown
/// </summary>
public class SkillButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Skill Settings")]
    [Tooltip("Индекс скилла (0 = кнопка 1, 1 = кнопка 2, и т.д.)")]
    [SerializeField] private int skillIndex = 0;

    [Header("UI Components")]
    [SerializeField] private Image iconImage; // Иконка скилла
    [SerializeField] private Image cooldownOverlay; // Затемнение во время cooldown
    [SerializeField] private TextMeshProUGUI cooldownText; // Текст оставшегося времени
    [SerializeField] private TextMeshProUGUI hotkeyText; // Текст горячей клавиши (1, 2, 3...)

    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("Haptic Feedback")]
    [SerializeField] private bool enableHapticFeedback = true;

    // State
    private bool isPressed = false;
    private bool isOnCooldown = false;
    private float cooldownRemaining = 0f;
    private float cooldownDuration = 0f;
    private SkillConfig currentSkillConfig; // НОВАЯ СИСТЕМА: SkillConfig вместо SkillData
    private SkillData currentSkill; // Старая система (для обратной совместимости)

    // Button Component для гарантированной работы кликов
    private Button buttonComponent;

    // Кэш локального игрока (чтобы не искать каждый раз)
    private static GameObject cachedLocalPlayer = null;

    void Awake()
    {
        // Настраиваем Button component для гарантированной работы кликов
        SetupButtonComponent();

        // Настраиваем начальное состояние
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = 0f;
        }

        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(false);
        }

        // Устанавливаем hotkey текст
        if (hotkeyText != null)
        {
            hotkeyText.text = (skillIndex + 1).ToString();
        }

        UpdateVisuals();
    }

    void Start()
    {
        Debug.Log($"[SkillButton] Инициализирован слот {skillIndex + 1} (индекс {skillIndex})");
    }

    void Update()
    {
        // Проверяем что игрок уже загрузился в игру
        if (cachedLocalPlayer == null)
        {
            // Первая попытка найти игрока (тихо, без логов)
            cachedLocalPlayer = FindLocalPlayer(silent: true);

            // Если не нашли - просто выходим (без спама)
            if (cachedLocalPlayer == null)
            {
                return; // Игрок ещё не загрузился, ждём
            }
        }

        // ОТКЛЮЧЕНО: Синхронизация вызывает спам ошибок в мультиплеере
        // Синхронизация не нужна - кулдаун запускается при UseSkill()
        // SyncCooldownWithSkillExecutor();

        // Обновляем cooldown (локальный или из SkillExecutor)
        if (isOnCooldown && cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;

            // Обновляем визуальный индикатор cooldown
            if (cooldownOverlay != null && cooldownDuration > 0f)
            {
                cooldownOverlay.fillAmount = cooldownRemaining / cooldownDuration;
            }

            // Обновляем текст
            if (cooldownText != null)
            {
                cooldownText.text = Mathf.Ceil(cooldownRemaining).ToString();
            }

            // Cooldown завершён
            if (cooldownRemaining <= 0f)
            {
                EndCooldown();
            }

            UpdateVisuals();
        }
    }

    /// <summary>
    /// Синхронизировать cooldown с SkillExecutor (автоматически)
    /// Если SkillExecutor имеет cooldown для этого слота - синхронизируем
    /// </summary>
    private void SyncCooldownWithSkillExecutor()
    {
        // Находим игрока (используем умный поиск)
        GameObject player = FindLocalPlayer();
        if (player == null) return;

        SkillExecutor skillExecutor = player.GetComponentInChildren<SkillExecutor>();
        if (skillExecutor == null) return;

        // Получаем cooldown из SkillExecutor для нашего слота
        float executorCooldown = skillExecutor.GetCooldown(skillIndex);

        // Если SkillExecutor имеет cooldown, а у нас нет - синхронизируем
        if (executorCooldown > 0f && !isOnCooldown)
        {
            // Получаем скилл для определения полной длительности
            SkillConfig skillConfig = skillExecutor.GetEquippedSkill(skillIndex);
            if (skillConfig != null)
            {
                StartCooldown(skillConfig.cooldown);
                Debug.Log($"[SkillButton] 🔄 Синхронизирован cooldown из SkillExecutor: {executorCooldown:F1}с");
            }
        }
    }

    /// <summary>
    /// Настраивает Button component для обработки кликов
    /// </summary>
    private void SetupButtonComponent()
    {
        buttonComponent = GetComponent<Button>();
        if (buttonComponent == null)
        {
            buttonComponent = gameObject.AddComponent<Button>();
            Debug.Log($"[SkillButton] ✅ Добавлен Button component к слоту {skillIndex}");
        }

        // Убираем стандартные визуальные переходы (мы используем свои через OnPointerDown/Up)
        buttonComponent.transition = Selectable.Transition.None;
        buttonComponent.interactable = true;

        // Подписываемся на событие клика
        buttonComponent.onClick.RemoveAllListeners();
        buttonComponent.onClick.AddListener(OnButtonClick);

        Debug.Log($"[SkillButton] ✅ Button настроен для слота {skillIndex} → UseSkill()");
    }

    /// <summary>
    /// Обработчик клика через Button component (основной метод активации)
    /// </summary>
    public void OnButtonClick()
    {
        Debug.Log($"[SkillButton] 🔘 Button.onClick для слота {skillIndex + 1} (индекс {skillIndex})");
        UseSkill();
    }

    /// <summary>
    /// Использовать скилл (основной метод)
    /// PUBLIC чтобы можно было вызвать из Unity Inspector или других скриптов
    /// </summary>
    public void UseSkill()
    {
        // Проверяем можно ли использовать скилл
        if (isOnCooldown)
        {
            Debug.Log($"[SkillButton] Скилл {skillIndex + 1} на кулдауне! Осталось: {cooldownRemaining:F1}с");
            return;
        }

        if (currentSkill == null)
        {
            Debug.Log($"[SkillButton] Слот {skillIndex + 1} пустой!");
            return;
        }

        Debug.Log($"[SkillButton] ⚡ Активация скилла {skillIndex + 1}: {currentSkill.skillName}");

        // Находим игрока несколькими способами
        GameObject player = FindLocalPlayer();
        if (player == null)
        {
            Debug.LogWarning("[SkillButton] ❌ Игрок не найден! Проверьте что у игрока установлен Tag 'Player' или есть компонент PlayerController/AetherionPlayerController");
            return;
        }

        Debug.Log($"[SkillButton] ✅ Игрок найден: {player.name}");

        // КРИТИЧНО: Ищем SkillExecutor (новая система)
        SkillExecutor skillExecutor = player.GetComponentInChildren<SkillExecutor>();
        if (skillExecutor != null)
        {
            Debug.Log($"[SkillButton] ✅ Найден SkillExecutor, использую новую систему");

            // Получаем скилл из SkillExecutor для получения информации
            SkillConfig skillConfig = skillExecutor.GetSkill(skillIndex + 1); // GetSkill принимает slotNumber (1-5)

            if (skillConfig == null)
            {
                Debug.LogWarning($"[SkillButton] ⚠️ Нет скилла в слоте {skillIndex + 1}");
                return;
            }

            // Используем скилл напрямую через SkillExecutor (индекс 0-4)
            bool success = skillExecutor.UseSkill(skillIndex, null);

            if (success)
            {
                Debug.Log($"[SkillButton] ✅ Скилл {skillConfig.skillName} успешно применён через SkillExecutor!");

                // Запускаем cooldown UI
                StartCooldown(skillConfig.cooldown);

                // Тактильная обратная связь
                if (enableHapticFeedback)
                {
                    TriggerHapticFeedback();
                }
            }
            else
            {
                Debug.LogWarning($"[SkillButton] ❌ Не удалось использовать скилл {skillConfig.skillName} (недостаточно маны или ошибка)");
            }
            return;
        }

        // FALLBACK: Используем старую систему через SkillManager
        SkillManager skillManager = player.GetComponentInChildren<SkillManager>();
        if (skillManager == null)
        {
            Debug.LogError("[SkillButton] ❌ Ни SkillExecutor, ни SkillManager не найдены на игроке!");
            return;
        }

        Debug.Log($"[SkillButton] ⚠️ SkillExecutor не найден, использую SkillManager (старая система)");

        // Используем скилл через SkillManager
        bool fallbackSuccess = skillManager.UseSkill(skillIndex);

        if (fallbackSuccess)
        {
            Debug.Log($"[SkillButton] ✅ Скилл {currentSkill.skillName} успешно применён через SkillManager!");

            // Запускаем cooldown UI
            StartCooldown(currentSkill.cooldown);

            // Тактильная обратная связь
            if (enableHapticFeedback)
            {
                TriggerHapticFeedback();
            }
        }
        else
        {
            Debug.LogWarning($"[SkillButton] ❌ Не удалось использовать скилл {currentSkill.skillName}");
        }
    }

    /// <summary>
    /// Установить скилл в этот слот (вызывается из SkillBarUI)
    /// </summary>
    public void SetSkill(SkillData skill)
    {
        currentSkill = skill;

        if (skill != null)
        {
            Debug.Log($"[SkillButton] SetSkill: {skill.skillName} в слот {skillIndex + 1}");

            // Устанавливаем иконку
            if (iconImage != null)
            {
                iconImage.sprite = skill.icon;
                iconImage.enabled = true;
                iconImage.color = normalColor;

                if (skill.icon == null)
                {
                    Debug.LogWarning($"[SkillButton] ⚠️ У скилла '{skill.skillName}' нет иконки!");
                }
            }
        }
        else
        {
            Debug.Log($"[SkillButton] Слот {skillIndex + 1} очищен");

            // Очищаем иконку
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }

        UpdateVisuals();
    }

    /// <summary>
    /// НОВАЯ СИСТЕМА: Установить SkillConfig в этот слот
    /// </summary>
    public void SetSkillConfig(SkillConfig skillConfig)
    {
        currentSkillConfig = skillConfig;

        if (skillConfig != null)
        {
            Debug.Log($"[SkillButton] SetSkillConfig: {skillConfig.skillName} в слот {skillIndex + 1}");

            // Устанавливаем иконку
            if (iconImage != null)
            {
                iconImage.sprite = skillConfig.icon;
                iconImage.enabled = true;
                iconImage.color = normalColor;

                if (skillConfig.icon == null)
                {
                    Debug.LogWarning($"[SkillButton] ⚠️ У скилла '{skillConfig.skillName}' нет иконки!");
                }
            }
        }
        else
        {
            Debug.Log($"[SkillButton] Слот {skillIndex + 1} очищен");

            // Очищаем иконку
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }

        UpdateVisuals();
    }

    /// <summary>
    /// Запустить cooldown
    /// </summary>
    public void StartCooldown(float duration)
    {
        cooldownDuration = duration;
        cooldownRemaining = duration;
        isOnCooldown = true;

        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(true);
        }

        Debug.Log($"[SkillButton] Слот {skillIndex + 1}: кулдаун {duration}с");
    }

    /// <summary>
    /// Завершить cooldown
    /// </summary>
    private void EndCooldown()
    {
        isOnCooldown = false;
        cooldownRemaining = 0f;

        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = 0f;
        }

        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(false);
        }

        Debug.Log($"[SkillButton] Слот {skillIndex + 1}: кулдаун завершён");
    }

    /// <summary>
    /// IPointerDownHandler - визуальная обратная связь при нажатии
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (isOnCooldown || currentSkill == null)
        {
            return;
        }

        isPressed = true;
        UpdateVisuals();

        Debug.Log($"[SkillButton] 👇 OnPointerDown для слота {skillIndex + 1}");
    }

    /// <summary>
    /// IPointerUpHandler - визуальная обратная связь при отпускании
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        UpdateVisuals();

        Debug.Log($"[SkillButton] 👆 OnPointerUp для слота {skillIndex + 1}");
    }

    /// <summary>
    /// Обновить визуальное состояние кнопки
    /// </summary>
    private void UpdateVisuals()
    {
        if (iconImage == null) return;

        if (currentSkill == null)
        {
            // Слот пустой
            iconImage.enabled = false;
        }
        else if (isOnCooldown)
        {
            // На кулдауне
            iconImage.color = disabledColor;
        }
        else if (isPressed)
        {
            // Нажата
            iconImage.color = pressedColor;
        }
        else
        {
            // Нормальное состояние
            iconImage.color = normalColor;
        }
    }

    /// <summary>
    /// Найти локального игрока несколькими способами
    /// ВАЖНО: Игнорирует NetworkPlayer (других игроков)
    /// </summary>
    private GameObject FindLocalPlayer(bool silent = false)
    {
        // Проверяем кэш (если уже нашли - не ищем заново)
        if (cachedLocalPlayer != null)
        {
            return cachedLocalPlayer;
        }

        // СПОСОБ 1: По тегу "Player" (стандартный способ)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && !IsNetworkPlayer(player))
        {
            Debug.Log($"[SkillButton] 🔍 Локальный игрок найден по тегу 'Player': {player.name}");
            cachedLocalPlayer = player; // Кэшируем!
            return player;
        }

        if (!silent)
        {
            Debug.LogWarning("[SkillButton] ⚠️ Игрок с тегом 'Player' не найден или это NetworkPlayer, пробую альтернативные способы...");
        }

        // СПОСОБ 2: По компоненту AetherionPlayerController (игнорируем NetworkPlayer)
        AetherionPlayerController[] aetherionControllers = FindObjectsOfType<AetherionPlayerController>();
        foreach (var controller in aetherionControllers)
        {
            if (!IsNetworkPlayer(controller.gameObject))
            {
                Debug.Log($"[SkillButton] 🔍 Локальный игрок найден через AetherionPlayerController: {controller.gameObject.name}");
                cachedLocalPlayer = controller.gameObject; // Кэшируем!
                return controller.gameObject;
            }
        }

        // СПОСОБ 3: По компоненту PlayerController (игнорируем NetworkPlayer)
        PlayerController[] playerControllers = FindObjectsOfType<PlayerController>();
        foreach (var controller in playerControllers)
        {
            if (!IsNetworkPlayer(controller.gameObject))
            {
                Debug.Log($"[SkillButton] 🔍 Локальный игрок найден через PlayerController: {controller.gameObject.name}");
                cachedLocalPlayer = controller.gameObject; // Кэшируем!
                return controller.gameObject;
            }
        }

        // СПОСОБ 4: По компоненту MixamoPlayerController (игнорируем NetworkPlayer)
        MixamoPlayerController[] mixamoControllers = FindObjectsOfType<MixamoPlayerController>();
        foreach (var controller in mixamoControllers)
        {
            if (!IsNetworkPlayer(controller.gameObject))
            {
                Debug.Log($"[SkillButton] 🔍 Локальный игрок найден через MixamoPlayerController: {controller.gameObject.name}");
                cachedLocalPlayer = controller.gameObject; // Кэшируем!
                return controller.gameObject;
            }
        }

        // СПОСОБ 5: По компоненту SkillExecutor (игнорируем NetworkPlayer)
        SkillExecutor[] executors = FindObjectsOfType<SkillExecutor>();
        foreach (var executor in executors)
        {
            if (!IsNetworkPlayer(executor.gameObject))
            {
                // Поднимаемся к родительскому объекту (обычно Player)
                Transform current = executor.transform;
                while (current.parent != null)
                {
                    current = current.parent;
                }
                Debug.Log($"[SkillButton] 🔍 Локальный игрок найден через SkillExecutor (root): {current.gameObject.name}");
                cachedLocalPlayer = current.gameObject; // Кэшируем!
                return current.gameObject;
            }
        }

        // НЕ НАШЛИ
        if (!silent)
        {
            Debug.LogWarning("[SkillButton] ⚠️ Локальный игрок ещё не загрузился в игру. Ожидание...");
        }
        return null;
    }

    /// <summary>
    /// Проверить является ли объект NetworkPlayer (чужим игроком)
    /// </summary>
    private bool IsNetworkPlayer(GameObject obj)
    {
        // ВАЖНО: Сначала проверяем что это ЛОКАЛЬНЫЙ игрок (у него есть SkillExecutor!)
        // Если есть SkillExecutor или SkillManager = это НАШ игрок, не NetworkPlayer
        if (obj.GetComponentInChildren<SkillExecutor>() != null ||
            obj.GetComponentInChildren<SkillManager>() != null)
        {
            Debug.Log($"[SkillButton] ✅ Это локальный игрок (есть SkillExecutor/Manager): {obj.name}");
            return false; // НЕ NetworkPlayer!
        }

        // Проверка 1: По имени
        if (obj.name.StartsWith("NetworkPlayer"))
        {
            Debug.Log($"[SkillButton] ⚠️ Пропускаю NetworkPlayer по имени: {obj.name}");
            return true;
        }

        // Проверка 2: По компоненту NetworkPlayer
        NetworkPlayer networkPlayerComp = obj.GetComponent<NetworkPlayer>();
        if (networkPlayerComp != null)
        {
            Debug.Log($"[SkillButton] ⚠️ Пропускаю NetworkPlayer по компоненту: {obj.name}");
            return true;
        }

        // Проверка 3: В родительской иерархии
        NetworkPlayer parentNetworkPlayer = obj.GetComponentInParent<NetworkPlayer>();
        if (parentNetworkPlayer != null)
        {
            Debug.Log($"[SkillButton] ⚠️ Пропускаю NetworkPlayer в иерархии: {obj.name}");
            return true;
        }

        // Если ничего не нашли = это может быть локальный игрок
        return false;
    }

    /// <summary>
    /// Тактильная обратная связь (вибрация)
    /// </summary>
    private void TriggerHapticFeedback()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// Получить текущий скилл
    /// </summary>
    public SkillData GetSkill()
    {
        return currentSkill;
    }

    /// <summary>
    /// Проверить доступна ли кнопка
    /// </summary>
    public bool IsAvailable
    {
        get { return !isOnCooldown && currentSkill != null; }
    }

    /// <summary>
    /// Проверить на кулдауне ли скилл
    /// </summary>
    public bool IsOnCooldown
    {
        get { return isOnCooldown; }
    }

    /// <summary>
    /// Получить оставшееся время кулдауна
    /// </summary>
    public float GetCooldownRemaining()
    {
        return cooldownRemaining;
    }

    /// <summary>
    /// Принудительно сбросить cooldown
    /// </summary>
    public void ResetCooldown()
    {
        EndCooldown();
    }

    // Дебаг визуализация в редакторе
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (isPressed)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }

        if (isOnCooldown)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.2f);
        }
    }
}
