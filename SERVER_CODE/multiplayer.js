/**
 * Multiplayer Logic - Socket.IO Event Handlers
 * Обрабатывает все real-time события мультиплеера
 */

const Room = require('./models/Room');

// Хранилище активных игроков
const activePlayers = new Map(); // socketId => { roomId, username, characterClass, position, animation }

// Хранилище врагов в комнатах
const roomEnemies = new Map(); // roomId => Map(enemyId => { health, alive, position })

module.exports = (io) => {
  console.log('🎮 Multiplayer module loaded');

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

        // Сохраняем информацию об игроке
        activePlayers.set(socket.id, {
          roomId,
          username,
          characterClass,
          userId,
          position: { x: 0, y: 0, z: 0 },
          rotation: { x: 0, y: 0, z: 0 },
          animation: 'Idle',
          health: 100,
          maxHealth: 100,
          connected: true,
          joinedAt: Date.now()
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
              maxHealth: player.maxHealth
            });
          }
        }

        // Отправляем текущему игроку список всех игроков
        socket.emit('room_players', {
          players: playersInRoom,
          yourSocketId: socket.id
        });

        // Уведомляем других игроков о новом игроке
        socket.to(roomId).emit('player_joined', {
          socketId: socket.id,
          username,
          characterClass,
          position: { x: 0, y: 0, z: 0 },
          rotation: { x: 0, y: 0, z: 0 }
        });

        console.log(`✅ ${username} joined room ${roomId}. Total players: ${playersInRoom.length}`);
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
              maxHealth: p.maxHealth
            });
          }
        }

        // Отправляем список игроков
        socket.emit('room_players', {
          players: playersInRoom,
          yourSocketId: socket.id
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
        velocity: data.velocity,
        isGrounded: data.isGrounded,
        timestamp: Date.now()
      });
    });

    // ═══════════════════════════════════════════
    // АНИМАЦИИ
    // ═══════════════════════════════════════════

    socket.on('player_animation', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) return;

      player.animation = data.animation;
      player.animationSpeed = data.speed || 1.0;

      // Отправляем другим игрокам
      socket.to(player.roomId).emit('player_animation_changed', {
        socketId: socket.id,
        animation: data.animation,
        speed: data.speed || 1.0,
        timestamp: Date.now()
      });
    });

    // ═══════════════════════════════════════════
    // АТАКА
    // ═══════════════════════════════════════════

    socket.on('player_attack', (data) => {
      const player = activePlayers.get(socket.id);
      if (!player) return;

      console.log(`[Attack] ${player.username} attacking ${data.targetType} (ID: ${data.targetId})`);

      // Отправляем всем игрокам в комнате
      io.to(player.roomId).emit('player_attacked', {
        socketId: socket.id,
        attackType: data.attackType || 'melee',
        targetType: data.targetType, // 'player' or 'enemy'
        targetId: data.targetId,
        damage: data.damage,
        position: data.position,
        direction: data.direction,
        skillId: data.skillId,
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
    // ОТКЛЮЧЕНИЕ
    // ═══════════════════════════════════════════

    socket.on('disconnect', () => {
      const player = activePlayers.get(socket.id);

      if (player) {
        console.log(`❌ Player disconnected: ${player.username} (${socket.id})`);

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
