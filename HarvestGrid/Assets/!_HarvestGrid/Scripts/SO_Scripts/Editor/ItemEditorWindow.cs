using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// Tools > Item Editor
public class ItemEditorWindow : EditorWindow
{
    private readonly List<ItemPrototype> items = new();
    private ItemPrototype selected;
    private SerializedObject so;
    private SerializedProperty itemProp, useConfigProp;
    private ReorderableList useList;

    private Vector2 listScroll, editScroll;
    private string search = "";

    [MenuItem("Tools/Item Editor")]
    public static void Open()
    {
        var w = GetWindow<ItemEditorWindow>("Item Editor");
        w.minSize = new Vector2(760, 480);
    }

    private void OnEnable()
    {
        RefreshItems();
        Undo.undoRedoPerformed += Repaint;
    }

    private void OnDisable() => Undo.undoRedoPerformed -= Repaint;

    private void OnProjectChange()
    {
        RefreshItems();
        Repaint();
    }

    // ------------------------------------------------------------ data
    private bool HasTarget() => selected != null && so != null && so.targetObject != null;

    private void RefreshItems()
    {
        items.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:ItemPrototype"))
        {
            var p = AssetDatabase.LoadAssetAtPath<ItemPrototype>(AssetDatabase.GUIDToAssetPath(guid));
            if (p != null) items.Add(p);
        }
        items.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

        if (!HasTarget()) Select(null);
    }

    private void Select(ItemPrototype proto)
    {
        selected = proto;
        if (proto == null)
        {
            so = null;
            return;
        }

        proto.EnsureInitialized();   // new the lists / grid if they are null

        // if the grid was never edited but the item already has a footprint, show it
        if (proto.IsFootprintGridEmpty())
            proto.LoadFootprintFromItem();

        so = new SerializedObject(proto);
        itemProp = so.FindProperty("item");
        useConfigProp = so.FindProperty("useConfig");
        BuildUseList();
        GUI.FocusControl(null);
    }

    private void CreateNewItem()
    {
        string path = EditorUtility.SaveFilePanelInProject("New Item", "NewItemPrototype", "asset", "Choose where to save the item");
        if (string.IsNullOrEmpty(path)) return;

        var proto = CreateInstance<ItemPrototype>();
        AssetDatabase.CreateAsset(proto, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(path);

        // reload so Unity gives the serialized 'item' field an instance
        var loaded = AssetDatabase.LoadAssetAtPath<ItemPrototype>(path);
        RefreshItems();
        Select(loaded);
    }

    private void DuplicateItem()
    {
        if (!HasTarget()) return;
        string src = AssetDatabase.GetAssetPath(selected);
        string dst = AssetDatabase.GenerateUniqueAssetPath(src);
        if (AssetDatabase.CopyAsset(src, dst))
        {
            RefreshItems();
            Select(AssetDatabase.LoadAssetAtPath<ItemPrototype>(dst));
        }
    }

    private void DeleteItem()
    {
        if (!HasTarget()) return;
        if (!EditorUtility.DisplayDialog("Delete item", $"Delete '{selected.name}'? This can't be undone.", "Delete", "Cancel")) return;
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(selected));
        selected = null;
        RefreshItems();
    }

    // ------------------------------------------------------------ use config list
    private void BuildUseList()
    {
        useList = new ReorderableList(so, useConfigProp, true, true, true, true);

        useList.drawHeaderCallback = r => EditorGUI.LabelField(r, "Item Uses (type + crop/plant to assign)");

        useList.elementHeightCallback = i =>
        {
            var el = useConfigProp.GetArrayElementAtIndex(i);
            return EditorGUI.GetPropertyHeight(el, true) + 6;
        };

        useList.drawElementCallback = (r, i, a, f) =>
        {
            var el = useConfigProp.GetArrayElementAtIndex(i);
            r.y += 3;
            r.height = EditorGUI.GetPropertyHeight(el, true);
            EditorGUI.PropertyField(r, el, new GUIContent("Use Type"), true);   // uses ItemUseConfigEntryDrawer
        };
    }

