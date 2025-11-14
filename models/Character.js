const mongoose = require('mongoose');

const CharacterSchema = new mongoose.Schema({
  // Ссылка на аккаунт
  userId: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'User',
    required: true
  },

  // Класс персонажа (используется как уникальный идентификатор)
  // У игрока может быть только ОДИН персонаж каждого класса
  characterClass: {
    type: String,
    required: true,
    enum: ['Warrior', 'Mage', 'Archer', 'Rogue', 'Paladin']
  },

  // Прогресс
  level: {
    type: Number,
    default: 1
  },

  experience: {
    type: Number,
    default: 0
  },

  // Свободные очки характеристик (выдаются при level up)
  availableStatPoints: {
    type: Number,
    default: 0
  },

  gold: {
    type: Number,
    default: 100
  },

  // SPECIAL характеристики (Fallout-style система)
  stats: {
    strength: { type: Number, default: 1, min: 1, max: 10 },
    perception: { type: Number, default: 1, min: 1, max: 10 },
    endurance: { type: Number, default: 1, min: 1, max: 10 },
    wisdom: { type: Number, default: 1, min: 1, max: 10 },
    intelligence: { type: Number, default: 1, min: 1, max: 10 },
    agility: { type: Number, default: 1, min: 1, max: 10 },
    luck: { type: Number, default: 1, min: 1, max: 10 }
  },

  // Здоровье и мана
  health: {
    current: { type: Number, default: 100 },
    max: { type: Number, default: 100 }
  },

  mana: {
    current: { type: Number, default: 50 },
    max: { type: Number, default: 50 }
  },

  // Позиция в мире (для будущего)
  position: {
    x: { type: Number, default: 0 },
    y: { type: Number, default: 0 },
    z: { type: Number, default: 0 },
    scene: { type: String, default: 'StartingZone' }
  },

  // Инвентарь (массив предметов с количеством)
  inventory: [{
    itemName: { type: String, required: true },
    quantity: { type: Number, default: 1, min: 1 }
  }],

  // Экипировка (названия предметов)
  equipment: {
    weapon: { type: String, default: "" },
    armor: { type: String, default: "" },
    helmet: { type: String, default: "" },
    accessory: { type: String, default: "" }
    // Удалено: boots (нет boots слота в UI)
  },

  // Прогресс квестов (пока пустой, потом расширим)
  questProgress: {
    type: Array,
    default: []
  },

  // Временные метки
  createdAt: {
    type: Date,
    default: Date.now
  },

  lastPlayed: {
    type: Date,
    default: Date.now
  }
});

// ВАЖНО: Один игрок может иметь только ОДНОГО персонажа каждого класса
CharacterSchema.index({ userId: 1, characterClass: 1 }, { unique: true });

module.exports = mongoose.model('Character', CharacterSchema);
