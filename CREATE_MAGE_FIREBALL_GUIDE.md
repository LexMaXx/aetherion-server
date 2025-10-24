# ИНСТРУКЦИЯ: Создание Mage_Fireball

**Цель:** Создать первый тестовый скилл для новой системы
**Тип:** ProjectileDamage с Burn эффектом
**Время:** ~5 минут

---

## ШАГ 1: Создать SkillConfig файл

### В Unity Editor:

1. **Откройте проект** Unity (Aetherion)

2. **В окне Project:**
   - Перейдите в папку `Assets/Resources/Skills/`
   - Правой кнопкой мыши → Create → Aetherion → Combat → **Skill Config**
   - Назовите файл: `Mage_Fireball`

3. **Если меню "Aetherion" не появляется:**
   - Дождитесь компиляции скриптов (Unity должна скомпилировать SkillConfig.cs)
   - Проверьте Console на ошибки
   - Если есть ошибки - сообщи мне

---

## ШАГ 2: Настроить параметры Fireball

### Выберите файл Mage_Fireball.asset в Project

### В Inspector заполните поля:

#### ═══ ОСНОВНАЯ ИНФОРМАЦИЯ ═══

```
Skill Id: 201
Skill Name: Fireball
Description: Огненный шар с большим уроном. Поджигает цель.
Icon: (оставьте пустым пока)
Character Class: Mage
```

#### ═══ ПАРАМЕТРЫ ИСПОЛЬЗОВАНИЯ ═══

```
Cooldown: 6
Mana Cost: 40
Cast Range: 20
Cast Time: 0.8
Can Use While Moving: ✅ (галочка)
```

#### ═══ ТИП СКИЛЛА ═══

```
Skill Type: ProjectileDamage
Target Type: Enemy
Requires Target: ✅ (галочка)
Can Target Allies: ❌ (снять галочку)
Can Target Enemies: ✅ (галочка)
```

#### ═══ УРОН / ЛЕЧЕНИЕ ═══

```
Base Damage Or Heal: 60
Strength Scaling: 0
Intelligence Scaling: 25
```

#### ═══ ЭФФЕКТЫ ═══

**НЕ ЗАПОЛНЯЕМ ПОКА** (добавим Burn effect позже)

```
Effects: (оставьте пустым List)
```

#### ═══ AOE ═══

```
Aoe Radius: 0
Max Targets: 1
```

#### ═══ СНАРЯД ═══

**ВАЖНО:** Нужно найти префаб FireballProjectile

```
Projectile Prefab: (нужно найти)
Projectile Speed: 20
Projectile Lifetime: 5
Projectile Homing: ✅ (галочка)
Homing Speed: 10
Homing Radius: 20
```

**Как найти префаб:**
1. В Project окне найдите `Assets/Prefabs/Projectiles/FireballProjectile.prefab`
2. Перетащите его в поле `Projectile Prefab`

**Если FireballProjectile НЕ НАЙДЕН:**
- Используйте `CelestialBallProjectile` (временно)
- Или сообщи - я помогу найти правильный префаб

#### ═══ ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ═══

```
Cast Effect Prefab: (оставьте пустым)
Hit Effect Prefab: (попробуйте найти CFXR3 Fire Explosion)
Aoe Effect Prefab: (пустым)
```

**Поиск Hit Effect:**
1. В Project: поиск "Fire Explosion"
2. Если найден - перетащите в Hit Effect Prefab

#### ═══ АНИМАЦИЯ ═══

```
Animation Trigger: Attack
Animation Speed: 1
Projectile Spawn Timing: 0.5
```

#### ═══ ЗВУКИ ═══

```
Cast Sound: (пустым пока)
Hit Sound: (пустым пока)
Sound Volume: 0.7
```

#### ═══ ДВИЖЕНИЕ ═══

```
Enable Movement: ❌ (снять галочку)
```

#### ═══ ПРИЗЫВ ═══

```
(всё пустым - не используется)
```

#### ═══ ТРАНСФОРМАЦИЯ ═══

```
(всё пустым - не используется)
```

#### ═══ СЕТЕВАЯ СИНХРОНИЗАЦИЯ ═══

```
Sync Projectiles: ✅ (галочка)
Sync Hit Effects: ✅ (галочка)
Sync Status Effects: ✅ (галочка)
```

---

## ШАГ 3: Сохранить

