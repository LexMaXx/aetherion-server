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

    /// <summary>
    /// Инициализация снаряда
    /// </summary>
    public void Initialize(Transform targetTransform, float projectileDamage, Vector3 initialDirection)
    {
        target = targetTransform;
        damage = projectileDamage;
        direction = initialDirection.normalized;
        spawnTime = Time.time;

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
                enemy.TakeDamage(damage);
                Debug.Log($"[Projectile] Попадание! Урон: {damage}");
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
        // Попадание во врага
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null && enemy.IsAlive())
            {
                enemy.TakeDamage(damage);
                Debug.Log($"[Projectile] Collision попадание! Урон: {damage}");
            }

            // Эффект попадания
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }

            DestroySelf();
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
