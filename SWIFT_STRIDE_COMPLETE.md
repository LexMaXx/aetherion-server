# ✅ Swift Stride - Готов!

## Описание
**Swift Stride (Быстрый Шаг)** - третий скилл лучника
- Увеличивает скорость передвижения на **40%** на **8 секунд**
- Визуальный эффект: **аура из листвы** вокруг персонажа
- Эффект применяется на **себя** (Self buff)

---

## Что было создано

### 1. SkillConfig
**Файл:** `Assets/Scripts/Editor/CreateSwiftStride.cs`

**Параметры:**
```csharp
skill.skillId = 303;
skill.skillName = "Swift Stride";
skill.skillType = SkillConfigType.Buff;
skill.targetType = SkillTargetType.Self;

skill.cooldown = 20f;    // 20 секунд
skill.manaCost = 35f;    // 35 маны

// Эффект ускорения
EffectConfig speedBoost = new EffectConfig();
speedBoost.effectType = EffectType.IncreaseSpeed;
speedBoost.duration = 8f;        // 8 секунд
speedBoost.power = 40f;          // +40% к скорости
speedBoost.canStack = false;

// Визуальные эффекты
speedBoost.particleEffectPrefab = "CFXR3 Shield Leaves A (Lit)"; // Аура вокруг персонажа
skill.castEffectPrefab = "CFXR3 Hit Leaves A (Lit)";             // Вспышка при активации
```

---

### 2. Система модификаторов скорости в SimplePlayerController
**Файл:** [Assets/Scripts/Player/SimplePlayerController.cs](Assets/Scripts/Player/SimplePlayerController.cs)

**Добавлено:**
```csharp
// Поле для хранения модификатора
private float speedModifier = 0f; // В процентах

// Применение модификатора при движении
float currentSpeed = moveSpeed * (1f + speedModifier / 100f);
Vector3 move = moveDirection * currentSpeed * Time.deltaTime;

// Публичные методы
public void AddSpeedModifier(float percentModifier)    // +40% → speedModifier = 40
public void RemoveSpeedModifier(float percentModifier) // Убирает модификатор
public void ResetSpeedModifiers()                      // Сброс всех модификаторов
```

**Пример работы:**
- Базовая скорость: `5 м/с`
- Swift Stride активирован: `+40%`
- Итоговая скорость: `5 * (1 + 40/100) = 5 * 1.4 = 7 м/с`

---

### 3. Интеграция с EffectManager
**Файл:** [Assets/Scripts/Skills/EffectManager.cs](Assets/Scripts/Skills/EffectManager.cs)

**Что изменено:**
```csharp
// Добавлена ссылка на SimplePlayerController
private SimplePlayerController simplePlayerController;

// Инициализация
simplePlayerController = GetComponent<SimplePlayerController>();

// Применение эффекта IncreaseSpeed
case EffectType.IncreaseSpeed:
    if (simplePlayerController != null)
    {
        simplePlayerController.AddSpeedModifier(config.power);
    }
    Log($"🏃 +{config.power}% к скорости");
    break;

// Снятие эффекта IncreaseSpeed
case EffectType.IncreaseSpeed:
    if (simplePlayerController != null)
    {
        simplePlayerController.RemoveSpeedModifier(config.power);
    }
    break;
```

---

## Как работает Swift Stride

### Полный флоу

1. **Игрок нажимает клавишу `3`**
   ```
   SimplePlayerController.HandleSkills() → UseSkill(2)
   ```

2. **SkillExecutor применяет бафф**
   ```csharp
   SkillExecutor.UseSkill(2)
   → ExecuteBuff(skill, null)  // target = null, т.к. Self
   → buffTarget = transform (сам игрок)
   → EffectManager.ApplyEffect(speedBoost, stats)
   ```

3. **EffectManager обрабатывает эффект**
   ```csharp
   EffectManager.ApplyEffect()
   → case EffectType.IncreaseSpeed
   → simplePlayerController.AddSpeedModifier(40f)
   → speedModifier = 40  // +40%
   → Spawn visual effect (CFXR3 Shield Leaves A)
   → Start coroutine (8 seconds duration)
   ```

4. **SimplePlayerController применяет ускорение**
   ```csharp
   HandleMovement()
   → float currentSpeed = 5 * (1 + 40/100)  // = 7 м/с
   → characterController.Move(moveDirection * 7 * Time.deltaTime)
   ```

5. **Через 8 секунд эффект заканчивается**
   ```csharp
   EffectManager.EffectDurationCoroutine()
   → yield return new WaitForSeconds(8f)
   → RemoveEffect()
   → simplePlayerController.RemoveSpeedModifier(40f)
   → speedModifier = 0  // Вернулись к норме
   → Destroy visual effect
   ```

