# 🚀 Быстрый старт - Aetherion Multiplayer

## ✅ Что уже сделано

1. ✅ Все серверные файлы созданы (Node.js + Socket.io)
2. ✅ Все Unity скрипты созданы (WebSocket, NetworkManagers)
3. ✅ Git репозиторий инициализирован
4. ✅ Код закоммичен
5. ✅ Editor скрипт для автонастройки создан

## 🎯 Что нужно сделать (5 минут)

### Шаг 1: Создать GitHub репозиторий (2 минуты)

1. Зайдите на https://github.com/new
2. Repository name: **aetherion** (или любое другое)
3. Visibility: **Private** (рекомендуется)
4. НЕ добавляйте README, .gitignore или license
5. Нажмите **Create repository**

### Шаг 2: Подключить локальный репозиторий к GitHub (1 минута)

GitHub покажет команды. Откройте Git Bash в папке проекта и выполните:

```bash
cd /c/Users/Asus/Aetherion
git remote add origin https://github.com/ВАШ_USERNAME/aetherion.git
git branch -M main
git push -u origin main
```

**ВАЖНО**: Замените `ВАШ_USERNAME` на ваш реальный GitHub username!

Пример:
```bash
git remote add origin https://github.com/ivan123/aetherion.git
```

### Шаг 3: У вас уже есть сервер на Render! (0 минут)

Ваш существующий сервер: **https://aetherion-server-gv5u.onrender.com**

Нужно только обновить его новым кодом:

1. Зайдите на [render.com](https://dashboard.render.com)
2. Найдите ваш сервис **aetherion-server-gv5u**
3. Settings → "Connect to Repository"
4. Выберите ваш новый GitHub репозиторий **aetherion**
5. Settings → Root Directory: установите **SERVER_CODE**
6. Manual Deploy → Deploy latest commit

Render автоматически задеплоит обновленный код с поддержкой мультиплеера!

### Шаг 4: Настроить Unity (2 минуты)

1. Откройте Unity проект
2. Откройте сцену **GameScene**
3. В меню Unity: **Tools → Aetherion → Setup Multiplayer Managers**
4. В окне проверьте что Server URL: `https://aetherion-server-gv5u.onrender.com`
5. Нажмите **"Setup NetworkManagers"**
6. Сохраните сцену (Ctrl+S)

Готово! NetworkManagers созданы автоматически!

---

## 🎮 Тестирование

### Вариант 1: В Unity Editor (быстро)

1. Откройте GameScene
2. Нажмите Play
3. Войдите в аккаунт
4. Нажмите "В бой"

Консоль покажет:
```
[GameScene] Поиск доступных комнат...
[RoomManager] Комната создана: YourName's Arena
[WebSocket] ✅ Подключено!
[ArenaManager] 🌐 MULTIPLAYER MODE
```

### Вариант 2: Два клиента (полный тест)

**Клиент 1** (Unity Editor):
1. Play → Войти → "В бой"
2. Создастся комната

**Клиент 2** (Build или другой Editor):
1. Войти под другим аккаунтом → "В бой"
2. Присоединится к комнате Клиента 1
3. Вы увидите друг друга!

---

## 🐛 Если что-то не работает

### "NetworkManagers not found"
- Откройте GameScene
- Tools → Aetherion → Setup Multiplayer Managers
- Нажмите "Setup NetworkManagers"

### "WebSocket не подключен"
- Проверьте что Render сервер запущен: https://aetherion-server-gv5u.onrender.com/health
- Если сервер "sleeping" - подождите 30-60 секунд (Free tier просыпается)

### "Комната не создается"
- Проверьте консоль Unity на ошибки
- Проверьте что вы залогинены (есть токен)
- Проверьте что MongoDB подключена (Render logs)

### Ошибка компиляции Unity
Если Unity ругается на отсутствующие классы:
1. Assets → Reimport All
2. Или перезапустите Unity

---

## 📋 Checklist

- [ ] GitHub репозиторий создан
- [ ] Код запушен на GitHub (`git push`)
- [ ] Render подключен к GitHub репозиторию
- [ ] Root Directory установлен в `SERVER_CODE`
- [ ] Render задеплоил новый код
- [ ] Unity: NetworkManagers созданы в GameScene
- [ ] Unity: Сцена сохранена
- [ ] Тест: запустили игру и нажали "В бой"

---

## 🎯 Итоговая архитектура

```
Unity Client → BattleButton click
    ↓
GameSceneManager.OnBattleButtonClick()
    ↓
RoomManager.GetAvailableRooms() → REST API
    ↓
Если комната найдена → JoinRoom()
Если нет → CreateRoom()
    ↓
WebSocketClient.Connect() → Socket.io
    ↓
WebSocketClient.JoinRoom()
    ↓
Load ArenaScene
    ↓
ArenaManager (detects multiplayer mode)
    ↓
Creates NetworkSyncManager
    ↓
Real-time synchronization starts!
```

---

## 💡 Следующие шаги

После успешного теста:
1. ✅ Создайте nameplate prefab для имен над головами игроков
2. ✅ Добавьте UI лобби комнат (опционально)
3. ✅ Оптимизируйте для мобильных (снизьте tick rate если нужно)
4. ✅ Перейдите на Starter plan Render ($7/мес) чтобы сервер не засыпал

---

**Версия**: 2.0.0
**Дата**: 2025-10-10
**Время на setup**: ~5-10 минут

**Удачи! 🚀**
