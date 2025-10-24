# Список всех 30 новых скиллов (SkillConfig) 📋

## Warrior (5 скиллов) ⚔️

| ID  | Имя скилла | Asset Path |
|-----|------------|------------|
| 101 | Battle Rage | Assets/Resources/Skills/Warrior_BattleRage.asset |
| 102 | Defensive Stance | Assets/Resources/Skills/Warrior_DefensiveStance.asset |
| 103 | Hammer Throw | Assets/Resources/Skills/Warrior_HammerThrow.asset |
| 104 | Battle Heal | Assets/Resources/Skills/Warrior_BattleHeal.asset |
| 105 | Charge | Assets/Resources/Skills/Warrior_Charge.asset |

---

## Mage (5 скиллов) 🔮

| ID  | Имя скилла | Asset Path |
|-----|------------|------------|
| 201 | Fireball | Assets/Resources/Skills/Mage_Fireball.asset |
| 202 | Ice Nova | Assets/Resources/Skills/Mage_IceNova.asset |
| 203 | Meteor | Assets/Resources/Skills/Mage_Meteor.asset |
| 204 | Teleport | Assets/Resources/Skills/Mage_Teleport.asset |
| 205 | Lightning Storm | Assets/Resources/Skills/Mage_LightningStorm.asset |

---

## Archer (5 скиллов) 🏹

| ID  | Имя скилла | Asset Path |
|-----|------------|------------|
| 301 | Rain of Arrows | Assets/Resources/Skills/Archer_RainOfArrows.asset |
| 302 | Stunning Shot | Assets/Resources/Skills/Archer_StunningShot.asset |
| 303 | Eagle Eye | Assets/Resources/Skills/Archer_EagleEye.asset |
| 304 | Swift Stride | Assets/Resources/Skills/Archer_SwiftStride.asset |
| 305 | Entangling Shot | Assets/Resources/Skills/Archer_EntanglingShot.asset |

---

## Rogue (5 скиллов) 🗡️

| ID  | Имя скилла | Asset Path |
|-----|------------|------------|
| 401 | Backstab | Assets/Resources/Skills/Rogue_Backstab.asset |
| 402 | Smoke Bomb | Assets/Resources/Skills/Rogue_SmokeBomb.asset |
| 403 | Shadow Step | Assets/Resources/Skills/Rogue_ShadowStep.asset |
| 404 | Poison Blade | Assets/Resources/Skills/Rogue_PoisonBlade.asset |
| 405 | Execute | Assets/Resources/Skills/Rogue_Execute.asset |

**ПРИМЕЧАНИЕ:** В текущей системе Rogue скиллы могут иметь другие имена файлов (Rogue был переименован в Necromancer).

---

## Necromancer (5 скиллов) 💀

| ID  | Имя скилла | Asset Path |
|-----|------------|------------|
| 601 | Summon Skeleton | Assets/Resources/Skills/Rogue_SummonSkeletons.asset |
| 602 | Soul Drain | Assets/Resources/Skills/Rogue_SoulDrain.asset |
| 603 | Curse of Weakness | Assets/Resources/Skills/Rogue_CurseOfWeakness.asset |
| 604 | Crippling Curse | Assets/Resources/Skills/Rogue_CripplingCurse.asset |
| 605 | Blood for Mana | Assets/Resources/Skills/Rogue_BloodForMana.asset |

**ПРИМЕЧАНИЕ:** Necromancer скиллы хранятся с префиксом "Rogue_" (старое имя класса).

---

## Paladin (5 скиллов) 🐻

| ID  | Имя скилла | Asset Path |
|-----|------------|------------|
| 501 | Bear Form | Assets/Resources/Skills/Paladin_BearForm.asset |
| 502 | Divine Protection | Assets/Resources/Skills/Paladin_DivineProtection.asset |
| 503 | Lay on Hands | Assets/Resources/Skills/Paladin_LayOnHands.asset |
| 504 | Divine Strength | Assets/Resources/Skills/Paladin_DivineStrength.asset |
| 505 | Holy Hammer | Assets/Resources/Skills/Paladin_HolyHammer.asset |

---

## Итоговая статистика

**Всего классов:** 6
**Всего скиллов:** 30
**По 5 скиллов на класс**

---

## Проверка существования assets

