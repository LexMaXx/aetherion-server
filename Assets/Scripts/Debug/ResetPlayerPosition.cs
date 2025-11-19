using UnityEngine;

/// <summary>
/// Утилита для сброса сохранённой позиции персонажа на карте мира
/// Полезно для тестирования дефолтной позиции спавна
/// </summary>
public class ResetPlayerPosition : MonoBehaviour
{
    [ContextMenu("Reset Saved Position")]
    public void ResetPosition()
    {
        PlayerPrefs.DeleteKey("WorldMap_PlayerX");
        PlayerPrefs.DeleteKey("WorldMap_PlayerY");
        PlayerPrefs.DeleteKey("WorldMap_PlayerZ");
        PlayerPrefs.Save();

        Debug.Log("[ResetPlayerPosition] ✅ Сохранённая позиция удалена!");
        Debug.Log("[ResetPlayerPosition] 📍 При следующем запуске персонаж появится на дефолтной позиции");
    }

    [ContextMenu("Show Current Saved Position")]
    public void ShowSavedPosition()
    {
        if (PlayerPrefs.HasKey("WorldMap_PlayerX"))
        {
            float x = PlayerPrefs.GetFloat("WorldMap_PlayerX");
            float z = PlayerPrefs.GetFloat("WorldMap_PlayerZ");

            Debug.Log($"[ResetPlayerPosition] 📍 Сохранённая позиция (X, Z): ({x}, {z})");
            Debug.Log($"[ResetPlayerPosition] 📍 Y координата будет определена по terrain при спавне");
        }
        else
        {
            Debug.Log("[ResetPlayerPosition] ⚠️ Сохранённой позиции нет");
        }
    }
}
