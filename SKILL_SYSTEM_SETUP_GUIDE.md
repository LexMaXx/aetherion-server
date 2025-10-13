# 🎮 Полное руководство по настройке системы скиллов Aetherion

> **Дата создания:** 2025-10-10
> **Система скиллов:** Lineage 2 style PVP skills
> **Версия Unity:** 6000.0.32f1

---

## 📋 Содержание

1. [UI панель выбора скиллов](#1-ui-панель-выбора-скиллов)
2. [Префабы скелетов (Rogue)](#2-префабы-скелетов-rogue)
3. [Префаб медведя (Paladin)](#3-префаб-медведя-paladin)
4. [Иконки скиллов](#4-иконки-скиллов-30-штук)
5. [Визуальные эффекты](#5-визуальные-эффекты-частицы)
6. [Звуки скиллов](#6-звуки-каста-скиллов)

---

## 1. UI панель выбора скиллов

### 📐 Структура UI (Character Selection сцена)

```
Canvas
└── SkillSelectionPanel (GameObject)
    ├── Background (Image) - полупрозрачный фон
    │
    ├── TitleText (TextMeshPro) - "Выберите 3 навыка"
    │
    ├── SkillLibraryPanel (GameObject) - Библиотека всех 6 скиллов класса
    │   ├── LibraryTitle (TextMeshPro) - "Доступные навыки"
    │   └── LibraryGrid (GridLayoutGroup) - 2 ряда по 3 скилла
    │       ├── LibrarySlot_1 (SkillSlotUI)
    │       ├── LibrarySlot_2 (SkillSlotUI)
    │       ├── LibrarySlot_3 (SkillSlotUI)
    │       ├── LibrarySlot_4 (SkillSlotUI)
    │       ├── LibrarySlot_5 (SkillSlotUI)
    │       └── LibrarySlot_6 (SkillSlotUI)
    │
    ├── EquippedSkillsPanel (GameObject) - 3 слота для выбранных скиллов
    │   ├── EquippedTitle (TextMeshPro) - "Выбранные навыки (1, 2, 3)"
    │   └── EquippedGrid (HorizontalLayoutGroup)
    │       ├── EquippedSlot_1 (SkillSlotUI) - Кнопка "1"
    │       ├── EquippedSlot_2 (SkillSlotUI) - Кнопка "2"
    │       └── EquippedSlot_3 (SkillSlotUI) - Кнопка "3"
    │
    ├── SkillInfoPanel (GameObject) - Информация о скилле при наведении
    │   ├── SkillNameText (TextMeshPro)
    │   ├── SkillDescriptionText (TextMeshPro)
    │   ├── CooldownText (TextMeshPro) - "Перезарядка: 10с"
    │   ├── ManaCostText (TextMeshPro) - "Стоимость: 50 MP"
    │   └── RangeText (TextMeshPro) - "Дальность: 20м"
    │
    └── ConfirmButton (Button) - "Подтвердить выбор"
```

### 🎨 Пошаговая инструкция создания UI

#### Шаг 1: Создать корневую панель

1. Открой сцену **CharacterSelection**
2. ПКМ на Canvas → Create Empty → Назови `SkillSelectionPanel`
3. Установи `RectTransform`:
   - Anchor: Center-Middle
   - Width: 1200, Height: 700
   - Pos X: 400, Pos Y: 0 (справа от персонажа)

#### Шаг 2: Создать фон

1. ПКМ на `SkillSelectionPanel` → UI → Image → Назови `Background`
2. Настройки:
   - Anchor: Stretch (all sides)
   - Margins: все 0
   - Color: черный с альфа 0.8 (полупрозрачный)

#### Шаг 3: Создать заголовок

1. ПКМ на `SkillSelectionPanel` → UI → Text - TextMeshPro → Назови `TitleText`
2. Настройки:
   - Text: "ВЫБЕРИТЕ 3 НАВЫКА"
   - Font Size: 36
   - Alignment: Center, Top
   - Anchor: Top-Center
   - Pos Y: -30

#### Шаг 4: Создать библиотеку скиллов

1. ПКМ на `SkillSelectionPanel` → Create Empty → Назови `SkillLibraryPanel`
2. Установи RectTransform:
   - Anchor: Top-Left
   - Width: 550, Height: 400
   - Pos X: 50, Pos Y: -100

3. Добавь компонент `GridLayoutGroup`:
   - Cell Size: 150x200
   - Spacing: 25x25
   - Constraint: Fixed Column Count = 3

4. Создай 6 слотов:
   - ПКМ на `SkillLibraryPanel` → Create Empty → Назови `LibrarySlot_1`
   - Добавь компонент `Image` (фон слота - тёмно-серый)
   - Добавь скрипт `SkillSlotUI`
   - Настрой Inspector:
     - `isLibrarySlot = true`
     - Назначь ссылку на SkillSelectionManager (drag & drop из сцены)
   - **Повтори для LibrarySlot_2 ... LibrarySlot_6**

#### Шаг 5: Создать слоты выбранных скиллов

1. ПКМ на `SkillSelectionPanel` → Create Empty → Назови `EquippedSkillsPanel`
2. Установи RectTransform:
   - Anchor: Bottom-Center
   - Width: 600, Height: 220
   - Pos Y: 50

3. Добавь компонент `HorizontalLayoutGroup`:
   - Spacing: 30
   - Child Alignment: Middle Center

4. Создай 3 слота (аналогично библиотеке):
   - `EquippedSlot_1`, `EquippedSlot_2`, `EquippedSlot_3`
   - `isLibrarySlot = false`
   - Добавь `TextMeshPro` в каждый слот с текстом "1", "2", "3" (номера кнопок)

#### Шаг 6: Создать панель информации

1. ПКМ на `SkillSelectionPanel` → Create Empty → Назови `SkillInfoPanel`
2. Добавь фон (Image) и 5 TextMeshPro элементов для информации о скилле
3. Изначально отключи панель (SetActive = false)

#### Шаг 7: Подключить SkillSelectionManager

1. Найди объект с компонентом `CharacterSelectionManager`
2. Добавь к нему компонент **`SkillSelectionManager`**
3. Назначь ссылки в Inspector:
   - `skillDatabase` → Assets/Data/SkillDatabase.asset
   - `librarySlots` → все 6 LibrarySlot
   - `equippedSlots` → все 3 EquippedSlot
   - `skillInfoPanel` → SkillInfoPanel

### 📦 Префаб слота скилла (SkillSlotUI)

Каждый слот должен содержать:

```
SkillSlot (GameObject + SkillSlotUI.cs)
├── Background (Image) - рамка слота
├── Icon (Image) - иконка скилла
├── CooldownOverlay (Image) - затемнение при КД
├── KeyBindText (TextMeshPro) - "1", "2", "3"
└── EmptyText (TextMeshPro) - "Пусто"
```

---

## 2. Префабы скелетов (Rogue)

### 💀 Характеристики скелета

**Требования к скелету:**
- Простая гуманоидная модель (можно использовать Mixamo)
- Анимации: Idle, Walk, Attack
- Размер: ~80% от размера игрока
- Время жизни: 30 секунд
- Урон: 10-15 (масштабируется от Intelligence Rogue)

### 🔧 Создание префаба скелета

#### Вариант 1: Использовать Mixamo модель

1. Зайди на [Mixamo](https://www.mixamo.com)
2. Скачай модель скелета (например "Skeleton" или "Zombie")
3. Скачай анимации:
   - Idle
   - Walk
   - Sword And Shield Slash

4. Импортируй в Unity:
   - Создай папку `Assets/Models/Skeleton/`
   - Перетащи .fbx файлы

5. Настрой модель:
   - Rig Type: Humanoid
   - Avatar Definition: Create From This Model

#### Вариант 2: Создать простой placeholder

1. Создай новый GameObject → назови `SkeletonPrefab`
2. Добавь простую капсулу (Capsule):
   - Scale: 0.3, 1.5, 0.3
   - Материал: тёмно-серый

3. Добавь компоненты:
   ```
   GameObject: SkeletonPrefab
   ├── Capsule (визуал)
   ├── NavMeshAgent (навигация)
   ├── CapsuleCollider (коллизия)
   └── SummonedCreature.cs (AI скрипт)
   ```

4. Настрой `NavMeshAgent`:
   - Speed: 3.5
   - Angular Speed: 120
   - Acceleration: 8
   - Stopping Distance: 2

5. Настрой `SummonedCreature` (Inspector):
   - Damage: 12
   - Attack Speed: 2 (атака каждые 2 сек)
   - Aggro Range: 15
   - Follow Distance: 5

6. Сохрани как Prefab:
   - Перетащи в `Assets/Prefabs/Skills/`
   - Назови `SkeletonMinion.prefab`

### 🎯 Подключение к скиллу Rogue

1. Открой `Assets/Data/SkillDatabase.asset`
2. Найди скилл "Summon Skeletons" (Rogue)
3. Назначь:
   - `summonPrefab` → SkeletonMinion.prefab
   - `summonCount` = 3
   - `summonDuration` = 30

---

## 3. Префаб медведя (Paladin)

### 🐻 Характеристики медведя

**Требования:**
- Модель медведя (можно использовать Unity Asset Store)
- Анимации не обязательны (статичная модель)
- Размер: 150% от игрока
- Время трансформации: 30 секунд
- Бонусы: +50% HP, +30% физический урон

### 🔧 Создание префаба медведя

#### Вариант 1: Free Asset Store модель

1. Открой **Asset Store** в Unity
2. Найди бесплатную модель медведя:
   - "Bear" by Dexsoft Games
   - "Low Poly Animals" (есть медведь)

3. Импортируй модель
4. Настрой Scale чтобы был крупнее игрока (~1.5x)

#### Вариант 2: Простой placeholder

1. Создай GameObject → назови `BearTransformPrefab`
2. Добавь несколько кубов/капсул чтобы сделать форму медведя:
   ```
   BearTransformPrefab
   ├── Body (Cube) - коричневый, Scale: 2, 1.5, 1
   ├── Head (Sphere) - коричневый, Scale: 0.8
   ├── Leg_FL (Capsule)
   ├── Leg_FR (Capsule)
   ├── Leg_BL (Capsule)
   └── Leg_BR (Capsule)
   ```

3. Примени коричневый материал ко всем частям

4. Сохрани как Prefab:
   - `Assets/Prefabs/Skills/BearTransformation.prefab`

### 🎯 Подключение к скиллу Paladin

1. Открой `Assets/Data/SkillDatabase.asset`
2. Найди скилл "Bear Form" (Paladin)
3. Назначь:
   - `transformationModel` → BearTransformation.prefab
   - `transformationDuration` = 30
   - `hpBonusPercent` = 50
   - `physicalDamageBonusPercent` = 30

---

## 4. Иконки скиллов (30 штук)

### 🎨 Источники иконок

#### Вариант 1: Бесплатные паки (рекомендуется)

**Game-icons.net** (лучший источник):
1. Зайди на https://game-icons.net
2. Найди иконки для каждого скилла по ключевым словам
3. Скачай в формате PNG (128x128 или 256x256)
4. Цвет: белый на прозрачном фоне

**Рекомендуемые иконки по классам:**

**Warrior (Воин):**
1. **Shield Bash** → "shield-bash"
2. **Whirlwind** → "tornado"
3. **Battle Cry** → "sonic-shout"
4. **Charge** → "running-ninja"
5. **Execute** → "sword-clash"
6. **Defensive Stance** → "shield"

**Mage (Маг):**
1. **Fireball** → "fireball"
2. **Ice Lance** → "ice-spear"
3. **Lightning Bolt** → "lightning-bolt"
4. **Mana Shield** → "magic-shield"
5. **Teleport** → "teleport"
6. **Meteor** → "meteor-impact"

**Archer (Лучник):**
1. **Power Shot** → "arrow-flights"
2. **Multi Shot** → "triple-scratches"
3. **Poison Arrow** → "poison-bottle"
4. **Trap** → "bear-trap"
5. **Eagle Eye** → "eagle-emblem"
6. **Rain of Arrows** → "arrow-cluster"

**Rogue (Разбойник):**
1. **Backstab** → "backstab"
2. **Summon Skeletons** → "skull-staff"
3. **Smoke Bomb** → "smoke-bomb"
4. **Poison Dagger** → "dripping-knife"
5. **Shadow Step** → "shadow-follower"
6. **Critical Strike** → "bleeding-eye"

**Paladin (Паладин):**
1. **Holy Light** → "healing"
2. **Bear Form** → "bear-head"
3. **Divine Shield** → "holy-shield"
4. **Resurrection** → "resurrection"
5. **Hammer of Justice** → "hammer-drop"
6. **Blessing** → "holy-symbol"

#### Вариант 2: AI генерация (Midjourney/DALL-E)

Промпт для генерации:
```
"skill icon, [название скилла], fantasy RPG game, simple design,
white icon on black background, 256x256px, game UI, isometric view"
```

### 📁 Организация иконок

1. Создай папку `Assets/UI/Icons/Skills/`
2. Подпапки по классам:
   ```
   Skills/
   ├── Warrior/
   │   ├── ShieldBash_Icon.png
   │   ├── Whirlwind_Icon.png
   │   └── ...
   ├── Mage/
   ├── Archer/
   ├── Rogue/
   └── Paladin/
   ```

3. Настрой Import Settings для каждой иконки:
   - Texture Type: **Sprite (2D and UI)**
   - Max Size: 256
   - Compression: None (или High Quality)

4. Назначь иконки в SkillDatabase:
   - Открой каждый SkillData
   - Перетащи соответствующую иконку в поле `skillIcon`

---

## 5. Визуальные эффекты (частицы)

### ✨ Типы эффектов для скиллов

| Тип скилла | Эффекты |
|------------|---------|
| **Урон огнём** | Огненный шар, взрыв пламени |
| **Урон льдом** | Ледяные осколки, замерзание |
| **Урон молнией** | Молния, электрические искры |
| **Лечение** | Зелёное свечение, плюсики |
| **Бафф** | Золотое свечение вокруг персонажа |
| **Дебафф** | Фиолетовое/красное свечение |
| **Призыв** | Круг призыва, дым |
| **Трансформация** | Вспышка света, дым |
| **Телепорт** | Частицы появления/исчезновения |

### 🎨 Создание базовых эффектов

#### Эффект каста (общий для всех скиллов)

1. GameObject → Effects → Particle System → назови `CastEffect`
2. Настрой Particle System:
   ```
   Main:
   - Duration: 1
   - Start Lifetime: 0.5
   - Start Speed: 2
   - Start Size: 0.3
   - Start Color: голубой → белый (gradient)

   Emission:
   - Rate over Time: 50

   Shape:
   - Shape: Sphere
   - Radius: 1
   ```
3. Сохрани как Prefab: `Assets/Prefabs/Effects/CastEffect.prefab`

#### Эффект попадания (HitEffect)

1. Particle System → назови `HitEffect`
2. Настройки:
   ```
   Main:
   - Duration: 0.3
   - Start Lifetime: 0.2
   - Start Speed: 5
   - Start Size: 0.5
   - Start Color: оранжевый

   Emission:
   - Burst: 20 частиц

   Shape:
   - Shape: Cone
   - Angle: 45
   ```
3. Сохрани: `Assets/Prefabs/Effects/HitEffect.prefab`

#### Эффект огненного шара (Fireball)

1. Создай 2 Particle Systems:
   - `FireCore` (ядро) - оранжевый, размер 0.8
   - `FireTrail` (хвост) - красно-жёлтый, размер 0.3

2. Сохрани: `Assets/Prefabs/Effects/FireballEffect.prefab`

### 📦 Asset Store пакеты (бесплатные)

**Рекомендуемые паки:**

1. **Epic Toon FX** (бесплатная версия)
   - Много готовых мультяшных эффектов
   - https://assetstore.unity.com/packages/vfx/particles/epic-toon-fx-free-57772

2. **Cartoon FX Remaster Free**
   - Стильные эффекты для всех типов скиллов
   - https://assetstore.unity.com/packages/vfx/particles/cartoon-fx-remaster-free-109565

3. **Particle Effect Pack**
   - Базовые частицы для кастомизации

### 🎯 Подключение эффектов к скиллам

1. Открой `Assets/Data/SkillDatabase.asset`
2. Для каждого скилла назначь:
   - `castEffect` → эффект каста (появляется на персонаже)
   - `projectilePrefab` → летящий снаряд (если есть)
   - `hitEffect` → эффект попадания (на цели)
   - `aoeEffect` → область действия (если AOE)

---

## 6. Звуки каста скиллов

### 🔊 Источники звуков

#### Вариант 1: Freesound.org (бесплатно)

1. Зайди на https://freesound.org
2. Найди звуки по ключевым словам:
   - "magic cast"
   - "fireball"
   - "sword slash"
   - "heal"
   - "lightning"

3. Фильтры:
   - License: CC0 (Public Domain)
   - Format: WAV или MP3

#### Вариант 2: Asset Store

**Рекомендуемые паки:**

1. **Universal Sound FX** (бесплатный)
   - Много звуков для RPG
   - https://assetstore.unity.com/packages/audio/sound-fx/universal-sound-fx-17256

2. **Magic Sound Effects** (бесплатный)
   - Звуки магии и заклинаний

### 📁 Организация звуков

```
Assets/Audio/Skills/
├── Cast/
│   ├── Cast_Generic.wav
│   ├── Cast_Fire.wav
│   ├── Cast_Ice.wav
│   └── Cast_Holy.wav
├── Impact/
│   ├── Impact_Fire.wav
│   ├── Impact_Ice.wav
│   └── Impact_Physical.wav
└── Ambient/
    ├── Buff_Apply.wav
    └── Debuff_Apply.wav
```

### 🎯 Подключение к скиллам

1. Импортируй аудио в Unity
2. Настрой Import Settings:
   - Load Type: Decompress On Load (для коротких звуков)
   - Compression Format: Vorbis

3. Назначь в SkillDatabase:
   - `castSound` → звук каста
   - `hitSound` → звук попадания

### 🎵 AudioSource на скиллах

Создай AudioSource на игроке:
```csharp
// В SkillManager.cs уже есть логика:
if (skill.castSound != null)
{
    AudioSource.PlayClipAtPoint(skill.castSound, transform.position);
}
```

---

## 📝 Чеклист завершения

### UI панель
- [ ] Создана корневая панель SkillSelectionPanel
- [ ] 6 слотов библиотеки с компонентом SkillSlotUI
- [ ] 3 слота экипированных скиллов
- [ ] Панель информации о скилле
- [ ] SkillSelectionManager подключен и настроен

### Префабы
- [ ] Создан префаб скелета (SkeletonMinion.prefab)
- [ ] NavMeshAgent настроен на скелете
- [ ] SummonedCreature.cs добавлен
- [ ] Создан префаб медведя (BearTransformation.prefab)
- [ ] Префабы назначены в SkillDatabase

### Иконки
- [ ] 6 иконок для Warrior
- [ ] 6 иконок для Mage
- [ ] 6 иконок для Archer
- [ ] 6 иконок для Rogue
- [ ] 6 иконок для Paladin
- [ ] Все иконки назначены в SkillDatabase

### Эффекты
- [ ] CastEffect (общий)
- [ ] HitEffect (общий)
- [ ] FireballEffect
- [ ] IceEffect
- [ ] LightningEffect
- [ ] HealEffect
- [ ] BuffEffect
- [ ] Эффекты назначены в SkillDatabase

### Звуки
- [ ] Звуки каста (минимум 5 разных)
- [ ] Звуки попадания (минимум 3 разных)
- [ ] Звуки баффов
- [ ] AudioSource настроен на игроке
- [ ] Звуки назначены в SkillDatabase

---

## 🚀 Быстрый старт (Minimal Setup)

Если нужно быстро протестировать систему:

### 1. Минимальный UI (5 минут)
- Создай простую панель с 6 кнопками (библиотека)
- Создай 3 кнопки (экипированные)
- Используй стандартные UI Image без иконок

### 2. Placeholder префабы (2 минуты)
- Скелет = простая капсула серого цвета
- Медведь = куб коричневого цвета, Scale (2, 1.5, 1)

### 3. Без иконок (0 минут)
- Оставь поле `skillIcon` пустым
- UI слоты будут просто показывать имя скилла

### 4. Без эффектов (0 минут)
- Оставь `castEffect`, `hitEffect` пустыми
- Скиллы будут работать без визуала

### 5. Без звуков (0 минут)
- Оставь `castSound`, `hitSound` пустыми
- Скиллы будут работать без звука

**Итого: 7 минут для минимального прототипа!**

---

## 📞 Поддержка

Если возникнут вопросы:
1. Проверь логи Unity Console (все скрипты логируют действия)
2. Используй ContextMenu команды для тестирования
3. Проверь что все ссылки в Inspector назначены

**Удачи в разработке! 🎮**
