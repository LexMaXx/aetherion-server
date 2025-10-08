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

// @route   POST /api/character
// @desc    Создать нового персонажа
// @access  Private
router.post('/',
  auth,
  [
    body('characterName')
      .trim()
      .isLength({ min: 3, max: 20 })
      .withMessage('Имя персонажа должно быть от 3 до 20 символов')
      .matches(/^[a-zA-Z0-9_а-яА-ЯёЁ]+$/)
      .withMessage('Имя может содержать только буквы, цифры и _'),
    body('characterClass')
      .isIn(['Warrior', 'Mage', 'Archer', 'Rogue', 'Paladin'])
      .withMessage('Недопустимый класс персонажа')
  ],
  validate,
  characterController.createCharacter
);

// @route   DELETE /api/character/:characterId
// @desc    Удалить персонажа
// @access  Private
router.delete('/:characterId', auth, characterController.deleteCharacter);

// @route   POST /api/character/:characterId/select
// @desc    Выбрать персонажа для игры
// @access  Private
router.post('/:characterId/select', auth, characterController.selectCharacter);

module.exports = router;
