using UnityEngine;

/// <summary>
/// Debug скрипт для отображения текущей скорости персонажа
/// Добавь на LocalPlayer чтобы увидеть реальную скорость в игре
/// </summary>
public class SpeedDebugger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool showDebugUI = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    private CharacterController characterController;
    private PlayerController playerController;
    private AetherionPlayerController aetherionController;
    private MixamoPlayerController mixamoController;
    private EasyStartPlayerController easyStartController;
    private SimplePlayerController simpleController;
    private CharacterStats characterStats;

    private float currentSpeed = 0f;
    private float maxRecordedSpeed = 0f;

    void Start()
    {
        // Находим все возможные компоненты
        characterController = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();
        aetherionController = GetComponent<AetherionPlayerController>();
        mixamoController = GetComponent<MixamoPlayerController>();
        easyStartController = GetComponent<EasyStartPlayerController>();
        simpleController = GetComponent<SimplePlayerController>();
        characterStats = GetComponent<CharacterStats>();

        Debug.Log("=== SPEED DEBUGGER STARTED ===");
        Debug.Log($"CharacterController: {(characterController != null ? "✅" : "❌")}");
        Debug.Log($"PlayerController: {(playerController != null ? "✅" : "❌")}");
        Debug.Log($"AetherionPlayerController: {(aetherionController != null ? "✅" : "❌")}");
        Debug.Log($"MixamoPlayerController: {(mixamoController != null ? "✅" : "❌")}");
        Debug.Log($"EasyStartPlayerController: {(easyStartController != null ? "✅" : "❌")}");
        Debug.Log($"SimplePlayerController: {(simpleController != null ? "✅" : "❌")}");
        Debug.Log($"CharacterStats: {(characterStats != null ? "✅" : "❌")}");

        if (characterStats != null)
        {
            Debug.Log($"Agility: {characterStats.agility}");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showDebugUI = !showDebugUI;
        }

        if (characterController != null)
        {
            currentSpeed = characterController.velocity.magnitude;
            if (currentSpeed > maxRecordedSpeed)
            {
                maxRecordedSpeed = currentSpeed;
            }
        }
    }

    void OnGUI()
    {
        if (!showDebugUI) return;

        GUI.Box(new Rect(10, 150, 400, 280), "SPEED DEBUGGER (F1 to toggle)");
        float y = 175;

        GUI.Label(new Rect(20, y, 380, 20), $"Current Speed: {currentSpeed:F2} units/sec"); y += 25;
        GUI.Label(new Rect(20, y, 380, 20), $"Max Recorded: {maxRecordedSpeed:F2} units/sec"); y += 25;

        GUI.color = Color.yellow;
        GUI.Label(new Rect(20, y, 380, 20), "Active Controllers:"); y += 25;
        GUI.color = Color.white;

        if (playerController != null && playerController.enabled)
        {
            GUI.color = Color.green;
            GUI.Label(new Rect(30, y, 370, 20), $"✅ PlayerController"); y += 20;
            GUI.color = Color.white;
        }

        if (aetherionController != null && aetherionController.enabled)
        {
            GUI.color = Color.green;
            GUI.Label(new Rect(30, y, 370, 20), $"✅ AetherionPlayerController"); y += 20;
            GUI.color = Color.white;
        }

        if (mixamoController != null && mixamoController.enabled)
        {
            GUI.color = Color.green;
            GUI.Label(new Rect(30, y, 370, 20), $"✅ MixamoPlayerController"); y += 20;
            GUI.color = Color.white;
        }

        if (easyStartController != null && easyStartController.enabled)
        {
            GUI.color = Color.green;
            GUI.Label(new Rect(30, y, 370, 20), $"✅ EasyStartPlayerController"); y += 20;
            GUI.color = Color.white;
        }

        if (simpleController != null && simpleController.enabled)
        {
            GUI.color = Color.green;
            GUI.Label(new Rect(30, y, 370, 20), $"✅ SimplePlayerController"); y += 20;
            GUI.color = Color.white;
        }

        if (characterStats != null)
        {
            GUI.color = Color.cyan;
            y += 10;
            GUI.Label(new Rect(20, y, 380, 20), $"Character Stats:"); y += 20;
            GUI.Label(new Rect(30, y, 370, 20), $"Agility: {characterStats.agility}"); y += 20;
            GUI.Label(new Rect(30, y, 370, 20), $"MovementSpeed: {characterStats.movementSpeed:F2}"); y += 20;
            GUI.color = Color.white;
        }

        GUI.color = Color.yellow;
        y += 10;
        GUI.Label(new Rect(20, y, 380, 20), "Hold SHIFT and move to test sprint speed");
    }
}
