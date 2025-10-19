using UnityEngine;
using Network;

/// <summary>
/// Спавнер для Ice Nova - создает ледяные осколки, вылетающие радиально во все стороны
/// </summary>
public class IceNovaProjectileSpawner : MonoBehaviour
{
    [Header("Network Settings")]
    [SerializeField] private int skillId = 202; // Ice Nova skill ID
    [Header("Projectile Settings")]
    [SerializeField] private GameObject iceShardPrefab; // Префаб ледяного осколка
    [SerializeField] private int shardCount = 12; // Количество осколков
    [SerializeField] private float shardSpeed = 15f; // Скорость полета осколков
    [SerializeField] private float shardLifetime = 1.5f; // Время жизни осколка
    [SerializeField] private float radius = 8f; // Радиус действия
    [SerializeField] private float damage = 40f; // Урон каждого осколка

    [Header("Visual Settings")]
    [SerializeField] private float spawnHeight = 1f; // Высота создания осколков
    [SerializeField] private bool randomRotation = true; // Случайное вращение осколков

    [Header("Spawn Pattern")]
    [SerializeField] private bool spawnInRings = false; // Создавать кольцами (true) или равномерно (false)
    [SerializeField] private int ringCount = 2; // Количество колец (если spawnInRings = true)

    private void Start()
    {
        SpawnIceShards();
        // Уничтожаем спавнер после создания осколков
        Destroy(gameObject, 0.1f);
    }

    /// <summary>
    /// Создает ледяные осколки радиально
    /// </summary>
    private void SpawnIceShards()
    {
        if (iceShardPrefab == null)
        {
            Debug.LogError("[IceNovaSpawner] Ice shard prefab is not assigned!");
            return;
        }

        Vector3 spawnPosition = transform.position + Vector3.up * spawnHeight;
        float angleStep = 360f / shardCount;

        for (int i = 0; i < shardCount; i++)
        {
            // Вычисляем угол для этого осколка
            float angle = i * angleStep;

            // Добавляем небольшую случайность к углу
            if (randomRotation)
            {
                angle += Random.Range(-angleStep * 0.2f, angleStep * 0.2f);
            }

            // Вычисляем направление
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            // Создаем осколок
            GameObject shard = Instantiate(iceShardPrefab, spawnPosition, Quaternion.identity);

            // Поворачиваем осколок по направлению полета
            shard.transform.rotation = Quaternion.LookRotation(direction);

            // Настраиваем компонент Projectile
            Projectile projectile = shard.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(null, damage, direction, gameObject);
            }
            else
            {
                Debug.LogError($"[IceNovaSpawner] ❌ Ice shard prefab missing Projectile component!");
            }

            // СИНХРОНИЗАЦИЯ: Отправляем осколок на сервер для мультиплеера
            if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
            {
                SocketIOManager.Instance.SendProjectileSpawned(
                    skillId,
                    spawnPosition,
                    direction,
                    "" // targetSocketId - осколки не наводятся
                );
                Debug.Log($"[IceNovaSpawner] 📡 Ice shard {i + 1} sent to server: angle={angle:F1}°, dir={direction}");
            }

            Debug.Log($"[IceNovaSpawner] 🧊 Created ice shard {i + 1}/{shardCount} at angle {angle:F1}°");
        }

        Debug.Log($"[IceNovaSpawner] ❄️ Spawned {shardCount} ice shards!");
    }
}
