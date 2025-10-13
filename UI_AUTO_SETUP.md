# ⚡ Автоматическое создание UI панели скиллов

## 🚀 Быстрый способ (30 секунд!)

Вместо ручного создания всей UI, используй автоматический скрипт!

---

## 📝 Инструкция

### Шаг 1: Открыть сцену CharacterSelection
1. В Unity, открой **Project** → **Scenes** → **CharacterSelection.unity**

### Шаг 2: Убедиться что Canvas существует
1. В **Hierarchy** должен быть объект **Canvas**
2. Если нет - создай: ПКМ → UI → Canvas

### Шаг 3: Запустить скрипт создания UI
1. В меню Unity, выбери: **Tools** → **Aetherion** → **Create Skill Selection UI**
2. Подожди 2-3 секунды
3. Готово! ✅

### Шаг 4: Назначить SkillDatabase
1. В Hierarchy появился `SkillSelectionPanel`
2. Найди объект с компонентом **SkillSelectionManager** (обычно на том же объекте что и CharacterSelectionManager)
3. В Inspector, в поле **Skill Database**, перетащи:
   - `Assets/Data/SkillDatabase.asset`

---

## ✅ Что создал скрипт:

```
✓ SkillSelectionPanel (корневая панель)
✓ Background (полупрозрачный фон)
✓ TitleText ("ВЫБЕРИТЕ 3 НАВЫКА")
✓ SkillLibraryPanel с 6 слотами (GridLayout 3x2)
✓ EquippedSkillsPanel с 3 слотами (HorizontalLayout)
✓ SkillInfoPanel (тултип при наведении)
✓ SkillSelectionManager (добавлен и настроен)
✓ Все 9 слотов с компонентом SkillSlotUI
✓ Все ссылки между компонентами
✓ Правильные настройки Layout Groups
✓ Тексты, иконки, стили
```

**Итого: Полностью рабочая UI панель за 30 секунд!**

---

## 🎨 Кастомизация (опционально)

Если хочешь изменить внешний вид:

### Изменить цвета
1. Выбери `SkillSelectionPanel/Background`
2. В Inspector → Image → Color → выбери свой цвет

### Изменить размер слотов
1. Выбери `SkillLibraryPanel`
2. В Inspector → Grid Layout Group → Cell Size → измени значения

### Изменить шрифт
1. Выбери любой текстовый элемент (например, TitleText)
2. В Inspector → TextMeshPro → Font Asset → выбери свой шрифт

### Изменить позицию панели
1. Выбери `SkillSelectionPanel`
2. В Inspector → RectTransform → Position → измени X, Y

---

## 🐛 Возможные проблемы

### Проблема: "Canvas не найден"
**Решение:** Создай Canvas (ПКМ в Hierarchy → UI → Canvas)

### Проблема: "TextMeshPro не найден"
**Решение:**
1. Окно "Import TMP Essentials" → нажми Import
2. Запусти скрипт снова

### Проблема: Скиллы не загружаются в UI
**Решение:**
1. Убедись что назначен SkillDatabase в SkillSelectionManager
2. Проверь что SkillDatabase.asset существует в Assets/Data/

---

## 📚 Сравнение методов

| Метод | Время | Сложность | Результат |
|-------|-------|-----------|-----------|
| **Автоматический скрипт** | 30 сек | Простая | Полностью готовая UI |
| **Ручное создание** | 20 мин | Средняя | Такой же результат |

**Рекомендация:** Используй автоматический скрипт! Экономит 19.5 минут 🚀

---

## 🎉 Что дальше?

После создания UI панели:

1. ✅ Нажми Play и проверь что всё работает
2. ✅ Выбери класс (Warrior, Mage, etc.)
3. ✅ В библиотеке должны появиться 6 скиллов
4. ✅ Перетащи скиллы в слоты 1, 2, 3

**Готово! UI работает!**

Теперь можешь переходить к:
- Добавлению иконок скиллов
- Созданию префабов скелетов и медведя
- Добавлению эффектов и звуков

---

## 📞 Полезные ссылки

- Детальное руководство: [UI_PANEL_DETAILED_GUIDE.md](UI_PANEL_DETAILED_GUIDE.md)
- Быстрый чеклист: [QUICK_SETUP_CHECKLIST.md](QUICK_SETUP_CHECKLIST.md)
- Система скиллов: [SKILL_SYSTEM_GUIDE.md](SKILL_SYSTEM_GUIDE.md)

**Удачи! 🎮**
