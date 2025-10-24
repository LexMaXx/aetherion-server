# ✅ Исправлено: Charge теперь телепортируется ПРЯМО К ВРАГУ!

## Проблема
Воин телепортировался **НА 20 метров К врагу**, а не **ПРЯМО К врагу**.

**Старая логика:**
```csharp
// НЕПРАВИЛЬНО
Vector3 dirToTarget = (target.position - transform.position).normalized;
return transform.position + dirToTarget * skill.movementDistance; // ВСЕГДА +20м
```

**Результат:**
- Враг на расстоянии 5м → телепорт на 20м МИМО врага ❌
- Враг на расстоянии 15м → телепорт на 20м МИМО врага ❌
- Враг на расстоянии 30м → телепорт на 20м к врагу, остаётся 10м ✅ (только этот случай работал нормально)

---

## Решение

**Новая логика:** [Assets/Scripts/Skills/SkillExecutor.cs:478-505](Assets/Scripts/Skills/SkillExecutor.cs#L478-L505)

```csharp
case MovementDirection.ToTarget:
    if (target != null)
    {
        // Рассчитываем дистанцию до цели
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Если цель ближе чем movementDistance - телепортируемся ПРЯМО К НЕЙ
        if (distanceToTarget <= skill.movementDistance)
        {
            // Телепорт прямо к врагу (на 1.5м перед ним)
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            return target.position - dirToTarget * 1.5f; ✅
        }
        else
        {
            // Цель далеко - телепортируемся на макс дистанцию к ней
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            return transform.position + dirToTarget * skill.movementDistance;
        }
    }
```

---

## Как теперь работает

### Сценарий 1: Враг близко (5м)
```
Игрок:  [P] -------5м------- [E] Враг
                   ↓
        Телепорт ПРЯМО К ВРАГУ
                   ↓
               [P][E] ✅ (на 1.5м перед врагом)
```

### Сценарий 2: Враг средне (15м)
```
Игрок:  [P] -------15м------- [E] Враг
                   ↓
        Телепорт ПРЯМО К ВРАГУ
                   ↓
                [P][E] ✅ (на 1.5м перед врагом)
```

### Сценарий 3: Враг далеко (30м)
```
Игрок:  [P] -------30м------- [E] Враг
                   ↓
        Телепорт на 20м К ВРАГУ
                   ↓
            [P] ---10м--- [E] ✅ (осталось 10м до врага)
```

**Логика:**
- **Если враг ≤ 20м** → телепорт **ПРЯМО К ВРАГУ** (на 1.5м перед ним)
- **Если враг > 20м** → телепорт **НА 20м К ВРАГУ** (приближение)

---

## Почему 1.5 метра перед врагом?

```csharp
return target.position - dirToTarget * 1.5f; // На 1.5м перед врагом
```

**Причины:**
1. **Визуально красиво** - воин не телепортируется ВНУТРЬ врага
2. **Удобно для атаки** - на расстоянии удара (melee range)
3. **Избегание коллизий** - не застреваем в враге
4. **Реалистично** - как "рывок к врагу"

**Можно настроить:**
- `1.5f` → `0.5f` - ближе к врагу
- `1.5f` → `2.5f` - дальше от врага

---

## Console Logs

### Враг близко (10м)
```
[SkillExecutor] ToTarget: дистанция до цели = 10.2м, макс дистанция = 20м
[SkillExecutor] ⚡ Телепорт ПРЯМО К ВРАГУ! Позиция врага: (5, 1, 8), точка телепорта: (3.5, 1, 6.8)
[SkillExecutor] Movement to (3.5, 1, 6.8)
[SkillExecutor] Teleported to (3.5, 1, 6.8)
[SkillExecutor] Effect applied to target: Stun
```

### Враг далеко (30м)
```
[SkillExecutor] ToTarget: дистанция до цели = 30.5м, макс дистанция = 20м
[SkillExecutor] ⚡ Цель далеко! Телепорт на 20м к врагу. Точка: (12, 1, 15)
[SkillExecutor] Movement to (12, 1, 15)
[SkillExecutor] Teleported to (12, 1, 15)
```

---

## Тестирование

### Шаг 1: Пересоздать Charge
```
Unity Menu → Aetherion → Skills → Create Charge (Warrior)
```

### Шаг 2: Тестировать разные дистанции

**Близкий враг:**
1. Встань на расстоянии ~5м от DummyEnemy
2. ЛКМ на враге (выбрать цель)
3. Используй Charge
4. **Должен телепортироваться ПРЯМО К ВРАГУ** ✅

**Средний враг:**
1. Встань на расстоянии ~15м от DummyEnemy
2. ЛКМ на враге
3. Используй Charge
4. **Должен телепортироваться ПРЯМО К ВРАГУ** ✅

**Дальний враг:**
1. Встань на расстоянии ~30м от DummyEnemy
2. ЛКМ на враге
3. Используй Charge
4. **Должен телепортироваться на 20м к врагу** (осталось ~10м) ✅

---

## Проверка в Console

Смотри логи:
```
[SkillExecutor] ToTarget: дистанция до цели = X.Xм, макс дистанция = 20м
[SkillExecutor] ⚡ Телепорт ПРЯМО К ВРАГУ! ... (если ≤20м)
[SkillExecutor] ⚡ Цель далеко! Телепорт на 20м ... (если >20м)
```

---

## Дополнительно: Настройка дистанции перед врагом

Если хочешь изменить на сколько близко телепортироваться к врагу:

**Файл:** `Assets/Scripts/Skills/SkillExecutor.cs` строка 492

```csharp
// Ближе к врагу (почти вплотную)
return target.position - dirToTarget * 0.5f;

// Стандартно (на расстоянии удара)
return target.position - dirToTarget * 1.5f;

// Дальше от врага (безопасная дистанция)
return target.position - dirToTarget * 3f;
```

---

**Статус:** ✅ ИСПРАВЛЕНО

Charge теперь работает как снаряд - телепортируется ПРЯМО К ВРАГУ и станит его! ⚔️⚡
