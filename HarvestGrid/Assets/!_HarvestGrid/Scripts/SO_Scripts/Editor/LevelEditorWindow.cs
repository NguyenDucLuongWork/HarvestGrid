using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

/// Tools > Level Editor
/// Edits LevelSO's serialized dictionaries directly (Unity 6.6+).
/// The window works on row lists and writes them back into the dictionaries after every change.
public class LevelEditorWindow : EditorWindow
{
    private class CropRow
    {
        public CropSO cropSO;
        public Crop crop;          // template, used when no matching CropSO asset exists
        public int stars = 1;
        public int amount = 1;
    }

    private class ItemRow
    {
        public ItemPrototype item;
        public float chance = 1f;
    }

    private readonly List<LevelSO> levels = new();
    private readonly List<CropRow> cropRows = new();
    private readonly List<ItemRow> itemRows = new();
    private readonly Dictionary<string, CropSO> cropById = new();

    private LevelSO selected;
    private SerializedObject so;
    private SerializedProperty levelID, money, farmMono, storingSpaceSO;
    private ReorderableList cropList, itemList;

    private Vector2 listScroll, editScroll;
    private string search = "";

    [MenuItem("Tools/Level Editor")]
    public static void Open()
    {
        var w = GetWindow<LevelEditorWindow>("Level Editor");
        w.minSize = new Vector2(720, 420);
    }

    private void OnEnable()
    {
        BuildLists();
        RefreshAssets();
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

    private void OnProjectChange()
    {
        RefreshAssets();
        Repaint();
    }

    private void OnUndoRedo()
    {
        if (HasTarget())
        {
            so.Update();
            ReloadRows();
        }
        Repaint();
    }

    // ------------------------------------------------------------ data
    private bool HasTarget() => selected != null && so != null && so.targetObject != null;

    private void RefreshAssets()
    {
        levels.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:LevelSO"))
        {
            var lv = AssetDatabase.LoadAssetAtPath<LevelSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (lv != null) levels.Add(lv);
        }
        levels.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

        cropById.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:CropSO"))
        {
            var c = AssetDatabase.LoadAssetAtPath<CropSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (c != null && c.crop != null && !string.IsNullOrEmpty(c.crop.CropID))
                cropById[c.crop.CropID] = c;
        }

        if (!HasTarget()) Select(null);
    }

    private void Select(LevelSO level)
    {
        selected = level;
        cropRows.Clear();
        itemRows.Clear();

        if (level == null)
        {
            so = null;
            return;
        }

        level.EnsureInitialized();   // new the dictionaries if they are null
        so = new SerializedObject(level);
        levelID = so.FindProperty("levelID");
        money = so.FindProperty("money");
        farmMono = so.FindProperty("farmMono");
        storingSpaceSO = so.FindProperty("storingSpaceSO");

        ReloadRows();
        GUI.FocusControl(null);
    }

    /// <summary>asset dictionaries -> window rows</summary>
    private void ReloadRows()
    {
        cropRows.Clear();
        itemRows.Clear();
        if (selected == null) return;
        selected.EnsureInitialized();

        foreach (var kv in selected.requiringCrops)
        {
            if (kv.Key == null) continue;
            cropById.TryGetValue(kv.Key.CropID ?? "", out var cropSO);
            cropRows.Add(new CropRow
            {
                cropSO = cropSO,
                crop = kv.Key,
                stars = Mathf.Clamp(kv.Key.Stars, 1, 5),
                amount = Mathf.Max(1, kv.Value)
            });
        }

        foreach (var kv in selected.itemAndChancePool)
        {
            if (kv.Key == null) continue;
            itemRows.Add(new ItemRow { item = kv.Key, chance = Mathf.Max(0f, kv.Value) });
        }
    }

    /// <summary>window rows -> asset dictionary (merges duplicates, skips empty rows)</summary>
    private void ApplyCropRows()
    {
        if (!HasTarget()) return;
        selected.EnsureInitialized();
        Undo.RecordObject(selected, "Edit Required Crops");

        var merged = new Dictionary<string, (Crop key, int amount)>();
        var order = new List<string>();

        foreach (var row in cropRows)
        {
            Crop template = row.cropSO != null ? row.cropSO.crop : row.crop;
            if (template == null) continue;

            string id = template.CropID + "|" + row.stars;
            if (merged.TryGetValue(id, out var existing))
            {
                merged[id] = (existing.key, existing.amount + row.amount);
            }
            else
            {
                Crop key = template.Clone();
                key.SetStars(row.stars);
                merged[id] = (key, row.amount);
                order.Add(id);
            }
        }

        selected.requiringCrops.Clear();
        foreach (var id in order)
            selected.requiringCrops[merged[id].key] = merged[id].amount;

        EditorUtility.SetDirty(selected);
    }

    private void ApplyItemRows()
    {
        if (!HasTarget()) return;
        selected.EnsureInitialized();
        Undo.RecordObject(selected, "Edit Item Pool");

        selected.itemAndChancePool.Clear();
        foreach (var row in itemRows)
        {
            if (row.item == null) continue;
            selected.itemAndChancePool.TryGetValue(row.item, out float current);
            selected.itemAndChancePool[row.item] = current + row.chance;
        }

        EditorUtility.SetDirty(selected);
    }

    private void CreateNewLevel()
    {
        string path = EditorUtility.SaveFilePanelInProject("New Level", "LevelSO", "asset", "Choose where to save the level");
        if (string.IsNullOrEmpty(path)) return;

        var level = CreateInstance<LevelSO>();
        level.levelID = System.IO.Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(level, path);
        AssetDatabase.SaveAssets();
        RefreshAssets();
        Select(level);
    }

    private void DuplicateLevel()
    {
        if (!HasTarget()) return;
        string src = AssetDatabase.GetAssetPath(selected);
        string dst = AssetDatabase.GenerateUniqueAssetPath(src);
        if (AssetDatabase.CopyAsset(src, dst))
        {
            RefreshAssets();
            Select(AssetDatabase.LoadAssetAtPath<LevelSO>(dst));
        }
    }

    private void DeleteLevel()
    {
        if (!HasTarget()) return;
        if (!EditorUtility.DisplayDialog("Delete level", $"Delete '{selected.name}'? This can't be undone.", "Delete", "Cancel")) return;
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(selected));
        selected = null;
        RefreshAssets();
    }

