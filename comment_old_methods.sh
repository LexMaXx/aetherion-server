#!/bin/bash
# Комментируем старые методы LoadSkillsForClass и LoadEquippedSkillsFromPlayerPrefs

cd /c/Users/Asus/Aetherion

# Находим начало LoadSkillsForClass
start_line=$(grep -n "private void LoadSkillsForClass" Assets/Scripts/Arena/ArenaManager.cs | cut -d: -f1)

# Находим конец LoadEquippedSkillsFromPlayerPrefs (закрывающая скобка + class EquippedSkillsData)
end_line=$(grep -n "private class EquippedSkillsData" Assets/Scripts/Arena/ArenaManager.cs | cut -d: -f1)
end_line=$((end_line - 2))  # Минус 2 строки (пустая строка и комментарий)

echo "Комментируем строки $start_line-$end_line (старые методы)"

# Комментируем блок
sed -i "${start_line},${end_line}s/^/\/\/ DEPRECATED: /" Assets/Scripts/Arena/ArenaManager.cs

echo "✅ Старые методы закомментированы"
