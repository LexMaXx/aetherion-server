using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Исправляет skillId во всех SkillConfig файлах согласно маппингу
/// </summary>
public class FixAllSkillIDs : MonoBehaviour
{
    [MenuItem("Aetherion/Skills/Fix All Skill IDs (CRITICAL!)")]
    public static void FixAll()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("ИСПРАВЛЕНИЕ ВСЕХ SKILL ID");
        Debug.Log("═══════════════════════════════════════════════════════\n");

        // Mapping: имя файла → правильный skillId
        Dictionary<string, int> correctIds = new Dictionary<string, int>()
        {
            // WARRIOR (101-105)
            { "Warrior_BattleRage", 101 },
            { "Warrior_DefensiveStance", 102 },
            { "Warrior_HammerThrow", 103 },
            { "Warrior_BattleHeal", 104 },
            { "Warrior_Charge", 105 },

            // MAGE (201-205)
            { "Mage_Fireball", 201 },
            { "Mage_IceNova", 202 },
            { "Mage_Meteor", 203 },
            { "Mage_Teleport", 204 },
            { "Mage_LightningStorm", 205 },

            // ARCHER (301-305)
            { "Archer_RainOfArrows", 301 },
            { "Archer_StunningShot", 302 },
            { "Archer_EagleEye", 303 },
            { "Archer_SwiftStride", 304 },
            { "Archer_EntanglingShot", 305 },

            // NECROMANCER (601-605) - файлы с префиксом Rogue_
            { "Rogue_SummonSkeletons", 601 },
            { "Rogue_SoulDrain", 602 },
            { "Rogue_CurseOfWeakness", 603 },
            { "Rogue_CripplingCurse", 604 },
            { "Rogue_BloodForMana", 605 },

            // PALADIN (501-505)
            { "Paladin_BearForm", 501 },
            { "Paladin_DivineProtection", 502 },
            { "Paladin_LayOnHands", 503 },
            { "Paladin_DivineStrength", 504 },
            { "Paladin_HolyHammer", 505 }
        };

        int fixedCount = 0;
        int errorCount = 0;

        foreach (var kvp in correctIds)
        {
            string fileName = kvp.Key;
            int correctId = kvp.Value;
            string path = $"Assets/Resources/Skills/{fileName}.asset";

            // Загружаем SkillConfig
            SkillConfig skill = AssetDatabase.LoadAssetAtPath<SkillConfig>(path);

            if (skill == null)
            {
                Debug.LogWarning($"⚠️ {fileName}.asset не найден! Пропускаем...");
                errorCount++;
                continue;
            }

            // Проверяем ID
            if (skill.skillId != correctId)
            {
                Debug.Log($"🔧 {fileName}: {skill.skillId} → {correctId}");

                skill.skillId = correctId;
                EditorUtility.SetDirty(skill);
                fixedCount++;
            }
            else
            {
                Debug.Log($"✅ {fileName}: ID = {correctId} (уже правильный)");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("\n═══════════════════════════════════════════════════════");
        Debug.Log($"ИТОГИ:");
        Debug.Log($"✅ Исправлено: {fixedCount}");
        Debug.Log($"⚠️ Не найдено: {errorCount}");
        Debug.Log($"Всего проверено: {correctIds.Count}");
        Debug.Log("═══════════════════════════════════════════════════════");

        if (errorCount > 0)
        {
            Debug.LogWarning($"\n⚠️ ВНИМАНИЕ: {errorCount} файлов не найдены!");
            Debug.LogWarning("Запусти 'Recreate ALL Missing Skills' чтобы создать их!");
        }

        if (fixedCount > 0)
        {
            Debug.Log($"\n💡 ВАЖНО: Запусти 'Test Skill Loading' чтобы проверить!");
        }
    }

    [MenuItem("Aetherion/Skills/Show All Current Skill IDs")]
    public static void ShowAllIds()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("ТЕКУЩИЕ SKILL ID");
        Debug.Log("═══════════════════════════════════════════════════════\n");

        string[] guids = AssetDatabase.FindAssets("t:SkillConfig", new[] { "Assets/Resources/Skills" });

        List<SkillConfig> allSkills = new List<SkillConfig>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillConfig skill = AssetDatabase.LoadAssetAtPath<SkillConfig>(path);

            if (skill != null)
            {
                allSkills.Add(skill);
            }
        }

        // Сортируем по skillId
        allSkills.Sort((a, b) => a.skillId.CompareTo(b.skillId));

        string currentClass = "";

        foreach (SkillConfig skill in allSkills)
        {
            string className = skill.characterClass.ToString();

            if (className != currentClass)
            {
                Debug.Log($"\n━━━━━━━━━ {className} ━━━━━━━━━");
                currentClass = className;
            }

            Debug.Log($"  {skill.skillId}: {skill.skillName}");
        }

        Debug.Log("\n═══════════════════════════════════════════════════════");
        Debug.Log($"Всего SkillConfig найдено: {allSkills.Count}");
        Debug.Log("═══════════════════════════════════════════════════════");
    }
}
