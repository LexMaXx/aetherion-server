# ✅ ЧЕКЛИСТ ПЕРЕД ЗАПУСКОМ

## 📋 Финальная проверка перед тестированием

### 1. Изменённые файлы

Убедитесь, что следующие файлы содержат изменения:

#### ✅ SkillExecutor.cs (строки 283-299)
```bash
# Проверка
grep -n "🚀 SYNC: Send projectile to other players" Assets/Scripts/Skills/SkillExecutor.cs
```

**Ожидаемый результат:** `283:        // 🚀 SYNC: Send projectile to other players`

#### ✅ NetworkSyncManager.cs (строки 829-868)
```bash
# Проверка
grep -n "пробуем загрузить из SkillConfig" Assets/Scripts/Network/NetworkSyncManager.cs
```

**Ожидаемый результат:** `829:                    // Это скилл - пробуем загрузить из SkillConfig`

#### ✅ server.js (строки 640-657)
```bash
# Проверка
grep -n "Projectile.*spawned projectile" Server/server.js
```

**Ожидаемый результат:** `646:        console.log(\`[Projectile] ${player.username} spawned projectile: skillId=${parsedData.skillId}\`);`

---

### 2. Компиляция Unity

**Откройте Unity Editor и проверьте:**

- [ ] Нет ошибок компиляции (Console должна быть чистая)
- [ ] Нет предупреждений о missing references
- [ ] SkillExecutor.cs скомпилирован без ошибок
- [ ] NetworkSyncManager.cs скомпилирован без ошибок

**Если есть ошибки компиляции:**
1. Закройте Unity
2. Удалите папки `Library/`, `Temp/`, `obj/`
3. Откройте Unity снова
4. Дождитесь полной пересборки

---

### 3. Серверная часть

**Проверьте зависимости:**

```bash
cd Server
npm install
```

**Должны быть установлены:**
- ✅ express@4.18.2
- ✅ socket.io@4.6.1
- ✅ mongoose@7.0.3
- ✅ cors@2.8.5
- ✅ jsonwebtoken@9.0.0
- ✅ bcryptjs@2.4.3

**Проверьте MongoDB:**

```bash
# Windows
net start MongoDB

# Linux/Mac
sudo systemctl start mongod
```

**Запустите сервер:**

```bash
npm start
```

**Ожидаемый вывод:**
```
Server running on port 3000
MongoDB connected to: mongodb://localhost:27017/aetherion
```

---

### 4. Проверка SkillConfig файлов

**Убедитесь, что все скиллы имеют префабы снарядов:**

```bash
# Проверка количества SkillConfig файлов
ls Assets/Resources/Skills/*.asset | wc -l
```

**Ожидаемый результат:** ≥ 15 файлов

**Критические скиллы для теста:**
- [ ] `Assets/Resources/Skills/Mage_Fireball.asset` → projectilePrefab назначен
- [ ] `Assets/Resources/Skills/Archer_RainofArrows.asset` → projectilePrefab назначен
- [ ] `Assets/Resources/Skills/Rogue_SoulDrain.asset` → projectilePrefab назначен

**Откройте каждый файл в Inspector и проверьте:**
- `skillId` ≠ 0
- `projectilePrefab` назначен (не None)
- `skillName` заполнен

---

### 5. Проверка префабов снарядов

**Критические префабы:**

#### Fireball:
- [ ] `Assets/Resources/Projectiles/FireballProjectile.prefab` существует
- [ ] Компонент `CelestialProjectile` прикреплён
- [ ] `Trail Renderer` присутствует
- [ ] `Point Light` присутствует (опционально)

#### Arrow:
- [ ] `Assets/Resources/Projectiles/ArrowProjectile.prefab` существует
- [ ] Компонент `ArrowProjectile` прикреплён
- [ ] `Trail Renderer` присутствует

#### Ethereal Skull:
- [ ] `Assets/Resources/Projectiles/EtherealSkullProjectile.prefab` существует
- [ ] Компонент `CelestialProjectile` прикреплён
- [ ] `Trail Renderer` присутствует (фиолетовый)

---

### 6. Проверка NetworkSyncManager подписок

**Откройте:** `Assets/Scripts/Network/NetworkSyncManager.cs`

**Найдите метод** `SubscribeToNetworkEvents()` (около строки 120)

**Проверьте наличие:**
```csharp
SocketIOManager.Instance.On("projectile_spawned", OnProjectileSpawned);
```

- [ ] Подписка на `projectile_spawned` существует
- [ ] Метод `OnProjectileSpawned` существует (около строки 789)

---

### 7. Проверка SocketIOManager

**Откройте:** `Assets/Scripts/Network/SocketIOManager.cs`

**Найдите метод** `SendProjectileSpawned` (около строки 471)

- [ ] Метод существует
- [ ] Сигнатура: `public void SendProjectileSpawned(int skillId, Vector3 spawnPosition, Vector3 direction, string targetSocketId)`
- [ ] Использует `Emit("projectile_spawned", json)`

