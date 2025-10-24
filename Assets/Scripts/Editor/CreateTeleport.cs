using UnityEngine;
using UnityEditor;

/// <summary>
/// Создаёт Teleport SkillConfig для мага
/// Mobility скилл с ground target механикой
/// </summary>
public class CreateTeleport : Editor
{
    [MenuItem("Aetherion/Skills/Create Teleport (Mage)")]
    public static void Create()
    {
        // Создаём SkillConfig
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ═══════════════════════════════════════════════════════════
        // БАЗОВАЯ ИНФОРМАЦИЯ
        // ═══════════════════════════════════════════════════════════
        skill.skillId = 204;
        skill.skillName = "Teleport";
        skill.description = "Телепортация в указанную точку. Максимальная дальность 15 метров.";
        skill.icon = null; // TODO: добавить иконку

        // ═══════════════════════════════════════════════════════════
        // КЛАСС
        // ═══════════════════════════════════════════════════════════
        skill.characterClass = CharacterClass.Mage;

        // ═══════════════════════════════════════════════════════════
        // КАСТ И ИСПОЛЬЗОВАНИЕ
        // ═══════════════════════════════════════════════════════════
        skill.castTime = 0f; // Instant
        skill.castRange = 15f; // Максимум 15 метров
        skill.canUseWhileMoving = true;

        // ═══════════════════════════════════════════════════════════
        // ТИП СКИЛЛА - MOVEMENT (TELEPORT)
        // ═══════════════════════════════════════════════════════════
        skill.skillType = SkillConfigType.Movement;
        skill.targetType = SkillTargetType.Ground; // Ground target
        skill.requiresTarget = false; // Ground target не требует Entity цели
        skill.canTargetAllies = false;
        skill.canTargetEnemies = false;

        // ═══════════════════════════════════════════════════════════
        // УРОН / ЛЕЧЕНИЕ
        // ═══════════════════════════════════════════════════════════
        skill.baseDamageOrHeal = 0f; // Нет урона
        skill.strengthScaling = 0f;
        skill.intelligenceScaling = 0f;

        // ═══════════════════════════════════════════════════════════
        // ДВИЖЕНИЕ
        // ═══════════════════════════════════════════════════════════
        skill.enableMovement = true;
        skill.movementType = MovementType.Teleport;
        skill.movementSpeed = 0f; // Мгновенная телепортация
        skill.movementDistance = 15f; // Максимум 15 метров
        skill.movementDirection = MovementDirection.MouseDirection;

        // ═══════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ═══════════════════════════════════════════════════════════
        // Эффект исчезновения (на старом месте)
        skill.castEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Magic Aura A (Runic).prefab"
        );

        // Эффект появления (на новом месте)
        skill.hitEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Magic Aura A (Runic).prefab"
        );

        // Ground target эффект (показывает где появится игрок)
        skill.aoeEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Light B (Air).prefab"
        );

        // ═══════════════════════════════════════════════════════════
        // РЕСУРСЫ
        // ═══════════════════════════════════════════════════════════
        skill.cooldown = 8f;
        skill.manaCost = 30f;

        // ═══════════════════════════════════════════════════════════
        // ДОПОЛНИТЕЛЬНЫЕ ЭФФЕКТЫ
        // ═══════════════════════════════════════════════════════════
        // Можно добавить например краткий Invulnerability во время телепорта

        // ═══════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ
        // ═══════════════════════════════════════════════════════════
        string path = "Assets/Resources/Skills/Mage_Teleport.asset";

        AssetDatabase.CreateAsset(skill, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = skill;

        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("✨ Teleport создан!");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"📍 Путь: {path}");
        Debug.Log($"🆔 Skill ID: {skill.skillId}");
        Debug.Log($"🎯 Тип: {skill.skillType} ({skill.targetType})");
        Debug.Log($"📏 Максимальная дальность: {skill.castRange}м");
        Debug.Log($"⚡ Тип движения: {skill.movementType}");
        Debug.Log($"⏱️ Cooldown: {skill.cooldown}с");
        Debug.Log($"💧 Mana: {skill.manaCost}");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("📝 Teleport работает так:");
        Debug.Log("1. Игрок выбирает точку на земле (ground target)");
        Debug.Log("2. Мгновенная телепортация в эту точку");
        Debug.Log("3. Визуальные эффекты на старом и новом месте");
        Debug.Log("4. Максимум 15 метров от текущей позиции");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("⚠️ ВАЖНО: Нужно реализовать ground target механику!");
        Debug.Log("   - SimplePlayerController нужно добавить выбор точки");
        Debug.Log("   - SkillExecutor.ExecuteMovement() для телепорта");
        Debug.Log("═══════════════════════════════════════════════════════");
    }
}
