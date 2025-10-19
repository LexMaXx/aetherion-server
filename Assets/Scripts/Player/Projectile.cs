using UnityEngine;
using System.Collections.Generic;

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
    private List<SkillEffect> effects; // Эффекты скилла (горение, отравление и т.д.)

    /// <summary>
    /// Инициализация снаряда
    /// </summary>
    public void Initialize(Transform targetTransform, float projectileDamage, Vector3 initialDirection, GameObject projectileOwner = null, List<SkillEffect> skillEffects = null)
    {
        target = targetTransform;
        damage = projectileDamage;
        direction = initialDirection.normalized;
        spawnTime = Time.time;
        owner = projectileOwner;
        effects = skillEffects;

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

    /// <summary>
    /// Инициализация снаряда с настройками из SkillData (НОВОЕ)
    /// </summary>
    public void InitializeFromSkill(SkillData skill, Transform targetTransform, Vector3 initialDirection, GameObject projectileOwner = null)
    {
        target = targetTransform;
        damage = skill.baseDamageOrHeal;
        direction = initialDirection.normalized;
        spawnTime = Time.time;
        owner = projectileOwner;
        effects = skill.effects;

        // Применяем настройки из SkillData
        speed = skill.projectileSpeed;
        homing = skill.projectileHoming;
        lifetime = skill.projectileLifetime;

        // Применяем hitEffect из SkillData
        if (skill.projectileHitEffectPrefab != null)
        {
            // Создаем временную ссылку на prefab эффекта попадания
            hitEffect = skill.projectileHitEffectPrefab.GetComponent<ParticleSystem>();
        }

        // Ищем дочерний объект для вращения (если есть)
        if (transform.childCount > 0)
        {
            visualTransform = transform.GetChild(0);
        }

        // Поворачиваем снаряд по направлению полета
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        Debug.Log($"[Projectile] Инициализирован из SkillData: speed={speed}, homing={homing}, lifetime={lifetime}");
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

                    // Применяем эффекты (горение, отравление и т.д.)
                    ApplyEffects(target);
                }
                else
                {
                    // Это NetworkPlayer - урон уже отправлен на сервер через PlayerAttack
                    Debug.Log($"[Projectile] Попадание в NetworkPlayer {networkTarget.username}! Урон применит сервер");
                }
            }
        }

        // Эффект попадания (взрыв, искры и т.д.)
        if (hitEffect != null)
        {
            GameObject effectObj = Instantiate(hitEffect, transform.position, Quaternion.identity);

            // СИНХРОНИЗАЦИЯ: Отправляем визуальный эффект на сервер для мультиплеера
            if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
            {
                // Определяем название prefab эффекта для загрузки на других клиентах
                string effectName = hitEffect.name.Replace("(Clone)", "").Trim();
                SocketIOManager.Instance.SendVisualEffect(
                    "explosion", // тип эффекта
                    effectName, // название prefab
                    transform.position, // позиция взрыва
                    Quaternion.identity, // ротация
                    "", // не привязан к игроку (world space)
                    0f // длительность (0 = автоматически через ParticleSystem)
                );
                Debug.Log($"[Projectile] ✨ Эффект попадания отправлен на сервер: {effectName}");
            }
        }

        DestroySelf();
    }

    /// <summary>
    /// Столкновение с коллайдером
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Projectile] ⚡ OnTriggerEnter: {other.gameObject.name}, tag: {other.tag}");

        // Игнорируем владельца снаряда (не попадаем в себя)
        if (owner != null && other.gameObject == owner)
        {
            Debug.Log($"[Projectile] ⏭️ Игнорируем владельца");
            return;
        }

        // Игнорируем коллизии с землёй и другими не-целевыми объектами
        if (other.CompareTag("Ground") || other.CompareTag("Terrain"))
        {
            Debug.Log($"[Projectile] ⏭️ Игнорируем землю/терейн");
            return;
        }

        // Попадание во врага (Enemy tag) или NetworkPlayer
        NetworkPlayer networkTarget = other.GetComponent<NetworkPlayer>();
        Enemy enemy = other.GetComponent<Enemy>();

        Debug.Log($"[Projectile] 🎯 NetworkPlayer: {networkTarget != null}, Enemy: {enemy != null}");

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
                    Debug.Log($"[Projectile] 💥 Попадание в NPC! Урон: {damage}");

                    // Применяем эффекты (горение, отравление и т.д.)
                    ApplyEffects(other.transform);
                }
                else if (networkTarget != null)
                {
                    // NetworkPlayer - урон применит сервер, но визуальные эффекты применяем локально!
                    Debug.Log($"[Projectile] 💥 Попадание в NetworkPlayer {networkTarget.username}! Урон применит сервер");

                    // Применяем визуальные эффекты (горение и т.д.) на NetworkPlayer
                    ApplyEffects(other.transform);
                }

                // Эффект попадания (взрыв, искры и т.д.)
                if (hitEffect != null)
                {
                    GameObject effectObj = Instantiate(hitEffect, transform.position, Quaternion.identity);

                    // СИНХРОНИЗАЦИЯ: Отправляем визуальный эффект на сервер для мультиплеера
                    if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
                    {
                        // Определяем название prefab эффекта для загрузки на других клиентах
                        string effectName = hitEffect.name.Replace("(Clone)", "").Trim();
                        SocketIOManager.Instance.SendVisualEffect(
                            "explosion", // тип эффекта
                            effectName, // название prefab
                            transform.position, // позиция взрыва
                            Quaternion.identity, // ротация
                            "", // не привязан к игроку (world space)
                            0f // длительность (0 = автоматически через ParticleSystem)
                        );
                        Debug.Log($"[Projectile] ✨ Эффект попадания (OnTriggerEnter) отправлен на сервер: {effectName}");
                    }
                }

                DestroySelf();
            }
        }
    }

    /// <summary>
    /// Применить эффекты к цели
    /// </summary>
    private void ApplyEffects(Transform targetTransform)
    {
        if (effects == null || effects.Count == 0)
        {
            Debug.Log($"[Projectile] ⚠️ Нет эффектов для применения");
            return;
        }

        Debug.Log($"[Projectile] 🔥 Применяем {effects.Count} эффектов к {targetTransform.name}");

        SkillManager skillManager = targetTransform.GetComponent<SkillManager>();
        if (skillManager == null)
        {
            // Если нет SkillManager - добавляем (для врагов)
            skillManager = targetTransform.gameObject.AddComponent<SkillManager>();
            Debug.Log($"[Projectile] ➕ Добавлен SkillManager к {targetTransform.name}");
        }

        foreach (SkillEffect effect in effects)
        {
            Debug.Log($"[Projectile] 🔥 Применяем эффект {effect.effectType}, particleEffectPrefab: {(effect.particleEffectPrefab != null ? effect.particleEffectPrefab.name : "NULL")}");
            skillManager.AddEffect(effect, targetTransform);
            Debug.Log($"[Projectile] ✅ Эффект {effect.effectType} применён к {targetTransform.name}");
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
