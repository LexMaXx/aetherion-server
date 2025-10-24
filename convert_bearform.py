#!/usr/bin/env python3
"""
Конвертирует Paladin_BearForm.asset из старого SkillData в новый SkillConfig
"""

file_path = "Assets/Resources/Skills/Paladin_BearForm.asset"

# Читаем файл
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Заменяем GUID: старый SkillData → новый SkillConfig
old_guid = "6e79cfd8b12443c408c3d4a9fbdce0c8"
new_guid = "93ea6d4f751c12e48a5c2881809ebb04"

content = content.replace(old_guid, new_guid)

# Заменяем m_Name: BearForm → Paladin_BearForm (для консистентности)
content = content.replace("m_Name: BearForm", "m_Name: Paladin_BearForm")

# Исправляем skillId: 401 → 501 (первый скилл паладина)
content = content.replace("skillId: 401", "skillId: 501")

# Добавляем отсутствующие поля SkillConfig после строки intelligenceScaling
# Находим место для вставки
lines = content.split('\n')
new_lines = []

for i, line in enumerate(lines):
    new_lines.append(line)

    # После intelligenceScaling добавляем недостающие поля SkillConfig
    if line.strip() == "intelligenceScaling: 0" and i < len(lines) - 1:
        # Проверяем, что следующая строка - это effects (значит пропущены поля)
        if "effects:" in lines[i + 1]:
            # Вставляем пропущенные поля
            new_lines.append("  lifeStealPercent: 0")

# Объединяем обратно
content = '\n'.join(new_lines)

# Добавляем отсутствующие поля SkillConfig в конец файла (если их нет)
required_fields = [
    "projectilePrefab:",
    "projectileSpeed:",
    "projectileLifetime:",
    "projectileHoming:",
    "homingSpeed:",
    "homingRadius:",
    "castEffectPrefab:",
    "hitEffectPrefab:",
    "aoeEffectPrefab:",
    "casterEffectPrefab:",
    "animationSpeed:",
    "projectileSpawnTiming:",
    "hitSound:",
    "soundVolume:",
    "enableMovement:",
    "movementType:",
    "movementDistance:",
    "movementSpeed:",
    "movementDirection:",
    "damageBonusPercent:",
    "syncProjectiles:",
    "syncHitEffects:",
    "syncStatusEffects:",
    "customData:"
]

# Проверяем какие поля отсутствуют
missing_fields = [field for field in required_fields if field not in content]

if missing_fields:
    # Добавляем недостающие поля перед последней пустой строкой
    additions = []

    if "projectilePrefab:" not in content:
        additions.extend([
            "  projectilePrefab: {fileID: 0}",
            "  projectileSpeed: 20",
            "  projectileLifetime: 5",
            "  projectileHoming: 1",
            "  homingSpeed: 10",
            "  homingRadius: 20"
        ])

    if "castEffectPrefab:" not in content:
        additions.extend([
            "  castEffectPrefab: {fileID: 0}",
            "  hitEffectPrefab: {fileID: 0}",
            "  aoeEffectPrefab: {fileID: 0}",
            "  casterEffectPrefab: {fileID: 0}"
        ])

    if "animationSpeed:" not in content:
        additions.extend([
            "  animationSpeed: 1",
            "  projectileSpawnTiming: 0.5"
        ])

    if "hitSound:" not in content:
        additions.extend([
            "  hitSound: {fileID: 0}",
            "  soundVolume: 0.8"
        ])

    if "enableMovement:" not in content:
        additions.extend([
            "  enableMovement: 0",
            "  movementType: 0",
            "  movementDistance: 5",
            "  movementSpeed: 10",
            "  movementDirection: 0"
        ])

    if "damageBonusPercent:" not in content:
        additions.append("  damageBonusPercent: 30")

    if "syncProjectiles:" not in content:
        additions.extend([
            "  syncProjectiles: 0",
            "  syncHitEffects: 1",
            "  syncStatusEffects: 1"
        ])

    if "customData:" not in content:
        additions.extend([
            "  customData:",
            "    chainCount: 0",
            "    chainRadius: 8",
            "    chainDamageMultiplier: 0.7",
            "    hitCount: 1",
            "    hitDelay: 0.1",
            "    manaRestorePercent: 0",
            "    piercing: 0",
            "    maxPierceTargets: 3"
        ])

    # Добавляем все недостающие поля
    content = content.rstrip('\n') + '\n' + '\n'.join(additions) + '\n'

# Сохраняем
with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print(f"✅ Файл {file_path} успешно конвертирован!")
print(f"   - GUID изменён: {old_guid} → {new_guid}")
print(f"   - skillId исправлен: 401 → 501")
print(f"   - m_Name исправлен: BearForm → Paladin_BearForm")
print(f"   - Добавлены недостающие поля SkillConfig")
