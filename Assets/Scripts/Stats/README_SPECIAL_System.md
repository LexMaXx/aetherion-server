# SPECIAL - Система характеристик персонажа

## 📋 Обзор

Полноценная RPG система характеристик по типу Fallout SPECIAL:
- **7 характеристик**: Сила, Восприятие, Выносливость, Мудрость, Интеллект, Ловкость, Удача
- **20 уровней** максимум
- **15 начальных очков** (минимум 1 в каждой характеристике = 7 очков + 8 свободных)
- **1 очко за уровень** для прокачки
- **Готовность к онлайн**: заглушка для Node.js сервера

---

## 🎯 Характеристики SPECIAL

### **Strength (Сила)** 💪
- **Влияет на**: Физический урон (ближний бой)
- **Формула**: `Урон = Урон_оружия + (Сила × 5)`
- **Редактируется в**: StatsFormulas → strengthDamageBonus

### **Perception (Восприятие)** 👁️
- **Влияет на**: Радиус Fog of War
- **Формула**: `Радиус = 10 + (Восприятие × 3)` (макс 40м)
- **Редактируется в**: StatsFormulas → baseVisionRadius, perceptionRadiusBonus, maxVisionRadius

### **Endurance (Выносливость)** ❤️
- **Влияет на**: Максимальное здоровье (HP)
- **Формула**: `HP = 100 + (Выносливость × 20)`
- **Редактируется в**: StatsFormulas → baseHealth, enduranceHealthBonus

### **Wisdom (Мудрость)** 🔮
- **Влияет на**: Максимальная мана (MP) и регенерация
- **Формула MP**: `MP = 50 + (Мудрость × 15)`
- **Формула Regen**: `Regen = 1 + (Мудрость × 0.5)` MP/сек
- **Редактируется в**: StatsFormulas → baseMana, wisdomManaBonus, baseManaRegen, wisdomManaRegenBonus

### **Intelligence (Интеллект)** 🧠
- **Влияет на**: Магический урон (дальний бой)
- **Формула**: `Урон = Урон_заклинания + (Интеллект × 5)`
- **Редактируется в**: StatsFormulas → intelligenceDamageBonus

### **Agility (Ловкость)** ⚡
- **Влияет на**: Очки действия (AP) и скорость восстановления
- **Формула AP**: `AP = 3 + (Ловкость × 0.9)` (макс 12)
- **Формула Regen**: `Regen = 1 + (Ловкость × 0.1)` AP/сек
- **Редактируется в**: StatsFormulas → baseActionPoints, agilityAPBonus, maxActionPoints

### **Luck (Удача)** 🍀
- **Влияет на**: Шанс критического удара
- **Формула**: `Крит% = 5 + (Удача × 3)` (макс 35%)
- **Множитель крита**: 2x урона
- **Редактируется в**: StatsFormulas → baseCritChance, luckCritBonus, maxCritChance, critDamageMultiplier

---

## 🛠️ Редактирование характеристик

### 1. Создать StatsFormulas asset
```
Unity → Assets → Create → Aetherion → Stats Formulas
Сохранить в: Assets/Resources/StatsFormulas.asset
```

### 2. Настроить формулы
Откройте созданный asset и настройте параметры:
- **Strength**: strengthDamageBonus = 5
- **Perception**: baseVisionRadius = 10, perceptionRadiusBonus = 3
- **Endurance**: baseHealth = 100, enduranceHealthBonus = 20
- **Wisdom**: baseMana = 50, wisdomManaBonus = 15
- **Intelligence**: intelligenceDamageBonus = 5
- **Agility**: baseActionPoints = 3, agilityAPBonus = 0.9
- **Luck**: baseCritChance = 5, luckCritBonus = 3

---

## 👥 Настройка классов

### Создать пресет класса
```
Unity → Assets → Create → Aetherion → Class Stats Preset
Сохранить в: Assets/Resources/ClassStats/[ClassName]Stats.asset
```

### Примеры распределения (всего 15 очков):

**Warrior** (Воин):
- S:4 P:2 E:3 W:1 I:1 A:2 L:2
- Фокус: Сила, Выносливость

**Mage** (Маг):
- S:1 P:2 E:1 W:4 I:4 A:1 L:2
- Фокус: Мудрость, Интеллект

**Archer** (Лучник):
- S:2 P:4 E:2 W:1 I:2 A:3 L:1
- Фокус: Восприятие, Ловкость

**Rogue** (Разбойник):
- S:2 P:2 E:2 W:1 I:1 A:4 L:3
- Фокус: Ловкость, Удача

**Paladin** (Паладин):
- S:3 P:2 E:3 W:2 I:1 A:2 L:2
- Фокус: Сила, Выносливость, Мудрость

