# 🎮 Aetherion MMO

**Lineage 2-style PVP Arena** с real-time мультиплеером

---

## ⚡ БЫСТРЫЙ СТАРТ

### Запустить мультиплеер (5 минут):
📖 **[DEPLOY_NOW.md](DEPLOY_NOW.md)** ← Начни отсюда!

### Полное руководство:
📚 **[QUICK_START.md](QUICK_START.md)**

---

## 🌟 Что сделано

✅ **Real-time multiplayer** - до 20 игроков в комнате
✅ **PvP Arena** - все против всех (free-for-all)
✅ **Auto matchmaking** - автоматический поиск/создание комнат
✅ **WebSocket синхронизация** - 10 Hz позиция, 20 Hz бой
✅ **Серверная авторизация урона** - защита от читинга
✅ **5 классов** - Warrior, Mage, Archer, Rogue (Necromancer), Paladin
✅ **SPECIAL stats system** - 7 характеристик
✅ **Mobile friendly** - оптимизировано для Android

---

## 🎯 Как это работает

```
Unity Client → BattleButton click
    ↓
RoomManager → Поиск/создание комнаты (REST API)
    ↓
WebSocketClient → Подключение (Socket.io)
    ↓
ArenaScene → Multiplayer mode detected
    ↓
NetworkSyncManager → Real-time синхронизация
    ↓
NetworkPlayers появляются и синхронизируются!
```

---

## 📂 Созданные файлы

### Серверная часть (9 файлов)
- `SERVER_CODE/models/Room.js` - Модель комнаты
- `SERVER_CODE/models/User.js` - Модель пользователя
- `SERVER_CODE/models/Character.js` - Модель персонажа
- `SERVER_CODE/routes/room.js` - REST API комнат
- `SERVER_CODE/routes/auth.js` - Аутентификация
- `SERVER_CODE/routes/character.js` - Управление персонажами
- `SERVER_CODE/socket/gameSocket.js` - WebSocket сервер
- `SERVER_CODE/middleware/auth.js` - JWT middleware
- `SERVER_CODE/package.json` - Зависимости

### Unity клиент (6 новых + 2 модифицированных)
**Новые**:
- `Assets/Scripts/Network/WebSocketClient.cs` - Socket.io клиент
- `Assets/Scripts/Network/RoomManager.cs` - Управление комнатами
- `Assets/Scripts/Network/NetworkPlayer.cs` - Удаленный игрок
- `Assets/Scripts/Network/NetworkSyncManager.cs` - Синхронизация
- `Assets/Scripts/Network/NetworkCombatSync.cs` - Синхронизация боя
- `Assets/Scripts/Editor/SetupMultiplayerManagers.cs` - Auto-setup

**Модифицированные**:
- `Assets/Scripts/UI/GameSceneManager.cs` - BattleButton + room system
- `Assets/Scripts/Arena/ArenaManager.cs` - Multiplayer support

---

## 🚀 Деплой (3 шага)

### 1️⃣ Создать GitHub репозиторий
https://github.com/new → `aetherion` (Private)

### 2️⃣ Запушить код
```bash
cd /c/Users/Asus/Aetherion
git remote add origin https://github.com/ВАШ_USERNAME/aetherion.git
git push -u origin main
```

### 3️⃣ Обновить Render
- Dashboard: https://dashboard.render.com
- Найти: **aetherion-server-gv5u**
- Connect Repository → aetherion
- Root Directory: `SERVER_CODE`
- Manual Deploy

### 4️⃣ Unity Setup
- Tools → Aetherion → Setup Multiplayer Managers
- Сохранить сцену

**Готово! 🎉**

---

## 📚 Документация

### Быстрый старт
- **[DEPLOY_NOW.md](DEPLOY_NOW.md)** - ⚡ Запустить за 5 минут
- **[QUICK_START.md](QUICK_START.md)** - 🚀 Полное руководство

### Подробная документация
- **[MULTIPLAYER_README.md](MULTIPLAYER_README.md)** - 📖 Архитектура системы
- **[SERVER_CODE/RENDER_DEPLOY.md](SERVER_CODE/RENDER_DEPLOY.md)** - 🔧 Настройка Render
- **[SERVER_CODE/MULTIPLAYER_SETUP.md](SERVER_CODE/MULTIPLAYER_SETUP.md)** - 📘 Полная документация + troubleshooting

---

## 🎮 Тестирование

1. Play в Unity
2. Войти в аккаунт
3. Нажать "В бой"
4. Мультиплеер работает!

**Два клиента**:
- Клиент 1: создает комнату
- Клиент 2: присоединяется автоматически
- Оба видят друг друга!

---

## 🏗️ Технологии

**Backend**: Node.js + Express + Socket.io + MongoDB
**Frontend**: Unity 2022.3.11f1 + C# + WebSocket
**Hosting**: Render.com
**Database**: MongoDB Atlas

---

## 🌍 Production

**Server**: https://aetherion-server-gv5u.onrender.com
**Health**: https://aetherion-server-gv5u.onrender.com/health

---

## 📊 Статистика

- **Серверные файлы**: 9 новых
- **Unity скрипты**: 6 новых + 2 модифицированных
- **Строк кода**: ~4000+
- **Документация**: 5 файлов
- **Время деплоя**: 5-10 минут

---

## ✅ Checklist для деплоя

- [ ] GitHub репозиторий создан
- [ ] `git push` выполнен
- [ ] Render подключен к GitHub
- [ ] Root Directory = `SERVER_CODE`
- [ ] Manual Deploy нажат
- [ ] Unity: NetworkManagers созданы
- [ ] Unity: Сцена сохранена
- [ ] Тест: игра запущена

---

## 🐛 Проблемы?

См. troubleshooting в [SERVER_CODE/MULTIPLAYER_SETUP.md](SERVER_CODE/MULTIPLAYER_SETUP.md#решение-проблем)

---

**Version**: 2.0.0
**Date**: 2025-10-10
**Engine**: Unity 2022.3.11f1

**Ready to play! 🎮🚀**
