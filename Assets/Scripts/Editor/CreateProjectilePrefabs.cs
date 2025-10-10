using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor скрипт для создания префабов снарядов
/// </summary>
public class CreateProjectilePrefabs : EditorWindow
{
    [MenuItem("Tools/Projectiles/Create All Projectile Prefabs")]
    public static void CreateAllPrefabs()
    {
        // Сначала создаем материалы
        CreateMaterials();

        // Затем создаем префабы
        CreateArrowPrefab();
        CreateFireballPrefab();
        CreateSoulShardsPrefab();

        Debug.Log("✅ Все префабы снарядов созданы!");
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Создать материалы для снарядов
    /// </summary>
    private static void CreateMaterials()
    {
        string materialPath = "Assets/Materials/Projectiles";

        // Создаем папки если их нет
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        if (!AssetDatabase.IsValidFolder(materialPath))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "Projectiles");
        }

        // 1. Материал стрелы (коричневый) - URP
        Material arrowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        arrowMat.SetColor("_BaseColor", new Color(0.7f, 0.45f, 0.2f, 1f));
        arrowMat.SetFloat("_Metallic", 0.1f);
        arrowMat.SetFloat("_Smoothness", 0.5f);
        AssetDatabase.CreateAsset(arrowMat, $"{materialPath}/ArrowMaterial.mat");

        // 2. Материал огненного шара (красный с эмиссией) - URP
        Material fireballMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        fireballMat.SetColor("_BaseColor", new Color(1f, 0.2f, 0f, 1f));
        fireballMat.SetFloat("_Metallic", 0.3f);
        fireballMat.SetFloat("_Smoothness", 0.9f);

        // Эмиссия для URP
        fireballMat.EnableKeyword("_EMISSION");
        fireballMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        fireballMat.SetColor("_EmissionColor", new Color(1f, 0.3f, 0f) * 2.5f);
        AssetDatabase.CreateAsset(fireballMat, $"{materialPath}/FireballMaterial.mat");

        // 3. Материал осколков души (зеленый с эмиссией) - URP с прозрачностью
        Material shardMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        shardMat.SetColor("_BaseColor", new Color(0.2f, 1f, 0.3f, 0.8f)); // Полупрозрачный

        // Прозрачность для URP
        shardMat.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
        shardMat.SetFloat("_Blend", 0); // 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply
        shardMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        shardMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        shardMat.SetInt("_ZWrite", 0);
        shardMat.renderQueue = 3000;
        shardMat.SetOverrideTag("RenderType", "Transparent");

        shardMat.SetFloat("_Metallic", 0.2f);
        shardMat.SetFloat("_Smoothness", 0.7f);

        // Эмиссия для URP
        shardMat.EnableKeyword("_EMISSION");
        shardMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        shardMat.SetColor("_EmissionColor", new Color(0.3f, 1f, 0.4f) * 2.0f);
        AssetDatabase.CreateAsset(shardMat, $"{materialPath}/SoulShardMaterial.mat");

        // 4. Материал для Trail (след)
        Material trailMat = new Material(Shader.Find("Sprites/Default"));
        AssetDatabase.CreateAsset(trailMat, $"{materialPath}/TrailMaterial.mat");

