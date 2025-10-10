const express = require('express');
const router = express.Router();
const auth = require('../middleware/auth');
const Character = require('../models/Character');

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
        const { name, class: characterClass } = req.body;

        // Валидация
        if (!name || !characterClass) {
            return res.status(400).json({
                success: false,
                message: 'Имя и класс обязательны'
            });
        }

        // Проверка лимита персонажей (макс 5)
        const existingCharacters = await Character.countDocuments({ userId: req.user.id });
        if (existingCharacters >= 5) {
            return res.status(400).json({
                success: false,
                message: 'Достигнут лимит персонажей (максимум 5)'
            });
        }

        // Создание персонажа
        const character = new Character({
            userId: req.user.id,
            name,
            class: characterClass
        });

        await character.save();

        res.status(201).json({
            success: true,
            message: 'Персонаж создан успешно',
            character
        });
    } catch (error) {
        console.error('Create character error:', error);
        res.status(500).json({
            success: false,
            message: 'Ошибка при создании персонажа'
        });
    }
});

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

module.exports = router;
