# 🔧 Server Room API Fix - Deployment Guide

## Проблема
При попытке создать или присоединиться к комнате сервер возвращал ошибки:
- **HTTP 500 Internal Server Error** - при входе в существующую комнату
- **HTTP 400 Bad Request** - при создании новой комнаты

## Причина
JWT токен возвращает `req.user.id` как **string**, но MongoDB модель Room ожидает `userId` как **ObjectId**. Это вызывало ошибки при сравнении и сохранении данных.

## Исправления
Обновлен файл `SERVER_CODE/routes/room.js`:

### 1. Добавлен импорт mongoose
```javascript
const mongoose = require('mongoose'); // Для конвертации userId в ObjectId
```

### 2. Исправлены все route handlers для конвертации userId

#### `/api/room/create`
```javascript
const userIdStr = req.user.id; // Из JWT токена (string)
const userId = mongoose.Types.ObjectId(userIdStr); // Конвертируем в ObjectId
```

#### `/api/room/join`
```javascript
const userIdStr = req.user.id;
const userId = mongoose.Types.ObjectId(userIdStr);
```

#### `/api/room/leave`
```javascript
const userIdStr = req.user.id;
const userId = mongoose.Types.ObjectId(userIdStr);
```

#### `/api/room/start`
```javascript
const userIdStr = req.user.id;
const userId = mongoose.Types.ObjectId(userIdStr);
```

## Как задеплоить на Render.com

### Вариант 1: Через Git Push (Рекомендуется)

1. **Закоммитить изменения:**
```bash
cd c:/Users/Asus/Aetherion
git add SERVER_CODE/routes/room.js
git commit -m "Fix: Convert userId from JWT string to ObjectId in room routes

- Fixed HTTP 500 error when joining rooms
- Fixed HTTP 400 error when creating rooms
- Added mongoose import for ObjectId conversion
- Updated all room route handlers (create, join, leave, start)"
git push origin main
```

2. **Render автоматически задеплоит изменения:**
   - Зайдите на https://dashboard.render.com
   - Найдите ваш сервис "aetherion-server"
   - Дождитесь автоматического деплоя (обычно 2-5 минут)
   - Статус изменится на "Live"

### Вариант 2: Manual Deploy

Если автодеплой не настроен:

1. Зайдите на https://dashboard.render.com
2. Выберите ваш сервис "aetherion-server"
3. Нажмите **"Manual Deploy"** → **"Deploy latest commit"**
4. Дождитесь завершения деплоя

## Как проверить что фикс работает

### 1. Проверить health endpoint
```bash
curl https://aetherion-server-gv5u.onrender.com/health
```

Должен вернуть:
```json
{
  "status": "healthy",
  "mongodb": "connected"
}
```

### 2. Запустить Unity игру и попробовать:
1. Зарегистрироваться / Войти
2. Выбрать персонажа
3. **Создать комнату** - должно работать без ошибок 400
4. **Войти в комнату** - должно работать без ошибок 500
5. Загрузить ArenaScene

### 3. Проверить логи сервера
На Render.com dashboard → Logs, должны появиться:
```
[Room] Created: <room-id> by <username>
[Room] <username> joined room <room-id>
```

## Что делать если всё ещё есть ошибки

### Проверьте версию Mongoose
В `SERVER_CODE/package.json` должно быть:
```json
"mongoose": "^7.0.0"
```

Если версия < 6.0, используйте другой синтаксис:
```javascript
const userId = new mongoose.Types.ObjectId(userIdStr);
```

### Проверьте JWT payload
JWT токен должен содержать `id` поле:
```javascript
// В auth middleware должен быть:
const decoded = jwt.verify(token, process.env.JWT_SECRET);
// decoded.id должен существовать
```

### Проверьте логи Unity
В Unity Console должны быть логи:
```
[RoomManager] Create room request: {"roomName":"...","characterClass":"Warrior",...}
[RoomManager] ✅ Комната создана: ... (ID: ...)
```

Если видите:
```
[RoomManager] ❌ HTTP ошибка: ...
```

Проверьте response body для детальной ошибки.

## Дополнительные улучшения (опционально)

### Добавить error handling для невалидного ObjectId
```javascript
try {
    const userId = mongoose.Types.ObjectId(userIdStr);
} catch (error) {
    return res.status(400).json({
        success: false,
        message: 'Invalid user ID format'
    });
}
```

### Добавить логирование для дебага
```javascript
console.log(`[Room] User ${userId} attempting to create room`);
```

## Статус исправлений

✅ Добавлен импорт mongoose
✅ Исправлен POST /api/room/create
✅ Исправлен POST /api/room/join
✅ Исправлен POST /api/room/leave
✅ Исправлен POST /api/room/start
⏳ Ожидает деплоя на Render.com
⏳ Ожидает тестирования в Unity

---

**Дата:** 2025-10-12
**Версия:** 2.0.1
**Автор:** Claude Code Assistant
