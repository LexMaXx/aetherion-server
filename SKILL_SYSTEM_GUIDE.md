# ⚔️ Система Скиллов Aetherion (Lineage 2 Style)

## 📋 Обзор

Полноценная система скиллов по типу Lineage 2 с поддержкой:
- ✅ **6 скиллов на класс** (библиотека)
- ✅ **3 экипированных скилла** (хотбар)
- ✅ **Drag & Drop** выбор скиллов
- ✅ **MongoDB интеграция** (сохранение/загрузка)
- ✅ **Все типы скиллов**: урон, лечение, контроль, призыв, трансформация, бафы/дебафы
- ✅ **Расширяемая система** - легко добавлять новые скиллы

---

## 🎯 Быстрый старт

### 1. Создать базу данных скиллов

```
1. Unity → Tools → Skills → Create Skill Database
2. Это создаст:
   - Assets/Resources/SkillDatabase.asset
   - Assets/Resources/Skills/ (30 примеров скиллов)
```

### 2. Настройка Character Selection Scene

1. Откройте `CharacterSelectionScene`
2. Создайте UI панель скиллов:
   ```
   SkillPanel (Canvas)
   ├── LibrarySlots (6 SkillSlotUI) - библиотека
   └── EquippedSlots (3 SkillSlotUI) - экипированные
   ```
3. Добавьте `SkillSelectionManager` на GameObject
4. Назначьте:
   - `librarySlots` → 6 слотов библиотеки
   - `equippedSlots` → 3 слота экипировки
   - `skillDatabase` → SkillDatabase.asset

### 3. Настройка Arena Scene

1. На персонажа добавьте `SkillManager`
2. В скрипте загрузки персонажа:
   ```csharp
   SkillManager skillManager = player.GetComponent<SkillManager>();
   skillManager.LoadEquippedSkills(equippedSkillIds);
   ```

---

## 📦 Созданные файлы

### Основные системы:
```
Assets/Scripts/Skills/
├── SkillData.cs              - ScriptableObject для настройки скилла
├── SkillManager.cs            - Менеджер скиллов на персонаже (Arena)
├── ActiveEffect.cs            - Активные эффекты (бафы/дебафы/контроль)
└── SummonedCreature.cs        - Призванные существа (скелеты)

Assets/Scripts/UI/Skills/
├── SkillSlotUI.cs             - UI слот скилла (Drag & Drop)
└── SkillSelectionManager.cs   - Менеджер выбора скиллов (Character Selection)

Assets/Scripts/Data/
└── SkillDatabase.cs           - База данных всех скиллов

Assets/Scripts/Network/
└── ServerAPI_Skills.cs        - MongoDB интеграция
```

### Editor утилиты:
```
Assets/Scripts/Editor/
└── CreateSkillDatabase.cs     - Автосоздание базы и примеров
```

---

## 🎮 Типы скиллов

### 1. **Damage** - Урон
```csharp
// Одиночный урон
CreateSkill(101, "Power Strike", CharacterClass.Warrior,
    SkillType.Damage, baseDamage: 50f, strengthScaling: 20f);

// AOE урон
CreateSkill(102, "Whirlwind", CharacterClass.Warrior,
    SkillType.Damage, baseDamage: 35f, aoeRadius: 5f, maxTargets: 5);
```

### 2. **Heal** - Лечение
```csharp
CreateSkill(504, "Lay on Hands", CharacterClass.Paladin,
    SkillType.Heal, baseDamage: 100f, canTargetAllies: true);
```

### 3. **Buff/Debuff** - Усиления/Ослабления
```csharp
CreateSkill(103, "Battle Cry", CharacterClass.Warrior,
    SkillType.Buff, canTargetAllies: true);
```

### 4. **CrowdControl** - Контроль
```csharp
// Стан
CreateSkill(104, "Shield Bash", CharacterClass.Warrior,
    SkillType.CrowdControl, baseDamage: 20f);

// Корни (Root)
CreateSkill(305, "Entangling Shot", CharacterClass.Archer,
    SkillType.CrowdControl, baseDamage: 20f);
```

### 5. **Summon** - Призыв (Rogue - Скелеты)
```csharp
CreateSkill(402, "Summon Skeletons", CharacterClass.Rogue,
    SkillType.Summon, requiresTarget: false);

// В SkillData настроить:
skill.summonPrefab = skeletonPrefab;
skill.summonCount = 3;
skill.summonDuration = 30f;
```

### 6. **Transformation** - Трансформация (Paladin - Медведь)
```csharp
CreateSkill(502, "Bear Form", CharacterClass.Paladin,
    SkillType.Transformation, requiresTarget: false);

// В SkillData настроить:
skill.transformationModel = bearModel;
skill.transformationDuration = 30f;
skill.hpBonusPercent = 50f;        // +50% HP
skill.physicalDamageBonusPercent = 30f; // +30% урон
```

