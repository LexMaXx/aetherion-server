# ПОЛНОЕ РУКОВОДСТВО ПО НАСТРОЙКЕ СКИЛЛОВ В AETHERION

## ОБЗОР УЛУЧШЕНИЙ

Система скиллов была полностью обновлена для корректной работы визуальных эффектов, снарядов и сетевой синхронизации.

### ЧТО БЫЛО ДОБАВЛЕНО:

1. **Расширенные настройки визуальных эффектов** в SkillData
2. **Настройки снарядов** (скорость, самонаведение, время жизни)
3. **Улучшенные SkillEffect** (стакирование, звуки, сетевая синхронизация)
4. **Новые методы в Projectile** для применения настроек из SkillData
5. **Методы синхронизации с сервером** для урона и баффов

---

## СТРУКТУРА SKILLDATA

### Основные поля:

```
[Header("Основная информация")]
- skillId: int - Уникальный ID скилла (101-106 Warrior, 201-206 Mage и т.д.)
- skillName: string - Название скилла
- description: string - Описание для UI
- icon: Sprite - Иконка для UI
- characterClass: CharacterClass - Класс персонажа

[Header("Параметры использования")]
- cooldown: float - Перезарядка (секунды)
- manaCost: float - Стоимость маны
- castRange: float - Дальность применения (метры)
- castTime: float - Время каста (0 = мгновенно)
- canUseWhileMoving: bool - Можно ли использовать на ходу

[Header("Тип скилла")]
- skillType: SkillType - Damage/Heal/Buff/Debuff/CrowdControl/Summon/Transformation/Teleport/Ressurect

[Header("Целевая система")]
- targetType: SkillTargetType - Self/SingleTarget/GroundTarget/NoTarget/Directional
- requiresTarget: bool - Нужна ли цель
- canTargetAllies: bool - Можно ли на союзников
- canTargetEnemies: bool - Можно ли на врагов

[Header("Урон / Лечение")]
- baseDamageOrHeal: float - Базовое значение
- intelligenceScaling: float - Скейлинг от Intelligence
- strengthScaling: float - Скейлинг от Strength

[Header("Эффекты")]
- effects: List<SkillEffect> - Список баффов/дебаффов/контроля

[Header("AOE")]
- aoeRadius: float - Радиус области (0 = одна цель)
- maxTargets: int - Максимум целей
```

### НОВЫЕ ПОЛЯ - Визуальные эффекты:

```csharp
[Header("Визуальные эффекты")]
visualEffectPrefab: GameObject
  - Эффект каста (вспышка, магический круг на земле)
  - Создается в момент использования скилла
  - Примеры: вспышка света, огненный круг, ледяной взрыв

casterEffectPrefab: GameObject
  - Эффект на кастере во время каста
  - Свечение рук мага, аура вокруг персонажа
  - Примеры: светящиеся руки, аура силы, магическая энергия
```

### НОВЫЕ ПОЛЯ - Снаряды:

```csharp
[Header("Снаряды")]
projectilePrefab: GameObject
  - Префаб летящего снаряда
  - Должен иметь компонент Projectile.cs
  - Примеры: файрбол, стрела, молот, ледяной шар

projectileHitEffectPrefab: GameObject
  - Эффект попадания снаряда
  - Создается когда снаряд попадает в цель
  - Примеры: взрыв, искры, ледяные осколки

projectileSpeed: float (default 20)
  - Скорость полета снаряда в м/с
  - 10 = медленный, 20 = средний, 40 = быстрый

projectileHoming: bool (default false)
  - Самонаведение на цель
  - true = снаряд следует за целью
  - false = прямолинейный полет

projectileLifetime: float (default 5)
  - Время жизни снаряда в секундах
  - Снаряд самоуничтожится через это время
```

### НОВЫЕ ПОЛЯ - Звуки:

```csharp
[Header("Звуки")]
castSound: AudioClip - Звук каста скилла
impactSound: AudioClip - Звук попадания (AOE/мгновенный урон)
projectileHitSound: AudioClip - Звук попадания снаряда
```