---

## 🎮 Тестирование в редакторе

### CharacterStats
- Right-click компонент → Debug Mode
- Прокачайте характеристики руками, смотрите на Calculated Stats

### LevelingSystem
- Right-click компонент → Context Menu → "Test: Gain 50 EXP"
- Right-click компонент → Context Menu → "Test: Level Up"
- Right-click компонент → Context Menu → "Test: Add Stat Point"

### HealthSystem / ManaSystem
- Right-click → "Test: Take 20 Damage"
- Right-click → "Test: Heal 30 HP"
- Right-click → "Test: Spend 20 Mana"

---

## 💾 Сохранение / Загрузка

### Локальное (PlayerPrefs) - СЕЙЧАС
```csharp
// Сохранить
ServerAPI.Instance.SaveCharacter(
    "Warrior",
    characterStats.GetStatsData(),
    levelingSystem.GetLevelingData()
);

// Загрузить
ServerAPI.Instance.LoadCharacter("Warrior", (stats, leveling, success) => {
    if (success) {
        characterStats.LoadStatsData(stats);
        levelingSystem.LoadLevelingData(leveling);
    }
});
```

### Node.js сервер - БУДУЩЕЕ
1. В ServerAPI.cs поменяйте `useLocalStorage = false`
2. Укажите `serverURL = "http://ваш-сервер.com/api"`
3. API endpoints:
   - POST `/character/save` - сохранить персонажа
   - GET `/character/load?class=Warrior` - загрузить

---

## 📦 Структура файлов

```
Assets/Scripts/Stats/
├── StatsFormulas.cs           # Редактируемые формулы
├── ClassStatsPreset.cs        # Пресеты классов
├── CharacterStats.cs          # Компонент характеристик
└── LevelingSystem.cs          # Система прокачки

Assets/Scripts/Player/
├── HealthSystem.cs            # Система HP
├── ManaSystem.cs              # Система MP
├── ActionPointsSystem.cs      # Система AP (изменена)
└── PlayerAttack.cs            # Урон + крит (изменена)

Assets/Scripts/Effects/
└── FogOfWar.cs                # Туман войны (изменена)

Assets/Scripts/Network/
└── ServerAPI.cs               # Заглушка для сервера

Assets/Scripts/Arena/
└── ArenaManager.cs            # Автоматическое добавление систем

Assets/Resources/
├── StatsFormulas.asset        # Глобальные формулы
└── ClassStats/
    ├── WarriorStats.asset
    ├── MageStats.asset
    ├── ArcherStats.asset
    ├── RogueStats.asset
    └── PaladinStats.asset
```

---

## ✅ Чек-лист настройки

### Обязательно:
- [ ] Создать `StatsFormulas.asset` в Resources/
- [ ] Создать пресеты классов в Resources/ClassStats/
- [ ] Проверить что ArenaManager находит пресеты
- [ ] Протестировать прокачку характеристик
- [ ] Проверить что урон рассчитывается правильно

### Опционально:
- [ ] Настроить Node.js сервер
- [ ] Создать UI для прокачки
- [ ] Добавить визуализацию HP/MP/AP баров

---

## 🐛 Отладка

### Проблема: Характеристики не применяются
**Решение**: Проверьте что StatsFormulas.asset лежит в `Assets/Resources/` (не в подпапке!)

### Проблема: Пресет класса не загружается
**Решение**: Пресеты должны быть в `Assets/Resources/ClassStats/[ClassName]Stats.asset`

### Проблема: Урон не учитывает характеристики
**Решение**: CharacterStats должен быть на том же объекте что и PlayerAttack

---

## 📝 Примеры кода

### Получить характеристику
```csharp
CharacterStats stats = GetComponent<CharacterStats>();
int strength = stats.strength;
float maxHP = stats.MaxHealth;
```

### Добавить опыт
```csharp
LevelingSystem leveling = GetComponent<LevelingSystem>();
leveling.GainExperience(100);
```

### Прокачать характеристику
```csharp
LevelingSystem leveling = GetComponent<LevelingSystem>();
bool success = leveling.SpendStatPoint("strength"); // или "perception", "agility" и т.д.
```

### Подписаться на события
```csharp
CharacterStats stats = GetComponent<CharacterStats>();
stats.OnStatsChanged += () => {
    Debug.Log("Характеристики изменились!");
};

LevelingSystem leveling = GetComponent<LevelingSystem>();
leveling.OnLevelUp += (newLevel) => {
    Debug.Log($"Новый уровень: {newLevel}!");
};
```

---

**Система полностью настроена и готова к использованию!** 🎉
