using UnityEngine;

/// <summary>
/// Помощник для AcceptButton - находит WorldMapPlayerController и вызывает метод
/// Подключите этот компонент на AcceptButton и привяжите OnAcceptButtonClick() в Inspector
/// </summary>
public class AcceptButtonHelper : MonoBehaviour
{
    /// <summary>
    /// Публичный метод для подключения в Inspector через Button.onClick
    /// </summary>
    public void OnAcceptButtonClick()
    {
        Debug.Log("[AcceptButtonHelper] 🔘 Кнопка нажата!");

        // Ищем WorldMapPlayerController в сцене
        WorldMapPlayerController player = FindObjectOfType<WorldMapPlayerController>();

        if (player != null)
        {
            Debug.Log("[AcceptButtonHelper] ✅ WorldMapPlayerController найден, вызываю OnAcceptButtonPressed()");
            player.OnAcceptButtonPressed();
        }
        else
        {
            Debug.LogError("[AcceptButtonHelper] ❌ WorldMapPlayerController не найден в сцене!");
            Debug.LogError("[AcceptButtonHelper] 💡 Убедитесь что персонаж заспавнился (ForceSpawnCharacter должен создать его)");
        }
    }
}
