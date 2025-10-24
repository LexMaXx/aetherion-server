# ✅ Curse of Weakness - Проклятье Слабости Некроманта

## Описание
**Curse of Weakness** - второй скилл Rogue (Некромант). Мощное проклятие, которое **снижает Perception противника до 1** на 10 секунд, практически ослепляя врага.

Неважно сколько Perception у врага - **проклятие установит его в 1**, минимизируя радиус обзора!

---

## Параметры скилла

| Параметр | Значение |
|----------|----------|
| **Skill ID** | 502 |
| **Название** | Curse of Weakness |
| **Класс** | Rogue (Necromancer) |
| **Тип** | ProjectileDamage + Debuff |
| **Cooldown** | 8 секунд |
| **Mana Cost** | 35 |
| **Дальность** | 20 метров |
| **Длительность эффекта** | 10 секунд |

---

## Урон и эффект

| Компонент | Значение |
|-----------|----------|
| **Базовый урон** | 20 |
| **Intelligence Scaling** | +50% |
| **Эффект** | **Perception → 1** (независимо от оригинального значения) |
| **Длительность** | 10 секунд |

### Формула урона:
```
Урон = 20 + (Intelligence × 0.5)
```

### Примеры:
**Некромант с 10 INT:**
```
Урон = 20 + (10 × 0.5) = 20 + 5 = 25 урона
Эффект: Perception враго 10 → 1
```

**Некромант с 20 INT:**
```
Урон = 20 + (20 × 0.5) = 20 + 10 = 30 урона
Эффект: Perception врага 8 → 1
```

---

## Как работает эффект

### До проклятия:
```
Враг: Perception = 8
VisionRadius = ~20 метров (видит далеко)
```

### После проклятия:
```
Враг: Perception = 1
VisionRadius = ~5 метров (почти слепой!)
```

### Система работы:

**1. Применение проклятия:**
```csharp
// CharacterStats.SetPerception(1)
1. Сохраняется оригинальный Perception (например: 8)
2. Perception устанавливается в 1
3. Пересчитывается VisionRadius (становится минимальным)
4. Враг теперь видит только на ~5 метров!
```

**2. Окончание проклятия (через 10 секунд):**
```csharp
// CharacterStats.RestorePerception()
1. Восстанавливается оригинальный Perception (8)
2. Пересчитывается VisionRadius (возвращается к норме)
3. Враг снова видит нормально
```

---

## Визуальные эффекты

### Снаряд
- **Префаб:** `SoulShardsProjectile.prefab` (тёмная энергия)
- **Fallback:** `Ethereal_Skull_1020210937_texture.prefab` (череп)
- **Скорость:** 18 м/с
- **Автонаведение:** Да

### Cast Effect (при запуске)
- **Префаб:** `CFXR3 Hit Electric C (Air)`
- Тёмная энергия вокруг некроманта

### Hit Effect (при попадании в врага)
- **Префаб:** `CFXR3 Hit Electric C (Air)`
- Взрыв тёмной энергии на враге

### Debuff Effect (проклятие на враге)
- **Префаб:** `CFXR3 Hit Electric C (Air)`
- Тёмная аура над головой врага
- Длительность: 10 секунд
- Следует за врагом

---

## Стратегическое применение

### PvP (против игроков):
```
1. Враг с высоким Perception (8-10) видит далеко
2. Применяй Curse of Weakness
3. Perception врага → 1 (радиус обзора минимальный)
4. Враг не видит тебя на расстоянии!
5. Можно подойти незаметно или убежать
```

### PvE (против монстров):
```
1. Мощный монстр с большим радиусом агро
2. Применяй проклятие
3. Монстр теперь почти слепой
4. Можно обойти или атаковать издалека
```

### Комбо с другими скиллами:
```
1. Curse of Weakness (враг слепнет)
2. Soul Drain (вампиризм, враг не может убежать т.к. не видит)
3. Враг дезориентирован и получает урон + не может убежать
```

---

## Интеграция с системами

### EffectConfig.cs
**Добавлен новый тип эффекта:**

```csharp
// Line 246
[Tooltip("Уменьшение восприятия (снижает радиус обзора, устанавливает perception = 1)")]
DecreasePerception,
```

### CharacterStats.cs
**Добавлены методы для управления Perception:**

```csharp
// Lines 36-37 - Сохранение оригинального значения
private int originalPerception = -1; // -1 = не сохранено

// Lines 361-373 - Установка Perception
public void SetPerception(int value)
{
    // Сохраняем оригинальное значение если ещё не сохранено
    if (originalPerception == -1)
    {
        originalPerception = perception;
        Debug.Log($"[CharacterStats] 💾 Оригинальный Perception сохранён: {originalPerception}");
    }

    perception = value;
    RecalculateStats(); // Пересчитываем visionRadius
    Debug.Log($"[CharacterStats] 👁️ Perception установлен в {value} (было: {originalPerception}, visionRadius: {visionRadius}м)");
}

// Lines 378-391 - Восстановление Perception
public void RestorePerception()
{
    if (originalPerception != -1)
    {
        perception = originalPerception;
        RecalculateStats();
        Debug.Log($"[CharacterStats] 🔄 Perception восстановлен: {perception} (visionRadius: {visionRadius}м)");
        originalPerception = -1; // Сброс
    }
    else
    {
        Debug.LogWarning("[CharacterStats] ⚠️ Нет сохранённого Perception для восстановления!");
    }
}
```

