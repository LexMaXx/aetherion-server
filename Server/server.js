/**
 * Aetherion MMO Server
 * Socket.IO multiplayer server for Unity client
 *
 * Features:
 * - REST API для комнат и авторизации (с MongoDB)
 * - WebSocket (Socket.IO) для реального времени
 * - Система лобби и автоматический старт игры
 * - Синхронизация игроков, снарядов, скиллов
 */

const express = require('express');
const http = require('http');
const socketIO = require('socket.io');
const mongoose = require('mongoose');
const cors = require('cors');
const jwt = require('jsonwebtoken');
const bcrypt = require('bcryptjs');

// ===== CONFIGURATION =====

const PORT = process.env.PORT || 3000;
const MONGODB_URI = process.env.MONGODB_URI || 'mongodb://localhost:27017/aetherion';
const JWT_SECRET = process.env.JWT_SECRET || 'your-secret-key-change-in-production';
const LOBBY_WAIT_TIME = 17000; // 17 seconds (14s wait + 3s countdown)

// ===== EXPRESS & SOCKET.IO SETUP =====

const app = express();
const server = http.createServer(app);
const io = socketIO(server, {
    cors: {
        origin: "*",
        methods: ["GET", "POST"]
    },
    pingInterval: 10000,
    pingTimeout: 5000,
    transports: ['websocket', 'polling']
});

app.use(cors());
app.use(express.json());

// ===== MONGODB SCHEMAS =====

const UserSchema = new mongoose.Schema({
    username: { type: String, required: true, unique: true },
    email: { type: String, required: true, unique: true },
    password: { type: String, required: true },
    createdAt: { type: Date, default: Date.now }
});

const CharacterSchema = new mongoose.Schema({
    userId: { type: mongoose.Schema.Types.ObjectId, ref: 'User', required: true },
    characterClass: { type: String, required: true }, // Mage, Warrior, Archer, Rogue, Paladin
    level: { type: Number, default: 1 },
    gold: { type: Number, default: 0 },
    stats: {
        strength: { type: Number, default: 5 },
        perception: { type: Number, default: 5 },
        endurance: { type: Number, default: 5 },
        wisdom: { type: Number, default: 5 },
        intelligence: { type: Number, default: 5 },
        agility: { type: Number, default: 5 },
        luck: { type: Number, default: 5 }
    },
    createdAt: { type: Date, default: Date.now }
});

// Уникальность: один персонаж каждого класса на пользователя
CharacterSchema.index({ userId: 1, characterClass: 1 }, { unique: true });

const User = mongoose.model('User', UserSchema);
const Character = mongoose.model('Character', CharacterSchema);

// ===== IN-MEMORY GAME STATE =====

const rooms = new Map(); // roomId => Room object
const players = new Map(); // socketId => Player object

class Room {
    constructor(roomId, roomName, creatorSocketId, maxPlayers = 20) {
        this.roomId = roomId;
        this.roomName = roomName;
        this.maxPlayers = maxPlayers;
        this.players = []; // Array of Player objects
        this.creatorSocketId = creatorSocketId;
        this.gameState = 'lobby'; // 'lobby', 'countdown', 'playing'
        this.lobbyTimer = null;
        this.countdownTimer = null;
        this.createdAt = new Date();
    }

    addPlayer(player) {
        if (this.players.length >= this.maxPlayers) {
            return false;
        }

        // Assign spawn index
        player.spawnIndex = this.players.length;
        this.players.push(player);

        console.log(`[Room ${this.roomId}] Player ${player.username} joined (${this.players.length}/${this.maxPlayers})`);

        // Start lobby if 2+ players
        if (this.players.length >= 2 && this.gameState === 'lobby' && !this.lobbyTimer) {
            this.startLobby();
        }

        return true;
    }

    removePlayer(socketId) {
        const index = this.players.findIndex(p => p.socketId === socketId);
        if (index !== -1) {
            const player = this.players[index];
            this.players.splice(index, 1);
            console.log(`[Room ${this.roomId}] Player ${player.username} left (${this.players.length}/${this.maxPlayers})`);

            // Stop lobby if < 2 players
            if (this.players.length < 2 && this.lobbyTimer) {
                this.stopLobby();
            }

            return player;
        }
        return null;
    }

