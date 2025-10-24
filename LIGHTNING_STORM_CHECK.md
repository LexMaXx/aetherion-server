# ⚡ ПРОВЕРКА LIGHTNING STORM - ЭНЕРГЕТИЧЕСКИЙ ШАР

## ✅ ЧТО УЖЕ НАСТРОЕНО

### 1. SkillData: Mage_LightningStorm.asset

**Основные параметры:**
```yaml
skillId: 206
skillName: Lightning Storm
cooldown: 20 сек
manaCost: 55
castRange: 20 метров
castTime: 1.5 сек
```

**Урон:**
```yaml
baseDamageOrHeal: 30
intelligenceScaling: 15
Итого: 30 + (INT * 15) урона
```

**Эффект (DOT - Damage Over Time):**
```yaml
effectType: 10 (DOT)
duration: 5 секунд
damageOrHealPerTick: 15 урона
tickInterval: 1 секунда
Итого: 15 урона каждую секунду в течение 5 секунд = 75 доп урона
```

**Снаряд:**
```yaml
projectilePrefab: LightningBallProjectile
projectileSpeed: 20
projectileHoming: false (летит прямо, не следует за целью)
projectileLifetime: 5 секунд
```

**Визуальные эффекты:**
```yaml
visualEffectPrefab: GUID effb650189dc5714e9e9801b1cfb3991 (эффект на кастере)
projectileHitEffectPrefab: cfxr electric_arc_circle (взрыв при попадании)
```

---

### 2. Prefab: LightningBallProjectile

**Компоненты:**
```
✅ Projectile.cs - управление полётом и попаданием
✅ SphereCollider - обнаружение столкновений (radius: 0.5)
✅ Rigidbody - физика (kinematic, без гравитации)
✅ Light - СВЕЧЕНИЕ (голубой цвет, intensity: 4, range: 6)
✅ Visual (child) - визуальная модель энергетического шара
```

**Параметры Projectile:**
```csharp
speed: 20
lifetime: 5
homing: false
rotationSpeed: 360 (вращается)
```

---

### 3. Как работает система:

**Последовательность событий:**

1. **Маг кастует Lightning Storm:**
   - SkillManager.UseSkill() вызывается
   - Проверка маны (55 MP)
   - Проверка кулдауна (20 сек)
   - Проверка дистанции (20 метров)

2. **Анимация каста:**
   - Trigger: "MageAttack"
   - Длительность: 1.5 секунды

3. **Создание снаряда:**
   ```csharp
   // В SkillManager.cs, метод UseSkill()
   GameObject projectileObj = Instantiate(
       skill.projectilePrefab,
       caster.position + Vector3.up * 1.5f,
       Quaternion.identity
   );

   Projectile projectile = projectileObj.GetComponent<Projectile>();
   projectile.InitializeFromSkill(skill, target, direction, caster.gameObject);
   ```

4. **Полёт снаряда:**
   - Скорость: 20 м/с
   - Вращение: 360°/с (визуальная часть)
   - Свечение: голубой свет (intensity: 4, range: 6)
   - Направление: прямо к цели (homing: false)

5. **Попадание в цель:**
   - OnTriggerEnter обнаруживает врага
   - Наносится урон: 30 + (INT * 15)
   - Создаётся эффект взрыва: electric_arc_circle
   - Применяется DOT эффект: 15 урона/сек на 5 секунд
   - Снаряд уничтожается

6. **DOT эффект (горение молнией):**
   - Каждую секунду: 15 урона
   - Длительность: 5 секунд
   - Визуальный эффект частиц на цели
   - Суммарный урон: 75

**Общий урон Lightning Storm:**
```
Прямой урон: 30 + (INT * 15)
DOT урон: 15 * 5 = 75
Итого: 105 + (INT * 15)

Пример с INT=10: 30 + 150 + 75 = 255 урона!
```

---

## 🎨 ВИЗУАЛЬНЫЕ ЭФФЕКТЫ

### 1. Свечение энергетического шара:
```
Type: Point Light
Color: RGB(0.3, 0.7, 1.0) - голубой
Intensity: 4
Range: 6 метров
```

### 2. Вращение:
```
Скорость: 360°/сек
Ось: Forward (Z)
```

### 3. Взрыв при попадании:
```
Prefab: cfxr electric_arc_circle
Эффект: электрическая дуга/круг
```

### 4. DOT визуал на цели:
```
Prefab: particleEffectPrefab из SkillEffect
GUID: effb650189dc5714e9e9801b1cfb3991
```

---

## 🧪 КАК ПРОТЕСТИРОВАТЬ В UNITY

### Вариант 1: В игре (Arena Scene)

1. **Запустить Arena Scene**
2. **Выбрать Mage класс**
3. **Экипировать Lightning Storm** (в Character Selection)
4. **В игре:**
   - Найти врага
   - Нажать клавишу скилла (1, 2 или 3)
   - Проверить:
     * ✅ Анимация каста (1.5 сек)
     * ✅ Энергетический шар вылетает
     * ✅ Шар светится голубым
     * ✅ Шар вращается
     * ✅ Летит к врагу
     * ✅ Взрыв при попадании
     * ✅ Урон врагу
     * ✅ DOT эффект (15 урона/сек, 5 сек)

