# 💀 Skeleton Prefab - Инструкция по настройке

## Обзор
Скелет-миньон для скилла Raise Dead. Атакует врагов с Target компонентом.

---

## Шаг 1: Создание префаба

### 1.1. Создай папку для миньонов
```
1. Project → Assets → Resources
2. ПКМ → Create → Folder
3. Имя: "Minions"
```

**Путь:** `Assets/Resources/Minions/`

### 1.2. Перемести префаб Skeleton
```
1. Найди свой Skeleton humanoid префаб
2. Перетащи его в Assets/Resources/Minions/
3. Переименуй в "Skeleton" (без пробелов!)
```

**Финальный путь:** `Assets/Resources/Minions/Skeleton.prefab`

---

## Шаг 2: Настройка компонентов

### 2.1. Компоненты которые ДОЛЖНЫ быть:

#### ✅ Animator
```
Animator Controller: Rogue (у тебя уже настроен)

Параметры:
- bool IsMoving  (для анимации ходьбы)
- trigger Attack (для анимации атаки)
```

**Проверка:**
- Открой Animator окно
- Проверь что есть переходы: Idle → Walk (IsMoving = true)
- Проверь что есть триггер Attack → Attack Animation

#### ✅ NavMeshAgent
```
Добавь компонент: Add Component → Navigation → Nav Mesh Agent

Настройки:
Speed: 3.5
Stopping Distance: 1.5
Angular Speed: 360
Acceleration: 8
```

**Почему нужен:** Для движения к врагам

#### ✅ Capsule Collider
```
Должен быть на Skeleton (для физики)

Настройки:
Center: (0, 1, 0)
Radius: 0.3
Height: 2
```

#### ✅ Rigidbody
```
Добавь: Add Component → Rigidbody

Настройки:
Mass: 50
Use Gravity: ✅
Is Kinematic: ❌
Constraints:
  - Freeze Rotation X ✅
  - Freeze Rotation Z ✅
```

**Почему:** Чтобы скелет не падал и не переворачивался

#### ✅ SkeletonAI (Auto-добавляется)
```
Скрипт автоматически добавится при призыве через ExecuteSummon
НЕ добавляй вручную в префаб!

Параметры (настроятся автоматически):
- Attack Range: 2
- Attack Cooldown: 1.5
- Enable Logs: true
```

---

## Шаг 3: Layer и Tag

### 3.1. Tag
```
Inspector → Tag → Add Tag
Создай новый тег: "Minion"

Применить к Skeleton:
Tag: Minion
```

### 3.2. Layer
```
Используй существующий Layer или создай новый:
Layer: Default (или создай "Minion" layer)
```

---

## Шаг 4: Проверка анимаций

### 4.1. Необходимые анимации в Animator Controller (Rogue)

**Idle Animation** (простой)
- Скелет стоит на месте
- Медленное покачивание

**Walk Animation**
- Ходьба вперед
- Связана с параметром `IsMoving = true`

**Attack Animation**
- Удар мечом/кулаком
- Триггер `Attack`
- Длительность ~1 секунда

### 4.2. Transitions (переходы)
```
Idle → Walk
Condition: IsMoving = true
Exit Time: 0
Transition Duration: 0.15

Walk → Idle
Condition: IsMoving = false
Exit Time: 0
Transition Duration: 0.15

Any State → Attack
Condition: Attack (trigger)
Exit Time: 0
Transition Duration: 0.1

Attack → Idle
Exit Time: 0.9
No Conditions
```

---

## Шаг 5: NavMesh настройка сцены

### 5.1. Bake NavMesh
```
1. Window → AI → Navigation
2. Выбери вкладку "Bake"
3. Настройки:
   - Agent Radius: 0.3
   - Agent Height: 2
   - Max Slope: 45
   - Step Height: 0.4

4. Нажми "Bake"
```

**Важно:** NavMesh должен покрывать всю арену, где будет драться скелет!

### 5.2. Проверка NavMesh
```
Scene View → Gizmos → Navigation
Синие области = walkable NavMesh ✅
```

---

## Шаг 6: Тестирование

### 6.1. Запуск теста
```
1. ▶️ Play Scene
2. Выбери Rogue (Necromancer)
3. Поставь Target на DummyEnemy:
   - Найди DummyEnemy в Hierarchy
   - Add Component → Target

4. Нажми клавишу призыва (обычно "5")
```

