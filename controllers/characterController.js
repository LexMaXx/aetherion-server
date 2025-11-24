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

    console.log('[saveProgress] 📥 Получен запрос:', {
      userId,
      characterClass,
      leveling: leveling
    });

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

    console.log('[saveProgress] 📊 ДО обновления:', {
      level: character.level,
      experience: character.experience,
      availableStatPoints: character.availableStatPoints
    });

    // Обновляем данные
    character.level = leveling.level;
    character.experience = leveling.experience;
    character.availableStatPoints = leveling.availableStatPoints;

    console.log('[saveProgress] 📊 ПОСЛЕ обновления:', {
      level: character.level,
      experience: character.experience,
      availableStatPoints: character.availableStatPoints
    });

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

    console.log('[saveProgress] ✅ Сохранено в MongoDB:', {
      level: character.level,
      experience: character.experience,
      availableStatPoints: character.availableStatPoints
    });

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

    console.log('[loadProgress] 📤 Отправка данных клиенту:', {
      characterClass,
      level: character.level,
      experience: character.experience,
      availableStatPoints: character.availableStatPoints
    });

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
// ВАЖНО: Эти значения должны соответствовать Unity ClassStatsPreset файлам!
// (Assets/Resources/ClassStats/*.asset)
// Сумма SPECIAL характеристик = 15 (баланс)
function getClassBaseStats(characterClass) {
  const classStats = {
    Warrior: {
      stats: { strength: 4, perception: 2, endurance: 3, wisdom: 1, intelligence: 1, agility: 3, luck: 1 }, // Сумма = 15
      health: { current: 100, max: 100 },
      mana: { current: 50, max: 50 }
    },
    Mage: {
      stats: { strength: 1, perception: 3, endurance: 1, wisdom: 3, intelligence: 3, agility: 3, luck: 1 }, // Сумма = 15
      health: { current: 100, max: 100 },
      mana: { current: 50, max: 50 }
    },
    Archer: {
      stats: { strength: 4, perception: 3, endurance: 1, wisdom: 1, intelligence: 1, agility: 3, luck: 2 }, // Сумма = 15
      health: { current: 100, max: 100 },
      mana: { current: 50, max: 50 }
    },
    Rogue: {
      stats: { strength: 1, perception: 2, endurance: 2, wisdom: 3, intelligence: 3, agility: 3, luck: 1 }, // Сумма = 15
      health: { current: 100, max: 100 },
      mana: { current: 50, max: 50 }
    },
    Paladin: {
      stats: { strength: 2, perception: 2, endurance: 4, wisdom: 2, intelligence: 1, agility: 3, luck: 1 }, // Сумма = 15
      health: { current: 100, max: 100 },
      mana: { current: 50, max: 50 }
    }
  };

  return classStats[characterClass] || classStats.Warrior;
}
