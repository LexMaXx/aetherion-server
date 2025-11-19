using UnityEngine;

/// <summary>
/// Очистка WorldMapScene от лишних объектов с тегом Player
/// ВАЖНО: Удаляет все Player объекты перед спавном персонажа из GameProgressManager
/// Добавьте этот компонент на WorldMapManager
/// </summary>
public class CleanupWorldMapScene : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Удалять ли объекты с тегом Player при старте сцены")]
    [SerializeField] private bool cleanupOnStart = true;

    [Tooltip("Исключения - не удалять эти объекты (по имени)")]
    [SerializeField] private string[] exceptions = new string[0];

    void Awake()
    {
        if (cleanupOnStart)
        {
            CleanupPlayerObjects();
        }
    }

    /// <summary>
    /// Удалить все GameObject с тегом Player из сцены
    /// (Capsule, старые префабы и т.д.)
    /// </summary>
    [ContextMenu("Cleanup Player Objects")]
    public void CleanupPlayerObjects()
    {
        Debug.Log("[CleanupWorldMapScene] 🧹 Очистка сцены от старых Player объектов...");

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length == 0)
        {
            Debug.Log("[CleanupWorldMapScene] ✅ Сцена чистая - Player объектов не найдено");
            return;
        }

        int removedCount = 0;
        int skippedCount = 0;

        foreach (GameObject player in players)
        {
            // Проверяем исключения
            bool isException = false;
            foreach (string exception in exceptions)
            {
                if (!string.IsNullOrEmpty(exception) && player.name.Contains(exception))
                {
                    isException = true;
                    break;
                }
            }

            if (isException)
            {
                Debug.Log($"[CleanupWorldMapScene] ⏭️ Пропущен (исключение): {player.name}");
                skippedCount++;
                continue;
            }

            // Удаляем объект
            Debug.Log($"[CleanupWorldMapScene] 🗑️ Удаляю: {player.name} (Tag: {player.tag})");
            Destroy(player);
            removedCount++;
        }

        Debug.Log($"[CleanupWorldMapScene] ✅ Очистка завершена! Удалено: {removedCount}, Пропущено: {skippedCount}");
    }

    /// <summary>
    /// Проверить наличие Player объектов в сцене
    /// </summary>
    [ContextMenu("Check Player Objects")]
    public void CheckPlayerObjects()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        Debug.Log($"[CleanupWorldMapScene] 🔍 Найдено Player объектов: {players.Length}");

        foreach (GameObject player in players)
        {
            Debug.Log($"[CleanupWorldMapScene]   - {player.name} (Position: {player.transform.position})");
        }
    }
}
