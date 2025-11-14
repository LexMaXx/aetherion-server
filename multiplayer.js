/**
 * Multiplayer Logic - Socket.IO Event Handlers
 * Обрабатывает все real-time события мультиплеера
 */

const Room = require('./models/Room');

// Хранилище активных игроков
const activePlayers = new Map(); // socketId => { roomId, username, characterClass, position, animation }

// Хранилище врагов в комнатах
const roomEnemies = new Map(); // roomId => Map(enemyId => { health, alive, position })

// LOBBY SYSTEM: Хранилище лобби комнат
const roomLobbies = new Map(); // roomId => { waitTime, startTime, countdownTimer, gameStarted }

module.exports = (io) => {
  console.log('🎮 Multiplayer module loaded');

  // ═══════════════════════════════════════════════════════════════════
  // ГЛОБАЛЬНАЯ MMO КОМНАТА (PERSISTENT WORLD)
  // ═══════════════════════════════════════════════════════════════════
  const GLOBAL_ROOM_ID = 'aetherion-global-world';
  const GLOBAL_ROOM_MAX_PLAYERS = 500;
  const USE_GLOBAL_ROOM = true; // MMO режим: все в одной комнате

  console.log('🌍 ═══════════════════════════════════════════');
  console.log('🌍 ГЛОБАЛЬНАЯ MMO КОМНАТА СОЗДАНА');
  console.log(`🌍 Room ID: ${GLOBAL_ROOM_ID}`);
  console.log(`🌍 Max Players: ${GLOBAL_ROOM_MAX_PLAYERS}`);
  console.log('🌍 Type: Persistent World (никогда не закрывается)');
  console.log('🌍 Все игроки автоматически подключаются к этой комнате');
  console.log('🌍 ═══════════════════════════════════════════');

  io.on('connection', (socket) => {
    console.log(`✅ Player connected: ${socket.id}`);

    // DEBUG: Лог ТОЛЬКО Party событий (чтобы не спамить логи)
    socket.onAny((eventName, ...args) => {
      if (eventName.startsWith('party_')) {
        console.log(`[🔍 PARTY EVENT] ${eventName} from ${socket.id}`);
      }
    });

    // ═══════════════════════════════════════════
    // ПОДКЛЮЧЕНИЕ К КОМНАТЕ
    // ═══════════════════════════════════════════

    socket.on('join_room', async (data) => {
      try {
        // ВАЖНО: Unity может отправить как строку, так и как объект
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
            console.log('[Join Room] ✅ Parsed JSON string to object');
          } catch (e) {
            console.error('[Join Room] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        let { roomId, username, characterClass, userId } = parsedData;

        // ═══════════════════════════════════════════════════════════════════
        // MMO MODE: Все игроки подключаются к ОДНОЙ глобальной комнате
        // ═══════════════════════════════════════════════════════════════════
        if (USE_GLOBAL_ROOM) {
          roomId = GLOBAL_ROOM_ID; // Принудительно используем глобальную комнату
          console.log(`[Join Room - MMO] 🌍 ${username} (${socket.id}) подключается к глобальной MMO комнате`);
        } else {
          console.log(`[Join Room] ${username} (${socket.id}) joining room ${roomId} as ${characterClass}`);
        }

        // Присоединяемся к Socket.IO room
        socket.join(roomId);

        // ВАЖНО: Обновляем или создаём комнату в MongoDB
        try {
          let room = await Room.findOne({ roomId });

          if (!room) {
            // Комната не существует - создаём новую
            const roomData = {
              roomId,
              roomName: USE_GLOBAL_ROOM ? 'Aetherion Global World' : `${username}'s Room`,
              maxPlayers: USE_GLOBAL_ROOM ? GLOBAL_ROOM_MAX_PLAYERS : 20,
              isPrivate: false,
              status: USE_GLOBAL_ROOM ? 'in_progress' : 'waiting', // Глобальная комната всегда "в игре"
              players: []
            };

            // Добавляем hostUserId только если userId валидный
            if (userId && userId.trim() !== '') {
              roomData.hostUserId = userId;
            }

            room = new Room(roomData);

            if (USE_GLOBAL_ROOM) {
              console.log(`[Join Room - MMO] 🌍 Создана глобальная MMO комната (лимит: ${GLOBAL_ROOM_MAX_PLAYERS} игроков)`);
            }
          }

          // Проверяем лимит игроков
          if (room.players.length >= room.maxPlayers) {
            console.log(`[Join Room] ❌ Комната ${roomId} полная (${room.players.length}/${room.maxPlayers})`);
            socket.emit('room_full', { message: 'Комната полная, попробуйте позже' });
            return;
          }

          // Проверяем есть ли игрок уже в комнате
          const existingPlayer = room.players.find(p => p.socketId === socket.id);

          if (!existingPlayer) {
            // Добавляем игрока в комнату
            const playerData = {
              characterClass,
              username,
              socketId: socket.id,
              position: { x: 0, y: 0, z: 0 },
              health: { current: 100, max: 100 },
              mana: { current: 100, max: 100 },
              isAlive: true
            };

            // Добавляем userId только если он валидный (не пустая строка)
            if (userId && userId.trim() !== '') {
              playerData.userId = userId;
            }

            room.players.push(playerData);

            // Для глобальной комнаты не меняем статус
            // Статус всегда "in_progress" для MMO режима

            await room.save();
            console.log(`[Join Room] ✅ Room ${roomId} updated in MongoDB. Players: ${room.players.length}/${room.maxPlayers}`);
          }
        } catch (dbError) {
          console.error('[Join Room] ❌ MongoDB error:', dbError.message);
          // Продолжаем даже если MongoDB не работает
        }

        // Сохраняем информацию об игроке в памяти
        // ВАЖНО: HP будет установлено через update_player_stats от клиента!
        // Используем 0 как placeholder вместо null (Unity не поддерживает null для float)

        // Проверяем если игра уже началась - назначаем spawnIndex сразу
        let assignedSpawnIndex = undefined;
        const lobby = roomLobbies.get(roomId);
        if (lobby && lobby.gameStarted) {
          // Находим следующий свободный spawnIndex
          const usedIndices = new Set();
          for (const [sid, player] of activePlayers.entries()) {
            if (player.roomId === roomId && player.spawnIndex !== undefined) {
              usedIndices.add(player.spawnIndex);
            }
          }
          // Назначаем минимальный свободный индекс
          for (let i = 0; i < 100; i++) {
            if (!usedIndices.has(i)) {
              assignedSpawnIndex = i;
              break;
            }
          }
          console.log(`[Join Room] 🎯 Assigned spawnIndex ${assignedSpawnIndex} to ${username} (game already started)`);
        }

        activePlayers.set(socket.id, {
          roomId,
          username,
          characterClass,
          userId,
          position: { x: 0, y: 0, z: 0 },
          rotation: { x: 0, y: 0, z: 0 },
          animation: 'Idle',
          health: 0,         // ← Placeholder, будет обновлено через update_player_stats
          maxHealth: 0,      // ← Placeholder, будет обновлено через update_player_stats
          currentHealth: 0,  // ← Placeholder для совместимости с Server/server.js
          connected: true,
          joinedAt: Date.now(),
          level: 1,  // Добавляем level для party system
          spawnIndex: assignedSpawnIndex  // Присваиваем spawnIndex если игра уже идёт
        });

        console.log(`[Join Room] ✅ Player ${username} added to activePlayers with socketId: ${socket.id}`);
        console.log(`[Join Room] 📊 Total active players: ${activePlayers.size}`);

        // Получаем всех игроков в комнате
        const playersInRoom = [];
        for (const [sid, player] of activePlayers.entries()) {
          if (player.roomId === roomId) {
            playersInRoom.push({
              socketId: sid,
              username: player.username,
              characterClass: player.characterClass,
              spawnIndex: player.spawnIndex !== undefined ? player.spawnIndex : 0, // КРИТИЧНО!
              position: player.position,
              rotation: player.rotation,
              animation: player.animation,
              health: player.health,
              maxHealth: player.maxHealth
            });
          }
        }

        // Отправляем текущему игроку список всех игроков
        console.log(`[Join Room] 📤 Sending room_players to ${username}: ${playersInRoom.length} players`);

        // КРИТИЧНО: Проверяем статус игры
        const lobby = roomLobbies.get(roomId);
        const gameStarted = lobby ? lobby.gameStarted : false;

        console.log(`[Join Room] 🎮 Game started status: ${gameStarted}`);
        console.log(`[Join Room] 🎯 Your spawnIndex: ${assignedSpawnIndex !== undefined ? assignedSpawnIndex : 'not assigned yet (will be set on game_start)'}`);

        socket.emit('room_players', {
          players: playersInRoom,
          yourSocketId: socket.id,
          yourSpawnIndex: assignedSpawnIndex !== undefined ? assignedSpawnIndex : 0, // КРИТИЧНО для Unity!
          gameStarted: gameStarted  // КРИТИЧНО: Флаг для Unity!
        });

        // Уведомляем других игроков о новом игроке
        console.log(`[Join Room] 📢 Broadcasting player_joined for ${username} to room ${roomId}`);
        socket.to(roomId).emit('player_joined', {
          socketId: socket.id,
          username,
          characterClass,
          position: { x: 0, y: 0, z: 0 },
          rotation: { x: 0, y: 0, z: 0 }
        });

        console.log(`✅ ${username} joined room ${roomId}. Total players: ${playersInRoom.length}`);

        // ═══════════════════════════════════════════
        // LOBBY SYSTEM: Запускаем таймер если >= 2 игроков
        // ═══════════════════════════════════════════
        console.log(`[Lobby] 🔍 Checking if lobby should start. Players in room: ${playersInRoom.length}`);

        // ═══════════════════════════════════════════
        // MMO MODE: Для глобальной комнаты игра ВСЕГДА идёт!
        // ═══════════════════════════════════════════
        if (USE_GLOBAL_ROOM && roomId === GLOBAL_ROOM_ID) {
          let lobby = roomLobbies.get(roomId);

          if (!lobby) {
            // Создаём лобби для глобальной комнаты с gameStarted = true
            console.log(`[Lobby - MMO] 🌍 Creating persistent lobby for global room (game always running)`);
            lobby = {
              waitTime: 0,
              currentTime: 0,
              startTime: Date.now(),
              countdownStarted: false,
              gameStarted: true, // ← КРИТИЧНО: Игра ВСЕГДА идёт в MMO режиме!
              timer: null
            };
            roomLobbies.set(roomId, lobby);
          }

          // Для каждого игрока подключающегося к ongoing MMO игре - отправляем game_start
          if (lobby.gameStarted) {
            console.log(`[Lobby - MMO] 🎮 Player ${username} joined ONGOING MMO game - sending game_start`);

            // Получаем всех игроков с их spawnIndex
            const currentPlayers = [];
            for (const [sid, player] of activePlayers.entries()) {
              if (player.roomId === roomId) {
                currentPlayers.push({
                  socketId: sid,
                  username: player.username,
                  characterClass: player.characterClass,
                  spawnIndex: player.spawnIndex !== undefined ? player.spawnIndex : 0,
                  position: player.position,
                  rotation: player.rotation,
                  health: player.health,
                  maxHealth: player.maxHealth
                });
              }
            }

            // Отправляем game_start этому игроку
            const gameStartData = {
              roomId,
              players: currentPlayers,
              timestamp: Date.now(),
              alreadyStarted: true
            };

            console.log(`[Lobby - MMO] 📤 Sending game_start to ${username}. Players in payload: ${currentPlayers.length}`);
            console.log(`[Lobby - MMO] 📋 Players: ${currentPlayers.map(p => p.username).join(', ')}`);

            // КРИТИЧНО: Отправляем JSON СТРОКУ, не объект!
            const jsonString = JSON.stringify(gameStartData);
            console.log(`[Lobby - MMO] 📝 JSON length: ${jsonString.length} chars`);

            socket.emit('game_start', jsonString);

            console.log(`[Lobby - MMO] ✅ Sent game_start to ${username} (${currentPlayers.length} players in MMO world)`);
          }
        } else if (playersInRoom.length >= 2) {
          // ARENA MODE: Обычная логика с таймером лобби
          let lobby = roomLobbies.get(roomId);
          console.log(`[Lobby] 🎲 Checking lobby state for room ${roomId}. Players: ${playersInRoom.length}. Lobby exists: ${!!lobby}. Game started: ${lobby?.gameStarted}`);

          // Если лобби еще нет - создаём и запускаем таймер
          if (!lobby || lobby.gameStarted) {
            console.log(`[Lobby] 🎮 Starting lobby for room ${roomId} (${playersInRoom.length} players)`);

            lobby = {
              waitTime: 20, // 20 секунд ожидания
              currentTime: 20,
              startTime: Date.now(),
              countdownStarted: false,
              gameStarted: false,
              timer: null
            };

            roomLobbies.set(roomId, lobby);

            // Отправляем lobby_created ВСЕМ игрокам в комнате
            io.to(roomId).emit('lobby_created', {
              roomId,
              waitTime: lobby.waitTime,
              playerCount: playersInRoom.length,
              maxPlayers: 20,
              timestamp: Date.now()
            });

            // Таймер обратного отсчёта (каждую секунду)
            lobby.timer = setInterval(() => {
              lobby.currentTime--;

              // Отправляем обновление всем игрокам
              io.to(roomId).emit('lobby_timer_update', {
                roomId,
                timeRemaining: lobby.currentTime,
                timestamp: Date.now()
              });

              console.log(`[Lobby] Room ${roomId}: ${lobby.currentTime} seconds remaining`);

              // Когда осталось 3 секунды или меньше - отправляем countdown
              if (lobby.currentTime > 0 && lobby.currentTime <= 3) {
                io.to(roomId).emit('game_countdown', {
                  roomId,
                  count: lobby.currentTime,
                  timestamp: Date.now()
                });
                console.log(`[Lobby] ⏱️ Countdown: ${lobby.currentTime}`);
              }

              // Когда таймер закончился - начинаем игру
              if (lobby.currentTime <= 0) {
                clearInterval(lobby.timer);
                lobby.gameStarted = true;

                console.log(`[Lobby] ✅ Game starting for room ${roomId}`);

                // Получаем финальный список игроков и НАЗНАЧАЕМ spawn indices
                const finalPlayers = [];
                let spawnIndex = 0;
                for (const [sid, player] of activePlayers.entries()) {
                  if (player.roomId === roomId) {
                    // КРИТИЧЕСКОЕ: Назначаем spawn index каждому игроку
                    player.spawnIndex = spawnIndex++;

                    finalPlayers.push({
                      socketId: sid,
                      username: player.username,
                      characterClass: player.characterClass,
                      spawnIndex: player.spawnIndex, // КРИТИЧЕСКОЕ: Отправляем spawnIndex для синхронизации позиций!
                      position: player.position, // Оставляем для совместимости (будет 0,0,0)
                      rotation: player.rotation,
                      health: player.health,
                      maxHealth: player.maxHealth
                    });
                  }
                }

                // Отправляем game_start ВСЕМ игрокам
                io.to(roomId).emit('game_start', {
                  roomId,
                  players: finalPlayers,
                  timestamp: Date.now()
                });

                console.log(`[Lobby] 🚀 Game started! Players: ${finalPlayers.length}`);
              }
            }, 1000);
          } else {
            // Лобби уже существует
            if (lobby.gameStarted) {
              // ═══════════════════════════════════════════════════════════════════
              // КРИТИЧЕСКОЕ: Игра уже началась - отправляем game_start немедленно!
              // ═══════════════════════════════════════════════════════════════════
              console.log(`[Lobby] 🎮 Player ${username} joined ONGOING game - sending game_start immediately`);

              // Получаем всех игроков с их spawnIndex
              const currentPlayers = [];
              for (const [sid, player] of activePlayers.entries()) {
                if (player.roomId === roomId) {
                  currentPlayers.push({
                    socketId: sid,
                    username: player.username,
                    characterClass: player.characterClass,
                    spawnIndex: player.spawnIndex !== undefined ? player.spawnIndex : 0,
                    position: player.position,
                    rotation: player.rotation,
                    health: player.health,
                    maxHealth: player.maxHealth
                  });
                }
              }

              // Отправляем game_start только этому игроку
              socket.emit('game_start', {
                roomId,
                players: currentPlayers,
                timestamp: Date.now(),
                alreadyStarted: true // Флаг что игра уже идёт
              });

              console.log(`[Lobby] ✅ Sent game_start to ${username} (${currentPlayers.length} players in game)`);
            } else {
              // Лобби ещё в ожидании - отправляем текущее состояние таймера
              console.log(`[Lobby] Player ${username} joined existing lobby. Time remaining: ${lobby.currentTime}s`);

              socket.emit('lobby_created', {
                roomId,
                waitTime: lobby.currentTime, // Отправляем оставшеее время
                playerCount: playersInRoom.length,
                maxPlayers: 20,
                timestamp: Date.now()
              });
            }
          }
        }

      } catch (error) {
        console.error('[Join Room] Error:', error);
        socket.emit('error', { message: 'Failed to join room' });
      }
    });

    // ═══════════════════════════════════════════
    // ЗАПРОС СПИСКА ИГРОКОВ (для повторной синхронизации)
    // ═══════════════════════════════════════════

    socket.on('get_room_players', (data) => {
      try {
        // ВАЖНО: Unity может отправить как строку, так и как объект
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
          } catch (e) {
            console.error('[Get Room Players] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        const { roomId } = parsedData;
        const player = activePlayers.get(socket.id);

        if (!player) {
          console.warn(`[Get Room Players] ⚠️ Player ${socket.id} not found in activePlayers - might be race condition`);
          console.log(`[Get Room Players] 🔄 Sending empty player list with gameStarted flag anyway`);

          // КРИТИЧНО: Не выходим! Отправляем хотя бы статус игры
          const lobby = roomLobbies.get(roomId);
          const gameStarted = lobby ? lobby.gameStarted : false;

          socket.emit('room_players', {
            players: [],
            yourSocketId: socket.id,
            yourSpawnIndex: 0,
            gameStarted: gameStarted
          });
          return;
        }

        console.log(`[Get Room Players] ${player.username} requesting players for room ${roomId}`);

        // Получаем всех игроков в комнате
        const playersInRoom = [];
        for (const [sid, p] of activePlayers.entries()) {
          if (p.roomId === roomId) {
            playersInRoom.push({
              socketId: sid,
              username: p.username,
              characterClass: p.characterClass,
              spawnIndex: p.spawnIndex !== undefined ? p.spawnIndex : 0, // КРИТИЧНО!
              position: p.position,
              rotation: p.rotation,
              animation: p.animation,
              health: p.health,
              maxHealth: p.maxHealth
            });
          }
        }

        // КРИТИЧНО: Проверяем статус игры
        const lobby = roomLobbies.get(roomId);
        const gameStarted = lobby ? lobby.gameStarted : false;

        // Отправляем список игроков
        socket.emit('room_players', {
          players: playersInRoom,
          yourSocketId: socket.id,
          yourSpawnIndex: player.spawnIndex !== undefined ? player.spawnIndex : 0, // КРИТИЧНО для Unity!
          gameStarted: gameStarted  // КРИТИЧНО: Флаг для Unity!
        });

        console.log(`✅ Sent ${playersInRoom.length} players to ${player.username} (gameStarted: ${gameStarted})`);

        // КРИТИЧЕСКОЕ: Уведомляем ДРУГИХ игроков что этот игрок "вернулся"
        // Это нужно для случая когда игрок выходит на WorldMap и возвращается в BattleScene
        // Другие игроки должны заспавнить его снова
        socket.to(roomId).emit('player_joined', {
          socketId: socket.id,
          username: player.username,
          characterClass: player.characterClass,
          spawnIndex: player.spawnIndex !== undefined ? player.spawnIndex : 0,
          position: player.position,
          rotation: player.rotation,
          animation: player.animation,
          health: player.health,
          maxHealth: player.maxHealth
        });

        console.log(`📢 Broadcast player_joined for ${player.username} (returning to BattleScene)`);

      } catch (error) {
        console.error('[Get Room Players] Error:', error);
      }
    });

    // ═══════════════════════════════════════════
    // ОБНОВЛЕНИЕ ПОЗИЦИИ И ДВИЖЕНИЯ
    // ═══════════════════════════════════════════

    socket.on('player_update', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) return;

      // ВАЖНО: Unity может отправить как строку, так и как объект
      let parsedData = data;
      if (typeof data === 'string') {
        try {
          parsedData = JSON.parse(data);
        } catch (e) {
          console.error('[Player Update] ❌ Failed to parse JSON:', e.message);
          return;
        }
      }

      // Обновляем данные игрока
      if (parsedData.position) player.position = parsedData.position;
      if (parsedData.rotation) player.rotation = parsedData.rotation;
      if (parsedData.velocity) player.velocity = parsedData.velocity;
      if (parsedData.isGrounded !== undefined) player.isGrounded = parsedData.isGrounded;

      // Отправляем обновление другим игрокам в комнате
      socket.to(player.roomId).emit('player_moved', {
        socketId: socket.id,
        position: player.position,
        rotation: player.rotation,
        velocity: parsedData.velocity || { x: 0, y: 0, z: 0 },
        isGrounded: parsedData.isGrounded !== undefined ? parsedData.isGrounded : true,
        timestamp: parsedData.timestamp || Date.now()
      });
    });

    // ═══════════════════════════════════════════
    // АНИМАЦИИ
    // ═══════════════════════════════════════════

    // ИСПРАВЛЕНО: Слушаем оба события (update_animation и player_animation)
    // и рассылаем как player_animation_changed (как ожидает клиент!)
    socket.on('update_animation', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) {
        console.warn(`[Animation] ⚠️ Player not found: ${socket.id}`);
        return;
      }

      // ВАЖНО: Unity может отправить как строку, так и как объект
      let parsedData = data;
      if (typeof data === 'string') {
        try {
          parsedData = JSON.parse(data);
        } catch (e) {
          console.error('[Animation] ❌ Failed to parse JSON:', e.message);
          return;
        }
      }

      player.animation = parsedData.animation || parsedData.animationState || 'Idle';
      player.animationSpeed = parsedData.speed || 1.0;

      // ИСПРАВЛЕНО: Рассылаем как player_animation_changed (как ожидает клиент)
      io.to(player.roomId).emit('player_animation_changed', {
        socketId: socket.id,
        animation: player.animation,
        speed: player.animationSpeed,
        timestamp: Date.now()
      });

      console.log(`[Animation] 🎬 ${player.username} -> ${player.animation} (разослано в room ${player.roomId})`);
    });

    // Обратная совместимость: старый event name
    socket.on('player_animation', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) {
        console.warn(`[Animation] ⚠️ Player not found: ${socket.id}`);
        return;
      }

      // ВАЖНО: Unity может отправить как строку, так и как объект
      let parsedData = data;
      if (typeof data === 'string') {
        try {
          parsedData = JSON.parse(data);
        } catch (e) {
          console.error('[Animation] ❌ Failed to parse JSON:', e.message);
          return;
        }
      }

      player.animation = parsedData.animation || parsedData.animationState || 'Idle';
      player.animationSpeed = parsedData.speed || 1.0;

      // ИСПРАВЛЕНО: Рассылаем как player_animation_changed (как ожидает клиент)
      io.to(player.roomId).emit('player_animation_changed', {
        socketId: socket.id,
        animation: player.animation,
        speed: player.animationSpeed,
        timestamp: Date.now()
      });

      console.log(`[Animation] 🎬 ${player.username} -> ${player.animation} (старый event, разослано в room ${player.roomId})`);
    });

    // ═══════════════════════════════════════════
    // АТАКА
    // ═══════════════════════════════════════════

    socket.on('player_attack', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) {
        console.warn(`[Attack] ⚠️ Player not found: ${socket.id}`);
        return;
      }

      // ВАЖНО: Unity может отправить как строку, так и как объект
      let parsedData = data;
      if (typeof data === 'string') {
        try {
          parsedData = JSON.parse(data);
        } catch (e) {
          console.error('[Attack] ❌ Failed to parse JSON:', e.message);
          return;
        }
      }

      console.log(`[Attack] ⚔️ ${player.username} attacking ${parsedData.targetType} (ID: ${parsedData.targetId}), type: ${parsedData.attackType}`);

      // Отправляем всем игрокам в комнате (включая атакующего для визуальных эффектов)
      io.to(player.roomId).emit('player_attacked', {
        socketId: socket.id,
        attackType: parsedData.attackType || 'melee',
        targetType: parsedData.targetType, // 'player' or 'enemy'
        targetId: parsedData.targetId,
        damage: parsedData.damage || 0,
        baseDamage: parsedData.baseDamage || 0,
        strength: parsedData.strength || 0,
        intelligence: parsedData.intelligence || 0,
        luck: parsedData.luck || 0,
        position: parsedData.position,
        direction: parsedData.direction,
        targetPosition: parsedData.targetPosition,
        skillId: parsedData.skillId,
        timestamp: Date.now()
      });

      console.log(`[Attack] ✅ player_attacked разослан в room ${player.roomId}`);
    });

    // ═══════════════════════════════════════════
    // СКИЛЛЫ (ABILITIES)
    // ═══════════════════════════════════════════

    socket.on('player_skill', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) {
        console.warn(`[Skill] ⚠️ Player not found: ${socket.id}`);
        return;
      }

      // ВАЖНО: Unity может отправить как строку, так и как объект
      let parsedData = data;
      if (typeof data === 'string') {
        try {
          parsedData = JSON.parse(data);
        } catch (e) {
          console.error('[Skill] ❌ Failed to parse JSON:', e.message);
          return;
        }
      }

      console.log(`[Skill] ⚡ ${player.username} used skill ${parsedData.skillId}, type: ${parsedData.skillType || 'unknown'}`);

      // Рассылаем всем игрокам в комнате (включая кастера для локальных эффектов)
      io.to(player.roomId).emit('player_used_skill', {
        socketId: socket.id,
        skillId: parsedData.skillId,
        targetSocketId: parsedData.targetSocketId || null,
        targetPosition: parsedData.targetPosition || { x: 0, y: 0, z: 0 },
        skillType: parsedData.skillType || '',
        animationTrigger: parsedData.animationTrigger || '',
        animationSpeed: parsedData.animationSpeed || 1.0,
        castTime: parsedData.castTime || 0,
        timestamp: Date.now()
      });

      console.log(`[Skill] ✅ player_used_skill разослан в room ${player.roomId}`);
    });

    // ═══════════════════════════════════════════
    // PROJECTILE SPAWNED (для снарядов скиллов)
    // ═══════════════════════════════════════════

    socket.on('projectile_spawned', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) {
        console.warn(`[Projectile] ⚠️ Player not found: ${socket.id}`);
        return;
      }

      let parsedData = data;
      if (typeof data === 'string') {
        try {
          parsedData = JSON.parse(data);
        } catch (e) {
          console.error('[Projectile] ❌ Failed to parse JSON:', e.message);
          return;
        }
      }

      console.log(`[Projectile] 🚀 ${player.username} spawned projectile for skill ${parsedData.skillId}`);

      // Рассылаем всем в комнате КРОМЕ отправителя
      socket.to(player.roomId).emit('projectile_spawned', {
        socketId: socket.id,
        skillId: parsedData.skillId,
        spawnPosition: parsedData.spawnPosition,
        direction: parsedData.direction,
        targetSocketId: parsedData.targetSocketId || '',
        timestamp: Date.now()
      });
    });

    // ═══════════════════════════════════════════
    // VISUAL EFFECT SPAWNED (для визуальных эффектов)
    // ═══════════════════════════════════════════

    socket.on('visual_effect_spawned', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) {
        console.warn(`[VisualEffect] ⚠️ Player not found: ${socket.id}`);
        return;
      }

      let parsedData = data;
      if (typeof data === 'string') {
        try {
          parsedData = JSON.parse(data);
        } catch (e) {
          console.error('[VisualEffect] ❌ Failed to parse JSON:', e.message);
          return;
        }
      }

      console.log(`[VisualEffect] ✨ ${player.username} spawned effect: ${parsedData.effectType} - ${parsedData.effectPrefabName}`);

      // Рассылаем всем в комнате КРОМЕ отправителя
      socket.to(player.roomId).emit('visual_effect_spawned', {
        socketId: socket.id,
        effectType: parsedData.effectType,
        effectPrefabName: parsedData.effectPrefabName,
        position: parsedData.position,
        rotation: parsedData.rotation,
        targetSocketId: parsedData.targetSocketId || '',
        duration: parsedData.duration || 0,
        timestamp: Date.now()
      });
    });

    // ═══════════════════════════════════════════
    // EFFECT APPLIED (баффы/дебаффы/DoT)
    // ═══════════════════════════════════════════

    socket.on('effect_applied', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) {
        console.warn(`[Effect] ⚠️ Player not found: ${socket.id}`);
        return;
      }

      let parsedData = data;
      if (typeof data === 'string') {
        try {
          parsedData = JSON.parse(data);
        } catch (e) {
          console.error('[Effect] ❌ Failed to parse JSON:', e.message);
          return;
        }
      }

      console.log(`[Effect] 💊 ${player.username} applied effect: ${parsedData.effectType} to ${parsedData.targetSocketId || 'self'}`);

      // Рассылаем всем в комнате КРОМЕ отправителя
      socket.to(player.roomId).emit('effect_applied', {
        casterSocketId: socket.id,
        targetSocketId: parsedData.targetSocketId || socket.id, // Если пусто - на себя
        effectType: parsedData.effectType,
        duration: parsedData.duration || 0,
        power: parsedData.power || 0,
        tickInterval: parsedData.tickInterval || 0,
        particleEffectPrefabName: parsedData.particleEffectPrefabName || '',
        timestamp: Date.now()
      });
    });

    // ═══════════════════════════════════════════
    // MINION SUMMONED (призыв миньонов)
    // ═══════════════════════════════════════════
    socket.on('minion_summoned', (data) => {
      console.log(`[Minion] 🔥🔥🔥 EVENT RECEIVED from ${socket.id}`);
      console.log(`[Minion] 🔍 activePlayers.size: ${activePlayers.size}`);
      console.log(`[Minion] 🔍 activePlayers keys:`, Array.from(activePlayers.keys()));

      const player = activePlayers.get(socket.id);
      if (!player) {
        console.warn(`[Minion] ⚠️ Player not found: ${socket.id}`);
        console.warn(`[Minion] ⚠️ activePlayers содержит:`, Array.from(activePlayers.entries()).map(([k, v]) => `${k}:${v.username}`));
        return;
      }

      console.log(`[Minion] ✅ Player found: ${player.username}, roomId: ${player.roomId}`);

      let parsedData = data;
      if (typeof data === 'string') {
        try {
          parsedData = JSON.parse(data);
          console.log('[Minion] ✅ Parsed JSON string to object');
        } catch (e) {
          console.error('[Minion] ❌ Failed to parse JSON:', e.message);
          return;
        }
      }

      console.log(`[Minion] 💀 ${player.username} summoned ${parsedData.minionType} at (${parsedData.positionX}, ${parsedData.positionY}, ${parsedData.positionZ})`);
      console.log(`[Minion] 📊 Duration: ${parsedData.duration}s, Damage: ${parsedData.damage}, Owner: ${parsedData.ownerSocketId}`);

      // Рассылаем всем в комнате КРОМЕ отправителя
      const broadcastData = {
        ownerSocketId: socket.id,
        minionType: parsedData.minionType || 'skeleton',
        positionX: parsedData.positionX || 0,
        positionY: parsedData.positionY || 0,
        positionZ: parsedData.positionZ || 0,
        rotationY: parsedData.rotationY || 0,
        duration: parsedData.duration || 20,
        damage: parsedData.damage || 30,
        intelligenceScaling: parsedData.intelligenceScaling || 0.5,
        timestamp: Date.now()
      };

      console.log(`[Minion] 📤 Broadcasting to room ${player.roomId}:`, broadcastData);
      socket.to(player.roomId).emit('minion_summoned', broadcastData);
      console.log(`[Minion] ✅ Broadcasted minion summon to room ${player.roomId}`);
    });

    // ═══════════════════════════════════════════
    // PLAYER TRANSFORMED (трансформация Paladin Bear Form и т.д.)
    // ═══════════════════════════════════════════
    socket.on('player_transformed', (data) => {
      console.log(`[Transform] 🔥🔥🔥 EVENT RECEIVED from ${socket.id}`);
      console.log(`[Transform] 📥 RAW data type: ${typeof data}`);
      console.log(`[Transform] 📥 RAW data:`, data);

      const player = activePlayers.get(socket.id);
      if (!player) {
        console.warn(`[Transform] ⚠️ Player not found: ${socket.id}`);
        console.warn(`[Transform] ⚠️ activePlayers:`, Array.from(activePlayers.keys()));
        return;
      }

      console.log(`[Transform] ✅ Player found: ${player.username}, roomId: ${player.roomId}`);

      let parsedData = data;
      if (typeof data === 'string') {
        try {
          parsedData = JSON.parse(data);
          console.log('[Transform] ✅ Parsed JSON string to object');
        } catch (e) {
          console.error('[Transform] ❌ Failed to parse JSON:', e.message);
          return;
        }
      }

      console.log(`[Transform] 🐻 ${player.username} transformed using skillId=${parsedData.skillId}`);

      // Рассылаем всем в комнате КРОМЕ отправителя
      const broadcastData = {
        socketId: socket.id,
        skillId: parsedData.skillId,
        timestamp: Date.now()
      };

      console.log(`[Transform] 📤 Broadcasting to room ${player.roomId}:`, broadcastData);
      socket.to(player.roomId).emit('player_transformed', broadcastData);
      console.log(`[Transform] ✅ Broadcast complete!`);
    });

    // ═══════════════════════════════════════════
    // PLAYER TRANSFORMATION ENDED (окончание трансформации)
    // ═══════════════════════════════════════════
    socket.on('player_transformation_ended', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) {
        console.warn(`[Transform] ⚠️ Player not found: ${socket.id}`);
        return;
      }

      console.log(`[Transform] 🔄 ${player.username} transformation ended`);

      // Рассылаем всем в комнате КРОМЕ отправителя
      socket.to(player.roomId).emit('player_transformation_ended', {
        socketId: socket.id,
        timestamp: Date.now()
      });

      console.log(`[Transform] 📤 Broadcasted transformation end to room ${player.roomId}`);
    });

    // ═══════════════════════════════════════════
    // ПОЛУЧЕНИЕ УРОНА - УДАЛЕНО! (Дублирование)
    // ═══════════════════════════════════════════
    // КРИТИЧЕСКОЕ: Этот обработчик УДАЛЕН!
    // Урон теперь обрабатывается ТОЛЬКО в Server/server.js через событие player_damage
    // Это устраняет дублирование и рассинхронизацию HP

    // Старый код (закомментирован для истории):
    /*
    socket.on('player_damaged', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) return;

      player.health = Math.max(0, data.currentHealth);

      console.log(`[Damage] ${player.username} took ${data.damage} damage. Health: ${player.health}/${player.maxHealth}`);

      // Уведомляем всех игроков
      io.to(player.roomId).emit('player_damaged', {
        targetSocketId: socket.id,
        attackerSocketId: data.attackerId,
        attackerName: data.attackerName || 'Unknown',
        damage: data.damage,
        currentHealth: player.health,
        maxHealth: player.maxHealth,
        timestamp: Date.now()
      });

      // Если игрок умер
      if (player.health <= 0) {
        player.animation = 'Dead';
        io.to(player.roomId).emit('player_died', {
          socketId: socket.id,
          killerId: data.attackerId,
          timestamp: Date.now()
        });
      }
    });
    */

    // ═══════════════════════════════════════════
    // ОБНОВЛЕНИЕ HP И STATS
    // ═══════════════════════════════════════════

    socket.on('update_player_stats', (data) => {
      try {
        // Unity может отправить как строку, так и объект
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
            console.log('[Stats] ✅ Parsed JSON string to object');
          } catch (e) {
            console.error('[Stats] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.error(`[Stats] ❌ Player ${socket.id} not found in activePlayers`);
          return;
        }

        // Обновляем HP
        if (parsedData.maxHealth !== undefined && parsedData.maxHealth > 0) {
          player.maxHealth = parsedData.maxHealth;
          console.log(`[Stats] 💚 ${player.username} maxHealth updated: ${player.maxHealth}`);

          // ВАЖНО: Если health еще не инициализировано (0 = placeholder) - устанавливаем = maxHealth
          if (player.health === 0 || player.health === null || player.health === undefined) {
            player.health = player.maxHealth;
            player.currentHealth = player.maxHealth;
            console.log(`[Stats] ✨ ${player.username} health инициализировано: ${player.health}`);
          }
        }

        if (parsedData.currentHealth !== undefined) {
          player.health = parsedData.currentHealth;
          player.currentHealth = parsedData.currentHealth; // Для совместимости с Server/server.js
          console.log(`[Stats] 💙 ${player.username} currentHealth updated: ${player.health}`);
        }

        // Обновляем stats (если есть)
        if (parsedData.stats) {
          player.stats = {
            ...player.stats,
            ...parsedData.stats
          };
          console.log(`[Stats] 📊 ${player.username} stats updated:`, player.stats);
        }

        console.log(`[Stats] ✅ ${player.username} HP: ${player.health}/${player.maxHealth}`);

      } catch (error) {
        console.error('[Stats] ❌ Error updating player stats:', error.message);
      }
    });

    // ═══════════════════════════════════════════
    // ОБРАБОТКА ЛЕЧЕНИЯ
    // ═══════════════════════════════════════════
    socket.on('player_healed', (data) => {
      try {
        console.log('[Heal] 📥 ========== ПОЛУЧЕНО player_healed ==========');
        console.log('[Heal] 📦 Raw data type:', typeof data);
        console.log('[Heal] 📦 Raw data:', data);

        // Unity может отправить как строку, так и объект
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
            console.log('[Heal] ✅ Parsed JSON string to object');
          } catch (e) {
            console.error('[Heal] ❌ Failed to parse JSON:', e.message);
            console.error('[Heal] 📦 Raw data:', data);
            return;
          }
        }

        const { targetSocketId, healAmount, currentHealth, maxHealth, healerSocketId } = parsedData;
        console.log(`[Heal] 💚 Лечение: ${healerSocketId || socket.id} → Target: ${targetSocketId}, Heal: ${healAmount}`);
        console.log(`[Heal] 💚 HP после лечения: ${currentHealth}/${maxHealth}`);

        // Проверка входных данных
        if (!targetSocketId || healAmount === undefined) {
          console.error('[Heal] ❌ Недостаточно данных для лечения');
          return;
        }

        // Получаем целителя и цель из activePlayers
        const healer = activePlayers.get(healerSocketId || socket.id);
        const target = activePlayers.get(targetSocketId);

        if (!healer) {
          console.error(`[Heal] ❌ Целитель не найден: ${healerSocketId || socket.id}`);
          return;
        }

        if (!target) {
          console.error(`[Heal] ❌ Цель не найдена: ${targetSocketId}`);
          return;
        }

        // Проверяем что оба игрока в одной комнате
        if (healer.roomId !== target.roomId) {
          console.error(`[Heal] ❌ Целитель и цель в разных комнатах! Healer: ${healer.roomId}, Target: ${target.roomId}`);
          return;
        }

        const roomId = target.roomId;
        console.log(`[Heal] 🏠 Комната: ${roomId}`);

        // Обновляем HP цели на сервере
        target.health = currentHealth;
        target.currentHealth = currentHealth;
        target.maxHealth = maxHealth;

        console.log(`[Heal] ✅ HP цели обновлено: ${target.username} → ${target.health}/${target.maxHealth}`);

        // Рассылаем событие лечения всем игрокам в комнате
        io.to(roomId).emit('player_healed', {
          targetSocketId: targetSocketId,
          healerSocketId: healerSocketId || socket.id,
          healerName: healer.username,
          healAmount: healAmount,
          currentHealth: currentHealth,
          maxHealth: maxHealth,
          timestamp: Date.now()
        });

        console.log(`[Heal] 📤 Broadcasted healing to room ${roomId}`);

      } catch (error) {
        console.error('[Heal] ❌ Error processing healing:', error.message);
        console.error('[Heal] Stack:', error.stack);
      }
    });

    // ═══════════════════════════════════════════
    // PVP: ОБРАБОТКА УРОНА МЕЖДУ ИГРОКАМИ
    // ═══════════════════════════════════════════
    socket.on('player_damage', (data) => {
      try {
        console.log('[PvP] 📥 ========== ПОЛУЧЕНО player_damage ==========');
        console.log('[PvP] 📦 Raw data type:', typeof data);
        console.log('[PvP] 📦 Raw data:', data);

        // Unity может отправить как строку, так и объект
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
            console.log('[PvP] ✅ Parsed JSON string to object');
          } catch (e) {
            console.error('[PvP] ❌ Failed to parse JSON:', e.message);
            console.error('[PvP] 📦 Raw data:', data);
            return;
          }
        }

        const { targetSocketId, damage, attackerId } = parsedData;
        console.log(`[PvP] 💥 Урон: ${socket.id} → Target: ${targetSocketId}, Damage: ${damage}`);
        console.log(`[PvP] 👊 Attacker ID: ${attackerId}`);

        // Проверка входных данных
        if (!targetSocketId || damage === undefined) {
          console.error('[PvP] ❌ Недостаточно данных для урона');
          return;
        }

        // Получаем атакующего и цель из activePlayers
        const attacker = activePlayers.get(socket.id);
        const target = activePlayers.get(targetSocketId);

        if (!attacker) {
          console.error(`[PvP] ❌ Атакующий не найден: ${socket.id}`);
          return;
        }

        if (!target) {
          console.error(`[PvP] ❌ Цель не найдена: ${targetSocketId}`);
          return;
        }

        // Проверяем что оба игрока в одной комнате
        if (attacker.roomId !== target.roomId) {
          console.error(`[PvP] ❌ Игроки в разных комнатах! Атакующий: ${attacker.roomId}, Цель: ${target.roomId}`);
          return;
        }

        console.log(`[PvP] ✅ ${attacker.username} атакует ${target.username} на ${damage} урона`);
        console.log(`[PvP] 🔍 Target HP ДО обработки: currentHealth=${target.currentHealth}, maxHealth=${target.maxHealth}, health=${target.health}`);

        // Применяем урон к цели (обновляем HP на сервере)
        if (!target.currentHealth && target.currentHealth !== 0) {
          console.log(`[PvP] 🔧 currentHealth пустое, инициализируем...`);
          // ВАЖНО: Проверяем что maxHealth уже установлено через update_player_stats
          // Если maxHealth === 0 - значит update_player_stats ещё не пришло!
          if (target.maxHealth === 0 || !target.maxHealth) {
            console.error(`[PvP] ❌ ${target.username} maxHealth не инициализировано (${target.maxHealth})! update_player_stats не пришло.`);
            console.error(`[PvP] ⚠️ Пропускаем урон - ждём инициализации HP`);
            return;
          }
          target.currentHealth = target.maxHealth;
          target.health = target.maxHealth; // Для совместимости
          console.log(`[PvP] ✅ Инициализировано: currentHealth=${target.currentHealth}`);
        }

        target.currentHealth -= damage;
        target.currentHealth = Math.max(0, target.currentHealth); // Не может быть меньше 0
        target.health = target.currentHealth; // Для совместимости

        console.log(`[PvP] 💚 ${target.username} HP: ${target.currentHealth}/${target.maxHealth}`);

        // Отправляем событие player_damaged ВСЕМ игрокам в комнате
        // Это обновит HP у всех клиентов синхронно
        io.to(attacker.roomId).emit('player_damaged', {
          targetSocketId: targetSocketId,
          attackerSocketId: socket.id,
          attackerName: attacker.username,
          damage: damage,
          currentHealth: target.currentHealth,
          maxHealth: target.maxHealth,
          timestamp: Date.now()
        });

        console.log(`[PvP] 📡 Отправлено player_damaged в комнату ${attacker.roomId}`);

        // Проверяем смерть
        if (target.currentHealth <= 0) {
          console.log(`[PvP] 💀 ${target.username} погиб от руки ${attacker.username}!`);

          // Отправляем событие смерти
          io.to(attacker.roomId).emit('player_died', {
            socketId: targetSocketId,
            killerId: socket.id,
            timestamp: Date.now(),
            respawnTime: 10000 // 10 секунд респавн
          });
        }

      } catch (error) {
        console.error('[PvP] ❌ Error processing player_damage:', error.message);
        console.error(error.stack);
      }
    });

    // ═══════════════════════════════════════════
    // СМЕРТЬ И РЕСПАВН
    // ═══════════════════════════════════════════

    // Обработка смерти игрока
    socket.on('player_died', (data) => {
      try {
        const player = activePlayers.get(socket.id);
        if (!player) {
          console.error(`[Death] ❌ Player ${socket.id} not found`);
          return;
        }

        // Помечаем игрока как мертвого
        player.isDead = true;
        player.health = 0;

        // Оповещаем всех игроков в комнате о смерти
        io.to(player.roomId).emit('player_died', {
          socketId: socket.id,
          killerId: data.killerId || null,
          respawnTime: 10000  // 10 секунд
        });

        console.log(`[Death] 💀 ${player.username} killed by ${data.killerId || 'unknown'}. Respawn in 10s`);

      } catch (error) {
        console.error('[Death] ❌ Error:', error.message);
      }
    });

    // Обработка запроса на респавн
    socket.on('request_respawn', () => {
      try {
        const player = activePlayers.get(socket.id);
        if (!player) {
          console.error(`[Respawn] ❌ Player ${socket.id} not found`);
          return;
        }

        if (!player.isDead) {
          console.warn(`[Respawn] ⚠️ ${player.username} is not dead, ignoring respawn request`);
          return;
        }

        // Выбираем случайную точку спавна (0-19)
        const spawnIndex = Math.floor(Math.random() * 20);

        // Восстанавливаем HP и статус
        player.health = player.maxHealth;
        player.isDead = false;

        // Оповещаем всех игроков в комнате о респавне
        io.to(player.roomId).emit('player_respawned', {
          socketId: socket.id,
          spawnIndex: spawnIndex,
          health: player.health,
          maxHealth: player.maxHealth,
          timestamp: Date.now()
        });

        console.log(`[Respawn] ✅ ${player.username} respawned at spawn ${spawnIndex}. HP: ${player.health}/${player.maxHealth}`);

      } catch (error) {
        console.error('[Respawn] ❌ Error:', error.message);
      }
    });

    // Legacy обработчик player_respawn (для обратной совместимости)
    socket.on('player_respawn', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) return;

      player.health = player.maxHealth;
      player.position = data.position;
      player.animation = 'Idle';

      console.log(`[Respawn] ${player.username} respawned at (${data.position.x}, ${data.position.y}, ${data.position.z})`);

      // Уведомляем всех
      io.to(player.roomId).emit('player_respawned', {
        socketId: socket.id,
        position: data.position,
        health: player.health,
        timestamp: Date.now()
      });
    });

    // ═══════════════════════════════════════════
    // ВРАГИ (NPC)
    // ═══════════════════════════════════════════

    socket.on('enemy_damaged', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) return;

      const { roomId, enemyId, damage, currentHealth } = data;

      // Сохраняем состояние врага
      if (!roomEnemies.has(roomId)) {
        roomEnemies.set(roomId, new Map());
      }
      const enemies = roomEnemies.get(roomId);
      enemies.set(enemyId, {
        health: currentHealth,
        alive: currentHealth > 0
      });

      console.log(`[Enemy Damage] ${enemyId} took ${damage} damage. Health: ${currentHealth}`);

      // Уведомляем всех игроков в комнате
      io.to(roomId).emit('enemy_health_changed', {
        enemyId,
        damage,
        currentHealth,
        attackerId: socket.id,
        timestamp: Date.now()
      });
    });

    socket.on('enemy_killed', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) return;

      const { roomId, enemyId, position } = data;

      // Помечаем врага как мёртвого
      if (roomEnemies.has(roomId)) {
        const enemies = roomEnemies.get(roomId);
        enemies.set(enemyId, {
          health: 0,
          alive: false
        });
      }

      console.log(`[Enemy Killed] ${enemyId} killed by ${player.username}`);

      // Уведомляем всех игроков
      io.to(roomId).emit('enemy_died', {
        enemyId,
        killerId: socket.id,
        killerUsername: player.username,
        position,
        timestamp: Date.now()
      });
    });

    socket.on('enemy_respawned', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) return;

      const { roomId, enemyId, enemyType, position, health } = data;

      // Обновляем состояние врага
      if (roomEnemies.has(roomId)) {
        const enemies = roomEnemies.get(roomId);
        enemies.set(enemyId, {
          health,
          alive: true,
          position
        });
      }

      console.log(`[Enemy Respawned] ${enemyId} (${enemyType}) at (${position.x}, ${position.y}, ${position.z})`);

      // Уведомляем всех игроков
      io.to(roomId).emit('enemy_respawned', {
        enemyId,
        enemyType,
        position,
        health,
        timestamp: Date.now()
      });
    });

    // ═══════════════════════════════════════════
    // ОТКЛЮЧЕНИЕ
    // ═══════════════════════════════════════════

    socket.on('disconnect', async () => {
      const player = activePlayers.get(socket.id);

      if (player) {
        console.log(`❌ Player disconnected: ${player.username} (${socket.id})`);

        // Удаляем игрока из MongoDB
        try {
          const room = await Room.findOne({ roomId: player.roomId });

          if (room) {
            // Удаляем игрока из массива
            room.players = room.players.filter(p => p.socketId !== socket.id);

            // Если комната пустая - удаляем её
            if (room.players.length === 0) {
              await Room.deleteOne({ roomId: player.roomId });
              console.log(`[Disconnect] ✅ Room ${player.roomId} deleted (empty)`);
            } else {
              // Если игроков < 2, возвращаем статус в waiting
              if (room.players.length < 2 && room.status === 'in_progress') {
                room.status = 'waiting';
              }
              await room.save();
              console.log(`[Disconnect] ✅ Player removed from room. Remaining: ${room.players.length}`);
            }
          }
        } catch (dbError) {
          console.error('[Disconnect] ❌ MongoDB error:', dbError.message);
        }

        // Уведомляем других игроков
        socket.to(player.roomId).emit('player_left', {
          socketId: socket.id,
          username: player.username
        });

        // LOBBY CLEANUP: Останавливаем таймер если игроков < 2
        const remainingPlayers = Array.from(activePlayers.values()).filter(p => p.roomId === player.roomId);
        if (remainingPlayers.length < 2) {
          const lobby = roomLobbies.get(player.roomId);
          if (lobby && lobby.timer) {
            clearInterval(lobby.timer);
            roomLobbies.delete(player.roomId);
            console.log(`[Lobby] ⏹️ Lobby cancelled for room ${player.roomId} (not enough players)`);

            // Уведомляем оставшихся игроков
            io.to(player.roomId).emit('lobby_cancelled', {
              roomId: player.roomId,
              reason: 'Not enough players',
              timestamp: Date.now()
            });
          }
        }

        // Удаляем игрока из памяти
        activePlayers.delete(socket.id);
      } else {
        console.log(`❌ Unknown player disconnected: ${socket.id}`);
      }
    });

    // ═══════════════════════════════════════════
    // СИСТЕМА ПРОКАЧКИ (REAL-TIME LEVELING)
    // ═══════════════════════════════════════════

    // Получение уровня игроком
    socket.on('player_level_up', (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          parsedData = JSON.parse(data);
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.error('[Level Up] ❌ Player not found:', socket.id);
          return;
        }

        const { newLevel, characterClass, availableStatPoints } = parsedData;

        console.log(`[Level Up] 🎉 ${player.username} достиг уровня ${newLevel}!`);

        // Обновляем уровень в памяти
        player.level = newLevel;
        player.availableStatPoints = availableStatPoints;

        // Broadcast всем игрокам в комнате (включая отправителя для подтверждения)
        io.to(player.roomId).emit('player_level_up', {
          socketId: socket.id,
          username: player.username,
          characterClass: player.characterClass,
          newLevel,
          availableStatPoints,
          timestamp: Date.now()
        });

      } catch (error) {
        console.error('[Level Up] ❌ Error:', error.message);
      }
    });

    // Повышение характеристики игроком
    socket.on('player_stat_upgraded', (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          parsedData = JSON.parse(data);
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.error('[Stat Upgrade] ❌ Player not found:', socket.id);
          return;
        }

        const { statName, newValue } = parsedData;

        console.log(`[Stat Upgrade] 📈 ${player.username} повысил ${statName} до ${newValue}`);

        // Обновляем статы в памяти
        if (!player.stats) {
          player.stats = {};
        }
        player.stats[statName] = newValue;

        // Broadcast всем игрокам в комнате
        io.to(player.roomId).emit('player_stat_upgraded', {
          socketId: socket.id,
          username: player.username,
          statName,
          newValue,
          timestamp: Date.now()
        });

      } catch (error) {
        console.error('[Stat Upgrade] ❌ Error:', error.message);
      }
    });

    // Полная синхронизация статов игрока
    socket.on('player_stats_sync', (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          parsedData = JSON.parse(data);
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.error('[Stats Sync] ❌ Player not found:', socket.id);
          return;
        }

        const { level, experience, availableStatPoints, characterClass, stats } = parsedData;

        console.log(`[Stats Sync] 📊 ${player.username} синхронизирует статы: Level ${level}, Points ${availableStatPoints}`);

        // Обновляем все данные в памяти
        player.level = level;
        player.experience = experience;
        player.availableStatPoints = availableStatPoints;
        player.stats = stats;

        // Broadcast всем игрокам в комнате (кроме отправителя)
        socket.to(player.roomId).emit('player_stats_sync', {
          socketId: socket.id,
          username: player.username,
          level,
          experience,
          availableStatPoints,
          characterClass: player.characterClass,
          stats,
          timestamp: Date.now()
        });

      } catch (error) {
        console.error('[Stats Sync] ❌ Error:', error.message);
      }
    });

    // Запрос синхронизации статов другого игрока
    socket.on('request_player_stats', (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          parsedData = JSON.parse(data);
        }

        const { targetSocketId } = parsedData;
        const targetPlayer = activePlayers.get(targetSocketId);

        if (!targetPlayer) {
          console.error('[Request Stats] ❌ Target player not found:', targetSocketId);
          return;
        }

        console.log(`[Request Stats] 📥 ${socket.id} запросил статы игрока ${targetPlayer.username}`);

        // Отправляем статы запрашивающему игроку
        socket.emit('player_stats_sync', {
          socketId: targetSocketId,
          username: targetPlayer.username,
          level: targetPlayer.level || 1,
          experience: targetPlayer.experience || 0,
          availableStatPoints: targetPlayer.availableStatPoints || 0,
          characterClass: targetPlayer.characterClass,
          stats: targetPlayer.stats || {},
          timestamp: Date.now()
        });

      } catch (error) {
        console.error('[Request Stats] ❌ Error:', error.message);
      }
    });

    // ═══════════════════════════════════════════
    // ИНВЕНТАРЬ И ЭКИПИРОВКА
    // ═══════════════════════════════════════════

    // Синхронизация инвентаря с MongoDB
    socket.on('inventory_sync', async (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          parsedData = JSON.parse(data);
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.error('[Inventory Sync] ❌ Player not found:', socket.id);
          return;
        }

        const { characterClass, inventoryData } = parsedData;

        // Parse inventoryData если это строка
        let inventoryObj;
        if (typeof inventoryData === 'string') {
          inventoryObj = JSON.parse(inventoryData);
        } else {
          inventoryObj = inventoryData;
        }

        console.log(`[Inventory Sync] 📦 ${player.username} синхронизирует инвентарь`);
        console.log(`[Inventory Sync] Предметов: ${inventoryObj.items ? inventoryObj.items.length : 0}`);
        console.log(`[Inventory Sync] Экипировка: weapon=${inventoryObj.equipment?.weapon || 'none'}, armor=${inventoryObj.equipment?.armor || 'none'}`);

        // Сохраняем в MongoDB
        const Character = require('./models/Character');
        await Character.updateOne(
          { userId: player.userId, characterClass: characterClass },
          {
            $set: {
              inventory: inventoryObj.items || [],
              equipment: inventoryObj.equipment || {}
            }
          }
        );

        console.log(`[Inventory Sync] ✅ Инвентарь ${player.username} сохранён в MongoDB`);

        // Отправляем подтверждение
        socket.emit('inventory_synced', {
          success: true,
          timestamp: Date.now()
        });

      } catch (error) {
        console.error('[Inventory Sync] ❌ Error:', error.message);
        socket.emit('inventory_synced', {
          success: false,
          error: error.message
        });
      }
    });

    // Загрузка инвентаря из MongoDB (при подключении/переподключении)
    socket.on('load_inventory', async (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          parsedData = JSON.parse(data);
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.error('[Load Inventory] ❌ Player not found:', socket.id);
          return;
        }

        const { characterClass } = parsedData;

        console.log(`[Load Inventory] 📥 ${player.username} запрашивает инвентарь для ${characterClass}`);

        // Загружаем из MongoDB
        const Character = require('./models/Character');
        const character = await Character.findOne({
          userId: player.userId,
          characterClass: characterClass
        });

        if (!character) {
          console.error(`[Load Inventory] ❌ Character not found: ${characterClass}`);
          socket.emit('inventory_loaded', {
            success: false,
            error: 'Character not found'
          });
          return;
        }

        // Формируем JSON для Unity
        const inventoryData = {
          items: character.inventory || [],
          equipment: character.equipment || {}
        };

        console.log(`[Load Inventory] ✅ Инвентарь загружен: ${inventoryData.items.length} предметов`);

        socket.emit('inventory_loaded', {
          success: true,
          inventoryJson: JSON.stringify(inventoryData),
          timestamp: Date.now()
        });

      } catch (error) {
        console.error('[Load Inventory] ❌ Error:', error.message);
        socket.emit('inventory_loaded', {
          success: false,
          error: error.message
        });
      }
    });

    // ═══════════════════════════════════════════
    // PARTY SYSTEM (ГРУППЫ)
    // ═══════════════════════════════════════════

    // DEBUG: Показать список активных игроков
    socket.on('debug_active_players', () => {
      console.log(`[Debug] 📊 Active players count: ${activePlayers.size}`);
      for (const [socketId, player] of activePlayers.entries()) {
        console.log(`  - ${player.username} (${socketId}) in room ${player.roomId}`);
      }
      socket.emit('debug_response', {
        count: activePlayers.size,
        players: Array.from(activePlayers.entries()).map(([sid, p]) => ({
          socketId: sid,
          username: p.username,
          roomId: p.roomId
        }))
      });
    });

    // Приглашение в группу
    console.log(`[Party System] 🔧 Регистрируем обработчик 'party_invite' для ${socket.id}`);
    socket.on('party_invite', async (data) => {
      try {
        console.log(`[Party Invite] 📥 RAW data received (type: ${typeof data}):`, data);

        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
            console.log(`[Party Invite] ✅ JSON parsed successfully:`, parsedData);
          } catch (e) {
            console.error('[Party Invite] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.warn(`[Party Invite] ⚠️ Player not found: ${socket.id}`);
          return;
        }

        console.log(`[Party Invite] 🔍 parsedData:`, parsedData);
        const { targetSocketId, partyId } = parsedData;
        console.log(`[Party Invite] 🔍 targetSocketId extracted:`, targetSocketId);
        console.log(`[Party Invite] 🔍 partyId extracted:`, partyId);

        const targetPlayer = activePlayers.get(targetSocketId);

        if (!targetPlayer) {
          console.warn(`[Party Invite] ⚠️ Target player not found: ${targetSocketId}`);
          console.warn(`[Party Invite] 📊 Active players: ${Array.from(activePlayers.keys()).join(', ')}`);
          console.warn(`[Party Invite] 📊 Total active players: ${activePlayers.size}`);
          socket.emit('party_error', JSON.stringify({ message: 'Игрок не найден' }));
          return;
        }

        console.log(`[Party Invite] 📨 ${player.username} (${socket.id}) приглашает ${targetPlayer.username} (${targetSocketId}) в группу ${partyId}`);

        // Отправляем приглашение целевому игроку
        const inviteData = {
          partyId: partyId,
          inviterSocketId: socket.id,
          inviterUsername: player.username,
          inviterClass: player.characterClass,
          inviterLevel: player.level || 1,
          timestamp: Date.now()
        };

        console.log(`[Party Invite] 📤 Отправляем party_invite_received на socketId: ${targetSocketId}`);
        console.log(`[Party Invite] 📦 Данные:`, JSON.stringify(inviteData));

        // ВАЖНО: Отправляем JSON строку, не объект! Unity ожидает строку.
        io.to(targetSocketId).emit('party_invite_received', JSON.stringify(inviteData));

        console.log(`[Party Invite] ✅ Событие party_invite_received отправлено`);

        // Подтверждаем отправку инвайтеру
        socket.emit('party_invite_sent', JSON.stringify({
          targetUsername: targetPlayer.username,
          timestamp: Date.now()
        }));

        console.log(`[Party Invite] ✅ Подтверждение отправлено инвайтеру`);

      } catch (error) {
        console.error('[Party Invite] ❌ Error:', error.message);
        socket.emit('party_error', JSON.stringify({ message: 'Ошибка отправки приглашения' }));
      }
    });

    // Принятие приглашения в группу
    socket.on('party_accept', async (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
          } catch (e) {
            console.error('[Party Accept] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.warn(`[Party Accept] ⚠️ Player not found: ${socket.id}`);
          return;
        }

        const { partyId, inviterSocketId } = parsedData;

        console.log(`[Party Accept] ✅ ${player.username} принял приглашение в группу ${partyId}`);

        // Уведомляем инвайтера о принятии
        io.to(inviterSocketId).emit('party_member_joined', JSON.stringify({
          partyId: partyId,
          memberSocketId: socket.id,
          memberUsername: player.username,
          memberClass: player.characterClass,
          memberLevel: player.level || 1,
          timestamp: Date.now()
        }));

        // Получаем информацию о лидере (инвайтере) для отправки новому члену
        const inviter = activePlayers.get(inviterSocketId);
        if (!inviter) {
          console.log('[Party Accept] ⚠️ Инвайтер не найден в activePlayers');
        }

        // Подтверждаем вступление самому игроку и отправляем информацию о лидере
        socket.emit('party_joined', JSON.stringify({
          partyId: partyId,
          leaderSocketId: inviterSocketId,
          leaderUsername: inviter ? inviter.username : 'Unknown',
          leaderClass: inviter ? inviter.characterClass : 'Warrior',
          leaderLevel: inviter ? (inviter.level || 1) : 1,
          timestamp: Date.now()
        }));

      } catch (error) {
        console.error('[Party Accept] ❌ Error:', error.message);
        socket.emit('party_error', JSON.stringify({ message: 'Ошибка принятия приглашения' }));
      }
    });

    // Отклонение приглашения в группу
    socket.on('party_decline', async (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
          } catch (e) {
            console.error('[Party Decline] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.warn(`[Party Decline] ⚠️ Player not found: ${socket.id}`);
          return;
        }

        const { partyId, inviterSocketId } = parsedData;

        console.log(`[Party Decline] ❌ ${player.username} отклонил приглашение в группу ${partyId}`);

        // Уведомляем инвайтера об отклонении
        io.to(inviterSocketId).emit('party_invite_declined', JSON.stringify({
          partyId: partyId,
          declinedUsername: player.username,
          timestamp: Date.now()
        }));

      } catch (error) {
        console.error('[Party Decline] ❌ Error:', error.message);
      }
    });

    // Выход из группы
    socket.on('party_leave', async (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
          } catch (e) {
            console.error('[Party Leave] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.warn(`[Party Leave] ⚠️ Player not found: ${socket.id}`);
          return;
        }

        const { partyId, memberSocketIds } = parsedData;

        console.log(`[Party Leave] 👋 ${player.username} покинул группу ${partyId}`);

        // Уведомляем всех членов группы о выходе
        if (memberSocketIds && Array.isArray(memberSocketIds)) {
          memberSocketIds.forEach(memberId => {
            if (memberId !== socket.id) {
              io.to(memberId).emit('party_member_left', JSON.stringify({
                partyId: partyId,
                leftSocketId: socket.id,
                leftUsername: player.username,
                timestamp: Date.now()
              }));
            }
          });
        }

        // Подтверждаем выход самому игроку
        socket.emit('party_left', JSON.stringify({
          partyId: partyId,
          timestamp: Date.now()
        }));

      } catch (error) {
        console.error('[Party Leave] ❌ Error:', error.message);
      }
    });

    // Синхронизация HP/MP членов группы
    socket.on('party_stats_update', async (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
          } catch (e) {
            console.error('[Party Stats] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.warn(`[Party Stats] ⚠️ Player not found: ${socket.id}`);
          return;
        }

        const { partyId, memberSocketIds, health, mana, maxHealth, maxMana } = parsedData;

        console.log(`[Party Stats] 📊 ${player.username} обновляет статы (HP: ${health}/${maxHealth}, MP: ${mana}/${maxMana})`);

        // Рассылаем обновление статов всем членам группы
        if (memberSocketIds && Array.isArray(memberSocketIds)) {
          memberSocketIds.forEach(memberId => {
            if (memberId !== socket.id) {
              io.to(memberId).emit('party_member_stats_updated', JSON.stringify({
                partyId: partyId,
                memberSocketId: socket.id,
                memberUsername: player.username,
                health: health,
                mana: mana,
                maxHealth: maxHealth,
                maxMana: maxMana,
                timestamp: Date.now()
              }));
            }
          });
        }

      } catch (error) {
        console.error('[Party Stats] ❌ Error:', error.message);
      }
    });

    // Запрос информации о группе
    socket.on('party_sync_request', async (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
          } catch (e) {
            console.error('[Party Sync] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.warn(`[Party Sync] ⚠️ Player not found: ${socket.id}`);
          return;
        }

        const { partyId, memberSocketIds } = parsedData;

        console.log(`[Party Sync] 🔄 ${player.username} запрашивает синхронизацию группы ${partyId}`);

        // Собираем информацию о всех членах группы
        const members = [];
        if (memberSocketIds && Array.isArray(memberSocketIds)) {
          memberSocketIds.forEach(memberId => {
            const memberPlayer = activePlayers.get(memberId);
            if (memberPlayer) {
              members.push({
                socketId: memberId,
                username: memberPlayer.username,
                characterClass: memberPlayer.characterClass,
                level: memberPlayer.level || 1,
                health: memberPlayer.health || 100,
                mana: memberPlayer.mana || 100,
                maxHealth: memberPlayer.maxHealth || 100,
                maxMana: memberPlayer.maxMana || 100
              });
            }
          });
        }

        // Отправляем данные запрашивающему игроку
        socket.emit('party_synced', JSON.stringify({
          partyId: partyId,
          members: members,
          timestamp: Date.now()
        }));

      } catch (error) {
        console.error('[Party Sync] ❌ Error:', error.message);
      }
    });


    // ═══════════════════════════════════════════
    // CHAT SYSTEM (ОБЩИЙ ЧАТ И КОМАНДНЫЙ ЧАТ)
    // ═══════════════════════════════════════════

    socket.on('chat_message', (data) => {
      try {
        console.log('[Chat] 📥 ========== ПОЛУЧЕНО chat_message ==========');
        console.log('[Chat] 📦 Raw data type:', typeof data);
        console.log('[Chat] 📦 Raw data:', data);

        // Unity может отправить как строку, так и объект
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
            console.log('[Chat] ✅ Parsed JSON string to object');
          } catch (e) {
            console.error('[Chat] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.error(`[Chat] ❌ Player ${socket.id} not found in activePlayers`);
          return;
        }

        const { message, channel, username } = parsedData;

        if (!message || !channel) {
          console.error('[Chat] ❌ Недостаточно данных для отправки сообщения');
          return;
        }

        console.log(`[Chat] 💬 ${username} [${channel}]: ${message}`);

        // Формируем сообщение для отправки
        const chatMessage = {
          username: username || player.username,
          message: message,
          channel: channel, // 'all' или 'party'
          socketId: socket.id,
          timestamp: Date.now()
        };

        // Определяем кому отправлять
        if (channel === 'all') {
          // Отправляем всем игрокам в комнате
          if (player.roomId) {
            io.to(player.roomId).emit('chat_message', chatMessage); // Убран JSON.stringify - Socket.IO сделает это автоматически
            console.log(`[Chat] ✅ Сообщение отправлено в комнату ${player.roomId} (All Chat)`);
          } else {
            console.warn('[Chat] ⚠️ Игрок не в комнате, сообщение не отправлено');
          }
        } else if (channel === 'party') {
          // Отправляем только членам группы
          if (player.partyId) {
            // Находим всех членов группы в этой комнате
            const partyMembers = [];
            for (const [memberId, memberPlayer] of activePlayers.entries()) {
              if (memberPlayer.partyId === player.partyId && memberPlayer.roomId === player.roomId) {
                partyMembers.push(memberId);
              }
            }

            // Отправляем каждому члену группы
            partyMembers.forEach(memberId => {
              const memberSocket = io.sockets.sockets.get(memberId);
              if (memberSocket) {
                memberSocket.emit('chat_message', chatMessage); // Убран JSON.stringify - Socket.IO сделает это автоматически
              }
            });

            console.log(`[Chat] ✅ Сообщение отправлено группе ${player.partyId} (${partyMembers.length} игроков)`);
          } else {
            console.warn('[Chat] ⚠️ Игрок не в группе, сообщение не отправлено');
            // Отправляем ошибку обратно игроку
            socket.emit('chat_error', {
              error: 'not_in_party',
              message: 'Вы не состоите в группе'
            }); // Убран JSON.stringify - Socket.IO сделает это автоматически
          }
        } else {
          console.error(`[Chat] ❌ Неизвестный канал: ${channel}`);
        }

      } catch (error) {
        console.error('[Chat] ❌ Error processing chat message:', error.message);
      }
    });

    // ═══════════════════════════════════════════
    // ПИНГ (ДЛЯ ПРОВЕРКИ СОЕДИНЕНИЯ)
    // ═══════════════════════════════════════════

    socket.on('ping', () => {
      socket.emit('pong', { timestamp: Date.now() });
    });

    // ═══════════════════════════════════════════
    // WORLD MAP СИНХРОНИЗАЦИЯ
    // ═══════════════════════════════════════════

    // Игрок зашел на WorldMap
    socket.on('world_map_join', (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          parsedData = JSON.parse(data);
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.warn(`[WorldMap] ⚠️ Player not found: ${socket.id}`);
          return;
        }

        // Обновляем информацию что игрок на WorldMap
        player.isOnWorldMap = true;
        player.worldMapPosition = parsedData.position;

        console.log(`[WorldMap] ✅ ${player.username} зашел на WorldMap at position (${parsedData.position.x}, ${parsedData.position.y}, ${parsedData.position.z})`);

        // Отправляем список других игроков на WorldMap
        const worldMapPlayers = [];
        for (const [socketId, otherPlayer] of activePlayers.entries()) {
          if (socketId !== socket.id && otherPlayer.isOnWorldMap && otherPlayer.worldMapPosition) {
            worldMapPlayers.push({
              socketId: socketId,
              username: otherPlayer.username,
              characterClass: otherPlayer.characterClass,
              position: otherPlayer.worldMapPosition,
              rotation: otherPlayer.worldMapRotation || { x: 0, y: 0, z: 0 }
            });
          }
        }

        socket.emit('world_map_players_list', {
          players: worldMapPlayers
        });

        console.log(`[WorldMap] 📋 Отправлен список игроков на WorldMap: ${worldMapPlayers.length} игроков`);

        // Уведомляем других игроков на WorldMap о новом игроке
        socket.broadcast.emit('world_map_player_joined', {
          socketId: socket.id,
          username: player.username,
          characterClass: player.characterClass,
          position: parsedData.position,
          rotation: { x: 0, y: 0, z: 0 }
        });

      } catch (error) {
        console.error('[WorldMap Join] ❌ Error:', error.message);
      }
    });

    // Обновление позиции на WorldMap
    socket.on('world_map_position_update', (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          parsedData = JSON.parse(data);
        }

        const player = activePlayers.get(socket.id);
        if (!player || !player.isOnWorldMap) {
          return;
        }

        // Обновляем позицию
        player.worldMapPosition = parsedData.position;
        player.worldMapRotation = parsedData.rotation;

        // Рассылаем другим игрокам на WorldMap
        socket.broadcast.emit('world_map_player_moved', {
          socketId: socket.id,
          position: parsedData.position,
          rotation: parsedData.rotation
        });

      } catch (error) {
        console.error('[WorldMap Position] ❌ Error:', error.message);
      }
    });

    // Игрок покинул WorldMap
    socket.on('world_map_leave', () => {
      try {
        const player = activePlayers.get(socket.id);
        if (player && player.isOnWorldMap) {
          player.isOnWorldMap = false;
          player.worldMapPosition = null;
          player.worldMapRotation = null;

          console.log(`[WorldMap] 🚪 ${player.username} покинул WorldMap`);

          // Уведомляем других игроков
          socket.broadcast.emit('world_map_player_left', {
            socketId: socket.id
          });
        }
      } catch (error) {
        console.error('[WorldMap Leave] ❌ Error:', error.message);
      }
    });
  });

  // Периодическая очистка отключённых игроков (каждые 5 минут)
  setInterval(() => {
    const now = Date.now();
    const timeout = 5 * 60 * 1000; // 5 minutes

    for (const [socketId, player] of activePlayers.entries()) {
      if (now - player.joinedAt > timeout && !player.connected) {
        console.log(`🧹 Cleaning up inactive player: ${player.username}`);
        activePlayers.delete(socketId);
      }
    }
  }, 5 * 60 * 1000);
};
