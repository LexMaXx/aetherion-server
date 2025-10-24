using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor скрипт для создания первого тестового скилла Mage_Fireball
/// </summary>
public class CreateMageFireball : EditorWindow
{
    [MenuItem("Tools/Skills/Create Mage Fireball")]
    public static void CreateFireballSkill()
    {
        // Создаём SkillConfig
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ═══════════════════════════════════════════════════════════
        // ОСНОВНАЯ ИНФОРМАЦИЯ
        // ═══════════════════════════════════════════════════════════
        skill.skillId = 201;
        skill.skillName = "Fireball";
        skill.description = "Запускает огненный шар, наносящий урон и накладывающий эффект горения";
        skill.characterClass = CharacterClass.Mage;

        // ═══════════════════════════════════════════════════════════
        // ПАРАМЕТРЫ ИСПОЛЬЗОВАНИЯ
        // ═══════════════════════════════════════════════════════════
        skill.cooldown = 6f;
        skill.manaCost = 30f;
        skill.castRange = 25f;
        skill.castTime = 0.8f;
        skill.canUseWhileMoving = false;

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
        skill.baseDamageOrHeal = 50f;
        skill.strengthScaling = 0f;
        skill.intelligenceScaling = 2.5f;
        // При 100 Intelligence: 50 + 100*2.5 = 300 урона

        // ═══════════════════════════════════════════════════════════
        // СНАРЯД
        // ═══════════════════════════════════════════════════════════
        // Загружаем префаб снаряда Fireball
        skill.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Projectiles/Fireball.prefab"
        );

        skill.projectileSpeed = 15f;
        skill.projectileLifetime = 5f;
        skill.projectileHoming = true;
        skill.homingSpeed = 10f;
        skill.homingRadius = 20f;

        // ═══════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ═══════════════════════════════════════════════════════════
        skill.hitEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Fire Explosion B 1.prefab"
        );

        // ═══════════════════════════════════════════════════════════
        // АНИМАЦИЯ
        // ═══════════════════════════════════════════════════════════
        skill.animationTrigger = "Attack";
        skill.animationSpeed = 1.5f;
        skill.projectileSpawnTiming = 0.6f;

        // ═══════════════════════════════════════════════════════════
        // ЗВУКИ
        // ═══════════════════════════════════════════════════════════
        skill.soundVolume = 0.7f;

        // ═══════════════════════════════════════════════════════════
        // ЭФФЕКТЫ (BURN)
        // ═══════════════════════════════════════════════════════════
        EffectConfig burnEffect = new EffectConfig();
        burnEffect.effectType = EffectType.Burn;
        burnEffect.duration = 5f;
        burnEffect.power = 0f; // Не используется для Burn
        burnEffect.damageOrHealPerTick = 10f;
        burnEffect.tickInterval = 1f;
        burnEffect.intelligenceScaling = 0.5f;
        burnEffect.strengthScaling = 0f;
        // При 100 Intelligence: 10 + 100*0.5 = 60 урона за тик
        // 5 тиков = 300 урона от DoT
        // Общий урон: 300 (прямой) + 300 (DoT) = 600

        burnEffect.canBeDispelled = true;
        burnEffect.canStack = false;
        burnEffect.maxStacks = 1;
        burnEffect.syncWithServer = true;

        skill.effects = new System.Collections.Generic.List<EffectConfig> { burnEffect };

        // ═══════════════════════════════════════════════════════════
        // СЕТЕВАЯ СИНХРОНИЗАЦИЯ
        // ═══════════════════════════════════════════════════════════
        skill.syncProjectiles = true;
        skill.syncHitEffects = true;
        skill.syncStatusEffects = true;

        // ═══════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ
        // ═══════════════════════════════════════════════════════════
        string path = "Assets/ScriptableObjects/Skills/Mage/Mage_Fireball.asset";

        // Проверяем, существует ли уже
        SkillConfig existing = AssetDatabase.LoadAssetAtPath<SkillConfig>(path);
        if (existing != null)
        {
            Debug.LogWarning("⚠️ Mage_Fireball уже существует! Удалите старый файл или переименуйте.");
            return;
        }

        AssetDatabase.CreateAsset(skill, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Выделяем созданный asset
        EditorGUIUtility.PingObject(skill);
        Selection.activeObject = skill;

        Debug.Log("✅ Mage_Fireball успешно создан!");
        Debug.Log($"📍 Путь: {path}");
        Debug.Log($"🔥 Урон: {skill.baseDamageOrHeal} + INT*{skill.intelligenceScaling}");
        Debug.Log($"🔥 Burn: {burnEffect.damageOrHealPerTick} + INT*{burnEffect.intelligenceScaling} урона/сек на {burnEffect.duration}с");
        Debug.Log($"⏱️ Кулдаун: {skill.cooldown}с, Мана: {skill.manaCost}");
        Debug.Log($"🎯 Дистанция: {skill.castRange}м");
        Debug.Log("\n📖 Следующий шаг: Добавьте Mage_Fireball к LocalPlayer → SkillExecutor → Equipped Skills[0]");
    }
}
