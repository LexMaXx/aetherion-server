using UnityEngine;
using UnityEditor;

/// <summary>
/// Утилита для очистки PlayerPrefs и сброса скиллов
/// </summary>
public class ClearPlayerPrefsSkills : MonoBehaviour
{
    [MenuItem("Aetherion/Debug/Clear Equipped Skills PlayerPrefs")]
    public static void ClearSkills()
    {
        PlayerPrefs.DeleteKey("EquippedSkills");
        PlayerPrefs.Save();
        Debug.Log("[ClearPlayerPrefsSkills] ✅ PlayerPrefs 'EquippedSkills' очищены!");
        Debug.Log("[ClearPlayerPrefsSkills] 💡 Теперь при спавне в арене будут автоэкипированы все 5 скиллов класса");
    }

    [MenuItem("Aetherion/Debug/Show Current Equipped Skills")]
    public static void ShowEquippedSkills()
    {
        string equipJson = PlayerPrefs.GetString("EquippedSkills", "");
        if (string.IsNullOrEmpty(equipJson))
        {
            Debug.Log("[ClearPlayerPrefsSkills] ℹ️ EquippedSkills пуст в PlayerPrefs");
        }
        else
        {
            Debug.Log($"[ClearPlayerPrefsSkills] 📄 EquippedSkills JSON:\n{equipJson}");
        }

        string selectedClass = PlayerPrefs.GetString("SelectedCharacterClass", "");
        Debug.Log($"[ClearPlayerPrefsSkills] 🎭 SelectedCharacterClass: {selectedClass}");
    }
}
