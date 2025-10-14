using UnityEngine;

/// <summary>
/// Снаряд (стрела, магический шар, эффект души)
/// Летит к цели и наносит урон при попадании
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 20f; // Скорость полета
    [SerializeField] private float lifetime = 5f; // Время жизни (секунды)
    [SerializeField] private bool homing = false; // Следовать за целью?

    [Header("Visual Settings")]
    [SerializeField] private TrailRenderer trail; // След за снарядом (опционально)
    [SerializeField] private ParticleSystem hitEffect; // Эффект при попадании
    [SerializeField] private float rotationSpeed = 360f; // Скорость вращения (градусы/сек)

    private Transform target; // Цель
    private float damage; // Урон
    private Vector3 direction; // Направление полета
    private float spawnTime; // Время создания
    private Transform visualTransform; // Трансформ визуальной части (для вращения)
    private GameObject owner; // Владелец снаряда (кто его выпустил) - для игнорирования коллизий

    /// <summary>
    /// Инициализация снаряда
    /// </summary>
    public void Initialize(Transform targetTransform, float projectileDamage, Vector3 initialDirection, GameObject projectileOwner = null)
    {
        target = targetTransform;
        damage = projectileDamage;
        direction = initialDirection.normalized;
        spawnTime = Time.time;
        owner = projectileOwner;

        // Ищем дочерний объект для вращения (если есть)
        if (transform.childCount > 0)
        {
            visualTransform = transform.GetChild(0); // Первый ребенок = визуал
        }

        // Поворачиваем снаряд по направлению полета
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void Update()
    {
        // Проверяем время жизни
        if (Time.time - spawnTime > lifetime)
        {
            DestroySelf();
            return;
        }

        // Движение снаряда
        MoveProjectile();

        // Вращение визуальной части (для красоты)
        if (visualTransform != null && rotationSpeed > 0)
        {
            visualTransform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime, Space.Self);
        }

        // Проверка достижения цели
        if (target != null && Vector3.Distance(transform.position, target.position) < 0.5f)
        {
            HitTarget();
        }
    }

    /// <summary>
    /// Движение снаряда
    /// </summary>
    private void MoveProjectile()
    {
        if (homing && target != null)
        {
            // Самонаведение на цель
            direction = (target.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Двигаем снаряд
        transform.position += direction * speed * Time.deltaTime;
    }

    /// <summary>
    /// Попадание в цель
    /// </summary>
    private void HitTarget()
    {
        if (target != null)
        {
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null && enemy.IsAlive())
            {
                // ВАЖНО: В мультиплеере НЕ наносим урон NetworkPlayer локально!
                NetworkPlayer networkTarget = target.GetComponent<NetworkPlayer>();
                if (networkTarget == null)
                {
                    // Это обычный NPC враг - наносим урон локально
                    enemy.TakeDamage(damage);
                    Debug.Log($"[Projectile] Попадание в NPC! Урон: {damage}");
                }
                else
                {
                    // Это NetworkPlayer - урон уже отправлен на сервер через PlayerAttack
                    Debug.Log($"[Projectile] Попадание в NetworkPlayer {networkTarget.username}! Урон применит сервер");
                }
            }
        }

        // Эффект попадания
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        DestroySelf();
    }

    /// <summary>
    /// Столкновение с коллайдером
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Игнорируем владельца снаряда (не попадаем в себя)
        if (owner != null && other.gameObject == owner)
        {
            return;
        }

        // Игнорируем коллизии с землёй и другими не-целевыми объектами
        if (other.CompareTag("Ground") || other.CompareTag("Terrain"))
        {
            return;
        }

        // Попадание во врага (Enemy tag) или NetworkPlayer
        NetworkPlayer networkTarget = other.GetComponent<NetworkPlayer>();
        Enemy enemy = other.GetComponent<Enemy>();

        if (networkTarget != null || enemy != null)
        {
            // Проверяем живой ли враг
            bool isAlive = true;
            if (enemy != null)
            {
                isAlive = enemy.IsAlive();
            }
            else if (networkTarget != null)
            {
                isAlive = networkTarget.IsAlive;
            }

            if (isAlive)
            {
                // Наносим урон только NPC врагам (NetworkPlayer получает урон через сервер)
                if (networkTarget == null && enemy != null)
                {
                    enemy.TakeDamage(damage);
                    Debug.Log($"[Projectile] Попадание в NPC! Урон: {damage}");
                }
                else if (networkTarget != null)
                {
                    Debug.Log($"[Projectile] Попадание в NetworkPlayer {networkTarget.username}! Урон применит сервер");
                }

                // Эффект попадания
                if (hitEffect != null)
                {
                    Instantiate(hitEffect, transform.position, Quaternion.identity);
                }

                DestroySelf();
            }
        }
    }

    /// <summary>
    /// Уничтожение снаряда
    /// </summary>
    private void DestroySelf()
    {
        Destroy(gameObject);
    }
}
