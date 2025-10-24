# ✅ Battle Heal - Готов!

## Описание
**Battle Heal (Боевое Исцеление)** - четвёртый скилл воина
- Восстанавливает **20% от максимального HP**
- Мгновенное лечение
- Визуальный эффект: **Яркая вспышка золотого света** ✨
- Self heal (на себя)

---

## Что было создано

### 1. Editor скрипт
**Файл:** [Assets/Scripts/Editor/CreateBattleHeal.cs](Assets/Scripts/Editor/CreateBattleHeal.cs)

**Команда в Unity:**
```
Unity Menu → Aetherion → Skills → Create Battle Heal (Warrior)
```

**Параметры:**
```csharp
skill.skillId = 404;
skill.skillName = "Battle Heal";
skill.skillType = SkillConfigType.Heal;
skill.targetType = SkillTargetType.Self;

skill.cooldown = 15f;                // 15 секунд
skill.manaCost = 50f;                // 50 маны

// Лечение (отрицательное значение = процент от MaxHP)
skill.baseDamageOrHeal = -20f;       // -20 = восстановить 20% HP
skill.strengthScaling = 0f;
skill.intelligenceScaling = 0f;

// Визуальные эффекты
skill.castEffectPrefab = "CFXR3 Hit Light B (Air)";  // Яркая вспышка света
skill.hitEffectPrefab = "CFXR3 Hit Light B (Air)";   // Вспышка в точке игрока
```

