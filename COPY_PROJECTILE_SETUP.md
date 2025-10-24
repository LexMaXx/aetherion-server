# 🚀 Копирование настроек снаряда

## ✅ БЫСТРОЕ РЕШЕНИЕ

Чтобы Fireball летел как CelestialBall, нужно скопировать ВСЕ компоненты:

### Одна команда:
```
Unity → Tools → Skills → Copy CelestialBall Setup to Fireball
```

**Готово!** ✅

---

## 📋 Что будет скопировано:

### 1. CelestialProjectile (скрипт полёта)
- Все настройки скрипта
- Логика полёта и хоминга
- Обработка попаданий

### 2. Rigidbody (физика)
- useGravity: false
- isKinematic: true
- Все остальные параметры

### 3. SphereCollider (триггер попадания)
- isTrigger: true
- radius: (как у CelestialBall)
- center: (как у CelestialBall)

### 4. TrailRenderer (хвост)
- Время следа
- Ширина
- Цвет и градиент
- Материал

### 5. Light (свечение)
- Тип света
- Цвет
- Интенсивность
- Радиус

### 6. Particle System (частицы)
- Все модули
- Emission, Shape, Size, Color
- Материал частиц

### 7. Layer
- Projectile (Layer 7)

---

## 🎯 Использование:

### Шаг 1: Скопировать настройки
```
Tools → Skills → Copy CelestialBall Setup to Fireball
```

### Шаг 2: Обновить SkillConfig
1. Откройте `Mage_Fireball.asset`
2. **Projectile Prefab** → перетащите **FireballProjectile**

### Шаг 3: Тест
1. **Play** ▶️
2. **ЛКМ** - выбрать врага
3. **1** - Fireball 🔥

**Результат:** Fireball летит точно так же как базовая атака!

---

## 🔧 Что делает скрипт:

```
CelestialBallProjectile     →     FireballProjectile
├─ CelestialProjectile      →     ✅ Скопировано
├─ Rigidbody               →     ✅ Скопировано
├─ SphereCollider          →     ✅ Скопировано
├─ TrailRenderer           →     ✅ Скопировано
├─ Light                   →     ✅ Скопировано
├─ Particle System         →     ✅ Скопировано
└─ Layer: Projectile (7)   →     ✅ Скопировано
```

---

## ✅ Преимущества:

- ✅ **Быстро** - одна команда
- ✅ **Точно** - копирует ВСЁ
- ✅ **Безопасно** - не трогает исходный префаб
- ✅ **Полностью** - включая дочерние объекты

---

## 🎨 После копирования можно изменить:

Если хотите изменить **только визуал** (не трогая полёт):

### Цвет хвоста:
1. Выберите FireballProjectile
2. TrailRenderer → Color
3. Измените градиент (например, красный вместо синего)

### Цвет света:
1. Выберите FireballProjectile → Light (child)
2. Light → Color
3. Измените цвет (например, оранжевый)

### Цвет частиц:
1. Выберите FireballProjectile → Particles (child)
2. Particle System → Start Color
3. Измените цвет

---

## 🐛 Возможные проблемы:

### "CelestialBallProjectile не найден"
**Решение:** Проверьте путь: `Assets/Prefabs/Projectiles/CelestialBallProjectile.prefab`

### "Fireball не найден"
**Решение:** Проверьте путь: `Assets/Prefabs/Projectiles/Fireball.prefab`

### Fireball всё равно не летит
**Решение:**
1. Проверьте Console на ошибки
2. Убедитесь, что скрипт выполнился успешно
3. Перезапустите Unity Editor

---

**🔥 ГОТОВО! Одна команда и Fireball летит как CelestialBall!**

**Команда:** `Tools → Skills → Copy CelestialBall Setup to Fireball`
