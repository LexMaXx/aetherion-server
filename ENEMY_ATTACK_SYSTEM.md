# ✅ Система атаки врага готова!

## Описание
**DummyEnemyAttacker** - компонент который заставляет DummyEnemy атаковать игрока каждую секунду и отображать урон.

---

## Что было создано

**Файл:** `Assets/Scripts/Arena/DummyEnemyAttacker.cs`

### Функционал:
✅ Атакует игрока каждую секунду (настраивается)
✅ Наносит урон с вариацией (10 ± 2 урона)
✅ Имеет шанс крита (10% × 2 урона)
✅ Отображает цифры урона над игроком через DamageNumberManager
✅ Наносит урон через HealthSystem игрока
✅ Визуализация в редакторе (красная линия к игроку)

### Параметры:
```csharp
[Header("Attack Settings")]
attackInterval = 1f;         // Атака каждую секунду
baseDamage = 10f;            // Базовый урон
damageVariation = 2f;        // ±2 вариация
critChance = 10f;            // 10% шанс крита
critMultiplier = 2f;         // x2 урон при крите

[Header("Target")]
playerTag = "Player";        // Тег игрока
```

---

## Как настроить

### Шаг 1: Добавить компонент на DummyEnemy

**В Unity:**
1. Открой сцену `SkillTestScene`
2. Выбери любого **DummyEnemy** в Hierarchy
3. **Add Component** → DummyEnemyAttacker
4. Настрой параметры (по желанию):
   - Attack Interval: `1` (атакует каждую секунду)
   - Base Damage: `10` (базовый урон)
   - Damage Variation: `2` (±2 урона)
   - Crit Chance: `10` (10% шанс крита)
   - Crit Multiplier: `2` (x2 урон при крите)
   - Player Tag: `Player`

### Шаг 2: Убедиться что у игрока есть компоненты

У игрока должны быть:
- ✅ **Tag: Player** (чтобы враг его нашёл)
- ✅ **HealthSystem** (для получения урона)
- ✅ **CharacterStats** (опционально, для максимального HP)

В сцене должен быть:
- ✅ **DamageNumberManager** (для отображения цифр урона)

---

## Как работает

### 1. Start - поиск игрока
```csharp
GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
player = playerObj.transform;
playerHealthSystem = player.GetComponent<HealthSystem>();
damageNumberManager = FindObjectOfType<DamageNumberManager>();
```

### 2. Update - таймер атаки
```csharp
attackTimer -= Time.deltaTime;
if (attackTimer <= 0f)
{
    AttackPlayer();
    attackTimer = attackInterval; // Каждую секунду
}
```

### 3. AttackPlayer - нанесение урона
```csharp
// Рассчитываем урон
float damage = baseDamage + Random.Range(-damageVariation, damageVariation);
// 10 + Random(-2, 2) = от 8 до 12 урона

// Проверяем крит
bool isCrit = Random.Range(0f, 100f) < critChance;
if (isCrit)
{
    damage *= critMultiplier; // 10 * 2 = 20 урона при крите
}

// Наносим урон
playerHealthSystem.TakeDamage(damage);

// Показываем цифры урона
damageNumberManager.ShowDamage(player.position + Vector3.up * 2f, damage, isCrit, false);
```

---

## Console Logs

```
[DummyEnemyAttacker] Найден игрок: TestPlayer
[DummyEnemyAttacker] ⚔️ Атака! Урон: 11.2
[HealthSystem] -11 HP. Осталось: 289/300
[DamageNumberManager] ✅ Урон 11.2 показан (Crit: False)

... 1 секунда проходит ...

[DummyEnemyAttacker] ⚔️ Атака! Урон: 18.6 КРИТ!
[HealthSystem] -19 HP. Осталось: 270/300
[DamageNumberManager] ✅ Урон 18.6 показан (Crit: True)
```

---

## Визуализация

### В игре
- Цифры урона над головой игрока (красные или жёлтые при крите)
- HP бар игрока уменьшается

### В редакторе (Scene View)
- Красная линия от врага к игроку (Gizmos)
- Красная линия при атаке (Debug.DrawLine)

---

## Публичные методы

Можно управлять атаками через код:

```csharp
DummyEnemyAttacker attacker = GetComponent<DummyEnemyAttacker>();

// Изменить интервал атаки
attacker.SetAttackInterval(0.5f); // Атака каждые 0.5 сек

// Изменить базовый урон
attacker.SetBaseDamage(20f); // 20 урона вместо 10

// Включить/выключить атаки
attacker.SetEnabled(false); // Остановить атаки
attacker.SetEnabled(true);  // Возобновить атаки
```

---

## Примеры настроек

### Слабый враг (новичок)
```
Attack Interval: 2.0s
Base Damage: 5
Damage Variation: 1
Crit Chance: 5%
Crit Multiplier: 1.5x
```

### Средний враг (стандарт)
```
Attack Interval: 1.0s
Base Damage: 10
Damage Variation: 2
Crit Chance: 10%
Crit Multiplier: 2.0x
```

### Сильный враг (босс)
```
Attack Interval: 0.5s
Base Damage: 25
Damage Variation: 5
Crit Chance: 20%
Crit Multiplier: 3.0x
```

---

## Тестирование защитных баффов

Теперь можно протестировать защитные скиллы воина:
1. Добавь DummyEnemyAttacker на врага
2. Враг начнёт атаковать игрока
3. Используй защитный скилл воина (щит, броня, и т.д.)
4. Смотри как урон уменьшается!

**Идеально для тестирования:**
- Щиты
- Броня
- Уклонение
- Блокирование
- Контратаки

---

## Следующие шаги

Теперь можно создавать скиллы воина:
1. **Железная кожа** - увеличение защиты
2. **Блок щитом** - блокирование атак
3. **Контратака** - урон при получении урона
4. **Берсерк** - больше урона, меньше защиты
5. **Боевой клич** - AOE бафф союзникам

---

**Статус:** ✅ ГОТОВО

Враг теперь атакует игрока и показывает урон! ⚔️
