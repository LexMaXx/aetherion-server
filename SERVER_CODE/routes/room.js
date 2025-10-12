// routes/room.js
// REST API для управления комнатами

const express = require('express');
const router = express.Router();
const mongoose = require('mongoose'); // Для конвертации userId в ObjectId
const Room = require('../models/Room');
const auth = require('../middleware/auth'); // Предполагаем что у вас есть JWT middleware
const { v4: uuidv4 } = require('uuid');

/**
 * GET /api/room/list
 * Получить список доступных комнат
 */
router.get('/list', auth, async (req, res) => {
    try {
        const rooms = await Room.find({
            status: { $in: ['waiting', 'in_progress'] },
            isPrivate: false
        })
        .select('roomId roomName hostUserId maxPlayers players status createdAt')
        .sort({ createdAt: -1 })
        .limit(50);

        // Форматируем ответ
        const roomList = rooms.map(room => ({
            roomId: room.roomId,
            roomName: room.roomName,
            currentPlayers: room.players.length,
            maxPlayers: room.maxPlayers,
            status: room.status,
            canJoin: room.players.length < room.maxPlayers && room.status === 'waiting',
            createdAt: room.createdAt
        }));

        res.json({
            success: true,
            rooms: roomList,
            total: roomList.length
        });

    } catch (error) {
        console.error('Error fetching room list:', error);
        res.status(500).json({
            success: false,
            message: 'Failed to fetch rooms'
        });
    }
});

/**
 * POST /api/room/create
 * Создать новую комнату
 */
router.post('/create', auth, async (req, res) => {
    try {
        const { roomName, maxPlayers, isPrivate, password } = req.body;
        const userIdStr = req.user.id; // Из JWT токена (string)

        // Конвертируем userId в ObjectId с обработкой ошибок
        let userId;
        try {
            userId = new mongoose.Types.ObjectId(userIdStr);
        } catch (err) {
            console.error('[Room] Invalid userId format:', userIdStr, err);
            return res.status(400).json({
                success: false,
                message: 'Invalid user ID format'
            });
        }

        // Проверяем что игрок не в другой комнате
        const existingRoom = await Room.findOne({
            'players.userId': userId,
            status: { $in: ['waiting', 'in_progress'] }
        });

        if (existingRoom) {
            return res.status(400).json({
                success: false,
                message: 'You are already in a room',
                roomId: existingRoom.roomId
            });
        }

        // Получаем данные персонажа из запроса
        const { characterClass, username, level } = req.body;

        // Генерируем уникальный ID комнаты
        const roomId = uuidv4();

        // Создаем комнату
        const room = new Room({
            roomId,
            roomName: roomName || `${username}'s Room`,
            hostUserId: userId,
            maxPlayers: maxPlayers || 20,
            isPrivate: isPrivate || false,
            password: password || null,
            players: [{
                userId,
                characterClass,
                username,
                level: level || 1,
                position: { x: 0, y: 0, z: 0 },
                health: { current: 100, max: 100 },
                mana: { current: 100, max: 100 },
                isAlive: true,
                kills: 0,
                deaths: 0
            }]
        });

        await room.save();

        console.log(`[Room] Created: ${roomId} by ${username}`);

        res.json({
            success: true,
            message: 'Room created successfully',
            room: {
                roomId: room.roomId,
                roomName: room.roomName,
                currentPlayers: 1,
                maxPlayers: room.maxPlayers,
                isHost: true
            }
        });

    } catch (error) {
        console.error('Error creating room:', error);
        res.status(500).json({
            success: false,
            message: 'Failed to create room'
        });
    }
});

/**
 * POST /api/room/join
 * Присоединиться к комнате
 */
