using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor script для создания Crippling Curse - третий скилл Rogue (Necromancer)
/// Проклятие, которое замедляет врага на 80% на несколько секунд
/// </summary>
public class CreateCripplingCurse : MonoBehaviour
{
    [MenuItem("Aetherion/Skills/Rogue/Create Crippling Curse")]
    public static void CreateSkill()
    {
        // ════════════════════════════════════════════════════════════
        // ОСНОВНЫЕ ПАРАМЕТРЫ
        // ════════════════════════════════════════════════════════════

        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        skill.skillId = 503;
        skill.skillName = "Crippling Curse";
        skill.skillType = SkillConfigType.ProjectileDamage; // Снаряд с дебаффом
        skill.targetType = SkillTargetType.Enemy;
        skill.characterClass = CharacterClass.Rogue;

        // ════════════════════════════════════════════════════════════
        // УРОН (НЕБОЛЬШОЙ)
        // ════════════════════════════════════════════════════════════

        skill.baseDamageOrHeal = 15f;        // Небольшой базовый урон
        skill.intelligenceScaling = 0.3f;    // +30% от Intelligence

        // ════════════════════════════════════════════════════════════
        // ДАЛЬНОСТЬ И СКОРОСТЬ
        // ════════════════════════════════════════════════════════════

        skill.castRange = 18f;               // 18 метров дальность
        skill.projectileSpeed = 16f;         // Средняя скорость

        // ════════════════════════════════════════════════════════════
        // РЕСУРСЫ
        // ════════════════════════════════════════════════════════════

        skill.cooldown = 10f;                // 10 секунд кулдаун
        skill.manaCost = 30f;                // 30 маны

        // ════════════════════════════════════════════════════════════
        // ЭФФЕКТ: DECREASE SPEED (ЗАМЕДЛЕНИЕ)
        // ════════════════════════════════════════════════════════════

        EffectConfig slowEffect = new EffectConfig();
        slowEffect.effectType = EffectType.DecreaseSpeed;
        slowEffect.duration = 6f;            // 6 секунд
        slowEffect.power = 80f;              // 80% замедление
        slowEffect.canStack = false;         // Не стакается
        slowEffect.canBeDispelled = true;    // Можно снять
        slowEffect.syncWithServer = true;    // Синхронизация с сервером

        // Визуальный эффект проклятия (фиолетовая/тёмная аура над врагом)
        slowEffect.particleEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Hit Ice B (Air)");

        skill.effects = new List<EffectConfig> { slowEffect };

        // ════════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ════════════════════════════════════════════════════════════

        // Снаряд: SoulShardsProjectile (тёмная энергия)
        GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Projectiles/SoulShardsProjectile.prefab");

        if (projectilePrefab == null)
        {
            // Fallback: Ethereal Skull
            projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Projectiles/Ethereal_Skull_1020210937_texture.prefab");
        }

        skill.projectilePrefab = projectilePrefab;

        // Cast Effect: Тёмная энергия при касте
        skill.castEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Hit Ice B (Air)");

        // Hit Effect: Ледяной взрыв при попадании (замедление)
        skill.hitEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Hit Ice B (Air)");

        // ════════════════════════════════════════════════════════════
        // ОПИСАНИЕ
        // ════════════════════════════════════════════════════════════

        skill.description = "Калечащее проклятие, которое замедляет врага на 80% на 6 секунд. Враг еле двигается, позволяя легко убежать или добить.\n\n" +
                           "Урон: 15 + 30% Intelligence\n" +
                           "Эффект: -80% скорости (6 секунд)\n" +
                           "Дальность: 18м\n" +
                           "Cooldown: 10 сек\n" +
                           "Mana: 30";

        // ════════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ ASSET
        // ════════════════════════════════════════════════════════════

        string path = "Assets/Resources/Skills/Rogue_CripplingCurse.asset";
        AssetDatabase.CreateAsset(skill, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ Crippling Curse создан: {path}");
        Debug.Log($"⚔️ Урон: {skill.baseDamageOrHeal} + {skill.intelligenceScaling * 100}% INT");
        Debug.Log($"🐌 Эффект: -80% скорости на {slowEffect.duration} секунд");
        Debug.Log($"🔮 Cooldown: {skill.cooldown}с, Mana: {skill.manaCost}");
        Debug.Log($"📏 Дальность: {skill.castRange}м");

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = skill;
    }
}
