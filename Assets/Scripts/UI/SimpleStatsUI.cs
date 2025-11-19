using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ПРОСТОЙ UI для прокачки характеристик
/// Работает БЕЗ настройки prefab - создаётся программно
/// Нажмите P чтобы открыть/закрыть
/// </summary>
public class SimpleStatsUI : MonoBehaviour
{
    [Header("UI Prefab (Optional)")]
    [Tooltip("Если заполнен - используется твой кастомный prefab. Если пусто - создаётся программно")]
    [SerializeField] private GameObject statsPanelPrefab;

    private GameObject panel;
    private Text infoText;
    private Text statsText;
    private bool isVisible = false;
    private Font arialFont; // Шрифт для всех Text компонентов
    private bool isInitialized = false; // Флаг инициализации

    private LevelingSystem levelingSystem;
    private CharacterStats characterStats;

    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.P;

    void Start()
    {
        // ЛЕНИВАЯ ИНИЦИАЛИЗАЦИЯ: Не создаём UI в Start(), а ждём первого вызова Show()
        // Это решает проблему когда StatsButtonController находит SimpleStatsUI ДО вызова Start()
        Debug.Log("[SimpleStatsUI] ==================== START ====================");
        Debug.Log("[SimpleStatsUI] Start() вызван, но UI будет создан лениво при первом Show()");
    }

    void Update()
    {
        // Переключение видимости по нажатию клавиши
        if (Input.GetKeyDown(toggleKey))
        {
            Debug.Log($"[SimpleStatsUI] ⌨️ Нажата клавиша {toggleKey} - переключаем панель");
            Toggle();
        }

        // Обновляем UI если видим
        if (isVisible && isInitialized && levelingSystem != null && characterStats != null)
        {
            UpdateUI();
        }
    }

    /// <summary>
    /// Отложенный поиск систем игрока (ждём инициализации)
    /// </summary>
    private System.Collections.IEnumerator DelayedFindPlayerSystems()
    {
        Debug.Log("[SimpleStatsUI] 🔍 Начинаем отложенный поиск систем игрока...");

        int attempts = 0;
        int maxAttempts = 10; // 10 попыток по 0.5 сек = 5 секунд максимум

        while (attempts < maxAttempts && (levelingSystem == null || characterStats == null))
        {
            attempts++;
            Debug.Log($"[SimpleStatsUI] 🔍 Попытка {attempts}/{maxAttempts}...");

            FindPlayerSystems();

            if (levelingSystem != null && characterStats != null)
            {
                Debug.Log("[SimpleStatsUI] ✅ Системы найдены! UI готов к работе.");
                yield break;
            }

            // Ждём 0.5 секунды перед следующей попыткой
            yield return new WaitForSeconds(0.5f);
        }

        if (levelingSystem == null || characterStats == null)
        {
            Debug.LogError("[SimpleStatsUI] ❌ Не удалось найти системы игрока после 5 секунд ожидания!");
            Debug.LogError("[SimpleStatsUI] ❌ UI для прокачки НЕ БУДЕТ РАБОТАТЬ!");
        }
    }

