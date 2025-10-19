/**
 * Multiplayer Logic - Socket.IO Event Handlers
 * Обрабатывает все real-time события мультиплеера
 */

const Room = require('./models/Room');
const Character = require('./models/Character');

// Хранилище активных игроков
const activePlayers = new Map(); // socketId => { roomId, username, characterClass, position, animation, stats, userId, spawnIndex }

// Хранилище врагов в комнатах
const roomEnemies = new Map(); // roomId => Map(enemyId => { health, alive, position })

// Хранилище использованных точек спавна в комнатах
const roomSpawnPoints = new Map(); // roomId => Set(spawnIndex)
const MAX_SPAWN_POINTS = 20; // Максимум 20 точек спавна

// ═══════════════════════════════════════════
// LOBBY SYSTEM (как в Dota 2, CS:GO)
// ═══════════════════════════════════════════
const roomStates = new Map(); // roomId => { state: 'LOBBY' | 'COUNTDOWN' | 'GAME', startTime, countdownTimer }
const LOBBY_WAIT_TIME = 20000; // 20 секунд ожидание в лобби (ИЗМЕНЕНО с 10 на 20!)
const COUNTDOWN_TIME = 3000; // 3 секунды countdown перед стартом

// ═══════════════════════════════════════════
// SERVER AUTHORITY SETTINGS
// ═══════════════════════════════════════════
const MAX_PLAYER_SPEED = 15.0; // Максимальная скорость игрока (m/s)
const POSITION_CORRECTION_THRESHOLD = 2.0; // Коррекция если рассинхрон > 2 метров
const VALIDATION_ENABLED = true; // Включить серверную валидацию позиций