### EffectManager.cs
**Добавлена поддержка DecreasePerception:**

```csharp
// Lines 253-260 - ApplyImmediateEffect
case EffectType.DecreasePerception:
    if (characterStats != null)
    {
        // Устанавливаем perception в 1 (проклятие)
        characterStats.SetPerception(1);
    }
    Log($"👁️🔻 Perception снижен до 1 (проклятие)");
    break;

// Lines 397-403 - RemoveImmediateEffect
case EffectType.DecreasePerception:
    if (characterStats != null)
    {
        // Восстанавливаем оригинальный perception
        characterStats.RestorePerception();
    }
    break;
```

---

## Создание скилла в Unity

### Шаг 1: Запуск Editor Script
```
Unity Menu → Aetherion → Skills → Rogue → Create Curse of Weakness
```

### Шаг 2: Проверка Asset
Скилл создан по пути:
```
Assets/Resources/Skills/Rogue_CurseOfWeakness.asset
```

### Шаг 3: Добавить в SkillBar
```
1. Открой сцену с Rogue (Necromancer)
2. Найди SkillBar компонент
3. Перетащи Rogue_CurseOfWeakness.asset в список скиллов
4. Настрой hotkey (например, клавиша "2")
```

---

## Тестирование

### Быстрый тест

**Шаг 1: Подготовка**
```
1. ▶️ Play Scene
2. Найди DummyEnemy или другого врага
3. Проверь его Perception в Inspector (например: 5)
4. Проверь его VisionRadius (например: 15м)
```

**Шаг 2: Применение проклятия**
```
1. Прицелься на врага
2. Нажми клавишу Curse of Weakness (например, "2")
```

**Ожидаемое поведение:**
```
✅ Расход 35 маны
✅ Появление cast effect (тёмная энергия на некроманте)
✅ Запуск снаряда к врагу
✅ Попадание снаряда
✅ Появление hit effect (взрыв на враге)
✅ Появление debuff aura над врагом (тёмная аура)
✅ Небольшой урон врагу (~25-30)
```

**Шаг 3: Проверка эффекта**
```
1. Открой Inspector врага
2. Найди CharacterStats компонент
3. Perception должен быть = 1
4. VisionRadius должен быть минимальным (~5м)
```

**Шаг 4: Проверка восстановления**
```
1. Подожди 10 секунд
2. Тёмная аура исчезает
3. Perception восстанавливается к оригинальному значению (5)
4. VisionRadius восстанавливается к норме (15м)
```

### Проверка логов

**Ожидаемые логи (применение):**
```
[CharacterStats] 💾 Оригинальный Perception сохранён: 5
[CharacterStats] 👁️ Perception установлен в 1 (было: 5, visionRadius: 5м)
[EffectManager] 👁️🔻 Perception снижен до 1 (проклятие)
[EffectManager] ✨ Применён эффект: DecreasePerception (10с)
```

**Ожидаемые логи (окончание):**
```
[CharacterStats] 🔄 Perception восстановлен: 5 (visionRadius: 15м)
[EffectManager] 🔚 Снят эффект: DecreasePerception
```

### Детальные проверки

**Тест 1: Независимость от оригинального Perception**
```
Враг 1: Perception = 3 → После проклятия = 1 ✅
Враг 2: Perception = 8 → После проклятия = 1 ✅
Враг 3: Perception = 10 → После проклятия = 1 ✅

Проклятие ВСЕГДА устанавливает Perception в 1!
```

**Тест 2: Восстановление после окончания**
```
До: Perception = 7, VisionRadius = 18м
Проклятие: Perception = 1, VisionRadius = 5м
После (10 сек): Perception = 7, VisionRadius = 18м ✅
```

**Тест 3: Не стакается**
```
1. Применить Curse of Weakness
2. Применить ещё раз (через 2 секунды)
3. Длительность обновляется до 10 секунд (не стакается)
4. Perception остаётся = 1 (не становится 0 или отрицательным)
```

**Тест 4: Можно снять Dispel**
```
1. Применить Curse of Weakness
2. Враг использует Dispel/Cleanse
3. Проклятие снимается досрочно
4. Perception восстанавливается
```

---

## Балансировка

### Сильные стороны:
- **Мощный дебафф** - полностью ослепляет врага
- **Независимость от статов** - работает на любого врага
- **Долгая длительность** (10 сек) - много времени для действий
- **Средний кулдаун** (8 сек) - можно часто использовать
- **Низкая стоимость маны** (35) - экономичный скилл

