using UnityEngine;

/// <summary>
/// База данных оружия - хранит ссылки на все префабы оружия
/// Используется для автоматической загрузки оружия в любой сцене
/// </summary>
[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Aetherion/Weapon Database")]
public class WeaponDatabase : ScriptableObject
{
    [System.Serializable]
    public class WeaponEntry
    {
        public string weaponName;
        public GameObject weaponPrefab;
        public Vector3 defaultPosition = Vector3.zero;
        public Vector3 defaultRotation = Vector3.zero;
        public Vector3 defaultScale = Vector3.one;
    }

    [Header("Warrior Weapons")]
    public WeaponEntry warriorSword;
    public WeaponEntry warriorShield;

    [Header("Mage Weapons")]
    public WeaponEntry mageStaff;

    [Header("Archer Weapons")]
    public WeaponEntry archerBow;
    public WeaponEntry archerQuiver;

    [Header("Rogue Weapons")]
    public WeaponEntry rogueDagger;

    [Header("Paladin Weapons")]
    public WeaponEntry paladinSword;

    /// <summary>
    /// Получить оружие для правой руки по классу
    /// </summary>
    public WeaponEntry GetRightHandWeapon(CharacterClass characterClass)
    {
        WeaponEntry result = null;

        switch (characterClass)
        {
            case CharacterClass.Warrior:
                result = warriorSword;
                break;
            case CharacterClass.Mage:
                result = mageStaff;
                break;
            case CharacterClass.Archer:
                result = null; // Лучник держит лук в левой руке
                break;
            case CharacterClass.Rogue:
                result = rogueDagger;
                break;
            case CharacterClass.Paladin:
                result = paladinSword;
                break;
        }

        if (result != null)
        {
            Debug.Log($"[WeaponDatabase] GetRightHandWeapon({characterClass}): {result.weaponName}, Prefab={(result.weaponPrefab != null ? "✓" : "NULL")}, Pos={result.defaultPosition}, Rot={result.defaultRotation}, Scale={result.defaultScale}");
        }

        return result;
    }

    /// <summary>
    /// Получить оружие для левой руки по классу
    /// </summary>
    public WeaponEntry GetLeftHandWeapon(CharacterClass characterClass)
    {
        WeaponEntry result = null;

        switch (characterClass)
        {
            case CharacterClass.Warrior:
                result = warriorShield;
                break;
            case CharacterClass.Archer:
                result = archerBow;
                break;
        }

        if (result != null)
        {
            Debug.Log($"[WeaponDatabase] GetLeftHandWeapon({characterClass}): {result.weaponName}, Prefab={(result.weaponPrefab != null ? "✓" : "NULL")}, Pos={result.defaultPosition}, Rot={result.defaultRotation}, Scale={result.defaultScale}");
        }

        return result;
    }

    /// <summary>
    /// Получить оружие для спины по классу
    /// </summary>
    public WeaponEntry GetBackWeapon(CharacterClass characterClass)
    {
        WeaponEntry result = null;

        switch (characterClass)
        {
            case CharacterClass.Archer:
                result = archerQuiver;
                break;
        }

        if (result != null)
        {
            Debug.Log($"[WeaponDatabase] GetBackWeapon({characterClass}): {result.weaponName}, Prefab={(result.weaponPrefab != null ? "✓" : "NULL")}, Pos={result.defaultPosition}, Rot={result.defaultRotation}, Scale={result.defaultScale}");
        }

        return result;
    }

    private static WeaponDatabase instance;

    /// <summary>
    /// Получить экземпляр базы данных (синглтон)
    /// </summary>
    public static WeaponDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<WeaponDatabase>("WeaponDatabase");
                if (instance == null)
                {
                    Debug.LogError("WeaponDatabase не найдена в Resources! Создайте через Create → Aetherion → Weapon Database");
                }
            }
            return instance;
        }
    }
}
