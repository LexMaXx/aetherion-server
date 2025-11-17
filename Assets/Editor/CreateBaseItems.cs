using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor tool to create base ItemData assets for testing
/// </summary>
[InitializeOnLoad]
public class CreateBaseItems : EditorWindow
{
    static CreateBaseItems()
    {
        // Auto-create items on editor load (after domain reload)
        EditorApplication.delayCall += () =>
        {
            CreateItemsSilent();
        };
    }

    [MenuItem("Tools/Aetherion/Create Base Items")]
    public static void CreateItems()
    {
        int created = CreateItemsSilent();
        EditorUtility.DisplayDialog("Create Base Items",
            $"Created {created} new items in Assets/Resources/Data/Items", "OK");
    }

    private static int CreateItemsSilent()
    {
        string folderPath = "Assets/Resources/Data/Items";

        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Data"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Data");
        }
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Resources/Data", "Items");
        }

        int created = 0;

        // Create Health Potion
        if (!AssetExists(folderPath, "Health Potion"))
        {
            ItemData healthPotion = ScriptableObject.CreateInstance<ItemData>();
            healthPotion.itemName = "Health Potion";
            healthPotion.description = "Restores a moderate amount of health.";
            healthPotion.itemType = ItemType.Consumable;
            healthPotion.isStackable = true;
            healthPotion.maxStackSize = 99;
            healthPotion.isEquippable = false;
            healthPotion.healAmount = 50;
            healthPotion.manaRestoreAmount = 0;
            healthPotion.sellPrice = 25;
            healthPotion.buyPrice = 100;
            AssetDatabase.CreateAsset(healthPotion, $"{folderPath}/Health Potion.asset");
            created++;
        }

        // Create Mana Potion
        if (!AssetExists(folderPath, "Mana Potion"))
        {
            ItemData manaPotion = ScriptableObject.CreateInstance<ItemData>();
            manaPotion.itemName = "Mana Potion";
            manaPotion.description = "Restores a moderate amount of mana.";
            manaPotion.itemType = ItemType.Consumable;
            manaPotion.isStackable = true;
            manaPotion.maxStackSize = 99;
            manaPotion.isEquippable = false;
            manaPotion.healAmount = 0;
            manaPotion.manaRestoreAmount = 50;
            manaPotion.sellPrice = 30;
            manaPotion.buyPrice = 120;
            AssetDatabase.CreateAsset(manaPotion, $"{folderPath}/Mana Potion.asset");
            created++;
        }

        // Create Iron Sword (Weapon)
        if (!AssetExists(folderPath, "Iron Sword"))
        {
            ItemData ironSword = ScriptableObject.CreateInstance<ItemData>();
            ironSword.itemName = "Iron Sword";
            ironSword.description = "A basic iron sword. Reliable and sturdy.";
            ironSword.itemType = ItemType.Weapon;
            ironSword.isStackable = false;
            ironSword.maxStackSize = 1;
            ironSword.isEquippable = true;
            ironSword.equipmentSlot = EquipmentSlot.Weapon;
            ironSword.attackBonus = 15;
            ironSword.defenseBonus = 0;
            ironSword.healthBonus = 0;
            ironSword.manaBonus = 0;
            ironSword.sellPrice = 100;
            ironSword.buyPrice = 400;
            AssetDatabase.CreateAsset(ironSword, $"{folderPath}/Iron Sword.asset");
            created++;
        }

        // Create Steel Sword (Weapon)
        if (!AssetExists(folderPath, "Steel Sword"))
        {
            ItemData steelSword = ScriptableObject.CreateInstance<ItemData>();
            steelSword.itemName = "Steel Sword";
            steelSword.description = "A well-crafted steel sword with superior sharpness.";
            steelSword.itemType = ItemType.Weapon;
            steelSword.isStackable = false;
            steelSword.maxStackSize = 1;
            steelSword.isEquippable = true;
            steelSword.equipmentSlot = EquipmentSlot.Weapon;
            steelSword.attackBonus = 25;
            steelSword.defenseBonus = 0;
            steelSword.healthBonus = 0;
            steelSword.manaBonus = 0;
            steelSword.sellPrice = 200;
            steelSword.buyPrice = 800;
            AssetDatabase.CreateAsset(steelSword, $"{folderPath}/Steel Sword.asset");
            created++;
        }

        // Create Leather Armor
        if (!AssetExists(folderPath, "Leather Armor"))
        {
            ItemData leatherArmor = ScriptableObject.CreateInstance<ItemData>();
            leatherArmor.itemName = "Leather Armor";
            leatherArmor.description = "Light armor made of leather. Provides basic protection.";
            leatherArmor.itemType = ItemType.Armor;
            leatherArmor.isStackable = false;
            leatherArmor.maxStackSize = 1;
            leatherArmor.isEquippable = true;
            leatherArmor.equipmentSlot = EquipmentSlot.Armor;
            leatherArmor.attackBonus = 0;
            leatherArmor.defenseBonus = 10;
            leatherArmor.healthBonus = 20;
            leatherArmor.manaBonus = 0;
            leatherArmor.sellPrice = 80;
            leatherArmor.buyPrice = 350;
            AssetDatabase.CreateAsset(leatherArmor, $"{folderPath}/Leather Armor.asset");
            created++;
        }

        // Create Iron Armor
        if (!AssetExists(folderPath, "Iron Armor"))
        {
            ItemData ironArmor = ScriptableObject.CreateInstance<ItemData>();
            ironArmor.itemName = "Iron Armor";
            ironArmor.description = "Heavy iron armor. Excellent protection at the cost of mobility.";
            ironArmor.itemType = ItemType.Armor;
            ironArmor.isStackable = false;
            ironArmor.maxStackSize = 1;
            ironArmor.isEquippable = true;
            ironArmor.equipmentSlot = EquipmentSlot.Armor;
            ironArmor.attackBonus = 0;
            ironArmor.defenseBonus = 20;
            ironArmor.healthBonus = 40;
            ironArmor.manaBonus = 0;
            ironArmor.sellPrice = 150;
            ironArmor.buyPrice = 600;
            AssetDatabase.CreateAsset(ironArmor, $"{folderPath}/Iron Armor.asset");
            created++;
        }

        // Create Iron Helmet
        if (!AssetExists(folderPath, "Iron Helmet"))
        {
            ItemData ironHelmet = ScriptableObject.CreateInstance<ItemData>();
            ironHelmet.itemName = "Iron Helmet";
            ironHelmet.description = "A sturdy iron helmet protecting the head.";
            ironHelmet.itemType = ItemType.Helmet;
            ironHelmet.isStackable = false;
            ironHelmet.maxStackSize = 1;
            ironHelmet.isEquippable = true;
            ironHelmet.equipmentSlot = EquipmentSlot.Helmet;
            ironHelmet.attackBonus = 0;
            ironHelmet.defenseBonus = 8;
            ironHelmet.healthBonus = 15;
            ironHelmet.manaBonus = 0;
            ironHelmet.sellPrice = 60;
            ironHelmet.buyPrice = 250;
            AssetDatabase.CreateAsset(ironHelmet, $"{folderPath}/Iron Helmet.asset");
            created++;
        }

        // Create Leather Cap
        if (!AssetExists(folderPath, "Leather Cap"))
        {
            ItemData leatherCap = ScriptableObject.CreateInstance<ItemData>();
            leatherCap.itemName = "Leather Cap";
            leatherCap.description = "A light leather cap offering minimal protection.";
            leatherCap.itemType = ItemType.Helmet;
            leatherCap.isStackable = false;
            leatherCap.maxStackSize = 1;
            leatherCap.isEquippable = true;
            leatherCap.equipmentSlot = EquipmentSlot.Helmet;
            leatherCap.attackBonus = 0;
            leatherCap.defenseBonus = 4;
            leatherCap.healthBonus = 10;
            leatherCap.manaBonus = 0;
            leatherCap.sellPrice = 40;
            leatherCap.buyPrice = 180;
            AssetDatabase.CreateAsset(leatherCap, $"{folderPath}/Leather Cap.asset");
            created++;
        }

        // Create Magic Ring (Accessory)
        if (!AssetExists(folderPath, "Magic Ring"))
        {
            ItemData magicRing = ScriptableObject.CreateInstance<ItemData>();
            magicRing.itemName = "Magic Ring";
            magicRing.description = "A ring imbued with magical energy. Increases mana capacity.";
            magicRing.itemType = ItemType.Accessory;
            magicRing.isStackable = false;
            magicRing.maxStackSize = 1;
            magicRing.isEquippable = true;
            magicRing.equipmentSlot = EquipmentSlot.Accessory;
            magicRing.attackBonus = 5;
            magicRing.defenseBonus = 0;
            magicRing.healthBonus = 0;
            magicRing.manaBonus = 30;
            magicRing.sellPrice = 120;
            magicRing.buyPrice = 500;
            AssetDatabase.CreateAsset(magicRing, $"{folderPath}/Magic Ring.asset");
            created++;
        }

        // Create Power Amulet (Accessory)
        if (!AssetExists(folderPath, "Power Amulet"))
        {
            ItemData powerAmulet = ScriptableObject.CreateInstance<ItemData>();
            powerAmulet.itemName = "Power Amulet";
            powerAmulet.description = "An amulet that enhances physical prowess.";
            powerAmulet.itemType = ItemType.Accessory;
            powerAmulet.isStackable = false;
            powerAmulet.maxStackSize = 1;
            powerAmulet.isEquippable = true;
            powerAmulet.equipmentSlot = EquipmentSlot.Accessory;
            powerAmulet.attackBonus = 10;
            powerAmulet.defenseBonus = 5;
            powerAmulet.healthBonus = 10;
            powerAmulet.manaBonus = 0;
            powerAmulet.sellPrice = 150;
            powerAmulet.buyPrice = 600;
            AssetDatabase.CreateAsset(powerAmulet, $"{folderPath}/Power Amulet.asset");
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (created > 0)
        {
            Debug.Log($"[CreateBaseItems] Created {created} items in {folderPath}");
        }

        return created;
    }

    private static bool AssetExists(string folderPath, string assetName)
    {
        string path = $"{folderPath}/{assetName}.asset";
        return AssetDatabase.LoadAssetAtPath<ItemData>(path) != null;
    }
}
