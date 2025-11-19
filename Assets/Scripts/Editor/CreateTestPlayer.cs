using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Простой скрипт для создания TestPlayer в текущей сцене
/// </summary>
public class CreateTestPlayer : Editor
{
    [MenuItem("Aetherion/Create Test Player in Scene")]
    public static void CreatePlayer()
    {
        Debug.Log("🎮 Создание Test Player...");

        // Удаляем старого если есть
        GameObject oldPlayer = GameObject.Find("TestPlayer");
        if (oldPlayer != null)
        {
            DestroyImmediate(oldPlayer);
            Debug.Log("🗑️ Старый TestPlayer удалён");
        }

        // Создаём нового игрока
        GameObject testPlayer = new GameObject("TestPlayer");
        testPlayer.tag = "Player";
        testPlayer.layer = LayerMask.NameToLayer("Default");

        // Позиция
        GameObject spawnPoint = GameObject.Find("PlayerSpawnPoint");
        if (spawnPoint != null)
        {
            testPlayer.transform.position = spawnPoint.transform.position;
        }
        else
        {
            testPlayer.transform.position = new Vector3(0, 1, 0);
        }

        // ═══════════════════════════════════════════════════════════
        // КОМПОНЕНТЫ
        // ═══════════════════════════════════════════════════════════

        // CharacterController для движения
        CharacterController cc = testPlayer.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0, 1, 0);

        // CharacterStats (SPECIAL система)
        CharacterStats stats = testPlayer.AddComponent<CharacterStats>();
        stats.strength = 3;
        stats.perception = 5;
        stats.endurance = 4;
        stats.wisdom = 8;         // High wisdom = lots of MP
        stats.intelligence = 9;   // High intelligence = magic damage
        stats.agility = 4;
        stats.luck = 5;

        // HealthSystem
        testPlayer.AddComponent<HealthSystem>();

        // ManaSystem
        testPlayer.AddComponent<ManaSystem>();

        // SkillExecutor (НОВАЯ СИСТЕМА)
        SkillExecutor skillExecutor = testPlayer.AddComponent<SkillExecutor>();

        // EffectManager (НОВАЯ СИСТЕМА)
        testPlayer.AddComponent<EffectManager>();

        // PlayerAttackNew
        testPlayer.AddComponent<PlayerAttackNew>();

        // SimplePlayerController
        testPlayer.AddComponent<SimplePlayerController>();

        // ═══════════════════════════════════════════════════════════
        // ВИЗУАЛ
        // ═══════════════════════════════════════════════════════════

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        visual.transform.SetParent(testPlayer.transform);
        visual.transform.localPosition = new Vector3(0, 1, 0);
        visual.transform.localScale = Vector3.one;

        // Удаляем Collider у визуала
        DestroyImmediate(visual.GetComponent<CapsuleCollider>());

        // Синий материал (маг)
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = Color.blue;
        visual.GetComponent<Renderer>().material = mat;

        // Точка спавна снарядов
        GameObject projectileSpawn = new GameObject("ProjectileSpawnPoint");
        projectileSpawn.transform.SetParent(testPlayer.transform);
        projectileSpawn.transform.localPosition = new Vector3(0, 1.5f, 0.5f);

        // ═══════════════════════════════════════════════════════════
        // СКИЛЛЫ МАГА
        // ═══════════════════════════════════════════════════════════

        skillExecutor.equippedSkills.Clear();

        // Slot 0: Fireball
        SkillConfig fireball = AssetDatabase.LoadAssetAtPath<SkillConfig>(
            "Assets/Resources/Skills/Mage_Fireball.asset"
        );
        if (fireball == null)
        {
            fireball = AssetDatabase.LoadAssetAtPath<SkillConfig>(
                "Assets/ScriptableObjects/Skills/Mage/Mage_Fireball.asset"
            );
        }

        // Slot 1: Ice Nova
        SkillConfig iceNova = AssetDatabase.LoadAssetAtPath<SkillConfig>(
            "Assets/Resources/Skills/Mage_IceNova.asset"
        );
        if (iceNova == null)
        {
            iceNova = AssetDatabase.LoadAssetAtPath<SkillConfig>(
                "Assets/ScriptableObjects/Skills/Mage/Mage_IceNova.asset"
            );
        }

        // Slot 2: Lightning Storm
        SkillConfig lightningStorm = AssetDatabase.LoadAssetAtPath<SkillConfig>(
            "Assets/Resources/Skills/Mage_LightningStorm.asset"
        );

        // Slot 3: Teleport
        SkillConfig teleport = AssetDatabase.LoadAssetAtPath<SkillConfig>(
            "Assets/Resources/Skills/Mage_Teleport.asset"
        );

        // Slot 4: Meteor
        SkillConfig meteor = AssetDatabase.LoadAssetAtPath<SkillConfig>(
            "Assets/Resources/Skills/Mage_Meteor.asset"
        );

        int skillCount = 0;

        if (fireball != null)
        {
            skillExecutor.equippedSkills.Add(fireball);
            Debug.Log("✅ Slot 0: Fireball");
            skillCount++;
        }
        else
        {
            Debug.LogWarning("⚠️ Mage_Fireball не найден");
        }

        if (iceNova != null)
        {
            skillExecutor.equippedSkills.Add(iceNova);
            Debug.Log("✅ Slot 1: Ice Nova");
            skillCount++;
        }
        else
        {
            Debug.LogWarning("⚠️ Mage_IceNova не найден");
        }

        if (lightningStorm != null)
        {
            skillExecutor.equippedSkills.Add(lightningStorm);
            Debug.Log("✅ Slot 2: Lightning Storm");
            skillCount++;
        }
        else
        {
            Debug.LogWarning("⚠️ Mage_LightningStorm не найден");
        }

        if (teleport != null)
        {
            skillExecutor.equippedSkills.Add(teleport);
            Debug.Log("✅ Slot 3: Teleport");
            skillCount++;
        }
        else
        {
            Debug.LogWarning("⚠️ Mage_Teleport не найден");
        }

        if (meteor != null)
        {
            skillExecutor.equippedSkills.Add(meteor);
            Debug.Log("✅ Slot 4: Meteor");
            skillCount++;
        }
        else
        {
            Debug.LogWarning("⚠️ Mage_Meteor не найден");
        }

        // ═══════════════════════════════════════════════════════════
        // КАМЕРА
        // ═══════════════════════════════════════════════════════════

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = testPlayer.transform.position + new Vector3(0, 10, -10);
            mainCam.transform.LookAt(testPlayer.transform.position + Vector3.up * 2);
        }

        // ═══════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ
        // ═══════════════════════════════════════════════════════════

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("✅ TestPlayer создан!");
        Debug.Log($"📍 Позиция: {testPlayer.transform.position}");
        Debug.Log($"🧠 Intelligence: 9");
        Debug.Log($"🔮 Wisdom: 8");
        Debug.Log($"⚡ Экипировано скиллов: {skillCount}/5");
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("🎮 УПРАВЛЕНИЕ:");
        Debug.Log("WASD - движение");
        Debug.Log("ЛКМ - выбрать цель");
        Debug.Log("1 - Fireball (требует цель)");
        Debug.Log("2 - Ice Nova (AOE вокруг себя)");
        Debug.Log("3 - Lightning Storm (AOE + Chain Lightning)");
        Debug.Log("4 - Teleport (телепорт вперёд на 5м)");
        Debug.Log("5 - Meteor (ground target, cast 2 сек)");
        Debug.Log("H - помощь");
        Debug.Log("═══════════════════════════════════════════════════════");

        Selection.activeGameObject = testPlayer;
    }
}
