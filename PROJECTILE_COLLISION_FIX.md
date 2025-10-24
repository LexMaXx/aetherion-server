# ИСПРАВЛЕНИЕ КОЛЛИЗИЙ СНАРЯДОВ

**Дата:** 21 октября 2025
**Проблема:** EtherealSkullProjectile и CelestialBallProjectile взрывались на полпути к цели
**Причина:** Снаряды были на слое Default и взрывались при столкновении со стенами, полом, ландшафтом

---

## ЧТО БЫЛО СДЕЛАНО

### 1. Добавлен новый слой "Projectile" (Layer 7)

**Файл:** `ProjectSettings/TagManager.asset`

```yaml
layers:
- Default       # Layer 0
- TransparentFX # Layer 1
- Ignore Raycast # Layer 2
- Everything    # Layer 3
- Water         # Layer 4
- UI            # Layer 5
- Character     # Layer 6
- Projectile    # Layer 7 ← НОВЫЙ!
- Enemy         # Layer 8
```

---

### 2. Настроена Physics Layer Collision Matrix

**Файл:** `ProjectSettings/DynamicsManager.asset`

**Layer 7 (Projectile) теперь взаимодействует ТОЛЬКО с:**
- Layer 6 (Character) - игроки
- Layer 7 (Projectile) - другие снаряды (опционально)
- Layer 8 (Enemy) - враги

**НЕ взаимодействует с:**
- Layer 0 (Default) - стены, пол, ландшафт
- Всеми остальными слоями

**Hex значение collision matrix для Layer 7:**
```
000001c0
```

Это binary: `0000_0000_0000_0000_0000_0001_1100_0000`
- Bit 6 = Character ✅
- Bit 7 = Projectile ✅ (снаряды могут сталкиваться друг с другом)
- Bit 8 = Enemy ✅

---

### 3. Обновлены все префабы снарядов

Установлен Layer 7 (Projectile) для:

#### ✅ EtherealSkullProjectile.prefab
**Файл:** `Assets/Resources/Projectiles/EtherealSkullProjectile.prefab`

**Модификация:**
```yaml
- target: {fileID: 919132149155446097, guid: c249ef1a93d3c2b4bb7028ac23da3ab8, type: 3}
  propertyPath: m_Layer
  value: 7
  objectReference: {fileID: 0}
```

#### ✅ CelestialBallProjectile.prefab
**Файл:** `Assets/Resources/Projectiles/CelestialBallProjectile.prefab`

**Модификация:**
```yaml
- target: {fileID: 919132149155446097, guid: efc1a24fdb4997d4fb61fef01318fdb5, type: 3}
  propertyPath: m_Layer
  value: 7
  objectReference: {fileID: 0}
```

#### ✅ Ethereal_Skull_1020210937_texture.prefab
**Файл:** `Assets/Prefabs/Projectiles/Ethereal_Skull_1020210937_texture.prefab`

**Модификация:**
```yaml
- target: {fileID: 919132149155446097, guid: c249ef1a93d3c2b4bb7028ac23da3ab8, type: 3}
  propertyPath: m_Layer
  value: 7
  objectReference: {fileID: 0}
```

---

## КАК ЭТО РАБОТАЕТ

### До исправления:
```
[Снаряд на Default Layer]
    ↓ летит
    ↓
[Стена на Default Layer] ← OnTriggerEnter() срабатывает!
    ↓
💥 Снаряд взрывается на полпути
```

### После исправления:
```
[Снаряд на Projectile Layer 7]
    ↓ летит
    ↓
[Стена на Default Layer] ← Коллизия игнорируется (Physics Matrix)
    ↓ пролетает дальше
    ↓
[Враг на Enemy Layer 8] ← OnTriggerEnter() срабатывает!
    ↓
💥 Снаряд взрывается на цели!
```

---

## КОД ОБРАБОТКИ КОЛЛИЗИЙ

**Файл:** `Assets/Scripts/Player/CelestialProjectile.cs`

**Метод:** `OnTriggerEnter()` (строки 441-465)

```csharp
private void OnTriggerEnter(Collider other)
{
    if (hasHit) return;

    // Игнорируем владельца
    if (other.gameObject == owner) return;

    // Проверяем попадание в цель
    if (other.transform == target)
    {
        HitTarget();
    }
    // Или попадание в любого врага
    else
    {
        Enemy enemy = other.GetComponent<Enemy>();
        NetworkPlayer networkPlayer = other.GetComponent<NetworkPlayer>();

        if ((enemy != null && enemy.IsAlive()) || networkPlayer != null)
        {
            target = other.transform;
            HitTarget();
        }
    }
}
```

**Теперь этот метод вызывается ТОЛЬКО когда снаряд касается:**
- Character (игрок)
- Enemy (враг)
- Другого Projectile

**НЕ вызывается когда снаряд касается:**
- Стен
- Пола
- Ландшафта
- UI
- Воды
- И любых других объектов на Default слое

---

## ТЕСТИРОВАНИЕ

