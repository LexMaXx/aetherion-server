# 🔧 БЫСТРОЕ ИСПРАВЛЕНИЕ - Unity не видит UnifiedSocketIO

## Проблема
Unity не видит новый скрипт `UnifiedSocketIO.cs` в списке компонентов.

## Решение (3 способа)

### Способ 1: Обновить проект в Unity (рекомендуется)
1. В Unity нажми `Ctrl+R` (Refresh/Reimport)
2. Или в меню: `Assets → Reimport All`
3. Подожди пока Unity перекомпилирует скрипты (30-60 сек)
4. Проверь консоль - не должно быть ошибок компиляции

### Способ 2: Перезапустить Unity
1. Закрой Unity полностью (`File → Exit`)
2. Открой проект заново
3. Unity автоматически обнаружит новые файлы

### Способ 3: Удалить Library/ScriptAssemblies (если не помогло)
1. Закрой Unity
2. Открой папку проекта: `C:\Users\Asus\Aetherion`
3. Удали папку `Library\ScriptAssemblies`
4. Открой Unity - он пересоберёт все скрипты

---

## После обновления - проверь:

1. **Console в Unity** - НЕ должно быть ошибок:
   ```
   ✅ Всё чисто - идеально!
   ❌ Есть ошибки - нужно исправить
   ```

2. **Добавь UnifiedSocketIO**:
   - Создай пустой GameObject в GameScene
   - `Add Component → UnifiedSocketIO`
   - Если компонент появился - всё работает! ✅

---

## Если всё равно не видит - проверь эти файлы:

### 1. UnifiedSocketIO.cs существует?
Путь: `Assets/Scripts/Network/UnifiedSocketIO.cs`
Размер: ~20 KB
Должен содержать: `public class UnifiedSocketIO : MonoBehaviour`

### 2. UnifiedSocketIO.cs.meta существует?
Путь: `Assets/Scripts/Network/UnifiedSocketIO.cs.meta`
Должен содержать GUID: `e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2`

### 3. NetworkDataClasses.cs существует?
Путь: `Assets/Scripts/Network/NetworkDataClasses.cs`
Должен содержать классы: `SerializableVector3`, `JoinRoomRequest`, и т.д.

---

## Проверка ошибок компиляции

Если Unity показывает ошибки в Console:

### Ошибка: "SerializableVector3 does not exist"
**Причина**: NetworkDataClasses.cs не найден
**Решение**:
```bash
# В Git Bash или PowerShell:
cd /c/Users/Asus/Aetherion
git add Assets/Scripts/Network/NetworkDataClasses.cs
git add Assets/Scripts/Network/NetworkDataClasses.cs.meta
```
Затем обнови Unity (`Ctrl+R`)

### Ошибка: "Duplicate class UnifiedSocketIO"
**Причина**: Старые версии файла
**Решение**:
1. `Edit → Project Settings → Editor`
2. Version Control Mode: `Visible Meta Files`
3. Asset Serialization Mode: `Force Text`
4. Перезапусти Unity

---

## Если всё работает - следующий шаг:

Открой файл `UNIFIED_MULTIPLAYER_COMPLETE.md` и следуй инструкциям по настройке!

---

**Удачи! Если что-то не получается - скажи и я помогу** 🚀
