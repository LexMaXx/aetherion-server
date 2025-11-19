using UnityEngine;

/// <summary>
/// АВТОМАТИЧЕСКОЕ ИСПРАВЛЕНИЕ Canvas sorting order при старте игры
/// Чтобы НЕ НУЖНО было каждый раз жать "Fix All Issues"!
/// </summary>
public class AutoFixCanvasOnStart : MonoBehaviour
{
    private void Start()
    {
        // Задержка 0.5 секунды чтобы все UI успели создаться
        Invoke(nameof(FixAllCanvasIssues), 0.5f);
    }

    private void FixAllCanvasIssues()
    {
        Debug.Log("🔧 [AutoFixCanvas] Автоматическое исправление Canvas sorting order...");

        // Найти все Canvas в сцене
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);

        Canvas skillBarCanvas = null;
        Canvas mobileControlsCanvas = null;

        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.name.Contains("SkillBar"))
            {
                skillBarCanvas = canvas;
            }
            else if (canvas.name.Contains("MobileControls"))
            {
                mobileControlsCanvas = canvas;
            }
        }

        // Исправить sorting order
        if (skillBarCanvas != null)
        {
            skillBarCanvas.sortingOrder = 10; // SkillBar сверху
            Debug.Log($"✅ [AutoFixCanvas] {skillBarCanvas.name} → sortingOrder = 10");
        }

        if (mobileControlsCanvas != null)
        {
            mobileControlsCanvas.sortingOrder = 5; // MobileControls снизу
            Debug.Log($"✅ [AutoFixCanvas] {mobileControlsCanvas.name} → sortingOrder = 5");
        }

        // Убедиться что у всех Image в SkillBar включен Raycast Target
        if (skillBarCanvas != null)
        {
            UnityEngine.UI.Image[] images = skillBarCanvas.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            int fixedCount = 0;
            foreach (UnityEngine.UI.Image img in images)
            {
                if (!img.raycastTarget && img.gameObject.name.Contains("Icon"))
                {
                    img.raycastTarget = true;
                    fixedCount++;
                }
            }
            if (fixedCount > 0)
            {
                Debug.Log($"✅ [AutoFixCanvas] Включён Raycast Target для {fixedCount} иконок скиллов");
            }
        }

        Debug.Log("✅ [AutoFixCanvas] Все исправления применены автоматически!");
    }
}
