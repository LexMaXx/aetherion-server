using UnityEngine;
using UnityEditor;

/// <summary>
/// Обновляет WeaponDatabase с правильными префабами
/// </summary>
public class UpdateWeaponDatabase
{
    [MenuItem("Tools/Update Weapon Database")]
    public static void UpdateDatabase()
    {
        Debug.Log("\n=== ОБНОВЛЕНИЕ WEAPON DATABASE ===\n");

        // Загружаем WeaponDatabase
        WeaponDatabase db = AssetDatabase.LoadAssetAtPath<WeaponDatabase>("Assets/Resources/WeaponDatabase.asset");

        if (db == null)
        {
            Debug.LogError("❌ WeaponDatabase не найдена!");
            return;
        }

        Debug.Log("✓ WeaponDatabase загружена");

        // Загружаем все префабы оружия
        GameObject warriorSword = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/WarriorSword.prefab");
        GameObject warriorShield = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/WarriorShield.prefab");
        GameObject mageStaff = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/MageStaff.prefab");
        GameObject archerBow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/ArcherBow.prefab");
        GameObject archerQuiver = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/ArcherQuiver.prefab");
        GameObject rogueDagger = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/RogueDagger.prefab");
        GameObject paladinSword = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/PaladinSword.prefab");

        // Обновляем записи
        int updatedCount = 0;

        if (warriorSword != null)
        {
            if (db.warriorSword == null) db.warriorSword = new WeaponDatabase.WeaponEntry();
            db.warriorSword.weaponName = "Warrior Sword";
            db.warriorSword.weaponPrefab = warriorSword;
            Debug.Log("✓ Warrior Sword обновлен");
            updatedCount++;
        }

        if (warriorShield != null)
        {
            if (db.warriorShield == null) db.warriorShield = new WeaponDatabase.WeaponEntry();
            db.warriorShield.weaponName = "Warrior Shield";
            db.warriorShield.weaponPrefab = warriorShield;
            Debug.Log("✓ Warrior Shield обновлен");
            updatedCount++;
        }

        if (mageStaff != null)
        {
            if (db.mageStaff == null) db.mageStaff = new WeaponDatabase.WeaponEntry();
            db.mageStaff.weaponName = "Mage Staff";
            db.mageStaff.weaponPrefab = mageStaff;
            Debug.Log("✓ Mage Staff обновлен");
            updatedCount++;
        }

        if (archerBow != null)
        {
            if (db.archerBow == null) db.archerBow = new WeaponDatabase.WeaponEntry();
            db.archerBow.weaponName = "Archer Bow";
            db.archerBow.weaponPrefab = archerBow;
            Debug.Log("✓ Archer Bow обновлен");
            updatedCount++;
        }

        if (archerQuiver != null)
        {
            if (db.archerQuiver == null) db.archerQuiver = new WeaponDatabase.WeaponEntry();
            db.archerQuiver.weaponName = "Archer Quiver";
            db.archerQuiver.weaponPrefab = archerQuiver;
            Debug.Log("✓ Archer Quiver обновлен");
            updatedCount++;
        }
        else
        {
            Debug.LogError("❌ ArcherQuiver.prefab не найден!");
        }

        if (rogueDagger != null)
        {
            if (db.rogueDagger == null) db.rogueDagger = new WeaponDatabase.WeaponEntry();
            db.rogueDagger.weaponName = "Rogue Dagger";
            db.rogueDagger.weaponPrefab = rogueDagger;
            Debug.Log("✓ Rogue Dagger обновлен");
            updatedCount++;
        }

        if (paladinSword != null)
        {
            if (db.paladinSword == null) db.paladinSword = new WeaponDatabase.WeaponEntry();
            db.paladinSword.weaponName = "Paladin Sword";
            db.paladinSword.weaponPrefab = paladinSword;
            Debug.Log("✓ Paladin Sword обновлен");
            updatedCount++;
        }

        // Сохраняем изменения
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"\n✓✓✓ ГОТОВО! Обновлено префабов: {updatedCount}/7");
        EditorUtility.DisplayDialog("Успех!",
            $"WeaponDatabase обновлена!\n\nОбновлено префабов: {updatedCount}/7\n\nТеперь запустите Tools → Copy Weapon Settings From CharacterSelection",
            "OK");
    }
}
