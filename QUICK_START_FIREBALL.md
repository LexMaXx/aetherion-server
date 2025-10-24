# 🔥 БЫСТРЫЙ СТАРТ - Mage Fireball

## 📋 Шаг 1: Создать скилл (Unity Editor)

```
Tools → Skills → Create Mage Fireball
```

✅ **Результат:** Создан файл `Assets/ScriptableObjects/Skills/Mage/Mage_Fireball.asset`

---

## 📋 Шаг 2: Добавить к персонажу

1. Откройте сцену: `Assets/Scenes/Arena.unity`
2. Найдите в Hierarchy: `LocalPlayer`
3. В Inspector → `Skill Executor (Script)`:
   ```
   Equipped Skills
     Size: 3
     Element 0: <перетащите Mage_Fireball>
     Element 1: None
     Element 2: None
   ```

---

## 📋 Шаг 3: Запустить и протестировать

1. **Play** ▶️
2. **Enter Game** → Выбрать Мага → Войти в арену
3. **Выбрать врага** (ЛКМ)
4. **Нажать "1"** 🔥

---

## ✅ Ожидаемый результат:

- Анимация каста
- Мана -30
- Огненный шар летит к цели
- Взрыв при попадании
- Урон ~300
- Эффект Burn (5 сек, 60 урона/сек)
- Кулдаун 6 сек

---

## 📊 Параметры скилла:

```
Урон: 50 + Intelligence*2.5 (при 100 INT = 300)
Burn: 10 + Intelligence*0.5 за тик (при 100 INT = 60/сек)
Общий урон: 600 (300 прямой + 300 DoT)
Кулдаун: 6 сек
Мана: 30
Дистанция: 25м
```

---

**🎮 Готово! Запускайте Unity и создавайте Fireball!**
