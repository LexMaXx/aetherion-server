# ПЛАН СОЗДАНИЯ НОВОЙ СИСТЕМЫ СКИЛЛОВ

**Дата начала:** 21 октября 2025
**Статус:** В процессе разработки (Step 1/7 выполнен)

---

## ЦЕЛИ СИСТЕМЫ

### ✅ Что должно работать:

1. **Все типы скиллов:**
   - Снаряды (Fireball, Arrow, Hammer of Justice)
   - Мгновенный урон (Holy Strike, Backstab)
   - AOE (Ice Nova, Explosive Arrow)
   - Лечение (Lay on Hands)
   - Баффы (Battle Cry, Divine Shield)
   - Дебафы (Poison, Slow)
   - Контроль (Stun, Root, Sleep, Silence)
   - Движение (Charge, Teleport, Shadow Step)
   - Призыв (Summon Skeletons)
   - Трансформация (Bear Form)

2. **Все эффекты:**
   - DoT (Damage over Time) - Poison, Burn, Bleed
   - HoT (Heal over Time) - Regeneration
   - CC (Crowd Control) - Stun, Root, Sleep, Fear, Silence
   - Баффы - Increase Attack/Defense/Speed
   - Дебаффы - Decrease Attack/Defense/Speed
   - Особые - Shield, Invulnerability, Invisibility

3. **Полная сетевая синхронизация:**
   - Все игроки видят снаряды
   - Все игроки видят визуальные эффекты
   - Все игроки видят ауры и частицы эффектов
   - Все игроки видят анимации
   - Урон и эффекты работают корректно

4. **Серверная валидация:**
   - Проверка cooldown на сервере
   - Проверка mana на сервере
   - Проверка дальности на сервере
   - Предотвращение читерства

---

## ПОШАГОВЫЙ ПЛАН РАЗРАБОТКИ

### ✅ STEP 1: Базовые структуры данных (ЗАВЕРШЕНО)

**Создано:**
- ✅ `SkillConfig.cs` - ScriptableObject для настройки скиллов
- ✅ `EffectConfig.cs` - структура для статус-эффектов

**Возможности:**
- Все типы скиллов поддерживаются
- Все типы эффектов реализованы (30+ типов)
- Методы расчёта урона/лечения
- Методы проверки (CanUse, IsCrowdControl, и т.д.)

---

### 🔄 STEP 2: Execution Logic (В ПРОЦЕССЕ)

**Нужно создать:**

#### 2.1. `SkillExecutor.cs`
Компонент на персонаже, который выполняет скиллы.

```csharp
public class SkillExecutor : MonoBehaviour
{
    // Список экипированных скиллов (3 слота)
    public List<SkillConfig> equippedSkills;

    // Кулдауны
    private Dictionary<int, float> skillCooldowns;

    // Ссылки на компоненты
    private CharacterStats stats;
    private ManaSystem manaSystem;
    private EffectManager effectManager;
    private Animator animator;

    // Использовать скилл
    public bool UseSkill(int slotIndex, Transform target = null)
    {
        // 1. Проверки (cooldown, mana, range)
        // 2. Трата маны
        // 3. Запуск cooldown
        // 4. Анимация
        // 5. ОТПРАВКА НА СЕРВЕР
        // 6. Выполнение локально
    }

    // Выполнить скилл (вызывается локально и при получении от сервера)
    private void ExecuteSkill(SkillConfig skill, Transform target, Vector3 targetPos)
    {
        switch (skill.skillType)
        {
            case ProjectileDamage:
                SpawnProjectile(skill, target);
                break;
            case InstantDamage:
                DealInstantDamage(skill, target);
                break;
            case AOEDamage:
                DealAOEDamage(skill, targetPos);
                break;
            // ... и т.д.
        }
    }
}
```

#### 2.2. `EffectManager.cs`
Компонент на персонаже, который управляет активными эффектами.

