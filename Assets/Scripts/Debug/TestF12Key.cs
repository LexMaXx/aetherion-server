using UnityEngine;

/// <summary>
/// Простой тест F12 клавиши - показывает работает ли Input вообще
/// </summary>
public class TestF12Key : MonoBehaviour
{
    void Update()
    {
        // Тест всех возможных клавиш
        if (Input.GetKeyDown(KeyCode.F12))
        {
            Debug.Log("✅ F12 PRESSED!");
        }

        if (Input.GetKeyDown(KeyCode.F11))
        {
            Debug.Log("✅ F11 PRESSED!");
        }

        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            Debug.Log("✅ BACKTICK (`) PRESSED!");
        }

        // Тест любой клавиши
        if (Input.anyKeyDown)
        {
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    Debug.Log($"Key pressed: {keyCode}");
                }
            }
        }
    }

    void OnGUI()
    {
        // Показываем инструкцию на экране
        GUI.color = Color.yellow;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.fontStyle = FontStyle.Bold;

        GUI.Label(new Rect(10, 10, 800, 100),
            "TEST F12 KEY DETECTOR\n" +
            "Press F12, F11, or ` (backtick)\n" +
            "Check Unity Console for messages",
            style);
    }
}
