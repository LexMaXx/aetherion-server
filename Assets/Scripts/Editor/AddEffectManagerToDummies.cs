using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor скрипт для автоматического добавления EffectManager ко всем DummyEnemy в сцене
/// </summary>
public class AddEffectManagerToDummies
{
    [MenuItem("Aetherion/Utilities/Add EffectManager to All DummyEnemies")]
    public static void AddEffectManagerToAll()
    {
        // Находим все DummyEnemy в сцене
        DummyEnemy[] dummies = Object.FindObjectsOfType<DummyEnemy>();

        if (dummies.Length == 0)
        {
            Debug.LogWarning("[AddEffectManagerToDummies] Не найдено ни одного DummyEnemy в сцене!");
            return;
        }

        int addedCount = 0;
        int alreadyHadCount = 0;

        foreach (DummyEnemy dummy in dummies)
        {
            // Проверяем есть ли уже EffectManager
            if (dummy.GetComponent<EffectManager>() == null)
            {
                // Добавляем EffectManager
                dummy.gameObject.AddComponent<EffectManager>();
                addedCount++;
                Debug.Log($"[AddEffectManagerToDummies] ✅ Добавлен EffectManager на {dummy.gameObject.name}");
            }
            else
            {
                alreadyHadCount++;
                Debug.Log($"[AddEffectManagerToDummies] ⏭️ {dummy.gameObject.name} уже имеет EffectManager");
            }
        }

        // Итоговый отчёт
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"[AddEffectManagerToDummies] 📊 ИТОГО:");
        Debug.Log($"  Найдено DummyEnemy: {dummies.Length}");
        Debug.Log($"  ✅ Добавлено EffectManager: {addedCount}");
        Debug.Log($"  ⏭️ Уже было: {alreadyHadCount}");
        Debug.Log("═══════════════════════════════════════════════════════");

        if (addedCount > 0)
        {
            Debug.Log($"[AddEffectManagerToDummies] 💾 Не забудь сохранить сцену! (Ctrl+S)");
        }
    }
}
