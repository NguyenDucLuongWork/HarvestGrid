using System;
using UnityEngine;

namespace LgTyLib.Modules.DataPersistence
{
    public abstract class DataHolder<T> : ScriptableObject where T : class
    {
        [SerializeField] private T data;           // visible in Inspector
        public T Data => data;
        public bool HasData => data != null;

        public event Action<T> OnDataChanged;

        internal void Set(T newData)
        {
            data = newData;
            OnDataChanged?.Invoke(data);
        }

        internal void Clear() => data = null;

        // Prevent Editor play-mode data from lingering in the asset
        private void OnEnable() { hideFlags = HideFlags.DontUnloadUnusedAsset; }
    }
}
