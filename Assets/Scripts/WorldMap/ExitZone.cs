using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Зона выхода из локации на мировую карту
/// Размещается на границе локации (BattleScene)
/// При входе игрока - открывается WorldMapScene
/// </summary>
[RequireComponent(typeof(Collider))]
public class ExitZone : MonoBehaviour
{
    [Header("Exit Settings")]
    [Tooltip("Название сцены мировой карты")]
    [SerializeField] private string worldMapSceneName = "WorldMapScene";

    [Tooltip("Показывать UI подсказку при приближении?")]
    [SerializeField] private bool showPrompt = true;

    [Tooltip("Текст подсказки")]
    [SerializeField] private string promptText = "Нажмите E для выхода на карту мира";

    [Tooltip("Требуется нажатие клавиши или автоматический переход?")]
    [SerializeField] private bool requireKeyPress = true;

    [Tooltip("Клавиша для выхода")]
    [SerializeField] private KeyCode exitKey = KeyCode.E;

    [Header("Visual Feedback")]
    [Tooltip("Эффект при входе в зону (опционально)")]
    [SerializeField] private GameObject exitEffectPrefab;

    private bool playerInZone = false;
    private GameObject currentEffect;

    void Start()
    {
        // Убеждаемся что Collider настроен как Trigger
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"[ExitZone] Collider на '{gameObject.name}' не был Trigger. Исправлено автоматически.");
        }
    }

    void Update()
    {
        if (playerInZone && requireKeyPress)
        {
            if (Input.GetKeyDown(exitKey))
            {
                ExitToWorldMap();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем что это игрок
        if (other.CompareTag("Player"))
        {
            playerInZone = true;

            Debug.Log($"[ExitZone] Игрок вошёл в зону выхода '{gameObject.name}'");

            // Показываем визуальный эффект
            if (exitEffectPrefab != null && currentEffect == null)
            {
                currentEffect = Instantiate(exitEffectPrefab, transform.position, Quaternion.identity);
            }

            // Показываем подсказку
            if (showPrompt)
            {
                ShowPrompt(promptText);
            }

            // Автоматический переход если не требуется нажатие клавиши
            if (!requireKeyPress)
            {
                ExitToWorldMap();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;

            Debug.Log($"[ExitZone] Игрок покинул зону выхода '{gameObject.name}'");

            // Убираем визуальный эффект
            if (currentEffect != null)
            {
                Destroy(currentEffect);
            }

            // Скрываем подсказку
            if (showPrompt)
            {
                HidePrompt();
            }
        }
    }

    /// <summary>
    /// Переход на мировую карту
    /// </summary>
    private void ExitToWorldMap()
    {
        Debug.Log($"[ExitZone] Переход на мировую карту: {worldMapSceneName}");

        // Сохраняем текущую локацию в GameProgress
        GameProgressManager.Instance?.SetLastLocation(SceneManager.GetActiveScene().name);

        // Загружаем сцену мировой карты через SceneTransitionManager
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(worldMapSceneName);
        }
        else
        {
            // Fallback: прямая загрузка без fade-эффекта
            SceneManager.LoadScene(worldMapSceneName);
        }
    }

    /// <summary>
    /// Показать подсказку игроку
    /// </summary>
    private void ShowPrompt(string text)
    {
        // TODO: Интеграция с вашей UI системой
        // Например: UIManager.Instance?.ShowPrompt(text);
        Debug.Log($"[ExitZone] UI Prompt: {text}");
    }

    /// <summary>
    /// Скрыть подсказку
    /// </summary>
    private void HidePrompt()
    {
        // TODO: Интеграция с вашей UI системой
        // Например: UIManager.Instance?.HidePrompt();
        Debug.Log($"[ExitZone] UI Prompt: СКРЫТО");
    }

    // Визуализация зоны в редакторе
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
            }
        }

        // Надпись над зоной
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, "EXIT ZONE", new GUIStyle()
        {
            normal = { textColor = Color.green },
            fontSize = 12,
            fontStyle = FontStyle.Bold
        });
        #endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
        }
    }
}
