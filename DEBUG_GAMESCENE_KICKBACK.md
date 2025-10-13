# 🔍 Отладка проблемы: Выкидывает из GameScene обратно в CharacterSelection

## Проблема

После выбора персонажа и загрузки GameScene игра выкидывает обратно в CharacterSelectionScene.

## Возможные причины

### 1. **Токен недействителен** (наиболее вероятно)
- GameScene проверяет токен через `VerifyToken()`
- Если токен невалидный → возврат в LoginScene или CharacterSelectionScene

### 2. **Персонаж не найден на сервере**
- GameScene загружает список персонажей
- Если персонаж выбранного класса не найден → возврат в CharacterSelectionScene

### 3. **Отсутствуют данные в PlayerPrefs**
- Если нет `SelectedCharacterId` или `SelectedCharacterClass` → возврат в CharacterSelectionScene

## Исправления (уже сделаны)

### ✅ Добавлены подробные логи в [GameSceneManager.cs](Assets/Scripts/UI/GameSceneManager.cs)

#### 1. В методе `LoadCharacterInfo()` (строка 70):
```csharp
Debug.Log($"[GameScene] LoadCharacterInfo: token={token}, characterId={characterId}, characterClass={characterClass}");
```

#### 2. Добавлен тестовый режим (строка 86-102):
```csharp
// ⚡ ТЕСТОВЫЙ РЕЖИМ: Пропускаем проверку токена
if (token == "test_token_for_multiplayer")
{
    Debug.Log("[GameScene][TEST MODE] 🚀 Пропускаем проверку токена");
    username = PlayerPrefs.GetString("Username", "TestPlayer");
    currentCharacter = new CharacterInfo
    {
        characterClass = characterClass,
        level = 1
    };
    DisplayCharacterInfo();
    return;
}
```

#### 3. В методе `LoadCharacter()` (строка 135-170):
```csharp
Debug.Log($"[GameScene] LoadCharacter: class={characterClass}");
Debug.Log($"[GameScene] GetCharacters response: success={response.success}, characters count={response.characters?.Length ?? 0}");
Debug.Log($"[GameScene] Проверяем персонажа: {character.characterClass}");
Debug.Log($"[GameScene] ✅ Персонаж найден: {characterClass}, Level {character.level}");
```

## Как отладить проблему

### Шаг 1: Включите фильтр логов в Unity Console
1. Откройте Unity Console (Window → General → Console)
2. Включите "Collapse" чтобы сгруппировать повторяющиеся логи
3. Оставьте включенными все типы: Log, Warning, Error

### Шаг 2: Запустите игру и следите за логами

Вы должны увидеть одну из следующих последовательностей:

#### Сценарий A: Успешная загрузка (тестовый режим)
```
[QuickTest] ✅ Установлен тестовый токен
[TEST MODE] 🚀 Пропускаем загрузку с сервера, показываем Warrior
[TEST MODE] 🚀 Пропускаем серверную проверку, загружаем GameScene напрямую
[GameScene] LoadCharacterInfo: token=test_token_for_multiplayer, characterId=test_character_Warrior, characterClass=Warrior
[GameScene][TEST MODE] 🚀 Пропускаем проверку токена, используем тестовые данные
GameScene загружена: TestPlayer - Warrior (Level 1)
```

#### Сценарий B: Проблема с токеном (реальная авторизация)
```
[GameScene] LoadCharacterInfo: token=<реальный_токен>, characterId=<id>, characterClass=Warrior
[GameScene] Ошибка проверки токена: <детали ошибки>
[GameScene] ❌ Ошибка проверки токена: <error>
```

#### Сценарий C: Проблема с персонажем
```
[GameScene] LoadCharacterInfo: token=<токен>, characterId=<id>, characterClass=Warrior
[GameScene] Токен валидный, username: <username>
[GameScene] LoadCharacter: class=Warrior
[GameScene] GetCharacters response: success=true, characters count=0
[GameScene] ❌ Ошибка загрузки персонажей! success=true
```

#### Сценарий D: Отсутствуют данные
```
[GameScene] LoadCharacterInfo: token=, characterId=, characterClass=
[GameScene] Нет токена! Возврат к LoginScene
```

