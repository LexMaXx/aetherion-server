# ✅ Soul Drain - Кража Души Некроманта

## Описание
**Soul Drain** - первый скилл Rogue (Некромант). Выстреливает призрачный череп, который наносит урон врагу и **восстанавливает 100% нанесённого урона** в виде HP некроманту.

Чем сильнее атака некроманта - тем больше HP он восстановит!

---

## Параметры скилла

| Параметр | Значение |
|----------|----------|
| **Skill ID** | 501 |
| **Название** | Soul Drain |
| **Класс** | Rogue (Necromancer) |
| **Тип** | DamageAndHeal (Вампиризм) |
| **Cooldown** | 4 секунды |
| **Mana Cost** | 25 |
| **Дальность** | 15 метров |

---

## Урон и скейлинг

| Компонент | Значение |
|-----------|----------|
| **Базовый урон** | 40 |
| **Intelligence Scaling** | +120% (некромант - маг) |
| **Strength Scaling** | +30% (физический компонент) |
| **Life Steal** | **100%** от нанесённого урона |

### Формула урона:
```
Урон = 40 + (Intelligence × 1.2) + (Strength × 0.3)
```

### Примеры:
**Некромант с 10 INT, 5 STR:**
```
Урон = 40 + (10 × 1.2) + (5 × 0.3)
     = 40 + 12 + 1.5
     = 53.5 урона

Лечение = 53.5 HP (100% вампиризм)
```

**Некромант с 20 INT, 8 STR:**
```
Урон = 40 + (20 × 1.2) + (8 × 0.3)
     = 40 + 24 + 2.4
     = 66.4 урона

Лечение = 66.4 HP
```

**С Battle Rage (+40% урона):**
```
Базовый урон: 66.4
С Battle Rage: 66.4 × 1.4 = 92.96 урона

Лечение = 92.96 HP! (вампиризм работает с модифицированным уроном)
```

---

## Визуальные эффекты

### Снаряд
- **Префаб:** `Ethereal_Skull_1020210937_texture.prefab` (призрачный череп)
- **Fallback:** `SoulShardsProjectile.prefab` (если череп не найден)
- **Скорость:** 20 м/с
- **Автонаведение:** Да

### Cast Effect (при запуске)
- **Префаб:** `CFXR3 Hit Electric C (Air)`
- Тёмная энергия вокруг некроманта

### Hit Effect (при попадании в врага)
- **Префаб:** `CFXR3 Hit Electric C (Air)`
- Взрыв тёмной энергии на враге

### Caster Effect (лечение некроманта)
- **Префаб:** `CFXR3 Hit Light B (Air)`
- Световая вспышка на некроманте при получении HP
- Появляется **автоматически** при попадании снаряда

---

## Как работает Soul Drain

### 1. Каст скилла
```
1. Некромант использует Soul Drain
2. Расходуется 25 маны
3. Появляется cast effect (тёмная энергия)
4. Запускается снаряд (череп) к цели
```

### 2. Полёт снаряда
```
1. Череп летит со скоростью 20 м/с
2. Автоматически наводится на цель
3. Если цель умерла - ищет новую в радиусе
```

### 3. Попадание в цель
```
1. Череп попадает в врага
2. Наносится урон (например: 66.4)
3. Появляется hit effect (взрыв на враге)
4. Рассчитывается лечение: 66.4 × 100% = 66.4 HP
5. Некромант получает 66.4 HP
6. Появляется caster effect (вспышка света на некроманте)
```

---

## Интеграция с системами

### SkillConfig.cs
**Добавлены новые поля:**

```csharp
// Lines 88-90
[Tooltip("Процент Life Steal (восстановление HP от нанесённого урона, 0-100%)")]
[Range(0f, 100f)]
public float lifeStealPercent = 0f;

// Lines 150-151
[Tooltip("Визуальный эффект на кастере (для лечения, баффов и т.д.)")]
public GameObject casterEffectPrefab;
```

**Новый enum тип:**
```csharp
// Line 343
DamageAndHeal,      // Урон + лечение (Soul Drain - вампиризм)
```

### CelestialProjectile.cs
**Добавлена поддержка Life Steal:**

