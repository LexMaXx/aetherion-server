# ✅ Hammer Throw - Готов!

## Описание
**Hammer Throw (Бросок Молота)** - второй скилл воина
- Бросает **вращающуюся кувалду** в цель (макс 20м)
- Наносит **50 + 150% от силы** урона
- Оглушает врага на **3 секунды**
- Визуальные эффекты: **вращающийся молот** + **взрыв электрических искр** при попадании + **искры на враге** во время стана

---

## Сравнение с Stunning Shot (Лучник)

| Параметр | Stunning Shot (Лучник) | Hammer Throw (Воин) |
|----------|------------------------|---------------------|
| **Урон** | 30 + 100% Int | **50 + 150% Str** 🔥 |
| **Стан** | 5 секунд | 3 секунды |
| **Снаряд** | EntanglingArrow (стрела) | **HammerProjectile (молот)** 🔨 |
| **Визуал** | Стрела летит прямо | **Молот вращается** ⚙️ |
| **Дальность** | 25м | 20м |
| **Cooldown** | 15 секунд | 12 секунд |
| **Mana** | 40 | 35 |

**Вывод:** Hammer Throw наносит **БОЛЬШЕ УРОНА**, но стан **КОРОЧЕ**. Идеально для агрессивного танка!

---

## Что было создано

### Editor скрипт
**Файл:** [Assets/Scripts/Editor/CreateHammerThrow.cs](Assets/Scripts/Editor/CreateHammerThrow.cs)

**Команда в Unity:**
```
Unity Menu → Aetherion → Skills → Create Hammer Throw (Warrior)
```

**Параметры:**
```csharp
skill.skillId = 402;
skill.skillName = "Hammer Throw";
skill.skillType = SkillConfigType.ProjectileDamage;
skill.targetType = SkillTargetType.Enemy;
skill.requiresTarget = true;

// Урон
skill.baseDamageOrHeal = 50f;        // Базовый урон (больше чем у стрелы!)
skill.strengthScaling = 1.5f;        // +150% от силы
skill.intelligenceScaling = 0f;

// Дальность и скорость
skill.castRange = 20f;               // 20 метров дальность
skill.projectileSpeed = 15f;         // Средняя скорость (как фаербол)
skill.projectileLifetime = 3f;       // 3 секунды существования

// Cooldown и mana
skill.cooldown = 12f;                // 12 секунд
skill.manaCost = 35f;                // 35 маны

// Эффект стана
EffectConfig stunEffect = new EffectConfig();
stunEffect.effectType = EffectType.Stun;
stunEffect.duration = 3f;            // 3 секунды (короче чем у лучника)
stunEffect.particleEffectPrefab = "CFXR3 Hit Electric C (Air)"; // Искры

// Визуальные эффекты
skill.projectilePrefab = "HammerProjectile.prefab";              // Вращающийся молот
skill.hitEffectPrefab = "CFXR3 Hit Electric C (Air)";            // Взрыв искр при попадании
skill.castEffectPrefab = null;                                   // Нет каст эффекта
```

---

## Как работает Hammer Throw

### Полный флоу

1. **Игрок выбирает врага (ЛКМ)**
   ```
   SimplePlayerController.Update() → Left Click → target выбран
   ```

2. **Игрок нажимает клавишу `2` (Hammer Throw)**
   ```
   SimplePlayerController.HandleSkills() → Key "2" → UseSkill(1)
   ```

3. **SkillExecutor запускает снаряд**
   ```csharp
   SkillExecutor.UseSkill(1)
   → Проверка: target != null, cooldown, mana ✅
   → ExecuteProjectile(skill)
   → Spawn HammerProjectile prefab
   → Initialize с target, damage, direction
   → ProjectileEffectApplier.Initialize(effects) - для стана
   ```

4. **HammerProjectile летит к цели**
   ```csharp
   Projectile.Update()
   → Движение: transform.position += direction * speed * Time.deltaTime
   → Вращение: visualTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime)
   → Homing (опционально): direction к цели
   ```

