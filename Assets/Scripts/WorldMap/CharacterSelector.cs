using UnityEngine;

/// <summary>
/// Автоматически сохраняет выбранного персонажа в GameProgressManager
/// Добавьте на GameObject персонажа в BattleScene
/// </summary>
public class CharacterSelector : MonoBehaviour
{
    [Header("Character Settings")]
    [Tooltip("Имя префаба персонажа (должно совпадать с именем в Resources/Characters/)")]
    [SerializeField] private string characterPrefabName = "";

    [Tooltip("Автоматически использовать имя GameObject если prefabName не указан")]
    [SerializeField] private bool useGameObjectName = true;

    void Start()
    {
        RegisterCharacter();
    }

    /// <summary>
    /// Зарегистрировать персонажа в GameProgressManager
    /// </summary>
    private void RegisterCharacter()
    {
        // Определяем имя префаба
        string prefabName = characterPrefabName;

        if (string.IsNullOrEmpty(prefabName) && useGameObjectName)
        {
            // Используем имя GameObject
            prefabName = gameObject.name;

            // Убираем "(Clone)" если есть
            prefabName = prefabName.Replace("(Clone)", "").Trim();
        }

        if (string.IsNullOrEmpty(prefabName))
        {
            Debug.LogWarning($"[CharacterSelector] Имя персонажа не указано на '{gameObject.name}'!");
            return;
        }

        // Сохраняем в GameProgressManager
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.SetSelectedCharacter(prefabName);
            Debug.Log($"[CharacterSelector] ✅ Персонаж '{prefabName}' зарегистрирован");
        }
        else
        {
            Debug.LogWarning("[CharacterSelector] GameProgressManager не найден! Персонаж не сохранён.");
        }
    }

    /// <summary>
    /// Принудительно зарегистрировать персонажа (можно вызвать вручную)
    /// </summary>
    public void RegisterManually()
    {
        RegisterCharacter();
    }

    /// <summary>
    /// Установить имя префаба вручную
    /// </summary>
    public void SetCharacterPrefabName(string name)
    {
        characterPrefabName = name;
        RegisterCharacter();
    }
}
