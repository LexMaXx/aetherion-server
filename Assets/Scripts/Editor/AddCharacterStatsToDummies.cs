using UnityEngine;
using UnityEditor;

/// <summary>
/// Утилита для добавления CharacterStats ко всем DummyEnemy в сцене
/// Необходимо для поддержки эффектов на характеристики (Perception, Attack, Defense)
/// </summary>
public class AddCharacterStatsToDummies : MonoBehaviour
{
    [MenuItem("Aetherion/Utilities/Add CharacterStats to All DummyEnemies")]
    public static void AddCharacterStatsToAll()
    {
        // Находим всех DummyEnemy в сцене
        DummyEnemy[] dummies = Object.FindObjectsOfType<DummyEnemy>();

        if (dummies.Length == 0)
        {
            Debug.LogWarning("[AddCharacterStatsToDummies] No DummyEnemy found in scene!");
            return;
        }

        int added = 0;
        int alreadyHad = 0;

        foreach (DummyEnemy dummy in dummies)
        {
            CharacterStats stats = dummy.GetComponent<CharacterStats>();

            if (stats == null)
            {
                // Добавляем CharacterStats
                stats = dummy.gameObject.AddComponent<CharacterStats>();
                stats.perception = 5; // Среднее восприятие
                added++;
                Debug.Log($"[AddCharacterStatsToDummies] ✅ Добавлен CharacterStats к {dummy.gameObject.name} (Perception: 5)");
            }
            else
            {
                alreadyHad++;
                Debug.Log($"[AddCharacterStatsToDummies] ⏭️ {dummy.gameObject.name} уже имеет CharacterStats (Perception: {stats.perception})");
            }
        }

        Debug.Log($"[AddCharacterStatsToDummies] ✅ Обработано DummyEnemy: {dummies.Length}");
        Debug.Log($"[AddCharacterStatsToDummies] ➕ Добавлено CharacterStats: {added}");
        Debug.Log($"[AddCharacterStatsToDummies] ⏭️ Уже было CharacterStats: {alreadyHad}");
    }
}
