# ⚡ Быстрый старт - Система Скиллов

## 🚀 3 шага до запуска

### Шаг 1: Создать базу данных (30 секунд)
```
Unity → Tools → Skills → Create Skill Database
```
✅ Создано:
- `Assets/Resources/SkillDatabase.asset` - база скиллов
- `Assets/Resources/Skills/` - 30 примеров скиллов (6 на класс)

---

### Шаг 2: Настроить UI (5 минут)

#### В Character Selection Scene:

1. **Создать UI панель:**
   ```
   Canvas
   └── SkillPanel
       ├── Title: "Выберите 3 скилла"
       ├── LibrarySection (библиотека - 6 слотов)
       │   ├── SkillSlot_1 [Add: SkillSlotUI, isEquipSlot=false]
       │   ├── SkillSlot_2
       │   ├── SkillSlot_3
       │   ├── SkillSlot_4
       │   ├── SkillSlot_5
       │   └── SkillSlot_6
       │
       └── EquippedSection (экипировка - 3 слота)
           ├── EquippedSlot_1 [Add: SkillSlotUI, isEquipSlot=true]
           ├── EquippedSlot_2
           └── EquippedSlot_3
   ```

2. **Добавить менеджер:**
   ```
   GameObject → Create Empty → "SkillSelectionManager"
   Add Component → SkillSelectionManager
   ```

3. **Назначить ссылки:**
   ```
   SkillSelectionManager:
   - Library Slots → перетащить 6 слотов библиотеки
   - Equipped Slots → перетащить 3 слота экипировки
   - Skill Database → SkillDatabase.asset
   ```

---

### Шаг 3: Интегрировать в Arena (3 минуты)

#### В Arena Scene на персонаже:

1. **Добавить компонент:**
   ```
   PlayerModel → Add Component → SkillManager
   ```

2. **В скрипте загрузки персонажа:**
   ```csharp
   // Загружаем скиллы из MongoDB
   ServerAPI.Instance.LoadCharacterSkills(characterClass, (skillIds, success) =>
   {
       if (success)
       {
           SkillManager skillMgr = player.GetComponent<SkillManager>();
           skillMgr.LoadEquippedSkills(skillIds);
       }
   });
   ```

3. **Управление скиллами (в PlayerInput):**
   ```csharp
   void Update()
   {
       // Скилл 1
       if (Input.GetKeyDown(KeyCode.Alpha1))
       {
           skillManager.UseSkill(0, targetSystem.CurrentTarget);
       }

       // Скилл 2
       if (Input.GetKeyDown(KeyCode.Alpha2))
       {
           skillManager.UseSkill(1, targetSystem.CurrentTarget);
       }

       // Скилл 3
       if (Input.GetKeyDown(KeyCode.Alpha3))
       {
           skillManager.UseSkill(2, targetSystem.CurrentTarget);
       }
   }
   ```

---

## ✅ Готово!

Система работает! Теперь:
- ✅ Drag & Drop скиллов работает
- ✅ Сохранение в MongoDB
- ✅ Загрузка в Arena
- ✅ Использование кнопками 1, 2, 3

---

## 🎨 Кастомизация

### Изменить параметры скилла:

1. Найти скилл: `Assets/Resources/Skills/Warrior_PowerStrike.asset`
2. Открыть в Inspector
3. Изменить:
   ```
   Cooldown: 10 сек
   Mana Cost: 30
   Base Damage: 60
   Strength Scaling: 25
   ```
4. Сохранить (Ctrl+S)

### Создать новый скилл:

```
Right Click → Create → Aetherion → Skills → Skill Data

Настроить:
- Skill ID: 701 (уникальный)
- Skill Name: "Мой скилл"
- Character Class: Warrior
- Skill Type: Damage
- Cooldown: 8
- Mana Cost: 35
- Cast Range: 5
- Base Damage: 50
```

Добавить в `SkillDatabase` → соответствующий массив класса.

---

## 📝 Управление

**Character Selection:**
- Перетащите скилл из библиотеки в слот экипировки
- Или наоборот - обмен

**Arena:**
- `1` - первый скилл
- `2` - второй скилл
- `3` - третий скилл

---

## 🐛 Проблемы?

**Скиллы не загружаются:**
- Проверьте `SkillDatabase.asset` в Resources
- Убедитесь что скиллы добавлены в массивы

**Drag & Drop не работает:**
- Проверьте `Canvas Raycaster` на Canvas
- Убедитесь что `SkillSlotUI` имеет `Image` компонент

**Скиллы не сохраняются:**
- Проверьте `ServerAPI.useLocalStorage = true` для теста
- Для MongoDB настроить сервер

---

Полная документация: [SKILL_SYSTEM_GUIDE.md](SKILL_SYSTEM_GUIDE.md)
