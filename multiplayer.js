/**
 * Multiplayer Logic - Socket.IO Event Handlers
 * Обрабатывает все real-time события мультиплеера
 */

const Room = require('./models/Room');

// Хранилище активных игроков
const activePlayers = new Map(); // socketId => { roomId, username, characterClass, position, animation }

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
        // ЗАГРУЗКА СТАТОВ ИЗ MONGODB
        // ═══════════════════════════════════════════════════════════════════
        let characterStats = null;
        let characterLevel = 1;
        let maxHealth = 100;
        let maxMana = 100;

        if (userId && characterClass) {
          try {
            const Character = require('./models/Character');
            const character = await Character.findOne({ userId, characterClass });

            if (character && character.stats) {
              characterStats = character.stats;
              characterLevel = character.level || 1;

              // Рассчитываем maxHealth и maxMana на основе статов
              // HP = 200 + (Endurance * 40) + equipment bonuses
              // MP = 50 + (Wisdom * 15) + equipment bonuses
              const baseHealth = 200 + (characterStats.endurance * 40);
              const baseMana = 50 + (characterStats.wisdom * 15);

              maxHealth = baseHealth;
              maxMana = baseMana;

              console.log(`[Join Room] 📊 Loaded stats for ${characterClass}: END=${characterStats.endurance} WIS=${characterStats.wisdom} → HP=${maxHealth} MP=${maxMana}`);
            } else {
              console.log(`[Join Room] ⚠️ Character not found in DB, using default stats`);
              characterStats = { strength: 1, perception: 1, endurance: 1, wisdom: 1, intelligence: 1, agility: 1, luck: 1 };
            }
          } catch (err) {
            console.error(`[Join Room] ❌ Error loading character stats:`, err.message);
            characterStats = { strength: 1, perception: 1, endurance: 1, wisdom: 1, intelligence: 1, agility: 1, luck: 1 };
          }
        } else {
          console.log(`[Join Room] ⚠️ No userId or characterClass, using default stats`);
          characterStats = { strength: 1, perception: 1, endurance: 1, wisdom: 1, intelligence: 1, agility: 1, luck: 1 };
        }

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
          health: maxHealth,         // Используем реальное значение из статов
          maxHealth: maxHealth,      // Используем реальное значение из статов
          currentHealth: maxHealth,  // Текущее HP = максимальному при входе
          mana: maxMana,             // Добавляем ману
          maxMana: maxMana,          // Максимальная мана
          currentMana: maxMana,      // Текущая мана = максимальной при входе
          stats: characterStats,     // Сохраняем статы персонажа
          connected: true,
          joinedAt: Date.now(),
          level: characterLevel,     // Используем реальный уровень
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
              maxHealth: player.maxHealth,
              mana: player.mana || 0,
              maxMana: player.maxMana || 0,
              stats: player.stats || null,
              level: player.level || 1
            });
          }
        }

        // Отправляем текущему игроку список всех игроков
        console.log(`[Join Room] 📤 Sending room_players to ${username}: ${playersInRoom.length} players`);

        // КРИТИЧНО: Проверяем статус игры (lobby уже объявлен на line 154)
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
          spawnIndex: assignedSpawnIndex !== undefined ? assignedSpawnIndex : 0, // КРИТИЧНО для MMO режима!
          position: { x: 0, y: 0, z: 0 }, // Пока неизвестна, будет обновлена через player_update
          rotation: { x: 0, y: 0, z: 0 },
          stats: characterStats,      // Отправляем реальные статы
          level: characterLevel,       // Отправляем уровень
          health: maxHealth,           // Отправляем текущее HP
          maxHealth: maxHealth,        // Отправляем максимальное HP
          mana: maxMana,               // Отправляем текущую MP
          maxMana: maxMana             // Отправляем максимальную MP
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
      console.log(`[Get Room Players] 🔥🔥🔥 EVENT RECEIVED from ${socket.id}`);
      console.log(`[Get Room Players] 📦 Raw data type: ${typeof data}`);
      console.log(`[Get Room Players] 📦 Raw data: ${JSON.stringify(data)}`);

      try {
        // ВАЖНО: Unity может отправить как строку, так и как объект
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
            console.log(`[Get Room Players] ✅ Parsed JSON: ${JSON.stringify(parsedData)}`);
          } catch (e) {
            console.error('[Get Room Players] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        const { roomId } = parsedData;
        console.log(`[Get Room Players] 🎯 Extracted roomId: ${roomId}`);
        console.log(`[Get Room Players] 🔍 Checking activePlayers for ${socket.id}...`);

        // КРИТИЧНО: Получаем lobby СРАЗУ для использования в обоих ветках (if и else)
        const lobby = roomLobbies.get(roomId);
        const gameStarted = lobby ? lobby.gameStarted : false;

        const player = activePlayers.get(socket.id);
        console.log(`[Get Room Players] 👤 Player found: ${player ? `YES (${player.username})` : 'NO'}`);

        if (!player) {
          console.warn(`[Get Room Players] ⚠️ Player ${socket.id} not found in activePlayers - might be race condition`);
          console.log(`[Get Room Players] 🔄 Sending empty player list with gameStarted flag anyway`);

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

        // Отправляем список игроков (gameStarted уже вычислен на line 477)
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

      // НОВОЕ: Если атакуем врага (NPC), рассчитываем урон на сервере и отправляем обратно
      if (parsedData.targetType === 'enemy') {
        // Рассчитываем урон на основе SPECIAL статов игрока
        const strength = parsedData.strength || 1;
        const intelligence = parsedData.intelligence || 1;
        const luck = parsedData.luck || 1;
        const baseDamage = parsedData.baseDamage || 10;

        // Формула урона (можно настроить)
        // Физический урон = baseDamage + (strength * 2)
        // Магический урон = baseDamage + (intelligence * 2)
        // Берем максимум из двух
        const physicalDamage = baseDamage + (strength * 2);
        const magicalDamage = baseDamage + (intelligence * 2);
        let finalDamage = Math.max(physicalDamage, magicalDamage);

        // Шанс крита = luck * 2% (максимум 50%)
        const critChance = Math.min(luck * 2, 50);
        const isCritical = Math.random() * 100 < critChance;

        if (isCritical) {
          finalDamage *= 2; // Крит удваивает урон
        }

        finalDamage = Math.round(finalDamage);

        console.log(`[Attack] 🎯 Серверный расчет урона: ${finalDamage} (STR=${strength}, INT=${intelligence}, LUCK=${luck}, Base=${baseDamage}, Crit=${isCritical})`);

        // Отправляем рассчитанный урон ВСЕМ клиентам в комнате
        io.to(player.roomId).emit('enemy_damaged_by_server', {
          enemyId: parsedData.targetId,
          damage: finalDamage,
          isCritical: isCritical,
          attackerSocketId: socket.id,
          attackerName: player.username,
          timestamp: Date.now()
        });

        console.log(`[Attack] ✅ enemy_damaged_by_server отправлен: враг ${parsedData.targetId}, урон ${finalDamage}${isCritical ? ' (КРИТ!)' : ''}`);
      }
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

        // КРИТИЧЕСКОЕ: Вычисляем новый HP на сервере (server-authoritative!)
        // Получаем ТЕКУЩИЙ HP цели на сервере (может отличаться от клиента из-за урона)
        const serverCurrentHP = target.health || target.currentHealth || 0;
        const serverMaxHP = target.maxHealth || maxHealth;

        console.log(`[Heal] 📊 Server HP BEFORE heal: ${serverCurrentHP}/${serverMaxHP}`);

        // Вычисляем новый HP = текущий на сервере + лечение
        const newHP = Math.min(serverCurrentHP + healAmount, serverMaxHP);

        // Обновляем HP цели на сервере
        target.health = newHP;
        target.currentHealth = newHP;
        target.maxHealth = serverMaxHP;

        console.log(`[Heal] ✅ ${target.username} HP AFTER heal: ${newHP}/${serverMaxHP} (+${healAmount})`);

        // Рассылаем событие лечения всем игрокам в комнате
        io.to(roomId).emit('player_healed', {
          targetSocketId: targetSocketId,
          healerSocketId: healerSocketId || socket.id,
          healerName: healer.username,
          healAmount: healAmount,
          currentHealth: newHP,  // ← ВАЖНО: Отправляем вычисленное значение от сервера!
          maxHealth: serverMaxHP,
          timestamp: Date.now()
        });

        console.log(`[Heal] 📤 Broadcasted healing to room ${roomId}`);

      } catch (error) {
        console.error('[Heal] ❌ Error processing healing:', error.message);
        console.error('[Heal] Stack:', error.stack);
      }
    });

    // ═══════════════════════════════════════════
    // ОБРАБОТКА ВОССТАНОВЛЕНИЯ МАНЫ
    // ═══════════════════════════════════════════
    socket.on('player_mana_restored', (data) => {
      try {
        console.log('[Mana] 📥 ========== ПОЛУЧЕНО player_mana_restored ==========');

        // Парсим данные (может быть строкой или объектом)
        let parsedData = data;
        if (typeof data === 'string') {
          parsedData = JSON.parse(data);
        }

        const { targetSocketId, manaAmount, currentMana, maxMana, restorerSocketId } = parsedData;

        console.log(`[Mana] 💙 Восстановление маны: ${manaAmount}`);
        console.log(`[Mana] 🎯 Target: ${targetSocketId}`);
        console.log(`[Mana] 📊 Client reported: ${currentMana}/${maxMana} (IGNORED - will use server values)`);

        // Получаем цель и восстанавливающего из activePlayers
        const target = activePlayers.get(targetSocketId);
        const restorer = activePlayers.get(restorerSocketId || socket.id);

        if (!target) {
          console.error(`[Mana] ❌ Target not found: ${targetSocketId}`);
          return;
        }

        if (!restorer) {
          console.error(`[Mana] ❌ Restorer not found: ${restorerSocketId || socket.id}`);
          return;
        }

        const roomId = target.roomId;
        console.log(`[Mana] 🏠 Комната: ${roomId}`);

        // КРИТИЧЕСКОЕ: Вычисляем новую ману на сервере (server-authoritative!)
        // Получаем ТЕКУЩУЮ ману цели на сервере (может отличаться от клиента из-за использования)
        const serverCurrentMana = target.mana || target.currentMana || 0;
        const serverMaxMana = target.maxMana || maxMana;

        console.log(`[Mana] 📊 Server Mana BEFORE restore: ${serverCurrentMana}/${serverMaxMana}`);

        // Вычисляем новую ману = текущая на сервере + восстановление
        const newMana = Math.min(serverCurrentMana + manaAmount, serverMaxMana);

        // Обновляем ману цели на сервере
        target.mana = newMana;
        target.currentMana = newMana;
        target.maxMana = serverMaxMana;

        console.log(`[Mana] ✅ ${target.username} Mana AFTER restore: ${newMana}/${serverMaxMana} (+${manaAmount})`);

        // Рассылаем событие восстановления маны всем игрокам в комнате
        io.to(roomId).emit('player_mana_restored', {
          targetSocketId: targetSocketId,
          restorerSocketId: restorerSocketId || socket.id,
          restorerName: restorer.username,
          manaAmount: manaAmount,
          currentMana: newMana,  // ← ВАЖНО: Отправляем вычисленное значение от сервера!
          maxMana: serverMaxMana,
          timestamp: Date.now()
        });

        console.log(`[Mana] 📤 Broadcasted mana restore to room ${roomId}`);

      } catch (error) {
        console.error('[Mana] ❌ Error processing mana restore:', error.message);
        console.error('[Mana] Stack:', error.stack);
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

    // ═══════════════════════════════════════════
    // ИСПОЛЬЗОВАНИЕ ЗЕЛИЙ (HP/MANA POTIONS)
    // ═══════════════════════════════════════════

    socket.on('use_potion', (data) => {
      try {
        const player = activePlayers.get(socket.id);
        if (!player) {
          console.warn('[Potion] ⚠️ Player not found');
          return;
        }

        // Парсим данные если пришли как JSON строка
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
          } catch (e) {
            console.error('[Potion] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        const { potionType, restoreAmount, currentValue, maxValue } = parsedData;

        console.log(`[Potion] 🍾 ${player.username} used ${potionType} potion`);
        console.log(`[Potion] 📊 Received: restoreAmount=${restoreAmount}, currentValue=${currentValue}, maxValue=${maxValue}`);

        // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Вычисляем новое значение на сервере
        // currentValue от клиента - это ТЕКУЩЕЕ значение ДО использования зелья
        let newValue;

        if (potionType === 'health') {
          // Получаем ТЕКУЩИЙ HP игрока на сервере (server-authoritative)
          const serverCurrentHP = player.health || player.currentHealth || currentValue;
          const serverMaxHP = player.maxHealth || maxValue;

          console.log(`[Potion] 📊 Server HP BEFORE potion: ${serverCurrentHP}/${serverMaxHP}`);

          // Вычисляем новый HP = текущий + восстановление
          newValue = Math.min(serverCurrentHP + restoreAmount, serverMaxHP);

          // Обновляем на сервере
          player.health = newValue;
          player.maxHealth = serverMaxHP;
          player.currentHealth = newValue; // Для совместимости

          console.log(`[Potion] ❤️ ${player.username} HP AFTER potion: ${newValue}/${serverMaxHP} (+${restoreAmount})`);
        } else if (potionType === 'mana') {
          // Получаем ТЕКУЩУЮ ману игрока на сервере
          const serverCurrentMana = player.mana || player.currentMana || currentValue;
          const serverMaxMana = player.maxMana || maxValue;

          console.log(`[Potion] 📊 Server Mana BEFORE potion: ${serverCurrentMana}/${serverMaxMana}`);

          // Вычисляем новую ману = текущая + восстановление
          newValue = Math.min(serverCurrentMana + restoreAmount, serverMaxMana);

          // Обновляем на сервере
          player.mana = newValue;
          player.maxMana = serverMaxMana;
          player.currentMana = newValue; // Для совместимости

          console.log(`[Potion] 💙 ${player.username} Mana AFTER potion: ${newValue}/${serverMaxMana} (+${restoreAmount})`);
        }

        // Отправляем обновление всем игрокам в комнате (включая использовавшего зелье)
        const broadcastData = {
          socketId: socket.id,
          username: player.username,
          potionType: potionType,
          restoreAmount: restoreAmount,
          currentValue: potionType === 'health' ? player.health : player.mana, // НОВОЕ значение от сервера
          maxValue: potionType === 'health' ? player.maxHealth : player.maxMana,
          health: player.health,
          maxHealth: player.maxHealth,
          mana: player.mana || 0,
          maxMana: player.maxMana || 0,
          timestamp: Date.now()
        };

        io.to(player.roomId).emit('potion_used', broadcastData);

        console.log(`[Potion] ✅ ${player.username} potion effect broadcasted to room ${player.roomId}`);
        console.log(`[Potion] 📤 Broadcast data:`, JSON.stringify(broadcastData));

      } catch (error) {
        console.error('[Potion] ❌ Error:', error.message);
        console.error('[Potion] Stack:', error.stack);
      }
    });

    // ═══════════════════════════════════════════
    // ИЗМЕНЕНИЕ ЭКИПИРОВКИ
    // ═══════════════════════════════════════════

    socket.on('equipment_changed', async (data) => {
      try {
        const player = activePlayers.get(socket.id);
        if (!player) {
          console.error('[Equipment] ❌ Player not found');
          return;
        }

        // Парсим данные
        let parsedData = typeof data === 'string' ? JSON.parse(data) : data;
        const {
          slotType,
          itemName,
          isEquip,
          attackBonus,
          defenseBonus,
          healthBonus,
          manaBonus,
          totalAttackBonus,
          totalDefenseBonus,
          totalHealthBonus,
          totalManaBonus,
          currentHealth,
          maxHealth,
          currentMana,
          maxMana,
          attack,
          defense
        } = parsedData;

        console.log(`[Equipment] ⚔️ ${player.username} ${isEquip ? 'equipped' : 'unequipped'} ${itemName} in ${slotType} slot`);
        console.log(`[Equipment] 📊 Item bonuses: ATK+${attackBonus} DEF+${defenseBonus} HP+${healthBonus} MP+${manaBonus}`);
        console.log(`[Equipment] 📊 Total bonuses: ATK+${totalAttackBonus} DEF+${totalDefenseBonus} HP+${totalHealthBonus} MP+${totalManaBonus}`);

        // ═══════════════════════════════════════════════════════════════════
        // SERVER-AUTHORITATIVE: Сервер сам рассчитывает HP/MP/ATK/DEF
        // ═══════════════════════════════════════════════════════════════════

        // Загружаем статы персонажа из БД для точного расчёта
        let characterStats = null;
        let baseHealth = 100;
        let baseMana = 100;
        let baseAttack = 10;

        console.log(`[Equipment] 🔍 Looking for character: userId='${player.userId}' class='${player.characterClass}'`);

        if (player.userId && player.characterClass) {
          try {
            const Character = require('./models/Character');
            const mongoose = require('mongoose');

            // ВАЖНО: Конвертируем userId в ObjectId для MongoDB запроса
            // В схеме Character userId это ObjectId, но player.userId это строка
            let userIdQuery = player.userId;
            if (typeof player.userId === 'string' && mongoose.Types.ObjectId.isValid(player.userId)) {
              userIdQuery = new mongoose.Types.ObjectId(player.userId);
              console.log(`[Equipment] 🔧 Converted userId string to ObjectId`);
            }

            const character = await Character.findOne({ userId: userIdQuery, characterClass: player.characterClass });

            console.log(`[Equipment] 🔍 Database query result: ${character ? 'FOUND' : 'NOT FOUND'}`);

            // DEBUG: Если не найден, покажем все персонажи этого userId
            if (!character) {
              const allChars = await Character.find({ userId: userIdQuery });
              console.log(`[Equipment] 🔍 DEBUG: All characters for this userId: ${allChars.map(c => c.characterClass).join(', ') || 'NONE'}`);
            }

            if (character && character.stats) {
              characterStats = character.stats;

              // Рассчитываем базовые значения на основе РЕАЛЬНЫХ статов персонажа
              // HP = 200 + (Endurance * 40)
              // MP = 50 + (Wisdom * 15)
              // ATK = STR * 5 + INT * 3 (физическая + магическая атака)
              baseHealth = 200 + (characterStats.endurance * 40);
              baseMana = 50 + (characterStats.wisdom * 15);
              baseAttack = (characterStats.strength * 5) + (characterStats.intelligence * 3);

              console.log(`[Equipment] 📊 ${player.characterClass} base stats: END=${characterStats.endurance} WIS=${characterStats.wisdom} STR=${characterStats.strength} INT=${characterStats.intelligence}`);
              console.log(`[Equipment] 📊 Calculated base: HP=${baseHealth} MP=${baseMana} ATK=${baseAttack}`);
            } else {
              // FALLBACK: Персонаж не найден в БД - доверяем значениям от клиента
              // Это происходит когда игрок ещё не сохранил персонажа в MongoDB
              console.warn(`[Equipment] ⚠️ Character not found in DB, using CLIENT values as fallback!`);

              // Вычисляем базовые значения из клиентских данных минус бонусы экипировки
              baseHealth = maxHealth - totalHealthBonus;
              baseMana = maxMana - totalManaBonus;
              baseAttack = attack - totalAttackBonus;

              console.log(`[Equipment] 📊 Fallback base (from client): HP=${baseHealth} MP=${baseMana} ATK=${baseAttack}`);
            }
          } catch (err) {
            console.error(`[Equipment] ❌ Error loading character stats:`, err.message);
          }
        } else {
          console.warn(`[Equipment] ⚠️ Missing userId or characterClass on player object!`);
        }

        // Рассчитываем финальные значения С ЭКИПИРОВКОЙ (server-authoritative!)
        const serverMaxHealth = baseHealth + totalHealthBonus;
        const serverMaxMana = baseMana + totalManaBonus;
        const serverAttack = baseAttack + totalAttackBonus;
        const serverDefense = 0 + totalDefenseBonus; // У нас нет базовой защиты, только от экипировки

        // Сохраняем текущий % HP/MP чтобы не сбрасывать прогресс
        const healthPercent = player.maxHealth > 0 ? (player.currentHealth / player.maxHealth) : 1;
        const manaPercent = player.maxMana > 0 ? (player.currentMana / player.maxMana) : 1;

        // Применяем новый maxHP/maxMP но сохраняем % заполненности
        const serverCurrentHealth = Math.min(serverMaxHealth, Math.round(serverMaxHealth * healthPercent));
        const serverCurrentMana = Math.min(serverMaxMana, Math.round(serverMaxMana * manaPercent));

        console.log(`[Equipment] 🔧 Server recalculated: HP=${serverCurrentHealth}/${serverMaxHealth} MP=${serverCurrentMana}/${serverMaxMana} ATK=${serverAttack} DEF=${serverDefense}`);
        console.log(`[Equipment] 📊 Client sent: HP=${currentHealth}/${maxHealth} MP=${currentMana}/${maxMana} ATK=${attack} DEF=${defense}`);

        // Обновляем данные игрока на сервере (используем СЕРВЕРНЫЕ значения!)
        player.maxHealth = serverMaxHealth;
        player.health = serverCurrentHealth;
        player.currentHealth = serverCurrentHealth;
        player.maxMana = serverMaxMana;
        player.mana = serverCurrentMana;
        player.currentMana = serverCurrentMana;
        player.attack = serverAttack;
        player.defense = serverDefense;

        // Сохраняем информацию о бонусах экипировки
        if (!player.equipment) {
          player.equipment = {};
        }
        player.equipment.totalAttackBonus = totalAttackBonus;
        player.equipment.totalDefenseBonus = totalDefenseBonus;
        player.equipment.totalHealthBonus = totalHealthBonus;
        player.equipment.totalManaBonus = totalManaBonus;

        console.log(`[Equipment] ✅ ${player.username} stats updated (SERVER-AUTHORITATIVE): HP=${serverCurrentHealth}/${serverMaxHealth} MP=${serverCurrentMana}/${serverMaxMana} ATK=${serverAttack} DEF=${serverDefense}`);

        // Broadcast всем игрокам в комнате (отправляем СЕРВЕРНЫЕ значения!)
        io.to(player.roomId).emit('player_equipment_changed', {
          socketId: socket.id,
          username: player.username,
          slotType: slotType,
          itemName: itemName,
          isEquip: isEquip,
          // Бонусы от конкретного предмета
          attackBonus: attackBonus,
          defenseBonus: defenseBonus,
          healthBonus: healthBonus,
          manaBonus: manaBonus,
          // Суммарные бонусы
          totalAttackBonus: totalAttackBonus,
          totalDefenseBonus: totalDefenseBonus,
          totalHealthBonus: totalHealthBonus,
          totalManaBonus: totalManaBonus,
          // Текущие значения (SERVER-AUTHORITATIVE!)
          health: serverCurrentHealth,
          maxHealth: serverMaxHealth,
          mana: serverCurrentMana,
          maxMana: serverMaxMana,
          attack: serverAttack,
          defense: serverDefense,
          timestamp: Date.now()
        });

        console.log(`[Equipment] ✅ ${player.username} equipment change broadcasted to room ${player.roomId}`);

      } catch (error) {
        console.error('[Equipment] ❌ Error:', error.message);
      }
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
    socket.on('player_level_up', async (data) => {
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

        const { newLevel, characterClass, availableStatPoints, currentExperience } = parsedData;

        console.log(`[Level Up] 🎉 ${player.username} достиг уровня ${newLevel}!`);

        // Обновляем уровень в памяти
        player.level = newLevel;
        player.availableStatPoints = availableStatPoints;

        // НОВОЕ: Сохраняем в MongoDB
        try {
          const Character = require('./models/Character');
          const updateData = {
            level: newLevel,
            availableStatPoints: availableStatPoints
          };

          // Если передан текущий опыт, тоже сохраняем
          if (currentExperience !== undefined) {
            updateData.experience = currentExperience;
          }

          const result = await Character.findOneAndUpdate(
            { userId: player.userId, characterClass: characterClass },
            { $set: updateData },
            { new: true }
          );

          if (result) {
            console.log(`[Level Up] 💾 Сохранено в MongoDB: ${player.username} Level ${newLevel}, StatPoints ${availableStatPoints}, XP ${currentExperience || 'N/A'}`);
          } else {
            console.warn(`[Level Up] ⚠️ Character not found in DB: userId=${player.userId}, class=${characterClass}`);
          }
        } catch (dbError) {
          console.error('[Level Up] ❌ MongoDB save error:', dbError.message);
        }

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
    // PARTY SYSTEM (ГРУППЫ)
    // ═══════════════════════════════════════════

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
    // ENEMY HP SYNC SYSTEM (СИНХРОНИЗАЦИЯ HP МОНСТРОВ)
    // ═══════════════════════════════════════════

    // Серверное хранилище HP врагов (enemyId => { currentHealth, maxHealth, lastUpdate })
    // Используем глобальную Map вне socket.on для хранения между подключениями
    if (!io.enemyHealthStorage) {
      io.enemyHealthStorage = new Map();
      console.log('[Enemy Sync] 🗄️ Серверное хранилище HP врагов инициализировано');
    }

    /**
     * НОВОЕ: Регистрация врага на сервере (вызывается хостом при старте)
     * Это позволяет серверу знать о враге ДО первой атаки
     * и гарантирует правильный HP для всех игроков
     */
    socket.on('enemy_register', (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
          } catch (e) {
            console.error('[Enemy Register] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          return; // Молча игнорируем
        }

        // Только хост может регистрировать врагов
        const roomHost = io.enemyHostByRoom?.get(player.roomId);
        if (roomHost !== socket.id) {
          return; // Не хост - игнорируем
        }

        const { enemyId, maxHealth, x, y, z } = parsedData;
        if (!enemyId) return;

        // Проверяем существует ли враг уже
        const existingEnemy = io.enemyHealthStorage.get(enemyId);
        if (existingEnemy && existingEnemy.currentHealth > 0) {
          // Враг уже зарегистрирован и жив - не перезаписываем
          return;
        }

        // Регистрируем врага с полным HP
        io.enemyHealthStorage.set(enemyId, {
          roomId: player.roomId,
          currentHealth: maxHealth,
          maxHealth: maxHealth,
          x: x,
          y: y,
          z: z,
          lastUpdate: Date.now(),
          isDead: false,
          registeredBy: player.username
        });

        console.log(`[Enemy Register] 📝 Враг ${enemyId} зарегистрирован хостом ${player.username} с HP ${maxHealth}`);

      } catch (error) {
        console.error('[Enemy Register] ❌ Error:', error.message);
      }
    });

    /**
     * Синхронизация урона по врагу от клиента
     * СЕРВЕР АВТОРИТЕТЕН: рассчитывает HP сам и рассылает ВСЕМ клиентам (включая атакующего)
     * Это как player_damage в MMO - клиент не доверяется
     */
    socket.on('enemy_damage_sync', (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
          } catch (e) {
            console.error('[Enemy Sync] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.warn(`[Enemy Sync] ⚠️ Player not found: ${socket.id}`);
          return;
        }

        const { enemyId, damage, maxHealth, isCritical } = parsedData;

        if (!enemyId) {
          console.error('[Enemy Sync] ❌ No enemyId provided');
          return;
        }

        if (!damage || damage <= 0) {
          console.error('[Enemy Sync] ❌ Invalid damage:', damage);
          return;
        }

        // Получаем текущее состояние HP или инициализируем
        let enemyData = io.enemyHealthStorage.get(enemyId);

        if (!enemyData) {
          // Враг не был атакован ранее - инициализируем с maxHealth от клиента
          enemyData = {
            roomId: player.roomId, // КРИТИЧЕСКОЕ: Привязываем врага к комнате
            currentHealth: maxHealth || 2000,
            maxHealth: maxHealth || 2000,
            lastUpdate: Date.now(),
            isDead: false
          };
          console.log(`[Enemy Sync] 🆕 Инициализирован враг ${enemyId} в комнате ${player.roomId} с HP ${enemyData.maxHealth}`);
        } else if (!enemyData.roomId) {
          // Если roomId не был установлен ранее - добавляем
          enemyData.roomId = player.roomId;
        }

        // Если враг уже мёртв - игнорируем урон
        if (enemyData.isDead || enemyData.currentHealth <= 0) {
          console.log(`[Enemy Sync] ⏭️ Враг ${enemyId} уже мёртв, игнорируем урон`);
          return;
        }

        // СЕРВЕР РАССЧИТЫВАЕТ HP (авторитетно!)
        const oldHealth = enemyData.currentHealth;
        enemyData.currentHealth = Math.max(0, enemyData.currentHealth - damage);
        enemyData.lastUpdate = Date.now();
        enemyData.lastAttacker = player.username;

        // Проверяем смерть
        const justDied = oldHealth > 0 && enemyData.currentHealth <= 0;
        if (justDied) {
          enemyData.isDead = true;
          enemyData.killedBy = player.username;
        }

        // Сохраняем на сервере
        io.enemyHealthStorage.set(enemyId, enemyData);

        console.log(`[Enemy Sync] 📥 ${player.username} нанёс урон ${enemyId}: -${damage} HP → ${enemyData.currentHealth}/${enemyData.maxHealth}${justDied ? ' (УБИТ!)' : ''}`);

        // Рассылаем ВСЕМ клиентам в комнате (ВКЛЮЧАЯ атакующего!)
        // Это критически важно - все клиенты должны получить серверный HP
        if (player.roomId) {
          const syncData = JSON.stringify({
            enemyId: enemyId,
            damage: damage,
            currentHealth: enemyData.currentHealth,
            maxHealth: enemyData.maxHealth,
            attackerSocketId: socket.id,
            attackerName: player.username,
            isCritical: isCritical || false,
            isDead: enemyData.isDead || false,
            timestamp: Date.now()
          });

          // Отправляем ВСЕМ в комнате (io.to вместо socket.to)
          io.to(player.roomId).emit('enemy_hp_synced', syncData);

          console.log(`[Enemy Sync] 📤 HP ${enemyId} разослан ВСЕМ в комнате ${player.roomId}: ${enemyData.currentHealth}/${enemyData.maxHealth}`);
        }

      } catch (error) {
        console.error('[Enemy Sync] ❌ Error:', error.message);
      }
    });

    /**
     * Синхронизация смерти врага
     */
    socket.on('enemy_death_sync', (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
          } catch (e) {
            console.error('[Enemy Death] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.warn(`[Enemy Death] ⚠️ Player not found: ${socket.id}`);
          return;
        }

        const { enemyId, killerSocketId, killerName, timestamp } = parsedData;

        if (!enemyId) {
          console.error('[Enemy Death] ❌ No enemyId provided');
          return;
        }

        // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Сохраняем roomId при смерти!
        const existingDeathData = io.enemyHealthStorage.get(enemyId) || {};
        io.enemyHealthStorage.set(enemyId, {
          ...existingDeathData, // Сохраняем существующие данные (включая roomId и позицию!)
          roomId: player.roomId, // Явно устанавливаем roomId
          currentHealth: 0,
          maxHealth: existingDeathData.maxHealth || 100,
          lastUpdate: timestamp || Date.now(),
          isDead: true,
          killedBy: killerName
        });

        console.log(`[Enemy Death] 💀 ${enemyId} убит игроком ${killerName}`);

        // Рассылаем ВСЕМ клиентам в комнате
        if (player.roomId) {
          io.to(player.roomId).emit('enemy_death_synced', JSON.stringify({
            enemyId: enemyId,
            killerSocketId: killerSocketId || socket.id,
            killerName: killerName || player.username,
            timestamp: Date.now()
          }));

          console.log(`[Enemy Death] 📤 Смерть ${enemyId} разослана в комнату ${player.roomId}`);
        }

      } catch (error) {
        console.error('[Enemy Death] ❌ Error:', error.message);
      }
    });

    /**
     * Запрос текущего HP врага (при подключении нового клиента)
     */
    socket.on('enemy_request_health', (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
          } catch (e) {
            console.error('[Enemy Request] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        const { enemyId } = parsedData;

        if (!enemyId) {
          console.error('[Enemy Request] ❌ No enemyId provided');
          return;
        }

        // Ищем HP в серверном хранилище
        const enemyData = io.enemyHealthStorage.get(enemyId);

        if (enemyData) {
          // Отправляем текущий HP запрашивающему клиенту
          socket.emit('enemy_hp_response', JSON.stringify({
            enemyId: enemyId,
            currentHealth: enemyData.currentHealth,
            maxHealth: enemyData.maxHealth,
            isDead: enemyData.isDead || false,
            timestamp: Date.now()
          }));

          console.log(`[Enemy Request] 📤 HP ${enemyId}: ${enemyData.currentHealth}/${enemyData.maxHealth} отправлен клиенту ${socket.id}`);
        } else {
          console.log(`[Enemy Request] ⚠️ HP для ${enemyId} не найден на сервере (враг не был атакован)`);
        }

      } catch (error) {
        console.error('[Enemy Request] ❌ Error:', error.message);
      }
    });

    /**
     * Респавн врага (сброс HP)
     */
    socket.on('enemy_respawn_sync', (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
          } catch (e) {
            console.error('[Enemy Respawn] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.warn(`[Enemy Respawn] ⚠️ Player not found: ${socket.id}`);
          return;
        }

        const { enemyId, maxHealth, x, y, z, timestamp } = parsedData;

        if (!enemyId) {
          console.error('[Enemy Respawn] ❌ No enemyId provided');
          return;
        }

        // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Сохраняем roomId при респавне!
        // Раньше roomId терялся при полной перезаписи объекта
        const existingData = io.enemyHealthStorage.get(enemyId) || {};
        io.enemyHealthStorage.set(enemyId, {
          ...existingData, // Сохраняем существующие данные (включая roomId!)
          roomId: player.roomId, // Явно устанавливаем roomId на случай если его не было
          currentHealth: maxHealth,
          maxHealth: maxHealth,
          x: x,
          y: y,
          z: z,
          lastUpdate: timestamp || Date.now(),
          isDead: false
        });

        console.log(`[Enemy Respawn] ♻️ ${enemyId} респавнулся с HP ${maxHealth} в позиции (${x}, ${y}, ${z})`);

        // Рассылаем ВСЕМ клиентам в комнате
        if (player.roomId) {
          io.to(player.roomId).emit('enemy_respawn_synced', JSON.stringify({
            enemyId: enemyId,
            maxHealth: maxHealth,
            x: x,
            y: y,
            z: z,
            timestamp: Date.now()
          }));

          console.log(`[Enemy Respawn] 📤 Респавн ${enemyId} разослан в комнату ${player.roomId}`);
        }

      } catch (error) {
        console.error('[Enemy Respawn] ❌ Error:', error.message);
      }
    });

    // ═══════════════════════════════════════════
    // ENEMY POSITION SYNC SYSTEM (СИНХРОНИЗАЦИЯ ПОЗИЦИИ МОНСТРОВ)
    // ═══════════════════════════════════════════

    // Хранилище хостов врагов по комнатам (roomId => socketId хоста)
    if (!io.enemyHostByRoom) {
      io.enemyHostByRoom = new Map();
      console.log('[Enemy Host] 🗄️ Хранилище хостов врагов инициализировано');
    }

    /**
     * Запрос статуса хоста врагов
     * Первый игрок в комнате становится хостом
     */
    socket.on('enemy_request_host_status', (data) => {
      try {
        const player = activePlayers.get(socket.id);
        if (!player || !player.roomId) {
          console.warn(`[Enemy Host] ⚠️ Player not found or not in room: ${socket.id}`);
          // КРИТИЧЕСКОЕ: Отправляем ответ с isHost=false чтобы клиент знал что запрос получен
          // но не может быть обработан (игрок ещё не присоединился к комнате)
          socket.emit('enemy_host_status', JSON.stringify({
            isHost: false,
            hostSocketId: null,
            error: 'not_in_room',
            timestamp: Date.now()
          }));
          return;
        }

        const roomId = player.roomId;
        let currentHost = io.enemyHostByRoom.get(roomId);

        // Проверяем существует ли хост и подключен ли он
        if (currentHost) {
          const hostPlayer = activePlayers.get(currentHost);
          if (!hostPlayer || hostPlayer.roomId !== roomId) {
            // Хост отключился или сменил комнату - сбрасываем
            currentHost = null;
            io.enemyHostByRoom.delete(roomId);
            console.log(`[Enemy Host] 🔄 Хост комнаты ${roomId} отключился, сбрасываем`);
          }
        }

        // Если нет хоста - назначаем текущего игрока
        let isHost = false;
        if (!currentHost) {
          io.enemyHostByRoom.set(roomId, socket.id);
          isHost = true;
          console.log(`[Enemy Host] 👑 ${player.username} назначен хостом врагов в комнате ${roomId}`);
        } else if (currentHost === socket.id) {
          isHost = true;
        }

        // Отправляем статус игроку
        socket.emit('enemy_host_status', JSON.stringify({
          isHost: isHost,
          hostSocketId: io.enemyHostByRoom.get(roomId),
          timestamp: Date.now()
        }));

        console.log(`[Enemy Host] 📤 Статус хоста отправлен ${player.username}: isHost=${isHost}`);

      } catch (error) {
        console.error('[Enemy Host] ❌ Error:', error.message);
      }
    });

    /**
     * Синхронизация позиции врага от хоста
     * НОВОЕ: Поддержка velocity для Dead Reckoning (как у player_update → player_moved)
     */
    socket.on('enemy_position_sync', (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
          } catch (e) {
            console.error('[Enemy Position] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        const player = activePlayers.get(socket.id);
        if (!player || !player.roomId) {
          return; // Молча игнорируем если игрок не в комнате
        }

        // Проверяем что отправитель - хост
        const roomHost = io.enemyHostByRoom.get(player.roomId);
        if (roomHost !== socket.id) {
          // Не хост - игнорируем (это нормально, не логируем)
          return;
        }

        // НОВОЕ: Извлекаем velocity для Dead Reckoning
        const { enemyId, x, y, z, rotY, velX, velY, velZ, isMoving, isDead, timestamp } = parsedData;

        if (!enemyId) {
          return;
        }

        // Обновляем позицию в хранилище (добавляем roomId для фильтрации!)
        const existingData = io.enemyHealthStorage.get(enemyId) || {};
        io.enemyHealthStorage.set(enemyId, {
          ...existingData,
          roomId: player.roomId, // КРИТИЧЕСКОЕ: Привязываем врага к комнате
          x: x,
          y: y,
          z: z,
          rotY: rotY,
          velX: velX || 0,
          velY: velY || 0,
          velZ: velZ || 0,
          isMoving: isMoving,
          isDead: isDead,
          lastPositionUpdate: Date.now()
        });

        // НОВОЕ: Рассылаем позицию ВСЕМ клиентам в комнате С velocity для Dead Reckoning
        // Это идентично паттерну player_update → player_moved для NetworkPlayer
        const positionData = JSON.stringify({
          enemyId: enemyId,
          x: x,
          y: y,
          z: z,
          rotY: rotY,
          velX: velX || 0,
          velY: velY || 0,
          velZ: velZ || 0,
          isMoving: isMoving,
          isDead: isDead,
          timestamp: timestamp || Date.now()
        });

        socket.to(player.roomId).emit('enemy_position_synced', positionData);

        // ДИАГНОСТИКА: Логируем каждую 100-ую позицию
        if (Math.random() < 0.01) {
          const roomSockets = io.sockets.adapter.rooms.get(player.roomId);
          const roomSize = roomSockets ? roomSockets.size : 0;
          console.log(`[Enemy Position] 📤 ${enemyId} → room ${player.roomId} (${roomSize} клиентов): pos=(${x.toFixed(1)},${y.toFixed(1)},${z.toFixed(1)})`);
        }

      } catch (error) {
        console.error('[Enemy Position] ❌ Error:', error.message);
      }
    });

    /**
     * НОВОЕ: Синхронизация анимации врага от хоста
     * Аналог update_animation → player_animation_changed для игроков
     */
    socket.on('enemy_animation_sync', (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          try {
            parsedData = JSON.parse(data);
          } catch (e) {
            console.error('[Enemy Animation] ❌ Failed to parse JSON:', e.message);
            return;
          }
        }

        const player = activePlayers.get(socket.id);
        if (!player || !player.roomId) {
          return;
        }

        // Проверяем что отправитель - хост
        const roomHost = io.enemyHostByRoom.get(player.roomId);
        if (roomHost !== socket.id) {
          return;
        }

        const { enemyId, animation, timestamp } = parsedData;

        if (!enemyId || !animation) {
          return;
        }

        // Сохраняем анимацию в хранилище
        const existingData = io.enemyHealthStorage.get(enemyId) || {};
        io.enemyHealthStorage.set(enemyId, {
          ...existingData,
          animation: animation,
          lastAnimationUpdate: Date.now()
        });

        // Рассылаем анимацию ВСЕМ клиентам в комнате (кроме хоста)
        socket.to(player.roomId).emit('enemy_animation_synced', JSON.stringify({
          enemyId: enemyId,
          animation: animation,
          timestamp: timestamp || Date.now()
        }));

        // Логируем каждую 10-ую анимацию для отладки
        if (Math.random() < 0.1) {
          console.log(`[Enemy Animation] 🎬 ${enemyId} → ${animation}`);
        }

      } catch (error) {
        console.error('[Enemy Animation] ❌ Error:', error.message);
      }
    });

    /**
     * Запрос текущих позиций всех врагов (при подключении)
     * НОВОЕ: Включает velocity и animation для полной синхронизации
     */
    socket.on('enemy_request_all_positions', (data) => {
      try {
        const player = activePlayers.get(socket.id);
        if (!player || !player.roomId) {
          return;
        }

        // Собираем позиции врагов ТОЛЬКО из текущей комнаты
        const enemies = [];
        for (const [enemyId, enemyData] of io.enemyHealthStorage.entries()) {
          // КРИТИЧЕСКОЕ: Фильтруем по roomId!
          if (enemyData.x !== undefined && enemyData.roomId === player.roomId) {
            enemies.push({
              enemyId: enemyId,
              x: enemyData.x,
              y: enemyData.y,
              z: enemyData.z,
              rotY: enemyData.rotY || 0,
              velX: enemyData.velX || 0,
              velY: enemyData.velY || 0,
              velZ: enemyData.velZ || 0,
              isMoving: enemyData.isMoving || false,
              isDead: enemyData.isDead || false,
              currentHealth: enemyData.currentHealth,
              maxHealth: enemyData.maxHealth,
              animation: enemyData.animation || 'Idle'
            });
          }
        }

        socket.emit('enemy_all_positions', JSON.stringify({
          enemies: enemies,
          timestamp: Date.now()
        }));

        console.log(`[Enemy Position] 📤 Отправлено ${enemies.length} позиций врагов клиенту ${player.username}`);

      } catch (error) {
        console.error('[Enemy Position] ❌ Error:', error.message);
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
    // ═══════════════════════════════════════════
    // MMO INVENTORY SYSTEM (NEW)
    // ═══════════════════════════════════════════

    socket.on('mmo_load_inventory', async (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          parsedData = JSON.parse(data);
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.error('[MMO Inventory] ❌ Player not found:', socket.id);
          socket.emit('mmo_inventory_response', JSON.stringify({
            success: false,
            message: 'Player not found'
          }));
          return;
        }

        const { characterClass } = parsedData;

        console.log(`[MMO Inventory] 📥 ${player.username} загружает инвентарь (${characterClass})`);

        const User = require('./models/User');
        const Character = require('./models/Character');

        // Находим User по username
        const user = await User.findOne({ username: player.username });
        if (!user) {
          socket.emit('mmo_inventory_response', JSON.stringify({
            success: false,
            message: `User not found: ${player.username}`
          }));
          return;
        }

        // Загружаем Character
        let character = await Character.findOne({
          userId: user._id,
          characterClass: characterClass
        });

        // Если персонаж не найден, создаём с пустым инвентарём
        if (!character) {
          console.log(`[MMO Inventory] 🆕 Создаём нового персонажа для ${player.username} (${characterClass})`);

          character = new Character({
            userId: user._id,
            characterClass: characterClass,
            level: 1,
            experience: 0,
            stats: { strength: 1, perception: 1, endurance: 1, wisdom: 1, intelligence: 1, agility: 1, luck: 1 },
            availableStatPoints: 0,
            inventory: [],
            equipment: {}
          });

          await character.save();
        }

        // Формируем snapshot
        const snapshot = {
          items: character.inventory || [],
          equipment: character.equipment || {},
          gold: character.gold || 0,
          lastModified: Date.now()
        };

        console.log(`[MMO Inventory] ✅ Инвентарь загружен: ${snapshot.items.length} предметов, ${snapshot.gold} золота`);

        socket.emit('mmo_inventory_response', JSON.stringify({
          success: true,
          message: 'Inventory loaded',
          snapshot: snapshot
        }));

      } catch (error) {
        console.error('[MMO Inventory Load] ❌ Error:', error.message);
        socket.emit('mmo_inventory_response', JSON.stringify({
          success: false,
          message: error.message
        }));
      }
    });

    // Сохранить инвентарь на сервер
    socket.on('mmo_save_inventory', async (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          parsedData = JSON.parse(data);
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.error('[MMO Inventory Save] ❌ Player not found:', socket.id);
          return;
        }

        const { characterClass, inventory } = parsedData;

        console.log(`[MMO Inventory] 💾 ${player.username} сохраняет инвентарь (${characterClass})`);
        console.log(`[MMO Inventory] 📦 Items: ${inventory.items.length}, Gold: ${inventory.gold}`);

        const User = require('./models/User');
        const Character = require('./models/Character');

        // Находим User по username
        const user = await User.findOne({ username: player.username });
        if (!user) {
          console.error('[MMO Inventory Save] ❌ User not found:', player.username);
          return;
        }

        // Загружаем или создаём Character
        let character = await Character.findOne({
          userId: user._id,
          characterClass: characterClass
        });

        if (!character) {
          console.log(`[MMO Inventory Save] 🆕 Создаём нового персонажа для ${player.username} (${characterClass})`);
          character = new Character({
            userId: user._id,
            characterClass: characterClass,
            level: 1,
            experience: 0,
            stats: { strength: 1, perception: 1, endurance: 1, wisdom: 1, intelligence: 1, agility: 1, luck: 1 },
            availableStatPoints: 0,
            inventory: [],
            equipment: {},
            gold: 0
          });
        }

        // Обновляем инвентарь
        character.inventory = inventory.items || [];
        character.equipment = inventory.equipment || {};
        character.gold = inventory.gold || 0;

        await character.save();

        console.log(`[MMO Inventory] ✅ Инвентарь сохранён для ${player.username}: ${character.inventory.length} предметов, ${character.gold} золота`);

      } catch (error) {
        console.error('[MMO Inventory Save] ❌ Error:', error.message);
      }
    });

    socket.on('mmo_add_item', async (data) => {
      try {
        console.log(`[MMO Inventory] 🔥 EVENT RECEIVED: mmo_add_item from ${socket.id}`);

        let parsedData = data;
        if (typeof data === 'string') {
          parsedData = JSON.parse(data);
        }

        console.log(`[MMO Inventory] 📦 Parsed data:`, parsedData);
        console.log(`[MMO Inventory] 🔍 activePlayers has ${socket.id}? ${activePlayers.has(socket.id)}`);

        const player = activePlayers.get(socket.id);
        if (!player) {
          console.error(`[MMO Inventory] ❌ Player ${socket.id} NOT FOUND in activePlayers!`);
          socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: 'Player not found' }));
          return;
        }

        const { characterClass, itemId, itemName, quantity, slotIndex } = parsedData;

        console.log(`[MMO Inventory] ✅ Player found: ${player.username}`);
        console.log(`[MMO Inventory] ➕ ${player.username} добавляет предмет: ${itemName} x${quantity} в слот ${slotIndex}`);

        const User = require('./models/User');
        const Character = require('./models/Character');

        const user = await User.findOne({ username: player.username });
        if (!user) {
          socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: 'User not found' }));
          return;
        }

        const character = await Character.findOne({
          userId: user._id,
          characterClass: characterClass
        });

        if (!character) {
          socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: 'Character not found' }));
          return;
        }

        // Добавляем предмет в инвентарь
        if (!character.inventory) {
          character.inventory = [];
        }

        // Проверяем занят ли слот
        const existingItemIndex = character.inventory.findIndex(item => item.slotIndex === slotIndex);

        if (existingItemIndex !== -1) {
          // Слот занят - увеличиваем количество
          character.inventory[existingItemIndex].quantity += quantity;
        } else {
          // Слот свободен - добавляем новый предмет
          character.inventory.push({
            itemId: itemId,
            itemName: itemName,
            quantity: quantity,
            slotIndex: slotIndex,
            timestamp: Date.now()
          });
        }

        await character.save();

        // Возвращаем обновлённый snapshot
        const snapshot = {
          items: character.inventory || [],
          equipment: character.equipment || {},
          gold: character.gold || 0,
          lastModified: Date.now()
        };

        console.log(`[MMO Inventory] ✅ Предмет добавлен успешно`);

        const response = {
          success: true,
          message: 'Item added',
          snapshot: snapshot
        };

        console.log(`[MMO Inventory] 📤 Отправляем ответ клиенту: ${JSON.stringify(response).substring(0, 100)}...`);
        socket.emit('mmo_inventory_response', JSON.stringify(response));

      } catch (error) {
        console.error('[MMO Inventory Add] ❌ Error:', error.message);
        socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: error.message }));
      }
    });

    socket.on('mmo_move_item', async (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          parsedData = JSON.parse(data);
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: 'Player not found' }));
          return;
        }

        const { characterClass, fromSlot, toSlot } = parsedData;

        console.log(`[MMO Inventory] 🔄 ${player.username} перемещает предмет: слот ${fromSlot} → ${toSlot}`);

        const User = require('./models/User');
        const Character = require('./models/Character');

        const user = await User.findOne({ username: player.username });
        if (!user) {
          socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: 'User not found' }));
          return;
        }

        const character = await Character.findOne({
          userId: user._id,
          characterClass: characterClass
        });

        if (!character) {
          socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: 'Character not found' }));
          return;
        }

        // Находим предметы в слотах
        const fromIndex = character.inventory.findIndex(item => item.slotIndex === fromSlot);
        const toIndex = character.inventory.findIndex(item => item.slotIndex === toSlot);

        if (fromIndex === -1) {
          socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: 'Source slot is empty' }));
          return;
        }

        if (toIndex === -1) {
          // Целевой слот пустой - просто перемещаем
          character.inventory[fromIndex].slotIndex = toSlot;
        } else {
          // Целевой слот занят - меняем местами
          character.inventory[fromIndex].slotIndex = toSlot;
          character.inventory[toIndex].slotIndex = fromSlot;
        }

        await character.save();

        // Возвращаем обновлённый snapshot
        const snapshot = {
          items: character.inventory || [],
          equipment: character.equipment || {},
          gold: character.gold || 0,
          lastModified: Date.now()
        };

        console.log(`[MMO Inventory] ✅ Предмет перемещён успешно`);

        socket.emit('mmo_inventory_response', JSON.stringify({
          success: true,
          message: 'Item moved',
          snapshot: snapshot
        }));

      } catch (error) {
        console.error('[MMO Inventory Move] ❌ Error:', error.message);
        socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: error.message }));
      }
    });

    socket.on('mmo_remove_item', async (data) => {
      try {
        let parsedData = data;
        if (typeof data === 'string') {
          parsedData = JSON.parse(data);
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: 'Player not found' }));
          return;
        }

        const { characterClass, slotIndex, quantity } = parsedData;

        console.log(`[MMO Inventory] 🗑️ ${player.username} удаляет предмет из слота ${slotIndex} (количество: ${quantity || 'все'})`);

        const User = require('./models/User');
        const Character = require('./models/Character');

        const user = await User.findOne({ username: player.username });
        if (!user) {
          socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: 'User not found' }));
          return;
        }

        const character = await Character.findOne({
          userId: user._id,
          characterClass: characterClass
        });

        if (!character) {
          socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: 'Character not found' }));
          return;
        }

        // Находим предмет в слоте
        const itemIndex = character.inventory.findIndex(item => item.slotIndex === slotIndex);

        if (itemIndex === -1) {
          socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: 'Slot is empty' }));
          return;
        }

        if (quantity === 0 || quantity >= character.inventory[itemIndex].quantity) {
          // Удаляем весь стак
          character.inventory.splice(itemIndex, 1);
        } else {
          // Уменьшаем количество
          character.inventory[itemIndex].quantity -= quantity;
        }

        await character.save();

        // Возвращаем обновлённый snapshot
        const snapshot = {
          items: character.inventory || [],
          equipment: character.equipment || {},
          gold: character.gold || 0,
          lastModified: Date.now()
        };

        console.log(`[MMO Inventory] ✅ Предмет удалён успешно`);

        socket.emit('mmo_inventory_response', JSON.stringify({
          success: true,
          message: 'Item removed',
          snapshot: snapshot
        }));

      } catch (error) {
        console.error('[MMO Inventory Remove] ❌ Error:', error.message);
        socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: error.message }));
      }
    });

    // ═══════════════════════════════════════════════════════════════
    // MMO EQUIPMENT HANDLERS
    // ═══════════════════════════════════════════════════════════════

    socket.on('mmo_update_equipment', async (data) => {
      try {
        console.log(`[MMO Equipment] 🔥 EVENT RECEIVED: mmo_update_equipment from ${socket.id}`);

        let parsedData = data;
        if (typeof data === 'string') {
          parsedData = JSON.parse(data);
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          socket.emit('mmo_equipment_response', JSON.stringify({ success: false, message: 'Player not found' }));
          return;
        }

        const { characterClass, weapon, armor, helmet, accessory } = parsedData;

        console.log(`[MMO Equipment] ⚔️ ${player.username} updates equipment: Weapon=${weapon}, Armor=${armor}, Helmet=${helmet}, Accessory=${accessory}`);

        const User = require('./models/User');
        const Character = require('./models/Character');

        const user = await User.findOne({ username: player.username });
        if (!user) {
          socket.emit('mmo_equipment_response', JSON.stringify({ success: false, message: 'User not found' }));
          return;
        }

        const character = await Character.findOne({
          userId: user._id,
          characterClass: characterClass
        });

        if (!character) {
          socket.emit('mmo_equipment_response', JSON.stringify({ success: false, message: 'Character not found' }));
          return;
        }

        // Обновляем экипировку
        character.equipment = {
          weapon: weapon || "",
          armor: armor || "",
          helmet: helmet || "",
          accessory: accessory || ""
        };

        await character.save();

        console.log(`[MMO Equipment] ✅ Equipment saved for ${character.characterClass}`);

        socket.emit('mmo_equipment_response', JSON.stringify({
          success: true,
          message: 'Equipment updated',
          equipment: character.equipment
        }));

      } catch (error) {
        console.error('[MMO Equipment Update] ❌ Error:', error.message);
        socket.emit('mmo_equipment_response', JSON.stringify({ success: false, message: error.message }));
      }
    });

    socket.on('mmo_load_equipment', async (data) => {
      try {
        console.log(`[MMO Equipment] 📥 EVENT RECEIVED: mmo_load_equipment from ${socket.id}`);

        let parsedData = data;
        if (typeof data === 'string') {
          parsedData = JSON.parse(data);
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          socket.emit('mmo_equipment_response', JSON.stringify({ success: false, message: 'Player not found' }));
          return;
        }

        const { characterClass } = parsedData;

        const User = require('./models/User');
        const Character = require('./models/Character');

        const user = await User.findOne({ username: player.username });
        if (!user) {
          socket.emit('mmo_equipment_response', JSON.stringify({ success: false, message: 'User not found' }));
          return;
        }

        const character = await Character.findOne({
          userId: user._id,
          characterClass: characterClass
        });

        if (!character) {
          socket.emit('mmo_equipment_response', JSON.stringify({ success: false, message: 'Character not found' }));
          return;
        }

        console.log(`[MMO Equipment] ✅ Loaded equipment for ${character.characterClass}`);

        socket.emit('mmo_equipment_response', JSON.stringify({
          success: true,
          message: 'Equipment loaded',
          equipment: character.equipment || { weapon: "", armor: "", helmet: "", accessory: "" }
        }));

      } catch (error) {
        console.error('[MMO Equipment Load] ❌ Error:', error.message);
        socket.emit('mmo_equipment_response', JSON.stringify({ success: false, message: error.message }));
      }
    });

    socket.on('mmo_update_quantity', async (data) => {
      try {
        console.log(`[MMO Inventory] 🔢 EVENT RECEIVED: mmo_update_quantity from ${socket.id}`);

        let parsedData = data;
        if (typeof data === 'string') {
          parsedData = JSON.parse(data);
        }

        const player = activePlayers.get(socket.id);
        if (!player) {
          socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: 'Player not found' }));
          return;
        }

        const { characterClass, slotIndex, newQuantity } = parsedData;

        console.log(`[MMO Inventory] 🔢 ${player.username} updates quantity in slot ${slotIndex} to ${newQuantity}`);

        const User = require('./models/User');
        const Character = require('./models/Character');

        const user = await User.findOne({ username: player.username });
        if (!user) {
          socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: 'User not found' }));
          return;
        }

        const character = await Character.findOne({
          userId: user._id,
          characterClass: characterClass
        });

        if (!character) {
          socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: 'Character not found' }));
          return;
        }

        // Находим предмет в слоте
        const itemIndex = character.inventory.findIndex(item => item.slotIndex === slotIndex);

        if (itemIndex === -1) {
          socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: 'Item not found in slot' }));
          return;
        }

        // Обновляем количество
        character.inventory[itemIndex].quantity = newQuantity;

        await character.save();

        console.log(`[MMO Inventory] ✅ Quantity updated for slot ${slotIndex}`);

        const snapshot = {
          items: character.inventory || [],
          equipment: character.equipment || {},
          gold: character.gold || 0,
          lastModified: Date.now()
        };

        socket.emit('mmo_inventory_response', JSON.stringify({
          success: true,
          message: 'Quantity updated',
          snapshot: snapshot
        }));

      } catch (error) {
        console.error('[MMO Inventory Update Quantity] ❌ Error:', error.message);
        socket.emit('mmo_inventory_response', JSON.stringify({ success: false, message: error.message }));
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

  // ═══════════════════════════════════════════════════════════════════════════
  // НОВОЕ: ПЕРИОДИЧЕСКАЯ РАССЫЛКА ПОЗИЦИЙ МОНСТРОВ ВСЕМ КЛИЕНТАМ
  // Сервер хранит позиции и каждые 50мс рассылает их ВСЕМ игрокам в каждой комнате
  // Это гарантирует синхронизацию даже если отдельные пакеты потеряются
  // ═══════════════════════════════════════════════════════════════════════════
  setInterval(() => {
    // Собираем все комнаты с врагами
    // Проверяем что io.enemyHealthStorage инициализирован
    if (!io.enemyHealthStorage) return;

    const roomEnemies = new Map(); // roomId -> [enemies]

    for (const [enemyId, enemyData] of io.enemyHealthStorage.entries()) {
      if (!enemyData.roomId || enemyData.x === undefined) continue;

      if (!roomEnemies.has(enemyData.roomId)) {
        roomEnemies.set(enemyData.roomId, []);
      }

      roomEnemies.get(enemyData.roomId).push({
        enemyId: enemyId,
        x: enemyData.x,
        y: enemyData.y,
        z: enemyData.z,
        rotY: enemyData.rotY || 0,
        velX: enemyData.velX || 0,
        velY: enemyData.velY || 0,
        velZ: enemyData.velZ || 0,
        isMoving: enemyData.isMoving || false,
        isDead: enemyData.isDead || false,
        animation: enemyData.animation || 'Idle',
        currentHealth: enemyData.currentHealth,
        maxHealth: enemyData.maxHealth
      });
    }

    // Рассылаем позиции ВСЕМ клиентам в каждой комнате
    for (const [roomId, enemies] of roomEnemies.entries()) {
      if (enemies.length === 0) continue;

      const broadcastData = JSON.stringify({
        enemies: enemies,
        timestamp: Date.now()
      });

      // io.to() отправляет ВСЕМ в комнате включая хоста
      io.to(roomId).emit('enemy_world_state', broadcastData);
    }
  }, 50); // 20 раз в секунду = 50мс

  // Логируем статус каждые 30 секунд
  setInterval(() => {
    if (!io.enemyHealthStorage) return;
    const totalEnemies = io.enemyHealthStorage.size;
    const totalPlayers = activePlayers.size;
    if (totalEnemies > 0 || totalPlayers > 0) {
      console.log(`[World State] 🌍 Активно: ${totalPlayers} игроков, ${totalEnemies} врагов`);
    }
  }, 30000);
};
