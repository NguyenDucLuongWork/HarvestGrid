using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIGen
{
    // Needs a PanelRenderer (Component > UI Toolkit > Panel Renderer) with
    // PanelSettings + UXML assigned.
    [RequireComponent(typeof(PanelRenderer))]
    public abstract class BindedPanel : MonoBehaviour
    {
        static readonly Dictionary<Type, List<(MethodInfo method, string name)>> cache = new();

        // How bindings poll the (plain, non-notifying) entity. Applied to EVERY DataBinding
        // in the tree at bind time, so UXML (generated, AI-edited, hand-edited) needs no attribute.
        // EveryUpdate = always live. WhenDirty/OnSourceChanged = only when you call NotifyChanged/Refresh.
        [SerializeField] BindingUpdateTrigger updateTrigger = BindingUpdateTrigger.EveryUpdate;

        protected PanelRenderer panel;
        protected VisualElement root;
        readonly List<(Button btn, Action cb)> hooked = new();

        // subclass returns the entity (class, not struct)
        protected abstract object DataSource { get; }

        // Subclasses overriding Awake/OnDestroy must call base.
        protected virtual void Awake()
        {
            panel = GetComponent<PanelRenderer>();
            // fires on initial UI setup AND whenever the UXML changes (redesign hot-reload)
            panel.RegisterUIReloadCallback(OnUIReload);
        }

        protected virtual void OnDestroy()
        {
            if (panel != null) panel.UnregisterUIReloadCallback(OnUIReload);
            Unhook();
        }

        // Unity 6.5 signature has no 'version' param: (PanelRenderer r, VisualElement rootElement)
        void OnUIReload(PanelRenderer r, VisualElement rootElement, int version)
        {
            // Idempotent: the tree was rebuilt, so drop old handlers and bind again.
            Unhook();
            root = rootElement;
            Bind();
        }

        void Bind()
        {
            // 1) fields: DataBinding elements in UXML resolve against this
            root.dataSource = DataSource;
            ApplyUpdateTrigger();

            // 2) buttons: btn-<Name>  <->  [UIAction("<Name>")]
            foreach (var (method, actionName) in GetActions(GetType()))
            {
                var btn = root.Q<Button>("btn-" + actionName);
                if (btn == null)
                {
                    Debug.LogWarning($"[{name}] UXML missing Button 'btn-{actionName}'", this);
                    continue;
                }
                var cb = (Action)Delegate.CreateDelegate(typeof(Action), this, method);
                btn.clicked += cb;
                hooked.Add((btn, cb));
            }

            OnBound();
        }

        void ApplyUpdateTrigger()
        {
            void Apply(VisualElement ve)
            {
                foreach (var info in ve.GetBindingInfos())
                    if (info.binding is DataBinding db) db.updateTrigger = updateTrigger;
            }
            Apply(root);
            root.Query<VisualElement>().ForEach(Apply);
        }

        void Unhook()
        {
            foreach (var (btn, cb) in hooked) btn.clicked -= cb;
            hooked.Clear();
        }

        protected virtual void OnBound() { }

#if UNITY_EDITOR
        // Set by Editor/UxmlGenerator (runtime code can't reference the Editor assembly directly).
        public static Action<BindedPanel> EditorGenerateUxml;

        // Inspector: component header "⋮" menu (or right-click the component title) -> Generate UXML
        [ContextMenu("Generate UXML")]
        void GenerateUxml()
        {
            if (EditorGenerateUxml == null)
            {
                Debug.LogError("[UIGen] Editor generator not loaded (is Editor/UxmlGenerator.cs compiled?)", this);
                return;
            }
            EditorGenerateUxml(this);
        }
#endif

        // call after changing the entity from code if you don't use update-trigger="EveryUpdate"
        public void Refresh()
        {
            if (root == null) return;
            root.dataSource = null;
            root.dataSource = DataSource;
        }

        static List<(MethodInfo, string)> GetActions(Type t)
        {
            if (cache.TryGetValue(t, out var list)) return list;
            list = new();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var m in t.GetMethods(flags))
            {
                var a = m.GetCustomAttribute<UIActionAttribute>();
                if (a != null) list.Add((m, a.Name));
            }
            cache[t] = list;
            return list;
        }
    }


}