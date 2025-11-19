using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// База данных всех скиллов игры
/// Хранит все скиллы для всех классов
/// </summary>
[CreateAssetMenu(fileName = "SkillDatabase", menuName = "Aetherion/Skills/Skill Database")]
public class SkillDatabase : ScriptableObject
{
    [Header("Warrior Skills")]
    [SerializeField] private List<SkillData> warriorSkills = new List<SkillData>();

    [Header("Mage Skills")]
    [SerializeField] private List<SkillData> mageSkills = new List<SkillData>();

    [Header("Archer Skills")]
    [SerializeField] private List<SkillData> archerSkills = new List<SkillData>();

    [Header("Rogue Skills")]
    [SerializeField] private List<SkillData> rogueSkills = new List<SkillData>();

    [Header("Paladin Skills")]
    [SerializeField] private List<SkillData> paladinSkills = new List<SkillData>();

    /// <summary>
    /// Получить все скиллы для класса
    /// </summary>
    public List<SkillData> GetSkillsForClass(CharacterClass characterClass)
    {
        switch (characterClass)
        {
            case CharacterClass.Warrior:
                return new List<SkillData>(warriorSkills);
            case CharacterClass.Mage:
                return new List<SkillData>(mageSkills);
            case CharacterClass.Archer:
                return new List<SkillData>(archerSkills);
            case CharacterClass.Rogue:
                return new List<SkillData>(rogueSkills);
            case CharacterClass.Paladin:
                return new List<SkillData>(paladinSkills);
            default:
                return new List<SkillData>();
        }
    }

    /// <summary>
    /// Получить скилл по ID
    /// </summary>
    public SkillData GetSkillById(int skillId)
    {
        // Ищем во всех списках
        foreach (var skill in warriorSkills.Concat(mageSkills).Concat(archerSkills).Concat(rogueSkills).Concat(paladinSkills))
        {
            if (skill.skillId == skillId)
            {
                return skill;
            }
        }

        Debug.LogWarning($"[SkillDatabase] Скилл с ID {skillId} не найден!");
        return null;
    }

    /// <summary>
    /// Получить все скиллы
    /// </summary>
    public List<SkillData> GetAllSkills()
    {
        List<SkillData> allSkills = new List<SkillData>();
        allSkills.AddRange(warriorSkills);
        allSkills.AddRange(mageSkills);
        allSkills.AddRange(archerSkills);
        allSkills.AddRange(rogueSkills);
        allSkills.AddRange(paladinSkills);
        return allSkills;
    }

    /// <summary>
    /// Добавить скилл
    /// </summary>
    public void AddSkill(SkillData skill)
    {
        if (skill == null)
        {
            Debug.LogWarning("[SkillDatabase] Попытка добавить null скилл!");
            return;
        }

        switch (skill.characterClass)
        {
            case CharacterClass.Warrior:
                if (!warriorSkills.Contains(skill))
                    warriorSkills.Add(skill);
                break;
            case CharacterClass.Mage:
                if (!mageSkills.Contains(skill))
                    mageSkills.Add(skill);
                break;
            case CharacterClass.Archer:
                if (!archerSkills.Contains(skill))
                    archerSkills.Add(skill);
                break;
            case CharacterClass.Rogue:
                if (!rogueSkills.Contains(skill))
                    rogueSkills.Add(skill);
                break;
            case CharacterClass.Paladin:
                if (!paladinSkills.Contains(skill))
                    paladinSkills.Add(skill);
                break;
        }

        Debug.Log($"[SkillDatabase] ✅ Добавлен скилл: {skill.skillName} ({skill.characterClass})");
    }

    /// <summary>
    /// Получить количество скиллов для класса
    /// </summary>
    public int GetSkillCountForClass(CharacterClass characterClass)
    {
        return GetSkillsForClass(characterClass).Count;
    }

    /// <summary>
    /// Синглтон для быстрого доступа
    /// </summary>
    private static SkillDatabase instance;
    public static SkillDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<SkillDatabase>("SkillDatabase");
                if (instance == null)
                {
                    Debug.LogError("[SkillDatabase] ❌ SkillDatabase не найдена в Resources! Создайте через Create → Aetherion → Skills → Skill Database");
                }
            }
            return instance;
        }
    }

    #if UNITY_EDITOR
    /// <summary>
    /// Валидация (проверка на дубликаты ID)
    /// </summary>
    void OnValidate()
    {
        HashSet<int> usedIds = new HashSet<int>();
        List<SkillData> allSkills = GetAllSkills();

        foreach (SkillData skill in allSkills)
        {
            if (skill == null) continue;

            if (usedIds.Contains(skill.skillId))
            {
                Debug.LogError($"[SkillDatabase] ❌ Дубликат ID {skill.skillId} в скилле {skill.skillName}!");
            }
            else
            {
                usedIds.Add(skill.skillId);
            }
        }
    }
    #endif
}
