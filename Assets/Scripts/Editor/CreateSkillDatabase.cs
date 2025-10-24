using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Editor скрипт для создания SkillDatabase и примеров скиллов для всех классов
/// </summary>
public class CreateSkillDatabase : EditorWindow
{
    [MenuItem("Tools/Skills/Create Skill Database")]
    public static void CreateDatabase()
    {
        // Создаём папки
        CreateFolders();

        // Создаём базу данных
        string dbPath = "Assets/Resources/SkillDatabase.asset";
        SkillDatabase db = AssetDatabase.LoadAssetAtPath<SkillDatabase>(dbPath);

        if (db == null)
        {
            db = ScriptableObject.CreateInstance<SkillDatabase>();
            AssetDatabase.CreateAsset(db, dbPath);
            Debug.Log("✅ SkillDatabase создана");
        }

        // Создаём примеры скиллов для каждого класса
        CreateWarriorSkills(db);
        CreateMageSkills(db);
        CreateArcherSkills(db);
        CreateRogueSkills(db);
        CreatePaladinSkills(db);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Успех!",
            "SkillDatabase и примеры скиллов созданы!\n\n" +
            "Путь: Assets/Resources/SkillDatabase.asset\n" +
            "Скиллы: Assets/Resources/Skills/",
            "OK");

