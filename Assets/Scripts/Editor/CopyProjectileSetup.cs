using UnityEngine;
using UnityEditor;

/// <summary>
/// Копирует все компоненты и настройки с CelestialBallProjectile на Fireball
/// </summary>
public class CopyProjectileSetup : EditorWindow
{
    [MenuItem("Tools/Skills/Copy CelestialBall Setup to Fireball")]
    public static void CopySetup()
    {
        // Загружаем оба префаба
        string celestialPath = "Assets/Prefabs/Projectiles/CelestialBallProjectile.prefab";
        string fireballPath = "Assets/Prefabs/Projectiles/Fireball.prefab";

        GameObject celestialPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(celestialPath);
        GameObject fireballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fireballPath);

        if (celestialPrefab == null)
        {
            Debug.LogError($"❌ CelestialBallProjectile не найден: {celestialPath}");
            return;
        }

        if (fireballPrefab == null)
        {
            Debug.LogError($"❌ Fireball не найден: {fireballPath}");
            return;
        }

        Debug.Log("🔥 Копирование настроек с CelestialBall на Fireball...");

        // Создаём временный экземпляр Fireball для редактирования
        GameObject fireballInstance = PrefabUtility.InstantiatePrefab(fireballPrefab) as GameObject;

        // ════════════════════════════════════════════════════════════
        // 1. КОПИРУЕМ CELESTIALPROJECTILE
        // ════════════════════════════════════════════════════════════
        CelestialProjectile celestialScript = celestialPrefab.GetComponent<CelestialProjectile>();
        CelestialProjectile fireballScript = fireballInstance.GetComponent<CelestialProjectile>();

        if (celestialScript != null)
        {
            if (fireballScript == null)
            {
                fireballScript = fireballInstance.AddComponent<CelestialProjectile>();
                Debug.Log("✅ Добавлен CelestialProjectile");
            }

            // Копируем все публичные поля через SerializedObject
            SerializedObject celestialSO = new SerializedObject(celestialScript);
            SerializedObject fireballSO = new SerializedObject(fireballScript);

            SerializedProperty celestialProp = celestialSO.GetIterator();
            while (celestialProp.NextVisible(true))
            {
                SerializedProperty fireballProp = fireballSO.FindProperty(celestialProp.propertyPath);
                if (fireballProp != null && celestialProp.propertyPath != "m_Script")
                {
                    fireballSO.CopyFromSerializedProperty(celestialProp);
                }
            }
            fireballSO.ApplyModifiedProperties();
            Debug.Log("✅ Скопированы настройки CelestialProjectile");
        }

        // ════════════════════════════════════════════════════════════
        // 2. КОПИРУЕМ RIGIDBODY
        // ════════════════════════════════════════════════════════════
        Rigidbody celestialRb = celestialPrefab.GetComponent<Rigidbody>();
        Rigidbody fireballRb = fireballInstance.GetComponent<Rigidbody>();

        if (celestialRb != null)
        {
            if (fireballRb == null)
            {
                fireballRb = fireballInstance.AddComponent<Rigidbody>();
                Debug.Log("✅ Добавлен Rigidbody");
            }

            fireballRb.mass = celestialRb.mass;
            fireballRb.linearDamping = celestialRb.linearDamping;
            fireballRb.angularDamping = celestialRb.angularDamping;
            fireballRb.useGravity = celestialRb.useGravity;
            fireballRb.isKinematic = celestialRb.isKinematic;
            fireballRb.interpolation = celestialRb.interpolation;
            fireballRb.collisionDetectionMode = celestialRb.collisionDetectionMode;
            fireballRb.constraints = celestialRb.constraints;

            Debug.Log("✅ Скопированы настройки Rigidbody");
        }

        // ════════════════════════════════════════════════════════════
        // 3. КОПИРУЕМ COLLIDER
        // ════════════════════════════════════════════════════════════
        SphereCollider celestialCollider = celestialPrefab.GetComponent<SphereCollider>();
        SphereCollider fireballCollider = fireballInstance.GetComponent<SphereCollider>();

        if (celestialCollider != null)
        {
            if (fireballCollider == null)
            {
                fireballCollider = fireballInstance.AddComponent<SphereCollider>();
                Debug.Log("✅ Добавлен SphereCollider");
            }

            fireballCollider.isTrigger = celestialCollider.isTrigger;
            fireballCollider.radius = celestialCollider.radius;
            fireballCollider.center = celestialCollider.center;

            Debug.Log("✅ Скопированы настройки SphereCollider");
        }

