# 🎮 КАК РАБОТАЕТ СИНХРОНИЗАЦИЯ ВИЗУАЛЬНЫХ ЭФФЕКТОВ

## 📊 СХЕМА РАБОТЫ

```
ИГРОК 1 (Unity Client)                    NODE.JS SERVER (Render.com)                    ИГРОК 2 (Unity Client)
═══════════════════════                   ═══════════════════════════                   ═══════════════════════

1. Игрок кастует Fireball
   ├─ SkillManager.PlaySkillAnimation()
   │  └─ Проигрывает анимацию "Attack"
   │
   ├─ Fireball летит к цели
   │  └─ Projectile движется
   │
   └─ Попадает в цель!
      └─ Projectile.OnCollisionEnter()
         │
         ├─ [ЛОКАЛЬНО] Создает взрыв
         │  └─ Instantiate(hitEffect)
         │     Игрок 1 ВИДИТ взрыв ✅
         │
         └─ [СЕТЬ] Отправка на сервер ────────────────────────────>  2. Сервер получает событие
            SocketIOManager.SendVisualEffect()                          socket.on('visual_effect_spawned')
            │                                                            │
            Emit("visual_effect_spawned", {                             ├─ Логирует в консоль
              effectType: "explosion",                                  │  "[visual_effect_spawned] Получен эффект"
              effectPrefabName: "CFXR3 Fire Explosion B",              │
              position: { x: 10, y: 1, z: 12 },                        └─ Рассылает ВСЕМ игрокам
              rotation: { x: 0, y: 0, z: 0 },                              io.emit('visual_effect_spawned', data)
              targetSocketId: "",                                          │
              duration: 0                                                  │
            })                                                             │
                                                                           ├────────> Игрок 1 (отправитель)
                                                                           │          └─ Пропускает (это свой эффект)
                                                                           │
                                                                           └────────> Игрок 2 (другие игроки)
                                                                                      └─ NetworkSyncManager
                                                                                         .OnVisualEffectSpawned()
                                                                                         │
                                                                                         3. Создает взрыв у себя
                                                                                            ├─ TryLoadEffectPrefab()
                                                                                            │  └─ Загружает из Resources
                                                                                            │
                                                                                            └─ Instantiate()
                                                                                               Игрок 2 ВИДИТ взрыв ✅
```

---

## 🔄 ПОДРОБНЫЙ FLOW

### ЭТАП 1: Игрок использует скилл

**Файл:** `SkillManager.cs`

```
UseSkill()
  └─> PlaySkillAnimation()
       └─> animator.SetTrigger("Attack")  // ✅ АНИМАЦИЯ КАСТА ДЛЯ ВСЕХ КЛАССОВ
            └─> ExecuteSkill()
                 └─> Создает снаряд (Fireball)
```

---

### ЭТАП 2: Снаряд летит и попадает

**Файл:** `Projectile.cs`

```
OnCollisionEnter() или OnTriggerEnter()
  │
  ├─> [ЛОКАЛЬНЫЙ ЭФФЕКТ]
  │    └─> Instantiate(hitEffect, position, rotation)
  │         └─> Игрок 1 видит взрыв немедленно ✅
  │
  └─> [СЕТЕВАЯ СИНХРОНИЗАЦИЯ]
       └─> if (SocketIOManager.Instance.IsConnected)
            └─> SendVisualEffect(
                  "explosion",                    // Тип эффекта
                  "CFXR3 Fire Explosion B",       // Название prefab
                  transform.position,              // Где взорвалось
                  Quaternion.identity,             // Ротация
                  "",                              // Не привязан к игроку
                  0f                               // Авто длительность
                )
```

---

### ЭТАП 3: Отправка на сервер

**Файл:** `SocketIOManager.cs`

```
SendVisualEffect(...)
  └─> var data = new {
        effectType,
        effectPrefabName,
        position,
        rotation,
        targetSocketId,
        duration
      }
       └─> string json = JsonConvert.SerializeObject(data)
            └─> Emit("visual_effect_spawned", json)
                 │
                 └─────> Отправлено на сервер! 📤
```

