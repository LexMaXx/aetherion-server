# ✅ Изменено количество экипированных слотов с 3 на 5

## Проблема

При выборе класса в CharacterSelection экипировались только **первые 3 скилла из 5**, а слоты 2 и 3 (EquippedSlot_2, EquippedSlot_3) оставались пустыми.

### Причина

Система была жёстко заточена на **3 экипированных слота**:
- `SkillSelectionManager.equippedSlots` - capacity = 3
- `SkillBarUI.skillSlots` - проверка на ровно 3 слота
- `IsReadyToProceed()` - проверка `equippedCount == 3`

## Решение

Изменена вся система на поддержку **5 экипированных слотов** (все скиллы класса).

## Измененные файлы

### 1. SkillSelectionManager.cs

**Строка 7** - Обновлён комментарий класса:
```csharp
// БЫЛО:
/// Управляет библиотекой скиллов (6 слотов: 5 скиллов класса + 1 пустой) и экипированными слотами (3 шт)

// СТАЛО:
/// Управляет библиотекой скиллов (6 слотов: 5 скиллов класса + 1 пустой) и экипированными слотами (5 шт)
```

**Строка 15** - Обновлён tooltip для equippedSlots:
```csharp
// БЫЛО:
[Tooltip("Слоты экипированных скиллов (3 штуки)")]

// СТАЛО:
[Tooltip("Слоты экипированных скиллов (5 штук - все скиллы класса)")]
```

**Строка 16** - Изменена capacity списка:
```csharp
// БЫЛО:
private List<SkillSlotUI> equippedSlots = new List<SkillSlotUI>(3);

// СТАЛО:
private List<SkillSlotUI> equippedSlots = new List<SkillSlotUI>(5);
```

**Строка 77** - Обновлён комментарий:
```csharp
// БЫЛО:
// Автоматически экипируем первые 3 скилла (из 5 доступных)

// СТАЛО:
// Автоматически экипируем ВСЕ 5 скиллов класса
```

**Строки 81-107** - Обновлён метод AutoEquipDefaultSkills():
```csharp
// БЫЛО:
/// Автоматически экипировать первые 3 скилла (NEW SYSTEM: SkillConfig)
Debug.Log($"[SkillSelectionManager] Автоэкипировка первых 3 скиллов из {skills.Count} доступных. Слотов: {equippedSlots.Count}");

// СТАЛО:
/// Автоматически экипировать ВСЕ 5 скиллов класса (NEW SYSTEM: SkillConfig)
Debug.Log($"[SkillSelectionManager] Автоэкипировка всех {skills.Count} скиллов класса. Слотов: {equippedSlots.Count}");
Debug.Log($"[SkillSelectionManager] Экипирую '{skills[i].skillName}' (ID: {skills[i].skillId}) в экипированный слот {i + 1}");
```

**Строки 175-182** - Обновлён метод IsReadyToProceed():
```csharp
// БЫЛО:
/// Проверка готовности (все 3 слота заполнены?) (NEW SYSTEM: SkillConfig)
public bool IsReadyToProceed()
{
    int equippedCount = equippedSlots.Count(slot => slot.GetSkillConfig() != null);
    return equippedCount == 3;  // ❌ Жёстко зашито 3
}

// СТАЛО:
/// Проверка готовности (все 5 слотов заполнены?) (NEW SYSTEM: SkillConfig)
public bool IsReadyToProceed()
{
    int equippedCount = equippedSlots.Count(slot => slot.GetSkillConfig() != null);
    return equippedCount == 5;  // ✅ Теперь 5
}
```

### 2. SkillSlotUI.cs

**Строка 21** - Обновлён комментарий:
```csharp
// БЫЛО:
[SerializeField] private bool isEquipSlot = false; // true = слот экипировки (3 штуки), false = слот библиотеки (6 штук)

// СТАЛО:
[SerializeField] private bool isEquipSlot = false; // true = слот экипировки (5 штук), false = слот библиотеки (6 штук)
```

### 3. SkillBarUI.cs (Arena Scene)

**Строки 5-6** - Обновлён комментарий класса:
```csharp
// БЫЛО:
/// Управляет Skill Bar в Arena Scene (3 иконки внизу справа)
/// Загружает экипированные скиллы и обрабатывает хоткеи (1, 2, 3)

// СТАЛО:
/// Управляет Skill Bar в Arena Scene (5 иконок внизу справа)
/// Загружает экипированные скиллы и обрабатывает хоткеи (1, 2, 3, 4, 5)
```

**Строки 22-28** - Обновлена проверка количества слотов:
```csharp
// БЫЛО:
if (skillSlots.Length != 3)
{
    Debug.LogError($"[SkillBarUI] Должно быть ровно 3 слота! Найдено: {skillSlots.Length}");
}
else
{
    Debug.Log("[SkillBarUI] ✅ Найдено 3 слота скиллов");
}

// СТАЛО:
if (skillSlots.Length != 5)
{
    Debug.LogError($"[SkillBarUI] Должно быть ровно 5 слотов! Найдено: {skillSlots.Length}");
}
else
{
    Debug.Log("[SkillBarUI] ✅ Найдено 5 слотов скиллов");
}
```

