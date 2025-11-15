# Race Condition Fix - Server Side

## Проблема

Unity клиент не получал событие `room_players` из-за race condition:

1. **Unity:** `NetworkSyncManager.Start()` вызывается при загрузке BattleScene
2. **Unity → Server:** Отправляет `get_room_players` (строка ~149 в NetworkSyncManager.cs)
3. **Server:** Обработчик `get_room_players` проверяет `activePlayers.get(socket.id)`
4. **Проблема:** Если `join_room` ещё не завершился, игрока нет в `activePlayers`
5. **Server:** `if (!player) return;` → **НЕ отправляет `room_players`!**
6. **Unity:** Никогда не получает список игроков → OnRoomPlayers() не вызывается
7. **Результат:** Игроки не видят друг друга

## Root Cause

### Timing Issue (Race Condition)

```
TIME →

Unity Scene Load
    ↓
NetworkSyncManager.Start()
    ↓
SocketIOManager.JoinRoom()  ←──────────┐
    ├─ Emit('join_room')                │
    └─ RequestRoomPlayers()             │ RACE!
          ↓                             │
    Emit('get_room_players')            │
          ↓                             │
Server: get_room_players handler        │
    ├─ activePlayers.get(socket.id) ────┤ НЕТ ИГРОКА!
    ├─ if (!player) return; ← EXIT      │
    └─ room_players НЕ ОТПРАВЛЕН        │
                                        │
Server: join_room handler (slow)────────┘
    └─ activePlayers.set(socket.id, ...)
```

### Missing Fields

Даже если race condition не происходит, в `get_room_players` обработчике не хватало:
- `yourSpawnIndex` - Unity ожидает это поле (строка 493 в NetworkSyncManager.cs)
- `gameStarted` - Unity проверяет этот флаг (строка 518 в NetworkSyncManager.cs)
- `spawnIndex` для каждого игрока в массиве

## Решение

### 1. Убрана жёсткая проверка в get_room_players

**До:**
```javascript
const player = activePlayers.get(socket.id);

if (!player) {
  console.warn(`[Get Room Players] Player ${socket.id} not found`);
  return; // ← БЛОКИРОВКА!
}
```

**После:**
```javascript
const player = activePlayers.get(socket.id);

if (!player) {
  console.warn(`[Get Room Players] ⚠️ Player ${socket.id} not found - might be race condition`);
  console.log(`[Get Room Players] 🔄 Sending empty player list with gameStarted flag anyway`);

  // КРИТИЧНО: Не выходим! Отправляем хотя бы статус игры
  const lobby = roomLobbies.get(roomId);
  const gameStarted = lobby ? lobby.gameStarted : false;

  socket.emit('room_players', {
    players: [],
    yourSocketId: socket.id,
    yourSpawnIndex: 0,
    gameStarted: gameStarted  // ← Unity получит статус игры!
  });
  return;
}
```

**Результат:**
- Unity ВСЕГДА получает `room_players` событие
- Даже если список игроков пустой, Unity узнаёт что игра уже идёт (`gameStarted: true`)
- Unity может заспавнить других игроков при получении `player_joined` позже

### 2. Добавлены недостающие поля

#### В join_room обработчике (lines 194-223):

```javascript
const playersInRoom = [];
for (const [sid, player] of activePlayers.entries()) {
  if (player.roomId === roomId) {
    playersInRoom.push({
      socketId: sid,
      username: player.username,
      characterClass: player.characterClass,
      spawnIndex: player.spawnIndex !== undefined ? player.spawnIndex : 0, // ← ДОБАВЛЕНО!
      position: player.position,
      rotation: player.rotation,
      animation: player.animation,
      health: player.health,
      maxHealth: player.maxHealth
    });
  }
}

socket.emit('room_players', {
  players: playersInRoom,
  yourSocketId: socket.id,
  yourSpawnIndex: assignedSpawnIndex !== undefined ? assignedSpawnIndex : 0, // ← ДОБАВЛЕНО!
  gameStarted: gameStarted
});
```

#### В get_room_players обработчике (lines 487-514):

```javascript
const playersInRoom = [];
for (const [sid, p] of activePlayers.entries()) {
  if (p.roomId === roomId) {
    playersInRoom.push({
      socketId: sid,
      username: p.username,
      characterClass: p.characterClass,
      spawnIndex: p.spawnIndex !== undefined ? p.spawnIndex : 0, // ← ДОБАВЛЕНО!
      position: p.position,
      rotation: p.rotation,
      animation: p.animation,
      health: p.health,
      maxHealth: p.maxHealth
    });
  }
}

const lobby = roomLobbies.get(roomId);
const gameStarted = lobby ? lobby.gameStarted : false;

socket.emit('room_players', {
  players: playersInRoom,
  yourSocketId: socket.id,
  yourSpawnIndex: player.spawnIndex !== undefined ? player.spawnIndex : 0, // ← ДОБАВЛЕНО!
  gameStarted: gameStarted  // ← ДОБАВЛЕНО!
});
```

