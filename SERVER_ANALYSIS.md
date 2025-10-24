# 🔍 АНАЛИЗ СЕРВЕРНОЙ ЧАСТИ И ПРОБЛЕМ

## 🌐 АРХИТЕКТУРА ПРОЕКТА:

### Сервер:
```
URL: https://aetherion-server-gv5u.onrender.com
Статус: ❌ НЕ РАБОТАЕТ (судя по GitHub sync failed)
Технологии:
  - Node.js + Socket.IO (WebSocket)
  - MongoDB (база данных)
  - REST API (авторизация, персонажи)
  - Render.com (хостинг)
```

### Клиент (Unity):
```
Основные компоненты:
  1. ApiClient.cs - REST API запросы (регистрация, логин, персонажи)
  2. SocketIOManager.cs - WebSocket подключение (мультиплеер)
  3. NetworkSyncManager.cs - Синхронизация игроков
  4. RoomManager.cs - Управление комнатами
```

---

## ❌ ТЕКУЩИЕ ПРОБЛЕМЫ:

### 1. Сервер не работает
**Симптомы:**
- GitHub показывает "Неудачная синхронизация"
- Клиент не может подключиться

**Возможные причины:**
1. Render.com сервер в спящем режиме (cold start)
2. Ошибки деплоя на Render
3. MongoDB не подключена
4. Проблемы с кодом сервера

---

### 2. Персонажи спавнятся БЕЗ скриптов
**Причина:**
- `SetupCharacterComponents()` НЕ ВЫЗЫВАЕТСЯ из-за проблем с мультиплеер логикой

**Где ошибка:**
```csharp
// ArenaManager.cs:1080
if (isMultiplayer && spawnedCharacter == null && spawnIndexReceived)
{
    SpawnSelectedCharacter(); // ← Здесь должны добавляться скрипты
}
```

**Почему не работает:**
- Сервер не отправляет `game_start` событие
- FALLBACK запускает лобби, но `SpawnSelectedCharacter()` может не вызываться

---

### 3. Последовательный спавн вместо одновременного
**Проблема:**
Игроки спавнятся по очереди, а не одновременно после countdown 3-2-1

**Причина:**
- Каждый клиент запускает свой FALLBACK независимо
- Нет синхронизации между клиентами

---

### 4. Модели невидимы (анимации видны)
**Симптомы:**
- Анимации работают (видно движение "невидимого" персонажа)
- Сами модели не отображаются

**Возможные причины:**
1. Renderer отключен
2. Layer неправильный (не рендерится камерой)
3. Material/Shader проблемы
4. Модель за пределами Frustum камеры

---

### 5. Два вида Fireball у Мага
**Проблема:**
Мag отправляет старый И новый fireball

**Причина:**
- На префабе есть старый `PlayerAttack` компонент
- ArenaManager добавляет новый `PlayerAttackNew`
- Оба компонента активны одновременно!

**Решение:**
Удалять старый `PlayerAttack` при добавлении `PlayerAttackNew`

---

## 📋 ПОСЛЕДОВАТЕЛЬНОСТЬ РАБОТЫ (КАК ДОЛЖНО БЫТЬ):

### 1. Авторизация:
```
LoginScene
  ↓
ApiClient.Login() → REST API → /api/auth/login
  ↓
Получаем token → сохраняем в PlayerPrefs
  ↓
CharacterSelectionScene
```

### 2. Выбор персонажа:
```
CharacterSelectionScene
  ↓
ApiClient.GetCharacters() → REST API → /api/character
  ↓
Показываем список персонажей
  ↓
Пользователь выбирает класс → нажимает Play
  ↓
ApiClient.SelectOrCreateCharacter() → REST API → /api/character/select
  ↓
Сохраняем: SelectedCharacterClass, SelectedCharacterId
  ↓
LoadingScene → ArenaScene
```

