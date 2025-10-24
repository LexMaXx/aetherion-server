using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Создаёт ScriptableObject для Lay on Hands (Paladin skill #3)
/// Лечит 20% HP всем союзникам в радиусе 10м
/// </summary>
public class CreateLayOnHands : MonoBehaviour
{
    [MenuItem("Aetherion/Skills/Paladin/Create Lay on Hands")]
    public static void Create()
    {
        // Создаём SkillConfig
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ════════════════════════════════════════════════════════════
        // ОСНОВНАЯ ИНФОРМАЦИЯ
        // ════════════════════════════════════════════════════════════

        skill.skillId = 503;
        skill.skillName = "Lay on Hands";
        skill.description = "Лечит всех союзников в радиусе 10 метров на 20% от их максимального HP. Мощное групповое лечение.";
        skill.characterClass = CharacterClass.Paladin;

        // ════════════════════════════════════════════════════════════
        // ПАРАМЕТРЫ ИСПОЛЬЗОВАНИЯ
        // ════════════════════════════════════════════════════════════

        skill.cooldown = 60f; // 1 минута
        skill.manaCost = 60f;
        skill.castRange = 10f; // Радиус действия
        skill.castTime = 0.3f; // 0.3 секунды каст
        skill.canUseWhileMoving = false;

        // ════════════════════════════════════════════════════════════
        // ТИП СКИЛЛА
        // ════════════════════════════════════════════════════════════

        skill.skillType = SkillConfigType.Heal;
        skill.targetType = SkillTargetType.NoTarget; // На всех в радиусе
        skill.requiresTarget = false;
        skill.canTargetAllies = true;
        skill.canTargetEnemies = false;

        // ════════════════════════════════════════════════════════════
        // УРОН / ЛЕЧЕНИЕ
        // ════════════════════════════════════════════════════════════

        // ВАЖНО: Отрицательное значение = процент от максимального HP
        skill.baseDamageOrHeal = -20f; // -20 = 20% от MaxHP
        skill.strengthScaling = 0f;
        skill.intelligenceScaling = 0f;

        // ════════════════════════════════════════════════════════════
        // AOE (ОБЛАСТЬ ПОРАЖЕНИЯ)
        // ════════════════════════════════════════════════════════════

        skill.aoeRadius = 10f; // 10 метров
        skill.maxTargets = 20; // Без лимита на союзников

        // ════════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ════════════════════════════════════════════════════════════

        // Эффект каста (святое свечение)
        skill.castEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Hit Light B (Air)");

        // Эффект на цели (святое лечение)
        skill.hitEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Hit Light B (Air)");

        // Эффект на кастере (золотое свечение)
        skill.casterEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Magic Aura A (Runic)");

        // ════════════════════════════════════════════════════════════
        // АНИМАЦИЯ
        // ════════════════════════════════════════════════════════════

        skill.animationTrigger = ""; // Нет анимации
        skill.animationSpeed = 1f;

        // ════════════════════════════════════════════════════════════
        // ЗВУКИ
        // ════════════════════════════════════════════════════════════

        skill.soundVolume = 0.8f;

        // ════════════════════════════════════════════════════════════
        // СЕТЕВАЯ СИНХРОНИЗАЦИЯ
        // ════════════════════════════════════════════════════════════

        skill.syncProjectiles = false; // Нет снарядов
        skill.syncHitEffects = true;
        skill.syncStatusEffects = false; // Нет статус-эффектов

        // ════════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ
        // ════════════════════════════════════════════════════════════

        // Создаём директорию если нет
        string directory = "Assets/Resources/Skills";
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Сохраняем SkillConfig
        string skillPath = $"{directory}/Paladin_LayOnHands.asset";
        AssetDatabase.CreateAsset(skill, skillPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Lay on Hands создан: {skillPath}");
        Debug.Log("📋 Skill ID: 503");
        Debug.Log("⏱️ Cooldown: 60 секунд");
        Debug.Log("💙 Mana: 60");
        Debug.Log("📍 Radius: 10 метров");
        Debug.Log("❤️ Heal: 20% от MaxHP каждого союзника");

        // Выделяем созданный asset в Project window
        Selection.activeObject = skill;
        EditorGUIUtility.PingObject(skill);
    }
}
