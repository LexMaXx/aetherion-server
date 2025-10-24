# 💀 НАСТРОЙКА ETHEREAL SKULL PROJECTILE

**Дата:** 21 октября 2025
**Цель:** Создать снаряд Ethereal_Skull с теми же эффектами что у CelestialBall (свечение, хвост, хит эффект)

---

## 📋 ЧТО НУЖНО СДЕЛАТЬ

Превратить модель `Ethereal_Skull_1020210937_texture.fbx` в полноценный снаряд с:
- ✅ **Свечение** (Point Light)
- ✅ **Хвост** (Trail Renderer)
- ✅ **Эффект попадания** (Particle System)
- ✅ **Скрипт движения** (CelestialProjectile)
- ✅ **Коллизия** (Sphere Collider)
- ✅ **Физика** (Rigidbody)

---

## 🛠️ ПОШАГОВАЯ ИНСТРУКЦИЯ

### ШАГ 1: Откройте префаб в Unity

1. **Найдите файл** в Project:
   ```
   Assets/Prefabs/Projectiles/Ethereal_Skull_1020210937_texture.prefab
   ```

2. **Дважды кликните** на префаб чтобы открыть в Prefab Mode

3. **Если префаба нет** - создайте его:
   - Перетащите `.fbx` файл на сцену
   - Переименуйте в `EtherealSkullProjectile`
   - Перетащите обратно в папку `Assets/Prefabs/Projectiles/`
   - Удалите со сцены

---

### ШАГ 2: Настройка Transform

1. **Выберите корневой объект** `EtherealSkullProjectile` в Hierarchy
2. **В Inspector → Transform:**
   ```
   Position: (0, 0, 0)
   Rotation: (0, 0, 0)
   Scale:    (20, 20, 20)  ← Размер как у CelestialBall
   ```

3. **Если внутри есть Model** (дочерний объект):
   - Rotation можно настроить для правильной ориентации
   - Например: `(0, 90, 0)` если череп смотрит не в ту сторону

---

### ШАГ 3: Добавить Sphere Collider (Trigger)

1. **Выберите корневой объект**
2. **Add Component → Physics → Sphere Collider**
3. **В Inspector настройте:**
   ```
   Is Trigger: ✅ (включено!)
   Radius: 0.4
   Center: (0, 0, 0)
   ```

**Зачем:** Trigger для детектирования попадания в врага без физического столкновения

---

### ШАГ 4: Добавить Rigidbody (Kinematic)

1. **Add Component → Physics → Rigidbody**
2. **В Inspector настройте:**
   ```
   Mass: 1
   Drag: 0
   Angular Drag: 0.05
   Use Gravity: ❌ (ВЫКЛЮЧЕНО!)
   Is Kinematic: ✅ (ВКЛЮЧЕНО!)
   Interpolate: None
   Collision Detection: Discrete
   ```

**Зачем:** Rigidbody нужен для работы Trigger'ов, Kinematic = управляется скриптом

---

### ШАГ 5: Добавить CelestialProjectile Script

1. **Add Component** → напишите `CelestialProjectile`
2. **Скрипт автоматически найдет компоненты**, но можно настроить вручную:

#### Movement Settings:
```
Base Speed: 15
Homing Speed: 8
Lifetime: 5
Acceleration Rate: 1.5
```

#### Homing Settings:
```
Enable Homing: ✅
Homing Start Delay: 0.1
Homing Radius: 30
Target Acquisition Angle: 90
```

#### Visual Effects:
```
Hit Effect: ← Сюда перетащим эффект на ШАГ 6
Wind Effect: (оставить пустым или добавить позже)
Projectile Light: ← Создадим на ШАГ 7
Trail: ← Создадим на ШАГ 8
```

#### Rotation:
```
Rotation Speed: 360
Visual Transform: ← Перетащить дочерний Model (если есть)
```

#### Audio:
```
Launch Sound: (можно оставить пустым)
Hit Sound: (можно оставить пустым)
```

---

### ШАГ 6: Добавить Hit Effect (Эффект попадания)

**Вариант 1: Использовать существующий эффект**

1. **В Project найдите:**
   ```
   Assets/Resources/Effects/CFXR3 Fire Explosion B.prefab
   ```
   ИЛИ другой подходящий эффект из CFXR3 или Hovl Studio

2. **Перетащите** в поле `Hit Effect` скрипта CelestialProjectile

**Вариант 2: Создать свой**

1. Создайте Particle System с эффектом взрыва
2. Сохраните как префаб
3. Назначьте в `Hit Effect`

---

### ШАГ 7: Добавить Point Light (Свечение)

1. **Выберите корневой объект** `EtherealSkullProjectile`
2. **Add Component → Rendering → Light**
3. **В Inspector настройте:**

```
Type: Point
Color: #9B4DCA (фиолетовый) или #00FFFF (голубой)
       ИЛИ выберите цвет который подходит черепу
Intensity: 2
Range: 5
Shadow Type: No Shadows
```

