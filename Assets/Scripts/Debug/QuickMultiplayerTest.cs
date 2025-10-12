using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Быстрый тест мультиплеера БЕЗ логина
/// Просто устанавливает фейковые данные и загружает GameScene
/// ⚠️ ОТКЛЮЧЕН для production! Раскомментируйте RuntimeInitializeOnLoadMethod для тестирования
/// </summary>
public class QuickMultiplayerTest : MonoBehaviour
{
    // ОТКЛЮЧЕНО для production - убирает авто-создание TestPlayer
    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void QuickSetup()
    {
        // Устанавливаем фейковый токен (для теста)
        if (string.IsNullOrEmpty(PlayerPrefs.GetString("UserToken")))
        {
            PlayerPrefs.SetString("UserToken", "test_token_for_multiplayer");
            Debug.Log("[QuickTest] ✅ Установлен тестовый токен");
        }

        // Устанавливаем username
        if (string.IsNullOrEmpty(PlayerPrefs.GetString("Username")))
        {
            PlayerPrefs.SetString("Username", "TestPlayer" + Random.Range(1000, 9999));
            Debug.Log($"[QuickTest] ✅ Установлен username: {PlayerPrefs.GetString("Username")}");
        }

        // Устанавливаем класс персонажа
        if (string.IsNullOrEmpty(PlayerPrefs.GetString("SelectedCharacterClass")))
        {
            PlayerPrefs.SetString("SelectedCharacterClass", "Warrior");
            Debug.Log("[QuickTest] ✅ Установлен класс: Warrior");
        }

        PlayerPrefs.Save();
    }
}
