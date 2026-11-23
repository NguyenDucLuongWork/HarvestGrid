using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace LgTyLib.Modules.AutoBindedPanel
{
    // Plain C# sub-panel. One instance per pooled row, created in makeItem.
    public abstract class BindedView
    {
        public VisualElement Root { get; private set; }
        public BindedPanel Owner { get; private set; }
        public int Index { get; private set; }
        protected object Data { get; private set; }

        readonly List<(Button btn, Action cb)> hooked = new();

        public bool HasData => Data != null;
        internal virtual void SyncFields() { }

        internal void Attach(BindedPanel owner, VisualElement root)
        {
            Owner = owner; Root = root;
            root.userData = this;
            HookActions();
            OnAttached();
        }

        // called from ListView.bindItem every time the pooled row shows a different item
        internal void SetData(int index, object data)
        {
            Index = index; Data = data;
            SyncFields();      // fill the [UIField.Sync] fields before OnBind uses them
            OnBind();
        }

        void HookActions()
        {
            const BindingFlags f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var m in GetType().GetMethods(f))
            {
                var a = m.GetCustomAttribute<UIActionAttribute>();
                if (a == null) continue;
                var btn = Root.Q<Button>("btn-" + a.Name);
                if (btn == null) { Debug.LogWarning($"[{GetType().Name}] Missing Button 'btn-{a.Name}'"); continue; }
                var cb = (Action)Delegate.CreateDelegate(typeof(Action), this, m);
                btn.clicked += cb;
                hooked.Add((btn, cb));
            }
        }

        protected virtual void OnAttached() { }   // once per element: cache Q<> lookups here
        protected virtual void OnBind() { }       // every rebind: item changed
    }

    public abstract class BindedView<T> : BindedView where T : class
    {
        protected T Item => (T)Data;
        internal override void SyncFields() => ViewSync.Sync(this, Item, Owner);
    }
}