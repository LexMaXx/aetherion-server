using UnityEngine;

/// <summary>
/// Helper компонент для синхронизации снарядов в мультиплеере
/// Добавляется автоматически на локально созданные снаряды
/// </summary>
public class ProjectileSyncHelper : MonoBehaviour
{
    private bool isSynced = false;

    public void SyncToServer(int skillId, Vector3 spawnPos, Vector3 direction, string targetSocketId)
    {
        if (isSynced) return;

        SocketIOManager socketIO = SocketIOManager.Instance;
        if (socketIO != null && socketIO.IsConnected)
        {
            socketIO.SendProjectileSpawned(skillId, spawnPos, direction, targetSocketId);
            Debug.Log($"[ProjectileSync] 🚀 Снаряд отправлен на сервер: skillId={skillId}");
            isSynced = true;
        }
    }
}
