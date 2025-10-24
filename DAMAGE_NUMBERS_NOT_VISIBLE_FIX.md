# 🔍 ИСПРАВЛЕНИЕ: Цифры урона не видны

## ✅ ЧТО ИСПРАВЛЕНО:

### 1. Добавлен правильный масштаб WorldSpace Canvas
**Проблема:** Canvas создавался, но масштаб был неправильным для 3D мира
**Решение:**
```csharp
RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
canvasRect.sizeDelta = new Vector2(100, 100);
canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f); // ← ВАЖНО!
```

### 2. Добавлено детальное логирование
Теперь в Console будут показаны:
```
[DamageNumberManager] Canvas не найден, инициализирую...
[DamageNumberManager] Инициализирован с WorldSpace Canvas
[DamageNumberManager] Prefab не назначен, создаю дефолтный...
[DamageNumberManager] Создан дефолтный prefab
[DamageNumberManager] Создана цифра на позиции: (x, y, z)
[DamageNumberManager] ✅ Урон 45 показан (Crit: False)
```

### 3. Добавлена проверка Camera.main
```csharp
if (mainCamera == null)
{
    Debug.LogError("[DamageNumberManager] Camera.main не найдена!");
    return;
}
```

---

## 🎮 ТЕСТИРОВАНИЕ:

### Шаг 1: Проверьте логи
```
1. Unity → Play ▶️
2. Атакуйте врага (ЛКМ)
3. Смотрите Console
```

### Что должно быть в логах:
```
✅ [DamageNumberManager] Инициализирован с WorldSpace Canvas
✅ [DamageNumberManager] Создан дефолтный prefab
✅ [DamageNumberManager] Создана цифра на позиции: (x, y, z)
✅ [DamageNumberManager] ✅ Урон 45 показан (Crit: False)
```

### Шаг 2: Проверьте Hierarchy во время Play
```
Hierarchy → DamageNumberManager
  └─ DamageNumberCanvas
      └─ DamageNumberPrefab(Clone) ← Цифры урона должны быть здесь!
```

### Шаг 3: Если цифры всё ещё не видны
```
1. Выберите DamageNumberCanvas в Hierarchy
2. Inspector → Canvas:
   - Render Mode: World Space ✅
   - Scale: (0.01, 0.01, 0.01) ✅

3. Выберите DamageNumberPrefab(Clone)
4. Inspector → проверьте:
   - Position: должна быть над врагом (примерно y=2-3)
   - TextMeshProUGUI: текст должен быть "45" или другое число
   - Color: White или Yellow (если крит)
```

---

## 🔧 ДОПОЛНИТЕЛЬНЫЕ ПРОВЕРКИ:

### Проверка 1: Есть ли Camera.main?
```
Hierarchy → Main Camera
Inspector → Tag: должен быть "MainCamera"

Если тег неправильный:
Inspector → Tag → выберите "MainCamera"
```

### Проверка 2: Камера видит цифры?
```
Scene View → выберите цифру урона (в Hierarchy)
Нажмите F (Focus) → камера должна переместиться к цифре

Если цифра очень далеко или очень близко:
→ Проблема с масштабом (уже исправлено в коде)
```

### Проверка 3: TextMeshPro шрифт загружен?
```
Логи должны показывать:
✅ [DamageNumberManager] Создан дефолтный prefab

Если нет:
→ Проверьте что TextMesh Pro импортирован:
   Window → TextMeshPro → Import TMP Essentials
```

---

## 🎨 АЛЬТЕРНАТИВНОЕ РЕШЕНИЕ: Создать prefab вручную

Если автоматическое создание не работает, создайте prefab вручную:

### Шаг 1: Создать UI Text
```
Hierarchy → Right Click → UI → Text - TextMeshPro
(Если появится Import TMP → нажмите Import)
```

### Шаг 2: Настроить TextMeshPro
```
Inspector:
- Text: "999"
- Font Size: 36
- Color: White
- Alignment: Center
- Font Style: Bold
```

### Шаг 3: Добавить компонент
```
Add Component → DamageNumber

Settings:
- Lifetime: 1.5
- Move Speed: 2
- Fade Speed: 1
```

### Шаг 4: Создать Prefab
```
1. Перетащить из Hierarchy в Project (Assets/Prefabs/UI/)
2. Переименовать в "DamageNumberPrefab"
3. Удалить из Hierarchy
```

### Шаг 5: Назначить в Manager
```
1. Hierarchy → Create Empty → "DamageNumberManager"
2. Add Component → DamageNumberManager
3. Inspector → Damage Number Prefab → перетащить prefab
```

---

## 📊 РАЗМЕРЫ И МАСШТАБЫ:

### WorldSpace Canvas:
```
RectTransform:
  Size: (100, 100)
  Scale: (0.01, 0.01, 0.01)  ← КЛЮЧЕВОЙ ПАРАМЕТР!

Почему 0.01?
- 1 unit в Canvas = 100 pixels
- 0.01 scale = 1 pixel = 0.01 world units
- Текст размером 36px = 0.36 world units
- Примерно как персонаж высотой 2 метра
```

### Damage Number Position:
```
Spawn Position = Enemy Position + Vector3.up * 2f
                = (enemy.x, enemy.y + 2, enemy.z)

Почему +2?
- Цифра появляется на 2 метра выше врага
- Хорошо видна над головой
- Не перекрывает HP bar
```

---

## ✅ ФИНАЛЬНАЯ ПРОВЕРКА:

После обновления кода:
```
1. ✅ Сохраните все файлы
2. ✅ Unity → дождитесь компиляции
3. ✅ Play ▶️
4. ✅ Атакуйте врага
5. ✅ Смотрите логи в Console
6. ✅ Смотрите Hierarchy → DamageNumberCanvas
```

Если видите логи но не видите цифры:
```
→ Проверьте масштаб Canvas (должен быть 0.01)
→ Проверьте позицию цифры в Scene View
→ Попробуйте увеличить Canvas.localScale до (0.1, 0.1, 0.1)
```

---

## 🎯 ОЖИДАЕМЫЙ РЕЗУЛЬТАТ:

После исправления:
```
✅ Цифры урона видны над врагом
✅ Размер примерно как текст в игре
✅ Белые для обычного урона
✅ ЖЁЛТЫЕ и БОЛЬШИЕ для крита
✅ Движутся вверх
✅ Исчезают через 1.5 сек
✅ Всегда повёрнуты к камере
```

---

**Попробуйте ещё раз и проверьте логи!** 🎮

Если всё ещё не работает - пришлите скриншот Console с логами!