### Слабые стороны:
- **Небольшой урон** - это дебафф, а не урон-скилл
- **Можно снять** (canBeDispelled = true)
- **Не стакается** - повторное применение только обновляет длительность
- **Требует попадания** - снаряд может промазать

### PvP эффективность:
- **Против снайперов:** Отлично! Враг с высоким Perception теряет преимущество
- **Против танков:** Средне. Танк всё равно близко, Perception не так важен
- **Против магов:** Отлично! Маги полагаются на дистанцию и обзор

---

## Сравнение с другими дебаффами

| Дебафф | Эффект | Длительность | Cooldown | Снимается? |
|--------|--------|--------------|----------|------------|
| **Curse of Weakness** | Perception = 1 | 10 сек | 8 сек | Да |
| Stun (Charge) | Оглушение | 5 сек | 12 сек | Да |
| Root (Entangling Shot) | Корни | 4 сек | 10 сек | Да |
| DecreaseSpeed | Замедление | Varies | Varies | Да |

**Уникальность Curse of Weakness:**
- Единственный скилл, снижающий **Perception**
- Работает через **установку значения**, а не модификатор
- Сильно влияет на **FoV (Field of View)** врага

---

## Комбинации со скиллами

### Curse + Soul Drain:
```
1. Curse of Weakness (враг слепнет, Perception = 1)
2. Soul Drain (вампиризм, постоянное лечение)
3. Враг не может эффективно убежать (не видит)
4. Некромант восстанавливает HP и контролирует бой
```

### Curse + Kiting:
```
1. Curse of Weakness на врага
2. Отойти на 10-15 метров
3. Враг не видит тебя (VisionRadius < расстояние)
4. Атакуй издалека безопасно
```

### Curse + Stealth (если есть):
```
1. Curse of Weakness
2. Активировать Invisibility/Stealth
3. Враг точно не увидит (Perception = 1 + невидимость)
4. Идеальный побег или засада
```

---

## Технические детали

### Почему SetPerception, а не ModifyPerception?

**ModifyPerception (относительное изменение):**
```csharp
perception += amount; // Если amount = -5
// Враг с Perception 8 → 3
// Враг с Perception 3 → -2 (отрицательное! баг!)
```

**SetPerception (абсолютное значение):**
```csharp
perception = value; // value = 1
// Враг с Perception 8 → 1 ✅
// Враг с Perception 3 → 1 ✅
// Враг с Perception 10 → 1 ✅
```

**SetPerception гарантирует:**
- Perception **всегда** = 1 (независимо от оригинала)
- Никогда не уйдёт в отрицательные значения
- Оригинальное значение сохранено для восстановления

### Сохранение оригинального значения:

```csharp
// Первое применение проклятия
originalPerception = -1 (не сохранено)
SetPerception(1):
    originalPerception = perception; // Сохраняем 8
    perception = 1;

// Повторное применение (пока первое активно)
originalPerception = 8 (уже сохранено!)
SetPerception(1):
    // НЕ перезаписываем originalPerception!
    perception = 1;

// Восстановление
RestorePerception():
    perception = originalPerception; // Восстанавливаем 8
    originalPerception = -1; // Сброс флага
```

Это предотвращает баг, когда повторное проклятие перезаписывает сохранённое значение!

---

## Будущие улучшения

### Возможные вариации:
1. **Greater Curse** - Perception = 1, Duration = 15 сек
2. **Curse of Blindness** - Perception = 0 (полная слепота!)
3. **AOE Curse** - Проклятие на всех врагов в радиусе
4. **Curse of Silence** - Блокирует скиллы + Perception = 1

---

## Файлы

### Создано:
- [Assets/Scripts/Editor/CreateCurseOfWeakness.cs](Assets/Scripts/Editor/CreateCurseOfWeakness.cs) - Editor script для создания скилла
- Assets/Resources/Skills/Rogue_CurseOfWeakness.asset - ScriptableObject скилла (создаётся через меню)

### Модифицировано:
- [Assets/Scripts/Skills/EffectConfig.cs](Assets/Scripts/Skills/EffectConfig.cs) - Добавлен enum `DecreasePerception`
- [Assets/Scripts/Stats/CharacterStats.cs](Assets/Scripts/Stats/CharacterStats.cs) - Добавлены методы `SetPerception()` и `RestorePerception()`
- [Assets/Scripts/Skills/EffectManager.cs](Assets/Scripts/Skills/EffectManager.cs) - Добавлена поддержка `DecreasePerception`

---

## Статус: ✅ ГОТОВО

**Curse of Weakness** - второй скилл Rogue (Necromancer) полностью реализован!

Некромант теперь может ослеплять врагов проклятием! 👁️🔻💀✨

**2 из 5 скиллов Некроманта завершены:**
1. ✅ Soul Drain - Вампиризм (100% life steal)
2. ✅ **Curse of Weakness - Ослепление (Perception → 1)** 🆕
