# ✨ Teleport - ГОТОВ К ТЕСТИРОВАНИЮ

## Создано

### 1. Teleport SkillConfig
**Файл:** `Assets/Scripts/Editor/CreateTeleport.cs`

**Использование:**
```
Unity Menu → Aetherion → Skills → Create Teleport (Mage)
```

**Параметры скилла:**
- **Skill ID:** 204
- **Тип:** Movement (Teleport)
- **Target Type:** Ground (выбор точки на земле)
- **Максимальная дальность:** 15 метров
- **Скорость:** Мгновенная (0 = instant teleport)
- **Cooldown:** 8 секунд
- **Mana:** 30

---

## 2. Реализованная механика

### Изменения в SkillExecutor.cs

**1. Исправлен MovementDirection.MouseDirection** (line 870-881)
```csharp
case MovementDirection.MouseDirection:
    // Используем переданную targetPosition (ground target)
    if (targetPosition != Vector3.zero)
    {
        destination = targetPosition;
    }
    else
    {
        // Если targetPosition не задана, движемся вперёд
        destination = transform.position + transform.forward * skill.movementDistance;
    }
    break;
```

**2. Teleport уже работает** (line 730-735)
```csharp
case MovementType.Teleport:
case MovementType.Blink:
    // Мгновенное перемещение
    transform.position = destination;
    Log($"✨ Телепорт в {destination}");
    break;
```

---

### Изменения в SimplePlayerController.cs

**1. Добавлена поддержка Ground Target** (line 190-204)
```csharp
// Ground Target скиллы (Teleport, Meteor) - клик ПКМ на землю
if (skill.targetType == SkillTargetType.Ground)
{
    Debug.Log($"[SimplePlayerController] 📍 Ground target скилл. Нажмите ПКМ на землю для выбора позиции.");

    // Для тестирования - телепорт на 5 метров вперёд
    Vector3 groundTarget = transform.position + transform.forward * 5f;
    bool success = skillExecutor.UseSkill(slotIndex, null, groundTarget);

    if (!success)
    {
        Debug.LogWarning($"[SimplePlayerController] ❌ Не удалось использовать скилл {slotIndex}");
    }
    return;
}
```

**2. Добавлена клавиша 4** (line 173-176)
```csharp
else if (Input.GetKeyDown(KeyCode.Alpha4))
{
    UseSkill(3);
}
```

**3. Обновлена справка** (line 234-237)
```csharp
Debug.Log("  1 - Fireball (требует цель)");
Debug.Log("  2 - Ice Nova (AOE вокруг себя)");
Debug.Log("  3 - Lightning Storm (AOE + Chain Lightning)");
Debug.Log("  4 - Teleport (телепорт вперёд на 5м)");
```

---

### Изменения в CreateTestPlayer.cs

**Добавлен Teleport в слот 3:**
```csharp
// Slot 3: Teleport
SkillConfig teleport = AssetDatabase.LoadAssetAtPath<SkillConfig>(
    "Assets/Resources/Skills/Mage_Teleport.asset"
);

if (teleport != null)
{
    skillExecutor.equippedSkills.Add(teleport);
    Debug.Log("✅ Slot 3: Teleport");
    skillCount++;
}
```

---

## Как работает Teleport

### Текущая реализация (упрощённая для тестирования)
```
Игрок нажимает 4 (Teleport)
↓
SimplePlayerController проверяет: skill.targetType == Ground
↓
Вычисляет точку телепорта: transform.forward * 5м
↓
Передаёт groundTarget в SkillExecutor
↓
SkillExecutor.ExecuteMovement()
↓
CalculateMovementDestination() использует targetPosition
↓
transform.position = destination (мгновенная телепортация)
↓
Визуальные эффекты на старом и новом месте
```

### Будущая реализация (с ПКМ)
```
Игрок нажимает 4 (Teleport)
↓
Появляется курсор/индикатор на земле
↓
Игрок кликает ПКМ на нужную точку
↓
Raycast определяет точку на земле
↓
Проверка дальности (макс 15м)
↓
Телепортация в выбранную точку
```

---

## Визуальные эффекты

