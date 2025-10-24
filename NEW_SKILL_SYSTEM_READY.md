# НОВАЯ СИСТЕМА СКИЛЛОВ - ГОТОВА К ИСПОЛЬЗОВАНИЮ

**Дата:** 21 октября 2025
**Статус:** ✅ Базовая система создана
**Следующий шаг:** Интеграция с PlayerAttackNew и создание первого тестового скилла

---

## ЧТО СОЗДАНО

### ✅ 1. SkillConfig.cs
**Путь:** `Assets/Scripts/Skills/SkillConfig.cs`

**Что это:** ScriptableObject для настройки скиллов (аналог BasicAttackConfig)

**Поддерживаемые типы скиллов:**
- ✅ **ProjectileDamage** - Снаряд с уроном (Fireball, Arrow, Hammer)
- ✅ **InstantDamage** - Мгновенный урон (Holy Strike, Backstab)
- ✅ **AOEDamage** - Область поражения (Ice Nova, Explosive Arrow)
- ✅ **Heal** - Лечение (Lay on Hands)
- ✅ **Buff** - Положительный эффект (Battle Cry, Divine Shield)
- ✅ **Debuff** - Отрицательный эффект
- ✅ **CrowdControl** - Контроль (Stun, Root, Sleep)
- ✅ **Movement** - Движение (Charge, Teleport, Dash)
- ✅ **Summon** - Призыв (Summon Skeletons)
- ✅ **Transformation** - Трансформация (Bear Form)
- ✅ **Resurrection** - Воскрешение

**Ключевые параметры:**
```csharp
public int skillId;                   // Уникальный ID
public string skillName;              // Название
public float cooldown;                // Кулдаун (сек)
public float manaCost;                // Стоимость маны
public float castRange;               // Дальность (м)
public float castTime;                // Время каста (сек)
public float baseDamageOrHeal;        // Базовый урон/лечение
public float strengthScaling;         // Скейлинг от Strength
public float intelligenceScaling;     // Скейлинг от Intelligence
public List<EffectConfig> effects;    // Эффекты (DoT, CC, Buffs)
public GameObject projectilePrefab;   // Снаряд
public float projectileSpeed;         // Скорость снаряда
public bool projectileHoming;         // Автонаведение
public GameObject hitEffectPrefab;    // Эффект попадания
public string animationTrigger;       // Триггер анимации
```

---

### ✅ 2. EffectConfig.cs
**Путь:** `Assets/Scripts/Skills/EffectConfig.cs`

**Что это:** Структура для статус-эффектов (используется в SkillConfig.effects[])

**Поддерживаемые эффекты (30+ типов):**

#### Баффы (положительные):
- IncreaseAttack - Увеличение атаки
- IncreaseDefense - Увеличение защиты
- IncreaseSpeed - Увеличение скорости
- IncreaseHPRegen - Регенерация HP
- IncreaseMPRegen - Регенерация MP
- Shield - Щит (поглощает урон)
- Invulnerability - Неуязвимость
- Invisibility - Невидимость

#### Дебаффы (отрицательные):
- DecreaseAttack - Уменьшение атаки
- DecreaseDefense - Уменьшение защиты
- DecreaseSpeed - Замедление
- Poison - Яд (DoT)
- Burn - Горение (DoT)
- Bleed - Кровотечение (DoT)

#### Контроль (Crowd Control):
- Stun - Оглушение (блокирует всё)
- Root - Корни (блокирует движение)
- Sleep - Сон (сбрасывается при уроне)
- Silence - Молчание (блокирует скиллы)
- Fear - Страх
- Taunt - Провокация

**Методы проверки:**
```csharp
effect.IsCrowdControl();      // Это контроль?
effect.IsDamageOverTime();    // Это DoT?
effect.IsHealOverTime();      // Это HoT?
effect.IsBuff();              // Это баф?
effect.IsDebuff();            // Это дебаф?
effect.BlocksMovement();      // Блокирует движение?
effect.BlocksAttacks();       // Блокирует атаки?
effect.BlocksSkills();        // Блокирует скиллы?
```

---

### ✅ 3. SkillExecutor.cs
**Путь:** `Assets/Scripts/Skills/SkillExecutor.cs`

**Что это:** Компонент на персонаже, который выполняет скиллы

**Основные методы:**

```csharp
// Использовать скилл по индексу слота (0-2)
public bool UseSkill(int slotIndex, Transform target = null, Vector3? groundTarget = null)

// Получить кулдаун скилла
public float GetCooldown(int skillId)

// Установить кулдаун
public void SetCooldown(int skillId, float cooldown)
```

**Что делает:**
1. ✅ Проверяет кулдаун, ману, дальность
2. ✅ Проигрывает анимацию
3. ✅ Создаёт снаряды (через CelestialProjectile)
4. ✅ Наносит урон (мгновенный или AOE)
5. ✅ Применяет эффекты через EffectManager
6. ✅ Отправляет на сервер для синхронизации

