using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Точка локации на мировой карте
/// Интерактивный UI элемент для перехода в локацию
/// </summary>
public class LocationPoint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private LocationData locationData;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipText;

    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private float hoverScale = 1.2f;
    [SerializeField] private float animationSpeed = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip lockedSound;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private Color targetColor;
    private bool isUnlocked = false;
    private AudioSource audioSource;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        // Получаем AudioSource или создаём
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Инициализация
        Initialize();
    }

    void Update()
    {
        // Плавная анимация
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);

        if (iconImage != null)
        {
            iconImage.color = Color.Lerp(iconImage.color, targetColor, Time.deltaTime * animationSpeed);
        }
    }

    /// <summary>
    /// Инициализация точки локации
    /// </summary>
    public void Initialize()
    {
        if (locationData == null)
        {
            Debug.LogWarning($"[LocationPoint] LocationData не назначен на '{gameObject.name}'!");
            return;
        }

        // Проверяем доступность локации
        isUnlocked = CheckIfUnlocked();

        // Настраиваем иконку
        if (iconImage != null && locationData.locationIcon != null)
        {
            iconImage.sprite = locationData.locationIcon;
            targetColor = isUnlocked ? (locationData.iconColor * normalColor) : lockedColor;
            iconImage.color = targetColor;
        }

        // Настраиваем название
        if (nameText != null)
        {
            nameText.text = locationData.locationName;
            nameText.color = isUnlocked ? Color.white : lockedColor;
        }

        // Настраиваем overlay для заблокированных локаций
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isUnlocked);
        }

        // Скрываем tooltip
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }

        Debug.Log($"[LocationPoint] '{locationData.locationName}' инициализирована. Разблокирована: {isUnlocked}");
    }

    /// <summary>
    /// Проверка доступности локации
    /// </summary>
    private bool CheckIfUnlocked()
    {
        if (locationData == null)
            return false;

        // Проверяем через GameProgressManager
        if (GameProgressManager.Instance != null)
        {
            return GameProgressManager.Instance.IsLocationUnlocked(locationData.sceneName);
        }

        // Fallback: если менеджера нет, используем unlockedByDefault
        return locationData.unlockedByDefault;
    }

    /// <summary>
    /// Переход в локацию
    /// </summary>
    public void TravelToLocation()
    {
        if (!isUnlocked)
        {
            Debug.Log($"[LocationPoint] Локация '{locationData.locationName}' заблокирована!");
            PlaySound(lockedSound);
            ShowLockedMessage();
            return;
        }

        Debug.Log($"[LocationPoint] Переход в локацию: {locationData.locationName} ({locationData.sceneName})");

        PlaySound(clickSound);

        // Сохраняем целевую локацию
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.SetTargetLocation(locationData.sceneName);
            GameProgressManager.Instance.MarkLocationAsVisited(locationData.sceneName);
        }

        // Загружаем сцену через SceneTransitionManager
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(locationData.sceneName);
        }
        else
        {
            // Fallback
            UnityEngine.SceneManagement.SceneManager.LoadScene(locationData.sceneName);
        }
    }

    /// <summary>
    /// Показать сообщение о заблокированной локации
    /// </summary>
    private void ShowLockedMessage()
    {
        string message = $"Локация заблокирована!\n";

        if (!string.IsNullOrEmpty(locationData.requiredQuestId))
        {
            message += $"Требуется квест: {locationData.requiredQuestId}\n";
        }

        if (locationData.requiredLevel > 1)
        {
            message += $"Требуется уровень: {locationData.requiredLevel}";
        }

        // TODO: Интеграция с вашей UI системой
        Debug.LogWarning(message);
    }

    #region Event Handlers

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;

        if (isUnlocked)
        {
            targetColor = locationData.iconColor * hoverColor;
            PlaySound(hoverSound);
        }

        // Показываем tooltip
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
        targetColor = isUnlocked ? (locationData.iconColor * normalColor) : lockedColor;

        // Скрываем tooltip
        HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TravelToLocation();
    }

    #endregion

    #region Tooltip

    private void ShowTooltip()
    {
        if (tooltipPanel == null || tooltipText == null)
            return;

        // Формируем текст tooltip
        string tooltip = $"<b>{locationData.locationName}</b>\n";
        tooltip += $"{locationData.description}\n";
        tooltip += $"Тип: {GetLocationTypeString()}\n";
        tooltip += $"Уровень: {locationData.recommendedLevel}\n";

        if (!isUnlocked)
        {
            tooltip += "\n<color=red>ЗАБЛОКИРОВАНО</color>";
        }
        else if (locationData.isVisited)
        {
            tooltip += "\n<color=green>Посещена</color>";
        }

        tooltipText.text = tooltip;
        tooltipPanel.SetActive(true);
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    private string GetLocationTypeString()
    {
        switch (locationData.locationType)
        {
            case LocationType.City: return "Город";
            case LocationType.Village: return "Деревня";
            case LocationType.Dungeon: return "Подземелье";
            case LocationType.Wilderness: return "Дикая местность";
            case LocationType.Ruins: return "Руины";
            case LocationType.Camp: return "Лагерь";
            case LocationType.Special: return "Особая локация";
            default: return "Неизвестно";
        }
    }

    #endregion

    #region Audio

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    #endregion

    // Визуализация в редакторе
    private void OnDrawGizmos()
    {
        if (locationData != null)
        {
            Gizmos.color = locationData.iconColor;
            Gizmos.DrawSphere(transform.position, 0.5f);

            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up, locationData.locationName, new GUIStyle()
            {
                normal = { textColor = locationData.iconColor },
                fontSize = 10
            });
            #endif
        }
    }
}
