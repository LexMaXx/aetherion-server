# 🌟 ИНСТРУКЦИЯ: Настройка Celestial Ball для атаки мага

**Дата:** 20 октября 2025
**Задача:** Заменить FireballProjectile на Celestial Swirl Ball с автонаведением, эффектами и взрывом

---

## 📋 ЧТО СДЕЛАНО

✅ Создан скрипт `CelestialProjectile.cs` с:
- Автонаведением на ближайшего врага
- Ускорением полета
- Плавным поворотом к цели (как управляемая ракета)
- Поддержкой Light и Trail Renderer
- Эффектом взрыва при попадании
- Синхронизацией для мультиплеера
- Поиском новой цели если текущая умерла

---

## 🎨 ШАГ 1: Настройка префаба в Unity Editor

### 1.1 Открыть существующий префаб

1. Откройте Unity Editor
2. В Project window найдите: `Assets/Prefabs/Projectiles/Celestial_Swirl_ball_1019215156_texture.prefab`
3. Дважды кликните чтобы открыть Prefab Mode

### 1.2 Структура префаба

Создайте следующую иерархию:

```
CelestialBallProjectile (Root)
├── Celestial_Swirl_ball_1019215156_texture (Model - визуал)
├── Trail (TrailRenderer)
├── Light (Point Light)
├── WindEffect (Particle System - опционально)
└── HitEffect (НЕ добавляется, только ссылка в скрипте!)
```

### 1.3 Настройка Root объекта (CelestialBallProjectile)

1. **Переименуйте** префаб в `CelestialBallProjectile`
2. **Добавьте компоненты:**

   **A) Sphere Collider:**
   - Is Trigger: ✅ (включить!)
   - Radius: `0.5`

   **B) Rigidbody:**
   - Use Gravity: ❌ (отключить!)
   - Is Kinematic: ✅ (включить!)

   **C) CelestialProjectile Script:**
   - Перетащите скрипт `CelestialProjectile.cs` на объект

### 1.4 Настройка модели (Visual)

1. Выберите дочерний объект `Celestial_Swirl_ball_1019215156_texture`
2. Это будет вращаться - скрипт найдет его автоматически

### 1.5 Добавить Trail Renderer

1. **Create Empty** child объект: `Trail`
2. **Add Component → Trail Renderer**
3. **Настройки Trail Renderer:**

   ```
   Time: 0.5
   Min Vertex Distance: 0.1
   Width:
     - Start: 0.3
     - End: 0.0 (сужается к концу)

   Color Gradient:
     - Start: Яркий голубой/фиолетовый (RGB: 100, 200, 255, A: 255)
     - End: Прозрачный (A: 0)

   Material:
     - Создайте новый Material или используйте Default-Particle
     - Shader: Particles/Standard Unlit
   ```

### 1.6 Добавить Point Light (свечение)

1. **GameObject → Light → Point Light** как child
2. **Настройки Light:**

   ```
   Color: Голубой/фиолетовый (RGB: 150, 200, 255)
   Range: 5
   Intensity: 2
   Render Mode: Important
   ```

### 1.7 Добавить Wind Effect (опционально)

**Вариант A: Создать простой Particle System**

1. **GameObject → Effects → Particle System** как child
2. Назовите: `WindEffect`
3. **Настройки:**

   ```
   Start Lifetime: 0.5
   Start Speed: 2
   Start Size: 0.2

   Emission:
     Rate over Time: 20

   Shape:
     Shape: Sphere
     Radius: 0.3

   Color over Lifetime:
     Start: Белый прозрачный (A: 100)
     End: Прозрачный (A: 0)

   Size over Lifetime:
     Start: 1
     End: 0
   ```

**Вариант B: Использовать готовый эффект**

1. Найдите в проекте подходящий эффект ветра/энергии
2. Перетащите как child объект `WindEffect`

### 1.8 Настроить CelestialProjectile Script

В Inspector выберите Root объект и настройте:

#### Movement Settings:
```
Base Speed: 15
Homing Speed: 8
Lifetime: 5
Acceleration Rate: 1.5
```

#### Homing Settings:
```
Enable Homing: ✅ (включить!)
Homing Start Delay: 0.1
Homing Radius: 30
Target Acquisition Angle: 90
```