---

### 8. Тестирование (5 минут)

#### Шаг 1: Запустить сервер
```bash
cd Server
npm start
```

**Проверка:**
- [ ] Сервер запустился на порту 3000
- [ ] MongoDB подключена
- [ ] Нет ошибок в консоли

#### Шаг 2: Запустить клиент A (Unity Editor)
- [ ] Play в Unity Editor
- [ ] Войти в аккаунт (или создать)
- [ ] Выбрать Mage
- [ ] Нажать "Find Match"

#### Шаг 3: Запустить клиент B (Build)
- [ ] Запустить билд (или второй Unity Editor)
- [ ] Войти в другой аккаунт
- [ ] Выбрать любой класс
- [ ] Нажать "Find Match"

#### Шаг 4: Дождаться старта боя
- [ ] Оба игрока в одной комнате
- [ ] Countdown 14 → 0
- [ ] Игра началась

#### Шаг 5: Тест Fireball
**Клиент A:**
- [ ] Навести курсор на игрока B
- [ ] Нажать клавишу Fireball (обычно `1`)

**Клиент B:**
- [ ] Видит летящий огненный шар от игрока A
- [ ] Trail Renderer виден
- [ ] Particle эффекты работают
- [ ] Урон отображается один раз

#### Шаг 6: Проверка логов

**Unity Console (Клиент A):**
```
[SkillExecutor] 🌐 Projectile synced to server: skillId=104
```
- [ ] Лог присутствует

**Server Console:**
```
[Projectile] PlayerName spawned projectile: skillId=104
```
- [ ] Лог присутствует

**Unity Console (Клиент B):**
```
[NetworkSync] 🚀 Снаряд получен: socketId=..., skillId=104
[NetworkSync] 📦 Скилл загружен из SkillConfig: Fireball
[NetworkSync] ✅ CelestialProjectile создан для PlayerName (визуальный режим)
```
- [ ] Все 3 лога присутствуют

---

### 9. Возможные проблемы

#### Проблема: "SkillConfig не найден"

**Решение:**
1. Проверьте путь: `Assets/Resources/Skills/Mage_Fireball.asset`
2. Проверьте `skillId` в файле (должен быть 104)
3. Перезапустите Unity для пересборки Resources

#### Проблема: "Prefab не найден"

**Решение:**
1. Проверьте путь: `Assets/Resources/Projectiles/FireballProjectile.prefab`
2. Откройте `Mage_Fireball.asset` в Inspector
3. Назначьте `projectilePrefab` вручную

#### Проблема: "Снаряды не видны на клиенте B"

**Решение:**
1. Проверьте логи Unity Console (клиент A): есть ли `🌐 Projectile synced to server`?
   - Если НЕТ → проблема в SkillExecutor.cs
2. Проверьте логи сервера: есть ли `[Projectile] ... spawned projectile`?
   - Если НЕТ → проблема в server.js
3. Проверьте логи Unity Console (клиент B): есть ли `🚀 Снаряд получен`?
   - Если НЕТ → проблема в NetworkSyncManager.cs

#### Проблема: "Урон дублируется"

**Решение:**
1. Проверьте `NetworkSyncManager.OnProjectileSpawned()`:
   - Должен использовать `isVisualOnly = true`
   - Должен передавать `damage = 0`

#### Проблема: "Компиляция не проходит"

**Ошибка:** `'NetworkPlayer' does not contain a definition for 'socketId'`

**Решение:** NetworkPlayer должен иметь public поле `socketId`:
```csharp
public class NetworkPlayer : MonoBehaviour
{
    public string socketId;
    public string username;
    // ...
}
```

---

### 10. Финальная проверка

После успешного теста:

- [ ] Снаряды видны всем игрокам
- [ ] Trail Renderer работает
- [ ] Point Light работает (если есть)
- [ ] Particle системы работают
- [ ] Homing-эффект работает (для Soul Drain)
- [ ] Урон не дублируется
- [ ] Логи корректны на всех клиентах и сервере
- [ ] Нет ошибок в консоли Unity
- [ ] Нет ошибок на сервере

---

## ✅ ВСЁ ГОТОВО!

Если все пункты отмечены, система синхронизации снарядов **полностью функциональна**.

Можно переходить к:
- Тестированию всех 27 скиллов
- PvP матчам
- Записи демо-видео
- Публичному тестированию

---

## 📚 Документация

- **PROJECTILE_SYNC_COMPLETE.md** - Полное описание изменений
- **QUICK_TEST_PROJECTILES.md** - Быстрое тестирование за 5 минут
- **FINAL_SESSION_SUMMARY.md** - Итоговый отчёт сессии
- **DEPLOYMENT_CHECKLIST.md** - Текущий файл (чеклист)

---

**Удачного запуска!** 🚀🎮
