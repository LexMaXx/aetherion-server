using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Создает WeaponDatabase и автоматически заполняет её префабами оружия
/// </summary>
public class CreateWeaponDatabase
{
    [MenuItem("Tools/Create Weapon Database")]
    public static void CreateDatabase()
    {
        // Проверяем существование папки Resources
        string resourcesPath = "Assets/Resources";
        if (!Directory.Exists(resourcesPath))
        {
            Directory.CreateDirectory(resourcesPath);
            AssetDatabase.Refresh();
            Debug.Log("✓ Создана папка Resources");
        }

        // Путь к базе данных
        string dbPath = "Assets/Resources/WeaponDatabase.asset";

        // Проверяем существование
        WeaponDatabase existingDB = AssetDatabase.LoadAssetAtPath<WeaponDatabase>(dbPath);

        if (existingDB != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "База данных существует",
                "WeaponDatabase уже существует. Перезаписать?",
                "Да, перезаписать",
                "Отмена"
            );

            if (!overwrite)
            {
                Debug.Log("Создание отменено");
                return;
            }
        }

        // Создаем базу данных
        WeaponDatabase db = ScriptableObject.CreateInstance<WeaponDatabase>();

        // Путь к префабам оружия
        string weaponPrefabPath = "Assets/Prefabs/Weapons";

        Debug.Log($"\n=== Создание WeaponDatabase ===");
        Debug.Log($"Загрузка префабов из: {weaponPrefabPath}");

        // Загружаем префабы
        GameObject warriorSword = AssetDatabase.LoadAssetAtPath<GameObject>($"{weaponPrefabPath}/WarriorSword.prefab");
        GameObject warriorShield = AssetDatabase.LoadAssetAtPath<GameObject>($"{weaponPrefabPath}/WarriorShield.prefab");
        GameObject mageStaff = AssetDatabase.LoadAssetAtPath<GameObject>($"{weaponPrefabPath}/MageStaff.prefab");
        GameObject archerBow = AssetDatabase.LoadAssetAtPath<GameObject>($"{weaponPrefabPath}/ArcherBow.prefab");
        GameObject archerQuiver = AssetDatabase.LoadAssetAtPath<GameObject>($"{weaponPrefabPath}/ArcherQuiver.prefab");
        GameObject rogueDagger = AssetDatabase.LoadAssetAtPath<GameObject>($"{weaponPrefabPath}/RogueDagger.prefab");
        GameObject paladinSword = AssetDatabase.LoadAssetAtPath<GameObject>($"{weaponPrefabPath}/PaladinSword.prefab");

        // Warrior
        db.warriorSword = new WeaponDatabase.WeaponEntry
        {
            weaponName = "Warrior Sword",
            weaponPrefab = warriorSword,
            defaultPosition = Vector3.zero,
            defaultRotation = new Vector3(0, 90, 0),
            defaultScale = Vector3.one
        };

        db.warriorShield = new WeaponDatabase.WeaponEntry
        {
            weaponName = "Warrior Shield",
            weaponPrefab = warriorShield,
            defaultPosition = Vector3.zero,
            defaultRotation = Vector3.zero,
            defaultScale = Vector3.one
        };

        // Mage
        db.mageStaff = new WeaponDatabase.WeaponEntry
        {
            weaponName = "Mage Staff",
            weaponPrefab = mageStaff,
            defaultPosition = Vector3.zero,
            defaultRotation = Vector3.zero,
            defaultScale = Vector3.one
        };

        // Archer
        db.archerBow = new WeaponDatabase.WeaponEntry
        {
            weaponName = "Archer Bow",
            weaponPrefab = archerBow,
            defaultPosition = Vector3.zero,
            defaultRotation = Vector3.zero,
            defaultScale = Vector3.one
        };

        db.archerQuiver = new WeaponDatabase.WeaponEntry
        {
            weaponName = "Archer Quiver",
            weaponPrefab = archerQuiver,
            defaultPosition = new Vector3(0, 0.2f, -0.2f),
            defaultRotation = Vector3.zero,
            defaultScale = Vector3.one
        };

        // Rogue
        db.rogueDagger = new WeaponDatabase.WeaponEntry
        {
            weaponName = "Rogue Dagger",
            weaponPrefab = rogueDagger,
            defaultPosition = Vector3.zero,
            defaultRotation = Vector3.zero,
            defaultScale = Vector3.one
        };

        // Paladin
        db.paladinSword = new WeaponDatabase.WeaponEntry
        {
            weaponName = "Paladin Sword",
            weaponPrefab = paladinSword,
            defaultPosition = Vector3.zero,
            defaultRotation = new Vector3(0, 90, 0),
            defaultScale = Vector3.one
        };

        // Сохраняем
        AssetDatabase.CreateAsset(db, dbPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Выделяем созданный файл
        EditorGUIUtility.PingObject(db);
        Selection.activeObject = db;

        Debug.Log($"\n✓ WeaponDatabase создана: {dbPath}");
        Debug.Log($"Загружено оружия:");
        Debug.Log($"  Warrior: {(warriorSword != null ? "✓" : "✗")} Sword, {(warriorShield != null ? "✓" : "✗")} Shield");
        Debug.Log($"  Mage: {(mageStaff != null ? "✓" : "✗")} Staff");
        Debug.Log($"  Archer: {(archerBow != null ? "✓" : "✗")} Bow, {(archerQuiver != null ? "✓" : "✗")} Quiver");
        Debug.Log($"  Rogue: {(rogueDagger != null ? "✓" : "✗")} Dagger");
        Debug.Log($"  Paladin: {(paladinSword != null ? "✓" : "✗")} Sword");

        EditorUtility.DisplayDialog(
            "Успех!",
            "WeaponDatabase создана и настроена!\nПуть: Assets/Resources/WeaponDatabase.asset\n\nТеперь оружие будет автоматически загружаться во всех сценах.",
            "OK"
        );
    }
}
