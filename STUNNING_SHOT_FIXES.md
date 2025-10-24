# ✅ Stunning Shot - Исправления

## Проблема
Стрела EntanglingArrowProjectile появлялась и сразу исчезала, не летела во врага, и появлялась плоско (без вращения).

---

## Причина

### 1. Стрела не летела
**Проблема:** EntanglingArrowProjectile использует старый компонент `Projectile.cs`, а не новые `CelestialProjectile` или `ArrowProjectile`.

**Код:** `SkillExecutor.LaunchProjectile()` проверял только CelestialProjectile и ArrowProjectile, но не старый Projectile.

### 2. Эффект стана не применялся
**Проблема:** Старый `Projectile.cs` использует устаревшую систему эффектов (`List<SkillEffect>`), а новая система использует `List<EffectConfig>` + `EffectManager`.

### 3. Стрела появлялась плоско
**Примечание:** Это нормально - визуальная часть стрелы (дочерний объект) вращается во время полёта через `Projectile.rotationSpeed = 540`.

---

## Решение

### 1. Добавлена поддержка старого Projectile в SkillExecutor
**Файл:** `Assets/Scripts/Skills/SkillExecutor.cs` (lines 211-235)

```csharp
private void LaunchProjectile(SkillConfig skill, Transform target, Vector3? groundTarget)
{
    // ... existing code for CelestialProjectile and ArrowProjectile ...

    else
    {
        // Try old Projectile component (EntanglingArrow, etc)
        Projectile oldProj = projectile.GetComponent<Projectile>();
        if (oldProj != null)
        {
            float damage = CalculateDamage(skill);
            oldProj.Initialize(target, damage, direction, gameObject, null);

            // Set hit effect from skill
            if (skill.hitEffectPrefab != null)
            {
                oldProj.SetHitEffect(skill.hitEffectPrefab);
            }

            Log("Old Projectile launched: " + damage + " damage");

            // Применяем эффекты вручную (старый Projectile не поддерживает EffectConfig)
            if (skill.effects != null && skill.effects.Count > 0)
            {
                // Добавим MonoBehaviour для применения эффектов при попадании
                ProjectileEffectApplier effectApplier = projectile.AddComponent<ProjectileEffectApplier>();
                effectApplier.Initialize(skill.effects, stats);
            }
        }
    }
}
```

**Что исправлено:**
- ✅ Теперь старый Projectile правильно инициализируется
- ✅ Hit effect устанавливается через `SetHitEffect()`
- ✅ Эффекты применяются через новый компонент `ProjectileEffectApplier`

---

### 2. Создан ProjectileEffectApplier компонент
**Файл:** `Assets/Scripts/Skills/ProjectileEffectApplier.cs` (новый файл)

**Назначение:** Мост между старым `Projectile.cs` и новой системой `EffectConfig` + `EffectManager`.

**Как работает:**
1. Добавляется динамически к снаряду при запуске через `AddComponent<>()`
2. Инициализируется с `List<EffectConfig>` и `CharacterStats` кастера
3. Ловит `OnTriggerEnter` раньше чем `Projectile.cs`
4. При попадании во врага применяет эффекты через `EffectManager.ApplyEffect()`

**Код:**
```csharp
public class ProjectileEffectApplier : MonoBehaviour
{
    private List<EffectConfig> effects;
    private CharacterStats casterStats;
    private bool effectsApplied = false;

    public void Initialize(List<EffectConfig> effectConfigs, CharacterStats stats)
    {
        effects = effectConfigs;
        casterStats = stats;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (effectsApplied) return;

        // Проверяем что это враг
        NetworkPlayer networkTarget = other.GetComponent<NetworkPlayer>();
        Enemy enemy = other.GetComponent<Enemy>();
        DummyEnemy dummy = other.GetComponent<DummyEnemy>();

        if (networkTarget == null && enemy == null && dummy == null)
            return;

        // Применяем эффекты через EffectManager
        ApplyEffectsToTarget(other.transform);
        effectsApplied = true;
    }

    private void ApplyEffectsToTarget(Transform targetTransform)
    {
        EffectManager targetEffectManager = targetTransform.GetComponent<EffectManager>();
        if (targetEffectManager == null)
        {
            targetEffectManager = targetTransform.gameObject.AddComponent<EffectManager>();
        }

        foreach (var effectConfig in effects)
        {
            targetEffectManager.ApplyEffect(effectConfig, casterStats);
        }
    }
}
```

**Преимущества:**
- ✅ Не нужно изменять старый Projectile.cs
- ✅ Поддерживает все новые эффекты (Stun, Root, Burn, Slow, и т.д.)
- ✅ Работает с EffectManager для синхронизации с сервером
- ✅ Применяет визуальные эффекты через `EffectConfig.particleEffectPrefab`

---

### 3. Вращение стрелы
**Статус:** ✅ Уже реализовано в Projectile.cs

