require('dotenv').config();
const express = require('express');
const http = require('http');
const socketIO = require('socket.io');
const cors = require('cors');
const connectDB = require('./config/db');

// Validate required environment variables
const requiredEnvVars = ['MONGODB_URI', 'JWT_SECRET'];
const missingEnvVars = requiredEnvVars.filter(varName => !process.env[varName]);

if (missingEnvVars.length > 0) {
  console.error('❌ Missing required environment variables:');
  missingEnvVars.forEach(varName => console.error(`   - ${varName}`));
  console.error('\n💡 Create a .env file with these variables. See .env.example for reference.');
  process.exit(1);
}

const app = express();
const server = http.createServer(app);

// CORS configuration
const allowedOrigins = process.env.ALLOWED_ORIGINS
  ? process.env.ALLOWED_ORIGINS.split(',').map(origin => origin.trim())
  : ['http://localhost:3000', 'http://localhost:5173'];

// Socket.IO setup with CORS
const io = socketIO(server, {
  cors: {
    origin: allowedOrigins,
    methods: ["GET", "POST"],
    credentials: true
  },
  transports: ['websocket', 'polling']
});

// Подключение к MongoDB
connectDB();

// Middleware
app.use(cors({
  origin: allowedOrigins,
  credentials: true
}));
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
app.use('/api/party', require('./routes/party'));

// Health check
app.get('/', (req, res) => {
  res.json({
    message: 'Aetherion Server is running!',
    version: '2.3.2-socket-debug',
    commit: 'pending',
    status: 'online',
    timestamp: new Date().toISOString(),
    features: ['REST API', 'Socket.IO', 'Multiplayer', 'MMO Persistent World', 'Socket.IO Full Debug']
  });
});

// Multiplayer logic - ВАЖНО: Должен быть ДО тестовых обработчиков!
try {
  require('./multiplayer')(io);
  console.log('✅ Multiplayer module loaded successfully');
} catch (error) {
  console.error('❌ Failed to load multiplayer module:', error.message);
}

// ТЕСТ: Тестовый обработчик для диагностики (ПОСЛЕ multiplayer.js!)
// ЗАКОММЕНТИРОВАН - мешает работе multiplayer.js
/*
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
*/

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
