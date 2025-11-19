using UnityEngine;

/// <summary>
/// Диагностика: Показывает какой персонаж сохранён
/// </summary>
public class ShowSavedCharacter : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== ДИАГНОСТИКА СОХРАНЁННОГО ПЕРСОНАЖА ===");

        // 1. Проверка PlayerPrefs
        string savedClass = PlayerPrefs.GetString("SelectedCharacterClass", "НЕТ");
        Debug.Log($"📋 PlayerPrefs.SelectedCharacterClass: {savedClass}");

        // 2. Проверка GameProgressManager
        if (GameProgressManager.Instance != null)
        {
            GameObject savedPrefab = GameProgressManager.Instance.GetSelectedCharacterPrefab();
            if (savedPrefab != null)
            {
                Debug.Log($"📋 GameProgressManager.SelectedCharacter: {savedPrefab.name}");
            }
            else
            {
                Debug.LogWarning("⚠️ GameProgressManager.SelectedCharacter: НЕТ");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ GameProgressManager: НЕ НАЙДЕН");
        }

        Debug.Log("==========================================");
    }

    [ContextMenu("Show Saved Character")]
    public void ShowSaved()
    {
        Start();
    }
}
