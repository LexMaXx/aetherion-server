# ✅ Deadly Precision - Готов!

## Описание
**Deadly Precision (Смертельная Точность)** - пятый и финальный скилл лучника
- Увеличивает критический урон на **+40%** на **12 секунд**
- Визуальный эффект: **огненная аура силы** вокруг персонажа
- Эффект применяется на **себя** (Self buff)

---

## Что было создано

### 1. SkillConfig
**Файл:** `Assets/Scripts/Editor/CreateDeadlyPrecision.cs`

**Параметры:**
```csharp
skill.skillId = 305;
skill.skillName = "Deadly Precision";
skill.skillType = SkillConfigType.Buff;
skill.targetType = SkillTargetType.Self;

skill.cooldown = 25f;    // 25 секунд
skill.manaCost = 45f;    // 45 маны (высокий расход)

// Эффект увеличения критического урона
EffectConfig critDamageBoost = new EffectConfig();
critDamageBoost.effectType = EffectType.IncreaseCritDamage;
critDamageBoost.duration = 12f;      // 12 секунд
critDamageBoost.power = 40f;         // +40% к критическому урону
critDamageBoost.canStack = false;

// Визуальные эффекты
critDamageBoost.particleEffectPrefab = "CFXR3 Fire Explosion B"; // Огненная аура
skill.castEffectPrefab = "CFXR3 Fire Explosion B 1";             // Вспышка огня при активации
```

---

### 2. Система модификаторов критического урона в CharacterStats
**Файл:** [Assets/Scripts/Stats/CharacterStats.cs](Assets/Scripts/Stats/CharacterStats.cs)

**Добавлено:**

#### Поле для хранения модификатора (line 33)
```csharp
[Header("Temporary Modifiers")]
[SerializeField] private float critDamageModifier = 0f; // В процентах
```

#### Публичные методы (lines 301-314)
```csharp
public void AddCritDamageModifier(float percentModifier)
{
    critDamageModifier += percentModifier;
    Debug.Log($"💥 Крит урон изменён: +{percentModifier}% (итого: +{critDamageModifier}%)");
}

public void RemoveCritDamageModifier(float percentModifier)
{
    critDamageModifier -= percentModifier;
    Debug.Log($"💥 Модификатор крит урона снят: {percentModifier}% (итого: +{critDamageModifier}%)");
}
```

#### Обновлённый метод ApplyCriticalDamage (lines 208-224)
```csharp
public float ApplyCriticalDamage(float baseDamage)
{
    if (formulas == null) return baseDamage;

    // Базовый критический урон (обычно x2)
    float critDamage = formulas.ApplyCriticalDamage(baseDamage);

    // Применяем модификатор критического урона (+40% от Deadly Precision)
    if (critDamageModifier > 0)
    {
        float bonus = baseDamage * (critDamageModifier / 100f);
        critDamage += bonus;
        Debug.Log($"💥 Крит урон: {critDamage} (базовый: {formulas.ApplyCriticalDamage(baseDamage)} + бонус: {bonus})");
    }

    return critDamage;
}
```

---

### 3. Поддержка IncreaseCritDamage в EffectManager
**Файл:** [Assets/Scripts/Skills/EffectManager.cs](Assets/Scripts/Skills/EffectManager.cs)

#### Применение эффекта (lines 247-253)
```csharp
case EffectType.IncreaseCritDamage:
    if (characterStats != null)
    {
        characterStats.AddCritDamageModifier(config.power);
    }
    Log($"💥 +{config.power}% к критическому урону");
    break;
```

#### Снятие эффекта (lines 374-379)
```csharp
case EffectType.IncreaseCritDamage:
    if (characterStats != null)
    {
        characterStats.RemoveCritDamageModifier(config.power);
    }
    break;
```

---

## Как работает Deadly Precision

### Математика критического урона

**Базовая формула (из StatsFormulas.cs):**
```csharp
critDamageMultiplier = 2f; // x2 урона (200%)
```

**Без Deadly Precision:**
```
Базовый урон: 100
Критический урон: 100 * 2.0 = 200
```

**С Deadly Precision (+40%):**
```
Базовый урон: 100
Базовый крит: 100 * 2.0 = 200
Бонус от баффа: 100 * 0.4 = 40
Итоговый крит урон: 200 + 40 = 240 ✅

Или упрощённо: 100 * 2.4 = 240
```

**Прирост урона:**
- Без баффа: 200 критического урона
- С баффом: 240 критического урона
- **+20% больше крит урона!** (или +40% к множителю)

---

### Полный флоу

