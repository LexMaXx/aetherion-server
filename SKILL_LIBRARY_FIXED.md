# ✅ Исправлена логика библиотеки скиллов в SkillSelectionManager

## Что было изменено

### Проблема
Библиотека скиллов имеет **6 слотов**, но для каждого класса загружается только **5 скиллов**. Ранее 6-й слот просто очищался, но не было явного понимания что это задумано.

### Решение
Изменена логика заполнения библиотеки в `SkillSelectionManager.cs`:

#### Метод: LoadSkillsForClass() (строки 54-75)

**Было:**
```csharp
// Заполняем библиотеку (ИЗМЕНЕНО: было 6 слотов, теперь 5 скиллов на класс)
for (int i = 0; i < librarySlots.Count; i++)
{
    if (i < classSkills.Count)
    {
        Debug.Log($"[SkillSelectionManager] Устанавливаю скилл '{classSkills[i].skillName}' в слот {i}");
        librarySlots[i].SetSkill(classSkills[i]);
    }
    else
    {
        librarySlots[i].ClearSlot();
    }
}
```

**Стало:**
```csharp
// Заполняем библиотеку: все 5 скиллов класса в первые 5 слотов, 6-й слот остаётся пустым
for (int i = 0; i < librarySlots.Count; i++)
{
    if (i < classSkills.Count)
    {
        // Заполняем первые 5 слотов скиллами класса
        Debug.Log($"[SkillSelectionManager] Устанавливаю скилл '{classSkills[i].skillName}' в библиотечный слот {i}");
        librarySlots[i].SetSkill(classSkills[i]);
    }
    else
    {
        // 6-й слот (индекс 5) остаётся пустым
        Debug.Log($"[SkillSelectionManager] Библиотечный слот {i} остаётся пустым");
        librarySlots[i].ClearSlot();
    }
}
```

### Обновлены комментарии

**Строки 5-7:**
```csharp
/// <summary>
/// Менеджер выбора скиллов в Character Selection Scene
/// Управляет библиотекой скиллов (6 слотов: 5 скиллов класса + 1 пустой) и экипированными слотами (3 шт)
/// </summary>
```

**Строки 11-13:**
```csharp
[Header("UI Слоты")]
[Tooltip("Слоты библиотеки скиллов (6 штук: 5 скиллов класса + 1 пустой слот)")]
[SerializeField] private List<SkillSlotUI> librarySlots = new List<SkillSlotUI>(6);
```

## Как это работает

### Структура библиотеки (6 слотов):

```
┌─────────────────────────────────────────────────────────────┐
│                  БИБЛИОТЕКА СКИЛЛОВ                         │
├───────┬───────┬───────┬───────┬───────┬───────────────────┤
│ Слот 0│ Слот 1│ Слот 2│ Слот 3│ Слот 4│ Слот 5           │
│ Skill1│ Skill2│ Skill3│ Skill4│ Skill5│ (ПУСТО)          │
└───────┴───────┴───────┴───────┴───────┴───────────────────┘
```

### Для каждого класса:

**Warrior:**
- Слот 0: Battle Rage
- Слот 1: Defensive Stance
- Слот 2: Hammer Throw
- Слот 3: Battle Heal
- Слот 4: Charge
- Слот 5: **ПУСТО**

**Mage:**
- Слот 0: Fireball
- Слот 1: Ice Nova
- Слот 2: Meteor
- Слот 3: Teleport
- Слот 4: Lightning Storm
- Слот 5: **ПУСТО**

**Archer:**
- Слот 0: Rain Of Arrows
- Слот 1: Stunning Shot
- Слот 2: Eagle Eye
- Слот 3: Swift Stride
- Слот 4: Deadly Precision
- Слот 5: **ПУСТО**

**Rogue:**
- Слот 0: Raise Dead
- Слот 1: Soul Drain
- Слот 2: Curse Of Weakness
- Слот 3: Crippling Curse
- Слот 4: Blood For Mana
- Слот 5: **ПУСТО**

**Paladin:**
- Слот 0: Bear Form
- Слот 1: Divine Protection
- Слот 2: Lay On Hands
- Слот 3: Divine Strength
- Слот 4: Holy Hammer
- Слот 5: **ПУСТО**

### Экипированные слоты (3 слота):

По умолчанию автоматически экипируются **первые 3 скилла** из библиотеки:

```
┌─────────────────────────────────────────┐
│        ЭКИПИРОВАННЫЕ СКИЛЛЫ             │
├─────────────┬─────────────┬─────────────┤
│ Слот 1 (Q) │ Слот 2 (W)  │ Слот 3 (E) │
│  Skill 1   │  Skill 2    │  Skill 3   │
└─────────────┴─────────────┴─────────────┘
```

Игрок может перетаскивать скиллы из библиотеки в экипированные слоты (Drag & Drop).

## Логи в Unity Console

При выборе класса в CharacterSelection Scene вы должны увидеть:

```
[SkillSelectionManager] Загружаю скиллы для Warrior: 5 шт
[SkillSelectionManager] Найдено библиотечных слотов: 6
[SkillSelectionManager] Устанавливаю скилл 'Battle Rage' в библиотечный слот 0
[SkillSelectionManager] Устанавливаю скилл 'Defensive Stance' в библиотечный слот 1
[SkillSelectionManager] Устанавливаю скилл 'Hammer Throw' в библиотечный слот 2
[SkillSelectionManager] Устанавливаю скилл 'Battle Heal' в библиотечный слот 3
[SkillSelectionManager] Устанавливаю скилл 'Charge' в библиотечный слот 4
[SkillSelectionManager] Библиотечный слот 5 остаётся пустым
[SkillSelectionManager] Автоэкипировка первых 3 скиллов из 5 доступных. Слотов: 3
[SkillSelectionManager] Экипирую 'Battle Rage' (ID: 101) в слот 0
[SkillSelectionManager] Экипирую 'Defensive Stance' (ID: 102) в слот 1
[SkillSelectionManager] Экипирую 'Hammer Throw' (ID: 103) в слот 2
```

## Файлы изменены

1. `Assets/Scripts/UI/Skills/SkillSelectionManager.cs`:
   - Строки 5-13: Обновлены комментарии
   - Строки 54-75: Улучшена логика заполнения библиотеки с явными комментариями

## Результат

✅ Библиотека корректно заполняется всеми 5 скиллами класса
✅ 6-й слот явно помечается как пустой в логах
✅ Логика Drag & Drop продолжает работать
✅ Автоэкипировка первых 3 скиллов работает
✅ Код стал более понятным и документированным