    startLobby() {
        console.log(`[Room ${this.roomId}] 🏁 LOBBY STARTED (${this.players.length} players)`);

        // Emit lobby_created to all players in room
        io.to(this.roomId).emit('lobby_created', {
            waitTime: LOBBY_WAIT_TIME
        });

        // Wait 14 seconds, then start countdown
        this.lobbyTimer = setTimeout(() => {
            this.startCountdown();
        }, LOBBY_WAIT_TIME - 3000); // 17s - 3s = 14s
    }

    startCountdown() {
        console.log(`[Room ${this.roomId}] ⏱️ COUNTDOWN STARTED`);
        this.gameState = 'countdown';

        let count = 3;
        const countdownInterval = setInterval(() => {
            console.log(`[Room ${this.roomId}] Countdown: ${count}`);
            io.to(this.roomId).emit('game_countdown', { count });

            count--;
            if (count < 1) {
                clearInterval(countdownInterval);
                this.startGame();
            }
        }, 1000);

        this.countdownTimer = countdownInterval;
    }

    startGame() {
        console.log(`[Room ${this.roomId}] 🎮 GAME STARTED`);
        this.gameState = 'playing';

        // Send game_start with all players
        const playersData = this.players.map(p => ({
            socketId: p.socketId,
            username: p.username,
            characterClass: p.characterClass,
            spawnIndex: p.spawnIndex,
            position: p.position || { x: 0, y: 0, z: 0 },
            stats: p.stats
        }));

        io.to(this.roomId).emit('game_start', {
            players: playersData
        });
    }

    stopLobby() {
        console.log(`[Room ${this.roomId}] ⏹️ LOBBY STOPPED (not enough players)`);
        this.gameState = 'lobby';

        if (this.lobbyTimer) {
            clearTimeout(this.lobbyTimer);
            this.lobbyTimer = null;
        }

        if (this.countdownTimer) {
            clearInterval(this.countdownTimer);
            this.countdownTimer = null;
        }
    }
}

class Player {
    constructor(socketId, username, characterClass, roomId) {
        this.socketId = socketId;
        this.username = username;
        this.characterClass = characterClass;
        this.roomId = roomId;
        this.spawnIndex = 0;
        this.position = { x: 0, y: 0, z: 0 };
        this.rotation = { x: 0, y: 0, z: 0 };
        this.animation = 'Idle';
        this.health = 100;
        this.maxHealth = 100;
        this.stats = {
            strength: 5,
            perception: 5,
            endurance: 5,
            wisdom: 5,
            intelligence: 5,
            agility: 5,
            luck: 5
        };
    }
}

// ===== MIDDLEWARE =====

// JWT authentication middleware for HTTP routes
const authenticateToken = (req, res, next) => {
    const authHeader = req.headers['authorization'];
    const token = authHeader && authHeader.split(' ')[1];

    if (!token) {
        return res.status(401).json({ success: false, message: 'No token provided' });
    }

    jwt.verify(token, JWT_SECRET, (err, user) => {
        if (err) {
            return res.status(403).json({ success: false, message: 'Invalid token' });
        }
        req.user = user;
        next();
    });
};

// Socket.IO authentication middleware
io.use((socket, next) => {
    const token = socket.handshake.query.token;

    if (!token) {
        return next(new Error('Authentication error: No token'));
    }

    jwt.verify(token, JWT_SECRET, (err, decoded) => {
        if (err) {
            return next(new Error('Authentication error: Invalid token'));
        }
        socket.userId = decoded.userId;
        socket.username = decoded.username;
        next();
    });
});

// ===== REST API ENDPOINTS =====

// Health check
app.get('/', (req, res) => {
    res.json({
        success: true,
        message: 'Aetherion Server is running',
        version: '1.0.0',
        rooms: rooms.size,
        players: players.size
    });
});

