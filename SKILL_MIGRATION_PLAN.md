# План миграции на новые скиллы (SkillConfig) 🔄

## Проблема

**Текущее состояние:**
- SkillManager использует `List<SkillData>` (старая система)
- SkillExecutor использует `SkillConfig` (новая система) ✅
- ArenaManager загружает SkillData через SkillDatabase (старая)

**Что нужно:**
- SkillManager должен использовать `List<SkillConfig>` (новая система)
- Убрать зависимость от SkillDatabase (старая)
- Загружать скиллы напрямую из Resources по skillId

---

## Решение: Упростить SkillManager

**Идея:** SkillManager НЕ нужен! SkillExecutor УЖЕ управляет скиллами!

**Текущая архитектура:**
```
SkillManager (старая) → SkillData → НЕ РАБОТАЕТ
      ↓
SkillExecutor (новая) → SkillConfig → РАБОТАЕТ ✅
```

**Новая архитектура:**
```
SkillExecutor → SkillConfig (напрямую) ✅
```

---

## Что делаем

### Вариант 1: Удалить SkillManager (РАДИКАЛЬНО)

**Плюсы:**
- Убираем дублирование
- Упрощаем код
- SkillExecutor уже всё делает

**Минусы:**
- Нужно переписать ArenaManager
- Нужно переписать Character Selection
- Может сломать UI

### Вариант 2: Адаптировать SkillManager для SkillConfig (БЕЗОПАСНО)

**Плюсы:**
- Минимальные изменения
- Не ломаем существующий код
- Постепенная миграция

**Минусы:**
- SkillManager станет wrapper над SkillExecutor

---

## ВЫБИРАЕМ ВАРИАНТ 2 (безопасный)

### Шаг 1: Изменить SkillManager

**Изменения:**
```csharp
// БЫЛО:
public List<SkillData> equippedSkills;
public List<SkillData> allAvailableSkills;

// СТАЛО:
public List<SkillConfig> equippedSkills;
public List<SkillConfig> allAvailableSkills;
```

**Методы:**
```csharp
public void LoadEquippedSkills(List<int> skillIds)
{
    equippedSkills.Clear();

    foreach (int skillId in skillIds)
    {
        // Загружаем SkillConfig напрямую из Resources
        SkillConfig skill = LoadSkillConfigById(skillId);
        if (skill != null)
        {
            equippedSkills.Add(skill);
        }
    }
}

private SkillConfig LoadSkillConfigById(int skillId)
{
    // Определяем путь по skillId
    string path = GetSkillPathById(skillId);
    return Resources.Load<SkillConfig>(path);
}

private string GetSkillPathById(int skillId)
{
    // Warrior: 101-105
    if (skillId >= 101 && skillId <= 105)
    {
        switch (skillId)
        {
            case 101: return "Skills/Warrior_BattleRage";
            case 102: return "Skills/Warrior_DefensiveStance";
            case 103: return "Skills/Warrior_HammerThrow";
            case 104: return "Skills/Warrior_BattleHeal";
            case 105: return "Skills/Warrior_Charge";
        }
    }

    // Mage: 201-205
    // ... и т.д.
}
```

---

### Шаг 2: Изменить ArenaManager.LoadSkillsForClass()

**БЫЛО:**
```csharp
private void LoadSkillsForClass(SkillManager skillManager)
{
    SkillDatabase db = SkillDatabase.Instance;
    // ... load from SkillDatabase
}
```

**СТАЛО:**
```csharp
private void LoadSkillsForClass(SkillManager skillManager)
{
    string selectedClass = PlayerPrefs.GetString("SelectedCharacterClass", "");

    // Получаем skillIds для класса
    int[] skillIds = GetSkillIdsForClass(selectedClass);

    // Загружаем SkillConfig напрямую
    List<SkillConfig> skills = new List<SkillConfig>();
    foreach (int skillId in skillIds)
    {
        SkillConfig skill = LoadSkillConfigById(skillId);
        if (skill != null)
        {
            skills.Add(skill);
        }
    }

    skillManager.allAvailableSkills = skills;
}

private int[] GetSkillIdsForClass(string characterClass)
{
    switch (characterClass)
    {
        case "Warrior": return new int[] { 101, 102, 103, 104, 105 };
        case "Mage": return new int[] { 201, 202, 203, 204, 205 };
        case "Archer": return new int[] { 301, 302, 303, 304, 305 };
        case "Necromancer": return new int[] { 601, 602, 603, 604, 605 };
        case "Paladin": return new int[] { 501, 502, 503, 504, 505 };
        default: return new int[0];
    }
}
```

