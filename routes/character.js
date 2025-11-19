const express = require('express');
const router = express.Router();
const auth = require('../middleware/auth');
const Character = require('../models/Character');
const characterController = require('../controllers/characterController');

/**
 * GET /api/character
 * Получить всех персонажей текущего пользователя
 */
router.get('/', auth, async (req, res) => {
    try {
        const characters = await Character.find({ userId: req.user.id });
        res.json({
            success: true,
            characters
        });
    } catch (error) {
        console.error('Get characters error:', error);
        res.status(500).json({
            success: false,
            message: 'Ошибка при получении персонажей'
        });
    }
});

/**
 * POST /api/character
 * Создать нового персонажа
 */
router.post('/', auth, async (req, res) => {
    try {
        const { class: characterClass } = req.body;

        console.log('[Create Character] 📨 Запрос:', { characterClass, userId: req.user.id });

        // Валидация
        if (!characterClass) {
            console.log('[Create Character] ❌ Валидация: class отсутствует');
            return res.status(400).json({
                success: false,
                message: 'Класс персонажа обязателен'
            });
        }

        // Проверка лимита персонажей (макс 5)
        const existingCharacters = await Character.countDocuments({ userId: req.user.id });
        console.log('[Create Character] 📊 Существующих персонажей:', existingCharacters);

        if (existingCharacters >= 5) {
            console.log('[Create Character] ❌ Лимит достигнут');
            return res.status(400).json({
                success: false,
                message: 'Достигнут лимит персонажей (максимум 5)'
            });
        }

        // Создание персонажа
        const character = new Character({
            userId: req.user.id,
            characterClass: characterClass
        });

        console.log('[Create Character] 💾 Сохранение в БД...');
        await character.save();

        console.log('[Create Character] ✅ Успешно создан:', character._id);

        res.status(201).json({
            success: true,
            message: 'Персонаж создан успешно',
            character
        });
    } catch (error) {
        console.error('[Create Character] ❌ Ошибка:', error.message);
        console.error('[Create Character] Stack:', error.stack);
        res.status(500).json({
            success: false,
            message: 'Ошибка при создании персонажа',
            error: error.message
        });
    }
});

/**
 * POST /api/character/select-or-create
 * Умный выбор персонажа: если существует - вернет его, если нет - создаст нового
 * Это основной endpoint для Unity клиента при нажатии кнопки Play
 */
router.post('/select-or-create', auth, characterController.selectOrCreateCharacter);

/**
 * POST /api/character/select
 * Выбрать персонажа (установить isSelected = true)
 */
router.post('/select', auth, async (req, res) => {
    try {
        const { characterId } = req.body;

        if (!characterId) {
            return res.status(400).json({
                success: false,
                message: 'characterId обязателен'
            });
        }

        // Снять выбор со всех персонажей пользователя
        await Character.updateMany(
            { userId: req.user.id },
            { isSelected: false }
        );

        // Установить выбранного персонажа
        const character = await Character.findOneAndUpdate(
            { _id: characterId, userId: req.user.id },
            { isSelected: true },
            { new: true }
        );

        if (!character) {
            return res.status(404).json({
                success: false,
                message: 'Персонаж не найден'
            });
        }

        res.json({
            success: true,
            message: 'Персонаж выбран',
            character
        });
    } catch (error) {
        console.error('Select character error:', error);
        res.status(500).json({
            success: false,
            message: 'Ошибка при выборе персонажа'
        });
    }
});

/**
 * DELETE /api/character/:id
 * Удалить персонажа
 */
router.delete('/:id', auth, async (req, res) => {
    try {
        const character = await Character.findOneAndDelete({
            _id: req.params.id,
            userId: req.user.id
        });

        if (!character) {
            return res.status(404).json({
                success: false,
                message: 'Персонаж не найден'
            });
        }

        res.json({
            success: true,
            message: 'Персонаж удален'
        });
    } catch (error) {
        console.error('Delete character error:', error);
        res.status(500).json({
            success: false,
            message: 'Ошибка при удалении персонажа'
        });
    }
});

/**
 * POST /api/character/save-progress
 * Сохранить прогресс прокачки персонажа (уровень, опыт, статы)
 */
router.post('/save-progress', auth, async (req, res) => {
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

        // Находим персонажа по userId и class
        const character = await Character.findOne({
            userId,
            class: characterClass
        });

        if (!character) {
            return res.status(404).json({
                success: false,
                message: 'Персонаж не найден'
            });
        }

        // Обновляем данные прокачки
        character.level = leveling.level;
        character.experience = leveling.experience;
        character.availableStatPoints = leveling.availableStatPoints;

        // Обновляем SPECIAL stats
        character.stats = character.stats || {};
        character.stats.strength = stats.strength;
        character.stats.perception = stats.perception;
        character.stats.endurance = stats.endurance;
        character.stats.wisdom = stats.wisdom;
        character.stats.intelligence = stats.intelligence;
        character.stats.agility = stats.agility;
        character.stats.luck = stats.luck;

        character.lastPlayed = Date.now();
        await character.save();

        console.log(`[Save Progress] ✅ ${characterClass} прогресс сохранен: Level ${leveling.level}, XP ${leveling.experience}`);

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
});

/**
 * GET /api/character/load-progress
 * Загрузить прогресс прокачки персонажа (уровень, опыт, статы)
 */