// Register
app.post('/api/auth/register', async (req, res) => {
    try {
        const { username, email, password } = req.body;

        // Check if user exists
        const existingUser = await User.findOne({ $or: [{ username }, { email }] });
        if (existingUser) {
            return res.status(400).json({
                success: false,
                message: 'Username or email already exists'
            });
        }

        // Hash password
        const hashedPassword = await bcrypt.hash(password, 10);

        // Create user
        const user = new User({
            username,
            email,
            password: hashedPassword
        });

        await user.save();

        // Generate token
        const token = jwt.sign(
            { userId: user._id, username: user.username },
            JWT_SECRET,
            { expiresIn: '30d' }
        );

        res.json({
            success: true,
            token,
            user: {
                id: user._id,
                username: user.username,
                email: user.email
            }
        });
    } catch (error) {
        console.error('Register error:', error);
        res.status(500).json({
            success: false,
            message: 'Server error'
        });
    }
});

// Login
app.post('/api/auth/login', async (req, res) => {
    try {
        const { username, password } = req.body;

        // Find user
        const user = await User.findOne({ username });
        if (!user) {
            return res.status(401).json({
                success: false,
                message: 'Invalid credentials'
            });
        }

        // Check password
        const validPassword = await bcrypt.compare(password, user.password);
        if (!validPassword) {
            return res.status(401).json({
                success: false,
                message: 'Invalid credentials'
            });
        }

        // Generate token
        const token = jwt.sign(
            { userId: user._id, username: user.username },
            JWT_SECRET,
            { expiresIn: '30d' }
        );

        res.json({
            success: true,
            token,
            user: {
                id: user._id,
                username: user.username,
                email: user.email
            }
        });
    } catch (error) {
        console.error('Login error:', error);
        res.status(500).json({
            success: false,
            message: 'Server error'
        });
    }
});

// Verify token
app.get('/api/auth/verify', authenticateToken, (req, res) => {
    res.json({
        success: true,
        user: req.user
    });
});

// Get characters
app.get('/api/character', authenticateToken, async (req, res) => {
    try {
        const characters = await Character.find({ userId: req.user.userId });

        res.json({
            success: true,
            characters: characters.map(c => ({
                id: c._id,
                characterClass: c.characterClass,
                level: c.level,
                gold: c.gold,
                stats: c.stats
            }))
        });
    } catch (error) {
        console.error('Get characters error:', error);
        res.status(500).json({
            success: false,
            message: 'Server error'
        });
    }
});

// Select or create character
app.post('/api/character/select', authenticateToken, async (req, res) => {
    try {
        const { characterClass } = req.body;

        // Find or create character
        let character = await Character.findOne({
            userId: req.user.userId,
            characterClass
        });

        if (!character) {
            character = new Character({
                userId: req.user.userId,
                characterClass
            });
            await character.save();
        }

        res.json({
            success: true,
            character: {
                id: character._id,
                characterClass: character.characterClass,
                level: character.level,
                gold: character.gold,
                stats: character.stats
            }
        });
    } catch (error) {
        console.error('Select character error:', error);
        res.status(500).json({
            success: false,
            message: 'Server error'
        });
    }
});

// Create room
app.post('/api/room/create', authenticateToken, async (req, res) => {
    try {
        const { roomName, maxPlayers, characterClass, username } = req.body;

        const roomId = generateRoomId();

        // Room will be created when first player connects via WebSocket
        // For now just return success
        res.json({
            success: true,
            room: {
                roomId,
                roomName,
                maxPlayers: maxPlayers || 20,
                playerCount: 0
            }
        });
    } catch (error) {
        console.error('Create room error:', error);
        res.status(500).json({
            success: false,
            message: 'Server error'
        });
    }
});

// Join room (REST)
app.post('/api/room/join', authenticateToken, async (req, res) => {
    try {
        const { roomId } = req.body;

        res.json({
            success: true,
            room: {
                roomId,
                roomName: 'Room',
                maxPlayers: 20,
                playerCount: 0
            }
        });
    } catch (error) {
        console.error('Join room error:', error);
        res.status(500).json({
            success: false,
            message: 'Server error'
        });
    }
});

// ===== SOCKET.IO EVENTS =====

