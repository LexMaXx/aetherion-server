# 🔥 Настройка Fireball префаба с эффектами

## ✅ Автоматическая настройка (РЕКОМЕНДУЕТСЯ)

### Шаг 1: Запустить автоматическую настройку

```
Unity → Tools → Skills → Setup Fireball Prefab Effects
```

**Что произойдёт автоматически:**

1. ✅ **CelestialProjectile** - скрипт снаряда (тот же что у базовой атаки)
2. ✅ **Rigidbody + SphereCollider** - физика и триггер попадания
3. ✅ **TrailRenderer** - огненный хвост (оранжево-красный градиент)
4. ✅ **Point Light** - оранжевое свечение (intensity 2, range 5)
5. ✅ **Particle System** - огненные частицы вокруг снаряда
6. ✅ **Layer = Projectile (7)** - правильный слой для коллизий

---

### Шаг 2: Обновить SkillConfig

После настройки префаба, обновите Mage_Fireball.asset:

1. Откройте `Assets/ScriptableObjects/Skills/Mage/Mage_Fireball.asset`
2. Найдите поле **Projectile Prefab**
3. Перетащите **FireballProjectile** из `Assets/Prefabs/Projectiles/`

---

### Шаг 3: Протестировать

1. Запустите SkillTestScene (Play ▶️)
2. Выберите врага (ЛКМ)
3. Используйте Fireball (клавиша 1) 🔥

**Ожидаемый результат:**
- ✅ Огненный шар с хвостом
- ✅ Оранжевое свечение
- ✅ Огненные частицы
- ✅ Плавный полёт к цели
- ✅ Взрыв при попадании

---

## 📊 Что добавляется к префабу:

### 1. TrailRenderer (Огненный хвост)
```
Time: 0.5 секунды
Start Width: 0.5
End Width: 0.1
Gradient: Оранжевый → Красный → Прозрачный
```

### 2. Point Light (Свечение)
```
Type: Point
Color: RGB(255, 102, 0) - Оранжевый
Intensity: 2
Range: 5 метров
```

### 3. Particle System (Огненные частицы)
```
Emission: 20 частиц/сек
Lifetime: 0.5 секунды
Start Size: 0.2
Color: Оранжевый → Красный (градиент)
Shape: Sphere (радиус 0.3)
```

### 4. Компоненты физики
```
Rigidbody:
  - useGravity: false
  - isKinematic: true

SphereCollider:
  - isTrigger: true
  - radius: 0.5
```

---

## 🎨 Визуальное сравнение:

### До настройки:
```
Fireball
└─ Только модель (без эффектов)
```

### После настройки:
```
Fireball
├─ Model (визуал)
├─ TrailRenderer (огненный хвост) ⭐
├─ Light (оранжевое свечение) ⭐
├─ Particles (огненные частицы) ⭐
├─ CelestialProjectile (скрипт)
├─ Rigidbody (физика)
└─ SphereCollider (триггер)
```

---

## 🔧 Ручная настройка (если нужно)

Если автоматическая настройка не работает, можно настроить вручную:

### 1. Откройте префаб
- `Assets/Prefabs/Projectiles/FireballProjectile.prefab`

### 2. Добавьте компоненты
- Add Component → CelestialProjectile
- Add Component → Rigidbody (отключите Gravity)
- Add Component → Sphere Collider (включите Is Trigger)
- Add Component → Trail Renderer
- Create Empty Child → Add Component → Light
- Create Empty Child → Add Component → Particle System

### 3. Настройте Trail Renderer
- Time: 0.5
- Start Width: 0.5
- End Width: 0.1
- Color Gradient: Оранжевый → Красный → Прозрачный

### 4. Настройте Light
- Type: Point
- Color: Оранжевый
- Intensity: 2
- Range: 5

### 5. Настройте Particle System
- Duration: 5, Loop: true
- Start Lifetime: 0.5
- Start Speed: 1
- Start Size: 0.2
- Start Color: Оранжевый
- Emission Rate: 20
- Shape: Sphere, Radius: 0.3

---

## 🐛 Возможные проблемы:

### Проблема: "Layer 'Projectile' не найден"
**Решение:**
1. Project Settings → Tags and Layers
2. Layers → User Layer 7 = "Projectile"

### Проблема: Хвост не отображается
**Решение:**
- Проверьте, что Trail Renderer включен
- Проверьте материал Trail Renderer (должен быть Particle/Unlit)

### Проблема: Свет не виден
**Решение:**
- Проверьте Intensity (должно быть 2 или выше)
- Проверьте Range (должно быть 5 или выше)

### Проблема: Частицы не видны
**Решение:**
- Проверьте, что Particle System играет (Loop = true)
- Проверьте Emission Rate (должно быть 20)

---

## 🚀 Следующие шаги:

После успешной настройки Fireball:

1. ✅ Создать другие префабы:
   - IceShardProjectile (синий + холодный trail)
   - LightningBallProjectile (жёлтый + электрический эффект)

2. ✅ Создать больше скиллов:
   - Ice Nova (AOE)
   - Lightning Strike (Instant + Stun)
   - Meteor (Ground Target)

3. ✅ Добавить звуки:
   - Cast sound (свист при запуске)
   - Flight sound (шипение в полёте)
   - Hit sound (взрыв при попадании)

---

**🎮 ГОТОВО! Запускайте Unity и настраивайте Fireball!**

**Команда:** Tools → Skills → Setup Fireball Prefab Effects
