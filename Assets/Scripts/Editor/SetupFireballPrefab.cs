using UnityEngine;
using UnityEditor;

/// <summary>
/// Настройка Fireball префаба с эффектами (свечение, хвост, trail)
/// </summary>
public class SetupFireballPrefab : EditorWindow
{
    [MenuItem("Tools/Skills/Setup Fireball Prefab Effects")]
    public static void SetupFireball()
    {
        // Загружаем Fireball префаб
        string fireballPath = "Assets/Prefabs/Projectiles/FireballProjectile.prefab";
        GameObject fireballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fireballPath);

        if (fireballPrefab == null)
        {
            Debug.LogError($"❌ Fireball префаб не найден по пути: {fireballPath}");
            Debug.LogError("Проверьте, что файл существует в Assets/Prefabs/Projectiles/");
            return;
        }

        // Создаём временный экземпляр для редактирования
        GameObject tempInstance = PrefabUtility.InstantiatePrefab(fireballPrefab) as GameObject;

        if (tempInstance == null)
        {
            Debug.LogError("❌ Не удалось создать временный экземпляр префаба!");
            return;
        }

        Debug.Log("🔥 Настройка Fireball префаба...");

        // ════════════════════════════════════════════════════════════
        // 1. ПРОВЕРЯЕМ/ДОБАВЛЯЕМ КОМПОНЕНТЫ
        // ════════════════════════════════════════════════════════════

        // CelestialProjectile компонент
        CelestialProjectile projectile = tempInstance.GetComponent<CelestialProjectile>();
        if (projectile == null)
        {
            projectile = tempInstance.AddComponent<CelestialProjectile>();
            Debug.Log("✅ Добавлен CelestialProjectile");
        }

