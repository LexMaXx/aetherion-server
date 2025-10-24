using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor скрипт для создания скилла "Battle Heal" (Боевое Исцеление)
/// Воин восстанавливает 20% от максимального HP
/// Мгновенное лечение с яркой вспышкой света
/// </summary>
public class CreateBattleHeal
{
    [MenuItem("Aetherion/Skills/Create Battle Heal (Warrior)")]
    public static void Create()
    {
        // Создаём ScriptableObject
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ═══════════════════════════════════════════════════════════════
        // ОСНОВНЫЕ ПАРАМЕТРЫ
        // ═══════════════════════════════════════════════════════════════
        skill.skillId = 404;
        skill.skillName = "Battle Heal";
        skill.skillType = SkillConfigType.Heal;
        skill.targetType = SkillTargetType.Self; // На себя
        skill.requiresTarget = false;
        skill.characterClass = CharacterClass.Warrior;

        skill.cooldown = 15f; // 15 секунд кулдаун
        skill.manaCost = 50f; // Средний расход маны
        skill.castTime = 0f; // Мгновенно
        skill.canUseWhileMoving = true;

        // ═══════════════════════════════════════════════════════════════
        // ЛЕЧЕНИЕ
        // ═══════════════════════════════════════════════════════════════
        // baseDamageOrHeal - отрицательное значение = лечение
        // Используем отрицательное значение для обозначения процента
        skill.baseDamageOrHeal = -20f; // -20 = восстановить 20% HP
        skill.strengthScaling = 0f; // Не зависит от силы (фиксированный %)
        skill.intelligenceScaling = 0f;

        // ═══════════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ═══════════════════════════════════════════════════════════════

        // Cast effect - ЯРКАЯ вспышка золотого света (мощная вспышка)
        skill.castEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Light B (Air).prefab"
        );

        // Hit effect - тоже яркая вспышка в точке игрока
        skill.hitEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Light B (Air).prefab"
        );

        // ═══════════════════════════════════════════════════════════════
        // АНИМАЦИЯ
        // ═══════════════════════════════════════════════════════════════
        skill.animationTrigger = "Skill"; // Анимация лечения

        // ═══════════════════════════════════════════════════════════════
        // ОПИСАНИЕ
        // ═══════════════════════════════════════════════════════════════
        skill.description = "Воин концентрирует внутреннюю энергию, восстанавливая 20% от максимального здоровья. Яркая вспышка света знаменует момент исцеления.";

        // ═══════════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ
        // ═══════════════════════════════════════════════════════════════
        string path = "Assets/Resources/Skills/Warrior_BattleHeal.asset";

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
        Debug.Log("⚕️ Battle Heal создан!");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"📍 Путь: {path}");
        Debug.Log($"🆔 Skill ID: {skill.skillId}");
        Debug.Log($"🎯 Тип: Self Heal (самолечение)");
        Debug.Log($"❤️ Эффект: Восстановить 20% от максимального HP");
        Debug.Log($"✨ Визуальные эффекты:");
        Debug.Log($"   - Cast: CFXR3 Hit Light B (яркая вспышка света)");
        Debug.Log($"   - Hit: CFXR3 Hit Light B (вспышка в точке игрока)");
        Debug.Log($"⏱️ Cooldown: {skill.cooldown}с");
        Debug.Log($"💧 Mana: {skill.manaCost}");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("📊 КАК РАБОТАЕТ:");
        Debug.Log("  1. Нажимаешь клавишу Battle Heal");
        Debug.Log("  2. Яркая вспышка света вокруг персонажа");
        Debug.Log("  3. Восстанавливается 20% от максимального HP");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("💡 ПРИМЕРЫ:");
        Debug.Log("  Максимум HP: 180");
        Debug.Log("  Текущее HP: 50 (низкое!)");
        Debug.Log("  Battle Heal → +36 HP (20% от 180)");
        Debug.Log("  Итого: 50 + 36 = 86 HP ❤️");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("  Максимум HP: 180");
        Debug.Log("  Текущее HP: 170 (почти полное)");
        Debug.Log("  Battle Heal → +10 HP (до максимума)");
        Debug.Log("  Итого: 180 HP (полное здоровье!) ❤️");
        Debug.Log("═══════════════════════════════════════════════════════");
    }
}
