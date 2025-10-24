using UnityEngine;
using UnityEditor;

/// <summary>
/// Создаёт Lightning Storm SkillConfig для мага
/// AOE скилл с chain lightning эффектом
/// </summary>
public class CreateLightningStorm : Editor
{
    [MenuItem("Aetherion/Skills/Create Lightning Storm (Mage)")]
    public static void Create()
    {
        // Создаём SkillConfig
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ═══════════════════════════════════════════════════════════
        // БАЗОВАЯ ИНФОРМАЦИЯ
        // ═══════════════════════════════════════════════════════════
        skill.skillId = 203;
        skill.skillName = "Lightning Storm";
        skill.description = "Вызывает шторм молний, поражающий всех врагов в радиусе. Молнии перепрыгивают на соседних врагов.";
        skill.icon = null; // TODO: добавить иконку

        // ═══════════════════════════════════════════════════════════
        // КЛАСС
        // ═══════════════════════════════════════════════════════════
        skill.characterClass = CharacterClass.Mage;

        // ═══════════════════════════════════════════════════════════
        // КАСТ И ИСПОЛЬЗОВАНИЕ
        // ═══════════════════════════════════════════════════════════
        skill.castTime = 0f; // Instant
        skill.castRange = 0f; // AOE вокруг себя
        skill.canUseWhileMoving = false;

        // ═══════════════════════════════════════════════════════════
        // ТИП СКИЛЛА - AOE DAMAGE С CHAIN LIGHTNING
        // ═══════════════════════════════════════════════════════════
        skill.skillType = SkillConfigType.AOEDamage;
        skill.targetType = SkillTargetType.NoTarget; // Не требует цель
        skill.requiresTarget = false;
        skill.canTargetAllies = false;
        skill.canTargetEnemies = true;

        // ═══════════════════════════════════════════════════════════
        // УРОН
        // ═══════════════════════════════════════════════════════════
        skill.baseDamageOrHeal = 60f;
        skill.strengthScaling = 0f;
        skill.intelligenceScaling = 2.5f;
        // При 100 Intelligence: 60 + 100*2.5 = 310 урона
        // При 9 INT (тест): 60 + 9*2.5 = 82.5 урона

        // ═══════════════════════════════════════════════════════════
        // AOE ПАРАМЕТРЫ
        // ═══════════════════════════════════════════════════════════
        skill.aoeRadius = 10f; // 10 метров радиус
        skill.maxTargets = 5; // Максимум 5 врагов в начальном AOE

        // ═══════════════════════════════════════════════════════════
        // CHAIN LIGHTNING ПАРАМЕТРЫ (через customData)
        // ═══════════════════════════════════════════════════════════
        skill.customData = new SkillCustomData
        {
            chainCount = 3,           // Молния перепрыгивает 3 раза
            chainRadius = 8f,         // Радиус поиска следующей цели - 8м
            chainDamageMultiplier = 0.7f  // Каждый прыжок наносит 70% урона
        };

        // ═══════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ═══════════════════════════════════════════════════════════
        // Эффект каста (молния сверху на мага)
        skill.castEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Electric C (Air).prefab"
        );

        // Эффект попадания на каждого врага (электрический удар)
        skill.hitEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Electric C (Air).prefab"
        );

        // AOE эффект (электрический шторм вокруг мага)
        skill.aoeEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Light B (Air).prefab"
        );

        // ═══════════════════════════════════════════════════════════
        // РЕСУРСЫ
        // ═══════════════════════════════════════════════════════════
        skill.cooldown = 12f;
        skill.manaCost = 60f;

        // ═══════════════════════════════════════════════════════════
        // ДОПОЛНИТЕЛЬНЫЕ ЭФФЕКТЫ
        // ═══════════════════════════════════════════════════════════
        // Пока нет дополнительных эффектов (можно добавить например Stun)

        // ═══════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ
        // ═══════════════════════════════════════════════════════════
        string path = "Assets/Resources/Skills/Mage_LightningStorm.asset";

        AssetDatabase.CreateAsset(skill, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = skill;

        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("⚡ Lightning Storm создан!");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"📍 Путь: {path}");
        Debug.Log($"🆔 Skill ID: {skill.skillId}");
        Debug.Log($"💥 Базовый урон: {skill.baseDamageOrHeal}");
        Debug.Log($"🧠 Intelligence scaling: {skill.intelligenceScaling}x");
        Debug.Log($"🌀 AOE радиус: {skill.aoeRadius}м");
        Debug.Log($"⚡ Chain прыжков: {skill.customData.chainCount}");
        Debug.Log($"🔗 Chain радиус: {skill.customData.chainRadius}м");
        Debug.Log($"📉 Chain урон: {skill.customData.chainDamageMultiplier * 100}% каждый прыжок");
        Debug.Log($"⏱️ Cooldown: {skill.cooldown}с");
        Debug.Log($"💧 Mana: {skill.manaCost}");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("📝 Lightning Storm работает так:");
        Debug.Log("1. Вызывает молнии на всех врагов в радиусе 10м");
        Debug.Log("2. После попадания молния перепрыгивает на соседа");
        Debug.Log("3. Максимум 3 прыжка, каждый наносит 70% урона");
        Debug.Log("4. Каждый прыжок ищет цель в радиусе 8м");
        Debug.Log("═══════════════════════════════════════════════════════");
    }
}
