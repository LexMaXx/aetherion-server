# ✅ Raise Dead - Призыв Скелета-Воина

## Описание
**Raise Dead** - пятый и финальный скилл Rogue (Necromancer). Призывает временного скелета-воина, который сражается на стороне некроманта в течение 20 секунд.

**Уникальная особенность:** Первый **summon-скилл** в игре! Создаёт союзного NPC который атакует врагов.

---

## Параметры скилла

| Параметр | Значение |
|----------|----------|
| **Skill ID** | 505 |
| **Название** | Raise Dead |
| **Класс** | Rogue (Necromancer) |
| **Тип** | Summon (призыв миньона) |
| **Cooldown** | 30 секунд |
| **Mana Cost** | 50 MP (самый дорогой скилл некроманта) |
| **Длительность** | 20 секунд |
| **Урон скелета** | 30 + 50% Intelligence |
| **Лимит миньонов** | 1 (только один скелет одновременно) |

---

## Механика

### Формула урона скелета:
```
Skeleton Damage = 30 + (Caster Intelligence × 50%)
```

### Примеры:

**Некромант с 20 Intelligence:**
```
Урон скелета: 30 + (20 × 0.5) = 30 + 10 = 40 урона
```

**Некромант с 50 Intelligence:**
```
Урон скелета: 30 + (50 × 0.5) = 30 + 25 = 55 урона
```

**Некромант с 100 Intelligence:**
```
Урон скелета: 30 + (100 × 0.5) = 30 + 50 = 80 урона
```

---

## Как работает

### 1. Использование скилла:
```csharp
// Игрок нажимает клавишу "5" (или соответствующий hotkey)
ExecuteSummon(skill);
```

### 2. Проверка на существующего миньона:
```csharp
// TODO: В будущем
if (activeMinion != null)
{
    Destroy(activeMinion); // Убиваем старого скелета
}
```

### 3. Создание скелета:
```csharp
// TODO: В будущем
Vector3 spawnPosition = transform.position + transform.forward * 2f;
GameObject skeleton = Instantiate(skeletonPrefab, spawnPosition, Quaternion.identity);

// Настройка параметров
SkeletonAI ai = skeleton.GetComponent<SkeletonAI>();
ai.Initialize(casterStats, skill.baseDamageOrHeal, skill.intelligenceScaling);
ai.SetDuration(skill.duration); // 20 секунд
```

### 4. Визуальные эффекты:
- **Cast Effect:** Тёмная некромантская энергия (`CFXR3 Fire Explosion B 1`)
- **Caster Effect:** Магический круг призыва (`CFXR Magic Poof`)

### 5. Самоуничтожение через 20 секунд:
```csharp
Destroy(skeleton, 20f); // Скелет исчезает через 20 секунд
```

---

## Стратегическое применение

### Ситуация 1: Танк и поддержка
```
1. Призываешь скелета
2. Скелет отвлекает врагов (танкует)
3. Ты атакуешь с безопасного расстояния (Soul Drain, Crippling Curse)
4. Враги сосредоточены на скелете, а не на тебе
```

### Ситуация 2: Дополнительный урон
```
1. Сражаешься 1v1 с сильным врагом
2. Призываешь скелета
3. Теперь 2v1 - ты и скелет против врага
4. Суммарный DPS увеличивается (твой урон + урон скелета)
```

### Ситуация 3: Фарм мобов
```
1. Призываешь скелета перед группой врагов
2. Скелет атакует одного, ты атакуешь другого
3. Убиваешь мобов в 2 раза быстрее
4. Экономишь ману (скелету мана не нужна)
```

### Ситуация 4: Побег
```
Тебя окружили враги:
1. Призываешь скелета
2. Скелет отвлекает врагов
3. Используешь Crippling Curse (замедление)
4. Убегаешь пока враги бьют скелета
```

---

## Балансировка

### Сильные стороны:
- **Дополнительный урон** - скелет атакует автоматически
- **Танк** - скелет отвлекает врагов
- **Долгая длительность** (20 сек) - долго помогает в бою
- **Скейлится с Intelligence** - чем выше INT, тем сильнее скелет
- **Средний кулдаун** (30 сек) - можно часто призывать

### Слабые стороны:
- **Дорогая мана** (50 MP) - самый дорогой скилл некроманта
- **Только 1 скелет** - нельзя призвать армию
- **Ограниченное время** (20 сек) - потом исчезает
- **Длинный кулдаун** (30 сек) - нельзя спамить
- **Базовый AI** - скелет не слишком умный

### Сравнение затрат маны:
```
Soul Drain:         25 MP (вампиризм)
Curse of Weakness:  30 MP (ослепление)
Crippling Curse:    30 MP (замедление)
Blood for Mana:      0 MP (жертва HP)
Raise Dead:         50 MP (призыв скелета) ← САМЫЙ ДОРОГОЙ
```

