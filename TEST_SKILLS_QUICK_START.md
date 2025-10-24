# 🎮 БЫСТРЫЙ СТАРТ - Тестирование скиллов

## ✅ Шаг 1: Настроить тестовую сцену (автоматически)

Откройте Unity Editor и выполните:

```
Tools → Skills → Setup Skill Test Scene
```

Это автоматически:
- ✅ Откроет SkillTestScene.unity
- ✅ Создаст TestPlayer с всеми компонентами
- ✅ Добавит SkillExecutor и EffectManager
- ✅ Настроит камеру
- ✅ Создаст визуал игрока (синяя капсула = маг)

---

## ✅ Шаг 2: Создать скилл Fireball

```
Tools → Skills → Create Mage Fireball
```

Это создаст: `Assets/ScriptableObjects/Skills/Mage/Mage_Fireball.asset`

---

## ✅ Шаг 3: Добавить скилл к TestPlayer

1. В Hierarchy выберите **TestPlayer**
2. В Inspector найдите **Skill Executor (Script)**
3. В секции **Equipped Skills**:
   ```
   Size: 3
   Element 0: <перетащите Mage_Fireball сюда>
   Element 1: None
   Element 2: None
   ```

---

## ✅ Шаг 4: Запустить тестирование

1. **Нажмите Play** ▶️
2. **Управление:**
   - **WASD** - Движение
   - **ЛКМ** - Выбрать ближайшего врага
   - **1** - Использовать Fireball
   - **H** - Показать справку

3. **Тестирование:**
   - Подойдите к DummyEnemy
   - Нажмите **ЛКМ** (выбрать врага)
   - Нажмите **1** (Fireball) 🔥

---

## ✅ Что должно произойти:

1. ✅ Анимация каста (Attack)
2. ✅ Мана -30
3. ✅ Огненный шар летит к врагу
4. ✅ Взрыв при попадании
5. ✅ Урон ~300
6. ✅ Эффект Burn (5 сек, 60 урона/сек)
7. ✅ Кулдаун 6 секунд

**Console логи:**
```
[SimplePlayerController] 🎯 Цель: DummyEnemy_1 (дистанция: 5.2м)
[SimplePlayerController] 🔥 Попытка использовать скилл в слоте 0
[SkillExecutor] 🎯 Using skill: Fireball (slot 0)
[SkillExecutor] ✅ Executing ProjectileDamage: Fireball
[SkillExecutor] 🚀 Создан снаряд: CelestialBallProjectile, урон: 300
[CelestialProjectile] ⚡ Projectile hit Enemy!
[EffectManager] ✨ Applied effect: Burn (5.0s)
[EffectManager] 🔥 Burn tick: 60 damage
```

---

## 🐛 Возможные проблемы:

### Ошибка: "Mage_Fireball не найден"
**Решение:** Сначала создайте скилл через Tools → Skills → Create Mage Fireball

### Ошибка: "Skill slot 0 is empty"
**Решение:** Добавьте Mage_Fireball в Equipped Skills[0]

### Ошибка: "No target selected"
**Решение:** Нажмите ЛКМ чтобы выбрать врага перед использованием скилла

### Снаряд не летит
**Решение:**
- Проверьте, что CelestialBallProjectile.prefab существует
- Проверьте Console на ошибки

### DummyEnemy не получает урон
**Решение:**
- Убедитесь, что у DummyEnemy есть компонент HealthSystem
- Проверьте Console на логи "[HealthSystem]"

---

## 📊 Структура TestPlayer:

```
TestPlayer
├─ CharacterController (движение)
├─ CharacterStats (статы: 100 INT, 50 STR)
├─ HealthSystem (1000 HP)
├─ ManaSystem (500 MP)
├─ Animator (анимации)
├─ SkillExecutor (новая система скиллов) ⭐
├─ EffectManager (эффекты) ⭐
├─ PlayerAttackNew (интеграция)
├─ SimplePlayerController (управление)
├─ Visual (синяя капсула)
└─ ProjectileSpawnPoint (точка спавна снарядов)
```

---

## 🎯 Что тестировать:

### Базовая функциональность:
- ✅ Создание снаряда
- ✅ Полёт снаряда к цели
- ✅ Хоминг (наведение на цель)
- ✅ Попадание и взрыв
- ✅ Нанесение урона

### Система эффектов:
- ✅ Применение Burn эффекта
- ✅ Тикающий урон (каждую секунду)
- ✅ Длительность эффекта (5 секунд)
- ✅ Визуальный эффект горения

### Система ограничений:
- ✅ Кулдаун (нельзя использовать 6 секунд)
- ✅ Расход маны (-30)
- ✅ Дистанция (максимум 25м)
- ✅ Требование цели

---

## 🚀 После успешного теста:

1. ✅ Протестировать в ArenaScene (онлайн)
2. ✅ Создать Ice Nova (AOE скилл)
3. ✅ Создать Lightning Strike (Instant + Stun)
4. ✅ Добавить UI для скиллов

---

**🎮 ГОТОВО! Запускайте Unity и тестируйте Fireball!**

**Шаги:**
1. Tools → Skills → Setup Skill Test Scene
2. Tools → Skills → Create Mage Fireball
3. TestPlayer → SkillExecutor → Equipped Skills[0] = Mage_Fireball
4. Play → ЛКМ (выбрать врага) → 1 (Fireball) 🔥