---

### Шаг 3: Изменить LoadEquippedSkillsFromPlayerPrefs()

**БЫЛО:**
```csharp
private void LoadEquippedSkillsFromPlayerPrefs(SkillManager skillManager)
{
    // ... load 3 skills
    for (int i = 0; i < Math.Min(3, skillManager.allAvailableSkills.Count); i++)
}
```

**СТАЛО:**
```csharp
private void LoadEquippedSkillsFromPlayerPrefs(SkillManager skillManager)
{
    // ... load 5 skills
    for (int i = 0; i < Math.Min(5, skillManager.allAvailableSkills.Count); i++)
    {
        skillManager.equippedSkills.Add(skillManager.allAvailableSkills[i]);
    }

    Debug.Log($"[ArenaManager] ✅ Автоэкипировано {skillManager.equippedSkills.Count} скиллов");
}
```

---

### Шаг 4: Связать SkillManager и SkillExecutor

**Проблема:** SkillExecutor УЖЕ управляет скиллами, но SkillManager тоже!

**Решение:** SkillManager просто передаёт скиллы в SkillExecutor

**В ArenaManager после загрузки скиллов:**
```csharp
private void SpawnSelectedCharacter()
{
    // ... spawn character ...

    // Загружаем скиллы в SkillManager
    LoadSkillsForClass(skillManager);
    LoadEquippedSkillsFromPlayerPrefs(skillManager);

    // ДОБАВИТЬ: Передаём скиллы в SkillExecutor
    SkillExecutor skillExecutor = spawnedCharacter.GetComponent<SkillExecutor>();
    if (skillExecutor != null)
    {
        // Присваиваем 5 скиллов по слотам 1-5
        for (int i = 0; i < skillManager.equippedSkills.Count && i < 5; i++)
        {
            skillExecutor.SetSkill(i + 1, skillManager.equippedSkills[i]); // Слот 1-5
        }

        Debug.Log($"[ArenaManager] ✅ Скиллы загружены в SkillExecutor");
    }
}
```

**SkillExecutor.SetSkill():**
```csharp
// Добавить метод в SkillExecutor:
public void SetSkill(int slotNumber, SkillConfig skill)
{
    if (slotNumber < 1 || slotNumber > 5)
    {
        Debug.LogError($"[SkillExecutor] Invalid slot number: {slotNumber}");
        return;
    }

    // Сохраняем в Dictionary или Array
    equippedSkills[slotNumber] = skill;
    Debug.Log($"[SkillExecutor] Скилл {skill.skillName} загружен в слот {slotNumber}");
}
```

---

## Итого: 4 файла для изменения

1. ✅ **SkillManager.cs** - изменить `SkillData` на `SkillConfig`
2. ✅ **ArenaManager.cs** - LoadSkillsForClass() + LoadEquippedSkillsFromPlayerPrefs()
3. ✅ **SkillExecutor.cs** - добавить SetSkill() метод
4. ✅ **Создать SkillConfigLoader.cs** (helper для загрузки по skillId)

---

## Время выполнения

- Шаг 1 (SkillManager): ~30 минут
- Шаг 2 (ArenaManager): ~20 минут
- Шаг 3 (SkillExecutor): ~10 минут
- Шаг 4 (SkillConfigLoader): ~20 минут
- **ИТОГО:** ~1.5 часа

---

## Готов начать? 🚀

**Начинаем с:**
1. Создать SkillConfigLoader.cs (helper class)
2. Изменить SkillManager.cs
3. Изменить ArenaManager.cs
4. Добавить SetSkill() в SkillExecutor.cs
5. Тестировать!