module.exports = (io) => {
  console.log('🎮 Multiplayer module loaded');

  // ═══════════════════════════════════════════
  // HELPER: Получить свободную точку спавна
  // ═══════════════════════════════════════════
  function getNextSpawnPoint(roomId) {
    if (!roomSpawnPoints.has(roomId)) {
      roomSpawnPoints.set(roomId, new Set());
    }

    const usedSpawns = roomSpawnPoints.get(roomId);

    // Находим первую свободную точку спавна (0-19)
    for (let i = 0; i < MAX_SPAWN_POINTS; i++) {
      if (!usedSpawns.has(i)) {
        usedSpawns.add(i);
        console.log(`[Spawn] 🎯 Assigned spawn point ${i} for room ${roomId}`);
        return i;
      }
    }

    // Если все точки заняты - используем случайную (комната переполнена)
    const randomSpawn = Math.floor(Math.random() * MAX_SPAWN_POINTS);
    console.warn(`[Spawn] ⚠️ All spawn points occupied! Using random: ${randomSpawn}`);
    return randomSpawn;
  }

  // ═══════════════════════════════════════════
  // HELPER: Освободить точку спавна
  // ═══════════════════════════════════════════
  function releaseSpawnPoint(roomId, spawnIndex) {
    if (roomSpawnPoints.has(roomId)) {
      roomSpawnPoints.get(roomId).delete(spawnIndex);
      console.log(`[Spawn] 🔓 Released spawn point ${spawnIndex} in room ${roomId}`);
    }
  }

  // ═══════════════════════════════════════════
  // HELPER: Инициализация лобби для комнаты
  // ═══════════════════════════════════════════
  function initializeRoomLobby(roomId, io) {
    if (!roomStates.has(roomId)) {
      console.log(`[Lobby] 🏁 Creating lobby for room ${roomId}`);

      const roomState = {
        state: 'LOBBY',
        startTime: Date.now(),
        countdownTimer: null,
        playersReady: new Set()
      };

      roomStates.set(roomId, roomState);

      // Через 10 секунд начинаем countdown
      roomState.countdownTimer = setTimeout(() => {
        startCountdown(roomId, io);
      }, LOBBY_WAIT_TIME);

      // Уведомляем всех что лобби создано
      io.to(roomId).emit('lobby_created', {
        waitTime: LOBBY_WAIT_TIME,
        timestamp: Date.now()
      });
    }
  }

  function startCountdown(roomId, io) {
    const roomState = roomStates.get(roomId);
    if (!roomState || roomState.state !== 'LOBBY') return;

    console.log(`[Lobby] ⏱️ Starting countdown for room ${roomId}`);
    roomState.state = 'COUNTDOWN';

    // Отправляем countdown (3 секунды)
    let countdown = 3;
    io.to(roomId).emit('game_countdown', { countdown, timestamp: Date.now() });

    const countdownInterval = setInterval(() => {
      countdown--;
      if (countdown > 0) {
        io.to(roomId).emit('game_countdown', { countdown, timestamp: Date.now() });
      } else {
        clearInterval(countdownInterval);
        startGame(roomId, io);
      }
    }, 1000);
  }

  function startGame(roomId, io) {
    const roomState = roomStates.get(roomId);
    if (!roomState) return;

    console.log(`[Lobby] 🎮 Starting game for room ${roomId}`);
    roomState.state = 'GAME';

    // Собираем всех игроков с их spawn points
    const players = [];
    for (const [socketId, player] of activePlayers.entries()) {
      if (player.roomId === roomId) {
        players.push({
          socketId,
          username: player.username,
          characterClass: player.characterClass,
          spawnIndex: player.spawnIndex
        });
      }
    }

    // Отправляем команду "СПАВНИТЕ ВСЕХ ОДНОВРЕМЕННО!"
    io.to(roomId).emit('game_start', {
      players,
      timestamp: Date.now()
    });

    console.log(`[Lobby] ✅ Game started for ${players.length} players in room ${roomId}`);
  }

  // ═══════════════════════════════════════════
  // HELPER: Валидация и коррекция позиции (SERVER AUTHORITY)
  // ═══════════════════════════════════════════
  function validateAndCorrectPosition(player, newPosition, newVelocity, deltaTime) {
    if (!VALIDATION_ENABLED || !player.position) {
      return { corrected: false, position: newPosition };
    }

    const oldPos = player.position;

    // ═══════ 1. ПРОВЕРКА СКОРОСТИ (ANTI-CHEAT) ═══════
    const distance = Math.sqrt(
      Math.pow(newPosition.x - oldPos.x, 2) +
      Math.pow(newPosition.y - oldPos.y, 2) +
      Math.pow(newPosition.z - oldPos.z, 2)
    );

    const speed = deltaTime > 0 ? distance / deltaTime : 0;

    if (speed > MAX_PLAYER_SPEED) {
      // Скорость слишком высокая - возможен читер или телепорт
      console.warn(`[Authority] ⚠️ ${player.username} speed too high: ${speed.toFixed(2)} m/s (max: ${MAX_PLAYER_SPEED})`);

      // КОРРЕКЦИЯ: Интерполируем позицию с максимальной скоростью
      const maxDistance = MAX_PLAYER_SPEED * deltaTime;
      const direction = {
        x: newPosition.x - oldPos.x,
        y: newPosition.y - oldPos.y,
        z: newPosition.z - oldPos.z
      };
      const dirLength = Math.sqrt(direction.x ** 2 + direction.y ** 2 + direction.z ** 2);

      if (dirLength > 0) {
        const correctedPosition = {
          x: oldPos.x + (direction.x / dirLength) * maxDistance,
          y: oldPos.y + (direction.y / dirLength) * maxDistance,
          z: oldPos.z + (direction.z / dirLength) * maxDistance
        };

        return { corrected: true, position: correctedPosition, reason: 'speed_limit' };
      }
    }

    // ═══════ 2. ПРОВЕРКА ТЕЛЕПОРТАЦИИ ═══════
    if (distance > POSITION_CORRECTION_THRESHOLD * 10) {
      // Слишком большой скачок позиции (>20 метров) - возможен телепорт
      console.warn(`[Authority] ⚠️ ${player.username} teleport detected: ${distance.toFixed(2)}m`);
      // Возвращаем старую позицию (отклоняем телепорт)
      return { corrected: true, position: oldPos, reason: 'teleport_blocked' };
    }

    // Позиция валидна
    return { corrected: false, position: newPosition };
  }

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

        const { roomId, username, characterClass, userId } = parsedData;

        console.log(`[Join Room] ${username} (${socket.id}) joining room ${roomId} as ${characterClass}`);

        // Присоединяемся к Socket.IO room
        socket.join(roomId);

        // Загружаем SPECIAL stats из базы данных
        let playerStats = {
          strength: 5,
          perception: 5,
          endurance: 5,
          wisdom: 5,
          intelligence: 5,
          agility: 5,
          luck: 5
        };

        try {
          const character = await Character.findOne({ userId, isSelected: true });
          if (character && character.stats) {
            playerStats = character.stats;
            console.log(`[Join Room] Loaded stats for ${username}: STR=${playerStats.strength}, AGI=${playerStats.agility}, LUCK=${playerStats.luck}`);
          } else {
            console.log(`[Join Room] Character not found for ${username}, using default stats`);
          }
        } catch (error) {
          console.error(`[Join Room] Failed to load stats for ${username}:`, error.message);
        }

        // КРИТИЧЕСКОЕ: Назначаем уникальную точку спавна для этого игрока
        const spawnIndex = getNextSpawnPoint(roomId);

        // Сохраняем информацию об игроке
        activePlayers.set(socket.id, {
          roomId,
          username,
          characterClass,
          userId,
          stats: playerStats,
          position: { x: 0, y: 0, z: 0 },
          rotation: { x: 0, y: 0, z: 0 },
          animation: 'Idle',
          health: 100,
          maxHealth: 100,
          connected: true,
          joinedAt: Date.now(),
          spawnIndex: spawnIndex  // ВАЖНО: Индекс точки спавна (0-19)
        });

        // Получаем всех игроков в комнате
        const playersInRoom = [];
        for (const [sid, player] of activePlayers.entries()) {
          if (player.roomId === roomId) {
            playersInRoom.push({
              socketId: sid,
              username: player.username,
              characterClass: player.characterClass,
              position: player.position,
              rotation: player.rotation,
              animation: player.animation,
              health: player.health,
              maxHealth: player.maxHealth,
              spawnIndex: player.spawnIndex,  // ВАЖНО: Отправляем индекс точки спавна
              stats: player.stats  // КРИТИЧЕСКОЕ: Отправляем SPECIAL характеристики!
            });
          }
        }

        // Отправляем текущему игроку список всех игроков + его spawnIndex
        socket.emit('room_players', {
          players: playersInRoom,
          yourSocketId: socket.id,
          yourSpawnIndex: spawnIndex  // ВАЖНО: Твой индекс точки спавна
        });

        // Уведомляем других игроков о новом игроке
        // КРИТИЧЕСКОЕ: НЕ отправляем position здесь - игрок еще не заспавнился на клиенте!
        // Клиент отправит реальную позицию через player_update после спавна
        // NetworkPlayer временно заспавнится в (0,1,0), а потом телепортируется при первом player_moved
        socket.to(roomId).emit('player_joined', {
          socketId: socket.id,
          username,
          characterClass,
          // position/rotation НЕ отправляем - будут в первом player_moved
          spawnIndex: spawnIndex,  // ВАЖНО: Индекс точки спавна нового игрока
          stats: playerStats  // КРИТИЧЕСКОЕ: Отправляем SPECIAL характеристики!
        });

        console.log(`✅ ${username} joined room ${roomId}. Total players: ${playersInRoom.length}`);

        // КРИТИЧЕСКОЕ: Таймер запускается только когда 2+ игрока подключились!
        if (playersInRoom.length === 2) {
          console.log(`[Lobby] 👥 2 игрока в комнате - запускаем 20-секундный таймер!`);
          initializeRoomLobby(roomId, io);
        } else if (playersInRoom.length === 1) {
          console.log(`[Lobby] ⏳ Игрок 1 ждет... Таймер начнется когда зайдет 2й игрок`);
        } else {
          console.log(`[Lobby] 👥 Игрок ${playersInRoom.length} присоединился к лобби`);

          // НОВОЕ: Отправляем новому игроку текущее состояние лобби!
          const roomState = roomStates.get(roomId);
          if (roomState) {
            if (roomState.state === 'LOBBY') {
              // Лобби еще в состоянии ожидания - отправляем оставшееся время
              const elapsedTime = Date.now() - roomState.startTime;
              const remainingTime = Math.max(0, LOBBY_WAIT_TIME - elapsedTime);

              console.log(`[Lobby] 📤 Отправляем новому игроку lobby_created с оставшимся временем: ${remainingTime}ms`);
              socket.emit('lobby_created', {
                waitTime: remainingTime,
                timestamp: Date.now()
              });
            } else if (roomState.state === 'COUNTDOWN') {
              // Лобби уже в режиме countdown - отправляем текущий countdown
              // NOTE: Точное значение countdown неизвестно, отправим 3 (сервер переотправит через секунду)
              console.log(`[Lobby] 📤 Отправляем новому игроку game_countdown`);
              socket.emit('game_countdown', {
                countdown: 3,
                timestamp: Date.now()
              });
            } else if (roomState.state === 'GAME') {
              // Игра уже идёт - отправляем game_start
              console.log(`[Lobby] 📤 Отправляем новому игроку game_start (игра уже началась)`);
              socket.emit('game_start', {
                players: playersInRoom,
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
          console.warn(`[Get Room Players] Player ${socket.id} not found in activePlayers`);
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
              position: p.position,
              rotation: p.rotation,
              animation: p.animation,
              health: p.health,
              maxHealth: p.maxHealth,
              spawnIndex: p.spawnIndex,  // ВАЖНО: Индекс точки спавна
              stats: p.stats  // КРИТИЧЕСКОЕ: Отправляем SPECIAL характеристики!
            });
          }
        }

        // Отправляем список игроков
        socket.emit('room_players', {
          players: playersInRoom,
          yourSocketId: socket.id,
          yourSpawnIndex: player.spawnIndex  // ВАЖНО: Твой индекс точки спавна
        });

        console.log(`✅ Sent ${playersInRoom.length} players to ${player.username}`);
      } catch (error) {
        console.error('[Get Room Players] Error:', error);
      }
    });

    // ═══════════════════════════════════════════
    // ОБНОВЛЕНИЕ ПОЗИЦИИ И ДВИЖЕНИЯ
    // ═══════════════════════════════════════════

    socket.on('player_update', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) {
        // Это может быть нормально если игрок ещё не в activePlayers
        // console.warn(`[Player Update] Player ${socket.id} not found in activePlayers`);
        return;
      }

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

      // ═══════ SERVER AUTHORITY: Валидация позиции ═══════
      const now = Date.now();
      const deltaTime = player.lastUpdateTime ? (now - player.lastUpdateTime) / 1000.0 : 0.05; // 50ms по умолчанию
      player.lastUpdateTime = now;

      const validation = validateAndCorrectPosition(
        player,
        parsedData.position,
        parsedData.velocity,
        deltaTime
      );

      // Используем СКОРРЕКТИРОВАННУЮ позицию от сервера
      const authorityPosition = validation.position;

      // Обновляем данные игрока СЕРВЕРНОЙ позицией
      player.position = authorityPosition;
      if (parsedData.rotation) player.rotation = parsedData.rotation;
      if (parsedData.velocity) player.velocity = parsedData.velocity;
      if (parsedData.isGrounded !== undefined) player.isGrounded = parsedData.isGrounded;

      // ДИАГНОСТИКА: Логируем трансформированных игроков
      if (player.isTransformed) {
        console.log(`[Player Update] 🐻 TRANSFORMED ${player.username} pos=(${authorityPosition.x.toFixed(1)}, ${authorityPosition.y.toFixed(1)}, ${authorityPosition.z.toFixed(1)})`);
      }

      // КРИТИЧЕСКОЕ: Используем серверную позицию для broadcast
      const movementUpdate = {
        socketId: socket.id,
        position: authorityPosition, // СЕРВЕРНАЯ ИСТИННАЯ ПОЗИЦИЯ
        rotation: player.rotation,
        velocity: parsedData.velocity || { x: 0, y: 0, z: 0 },
        isGrounded: parsedData.isGrounded || false,
        timestamp: now
      };

      // Отправляем обновление другим игрокам в комнате
      socket.to(player.roomId).emit('player_moved', movementUpdate);

      // ДИАГНОСТИКА: Подтверждаем broadcast для трансформированных
      if (player.isTransformed) {
        console.log(`[Player Update] 📡 Broadcasted player_moved для трансформированного ${player.username}`);
      }

      // Если была коррекция - отправляем клиенту его ИСТИННУЮ позицию
      if (validation.corrected) {
        console.log(`[Authority] 🔧 Correcting ${player.username} position (reason: ${validation.reason})`);
        socket.emit('position_correction', {
          position: authorityPosition,
          reason: validation.reason,
          timestamp: now
        });
      }
    });

    // ═══════════════════════════════════════════
    // АНИМАЦИИ
    // ═══════════════════════════════════════════

    socket.on('player_animation', (data) => {
      // ДИАГНОСТИКА: Логируем ВСЁ что приходит
      console.log(`[Animation] 🔍 RAW EVENT RECEIVED! Type: ${typeof data}`);
      console.log(`[Animation] 🔍 RAW data:`, data);
      console.log(`[Animation] 🔍 RAW data (JSON):`, JSON.stringify(data));

      const player = activePlayers.get(socket.id);
      if (!player) {
        console.warn(`[Animation] Player ${socket.id} not found in activePlayers`);
        return;
      }

      // ВАЖНО: Unity может отправить как строку, так и как объект
      let parsedData = data;
      if (typeof data === 'string') {
        try {
          parsedData = JSON.parse(data);
          console.log('[Animation] ✅ Parsed JSON string to object');
        } catch (e) {
          console.error('[Animation] ❌ Failed to parse JSON:', e.message);
          console.error('[Animation] ❌ Problematic data:', data);
          return;
        }
      }

      const { animation, speed } = parsedData;

      console.log(`[Animation] 🎬 ${player.username} (${socket.id}) animation: ${animation}, speed: ${speed || 1.0}`);

      player.animation = animation;
      player.animationSpeed = speed || 1.0;

      // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Отправляем другим игрокам с правильными полями
      const animationUpdate = {
        socketId: socket.id,
        animation: animation,
        speed: speed || 1.0,
        timestamp: Date.now()
      };

      console.log(`[Animation] 📤 Broadcasting to room ${player.roomId}:`, animationUpdate);

      // Отправляем другим игрокам в комнате
      socket.to(player.roomId).emit('player_animation_changed', animationUpdate);

      console.log(`[Animation] ✅ Animation broadcasted for ${player.username}`);
    });

    // ═══════════════════════════════════════════
    // АТАКА (SERVER AUTHORITY)
    // ═══════════════════════════════════════════

    socket.on('player_attack', (data) => {
      const attacker = activePlayers.get(socket.id);
      if (!attacker) {
        console.warn(`[Attack] Attacker ${socket.id} not found in activePlayers`);
        return;
      }

      // ВАЖНО: Unity может отправить как строку, так и как объект
      let parsedData = data;
      console.log(`[Attack] 🔍 Raw data type: ${typeof data}`);
      console.log(`[Attack] 🔍 Raw data: ${JSON.stringify(data).substring(0, 200)}`);

      if (typeof data === 'string') {
        try {
          parsedData = JSON.parse(data);
          console.log('[Attack] ✅ Parsed JSON string to object');
        } catch (e) {
          console.error('[Attack] ❌ Failed to parse JSON:', e.message);
          return;
        }
      }

      console.log(`[Attack] 🔍 Parsed data: ${JSON.stringify(parsedData).substring(0, 200)}`);

      const { targetType, targetId, attackType, position, direction } = parsedData;

      console.log(`[Attack] 🗡️ ${attacker.username} attacking ${targetType} (ID: ${targetId}) with ${attackType}`);

      // ═══════ ШАГ 1: ВАЛИДАЦИЯ ДИСТАНЦИИ ═══════
      let targetPosition = null;
      let targetObject = null;

      if (targetType === 'player') {
        targetObject = activePlayers.get(targetId);
        if (!targetObject) {
          console.warn(`[Attack] ❌ Target player ${targetId} not found`);
          return;
        }
        targetPosition = targetObject.position;
      } else if (targetType === 'enemy') {
        // Для врагов используем позицию из запроса (клиент знает позицию врага)
        // В будущем можно хранить позиции врагов на сервере
        targetPosition = parsedData.targetPosition || { x: 0, y: 0, z: 0 };
      }

      // Вычисляем дистанцию между атакующим и целью
      const distance = Math.sqrt(
        Math.pow(attacker.position.x - targetPosition.x, 2) +
        Math.pow(attacker.position.y - targetPosition.y, 2) +
        Math.pow(attacker.position.z - targetPosition.z, 2)
      );

      // Проверяем максимальную дистанцию атаки
      const maxAttackRange = attackType === 'melee' ? 3.0 : 20.0; // 3м для ближнего, 20м для дальнего
      if (distance > maxAttackRange) {
        console.warn(`[Attack] ❌ Target too far: ${distance.toFixed(2)}m > ${maxAttackRange}m`);
        socket.emit('attack_failed', {
          reason: 'target_too_far',
          distance: distance,
          maxRange: maxAttackRange
        });
        return;
      }

      // ═══════ ШАГ 2: РАСЧЁТ УРОНА НА СЕРВЕРЕ ═══════
      let baseDamage = 5; // Базовый урон (уменьшен для баланса)
      const stats = attacker.stats;

      // Урон зависит от типа атаки и SPECIAL статов
      // БАЛАНСИРОВКА: Уменьшаем множители чтобы враги не умирали с 1 удара
      if (attackType === 'melee') {
        // Ближняя атака: STR * 0.8 + AGI * 0.2
        baseDamage = (stats.strength * 0.8) + (stats.agility * 0.2) + 3;
      } else if (attackType === 'ranged') {
        // Дальняя атака: PER * 0.6 + AGI * 0.4
        baseDamage = (stats.perception * 0.6) + (stats.agility * 0.4) + 3;
      } else if (attackType === 'magic') {
        // Магия: INT * 0.8 + WIS * 0.2
        baseDamage = (stats.intelligence * 0.8) + (stats.wisdom * 0.2) + 3;
      }

      // Применяем класс персонажа (бонусы)
      const classMultipliers = {
        'Warrior': attackType === 'melee' ? 1.3 : 1.0,
        'Archer': attackType === 'ranged' ? 1.3 : 1.0,
        'Mage': attackType === 'magic' ? 1.3 : 1.0,
        'Rogue': attackType === 'melee' ? 1.15 : 1.1, // Роги хороши и в ближнем и в дальнем
        'Paladin': 1.1 // Паладины универсальны
      };
      const classMultiplier = classMultipliers[attacker.characterClass] || 1.0;
      baseDamage *= classMultiplier;

      // Случайный разброс ±10%
      const randomFactor = 0.9 + Math.random() * 0.2;
      baseDamage *= randomFactor;

      // ═══════ ШАГ 3: КРИТИЧЕСКИЙ УДАР ═══════
      const critChance = Math.min(0.5, stats.luck * 0.02); // Макс 50% крит при 25 LUCK
      const isCritical = Math.random() < critChance;
      let finalDamage = baseDamage;

      if (isCritical) {
        finalDamage *= 2.0; // Критический удар x2
        console.log(`[Attack] 💥 CRITICAL HIT! Damage: ${finalDamage.toFixed(1)}`);
      }

      finalDamage = Math.round(finalDamage);

      console.log(`[Attack] 🎯 ${attacker.username} deals ${finalDamage} damage (base: ${baseDamage.toFixed(1)}, crit: ${isCritical})`);

      // ═══════ ШАГ 4: ПРИМЕНЯЕМ УРОН К ЦЕЛИ ═══════
      if (targetType === 'player' && targetObject) {
        // Урон по игроку
        targetObject.health = Math.max(0, targetObject.health - finalDamage);

        console.log(`[Attack] ${targetObject.username} HP: ${targetObject.health}/${targetObject.maxHealth}`);

        // Отправляем событие получения урона
        io.to(attacker.roomId).emit('player_health_changed', {
          socketId: targetId,
          damage: finalDamage,
          currentHealth: targetObject.health,
          maxHealth: targetObject.maxHealth,
          attackerId: socket.id,
          isCritical: isCritical,
          timestamp: Date.now()
        });

        // Проверяем смерть
        if (targetObject.health <= 0) {
          targetObject.animation = 'Dead';
          io.to(attacker.roomId).emit('player_died', {
            socketId: targetId,
            killerId: socket.id,
            killerUsername: attacker.username,
            timestamp: Date.now()
          });
          console.log(`[Attack] 💀 ${targetObject.username} was killed by ${attacker.username}`);
        }
      } else if (targetType === 'enemy') {
        // Урон по NPC врагу - отправляем клиенту для применения
        io.to(attacker.roomId).emit('enemy_damaged_by_server', {
          enemyId: targetId,
          damage: finalDamage,
          attackerId: socket.id,
          attackerUsername: attacker.username,
          isCritical: isCritical,
          timestamp: Date.now()
        });
      }

      // ═══════ ШАГ 5: BROADCAST АНИМАЦИИ АТАКИ ═══════
      // Отправляем всем игрокам анимацию атаки (визуальный эффект)
      io.to(attacker.roomId).emit('player_attacked', {
        socketId: socket.id,
        attackType: attackType,
        targetType: targetType,
        targetId: targetId,
        damage: finalDamage,
        isCritical: isCritical,
        position: position,
        direction: direction,
        timestamp: Date.now()
      });
    });

    // ═══════════════════════════════════════════
    // ПОЛУЧЕНИЕ УРОНА
    // ═══════════════════════════════════════════

    socket.on('player_damaged', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) return;

      player.health = Math.max(0, data.currentHealth);

      console.log(`[Damage] ${player.username} took ${data.damage} damage. Health: ${player.health}/${player.maxHealth}`);

      // Уведомляем всех игроков
      io.to(player.roomId).emit('player_health_changed', {
        socketId: socket.id,
        damage: data.damage,
        currentHealth: player.health,
        maxHealth: player.maxHealth,
        attackerId: data.attackerId,
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

    // ═══════════════════════════════════════════
    // РЕСПАВН
    // ═══════════════════════════════════════════

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
    // СКИЛЛЫ (ТРАНСФОРМАЦИЯ И ДР.)
    // ═══════════════════════════════════════════

    socket.on('player_skill', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) {
        console.warn(`[Skill] Player ${socket.id} not found in activePlayers`);
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

      const { skillId, targetSocketId, targetPosition, skillType } = parsedData;

      console.log(`[Skill] ⚡ ${player.username} использует скилл ID=${skillId}, тип=${skillType || 'unknown'}`);

      // СПЕЦИАЛЬНАЯ ОБРАБОТКА: Трансформация (например, Bear Form для Paladin)
      if (skillType === 'Transformation' || skillId === 502) { // 502 = Bear Form
        console.log(`[Skill] 🐻 ТРАНСФОРМАЦИЯ! ${player.username} превращается...`);

        // Сохраняем состояние трансформации игрока
        player.isTransformed = true;
        player.transformationSkillId = skillId;
        player.transformationStartTime = Date.now();

        // Рассылаем всем игрокам в комнате (ВКЛЮЧАЯ самого игрока для подтверждения)
        io.to(player.roomId).emit('player_transformed', {
          socketId: socket.id,
          username: player.username,
          skillId: skillId,
          timestamp: Date.now()
        });

        console.log(`[Skill] 📡 Broadcast player_transformed для ${player.username} в комнату ${player.roomId}`);
      }
      // Обычные скиллы (урон, хил и т.д.)
      else {
        // Broadcast использования скилла другим игрокам
        socket.to(player.roomId).emit('player_skill_used', {
          socketId: socket.id,
          username: player.username,
          characterClass: player.characterClass,
          skillId: skillId,
          targetSocketId: targetSocketId || '',
          targetPosition: targetPosition || { x: 0, y: 0, z: 0 },
          timestamp: Date.now()
        });

        console.log(`[Skill] 📡 Broadcast player_skill_used для ${player.username}`);
      }
    });

    // Окончание трансформации (опционально - можно использовать таймер на клиенте)
    socket.on('player_transformation_end', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) return;

      console.log(`[Skill] 🔄 ${player.username} завершает трансформацию`);

      player.isTransformed = false;
      player.transformationSkillId = null;

      // Broadcast окончания трансформации
      io.to(player.roomId).emit('player_transformation_ended', {
        socketId: socket.id,
        username: player.username,
        timestamp: Date.now()
      });
    });

    // ═══════════════════════════════════════════
    // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ (взрывы, ауры, горение, баффы и т.д.)
    // ═══════════════════════════════════════════
    socket.on('visual_effect_spawned', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) {
        console.warn('[Visual Effect] ⚠️ Player not found for socket:', socket.id);
        return;
      }

      // Парсим data если пришла строка
      let parsedData = data;
      if (typeof data === 'string') {
        try {
          parsedData = JSON.parse(data);
        } catch (e) {
          console.error('[Visual Effect] ❌ Failed to parse JSON:', e.message);
          return;
        }
      }

      console.log('[Visual Effect] ✨ Получен эффект:', {
        socketId: socket.id,
        username: player.username,
        type: parsedData.effectType,
        prefab: parsedData.effectPrefabName,
        position: parsedData.position
      });

      // Рассылаем ВСЕМ игрокам в комнате (включая отправителя для других клиентов)
      io.to(player.roomId).emit('visual_effect_spawned', {
        socketId: socket.id,
        effectType: parsedData.effectType,
        effectPrefabName: parsedData.effectPrefabName,
        position: parsedData.position,
        rotation: parsedData.rotation,
        targetSocketId: parsedData.targetSocketId || '',
        duration: parsedData.duration || 0,
        timestamp: Date.now()
      });

      console.log('[Visual Effect] 📡 Эффект разослан всем игрокам в комнате', player.roomId);
    });

    // ═══════════════════════════════════════════
    // LOBBY SYSTEM - CLIENT-SIDE EVENTS
    // ═══════════════════════════════════════════

    // Обработка start_game от клиента (FALLBACK countdown завершился)
    socket.on('start_game', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) {
        console.warn(`[Start Game] Player ${socket.id} not found in activePlayers`);
        return;
      }

      // ВАЖНО: Unity может отправить как строку, так и как объект
      let parsedData = data;
      if (typeof data === 'string') {
        try {
          parsedData = JSON.parse(data);
        } catch (e) {
          console.error('[Start Game] ❌ Failed to parse JSON:', e.message);
          return;
        }
      }

      const { roomId } = parsedData;
      console.log(`[Start Game] 🎮 ${player.username} отправил start_game для комнаты ${roomId}`);

      // КРИТИЧЕСКОЕ: Закрываем комнату для новых игроков (игра началась!)
      const roomState = roomStates.get(roomId);
      if (roomState) {
        roomState.state = 'GAME';
        roomState.canJoin = false;
        console.log(`[Start Game] 🔒 Комната ${roomId} ЗАКРЫТА (canJoin=false, status=in-game)`);
      }

      // Собираем всех игроков с их spawn points
      const players = [];
      for (const [socketId, p] of activePlayers.entries()) {
        if (p.roomId === roomId) {
          players.push({
            socketId,
            username: p.username,
            characterClass: p.characterClass,
            spawnIndex: p.spawnIndex
          });
        }
      }

      // Рассылаем game_start ВСЕМ игрокам в комнате (СИНХРОННЫЙ СПАВН!)
      io.to(roomId).emit('game_start', {
        players,
        timestamp: Date.now()
      });

      console.log(`[Start Game] ✅ Разослан game_start всем ${players.length} игрокам в комнате ${roomId}`);
    });

    // Обработка create_lobby от клиента (FALLBACK лобби запущено)
    socket.on('create_lobby', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) {
        console.warn(`[Create Lobby] Player ${socket.id} not found in activePlayers`);
        return;
      }

      // ВАЖНО: Unity может отправить как строку, так и как объект
      let parsedData = data;
      if (typeof data === 'string') {
        try {
          parsedData = JSON.parse(data);
        } catch (e) {
          console.error('[Create Lobby] ❌ Failed to parse JSON:', e.message);
          return;
        }
      }

      const { roomId, waitTime } = parsedData;
      console.log(`[Create Lobby] 🏁 ${player.username} отправил create_lobby для комнаты ${roomId}, время: ${waitTime}ms`);

      // Рассылаем lobby_created ВСЕМ игрокам в комнате
      io.to(roomId).emit('lobby_created', {
        waitTime: waitTime || LOBBY_WAIT_TIME,
        timestamp: Date.now()
      });

      console.log(`[Create Lobby] ✅ Разослан lobby_created всем игрокам в комнате ${roomId}`);
    });

    // ═══════════════════════════════════════════
    // ОТКЛЮЧЕНИЕ
    // ═══════════════════════════════════════════

    socket.on('disconnect', () => {
      const player = activePlayers.get(socket.id);

      if (player) {
        console.log(`❌ Player disconnected: ${player.username} (${socket.id})`);

        // Освобождаем точку спавна
        releaseSpawnPoint(player.roomId, player.spawnIndex);

        // Уведомляем других игроков
        socket.to(player.roomId).emit('player_left', {
          socketId: socket.id,
          username: player.username
        });

        // Удаляем игрока
        activePlayers.delete(socket.id);
      } else {
        console.log(`❌ Unknown player disconnected: ${socket.id}`);
      }
    });

    // ═══════════════════════════════════════════
    // ПИНГ (ДЛЯ ПРОВЕРКИ СОЕДИНЕНИЯ)
    // ═══════════════════════════════════════════

    socket.on('ping', () => {
      socket.emit('pong', { timestamp: Date.now() });
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
