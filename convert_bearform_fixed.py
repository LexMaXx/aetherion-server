#!/usr/bin/env python3
"""
Конвертирует Paladin_BearForm.asset из старого SkillData в новый SkillConfig
СОХРАНЯЯ ВСЮ МЕХАНИКУ трансформации
"""

file_path = "Assets/Resources/Skills/Paladin_BearForm.asset"

# Читаем файл
with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

# Обрабатываем построчно
new_lines = []
for line in lines:
    # 1. Заменяем GUID: старый SkillData → новый SkillConfig
    if "guid: 6e79cfd8b12443c408c3d4a9fbdce0c8" in line:
        line = line.replace("6e79cfd8b12443c408c3d4a9fbdce0c8", "93ea6d4f751c12e48a5c2881809ebb04")
        print("✅ GUID изменён на SkillConfig")

    # 2. Исправляем m_Name для консистентности
    if "m_Name: BearForm" in line:
        line = line.replace("m_Name: BearForm", "m_Name: Paladin_BearForm")
        print("✅ m_Name изменён: BearForm → Paladin_BearForm")

    # 3. Исправляем skillId: 401 → 501 (первый скилл паладина)
    if "skillId: 401" in line:
        line = line.replace("skillId: 401", "skillId: 501")
        print("✅ skillId изменён: 401 → 501")

    # 4. Переименовываем physicalDamageBonusPercent → damageBonusPercent
    if "physicalDamageBonusPercent:" in line:
        line = line.replace("physicalDamageBonusPercent:", "damageBonusPercent:")
        print("✅ Поле переименовано: physicalDamageBonusPercent → damageBonusPercent")

    new_lines.append(line)

# Находим позицию для вставки недостающих полей SkillConfig
# Ищем строку с strengthScaling, после неё вставим lifeStealPercent
insert_index = -1
for i, line in enumerate(new_lines):
    if "strengthScaling:" in line:
        insert_index = i + 1
        break

# Вставляем lifeStealPercent если его нет
if insert_index > 0:
    has_lifesteal = any("lifeStealPercent:" in line for line in new_lines)
    if not has_lifesteal:
        new_lines.insert(insert_index, "  lifeStealPercent: 0\n")
        print("✅ Добавлено поле: lifeStealPercent")

# Проверяем наличие обязательных полей SkillConfig в конце файла
required_fields = {
    "projectileSpeed": "  projectileSpeed: 20\n",
    "projectileLifetime": "  projectileLifetime: 5\n",
    "projectileHoming": "  projectileHoming: 1\n",
    "homingSpeed": "  homingSpeed: 10\n",
    "homingRadius": "  homingRadius: 20\n",
    "castEffectPrefab": "  castEffectPrefab: {fileID: 0}\n",
    "hitEffectPrefab": "  hitEffectPrefab: {fileID: 0}\n",
    "aoeEffectPrefab": "  aoeEffectPrefab: {fileID: 0}\n",
    "casterEffectPrefab": "  casterEffectPrefab: {fileID: 0}\n",
    "animationSpeed": "  animationSpeed: 1\n",
    "projectileSpawnTiming": "  projectileSpawnTiming: 0.5\n",
    "hitSound": "  hitSound: {fileID: 0}\n",
    "soundVolume": "  soundVolume: 0.8\n",
    "enableMovement": "  enableMovement: 0\n",
    "movementType": "  movementType: 0\n",
    "movementDistance": "  movementDistance: 5\n",
    "movementSpeed": "  movementSpeed: 10\n",
    "movementDirection": "  movementDirection: 0\n",
    "syncProjectiles": "  syncProjectiles: 0\n",
    "syncHitEffects": "  syncHitEffects: 1\n",
    "syncStatusEffects": "  syncStatusEffects: 1\n",
}

# Проверяем какие поля отсутствуют
content_str = ''.join(new_lines)
missing_fields = []
for field_name, field_line in required_fields.items():
    if field_name + ":" not in content_str:
        missing_fields.append(field_line)

# Добавляем customData если его нет
if "customData:" not in content_str:
    missing_fields.extend([
        "  customData:\n",
        "    chainCount: 0\n",
        "    chainRadius: 8\n",
        "    chainDamageMultiplier: 0.7\n",
        "    hitCount: 1\n",
        "    hitDelay: 0.1\n",
        "    manaRestorePercent: 0\n",
        "    piercing: 0\n",
        "    maxPierceTargets: 3\n"
    ])

# Добавляем недостающие поля в конец (перед последней пустой строкой)
if missing_fields:
    # Удаляем последнюю пустую строку если есть
    if new_lines[-1].strip() == "":
        new_lines = new_lines[:-1]

    # Добавляем недостающие поля
    new_lines.extend(missing_fields)
    print(f"✅ Добавлено {len(missing_fields)} недостающих полей SkillConfig")

# Сохраняем
with open(file_path, 'w', encoding='utf-8') as f:
    f.writelines(new_lines)

print(f"\n✅✅✅ Файл {file_path} успешно конвертирован!")
print(f"\n🔧 СОХРАНЁННАЯ МЕХАНИКА BEARFORM:")
print(f"   - transformationModel: {fileID: 2392146731170012137, guid: 854220d1cd63d4049a99e4c4ec58555e}")
print(f"   - transformationDuration: 30")
print(f"   - hpBonusPercent: 50")
print(f"   - damageBonusPercent: 30 (было physicalDamageBonusPercent)")
