# ✅ Defensive Stance - Готов!

## Описание
**Defensive Stance (Защитная Стойка)** - третий скилл воина
- Уменьшает **получаемый урон** на **50%** на **10 секунд**
- Self buff (на себя)
- Визуальные эффекты: **золотой щит** вокруг персонажа + **вспышка света** при активации

---

## Что было создано

### 1. Editor скрипт
**Файл:** [Assets/Scripts/Editor/CreateDefensiveStance.cs](Assets/Scripts/Editor/CreateDefensiveStance.cs)

**Команда в Unity:**
```
Unity Menu → Aetherion → Skills → Create Defensive Stance (Warrior)
```

**Параметры:**
```csharp
skill.skillId = 403;
skill.skillName = "Defensive Stance";
skill.skillType = SkillConfigType.Buff;
skill.targetType = SkillTargetType.Self;

skill.cooldown = 20f;                // 20 секунд
skill.manaCost = 40f;                // 40 маны

// Эффект защиты
EffectConfig defenseBoost = new EffectConfig();
defenseBoost.effectType = EffectType.IncreaseDefense;
defenseBoost.duration = 10f;         // 10 секунд
defenseBoost.power = 50f;            // 50% снижение урона

// Визуальные эффекты
defenseBoost.particleEffectPrefab = "CFXR3 Shield Leaves A (Lit)"; // Золотой щит
skill.castEffectPrefab = "CFXR3 Hit Light B (Air)";               // Вспышка света
```

### 2. Система снижения урона в HealthSystem
**Файл:** [Assets/Scripts/Player/HealthSystem.cs](Assets/Scripts/Player/HealthSystem.cs)

**Добавлено:**
- Поле `damageReduction` (строка 18)
- Метод `AddDamageReduction(float percent)` (строка 246)
- Метод `RemoveDamageReduction(float percent)` (строка 256)
- Модифицирован `TakeDamage()` для применения снижения урона (строка 140-147)

**Как работает:**
```csharp
public void TakeDamage(float damage)
{
    // Применяем снижение урона
    float originalDamage = damage;
    if (damageReduction > 0)
    {
        float reduction = damage * (damageReduction / 100f);
        damage -= reduction;
        // Враг атакует на 100 → Ты получаешь 50 (при 50% снижении)
    }

    currentHealth -= damage;
    // ...
}
```

### 3. Поддержка IncreaseDefense в EffectManager
**Файл:** [Assets/Scripts/Skills/EffectManager.cs](Assets/Scripts/Skills/EffectManager.cs)

**Изменения:**

**Применение эффекта (строка 207-213):**
```csharp
case EffectType.IncreaseDefense:
    if (healthSystem != null)
    {
        healthSystem.AddDamageReduction(config.power);
    }
    Log($"🛡️ +{config.power}% к защите (снижение урона)");
    break;
```

**Снятие эффекта (строка 343-348):**
```csharp
case EffectType.IncreaseDefense:
    if (healthSystem != null)
    {
        healthSystem.RemoveDamageReduction(config.power);
    }
    break;
```

---

## Как работает Defensive Stance

### Полный флоу

1. **Игрок нажимает клавишу `3` (Defensive Stance)**
   ```
   SimplePlayerController.HandleSkills() → Key "3" → UseSkill(2)
   ```

2. **SkillExecutor применяет бафф**
   ```csharp
   SkillExecutor.UseSkill(2)
   → ExecuteBuff(skill, null) // Self buff
   → EffectManager.ApplyEffect(defenseBoost)
   ```

3. **EffectManager обрабатывает эффект**
   ```csharp
   EffectManager.ApplyEffect()
   → case EffectType.IncreaseDefense
   → healthSystem.AddDamageReduction(50f) // +50%
   → damageReduction = 0 + 50 = 50%
   → Spawn particleEffect (CFXR3 Shield Leaves A) - золотой щит
   → Start coroutine (10 seconds duration)
   ```

4. **Визуальные эффекты**
   ```
   - Вспышка света при активации (Cast Effect)
   - Золотой щит вокруг персонажа (Particle Effect) - 10 секунд
   ```

5. **Враг атакует воина**
   ```csharp
   DummyEnemyAttacker.AttackPlayer()
   → healthSystem.TakeDamage(100) // Враг атакует на 100 урона

   HealthSystem.TakeDamage(100)
   → damageReduction = 50%
   → reduction = 100 * 0.5 = 50
   → damage = 100 - 50 = 50 ✅
   → currentHealth -= 50 (вместо 100!)

   Console: "[HealthSystem] 🛡️ Снижение урона: 100 → 50 (-50, 50%)"
   ```