```csharp
public class EffectManager : MonoBehaviour
{
    // Активные эффекты на персонаже
    private List<ActiveEffect> activeEffects;

    // Применить эффект
    public void ApplyEffect(EffectConfig effect, CharacterStats casterStats, string casterSocketId)
    {
        // 1. Проверка может ли эффект быть применён (иммунитет, стаки)
        // 2. Создать ActiveEffect
        // 3. Применить мгновенные изменения (модификаторы stats)
        // 4. Создать визуальный эффект (частицы)
        // 5. ОТПРАВКА НА СЕРВЕР (если syncWithServer)
        // 6. Добавить в список activeEffects
    }

    // Удалить эффект
    public void RemoveEffect(int effectId)
    {
        // 1. Найти эффект
        // 2. Снять модификаторы
        // 3. Удалить визуал
        // 4. ОТПРАВКА НА СЕРВЕР
    }

    // Обновление каждый кадр (тики урона/лечения)
    void Update()
    {
        UpdateEffects();
    }

    // Проверка находится ли под контролем
    public bool IsUnderCrowdControl()
    {
        return activeEffects.Any(e => e.config.IsCrowdControl());
    }

    // Проверка может ли двигаться
    public bool CanMove()
    {
        return !activeEffects.Any(e => e.config.BlocksMovement());
    }

    // Проверка может ли атаковать
    public bool CanAttack()
    {
        return !activeEffects.Any(e => e.config.BlocksAttacks());
    }

    // Проверка может ли использовать скиллы
    public bool CanUseSkills()
    {
        return !activeEffects.Any(e => e.config.BlocksSkills());
    }
}

// Активный эффект (инстанс)
public class ActiveEffect
{
    public EffectConfig config;
    public float remainingDuration;
    public float nextTickTime;
    public CharacterStats casterStats;  // Для расчёта DoT/HoT
    public string casterSocketId;       // Для сетевой синхронизации
    public GameObject visualEffect;     // Частицы
    public int stackCount;              // Количество стаков
}
```

---

### 📋 STEP 3: Интеграция с PlayerAttackNew

**Модифицировать `PlayerAttackNew.cs`:**

```csharp
public class PlayerAttackNew : MonoBehaviour
{
    [Header("Basic Attack")]
    public BasicAttackConfig attackConfig;  // УЖЕ ЕСТЬ

    [Header("Skills")]
    public List<SkillConfig> equippedSkills = new List<SkillConfig>(3);  // НОВОЕ

    private SkillExecutor skillExecutor;  // НОВОЕ
    private EffectManager effectManager;  // НОВОЕ

    void Start()
    {
        skillExecutor = GetComponent<SkillExecutor>();
        effectManager = GetComponent<EffectManager>();

        // Загрузить скиллы из PlayerPrefs или SkillDatabase
        LoadEquippedSkills();
    }

    void Update()
    {
        // Проверка может ли действовать
        if (effectManager != null && effectManager.IsUnderCrowdControl())
        {
            return; // Под контролем - не может действовать
        }

        // ЛКМ - basic attack (УЖЕ РАБОТАЕТ)
        if (Input.GetMouseButtonDown(0))
        {
            if (effectManager == null || effectManager.CanAttack())
            {
                PerformBasicAttack();
            }
        }

        // 1/2/3 - скиллы (НОВОЕ)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (effectManager == null || effectManager.CanUseSkills())
            {
                skillExecutor?.UseSkill(0, currentTarget);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (effectManager == null || effectManager.CanUseSkills())
            {
                skillExecutor?.UseSkill(1, currentTarget);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (effectManager == null || effectManager.CanUseSkills())
            {
                skillExecutor?.UseSkill(2, currentTarget);
            }
        }
    }
}
```

---

### 🌐 STEP 4: Сетевая синхронизация (Клиент)

**Модифицировать `SocketIOManager.cs`:**