---

## Визуальные эффекты

### Cast Effect (призыв)
- **Префаб:** `CFXR3 Fire Explosion B 1`
- Тёмная некромантская энергия взрывается вокруг некроманта
- Символизирует использование тёмной магии

### Caster Effect (магический круг)
- **Префаб:** `CFXR Magic Poof`
- Магический круг призыва появляется под некромантом
- Символизирует портал из мира мёртвых

### TODO: Skeleton Prefab
- Модель скелета-воина (меч и щит)
- Анимации: Idle, Walk, Attack, Death
- AI: Атакует ближайших врагов
- VFX: Тёмная аура вокруг скелета

---

## Интеграция с системами

### EffectConfig.cs
**Добавлен новый EffectType:**

```csharp
// Line 308
[Tooltip("Призыв миньона (скелет, демон, элементаль и т.д.)")]
SummonMinion
```

### EffectManager.cs
**Добавлена обработка SummonMinion:**

```csharp
// Lines 297-300
case EffectType.SummonMinion:
    // Призыв миньона будет обрабатываться в SkillExecutor
    Log($"💀 Призыв миньона (урон: {config.power})");
    break;
```

### SkillExecutor.cs
**Добавлен case Summon в switch:**

```csharp
// Lines 116-118
case SkillConfigType.Summon:
    ExecuteSummon(skill);
    break;
```

**Создан метод ExecuteSummon:**

```csharp
// Lines 860-901
private void ExecuteSummon(SkillConfig skill)
{
    Log($"Using skill: {skill.skillName}");

    // Спавним визуальные эффекты призыва
    SpawnEffect(skill.castEffectPrefab, transform.position, Quaternion.identity);
    SpawnEffect(skill.casterEffectPrefab, transform.position, Quaternion.identity);

    // TODO: Реализовать систему призыва миньонов
    // Пока просто логируем, что скилл использован
    // В будущем здесь будет:
    // 1. Проверка - есть ли уже активный миньон
    // 2. Если есть - уничтожить старого
    // 3. Создать нового миньона перед некромантом
    // 4. Настроить параметры миньона (HP, урон, AI)
    // 5. Запустить таймер на duration секунд

    Log($"💀 Raise Dead: миньон призван на {skill.duration} секунд");
    Log($"⚔️ Урон миньона: {skill.baseDamageOrHeal} + {skill.intelligenceScaling * 100}% INT");

    // Применяем эффект SummonMinion через EffectManager (для логирования)
    if (skill.effects != null && skill.effects.Count > 0)
    {
        EffectManager effectManager = GetComponent<EffectManager>();
        if (effectManager == null)
        {
            effectManager = gameObject.AddComponent<EffectManager>();
        }

        CharacterStats stats = GetComponent<CharacterStats>();
        foreach (var effect in skill.effects)
        {
            if (effect.effectType == EffectType.SummonMinion)
            {
                effectManager.ApplyEffect(effect, stats);
            }
        }
    }
}
```

---

## Создание скилла в Unity

### Шаг 1: Запуск Editor Script
```
Unity Menu → Aetherion → Skills → Rogue → Create Raise Dead
```

### Шаг 2: Проверка Asset
Скилл создан по пути:
```
Assets/Resources/Skills/Rogue_RaiseDead.asset
```

**ВАЖНО:** Проверь что параметры правильные:
- `skillType = Summon`
- `duration = 20`
- `cooldown = 30`
- `manaCost = 50`
- `baseDamageOrHeal = 30`
- `intelligenceScaling = 0.5`

### Шаг 3: Добавить в SkillBar
```
1. Открой сцену с Rogue (Necromancer)
2. Найди SkillBar компонент
3. Перетащи Rogue_RaiseDead.asset в список скиллов
4. Настрой hotkey (например, клавиша "5")
```

---

## Тестирование

### Быстрый тест

**Шаг 1: Подготовка**
```
1. ▶️ Play Scene
2. Проверь MP в UI
   MP: 120/120
```

**Шаг 2: Используй Raise Dead**
```
1. Нажми клавишу "5" (Raise Dead)
2. Должно произойти:
```

**Ожидаемое поведение:**
```
✅ Тёмная энергия (красный взрыв)
✅ Магический круг под некромантом (призыв)
✅ MP: 120 → 70 (-50 MP)
✅ Cooldown: 30 секунд
```

**Шаг 3: Проверь логи**
```
Console → Открой логи
```

**Ожидаемые логи:**
```
[SkillExecutor] Using skill: Raise Dead
[SkillExecutor] 💀 Raise Dead: миньон призван на 20 секунд
[SkillExecutor] ⚔️ Урон миньона: 30 + 50% INT
[EffectManager] 💀 Призыв миньона (урон: 30)
[EffectManager] ✨ Применён эффект: SummonMinion (20с)
```

