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
        // ВАЖНО: Animator находится на дочернем объекте "Model"
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"[NetworkPlayer] ⚠️ Animator не найден для {gameObject.name}!");
        }

        characterController = GetComponent<CharacterController>();

        // ВАЖНО: Отключаем CharacterController для сетевых игроков
        // Иначе он будет применять гравитацию и коллизии, которые конфликтуют с сетевой позицией
        if (characterController != null)
        {
            characterController.enabled = false;
            Debug.Log("[NetworkPlayer] ✅ CharacterController отключён для сетевого игрока");
        }

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

        // ВАЖНО: Если NetworkTransform существует, он управляет позицией
        // Иначе управляем позицией здесь
        if (networkTransform == null)
        {
            // Smooth position interpolation
            // CharacterController отключён, используем прямую установку transform.position
            transform.position = Vector3.Lerp(transform.position, targetPosition, positionLerpSpeed * Time.deltaTime);

            // Smooth rotation interpolation
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);
        }

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

        // ДИАГНОСТИКА: Логируем обновление позиции
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[NetworkPlayer] 🔧 UpdatePosition для {username}: current=({transform.position.x:F2}, {transform.position.y:F2}, {transform.position.z:F2}), target=({position.x:F2}, {position.y:F2}, {position.z:F2}), distance={Vector3.Distance(transform.position, position):F2}m");
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
                Debug.Log($"[NetworkPlayer] 🎯 Первая позиция для {username}: ({position.x:F2}, {position.y:F2}, {position.z:F2})");
            }
        }

        hasReceivedFirstUpdate = true;
    }

    /// <summary>
    /// Обновить анимацию от сервера
    /// </summary>
    public void UpdateAnimation(string animationState)
    {
        // ДИАГНОСТИКА: Логируем ВСЕ попытки обновления анимации (ВСЕГДА, не только каждую секунду!)
        Debug.Log($"[NetworkPlayer] 🔄 UpdateAnimation вызван для {username}: текущее={currentAnimationState}, новое={animationState}");

        if (animator == null)
        {
            Debug.LogError($"[NetworkPlayer] ❌ КРИТИЧЕСКАЯ ОШИБКА: Animator is null для {username}!");
            Debug.LogError($"[NetworkPlayer] 🔍 Пытаюсь найти Animator в дочерних объектах...");

            // Пытаемся найти Animator снова
            animator = GetComponentInChildren<Animator>();

            if (animator == null)
            {
                Debug.LogError($"[NetworkPlayer] ❌ Animator НЕ НАЙДЕН даже в дочерних объектах!");
                return;
            }
            else
            {
                Debug.Log($"[NetworkPlayer] ✅ Animator найден в дочернем объекте: {animator.gameObject.name}");
            }
        }

        // ВАЖНО: ВСЕГДА обновляем анимацию для реал-тайм синхронизации
        bool stateChanged = (currentAnimationState != animationState);

        if (stateChanged)
        {
            Debug.Log($"[NetworkPlayer] 🎬 Анимация для {username}: {currentAnimationState} → {animationState}");
            currentAnimationState = animationState;
        }
        else
        {
            Debug.Log($"[NetworkPlayer] 🔄 Повторное применение анимации {animationState} для {username}");
        }

        // ВАЖНО: PlayerController использует Blend Tree систему
        // IsMoving (bool), MoveX (float), MoveY (float)
        // Не используем isWalking/isRunning, потому что они отсутствуют

        // Set new state
        Debug.Log($"[NetworkPlayer] 🎭 Применяю анимацию '{animationState}' для {username}");

        switch (animationState)
        {
            case "Idle":
                Debug.Log($"[NetworkPlayer] ➡️ Idle: IsMoving=false, MoveX=0, MoveY=0, speed=1.0");
                animator.SetBool("IsMoving", false);
                animator.SetFloat("MoveX", 0);
                animator.SetFloat("MoveY", 0);
                animator.speed = 1.0f;
                break;

            case "Walking":
                Debug.Log($"[NetworkPlayer] ➡️ Walking: IsMoving=true, MoveX=0, MoveY=0.5, speed=0.5");
                animator.SetBool("IsMoving", true);
                animator.SetFloat("MoveX", 0);
                animator.SetFloat("MoveY", 0.5f); // 0.5 = Slow Run (ходьба)
                animator.speed = 0.5f; // Замедленная анимация для ходьбы
                break;

            case "Running":
                Debug.Log($"[NetworkPlayer] ➡️ Running: IsMoving=true, MoveX=0, MoveY=1.0, speed=1.0");
                animator.SetBool("IsMoving", true);
                animator.SetFloat("MoveX", 0);
                animator.SetFloat("MoveY", 1.0f); // 1.0 = Sprint (бег)
                animator.speed = 1.0f; // Нормальная скорость анимации
                break;

            case "Attacking":
                Debug.Log($"[NetworkPlayer] ➡️ Attacking: Trigger=Attack");
                animator.SetTrigger("Attack");
                // Не меняем IsMoving - атака может быть во время движения
                break;

            case "Dead":
                Debug.Log($"[NetworkPlayer] ➡️ Dead: isDead=true, IsMoving=false");
                if (HasAnimatorParameter(animator, "isDead"))
                {
                    animator.SetBool("isDead", true);
                }
                animator.SetBool("IsMoving", false);
                break;

            case "Casting":
                Debug.Log($"[NetworkPlayer] ➡️ Casting: Trigger=Cast");
                animator.SetTrigger("Cast");
                break;

            default:
                Debug.LogWarning($"[NetworkPlayer] ⚠️ Неизвестное состояние анимации: '{animationState}' для {username}");
                break;
        }

        // Логируем итоговое состояние параметров аниматора
        Debug.Log($"[NetworkPlayer] 📊 Состояние Animator для {username}: IsMoving={animator.GetBool("IsMoving")}, MoveY={animator.GetFloat("MoveY"):F2}, speed={animator.speed:F2}");
    }

    /// <summary>
    /// Проверить есть ли параметр в Animator
    /// </summary>
    private bool HasAnimatorParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
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