### 3. Подключение к мультиплееру:
```
ArenaScene Start()
  ↓
RoomManager.JoinOrCreateRoom()
  ↓
SocketIOManager.Connect(token)
  ↓
Emit: create_room или join_room
  ↓
Сервер отправляет: room_players
  ↓
NetworkSyncManager получает список игроков + spawnIndex
  ↓
Ждём lobby_created от сервера...
```

### 4. Запуск игры (ИДЕАЛЬНЫЙ СЦЕНАРИЙ):
```
Сервер: lobby_created (17000ms)
  ↓
ArenaManager.OnLobbyStarted(17000)
  ↓
Ждём 14 секунд
  ↓
Сервер: game_countdown (3, 2, 1)
  ↓
ArenaManager.OnCountdown()
  ↓
Сервер: game_start
  ↓
ArenaManager.OnGameStarted()
  ↓
SpawnSelectedCharacter() ← ЛОКАЛЬНЫЙ ИГРОК
SetupCharacterComponents() ← ДОБАВЛЯЕТ ВСЕ СКРИПТЫ!
  ↓
NetworkSyncManager.OnGameStart() ← СЕТЕВЫЕ ИГРОКИ
SpawnNetworkPlayer() для каждого pending игрока
```

### 5. Запуск игры (FALLBACK - СЕЙЧАС):
```
NetworkSyncManager получает room_players (2+ игрока)
  ↓
Сервер НЕ отправляет lobby_created ❌
  ↓
FALLBACK: ArenaManager.OnLobbyStarted(17000)
  ↓
Локальный countdown таймер: 14с + 3-2-1
  ↓
ArenaManager.OnGameStarted() ← ЛОКАЛЬНЫЙ таймер
  ↓
SpawnSelectedCharacter()
SetupCharacterComponents()
```

---

## 🔧 ЧТО НУЖНО ИСПРАВИТЬ В КЛИЕНТЕ:

### 1. Удалять старые компоненты с префабов

**Файл:** `ArenaManager.cs`
**Метод:** `SetupCharacterComponents()`
**Строка:** После 318 (перед добавлением PlayerAttackNew)

```csharp
// КРИТИЧЕСКОЕ: Удаляем старый PlayerAttack если есть!
PlayerAttack oldAttack = modelTransform.GetComponent<PlayerAttack>();
if (oldAttack != null)
{
    DestroyImmediate(oldAttack);
    Debug.Log("✓ Удалён старый PlayerAttack компонент");
}

// Теперь добавляем PlayerAttackNew...
```

---

### 2. Проверить почему SetupCharacterComponents() не вызывается

**Добавить больше логирования в OnGameStarted():**

```csharp
// ArenaManager.cs:1050
public void OnGameStarted()
{
    Debug.Log($"[ArenaManager] 🎮 GAME START! Спавним персонажа...");
    Debug.Log($"[ArenaManager] 🔍 gameStarted={gameStarted}");
    Debug.Log($"[ArenaManager] 🔍 isMultiplayer={isMultiplayer}");
    Debug.Log($"[ArenaManager] 🔍 spawnedCharacter={spawnedCharacter}");
    Debug.Log($"[ArenaManager] 🔍 spawnIndexReceived={spawnIndexReceived}");
    Debug.Log($"[ArenaManager] 🔍 assignedSpawnIndex={assignedSpawnIndex}");

    gameStarted = true;

    // ... rest of code
}
```

---

### 3. Исправить видимость моделей

**Проверить в SpawnSelectedCharacter():**

```csharp
// ArenaManager.cs:204 - после Instantiate
GameObject characterModel = Instantiate(characterPrefab, spawnedCharacter.transform);
characterModel.name = $"{selectedClass}Model";

// ДОБАВИТЬ ПРОВЕРКУ RENDERER
Renderer[] renderers = characterModel.GetComponentsInChildren<Renderer>();
Debug.Log($"[ArenaManager] Найдено Renderer'ов: {renderers.Length}");
foreach (Renderer r in renderers)
{
    Debug.Log($"[ArenaManager]   - {r.name}: enabled={r.enabled}, material={r.material != null}");
    r.enabled = true; // Убедимся что включены
}
```