### 7. **Teleport** - Телепорт
```csharp
CreateSkill(203, "Teleport", CharacterClass.Mage,
    SkillType.Teleport, castRange: 15f);
```

### 8. **Ressurect** - Воскрешение
```csharp
CreateSkill(506, "Ressurection", CharacterClass.Paladin,
    SkillType.Ressurect, canTargetAllies: true);
```

---

## 🎨 Эффекты (Buffs/Debuffs/Control)

### Бонусы (Buffs):
```csharp
IncreaseAttack,      // Увеличение атаки
IncreaseDefense,     // Увеличение защиты
IncreaseSpeed,       // Увеличение скорости
IncreaseHPRegen,     // Регенерация HP
IncreaseMPRegen,     // Регенерация MP
Shield,              // Щит (поглощение урона)
```

### Дебаффы:
```csharp
DecreaseAttack,      // Уменьшение атаки
DecreaseDefense,     // Уменьшение защиты
DecreaseSpeed,       // Замедление
Poison,              // Яд (DOT)
Burn,                // Горение (DOT)
Bleed,               // Кровотечение (DOT)
```

### Контроль:
```csharp
Stun,                // Оглушение (не может двигаться/атаковать)
Root,                // Корни (не может двигаться, может атаковать)
Sleep,               // Сон (снимается при уроне)
Silence,             // Молчание (не может кастовать)
Fear,                // Страх (убегает)
Taunt,               // Провокация (атакует кастера)
```

### Особые:
```csharp
DamageOverTime,      // Кастомный DOT
HealOverTime,        // Лечение во времени (HoT)
Invulnerability,     // Неуязвимость
Invisibility         // Невидимость
```

### Пример добавления эффекта:
```csharp
SkillEffect stunEffect = new SkillEffect
{
    effectType = EffectType.Stun,
    duration = 3f,
    power = 0f,
    particleEffectPrefab = stunParticles,
    canBeDispelled = true
};

skill.effects.Add(stunEffect);
```

---

## 📝 Примеры скиллов по классам

### ⚔️ **WARRIOR** (Воин)
| ID | Название | Тип | Описание |
|----|----------|-----|----------|
| 101 | Power Strike | Урон | Мощный удар мечом |
| 102 | Whirlwind | AOE | Вихрь клинков вокруг себя |
| 103 | Battle Cry | Бафф | Увеличивает атаку союзников |
| 104 | Shield Bash | Стан | Удар щитом, оглушение |
| 105 | Charge | Рывок | Рывок к врагу с ударом |
| 106 | Berserker Rage | Бафф | Ярость берсерка (+атака, +скорость) |

### 🔮 **MAGE** (Маг)
| ID | Название | Тип | Описание |
|----|----------|-----|----------|
| 201 | Fireball | Урон | Огненный шар |
| 202 | Ice Nova | AOE | Взрыв льда, урон и замедление |
| 203 | Teleport | Телепорт | Мгновенный телепорт |
| 204 | Meteor | AOE | Метеорит, огромный урон |
| 205 | Mana Shield | Щит | Щит из маны |
| 206 | Lightning Storm | DOT | Гроза, периодический урон |

### 🏹 **ARCHER** (Лучник)
| ID | Название | Тип | Описание |
|----|----------|-----|----------|
| 301 | Piercing Shot | Урон | Пробивает насквозь |
| 302 | Explosive Arrow | AOE | Взрывная стрела |
| 303 | Rain of Arrows | AOE | Дождь стрел |
| 304 | Eagle Eye | Бафф | Увеличивает дальность и точность |
| 305 | Entangling Shot | Корни | Опутывает корнями |
| 306 | Volley | Урон | Залп из 5 стрел |

### 🗡️ **ROGUE** (Разбойник)
| ID | Название | Тип | Описание |
|----|----------|-----|----------|
| 401 | Backstab | Крит | Критический удар со спины |
| 402 | **Summon Skeletons** ⭐ | **Призыв** | **Призывает 3 скелетов** |
| 403 | Shadow Step | Телепорт | Телепорт за спину |
| 404 | Poison Blade | DOT | Яд на клинке |
| 405 | Smoke Bomb | Невидимость | Дымовая завеса |
| 406 | Execute | Урон | Огромный урон по раненым |

### ⚖️ **PALADIN** (Паладин)
| ID | Название | Тип | Описание |
|----|----------|-----|----------|
| 501 | Holy Strike | Урон | Святой удар |
| 502 | **Bear Form** ⭐ | **Трансформация** | **Превращение в медведя** |
| 503 | Divine Shield | Неуязвимость | Божественный щит |
| 504 | Lay on Hands | Лечение | Сильное лечение |
| 505 | Hammer of Justice | Стан | Молот правосудия |
| 506 | Resurrection | Воскрешение | Воскрешает союзника |

