# ⚡ СОЗДАНИЕ LIGHTNING BALL PREFAB - ПОШАГОВО

## ❌ ПРОБЛЕМА

```
[SkillManager] ❌ У префаба EnergyBall(Clone) нет компонента Projectile!
```

**Причина:** EnergyBall.prefab - это только визуальная модель (3D модель), у него **НЕТ** компонента Projectile.cs!

**Решение:** Создать новый префаб который объединяет:
- ✅ **Визуал** (EnergyBall)
- ✅ **Логику полёта** (Projectile.cs)
- ✅ **Физику** (Rigidbody, Collider)
- ✅ **Свечение** (Light)

---

## 🎯 СОЗДАНИЕ ПРЕФАБА В UNITY (5 МИНУТ)

### Шаг 1: Создать пустой GameObject

1. **В Unity Editor:**
   ```
   Hierarchy → Правый клик → Create Empty
   Назвать: "LightningBallProjectile"
   ```

2. **Сбросить Transform:**
   ```
   Inspector → Transform → Reset (gear icon)
   Position: (0, 0, 0)
   Rotation: (0, 0, 0)
   Scale: (1, 1, 1)
   ```

---

### Шаг 2: Добавить компонент Projectile

1. **Select LightningBallProjectile в Hierarchy**

2. **Inspector → Add Component:**
   ```
   Найти: "Projectile"
   ИЛИ
   Scripts → Projectile
   ```

3. **Настроить Projectile:**
   ```
   Speed: 20
   Lifetime: 5
   Homing: false (или true для самонаведения)
   Rotation Speed: 360
   Trail: (оставить пустым)
   Hit Effect: (оставить пустым пока)
   ```

---

### Шаг 3: Добавить SphereCollider

1. **Add Component → Physics → Sphere Collider**

2. **Настроить:**
   ```
   Is Trigger: ✓ (ОБЯЗАТЕЛЬНО!)
   Radius: 0.5
   Center: (0, 0, 0)
   ```

---

### Шаг 4: Добавить Rigidbody

1. **Add Component → Physics → Rigidbody**

2. **Настроить:**
   ```
   Mass: 1
   Drag: 0
   Angular Drag: 0.05
   Use Gravity: ✗ (ОТКЛЮЧИТЬ!)
   Is Kinematic: ✓ (ВКЛЮЧИТЬ!)
   ```

---

### Шаг 5: Добавить Light (свечение)

1. **Add Component → Rendering → Light**

2. **Настроить:**
   ```
   Type: Point
   Color: RGB(0.3, 0.7, 1.0) ← Голубой!
   Intensity: 6
   Range: 8
   ```

3. **Дополнительно (для URP):**
   ```
   Add Component → Rendering → Light 2D (Universal RP)
   ИЛИ
   Universal Render Pipeline → Light
   ```

---

### Шаг 6: Добавить визуал (EnergyBall)

**Вариант A: Использовать EnergyBall префаб**

1. **Project → Assets/Prefabs/Transformations/EnergyBall.prefab**

2. **Перетащить на LightningBallProjectile в Hierarchy**
   - EnergyBall станет child объектом

3. **Переименовать EnergyBall → "Visual"**

4. **Настроить Transform:**
   ```
   Position: (0, 0, 0)
   Rotation: (0, 0, 0)
   Scale: (2, 2, 2) ← УВЕЛИЧИТЬ чтобы было видно!
   ```

**Вариант B: Использовать Hovl Studio эффект**