---

### 4. Одновременный спавн (синхронизация)

**Проблема:** Каждый клиент запускает FALLBACK независимо

**Решение:** Добавить задержку основанную на `socketId`:

```csharp
// NetworkSyncManager.cs:385
if (data.players.Length >= 2 && ArenaManager.Instance != null)
{
    var lobbyUI = GameObject.Find("LobbyUI");
    if (lobbyUI == null)
    {
        // КРИТИЧЕСКОЕ: Запускаем лобби ТОЛЬКО у первого игрока в списке!
        bool isFirstPlayer = (data.players[0].socketId == data.yourSocketId);

        if (isFirstPlayer)
        {
            Debug.Log($"[NetworkSync] 🏁 FALLBACK: Я первый игрок, запускаю лобби");
            ArenaManager.Instance.OnLobbyStarted(17000);
        }
        else
        {
            Debug.Log($"[NetworkSync] ⏳ Ждём запуска лобби от первого игрока");
            // Подписываемся на событие от первого игрока или ждём game_start
        }
    }
}
```

---

## 🔍 ЧТО ПРОВЕРИТЬ НА СЕРВЕРЕ:

### 1. Render.com статус:
- Зайти на https://render.com
- Проверить статус деплоя
- Посмотреть логи сервера

### 2. Проверить событие lobby_created:
Сервер должен отправлять при 2+ игроках:
```javascript
// server.js
socket.on('room_players_request', () => {
    // ...
    if (room.players.length >= 2) {
        io.to(roomId).emit('lobby_created', { waitTime: 17000 });
    }
});
```

### 3. Проверить событие game_countdown:
```javascript
// После 14 секунд
setTimeout(() => {
    for (let i = 3; i >= 1; i--) {
        setTimeout(() => {
            io.to(roomId).emit('game_countdown', { count: i });
        }, (3 - i) * 1000);
    }
}, 14000);
```

### 4. Проверить событие game_start:
```javascript
// После countdown
setTimeout(() => {
    io.to(roomId).emit('game_start', {
        players: room.players
    });
}, 17000);
```

---

## 📊 ПРИОРИТЕТЫ ИСПРАВЛЕНИЙ:

### КРИТИЧЕСКОЕ (исправить сейчас):
1. ✅ Удалять старый PlayerAttack перед добавлением PlayerAttackNew
2. ✅ Добавить логирование в OnGameStarted() и SpawnSelectedCharacter()
3. ✅ Проверить почему SetupCharacterComponents() не вызывается

### ВАЖНОЕ (после базового теста):
4. ⏳ Исправить видимость моделей (Renderer check)
5. ⏳ Синхронизация запуска лобби (только первый игрок)

### МОЖНО ПОЗЖЕ:
6. 🔜 Запустить сервер на Render.com
7. 🔜 Проверить события lobby_created, game_countdown, game_start

---

## 🧪 ПЛАН ТЕСТИРОВАНИЯ:

### Тест 1: Локальный режим (без сервера)
```
1. PlayerPrefs.DeleteKey("CurrentRoomId")
2. Play ▶️ в Unity
3. Должен заспавниться персонаж СО ВСЕМИ скриптами
4. Проверить Console логи:
   ✅ "✓ Добавлен PlayerController"
   ✅ "✓ Добавлен PlayerAttackNew"
   ✅ "✓ Назначен BasicAttackConfig_Mage"
```

### Тест 2: Мультиплеер (с FALLBACK)
```
1. Запустить 2 клиента
2. Создать комнату
3. Присоединиться
4. Проверить FALLBACK лобби запускается
5. Дождаться countdown 3-2-1
6. Проверить что ОБА игрока заспавнились СО СКРИПТАМИ
```

---

**Начнём с КРИТИЧЕСКИХ исправлений!**
