using UnityEngine;

/// <summary>
/// Выдаёт опыт за убийство монстров (TargetableEntity) локальному или сетевому игроку.
/// Добавьте компонент на любой NPC/монстра и задайте базовую награду.
/// </summary>
[RequireComponent(typeof(TargetableEntity))]
public class MonsterExperienceReward : MonoBehaviour
{
    [Header("Experience")]
    [Tooltip("Базовое количество опыта за убийство этого монстра.")]
    [SerializeField] private int baseExperience = 25;

    [Tooltip("«Уровень» монстра для расчёта бонусов/штрафов.")]
    [SerializeField] private int monsterLevel = 1;

    [Tooltip("Бонус к опыту за каждый уровень монстра выше игрока (в долях от базового значения).")]
    [SerializeField] private float higherLevelBonusPerLevel = 0.2f;

    [Tooltip("Минимальная доля опыта при убийстве монстра значительно ниже уровнем.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float lowerLevelPenalty = 0.4f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private TargetableEntity targetable;

    private void Awake()
    {
        targetable = GetComponent<TargetableEntity>();
        if (targetable == null)
        {
            Debug.LogError("[MonsterExperienceReward] TargetableEntity не найден! Компонент отключён.");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (targetable != null)
        {
            targetable.OnDeath += OnMonsterDeath;
        }
    }

    private void OnDisable()
    {
        if (targetable != null)
        {
            targetable.OnDeath -= OnMonsterDeath;
        }
    }

    private void OnMonsterDeath(TargetableEntity deadEntity)
    {
        if (deadEntity == null)
            return;

        TargetableEntity killer = deadEntity.GetLastAttacker();
        if (killer == null)
        {
            Log("У монстра нет записанного убийцы – опыт не выдан.");
            return;
        }

        LevelingSystem leveling = ResolveLevelingSystem(killer);
        if (leveling == null)
        {
            Log($"Убийца {killer.GetEntityName()} не имеет LevelingSystem – опыт не выдан.");
            return;
        }

        int experienceReward = CalculateExperience(leveling.CurrentLevel);
        if (experienceReward <= 0)
            return;

        leveling.GainExperience(experienceReward);
        Log($"+{experienceReward} XP игроку {killer.GetEntityName()} за {deadEntity.GetEntityName()} (monster lvl {monsterLevel}).");
    }

    private LevelingSystem ResolveLevelingSystem(TargetableEntity killer)
    {
        if (killer == null)
            return null;

        LevelingSystem leveling = killer.GetComponent<LevelingSystem>();

        if (leveling == null)
            leveling = killer.GetComponentInParent<LevelingSystem>();

        if (leveling == null)
            leveling = killer.GetComponentInChildren<LevelingSystem>();

        if (leveling == null)
        {
            // Попробуем найти на корневом объекте игрока (например, PlayerController держит LevelingSystem на дочернем)
            var root = killer.transform.root;
            if (root != null)
            {
                leveling = root.GetComponentInChildren<LevelingSystem>();
            }
        }

        return leveling;
    }

    private int CalculateExperience(int killerLevel)
    {
        float reward = Mathf.Max(1, baseExperience);
        int levelDifference = monsterLevel - Mathf.Max(1, killerLevel);

        if (levelDifference > 0)
        {
            reward *= 1f + levelDifference * Mathf.Max(0f, higherLevelBonusPerLevel);
        }
        else if (levelDifference < 0)
        {
            float penalty = 1f + levelDifference * 0.1f; // levelDifference отрицательный
            reward *= Mathf.Max(lowerLevelPenalty, penalty);
        }

        return Mathf.Max(1, Mathf.RoundToInt(reward));
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[MonsterExperienceReward] {message}");
        }
    }
}