        // Rigidbody (для физики)
        Rigidbody rb = tempInstance.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = tempInstance.AddComponent<Rigidbody>();
            Debug.Log("✅ Добавлен Rigidbody");
        }
        rb.useGravity = false;
        rb.isKinematic = true;

        // SphereCollider (для триггера попадания)
        SphereCollider collider = tempInstance.GetComponent<SphereCollider>();
        if (collider == null)
        {
            collider = tempInstance.AddComponent<SphereCollider>();
            Debug.Log("✅ Добавлен SphereCollider");
        }
        collider.isTrigger = true;
        collider.radius = 0.5f;

        // ════════════════════════════════════════════════════════════
        // 2. НАСТРАИВАЕМ LAYER
        // ════════════════════════════════════════════════════════════
        tempInstance.layer = LayerMask.NameToLayer("Projectile"); // Layer 7
        if (tempInstance.layer == 0)
        {
            Debug.LogWarning("⚠️ Layer 'Projectile' не найден! Используется Default (0)");
            Debug.LogWarning("Создайте Layer 7 = Projectile в Project Settings → Tags and Layers");
        }

        // ════════════════════════════════════════════════════════════
        // 3. ДОБАВЛЯЕМ TRAIL RENDERER (ХВОСТ)
        // ════════════════════════════════════════════════════════════
        TrailRenderer trail = tempInstance.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = tempInstance.AddComponent<TrailRenderer>();
            Debug.Log("✅ Добавлен TrailRenderer (хвост)");
        }

        // Настройка Trail Renderer
        trail.time = 0.5f; // Длительность следа
        trail.startWidth = 0.5f;
        trail.endWidth = 0.1f;
        trail.minVertexDistance = 0.1f;

        // Материал для trail (огненный градиент)
        Material trailMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

        // Создаём градиент для trail (оранжево-красный)
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.5f, 0f, 1f), 0.0f), // Ярко-оранжевый
                new GradientColorKey(new Color(1f, 0.2f, 0f, 1f), 0.5f), // Оранжево-красный
                new GradientColorKey(new Color(0.5f, 0f, 0f, 1f), 1.0f)  // Темно-красный
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f), // Полная непрозрачность в начале
                new GradientAlphaKey(0.0f, 1.0f)  // Полная прозрачность в конце
            }
        );
        trail.colorGradient = gradient;
        trail.material = trailMaterial;

        // ════════════════════════════════════════════════════════════
        // 4. ДОБАВЛЯЕМ LIGHT (СВЕЧЕНИЕ)
        // ════════════════════════════════════════════════════════════
        Light pointLight = tempInstance.GetComponentInChildren<Light>();
        if (pointLight == null)
        {
            GameObject lightObj = new GameObject("Light");
            lightObj.transform.SetParent(tempInstance.transform);
            lightObj.transform.localPosition = Vector3.zero;

            pointLight = lightObj.AddComponent<Light>();
            Debug.Log("✅ Добавлен Point Light (свечение)");
        }

        // Настройка света
        pointLight.type = LightType.Point;
        pointLight.color = new Color(1f, 0.4f, 0f); // Оранжевый цвет
        pointLight.intensity = 2f;
        pointLight.range = 5f;
        pointLight.shadows = LightShadows.None;

        // ════════════════════════════════════════════════════════════
        // 5. ДОБАВЛЯЕМ PARTICLE SYSTEM (ОГНЕННЫЕ ЧАСТИЦЫ)
        // ════════════════════════════════════════════════════════════
        ParticleSystem particles = tempInstance.GetComponentInChildren<ParticleSystem>();
        if (particles == null)
        {
            GameObject particlesObj = new GameObject("Particles");
            particlesObj.transform.SetParent(tempInstance.transform);
            particlesObj.transform.localPosition = Vector3.zero;

            particles = particlesObj.AddComponent<ParticleSystem>();
            Debug.Log("✅ Добавлена Particle System (огненные частицы)");
        }

        // Настройка Particle System
        var main = particles.main;
        main.duration = 5.0f;
        main.loop = true;
        main.startLifetime = 0.5f;
        main.startSpeed = 1f;
        main.startSize = 0.2f;
        main.startColor = new Color(1f, 0.5f, 0f, 1f); // Оранжевый
        main.maxParticles = 50;

        var emission = particles.emission;
        emission.rateOverTime = 20;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        // Цвет частиц меняется со временем (от оранжевого к красному)
        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient particleGradient = new Gradient();
        particleGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.7f, 0f), 0.0f), // Ярко-оранжевый
                new GradientColorKey(new Color(1f, 0.3f, 0f), 0.5f), // Оранжево-красный
                new GradientColorKey(new Color(0.8f, 0f, 0f), 1.0f)  // Красный
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(particleGradient);

        // Размер уменьшается со временем
        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 1.0f);
        sizeCurve.AddKey(1.0f, 0.0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Материал для частиц
        ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer != null)
        {
            Material particleMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            particleMaterial.SetColor("_BaseColor", new Color(1f, 0.5f, 0f, 1f));
            particleRenderer.material = particleMaterial;
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        // ════════════════════════════════════════════════════════════
        // 6. СОХРАНЯЕМ ИЗМЕНЕНИЯ
        // ════════════════════════════════════════════════════════════
        PrefabUtility.SaveAsPrefabAsset(tempInstance, fireballPath);
        DestroyImmediate(tempInstance);

        Debug.Log("════════════════════════════════════════════════════════════");
        Debug.Log("✅ Fireball префаб успешно настроен!");
        Debug.Log("════════════════════════════════════════════════════════════");
        Debug.Log("📦 Добавлено:");
        Debug.Log("  ✅ CelestialProjectile (скрипт снаряда)");
        Debug.Log("  ✅ Rigidbody + SphereCollider (физика)");
        Debug.Log("  ✅ TrailRenderer (огненный хвост)");
        Debug.Log("  ✅ Point Light (оранжевое свечение)");
        Debug.Log("  ✅ Particle System (огненные частицы)");
        Debug.Log("  ✅ Layer = Projectile (7)");
        Debug.Log("════════════════════════════════════════════════════════════");
        Debug.Log("📋 Следующий шаг:");
        Debug.Log("  1. Обновите Mage_Fireball.asset:");
        Debug.Log("     Projectile Prefab → FireballProjectile");
        Debug.Log("  2. Протестируйте в SkillTestScene");
        Debug.Log("════════════════════════════════════════════════════════════");

        // Выделяем префаб
        Selection.activeObject = fireballPrefab;
        EditorGUIUtility.PingObject(fireballPrefab);
    }
}
