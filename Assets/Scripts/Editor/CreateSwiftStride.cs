using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor скрипт для создания скилла "Swift Stride" (Быстрый Шаг)
/// Лучник получает +40% к скорости бега на 8 секунд
/// Визуальный эффект: аура из листвы вокруг персонажа
/// </summary>
public class CreateSwiftStride
{
    [MenuItem("Aetherion/Skills/Create Swift Stride (Archer)")]
    public static void Create()
    {
        // Создаём ScriptableObject
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ═══════════════════════════════════════════════════════════════
        // ОСНОВНЫЕ ПАРАМЕТРЫ
        // ═══════════════════════════════════════════════════════════════
        skill.skillId = 303;
        skill.skillName = "Swift Stride";
        skill.skillType = SkillConfigType.Buff;
        skill.targetType = SkillTargetType.Self; // На себя

        skill.baseDamageOrHeal = 0f; // Не наносит урон/хил
        skill.intelligenceScaling = 0f;
        skill.cooldown = 20f; // 20 секунд кулдаун
        skill.manaCost = 35f; // Средний расход маны

        // ═══════════════════════════════════════════════════════════════
        // ЭФФЕКТ УСКОРЕНИЯ
        // ═══════════════════════════════════════════════════════════════
        EffectConfig speedBoost = new EffectConfig();
        speedBoost.effectType = EffectType.IncreaseSpeed;
        speedBoost.duration = 8f; // 8 секунд ускорения
        speedBoost.power = 40f; // +40% к скорости
        speedBoost.canStack = false; // Не стакается
        speedBoost.maxStacks = 1;

        // Визуальный эффект - аура из листвы вокруг персонажа
        speedBoost.particleEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Shield Leaves A (Lit).prefab"
        );

        skill.effects = new List<EffectConfig> { speedBoost };

        // ═══════════════════════════════════════════════════════════════
        // ЭФФЕКТЫ КАСТА
        // ═══════════════════════════════════════════════════════════════
        // Эффект при активации (вспышка листьев)
        skill.castEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Leaves A (Lit).prefab"
        );

        // ═══════════════════════════════════════════════════════════════
        // АНИМАЦИЯ
        // ═══════════════════════════════════════════════════════════════
        skill.animationTrigger = "Skill"; // Используем стандартную анимацию скилла

        // ═══════════════════════════════════════════════════════════════
        // ОПИСАНИЕ
        // ═══════════════════════════════════════════════════════════════
        skill.description = "Лучник призывает силу природы, увеличивая скорость передвижения на 40% на 8 секунд. Окружает персонажа аурой из листвы.";

        // ═══════════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ
        // ═══════════════════════════════════════════════════════════════
        string path = "Assets/Resources/Skills/Archer_SwiftStride.asset";

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
        Debug.Log("🏃 Swift Stride создан!");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"📍 Путь: {path}");
        Debug.Log($"🆔 Skill ID: {skill.skillId}");
        Debug.Log($"⚡ Эффект: +{speedBoost.power}% к скорости ({speedBoost.duration} секунд)");
        Debug.Log($"🍃 Визуальный эффект: CFXR3 Shield Leaves A (Lit)");
        Debug.Log($"⏱️ Cooldown: {skill.cooldown}с");
        Debug.Log($"💧 Mana: {skill.manaCost}");
        Debug.Log("═══════════════════════════════════════════════════════");
    }
}