1. **Project → Assets/Hovl Studio/Magic effects pack/Prefabs/Hits and explosions/**

2. **Найти "Electro hit.prefab"**

3. **Перетащить на LightningBallProjectile**

4. **Переименовать → "Visual"**

5. **Scale: (1.5, 1.5, 1.5)**

**Вариант C: Создать простую Sphere**

1. **Select LightningBallProjectile**

2. **Правый клик → 3D Object → Sphere**

3. **Назвать "Visual"**

4. **Transform:**
   ```
   Position: (0, 0, 0)
   Scale: (0.5, 0.5, 0.5)
   ```

5. **Material:**
   ```
   Inspector → Mesh Renderer → Materials
   - Material: Standard
   - Albedo Color: Голубой/Жёлтый
   - Emission: ✓
   - Emission Color: Яркий голубой
   - Emission Intensity: 2
   ```

---

### Шаг 7: Сохранить как Prefab

1. **Создать папку (если нет):**
   ```
   Project → Assets/Prefabs/Projectiles
   (Правый клик → Create → Folder)
   ```

2. **Перетащить LightningBallProjectile:**
   ```
   Из Hierarchy → В Project → Assets/Prefabs/Projectiles/
   ```

3. **Подтвердить:**
   ```
   "Original Prefab" (если спросит)
   ```

4. **Префаб создан!** ✅
   ```
   Assets/Prefabs/Projectiles/LightningBallProjectile.prefab
   ```

5. **Удалить из сцены:**
   ```
   Select LightningBallProjectile в Hierarchy → Delete
   ```

---

### Шаг 8: Назначить в SkillData

1. **Project → Assets/Resources/Skills/Mage_LightningStorm.asset**

2. **Inspector → Projectile Prefab:**
   ```
   Перетащить LightningBallProjectile из:
   Assets/Prefabs/Projectiles/LightningBallProjectile.prefab
   ```

3. **Сохранить:**
   ```
   Ctrl + S
   File → Save Project
   ```

---

## ✅ ИТОГОВАЯ СТРУКТУРА ПРЕФАБА

```
LightningBallProjectile.prefab
│
├── Transform
│   ├── Position: (0, 0, 0)
│   ├── Rotation: (0, 0, 0)
│   └── Scale: (1, 1, 1)
│
├── Projectile (Script) ⭐
│   ├── speed: 20
│   ├── lifetime: 5
│   ├── homing: false
│   ├── trail: none
│   ├── hitEffect: none
│   └── rotationSpeed: 360
│
├── SphereCollider ⭐
│   ├── isTrigger: true
│   └── radius: 0.5
│
├── Rigidbody ⭐
│   ├── useGravity: false
│   └── isKinematic: true
│
├── Light ⭐
│   ├── type: Point
│   ├── color: (0.3, 0.7, 1) голубой
│   ├── intensity: 6
│   └── range: 8
│
└── Visual (Child GameObject) ⭐
    ├── EnergyBall prefab instance
    │   ИЛИ
    ├── Electro hit prefab
    │   ИЛИ
    └── Sphere + Material

    Transform:
    ├── position: (0, 0, 0)
    └── scale: (2, 2, 2)
```

---

## 🧪 ТЕСТИРОВАНИЕ

1. **Проверить префаб:**
   ```
   Project → LightningBallProjectile.prefab
   Двойной клик → Prefab Mode

   Проверить компоненты:
   ✅ Projectile (Script)
   ✅ SphereCollider (IsTrigger = true)
   ✅ Rigidbody (UseGravity = false, IsKinematic = true)
   ✅ Light (голубой)
   ✅ Visual (child)
   ```

2. **Проверить SkillData:**
   ```
   Assets/Resources/Skills/Mage_LightningStorm.asset
   Inspector → Projectile Prefab → назначен LightningBallProjectile? ✅
   ```

3. **Запустить игру:**
   ```
   Play
   Использовать Lightning Storm (клавиша 3)
   ```

4. **Должно работать:**
   ```
   ✅ Энергетический шар появляется
   ✅ Шар светится голубым
   ✅ Шар летит к врагу
   ✅ Шар вращается
   ✅ Попадает в цель
   ✅ Наносит урон
   ✅ Взрыв при попадании
   ```

---

## 📊 СРАВНЕНИЕ С FIREBALL

Можете использовать FireballProjectile как шаблон:

1. **Project → Assets/Prefabs/Projectiles/FireballProjectile.prefab**

2. **Правый клик → Duplicate**

3. **Переименовать → LightningBallProjectile**

4. **Изменить:**
   - Visual → заменить на EnergyBall
   - Light → Color → голубой
   - Material → голубой вместо красного

5. **Назначить в Mage_LightningStorm.asset**

---

## 🎨 УЛУЧШЕНИЯ (ОПЦИОНАЛЬНО)

### Добавить Trail (след за снарядом):

1. **Select LightningBallProjectile (в Prefab Mode)**

2. **Add Component → Effects → Trail Renderer**

3. **Настроить:**
   ```
   Width:
   - Start: 0.5
   - End: 0.05

   Time: 0.5

   Color Gradient:
   - Start: Голубой (прозрачный)
   - Middle: Яркий голубой
   - End: Прозрачный

   Material: Trail материал
   ```

4. **В Projectile script:**
   ```
   Trail → перетащить Trail Renderer
   ```

### Добавить Particle System (искры):

1. **Select Visual**

2. **Add Component → Effects → Particle System**

3. **Настроить:**
   ```
   Start Lifetime: 0.5
   Start Speed: 2
   Start Size: 0.1
   Emission Rate: 30
   Color: Голубой + Жёлтый
   ```

### Добавить взрыв при попадании:

1. **SkillData → Projectile Hit Effect Prefab:**
   ```
   Перетащить:
   Assets/Hovl Studio/.../Electro hit.prefab
   ```

---

## 🐛 ВОЗМОЖНЫЕ ПРОБЛЕМЫ

### Проблема 1: Снаряд всё ещё не летит

**Проверить:**
- ✅ Projectile компонент добавлен?
- ✅ Speed > 0?
- ✅ Rigidbody → IsKinematic = true?

### Проблема 2: Снаряд не видно

**Проверить:**
- ✅ Visual child объект существует?
- ✅ Visual → Scale достаточно большой? (минимум 1, рекомендуется 2)
- ✅ Visual → Active (галочка включена)?
- ✅ Material с Emission включен?

### Проблема 3: Снаряд не попадает в цель

**Проверить:**
- ✅ SphereCollider → IsTrigger = true?
- ✅ Враг имеет Collider?
- ✅ Projectile компонент инициализирован?

### Проблема 4: Нет свечения

**Проверить:**
- ✅ Light компонент добавлен?
- ✅ Light → Enabled = true?
- ✅ Light → Intensity > 0?
- ✅ Light → Range > 0?

---

## 💡 БЫСТРОЕ РЕШЕНИЕ (2 МИНУТЫ)

**Если некогда разбираться:**

1. **Скопировать FireballProjectile:**
   ```
   Assets/Prefabs/Projectiles/FireballProjectile.prefab
   Ctrl+D → Duplicate
   Переименовать → LightningBallProjectile
   ```

2. **Заменить визуал:**
   ```
   Открыть префаб
   Удалить старый Visual
   Добавить EnergyBall как Visual
   Scale: (2, 2, 2)
   ```

3. **Изменить цвет:**
   ```
   Light → Color → Голубой (0.3, 0.7, 1)
   ```

4. **Назначить в SkillData:**
   ```
   Mage_LightningStorm → Projectile Prefab → LightningBallProjectile
   ```

5. **Готово!** ⚡

---

## ✅ ЧЕКЛИСТ

Перед тестированием убедитесь:

- [ ] Создан LightningBallProjectile.prefab
- [ ] Добавлен Projectile (Script)
- [ ] Добавлен SphereCollider (IsTrigger = true)
- [ ] Добавлен Rigidbody (UseGravity = false, IsKinematic = true)
- [ ] Добавлен Light (голубой)
- [ ] Добавлен Visual (child, Scale >= 1)
- [ ] Префаб сохранён в Assets/Prefabs/Projectiles/
- [ ] Назначен в Mage_LightningStorm.asset → Projectile Prefab
- [ ] Unity Editor → Save Project (Ctrl+S)

---

**После этого Lightning Storm будет работать как Fireball!** ⚡🔥
