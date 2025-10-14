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
    private bool isTargeted = false; // КРИТИЧЕСКОЕ: Никнейм врага показываем ТОЛЬКО при таргете!

    /// <summary>
    /// Инициализация никнейма
    /// </summary>
    public void Initialize(Transform target, string playerName, bool friendly)
    {
        Debug.Log($"[Nameplate] Инициализация начата для {playerName} (friendly={friendly})");

        targetTransform = target;
        username = playerName;
        isFriendly = friendly;

        try
        {
            CreateNameplateUI();
            Debug.Log($"[Nameplate] CreateNameplateUI() завершён для {username}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Nameplate] ОШИБКА в CreateNameplateUI(): {ex.Message}\n{ex.StackTrace}");
            return;
        }

        UpdateNameplateColor();

        // ВАЖНО: Никнейм врага скрыт по умолчанию (пока не таргетнут)
        if (!isFriendly && nameplateContainer != null)
        {
            nameplateContainer.SetActive(false);
            Debug.Log($"[Nameplate] Никнейм врага {username} скрыт по умолчанию (SetActive false)");
        }

        Debug.Log($"[Nameplate] ✅ Создан никнейм для {username} ({(friendly ? "Союзник" : "Враг")}), enabled={enabled}");
    }

    void Update()
    {
        // ДИАГНОСТИКА: Проверяем что Update вызывается
        if (Time.frameCount % 60 == 0 && !isFriendly)
        {
            Debug.Log($"[Nameplate] Update() вызван для {username}, targetTransform={targetTransform != null}, nameplateContainer={nameplateContainer != null}, Camera={Camera.main != null}");
        }

        // Обновляем позицию никнейма над головой игрока
        if (targetTransform != null && nameplateContainer != null && Camera.main != null)
        {
            Vector3 worldPosition = targetTransform.position + offset;
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

            // КРИТИЧЕСКОЕ: Проверяем видимость через FogOfWar!
            bool isVisible = CheckFogOfWarVisibility();
            bool isBehindCamera = screenPosition.z < 0;

            // ВАЖНО: Для ВРАГОВ никнейм показываем если:
            // 1. Таргетнут (показываем ВСЕГДА когда таргетнут, даже если FogOfWar говорит невидим)
            // 2. Союзник и виден
            bool shouldShow = !isBehindCamera && (isFriendly ? isVisible : isTargeted);

            // ДИАГНОСТИКА: Логируем состояние каждую секунду для врагов
            if (!isFriendly && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[Nameplate] {username}: isVisible={isVisible}, isBehindCamera={isBehindCamera}, isTargeted={isTargeted}, shouldShow={shouldShow}, active={nameplateContainer.activeSelf}");
            }

            if (shouldShow)
            {
                if (!nameplateContainer.activeSelf)
                {
                    Debug.Log($"[Nameplate] Показываем никнейм для {username}");
                    nameplateContainer.SetActive(true);
                }
                nameplateContainer.transform.position = screenPosition;
            }
            else
            {
                if (nameplateContainer.activeSelf)
                {
                    Debug.Log($"[Nameplate] Скрываем никнейм для {username}: isVisible={isVisible}, isTargeted={isTargeted}");
                    nameplateContainer.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Проверить видимость через FogOfWar локального игрока
    /// </summary>
    private bool CheckFogOfWarVisibility()
    {
        // ВАЖНО: Никнейм показываем ТОЛЬКО врагам (красным)
        // Союзники (зеленые) всегда видны
        if (isFriendly) return true;

        // Найти локального игрока
        GameObject localPlayer = GameObject.FindGameObjectWithTag("Player");
        if (localPlayer == null) return false;

        // Получить FogOfWar компонент
        FogOfWar fogOfWar = localPlayer.GetComponent<FogOfWar>();
        if (fogOfWar == null) return false;

        // Проверить через Enemy компонент
        Enemy enemy = targetTransform.GetComponent<Enemy>();
        if (enemy != null)
        {
            return fogOfWar.IsEnemyVisible(enemy);
        }

        // Fallback: проверка по Transform
        return fogOfWar.IsEnemyVisible(targetTransform);
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

        // Фон УБРАН - никнейм без фона (только текст с outline)
        // Background удалён по запросу пользователя

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

    /// <summary>
    /// Установить статус таргета (показывать/скрывать никнейм врага)
    /// </summary>
    public void SetTargeted(bool targeted)
    {
        isTargeted = targeted;
        Debug.Log($"[Nameplate] Никнейм {username} - таргет: {targeted}");
    }

    /// <summary>
    /// Показать никнейм (для совместимости с TargetSystem)
    /// </summary>
    public void Show()
    {
        SetTargeted(true);
    }

    /// <summary>
    /// Скрыть никнейм (для совместимости с TargetSystem)
    /// </summary>
    public void Hide()
    {
        SetTargeted(false);
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
