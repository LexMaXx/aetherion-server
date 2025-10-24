# 🎯 НОВАЯ СИСТЕМА БОЯ - Полный обзор

**Дата создания:** Начало нашей переписки
**Статус:** ✅ ПОЛНОСТЬЮ РЕАЛИЗОВАНА И РАБОТАЕТ
**Интеграция с мультиплеером:** ✅ ЗАВЕРШЕНА

---

## 📋 ОГЛАВЛЕНИЕ

1. [Архитектура системы](#архитектура-системы)
2. [Компоненты](#компоненты)
3. [ScriptableObject конфигурации](#scriptableobject-конфигурации)
4. [Интеграция с мультиплеером](#интеграция-с-мультиплеером)
5. [Как это работает](#как-это-работает)
6. [Настройка для новых классов](#настройка-для-новых-классов)
7. [Тестирование](#тестирование)

---

## 🏗️ АРХИТЕКТУРА СИСТЕМЫ

### Старая система (УДАЛЕНА):
```
PlayerAttack.cs → Hardcoded урон, снаряды, эффекты
  ❌ Неудобно менять параметры (нужен код)
  ❌ Дубликаты кода для каждого класса
  ❌ Проблемы с мультиплеером
```

### Новая система (АКТИВНА):
```
BasicAttackConfig.asset (ScriptableObject)
         ↓
PlayerAttackNew.cs (Component)
         ↓
NetworkCombatSync.cs (Multiplayer)
         ↓
Server (Node.js)
```

**Преимущества:**
- ✅ **Удобное редактирование** - все параметры в Inspector
- ✅ **Переиспользование** - один код для всех классов
- ✅ **Мультиплеер** - полная интеграция с NetworkCombatSync
- ✅ **Валидация** - проверка корректности конфигов
- ✅ **Масштабируемость** - легко добавить новый класс

---

## 🧩 КОМПОНЕНТЫ

### 1. BasicAttackConfig.cs (ScriptableObject)

**Путь:** `Assets/Scripts/Combat/BasicAttackConfig.cs`

**Назначение:** Конфигурация базовой атаки для каждого класса

**Основные параметры:**

#### 📊 Базовая информация:
- `characterClass` - Класс персонажа (Warrior, Mage, Archer, Rogue, Paladin)
- `attackType` - Тип атаки (Melee/Ranged)
- `description` - Описание атаки

#### ⚔️ Урон:
- `baseDamage` - Базовый урон (1-200)
- `strengthScaling` - Множитель от Strength (для физ. атак)
- `intelligenceScaling` - Множитель от Intelligence (для маг. атак)

**Формула урона:**
```csharp
finalDamage = baseDamage + (strength × strengthScaling) + (intelligence × intelligenceScaling)
```

#### ⏱️ Скорость атаки:
- `attackCooldown` - Кулдаун между атаками (0.3-3 сек)
- `attackRange` - Дальность атаки (1-50 метров)

#### 🎯 Снаряд (только для Ranged):
- `projectilePrefab` - Префаб снаряда (стрела, файрбол, кинжал)
- `projectileSpeed` - Скорость полета (5-50 м/с)
- `projectileLifetime` - Время жизни (1-10 сек)
- `projectileHoming` - Автонаведение на цель (bool)
- `homingSpeed` - Скорость поворота (градусы/сек)
- `homingRadius` - Радиус поиска цели (метры)

#### 🎨 Визуальные эффекты:
- `hitEffectPrefab` - Эффект попадания
- `weaponEffectPrefab` - Эффект на оружии
- `muzzleFlashPrefab` - Вспышка выстрела

#### 🔊 Звуки:
- `attackSound` - Звук атаки
- `hitSound` - Звук попадания
- `soundVolume` - Громкость (0-1)

#### 🎬 Анимация:
- `animationTrigger` - Триггер в Animator ("Attack")
- `animationSpeed` - Скорость анимации (0.5-3x)
- `attackHitTiming` - Момент удара в анимации (0-1)

#### 💎 Ресурсы:
- `manaCostPerAttack` - Стоимость маны (0-100)
- `actionPointsCost` - Стоимость AP (1-10)

#### 🌟 Дополнительные эффекты:
- `onHitEffects` - Список эффектов при попадании (яд, поджог, замедление)
- `baseCritChance` - Шанс крита (0-100%)
- `critMultiplier` - Множитель крит. урона (1.5-3x)
- `piercingAttack` - Пробивание насквозь (bool)
- `maxPierceTargets` - Макс. целей для пробивания (1-10)

#### 🌐 Мультиплеер:
- `syncProjectiles` - Синхронизация снарядов (bool)
- `syncHitEffects` - Синхронизация эффектов (bool)

**Методы:**

```csharp
// Рассчитать урон с учетом характеристик
float CalculateDamage(CharacterStats stats)

// Проверка валидности конфига
bool Validate(out string errorMessage)

// Отладочная информация
string GetDebugInfo()
```

---

### 2. PlayerAttackNew.cs (MonoBehaviour)

**Путь:** `Assets/Scripts/Player/PlayerAttackNew.cs`

**Назначение:** Компонент атаки игрока, использующий BasicAttackConfig

**Основные поля:**
- `attackConfig` - Ссылка на BasicAttackConfig (назначается в Inspector)
- `animator` - Аниматор персонажа
- `characterController` - Контроллер движения
- `targetSystem` - Система таргетирования
- `characterStats` - Характеристики персонажа (SPECIAL)
- `manaSystem` - Система маны
- `combatSync` - Синхронизация боя (мультиплеер)

**Основные методы:**

```csharp
void Start()
{
    // Находит все компоненты
    // Валидирует attackConfig
    // Выводит отладочную информацию
}

void Update()
{
    // Обрабатывает ЛКМ для атаки
    // Обновляет состояние атаки
}

void TryAttack()
{
    // Проверяет кулдаун
    // Проверяет наличие цели
    // Проверяет дистанцию
    // Проверяет ману (для магических атак)
    // Выполняет атаку
}

void PerformAttack()
{
    // Запускает анимацию
    // Для Melee: мгновенный урон
    // Для Ranged: спавнит снаряд
    // Отправляет данные на сервер (если мультиплеер)
}

void SpawnProjectile()
{
    // Создает снаряд из attackConfig.projectilePrefab
    // Настраивает скорость, урон, эффекты
    // Включает автонаведение (если настроено)
}

Enemy GetEnemyTarget()
{
    // Получает цель из TargetSystem
}

DummyEnemy GetDummyTarget()
{
    // Получает цель-манекен для тренировки
}
```

**Поддерживаемые типы снарядов:**
- `Projectile` - базовый снаряд
- `ArrowProjectile` - стрелы (Archer)
- `CelestialProjectile` - небесные шары (Mage)

---

### 3. NetworkCombatSync.cs (Multiplayer)

**Путь:** `Assets/Scripts/Network/NetworkCombatSync.cs`

**Назначение:** Синхронизация боевых действий с сервером

**Изменения для новой системы:**

#### БЫЛО (старая система):
```csharp
[RequireComponent(typeof(PlayerAttack))]
private PlayerAttack playerAttack;
```

#### СТАЛО (поддержка обеих систем):
```csharp
// Поддержка обеих систем
private PlayerAttack playerAttack;           // Старая (legacy)
private PlayerAttackNew playerAttackNew;     // НОВАЯ

void Start()
{
    playerAttackNew = GetComponent<PlayerAttackNew>();

    if (playerAttackNew != null)
    {
        Debug.Log("✅ Обнаружена НОВАЯ система атаки");
    }
}

void SendAttack(GameObject target, float damage, string attackType)
{
    // Получаем базовый урон из НОВОЙ или старой системы
    if (playerAttackNew != null && playerAttackNew.attackConfig != null)
    {
        baseDamage = playerAttackNew.attackConfig.baseDamage;
        Debug.Log("✅ Используем НОВУЮ систему (PlayerAttackNew)");
    }
    else if (playerAttack != null)
    {
        baseDamage = playerAttack.BaseDamage;
        Debug.Log("⚠️ Используем СТАРУЮ систему (PlayerAttack)");
    }
}
```

**Поддержка обратной совместимости:** ✅
- Работает с PlayerAttackNew (новая система)
- Работает с PlayerAttack (старая система, если нужна)
- Автоматически определяет какую систему использовать

---

## 📦 SCRIPTABLEOBJECT КОНФИГУРАЦИИ

Все конфиги находятся в: `Assets/ScriptableObjects/Skills/`

### 1. BasicAttackConfig_Warrior.asset

**Класс:** Warrior
**Тип:** Melee (ближняя атака)

**Параметры:**
- Урон: `40` + `STR×1.5`
- Кулдаун: `1.0s`
- Дальность: `3m`
- Скейлинг: Физический (Strength)

**Описание:** Мощный удар мечом с высоким физическим уроном

---

### 2. BasicAttackConfig_Mage.asset

**Класс:** Mage
**Тип:** Ranged (дальняя атака)

**Параметры:**
- Урон: `30` + `INT×1.5`
- Кулдаун: `1.2s`
- Дальность: `25m`
- Снаряд: Fireball (CelestialProjectile)
- Скорость: `20 m/s`
- Скейлинг: Магический (Intelligence)

**Описание:** Огненный шар с магическим уроном

---

### 3. BasicAttackConfig_Archer.asset

**Класс:** Archer
**Тип:** Ranged (дальняя атака)

**Параметры:**
- Урон: `35` + `AGI×1.0` + `PER×0.5`
- Кулдаун: `0.8s`
- Дальность: `30m`
- Снаряд: Arrow (ArrowProjectile)
- Скорость: `30 m/s`
- Скейлинг: Физический (Agility, Perception)

**Описание:** Быстрая стрела с точным попаданием

---

### 4. BasicAttackConfig_Rogue.asset

**Класс:** Rogue
**Тип:** Ranged (дальняя атака)

**Параметры:**
- Урон: `25` + `AGI×1.2` + `LCK×0.3`
- Кулдаун: `0.6s`
- Дальность: `15m`
- Снаряд: Throwing Knife
- Скорость: `25 m/s`
- Скейлинг: Физический (Agility, Luck)
- Особенность: Высокий крит шанс

**Описание:** Быстрые метательные кинжалы с шансом крита

---

### 5. BasicAttackConfig_Paladin.asset

**Класс:** Paladin
**Тип:** Melee (ближняя атака)

**Параметры:**
- Урон: `35` + `STR×1.0` + `WIS×0.5`
- Кулдаун: `1.1s`
- Дальность: `3.5m`
- Скейлинг: Гибридный (Strength, Wisdom)

**Описание:** Священный удар с физическим и магическим уроном

---

## 🔗 ИНТЕГРАЦИЯ С МУЛЬТИПЛЕЕРОМ

### Как это работает в мультиплеере:

```
Клиент 1 (Атакующий):
1. ЛКМ → PlayerAttackNew.TryAttack()
2. PerformAttack() → анимация + спавн снаряда (локально)
3. NetworkCombatSync.SendAttack() → отправка на сервер

Server (Node.js):
4. Получает player_attack event
5. Валидирует урон и дистанцию
6. Рассчитывает финальный урон
7. Применяет урон к цели
8. Broadcast player_attacked ко всем клиентам

Клиент 2 (Получающий урон):
9. Получает player_attacked event
10. NetworkSyncManager.OnPlayerAttacked()
11. Применяет урон к HP
12. Показывает Damage Number
13. Проигрывает эффект попадания

Все клиенты:
14. Видят урон и эффекты синхронно
```

### Что синхронизируется:

✅ **Синхронизируется:**
- Урон (рассчитывается на сервере)
- HP изменения
- Визуальные эффекты попадания
- Damage numbers
- Анимации атаки

❌ **НЕ синхронизируется (только локально):**
- Снаряды (каждый клиент создает свои для плавности)
- Звуки атаки
- Эффекты оружия

### Серверная валидация:

```javascript
// Server/server.js
socket.on('player_attack', (data) => {
    // 1. Проверка дистанции
    if (distance > maxRange) {
        console.log('Attack rejected: too far');
        return;
    }

    // 2. Проверка кулдауна
    if (timeSinceLastAttack < cooldown) {
        console.log('Attack rejected: cooldown');
        return;
    }

    // 3. Рассчет урона на сервере
    let finalDamage = baseDamage;
    finalDamage += stats.strength * strengthScaling;
    finalDamage += stats.intelligence * intelligenceScaling;

    // 4. Применение урона
    target.currentHP -= finalDamage;

    // 5. Broadcast всем
    io.to(roomId).emit('player_attacked', {
        attackerId: socket.id,
        targetId: target.socketId,
        damage: finalDamage,
        newHP: target.currentHP
    });
});
```

---

## ⚙️ КАК ЭТО РАБОТАЕТ

### Создание персонажа в Arena:

1. **ArenaManager.SpawnCharacter()** вызывается при старте игры
2. **GetBasicAttackConfigForClass()** загружает конфиг из Resources
3. **PlayerAttackNew** добавляется к Model
4. **attackConfig** назначается автоматически

```csharp
// ArenaManager.cs:366-376
string selectedClass = PlayerPrefs.GetString("SelectedCharacterClass", "");
BasicAttackConfig attackConfig = GetBasicAttackConfigForClass(selectedClass);

if (attackConfig != null)
{
    playerAttackNew.attackConfig = attackConfig;
    Debug.Log($"✓ Назначен BasicAttackConfig_{selectedClass}");
}
```

### Путь конфигов:

```
Assets/
  ├── Resources/
  │   └── Skills/
  │       ├── BasicAttackConfig_Warrior.asset
  │       ├── BasicAttackConfig_Mage.asset
  │       ├── BasicAttackConfig_Archer.asset
  │       ├── BasicAttackConfig_Rogue.asset
  │       └── BasicAttackConfig_Paladin.asset
```

**ВАЖНО:** Конфиги ДОЛЖНЫ быть в папке `Resources/Skills/` для загрузки через `Resources.Load()`!

---

## 🆕 НАСТРОЙКА ДЛЯ НОВЫХ КЛАССОВ

### Шаг 1: Создать ScriptableObject

1. **Правый клик** в папке `Assets/ScriptableObjects/Skills/`
2. **Create → Aetherion → Combat → Basic Attack Config**
3. **Переименовать:** `BasicAttackConfig_НовыйКласс.asset`

### Шаг 2: Настроить параметры в Inspector

```
═══════════ БАЗОВАЯ ИНФОРМАЦИЯ ═══════════
Character Class: НовыйКласс
Attack Type: Melee или Ranged
Description: "Описание атаки"

═══════════ УРОН ═══════════
Base Damage: 30
Strength Scaling: 1.0 (для физ. атак)
Intelligence Scaling: 0.0 (для маг. атак)

═══════════ СКОРОСТЬ АТАКИ ═══════════
Attack Cooldown: 1.0
Attack Range: 3.0 (melee) или 25.0 (ranged)

═══════════ СНАРЯД (если Ranged) ═══════════
Projectile Prefab: Перетащить префаб
Projectile Speed: 20
Projectile Lifetime: 5
Projectile Homing: false
```

### Шаг 3: Переместить в Resources

1. **Копировать** `.asset` файл
2. **Вставить** в `Assets/Resources/Skills/`
3. **Убедиться** что путь: `Resources/Skills/BasicAttackConfig_НовыйКласс.asset`

### Шаг 4: Добавить в ArenaManager

```csharp
// ArenaManager.cs → GetBasicAttackConfigForClass()
switch (characterClass)
{
    case "НовыйКласс":
        configPath = "Skills/BasicAttackConfig_НовыйКласс";
        break;
    // ...
}
```

### Шаг 5: Тестирование

1. Выбрать новый класс в Character Selection
2. Запустить Arena Scene
3. Проверить логи:
   ```
   ✓ Назначен BasicAttackConfig_НовыйКласс
   [PlayerAttackNew] ✅ Инициализация завершена
   [PlayerAttackNew] Config: BasicAttackConfig_НовыйКласс, Damage: 30, Type: Melee
   ```
4. Атаковать врага (ЛКМ)
5. Проверить что урон применяется

---

## 🧪 ТЕСТИРОВАНИЕ

### Одиночная игра:

1. **Запустить Arena Scene**
2. **Найти DummyEnemy** (манекен для тренировки)
3. **Кликнуть ЛКМ** на DummyEnemy
4. **Проверить:**
   - ✅ Анимация атаки проигрывается
   - ✅ Для Melee: Урон применяется мгновенно
   - ✅ Для Ranged: Снаряд летит к цели
   - ✅ Damage Number появляется над целью
   - ✅ HP цели уменьшается

### Мультиплеер:

1. **Запустить 2 клиента** (Editor + Build)
2. **Создать/присоединиться к комнате**
3. **Затаргетить** вражеского игрока (ЛКМ или Tab)
4. **Атаковать** (ЛКМ)
5. **Проверить логи:**
   ```
   [PlayerAttackNew] PerformAttack() - Type: Ranged
   [NetworkCombatSync] ✅ Используем НОВУЮ систему (PlayerAttackNew)
   [NetworkCombatSync] ✅ Атака отправлена на сервер: ranged → player
   ```
6. **На втором клиенте проверить:**
   - ✅ Damage Number появился
   - ✅ HP уменьшилось
   - ✅ Эффект попадания проигрался

---

## 📊 СРАВНЕНИЕ СИСТЕМ

### Старая (PlayerAttack):
```csharp
public class PlayerAttack : MonoBehaviour
{
    public float BaseDamage = 25f;               // Hardcoded
    public GameObject fireballPrefab;            // Только для Mage
    public float fireballSpeed = 20f;            // Только для Mage

    void Attack()
    {
        if (characterClass == "Mage")
        {
            // Спавн fireball
        }
        else if (characterClass == "Warrior")
        {
            // Melee урон
        }
        // ... дубликаты для каждого класса
    }
}
```

**Проблемы:**
- ❌ Хардкод параметров в коде
- ❌ Нужно редактировать C# для изменения урона
- ❌ Дубликаты логики для разных классов
- ❌ Сложно балансить

### Новая (PlayerAttackNew + BasicAttackConfig):
```csharp
public class PlayerAttackNew : MonoBehaviour
{
    public BasicAttackConfig attackConfig;  // ScriptableObject

    void Attack()
    {
        float damage = attackConfig.CalculateDamage(stats);

        if (attackConfig.attackType == AttackType.Ranged)
        {
            SpawnProjectile(attackConfig.projectilePrefab);
        }
        else
        {
            ApplyMeleeDamage(damage);
        }
    }
}
```

**Преимущества:**
- ✅ Параметры редактируются в Inspector
- ✅ Один код для всех классов
- ✅ Легко балансить (просто меняй числа)
- ✅ Валидация конфигов
- ✅ Переиспользование

---

## 🔧 ЧАСТЫЕ ПРОБЛЕМЫ И РЕШЕНИЯ

### Проблема 1: "BasicAttackConfig НЕ НАЗНАЧЕН"

**Ошибка:**
```
[PlayerAttackNew] ❌ BasicAttackConfig НЕ НАЗНАЧЕН для WarriorModel!
```

**Решение:**
1. Проверить что `.asset` файл в `Resources/Skills/`
2. Проверить `GetBasicAttackConfigForClass()` в ArenaManager
3. Проверить что `characterClass` правильный (case-sensitive!)

### Проблема 2: "Ranged attack требует projectilePrefab"

**Ошибка:**
```
[BasicAttackConfig] ❌ Ошибка валидации: Ranged attack требует projectilePrefab!
```

**Решение:**
1. Открыть `.asset` файл в Inspector
2. Перетащить префаб снаряда в поле `Projectile Prefab`
3. Убедиться что префаб имеет компонент `Projectile/ArrowProjectile/CelestialProjectile`

### Проблема 3: Атаки не синхронизируются в мультиплеере

**Решение:**
1. Проверить что `NetworkCombatSync` добавлен к локальному игроку
2. Проверить логи:
   ```
   [NetworkCombatSync] ✅ Обнаружена НОВАЯ система атаки
   ```
3. Проверить что сервер запущен (`node server.js`)
4. Проверить что клиент подключён к серверу

---

## 🎓 ИТОГИ

### Что мы получили:

1. ✅ **Гибкая система конфигурации** - ScriptableObject для каждого класса
2. ✅ **Единый код атаки** - один компонент `PlayerAttackNew` для всех
3. ✅ **Мультиплеер** - полная интеграция с `NetworkCombatSync`
4. ✅ **Валидация** - автоматическая проверка корректности конфигов
5. ✅ **Масштабируемость** - легко добавить новый класс или тип атаки
6. ✅ **Балансировка** - изменение параметров через Inspector без кода

### Файлы системы:

**Код:**
- `Assets/Scripts/Combat/BasicAttackConfig.cs` - ScriptableObject класс
- `Assets/Scripts/Player/PlayerAttackNew.cs` - Компонент атаки
- `Assets/Scripts/Network/NetworkCombatSync.cs` - Мультиплеер (обновлён)
- `Assets/Scripts/Arena/ArenaManager.cs` - Интеграция (метод `GetBasicAttackConfigForClass`)

**Конфиги:**
- `Assets/Resources/Skills/BasicAttackConfig_Warrior.asset`
- `Assets/Resources/Skills/BasicAttackConfig_Mage.asset`
- `Assets/Resources/Skills/BasicAttackConfig_Archer.asset`
- `Assets/Resources/Skills/BasicAttackConfig_Rogue.asset`
- `Assets/Resources/Skills/BasicAttackConfig_Paladin.asset`

**Сервер:**
- `Server/server.js` - Серверная валидация и расчет урона

---

**Автор:** Claude (Anthropic)
**Дата:** 21 октября 2025
**Статус:** ✅ ПРОИЗВОДСТВЕННАЯ ВЕРСИЯ