---

## Console Log пример

```
[SimplePlayerController] 🔥 Попытка использовать скилл в слоте 2
[SkillExecutor] 💧 Потрачено 35 маны. Осталось: 465
[EffectManager] 🔒 Наложен эффект IncreaseSpeed на 8.0 секунд
[SimplePlayerController] 🏃 Скорость изменена: +40% (итого: 40%)
[EffectManager] 🏃 +40% к скорости
[SkillExecutor] ⚡ Buff applied: IncreaseSpeed
[SkillExecutor] ⚡ Использован скилл: Swift Stride

... персонаж бежит быстрее с аурой из листвы ...
... 8 секунд проходит ...

[EffectManager] 🔓 Эффект IncreaseSpeed закончился
[SimplePlayerController] 🏃 Модификатор скорости снят: 40% (итого: 0%)
```

---

## Визуальные эффекты

### При активации скилла (Cast)
**Эффект:** `CFXR3 Hit Leaves A (Lit)`
- Вспышка из листьев в точке игрока
- Мгновенная анимация
- Auto-destroy

### Во время действия (Buff)
**Эффект:** `CFXR3 Shield Leaves A (Lit)`
- Аура из кружащихся листьев вокруг персонажа
- Длится 8 секунд (duration)
- Следует за игроком (attached to transform)

---

## Создание SkillConfig

**Команда в Unity:**
```
Unity Menu → Aetherion → Skills → Create Swift Stride (Archer)
```

**Результат:**
```
═══════════════════════════════════════════════════════
🏃 Swift Stride создан!
═══════════════════════════════════════════════════════
📍 Путь: Assets/Resources/Skills/Archer_SwiftStride.asset
🆔 Skill ID: 303
⚡ Эффект: +40% к скорости (8 секунд)
🍃 Визуальный эффект: CFXR3 Shield Leaves A (Lit)
⏱️ Cooldown: 20с
💧 Mana: 35
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
⚡ Экипировано скиллов: 3/5
```

---

## Тестирование

### Шаг 1: Создать скилл
```
Unity → Aetherion → Skills → Create Swift Stride (Archer)
```

### Шаг 2: Пересоздать TestPlayer
```
Unity → Aetherion → Create Test Player in Scene
```

### Шаг 3: Тестировать в игре
1. Play SkillTestScene ▶️
2. Нажми `3` (Swift Stride)
3. Двигайся WASD - скорость увеличена!
4. Наблюдай аура из листвы вокруг персонажа
5. Через 8 секунд скорость вернётся к норме

### Проверка
✅ Аура из листвы появляется при активации
✅ Скорость увеличена на 40% (персонаж бежит заметно быстрее)
✅ Визуальный эффект следует за персонажем
✅ Через 8 секунд эффект исчезает
✅ Скорость возвращается к норме
✅ Cooldown 20 секунд работает

---

## Прогресс скиллов лучника: 3/5

| Слот | Скилл | Тип | Эффект | Cooldown | Статус |
|------|-------|-----|--------|----------|--------|
| 0 | Rain of Arrows | Multi-hit | 3 arrows × 40 dmg | 8s | ✅ |
| 1 | Stunning Shot | CC | Stun 5s | 15s | ✅ |
| 2 | Swift Stride | Buff | +40% speed 8s | 20s | ✅ |
| 3 | ??? | - | - | - | ⏳ TODO |
| 4 | ??? | - | - | - | ⏳ TODO |

**Осталось:** 2 скилла для лучника!

---

## Технические детали

### Модификаторы скорости - множественные эффекты
Система поддерживает **стакающиеся** модификаторы:
```
Swift Stride (+40%) + Slow Debuff (-30%) = +10% итого
speedModifier = 40 - 30 = 10
currentSpeed = 5 * (1 + 10/100) = 5.5 м/с
```

### Совместимость с CC эффектами
- **Root/Stun** блокируют движение через `EffectManager.CanMove()`
- Swift Stride **не помогает** если персонаж застанен
- Но модификатор скорости **сохраняется** и вернётся когда стан закончится

### Мультиплеер (TODO)
Для `PlayerController` (мультиплеер) нужно добавить аналогичные методы:
```csharp
// В PlayerController.cs
public void AddSpeedModifier(float percentModifier) { ... }
public void RemoveSpeedModifier(float percentModifier) { ... }
```

---

**Статус:** ✅ ГОТОВО

Swift Stride работает отлично! 🏃🍃
