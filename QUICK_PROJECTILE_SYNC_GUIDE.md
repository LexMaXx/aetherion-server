# БЫСТРОЕ ИСПРАВЛЕНИЕ СИНХРОНИЗАЦИИ СНАРЯДОВ

## ✅ Создан helper компонент: ProjectileSyncHelper.cs

Этот компонент автоматически синхронизирует снаряды с сервером.

## 📝 ИНСТРУКЦИЯ ПО ПРИМЕНЕНИЮ

### Шаг 1: Добавить синхронизацию в SkillExecutor.cs

**Откройте файл:** `Assets/Scripts/Skills/SkillExecutor.cs`

**Найдите метод** `LaunchProjectile` (около строки 263)

**После строки:**
```csharp
GameObject projectile = Instantiate(skill.projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
```

**Добавьте следующий код:**
```csharp
// 🚀 SYNC: Add sync helper to projectile
ProjectileSyncHelper syncHelper = projectile.AddComponent<ProjectileSyncHelper>();
string targetSocketId = "";
if (target != null)
{
    NetworkPlayer networkTarget = target.GetComponent<NetworkPlayer>();
    if (networkTarget != null)
    {
        targetSocketId = networkTarget.socketId;
    }
}
syncHelper.SyncToServer(skill.skillId, spawnPos, direction, targetSocketId);
```

### Шаг 2: Добавить обработку в NetworkSyncManager.cs

**Откройте файл:** `Assets/Scripts/Network/NetworkSyncManager.cs`

**В метод** `SubscribeToNetworkEvents()` добавьте:
```csharp
socketIO.On("projectile_spawned", OnProjectileSpawned);
```

**Добавьте новый метод в класс:**
```csharp
private void OnProjectileSpawned(string jsonData)
{
    try
    {
        var data = JsonConvert.DeserializeObject<ProjectileSpawnedEvent>(jsonData);

        Debug.Log($"[NetworkSync] 🚀 Received projectile from {data.casterSocketId}: skillId={data.skillId}");

        // Don't create projectile for ourselves (we already have it)
        if (data.casterSocketId == socketIO.GetSocketId())
        {
            return;
        }

        // Load skill config by ID
        SkillConfig[] allSkills = Resources.LoadAll<SkillConfig>("Skills");
        SkillConfig skill = null;
        foreach (SkillConfig s in allSkills)
        {
            if (s.skillId == data.skillId)
            {
                skill = s;
                break;
            }
        }

        if (skill == null || skill.projectilePrefab == null)
        {
            Debug.LogWarning($"[NetworkSync] Projectile prefab not found for skillId={data.skillId}");
            return;
        }

        // Create projectile visually
        Vector3 spawnPos = new Vector3(data.spawnPosition.x, data.spawnPosition.y, data.spawnPosition.z);
        Vector3 direction = new Vector3(data.direction.x, data.direction.y, data.direction.z);

        GameObject projectile = Instantiate(skill.projectilePrefab, spawnPos, Quaternion.LookRotation(direction));

        // Initialize for visual only (NO damage from network projectiles)
        Projectile proj = projectile.GetComponent<Projectile>();
        ArrowProjectile arrow = projectile.GetComponent<ArrowProjectile>();
        CelestialProjectile celestial = projectile.GetComponent<CelestialProjectile>();

        Transform targetTransform = null;
        if (!string.IsNullOrEmpty(data.targetSocketId))
        {
            if (networkPlayers.ContainsKey(data.targetSocketId))
            {
                targetTransform = networkPlayers[data.targetSocketId].transform;
            }
        }

        // Initialize with 0 damage (visual only)
        if (celestial != null)
        {
            celestial.Initialize(targetTransform, 0f, direction, null, null, true, false);
        }
        else if (arrow != null)
        {
            arrow.Initialize(targetTransform, 0f, direction, null, true, false);
        }
        else if (proj != null)
        {
            proj.Initialize(targetTransform, 0f, direction, null, null);
        }

        Debug.Log($"[NetworkSync] ✅ Visual projectile created");
    }
    catch (System.Exception e)
    {
        Debug.LogError($"[NetworkSync] Error creating projectile: {e.Message}");
    }
}
```

**Добавьте класс события (в конец файла перед закрывающей скобкой namespace):**
```csharp
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

### Шаг 3: Обновить серверную часть

**Откройте файл:** `Server/server.js`

**Найдите секцию Socket.IO событий и добавьте:**
```javascript
// Handle projectile spawned
socket.on('projectile_spawned', (data) => {
    try {
        const projectileData = JSON.parse(data);
        console.log(`[Projectile] ${player.username} spawned projectile: skillId=${projectileData.skillId}`);

        // Broadcast to all players in room except sender
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

## 🎯 РЕЗУЛЬТАТ

После применения всех изменений:

✅ Все игроки видят летящие снаряды (Fireball, Arrow, Soul Drain, etc.)
✅ Снаряды правильно наводятся на цели (homing)
✅ Trail Renderer и Point Light видны всем
✅ Нет двойного урона (сетевые снаряды имеют damage = 0)
✅ Работает для всех 27 скиллов с ProjectileDamage типом

## 🧪 ТЕСТИРОВАНИЕ

1. Запустить два клиента
2. Игрок A использует Fireball → Игрок B видит летящий огненный шар
3. Игрок A использует Rain of Arrows → Игрок B видит все 3 стрелы
4. Игрок A использует Soul Drain → Игрок B видит фиолетовый череп
5. Проверить что урон приходит только один раз (не дублируется)

## ⚠️ ВАЖНО

- Сетевые снаряды создаются только визуально (damage = 0)
- Урон наносится только от локального снаряда
- Сервер отправляет результат урона через отдельное событие
- Это предотвращает читы и дублирование урона

## 📁 ФАЙЛЫ КОТОРЫЕ НУЖНО ИЗМЕНИТЬ

1. ✅ `Assets/Scripts/Skills/ProjectileSyncHelper.cs` - **УЖЕ СОЗДАН**
2. ⚠️ `Assets/Scripts/Skills/SkillExecutor.cs` - добавить 10 строк кода
3. ⚠️ `Assets/Scripts/Network/NetworkSyncManager.cs` - добавить метод и класс
4. ⚠️ `Server/server.js` - добавить обработчик события

Следуйте инструкциям выше для каждого файла.
