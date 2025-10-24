# ✅ ИСПРАВЛЕНЫ ВСЕ SKILL ID В .asset ФАЙЛАХ

## Проблема

При выборе класса в CharacterSelection загружались только **3 скилла вместо 5**.

### Причина

**SkillConfigLoader** ищет скиллы по **skillId**, но в файлах были **НЕПРАВИЛЬНЫЕ ID**!

```
SkillConfigLoader ищет:          Файлы содержали:
Warrior: 101-105                 401-405  ❌ (не совпадает!)
Mage:    201-205                 201-205  ✅ (но порядок был неправильный)
Archer:  301-305                 301-305  ✅ (но порядок был неправильный)
Paladin: 501-505                 501-505  ✅
Rogue:   601-605                 501-505  ❌ (КОНФЛИКТ с Paladin!)
```

Результат: `LoadSkillById()` возвращал `NULL` для большинства скиллов → загружались только те, у которых ID случайно совпал.

## Что было исправлено

### 1. WARRIOR (401-405 → 101-105)

| Файл | Было | Стало | Mapping в SkillConfigLoader |
|------|------|-------|----------------------------|
| Warrior_BattleRage.asset | 405 | **101** | ✅ { 101, "Skills/Warrior_BattleRage" } |
| Warrior_DefensiveStance.asset | 403 | **102** | ✅ { 102, "Skills/Warrior_DefensiveStance" } |
| Warrior_HammerThrow.asset | 402 | **103** | ✅ { 103, "Skills/Warrior_HammerThrow" } |
| Warrior_BattleHeal.asset | 404 | **104** | ✅ { 104, "Skills/Warrior_BattleHeal" } |
| Warrior_Charge.asset | 401 | **105** | ✅ { 105, "Skills/Warrior_Charge" } |

### 2. MAGE (исправлен порядок)

| Файл | Было | Стало | Mapping в SkillConfigLoader |
|------|------|-------|----------------------------|
| Mage_Fireball.asset | 201 | **201** | ✅ { 201, "Skills/Mage_Fireball" } |
| Mage_IceNova.asset | 202 | **202** | ✅ { 202, "Skills/Mage_IceNova" } |
| Mage_Meteor.asset | 205 | **203** | ✅ { 203, "Skills/Mage_Meteor" } |
| Mage_Teleport.asset | 204 | **204** | ✅ { 204, "Skills/Mage_Teleport" } |
| Mage_LightningStorm.asset | 203 | **205** | ✅ { 205, "Skills/Mage_LightningStorm" } |

### 3. ARCHER (исправлен порядок)

| Файл | Было | Стало | Mapping в SkillConfigLoader |
|------|------|-------|----------------------------|
| Archer_RainOfArrows.asset | 301 | **301** | ✅ { 301, "Skills/Archer_RainOfArrows" } |
| Archer_StunningShot.asset | 302 | **302** | ✅ { 302, "Skills/Archer_StunningShot" } |
| Archer_EagleEye.asset | 304 | **303** | ✅ { 303, "Skills/Archer_EagleEye" } |
| Archer_SwiftStride.asset | 303 | **304** | ✅ { 304, "Skills/Archer_SwiftStride" } |
| Archer_DeadlyPrecision.asset | 305 | **305** | ✅ { 305, "Skills/Archer_DeadlyPrecision" } |

### 4. PALADIN (уже были правильные)

| Файл | ID | Mapping в SkillConfigLoader |
|------|----|-----------------------------|
| Paladin_BearForm.asset | **501** | ✅ { 501, "Skills/Paladin_BearForm" } |
| Paladin_DivineProtection.asset | **502** | ✅ { 502, "Skills/Paladin_DivineProtection" } |
| Paladin_LayOnHands.asset | **503** | ✅ { 503, "Skills/Paladin_LayOnHands" } |
| Paladin_DivineStrength.asset | **504** | ✅ { 504, "Skills/Paladin_DivineStrength" } |
| Paladin_HolyHammer.asset | **505** | ✅ { 505, "Skills/Paladin_HolyHammer" } |