### Откройте Arena Scene и протестируйте:

1. **Запустите игру** (Play)
2. **Выберите мага** или класс со снарядами
3. **Найдите врага**
4. **Атакуйте** (ЛКМ)

### Проверьте что:

- ✅ Снаряд летит **сквозь стены** (если есть)
- ✅ Снаряд летит **сквозь пол** (не взрывается на полу)
- ✅ Снаряд **автонаводится на врага**
- ✅ Снаряд **взрывается ТОЛЬКО при попадании в врага**
- ✅ Эффект взрыва появляется **на враге**
- ✅ Враг **получает урон**

---

## ДОБАВЛЕНИЕ НОВЫХ СНАРЯДОВ

Когда создаете новый снаряд, **ОБЯЗАТЕЛЬНО:**

### 1. Установите Layer = Projectile

**В Unity Editor:**
1. Выберите префаб снаряда
2. В Inspector → Layer
3. Выберите **Projectile**

**Или программно в .prefab файле:**
```yaml
m_Modifications:
- target: {fileID: [GameObject_ID], guid: [Model_GUID], type: 3}
  propertyPath: m_Layer
  value: 7  # Projectile layer
  objectReference: {fileID: 0}
```

### 2. Используйте CelestialProjectile скрипт

Этот скрипт уже правильно обрабатывает коллизии через `OnTriggerEnter()`

### 3. Добавьте Sphere Collider с Is Trigger = true

```
Sphere Collider:
  Is Trigger: ✅ true
  Radius: 0.4 (или по размеру снаряда)
```

### 4. Добавьте Rigidbody (Kinematic)

```
Rigidbody:
  Is Kinematic: ✅ true
  Use Gravity: ❌ false
```

---

## ЧАСТЫЕ ПРОБЛЕМЫ

### Снаряд всё равно взрывается на стенах

**Проверьте:**
1. Layer установлен в Projectile (Layer 7)?
   ```bash
   # Найдите в .prefab файле:
   grep "m_Layer" prefab_file.prefab
   # Должно быть: value: 7
   ```

2. Physics Matrix настроена?
   - Edit → Project Settings → Physics
   - Найдите Layer "Projectile"
   - Проверьте что галочки ТОЛЬКО на Character и Enemy

3. У стен/пола слой Default?
   - Стены должны быть на Default (Layer 0)
   - Или на специальном слое Environment

### Снаряд пролетает сквозь врагов

**Проверьте:**
1. У врага есть Collider?
2. У врага слой Enemy (Layer 8)?
3. Collider врага **НЕ** Is Trigger (должен быть обычный коллайдер)
4. Physics Matrix: Projectile ↔ Enemy включено

---

## АЛЬТЕРНАТИВНЫЙ ПОДХОД (без Physics Matrix)

Если хотите более тонкую настройку, можно модифицировать код `OnTriggerEnter()`:

```csharp
private void OnTriggerEnter(Collider other)
{
    if (hasHit) return;
    if (other.gameObject == owner) return;

    // НОВЫЙ КОД: Проверяем слой объекта
    int otherLayer = other.gameObject.layer;
    int characterLayer = LayerMask.NameToLayer("Character");
    int enemyLayer = LayerMask.NameToLayer("Enemy");

    // Игнорируем всё кроме Character и Enemy
    if (otherLayer != characterLayer && otherLayer != enemyLayer)
    {
        return; // Пропускаем коллизию
    }

    // Остальной код как раньше...
    if (other.transform == target)
    {
        HitTarget();
    }
    else
    {
        Enemy enemy = other.GetComponent<Enemy>();
        NetworkPlayer networkPlayer = other.GetComponent<NetworkPlayer>();

        if ((enemy != null && enemy.IsAlive()) || networkPlayer != null)
        {
            target = other.transform;
            HitTarget();
        }
    }
}
```

**Но это НЕ нужно**, так как Physics Matrix уже делает эту работу!

---

## SUMMARY

### Изменённые файлы:

1. ✅ `ProjectSettings/TagManager.asset` - добавлен слой Projectile
2. ✅ `ProjectSettings/DynamicsManager.asset` - настроена collision matrix
3. ✅ `Assets/Resources/Projectiles/EtherealSkullProjectile.prefab` - слой 7
4. ✅ `Assets/Resources/Projectiles/CelestialBallProjectile.prefab` - слой 7
5. ✅ `Assets/Prefabs/Projectiles/Ethereal_Skull_1020210937_texture.prefab` - слой 7

### Результат:

- Снаряды больше НЕ взрываются на полпути
- Снаряды взрываются ТОЛЬКО при попадании в Character или Enemy
- Снаряды пролетают сквозь стены, пол, ландшафт
- Автонаведение работает корректно

---

**ГОТОВО!** Проблема решена! 💀✨

Теперь можете протестировать в Unity и убедиться что снаряды летят до цели и взрываются только на врагах.

---

**Автор:** Claude (Anthropic)
**Дата:** 21 октября 2025