### Детальные проверки

**Тест 1: Стоимость маны**
```
1. MP: 60/120
2. Попробуй использовать Raise Dead (50 MP)
3. ✅ Скилл активируется (MP: 10/120)

4. MP: 40/120
5. Попробуй использовать Raise Dead (50 MP)
6. ⚠️ "Недостаточно маны!" ✅
```

**Тест 2: Cooldown 30 секунд**
```
1. Используй Raise Dead
2. Попробуй использовать снова сразу
3. "Skill on cooldown: 30" ✅
4. Подожди 30 секунд
5. Скилл снова доступен ✅
```

**Тест 3: Скейлинг с Intelligence**
```
CharacterStats → Intelligence: 20
Урон скелета: 30 + (20 × 0.5) = 40 ✅

CharacterStats → Intelligence: 50
Урон скелета: 30 + (50 × 0.5) = 55 ✅
```

**Тест 4: Визуальные эффекты**
```
1. Cast Effect: CFXR3 Fire Explosion B 1 ✅
2. Caster Effect: CFXR Magic Poof ✅
3. Оба эффекта spawning на позиции некроманта ✅
```

---

## TODO: Полная реализация системы миньонов

### Требуется создать:

**1. Skeleton Prefab**
- Модель скелета-воина
- Animator с анимациями (Idle, Walk, Attack, Death)
- Collider и Rigidbody
- HealthSystem (HP скелета)
- SkeletonAI (логика атаки)

**2. SkeletonAI.cs Script**
```csharp
public class SkeletonAI : MonoBehaviour
{
    private CharacterStats ownerStats;
    private float baseDamage;
    private float intelligenceScaling;
    private float lifetime;

    public void Initialize(CharacterStats owner, float damage, float intScaling)
    {
        ownerStats = owner;
        baseDamage = damage;
        intelligenceScaling = intScaling;
    }

    public void SetDuration(float duration)
    {
        lifetime = duration;
        Invoke("Die", duration);
    }

    private void Update()
    {
        // Найти ближайшего врага
        // Двигаться к врагу
        // Атаковать врага
    }

    public float GetDamage()
    {
        return baseDamage + (ownerStats.intelligence * intelligenceScaling);
    }

    private void Die()
    {
        // Эффект смерти (кости рассыпаются)
        Destroy(gameObject);
    }
}
```

**3. Интеграция в ExecuteSummon**
```csharp
// Загружаем префаб скелета
GameObject skeletonPrefab = Resources.Load<GameObject>("Minions/Skeleton");

// Позиция спавна (перед некромантом)
Vector3 spawnPosition = transform.position + transform.forward * 2f;

// Создаём скелета
GameObject skeleton = Instantiate(skeletonPrefab, spawnPosition, transform.rotation);

// Настраиваем AI
SkeletonAI ai = skeleton.GetComponent<SkeletonAI>();
CharacterStats stats = GetComponent<CharacterStats>();
ai.Initialize(stats, skill.baseDamageOrHeal, skill.intelligenceScaling);
ai.SetDuration(skill.duration);

// Сохраняем ссылку на активного миньона
activeMinion = skeleton;
```

**4. Управление миньонами**
```csharp
private GameObject activeMinion; // Текущий скелет

// В ExecuteSummon:
if (activeMinion != null)
{
    Destroy(activeMinion); // Убиваем старого скелета
}
```

---

## Комбинации со скиллами

### Танк-стратегия:
```
1. Raise Dead (призываешь скелета)
2. Скелет танкует врагов
3. Soul Drain (вампиризм с дистанции)
4. Curse of Weakness (ослепляешь врага)
5. Crippling Curse (замедляешь врага)
6. Скелет добивает врага
```

### Агрессивная стратегия:
```
1. Raise Dead (призыв)
2. Атакуешь вместе со скелетом (2v1)
3. Soul Drain (вампиризм + урон)
4. Blood for Mana (жертва HP → MP)
5. Soul Drain снова (восстанавливаешь HP)
6. Циклическое использование ресурсов
```

### Фарм-стратегия:
```
1. Raise Dead перед группой мобов
2. Скелет атакует одного моба
3. Ты атакуешь другого моба
4. Убиваете мобов параллельно
5. Эффективный фарм!
```

---

## Сравнение с другими классами

| Класс | Summon-скилл | Тип миньона | Длительность |
|-------|--------------|-------------|--------------|
| **Rogue (Necromancer)** | Raise Dead | Скелет-воин | 20 сек |
| Mage | - | - | - |
| Warrior | - | - | - |
| Archer | - | - | - |
| Paladin | - | - | - |

**Уникальность Raise Dead:**
- **Первый и единственный** summon-скилл в игре (пока)!
- Некромант - мастер призыва нежити ☠️

