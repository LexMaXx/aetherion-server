# ⚡ БЫСТРЫЙ СТАРТ СЕРВЕРА

## 🚀 ЗАПУСК ЗА 3 МИНУТЫ:

### 1. Установить зависимости:
```bash
cd c:/Users/Asus/Aetherion/Server
npm install
```

### 2. Запустить сервер:
```bash
npm start
```

**Ожидаемое:**
```
⚠️ Running without database (testing mode)
🚀 Aetherion Server running on port 3000
```

### 3. Обновить Unity:

**SocketIOManager.cs:16** и **ApiClient.cs:14:**
```csharp
private string serverUrl = "http://localhost:3000";
```

### 4. Тест в Unity:
```
1. Play ▶️
2. Создать комнату
3. Второй клиент → присоединиться
4. Countdown 3-2-1 → СПАВН!
```

---

## 📋 ЛОГИ СЕРВЕРА (должны быть):

```
✅ Socket connected: abc123 (user: player1)
[Room xyz] 🏁 LOBBY STARTED (2 players)
[Room xyz] ⏱️ COUNTDOWN STARTED
[Room xyz] Countdown: 3
[Room xyz] Countdown: 2
[Room xyz] Countdown: 1
[Room xyz] 🎮 GAME STARTED
```

---

## ⚠️ ЕСЛИ НЕ РАБОТАЕТ:

### Проблема: `npm: command not found`
**Решение:** Установите Node.js → [nodejs.org](https://nodejs.org/)

### Проблема: Unity не подключается
**Решение:**
1. Сервер запущен? `npm start`
2. `serverUrl = "http://localhost:3000"`?
3. Проверьте Console Unity

---

**ВСЁ! ТЕСТИРУЙТЕ!** 🎮
