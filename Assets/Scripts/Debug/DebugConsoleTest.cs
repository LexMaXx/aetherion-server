using UnityEngine;

/// <summary>
/// Test script to verify DebugConsole is working
/// Automatically generates test logs every 2 seconds
/// </summary>
public class DebugConsoleTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool enableAutoTest = true;
    [SerializeField] private float testInterval = 2f;

    private float timer = 0f;
    private int logCount = 0;

    void Start()
    {
        Debug.Log("[DebugConsoleTest] ✅ Test script started!");
        Debug.Log("[DebugConsoleTest] Press F12, F11, or ` (backtick) to open debug console");
        Debug.Log("[DebugConsoleTest] Test logs will appear every 2 seconds");
    }

    void Update()
    {
        if (!enableAutoTest)
            return;

        timer += Time.deltaTime;
        if (timer >= testInterval)
        {
            timer = 0f;
            logCount++;

            // Generate different types of logs
            switch (logCount % 3)
            {
                case 0:
                    Debug.Log($"[DebugConsoleTest] Normal log #{logCount}");
                    break;
                case 1:
                    Debug.LogWarning($"[DebugConsoleTest] Warning log #{logCount}");
                    break;
                case 2:
                    Debug.LogError($"[DebugConsoleTest] Error log #{logCount}");
                    break;
            }
        }

        // Show reminder
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("=== DEBUG CONSOLE HELP ===");
            Debug.Log("F12, F11, or ` - Toggle console");
            Debug.Log("H - Show this help");
            Debug.Log("Current logs: " + logCount);
        }
    }

    // OnGUI removed - it was blocking UI text rendering
    // To see instructions, check Unity Console for [DebugConsoleTest] messages
}
