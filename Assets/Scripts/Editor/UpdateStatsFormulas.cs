using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor скрипт для обновления StatsFormulas.asset с новыми значениями HP
/// </summary>
public class UpdateStatsFormulas : EditorWindow
{
    [MenuItem("Tools/Update Stats Formulas (HP x10)")]
    public static void UpdateFormulas()
    {
        // Загружаем StatsFormulas asset
        StatsFormulas formulas = Resources.Load<StatsFormulas>("StatsFormulas");

        if (formulas == null)
        {
            Debug.LogError("[UpdateStatsFormulas] ❌ StatsFormulas.asset не найден в Resources!");
            EditorUtility.DisplayDialog("Error", "StatsFormulas.asset не найден в Resources/", "OK");
            return;
        }

        Debug.Log("[UpdateStatsFormulas] 📊 Найден StatsFormulas.asset");
        Debug.Log($"[UpdateStatsFormulas] Старые значения: baseHealth={formulas.baseHealth}, enduranceHealthBonus={formulas.enduranceHealthBonus}");

        // Обновляем значения HP (x10)
        formulas.baseHealth = 1000f;              // Было 100
        formulas.enduranceHealthBonus = 200f;     // Было 20

        Debug.Log($"[UpdateStatsFormulas] Новые значения: baseHealth={formulas.baseHealth}, enduranceHealthBonus={formulas.enduranceHealthBonus}");

        // Сохраняем изменения
        EditorUtility.SetDirty(formulas);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[UpdateStatsFormulas] ✅ StatsFormulas.asset обновлён!");
        Debug.Log("[UpdateStatsFormulas] 📊 MaxHealth теперь: 1000 + (Endurance * 200) = 3000 для Endurance 10");

        EditorUtility.DisplayDialog(
            "Success!",
            "StatsFormulas.asset обновлён!\n\n" +
            "baseHealth: 100 → 1000\n" +
            "enduranceHealthBonus: 20 → 200\n\n" +
            "MaxHealth = 1000 + (Endurance * 200)\n" +
            "Для Endurance 10 = 3000 HP\n\n" +
            "Теперь запусти игру и проверь логи!",
            "OK"
        );
    }
}