---

## СТРУКТУРА SKILLEFFECT (УЛУЧШЕННАЯ)

### Основные параметры:

```csharp
[Header("Основные параметры")]
effectType: EffectType
  - Тип эффекта (IncreaseAttack, Stun, Poison и т.д.)

duration: float
  - Длительность эффекта в секундах

power: float
  - Сила эффекта:
    - Для баффов/дебаффов: % изменения (25 = +25% атаки)
    - Для DoT: урон за тик
    - Для Shield: количество HP щита
```

### Урон/Лечение во времени:

```csharp
[Header("Урон/Лечение во времени")]
damageOrHealPerTick: float
  - Урон или лечение за тик
  - Отрицательное = урон, положительное = лечение

tickInterval: float
  - Интервал между тиками в секундах
  - Обычно 1.0 (раз в секунду)
```

### НОВЫЕ ПОЛЯ - Визуальные эффекты:

```csharp
[Header("Визуальные эффекты")]
particleEffectPrefab: GameObject
  - Визуальный эффект НА ЦЕЛИ
  - Примеры: огонь (горение), зеленые пузырьки (яд), светящаяся аура (бафф)

applySound: AudioClip
  - Звук применения эффекта

removeSound: AudioClip
  - Звук окончания эффекта
```

### НОВЫЕ ПОЛЯ - Настройки:

```csharp
[Header("Настройки")]
canBeDispelled: bool (default true)
  - Можно ли снять эффект dispel'ом

canStack: bool (default false)
  - Можно ли стакать эффект
  - true = можно накладывать несколько раз

maxStacks: int (default 1)
  - Максимум стаков эффекта
  - Работает только если canStack = true

[Header("Сетевая синхронизация")]
syncWithServer: bool (default true)
  - Синхронизировать эффект с сервером
  - true = эффект отправляется на сервер (для PvP)
  - false = только локально (для PvE баффов)
```

---

## ПОШАГОВАЯ НАСТРОЙКА СКИЛЛА

### ШАГ 1: СОЗДАНИЕ SKILL DATA ASSET

1. В Unity: `Assets → Create → Aetherion → Skills → Skill Data`
2. Назовите файл по шаблону: `ClassName_SkillName.asset`
   - Примеры: `Mage_Fireball.asset`, `Warrior_PowerStrike.asset`

### ШАГ 2: БАЗОВЫЕ НАСТРОЙКИ

```
Основная информация:
- skillId: 201 (для Mage Fireball)
- skillName: "Fireball"
- description: "Метает огненный шар, наносящий урон и поджигающий врага"
- icon: [Перетащите иконку]
- characterClass: Mage

Параметры:
- cooldown: 5.0
- manaCost: 30
- castRange: 25
- castTime: 0 (мгновенный)
- canUseWhileMoving: true

Тип скилла:
- skillType: Damage
- targetType: SingleTarget
- requiresTarget: true
- canTargetEnemies: true

Урон:
- baseDamageOrHeal: 50
- intelligenceScaling: 2.0 (урон = 50 + Intelligence * 2)
```

### ШАГ 3: НАСТРОЙКА СНАРЯДА

```
[Снаряды]
- projectilePrefab: FireballProjectile (из Assets/Prefabs/Projectiles/)
- projectileHitEffectPrefab: FireExplosion (эффект взрыва)
- projectileSpeed: 25
- projectileHoming: false
- projectileLifetime: 3.0

[Звуки]
- castSound: Fire_Cast
- projectileHitSound: Fire_Explode
```

### ШАГ 4: ДОБАВЛЕНИЕ ЭФФЕКТОВ (ГОРЕНИЕ)

Нажмите `+` в списке `effects`:

```
[Основные параметры]
- effectType: Burn
- duration: 5.0
- power: 10 (10 урона за тик)

[Урон во времени]
- damageOrHealPerTick: 10
- tickInterval: 1.0 (каждую секунду)

[Визуальные эффекты]
- particleEffectPrefab: Fire_Particles (огонь на персонаже)
- applySound: Fire_Apply
- removeSound: Fire_Remove

[Настройки]
- canBeDispelled: true
- canStack: false
- syncWithServer: true
```