```csharp
// Отправить использование скилла на сервер
public void SendSkillCast(int skillId, string targetSocketId, Vector3 targetPosition)
{
    var data = new
    {
        skillId = skillId,
        targetSocketId = targetSocketId,
        targetPosition = new { x = targetPosition.x, y = targetPosition.y, z = targetPosition.z },
        timestamp = GetCurrentTimestamp()
    };

    Emit("player_skill_cast", JsonConvert.SerializeObject(data));
}

// Отправить применение эффекта на сервер
public void SendEffectApplied(int skillId, int effectIndex, string targetSocketId, float duration)
{
    var data = new
    {
        skillId = skillId,
        effectIndex = effectIndex,
        targetSocketId = targetSocketId,
        duration = duration
    };

    Emit("effect_applied", JsonConvert.SerializeObject(data));
}
```

**Модифицировать `NetworkSyncManager.cs`:**

```csharp
void Start()
{
    // Уже есть
    SocketIOManager.Instance.On("player_used_skill", OnPlayerSkillUsed);

    // НОВЫЕ события
    SocketIOManager.Instance.On("skill_casted", OnSkillCasted);
    SocketIOManager.Instance.On("effect_applied", OnEffectApplied);
    SocketIOManager.Instance.On("effect_removed", OnEffectRemoved);
}

// Сервер подтвердил использование скилла
private void OnSkillCasted(string jsonData)
{
    var data = JsonConvert.DeserializeObject<SkillCastedEvent>(jsonData);

    // Если это НЕ наш скилл
    if (data.socketId != localPlayerSocketId)
    {
        // Найти NetworkPlayer
        if (networkPlayers.TryGetValue(data.socketId, out NetworkPlayer player))
        {
            // Получить SkillConfig
            SkillConfig skill = GetSkillConfigById(data.skillId);

            // Выполнить визуальные эффекты
            SkillExecutor executor = player.GetComponent<SkillExecutor>();
            executor?.ExecuteSkillVisuals(skill, data.targetPosition);
        }
    }
}

// Эффект применён на цель
private void OnEffectApplied(string jsonData)
{
    var data = JsonConvert.DeserializeObject<EffectAppliedEvent>(jsonData);

    // Найти цель
    GameObject target = FindTargetBySocketId(data.targetSocketId);

    if (target != null)
    {
        EffectManager effectMgr = target.GetComponent<EffectManager>();
        SkillConfig skill = GetSkillConfigById(data.skillId);

        if (effectMgr != null && skill != null)
        {
            // Применить эффект (ТОЛЬКО визуал, урон на сервере)
            EffectConfig effect = skill.effects[data.effectIndex];
            effectMgr.ApplyEffectVisual(effect, data.duration);
        }
    }
}
```

---

### 🖥️ STEP 5: Серверная валидация (Server)

**Модифицировать `server.js`:**

