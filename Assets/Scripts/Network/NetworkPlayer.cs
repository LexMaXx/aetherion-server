using UnityEngine;
using TMPro;

/// <summary>
/// Представляет удаленного игрока в мультиплеере
/// Синхронизирует позицию, анимацию, здоровье
/// </summary>
public class NetworkPlayer : MonoBehaviour
{
    [Header("Network Info")]
    public string socketId;
    public string username;
    public string characterClass;

    [Header("Components")]
    private Animator animator;
    private CharacterController characterController;
    private NetworkTransform networkTransform;

    [Header("UI")]
    [SerializeField] private GameObject nameplatePrefab;
    private GameObject nameplateInstance;
    private TextMeshProUGUI usernameText;
    private UnityEngine.UI.Image healthBar;

    [Header("Sync Settings")]
    [SerializeField] private float positionLerpSpeed = 10f;
    [SerializeField] private float rotationLerpSpeed = 10f;

    // Target state (received from server)
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private string currentAnimationState = "Idle";

    // Health
    private int currentHP = 100;
    private int maxHP = 100;
    private int currentMP = 100;
    private int maxMP = 100;

    // Interpolation
    private bool hasReceivedFirstUpdate = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        // Добавляем или получаем NetworkTransform для плавной синхронизации
        networkTransform = GetComponent<NetworkTransform>();
        if (networkTransform == null)
        {
            networkTransform = gameObject.AddComponent<NetworkTransform>();
        }

        // Disable local player components for network players
        var playerController = GetComponent<PlayerController>();
        if (playerController != null) playerController.enabled = false;

        var playerAttack = GetComponent<PlayerAttack>();
        if (playerAttack != null) playerAttack.enabled = false;

        var targetSystem = GetComponent<TargetSystem>();
        if (targetSystem != null) targetSystem.enabled = false;