---

## ПРИМЕРЫ НАСТРОЙКИ РАЗНЫХ ТИПОВ СКИЛЛОВ

### ПРИМЕР 1: МГНОВЕННЫЙ УРОН (БЕЗ СНАРЯДА)

**Warrior - Power Strike**

```
Тип: Damage
Снаряд: НЕТ (projectilePrefab = NULL)
Визуал: visualEffectPrefab = Slash_Effect

Эффект (Bleed):
- effectType: Bleed
- duration: 6.0
- damageOrHealPerTick: 5
- particleEffectPrefab: Blood_Particles
```

### ПРИМЕР 2: СНАРЯД С КОНТРОЛЕМ

**Paladin - Hammer of Justice**

```
Тип: CrowdControl
Снаряд: HammerProjectile
projectileSpeed: 15
projectileHoming: true

Эффект (Stun):
- effectType: Stun
- duration: 2.0
- particleEffectPrefab: Stun_Stars (звездочки над головой)
```

### ПРИМЕР 3: БАФФ НА СЕБЯ

**Warrior - Battle Cry**

```
Тип: Buff
targetType: Self
requiresTarget: false

Эффект (IncreaseAttack):
- effectType: IncreaseAttack
- duration: 15.0
- power: 25 (повышение на 25%)
- particleEffectPrefab: Red_Aura (красная аура вокруг персонажа)
- canStack: false
- syncWithServer: true
```

### ПРИМЕР 4: AOE УРОН

**Mage - Ice Nova**

```
Тип: Damage
targetType: NoTarget
aoeRadius: 8.0
maxTargets: 10

projectilePrefab: IceShardProjectile (создаст 12 осколков радиально)
visualEffectPrefab: Ice_Explosion

Эффект (DecreaseSpeed):
- effectType: DecreaseSpeed
- duration: 3.0
- power: 40 (замедление на 40%)
- particleEffectPrefab: Ice_Particles
```

### ПРИМЕР 5: ЛЕЧЕНИЕ

**Paladin - Lay on Hands**

```
Тип: Heal
targetType: SingleTarget
canTargetAllies: true

baseDamageOrHeal: 100
intelligenceScaling: 1.5

visualEffectPrefab: Holy_Light
impactSound: Heal_Sound
```

### ПРИМЕР 6: ТРАНСФОРМАЦИЯ

**Paladin - Bear Form**

```
Тип: Transformation
targetType: Self

transformationModel: BearForm (префаб медведя)
transformationDuration: 30.0
hpBonusPercent: 50 (+50% HP)
physicalDamageBonusPercent: 30 (+30% урона)

visualEffectPrefab: Transformation_Smoke
```

---

## CHECKLIST НАСТРОЙКИ СКИЛЛА

### ✅ Базовая настройка:
- [ ] Установлен уникальный skillId
- [ ] Заполнены skillName и description
- [ ] Назначена icon
- [ ] Выбран characterClass
- [ ] Настроены cooldown, manaCost, castRange
- [ ] Выбран правильный skillType

### ✅ Визуальные эффекты:
- [ ] Назначен visualEffectPrefab (эффект каста)
- [ ] Назначен casterEffectPrefab (опционально)
- [ ] Назначен castSound

### ✅ Для скиллов со снарядами:
- [ ] Назначен projectilePrefab
- [ ] Настроен projectileSpeed
- [ ] Настроен projectileHoming
- [ ] Назначен projectileHitEffectPrefab
- [ ] Назначен projectileHitSound

### ✅ Для скиллов с эффектами:
- [ ] Добавлены SkillEffect в список
- [ ] Для каждого эффекта:
  - [ ] Установлен effectType
  - [ ] Установлена duration
  - [ ] Установлена power
  - [ ] Назначен particleEffectPrefab
  - [ ] Настроен syncWithServer

