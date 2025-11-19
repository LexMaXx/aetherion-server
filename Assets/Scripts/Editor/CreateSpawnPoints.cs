using UnityEngine;
using UnityEditor;

/// <summary>
/// Редакторский инструмент для автоматического создания 20 точек спавна в BattleScene
/// </summary>
public class CreateSpawnPoints : EditorWindow
{
    private int spawnPointCount = 20;
    private float radius = 10f;
    private float heightY = 0f;
    private string parentName = "SpawnPoints";

    [MenuItem("Tools/Aetherion/Create Spawn Points")]
    static void ShowWindow()
    {
        GetWindow<CreateSpawnPoints>("Create Spawn Points");
    }

    void OnGUI()
    {
        GUILayout.Label("Spawn Points Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        spawnPointCount = EditorGUILayout.IntField("Количество точек:", spawnPointCount);
        radius = EditorGUILayout.FloatField("Радиус круга (м):", radius);
        heightY = EditorGUILayout.FloatField("Высота Y:", heightY);
        parentName = EditorGUILayout.TextField("Имя родителя:", parentName);

        GUILayout.Space(10);

        if (GUILayout.Button("✨ Создать точки спавна", GUILayout.Height(40)))
        {
            CreatePoints();
        }

        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            $"Будет создано {spawnPointCount} точек по кругу радиусом {radius}м.\n" +
            $"Точки будут расположены равномерно на высоте Y={heightY}.",
            MessageType.Info
        );

        GUILayout.Space(10);

        if (GUILayout.Button("Удалить существующие SpawnPoints", GUILayout.Height(30)))
        {
            DeleteExistingPoints();
        }
    }

    void CreatePoints()
    {
        // Проверяем есть ли уже родительский объект
        GameObject existingParent = GameObject.Find(parentName);
        if (existingParent != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Объект уже существует",
                $"Объект '{parentName}' уже существует. Удалить и создать заново?",
                "Да, пересоздать",
                "Отмена"
            );

            if (overwrite)
            {
                DestroyImmediate(existingParent);
            }
            else
            {
                return;
            }
        }

        // Создаём родительский объект
        GameObject parent = new GameObject(parentName);
        parent.transform.position = new Vector3(0, heightY, 0);

        // Создаём точки спавна по кругу
        for (int i = 0; i < spawnPointCount; i++)
        {
            // Вычисляем угол для равномерного распределения
            float angle = (360f / spawnPointCount) * i * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            // Создаём точку спавна
            GameObject spawnPoint = new GameObject($"SpawnPoint_{i:D2}");
            spawnPoint.transform.parent = parent.transform;
            spawnPoint.transform.position = new Vector3(x, heightY, z);

            // Поворачиваем точку к центру (опционально)
            spawnPoint.transform.LookAt(parent.transform.position);
            spawnPoint.transform.Rotate(0, 180, 0); // Персонаж будет смотреть от центра

            // Добавляем визуальный маркер (Gizmo)
            var gizmo = spawnPoint.AddComponent<SpawnPointGizmo>();
            gizmo.gizmoColor = Color.green;
        }

        // Выделяем созданный объект
        Selection.activeGameObject = parent;

        Debug.Log($"✅ Создано {spawnPointCount} точек спавна в объекте '{parentName}'!");
        EditorUtility.DisplayDialog(
            "Успех!",
            $"✅ Создано {spawnPointCount} точек спавна!\n\n" +
            $"Объект '{parentName}' выделен в Hierarchy.\n" +
            $"Теперь назначьте эти точки в:\n" +
            $"1. BattleSceneManager → Spawn Points\n" +
            $"2. NetworkSyncManager.prefab → Spawn Points",
            "OK"
        );
    }

    void DeleteExistingPoints()
    {
        GameObject existingParent = GameObject.Find(parentName);
        if (existingParent != null)
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Подтверждение удаления",
                $"Удалить объект '{parentName}' и все его дочерние точки спавна?",
                "Да, удалить",
                "Отмена"
            );

            if (confirm)
            {
                DestroyImmediate(existingParent);
                Debug.Log($"🗑️ Удалён объект '{parentName}'");
            }
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Не найдено",
                $"Объект '{parentName}' не найден в сцене.",
                "OK"
            );
        }
    }
}

/// <summary>
/// Компонент для визуализации точек спавна в редакторе
/// </summary>
public class SpawnPointGizmo : MonoBehaviour
{
    public Color gizmoColor = Color.green;
    public float gizmoRadius = 0.5f;

    void OnDrawGizmos()
    {
        // Рисуем сферу в позиции точки спавна
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);

        // Рисуем стрелку направления
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);

        // Рисуем номер точки спавна
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1f,
            gameObject.name,
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.white },
                fontSize = 12,
                fontStyle = FontStyle.Bold
            }
        );
        #endif
    }

    void OnDrawGizmosSelected()
    {
        // Когда выбрана - рисуем больше деталей
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, gizmoRadius);

        // Рисуем линию к центру родителя
        if (transform.parent != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.parent.position);
        }
    }
}
