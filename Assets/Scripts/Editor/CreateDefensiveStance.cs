using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor скрипт для создания скилла "Defensive Stance" (Защитная Стойка)
/// Воин принимает защитную стойку, уменьшая получаемый урон на 50% на 10 секунд
/// </summary>
public class CreateDefensiveStance
{
    [MenuItem("Aetherion/Skills/Create Defensive Stance (Warrior)")]
    public static void Create()
    {
        // Создаём ScriptableObject
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ═══════════════════════════════════════════════════════════════
        // ОСНОВНЫЕ ПАРАМЕТРЫ
        // ═══════════════════════════════════════════════════════════════
        skill.skillId = 403;
        skill.skillName = "Defensive Stance";
        skill.skillType = SkillConfigType.Buff;
        skill.targetType = SkillTargetType.Self; // На себя
        skill.requiresTarget = false;
        skill.characterClass = CharacterClass.Warrior;

        skill.cooldown = 20f; // 20 секунд кулдаун (мощный защитный бафф)
        skill.manaCost = 40f; // Средний расход маны
        skill.castTime = 0f; // Мгновенно
        skill.canUseWhileMoving = true;

        // ═══════════════════════════════════════════════════════════════
        // УРОН/ЛЕЧЕНИЕ
        // ═══════════════════════════════════════════════════════════════
        skill.baseDamageOrHeal = 0f; // Не наносит урон/хил
        skill.strengthScaling = 0f;
        skill.intelligenceScaling = 0f;

        // ═══════════════════════════════════════════════════════════════
        // ЭФФЕКТ УВЕЛИЧЕНИЯ ЗАЩИТЫ (50% снижение урона)
        // ═══════════════════════════════════════════════════════════════
        EffectConfig defenseBoost = new EffectConfig();
        defenseBoost.effectType = EffectType.IncreaseDefense;
        defenseBoost.duration = 10f; // 10 секунд защиты
        defenseBoost.power = 50f; // 50% снижение получаемого урона
        defenseBoost.canStack = false;
        defenseBoost.maxStacks = 1;

        // Визуальный эффект защиты - золотой щит вокруг персонажа
        defenseBoost.particleEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Shield Leaves A (Lit).prefab"
        );

        skill.effects = new List<EffectConfig> { defenseBoost };

        // ═══════════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ═══════════════════════════════════════════════════════════════

        // Cast effect - вспышка золотого света при активации
        skill.castEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Light B (Air).prefab"
        );

        // Hit effect не нужен (self buff)
        skill.hitEffectPrefab = null;

        // ═══════════════════════════════════════════════════════════════
        // АНИМАЦИЯ
        // ═══════════════════════════════════════════════════════════════
        skill.animationTrigger = "Skill"; // Анимация защиты

        // ═══════════════════════════════════════════════════════════════
        // ОПИСАНИЕ
        // ═══════════════════════════════════════════════════════════════
        skill.description = "Воин принимает защитную стойку, окружая себя магическим щитом. Получаемый урон уменьшается на 50% в течение 10 секунд.";

        // ═══════════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ
        // ═══════════════════════════════════════════════════════════════
        string path = "Assets/Resources/Skills/Warrior_DefensiveStance.asset";

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
        Debug.Log("🛡️ Defensive Stance создан!");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"📍 Путь: {path}");
        Debug.Log($"🆔 Skill ID: {skill.skillId}");
        Debug.Log($"🎯 Тип: Self Buff (защитная стойка)");
        Debug.Log($"🛡️ Эффект: -{defenseBoost.power}% получаемого урона ({defenseBoost.duration} секунд)");
        Debug.Log($"✨ Визуальные эффекты:");
        Debug.Log($"   - Активация: CFXR3 Hit Light B (вспышка света)");
        Debug.Log($"   - Бафф: CFXR3 Shield Leaves A (золотой щит вокруг персонажа)");
        Debug.Log($"⏱️ Cooldown: {skill.cooldown}с");
        Debug.Log($"💧 Mana: {skill.manaCost}");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("📊 КАК РАБОТАЕТ:");
        Debug.Log("  1. Нажимаешь клавишу Defensive Stance");
        Debug.Log("  2. Вспышка золотого света");
        Debug.Log("  3. Вокруг воина появляется золотой щит");
        Debug.Log("  4. Получаемый урон уменьшается на 50%");
        Debug.Log("  5. Эффект длится 10 секунд");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("💡 ПРИМЕРЫ:");
        Debug.Log("  БЕЗ Defensive Stance:");
        Debug.Log("    • Враг атакует на 100 урона → Ты получаешь 100 урона");
        Debug.Log("  С Defensive Stance:");
        Debug.Log("    • Враг атакует на 100 урона → Ты получаешь 50 урона! 🛡️");
        Debug.Log("  Результат: ЖИВУЧЕСТЬ x2!");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("⚠️ ТРЕБУЕТСЯ:");
        Debug.Log("  1. Добавить damageReduction модификатор в HealthSystem");
        Debug.Log("  2. Добавить поддержку IncreaseDefense в EffectManager");
        Debug.Log("  3. Интегрировать с HealthSystem.TakeDamage()");
        Debug.Log("═══════════════════════════════════════════════════════");
    }
}