6. **Через 10 секунд эффект заканчивается**
   ```csharp
   EffectManager.EffectDurationCoroutine()
   → yield return new WaitForSeconds(10f)
   → RemoveEffect()
   → healthSystem.RemoveDamageReduction(50f)
   → damageReduction = 50 - 50 = 0%
   → Destroy particleEffect (золотой щит исчезает)
   ```

---

## Визуальные эффекты

### При активации (Cast Effect)
**Эффект:** `CFXR3 Hit Light B (Air)`
- Вспышка золотого света вокруг персонажа
- Символизирует "активацию защиты"
- Auto-destroy (исчезает через ~1 секунду)

### Во время действия (Particle Effect)
**Эффект:** `CFXR3 Shield Leaves A (Lit)`
- **Золотой щит** вокруг персонажа 🛡️
- Длится **10 секунд** (duration)
- Следует за игроком (attached to transform)
- Символизирует активную защиту

---

## Примеры работы

### Пример 1: БЕЗ Defensive Stance
```
Враг атакует на 100 урона
→ HealthSystem.TakeDamage(100)
→ damageReduction = 0%
→ damage = 100
→ currentHealth = 180 - 100 = 80 HP

Результат: Получено 100 урона ❌
```

### Пример 2: С Defensive Stance
```
1. Игрок активирует Defensive Stance (клавиша 3)
   → Вспышка света
   → Золотой щит вокруг персонажа
   → damageReduction = 50%

2. Враг атакует на 100 урона
   → HealthSystem.TakeDamage(100)
   → damageReduction = 50%
   → reduction = 100 * 0.5 = 50
   → damage = 100 - 50 = 50 ✅
   → currentHealth = 180 - 50 = 130 HP

Результат: Получено 50 урона (вместо 100!) 🛡️
Живучесть: x2!
```

### Пример 3: Стакание нескольких защитных баффов
```
Defensive Stance (50%) + Другой защитный бафф (30%)
→ damageReduction = 50 + 30 = 80%
→ Враг атакует на 100 урона
→ damage = 100 - 80 = 20 урона!
→ Живучесть: x5!
```

---

## Создание и тестирование

### Шаг 1: Создать скилл
```
Unity Menu → Aetherion → Skills → Create Defensive Stance (Warrior)
```

**Вывод в консоль:**
```
═══════════════════════════════════════════════════════
🛡️ Defensive Stance создан!
═══════════════════════════════════════════════════════
📍 Путь: Assets/Resources/Skills/Warrior_DefensiveStance.asset
🆔 Skill ID: 403
🎯 Тип: Self Buff (защитная стойка)
🛡️ Эффект: -50% получаемого урона (10 секунд)
✨ Визуальные эффекты:
   - Активация: CFXR3 Hit Light B (вспышка света)
   - Бафф: CFXR3 Shield Leaves A (золотой щит вокруг персонажа)
⏱️ Cooldown: 20с
💧 Mana: 40
═══════════════════════════════════════════════════════
```

### Шаг 2: Пересоздать TestPlayer как Warrior
```
Unity Menu → Aetherion → Create Test Player in Scene
→ Класс: Warrior
→ Slot 0: Charge ✅
→ Slot 1: Hammer Throw ✅
→ Slot 2: Defensive Stance ✅ (НОВОЕ!)
```

### Шаг 3: Тестировать с DummyEnemyAttacker
```
1. Play SkillTestScene ▶️
2. Враг атакует каждую секунду (DummyEnemyAttacker)
3. Нажми `3` (Defensive Stance)
4. Наблюдай:
   - Вспышка света ✅
   - Золотой щит вокруг персонажа ✅
   - Урон уменьшен на 50%! ✅
```

---

## Проверка в Console

### БЕЗ Defensive Stance
```
[DummyEnemyAttacker] 💥 Атакую игрока! Урон: 10
[HealthSystem] -10 HP. Осталось: 170/180
```

