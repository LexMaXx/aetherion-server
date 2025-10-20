using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Celestial Swirl Ball - улучшенный снаряд с автонаведением и эффектами
/// Используется для обычной атаки мага
/// </summary>
public class CelestialProjectile : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float baseSpeed = 15f; // Базовая скорость
    [SerializeField] private float homingSpeed = 8f; // Скорость поворота при наведении
    [SerializeField] private float lifetime = 5f; // Время жизни
    [SerializeField] private float accelerationRate = 1.5f; // Ускорение со временем

    [Header("Homing Settings")]
    [SerializeField] private bool enableHoming = true; // Автонаведение
    [SerializeField] private float homingStartDelay = 0.1f; // Задержка перед началом наведения
    [SerializeField] private float homingRadius = 30f; // Радиус поиска цели
    [SerializeField] private float targetAcquisitionAngle = 90f; // Угол поиска цели

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem hitEffect; // Эффект взрыва
    [SerializeField] private GameObject windEffect; // Эффект ветра вокруг сферы
    [SerializeField] private Light projectileLight; // Свечение
    [SerializeField] private TrailRenderer trail; // След

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 360f; // Вращение визуала
    [SerializeField] private Transform visualTransform; // Трансформ для вращения

    [Header("Audio")]
    [SerializeField] private AudioClip launchSound;
    [SerializeField] private AudioClip hitSound;

    // Runtime variables
    private Transform target;
    private float damage;
    private Vector3 direction;
    private float spawnTime;
    private GameObject owner;
    private List<SkillEffect> effects;
    private float currentSpeed;
    private bool hasHit = false;
    private AudioSource audioSource;

    /// <summary>
    /// Инициализация снаряда
    /// </summary>
    /// <param name="isVisualOnly">Если true, снаряд чисто визуальный (без коллизии, автонаведения, урона)</param>
    public void Initialize(Transform targetTransform, float projectileDamage, Vector3 initialDirection, GameObject projectileOwner = null, List<SkillEffect> skillEffects = null, bool isVisualOnly = false)
    {
        target = targetTransform;
        damage = projectileDamage;
        direction = initialDirection.normalized;
        spawnTime = Time.time;
        owner = projectileOwner;
        effects = skillEffects;
        currentSpeed = baseSpeed;

        // Если это визуальный снаряд - отключаем автонаведение и коллизию
        if (isVisualOnly)
        {
            enableHoming = false;

            // Отключаем коллайдер для визуальных снарядов
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            // Уменьшаем lifetime для визуальных снарядов (1.5 секунды вместо 5)
            // Это примерно время полета на 20-25 метров при скорости 15 м/с
            lifetime = 1.5f;

            Debug.Log($"[CelestialProjectile] 👁️ Визуальный снаряд создан (без коллизии, автонаведения, lifetime: {lifetime}s)");
        }

        // Поворачиваем снаряд по направлению полета
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Настраиваем аудио
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null && launchSound != null)
        {
            audioSource.PlayOneShot(launchSound);
        }

        // Автопоиск компонентов если не назначены
        AutoFindComponents();

        Debug.Log($"[CelestialProjectile] ✨ Создан! Target: {target?.name ?? "None"}, Damage: {damage}, Homing: {enableHoming}, Visual: {isVisualOnly}");
    }

    /// <summary>
    /// Автоматический поиск компонентов
    /// </summary>
    private void AutoFindComponents()
    {
        if (projectileLight == null)
            projectileLight = GetComponentInChildren<Light>();

        if (trail == null)
            trail = GetComponentInChildren<TrailRenderer>();

        if (visualTransform == null && transform.childCount > 0)
            visualTransform = transform.GetChild(0);

        if (windEffect == null)
        {
            // Ищем эффект ветра в дочерних объектах
            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                if (ps.gameObject != gameObject && ps.name.Contains("Wind"))
                {
                    windEffect = ps.gameObject;
                    break;
                }
            }
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

        // Вращение визуальной части
        RotateVisual();

        // Проверка достижения цели
        CheckTargetHit();
    }

    /// <summary>
    /// Движение снаряда с автонаведением
    /// </summary>
    private void MoveProjectile()
    {
        // Ускорение со временем (для более динамичного полета)
        float timeSinceSpawn = Time.time - spawnTime;
        currentSpeed = baseSpeed + (timeSinceSpawn * accelerationRate);

        // Автонаведение
        if (enableHoming && timeSinceSpawn > homingStartDelay)
        {
            // Проверяем есть ли цель
            if (target == null || !IsTargetValid(target))
            {
                // Пытаемся найти новую цель
                target = FindNearestTarget();
            }

            if (target != null)
            {
                // Вычисляем направление к цели
                Vector3 targetDirection = (target.position - transform.position).normalized;

                // Плавно поворачиваем к цели (как управляемая ракета)
                direction = Vector3.RotateTowards(
                    direction,
                    targetDirection,
                    homingSpeed * Time.deltaTime,
                    0f
                ).normalized;

                // Обновляем ротацию снаряда
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        // Двигаем снаряд
        transform.position += direction * currentSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Вращение визуальной части снаряда
    /// </summary>
    private void RotateVisual()
    {
        if (visualTransform != null && rotationSpeed > 0)
        {
            // Вращаем вокруг оси Z (как планета)
            visualTransform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime, Space.Self);

            // Дополнительное медленное вращение вокруг Y (более интересный эффект)
            visualTransform.Rotate(Vector3.up * (rotationSpeed * 0.3f) * Time.deltaTime, Space.Self);
        }
    }

    /// <summary>
    /// Проверка попадания в цель
    /// </summary>
    private void CheckTargetHit()
    {
        if (hasHit || target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Проверяем достижение цели (увеличенный радиус для надежности)
        if (distanceToTarget < 1.0f)
        {
            HitTarget();
        }
    }

    /// <summary>
    /// Попадание в цель
    /// </summary>
    private void HitTarget()
    {
        if (hasHit) return;
        hasHit = true;

        Debug.Log($"[CelestialProjectile] 💥 Попадание в {target?.name}! Урон: {damage}");

        // Наносим урон
        if (target != null)
        {
            // Проверяем тип цели
            Enemy enemy = target.GetComponent<Enemy>();
            NetworkPlayer networkTarget = target.GetComponent<NetworkPlayer>();

            if (enemy != null && enemy.IsAlive() && networkTarget == null)
            {
                // Обычный NPC враг - наносим урон локально
                enemy.TakeDamage(damage);
                Debug.Log($"[CelestialProjectile] ✅ Урон нанесен NPC: {damage}");
            }
            else if (networkTarget != null)
            {
                // Сетевой игрок - урон наносится через сервер (в NetworkCombatSync)
                Debug.Log($"[CelestialProjectile] 🌐 Попадание в сетевого игрока - урон через сервер");
            }

            // Применяем эффекты скилла (если есть)
            if (effects != null && effects.Count > 0)
            {
                SkillManager targetSkillManager = target.GetComponent<SkillManager>();
                if (targetSkillManager != null)
                {
                    // Используем публичный метод AddEffect из SkillManager
                    foreach (var effect in effects)
                    {
                        targetSkillManager.AddEffect(effect, target);
                    }
                    Debug.Log($"[CelestialProjectile] ✨ Применено эффектов: {effects.Count}");
                }
                else
                {
                    Debug.LogWarning($"[CelestialProjectile] ⚠️ У цели {target.name} нет SkillManager для применения эффектов!");
                }
            }
        }

        // Создаем эффект взрыва
        SpawnHitEffect();

        // Звук попадания
        if (audioSource != null && hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }

        // Синхронизация визуального эффекта взрыва для мультиплеера
        if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
        {
            string effectName = hitEffect != null ? hitEffect.name : "CFXR3 Fire Explosion B";
            // Правильный порядок параметров: effectType, effectPrefabName, position, rotation, targetSocketId, duration, parentTransform
            SocketIOManager.Instance.SendVisualEffect(
                "explosion",              // effectType
                effectName,               // effectPrefabName
                transform.position,       // position
                Quaternion.identity,      // rotation
                "",                       // targetSocketId (пусто для всех)
                0f,                       // duration
                null                      // parentTransform
            );
            Debug.Log($"[CelestialProjectile] 📡 Эффект взрыва отправлен на сервер: {effectName}");
        }

        // Уничтожаем снаряд
        DestroySelf();
    }

    /// <summary>
    /// Создание эффекта взрыва при попадании
    /// </summary>
    private void SpawnHitEffect()
    {
        if (hitEffect != null)
        {
            ParticleSystem explosion = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(explosion.gameObject, 3f);
            Debug.Log($"[CelestialProjectile] 💥 Эффект взрыва создан: {hitEffect.name}");
        }
        else
        {
            Debug.LogWarning("[CelestialProjectile] ⚠️ Hit effect не назначен!");
        }
    }

    /// <summary>
    /// Поиск ближайшей подходящей цели
    /// </summary>
    private Transform FindNearestTarget()
    {
        // Ищем все коллайдеры врагов в радиусе
        Collider[] colliders = Physics.OverlapSphere(transform.position, homingRadius);
        Transform nearestTarget = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider col in colliders)
        {
            // Проверяем что это враг и не владелец снаряда
            if (col.gameObject == owner) continue;

            Enemy enemy = col.GetComponent<Enemy>();
            NetworkPlayer networkPlayer = col.GetComponent<NetworkPlayer>();

            if (enemy != null && enemy.IsAlive())
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);

                // Проверяем угол (снаряд не должен разворачиваться на 180°)
                Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(direction, directionToTarget);

                if (angle <= targetAcquisitionAngle && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = col.transform;
                }
            }
            else if (networkPlayer != null)
            {
                // Сетевой игрок тоже может быть целью
                float distance = Vector3.Distance(transform.position, col.transform.position);
                Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(direction, directionToTarget);

                if (angle <= targetAcquisitionAngle && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = col.transform;
                }
            }
        }

        if (nearestTarget != null)
        {
            Debug.Log($"[CelestialProjectile] 🎯 Новая цель найдена: {nearestTarget.name} на расстоянии {nearestDistance:F1}m");
        }

        return nearestTarget;
    }

    /// <summary>
    /// Проверка валидности цели
    /// </summary>
    private bool IsTargetValid(Transform target)
    {
        if (target == null) return false;

        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy != null)
        {
            return enemy.IsAlive();
        }

        // Сетевой игрок всегда валиден (проверка HP на его стороне)
        NetworkPlayer networkPlayer = target.GetComponent<NetworkPlayer>();
        if (networkPlayer != null)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Уничтожение снаряда
    /// </summary>
    private void DestroySelf()
    {
        Debug.Log($"[CelestialProjectile] 🗑️ Уничтожение снаряда");
        Destroy(gameObject);
    }

    /// <summary>
    /// Обработка столкновений с триггерами
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Игнорируем владельца
        if (other.gameObject == owner) return;

        // Проверяем попадание в цель
        if (other.transform == target)
        {
            HitTarget();
        }
        // Или попадание в любого врага
        else
        {
            Enemy enemy = other.GetComponent<Enemy>();
            NetworkPlayer networkPlayer = other.GetComponent<NetworkPlayer>();

            if ((enemy != null && enemy.IsAlive()) || networkPlayer != null)
            {
                target = other.transform;
                HitTarget();
            }
        }
    }

    /// <summary>
    /// Визуализация в редакторе
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (enableHoming)
        {
            // Радиус поиска цели
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, homingRadius);

            // Направление полета
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, direction * 5f);

            // Линия к цели
            if (target != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
    }
}
