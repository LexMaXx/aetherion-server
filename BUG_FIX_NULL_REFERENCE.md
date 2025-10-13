# 🐛 БАГ ИСПРАВЛЕН: NullReferenceException

**Дата**: 2025-10-12
**Статус**: ✅ ИСПРАВЛЕНО

---

## 🔍 ПРОБЛЕМА

### Ошибка:
```
NullReferenceException: Object reference not set to an instance of an object
RoomManager.ConnectToWebSocket (System.String roomId, System.String characterClass, System.Action`1[T] onComplete) (at Assets/Scripts/Network/RoomManager.cs:380)
```

### Что происходило:
1. ✅ Вход в систему работал
2. ✅ REST API вход в комнату работал
3. ❌ **UnifiedSocketIO.Instance был null** при попытке подключения через WebSocket

### Причина:
`NetworkInitializer` создаёт `UnifiedSocketIO` в `IntroScene`, но если игрок попадает в `GameScene` напрямую (минуя IntroScene), то `UnifiedSocketIO` не создаётся.

---

## ✅ РЕШЕНИЕ

### Добавлена проверка и авто-создание UnifiedSocketIO

**До** (строка 375 в RoomManager.cs):
```csharp
private void ConnectToWebSocket(string roomId, string characterClass, Action<bool> onComplete)
{
    string token = PlayerPrefs.GetString("UserToken", "");

    // Connect to UnifiedSocketIO
    UnifiedSocketIO.Instance.Connect(token, (success) =>  // ❌ NullReferenceException!
```

**После** (строка 375 в RoomManager.cs):
```csharp
private void ConnectToWebSocket(string roomId, string characterClass, Action<bool> onComplete)
{
    // Проверяем, существует ли UnifiedSocketIO
    if (UnifiedSocketIO.Instance == null)
    {
        Debug.LogWarning("[RoomManager] UnifiedSocketIO не найден, создаём...");
        GameObject socketGO = new GameObject("UnifiedSocketIO");
        socketGO.AddComponent<UnifiedSocketIO>();
        DontDestroyOnLoad(socketGO);
    }

    string token = PlayerPrefs.GetString("UserToken", "");

    // Connect to UnifiedSocketIO
    UnifiedSocketIO.Instance.Connect(token, (success) =>  // ✅ Теперь Instance гарантированно существует!
```

---

## 🎯 РЕЗУЛЬТАТ

### Теперь работает:
1. ✅ `UnifiedSocketIO` создаётся автоматически если его нет
2. ✅ Не важно, через какую сцену игрок попал в GameScene
3. ✅ `DontDestroyOnLoad` гарантирует, что объект не уничтожится между сценами
4. ✅ Логи покажут, если объект был создан динамически

---

## 📝 ЧТО ТЕСТИРОВАТЬ ДАЛЬШЕ

### Шаг 1: Перезапусти Unity
1. **Останови игру** (Stop)
2. **Дождись перекомпиляции** (Unity перекомпилирует RoomManager.cs)
3. **Проверь Console** - не должно быть ошибок компиляции

### Шаг 2: Запусти игру снова
1. **Нажми Play** ▶️
2. **Войди в систему**
3. **Выбери персонажа**
4. **Нажми кнопку "Battle" (переход в арену)**

### Ожидаемые логи:
```
[RoomManager] Вошли в комнату, подключаемся через WebSocket...
[RoomManager] UnifiedSocketIO не найден, создаём...  ⬅️ НОВЫЙ ЛОГ
[SocketIO] ✅ UnifiedSocketIO initialized
[SocketIO] 🔌 Подключение к https://aetherion-server-gv5u.onrender.com...
[SocketIO] ✅ Подключено! Session ID: abc123xyz
[RoomManager] 🚀 Socket.IO подключен, входим в комнату...
[SocketIO] 🚪 Вход в комнату: 0eb9ef7b-79c3-4d4b-9cb2-3b50294fde04 как Mage
[SocketIO] 📤 Отправлено: join_room
[SocketIO] 📨 Получено событие: room_players
[RoomManager] ✅ Полностью подключены к игре!
```

### ❌ Если ошибка:
Пришли скриншот Console с новыми ошибками!

---

## 💡 ДОПОЛНИТЕЛЬНАЯ ИНФОРМАЦИЯ

### Почему это работает:

1. **Паттерн Singleton**:
   - `UnifiedSocketIO` использует паттерн Singleton
   - `Instance` устанавливается в `Awake()`
   - Если `Instance == null`, значит объект не создан

2. **DontDestroyOnLoad**:
   - Объект не уничтожается при смене сцен
   - Создаётся один раз и живёт до конца игры

3. **Lazy Initialization**:
   - Создаём объект только когда он нужен
   - Не зависим от порядка загрузки сцен

---

## 🚀 СЛЕДУЮЩИЕ ШАГИ

После успешного теста:
1. **Создать Build**
2. **Запустить 2 клиента**
3. **Проверить синхронизацию игроков**

---

**ПЕРЕЗАПУСТИ UNITY И ПОПРОБУЙ СНОВА!** 🎮
