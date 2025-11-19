using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Проверяет наличие иконок у всех SkillConfig
/// </summary>
public class CheckSkillIcons : MonoBehaviour
{
    [MenuItem("Aetherion/Debug/Check All Skill Icons")]
    public static void CheckAllIcons()
    {
        Debug.Log("═════════════════════════════════════════════════════");
        Debug.Log("ПРОВЕРКА ИКОНОК СКИЛЛОВ");
        Debug.Log("═════════════════════════════════════════════════════\n");

        string[] classes = SkillConfigLoader.GetAllClasses();
        int totalSkills = 0;
        int withIcons = 0;
        int withoutIcons = 0;

        List<string> missingIcons = new List<string>();

        foreach (string className in classes)
        {
            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.Log($"Класс: {className}");
            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            List<SkillConfig> skills = SkillConfigLoader.LoadSkillsForClass(className);

            foreach (SkillConfig skill in skills)
            {
                totalSkills++;

                if (skill.icon != null)
                {
                    withIcons++;
                    Debug.Log($"  ✅ {skill.skillName} - Icon: {skill.icon.name}");
                }
                else
                {
                    withoutIcons++;
                    string skillInfo = $"{className}: {skill.skillName} (ID: {skill.skillId})";
                    missingIcons.Add(skillInfo);
                    Debug.LogWarning($"  ❌ {skill.skillName} - НЕТ ИКОНКИ!");
                }
            }
            Debug.Log("");
        }

        Debug.Log("═════════════════════════════════════════════════════");
        Debug.Log($"ИТОГИ:");
        Debug.Log($"Всего скиллов: {totalSkills}");
        Debug.Log($"✅ С иконками: {withIcons}");
        Debug.Log($"❌ Без иконок: {withoutIcons}");

        if (withoutIcons > 0)
        {
            Debug.LogWarning($"\n⚠️ СКИЛЛЫ БЕЗ ИКОНОК:");
            foreach (string skillInfo in missingIcons)
            {
                Debug.LogWarning($"  - {skillInfo}");
            }
        }
        else
        {
            Debug.Log($"\n🎉 ВСЕ СКИЛЛЫ ИМЕЮТ ИКОНКИ!");
        }

        Debug.Log("═════════════════════════════════════════════════════");
    }

    [MenuItem("Aetherion/Debug/List Available Skill Icons")]
    public static void ListAvailableIcons()
    {
        Debug.Log("═════════════════════════════════════════════════════");
        Debug.Log("ДОСТУПНЫЕ ИКОНКИ В ПРОЕКТЕ");
        Debug.Log("═════════════════════════════════════════════════════\n");

        // Ищем все Sprite в папках UI/Icons
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/UI" });

        if (guids.Length == 0)
        {
            Debug.LogWarning("❌ Не найдено иконок в Assets/UI/");
            return;
        }

        Debug.Log($"Найдено иконок: {guids.Length}\n");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite != null)
            {
                Debug.Log($"  📁 {sprite.name}");
                Debug.Log($"     Path: {path}");
            }
        }

        Debug.Log("\n═════════════════════════════════════════════════════");
    }
}
