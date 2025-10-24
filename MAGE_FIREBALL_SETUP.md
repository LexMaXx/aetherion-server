# 🔥 Настройка первого скилла: Mage Fireball

## ✅ Статус: Система готова к тестированию

Все 4 скрипта новой системы скиллов созданы и интегрированы:
- ✅ `SkillConfig.cs` - конфигурация скилла
- ✅ `EffectConfig.cs` - конфигурация эффектов
- ✅ `SkillExecutor.cs` - исполнитель скиллов
- ✅ `EffectManager.cs` - менеджер эффектов
- ✅ `PlayerAttackNew.cs` - интеграция (клавиши 1/2/3)
- ✅ `SocketIOManager.cs` - сетевая синхронизация
- ✅ Конфликты enum'ов исправлены

---

## 📋 Следующий шаг: Создать Mage_Fireball.asset в Unity

### 1. Откройте Unity Editor

### 2. Создайте ScriptableObject для Fireball
**Project Window → ПКМ →**
```
Create → Aetherion → Combat → Skill Config
```

**Имя файла:** `Mage_Fireball`
**Путь:** `Assets/ScriptableObjects/Skills/Mage/`

---

## 🔥 Настройки Mage_Fireball

### Основная информация:
```
Skill Id: 201
Skill Name: Fireball
Description: Запускает огненный шар, наносящий урон и накладывающий эффект горения
Character Class: Mage
```

### Параметры использования:
```
Cooldown: 6
Mana Cost: 30
Cast Range: 25
Cast Time: 0.8
Can Use While Moving: false (нужно стоять при касте)
```

### Тип скилла:
```
Skill Type: ProjectileDamage
Target Type: Enemy
Requires Target: true
Can Target Allies: false
Can Target Enemies: true
```

### Урон:
```
Base Damage Or Heal: 50
Strength Scaling: 0
Intelligence Scaling: 2.5
```
*(При 100 Intelligence = 50 + 100*2.5 = 300 урона)*

### Снаряд:
```
Projectile Prefab: Assets/Prefabs/Projectiles/CelestialBallProjectile.prefab
Projectile Speed: 15
Projectile Lifetime: 5
Projectile Homing: true
Homing Speed: 10
Homing Radius: 20
```

### Визуальные эффекты:
```
Cast Effect Prefab: (оставить пустым пока)
Hit Effect Prefab: Assets/Resources/Effects/CFXR3 Fire Explosion B 1.prefab
Aoe Effect Prefab: (оставить пустым)
```

### Анимация:
```
Animation Trigger: Attack
Animation Speed: 1.5
Projectile Spawn Timing: 0.6
```

### Звуки:
```
Cast Sound: (оставить пустым пока)
Hit Sound: (оставить пустым пока)
Sound Volume: 0.7
```

### Движение:
```
Enable Movement: false
```

### Эффекты (Effects):
**Нажмите "+" чтобы добавить эффект:**

#### Effect [0] - Burn (Горение):
```
Effect Type: Burn
Duration: 5
Power: 0
Damage Or Heal Per Tick: 10
Tick Interval: 1
Intelligence Scaling: 0.5
Strength Scaling: 0
Particle Effect Prefab: (можно оставить пустым или добавить огненный эффект)
Can Be Dispelled: true
Can Stack: false
Max Stacks: 1
Sync With Server: true
```
*(При 100 Intelligence = 10 + 100*0.5 = 60 урона за тик, 5 тиков = 300 урона от DoT)*

### Сетевая синхронизация:
```
Sync Projectiles: true
Sync Hit Effects: true
Sync Status Effects: true
```

---

## 🎮 Тестирование в Arena сцене

### 1. Откройте сцену Arena
`Assets/Scenes/Arena.unity`

### 2. Найдите LocalPlayer префаб в сцене

### 3. Проверьте компоненты на LocalPlayer:
Unity автоматически добавит при запуске:
- ✅ `SkillExecutor`
- ✅ `EffectManager`

### 4. Настройте SkillExecutor:
В компоненте `SkillExecutor` на LocalPlayer:
```
Equipped Skills → Size: 3
  Element 0: Mage_Fireball (перетащите созданный asset)
  Element 1: None
  Element 2: None
```

### 5. Запустите игру (Play)

### 6. Протестируйте:
1. **Подключитесь к серверу** (Enter Game)
2. **Выберите мага** (Character Selection)
3. **Войдите в арену**
4. **Найдите врага** (любого dummy или другого игрока)
5. **Нажмите клавишу "1"** чтобы использовать Fireball

---

## ✅ Ожидаемый результат:

### Клиент-кастер:
1. ✅ Анимация атаки (Attack trigger, скорость 1.5x)
2. ✅ Расход маны (-30)
3. ✅ Кулдаун 6 секунд
4. ✅ Создание огненного шара (CelestialBallProjectile)
5. ✅ Полёт снаряда к цели с хомингом
6. ✅ Попадание = урон + эффект взрыва
7. ✅ Эффект Burn на цели (5 секунд, тикает каждую секунду)
8. ✅ HP цели уменьшается (50 + int*2.5 + 5 тиков по (10 + int*0.5))

### Другие игроки видят:
1. ✅ Анимацию каста
2. ✅ Летящий огненный шар
3. ✅ Эффект взрыва при попадании
4. ✅ Иконку Burn над целью
5. ✅ Уменьшение HP цели

---

## 🐛 Если что-то не работает:

### Проблема: "Skill slot 0 is empty"
**Решение:** Убедитесь, что Mage_Fireball добавлен в Equipped Skills[0]

### Проблема: "Not enough mana"
**Решение:** Проверьте, что у мага есть минимум 30 маны

### Проблема: "No target selected"
**Решение:** Нажмите ЛКМ на враге, затем нажмите "1"

### Проблема: Снаряд взрывается на пол пути
**Решение:** Проверьте, что CelestialBallProjectile имеет Layer 7 (Projectile)

### Проблема: Урон не наносится
**Решение:** Проверьте консоль Unity на ошибки, убедитесь что у цели есть HealthSystem

### Проблема: Эффект Burn не тикает
**Решение:** Проверьте, что EffectManager правильно добавлен к цели и вызывает Update()

---

## 📊 Debug логи

В консоли должны появиться логи:
```
[SkillExecutor] 🎯 Using skill: Fireball (slot 0)
[SkillExecutor] ✅ Executing ProjectileDamage: Fireball
[SkillExecutor] 🔮 Creating projectile for Fireball
[CelestialProjectile] ⚡ Projectile hit Enemy!
[CelestialProjectile] 💥 Dealing 300 damage
[EffectManager] ✨ Applied effect: Burn (5.0s)
[EffectManager] 🔥 Burn tick: 60 damage
```

---

## 🚀 После успешного теста:

1. ✅ Создать ещё 2-3 простых скилла (Ice Nova AOE, Lightning Strike instant)
2. ✅ Добавить UI индикаторы кулдаунов
3. ✅ Обновить server.js для валидации скиллов
4. ✅ Добавить обработчики в NetworkSyncManager (skill_casted, effect_applied)
5. ✅ Протестировать в мультиплеере (2 клиента)

---

## 📝 Примечания:

- **Старая система (SkillData)** всё ещё существует и работает - мы её не трогали
- **Новая система (SkillConfig)** работает параллельно
- Постепенно мигрируем все скиллы на новую систему
- Для тестов используем Arena сцену (уже настроена)

**Готово к тестированию! 🎉**
