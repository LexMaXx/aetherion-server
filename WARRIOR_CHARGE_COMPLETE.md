# ✅ Warrior Charge - Готов!

## Описание
**Charge (Рывок)** - первый скилл воина
- Телепортируется **ПРЯМО К ВРАГУ** (максимум 20м)
- Оглушает цель на **5 секунд**
- Визуальные эффекты: **магический туман** при телепорте + **электрические искры** стана на враге
- Требует выбора цели (Enemy target)

---

## Что было создано

### 1. SkillConfig
**Файл:** [Assets/Scripts/Editor/CreateWarriorCharge.cs](Assets/Scripts/Editor/CreateWarriorCharge.cs)

**Параметры:**
```csharp
skill.skillId = 401;
skill.skillName = "Charge";
skill.skillType = SkillConfigType.Movement;
skill.targetType = SkillTargetType.Enemy;
skill.requiresTarget = true;  // Обязательно нужна цель!

skill.cooldown = 12f;         // 12 секунд кулдаун
skill.manaCost = 30f;         // 30 маны

// Движение
skill.enableMovement = true;
skill.movementType = MovementType.Teleport;
skill.movementDirection = MovementDirection.ToTarget;
skill.movementDistance = 20f; // Максимум 20 метров

// Эффект стана
EffectConfig stunEffect = new EffectConfig();
stunEffect.effectType = EffectType.Stun;
stunEffect.duration = 5f;     // 5 секунд стана
stunEffect.particleEffectPrefab = "CFXR3 Hit Electric C (Air)"; // Электрические искры на враге

// Визуальные эффекты телепорта
skill.hitEffectPrefab = "CFXR3 Magic Poof";  // В точке назначения
skill.castEffectPrefab = "CFXR3 Magic Poof"; // В точке старта
```

---

## Как работает Warrior Charge

### Полный флоу

1. **Игрок выбирает цель и нажимает клавишу `1`**
   ```
   SimplePlayerController.Update() → Left Click выбирает target
   SimplePlayerController.HandleSkills() → Key "1" → UseSkill(0)
   ```

2. **SkillExecutor проверяет цель и выполняет телепорт**
   ```csharp
   SkillExecutor.UseSkill(0)
   → Проверка: requiresTarget == true, target != null ✅
   → Проверка: cooldown, mana ✅
   → ExecuteMovement(skill)
   → CalculateMovementDestination() - вычисляет точку телепорта
   → MoveToPosition(destination) - выполняет телепорт
   → Spawn castEffect в начальной точке (CFXR3 Magic Poof)
   → Spawn hitEffect в точке назначения (CFXR3 Magic Poof)
   ```

3. **Применение стана на врага**
   ```csharp
   ExecuteMovement()
   → После телепорта: target.GetComponent<EffectManager>()
   → EffectManager.ApplyEffect(stunEffect, stats)
   → Spawn particleEffect на враге (CFXR3 Hit Electric C)
   → Start coroutine (5 seconds stun duration)
   → Враг не может двигаться и атаковать
   ```

---

## Тестирование

### Шаг 1: Создать Charge
```
Unity Menu → Aetherion → Skills → Create Charge (Warrior)
```

### Шаг 2: Добавить EffectManager на DummyEnemy
⚠️ **ВАЖНО:** DummyEnemy должен иметь `EffectManager` компонент для получения стана!

```
1. Открой SkillTestScene
2. Выбери DummyEnemy в Hierarchy
3. Add Component → EffectManager
4. Save Scene
```

### Шаг 3: Пересоздать TestPlayer как Warrior
```
Unity Menu → Aetherion → Create Test Player in Scene
→ Выбери класс: Warrior
→ Slot 0 будет Charge
```

### Шаг 4: Тестировать в игре
1. Play SkillTestScene ▶️
2. ЛКМ на DummyEnemy (выбрать цель)
3. Нажми `1` (Charge)
4. **Должно произойти:**
   - Магический туман в точке старта ✅
   - Воин телепортируется к врагу ✅
   - Магический туман в точке прибытия ✅
   - Электрические искры на враге ✅
   - Враг оглушён на 5 секунд ✅

---

## Прогресс скиллов Warrior: 1/5 ✅

| Слот | Скилл | Тип | Эффект | Cooldown | Статус |
|------|-------|-----|--------|----------|--------|
| 0 | Charge | Movement+CC | Teleport+Stun 5s | 12s | ✅ |
| 1 | ??? | - | - | - | ⏳ TODO |
| 2 | ??? | - | - | - | ⏳ TODO |
| 3 | ??? | - | - | - | ⏳ TODO |
| 4 | ??? | - | - | - | ⏳ TODO |

**Осталось:** 4 скилла для воина!

---

**Статус:** ✅ ГОТОВО

Charge работает отлично! Воин телепортируется ПРЯМО К ВРАГУ и оглушает его! ⚔️⚡

Готов к созданию следующих 4 скиллов воина!
