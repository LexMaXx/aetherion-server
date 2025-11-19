using UnityEditor;
using UnityEngine;

/// <summary>
/// Создание скилла Rain of Arrows для лучника
/// 3 стрелы подряд с ускоренной анимацией
/// </summary>
public class CreateRainOfArrows : EditorWindow
{
    [MenuItem("Aetherion/Skills/Archer/Create Rain of Arrows")]
    public static void CreateSkill()
    {
        // Создаём ScriptableObject
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ═══════════════════════════════════════════════════════════
        // ОСНОВНАЯ ИНФОРМАЦИЯ
        // ═══════════════════════════════════════════════════════════
        skill.skillId = 301;
        skill.skillName = "Rain of Arrows";
        skill.description = "Выпускает 3 стрелы подряд с ускоренной анимацией атаки.";
        skill.characterClass = CharacterClass.Archer;

        // ═══════════════════════════════════════════════════════════
        // ПАРАМЕТРЫ ИСПОЛЬЗОВАНИЯ
        // ═══════════════════════════════════════════════════════════
        skill.cooldown = 8f;
        skill.manaCost = 30f;
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
        skill.baseDamageOrHeal = 40f; // 40 урона за стрелу × 3 = 120 суммарно
        skill.strengthScaling = 0f; // Лучник не использует Strength
        skill.intelligenceScaling = 1.5f; // Скейлинг от Intelligence

        // ═══════════════════════════════════════════════════════════
        // ЭФФЕКТЫ
        // ═══════════════════════════════════════════════════════════
        // Нет дополнительных эффектов (чистый урон)

        // ═══════════════════════════════════════════════════════════
        // MULTI-HIT (3 СТРЕЛЫ ПОДРЯД)
        // ═══════════════════════════════════════════════════════════
        skill.customData = new SkillCustomData
        {
            hitCount = 3,           // 3 стрелы
            hitDelay = 0.15f        // 0.15 секунды между стрелами (быстро!)
        };

        // ═══════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ═══════════════════════════════════════════════════════════

        // Префаб стрелы (стандартная атака лучника)
        skill.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Projectiles/ArrowProjectile.prefab"
        );

        // Cast effect - нет (используем анимацию атаки)
        skill.castEffectPrefab = null;

        // Hit effect - стандартный эффект попадания стрелы
        skill.hitEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Light B (Air).prefab"
        );

        // ═══════════════════════════════════════════════════════════
        // АНИМАЦИЯ
        skill.animationTrigger = "Attack"; // Используем стандартную анимацию атаки

        // ═══════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ
        // ═══════════════════════════════════════════════════════════
        string path = "Assets/Resources/Skills/Archer_RainOfArrows.asset";
        AssetDatabase.CreateAsset(skill, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ [CreateRainOfArrows] Скилл создан: {path}");
        Debug.Log($"   • 3 стрелы подряд (интервал 0.15с)");
        Debug.Log($"   • Урон: {skill.baseDamageOrHeal} × 3 = {skill.baseDamageOrHeal * 3}");

        Selection.activeObject = skill;
    }
}
