# ✅ Battle Rage - Боевая Ярость Война

## Описание
**Battle Rage** - пятый и финальный скилл Воина. Мощный бафф, увеличивающий весь наносимый урон на **40%** в течение **12 секунд**.

---

## Параметры скилла

| Параметр | Значение |
|----------|----------|
| **Skill ID** | 405 |
| **Название** | Battle Rage |
| **Класс** | Warrior |
| **Тип** | Buff (Self) |
| **Cooldown** | 18 секунд |
| **Mana Cost** | 45 |
| **Длительность** | 12 секунд |
| **Эффект** | +40% урона ко всем атакам и скиллам |

---

## Эффекты

### ⚔️ IncreaseAttack
- **Тип:** EffectType.IncreaseAttack
- **Power:** 40 (40% увеличение урона)
- **Duration:** 12 секунд
- **Can Stack:** Нет
- **Can Be Dispelled:** Да

### Визуальные эффекты

**Cast Effect (при активации):**
- Префаб: `CFXR3 Fire Explosion B 1`
- Эффект огненной вспышки при активации баффа
- Auto-destroy

**Particle Effect (на персонаже):**
- Префаб: `CFXR3 Fire Explosion B`
- Огненная аура вокруг воина
- Длительность: 12 секунд
- Следует за персонажем (attached to transform)
- Автоматически удаляется после окончания баффа

---

## Как работает Battle Rage

### 1. Применение баффа
При активации Battle Rage:
```
1. Применяется эффект IncreaseAttack (+40%)
2. CharacterStats.attackModifier увеличивается на 40
3. Появляется огненная аура на персонаже
4. Огненная вспышка в точке активации
```

### 2. Модификация урона

Battle Rage увеличивает **ВСЕ** виды урона:

**Базовые атаки:**
- Расчёт в BasicAttackConfig.CalculateDamage()
- Модификатор применяется после скейлинга от Strength/Intelligence
- Формула: `finalDamage = (baseDamage + statScaling) * (1 + attackModifier / 100)`

**Скиллы:**
- Расчёт в SkillExecutor.CalculateDamage()
- Модификатор применяется после скейлинга
- Работает со ВСЕМИ скиллами: Charge, Hammer Throw, и т.д.

**Пример расчёта:**
```
Базовый урон скилла: 50
Бонус от Strength (10 × 1.5): +15
Итого без баффа: 65

С Battle Rage (+40%):
65 + (65 × 0.4) = 65 + 26 = 91 урона

Прирост: +26 урона (+40%)
```

### 3. Снятие баффа

После окончания 12 секунд:
```
1. CharacterStats.attackModifier уменьшается на 40
2. Огненная аура исчезает
3. Урон возвращается к нормальному значению
```

---

## Интеграция с системами

### CharacterStats.cs
**Добавлены методы:**

```csharp
// Line 34
[SerializeField] private float attackModifier = 0f; // Модификатор урона в процентах

// Line 48
public float AttackModifier => attackModifier; // Доступ к модификатору урона

// Lines 339-343
public void AddAttackModifier(float percentModifier)
{
    attackModifier += percentModifier;
    Debug.Log($"[CharacterStats] ⚔️ Урон изменён: {(percentModifier > 0 ? "+" : "")}{percentModifier}% (итого: +{attackModifier}%)");
}

// Lines 348-352
public void RemoveAttackModifier(float percentModifier)
{
    attackModifier -= percentModifier;
    Debug.Log($"[CharacterStats] ⚔️ Модификатор урона снят: {percentModifier}% (итого: +{attackModifier}%)");
}
```

### EffectManager.cs
**Добавлена поддержка IncreaseAttack:**

```csharp
// Lines 202-208 - ApplyImmediateEffect
case EffectType.IncreaseAttack:
    if (characterStats != null)
    {
        characterStats.AddAttackModifier(config.power);
    }
    Log($"⚔️ +{config.power}% к атаке");
    break;

// Lines 342-347 - RemoveImmediateEffect
case EffectType.IncreaseAttack:
    if (characterStats != null)
    {
        characterStats.RemoveAttackModifier(config.power);
    }
    break;
```

