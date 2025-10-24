# ⚒️ HAMMER OF JUSTICE ГОТОВ!

## ✅ ЧТО СДЕЛАНО

### Обновлён HammerProjectile.prefab

**Местоположение:** `Assets/Prefabs/Projectiles/HammerProjectile.prefab`

**Изменения:**
- ❌ **Удалён:** Простой жёлтый Cube
- ✅ **Добавлен:** Golden_Forge (3D модель молота)
- ✅ **Scale:** 20x20x20 (большой и хорошо видимый!)

**Структура:**
```
HammerProjectile
├── Projectile (Script)
│   ├── speed: 22
│   ├── lifetime: 3
│   ├── homing: true (самонаведение)
│   └── rotationSpeed: 540
│
├── SphereCollider
│   ├── isTrigger: true
│   └── radius: 0.5
│
├── Rigidbody
│   ├── useGravity: false
│   └── isKinematic: true
│
├── Light
│   ├── type: Point
│   ├── color: RGB(1, 0.9, 0.5) ← Золотистый!
│   ├── intensity: 4
│   └── range: 5
│
├── TrailRenderer ⭐
│   ├── width: 0.3 → 0.05
│   ├── time: 0.5
│   └── color: Золотистый градиент
│
├── HammerVisual (Child) ⭐ НОВЫЙ!
│   └── Golden_Forge (Scale: 20, 20, 20)
│
└── CFXR3 LightGlow (Particle Effect)
    └── Scale: 0.5
```

---

## 🎯 ХАРАКТЕРИСТИКИ

**Hammer of Justice:**
- **Урон:** 35 + (STR × 12)
- **Эффект:** Оглушение на 3 секунды
- **Мана:** 35
- **Кулдаун:** 12 секунд
- **Дальность:** 20 метров
- **Cast Time:** 0.3 секунды
- **Класс:** Paladin

**Projectile:**
- **Скорость:** 22 м/с (быстрее чем Fireball!)
- **Lifetime:** 3 секунды
- **Самонаведение:** Да
- **Вращение:** 540°/сек (быстрое!)
- **След:** Золотистый trail

---

## 🎮 КАК ТЕСТИРОВАТЬ

1. **Открыть Unity**
2. **Запустить игру** (Play)
3. **Выбрать Paladin**
4. **Использовать Hammer of Justice**

### ✅ Ожидаемый результат:

```
✅ Золотой молот появляется
✅ Молот светится золотистым светом
✅ Молот вращается быстро (540°/сек)
✅ Золотистый след за молотом
✅ Particle effect (свечение)
✅ Летит к врагу
✅ Самонаведение
✅ Попадает в цель
✅ Наносит урон
✅ Оглушает на 3 секунды
```

---

## 🔨 ВИЗУАЛЬНЫЕ ЭФФЕКТЫ

1. **Golden_Forge модель**
   - 3D модель молота
   - Золотистая текстура
   - Scale: 20x (большой и заметный)
   - Вращается во время полёта

2. **Золотистое свечение**
   - Point Light, Intensity: 4
   - RGB(1, 0.9, 0.5) - тёплый золотой
   - Range: 5 метров

3. **Trail Renderer**
   - Золотистый градиент
   - Width: 0.3 → 0.05
   - Time: 0.5 сек

4. **Particle Effect**
   - CFXR3 LightGlow (Loop)
   - Золотистое свечение
   - Scale: 0.5

---

## 🎨 СРАВНЕНИЕ

| | **Fireball** | **Lightning Ball** | **Hammer of Justice** |
|---|---|---|---|
| **Визуал** | Огонь | Энергия | Молот ⚒️ |
| **Цвет** | Красный | Голубой | Золотой |
| **Урон** | 60 + INT×25 | 30 + INT×15 | 35 + STR×12 |
| **Эффект** | Горение 3сек | DOT 5сек | Оглушение 3сек |
| **Скорость** | 20 м/с | 20 м/с | 22 м/с ⚡ |
| **Вращение** | 360°/с | 360°/с | 540°/с ⚡ |
| **Trail** | Нет | Нет | Да ⭐ |
| **Класс** | Mage | Mage | Paladin |

**Hammer of Justice - самый быстрый снаряд с эффектом оглушения!**

---

## 📝 ФАЙЛЫ

**Изменённые:**
- `Assets/Prefabs/Projectiles/HammerProjectile.prefab` - заменён Cube на Golden_Forge

**Используемые:**
- `Assets/Prefabs/Transformations/Golden_Forge.prefab` - визуал молота
- `Assets/Resources/Skills/Paladin_HammerofJustice.asset` - настройки скилла

---

## ✅ ГОТОВО!

**Hammer of Justice теперь летит с 3D моделью Golden_Forge!**

Протестируйте в Unity - молот должен быть хорошо виден! ⚒️🎮

---

**Все снаряды готовы:**
- ✅ Fireball (Mage) - красный огненный шар
- ✅ Lightning Storm (Mage) - голубой энергетический шар
- ✅ Hammer of Justice (Paladin) - золотой молот ⚒️
