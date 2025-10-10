# Aetherion MMO - Полное руководство по настройке

## 📋 Оглавление
1. [Архитектура системы](#архитектура-системы)
2. [Серверная часть (Backend)](#серверная-часть-backend)
3. [Клиентская часть (Unity)](#клиентская-часть-unity)
4. [Развертывание на Render](#развертывание-на-render)
5. [Тестирование](#тестирование)
6. [Решение проблем](#решение-проблем)

---

## 🏗️ Архитектура системы

### Гибридный подход
- **REST API** - для статических операций (создание/поиск комнат, аутентификация)
- **WebSocket (Socket.io)** - для реального времени (позиция, анимация, бой)

### Технологический стек
- **Backend**: Node.js + Express + Socket.io + MongoDB
- **Frontend**: Unity 2022.3.11f1 + C#
- **Database**: MongoDB Atlas (облако)
- **Hosting**: Render.com

### Tick Rate
- **Позиция**: 10 Hz (каждые 0.1 секунды)
- **Бой**: 20 Hz (каждые 0.05 секунды)
- **HP/MP**: При изменении + каждые 0.5 секунды

---

## 🖥️ Серверная часть (Backend)

### 1. Установка зависимостей

Обновите `package.json`:

```json
{
  "name": "aetherion-server",
  "version": "2.0.0",
  "description": "Aetherion MMO Server with WebSocket support",
  "main": "server.js",
  "scripts": {
    "start": "node server.js",
    "dev": "nodemon server.js"
  },
  "dependencies": {
    "express": "^4.18.2",
    "mongoose": "^7.0.0",
    "bcrypt": "^5.1.0",
    "jsonwebtoken": "^9.0.0",
    "cors": "^2.8.5",
    "dotenv": "^16.0.3",
    "socket.io": "^4.6.0",
    "uuid": "^9.0.0"
  },
  "devDependencies": {
    "nodemon": "^2.0.20"
  },
  "engines": {
    "node": ">=16.0.0"
  }
}
```

Установите:
```bash
cd SERVER_CODE
npm install
```

### 2. Структура файлов

```
SERVER_CODE/
├── server.js                 # Главный файл сервера
├── models/
│   ├── User.js              # Модель пользователя
│   ├── Character.js         # Модель персонажа
│   └── Room.js              # НОВОЕ: Модель комнаты
├── routes/
│   ├── auth.js              # Регистрация/логин
│   ├── character.js         # Управление персонажами
│   └── room.js              # НОВОЕ: Управление комнатами
├── socket/
│   └── gameSocket.js        # НОВОЕ: WebSocket сервер
├── middleware/
│   └── auth.js              # JWT аутентификация
└── .env                     # Переменные окружения
```

### 3. Переменные окружения (.env)

```env
# MongoDB
MONGODB_URI=mongodb+srv://username:password@cluster.mongodb.net/aetherion?retryWrites=true&w=majority

# JWT Secret
JWT_SECRET=your_super_secret_jwt_key_here_change_in_production

# Server
PORT=3000
NODE_ENV=production

# CORS (для Unity клиента)
ALLOWED_ORIGINS=*
```

### 4. Запуск локально

```bash
npm start
```

Сервер запустится на `http://localhost:3000`

Проверка:
- REST API: `http://localhost:3000/health`
- WebSocket: `ws://localhost:3000`

---

## 🎮 Клиентская часть (Unity)

### 1. Созданные скрипты

**Network Scripts** (`Assets/Scripts/Network/`):
- `WebSocketClient.cs` - Socket.io клиент для Unity
- `RoomManager.cs` - Управление комнатами (REST API)
- `NetworkPlayer.cs` - Представление удаленного игрока
- `NetworkSyncManager.cs` - Синхронизация всех игроков
- `NetworkCombatSync.cs` - Синхронизация боя

**Модифицированные скрипты**:
- `GameSceneManager.cs` - BattleButton теперь создает/ищет комнату
- `ArenaManager.cs` - Поддержка мультиплеера при спавне

### 2. Настройка Unity сцены

#### GameScene (главное меню)
1. Убедитесь что `GameSceneManager` прикреплен к пустому GameObject
2. Настройте кнопку BattleButton в Inspector
3. Добавьте `RoomManager` в сцену:
   - Создайте пустой GameObject → "RoomManager"
   - Добавьте компонент `RoomManager`
   - Установите Server URL: `https://aetherion-server-gv5u.onrender.com`
4. Добавьте `WebSocketClient` в сцену:
   - Создайте пустой GameObject → "WebSocketClient"
   - Добавьте компонент `WebSocketClient`
   - Установите Server URL: `https://aetherion-server-gv5u.onrender.com`

**ВАЖНО**: Оба менеджера должны иметь `DontDestroyOnLoad`, чтобы сохраняться между сценами!

#### ArenaScene (игровая арена)
1. `ArenaManager` уже настроен автоматически
2. Добавьте массив спаун-поинтов (опционально):
   - В Inspector → ArenaManager → Multiplayer Spawn Points
   - Перетащите Transform'ы точек спавна
3. Назначьте префабы персонажей:
   - Warrior Prefab
   - Mage Prefab
   - Archer Prefab
   - Rogue Prefab
   - Paladin Prefab

### 3. Создание Nameplate Prefab (опционально)

Табличка с именем над головой игрока:

1. Создайте пустой GameObject → "PlayerNameplate"
2. Добавьте Canvas (World Space):
   - Render Mode: World Space
   - Scale: 0.01, 0.01, 0.01
3. Добавьте дочерний TextMeshPro:
   - Имя: "UsernameText"
   - Font Size: 24
   - Alignment: Center
   - Color: White
4. Добавьте Image для HP бара:
   - Имя: "HealthBar"
   - Image Type: Filled
   - Fill Method: Horizontal
   - Color: Red

Сохраните как Prefab: `Assets/Prefabs/PlayerNameplate.prefab`

Назначьте в `NetworkSyncManager` → Nameplate Prefab

---

## 🚀 Развертывание на Render

### 1. Подготовка репозитория

Убедитесь что все серверные файлы закоммичены:

```bash
git add SERVER_CODE/
git commit -m "Add multiplayer server with WebSocket support"
git push origin main
```

### 2. Создание Web Service на Render

1. Зайдите на [Render.com](https://render.com)
2. Dashboard → New → Web Service
3. Подключите ваш GitHub репозиторий
4. Настройки:
   - **Name**: `aetherion-server`
   - **Region**: Frankfurt (EU Central) - ближе к вашим игрокам
   - **Branch**: `main`
   - **Root Directory**: `SERVER_CODE`
   - **Runtime**: Node
   - **Build Command**: `npm install`
   - **Start Command**: `npm start`
   - **Instance Type**: Free (для тестирования) или Starter ($7/мес для продакшена)

5. Environment Variables (добавьте):
   ```
   MONGODB_URI=mongodb+srv://...
   JWT_SECRET=your_secret_key
   NODE_ENV=production
   PORT=3000
   ```

6. Нажмите **Create Web Service**

### 3. После развертывания

Render даст вам URL вида: `https://aetherion-server-xxxx.onrender.com`

**Важно**: Free tier засыпает через 15 минут бездействия. При первом запросе может быть задержка 30-60 секунд.

### 4. Обновление Unity клиента

После развертывания обновите URL в Unity:

**RoomManager.cs**:
```csharp
[SerializeField] private string serverUrl = "https://aetherion-server-xxxx.onrender.com";
```

**WebSocketClient.cs**:
```csharp
[SerializeField] private string serverUrl = "https://aetherion-server-xxxx.onrender.com";
```

---

## 🧪 Тестирование

### Тест 1: Проверка сервера

```bash
# Health check
curl https://aetherion-server-xxxx.onrender.com/health

# Ожидаемый ответ:
{
  "status": "healthy",
  "uptime": 123.45,
  "mongodb": "connected",
  "timestamp": 1234567890
}
```

### Тест 2: Регистрация и логин

В Unity:
1. Запустите игру
2. Зарегистрируйтесь с новым аккаунтом
3. Войдите в систему
4. Создайте персонажа
5. Войдите в GameScene

Проверьте консоль Unity на ошибки.

### Тест 3: Создание комнаты

1. В GameScene нажмите кнопку "В бой"
2. Консоль должна показать:
   ```
   [GameScene] Поиск доступных комнат...
   [RoomManager] Получено комнат: 0
   [GameScene] Нет комнат, создаем первую
   [RoomManager] ✅ Комната создана: YourName's Arena
   [WebSocket] ✅ Подключено!
   [WebSocket] ✅ Вошли в комнату
   ```

3. Загрузится ArenaScene

### Тест 4: Мультиплеер (2 клиента)

1. **Клиент 1**: Нажмите "В бой" → Создастся комната
2. **Клиент 2**: Нажмите "В бой" → Присоединится к комнате Клиента 1
3. Оба игрока должны видеть друг друга в ArenaScene
4. Движение синхронизируется в реальном времени
5. Атаки между игроками работают

### Тест 5: Бой

1. Наведите мышку на другого игрока
2. Кликните для атаки
3. У цели должен уменьшиться HP бар
4. Консоль покажет:
   ```
   [NetworkCombatSync] ⚔️ Атака отправлена на сервер: 25 урона
   [NetworkSync] Игрок получил 25 урона
   ```

---

## 🐛 Решение проблем

### Проблема 1: "WebSocket не подключен"

**Причина**: WebSocketClient не создан или не сохранился между сценами

**Решение**:
1. Убедитесь что GameObject с WebSocketClient имеет `DontDestroyOnLoad`
2. Проверьте что WebSocketClient.Instance != null перед использованием
3. В GameScene добавьте проверку:
   ```csharp
   if (WebSocketClient.Instance == null)
   {
       GameObject ws = new GameObject("WebSocketClient");
       ws.AddComponent<WebSocketClient>();
   }
   ```

### Проблема 2: "Не удалось создать комнату - 401 Unauthorized"

**Причина**: Токен JWT невалидный или отсутствует

**Решение**:
1. Проверьте что токен сохранен: `PlayerPrefs.GetString("UserToken")`
2. Перелогиньтесь
3. Убедитесь что JWT_SECRET на сервере совпадает

### Проблема 3: "Игроки не видят друг друга"

**Причина**: NetworkSyncManager не подписан на события WebSocket

**Решение**:
1. Убедитесь что NetworkSyncManager создается в ArenaScene
2. Проверьте что `WebSocketClient.Instance.StartListening()` вызван
3. Проверьте консоль сервера на ошибки

### Проблема 4: "Render сервер засыпает"

**Причина**: Free tier Render засыпает через 15 минут

**Решение**:
- **Временное**: Перейдите на Starter plan ($7/мес) - сервер не засыпает
- **Альтернатива**: Используйте [UptimeRobot](https://uptimerobot.com) для пинга каждые 5 минут
- **Для разработки**: Запускайте сервер локально (`npm start`)

### Проблема 5: "MongoDB connection failed"

**Причина**: Неверный MONGODB_URI или IP не добавлен в whitelist

**Решение**:
1. Зайдите в MongoDB Atlas → Network Access
2. Добавьте `0.0.0.0/0` (разрешить все IP)
3. Проверьте что connection string правильный
4. Убедитесь что пароль не содержит специальных символов (используйте URL encoding)

### Проблема 6: "CORS ошибка"

**Причина**: Unity WebGL не может подключиться из-за CORS

**Решение**:
В `server.js`:
```javascript
app.use(cors({
    origin: '*', // Разрешить все для разработки
    credentials: true
}));

const io = socketIo(server, {
    cors: {
        origin: "*",
        methods: ["GET", "POST"]
    }
});
```

---

## 📊 Мониторинг

### Render Dashboard
- Логи: Render Dashboard → aetherion-server → Logs
- Метрики: CPU, Memory, Requests

### MongoDB Atlas
- Database → Metrics
- Collections → Rooms (проверьте что старые комнаты удаляются)

### Unity Console
Фильтры для поиска проблем:
- `[NetworkSync]` - синхронизация
- `[WebSocket]` - подключение
- `[RoomManager]` - комнаты
- `[NetworkCombatSync]` - бой

---

## 🎯 Оптимизация для мобильных устройств (Android)

### 1. Уменьшить частоту синхронизации

В `NetworkSyncManager.cs`:
```csharp
[SerializeField] private float positionSyncInterval = 0.2f; // Было 0.1 (5 Hz вместо 10 Hz)
```

### 2. Использовать LOD для удаленных игроков

```csharp
// В NetworkPlayer.cs
void Update()
{
    float distance = Vector3.Distance(transform.position, Camera.main.transform.position);

    // Если игрок далеко - снизить частоту обновлений
    if (distance > 30f)
    {
        positionLerpSpeed = 5f; // Медленнее
    }
    else
    {
        positionLerpSpeed = 10f; // Нормально
    }
}
```

### 3. Качество графики

Project Settings → Quality → Android → Low:
- Shadows: Disable
- Anti-Aliasing: Disabled
- Texture Quality: Half Res

---

## 📝 Заметки разработчика

### Текущие ограничения
1. **Без валидации на сервере**: Сервер верит клиенту (возможен читинг)
2. **Нет интерполяции**: Движение может выглядеть дерганным при плохом интернете
3. **Нет lag compensation**: Стрельба требует предсказания
4. **Нет observer pattern**: Игроки за пределами видимости тоже синхронизируются

### Будущие улучшения
- [ ] Серверная валидация урона
- [ ] Client-side prediction для движения
- [ ] Lag compensation для выстрелов
- [ ] Зоны интереса (Area of Interest) - не синхронизировать далеких игроков
- [ ] Reconnect механизм при разрыве соединения
- [ ] Matchmaking по рейтингу
- [ ] Чат в комнате
- [ ] Голосовой чат (WebRTC)

---

## ✅ Чеклист перед запуском

- [ ] MongoDB Atlas настроен и работает
- [ ] .env файл с правильными credentials
- [ ] Socket.io добавлен в package.json
- [ ] Все серверные файлы закоммичены в Git
- [ ] Render Web Service создан и развернут
- [ ] Unity скрипты добавлены в проект
- [ ] RoomManager и WebSocketClient в GameScene
- [ ] Server URL обновлен в Unity скриптах
- [ ] Тест с 2 клиентами пройден
- [ ] Бой между игроками работает

---

## 📞 Поддержка

Если что-то не работает:
1. Проверьте логи Render: Dashboard → Logs
2. Проверьте Unity Console на ошибки
3. Проверьте Network tab в браузере (если WebGL)
4. Создайте Issue в GitHub репозитории

---

**Версия**: 2.0.0
**Дата**: 2025-10-10
**Автор**: Claude (Anthropic)
**Проект**: Aetherion MMO
