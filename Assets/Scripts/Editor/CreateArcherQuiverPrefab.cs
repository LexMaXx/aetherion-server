using UnityEngine;
using UnityEditor;

/// <summary>
/// Создает префаб колчана для лучника
/// </summary>
public class CreateArcherQuiverPrefab
{
    [MenuItem("Tools/Create Archer Quiver Prefab")]
    public static void CreatePrefab()
    {
        string fbxPath = "Assets/UI/Textures/Archer_s_Quiver_0917085133_texture.fbx";
        string prefabPath = "Assets/Prefabs/Weapons/ArcherQuiver.prefab";

        // Загружаем FBX
        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

        if (fbxAsset == null)
        {
            Debug.LogError($"❌ Не удалось загрузить FBX: {fbxPath}");
            return;
        }

        // Создаем экземпляр
        GameObject quiverInstance = Object.Instantiate(fbxAsset);
        quiverInstance.name = "ArcherQuiver";

        // Удаляем Animator если есть
        Animator animator = quiverInstance.GetComponent<Animator>();
        if (animator != null)
        {
            Object.DestroyImmediate(animator);
        }

        // Создаем префаб
        bool success;
        PrefabUtility.SaveAsPrefabAsset(quiverInstance, prefabPath, out success);

        if (success)
        {
            Debug.Log($"✓ Создан префаб колчана: {prefabPath}");
        }
        else
        {
            Debug.LogError($"❌ Ошибка создания префаба: {prefabPath}");
        }

        // Удаляем временный экземпляр
        Object.DestroyImmediate(quiverInstance);

        AssetDatabase.Refresh();

        // Теперь обновляем WeaponDatabase
        UpdateWeaponDatabase();
    }

    private static void UpdateWeaponDatabase()
    {
        WeaponDatabase db = AssetDatabase.LoadAssetAtPath<WeaponDatabase>("Assets/Resources/WeaponDatabase.asset");

        if (db == null)
        {
            Debug.LogError("❌ WeaponDatabase не найдена!");
            return;
        }

        // Загружаем новый префаб колчана
        GameObject quiverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/ArcherQuiver.prefab");

        if (quiverPrefab == null)
        {
            Debug.LogError("❌ Не удалось загрузить ArcherQuiver.prefab!");
            return;
        }

        // Обновляем запись в базе данных
        if (db.archerQuiver == null)
        {
            db.archerQuiver = new WeaponDatabase.WeaponEntry();
        }

        db.archerQuiver.weaponName = "Archer Quiver";
        db.archerQuiver.weaponPrefab = quiverPrefab;

        // Сохраняем изменения
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log("✓ WeaponDatabase обновлена с префабом колчана");
    }
}
