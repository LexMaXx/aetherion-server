# 🎯 ГАЙД ПО СИНХРОНИЗАЦИИ СКИЛЛОВ В МУЛЬТИПЛЕЕРЕ

## 📋 **ПРОБЛЕМА:**
Скиллы (например Eagle Eye) работают **ЛОКАЛЬНО**, но **НЕ ВИДНЫ** другим игрокам:
- ❌ Анимация каста не проигрывается
- ❌ Визуальные эффекты не создаются
- ❌ Ауры (buffs) не видны

---

## ✅ **РЕШЕНИЕ В 3 ШАГА:**

### **ШАГ 1: Добавить метод SendSkillUsed в NetworkCombatSync.cs**

Откройте `Assets/Scripts/Network/NetworkCombatSync.cs` и добавьте этот метод:

```csharp
/// <summary>
/// Отправить использование скилла на сервер
/// Вызывается из PlayerAttack после успешного UseSkill()
/// </summary>
public void SendSkillUsed(int skillId, string animationTrigger, Vector3 targetPosition, float castTime)
{
    if (!enableSync || !isMultiplayer || SocketIOManager.Instance == null)
        return;

    if (!SocketIOManager.Instance.IsConnected)
    {
        Debug.LogWarning("[NetworkCombatSync] Нет подключения к серверу, скилл не будет синхронизирован");
        return;
    }

    // Отправляем на сервер
    SocketIOManager.Instance.SendSkillUsed(skillId, animationTrigger, targetPosition, castTime);

    Debug.Log($"[NetworkCombatSync] 📡 Скилл ID:{skillId} ({animationTrigger}) отправлен на сервер");
}
```

---

### **ШАГ 2: Добавить вызов SendSkillUsed в PlayerAttack.cs**

Откройте `Assets/Scripts/Player/PlayerAttack.cs`, найдите метод `UseSkillDirectly()` (строка ~1120) и **ЗАМЕНИТЕ** блок:

```csharp
if (success)
{
    Debug.Log($"[PlayerAttack] ⚡ Скилл {skill.skillName} УСПЕШНО использован!");
}
```

**НА:**

```csharp
if (success)
{
    Debug.Log($"[PlayerAttack] ⚡ Скилл {skill.skillName} УСПЕШНО использован!");

    // ✅ КРИТИЧЕСКОЕ: Отправляем на сервер для синхронизации
    NetworkCombatSync networkSync = GetComponent<NetworkCombatSync>();
    if (networkSync != null && networkSync.enabled)
    {
        Vector3 targetPos = target != null ? target.position : transform.position + transform.forward * 10f;
        networkSync.SendSkillUsed(skill.skillId, skill.animationTrigger, targetPos, skill.castTime);
        Debug.Log($"[PlayerAttack] 📡 Скилл {skill.skillName} (ID:{skill.skillId}) отправлен на сервер");
    }
}
```

---

### **ШАГ 3: Добавить метод SendSkillUsed в SocketIOManager.cs**

Откройте `Assets/Scripts/Network/SocketIOManager.cs` и добавьте этот метод (после `SendProjectileSpawned`):

```csharp
/// <summary>
/// Отправить использование скилла на сервер
/// </summary>
public void SendSkillUsed(int skillId, string animationTrigger, Vector3 targetPosition, float castTime)
{
    if (!IsConnected)
    {
        DebugLog("⚠️ SendSkillUsed: Не подключены к серверу");
        return;
    }

    var data = new
    {
        skillId = skillId,
        animationTrigger = animationTrigger,
        targetPosition = new { x = targetPosition.x, y = targetPosition.y, z = targetPosition.z },
        castTime = castTime,
        timestamp = GetTimestamp()
    };

    string json = JsonConvert.SerializeObject(data);
    Emit("player_used_skill", json);

    DebugLog($"⚡ Отправка скилла: ID={skillId}, trigger={animationTrigger}, castTime={castTime}с");
}
```

---

## 🎬 **КАК ЭТО РАБОТАЕТ:**

### **Локальный игрок (Игрок A):**
1. Нажимает кнопку скилла (например `4` = Eagle Eye)
2. `PlayerAttack.UseSkillDirectly()` → `SkillManager.UseSkill()` → `SkillExecutor.UseSkill()`
3. **ЛОКАЛЬНО:**
   - Тратит ману ✅
   - Проигрывает анимацию ✅
   - Создаёт визуальные эффекты ✅
   - Применяет buff/debuff ✅
4. **ОТПРАВКА НА СЕРВЕР:**
   - `PlayerAttack` → `NetworkCombatSync.SendSkillUsed()` → `SocketIOManager.SendSkillUsed()`
   - Сервер получает `player_used_skill` event

