// models/Room.js
// Модель комнаты для PvP арены

const mongoose = require('mongoose');

const playerInRoomSchema = new mongoose.Schema({
    userId: { type: mongoose.Schema.Types.ObjectId, ref: 'User', required: true },
    characterClass: { type: String, required: true },
    username: { type: String, required: true },
    level: { type: Number, default: 1 },
    socketId: { type: String }, // Socket.io ID для real-time

    // Позиция в арене
    position: {
        x: { type: Number, default: 0 },
        y: { type: Number, default: 0 },
        z: { type: Number, default: 0 }
    },

    // Ресурсы
    health: {
        current: { type: Number, default: 100 },
        max: { type: Number, default: 100 }
    },
    mana: {
        current: { type: Number, default: 100 },
        max: { type: Number, default: 100 }
    },

    // Статус
    isAlive: { type: Boolean, default: true },
    kills: { type: Number, default: 0 },
    deaths: { type: Number, default: 0 },

    joinedAt: { type: Date, default: Date.now }
});

const roomSchema = new mongoose.Schema({
    roomId: {
        type: String,
        required: true,
        unique: true,
        index: true
    },

    roomName: { type: String, required: true },

    // Хост комнаты (первый игрок)
    hostUserId: { type: mongoose.Schema.Types.ObjectId, ref: 'User' },

    // Настройки комнаты
    maxPlayers: { type: Number, default: 20 },
    isPrivate: { type: Boolean, default: false },
    password: { type: String },

    // Статус комнаты
    status: {
        type: String,
        enum: ['waiting', 'in_progress', 'finished'],
        default: 'waiting'
    },

    // Игроки в комнате
    players: [playerInRoomSchema],

    // Арена настройки
    arenaScene: { type: String, default: 'ArenaScene' },
    gameMode: { type: String, default: 'FreeForAll' }, // FreeForAll, TeamDeathmatch

    // Время
    createdAt: { type: Date, default: Date.now },
    startedAt: { type: Date },
    finishedAt: { type: Date },

    // Статистика
    totalKills: { type: Number, default: 0 },

    // Для автоудаления пустых комнат
    lastActivity: { type: Date, default: Date.now }
}, {
    timestamps: true
});

// Индексы для быстрого поиска
roomSchema.index({ status: 1, isPrivate: 1 });
roomSchema.index({ lastActivity: 1 });

// Метод для добавления игрока
roomSchema.methods.addPlayer = function(playerData) {
    if (this.players.length >= this.maxPlayers) {
        throw new Error('Room is full');
    }

    // Проверяем что игрок уже не в комнате
    const existingPlayer = this.players.find(p => p.userId.toString() === playerData.userId.toString());
    if (existingPlayer) {
        throw new Error('Player already in room');
    }

    this.players.push(playerData);
    this.lastActivity = Date.now();

    // Первый игрок становится хостом
    if (this.players.length === 1) {
        this.hostUserId = playerData.userId;
    }

    return this.save();
};

// Метод для удаления игрока
roomSchema.methods.removePlayer = function(userId) {
    const playerIndex = this.players.findIndex(p => p.userId.toString() === userId.toString());

    if (playerIndex === -1) {
        throw new Error('Player not in room');
    }

    this.players.splice(playerIndex, 1);
    this.lastActivity = Date.now();

    // Если хост вышел - назначаем нового
    if (this.hostUserId && this.hostUserId.toString() === userId.toString() && this.players.length > 0) {
        this.hostUserId = this.players[0].userId;
    }

    return this.save();
};

// Метод для обновления позиции игрока
roomSchema.methods.updatePlayerPosition = function(userId, position) {
    const player = this.players.find(p => p.userId.toString() === userId.toString());

    if (!player) {
        throw new Error('Player not in room');
    }

    player.position = position;
    this.lastActivity = Date.now();

    // Не сохраняем в БД каждый раз (только в памяти для производительности)
    // Позиции синхронизируются через WebSocket
    return player;
};

// Метод для обновления здоровья игрока
roomSchema.methods.updatePlayerHealth = function(userId, health) {
    const player = this.players.find(p => p.userId.toString() === userId.toString());

    if (!player) {
        throw new Error('Player not in room');
    }

    player.health = health;

    // Проверяем смерть
    if (health.current <= 0 && player.isAlive) {
        player.isAlive = false;
        player.deaths += 1;
    }

    this.lastActivity = Date.now();
    return player;
};

// Метод для регистрации убийства
roomSchema.methods.registerKill = function(killerUserId, victimUserId) {
    const killer = this.players.find(p => p.userId.toString() === killerUserId.toString());
    const victim = this.players.find(p => p.userId.toString() === victimUserId.toString());

    if (killer) {
        killer.kills += 1;
    }

    if (victim) {
        victim.deaths += 1;
        victim.isAlive = false;
    }

    this.totalKills += 1;
    this.lastActivity = Date.now();

    return this.save();
};

// Автоудаление старых комнат
roomSchema.statics.cleanupOldRooms = async function() {
    const oneHourAgo = new Date(Date.now() - 60 * 60 * 1000);

    const result = await this.deleteMany({
        $or: [
            { status: 'finished', finishedAt: { $lt: oneHourAgo } },
            { status: 'waiting', players: { $size: 0 }, createdAt: { $lt: oneHourAgo } }
        ]
    });

    return result.deletedCount;
};

const Room = mongoose.model('Room', roomSchema);

module.exports = Room;