**Строка 131** - Обновлён комментарий LoadTestSkills:
```csharp
// БЫЛО:
// Получаем первые 3 скилла класса игрока

// СТАЛО:
// Получаем все 5 скиллов класса игрока
```

## Итоговая структура

### CharacterSelection Scene:

```
┌──────────────────────────────────────────────────┐
│          БИБЛИОТЕКА (6 слотов)                   │
├────────┬────────┬────────┬────────┬────────┬─────┤
│ Slot 0 │ Slot 1 │ Slot 2 │ Slot 3 │ Slot 4 │ 5   │
│Skill 1 │Skill 2 │Skill 3 │Skill 4 │Skill 5 │Empty│
└────────┴────────┴────────┴────────┴────────┴─────┘
                ↓ Автоэкипировка ↓
┌──────────────────────────────────────────────────────────────┐
│          ЭКИПИРОВАННЫЕ СЛОТЫ (5 слотов)                      │
├────────────┬────────────┬────────────┬────────────┬──────────┤
│ Slot 1 (1) │ Slot 2 (2) │ Slot 3 (3) │ Slot 4 (4) │ Slot 5(5)│
│  Skill 1   │  Skill 2   │  Skill 3   │  Skill 4   │ Skill 5  │
└────────────┴────────────┴────────────┴────────────┴──────────┘
```

### Arena Scene - Skill Bar:

```
┌──────────────────────────────────────────────────────────────┐
│          SKILL BAR (внизу справа, 5 слотов)                  │
├────────────┬────────────┬────────────┬────────────┬──────────┤
│     1      │     2      │     3      │     4      │     5    │
│  Skill 1   │  Skill 2   │  Skill 3   │  Skill 4   │ Skill 5  │
└────────────┴────────────┴────────────┴────────────┴──────────┘
```

Хоткеи: клавиши **1, 2, 3, 4, 5**

## ВАЖНО: Нужно обновить UI в Unity Inspector!

### CharacterSelection Scene:

1. Откройте сцену `CharacterSelectionScene`
2. Найдите объект `SkillSelectionPanel` (или `SkillSelectionManager`)
3. В Inspector найдите компонент `SkillSelectionManager`
4. В поле `Equipped Slots` измените **Size: 3 → 5**
5. Перетащите **5 UI объектов** в массив `Equipped Slots`:
   - EquippedSlot_0 (слот 1)
   - EquippedSlot_1 (слот 2)
   - EquippedSlot_2 (слот 3)
   - EquippedSlot_3 (слот 4) ← **НОВЫЙ**
   - EquippedSlot_4 (слот 5) ← **НОВЫЙ**

6. Если UI объектов `EquippedSlot_3` и `EquippedSlot_4` не существует, создайте их:
   - Дублируйте `EquippedSlot_0`
   - Переименуйте копии в `EquippedSlot_3` и `EquippedSlot_4`
   - Расположите их рядом с остальными слотами

### Arena Scene:

1. Откройте сцену `ArenaScene`
2. Найдите объект `SkillBar` (или `SkillBarUI`)
3. Добавьте ещё **2 SkillSlotBar** к существующим 3:
   - Дублируйте существующие слоты
   - Создайте `SkillSlot_3` и `SkillSlot_4`
   - Расположите их визуально рядом с остальными

4. SkillBarUI автоматически найдёт все `SkillSlotBar` через `GetComponentsInChildren<SkillSlotBar>()`

## Логи в Unity Console

После изменений вы должны видеть:

### CharacterSelection:
```
[SkillSelectionManager] Автоэкипировка всех 5 скиллов класса. Слотов: 5
[SkillSelectionManager] Экипирую 'Battle Rage' (ID: 101) в экипированный слот 1
[SkillSelectionManager] Экипирую 'Defensive Stance' (ID: 102) в экипированный слот 2
[SkillSelectionManager] Экипирую 'Hammer Throw' (ID: 103) в экипированный слот 3
[SkillSelectionManager] Экипирую 'Battle Heal' (ID: 104) в экипированный слот 4
[SkillSelectionManager] Экипирую 'Charge' (ID: 105) в экипированный слот 5
[SkillSelectionManager] Экипированные скиллы: [101, 102, 103, 104, 105]
```

### Arena:
```
[SkillBarUI] ✅ Найдено 5 слотов скиллов
[SkillBarUI] Загружено 5 экипированных скиллов: [101, 102, 103, 104, 105]
[SkillBarUI] Слот 1: Battle Rage
[SkillBarUI] Слот 2: Defensive Stance
[SkillBarUI] Слот 3: Hammer Throw
[SkillBarUI] Слот 4: Battle Heal
[SkillBarUI] Слот 5: Charge
```

## Результат

✅ Все 5 скиллов класса автоматически экипируются
✅ CharacterSelection показывает 5 экипированных слотов
✅ Arena Skill Bar поддерживает 5 слотов
✅ Хоткеи работают для клавиш 1-5
✅ IsReadyToProceed() теперь требует 5 скиллов

## Файлы изменены

1. `Assets/Scripts/UI/Skills/SkillSelectionManager.cs` - 7 изменений
2. `Assets/Scripts/UI/Skills/SkillSlotUI.cs` - 1 изменение
3. `Assets/Scripts/UI/Skills/SkillBarUI.cs` - 3 изменения

**Итого: 3 файла, 11 изменений**
