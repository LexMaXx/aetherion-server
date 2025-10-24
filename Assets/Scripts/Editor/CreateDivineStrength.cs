using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Создаёт ScriptableObject для Divine Strength (Paladin skill #4)
/// Увеличивает атаку на 50% всем союзникам в радиусе 10м на 15 секунд
/// </summary>
public class CreateDivineStrength : MonoBehaviour
{
    [MenuItem("Aetherion/Skills/Paladin/Create Divine Strength")]
    public static void Create()
    {
        // Создаём SkillConfig
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ════════════════════════════════════════════════════════════
        // ОСНОВНАЯ ИНФОРМАЦИЯ
        // ════════════════════════════════════════════════════════════

        skill.skillId = 504;
        skill.skillName = "Divine Strength";
        skill.description = "Увеличивает физическую атаку на 50% всем союзникам в радиусе 10 метров на 15 секунд. Мощный наступательный бафф.";
        skill.characterClass = CharacterClass.Paladin;

        // ════════════════════════════════════════════════════════════
        // ПАРАМЕТРЫ ИСПОЛЬЗОВАНИЯ
        // ════════════════════════════════════════════════════════════

        skill.cooldown = 90f; // 1.5 минуты
        skill.manaCost = 70f;
        skill.castRange = 10f; // Радиус действия
        skill.castTime = 0.2f; // 0.2 секунды каст
        skill.canUseWhileMoving = true; // Можно использовать в движении

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

        // Создаём эффект IncreaseAttack
        EffectConfig attackBuff = new EffectConfig();
        attackBuff.effectType = EffectType.IncreaseAttack;
        attackBuff.duration = 15f; // 15 секунд
        attackBuff.power = 50f; // +50% к атаке
        attackBuff.canStack = false;
        attackBuff.maxStacks = 1;
        attackBuff.canBeDispelled = true;

        // Визуальный эффект (красная боевая аура)
        attackBuff.particleEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Hit Light B (Air)");

        skill.effects.Add(attackBuff);

        // ════════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ════════════════════════════════════════════════════════════

        // Эффект каста (красное свечение паладина)
        skill.castEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Hit Light B (Air)");

        // Эффект на цели (красная аура на союзниках)
        skill.hitEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Hit Light B (Air)");

        // Эффект на кастере (золотое + красное свечение)
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
        skill.syncStatusEffects = true; // ВАЖНО! Синхронизируем бафф атаки

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
        string skillPath = $"{directory}/Paladin_DivineStrength.asset";
        AssetDatabase.CreateAsset(skill, skillPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Divine Strength создан: {skillPath}");
        Debug.Log("📋 Skill ID: 504");
        Debug.Log("⏱️ Cooldown: 90 секунд");
        Debug.Log("💙 Mana: 70");
        Debug.Log("📍 Radius: 10 метров");
        Debug.Log("⏳ Duration: 15 секунд");
        Debug.Log("⚔️ Effect: +50% к физической атаке");

        // Выделяем созданный asset в Project window
        Selection.activeObject = skill;
        EditorGUIUtility.PingObject(skill);
    }
}