**Для мистического черепа рекомендую:**
- 🟣 Фиолетовый `(155, 77, 202)` - темная магия
- 🟢 Зеленый `(0, 255, 100)` - токсичная магия
- 🔵 Голубой `(0, 200, 255)` - ледяная магия
- 🔴 Красный `(255, 50, 50)` - огненная магия

4. **Перетащите** Light компонент в поле `Projectile Light` скрипта

---

### ШАГ 8: Добавить Trail Renderer (Хвост)

**Способ 1: Добавить к корневому объекту**

1. **Add Component → Effects → Trail Renderer**
2. **Настройте параметры:**

```
═══════════ Time ═══════════
Time: 0.5  (длина следа)
Min Vertex Distance: 0.1

═══════════ Width ═══════════
Width: 0.3 → 0.05  (кривая от широкого к узкому)

═══════════ Color ═══════════
Color: Gradient (от яркого к прозрачному)
  - Start: Белый (255, 255, 255, 255)
  - Mid (50%): Фиолетовый/Голубой (яркий)
  - End: Прозрачный (0, 0, 0, 0)

═══════════ Materials ═══════════
Material: Default-Particle (или создайте свой)
```

**Способ 2: Скопировать с CelestialBall**

1. Откройте `CelestialBallProjectile.prefab`
2. Скопируйте настройки Trail Renderer
3. Вставьте в EtherealSkullProjectile

3. **Перетащите** Trail Renderer компонент в поле `Trail` скрипта

---

### ШАГ 9: Настроить Material (Эмиссия)

Чтобы череп светился:

1. **Выберите дочерний объект** с Mesh Renderer (модель черепа)
2. **В Inspector → Materials** найдите материал
3. **Если материал не светится:**
   - Создайте новый материал `EtherealSkullMaterial`
   - Shader: `Universal Render Pipeline/Lit` или `Standard`
   - **Включите Emission:**
     ```
     Emission: ✅
     Emission Color: Фиолетовый/Голубой (яркий)
     Emission Intensity: 2-3
     ```
4. **Назначьте** материал на Mesh Renderer

---

### ШАГ 10: Настроить Layer

1. **Выберите корневой объект**
2. **В Inspector → Layer:**
   ```
   Layer: Projectile
   ```

3. **Если слоя "Projectile" нет:**
   - Edit → Project Settings → Tags and Layers
   - Добавьте новый Layer: `Projectile`

---

### ШАГ 11: Сохранить и протестировать

1. **Сохраните префаб** (Ctrl+S или File → Save)
2. **Закройте Prefab Mode** (стрелка ← вверху Hierarchy)

**Тестирование:**

1. **Создайте тестовую сцену** или используйте Arena
2. **Перетащите** `EtherealSkullProjectile` на сцену
3. **В скрипте PlayerAttackNew** временно замените projectilePrefab:
   ```csharp
   // В BasicAttackConfig_Mage.asset
   Projectile Prefab: EtherealSkullProjectile
   ```
4. **Запустите игру** и атакуйте (ЛКМ)
5. **Проверьте:**
   - ✅ Череп летит вперед
   - ✅ Светится (Point Light работает)
   - ✅ Оставляет след (Trail Renderer)
   - ✅ Автонаведение на врага (если включено)
   - ✅ Взрывается при попадании (Hit Effect)

---

## 🎨 ВИЗУАЛЬНЫЕ НАСТРОЙКИ

### Рекомендуемые цветовые схемы:

#### 1. Темная Магия (Necromancy):
```
Light Color: Фиолетовый (155, 77, 202)
Trail Color Gradient:
  - Start: Белый (255, 255, 255)
  - Mid: Фиолетовый (155, 77, 202)
  - End: Прозрачный (0, 0, 0, 0)
Emission Color: Темно-фиолетовый
Hit Effect: Темный взрыв с черепами
```

#### 2. Ледяная Магия:
```
Light Color: Голубой (0, 200, 255)
Trail Color Gradient:
  - Start: Белый (255, 255, 255)
  - Mid: Голубой (0, 200, 255)
  - End: Прозрачный (0, 0, 0, 0)
Emission Color: Ледяной голубой
Hit Effect: Ледяной взрыв
```

#### 3. Токсичная Магия:
```
Light Color: Зеленый (0, 255, 100)
Trail Color Gradient:
  - Start: Белый (255, 255, 255)
  - Mid: Зеленый (0, 255, 100)
  - End: Прозрачный (0, 0, 0, 0)
Emission Color: Ядовито-зеленый
Hit Effect: Токсичный взрыв
```

---

## 🔧 ЧАСТЫЕ ПРОБЛЕМЫ

### Проблема 1: Череп не светится

**Решение:**
1. Проверьте что Point Light добавлен и `Enabled = true`
2. Проверьте `Intensity` (должен быть 2-3)
3. Проверьте что `Range` достаточно большой (5-10)
4. Добавьте Emission на материал модели

### Проблема 2: След не появляется