        Selection.activeObject = db;
        EditorGUIUtility.PingObject(db);
    }

    private static void CreateFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        if (!AssetDatabase.IsValidFolder("Assets/Resources/Skills"))
            AssetDatabase.CreateFolder("Assets/Resources", "Skills");

        Debug.Log("✅ Папки созданы");
    }

    // ===== WARRIOR SKILLS =====
    private static void CreateWarriorSkills(SkillDatabase db)
    {
        Debug.Log("\n=== Создание скиллов Warrior ===");

        // 1. Power Strike - мощный удар
        CreateSkill(db, 101, "Power Strike", "Мощный удар мечом, наносящий большой урон", CharacterClass.Warrior,
            SkillType.Damage, 8f, 30f, 5f, 1f, baseDamage: 50f, strengthScaling: 20f);

        // 2. Whirlwind - вихрь мечей (AOE)
        CreateSkill(db, 102, "Whirlwind", "Вихрь клинков вокруг себя, урон по области", CharacterClass.Warrior,
            SkillType.Damage, 15f, 50f, 0f, 0f, baseDamage: 35f, strengthScaling: 15f, aoeRadius: 5f, maxTargets: 5);

        // 3. Battle Cry - боевой клич (бафф атаки)
        CreateSkill(db, 103, "Battle Cry", "Боевой клич, увеличивающий атаку союзников", CharacterClass.Warrior,
            SkillType.Buff, 20f, 40f, 0f, 0f, requiresTarget: false, canTargetAllies: true);

        // 4. Shield Bash - удар щитом (стан)
        CreateSkill(db, 104, "Shield Bash", "Удар щитом, оглушающий врага", CharacterClass.Warrior,
            SkillType.CrowdControl, 12f, 20f, 3f, 0.5f, baseDamage: 20f, strengthScaling: 10f);

        // 5. Charge - рывок к врагу
        CreateSkill(db, 105, "Charge", "Рывок к врагу с мощным ударом", CharacterClass.Warrior,
            SkillType.Damage, 10f, 25f, 15f, 0.3f, baseDamage: 40f, strengthScaling: 18f);

        // 6. Berserker Rage - ярость берсерка (бафф)
        CreateSkill(db, 106, "Berserker Rage", "Впадает в ярость, увеличивая атаку и скорость", CharacterClass.Warrior,
            SkillType.Buff, 30f, 60f, 0f, 0f, requiresTarget: false);
    }

    // ===== MAGE SKILLS =====
    private static void CreateMageSkills(SkillDatabase db)
    {
        Debug.Log("\n=== Создание скиллов Mage ===");

        // 1. Fireball - огненный шар
        CreateSkill(db, 201, "Fireball", "Огненный шар с большим уроном", CharacterClass.Mage,
            SkillType.Damage, 6f, 40f, 20f, 0.8f, baseDamage: 60f, intelligenceScaling: 25f);

        // 2. Ice Nova - ледяная новая (AOE + замедление)
        CreateSkill(db, 202, "Ice Nova", "Взрыв льда вокруг себя, урон и замедление", CharacterClass.Mage,
            SkillType.Damage, 18f, 60f, 0f, 1.2f, baseDamage: 40f, intelligenceScaling: 20f, aoeRadius: 8f, maxTargets: 8);

        // 3. Teleport - телепорт
        CreateSkill(db, 203, "Teleport", "Мгновенный телепорт на короткую дистанцию", CharacterClass.Mage,
            SkillType.Teleport, 8f, 30f, 15f, 0f, requiresTarget: false);

        // 4. Meteor - метеор (AOE)
        CreateSkill(db, 204, "Meteor", "Вызывает метеорит с огромным уроном по области", CharacterClass.Mage,
            SkillType.Damage, 25f, 80f, 25f, 2f, baseDamage: 100f, intelligenceScaling: 40f, aoeRadius: 10f, maxTargets: 10);

        // 5. Mana Shield - щит маны
        CreateSkill(db, 205, "Mana Shield", "Создаёт щит, поглощающий урон", CharacterClass.Mage,
            SkillType.Buff, 15f, 50f, 0f, 0.5f, requiresTarget: false);

        // 6. Lightning Storm - гроза (DOT)
        CreateSkill(db, 206, "Lightning Storm", "Вызывает грозу, периодический урон", CharacterClass.Mage,
            SkillType.Damage, 20f, 55f, 20f, 1.5f, baseDamage: 30f, intelligenceScaling: 15f, aoeRadius: 7f);
    }

    // ===== ARCHER SKILLS =====
    private static void CreateArcherSkills(SkillDatabase db)
    {
        Debug.Log("\n=== Создание скиллов Archer ===");

        // 1. Piercing Shot - пробивающий выстрел
        CreateSkill(db, 301, "Piercing Shot", "Стрела пробивает врагов насквозь", CharacterClass.Archer,
            SkillType.Damage, 8f, 25f, 30f, 0.6f, baseDamage: 45f, strengthScaling: 15f);

        // 2. Explosive Arrow - взрывная стрела (AOE)
        CreateSkill(db, 302, "Explosive Arrow", "Стрела взрывается при попадании", CharacterClass.Archer,
            SkillType.Damage, 15f, 40f, 25f, 1f, baseDamage: 50f, strengthScaling: 20f, aoeRadius: 4f, maxTargets: 5);

        // 3. Rain of Arrows - дождь стрел (AOE)
        CreateSkill(db, 303, "Rain of Arrows", "Град стрел на большую область", CharacterClass.Archer,
            SkillType.Damage, 20f, 50f, 20f, 1.5f, baseDamage: 35f, strengthScaling: 12f, aoeRadius: 12f, maxTargets: 10);

        // 4. Eagle Eye - орлиный глаз (бафф)
        CreateSkill(db, 304, "Eagle Eye", "Увеличивает дальность и точность", CharacterClass.Archer,
            SkillType.Buff, 25f, 30f, 0f, 0f, requiresTarget: false);

        // 5. Entangling Shot - опутывающий выстрел (корни)
        CreateSkill(db, 305, "Entangling Shot", "Стрела опутывает врага корнями", CharacterClass.Archer,
            SkillType.CrowdControl, 12f, 30f, 20f, 0.5f, baseDamage: 20f);

        // 6. Volley - залп
        CreateSkill(db, 306, "Volley", "Быстрый залп из 5 стрел", CharacterClass.Archer,
            SkillType.Damage, 10f, 35f, 15f, 0.8f, baseDamage: 25f, strengthScaling: 10f);
    }

    // ===== ROGUE SKILLS =====
    private static void CreateRogueSkills(SkillDatabase db)
    {
        Debug.Log("\n=== Создание скиллов Rogue ===");

        // 1. Backstab - удар в спину
        CreateSkill(db, 401, "Backstab", "Критический удар кинжалом со спины", CharacterClass.Rogue,
            SkillType.Damage, 8f, 20f, 2f, 0.4f, baseDamage: 70f, strengthScaling: 25f);

        // 2. Summon Skeletons - призыв скелетов ⭐
        CreateSkill(db, 402, "Summon Skeletons", "Призывает 3 скелетов для помощи в бою", CharacterClass.Rogue,
            SkillType.Summon, 30f, 80f, 0f, 1f, requiresTarget: false);

        // 3. Shadow Step - шаг в тени (телепорт за спину)
        CreateSkill(db, 403, "Shadow Step", "Телепорт за спину врага", CharacterClass.Rogue,
            SkillType.Teleport, 10f, 25f, 10f, 0f);

        // 4. Poison Blade - отравленный клинок (DOT)
        CreateSkill(db, 404, "Poison Blade", "Атака с ядом, урон со временем", CharacterClass.Rogue,
            SkillType.Damage, 12f, 30f, 3f, 0.5f, baseDamage: 30f, strengthScaling: 10f);

        // 5. Smoke Bomb - дымовая завеса (невидимость)
        CreateSkill(db, 405, "Smoke Bomb", "Дымовая завеса, невидимость", CharacterClass.Rogue,
            SkillType.Buff, 20f, 40f, 0f, 0f, requiresTarget: false);

        // 6. Execute - казнь (большой урон по раненым)
        CreateSkill(db, 406, "Execute", "Огромный урон по врагам с низким HP", CharacterClass.Rogue,
            SkillType.Damage, 15f, 35f, 3f, 0.6f, baseDamage: 100f, strengthScaling: 30f);
    }

    // ===== PALADIN SKILLS =====
    private static void CreatePaladinSkills(SkillDatabase db)
    {
        Debug.Log("\n=== Создание скиллов Paladin ===");

        // 1. Holy Strike - святой удар
        CreateSkill(db, 501, "Holy Strike", "Удар святой силой", CharacterClass.Paladin,
            SkillType.Damage, 10f, 30f, 5f, 0.7f, baseDamage: 45f, strengthScaling: 18f);

        // 2. Bear Form - форма медведя ⭐
        CreateSkill(db, 502, "Bear Form", "Превращается в медведя с бонусами к HP и атаке", CharacterClass.Paladin,
            SkillType.Transformation, 40f, 70f, 0f, 1f, requiresTarget: false);

        // 3. Divine Shield - божественный щит
        CreateSkill(db, 503, "Divine Shield", "Временная неуязвимость", CharacterClass.Paladin,
            SkillType.Buff, 45f, 60f, 0f, 0.5f, requiresTarget: false);

        // 4. Lay on Hands - наложение рук (лечение)
        CreateSkill(db, 504, "Lay on Hands", "Сильное лечение союзника", CharacterClass.Paladin,
            SkillType.Heal, 25f, 50f, 10f, 1.5f, baseDamage: 100f, intelligenceScaling: 30f, canTargetAllies: true);

        // 5. Hammer of Justice - молот правосудия (стан)
        CreateSkill(db, 505, "Hammer of Justice", "Бросает молот, оглушает врага", CharacterClass.Paladin,
            SkillType.CrowdControl, 15f, 25f, 15f, 0.6f, baseDamage: 30f);

        // 6. Ressurection - воскрешение
        CreateSkill(db, 506, "Ressurection", "Воскрешает павшего союзника", CharacterClass.Paladin,
            SkillType.Ressurect, 60f, 100f, 10f, 3f, canTargetAllies: true);
    }

    /// <summary>
    /// Создать скилл
    /// </summary>
    private static void CreateSkill(SkillDatabase db, int skillId, string skillName, string description,
        CharacterClass characterClass, SkillType skillType, float cooldown, float manaCost, float castRange,
        float castTime, float baseDamage = 0f, float intelligenceScaling = 0f, float strengthScaling = 0f,
        float aoeRadius = 0f, int maxTargets = 1, bool requiresTarget = true, bool canTargetAllies = false,
        bool canTargetEnemies = true)
    {
        string path = $"Assets/Resources/Skills/{characterClass}_{skillName.Replace(" ", "")}.asset";

        SkillData existing = AssetDatabase.LoadAssetAtPath<SkillData>(path);
        if (existing != null)
        {
            Debug.Log($"  ⏭️  Скилл {skillName} уже существует, пропускаю");
            return;
        }

        SkillData skill = ScriptableObject.CreateInstance<SkillData>();
        skill.skillId = skillId;
        skill.skillName = skillName;
        skill.description = description;
        skill.characterClass = characterClass;
        skill.skillType = skillType;
        skill.cooldown = cooldown;
        skill.manaCost = manaCost;
        skill.castRange = castRange;
        skill.castTime = castTime;
        skill.baseDamageOrHeal = baseDamage;
        skill.intelligenceScaling = intelligenceScaling;
        skill.strengthScaling = strengthScaling;
        skill.aoeRadius = aoeRadius;
        skill.maxTargets = maxTargets;
        skill.requiresTarget = requiresTarget;
        skill.canTargetAllies = canTargetAllies;
        skill.canTargetEnemies = canTargetEnemies;
        skill.targetType = requiresTarget ? OldSkillTargetType.SingleTarget : OldSkillTargetType.Self;

        AssetDatabase.CreateAsset(skill, path);
        db.AddSkill(skill);

        Debug.Log($"  ✅ {skillName} создан");
    }
}