        // But enable health/stats for damage visualization
        // Keep CharacterStatsData, HealthSystem, etc enabled
    }

    void Start()
    {
        CreateNameplate();
    }

    void Update()
    {
        if (!hasReceivedFirstUpdate) return;

        // Smooth position interpolation
        if (characterController != null && characterController.enabled)
        {
            Vector3 movement = Vector3.Lerp(transform.position, targetPosition, positionLerpSpeed * Time.deltaTime);
            characterController.Move(movement - transform.position);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, positionLerpSpeed * Time.deltaTime);
        }

        // Smooth rotation interpolation
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);

        // Update nameplate position
        if (nameplateInstance != null)
        {
            Vector3 nameplatePos = transform.position + Vector3.up * 2.5f; // Above player
            nameplateInstance.transform.position = nameplatePos;
            nameplateInstance.transform.rotation = Camera.main.transform.rotation; // Billboard
        }
    }

    /// <summary>
    /// Создать табличку с именем над головой
    /// </summary>
    private void CreateNameplate()
    {
        if (nameplatePrefab != null)
        {
            nameplateInstance = Instantiate(nameplatePrefab, transform.position + Vector3.up * 2.5f, Quaternion.identity);
            nameplateInstance.transform.SetParent(null); // Don't parent to player

            // Find UI components
            usernameText = nameplateInstance.GetComponentInChildren<TextMeshProUGUI>();
            healthBar = nameplateInstance.transform.Find("HealthBar")?.GetComponent<UnityEngine.UI.Image>();

            if (usernameText != null)
            {
                usernameText.text = username;
            }

            UpdateHealthBar();
        }
        else
        {
            Debug.LogWarning("[NetworkPlayer] Nameplate prefab не назначен!");
        }
    }

    /// <summary>
    /// Обновить позицию от сервера (с поддержкой velocity для Dead Reckoning)
    /// </summary>
    public void UpdatePosition(Vector3 position, Quaternion rotation, Vector3 velocity = default, float timestamp = 0f)
    {
        if (timestamp == 0f)
        {
            timestamp = Time.time;
        }

        // Используем NetworkTransform для плавной синхронизации
        if (networkTransform != null)
        {
            networkTransform.ReceivePositionUpdate(position, rotation, velocity, timestamp);
        }
        else
        {
            // Fallback к старому методу если NetworkTransform отсутствует
            targetPosition = position;
            targetRotation = rotation;

            if (!hasReceivedFirstUpdate)
            {
                // First update - teleport
                transform.position = position;
                transform.rotation = rotation;
                hasReceivedFirstUpdate = true;
            }
        }

        hasReceivedFirstUpdate = true;
    }

    /// <summary>
    /// Обновить анимацию от сервера
    /// </summary>
    public void UpdateAnimation(string animationState)
    {
        if (currentAnimationState == animationState) return;

        currentAnimationState = animationState;

        if (animator == null) return;

        // Reset all animation states
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        animator.SetBool("isAttacking", false);
        animator.SetBool("isDead", false);

        // Set new state
        switch (animationState)
        {
            case "Idle":
                // Default state
                break;
            case "Walking":
                animator.SetBool("isWalking", true);
                break;
            case "Running":
                animator.SetBool("isRunning", true);
                break;
            case "Attacking":
                animator.SetTrigger("Attack");
                animator.SetBool("isAttacking", true);
                break;
            case "Dead":
                animator.SetBool("isDead", true);
                break;
            case "Casting":
                animator.SetTrigger("Cast");
                break;
        }
    }

    /// <summary>
    /// Обновить здоровье от сервера
    /// </summary>
    public void UpdateHealth(int hp, int maxHp, int mp, int maxMp)
    {
        currentHP = hp;
        maxHP = maxHp;
        currentMP = mp;
        maxMP = maxMp;

        UpdateHealthBar();

        // Check death
        if (currentHP <= 0)
        {
            OnDeath();
        }
    }

    /// <summary>
    /// Обновить визуализацию HP бара
    /// </summary>
    private void UpdateHealthBar()
    {
        if (healthBar != null && maxHP > 0)
        {
            healthBar.fillAmount = (float)currentHP / maxHP;
        }
    }

    /// <summary>
    /// Обработать смерть
    /// </summary>
    private void OnDeath()
    {
        UpdateAnimation("Dead");
        Debug.Log($"[NetworkPlayer] {username} погиб!");

        // Disable collider so players can walk through corpse
        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
    }

    /// <summary>
    /// Обработать респавн
    /// </summary>
    public void OnRespawn(Vector3 spawnPosition)
    {
        currentHP = maxHP;
        currentMP = maxMP;

        UpdatePosition(spawnPosition, Quaternion.identity);
        UpdateAnimation("Idle");
        UpdateHealthBar();

        // Re-enable collider
        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = true;

        Debug.Log($"[NetworkPlayer] {username} возродился!");
    }

    /// <summary>
    /// Воспроизвести анимацию атаки
    /// </summary>
    public void PlayAttackAnimation(string attackType)
    {
        if (animator == null) return;

        switch (attackType)
        {
            case "melee":
                animator.SetTrigger("Attack");
                break;
            case "ranged":
                animator.SetTrigger("RangedAttack");
                break;
            case "skill":
                animator.SetTrigger("Cast");
                break;
        }
    }

    /// <summary>
    /// Показать урон (визуальный эффект)
    /// </summary>
    public void ShowDamage(float damage)
    {
        // TODO: Create damage number popup
        Debug.Log($"[NetworkPlayer] {username} получил {damage} урона!");

        // Flash red effect
        StartCoroutine(FlashRed());
    }

    /// <summary>
    /// Мигание красным при получении урона
    /// </summary>
    private System.Collections.IEnumerator FlashRed()
    {
        var renderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;
            renderer.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            renderer.material.color = originalColor;
        }
    }

    void OnDestroy()
    {
        // Clean up nameplate
        if (nameplateInstance != null)
        {
            Destroy(nameplateInstance);
        }
    }

    // Public getters
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public int CurrentMP => currentMP;
    public int MaxMP => maxMP;
    public bool IsAlive => currentHP > 0;
}
