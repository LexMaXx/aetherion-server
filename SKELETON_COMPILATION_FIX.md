# ✅ Skeleton AI - Исправление ошибок компиляции

## Проблема
```
error CS0246: The type or namespace name 'Target' could not be found
```

## Причина
В проекте используется класс `Enemy`, а не `Target` для маркировки врагов.

## Решение
Заменил все упоминания `Target` на `Enemy` в SkeletonAI.cs:

### Изменение 1: FindTarget() метод
```csharp
// БЫЛО:
Target[] targets = FindObjectsOfType<Target>();
foreach (Target target in targets)

// СТАЛО:
Enemy[] enemies = FindObjectsOfType<Enemy>();
foreach (Enemy enemy in enemies)
```

### Изменение 2: IsTargetValid() метод  
```csharp
// БЫЛО:
Target targetComponent = currentTarget.GetComponent<Target>();

// СТАЛО:
Enemy enemyComponent = currentTarget.GetComponent<Enemy>();
if (!enemyComponent.IsAlive())
```

### Изменение 3: Attack() метод
```csharp
// БЫЛО:
HealthSystem targetHealth = currentTarget.GetComponent<HealthSystem>();

// СТАЛО:
Enemy enemy = currentTarget.GetComponent<Enemy>();
if (enemy != null)
{
    enemy.TakeDamage(damage);
}
```

## Статус: ✅ Исправлено!

Все ошибки компиляции устранены. SkeletonAI теперь корректно использует класс Enemy.

## Инструкции по настройке в Unity

### Шаг 1: Skeleton prefab
```
1. Перемести Skeleton humanoid префаб в:
   Assets/Resources/Minions/Skeleton.prefab

2. Добавь компоненты:
   - NavMeshAgent (Speed: 3.5, Stopping Distance: 1.5)
   - Rigidbody (Freeze Rotation X, Z)
   - Animator (Controller: Rogue)
```

### Шаг 2: Добавь Enemy компонент на врагов
```
1. Найди DummyEnemy в сцене
2. Add Component → Enemy
3. Tag: "Enemy"
```

### Шаг 3: Bake NavMesh
```
Window → AI → Navigation → Bake
```

### Шаг 4: Тест
```
1. Play
2. Нажми Raise Dead (клавиша 5)
3. Скелет побежит к врагу и атакует!
```

## Ожидаемые логи:
```
[SkillExecutor] 💀 Raise Dead: миньон призван на 20 секунд
[SkeletonAI] 💀 Skeleton initialized
[SkeletonAI] 🎯 Новая цель найдена: DummyEnemy
[SkeletonAI] ⚔️ Skeleton атакует DummyEnemy: 40 урона
[Enemy] DummyEnemy получил 40 урона
```

Готово к тестированию! 🎉