### SkillExecutor.cs
**Модифицирован CalculateDamage():**

```csharp
// Lines 595-601
// Применяем модификатор атаки (от Battle Rage и других баффов)
if (stats.AttackModifier > 0)
{
    float bonus = damage * (stats.AttackModifier / 100f);
    damage += bonus;
    Log($"⚔️ Attack modifier applied: +{stats.AttackModifier}% (+{bonus:F1} damage, total: {damage:F1})");
}
```

### BasicAttackConfig.cs
**Модифицирован CalculateDamage():**

```csharp
// Lines 168-174
// Применяем модификатор атаки (от Battle Rage и других баффов)
if (stats.AttackModifier > 0)
{
    float bonus = damage * (stats.AttackModifier / 100f);
    damage += bonus;
    Debug.Log($"[BasicAttackConfig] ⚔️ Attack modifier applied: +{stats.AttackModifier}% (+{bonus:F1} damage)");
}
```

---

## Создание скилла в Unity

### Шаг 1: Запуск Editor Script
```
Unity Menu → Aetherion → Skills → Warrior → Create Battle Rage
```

### Шаг 2: Проверка Asset
Скилл создан по пути:
```
Assets/Resources/Skills/Warrior_BattleRage.asset
```

### Шаг 3: Добавить в SkillBar
Добавь скилл в SkillBar воина:
```
1. Открой сцену с воином
2. Найди SkillBar компонент на воине
3. Перетащи Warrior_BattleRage.asset в список скиллов
4. Настрой hotkey (например, клавиша "5")
```

---

## Тестирование

### Быстрый тест

**Шаг 1:** Проверь базовый урон
```
1. ▶️ Play Scene
2. Атакуй DummyEnemy
3. Запомни урон (например: 65)
```

**Шаг 2:** Активируй Battle Rage
```
1. Нажми клавишу Battle Rage (например, "5")
2. Должно произойти:
   - Огненная вспышка ✅
   - Огненная аура на воине ✅
   - Лог: "⚔️ Урон изменён: +40% (итого: +40%)" ✅
```

**Шаг 3:** Проверь увеличенный урон
```
1. Атакуй того же DummyEnemy
2. Новый урон должен быть ~1.4× (например: 65 → 91)
3. Лог: "⚔️ Attack modifier applied: +40% (+26.0 damage, total: 91.0)" ✅
```

**Шаг 4:** Проверь окончание баффа
```
1. Подожди 12 секунд
2. Огненная аура исчезает ✅
3. Лог: "⚔️ Модификатор урона снят: 40% (итого: +0%)" ✅
4. Урон возвращается к базовому (65) ✅
```

### Детальные проверки

**Console Logs (хорошо):**
```
[EffectManager] ⚔️ +40% к атаке
[CharacterStats] ⚔️ Урон изменён: +40% (итого: +40%)
[EffectManager] ✨ Применён эффект: IncreaseAttack (12с)

[BasicAttackConfig] ⚔️ Attack modifier applied: +40% (+26.0 damage)
[BasicAttackConfig] Warrior Attack Damage: 50 + STR(10×1.5) = 91

[CharacterStats] ⚔️ Модификатор урона снят: 40% (итого: +0%)
[EffectManager] 🔚 Снят эффект: IncreaseAttack
```

**Визуальная проверка:**
- Огненная вспышка при активации ✅
- Огненная аура следует за воином ✅
- Аура длится 12 секунд ✅
- Аура исчезает после окончания ✅

**UI Проверка:**
- Cooldown индикатор работает (18 секунд) ✅
- Mana расходуется (45 маны) ✅
- Иконка Battle Rage корректная ✅

---

## Комбо со скиллами

Battle Rage усиливает **ВСЕ** скиллы воина:

### Charge (401) + Battle Rage (405)
```
1. Активируй Battle Rage
2. Используй Charge на врага
3. Урон Charge увеличится на 40%!
```

### Hammer Throw (402) + Battle Rage (405)
```
Без Battle Rage: 50 + (10 STR × 1.5) = 65 урона
С Battle Rage: 65 × 1.4 = 91 урон (+26 урона!)
```

### Defensive Stance (403) + Battle Rage (405)
```
Можно комбинировать: уменьшение получаемого урона на 50% + увеличение наносимого урона на 40%
Танкующий DPS!
```

### Battle Heal (404) + Battle Rage (405)
```
Battle Rage НЕ увеличивает лечение (только урон)
Но можно вылечиться и затем активировать Battle Rage для максимального DPS
```

---

## Технические детали

### Модификаторы статов

Battle Rage использует **процентный модификатор**, а не **абсолютный**:

```csharp
// ПРАВИЛЬНО (процентный модификатор)
float bonus = baseDamage * (attackModifier / 100f);
finalDamage = baseDamage + bonus;

// НЕПРАВИЛЬНО (абсолютный модификатор)
finalDamage = baseDamage + attackModifier; // Нет! Это добавит +40 урона, а не +40%
```

### Стакинг

Battle Rage **не стакается**:
- Если активировать повторно - обновится длительность
- Второе применение **НЕ** даст +80% урона
- Эффект всегда +40%

### Dispel

Battle Rage **может быть снят** (canBeDispelled = true):
- Если враг использует Dispel/Cleanse - бафф снимется досрочно
- Это баланс: мощный бафф, но уязвим к контролю

---

## Файлы

### Создано:
- [Assets/Scripts/Editor/CreateBattleRage.cs](Assets/Scripts/Editor/CreateBattleRage.cs) - Editor script для создания скилла
- Assets/Resources/Skills/Warrior_BattleRage.asset - ScriptableObject скилла (создаётся через меню)

### Модифицировано:
- [Assets/Scripts/Stats/CharacterStats.cs](Assets/Scripts/Stats/CharacterStats.cs) - Добавлен attackModifier и методы Add/Remove
- [Assets/Scripts/Skills/EffectManager.cs](Assets/Scripts/Skills/EffectManager.cs) - Добавлена поддержка IncreaseAttack
- [Assets/Scripts/Skills/SkillExecutor.cs](Assets/Scripts/Skills/SkillExecutor.cs) - Модифицирован CalculateDamage() для применения attackModifier
- [Assets/Scripts/Combat/BasicAttackConfig.cs](Assets/Scripts/Combat/BasicAttackConfig.cs) - Модифицирован CalculateDamage() для базовых атак

---

## Балансировка

### Сильные стороны:
- Огромный прирост урона (+40%)
- Усиливает ВСЕ атаки и скиллы
- Долгая длительность (12 секунд)
- Доступно часто (18 сек кулдаун)

### Слабые стороны:
- Не защищает воина
- Может быть снято Dispel
- Требует маны (45)
- Не работает на лечение

### Сравнение с другими баффами:
- **Deadly Precision (Archer):** +40% крит урон (только на криты)
- **Battle Rage (Warrior):** +40% урон (на все атаки)
- **Eagle Eye (Archer):** +5 Perception (радиус обзора)
- **Swift Stride (Archer):** +50% скорость (мобильность)

Battle Rage - самый простой и универсальный DPS-бафф!

---

## Статус: ✅ ГОТОВО

Все 5 скиллов Воина завершены:

1. ✅ **Charge** (401) - Телепорт к врагу + 5 секунд стан
2. ✅ **Hammer Throw** (402) - Дальний стан (3 секунды)
3. ✅ **Defensive Stance** (403) - 50% снижение урона на 10 секунд
4. ✅ **Battle Heal** (404) - Лечение 20% HP
5. ✅ **Battle Rage** (405) - +40% урон на 12 секунд

**Warrior - готов к бою!** ⚔️🔥
