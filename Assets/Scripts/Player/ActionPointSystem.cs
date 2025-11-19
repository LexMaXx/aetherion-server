using UnityEngine;

/// <summary>
/// Система очков действия (Action Points)
/// Используется для ограничения частоты использования скиллов
/// Восстанавливается со временем
/// </summary>
public class ActionPointSystem : MonoBehaviour
{
    [Header("Action Points")]
    [SerializeField] private float maxActionPoints = 100f;
    [SerializeField] private float currentActionPoints = 100f;

    [Header("Regeneration")]
    [SerializeField] private float regenPerSecond = 10f;
    [SerializeField] private bool autoRegen = true;

    [Header("Debug")]
    public bool enableLogs = false;

    public float MaxActionPoints => maxActionPoints;
    public float CurrentActionPoints => currentActionPoints;

    private void Start()
    {
        currentActionPoints = maxActionPoints;
        Log($"✅ ActionPointSystem initialized: {currentActionPoints}/{maxActionPoints}");
    }

    private void Update()
    {
        if (autoRegen && currentActionPoints < maxActionPoints)
        {
            RegenerateActionPoints(regenPerSecond * Time.deltaTime);
        }
    }

    /// <summary>
    /// Восстановить AP
    /// </summary>
    public void RegenerateActionPoints(float amount)
    {
        if (amount <= 0f) return;

        float oldAP = currentActionPoints;
        currentActionPoints = Mathf.Min(currentActionPoints + amount, maxActionPoints);

        if (enableLogs && currentActionPoints != oldAP)
        {
            Log($"🔄 AP Regen: +{amount:F1} → {currentActionPoints:F0}/{maxActionPoints:F0}");
        }
    }

    /// <summary>
    /// Потратить AP
    /// </summary>
    public bool SpendActionPoints(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (currentActionPoints >= amount)
        {
            currentActionPoints -= amount;
            Log($"💰 AP Spent: -{amount:F0} → {currentActionPoints:F0}/{maxActionPoints:F0}");
            return true;
        }
        else
        {
            Log($"❌ Not enough AP! Need: {amount:F0}, Have: {currentActionPoints:F0}");
            return false;
        }
    }

    /// <summary>
    /// Проверить достаточно ли AP
    /// </summary>
    public bool HasEnoughActionPoints(float amount)
    {
        return currentActionPoints >= amount;
    }

    /// <summary>
    /// Установить максимальные AP
    /// </summary>
    public void SetMaxActionPoints(float newMax)
    {
        maxActionPoints = newMax;
        currentActionPoints = Mathf.Min(currentActionPoints, maxActionPoints);
        Log($"📊 Max AP set to: {maxActionPoints}");
    }

    /// <summary>
    /// Установить текущие AP
    /// </summary>
    public void SetCurrentActionPoints(float amount)
    {
        currentActionPoints = Mathf.Clamp(amount, 0f, maxActionPoints);
        Log($"📊 Current AP set to: {currentActionPoints}/{maxActionPoints}");
    }

    /// <summary>
    /// Полностью восстановить AP
    /// </summary>
    public void RestoreFull()
    {
        currentActionPoints = maxActionPoints;
        Log($"✨ AP fully restored: {currentActionPoints}/{maxActionPoints}");
    }

    private void Log(string message)
    {
        if (enableLogs)
        {
            Debug.Log($"[ActionPointSystem] {message}");
        }
    }
}
