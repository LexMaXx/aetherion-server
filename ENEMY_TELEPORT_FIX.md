# Исправление телепортации врагов

## Проблема
Враги телепортируются обратно в начальную позицию после атаки.

## Причина
Скорее всего на GameObject врага есть один из этих компонентов:
1. **PlayerController** - применяет скорость от ловкости и двигает персонажа
2. **CharacterStats** - содержит параметр agility
3. **CharacterController** - Unity компонент для движения

Эти компоненты предназначены для **игроков**, а не для **врагов**.

## Решение

### Вариант 1: Проверить в Unity Inspector (РЕКОМЕНДУЕТСЯ)

1. Откройте **ArenaScene**
2. Выберите любого врага в Hierarchy (например `enemy (1)`)
3. Посмотрите на все компоненты в **Inspector**
4. Найдите и **удалите** следующие компоненты если они есть:
   - ❌ `PlayerController` (Script)
   - ❌ `CharacterStats` (Script)
   - ⚠️ `CharacterController` (если он не используется для анимации)
   - ⚠️ `NavMeshAgent` (если есть AI)

5. Оставьте только нужные компоненты:
   - ✅ `Transform`
   - ✅ `Enemy` (Script)
   - ✅ `Animator` (если есть анимации)
   - ✅ `Collider` (для физики)
   - ✅ `Rigidbody` (если нужна физика, но с `isKinematic = true`)

### Вариант 2: Программная проверка

Добавьте в Enemy.cs метод для диагностики:

```csharp
void Start()
{
    currentHealth = maxHealth;

    // ДИАГНОСТИКА: Проверяем нежелательные компоненты
    CheckForPlayerComponents();

    // ... остальной код Start()
}

private void CheckForPlayerComponents()
{
    // Проверяем PlayerController
    PlayerController playerController = GetComponent<PlayerController>();
    if (playerController != null)
    {
        Debug.LogError($"[Enemy] ❌ ОШИБКА: Враг {gameObject.name} имеет компонент PlayerController! Это может вызвать телепортацию!");
        Debug.LogError($"[Enemy] Удалите PlayerController из GameObject врага в Inspector!");
        // Опционально: автоматически удалить
        // Destroy(playerController);
    }

    // Проверяем CharacterStats
    CharacterStats characterStats = GetComponent<CharacterStats>();
    if (characterStats != null)
    {
        Debug.LogWarning($"[Enemy] ⚠️ ВНИМАНИЕ: Враг {gameObject.name} имеет компонент CharacterStats!");
        Debug.LogWarning($"[Enemy] Это может влиять на скорость врага через ловкость (agility={characterStats.agility})");
    }

    // Проверяем CharacterController
    CharacterController characterController = GetComponent<CharacterController>();
    if (characterController != null)
    {
        Debug.LogWarning($"[Enemy] ⚠️ Враг {gameObject.name} имеет CharacterController");
    }

    Debug.Log($"[Enemy] ✅ Диагностика {gameObject.name} завершена");
}
```

### Вариант 3: Создать префаб врага без лишних компонентов

1. Создайте новый пустой GameObject: `EnemyTemplate`
2. Добавьте только нужные компоненты:
   - Enemy (Script)
   - Capsule Collider
   - 3D модель врага
3. Сохраните как префаб: `Assets/Prefabs/Enemies/BasicEnemy.prefab`
4. Замените всех существующих врагов на новый префаб

## Проверка исправления

После удаления лишних компонентов:

1. Запустите игру
2. Атакуйте врага
3. Враг **не должен** телепортироваться обратно
4. Проверьте логи - не должно быть сообщений об ошибках

## Дополнительная диагностика

Если враги всё ещё телепортируются, проверьте:

### 1. Rigidbody настройки
Если у врага есть Rigidbody, убедитесь:
- ✅ `Is Kinematic = true` (если враг не должен падать/двигаться)
- ✅ `Use Gravity = false` (если враг на земле и не двигается)

### 2. Animator Controller
Проверьте, что анимации врага не используют **Root Motion**:
- В Animator: `Apply Root Motion = false`

### 3. NetworkTransform
Если враг синхронизируется по сети, проверьте:
- У врагов **не должно быть** компонента `NetworkTransform`
- Только у игроков должен быть NetworkTransform

## Почему это происходит?

**PlayerController** делает следующее в Update():
1. Считывает input от клавиатуры/геймпада
2. Вычисляет направление движения
3. **Применяет movement к CharacterController**
4. Если input = 0, персонаж стоит на месте

Когда PlayerController на враге:
- Input всегда = 0 (никто не жмёт кнопки для врага)
- CharacterController пытается "стоять на месте"
- Но враг может двигаться от атак/физики
- PlayerController **возвращает** его в "правильную" позицию (0,0,0 или spawn point)

## Итог

✅ **Удалите PlayerController с врагов** - это компонент только для игроков
✅ **Удалите CharacterStats с врагов** - враги не должны иметь SPECIAL статы
✅ Оставьте только Enemy.cs и компоненты для рендеринга/физики

После этого телепортация должна исчезнуть!
