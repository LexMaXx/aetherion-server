# ✅ Автоматическое уничтожение визуальных эффектов

## Проблема
Визуальные эффекты (ауры, молнии, взрывы) не исчезали после использования скиллов, оставаясь в сцене навсегда.

## Решение

### Создан метод SpawnEffect()
**Файл:** `Assets/Scripts/Skills/SkillExecutor.cs` (line 938-947)

```csharp
/// <summary>
/// Создать визуальный эффект с автоуничтожением через 1 секунду
/// </summary>
private void SpawnEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation, float lifetime = 1f)
{
    if (effectPrefab == null) return;

    GameObject effect = Instantiate(effectPrefab, position, rotation);
    Destroy(effect, lifetime);
}
```

### Заменены все Instantiate на SpawnEffect

**Заменено в методах:**
1. ✅ UseSkill() - Cast effect (line 233)
2. ✅ ExecuteInstantDamage() - Hit effect (line 427)
3. ✅ ExecuteAOEDamage() - AOE effect (line 456)
4. ✅ ExecuteAOEDamage() - Hit effects on enemies (line 484, 505)
5. ✅ ExecuteChainLightning() - Chain hit effect (line 611)
6. ✅ ExecuteHeal() - Heal effect (line 639)
7. ✅ ExecuteBuff() - Buff effect (line 662)
8. ✅ ExecuteDebuff() - Debuff effect (line 679)
9. ✅ ExecuteMovement() - Teleport effects (line 708, 723)

### Особенности ExecuteMovement (Teleport)

**Старое место (исчезновение):**
```csharp
// Визуальный эффект на старом месте (исчезновение)
if (skill.castEffectPrefab != null)
{
    GameObject castEffect = Instantiate(skill.castEffectPrefab, startPosition, Quaternion.identity);
    Destroy(castEffect, 1f); // Уничтожаем через 1 секунду
}
```

**Новое место (появление):**
```csharp
// Визуальный эффект на новом месте (появление)
if (skill.hitEffectPrefab != null)
{
    GameObject hitEffect = Instantiate(skill.hitEffectPrefab, destination, Quaternion.identity);
    Destroy(hitEffect, 1f); // Уничтожаем через 1 секунду
}
```

## Результат

Все визуальные эффекты теперь:
- ✅ Появляются при использовании скилла
- ✅ Автоматически исчезают через 1 секунду
- ✅ Не загромождают сцену
- ✅ Не влияют на производительность

## Как это работает

```
Игрок использует Teleport
↓
ExecuteMovement вызывается
↓
SpawnEffect создаёт аура на старом месте
↓
Destroy(effect, 1f) планирует уничтожение через 1 секунду
↓
Телепортация происходит
↓
SpawnEffect создаёт ауру на новом месте
↓
Destroy(effect, 1f) планирует уничтожение через 1 секунду
↓
Через 1 секунду обе ауры автоматически удаляются
```

## Изменения в коде

**До:**
```csharp
// Старый код
if (skill.hitEffectPrefab != null)
{
    Instantiate(skill.hitEffectPrefab, target.position, Quaternion.identity);
}
```

**После:**
```csharp
// Новый код - эффект исчезает через 1 секунду
SpawnEffect(skill.hitEffectPrefab, target.position, Quaternion.identity);
```

## Настройка времени жизни

По умолчанию: **1 секунда**

Можно изменить для конкретного эффекта:
```csharp
SpawnEffect(skill.hitEffectPrefab, position, rotation, 2f); // 2 секунды
SpawnEffect(skill.hitEffectPrefab, position, rotation, 0.5f); // 0.5 секунды
```

## Применяется ко всем скиллам

- ✅ Fireball - Cast effect, Hit effect
- ✅ Ice Nova - AOE effect, Hit effects на врагах
- ✅ Lightning Storm - AOE effect, Hit effects, Chain effects
- ✅ Teleport - Cast effect (старое место), Hit effect (новое место)
- ✅ Все будущие скиллы - автоматически

---

**Статус:** ✅ ИСПРАВЛЕНО

Теперь все визуальные эффекты автоматически исчезают! 🎉
