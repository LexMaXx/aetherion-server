# 🎨 Детальное руководство: UI панель выбора скиллов

> **Время выполнения:** 15-20 минут
> **Сложность:** Средняя
> **Сцена:** CharacterSelection

---

## 📋 Что мы создаём

```
Canvas
└── SkillSelectionPanel (корневая панель справа)
    ├── Background (полупрозрачный чёрный фон)
    ├── TitleText ("ВЫБЕРИТЕ 3 НАВЫКА")
    │
    ├── SkillLibraryPanel (библиотека 6 скиллов)
    │   ├── LibraryTitle ("Доступные навыки")
    │   └── LibraryGrid (GridLayoutGroup: 3 колонки x 2 ряда)
    │       ├── LibrarySlot_1 (Image + SkillSlotUI.cs)
    │       ├── LibrarySlot_2
    │       ├── LibrarySlot_3
    │       ├── LibrarySlot_4
    │       ├── LibrarySlot_5
    │       └── LibrarySlot_6
    │
    ├── EquippedSkillsPanel (3 выбранных скилла)
    │   ├── EquippedTitle ("Выбранные навыки")
    │   └── EquippedGrid (HorizontalLayoutGroup)
    │       ├── EquippedSlot_1 (Image + SkillSlotUI.cs)
    │       │   └── KeyText ("1")
    │       ├── EquippedSlot_2
    │       │   └── KeyText ("2")
    │       └── EquippedSlot_3
    │           └── KeyText ("3")
    │
    ├── SkillInfoPanel (тултип при наведении)
    │   ├── InfoBackground
    │   ├── SkillNameText
    │   ├── SkillDescriptionText
    │   └── StatsText (кулдаун, мана, дальность)
    │
    └── ConfirmButton ("Подтвердить выбор")
```

---

## 🎯 ШАГ 1: Создать корневую панель

### 1.1 Открыть сцену
1. В Unity, открой **Project** → **Scenes** → **CharacterSelection.unity**
2. Двойной клик для загрузки сцены

### 1.2 Найти Canvas
1. В **Hierarchy** найди объект **Canvas**
2. Если Canvas нет:
   - ПКМ в Hierarchy → **UI** → **Canvas**
   - Canvas Scaler → UI Scale Mode: **Scale With Screen Size**
   - Reference Resolution: **1920 x 1080**

### 1.3 Создать корневую панель
1. **ПКМ на Canvas** → **Create Empty**
2. Назови: `SkillSelectionPanel`
3. В **Inspector**, настрой **RectTransform**:

```
RectTransform:
├── Anchor Presets: Middle-Right
│   (Alt+Click на правый средний preset)
│
├── Pivot: X: 1, Y: 0.5
│
├── Position:
│   X: -50  (отступ от правого края)
│   Y: 0
│   Z: 0
│
└── Size:
    Width: 1100
    Height: 900
```

**Визуально:** Панель будет справа, занимая большую часть правой стороны экрана

---

## 🎨 ШАГ 2: Добавить фон

### 2.1 Создать фон
1. **ПКМ на SkillSelectionPanel** → **UI** → **Image**
2. Назови: `Background`

### 2.2 Настроить RectTransform
```
RectTransform:
├── Anchor Presets: Stretch (all sides)
│   (Alt+Shift+Click на правый нижний preset)
│
└── Margins: все по 0
    Left: 0, Right: 0, Top: 0, Bottom: 0
```

### 2.3 Настроить Image
```
Image Component:
├── Source Image: None (оставить пустым = белый квадрат)
│
└── Color:
    R: 0, G: 0, B: 0, A: 200  (чёрный, 78% прозрачности)
    Или используй ColorPicker → чёрный цвет, альфа ~0.78
```

### 2.4 Добавить закругление (опционально)
Если хочешь закруглённые углы:
1. Создай простой sprite с закруглёнными углами
2. Или используй стандартный UI Sprite: **UISprite** (в Unity есть по умолчанию)

---

## 📝 ШАГ 3: Добавить заголовок

### 3.1 Создать текст
1. **ПКМ на SkillSelectionPanel** → **UI** → **Text - TextMeshPro**
2. Если появится окно "Import TMP Essentials" → нажми **Import**
3. Назови: `TitleText`

### 3.2 Настроить RectTransform
```
RectTransform:
├── Anchor: Top-Center
│
├── Position:
│   X: 0
│   Y: -40  (отступ от верха)
│   Z: 0
│
└── Size:
    Width: 800
    Height: 60
```

