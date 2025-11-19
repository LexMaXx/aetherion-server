using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Туман войны используя Canvas WorldSpace (работает с любым рендер пайплайном)
/// </summary>
public class FogOfWarCanvas : MonoBehaviour
{
    [Header("Visibility Settings")]
    [SerializeField] private float visibilityRadius = 60f;
    [SerializeField] private float canvasHeight = 0.5f; // Высота над terrain (почти на уровне)
    [SerializeField] private float canvasSize = 1000f; // Размер canvas

    [Header("Fog Appearance")]
    [SerializeField] [Range(0f, 1f)] private float fogAlpha = 1f; // Прозрачность тумана (0 = прозрачный, 1 = непрозрачный)

    private Transform player;
    private GameObject canvasObject;
    private RawImage fogImage;
    private Texture2D fogTexture;
    private Canvas canvas;
    private float fixedTerrainHeight = 0f; // Фиксированная высота terrain
    private float lastVisibilityRadius = -1f; // Последний радиус для оптимизации

    void Start()
    {
        player = transform;

        // Получаем фиксированную высоту terrain один раз
        fixedTerrainHeight = GetTerrainHeight(player.position);

        CreateFogCanvas();
        Debug.Log($"[FogOfWarCanvas] ✅ Canvas туман войны создан на высоте {fixedTerrainHeight + canvasHeight}м");
    }

    void Update()
    {
        if (canvasObject == null || player == null) return;

        // Canvas следует за игроком только по X и Z (высота фиксирована)
        Vector3 canvasPos = new Vector3(
            player.position.x,
            fixedTerrainHeight + canvasHeight, // Фиксированная высота
            player.position.z
        );
        canvasObject.transform.position = canvasPos;

        // ОПТИМИЗАЦИЯ: Обновляем текстуру только если радиус изменился
        // НЕ каждый кадр!
    }

    void LateUpdate()
    {
        // Обновляем прозрачность через RawImage (быстро, без пересоздания текстуры)
        if (fogImage != null)
        {
            Color imageColor = fogImage.color;
            imageColor.a = fogAlpha;
            fogImage.color = imageColor;
        }
    }

    /// <summary>
    /// Получить высоту terrain под позицией
    /// </summary>
    private float GetTerrainHeight(Vector3 position)
    {
        // Ищем Terrain
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            // Получаем высоту terrain в позиции игрока
            return terrain.SampleHeight(position);
        }

        // Если нет terrain, используем raycast
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out hit, 200f))
        {
            return hit.point.y;
        }

        return 0f;
    }

    /// <summary>
    /// Создать Canvas для тумана войны
    /// </summary>
    private void CreateFogCanvas()
    {
        // Создаем GameObject
        canvasObject = new GameObject("FogOfWarCanvas");

        // Добавляем Canvas
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Настраиваем RectTransform
        RectTransform rectTransform = canvasObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(canvasSize, canvasSize);
        rectTransform.localScale = Vector3.one;

        // Позиция на фиксированной высоте над terrain
        canvasObject.transform.position = new Vector3(
            player.position.x,
            fixedTerrainHeight + canvasHeight,
            player.position.z
        );
        canvasObject.transform.rotation = Quaternion.Euler(90, 0, 0); // Горизонтально

        // Добавляем CanvasScaler
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 1;

        // Создаем Image для тумана
        GameObject imageObj = new GameObject("FogImage");
        imageObj.transform.SetParent(canvasObject.transform, false);

        fogImage = imageObj.AddComponent<RawImage>();
        RectTransform imageRect = imageObj.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.sizeDelta = Vector2.zero;
        imageRect.anchoredPosition = Vector2.zero;

        // Создаем текстуру тумана
        CreateFogTexture();
        fogImage.texture = fogTexture;

        // Отключаем raycast блокирование
        fogImage.raycastTarget = false;

        // Layer
        canvasObject.layer = 2; // Ignore Raycast

        Debug.Log($"[FogOfWarCanvas] Canvas создан {canvasSize}x{canvasSize}");
    }

    /// <summary>
    /// Создать текстуру тумана
    /// </summary>
    private void CreateFogTexture()
    {
        int textureSize = 512;
        fogTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        fogTexture.wrapMode = TextureWrapMode.Clamp;
        fogTexture.filterMode = FilterMode.Bilinear;

        UpdateFogTexture();
    }

    /// <summary>
    /// Обновить текстуру тумана (пересоздать градиент)
    /// ОПТИМИЗАЦИЯ: Вызывается только при изменении радиуса!
    /// </summary>
    private void UpdateFogTexture()
    {
        if (fogTexture == null) return;

        // ОПТИМИЗАЦИЯ: Не пересоздаем если радиус не изменился
        if (Mathf.Approximately(lastVisibilityRadius, visibilityRadius))
            return;

        lastVisibilityRadius = visibilityRadius;

        int textureSize = fogTexture.width;
        float centerX = textureSize * 0.5f;
        float centerY = textureSize * 0.5f;

        // Радиус в пикселях (зависит от visibilityRadius и canvasSize)
        float radiusInPixels = (visibilityRadius / canvasSize) * textureSize;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                // Расстояние от центра
                float dx = x - centerX;
                float dy = y - centerY;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                // Вычисляем альфа канал
                float alpha;
                float innerRadius = radiusInPixels * 0.8f; // Внутренний радиус (полностью прозрачно)
                float outerRadius = radiusInPixels * 1.2f; // Внешний радиус (начало тумана)

                if (distance < innerRadius)
                {
                    alpha = 0f; // Полностью прозрачно - видно
                }
                else if (distance < outerRadius)
                {
                    // Плавный переход
                    float t = (distance - innerRadius) / (outerRadius - innerRadius);
                    alpha = Mathf.SmoothStep(0f, 1f, t);
                }
                else
                {
                    alpha = 1f; // Полностью черный - туман
                }

                // Черный цвет с вычисленной прозрачностью
                // Прозрачность контролируется через RawImage.color.a
                fogTexture.SetPixel(x, y, new Color(0, 0, 0, alpha));
            }
        }

        fogTexture.Apply();
        Debug.Log($"[FogOfWarCanvas] Текстура обновлена для радиуса {visibilityRadius}м");
    }

    /// <summary>
    /// Изменить радиус видимости
    /// </summary>
    public void SetVisibilityRadius(float radius)
    {
        visibilityRadius = radius;
        UpdateFogTexture();
    }

    /// <summary>
    /// Изменить прозрачность тумана
    /// </summary>
    public void SetFogAlpha(float alpha)
    {
        fogAlpha = Mathf.Clamp01(alpha);
        // Прозрачность обновляется в LateUpdate() автоматически
    }

    void OnDestroy()
    {
        if (canvasObject != null)
        {
            Destroy(canvasObject);
        }

        if (fogTexture != null)
        {
            Destroy(fogTexture);
        }
    }

    void OnDrawGizmos()
    {
        if (player == null) return;

        // Рисуем зеленый круг - радиус видимости
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        DrawCircle(player.position, visibilityRadius, 32);
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