        AssetDatabase.SaveAssets();
        Debug.Log("✅ Материалы созданы в Assets/Materials/Projectiles/");
    }

    /// <summary>
    /// Создать префаб стрелы для Лучника
    /// </summary>
    [MenuItem("Tools/Projectiles/1. Create Arrow (Archer)")]
    public static void CreateArrowPrefab()
    {
        // Создаем GameObject
        GameObject arrow = new GameObject("ArrowProjectile");

        // Добавляем визуал - коричневый цилиндр (стрела)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "ArrowVisual";
        visual.transform.SetParent(arrow.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(90, 0, 0); // Поворачиваем горизонтально
        visual.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f); // Длинная тонкая стрела

        // Загружаем материал из файла
        Renderer renderer = visual.GetComponent<Renderer>();
        Material arrowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Projectiles/ArrowMaterial.mat");
        renderer.sharedMaterial = arrowMat;

        // Удаляем коллайдер с визуала
        DestroyImmediate(visual.GetComponent<Collider>());

        // Добавляем Rigidbody (kinematic)
        Rigidbody rb = arrow.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Добавляем сферический коллайдер для триггера
        SphereCollider collider = arrow.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.3f;

        // Добавляем Trail Renderer (след)
        TrailRenderer trail = arrow.AddComponent<TrailRenderer>();
        trail.time = 0.3f;
        trail.startWidth = 0.1f;
        trail.endWidth = 0.01f;
        Material trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Projectiles/TrailMaterial.mat");
        trail.sharedMaterial = trailMat;
        trail.startColor = new Color(0.8f, 0.6f, 0.3f, 1f);
        trail.endColor = new Color(0.8f, 0.6f, 0.3f, 0f);

        // Добавляем скрипт Projectile
        Projectile projectile = arrow.AddComponent<Projectile>();

        // Сохраняем как префаб
        string path = "Assets/Prefabs/Projectiles";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Projectiles");
        }

        PrefabUtility.SaveAsPrefabAsset(arrow, $"{path}/ArrowProjectile.prefab");
        DestroyImmediate(arrow);

        Debug.Log("✅ Стрела создана: Assets/Prefabs/Projectiles/ArrowProjectile.prefab");
    }

    /// <summary>
    /// Создать префаб огненного шара для Мага
    /// </summary>
    [MenuItem("Tools/Projectiles/2. Create Fireball (Mage)")]
    public static void CreateFireballPrefab()
    {
        // Создаем GameObject
        GameObject fireball = new GameObject("FireballProjectile");

        // Добавляем визуал - красная сфера
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "FireballVisual";
        visual.transform.SetParent(fireball.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        // Загружаем материал из файла
        Renderer fireballRenderer = visual.GetComponent<Renderer>();
        Material fireballMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Projectiles/FireballMaterial.mat");
        fireballRenderer.sharedMaterial = fireballMat;

        // Удаляем коллайдер с визуала
        DestroyImmediate(visual.GetComponent<Collider>());

        // Добавляем яркое свечение (Point Light)
        Light light = fireball.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.3f, 0f); // Оранжево-красный
        light.intensity = 4f; // Увеличили яркость
        light.range = 5f; // Увеличили радиус

        // Добавляем Rigidbody (kinematic)
        Rigidbody rb = fireball.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Добавляем сферический коллайдер для триггера
        SphereCollider collider = fireball.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.4f;

        // Добавляем Trail Renderer (огненный след)
        TrailRenderer trail = fireball.AddComponent<TrailRenderer>();
        trail.time = 0.5f;
        trail.startWidth = 0.3f;
        trail.endWidth = 0.05f;
        Material trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Projectiles/TrailMaterial.mat");
        trail.sharedMaterial = trailMat;
        trail.startColor = new Color(1f, 0.5f, 0f, 1f); // Оранжевый
        trail.endColor = new Color(1f, 0f, 0f, 0f); // Красный прозрачный

        // Добавляем скрипт Projectile
        Projectile projectile = fireball.AddComponent<Projectile>();

        // Сохраняем как префаб
        string path = "Assets/Prefabs/Projectiles";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Projectiles");
        }

        PrefabUtility.SaveAsPrefabAsset(fireball, $"{path}/FireballProjectile.prefab");
        DestroyImmediate(fireball);

        Debug.Log("✅ Огненный шар создан: Assets/Prefabs/Projectiles/FireballProjectile.prefab");
    }

    /// <summary>
    /// Создать префаб зеленых осколков души для Разбойника
    /// </summary>
    [MenuItem("Tools/Projectiles/3. Create Soul Shards (Rogue)")]
    public static void CreateSoulShardsPrefab()
    {
        // Создаем главный GameObject
        GameObject soulShards = new GameObject("SoulShardsProjectile");

        // Создаем контейнер для визуала (для вращения)
        GameObject visualContainer = new GameObject("ShardsVisual");
        visualContainer.transform.SetParent(soulShards.transform);
        visualContainer.transform.localPosition = Vector3.zero;

        // Создаем несколько зеленых осколков (5 штук)
        for (int i = 0; i < 5; i++)
        {
            GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name = $"Shard{i}";
            shard.transform.SetParent(visualContainer.transform); // Родитель = контейнер визуала

            // Случайное смещение
            float angle = i * 72f; // 360 / 5 = 72 градуса между осколками
            float radius = 0.3f;
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            float y = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            shard.transform.localPosition = new Vector3(x, y, 0);
            shard.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            shard.transform.localRotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));

            // Загружаем материал из файла
            Renderer shardRenderer = shard.GetComponent<Renderer>();
            Material shardMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Projectiles/SoulShardMaterial.mat");
            shardRenderer.sharedMaterial = shardMat;

            // Удаляем коллайдер с осколка
            DestroyImmediate(shard.GetComponent<Collider>());
        }

        // Добавляем Rigidbody (kinematic)
        Rigidbody rb = soulShards.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Добавляем сферический коллайдер для триггера
        SphereCollider collider = soulShards.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.5f;

        // Добавляем Trail Renderer (зеленый след)
        TrailRenderer trail = soulShards.AddComponent<TrailRenderer>();
        trail.time = 0.4f;
        trail.startWidth = 0.4f;
        trail.endWidth = 0.1f;
        Material trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Projectiles/TrailMaterial.mat");
        trail.sharedMaterial = trailMat;
        trail.startColor = new Color(0f, 1f, 0f, 0.8f); // Зеленый
        trail.endColor = new Color(0f, 0.5f, 0f, 0f); // Темно-зеленый прозрачный

        // Добавляем яркое зеленое свечение (Point Light)
        Light light = soulShards.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.3f, 1f, 0.4f); // Яркий зеленый
        light.intensity = 3f; // Увеличили яркость
        light.range = 4f; // Увеличили радиус

        // Добавляем скрипт Projectile (он сам будет вращать визуал)
        Projectile projectile = soulShards.AddComponent<Projectile>();

        // Сохраняем как префаб
        string path = "Assets/Prefabs/Projectiles";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Projectiles");
        }

        PrefabUtility.SaveAsPrefabAsset(soulShards, $"{path}/SoulShardsProjectile.prefab");
        DestroyImmediate(soulShards);

        Debug.Log("✅ Осколки души созданы: Assets/Prefabs/Projectiles/SoulShardsProjectile.prefab");
    }
}