### 3.3 Настроить TextMeshPro
```
TextMeshProUGUI Component:
├── Text: "ВЫБЕРИТЕ 3 НАВЫКА"
│
├── Font: LiberationSans SDF (по умолчанию)
│
├── Font Style: Bold
│
├── Font Size: 42
│
├── Alignment:
│   Horizontal: Center
│   Vertical: Middle
│
├── Color: Белый (R: 255, G: 255, B: 255)
│
└── Extra Settings → Outline:
    Enable: true
    Color: чёрный
    Thickness: 0.2
```

---

## 📚 ШАГ 4: Создать библиотеку скиллов (6 слотов)

### 4.1 Создать контейнер
1. **ПКМ на SkillSelectionPanel** → **Create Empty**
2. Назови: `SkillLibraryPanel`

### 4.2 Настроить RectTransform
```
RectTransform:
├── Anchor: Top-Left (но внутри панели)
│
├── Position:
│   X: 570  (центр относительно всей панели)
│   Y: -150  (отступ от заголовка)
│   Z: 0
│
├── Pivot: X: 0.5, Y: 1
│
└── Size:
    Width: 1000
    Height: 450
```

### 4.3 Добавить GridLayoutGroup
1. С выбранным `SkillLibraryPanel`, нажми **Add Component**
2. Найди: **Grid Layout Group**
3. Настрой:

```
Grid Layout Group:
├── Padding:
│   Left: 20, Right: 20
│   Top: 20, Bottom: 20
│
├── Cell Size:
│   X: 150
│   Y: 180
│
├── Spacing:
│   X: 30
│   Y: 30
│
├── Start Corner: Upper Left
│
├── Start Axis: Horizontal
│
├── Child Alignment: Upper Center
│
├── Constraint: Fixed Column Count
│
└── Constraint Count: 3  (3 скилла в ряду)
```

### 4.4 Добавить заголовок библиотеки
1. **ПКМ на SkillLibraryPanel** → **UI** → **Text - TextMeshPro**
2. Назови: `LibraryTitle`
3. Настрой:

```
RectTransform:
├── Anchor: Top-Center
├── Position: X: 0, Y: 50, Z: 0
└── Size: Width: 600, Height: 40

TextMeshProUGUI:
├── Text: "Доступные навыки"
├── Font Size: 28
├── Alignment: Center
└── Color: золотистый (R: 255, G: 220, B: 100)
```

**ВАЖНО:** Перетащи `LibraryTitle` **НАД** компонентом GridLayoutGroup в Hierarchy, чтобы он не попал в сетку!

---

## 🎴 ШАГ 5: Создать слоты библиотеки (6 штук)

Теперь создадим один слот, настроим его идеально, а потом продублируем 5 раз.

### 5.1 Создать первый слот
1. **ПКМ на SkillLibraryPanel** → **UI** → **Image**
2. Назови: `LibrarySlot_1`

### 5.2 Настроить Image слота
```
Image Component:
├── Source Image: UISprite (стандартный белый квадрат)
│
├── Color: тёмно-серый (R: 40, G: 40, B: 40, A: 255)
│
└── Image Type: Sliced (для закруглённых углов)
```

### 5.3 Добавить компонент SkillSlotUI
1. С выбранным `LibrarySlot_1`, нажми **Add Component**
2. Введи: `SkillSlotUI`
3. Нажми **New script** → **Create and Add**
4. Скрипт уже существует, поэтому просто найди его:
   - Поиск: `SkillSlotUI`
   - Выбери существующий скрипт из Assets/Scripts/UI/Skills/

### 5.4 Настроить SkillSlotUI в Inspector
```
SkillSlotUI (Script):
├── Is Library Slot: ✓ TRUE  (галочка!)
│
├── Skill Selection Manager: (оставь пустым пока)
│
└── Остальные поля: пустые
```

### 5.5 Добавить иконку внутри слота
1. **ПКМ на LibrarySlot_1** → **UI** → **Image**
2. Назови: `Icon`
3. Настрой:

```
RectTransform:
├── Anchor: Stretch (all sides)
└── Margins: 10 со всех сторон

Image:
├── Source Image: (пустое - будет назначено кодом)
├── Color: белый
├── Preserve Aspect: ✓ TRUE
└── Raycast Target: ✗ FALSE  (не нужно)
```

### 5.6 Добавить текст "Пусто"
1. **ПКМ на LibrarySlot_1** → **UI** → **Text - TextMeshPro**
2. Назови: `EmptyText`
3. Настрой:

```
RectTransform:
├── Anchor: Stretch
└── Margins: 0 со всех сторон

TextMeshProUGUI:
├── Text: "Пусто"
├── Font Size: 18
├── Alignment: Center, Middle
├── Color: серый (R: 150, G: 150, B: 150)
└── Enabled: TRUE (потом скроется кодом если есть скилл)
```

### 5.7 Добавить название скилла
1. **ПКМ на LibrarySlot_1** → **UI** → **Text - TextMeshPro**
2. Назови: `SkillNameText`
3. Настрой:

```
RectTransform:
├── Anchor: Bottom-Center
├── Position: X: 0, Y: 10, Z: 0
└── Size: Width: 140, Height: 40

TextMeshProUGUI:
├── Text: "" (пусто)
├── Font Size: 14
├── Alignment: Center, Middle
├── Color: белый
├── Wrapping: Enable
└── Overflow: Truncate
```

### 5.8 Назначить ссылки в SkillSlotUI
Теперь соедини все элементы:

1. Выбери `LibrarySlot_1`
2. В Inspector, найди компонент **SkillSlotUI (Script)**
3. Перетащи элементы:

```
SkillSlotUI (Script):
├── Icon Image: → перетащи Icon
├── Skill Name Text: → перетащи SkillNameText
├── Empty Text: → перетащи EmptyText
├── Cooldown Overlay: (оставь пустым - для арены)
├── Cooldown Text: (оставь пустым - для арены)
└── Key Bind Text: (оставь пустым - только для equipped слотов)
```

---

## 🔁 ШАГ 6: Дублировать слоты (x5)

### 6.1 Создать копии
1. Выбери `LibrarySlot_1` в Hierarchy
2. **Ctrl+D** (дублировать) 5 раз
3. Переименуй копии:
   - LibrarySlot_2
   - LibrarySlot_3
   - LibrarySlot_4
   - LibrarySlot_5
   - LibrarySlot_6

**GridLayoutGroup автоматически расположит их в сетку 3x2!**

### 6.2 Проверка
В **Game View** (или Scene с выбранным Canvas):
- Должна быть сетка 3x2 (3 колонки, 2 ряда)
- Все 6 слотов одинакового размера
- Надпись "Пусто" в каждом

---

## 🎯 ШАГ 7: Создать панель выбранных скиллов (3 слота)

### 7.1 Создать контейнер
1. **ПКМ на SkillSelectionPanel** → **Create Empty**
2. Назови: `EquippedSkillsPanel`

### 7.2 Настроить RectTransform
```
RectTransform:
├── Anchor: Bottom-Center
│
├── Position:
│   X: 0
│   Y: 80  (отступ от низа)
│   Z: 0
│
├── Pivot: X: 0.5, Y: 0
│
└── Size:
    Width: 700
    Height: 220
```

### 7.3 Добавить HorizontalLayoutGroup
1. С выбранным `EquippedSkillsPanel`, нажми **Add Component**
2. Найди: **Horizontal Layout Group**
3. Настрой:

```
Horizontal Layout Group:
├── Padding:
│   Left: 20, Right: 20
│   Top: 20, Bottom: 20
│
├── Spacing: 40
│
├── Child Alignment: Middle Center
│
├── Child Controls Size:
│   Width: ✗ FALSE
│   Height: ✗ FALSE
│
└── Child Force Expand:
    Width: ✗ FALSE
    Height: ✗ FALSE
```

### 7.4 Добавить заголовок
1. **ПКМ на EquippedSkillsPanel** → **UI** → **Text - TextMeshPro**
2. Назови: `EquippedTitle`
3. Настрой:

```
RectTransform:
├── Anchor: Top-Center
├── Position: X: 0, Y: 50, Z: 0
└── Size: Width: 600, Height: 40

TextMeshProUGUI:
├── Text: "Выбранные навыки (1, 2, 3)"
├── Font Size: 24
├── Alignment: Center
└── Color: зелёный (R: 100, G: 255, B: 100)
```

**Перетащи `EquippedTitle` НАД HorizontalLayoutGroup в Hierarchy!**

---

## 🎮 ШАГ 8: Создать слоты для выбранных скиллов (3 штуки)

### 8.1 Создать первый слот
1. **ПКМ на EquippedSkillsPanel** → **UI** → **Image**
2. Назови: `EquippedSlot_1`

### 8.2 Настроить RectTransform
```
RectTransform:
└── Size:
    Width: 180
    Height: 180
```

### 8.3 Настроить Image
```
Image:
├── Color: тёмно-зелёный (R: 20, G: 60, B: 20, A: 255)
└── Raycast Target: ✓ TRUE  (для drag & drop)
```