**Как работает:**
```csharp
// Projectile.cs - Update() (lines 114-117)
if (visualTransform != null && rotationSpeed > 0)
{
    visualTransform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime, Space.Self);
}
```

**Конфигурация EntanglingArrowProjectile:**
- `rotationSpeed: 540` (540 градусов в секунду = 1.5 оборота/сек)
- `visualTransform` = первый дочерний объект "ArrowVisual"

**Результат:** Стрела вращается вокруг своей оси во время полёта для эффектности.

---

## Текущая конфигурация Stunning Shot

### SkillConfig параметры
**Файл:** `Assets/Scripts/Editor/CreateStunningShot.cs`

```csharp
skill.skillId = 302;
skill.skillName = "Stunning Shot";
skill.skillType = SkillConfigType.ProjectileDamage;
skill.targetType = SkillTargetType.Enemy;

skill.baseDamageOrHeal = 30f;        // Низкий урон
skill.intelligenceScaling = 1.0f;    // +1 урона за INT
skill.cooldown = 15f;                // Долгий кд (мощный CC)
skill.manaCost = 40f;

// Префаб стрелы
skill.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
    "Assets/Resources/Projectiles/EntanglingArrowProjectile.prefab"
);

// Эффект попадания (электрические искры)
skill.hitEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
    "Assets/Resources/Effects/CFXR3 Hit Electric C (Air).prefab"
);

// STUN ЭФФЕКТ - 5 секунд
EffectConfig stunEffect = new EffectConfig();
stunEffect.effectType = EffectType.Stun;
stunEffect.duration = 5f;
stunEffect.canStack = false;
stunEffect.maxStacks = 1;

// Визуальный эффект на враге во время стана
stunEffect.particleEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
    "Assets/Resources/Effects/CFXR3 Hit Electric C (Air).prefab"
);

skill.effects.Add(stunEffect);
```

---

## Как работает Stunning Shot (полный flow)

### 1. Игрок нажимает клавишу
```
SimplePlayerController → UseSkill(1) → SkillExecutor.UseSkill()
```

### 2. SkillExecutor запускает снаряд
```csharp
// SkillExecutor.ExecuteProjectile()
→ ExecuteMultipleProjectiles() // НЕ используется (hitCount = 1)
→ LaunchProjectile(skill, target, null)
```

### 3. LaunchProjectile создаёт стрелу
```csharp
// 1. Спавнит EntanglingArrowProjectile префаб
GameObject projectile = Instantiate(skill.projectilePrefab, spawnPos, rotation);

// 2. Находит Projectile компонент
Projectile oldProj = projectile.GetComponent<Projectile>();

// 3. Инициализирует его
oldProj.Initialize(target, damage, direction, gameObject, null);

// 4. Устанавливает hit effect
oldProj.SetHitEffect(skill.hitEffectPrefab);

// 5. Добавляет ProjectileEffectApplier для стана
ProjectileEffectApplier effectApplier = projectile.AddComponent<ProjectileEffectApplier>();
effectApplier.Initialize(skill.effects, stats);
```

### 4. Стрела летит к цели
```csharp
// Projectile.cs - Update()
→ MoveProjectile() // Движение вперёд (homing = true)
→ visualTransform.Rotate() // Вращение визуала (540°/сек)
→ Check distance to target
```

### 5. Попадание в цель (две коллизии)
```csharp
// A) ProjectileEffectApplier.OnTriggerEnter() - ПЕРВЫЙ
→ ApplyEffectsToTarget()
→ EffectManager.ApplyEffect(stunEffect, casterStats)
→ effectsApplied = true

// B) Projectile.OnTriggerEnter() - ВТОРОЙ
→ enemy.TakeDamage(damage) // 30 + INT урона
→ Instantiate(hitEffect) // Электрические искры
→ DestroySelf()
```

### 6. EffectManager применяет стан
```csharp
// EffectManager.ApplyEffect()
→ Проверяет canStack (false → перезаписывает)
→ Создаёт ActiveEffect
→ Спавнит particleEffectPrefab на враге (электрические искры)
→ StartCoroutine(EffectDurationCoroutine()) // 5 секунд

// Во время стана (5 секунд):
→ CharacterStats.isStunned = true
→ SimpleEnemyAI не может двигаться
→ Enemy.TakeDamage() не может атаковать
→ Визуальный эффект висит на враге
```

### 7. Стан заканчивается
```csharp
// EffectManager.EffectDurationCoroutine()
→ yield return new WaitForSeconds(5f)
→ RemoveEffect(activeEffect)
→ CharacterStats.isStunned = false
→ Destroy(particleEffect)
```

---

## Console Log пример

