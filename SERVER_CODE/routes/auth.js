const express = require('express');
const router = express.Router();
const bcrypt = require('bcrypt');
const jwt = require('jsonwebtoken');
const User = require('../models/User');

/**
 * POST /api/auth/register
 * Регистрация нового пользователя
 */
router.post('/register', async (req, res) => {
    try {
        const { username, email, password } = req.body;

        // Валидация
        if (!username || !email || !password) {
            return res.status(400).json({
                success: false,
                message: 'Все поля обязательны'
            });
        }

        // Проверка существующего пользователя
        const existingUser = await User.findOne({ $or: [{ email }, { username }] });
        if (existingUser) {
            return res.status(400).json({
                success: false,
                message: 'Пользователь с таким email или username уже существует'
            });
        }

        // Хеширование пароля
        const hashedPassword = await bcrypt.hash(password, 10);

        // Создание пользователя
        const user = new User({
            username,
            email,
            password: hashedPassword
        });

        await user.save();

        // Создание JWT токена
        const token = jwt.sign(
            { id: user._id, username: user.username },
            process.env.JWT_SECRET,
            { expiresIn: '7d' }
        );

        res.status(201).json({
            success: true,
            message: 'Регистрация успешна',
            token,
            user: {
                id: user._id,
                username: user.username,
                email: user.email
            }
        });
    } catch (error) {
        console.error('Registration error:', error);
        res.status(500).json({
            success: false,
            message: 'Ошибка сервера при регистрации'
        });
    }
});

/**
 * POST /api/auth/login
 * Вход пользователя
 */
router.post('/login', async (req, res) => {
    try {
        const { username, password } = req.body;

        // Валидация
        if (!username || !password) {
            return res.status(400).json({
                success: false,
                message: 'Username и пароль обязательны'
            });
        }

        // Поиск пользователя по username
        const user = await User.findOne({ username });
        if (!user) {
            return res.status(401).json({
                success: false,
                message: 'Неверный username или пароль'
            });
        }

        // Проверка пароля
        const isPasswordValid = await bcrypt.compare(password, user.password);
        if (!isPasswordValid) {
            return res.status(401).json({
                success: false,
                message: 'Неверный username или пароль'
            });
        }

        // Обновление lastLogin
        user.lastLogin = new Date();
        await user.save();

        // Создание JWT токена
        const token = jwt.sign(
            { id: user._id, username: user.username },
            process.env.JWT_SECRET,
            { expiresIn: '7d' }
        );

        res.json({
            success: true,
            message: 'Вход выполнен успешно',
            token,
            user: {
                id: user._id,
                username: user.username,
                email: user.email
            }
        });
    } catch (error) {
        console.error('Login error:', error);
        res.status(500).json({
            success: false,
            message: 'Ошибка сервера при входе'
        });
    }
});

/**
 * GET /api/auth/verify
 * Проверка токена
 */
router.get('/verify', async (req, res) => {
    try {
        const token = req.header('Authorization')?.replace('Bearer ', '');

        if (!token) {
            return res.status(401).json({
                success: false,
                message: 'Токен не предоставлен'
            });
        }

        const decoded = jwt.verify(token, process.env.JWT_SECRET);
        const user = await User.findById(decoded.id).select('-password');

        if (!user) {
            return res.status(404).json({
                success: false,
                message: 'Пользователь не найден'
            });
        }

        res.json({
            success: true,
            user: {
                id: user._id,
                username: user.username,
                email: user.email
            }
        });
    } catch (error) {
        res.status(401).json({
            success: false,
            message: 'Недействительный токен'
        });
    }
});

module.exports = router;
