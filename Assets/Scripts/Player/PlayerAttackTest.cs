using UnityEngine;

/// <summary>
/// ТЕСТОВЫЙ скрипт для проверки BasicAttackConfig
/// Используйте ЭТОТ компонент вместо PlayerAttack для тестирования!
/// </summary>
public class PlayerAttackTest : MonoBehaviour
{
    [Header("⚔️ BASIC ATTACK CONFIG (TEST)")]
    [Tooltip("Перетащите сюда BasicAttackConfig_Mage")]
    public BasicAttackConfig attackConfig;

    [Header("Тестовая информация")]
    [Tooltip("Нажмите Play чтобы увидеть лог")]
    public bool showDebugInfo = true;

    void Start()
    {
        if (attackConfig == null)
        {
            Debug.LogWarning("[PlayerAttackTest] ⚠️ BasicAttackConfig НЕ НАЗНАЧЕН!");
            Debug.LogWarning("[PlayerAttackTest] Перетащите BasicAttackConfig_Mage в поле 'Attack Config'");
            return;
        }

        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("[PlayerAttackTest] ✅ BasicAttackConfig НАЗНАЧЕН!");
        Debug.Log($"[PlayerAttackTest] Имя: {attackConfig.name}");
        Debug.Log($"[PlayerAttackTest] Класс: {attackConfig.characterClass}");
        Debug.Log($"[PlayerAttackTest] Тип: {attackConfig.attackType}");
        Debug.Log($"[PlayerAttackTest] Урон: {attackConfig.baseDamage}");
        Debug.Log($"[PlayerAttackTest] Дальность: {attackConfig.attackRange}m");
        Debug.Log($"[PlayerAttackTest] INT Scaling: {attackConfig.intelligenceScaling}x");

        // Тест расчёта урона
        if (showDebugInfo)
        {
            CharacterStats testStats = GetComponent<CharacterStats>();
            if (testStats != null)
            {
                float damage = attackConfig.CalculateDamage(testStats);
                Debug.Log($"[PlayerAttackTest] 💥 Рассчитанный урон: {damage:F1}");
            }
            else
            {
                Debug.Log($"[PlayerAttackTest] 💥 Базовый урон (без CharacterStats): {attackConfig.baseDamage}");
            }
        }

        Debug.Log("═══════════════════════════════════════════");
    }

    void Update()
    {
        // Тест атаки по нажатию Space
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TestAttack();
        }
    }

    void TestAttack()
    {
        if (attackConfig == null)
        {
            Debug.LogError("[PlayerAttackTest] ❌ Не могу атаковать - config не назначен!");
            return;
        }

        // Ищем ближайшего DummyEnemy
        DummyEnemy[] dummies = FindObjectsOfType<DummyEnemy>();
        if (dummies.Length == 0)
        {
            Debug.LogWarning("[PlayerAttackTest] ⚠️ Нет DummyEnemy в сцене!");
            return;
        }

        // Берём первого
        DummyEnemy target = dummies[0];

        // Рассчитываем урон
        float damage = attackConfig.baseDamage;
        CharacterStats stats = GetComponent<CharacterStats>();
        if (stats != null)
        {
            damage = attackConfig.CalculateDamage(stats);
        }

        // Наносим урон
        target.TakeDamage(damage);

        Debug.Log($"[PlayerAttackTest] ⚡ АТАКА! Нанесено {damage:F1} урона врагу {target.gameObject.name}");
    }

    // Отрисовка подсказок
    void OnDrawGizmos()
    {
        if (attackConfig != null)
        {
            // Показываем дальность атаки
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, attackConfig.attackRange);
        }
    }
}