```javascript
// Хранение кулдаунов игроков
const playerCooldowns = new Map(); // socketId -> {skillId: expireTime}

// Игрок использует скилл
socket.on('player_skill_cast', (data) => {
  try {
    const { skillId, targetSocketId, targetPosition, timestamp } = JSON.parse(data);

    const player = players.get(socket.id);
    if (!player) return;

    // 1. ВАЛИДАЦИЯ: Получить конфиг скилла
    const skillConfig = getSkillConfig(skillId);
    if (!skillConfig) {
      socket.emit('skill_error', { error: 'Invalid skill ID' });
      return;
    }

    // 2. ВАЛИДАЦИЯ: Проверка cooldown
    if (isSkillOnCooldown(socket.id, skillId)) {
      socket.emit('skill_error', { error: 'Skill on cooldown' });
      return;
    }

    // 3. ВАЛИДАЦИЯ: Проверка маны
    if (player.mana < skillConfig.manaCost) {
      socket.emit('skill_error', { error: 'Not enough mana' });
      return;
    }

    // 4. ВАЛИДАЦИЯ: Проверка дальности (если есть цель)
    if (targetSocketId) {
      const target = players.get(targetSocketId);
      if (target) {
        const distance = calculateDistance(player.position, target.position);
        if (distance > skillConfig.castRange) {
          socket.emit('skill_error', { error: 'Target out of range' });
          return;
        }
      }
    }

    // 5. ВСЁ ОК: Списать ману
    player.mana -= skillConfig.manaCost;
    player.mana = Math.max(0, player.mana);

    // 6. ВСЁ ОК: Установить cooldown
    setSkillCooldown(socket.id, skillId, skillConfig.cooldown);

    // 7. ВСЁ ОК: Отправить ВСЕМ в комнате
    io.to(player.roomId).emit('skill_casted', JSON.stringify({
      socketId: socket.id,
      skillId: skillId,
      targetSocketId: targetSocketId,
      targetPosition: targetPosition,
      timestamp: Date.now()
    }));

    console.log(`[Server] ${player.username} used skill ${skillId}`);

  } catch (error) {
    console.error('[Server] Error in player_skill_cast:', error);
  }
});

// Применение эффекта
socket.on('effect_applied', (data) => {
  const { skillId, effectIndex, targetSocketId, duration } = JSON.parse(data);

  const player = players.get(socket.id);
  if (!player) return;

  // Отправить ВСЕМ в комнате
  io.to(player.roomId).emit('effect_applied', JSON.stringify({
    casterSocketId: socket.id,
    skillId: skillId,
    effectIndex: effectIndex,
    targetSocketId: targetSocketId,
    duration: duration
  }));
});

// Утилиты
function getSkillConfig(skillId) {
  // Загрузить конфиг из JSON файла или базы данных
  return skillConfigs[skillId];
}

function isSkillOnCooldown(socketId, skillId) {
  if (!playerCooldowns.has(socketId)) return false;

  const cooldowns = playerCooldowns.get(socketId);
  if (!cooldowns[skillId]) return false;

  return Date.now() < cooldowns[skillId];
}

function setSkillCooldown(socketId, skillId, duration) {
  if (!playerCooldowns.has(socketId)) {
    playerCooldowns.set(socketId, {});
  }

  const cooldowns = playerCooldowns.get(socketId);
  cooldowns[skillId] = Date.now() + (duration * 1000);
}
```

---

### 🎨 STEP 6: Визуальные эффекты и анимации

**Создать префабы для эффектов:**

1. **DoT эффекты:**
   - Burn - огненные частицы вокруг персонажа
   - Poison - зелёные пузыри/дым
   - Bleed - красные капли крови

2. **CC эффекты:**
   - Stun - звёзды вокруг головы
   - Root - корни у ног
   - Sleep - Z Z Z над головой
   - Silence - запрещающий символ
   - Fear - тёмная аура

3. **Buff эффекты:**
   - Increase Attack - красная аура
   - Increase Defense - синяя аура
   - Increase Speed - белые линии движения
   - Shield - прозрачный щит

**Интеграция в EffectManager:**
```csharp
private GameObject CreateVisualEffect(EffectConfig effect, Transform parent)
{
    if (effect.particleEffectPrefab == null) return null;

    GameObject vfx = Instantiate(effect.particleEffectPrefab, parent);
    vfx.transform.localPosition = Vector3.up * 1.5f; // Над головой

    return vfx;
}
```

---

### 🧪 STEP 7: Тестирование

**План тестирования:**

1. **Single-player тест:**
   - Использовать скилл на врага
   - Проверить урон
   - Проверить визуальные эффекты
   - Проверить DoT тики
   - Проверить CC (stun блокирует действия)

2. **Multiplayer тест (локально, 2 клиента):**
   - Игрок 1 использует Fireball
   - Игрок 2 видит снаряд
   - Игрок 2 видит взрыв
   - Игрок 1 использует Stun
   - Игрок 2 видит эффект stun
   - Проверить что Игрок 2 не может двигаться

