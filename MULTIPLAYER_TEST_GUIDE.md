# 🎮 Руководство по тестированию мультиплеера

## ✅ Что уже сделано:

1. **Сервер развернут на Render.com**
   - URL: `https://aetherion-server-gv5u.onrender.com`
   - Статус: 🟢 Работает (Socket.io + MongoDB)

2. **Unity Client настроен**
   - NetworkManagers созданы в GameScene
   - WebSocketClient подключен к серверу
   - RoomManager готов к работе

3. **Тестовый режим активирован**
   - Автоматическая установка тестовых данных
   - Пропуск серверной аутентификации
   - Быстрый вход в арену

---

## 🚀 Как тестировать мультиплеер:

### Шаг 1: Запустить игру в Unity
1. Откройте Unity проект Aetherion
2. Нажмите **Play** в редакторе
3. Игра автоматически:
   - Установит тестовый токен
   - Выберет случайный username (TestPlayer1234)
   - Выберет класс Warrior

### Шаг 2: Выбор персонажа
1. В CharacterSelectionScene автоматически выберется Warrior
2. Можете выбрать другой класс (любой класс доступен)
3. Выберите 4 скилла для персонажа
4. Нажмите кнопку **"В бой"** (Play)

### Шаг 3: Загрузка в арену
1. Пройдет LoadingScene
2. Автоматически загрузится GameScene
3. В консоли вы увидите:
   ```
   [TEST MODE] 🚀 Пропускаем серверную проверку, загружаем GameScene напрямую
   [TEST MODE] ✅ Скиллы сохранены
   [TEST MODE] ✅ Персонаж выбран: Warrior
   ```

### Шаг 4: Подключение к серверу
После загрузки GameScene автоматически:
1. WebSocketClient подключится к серверу
2. RoomManager создаст или подключится к комнате
3. В консоли появятся логи:
   ```
   [WebSocket] Connecting to: https://aetherion-server-gv5u.onrender.com
   [WebSocket] Connected to server
   [RoomManager] Creating/Joining room...
   [RoomManager] Player joined room: <room_id>
   ```

---

## 🔍 Тестирование с двумя клиентами:

### Вариант 1: Unity Editor + Build
1. **В Unity Editor**: Запустите игру (Ctrl+P)
2. **Соберите Build**: File → Build and Run
3. Два клиента автоматически подключатся к одной комнате

### Вариант 2: Два Build-а
1. File → Build Settings → Build
2. Запустите `.exe` файл дважды
3. Оба клиента увидят друг друга

### Что должно произойти:
- ✅ Оба игрока появятся в одной комнате
- ✅ Вы увидите модель второго игрока
- ✅ Движение второго игрока синхронизируется
- ✅ Атаки и скиллы видны обоим игрокам
- ✅ HP/MP синхронизируются в реальном времени

---

## 📊 Проверка логов:

### Unity Console (Client):
```
[QuickTest] ✅ Установлен тестовый токен
[TEST MODE] 🚀 Пропускаем загрузку с сервера
[WebSocket] Connected to server
[RoomManager] Joined room: room_12345
[NetworkPlayer] Spawned remote player: TestPlayer5678
```

### Render Logs (Server):
```
[Socket.io] Client connected: socket_abc123
[Room] Player TestPlayer1234 joined room: room_12345
[Game] Position update from TestPlayer1234
[Game] Skill used by TestPlayer1234
```

---

## 🐛 Устранение проблем:

### Проблема 1: "Нет токена!"
**Решение**: Перезапустите Unity - QuickMultiplayerTest.cs должен автоматически установить токен

### Проблема 2: "WebSocket connection failed"
**Решение**:
1. Проверьте что сервер работает: https://aetherion-server-gv5u.onrender.com/health
2. Проверьте URL в NetworkManagers/WebSocketClient

### Проблема 3: "Второй игрок не появляется"
**Решение**:
1. Проверьте что оба клиента подключились к одной комнате
2. Смотрите логи сервера на Render.com
3. Убедитесь что NetworkManagers создан в GameScene (Tools → Aetherion → Setup Multiplayer Managers)

### Проблема 4: "401 Unauthorized"
**Решение**: Это нормально для теста - тестовый режим автоматически пропускает эту ошибку

---

## 📝 Следующие шаги после теста:

1. **Если мультиплеер работает**:
   - Протестировать все скиллы
   - Протестировать PvP бой
   - Добавить чат между игроками
   - Реализовать систему комнат (лобби)

2. **Если есть проблемы**:
   - Скопируйте логи из Unity Console
   - Скопируйте логи из Render.com
   - Опишите что не работает

---

## 🎯 Текущая конфигурация:

- **Server URL**: `https://aetherion-server-gv5u.onrender.com`
- **Test Token**: `test_token_for_multiplayer`
- **Default Class**: `Warrior`
- **Room System**: Автоматическое создание/подключение
- **Max Players per Room**: 20 (настраивается в RoomManager)

---

## 🔧 Технические детали:

### Файлы тестового режима:
- `Assets/Scripts/Debug/QuickMultiplayerTest.cs` - Автоустановка тестовых данных
- `Assets/Scripts/UI/CharacterSelectionManager.cs` - Пропуск аутентификации
- `Assets/Scripts/Network/WebSocketClient.cs` - Socket.io клиент
- `Assets/Scripts/Network/RoomManager.cs` - Управление комнатами

### Серверные файлы:
- `SERVER_CODE/server.js` - Главный файл сервера
- `SERVER_CODE/socket/gameSocket.js` - WebSocket обработчики
- `SERVER_CODE/routes/room.js` - REST API для комнат

---

**Удачного тестирования! 🚀**

Если возникнут проблемы - присылайте скриншоты консоли Unity и логов Render.
