# 🎮 Aetherion - RPG с системой SPECIAL

**Unity версия:** 6000.0.32f1 (Unity 6)
**Жанр:** 3D RPG с тактическими боями

---

## 📋 Основные системы

### ⚔️ Игровая механика
- **5 классов персонажей**: Warrior, Mage, Archer, Rogue, Paladin
- **Система SPECIAL**: Strength, Perception, Endurance, Wisdom, Intelligence, Agility, Luck
- **Action Points система**: 10 очков максимум, 4 за атаку, регенерация при остановке
- **Боевая система**: Таргетирование, атаки ближнего/дальнего боя, снаряды
- **Fog of War**: Туман войны с радиусом видимости (зависит от Perception)

### 🎯 Управление
- **WASD** - движение
- **Shift** - быстрый бег
- **ЛКМ** - атака
- **Tab** - смена цели
- **Esc** - сброс цели
- **C** - окно характеристик
- **H** - показать/скрыть HUD

---

## ⚔️ Система Скиллов (NEW!)

- **Drag & Drop выбор** - 6 скиллов на класс, выбор 3 для экипировки
- **30+ уникальных скиллов** - по 6 на каждый класс
- **Призыв существ** - Rogue призывает 3 скелетов
- **Трансформация** - Paladin превращается в медведя
- **Контроль** - стан, корни, сон, молчание, страх
- **MongoDB интеграция** - сохранение/загрузка скиллов
- **Полностью расширяемо** - легко добавлять новые скиллы

📖 **Документация**: [SKILL_SYSTEM_GUIDE.md](SKILL_SYSTEM_GUIDE.md) | [Быстрый старт](SKILLS_QUICKSTART.md)

---

## 🗂️ Структура проекта

### Сцены
```
Assets/Scenes/
├── LoginScene.unity              - Вход в игру
├── CharacterSelectionScene.unity - Выбор класса
├── ArenaScene.unity              - Игровая арена
└── GameScene.unity               - Главная сцена
```

### Ресурсы (Resources)
```
Assets/Resources/
├── StatsFormulas.asset           - Формулы SPECIAL
├── CharacterAttackSettings.asset - Настройки атаки классов
├── WeaponDatabase.asset          - База данных оружия
└── ClassStats/                   - Пресеты характеристик классов
    ├── WarriorStats.asset
    ├── MageStats.asset
    ├── ArcherStats.asset
    ├── RogueStats.asset
    └── PaladinStats.asset
```

### Префабы
```
Assets/Prefabs/
├── Weapons/                      - Оружие (мечи, посох, лук и т.д.)
├── Projectiles/                  - Снаряды (стрелы, fireball, осколки души)
└── Effects/                      - Эффекты
```

---

## 🛠️ Полезные Editor утилиты (Tools меню)

**Доступные команды:**
```
Tools/
├── Skills/
│   └── Create Skill Database     - Создать базу скиллов + 30 примеров
├── Create Weapon Database        - Создать базу оружия
├── Create Attack Settings Asset  - Создать настройки атаки
├── Update Weapon Database        - Обновить базу оружия
└── Projectiles/
    ├── Create All Projectile Prefabs
    ├── 1. Create Arrow (Archer)
    ├── 2. Create Fireball (Mage)
    └── 3. Create Soul Shards (Rogue)
```

---

## 📚 Документация

### Основная:
- **[SETUP_INSTRUCTIONS.md](SETUP_INSTRUCTIONS.md)** - Главная инструкция по настройке
- **[ACTION_POINTS_SETUP.md](ACTION_POINTS_SETUP.md)** - Настройка системы Action Points
- **[FOG_OF_WAR_SYSTEM.md](FOG_OF_WAR_SYSTEM.md)** - Система тумана войны
- **[WEAPON_GLOW_ENHANCED.md](WEAPON_GLOW_ENHANCED.md)** - Эффекты свечения оружия

### Система скиллов (NEW!):
- **[SKILL_SYSTEM_GUIDE.md](SKILL_SYSTEM_GUIDE.md)** - Полная документация системы скиллов
- **[SKILLS_QUICKSTART.md](SKILLS_QUICKSTART.md)** - Быстрый старт (3 шага)

### Служебная:
- **[CLEANUP_PLAN.md](CLEANUP_PLAN.md)** - План очистки проекта (выполнен)
- **[BUGFIXES_LOG.md](BUGFIXES_LOG.md)** - Журнал исправления багов

---

## 🚀 Быстрый старт

1. Откройте проект в Unity 6
2. Откройте сцену `Assets/Scenes/ArenaScene.unity`
3. Нажмите Play
4. Управление: WASD для движения, ЛКМ для атаки

---

## ✅ Текущее состояние проекта

**Реализовано:**
- ✅ Все 5 классов персонажей
- ✅ SPECIAL система характеристик
- ✅ Боевая система с анимациями
- ✅ **Система скиллов (30+ скиллов, Drag & Drop)**
- ✅ **Призыв существ и трансформация**
- ✅ Action Points система
- ✅ Система таргетирования
- ✅ Fog of War
- ✅ Оружие и снаряды
- ✅ UI (HUD, характеристики, AP, скиллы)
- ✅ Эффекты (свечение оружия, попадания, бафы/дебафы)
- ✅ MongoDB интеграция

**В разработке:**
- 🔄 Система прокачки (LevelingSystem)
- 🔄 Онлайн интеграция (ServerAPI)
- 🔄 Система инвентаря
- 🔄 Квесты и AI врагов

---

**Проект очищен от устаревших скриптов 10.10.2025** ✨
