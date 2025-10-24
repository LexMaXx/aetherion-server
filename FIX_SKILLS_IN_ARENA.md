# 🔧 ИСПРАВЛЕНИЕ ПРОБЛЕМ СО СКИЛЛАМИ В АРЕНЕ

## Дата: 2025-10-22

---

## ❌ ТЕКУЩИЕ ПРОБЛЕМЫ:

1. **Скиллы не работают** при нажатии клавиш 1-5
2. **Показываются только 2 скилла** вместо 5
3. **Иконки одинаковые** у всех классов (Hammer Throw и Holy Hammer)

### На скриншоте видно:
- `Equipped Skills`: только 2 элемента
  - Element 0: `Warrior_HammerT` (Hammer Throw)
  - Element 1: `Paladin_HolyHam` (Holy Hammer)
- Это **НЕПРАВИЛЬНО** - должно быть 5 скиллов конкретного класса!

---

## 🔍 ДИАГНОСТИКА - ВЫПОЛНИ В UNITY:

### Шаг 1: Проверь загрузку всех скиллов

В Unity Editor:
```
Top Menu → Aetherion → Debug → Test Skill Loading - All Classes
```

**Проверь Console:**
- Должно быть: `✅ Успешно загружено: 25/25`
- Если меньше - значит какие-то SkillConfig файлы не найдены

---

### Шаг 2: Проверь иконки

В Unity Editor:
```
Top Menu → Aetherion → Debug → Check All Skill Icons
```

**Проверь Console:**
- Должно быть: `✅ С иконками: 25`
- Если меньше - нужно назначить иконки в Inspector

---

### Шаг 3: Очисти PlayerPrefs

В Unity Editor:
```
Top Menu → Aetherion → Debug → Clear Equipped Skills PlayerPrefs
```

**Это обязательно!** Сейчас там сохранены старые данные (те 2 скилла).

---

### Шаг 4: Проверь что класс правильно сохраняется

В Unity Editor:
```
Top Menu → Aetherion → Debug → Show Current Equipped Skills
```

**Проверь Console:**
- `SelectedCharacterClass` должен быть: `Warrior`, `Mage`, `Archer`, `Necromancer` или `Paladin`
- Если пусто или неправильный класс - это проблема в CharacterSelection сцене

---

### Шаг 5: Тест в Arena

1. **Play → CharacterSelectionScene**
2. **Выбери Warrior**
3. **Enter Arena**
4. **Посмотри Console - должно быть:**

```
[SkillConfigLoader] ✅ Загружен скилл: Battle Rage (ID: 101)
[SkillConfigLoader] ✅ Загружен скилл: Defensive Stance (ID: 102)
[SkillConfigLoader] ✅ Загружен скилл: Hammer Throw (ID: 103)
[SkillConfigLoader] ✅ Загружен скилл: Battle Heal (ID: 104)
[SkillConfigLoader] ✅ Загружен скилл: Charge (ID: 105)
[ArenaManager] ✅ Автоэкипировано 5 скиллов по умолчанию
[SkillExecutor] ✅ Скилл 'Battle Rage' установлен в слот 1
[SkillExecutor] ✅ Скилл 'Defensive Stance' установлен в слот 2
[SkillExecutor] ✅ Скилл 'Hammer Throw' установлен в слот 3
[SkillExecutor] ✅ Скилл 'Battle Heal' установлен в слот 4
[SkillExecutor] ✅ Скилл 'Charge' установлен в слот 5
```

5. **Кликни на персонажа в Hierarchy:**
   - Должен называться `Warrior_Model (Clone)`
   - Посмотри на `Skill Manager (Script)`:
     - `Equipped Skills` → **5 элементов** (все Warrior скиллы)
     - `All Available Skills` → **5 элементов** (все Warrior скиллы)

6. **Нажми клавиши 1-5:**
   - Должны активироваться скиллы
   - В Console должны появиться логи использования

---

## 🛠️ ВОЗМОЖНЫЕ ИСПРАВЛЕНИЯ:

### Исправление 1: Если скиллы не загружаются (меньше 25/25)

**Проблема:** Файлы SkillConfig не найдены

**Проверь:**
```bash
Assets/Resources/Skills/Warrior_BattleRage.asset
Assets/Resources/Skills/Warrior_DefensiveStance.asset
Assets/Resources/Skills/Warrior_HammerThrow.asset
Assets/Resources/Skills/Warrior_BattleHeal.asset
Assets/Resources/Skills/Warrior_Charge.asset

Assets/Resources/Skills/Mage_Fireball.asset
Assets/Resources/Skills/Mage_IceNova.asset
Assets/Resources/Skills/Mage_Meteor.asset
Assets/Resources/Skills/Mage_Teleport.asset
Assets/Resources/Skills/Mage_LightningStorm.asset

Assets/Resources/Skills/Archer_RainOfArrows.asset
Assets/Resources/Skills/Archer_StunningShot.asset
Assets/Resources/Skills/Archer_EagleEye.asset
Assets/Resources/Skills/Archer_SwiftStride.asset
Assets/Resources/Skills/Archer_EntanglingShot.asset

Assets/Resources/Skills/Rogue_SummonSkeletons.asset
Assets/Resources/Skills/Rogue_SoulDrain.asset
Assets/Resources/Skills/Rogue_CurseOfWeakness.asset
Assets/Resources/Skills/Rogue_CripplingCurse.asset
Assets/Resources/Skills/Rogue_BloodForMana.asset

Assets/Resources/Skills/Paladin_BearForm.asset
Assets/Resources/Skills/Paladin_DivineProtection.asset
Assets/Resources/Skills/Paladin_LayOnHands.asset
Assets/Resources/Skills/Paladin_DivineStrength.asset
Assets/Resources/Skills/Paladin_HolyHammer.asset
```