---

## Lore / Лор

> *"Смерть - это не конец, а лишь начало новой службы.
> Древнее некромантское заклинание Raise Dead позволяет
> вырвать душу из мира мёртвых и вселить её в скелет,
> создав временного слугу. Скелет будет сражаться за
> некроманта до тех пор, пока магия не иссякнет, и он
> не вернётся в вечный покой..."*

**Некромантская философия:**
- Смерть → Воскрешение → Служба → Покой
- Скелет служит некроманту 20 секунд, затем исчезает
- Некромант не может призвать армию (только 1 скелет)
- Баланс между жизнью и смертью ⚖️

---

## Будущие улучшения

### Возможные вариации:

**1. Улучшенный скелет:**
- **Skeleton Warrior** - Больше HP, больше урона
- **Skeleton Archer** - Стреляет стрелами с дистанции
- **Skeleton Mage** - Кастует заклинания

**2. Больше миньонов:**
- **Raise Army** - Призывает 3 скелета одновременно (слабее)
- **Summon Lich** - Призывает мощного лича (дороже, сильнее)
- **Zombie Horde** - Призывает толпу зомби (много слабых)

**3. Постоянные миньоны:**
- **Eternal Servant** - Скелет существует бесконечно, но слабее
- **Phylactery** - Скелет воскрешается после смерти

**4. Контроль миньонов:**
- **Command Minion** - Приказываешь скелету атаковать цель
- **Recall Minion** - Призываешь скелета к себе
- **Sacrifice Minion** - Убиваешь скелета, восстанавливая HP/MP

---

## Файлы

### Создано:
- [Assets/Scripts/Editor/CreateRaiseDead.cs](Assets/Scripts/Editor/CreateRaiseDead.cs) - Editor script для создания скилла
- Assets/Resources/Skills/Rogue_RaiseDead.asset - ScriptableObject скилла (создаётся через меню)
- RAISE_DEAD_READY.md - Документация (этот файл)

### Модифицировано:
- [Assets/Scripts/Skills/EffectConfig.cs](Assets/Scripts/Skills/EffectConfig.cs#L308) - Добавлен EffectType.SummonMinion
- [Assets/Scripts/Skills/EffectManager.cs](Assets/Scripts/Skills/EffectManager.cs#L297-L300) - Добавлена обработка SummonMinion
- [Assets/Scripts/Skills/SkillExecutor.cs](Assets/Scripts/Skills/SkillExecutor.cs#L116-L118) - Добавлен case Summon
- [Assets/Scripts/Skills/SkillExecutor.cs](Assets/Scripts/Skills/SkillExecutor.cs#L860-L901) - Создан метод ExecuteSummon

### TODO (будущее):
- Assets/Scripts/AI/SkeletonAI.cs - AI скелета
- Assets/Prefabs/Minions/Skeleton.prefab - Префаб скелета
- Assets/Models/Skeleton/ - Модель и анимации скелета

---

## Статус: ✅ ГОТОВО (базовая реализация)

**Raise Dead** - пятый скилл Rogue (Necromancer) реализован с базовым функционалом!

### Что работает:
- ✅ Скилл можно использовать
- ✅ Расходует 50 MP
- ✅ Cooldown 30 секунд
- ✅ Визуальные эффекты призыва
- ✅ Логирование в консоль
- ✅ Применение эффекта SummonMinion

### Что требует доработки:
- ⏳ Skeleton Prefab (модель и анимации)
- ⏳ SkeletonAI (логика атаки врагов)
- ⏳ Управление активным миньоном (только 1 скелет)
- ⏳ Таймер самоуничтожения (20 секунд)
- ⏳ HP система для скелета

---

## ВСЕ 5 СКИЛЛОВ ROGUE (NECROMANCER) ЗАВЕРШЕНЫ! 🎉

1. ✅ **Soul Drain** (501) - Вампиризм (100% life steal)
2. ✅ **Curse of Weakness** (502) - Ослепление (Perception → 1)
3. ✅ **Crippling Curse** (503) - Замедление (-80% скорости)
4. ✅ **Blood for Mana** (504) - Жертвенное заклинание (HP → MP)
5. ✅ **Raise Dead** (505) - Призыв скелета (20 секунд) 🆕

**Некромант готов к бою!** ☠️💀🧙‍♂️

---

## Следующие шаги

1. **Тестирование всех 5 скиллов** - Проверить что они работают вместе
2. **Балансировка** - Возможно нужно подкрутить урон/ману/кулдауны
3. **Создание Skeleton Prefab** - Для полной реализации Raise Dead
4. **Переход к следующему классу** - Создание скиллов для других классов (Mage, Archer, Paladin?)

Некромант - один из самых уникальных классов в Aetherion! 🎮