    /// <summary>
    /// Найти системы игрока
    /// </summary>
    private void FindPlayerSystems()
    {
        Debug.Log("[SimpleStatsUI] === FindPlayerSystems() ===");

        UnsubscribeFromSystems();

        // Ищем локального игрока
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("[SimpleStatsUI] Player tag не найден, ищем через LocalPlayerEntity...");

            // Пытаемся найти через LocalPlayerEntity (работает в редакторе и в build)
            #if UNITY_EDITOR
            LocalPlayerEntity localPlayer = GameObject.FindObjectOfType<LocalPlayerEntity>();
            #else
            LocalPlayerEntity localPlayer = FindFirstObjectByType<LocalPlayerEntity>();
            #endif

            if (localPlayer != null)
            {
                player = localPlayer.gameObject;
                Debug.Log($"[SimpleStatsUI] ✅ Найден через LocalPlayerEntity: {player.name}");
            }
            else
            {
                Debug.LogWarning("[SimpleStatsUI] ⚠️ LocalPlayerEntity не найден! Ищем все CharacterStats...");

                // Если LocalPlayerEntity нет - ищем CharacterStats напрямую
                #if UNITY_EDITOR
                CharacterStats[] allStats = GameObject.FindObjectsOfType<CharacterStats>();
                #else
                CharacterStats[] allStats = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
                #endif

                Debug.Log($"[SimpleStatsUI] Найдено CharacterStats компонентов: {allStats.Length}");

                // Берем первый найденный (если нет LocalPlayerEntity - значит одиночная игра)
                if (allStats.Length > 0)
                {
                    player = allStats[0].gameObject;
                    Debug.Log($"[SimpleStatsUI] ✅ Используем первый найденный CharacterStats: {player.name}");
                }
            }
        }
        else
        {
            Debug.Log($"[SimpleStatsUI] ✅ Игрок найден по тегу Player: {player.name}");
        }

