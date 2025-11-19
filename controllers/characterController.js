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

    console.log('[Select/Create Character] 📨 Запрос:', { userId, characterClass });

    // Проверка класса
    const validClasses = ['Warrior', 'Mage', 'Archer', 'Rogue', 'Paladin'];
    if (!characterClass) {
      console.log('[Select/Create Character] ❌ characterClass отсутствует');
      return res.status(400).json({
        success: false,
        message: 'Класс персонажа обязателен'
      });
    }

    if (!validClasses.includes(characterClass)) {
      console.log('[Select/Create Character] ❌ Недопустимый класс:', characterClass);
      return res.status(400).json({
        success: false,
        message: 'Недопустимый класс персонажа'
      });
    }

    // Пытаемся найти существующего персонажа этого класса
    let character = await Character.findOne({ userId, characterClass });

    // Если персонажа нет - создаем нового
    if (!character) {
      console.log('[Select/Create Character] ✨ Персонаж не найден, создаем нового');
      const baseStats = getClassBaseStats(characterClass);

      character = new Character({
        userId,
        characterClass,
        stats: baseStats.stats,
        health: baseStats.health,
        mana: baseStats.mana
      });

      await character.save();
      console.log('[Select/Create Character] ✅ Персонаж создан:', character._id);
    } else {
      // Если персонаж уже есть - обновляем lastPlayed
      console.log('[Select/Create Character] 📖 Персонаж найден, обновляем lastPlayed');
      character.lastPlayed = Date.now();
      await character.save();
      console.log('[Select/Create Character] ✅ Персонаж загружен:', character._id);
    }

    const isNew = Math.abs(new Date(character.createdAt) - new Date(character.lastPlayed)) < 1000;

    res.json({
      success: true,
      message: isNew ? 'Персонаж создан!' : 'Персонаж загружен!',
      character: {
        _id: character._id.toString(), // Unity JsonUtility нужно поле _id
        id: character._id.toString(),   // Для совместимости
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


// Сохранить прогресс прокачки персонажа
exports.saveProgress = async (req, res) => {
  try {
    const userId = req.user.id;
    const { characterClass, stats, leveling } = req.body;

    // Валидация
    if (!characterClass || !stats || !leveling) {
      return res.status(400).json({
        success: false,
        message: 'Отсутствуют обязательные поля'
      });
    }

    // Находим персонажа
    const character = await Character.findOne({ userId, characterClass });

    if (!character) {
      return res.status(404).json({
        success: false,
        message: 'Персонаж не найден'
      });
    }

    // Обновляем данные
    character.level = leveling.level;
    character.experience = leveling.experience;
    character.availableStatPoints = leveling.availableStatPoints;

    // Обновляем SPECIAL stats
    character.stats.strength = stats.strength;
    character.stats.perception = stats.perception;
    character.stats.endurance = stats.endurance;
    character.stats.wisdom = stats.wisdom;
    character.stats.intelligence = stats.intelligence;
    character.stats.agility = stats.agility;
    character.stats.luck = stats.luck;

    character.lastPlayed = Date.now();
    await character.save();

    res.json({
      success: true,
      message: 'Прогресс сохранен'
    });

  } catch (error) {
    console.error('Save progress error:', error);
    res.status(500).json({
      success: false,
      message: 'Ошибка сохранения прогресса'
    });
  }
};

// Загрузить прогресс прокачки персонажа
exports.loadProgress = async (req, res) => {
  try {
    const userId = req.user.id;
    const { characterClass } = req.query;

    if (!characterClass) {
      return res.status(400).json({
        success: false,
        message: 'Не указан класс персонажа'
      });
    }

    const character = await Character.findOne({ userId, characterClass });

    if (!character) {
      return res.status(404).json({
        success: false,
        message: 'Персонаж не найден'
      });
    }

    res.json({
      success: true,
      stats: {
        strength: character.stats.strength,
        perception: character.stats.perception,
        endurance: character.stats.endurance,
        wisdom: character.stats.wisdom,
        intelligence: character.stats.intelligence,
        agility: character.stats.agility,
        luck: character.stats.luck
      },
      leveling: {
        level: character.level,
        experience: character.experience,
        availableStatPoints: character.availableStatPoints
      }
    });

  } catch (error) {
    console.error('Load progress error:', error);
    res.status(500).json({
      success: false,
      message: 'Ошибка загрузки прогресса'
    });
  }
};

// Получить базовые характеристики класса
function getClassBaseStats(characterClass) {
  const classStats = {
    Warrior: {
      stats: { strength: 7, perception: 5, endurance: 8, wisdom: 4, intelligence: 3, agility: 6, luck: 5 },
      health: { current: 150, max: 150 },
      mana: { current: 30, max: 30 }
    },
    Mage: {
      stats: { strength: 2, perception: 6, endurance: 3, wisdom: 8, intelligence: 9, agility: 4, luck: 6 },
      health: { current: 80, max: 80 },
      mana: { current: 150, max: 150 }
    },
    Archer: {
      stats: { strength: 5, perception: 9, endurance: 5, wisdom: 4, intelligence: 4, agility: 8, luck: 5 },
      health: { current: 100, max: 100 },
      mana: { current: 50, max: 50 }
    },
    Rogue: {
      stats: { strength: 5, perception: 7, endurance: 4, wisdom: 4, intelligence: 5, agility: 9, luck: 8 },
      health: { current: 90, max: 90 },
      mana: { current: 40, max: 40 }
    },
    Paladin: {
      stats: { strength: 7, perception: 5, endurance: 7, wisdom: 6, intelligence: 5, agility: 5, luck: 4 },
      health: { current: 130, max: 130 },
      mana: { current: 80, max: 80 }
    }
  };

  return classStats[characterClass] || classStats.Warrior;
}
