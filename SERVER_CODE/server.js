// server.js
// Главный файл сервера с WebSocket поддержкой

const express = require('express');
const http = require('http');
const socketIo = require('socket.io');
const mongoose = require('mongoose');
const cors = require('cors');
require('dotenv').config();

const app = express();
const server = http.createServer(app);

// Socket.io с CORS
const io = socketIo(server, {
    cors: {
        origin: "*", // В продакшене укажите конкретный домен
        methods: ["GET", "POST"],
        credentials: true
    },
    pingTimeout: 60000,
    pingInterval: 25000
});

// Middleware
app.use(cors());
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// MongoDB подключение
mongoose.connect(process.env.MONGODB_URI, {
    useNewUrlParser: true,
    useUnifiedTopology: true
})
.then(() => console.log('[MongoDB] Connected'))
.catch(err => console.error('[MongoDB] Connection error:', err));

// REST API Routes
const authRoutes = require('./routes/auth');
const characterRoutes = require('./routes/character');
const roomRoutes = require('./routes/room'); // НОВОЕ

app.use('/api/auth', authRoutes);
app.use('/api/character', characterRoutes);
app.use('/api/room', roomRoutes); // НОВОЕ

// WebSocket инициализация
const { initializeGameSocket } = require('./socket/gameSocket');
initializeGameSocket(io);

// Главная страница
app.get('/', (req, res) => {
    res.json({
        status: 'online',
        version: '2.0.0',
        message: 'Aetherion MMO Server is running!',
        features: {
            rest_api: true,
            websocket: true,
            realtime_sync: true
        }
    });
});

// Health check
app.get('/health', (req, res) => {
    res.json({
        status: 'healthy',
        uptime: process.uptime(),
        mongodb: mongoose.connection.readyState === 1 ? 'connected' : 'disconnected',
        timestamp: Date.now()
    });
});

// Автоочистка старых комнат каждый час
const Room = require('./models/Room');
setInterval(async () => {
    try {
        const deleted = await Room.cleanupOldRooms();
        if (deleted > 0) {
            console.log(`[Cleanup] Removed ${deleted} old rooms`);
        }
    } catch (error) {
        console.error('[Cleanup] Error:', error);
    }
}, 60 * 60 * 1000); // 1 час

// Error handling
app.use((err, req, res, next) => {
    console.error('[Error]', err);
    res.status(500).json({
        success: false,
        message: 'Internal server error',
        error: process.env.NODE_ENV === 'development' ? err.message : undefined
    });
});

// Запуск сервера
const PORT = process.env.PORT || 3000;
server.listen(PORT, () => {
    console.log(`[Server] Running on port ${PORT}`);
    console.log(`[Server] REST API: http://localhost:${PORT}/api`);
    console.log(`[Server] WebSocket: ws://localhost:${PORT}`);
});

// Graceful shutdown
process.on('SIGTERM', () => {
    console.log('[Server] SIGTERM received, shutting down gracefully');
    server.close(() => {
        console.log('[Server] Closed');
        mongoose.connection.close(false, () => {
            console.log('[MongoDB] Connection closed');
            process.exit(0);
        });
    });
});
