using UnityEngine;
using System;

/// <summary>
/// Система маны персонажа
/// Интегрируется с CharacterStats (Wisdom → MaxMP и Regen)
/// </summary>
public class ManaSystem : MonoBehaviour
{
    [Header("Mana Settings")]
    [SerializeField] private float currentMana;
    [SerializeField] private float maxMana = 100f;
    [SerializeField] private float manaRegenRate = 0.1f; // MP/сек (ЗАМЕДЛЕНО в 10 раз)

    // Интеграция с CharacterStats
    private CharacterStats characterStats;

    // Регенерация
    private bool isRegenerating = false;

    // События
    public event Action<float, float> OnManaChanged; // (current, max)

    // Геттеры
    public float CurrentMana => currentMana;
    public float MaxMana => maxMana;
    public float ManaPercent => maxMana > 0 ? currentMana / maxMana : 0f;

    void Start()
    {
        // Интеграция с CharacterStats (Wisdom → MP и Regen)
        characterStats = GetComponent<CharacterStats>();
        if (characterStats != null)
        {
            characterStats.OnStatsChanged += UpdateManaFromStats;
            UpdateManaFromStats();
            Debug.Log("[ManaSystem] ✅ Интеграция с CharacterStats активирована");
        }
        else
        {
            // Если нет CharacterStats - используем дефолтные значения
            currentMana = maxMana;
        }

        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    void Update()
    {
        // Регенерация MP только когда стоит на месте
        bool isStanding = IsPlayerStanding();

        if (isStanding && currentMana < maxMana)
        {
            if (!isRegenerating)
            {
                isRegenerating = true;
                Debug.Log("[ManaSystem] 🔄 Начало восстановления MP (персонаж стоит)");
            }

            currentMana += manaRegenRate * Time.deltaTime;
            currentMana = Mathf.Min(currentMana, maxMana);
            OnManaChanged?.Invoke(currentMana, maxMana);

            // Полностью восстановлено
            if (currentMana >= maxMana)
            {
                currentMana = maxMana;
                if (isRegenerating)
                {
                    isRegenerating = false;
                    Debug.Log("[ManaSystem] ✅ MP полностью восстановлена!");
                }
            }
        }
        else if (!isStanding && isRegenerating)
        {
            // Остановили регенерацию при движении
            isRegenerating = false;
            Debug.Log("[ManaSystem] ⏸️ Остановка восстановления MP (персонаж движется)");
        }
    }

    /// <summary>
    /// Проверка, стоит ли игрок на месте
    /// </summary>
    private bool IsPlayerStanding()
    {
        // Проверяем ввод движения - самый надёжный метод
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool hasMovementInput = Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f;

        // Если есть ввод - персонаж движется
        return !hasMovementInput;
    }

    /// <summary>
    /// Обновить Mana на основе Wisdom
    /// </summary>
    private void UpdateManaFromStats()
    {
        if (characterStats == null) return;

        float oldMaxMana = maxMana;
        maxMana = characterStats.MaxMana;
        manaRegenRate = characterStats.ManaRegen;

        // Восстанавливаем MP пропорционально
        if (oldMaxMana > 0)
        {
            float manaPercent = currentMana / oldMaxMana;
            currentMana = maxMana * manaPercent;
        }
        else
        {
            currentMana = maxMana;
        }

        OnManaChanged?.Invoke(currentMana, maxMana);
        Debug.Log($"[ManaSystem] Обновлено из Stats: {currentMana:F0}/{maxMana:F0} MP, Regen: {manaRegenRate:F1} MP/сек (Wisdom: {characterStats.wisdom})");
    }

    /// <summary>
    /// Потратить ману
    /// </summary>
    public bool SpendMana(float amount)
    {
        if (currentMana < amount)
        {
            Debug.LogWarning($"[ManaSystem] Недостаточно маны! Нужно: {amount:F0}, Есть: {currentMana:F0}");
            return false;
        }

        currentMana -= amount;
        Debug.Log($"[ManaSystem] -{amount:F0} MP. Осталось: {currentMana:F0}/{maxMana:F0}");
        OnManaChanged?.Invoke(currentMana, maxMana);
        return true;
    }

    /// <summary>
    /// Восстановить ману
    /// </summary>
    public void RestoreMana(float amount)
    {
        currentMana += amount;
        currentMana = Mathf.Min(currentMana, maxMana);

        Debug.Log($"[ManaSystem] +{amount:F0} MP. Текущая: {currentMana:F0}/{maxMana:F0}");
        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    /// <summary>
    /// Проверить достаточно ли маны
    /// </summary>
    public bool HasEnoughMana(float amount)
    {
        return currentMana >= amount;
    }

    private void OnDestroy()
    {
        if (characterStats != null)
        {
            characterStats.OnStatsChanged -= UpdateManaFromStats;
        }
    }

    // Для тестирования в редакторе
    [ContextMenu("Test: Spend 20 Mana")]
    private void TestSpendMana()
    {
        SpendMana(20f);
    }

    [ContextMenu("Test: Restore 30 Mana")]
    private void TestRestoreMana()
    {
        RestoreMana(30f);
    }
}
