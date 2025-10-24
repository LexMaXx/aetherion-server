using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Arrow Projectile - улучшенный снаряд стрелы с автонаведением
/// Используется для обычной атаки лучника
/// </summary>
public class ArrowProjectile : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float baseSpeed = 25f; // Базовая скорость
    [SerializeField] private float homingSpeed = 10f; // Скорость поворота при наведении
    [SerializeField] private float lifetime = 5f; // Время жизни
    [SerializeField] private float accelerationRate = 1.2f; // Ускорение со временем

    [Header("Homing Settings")]
    [SerializeField] private bool enableHoming = true; // Автонаведение
    [SerializeField] private float homingStartDelay = 0.05f; // Задержка перед началом наведения
    [SerializeField] private float homingRadius = 35f; // Радиус поиска цели
    [SerializeField] private float targetAcquisitionAngle = 90f; // Угол поиска цели

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem hitEffect; // Эффект взрыва
    [SerializeField] private Light projectileLight; // Свечение
    [SerializeField] private TrailRenderer trail; // След

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 0f; // Вращение визуала
    [SerializeField] private Transform visualTransform; // Трансформ для вращения

    [Header("Audio")]
    [SerializeField] private AudioClip launchSound;
    [SerializeField] private AudioClip hitSound;

    // Runtime variables
    private Transform target;
    private float damage;
    private bool isCritical = false;
    private Vector3 direction;
    private float spawnTime;
    private GameObject owner;
    private List<EffectConfig> effects; // Эффекты скилла
    private CharacterStats casterStats; // Статы кастера для эффектов
    private float currentSpeed;
    private bool hasHit = false;
    private AudioSource audioSource;

    /// <summary>
    /// Инициализация снаряда
    /// </summary>
    /// <param name="isVisualOnly">Если true, снаряд чисто визуальный (без коллизии, автонаведения, урона)</param>
    public void Initialize(Transform targetTransform, float projectileDamage, Vector3 initialDirection, GameObject projectileOwner = null, List<EffectConfig> skillEffects = null, bool isVisualOnly = false, bool isCrit = false)
    {
        target = targetTransform;
        damage = projectileDamage;
        isCritical = isCrit;
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

            Debug.Log($"[ArrowProjectile] 👁️ Визуальный снаряд создан (без коллизии, автонаведения, lifetime: {lifetime}s)");
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

        Debug.Log($"[ArrowProjectile] ✨ Создан! Target: {target?.name ?? "None"}, Damage: {damage}, Homing: {enableHoming}, Visual: {isVisualOnly}");
    }

    /// <summary>
    /// Инициализация снаряда с новой системой эффектов (EffectConfig)
    /// </summary>
    public void InitializeWithEffects(Transform targetTransform, float projectileDamage, Vector3 initialDirection, GameObject projectileOwner, List<EffectConfig> skillEffectConfigs, CharacterStats stats, bool isVisualOnly = false, bool isCrit = false)
    {
        // Вызываем базовую инициализацию
        Initialize(targetTransform, projectileDamage, initialDirection, projectileOwner, null, isVisualOnly, isCrit);

        // Сохраняем эффекты новой системы
        effects = skillEffectConfigs;
        casterStats = stats;

        if (effects != null && effects.Count > 0)
        {
            Debug.Log("[ArrowProjectile] Initialized with " + effects.Count + " effects (new system)");
        }
    }

    /// <summary>
    /// Установить эффект попадания из BasicAttackConfig
    /// </summary>
    public void SetHitEffect(ParticleSystem effect)
    {
        hitEffect = effect;
        Debug.Log($"[ArrowProjectile] 🎨 Hit effect установлен: {effect?.name ?? "None"}");
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

        {
            // Ищем эффект ветра в дочерних объектах
            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                if (ps.gameObject != gameObject && ps.name.Contains("Wind"))
                {
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

        Debug.Log($"[ArrowProjectile] 💥 Попадание в {target?.name}! Урон: {damage}");

        // Наносим урон
        if (target != null)
        {
            // Проверяем тип цели
            Enemy enemy = target.GetComponent<Enemy>();
            DummyEnemy dummy = target.GetComponent<DummyEnemy>();
            NetworkPlayer networkTarget = target.GetComponent<NetworkPlayer>();

            if (enemy != null && enemy.IsAlive() && networkTarget == null)
            {
                // Обычный NPC враг - наносим урон локально
                enemy.TakeDamage(damage);
                Debug.Log($"[ArrowProjectile] ✅ Урон нанесен NPC: {damage}");

                // Показываем цифру урона
                if (DamageNumberManager.Instance != null)
                {
                    DamageNumberManager.Instance.ShowDamage(target.position, damage, isCritical);
                }
            }
            else if (dummy != null && dummy.IsAlive())
            {
                // DummyEnemy - тестовый враг
                dummy.TakeDamage(damage);
                Debug.Log($"[ArrowProjectile] ✅ Урон нанесен DummyEnemy: {damage}");

                // Показываем цифру урона
                if (DamageNumberManager.Instance != null)
                {
                    DamageNumberManager.Instance.ShowDamage(target.position, damage, isCritical);
                }
            }
            else if (networkTarget != null)
            {
                // КРИТИЧЕСКОЕ ИЗМЕНЕНИЕ: Отправляем урон через NetworkCombatSync!
                Debug.Log($"[ArrowProjectile] 🎯 Попадание в игрока {networkTarget.username}! Отправляем урон на сервер...");

                // Находим владельца снаряда (того кто его выпустил)
                if (owner != null)
                {
                    NetworkCombatSync ownerSync = owner.GetComponent<NetworkCombatSync>();
                    if (ownerSync != null)
                    {
                        // Отправляем урон на сервер (сервер рассчитает урон с учетом SPECIAL stats)
                        ownerSync.SendAttack(target.gameObject, damage, "skill");
                        Debug.Log($"[ArrowProjectile] ✅ Урон от скилла отправлен на сервер через NetworkCombatSync!");
                    }
                    else
                    {
                        Debug.LogWarning($"[ArrowProjectile] ⚠️ NetworkCombatSync не найден у владельца снаряда!");
                    }
                }
                else
                {
                    Debug.LogWarning($"[ArrowProjectile] ⚠️ Владелец снаряда (owner) не установлен!");
                }
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
                    Debug.Log($"[ArrowProjectile] ✨ Применено эффектов: {effects.Count}");
                }
                else
                {
                    Debug.LogWarning($"[ArrowProjectile] ⚠️ У цели {target.name} нет SkillManager для применения эффектов!");
                }
            }

            // Новая система эффектов (EffectConfig + EffectManager)
            if (effects != null && effects.Count > 0)
            {
                EffectManager targetEffectManager = target.GetComponent<EffectManager>();
                if (targetEffectManager != null)
                {
                    foreach (var effectConfig in effects)
                    {
                        targetEffectManager.ApplyEffect(effectConfig, casterStats);
                    }
                    Debug.Log("[ArrowProjectile] Applied " + effects.Count + " effects (new system)");
                }
                else
                {
                    Debug.LogWarning("[ArrowProjectile] Target " + target.name + " has no EffectManager!");
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
            Debug.Log($"[ArrowProjectile] 📡 Эффект взрыва отправлен на сервер: {effectName}");
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
            Debug.Log($"[ArrowProjectile] 💥 Эффект взрыва создан: {hitEffect.name}");
        }
        else
        {
            Debug.LogWarning("[ArrowProjectile] ⚠️ Hit effect не назначен!");
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
            Debug.Log($"[ArrowProjectile] 🎯 Новая цель найдена: {nearestTarget.name} на расстоянии {nearestDistance:F1}m");
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

        DummyEnemy dummy = target.GetComponent<DummyEnemy>();
        if (dummy != null)
        {
            return dummy.IsAlive();
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
        Debug.Log($"[ArrowProjectile] 🗑️ Уничтожение снаряда");
        Destroy(gameObject);
    }

    /// <summary>
    /// Обработка столкновений с триггерами
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[ArrowProjectile] ⚡ OnTriggerEnter: {other.gameObject.name}, hasHit={hasHit}");

        if (hasHit) return;

        // Игнорируем владельца
        if (other.gameObject == owner)
        {
            Debug.Log($"[ArrowProjectile] ⏭️ Пропуск владельца: {other.gameObject.name}");
            return;
        }

        // Проверяем попадание в цель
        if (other.transform == target)
        {
            Debug.Log($"[ArrowProjectile] 🎯 Попадание в цель: {target.name}");
            HitTarget();
        }
        // Или попадание в любого врага
        else
        {
            Enemy enemy = other.GetComponent<Enemy>();
            NetworkPlayer networkPlayer = other.GetComponent<NetworkPlayer>();

            Debug.Log($"[ArrowProjectile] 🔍 Проверка: enemy={enemy != null}, networkPlayer={networkPlayer != null}");

            if ((enemy != null && enemy.IsAlive()) || networkPlayer != null)
            {
                Debug.Log($"[ArrowProjectile] ✅ Попадание в врага: {other.gameObject.name}");
                target = other.transform;
                HitTarget();
            }
            else
            {
                Debug.Log($"[ArrowProjectile] ❌ Не враг, пропуск: {other.gameObject.name}");
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