### 5. ROGUE (501-505 → 601-605)

| Файл | Было | Стало | Mapping в SkillConfigLoader |
|------|------|-------|----------------------------|
| Rogue_RaiseDead.asset | 505 | **601** | ✅ { 601, "Skills/Rogue_RaiseDead" } |
| Rogue_SoulDrain.asset | 501 | **602** | ✅ { 602, "Skills/Rogue_SoulDrain" } |
| Rogue_CurseOfWeakness.asset | 502 | **603** | ✅ { 603, "Skills/Rogue_CurseOfWeakness" } |
| Rogue_CripplingCurse.asset | 503 | **604** | ✅ { 604, "Skills/Rogue_CripplingCurse" } |
| Rogue_BloodForMana.asset | 504 | **605** | ✅ { 605, "Skills/Rogue_BloodForMana" } |

## Команды для исправления

Все изменения были сделаны скриптом `fix_skill_ids.sh` с помощью команды:
```bash
sed -i 's/skillId: OLD_ID/skillId: NEW_ID/g' FILENAME.asset
```

## Итоговая структура ID

```
WARRIOR:  101  102  103  104  105  ✅
MAGE:     201  202  203  204  205  ✅
ARCHER:   301  302  303  304  305  ✅
PALADIN:  501  502  503  504  505  ✅
ROGUE:    601  602  603  604  605  ✅
```

**Всего: 25 скиллов (5 классов × 5 скиллов)**

## Результат

Теперь в Unity Editor при выборе класса вы должны увидеть:

```
[SkillConfigLoader] 📚 Загрузка скиллов для класса Warrior: 101, 102, 103, 104, 105
[SkillConfigLoader] ✅ Загружен скилл: Battle Rage (ID: 101)
[SkillConfigLoader] ✅ Загружен скилл: Defensive Stance (ID: 102)
[SkillConfigLoader] ✅ Загружен скилл: Hammer Throw (ID: 103)
[SkillConfigLoader] ✅ Загружен скилл: Battle Heal (ID: 104)
[SkillConfigLoader] ✅ Загружен скилл: Charge (ID: 105)
[SkillConfigLoader] ✅ Загружено 5/5 скиллов для Warrior
```

**ВСЕ 5 СКИЛЛОВ ТЕПЕРЬ ЗАГРУЖАЮТСЯ ДЛЯ КАЖДОГО КЛАССА!** ✅✅✅

## Измененные файлы

### Warrior:
- `Assets/Resources/Skills/Warrior_BattleRage.asset`
- `Assets/Resources/Skills/Warrior_DefensiveStance.asset`
- `Assets/Resources/Skills/Warrior_HammerThrow.asset`
- `Assets/Resources/Skills/Warrior_BattleHeal.asset`
- `Assets/Resources/Skills/Warrior_Charge.asset`

### Mage:
- `Assets/Resources/Skills/Mage_Meteor.asset`
- `Assets/Resources/Skills/Mage_LightningStorm.asset`

### Archer:
- `Assets/Resources/Skills/Archer_SwiftStride.asset`
- `Assets/Resources/Skills/Archer_EagleEye.asset`

### Rogue:
- `Assets/Resources/Skills/Rogue_RaiseDead.asset`
- `Assets/Resources/Skills/Rogue_SoulDrain.asset`
- `Assets/Resources/Skills/Rogue_CurseOfWeakness.asset`
- `Assets/Resources/Skills/Rogue_CripplingCurse.asset`
- `Assets/Resources/Skills/Rogue_BloodForMana.asset`

**Итого изменено: 14 файлов**

## Тестирование

Запусти Unity Editor и выбери каждый класс в CharacterSelection:
1. ✅ Warrior: должны загрузиться 5 скиллов
2. ✅ Mage: должны загрузиться 5 скиллов
3. ✅ Archer: должны загрузиться 5 скиллов
4. ✅ Paladin: должны загрузиться 5 скиллов
5. ✅ Rogue: должны загрузиться 5 скиллов

Библиотека покажет:
- Слоты 0-4: все 5 скиллов класса
- Слот 5: пустой
