const mongoose = require('mongoose');

const characterSchema = new mongoose.Schema({
    userId: {
        type: mongoose.Schema.Types.ObjectId,
        ref: 'User',
        required: true
    },
    name: {
        type: String,
        required: true,
        trim: true
    },
    class: {
        type: String,
        required: true,
        enum: ['Warrior', 'Mage', 'Archer', 'Rogue', 'Paladin']
    },
    level: {
        type: Number,
        default: 1
    },
    experience: {
        type: Number,
        default: 0
    },
    stats: {
        strength: { type: Number, default: 5 },
        perception: { type: Number, default: 5 },
        endurance: { type: Number, default: 5 },
        wisdom: { type: Number, default: 5 },
        intelligence: { type: Number, default: 5 },
        agility: { type: Number, default: 5 },
        luck: { type: Number, default: 5 }
    },
    isSelected: {
        type: Boolean,
        default: false
    },
    createdAt: {
        type: Date,
        default: Date.now
    }
});

module.exports = mongoose.model('Character', characterSchema);
