using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Создает префабы оружия из FBX файлов
/// </summary>
public class CreateWeaponPrefabs
{
    [MenuItem("Tools/Create Weapon Prefabs")]
    public static void CreatePrefabs()
    {
        string sourceFolder = "Assets/UI/Textures";
        string targetFolder = "Assets/Prefabs/Weapons";

        // Создаем папку для префабов если её нет
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
            AssetDatabase.Refresh();
        }

        // Список FBX файлов оружия
        string[] weaponFiles = new string[]
        {
            "sword_0915211634_texture.fbx",           // Warrior - Меч (правая рука)
            "Star_Shield_0915215007_texture.fbx",     // Warrior - Щит (левая рука)
            "Flamecrystal_Staff_0916183717_texture.fbx", // Mage - Посох
            "Recurve_Bow_0917080616_texture.fbx",     // Archer - Лук (левая рука)
            "Archer_s_Quiver_0917085133_texture.fbx", // Archer - Колчан (спина)
            "Reaper_s_Ascension_0918051819_texture.fbx", // Rogue - Кинжал
            "Sword_0916082404_texture.fbx"            // Paladin - Меч
        };

        string[] prefabNames = new string[]
        {
            "WarriorSword",
            "WarriorShield",
            "MageStaff",
            "ArcherBow",
            "ArcherQuiver",
            "RogueDagger",
            "PaladinSword"
        };

        int createdCount = 0;

        for (int i = 0; i < weaponFiles.Length; i++)
        {
            string fbxPath = Path.Combine(sourceFolder, weaponFiles[i]);
            string prefabPath = Path.Combine(targetFolder, prefabNames[i] + ".prefab");

            if (!File.Exists(fbxPath))
            {
                Debug.LogWarning($"Не найден файл: {fbxPath}");
                continue;
            }

            // Загружаем FBX
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

            if (fbxAsset == null)
            {
                Debug.LogWarning($"Не удалось загрузить FBX: {fbxPath}");
                continue;
            }

            // Создаем экземпляр
            GameObject weaponInstance = Object.Instantiate(fbxAsset);
            weaponInstance.name = prefabNames[i];

            // Удаляем ненужные компоненты
            Animator animator = weaponInstance.GetComponent<Animator>();
            if (animator != null)
            {
                Object.DestroyImmediate(animator);
            }

            // Создаем префаб
            bool success;
            PrefabUtility.SaveAsPrefabAsset(weaponInstance, prefabPath, out success);

            if (success)
            {
                Debug.Log($"✓ Создан префаб: {prefabPath}");
                createdCount++;
            }
            else
            {
                Debug.LogError($"Ошибка создания префаба: {prefabPath}");
            }

            // Удаляем временный экземпляр
            Object.DestroyImmediate(weaponInstance);
        }

        AssetDatabase.Refresh();
        Debug.Log($"\n=== Готово! Создано префабов: {createdCount}/{weaponFiles.Length} ===");
        Debug.Log($"Префабы находятся в: {targetFolder}");
    }
}
