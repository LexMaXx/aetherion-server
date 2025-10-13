# ✅ DEBUG Scripts Fixed - No More TestPlayer!

## 🔴 Проблема которую вы обнаружили

Вы совершенно правы! В проекте были **DEBUG скрипты** которые:
- Автоматически создавали **TestPlayer** при старте игры
- Пропускали авторизацию через LoginScene
- Позволяли загружать Arena/GameScene напрямую с фейковыми данными
- Создавали **ФЕЙКОВЫХ игроков** с тестовыми токенами на сервере

Это объясняет логи сервера:
```
[Комната] TestPlayer присоединился к комнате f5040710-ed23-4883-b576-7ba1dd6e095a
[Комната] TestPlayer3606 присоединился к комнате f5040710-ed23-4883-b576-7ba1dd6e095a
```

Каждый раз игра создавала НОВОГО TestPlayer с НОВЫМ userId!

---

## 🛠️ Найденные и отключённые скрипты

### 1. ❌ QuickMultiplayerTest.cs (САМЫЙ ОПАСНЫЙ)
**Местоположение:** `Assets/Scripts/Debug/QuickMultiplayerTest.cs`

**Что делал:**
- Использовал `[RuntimeInitializeOnLoadMethod]` - запускался **ДО загрузки первой сцены**
- Автоматически устанавливал фейковый токен: `"test_token_for_multiplayer"`
- Создавал случайного TestPlayer: `"TestPlayer" + Random.Range(1000, 9999)`
- Устанавливал класс персонажа: `"Warrior"`

**Исправление:**
```csharp
// БЫЛО:
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void QuickSetup() { ... }

// СТАЛО:
// [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]  ← ЗАКОММЕНТИРОВАН
static void QuickSetup() { ... }
```

---

### 2. ❌ QuickStart.cs
**Местоположение:** `Assets/Scripts/Debug/QuickStart.cs`

**Что делал:**
- Автоматически пропускал `IntroScene` и `LoginScene`
- Устанавливал тестовый токен: `"test_token_12345"`
- Создавал TestPlayer: `"TestPlayer"`
- Позволял нажать F5 для перехода к CharacterSelectionScene
- F6 - загрузить Arena с Warrior
- F7 - загрузить Arena с Mage

**Исправление:**
```csharp
// БЫЛО:
[SerializeField] private bool enableQuickStart = true;

// СТАЛО:
[SerializeField] private bool enableQuickStart = false; // ОТКЛЮЧЕН для production
```

---

### 3. ❌ DebugSceneLoader.cs
**Местоположение:** `Assets/Scripts/Debug/DebugSceneLoader.cs`

**Что делал:**
- F1 - загрузить Arena с тестовым персонажем
- F2 - загрузить GameScene с тестовым персонажем
- F3 - загрузить CharacterSelection с тестовым токеном

**Исправление:**
```csharp
// БЫЛО:
[SerializeField] private bool enableDebugMode = true;

// СТАЛО:
[SerializeField] private bool enableDebugMode = false; // ОТКЛЮЧЕН для production
```

---

## ✅ Результат

Теперь:
- ✅ **НЕТ автологина** - требуется регистрация через LoginScene
- ✅ **НЕТ TestPlayer** - только реальные зарегистрированные пользователи
- ✅ **НЕТ фейковых токенов** - только настоящие JWT от сервера
- ✅ **Multiplayer будет работать** с правильными токенами

---

## 🧪 Как протестировать

### 1. Очистите PlayerPrefs

**В Unity Editor:**
```csharp
// Нажмите F8 в игре (если QuickStart включён)
// ИЛИ в Unity menu:
Edit → Clear All PlayerPrefs
```

**Или вручную удалите файл:**
```
Windows: %USERPROFILE%\AppData\LocalLow\<CompanyName>\<GameName>\
```

### 2. Перезапустите Unity игру

Теперь должно открыться **IntroScene** или **LoginScene** вместо автоматического входа.

### 3. Зарегистрируйтесь

- Введите email и пароль
- Нажмите Register
- Токен сохранится в PlayerPrefs

### 4. Создайте персонажа

- Выберите класс
- Введите имя
- Создайте до 5 персонажей (лимит)

### 5. Войдите в multiplayer

- Create Room или Join Room
- Теперь сервер увидит **РЕАЛЬНОГО пользователя** с настоящим токеном!

---

## 📊 Логи сервера (теперь правильные)

**БЫЛО (с TestPlayer):**
```
[Комната] TestPlayer присоединился к комнате ...
[Комната] TestPlayer3606 присоединился к комнате ...
[Комната] Пользователь 68eb53cfd75705b9ce7a694a уже находится в комнате
```
← Каждый раз НОВЫЙ userId!

**СТАЛО (с реальными пользователями):**
```
[Комната] YourRealUsername присоединился к комнате ...
[Комната] Player2RealName присоединился к комнате ...
[Комната] Создано: abc-123-xyz пользователем YourRealUsername
```
← Стабильные userId от MongoDB!

---

## 🔧 Для разработчиков

Если нужно **включить debug mode** для разработки:

### QuickMultiplayerTest.cs
```csharp
// Раскомментируйте:
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void QuickSetup() { ... }
```

### QuickStart.cs
```csharp
// В Inspector или коде:
enableQuickStart = true;
```

### DebugSceneLoader.cs
```csharp
// В Inspector или коде:
enableDebugMode = true;
```

---

## 📝 О лимите 5 персонажей

Вы упомянули что не могли создать 5+ персонажей. Это **НЕ баг**, это **фича**!

**Файл:** `SERVER_CODE/routes/character.js:44-48`
```javascript
const existingCharacters = await Character.countDocuments({ userId: req.user.id });
if (existingCharacters >= 5) {
    return res.status(400).json({
        success: false,
        message: 'Достигнут лимит персонажей (максимум 5)'
    });
}
```

**Это нормально для MMO:**
- World of Warcraft - 50 персонажей на аккаунт
- Final Fantasy XIV - 8 персонажей на сервер
- Guild Wars 2 - 5 персонажей по умолчанию

**Если хотите больше 5:**
1. Измените `>= 5` на `>= 10` (или любое число)
2. Или удалите проверку полностью

**Чтобы удалить персонажа:**
```
DELETE /api/character/:id
```

Unity не имеет UI для удаления, но можно добавить кнопку "Delete Character" в CharacterSelectionScene.

---

## 🎯 Итоги

| Проблема | Статус | Решение |
|----------|--------|---------|
| Автологин TestPlayer | ✅ Исправлено | Отключен QuickMultiplayerTest |
| Пропуск LoginScene | ✅ Исправлено | Отключен QuickStart |
| F1/F2/F3 шорткаты | ✅ Исправлено | Отключен DebugSceneLoader |
| Фейковые токены | ✅ Исправлено | Только реальные JWT |
| Лимит 5 персонажей | ℹ️ Фича | Можно изменить в character.js |
| Multiplayer работает | ⏳ Тестируется | Должно работать с реальными пользователями |

---

**Теперь запустите игру заново и зарегистрируйтесь!**
Multiplayer должен работать с **реальными пользователями** вместо TestPlayer! 🎮

---

**Дата:** 2025-10-12
**Автор:** Claude Code Assistant
**Commit:** 71ffa12
