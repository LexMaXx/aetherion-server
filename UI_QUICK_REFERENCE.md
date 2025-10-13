# 🎯 UI панель - Быстрая справка

## ⚡ Самый быстрый способ (30 секунд)

```
1. Открой CharacterSelection сцену
2. Tools → Aetherion → Create Skill Selection UI
3. Назначь SkillDatabase в SkillSelectionManager
4. Готово! ✅
```

**Подробности:** [UI_AUTO_SETUP.md](UI_AUTO_SETUP.md)

---

## 📐 Структура UI (для справки)

```
SkillSelectionPanel (1100x900, справа)
│
├── Background (полупрозрачный чёрный)
├── TitleText ("ВЫБЕРИТЕ 3 НАВЫКА")
│
├── SkillLibraryPanel (GridLayout 3x2)
│   ├── LibrarySlot_1 (150x180) + SkillSlotUI
│   ├── LibrarySlot_2
│   ├── LibrarySlot_3
│   ├── LibrarySlot_4
│   ├── LibrarySlot_5
│   └── LibrarySlot_6
│
├── EquippedSkillsPanel (HorizontalLayout)
│   ├── EquippedSlot_1 (180x180) + SkillSlotUI + "1"
│   ├── EquippedSlot_2 + "2"
│   └── EquippedSlot_3 + "3"
│
└── SkillInfoPanel (тултип, скрыт по умолчанию)
    ├── SkillNameText
    ├── SkillDescriptionText
    └── SkillStatsText
```

---

## 🔧 Каждый слот содержит

```
SkillSlot (GameObject)
├── Image (фон слота)
├── SkillSlotUI.cs (компонент)
│   ├── isLibrarySlot: true/false
│   ├── skillSelectionManager: ссылка на менеджер
│   └── iconImage, skillNameText, emptyText, keyBindText
│
└── Children:
    ├── Icon (Image) - иконка скилла
    ├── EmptyText (TMP) - "Пусто"
    ├── SkillNameText (TMP) - название скилла
    └── KeyBindText (TMP) - "1", "2", "3" (только для equipped)
```

---

## ⚙️ Настройки компонентов

### GridLayoutGroup (библиотека)
```
Cell Size: 150x180
Spacing: 30x30
Constraint: Fixed Column Count = 3
```

### HorizontalLayoutGroup (выбранные)
```
Spacing: 40
Child Control Size: FALSE
Child Force Expand: FALSE
```

### SkillSlotUI (Library)
```
Is Library Slot: ✓ TRUE
Skill Selection Manager: [назначить]
```

### SkillSlotUI (Equipped)
```
Is Library Slot: ✗ FALSE
Key Bind Text: [назначить TextMeshPro с "1", "2", "3"]
Skill Selection Manager: [назначить]
```

---

## 🎨 Цветовая схема (по умолчанию)

```
Background: RGB(0, 0, 0), Alpha 200 (чёрный 78%)
Title: белый, размер 42, bold
Library slots: RGB(40, 40, 40) - тёмно-серый
Equipped slots: RGB(20, 60, 20) - тёмно-зелёный
Library title: RGB(255, 220, 100) - золотистый
Equipped title: RGB(100, 255, 100) - зелёный
Key bind text: жёлтый, размер 32, bold
```

---

## 📏 Точные координаты

### SkillSelectionPanel
```
Anchor: Right-Middle (1, 0.5)
Position: (-50, 0, 0)
Size: (1100, 900)
```

### SkillLibraryPanel
```
Anchor: Top-Center relative to parent
Position: (0, -150, 0)
Size: (1000, 450)
```

### EquippedSkillsPanel
```
Anchor: Bottom-Center relative to parent
Position: (0, 80, 0)
Size: (700, 220)
```

### SkillInfoPanel
```
Anchor: Top-Right relative to parent
Position: (-20, -300, 0)
Size: (350, 250)
Active: FALSE (скрыта)
```

---

## ✅ Чеклист после создания

- [ ] SkillDatabase назначен в SkillSelectionManager
- [ ] Все 6 library slots имеют isLibrarySlot = true
- [ ] Все 3 equipped slots имеют isLibrarySlot = false
- [ ] Все 3 equipped slots имеют keyBindText с "1", "2", "3"
- [ ] Все 9 слотов ссылаются на один SkillSelectionManager
- [ ] SkillInfoPanel выключен (SetActive = false)
- [ ] В Game View панель справа, не перекрывает персонажа

---

## 🎮 Тест в Play Mode

```
1. Play ▶
2. Выбери класс (Warrior)
3. Проверь:
   ✓ 6 скиллов в библиотеке
   ✓ 3 пустых слота внизу
   ✓ Можно перетащить скилл в слот
   ✓ При наведении показывается тултип
   ✓ Логи в Console без ошибок
```

---

## 📚 Все руководства

1. **Автоматический скрипт (30 сек):** [UI_AUTO_SETUP.md](UI_AUTO_SETUP.md)
2. **Ручное создание (20 мин):** [UI_PANEL_DETAILED_GUIDE.md](UI_PANEL_DETAILED_GUIDE.md)
3. **Общий чеклист:** [QUICK_SETUP_CHECKLIST.md](QUICK_SETUP_CHECKLIST.md)

**Рекомендация:** Используй автоматический скрипт! 🚀
