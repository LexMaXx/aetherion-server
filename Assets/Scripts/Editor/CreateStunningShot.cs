using UnityEditor;
using UnityEngine;

/// <summary>
/// Создание скилла Stunning Shot для лучника
/// Одиночная стрела которая станит врага на 5 секунд
/// </summary>
public class CreateStunningShot : EditorWindow
{
    [MenuItem("Aetherion/Skills/Archer/Create Stunning Shot")]
    public static void CreateSkill()
    {
        // Создаём ScriptableObject
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ═══════════════════════════════════════════════════════════
        // ОСНОВНАЯ ИНФОРМАЦИЯ
        // ═══════════════════════════════════════════════════════════
        skill.skillId = 302;
        skill.skillName = "Stunning Shot";
        skill.description = "Выпускает заряженную стрелу, которая станит врага на 5 секунд. Враг не может двигаться, атаковать или использовать способности.";
        skill.characterClass = CharacterClass.Archer;

        // ═══════════════════════════════════════════════════════════
        // ПАРАМЕТРЫ ИСПОЛЬЗОВАНИЯ
        // ═══════════════════════════════════════════════════════════
        skill.cooldown = 15f; // Долгий кулдаун для мощного CC
        skill.manaCost = 40f;
        skill.castRange = 25f;
        skill.castTime = 0f; // Мгновенно
        skill.canUseWhileMoving = true;

        // ═══════════════════════════════════════════════════════════
        // ТИП СКИЛЛА
        // ═══════════════════════════════════════════════════════════
        skill.skillType = SkillConfigType.ProjectileDamage;
        skill.targetType = SkillTargetType.Enemy;
        skill.requiresTarget = true;
        skill.canTargetAllies = false;
        skill.canTargetEnemies = true;

        // ═══════════════════════════════════════════════════════════
        // УРОН
        // ═══════════════════════════════════════════════════════════
        skill.baseDamageOrHeal = 30f; // Небольшой урон (основная ценность - стан)
        skill.strengthScaling = 0f;
        skill.intelligenceScaling = 1.0f; // Умеренный скейлинг

        // ═══════════════════════════════════════════════════════════
        // ЭФФЕКТЫ - STUN НА 5 СЕКУНД
        // ═══════════════════════════════════════════════════════════
        EffectConfig stunEffect = new EffectConfig();
        stunEffect.effectType = EffectType.Stun;
        stunEffect.duration = 5f; // 5 секунд стана
        stunEffect.canStack = false; // Стан не стакается
        stunEffect.maxStacks = 1;

        // Визуальный эффект стана - электрические искры ОСТАЮТСЯ НА ВРАГЕ 5 секунд
        stunEffect.particleEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Electric C (Air).prefab"
        );


        skill.effects.Add(stunEffect);

        // ═══════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ═══════════════════════════════════════════════════════════

        // Префаб стрелы - специальная связывающая стрела для стана
        skill.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Projectiles/EntanglingArrowProjectile.prefab"
        );

        // Cast effect - нет (мгновенно)
        skill.castEffectPrefab = null;

        // Hit effect - электрический эффект при попадании
        skill.hitEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Electric C (Air).prefab"
        );

        // ═══════════════════════════════════════════════════════════
        // АНИМАЦИЯ
        // ═══════════════════════════════════════════════════════════
        skill.animationTrigger = "Attack"; // Используем стандартную анимацию атаки

        // ═══════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ
        // ═══════════════════════════════════════════════════════════
        string path = "Assets/Resources/Skills/Archer_StunningShot.asset";
        AssetDatabase.CreateAsset(skill, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ [CreateStunningShot] Скилл создан: {path}");
        Debug.Log($"   • Урон: {skill.baseDamageOrHeal}");
        Debug.Log($"   • STUN эффект: {stunEffect.duration} секунд");
        Debug.Log($"   • Блокирует: движение, атаки, скиллы");
        Debug.Log($"   • Cooldown: {skill.cooldown}с");

        Selection.activeObject = skill;
    }
}