#### Visual Effects:
```
Hit Effect: Перетащите "CFXR3 Fire Explosion B" из Assets/Resources/Effects/
Wind Effect: (автоматически найдет или назначьте вручную)
Projectile Light: (автоматически найдет или перетащите Light объект)
Trail: (автоматически найдет или перетащите Trail объект)
```

#### Rotation:
```
Rotation Speed: 360
Visual Transform: Перетащите Celestial_Swirl_ball_1019215156_texture
```

### 1.9 Сохранить префаб

1. Нажмите **File → Save** или Ctrl+S
2. Выйдите из Prefab Mode

---

## 🔧 ШАГ 2: Обновить CharacterAttackSettings

### 2.1 Найти настройки

1. В Project window: `Assets/Resources/CharacterAttackSettings.asset`
2. Откройте в Inspector

### 2.2 Изменить настройки мага

Найдите секцию **Mage** и измените:

```
Attack Animation Speed: 1.5 (ускорить анимацию)
Attack Hit Timing: 0.6 (момент выстрела)
Attack Damage: 30
Attack Range: 25 (дальность)
Attack Cooldown: 1.0
Is Ranged Attack: ✅ (включить!)
Projectile Speed: 15
Projectile Prefab Name: "CelestialBallProjectile"  <-- ВАЖНО!
```

### 2.3 Сохранить

1. Ctrl+S для сохранения

---

## 📦 ШАГ 3: Убедиться что префаб в Resources

### Переместить префаб (если еще не там)

Префаб должен быть в одной из папок:
- `Assets/Resources/Projectiles/CelestialBallProjectile.prefab`
- `Assets/Prefabs/Projectiles/CelestialBallProjectile.prefab` (и копия в Resources)

**Рекомендация:** Создайте копию в `Assets/Resources/Projectiles/`

1. В Project window:
2. Найдите `Assets/Prefabs/Projectiles/CelestialBallProjectile.prefab`
3. Ctrl+C, Ctrl+V
4. Перетащите копию в `Assets/Resources/Projectiles/`

---

## 🎮 ШАГ 4: Тестирование в игре

### 4.1 Запустить игру

1. **Play** в Unity Editor
2. Выберите **Mage** (маг)
3. Войдите в арену

### 4.2 Протестировать атаку

1. **ЛКМ** по врагу для атаки
2. **Проверить:**
   - ✅ Celestial Ball летит к врагу
   - ✅ Сфера вращается
   - ✅ Есть свечение (Light)
   - ✅ Есть след (Trail)
   - ✅ Есть эффект ветра вокруг сферы
   - ✅ Снаряд ПОВОРАЧИВАЕТ к цели (автонаведение)
   - ✅ При попадании - взрыв (CFXR3 Fire Explosion B)
   - ✅ Урон наносится врагу

### 4.3 Проверить в Console

Должны быть логи:
```
[CelestialProjectile] ✨ Создан! Target: EnemyName, Damage: 30, Homing: True
[CelestialProjectile] 🎯 Новая цель найдена: EnemyName2 на расстоянии 15.3m
[CelestialProjectile] 💥 Попадание в EnemyName! Урон: 30
[CelestialProjectile] ✅ Урон нанесен NPC: 30
[CelestialProjectile] 💥 Эффект взрыва создан: CFXR3 Fire Explosion B
```

### 4.4 Протестировать автонаведение

1. Запустите снаряд в одного врага
2. **Быстро убейте цель** (другим способом)
3. **Снаряд должен найти НОВУЮ цель** и повернуть к ней!

---

## 🌐 ШАГ 5: Мультиплеер (опционально)

### Проверить синхронизацию

1. Запустите 2 клиента
2. Игрок A (маг) атакует
3. **Игрок B должен видеть:**
   - Celestial Ball летит
   - Взрыв при попадании
   - Урон отображается

---

## 🎨 ДОПОЛНИТЕЛЬНЫЕ УЛУЧШЕНИЯ (опционально)

### Вариант 1: Улучшенный Trail

Создайте **Gradient** для Trail Renderer:

```
Position 0%: RGB(100, 200, 255, 255) - яркий голубой
Position 50%: RGB(150, 100, 255, 200) - фиолетовый
Position 100%: RGB(0, 0, 0, 0) - прозрачный
```

### Вариант 2: Pulsating Light (пульсирующий свет)

Добавьте скрипт `LightPulse.cs`:

