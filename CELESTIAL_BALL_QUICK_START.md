# ⚡ БЫСТРЫЙ СТАРТ: Celestial Ball для мага

**Время настройки:** ~10 минут

---

## ✅ ЧТО УЖЕ СДЕЛАНО

- ✅ Создан скрипт `CelestialProjectile.cs` с автонаведением
- ✅ Обновлен `PlayerAttack.cs` для поддержки нового снаряда
- ✅ Все готово к настройке в Unity Editor

---

## 🎯 ЧТО ДЕЛАТЬ (5 шагов)

### 1. Открыть префаб в Unity

```
Project → Assets/Prefabs/Projectiles/Celestial_Swirl_ball_1019215156_texture.prefab
```

Дважды кликните для открытия в Prefab Mode.

---

### 2. Переименовать и настроить Root

**Переименовать:** `CelestialBallProjectile`

**Добавить компоненты:**

1. **Sphere Collider:**
   - Is Trigger: ✅
   - Radius: `0.5`

2. **Rigidbody:**
   - Use Gravity: ❌
   - Is Kinematic: ✅

3. **CelestialProjectile Script:**
   - Перетащите скрипт из `Assets/Scripts/Player/CelestialProjectile.cs`

---

### 3. Добавить эффекты

#### A) Trail Renderer

1. Create Empty child: `Trail`
2. Add Component → Trail Renderer
3. Настройки:
   ```
   Time: 0.5
   Width: 0.3 → 0.0
   Color: Голубой (100,200,255) → Прозрачный
   ```

#### B) Point Light

1. GameObject → Light → Point Light
2. Настройки:
   ```
   Color: Голубой (150,200,255)
   Range: 5
   Intensity: 2
   ```

#### C) Wind Effect (опционально)

1. GameObject → Effects → Particle System
2. Назовите: `WindEffect`
3. Базовые настройки:
   ```
   Lifetime: 0.5
   Speed: 2
   Size: 0.2
   Emission Rate: 20
   Shape: Sphere (radius 0.3)
   ```

---

### 4. Настроить CelestialProjectile Script

В Inspector (Root объект):

#### Movement:
```
Base Speed: 15
Homing Speed: 8
Lifetime: 5
Acceleration Rate: 1.5
```

#### Homing:
```
Enable Homing: ✅
Homing Start Delay: 0.1
Homing Radius: 30
Target Acquisition Angle: 90
```

#### Visual Effects:
```
Hit Effect: Перетащите "CFXR3 Fire Explosion B"
           (из Assets/Resources/Effects/)
```

**Остальное заполнится автоматически!**

#### Rotation:
```
Rotation Speed: 360
Visual Transform: Перетащите Celestial_Swirl_ball_1019215156_texture
```

---

### 5. Сохранить и обновить настройки

**A) Сохранить префаб:**
- File → Save (Ctrl+S)
- Выйти из Prefab Mode

**B) Скопировать в Resources:**
```
1. Найдите префаб: Assets/Prefabs/Projectiles/CelestialBallProjectile.prefab
2. Ctrl+C, Ctrl+V (создать копию)
3. Перетащите копию в: Assets/Resources/Projectiles/
```

**C) Обновить CharacterAttackSettings:**
```
1. Project → Assets/Resources/CharacterAttackSettings.asset
2. Найти секцию "Mage"
3. Изменить "Projectile Prefab Name": "CelestialBallProjectile"
4. Ctrl+S (сохранить)
```

---

## 🎮 ТЕСТИРОВАНИЕ

1. **Play** в Unity
2. Выбрать **Mage**
3. Войти в арену
4. **ЛКМ** по врагу

**Проверить:**
- ✅ Celestial Ball летит
- ✅ Вращается
- ✅ Светится (Light)
- ✅ Оставляет след (Trail)
- ✅ **ПОВОРАЧИВАЕТ к цели** (автонаведение!)
- ✅ Взрыв при попадании
- ✅ Урон наносится

---

## 📋 ЧЕКЛИСТ

Перед тестом:

- [ ] Префаб переименован в `CelestialBallProjectile`
- [ ] Добавлен Sphere Collider (is Trigger ✅)
- [ ] Добавлен Rigidbody (is Kinematic ✅)
- [ ] Добавлен CelestialProjectile Script
- [ ] Добавлен Trail Renderer
- [ ] Добавлен Point Light
- [ ] Назначен Hit Effect (CFXR3 Fire Explosion B)
- [ ] Enable Homing ✅
- [ ] Префаб скопирован в Resources/Projectiles/
- [ ] CharacterAttackSettings обновлен (Mage → CelestialBallProjectile)
- [ ] Протестировано в игре

---

## 🔥 ФИШКИ

### Что делает Celestial Ball особенным:

1. **Умное автонаведение:**
   - Ищет цель в радиусе 30m
   - Если цель умерла → находит новую
   - Не разворачивается на 180° (угол 90°)

2. **Ускорение полета:**
   - Начинает с 15 m/s
   - Ускоряется до ~20+ m/s

3. **Красивая визуализация:**
   - Двойное вращение (2 оси)
   - Свечение
   - След
   - Эффект ветра

4. **Взрыв при попадании:**
   - CFXR3 Fire Explosion B
   - Синхронизация в мультиплеере

---

## ⚠️ TROUBLESHOOTING

**Снаряд не летит:**
- Проверьте Base Speed > 0
- Rigidbody: Is Kinematic ✅

**Нет автонаведения:**
- Enable Homing ✅
- Homing Radius > 0

**Нет взрыва:**
- Назначьте Hit Effect

**Не видно в мультиплеере:**
- Проверьте SocketIOManager подключен

---

## 📚 ДОПОЛНИТЕЛЬНО

**Подробная инструкция:** [CELESTIAL_BALL_SETUP_GUIDE.md](CELESTIAL_BALL_SETUP_GUIDE.md)

**Технические детали:**
- Скрипт: `Assets/Scripts/Player/CelestialProjectile.cs`
- Обновлен: `Assets/Scripts/Player/PlayerAttack.cs`

---

**Готово! Маг теперь стреляет космическими шарами с автонаведением! 🌟✨**
