# Divine Strength - Готов к использованию! ✅

## Описание

**Divine Strength** (Божественная сила) - четвёртый скилл Паладина.
Увеличивает **физическую атаку на 50%** всем союзникам в радиусе 10 метров на 15 секунд.

---

## Характеристики

- **Skill ID:** 504
- **Class:** Paladin
- **Type:** Buff (AOE)
- **Target:** NoTarget (все союзники в радиусе)
- **Cooldown:** 90 секунд (1.5 минуты)
- **Mana Cost:** 70
- **Cast Time:** 0.2 секунды
- **Can Use While Moving:** ✅ Да
- **Radius:** 10 метров
- **Duration:** 15 секунд
- **Effect:** +50% к физической атаке

---

## Как создать в Unity

1. В меню: **Aetherion → Skills → Paladin → Create Divine Strength**
2. Asset создастся: `Assets/Resources/Skills/Paladin_DivineStrength.asset`

---

## Как работает

### Логика выполнения:

```
Игрок использует Divine Strength (клавиша 4)
    ↓
SkillExecutor.ExecuteBuff() вызван
    ↓
Проверка: aoeRadius > 0? ✅ ДА (10м)
    ↓
ExecuteAOEBuff() вызван
    ↓
OverlapSphere(центр=позиция_кастера, радиус=10м)
    ↓
Найдены союзники:
  - TestPlayer (вы)
  - NetworkPlayer1
  - NetworkPlayer2
    ↓
EffectManager.ApplyEffect(IncreaseAttack, power=50, duration=15)
    ↓
CharacterStats.AddAttackModifier(+50%)
    ↓
Спавнены красные ауры на всех союзниках
    ↓
Через 15 секунд → RemoveEffect() → AttackModifier сброшен
```

### Ключевые особенности:

1. **Бафф атаки**
   - `EffectType.IncreaseAttack`
   - `power = 50` → +50% к физической атаке
   - Работает через `CharacterStats.AttackModifier`

2. **Применение к урону**
   ```csharp
   // В SkillExecutor.CalculateDamage()
   if (stats.AttackModifier > 0)
   {
       float bonus = damage * (stats.AttackModifier / 100f);
       damage += bonus;
       // Было: 100 урона → Стало: 150 урона (+50%)
   }
   ```

3. **AOE Detection**
   - Ищет всех игроков в радиусе 10м
   - Включает кастера (паладин тоже получает бафф)
   - Не баффает врагов

4. **Визуальные эффекты**
   - Cast Effect: Красное свечение на кастере
   - Caster Effect: Золотая + красная аура на кастере
   - Hit Effect: Красная аура на каждом союзнике
   - Duration Effect: Красная аура остаётся 15 секунд

---

## Тестирование

### В Unity (Play Mode):

1. Запустите игру
2. Выберите Paladin
3. Нажмите `4` для использования Divine Strength
4. **Атакуйте врага** - урон должен быть на 50% больше

### Ожидаемые логи:

```
[SkillExecutor] Using skill: Divine Strength
[SkillExecutor] AOE Buff center: (X, Y, Z), radius: 10
[SkillExecutor] Found 1 allies in radius
[SkillExecutor] ✅ Buff IncreaseAttack applied to TestPlayer
[EffectManager] ✅ Применён эффект: IncreaseAttack (15.0с)
[EffectManager] ⚔️ +50% к атаке
[CharacterStats] AttackModifier увеличен: 0 → 50

// При атаке:
[SkillExecutor] Damage calculated: 150 (base: 100)
[SkillExecutor] ⚔️ Attack modifier applied: +50% (+50.0 damage, total: 150.0)
```

### Визуально:

- 🔴 Красная аура появляется на паладине
- 🔴 Красная аура появляется на всех союзниках в радиусе
- ⚔️ Урон союзников увеличивается на 50%
- ⏱️ Через 15 секунд аура исчезает, урон возвращается к норме

---

## Сравнение с Battle Rage (Warrior)

| Параметр | Battle Rage (Warrior) | Divine Strength (Paladin) |
|----------|----------------------|---------------------------|
| Skill ID | 103 | 504 |
| Бонус атаки | +50% | +50% |
| Duration | 10 сек | 15 сек ⬆️ |
| Cooldown | 60 сек | 90 сек ⬇️ |
| Mana Cost | 50 | 70 ⬇️ |
| Target | Self (только себе) | AOE (вся команда) ⬆️⬆️⬆️ |
| Cast Time | 0 сек | 0.2 сек |
| While Moving | Да | Да |

**Вывод:** Divine Strength сильнее для команды (баффает всех), но дольше кулдаун и дороже в мане.

---

## Примеры использования

### 1. Командный бафф перед боссом
```
Paladin: Divine Strength → Вся команда +50% урон
Warrior: Battle Rage → Ещё +50% урон (стакается!)
Итого: Команда наносит x2.25 урона!
```

### 2. Burst damage window
```
Divine Strength (15 сек) → Archer Eagle Eye → Rogue Deadly Precision
→ Вся команда бьёт критами с +50% урона
```

### 3. Защитник + атакующий
```
Divine Protection (неуязвимость 5 сек)
→ Команда атакует без риска
→ Divine Strength (+50% урон 15 сек)
→ Безопасный burst damage
```