### Вариант 2: В редакторе (Scene View)

1. **Открыть Arena Scene**
2. **Найти Mage в Hierarchy**
3. **Проверить SkillManager:**
   - Equipped Skills → есть Mage_LightningStorm?
   - All Available Skills → есть все 6 скилов мага?

4. **Play Mode:**
   - Выбрать Mage в Hierarchy
   - В Inspector видно SkillManager
   - Использовать скилл через клавишу

5. **Проверить префаб:**
   - Project → Assets/Prefabs/Projectiles/LightningBallProjectile
   - Перетащить в Scene
   - Проверить компоненты:
     * Projectile script
     * Light (голубой)
     * Visual child object

---

## 🔍 СРАВНЕНИЕ С FIREBALL

| Параметр | Fireball | Lightning Storm |
|----------|----------|-----------------|
| **Урон базовый** | 60 | 30 |
| **Скейлинг** | INT * 25 | INT * 15 |
| **Мана** | 40 | 55 |
| **Кулдаун** | 6 сек | 20 сек |
| **Дальность** | 15 м | 20 м |
| **Cast Time** | 0.8 сек | 1.5 сек |
| **DOT эффект** | Горение 10/тик, 3сек | Молния 15/тик, 5сек |
| **DOT урон** | 30 | 75 |
| **Общий урон** | 90 + INT*25 | 105 + INT*15 |
| **Скорость** | 20 | 20 |
| **Самонаведение** | true | false |
| **Цвет света** | Красный | Голубой |

**Вывод:** Lightning Storm - более медленный, но более мощный скилл с сильным DOT!

---

## ✅ ЧТО ПРОВЕРИТЬ

### В Inspector (SkillData):
- [ ] projectilePrefab назначен (LightningBallProjectile)
- [ ] projectileHitEffectPrefab назначен (electric_arc_circle)
- [ ] visualEffectPrefab назначен (эффект на кастере)
- [ ] effects[0].particleEffectPrefab назначен (DOT визуал)

### В Prefab (LightningBallProjectile):
- [ ] Projectile script с правильными параметрами
- [ ] SphereCollider (IsTrigger = true)
- [ ] Light компонент (голубой цвет)
- [ ] Visual child object существует
- [ ] Rigidbody (IsKinematic = true, UseGravity = false)

### В игре:
- [ ] Снаряд создаётся при касте
- [ ] Снаряд светится голубым
- [ ] Снаряд вращается
- [ ] Снаряд летит к врагу
- [ ] Взрыв при попадании
- [ ] Урон врагу отображается
- [ ] DOT эффект работает (5 тиков по 15 урона)
- [ ] Визуальные эффекты проигрываются

---

## 🐛 ВОЗМОЖНЫЕ ПРОБЛЕМЫ

### Проблема 1: Снаряд не вылетает
**Причина:** projectilePrefab не назначен в SkillData
**Решение:** В Unity Editor → Assets/Resources/Skills/Mage_LightningStorm.asset → назначить LightningBallProjectile

### Проблема 2: Нет свечения
**Причина:** Light компонент выключен или неправильно настроен
**Решение:** Проверить LightningBallProjectile → Light → Enabled = true, Intensity > 0

### Проблема 3: Нет взрыва при попадании
**Причина:** projectileHitEffectPrefab не назначен
**Решение:** Назначить cfxr electric_arc_circle в SkillData

### Проблема 4: DOT не работает
**Причина:** Эффект не настроен правильно
**Решение:** Проверить в SkillData:
- effects[0].effectType = 10 (DAMAGE_OVER_TIME)
- effects[0].damageOrHealPerTick = 15
- effects[0].tickInterval = 1
- effects[0].duration = 5

### Проблема 5: Снаряд не вращается
**Причина:** rotationSpeed = 0
**Решение:** В LightningBallProjectile → Projectile → rotationSpeed = 360

---

## 📝 РЕЗЮМЕ

**Lightning Storm полностью настроен и должен работать как Fireball!**

✅ **Вылетает как снаряд** - Projectile.cs управляет полётом
✅ **Имеет свечение** - Light компонент (голубой, intensity: 4)
✅ **Взрыв при попадании** - projectileHitEffectPrefab (electric_arc_circle)
✅ **DOT эффект** - 15 урона/сек, 5 секунд
✅ **Вращение** - rotationSpeed: 360°/сек

**Просто запустите игру и используйте скилл! Всё должно работать!** ⚡

---

## 🚀 ЕСЛИ ХОТИТЕ УЛУЧШИТЬ

### Добавить Trail (след за снарядом):
```
1. LightningBallProjectile → Add Component → Trail Renderer
2. Настроить:
   - Width: 0.5 → 0.1
   - Time: 0.3
   - Color: Голубой градиент
   - Material: Trail материал
3. В Projectile script → Trail → назначить Trail Renderer
```

### Усилить свечение:
```
LightningBallProjectile → Light:
- Intensity: 4 → 8
- Range: 6 → 10
```

### Добавить звук:
```
SkillData:
- castSound: звук каста молнии
- projectileHitSound: звук удара молнии
```

---

**Всё готово к тестированию! ⚡🎮**
