using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor script для создания Curse of Weakness - второй скилл Rogue (Necromancer)
/// Проклятие, которое снижает Perception противника до 1 на 10 секунд
/// </summary>
public class CreateCurseOfWeakness : MonoBehaviour
{
    [MenuItem("Aetherion/Skills/Rogue/Create Curse of Weakness")]
    public static void CreateSkill()
    {
        // ════════════════════════════════════════════════════════════
        // ОСНОВНЫЕ ПАРАМЕТРЫ
        // ════════════════════════════════════════════════════════════

        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        skill.skillId = 502;
        skill.skillName = "Curse of Weakness";
        skill.skillType = SkillConfigType.ProjectileDamage; // Снаряд с дебаффом
        skill.targetType = SkillTargetType.Enemy;
        skill.characterClass = CharacterClass.Rogue;

        // ════════════════════════════════════════════════════════════
        // УРОН (НЕБОЛЬШОЙ)
        // ════════════════════════════════════════════════════════════

        skill.baseDamageOrHeal = 20f;        // Небольшой базовый урон
        skill.intelligenceScaling = 0.5f;    // +50% от Intelligence (магический урон)

        // ════════════════════════════════════════════════════════════
        // ДАЛЬНОСТЬ И СКОРОСТЬ
        // ════════════════════════════════════════════════════════════

        skill.castRange = 20f;               // 20 метров дальность
        skill.projectileSpeed = 18f;         // Средняя скорость

        // ════════════════════════════════════════════════════════════
        // РЕСУРСЫ
        // ════════════════════════════════════════════════════════════

        skill.cooldown = 8f;                 // 8 секунд кулдаун
        skill.manaCost = 35f;                // 35 маны

        // ════════════════════════════════════════════════════════════
        // ЭФФЕКТ: DECREASE PERCEPTION
        // ════════════════════════════════════════════════════════════

        EffectConfig curseEffect = new EffectConfig();
        curseEffect.effectType = EffectType.DecreasePerception;
        curseEffect.duration = 10f;          // 10 секунд
        curseEffect.power = 1f;              // Устанавливает perception = 1
        curseEffect.canStack = false;        // Не стакается
        curseEffect.canBeDispelled = true;   // Можно снять
        curseEffect.syncWithServer = true;   // Синхронизация с сервером

        // Визуальный эффект проклятия (тёмная аура над врагом)
        curseEffect.particleEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Hit Electric C (Air)");

        skill.effects = new List<EffectConfig> { curseEffect };

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
        skill.castEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Hit Electric C (Air)");

        // Hit Effect: Взрыв тёмной энергии при попадании
        skill.hitEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Hit Electric C (Air)");

        // ════════════════════════════════════════════════════════════
        // ОПИСАНИЕ
        // ════════════════════════════════════════════════════════════

        skill.description = "Проклятие, которое снижает Perception противника до 1 на 10 секунд. Враг практически слепнет, его радиус обзора минимален.\n\n" +
                           "Урон: 20 + 50% Intelligence\n" +
                           "Эффект: Perception = 1 (10 секунд)\n" +
                           "Дальность: 20м\n" +
                           "Cooldown: 8 сек\n" +
                           "Mana: 35";

        // ════════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ ASSET
        // ════════════════════════════════════════════════════════════

        string path = "Assets/Resources/Skills/Rogue_CurseOfWeakness.asset";
        AssetDatabase.CreateAsset(skill, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ Curse of Weakness создан: {path}");
        Debug.Log($"⚔️ Урон: {skill.baseDamageOrHeal} + {skill.intelligenceScaling * 100}% INT");
        Debug.Log($"👁️ Эффект: Perception → 1 на {curseEffect.duration} секунд");
        Debug.Log($"🔮 Cooldown: {skill.cooldown}с, Mana: {skill.manaCost}");
        Debug.Log($"📏 Дальность: {skill.castRange}м");

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = skill;
    }
}
