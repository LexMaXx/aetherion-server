using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Пул объектов для повторного использования (избегаем Instantiate/Destroy)
/// Критично для производительности на мобильных устройствах
/// </summary>
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int initialSize = 10;
        public int maxSize = 50;
        public bool autoExpand = true;
    }

    [Header("Pools Configuration")]
    [SerializeField] private List<Pool> pools = new List<Pool>();

    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, Pool> poolConfigs;
    private Dictionary<string, int> poolCounts; // Отслеживание количества активных объектов

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePools();
    }

    void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        poolConfigs = new Dictionary<string, Pool>();
        poolCounts = new Dictionary<string, int>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            // Создаем начальное количество объектов
            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject obj = CreateNewObject(pool);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
            poolConfigs.Add(pool.tag, pool);
            poolCounts.Add(pool.tag, 0);

            Debug.Log($"[ObjectPool] Initialized pool '{pool.tag}' with {pool.initialSize} objects");
        }
    }

    GameObject CreateNewObject(Pool pool)
    {
        GameObject obj = Instantiate(pool.prefab, transform);
        obj.name = $"{pool.prefab.name}_Pooled";
        obj.SetActive(false);
        return obj;
    }

    /// <summary>
    /// Получить объект из пула
    /// </summary>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"[ObjectPool] Pool with tag '{tag}' doesn't exist!");
            return null;
        }

        GameObject objectToSpawn = null;

        // Если в пуле есть свободные объекты
        if (poolDictionary[tag].Count > 0)
        {
            objectToSpawn = poolDictionary[tag].Dequeue();
        }
        // Если пул пустой, но можно расширить
        else if (poolConfigs[tag].autoExpand)
        {
            Pool config = poolConfigs[tag];

            // Проверяем, не превышен ли максимальный размер
            if (poolCounts[tag] < config.maxSize)
            {
                objectToSpawn = CreateNewObject(config);
                Debug.LogWarning($"[ObjectPool] Pool '{tag}' expanded! Active: {poolCounts[tag] + 1}/{config.maxSize}");
            }
            else
            {
                Debug.LogWarning($"[ObjectPool] Pool '{tag}' reached max size ({config.maxSize})!");
                return null;
            }
        }
        else
        {
            Debug.LogWarning($"[ObjectPool] Pool '{tag}' is empty and auto-expand is disabled!");
            return null;
        }

        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);

        poolCounts[tag]++;

        // Вызываем интерфейс IPooledObject если есть
        IPooledObject pooledObj = objectToSpawn.GetComponent<IPooledObject>();
        if (pooledObj != null)
        {
            pooledObj.OnObjectSpawn();
        }

        return objectToSpawn;
    }

    /// <summary>
    /// Вернуть объект в пул
    /// </summary>
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"[ObjectPool] Pool with tag '{tag}' doesn't exist! Destroying object.");
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        obj.transform.SetParent(transform);
        poolDictionary[tag].Enqueue(obj);

        poolCounts[tag]--;
    }

    /// <summary>
    /// Автоматически вернуть объект в пул через время
    /// </summary>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation, float lifetime)
    {
        GameObject obj = SpawnFromPool(tag, position, rotation);
        if (obj != null)
        {
            StartCoroutine(ReturnAfterTime(tag, obj, lifetime));
        }
        return obj;
    }

    private System.Collections.IEnumerator ReturnAfterTime(string tag, GameObject obj, float time)
    {
        yield return new WaitForSeconds(time);

        if (obj != null && obj.activeInHierarchy)
        {
            ReturnToPool(tag, obj);
        }
    }

    /// <summary>
    /// Получить статистику пула
    /// </summary>
    public void GetPoolStats(string tag, out int available, out int active, out int total)
    {
        if (poolDictionary.ContainsKey(tag))
        {
            available = poolDictionary[tag].Count;
            active = poolCounts[tag];
            total = available + active;
        }
        else
        {
            available = active = total = 0;
        }
    }
}

/// <summary>
/// Интерфейс для объектов, которые могут быть переиспользованы из пула
/// </summary>
public interface IPooledObject
{
    void OnObjectSpawn();
}
