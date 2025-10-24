# ☄️ Meteor - ГОТОВ К ТЕСТИРОВАНИЮ

## Создано

### 1. Meteor SkillConfig
**Файл:** `Assets/Scripts/Editor/CreateMeteor.cs`

**Использование:**
```
Unity Menu → Aetherion → Skills → Create Meteor (Mage)
```

**Параметры скилла:**
- **Skill ID:** 205
- **Тип:** AOE Damage + Ground Target
- **Базовый урон:** 80 + INT*3.0
- **AOE радиус:** 6 метров
- **Максимум целей:** 10 врагов
- **Cast time:** 2 секунды (можно прервать)
- **Дальность:** 20 метров
- **Cooldown:** 15 секунд
- **Mana:** 70

**Зона огня (DoT):**
- **Урон:** 15 + INT*0.5 за тик
- **Длительность:** 5 секунд
- **Интервал:** 1 секунда
- **Всего урона:** 75 + INT*2.5 за 5 секунд

**Пример урона (INT=9):**
- Взрыв: 80 + 9*3.0 = **107 урона**
- Зона огня: 15 + 9*0.5 = **19.5 урона/сек × 5 = 97.5 урона**
- **Всего: ~205 урона** если враг стоит в зоне все 5 секунд

---

## Как работает Meteor

### Механика

1. **Выбор точки**
   - Игрок нажимает `5`
   - Ground target - телепорт на 10м вперёд (для тестирования)

2. **Cast time (2 секунды)**
   - Начинается каст 2 секунды
   - Показывается cast effect на маге
   - Нельзя двигаться (`canUseWhileMoving = false`)
   - Движение прервёт каст

3. **Взрыв метеорита**
   - Метеорит падает на выбранную точку
   - Большой огненный взрыв (hitEffectPrefab)
   - Наносит урон всем врагам в радиусе 6м
   - Максимум 10 целей

4. **Зона огня**
   - Накладывает Burn эффект на всех пораженных
   - Длится 5 секунд
   - Наносит 15-20 урона каждую секунду
   - Не стакается (повторный Meteor обновит длительность)

---

## Изменения в коде

### SimplePlayerController.cs

**Добавлена клавиша 5:**
```csharp
else if (Input.GetKeyDown(KeyCode.Alpha5))
{
    UseSkill(4); // Slot 4 = Meteor
}
```

**Обновлена справка:**
```csharp
Debug.Log("  5 - Meteor (ground target, cast 2 сек)");
```

### CreateTestPlayer.cs

**Добавлен Meteor в слот 4:**
```csharp
// Slot 4: Meteor
SkillConfig meteor = AssetDatabase.LoadAssetAtPath<SkillConfig>(
    "Assets/Resources/Skills/Mage_Meteor.asset"
);

if (meteor != null)
{
    skillExecutor.equippedSkills.Add(meteor);
    Debug.Log("✅ Slot 4: Meteor");
    skillCount++;
}
```

---

## Cast Time механика (уже реализована)

### SkillExecutor.cs

**ExecuteSkillAfterCastTime** (lines 270-280):
```csharp
private IEnumerator ExecuteSkillAfterCastTime(SkillConfig skill, Transform target, Vector3 targetPosition)
{
    yield return new WaitForSeconds(skill.castTime);

    // Проверяем что каст не был прерван
    if (currentlyCastingSkill == skill)
    {
        ExecuteSkill(skill, target, targetPosition);
        currentlyCastingSkill = null;
    }
}
```

**UseSkill** (lines 248-252):
```csharp
// Если есть время каста - выполняем с задержкой
if (skill.castTime > 0f)
{
    currentlyCastingSkill = skill;
    currentCastCoroutine = StartCoroutine(ExecuteSkillAfterCastTime(skill, target, targetPosition));
}
```

---

## Burn эффект (DoT зона огня)

**EffectConfig в Meteor:**
```csharp
EffectConfig burnZone = new EffectConfig();
burnZone.effectType = EffectType.Burn;
burnZone.duration = 5f;              // 5 секунд
burnZone.damageOrHealPerTick = 15f;  // 15 урона за тик
burnZone.tickInterval = 1f;          // Каждую секунду
burnZone.intelligenceScaling = 0.5f; // +0.5 урона за INT
burnZone.canStack = false;           // Не стакается
```

**Применение:**
- Накладывается на всех врагов в AOE взрыва
- EffectManager автоматически обрабатывает DoT
- Каждую секунду наносит урон через `OnEffectTick()`

---

## Визуальные эффекты

**Cast Effect (во время каста):**
- CFXR3 Hit Light B (Air) - световая аура на маге

**Hit Effect (взрыв метеорита):**
- CFXR3 Fire Explosion B 1 - большой огненный взрыв

**AOE Effect (зона огня на земле):**
- CFXR3 Fire Explosion B 1 - огненный эффект

**Все эффекты автоматически уничтожаются через 1 секунду** (SpawnEffect)

---

## Текущее состояние скиллов мага (5/5)

