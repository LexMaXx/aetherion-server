/**
 * Multiplayer Logic - Socket.IO Event Handlers
 * Обрабатывает все real-time события мультиплеера
 */

const Room = require('./models/Room');
const Character = require('./models/Character');

// Хранилище активных игроков
const activePlayers = new Map(); // socketId => { roomId, username, characterClass, position, animation, stats, userId }

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

      // Обновляем данные игрока
      if (parsedData.position) player.position = parsedData.position;
      if (parsedData.rotation) player.rotation = parsedData.rotation;
      if (parsedData.velocity) player.velocity = parsedData.velocity;
      if (parsedData.isGrounded !== undefined) player.isGrounded = parsedData.isGrounded;

      // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Используем parsedData.velocity вместо data.velocity
      const movementUpdate = {
        socketId: socket.id,
        position: player.position,
        rotation: player.rotation,
        velocity: parsedData.velocity || { x: 0, y: 0, z: 0 },
        isGrounded: parsedData.isGrounded || false,
        timestamp: Date.now()
      };

      // Отправляем обновление другим игрокам в комнате
      socket.to(player.roomId).emit('player_moved', movementUpdate);
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