        // ════════════════════════════════════════════════════════════
        // 4. КОПИРУЕМ LAYER
        // ════════════════════════════════════════════════════════════
        fireballInstance.layer = celestialPrefab.layer;
        Debug.Log($"✅ Установлен Layer: {LayerMask.LayerToName(fireballInstance.layer)} ({fireballInstance.layer})");

        // ════════════════════════════════════════════════════════════
        // 5. КОПИРУЕМ ДОЧЕРНИЕ ОБЪЕКТЫ (Trail, Light, Particles)
        // ════════════════════════════════════════════════════════════

        // Trail Renderer
        TrailRenderer celestialTrail = celestialPrefab.GetComponent<TrailRenderer>();
        TrailRenderer fireballTrail = fireballInstance.GetComponent<TrailRenderer>();

        if (celestialTrail != null)
        {
            if (fireballTrail == null)
            {
                fireballTrail = fireballInstance.AddComponent<TrailRenderer>();
                Debug.Log("✅ Добавлен TrailRenderer");
            }

            // Копируем настройки trail
            EditorUtility.CopySerialized(celestialTrail, fireballTrail);
            Debug.Log("✅ Скопированы настройки TrailRenderer");
        }

        // Light (ищем в дочерних объектах)
        Light celestialLight = celestialPrefab.GetComponentInChildren<Light>();
        Light fireballLight = fireballInstance.GetComponentInChildren<Light>();

        if (celestialLight != null)
        {
            if (fireballLight == null)
            {
                // Создаём дочерний объект для света
                GameObject lightObj = new GameObject("Light");
                lightObj.transform.SetParent(fireballInstance.transform);
                lightObj.transform.localPosition = celestialLight.transform.localPosition;
                lightObj.transform.localRotation = celestialLight.transform.localRotation;
                fireballLight = lightObj.AddComponent<Light>();
                Debug.Log("✅ Добавлен Light");
            }

            // Копируем настройки света
            EditorUtility.CopySerialized(celestialLight, fireballLight);
            Debug.Log("✅ Скопированы настройки Light");
        }

        // Particle System (ищем в дочерних объектах)
        ParticleSystem celestialParticles = celestialPrefab.GetComponentInChildren<ParticleSystem>();
        ParticleSystem fireballParticles = fireballInstance.GetComponentInChildren<ParticleSystem>();

        if (celestialParticles != null)
        {
            if (fireballParticles == null)
            {
                // Создаём дочерний объект для частиц
                GameObject particlesObj = new GameObject("Particles");
                particlesObj.transform.SetParent(fireballInstance.transform);
                particlesObj.transform.localPosition = celestialParticles.transform.localPosition;
                particlesObj.transform.localRotation = celestialParticles.transform.localRotation;
                fireballParticles = particlesObj.AddComponent<ParticleSystem>();
                Debug.Log("✅ Добавлена Particle System");
            }

            // Копируем настройки частиц
            EditorUtility.CopySerialized(celestialParticles, fireballParticles);

            // Копируем также ParticleSystemRenderer
            ParticleSystemRenderer celestialRenderer = celestialParticles.GetComponent<ParticleSystemRenderer>();
            ParticleSystemRenderer fireballRenderer = fireballParticles.GetComponent<ParticleSystemRenderer>();
            if (celestialRenderer != null && fireballRenderer != null)
            {
                EditorUtility.CopySerialized(celestialRenderer, fireballRenderer);
            }

            Debug.Log("✅ Скопированы настройки Particle System");
        }

        // ════════════════════════════════════════════════════════════
        // 6. СОХРАНЯЕМ ПРЕФАБ
        // ════════════════════════════════════════════════════════════
        PrefabUtility.SaveAsPrefabAsset(fireballInstance, fireballPath);
        DestroyImmediate(fireballInstance);

        Debug.Log("════════════════════════════════════════════════════════════");
        Debug.Log("✅ ВСЕ НАСТРОЙКИ СКОПИРОВАНЫ НА FIREBALL!");
        Debug.Log("════════════════════════════════════════════════════════════");
        Debug.Log("📦 Скопировано:");
        Debug.Log("  ✅ CelestialProjectile (скрипт полёта)");
        Debug.Log("  ✅ Rigidbody (физика)");
        Debug.Log("  ✅ SphereCollider (триггер)");
        Debug.Log("  ✅ TrailRenderer (хвост)");
        Debug.Log("  ✅ Light (свечение)");
        Debug.Log("  ✅ Particle System (частицы)");
        Debug.Log($"  ✅ Layer: {LayerMask.LayerToName(celestialPrefab.layer)}");
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