---

### ЭТАП 4: Сервер обрабатывает (⚠️ НУЖНО ДОБАВИТЬ!)

**Файл:** `server.js` (Node.js)

```javascript
socket.on('visual_effect_spawned', (data) => {
  console.log('[visual_effect_spawned] Получен эффект');

  // Рассылаем ВСЕМ игрокам (broadcast)
  io.emit('visual_effect_spawned', {
    socketId: socket.id,
    effectType: data.effectType,
    effectPrefabName: data.effectPrefabName,
    position: data.position,
    rotation: data.rotation,
    targetSocketId: data.targetSocketId,
    duration: data.duration,
    timestamp: Date.now()
  });

  console.log('[visual_effect_spawned] Разослано всем игрокам');
});
```

**❌ СЕЙЧАС:** Этого кода НЕТ на сервере!
**✅ НУЖНО:** Добавить этот код (см. `SERVER_VISUAL_EFFECTS_FIX.md`)

---

### ЭТАП 5: Другие игроки получают событие

**Файл:** `NetworkSyncManager.cs`

```
Регистрация обработчика (в Start):
  SocketIOManager.Instance.On("visual_effect_spawned", OnVisualEffectSpawned)

Когда сервер отправляет событие:
  OnVisualEffectSpawned(jsonData)
    │
    ├─> Десериализация JSON
    │    └─> var data = JsonConvert.DeserializeObject<VisualEffectSpawnedEvent>(jsonData)
    │
    ├─> Проверка: это не мой эффект?
    │    └─> if (data.socketId == localPlayerSocketId) return; // Пропустить
    │
    ├─> Загрузка prefab из Resources
    │    └─> TryLoadEffectPrefab(data.effectPrefabName)
    │         │
    │         ├─> Пробуем: Resources.Load($"Effects/{prefabName}")
    │         ├─> Пробуем: Resources.Load($"Prefabs/Effects/{prefabName}")
    │         ├─> Пробуем: Resources.Load($"Projectiles/{prefabName}")
    │         └─> Пробуем: Resources.Load(prefabName)
    │
    └─> Создание эффекта
         └─> Instantiate(effectPrefab, position, rotation)
              └─> Игрок 2 видит взрыв! ✅
```

---

## 🎯 ТИПЫ ЭФФЕКТОВ

### 1. EXPLOSION (взрывы)
**Откуда:** `Projectile.cs` при попадании снаряда
**Примеры:**
- `CFXR3 Fire Explosion B` (Fireball)
- `CFXR Lightning Hit` (Lightning Ball)
- `CFXR Impact` (Hammer of Justice)

### 2. AOE (area of effect)
**Откуда:** `SkillManager.cs` при каст AOE скиллов
**Примеры:**
- `Ice Nova Wave` (Ice Nova)
- `Meteor Fall` (Meteor)
- `Ground Explosion` (AOE взрывы)

### 3. SKILL_HIT (попадание скилла)
**Откуда:** `SkillManager.cs` при попадании direct skill
**Примеры:**
- `Hit Sparks`
- `Impact Flash`

### 4. HEAL (лечение)
**Откуда:** `SkillManager.cs` при использовании хил скилла
**Примеры:**
- `Healing Particles`
- `Restoration Aura`

### 5. TRANSFORMATION (трансформация)
**Откуда:** `SkillManager.cs` при активации трансформации
**Примеры:**
- `Smoke Puff` (Bear Form появление)
- `Magic Flash` (трансформация магия)

### 6. BUFF (баффы)
**Откуда:** `ActiveEffect.cs` при наложении баффа
**Примеры:**
- `Shield Effect` (Shield)
- `Power Aura` (IncreaseAttack)
- `Defense Glow` (IncreaseDefense)
- `Speed Trail` (IncreaseSpeed)
- `Regen Particles` (HealOverTime)

