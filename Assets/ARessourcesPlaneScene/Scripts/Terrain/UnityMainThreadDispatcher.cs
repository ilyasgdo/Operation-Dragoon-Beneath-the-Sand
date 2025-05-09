using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// UnityMainThreadDispatcher permet d'exécuter des actions sur le thread principal d'Unity
/// depuis d'autres threads.
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static UnityMainThreadDispatcher _instance = null;

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
        return _instance;
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void Update()
    {
        lock(_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    /// <summary>
    /// Ajoute une action à la file d'attente pour exécution sur le thread principal
    /// </summary>
    /// <param name="action">L'action à exécuter sur le thread principal</param>
    public void Enqueue(Action action)
    {
        lock(_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    /// <summary>
    /// Exécute une fonction sur le thread principal et attend son résultat
    /// </summary>
    public async Task<T> EnqueueAsync<T>(Func<T> func)
    {
        TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
        
        Enqueue(() => {
            try
            {
                var result = func();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        return await tcs.Task;
    }

    /// <summary>
    /// Exécute une action sur le thread principal et attend qu'elle soit terminée
    /// </summary>
    public async Task EnqueueAsync(Action action)
    {
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        
        Enqueue(() => {
            try
            {
                action();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        await tcs.Task;
    }
} 