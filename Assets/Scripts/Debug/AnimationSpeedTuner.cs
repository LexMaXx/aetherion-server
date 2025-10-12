using UnityEngine;

/// <summary>
/// Настройка скорости анимаций в реальном времени
/// Используй UI слайдеры или клавиши для настройки скорости бега
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimationSpeedTuner : MonoBehaviour
{
    [Header("Animation Speed Multipliers")]
    [Range(0.1f, 3f)]
    [SerializeField] private float walkSpeedMultiplier = 1f;

    [Range(0.1f, 3f)]
    [SerializeField] private float runSpeedMultiplier = 1f;

    [Header("Hotkeys")]
    [SerializeField] private KeyCode increaseWalkSpeed = KeyCode.KeypadPlus;
    [SerializeField] private KeyCode decreaseWalkSpeed = KeyCode.KeypadMinus;
    [SerializeField] private KeyCode increaseRunSpeed = KeyCode.RightBracket;  // ]
    [SerializeField] private KeyCode decreaseRunSpeed = KeyCode.LeftBracket;   // [

    [Header("Adjustment Step")]
    [SerializeField] private float adjustmentStep = 0.1f;

    private Animator animator;
    private bool isRunning = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Определяем, бежит ли персонаж (Shift зажат)
        isRunning = Input.GetKey(KeyCode.LeftShift);

        // Горячие клавиши для настройки скорости ходьбы
        if (Input.GetKeyDown(increaseWalkSpeed))
        {
            walkSpeedMultiplier = Mathf.Clamp(walkSpeedMultiplier + adjustmentStep, 0.1f, 3f);
            Debug.Log($"Walk Speed: {walkSpeedMultiplier:F1}x");
        }
        if (Input.GetKeyDown(decreaseWalkSpeed))
        {
            walkSpeedMultiplier = Mathf.Clamp(walkSpeedMultiplier - adjustmentStep, 0.1f, 3f);
            Debug.Log($"Walk Speed: {walkSpeedMultiplier:F1}x");
        }

        // Горячие клавиши для настройки скорости бега
        if (Input.GetKeyDown(increaseRunSpeed))
        {
            runSpeedMultiplier = Mathf.Clamp(runSpeedMultiplier + adjustmentStep, 0.1f, 3f);
            Debug.Log($"Run Speed: {runSpeedMultiplier:F1}x");
        }
        if (Input.GetKeyDown(decreaseRunSpeed))
        {
            runSpeedMultiplier = Mathf.Clamp(runSpeedMultiplier - adjustmentStep, 0.1f, 3f);
            Debug.Log($"Run Speed: {runSpeedMultiplier:F1}x");
        }

        // Применяем множитель скорости к аниматору
        ApplyAnimationSpeed();
    }

    void ApplyAnimationSpeed()
    {
        if (animator == null)
            return;

        // Получаем текущее состояние анимации
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // Применяем множитель в зависимости от того, бежит персонаж или идет
        float currentMultiplier = isRunning ? runSpeedMultiplier : walkSpeedMultiplier;

        // Устанавливаем скорость анимации
        animator.speed = currentMultiplier;
    }

    void OnGUI()
    {
        // Показываем текущие значения на экране
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.UpperLeft;

        string info = $"Animation Speed Tuner:\n";
        info += $"Walk Speed: {walkSpeedMultiplier:F1}x (Numpad +/-)\n";
        info += $"Run Speed (Shift): {runSpeedMultiplier:F1}x ([ ])\n";
        info += $"\nCurrent Mode: {(isRunning ? "RUN" : "WALK")}";

        GUI.Label(new Rect(10, 10, 400, 150), info, style);
    }

    /// <summary>
    /// Получить текущий множитель скорости ходьбы
    /// </summary>
    public float GetWalkSpeedMultiplier()
    {
        return walkSpeedMultiplier;
    }

    /// <summary>
    /// Получить текущий множитель скорости бега
    /// </summary>
    public float GetRunSpeedMultiplier()
    {
        return runSpeedMultiplier;
    }

    /// <summary>
    /// Установить множитель скорости ходьбы
    /// </summary>
    public void SetWalkSpeedMultiplier(float multiplier)
    {
        walkSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 3f);
    }

    /// <summary>
    /// Установить множитель скорости бега
    /// </summary>
    public void SetRunSpeedMultiplier(float multiplier)
    {
        runSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 3f);
    }
}
