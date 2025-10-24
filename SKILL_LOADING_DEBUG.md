# 🔍 ДИАГНОСТИКА ПРОБЛЕМЫ ЗАГРУЗКИ СКИЛЛОВ

## Дата: 2025-10-22

---

## ❌ ПРОБЛЕМЫ:

1. **Скиллы не работают при нажатии клавиш 1-5**
2. **Иконки одинаковые у всех классов** (показывают Hammer Throw и Holy Hammer)
3. **Показываются только 2 скилла вместо 5**

---

## 🔧 СОЗДАННЫЕ ИНСТРУМЕНТЫ ОТЛАДКИ:

### 1. TestSkillLoader.cs
Тестирует загрузку всех 25 скиллов

**Меню Unity:**
- `Aetherion → Debug → Test Skill Loading - All Classes` - тест всех классов
- `Aetherion → Debug → Test Skill Loading - Warrior Only` - тест только Warrior
- `Aetherion → Debug → Test Skill Loading - Paladin Only` - тест только Paladin

### 2. ClearPlayerPrefsSkills.cs
Очищает PlayerPrefs и показывает текущие настройки

**Меню Unity:**
- `Aetherion → Debug → Clear Equipped Skills PlayerPrefs` - очистить сохранённые скиллы
- `Aetherion → Debug → Show Current Equipped Skills` - показать текущие скиллы

---

## 🧪 ПОШАГОВАЯ ДИАГНОСТИКА:

### ШАГ 1: Проверить загрузку всех скиллов

1. **В Unity откройте меню:**
   ```
   Aetherion → Debug → Test Skill Loading - All Classes
   ```

2. **Проверьте Console:**
   - Должны загрузиться **25/25 скиллов**
   - Каждый класс должен иметь **5/5 скиллов**
   - Каждый скилл должен иметь **иконку** (Icon: ✅)

3. **Если скиллы не загружаются:**
   - Проверьте что файлы существуют в `Assets/Resources/Skills/`
   - Проверьте что файлы названы правильно (например: `Warrior_BattleRage.asset`)

**Ожидаемый результат:**
```
═══════════════════════════════════════════════════
ИТОГИ:
✅ Успешно загружено: 25/25
❌ Ошибок: 0
═══════════════════════════════════════════════════
```

---

### ШАГ 2: Проверить PlayerPrefs

1. **В Unity откройте меню:**
   ```
   Aetherion → Debug → Show Current Equipped Skills
   ```

2. **Проверьте что показывается в Console:**
   - Если `EquippedSkills` пуст → ✅ ХОРОШО (будет автоэкипировка)
   - Если `EquippedSkills` содержит старые ID → ❌ ПЛОХО (нужно очистить)

3. **Если есть старые данные, очистите:**
   ```
   Aetherion → Debug → Clear Equipped Skills PlayerPrefs
   ```

---

### ШАГ 3: Тест в Arena Scene

1. **Запустите Arena Scene:**
   ```
   Play → CharacterSelectionScene → Выберите Warrior → Enter Arena
   ```

2. **Проверьте Console при спавне:**

**Ожидаемые логи:**
```
[ArenaManager] 📚 Загрузка скиллов для класса: Warrior
[SkillConfigLoader] 📚 Загрузка скиллов для класса Warrior: 101, 102, 103, 104, 105
[SkillConfigLoader] ✅ Загружен скилл: Battle Rage (ID: 101)
[SkillConfigLoader] ✅ Загружен скилл: Defensive Stance (ID: 102)
[SkillConfigLoader] ✅ Загружен скилл: Hammer Throw (ID: 103)
[SkillConfigLoader] ✅ Загружен скилл: Battle Heal (ID: 104)
[SkillConfigLoader] ✅ Загружен скилл: Charge (ID: 105)
[SkillConfigLoader] ✅ Загружено 5/5 скиллов для Warrior
[ArenaManager] ✅ Загружено 5 скиллов для класса Warrior:
  - Battle Rage (ID: 101)
  - Defensive Stance (ID: 102)
  - Hammer Throw (ID: 103)
  - Battle Heal (ID: 104)
  - Charge (ID: 105)
[ArenaManager] ✅ Автоэкипировано 5 скиллов по умолчанию
[SkillManager] ✅ 5 скиллов переданы в SkillExecutor
[SkillExecutor] ✅ Скилл 'Battle Rage' (ID: 101) установлен в слот 1
[SkillExecutor] ✅ Скилл 'Defensive Stance' (ID: 102) установлен в слот 2
[SkillExecutor] ✅ Скилл 'Hammer Throw' (ID: 103) установлен в слот 3
[SkillExecutor] ✅ Скилл 'Battle Heal' (ID: 104) установлен в слот 4
[SkillExecutor] ✅ Скилл 'Charge' (ID: 105) установлен в слот 5
```

3. **Проверьте Inspector персонажа:**
   - Найдите в Hierarchy: `Warrior_Model (Clone)`
   - Посмотрите компонент `Skill Manager (Script)`
   - Должны быть:
     - `Equipped Skills` → 5 элементов
     - `All Available Skills` → 5 элементов

