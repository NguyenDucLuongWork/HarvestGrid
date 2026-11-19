#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UIGen.Editor
{
    public static class UxmlGenerator
    {
        const string BeginMarker = "<!-- generated:begin -->";
        const string EndMarker = "<!-- generated:end -->";
        const int MaxDepth = 4; // guards against self-referencing entities
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [InitializeOnLoadMethod]
        static void Init() => BindedPanel.EditorGenerateUxml = FromPanel;

        // Inspector ⋮ menu -> "Generate UXML" on any BindedPanel component.
        static void FromPanel(BindedPanel panel)
        {
            var panelType = panel.GetType();
            var entity = FindEntityInPanel(panelType);
            if (entity == null)
            {
                Debug.LogError($"[UIGen] {panelType.Name}: no field holding an entity with [UIField] members. " +
                               "Add e.g. [SerializeField] PlayerSO so;", panel);
                return;
            }
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(panel));
            var path = Path.ChangeExtension(scriptPath, ".uxml");
            Debug.Log($"[UIGen] entity={entity.Name}, panel={panelType.Name}, file={path}", panel);
            Generate(entity, panelType, path);
        }

        // ---------- entity discovery ----------
        static bool HasUIFields(Type t) =>
            t.GetFields(Flags).Any(f => f.GetCustomAttribute<UIFieldAttribute>() != null);

        static Type FindEntityIn(Type holder)
        {
            foreach (var f in holder.GetFields(Flags))
                if (HasUIFields(f.FieldType)) return f.FieldType;
            return null;
        }

        static Type FindEntityInPanel(Type panel)
        {
            for (var t = panel; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
                foreach (var f in t.GetFields(Flags | BindingFlags.DeclaredOnly))
                {
                    if (HasUIFields(f.FieldType)) return f.FieldType;
                    if (typeof(ScriptableObject).IsAssignableFrom(f.FieldType))
                    {
                        var e = FindEntityIn(f.FieldType);
                        if (e != null) return e;
                    }
                }
            return null;
        }

        // ---------- model ----------
        class Item
        {
            public string Name;            // UXML element name, unique across the whole file
            public string Xml;             // leaf: unindented xml
            public List<Item> Children;    // non-null => group (nested entity)
            public string Path, Title;     // group: relative data-source-path + caption
            public string Key => $"name=\"{Name}\"";
        }

        static string EndMarkerOf(Item g) => $"<!-- end:{g.Name} -->";

        // Root fields are opt-in ([UIField]). Inside a group, all serialized fields
        // are shown unless [UIIgnore]; [UIField] there only customizes the control.
        static List<Item> Build(Type type, string idPrefix, bool optIn, int depth, List<string> skipped)
        {
            var items = new List<Item>();
            foreach (var f in type.GetFields(Flags))
            {
                if (f.Name.StartsWith("<") || f.IsDefined(typeof(UIIgnoreAttribute), true)) continue;
                var a = f.GetCustomAttribute<UIFieldAttribute>();
                bool serialized = (f.IsPublic || f.IsDefined(typeof(SerializeField), true)) && !f.IsNotSerialized;
                if (a == null && (optIn || !serialized)) continue;

                string id = idPrefix == "" ? f.Name : idPrefix + "-" + f.Name;

                if (IsGroup(f.FieldType))
                {
                    if (depth >= MaxDepth) { skipped.Add($"{type.Name}.{f.Name} (max depth)"); continue; }
                    var kids = Build(f.FieldType, id, false, depth + 1, skipped);
                    if (kids.Count == 0) continue;
                    items.Add(new Item
                    {
                        Name = "grp-" + id,
                        Path = f.Name,
                        Children = kids,
                        Title = a?.Label ?? ObjectNames.NicifyVariableName(f.Name)
                    });
                }
                else if (f.FieldType.IsPrimitive || f.FieldType == typeof(string))
                    items.Add(MakeLeaf(f.Name, id, f.FieldType, a ?? new UIFieldAttribute()));
                else
                    skipped.Add($"{type.Name}.{f.Name} ({f.FieldType.Name})");
            }
            return items;
        }

        static bool IsGroup(Type t)
        {
            if (t.IsPrimitive || t.IsEnum || t == typeof(string)) return false;
            if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return false;
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(t)) return false;
            if (!t.IsDefined(typeof(SerializableAttribute), false)) return false;
            var ns = t.Namespace ?? "";
            return !ns.StartsWith("UnityEngine") && !ns.StartsWith("System");
        }

        // ---------- generation (create or merge) ----------
        public static void Generate(Type entityType, Type panelType, string path)
        {
            var skipped = new List<string>();
            var items = Build(entityType, "", true, 0, skipped);

            if (panelType != null)
                foreach (var m in panelType.GetMethods(Flags))
                {
                    var a = m.GetCustomAttribute<UIActionAttribute>();
                    if (a == null) continue;
                    string name = "btn-" + a.Name;
                    items.Add(new Item { Name = name, Xml = $"<ui:Button name=\"{name}\" text=\"{a.Name}\" class=\"btn\" />" });
                }

            if (skipped.Count > 0)
                Debug.LogWarning("[UIGen] Skipped unsupported fields: " + string.Join(", ", skipped));

            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, Template(string.Join("\n", items.Select(i => Render(i, 2)))));
                AssetDatabase.Refresh();
                Debug.Log($"[UIGen] Created {path}");
                return;
            }

            var text = File.ReadAllText(path);
            int added = MergeInto(ref text, items, EndMarker, 2);
            if (added == 0) { Debug.Log("[UIGen] Nothing to add."); return; }
            File.WriteAllText(path, text);
            AssetDatabase.Refresh();
            Debug.Log($"[UIGen] Added {added} missing element(s) to {path}");
        }

        // Missing item -> insert before its container's end marker.
        // Existing group -> recurse so missing children land inside that group.
        static int MergeInto(ref string text, List<Item> items, string endMarker, int depth)
        {
            int added = 0;
            foreach (var it in items)
            {
                if (!text.Contains(it.Key))
                {
                    int idx = text.IndexOf(endMarker, StringComparison.Ordinal);
                    if (idx < 0) { Debug.LogError($"[UIGen] Missing marker {endMarker}; skipped {it.Name}"); continue; }
                    text = text.Insert(idx, Render(it, depth).TrimStart() + "\n" + new string(' ', depth * 4));
                    added++;
                }
                else if (it.Children != null)
                    added += MergeInto(ref text, it.Children, EndMarkerOf(it), depth + 1);
            }
            return added;
        }

        static string Render(Item it, int depth)
        {
            var ind = new string(' ', depth * 4);
            if (it.Children == null)
                return string.Join("\n", it.Xml.Split('\n').Select(l => ind + l.TrimEnd('\r')));

            // group: children bind with RELATIVE paths, so the whole block can be moved/restyled freely
            var sb = new StringBuilder();
            sb.Append($"{ind}<ui:VisualElement name=\"{it.Name}\" data-source-path=\"{it.Path}\" class=\"group\">\n");
            sb.Append($"{ind}    <ui:Label text=\"{it.Title}\" class=\"group-title\" />\n");
            foreach (var c in it.Children) sb.Append(Render(c, depth + 1)).Append('\n');
            sb.Append($"{ind}    {EndMarkerOf(it)}\n");
            sb.Append($"{ind}</ui:VisualElement>");
            return sb.ToString();
        }

        // ---------- leaf controls ----------
        static Item MakeLeaf(string field, string id, Type type, UIFieldAttribute a)
        {
            string label = a.Label ?? ObjectNames.NicifyVariableName(field);
            var ct = a.Type == ControlType.Auto ? Guess(type) : a.Type;
            var range = $"low-value=\"{a.Min}\" high-value=\"{a.Max}\"";

            var (tag, prefix, prop, mode, extra) = ct switch
            {
                ControlType.TextField => ("TextField", "in-", "value", "TwoWay", ""),
                ControlType.Toggle => ("Toggle", "in-", "value", "TwoWay", ""),
                ControlType.Slider => ("Slider", "in-", "value", "TwoWay", range),
                ControlType.ProgressBar => ("ProgressBar", "bar-", "value", "ToTarget", range),
                _ => ("Label", "val-", "text", "ToTarget", ""),
            };
            string name = prefix + id;
            return new Item { Name = name, Xml = Row(label, tag, name, prop, field, mode, extra) };
        }

        static ControlType Guess(Type t)
        {
            if (t == typeof(bool)) return ControlType.Toggle;
            if (t == typeof(string)) return ControlType.TextField;
            return ControlType.Label;
        }

        // update trigger is set at runtime by BindedPanel, so it is not emitted here
        static string Row(string label, string tag, string name, string prop, string path, string mode, string extra)
        {
            return
$@"<ui:VisualElement class=""row"">
    <ui:Label text=""{label}"" class=""row-label"" />
    <ui:{tag} name=""{name}"" {extra}>
        <Bindings>
            <ui:DataBinding property=""{prop}"" data-source-path=""{path}"" binding-mode=""{mode}"" />
        </Bindings>
    </ui:{tag}>
</ui:VisualElement>";
        }

        static string Template(string body) =>
$@"<ui:UXML xmlns:ui=""UnityEngine.UIElements"">
    <ui:VisualElement name=""root"" class=""panel"">
        {BeginMarker}
{body}
        {EndMarker}
    </ui:VisualElement>
</ui:UXML>
";
    }
}
#endif