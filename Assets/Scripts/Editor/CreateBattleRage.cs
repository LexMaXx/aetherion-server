using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor скрипт для создания скилла "Battle Rage" (Боевая Ярость)
/// Воин входит в ярость, увеличивая урон на 40% на 12 секунд
/// </summary>
public class CreateBattleRage
{
    [MenuItem("Aetherion/Skills/Create Battle Rage (Warrior)")]
    public static void Create()
    {
        // Создаём ScriptableObject
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ═══════════════════════════════════════════════════════════════
        // ОСНОВНЫЕ ПАРАМЕТРЫ
        // ═══════════════════════════════════════════════════════════════
        skill.skillId = 405;
        skill.skillName = "Battle Rage";
        skill.skillType = SkillConfigType.Buff;
        skill.targetType = SkillTargetType.Self; // На себя
        skill.requiresTarget = false;
        skill.characterClass = CharacterClass.Warrior;

        skill.cooldown = 18f; // 18 секунд кулдаун
        skill.manaCost = 45f; // Средний расход маны
        skill.castTime = 0f; // Мгновенно
        skill.canUseWhileMoving = true;

        // ═══════════════════════════════════════════════════════════════
        // УРОН/ЛЕЧЕНИЕ
        // ═══════════════════════════════════════════════════════════════
        skill.baseDamageOrHeal = 0f; // Не наносит урон/хил
        skill.strengthScaling = 0f;
        skill.intelligenceScaling = 0f;

        // ═══════════════════════════════════════════════════════════════
        // ЭФФЕКТ УВЕЛИЧЕНИЯ УРОНА (+40%)
        // ═══════════════════════════════════════════════════════════════
        EffectConfig attackBoost = new EffectConfig();
        attackBoost.effectType = EffectType.IncreaseAttack;
        attackBoost.duration = 12f; // 12 секунд усиленного урона
        attackBoost.power = 40f; // +40% к урону
        attackBoost.canStack = false;
        attackBoost.maxStacks = 1;

        // Визуальный эффект ярости - огненная аура вокруг персонажа
        attackBoost.particleEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Fire Explosion B.prefab"
        );

        skill.effects = new List<EffectConfig> { attackBoost };

        // ═══════════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ═══════════════════════════════════════════════════════════════

        // Cast effect - огненная вспышка при активации (символ ярости)
        skill.castEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Fire Explosion B 1.prefab"
        );

        // Hit effect не нужен (self buff)
        skill.hitEffectPrefab = null;

        // ═══════════════════════════════════════════════════════════════
        // АНИМАЦИЯ
        // ═══════════════════════════════════════════════════════════════
        skill.animationTrigger = "Skill"; // Анимация ярости

        // ═══════════════════════════════════════════════════════════════
        // ОПИСАНИЕ
        // ═══════════════════════════════════════════════════════════════
        skill.description = "Воин входит в состояние боевой ярости, окружая себя огненной аурой. Весь наносимый урон увеличивается на 40% в течение 12 секунд.";

        // ═══════════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ
        // ═══════════════════════════════════════════════════════════════
        string path = "Assets/Resources/Skills/Warrior_BattleRage.asset";

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
        Debug.Log("🔥 Battle Rage создан!");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"📍 Путь: {path}");
        Debug.Log($"🆔 Skill ID: {skill.skillId}");
        Debug.Log($"🎯 Тип: Self Buff (боевая ярость)");
        Debug.Log($"⚔️ Эффект: +{attackBoost.power}% к урону ({attackBoost.duration} секунд)");
        Debug.Log($"✨ Визуальные эффекты:");
        Debug.Log($"   - Активация: CFXR3 Fire Explosion B 1 (огненная вспышка)");
        Debug.Log($"   - Бафф: CFXR3 Fire Explosion B (огненная аура вокруг персонажа)");
        Debug.Log($"⏱️ Cooldown: {skill.cooldown}с");
        Debug.Log($"💧 Mana: {skill.manaCost}");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("📊 КАК РАБОТАЕТ:");
        Debug.Log("  1. Нажимаешь клавишу Battle Rage");
        Debug.Log("  2. Огненная вспышка при активации");
        Debug.Log("  3. Вокруг воина появляется огненная аура");
        Debug.Log("  4. Весь урон увеличен на 40%");
        Debug.Log("  5. Эффект длится 12 секунд");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("💡 ПРИМЕРЫ:");
        Debug.Log("  БЕЗ Battle Rage:");
        Debug.Log("    • Базовая атака: 50 урона");
        Debug.Log("    • Hammer Throw: 95 урона");
        Debug.Log("  С Battle Rage:");
        Debug.Log("    • Базовая атака: 70 урона (+40%!) 🔥");
        Debug.Log("    • Hammer Throw: 133 урона (+40%!) 🔥");
        Debug.Log("  Результат: ОГРОМНЫЙ УРОН!");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("⚠️ ТРЕБУЕТСЯ:");
        Debug.Log("  1. Добавить attackModifier в CharacterStats/PlayerAttackNew");
        Debug.Log("  2. Добавить поддержку IncreaseAttack в EffectManager");
        Debug.Log("  3. Интегрировать модификатор урона в систему атак");
        Debug.Log("═══════════════════════════════════════════════════════");
    }
}
