require('dotenv').config();
const express = require('express');
const http = require('http');
const socketIO = require('socket.io');
const cors = require('cors');
const connectDB = require('./config/db');

const app = express();
const server = http.createServer(app);

// Socket.IO setup with CORS
const io = socketIO(server, {
  cors: {
    origin: "*", // В production укажите конкретные домены
    methods: ["GET", "POST"],
    credentials: true
  },
  transports: ['websocket', 'polling']
});

// Подключение к MongoDB
connectDB();

// Middleware
app.use(cors());
app.use(express.json());

// Logging middleware
app.use((req, res, next) => {
  console.log(`📨 ${req.method} ${req.path}`);
  next();
});

// Routes
app.use('/api/auth', require('./routes/auth'));
app.use('/api/character', require('./routes/character'));
app.use('/api/room', require('./routes/room'));

// Health check
app.get('/', (req, res) => {
  res.json({
    message: 'Aetherion Server is running!',
    version: '2.0.0',
    status: 'online',
    features: ['REST API', 'Socket.IO', 'Multiplayer']
  });
});

// ТЕСТ: Добавим тестовый обработчик Socket.IO для диагностики
io.on('connection', (socket) => {
  console.log(`🧪 TEST: Socket connected: ${socket.id}`);

  socket.on('ping', (data) => {
    console.log(`🧪 TEST: Received ping from ${socket.id}`);
    socket.emit('pong', { message: 'Server is alive!' });
  });

  socket.on('disconnect', () => {
    console.log(`🧪 TEST: Socket disconnected: ${socket.id}`);
  });
});

// Multiplayer logic
try {
  require('./multiplayer')(io);
  console.log('✅ Multiplayer module loaded successfully');
} catch (error) {
  console.error('❌ Failed to load multiplayer module:', error.message);
}

// Error handling middleware
app.use((err, req, res, next) => {
  console.error(err.stack);
  res.status(500).json({
    success: false,
    message: 'Server Error'
  });
});

const PORT = process.env.PORT || 5000;

server.listen(PORT, () => {
  console.log('═══════════════════════════════════════════');
  console.log(`🚀 Aetherion Server running on port ${PORT}`);
  console.log(`🌍 Environment: ${process.env.NODE_ENV}`);
  console.log(`🔌 Socket.IO enabled for real-time multiplayer`);
  console.log(`📊 MongoDB connected`);
  console.log('═══════════════════════════════════════════');
});

// Graceful shutdown
process.on('SIGTERM', () => {
  console.log('SIGTERM received, closing server...');
  server.close(() => {
    console.log('Server closed');
    process.exit(0);
  });
});