router.post('/join', auth, async (req, res) => {
    try {
        const { roomId, password } = req.body;
        const userIdStr = req.user.id; // Из JWT токена (string)

        // Конвертируем userId в ObjectId с обработкой ошибок
        let userId;
        try {
            userId = new mongoose.Types.ObjectId(userIdStr);
        } catch (err) {
            console.error('[Room] Invalid userId format:', userIdStr, err);
            return res.status(400).json({
                success: false,
                message: 'Invalid user ID format'
            });
        }

        // Находим комнату
        const room = await Room.findOne({ roomId });

        if (!room) {
            return res.status(404).json({
                success: false,
                message: 'Room not found'
            });
        }

        // Проверки
        if (room.players.length >= room.maxPlayers) {
            return res.status(400).json({
                success: false,
                message: 'Room is full'
            });
        }

        if (room.isPrivate && room.password !== password) {
            return res.status(403).json({
                success: false,
                message: 'Incorrect password'
            });
        }

        if (room.status !== 'waiting') {
            return res.status(400).json({
                success: false,
                message: 'Game already started'
            });
        }

        // Проверяем что игрок не в другой комнате
        const existingRoom = await Room.findOne({
            'players.userId': userId,
            status: { $in: ['waiting', 'in_progress'] }
        });

        if (existingRoom && existingRoom.roomId !== roomId) {
            return res.status(400).json({
                success: false,
                message: 'You are already in another room'
            });
        }

        // Получаем данные персонажа
        const { characterClass, username, level } = req.body;

        // Добавляем игрока
        await room.addPlayer({
            userId,
            characterClass,
            username,
            level: level || 1,
            position: { x: 0, y: 0, z: 0 },
            health: { current: 100, max: 100 },
            mana: { current: 100, max: 100 },
            isAlive: true,
            kills: 0,
            deaths: 0
        });

        console.log(`[Room] ${username} joined room ${roomId}`);

        res.json({
            success: true,
            message: 'Joined room successfully',
            room: {
                roomId: room.roomId,
                roomName: room.roomName,
                currentPlayers: room.players.length,
                maxPlayers: room.maxPlayers,
                isHost: room.hostUserId.toString() === userId
            }
        });

    } catch (error) {
        console.error('Error joining room:', error);
        res.status(500).json({
            success: false,
            message: error.message || 'Failed to join room'
        });
    }
});

/**
 * POST /api/room/leave
 * Покинуть комнату
 */
router.post('/leave', auth, async (req, res) => {
    try {
        const { roomId } = req.body;
        const userIdStr = req.user.id;
        let userId;
        try {
            userId = new mongoose.Types.ObjectId(userIdStr);
        } catch (err) {
            console.error('[Room] Invalid userId format:', userIdStr, err);
            return res.status(400).json({
                success: false,
                message: 'Invalid user ID format'
            });
        }

        const room = await Room.findOne({ roomId });

        if (!room) {
            return res.status(404).json({
                success: false,
                message: 'Room not found'
            });
        }

        await room.removePlayer(userId);

        // Если комната пустая - удаляем её
        if (room.players.length === 0) {
            await Room.deleteOne({ roomId });
            console.log(`[Room] Deleted empty room ${roomId}`);
        }

        res.json({
            success: true,
            message: 'Left room successfully'
        });

    } catch (error) {
        console.error('Error leaving room:', error);
        res.status(500).json({
            success: false,
            message: error.message || 'Failed to leave room'
        });
    }
});

/**
 * GET /api/room/:roomId
 * Получить информацию о комнате
 */
router.get('/:roomId', auth, async (req, res) => {
    try {
        const { roomId } = req.params;

        const room = await Room.findOne({ roomId })
            .populate('hostUserId', 'username');

        if (!room) {
            return res.status(404).json({
                success: false,
                message: 'Room not found'
            });
        }

        res.json({
            success: true,
            room: {
                roomId: room.roomId,
                roomName: room.roomName,
                host: room.hostUserId ? room.hostUserId.username : 'Unknown',
                currentPlayers: room.players.length,
                maxPlayers: room.maxPlayers,
                status: room.status,
                players: room.players.map(p => ({
                    username: p.username,
                    characterClass: p.characterClass,
                    level: p.level,
                    isAlive: p.isAlive,
                    kills: p.kills,
                    deaths: p.deaths
                }))
            }
        });

    } catch (error) {
        console.error('Error fetching room info:', error);
        res.status(500).json({
            success: false,
            message: 'Failed to fetch room info'
        });
    }
});

/**
 * POST /api/room/start
 * Начать игру (только хост)
 */
router.post('/start', auth, async (req, res) => {
    try {
        const { roomId } = req.body;
        const userIdStr = req.user.id;
        let userId;
        try {
            userId = new mongoose.Types.ObjectId(userIdStr);
        } catch (err) {
            console.error('[Room] Invalid userId format:', userIdStr, err);
            return res.status(400).json({
                success: false,
                message: 'Invalid user ID format'
            });
        }

        const room = await Room.findOne({ roomId });

        if (!room) {
            return res.status(404).json({
                success: false,
                message: 'Room not found'
            });
        }

        // Только хост может начать игру
        if (room.hostUserId.toString() !== userId) {
            return res.status(403).json({
                success: false,
                message: 'Only host can start the game'
            });
        }

        if (room.players.length < 2) {
            return res.status(400).json({
                success: false,
                message: 'Need at least 2 players to start'
            });
        }

        room.status = 'in_progress';
        room.startedAt = Date.now();
        await room.save();

        console.log(`[Room] Game started in room ${roomId}`);

        res.json({
            success: true,
            message: 'Game started'
        });

    } catch (error) {
        console.error('Error starting game:', error);
        res.status(500).json({
            success: false,
            message: 'Failed to start game'
        });
    }
});

module.exports = router;
