using UnityEngine;

/// <summary>
/// Автоматическая очистка камеры от старых компонентов при загрузке Arena сцены
/// Добавь этот компонент на Main Camera в Arena сцене
/// </summary>
public class CameraCleanup : MonoBehaviour
{
    void Awake()
    {
        // Удаляем все старые CameraFollow компоненты
        CameraFollow[] oldFollows = GetComponents<CameraFollow>();
        foreach (CameraFollow cf in oldFollows)
        {
            Debug.Log("✓ [CameraCleanup] Удален старый CameraFollow");
            Destroy(cf);
        }

        // Удаляем этот скрипт после очистки (больше не нужен)
        Destroy(this);
    }
}