### **Сервер (Render.com):**
1. Получает `player_used_skill` от Игрока A
2. **Транслирует** событие всем остальным игрокам в комнате:
   ```javascript
   socket.to(player.roomId).emit('player_used_skill', {
       socketId: socket.id,
       skillId: data.skillId,
       animationTrigger: data.animationTrigger,
       targetPosition: data.targetPosition,
       castTime: data.castTime
   });
   ```

### **Другие игроки (Игрок B, C, D...):**
1. `NetworkSyncManager.OnPlayerSkillUsed()` получает событие
2. Находит `NetworkPlayer` по `socketId` (Игрок A)
3. **ВИЗУАЛЬНО ПОКАЗЫВАЕТ:**
   - Проигрывает анимацию каста (`animator.SetTrigger("Skill")`) ✅
   - Создаёт визуальные эффекты (если есть) ✅
   - **НЕ** применяет урон/эффекты (они придут через отдельные события `effect_applied`)

---

## 🔧 **ИСПРАВЛЕНИЕ ОШИБКИ АНИМАЦИИ:**

### **Проблема:**
```
Parameter 'Skill' does not exist.
UnityEngine.Animator:SetTrigger (string)
```

### **Причина:**
У **Archer** анимация триггер `"Skill"` не существует в `ArcherAnimator.controller`.

### **Решение:**

**Вариант 1: Изменить animationTrigger в SkillConfig**

Откройте `Assets/Resources/Skills/Archer_EagleEye.asset` в Inspector и измените:
```
animationTrigger: "Skill" → "Cast" (или "Attack", или другой существующий параметр)
```

**Вариант 2: Добавить параметр "Skill" в Animator**

1. Откройте `Assets/Animations/Controllers/ArcherAnimator.controller`
2. В окне **Parameters** добавьте новый Trigger с именем `Skill`
3. Создайте transition: `Any State → CastAnimation` с условием `Skill` (trigger)

---

## 🐛 **ИСПРАВЛЕНИЕ targetSocketId В effect_applied:**

### **Проблема:**
```
[SocketIO] ✨ Отправка эффекта: IncreasePerception, цель=, duration=15с
```
`цель=""` - пустая! Сервер не знает **НА КОГО** применить эффект.

### **Решение:**

Откройте `Assets/Scripts/Skills/EffectManager.cs`, найдите метод `SendEffectToServer()` (строка ~620) и **ИСПРАВЬТЕ:**

**БЫЛО:**
```csharp
private void SendEffectToServer(ActiveEffect effect)
{
    if (NetworkCombatSync networkSync = GetComponent<NetworkCombatSync>())
    {
        networkSync.SendEffectApplied(effect.config, ""); // ❌ Пустая строка!
    }
}
```

**СТАЛО:**
```csharp
private void SendEffectToServer(ActiveEffect effect)
{
    NetworkCombatSync networkSync = GetComponent<NetworkCombatSync>();
    if (networkSync != null)
    {
        // ✅ Определяем targetSocketId: для self-buffs это МЫ САМИ
        string targetSocketId = GetMySocketId();
        networkSync.SendEffectApplied(effect.config, targetSocketId);

        Log($"📡 Эффект {effect.config.effectType} отправлен на сервер (target={targetSocketId})");
    }
}

/// <summary>
/// Получить мой socketId из SocketIOManager
/// </summary>
private string GetMySocketId()
{
    if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
    {
        return SocketIOManager.Instance.SessionId; // Наш socketId
    }
    return "";
}
```

---

## ✨ **ДОБАВЛЕНИЕ ВИЗУАЛЬНОЙ АУРЫ ДЛЯ ДРУГИХ ИГРОКОВ:**

### **Проблема:**
Eagle Eye применяет buff (+2 Perception), но **аура не видна** другим игрокам.

### **Решение:**

Откройте `Assets/Scripts/Network/NetworkSyncManager.cs`, найдите метод `OnEffectApplied()` (строка ~1067) и **ДОБАВЬТЕ** создание ауры:

```csharp
private void OnEffectApplied(string jsonData)
{
    Debug.Log($"[NetworkSync] ✨ RAW effect_applied JSON: {jsonData}");

    try
    {
        var data = JsonConvert.DeserializeObject<EffectAppliedEvent>(jsonData);

        // Пропускаем свои собственные эффекты
        if (data.socketId == localPlayerSocketId)
        {
            Debug.Log($"[NetworkSync] ⏭️ Это наш собственный эффект, пропускаем");
            return;
        }

        // Находим целевого игрока
        Transform targetTransform = null;

        if (data.targetSocketId == localPlayerSocketId && localPlayer != null)
        {
            // Эффект применён на НАС
            targetTransform = localPlayer.transform;
            Debug.Log($"[NetworkSync] ✨ Эффект {data.effectType} применён на ЛОКАЛЬНОГО игрока");
        }
        else if (networkPlayers.TryGetValue(data.targetSocketId, out NetworkPlayer targetPlayer))
        {
            // Эффект применён на сетевого игрока
            targetTransform = targetPlayer.transform;
            Debug.Log($"[NetworkSync] ✨ Эффект {data.effectType} применён на сетевого игрока {targetPlayer.username}");
        }

        if (targetTransform != null)
        {
            // ✅ СОЗДАЁМ ВИЗУАЛЬНУЮ АУРУ
            if (!string.IsNullOrEmpty(data.particleEffectPrefabName))
            {
                GameObject particlePrefab = Resources.Load<GameObject>($"Effects/{data.particleEffectPrefabName}");

                if (particlePrefab != null)
                {
                    // Создаём ауру как child объект игрока
                    GameObject aura = Instantiate(particlePrefab, targetTransform);
                    aura.transform.localPosition = Vector3.up * 1f; // 1 метр над головой
                    aura.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Поворот вниз

                    // Удаляем через duration секунд
                    Destroy(aura, data.duration);

                    Debug.Log($"[NetworkSync] ✨ Аура {data.effectType} создана для {targetTransform.name} на {data.duration} секунд");
                }
                else
                {
                    Debug.LogWarning($"[NetworkSync] ⚠️ Particle prefab не найден: Effects/{data.particleEffectPrefabName}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[NetworkSync] ⚠️ Целевой игрок не найден: targetSocketId={data.targetSocketId}");
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"[NetworkSync] ❌ Ошибка в OnEffectApplied: {ex.Message}\nJSON: {jsonData}");
    }
}
```

---

## 🧪 **ТЕСТИРОВАНИЕ:**

### **1. Запустите 2 клиента Unity:**

**Клиент 1 (Игрок A):**
- Создаёт комнату
- Выбирает Archer
- Входит в арену

**Клиент 2 (Игрок B):**
- Подключается к той же комнате
- Выбирает любой класс
- Входит в арену

### **2. Игрок A использует Eagle Eye (кнопка `4`):**

**Что ДОЛЖНО произойти:**

**У Игрока A (локально):**
```
[PlayerAttack] 🎯 Прямое использование скилла 3: Eagle Eye
[ManaSystem] -40 MP. Осталось: 0/65
[SkillExecutor] ✅ Потрачено 40 маны
[EffectManager] ✨ Применён эффект: IncreasePerception (15с)
[PlayerAttack] 📡 Скилл Eagle Eye (ID:303) отправлен на сервер  ← ✅ НОВОЕ
[FogOfWar] Радиус видимости обновлен: 22м
```

**У Игрока B (удалённо):**
```
[NetworkSync] ⚡ RAW player_used_skill JSON: {...}  ← ✅ НОВОЕ
[NetworkSync] ⚡ Скилл получен: socketId=xyz, skillId=303
[NetworkSync] 🎬 Анимация 'Skill' запущена для PlayerA  ← ✅ НОВОЕ
[NetworkSync] ✨ RAW effect_applied JSON: {...}
[NetworkSync] ✨ Эффект IncreasePerception применён на PlayerA
[NetworkSync] ✨ Аура IncreasePerception создана  ← ✅ НОВОЕ
```

---

## 📊 **РЕЗУЛЬТАТ:**

После этих исправлений:
- ✅ Игрок B **ВИДИТ** анимацию каста Eagle Eye у Игрока A
- ✅ Игрок B **ВИДИТ** золотую ауру над головой Игрока A (15 секунд)
- ✅ Игрок B **ВИДИТ** визуальный эффект каста (если есть `castEffectPrefab`)

---

## 🔥 **РАСШИРЕНИЕ НА ВСЕ СКИЛЛЫ:**

Эта система **АВТОМАТИЧЕСКИ** работает для **ВСЕХ** скиллов:
- Fireball - другие игроки видят анимацию каста + снаряд летит
- Ice Nova - другие видят анимацию + взрыв
- Battle Rage - другие видят красную ауру
- Bear Form - другие видят трансформацию (нужно добавить `OnPlayerTransformed`)

---

## 🎯 **СЛЕДУЮЩИЕ ШАГИ:**

1. ✅ Реализовать синхронизацию трансформаций (Bear Form)
2. ✅ Добавить синхронизацию hit effects (красная вспышка при уроне)
3. ✅ Оптимизировать создание снарядов (избежать дублирования)

---

**Готово! Теперь все скиллы будут видны в мультиплеере! 🚀**
