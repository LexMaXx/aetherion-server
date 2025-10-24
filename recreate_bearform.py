#!/usr/bin/env python3
"""
Создаёт правильный Paladin_BearForm.asset на основе шаблона
"""

# Читаем шаблон (DivineProtection)
with open("Assets/Resources/Skills/Paladin_DivineProtection.asset", 'r', encoding='utf-8') as f:
    lines = f.readlines()

# Изменяем нужные поля
new_lines = []
for line in lines:
    # Меняем m_Name
    if "m_Name: Paladin_DivineProtection" in line:
        line = line.replace("Paladin_DivineProtection", "Paladin_BearForm")

    # Меняем skillId
    if "skillId: 502" in line:
        line = line.replace("502", "501")

    # Меняем skillName
    if "skillName: Divine Protection" in line:
        line = line.replace("Divine Protection", "Bear Form")

    # Меняем description
    if "description:" in line and "Даёт НЕУЯЗВИМОСТЬ" in line:
        line = '  description: "Превращение в медведя. Увеличивает HP на 50% и физический урон на 30% на 30 секунд."\n'

    # Меняем icon
    if "icon: {fileID:" in line and "502" not in line:  # Оставляем icon из шаблона или очищаем
        pass  # Оставляем как есть

    # Меняем cooldown
    if "cooldown: 120" in line:
        pass  # Оставляем 120

    # Меняем manaCost
    if "manaCost: 80" in line:
        pass  # Оставляем 80

    # Меняем skillType
    if "skillType: 5" in line:
        line = line.replace("skillType: 5", "skillType: 6")  # 6 = Transformation

    # Меняем transformationModel
    if "transformationModel: {fileID: 0}" in line:
        line = line.replace("{fileID: 0}", "{fileID: 2392146731170012137, guid: 854220d1cd63d4049a99e4c4ec58555e, type: 3}")

    # Меняем transformationDuration
    if "transformationDuration: 30" in line:
        pass  # Оставляем 30

    # Меняем hpBonusPercent
    if "hpBonusPercent: 0" in line:
        line = line.replace("hpBonusPercent: 0", "hpBonusPercent: 50")

    # Меняем damageBonusPercent
    if "damageBonusPercent: 0" in line:
        line = line.replace("damageBonusPercent: 0", "damageBonusPercent: 30")

    new_lines.append(line)

# Сохраняем
with open("Assets/Resources/Skills/Paladin_BearForm.asset", 'w', encoding='utf-8') as f:
    f.writelines(new_lines)

print("✅ Paladin_BearForm.asset создан на основе правильного шаблона!")
print("✅ Все поля SkillConfig сохранены")
print("✅ Трансформация в медведя настроена")