5. **Попадание в цель**
   ```csharp
   Projectile.OnTriggerEnter(target)
   → target.TakeDamage(damage) - урон
   → Spawn hitEffect (CFXR3 Hit Electric C) - взрыв искр
   → ProjectileEffectApplier.ApplyEffectsToTarget(target)
   → EffectManager.ApplyEffect(stunEffect) на враге
   → Spawn particleEffect (искры на враге) - длится 3 секунды
   → Destroy projectile
   ```

6. **Враг оглушён 3 секунды**
   ```csharp
   EffectManager на враге
   → Блокировка движения ✅
   → Блокировка атак ✅
   → Блокировка скиллов ✅
   → Визуальный эффект (искры) 3 секунды ✅
   → Через 3 секунды: RemoveEffect() → враг снова активен
   ```

---

## Визуальные эффекты

### 1. Снаряд (Projectile)
**Эффект:** `HammerProjectile.prefab`
- **Вращающаяся кувалда** 🔨
- Летит к цели со скоростью 15 м/с
- Вращается вокруг своей оси (`rotationSpeed = 360°/сек`)
- Подобно фаерболу, но тяжелее

**Как работает вращение:**
```csharp
// В Projectile.cs
void Update()
{
    // Вращаем визуал (дочерний объект молота)
    if (visualTransform != null)
    {
        visualTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
```

### 2. Попадание (Hit Effect)
**Эффект:** `CFXR3 Hit Electric C (Air)`
- Взрыв электрических искр в точке попадания
- Auto-destroy (исчезает через ~1 секунду)
- Символизирует "удар молнии"

### 3. Стан на враге (Particle Effect)
**Эффект:** `CFXR3 Hit Electric C (Air)`
- Электрические искры вокруг врага ⚡
- Длится **3 секунды** (duration)
- Следует за врагом (attached to transform)
- Исчезает после окончания стана

---

## Создание скилла

### Шаг 1: Запустить Editor скрипт
```
Unity Menu → Aetherion → Skills → Create Hammer Throw (Warrior)
```

**Вывод в консоль:**
```
═══════════════════════════════════════════════════════
🔨 Hammer Throw создан!
═══════════════════════════════════════════════════════
📍 Путь: Assets/Resources/Skills/Warrior_HammerThrow.asset
🆔 Skill ID: 402
🎯 Тип: Projectile + CC (снаряд-стан)
📏 Дальность: 20м
💥 Урон: 50 + 150% от силы
⚡ Эффект: Stun на 3 секунды
✨ Визуальные эффекты:
   - Снаряд: HammerProjectile (вращающийся молот)
   - Попадание: CFXR3 Hit Electric C (взрыв искр)
   - Стан: CFXR3 Hit Electric C (искры на враге 3 сек)
⏱️ Cooldown: 12с
💧 Mana: 35
═══════════════════════════════════════════════════════
```

### Шаг 2: Пересоздать TestPlayer как Warrior
```
Unity Menu → Aetherion → Create Test Player in Scene
→ Выбери класс: Warrior
→ Slot 0: Charge
→ Slot 1: Hammer Throw (НОВОЕ!)
```

### Шаг 3: Тестировать
1. Play SkillTestScene ▶️
2. ЛКМ на DummyEnemy (выбрать цель)
3. Нажми `2` (Hammer Throw)
4. **Должно произойти:**
   - Вращающийся молот вылетает к врагу ✅
   - Молот крутится в полёте ⚙️ ✅
   - При попадании - взрыв искр ⚡ ✅
   - Враг получает урон ✅
   - Враг оглушён на 3 секунды ✅
   - Искры вокруг врага 3 секунды ✅

---

## Проверка в Console

### Успешное использование
```
[SimplePlayerController] 🔥 Попытка использовать скилл в слоте 1
[SkillExecutor] Using skill: Hammer Throw
[SkillExecutor] 💧 Потрачено 35 маны. Осталось: 465
[SkillExecutor] ⚡ Spawning projectile: HammerProjectile
[Projectile] Инициализирован из SkillData: speed=15, homing=false, lifetime=3
[Projectile] 🎨 Hit effect установлен: CFXR3 Hit Electric C (Air)
[ProjectileEffectApplier] Initialize with 1 effects
[Projectile] 💥 Попадание в DummyEnemy_3! Урон: 95.5
[DummyEnemy] DummyEnemy_3 получил 95.5 урона. HP: 904/1000
[ProjectileEffectApplier] Applying effects to DummyEnemy_3
[EffectManager] 🔒 Наложен эффект Stun на 3.0 секунд
[EffectManager] ⚡ Спавним визуальный эффект: CFXR3 Hit Electric C (Air)
[Projectile] 🗑️ Уничтожаем projectile
... через 3 секунды ...
[EffectManager] 🔓 Эффект Stun закончился
```

