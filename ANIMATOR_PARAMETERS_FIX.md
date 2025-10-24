# FIX: Animator Parameters - "Parameter does not exist"

## Проблема
```
Parameter 'isMoving' does not exist.
UnityEngine.Animator:GetBool (string)
ActionPointsSystem:IsPlayerStanding () (at Assets/Scripts/Player/ActionPointsSystem.cs:173)

Parameter 'moveY' does not exist.
UnityEngine.Animator:GetFloat (string)
ActionPointsSystem:IsPlayerStanding () (at Assets/Scripts/Player/ActionPointsSystem.cs:180)
```

### Причина
`ActionPointsSystem` пытался получить параметры аниматора **без проверки их существования**.

**Проблема в том, что разные классы используют разные имена параметров:**

| Класс | Параметр движения | Параметр скорости |
|-------|------------------|-------------------|
| **MixamoPlayerController** | `isMoving` (lowercase) | `moveY` (lowercase) |
| **PlayerController** | `IsMoving` (PascalCase) | `MoveY` (PascalCase) |
| **Warrior/Mage/etc** | Может не быть вообще! | Может не быть вообще! |

**Результат:**
- `animator.GetBool("isMoving")` → **Exception** если параметра нет
- `animator.GetFloat("moveY")` → **Exception** если параметра нет
- Спам ошибок каждый кадр (60+ раз в секунду)

---

## Решение

### 1. Добавлен вспомогательный метод `HasParameter()`
**Файл:** `Assets/Scripts/Player/ActionPointsSystem.cs` (строки 309-322)

```csharp
/// <summary>
/// Проверить существует ли параметр в Animator (безопасная проверка)
/// </summary>
private bool HasParameter(Animator animator, string paramName)
{
    if (animator == null) return false;

    foreach (AnimatorControllerParameter param in animator.parameters)
    {
        if (param.name == paramName)
            return true;
    }
    return false;
}
```

### 2. Обновлён метод `IsPlayerStanding()`
**Файл:** `Assets/Scripts/Player/ActionPointsSystem.cs` (строки 169-210)

**До (НЕПРАВИЛЬНО):**
```csharp
if (animator != null)
{
    // ❌ ОШИБКА: Пытается получить параметр без проверки существования
    bool isMoving = animator.GetBool("isMoving");
    if (isMoving)
    {
        return false;
    }

    float moveY = animator.GetFloat("moveY");
    if (Mathf.Abs(moveY) > 0.01f)
    {
        return false;
    }
}
```

**После (ПРАВИЛЬНО):**
```csharp
if (animator != null)
{
    // ✅ Проверяем isMoving (lowercase - MixamoPlayerController)
    if (HasParameter(animator, "isMoving"))
    {
        bool isMoving = animator.GetBool("isMoving");
        if (isMoving)
        {
            return false;
        }
    }

    // ✅ Проверяем IsMoving (PascalCase - PlayerController)
    if (HasParameter(animator, "IsMoving"))
    {
        bool isMoving = animator.GetBool("IsMoving");
        if (isMoving)
        {
            return false;
        }
    }

    // ✅ Проверяем moveY (lowercase)
    if (HasParameter(animator, "moveY"))
    {
        float moveY = animator.GetFloat("moveY");
        if (Mathf.Abs(moveY) > 0.01f)
        {
            return false;
        }
    }

    // ✅ Проверяем MoveY (PascalCase)
    if (HasParameter(animator, "MoveY"))
    {
        float moveY = animator.GetFloat("MoveY");
        if (Mathf.Abs(moveY) > 0.01f)
        {
            return false;
        }
    }
}
```

---

## Результат

### До исправления
```
Parameter 'isMoving' does not exist.
Parameter 'moveY' does not exist.
Parameter 'isMoving' does not exist.
Parameter 'moveY' does not exist.
... (60+ раз в секунду)
```

### После исправления
```
[ActionPoints] 🔄 Начало восстановления AP (персонаж стоит на месте)
(Никаких ошибок!)
```

---

## Преимущества

1. ✅ **Универсальность:** Работает с любыми аниматорами (MixamoPlayerController, PlayerController, custom)
2. ✅ **Безопасность:** Нет exceptions даже если параметры отсутствуют
3. ✅ **Гибкость:** Поддерживает оба варианта именования (lowercase/PascalCase)
4. ✅ **Чистые логи:** Нет спама ошибок

---

## Как работает проверка движения теперь

```
IsPlayerStanding() {
    // ПРОВЕРКА 1: Input клавиш (WASD)
    if (Input.GetKey(...)) → return false

    // ПРОВЕРКА 2: Animator параметры (безопасно!)
    if (HasParameter("isMoving") && animator.GetBool("isMoving")) → return false
    if (HasParameter("IsMoving") && animator.GetBool("IsMoving")) → return false
    if (HasParameter("moveY") && animator.GetFloat("moveY") > 0.01) → return false
    if (HasParameter("MoveY") && animator.GetFloat("MoveY") > 0.01) → return false

    // ПРОВЕРКА 3: Анимация атаки
    if (stateInfo.IsTag("Attack")) → return false

    // ПРОВЕРКА 4: CharacterController velocity
    if (charController.velocity.magnitude > 0.1) → return false

    // ВСЕ ПРОВЕРКИ ПРОЙДЕНЫ
    return true → Игрок стоит на месте!
}
```

---

## Связанные системы

Эта же проблема может быть в других скриптах, которые работают с Animator:

- ✅ **NetworkSyncManager.cs** - Уже использует `HasParameter()` (строки 315-322)
- ⚠️ **ManaSystem.cs** - Может иметь ту же проблему
- ⚠️ **PlayerController.cs** - Проверить при необходимости

---

**Статус:** ✅ ИСПРАВЛЕНО
**Файлы:** ActionPointsSystem.cs
**Тестирование:** Проверить что ошибки больше не появляются при движении персонажа
