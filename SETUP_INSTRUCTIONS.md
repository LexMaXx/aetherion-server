# 🎮 Инструкция по настройке SPECIAL системы

## ⚠️ ОБЯЗАТЕЛЬНО ВЫПОЛНИТЬ В UNITY:

### 1. Создать StatsFormulas (главный конфиг)
```
1. В Unity Project → правый клик
2. Create → Aetherion → Stats Formulas
3. Назвать: StatsFormulas
4. ВАЖНО: Переместить в Assets/Resources/ (создать папку Resources если её нет)
5. Настроить параметры (или оставить по умолчанию)
```

**Путь должен быть:** `Assets/Resources/StatsFormulas.asset`

---

### 2. Создать пресеты для каждого класса
```
1. В Unity Project → создать папку Assets/Resources/ClassStats/
2. Для каждого класса:
   - Create → Aetherion → Class Stats Preset
   - Назвать ТОЧНО: WarriorStats, MageStats, ArcherStats, RogueStats, PaladinStats
   - Сохранить в Assets/Resources/ClassStats/
```

**Примеры распределения (всего 15 очков):**

**WarriorStats:**
- S:4 P:2 E:3 W:1 I:1 A:2 L:2

**MageStats:**
- S:1 P:2 E:1 W:4 I:4 A:1 L:2

**ArcherStats:**
- S:2 P:4 E:2 W:1 I:2 A:3 L:1

**RogueStats:**
- S:2 P:2 E:2 W:1 I:1 A:4 L:3

**PaladinStats:**
- S:3 P:2 E:3 W:2 I:1 A:2 L:2

---

### 3. Проверить структуру папок

Должно получиться:
```
Assets/
├── Resources/
│   ├── StatsFormulas.asset          ← Главный конфиг
│   └── ClassStats/
│       ├── WarriorStats.asset       ← Пресеты классов
│       ├── MageStats.asset
│       ├── ArcherStats.asset
│       ├── RogueStats.asset
│       └── PaladinStats.asset
```

---

## 🎮 УПРАВЛЕНИЕ В ИГРЕ:

- **C** - открыть/закрыть детальную панель характеристик
- **H** - показать/скрыть HUD (всегда на экране)
- **F9** - debug информация (если есть)

---

## 🐛 ЕСЛИ НЕ РАБОТАЕТ:

### HUD не появляется:
1. Проверьте что `StatsFormulas.asset` лежит в `Assets/Resources/`
2. Проверьте консоль - должно быть: "✅ Системы персонажа найдены!"

### Характеристики не применяются:
1. Пресеты должны быть в `Assets/Resources/ClassStats/`
2. Имена файлов должны быть ТОЧНО: `WarriorStats.asset` и т.д.

### AP не отображаются у мага:
- Это уже исправлено, UI обновляется автоматически

### Проблема с Восприятием (Perception):
- На персонаже → FogOfWar компонент → галочка "Use Perception For Radius"
- Включено = радиус зависит от Perception (10-40м)
- Выключено = фиксированный радиус

---

## ✅ ПРОВЕРКА:

Запустите ArenaScene и нажмите **H**. Должен появиться HUD:
```
LVL 1  EXP: 0/100
HP: 100/120  MP: 50/65  AP: 3/5
S:4 P:2 E:3 W:1 I:1 A:2 L:2
Vision: 16m  Crit: 11.0%
```

Если видите "Loading character..." больше 1 секунды - проверьте что:
1. StatsFormulas.asset в Resources/
2. Пресеты классов в Resources/ClassStats/
3. Имена файлов правильные

---

**Всё готово!** 🎉