1. **Ctrl+S** или **File → Save**
2. Проверьте в Console - нет ли ошибок

---

## ШАГ 4: Добавить скилл на персонажа

### Вариант А: Вручную в Inspector

1. **Откройте Arena Scene** (Assets/Scenes/Arena.unity)
2. **В Hierarchy найдите LocalPlayer** (или игрока)
3. **В Inspector найдите компонент SkillExecutor**
   - Если нет - он добавится автоматически при запуске
4. **Раскройте "Equipped Skills"**
5. **Установите Size = 1**
6. **Перетащите Mage_Fireball в Element 0**

### Вариант Б: Автоматически через код (позже)

---

## ШАГ 5: Тестирование

### Запустить игру:

1. **Play** в Unity Editor
2. **Подойдите к врагу** (Enemy или DummyEnemy)
3. **Нажмите "1"** (первый слот скилла)

### Что должно произойти:

✅ **Консоль покажет:**
```
[SkillExecutor] ⚡ Использован скилл: Fireball
[SkillExecutor] 🚀 Создан снаряд: FireballProjectile, урон: 60
```

✅ **Визуально:**
- Анимация атаки
- Снаряд вылетает
- Летит к врагу (homing)
- Попадает
- Взрыв (hit effect)
- Враг получает урон

❌ **Если не работает:**
- Проверьте Console на ошибки
- Проверьте что SkillExecutor добавлен на персонажа
- Проверьте что Mage_Fireball в equippedSkills[0]
- Сообщи мне - я помогу

---

## ШАГ 6: Добавить Burn эффект (ОПЦИОНАЛЬНО)

### Если хотите добавить поджигание:

1. **Выберите Mage_Fireball.asset**
2. **В Inspector → Effects**
3. **Установите Size = 1**
4. **Раскройте Element 0:**

```
Effect Type: Burn
Duration: 3
Power: 0
Damage Or Heal Per Tick: 10
Tick Interval: 1
Intelligence Scaling: 0
Strength Scaling: 0
Particle Effect Prefab: (найдите огненные частицы если есть)
Apply Sound: (пустым)
Remove Sound: (пустым)
Sound Volume: 0.5
Can Be Dispelled: ✅
Can Stack: ❌
Max Stacks: 1
Break On Damage: ❌
Sync With Server: ✅
```

5. **Сохраните (Ctrl+S)**

6. **Протестируйте снова:**
   - Попадите по врагу
   - Враг должен получить тиковый урон каждую секунду в течение 3 секунд
   - Консоль покажет: `[EffectManager] ✨ Применён эффект: Burn (3с)`
   - Консоль покажет тики: `[EffectManager] 💀 DoT тик: -10 HP от Burn`

---

## ГОТОВО!

После выполнения этих шагов у вас будет:

✅ Первый рабочий скилл (Mage_Fireball)
✅ Снаряд летит и наводится на врага
✅ Наносит урон
✅ (Опционально) Поджигает цель

---

## ЕСЛИ ВОЗНИКЛИ ПРОБЛЕМЫ

### Проблема: "Меню Aetherion → Combat → Skill Config не появляется"

**Решение:**
1. Проверьте Console на ошибки компиляции
2. Убедитесь что файл `SkillConfig.cs` существует в `Assets/Scripts/Skills/`
3. Подождите завершения компиляции (крутящаяся иконка в правом нижнем углу Unity)

### Проблема: "Префаб FireballProjectile не найден"

**Решение:**
1. Используйте поиск в Project: "Fireball"
2. Если не найден - используйте `CelestialBallProjectile` (временно)
3. Сообщи - я помогу найти или создать правильный префаб

### Проблема: "При нажатии '1' ничего не происходит"

**Решение:**
1. Проверьте Console - есть ли сообщения?
2. Проверьте что SkillExecutor добавлен на LocalPlayer (в Inspector)
3. Проверьте что Mage_Fireball в Equipped Skills[0]
4. Проверьте что у вас есть цель (враг в пределах 20 метров)

### Проблема: "Ошибки в Console при создании скилла"

**Решение:**
- Скопируй текст ошибки и отправь мне
- Я помогу исправить

---

**Следующий шаг после тестирования:**
- Создать больше скиллов (Ice Nova, Hammer of Justice, и т.д.)
- Добавить сетевую синхронизацию для мультиплеера
- Создать UI для отображения иконок и кулдаунов

Удачи! 🔥🚀