**Реализованные типы:**
- ✅ ProjectileDamage (создаёт снаряд)
- ✅ InstantDamage (мгновенный урон)
- ✅ AOEDamage (область поражения)
- ✅ Heal (лечение)
- ✅ Buff (баф на союзника/себя)
- ✅ Debuff/CrowdControl (дебаф на врага)
- ⚠️ Movement (частично - Teleport и Dash)
- ⚠️ Summon (заглушка)
- ⚠️ Transformation (заглушка)
- ⚠️ Resurrection (заглушка)

---

### ✅ 4. EffectManager.cs
**Путь:** `Assets/Scripts/Skills/EffectManager.cs`

**Что это:** Компонент на персонаже, который управляет активными эффектами

**Основные методы:**

```csharp
// Применить эффект
public void ApplyEffect(EffectConfig config, CharacterStats casterStats, string casterSocketId = "")

// Удалить эффект по ID
public void RemoveEffect(int effectId)

// Проверки
public bool IsUnderCrowdControl()  // Под контролем?
public bool CanMove()              // Может двигаться?
public bool CanAttack()            // Может атаковать?
public bool CanUseSkills()         // Может использовать скиллы?

// Снять все эффекты (Dispel)
public void DispelAllEffects()
```

**Что делает:**
1. ✅ Хранит список активных эффектов (ActiveEffect[])
2. ✅ Обрабатывает тики урона/лечения (DoT/HoT)
3. ✅ Блокирует действия при CC (Stun, Root, Silence)
4. ✅ Создаёт визуальные эффекты (частицы)
5. ✅ Проигрывает звуки применения/снятия
6. ✅ Синхронизирует с сервером
7. ✅ Показывает DEBUG UI (список эффектов)

**Тики DoT/HoT:**
- Каждый `tickInterval` секунд наносится урон или лечение
- Урон рассчитывается с учётом характеристик кастера
- Sleep сбрасывается при получении урона

---

### ✅ 5. Сетевая синхронизация

**Добавлено в SocketIOManager.cs:**

```csharp
// Отправить использование скилла
public void SendSkillCast(int skillId, string targetSocketId, Vector3 targetPosition)

// Отправить применение эффекта
public void SendEffectApplied(int skillId, int effectIndex, string targetSocketId, float duration, EffectType effectType)

// Отправить снятие эффекта
public void SendEffectRemoved(int effectId, string targetSocketId, EffectType effectType)
```

**События от сервера (нужно добавить обработчики):**
- `skill_casted` - Скилл подтверждён сервером
- `effect_applied` - Эффект применён к цели
- `effect_removed` - Эффект снят с цели

---

## КАК ИСПОЛЬЗОВАТЬ

### ШАБЛОН: Создание нового скилла

**1. Создать SkillConfig файл:**
```
Unity Editor:
  Create → Aetherion → Combat → Skill Config

Название: Mage_Fireball
```

**2. Настроить параметры:**
```yaml
skillId: 201
skillName: "Fireball"
description: "Огненный шар с большим уроном"
characterClass: Mage
cooldown: 6
manaCost: 40
castRange: 20
castTime: 0.8
skillType: ProjectileDamage
targetType: Enemy
requiresTarget: true
baseDamageOrHeal: 60
intelligenceScaling: 25
projectilePrefab: FireballProjectile (из Assets/Prefabs/Projectiles/)
projectileSpeed: 20
projectileHoming: true
hitEffectPrefab: CFXR3 Fire Explosion B
animationTrigger: "Attack"
```

**3. Добавить эффекты (опционально):**
```yaml
effects:
  - effectType: Burn
    duration: 3
    damageOrHealPerTick: 10
    tickInterval: 1
    particleEffectPrefab: (огненные частицы)
    syncWithServer: true
```

**4. Добавить на персонажа:**
```csharp
// В PlayerAttackNew или Character Selection
SkillExecutor executor = player.GetComponent<SkillExecutor>();
executor.equippedSkills.Add(fireballSkill);
```

---

## СЛЕДУЮЩИЕ ШАГИ

### ⏳ STEP 1: Интеграция с PlayerAttackNew.cs

**Нужно добавить:**

