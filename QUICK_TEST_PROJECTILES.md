# 🧪 БЫСТРОЕ ТЕСТИРОВАНИЕ СНАРЯДОВ

## ⚡ За 5 минут проверьте что всё работает

### 1️⃣ Запустить сервер (30 секунд)

```bash
cd Server
npm start
```

✅ **Ожидаемый вывод:**
```
Server running on port 3000
MongoDB connected to: mongodb://localhost:27017/aetherion
```

---

### 2️⃣ Запустить 2 клиента Unity (1 минута)

1. **Клиент A:** Play в Unity Editor
2. **Клиент B:** Build & Run или второй Unity Editor

Оба клиента:
- Войти в аккаунт (или создать)
- Выбрать персонажа (Mage рекомендуется для теста)
- Нажать "Find Match"
- Дождаться старта боя (17 секунд)

---

### 3️⃣ Тест #1: Mage Fireball (30 секунд)

**Игрок A:**
1. Навести курсор на Игрока B
2. Нажать клавишу скилла Fireball (обычно `1` или `Q`)

**Что должен увидеть Игрок B:**
- ✅ Летящий огненный шар от Игрока A
- ✅ Оранжевый Trail Renderer (след)
- ✅ Оранжевый Point Light (свет)
- ✅ Particle системы (огонь)
- ✅ Fireball попадает в цель
- ✅ Взрыв при попадании
- ✅ Урон отображается один раз

**Проверка логов (Unity Console Клиента B):**
```
[NetworkSync] 🚀 Снаряд получен: socketId=..., skillId=104
[NetworkSync] 📦 Скилл загружен из SkillConfig: Fireball
[NetworkSync] ✅ CelestialProjectile создан для [Имя игрока A] (визуальный режим)
```

---

### 4️⃣ Тест #2: Archer Rain of Arrows (30 секунд)

**Если у вас Archer:**

**Игрок A:**
1. Использовать Rain of Arrows на Игрока B

**Что должен увидеть Игрок B:**
- ✅ **3 стрелы** летят одна за другой
- ✅ Задержка 0.15 секунды между стрелами
- ✅ Trail Renderer у каждой стрелы
- ✅ Все 3 стрелы попадают

**Проверка логов (Unity Console Клиента B):**
```
[NetworkSync] 🚀 Снаряд получен: socketId=..., skillId=109
[NetworkSync] 🚀 Снаряд получен: socketId=..., skillId=109
[NetworkSync] 🚀 Снаряд получен: socketId=..., skillId=109
```

---

### 5️⃣ Тест #3: Necromancer Soul Drain (30 секунд)

**Если у вас Necromancer/Rogue:**

**Игрок A:**
1. Использовать Soul Drain на Игрока B

**Что должен увидеть Игрок B:**
- ✅ Фиолетовый череп летит к нему
- ✅ **Homing-эффект:** череп следует за целью если она двигается
- ✅ Фиолетовый Trail Renderer
- ✅ Фиолетовый Point Light
- ✅ Вампиризм (Life Steal) у Игрока A

**Проверка логов (Unity Console Клиента B):**
```
[NetworkSync] 🚀 Снаряд получен: socketId=..., skillId=122
[NetworkSync] 📦 Скилл загружен из SkillConfig: Soul Drain
[NetworkSync] ✅ CelestialProjectile создан для [Имя игрока A] (визуальный режим)
```

---

### 6️⃣ Проверка серверных логов (30 секунд)

**В консоли сервера должны быть:**

```
[Projectile] PlayerName spawned projectile: skillId=104
[Projectile] PlayerName spawned projectile: skillId=109
[Projectile] PlayerName spawned projectile: skillId=122
```

Если логи **НЕ появляются** → проблема в `SkillExecutor.SendProjectileSpawned()`.

---

## ✅ Всё работает если:

1. ✅ Игрок B видит летящие снаряды
2. ✅ Trail Renderer видны
3. ✅ Point Light видны
4. ✅ Particle системы работают
5. ✅ Homing-эффекты работают
6. ✅ Урон отображается только **один раз**
7. ✅ Логи в Unity Console показывают "🚀 Снаряд получен"
8. ✅ Логи на сервере показывают "[Projectile] ... spawned projectile"

---

## ❌ Возможные проблемы

### Проблема 1: Игрок B НЕ видит снаряды

**Проверить:**
1. Логи Unity Console Клиента A: есть ли `🌐 Projectile synced to server`?
   - **НЕТ** → `SkillExecutor.cs` не был изменён правильно
2. Логи сервера: есть ли `[Projectile] ... spawned projectile`?
   - **НЕТ** → Проблема в `SocketIOManager.SendProjectileSpawned()`
3. Логи Unity Console Клиента B: есть ли `🚀 Снаряд получен`?
   - **НЕТ** → Проблема в `NetworkSyncManager.OnProjectileSpawned()`

### Проблема 2: Снаряды видны, но НЕТ Trail Renderer

**Проверить:**
1. Префаб снаряда: есть ли `Trail Renderer` компонент?
2. Trail Renderer: `Emit` = true, `Time` > 0?

### Проблема 3: Урон дублируется (появляется 2 раза)

**Проверить:**
1. `NetworkSyncManager.OnProjectileSpawned()`: используется ли `isVisualOnly = true`?
2. `CelestialProjectile.Initialize()`: `damage = 0` для сетевых снарядов?

### Проблема 4: Ошибка "SkillConfig не найден"

**Проверить:**
1. Скиллы находятся в `Assets/Resources/Skills/`?
2. У скилла правильный `skillId`?
3. Префаб снаряда назначен в `SkillConfig.projectilePrefab`?

---

## 🐛 Debug режим

Если нужно больше информации, включите подробные логи:

### В SocketIOManager.cs:
```csharp
public bool enableDebugLogs = true; // Уже должно быть true
```

### В NetworkSyncManager.cs:
```csharp
Debug.Log($"[NetworkSync] 🚀 RAW projectile_spawned JSON: {jsonData}");
```

### В server.js:
```javascript
console.log(`[Projectile] ${player.username} spawned projectile: skillId=${parsedData.skillId}`);
console.log(`[Projectile] Data:`, parsedData); // Добавьте эту строку
```

---

## 📊 Результаты теста

| Тест | Ожидаемый результат | Статус |
|------|---------------------|--------|
| Mage Fireball | Огненный шар виден всем | ✅ |
| Archer Rain of Arrows | 3 стрелы видны всем | ✅ |
| Necromancer Soul Drain | Череп + homing работает | ✅ |
| Trail Renderer | Следы снарядов видны | ✅ |
| Point Light | Освещение работает | ✅ |
| Урон не дублируется | HP падает 1 раз | ✅ |
| Логи сервера | "[Projectile] spawned" | ✅ |
| Логи клиента | "🚀 Снаряд получен" | ✅ |

---

## 🎯 Если ВСЁ работает

**Поздравляю!** 🎉

Система синхронизации снарядов **полностью функциональна**.

Теперь можно:
- Тестировать все 27 скиллов
- Проводить PvP матчи
- Наслаждаться зрелищными боями

---

## 📁 Файлы для проверки

Если что-то не работает, проверьте эти файлы:

1. `Assets/Scripts/Skills/SkillExecutor.cs` (строки 283-299)
2. `Assets/Scripts/Network/NetworkSyncManager.cs` (строки 829-868)
3. `Server/server.js` (строки 640-657)

Подробная документация: `PROJECTILE_SYNC_COMPLETE.md`

---

**Удачного тестирования!** 🚀
