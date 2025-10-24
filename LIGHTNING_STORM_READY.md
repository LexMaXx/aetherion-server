# ⚡ Lightning Storm - ГОТОВ К ТЕСТИРОВАНИЮ

## Создано

### 1. Lightning Storm SkillConfig
**Файл:** `Assets/Scripts/Editor/CreateLightningStorm.cs`

**Использование:**
```
Unity Menu → Aetherion → Skills → Create Lightning Storm (Mage)
```

**Параметры скилла:**
- **Skill ID:** 203
- **Тип:** AOE Damage + Chain Lightning
- **Базовый урон:** 60 + INT*2.5
- **AOE радиус:** 10 метров
- **Максимум целей в AOE:** 5 врагов
- **Cooldown:** 12 секунд
- **Mana:** 60

**Chain Lightning:**
- **Прыжков:** 3
- **Радиус поиска:** 8 метров
- **Урон каждого прыжка:** 70% от предыдущего
- **Пример:**
  - Первая цель: 82.5 урона (базовый)
  - Прыжок 1: 57.75 урона (70%)
  - Прыжок 2: 40.43 урона (49%)
  - Прыжок 3: 28.3 урона (34.3%)

---

## 2. Chain Lightning Механика

### Добавлено в SkillConfig.cs

**Новый класс:** `SkillCustomData`
```csharp
[System.Serializable]
public class SkillCustomData
{
    // Chain Lightning
    public int chainCount = 0;                    // Количество прыжков
    public float chainRadius = 8f;                // Радиус поиска следующей цели
    public float chainDamageMultiplier = 0.7f;    // Множитель урона (70%)

    // Multi-Hit (для будущих скиллов)
    public int hitCount = 1;
    public float hitDelay = 0.1f;

    // Piercing (для будущих скиллов)
    public bool piercing = false;
    public int maxPierceTargets = 3;
}
```

**Новое поле в SkillConfig:**
```csharp
public SkillCustomData customData;
```

---

### Добавлено в SkillExecutor.cs

**Новый метод:** `ExecuteChainLightning()`

**Логика:**
1. Первая молния бьет всех врагов в AOE радиусе (10м)
2. От каждого пораженного врага запускается цепь молний
3. Молния ищет ближайшего врага в радиусе 8м (который еще не был поражен)
4. Урон уменьшается на 30% с каждым прыжком (70% от предыдущего)
5. Максимум 3 прыжка
6. На каждом прыжке показывается визуальный эффект

**Ключевой код:**
```csharp
// В ExecuteAOEDamage - после нанесения урона каждой цели
if (skill.customData != null && skill.customData.chainCount > 0)
{
    ExecuteChainLightning(skill, enemy.transform, damage, hitTargets, 0);
}

// Новый метод
private void ExecuteChainLightning(SkillConfig skill, Transform fromTarget, float baseDamage, List<Transform> alreadyHitTargets, int currentChain)
{
    // Рекурсивный поиск следующей цели
    // Урон уменьшается с каждым прыжком
    // Не бьет по одной цели дважды
}
```

---

## Как работает Lightning Storm

### Сценарий 1: 5 врагов близко друг к другу
```
Игрок → нажимает 3 (Lightning Storm)

[AOE фаза - радиус 10м]
💥 Враг 1: 82.5 урона
💥 Враг 2: 82.5 урона
💥 Враг 3: 82.5 урона
💥 Враг 4: 82.5 урона
💥 Враг 5: 82.5 урона

[Chain Lightning от Врага 1]
⚡ Chain #1: 57.75 урона → Враг 6 (ближайший в радиусе 8м)
⚡ Chain #2: 40.43 урона → Враг 7
⚡ Chain #3: 28.3 урона → Враг 8

[Chain Lightning от Врага 2]
⚡ Chain #1: 57.75 урона → Враг 9
⚡ Chain #2: 40.43 урона → Враг 10
⚡ Chain Lightning прервана (нет целей в радиусе 8м)

... и так далее для остальных врагов
```

**Итого:**
- 5 врагов получили полный урон (82.5)
- До 15 дополнительных прыжков молний (3 прыжка × 5 начальных целей)
- Если врагов мало, цепь прерывается раньше

---

### Сценарий 2: 3 DummyEnemy в тестовой сцене
```
[AOE фаза]
💥 DummyEnemy1: 82.5 урона
💥 DummyEnemy2: 82.5 урона
💥 DummyEnemy3: 82.5 урона

[Chain Lightning от DummyEnemy1]
⚡ Chain #1: 57.75 урона → DummyEnemy2 (уже был поражен, пропускаем)
⚡ Chain #1: 57.75 урона → DummyEnemy3 (уже был поражен, пропускаем)
⚡ Chain Lightning прервана (все цели уже были поражены)

[Chain Lightning от DummyEnemy2]
⚡ Chain Lightning прервана (все цели уже были поражены)

[Chain Lightning от DummyEnemy3]
⚡ Chain Lightning прервана (все цели уже были поражены)
```

