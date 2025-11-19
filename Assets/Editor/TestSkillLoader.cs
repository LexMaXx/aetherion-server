using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Тестер загрузки скиллов - проверяет что все 25 скиллов загружаются корректно
/// </summary>
public class TestSkillLoader : MonoBehaviour
{
    [MenuItem("Aetherion/Debug/Test Skill Loading - All Classes")]
    public static void TestAllClasses()
    {
        Debug.Log("═════════════════════════════════════════════════════");
        Debug.Log("ТЕСТИРОВАНИЕ ЗАГРУЗКИ СКИЛЛОВ ДЛЯ ВСЕХ КЛАССОВ");
        Debug.Log("═════════════════════════════════════════════════════");

        string[] classes = SkillConfigLoader.GetAllClasses();
        int totalSuccess = 0;
        int totalFailed = 0;

        foreach (string className in classes)
        {
            Debug.Log($"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.Log($"КЛАСС: {className}");
            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            List<SkillConfig> skills = SkillConfigLoader.LoadSkillsForClass(className);

            Debug.Log($"✅ Загружено: {skills.Count}/5 скиллов\n");

            foreach (SkillConfig skill in skills)
            {
                if (skill != null)
                {
                    totalSuccess++;
                    Debug.Log($"  ✅ {skill.skillId}: {skill.skillName}");
                    Debug.Log($"     - Type: {skill.skillType}");
                    Debug.Log($"     - Cooldown: {skill.cooldown}s");
                    Debug.Log($"     - Mana: {skill.manaCost}");

                    // Проверка иконки
                    if (skill.icon != null)
                    {
                        Debug.Log($"     - Icon: ✅ {skill.icon.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"     - Icon: ❌ ОТСУТСТВУЕТ!");
                    }

                    Debug.Log("");
                }
                else
                {
                    totalFailed++;
                    Debug.LogError($"  ❌ Скилл = NULL!");
                }
            }

            if (skills.Count < 5)
            {
                totalFailed += (5 - skills.Count);
                Debug.LogError($"❌ Недостаточно скиллов! Ожидалось: 5, Загружено: {skills.Count}");
            }
        }

        Debug.Log("\n═════════════════════════════════════════════════════");
        Debug.Log($"ИТОГИ:");
        Debug.Log($"✅ Успешно загружено: {totalSuccess}/25");
        Debug.Log($"❌ Ошибок: {totalFailed}");
        Debug.Log("═════════════════════════════════════════════════════");
    }

    [MenuItem("Aetherion/Debug/Test Skill Loading - Warrior Only")]
    public static void TestWarriorSkills()
    {
        TestSingleClass("Warrior");
    }

    [MenuItem("Aetherion/Debug/Test Skill Loading - Paladin Only")]
    public static void TestPaladinSkills()
    {
        TestSingleClass("Paladin");
    }

    private static void TestSingleClass(string className)
    {
        Debug.Log($"═════════════════════════════════════════════════════");
        Debug.Log($"ТЕСТИРОВАНИЕ КЛАССА: {className}");
        Debug.Log($"═════════════════════════════════════════════════════\n");

        List<SkillConfig> skills = SkillConfigLoader.LoadSkillsForClass(className);

        Debug.Log($"Загружено: {skills.Count}/5 скиллов\n");

        for (int i = 0; i < skills.Count; i++)
        {
            SkillConfig skill = skills[i];
            Debug.Log($"[Slot {i + 1}] {skill.skillName} (ID: {skill.skillId})");
            Debug.Log($"  Type: {skill.skillType}");
            Debug.Log($"  Target: {skill.targetType}");
            Debug.Log($"  Damage/Heal: {skill.baseDamageOrHeal}");
            Debug.Log($"  Cooldown: {skill.cooldown}s");
            Debug.Log($"  Mana: {skill.manaCost}");
            Debug.Log($"  Icon: {(skill.icon != null ? $"✅ {skill.icon.name}" : "❌ ОТСУТСТВУЕТ")}");
            Debug.Log($"  Description: {skill.description}\n");
        }
    }
}
