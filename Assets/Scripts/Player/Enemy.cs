using UnityEngine;

/// <summary>
/// Компонент врага - маркирует GameObject как врага для системы таргетирования
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Enemy Info")]
    [SerializeField] private string enemyName = "Enemy";
    [SerializeField] private float maxHealth = 200f; // Увеличено для баланса (было 100)

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

        // ДИАГНОСТИКА: Проверяем нежелательные компоненты
        CheckForPlayerComponents();

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
        FogOfWar fogOfWar = FindFirstObjectByType<FogOfWar>();
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
    /// Получить уникальный ID врага (для мультиплеера)
    /// Использует instanceID GameObject который уникален в текущей сессии
    /// </summary>
    public string GetEnemyId()
    {
        return $"enemy_{gameObject.GetInstanceID()}";
    }

    /// <summary>
    /// ДИАГНОСТИКА: Проверка нежелательных компонентов
    /// Враги НЕ должны иметь PlayerController, CharacterStats и т.д.
    /// </summary>
    private void CheckForPlayerComponents()
    {
        Debug.Log($"[Enemy] 🔍 Диагностика компонентов для {gameObject.name}...");

        // Проверяем PlayerController
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            Debug.LogError($"[Enemy] ❌❌❌ КРИТИЧЕСКАЯ ОШИБКА: Враг {gameObject.name} имеет компонент PlayerController!");
            Debug.LogError($"[Enemy] ❌ Это ПРИЧИНА ТЕЛЕПОРТАЦИИ! PlayerController двигает персонажа на основе input!");
            Debug.LogError($"[Enemy] ❌ РЕШЕНИЕ: Удалите PlayerController из Inspector этого врага!");
            Debug.LogError($"[Enemy] ❌ Автоматически удаляю PlayerController...");
            Destroy(playerController);
        }

        // Проверяем CharacterStats
        CharacterStats characterStats = GetComponent<CharacterStats>();
        if (characterStats != null)
        {
            Debug.LogWarning($"[Enemy] ⚠️ ВНИМАНИЕ: Враг {gameObject.name} имеет CharacterStats!");
            Debug.LogWarning($"[Enemy] ⚠️ Ловкость (agility): {characterStats.agility}");
            Debug.LogWarning($"[Enemy] ⚠️ Это может влиять на скорость если есть PlayerController!");
            Debug.LogWarning($"[Enemy] ⚠️ Враги не должны иметь SPECIAL статы!");
        }

        // Проверяем CharacterController
        CharacterController characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            Debug.LogWarning($"[Enemy] ⚠️ Враг {gameObject.name} имеет CharacterController");
            Debug.LogWarning($"[Enemy] ⚠️ Это нормально если враг двигается, но может конфликтовать с физикой");
        }

        // Проверяем NetworkTransform
        NetworkTransform networkTransform = GetComponent<NetworkTransform>();
        if (networkTransform != null)
        {
            Debug.LogWarning($"[Enemy] ⚠️ Враг {gameObject.name} имеет NetworkTransform!");
            Debug.LogWarning($"[Enemy] ⚠️ Враги не должны синхронизироваться по сети (только игроки)!");
        }

        // Проверяем Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log($"[Enemy] ✅ Rigidbody найден: isKinematic={rb.isKinematic}, useGravity={rb.useGravity}");
            if (!rb.isKinematic)
            {
                Debug.LogWarning($"[Enemy] ⚠️ Rigidbody.isKinematic = false - враг может падать/двигаться от физики");
            }
        }

        Debug.Log($"[Enemy] ✅ Диагностика {gameObject.name} завершена");
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