**Итого:** Если врагов мало (3), они получат только AOE урон без дополнительных прыжков.

---

## Тестирование

### Шаг 1: Создать SkillConfig
```
1. Unity → Aetherion → Skills → Create Lightning Storm (Mage)
2. Проверить что создан: Assets/Resources/Skills/Mage_LightningStorm.asset
```

### Шаг 2: Добавить в слот
```csharp
// В SetupSkillTestScene.cs или вручную
SkillConfig lightningStorm = Resources.Load<SkillConfig>("Skills/Mage_LightningStorm");
skillExecutor.equippedSkills[2] = lightningStorm;  // Слот 3
```

### Шаг 3: Тестировать
```
1. Play SkillTestScene
2. Нажать 3 (Lightning Storm)
3. Проверить в Console:
   - "💥 AOE урон: X → DummyEnemy1"
   - "⚡ Chain #1: X урона → ..."
   - Визуальные эффекты на врагах
```

### Шаг 4: Тест с большим количеством врагов
```
Для теста chain lightning:
1. Создать 8-10 DummyEnemy в сцене
2. Расставить их на расстоянии 5-7 метров друг от друга
3. Встать в центр
4. Использовать Lightning Storm
5. Наблюдать как молнии перепрыгивают между врагами
```

---

## Визуальные эффекты

**Используются из Cartoon FX Remaster:**
- **Cast Effect:** CFXR3 Hit Electric C (Air) - молния сверху на мага
- **Hit Effect:** CFXR3 Hit Electric C (Air) - электрический удар на каждого врага
- **AOE Effect:** CFXR3 Hit Light B (Air) - светлый шторм вокруг мага

**Каждый chain прыжок** также показывает Hit Effect на новой цели.

---

## Детали реализации

### Почему chain от каждой цели?
Изначально можно было сделать chain только от ОДНОЙ цели, но текущая реализация:
- От **каждого** врага в AOE запускается своя цепь
- Это создаёт эффект "шторма молний"
- Больше урона и визуальных эффектов
- Более эпичный скилл

### Защита от дублирования урона
```csharp
List<Transform> alreadyHitTargets = new List<Transform>();
```
- Каждая цель добавляется в список после первого попадания
- Chain lightning пропускает уже пораженные цели
- Ни одна цель не получит урон дважды от одного каста

### Выбор следующей цели
```csharp
// Выбираем БЛИЖАЙШУЮ цель в радиусе
float closestDistance = float.MaxValue;
foreach (Collider hit in nearbyTargets)
{
    float distance = Vector3.Distance(fromTarget.position, hit.transform.position);
    if (distance < closestDistance)
    {
        closestDistance = distance;
        nextTarget = hit.transform;
    }
}
```

---

## Текущее состояние скиллов мага

| Слот | Скилл | Тип | Урон | Cooldown | Статус |
|------|-------|-----|------|----------|--------|
| 1 | Fireball | Projectile | 50 + INT*2.5 + Burn DoT | 6s | ✅ Работает |
| 2 | Ice Nova | AOE | 40 + INT*2.0 + Slow 50% | 8s | ✅ Работает |
| 3 | Lightning Storm | AOE + Chain | 60 + INT*2.5 + Chain×3 | 12s | ✅ ГОТОВ |

---

## Console Log Пример

```
[SkillExecutor] 🔥 Попытка использовать скилл в слоте 2
[SkillExecutor] 💧 Потрачено 60 маны. Осталось: 440
[SkillExecutor] 🔍 AOE поиск: центр=(5, 0, 5), радиус=10м, найдено коллайдеров=5
[SkillExecutor] 💥 AOE урон: 82 → DummyEnemy1
[SkillExecutor] ⚡ Chain #1: 58 урона → DummyEnemy4
[SkillExecutor] ⚡ Chain #2: 40 урона → DummyEnemy7
[SkillExecutor] ⚡ Chain #3: 28 урона → DummyEnemy8
[SkillExecutor] 💥 AOE урон: 82 → DummyEnemy2
[SkillExecutor] ⚡ Chain #1: 58 урона → DummyEnemy5
[SkillExecutor] ⚡ Chain #2: 40 урона → DummyEnemy6
[SkillExecutor] ⚡ Chain Lightning прервана (нет целей в радиусе 8м)
[SkillExecutor] 💥 AOE урон: 82 → DummyEnemy3
[SkillExecutor] ⚡ Chain Lightning прервана (все цели уже были поражены)
[SkillExecutor] 💥 AOE Lightning Storm: 82 урона по 3 целям
[SkillExecutor] ⚡ Использован скилл: Lightning Storm
```

---

## Что дальше?

**Готово к тестированию!**

Создай SkillConfig через меню Unity и протестируй скилл.

**Возможные улучшения:**
1. Добавить линию молнии между целями (LineRenderer)
2. Добавить звук грома
3. Добавить небольшой Stun на 0.5 секунды
4. Сделать визуальную анимацию прыжка молнии
