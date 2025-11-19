using UnityEngine;

/// <summary>
/// Компонент для DummyEnemy который атакует игрока каждую секунду
/// Отображает урон через DamageNumberManager
/// </summary>
public class DummyEnemyAttacker : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackInterval = 1f; // Атака каждую секунду
    [SerializeField] private float baseDamage = 10f; // Базовый урон
    [SerializeField] private float damageVariation = 2f; // Случайная вариация урона ±2
    [SerializeField] private float critChance = 10f; // 10% шанс крита
    [SerializeField] private float critMultiplier = 2f; // x2 урон при крите

    [Header("Target")]
    [SerializeField] private string playerTag = "Player"; // Тег игрока

    private float attackTimer = 0f;
    private Transform player;
    private HealthSystem playerHealthSystem;
    private DamageNumberManager damageNumberManager;

    void Start()
    {
        // Находим игрока
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealthSystem = player.GetComponent<HealthSystem>();

            Debug.Log($"[DummyEnemyAttacker] Найден игрок: {player.name}");
        }
        else
        {
            Debug.LogWarning($"[DummyEnemyAttacker] Игрок с тегом '{playerTag}' не найден!");
        }

        // Находим DamageNumberManager
        damageNumberManager = FindObjectOfType<DamageNumberManager>();
        if (damageNumberManager == null)
        {
            Debug.LogWarning("[DummyEnemyAttacker] DamageNumberManager не найден в сцене!");
        }

        // Начинаем с задержки
        attackTimer = attackInterval;
    }

    void Update()
    {
        // Проверяем что игрок найден
        if (player == null)
        {
            return;
        }

        // Таймер атаки
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            AttackPlayer();
            attackTimer = attackInterval; // Сбрасываем таймер
        }
    }

    void AttackPlayer()
    {
        if (player == null)
        {
            return;
        }

        // Рассчитываем урон
        float damage = baseDamage + Random.Range(-damageVariation, damageVariation);

        // Проверяем крит
        bool isCrit = Random.Range(0f, 100f) < critChance;
        if (isCrit)
        {
            damage *= critMultiplier;
        }

        // Наносим урон игроку через HealthSystem
        if (playerHealthSystem != null)
        {
            playerHealthSystem.TakeDamage(damage);
            Debug.Log($"[DummyEnemyAttacker] ⚔️ Атака! Урон: {damage:F1} {(isCrit ? "КРИТ!" : "")}");
        }
        else
        {
            Debug.Log($"[DummyEnemyAttacker] ⚔️ Атака (без HealthSystem)! Урон: {damage:F1} {(isCrit ? "КРИТ!" : "")}");
        }

        // Показываем цифры урона над игроком
        if (damageNumberManager != null)
        {
            Vector3 damagePosition = player.position + Vector3.up * 2f; // Над головой игрока
            damageNumberManager.ShowDamage(damagePosition, damage, isCrit, false);
        }

        // Можно добавить визуальный эффект атаки
        ShowAttackEffect();
    }

    void ShowAttackEffect()
    {
        // Простая визуализация - можно добавить particle эффект
        Debug.DrawLine(transform.position + Vector3.up, player.position + Vector3.up, Color.red, 0.5f);
    }

    // Для отладки - визуализация в редакторе
    void OnDrawGizmos()
    {
        if (player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up, player.position + Vector3.up);
        }
    }

    // Публичные методы для настройки

    /// <summary>
    /// Установить интервал между атаками
    /// </summary>
    public void SetAttackInterval(float interval)
    {
        attackInterval = interval;
        Debug.Log($"[DummyEnemyAttacker] Интервал атаки изменён на {interval}с");
    }

    /// <summary>
    /// Установить базовый урон
    /// </summary>
    public void SetBaseDamage(float damage)
    {
        baseDamage = damage;
        Debug.Log($"[DummyEnemyAttacker] Базовый урон изменён на {damage}");
    }

    /// <summary>
    /// Включить/выключить атаки
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
        Debug.Log($"[DummyEnemyAttacker] Атаки {(enabled ? "включены" : "выключены")}");
    }
}
