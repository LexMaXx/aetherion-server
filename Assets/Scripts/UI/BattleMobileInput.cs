using UnityEngine;
using StarterAssets;

/// <summary>
/// Связывает VirtualJoystick с StarterAssetsInputs для мобильного управления
/// Работает аналогично WASD - передает input из джойстика в систему управления
/// Автоматически находит StarterInputs на заспавненном персонаже
/// </summary>
public class BattleMobileInput : MonoBehaviour
{
    [Header("Joystick Reference")]
    [Tooltip("Виртуальный джойстик для управления")]
    [SerializeField] private VirtualJoystick joystick;

    [Header("Settings")]
    [SerializeField] private bool enableMobileInput = true;
    [SerializeField] private float runThreshold = 0.7f; // Порог для бега (если джойстик > 70%)

    // Текущий активный StarterInputs (берётся с заспавненного персонажа)
    private StarterAssetsInputs activeStarterInputs;
    private bool hasLoggedError = false; // Чтобы не спамить в Console

    private bool hasLoggedNoInputs = false;

    void Update()
    {
        // Быстрая проверка с логом ОДИН РАЗ
        if (!enableMobileInput || joystick == null || activeStarterInputs == null)
        {
            if (!hasLoggedNoInputs)
            {
                Debug.LogWarning($"[BattleMobileInput] ⚠️ Update НЕ работает! enableMobileInput={enableMobileInput}, joystick={joystick != null}, activeStarterInputs={activeStarterInputs != null}");
                hasLoggedNoInputs = true;
            }
            return; // Тихо выходим
        }

        // Получаем input из джойстика
        Vector2 joystickInput = joystick.GetInputDirection();

        // Передаем в StarterAssetsInputs (аналогично WASD)
        activeStarterInputs.MoveInput(joystickInput);

        // Автоматический бег если джойстик отклонен больше чем на 70%
        float magnitude = joystickInput.magnitude;
        activeStarterInputs.SprintInput(false); // ОТКЛЮЧЕНО: всегда ходьба, никогда спринт

        // Debug - показываем только когда джойстик двигается
        if (magnitude > 0.01f)
        {
            Debug.Log($"[BattleMobileInput] ✅ Joystick: ({joystickInput.x:F2}, {joystickInput.y:F2}), Sprint: {magnitude > runThreshold}");
        }
    }

    /// <summary>
    /// Инициализация - вызывается из BattleSceneUIManager
    /// ВАЖНО: Берёт StarterInputs С ЗАСПАВНЕННОГО ПЕРСОНАЖА, а не с префаба!
    /// </summary>
    public void Initialize(VirtualJoystick virtualJoystick, GameObject player)
    {
        Debug.Log($"[BattleMobileInput] 🔧 Инициализация джойстика для {player.name}...");

        joystick = virtualJoystick;

        if (joystick == null)
        {
            Debug.LogError("[BattleMobileInput] ❌ VirtualJoystick = null!");
            return;
        }

        if (player == null)
        {
            Debug.LogError("[BattleMobileInput] ❌ Player = null!");
            return;
        }

        // КРИТИЧЕСКОЕ: Ищем StarterInputs НА ЗАСПАВНЕННОМ ПЕРСОНАЖЕ!
        activeStarterInputs = player.GetComponent<StarterAssetsInputs>();

        if (activeStarterInputs == null)
        {
            Debug.LogError($"[BattleMobileInput] ❌ StarterInputs НЕ НАЙДЕН на {player.name}!");
            Debug.LogError($"[BattleMobileInput] 💡 Компонент должен быть на заспавненном персонаже!");

            // Показываем все компоненты для диагностики
            Component[] components = player.GetComponents<Component>();
            Debug.LogWarning($"[BattleMobileInput] 📋 Компоненты на {player.name}:");
            foreach (var comp in components)
            {
                Debug.LogWarning($"  - {comp.GetType().Name}");
            }

            return;
        }

        Debug.Log($"[BattleMobileInput] ✅ StarterInputs найден на заспавненном персонаже {player.name}!");

        // Определяем мобильная ли платформа
#if UNITY_ANDROID || UNITY_IOS
        enableMobileInput = true;
        Debug.Log("[BattleMobileInput] 📱 Мобильная платформа - джойстик АКТИВЕН");
#else
        enableMobileInput = true; // Для тестов в редакторе
        Debug.Log("[BattleMobileInput] 🖥️ Десктоп платформа - джойстик АКТИВЕН (для теста)");
#endif

        Debug.Log($"[BattleMobileInput] ✅ Инициализация завершена! Joystick: ✓, StarterInputs: ✓ (с персонажа)");
    }

    /// <summary>
    /// Включить/выключить мобильный input
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        enableMobileInput = enabled;
        Debug.Log($"[BattleMobileInput] Мобильный ввод: {(enabled ? "ВКЛЮЧЕН" : "ВЫКЛЮЧЕН")}");
    }

    /// <summary>
    /// Проверить активен ли мобильный input
    /// </summary>
    public bool IsEnabled()
    {
        return enableMobileInput;
    }
}