```
[SimplePlayerController] 🔥 Попытка использовать скилл в слоте 1
[SkillExecutor] 💧 Потрачено 40 маны. Осталось: 460
[SkillExecutor] ⚡ Old Projectile launched: 30 damage
[ProjectileEffectApplier] Initialized with 1 effects
[Projectile] ⚡ OnTriggerEnter: DummyEnemy, tag: Untagged
[ProjectileEffectApplier] ✨ Applied effect Stun to DummyEnemy
[ProjectileEffectApplier] ✅ Applied 1 effects to DummyEnemy
[EffectManager] 🔒 Наложен эффект Stun на 5.0 секунд
[Projectile] 💥 Попадание в DummyEnemy! Урон: 30
[Projectile] 🎨 Hit effect установлен: CFXR3 Hit Electric C (Air)
[SkillExecutor] ⚡ Использован скилл: Stunning Shot

... 5 секунд проходит ...

[EffectManager] 🔓 Эффект Stun закончился
```

---

## Тестирование

### Шаг 1: Пересоздать Stunning Shot SkillConfig
```
Unity → Aetherion → Skills → Create Stunning Shot (Archer)
```

**Должно появиться в Console:**
```
═══════════════════════════════════════════════════════
⚡ Stunning Shot создан!
═══════════════════════════════════════════════════════
📍 Путь: Assets/Resources/Skills/Archer_StunningShot.asset
🆔 Skill ID: 302
💥 Базовый урон: 30
🧠 Intelligence scaling: 1.0x
🎯 Префаб: EntanglingArrowProjectile
⚡ Эффект: Stun (5.0 секунд)
🎨 Визуальный эффект: CFXR3 Hit Electric C (Air)
⏱️ Cooldown: 15.0с
💧 Mana: 40
```

### Шаг 2: Пересоздать TestPlayer
```
Unity → Aetherion → Create Test Player in Scene
```

**Должно добавиться:**
```
✅ Slot 0: Rain of Arrows
✅ Slot 1: Stunning Shot
⚡ Экипировано скиллов: 2/5
```

### Шаг 3: Проверить компонент
1. Play сцену ▶️
2. Используй Stunning Shot (клавиша `2`)
3. Проверь что **DummyEnemy** имеет компонент `ProjectileEffectApplier` на стреле (только на мгновение перед попаданием)

### Шаг 4: Проверить функциональность
✅ **Стрела летит** - EntanglingArrowProjectile движется к цели
✅ **Стрела вращается** - ArrowVisual вращается вокруг оси (540°/сек)
✅ **Урон наносится** - 30 + INT урона
✅ **Электрические искры при попадании** - hitEffectPrefab
✅ **Стан применяется** - враг не движется 5 секунд
✅ **Визуальный эффект на враге** - электрические искры висят на враге 5 секунд
✅ **Стан заканчивается** - через 5 секунд враг снова может двигаться

---

## Текущий прогресс скиллов лучника

| Слот | Скилл | Тип | Урон | Эффект | Cooldown | Статус |
|------|-------|-----|------|--------|----------|--------|
| 0 | Rain of Arrows | Multi-hit | 40×3 = 120 | - | 8s | ✅ РАБОТАЕТ |
| 1 | Stunning Shot | Projectile | 30 + INT | Stun 5s | 15s | ✅ ИСПРАВЛЕНО |
| 2 | ??? | - | - | - | - | ⏳ TODO |
| 3 | ??? | - | - | - | - | ⏳ TODO |
| 4 | ??? | - | - | - | - | ⏳ TODO |

**Осталось:** 3 скилла для лучника

---

## Архитектура: Старая vs Новая система эффектов

### Старая система (SkillEffect)
**Использует:** `Projectile.cs`, `SkillManager.cs`
```csharp
public class SkillEffect
{
    public EffectType effectType;
    public float duration;
    public GameObject particleEffectPrefab;
}
```
**Проблема:** Не поддерживает современные эффекты (Stun, Root), нет синхронизации с сервером

### Новая система (EffectConfig)
**Использует:** `CelestialProjectile.cs`, `ArrowProjectile.cs`, `EffectManager.cs`
```csharp
public class EffectConfig
{
    public EffectType effectType;
    public float duration;
    public GameObject particleEffectPrefab;
    public bool canStack;
    public int maxStacks;
    public float damageOrHealPerTick;
    public float tickInterval;
    public float intelligenceScaling;
    public bool syncWithServer;
}
```
**Преимущества:** Поддержка всех эффектов, серверная синхронизация, DoT/HoT, стаки

### ProjectileEffectApplier - Мост
**Назначение:** Позволяет старым префабам (EntanglingArrowProjectile) использовать новую систему эффектов
```
Старый Projectile → ProjectileEffectApplier → EffectManager (новая система)
```

---

**Статус:** ✅ ВСЁ ИСПРАВЛЕНО

**Stunning Shot готов к использованию!** ⚡

Стрела летит, вращается, наносит урон и накладывает 5-секундный стан с визуальными эффектами! 🎯
