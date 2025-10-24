# Divine Protection - Настройка скилла

## Описание

**Divine Protection** - второй скилл Паладина (Друида).
Даёт **НЕУЯЗВИМОСТЬ** всем союзникам в радиусе 10 метров на 5 секунд.

---

## Характеристики

- **Skill ID:** 502
- **Class:** Paladin
- **Type:** Buff (AOE)
- **Target:** NoTarget (все союзники в радиусе)
- **Cooldown:** 120 секунд (2 минуты)
- **Mana Cost:** 80
- **Cast Time:** 0.5 секунд
- **Radius:** 10 метров
- **Duration:** 5 секунд
- **Effect:** Invulnerability (неуязвимость)

---

## Шаги установки

### ШАГ 1: Добавить проверку неуязвимости в HealthSystem.cs

**ВАЖНО:** Сделайте это вручную (Unity блокирует файл).

Откройте `Assets/Scripts/Player/HealthSystem.cs` в редакторе кода.

Найдите метод `TakeDamage` (строка ~136):

```csharp
public void TakeDamage(float damage)
{
    if (!IsAlive) return;

    // Применяем снижение урона
    float originalDamage = damage;
```

Измените на:

```csharp
public void TakeDamage(float damage)
{
    if (!IsAlive) return;

    // Проверяем неуязвимость
    EffectManager effectManager = GetComponent<EffectManager>();
    if (effectManager != null && effectManager.HasInvulnerability())
    {
        Debug.Log($"[HealthSystem] 🛡️ НЕУЯЗВИМОСТЬ! Урон {damage:F0} заблокирован");
        return;
    }

    // Применяем снижение урона
    float originalDamage = damage;
```

Сохраните файл.

---

### ШАГ 2: Создать ScriptableObject в Unity

1. Откройте **Unity Editor**
2. В меню выберите: **Aetherion → Skills → Paladin → Create Divine Protection**
3. Skill Config будет создан автоматически в `Assets/Resources/Skills/Paladin_DivineProtection.asset`

---

### ШАГ 3: Проверить настройки

Откройте созданный asset `Paladin_DivineProtection.asset` в Inspector и убедитесь:

**Основные параметры:**
- ✅ Skill ID = 502
- ✅ Skill Name = "Divine Protection"
- ✅ Character Class = Paladin
- ✅ Skill Type = Buff
- ✅ Target Type = NoTarget

**Использование:**
- ✅ Cooldown = 120
- ✅ Mana Cost = 80
- ✅ Cast Time = 0.5
- ✅ Can Use While Moving = false

**AOE:**
- ✅ AOE Radius = 10
- ✅ Max Targets = 20

**Effects:**
- ✅ Size = 1
- ✅ Element 0:
  - Effect Type = Invulnerability
  - Duration = 5
  - Effect Name = "Divine Protection"
  - Particle Effect = "CFXR3 Magic Aura A (Runic)"

**Visual Effects:**
- ✅ Cast Effect = "CFXR3 Magic Aura A (Runic)"
- ✅ Hit Effect = "CFXR3 Magic Aura A (Runic)"
- ✅ Caster Effect = "CFXR3 Hit Light B (Air)"

---

## Как работает

### 1. Использование скилла

```csharp
// Игрок нажимает клавишу скилла
SkillExecutor.UseSkill(1); // Слот 1
```

### 2. Выполнение (SkillExecutor.cs)

```csharp
// Skill Type = Buff → вызывается ExecuteBuff()
case SkillConfigType.Buff:
    ExecuteBuff(skill, target);
    break;
```

### 3. Поиск союзников в радиусе

```csharp
// ExecuteBuff ищет всех союзников в радиусе 10м
Collider[] hits = Physics.OverlapSphere(transform.position, 10f);

foreach (Collider hit in hits)
{
    // Находит игроков (NetworkPlayer, LocalPlayer)
    if (isAlly)
    {
        // Применяет эффект Invulnerability
        EffectManager effectManager = hit.GetComponent<EffectManager>();
        effectManager.ApplyEffect(invulnerabilityEffect, casterStats);
    }
}
```

### 4. Применение неуязвимости (EffectManager.cs)

```csharp
// EffectManager добавляет ActiveEffect
ActiveEffect effect = new ActiveEffect
{
    config = invulnerabilityEffect, // EffectType.Invulnerability
    remainingDuration = 5f,          // 5 секунд
    visualEffect = goldAura          // Золотая аура
};

activeEffects.Add(effect);
```

### 5. Блокировка урона (HealthSystem.cs)

