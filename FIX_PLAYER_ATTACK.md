# ✅ Исправление атак и скиллов в Arena

## Что было исправлено в коде:

### 1. SkillBarUI - исправлено сообщение об ошибке
**Было:**
```csharp
Debug.LogError($"[SkillBarUI] Должно быть ровно 3 слота! Найдено: {skillSlots.Length}");
```

**Стало:**
```csharp
Debug.LogError($"[SkillBarUI] Должно быть ровно 5 слотов! Найдено: {skillSlots.Length}");
```

### 2. ArenaManager - исправлен путь к BasicAttackConfig
**Было:**
```csharp
configPath = "Skills/BasicAttackConfig_Mage";
```

**Стало:**
```csharp
configPath = "skill old/BasicAttackConfig_Mage";
```

Изменено для всех классов (Mage, Archer, Warrior, Paladin, Rogue).

## Что нужно сделать в Unity Editor:

### 1. Включить компонент PlayerAttackNew

#### В Arena Scene:

1. Найди объект игрока (обычно это `ArcherModel`, `WarriorModel` и т.д.)
   - Можно найти через поиск по тегу `Player`

2. Выбери этот объект в Hierarchy

3. В Inspector найди компонент **PlayerAttackNew (Script)**

4. **ВКЛЮЧИ** этот компонент (checkbox слева от названия должен быть ✓)

5. Убедись что поле **Attack Config** назначено:
   - Если пусто, перетащи соответствующий BasicAttackConfig:
     - Archer → `Assets/Resources/skill old/BasicAttackConfig_Archer`
     - Warrior → `Assets/Resources/skill old/BasicAttackConfig_Warrior`
     - Mage → `Assets/Resources/skill old/BasicAttackConfig_Mage`
     - и т.д.

### 2. Добавить 5 слотов в SkillBar (если ещё не сделано)

1. Найди объект `SkillBar` в Hierarchy (обычно это Canvas → SkillBar)

2. Проверь сколько дочерних объектов `SkillSlotBar`:
   ```
   SkillBar
   ├── SkillSlot_0
   ├── SkillSlot_1
   ├── SkillSlot_2
   ├── SkillSlot_3  ← Должен быть
   └── SkillSlot_4  ← Должен быть
   ```

3. Если слотов только 3:
   - Дублируй `SkillSlot_0` два раза (Ctrl+D)
   - Переименуй копии в `SkillSlot_3` и `SkillSlot_4`
   - Расположи их визуально рядом

### 3. Проверка работоспособности

После этих изменений:

✅ **BasicAttackConfig загружается:**
```
[ArenaManager] Загружен BasicAttackConfig для Archer из skill old/BasicAttackConfig_Archer
```

✅ **PlayerAttackNew работает:**
```
[PlayerAttackNew] ✅ Базовая атака настроена для ArcherModel
```

✅ **SkillBar находит 5 слотов:**
```
[SkillBarUI] ✅ Найдено 5 слотов скиллов
```

✅ **Скиллы загружаются:**
```
[SkillBarUI] Слот 1: Rain Of Arrows
[SkillBarUI] Слот 2: Stunning Shot
...
[SkillBarUI] Слот 5: Deadly Precision
```

## Как проверить что всё работает:

1. **Базовая атака (ЛКМ):**
   - Нажми ЛКМ по врагу
   - Должен вылететь снаряд (стрела для лучника)
   - Враг должен получить урон

2. **Скиллы (клавиши 1-5):**
   - Нажми клавишу 1 (первый скилл)
   - Должен активироваться скилл
   - Проверь что работают все 5 клавиш

3. **Проверь Console на ошибки:**
   - Не должно быть:
     - ❌ "BasicAttackConfig НЕ НАЗНАЧЕН"
     - ❌ "Не найден BasicAttackConfig"
     - ❌ "Должно быть ровно 3 слота"

## Типичные проблемы:

### "PlayerAttackNew НЕ НАЗНАЧЕН"
**Решение:** Включи компонент PlayerAttackNew в Inspector (checkbox)

### "BasicAttackConfig не найден"
**Решение:** Проверь что файлы существуют в `Assets/Resources/skill old/`

### "Скиллы не работают"
**Решение:**
1. Проверь что SkillBar имеет 5 слотов
2. Проверь что скиллы загрузились (смотри Console)
3. Убедись что есть мана для использования скилла

## Файлы изменены:

1. `Assets/Scripts/UI/Skills/SkillBarUI.cs` - строка 24
2. `Assets/Scripts/Arena/ArenaManager.cs` - строки 1506-1518
