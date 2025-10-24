using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Настройка тестовой сцены для скиллов
/// </summary>
public class SetupSkillTestScene : EditorWindow
{
    [MenuItem("Tools/Skills/Setup Skill Test Scene")]
    public static void SetupScene()
    {
        // Открываем тестовую сцену
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SkillTestScene.unity");

        Debug.Log("🎮 Настройка тестовой сцены для скиллов...");

        // Ищем PlayerSpawnPoint
        GameObject spawnPoint = GameObject.Find("PlayerSpawnPoint");
        if (spawnPoint == null)
        {
            Debug.LogError("❌ PlayerSpawnPoint не найден в сцене!");
            return;
        }

        // Проверяем, есть ли уже TestPlayer
        GameObject existingPlayer = GameObject.Find("TestPlayer");
        if (existingPlayer != null)
        {
            Debug.Log("⚠️ TestPlayer уже существует, удаляю старый...");
            DestroyImmediate(existingPlayer);
        }

        // Создаём тестового игрока
        GameObject testPlayer = new GameObject("TestPlayer");
        testPlayer.transform.position = spawnPoint.transform.position;
        testPlayer.transform.rotation = spawnPoint.transform.rotation;
        testPlayer.tag = "Player";
        testPlayer.layer = LayerMask.NameToLayer("Default");

        // Добавляем необходимые компоненты
        CharacterController controller = testPlayer.AddComponent<CharacterController>();
        controller.center = new Vector3(0, 1, 0);
        controller.radius = 0.5f;
        controller.height = 2f;

        // CharacterStats (SPECIAL система)
        CharacterStats stats = testPlayer.AddComponent<CharacterStats>();
        // Устанавливаем базовые SPECIAL характеристики для мага
        stats.strength = 3;       // Низкая сила
        stats.perception = 5;     // Среднее восприятие
        stats.endurance = 4;      // Средняя выносливость
        stats.wisdom = 8;         // Высокая мудрость (MP и реген)
        stats.intelligence = 9;   // Очень высокий интеллект (маг. урон)
        stats.agility = 4;        // Средняя ловкость
        stats.luck = 5;           // Средняя удача

        // HealthSystem
        HealthSystem health = testPlayer.AddComponent<HealthSystem>();

        // ManaSystem
        ManaSystem mana = testPlayer.AddComponent<ManaSystem>();

        // Animator (нужен для анимаций каста)
        Animator animator = testPlayer.AddComponent<Animator>();
        // Пробуем загрузить animator controller мага (опционально)
        RuntimeAnimatorController animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Resources/Characters/MageAnimatorController.controller"
        );
        if (animController != null)
        {
            animator.runtimeAnimatorController = animController;
            Debug.Log("✅ Animator Controller загружен");
        }
        else
        {
            Debug.LogWarning("⚠️ MageAnimatorController не найден - анимации каста не будут показываться");
            Debug.LogWarning("   Это не критично - скиллы будут работать без анимаций");
        }

        // SkillExecutor (НОВАЯ СИСТЕМА)
        SkillExecutor skillExecutor = testPlayer.AddComponent<SkillExecutor>();

        // EffectManager (НОВАЯ СИСТЕМА)
        EffectManager effectManager = testPlayer.AddComponent<EffectManager>();

        // PlayerAttackNew (для интеграции)
        PlayerAttackNew attackNew = testPlayer.AddComponent<PlayerAttackNew>();

        // Простое управление камерой
        testPlayer.AddComponent<SimplePlayerController>();

        // Создаём визуал игрока (простой capsule)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        visual.transform.SetParent(testPlayer.transform);
        visual.transform.localPosition = new Vector3(0, 1, 0);
        visual.transform.localScale = Vector3.one;

        // Удаляем Collider у визуала (Collider уже есть на CharacterController)
        DestroyImmediate(visual.GetComponent<CapsuleCollider>());

        // Меняем цвет на синий (маг)
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = Color.blue;
        visual.GetComponent<Renderer>().material = mat;

        // Создаём точку спавна снарядов
        GameObject projectileSpawn = new GameObject("ProjectileSpawnPoint");
        projectileSpawn.transform.SetParent(testPlayer.transform);
        projectileSpawn.transform.localPosition = new Vector3(0, 1.5f, 0.5f);

        // Настраиваем камеру
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = testPlayer.transform.position + new Vector3(0, 10, -10);
            mainCam.transform.LookAt(testPlayer.transform.position + Vector3.up * 2);
        }

        // ═══════════════════════════════════════════════════════════
        // ЗАГРУЖАЕМ СКИЛЛЫ МАГА
        // ═══════════════════════════════════════════════════════════

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

        // Добавляем скиллы в equippedSkills
        skillExecutor.equippedSkills.Clear();

        if (fireball != null)
        {
            skillExecutor.equippedSkills.Add(fireball);
            Debug.Log("✅ Slot 0: Fireball");
        }
        else
        {
            Debug.LogWarning("⚠️ Mage_Fireball не найден");
        }

        if (iceNova != null)
        {
            skillExecutor.equippedSkills.Add(iceNova);
            Debug.Log("✅ Slot 1: Ice Nova");
        }
        else
        {
            Debug.LogWarning("⚠️ Mage_IceNova не найден");
        }

        if (lightningStorm != null)
        {
            skillExecutor.equippedSkills.Add(lightningStorm);
            Debug.Log("✅ Slot 2: Lightning Storm");
        }
        else
        {
            Debug.LogWarning("⚠️ Mage_LightningStorm не найден");
        }

        // Сохраняем сцену
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // Выделяем игрока
        Selection.activeGameObject = testPlayer;
        EditorGUIUtility.PingObject(testPlayer);

        Debug.Log("✅ Тестовая сцена настроена!");
        Debug.Log("📋 Следующие шаги:");
        Debug.Log("1. Создайте Mage_Fireball: Tools → Skills → Create Mage Fireball");
        Debug.Log("2. Добавьте Mage_Fireball в TestPlayer → SkillExecutor → Equipped Skills[0]");
        Debug.Log("3. Нажмите Play");
        Debug.Log("4. Управление: WASD - движение, ЛКМ - выбор врага, 1 - Fireball");
    }
}
