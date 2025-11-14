// models/Party.js
// Модель группы игроков (максимум 5 человек)

const mongoose = require('mongoose');

const partyMemberSchema = new mongoose.Schema({
    userId: {
        type: mongoose.Schema.Types.ObjectId,
        ref: 'User',
        required: true
    },
    username: {
        type: String,
        required: true
    },
    characterClass: {
        type: String,
        required: true
    },
    level: {
        type: Number,
        default: 1
    },
    health: {
        current: { type: Number, default: 100 },
        max: { type: Number, default: 100 }
    },
    mana: {
        current: { type: Number, default: 100 },
        max: { type: Number, default: 100 }
    },
    socketId: String, // Для отправки событий
    joinedAt: {
        type: Date,
        default: Date.now
    }
});

const partySchema = new mongoose.Schema({
    partyId: {
        type: String,
        required: true,
        unique: true
    },
    leaderId: {
        type: mongoose.Schema.Types.ObjectId,
        ref: 'User',
        required: true
    },
    roomId: {
        type: String,
        required: true
    },
    members: {
        type: [partyMemberSchema],
        default: [],
        validate: {
            validator: function(members) {
                return members.length <= 5; // Максимум 5 игроков
            },
            message: 'Party cannot have more than 5 members'
        }
    },
    maxMembers: {
        type: Number,
        default: 5
    },
    createdAt: {
        type: Date,
        default: Date.now
    }
}, {
    timestamps: true
});

// Методы для управления группой
partySchema.methods.addMember = function(memberData) {
    if (this.members.length >= this.maxMembers) {
        throw new Error('Party is full');
    }

    // Проверяем что игрок ещё не в группе
    const exists = this.members.some(m => m.userId.toString() === memberData.userId.toString());
    if (exists) {
        throw new Error('Player already in party');
    }

    this.members.push(memberData);
    return this.save();
};

partySchema.methods.removeMember = function(userId) {
    const memberIndex = this.members.findIndex(m => m.userId.toString() === userId.toString());

    if (memberIndex === -1) {
        throw new Error('Member not found in party');
    }

    this.members.splice(memberIndex, 1);

    // Если это был лидер и остались другие члены - назначаем нового лидера
    if (this.leaderId.toString() === userId.toString() && this.members.length > 0) {
        this.leaderId = this.members[0].userId;
        console.log(`[Party] New leader: ${this.members[0].username}`);
    }

    return this.save();
};

partySchema.methods.updateMemberStats = function(userId, health, mana) {
    const member = this.members.find(m => m.userId.toString() === userId.toString());

    if (!member) {
        throw new Error('Member not found in party');
    }

    if (health) {
        member.health.current = health.current || member.health.current;
        member.health.max = health.max || member.health.max;
    }

    if (mana) {
        member.mana.current = mana.current || member.mana.current;
        member.mana.max = mana.max || member.mana.max;
    }

    return this.save();
};

partySchema.methods.isMember = function(userId) {
    return this.members.some(m => m.userId.toString() === userId.toString());
};

partySchema.methods.isLeader = function(userId) {
    return this.leaderId.toString() === userId.toString();
};

module.exports = mongoose.model('Party', partySchema);