4. **Нажмите клавиши 1-5:**
   - Должны активироваться скиллы
   - Проверьте Console на логи использования

---

## 🔍 ВОЗМОЖНЫЕ ПРИЧИНЫ ПРОБЛЕМ:

### Проблема 1: Скиллы не загружаются (0/5)

**Причина:** Файлы скиллов не найдены

**Решение:**
1. Проверьте что файлы существуют:
   ```
   Assets/Resources/Skills/Warrior_BattleRage.asset
   Assets/Resources/Skills/Warrior_DefensiveStance.asset
   Assets/Resources/Skills/Warrior_HammerThrow.asset
   Assets/Resources/Skills/Warrior_BattleHeal.asset
   Assets/Resources/Skills/Warrior_Charge.asset
   ```

2. Проверьте что файлы это **SkillConfig** (не SkillData):
   - Откройте файл в Inspector
   - Должно быть: `Script: SkillConfig`

---

### Проблема 2: Загружаются только 2 скилла

**Причина:** Старые данные в PlayerPrefs

**Решение:**
1. Запустите:
   ```
   Aetherion → Debug → Clear Equipped Skills PlayerPrefs
   ```

2. Перезапустите Arena Scene

---

### Проблема 3: Иконки одинаковые

**Причина:** У SkillConfig не установлены иконки

**Решение:**
1. Откройте каждый SkillConfig в Inspector
2. Проверьте поле `Icon` - должна быть назначена иконка
3. Если иконки нет - найдите подходящую в `Assets/UI/Icons/Skills/` и назначьте

---

### Проблема 4: Скиллы не активируются клавишами 1-5

**Причина 1:** SkillExecutor не получил скиллы

**Проверка:**
- Посмотрите в Inspector на `SkillExecutor` компонент
- Должно быть 5 скиллов в `Equipped Skills`

**Причина 2:** SimplePlayerController не привязан к SkillExecutor

**Проверка:**
- Посмотрите скрипт `SimplePlayerController`
- Должен быть метод `Update()` с обработкой клавиш:
  ```csharp
  if (Input.GetKeyDown(KeyCode.Alpha1)) skillExecutor.UseSkill(0);
  if (Input.GetKeyDown(KeyCode.Alpha2)) skillExecutor.UseSkill(1);
  ...
  ```

**Причина 3:** Недостаточно маны

**Проверка:**
- Посмотрите в Console при нажатии клавиши
- Если пишет "недостаточно маны" - значит система работает, просто нет ресурсов

---

## 📋 ЧЕКЛИСТ ПРОВЕРКИ:

Проверь каждый пункт:

- [ ] Запустить `Test Skill Loading - All Classes`
  - [ ] Загружено 25/25 скиллов
  - [ ] У каждого скилла есть иконка
- [ ] Запустить `Show Current Equipped Skills`
  - [ ] PlayerPrefs либо пуст, либо содержит правильные данные
- [ ] Если PlayerPrefs содержит мусор:
  - [ ] Запустить `Clear Equipped Skills PlayerPrefs`
- [ ] Запустить Arena с Warrior
  - [ ] Проверить Console - должны загрузиться 5 скиллов
  - [ ] Проверить Inspector - SkillManager имеет 5 скиллов
  - [ ] Проверить Inspector - SkillExecutor имеет 5 скиллов
- [ ] Нажать клавиши 1-5
  - [ ] Скиллы активируются
  - [ ] В Console появляются логи использования
- [ ] Повторить для всех 5 классов

---

## 🐛 ЕСЛИ ПРОБЛЕМА НЕ РЕШЕНА:

**Сделай скриншоты и пришли:**

1. **Console после запуска Test Skill Loading:**
   ```
   Aetherion → Debug → Test Skill Loading - All Classes
   ```
   (Скриншот Console)

2. **Inspector SkillManager в Arena:**
   - Запусти Arena с Warrior
   - Кликни на `Warrior_Model (Clone)` в Hierarchy
   - Скриншот компонента `Skill Manager`

3. **Inspector SkillExecutor в Arena:**
   - Тот же объект
   - Скриншот компонента `Skill Executor`

4. **Console после нажатия клавиш 1-5:**
   - Нажми каждую клавишу
   - Скриншот Console с логами

---

## 📄 СВЯЗАННЫЕ ФАЙЛЫ:

- [SkillConfigLoader.cs](Assets/Scripts/Skills/SkillConfigLoader.cs) - загрузчик скиллов
- [ArenaManager.cs](Assets/Scripts/Arena/ArenaManager.cs) - спавн и загрузка
- [SkillManager.cs](Assets/Scripts/Skills/SkillManager.cs) - хранение скиллов
- [SkillExecutor.cs](Assets/Scripts/Skills/SkillExecutor.cs) - выполнение скиллов

---

🔍 **НАЧНИ С ШАГА 1 - ЗАПУСТИ TEST SKILL LOADING!** 🚀

**Сообщи результаты каждого шага!** 💪
