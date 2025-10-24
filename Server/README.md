# 🎮 Aetherion MMO Server

Multiplayer server for Aetherion Unity game using Socket.IO and MongoDB.

## 📋 Features

- ✅ REST API для авторизации (регистрация, логин)
- ✅ REST API для персонажей (создание, выбор)
- ✅ REST API для комнат (создание, присоединение)
- ✅ WebSocket (Socket.IO) для реального времени
- ✅ Автоматический старт лобби при 2+ игроках
- ✅ Countdown 3-2-1 перед началом игры
- ✅ Синхронизация позиций, анимаций, атак
- ✅ Синхронизация снарядов и скиллов
- ✅ MongoDB для хранения данных

---

## 🚀 Quick Start

### 1. Установить зависимости

```bash
cd Server
npm install
```

### 2. Настроить переменные окружения (опционально)

Создайте файл `.env`:

```env
PORT=3000
MONGODB_URI=mongodb://localhost:27017/aetherion
JWT_SECRET=your-super-secret-key-change-this
```

### 3. Запустить MongoDB (если локально)

```bash
# Windows
mongod

# Linux/Mac
sudo systemctl start mongod
```

### 4. Запустить сервер

```bash
# Production
npm start

# Development (auto-reload)
npm run dev
```

Сервер запустится на `http://localhost:3000`

---

## 📡 API Endpoints

### Authentication

#### POST `/api/auth/register`
Регистрация нового пользователя

**Request:**
```json
{
  "username": "player1",
  "email": "player1@example.com",
  "password": "password123"
}
```

**Response:**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "507f1f77bcf86cd799439011",
    "username": "player1",
    "email": "player1@example.com"
  }
}
```

#### POST `/api/auth/login`
Вход пользователя

**Request:**
```json
{
  "username": "player1",
  "password": "password123"
}
```

**Response:**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "507f1f77bcf86cd799439011",
    "username": "player1",
    "email": "player1@example.com"
  }
}
```

#### GET `/api/auth/verify`
Проверка токена

**Headers:**
```
Authorization: Bearer <token>
```

**Response:**
```json
{
  "success": true,
  "user": {
    "userId": "507f1f77bcf86cd799439011",
    "username": "player1"
  }
}
```

---

### Characters

#### GET `/api/character`
Получить список персонажей пользователя

**Headers:**
```
Authorization: Bearer <token>
```

**Response:**
```json
{
  "success": true,
  "characters": [
    {
      "id": "507f1f77bcf86cd799439012",
      "characterClass": "Mage",
      "level": 5,
      "gold": 1000,
      "stats": {
        "strength": 5,
        "perception": 5,
        "endurance": 5,
        "wisdom": 10,
        "intelligence": 15,
        "agility": 7,
        "luck": 8
      }
    }
  ]
}
```

#### POST `/api/character/select`
Выбрать или создать персонажа

**Headers:**
```
Authorization: Bearer <token>
```

**Request:**
```json
{
  "characterClass": "Mage"
}
```

**Response:**
```json
{
  "success": true,
  "character": {
    "id": "507f1f77bcf86cd799439012",
    "characterClass": "Mage",
    "level": 1,
    "gold": 0,
    "stats": {
      "strength": 5,
      "perception": 5,
      "endurance": 5,
      "wisdom": 5,
      "intelligence": 5,
      "agility": 5,
      "luck": 5
    }
  }
}
```

---

### Rooms

#### POST `/api/room/create`
Создать комнату

**Headers:**
```
Authorization: Bearer <token>
```

**Request:**
```json
{
  "roomName": "My Room",
  "maxPlayers": 20,
  "characterClass": "Mage",
  "username": "player1"
}
```

**Response:**
```json
{
  "success": true,
  "room": {
    "roomId": "a1b2c3d4e5f6...",
    "roomName": "My Room",
    "maxPlayers": 20,
    "playerCount": 0
  }
}
```

---

## 🔌 WebSocket Events

### Client → Server

#### `join_room`
Присоединиться к комнате

**Emit:**
```javascript
socket.emit('join_room', {
  roomId: "a1b2c3d4e5f6...",
  characterClass: "Mage"
});
```

**Response:**
```javascript
socket.on('join_room_success', (data) => {
  // data = { roomId, spawnIndex }
});
```

#### `room_players_request`
Запросить список игроков в комнате

**Emit:**
```javascript
socket.emit('room_players_request', {
  roomId: "a1b2c3d4e5f6..."
});
```

**Response:**
```javascript
socket.on('room_players', (data) => {
  // data = { players: [...], yourSocketId, yourSpawnIndex }
});
```

#### `update_position`
Обновить позицию игрока

**Emit:**
```javascript
socket.emit('update_position', {
  position: { x: 10, y: 0, z: 5 },
  rotation: { x: 0, y: 90, z: 0 },
  velocity: { x: 1, y: 0, z: 0 },
  isMoving: true
});
```

#### `update_animation`
Обновить анимацию игрока

**Emit:**
```javascript
socket.emit('update_animation', {
  animationState: "Running"
});
```

#### `player_used_skill`
Игрок использовал скилл

**Emit:**
```javascript
socket.emit('player_used_skill', {
  skillName: "Fireball",
  skillType: "Projectile",
  targetPosition: { x: 15, y: 0, z: 10 }
});
```

#### `projectile_spawned`
Создан снаряд