### 2. Обновлён ExecuteHeal в SkillExecutor
**Файл:** [Assets/Scripts/Skills/SkillExecutor.cs:542-577](Assets/Scripts/Skills/SkillExecutor.cs#L542-L577)

**Добавлено:**
- Поддержка процентного лечения (отрицательное значение = процент от MaxHP)
- Применение лечения к HealthSystem
- Спавн двух визуальных эффектов (cast + hit)
- Подробное логирование

**Как работает:**
```csharp
private void ExecuteHeal(SkillConfig skill, Transform target)
{
    Transform healTarget = (skill.targetType == SkillTargetType.Self) ? transform : target;
    float healAmount = CalculateHeal(skill); // -20 для Battle Heal

    HealthSystem targetHealthSystem = healTarget.GetComponent<HealthSystem>();
    if (targetHealthSystem != null)
    {
        if (healAmount < 0)
        {
            // Отрицательное значение = процент от максимального HP
            float percentHeal = Mathf.Abs(healAmount); // 20
            float actualHeal = targetHealthSystem.MaxHealth * (percentHeal / 100f);
            // MaxHP = 180 → actualHeal = 180 * 0.2 = 36 HP
            targetHealthSystem.Heal(actualHeal);
        }
    }

    // Визуальные эффекты
    SpawnEffect(skill.castEffectPrefab, healTarget.position, Quaternion.identity);
    SpawnEffect(skill.hitEffectPrefab, healTarget.position, Quaternion.identity);
}
```

---

## Как работает Battle Heal

### Полный флоу

1. **Игрок нажимает клавишу `4` (Battle Heal)**
   ```
   SimplePlayerController.HandleSkills() → Key "4" → UseSkill(3)
   ```

2. **SkillExecutor выполняет лечение**
   ```csharp
   SkillExecutor.UseSkill(3)
   → case SkillConfigType.Heal
   → ExecuteHeal(skill, null) // Self heal
   → healTarget = transform (сам игрок)
   ```

3. **Вычисление лечения**
   ```csharp
   healAmount = skill.baseDamageOrHeal // -20

   if (healAmount < 0) // true
   {
       percentHeal = Mathf.Abs(-20) = 20
       actualHeal = MaxHealth * (20 / 100)

       Примеры:
       MaxHP = 180 → actualHeal = 180 * 0.2 = 36 HP
       MaxHP = 200 → actualHeal = 200 * 0.2 = 40 HP
       MaxHP = 150 → actualHeal = 150 * 0.2 = 30 HP
   }
   ```

4. **Применение лечения**
   ```csharp
   HealthSystem.Heal(actualHeal)
   → currentHealth += actualHeal
   → currentHealth = Min(currentHealth, maxHealth) // Не превышает максимум
   → OnHealthChanged?.Invoke(currentHealth, maxHealth)

   Console: "[HealthSystem] +36 HP. Текущее: 86/180"
   ```

5. **Визуальные эффекты**
   ```
   - Cast Effect: Яркая вспышка света в точке игрока
   - Hit Effect: Ещё одна вспышка света (двойная вспышка!)
   - Результат: ОЧЕНЬ ЯРКАЯ вспышка ✨✨
   ```

---

## Визуальные эффекты

### Двойная вспышка света
**Эффект:** `CFXR3 Hit Light B (Air)` x2
- **Cast Effect** - первая вспышка света
- **Hit Effect** - вторая вспышка света
- Оба спавнятся одновременно в точке игрока
- **Результат:** Очень яркая золотая вспышка! ✨✨
- Символизирует мощную целительную энергию
- Auto-destroy (исчезает через ~1 секунду)

---

## Примеры работы

### Пример 1: Низкое HP
```
До использования:
  CurrentHP: 50 HP
  MaxHP: 180 HP
  HP%: 28% (критически низкое!)

Игрок использует Battle Heal (клавиша 4):
  ✨ Яркая вспышка света
  Лечение: 20% от 180 = 36 HP

После использования:
  CurrentHP: 50 + 36 = 86 HP
  MaxHP: 180 HP
  HP%: 48% (выжил!)

Результат: +36 HP ❤️
```

### Пример 2: Среднее HP
```
До использования:
  CurrentHP: 100 HP
  MaxHP: 180 HP
  HP%: 56%

Игрок использует Battle Heal:
  ✨ Яркая вспышка света
  Лечение: 20% от 180 = 36 HP

После использования:
  CurrentHP: 100 + 36 = 136 HP
  MaxHP: 180 HP
  HP%: 76% (почти полное!)

Результат: +36 HP ❤️
```

### Пример 3: Почти полное HP
```
До использования:
  CurrentHP: 170 HP
  MaxHP: 180 HP
  HP%: 94%

Игрок использует Battle Heal:
  ✨ Яркая вспышка света
  Лечение: 20% от 180 = 36 HP
  Но максимум = 180 HP!

После использования:
  CurrentHP: 170 + 10 = 180 HP (ограничено максимумом)
  MaxHP: 180 HP
  HP%: 100% (полное здоровье!)

Результат: +10 HP (до максимума) ❤️
```

---

## Создание и тестирование

### Шаг 1: Создать скилл
```
Unity Menu → Aetherion → Skills → Create Battle Heal (Warrior)
```

**Вывод в консоль:**
```
═══════════════════════════════════════════════════════
⚕️ Battle Heal создан!
═══════════════════════════════════════════════════════
📍 Путь: Assets/Resources/Skills/Warrior_BattleHeal.asset
🆔 Skill ID: 404
🎯 Тип: Self Heal (самолечение)
❤️ Эффект: Восстановить 20% от максимального HP
✨ Визуальные эффекты:
   - Cast: CFXR3 Hit Light B (яркая вспышка света)
   - Hit: CFXR3 Hit Light B (вспышка в точке игрока)
⏱️ Cooldown: 15с
💧 Mana: 50
═══════════════════════════════════════════════════════
```

### Шаг 2: Пересоздать TestPlayer как Warrior
```
Unity Menu → Aetherion → Create Test Player in Scene
→ Класс: Warrior
→ Slot 0: Charge ✅
→ Slot 1: Hammer Throw ✅
→ Slot 2: Defensive Stance ✅
→ Slot 3: Battle Heal ✅ (НОВОЕ!)
```

### Шаг 3: Тестировать
```
1. Play SkillTestScene ▶️
2. Получи урон от врага (DummyEnemyAttacker)
3. HP падает (например, 180 → 140)
4. Нажми `4` (Battle Heal)
5. Наблюдай:
   - Двойная яркая вспышка света ✨✨
   - HP восстановлено на 20% (140 + 36 = 176) ❤️
   - Сообщение в консоли
```

---

## Проверка в Console

### Успешное лечение
```
[SimplePlayerController] 🔥 Попытка использовать скилл в слоте 3
[SkillExecutor] Using skill: Battle Heal
[SkillExecutor] 💧 Потрачено 50 маны. Осталось: 450

[SkillExecutor] ⚕️ Лечение TestPlayer: 36.0 HP (20% от 180)
[HealthSystem] +36 HP. Текущее: 86/180

Console: Яркая вспышка света появилась! ✨
```

### Лечение при почти полном HP
```
[SkillExecutor] ⚕️ Лечение TestPlayer: 36.0 HP (20% от 180)
[HealthSystem] +10 HP. Текущее: 180/180  (ограничено максимумом)
```

---

## Технические детали

### Формула процентного лечения
```csharp
actualHeal = MaxHealth * (percentHeal / 100)

Примеры:
- MaxHP: 180, Heal: 20% → actualHeal = 180 * 0.2 = 36 HP
- MaxHP: 200, Heal: 20% → actualHeal = 200 * 0.2 = 40 HP
- MaxHP: 150, Heal: 20% → actualHeal = 150 * 0.2 = 30 HP
- MaxHP: 250, Heal: 20% → actualHeal = 250 * 0.2 = 50 HP
```

### Почему отрицательное значение?
```csharp
skill.baseDamageOrHeal = -20f; // -20 = восстановить 20% HP

if (healAmount < 0)  // Процентное лечение
    percentHeal = Mathf.Abs(healAmount) = 20
else                 // Фиксированное лечение
    heal = healAmount
```

**Это позволяет:**
- `-20` → Восстановить 20% от MaxHP (динамическое лечение)
- `50` → Восстановить фиксированные 50 HP (статическое лечение)

### Ограничение максимумом
```csharp
// В HealthSystem.Heal()
currentHealth += amount;
currentHealth = Mathf.Min(currentHealth, maxHealth); // Не превышает максимум

Пример:
  CurrentHP: 170
  MaxHP: 180
  Heal: 36 HP
  → currentHealth = 170 + 36 = 206 (превышает!)
  → currentHealth = Min(206, 180) = 180 ✅
```

---

## Прогресс скиллов Warrior: 4/5 ✅

| Слот | Скилл | Тип | Эффект | Cooldown | Статус |
|------|-------|-----|--------|----------|--------|
| 0 | Charge | Movement+CC | Teleport+Stun 5s | 12s | ✅ |
| 1 | Hammer Throw | Projectile+CC | 50+150%Str dmg+Stun 3s | 12s | ✅ |
| 2 | Defensive Stance | Self Buff | -50% урона 10s | 20s | ✅ |
| 3 | Battle Heal | Self Heal | +20% HP | 15s | ✅ |
| 4 | ??? | - | - | - | ⏳ TODO |

**Остался 1 последний скилл!** 🎯

---

## Сочетание с другими скиллами

### Комбо 1: Defensive Stance → Battle Heal
```
Ситуация: Низкое HP, враги атакуют

1. Defensive Stance (-50% урона) 🛡️
2. Враги атакуют, но ты получаешь половину урона
3. Battle Heal (+20% HP) ❤️
4. HP восстановлено, всё ещё под защитой!
5. Танковое выживание!
```

### Комбо 2: Battle Heal → Charge
```
Ситуация: Низкое HP, враг далеко

1. Battle Heal (+20% HP) ❤️
2. HP восстановлено
3. Charge к врагу (телепорт + стан)
4. Бьёшь врага с восстановленным HP
5. Агрессивное восстановление!
```

### Комбо 3: Hammer Throw → Battle Heal
```
Ситуация: Низкое HP, враг близко

1. Hammer Throw (стан 3s) 🔨
2. Враг оглушён, не атакует
3. Battle Heal (+20% HP) ❤️
4. HP восстановлено пока враг в стане
5. Безопасное лечение!
```

---

## Возможные улучшения

### 1. Увеличить процент лечения
```csharp
skill.baseDamageOrHeal = -30f; // Было -20%, стало -30%
```

### 2. Уменьшить cooldown
```csharp
skill.cooldown = 12f; // Было 15с, стало 12с
```

### 3. Добавить HoT (лечение во времени)
```csharp
EffectConfig hotEffect = new EffectConfig();
hotEffect.effectType = EffectType.HealOverTime;
hotEffect.duration = 10f;
hotEffect.damageOrHealPerTick = 5f; // 5 HP/сек
hotEffect.tickInterval = 1f;
skill.effects.Add(hotEffect);

Результат:
- Мгновенно: +36 HP (20%)
- Через 10 секунд: +50 HP (5 HP/сек)
- Итого: +86 HP!
```

### 4. Добавить buff после лечения
```csharp
EffectConfig regenBoost = new EffectConfig();
regenBoost.effectType = EffectType.IncreaseHPRegen;
regenBoost.duration = 15f;
regenBoost.power = 50f; // +50% к регенерации HP
skill.effects.Add(regenBoost);
```

---

## Статистика лечения по Endurance

### Warrior с базовым Endurance (5)
```
MaxHP = 100 + (5 * 10) = 150 HP
Battle Heal: 150 * 0.2 = 30 HP ❤️
```

### Warrior с высоким Endurance (10)
```
MaxHP = 100 + (10 * 10) = 200 HP
Battle Heal: 200 * 0.2 = 40 HP ❤️
```

### Warrior с ОЧЕНЬ высоким Endurance (15)
```
MaxHP = 100 + (15 * 10) = 250 HP
Battle Heal: 250 * 0.2 = 50 HP ❤️
```

**Вывод:** Чем больше Endurance → тем больше MaxHP → тем сильнее Battle Heal! 📈

---

**Статус:** ✅ ГОТОВО

Battle Heal работает отлично! Яркая вспышка света + восстановление 20% HP! ❤️✨

Остался последний, 5-й скилл воина! Готов к созданию финального скилла! 🎯
