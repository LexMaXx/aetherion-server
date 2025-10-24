# Holy Hammer - Готов к использованию! ✅

## Описание

**Holy Hammer** (Святой молот) - пятый скилл Паладина.
Призывает святой молот, который **оглушает всех врагов** в радиусе 10 метров на 5 секунд и наносит святой урон.

---

## Характеристики

- **Skill ID:** 505
- **Class:** Paladin
- **Type:** AOE Damage + Crowd Control
- **Target:** NoTarget (все враги в радиусе)
- **Cooldown:** 30 секунд
- **Mana Cost:** 70
- **Cast Time:** 0.5 секунды
- **Can Use While Moving:** ❌ Нет (нужно стоять во время каста)
- **Radius:** 10 метров
- **Damage:** 50 + 1.0x Strength
- **Stun Duration:** 5 секунд
- **Effect:** Stun (блокирует движение, атаки, скиллы)

---

## Как создать в Unity

1. В меню: **Aetherion → Skills → Paladin → Create Holy Hammer**
2. Asset создастся: `Assets/Resources/Skills/Paladin_HolyHammer.asset`

---

## Как работает

### Логика выполнения:

```
Игрок использует Holy Hammer (клавиша 5)
    ↓
SkillExecutor.ExecuteAOEDamage() вызван
    ↓
AOE центр = позиция паладина
    ↓
OverlapSphere(центр, радиус=10м)
    ↓
Найдены враги:
  - DummyEnemy1
  - DummyEnemy2
  - NetworkEnemy1
    ↓
Каждому врагу:
  1. TakeDamage(50 + strength)
  2. ApplyEffect(Stun, 5 секунд)
  3. Spawn электрические искры над головой
    ↓
Враги оглушены на 5 секунд (не могут двигаться/атаковать/использовать скиллы)
```

### Ключевые особенности:

1. **AOE Stun**
   - `EffectType.Stun`
   - `duration = 5` секунд
   - Блокирует: движение, атаки, скиллы
   - Работает через `EffectManager.ApplyEffect()`

2. **Визуальный эффект стана**
   ```csharp
   stunEffect.particleEffectPrefab = "CFXR3 Hit Electric C (Air)";
   // Электрические искры ОСТАЮТСЯ над головой врага 5 секунд
   ```

3. **AOE Detection (враги)**
   - Ищет всех врагов в радиусе 10м
   - Компоненты: `Enemy`, `DummyEnemy`, `NetworkPlayer`
   - НЕ затрагивает союзников

4. **Урон + CC комбо**
   - Наносит 50 + 1.0x Strength урона
   - Сразу же применяет стан на 5 секунд
   - Двойной эффект: урон + контроль

---

## Тестирование

### В Unity (Play Mode):

1. Запустите игру
2. Выберите Paladin
3. Подойдите к группе врагов
4. Нажмите `5` для использования Holy Hammer
5. **Все враги в 10м оглушены на 5 секунд**

### Ожидаемые логи:

```
[SkillExecutor] Using skill: Holy Hammer
[SkillExecutor] AOE center: (X, Y, Z), radius: 10
[SkillExecutor] Found targets: 3
[DummyEnemy] TakeDamage: 150 (50 base + 100 strength)
[EffectManager] ✅ Применён эффект: Stun (5.0с)
[EffectManager] 😵 Оглушение!

// Во время стана:
[Enemy] ❌ Оглушён! Не может двигаться
[Enemy] ❌ Оглушён! Не может атаковать
[Enemy] ❌ Оглушён! Не может использовать скиллы
```

### Визуально:

- ⚡ Электрические искры появляются над головой КАЖДОГО врага
- ⚡ Искры ОСТАЮТСЯ 5 секунд (duration effect)
- 😵 Враги не двигаются, не атакуют, не кастуют
- ⏱️ Через 5 секунд искры исчезают, враги снова активны

---

## Отличия от других AOE стан-скиллов

| Параметр | Stunning Shot (Archer) | Warrior Charge | Holy Hammer (Paladin) |
|----------|------------------------|----------------|----------------------|
| Skill ID | 302 | 105 | 505 |
| Target | Single enemy | Single enemy + knockback | AOE (все враги 10м) ⬆️⬆️⬆️ |
| Stun Duration | 5 сек | 5 сек | 5 сек |
| Damage | 30 + 1.0x Int | 60 + 1.5x Str | 50 + 1.0x Str |
| Cooldown | 15 сек | 20 сек | 30 сек ⬇️ |
| Mana Cost | 40 | 50 | 70 ⬇️ |
| Cast Time | 0 сек | 0 сек | 0.5 сек ⬇️ |
| While Moving | Да | Да | Нет ⬇️ |

