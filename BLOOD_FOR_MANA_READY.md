# ✅ Blood for Mana - Жертвенное Заклинание Некроманта

## Описание
**Blood for Mana** - четвёртый скилл Rogue (Некромант). Жертвенное заклинание, которое **жертвует 20% HP** для восстановления **20% маны**.

**Уникальная особенность:** НЕ требует ману для использования! Это аварийное заклинание когда мана закончилась.

---

## Параметры скилла

| Параметр | Значение |
|----------|----------|
| **Skill ID** | 504 |
| **Название** | Blood for Mana |
| **Класс** | Rogue (Necromancer) |
| **Тип** | Buff (Self, жертвенный) |
| **Cooldown** | 15 секунд |
| **Mana Cost** | **0** (БЕСПЛАТНО!) |
| **Жертва** | 20% текущего HP |
| **Восстановление** | 20% максимальной маны |

---

## Механика

### Формула жертвы:
```
HP Sacrifice = MaxHP × 20%
```

### Формула восстановления:
```
Mana Restore = MaxMP × 20%
```

### Примеры:

**Некромант с 100 MaxHP, 80 MaxMP:**
```
Жертва: 100 × 0.2 = 20 HP
Восстановление: 80 × 0.2 = 16 MP

Результат: -20 HP → +16 MP
```

**Некромант с 180 MaxHP, 120 MaxMP:**
```
Жертва: 180 × 0.2 = 36 HP
Восстановление: 120 × 0.2 = 24 MP

Результат: -36 HP → +24 MP
```

---

## Как работает

### 1. Проверка HP:
```csharp
if (CurrentHP <= HPSacrifice)
{
    // Недостаточно HP! Нельзя убить себя
    return "⚠️ Недостаточно HP для жертвы"
}
```

**Безопасность:**
- Нельзя использовать если HP ≤ 20% MaxHP
- Защита от самоубийства

### 2. Жертва HP:
```csharp
HealthSystem.TakeDamage(HPSacrifice);
// -36 HP (180 → 144)
```

### 3. Восстановление MP:
```csharp
ManaSystem.RestoreMana(ManaRestore);
// +24 MP (0 → 24)
```

### 4. Визуальные эффекты:
- **Cast Effect:** Кровавая жертва (красный взрыв)
- **Caster Effect:** Восстановление маны (синяя вспышка)

---

## Стратегическое применение

### Ситуация 1: Закончилась мана в бою
```
1. HP: 180/180, MP: 5/120 (мана закончилась!)
2. Используй Blood for Mana
3. HP: 144/180, MP: 29/120 (есть мана!)
4. Теперь можешь использовать Soul Drain и другие скиллы
```

### Ситуация 2: Экстренное восстановление
```
Враг убегает, мана на нуле:
1. Blood for Mana (получаешь ману)
2. Soul Drain / Curse of Weakness (добиваешь)
3. Soul Drain лечит тебя обратно!
```

### Ситуация 3: Комбо с Soul Drain
```
1. Blood for Mana (-20% HP, +20% MP)
2. Soul Drain с полученной маной (урон + вампиризм)
3. Восстанавливаешь потерянный HP через life steal!

Итого: Обмен HP на MP → обмен MP на HP = цикл ресурсов!
```

---

## Балансировка

### Сильные стороны:
- **НЕ требует ману** - можно использовать при 0 MP!
- **Экстренное восстановление** - спасает когда мана закончилась
- **Умеренный кулдаун** (15 сек) - можно использовать довольно часто
- **Процентное восстановление** - чем больше MaxMP, тем больше восстанавливается

### Слабые стороны:
- **Жертва HP** - опасно при низком HP!
- **Не работает при низком HP** (≤20% HP)
- **Средний кулдаун** - нельзя спамить
- **Нет прямой пользы** - только конверсия ресурсов

### Риски:
```
⚠️ ОПАСНО при HP < 40%!
⚠️ НЕ используй если вокруг враги!
⚠️ Можешь умереть если тебя атакуют сразу после!
```

---

## Визуальные эффекты

### Cast Effect (жертва крови)
- **Префаб:** `CFXR3 Fire Explosion B`
- Кровавый/красный взрыв вокруг некроманта
- Символизирует жертву HP

### Caster Effect (восстановление маны)
- **Префаб:** `CFXR3 Magic Poof`
- Синяя/магическая вспышка над некромантом
- Символизирует восстановление маны

---

## Интеграция с системами

### SkillConfig.cs
**Добавлено поле manaRestorePercent:**

```csharp
// Lines 424-427
[Header("Blood for Mana (жертвенное заклинание)")]
[Tooltip("Процент маны для восстановления (для Blood for Mana)")]
[Range(0f, 100f)]
public float manaRestorePercent = 0f;
```

