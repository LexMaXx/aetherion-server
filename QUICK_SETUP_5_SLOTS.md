# ⚡ Быстрая настройка 5 экипированных слотов

## ✅ Изменения в коде завершены!

Все скрипты обновлены и теперь поддерживают **5 экипированных слотов**.

## 🎨 Что нужно сделать в Unity Editor:

### 1. CharacterSelection Scene

1. Откройте сцену: `Scenes/CharacterSelectionScene`

2. Найдите объект с компонентом `SkillSelectionManager`
   - Обычно это `Canvas > SkillSelectionPanel` или похожее

3. В Inspector найдите раздел **Equipped Slots**:
   ```
   Equipped Slots
   Size: 3  ← Измените на 5
   ```

4. После изменения Size на 5, появятся 2 новых пустых слота:
   ```
   Element 0: [EquippedSlot_0]
   Element 1: [EquippedSlot_1]
   Element 2: [EquippedSlot_2]
   Element 3: [None (Skill Slot UI)]  ← Назначьте EquippedSlot_3
   Element 4: [None (Skill Slot UI)]  ← Назначьте EquippedSlot_4
   ```

5. **Если UI объектов EquippedSlot_3 и EquippedSlot_4 не существует:**
   - Найдите в Hierarchy: `EquippedSlots > EquippedSlot_0`
   - Дублируйте его (Ctrl+D) два раза
   - Переименуйте копии:
     - `EquippedSlot_0 (1)` → `EquippedSlot_3`
     - `EquippedSlot_0 (2)` → `EquippedSlot_4`
   - Расположите их визуально (RectTransform position)

6. Перетащите созданные объекты в Inspector:
   - `EquippedSlot_3` → Element 3
   - `EquippedSlot_4` → Element 4

7. **Обновите каждый слот**:
   - Выберите `EquippedSlot_3` в Hierarchy
   - В Inspector найдите компонент `SkillSlotUI`
   - Установите `Slot Index = 3`
   - Установите `Is Equip Slot = ✓ (true)`
   - Установите `Is Library Slot = ☐ (false)`

   - Повторите для `EquippedSlot_4` с `Slot Index = 4`

8. Сохраните сцену (Ctrl+S)

### 2. Arena Scene

1. Откройте сцену: `Scenes/ArenaScene`

2. Найдите объект `SkillBar` (или `SkillBarUI`)
   - Обычно это `Canvas > SkillBar` или `UI > SkillBar`

3. Проверьте сколько дочерних объектов `SkillSlotBar` есть:
   ```
   SkillBar
   ├── SkillSlot_0
   ├── SkillSlot_1
   ├── SkillSlot_2
   ├── SkillSlot_3  ← Должен быть
   └── SkillSlot_4  ← Должен быть
   ```

4. **Если слотов 3 и 4 нет:**
   - Дублируйте `SkillSlot_0` два раза
   - Переименуйте:
     - `SkillSlot_0 (1)` → `SkillSlot_3`
     - `SkillSlot_0 (2)` → `SkillSlot_4`
   - Расположите их визуально рядом с остальными слотами

5. Компонент `SkillBarUI` автоматически найдёт все слоты через:
   ```csharp
   skillSlots = GetComponentsInChildren<SkillSlotBar>();
   ```

6. Сохраните сцену (Ctrl+S)

## 🧪 Тестирование

### 1. Запустите CharacterSelection Scene:

Ожидаемые логи:
```
[SkillConfigLoader] ✅ Загружено 5/5 скиллов для Warrior
[SkillSelectionManager] Автоэкипировка всех 5 скиллов класса. Слотов: 5
[SkillSelectionManager] Экипирую 'Battle Rage' (ID: 101) в экипированный слот 1
[SkillSelectionManager] Экипирую 'Defensive Stance' (ID: 102) в экипированный слот 2
[SkillSelectionManager] Экипирую 'Hammer Throw' (ID: 103) в экипированный слот 3
[SkillSelectionManager] Экипирую 'Battle Heal' (ID: 104) в экипированный слот 4
[SkillSelectionManager] Экипирую 'Charge' (ID: 105) в экипированный слот 5
[SkillSelectionManager] Экипированные скиллы: [101, 102, 103, 104, 105]
```

**Визуально:** Все 5 слотов должны показывать иконки скиллов.

### 2. Перейдите в Arena Scene:

Ожидаемые логи:
```
[SkillBarUI] ✅ Найдено 5 слотов скиллов
[SkillBarUI] Загружено 5 экипированных скиллов: [101, 102, 103, 104, 105]
[SkillBarUI] Слот 1: Battle Rage
[SkillBarUI] Слот 2: Defensive Stance
[SkillBarUI] Слот 3: Hammer Throw
[SkillBarUI] Слот 4: Battle Heal
[SkillBarUI] Слот 5: Charge
```

**Визуально:** Skill Bar внизу должен показывать 5 иконок.

**Хоткеи:** Нажмите клавиши 1, 2, 3, 4, 5 - должны активироваться соответствующие скиллы.

## ❌ Типичные ошибки

### Ошибка 1:
```
[SkillSelectionManager] ❌ equippedSlots[3] = null!
[SkillSelectionManager] ❌ equippedSlots[4] = null!
```
**Решение:** Вы не назначили EquippedSlot_3 и EquippedSlot_4 в Inspector.

### Ошибка 2:
```
[SkillBarUI] Должно быть ровно 5 слотов! Найдено: 3
```
**Решение:** В Arena Scene не хватает 2 слотов. Добавьте SkillSlot_3 и SkillSlot_4.

### Ошибка 3:
```
[SkillSelectionManager] Экипированные скиллы: [101, 102, 103]
```
Экипируются только 3 вместо 5.
**Решение:** Проверьте что Size в Inspector установлен на 5, а не 3.

## ✅ Чеклист готовности

- [ ] CharacterSelection: Size = 5 в Inspector
- [ ] CharacterSelection: Все 5 EquippedSlot назначены
- [ ] CharacterSelection: Каждый слот имеет правильный Slot Index (0-4)
- [ ] Arena: 5 SkillSlotBar объектов существуют
- [ ] Arena: Они расположены визуально рядом друг с другом
- [ ] Обе сцены сохранены (Ctrl+S)
- [ ] Тест: Логи показывают "Автоэкипировка всех 5 скиллов"
- [ ] Тест: Визуально видны все 5 иконок скиллов

Если все пункты выполнены - **готово!** 🎉
