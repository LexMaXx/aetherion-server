using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Создаёт ScriptableObject для Divine Protection (Paladin skill #2)
/// Неуязвимость для всех союзников в радиусе 10м на 5 секунд
/// </summary>
public class CreateDivineProtection : MonoBehaviour
{
    [MenuItem("Aetherion/Skills/Paladin/Create Divine Protection")]
    public static void Create()
    {
        // Создаём SkillConfig
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ════════════════════════════════════════════════════════════
        // ОСНОВНАЯ ИНФОРМАЦИЯ
        // ════════════════════════════════════════════════════════════

        skill.skillId = 502;
        skill.skillName = "Divine Protection";
        skill.description = "Даёт НЕУЯЗВИМОСТЬ всем союзникам в радиусе 10 метров на 5 секунд. Защищает команду от любого урона.";
        skill.characterClass = CharacterClass.Paladin;

        // ════════════════════════════════════════════════════════════
        // ПАРАМЕТРЫ ИСПОЛЬЗОВАНИЯ
        // ════════════════════════════════════════════════════════════

        skill.cooldown = 120f; // 2 минуты - мощный скилл
        skill.manaCost = 80f;
        skill.castRange = 10f; // Радиус действия
        skill.castTime = 0.5f; // 0.5 секунды каст
        skill.canUseWhileMoving = false; // Нельзя двигаться во время каста

        // ════════════════════════════════════════════════════════════
        // ТИП СКИЛЛА
        // ════════════════════════════════════════════════════════════

        skill.skillType = SkillConfigType.Buff;
        skill.targetType = SkillTargetType.NoTarget; // На всех в радиусе
        skill.requiresTarget = false;
        skill.canTargetAllies = true;
        skill.canTargetEnemies = false;

        // ════════════════════════════════════════════════════════════
        // AOE (ОБЛАСТЬ ПОРАЖЕНИЯ)
        // ════════════════════════════════════════════════════════════

        skill.aoeRadius = 10f; // 10 метров
        skill.maxTargets = 20; // Без лимита на союзников

        // ════════════════════════════════════════════════════════════
        // ЭФФЕКТЫ
        // ════════════════════════════════════════════════════════════

        // Создаём эффект Invulnerability
        EffectConfig invulnerability = new EffectConfig();
        invulnerability.effectType = EffectType.Invulnerability;
        invulnerability.duration = 5f; // 5 секунд
        invulnerability.canStack = false;
        invulnerability.maxStacks = 1;
        invulnerability.canBeDispelled = true;

        // Визуальный эффект неуязвимости (золотая аура)
        invulnerability.particleEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Magic Aura A (Runic)");

        skill.effects.Add(invulnerability);

        // ════════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ════════════════════════════════════════════════════════════

        // Эффект каста (золотая аура паладина)
        skill.castEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Magic Aura A (Runic)");

        // Эффект на цели (золотая аура на союзниках)
        skill.hitEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Magic Aura A (Runic)");

        // Эффект на кастере (золотое свечение)
        skill.casterEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Hit Light B (Air)");

        // ════════════════════════════════════════════════════════════
        // АНИМАЦИЯ
        // ════════════════════════════════════════════════════════════

        skill.animationTrigger = ""; // Нет анимации (или пустая строка чтобы не было ошибки)
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
        skill.syncStatusEffects = true; // ВАЖНО! Синхронизируем неуязвимость

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
        string skillPath = $"{directory}/Paladin_DivineProtection.asset";
        AssetDatabase.CreateAsset(skill, skillPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Divine Protection создан: {skillPath}");
        Debug.Log("📋 Skill ID: 502");
        Debug.Log("⏱️ Cooldown: 120 секунд");
        Debug.Log("💙 Mana: 80");
        Debug.Log("📍 Radius: 10 метров");
        Debug.Log("⏳ Duration: 5 секунд");
        Debug.Log("🛡️ Effect: INVULNERABILITY для всех союзников");

        // Выделяем созданный asset в Project window
        Selection.activeObject = skill;
        EditorGUIUtility.PingObject(skill);
    }
}
