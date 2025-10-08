const express = require('express');
const router = express.Router();
const { body, validationResult } = require('express-validator');
const auth = require('../middleware/auth');
const characterController = require('../controllers/characterController');

// Middleware для валидации
const validate = (req, res, next) => {
  const errors = validationResult(req);
  if (!errors.isEmpty()) {
    return res.status(400).json({
      success: false,
      message: errors.array()[0].msg
    });
  }
  next();
};

// @route   GET /api/character
// @desc    Получить всех персонажей игрока
// @access  Private
router.get('/', auth, characterController.getCharacters);

// @route   POST /api/character/select
// @desc    Выбрать класс персонажа (создать если нет, загрузить если есть)
// @access  Private
router.post('/select',
  auth,
  [
    body('characterClass')
      .isIn(['Warrior', 'Mage', 'Archer', 'Rogue', 'Paladin'])
      .withMessage('Недопустимый класс персонажа')
  ],
  validate,
  characterController.selectOrCreateCharacter
);

// @route   DELETE /api/character/:characterId
// @desc    Удалить персонажа
// @access  Private
router.delete('/:characterId', auth, characterController.deleteCharacter);

module.exports = router;
