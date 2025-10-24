# БЫСТРЫЙ СТАРТ: СИНХРОНИЗАЦИЯ ВИЗУАЛЬНЫХ ЭФФЕКТОВ

**Время применения:** ~15 минут
**Результат:** Все визуальные эффекты работают в мультиплеере

---

## ✅ ЧТО УЖЕ СОЗДАНО

1. ✅ `ProjectileSyncHelper.cs` - синхронизация снарядов
2. ✅ `SkillCastAnimationSync.cs` - синхронизация анимаций
3. ✅ Документация и руководства

---

## 🚀 3 ПРОСТЫХ ШАГА

### ШАГ 1: ДОБАВИТЬ СИНХРОНИЗАЦИЮ СНАРЯДОВ (5 мин)

#### 1.1 В SkillExecutor.cs

**Найти:** Метод `LaunchProjectile` (~строка 263)

**После строки:**
```csharp
GameObject projectile = Instantiate(skill.projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
```

**Вставить:**
```csharp
// SYNC PROJECTILE
ProjectileSyncHelper syncHelper = projectile.AddComponent<ProjectileSyncHelper>();
string targetSocketId = "";
if (target != null)
{
    NetworkPlayer networkTarget = target.GetComponent<NetworkPlayer>();
    if (networkTarget != null) targetSocketId = networkTarget.socketId;
}
syncHelper.SyncToServer(skill.skillId, spawnPos, direction, targetSocketId);
```

#### 1.2 В NetworkSyncManager.cs

**В метод `SubscribeToNetworkEvents()` добавить:**
```csharp
socketIO.On("projectile_spawned", OnProjectileSpawned);
```

**В конец класса добавить:**
```csharp
private void OnProjectileSpawned(string jsonData)
{
    var data = JsonConvert.DeserializeObject<ProjectileSpawnedEvent>(jsonData);
    if (data.casterSocketId == socketIO.GetSocketId()) return;

    SkillConfig[] allSkills = Resources.LoadAll<SkillConfig>("Skills");
    SkillConfig skill = System.Array.Find(allSkills, s => s.skillId == data.skillId);
    if (skill == null || skill.projectilePrefab == null) return;

    Vector3 spawnPos = new Vector3(data.spawnPosition.x, data.spawnPosition.y, data.spawnPosition.z);
    Vector3 direction = new Vector3(data.direction.x, data.direction.y, data.direction.z);
    GameObject projectile = Instantiate(skill.projectilePrefab, spawnPos, Quaternion.LookRotation(direction));

    Transform targetTransform = null;
    if (!string.IsNullOrEmpty(data.targetSocketId) && networkPlayers.ContainsKey(data.targetSocketId))
        targetTransform = networkPlayers[data.targetSocketId].transform;

    var celestial = projectile.GetComponent<CelestialProjectile>();
    var arrow = projectile.GetComponent<ArrowProjectile>();
    var proj = projectile.GetComponent<Projectile>();

    if (celestial != null) celestial.Initialize(targetTransform, 0f, direction, null, null, true, false);
    else if (arrow != null) arrow.Initialize(targetTransform, 0f, direction, null, true, false);
    else if (proj != null) proj.Initialize(targetTransform, 0f, direction, null, null);
}

[System.Serializable]
public class ProjectileSpawnedEvent
{
    public string casterSocketId;
    public int skillId;
    public Vector3Simple spawnPosition;
    public Vector3Simple direction;
    public string targetSocketId;
}
```

#### 1.3 В Server/server.js

**Добавить обработчик (после других socket.on()):**
```javascript
socket.on('projectile_spawned', (data) => {
    try {
        const projectileData = JSON.parse(data);
        socket.to(player.roomId).emit('projectile_spawned', JSON.stringify({
            casterSocketId: socket.id,
            skillId: projectileData.skillId,
            spawnPosition: projectileData.spawnPosition,
            direction: projectileData.direction,
            targetSocketId: projectileData.targetSocketId || ''
        }));
    } catch (err) {
        console.error('[Projectile] Error:', err);
    }
});
```

---

### ШАГ 2: ДОБАВИТЬ СИНХРОНИЗАЦИЮ АНИМАЦИЙ (5 мин)

#### 2.1 В SkillExecutor.cs (метод Awake)

**Добавить в конец метода:**
```csharp
if (GetComponent<SkillCastAnimationSync>() == null)
{
    gameObject.AddComponent<SkillCastAnimationSync>();
}
```