### 7. DEBUFF (дебаффы)
**Откуда:** `ActiveEffect.cs` при наложении дебаффа
**Примеры:**
- `Stun Stars` (Stun)
- `Root Vines` (Root)
- `Silence Mark` (Silence)
- `Fear Aura` (Fear)

### 8. DOT (damage over time)
**Откуда:** `ActiveEffect.cs` при наложении DOT эффекта
**Примеры:**
- `Fire Burn` (Burn)
- `Poison Cloud` (Poison)
- `Blood Drip` (Bleed)

---

## 🔍 КАК ДЕБАЖИТЬ

### Проверка отправки (Игрок 1):

В Unity Console должны быть логи:
```
[SocketIO] ✅ Подключен к серверу
[SkillManager] 🎬 Анимация КАСТА: триггер='Attack', скорость=1x
[SocketIO] ✨ Отправка визуального эффекта: type=explosion, prefab=CFXR3 Fire Explosion B, pos=(10.1, 1.1, 12.1)
[Projectile] ✨ Эффект попадания отправлен на сервер: CFXR3 Fire Explosion B
```

### Проверка получения (Игрок 2):

В Unity Console должны быть логи:
```
[NetworkSync] ✨ RAW visual_effect_spawned JSON: {"socketId":"abc123","effectType":"explosion",...}
[NetworkSync] ✨ Визуальный эффект получен: type=explosion, prefab=CFXR3 Fire Explosion B, targetSocketId=
[NetworkSync] 🔍 Попытка загрузки: Effects/CFXR3 Fire Explosion B
[NetworkSync] ✅ Визуальный эффект создан: CFXR3 Fire Explosion B at (10.10, 1.10, 12.10)
```

### Проверка на сервере:

В логах Render.com должны быть:
```
[visual_effect_spawned] Получен эффект: { socketId: 'abc123', type: 'explosion', prefab: 'CFXR3 Fire Explosion B' }
[visual_effect_spawned] Эффект разослан всем игрокам
```

---

## ❌ ЕСЛИ НЕ РАБОТАЕТ

### Проблема 1: Эффект не отправляется
**Симптом:** Нет лога `[SocketIO] ✨ Отправка визуального эффекта`
**Причины:**
- SocketIOManager.Instance == null
- !IsConnected
- hitEffect == null (нет prefab эффекта)

**Решение:** Проверить что клиент подключен к серверу

### Проблема 2: Эффект отправился, но не вернулся
**Симптом:** Есть лог отправки, но НЕТ лога получения
**Причина:** ❌ Сервер не имеет обработчика `visual_effect_spawned`
**Решение:** Добавить код на сервер (см. `SERVER_VISUAL_EFFECTS_FIX.md`)

### Проблема 3: Событие получено, но эффект не создан
**Симптом:** Есть лог `получен`, но нет лога `создан`
**Причины:**
- Prefab не найден в Resources
- Неправильное имя prefab
- Prefab не в папке Resources

**Решение:** Проверить что prefab существует в одной из папок:
- `Assets/Resources/Effects/`
- `Assets/Resources/Prefabs/Effects/`
- `Assets/Resources/Projectiles/`
- `Assets/Resources/`

---

## ✅ КОГДА ВСЕ ЗАРАБОТАЕТ

После выполнения всех шагов:

1. **Игрок 1** кастует Fireball
   - ✅ Видит анимацию каста "Attack"
   - ✅ Видит снаряд летит
   - ✅ Видит взрыв при попадании

2. **Игрок 2** (и все остальные)
   - ✅ Видит как Игрок 1 делает анимацию каста
   - ✅ Видит снаряд летит (синхронизация уже была)
   - ✅ **НОВОЕ!** Видит взрыв при попадании! 🎉

3. **Сервер** в логах:
   - ✅ Получает события visual_effect_spawned
   - ✅ Рассылает всем игрокам
   - ✅ Логирует успешную рассылку

---

**🎮 РЕЗУЛЬТАТ:** Полноценная мультиплеерная игра с визуальными эффектами! 🚀
