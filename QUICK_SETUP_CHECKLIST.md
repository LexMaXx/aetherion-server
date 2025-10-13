# ⚡ Быстрая настройка системы скиллов - Чеклист

## 🎯 Задача 1: UI панель (15-20 минут)

### Создать структуру
```
Canvas/SkillSelectionPanel/
├── SkillLibraryPanel (6 слотов в GridLayout)
├── EquippedSkillsPanel (3 слота в HorizontalLayout)
└── ConfirmButton
```

### Пошагово:
1. ✅ Открыть сцену **CharacterSelection**
2. ✅ Создать GameObject `SkillSelectionPanel` (Width: 1200, Height: 700)
3. ✅ Добавить фон (Image, черный 80% прозрачности)
4. ✅ Создать `SkillLibraryPanel` с GridLayoutGroup (3 колонки, 2 ряда)
5. ✅ Создать 6 слотов: LibrarySlot_1 ... LibrarySlot_6
   - Каждый слот: Image + SkillSlotUI.cs
   - `isLibrarySlot = true`
6. ✅ Создать `EquippedSkillsPanel` с HorizontalLayoutGroup
7. ✅ Создать 3 слота: EquippedSlot_1, EquippedSlot_2, EquippedSlot_3
   - Каждый слот: Image + SkillSlotUI.cs
   - `isLibrarySlot = false`
8. ✅ Добавить компонент **SkillSelectionManager** на CharacterSelectionManager
9. ✅ Назначить ссылки в Inspector:
   - skillDatabase → Assets/Data/SkillDatabase.asset
   - librarySlots → все 6 LibrarySlot
   - equippedSlots → все 3 EquippedSlot

---

## 🦴 Задача 2: Скелеты (10 минут)

### Вариант A: Mixamo (качественно)
1. ✅ Зайти на mixamo.com
2. ✅ Скачать модель скелета + анимации (Idle, Walk, Attack)
3. ✅ Импортировать в Assets/Models/Skeleton/
4. ✅ Настроить Rig: Humanoid

### Вариант B: Placeholder (быстро)
1. ✅ Создать GameObject → Capsule (серый, Scale: 0.3, 1.5, 0.3)
2. ✅ Добавить компоненты:
   - NavMeshAgent (Speed: 3.5)
   - CapsuleCollider
   - SummonedCreature.cs
3. ✅ Сохранить как `Assets/Prefabs/Skills/SkeletonMinion.prefab`
4. ✅ Открыть SkillDatabase → Rogue → "Summon Skeletons"
5. ✅ Назначить summonPrefab = SkeletonMinion.prefab

---

## 🐻 Задача 3: Медведь (10 минут)

### Вариант A: Asset Store (красиво)
1. ✅ Asset Store → поиск "Bear" (бесплатные)
2. ✅ Импортировать модель
3. ✅ Настроить Scale (~1.5x больше игрока)

### Вариант B: Placeholder (быстро)
1. ✅ Создать GameObject → несколько Cube/Capsule
2. ✅ Создать форму медведя (тело, голова, лапы)
3. ✅ Материал: коричневый
4. ✅ Сохранить как `Assets/Prefabs/Skills/BearTransformation.prefab`
5. ✅ Открыть SkillDatabase → Paladin → "Bear Form"
6. ✅ Назначить transformationModel = BearTransformation.prefab

---

## 🎨 Задача 4: Иконки (30-60 минут)

### Где взять:
- **game-icons.net** (лучший вариант, бесплатно)
- **flaticon.com** (бесплатно с атрибуцией)
- **Midjourney/DALL-E** (AI генерация)

### Список иконок по классам:

#### Warrior (6 иконок)
- [ ] Shield Bash
- [ ] Whirlwind
- [ ] Battle Cry
- [ ] Charge
- [ ] Execute
- [ ] Defensive Stance

#### Mage (6 иконок)
- [ ] Fireball
- [ ] Ice Lance
- [ ] Lightning Bolt
- [ ] Mana Shield
- [ ] Teleport
- [ ] Meteor

#### Archer (6 иконок)
- [ ] Power Shot
- [ ] Multi Shot
- [ ] Poison Arrow
- [ ] Trap
- [ ] Eagle Eye
- [ ] Rain of Arrows