    // ------------------------------------------------------------ GUI
    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        DrawItemList();
        DrawEditor();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawItemList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(210));

        if (GUILayout.Button("+ New Item", GUILayout.Height(26))) CreateNewItem();
        search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);

        listScroll = EditorGUILayout.BeginScrollView(listScroll, "box");
        foreach (var p in items)
        {
            if (p == null) continue;
            if (!string.IsNullOrEmpty(search) &&
                p.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;

            bool isSel = p == selected;
            var style = new GUIStyle(EditorStyles.label) { fontStyle = isSel ? FontStyle.Bold : FontStyle.Normal };
            Rect r = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            if (isSel) EditorGUI.DrawRect(r, new Color(0.24f, 0.48f, 0.9f, 0.35f));
            if (GUI.Button(r, p.name, style)) Select(p);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void DrawEditor()
    {
        EditorGUILayout.BeginVertical();

        var picked = (ItemPrototype)EditorGUILayout.ObjectField("Item", selected, typeof(ItemPrototype), false);
        if (picked != selected) Select(picked);

        if (!HasTarget())
        {
            EditorGUILayout.HelpBox("Select an item on the left, or create a new one.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        editScroll = EditorGUILayout.BeginScrollView(editScroll);

        so.Update();
        DrawItemFields();
        EditorGUILayout.Space(10);
        DrawUses();
        so.ApplyModifiedProperties();

        // footprint edits the target directly, so it runs after ApplyModifiedProperties
        EditorGUILayout.Space(10);
        DrawFootprint();
        so.Update();

        EditorGUILayout.EndScrollView();

        DrawFooter();
        EditorGUILayout.EndVertical();
    }

    // ---- Item data ----
    private void DrawItemFields()
    {
        EditorGUILayout.LabelField("Item", EditorStyles.boldLabel);

        if (itemProp == null || selected.item == null)
        {
            EditorGUILayout.HelpBox("Item data is null on this asset. Re-select it after Unity finishes importing.", MessageType.Warning);
            return;
        }

        foreach (var field in new[] { "id", "name", "icon", "rarity", "price", "progressTimer", "gameObject" })
        {
            var p = itemProp.FindPropertyRelative(field);
            if (p != null) EditorGUILayout.PropertyField(p, true);
        }
    }

    // ---- Uses ----
    private void DrawUses()
    {
        EditorGUILayout.LabelField("Uses", EditorStyles.boldLabel);
        useList.DoLayoutList();

        if (GUILayout.Button("Init Item Uses  (build uses from the list above)"))
        {
            so.ApplyModifiedProperties();               // make sure the list edits are saved first
            Undo.RecordObject(selected, "Init Item Uses");
            selected.InitItem();
            EditorUtility.SetDirty(selected);
            so.Update();
        }

        // read-only view of what is currently built into the item
        var built = selected.item?.Uses;
        if (built == null || built.Count == 0)
        {
            EditorGUILayout.LabelField("Built uses: none", EditorStyles.miniLabel);
            return;
        }

        var sb = new System.Text.StringBuilder("Built uses: ");
        for (int i = 0; i < built.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(built[i] == null ? "null" : built[i].GetType().Name);
        }
        EditorGUILayout.LabelField(sb.ToString(), EditorStyles.miniLabel);
    }

    // ---- Footprint ----
    private void DrawFootprint()
    {
        EditorGUILayout.LabelField("Footprint", EditorStyles.boldLabel);
        selected.EnsureInitialized();

        EditorGUI.BeginChangeCheck();
        int w = EditorGUILayout.IntField("Width", selected.footprintWidth);
        int h = EditorGUILayout.IntField("Height", selected.footprintHeight);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(selected, "Resize Footprint");
            selected.ResizeFootprintGrid(w, h);
            EditorUtility.SetDirty(selected);
        }

        EditorGUILayout.Space(4);

        // same drawing order as the old ItemFootprintSO editor
        for (int y = selected.footprintHeight - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < selected.footprintWidth; x++)
            {
                bool value = selected.GetCell(x, y);
                bool newValue = GUILayout.Toggle(value, "", GUI.skin.button, GUILayout.Width(30), GUILayout.Height(30));
                if (newValue != value)
                {
                    Undo.RecordObject(selected, "Change Footprint Cell");
                    selected.SetCell(x, y, newValue);
                    EditorUtility.SetDirty(selected);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear"))
        {
            Undo.RecordObject(selected, "Clear Footprint");
            selected.FillFootprintGrid(false);
            EditorUtility.SetDirty(selected);
        }
        if (GUILayout.Button("Fill"))
        {
            Undo.RecordObject(selected, "Fill Footprint");
            selected.FillFootprintGrid(true);
            EditorUtility.SetDirty(selected);
        }
        if (GUILayout.Button("Load From Item"))
        {
            Undo.RecordObject(selected, "Load Footprint");
            if (!selected.LoadFootprintFromItem())
                Debug.LogWarning($"{selected.name}: the item has no footprint to load.", selected);
            EditorUtility.SetDirty(selected);
        }
        GUI.backgroundColor = new Color(0.5f, 0.9f, 0.5f);
        if (GUILayout.Button("Apply To Item"))
        {
            Undo.RecordObject(selected, "Apply Footprint");
            selected.ApplyFootprint();
            EditorUtility.SetDirty(selected);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (!selected.FootprintMatchesItem())
            EditorGUILayout.HelpBox("The grid differs from the footprint stored on the item. Press 'Apply To Item' to use it.", MessageType.Warning);

        var req = selected.item?.Footprint?.Requiring;
        EditorGUILayout.LabelField("Stored on item",
            req != null ? $"{req.GetLength(0)} x {req.GetLength(1)}" : "Empty", EditorStyles.miniLabel);
    }

    // ---- Footer ----
    private void DrawFooter()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Ping")) EditorGUIUtility.PingObject(selected);
        if (GUILayout.Button("Duplicate")) DuplicateItem();
        if (GUILayout.Button("Delete")) DeleteItem();

        GUI.backgroundColor = new Color(0.5f, 0.9f, 0.5f);
        if (GUILayout.Button("Init All + Save", GUILayout.Width(120)))
        {
            Undo.RecordObject(selected, "Init All");
            selected.InitItem();
            selected.ApplyFootprint();
            EditorUtility.SetDirty(selected);
            AssetDatabase.SaveAssets();
        }
        if (GUILayout.Button("Save", GUILayout.Width(80)))
        {
            EditorUtility.SetDirty(selected);
            AssetDatabase.SaveAssets();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }
}