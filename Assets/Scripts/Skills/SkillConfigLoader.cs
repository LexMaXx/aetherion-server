using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Helper class для загрузки SkillConfig по skillId
/// ВАЖНО: Загружает SkillData из Resources/Skills/ и конвертирует в SkillConfig!
/// </summary>
public static class SkillConfigLoader
{
    /// <summary>
    /// Mapping: класс → массив skillIds (5 скиллов на класс)
    /// </summary>
    private static readonly Dictionary<string, int[]> ClassSkillIds = new Dictionary<string, int[]>()
    {
        { "Warrior", new int[] { 101, 102, 103, 104, 105 } },
        { "Mage", new int[] { 201, 202, 203, 204, 205 } },
        { "Archer", new int[] { 301, 302, 303, 304, 305 } },
        { "Necromancer", new int[] { 601, 602, 603, 604, 605 } },
        { "Rogue", new int[] { 601, 602, 603, 604, 605 } }, // Rogue = Necromancer (используют одни файлы)
        { "Paladin", new int[] { 501, 502, 503, 504, 505 } }
    };

    /// <summary>
    /// Mapping: skillId → путь к asset
    /// </summary>
    private static readonly Dictionary<int, string> SkillPaths = new Dictionary<int, string>()
    {
        // ═══════════════════════════════════════════
        // WARRIOR (101-105)
        // ═══════════════════════════════════════════
        { 101, "Skills/Warrior_BattleRage" },
        { 102, "Skills/Warrior_DefensiveStance" },
        { 103, "Skills/Warrior_HammerThrow" },
        { 104, "Skills/Warrior_BattleHeal" },
        { 105, "Skills/Warrior_Charge" },

        // ═══════════════════════════════════════════
        // MAGE (201-205)
        // ═══════════════════════════════════════════
        { 201, "Skills/Mage_Fireball" },
        { 202, "Skills/Mage_IceNova" },
        { 203, "Skills/Mage_Meteor" },
        { 204, "Skills/Mage_Teleport" },
        { 205, "Skills/Mage_LightningStorm" },

        // ═══════════════════════════════════════════
        // ARCHER (301-305)
        // ═══════════════════════════════════════════
        { 301, "Skills/Archer_RainOfArrows" },
        { 302, "Skills/Archer_StunningShot" },
        { 303, "Skills/Archer_EagleEye" },
        { 304, "Skills/Archer_SwiftStride" },
        { 305, "Skills/Archer_DeadlyPrecision" }, // ИСПРАВЛЕНО: было EntanglingShot

        // ═══════════════════════════════════════════
        // NECROMANCER/ROGUE (601-605) - файлы с префиксом Rogue_
        // ═══════════════════════════════════════════
        { 601, "Skills/Rogue_RaiseDead" }, // ИСПРАВЛЕНО: было SummonSkeletons
        { 602, "Skills/Rogue_SoulDrain" },
        { 603, "Skills/Rogue_CurseOfWeakness" },
        { 604, "Skills/Rogue_CripplingCurse" },
        { 605, "Skills/Rogue_BloodForMana" },

        // ═══════════════════════════════════════════
        // PALADIN (501-505)
        // ═══════════════════════════════════════════
        { 501, "Skills/Paladin_BearForm" },
        { 502, "Skills/Paladin_DivineProtection" },
        { 503, "Skills/Paladin_LayOnHands" },
        { 504, "Skills/Paladin_DivineStrength" },
        { 505, "Skills/Paladin_HolyHammer" }
    };

    /// <summary>
    /// Загрузить SkillConfig по skillId (файлы УЖЕ SkillConfig, конвертация НЕ нужна)
    /// </summary>
    public static SkillConfig LoadSkillById(int skillId)
    {
        if (!SkillPaths.ContainsKey(skillId))
        {
            Debug.LogError($"[SkillConfigLoader] ❌ Неизвестный skillId: {skillId}");
            return null;
        }

        string path = SkillPaths[skillId];
        SkillConfig skillConfig = Resources.Load<SkillConfig>(path);

        if (skillConfig == null)
        {
            Debug.LogError($"[SkillConfigLoader] ❌ Не удалось загрузить скилл по пути: {path}");
            return null;
        }

        Debug.Log($"[SkillConfigLoader] ✅ Загружен скилл: {skillConfig.skillName} (ID: {skillId})");
        return skillConfig;
    }

    /// <summary>
    /// Загрузить ВСЕ скиллы класса (5 скиллов) как SkillConfig
    /// </summary>
    public static List<SkillConfig> LoadSkillsForClass(string characterClass)
    {
        List<SkillConfig> skills = new List<SkillConfig>();

        if (!ClassSkillIds.ContainsKey(characterClass))
        {
            Debug.LogError($"[SkillConfigLoader] ❌ Неизвестный класс: {characterClass}");
            return skills;
        }

        int[] skillIds = ClassSkillIds[characterClass];
        Debug.Log($"[SkillConfigLoader] 📚 Загрузка скиллов для класса {characterClass}: {string.Join(", ", skillIds)}");

        foreach (int skillId in skillIds)
        {
            SkillConfig skill = LoadSkillById(skillId);
            if (skill != null)
            {
                skills.Add(skill);
            }
        }

        Debug.Log($"[SkillConfigLoader] ✅ Загружено {skills.Count}/5 скиллов для {characterClass}");
        return skills;
    }

    /// <summary>
    /// Получить массив skillIds для класса
    /// </summary>
    public static int[] GetSkillIdsForClass(string characterClass)
    {
        if (!ClassSkillIds.ContainsKey(characterClass))
        {
            Debug.LogWarning($"[SkillConfigLoader] ⚠️ Неизвестный класс: {characterClass}");
            return new int[0];
        }

        return ClassSkillIds[characterClass];
    }

    /// <summary>
    /// Получить список всех классов
    /// </summary>
    public static string[] GetAllClasses()
    {
        return new string[] { "Warrior", "Mage", "Archer", "Necromancer", "Paladin" };
    }

    /// <summary>
    /// Проверить существует ли скилл с таким ID
    /// </summary>
    public static bool SkillExists(int skillId)
    {
        return SkillPaths.ContainsKey(skillId);
    }

    /// <summary>
    /// Получить путь к скиллу по ID (для отладки)
    /// </summary>
    public static string GetSkillPath(int skillId)
    {
        if (!SkillPaths.ContainsKey(skillId))
        {
            return null;
        }
        return SkillPaths[skillId];
    }
}