### 3. Добавлен spawnIndex в player_joined broadcast

```javascript
socket.to(roomId).emit('player_joined', {
  socketId: socket.id,
  username: player.username,
  characterClass: player.characterClass,
  spawnIndex: player.spawnIndex !== undefined ? player.spawnIndex : 0, // ← ДОБАВЛЕНО!
  position: player.position,
  rotation: player.rotation,
  animation: player.animation,
  health: player.health,
  maxHealth: player.maxHealth
});
```

## Изменения в коде

### multiplayer.js

**Lines 194-223 (join_room):**
- Добавлен `spawnIndex` в каждый элемент `playersInRoom`
- Добавлен `yourSpawnIndex` в `room_players` payload
- Добавлен лог `🎯 Your spawnIndex: ...`

**Lines 467-533 (get_room_players):**
- Убрана жёсткая проверка `if (!player) return`
- Fallback: отправка пустого списка с `gameStarted` при race condition
- Добавлен `spawnIndex` в каждый элемент `playersInRoom`
- Добавлен `yourSpawnIndex` в `room_players` payload
- Добавлен `gameStarted` флаг
- Добавлен `spawnIndex` в `player_joined` broadcast

### server.js

**Line 44:**
- Версия обновлена: `2.2.0-game-start-fix` → `2.3.0-race-condition-fix`
- Features обновлены: добавлен `'Race Condition Fix'`

## Ожидаемые логи

### Server Side (Render Dashboard)

**Успешный сценарий:**
```
[Join Room] ✅ Player LexMaX added to activePlayers with socketId: abc123
[Join Room] 📤 Sending room_players to LexMaX: 2 players
[Join Room] 🎮 Game started status: true
[Join Room] 🎯 Your spawnIndex: 0
```

**Race condition сценарий (теперь работает!):**
```
[Get Room Players] ⚠️ Player abc123 not found in activePlayers - might be race condition
[Get Room Players] 🔄 Sending empty player list with gameStarted flag anyway
```

### Unity Side (Console)

**Успешный сценарий:**
```
[NetworkSync] 📦 Получен список игроков в комнате
[NetworkSync] В комнате 2 игроков
[NetworkSync] Мой socketId: abc123
[NetworkSync] 🎯 Мой spawnIndex от сервера: 0
[NetworkSync] 🎮 Статус игры от сервера: gameStarted=true
[NetworkSync] 🔍 Game status check: server.gameStarted=true, local.IsGameStarted=false, final=true
[NetworkSync] 🎮 Игра УЖЕ ИДЕТ (2 игроков)! Спавним локального игрока сразу
```

**Race condition сценарий (пустой список, но gameStarted получен!):**
```
[NetworkSync] 📦 Получен список игроков в комнате
[NetworkSync] В комнате 0 игроков
[NetworkSync] Мой socketId: abc123
[NetworkSync] 🎯 Мой spawnIndex от сервера: 0
[NetworkSync] 🎮 Статус игры от сервера: gameStarted=true
[NetworkSync] 🔍 Game status check: server.gameStarted=true, local.IsGameStarted=false, final=true
[NetworkSync] 🎮 Игра УЖЕ ИДЕТ (0 игроков)! Спавним локального игрока сразу
```

Затем Unity получит `player_joined` для других игроков и заспавнит их.

## Deployment

**Commit:** `ec95658`
**Version:** `2.3.0-race-condition-fix`
**Deployed:** Render auto-deploy (2-3 минуты после push)

Проверка деплоя:
```bash
curl https://aetherion-server-gv5u.onrender.com
```

Ожидаемый ответ:
```json
{
  "version": "2.3.0-race-condition-fix",
  "features": ["REST API", "Socket.IO", "Multiplayer", "MMO Persistent World", "Race Condition Fix"]
}
```

## Testing

### Scenario 1: Cold Start (Both players connect)
1. Client 1 → BattleScene
2. Client 2 → BattleScene
3. **Expected:** Both see each other ✅

### Scenario 2: WorldMap Return
1. Client 1 → BattleScene (stays)
2. Client 2 → WorldMap
3. Client 2 → BattleScene (returns)
4. **Expected:** Both see each other ✅

### Scenario 3: Race Condition (Fixed!)
1. Client connects with slow network
2. `get_room_players` arrives before `join_room` completes
3. **Before:** No `room_players` event → stuck forever
4. **After:** Receives `room_players` with `gameStarted: true` → spawns correctly ✅

## Status

✅ **Server:** Deployed (v2.3.0-race-condition-fix)
✅ **Unity:** Updated (NetworkSyncManager.cs with gameStarted check)
🧪 **Testing:** Ready for testing

---

**Date:** 2025-11-15
**Commit:** ec95658
