# Инструкция: Создание Celestial Ball Projectile для базовой атаки мага

## Шаг 1: Создать префаб CelestialBallProjectile

1. **Откройте Unity Editor**

2. **Скопируйте FireballProjectile**:
   - В Project окне найдите: `Assets/Prefabs/Projectiles/FireballProjectile.prefab`
   - ПКМ → Duplicate
   - Переименуйте в `CelestialBallProjectile`

3. **Откройте префаб для редактирования**:
   - Двойной клик по `CelestialBallProjectile.prefab`

## Шаг 2: Заменить визуальную модель

1. **Удалите старый визуальный объект**:
   - В Hierarchy найдите дочерний объект с визуальной моделью (сфера/fireball)
   - Удалите его

2. **Добавьте Celestial_Swirl_ball**:
   - Перетащите `Assets/Prefabs/Projectiles/Celestial_Swirl_ball_1019215156_texture.fbx` в CelestialBallProjectile как дочерний объект
   - Установите Transform → Position: (0, 0, 0)
   - Установите Transform → Scale: (0.5, 0.5, 0.5) или по вкусу

## Шаг 3: Настроить свечение (Light)

1. **В корневом объекте CelestialBallProjectile найдите компонент Light**:
   - Color: RGB(0.3, 0.7, 1.0) - голубой цвет
   - Intensity: 3-4
   - Range: 4-5

## Шаг 4: Настроить Trail (синий ветер)

1. **Найдите компонент TrailRenderer** (или создайте его: Add Component → Effects → Trail Renderer):
   - Time: 0.5
   - Width: установите кривую от 0.3 до 0.05
   - Color Gradient:
     - Start: RGB(0.5, 0.8, 1.0) Alpha=1.0 - светло-голубой
     - End: RGB(0.2, 0.5, 1.0) Alpha=0.0 - темно-синий прозрачный
   - Material: найдите подходящий trail material или используйте существующий

2. **Опционально - добавить ветровой эффект**:
   - Можно добавить Particle System с небольшими голубыми частицами позади шара

## Шаг 5: Настроить компонент Projectile

1. **В Inspector найдите скрипт Projectile**:
   - **Speed**: 25-30 (быстрее чем фаербол)
   - **Lifetime**: 5
   - **Homing**: 1 (ключевое! Автонаведение включено)
   - **Hit Effect**: перетащите `Assets/Resources/Effects/CFXR Magic Poof.prefab`
   - **Rotation Speed**: 360-540 (вращение шара)

## Шаг 6: Настроить коллайдер

- **SphereCollider**:
  - Is Trigger: ✅
  - Radius: 0.4-0.5

## Шаг 7: Сохранить префаб

- Нажмите Save в верхней части окна Hierarchy
- Закройте режим редактирования префаба

## Шаг 8: Обновить базовую атаку мага

Теперь нужно найти где мага базовая атака использует FireballProjectile и заменить на CelestialBallProjectile.

### Вариант 1: Если есть ScriptableObject для базовой атаки

- Найдите в `Assets/Resources/Skills/` файл базовой атаки мага
- В Inspector найдите поле `Projectile Prefab`
- Перетащите `CelestialBallProjectile` вместо `FireballProjectile`

### Вариант 2: Если базовая атака задана в коде

Мне нужно будет найти и обновить код. Подскажите где именно базовая атака мага определяется!

## Визуальный результат

✅ Голубой светящийся Celestial Swirl Ball
✅ Синий ветровой след (trail) позади шара
✅ Автонаведение на врага (homing = 1)
✅ Вращение шара в полёте
✅ Магический взрыв при попадании (CFXR Magic Poof)
✅ Синхронизация в мультиплеере (уже работает через Projectile.cs)

## Дополнительные настройки (опционально)

### Добавить голубое свечение материалу:

1. Создайте новый Material в Project
2. Shader → Universal Render Pipeline → Lit
3. Base Map: белый
4. Emission: включите ✅
5. Emission Color: RGB(0.5, 0.8, 1.0) - голубой
6. Emission Intensity: 2-3
7. Примените материал к Celestial_Swirl_ball модели

---

**После настройки префаба - дайте знать, я помогу обновить базовую атаку мага!**