### 6.2. Ожидаемое поведение
```
✅ Скелет спавнится перед некромантом (2 метра вперед)
✅ Красный/чёрный эффект призыва
✅ Скелет автоматически ищет врага с Target
✅ Скелет бежит к врагу
✅ Когда близко - останавливается и атакует
✅ Анимация атаки проигрывается
✅ Враг получает урон (проверь логи)
✅ Через 20 секунд скелет исчезает
```

### 6.3. Логи в Console
```
[SkillExecutor] 💀 Raise Dead: миньон призван на 20 секунд
[SkillExecutor] ⚔️ Урон миньона: 30 + 50% INT
[SkillExecutor] 📍 Позиция спавна: (x, y, z)
[SkeletonAI] 💀 Skeleton initialized - Owner: RoguePlayer, Damage: 40, Lifetime: 20s
[SkeletonAI] 🎯 Новая цель найдена: DummyEnemy (дистанция: 10м)
[SkeletonAI] ⚔️ Skeleton атакует DummyEnemy: 40 урона
[SkeletonAI] 💀 Skeleton умирает (прожил 20с)
```

---

## Возможные проблемы и решения

### ❌ Проблема: Skeleton prefab не найден
```
[SkillExecutor] ❌ Skeleton prefab не найден в Resources/Minions/Skeleton!
```

**Решение:**
- Проверь путь: `Assets/Resources/Minions/Skeleton.prefab`
- Убедись что папка называется "Resources" (с большой буквы)
- Prefab должен быть внутри Resources/

### ❌ Проблема: Скелет не двигается
```
Скелет стоит на месте, не идёт к врагу
```

**Решение:**
- Проверь NavMeshAgent компонент
- Убедись что NavMesh забейкан (Window → AI → Navigation → Bake)
- Проверь что скелет стоит НА NavMesh (синяя область в Scene View)

### ❌ Проблема: Скелет не атакует
```
Скелет подбегает но не атакует
```

**Решение:**
- Проверь что у DummyEnemy есть компонент Target
- Проверь что у DummyEnemy есть HealthSystem
- Проверь логи - есть ли "🎯 Новая цель найдена"?

### ❌ Проблема: Нет анимаций
```
Скелет T-pose или анимации не работают
```

**Решение:**
- Проверь Animator Controller (должен быть Rogue)
- Проверь что есть параметр IsMoving (bool)
- Проверь что есть триггер Attack
- Проверь переходы между анимациями

### ❌ Проблема: Скелет падает/переворачивается
```
Скелет падает на бок или крутится
```

**Решение:**
- Rigidbody → Constraints:
  - Freeze Rotation X ✅
  - Freeze Rotation Z ✅
- NavMeshAgent → Auto Braking ✅

---

## Визуальная проверка (Gizmos)

При выборе скелета в Hierarchy должны появиться Gizmos:

```
🟡 Жёлтая сфера - радиус обнаружения (20м)
🔴 Красная сфера - радиус атаки (2м)
🟢 Зелёная линия - к текущей цели
```

**Как включить Gizmos:**
```
Scene View → Gizmos → ✅ включить
```

---

## Финальный чеклист

Перед запуском проверь:

- ✅ Prefab лежит в `Assets/Resources/Minions/Skeleton.prefab`
- ✅ Animator Controller настроен (Rogue)
- ✅ NavMeshAgent добавлен и настроен
- ✅ Rigidbody с Freeze Rotation X, Z
- ✅ Capsule Collider добавлен
- ✅ NavMesh забейкан в сцене
- ✅ DummyEnemy имеет компонент Target
- ✅ DummyEnemy имеет HealthSystem

---

## Что дальше?

После успешного теста:

1. **Визуальные улучшения:**
   - Эффект призыва (тёмная аура)
   - Эффект смерти (кости рассыпаются)
   - Trail за скелетом (тёмный дым)

2. **Балансировка:**
   - Подкрутить урон (сейчас 30 + 50% INT)
   - Изменить скорость атаки (сейчас 1.5 сек)
   - Настроить HP скелета (сейчас неуязвим)

3. **Расширение:**
   - Добавить HP скелету (можно убить)
   - Разные типы скелетов (Archer, Mage)
   - Командование скелетом (Attack, Follow, Stay)

---

## Готово! 💀

Скелет готов к призыву и бою!

**Удачи в создании армии нежити!** ☠️🧙‍♂️