### SkillExecutor.cs
**Добавлена обработка Blood for Mana:**

```csharp
// Lines 538-545 - Проверка в ExecuteBuff
if (skill.customData != null && skill.customData.manaRestorePercent > 0)
{
    ExecuteBloodForMana(skill);
    return;
}

// Lines 600-655 - ExecuteBloodForMana метод
private void ExecuteBloodForMana(SkillConfig skill)
{
    // Получаем системы
    HealthSystem healthSystem = GetComponent<HealthSystem>();
    ManaSystem manaSystem = GetComponent<ManaSystem>();

    // Рассчитываем жертву
    float hpSacrifice = healthSystem.MaxHealth * 0.2f; // 20%
    float manaRestore = manaSystem.MaxMana * 0.2f;    // 20%

    // Проверка безопасности
    if (healthSystem.CurrentHealth <= hpSacrifice)
    {
        return; // Нельзя убить себя
    }

    // Жертвуем HP и восстанавливаем MP
    healthSystem.TakeDamage(hpSacrifice);
    manaSystem.RestoreMana(manaRestore);
}
```

### ManaSystem.cs
**Используется существующий метод RestoreMana:**

```csharp
// Line 144
public void RestoreMana(float amount)
{
    currentMana += amount;
    currentMana = Mathf.Min(currentMana, maxMana);
}
```

---

## Создание скилла в Unity

### Шаг 1: Запуск Editor Script
```
Unity Menu → Aetherion → Skills → Rogue → Create Blood for Mana
```

### Шаг 2: Проверка Asset
Скилл создан по пути:
```
Assets/Resources/Skills/Rogue_BloodForMana.asset
```

**ВАЖНО:** Проверь что `customData.manaRestorePercent = 20`!

### Шаг 3: Добавить в SkillBar
```
1. Открой сцену с Rogue (Necromancer)
2. Найди SkillBar компонент
3. Перетащи Rogue_BloodForMana.asset в список скиллов
4. Настрой hotkey (например, клавиша "4")
```

---

## Тестирование

### Быстрый тест

**Шаг 1: Подготовка**
```
1. ▶️ Play Scene
2. Проверь HP и MP в UI
   HP: 180/180
   MP: 120/120
```

**Шаг 2: Трать ману**
```
1. Используй несколько скиллов (Soul Drain, Curse of Weakness, etc)
2. MP: 120 → 30 (мало маны!)
```

**Шаг 3: Используй Blood for Mana**
```
1. Нажми клавишу "4" (Blood for Mana)
2. Должно произойти:
```

**Ожидаемое поведение:**
```
✅ Кровавый взрыв (красный эффект)
✅ Синяя вспышка над некромантом (восстановление MP)
✅ HP: 180 → 144 (-36 HP, 20%)
✅ MP: 30 → 54 (+24 MP, 20% от 120)
```

**Шаг 4: Проверь логи**
```
Console → Открой логи
```

**Ожидаемые логи:**
```
[SkillExecutor] Using skill: Blood for Mana
[SkillExecutor] 🩸 Жертвуем 20% HP (36.0 HP)
[SkillExecutor] 🩸 Жертва: -36.0 HP (осталось: 144/180)
[ManaSystem] +24.0 MP. Текущая: 54.0/120.0
[SkillExecutor] 💙 Восстановлено: +24.0 MP (20% от максимума)
[SkillExecutor] ✅ Blood for Mana: -36 HP → +24 MP
```

### Детальные проверки

**Тест 1: Не работает при низком HP**
```
1. Урони HP до 30/180
2. Попробуй использовать Blood for Mana
3. Ожидаемый результат: ⚠️ Недостаточно HP для жертвы
4. Скилл не используется ✅
```

**Тест 2: Не требует ману**
```
1. MP: 0/120 (мана закончилась!)
2. Используй Blood for Mana
3. Скилл активируется (manaCost = 0) ✅
4. MP: 0 → 24 ✅
```

**Тест 3: Cooldown 15 секунд**
```
1. Используй Blood for Mana
2. Попробуй использовать снова сразу
3. "Skill on cooldown: 15" ✅
4. Подожди 15 секунд
5. Скилл снова доступен ✅
```

**Тест 4: Комбо с Soul Drain**
```
1. HP: 180/180, MP: 10/120
2. Blood for Mana → HP: 144/180, MP: 34/120
3. Soul Drain (25 MP) → HP: 144 → 190+ (life steal), MP: 9/120
4. Восстановил HP обратно через вампиризм! ✅
```

---

## Комбинации со скиллами

### Экстренный цикл ресурсов:
```
1. MP на нуле, HP высокий
2. Blood for Mana (-20% HP, +20% MP)
3. Soul Drain × 2 (вампиризм, восстановление HP)
4. Циклическое использование ресурсов!
```