**Если файлы отсутствуют** - нужно создать их заново (editor скрипты создания есть в `Assets/Scripts/Editor/`)

---

### Исправление 2: Если иконки отсутствуют

**Проблема:** У SkillConfig не назначены иконки

**Решение:**

1. Откройте каждый SkillConfig в Inspector
2. Найдите поле `Icon`
3. Назначьте подходящую иконку из `Assets/UI/Icons/` или другой папки

**Быстрый способ - через список:**

В Unity Editor:
```
Top Menu → Aetherion → Debug → List Available Skill Icons
```

Это покажет все доступные Sprite в проекте.

---

### Исправление 3: Если скиллы не активируются клавишами

**Проверь компонент SimplePlayerController:**

1. Найди персонажа в Hierarchy (например `Warrior_Model (Clone)`)
2. Посмотри есть ли компонент `Simple Player Controller`
3. Проверь что в коде есть обработка клавиш:

Открой `Assets/EasyStart Third Person Controller/Scripts/SimplePlayerController.cs` и найди:

```csharp
if (Input.GetKeyDown(KeyCode.Alpha1)) skillExecutor.UseSkill(0);
if (Input.GetKeyDown(KeyCode.Alpha2)) skillExecutor.UseSkill(1);
if (Input.GetKeyDown(KeyCode.Alpha3)) skillExecutor.UseSkill(2);
if (Input.GetKeyDown(KeyCode.Alpha4)) skillExecutor.UseSkill(3);
if (Input.GetKeyDown(KeyCode.Alpha5)) skillExecutor.UseSkill(4);
```

Если этого кода нет - нужно добавить!

---

## 🐛 ВОЗМОЖНЫЕ ПРИЧИНЫ ПРОБЛЕМ:

### Причина 1: Старые данные в PlayerPrefs

**Симптомы:**
- Показываются неправильные скиллы
- Скиллы от разных классов смешаны

**Решение:**
```
Aetherion → Debug → Clear Equipped Skills PlayerPrefs
```

---

### Причина 2: Неправильный класс в PlayerPrefs

**Симптомы:**
- Загружаются скиллы не того класса
- SelectedCharacterClass пуст или неправильный

**Проверка:**
```
Aetherion → Debug → Show Current Equipped Skills
```

**Решение:**
- Перезапусти CharacterSelection сцену
- Правильно выбери класс
- Убедись что при нажатии "Enter Arena" класс сохраняется

---

### Причина 3: SkillExecutor не получает скиллы

**Симптомы:**
- В Inspector SkillManager скиллы есть
- В Inspector SkillExecutor скиллов нет

**Проверка:**
- Посмотри Console при спавне
- Должно быть: `[SkillManager] ✅ 5 скиллов переданы в SkillExecutor`

**Решение:**
- Проблема в методе `TransferSkillsToExecutor()` в SkillManager
- Проверь что SkillExecutor существует на том же GameObject

---

## 📋 ЧЕКЛИСТ:

Отметь что выполнил:

- [ ] Запустил "Test Skill Loading - All Classes" → результат: **___/25** скиллов
- [ ] Запустил "Check All Skill Icons" → результат: **___/25** с иконками
- [ ] Запустил "Clear Equipped Skills PlayerPrefs"
- [ ] Запустил Arena с Warrior:
  - [ ] Console показывает загрузку 5 скиллов Warrior
  - [ ] Inspector SkillManager имеет 5 скиллов
  - [ ] Inspector SkillExecutor имеет 5 скиллов
  - [ ] Клавиши 1-5 активируют скиллы

---

## 📸 ЕСЛИ НЕ РАБОТАЕТ - ПРИШЛИ СКРИНШОТЫ:

1. **Console после "Test Skill Loading - All Classes"**
2. **Console после "Show Current Equipped Skills"**
3. **Console после запуска Arena с Warrior**
4. **Inspector SkillManager в Arena (во время Play mode)**
5. **Inspector SkillExecutor в Arena (во время Play mode)**
6. **Console после нажатия клавиш 1-5**

---

🚀 **НАЧНИ С ШАГА 1 - Test Skill Loading - All Classes!**

**Сообщи результаты каждого шага!** 💪

---

## 📄 СОЗДАННЫЕ ИНСТРУМЕНТЫ:

- [TestSkillLoader.cs](Assets/Editor/TestSkillLoader.cs) - тестирование загрузки
- [ClearPlayerPrefsSkills.cs](Assets/Editor/ClearPlayerPrefsSkills.cs) - очистка PlayerPrefs
- [CheckSkillIcons.cs](Assets/Editor/CheckSkillIcons.cs) - проверка иконок
- [SKILL_LOADING_DEBUG.md](SKILL_LOADING_DEBUG.md) - подробная инструкция