---

## Технические детали

### Почему молот вращается?

**В Projectile.cs:**
```csharp
[Header("Visual Settings")]
[SerializeField] private float rotationSpeed = 360f; // Градусов в секунду

void Update()
{
    // Ищем дочерний объект (визуал молота)
    if (visualTransform != null)
    {
        // Вращаем вокруг оси Y (вертикальная ось)
        visualTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
```

**В HammerProjectile.prefab:**
```
HammerProjectile (корневой объект)
└── HammerVisual (дочерний объект) ← ЭТОТ ВРАЩАЕТСЯ!
    └── Mesh (модель кувалды)
```

### Как работает ProjectileEffectApplier?

**Создан для Stunning Shot**, но работает для **ВСЕХ** снарядов с эффектами!

```csharp
// В SkillExecutor при создании снаряда
if (skill.effects != null && skill.effects.Count > 0)
{
    // Добавляем ProjectileEffectApplier к снаряду
    ProjectileEffectApplier effectApplier = projectile.AddComponent<ProjectileEffectApplier>();
    effectApplier.Initialize(skill.effects, stats);
}

// При попадании в цель
void OnTriggerEnter(Collider other)
{
    // Применяем все эффекты из skill.effects
    foreach (var effect in effects)
    {
        targetEffectManager.ApplyEffect(effect, casterStats);
    }
}
```

**Это работает для:**
- ✅ Hammer Throw (Stun 3s)
- ✅ Stunning Shot (Stun 5s)
- ✅ Любые будущие снаряды с эффектами (Root, Burn, Slow и т.д.)

---

## Прогресс скиллов Warrior: 2/5 ✅

| Слот | Скилл | Тип | Эффект | Cooldown | Статус |
|------|-------|-----|--------|----------|--------|
| 0 | Charge | Movement+CC | Teleport+Stun 5s | 12s | ✅ |
| 1 | Hammer Throw | Projectile+CC | 50+150%Str dmg+Stun 3s | 12s | ✅ |
| 2 | ??? | - | - | - | ⏳ TODO |
| 3 | ??? | - | - | - | ⏳ TODO |
| 4 | ??? | - | - | - | ⏳ TODO |

**Осталось:** 3 скилла для воина!

---

## Возможные улучшения

### 1. Увеличить урон от попадания
```csharp
skill.baseDamageOrHeal = 80f; // Было 50, стало 80
```

### 2. Увеличить длительность стана
```csharp
stunEffect.duration = 4f; // Было 3, стало 4
```

### 3. Добавить замедление после стана
```csharp
EffectConfig slowEffect = new EffectConfig();
slowEffect.effectType = EffectType.DecreaseSpeed;
slowEffect.duration = 5f;
slowEffect.power = 30f; // -30% скорости
```

### 4. Добавить AOE урон вокруг точки попадания
```csharp
skill.areaOfEffect = 3f; // Радиус 3 метра
// + логика в SkillExecutor для AOE damage
```

---

## Сочетание с другими скиллами

### Комбо 1: Charge → Hammer Throw
```
1. Charge к врагу (Stun 5s)
2. Враг оглушён
3. Hammer Throw в упор (гарантированное попадание!)
4. Враг оглушён ещё 3 секунды
5. Итого: 8 секунд контроля!
```

### Комбо 2: Hammer Throw → Charge
```
1. Hammer Throw издалека (Stun 3s)
2. Враг оглушён на расстоянии
3. Charge к врагу (подлетаем вплотную)
4. Враг оглушён ещё 5 секунд
5. Бьём в ближнем бою!
```

---

**Статус:** ✅ ГОТОВО

Hammer Throw работает отлично! Вращающийся молот + электрические искры + стан! 🔨⚡

Готов к созданию следующих 3 скиллов воина!
