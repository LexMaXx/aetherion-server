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
    [SerializeField] private GameObject hitEffect; // Эффект при попадании (prefab)
    [SerializeField] private float rotationSpeed = 360f; // Скорость вращения (градусы/сек)

    [Header("Audio")]
    [SerializeField] private AudioClip hitSound; // Звук попадания

    private Transform target; // Цель
    private float damage; // Урон
    private Vector3 direction; // Направление полета
    private float spawnTime; // Время создания
    private Transform visualTransform; // Трансформ визуальной части (для вращения)
    private GameObject owner; // Владелец снаряда (кто его выпустил) - для игнорирования коллизий
    private List<EffectConfig> effects; // Эффекты скилла (горение, отравление и т.д.)

    /// <summary>
    /// Инициализация снаряда
    /// </summary>
    public void Initialize(Transform targetTransform, float projectileDamage, Vector3 initialDirection, GameObject projectileOwner = null, List<EffectConfig> skillEffects = null)
    {
        target = targetTransform;
        damage = projectileDamage;
        direction = initialDirection.normalized;
        spawnTime = Time.time;
        owner = projectileOwner;
        effects = skillEffects;

        Debug.Log($"[Projectile] 🚀 Initialize: target={target?.name ?? "null"}, damage={damage}, speed={speed}, lifetime={lifetime}, homing={homing}");

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

        // Проверяем collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Debug.Log($"[Projectile] ✅ Collider найден: {col.GetType().Name}, isTrigger={col.isTrigger}");
        }
        else
        {
            Debug.LogWarning($"[Projectile] ❌ Collider НЕ найден! Снаряд не сможет попадать в цели!");
        }
    }

    /// <summary>
    /// Установить эффект попадания из BasicAttackConfig
    /// </summary>
    public void SetHitEffect(GameObject effect)
    {
        hitEffect = effect;
        Debug.Log($"[Projectile] 🎨 Hit effect установлен: {effect?.name ?? "None"}");
    }

    /// <summary>
    /// Установить звук попадания из SkillConfig
    /// </summary>
    public void SetHitSound(AudioClip sound)
    {
        hitSound = sound;
        Debug.Log($"[Projectile] 🔊 Hit sound установлен: {sound?.name ?? "None"}");
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
        effects = null; // Старая система SkillData не поддерживает новые эффекты

        // Применяем настройки из SkillData
        speed = skill.projectileSpeed;
        homing = skill.projectileHoming;
        lifetime = skill.projectileLifetime;

        // Применяем hitEffect из SkillData
        if (skill.projectileHitEffectPrefab != null)
        {
            // Создаем временную ссылку на prefab эффекта попадания
            hitEffect = skill.projectileHitEffectPrefab;
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

    /// <summary>
    /// Инициализация снаряда для серверной синхронизации (НОВОЕ)
    /// </summary>
    public void Initialize(Vector3 initialDirection, float projectileSpeed, float projectileLifetime, GameObject projectileOwner = null)
    {
        direction = initialDirection.normalized;
        speed = projectileSpeed;
        lifetime = projectileLifetime;
        spawnTime = Time.time;
        owner = projectileOwner;

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

        Debug.Log($"[Projectile] 🚀 Инициализирован для сервера: speed={speed}, lifetime={lifetime}, direction={direction}");
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
            DummyEnemy dummy = target.GetComponent<DummyEnemy>();
            NetworkPlayer networkTarget = target.GetComponent<NetworkPlayer>();

            // КРИТИЧЕСКОЕ ИЗМЕНЕНИЕ: Проверяем, это NetworkPlayer (другой игрок)?
            if (networkTarget != null)
            {
                if (PartyManager.Instance != null && PartyManager.Instance.IsAlly(networkTarget.socketId))
                {
                    Debug.Log($"[Projectile] ⛔ {networkTarget.username} является союзником, урон отменён.");
                    DestroySelf();
                    return;
                }

                // Это другой игрок в мультиплеере - отправляем урон через NetworkCombatSync!
                Debug.Log($"[Projectile] 🎯 Попадание в игрока {networkTarget.username}! Отправляем урон на сервер...");

                // Находим владельца снаряда (того кто его выпустил)
                if (owner != null)
                {
                    NetworkCombatSync ownerSync = owner.GetComponent<NetworkCombatSync>();
                    if (ownerSync != null)
                    {
                        // Отправляем урон на сервер (сервер рассчитает урон с учетом SPECIAL stats)
                        ownerSync.SendAttack(target.gameObject, damage, "skill");
                        Debug.Log($"[Projectile] ✅ Урон от скилла отправлен на сервер через NetworkCombatSync!");
                    }
                    else
                    {
                        Debug.LogWarning($"[Projectile] ⚠️ NetworkCombatSync не найден у владельца снаряда!");
                    }
                }
                else
                {
                    Debug.LogWarning($"[Projectile] ⚠️ Владелец снаряда (owner) не установлен!");
                }

                // КРИТИЧЕСКИ ВАЖНО: Применяем эффекты через ProjectileEffectApplier (Stun, Root, DoT)
                ProjectileEffectApplier effectApplier = GetComponent<ProjectileEffectApplier>();
                Debug.Log($"[Projectile] 🔍 ProjectileEffectApplier найден для NetworkPlayer: {effectApplier != null}");
                if (effectApplier != null)
                {
                    Debug.Log($"[Projectile] ⏭️ Применяем эффекты через ProjectileEffectApplier к {networkTarget.username}");
                    effectApplier.ApplyEffects(target);
                }
                else
                {
                    Debug.Log($"[Projectile] ⚠️ ProjectileEffectApplier НЕ найден, эффекты не будут применены");
                }
            }
            else if (enemy != null && enemy.IsAlive())
            {
                // Это обычный NPC враг - наносим урон локально
                enemy.TakeDamage(damage);
                Debug.Log($"[Projectile] Попадание в NPC! Урон: {damage}");

                // Применяем эффекты через ProjectileEffectApplier (новая система) или ApplyEffects (старая система)
                ProjectileEffectApplier effectApplier = GetComponent<ProjectileEffectApplier>();
                Debug.Log($"[Projectile] 🔍 ProjectileEffectApplier найден: {effectApplier != null}");
                if (effectApplier != null)
                {
                    Debug.Log($"[Projectile] ⏭️ Применяем эффекты через ProjectileEffectApplier (новая система)");
                    effectApplier.ApplyEffects(target);
                }
                else
                {
                    Debug.Log($"[Projectile] ⚠️ ProjectileEffectApplier НЕ найден, используем старую систему");
                    // Старая система: SkillEffect через SkillManager
                    ApplyEffects(target);
                }
            }
            else if (dummy != null && dummy.IsAlive())
            {
                // DummyEnemy - тестовый враг (локальный урон)
                dummy.TakeDamage(damage);
                Debug.Log($"[Projectile] Попадание в DummyEnemy! Урон: {damage}");

                // Применяем эффекты через ProjectileEffectApplier (новая система) или ApplyEffects (старая система)
                ProjectileEffectApplier effectApplier = GetComponent<ProjectileEffectApplier>();
                if (effectApplier != null)
                {
                    Debug.Log($"[Projectile] ⏭️ Применяем эффекты через ProjectileEffectApplier (новая система)");
                    effectApplier.ApplyEffects(target);
                }
                else
                {
                    // Старая система: SkillEffect через SkillManager
                    ApplyEffects(target);
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
        // Безопасная проверка тегов (не вызываем ошибку если тег не определён)
        if (other.tag == "Ground" || other.tag == "Terrain" || other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Debug.Log($"[Projectile] ⏭️ Игнорируем землю/терейн");
            return;
        }

        // Попадание во врага (Enemy tag) или NetworkPlayer или DummyEnemy
        NetworkPlayer networkTarget = other.GetComponent<NetworkPlayer>();
        Enemy enemy = other.GetComponent<Enemy>();
        DummyEnemy dummy = other.GetComponent<DummyEnemy>();

        Debug.Log($"[Projectile] 🎯 NetworkPlayer: {networkTarget != null}, Enemy: {enemy != null}, DummyEnemy: {dummy != null}");

        if (networkTarget != null || enemy != null || dummy != null)
        {
            // Проверяем живой ли враг
            bool isAlive = true;
            if (enemy != null)
            {
                isAlive = enemy.IsAlive();
            }
            else if (dummy != null)
            {
                isAlive = dummy.IsAlive();
            }
            else if (networkTarget != null)
            {
                isAlive = networkTarget.IsAlive();
            }

            if (isAlive)
            {
                // КРИТИЧЕСКОЕ ИЗМЕНЕНИЕ: Проверяем тип цели и отправляем урон правильно
                if (networkTarget != null)
                {
                    // Это другой игрок в мультиплеере - отправляем урон через NetworkCombatSync!
                    Debug.Log($"[Projectile] 🎯 OnTriggerEnter: Попадание в игрока {networkTarget.username}! Отправляем урон на сервер...");

                    // Находим владельца снаряда (того кто его выпустил)
                    if (owner != null)
                    {
                        NetworkCombatSync ownerSync = owner.GetComponent<NetworkCombatSync>();
                        if (ownerSync != null)
                        {
                            // Отправляем урон на сервер (сервер рассчитает урон с учетом SPECIAL stats)
                            ownerSync.SendAttack(other.gameObject, damage, "skill");
                            Debug.Log($"[Projectile] ✅ Урон от скилла отправлен на сервер через NetworkCombatSync!");
                        }
                        else
                        {
                            Debug.LogWarning($"[Projectile] ⚠️ NetworkCombatSync не найден у владельца снаряда!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Projectile] ⚠️ Владелец снаряда (owner) не установлен!");
                    }

                    // Применяем визуальные эффекты (горение и т.д.) на NetworkPlayer
                    ApplyEffects(other.transform);
                }
                else if (enemy != null)
                {
                    // Наносим урон только NPC врагам (локальный урон)
                    enemy.TakeDamage(damage);
                    Debug.Log($"[Projectile] 💥 Попадание в NPC! Урон: {damage}");

                    // Применяем эффекты (горение, отравление и т.д.)
                    ApplyEffects(other.transform);
                }
                else if (dummy != null)
                {
                    // DummyEnemy - тестовый враг (локальный урон)
                    dummy.TakeDamage(damage);
                    Debug.Log($"[Projectile] 💥 Попадание в DummyEnemy! Урон: {damage}");

                    // Применяем эффекты (горение, отравление и т.д.)
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

                // ЗВУК: Воспроизводим hitSound при попадании
                if (hitSound != null)
                {
                    AudioSource.PlayClipAtPoint(hitSound, transform.position);
                    Debug.Log($"[Projectile] 🔊 Hit sound воспроизведён: {hitSound.name}");
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

        // НОВАЯ СИСТЕМА: Используем EffectManager напрямую (не SkillManager)
        EffectManager effectManager = targetTransform.GetComponent<EffectManager>();
        if (effectManager == null)
        {
            // Если нет EffectManager - добавляем (для врагов/игроков)
            effectManager = targetTransform.gameObject.AddComponent<EffectManager>();
            Debug.Log($"[Projectile] ➕ Добавлен EffectManager к {targetTransform.name}");
        }

        foreach (EffectConfig effect in effects)
        {
            Debug.Log($"[Projectile] 🔥 Применяем эффект {effect.effectType}, particleEffectPrefab: {(effect.particleEffectPrefab != null ? effect.particleEffectPrefab.name : "NULL")}");
            // Используем метод AddEffect из EffectManager (он есть для обратной совместимости)
            effectManager.AddEffect(effect, targetTransform);
            Debug.Log($"[Projectile] ✅ Эффект {effect.effectType} применён к {targetTransform.name}");
        }
    }

    /// <summary>
    /// Уничтожение снаряда
    /// </summary>
    private void DestroySelf()
    {
        Debug.Log($"[Projectile] 💥 DestroySelf: lifetime expired or hit target");
        Destroy(gameObject);
    }
}
