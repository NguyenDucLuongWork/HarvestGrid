using UnityEngine;

namespace LgTyLib.Core
{
    public abstract class BaseSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _isQuitting = false;

        public static T Instance
        {
            get
            {
                if (_isQuitting)
                {
                    Debug.LogWarning($"[BaseSingleton] Instance of {typeof(T)} requested while quitting. Returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindAnyObjectByType<T>();

                        if (_instance == null)
                            Debug.LogError($"[BaseSingleton] No instance of {typeof(T)} found in the scene.");
#if UNITY_EDITOR
                        else
                        {
                            T[] instances = FindObjectsByType<T>();
                            if (instances.Length > 1)
                                Debug.LogError($"[BaseSingleton] Multiple instances of {typeof(T)} detected.");
                        }
#endif
                    }
                    return _instance;
                }
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                _isQuitting = false;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
            }
            else if (_instance != this)
            {
                Debug.LogError($"[BaseSingleton] Duplicate instance of {typeof(T)} on {gameObject.name}. Destroying.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
        }

    }
}