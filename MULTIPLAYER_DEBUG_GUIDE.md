# 🔍 ДИАГНОСТИКА МУЛЬТИПЛЕЕРА

## 🚨 ПРОБЛЕМЫ:

1. **F12 консоль пустая** - логи не отображаются
2. **Игроки не видят друг друга** - не создаются network players
3. **Враги не синхронизируются** - убитый враг не исчезает у другого игрока

---

## ✅ РЕШЕНИЯ:

### ПРОБЛЕМА 1: F12 Консоль пустая

**Причина**: Логи собираются, но текст не отображается (возможно из-за шрифта или GlobalTextStyleManager)

**Быстрое решение**:
1. Откройте Unity Console (Window → General → Console)
2. Там ВСЕ логи видны
3. F12 консоль - это просто дубликат для .exe билдов

**Долгое решение** (если нужна F12 консоль):
- Проблема в том что logText использует обычный Text вместо TextMeshPro
- GlobalTextStyleManager может ломать шрифт Arial
- Нужно переписать DebugConsole чтобы использовал TextMeshPro

---

### ПРОБЛЕМА 2: Игроки не видят друг друга

**Причина**: NetworkSyncManager не может создать network players потому что **prefab'ы не назначены**!

**Проверка**:
1. Откройте ArenaScene
2. Найдите в Hierarchy: GameObject с компонентом **NetworkSyncManager**
3. В Inspector проверьте секцию "Character Prefabs":
   - Warrior Prefab - **должен быть назначен**
   - Mage Prefab - **должен быть назначен**
   - Archer Prefab - **должен быть назначен**
   - Rogue Prefab - **должен быть назначен**
   - Paladin Prefab - **должен быть назначен**

**Если все NULL**:
```
[NetworkSync] Префаб для класса Warrior не найден!
```

**Решение**:
1. Найдите ваши prefab'ы персонажей в `Assets/Prefabs/Characters/` (или где они находятся)
2. Перетащите их в соответствующие поля NetworkSyncManager
3. Сохраните сцену (Ctrl+S)

**Если prefab'ов нет**:
- Нужно создать prefab'ы из моделей персонажей
- Или использовать программную загрузку из Resources

---

### ПРОБЛЕМА 3: Враги не синхронизируются

**Причина**: В вашем коде **НЕТ синхронизации врагов**!

NetworkSyncManager синхронизирует только:
- ✅ Игроков (players)
- ❌ Врагов (enemies) - НЕ СИНХРОНИЗИРУЮТСЯ

**Что происходит**:
1. Игрок 1 убивает врага
2. Враг исчезает у Игрока 1
3. У Игрока 2 враг НЕ исчезает (потому что нет синхронизации)

**Решение**:

Нужно добавить синхронизацию врагов на сервере:

#### На сервере (Node.js):

```javascript
// Добавить в server.js или отдельный файл enemy_sync.js

// Хранилище состояния врагов для каждой комнаты
const roomEnemies = new Map(); // roomId => { enemyId: { health, position, alive } }

// Когда игрок убивает врага
socket.on('enemy_killed', (data) => {
  const { roomId, enemyId, killerId } = data;

  // Обновляем состояние врага
  if (!roomEnemies.has(roomId)) {
    roomEnemies.set(roomId, new Map());
  }

  const enemies = roomEnemies.get(roomId);
  enemies.set(enemyId, { alive: false, health: 0 });

  // Отправляем всем игрокам в комнате
  io.to(roomId).emit('enemy_killed', {
    enemyId,
    killerId,
    timestamp: Date.now()
  });

  console.log(`[Room ${roomId}] Enemy ${enemyId} killed by ${killerId}`);
});

// Когда игрок атакует врага
socket.on('enemy_damaged', (data) => {
  const { roomId, enemyId, damage, newHealth } = data;

  // Отправляем всем игрокам в комнате
  io.to(roomId).emit('enemy_health_update', {
    enemyId,
    health: newHealth,
    damage,
    timestamp: Date.now()
  });
});

// Респавн врага
socket.on('enemy_respawned', (data) => {
  const { roomId, enemyId, position } = data;

  if (roomEnemies.has(roomId)) {
    const enemies = roomEnemies.get(roomId);
    enemies.set(enemyId, { alive: true, health: 100, position });
  }

  io.to(roomId).emit('enemy_respawned', {
    enemyId,
    position,
    timestamp: Date.now()
  });
});
```

#### В Unity (NetworkSyncManager.cs):

Добавьте подписку на события врагов:

```csharp
// В SubscribeToNetworkEvents()
SimpleWebSocketClient.Instance.On("enemy_killed", OnEnemyKilled);
SimpleWebSocketClient.Instance.On("enemy_health_update", OnEnemyHealthUpdate);
SimpleWebSocketClient.Instance.On("enemy_respawned", OnEnemyRespawned);

// Новые обработчики
private void OnEnemyKilled(string jsonData)
{
    var data = JsonUtility.FromJson<EnemyKilledData>(jsonData);
    Debug.Log($"[NetworkSync] Враг убит: {data.enemyId}");

    // Найти врага в сцене и уничтожить
    GameObject enemy = GameObject.Find(data.enemyId);
    if (enemy != null)
    {
        Destroy(enemy);
    }
}

private void OnEnemyHealthUpdate(string jsonData)
{
    var data = JsonUtility.FromJson<EnemyHealthData>(jsonData);

    // Найти врага и обновить здоровье
    GameObject enemy = GameObject.Find(data.enemyId);
    if (enemy != null)
    {
        var healthSystem = enemy.GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.SetHealth(data.health);
        }
    }
}

private void OnEnemyRespawned(string jsonData)
{
    var data = JsonUtility.FromJson<EnemyRespawnData>(jsonData);

    // Создать врага заново
    // TODO: реализовать логику респавна
}

// Новые data классы
[Serializable]
public class EnemyKilledData
{
    public string enemyId;
    public string killerId;
}

[Serializable]
public class EnemyHealthData
{
    public string enemyId;
    public int health;
    public float damage;
}

[Serializable]
public class EnemyRespawnData
{
    public string enemyId;
    public PositionData position;
}
```

#### В Unity (EnemyHealthSystem.cs или где враги получают урон):

Отправляйте события на сервер:

```csharp
// Когда враг получает урон
public void TakeDamage(int damage)
{
    currentHealth -= damage;

    // Отправить на сервер
    if (SimpleWebSocketClient.Instance != null)
    {
        SimpleWebSocketClient.Instance.Emit("enemy_damaged", JsonUtility.ToJson(new
        {
            roomId = PlayerPrefs.GetString("CurrentRoomId"),
            enemyId = gameObject.name, // Убедитесь что у каждого врага уникальный name!
            damage = damage,
            newHealth = currentHealth
        }));
    }

    if (currentHealth <= 0)
    {
        OnDeath();
    }
}

// Когда враг умирает
void OnDeath()
{
    // Отправить на сервер
    if (SimpleWebSocketClient.Instance != null)
    {
        SimpleWebSocketClient.Instance.Emit("enemy_killed", JsonUtility.ToJson(new
        {
            roomId = PlayerPrefs.GetString("CurrentRoomId"),
            enemyId = gameObject.name,
            killerId = SimpleWebSocketClient.Instance.SessionId
        }));
    }

    // Уничтожить врага локально (другие игроки получат событие и тоже уничтожат)
    Destroy(gameObject);
}
```

---

## 📋 ЧЕКЛИСТ ДЛЯ ОТЛАДКИ:

### 1. Проверка NetworkSyncManager

- [ ] GameObject "NetworkSyncManager" существует в ArenaScene
- [ ] Все prefab'ы (Warrior, Mage, Archer, Rogue, Paladin) назначены
- [ ] В Unity Console видны логи `[NetworkSync] ✅ Подписан на сетевые события`
- [ ] В Unity Console видны логи `[NetworkSync] Получено состояние комнаты: X игроков`

### 2. Проверка WebSocket

- [ ] В Unity Console видны логи `[SimpleWS] ✅ Connected`
- [ ] В Unity Console видны логи `[SimpleWS] 📊 Room state: X players`
- [ ] CurrentRoomId сохранён в PlayerPrefs
- [ ] Polling работает (логи каждую секунду)

### 3. Проверка синхронизации игроков

- [ ] Когда второй игрок заходит, видно лог `[NetworkSync] 🎭 Spawning network player`
- [ ] В Hierarchy появляется GameObject `NetworkPlayer_USERNAME`
- [ ] Игроки видят друг друга в игре

### 4. Проверка синхронизации врагов

- [ ] Враги имеют уникальные имена (name)
- [ ] При уроне врагу отправляется событие на сервер
- [ ] При смерти врага отправляется событие на сервер
- [ ] Другой игрок получает событие и уничтожает врага

---

## 🛠️ БЫСТРАЯ ДИАГНОСТИКА:

### Откройте Unity Console и ищите:

#### Хорошие логи ✅:
```
[NetworkSync] ✅ Подписан на сетевые события
[NetworkSync] Получено состояние комнаты: 2 игроков
[NetworkSync] My sessionId: ABC123
[NetworkSync] Player in room: player1 (socketId: ABC123, class: Warrior)
[NetworkSync] Player in room: player2 (socketId: DEF456, class: Mage)
[NetworkSync] ⏭️ Skipping ourselves: player1
[NetworkSync] 🎭 Spawning network player: player2
[NetworkSync] ✅ Создан сетевой игрок: player2 (Mage)
```

#### Плохие логи ❌:
```
[NetworkSync] ❌ Префаб для класса Warrior не найден!
[NetworkSync] WebSocketClient не найден!
[NetworkSync] Не в мультиплеере, отключаем синхронизацию
```

---

## 🎯 ЧТО ДЕЛАТЬ СЕЙЧАС:

1. **Назначьте prefab'ы в NetworkSyncManager**
   - Это самая частая причина!

2. **Проверьте Unity Console**
   - Ищите ошибки и предупреждения

3. **Добавьте синхронизацию врагов**
   - Без этого враги не будут синхронизироваться между игроками

4. **Используйте Unity Console вместо F12**
   - В Editor всегда используйте Unity Console
   - F12 только для .exe билдов

---

Готово! Исправьте prefab'ы и враги будут синхронизироваться! 🎮✨