### ✅ Тестирование:
- [ ] Скилл применяется без ошибок
- [ ] Визуальные эффекты отображаются
- [ ] Снаряд летит корректно
- [ ] Эффекты применяются к цели
- [ ] Звуки проигрываются
- [ ] В мультиплеере работает синхронизация

---

## ТИПЫ ЭФФЕКТОВ (EFFECTTYPE)

### Баффы (положительные):
- **IncreaseAttack** - Увеличение атаки (power = %)
- **IncreaseDefense** - Увеличение защиты (power = %)
- **IncreaseSpeed** - Увеличение скорости (power = %)
- **IncreaseHPRegen** - Регенерация HP
- **IncreaseMPRegen** - Регенерация MP
- **Shield** - Щит (power = количество HP щита)
- **HealOverTime** - Лечение во времени
- **Invulnerability** - Неуязвимость

### Дебаффы (отрицательные):
- **DecreaseAttack** - Уменьшение атаки (power = %)
- **DecreaseDefense** - Уменьшение защиты (power = %)
- **DecreaseSpeed** - Замедление (power = %)
- **Poison** - Яд (damageOrHealPerTick = урон)
- **Burn** - Горение (damageOrHealPerTick = урон)
- **Bleed** - Кровотечение (damageOrHealPerTick = урон)
- **DamageOverTime** - Урон во времени (кастомный)

### Контроль (Crowd Control):
- **Stun** - Оглушение (не может двигаться/атаковать)
- **Root** - Корни (не может двигаться, может атаковать)
- **Sleep** - Сон (снимается при уроне)
- **Silence** - Молчание (не может использовать скиллы)
- **Fear** - Страх (убегает)
- **Taunt** - Провокация (атакует кастера)

---

## ЧАСТЫЕ ПРОБЛЕМЫ И РЕШЕНИЯ

### Проблема: Снаряд не создается
**Решение:**
- Убедитесь что projectilePrefab назначен
- Проверьте что префаб имеет компонент Projectile.cs
- Проверьте что вы не NetworkPlayer (снаряды создаются только для локального игрока)

### Проблема: Эффект не отображается
**Решение:**
- Назначьте particleEffectPrefab в SkillEffect
- Проверьте что префаб содержит ParticleSystem
- Проверьте что эффект не применяется к NetworkPlayer (визуальные эффекты отключены для удаленных игроков)

### Проблема: Бафф не увеличивает урон
**Решение:**
- Установите syncWithServer = true
- Проверьте что power установлен правильно (25 = +25%)
- Убедитесь что CharacterStats.ModifyPhysicalDamage() вызывается

### Проблема: Снаряд летит слишком медленно/быстро
**Решение:**
- Настройте projectileSpeed (10-40)
- Используйте projectileHoming = true для самонаведения

---

## СОВЕТЫ ПО ОПТИМИЗАЦИИ

1. **Используйте Object Pooling** для часто создаваемых снарядов
2. **Ограничьте количество particle effects** - не больше 2-3 на скилл
3. **Используйте syncWithServer = false** для PvE баффов (меньше сетевого трафика)
4. **Не стакайте больше 5 раз** - maxStacks = 5 максимум
5. **Lifetime снарядов 3-5 секунд** - не делайте слишком долгие

---

## ЧТО ДАЛЬШЕ?

### Следующие шаги:
1. Настройте все 30+ скиллов по этому гайду
2. Импортируйте визуальные эффекты (из Hovl Studio или другие)
3. Назначьте префабы в каждый ScriptableObject
4. Протестируйте каждый скилл в игре
5. Обновите серверный код (см. SERVER_UPDATE_GUIDE.md)

### Серверная часть:
Для полной работы системы нужно обновить SERVER_CODE/socket/gameSocket.js:
- Добавить обработчик `skill_damage`
- Добавить обработчик `effect_applied`
- Валидировать урон на сервере

**Подробнее см. SERVER_UPDATE_GUIDE.md** (будет создан отдельно)

---

ГОТОВО! Теперь у вас есть полное руководство по настройке скиллов! 🎉
