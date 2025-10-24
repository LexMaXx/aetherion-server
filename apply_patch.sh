#!/bin/bash
# Применяем изменения к ArenaManager.cs

cd /c/Users/Asus/Aetherion

# Создаем временный файл с новым блоком кода
cat > new_block.txt << 'EOF'
        // СИСТЕМА СКИЛЛОВ: EffectManager + SkillExecutor + SkillManager

        // 1. Добавляем EffectManager (управление эффектами: Root, Stun, Slow и т.д.)
        EffectManager effectManager = modelTransform.GetComponent<EffectManager>();
        if (effectManager == null)
        {
            effectManager = modelTransform.gameObject.AddComponent<EffectManager>();
            Debug.Log("✓ Добавлен EffectManager");
        }

        // 2. Добавляем SkillExecutor (КРИТИЧЕСКОЕ! Должен быть ПЕРЕД SkillManager)
        SkillExecutor skillExecutor = modelTransform.GetComponent<SkillExecutor>();
        if (skillExecutor == null)
        {
            skillExecutor = modelTransform.gameObject.AddComponent<SkillExecutor>();
            Debug.Log("✓ Добавлен SkillExecutor");
        }

        // 3. Добавляем SkillManager (управление списком скиллов)
        SkillManager skillManager = modelTransform.GetComponent<SkillManager>();
        if (skillManager == null)
        {
            skillManager = modelTransform.gameObject.AddComponent<SkillManager>();
            Debug.Log("✓ Добавлен SkillManager");
        }

        // 4. АВТОМАТИЧЕСКАЯ ЗАГРУЗКА из Resources/Skills/ по классу персонажа
        string selectedClass = PlayerPrefs.GetString("SelectedCharacterClass", "Warrior");
        Debug.Log($"[ArenaManager] 🔄 Автоматическая загрузка скиллов для класса {selectedClass} из Resources/Skills/...");
        LoadAllSkillsToManager(skillManager, selectedClass);
EOF

# Применяем изменения через awk
awk '
/Добавляем систему очков действия/,/LoadEquippedSkillsFromPlayerPrefs/ {
    if (/Добавляем систему очков действия/) {
        print
        getline; print
        getline; print
        getline; print
        getline; print
        getline; print
        getline
        print ""
        while ((getline line < "new_block.txt") > 0) {
            print line
        }
        close("new_block.txt")
        # Пропускаем старые строки до LoadEquippedSkillsFromPlayerPrefs включительно
        while (getline && !/LoadEquippedSkillsFromPlayerPrefs/) {}
        next
    }
}
{print}
' Assets/Scripts/Arena/ArenaManager.cs > Assets/Scripts/Arena/ArenaManager.cs.tmp

mv Assets/Scripts/Arena/ArenaManager.cs.tmp Assets/Scripts/Arena/ArenaManager.cs
rm new_block.txt

echo "✅ Файл успешно обновлен!"