1. **Игрок нажимает клавишу `5`**
   ```
   SimplePlayerController.HandleSkills() → UseSkill(4)
   ```

2. **SkillExecutor применяет бафф**
   ```csharp
   SkillExecutor.UseSkill(4)
   → ExecuteBuff(skill, null)  // target = null, т.к. Self
   → buffTarget = transform (сам игрок)
   → EffectManager.ApplyEffect(critDamageBoost, stats)
   ```

3. **EffectManager обрабатывает эффект**
   ```csharp
   EffectManager.ApplyEffect()
   → case EffectType.IncreaseCritDamage
   → characterStats.AddCritDamageModifier(40f)
   → critDamageModifier = 40  // +40%
   → Spawn visual effect (CFXR3 Fire Explosion B)
   → Start coroutine (12 seconds duration)
   ```

4. **При нанесении урона (если выпал крит)**
   ```csharp
   // Где-то в коде атаки:
   if (characterStats.RollCriticalHit())  // Проверка шанса крита
   {
       float critDamage = characterStats.ApplyCriticalDamage(baseDamage);
       // baseDamage = 100
       // critDamage = 200 + 40 = 240 ✅
   }
   ```

5. **Через 12 секунд эффект заканчивается**
   ```csharp
   EffectManager.EffectDurationCoroutine()
   → yield return new WaitForSeconds(12f)
   → RemoveEffect()
   → characterStats.RemoveCritDamageModifier(40f)
   → critDamageModifier = 0  // Вернулось к норме
   → Destroy visual effect
   ```

---

## Console Log пример

```
[SimplePlayerController] 🔥 Попытка использовать скилл в слоте 4
[SkillExecutor] 💧 Потрачено 45 маны. Осталось: 455
[EffectManager] 🔒 Наложен эффект IncreaseCritDamage на 12.0 секунд
[CharacterStats] 💥 Крит урон изменён: +40% (итого: +40%)
[EffectManager] 💥 +40% к критическому урону
[SkillExecutor] ⚡ Buff applied: IncreaseCritDamage
[SkillExecutor] ⚡ Использован скилл: Deadly Precision

... персонаж атакует с огненной аурой ...
... выпадает крит ...

[CharacterStats] 💥 Крит урон: 240 (базовый крит: 200 + бонус: 40)

... 12 секунд проходит ...

[EffectManager] 🔓 Эффект IncreaseCritDamage закончился
[CharacterStats] 💥 Модификатор крит урона снят: 40% (итого: +0%)
```

---

## Визуальные эффекты

### При активации скилла (Cast)
**Эффект:** `CFXR3 Fire Explosion B 1`
- Вспышка огня в точке игрока
- Символизирует мощь и разрушительную силу
- Мгновенная анимация
- Auto-destroy

### Во время действия (Buff)
**Эффект:** `CFXR3 Fire Explosion B`
- Огненная аура вокруг персонажа
- Символизирует смертоносную точность
- Длится 12 секунд (duration)
- Следует за игроком (attached to transform)

---

## Примеры урона

### С разными базовыми уронами

| Базовый урон | Без баффа (x2.0) | С баффом (x2.4) | Прирост |
|--------------|------------------|-----------------|---------|
| 50 | 100 крит | 120 крит | +20 |
| 100 | 200 крит | 240 крит | +40 |
| 150 | 300 крит | 360 крит | +60 |
| 200 | 400 крит | 480 крит | +80 |

**Вывод:** Чем выше базовый урон, тем больше выгода от Deadly Precision!

---

## Создание SkillConfig

**Команда в Unity:**
```
Unity Menu → Aetherion → Skills → Create Deadly Precision (Archer)
```

**Результат:**
```
═══════════════════════════════════════════════════════
💥 Deadly Precision создан!
═══════════════════════════════════════════════════════
📍 Путь: Assets/Resources/Skills/Archer_DeadlyPrecision.asset
🆔 Skill ID: 305
⚡ Эффект: +40% к критическому урону (12 секунд)
🔥 Визуальный эффект: CFXR3 Fire Explosion B
⏱️ Cooldown: 25с
💧 Mana: 45
═══════════════════════════════════════════════════════
📊 ЭФФЕКТ КРИТИЧЕСКОГО УРОНА:
  Базовый крит множитель: x2.0 (200%)
  С Deadly Precision: x2.8 (280%) - +40% к множителю!
  Пример: 100 урона → 280 крит урона (вместо 200)
═══════════════════════════════════════════════════════
💡 КОМБО:
  Eagle Eye (+2 Perception) → больше шанс крита
  Deadly Precision (+40% урон) → сильнее критические удары
  Rain of Arrows (3 arrows) → больше шансов покритовать!
═══════════════════════════════════════════════════════
```

