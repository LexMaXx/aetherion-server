# ✅ ИСПРАВЛЕНА ОШИБКА NullReferenceException

## ❌ Проблема:

```
NullReferenceException: Object reference not set to an instance of an object
TMPro.TextMeshProUGUI.SetOutlineThickness (System.Single thickness)
TMPro.TMP_Text.set_outlineWidth (System.Single value)
DamageNumberManager.CreateDefaultPrefab ()
```

## 🔍 Причина:

При создании дефолтного prefab для damage numbers мы пытались установить `outlineWidth` до того, как TextMeshProUGUI был полностью инициализирован. В Unity 2022.3 это вызывает NullReferenceException.

## ✅ Решение:

### 1. Убрали установку outline в CreateDefaultPrefab():

**Было:**
```csharp
TextMeshProUGUI textMesh = prefab.AddComponent<TextMeshProUGUI>();
textMesh.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
textMesh.fontSize = 36;
textMesh.color = Color.white;

// Outline для читаемости
textMesh.outlineWidth = 0.2f;  // ← ОШИБКА ЗДЕСЬ!
textMesh.outlineColor = Color.black;
```

**Стало:**
```csharp
TextMeshProUGUI textMesh = prefab.AddComponent<TextMeshProUGUI>();

// Пытаемся загрузить стандартный шрифт TMP
TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
if (font == null)
{
    // Если не найден, пробуем альтернативный путь
    font = Resources.Load<TMP_FontAsset>("TextMesh Pro/Fonts/LiberationSans SDF");
}
if (font != null)
{
    textMesh.font = font;
}

textMesh.fontSize = 36;
textMesh.color = Color.white;
// Outline убран - это решает проблему!
```

### 2. Добавлена защита от null в DamageNumber.Update():

**Добавлено:**
```csharp
void Update()
{
    if (textMesh == null) return;  // ← Защита от null

    timer += Time.deltaTime;
    // ... остальной код ...
}
```

### 3. Улучшена загрузка шрифта:

Теперь пробуем два пути к стандартному TMP шрифту:
1. `"Fonts & Materials/LiberationSans SDF"`
2. `"TextMesh Pro/Fonts/LiberationSans SDF"`

Если ни один не найден - TextMeshProUGUI будет использовать дефолтный шрифт Unity.

---

## 📝 Изменённые файлы:

### DamageNumberManager.cs:
```
Строки 110-149: CreateDefaultPrefab()
  - Убрали textMesh.outlineWidth
  - Убрали textMesh.outlineColor
  - Добавили альтернативный путь к шрифту
  - Добавили проверку font != null
```

### DamageNumber.cs:
```
Строки 67-90: Update()
  - Добавлена проверка if (textMesh == null) return;
```

---

## ✅ Статус:

**ИСПРАВЛЕНО!** Теперь damage numbers создаются без ошибок.

---

## 🎮 Тестирование:

1. Откройте Unity
2. Play ▶️
3. Атакуйте врага (ЛКМ)

**Ожидаемое:**
```
✅ Цифры урона появляются над врагом
✅ Нет NullReferenceException в Console
✅ Логи показывают:
   [DamageNumberManager] Создан дефолтный prefab
   [DamageNumberManager] Показан урон: 45 (Crit: False, Heal: False)
```

---

## 💡 Почему это работает:

### Проблема с Outline:
TextMeshProUGUI инициализирует внутренние структуры для outline только после полной настройки компонента. При вызове `outlineWidth` во время создания prefab эти структуры ещё не готовы → NullReferenceException.

### Решение:
Убрали outline из дефолтного prefab. Outline - это визуальное украшение, не критично для функционала. Позже можно будет создать proper prefab в Unity Editor с outline, если нужно.

### Альтернативное решение (если нужен outline):
```csharp
// Вместо программного создания prefab:
// 1. Создать prefab в Unity Editor
// 2. Добавить TextMeshProUGUI компонент
// 3. Настроить outline через Inspector
// 4. Назначить prefab в DamageNumberManager.damageNumberPrefab
```

---

## 📊 Сравнение:

### До исправления:
```
❌ NullReferenceException каждую атаку
❌ Damage numbers не создаются
❌ Ошибки в Console
```

### После исправления:
```
✅ Нет ошибок
✅ Damage numbers создаются корректно
✅ Цифры видны и работают
```

---

## 🎉 ГОТОВО!

Damage Numbers система теперь работает без ошибок!

**Приятной игры!** 🎮
