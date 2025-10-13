# 🔧 Исправление API выбора/создания персонажа

## Проблема

При попытке выбрать персонажа возникала ошибка:
```
HTTP/1.1 400 Bad Request
{"success":false,"message":"characterId обязателен"}
```

## Причина

Клиент (Unity) неправильно использовал endpoint `/api/character/select`:
- Отправлял `{"characterClass":"Mage"}`
- А сервер ожидал `{"characterId":"<id>"}`

## Серверные Endpoints

### 1. **POST /api/character** - Создание нового персонажа
```json
// Request
{
  "name": "Warrior",
  "class": "Warrior"
}

// Response (201)
{
  "success": true,
  "message": "Персонаж создан успешно",
  "character": { ... }
}
```

### 2. **POST /api/character/select** - Выбор существующего персонажа
```json
// Request
{
  "characterId": "507f1f77bcf86cd799439011"
}

// Response (200)
{
  "success": true,
  "message": "Персонаж выбран",
  "character": { ... }
}
```

### 3. **GET /api/character** - Получить список всех персонажей
```json
// Response (200)
{
  "success": true,
  "characters": [
    { "id": "...", "characterClass": "Warrior", ... },
    { "id": "...", "characterClass": "Mage", ... }
  ]
}
```

## Решение

### Изменения в [CharacterData.cs](Assets/Scripts/Data/CharacterData.cs):

#### Добавлен `CreateCharacterRequest`:
```csharp
[Serializable]
public class CreateCharacterRequest
{
    public string name;
    private string @class; // Используем @ для обхода зарезервированного слова

    public string characterClass
    {
        get { return @class; }
        set { @class = value; }
    }
}
```

#### Изменен `SelectCharacterRequest`:
```csharp
// БЫЛО:
public class SelectCharacterRequest
{
    public string characterClass; // ❌ Неправильно
}

// СТАЛО:
public class SelectCharacterRequest
{
    public string characterId; // ✅ Правильно
}
```

### Изменения в [ApiClient.cs](Assets/Scripts/Network/ApiClient.cs):

#### Добавлены новые методы:

1. **CreateCharacter** - создание нового персонажа
```csharp
public void CreateCharacter(string token, CharacterClass characterClass,
    Action<CharacterResponse> onSuccess, Action<string> onError)
{
    string className = characterClass.ToString();
    string json = $"{{\"name\":\"{className}\",\"class\":\"{className}\"}}";
    // Формируем JSON вручную из-за зарезервированного слова "class"

    StartCoroutine(PostRequestWithToken(CREATE_CHARACTER_ENDPOINT, token, json, onSuccess, onError));
}
```

2. **SelectCharacter** - выбор существующего персонажа
```csharp
public void SelectCharacter(string token, string characterId,
    Action<CharacterResponse> onSuccess, Action<string> onError)
{
    SelectCharacterRequest request = new SelectCharacterRequest
    {
        characterId = characterId
    };

    string json = JsonUtility.ToJson(request);
    StartCoroutine(PostRequestWithToken(SELECT_CHARACTER_ENDPOINT, token, json, onSuccess, onError));
}
```

3. **SelectOrCreateCharacter** - умная логика (обновлен)
```csharp
public void SelectOrCreateCharacter(string token, CharacterClass characterClass,
    Action<CharacterResponse> onSuccess, Action<string> onError)
{
    // Шаг 1: Получаем список всех персонажей
    GetCharacters(token,
        onGetSuccess: (response) =>
        {
            // Шаг 2: Ищем персонажа нужного класса
            CharacterInfo existingChar = null;
            foreach (var character in response.characters)
            {
                if (character.characterClass == characterClass.ToString())
                {
                    existingChar = character;
                    break;
                }
            }

            if (existingChar != null)
            {
                // Персонаж существует - выбираем его
                SelectCharacter(token, existingChar.id, onSuccess, onError);
            }
            else
            {
                // Персонажа нет - создаем нового
                CreateCharacter(token, characterClass, onSuccess, onError);
            }
        },
        onGetError: (error) =>
        {
            onError?.Invoke(error);
        }
    );
}
```

## Логика работы

