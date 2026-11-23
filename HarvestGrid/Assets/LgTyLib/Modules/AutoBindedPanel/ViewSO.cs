using UnityEngine;

namespace LgTyLib.Modules.AutoBindedPanel
{
    public abstract class ViewSO<T> : ScriptableObject
    {
        [SerializeField] private T data;
        public T Data => data;

        public void SetData(T newData) { data = newData; Sync(); }
        protected virtual void OnValidate() => Sync();
        public void Sync() => ViewSync.Sync(this, data, this);
    }
}