router.get('/load-progress', auth, async (req, res) => {
    try {
        const userId = req.user.id;
        const { characterClass } = req.query;

        if (!characterClass) {
            return res.status(400).json({
                success: false,
                message: 'Не указан класс персонажа'
            });
        }

        // Находим персонажа по userId и class
        const character = await Character.findOne({
            userId,
            class: characterClass
        });

        if (!character) {
            return res.status(404).json({
                success: false,
                message: 'Персонаж не найден'
            });
        }

        console.log(`[Load Progress] ✅ ${characterClass} прогресс загружен: Level ${character.level}, XP ${character.experience}`);

        res.json({
            success: true,
            stats: {
                strength: character.stats?.strength || 1,
                perception: character.stats?.perception || 1,
                endurance: character.stats?.endurance || 1,
                wisdom: character.stats?.wisdom || 1,
                intelligence: character.stats?.intelligence || 1,
                agility: character.stats?.agility || 1,
                luck: character.stats?.luck || 1
            },
            leveling: {
                level: character.level || 1,
                experience: character.experience || 0,
                availableStatPoints: character.availableStatPoints || 0
            }
        });

    } catch (error) {
        console.error('Load progress error:', error);
        res.status(500).json({
            success: false,
            message: 'Ошибка загрузки прогресса'
        });
    }
});

/**
 * POST /api/character/progress
 * Сохранить прогресс персонажа (для Unity ApiClient)
 * Использует characterId вместо characterClass
 */
router.post('/progress', auth, async (req, res) => {
    try {
        const userId = req.user.id;
        const { characterId, stats, leveling } = req.body;

        console.log(`[Progress POST] Получен запрос от userId: ${userId}`);
        console.log(`[Progress POST] characterId: ${characterId}`);
        console.log(`[Progress POST] stats:`, stats);
        console.log(`[Progress POST] leveling:`, leveling);

        // Валидация
        if (!characterId) {
            return res.status(400).json({
                success: false,
                message: 'characterId обязателен'
            });
        }

        if (!stats || !leveling) {
            return res.status(400).json({
                success: false,
                message: 'Отсутствуют обязательные поля (stats, leveling)'
            });
        }

        // Находим персонажа по _id и userId
        const character = await Character.findOne({
            _id: characterId,
            userId
        });

        if (!character) {
            console.log(`[Progress POST] ❌ Персонаж не найден: characterId=${characterId}, userId=${userId}`);
            return res.status(404).json({
                success: false,
                message: 'Персонаж не найден'
            });
        }

        // Обновляем данные прокачки
        character.level = leveling.level || character.level;
        character.experience = leveling.experience || character.experience;
        character.availableStatPoints = leveling.availableStatPoints || character.availableStatPoints;

        // Обновляем SPECIAL stats
        character.stats = character.stats || {};
        character.stats.strength = stats.strength || character.stats.strength;
        character.stats.perception = stats.perception || character.stats.perception;
        character.stats.endurance = stats.endurance || character.stats.endurance;
        character.stats.wisdom = stats.wisdom || character.stats.wisdom;
        character.stats.intelligence = stats.intelligence || character.stats.intelligence;
        character.stats.agility = stats.agility || character.stats.agility;
        character.stats.luck = stats.luck || character.stats.luck;

        character.lastPlayed = Date.now();
        await character.save();

        console.log(`[Progress POST] ✅ Прогресс сохранен: ${character.class} Level ${character.level}, XP ${character.experience}`);

        res.json({
            success: true,
            message: 'Прогресс сохранен',
            stats: character.stats,
            leveling: {
                level: character.level,
                experience: character.experience,
                availableStatPoints: character.availableStatPoints
            }
        });

    } catch (error) {
        console.error('[Progress POST] ❌ Ошибка сохранения:', error);
        res.status(500).json({
            success: false,
            message: 'Ошибка сохранения прогресса'
        });
    }
});

/**
 * GET /api/character/progress
 * Загрузить прогресс персонажа (для Unity ApiClient)
 * Использует characterId вместо characterClass
 */
router.get('/progress', auth, async (req, res) => {
    try {
        const userId = req.user.id;
        const { characterId } = req.query;

        console.log(`[Progress GET] Получен запрос от userId: ${userId}, characterId: ${characterId}`);

        if (!characterId) {
            return res.status(400).json({
                success: false,
                message: 'characterId обязателен'
            });
        }

        // Находим персонажа по _id и userId
        const character = await Character.findOne({
            _id: characterId,
            userId
        });

        if (!character) {
            console.log(`[Progress GET] ❌ Персонаж не найден: characterId=${characterId}, userId=${userId}`);
            return res.status(404).json({
                success: false,
                message: 'Персонаж не найден'
            });
        }

        console.log(`[Progress GET] ✅ Прогресс загружен: ${character.class} Level ${character.level}, XP ${character.experience}`);

        res.json({
            success: true,
            stats: {
                strength: character.stats?.strength || 5,
                perception: character.stats?.perception || 5,
                endurance: character.stats?.endurance || 5,
                wisdom: character.stats?.wisdom || 5,
                intelligence: character.stats?.intelligence || 5,
                agility: character.stats?.agility || 5,
                luck: character.stats?.luck || 5
            },
            leveling: {
                level: character.level || 1,
                experience: character.experience || 0,
                availableStatPoints: character.availableStatPoints || 0
            }
        });

    } catch (error) {
        console.error('[Progress GET] ❌ Ошибка загрузки:', error);
        res.status(500).json({
            success: false,
            message: 'Ошибка загрузки прогресса'
        });
    }
});

module.exports = router;
