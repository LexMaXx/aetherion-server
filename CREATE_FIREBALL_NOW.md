# 🔥 Создание первого скилла Mage_Fireball

## ✅ Подготовка завершена!

Я создал Editor скрипт, который автоматически создаст ScriptableObject для Mage_Fireball.

---

## 🚀 Как создать скилл (2 способа):

### Способ 1: Через меню Unity (РЕКОМЕНДУЕТСЯ)

1. **Откройте Unity Editor**
2. **В верхнем меню выберите:**
   ```
   Tools → Skills → Create Mage Fireball
   ```
3. **Готово!** Скилл будет создан автоматически

### Способ 2: Вручную (если нужно)

1. **Project Window → ПКМ →**
   ```
   Create → Aetherion → Combat → Skill Config
   ```
2. **Назовите:** `Mage_Fireball`
3. **Переместите в:** `Assets/ScriptableObjects/Skills/Mage/`
4. **Настройте параметры вручную** (см. ниже)

---

## 📋 Что создаст автоматический скрипт:

### Основная информация:
```
Skill ID: 201
Skill Name: Fireball
Description: Запускает огненный шар, наносящий урон и накладывающий эффект горения
Character Class: Mage
```

### Параметры:
```
Cooldown: 6 секунд
Mana Cost: 30
Cast Range: 25 метров
Cast Time: 0.8 секунды
Can Use While Moving: false (нужно стоять)
```

### Тип скилла:
```
Skill Type: ProjectileDamage
Target Type: Enemy (требует цель)
Requires Target: true
```

### Урон:
```
Base Damage: 50
Intelligence Scaling: 2.5
Strength Scaling: 0

Расчёт: 50 + (Intelligence * 2.5)
При 100 Intelligence = 50 + 250 = 300 урона
```

### Снаряд:
```
Projectile Prefab: CelestialBallProjectile.prefab
Projectile Speed: 15
Projectile Lifetime: 5 секунд
Projectile Homing: true (наведение на цель)
Homing Speed: 10
Homing Radius: 20
```

### Визуальные эффекты:
```
Hit Effect: CFXR3 Fire Explosion B 1.prefab (огненный взрыв)
Animation Trigger: Attack
Animation Speed: 1.5x
Projectile Spawn Timing: 0.6 секунды
```

### Эффект Burn (Горение):
```
Effect Type: Burn
Duration: 5 секунд
Damage Per Tick: 10
Tick Interval: 1 секунда (тикает каждую секунду)
Intelligence Scaling: 0.5

Расчёт: 10 + (Intelligence * 0.5)
При 100 Intelligence = 10 + 50 = 60 урона за тик
5 тиков × 60 = 300 урона от горения

ОБЩИЙ УРОН: 300 (прямой) + 300 (DoT) = 600
```

### Сетевая синхронизация:
```
Sync Projectiles: true
Sync Hit Effects: true
Sync Status Effects: true
```

---

## 🎮 После создания скилла:

### Шаг 1: Добавить скилл к персонажу

1. **Откройте сцену Arena:**
   ```
   Assets/Scenes/Arena.unity
   ```

2. **Найдите в Hierarchy:**
   ```
   LocalPlayer (или GameManager/LocalPlayer)
   ```

3. **В Inspector найдите компонент:**
   ```
   Skill Executor (Script)
   ```

   Если компонента нет - он добавится автоматически при запуске игры.

4. **В секции Equipped Skills:**
   ```
   Size: 3
   Element 0: <перетащите Mage_Fireball сюда>
   Element 1: None
   Element 2: None
   ```

### Шаг 2: Запустить игру

1. **Нажмите Play** ▶️
2. **Войдите в игру** (Enter Game)
3. **Выберите мага** (Character Selection)
4. **Войдите в арену**

### Шаг 3: Протестировать скилл

1. **Найдите врага** (Dummy или другого игрока)
2. **Выберите цель** (ЛКМ на враге)
3. **Нажмите клавишу "1"** 🔥
4. **Наблюдайте:**
   - ✅ Анимация каста (Attack)
   - ✅ Расход маны (-30)
   - ✅ Огненный шар летит к цели
   - ✅ Взрыв при попадании
   - ✅ Урон (300)
   - ✅ Эффект Burn (5 секунд, 60 урона/сек)
   - ✅ Кулдаун 6 секунд

---

## ✅ Ожидаемые результаты:

### У кастера (вас):
- Анимация каста (0.8 сек)
- Мана уменьшается на 30
- Огненный шар вылетает из позиции ProjectileSpawnPoint
- Снаряд летит к цели с хомингом
- Кулдаун 6 секунд (нельзя использовать повторно)

### У цели:
- Снаряд попадает в цель
- Огненный взрыв (CFXR3 Fire Explosion B 1)
- HP уменьшается на ~300
- Появляется эффект Burn (визуальный эффект горения)
- Каждую секунду HP уменьшается на ~60 (5 раз)
- Через 5 секунд эффект Burn исчезает

### Другие игроки видят:
- Вашу анимацию каста
- Летящий огненный шар
- Взрыв при попадании
- Эффект Burn на цели
- Уменьшение HP цели

---

## 🐛 Возможные проблемы:

### "Skill slot 0 is empty"
**Решение:** Убедитесь, что Mage_Fireball добавлен в Equipped Skills[0]

### "Not enough mana"
**Решение:** У мага должно быть минимум 30 маны

### "No target selected"
**Решение:** Выберите врага ЛКМ, затем нажмите "1"

### Снаряд не летит
**Решение:**
- Проверьте, что CelestialBallProjectile.prefab существует
- Проверьте Console на ошибки

### Урон не наносится
**Решение:**
- Проверьте, что у цели есть HealthSystem
- Проверьте Console на ошибки
- Проверьте, что projectileSpeed > 0

### Эффект Burn не тикает
**Решение:**
- Проверьте, что EffectManager добавлен к цели
- Проверьте Console на логи "[EffectManager]"

---

## 📊 Debug информация:

В Console должны появиться логи:

```
[SkillExecutor] 🎯 Using skill: Fireball (slot 0)
[SkillExecutor] ✅ Executing ProjectileDamage: Fireball
[SkillExecutor] 🚀 Создан снаряд: CelestialBallProjectile, урон: 300
[CelestialProjectile] ⚡ Projectile hit Enemy!
[CelestialProjectile] 💥 Dealing 300 damage
[EffectManager] ✨ Applied effect: Burn (5.0s)
[EffectManager] 🔥 Burn tick: 60 damage
[EffectManager] 🔥 Burn tick: 60 damage
...
```

---

## 🚀 Следующие шаги:

После успешного теста Fireball:

1. ✅ Создать Ice Nova (AOE скилл)
2. ✅ Создать Lightning Strike (Instant Damage + Stun)
3. ✅ Добавить UI для скиллов (иконки, кулдауны)
4. ✅ Обновить server.js для валидации
5. ✅ Протестировать в мультиплеере

---

## 📝 Файлы:

**Editor скрипт (создан):**
- `Assets/Scripts/Editor/CreateMageFireball.cs`

**ScriptableObject (будет создан):**
- `Assets/ScriptableObjects/Skills/Mage/Mage_Fireball.asset`

**Система скиллов:**
- `Assets/Scripts/Skills/SkillConfig.cs`
- `Assets/Scripts/Skills/EffectConfig.cs`
- `Assets/Scripts/Skills/SkillExecutor.cs`
- `Assets/Scripts/Skills/EffectManager.cs`

---

**🔥 Готово! Откройте Unity и создайте Fireball через Tools → Skills → Create Mage Fireball**
