# Ice Nova AOE Fix - Complete

## Problems Fixed

### Problem 1: Ice Nova finds 0 enemies
**Issue:** Physics.OverlapSphere не находил DummyEnemy

**Root Cause:**
- SkillExecutor.ExecuteAOEDamage() проверял только `Enemy` component
- DummyEnemy - отдельный компонент для тестирования
- Collider находился, но DummyEnemy не обрабатывался

**Fix:** Added DummyEnemy support to ExecuteAOEDamage
```csharp
// БЫЛО (line 462-486):
Enemy enemy = hit.GetComponent<Enemy>();
NetworkPlayer networkTarget = hit.GetComponent<NetworkPlayer>();

if (enemy != null) { ... }
else if (networkTarget != null) { ... }

// СТАЛО:
Enemy enemy = hit.GetComponent<Enemy>();
DummyEnemy dummyEnemy = hit.GetComponent<DummyEnemy>();
NetworkPlayer networkTarget = hit.GetComponent<NetworkPlayer>();

if (enemy != null) { ... }
else if (dummyEnemy != null)
{
    // DummyEnemy для тестирования
    dummyEnemy.TakeDamage(damage);

    // Эффект попадания
    if (skill.hitEffectPrefab != null)
    {
        Instantiate(skill.hitEffectPrefab, dummyEnemy.transform.position, Quaternion.identity);
    }

    // Применяем эффекты
    ApplyEffectsToTarget(skill, dummyEnemy.transform);

    hitCount++;
    Log($"💥 AOE урон: {damage:F0} → {dummyEnemy.name}");
}
else if (networkTarget != null) { ... }
```

**File:** `Assets/Scripts/Skills/SkillExecutor.cs` (lines 462-504)

---

### Problem 2: Ice Nova works only when target is selected
**Issue:** Скилл срабатывал только если был выбран враг ЛКМ

**Root Cause:**
- SimplePlayerController.UseSkill() **всегда** передавал `currentTarget` в SkillExecutor
- Даже для AOE скиллов которым не нужна цель

**Fix:** Check skill.requiresTarget before passing target
```csharp
// БЫЛО (line 175-193):
void UseSkill(int slotIndex)
{
    Debug.Log($"[SimplePlayerController] 🔥 Попытка использовать скилл в слоте {slotIndex}");

    // Используем скилл
    bool success = skillExecutor.UseSkill(slotIndex, currentTarget, null);

    if (!success)
    {
        Debug.LogWarning($"[SimplePlayerController] ❌ Не удалось использовать скилл {slotIndex}");

        if (currentTarget == null)
        {
            Debug.LogWarning("  → Цель не выбрана! Нажмите ЛКМ чтобы выбрать врага");
        }
    }
}

// СТАЛО:
void UseSkill(int slotIndex)
{
    Debug.Log($"[SimplePlayerController] 🔥 Попытка использовать скилл в слоте {slotIndex}");

    // Получаем скилл из слота
    SkillConfig skill = skillExecutor.GetEquippedSkill(slotIndex);

    if (skill == null)
    {
        Debug.LogWarning($"[SimplePlayerController] ❌ Слот {slotIndex} пуст!");
        return;
    }

    // Для скиллов которые не требуют цель (AOE вокруг себя), передаём null
    Transform targetToUse = skill.requiresTarget ? currentTarget : null;

    // Используем скилл
    bool success = skillExecutor.UseSkill(slotIndex, targetToUse, null);

    if (!success)
    {
        Debug.LogWarning($"[SimplePlayerController] ❌ Не удалось использовать скилл {slotIndex}");

        if (skill.requiresTarget && currentTarget == null)
        {
            Debug.LogWarning("  → Цель не выбрана! Нажмите ЛКМ чтобы выбрать врага");
        }
    }
}
```

**File:** `Assets/Scripts/Player/SimplePlayerController.cs` (lines 175-205)

---

### New Method Added: GetEquippedSkill()
**Purpose:** Allow SimplePlayerController to check skill properties before using it

**Implementation:**
```csharp
/// <summary>
/// Получить скилл в слоте
/// </summary>
public SkillConfig GetEquippedSkill(int slotIndex)
{
    if (slotIndex < 0 || slotIndex >= equippedSkills.Count)
    {
        return null;
    }
    return equippedSkills[slotIndex];
}
```

**File:** `Assets/Scripts/Skills/SkillExecutor.cs` (lines 81-91)

---

## How It Works Now

### Fireball (Slot 0 - requires target)
1. Player presses LMB → selects enemy → `currentTarget` set
2. Player presses `1` → Fireball
3. SimplePlayerController checks: `skill.requiresTarget == true`
4. Passes `currentTarget` to SkillExecutor ✅
5. Fireball fires at selected enemy

### Ice Nova (Slot 1 - no target)
1. Player presses `2` → Ice Nova
2. SimplePlayerController checks: `skill.requiresTarget == false`
3. Passes `null` as target to SkillExecutor ✅
4. ExecuteAOEDamage uses `transform.position` as center
5. Physics.OverlapSphere finds all colliders in 8m radius
6. Checks each collider for `Enemy`, `DummyEnemy`, or `NetworkPlayer`
7. Calls `TakeDamage()` on all found targets ✅

---

## Expected Result

**Scenario 1: Ice Nova with target selected**
```
Player selects Dummy1
Player presses 2 (Ice Nova)
→ targetToUse = null (because !requiresTarget)
→ AOE center = player position
→ Finds: Dummy1, Dummy2, Dummy3 in 8m radius
→ Hits all 3 enemies
✅ WORKS
```

**Scenario 2: Ice Nova without target**
```
Player presses 2 (Ice Nova)
→ targetToUse = null (because !requiresTarget)
→ AOE center = player position
→ Finds: Dummy1, Dummy2, Dummy3 in 8m radius
→ Hits all 3 enemies
✅ WORKS
```

**Scenario 3: Fireball without target**
```
Player presses 1 (Fireball)
→ targetToUse = currentTarget (null)
→ SkillExecutor.UseSkill checks: requiresTarget && target == null
→ Returns false: "❌ Fireball требует цель!"
❌ DOESN'T WORK (expected behavior)
```

---

## Testing Checklist

- [ ] Ice Nova works without selecting target first
- [ ] Ice Nova works even when target IS selected
- [ ] Ice Nova hits all DummyEnemies in 8m radius
- [ ] Console shows: "💥 AOE урон: X → DummyEnemy1"
- [ ] Console shows: "💥 AOE Ice Nova: X урона по 3 целям" (if 3 enemies)
- [ ] Fireball still requires target selection
- [ ] No errors in Console

---

## Files Modified

1. **Assets/Scripts/Skills/SkillExecutor.cs**
   - Added DummyEnemy support to ExecuteAOEDamage (lines 462-504)
   - Added GetEquippedSkill() method (lines 84-91)

2. **Assets/Scripts/Player/SimplePlayerController.cs**
   - Fixed UseSkill to check requiresTarget before passing target (lines 175-205)

---

**Status:** ✅ READY TO TEST

**Next Step:** Run Unity → Play SkillTestScene → Test Ice Nova
