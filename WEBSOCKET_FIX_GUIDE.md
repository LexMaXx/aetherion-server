# 🔧 Инструкция по исправлению WebSocket / Мультиплеера

## ❌ ТЕКУЩАЯ ПРОБЛЕМА

WebSocket НЕ работает потому что:
1. Используется **HTTP polling** вместо настоящего WebSocket
2. Socket.IO протокол реализован неправильно
3. Сервер возвращает **400 Bad Request** на все события

---

## ✅ РЕШЕНИЕ: Установить Socket.IO клиент для Unity

### Вариант 1: Socket.IO Unity (РЕКОМЕНДУЕТСЯ)

#### Шаг 1: Скачать пакет

1. Перейдите на GitHub: https://github.com/itisnajim/SocketIOUnity
2. Скачайте ZIP архив (`Code → Download ZIP`)
3. Распакуйте в папку `Assets/Plugins/SocketIOUnity`

#### Шаг 2: Или через Package Manager

1. Откройте `Window → Package Manager`
2. Нажмите `+ → Add package from git URL`
3. Введите: `https://github.com/itisnajim/SocketIOUnity.git`
4. Нажмите `Add`

#### Шаг 3: Заменить WebSocketClient

После установки пакета:

1. Переименуйте `WebSocketClient.cs` → `WebSocketClient_OLD.cs`
2. Переименуйте `SocketIOManager.cs` → `WebSocketClient.cs`
3. Unity перекомпилируется автоматически

---

### Вариант 2: Native WebSocket (Альтернатива)

Если Socket.IO не установится:

1. Скачайте: https://github.com/endel/NativeWebSocket
2. Распакуйте в `Assets/Plugins/NativeWebSocket`
3. Используйте WebSocketClientFixed.cs

---

## 🎮 ПРОВЕРКА РАБОТЫ

### После установки:

1. Запустите Unity
2. Откройте игру
3. Попробуйте создать комнату
4. В консоли должно быть:
   ```
   [SocketIO] ✅ Подключено! Socket ID: xyz123
   [SocketIO] 🚪 Вход в комнату: room123
   [SocketIO] ✅ Получено состояние комнаты!
   ```

### Если видите ошибки:

- `"The type or namespace name 'SocketIOClient' could not be found"`
  → Пакет не установлен, установите через PackageManager

- `400 Bad Request`
  → Всё ещё используется старый WebSocketClient

---

## 📋 ЧЕКЛИСТ ИСПРАВЛЕНИЯ

- [ ] Установлен Socket.IO Unity пакет
- [ ] Переименован старый WebSocketClient
- [ ] Новый WebSocketClient использует SocketIOClient
- [ ] Игра компилируется без ошибок
- [ ] Подключение к серверу работает
- [ ] Создание комнаты работает
- [ ] Второй игрок может подключиться

---

## 🆘 ЕСЛИ НЕ РАБОТАЕТ

### Проблема: Пакет не устанавливается

**Решение:** Скачайте готовый DLL:

1. Идите на NuGet: https://www.nuget.org/packages/SocketIOClient
2. Скачайте `.nupkg` файл
3. Переименуйте в `.zip` и распакуйте
4. Скопируйте `SocketIOClient.dll` в `Assets/Plugins/`

### Проблема: Сервер не отвечает

**Проверьте сервер:**
```bash
curl https://aetherion-server-gv5u.onrender.com/socket.io/
```

Должен вернуть код 200 и текст с `{"sid":"..."}`

---

## 📞 КОНТАКТЫ РАЗРАБОТЧИКОВ

- SocketIOUnity: https://github.com/itisnajim/SocketIOUnity/issues
- NativeWebSocket: https://github.com/endel/NativeWebSocket/issues

---

**Дата создания:** $(date)
**Версия Unity:** 6000.0.32f1