**Вывод:** Holy Hammer — это **мощнейший AOE стан** в игре. Он затрагивает ВСЮ группу врагов одновременно, но имеет долгий кулдаун, высокую стоимость маны и каст-тайм.

---

## Примеры использования

### 1. Контроль толпы врагов
```
Группа из 5 врагов атакует команду
→ Paladin: Holy Hammer
→ Все 5 врагов оглушены на 5 секунд
→ Команда свободно атакует беззащитных врагов
```

### 2. Спасение команды
```
Команда окружена врагами
→ Paladin: Holy Hammer (стан всех)
→ Команда отступает или контратакует
→ 5 секунд свободного времени для тактики
```

### 3. Burst damage window
```
Holy Hammer (все враги оглушены 5 сек)
→ Divine Strength (+50% урон команде)
→ Команда наносит критический урон беззащитным врагам
→ Враги уничтожены до окончания стана
```

### 4. Interrupt enemy abilities
```
Boss кастует мощный AOE-spell
→ Paladin: Holy Hammer
→ Boss оглушён → Spell прерван
→ Команда спасена
```

---

## Комбинации

**Лучшие комбинации:**

1. **Holy Hammer + Divine Strength**
   - Стан всех врагов (5 сек)
   - +50% урон всей команде (15 сек)
   - Враги беззащитны, команда наносит огромный урон

2. **Holy Hammer + Eagle Eye (Archer)**
   - Стан всех врагов
   - +100% шанс крита (Archer)
   - Guaranteed crits по оглушённым врагам

3. **Holy Hammer + Meteor (Mage)**
   - Стан всех врагов в куче
   - Mage кастует Meteor на них
   - Враги не могут разбежаться → максимальный урон

4. **Holy Hammer + Battle Rage (Warrior)**
   - Стан врагов
   - Warrior +50% урон
   - Warrior свободно атакует оглушённых врагов

5. **Bear Form + Holy Hammer**
   - Paladin трансформируется в медведя (танк)
   - Втягивает врагов в группу
   - Holy Hammer → все враги оглушены
   - Команда добивает

---

## Баланс

**Сильные стороны:**
- ✅ AOE стан (единственный в игре!)
- ✅ 5 секунд стана (долго)
- ✅ 10м радиус (огромный)
- ✅ Наносит урон + стан одновременно
- ✅ Блокирует ВСЁ: движение, атаки, скиллы

**Слабые стороны:**
- ❌ Cooldown 30 сек (долго)
- ❌ Mana Cost 70 (дорого)
- ❌ Cast Time 0.5 сек (можно прервать)
- ❌ Нельзя двигаться во время каста
- ❌ Можно dispel (снять стан)

**Контрится:**
- Interrupt - прервать каст (0.5 сек окно)
- Silence - не даёт использовать
- Dispel/Cleanse - снимает стан с врагов
- Stun resist - если враг имеет иммунитет к стану

---

## Сетевая синхронизация

Holy Hammer автоматически синхронизируется:

### Клиент → Сервер
```json
{
  "type": "skill_used",
  "skillId": 505,
  "position": {"x": 10, "y": 0, "z": 15}
}
```

### Сервер → Все клиенты
```json
{
  "type": "aoe_damage",
  "skillId": 505,
  "position": {"x": 10, "y": 0, "z": 15},
  "targets": [
    {"socketId": "enemy1", "damage": 150, "effectType": 2} // Stun
  ]
}
```

Все клиенты применяют:
- Урон к врагам
- Стан-эффект (5 сек)
- Визуальный эффект (электрические искры)

---

## Технические детали

### EffectManager.cs обработка стана:

```csharp
case EffectType.Stun:
    Log($"😵 Оглушение!");
    // Блокирует движение через SimplePlayerController
    // Блокирует атаки через SkillExecutor
    // Блокирует скиллы через SkillExecutor
    break;
```

### SkillExecutor.cs проверка стана:

```csharp
public bool CanUseSkill(SkillConfig skill)
{
    EffectManager effectManager = GetComponent<EffectManager>();
    if (effectManager != null)
    {
        if (effectManager.HasStun())
        {
            Log("❌ Оглушён! Не могу использовать скиллы");
            return false;
        }
    }
    return true;
}
```

