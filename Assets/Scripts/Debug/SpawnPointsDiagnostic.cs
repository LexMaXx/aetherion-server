using UnityEngine;

/// <summary>
/// ДИАГНОСТИЧЕСКИЙ СКРИПТ: Показывает координаты всех spawn points
/// Поместите этот скрипт на NetworkSyncManager или BattleSceneManager
/// </summary>
public class SpawnPointsDiagnostic : MonoBehaviour
{
    [Header("Диагностика Spawn Points")]
    [Tooltip("Контейнер со spawn points (SpawnPoints или MultiplayerSpawnPoints)")]
    public Transform spawnPointsContainer;

    [ContextMenu("Print All Spawn Points")]
    void PrintAllSpawnPoints()
    {
        if (spawnPointsContainer == null)
        {
            Debug.LogError("[SpawnDiag] ❌ spawnPointsContainer не назначен! Перетащите GameObject 'SpawnPoints' в Inspector");
            return;
        }

        Debug.LogError($"[SpawnDiag] 🔥🔥🔥 === SPAWN POINTS COORDINATES ===");
        Debug.LogError($"[SpawnDiag] 🔥 Container: {spawnPointsContainer.name}");
        Debug.LogError($"[SpawnDiag] 🔥 Total points: {spawnPointsContainer.childCount}");
        Debug.LogError($"[SpawnDiag] 🔥🔥🔥 ================================");

        for (int i = 0; i < spawnPointsContainer.childCount; i++)
        {
            Transform point = spawnPointsContainer.GetChild(i);
            Vector3 pos = point.position;

            Debug.LogError($"[SpawnDiag] 🔥 [{i}] {point.name} = ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})");
        }

        Debug.LogError($"[SpawnDiag] 🔥🔥🔥 ================================");
        Debug.LogError($"[SpawnDiag] ✅ Скопируйте эти координаты в Server/server.js SPAWN_POINTS массив!");
    }

    void OnEnable()
    {
        // Автоматически найти SpawnPoints если не назначен
        if (spawnPointsContainer == null)
        {
            GameObject container = GameObject.Find("SpawnPoints");
            if (container == null)
                container = GameObject.Find("MultiplayerSpawnPoints");

            if (container != null)
            {
                spawnPointsContainer = container.transform;
                Debug.Log($"[SpawnDiag] ✅ Автоматически найден контейнер: {container.name}");
            }
        }
    }

    void Start()
    {
        // Автоматически печатаем координаты при старте сцены
        if (spawnPointsContainer != null)
        {
            PrintAllSpawnPoints();
        }
        else
        {
            Debug.LogError("[SpawnDiag] ❌ SpawnPoints контейнер НЕ НАЙДЕН! Создайте GameObject 'SpawnPoints' с 20 дочерними точками");
        }
    }
}
