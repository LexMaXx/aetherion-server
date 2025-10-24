using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Менеджер для создания всплывающих цифр урона
/// </summary>
public class DamageNumberManager : MonoBehaviour
{
    private static DamageNumberManager instance;
    public static DamageNumberManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("DamageNumberManager");
                instance = go.AddComponent<DamageNumberManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [Header("Prefab")]
    [SerializeField] private GameObject damageNumberPrefab;

    [Header("Canvas")]
    private Canvas worldCanvas;
    private Camera mainCamera;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Initialize();
    }

    void Initialize()
    {
        mainCamera = Camera.main;

        // Создаём Canvas для damage numbers
        GameObject canvasObj = new GameObject("DamageNumberCanvas");
        canvasObj.transform.SetParent(transform);

        worldCanvas = canvasObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;

        // ВАЖНО: Настраиваем масштаб Canvas для WorldSpace
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100, 100);
        canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f); // Маленький масштаб для 3D мира

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        canvasObj.AddComponent<GraphicRaycaster>();

        Debug.Log("[DamageNumberManager] Инициализирован с WorldSpace Canvas");
    }

    /// <summary>
    /// Показать цифру урона в мировых координатах
    /// </summary>
    public void ShowDamage(Vector3 worldPosition, float damage, bool isCritical = false, bool isHeal = false)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[DamageNumberManager] Camera.main не найдена!");
                return;
            }
        }

        if (worldCanvas == null)
        {
            Debug.Log("[DamageNumberManager] Canvas не найден, инициализирую...");
            Initialize();
        }

        // Создаём prefab если его нет
        if (damageNumberPrefab == null)
        {
            Debug.Log("[DamageNumberManager] Prefab не назначен, создаю дефолтный...");
            CreateDefaultPrefab();
        }

        // Создаём цифру
        GameObject numberObj = Instantiate(damageNumberPrefab, worldCanvas.transform);
        numberObj.SetActive(true); // Убеждаемся что активен

        // Позиция чуть выше точки попадания
        Vector3 spawnPos = worldPosition + Vector3.up * 2f;
        numberObj.transform.position = spawnPos;

        Debug.Log($"[DamageNumberManager] Создана цифра на позиции: {spawnPos}");

        // Поворачиваем к камере
        if (mainCamera != null)
        {
            numberObj.transform.LookAt(mainCamera.transform);
            numberObj.transform.Rotate(0, 180, 0); // Разворачиваем текст
        }

        // Инициализируем
        DamageNumber damageNumber = numberObj.GetComponent<DamageNumber>();
        if (damageNumber != null)
        {
            damageNumber.Initialize(damage, isCritical, isHeal);
            Debug.Log($"[DamageNumberManager] ✅ Урон {damage} показан (Crit: {isCritical})");
        }
        else
        {
            Debug.LogError("[DamageNumberManager] ❌ DamageNumber компонент не найден на prefab!");
        }
    }

    /// <summary>
    /// Создать дефолтный prefab если не назначен
    /// </summary>
    void CreateDefaultPrefab()
    {
        GameObject prefab = new GameObject("DamageNumberPrefab");
        prefab.SetActive(false);

        // Canvas для правильного масштаба
        RectTransform rectTransform = prefab.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 100);

        // TextMeshPro
        TextMeshProUGUI textMesh = prefab.AddComponent<TextMeshProUGUI>();

        // Пытаемся загрузить стандартный шрифт TMP
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null)
        {
            // Если не найден, пробуем альтернативный путь
            font = Resources.Load<TMP_FontAsset>("TextMesh Pro/Fonts/LiberationSans SDF");
        }
        if (font != null)
        {
            textMesh.font = font;
        }

        textMesh.fontSize = 36;
        textMesh.color = Color.white;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontStyle = FontStyles.Bold;
        textMesh.enableWordWrapping = false;

        // Компонент DamageNumber
        prefab.AddComponent<DamageNumber>();

        damageNumberPrefab = prefab;

        Debug.Log("[DamageNumberManager] Создан дефолтный prefab");
    }

    void Update()
    {
        // Обновляем rotation canvas чтобы всегда смотрел на камеру
        if (mainCamera != null && worldCanvas != null)
        {
            worldCanvas.transform.LookAt(mainCamera.transform);
            worldCanvas.transform.Rotate(0, 180, 0);
        }
    }
}
