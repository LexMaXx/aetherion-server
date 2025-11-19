using UnityEngine;
using UnityEditor;

/// <summary>
/// Создаёт ScriptableObject с настройками атаки для всех классов
/// </summary>
public class CreateAttackSettingsAsset : EditorWindow
{
    [MenuItem("Tools/Create Attack Settings Asset")]
    public static void CreateAsset()
    {
        // Создаём новый ScriptableObject
        CharacterAttackSettings settings = ScriptableObject.CreateInstance<CharacterAttackSettings>();

        // Настраиваем Warrior
        settings.warrior = new ClassAttackConfig
        {
            attackAnimationSpeed = 1.0f,
            attackHitTiming = 0.7f,
            attackDamage = 30f,
            attackRange = 3f,
            attackCooldown = 1.0f,
            isRangedAttack = false,
            attackRotationOffset = 45f
        };

        // Настраиваем Mage
        settings.mage = new ClassAttackConfig
        {
            attackAnimationSpeed = 3.0f,  // Быстрая атака!
            attackHitTiming = 0.4f,
            attackDamage = 40f,
            attackRange = 20f,
            attackCooldown = 0.8f,
            isRangedAttack = true,
            projectileSpeed = 20f,
            projectilePrefabName = "FireballProjectile",
            attackRotationOffset = 0f
        };

        // Настраиваем Archer
        settings.archer = new ClassAttackConfig
        {
            attackAnimationSpeed = 1.0f,
            attackHitTiming = 0.5f,
            attackDamage = 35f,
            attackRange = 50f,
            attackCooldown = 1.2f,
            isRangedAttack = true,
            projectileSpeed = 30f,
            projectilePrefabName = "ArrowProjectile",
            attackRotationOffset = 0f
        };

        // Настраиваем Rogue
        settings.rogue = new ClassAttackConfig
        {
            attackAnimationSpeed = 1.0f,
            attackHitTiming = 0.6f,
            attackDamage = 50f,
            attackRange = 20f,
            attackCooldown = 1.0f,
            isRangedAttack = true,
            projectileSpeed = 15f,
            projectilePrefabName = "SoulShardProjectile",
            attackRotationOffset = -30f
        };

        // Настраиваем Paladin
        settings.paladin = new ClassAttackConfig
        {
            attackAnimationSpeed = 1.0f,
            attackHitTiming = 0.7f,
            attackDamage = 35f,
            attackRange = 4f,
            attackCooldown = 1.1f,
            isRangedAttack = false,
            attackRotationOffset = 0f
        };

        // Сохраняем asset
        string path = "Assets/Data/CharacterAttackSettings.asset";

        // Создаём папку Data если её нет
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }

        AssetDatabase.CreateAsset(settings, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ CharacterAttackSettings создан: {path}");
        Debug.Log("Теперь вы можете редактировать настройки атаки для каждого класса в этом файле!");

        // Выделяем созданный asset
        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }
}