io.on('connection', (socket) => {
    console.log(`✅ Socket connected: ${socket.id} (user: ${socket.username})`);

    // Join room
    socket.on('join_room', (data) => {
        try {
            const { roomId, characterClass } = data;
            console.log(`[${socket.id}] Joining room: ${roomId} as ${characterClass}`);

            // Create room if doesn't exist
            if (!rooms.has(roomId)) {
                const room = new Room(roomId, `Room ${roomId}`, socket.id);
                rooms.set(roomId, room);
                console.log(`[Room ${roomId}] Created`);
            }

            const room = rooms.get(roomId);

            // Create player
            const player = new Player(socket.id, socket.username, characterClass, roomId);
            players.set(socket.id, player);

            // Add player to room
            if (!room.addPlayer(player)) {
                socket.emit('error', { message: 'Room is full' });
                return;
            }

            // Join socket.io room
            socket.join(roomId);

            // Send success
            socket.emit('join_room_success', {
                roomId,
                spawnIndex: player.spawnIndex
            });

            // Broadcast player_joined to others
            socket.to(roomId).emit('player_joined', {
                socketId: socket.id,
                username: player.username,
                characterClass: player.characterClass,
                spawnIndex: player.spawnIndex,
                stats: player.stats
            });

            console.log(`[${socket.id}] Successfully joined room ${roomId}`);
        } catch (error) {
            console.error('join_room error:', error);
            socket.emit('error', { message: 'Failed to join room' });
        }
    });

    // Request room players
    socket.on('room_players_request', (data) => {
        try {
            const { roomId } = data;
            const player = players.get(socket.id);

            if (!player || !rooms.has(roomId)) {
                socket.emit('error', { message: 'Room not found' });
                return;
            }

            const room = rooms.get(roomId);
            const playersData = room.players.map(p => ({
                socketId: p.socketId,
                username: p.username,
                characterClass: p.characterClass,
                position: p.position,
                rotation: p.rotation,
                animation: p.animation,
                health: p.health,
                maxHealth: p.maxHealth,
                spawnIndex: p.spawnIndex,
                stats: p.stats
            }));

            socket.emit('room_players', {
                players: playersData,
                yourSocketId: socket.id,
                yourSpawnIndex: player.spawnIndex
            });
        } catch (error) {
            console.error('room_players_request error:', error);
        }
    });

    // Update position
    socket.on('update_position', (data) => {
        const player = players.get(socket.id);
        if (!player) return;

        player.position = data.position;
        player.rotation = data.rotation;

        // Broadcast to others in room
        socket.to(player.roomId).emit('player_moved', {
            socketId: socket.id,
            position: data.position,
            rotation: data.rotation,
            velocity: data.velocity || { x: 0, y: 0, z: 0 },
            isMoving: data.isMoving || false
        });
    });

    // Update animation
    socket.on('update_animation', (data) => {
        const player = players.get(socket.id);
        if (!player) return;

        player.animation = data.animationState;

        socket.to(player.roomId).emit('player_animation_changed', {
            socketId: socket.id,
            animationState: data.animationState
        });
    });

    // Player attacked
    socket.on('player_attacked', (data) => {
        const player = players.get(socket.id);
        if (!player) return;

        socket.to(player.roomId).emit('player_attacked', {
            socketId: socket.id,
            targetId: data.targetId,
            damage: data.damage
        });
    });

    // Player used skill
    socket.on('player_used_skill', (data) => {
        const player = players.get(socket.id);
        if (!player) return;

        socket.to(player.roomId).emit('player_used_skill', {
            socketId: socket.id,
            skillName: data.skillName,
            skillType: data.skillType,
            targetPosition: data.targetPosition,
            timestamp: Date.now()
        });
    });

    // Projectile spawned
    socket.on('projectile_spawned', (data) => {
        const player = players.get(socket.id);
        if (!player) return;

        const parsedData = typeof data === 'string' ? JSON.parse(data) : data;

        console.log(`[Projectile] ${player.username} spawned projectile: skillId=${parsedData.skillId}`);

        // Broadcast to all players in room (including sender for confirmation)
        io.to(player.roomId).emit('projectile_spawned', JSON.stringify({
            socketId: socket.id,
            skillId: parsedData.skillId,
            spawnPosition: parsedData.spawnPosition,
            direction: parsedData.direction,
            targetSocketId: parsedData.targetSocketId || '',
            timestamp: Date.now()
        }));
    });

    // Visual effect spawned (ИСПРАВЛЕНО: передаем все нужные параметры)
    socket.on('visual_effect_spawned', (data) => {
        const player = players.get(socket.id);
        if (!player) return;

        console.log(`[Visual Effect] ${player.username} spawned effect: ${data.effectPrefabName} at (${data.position.x.toFixed(1)}, ${data.position.y.toFixed(1)}, ${data.position.z.toFixed(1)})`);

        // Отправляем визуальный эффект всем игрокам в комнате (кроме отправителя)
        socket.to(player.roomId).emit('visual_effect_spawned', JSON.stringify({
            socketId: socket.id,                    // Кто создал эффект
            effectType: data.effectType,            // Тип эффекта (cast, hit, buff, etc)
            effectPrefabName: data.effectPrefabName,// Имя префаба для поиска в Resources
            position: data.position,                // Позиция эффекта
            rotation: data.rotation,                // Поворот эффекта
            targetSocketId: data.targetSocketId || '',  // Если привязан к игроку
            duration: data.duration || 1.0          // Длительность эффекта
        }));
    });

    // Player transformed
    socket.on('player_transformed', (data) => {
        const player = players.get(socket.id);
        if (!player) return;

        socket.to(player.roomId).emit('player_transformed', {
            socketId: socket.id,
            transformationType: data.transformationType
        });
    });

    // Player transformation ended
    socket.on('player_transformation_ended', (data) => {
        const player = players.get(socket.id);
        if (!player) return;

        socket.to(player.roomId).emit('player_transformation_ended', {
            socketId: socket.id
        });
    });

    // Effect applied (NEW: Status effects, buffs, debuffs synchronization)
    socket.on('effect_applied', (data) => {
        const player = players.get(socket.id);
        if (!player) return;

        console.log(`[Effect] ${player.username} применил эффект ${data.effectType} к ${data.targetSocketId}`);

        // Broadcast effect to all players in room
        io.to(player.roomId).emit('effect_applied', {
            socketId: socket.id,
            casterUsername: player.username,
            targetSocketId: data.targetSocketId,
            effectType: data.effectType,
            duration: data.duration,
            power: data.power,
            tickInterval: data.tickInterval,
            particleEffectPrefabName: data.particleEffectPrefabName,
            timestamp: Date.now()
        });
    });

    // Disconnect
    socket.on('disconnect', () => {
        console.log(`❌ Socket disconnected: ${socket.id}`);

        const player = players.get(socket.id);
        if (player) {
            const room = rooms.get(player.roomId);
            if (room) {
                room.removePlayer(socket.id);

                // Broadcast player_left
                socket.to(player.roomId).emit('player_left', {
                    socketId: socket.id
                });

                // Delete room if empty
                if (room.players.length === 0) {
                    rooms.delete(player.roomId);
                    console.log(`[Room ${player.roomId}] Deleted (empty)`);
                }
            }

            players.delete(socket.id);
        }
    });
});

// ===== HELPER FUNCTIONS =====

function generateRoomId() {
    return require('crypto').randomBytes(16).toString('hex');
}

// ===== DATABASE CONNECTION =====

mongoose.connect(MONGODB_URI, {
    useNewUrlParser: true,
    useUnifiedTopology: true
})
.then(() => {
    console.log('✅ Connected to MongoDB');
})
.catch((err) => {
    console.error('❌ MongoDB connection error:', err);
    // Continue without MongoDB for testing
    console.log('⚠️ Running without database (testing mode)');
});

// ===== START SERVER =====

server.listen(PORT, () => {
    console.log(`🚀 Aetherion Server running on port ${PORT}`);
    console.log(`   REST API: http://localhost:${PORT}`);
    console.log(`   WebSocket: ws://localhost:${PORT}`);
});
