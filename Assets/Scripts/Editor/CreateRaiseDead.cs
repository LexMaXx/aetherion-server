using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor script для создания Raise Dead - пятый скилл Rogue (Necromancer)
/// Призывает временного скелета-миньона для помощи в бою
/// </summary>
public class CreateRaiseDead : MonoBehaviour
{
    [MenuItem("Aetherion/Skills/Rogue/Create Raise Dead")]
    public static void CreateSkill()
    {
        // ════════════════════════════════════════════════════════════
        // ОСНОВНЫЕ ПАРАМЕТРЫ
        // ════════════════════════════════════════════════════════════

        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        skill.skillId = 505;
        skill.skillName = "Raise Dead";
        skill.skillType = SkillConfigType.Summon; // Призыв миньона
        skill.targetType = SkillTargetType.Self;
        skill.characterClass = CharacterClass.Rogue;

        // ════════════════════════════════════════════════════════════
        // ПАРАМЕТРЫ МИНЬОНА
        // ════════════════════════════════════════════════════════════

        skill.baseDamageOrHeal = 30f;        // Урон скелета (базовый)
        skill.intelligenceScaling = 0.5f;    // +50% от Intelligence некроманта

        // ════════════════════════════════════════════════════════════
        // РЕСУРСЫ И КУЛДАУН
        // ════════════════════════════════════════════════════════════

        skill.cooldown = 30f;                // 30 секунд кулдаун
        skill.manaCost = 50f;                // 50 маны (дорогое заклинание)

        // ════════════════════════════════════════════════════════════
        // ЭФФЕКТ: SUMMON MINION
        // ════════════════════════════════════════════════════════════

        EffectConfig summonEffect = new EffectConfig();
        summonEffect.effectType = EffectType.SummonMinion;
        summonEffect.duration = 20f;            // 20 секунд существования
        summonEffect.power = 30f;               // Базовый урон миньона
        summonEffect.canStack = false;          // Только 1 скелет одновременно
        summonEffect.canBeDispelled = false;    // Нельзя диспелить призыв
        summonEffect.syncWithServer = true;     // Синхронизация с сервером

        // TODO: Визуальный эффект призыва (тёмная некромантская аура)
        summonEffect.particleEffectPrefab = Resources.Load<GameObject>("Effects/CFXR Magic Poof");

        skill.effects = new List<EffectConfig> { summonEffect };

        // ════════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ════════════════════════════════════════════════════════════

        // Cast Effect: Тёмная некромантская энергия при призыве
        skill.castEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Fire Explosion B 1");

        // Caster Effect: Магический круг призыва под некромантом
        skill.casterEffectPrefab = Resources.Load<GameObject>("Effects/CFXR Magic Poof");

        // ════════════════════════════════════════════════════════════
        // ОПИСАНИЕ
        // ════════════════════════════════════════════════════════════

        skill.description = "Призывает скелета-воина на 20 секунд. Скелет атакует ближайших врагов, нанося урон на основе Intelligence некроманта. Можно призвать только одного скелета одновременно.\\n\\n" +
                           "Урон скелета: 30 + 50% Intelligence\\n" +
                           "Длительность: 20 секунд\\n" +
                           "Cooldown: 30 сек\\n" +
                           "Mana: 50";

        // ════════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ ASSET
        // ════════════════════════════════════════════════════════════

        string path = "Assets/Resources/Skills/Rogue_RaiseDead.asset";
        AssetDatabase.CreateAsset(skill, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ Raise Dead создан: {path}");
        Debug.Log($"💀 Урон скелета: {skill.baseDamageOrHeal} + {skill.intelligenceScaling * 100}% INT");
        Debug.Log($"⏱️ Длительность: {summonEffect.duration} секунд");
        Debug.Log($"🔮 Cooldown: {skill.cooldown}с, Mana: {skill.manaCost}");

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = skill;
    }
}
