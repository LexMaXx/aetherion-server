using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Отображает активные статус-эффекты над головой персонажа (Stun, Root, Sleep, и т.д.)
/// </summary>
public class StatusEffectUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject effectPanel; // Родительский панель для эффектов
    [SerializeField] private TextMeshProUGUI effectText; // Текст эффекта (например "ОГЛУШЕНИЕ")
    [SerializeField] private Image effectIcon; // Иконка эффекта (опционально)

    [Header("Settings")]
    [SerializeField] private float yOffset = 2.5f; // Высота над головой персонажа
    [SerializeField] private bool followTarget = true; // Следовать за целью

    [Header("Dependencies")]
    private EffectManager effectManager;
    private Transform targetTransform; // Transform персонажа
    private Camera mainCamera;

    private ActiveEffect currentDisplayedEffect; // Текущий отображаемый эффект

    void Start()
    {
        mainCamera = Camera.main;

        // Ищем EffectManager на родителе
        effectManager = GetComponentInParent<EffectManager>();
        if (effectManager == null)
        {
            Debug.LogWarning("[StatusEffectUI] ⚠️ EffectManager не найден!");
        }

        // Ищем Transform персонажа (обычно Model)
        targetTransform = transform.parent;

        // Скрываем панель по умолчанию
        if (effectPanel != null)
        {
            effectPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (effectManager == null) return;

        // Проверяем активные контрольные эффекты (Stun, Root, Sleep, Silence, Fear)
        UpdateEffectDisplay();

        // Обновляем позицию UI над головой персонажа
        if (followTarget && effectPanel != null && effectPanel.activeSelf)
        {
            UpdateUIPosition();
        }
    }

    /// <summary>
    /// Обновить отображение эффекта
    /// </summary>
    void UpdateEffectDisplay()
    {
        // Получаем активные контрольные эффекты
        var activeEffects = effectManager.GetActiveEffects();

        // Ищем самый важный эффект для отображения (приоритет: Stun > Root > Sleep > Silence > Fear)
        ActiveEffect priorityEffect = null;

        foreach (var effect in activeEffects)
        {
            if (effect.config.IsCrowdControl())
            {
                // Определяем приоритет
                if (priorityEffect == null || GetEffectPriority(effect.config.effectType) > GetEffectPriority(priorityEffect.config.effectType))
                {
                    priorityEffect = effect;
                }
            }
        }

        // Если есть эффект для отображения
        if (priorityEffect != null)
        {
            ShowEffect(priorityEffect);
        }
        else
        {
            HideEffect();
        }
    }

    /// <summary>
    /// Показать эффект
    /// </summary>
    void ShowEffect(ActiveEffect effect)
    {
        if (effectPanel != null && !effectPanel.activeSelf)
        {
            effectPanel.SetActive(true);
        }

        if (effectText != null)
        {
            // Устанавливаем текст эффекта
            string effectName = GetEffectDisplayName(effect.config.effectType);
            float timeRemaining = effect.remainingDuration;
            effectText.text = $"{effectName}\n{timeRemaining:F1}s";

            // Цвет текста в зависимости от типа эффекта
            effectText.color = GetEffectColor(effect.config.effectType);
        }

        currentDisplayedEffect = effect;
    }

    /// <summary>
    /// Скрыть эффект
    /// </summary>
    void HideEffect()
    {
        if (effectPanel != null && effectPanel.activeSelf)
        {
            effectPanel.SetActive(false);
        }
        currentDisplayedEffect = null;
    }

    /// <summary>
    /// Обновить позицию UI над головой персонажа
    /// </summary>
    void UpdateUIPosition()
    {
        if (mainCamera == null || targetTransform == null) return;

        // Позиция над головой персонажа в мировых координатах
        Vector3 worldPosition = targetTransform.position + Vector3.up * yOffset;

        // Конвертируем в экранные координаты
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        // Проверяем видимость (не за камерой)
        if (screenPosition.z > 0)
        {
            effectPanel.transform.position = screenPosition;
            effectPanel.SetActive(true);
        }
        else
        {
            effectPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Получить приоритет эффекта (выше = важнее)
    /// </summary>
    int GetEffectPriority(EffectType effectType)
    {
        switch (effectType)
        {
            case EffectType.Stun: return 5;
            case EffectType.Sleep: return 4;
            case EffectType.Fear: return 3;
            case EffectType.Root: return 2;
            case EffectType.Silence: return 1;
            default: return 0;
        }
    }

    /// <summary>
    /// Получить отображаемое имя эффекта
    /// </summary>
    string GetEffectDisplayName(EffectType effectType)
    {
        switch (effectType)
        {
            case EffectType.Stun: return "😵 ОГЛУШЕНИЕ";
            case EffectType.Root: return "🌿 КОРНИ";
            case EffectType.Sleep: return "😴 СОН";
            case EffectType.Silence: return "🔇 МОЛЧАНИЕ";
            case EffectType.Fear: return "😱 СТРАХ";
            default: return effectType.ToString();
        }
    }

    /// <summary>
    /// Получить цвет эффекта
    /// </summary>
    Color GetEffectColor(EffectType effectType)
    {
        switch (effectType)
        {
            case EffectType.Stun: return new Color(1f, 0.8f, 0f); // Золотой
            case EffectType.Root: return new Color(0.4f, 0.8f, 0.2f); // Зеленый
            case EffectType.Sleep: return new Color(0.6f, 0.6f, 1f); // Голубой
            case EffectType.Silence: return new Color(0.8f, 0.2f, 0.8f); // Фиолетовый
            case EffectType.Fear: return new Color(1f, 0.3f, 0.3f); // Красный
            default: return Color.white;
        }
    }
}