**Emit:**
```javascript
socket.emit('projectile_spawned', {
  projectileType: "CelestialProjectile",
  spawnPosition: { x: 10, y: 1.5, z: 5 },
  direction: { x: 1, y: 0, z: 0 },
  targetId: "socketId123",
  damage: 50,
  speed: 10
});
```

---

### Server → Client

#### `lobby_created`
Лобби создано (2+ игроков)

**Data:**
```javascript
{
  waitTime: 17000 // milliseconds
}
```

#### `game_countdown`
Countdown перед стартом

**Data:**
```javascript
{
  count: 3 // 3, 2, 1
}
```

#### `game_start`
Игра началась - спавним всех!

**Data:**
```javascript
{
  players: [
    {
      socketId: "abc123",
      username: "player1",
      characterClass: "Mage",
      spawnIndex: 0,
      position: { x: 0, y: 0, z: 0 },
      stats: { ... }
    },
    ...
  ]
}
```

#### `player_joined`
Новый игрок присоединился

**Data:**
```javascript
{
  socketId: "xyz789",
  username: "player2",
  characterClass: "Warrior",
  spawnIndex: 1,
  stats: { ... }
}
```

#### `player_moved`
Игрок переместился

**Data:**
```javascript
{
  socketId: "abc123",
  position: { x: 10, y: 0, z: 5 },
  rotation: { x: 0, y: 90, z: 0 },
  velocity: { x: 1, y: 0, z: 0 },
  isMoving: true
}
```

#### `player_animation_changed`
Анимация игрока изменилась

**Data:**
```javascript
{
  socketId: "abc123",
  animationState: "Running"
}
```

---

## 🎮 Game Flow

### 1. Авторизация
```
1. Клиент отправляет POST /api/auth/login
2. Получает JWT token
3. Сохраняет token в PlayerPrefs
```

### 2. Выбор персонажа
```
1. Клиент отправляет POST /api/character/select
2. Получает данные персонажа (класс, характеристики)
3. Сохраняет в PlayerPrefs
```

### 3. Создание/присоединение к комнате
```
1. Клиент отправляет POST /api/room/create (или join)
2. Получает roomId
3. Подключается к WebSocket с токеном
4. Отправляет join_room с roomId и классом персонажа
```

### 4. Ожидание в лобби
```
1. Первый игрок подключается → ждет
2. Второй игрок подключается → сервер отправляет lobby_created
3. Ждем 14 секунд
4. Сервер отправляет game_countdown (3, 2, 1)
5. Сервер отправляет game_start
6. Все игроки спавнятся ОДНОВРЕМЕННО!
```

### 5. Игра
```
1. Клиенты отправляют update_position, update_animation
2. Сервер транслирует всем в комнате
3. При атаках/скиллах: projectile_spawned, visual_effect_spawned
4. Сервер синхронизирует все события
```

---

## 🔧 Development

### Тестирование без MongoDB

Сервер может работать БЕЗ MongoDB для тестирования. В этом случае:
- Авторизация будет недоступна
- Но WebSocket мультиплеер будет работать

### Логирование

Сервер выводит подробные логи:
```
✅ Socket connected: abc123 (user: player1)
[abc123] Joining room: xyz789 as Mage
[Room xyz789] Created
[Room xyz789] Player player1 joined (1/20)
[Room xyz789] Player player2 joined (2/20)
[Room xyz789] 🏁 LOBBY STARTED (2 players)
[Room xyz789] ⏱️ COUNTDOWN STARTED
[Room xyz789] Countdown: 3
[Room xyz789] Countdown: 2
[Room xyz789] Countdown: 1
[Room xyz789] 🎮 GAME STARTED
```

---

## 📦 Деплой на Render.com

### 1. Создать аккаунт на Render.com

### 2. Подключить GitHub репозиторий

### 3. Создать Web Service

- **Build Command:** `npm install`
- **Start Command:** `npm start`
- **Environment Variables:**
  - `MONGODB_URI` - MongoDB connection string
  - `JWT_SECRET` - Секретный ключ для JWT
  - `PORT` - Порт (автоматически от Render)

### 4. Подключить MongoDB Atlas (бесплатно)

1. Создать аккаунт на [MongoDB Atlas](https://www.mongodb.com/cloud/atlas)
2. Создать бесплатный кластер
3. Скопировать connection string
4. Добавить в `MONGODB_URI` на Render

---

## 🐛 Troubleshooting

### Сервер не запускается

**Проблема:** `Error: Cannot find module 'express'`

**Решение:**
```bash
npm install
```

---

### MongoDB connection error

**Проблема:** `MongooseServerSelectionError`

**Решение:**
1. Убедитесь что MongoDB запущен локально
2. Или используйте MongoDB Atlas
3. Или сервер работает в режиме "без БД"

---

### WebSocket не подключается

**Проблема:** Клиент не может подключиться к WebSocket

**Решение:**
1. Проверьте что сервер запущен
2. Проверьте `serverUrl` в Unity (SocketIOManager.cs)
3. Проверьте CORS настройки

---

## 📊 Statistics

**REST API Endpoints:** 7
**WebSocket Events (Client → Server):** 10
**WebSocket Events (Server → Client):** 15

---

## 📝 License

MIT

---

## 👥 Authors

Aetherion Team

---

**Ready to deploy!** 🚀