```csharp
public void TakeDamage(float damage)
{
    // ПРОВЕРКА НЕУЯЗВИМОСТИ
    if (effectManager.HasInvulnerability())
    {
        Debug.Log("🛡️ НЕУЯЗВИМОСТЬ! Урон заблокирован");
        return; // Урон не применяется!
    }

    // Обычный урон...
}
```

### 6. Истечение эффекта

Через 5 секунд `EffectManager.Update()` удаляет эффект:

```csharp
if (effect.remainingDuration <= 0)
{
    RemoveEffect(effect);
    Debug.Log("⏱️ Divine Protection закончился");
}
```

---

## Тестирование

### В Unity (Play Mode)

1. Запустите игру (ArenaScene или SkillTestScene)
2. Выберите Paladin
3. Убедитесь что Divine Protection в слоте 1
4. Подойдите к союзникам
5. Нажмите `2` для использования Divine Protection
6. **Ожидаемый результат:**
   - Золотая аура появляется на паладине
   - Золотая аура появляется на всех союзниках в 10м
   - Союзники не получают урон 5 секунд
   - В логах: `🛡️ НЕУЯЗВИМОСТЬ! Урон X заблокирован`

### Логи в консоли

```
[SkillExecutor] Using skill: Divine Protection
[SkillExecutor] AOE center: (X, Y, Z), radius: 10
[SkillExecutor] Found targets: 3
[EffectManager] ✅ Применён эффект: Invulnerability (5.0с)
[HealthSystem] 🛡️ НЕУЯЗВИМОСТЬ! Урон 50 заблокирован
[HealthSystem] 🛡️ НЕУЯЗВИМОСТЬ! Урон 30 заблокирован
[EffectManager] ⏱️ Эффект завершён: Invulnerability
```

---

## Сетевая синхронизация

Divine Protection автоматически синхронизируется с сервером:

### Клиент → Сервер
```json
{
  "type": "skill_used",
  "skillId": 502,
  "targetPosition": {"x": 10, "y": 0, "z": 15}
}
```

### Сервер → Все клиенты
```json
{
  "type": "effect_applied",
  "targetSocketId": "abc123",
  "effectType": 13, // Invulnerability
  "duration": 5
}
```

Все клиенты применяют визуальный эффект (золотую ауру) на целях.

---

## Визуальные эффекты

- **Cast Effect:** Золотая магическая аура на кастере (CFXR3 Magic Aura A)
- **Hit Effect:** Золотая магическая аура на целях (CFXR3 Magic Aura A)
- **Caster Effect:** Золотое свечение вокруг кастера (CFXR3 Hit Light B)
- **Duration Effect:** Золотая аура остаётся на целях 5 секунд

---

## Баланс

- **Cooldown 120 секунд** - очень мощный защитный скилл, можно использовать раз в 2 минуты
- **Mana Cost 80** - высокая стоимость (40% маны паладина)
- **Duration 5 секунд** - достаточно для защиты от комбо атак
- **Radius 10 метров** - нужно держать команду рядом
- **Не стакается** - нельзя использовать несколько раз подряд

---

## Комбинации

**Хорошо сочетается с:**
- Bear Form (трансформация) - неуязвимость + повышенный урон
- Warrior Battle Rage - команда получает +50% урон без риска смерти
- Archer Eagle Eye - безопасная позиция для критов

**Контрится:**
- Silence (Молчание) - нельзя использовать скилл
- Stun (Оглушение) - не даёт произнести каст
- Interrupt - прерывание 0.5с каста

---

## Файлы

**Созданные файлы:**
- `Assets/Scripts/Editor/CreateDivineProtection.cs` - Editor скрипт для создания
- `Assets/Resources/Skills/Paladin_DivineProtection.asset` - SkillConfig
- `DIVINE_PROTECTION_SETUP.md` - Эта документация

**Изменённые файлы:**
- `Assets/Scripts/Player/HealthSystem.cs` - Добавлена проверка неуязвимости

---

## Следующие шаги

После создания Divine Protection можно добавить:

1. 🔲 **Paladin Skill #3** - Resurrection (воскрешение союзника)
2. 🔲 **Paladin Skill #4** - Holy Strike (физическая атака + лечение)
3. 🔲 **Paladin Skill #5** - Lay on Hands (мгновенное лечение)

---

## Troubleshooting

### Проблема: Урон всё равно проходит

**Решение:** Убедитесь что добавили проверку в HealthSystem.cs (Шаг 1)

### Проблема: Эффект не применяется к союзникам

**Решение:** Проверьте что `canTargetAllies = true` в SkillConfig

### Проблема: Золотая аура не появляется

**Решение:** Проверьте что префаб `CFXR3 Magic Aura A (Runic)` существует в `Resources/Effects/`

---

✅ **Divine Protection готов к тестированию!**
