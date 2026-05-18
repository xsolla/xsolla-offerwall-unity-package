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
        /// Enqueue an action to be executed on the main thread.
        /// </summary>
        public static void Enqueue(Action action)
        {
            if (action == null) return;

            EnsureInstance();

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

        private static void EnsureInstance()
        {
            if (_instance != null) return;

            var go = new GameObject("[XsollaOfferwall.MainThreadDispatcher]");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            _instance = go.AddComponent<MainThreadDispatcher>();
        }
    }
}
