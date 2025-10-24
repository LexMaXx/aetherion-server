# 🔥 Быстрая настройка Fireball

## ✅ 2 КОМАНДЫ - И ВСЁ РАБОТАЕТ!

### Шаг 1: Скопировать настройки с CelestialBall
```
Unity → Tools → Skills → Copy CelestialBall Setup to Fireball
```

**Что произойдёт:**
- ✅ На **Fireball.prefab** скопируются все компоненты
- ✅ CelestialProjectile (скрипт полёта)
- ✅ Rigidbody, SphereCollider (физика)
- ✅ TrailRenderer, Light, Particles (эффекты)
- ✅ Layer = Projectile (7)

---

### Шаг 2: Создать/Обновить скилл Mage_Fireball

**Если скилл НЕ создан:**
```
Unity → Tools → Skills → Create Mage Fireball
```
(Автоматически будет использовать Fireball.prefab)

**Если скилл УЖЕ создан:**
1. Откройте `Mage_Fireball.asset`
2. **Projectile Prefab** → перетащите **Fireball** (из Assets/Prefabs/Projectiles/)

---

### Шаг 3: Тест
1. **Play** ▶️ в SkillTestScene
2. **ЛКМ** - выбрать врага
3. **1** - Fireball 🔥

**Результат:** Красный огненный шар летит к врагу! 🚀

---

## 📦 Файлы:

- **Префаб:** `Assets/Prefabs/Projectiles/Fireball.prefab`
- **SkillConfig:** `Assets/ScriptableObjects/Skills/Mage/Mage_Fireball.asset`

---

## 🎯 Итог:

```
1. Tools → Skills → Copy CelestialBall Setup to Fireball
2. Tools → Skills → Create Mage Fireball (или обновить вручную)
3. Play → ЛКМ → 1 🔥
```

**Готово!** ✅