### Шаг 3: Скопируйте логи

После того как игра выкинет вас обратно:
1. Откройте Unity Console
2. Найдите все логи с префиксом `[GameScene]`
3. Скопируйте их полностью (включая ошибки)

## Типичные ошибки и решения

### Ошибка 1: "Нет токена! Возврат к LoginScene"
**Причина**: PlayerPrefs пустой, токен отсутствует
**Решение**:
- Используйте тестовый режим (QuickMultiplayerTest.cs установит токен автоматически)
- Или выполните регистрацию/логин заново

### Ошибка 2: "Токен невалидный!"
**Причина**: Сервер не принимает токен (истёк или неправильный формат)
**Решение**:
- Проверьте что сервер на Render работает (https://aetherion-server-gv5u.onrender.com/health)
- Проверьте что вы правильно зарегистрировались/залогинились
- Используйте тестовый режим для обхода проверки

### Ошибка 3: "Персонаж класса Warrior не найден!"
**Причина**: На сервере нет персонажа, который вы выбрали
**Решение**:
- Проверьте логи создания персонажа в CharacterSelectionScene
- Убедитесь что персонаж был успешно создан (код 201)
- Попробуйте создать персонажа заново

### Ошибка 4: "Нет выбранного персонажа!"
**Причина**: `SelectedCharacterId` или `SelectedCharacterClass` не сохранены в PlayerPrefs
**Решение**:
- Проверьте логи CharacterSelectionManager - там должно быть `PlayerPrefs.SetString(...)`
- Убедитесь что нажали кнопку "В бой" и дождались успешного ответа

## Быстрое решение: Используйте тестовый режим

Если вы не можете найти проблему, используйте тестовый режим:

### Вариант 1: QuickMultiplayerTest.cs (автоматический)
Скрипт уже создан и работает при запуске игры:
- Устанавливает токен `test_token_for_multiplayer`
- Устанавливает username `TestPlayerXXXX`
- Устанавливает класс `Warrior`

### Вариант 2: Ручная установка через Unity Console
Выполните в Unity Console (Window → General → Console → в нижней части поле ввода):
```csharp
PlayerPrefs.SetString("UserToken", "test_token_for_multiplayer");
PlayerPrefs.SetString("Username", "TestPlayer");
PlayerPrefs.SetString("SelectedCharacterId", "test_character_Warrior");
PlayerPrefs.SetString("SelectedCharacterClass", "Warrior");
PlayerPrefs.Save();
Debug.Log("Тестовые данные установлены!");
```

После этого перезапустите игру.

## Проверка текущего состояния

Чтобы проверить что сохранено в PlayerPrefs, выполните в Unity Console:
```csharp
Debug.Log("UserToken: " + PlayerPrefs.GetString("UserToken", "<пусто>"));
Debug.Log("Username: " + PlayerPrefs.GetString("Username", "<пусто>"));
Debug.Log("SelectedCharacterId: " + PlayerPrefs.GetString("SelectedCharacterId", "<пусто>"));
Debug.Log("SelectedCharacterClass: " + PlayerPrefs.GetString("SelectedCharacterClass", "<пусто>"));
```

## Ожидаемый результат (правильная работа)

При правильной работе логи должны выглядеть так:

```
// CharacterSelectionScene
SelectOrCreateCharacter для класса: Warrior
Запрос списка персонажей: https://aetherion-server-gv5u.onrender.com/api/character
Список персонажей получен: {"success":true,"characters":[]}
Персонаж Warrior не найден, создаем нового
Создание персонажа класса: Warrior
JSON: {"name":"Warrior","class":"Warrior"}
HTTP/1.1 201 Created
Персонаж выбран: Warrior, Level 1

// GameScene
[GameScene] LoadCharacterInfo: token=<токен>, characterId=<id>, characterClass=Warrior
[GameScene] Токен валидный, username: <username>
[GameScene] LoadCharacter: class=Warrior
[GameScene] GetCharacters response: success=true, characters count=1
[GameScene] Проверяем персонажа: Warrior
[GameScene] ✅ Персонаж найден: Warrior, Level 1
GameScene загружена: <username> - Warrior (Level 1)
```

---

**После получения логов отправьте их для дальнейшей диагностики!**
