using UnityEngine;
using UnityEditor;

/// <summary>
/// Пересоздание всех недостающих SkillConfig
/// </summary>
public class RecreateAllMissingSkills : MonoBehaviour
{
    [MenuItem("Aetherion/Skills/Recreate ALL Missing Skills (5 skills)")]
    public static void RecreateAll()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("ПЕРЕСОЗДАНИЕ НЕДОСТАЮЩИХ SKILLCONFIG");
        Debug.Log("═══════════════════════════════════════════════════════\n");

        int created = 0;

        // 1. Mage_Fireball (201)
        if (RecreateMageFireball())
        {
            created++;
            Debug.Log("[RecreateAllMissingSkills] ✅ 1/5 Mage_Fireball создан\n");
        }

        // 2. Mage_IceNova (202)
        if (RecreateMageIceNova())
        {
            created++;
            Debug.Log("[RecreateAllMissingSkills] ✅ 2/5 Mage_IceNova создан\n");
        }

        // 3. Archer_EntanglingShot (305)
        if (RecreateArcherEntanglingShot())
        {
            created++;
            Debug.Log("[RecreateAllMissingSkills] ✅ 3/5 Archer_EntanglingShot создан\n");
        }

        // 4. Rogue_SummonSkeletons (601)
        if (RecreateRogueSummonSkeletons())
        {
            created++;
            Debug.Log("[RecreateAllMissingSkills] ✅ 4/5 Rogue_SummonSkeletons создан\n");
        }