```csharp
// Lines 48-50 - Новые поля
private float lifeStealPercent = 0f;       // Процент вампиризма
private GameObject casterEffectPrefab;     // Эффект на кастере

// Lines 117-122 - Установка Life Steal
public void SetLifeSteal(float percent, GameObject casterEffect = null)
{
    lifeStealPercent = percent;
    casterEffectPrefab = casterEffect;
    Debug.Log($"[CelestialProjectile] 🧛 Life Steal установлен: {percent}%");
}

// Lines 311-334 - Лечение владельца при попадании
if (lifeStealPercent > 0 && owner != null)
{
    float healAmount = damage * (lifeStealPercent / 100f);
    HealthSystem ownerHealth = owner.GetComponent<HealthSystem>();

    if (ownerHealth != null)
    {
        ownerHealth.Heal(healAmount);
        Debug.Log($"[CelestialProjectile] 🧛 Life Steal: +{healAmount:F1} HP ({lifeStealPercent}% от {damage:F1} урона)");

        // Визуальный эффект лечения на кастере
        if (casterEffectPrefab != null)
        {
            GameObject healEffect = Instantiate(casterEffectPrefab, owner.transform.position + Vector3.up * 1.5f, Quaternion.identity);
            Destroy(healEffect, 2f);
        }
    }
}
```

### SkillExecutor.cs
**Добавлена передача Life Steal в снаряд:**

```csharp
// Lines 182-187
// Устанавливаем Life Steal если скилл имеет вампиризм
if (skill.lifeStealPercent > 0)
{
    celestialProj.SetLifeSteal(skill.lifeStealPercent, skill.casterEffectPrefab);
    Log($"🧛 Life Steal активирован: {skill.lifeStealPercent}%");
}
```

**Добавлена обработка DamageAndHeal:**
```csharp
// Lines 100-102
case SkillConfigType.ProjectileDamage:
case SkillConfigType.DamageAndHeal: // Soul Drain и другие вампирические скиллы
    ExecuteProjectile(skill, target, groundTarget);
```

---

## Создание скилла в Unity

### Шаг 1: Запуск Editor Script
```
Unity Menu → Aetherion → Skills → Rogue → Create Soul Drain
```

### Шаг 2: Проверка Asset
Скилл создан по пути:
```
Assets/Resources/Skills/Rogue_SoulDrain.asset
```

### Шаг 3: Добавить в SkillBar
```
1. Открой сцену с Rogue (Necromancer)
2. Найди SkillBar компонент
3. Перетащи Rogue_SoulDrain.asset в список скиллов
4. Настрой hotkey (например, клавиша "1")
```

---

## Тестирование

### Быстрый тест

**Шаг 1:** Запуск скилла
```
1. ▶️ Play Scene
2. Прицелься на DummyEnemy
3. Нажми клавишу Soul Drain (например, "1")
```

**Ожидаемое поведение:**
```
✅ Расход 25 маны
✅ Появление cast effect (тёмная энергия на некроманте)
✅ Запуск черепа к врагу
✅ Череп летит с автонаведением
```

**Шаг 2:** Попадание и вампиризм
```
1. Череп попадает в DummyEnemy
2. Наносится урон (отображается красным числом)
3. Некромант получает HP (зелёное число или HP bar увеличивается)
4. Появляется световая вспышка на некроманте (caster effect)
```

**Шаг 3:** Проверка логов
```
Console → Открой логи
```

**Ожидаемые логи:**
```
[SkillExecutor] 🧛 Life Steal активирован: 100%
[CelestialProjectile] 🧛 Life Steal установлен: 100%
[CelestialProjectile] 💥 Попадание в DummyEnemy_1! Урон: 53.5
[CelestialProjectile] 🧛 Life Steal: +53.5 HP (100% от 53.5 урона)
[CelestialProjectile] ✨ Эффект лечения на кастере: CFXR3 Hit Light B (Air)
[HealthSystem] 💚 Лечение: +53.5 HP (текущее: 153.5/100)
```

### Детальные проверки

**Тест 1: Урон масштабируется от статов**
```
1. Проверь текущий Intelligence некроманта (например: 10)
2. Используй Soul Drain
3. Урон = 40 + (10 × 1.2) + (Str × 0.3)
4. Проверь что урон соответствует формуле
```

**Тест 2: Лечение = 100% урона**
```
1. Запомни текущий HP (например: 80/100)
2. Нанеси 50 урона врагу
3. HP должно стать: 80 + 50 = 130 (или 100/100 если было полное HP)
4. Проверь что лечение = урон
```

**Тест 3: Работа с Battle Rage**
```
1. Активируй Battle Rage (+40% урона)
2. Используй Soul Drain
3. Урон должен быть × 1.4
4. Лечение также × 1.4!
```