**Используются из Cartoon FX Remaster:**
- **Cast Effect:** CFXR3 Magic Aura A (Runic) - магическая аура на старом месте (исчезновение)
- **Hit Effect:** CFXR3 Magic Aura A (Runic) - магическая аура на новом месте (появление)
- **AOE Effect:** CFXR3 Hit Light B (Air) - световой эффект (ground target индикатор)

---

## Тестирование

### Шаг 1: Создать SkillConfig
```
Unity → Aetherion → Skills → Create Teleport (Mage)
```

Проверить что создан: `Assets/Resources/Skills/Mage_Teleport.asset`

### Шаг 2: Создать TestPlayer
```
Unity → Aetherion → Create Test Player in Scene
```

В консоли должно появиться:
```
✅ Slot 0: Fireball
✅ Slot 1: Ice Nova
✅ Slot 2: Lightning Storm
✅ Slot 3: Teleport
⚡ Экипировано скиллов: 4/4
```

### Шаг 3: Тестировать
1. Play SkillTestScene ▶️
2. Нажми `4` (Teleport)
3. Персонаж должен телепортироваться на 5 метров вперёд
4. Проверь в Console:
   - "📍 Ground target скилл..."
   - "✨ Телепорт в (X, Y, Z)"
5. Проверь визуальные эффекты на старом и новом месте

---

## Текущее состояние скиллов мага

| Слот | Скилл | Тип | Механика | Cooldown | Статус |
|------|-------|-----|----------|----------|--------|
| 0 | Fireball | Projectile | Requires target + Burn DoT | 6s | ✅ Работает |
| 1 | Ice Nova | AOE | No target + Slow 50% | 8s | ✅ Работает |
| 2 | Lightning Storm | AOE + Chain | Chain x3 (70% dmg) | 12s | ✅ Работает |
| 3 | Teleport | Movement | Ground target (15m max) | 8s | ✅ ГОТОВ |

---

## Управление

| Клавиша | Действие |
|---------|----------|
| WASD | Движение |
| ЛКМ | Выбрать врага (для Fireball) |
| 1 | Fireball (требует цель) |
| 2 | Ice Nova (AOE вокруг себя) |
| 3 | Lightning Storm (AOE + Chain Lightning) |
| 4 | Teleport (телепорт вперёд на 5м) |
| H | Помощь |

---

## Console Log Пример

```
[SimplePlayerController] 🔥 Попытка использовать скилл в слоте 3
[SimplePlayerController] 📍 Ground target скилл. Нажмите ПКМ на землю для выбора позиции.
[SkillExecutor] 💧 Потрачено 30 маны. Осталось: 470
[SkillExecutor] ✨ Телепорт в (5.0, 0.0, 5.0)
[SkillExecutor] ⚡ Использован скилл: Teleport
```

---

## Улучшения для будущего

### 1. Настоящий Ground Target с ПКМ
```csharp
// В SimplePlayerController
if (Input.GetMouseButtonDown(1)) // ПКМ
{
    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    if (Physics.Raycast(ray, out RaycastHit hit))
    {
        Vector3 groundTarget = hit.point;
        // Проверка дальности
        if (Vector3.Distance(transform.position, groundTarget) <= skill.castRange)
        {
            skillExecutor.UseSkill(slotIndex, null, groundTarget);
        }
    }
}
```

### 2. Визуальный индикатор ground target
- Проектор на землю показывающий куда телепортируешься
- Цвет: зелёный если в пределах дальности, красный если далеко

### 3. Проверка проходимости
```csharp
// Проверить что точка телепорта не в стене/препятствии
if (Physics.CheckSphere(destination, 0.5f, obstacleLayer))
{
    Debug.LogWarning("Нельзя телепортироваться сюда!");
    return false;
}
```

### 4. Invulnerability во время телепорта
```csharp
// Добавить краткую неуязвимость (0.1 секунды)
EffectConfig invuln = new EffectConfig();
invuln.effectType = EffectType.Invulnerable;
invuln.duration = 0.1f;
skill.effects.Add(invuln);
```

---

## Что дальше?

**Готово к тестированию!**

Создай SkillConfig через Unity меню и протестируй Teleport.

**Готовые скиллы мага (4/4):**
1. ✅ Fireball - Projectile + DoT
2. ✅ Ice Nova - AOE + Slow
3. ✅ Lightning Storm - AOE + Chain Lightning
4. ✅ Teleport - Mobility

**Маг полностью готов для тестирования!** 🔮⚡✨
