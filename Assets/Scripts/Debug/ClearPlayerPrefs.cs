using UnityEngine;

/// <summary>
/// Утилита для очистки PlayerPrefs
/// Используйте в Inspector: ПКМ на скрипте → Clear All PlayerPrefs
/// </summary>
public class ClearPlayerPrefs : MonoBehaviour
{
    [Header("Информация")]
    [TextArea(3, 10)]
    [SerializeField] private string info =
        "Этот скрипт очищает все PlayerPrefs.\n" +
        "ПКМ на скрипте в Inspector → Clear All PlayerPrefs\n" +
        "Или нажмите кнопку в Inspector";

    /// <summary>
    /// Очистить все PlayerPrefs через Context Menu
    /// </summary>
    [ContextMenu("Clear All PlayerPrefs")]
    public void ClearAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("════════════════════════════════════════════");
        Debug.Log("✅ PlayerPrefs полностью очищены!");
        Debug.Log("Очищены данные:");
        Debug.Log("  - UserToken");
        Debug.Log("  - Username");
        Debug.Log("  - SelectedCharacterId");
        Debug.Log("  - SelectedCharacterClass");
        Debug.Log("  - CurrentRoomId");
        Debug.Log("  - И все остальные данные");
        Debug.Log("════════════════════════════════════════════");
        Debug.Log("⚠️ Теперь нужно заново войти в игру!");
        Debug.Log("════════════════════════════════════════════");
    }

    /// <summary>
    /// Очистить только данные авторизации
    /// </summary>
    [ContextMenu("Clear Auth Data Only")]
    public void ClearAuthOnly()
    {
        PlayerPrefs.DeleteKey("UserToken");
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.Save();
        Debug.Log("✅ Данные авторизации очищены (UserToken, Username)");
    }

    /// <summary>
    /// Очистить только данные персонажа
    /// </summary>
    [ContextMenu("Clear Character Data Only")]
    public void ClearCharacterOnly()
    {
        PlayerPrefs.DeleteKey("SelectedCharacterId");
        PlayerPrefs.DeleteKey("SelectedCharacterClass");
        PlayerPrefs.Save();
        Debug.Log("✅ Данные персонажа очищены (CharacterId, CharacterClass)");
    }

    /// <summary>
    /// Очистить только данные комнаты
    /// </summary>
    [ContextMenu("Clear Room Data Only")]
    public void ClearRoomOnly()
    {
        PlayerPrefs.DeleteKey("CurrentRoomId");
        PlayerPrefs.Save();
        Debug.Log("✅ Данные комнаты очищены (CurrentRoomId)");
    }

    /// <summary>
    /// Показать все сохранённые PlayerPrefs
    /// </summary>
    [ContextMenu("Show All PlayerPrefs")]
    public void ShowAll()
    {
        Debug.Log("════════════════════════════════════════════");
        Debug.Log("📋 ТЕКУЩИЕ PLAYERPREFS:");
        Debug.Log("════════════════════════════════════════════");

        ShowKey("UserToken");
        ShowKey("Username");
        ShowKey("SelectedCharacterId");
        ShowKey("SelectedCharacterClass");
        ShowKey("CurrentRoomId");

        Debug.Log("════════════════════════════════════════════");
    }

    private void ShowKey(string key)
    {
        if (PlayerPrefs.HasKey(key))
        {
            string value = PlayerPrefs.GetString(key, "");
            // Скрываем часть токена для безопасности
            if (key == "UserToken" && value.Length > 20)
            {
                value = value.Substring(0, 20) + "...";
            }
            Debug.Log($"  ✅ {key}: {value}");
        }
        else
        {
            Debug.Log($"  ❌ {key}: NOT SET");
        }
    }
}
