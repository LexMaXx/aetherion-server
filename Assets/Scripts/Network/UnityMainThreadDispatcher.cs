using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Unity Main Thread Dispatcher - выполняет действия в главном потоке Unity
/// Необходим для SocketIOManager, так как Socket.IO callbacks приходят в фоновом потоке
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance = null;
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    /// <summary>
    /// Singleton Instance
    /// </summary>
    public static UnityMainThreadDispatcher Instance()
    {
        if (!Exists())
        {
            // Create instance if it doesn't exist
            GameObject go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
            Debug.Log("[MainThreadDispatcher] ✅ Created");
        }

        return _instance;
    }

    /// <summary>
    /// Check if instance exists
    /// </summary>
    public static bool Exists()
    {
        return _instance != null;
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Execute all queued actions in the main thread
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                Action action = _executionQueue.Dequeue();
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MainThreadDispatcher] Error executing action: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
    }

    /// <summary>
    /// Enqueue an action to be executed on the main thread
    /// </summary>
    public void Enqueue(Action action)
    {
        if (action == null)
        {
            Debug.LogWarning("[MainThreadDispatcher] Attempted to enqueue null action");
            return;
        }

        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    /// <summary>
    /// Enqueue an IEnumerator to be started as a coroutine on the main thread
    /// </summary>
    public void EnqueueCoroutine(IEnumerator coroutine)
    {
        if (coroutine == null)
        {
            Debug.LogWarning("[MainThreadDispatcher] Attempted to enqueue null coroutine");
            return;
        }

        Enqueue(() =>
        {
            StartCoroutine(coroutine);
        });
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
