using UnityEngine;
using UnityEditor;

/// <summary>
/// Создаёт Meteor SkillConfig для мага
/// Powerful AOE с cast time и зоной огня
/// </summary>
public class CreateMeteor : Editor
{
    [MenuItem("Aetherion/Skills/Create Meteor (Mage)")]
    public static void Create()
    {
        // Создаём SkillConfig
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ═══════════════════════════════════════════════════════════
        // БАЗОВАЯ ИНФОРМАЦИЯ
        // ═══════════════════════════════════════════════════════════
        skill.skillId = 205;
        skill.skillName = "Meteor";
        skill.description = "Вызывает метеорит с неба, наносящий огромный урон в области. Оставляет горящую зону на земле.";
        skill.icon = null; // TODO: добавить иконку

        // ═══════════════════════════════════════════════════════════
        // КЛАСС
        // ═══════════════════════════════════════════════════════════
        skill.characterClass = CharacterClass.Mage;

        // ═══════════════════════════════════════════════════════════
        // КАСТ И ИСПОЛЬЗОВАНИЕ
        // ═══════════════════════════════════════════════════════════
        skill.castTime = 2f; // 2 секунды каст (можно прервать)
        skill.castRange = 20f; // Максимум 20 метров
        skill.canUseWhileMoving = false; // Нельзя двигаться во время каста

        // ═══════════════════════════════════════════════════════════
        // ТИП СКИЛЛА - AOE DAMAGE + GROUND TARGET
        // ═══════════════════════════════════════════════════════════
        skill.skillType = SkillConfigType.AOEDamage;
        skill.targetType = SkillTargetType.Ground; // Ground target
        skill.requiresTarget = false; // Ground target не требует Entity цели
        skill.canTargetAllies = false;
        skill.canTargetEnemies = true;

        // ═══════════════════════════════════════════════════════════
        // УРОН
        // ═══════════════════════════════════════════════════════════
        skill.baseDamageOrHeal = 80f;
        skill.strengthScaling = 0f;
        skill.intelligenceScaling = 3.0f;
        // При 100 Intelligence: 80 + 100*3.0 = 380 урона
        // При 9 INT (тест): 80 + 9*3.0 = 107 урона

        // ═══════════════════════════════════════════════════════════
        // AOE ПАРАМЕТРЫ
        // ═══════════════════════════════════════════════════════════
        skill.aoeRadius = 6f; // 6 метров радиус взрыва
        skill.maxTargets = 10; // Максимум 10 врагов

        // ═══════════════════════════════════════════════════════════
        // ПРЕФАБ МЕТЕОРИТА (meteor prefab со scale 100)
        // ═══════════════════════════════════════════════════════════
        skill.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Projectiles/meteor.prefab"
        );

        if (skill.projectilePrefab == null)
        {
            // Fallback на Fireball если meteor не найден
            skill.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Projectiles/Fireball.prefab"
            );
            Debug.LogWarning("⚠️ meteor.prefab не найден, используется Fireball.prefab");
        }

        // ═══════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ═══════════════════════════════════════════════════════════
        // Эффект каста (огненная аура на маге во время каста)
        skill.castEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Light B (Air).prefab"
        );

        // Эффект попадания метеорита (большой огненный взрыв)
        skill.hitEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Fire Explosion B 1.prefab"
        );

        // AOE эффект (зона огня на земле)
        skill.aoeEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Fire Explosion B 1.prefab"
        );

        // ═══════════════════════════════════════════════════════════
        // РЕСУРСЫ
        // ═══════════════════════════════════════════════════════════
        skill.cooldown = 15f;
        skill.manaCost = 70f;

        // ═══════════════════════════════════════════════════════════
        // ДОПОЛНИТЕЛЬНЫЕ ЭФФЕКТЫ - ЗОНА ОГНЯ (DoT)
        // ═══════════════════════════════════════════════════════════
        EffectConfig burnZone = new EffectConfig();
        burnZone.effectType = EffectType.Burn;
        burnZone.duration = 5f; // 5 секунд
        burnZone.damageOrHealPerTick = 15f; // 15 урона за тик
        burnZone.tickInterval = 1f; // Каждую секунду
        burnZone.intelligenceScaling = 0.5f; // +0.5 урона за INT
        burnZone.canStack = false; // Не стакается
        burnZone.syncWithServer = true;

        skill.effects.Add(burnZone);

        // ═══════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ
        // ═══════════════════════════════════════════════════════════
        string path = "Assets/Resources/Skills/Mage_Meteor.asset";

        AssetDatabase.CreateAsset(skill, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = skill;

        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("☄️ Meteor создан!");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"📍 Путь: {path}");
        Debug.Log($"🆔 Skill ID: {skill.skillId}");
        Debug.Log($"💥 Базовый урон: {skill.baseDamageOrHeal}");
        Debug.Log($"🧠 Intelligence scaling: {skill.intelligenceScaling}x");
        Debug.Log($"🌀 AOE радиус: {skill.aoeRadius}м");
        Debug.Log($"⏱️ Cast time: {skill.castTime}с (можно прервать)");
        Debug.Log($"🔥 Зона огня: {burnZone.damageOrHealPerTick} урона/сек × {burnZone.duration}с");
        Debug.Log($"⏱️ Cooldown: {skill.cooldown}с");
        Debug.Log($"💧 Mana: {skill.manaCost}");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("📝 Meteor работает так:");
        Debug.Log("1. Игрок выбирает точку на земле (ground target)");
        Debug.Log("2. Начинается каст 2 секунды (можно прервать движением)");
        Debug.Log("3. Метеорит падает с неба (Fireball ×5 размером)");
        Debug.Log("4. Взрывается при ударе о землю");
        Debug.Log("5. Наносит 107 урона всем врагам в радиусе 6м");
        Debug.Log("6. Оставляет зону огня на 5 секунд (15 урона/сек)");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("⚠️ ВАЖНО: Cast time = 2 секунды!");
        Debug.Log("   Движение во время каста прервёт скилл");
        Debug.Log("═══════════════════════════════════════════════════════");
    }
}
