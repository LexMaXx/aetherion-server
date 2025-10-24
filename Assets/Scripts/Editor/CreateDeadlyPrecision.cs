using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor скрипт для создания скилла "Deadly Precision" (Смертельная Точность)
/// Лучник получает +40% к силе критического урона на 12 секунд
/// Визуальный эффект: огненная аура силы вокруг персонажа
/// </summary>
public class CreateDeadlyPrecision
{
    [MenuItem("Aetherion/Skills/Create Deadly Precision (Archer)")]
    public static void Create()
    {
        // Создаём ScriptableObject
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ═══════════════════════════════════════════════════════════════
        // ОСНОВНЫЕ ПАРАМЕТРЫ
        // ═══════════════════════════════════════════════════════════════
        skill.skillId = 305;
        skill.skillName = "Deadly Precision";
        skill.skillType = SkillConfigType.Buff;
        skill.targetType = SkillTargetType.Self; // На себя

        skill.baseDamageOrHeal = 0f; // Не наносит урон/хил
        skill.intelligenceScaling = 0f;
        skill.cooldown = 25f; // 25 секунд кулдаун
        skill.manaCost = 45f; // Высокий расход маны (мощный бафф)

        // ═══════════════════════════════════════════════════════════════
        // ЭФФЕКТ УВЕЛИЧЕНИЯ КРИТИЧЕСКОГО УРОНА
        // ═══════════════════════════════════════════════════════════════
        EffectConfig critDamageBoost = new EffectConfig();
        critDamageBoost.effectType = EffectType.IncreaseCritDamage;
        critDamageBoost.duration = 12f; // 12 секунд усиленных критов
        critDamageBoost.power = 100f; // +100% к критическому урону (удвоение крита!)
        critDamageBoost.canStack = false; // Не стакается
        critDamageBoost.maxStacks = 1;

        // Визуальный эффект - огненная аура силы вокруг персонажа
        // Огонь символизирует разрушительную мощь критических ударов
        critDamageBoost.particleEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Fire Explosion B.prefab"
        );

        skill.effects = new List<EffectConfig> { critDamageBoost };

        // ═══════════════════════════════════════════════════════════════
        // ЭФФЕКТЫ КАСТА
        // ═══════════════════════════════════════════════════════════════
        // Эффект при активации (вспышка огня - символ силы)
        skill.castEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Fire Explosion B 1.prefab"
        );

        // ═══════════════════════════════════════════════════════════════
        // АНИМАЦИЯ
        // ═══════════════════════════════════════════════════════════════
        skill.animationTrigger = "Skill"; // Используем стандартную анимацию скилла

        // ═══════════════════════════════════════════════════════════════
        // ОПИСАНИЕ
        // ═══════════════════════════════════════════════════════════════
        skill.description = "Лучник концентрирует всю свою силу и точность, увеличивая урон от критических ударов на 100% на 12 секунд. Критические удары наносят в 2 раза больше урона!";

        // ═══════════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ
        // ═══════════════════════════════════════════════════════════════
        string path = "Assets/Resources/Skills/Archer_DeadlyPrecision.asset";

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
        Debug.Log("💥 Deadly Precision создан!");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"📍 Путь: {path}");
        Debug.Log($"🆔 Skill ID: {skill.skillId}");
        Debug.Log($"⚡ Эффект: +{critDamageBoost.power}% к критическому урону ({critDamageBoost.duration} секунд)");
        Debug.Log($"🔥 Визуальный эффект: CFXR3 Fire Explosion B");
        Debug.Log($"⏱️ Cooldown: {skill.cooldown}с");
        Debug.Log($"💧 Mana: {skill.manaCost}");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("📊 ЭФФЕКТ КРИТИЧЕСКОГО УРОНА:");
        Debug.Log("  Базовый крит множитель: x2.0 (200%)");
        Debug.Log("  С Deadly Precision: x4.0 (400%) - +100% к множителю!");
        Debug.Log("  Пример: 100 урона → 400 крит урона (вместо 200)");
        Debug.Log("  ⚠️ УДВОЕННЫЙ КРИТИЧЕСКИЙ УРОН!");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("💡 КОМБО:");
        Debug.Log("  Eagle Eye (+2 Perception) → больше шанс крита");
        Debug.Log("  Deadly Precision (+100% урон) → x4 критический урон!");
        Debug.Log("  Rain of Arrows (3 arrows) → больше шансов покритовать!");
        Debug.Log("═══════════════════════════════════════════════════════");
    }
}
