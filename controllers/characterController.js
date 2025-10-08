const Character = require('../models/Character');

// Получить всех персонажей игрока
exports.getCharacters = async (req, res) => {
  try {
    const userId = req.user.id;

    const characters = await Character.find({ userId })
      .sort({ lastPlayed: -1 }) // Сортировка по последней игре
      .select('-inventory -questProgress'); // Не отправляем большие данные

    res.json({
      success: true,
      characters: characters.map(char => ({
        id: char._id,
        characterClass: char.characterClass,
        level: char.level,
        experience: char.experience,
        gold: char.gold,
        stats: char.stats,
        health: char.health,
        mana: char.mana,
        equipment: char.equipment,
        createdAt: char.createdAt,
        lastPlayed: char.lastPlayed
      }))
    });

  } catch (error) {
    console.error('Get characters error:', error);
    res.status(500).json({
      success: false,
      message: 'Ошибка загрузки персонажей'
    });
  }
};

// Создать/получить персонажа по классу (если уже есть - вернуть существующий)
exports.selectOrCreateCharacter = async (req, res) => {
  try {
    const userId = req.user.id;
    const { characterClass } = req.body;

    // Проверка класса
    const validClasses = ['Warrior', 'Mage', 'Archer', 'Rogue', 'Paladin'];
    if (!validClasses.includes(characterClass)) {
      return res.status(400).json({
        success: false,
        message: 'Недопустимый класс персонажа'
      });
    }

    // Пытаемся найти существующего персонажа этого класса
    let character = await Character.findOne({ userId, characterClass });

    // Если персонажа нет - создаем нового
    if (!character) {
      const baseStats = getClassBaseStats(characterClass);

      character = new Character({
        userId,
        characterClass,
        stats: baseStats.stats,
        health: baseStats.health,
        mana: baseStats.mana
      });

      await character.save();
    } else {
      // Если персонаж уже есть - обновляем lastPlayed
      character.lastPlayed = Date.now();
      await character.save();
    }

    res.json({
      success: true,
      message: character.createdAt === character.lastPlayed ? 'Персонаж создан!' : 'Персонаж загружен!',
      character: {
        id: character._id,
        characterClass: character.characterClass,
        level: character.level,
        experience: character.experience,
        gold: character.gold,
        stats: character.stats,
        health: character.health,
        mana: character.mana,
        position: character.position,
        equipment: character.equipment,
        createdAt: character.createdAt,
        lastPlayed: character.lastPlayed
      }
    });

  } catch (error) {
    console.error('Select or create character error:', error);
    res.status(500).json({
      success: false,
      message: 'Ошибка выбора персонажа'
    });
  }
};

// Удалить персонажа
exports.deleteCharacter = async (req, res) => {
  try {
    const userId = req.user.id;
    const { characterId } = req.params;

    const character = await Character.findOne({ _id: characterId, userId });

    if (!character) {
      return res.status(404).json({
        success: false,
        message: 'Персонаж не найден'
      });
    }

    await Character.deleteOne({ _id: characterId });

    res.json({
      success: true,
      message: 'Персонаж удален'
    });

  } catch (error) {
    console.error('Delete character error:', error);
    res.status(500).json({
      success: false,
      message: 'Ошибка удаления персонажа'
    });
  }
};


// Получить базовые характеристики класса
function getClassBaseStats(characterClass) {
  const classStats = {
    Warrior: {
      stats: { strength: 15, agility: 8, intelligence: 5, vitality: 15, luck: 7 },
      health: { current: 150, max: 150 },
      mana: { current: 30, max: 30 }
    },
    Mage: {
      stats: { strength: 5, agility: 7, intelligence: 18, vitality: 8, luck: 12 },
      health: { current: 80, max: 80 },
      mana: { current: 150, max: 150 }
    },
    Archer: {
      stats: { strength: 8, agility: 16, intelligence: 7, vitality: 10, luck: 9 },
      health: { current: 100, max: 100 },
      mana: { current: 50, max: 50 }
    },
    Rogue: {
      stats: { strength: 10, agility: 15, intelligence: 8, vitality: 8, luck: 14 },
      health: { current: 90, max: 90 },
      mana: { current: 40, max: 40 }
    },
    Paladin: {
      stats: { strength: 12, agility: 8, intelligence: 10, vitality: 14, luck: 6 },
      health: { current: 130, max: 130 },
      mana: { current: 80, max: 80 }
    }
  };

  return classStats[characterClass] || classStats.Warrior;
}
