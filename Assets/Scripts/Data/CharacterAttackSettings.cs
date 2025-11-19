using UnityEngine;

/// <summary>
/// Настройки атаки для конкретного класса персонажа
/// Сохраняются как ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "CharacterAttackSettings", menuName = "Aetherion/Character Attack Settings")]
public class CharacterAttackSettings : ScriptableObject
{
    [Header("Настройки для всех классов")]
    public ClassAttackConfig warrior;
    public ClassAttackConfig mage;
    public ClassAttackConfig archer;
    public ClassAttackConfig rogue;
    public ClassAttackConfig paladin;

    /// <summary>
    /// Получить настройки для класса
    /// </summary>
    public ClassAttackConfig GetConfigForClass(CharacterClass characterClass)
    {
        switch (characterClass)
        {
            case CharacterClass.Warrior: return warrior;
            case CharacterClass.Mage: return mage;
            case CharacterClass.Archer: return archer;
            case CharacterClass.Rogue: return rogue;
            case CharacterClass.Paladin: return paladin;
            default: return warrior;
        }
    }

    /// <summary>
    /// Получить настройки для класса по имени
    /// </summary>
    public ClassAttackConfig GetConfigForClass(string className)
    {
        if (System.Enum.TryParse(className, true, out CharacterClass characterClass))
        {
            return GetConfigForClass(characterClass);
        }
        return warrior;
    }
}

/// <summary>
/// Конфигурация атаки для одного класса
/// </summary>
[System.Serializable]
public class ClassAttackConfig
{
    [Header("Анимация")]
    [Tooltip("Скорость анимации атаки (1 = нормально, 3 = в 3 раза быстрее)")]
    [Range(0.1f, 20f)]
    public float attackAnimationSpeed = 1.0f;

    [Tooltip("Момент выстрела/удара в анимации (0 = начало, 1 = конец)")]
    [Range(0f, 1f)]
    public float attackHitTiming = 0.8f;

    [Header("Урон и дистанция")]
    public float attackDamage = 30f;
    public float attackRange = 3f;
    public float attackCooldown = 1.0f;

    [Header("Дальняя атака")]
    public bool isRangedAttack = false;
    public float projectileSpeed = 20f;
    public string projectilePrefabName = "";

    [Header("Поворот")]
    [Tooltip("Смещение поворота для компенсации анимации (градусы)")]
    public float attackRotationOffset = 0f;
}