### Сценарий 1: Первый запуск (нет персонажей)
1. Пользователь выбирает класс Warrior и нажимает "В бой"
2. `SelectOrCreateCharacter(token, Warrior, ...)`
3. Запрос GET /api/character → `{"success":true,"characters":[]}`
4. Список пуст → вызов `CreateCharacter(token, Warrior, ...)`
5. Запрос POST /api/character → `{"name":"Warrior","class":"Warrior"}`
6. Ответ: `{"success":true,"character":{...}}`
7. Персонаж создан и выбран ✅

### Сценарий 2: Повторный запуск (персонаж существует)
1. Пользователь выбирает класс Warrior и нажимает "В бой"
2. `SelectOrCreateCharacter(token, Warrior, ...)`
3. Запрос GET /api/character → `{"success":true,"characters":[{"id":"123","characterClass":"Warrior",...}]}`
4. Найден Warrior с id=123 → вызов `SelectCharacter(token, "123", ...)`
5. Запрос POST /api/character/select → `{"characterId":"123"}`
6. Ответ: `{"success":true,"character":{...}}`
7. Персонаж выбран ✅

### Сценарий 3: Создание второго персонажа
1. Пользователь создал Warrior, теперь выбирает Mage
2. `SelectOrCreateCharacter(token, Mage, ...)`
3. Запрос GET /api/character → `{"success":true,"characters":[{"id":"123","characterClass":"Warrior"}]}`
4. Mage не найден → вызов `CreateCharacter(token, Mage, ...)`
5. Запрос POST /api/character → `{"name":"Mage","class":"Mage"}`
6. Ответ: `{"success":true,"character":{...}}`
7. Новый персонаж Mage создан ✅

## Тестовый режим

В [CharacterSelectionManager.cs](Assets/Scripts/UI/CharacterSelectionManager.cs) есть тестовый режим:

```csharp
// Если токен тестовый - пропускаем серверную проверку
if (token == "test_token_for_multiplayer")
{
    Debug.Log("[TEST MODE] 🚀 Пропускаем серверную проверку");
    PlayerPrefs.SetString("SelectedCharacterId", "test_character_" + selectedClass.ToString());
    PlayerPrefs.SetString("SelectedCharacterClass", selectedClass.ToString());
    SceneManager.LoadScene(loadingSceneName);
    return;
}
```

## Проверка работы

### Unity Console (правильная работа):
```
SelectOrCreateCharacter для класса: Mage
Запрос списка персонажей: https://aetherion-server-gv5u.onrender.com/api/character
Список персонажей получен: {"success":true,"characters":[...]}
Персонаж Mage не найден, создаем нового
Создание персонажа класса: Mage
JSON: {"name":"Mage","class":"Mage"}
Выбор персонажа: https://aetherion-server-gv5u.onrender.com/api/character
HTTP/1.1 201 Created
Персонаж выбран: Mage, Level 1
```

### Unity Console (ошибка до исправления):
```
Отправляем запрос для класса: Mage
Данные: {"characterClass":"Mage"}
Ошибка: HTTP/1.1 400 Bad Request
Ответ сервера при ошибке: {"success":false,"message":"characterId обязателен"}
```

## Файлы изменены

1. ✅ [Assets/Scripts/Data/CharacterData.cs](Assets/Scripts/Data/CharacterData.cs)
   - Добавлен `CreateCharacterRequest`
   - Изменен `SelectCharacterRequest`

2. ✅ [Assets/Scripts/Network/ApiClient.cs](Assets/Scripts/Network/ApiClient.cs)
   - Добавлен `CREATE_CHARACTER_ENDPOINT`
   - Добавлен метод `CreateCharacter()`
   - Добавлен метод `SelectCharacter()`
   - Переписан метод `SelectOrCreateCharacter()` с правильной логикой

3. ✅ [Assets/Scripts/UI/CharacterSelectionManager.cs](Assets/Scripts/UI/CharacterSelectionManager.cs)
   - Добавлен тестовый режим (уже был)

## Следующие шаги

Теперь система должна работать правильно:
1. При первом выборе класса - создается новый персонаж
2. При повторном выборе того же класса - выбирается существующий персонаж
3. Можно создать до 5 персонажей (лимит сервера)
4. Тестовый режим работает без сервера

**Протестируйте:**
1. Удалите тестовый токен: `PlayerPrefs.DeleteAll()` в Unity Console
2. Зарегистрируйтесь/войдите в игру
3. Выберите класс и нажмите "В бой"
4. Проверьте что персонаж создается успешно
