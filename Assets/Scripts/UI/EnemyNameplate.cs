using UnityEngine;
using TMPro;

/// <summary>
/// Никнейм врага с механикой исчезновения в Fog of War
/// НОВАЯ СИСТЕМА: Никнейм управляется только FogOfWar и TargetSystem
/// </summary>
public class EnemyNameplate : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private UnityEngine.UI.Image healthBar;

    [Header("Settings")]
    [SerializeField] private float heightAbovePlayer = 2.5f;
    [SerializeField] private bool billboard = true; // Поворот к камере

    // Ссылки
    private Transform playerTransform; // Владелец этого nameplate
    private Camera mainCamera;
    private FogOfWar fogOfWar; // Для проверки видимости
    private bool isTargeted = false; // Затаргечен ли игрок

    void Start()
    {
        mainCamera = Camera.main;

        // Находим FogOfWar у локального игрока
        var localPlayer = GameObject.FindGameObjectWithTag("Player");
        if (localPlayer != null)
        {
            fogOfWar = localPlayer.GetComponent<FogOfWar>();
        }

        // ПО УМОЛЧАНИЮ СКРЫТ!
        gameObject.SetActive(false);

        Debug.Log($"[EnemyNameplate] Создан для {usernameText?.text ?? "unknown"}, СКРЫТ по умолчанию");
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        // КРИТИЧЕСКИ ВАЖНО: Проверяем видимость через FogOfWar
        bool isVisible = CheckVisibility();

        // Показываем никнейм ТОЛЬКО если:
        // 1. Враг виден (не в тумане войны, не за стеной)
        // 2. Враг затаргечен
        bool shouldBeVisible = isVisible && isTargeted;

        if (gameObject.activeSelf != shouldBeVisible)
        {
            gameObject.SetActive(shouldBeVisible);
            Debug.Log($"[EnemyNameplate] {usernameText?.text}: visible={shouldBeVisible} (isVisible={isVisible}, isTargeted={isTargeted})");
        }

        // Обновляем позицию ТОЛЬКО если видим
        if (shouldBeVisible)
        {
            // Позиция над игроком
            transform.position = playerTransform.position + Vector3.up * heightAbovePlayer;

            // Billboard (поворот к камере)
            if (billboard && mainCamera != null)
            {
                transform.rotation = mainCamera.transform.rotation;
            }
        }
    }

    /// <summary>
    /// Проверить виден ли враг (через FogOfWar)
    /// </summary>
    private bool CheckVisibility()
    {
        if (fogOfWar == null || playerTransform == null)
        {
            // Если нет FogOfWar - считаем что всегда виден
            return true;
        }

        // Проверяем через Enemy компонент
        Enemy enemy = playerTransform.GetComponent<Enemy>();
        if (enemy != null)
        {
            return fogOfWar.IsEnemyVisible(enemy);
        }

        // Fallback - проверяем расстояние напрямую
        return fogOfWar.IsEnemyVisible(playerTransform);
    }

    /// <summary>
    /// Установить владельца (NetworkPlayer)
    /// </summary>
    public void SetOwner(Transform owner)
    {
        playerTransform = owner;
        Debug.Log($"[EnemyNameplate] Владелец установлен: {owner.name}");
    }

    /// <summary>
    /// Установить текст никнейма
    /// </summary>
    public void SetUsername(string username)
    {
        if (usernameText != null)
        {
            usernameText.text = username;
        }
    }

    /// <summary>
    /// Обновить HP бар
    /// </summary>
    public void UpdateHealthBar(float currentHP, float maxHP)
    {
        if (healthBar != null && maxHP > 0)
        {
            healthBar.fillAmount = currentHP / maxHP;
        }
    }

    /// <summary>
    /// Установить состояние таргета (вызывается TargetSystem)
    /// </summary>
    public void SetTargeted(bool targeted)
    {
        isTargeted = targeted;
        Debug.Log($"[EnemyNameplate] {usernameText?.text}: targeted={targeted}");
    }
}