```csharp
using UnityEngine;

public class LightPulse : MonoBehaviour
{
    public float minIntensity = 1.5f;
    public float maxIntensity = 2.5f;
    public float speed = 3f;

    private Light lightComponent;

    void Start()
    {
        lightComponent = GetComponent<Light>();
    }

    void Update()
    {
        float intensity = Mathf.Lerp(minIntensity, maxIntensity,
            (Mathf.Sin(Time.time * speed) + 1f) / 2f);
        lightComponent.intensity = intensity;
    }
}
```

### Вариант 3: Добавить звуки

1. Найдите звуки:
   - Launch sound (свист при запуске)
   - Hit sound (взрыв при попадании)

2. Назначьте в CelestialProjectile Script:
   - Launch Sound: drag & drop audio clip
   - Hit Sound: drag & drop audio clip

3. Добавьте **Audio Source** компонент к префабу:
   - Play On Awake: ❌ (отключить!)
   - Spatial Blend: 1.0 (3D звук)

---

## 📊 ТЕХНИЧЕСКИЕ ДЕТАЛИ

### Как работает автонаведение:

```
1. Снаряд летит в начальном направлении 0.1s
2. Затем начинается homing:
   - Каждый кадр проверяет цель
   - Если цель мертва → ищет новую в радиусе 30m
   - Плавно поворачивает к цели (8 рад/с)
   - Не разворачивается больше чем на 90°
3. При достижении цели (<1m):
   - Наносит урон
   - Создает взрыв
   - Синхронизирует в мультиплеере
   - Уничтожается
```

### Почему лучше чем Projectile.cs:

| Функция | Projectile.cs | CelestialProjectile.cs |
|---------|---------------|------------------------|
| Автонаведение | Простое | Умное (ищет новую цель) |
| Ускорение | Нет | Да (1.5x) |
| Поиск цели | Только заданная | Радиус 30m |
| Вращение | Одна ось | Две оси (интереснее) |
| Эффекты | Базовые | Расширенные |
| Wind effect | Нет | Да |

---

## ⚠️ TROUBLESHOOTING

### Проблема 1: Снаряд не летит
**Решение:**
- Проверьте Rigidbody: Is Kinematic должен быть ✅
- Проверьте что Base Speed > 0

### Проблема 2: Нет автонаведения
**Решение:**
- Enable Homing должен быть ✅
- Убедитесь что у врага есть коллайдер
- Проверьте слой врага (Layer Mask)

### Проблема 3: Нет эффекта взрыва
**Решение:**
- Назначьте Hit Effect в Inspector
- Путь: `Assets/Resources/Effects/CFXR3 Fire Explosion B.prefab`

### Проблема 4: Снаряд не наносит урон
**Решение:**
- Проверьте что Collider is Trigger ✅
- Проверьте метод Initialize() вызывается
- Проверьте что damage > 0

### Проблема 5: Не видно в мультиплеере
**Решение:**
- Убедитесь что SocketIOManager.Instance не null
- Проверьте логи: должно быть "📡 Эффект взрыва отправлен на сервер"

---

## 📝 ИТОГОВЫЙ ЧЕКЛИСТ

Перед финальным тестом проверьте:

- [ ] Префаб создан: `CelestialBallProjectile.prefab`
- [ ] Добавлены компоненты: Sphere Collider, Rigidbody
- [ ] Добавлен скрипт: CelestialProjectile.cs
- [ ] Настроен Trail Renderer
- [ ] Добавлен Point Light
- [ ] Добавлен Wind Effect (опционально)
- [ ] Назначен Hit Effect (CFXR3 Fire Explosion B)
- [ ] CharacterAttackSettings обновлен (Mage → CelestialBallProjectile)
- [ ] Префаб в Resources/Projectiles/
- [ ] Протестировано в игре
- [ ] Проверено автонаведение
- [ ] Проверен взрыв при попадании

---

## 🎉 РЕЗУЛЬТАТ

После выполнения всех шагов:

✅ Маг стреляет красивым Celestial Swirl Ball
✅ Снаряд светится и оставляет след
✅ Автоматически наводится на врагов
✅ Ищет новую цель если текущая умерла
✅ Взрывается при попадании (эффектный взрыв)
✅ Работает в мультиплеере
✅ Эффект ветра вокруг сферы (опционально)

---

**Приятной игры! 🎮✨**
