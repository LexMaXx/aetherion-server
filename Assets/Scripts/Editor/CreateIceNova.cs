using UnityEngine;
using UnityEditor;

/// <summary>
/// Создание Ice Nova скилла - AOE заморозка вокруг мага
/// </summary>
public class CreateIceNova : EditorWindow
{
    [MenuItem("Tools/Skills/Create Ice Nova")]
    public static void CreateSkill()
    {
        // Создаём SkillConfig
        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        // ═══════════════════════════════════════════════════════════
        // ОСНОВНАЯ ИНФОРМАЦИЯ
        // ═══════════════════════════════════════════════════════════
        skill.skillId = 202;
        skill.skillName = "Ice Nova";
        skill.description = "Ледяной взрыв вокруг мага, наносящий урон и замедляющий всех врагов в радиусе";
        skill.characterClass = CharacterClass.Mage;

        // ═══════════════════════════════════════════════════════════
        // ПАРАМЕТРЫ ИСПОЛЬЗОВАНИЯ
        // ═══════════════════════════════════════════════════════════
        skill.cooldown = 8f;
        skill.manaCost = 40f;
        skill.castRange = 0f; // Вокруг себя
        skill.castTime = 0.5f; // Быстрый каст
        skill.canUseWhileMoving = false;

        // ═══════════════════════════════════════════════════════════
        // ТИП СКИЛЛА - AOE DAMAGE
        // ═══════════════════════════════════════════════════════════
        skill.skillType = SkillConfigType.AOEDamage;
        skill.targetType = SkillTargetType.NoTarget; // Не требует цель
        skill.requiresTarget = false;
        skill.canTargetAllies = false;
        skill.canTargetEnemies = true;

        // ═══════════════════════════════════════════════════════════
        // УРОН
        // ═══════════════════════════════════════════════════════════
        skill.baseDamageOrHeal = 40f;
        skill.strengthScaling = 0f;
        skill.intelligenceScaling = 2.0f;
        // При 100 Intelligence: 40 + 100*2.0 = 240 урона
        // При 9 INT (тест): 40 + 9*2.0 = 58 урона

        // ═══════════════════════════════════════════════════════════
        // AOE ПАРАМЕТРЫ
        // ═══════════════════════════════════════════════════════════
        skill.aoeRadius = 8f; // 8 метров радиус
        skill.maxTargets = 10; // Максимум 10 врагов

        // ═══════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ═══════════════════════════════════════════════════════════
        // Эффект каста (вокруг мага)
        skill.castEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Ice B (Air).prefab"
        );

        // Эффект попадания на каждого врага
        skill.hitEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Ice B (Air).prefab"
        );

        // AOE эффект (ледяная волна)
        skill.aoeEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Effects/IceNovaSpawner.prefab"
        );

        if (skill.aoeEffectPrefab == null)
        {
            Debug.LogWarning("⚠️ IceNovaSpawner не найден, будет использован только Ice Hit эффект");
        }

        // ═══════════════════════════════════════════════════════════
        // АНИМАЦИЯ
        // ═══════════════════════════════════════════════════════════
        skill.animationTrigger = "Attack"; // Анимация каста
        skill.animationSpeed = 2.0f; // Быстрая анимация
        skill.projectileSpawnTiming = 0.3f; // Эффект появляется быстро

        // ═══════════════════════════════════════════════════════════
        // ЗВУКИ
        // ═══════════════════════════════════════════════════════════
        skill.soundVolume = 0.8f;

        // ═══════════════════════════════════════════════════════════
        // ЭФФЕКТЫ - SLOW (ЗАМЕДЛЕНИЕ)
        // ═══════════════════════════════════════════════════════════
        EffectConfig slowEffect = new EffectConfig();
        slowEffect.effectType = EffectType.DecreaseSpeed;
        slowEffect.duration = 3f; // 3 секунды замедления
        slowEffect.power = 50f; // 50% замедление
        slowEffect.damageOrHealPerTick = 0f; // Нет DoT
        slowEffect.tickInterval = 0f;
        slowEffect.intelligenceScaling = 0f;
        slowEffect.strengthScaling = 0f;

        // Визуальный эффект заморозки на цели
        slowEffect.particleEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Effects/CFXR3 Hit Ice B (Air).prefab"
        );

        slowEffect.canBeDispelled = true;
        slowEffect.canStack = false; // Замедление не стакается
        slowEffect.maxStacks = 1;
        slowEffect.syncWithServer = true;

        skill.effects = new System.Collections.Generic.List<EffectConfig> { slowEffect };

        // ═══════════════════════════════════════════════════════════
        // СЕТЕВАЯ СИНХРОНИЗАЦИЯ
        // ═══════════════════════════════════════════════════════════
        skill.syncProjectiles = false; // Нет снарядов
        skill.syncHitEffects = true;
        skill.syncStatusEffects = true;

        // ═══════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ
        // ═══════════════════════════════════════════════════════════
        string path = "Assets/ScriptableObjects/Skills/Mage/Mage_IceNova.asset";

        // Проверяем существование
        SkillConfig existing = AssetDatabase.LoadAssetAtPath<SkillConfig>(path);
        if (existing != null)
        {
            Debug.LogWarning("⚠️ Mage_IceNova уже существует! Удалите старый файл или переименуйте.");
            return;
        }

        AssetDatabase.CreateAsset(skill, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Выделяем созданный asset
        EditorGUIUtility.PingObject(skill);
        Selection.activeObject = skill;

        Debug.Log("════════════════════════════════════════════════════════════");
        Debug.Log("✅ Ice Nova успешно создан!");
        Debug.Log("════════════════════════════════════════════════════════════");
        Debug.Log($"📍 Путь: {path}");
        Debug.Log($"🧊 Урон: {skill.baseDamageOrHeal} + INT*{skill.intelligenceScaling}");
        Debug.Log($"🧊 Радиус: {skill.aoeRadius}м");
        Debug.Log($"🧊 Макс. целей: {skill.maxTargets}");
        Debug.Log($"❄️ Slow: {slowEffect.power}% на {slowEffect.duration}с");
        Debug.Log($"⏱️ Кулдаун: {skill.cooldown}с, Мана: {skill.manaCost}");
        Debug.Log("════════════════════════════════════════════════════════════");
        Debug.Log("📋 Следующий шаг:");
        Debug.Log("  1. Добавьте Ice Nova в TestPlayer → SkillExecutor → Equipped Skills[1]");
        Debug.Log("  2. Нажмите Play");
        Debug.Log("  3. Клавиша 2 - использовать Ice Nova (не требует цель!)");
        Debug.Log("════════════════════════════════════════════════════════════");
    }
}
