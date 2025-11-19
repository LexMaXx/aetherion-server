using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI индикатор цели - показывает маркер над врагом
/// Отображает имя, HP bar, дистанцию
/// </summary>
public class TargetIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TargetSystem targetSystem;
    [SerializeField] private Transform playerTransform;

    [Header("UI Elements")]
    [SerializeField] private GameObject indicatorPanel;
    [SerializeField] private TextMeshProUGUI targetNameText;
    [SerializeField] private TextMeshProUGUI targetDistanceText;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("World Marker")]
    [SerializeField] private GameObject worldMarkerPrefab;
    private GameObject currentWorldMarker;

    [Header("Settings")]
    [SerializeField] private Vector3 markerOffset = new Vector3(0, 2.5f, 0);
    [SerializeField] private Color markerColor = Color.red;

    private TargetableEntity currentTarget; // ОБНОВЛЕНО: используем TargetableEntity вместо Enemy
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        // Скрываем панель по умолчанию
        if (indicatorPanel != null)
        {
            indicatorPanel.SetActive(false);
        }

        // Подписываемся на событие смены цели
        if (targetSystem != null)
        {
            targetSystem.OnTargetChanged += OnTargetChanged;
        }
        else
        {
            Debug.LogError("[TargetIndicator] TargetSystem не назначен!");
        }
    }

    void OnDestroy()
    {
        // Отписываемся от события
        if (targetSystem != null)
        {
            targetSystem.OnTargetChanged -= OnTargetChanged;
        }
    }

    void Update()
    {
        if (currentTarget != null && currentTarget.IsEntityAlive()) // ОБНОВЛЕНО: IsEntityAlive()
        {
            UpdateIndicator();
            UpdateWorldMarker();
        }
    }

    /// <summary>
    /// Обработчик смены цели
    /// ОБНОВЛЕНО: Принимает TargetableEntity вместо Enemy
    /// </summary>
    private void OnTargetChanged(TargetableEntity newTarget)
    {
        currentTarget = newTarget;

        if (currentTarget == null)
        {
            HideIndicator();
        }
        else
        {
            ShowIndicator();
            CreateWorldMarker();
        }
    }

    /// <summary>
    /// Показать индикатор
    /// </summary>
    private void ShowIndicator()
    {
        if (indicatorPanel != null)
        {
            indicatorPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Скрыть индикатор
    /// </summary>
    private void HideIndicator()
    {
        if (indicatorPanel != null)
        {
            indicatorPanel.SetActive(false);
        }

        DestroyWorldMarker();
    }

    /// <summary>
    /// Обновить UI индикатор
    /// </summary>
    private void UpdateIndicator()
    {
        if (currentTarget == null)
            return;

        // Имя врага
        if (targetNameText != null)
        {
            targetNameText.text = currentTarget.GetEnemyName();
        }

        // Дистанция (только горизонтальная плоскость X,Z - игнорируем высоту)
        if (targetDistanceText != null && playerTransform != null)
        {
            Vector3 playerPosFlat = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
            Vector3 targetPosFlat = new Vector3(currentTarget.transform.position.x, 0, currentTarget.transform.position.z);
            float distance = Vector3.Distance(playerPosFlat, targetPosFlat);
            targetDistanceText.text = $"{distance:F1}m";
        }

        // HP Bar
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = currentTarget.GetHealthPercent();
        }

        // HP Text
        if (healthText != null)
        {
            healthText.text = $"{currentTarget.GetCurrentHealth():F0}/{currentTarget.GetMaxHealth():F0}";
        }
    }

    /// <summary>
    /// Создать маркер в мире над врагом
    /// </summary>
    private void CreateWorldMarker()
    {
        DestroyWorldMarker();

        if (worldMarkerPrefab != null && currentTarget != null)
        {
            currentWorldMarker = Instantiate(worldMarkerPrefab, currentTarget.transform);

            // ИСПРАВЛЕНО: Позиция стрелки зависит от высоты врага
            // Находим самую высокую точку врага (через Renderer или Collider)
            float enemyHeight = GetEnemyHeight(currentTarget);
            Vector3 adaptiveOffset = new Vector3(0, enemyHeight + 1.5f, 0); // +1.5м над верхней точкой
            currentWorldMarker.transform.localPosition = adaptiveOffset;

            Debug.Log($"[TargetIndicator] Стрелка создана над врагом. Высота врага: {enemyHeight}м, offset: {adaptiveOffset.y}м");

            // Настраиваем цвет маркера
            SpriteRenderer spriteRenderer = currentWorldMarker.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = markerColor;
            }
        }
    }

    /// <summary>
    /// Получить высоту существа (от центра до верхней точки)
    /// ОБНОВЛЕНО: Работает с TargetableEntity вместо Enemy
    /// </summary>
    private float GetEnemyHeight(TargetableEntity entity)
    {
        if (entity == null) return 2.5f;

        // Пробуем получить высоту через Renderer (bounds)
        Renderer[] renderers = entity.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            // Находим максимальную Y координату среди всех renderer'ов
            float maxY = float.MinValue;
            foreach (Renderer rend in renderers)
            {
                float topY = rend.bounds.max.y;
                if (topY > maxY)
                    maxY = topY;
            }

            // Возвращаем высоту относительно центра существа
            float height = maxY - entity.transform.position.y;
            return height;
        }

        // Если нет Renderer - пробуем через Collider
        Collider collider = entity.GetComponent<Collider>();
        if (collider != null)
        {
            float height = collider.bounds.max.y - entity.transform.position.y;
            return height;
        }

        // Дефолтное значение если ничего не нашли
        return 2.5f;
    }

    /// <summary>
    /// Обновить позицию маркера
    /// </summary>
    private void UpdateWorldMarker()
    {
        if (currentWorldMarker != null && currentTarget != null)
        {
            // Маркер следует за врагом (уже прикреплен к transform)
            // Поворачиваем маркер к камере
            if (mainCamera != null)
            {
                currentWorldMarker.transform.LookAt(mainCamera.transform);
                currentWorldMarker.transform.Rotate(0, 180, 0); // Разворот к камере
            }
        }
    }

    /// <summary>
    /// Уничтожить маркер
    /// </summary>
    private void DestroyWorldMarker()
    {
        if (currentWorldMarker != null)
        {
            Destroy(currentWorldMarker);
            currentWorldMarker = null;
        }
    }
}
