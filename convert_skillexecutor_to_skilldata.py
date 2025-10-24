#!/usr/bin/env python3
"""
Script to convert SkillExecutor.cs from using SkillConfig to SkillData
"""

import re

file_path = r"Assets/Scripts/Skills/SkillExecutor.cs"

# Read the file
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace all method parameters: SkillConfig → SkillData
content = re.sub(r'\(SkillConfig skill', r'(SkillData skill', content)

# Replace enum types in switch statement:
# SkillConfigType → SkillType mappings:
enum_mappings = [
    (r'case SkillConfigType\.ProjectileDamage:', r'case SkillType.Damage:'),
    (r'case SkillConfigType\.DamageAndHeal:', r'case SkillType.Damage: // DamageAndHeal (Soul Drain)'),
    (r'case SkillConfigType\.AOEDamage:', r'case SkillType.Damage: // AOE'),
    (r'case SkillConfigType\.Movement:', r'case SkillType.Teleport:'),
    (r'case SkillConfigType\.Buff:', r'case SkillType.Buff:'),
    (r'case SkillConfigType\.Heal:', r'case SkillType.Heal:'),
    (r'case SkillConfigType\.Summon:', r'case SkillType.Summon:'),
    (r'case SkillConfigType\.Transformation:', r'case SkillType.Transformation:'),
]

for old_enum, new_enum in enum_mappings:
    content = re.sub(old_enum, new_enum, content)

# Write back
with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("✅ SkillExecutor.cs converted from SkillConfig to SkillData")
print("✅ All method parameters updated")
print("✅ Enum types in switch statement updated")
print("\n⚠️ IMPORTANT: This script does basic text replacement.")
print("   You may need to adjust the enum mappings manually,")
print("   since SkillType (old) and SkillConfigType (new) have different values.")