**Тест 4: Короткий кулдаун (спамабельность)**
```
1. Используй Soul Drain
2. Подожди 4 секунды
3. Скилл снова доступен
4. Можно спамить для постоянного вампиризма
```

---

## Балансировка

### Сильные стороны:
- **Высокий sustain** - постоянное восстановление HP
- **Короткий кулдаун** (4 сек) - можно спамить
- **Низкая стоимость маны** (25) - не иссякает мана
- **100% вампиризм** - полное восстановление нанесённого урона
- **Хороший урон** - масштабируется от Intelligence

### Слабые стороны:
- **Требует попадания** - если промах, нет лечения
- **Одиночная цель** - лечит только от 1 врага
- **Зависимость от урона** - слабый урон = слабое лечение
- **Нужна цель** - не работает на себя без врага

### Синергия с баффами:
- **Battle Rage:** +40% урон → +40% лечение!
- **Deadly Precision:** Критический урон → Критическое лечение!
- **IncreaseAttack эффект:** Любой бафф урона увеличивает лечение

---

## Стратегия использования

### PvE (против монстров):
```
1. Используй Soul Drain как основную атаку
2. Короткий кулдаун позволяет спамить
3. Поддерживай HP на высоком уровне
4. Комбинируй с другими урон-скиллами
```

### Танкование:
```
1. Soul Drain + высокая защита = почти бессмертный танк
2. Чем больше врагов атакуешь, тем больше лечишься
3. Комбинируй с Defensive Stance (если есть) для -50% урона
```

### Burst Healing:
```
1. Активируй Battle Rage (+40% урона)
2. Используй Soul Drain
3. Получи огромное лечение от повышенного урона
```

---

## Сравнение с другими скиллами

| Скилл | Тип | Cooldown | Эффект |
|-------|-----|----------|--------|
| **Soul Drain** | Урон + лечение | 4 сек | 100% вампиризм |
| Battle Heal | Только лечение | 15 сек | 20% MaxHP (фиксированное) |
| Fireball | Только урон | 5 сек | Высокий урон, нет лечения |

**Преимущество Soul Drain:**
- Самый **короткий кулдаун** для лечения (4 сек vs 15 сек Battle Heal)
- **Динамическое лечение** - чем сильнее урон, тем больше HP
- **Наносит урон** одновременно с лечением

---

## Технические детали

### Life Steal механика

Life Steal применяется **после всех модификаторов**:
```csharp
1. Рассчитывается базовый урон + скейлинг
2. Применяются модификаторы (Battle Rage, IncreaseAttack)
3. Наносится финальный урон
4. Лечение = финальный урон × (lifeStealPercent / 100)
```

**Пример:**
```
Базовый урон: 40 + (10 INT × 1.2) = 52
Battle Rage: 52 × 1.4 = 72.8 урона
Life Steal: 72.8 × 100% = 72.8 HP лечения
```

### Сетевая синхронизация

Life Steal работает **локально** на клиенте:
- Урон по NPC - локально
- Лечение кастера - локально
- Визуальные эффекты - синхронизируются с сервером

Для PvP нужно будет добавить серверную валидацию.

---

## Будущие улучшения

### Возможные вариации:
1. **Enhanced Soul Drain** - 150% life steal (талант)
2. **Soul Burst** - AOE версия Soul Drain (лечение от нескольких врагов)
3. **Dark Pact** - увеличенный урон, но уменьшенный life steal (50%)

---

## Файлы

### Создано:
- [Assets/Scripts/Editor/CreateSoulDrain.cs](Assets/Scripts/Editor/CreateSoulDrain.cs) - Editor script для создания скилла
- Assets/Resources/Skills/Rogue_SoulDrain.asset - ScriptableObject скилла (создаётся через меню)

### Модифицировано:
- [Assets/Scripts/Skills/SkillConfig.cs](Assets/Scripts/Skills/SkillConfig.cs) - Добавлены поля `lifeStealPercent` и `casterEffectPrefab`, enum `DamageAndHeal`
- [Assets/Scripts/Player/CelestialProjectile.cs](Assets/Scripts/Player/CelestialProjectile.cs) - Добавлена поддержка Life Steal
- [Assets/Scripts/Skills/SkillExecutor.cs](Assets/Scripts/Skills/SkillExecutor.cs) - Передача Life Steal в снаряд

---

## Статус: ✅ ГОТОВО

**Soul Drain** - первый скилл Rogue (Necromancer) полностью реализован!

Некромант теперь может красть жизненную силу у врагов! 🧛‍♂️💀✨
