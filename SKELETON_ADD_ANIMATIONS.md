# 🎬 Добавление анимаций к скелету

## Проблема
Скелет работает, но нет анимаций (не двигается визуально, не атакует).

## Причина
У Skeleton префаба не назначен Animator Controller.

---

## ✅ Решение (3 минуты)

### Шаг 1: Открой Skeleton префаб
```
1. Project → Assets/Resources/Minions/Skeleton.prefab
2. Двойной клик (откроется Prefab Mode)
```

### Шаг 2: Найди Animator компонент
```
1. Inspector → Найди компонент "Animator"
2. Если нет - Add Component → Animator
```

### Шаг 3: Назначь Controller
```
1. Animator → Controller (пустое поле)
2. Нажми на кружок справа (Select)
3. Выбери "RogueAnimator"
4. Или перетащи RogueAnimator.controller из Project
```

**Путь к controller:**
```
Assets/Animations/Controllers/RogueAnimator.controller
```

### Шаг 4: Проверь настройки
```
Animator компонент должен иметь:
- Controller: RogueAnimator ✅
- Avatar: (опционально, можно оставить None)
- Apply Root Motion: ❌ (выключено)
```

### Шаг 5: Сохрани префаб
```
1. Ctrl+S или File → Save
2. Выйди из Prefab Mode (стрелка назад)
```

---

## 🎮 Тестирование

### Шаг 1: Призови скелета
```
Play → Нажми Raise Dead
```

### Шаг 2: Проверь логи
```
Должно быть:
✅ [SkeletonAI] ✅ Animator Controller активен: RogueAnimator
(Вместо "⚠️ Animator Controller не назначен")
```

### Шаг 3: Проверь анимации
```
✅ Idle - скелет стоит (покачивается)
✅ Walk - скелет идёт к врагу (анимация ходьбы)
✅ Attack - скелет атакует (анимация удара)
```

---

## 📋 Требования к RogueAnimator

### Параметры (должны быть в Controller):
- **bool IsMoving** - для переключения Idle ↔ Walk
- **trigger Attack** - для запуска атаки

### Анимации (должны быть):
- **Idle** - стоячая анимация
- **Walk** - анимация ходьбы
- **Attack** - анимация атаки

### Если параметров нет:
```
1. Открой RogueAnimator.controller (двойной клик)
2. Animator → Parameters
3. Добавь:
   - IsMoving (Bool)
   - Attack (Trigger)
```

---

## 🔧 Альтернатива: Использовать другой Controller

Если RogueAnimator не подходит, можешь использовать любой другой:

### Найди подходящий Controller:
```
Project → Assets/Animations/Controllers/
Ищи файлы с расширением .controller
```

### Популярные варианты:
- HumanoidAnimator.controller (если есть)
- PlayerAnimator.controller
- EnemyAnimator.controller

### Требования:
- Должен иметь параметр "IsMoving" (bool)
- Должен иметь параметр "Attack" (trigger)
- Должен быть для Humanoid риг

---

## 📊 Проверка работы

### Idle (стоит на месте):
```
Animator → IsMoving = false
Скелет воспроизводит анимацию стойки
```

### Walk (идёт к врагу):
```
Animator → IsMoving = true
Скелет воспроизводит анимацию ходьбы
```

### Attack (атакует):
```
Animator → Attack trigger
Скелет воспроизводит анимацию атаки
Каждые 1.5 секунды
```

---

## 🎯 Ожидаемое поведение

### При призыве:
```
[SkeletonAI] ✅ Animator Controller активен: RogueAnimator
[SkeletonAI] 💀 Skeleton initialized
```

### При поиске цели:
```
[SkeletonAI] 🎯 Новая цель найдена: DummyEnemy
IsMoving = false (Idle анимация)
```

### При движении:
```
IsMoving = true
Анимация ходьбы проигрывается
Скелет плавно двигается к цели
```

### При атаке:
```
Attack trigger срабатывает
Анимация атаки проигрывается
[SkeletonAI] ⚔️ Skeleton атакует DummyEnemy: 34.5 урона
```

---

## ⚠️ Возможные проблемы

### Проблема 1: Анимации слишком быстрые/медленные
**Решение:**
```
1. Выбери Skeleton в Hierarchy (когда появится)
2. Inspector → Animator → Speed
3. Измени на 1.0 (нормально), 0.5 (медленнее), 2.0 (быстрее)
```

### Проблема 2: Скелет "скользит" при ходьбе
**Решение:**
```
1. Animator → Apply Root Motion: ❌ (выключить)
2. Или уменьши Move Speed в SkeletonAI (сейчас 3.5)
```

### Проблема 3: Анимация атаки не проигрывается
**Решение:**
```
1. Проверь что Attack - это Trigger (не Bool!)
2. Открой RogueAnimator → Parameters → Attack должен быть Trigger
3. Проверь переход Any State → Attack
```

### Проблема 4: "Animator is not playing"
**Решение:**
```
1. Убедись что RogueAnimator назначен
2. Проверь что префаб сохранён (Ctrl+S)
3. Пересоздай скелета (Raise Dead снова)
```

---

## 📝 Чеклист

Перед тестом убедись:

- ✅ Skeleton.prefab находится в Resources/Minions/
- ✅ У Skeleton есть компонент Animator
- ✅ Animator → Controller = RogueAnimator
- ✅ RogueAnimator имеет параметры IsMoving и Attack
- ✅ Префаб сохранён (Ctrl+S)
- ✅ Play сцену и призови скелета

---

## 🎉 Готово!

После назначения RogueAnimator Controller:
- ✅ Скелет будет стоять с анимацией Idle
- ✅ При движении будет анимация Walk
- ✅ При атаке будет анимация Attack
- ✅ Всё будет выглядеть живо и динамично!

**Время настройки: 2 минуты**

Назначь Controller и протестируй! 🎬💀⚔️
