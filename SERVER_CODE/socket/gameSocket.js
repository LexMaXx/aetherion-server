// socket/gameSocket.js
// WebSocket сервер для real-time синхронизации игроков

const Room = require('../models/Room');
const jwt = require('jsonwebtoken');

// Хранилище активных игроков в памяти (для быстрого доступа)
const activePlayers = new Map(); // socketId -> playerData

// Tick rate для синхронизации (20 обновлений в секунду)
const TICK_RATE = 50; // ms (20 ticks/sec)
const POSITION_SYNC_INTERVAL = 100; // ms (10 Hz для позиций)

/**
 * Инициализация WebSocket сервера
 */
function initializeGameSocket(io) {
    console.log('[Socket.io] Game Socket initialized');

    // Middleware для аутентификации WebSocket
    io.use(async (socket, next) => {
        try {
            const token = socket.handshake.auth.token;

            if (!token) {
                return next(new Error('Authentication token missing'));
            }

            // Верифицируем JWT токен
            const decoded = jwt.verify(token, process.env.JWT_SECRET || 'your-secret-key');
            socket.userId = decoded.id;
            socket.username = decoded.username;

            next();
        } catch (error) {
            console.error('[Socket.io] Auth error:', error.message);
            next(new Error('Authentication failed'));
        }
    });

    // Подключение клиента
    io.on('connection', (socket) => {
        console.log(`[Socket.io] Client connected: ${socket.id} (User: ${socket.username})`);

        // Присоединение к комнате
        socket.on('join_room', async (data) => {
            try {
                const { roomId, characterClass, level } = data;

                // Находим комнату в БД
                const room = await Room.findOne({ roomId });

                if (!room) {
                    socket.emit('error', { message: 'Room not found' });
                    return;
                }

                // Обновляем socketId игрока в комнате
                const player = room.players.find(p => p.userId.toString() === socket.userId);

                if (!player) {
                    socket.emit('error', { message: 'Player not in room' });
                    return;
                }

                player.socketId = socket.id;
                await room.save();

                // Присоединяемся к Socket.io room
                socket.join(roomId);
                socket.currentRoom = roomId;

                // Сохраняем данные игрока в памяти
                activePlayers.set(socket.id, {
                    userId: socket.userId,
                    username: socket.username,
                    characterClass,
                    level,
                    roomId,
                    position: player.position,
                    rotation: { x: 0, y: 0, z: 0, w: 1 },
                    health: player.health,
                    mana: player.mana,
                    isAlive: player.isAlive,
                    lastUpdate: Date.now()
                });

                // Отправляем текущее состояние всех игроков в комнате
                const roomPlayers = room.players
                    .filter(p => p.socketId)
                    .map(p => {
                        const activePlayer = activePlayers.get(p.socketId);
                        return {
                            socketId: p.socketId,
                            userId: p.userId,
                            username: p.username,
                            characterClass: p.characterClass,
                            level: p.level,
                            position: activePlayer ? activePlayer.position : p.position,
                            rotation: activePlayer ? activePlayer.rotation : { x: 0, y: 0, z: 0, w: 1 },
                            health: p.health,
                            mana: p.mana,
                            isAlive: p.isAlive,
                            kills: p.kills,
                            deaths: p.deaths
                        };
                    });

                // Отправляем клиенту список всех игроков
                socket.emit('room_state', {
                    roomId,
                    players: roomPlayers,
                    status: room.status
                });

                // Уведомляем других игроков о новом подключении
                socket.to(roomId).emit('player_joined', {
                    socketId: socket.id,
                    userId: socket.userId,
                    username: socket.username,
                    characterClass,
                    level,
                    position: player.position,
                    rotation: { x: 0, y: 0, z: 0, w: 1 },
                    health: player.health,
                    mana: player.mana,
                    isAlive: true
                });

                console.log(`[Socket.io] ${socket.username} joined room ${roomId}`);

            } catch (error) {
                console.error('[Socket.io] Error joining room:', error);
                socket.emit('error', { message: 'Failed to join room' });
            }
        });

        // Обновление позиции игрока
        socket.on('update_position', (data) => {
            try {
                const player = activePlayers.get(socket.id);

                if (!player) return;

                // Обновляем позицию в памяти
                player.position = data.position;
                player.rotation = data.rotation;
                player.lastUpdate = Date.now();

                // Рассылаем позицию другим игрокам (НЕ включая отправителя)
                socket.to(player.roomId).emit('player_moved', {
                    socketId: socket.id,
                    position: data.position,
                    rotation: data.rotation
                });

            } catch (error) {
                console.error('[Socket.io] Error updating position:', error);
            }
        });

        // Обновление анимации
        socket.on('update_animation', (data) => {
            try {
                const player = activePlayers.get(socket.id);
                if (!player) return;

                // Рассылаем анимацию другим игрокам
                socket.to(player.roomId).emit('player_animation', {
                    socketId: socket.id,
                    animationState: data.animationState,
                    animationParams: data.animationParams
                });

            } catch (error) {
                console.error('[Socket.io] Error updating animation:', error);
            }
        });

        // Атака игрока
        socket.on('player_attack', async (data) => {
            try {
                const player = activePlayers.get(socket.id);
                if (!player || !player.isAlive) return;

                const { targetSocketId, damage, attackType } = data;

                // Рассылаем событие атаки всем в комнате
                io.to(player.roomId).emit('player_attacked', {
                    attackerSocketId: socket.id,
                    targetSocketId,
                    damage,
                    attackType,
                    timestamp: Date.now()
                });

                // Если есть цель - применяем урон
                if (targetSocketId) {
                    const targetPlayer = activePlayers.get(targetSocketId);

                    if (targetPlayer && targetPlayer.isAlive) {
                        targetPlayer.health.current = Math.max(0, targetPlayer.health.current - damage);

                        // Отправляем обновление HP цели
                        io.to(player.roomId).emit('player_health_update', {
                            socketId: targetSocketId,
                            health: targetPlayer.health
                        });

                        // Проверяем смерть
                        if (targetPlayer.health.current <= 0 && targetPlayer.isAlive) {
                            targetPlayer.isAlive = false;

                            // Обновляем статистику в БД
                            const room = await Room.findOne({ roomId: player.roomId });
                            if (room) {
                                await room.registerKill(socket.userId, targetPlayer.userId);
                            }

                            // Уведомляем о смерти
                            io.to(player.roomId).emit('player_died', {
                                socketId: targetSocketId,
                                killerSocketId: socket.id,
                                killerUsername: socket.username
                            });

                            console.log(`[Combat] ${socket.username} killed ${targetPlayer.username}`);
                        }
                    }
                }

            } catch (error) {
                console.error('[Socket.io] Error processing attack:', error);
            }
        });

        // Использование скилла
        socket.on('player_skill', (data) => {
            try {
                const player = activePlayers.get(socket.id);
                if (!player || !player.isAlive) return;

                // Рассылаем событие скилла всем в комнате (включая информацию об анимации)
                io.to(player.roomId).emit('player_used_skill', {
                    socketId: socket.id,
                    skillId: data.skillId,
                    targetPosition: data.targetPosition,
                    targetSocketId: data.targetSocketId,
                    skillType: data.skillType || '', // Тип скилла (Damage, Heal, Transformation и т.д.)
                    animationTrigger: data.animationTrigger || '', // Триггер анимации
                    animationSpeed: data.animationSpeed || 1.0, // Скорость анимации
                    castTime: data.castTime || 0, // Время каста для задержки создания снаряда
                    timestamp: Date.now()
                });

                console.log(`[Socket.io] ${socket.username} использовал скилл ${data.skillId} (анимация: ${data.animationTrigger}, castTime: ${data.castTime || 0}с)`);

            } catch (error) {
                console.error('[Socket.io] Error processing skill:', error);
            }
        });

        // Обновление здоровья
        socket.on('update_health', (data) => {
            try {
                const player = activePlayers.get(socket.id);
                if (!player) return;

                player.health = data.health;

                // Рассылаем обновление HP
                socket.to(player.roomId).emit('player_health_update', {
                    socketId: socket.id,
                    health: data.health
                });

            } catch (error) {
                console.error('[Socket.io] Error updating health:', error);
            }
        });

        // Обновление маны
        socket.on('update_mana', (data) => {
            try {
                const player = activePlayers.get(socket.id);
                if (!player) return;

                player.mana = data.mana;

                // Рассылаем обновление MP
                socket.to(player.roomId).emit('player_mana_update', {
                    socketId: socket.id,
                    mana: data.mana
                });

            } catch (error) {
                console.error('[Socket.io] Error updating mana:', error);
            }
        });

        // Респавн игрока
        socket.on('player_respawn', async (data) => {
            try {
                const player = activePlayers.get(socket.id);
                if (!player) return;

                player.isAlive = true;
                player.health = { current: player.health.max, max: player.health.max };
                player.position = data.position;

                // Уведомляем всех о респавне
                io.to(player.roomId).emit('player_respawned', {
                    socketId: socket.id,
                    position: data.position,
                    health: player.health
                });

                console.log(`[Combat] ${socket.username} respawned`);

            } catch (error) {
                console.error('[Socket.io] Error respawning:', error);
            }
        });

        // Отключение клиента
        socket.on('disconnect', async () => {
            try {
                const player = activePlayers.get(socket.id);

                if (player) {
                    console.log(`[Socket.io] ${socket.username} disconnected from room ${player.roomId}`);

                    // Уведомляем других игроков
                    socket.to(player.roomId).emit('player_left', {
                        socketId: socket.id,
                        username: socket.username
                    });

                    // Удаляем из активных игроков
                    activePlayers.delete(socket.id);

                    // Обновляем в БД (убираем socketId)
                    const room = await Room.findOne({ roomId: player.roomId });
                    if (room) {
                        const dbPlayer = room.players.find(p => p.userId.toString() === socket.userId);
                        if (dbPlayer) {
                            dbPlayer.socketId = null;
                            await room.save();
                        }
                    }
                }

            } catch (error) {
                console.error('[Socket.io] Error on disconnect:', error);
            }
        });

        // Ping/Pong для проверки соединения
        socket.on('ping', () => {
            socket.emit('pong', { timestamp: Date.now() });
        });
    });

    // Автоочистка неактивных игроков каждую минуту
    setInterval(() => {
        const now = Date.now();
        const timeout = 60000; // 1 минута

        for (const [socketId, player] of activePlayers.entries()) {
            if (now - player.lastUpdate > timeout) {
                console.log(`[Socket.io] Removing inactive player: ${player.username}`);
                activePlayers.delete(socketId);
            }
        }
    }, 60000);

    console.log('[Socket.io] Game Socket ready');
}

module.exports = { initializeGameSocket };
