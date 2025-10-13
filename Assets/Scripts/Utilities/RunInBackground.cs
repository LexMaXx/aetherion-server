using UnityEngine;

/// <summary>
/// Позволяет Unity работать в фоновом режиме
/// ВАЖНО для мультиплеера - чтобы можно было тестировать на двух окнах одновременно
/// </summary>
public class RunInBackground : MonoBehaviour
{
    [Header("Background Settings")]
    [Tooltip("Продолжать обновление игры когда окно не в фокусе")]
    [SerializeField] private bool runInBackground = true;

    [Tooltip("Ограничить FPS в фоновом режиме (0 = без ограничений)")]
    [SerializeField] private int backgroundFrameRate = 30;

    [Tooltip("Ограничить FPS в активном режиме (0 = VSync, -1 = без ограничений)")]
    [SerializeField] private int foregroundFrameRate = 60;

    private bool hasFocus = true;

    void Awake()
    {
        // Разрешаем работу в фоновом режиме
        Application.runInBackground = runInBackground;

        // Устанавливаем целевой FPS для активного режима
        if (foregroundFrameRate == 0)
        {
            QualitySettings.vSyncCount = 1; // VSync включен
            Application.targetFrameRate = -1;
        }
        else
        {
            QualitySettings.vSyncCount = 0; // VSync выключен
            Application.targetFrameRate = foregroundFrameRate;
        }

        Debug.Log($"[RunInBackground] ✅ Фоновый режим: {(runInBackground ? "ВКЛЮЧЁН" : "ВЫКЛЮЧЕН")}");
        Debug.Log($"[RunInBackground] FPS: Активное окно={foregroundFrameRate}, Фоновое={backgroundFrameRate}");
    }

    void OnApplicationFocus(bool focus)
    {
        hasFocus = focus;

        if (!runInBackground)
            return;

        if (focus)
        {
            // Окно получило фокус - возвращаем нормальный FPS
            if (foregroundFrameRate == 0)
            {
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = -1;
            }
            else
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = foregroundFrameRate;
            }

            Debug.Log($"[RunInBackground] 🎯 Окно активно, FPS={foregroundFrameRate}");
        }
        else
        {
            // Окно потеряло фокус - снижаем FPS для экономии ресурсов
            if (backgroundFrameRate > 0)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = backgroundFrameRate;
                Debug.Log($"[RunInBackground] 💤 Окно в фоне, FPS={backgroundFrameRate}");
            }
        }
    }

    void OnGUI()
    {
        // Показываем статус в углу экрана (только в редакторе или при отладке)
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 12;
        style.normal.textColor = hasFocus ? Color.green : Color.yellow;

        string status = hasFocus ? "ACTIVE" : "BACKGROUND";
        string fpsInfo = $"FPS: {(int)(1f / Time.smoothDeltaTime)}";

        GUI.Label(new Rect(10, 10, 200, 40), $"{status}\n{fpsInfo}", style);
        #endif
    }
}
