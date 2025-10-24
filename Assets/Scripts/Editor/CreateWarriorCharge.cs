using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor скрипт для создания скилла "Charge" (Рывок)
/// Воин телепортируется к врагу (макс. 20м) и оглушает его на 5 секунд
/// </summary>
public class CreateWarriorCharge
{
    [MenuItem("Aetherion/Skills/Create Charge (Warrior)")]
    public static void Create()
    {
        // Создаём ScriptableObject
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ═══════════════════════════════════════════════════════════════
        // ОСНОВНЫЕ ПАРАМЕТРЫ
        // ═══════════════════════════════════════════════════════════════
        skill.skillId = 401;
        skill.skillName = "Charge";
        skill.skillType = SkillConfigType.Movement; // Движение + CC
        skill.targetType = SkillTargetType.Enemy; // Требует цель (врага)
        skill.requiresTarget = true;

        skill.baseDamageOrHeal = 0f; // Не наносит прямой урон
        skill.intelligenceScaling = 0f;
        skill.cooldown = 12f; // 12 секунд кулдаун
        skill.manaCost = 30f; // Средний расход маны

        // ═══════════════════════════════════════════════════════════════
        // НАСТРОЙКИ ДВИЖЕНИЯ (ТЕЛЕПОРТ К ВРАГУ)
        // ═══════════════════════════════════════════════════════════════
        skill.enableMovement = true;
        skill.movementType = MovementType.Teleport; // Мгновенный телепорт (как у мага)
        skill.movementDirection = MovementDirection.ToTarget; // К цели
        skill.movementDistance = 20f; // Максимум 20 метров
        skill.movementSpeed = 0f; // Не используется для телепорта

        // ═══════════════════════════════════════════════════════════════
        // ЭФФЕКТ ОГЛУШЕНИЯ (STUN)
        // ═══════════════════════════════════════════════════════════════
        EffectConfig stunEffect = new EffectConfig();
        stunEffect.effectType = EffectType.Stun;
        stunEffect.duration = 5f; // 5 секунд оглушения
        stunEffect.canStack = false;
        stunEffect.maxStacks = 1;

        // Визуальный эффект оглушения на враге (электрические искры как у лучника)
        // ВАЖНО: EffectConfig.particleEffectPrefab это GameObject, а не строка!
        stunEffect.particleEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Electric C (Air).prefab"
        );

        skill.effects = new List<EffectConfig> { stunEffect };

        // ═══════════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ═══════════════════════════════════════════════════════════════

        // Эффект телепорта под ногами (в точке появления)
        // Используем тот же эффект что и у мага для единообразия
        skill.hitEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Magic Poof.prefab"
        );

        // Эффект в точке старта (откуда телепортировались)
        skill.castEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Magic Poof.prefab"
        );

        // ═══════════════════════════════════════════════════════════════
        // АНИМАЦИЯ
        // ═══════════════════════════════════════════════════════════════
        skill.animationTrigger = "Skill"; // Стандартная анимация скилла

        // ═══════════════════════════════════════════════════════════════
        // ОПИСАНИЕ
        // ═══════════════════════════════════════════════════════════════
        skill.description = "Воин мгновенно телепортируется к выбранному врагу (макс. 20м) и оглушает его на 5 секунд, блокируя движение, атаки и скиллы.";

        // ═══════════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ
        // ═══════════════════════════════════════════════════════════════
        string path = "Assets/Resources/Skills/Warrior_Charge.asset";

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
        Debug.Log("⚔️ Charge (Рывок) создан!");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"📍 Путь: {path}");
        Debug.Log($"🆔 Skill ID: {skill.skillId}");
        Debug.Log($"🎯 Тип: Movement + CC (телепорт к врагу)");
        Debug.Log($"📏 Дистанция: {skill.movementDistance}м (максимум)");
        Debug.Log($"⚡ Эффект: Stun на {stunEffect.duration} секунд");
        Debug.Log($"✨ Визуальные эффекты:");
        Debug.Log($"   - Телепорт: CFXR3 Magic Poof (как у мага)");
        Debug.Log($"   - Оглушение: CFXR3 Hit Electric C (как у лучника)");
        Debug.Log($"⏱️ Cooldown: {skill.cooldown}с");
        Debug.Log($"💧 Mana: {skill.manaCost}");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("📊 КАК РАБОТАЕТ:");
        Debug.Log("  1. Выбираешь врага (ЛКМ)");
        Debug.Log("  2. Используешь Charge (клавиша скилла)");
        Debug.Log("  3. Воин телепортируется К ВРАГУ (макс. 20м)");
        Debug.Log("  4. Эффект телепорта под ногами");
        Debug.Log("  5. Враг оглушён на 5 секунд + электрические искры");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("⚠️ ВАЖНО:");
        Debug.Log("  - Требует выбранную цель (ЛКМ на враге)");
        Debug.Log("  - Максимальная дистанция: 20 метров");
        Debug.Log("  - Если враг дальше 20м - телепортируешься НА 20м К НЕМУ");
        Debug.Log("═══════════════════════════════════════════════════════");
    }
}