### 8.4 Добавить SkillSlotUI
1. **Add Component** → `SkillSlotUI`
2. Настрой:

```
SkillSlotUI (Script):
└── Is Library Slot: ✗ FALSE  (БЕЗ галочки!)
```

### 8.5 Добавить внутренние элементы (аналогично LibrarySlot)
Создай те же элементы:
- **Icon** (Image)
- **EmptyText** (TextMeshPro: "Пусто")
- **SkillNameText** (TextMeshPro, внизу слота)

Настройки точно такие же, как в LibrarySlot_1!

### 8.6 Добавить номер клавиши (ВАЖНО!)
1. **ПКМ на EquippedSlot_1** → **UI** → **Text - TextMeshPro**
2. Назови: `KeyBindText`
3. Настрой:

```
RectTransform:
├── Anchor: Top-Left
├── Position: X: 10, Y: -10, Z: 0
└── Size: Width: 40, Height: 40

TextMeshProUGUI:
├── Text: "1"
├── Font Size: 32
├── Font Style: Bold
├── Alignment: Center, Middle
├── Color: жёлтый (R: 255, G: 255, B: 0)
└── Outline:
    Color: чёрный
    Thickness: 0.3
```

### 8.7 Назначить ссылки в SkillSlotUI
```
SkillSlotUI (Script):
├── Icon Image: → Icon
├── Skill Name Text: → SkillNameText
├── Empty Text: → EmptyText
└── Key Bind Text: → KeyBindText  (ВАЖНО для equipped!)
```

### 8.8 Дублировать для слотов 2 и 3
1. Выбери `EquippedSlot_1`
2. **Ctrl+D** два раза
3. Переименуй:
   - EquippedSlot_2 → измени KeyBindText на "2"
   - EquippedSlot_3 → измени KeyBindText на "3"

---

## 💡 ШАГ 9: Создать панель информации (тултип)

### 9.1 Создать контейнер
1. **ПКМ на SkillSelectionPanel** → **Create Empty**
2. Назови: `SkillInfoPanel`

### 9.2 Настроить RectTransform
```
RectTransform:
├── Anchor: Top-Right
├── Position: X: -20, Y: -300, Z: 0
└── Size: Width: 350, Height: 250
```

### 9.3 Добавить фон
1. **ПКМ на SkillInfoPanel** → **UI** → **Image**
2. Назови: `InfoBackground`
3. Настрой:

```
RectTransform:
├── Anchor: Stretch
└── Margins: 0 со всех сторон

Image:
└── Color: чёрный с альфа 220 (R: 0, G: 0, B: 0, A: 220)
```

### 9.4 Добавить текстовые поля
Создай 3 TextMeshPro элемента внутри SkillInfoPanel:

#### **SkillNameText**
```
Position: X: 0, Y: -30, Z: 0
Size: Width: 320, Height: 40
Text: ""
Font Size: 24
Bold: true
Color: золотистый
Alignment: Center
```

#### **SkillDescriptionText**
```
Position: X: 0, Y: -100, Z: 0
Size: Width: 320, Height: 100
Text: ""
Font Size: 16
Color: белый
Alignment: Top, Left
Wrapping: Enable
```

#### **SkillStatsText**
```
Position: X: 0, Y: -200, Z: 0
Size: Width: 320, Height: 60
Text: ""
Font Size: 14
Color: светло-серый (R: 200, G: 200, B: 200)
Alignment: Top, Left
```

### 9.5 Отключить панель по умолчанию
1. Выбери `SkillInfoPanel`
2. В Inspector, **сними галочку** слева от имени (deactivate)

Панель будет активироваться кодом при наведении на скилл!

---

## 🔗 ШАГ 10: Подключить SkillSelectionManager

### 10.1 Найти CharacterSelectionManager
1. В Hierarchy, найди объект с компонентом **CharacterSelectionManager**
   - Обычно это объект с именем типа `Managers` или `GameManager`

2. Если не можешь найти:
   - Создай новый GameObject: **Create Empty** → назови `Managers`
   - Добавь компонент **CharacterSelectionManager**

### 10.2 Добавить SkillSelectionManager
1. С выбранным объектом (где CharacterSelectionManager), нажми **Add Component**
2. Введи: `SkillSelectionManager`
3. Скрипт уже создан, просто добавь его

### 10.3 Назначить все ссылки
Теперь самая важная часть - соединить всё вместе!

