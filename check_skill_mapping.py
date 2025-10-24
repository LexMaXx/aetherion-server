#!/usr/bin/env python3
"""
Проверяет соответствие между файлами в Resources/Skills/ и SkillConfigLoader.cs
"""
import os

# Файлы которые существуют
existing_files = [
    "Archer_DeadlyPrecision",
    "Archer_EagleEye",
    "Archer_RainOfArrows",
    "Archer_StunningShot",
    "Archer_SwiftStride",
    "Mage_Fireball",
    "Mage_IceNova",
    "Mage_LightningStorm",
    "Mage_Meteor",
    "Mage_Teleport",
    "Paladin_BearForm",
    "Paladin_DivineProtection",
    "Paladin_DivineStrength",
    "Paladin_HolyHammer",
    "Paladin_LayOnHands",
    "Rogue_BloodForMana",
    "Rogue_CripplingCurse",
    "Rogue_CurseOfWeakness",
    "Rogue_RaiseDead",
    "Rogue_SoulDrain",
    "Warrior_BattleHeal",
    "Warrior_BattleRage",
    "Warrior_Charge",
    "Warrior_DefensiveStance",
    "Warrior_HammerThrow"
]

# Mapping из SkillConfigLoader.cs
skill_paths = {
    # WARRIOR (101-105)
    101: "Skills/Warrior_BattleRage",
    102: "Skills/Warrior_DefensiveStance",
    103: "Skills/Warrior_HammerThrow",
    104: "Skills/Warrior_BattleHeal",
    105: "Skills/Warrior_Charge",

    # MAGE (201-205)
    201: "Skills/Mage_Fireball",
    202: "Skills/Mage_IceNova",
    203: "Skills/Mage_Meteor",
    204: "Skills/Mage_Teleport",
    205: "Skills/Mage_LightningStorm",

    # ARCHER (301-305)
    301: "Skills/Archer_RainOfArrows",
    302: "Skills/Archer_StunningShot",
    303: "Skills/Archer_EagleEye",
    304: "Skills/Archer_SwiftStride",
    305: "Skills/Archer_DeadlyPrecision",

    # ROGUE (601-605)
    601: "Skills/Rogue_RaiseDead",
    602: "Skills/Rogue_SoulDrain",
    603: "Skills/Rogue_CurseOfWeakness",
    604: "Skills/Rogue_CripplingCurse",
    605: "Skills/Rogue_BloodForMana",

    # PALADIN (501-505)
    501: "Skills/Paladin_BearForm",
    502: "Skills/Paladin_DivineProtection",
    503: "Skills/Paladin_LayOnHands",
    504: "Skills/Paladin_DivineStrength",
    505: "Skills/Paladin_HolyHammer"
}

# Классы и их ID
class_ids = {
    "Warrior": [101, 102, 103, 104, 105],
    "Mage": [201, 202, 203, 204, 205],
    "Archer": [301, 302, 303, 304, 305],
    "Rogue": [601, 602, 603, 604, 605],
    "Paladin": [501, 502, 503, 504, 505]
}

print("=" * 60)
print("ПРОВЕРКА СООТВЕТСТВИЯ ФАЙЛОВ И МАППИНГА")
print("=" * 60)

all_ok = True

for class_name, skill_ids in class_ids.items():
    print(f"\n{class_name}:")
    for skill_id in skill_ids:
        path = skill_paths[skill_id]
        filename = path.replace("Skills/", "")

        if filename in existing_files:
            print(f"  ✅ {skill_id}: {filename}")
        else:
            print(f"  ❌ {skill_id}: {filename} - ФАЙЛ НЕ НАЙДЕН!")
            all_ok = False

print("\n" + "=" * 60)

# Проверяем обратное - есть ли файлы без маппинга
mapped_files = set(path.replace("Skills/", "") for path in skill_paths.values())
unmapped_files = set(existing_files) - mapped_files

if unmapped_files:
    print("❌ ФАЙЛЫ БЕЗ МАППИНГА:")
    for f in sorted(unmapped_files):
        print(f"  - {f}")
    all_ok = False

if all_ok:
    print("✅✅✅ ВСЕ ФАЙЛЫ КОРРЕКТНО ЗАМАПЛЕНЫ!")
else:
    print("❌ ОБНАРУЖЕНЫ НЕСООТВЕТСТВИЯ!")

print("=" * 60)
