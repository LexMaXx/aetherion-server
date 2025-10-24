using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor скрипт для создания wrapper префабов из Projectile Factory снарядов
/// Добавляет Projectile.cs, Collider, Rigidbody к визуальным снарядам
/// </summary>
public class ProjectileFactoryWrapper : EditorWindow
{
    private GameObject sourceProjectilePrefab;
    private string wrapperName = "NewProjectile";
    private float colliderRadius = 0.5f;
    private float projectileSpeed = 20f;
    private float projectileLifetime = 5f;
    private bool enableHoming = false;

    [MenuItem("Tools/Projectile Factory Wrapper")]
    public static void ShowWindow()
    {
        GetWindow<ProjectileFactoryWrapper>("Projectile Wrapper");
    }

    private void OnGUI()
    {
        GUILayout.Label("Создание Wrapper Префаба", EditorStyles.boldLabel);
        GUILayout.Space(10);

        sourceProjectilePrefab = (GameObject)EditorGUILayout.ObjectField(
            "Projectile Factory Prefab",
            sourceProjectilePrefab,
            typeof(GameObject),
            false
        );

        GUILayout.Space(10);

        wrapperName = EditorGUILayout.TextField("Wrapper Name", wrapperName);
        colliderRadius = EditorGUILayout.FloatField("Collider Radius", colliderRadius);
        projectileSpeed = EditorGUILayout.FloatField("Speed", projectileSpeed);
        projectileLifetime = EditorGUILayout.FloatField("Lifetime", projectileLifetime);
        enableHoming = EditorGUILayout.Toggle("Enable Homing", enableHoming);

        GUILayout.Space(20);

        if (GUILayout.Button("Создать Wrapper Префаб", GUILayout.Height(40)))
        {
            CreateWrapperPrefab();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Этот инструмент создаёт wrapper префаб:\n" +
            "1. Родительский объект с Projectile.cs + Collider + Rigidbody\n" +
            "2. Child объект = красивый снаряд из Projectile Factory\n\n" +
            "Префаб сохраняется в Assets/Prefabs/Projectiles/",
            MessageType.Info
        );
    }

    private void CreateWrapperPrefab()
    {
        if (sourceProjectilePrefab == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Выбери Projectile Factory префаб!", "OK");
            return;
        }

        if (string.IsNullOrEmpty(wrapperName))
        {
            EditorUtility.DisplayDialog("Ошибка", "Введи имя для wrapper префаба!", "OK");
            return;
        }

        // Создаём пустой родительский объект
        GameObject wrapper = new GameObject(wrapperName);

        // Добавляем компонент Projectile
        Projectile projectile = wrapper.AddComponent<Projectile>();

        // Используем SerializedObject для установки private полей
        SerializedObject serializedProjectile = new SerializedObject(projectile);
        serializedProjectile.FindProperty("speed").floatValue = projectileSpeed;
        serializedProjectile.FindProperty("lifetime").floatValue = projectileLifetime;
        serializedProjectile.FindProperty("homing").boolValue = enableHoming;
        serializedProjectile.FindProperty("rotationSpeed").floatValue = 360f;
        serializedProjectile.ApplyModifiedProperties();

        // Добавляем SphereCollider (trigger)
        SphereCollider collider = wrapper.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = colliderRadius;

        // Добавляем Rigidbody (kinematic)
        Rigidbody rb = wrapper.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Добавляем визуальный снаряд как child
        GameObject visual = PrefabUtility.InstantiatePrefab(sourceProjectilePrefab) as GameObject;
        visual.transform.SetParent(wrapper.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        visual.name = "Visual";

        // КРИТИЧЕСКИ ВАЖНО: Удаляем все missing скрипты с visual и его children
        RemoveMissingScripts(visual);

        // Создаём директорию если не существует
        string folderPath = "Assets/Prefabs/Projectiles";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Сохраняем как префаб
        string prefabPath = $"{folderPath}/{wrapperName}.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);

        // Удаляем временный объект из сцены
        DestroyImmediate(wrapper);

        // Выделяем созданный префаб
        Selection.activeObject = savedPrefab;
        EditorGUIUtility.PingObject(savedPrefab);

        EditorUtility.DisplayDialog(
            "Успех!",
            $"Wrapper префаб создан:\n{prefabPath}",
            "OK"
        );

        Debug.Log($"[ProjectileFactoryWrapper] ✅ Создан wrapper префаб: {prefabPath}");
    }

    /// <summary>
    /// Удалить все missing скрипты с GameObject и всех его children (рекурсивно)
    /// </summary>
    private void RemoveMissingScripts(GameObject go)
    {
        // Получаем все компоненты
        Component[] components = go.GetComponents<Component>();

        // Удаляем missing компоненты
        SerializedObject so = new SerializedObject(go);
        SerializedProperty prop = so.FindProperty("m_Component");

        int removedCount = 0;
        for (int i = prop.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty componentProp = prop.GetArrayElementAtIndex(i);
            if (componentProp.objectReferenceValue == null)
            {
                prop.DeleteArrayElementAtIndex(i);
                removedCount++;
            }
        }

        so.ApplyModifiedProperties();

        if (removedCount > 0)
        {
            Debug.Log($"[ProjectileFactoryWrapper] Удалено {removedCount} missing скриптов с {go.name}");
        }

        // Рекурсивно обрабатываем всех children
        foreach (Transform child in go.transform)
        {
            RemoveMissingScripts(child.gameObject);
        }
    }
}