    // ------------------------------------------------------------ lists
    private void BuildLists()
    {
        // ---- crops ----
        cropList = new ReorderableList(cropRows, typeof(CropRow), true, true, true, true);

        cropList.drawHeaderCallback = r =>
        {
            float w = r.width;
            EditorGUI.LabelField(new Rect(r.x, r.y, w * 0.5f, r.height), "Crop SO");
            EditorGUI.LabelField(new Rect(r.x + w * 0.5f, r.y, w * 0.25f, r.height), "Stars");
            EditorGUI.LabelField(new Rect(r.x + w * 0.75f, r.y, w * 0.25f, r.height), "Amount");
        };

        cropList.drawElementCallback = (r, i, a, f) =>
        {
            if (i >= cropRows.Count) return;
            var row = cropRows[i];
            r.y += 2; r.height = EditorGUIUtility.singleLineHeight;
            float w = r.width;

            EditorGUI.BeginChangeCheck();

            var newSO = (CropSO)EditorGUI.ObjectField(new Rect(r.x, r.y, w * 0.5f - 6, r.height),
                row.cropSO, typeof(CropSO), false);
            if (newSO != row.cropSO)
            {
                row.cropSO = newSO;
                row.crop = newSO != null ? newSO.crop : null;
            }

            row.stars = EditorGUI.IntSlider(new Rect(r.x + w * 0.5f, r.y, w * 0.25f - 6, r.height), row.stars, 1, 5);
            row.amount = Mathf.Max(1, EditorGUI.IntField(new Rect(r.x + w * 0.75f, r.y, w * 0.25f, r.height), row.amount));

            if (EditorGUI.EndChangeCheck()) ApplyCropRows();
        };

        cropList.onAddCallback = _ => cropRows.Add(new CropRow());   // blank row, saved once a crop is assigned
        cropList.onRemoveCallback = l =>
        {
            if (l.index >= 0 && l.index < cropRows.Count) cropRows.RemoveAt(l.index);
            ApplyCropRows();
        };
        cropList.onReorderCallback = _ => ApplyCropRows();

        // ---- items ----
        itemList = new ReorderableList(itemRows, typeof(ItemRow), true, true, true, true);

        itemList.drawHeaderCallback = r =>
        {
            float w = r.width;
            EditorGUI.LabelField(new Rect(r.x, r.y, w * 0.55f, r.height), "Item Prototype");
            EditorGUI.LabelField(new Rect(r.x + w * 0.55f, r.y, w * 0.25f, r.height), "Chance");
            EditorGUI.LabelField(new Rect(r.x + w * 0.8f, r.y, w * 0.2f, r.height), "%");
        };

        itemList.drawElementCallback = (r, i, a, f) =>
        {
            if (i >= itemRows.Count) return;
            var row = itemRows[i];
            r.y += 2; r.height = EditorGUIUtility.singleLineHeight;
            float w = r.width;

            EditorGUI.BeginChangeCheck();

            row.item = (ItemPrototype)EditorGUI.ObjectField(new Rect(r.x, r.y, w * 0.55f - 6, r.height),
                row.item, typeof(ItemPrototype), false);
            row.chance = Mathf.Max(0f, EditorGUI.FloatField(new Rect(r.x + w * 0.55f, r.y, w * 0.25f - 6, r.height), row.chance));

            if (EditorGUI.EndChangeCheck()) ApplyItemRows();

            float total = TotalChance();
            float pct = total > 0f && row.item != null ? row.chance / total * 100f : 0f;
            EditorGUI.LabelField(new Rect(r.x + w * 0.8f, r.y, w * 0.2f, r.height), pct.ToString("0.#") + "%");
        };

        itemList.onAddCallback = _ => itemRows.Add(new ItemRow());   // blank row, saved once an item is assigned
        itemList.onRemoveCallback = l =>
        {
            if (l.index >= 0 && l.index < itemRows.Count) itemRows.RemoveAt(l.index);
            ApplyItemRows();
        };
        itemList.onReorderCallback = _ => ApplyItemRows();
    }