```csharp
public class PlayerAttackNew : MonoBehaviour
{
    [Header("Basic Attack")]
    public BasicAttackConfig attackConfig;  // УЖЕ ЕСТЬ

    [Header("Skills")] // НОВОЕ
    private SkillExecutor skillExecutor;
    private EffectManager effectManager;

    void Start()
    {
        // НОВОЕ
        skillExecutor = GetComponent<SkillExecutor>();
        effectManager = GetComponent<EffectManager>();

        // Если нет - добавить
        if (skillExecutor == null)
            skillExecutor = gameObject.AddComponent<SkillExecutor>();

        if (effectManager == null)
            effectManager = gameObject.AddComponent<EffectManager>();
    }

    void Update()
    {
        // Проверка контроля
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

### ⏳ STEP 2: Создать первый тестовый скилл

**Рекомендую:** Mage_Fireball (простой снаряд)

**Параметры:**
- Projectile Damage
- Homing = true
- Burn effect (3 секунды, 10 урона/сек)

**Префабы нужны:**
- Снаряд: `FireballProjectile` (уже есть в Assets/Prefabs/Projectiles/)
- Hit Effect: `CFXR3 Fire Explosion B` (уже есть)
- Burn Particles: (нужно найти или создать)

---

### ⏳ STEP 3: Тестирование

**Тест в Arena:**

1. Открыть Arena Scene
2. Добавить SkillExecutor и EffectManager на LocalPlayer
3. Добавить Mage_Fireball в equippedSkills[0]
4. Запустить игру
5. Нажать "1"
6. Проверить:
   - ✅ Снаряд создаётся
   - ✅ Летит к врагу
   - ✅ Наводится на врага (homing)
   - ✅ Попадает и взрывается
   - ✅ Враг получает урон
   - ✅ На враге появляется огонь (Burn effect)
   - ✅ Враг получает тиковый урон каждую секунду

---

### ⏳ STEP 4: Мультиплеер тест

**Сервер нужно обновить:**

```javascript
// server.js - НОВОЕ
socket.on('player_skill_cast', (data) => {
  const { skillId, targetSocketId, targetPosition, timestamp } = JSON.parse(data);

  const player = players.get(socket.id);
  if (!player) return;

  // Валидация (TODO)
  // - Cooldown
  // - Mana cost
  // - Range

  // Отправить ВСЕМ
  io.to(player.roomId).emit('skill_casted', JSON.stringify({
    socketId: socket.id,
    skillId: skillId,
    targetSocketId: targetSocketId,
    targetPosition: targetPosition
  }));
});
```

**Клиент - добавить обработчик:**

```csharp
// NetworkSyncManager.cs
SocketIOManager.Instance.On("skill_casted", OnSkillCasted);

private void OnSkillCasted(string jsonData)
{
    // Десериализация
    // Найти NetworkPlayer
    // Получить SkillConfig по skillId
    // Выполнить визуальные эффекты
}
```

---

## ВАЖНЫЕ ФАЙЛЫ

### Созданные файлы:
1. ✅ `Assets/Scripts/Skills/SkillConfig.cs` - ScriptableObject скилла
2. ✅ `Assets/Scripts/Skills/EffectConfig.cs` - Структура эффекта
3. ✅ `Assets/Scripts/Skills/SkillExecutor.cs` - Выполнение скиллов
4. ✅ `Assets/Scripts/Skills/EffectManager.cs` - Управление эффектами
5. ✅ `Assets/Scripts/Network/SocketIOManager.cs` - Обновлён (методы SendSkillCast, SendEffectApplied)

### Нужно модифицировать:
6. ⏳ `Assets/Scripts/Player/PlayerAttackNew.cs` - Интеграция (клавиши 1/2/3)
7. ⏳ `Assets/Scripts/Network/NetworkSyncManager.cs` - Обработчики событий

### Нужно создать:
8. ⏳ Первый SkillConfig (Mage_Fireball.asset)
9. ⏳ Prefab для Burn effect (огненные частицы)

---

## СОВМЕСТИМОСТЬ СО СТАРОЙ СИСТЕМОЙ

### ✅ Полная совместимость:

- **Старая система:** SkillData.cs + SkillManager.cs (НЕ ТРОГАЕМ)
- **Новая система:** SkillConfig.cs + SkillExecutor.cs (ПАРАЛЛЕЛЬНО)
- **Нет конфликтов:** Разные компоненты, разные файлы

### Переход:

1. Оставить SkillData файлы как есть
2. Создать SkillConfig для новых скиллов
3. Когда новая система заработает - конвертировать все скиллы
4. Удалить старую систему

---

## ПРЕИМУЩЕСТВА НОВОЙ СИСТЕМЫ

### ✅ VS Старая система (SkillData):

| Функция | Старая система | Новая система |
|---------|----------------|---------------|
| Сетевая синхронизация снарядов | ❌ НЕТ | ✅ ДА (через CelestialProjectile) |
| Синхронизация эффектов | ❌ НЕТ | ✅ ДА (через EffectManager) |
| Серверная валидация | ❌ НЕТ | ⏳ ПЛАНИРУЕТСЯ |
| Блокировка при CC | ⚠️ ЧАСТИЧНО | ✅ ДА (EffectManager.CanMove/Attack/UseSkills) |
| DoT/HoT тики | ⚠️ ЧАСТИЧНО | ✅ ДА (тики каждый tickInterval) |
| Визуальные эффекты | ✅ ДА | ✅ ДА |
| Анимации | ✅ ДА | ✅ ДА |
| Звуки | ✅ ДА | ✅ ДА |
| Модульность | ⚠️ НИЗКАЯ | ✅ ВЫСОКАЯ |
| Легко добавлять новые скиллы | ⚠️ СРЕДНЕ | ✅ ДА |

---

## ГОТОВ К СЛЕДУЮЩЕМУ ШАГУ!

**Что делать дальше:**

1. **Вариант А (рекомендую):** Интегрировать с PlayerAttackNew и протестировать в single-player
2. **Вариант Б:** Сразу создать первый SkillConfig (Mage_Fireball) и протестировать
3. **Вариант В:** Обновить сервер для валидации и мультиплеера

**Скажи какой вариант выбираешь!** 🚀

---

**Автор:** Claude (Anthropic)
**Дата:** 21 октября 2025
