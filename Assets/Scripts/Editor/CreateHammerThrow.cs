using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Editor скрипт для создания скилла "Hammer Throw" (Бросок Молота)
/// Воин бросает кувалду, которая станит врага на 3 секунды
/// Аналог Stunning Shot лучника, но с молотом
/// </summary>
public class CreateHammerThrow
{
    [MenuItem("Aetherion/Skills/Create Hammer Throw (Warrior)")]
    public static void Create()
    {
        // Создаём ScriptableObject
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ═══════════════════════════════════════════════════════════════
        // ОСНОВНЫЕ ПАРАМЕТРЫ
        // ═══════════════════════════════════════════════════════════════
        skill.skillId = 402;
        skill.skillName = "Hammer Throw";
        skill.skillType = SkillConfigType.ProjectileDamage;
        skill.targetType = SkillTargetType.Enemy;
        skill.requiresTarget = true;
        skill.characterClass = CharacterClass.Warrior;

        skill.cooldown = 12f; // 12 секунд кулдаун (как у Charge)
        skill.manaCost = 35f; // Средний расход маны
        skill.castRange = 20f; // 20 метров дальность
        skill.castTime = 0f; // Мгновенно
        skill.canUseWhileMoving = true;

        // ═══════════════════════════════════════════════════════════════
        // УРОН
        // ═══════════════════════════════════════════════════════════════
        skill.baseDamageOrHeal = 50f; // Больше урона чем у стрелы (молот тяжелее!)
        skill.strengthScaling = 1.5f; // Сильный скейлинг от силы (воин!)
        skill.intelligenceScaling = 0f;

        // ═══════════════════════════════════════════════════════════════
        // ЭФФЕКТ ОГЛУШЕНИЯ (STUN) - 3 СЕКУНДЫ
        // ═══════════════════════════════════════════════════════════════
        EffectConfig stunEffect = new EffectConfig();
        stunEffect.effectType = EffectType.Stun;
        stunEffect.duration = 3f; // 3 секунды стана (меньше чем у лучника)
        stunEffect.canStack = false;
        stunEffect.maxStacks = 1;

        // Визуальный эффект стана - электрические искры (как у лучника)
        stunEffect.particleEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Electric C (Air).prefab"
        );

        skill.effects = new List<EffectConfig> { stunEffect };

        // ═══════════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ═══════════════════════════════════════════════════════════════

        // Префаб кувалды - вращающийся молот (как фаербол)
        skill.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Projectiles/HammerProjectile.prefab"
        );

        // Cast effect - нет (мгновенно)
        skill.castEffectPrefab = null;

        // Hit effect - электрический взрыв при попадании
        skill.hitEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Electric C (Air).prefab"
        );

        // ═══════════════════════════════════════════════════════════════
        // НАСТРОЙКИ СНАРЯДА
        // ═══════════════════════════════════════════════════════════════
        skill.projectileSpeed = 15f; // Средняя скорость (как у фаербола)
        skill.projectileLifetime = 3f; // 3 секунды существования

        // ═══════════════════════════════════════════════════════════════
        // АНИМАЦИЯ
        // ═══════════════════════════════════════════════════════════════
        skill.animationTrigger = "Attack"; // Стандартная анимация атаки

        // ═══════════════════════════════════════════════════════════════
        // ОПИСАНИЕ
        // ═══════════════════════════════════════════════════════════════
        skill.description = "Воин бросает кувалду в выбранного врага (макс. 20м). При попадании наносит урон и оглушает цель на 3 секунды, блокируя движение, атаки и скиллы.";

        // ═══════════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ
        // ═══════════════════════════════════════════════════════════════
        string path = "Assets/Resources/Skills/Warrior_HammerThrow.asset";

        // Удаляем старый файл если существует
        AssetDatabase.DeleteAsset(path);

        // Создаём новый
        AssetDatabase.CreateAsset(skill, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Выделяем созданный ассет
        Selection.activeObject = skill;
        EditorGUIUtility.PingObject(skill);

        // ═══════════════════════════════════════════════════════════════
        // ИНФОРМАЦИЯ
        // ═══════════════════════════════════════════════════════════════
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("🔨 Hammer Throw создан!");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"📍 Путь: {path}");
        Debug.Log($"🆔 Skill ID: {skill.skillId}");
        Debug.Log($"🎯 Тип: Projectile + CC (снаряд-стан)");
        Debug.Log($"📏 Дальность: {skill.castRange}м");
        Debug.Log($"💥 Урон: {skill.baseDamageOrHeal} + {skill.strengthScaling * 100}% от силы");
        Debug.Log($"⚡ Эффект: Stun на {stunEffect.duration} секунды");
        Debug.Log($"✨ Визуальные эффекты:");
        Debug.Log($"   - Снаряд: HammerProjectile (вращающийся молот)");
        Debug.Log($"   - Попадание: CFXR3 Hit Electric C (взрыв искр)");
        Debug.Log($"   - Стан: CFXR3 Hit Electric C (искры на враге 3 сек)");
        Debug.Log($"⏱️ Cooldown: {skill.cooldown}с");
        Debug.Log($"💧 Mana: {skill.manaCost}");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("📊 КАК РАБОТАЕТ:");
        Debug.Log("  1. Выбираешь врага (ЛКМ)");
        Debug.Log("  2. Используешь Hammer Throw (клавиша скилла)");
        Debug.Log("  3. Кувалда летит к врагу (вращается как фаербол)");
        Debug.Log("  4. При попадании - взрыв электрических искр");
        Debug.Log("  5. Враг оглушён на 3 секунды + искры вокруг него");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("💡 СРАВНЕНИЕ:");
        Debug.Log("  Stunning Shot (Лучник):");
        Debug.Log("    • Урон: 30 + 100% Int");
        Debug.Log("    • Стан: 5 секунд");
        Debug.Log("    • Снаряд: EntanglingArrow");
        Debug.Log("  Hammer Throw (Воин):");
        Debug.Log("    • Урон: 50 + 150% Str (БОЛЬШЕ УРОНА!)");
        Debug.Log("    • Стан: 3 секунды (короче)");
        Debug.Log("    • Снаряд: HammerProjectile (МОЛОТ!)");
        Debug.Log("═══════════════════════════════════════════════════════");
    }
}