#### 2.2 В SkillExecutor.cs (метод ExecuteSkill)

**После проверок, перед `manaSystem.UseMana()`:**
```csharp
// SYNC ANIMATION
SkillCastAnimationSync animSync = GetComponent<SkillCastAnimationSync>();
if (animSync != null && !string.IsNullOrEmpty(skill.animationTrigger))
{
    animSync.PlayCastAnimation(skill.animationTrigger, skill.animationSpeed, skill.castTime);
}
```

#### 2.3 В NetworkSyncManager.cs

**В `SubscribeToNetworkEvents()`:**
```csharp
socketIO.On("skill_cast_animation", OnSkillCastAnimation);
```

**В конец класса:**
```csharp
private void OnSkillCastAnimation(string jsonData)
{
    var data = JsonConvert.DeserializeObject<SkillCastAnimationEvent>(jsonData);
    if (data.casterSocketId == socketIO.GetSocketId()) return;

    GameObject playerObj = null;
    if (networkPlayers.ContainsKey(data.casterSocketId))
        playerObj = networkPlayers[data.casterSocketId].gameObject;
    if (playerObj == null) return;

    SkillCastAnimationSync animSync = playerObj.GetComponent<SkillCastAnimationSync>();
    if (animSync != null)
        animSync.PlayRemoteCastAnimation(data.animationTrigger, data.animationSpeed);
}

[System.Serializable]
public class SkillCastAnimationEvent
{
    public string casterSocketId;
    public string animationTrigger;
    public float animationSpeed;
    public float castTime;
}
```

#### 2.4 В Server/server.js

```javascript
socket.on('skill_cast_animation', (data) => {
    try {
        const animData = JSON.parse(data);
        socket.to(player.roomId).emit('skill_cast_animation', JSON.stringify({
            casterSocketId: socket.id,
            animationTrigger: animData.animationTrigger,
            animationSpeed: animData.animationSpeed,
            castTime: animData.castTime
        }));
    } catch (err) {
        console.error('[CastAnim] Error:', err);
    }
});
```

---

### ШАГ 3: ТЕСТИРОВАНИЕ (5 мин)

#### 3.1 Запуск
1. Запустить сервер: `cd Server && npm start`
2. Запустить два клиента Unity
3. Оба подключиться к одной комнате

#### 3.2 Быстрый тест
- [ ] **Fireball** - виден снаряд у обоих?
- [ ] **Rain of Arrows** - видны все 3 стрелы?
- [ ] **Battle Rage** - видна огненная аура?
- [ ] **Teleport** - виден дымок?

#### 3.3 Если работает ✅
Продолжить тестирование по `ALL_27_SKILLS_TESTING_CHECKLIST.md`

#### 3.4 Если НЕ работает ❌
Проверить логи:
- Unity Console: `[ProjectileSync]`, `[CastAnim]`, `[NetworkSync]`
- Server Console: `[Projectile]`, `[CastAnim]`

---

## 📋 ФАЙЛЫ ДЛЯ ИЗМЕНЕНИЯ

| Файл | Изменения | Строк |
|------|-----------|-------|
| `Assets/Scripts/Skills/SkillExecutor.cs` | +15 строк | 2 места |
| `Assets/Scripts/Network/NetworkSyncManager.cs` | +60 строк | 2 метода + 2 класса |
| `Server/server.js` | +30 строк | 2 обработчика |

---

## ✅ ЧТО БУДЕТ РАБОТАТЬ

После применения:
- ✅ Все снаряды видны обоим игрокам
- ✅ Анимации каста синхронизированы
- ✅ Trail Renderer и Point Light на снарядах
- ✅ Аур эффекты (уже работали)
- ✅ Stun/Root визуализация (уже работала)
- ✅ Все 27 скиллов полностью синхронизированы

---

## 📚 ПОЛНАЯ ДОКУМЕНТАЦИЯ

Если нужны детали:
1. `MULTIPLAYER_VISUAL_EFFECTS_SOLUTION.md` - полное решение
2. `VISUAL_EFFECTS_SYNC_COMPLETE_GUIDE.md` - детальное руководство
3. `ALL_27_SKILLS_TESTING_CHECKLIST.md` - чек-лист тестирования
4. `QUICK_PROJECTILE_SYNC_GUIDE.md` - только снаряды

---

**Время:** ~15 минут
**Сложность:** Низкая (копировать-вставить)
**Результат:** Полная синхронизация визуальных эффектов! 🎉
