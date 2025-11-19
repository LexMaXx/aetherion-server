using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Простой Dummy враг для тестирования атак и скиллов
/// Визуализирует получение урона и имеет простую HP систему
/// Наследуется от TargetableEntity для полной совместимости с системой таргетирования
/// </summary>
public class DummyEnemy : TargetableEntity
{
    [Header("Dummy Settings")]
    [SerializeField] private bool autoRespawn = true;
    [SerializeField] private float respawnDelay = 3f;

    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] private Color damageColor = Color.white;
    [SerializeField] private float damageFlashDuration = 0.1f;

    [Header("UI")]
    [SerializeField] private GameObject hpBarPrefab; // Опционально: префаб HP бара
    [SerializeField] private Vector3 hpBarOffset = new Vector3(0f, 2.5f, 0f);

    private Renderer visualRenderer;
    private Material visualMaterial;
    private float damageFlashTimer = 0f;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // UI элементы
    private GameObject hpBarInstance;
    private Slider hpSlider;
    private Text hpText;

    protected override void Start()
    {
        // ВАЖНО: Сначала вызываем базовый Start() для инициализации TargetableEntity
        base.Start();

        // Устанавливаем имя и фракцию
        entityName = gameObject.name;
        faction = Faction.Enemy;

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // Находим Renderer
        visualRenderer = GetComponentInChildren<Renderer>();
        if (visualRenderer != null)
        {
            visualMaterial = visualRenderer.material;
            visualMaterial.color = normalColor;
        }

        // Создаём HP bar
        CreateHealthBar();

        // ВАЖНО: Добавляем Enemy компонент для FogOfWar (если нужен)
        // Теперь DummyEnemy наследуется от TargetableEntity, но FogOfWar требует Enemy
        if (GetComponent<Enemy>() == null)
        {
            Enemy enemyComp = gameObject.AddComponent<Enemy>();
            Debug.Log($"[DummyEnemy] ✅ Добавлен Enemy компонент для FogOfWar на {gameObject.name}");
        }

        // Устанавливаем тег Enemy для TargetSystem
        if (!gameObject.CompareTag("Enemy"))
        {
            try
            {
                gameObject.tag = "Enemy";
                Debug.Log($"[DummyEnemy] ✅ Установлен тег 'Enemy' на {gameObject.name}");
            }
            catch (UnityException)
            {
                Debug.LogWarning($"[DummyEnemy] Тег 'Enemy' не существует в Unity! Создайте его в Tags & Layers.");
            }
        }

        // Добавляем EffectManager для получения эффектов (стан, рут, баффы и т.д.)
        if (GetComponent<EffectManager>() == null)
        {
            gameObject.AddComponent<EffectManager>();
            Debug.Log($"[DummyEnemy] ✅ Добавлен EffectManager на {gameObject.name}");
        }

        // Добавляем CharacterStats для поддержки эффектов на характеристики (Perception, Attack, Defense и т.д.)
        if (GetComponent<CharacterStats>() == null)
        {
            CharacterStats stats = gameObject.AddComponent<CharacterStats>();
            // Устанавливаем базовые характеристики для DummyEnemy
            stats.perception = 5; // Среднее восприятие
            Debug.Log($"[DummyEnemy] ✅ Добавлен CharacterStats на {gameObject.name} (Perception: {stats.perception})");
        }

        UpdateHealthBar();
    }

    void Update()
    {
        // Обновляем визуальный feedback урона
        if (damageFlashTimer > 0f)
        {
            damageFlashTimer -= Time.deltaTime;
            if (damageFlashTimer <= 0f && visualMaterial != null)
            {
                visualMaterial.color = normalColor;
            }
        }

        // HP bar всегда смотрит на камеру
        if (hpBarInstance != null && Camera.main != null)
        {
            hpBarInstance.transform.LookAt(Camera.main.transform);
            hpBarInstance.transform.Rotate(0f, 180f, 0f); // Разворачиваем к камере
        }
    }

    /// <summary>
    /// Создать HP bar над врагом
    /// </summary>
    void CreateHealthBar()
    {
        // Если есть префаб - используем его
        if (hpBarPrefab != null)
        {
            hpBarInstance = Instantiate(hpBarPrefab, transform);
            hpBarInstance.transform.localPosition = hpBarOffset;
            hpSlider = hpBarInstance.GetComponentInChildren<Slider>();
            hpText = hpBarInstance.GetComponentInChildren<Text>();
        }
        // Иначе создаём простой HP bar через код
        else
        {
            CreateSimpleHealthBar();
        }
    }

    /// <summary>
    /// Создать простой HP bar (если нет префаба)
    /// </summary>
    void CreateSimpleHealthBar()
    {
        // Canvas для HP бара
        GameObject canvasObj = new GameObject("HPBarCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = hpBarOffset;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(2f, 0.3f);
        canvasRect.localScale = Vector3.one * 0.01f;

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // HP Slider
        GameObject sliderObj = new GameObject("HPSlider");
        sliderObj.transform.SetParent(canvasObj.transform);
        hpSlider = sliderObj.AddComponent<Slider>();

        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = new Vector2(5f, 5f);
        sliderRect.offsetMax = new Vector2(-5f, -25f);

        // Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform);
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform);
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = Color.green;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        hpSlider.fillRect = fillRect;
        hpSlider.minValue = 0f;
        hpSlider.maxValue = 1f;
        hpSlider.value = 1f;

        // HP Text
        GameObject textObj = new GameObject("HPText");
        textObj.transform.SetParent(canvasObj.transform);
        hpText = textObj.AddComponent<Text>();
        hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hpText.fontSize = 18;
        hpText.color = Color.white;
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.text = $"{maxHealth}/{maxHealth}";

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.anchoredPosition = new Vector2(0f, 5f);
        textRect.sizeDelta = new Vector2(0f, 20f);

        hpBarInstance = canvasObj;

        Debug.Log($"[DummyEnemy] Создан простой HP bar для {gameObject.name}");
    }

    /// <summary>
    /// Обновить HP bar
    /// </summary>
    void UpdateHealthBar()
    {
        if (hpSlider != null)
        {
            hpSlider.value = currentHealth / maxHealth;

            // Меняем цвет заливки в зависимости от HP
            Image fillImage = hpSlider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                float hpPercent = currentHealth / maxHealth;
                if (hpPercent > 0.5f)
                    fillImage.color = Color.green;
                else if (hpPercent > 0.25f)
                    fillImage.color = Color.yellow;
                else
                    fillImage.color = Color.red;
            }
        }

        if (hpText != null)
        {
            hpText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }
    }

    /// <summary>
    /// Получить урон (переопределяем метод TargetableEntity)
    /// </summary>
    public override void TakeDamage(float damage, TargetableEntity attacker = null)
    {
        if (!isAlive) return;

        // Вызываем базовый метод для обновления HP и вызова события OnHealthChanged
        base.TakeDamage(damage, attacker);

        // Визуальный feedback
        DamageFlash();
        UpdateHealthBar();

        // Смерть обрабатывается в базовом классе, но добавляем визуализацию
        if (!isAlive)
        {
            HandleDeath();
        }
    }

    /// <summary>
    /// Визуальный эффект получения урона
    /// </summary>
    void DamageFlash()
    {
        if (visualMaterial != null)
        {
            visualMaterial.color = damageColor;
            damageFlashTimer = damageFlashDuration;
        }
    }

    /// <summary>
    /// Обработка смерти dummy (визуальные эффекты)
    /// </summary>
    void HandleDeath()
    {
        Debug.Log($"[DummyEnemy] 💀 {gameObject.name} уничтожен!");

        // Визуальный эффект смерти
        if (visualMaterial != null)
        {
            visualMaterial.color = Color.gray;
        }

        // Авто-респавн или уничтожение
        if (autoRespawn)
        {
            Invoke(nameof(Respawn), respawnDelay);
        }
        else
        {
            // Уничтожаем через 2 секунды
            Destroy(gameObject, 2f);
        }
    }

    /// <summary>
    /// Респавн dummy
    /// </summary>
    void Respawn()
    {
        Debug.Log($"[DummyEnemy] ♻️ {gameObject.name} респавн!");

        // Восстанавливаем HP через базовый класс
        currentHealth = maxHealth;
        isAlive = true;

        // Вызываем событие изменения HP
        InvokeHealthChanged(currentHealth, maxHealth);

        // Восстанавливаем позицию
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        // Восстанавливаем цвет
        if (visualMaterial != null)
        {
            visualMaterial.color = normalColor;
        }

        UpdateHealthBar();
    }

    /// <summary>
    /// Heal (переопределяем метод TargetableEntity)
    /// </summary>
    public override void Heal(float amount, TargetableEntity healer = null)
    {
        if (!isAlive) return;

        // Вызываем базовый метод для обновления HP и вызова события OnHealthChanged
        base.Heal(amount, healer);

        // Обновляем HP бар
        UpdateHealthBar();
    }

    /// <summary>
    /// Отладочная визуализация
    /// </summary>
    void OnDrawGizmos()
    {
        // HP bar позиция
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + hpBarOffset, new Vector3(2f, 0.3f, 0.1f));
    }
}