### ExecuteAOEDamage() применение эффектов:

```csharp
// Line 310-320 в SkillExecutor.cs
if (skill.effects != null && skill.effects.Count > 0)
{
    EffectManager targetEffectManager = hitTarget.GetComponent<EffectManager>();
    if (targetEffectManager != null)
    {
        foreach (EffectConfig effect in skill.effects)
        {
            targetEffectManager.ApplyEffect(effect, stats);
            // Stun effect применён → враг оглушён на 5 сек
        }
    }
}
```

---

## Файлы

**Созданные:**
- `Assets/Scripts/Editor/CreateHolyHammer.cs` - Editor скрипт
- `Assets/Resources/Skills/Paladin_HolyHammer.asset` - SkillConfig
- `HOLY_HAMMER_READY.md` - Эта документация

**Используемые:**
- `Assets/Scripts/Skills/SkillExecutor.cs` - ExecuteAOEDamage() (уже готов)
- `Assets/Scripts/Skills/EffectManager.cs` - ApplyEffect() для Stun (уже готов)
- `Assets/Scripts/Skills/EffectConfig.cs` - Stun blocking logic (уже готов)

---

## Прогресс Paladin скиллов

✅ **5 скиллов Paladin готовы:**
1. ✅ Bear Form - Трансформация в медведя
2. ✅ Divine Protection - Неуязвимость 5 сек (AOE для союзников)
3. ✅ Lay on Hands - Хил 20% HP (AOE для союзников)
4. ✅ Divine Strength - +50% атака 15 сек (AOE для союзников)
5. ✅ Holy Hammer - AOE стан 5 сек (для врагов)

🎉 **Paladin (Druid) ПОЛНОСТЬЮ ГОТОВ!**

---

## Troubleshooting

### Проблема: Стан не применяется к врагам

**Решение:**
1. Проверьте что у врагов есть `EffectManager` компонент
2. Проверьте что `EffectType.Stun` правильно установлен
3. Проверьте логи: должен быть "😵 Оглушение!"

### Проблема: Электрические искры не появляются

**Решение:** Проверьте что префаб `CFXR3 Hit Electric C (Air)` существует в `Resources/Effects/`

### Проблема: Враги могут атаковать во время стана

**Решение:** Убедитесь что `EffectManager.HasStun()` проверяется в AI врагов перед атакой

### Проблема: Стан затрагивает союзников

**Решение:** Проверьте что `canTargetEnemies = true` и `canTargetAllies = false`

---

## Сравнение с другими Paladin скиллами

| Скилл | Target | Эффект | Cooldown | Mana |
|-------|--------|--------|----------|------|
| Bear Form | Self | Трансформация | 60с | 50 |
| Divine Protection | Allies AOE | Неуязвимость 5с | 120с | 80 |
| Lay on Hands | Allies AOE | Хил 20% HP | 60с | 60 |
| Divine Strength | Allies AOE | +50% атака 15с | 90с | 70 |
| **Holy Hammer** | **Enemies AOE** | **Стан 5с + урон** | **30с** | **70** |

**Уникальность Holy Hammer:**
- Единственный AOE стан в игре
- Единственный скилл Paladin, который бьёт врагов AOE
- Балансирует offensive и support роль Paladin

---

## Итоговый анализ Paladin класса

### Роль: **Hybrid Support/Tank/Offensive**

**Support (3 скилла):**
1. Divine Protection - спасает команду от урона
2. Lay on Hands - лечит команду
3. Divine Strength - усиливает урон команды

**Tank (1 скилл):**
1. Bear Form - увеличивает HP, превращает в танка

**Offensive (1 скилл):**
1. Holy Hammer - AOE урон + стан врагов

### Синергия скиллов:

```
Bear Form (танк)
→ Holy Hammer (стан врагов)
→ Divine Strength (бафф команды)
→ Divine Protection (защита команды)
→ Lay on Hands (хил команды)
```

Paladin может:
- Втянуть врагов (Bear Form)
- Оглушить их (Holy Hammer)
- Усилить команду (Divine Strength)
- Защитить команду (Divine Protection)
- Вылечить команду (Lay on Hands)

**ПОЛНЫЙ ЦИКЛ ПОДДЕРЖКИ + КОНТРОЛЯ!**

---

✅ **Holy Hammer готов к тестированию!**

Создайте ScriptableObject в Unity и протестируйте! ⚡😵🔨
