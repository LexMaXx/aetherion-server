# ИСПРАВЛЕНИЕ СИНХРОНИЗАЦИИ СНАРЯДОВ И ВИЗУАЛЬНЫХ ЭФФЕКТОВ

## Проблема
Снаряды и визуальные эффекты не видны другим игрокам в мультиплеере, потому что:
1. `SendProjectileSpawned` не вызывается в LaunchProjectile()
2. Визуальные эффекты каста не синхронизируются
3. Эффекты попадания (explosions) не отправляются на сервер

## Решение 1: Добавить синхронизацию снарядов

### В файле `Assets/Scripts/Skills/SkillExecutor.cs`

**После строки 281** (после `GameObject projectile = Instantiate...`):

```csharp
        GameObject projectile = Instantiate(skill.projectilePrefab, spawnPos, Quaternion.LookRotation(direction));

        // 🚀 НОВОЕ: Синхронизация снаряда в мультиплеере
        string targetSocketId = "";
        if (target != null)
        {
            NetworkPlayer networkTarget = target.GetComponent<NetworkPlayer>();
            if (networkTarget != null)
            {
                targetSocketId = networkTarget.socketId;
            }
        }

        // Отправляем на сервер для отображения у других игроков
        SocketIOManager socketIO = SocketIOManager.Instance;
        if (socketIO != null && socketIO.IsConnected)
        {
            socketIO.SendProjectileSpawned(skill.skillId, spawnPos, direction, targetSocketId);
            Log($"🌐 Снаряд отправлен на сервер: {skill.skillName}");
        }

        // Try CelestialProjectile first (for mage)
        CelestialProjectile celestialProj = projectile.GetComponent<CelestialProjectile>();
```

## Решение 2: Добавить обработку снарядов на удалённых клиентах

### В файле `Assets/Scripts/Network/NetworkSyncManager.cs`

Нужно добавить обработчик события `projectile_spawned` от сервера, чтобы другие игроки видели летящие снаряды.

**Добавить в SubscribeToNetworkEvents():**

```csharp
socketIO.On("projectile_spawned", OnProjectileSpawned);
```

**Добавить новый метод:**

```csharp
private void OnProjectileSpawned(string jsonData)
{
    try
    {
        var data = JsonConvert.DeserializeObject<ProjectileSpawnedEvent>(jsonData);

        Debug.Log($"[NetworkSync] 🚀 Получен снаряд от {data.casterSocketId}: skillId={data.skillId}");

        // Не создаём снаряд для себя (у нас он уже есть)
        if (data.casterSocketId == socketIO.GetSocketId())
        {
            Debug.Log("[NetworkSync] Это наш снаряд, пропускаем");
            return;
        }

        // Найти SkillConfig по ID
        SkillConfig skill = LoadSkillById(data.skillId);
        if (skill == null || skill.projectilePrefab == null)
        {
            Debug.LogWarning($"[NetworkSync] Не найден префаб снаряда для skillId={data.skillId}");
            return;
        }

        // Создать снаряд визуально
        Vector3 spawnPos = new Vector3(data.spawnPosition.x, data.spawnPosition.y, data.spawnPosition.z);
        Vector3 direction = new Vector3(data.direction.x, data.direction.y, data.direction.z);

        GameObject projectile = Instantiate(skill.projectilePrefab, spawnPos, Quaternion.LookRotation(direction));

        // Инициализировать только для визуального отображения (БЕЗ урона)
        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj != null)
        {
            Transform targetTransform = null;
            if (!string.IsNullOrEmpty(data.targetSocketId))
            {
                // Найти цель среди сетевых игроков
                if (networkPlayers.ContainsKey(data.targetSocketId))
                {
                    targetTransform = networkPlayers[data.targetSocketId].transform;
                }
            }

            // Инициализируем только визуально (damage = 0, isFromNetwork = true)
            proj.Initialize(targetTransform, 0f, direction, null, null);
            proj.DisableDamage(); // Метод чтобы снаряд не наносил урон
        }

        Debug.Log($"[NetworkSync] ✅ Снаряд создан визуально");
    }
    catch (System.Exception e)
    {
        Debug.LogError($"[NetworkSync] Ошибка при создании снаряда: {e.Message}");
    }
}

// Вспомогательный метод для загрузки скилла по ID
private SkillConfig LoadSkillById(int skillId)
{
    SkillConfig[] allSkills = Resources.LoadAll<SkillConfig>("Skills");
    foreach (SkillConfig skill in allSkills)
    {
        if (skill.skillId == skillId)
        {
            return skill;
        }
    }
    return null;
}
```

**Добавить класс события:**

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

## Решение 3: Добавить метод DisableDamage в снарядах

### В файле `Assets/Scripts/Player/Projectile.cs`

```csharp
private bool isNetworkVisualOnly = false; // Флаг для сетевых снарядов

public void DisableDamage()
{
    isNetworkVisualOnly = true;
}

// В методе HitTarget() в начале:
private void HitTarget()
{
    if (isNetworkVisualOnly)
    {
        // Это визуальный снаряд, не наносим урон
        Destroy(gameObject);
        return;
    }

    // ... остальной код
}
```

## Решение 4: Синхронизация визуальных эффектов каста

### В файле `Assets/Scripts/Skills/SkillExecutor.cs`

Изменить метод `SpawnEffect`:

**Текущая версия:**
```csharp
private void SpawnEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation)
```

**Новая версия с синхронизацией:**
```csharp
private void SpawnEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation, bool syncToNetwork = false)
{
    if (effectPrefab == null) return;

    GameObject effect = Instantiate(effectPrefab, position, rotation);

    // 🌐 Синхронизация в мультиплеере
    if (syncToNetwork)
    {
        SocketIOManager socketIO = SocketIOManager.Instance;
        if (socketIO != null && socketIO.IsConnected)
        {
            socketIO.SendVisualEffect(
                "cast",
                effectPrefab.name,
                position,
                rotation,
                "", // targetSocketId пусто для эффектов каста
                3f, // duration
                null
            );
            Log($"🎨 Эффект каста отправлен на сервер: {effectPrefab.name}");
        }
    }

    Destroy(effect, 3f);
}
```

**В LaunchProjectile() изменить вызов:**
```csharp
SpawnEffect(skill.castEffectPrefab, spawnPos, Quaternion.identity, true); // true = синхронизировать
```

## Результат

После применения всех исправлений:
- ✅ Все игроки видят летящие снаряды
- ✅ Снаряды отображаются корректно с Trail Renderer и Point Light
- ✅ Эффекты каста (cast effects) видны всем игрокам
- ✅ Снаряды других игроков не наносят двойной урон (только визуал)
- ✅ Корректная синхронизация для всех 27 скиллов

## Тестирование

1. Запустить два клиента
2. Использовать Fireball (Mage) - должен быть виден у обоих
3. Использовать Rain of Arrows (Archer) - все 3 стрелы видны
4. Проверить Life Steal эффект (Soul Drain) - фиолетовый череп виден обоим
5. Убедиться что урон приходит только один раз (от сервера)
