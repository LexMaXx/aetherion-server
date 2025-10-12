using UnityEngine;

/// <summary>
/// Компонент врага - маркирует GameObject как врага для системы таргетирования
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Enemy Info")]
    [SerializeField] private string enemyName = "Enemy";
    [SerializeField] private float maxHealth = 100f;

    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffectPrefab;

    private float currentHealth;
    private bool isDead = false;

    // Событие смерти врага
    public delegate void DeathHandler(Enemy enemy);
    public event DeathHandler OnDeath;

    void Start()
    {
        currentHealth = maxHealth;

        // Убеждаемся что у объекта есть тег Enemy
        if (!gameObject.CompareTag("Enemy"))
        {
            Debug.LogWarning($"[Enemy] GameObject {gameObject.name} не имеет тег 'Enemy'. Добавьте тег в Unity Editor!");
        }

        // Загружаем Hit Effect из Resources если не назначен в Inspector
        if (hitEffectPrefab == null)
        {
            hitEffectPrefab = Resources.Load<GameObject>("Effects/HitEffect");
            if (hitEffectPrefab == null)
            {
                Debug.LogWarning($"[Enemy] Hit Effect prefab не найден! Создайте HitEffect в Assets/Resources/Effects/");
            }
        }

        // Регистрируемся в системе тумана войны
        FogOfWar fogOfWar = FindObjectOfType<FogOfWar>();
        if (fogOfWar != null)
        {
            fogOfWar.RegisterEnemy(this);
        }
    }

    /// <summary>
    /// Получить урон
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        Debug.Log($"[Enemy] {enemyName} получил {damage} урона. HP: {currentHealth}/{maxHealth}");

        // СПАВН ЭФФЕКТА ПОПАДАНИЯ
        SpawnHitEffect();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Спавн визуального эффекта попадания
    /// </summary>
    private void SpawnHitEffect()
    {
        if (hitEffectPrefab == null)
            return;

        // Позиция эффекта - центр модели врага
        Vector3 hitPosition = transform.position + Vector3.up * 1.0f; // Немного выше центра

        // Спавним эффект
        GameObject effect = Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);

        Debug.Log($"[Enemy] 💥 Эффект попадания создан в позиции {hitPosition}");
    }

    /// <summary>
    /// Смерть врага
    /// </summary>
    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        Debug.Log($"[Enemy] {enemyName} умер!");

        // Вызываем событие смерти
        OnDeath?.Invoke(this);

        // TODO: Добавить анимацию смерти
        // TODO: Добавить эффекты смерти

        // Временно: уничтожаем через 2 секунды
        Destroy(gameObject, 2f);
    }

    /// <summary>
    /// Проверить жив ли враг
    /// </summary>
    public bool IsAlive()
    {
        return !isDead;
    }

    /// <summary>
    /// Получить имя врага
    /// </summary>
    public string GetEnemyName()
    {
        return enemyName;
    }

    /// <summary>
    /// Получить текущее здоровье
    /// </summary>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// Получить максимальное здоровье
    /// </summary>
    public float GetMaxHealth()
    {
        return maxHealth;
    }

    /// <summary>
    /// Получить процент здоровья (0-1)
    /// </summary>
    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }

    /// <summary>
    /// Визуализация в редакторе
    /// </summary>
    private void OnDrawGizmos()
    {
        // Красная сфера над врагом
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
    }
}