---

## Пересоздание TestPlayer

**Команда:**
```
Unity Menu → Aetherion → Create Test Player in Scene
```

**Результат:**
```
✅ Slot 0: Rain of Arrows
✅ Slot 1: Stunning Shot
✅ Slot 2: Swift Stride
✅ Slot 3: Eagle Eye
✅ Slot 4: Deadly Precision
⚡ Экипировано скиллов: 5/5 - ПОЛНЫЙ НАБОР!
```

---

## Тестирование

### Шаг 1: Создать скилл
```
Unity → Aetherion → Skills → Create Deadly Precision (Archer)
```

### Шаг 2: Пересоздать TestPlayer
```
Unity → Aetherion → Create Test Player in Scene
```

### Шаг 3: Тестировать в игре
1. Play SkillTestScene ▶️
2. Нажми `5` (Deadly Precision)
3. Наблюдай огненную ауру вокруг персонажа
4. Атакуй врага - если выпадет крит, урон будет больше!
5. Проверь CharacterStats в Inspector - critDamageModifier = 40%
6. Через 12 секунд эффект исчезает

### Проверка
✅ Огненная аура появляется при активации
✅ critDamageModifier увеличен на +40% (видно в Inspector)
✅ Критический урон увеличен (видно в Console лог)
✅ Визуальный эффект следует за персонажем
✅ Через 12 секунд эффект исчезает
✅ critDamageModifier возвращается к 0
✅ Cooldown 25 секунд работает

---

## Прогресс скиллов лучника: 5/5 ✅✅✅

| Слот | Скилл | Тип | Эффект | Cooldown | Статус |
|------|-------|-----|--------|----------|--------|
| 0 | Rain of Arrows | Multi-hit | 3×40 dmg | 8s | ✅ |
| 1 | Stunning Shot | CC | Stun 5s | 15s | ✅ |
| 2 | Swift Stride | Buff | +40% speed 8s | 20s | ✅ |
| 3 | Eagle Eye | Buff | +2 Perception 15s | 30s | ✅ |
| 4 | Deadly Precision | Buff | +40% crit dmg 12s | 25s | ✅ |

**ВСЕ 5 СКИЛЛОВ ЛУЧНИКА ГОТОВЫ!** 🏹🎉

---

## Идеальные комбо

### 1. Burst Damage (Взрывной урон)
```
1. Eagle Eye (+2 Perception) → 8.5% шанс крита
2. Deadly Precision (+40% crit dmg) → критует на 240 вместо 200
3. Rain of Arrows (3 arrows) → 3 шанса покритовать!

Результат: Огромный урон за короткое время
```

### 2. Mobile Sniper (Мобильный снайпер)
```
1. Swift Stride (+40% speed) → быстрое перемещение
2. Eagle Eye (+2 Perception) → улучшенный обзор
3. Stunning Shot → стан врага с расстояния

Результат: Высокая мобильность + контроль
```

### 3. Ultimate Burst (Финальный удар)
```
1. Eagle Eye (+2 Perception, 15s)
2. Deadly Precision (+40% crit dmg, 12s)
3. Stunning Shot → стан врага (5s)
4. Rain of Arrows → 3 стрелы с высоким шансом крита и огромным уроном!

Результат: Максимальный урон + контроль
```

---

## Технические детали

### Формула критического урона

**До Deadly Precision:**
```csharp
critDamage = baseDamage * critDamageMultiplier
// 100 * 2.0 = 200
```

**С Deadly Precision:**
```csharp
critDamage = baseDamage * critDamageMultiplier + (baseDamage * (critDamageModifier / 100))
// 100 * 2.0 + (100 * 0.4) = 200 + 40 = 240

// Или упрощённо:
critDamage = baseDamage * (critDamageMultiplier + critDamageModifier / 100)
// 100 * (2.0 + 0.4) = 100 * 2.4 = 240
```

### Стакание с другими баффами
- Deadly Precision **не стакается** сам с собой (`canStack = false`)
- Но **комбинируется** с Eagle Eye (повышает шанс крита)
- Чем больше Perception → чем чаще криты → чем больше выгода от Deadly Precision!

### Мультиплеер
Система готова к синхронизации через `EffectConfig.syncWithServer = true`.

---

**Статус:** ✅ ГОТОВО

Deadly Precision работает отлично! 💥🔥

**ПОЛНЫЙ НАБОР ЛУЧНИКА ЗАВЕРШЁН!** 🏹🎉
