using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Диагностика AcceptButton - почему кнопка не работает?
/// </summary>
public class DiagnoseAcceptButton : MonoBehaviour
{
    [Header("Check Every Frame")]
    [SerializeField] private bool checkContinuously = true;

    private Button acceptButton;
    private WorldMapPlayerController playerController;
    private float lastCheckTime = 0f;

    void Start()
    {
        // Ищем AcceptButton
        acceptButton = GameObject.Find("AcceptButton")?.GetComponent<Button>();

        if (acceptButton == null)
        {
            Debug.LogError("[DiagnoseAcceptButton] ❌ AcceptButton не найдена в сцене!");
            Debug.LogError("[DiagnoseAcceptButton] 💡 Создайте кнопку с именем 'AcceptButton'");
        }
        else
        {
            Debug.Log($"[DiagnoseAcceptButton] ✅ AcceptButton найдена: {acceptButton.gameObject.name}");
            Debug.Log($"[DiagnoseAcceptButton] 📍 Active: {acceptButton.gameObject.activeSelf}");
            Debug.Log($"[DiagnoseAcceptButton] 📍 Interactable: {acceptButton.interactable}");
            Debug.Log($"[DiagnoseAcceptButton] 📍 Listeners: {acceptButton.onClick.GetPersistentEventCount()}");
        }

        // Ищем игрока
        Invoke(nameof(FindPlayer), 0.5f);
    }

    void FindPlayer()
    {
        playerController = FindObjectOfType<WorldMapPlayerController>();

        if (playerController == null)
        {
            Debug.LogWarning("[DiagnoseAcceptButton] ⚠️ WorldMapPlayerController не найден!");
            Debug.Log("[DiagnoseAcceptButton] 🔄 Повторная попытка через 0.5с...");
            Invoke(nameof(FindPlayer), 0.5f);
        }
        else
        {
            Debug.Log($"[DiagnoseAcceptButton] ✅ WorldMapPlayerController найден: {playerController.gameObject.name}");
            CheckConnection();
        }
    }

    void Update()
    {
        if (!checkContinuously)
            return;

        // Проверяем каждую секунду
        if (Time.time - lastCheckTime > 1f)
        {
            lastCheckTime = Time.time;
            CheckButtonState();
        }
    }

    [ContextMenu("Check Connection")]
    void CheckConnection()
    {
        if (playerController == null || acceptButton == null)
            return;

        // Используем рефлексию чтобы проверить подключение
        var field = typeof(WorldMapPlayerController).GetField("acceptButton",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            Button connectedButton = field.GetValue(playerController) as Button;

            if (connectedButton == acceptButton)
            {
                Debug.Log("[DiagnoseAcceptButton] ✅ AcceptButton ПОДКЛЮЧЕНА к WorldMapPlayerController!");
            }
            else if (connectedButton == null)
            {
                Debug.LogError("[DiagnoseAcceptButton] ❌ AcceptButton НЕ подключена к WorldMapPlayerController!");
                Debug.LogError("[DiagnoseAcceptButton] 💡 Запустите AutoConnectAcceptButton.TryConnect()");
            }
            else
            {
                Debug.LogWarning("[DiagnoseAcceptButton] ⚠️ Подключена другая кнопка!");
            }
        }
    }

    [ContextMenu("Check Button State")]
    void CheckButtonState()
    {
        if (acceptButton == null)
            return;

        bool isActive = acceptButton.gameObject.activeSelf;
        bool isInteractable = acceptButton.interactable;

        if (isActive)
        {
            Debug.Log($"[DiagnoseAcceptButton] 👁️ Кнопка ВИДИМА | Interactable: {isInteractable}");
        }
    }

    [ContextMenu("Full Diagnosis")]
    public void FullDiagnosis()
    {
        Debug.Log("=== ПОЛНАЯ ДИАГНОСТИКА ACCEPTBUTTON ===");

        // 1. Проверка кнопки
        if (acceptButton != null)
        {
            Debug.Log($"✅ AcceptButton существует: {acceptButton.gameObject.name}");
            Debug.Log($"   Active: {acceptButton.gameObject.activeSelf}");
            Debug.Log($"   Interactable: {acceptButton.interactable}");
            Debug.Log($"   Position: {acceptButton.transform.position}");

            Canvas canvas = acceptButton.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"   Canvas: {canvas.name} (RenderMode: {canvas.renderMode})");
            }
            else
            {
                Debug.LogError("   ❌ Кнопка НЕ внутри Canvas!");
            }
        }
        else
        {
            Debug.LogError("❌ AcceptButton не найдена!");
        }

        // 2. Проверка игрока
        if (playerController != null)
        {
            Debug.Log($"✅ WorldMapPlayerController найден: {playerController.gameObject.name}");
            Debug.Log($"   Position: {playerController.transform.position}");
        }
        else
        {
            Debug.LogError("❌ WorldMapPlayerController не найден!");
        }

        // 3. Проверка подключения
        CheckConnection();

        // 4. Проверка локаций
        WorldMapManager manager = FindObjectOfType<WorldMapManager>();
        if (manager != null)
        {
            Debug.Log($"✅ WorldMapManager найден");

            var nearestMethod = typeof(WorldMapManager).GetMethod("GetNearestMarker",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            if (nearestMethod != null)
            {
                var nearest = nearestMethod.Invoke(manager, null);
                if (nearest != null)
                {
                    Debug.Log($"   Ближайшая локация: {nearest}");
                }
                else
                {
                    Debug.LogWarning("   ⚠️ Ближайшая локация: НЕТ (слишком далеко?)");
                }
            }
        }
        else
        {
            Debug.LogError("❌ WorldMapManager не найден!");
        }

        Debug.Log("==========================================");
    }

    [ContextMenu("Test Click")]
    public void TestClick()
    {
        if (acceptButton != null)
        {
            Debug.Log("[DiagnoseAcceptButton] 🖱️ Симуляция клика на AcceptButton...");
            acceptButton.onClick.Invoke();
        }
    }
}
