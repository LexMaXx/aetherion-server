# 🎯 Как создать BasicAttackConfig для класса

## ✅ ИСПРАВЛЕНИЕ: Добавлены enum'ы

Добавил в начало `BasicAttackConfig.cs`:
```csharp
public enum AttackType { Melee, Ranged }
public enum CharacterClass { Warrior, Mage, Archer, Rogue, Paladin }
```

Теперь скрипт должен скомпилироваться без ошибок!

---

## 📝 Пошаговая инструкция

### Шаг 1: Дождитесь компиляции Unity
1. Unity Editor должен автоматически перекомпилировать скрипты
2. Подождите пока внизу справа пропадёт индикатор компиляции
3. Проверьте Console - не должно быть ошибок (красных сообщений)

### Шаг 2: Создайте ScriptableObject для Мага

#### Вариант A: Через меню Project (рекомендуется)
```
1. В Project окне откройте папку:
   Assets → ScriptableObjects → Skills

2. ПКМ в пустом месте папки → Create → Aetherion → Combat → Basic Attack Config

3. Новый файл появится с именем: BasicAttack_.asset

4. Переименуйте его: BasicAttackConfig_Mage
```

#### Вариант B: Через верхнее меню
```
1. Assets (в верхнем меню) → Create → Aetherion → Combat → Basic Attack Config

2. Файл создастся в текущей выбранной папке

3. Переименуйте: BasicAttackConfig_Mage
```

### Шаг 3: Настройте параметры Мага

Выберите созданный `BasicAttackConfig_Mage.asset` и в Inspector заполните:

```
═══════════ БАЗОВАЯ ИНФОРМАЦИЯ ═══════════
Character Class:     [Mage          ▼]
Attack Type:         [Ranged        ▼]
Description:         "Магическая дальняя атака огненным шаром"

═══════════ УРОН ═══════════
Base Damage:         40
Strength Scaling:    0     ← Маг не использует Strength
Intelligence Scaling:2.0   ← Маг масштабируется от Intelligence!

═══════════ СКОРОСТЬ АТАКИ ═══════════
Attack Cooldown:     1.0 сек
Attack Range:        20 метров

═══════════ СНАРЯД ═══════════
Projectile Prefab:   [Перетащите CelestialBallProjectile]
                     Путь: Assets/Resources/Projectiles/CelestialBallProjectile
Projectile Speed:    20 м/с
Projectile Lifetime: 5 сек
☑ Projectile Homing
  Homing Speed:      10 м/с
  Homing Radius:     30 метров

═══════════ АНИМАЦИЯ ═══════════
Animation Trigger:   Attack
Animation Speed:     3.0   ← Маг атакует быстро!

═══════════ РАСХОД РЕСУРСОВ ═══════════
Mana Cost Per Attack:10
Action Points Cost:  1
```

### Шаг 4: Проверьте конфигурацию

В нижней части Inspector есть кнопки:
```
[🔍 ПРОВЕРИТЬ КОНФИГУРАЦИЮ]  ← Нажмите эту кнопку
[🐛 ВЫВЕСТИ DEBUG INFO]
```

Если всё правильно, в Console будет:
```
✅ Конфигурация BasicAttackConfig_Mage валидна!
```

Если есть ошибки:
```
❌ Ошибка валидации: Projectile Prefab не назначен для Ranged атаки!
```

---

## 🎨 Как должен выглядеть редактор

### Если всё правильно:

```
╔═══════════════════════════════════════════════════════╗
║  Inspector: BasicAttackConfig_Mage                    ║
╠═══════════════════════════════════════════════════════╣
║  Script: BasicAttackConfig                            ║
║                                                       ║
║  ┌─────────────────────────────────────────────────┐ ║
║  │ 📊 БАЗОВАЯ ИНФОРМАЦИЯ                           │ ║
║  ├─────────────────────────────────────────────────┤ ║
║  │ Character Class:  [Mage        ▼]              │ ║
║  │ Attack Type:      [Ranged      ▼]              │ ║
║  └─────────────────────────────────────────────────┘ ║
║                                                       ║
║  ┌─────────────────────────────────────────────────┐ ║
║  │ 💥 УРОН И СКЕЙЛИНГ                              │ ║
║  ├─────────────────────────────────────────────────┤ ║
║  │ Base Damage:          [40.0]                   │ ║
║  │ Intelligence Scaling: [2.0]                    │ ║
║  │                                                │ ║
║  │ 📈 ПРЕВЬЮ УРОНА:                                │ ║
║  │ С STR=10, INT=10: 60.0 урона                   │ ║
║  └─────────────────────────────────────────────────┘ ║
║                                                       ║
║  ┌─────────────────────────────────────────────────┐ ║
║  │ ⚡ СКОРОСТЬ АТАКИ                               │ ║
║  ├─────────────────────────────────────────────────┤ ║
║  │ Attack Cooldown: [1.0] сек                     │ ║
║  │ Attack Range:    [20.0] метров                 │ ║
║  │                                                │ ║
║  │ 📊 DPS: 60.0 урона/сек                          │ ║
║  └─────────────────────────────────────────────────┘ ║
║                                                       ║
║  ... и другие секции ...                              ║
║                                                       ║
║  [🔍 ПРОВЕРИТЬ КОНФИГУРАЦИЮ]                          ║
║  [🐛 ВЫВЕСТИ DEBUG INFO]                              ║
╚═══════════════════════════════════════════════════════╝
```

### Если есть ошибка компиляции:

```
╔═══════════════════════════════════════════════════════╗
║  Inspector: BasicAttackConfig_Mage                    ║
╠═══════════════════════════════════════════════════════╣
║  ⚠️                                                    ║
║  Script: None (Mono Script)                           ║
║                                                       ║
║  ⚠️ The associated script can not be loaded.          ║
║  Please fix any compile errors and assign a           ║
║  valid script.                                        ║
║                                                       ║
╚═══════════════════════════════════════════════════════╝
```

Если видите эту ошибку:
1. Откройте Console (Ctrl+Shift+C)
2. Найдите красные ошибки
3. Исправьте их
4. Дождитесь перекомпиляции

---

## 🔧 Найти префаб CelestialBallProjectile

### Поиск в Project:
```
1. Project окно → поиск (вверху справа)
2. Введите: CelestialBallProjectile
3. Фильтр: t:Prefab (только префабы)
```

### Возможные местоположения:
```
Assets/Resources/Projectiles/CelestialBallProjectile
Assets/Prefabs/Projectiles/CelestialBallProjectile
Assets/Prefabs/Transformations/CelestialBallProjectile
```

### Как назначить:
```
1. Найдите префаб через поиск
2. Выберите BasicAttackConfig_Mage
3. В Inspector найдите поле "Projectile Prefab"
4. Перетащите префаб из Project в это поле
5. Отпустите мышь
```

---

## ✅ Проверка что всё работает

### 1. Проверьте что скрипт скомпилировался:
```
Console (Ctrl+Shift+C) → Нет красных ошибок
```

### 2. Проверьте что asset создан:
```
Project → Assets/ScriptableObjects/Skills/BasicAttackConfig_Mage.asset
Выбрать → Inspector показывает параметры (не ошибку)
```

### 3. Проверьте что Custom Editor работает:
```
Inspector должен показывать цветные секции:
📊 БАЗОВАЯ ИНФОРМАЦИЯ (синяя)
💥 УРОН И СКЕЙЛИНГ (красная)
⚡ СКОРОСТЬ АТАКИ (жёлтая)
🎯 НАСТРОЙКИ СНАРЯДА (зелёная)
```

### 4. Проверьте превью урона:
```
Inspector → секция "УРОН И СКЕЙЛИНГ"
Должен быть текст:
"С STR=10, INT=10: 60.0 урона"
```

### 5. Проверьте DPS:
```
Inspector → секция "СКОРОСТЬ АТАКИ"
Должен быть текст:
"DPS: 60.0 урона/сек"
```

---

## 🚀 Следующий шаг: Назначение в PlayerAttack

После создания BasicAttackConfig_Mage:

```
1. Scene → Откройте ArenaScene или создайте SkillTestScene
2. Найдите префаб Мага в сцене
3. Выберите его
4. Inspector → компонент PlayerAttack
5. Найдите поле "Attack Config"
6. Перетащите BasicAttackConfig_Mage в это поле
7. Нажмите Apply (если это префаб)
```

Теперь при запуске игры в Console будет:
```
[PlayerAttack] ✅ Используется BasicAttackConfig: BasicAttackConfig_Mage
[PlayerAttack] 📊 Config: Damage=40, Range=20m, Type=Ranged
```

---

## ❓ Решение проблем

### Проблема 1: "The associated script can not be loaded"
**Решение:**
1. Проверьте Console на ошибки компиляции
2. Убедитесь что файл BasicAttackConfig.cs существует в правильной папке
3. Дождитесь окончания компиляции Unity

### Проблема 2: "Character Class" и "Attack Type" не отображаются
**Решение:**
1. Убедитесь что enum'ы определены в BasicAttackConfig.cs
2. Перекомпилируйте: Assets → Reimport All

### Проблема 3: Меню "Aetherion → Combat → Basic Attack Config" отсутствует
**Решение:**
1. Проверьте что в BasicAttackConfig.cs есть атрибут [CreateAssetMenu]
2. Перезапустите Unity Editor

### Проблема 4: Префаб CelestialBallProjectile не найден
**Решение:**
1. Проверьте существование префаба через поиск
2. Если не существует - пока оставьте поле пустым
3. Можно протестировать и без префаба (будет предупреждение)

### Проблема 5: Custom Editor не показывает цветные секции
**Решение:**
1. Проверьте что файл BasicAttackConfigEditor.cs существует
2. Он должен быть в папке Assets/Scripts/Editor/
3. Перекомпилируйте Unity

---

## 📚 Для справки: Значения для всех классов

### Warrior (Воин)
```
Character Class: Warrior
Attack Type: Melee
Base Damage: 30
Strength Scaling: 2.0
Intelligence Scaling: 0
Attack Range: 3
```

### Mage (Маг)
```
Character Class: Mage
Attack Type: Ranged
Base Damage: 40
Strength Scaling: 0
Intelligence Scaling: 2.0
Attack Range: 20
Projectile: CelestialBallProjectile
```

### Archer (Лучник)
```
Character Class: Archer
Attack Type: Ranged
Base Damage: 35
Strength Scaling: 1.0
Intelligence Scaling: 0.5
Attack Range: 50
Projectile: ArrowProjectile
```

### Rogue (Некромант)
```
Character Class: Rogue
Attack Type: Ranged
Base Damage: 50
Strength Scaling: 0.5
Intelligence Scaling: 1.5
Attack Range: 20
Projectile: SoulShardsProjectile
```

### Paladin (Паладин)
```
Character Class: Paladin
Attack Type: Melee
Base Damage: 25
Strength Scaling: 1.5
Intelligence Scaling: 0.5
Attack Range: 3
```
