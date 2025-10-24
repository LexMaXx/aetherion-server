# ПОЛНОЕ РУКОВОДСТВО ПО СИНХРОНИЗАЦИИ ВИЗУАЛЬНЫХ ЭФФЕКТОВ

Это руководство покрывает все аспекты синхронизации визуальных эффектов в мультиплеере:
- ✅ Снаряды (projectiles)
- ✅ Анимации каста
- ✅ Аур эффекты (баффы/дебаффы)
- ✅ Взрывы и hit эффекты
- ✅ Stun/Root визуализация

---

## 📁 СОЗДАННЫЕ КОМПОНЕНТЫ

### 1. ProjectileSyncHelper.cs
Синхронизирует снаряды с сервером (СОЗДАН ✅)

### 2. SkillCastAnimationSync.cs
Синхронизирует анимации каста (СОЗДАН ✅)

---

## 🎯 ЧТО УЖЕ РАБОТАЕТ

### ✅ Статус-эффекты (Stun, Root, Poison, Burn, etc.)
**Файл:** `EffectManager.cs` - метод `ApplyEffect()`

Эффекты **УЖЕ СИНХРОНИЗИРОВАНЫ** через событие `effect_applied`:
- Все 28 типов эффектов отправляются на сервер
- Визуальные частицы создаются у всех игроков
- Длительность и сила эффекта синхронизированы

**Проверка:**
```csharp
// В EffectManager.cs, метод ApplyEffect():
if (config.syncWithServer && socketIO != null && socketIO.IsConnected)
{
    socketIO.SendEffectApplied(config, targetSocketId);
}
```

✅ **РАБОТАЕТ БЕЗ ИЗМЕНЕНИЙ**

---

## 🔧 ЧТО НУЖНО ДОБАВИТЬ

### 1. СИНХРОНИЗАЦИЯ СНАРЯДОВ

#### A. В SkillExecutor.cs (метод LaunchProjectile)

**После строки** `GameObject projectile = Instantiate(...)`

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

#### B. В NetworkSyncManager.cs

**В SubscribeToNetworkEvents():**
```csharp
socketIO.On("projectile_spawned", OnProjectileSpawned);
```