---

## 🔧 Как добавить новый скилл

### Способ 1: Через Inspector (для дизайнеров)

1. **Create → Aetherion → Skills → Skill Data**
2. Настройте параметры:
   ```
   Skill ID: уникальный номер (700+)
   Skill Name: название скилла
   Character Class: класс персонажа
   Skill Type: тип скилла
   Cooldown: перезарядка (сек)
   Mana Cost: стоимость маны
   Cast Range: дальность (метры)
   Base Damage: базовый урон
   ```
3. Сохраните в `Assets/Resources/Skills/`
4. Добавьте в `SkillDatabase`

### Способ 2: Через код (для программистов)

```csharp
// В CreateSkillDatabase.cs добавить:
CreateSkill(db, 701, "My Custom Skill", "Описание",
    CharacterClass.Warrior,
    SkillType.Damage,
    cooldown: 10f,
    manaCost: 40f,
    castRange: 5f,
    castTime: 0.5f,
    baseDamage: 60f,
    strengthScaling: 20f);
```

---

## 🎮 Использование в игре

### Character Selection:
1. Игрок видит 6 скиллов класса (библиотека)
2. Drag & Drop 3 скилла в слоты экипировки
3. При создании персонажа → сохранение в MongoDB

### Arena Scene:
1. При загрузке персонажа → загрузка 3 скиллов из MongoDB
2. Назначение на кнопки 1, 2, 3
3. Использование скиллов:
   ```csharp
   // По кнопке 1
   if (Input.GetKeyDown(KeyCode.Alpha1))
   {
       skillManager.UseSkill(0, currentTarget);
   }
   ```

---

## 🌟 Особые скиллы

### 1. **Summon Skeletons** (Rogue)

**Настройка:**
```csharp
// В SkillData:
skill.summonPrefab = skeletonPrefab; // Префаб скелета
skill.summonCount = 3;                // 3 скелета
skill.summonDuration = 30f;           // 30 секунд жизни
```

**Поведение скелетов:**
- Автоматически атакуют врагов с таргета хозяина
- Следуют за игроком если нет цели
- Наносят урон каждые 2 секунды
- Исчезают через 30 секунд

### 2. **Bear Form** (Paladin)

**Настройка:**
```csharp
// В SkillData:
skill.transformationModel = bearModel;        // Модель медведя
skill.transformationDuration = 30f;           // 30 секунд
skill.hpBonusPercent = 50f;                  // +50% HP
skill.physicalDamageBonusPercent = 30f;      // +30% урон
```

**Эффекты:**
- Модель персонажа заменяется на медведя
- Увеличивается HP и максимальный HP
- Увеличивается физический урон
- Автоматически отменяется через 30 секунд

---

## 📡 MongoDB интеграция

### Сохранение:
```csharp
List<int> equippedSkillIds = skillSelectionManager.GetEquippedSkillIds();
ServerAPI.Instance.SaveCharacterSkills("Warrior", equippedSkillIds, (success) => {
    Debug.Log($"Скиллы сохранены: {success}");
});
```

### Загрузка:
```csharp
ServerAPI.Instance.LoadCharacterSkills("Warrior", (skillIds, success) => {
    if (success)
    {
        skillManager.LoadEquippedSkills(skillIds);
    }
});
```

### Формат данных (JSON):
```json
{
  "characterClass": "Warrior",
  "equippedSkills": [101, 102, 103],
  "timestamp": 1696936800
}
```

---

## ✅ Чеклист настройки

- [ ] Создана SkillDatabase через Tools → Skills → Create Skill Database
- [ ] В Character Selection добавлена UI панель скиллов
- [ ] Настроен SkillSelectionManager с 6+3 слотами
- [ ] В Arena Scene добавлен SkillManager на персонажа
- [ ] Интегрирована загрузка скиллов из MongoDB
- [ ] Назначены горячие клавиши (1, 2, 3)
- [ ] Созданы префабы для призванных существ (скелеты)
- [ ] Создана модель трансформации (медведь)
- [ ] Добавлены визуальные эффекты скиллов
- [ ] Добавлены звуки скиллов

---

## 🚀 Расширение системы

### Добавление новых типов эффектов:

1. Добавить в `EffectType` enum:
   ```csharp
   public enum EffectType
   {
       // ... существующие
       Confusion,  // Замешательство
       Charm,      // Очарование
       Slow        // Замедление
   }
   ```

2. Обработать в `ActiveEffect.cs`
3. Добавить логику в `SkillManager.cs`

### Добавление новых типов скиллов:

1. Добавить в `SkillType` enum
2. Создать метод Execute в `SkillManager`
3. Добавить настройки в `SkillData`

---

**Система готова к использованию!** 🎉

Все скиллы полностью настраиваются через Inspector, легко добавлять новые, и всё автоматически синхронизируется с MongoDB.