3. **Тест каждого типа скилла:**
   - ✅ ProjectileDamage (Fireball)
   - ✅ InstantDamage (Holy Strike)
   - ✅ AOEDamage (Ice Nova)
   - ✅ Heal (Lay on Hands)
   - ✅ Buff (Battle Cry)
   - ✅ CrowdControl (Hammer of Justice - Stun)
   - ✅ Movement (Charge)

4. **Тест эффектов:**
   - ✅ Burn (DoT)
   - ✅ Poison (DoT)
   - ✅ Stun (блокирует действия)
   - ✅ Root (блокирует движение)
   - ✅ Silence (блокирует скиллы)
   - ✅ Shield (поглощает урон)

---

## ТЕКУЩИЙ ПРОГРЕСС

### ✅ Завершено:

1. ✅ Анализ старой системы SkillData
2. ✅ Создание SkillConfig.cs (базовая структура)
3. ✅ Создание EffectConfig.cs (30+ типов эффектов)
4. ✅ Методы расчёта и проверки

### 🔄 В процессе:

5. 🔄 Создание SkillExecutor.cs
6. 🔄 Создание EffectManager.cs

### 📋 Следующие шаги:

7. ⏳ Интеграция с PlayerAttackNew.cs
8. ⏳ Сетевая синхронизация (клиент)
9. ⏳ Серверная валидация
10. ⏳ Визуальные эффекты
11. ⏳ Тестирование

---

## МИГРАЦИЯ СКИЛЛОВ

### План конвертации из SkillData в SkillConfig:

**Автоматизация через Editor Script:**

```csharp
#if UNITY_EDITOR
[MenuItem("Aetherion/Convert SkillData to SkillConfig")]
static void ConvertSkills()
{
    // 1. Найти все SkillData файлы
    string[] guids = AssetDatabase.FindAssets("t:SkillData");

    foreach (string guid in guids)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        SkillData oldSkill = AssetDatabase.LoadAssetAtPath<SkillData>(path);

        // 2. Создать новый SkillConfig
        SkillConfig newSkill = ScriptableObject.CreateInstance<SkillConfig>();

        // 3. Скопировать данные
        newSkill.skillId = oldSkill.skillId;
        newSkill.skillName = oldSkill.skillName;
        newSkill.description = oldSkill.description;
        // ... и т.д.

        // 4. Сохранить
        string newPath = path.Replace("Skills/", "Skills/NEW/");
        AssetDatabase.CreateAsset(newSkill, newPath);
    }

    AssetDatabase.SaveAssets();
    Debug.Log("Conversion complete!");
}
#endif
```

### Приоритет конвертации (какие скиллы делать первыми):

**Фаза 1 (Тестовые):**
1. Mage_Fireball (ProjectileDamage + Burn effect)
2. Warrior_Charge (Movement + InstantDamage)
3. Paladin_LayonHands (Heal)

**Фаза 2 (Все остальные):**
- Автоматическая конвертация через Editor Script
- Ручная проверка каждого скилла
- Настройка параметров

---

## СОВМЕСТИМОСТЬ СО СТАРОЙ СИСТЕМОЙ

### Переходный период:

1. **Оставить SkillData.cs и SkillManager.cs** в проекте
2. **Создать SkillConfig.cs и SkillExecutor.cs** параллельно
3. **PlayerAttackNew.cs** использует ТОЛЬКО SkillConfig
4. **Старый PlayerAttack.cs** (если ещё используется) использует SkillData
5. Когда все скиллы мигрированы - удалить старую систему

### Изоляция:

- Новая система: `Assets/Scripts/Skills/SkillConfig.cs`, `SkillExecutor.cs`, `EffectManager.cs`
- Старая система: `Assets/Scripts/Skills/SkillData.cs`, `SkillManager.cs`
- НЕТ зависимостей между ними

---

## ГОТОВ К ПРОДОЛЖЕНИЮ

Следующий шаг: **Создание SkillExecutor.cs**

Когда скажешь - продолжу разработку! 🚀