---

## Комбинации

**Лучшие комбинации:**

1. **Divine Strength + Battle Rage**
   - Paladin: +50% урон (AOE, 15 сек)
   - Warrior: +50% урон (Self, 10 сек)
   - Стакается? **ДА!** (+100% урон на Warrior)

2. **Divine Strength + Eagle Eye (Archer)**
   - +50% урон
   - +100% шанс крита
   - Гарантированные криты с бонусным уроном

3. **Divine Strength + Divine Protection**
   - Сначала неуязвимость (5 сек)
   - Потом бафф атаки (15 сек)
   - Команда атакует безопасно и сильно

4. **Divine Strength + Lay on Hands**
   - Бафф атаки (15 сек)
   - Вылечить команду (20% HP)
   - Полная поддержка команды

---

## Баланс

**Сильные стороны:**
- ✅ Баффает ВСЮ команду (+50% каждому)
- ✅ Длительность 15 сек (дольше чем Battle Rage)
- ✅ Можно использовать в движении
- ✅ Паладин тоже получает бафф

**Слабые стороны:**
- ❌ Cooldown 90 сек (длиннее чем Battle Rage)
- ❌ Mana Cost 70 (дороже чем Battle Rage)
- ❌ Требует близость союзников (10м)
- ❌ Можно dispel (снять)

**Контрится:**
- Silence - нельзя использовать
- Dispel/Cleanse - снимает бафф
- Разделение команды - не все получат бафф

---

## Сетевая синхронизация

Divine Strength автоматически синхронизируется:

### Клиент → Сервер
```json
{
  "type": "skill_used",
  "skillId": 504,
  "position": {"x": 10, "y": 0, "z": 15}
}
```

### Сервер → Все клиенты
```json
{
  "type": "effect_applied",
  "targetSocketId": "abc123",
  "effectType": 0, // IncreaseAttack
  "power": 50,
  "duration": 15
}
```

Все клиенты применяют:
- Визуальный эффект (красная аура)
- Расчёт урона с бонусом +50%

---

## Технические детали

### EffectManager.cs обработка:

```csharp
case EffectType.IncreaseAttack:
    if (characterStats != null)
    {
        characterStats.AddAttackModifier(config.power); // +50
        Log($"⚔️ +{config.power}% к атаке");
    }
    break;
```

### CharacterStats.cs:

```csharp
private float attackModifier = 0f;

public void AddAttackModifier(float amount)
{
    attackModifier += amount;
    Log($"AttackModifier увеличен: {attackModifier - amount} → {attackModifier}");
}

public void RemoveAttackModifier(float amount)
{
    attackModifier -= amount;
    attackModifier = Mathf.Max(0, attackModifier);
}
```

### SkillExecutor.cs применение:

```csharp
private float CalculateDamage(SkillConfig skill)
{
    float damage = skill.baseDamageOrHeal;
    damage += stats.strength * skill.strengthScaling;

    // ПРИМЕНЯЕМ БАФФ АТАКИ
    if (stats.AttackModifier > 0)
    {
        float bonus = damage * (stats.AttackModifier / 100f);
        damage += bonus;
        Log($"⚔️ Attack modifier applied: +{stats.AttackModifier}% (+{bonus:F1} damage)");
    }

    return damage;
}
```

---

## Файлы

**Созданные:**
- `Assets/Scripts/Editor/CreateDivineStrength.cs` - Editor скрипт
- `Assets/Resources/Skills/Paladin_DivineStrength.asset` - SkillConfig
- `DIVINE_STRENGTH_READY.md` - Эта документация

**Используемые:**
- `Assets/Scripts/Skills/SkillExecutor.cs` - ExecuteAOEBuff() (уже готов)
- `Assets/Scripts/Skills/EffectManager.cs` - IncreaseAttack (уже готов)
- `Assets/Scripts/Stats/CharacterStats.cs` - AttackModifier (уже готов)

---

## Прогресс Paladin скиллов

✅ **4 из 5 скиллов готовы:**
1. ✅ Bear Form - Трансформация в медведя
2. ✅ Divine Protection - Неуязвимость 5 сек (AOE)
3. ✅ Lay on Hands - Хил 20% HP (AOE)
4. ✅ Divine Strength - +50% атака 15 сек (AOE)

🔲 **Осталось:**
5. 🔲 Resurrection - Воскрешение союзника (или другой скилл на выбор)

---

## Troubleshooting

### Проблема: Урон не увеличивается

**Решение:**
1. Проверьте что `EffectManager` применил эффект (лог: `⚔️ +50% к атаке`)
2. Проверьте что `CharacterStats.AttackModifier > 0`
3. Проверьте что `CalculateDamage()` применяет модификатор

### Проблема: Бафф не применяется к союзникам

**Решение:** Убедитесь что `canTargetAllies = true` и `aoeRadius = 10`

### Проблема: Красная аура не появляется

**Решение:** Проверьте что префаб `CFXR3 Hit Light B (Air)` существует в `Resources/Effects/`

---

✅ **Divine Strength готов к тестированию!**

Создайте ScriptableObject в Unity и протестируйте! ⚔️🔴✨
