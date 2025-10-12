using UnityEngine;

/// <summary>
/// Туман войны используя Projector (работает с рельефом terrain)
/// </summary>
[RequireComponent(typeof(Projector))]
public class FogOfWarProjector : MonoBehaviour
{
    [Header("Visibility Settings")]
    [SerializeField] private float visibilityRadius = 60f;
    [SerializeField] private float projectorHeight = 100f; // Высота проектора над игроком

    private Transform player;
    private Projector projector;
    private Texture2D fogTexture;
    private Material projectorMaterial;

    void Start()
    {
        player = transform;

        // Создаем текстуру для тумана войны
        CreateFogTexture();

        // Настраиваем Projector
        SetupProjector();

        Debug.Log("[FogOfWarProjector] ✅ Projector туман войны инициализирован");
    }

    void Update()
    {
        if (projector == null || player == null) return;

        // Projector следует за игроком
        Vector3 projectorPos = player.position;
        projectorPos.y += projectorHeight;
        transform.position = projectorPos;

        // Направлен строго вниз
        transform.rotation = Quaternion.Euler(90, 0, 0);

        // Обновляем размер проекции (радиус видимости * 10)
        projector.orthographicSize = visibilityRadius * 5f;
    }

    /// <summary>
    /// Создать текстуру для тумана войны (радиальный градиент)
    /// </summary>
    private void CreateFogTexture()
    {
        int textureSize = 512;
        fogTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

        // Создаем радиальный градиент
        // Центр = прозрачный (видно)
        // Края = черный (туман)
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                // Вычисляем расстояние от центра (0-1)
                float dx = (x - textureSize * 0.5f) / (textureSize * 0.5f);
                float dy = (y - textureSize * 0.5f) / (textureSize * 0.5f);
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                // Вычисляем альфа канал
                // 0-0.2 = прозрачно (видно)
                // 0.2-0.3 = плавный переход
                // 0.3+ = черный (туман)
                float alpha;
                if (distance < 0.15f)
                {
                    alpha = 0f; // Полностью прозрачно
                }
                else if (distance < 0.25f)
                {
                    // Плавный переход
                    float t = (distance - 0.15f) / 0.1f;
                    alpha = Mathf.SmoothStep(0f, 1f, t);
                }
                else
                {
                    alpha = 1f; // Полностью черный
                }

                // Черный цвет с вычисленной прозрачностью
                fogTexture.SetPixel(x, y, new Color(0, 0, 0, alpha));
            }
        }

        fogTexture.Apply();
        fogTexture.wrapMode = TextureWrapMode.Clamp;

        Debug.Log("[FogOfWarProjector] Текстура тумана создана 512x512");
    }

    /// <summary>
    /// Настроить Projector компонент
    /// </summary>
    private void SetupProjector()
    {
        projector = GetComponent<Projector>();

        // Создаем материал для проектора
        Shader shader = Shader.Find("Projector/Multiply");
        if (shader == null)
        {
            shader = Shader.Find("Legacy Shaders/Projector/Multiply");
        }

        if (shader == null)
        {
            Debug.LogError("[FogOfWarProjector] ❌ Shader Projector/Multiply не найден!");
            enabled = false;
            return;
        }

        projectorMaterial = new Material(shader);
        projectorMaterial.SetTexture("_ShadowTex", fogTexture);
        projectorMaterial.SetColor("_Color", Color.white);

        // Настраиваем Projector
        projector.material = projectorMaterial;
        projector.orthographic = true;
        projector.orthographicSize = visibilityRadius * 5f;
        projector.nearClipPlane = 0.1f;
        projector.farClipPlane = projectorHeight + 50f;
        projector.ignoreLayers = 0; // Проецируем на все слои

        Debug.Log("[FogOfWarProjector] Projector настроен");
    }

    /// <summary>
    /// Изменить радиус видимости
    /// </summary>
    public void SetVisibilityRadius(float radius)
    {
        visibilityRadius = radius;
        if (projector != null)
        {
            projector.orthographicSize = visibilityRadius * 5f;
        }
    }

    void OnDestroy()
    {
        if (fogTexture != null)
        {
            Destroy(fogTexture);
        }

        if (projectorMaterial != null)
        {
            Destroy(projectorMaterial);
        }
    }

    void OnDrawGizmos()
    {
        if (player == null) return;

        // Рисуем зеленый круг - радиус видимости
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        DrawCircle(player.position, visibilityRadius, 32);

        // Рисуем область проекции
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        float projectionSize = visibilityRadius * 5f;
        DrawCircle(player.position, projectionSize, 32);
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
