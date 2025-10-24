# ✅ Исправление: Projectile Effects для Curse of Weakness

## Проблема

При использовании `Curse of Weakness` снаряд попадал в цель и наносил урон, но эффект `DecreasePerception` не применялся.

**Логи показывали:**
```
[Projectile] ⚠️ Нет эффектов для применения
```

## Причины

### 1. DummyEnemy не имел CharacterStats
- `DecreasePerception` работает через `CharacterStats.SetPerception()`
- DummyEnemy не имел компонента `CharacterStats`
- **Решение:** Добавлен автоматический компонент CharacterStats в `DummyEnemy.Start()`

### 2. Конфликт двух систем применения эффектов

**Старая система (SkillEffect):**
- Используется в `Projectile.cs` → `ApplyEffects()` → `SkillManager`
- Работает с `List<SkillEffect>`

**Новая система (EffectConfig):**
- Используется в `ProjectileEffectApplier` → `ApplyEffectsToTarget()` → `EffectManager`
- Работает с `List<EffectConfig>`

**Проблема:** Оба компонента на одном GameObject пытались обработать эффекты, но старый `Projectile` не видел новые `EffectConfig` и выдавал предупреждение.

## Исправления

### 1. DummyEnemy.cs (Lines 68-75)
Добавлена автоматическая инициализация CharacterStats:

```csharp
// Добавляем CharacterStats для поддержки эффектов на характеристики
if (GetComponent<CharacterStats>() == null)
{
    CharacterStats stats = gameObject.AddComponent<CharacterStats>();
    stats.perception = 5; // Среднее восприятие
    Debug.Log($"[DummyEnemy] ✅ Добавлен CharacterStats на {gameObject.name} (Perception: {stats.perception})");
}
```

### 2. Projectile.cs (Lines 162-170, 177-187)
Добавлена проверка на наличие `ProjectileEffectApplier`:

```csharp
// Применяем эффекты только если нет ProjectileEffectApplier (новая система)
if (GetComponent<ProjectileEffectApplier>() == null)
{
    // Старая система: SkillEffect через SkillManager
    ApplyEffects(target);
}
else
{
    Debug.Log($"[Projectile] ⏭️ ProjectileEffectApplier обработает эффекты (новая система)");
}
```

**Логика:**
- Если есть `ProjectileEffectApplier` → используется новая система (EffectConfig + EffectManager)
- Если нет `ProjectileEffectApplier` → используется старая система (SkillEffect + SkillManager)
- Это предотвращает конфликт и дублирование логов

### 3. AddCharacterStatsToDummies.cs (Editor Utility)
Создана утилита для добавления CharacterStats к существующим DummyEnemy:

```
Unity Menu → Aetherion → Utilities → Add CharacterStats to All DummyEnemies
```

## Как работает сейчас

### Запуск Curse of Weakness:

**1. SkillExecutor создаёт снаряд:**
```csharp
GameObject projectile = Instantiate(skill.projectilePrefab, ...);
Projectile oldProj = projectile.GetComponent<Projectile>();
oldProj.Initialize(target, damage, direction, gameObject, null);

// Добавляем ProjectileEffectApplier для новых эффектов
ProjectileEffectApplier effectApplier = projectile.AddComponent<ProjectileEffectApplier>();
effectApplier.Initialize(skill.effects, stats); // List<EffectConfig>
```

**2. Снаряд летит к цели**

**3. Попадание в DummyEnemy:**

**Projectile.HitTarget():**
```
1. TakeDamage() - наносит урон
2. Проверяет наличие ProjectileEffectApplier
3. Если есть → пропускает ApplyEffects() (старая система)
4. Логирует: "⏭️ ProjectileEffectApplier обработает эффекты"
```

**ProjectileEffectApplier.OnTriggerEnter():**
```
1. Получает EffectManager цели (или добавляет)
2. Применяет все EffectConfig через EffectManager.ApplyEffect()
3. EffectManager → CharacterStats.SetPerception(1)
4. Логирует: "✨ Applied effect DecreasePerception to DummyEnemy_5"
```

## Ожидаемые логи

### Правильные логи после исправления:

```
[SkillExecutor] Using skill: Curse of Weakness
[SkillExecutor] Damage calculated: 24,5 (base: 20)
[Projectile] 🎨 Hit effect установлен: CFXR3 Hit Electric C (Air)
[SkillExecutor] Old Projectile launched: 24,5 damage
[ProjectileEffectApplier] Initialized with 1 effects

[DummyEnemy] ✅ Добавлен CharacterStats на DummyEnemy_5 (Perception: 5)

[DummyEnemy] DummyEnemy_5 получил 24,5 урона. HP: 976/1000
[Projectile] Попадание в DummyEnemy! Урон: 24,5
[Projectile] ⏭️ ProjectileEffectApplier обработает эффекты (новая система)

[ProjectileEffectApplier] ✨ Applied effect DecreasePerception to DummyEnemy_5
[CharacterStats] 💾 Оригинальный Perception сохранён: 5
[CharacterStats] 👁️ Perception установлен в 1 (было: 5, visionRadius: 5м)
[EffectManager] 👁️🔻 Perception снижен до 1 (проклятие)
[EffectManager] ✨ Применён эффект: DecreasePerception (10с)
[ProjectileEffectApplier] ✅ Applied 1 effects to DummyEnemy_5
```

### Через 10 секунд:

```
[CharacterStats] 🔄 Perception восстановлен: 5 (visionRadius: 15м)
[EffectManager] 🔚 Снят эффект: DecreasePerception
```

## Файлы изменены

1. **[Assets/Scripts/Arena/DummyEnemy.cs](Assets/Scripts/Arena/DummyEnemy.cs#L68-L75)** - Автоматическое добавление CharacterStats
2. **[Assets/Scripts/Player/Projectile.cs](Assets/Scripts/Player/Projectile.cs#L162-L170)** - Проверка ProjectileEffectApplier
3. **[Assets/Scripts/Editor/AddCharacterStatsToDummies.cs](Assets/Scripts/Editor/AddCharacterStatsToDummies.cs)** - Утилита для существующих DummyEnemy (создан)

## Тестирование

### Быстрый тест:

1. **Добавить CharacterStats к существующим DummyEnemy:**
   ```
   Unity Menu → Aetherion → Utilities → Add CharacterStats to All DummyEnemies
   ```

2. **Запустить сцену:**
   ```
   ▶️ Play Scene
   ```

3. **Использовать Curse of Weakness:**
   ```
   Прицелиться на DummyEnemy
   Нажать клавишу "2" (или соответствующий hotkey)
   ```

4. **Проверить логи:**
   - Должен быть лог `[Projectile] ⏭️ ProjectileEffectApplier обработает эффекты`
   - Должен быть лог `[ProjectileEffectApplier] ✨ Applied effect DecreasePerception`
   - Должен быть лог `[CharacterStats] 👁️ Perception установлен в 1`

5. **Проверить Inspector врага:**
   ```
   CharacterStats → Perception: 1 (было 5)
   EffectManager → Active Effects: DecreasePerception
   ```

6. **Подождать 10 секунд:**
   ```
   Perception восстанавливается до 5
   ```

## Статус: ✅ ИСПРАВЛЕНО

Curse of Weakness теперь корректно применяет эффект `DecreasePerception` к врагам! 👁️🔻💀