```bash
# Warrior
ls Assets/Resources/Skills/Warrior_*.asset

# Mage
ls Assets/Resources/Skills/Mage_*.asset

# Archer
ls Assets/Resources/Skills/Archer_*.asset

# Necromancer (Rogue)
ls Assets/Resources/Skills/Rogue_*.asset

# Paladin
ls Assets/Resources/Skills/Paladin_*.asset
```

---

## Загрузка скиллов в коде

```csharp
// Warrior
SkillConfig battleRage = Resources.Load<SkillConfig>("Skills/Warrior_BattleRage");
SkillConfig defensiveStance = Resources.Load<SkillConfig>("Skills/Warrior_DefensiveStance");
SkillConfig hammerThrow = Resources.Load<SkillConfig>("Skills/Warrior_HammerThrow");
SkillConfig battleHeal = Resources.Load<SkillConfig>("Skills/Warrior_BattleHeal");
SkillConfig charge = Resources.Load<SkillConfig>("Skills/Warrior_Charge");

// Mage
SkillConfig fireball = Resources.Load<SkillConfig>("Skills/Mage_Fireball");
SkillConfig iceNova = Resources.Load<SkillConfig>("Skills/Mage_IceNova");
SkillConfig meteor = Resources.Load<SkillConfig>("Skills/Mage_Meteor");
SkillConfig teleport = Resources.Load<SkillConfig>("Skills/Mage_Teleport");
SkillConfig lightningStorm = Resources.Load<SkillConfig>("Skills/Mage_LightningStorm");

// Archer
SkillConfig rainOfArrows = Resources.Load<SkillConfig>("Skills/Archer_RainOfArrows");
SkillConfig stunningShot = Resources.Load<SkillConfig>("Skills/Archer_StunningShot");
SkillConfig eagleEye = Resources.Load<SkillConfig>("Skills/Archer_EagleEye");
SkillConfig swiftStride = Resources.Load<SkillConfig>("Skills/Archer_SwiftStride");
SkillConfig entanglingShot = Resources.Load<SkillConfig>("Skills/Archer_EntanglingShot");

// Necromancer (файлы с префиксом Rogue_)
SkillConfig summonSkeleton = Resources.Load<SkillConfig>("Skills/Rogue_SummonSkeletons");
SkillConfig soulDrain = Resources.Load<SkillConfig>("Skills/Rogue_SoulDrain");
SkillConfig curseOfWeakness = Resources.Load<SkillConfig>("Skills/Rogue_CurseOfWeakness");
SkillConfig cripplingCurse = Resources.Load<SkillConfig>("Skills/Rogue_CripplingCurse");
SkillConfig bloodForMana = Resources.Load<SkillConfig>("Skills/Rogue_BloodForMana");

// Paladin
SkillConfig bearForm = Resources.Load<SkillConfig>("Skills/Paladin_BearForm");
SkillConfig divineProtection = Resources.Load<SkillConfig>("Skills/Paladin_DivineProtection");
SkillConfig layOnHands = Resources.Load<SkillConfig>("Skills/Paladin_LayOnHands");
SkillConfig divineStrength = Resources.Load<SkillConfig>("Skills/Paladin_DivineStrength");
SkillConfig holyHammer = Resources.Load<SkillConfig>("Skills/Paladin_HolyHammer");
```

---

## Mapping CharacterClass → Skill IDs

```csharp
public static Dictionary<string, int[]> ClassSkillIds = new Dictionary<string, int[]>()
{
    { "Warrior", new int[] { 101, 102, 103, 104, 105 } },
    { "Mage", new int[] { 201, 202, 203, 204, 205 } },
    { "Archer", new int[] { 301, 302, 303, 304, 305 } },
    { "Rogue", new int[] { 401, 402, 403, 404, 405 } }, // ЕСЛИ используется Rogue
    { "Necromancer", new int[] { 601, 602, 603, 604, 605 } }, // Necromancer
    { "Paladin", new int[] { 501, 502, 503, 504, 505 } }
};
```

---

## Следующие шаги

1. ✅ Проверить что все 30 assets существуют
2. ✅ Изменить SkillManager: использовать SkillConfig вместо SkillData
3. ✅ Изменить ArenaManager: загружать 5 скиллов по skillId
4. ✅ Обновить Character Selection (опционально)
5. ✅ Протестировать локально

---

**ГОТОВО К ИСПОЛЬЗОВАНИЮ!** 🚀
