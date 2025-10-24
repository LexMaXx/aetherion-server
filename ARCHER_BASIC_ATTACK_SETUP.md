# ✅ BasicAttackConfig_Archer - Настройка атаки Лучника

## Создано

**Файл:** `Assets/ScriptableObjects/Skills/BasicAttackConfig_Archer.asset`

### Параметры атаки Лучника:

```yaml
Класс: Archer
Тип атаки: Ranged (дальняя)
Описание: "Быстрая дальняя атака стрелой"

=== УРОН ===
Base Damage: 35
Strength Scaling: 0.5  (небольшой бонус от силы)
Intelligence Scaling: 0  (не использует интеллект)

Формула урона: 35 + (Strength × 0.5)

=== СКОРОСТЬ ===
Attack Cooldown: 0.8 сек  (быстрее чем у Мага - 1.0 сек)
Animation Speed: 2.5x
Attack Hit Timing: 0.3 сек

=== ДАЛЬНОСТЬ ===
Attack Range: 35m  (меньше чем у Мага - 50m, но больше чем ближний бой)

=== СНАРЯД ===
Projectile: ArrowProjectile
Projectile Speed: 30  (быстрее чем Celestial Ball - 20)
Projectile Lifetime: 3 сек
Homing: НЕТ  (стрелы летят прямо, без самонаведения)

=== КРИТИЧЕСКИЙ УРОН ===
Base Crit Chance: 15%  (выше чем у Мага - 5%)
Crit Multiplier: 2.5x  (выше чем у Мага - 2.0x)

=== СПЕЦИАЛЬНЫЕ ЭФФЕКТЫ ===
Piercing Attack: ДА  (стрела пробивает врагов!)
Max Pierce Targets: 2  (может попасть в 2 врагов одной стрелой)

=== РЕСУРСЫ ===
Mana Cost: 0  (не использует ману)
Action Points: 2  (меньше чем у Мага - 3)
```

---

## Сравнение: Маг vs Лучник

| Параметр | Маг | Лучник |
|----------|-----|--------|
| **Урон** | 40 + Int×2 | 35 + Str×0.5 |
| **Скорость атаки** | 1.0 сек | 0.8 сек ⚡ |
| **Дальность** | 50m 🎯 | 35m |
| **Скорость снаряда** | 20 | 30 ⚡ |
| **Самонаведение** | ДА 🎯 | НЕТ |
| **Крит шанс** | 5% | 15% 🎯 |
| **Крит урон** | ×2.0 | ×2.5 🎯 |
| **Пробивание** | НЕТ | ДА (2 цели) 🎯 |
| **Мана** | 10 | 0 ⚡ |

**Итог:**
- **Маг** - медленнее, но больше урон и самонаведение
- **Лучник** - быстрее, выше шанс крита, пробивание врагов

---

## Как применить к персонажу Archer

### Вариант 1: В сцене ArenaScene

1. **Откройте сцену:**
   ```
   Assets/Scenes/ArenaScene.unity
   ```

2. **Найдите персонажа Лучника в Hierarchy**
   - Ищите объект с именем содержащим "Archer" или "Player"

3. **Добавьте/замените компонент PlayerAttackNew:**
   ```
   Inspector → Add Component → PlayerAttackNew
   ИЛИ
   Inspector → PlayerAttack → Replace with PlayerAttackNew
   ```

4. **Назначьте BasicAttackConfig_Archer:**
   ```
   Inspector → PlayerAttackNew
   ├── Attack Config → BasicAttackConfig_Archer
   └── (перетащите из Assets/ScriptableObjects/Skills/)
   ```

5. **Сохраните сцену** (Ctrl+S)

---

### Вариант 2: Создать тестовый объект Archer

Если хотите протестировать прямо сейчас:

1. **В текущей тестовой сцене:**
   - Найдите TestPlayer (который с Магом)
   - Duplicate (Ctrl+D)
   - Переименуйте в "TestArcher"

2. **Измените Attack Config:**
   ```
   Inspector → PlayerAttackNew → Attack Config
   BasicAttackConfig_Mage → BasicAttackConfig_Archer
   ```

3. **Замените визуальную модель (опционально):**
   ```
   Удалите старую модель Мага
   Перетащите ArcherModel из Assets/Resources/Characters/
   ```

4. **Запустите игру и протестируйте!**

---

## Тестирование

### Запустите Unity и протестируйте:

1. **Нажмите Play ▶️**
2. **Нажмите ЛКМ** для атаки

### Ожидаемое поведение:

