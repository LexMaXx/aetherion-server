using UnityEngine;

/// <summary>
/// Начальное распределение SPECIAL характеристик для класса персонажа
/// Создать: Assets → Create → Aetherion → Class Stats Preset
/// </summary>
[CreateAssetMenu(fileName = "ClassStatsPreset", menuName = "Aetherion/Class Stats Preset", order = 2)]
public class ClassStatsPreset : ScriptableObject
{
    [Header("Class Info")]
    [Tooltip("Название класса (Warrior, Mage, Archer, Rogue, Paladin)")]
    public string className = "Warrior";

    [Header("SPECIAL Stats (Total = 15, Min each = 1, Max each = 10)")]
    [Tooltip("Сила - физический урон")]
    [Range(1, 10)]
    public int strength = 4;

    [Tooltip("Восприятие - радиус Fog of War (макс 40м)")]
    [Range(1, 10)]
    public int perception = 2;

    [Tooltip("Выносливость - здоровье")]
    [Range(1, 10)]
    public int endurance = 3;

    [Tooltip("Мудрость - мана и регенерация маны")]
    [Range(1, 10)]
    public int wisdom = 1;

    [Tooltip("Интеллект - магический урон")]
    [Range(1, 10)]
    public int intelligence = 1;

    [Tooltip("Ловкость - очки действия (макс 12) и скорость регена")]
    [Range(1, 10)]
    public int agility = 2;

    [Tooltip("Удача - шанс крита (макс 35%)")]
    [Range(1, 10)]
    public int luck = 2;

    /// <summary>
    /// Получить сумму всех характеристик
    /// </summary>
    public int GetTotalPoints()
    {
        return strength + perception + endurance + wisdom + intelligence + agility + luck;
    }

    /// <summary>
    /// Валидация: проверка что сумма = 15
    /// </summary>
    private void OnValidate()
    {
        int total = GetTotalPoints();
        if (total != 15)
        {
            Debug.LogWarning($"[{className}] Сумма характеристик = {total}, должна быть 15!");
        }

        // Проверка что все характеристики >= 1
        if (strength < 1) strength = 1;
        if (perception < 1) perception = 1;
        if (endurance < 1) endurance = 1;
        if (wisdom < 1) wisdom = 1;
        if (intelligence < 1) intelligence = 1;
        if (agility < 1) agility = 1;
        if (luck < 1) luck = 1;
    }
}
