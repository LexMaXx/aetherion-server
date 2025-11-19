using UnityEngine;
using TMPro;

/// <summary>
/// 3D маркер локации на карте мира
/// Размещается на terrain, показывает иконку и название локации
/// Взаимодействие через приближение игрока + нажатие E
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class WorldMapLocationMarker : MonoBehaviour
{
    [Header("Visual Elements")]
    [Tooltip("3D модель/спрайт иконки")]
    [SerializeField] private GameObject iconObject;

    [Tooltip("Текст названия локации (3D Text или TextMeshPro)")]
    [SerializeField] private TextMeshPro nameText;

    [Tooltip("Визуальный эффект при подсветке")]
    [SerializeField] private GameObject highlightEffect;

    [Tooltip("Визуальный эффект для заблокированной локации")]
    [SerializeField] private GameObject lockedOverlay;

    [Header("Settings")]
    [Tooltip("Скорость вращения иконки")]
    [SerializeField] private float rotationSpeed = 30f;

    [Tooltip("Амплитуда плавающего движения")]
    [SerializeField] private float floatAmplitude = 0.3f;

    [Tooltip("Скорость плавающего движения")]
    [SerializeField] private float floatSpeed = 1f;

    [Tooltip("Цвет подсветки доступной локации")]
    [SerializeField] private Color unlockedColor = Color.green;

    [Tooltip("Цвет заблокированной локации")]
    [SerializeField] private Color lockedColor = Color.gray;

    // Runtime переменные
    private LocationData locationData;
    private bool isUnlocked = false;
    private bool isHighlighted = false;
    private Vector3 startPosition;
    private float floatTimer = 0f;
    private Material iconMaterial;
    private SphereCollider triggerCollider;

    void Awake()
    {
        // Настраиваем триггер для взаимодействия
        triggerCollider = GetComponent<SphereCollider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.radius = 1f; // Радиус взаимодействия
        }
    }

    void Start()
    {
        startPosition = transform.position;

        // Получаем материал иконки
        if (iconObject != null)
        {
            Renderer renderer = iconObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                iconMaterial = renderer.material;
            }
        }

        // Выключаем эффект подсветки по умолчанию
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(false);
        }
    }

    void Update()
    {
        // Вращение иконки
        if (iconObject != null)
        {
            iconObject.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }

        // Плавающее движение вверх-вниз
        floatTimer += Time.deltaTime * floatSpeed;
        float yOffset = Mathf.Sin(floatTimer) * floatAmplitude;
        transform.position = startPosition + Vector3.up * yOffset;

        // Разворот текста к камере
        if (nameText != null && Camera.main != null)
        {
            nameText.transform.LookAt(Camera.main.transform);
            nameText.transform.Rotate(0, 180, 0); // Переворачиваем текст
        }
    }

    /// <summary>
    /// Инициализация маркера с данными локации
    /// </summary>
    public void Initialize(LocationData data)
    {
        locationData = data;

        if (locationData == null)
        {
            Debug.LogError("[WorldMapLocationMarker] LocationData is null!");
            return;
        }

        // Устанавливаем название
        if (nameText != null)
        {
            nameText.text = locationData.locationName;
        }

        // Проверяем статус разблокировки
        UpdateLockedStatus();

        // Устанавливаем цвет иконки в зависимости от типа локации
        UpdateIconColor();

        Debug.Log($"[WorldMapLocationMarker] Инициализирован маркер: {locationData.locationName}");
    }

    /// <summary>
    /// Обновить статус блокировки локации
    /// </summary>
    public void UpdateLockedStatus()
    {
        if (locationData == null)
            return;

        // Проверяем разблокирована ли локация
        if (GameProgressManager.Instance != null)
        {
            isUnlocked = GameProgressManager.Instance.IsLocationUnlocked(locationData.sceneName);
        }
        else
        {
            isUnlocked = locationData.unlockedByDefault;
        }

        // Обновляем визуал
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isUnlocked);
        }

        // Меняем цвет текста
        if (nameText != null)
        {
            nameText.color = isUnlocked ? unlockedColor : lockedColor;
        }
    }

    /// <summary>
    /// Установить цвет иконки в зависимости от типа локации
    /// </summary>
    private void UpdateIconColor()
    {
        if (iconMaterial == null || locationData == null)
            return;

        // Используем цвет из LocationData
        iconMaterial.color = locationData.iconColor;
    }

    /// <summary>
    /// Подсветить/убрать подсветку маркера
    /// </summary>
    public void SetHighlighted(bool highlighted)
    {
        isHighlighted = highlighted;

        if (highlightEffect != null)
        {
            highlightEffect.SetActive(highlighted);
        }

        // Увеличиваем размер при подсветке
        if (iconObject != null)
        {
            float scale = highlighted ? 1.3f : 1f;
            iconObject.transform.localScale = Vector3.one * scale;
        }
    }

    /// <summary>
    /// Проверка разблокирована ли локация
    /// </summary>
    public bool IsUnlocked()
    {
        return isUnlocked;
    }

    /// <summary>
    /// Получить данные локации
    /// </summary>
    public LocationData GetLocationData()
    {
        return locationData;
    }

    /// <summary>
    /// Попытка войти в локацию
    /// </summary>
    public void TryEnterLocation()
    {
        if (!isUnlocked)
        {
            Debug.Log($"[WorldMapLocationMarker] Локация '{locationData.locationName}' заблокирована!");
            ShowLockedMessage();
            return;
        }

        Debug.Log($"[WorldMapLocationMarker] Вход в локацию: {locationData.locationName}");

        // Переходим в локацию
        if (WorldMapManager.Instance != null)
        {
            WorldMapManager.Instance.TravelToLocation(locationData);
        }
    }

    /// <summary>
    /// Показать сообщение о заблокированной локации
    /// </summary>
    private void ShowLockedMessage()
    {
        string message = $"Локация заблокирована";

        if (locationData.requiredLevel > 1)
        {
            message += $"\nТребуется уровень: {locationData.requiredLevel}";
        }

        if (!string.IsNullOrEmpty(locationData.requiredQuestId))
        {
            message += $"\nТребуется квест: {locationData.requiredQuestId}";
        }

        Debug.Log($"[WorldMapLocationMarker] {message}");
        // TODO: Показать UI сообщение игроку
    }

    // Gizmos для отладки
    void OnDrawGizmos()
    {
        Gizmos.color = isUnlocked ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);

        // Линия вверх для видимости
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
    }

    void OnDrawGizmosSelected()
    {
        // Показываем радиус триггера
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerCollider != null ? triggerCollider.radius : 1f);
    }
}
