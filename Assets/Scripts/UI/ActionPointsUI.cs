using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI для отображения очков действия в виде шариков
/// </summary>
public class ActionPointsUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private GameObject actionPointPrefab; // Prefab шарика
    [SerializeField] private Transform pointsContainer; // Контейнер для шариков
    [SerializeField] private float pointSpacing = 40f; // Расстояние между шариками

    [Header("Visual Settings")]
    [SerializeField] private Color activeColor = new Color(1f, 0.8f, 0.2f, 1f); // Золотой
    [SerializeField] private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.3f); // Серый полупрозрачный
    [SerializeField] private float pointSize = 30f; // Размер шарика

    private List<Image> actionPointImages = new List<Image>();
    private ActionPointsSystem apSystem;
    private int maxPoints = 10;

    /// <summary>
    /// Инициализация UI
    /// </summary>
    public void Initialize(ActionPointsSystem system)
    {
        apSystem = system;
        maxPoints = apSystem.GetMaxPoints();

        // Создаем контейнер если его нет
        if (pointsContainer == null)
        {
            CreateContainer();
        }

        // Подписываемся на события изменения очков
        apSystem.OnActionPointsChanged += UpdateUI;

        // Создаем визуальные шарики
        CreateActionPointVisuals();

        Debug.Log($"[ActionPointsUI] UI инициализирован для {maxPoints} очков");
    }

    /// <summary>
    /// Создать контейнер для шариков если его нет
    /// </summary>
    private void CreateContainer()
    {
        // Находим или создаём Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("ActionPoints_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Как у PlayerHUD

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Создаем контейнер внизу по центру
        GameObject containerObj = new GameObject("ActionPoints_Container");
        containerObj.transform.SetParent(canvas.transform, false);
        pointsContainer = containerObj.transform;

        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0);
        containerRect.anchorMax = new Vector2(0.5f, 0);
        containerRect.pivot = new Vector2(0.5f, 0);
        containerRect.anchoredPosition = new Vector2(0, 50); // 50px от низа
        containerRect.sizeDelta = new Vector2(500, 50);

        Debug.Log("[ActionPointsUI] ✅ Контейнер создан программно");
    }

    /// <summary>
    /// Создает визуальные элементы (шарики) для всех очков
    /// </summary>
    private void CreateActionPointVisuals()
    {
        // Очищаем старые если есть
        if (pointsContainer != null)
        {
            // Используем обратный цикл чтобы избежать MissingReferenceException
            for (int i = pointsContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = pointsContainer.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        actionPointImages.Clear();

        // Создаем шарики
        for (int i = 0; i < maxPoints; i++)
        {
            GameObject pointObj = CreateActionPoint(i);
            Image pointImage = pointObj.GetComponent<Image>();
            actionPointImages.Add(pointImage);
        }

        Debug.Log($"[ActionPointsUI] Создано {actionPointImages.Count} шариков");
    }

    /// <summary>
    /// Создает один шарик
    /// </summary>
    private GameObject CreateActionPoint(int index)
    {
        GameObject pointObj;

        if (actionPointPrefab != null)
        {
            // Используем prefab если назначен
            pointObj = Instantiate(actionPointPrefab, pointsContainer);
        }
        else
        {
            // Создаем программно
            pointObj = new GameObject($"ActionPoint_{index}");
            pointObj.transform.SetParent(pointsContainer);

            // Добавляем Image компонент
            Image img = pointObj.AddComponent<Image>();
            img.sprite = CreateCircleSprite(); // Создаем круглый спрайт
            img.color = activeColor;

            // Устанавливаем размер
            RectTransform rt = pointObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(pointSize, pointSize);
        }

        // Позиционирование
        RectTransform rectTransform = pointObj.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(index * pointSpacing, 0);

        return pointObj;
    }

    /// <summary>
    /// Создает круглый спрайт для шарика
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        // Создаем текстуру с кругом
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);

                // Анти-алиасинг для гладких краев
                if (distance < radius - 1)
                {
                    pixels[y * size + x] = Color.white;
                }
                else if (distance < radius)
                {
                    float alpha = radius - distance;
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Обновление UI при изменении очков
    /// </summary>
    private void UpdateUI(int currentPoints, int newMaxPoints)
    {
        // Если maxPoints изменился - пересоздаём шарики
        if (newMaxPoints != this.maxPoints)
        {
            Debug.Log($"[ActionPointsUI] MaxPoints изменился: {this.maxPoints} → {newMaxPoints}. Пересоздание UI...");
            this.maxPoints = newMaxPoints;
            CreateActionPointVisuals();
        }

        // Обновляем цвета шариков
        for (int i = 0; i < actionPointImages.Count; i++)
        {
            if (i < currentPoints)
            {
                // Активный шарик - яркий
                actionPointImages[i].color = activeColor;
            }
            else
            {
                // Неактивный шарик - серый полупрозрачный
                actionPointImages[i].color = inactiveColor;
            }
        }

        Debug.Log($"[ActionPointsUI] Обновлено: {currentPoints}/{maxPoints} активных шариков");
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (apSystem != null)
        {
            apSystem.OnActionPointsChanged -= UpdateUI;
        }
    }
}