**Решение:**
1. Проверьте что Trail Renderer добавлен
2. Проверьте `Time` (должно быть > 0.3)
3. Проверьте `Min Vertex Distance` (0.1)
4. Проверьте что Material назначен
5. Проверьте `Color Gradient` - не весь прозрачный?

### Проблема 3: Череп не попадает во врагов

**Решение:**
1. Проверьте Sphere Collider:
   - `Is Trigger = true`
   - `Radius = 0.4` (или больше)
2. Проверьте Layer: должен быть `Projectile`
3. Проверьте что у врага есть коллайдер
4. Проверьте Physics Matrix: `Projectile` должен взаимодействовать с `Character`

### Проблема 4: Череп слишком большой/маленький

**Решение:**
1. Измените `Scale` корневого объекта:
   - Больше: `(30, 30, 30)`
   - Меньше: `(10, 10, 10)`
2. Настройте `Sphere Collider Radius` пропорционально

### Проблема 5: Череп вращается неправильно

**Решение:**
1. Проверьте `Rotation Speed` в CelestialProjectile (360)
2. Проверьте `Visual Transform` - должен быть дочерний Model
3. Измените начальную ротацию модели (0, 90, 0) если смотрит не туда

---

## 📦 ФИНАЛЬНАЯ СТРУКТУРА ПРЕФАБА

```
EtherealSkullProjectile (Root)
├── Components:
│   ├── Transform (Scale: 20, 20, 20)
│   ├── Sphere Collider (Trigger, Radius: 0.4)
│   ├── Rigidbody (Kinematic, No Gravity)
│   ├── CelestialProjectile (Script)
│   ├── Light (Point, Color: Purple/Blue, Intensity: 2, Range: 5)
│   └── Trail Renderer (Time: 0.5, Width curve, Color gradient)
│
└── Ethereal_Skull_1020210937_texture (Child Model)
    ├── Transform (может иметь Rotation для ориентации)
    └── Mesh Renderer (Material с Emission)
```

---

## 🎯 ИНТЕГРАЦИЯ С BASICATTACKCONFIG

После создания префаба, назначьте его в BasicAttackConfig:

### Для нового класса Necromancer:

1. **Создайте** `BasicAttackConfig_Necromancer.asset`
2. **Настройте параметры:**
   ```
   Character Class: Necromancer (или другой класс)
   Attack Type: Ranged
   Base Damage: 35
   Intelligence Scaling: 1.5
   Attack Range: 30
   Projectile Prefab: EtherealSkullProjectile ← ЗДЕСЬ!
   Projectile Speed: 15
   Homing: true
   ```

### Для существующего класса (например, Rogue):

1. **Откройте** `BasicAttackConfig_Rogue.asset`
2. **Замените:**
   ```
   Projectile Prefab: EtherealSkullProjectile
   ```
3. **Измените описание:**
   ```
   Description: "Метание проклятых черепов"
   ```

---

## ✅ ЧЕКЛИСТ ФИНАЛЬНОЙ ПРОВЕРКИ

Перед использованием убедитесь:

- [ ] ✅ Sphere Collider добавлен (Is Trigger = true)
- [ ] ✅ Rigidbody добавлен (Kinematic = true, Gravity = false)
- [ ] ✅ CelestialProjectile скрипт добавлен
- [ ] ✅ Point Light добавлен и светится
- [ ] ✅ Trail Renderer добавлен и работает
- [ ] ✅ Hit Effect назначен (взрыв при попадании)
- [ ] ✅ Material модели имеет Emission (свечение)
- [ ] ✅ Layer установлен в "Projectile"
- [ ] ✅ Scale правильный (20, 20, 20 или по вкусу)
- [ ] ✅ Префаб сохранен в `Assets/Prefabs/Projectiles/`
- [ ] ✅ Префаб также скопирован в `Assets/Resources/Projectiles/` (для динамической загрузки)
- [ ] ✅ Протестирован в игре (летит, светится, попадает, взрывается)

---

## 🚀 ДОПОЛНИТЕЛЬНЫЕ УЛУЧШЕНИЯ

### Частицы вокруг черепа (опционально):

1. **Создайте дочерний объект** `Particles`
2. **Add Component → Particle System**
3. **Настройте:**
   ```
   Start Lifetime: 1
   Start Speed: 1
   Start Size: 0.1
   Emission Rate: 20
   Shape: Sphere, Radius: 0.5
   Color: Фиолетовый/Зеленый (под тему черепа)
   ```

### Звуки (опционально):

1. **Add Component → Audio Source**
2. **Назначьте:**
   ```
   Launch Sound: Звук запуска (свист, магический звук)
   Hit Sound: Звук взрыва
   ```
3. **В CelestialProjectile скрипте** назначьте эти клипы

---

**Готово!** 💀✨

Теперь у вас есть полноценный снаряд Ethereal Skull с:
- Свечением
- Хвостом
- Автонаведением
- Эффектом попадания
- Вращением

Используйте его в любом BasicAttackConfig для создания уникальных атак!

---

**Автор:** Claude (Anthropic)
**Дата:** 21 октября 2025
