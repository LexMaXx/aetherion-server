# 🔧 Исправление системы скиллов - Руководство

## ✅ Что исправлено:

### 1. **NullReferenceException в CreateSkillSelectionUI.cs**
- ✅ Добавлена проверка `slotUI != null`
- ✅ Добавлены недостающие поля в SkillSlotUI:
  - `skillNameText`
  - `emptyText`
  - `keyBindText`
  - `skillSelectionManager`
  - `isLibrarySlot`

### 2. **SkillDatabase.asset перемещён**
- ✅ Скопирован из `Assets/Resources/` в `Assets/Data/`
- ✅ Теперь доступен по пути: `Assets/Data/SkillDatabase.asset`

---

## 🚀 Как создать UI панель сейчас:

### Шаг 1: Создать скиллы (если их нет)

1. В Unity, меню: **Tools → Aetherion → Create Skill Database**
2. Подожди 5-10 секунд
3. В Console появится:
   ```
   ✅ Создано 30 скиллов!
   ✅ SkillDatabase обновлена!
   ```

### Шаг 2: Создать UI панель

1. Открой сцену **CharacterSelection**
2. Меню: **Tools → Aetherion → Create Skill Selection UI**
3. Подожди 2-3 секунды
4. В Hierarchy появится `SkillSelectionPanel`

### Шаг 3: Назначить SkillDatabase

1. Найди объект с **SkillSelectionManager** (обычно на том же объекте что CharacterSelectionManager)
2. В Inspector, поле **Skill Database** → перетащи:
   - `Assets/Data/SkillDatabase.asset`
3. Готово! ✅

---

## 🎮 Тест

1. Нажми **Play** ▶
2. Выбери класс (Warrior, Mage, etc.)
3. Должны появиться 6 скиллов в библиотеке
4. Drag & Drop скиллы в слоты 1, 2, 3

---

## 🐛 Если возникли ошибки:

### Ошибка: "SkillSlotUI не найден"
**Решение:**
1. Проверь что файл существует: `Assets/Scripts/UI/Skills/SkillSlotUI.cs`
2. Проверь Console на ошибки компиляции
3. Перезапусти Unity (File → Reimport All)

### Ошибка: "SkillDatabase не найдена"
**Решение:**
1. Запусти: **Tools → Aetherion → Create Skill Database**
2. Проверь что файл создался: `Assets/Data/SkillDatabase.asset`
3. Назначь его вручную в SkillSelectionManager

### Ошибка: "Скиллы не загружаются"
**Решение:**
1. Открой `Assets/Data/SkillDatabase.asset` в Inspector
2. Проверь что списки скиллов не пустые (должно быть 6 в каждом)
3. Если пусто → запусти Create Skill Database снова

### UI создалась, но выглядит неправильно
**Решение:**
1. Удали `SkillSelectionPanel` из Hierarchy
2. Запусти Create Skill Selection UI снова
3. Проверь что Canvas существует в сцене

---

## 📋 Структура после настройки:

```
Assets/
├── Data/
│   └── SkillDatabase.asset ✅ (база всех скиллов)
│
├── Resources/
│   ├── Skills/ (30 SkillData assets)
│   │   ├── Warrior/
│   │   │   ├── ShieldBash.asset
│   │   │   ├── Whirlwind.asset
│   │   │   └── ... (6 всего)
│   │   ├── Mage/ (6 скиллов)
│   │   ├── Archer/ (6 скиллов)
│   │   ├── Rogue/ (6 скиллов)
│   │   └── Paladin/ (6 скиллов)
│   └── SkillDatabase.asset (старая копия)
│
└── Scripts/
    ├── Data/
    │   ├── SkillData.cs
    │   └── SkillDatabase.cs
    ├── Skills/
    │   ├── SkillManager.cs
    │   ├── ActiveEffect.cs
    │   └── SummonedCreature.cs
    ├── UI/Skills/
    │   ├── SkillSlotUI.cs
    │   └── SkillSelectionManager.cs
    └── Editor/
        ├── CreateSkillDatabase.cs
        └── CreateSkillSelectionUI.cs
```

---

## 🎯 Следующие шаги:

После того как UI работает:

1. ✅ **Добавить иконки** (30 штук)
   - Используй промпты из `PROMPTS_READY_TO_USE.txt`
   - Генерируй в Midjourney/DALL-E
   - Сохрани в `Assets/UI/Icons/Skills/`

2. ✅ **Создать префабы**
   - Скелеты для Rogue (3 штуки)
   - Медведь для Paladin

3. ✅ **Добавить эффекты**
   - Particle systems из Asset Store
   - Звуки из freesound.org

**Подробности:** См. файлы `QUICK_SETUP_CHECKLIST.md` и `SKILL_SYSTEM_SETUP_GUIDE.md`

---

## 💡 Быстрая проверка состояния

Запусти эти команды в Unity Console для проверки:

```csharp
// Проверить что SkillDatabase существует
Debug.Log(Resources.Load<SkillDatabase>("SkillDatabase") != null ? "✅ SkillDatabase найдена" : "❌ SkillDatabase не найдена");

// Проверить количество скиллов
var db = Resources.Load<SkillDatabase>("SkillDatabase");
if (db != null) {
    Debug.Log($"Warrior: {db.GetSkillCountForClass(CharacterClass.Warrior)}");
    Debug.Log($"Mage: {db.GetSkillCountForClass(CharacterClass.Mage)}");
    Debug.Log($"Archer: {db.GetSkillCountForClass(CharacterClass.Archer)}");
    Debug.Log($"Rogue: {db.GetSkillCountForClass(CharacterClass.Rogue)}");
    Debug.Log($"Paladin: {db.GetSkillCountForClass(CharacterClass.Paladin)}");
}
```

Ожидаемый результат: по 6 скиллов для каждого класса

---

**Готово! Система скиллов настроена и готова к работе! 🎉**
