using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor script для создания Soul Drain - первый скилл Rogue (Necromancer)
/// Скилл наносит урон врагу и восстанавливает HP на основе нанесённого урона (life steal)
/// </summary>
public class CreateSoulDrain : MonoBehaviour
{
    [MenuItem("Aetherion/Skills/Rogue/Create Soul Drain")]
    public static void CreateSkill()
    {
        // ════════════════════════════════════════════════════════════
        // ОСНОВНЫЕ ПАРАМЕТРЫ
        // ════════════════════════════════════════════════════════════

        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        skill.skillId = 501;
        skill.skillName = "Soul Drain";
        skill.skillType = SkillConfigType.DamageAndHeal; // Урон + лечение
        skill.targetType = SkillTargetType.Enemy;
        skill.characterClass = CharacterClass.Rogue;

        // ════════════════════════════════════════════════════════════
        // УРОН
        // ════════════════════════════════════════════════════════════

        skill.baseDamageOrHeal = 40f;        // Базовый урон
        skill.intelligenceScaling = 1.2f;    // +120% от Intelligence (некромант - маг)
        skill.strengthScaling = 0.3f;        // +30% от Strength (небольшой физический компонент)

        // ════════════════════════════════════════════════════════════
        // LIFE STEAL (ВАМПИРИЗМ)
        // ════════════════════════════════════════════════════════════

        // Лечим на 100% от нанесённого урона
        skill.lifeStealPercent = 100f;       // 100% вампиризм

        // ════════════════════════════════════════════════════════════
        // ДАЛЬНОСТЬ И СКОРОСТЬ
        // ════════════════════════════════════════════════════════════

        skill.castRange = 15f;               // 15 метров дальность
        skill.projectileSpeed = 20f;         // Быстрый снаряд

        // ════════════════════════════════════════════════════════════
        // РЕСУРСЫ
        // ════════════════════════════════════════════════════════════

        skill.cooldown = 4f;                 // Короткий кулдаун (спамабельный скилл)
        skill.manaCost = 25f;                // Низкая стоимость маны

        // ════════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ════════════════════════════════════════════════════════════

        // Снаряд: Ethereal Skull (призрачный череп)
        GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Projectiles/Ethereal_Skull_1020210937_texture.prefab");

        if (projectilePrefab == null)
        {
            // Fallback: SoulShardsProjectile
            projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Projectiles/SoulShardsProjectile.prefab");
        }

        skill.projectilePrefab = projectilePrefab;

        // Cast Effect: Тёмная энергия при касте
        skill.castEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Hit Electric C (Air)");

        // Hit Effect: Взрыв тёмной энергии при попадании
        skill.hitEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Hit Electric C (Air)");

        // ════════════════════════════════════════════════════════════
        // ЭФФЕКТЫ НА КАСТЕРЕ (ЛЕЧЕНИЕ)
        // ════════════════════════════════════════════════════════════

        // Визуальный эффект лечения на кастере
        skill.casterEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Hit Light B (Air)");

        // ════════════════════════════════════════════════════════════
        // ОПИСАНИЕ
        // ════════════════════════════════════════════════════════════

        skill.description = "Выстреливает призрачный череп, который наносит урон врагу и восстанавливает HP некроманту на 100% от нанесённого урона.\n\n" +
                           "Урон: 40 + 120% Intelligence + 30% Strength\n" +
                           "Life Steal: 100% от нанесённого урона\n" +
                           "Дальность: 15м\n" +
                           "Cooldown: 4 сек\n" +
                           "Mana: 25";

        // ════════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ ASSET
        // ════════════════════════════════════════════════════════════

        string path = "Assets/Resources/Skills/Rogue_SoulDrain.asset";
        AssetDatabase.CreateAsset(skill, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ Soul Drain создан: {path}");
        Debug.Log($"⚔️ Урон: {skill.baseDamageOrHeal} + {skill.intelligenceScaling * 100}% INT + {skill.strengthScaling * 100}% STR");
        Debug.Log($"💚 Life Steal: {skill.lifeStealPercent}% от урона");
        Debug.Log($"🔮 Cooldown: {skill.cooldown}с, Mana: {skill.manaCost}");
        Debug.Log($"📏 Дальность: {skill.castRange}м");

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = skill;
    }
}