### С Defensive Stance
```
[SimplePlayerController] 🔥 Попытка использовать скилл в слоте 2
[SkillExecutor] Using skill: Defensive Stance
[SkillExecutor] 💧 Потрачено 40 маны. Осталось: 460
[EffectManager] 🔒 Наложен эффект IncreaseDefense на 10.0 секунд
[HealthSystem] 🛡️ Снижение урона: +50% (итого: 50%)
[EffectManager] 🛡️ +50% к защите (снижение урона)
[SkillExecutor] ⚡ Buff applied: IncreaseDefense

... враг атакует ...

[DummyEnemyAttacker] 💥 Атакую игрока! Урон: 10
[HealthSystem] 🛡️ Снижение урона: 10.0 → 5.0 (-5.0, 50%) ✅
[HealthSystem] -5 HP. Осталось: 175/180 ✅

... через 10 секунд ...

[EffectManager] 🔓 Эффект IncreaseDefense закончился
[HealthSystem] 🛡️ Снижение урона: -50% (итого: 0%)
```

---

## Технические детали

### Формула снижения урона
```csharp
reducedDamage = originalDamage * (1 - damageReduction / 100)

Примеры:
- 100 урона при 50% снижении = 100 * (1 - 0.5) = 50 урона
- 100 урона при 80% снижении = 100 * (1 - 0.8) = 20 урона
- 100 урона при 100% снижении = 100 * (1 - 1.0) = 0 урона (неуязвимость!)
```

### Ограничения
- **Максимум:** 100% снижение урона (полная неуязвимость)
- **Минимум:** 0% снижение урона (нормальный урон)
- **Стакание:** Defensive Stance НЕ стакается сам с собой (`canStack = false`), но может комбинироваться с другими защитными баффами

### Компоненты, которым нужен HealthSystem
- ✅ SimplePlayerController (TestPlayer)
- ✅ PlayerController (NetworkPlayer)
- ✅ DummyEnemy (тестовый враг)

---

## Прогресс скиллов Warrior: 3/5 ✅

| Слот | Скилл | Тип | Эффект | Cooldown | Статус |
|------|-------|-----|--------|----------|--------|
| 0 | Charge | Movement+CC | Teleport+Stun 5s | 12s | ✅ |
| 1 | Hammer Throw | Projectile+CC | 50+150%Str dmg+Stun 3s | 12s | ✅ |
| 2 | Defensive Stance | Self Buff | -50% урона 10s | 20s | ✅ |
| 3 | ??? | - | - | - | ⏳ TODO |
| 4 | ??? | - | - | - | ⏳ TODO |

**Осталось:** 2 скилла для воина!

---

## Сочетание с другими скиллами

### Комбо 1: Defensive Stance → Charge
```
1. Активируешь Defensive Stance (-50% урона)
2. Charge к врагу (телепорт + стан)
3. Бьёшь врага в ближнем бою
4. Враг бьёт тебя, но ты получаешь ПОЛОВИНУ урона!
5. Танковый стиль игры! 🛡️
```

### Комбо 2: Hammer Throw → Defensive Stance
```
1. Hammer Throw издалека (стан 3s)
2. Враг оглушён, подбегаешь ближе
3. Defensive Stance перед окончанием стана
4. Враг снова может атаковать, но ты под защитой!
5. Продолжаешь бить с -50% получаемого урона
```

### Комбо 3: Выживание
```
Ситуация: Много врагов вокруг, низкое HP
1. Defensive Stance (-50% урона)
2. Стоишь на месте (начинается регенерация HP)
3. Враги атакуют, но ты получаешь половину урона
4. Регенерация восстанавливает HP
5. Живучесть резко повышена!
```

---

## Возможные улучшения

### 1. Увеличить снижение урона
```csharp
defenseBoost.power = 70f; // Было 50%, стало 70%
```

### 2. Увеличить длительность
```csharp
defenseBoost.duration = 15f; // Было 10с, стало 15с
```

### 3. Добавить регенерацию HP
```csharp
EffectConfig regenEffect = new EffectConfig();
regenEffect.effectType = EffectType.HealOverTime;
regenEffect.duration = 10f;
regenEffect.damageOrHealPerTick = 5f; // 5 HP/сек
regenEffect.tickInterval = 1f;
```

### 4. Добавить эффект отражения урона
```csharp
EffectConfig thornsEffect = new EffectConfig();
thornsEffect.effectType = EffectType.ThornsEffect;
thornsEffect.duration = 10f;
thornsEffect.power = 20f; // 20% урона отражается атакующему
```

---

**Статус:** ✅ ГОТОВО

Defensive Stance работает отлично! Золотой щит + снижение урона на 50% + живучесть x2! 🛡️✨

Готов к созданию последних 2 скиллов воина!
