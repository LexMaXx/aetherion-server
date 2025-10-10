using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Автоматически добавляет все скиллы из папки Resources/Skills в SkillDatabase
/// Запуск: Tools → Aetherion → Populate Skill Database
/// </summary>
public class PopulateSkillDatabase : Editor
{
    [MenuItem("Tools/Aetherion/Populate Skill Database")]
    public static void PopulateDatabase()
    {
        Debug.Log("[PopulateSkillDatabase] Начинаю поиск и добавление скиллов...");

        // Загружаем SkillDatabase
        SkillDatabase database = Resources.Load<SkillDatabase>("SkillDatabase");

        if (database == null)
        {
            Debug.LogError("[PopulateSkillDatabase] ❌ SkillDatabase не найдена в Resources!");
            return;
        }

        // Ищем все SkillData в папке Resources/Skills
        string[] skillGuids = AssetDatabase.FindAssets("t:SkillData", new[] { "Assets/Resources/Skills" });

        Debug.Log($"[PopulateSkillDatabase] Найдено {skillGuids.Length} файлов скиллов");

        // Создаем временные списки для каждого класса
        List<SkillData> warriorSkills = new List<SkillData>();
        List<SkillData> mageSkills = new List<SkillData>();
        List<SkillData> archerSkills = new List<SkillData>();
        List<SkillData> rogueSkills = new List<SkillData>();
        List<SkillData> paladinSkills = new List<SkillData>();

        // Сортируем скиллы по классам
        foreach (string guid in skillGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);

            if (skill == null)
            {
                Debug.LogWarning($"[PopulateSkillDatabase] ⚠️ Не удалось загрузить скилл: {path}");
                continue;
            }

            // Добавляем в соответствующий список
            switch (skill.characterClass)
            {
                case CharacterClass.Warrior:
                    warriorSkills.Add(skill);
                    break;
                case CharacterClass.Mage:
                    mageSkills.Add(skill);
                    break;
                case CharacterClass.Archer:
                    archerSkills.Add(skill);
                    break;
                case CharacterClass.Rogue:
                    rogueSkills.Add(skill);
                    break;
                case CharacterClass.Paladin:
                    paladinSkills.Add(skill);
                    break;
            }

            Debug.Log($"[PopulateSkillDatabase] ✅ {skill.characterClass}: {skill.skillName}");
        }

        // Обновляем базу данных через SerializedObject (чтобы работало в Editor)
        SerializedObject serializedDatabase = new SerializedObject(database);

        serializedDatabase.FindProperty("warriorSkills").ClearArray();
        serializedDatabase.FindProperty("mageSkills").ClearArray();
        serializedDatabase.FindProperty("archerSkills").ClearArray();
        serializedDatabase.FindProperty("rogueSkills").ClearArray();
        serializedDatabase.FindProperty("paladinSkills").ClearArray();

        // Добавляем скиллы воина
        SerializedProperty warriorProp = serializedDatabase.FindProperty("warriorSkills");
        warriorProp.arraySize = warriorSkills.Count;
        for (int i = 0; i < warriorSkills.Count; i++)
        {
            warriorProp.GetArrayElementAtIndex(i).objectReferenceValue = warriorSkills[i];
        }

        // Добавляем скиллы мага
        SerializedProperty mageProp = serializedDatabase.FindProperty("mageSkills");
        mageProp.arraySize = mageSkills.Count;
        for (int i = 0; i < mageSkills.Count; i++)
        {
            mageProp.GetArrayElementAtIndex(i).objectReferenceValue = mageSkills[i];
        }

        // Добавляем скиллы лучника
        SerializedProperty archerProp = serializedDatabase.FindProperty("archerSkills");
        archerProp.arraySize = archerSkills.Count;
        for (int i = 0; i < archerSkills.Count; i++)
        {
            archerProp.GetArrayElementAtIndex(i).objectReferenceValue = archerSkills[i];
        }

        // Добавляем скиллы разбойника
        SerializedProperty rogueProp = serializedDatabase.FindProperty("rogueSkills");
        rogueProp.arraySize = rogueSkills.Count;
        for (int i = 0; i < rogueSkills.Count; i++)
        {
            rogueProp.GetArrayElementAtIndex(i).objectReferenceValue = rogueSkills[i];
        }

        // Добавляем скиллы паладина
        SerializedProperty paladinProp = serializedDatabase.FindProperty("paladinSkills");
        paladinProp.arraySize = paladinSkills.Count;
        for (int i = 0; i < paladinSkills.Count; i++)
        {
            paladinProp.GetArrayElementAtIndex(i).objectReferenceValue = paladinSkills[i];
        }

        // Сохраняем изменения
        serializedDatabase.ApplyModifiedProperties();

        // Помечаем объект как изменённый
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PopulateSkillDatabase] ✅ ГОТОВО! Добавлено скиллов:");
        Debug.Log($"  - Warrior: {warriorSkills.Count}");
        Debug.Log($"  - Mage: {mageSkills.Count}");
        Debug.Log($"  - Archer: {archerSkills.Count}");
        Debug.Log($"  - Rogue: {rogueSkills.Count}");
        Debug.Log($"  - Paladin: {paladinSkills.Count}");
    }
}
