#!/usr/bin/env python3
"""
Скрипт для добавления SkillExecutor в ArenaManager.cs
"""

import re

# Читаем файл
with open('Assets/Scripts/Arena/ArenaManager.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Ищем и заменяем блок кода
old_code = """        // Добавляем систему очков действия
        ActionPointsSystem actionPointsSystem = modelTransform.GetComponent<ActionPointsSystem>();
        if (actionPointsSystem == null)
        {
            actionPointsSystem = modelTransform.gameObject.AddComponent<ActionPointsSystem>();
            Debug.Log("✓ Добавлен ActionPointsSystem");
        }

        // Добавляем систему скиллов (КРИТИЧЕСКОЕ!)
        SkillManager skillManager = modelTransform.GetComponent<SkillManager>();
        if (skillManager == null)
        {
            skillManager = modelTransform.gameObject.AddComponent<SkillManager>();
            Debug.Log("✓ Добавлен SkillManager");
        }

        // КРИТИЧЕСКОЕ: Загружаем скиллы из SkillDatabase для класса
        LoadSkillsForClass(skillManager);

        // КРИТИЧЕСКОЕ: Загружаем экипированные скиллы из PlayerPrefs
        LoadEquippedSkillsFromPlayerPrefs(skillManager);"""

new_code = """        // Добавляем систему очков действия
        ActionPointsSystem actionPointsSystem = modelTransform.GetComponent<ActionPointsSystem>();
        if (actionPointsSystem == null)
        {
            actionPointsSystem = modelTransform.gameObject.AddComponent<ActionPointsSystem>();
            Debug.Log("✓ Добавлен ActionPointsSystem");
        }

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
        LoadAllSkillsToManager(skillManager, selectedClass);"""

# Проверяем что блок найден
if old_code in content:
    print("✅ Найден блок для замены")
    content = content.replace(old_code, new_code)

    # Сохраняем
    with open('Assets/Scripts/Arena/ArenaManager.cs', 'w', encoding='utf-8') as f:
        f.write(content)

    print("✅ Файл успешно обновлен!")
    print("\n📝 Изменения:")
    print("  - Добавлен EffectManager")
    print("  - Добавлен SkillExecutor (перед SkillManager)")
    print("  - Убраны LoadSkillsForClass и LoadEquippedSkillsFromPlayerPrefs")
    print("  - Добавлен LoadAllSkillsToManager с автозагрузкой из Resources/Skills/")
else:
    print("❌ Блок не найден! Возможно файл уже изменен.")
