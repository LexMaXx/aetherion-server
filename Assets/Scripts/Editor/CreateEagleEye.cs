using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor скрипт для создания скилла "Eagle Eye" (Орлиный Глаз)
/// Лучник получает +2 к восприятию на 15 секунд
/// Визуальный эффект: магическая аура с намёком на улучшенное зрение
/// </summary>
public class CreateEagleEye
{
    [MenuItem("Aetherion/Skills/Create Eagle Eye (Archer)")]
    public static void Create()
    {
        // Создаём ScriptableObject
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ═══════════════════════════════════════════════════════════════
        // ОСНОВНЫЕ ПАРАМЕТРЫ
        // ═══════════════════════════════════════════════════════════════
        skill.skillId = 304;
        skill.skillName = "Eagle Eye";
        skill.skillType = SkillConfigType.Buff;
        skill.targetType = SkillTargetType.Self; // На себя

        skill.baseDamageOrHeal = 0f; // Не наносит урон/хил
        skill.intelligenceScaling = 0f;
        skill.cooldown = 30f; // 30 секунд кулдаун (мощный бафф)
        skill.manaCost = 40f; // Средний-высокий расход маны

        // ═══════════════════════════════════════════════════════════════
        // ЭФФЕКТ УВЕЛИЧЕНИЯ ВОСПРИЯТИЯ
        // ═══════════════════════════════════════════════════════════════
        EffectConfig perceptionBoost = new EffectConfig();
        perceptionBoost.effectType = EffectType.IncreasePerception;
        perceptionBoost.duration = 15f; // 15 секунд усиленного зрения
        perceptionBoost.power = 2f; // +2 к восприятию (прямое значение, не процент!)
        perceptionBoost.canStack = false; // Не стакается
        perceptionBoost.maxStacks = 1;

        // Визуальный эффект - магическая аура вокруг персонажа
        // Руническая аура символизирует магическое улучшение зрения
        perceptionBoost.particleEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Magic Aura A (Runic).prefab"
        );

        skill.effects = new List<EffectConfig> { perceptionBoost };

        // ═══════════════════════════════════════════════════════════════
        // ЭФФЕКТЫ КАСТА
        // ═══════════════════════════════════════════════════════════════
        // Эффект при активации (вспышка света - символ прозрения)
        skill.castEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Light B (Air).prefab"
        );

        // ═══════════════════════════════════════════════════════════════
        // АНИМАЦИЯ
        // ═══════════════════════════════════════════════════════════════
        skill.animationTrigger = "Skill"; // Используем стандартную анимацию скилла

        // ═══════════════════════════════════════════════════════════════
        // ОПИСАНИЕ
        // ═══════════════════════════════════════════════════════════════
        skill.description = "Лучник обостряет своё зрение до орлиного, увеличивая восприятие на +2 на 15 секунд. Позволяет видеть дальше и точнее замечать врагов.";

        // ═══════════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ
        // ═══════════════════════════════════════════════════════════════
        string path = "Assets/Resources/Skills/Archer_EagleEye.asset";

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
        Debug.Log("👁️ Eagle Eye создан!");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"📍 Путь: {path}");
        Debug.Log($"🆔 Skill ID: {skill.skillId}");
        Debug.Log($"⚡ Эффект: +{perceptionBoost.power} к восприятию ({perceptionBoost.duration} секунд)");
        Debug.Log($"✨ Визуальный эффект: CFXR3 Magic Aura A (Runic)");
        Debug.Log($"⏱️ Cooldown: {skill.cooldown}с");
        Debug.Log($"💧 Mana: {skill.manaCost}");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("📊 ЭФФЕКТ ВОСПРИЯТИЯ:");
        Debug.Log("  Perception влияет на:");
        Debug.Log("  • Vision Radius (радиус обзора)");
        Debug.Log("  • Critical Hit Chance (шанс крита)");
        Debug.Log("  • Обнаружение скрытых врагов");
        Debug.Log("═══════════════════════════════════════════════════════");
    }
}
