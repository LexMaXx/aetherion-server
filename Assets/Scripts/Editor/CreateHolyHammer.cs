using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Создаёт ScriptableObject для Holy Hammer (Paladin skill #5)
/// AOE стан всем врагам в радиусе 10м на 5 секунд
/// </summary>
public class CreateHolyHammer : MonoBehaviour
{
    [MenuItem("Aetherion/Skills/Paladin/Create Holy Hammer")]
    public static void Create()
    {
        // Создаём SkillConfig
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ════════════════════════════════════════════════════════════
        // ОСНОВНАЯ ИНФОРМАЦИЯ
        // ════════════════════════════════════════════════════════════

        skill.skillId = 505;
        skill.skillName = "Holy Hammer";
        skill.description = "Призывает святой молот, который обрушивается на землю, оглушая всех врагов в радиусе 10 метров на 5 секунд. Также наносит небольшой святой урон.";
        skill.characterClass = CharacterClass.Paladin;

        // ════════════════════════════════════════════════════════════
        // ПАРАМЕТРЫ ИСПОЛЬЗОВАНИЯ
        // ════════════════════════════════════════════════════════════

        skill.cooldown = 30f; // 30 секунд - AOE стан очень мощный
        skill.manaCost = 70f;
        skill.castRange = 10f; // Радиус действия
        skill.castTime = 0.5f; // 0.5 секунды каст
        skill.canUseWhileMoving = false; // Нельзя двигаться во время каста

        // ════════════════════════════════════════════════════════════
        // ТИП СКИЛЛА
        // ════════════════════════════════════════════════════════════

        skill.skillType = SkillConfigType.AOEDamage; // AOE урон + эффекты
        skill.targetType = SkillTargetType.NoTarget; // На всех врагов вокруг
        skill.requiresTarget = false;
        skill.canTargetAllies = false;
        skill.canTargetEnemies = true;

        // ════════════════════════════════════════════════════════════
        // УРОН
        // ════════════════════════════════════════════════════════════

        skill.baseDamageOrHeal = 50f; // Небольшой святой урон
        skill.strengthScaling = 1.0f; // Скейлится со силы
        skill.intelligenceScaling = 0f;

        // ════════════════════════════════════════════════════════════
        // AOE (ОБЛАСТЬ ПОРАЖЕНИЯ)
        // ════════════════════════════════════════════════════════════

        skill.aoeRadius = 10f; // 10 метров
        skill.maxTargets = 20; // Без лимита на врагов

        // ════════════════════════════════════════════════════════════
        // ЭФФЕКТЫ - STUN НА 5 СЕКУНД
        // ════════════════════════════════════════════════════════════

        // Создаём эффект Stun
        EffectConfig stunEffect = new EffectConfig();
        stunEffect.effectType = EffectType.Stun;
        stunEffect.duration = 5f; // 5 секунд стана
        stunEffect.canStack = false;
        stunEffect.maxStacks = 1;
        stunEffect.canBeDispelled = true;

        // Визуальный эффект стана - электрические искры как у лучника
        stunEffect.particleEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Hit Electric C (Air)");

        skill.effects.Add(stunEffect);

        // ════════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ════════════════════════════════════════════════════════════

        // Эффект каста (золотое святое свечение)
        skill.castEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Magic Aura A (Runic)");

        // Эффект попадания (электрические искры + святой свет)
        skill.hitEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Hit Electric C (Air)");

        // Эффект на кастере (золотое свечение паладина)
        skill.casterEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Hit Light B (Air)");

        // ════════════════════════════════════════════════════════════
        // АНИМАЦИЯ
        // ════════════════════════════════════════════════════════════

        skill.animationTrigger = ""; // Нет анимации
        skill.animationSpeed = 1f;

        // ════════════════════════════════════════════════════════════
        // ЗВУКИ
        // ════════════════════════════════════════════════════════════

        skill.soundVolume = 0.9f; // Громкий удар молота

        // ════════════════════════════════════════════════════════════
        // СЕТЕВАЯ СИНХРОНИЗАЦИЯ
        // ════════════════════════════════════════════════════════════

        skill.syncProjectiles = false; // Нет снарядов
        skill.syncHitEffects = true;
        skill.syncStatusEffects = true; // ВАЖНО! Синхронизируем стан

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
        string skillPath = $"{directory}/Paladin_HolyHammer.asset";
        AssetDatabase.CreateAsset(skill, skillPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Holy Hammer создан: {skillPath}");
        Debug.Log("📋 Skill ID: 505");
        Debug.Log("⏱️ Cooldown: 30 секунд");
        Debug.Log("💙 Mana: 70");
        Debug.Log("📍 Radius: 10 метров");
        Debug.Log("⏳ Stun Duration: 5 секунд");
        Debug.Log("⚡ Effect: STUN для всех врагов в радиусе");
        Debug.Log("⚔️ Damage: 50 + 1.0x Strength");

        // Выделяем созданный asset в Project window
        Selection.activeObject = skill;
        EditorGUIUtility.PingObject(skill);
    }
}