**Атака Лучника:**
- ⚡ **Быстрее** чем у Мага (кулдаун 0.8 сек vs 1.0 сек)
- 🏹 **Стрела** летит быстрее (30 vs 20)
- 🎯 **Стрела НЕ следует** за целью (летит прямо)
- 💥 **Урон:** 35 + (Strength × 0.5)
- 🎲 **Крит:** 15% шанс нанести ×2.5 урона
- 🔫 **Пробивание:** стрела может попасть в 2 врагов подряд

### Ожидаемые логи в Console:

```
[PlayerAttackNew] ⚔️ Атака!
[PlayerAttackNew] 💥 Урон рассчитан: 35.0  ← (или больше если есть Strength)
[PlayerAttackNew] 🎯 ArrowProjectile создан и инициализирован!
[ArrowProjectile] ✨ Создан! Target: DummyEnemy_2, Damage: 35
[ArrowProjectile] 💥 Попадание в DummyEnemy_2! Урон: 35
[ArrowProjectile] ✅ Урон нанесен DummyEnemy: 35
[DummyEnemy] DummyEnemy_2 получил 35.0 урона. HP: 965/1000
```

### Визуально:

- ✅ **Стрела** создаётся (тонкий цилиндр)
- ✅ Летит **быстро и прямо** (без изгибов)
- ✅ Попадает в DummyEnemy
- ✅ HP bar уменьшается
- ✅ Эффект попадания
- ✅ Стрела исчезает

---

## Особенность: Piercing Attack (Пробивание)

Если выстроить нескольких DummyEnemy в линию:

```
Archer → DummyEnemy_1 → DummyEnemy_2 → DummyEnemy_3
```

Стрела пробьёт **первых двух** врагов:

```
[ArrowProjectile] 💥 Попадание в DummyEnemy_1! Урон: 35
[ArrowProjectile] 🔫 Пробивание! Ищем следующую цель...
[ArrowProjectile] 💥 Попадание в DummyEnemy_2! Урон: 35
[ArrowProjectile] 🗑️ Максимум целей достигнут (2/2)
```

---

## Файлы

### Созданные файлы:
- ✅ `Assets/ScriptableObjects/Skills/BasicAttackConfig_Archer.asset`
- ✅ `Assets/ScriptableObjects/Skills/BasicAttackConfig_Archer.asset.meta`

### Используемые ресурсы:
- 🏹 `Assets/Resources/Projectiles/ArrowProjectile.prefab` (снаряд стрела)
- 👤 `Assets/Resources/Characters/ArcherModel.prefab` (визуальная модель)

---

## Что дальше?

После успешного теста Лучника:

1. **Создать BasicAttackConfig для других классов:**
   - ✅ Mage (готов)
   - ✅ Archer (готов)
   - ⏳ Warrior (ближний бой, без снарядов)
   - ⏳ Paladin (ближний бой, танк)
   - ⏳ Rogue/Necromancer (дальний бой, магия)

2. **Применить на реальные префабы:**
   - Найти все префабы персонажей
   - Добавить/заменить PlayerAttackNew
   - Назначить соответствующие BasicAttackConfig

3. **Протестировать все классы:**
   - Убедиться что урон, скорость, дальность работают правильно
   - Проверить визуальные эффекты
   - Протестировать мультиплеер

---

## Технические детали

### Формула урона Лучника:

```csharp
float damage = baseDamage;  // 35
damage += stats.Strength * strengthScaling;  // + (Strength × 0.5)

// Проверка крита
float critRoll = Random.Range(0f, 100f);
if (critRoll < baseCritChance)  // 15% шанс
{
    damage *= critMultiplier;  // ×2.5
    Debug.Log("[Archer] 🎯 КРИТИЧЕСКИЙ УДАР!");
}

// Итоговый урон: 35-87.5 (без крита) или 87.5-218.75 (с критом)
```

### Piercing Attack (пробивание):

```csharp
if (piercingAttack && targetsHit < maxPierceTargets)
{
    // Стрела НЕ уничтожается
    // Продолжает полёт
    // Может попасть ещё в 1 врага (maxPierceTargets = 2)
}
else
{
    // Уничтожаем стрелу
    DestroySelf();
}
```

---

## Статус

✅ **BasicAttackConfig_Archer создан и настроен**
✅ **Снаряд ArrowProjectile найден и подключен**
📋 **Готов к применению на персонаж Archer**

---

**Попробуйте протестировать!** Создайте тестового Archer или найдите персонажа в ArenaScene! 🏹