### Максимум урона без простоя:
```
1. Трать всю ману на Soul Drain
2. Blood for Mana (получаешь ману)
3. Снова трать всю ману
4. Никогда не останавливаешься!
```

### Опасная стратегия (высокий риск):
```
1. Blood for Mana несколько раз (HP очень низкий)
2. Огромный запас маны!
3. Спам Soul Drain (восстанавливаешь HP через life steal)
4. Высокий риск, но высокая награда
```

---

## Сравнение с другими скиллами восстановления

| Скилл | Ресурс | Восстановление | Стоимость | Cooldown |
|-------|--------|----------------|-----------|----------|
| **Blood for Mana** | MP | +20% MP | 20% HP | 15 сек |
| Battle Heal | HP | +20% HP | 50 MP | 15 сек |
| Mana Regen (пассивный) | MP | ~0.1 MP/сек | Бесплатно | Нет |

**Уникальность Blood for Mana:**
- Единственный **активный** скилл восстановления MP
- **Не требует ману** (критично когда MP = 0!)
- **Конверсия ресурсов** (HP ↔ MP)

---

## Технические детали

### Почему нельзя убить себя?

```csharp
if (healthSystem.CurrentHealth <= hpSacrifice)
{
    // Если текущий HP ≤ жертвы → смерть!
    return; // Блокируем использование
}

// Пример:
CurrentHP = 30
HPSacrifice = 36 (20% от 180)
30 ≤ 36 → БЛОКИРОВАНО ✅

CurrentHP = 100
HPSacrifice = 36
100 > 36 → РАЗРЕШЕНО ✅
```

### Процентное vs Фиксированное восстановление:

**Blood for Mana (процентное):**
```
MaxMP = 80  → Restore = 16 MP
MaxMP = 120 → Restore = 24 MP
MaxMP = 200 → Restore = 40 MP

Скейлится с Wisdom!
```

**Фиксированное восстановление:**
```
Всегда +30 MP (не зависит от MaxMP)
```

---

## Lore / Лор

> *"Кровь - это жизнь. Но для некроманта кровь - это также и сила.
> Древнее жертвенное заклинание позволяет обменять собственную кровь
> на магическую энергию. Многие некроманты злоупотребляют этой силой
> и погибают от собственной жадности..."*

**Некромантский компромисс:**
- HP → MP (Blood for Mana)
- MP → HP (Soul Drain)
- Вечный цикл жизни и смерти ☠️

---

## Будущие улучшения

### Возможные вариации:
1. **Greater Blood Sacrifice** - 40% HP → 40% MP (более эффективно, но опаснее)
2. **Blood Pact** - Пассивная аура: автоматически конвертирует HP в MP при достижении 0 MP
3. **Life Tap** - Обратная версия: 20% MP → 20% HP
4. **Blood Ritual** - Channeled spell: постепенно конвертирует HP в MP (5% HP/сек → 5% MP/сек)

---

## Файлы

### Создано:
- [Assets/Scripts/Editor/CreateBloodForMana.cs](Assets/Scripts/Editor/CreateBloodForMana.cs) - Editor script для создания скилла
- Assets/Resources/Skills/Rogue_BloodForMana.asset - ScriptableObject скилла (создаётся через меню)

### Модифицировано:
- [Assets/Scripts/Skills/SkillConfig.cs](Assets/Scripts/Skills/SkillConfig.cs#L424-L427) - Добавлено поле `manaRestorePercent`
- [Assets/Scripts/Skills/SkillExecutor.cs](Assets/Scripts/Skills/SkillExecutor.cs#L538-L545) - Добавлена проверка Blood for Mana в ExecuteBuff
- [Assets/Scripts/Skills/SkillExecutor.cs](Assets/Scripts/Skills/SkillExecutor.cs#L600-L655) - Добавлен метод ExecuteBloodForMana

### Используется существующая система:
- [Assets/Scripts/Player/ManaSystem.cs](Assets/Scripts/Player/ManaSystem.cs#L144-L151) - RestoreMana() уже существует
- [Assets/Scripts/Player/HealthSystem.cs](Assets/Scripts/Player/HealthSystem.cs) - TakeDamage() для жертвы HP

---

## Статус: ✅ ГОТОВО

**Blood for Mana** - четвёртый скилл Rogue (Necromancer) полностью реализован!

Некромант теперь может жертвовать HP для восстановления маны! 🩸💙☠️

**4 из 5 скиллов Некроманта завершены:**
1. ✅ Soul Drain - Вампиризм (100% life steal)
2. ✅ Curse of Weakness - Ослепление (Perception → 1)
3. ✅ Crippling Curse - Замедление (-80% скорости)
4. ✅ **Blood for Mana - Жертвенное заклинание (HP → MP)** 🆕
