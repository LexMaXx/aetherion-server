const express = require('express');
const router = express.Router();
const { body, validationResult } = require('express-validator');
const authController = require('../controllers/authController');
const auth = require('../middleware/auth');

// Валидация для регистрации
const registerValidation = [
  body('username')
    .trim()
    .isLength({ min: 3, max: 20 })
    .withMessage('Username должен быть от 3 до 20 символов')
    .matches(/^[a-zA-Z0-9_]+$/)
    .withMessage('Username может содержать только буквы, цифры и подчеркивание'),

  body('email')
    .trim()
    .isEmail()
    .withMessage('Введите корректный email')
    .normalizeEmail(),

  body('password')
    .isLength({ min: 6 })
    .withMessage('Пароль должен быть минимум 6 символов')
    .matches(/\d/)
    .withMessage('Пароль должен содержать хотя бы одну цифру')
    .matches(/[a-zA-Z]/)
    .withMessage('Пароль должен содержать хотя бы одну букву')
];

// Валидация для логина
const loginValidation = [
  body('username')
    .trim()
    .notEmpty()
    .withMessage('Username обязателен'),

  body('password')
    .notEmpty()
    .withMessage('Пароль обязателен')
];

// Middleware для проверки ошибок валидации
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

// @route   POST /api/auth/register
// @desc    Регистрация нового пользователя
// @access  Public
router.post('/register', registerValidation, validate, authController.register);

// @route   POST /api/auth/login
// @desc    Вход пользователя
// @access  Public
router.post('/login', loginValidation, validate, authController.login);

// @route   GET /api/auth/verify
// @desc    Проверка токена
// @access  Private
router.get('/verify', auth, authController.verifyToken);

module.exports = router;