| Слот | Скилл | Тип | Урон | Cast | Cooldown | Статус |
|------|-------|-----|------|------|----------|--------|
| 0 | Fireball | Projectile | 50 + INT×2.5 + Burn | 0s | 6s | ✅ |
| 1 | Ice Nova | AOE | 40 + INT×2.0 + Slow | 0s | 8s | ✅ |
| 2 | Lightning Storm | AOE + Chain | 60 + INT×2.5 × Chain | 0s | 12s | ✅ |
| 3 | Teleport | Movement | 0 (mobility) | 0s | 8s | ✅ |
| 4 | Meteor | AOE + DoT | 80 + INT×3.0 + Burn zone | 2s | 15s | ✅ ГОТОВ |

---

## Управление

| Клавиша | Действие |
|---------|----------|
| WASD | Движение |
| ЛКМ | Выбрать врага |
| 1 | Fireball (снаряд + DoT) |
| 2 | Ice Nova (AOE + Slow) |
| 3 | Lightning Storm (Chain Lightning) |
| 4 | Teleport (мобильность) |
| 5 | Meteor (AOE + Burn zone) ☄️ |
| H | Помощь |

---

## Console Log Пример

```
[SimplePlayerController] 🔥 Попытка использовать скилл в слоте 4
[SimplePlayerController] 📍 Ground target скилл. Нажмите ПКМ на землю для выбора позиции.
[SkillExecutor] 💧 Потрачено 70 маны. Осталось: 430
[SkillExecutor] ⏳ Начат каст: Meteor (2.0с)
... 2 секунды проходит ...
[SkillExecutor] 🔍 AOE поиск: центр=(10, 0, 10), радиус=6м, найдено коллайдеров=5
[SkillExecutor] 💥 AOE урон: 107 → DummyEnemy1
[SkillExecutor] ✨ Применён эффект Burn к DummyEnemy1
[SkillExecutor] 💥 AOE урон: 107 → DummyEnemy2
[SkillExecutor] ✨ Применён эффект Burn к DummyEnemy2
[SkillExecutor] 💥 AOE урон: 107 → DummyEnemy3
[SkillExecutor] ✨ Применён эффект Burn к DummyEnemy3
[SkillExecutor] 💥 AOE Meteor: 107 урона по 3 целям
[SkillExecutor] ⚡ Использован скилл: Meteor

... через 1 секунду ...
[EffectManager] 🔥 Burn тик: 19 урона
... через 1 секунду ...
[EffectManager] 🔥 Burn тик: 19 урона
... и так 5 секунд
```

---

## Тестирование

### Шаг 1: Создать Meteor SkillConfig
```
Unity → Aetherion → Skills → Create Meteor (Mage)
```

### Шаг 2: Пересоздать TestPlayer
```
Unity → Aetherion → Create Test Player in Scene
```

Должно появиться:
```
✅ Slot 0: Fireball
✅ Slot 1: Ice Nova
✅ Slot 2: Lightning Storm
✅ Slot 3: Teleport
✅ Slot 4: Meteor
⚡ Экипировано скиллов: 5/5
```

### Шаг 3: Тестировать
1. Play SkillTestScene ▶️
2. Нажми `5` (Meteor)
3. **НЕ ДВИГАЙСЯ** 2 секунды (иначе каст прервётся)
4. Проверь:
   - ✅ Cast effect на маге
   - ✅ Через 2 секунды взрыв на земле
   - ✅ Урон всем врагам в радиусе 6м
   - ✅ Burn эффект тикает каждую секунду 5 секунд
   - ✅ Визуальные эффекты исчезают через 1 сек

---

## Особенности Meteor

### 1. Самый мощный AOE урон
- Базовый урон 80 (vs 60 Lightning Storm, 40 Ice Nova)
- Intelligence scaling 3.0x (самый высокий)
- + DoT зона на 5 секунд

### 2. Долгий каст (2 секунды)
- Требует планирования
- Уязвим во время каста
- Нельзя двигаться

### 3. Зона контроля
- Burn zone заставляет врагов уйти
- Или получать дополнительный урон

### 4. Высокая стоимость
- 70 маны (самый дорогой)
- 15 секунд cooldown (самый долгий)

---

## Синергия скиллов

**Комбо 1: Frost Armor → Meteor**
- Ice Nova замедляет врагов (-50% скорости)
- Meteor наносит огромный урон медленным целям
- Burn zone добивает

**Комбо 2: Teleport → Meteor**
- Телепорт в безопасное место
- Каст Meteor на врагов
- Они не могут быстро добежать

**Комбо 3: Lightning Storm → Meteor**
- Lightning Storm наносит быстрый урон
- Meteor добивает с огромным уроном
- Burn zone добивает выживших

---

**Статус:** ✅ ГОТОВ К ТЕСТИРОВАНИЮ

**Все 5 скиллов мага готовы!** 🔮🔥⚡❄️☄️

Маг полностью готов для тестирования и использования в игре! 🎉