        if (player != null)
        {
            // Логируем иерархию для отладки
            Debug.Log($"[SimpleStatsUI] 🔍 Проверяем иерархию игрока: {player.name}");
            LogGameObjectHierarchy(player, 0);

            // Ищем LevelingSystem - он добавляется на Model (дочерний объект)
            levelingSystem = player.GetComponentInChildren<LevelingSystem>();
            characterStats = player.GetComponentInChildren<CharacterStats>();

            // Если не нашли в детях - ищем везде
            if (levelingSystem == null)
            {
                Debug.LogWarning("[SimpleStatsUI] LevelingSystem не найден в детях, ищем глобально...");

                #if UNITY_EDITOR
                levelingSystem = GameObject.FindObjectOfType<LevelingSystem>();
                #else
                levelingSystem = FindFirstObjectByType<LevelingSystem>();
                #endif

                if (levelingSystem != null)
                {
                    Debug.Log($"[SimpleStatsUI] ✅ Найден глобально на: {levelingSystem.gameObject.name}");
                }
            }

            if (characterStats == null)
            {
                Debug.LogWarning("[SimpleStatsUI] CharacterStats не найден в детях, ищем глобально...");

                #if UNITY_EDITOR
                characterStats = GameObject.FindObjectOfType<CharacterStats>();
                #else
                characterStats = FindFirstObjectByType<CharacterStats>();
                #endif

                if (characterStats != null)
                {
                    Debug.Log($"[SimpleStatsUI] ✅ Найден глобально на: {characterStats.gameObject.name}");
                }
            }

            if (levelingSystem != null && characterStats != null)
            {
                Debug.Log($"[SimpleStatsUI] ✅ Найдены системы!");
                Debug.Log($"[SimpleStatsUI]   - LevelingSystem на: {levelingSystem.gameObject.name} (Level: {levelingSystem.CurrentLevel}, XP: {levelingSystem.CurrentExperience}, Points: {levelingSystem.AvailableStatPoints})");
                Debug.Log($"[SimpleStatsUI]   - CharacterStats на: {characterStats.gameObject.name} (Class: {characterStats.ClassName}, HP: {characterStats.MaxHealth})");
                SubscribeToSystems();
            }
            else
            {
                if (levelingSystem == null) Debug.LogWarning("[SimpleStatsUI] ⚠️ LevelingSystem НЕ НАЙДЕН (пока)");
                if (characterStats == null) Debug.LogWarning("[SimpleStatsUI] ⚠️ CharacterStats НЕ НАЙДЕН (пока)");
            }
        }
        else
        {
            Debug.LogWarning("[SimpleStatsUI] ⚠️ Локальный игрок не найден (пока)");
        }
    }

    /// <summary>
    /// Логировать иерархию GameObject для отладки
    /// </summary>
    private void LogGameObjectHierarchy(GameObject obj, int depth)
    {
        if (obj == null || depth > 3) return; // Максимум 3 уровня вложенности

        string indent = new string(' ', depth * 2);
        var components = obj.GetComponents<Component>();
        string componentsList = string.Join(", ", System.Array.ConvertAll(components, c => c.GetType().Name));

        Debug.Log($"[SimpleStatsUI] {indent}└─ {obj.name} [{componentsList}]");

        // Логируем дочерние объекты
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            LogGameObjectHierarchy(obj.transform.GetChild(i).gameObject, depth + 1);
        }
    }

    /// <summary>
    /// Загрузить UI из кастомного prefab
    /// </summary>
    private void LoadFromPrefab()
    {
        Debug.Log("[SimpleStatsUI] 🎨 LoadFromPrefab() начало выполнения...");

        // Инстанцируем prefab
        GameObject instantiatedPrefab = Instantiate(statsPanelPrefab);
        DontDestroyOnLoad(instantiatedPrefab); // Сохраняем между сценами

        // Ищем Canvas в prefab (может быть как корневой объект, так и родитель)
        Canvas canvas = instantiatedPrefab.GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[SimpleStatsUI] ❌ Canvas не найден в prefab! Prefab должен содержать Canvas.");
            Destroy(instantiatedPrefab);
            throw new System.Exception("Canvas not found in prefab");
        }

        Debug.Log($"[SimpleStatsUI] ✅ Canvas найден в prefab: {canvas.gameObject.name}");

        // Ищем панель (SimpleStatsPanel или объект с Image компонентом)
        panel = canvas.transform.Find("SimpleStatsPanel")?.gameObject;
        if (panel == null)
        {
            // Если не нашли по имени - берём первый дочерний с Image
            Image[] images = canvas.GetComponentsInChildren<Image>(true);
            if (images.Length > 0)
            {
                panel = images[0].gameObject;
                Debug.Log($"[SimpleStatsUI] ℹ️ SimpleStatsPanel не найден по имени, используем первый Image: {panel.name}");
            }
        }

        if (panel == null)
        {
            Debug.LogError("[SimpleStatsUI] ❌ Панель (SimpleStatsPanel) не найдена в prefab!");
            Destroy(instantiatedPrefab);
            throw new System.Exception("Panel not found in prefab");
        }

        Debug.Log($"[SimpleStatsUI] ✅ Панель найдена: {panel.name}");

        // Ищем Text компоненты по имени
        Transform infoTransform = panel.transform.Find("InfoText");
        Transform statsTransform = panel.transform.Find("StatsText");

        if (infoTransform != null)
        {
            infoText = infoTransform.GetComponent<Text>();
            Debug.Log($"[SimpleStatsUI] ✅ InfoText найден: {infoText != null}");
        }
        else
        {
            Debug.LogWarning("[SimpleStatsUI] ⚠️ InfoText не найден в prefab по имени!");
        }

        if (statsTransform != null)
        {
            statsText = statsTransform.GetComponent<Text>();
            Debug.Log($"[SimpleStatsUI] ✅ StatsText найден: {statsText != null}");
        }
        else
        {
            Debug.LogWarning("[SimpleStatsUI] ⚠️ StatsText не найден в prefab по имени!");
        }

        // Если Text компоненты не найдены по имени - ищем все Text в панели
        if (infoText == null || statsText == null)
        {
            Debug.LogWarning("[SimpleStatsUI] ⚠️ Некоторые Text компоненты не найдены, ищем все Text в панели...");
            Text[] allTexts = panel.GetComponentsInChildren<Text>(true);

            if (allTexts.Length >= 2)
            {
                if (infoText == null) infoText = allTexts[0];
                if (statsText == null) statsText = allTexts[1];
                Debug.Log($"[SimpleStatsUI] ℹ️ Используем первые 2 Text компонента: infoText={infoText.name}, statsText={statsText.name}");
            }
        }

        // Финальная проверка
        if (infoText == null || statsText == null)
        {
            Debug.LogError($"[SimpleStatsUI] ❌ Text компоненты не найдены! infoText={infoText != null}, statsText={statsText != null}");
            Debug.LogError("[SimpleStatsUI] 💡 Убедись что в prefab есть объекты 'InfoText' и 'StatsText' с компонентом Text!");
            Destroy(instantiatedPrefab);
            throw new System.Exception("Text components not found in prefab");
        }

        // КРИТИЧЕСКИ ВАЖНО: Привязываем кнопки + к методам повышения статов
        ConnectStatButtons();

        // КРИТИЧЕСКИ ВАЖНО: Привязываем кнопку закрытия (X)
        Button closeButton = panel.GetComponentInChildren<Button>();
        if (closeButton != null && closeButton.name.Contains("Close"))
        {
            closeButton.onClick.RemoveAllListeners(); // Очистить старые listeners
            closeButton.onClick.AddListener(Hide);
            Debug.Log("[SimpleStatsUI] ✅ Кнопка закрытия (X) привязана к Hide()");
        }

        Debug.Log("[SimpleStatsUI] ✅ Prefab загружен успешно!");
    }

    /// <summary>
    /// Создать UI программно
    /// </summary>
    private void CreateUI()
    {
        Debug.Log("[SimpleStatsUI] 🔧 CreateUI() начало выполнения...");

        // ВАЖНО: Создаём СОБСТВЕННЫЙ Canvas для этой панели (не используем существующий!)
        GameObject canvasObj = new GameObject("StatsCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Поверх экрана!
        canvas.sortingOrder = 100; // Поверх всех других UI

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();

        // КРИТИЧЕСКИ ВАЖНО: Canvas должен быть активен!
        canvasObj.SetActive(true);
        DontDestroyOnLoad(canvasObj); // Сохраняем Canvas между сценами

        Debug.Log($"[SimpleStatsUI] ✅ Создан Canvas в режиме ScreenSpaceOverlay (active={canvasObj.activeSelf}, renderMode={canvas.renderMode})");

        // Проверяем есть ли EventSystem (нужен для кнопок)
        #if UNITY_EDITOR
        UnityEngine.EventSystems.EventSystem eventSystem = GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        #else
        UnityEngine.EventSystems.EventSystem eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        #endif

        if (eventSystem == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[SimpleStatsUI] ✅ Создан EventSystem для обработки кнопок");
        }
        else
        {
            Debug.Log("[SimpleStatsUI] ✅ EventSystem уже существует");
        }

        // Создаём панель
        panel = new GameObject("SimpleStatsPanel");
        panel.transform.SetParent(canvas.transform, false);

        // Добавляем Image (фон)
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f); // Тёмно-серый полупрозрачный (было чёрный - изменено для лучшей видимости)

        Debug.Log($"[SimpleStatsUI] ✅ Panel создан: parent={panel.transform.parent.name}, active={panel.activeSelf}");

        // RectTransform панели
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(600f, 700f);
        panelRect.anchoredPosition = Vector2.zero;

        // КРИТИЧЕСКИ ВАЖНО: Получаем шрифт один раз для всех Text компонентов
        // Unity 2023+ не поддерживает Arial.ttf, используем LegacyRuntime.ttf
        arialFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (arialFont == null)
        {
            Debug.LogError("[SimpleStatsUI] ❌ LegacyRuntime.ttf не найден! Пробуем Arial.ttf...");
            arialFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        if (arialFont == null)
        {
            Debug.LogError("[SimpleStatsUI] ❌ Не удалось загрузить встроенный шрифт!");
            throw new System.Exception("Font not found");
        }

        Debug.Log($"[SimpleStatsUI] ✅ Шрифт загружен: {arialFont.name}");

        // Создаём заголовок
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "CHARACTER STATS";
        titleText.font = arialFont;
        titleText.material = arialFont.material; // Используем материал шрифта!

        titleText.fontSize = 24;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.yellow;

        Debug.Log($"[SimpleStatsUI] Title Text: font={titleText.font != null}, material={titleText.material != null}, text='{titleText.text}', color={titleText.color}");

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 50f);
        titleRect.anchoredPosition = new Vector2(0f, 0f);

        // Создаём кнопку закрытия (X) в правом верхнем углу
        GameObject closeButtonObj = new GameObject("CloseButton");
        closeButtonObj.transform.SetParent(panel.transform, false);

        Image closeButtonImage = closeButtonObj.AddComponent<Image>();
        closeButtonImage.color = new Color(0.8f, 0.2f, 0.2f, 0.9f); // Красный

        Button closeButton = closeButtonObj.AddComponent<Button>();
        closeButton.targetGraphic = closeButtonImage;
        closeButton.onClick.AddListener(() => Hide());

        RectTransform closeButtonRect = closeButtonObj.GetComponent<RectTransform>();
        closeButtonRect.anchorMin = new Vector2(1f, 1f);
        closeButtonRect.anchorMax = new Vector2(1f, 1f);
        closeButtonRect.pivot = new Vector2(1f, 1f);
        closeButtonRect.sizeDelta = new Vector2(40f, 40f);
        closeButtonRect.anchoredPosition = new Vector2(-5f, -5f);

        // Текст кнопки закрытия
        GameObject closeTextObj = new GameObject("Text");
        closeTextObj.transform.SetParent(closeButtonObj.transform, false);
        Text closeText = closeTextObj.AddComponent<Text>();
        closeText.text = "X";
        closeText.font = arialFont;
        closeText.material = arialFont.material;

        closeText.fontSize = 24;
        closeText.fontStyle = FontStyle.Bold;
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.color = Color.white;

        RectTransform closeTextRect = closeTextObj.GetComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.sizeDelta = Vector2.zero;
        closeTextRect.anchoredPosition = Vector2.zero;

        // Создаём текст с информацией (уровень, опыт, очки)
        GameObject infoObj = new GameObject("InfoText");
        infoObj.transform.SetParent(panel.transform, false);
        infoText = infoObj.AddComponent<Text>();
        infoText.font = arialFont;
        infoText.material = arialFont.material;

        infoText.fontSize = 20;
        infoText.alignment = TextAnchor.UpperLeft;
        infoText.color = Color.white; // Белый текст
        infoText.text = "Loading..."; // Начальный текст чтобы было видно что UI работает

        Debug.Log($"[SimpleStatsUI] Info Text: font={infoText.font != null}, material={infoText.material != null}, text='{infoText.text}', color={infoText.color}");

        RectTransform infoRect = infoObj.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0f, 0.7f);
        infoRect.anchorMax = new Vector2(1f, 1f);
        infoRect.pivot = new Vector2(0.5f, 1f);
        infoRect.anchoredPosition = new Vector2(0f, -60f);
        infoRect.offsetMin = new Vector2(20f, infoRect.offsetMin.y);
        infoRect.offsetMax = new Vector2(-20f, infoRect.offsetMax.y);

        // Создаём текст со статами
        GameObject statsObj = new GameObject("StatsText");
        statsObj.transform.SetParent(panel.transform, false);
        statsText = statsObj.AddComponent<Text>();
        statsText.font = arialFont;
        statsText.material = arialFont.material;

        statsText.fontSize = 18;
        statsText.alignment = TextAnchor.UpperLeft;
        statsText.color = Color.white; // Белый текст
        statsText.text = "Stats loading..."; // Начальный текст

        Debug.Log($"[SimpleStatsUI] Stats Text: font={statsText.font != null}, material={statsText.material != null}, text='{statsText.text}', color={statsText.color}");

        RectTransform statsRect = statsObj.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0f, 0f);
        statsRect.anchorMax = new Vector2(1f, 0.7f);
        statsRect.pivot = new Vector2(0f, 1f);
        statsRect.anchoredPosition = new Vector2(20f, 0f);
        statsRect.offsetMin = new Vector2(20f, 20f);
        statsRect.offsetMax = new Vector2(-20f, statsRect.offsetMax.y);

        // Создаём кнопки для прокачки
        CreateStatButtons();

        Debug.Log("[SimpleStatsUI] ✅ UI создан программно");
    }

    /// <summary>
    /// Создать кнопки прокачки
    /// </summary>
    private void CreateStatButtons()
    {
        string[] statNames = { "Strength", "Perception", "Endurance", "Wisdom", "Intelligence", "Agility", "Luck" };
        string[] statKeys = { "strength", "perception", "endurance", "wisdom", "intelligence", "agility", "luck" };

        for (int i = 0; i < statNames.Length; i++)
        {
            int index = i; // Копия для замыкания
            string statName = statNames[i];
            string statKey = statKeys[i];

            GameObject buttonObj = new GameObject($"Button_{statName}");
            buttonObj.transform.SetParent(panel.transform, false);

            // Image для кнопки
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 1f, 0.8f);

            // Button компонент
            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            // Обработчик нажатия
            button.onClick.AddListener(() => OnStatButtonClick(statKey));

            // Позиция кнопки (справа от текста статов)
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.sizeDelta = new Vector2(50f, 30f);
            buttonRect.anchoredPosition = new Vector2(-20f, -60f - (i * 80f));

            // Текст на кнопке
            GameObject buttonTextObj = new GameObject("Text");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            Text buttonText = buttonTextObj.AddComponent<Text>();
            buttonText.text = "+";
            buttonText.font = arialFont;
            buttonText.material = arialFont.material;

            buttonText.fontSize = 24;
            buttonText.fontStyle = FontStyle.Bold;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;

            RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.sizeDelta = Vector2.zero;
        }
    }

    /// <summary>
    /// Привязать кнопки + к методам повышения статов (для prefab)
    /// </summary>
    private void ConnectStatButtons()
    {
        Debug.Log("[SimpleStatsUI] 🔌 Привязываем кнопки статов...");

        // Ищем все кнопки в панели
        Button[] allButtons = panel.GetComponentsInChildren<Button>(true);
        Debug.Log($"[SimpleStatsUI] Найдено {allButtons.Length} кнопок в prefab");

        // Список статов в правильном порядке (как в CreateUI)
        string[] statNames = { "strength", "perception", "endurance", "wisdom", "intelligence", "agility", "luck" };

        int connectedButtons = 0;
        foreach (Button btn in allButtons)
        {
            // Пропускаем кнопку закрытия
            if (btn.name.Contains("Close")) continue;

            // СПОСОБ 1: Кнопка названа по имени стата (например "StrengthButton")
            string statName = null;
            foreach (string stat in statNames)
            {
                if (btn.name.ToLower().Contains(stat.ToLower()))
                {
                    statName = stat;
                    break;
                }
            }

            // СПОСОБ 2: Кнопка просто называется "Button" или "+" - привязываем по индексу
            if (statName == null && connectedButtons < statNames.Length)
            {
                statName = statNames[connectedButtons];
                Debug.Log($"[SimpleStatsUI] ℹ️ Кнопка {btn.name} не имеет имени стата, используем индекс → {statName}");
            }

            if (statName != null)
            {
                // Очищаем старые listeners и добавляем новый
                btn.onClick.RemoveAllListeners();
                string capturedStatName = statName; // ВАЖНО: Capture для lambda
                btn.onClick.AddListener(() => OnStatButtonClick(capturedStatName));
                connectedButtons++;
                Debug.Log($"[SimpleStatsUI] ✅ Кнопка '{btn.name}' привязана к стату '{statName}'");
            }
        }

        if (connectedButtons == 0)
        {
            Debug.LogWarning("[SimpleStatsUI] ⚠️ Не удалось привязать ни одной кнопки! Убедись что в prefab есть кнопки с компонентом Button.");
        }
        else
        {
            Debug.Log($"[SimpleStatsUI] ✅ Привязано {connectedButtons} кнопок статов");
        }
    }

    /// <summary>
    /// Обработка нажатия на кнопку прокачки
    /// </summary>
    private void OnStatButtonClick(string statName)
    {
        if (levelingSystem == null || characterStats == null)
        {
            Debug.LogError("[SimpleStatsUI] ❌ Системы не найдены!");
            return;
        }

        if (levelingSystem.AvailableStatPoints <= 0)
        {
            Debug.LogWarning("[SimpleStatsUI] ⚠️ Нет свободных очков!");
            return;
        }

        bool success = levelingSystem.SpendStatPoint(statName);
        if (success)
        {
            Debug.Log($"[SimpleStatsUI] ✅ Прокачана характеристика: {statName}");
            UpdateUI();
        }
    }

    /// <summary>
    /// Обновить UI
    /// </summary>
    private void UpdateUI()
    {
        if (levelingSystem == null || characterStats == null)
        {
            Debug.LogWarning("[SimpleStatsUI] ⚠️ UpdateUI: Systems are null!");
            return;
        }

        if (infoText == null || statsText == null)
        {
            Debug.LogError("[SimpleStatsUI] ❌ UpdateUI: Text components are NULL! UI was not created properly!");
            return;
        }

        // Информация о уровне и очках
        infoText.text = $"<b>CLASS:</b> {characterStats.ClassName}\n" +
                       $"<b>LEVEL:</b> {levelingSystem.CurrentLevel} / {levelingSystem.MaxLevel}\n" +
                       $"<b>EXPERIENCE:</b> {levelingSystem.CurrentExperience} / {levelingSystem.GetExperienceForNextLevel()}\n" +
                       $"<b>AVAILABLE POINTS:</b> <color=yellow>{levelingSystem.AvailableStatPoints}</color>";

        // SPECIAL характеристики
        statsText.text = "<b>=== SPECIAL STATS ===</b>\n\n" +
                        $"<b>Strength:</b> {characterStats.strength} / 10\n" +
                        $"  → Physical Damage\n\n" +
                        $"<b>Perception:</b> {characterStats.perception} / 10\n" +
                        $"  → Vision: {characterStats.VisionRadius:F0}m\n\n" +
                        $"<b>Endurance:</b> {characterStats.endurance} / 10\n" +
                        $"  → HP: {characterStats.MaxHealth:F0}\n\n" +
                        $"<b>Wisdom:</b> {characterStats.wisdom} / 10\n" +
                        $"  → Mana: {characterStats.MaxMana:F0}\n\n" +
                        $"<b>Intelligence:</b> {characterStats.intelligence} / 10\n" +
                        $"  → Magical Damage\n\n" +
                        $"<b>Agility:</b> {characterStats.agility} / 10\n" +
                        $"  → AP: {characterStats.MaxActionPoints:F0}\n\n" +
                        $"<b>Luck:</b> {characterStats.luck} / 10\n" +
                        $"  → Crit: {characterStats.CritChance:F1}%";
    }

    /// <summary>
    /// Показать панель
    /// </summary>
    public void Show()
    {
        // ЛЕНИВАЯ ИНИЦИАЛИЗАЦИЯ: Создаём UI при первом вызове Show()
        if (!isInitialized)
        {
            Debug.Log("[SimpleStatsUI] ⚠️ UI не инициализирован, выполняем ленивую инициализацию...");
            try
            {
                // ВАЖНО: Проверяем есть ли кастомный prefab
                if (statsPanelPrefab != null)
                {
                    Debug.Log("[SimpleStatsUI] 🎨 Используем КАСТОМНЫЙ prefab вместо программного создания!");
                    LoadFromPrefab();
                }
                else
                {
                    Debug.Log("[SimpleStatsUI] 🔧 Prefab не задан - создаём UI программно");
                    CreateUI();
                }

                Debug.Log($"[SimpleStatsUI] ✅ Ленивая инициализация успешна: panel={panel != null}, infoText={infoText != null}, statsText={statsText != null}");

                if (panel != null)
                {
                    panel.SetActive(false); // Сначала скрываем
                    isInitialized = true;
                    StartCoroutine(DelayedFindPlayerSystems());
                }
                else
                {
                    Debug.LogError("[SimpleStatsUI] ❌ Ленивая инициализация провалилась: panel is null!");
                    return;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SimpleStatsUI] ❌ Ленивая инициализация провалилась: {e.Message}\n{e.StackTrace}");
                return;
            }
        }

        Debug.Log($"[SimpleStatsUI] Show() - panel={panel != null}, isInitialized={isInitialized}");

        if (panel != null)
        {
            panel.SetActive(true);
            isVisible = true;
            Debug.Log("[SimpleStatsUI] ✅ Панель показана");

            // КРИТИЧЕСКИ ВАЖНО: Попытаться найти системы еще раз если их нет
            if (levelingSystem == null || characterStats == null)
            {
                Debug.LogWarning("[SimpleStatsUI] ⚠️ Системы не найдены, пытаемся найти еще раз...");
                FindPlayerSystems();
            }

            if (levelingSystem != null && characterStats != null)
            {
                UpdateUI();
                Debug.Log($"[SimpleStatsUI] ✅ UI обновлен: infoText='{infoText.text.Substring(0, System.Math.Min(50, infoText.text.Length))}'");
            }
            else
            {
                Debug.LogError($"[SimpleStatsUI] ❌ Системы НЕ НАЙДЕНЫ после Show(): levelingSystem={levelingSystem != null}, characterStats={characterStats != null}");

                // Показываем сообщение об ошибке в UI
                if (infoText != null)
                {
                    infoText.text = "<color=red><b>ERROR: Systems not found!</b></color>\n\n" +
                                   $"LevelingSystem: {(levelingSystem != null ? "✅" : "❌")}\n" +
                                   $"CharacterStats: {(characterStats != null ? "✅" : "❌")}\n\n" +
                                   "Make sure LocalPlayerEntity has these components!";
                }

                if (statsText != null)
                {
                    statsText.text = "<color=yellow>Press P to retry...</color>";
                }
            }
        }
        else
        {
            Debug.LogError("[SimpleStatsUI] ❌ Panel is NULL! Не могу показать панель!");
        }
    }

    private void SubscribeToSystems()
    {
        if (characterStats != null)
        {
            characterStats.OnStatsChanged -= OnCharacterStatsChanged;
            characterStats.OnStatsChanged += OnCharacterStatsChanged;
        }

        if (levelingSystem != null)
        {
            levelingSystem.OnExperienceGained -= OnLevelingExperienceGained;
            levelingSystem.OnLevelUp -= OnLevelingLevelUp;
            levelingSystem.OnStatPointsChanged -= OnLevelingStatPointsChanged;

            levelingSystem.OnExperienceGained += OnLevelingExperienceGained;
            levelingSystem.OnLevelUp += OnLevelingLevelUp;
            levelingSystem.OnStatPointsChanged += OnLevelingStatPointsChanged;
        }
    }

    private void UnsubscribeFromSystems()
    {
        if (characterStats != null)
        {
            characterStats.OnStatsChanged -= OnCharacterStatsChanged;
        }

        if (levelingSystem != null)
        {
            levelingSystem.OnExperienceGained -= OnLevelingExperienceGained;
            levelingSystem.OnLevelUp -= OnLevelingLevelUp;
            levelingSystem.OnStatPointsChanged -= OnLevelingStatPointsChanged;
        }
    }

    private void OnCharacterStatsChanged()
    {
        TryUpdateUIFromEvents();
    }

    private void OnLevelingExperienceGained(int _)
    {
        TryUpdateUIFromEvents();
    }

    private void OnLevelingLevelUp(int _)
    {
        TryUpdateUIFromEvents();
    }

    private void OnLevelingStatPointsChanged(int _)
    {
        TryUpdateUIFromEvents();
    }

    private void TryUpdateUIFromEvents()
    {
        if (!isInitialized || infoText == null || statsText == null)
            return;

        if (isVisible)
        {
            UpdateUI();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromSystems();
    }

    /// <summary>
    /// Скрыть панель
    /// </summary>
    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
            isVisible = false;
        }
    }

    /// <summary>
    /// Переключить видимость
    /// </summary>
    public void Toggle()
    {
        if (isVisible)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }
}