    private void AddCrop(CropSO cropSO)
    {
        if (!HasTarget() || cropSO == null) return;

        var existing = cropRows.Find(r => r.cropSO == cropSO && r.stars == 1);
        if (existing != null) existing.amount++;
        else cropRows.Add(new CropRow { cropSO = cropSO, crop = cropSO.crop });

        ApplyCropRows();
    }

    private void AddItem(ItemPrototype item)
    {
        if (!HasTarget() || item == null) return;
        if (itemRows.Exists(r => r.item == item)) return;

        itemRows.Add(new ItemRow { item = item });
        ApplyItemRows();
    }

    private float TotalChance()
    {
        float t = 0f;
        foreach (var row in itemRows)
            if (row.item != null) t += row.chance;
        return t;
    }

    // ------------------------------------------------------------ GUI
    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        DrawLevelList();
        DrawEditor();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLevelList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(210));

        if (GUILayout.Button("+ New Level", GUILayout.Height(26))) CreateNewLevel();
        search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);

        listScroll = EditorGUILayout.BeginScrollView(listScroll, "box");
        foreach (var lv in levels)
        {
            if (lv == null) continue;
            if (!string.IsNullOrEmpty(search) &&
                lv.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;

            bool isSel = lv == selected;
            var style = new GUIStyle(EditorStyles.label) { fontStyle = isSel ? FontStyle.Bold : FontStyle.Normal };
            Rect r = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            if (isSel) EditorGUI.DrawRect(r, new Color(0.24f, 0.48f, 0.9f, 0.35f));
            if (GUI.Button(r, lv.name, style)) Select(lv);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void DrawEditor()
    {
        EditorGUILayout.BeginVertical();

        var picked = (LevelSO)EditorGUILayout.ObjectField("Level", selected, typeof(LevelSO), false);
        if (picked != selected) Select(picked);

        if (!HasTarget())
        {
            EditorGUILayout.HelpBox("Select a level on the left, or create a new one.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        editScroll = EditorGUILayout.BeginScrollView(editScroll);

        // basic fields go through SerializedObject
        so.Update();
        EditorGUILayout.PropertyField(levelID);
        EditorGUILayout.PropertyField(money);
        EditorGUILayout.PropertyField(farmMono);
        EditorGUILayout.PropertyField(storingSpaceSO);
        so.ApplyModifiedProperties();

        // dictionaries are edited directly on the asset (after ApplyModifiedProperties so nothing overwrites them)
        DrawLegacyBanner();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Required Crops", EditorStyles.boldLabel);
        DropArea<CropSO>("Drop CropSO assets here to add", AddCrop);
        cropList.DoLayoutList();
        DrawCropWarnings();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Item & Chance Pool", EditorStyles.boldLabel);
        DropArea<ItemPrototype>("Drop ItemPrototype assets here to add", AddItem);
        itemList.DoLayoutList();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Total chance: {TotalChance():0.###}", EditorStyles.miniLabel);
        if (GUILayout.Button("Normalize to 1", GUILayout.Width(110))) NormalizeChances();
        if (GUILayout.Button("Equalize", GUILayout.Width(80))) EqualizeChances();
        EditorGUILayout.EndHorizontal();
        DrawItemWarnings();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField(
            $"Stored in asset: {selected.requiringCrops.Count} crop entries, {selected.itemAndChancePool.Count} item entries",
            EditorStyles.miniLabel);

        EditorGUILayout.EndScrollView();

        DrawFooter();
        EditorGUILayout.EndVertical();
    }

    private void DrawLegacyBanner()
    {
        if (!selected.HasLegacyData) return;

        EditorGUILayout.HelpBox("This level still has data saved by the old list-based tool.", MessageType.Warning);
        if (GUILayout.Button("Import old data into the dictionaries"))
        {
            Undo.RecordObject(selected, "Migrate Level Data");
            selected.MigrateLegacy();
            EditorUtility.SetDirty(selected);
            ReloadRows();
        }
    }

    private void DrawFooter()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Ping")) EditorGUIUtility.PingObject(selected);
        if (GUILayout.Button("Reload")) { so.Update(); ReloadRows(); }
        if (GUILayout.Button("Duplicate")) DuplicateLevel();
        if (GUILayout.Button("Delete")) DeleteLevel();

        GUI.backgroundColor = new Color(0.5f, 0.9f, 0.5f);
        if (GUILayout.Button("Save", GUILayout.Width(100)))
        {
            ApplyCropRows();
            ApplyItemRows();
            EditorUtility.SetDirty(selected);
            AssetDatabase.SaveAssets();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    private void NormalizeChances()
    {
        float total = TotalChance();
        if (total <= 0f) return;
        foreach (var row in itemRows) row.chance /= total;
        ApplyItemRows();
    }

    private void EqualizeChances()
    {
        int n = itemRows.FindAll(r => r.item != null).Count;
        if (n == 0) return;
        foreach (var row in itemRows) row.chance = 1f / n;
        ApplyItemRows();
    }

    // ------------------------------------------------------------ validation
    private void DrawCropWarnings()
    {
        for (int i = 0; i < cropRows.Count; i++)
        {
            var a = cropRows[i];
            if (a.cropSO == null && a.crop == null)
            {
                EditorGUILayout.HelpBox($"Crop row {i} has no CropSO. It is not saved until you assign one.", MessageType.Warning);
                continue;
            }
            if (a.cropSO == null)
                EditorGUILayout.HelpBox($"Crop row {i}: no CropSO asset matches crop id '{a.crop.CropID}'. The stored crop is kept as is.", MessageType.Info);

            string idA = a.cropSO != null ? a.cropSO.crop?.CropID : a.crop?.CropID;
            for (int j = i + 1; j < cropRows.Count; j++)
            {
                var b = cropRows[j];
                string idB = b.cropSO != null ? b.cropSO.crop?.CropID : b.crop?.CropID;
                if (idA != null && idA == idB && a.stars == b.stars)
                    EditorGUILayout.HelpBox($"Rows {i} and {j}: same crop and stars ({idA}, {a.stars}★). Amounts are merged.", MessageType.Info);
            }
        }
    }

    private void DrawItemWarnings()
    {
        for (int i = 0; i < itemRows.Count; i++)
        {
            var a = itemRows[i].item;
            if (a == null)
            {
                EditorGUILayout.HelpBox($"Item row {i} has no ItemPrototype. It is not saved until you assign one.", MessageType.Warning);
                continue;
            }
            for (int j = i + 1; j < itemRows.Count; j++)
                if (itemRows[j].item == a)
                    EditorGUILayout.HelpBox($"Rows {i} and {j} both use '{a.name}'. Chances are summed.", MessageType.Info);
        }
    }

    // ------------------------------------------------------------ drag & drop
    private static void DropArea<T>(string label, Action<T> onDrop) where T : Object
    {
        Rect r = GUILayoutUtility.GetRect(0, 32, GUILayout.ExpandWidth(true));
        GUI.Box(r, label, EditorStyles.helpBox);

        Event e = Event.current;
        if (!r.Contains(e.mousePosition)) return;

        if (e.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            e.Use();
        }
        else if (e.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            foreach (var obj in DragAndDrop.objectReferences)
                if (obj is T t) onDrop(t);
            e.Use();
        }
    }
}