# 📋 Таблица соответствия Skill ID → Файлы

## Полная таблица всех 25 скиллов

| ID  | Класс   | Файл | Название скилла |
|-----|---------|------|-----------------|
| 101 | Warrior | Warrior_BattleRage.asset | Battle Rage |
| 102 | Warrior | Warrior_DefensiveStance.asset | Defensive Stance |
| 103 | Warrior | Warrior_HammerThrow.asset | Hammer Throw |
| 104 | Warrior | Warrior_BattleHeal.asset | Battle Heal |
| 105 | Warrior | Warrior_Charge.asset | Charge |
| 201 | Mage    | Mage_Fireball.asset | Fireball |
| 202 | Mage    | Mage_IceNova.asset | Ice Nova |
| 203 | Mage    | Mage_Meteor.asset | Meteor |
| 204 | Mage    | Mage_Teleport.asset | Teleport |
| 205 | Mage    | Mage_LightningStorm.asset | Lightning Storm |
| 301 | Archer  | Archer_RainOfArrows.asset | Rain Of Arrows |
| 302 | Archer  | Archer_StunningShot.asset | Stunning Shot |
| 303 | Archer  | Archer_EagleEye.asset | Eagle Eye |
| 304 | Archer  | Archer_SwiftStride.asset | Swift Stride |
| 305 | Archer  | Archer_DeadlyPrecision.asset | Deadly Precision |
| 501 | Paladin | Paladin_BearForm.asset | Bear Form |
| 502 | Paladin | Paladin_DivineProtection.asset | Divine Protection |
| 503 | Paladin | Paladin_LayOnHands.asset | Lay On Hands |
| 504 | Paladin | Paladin_DivineStrength.asset | Divine Strength |
| 505 | Paladin | Paladin_HolyHammer.asset | Holy Hammer |
| 601 | Rogue   | Rogue_RaiseDead.asset | Raise Dead |
| 602 | Rogue   | Rogue_SoulDrain.asset | Soul Drain |
| 603 | Rogue   | Rogue_CurseOfWeakness.asset | Curse Of Weakness |
| 604 | Rogue   | Rogue_CripplingCurse.asset | Crippling Curse |
| 605 | Rogue   | Rogue_BloodForMana.asset | Blood For Mana |

## Схема распределения ID

```
┌─────────────────────────────────────────────────────────────────┐
│                        SKILL ID RANGES                          │
├─────────────┬─────────────┬─────────────┬─────────────┬─────────┤
│  WARRIOR    │    MAGE     │   ARCHER    │  PALADIN    │  ROGUE  │
│  101-105    │  201-205    │  301-305    │  501-505    │ 601-605 │
└─────────────┴─────────────┴─────────────┴─────────────┴─────────┘
```

## Правила именования ID

- **Warrior**: 101-105 (первый класс, первая сотня)
- **Mage**: 201-205 (второй класс, вторая сотня)
- **Archer**: 301-305 (третий класс, третья сотня)
- **Paladin**: 501-505 (пятый класс, пятая сотня)
- **Rogue**: 601-605 (шестой класс, шестая сотня)

> Примечание: ID 401-405 НЕ используются (пропущены)

## Для разработчиков

При создании нового скилла:
1. Определи класс
2. Найди свободный ID в диапазоне класса
3. Добавь mapping в `SkillConfigLoader.cs`:
   ```csharp
   { NEW_ID, "Skills/ClassName_SkillName" }
   ```
4. Создай .asset файл с `skillId: NEW_ID`

## Проверка целостности

Используй эту команду для проверки всех ID:
```bash
cd Assets/Resources/Skills
for file in *.asset; do
    echo "$file: $(grep 'skillId:' "$file" | awk '{print $2}')"
done | sort
```

Должно быть ровно **25 строк** без дубликатов!
