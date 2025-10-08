# Aetherion MMO RPG Server

Backend сервер для игры Aetherion на Node.js + MongoDB + Express

## 🚀 Установка

1. Установи зависимости:
```bash
npm install
```

2. Настрой `.env` файл:
   - Замени `MONGODB_URI` на свою строку подключения MongoDB
   - Измени `JWT_SECRET` на случайную строку

3. Запусти сервер:
```bash
npm start
```

Для разработки с автоперезагрузкой:
```bash
npm run dev
```

## 📋 API Endpoints

### Регистрация
```
POST /api/auth/register
Body: {
  "username": "player1",
  "email": "player@example.com",
  "password": "password123"
}
```

### Вход
```
POST /api/auth/login
Body: {
  "username": "player1",
  "password": "password123"
}
```

### Проверка токена
```
GET /api/auth/verify
Headers: {
  "Authorization": "Bearer YOUR_JWT_TOKEN"
}
```

## 🌍 Деплой на Render

1. Создай новый Web Service на Render.com
2. Подключи GitHub репозиторий
3. Настрой переменные окружения (MONGODB_URI, JWT_SECRET)
4. Deploy!

## 📝 Структура проекта

```
aetherion-server/
├── config/         # Конфигурация (MongoDB)
├── controllers/    # Логика обработки запросов
├── middleware/     # JWT авторизация
├── models/         # MongoDB модели
├── routes/         # API маршруты
├── .env           # Переменные окружения
└── server.js      # Точка входа
```