#### Rogue (6 иконок)
- [ ] Backstab
- [ ] Summon Skeletons
- [ ] Smoke Bomb
- [ ] Poison Dagger
- [ ] Shadow Step
- [ ] Critical Strike

#### Paladin (6 иконок)
- [ ] Holy Light
- [ ] Bear Form
- [ ] Divine Shield
- [ ] Resurrection
- [ ] Hammer of Justice
- [ ] Blessing

### Настройка:
1. ✅ Создать папку `Assets/UI/Icons/Skills/`
2. ✅ Импортировать все 30 иконок
3. ✅ Texture Type: **Sprite (2D and UI)**
4. ✅ Max Size: 256
5. ✅ Открыть SkillDatabase
6. ✅ Назначить каждую иконку в соответствующий SkillData

---

## ✨ Задача 5: Эффекты частицы (20-40 минут)

### Вариант A: Asset Store (быстро)
**Рекомендуемые паки:**
- [ ] Epic Toon FX (Free)
- [ ] Cartoon FX Remaster Free
- [ ] Particle Effect Pack

1. ✅ Скачать и импортировать
2. ✅ Назначить эффекты в SkillDatabase:
   - castEffect
   - hitEffect
   - projectilePrefab (если есть)

### Вариант B: Создать свои (дольше)
**Минимальный набор:**
- [ ] CastEffect (общий эффект каста)
- [ ] HitEffect (общий эффект попадания)
- [ ] FireballEffect (огонь)
- [ ] IceEffect (лёд)
- [ ] HealEffect (лечение)

### Создание базового эффекта:
1. ✅ GameObject → Effects → Particle System
2. ✅ Настроить параметры (см. полный гайд)
3. ✅ Сохранить как Prefab в `Assets/Prefabs/Effects/`
4. ✅ Назначить в SkillDatabase

---

## 🔊 Задача 6: Звуки (15-30 минут)

### Где взять:
- **freesound.org** (бесплатно, CC0)
- **Asset Store** (пакеты типа "Universal Sound FX")

### Типы звуков:
- [ ] Звуки каста (5 разных: магия, физика, лечение)
- [ ] Звуки попадания (3 разных: урон, лечение, бафф)
- [ ] Звуки баффов/дебаффов

### Настройка:
1. ✅ Создать папку `Assets/Audio/Skills/`
2. ✅ Импортировать звуки
3. ✅ Load Type: **Decompress On Load**
4. ✅ Compression: **Vorbis**
5. ✅ Назначить в SkillDatabase:
   - castSound
   - hitSound

---

## 🚀 Минимальная версия (7 минут)

Если нужно **срочно протестировать систему**:

### 1. UI без иконок (5 мин)
- Создай панель с 9 пустыми кнопками (6+3)
- Используй просто серые Image без иконок
- Текст скилла вместо иконки

### 2. Placeholder префабы (2 мин)
- Скелет = серая капсула
- Медведь = коричневый куб

### 3. Без эффектов и звуков (0 мин)
- Оставь все поля пустыми
- Система работает без визуала

---

## ✅ Финальная проверка

После завершения всех задач:

### Тест в Unity Editor:
1. ✅ Открой CharacterSelection сцену
2. ✅ Нажми Play
3. ✅ Выбери класс персонажа
4. ✅ Проверь что появились 6 скиллов в библиотеке
5. ✅ Drag & Drop 3 скилла в слоты
6. ✅ Нажми "Подтвердить"
7. ✅ Войди в Arena сцену
8. ✅ Нажми 1, 2, 3 для использования скиллов

### Проверка логов:
- Не должно быть ошибок в Console
- Логи должны показывать:
  ```
  [SkillSelectionManager] Загружено 6 скиллов для Warrior
  [SkillManager] ✅ Скилл Shield Bash использован!
  [SummonedCreature] Атакую цель: Enemy
  ```

---

## 📚 Дополнительно

- Полный гайд: `SKILL_SYSTEM_SETUP_GUIDE.md`
- Документация API: `SKILL_SYSTEM_GUIDE.md`
- Быстрый старт: `SKILLS_QUICKSTART.md`

**Время на полную настройку: 2-3 часа**
**Время на минимальный прототип: 7 минут**

Удачи! 🎮
