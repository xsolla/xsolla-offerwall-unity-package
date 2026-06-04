using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xsolla.Offerwall
{
    /// <summary>
    /// Dispatches actions to Unity's main thread.
    /// Native callbacks arrive on background threads — this ensures
    /// C# callbacks execute on the main thread where Unity API calls are safe.
    /// </summary>
    internal class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher _instance;
        private static readonly Queue<Action> _queue = new Queue<Action>();
        private static readonly object _lock = new object();

        /// <summary>
        /// Automatically initialises the dispatcher on the main thread at app startup,
        /// before any scene is loaded. This prevents the race condition where a native
        /// background callback (e.g. onClosed from Android) calls Enqueue() before the
        /// instance exists and tries to create a GameObject off the main thread.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_instance != null) return;

            var go = new GameObject("[XsollaOfferwall.MainThreadDispatcher]");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            _instance = go.AddComponent<MainThreadDispatcher>();
        }

        /// <summary>
        /// Enqueue an action to be executed on the main thread.
        /// Safe to call from any thread.
        /// </summary>
        public static void Enqueue(Action action)
        {
            if (action == null) return;

            lock (_lock)
            {
                _queue.Enqueue(action);
            }
        }

        private void Update()
        {
            lock (_lock)
            {
                while (_queue.Count > 0)
                {
                    var action = _queue.Dequeue();
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }
    }
}