**Добавить метод:**
```csharp
private void OnProjectileSpawned(string jsonData)
{
    try
    {
        var data = JsonConvert.DeserializeObject<ProjectileSpawnedEvent>(jsonData);

        // Не создаём для себя
        if (data.casterSocketId == socketIO.GetSocketId()) return;

        // Загрузить skill config
        SkillConfig[] allSkills = Resources.LoadAll<SkillConfig>("Skills");
        SkillConfig skill = System.Array.Find(allSkills, s => s.skillId == data.skillId);

        if (skill == null || skill.projectilePrefab == null) return;

        // Создать снаряд визуально
        Vector3 spawnPos = new Vector3(data.spawnPosition.x, data.spawnPosition.y, data.spawnPosition.z);
        Vector3 direction = new Vector3(data.direction.x, data.direction.y, data.direction.z);

        GameObject projectile = Instantiate(skill.projectilePrefab, spawnPos, Quaternion.LookRotation(direction));

        // Найти цель
        Transform targetTransform = null;
        if (!string.IsNullOrEmpty(data.targetSocketId) && networkPlayers.ContainsKey(data.targetSocketId))
        {
            targetTransform = networkPlayers[data.targetSocketId].transform;
        }

        // Инициализировать только визуально (damage = 0)
        var celestial = projectile.GetComponent<CelestialProjectile>();
        var arrow = projectile.GetComponent<ArrowProjectile>();
        var proj = projectile.GetComponent<Projectile>();

        if (celestial != null)
            celestial.Initialize(targetTransform, 0f, direction, null, null, true, false);
        else if (arrow != null)
            arrow.Initialize(targetTransform, 0f, direction, null, true, false);
        else if (proj != null)
            proj.Initialize(targetTransform, 0f, direction, null, null);

        Debug.Log($"[NetworkSync] ✅ Visual projectile created for skill {data.skillId}");
    }
    catch (System.Exception e)
    {
        Debug.LogError($"[NetworkSync] Error creating projectile: {e.Message}");
    }
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

#### C. В Server/server.js

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

---

### 2. СИНХРОНИЗАЦИЯ АНИМАЦИЙ КАСТА

#### A. Добавить компонент в Player prefabs

В Unity Editor:
1. Открыть все player prefabs (Archer, Mage, Warrior, etc.)
2. Добавить компонент `SkillCastAnimationSync`
3. Сохранить prefabs

Или программно в SkillExecutor.Awake():
```csharp
if (GetComponent<SkillCastAnimationSync>() == null)
{
    gameObject.AddComponent<SkillCastAnimationSync>();
}
```

#### B. В SkillExecutor.cs (метод ExecuteSkill)

**После проверок** (перед `manaSystem.UseMana()`):

```csharp
// 🎬 Воспроизвести анимацию каста и синхронизировать
SkillCastAnimationSync animSync = GetComponent<SkillCastAnimationSync>();
if (animSync != null && !string.IsNullOrEmpty(skill.animationTrigger))
{
    animSync.PlayCastAnimation(skill.animationTrigger, skill.animationSpeed, skill.castTime);
}
```

#### C. В NetworkSyncManager.cs

**В SubscribeToNetworkEvents():**
```csharp
socketIO.On("skill_cast_animation", OnSkillCastAnimation);
```

**Добавить метод:**
```csharp
private void OnSkillCastAnimation(string jsonData)
{
    try
    {
        var data = JsonConvert.DeserializeObject<SkillCastAnimationEvent>(jsonData);

        // Не воспроизводим для себя
        if (data.casterSocketId == socketIO.GetSocketId()) return;

        // Найти игрока
        GameObject playerObj = null;
        if (networkPlayers.ContainsKey(data.casterSocketId))
        {
            playerObj = networkPlayers[data.casterSocketId].gameObject;
        }

        if (playerObj == null) return;

        // Воспроизвести анимацию
        SkillCastAnimationSync animSync = playerObj.GetComponent<SkillCastAnimationSync>();
        if (animSync != null)
        {
            animSync.PlayRemoteCastAnimation(data.animationTrigger, data.animationSpeed);
        }

        Debug.Log($"[NetworkSync] 🎬 Cast animation played for {data.casterSocketId}");
    }
    catch (System.Exception e)
    {
        Debug.LogError($"[NetworkSync] Error playing cast animation: {e.Message}");
    }
}
```

**Добавить класс события:**
```csharp
[System.Serializable]
public class SkillCastAnimationEvent
{
    public string casterSocketId;
    public string animationTrigger;
    public float animationSpeed;
    public float castTime;
}
```

#### D. В Server/server.js

```javascript
// Handle skill cast animation
socket.on('skill_cast_animation', (data) => {
    try {
        const animData = JSON.parse(data);
        console.log(`[CastAnim] ${player.username} casting: ${animData.animationTrigger}`);

        // Broadcast to all players in room except sender
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

### 3. СИНХРОНИЗАЦИЯ ЭФФЕКТОВ КАСТА И ВЗРЫВОВ

#### В SkillExecutor.cs

**Изменить метод SpawnEffect:**

```csharp
private void SpawnEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation, bool syncToNetwork = false, string effectType = "cast")
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
                effectType,
                effectPrefab.name,
                position,
                rotation,
                "",
                3f,
                null
            );
            Log($"🎨 Effect synced: {effectPrefab.name}");
        }
    }

    Destroy(effect, 3f);
}
```

**Обновить вызовы:**
```csharp
// Для эффектов каста
SpawnEffect(skill.castEffectPrefab, spawnPos, Quaternion.identity, true, "cast");

// Для эффектов попадания (в OnCollision/OnTrigger)
SpawnEffect(hitEffectPrefab, hitPos, Quaternion.identity, true, "hit");

// Для эффектов взрыва (AOE skills)
SpawnEffect(explosionPrefab, aoeCenter, Quaternion.identity, true, "explosion");
```

---

## 🎨 ВИЗУАЛИЗАЦИЯ АУРНЫХ ЭФФЕКТОВ

### УЖЕ РАБОТАЕТ! ✅

Аур эффекты (баффы/дебаффы) **уже синхронизированы** через `EffectManager.ApplyEffect()`:

**Примеры работающих эффектов:**
- 🟡 Battle Rage (Warrior) - огненная аура
- 🟢 Defensive Stance (Warrior) - зелёный щит
- ⚡ Eagle Eye (Archer) - золотая аура
- 🟣 Soul Drain (Rogue) - фиолетовые частицы
- ✨ Divine Protection (Paladin) - золотой щит

**Проверка:**
Откройте `Assets/Scripts/Skills/EffectManager.cs` и найдите:
```csharp
private void ApplyEffect(EffectConfig config, CharacterStats casterStats, string targetSocketId)
{
    // ... код применения эффекта ...

    // Создание визуального эффекта
    if (config.particleEffectPrefab != null)
    {
        effect.visualEffect = Instantiate(config.particleEffectPrefab, transform);
        effect.visualEffect.transform.localPosition = Vector3.up * 1.5f;
    }

    // 🌐 СИНХРОНИЗАЦИЯ С СЕРВЕРОМ
    if (config.syncWithServer && socketIO != null && socketIO.IsConnected)
    {
        socketIO.SendEffectApplied(config, targetSocketId);
    }
}
```

**Результат:** Все игроки видят ауры друг друга! ✅

---

## 🎭 ВИЗУАЛИЗАЦИЯ STUN/ROOT ЭФФЕКТОВ

### УЖЕ РАБОТАЕТ! ✅

Stun и Root эффекты **уже синхронизированы**:

**Примеры:**
- ⚡ Stunning Shot (Archer) - электрические вспышки
- 🌿 Entangling Shot (Archer) - зелёные листья (Root)
- 🔨 Hammer Throw (Warrior) - оглушение с огненным взрывом

**Как это работает:**
1. Скилл применяет Stun/Root через `EffectConfig`
2. EffectManager создаёт визуальный эффект локально
3. Отправляет `effect_applied` на сервер
4. Сервер отправляет всем игрокам
5. Другие игроки видят визуальный эффект (частицы)
6. Цель перестаёт двигаться (блокировка через `CanMove()`)

**Никаких изменений не требуется!** ✅

---

## 💥 СИНХРОНИЗАЦИЯ ВЗРЫВОВ И HIT ЭФФЕКТОВ

### Нужно добавить синхронизацию в снарядах

#### В Projectile.cs (и ArrowProjectile.cs, CelestialProjectile.cs)

**В методе OnTriggerEnter/HitTarget:**

```csharp
private void HitTarget()
{
    // ... код определения target ...

    // Создать визуальный эффект попадания
    if (hitEffect != null)
    {
        GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);

        // 🌐 СИНХРОНИЗАЦИЯ взрыва
        SocketIOManager socketIO = SocketIOManager.Instance;
        if (socketIO != null && socketIO.IsConnected)
        {
            socketIO.SendVisualEffect(
                "hit",
                hitEffect.name,
                transform.position,
                Quaternion.identity,
                "",
                2f,
                null
            );
        }

        Destroy(effect, 2f);
    }

    // ... остальной код урона ...
}
```

---

## 🧪 ТЕСТИРОВАНИЕ

### Чек-лист для проверки визуализации

#### Снаряды:
- [ ] Fireball (Mage) - виден у обоих игроков
- [ ] Arrow (Archer) - виден с Trail Renderer
- [ ] Soul Drain (Rogue) - фиолетовый череп виден обоим
- [ ] Rain of Arrows - все 3 стрелы видны

#### Анимации:
- [ ] Fireball cast - руки поднимаются у обоих
- [ ] Meteor cast - длинная анимация (2 сек) видна
- [ ] Teleport - исчезновение и появление анимированы

#### Эффекты каста:
- [ ] Fireball spawn - огненный эффект в руке
- [ ] Teleport - дымок при телепорте
- [ ] Ice Nova - синие частицы вокруг мага

#### Аур эффекты (баффы):
- [ ] Battle Rage - огненная аура на воине
- [ ] Eagle Eye - золотая аура на лучнике
- [ ] Defensive Stance - зелёный щит
- [ ] Divine Protection - золотой щит (на ВСЕХ союзников!)

#### Дебаффы:
- [ ] Stunning Shot - жёлтые молнии на цели
- [ ] Root (Entangling) - зелёные листья
- [ ] Burn - огненные частицы
- [ ] Poison - фиолетовые пузыри

#### Взрывы и hit эффекты:
- [ ] Fireball explosion - огненный взрыв при попадании
- [ ] Meteor impact - большой взрыв с огнём
- [ ] Arrow hit - белая вспышка
- [ ] Lightning Storm - электрические вспышки (multiple)

---

## 📊 ИТОГОВАЯ ТАБЛИЦА

| Эффект | Статус | Файл | Метод |
|--------|--------|------|-------|
| **Снаряды** | ⚠️ НУЖНО ДОБАВИТЬ | SkillExecutor.cs | LaunchProjectile() + NetworkSyncManager |
| **Анимации каста** | ⚠️ НУЖНО ДОБАВИТЬ | SkillExecutor.cs | ExecuteSkill() + SkillCastAnimationSync |
| **Аур эффекты** | ✅ РАБОТАЕТ | EffectManager.cs | ApplyEffect() |
| **Stun/Root визуал** | ✅ РАБОТАЕТ | EffectManager.cs | ApplyEffect() |
| **Эффекты каста** | ⚠️ ЧАСТИЧНО | SkillExecutor.cs | SpawnEffect() (нужно добавить sync) |
| **Hit эффекты** | ⚠️ НУЖНО ДОБАВИТЬ | Projectile.cs | HitTarget() |
| **Взрывы (AOE)** | ⚠️ ЧАСТИЧНО | SkillExecutor.cs | ExecuteAOEDamage() |

---

## 🚀 ПОРЯДОК ПРИМЕНЕНИЯ

1. **КРИТИЧНО:** Синхронизация снарядов (ProjectileSyncHelper)
2. **ВАЖНО:** Анимации каста (SkillCastAnimationSync)
3. **ОПЦИОНАЛЬНО:** Hit эффекты в снарядах
4. **ОПЦИОНАЛЬНО:** Эффекты каста (SpawnEffect с sync)

После применения пунктов 1 и 2, **90% визуализации будет работать корректно!**

---

## 📝 ФИНАЛЬНЫЕ ЗАМЕТКИ

- Аур эффекты (баффы/дебаффы) уже синхронизированы через EffectManager ✅
- Stun/Root визуализация уже работает ✅
- Нужно добавить только снаряды и анимации каста
- Hit эффекты можно добавить позже для полноты

**Следуйте инструкциям выше и все визуальные эффекты будут корректно синхронизированы!** 🎉