```
SkillSelectionManager (Script):

├── Skill Database:
│   → Найди Assets/Data/SkillDatabase.asset
│   → Перетащи в это поле
│
├── Library Slots (Size: 6):
│   Element 0: → LibrarySlot_1
│   Element 1: → LibrarySlot_2
│   Element 2: → LibrarySlot_3
│   Element 3: → LibrarySlot_4
│   Element 4: → LibrarySlot_5
│   Element 5: → LibrarySlot_6
│
├── Equipped Slots (Size: 3):
│   Element 0: → EquippedSlot_1
│   Element 1: → EquippedSlot_2
│   Element 2: → EquippedSlot_3
│
├── Skill Info Panel:
│   → перетащи SkillInfoPanel
│
├── Skill Name Text:
│   → перетащи SkillInfoPanel/SkillNameText
│
├── Skill Description Text:
│   → перетащи SkillInfoPanel/SkillDescriptionText
│
└── Skill Stats Text:
    → перетащи SkillInfoPanel/SkillStatsText
```

### 10.4 Назначить SkillSelectionManager во все слоты
**Для каждого из 9 слотов** (6 library + 3 equipped):

1. Выбери слот (например, LibrarySlot_1)
2. Найди компонент **SkillSlotUI (Script)**
3. В поле **Skill Selection Manager**, перетащи объект с SkillSelectionManager

**Повтори для всех 9 слотов!**

---

## ✅ ШАГ 11: Финальная проверка

### 11.1 Проверить структуру
Открой Hierarchy и убедись что структура выглядит так:

```
Canvas
└── SkillSelectionPanel
    ├── Background ✓
    ├── TitleText ✓
    ├── SkillLibraryPanel ✓
    │   ├── LibraryTitle ✓
    │   ├── LibrarySlot_1 ✓ (с SkillSlotUI)
    │   ├── LibrarySlot_2 ✓
    │   ├── LibrarySlot_3 ✓
    │   ├── LibrarySlot_4 ✓
    │   ├── LibrarySlot_5 ✓
    │   └── LibrarySlot_6 ✓
    ├── EquippedSkillsPanel ✓
    │   ├── EquippedTitle ✓
    │   ├── EquippedSlot_1 ✓ (с SkillSlotUI)
    │   ├── EquippedSlot_2 ✓
    │   └── EquippedSlot_3 ✓
    └── SkillInfoPanel ✓ (deactivated)
```

### 11.2 Проверить Inspector настройки

**Все LibrarySlot (6 штук):**
- ✓ SkillSlotUI → Is Library Slot = TRUE
- ✓ SkillSlotUI → Skill Selection Manager назначен

**Все EquippedSlot (3 штуки):**
- ✓ SkillSlotUI → Is Library Slot = FALSE
- ✓ SkillSlotUI → Key Bind Text назначен (с текстом 1, 2, 3)
- ✓ SkillSlotUI → Skill Selection Manager назначен

**SkillSelectionManager:**
- ✓ Skill Database назначен
- ✓ Library Slots (Size: 6) - все назначены
- ✓ Equipped Slots (Size: 3) - все назначены
- ✓ Skill Info Panel назначен

### 11.3 Тест в Play Mode
1. Нажми **Play** ▶
2. Выбери любой класс (например, Warrior)
3. **Ожидаемый результат:**
   - В библиотеке появились 6 скиллов
   - Можно перетащить скилл из библиотеки в слот 1, 2 или 3
   - При наведении на скилл появляется тултип справа

---

## 🐛 Возможные проблемы и решения

### Проблема 1: Скиллы не загружаются
**Решение:**
- Убедись что SkillDatabase.asset существует в Assets/Data/
- Проверь что в SkillSelectionManager назначен Skill Database
- Открой Console (Ctrl+Shift+C) и посмотри логи

### Проблема 2: Drag & Drop не работает
**Решение:**
- Убедись что у всех слотов Image → Raycast Target = TRUE
- Проверь что EventSystem есть в сцене (должен создаться автоматически с Canvas)

### Проблема 3: Слоты отображаются неправильно
**Решение:**
- Проверь настройки GridLayoutGroup и HorizontalLayoutGroup
- Убедись что Cell Size и Spacing установлены правильно

### Проблема 4: Тултип не появляется
**Решение:**
- Убедись что SkillInfoPanel назначен в SkillSelectionManager
- Проверь что все текстовые поля назначены
- Панель должна быть деактивирована по умолчанию

---

## 🎉 Готово!

UI панель создана и полностью функциональна!

**Что дальше:**
- Создать префабы скелетов и медведя
- Добавить иконки для скиллов
- Добавить эффекты и звуки

**Время выполнения:** ~20 минут
**Результат:** Полностью рабочая UI панель для выбора скиллов с drag & drop!
