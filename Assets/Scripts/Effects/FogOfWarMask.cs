using UnityEngine;

/// <summary>
/// Настоящий туман войны как в стратегиях (Starcraft, Warcraft)
/// Всё за пределами радиуса видимости покрыто черной маской
/// </summary>
public class FogOfWarMask : MonoBehaviour
{
    [Header("Visibility Settings")]
    [SerializeField] private float visibilityRadius = 60f;
    [SerializeField] private float transitionRange = 10f;

    [Header("Visual Settings")]
    [SerializeField] private Color fogColor = Color.black;
    [SerializeField] private float maskHeight = 200f; // Высота маски над землей
    [SerializeField] private float maskSize = 5000f; // Размер маски (должна покрывать всю сцену)

    private Transform player;
    private GameObject maskPlane;
    private Material maskMaterial;
    private Shader maskShader;

    void Start()
    {
        player = transform;

        // Загружаем shader
        maskShader = Shader.Find("Custom/FogOfWarMask");
        if (maskShader == null)
        {
            Debug.LogError("[FogOfWarMask] ❌ Shader 'Custom/FogOfWarMask' не найден!");
            enabled = false;
            return;
        }

        // Создаем материал
        maskMaterial = new Material(maskShader);
        maskMaterial.SetColor("_FogColor", fogColor);
        maskMaterial.SetFloat("_VisibilityRadius", visibilityRadius);
        maskMaterial.SetFloat("_TransitionRange", transitionRange);

        // Создаем plane (плоскость) которая покрывает всю сцену
        CreateMaskPlane();

        Debug.Log("[FogOfWarMask] ✅ Туман войны инициализирован");
    }

    void Update()
    {
        if (maskMaterial == null || player == null) return;

        // Обновляем позицию игрока в шейдере
        maskMaterial.SetVector("_PlayerPosition", new Vector4(player.position.x, 0, player.position.z, 0));

        // Маска следует за игроком
        if (maskPlane != null)
        {
            Vector3 maskPos = player.position;
            maskPos.y = maskHeight;
            maskPlane.transform.position = maskPos;
        }
    }

    /// <summary>
    /// Создать plane (плоскость) для маски тумана
    /// </summary>
    private void CreateMaskPlane()
    {
        // Создаем GameObject для маски
        maskPlane = new GameObject("FogOfWarMask_Plane");

        // Добавляем MeshFilter и MeshRenderer
        MeshFilter meshFilter = maskPlane.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = maskPlane.AddComponent<MeshRenderer>();

        // Создаем mesh для plane
        Mesh mesh = new Mesh();
        mesh.name = "FogOfWarMask_Mesh";

        // Вершины plane (огромный квадрат)
        float halfSize = maskSize * 0.5f;
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-halfSize, 0, -halfSize),
            new Vector3(halfSize, 0, -halfSize),
            new Vector3(-halfSize, 0, halfSize),
            new Vector3(halfSize, 0, halfSize)
        };

        // UV координаты
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        // Треугольники
        int[] triangles = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshRenderer.material = maskMaterial;

        // Настройки рендеринга
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        // Позиционируем над игроком и направляем вниз
        maskPlane.transform.position = new Vector3(player.position.x, maskHeight, player.position.z);
        maskPlane.transform.rotation = Quaternion.identity; // Без поворота, plane смотрит вверх

        // НЕ делаем parent чтобы маска не вращалась вместе с игроком
        maskPlane.transform.parent = null;

        // Layer для игнорирования raycast
        maskPlane.layer = 2; // Ignore Raycast

        Debug.Log($"[FogOfWarMask] Создана маска размером {maskSize}x{maskSize}");
    }

    /// <summary>
    /// Изменить радиус видимости
    /// </summary>
    public void SetVisibilityRadius(float radius)
    {
        visibilityRadius = radius;
        if (maskMaterial != null)
        {
            maskMaterial.SetFloat("_VisibilityRadius", visibilityRadius);
        }
    }

    /// <summary>
    /// Изменить диапазон перехода
    /// </summary>
    public void SetTransitionRange(float range)
    {
        transitionRange = range;
        if (maskMaterial != null)
        {
            maskMaterial.SetFloat("_TransitionRange", transitionRange);
        }
    }

    /// <summary>
    /// Изменить цвет тумана
    /// </summary>
    public void SetFogColor(Color color)
    {
        fogColor = color;
        if (maskMaterial != null)
        {
            maskMaterial.SetColor("_FogColor", fogColor);
        }
    }

    void OnDestroy()
    {
        // Уничтожаем созданные объекты
        if (maskPlane != null)
        {
            Destroy(maskPlane);
        }

        if (maskMaterial != null)
        {
            Destroy(maskMaterial);
        }
    }

    void OnDrawGizmos()
    {
        if (player == null) return;

        // Рисуем зеленый круг - радиус видимости
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        DrawCircle(player.position, visibilityRadius, 64);

        // Рисуем желтую границу перехода
        Gizmos.color = Color.yellow;
        DrawCircle(player.position, visibilityRadius + transitionRange, 64);
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}
