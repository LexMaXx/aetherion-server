using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Никнейм над головой игрока (зеленый для союзника, красный для врага)
/// </summary>
public class Nameplate : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0); // Смещение над головой
    [SerializeField] private bool isFriendly = true; // Союзник (зеленый) или враг (красный)

    [Header("References")]
    private Canvas canvas;
    private GameObject nameplateContainer;
    private Text usernameText;
    private Transform targetTransform;

    private string username = "Player";

    /// <summary>
    /// Инициализация никнейма
    /// </summary>
    public void Initialize(Transform target, string playerName, bool friendly)
    {
        targetTransform = target;
        username = playerName;
        isFriendly = friendly;

        CreateNameplateUI();
        UpdateNameplateColor();

        Debug.Log($"[Nameplate] ✅ Создан никнейм для {username} ({(friendly ? "Союзник" : "Враг")})");
    }

    void Update()
    {
        // Обновляем позицию никнейма над головой игрока
        if (targetTransform != null && nameplateContainer != null && Camera.main != null)
        {
            Vector3 worldPosition = targetTransform.position + offset;
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

            // Скрываем если за камерой
            if (screenPosition.z < 0)
            {
                nameplateContainer.SetActive(false);
            }
            else
            {
                nameplateContainer.SetActive(true);
                nameplateContainer.transform.position = screenPosition;
            }
        }
    }

    private void CreateNameplateUI()
    {
        // Находим или создаём Canvas
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Nameplate_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // Поверх всего

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Контейнер для никнейма
        nameplateContainer = new GameObject($"Nameplate_{username}");
        nameplateContainer.transform.SetParent(canvas.transform, false);

        RectTransform containerRect = nameplateContainer.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(200, 30);

        // Фон для никнейма (полупрозрачный)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(nameplateContainer.transform, false);

        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(-10, -5);
        bgRect.offsetMax = new Vector2(10, 5);

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.6f);

        // Текст никнейма
        GameObject textObj = new GameObject("Username");
        textObj.transform.SetParent(nameplateContainer.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        usernameText = textObj.AddComponent<Text>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null) usernameText.font = font;
        usernameText.fontSize = 18;
        usernameText.alignment = TextAnchor.MiddleCenter;
        usernameText.fontStyle = FontStyle.Bold;
        usernameText.text = username;

        // Outline для читаемости
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);
    }

    private void UpdateNameplateColor()
    {
        if (usernameText == null) return;

        if (isFriendly)
        {
            usernameText.color = new Color(0.3f, 1f, 0.3f); // Зеленый
            Debug.Log($"[Nameplate] Никнейм {username} - Зеленый (Союзник)");
        }
        else
        {
            usernameText.color = new Color(1f, 0.2f, 0.2f); // Красный
            Debug.Log($"[Nameplate] Никнейм {username} - Красный (Враг)");
        }
    }

    /// <summary>
    /// Изменить статус союзник/враг
    /// </summary>
    public void SetFriendly(bool friendly)
    {
        isFriendly = friendly;
        UpdateNameplateColor();
    }

    /// <summary>
    /// Изменить имя
    /// </summary>
    public void SetUsername(string newName)
    {
        username = newName;
        if (usernameText != null)
        {
            usernameText.text = username;
        }
    }

    void OnDestroy()
    {
        // Удаляем UI при уничтожении компонента
        if (nameplateContainer != null)
        {
            Destroy(nameplateContainer);
        }
    }
}