        // 5. Paladin_BearForm (501)
        if (RecreatePaladinBearForm())
        {
            created++;
            Debug.Log("[RecreateAllMissingSkills] ✅ 5/5 Paladin_BearForm создан\n");
        }

        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"ИТОГИ: Создано {created}/5 скиллов");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("💡 ВАЖНО: Запусти 'Test Skill Loading - All Classes' чтобы проверить!");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static bool RecreateMageFireball()
    {
        // Удаляем старый файл если есть
        string path = "Assets/Resources/Skills/Mage_Fireball.asset";
        if (System.IO.File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
            Debug.Log($"[RecreateAllMissingSkills] 🗑️ Удалён старый {path}");
        }

        // Создаём новый SkillConfig
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        skill.skillId = 201;
        skill.skillName = "Fireball";
        skill.description = "Огненный шар с большим уроном";
        skill.characterClass = CharacterClass.Mage;
        skill.skillType = SkillConfigType.ProjectileDamage;
        skill.targetType = SkillTargetType.Enemy;
        skill.cooldown = 6f;
        skill.manaCost = 40f;
        skill.castRange = 20f;
        skill.castTime = 0.8f;
        skill.baseDamageOrHeal = 60f;
        skill.intelligenceScaling = 25f;
        skill.animationTrigger = "Attack";
        skill.canUseWhileMoving = true;

        // Эффект горения
        EffectConfig burnEffect = new EffectConfig();
        burnEffect.effectType = EffectType.DamageOverTime;
        burnEffect.duration = 3f;
        burnEffect.damageOrHealPerTick = 10f;
        burnEffect.tickInterval = 1f;
        skill.effects.Add(burnEffect);

        // Сохраняем
        AssetDatabase.CreateAsset(skill, path);
        Debug.Log($"[RecreateAllMissingSkills] ✅ Создан: {path}");

        return true;
    }

    private static bool RecreateMageIceNova()
    {
        string path = "Assets/Resources/Skills/Mage_IceNova.asset";
        if (System.IO.File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }

        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        skill.skillId = 202;
        skill.skillName = "Ice Nova";
        skill.description = "AOE замораживание врагов вокруг";
        skill.characterClass = CharacterClass.Mage;
        skill.skillType = SkillConfigType.AOEDamage;
        skill.targetType = SkillTargetType.NoTarget;
        skill.aoeRadius = 8f;
        skill.cooldown = 10f;
        skill.manaCost = 50f;
        skill.baseDamageOrHeal = 40f;
        skill.intelligenceScaling = 20f;
        skill.animationTrigger = "Attack";
        skill.canUseWhileMoving = false;

        // Эффект замедления
        EffectConfig slowEffect = new EffectConfig();
        slowEffect.effectType = EffectType.DecreaseSpeed;
        slowEffect.duration = 3f;
        slowEffect.power = 50f; // -50% скорости
        skill.effects.Add(slowEffect);

        AssetDatabase.CreateAsset(skill, path);
        Debug.Log($"[RecreateAllMissingSkills] ✅ Создан: {path}");

        return true;
    }

    private static bool RecreateArcherEntanglingShot()
    {
        string path = "Assets/Resources/Skills/Archer_EntanglingShot.asset";
        if (System.IO.File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }

        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        skill.skillId = 305;
        skill.skillName = "Entangling Shot";
        skill.description = "Стрела которая обездвиживает врага на 2 секунды";
        skill.characterClass = CharacterClass.Archer;
        skill.skillType = SkillConfigType.ProjectileDamage;
        skill.targetType = SkillTargetType.Enemy;
        skill.cooldown = 15f;
        skill.manaCost = 40f;
        skill.castRange = 20f;
        skill.baseDamageOrHeal = 30f;
        skill.strengthScaling = 15f; // Archer использует physical damage
        skill.animationTrigger = "Attack";

        // Эффект Root (обездвиживание)
        EffectConfig rootEffect = new EffectConfig();
        rootEffect.effectType = EffectType.Root;
        rootEffect.duration = 2f;
        skill.effects.Add(rootEffect);

        AssetDatabase.CreateAsset(skill, path);
        Debug.Log($"[RecreateAllMissingSkills] ✅ Создан: {path}");

        return true;
    }

    private static bool RecreateRogueSummonSkeletons()
    {
        string path = "Assets/Resources/Skills/Rogue_SummonSkeletons.asset";
        if (System.IO.File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }

        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        skill.skillId = 601;
        skill.skillName = "Summon Skeletons";
        skill.description = "Призывает 2 скелетов на 30 секунд";
        skill.characterClass = CharacterClass.Rogue; // Necromancer = Rogue в enum
        skill.skillType = SkillConfigType.Summon;
        skill.targetType = SkillTargetType.Ground;
        skill.cooldown = 60f;
        skill.manaCost = 80f;
        skill.summonCount = 2;
        skill.summonDuration = 30f;
        skill.animationTrigger = "Attack";

        AssetDatabase.CreateAsset(skill, path);
        Debug.Log($"[RecreateAllMissingSkills] ✅ Создан: {path}");

        return true;
    }

    private static bool RecreatePaladinBearForm()
    {
        string path = "Assets/Resources/Skills/Paladin_BearForm.asset";
        if (System.IO.File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }

        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        skill.skillId = 501;
        skill.skillName = "Bear Form";
        skill.description = "Превращение в медведя: +100% HP, +100% физ. урона на 30 секунд";
        skill.characterClass = CharacterClass.Paladin;
        skill.skillType = SkillConfigType.Transformation;
        skill.targetType = SkillTargetType.Self;
        skill.cooldown = 120f;
        skill.manaCost = 60f;
        skill.transformationDuration = 30f;
        skill.hpBonusPercent = 100f;
        // Физический урон будет через strengthScaling или effects
        skill.animationTrigger = "Attack";

        // Добавим эффект увеличения атаки
        EffectConfig attackBonus = new EffectConfig();
        attackBonus.effectType = EffectType.IncreaseAttack;
        attackBonus.duration = 30f;
        attackBonus.power = 100f; // +100% атаки
        skill.effects.Add(attackBonus);

        AssetDatabase.CreateAsset(skill, path);
        Debug.Log($"[RecreateAllMissingSkills] ✅ Создан: {path}");

        return true;
    }
}
