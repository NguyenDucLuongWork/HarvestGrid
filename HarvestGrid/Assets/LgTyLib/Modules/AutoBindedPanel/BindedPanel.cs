using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace LgTyLib.Modules.AutoBindedPanel
{
    // Needs a PanelRenderer (Component > UI Toolkit > Panel Renderer) with
    // PanelSettings + UXML assigned.
    [RequireComponent(typeof(PanelRenderer))]
    public abstract class BindedPanel : MonoBehaviour
    {
        sealed class ListBinding
        {
            public ListView view;
            public Func<IList> get;
            public int count;
            public readonly List<BindedView> views = new();   // pooled rows only, so bounded by what's visible
        }
        readonly List<ListBinding> lists = new();

        // override to react to row clicks
        protected virtual void OnListItemChosen(string listName, int index, object item) { }
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
            ApplyUpdateTrigger(root);
            BindLists();
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

        void ApplyUpdateTrigger(VisualElement scope)
        {
            void Apply(VisualElement ve)
            {
                foreach (var info in ve.GetBindingInfos())
                    if (info.binding is DataBinding db) db.updateTrigger = updateTrigger;
            }
            Apply(scope);
            scope.Query<VisualElement>().ForEach(Apply);
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

        // Lists are declared on the DataSource with [UIField.List(Template = ..., View = ...)]
        void BindLists()
        {
            var src = DataSource;
            if (src == null) return;
            const BindingFlags f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var m in src.GetType().GetMembers(f))
            {
                var a = m.GetCustomAttribute<UIField.ListAttribute>();
                if (a == null) continue;

                var lv = root.Q<ListView>(a.ListName ?? "list-" + m.Name);
                if (lv == null) { Debug.LogWarning($"[{name}] No ListView for '{m.Name}'", this); continue; }

                // template from the SO member, falling back to item-template set in UXML
                var tpl = GetValue(src, a.Template) as VisualTreeAsset ?? lv.itemTemplate;
                if (tpl == null) { Debug.LogWarning($"[{name}] No template for '{m.Name}'", this); continue; }

                var memberName = m.Name;
                var viewType = a.View;   // optional BindedView subclass
                var lb = new ListBinding { view = lv, get = () => GetValue(DataSource, memberName) as IList };

                lv.makeItem = () =>
                {
                    var e = tpl.Instantiate();
                    ApplyUpdateTrigger(e);
                    if (viewType != null)
                    {
                        var v = (BindedView)Activator.CreateInstance(viewType);
                        v.Attach(this, e);
                        lb.views.Add(v);
                    }
                    return e;
                };

                // runs each time a pooled row shows another item
                lv.bindItem = (e, i) =>
                {
                    if (e.userData is BindedView v)
                    {
                        v.SetData(i, lv.itemsSource[i]);
                        e.dataSource = v;               // row bindings resolve against the view's fields
                    }
                };

                // with a view class the row source is the view (set above); without one it's the raw item
                lv.bindingSourceSelectionMode = viewType != null
                    ? BindingSourceSelectionMode.Manual
                    : BindingSourceSelectionMode.AutoAssign;

                lv.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
                lv.itemsChosen += items =>
                {
                    var i = lv.selectedIndex;
                    foreach (var it in items) OnListItemChosen(memberName, i, it);
                };

                lists.Add(lb);
            }
            SyncLists(true);
        }

        static object GetValue(object o, string member)
        {
            if (o == null || string.IsNullOrEmpty(member)) return null;
            const BindingFlags f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var t = o.GetType();
            return t.GetField(member, f)?.GetValue(o) ?? t.GetProperty(member, f)?.GetValue(o);
        }

        // Poll for list changes: new reference -> reassign, count change -> RefreshItems
        void SyncLists(bool force = false)
        {
            foreach (var l in lists)
            {
                var cur = l.get();
                if (force || !ReferenceEquals(cur, l.view.itemsSource))
                {
                    l.view.itemsSource = cur;
                    l.count = cur?.Count ?? 0;
                }
                else if ((cur?.Count ?? 0) != l.count)
                {
                    l.count = cur?.Count ?? 0;
                    l.view.RefreshItems();
                }
            }
        }

        void Unhook()
        {
            foreach (var (btn, cb) in hooked) btn.clicked -= cb;
            hooked.Clear();
            lists.Clear();
        }

        public void Refresh()
        {
            if (root == null) return;
            root.dataSource = null;
            root.dataSource = DataSource;
            SyncLists();
        }

        protected virtual void LateUpdate()
        {
            if (updateTrigger != BindingUpdateTrigger.EveryUpdate) return;
            SyncLists();
            SyncRows();
        }

        void SyncRows()
        {
            foreach (var l in lists)
                foreach (var v in l.views)
                    if (v.HasData) v.SyncFields();
        }

        public void RefreshRows() => SyncRows();   // for WhenDirty / OnSourceChanged modes
    }
}