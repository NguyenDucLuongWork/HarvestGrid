using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// Tools > Plant Editor
/// Edits PlantPrototype.plant directly (stages hold natively serialized dictionaries, Unity 6.6+).
public class PlantEditorWindow : EditorWindow
{
    private readonly List<PlantPrototype> plants = new();
    private readonly Dictionary<string, CropSO> cropById = new();

    private PlantPrototype selected;
    private SerializedObject so;
    private SerializedProperty idProp, nameProp, countProp, stagesProp;
    private ReorderableList stageList;
    private bool needSync;

    // random requirement settings
    private const int PendingNone = -1, PendingAll = -2;
    private int pendingRandom = PendingNone;
    private int randomMin = 5, randomMax = 15, randomCount = 1;

    private Vector2 listScroll, editScroll;
    private string search = "";

    [MenuItem("Tools/Plant Editor")]
    public static void Open()
    {
        var w = GetWindow<PlantEditorWindow>("Plant Editor");
        w.minSize = new Vector2(760, 480);
    }

    private void OnEnable()
    {
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
            selected.SyncGrowthState();   // derived data, keep it matching the stages
            so.Update();
        }
        Repaint();
    }

    // ------------------------------------------------------------ data
    private bool HasTarget() => selected != null && so != null && so.targetObject != null;

    private void RefreshAssets()
    {
        plants.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:PlantPrototype"))
        {
            var p = AssetDatabase.LoadAssetAtPath<PlantPrototype>(AssetDatabase.GUIDToAssetPath(guid));
            if (p != null) plants.Add(p);
        }
        plants.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

        cropById.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:CropSO"))
        {
            var c = AssetDatabase.LoadAssetAtPath<CropSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (c != null && c.crop != null && !string.IsNullOrEmpty(c.crop.CropID))
                cropById[c.crop.CropID] = c;
        }

        if (!HasTarget()) Select(null);
    }

    private CropSO FindCropSO(Crop crop)
    {
        if (crop == null || string.IsNullOrEmpty(crop.CropID)) return null;
        cropById.TryGetValue(crop.CropID, out var so);
        return so;
    }

    private void Select(PlantPrototype proto)
    {
        selected = proto;
        if (proto == null)
        {
            so = null;
            return;
        }

        proto.EnsureInitialized();   // new the plant if it is null

        so = new SerializedObject(proto);
        var plantProp = so.FindProperty("plant");
        idProp = plantProp.FindPropertyRelative("plantID");
        nameProp = plantProp.FindPropertyRelative("plantName");
        countProp = plantProp.FindPropertyRelative("averageCropCount");
        stagesProp = plantProp.FindPropertyRelative("stages");

        BuildStageList();
        ExpandAllStages();
        GUI.FocusControl(null);
    }

    private void CreateNewPlant()
    {
        string path = EditorUtility.SaveFilePanelInProject("New Plant", "PlantPrototype", "asset", "Choose where to save the plant");
        if (string.IsNullOrEmpty(path)) return;

        string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        var proto = CreateInstance<PlantPrototype>();
        proto.plant = new Plant(fileName, fileName, 3, new List<PlantStage>());
        AssetDatabase.CreateAsset(proto, path);
        AssetDatabase.SaveAssets();

        RefreshAssets();
        Select(proto);
    }

    private void DuplicatePlant()
    {
        if (!HasTarget()) return;
        string src = AssetDatabase.GetAssetPath(selected);
        string dst = AssetDatabase.GenerateUniqueAssetPath(src);
        if (AssetDatabase.CopyAsset(src, dst))
        {
            RefreshAssets();
            Select(AssetDatabase.LoadAssetAtPath<PlantPrototype>(dst));
        }
    }

    private void DeletePlant()
    {
        if (!HasTarget()) return;
        if (!EditorUtility.DisplayDialog("Delete plant", $"Delete '{selected.name}'? This can't be undone.", "Delete", "Cancel")) return;
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(selected));
        selected = null;
        RefreshAssets();
    }

    // ------------------------------------------------------------ stage list
    private void ExpandAllStages()
    {
        for (int i = 0; i < stagesProp.arraySize; i++)
            stagesProp.GetArrayElementAtIndex(i).FindPropertyRelative("requiredResources").isExpanded = true;
    }

    private void BuildStageList()
    {
        stageList = new ReorderableList(so, stagesProp, true, true, true, true);

        stageList.drawHeaderCallback = r => EditorGUI.LabelField(r, "Growth Stages (in growth order)");

        stageList.elementHeightCallback = i =>
        {
            if (i >= stagesProp.arraySize) return EditorGUIUtility.singleLineHeight;
            var dict = stagesProp.GetArrayElementAtIndex(i).FindPropertyRelative("requiredResources");
            return EditorGUIUtility.singleLineHeight + 4 + EditorGUI.GetPropertyHeight(dict, true) + 8;
        };

        stageList.drawElementCallback = (r, i, a, f) =>
        {
            if (i >= stagesProp.arraySize) return;
            var el = stagesProp.GetArrayElementAtIndex(i);
            var sprite = el.FindPropertyRelative("sprite");
            var dict = el.FindPropertyRelative("requiredResources");

            float line = EditorGUIUtility.singleLineHeight;
            float y = r.y + 4;

            EditorGUI.LabelField(new Rect(r.x, y, 70, line), $"Stage {i + 1}", EditorStyles.boldLabel);
            EditorGUI.PropertyField(new Rect(r.x + 75, y, r.width - 75 - 80, line), sprite, GUIContent.none);
            if (GUI.Button(new Rect(r.xMax - 75, y, 75, line), "Random"))
                pendingRandom = i;   // processed after drawing, never while the dictionary is being drawn
            y += line + 4;

            // the dictionary is drawn by Unity's own dictionary inspector (keys | values)
            float h = EditorGUI.GetPropertyHeight(dict, true);
            EditorGUI.PropertyField(new Rect(r.x + 10, y, r.width - 10, h), dict, new GUIContent("Required Resources"), true);
        };

        stageList.onAddCallback = l =>
        {
            ReorderableList.defaultBehaviours.DoAddButton(l);
            if (l.index >= 0 && l.index < stagesProp.arraySize)
                stagesProp.GetArrayElementAtIndex(l.index).FindPropertyRelative("requiredResources").isExpanded = true;
        };
    }

    // ------------------------------------------------------------ GUI
    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        DrawPlantList();
        DrawEditor();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPlantList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(210));

        if (GUILayout.Button("+ New Plant", GUILayout.Height(26))) CreateNewPlant();
        search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);

        listScroll = EditorGUILayout.BeginScrollView(listScroll, "box");
        foreach (var p in plants)
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

        var picked = (PlantPrototype)EditorGUILayout.ObjectField("Plant", selected, typeof(PlantPrototype), false);
        if (picked != selected) Select(picked);

        if (!HasTarget())
        {
            EditorGUILayout.HelpBox("Select a plant on the left, or create a new one.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        editScroll = EditorGUILayout.BeginScrollView(editScroll);

        // ---- serialized fields (id, name, count, stages with their dictionaries) ----
        so.Update();
        EditorGUILayout.LabelField("Plant", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(idProp, new GUIContent("Plant ID"));
        EditorGUILayout.PropertyField(nameProp, new GUIContent("Plant Name"));
        EditorGUILayout.PropertyField(countProp, new GUIContent("Average Crop Count"));

        EditorGUILayout.Space(8);
        DrawRandomSettings();
        stageList.DoLayoutList();
        if (so.ApplyModifiedProperties()) needSync = true;

        // ---- crop (set through the prototype, runs after ApplyModifiedProperties) ----
        EditorGUILayout.Space(8);
        DrawCrop();

        DrawWarnings();
        DrawGrowthInfo();

        EditorGUILayout.EndScrollView();

        DrawFooter();
        EditorGUILayout.EndVertical();

        ProcessPendingRandom();

        // keep nextStageRequirement matching the first stage after any edit
        if (needSync && HasTarget())
        {
            needSync = false;
            selected.SyncGrowthState();
            EditorUtility.SetDirty(selected);
            so.Update();
            Repaint();
        }
    }

    // ------------------------------------------------------------ random requirements
    private void DrawRandomSettings()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Random", EditorStyles.boldLabel, GUILayout.Width(55));

        EditorGUIUtility.labelWidth = 60;
        randomMin = Mathf.Max(1, EditorGUILayout.IntField("Min", randomMin, GUILayout.Width(100)));
        randomMax = Mathf.Max(randomMin, EditorGUILayout.IntField("Max", randomMax, GUILayout.Width(100)));
        EditorGUIUtility.labelWidth = 130;
        randomCount = Mathf.Max(1, EditorGUILayout.IntField("Resources / stage", randomCount, GUILayout.Width(170)));
        EditorGUIUtility.labelWidth = 0;

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Randomize All Stages", GUILayout.Width(150)))
            pendingRandom = PendingAll;

        EditorGUILayout.EndHorizontal();
    }

    private void ProcessPendingRandom()
    {
        int pending = pendingRandom;
        pendingRandom = PendingNone;
        if (pending == PendingNone || !HasTarget()) return;

        var resources = GetAllResources();
        if (resources.Count == 0)
        {
            Debug.LogWarning("Plant Editor: no Resource values found to randomize with.");
            return;
        }

        var stages = selected.plant.Stages;
        if (stages == null || stages.Count == 0) return;

        Undo.RecordObject(selected, "Randomize Requirements");

        if (pending == PendingAll)
        {
            foreach (var stage in stages)
                if (stage != null) RandomizeStage(stage, resources);
        }
        else if (pending >= 0 && pending < stages.Count && stages[pending] != null)
        {
            RandomizeStage(stages[pending], resources);
        }

        EditorUtility.SetDirty(selected);
        needSync = true;   // first-stage requirements may have changed
        so.Update();
        ExpandAllStages();
        Repaint();
    }

    /// <summary>Replaces the stage's requirements with random resource(s), each with an amount in [randomMin, randomMax].</summary>
    private void RandomizeStage(PlantStage stage, List<Resource> resources)
    {
        var pool = new List<Resource>(resources);
        int count = Mathf.Clamp(randomCount, 1, pool.Count);

        var dict = stage.RequiredResources;
        dict.Clear();

        for (int n = 0; n < count; n++)
        {
            int pick = UnityEngine.Random.Range(0, pool.Count);   // distinct resources: remove after picking
            dict[pool[pick]] = UnityEngine.Random.Range(randomMin, randomMax + 1);
            pool.RemoveAt(pick);
        }
    }

    /// <summary>All values of Resource, whether it is an enum or a ScriptableObject type.</summary>
    private static List<Resource> GetAllResources()
    {
        var result = new List<Resource>();

        if (typeof(Resource).IsEnum)
        {
            foreach (var v in Enum.GetValues(typeof(Resource)))
                result.Add((Resource)v);
        }
        else if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(Resource)))
        {
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(Resource).Name))
            {
                var obj = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(Resource));
                if (obj != null) result.Add((Resource)(object)obj);
            }
        }

        return result;
    }

    private void DrawCrop()
    {
        EditorGUILayout.LabelField("Crop", EditorStyles.boldLabel);

        var crop = selected.plant.Crop;
        var currentSO = FindCropSO(crop);

        EditorGUI.BeginChangeCheck();
        var newSO = (CropSO)EditorGUILayout.ObjectField("Crop SO", currentSO, typeof(CropSO), false);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(selected, "Assign Crop");
            selected.SetCrop(newSO != null ? newSO.crop : null);
            EditorUtility.SetDirty(selected);
            so.Update();
            ExpandAllStages();
            crop = selected.plant.Crop;
            currentSO = newSO;
        }

        if (crop == null)
            EditorGUILayout.HelpBox("No crop assigned. The plant will give nothing when harvested.", MessageType.Warning);
        else
        {
            if (currentSO == null)
                EditorGUILayout.HelpBox($"The stored crop '{crop.CropID}' has no matching CropSO asset.", MessageType.Info);
            EditorGUILayout.LabelField($"{crop.Name}  (id: {crop.CropID}, base price: {crop.BaseSellPrice})", EditorStyles.miniLabel);
        }
    }

    private void DrawWarnings()
    {
        var plant = selected.plant;

        if (string.IsNullOrWhiteSpace(plant.PlantID))
            EditorGUILayout.HelpBox("Plant ID is empty.", MessageType.Warning);

        var stages = plant.Stages;
        if (stages == null || stages.Count == 0)
        {
            EditorGUILayout.HelpBox("No stages. The plant can never grow.", MessageType.Warning);
            return;
        }

        for (int i = 0; i < stages.Count; i++)
        {
            var stage = stages[i];
            if (stage == null) continue;

            if (stage.Sprite == null)
                EditorGUILayout.HelpBox($"Stage {i + 1} has no sprite.", MessageType.Warning);

            if (stage.RequiredResources.Count == 0)
                EditorGUILayout.HelpBox($"Stage {i + 1} has no required resources, so it can't be reached by absorbing resources.", MessageType.Warning);
        }
    }

    private void DrawGrowthInfo()
    {
        var plant = selected.plant;
        int next = plant.NextStageRequirement != null ? plant.NextStageRequirement.Count : 0;
        EditorGUILayout.LabelField(
            $"Stages: {(plant.Stages != null ? plant.Stages.Count : 0)}   |   First-stage requirements synced: {next} resource(s)",
            EditorStyles.miniLabel);
    }

    private void DrawFooter()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Ping")) EditorGUIUtility.PingObject(selected);
        if (GUILayout.Button("Duplicate")) DuplicatePlant();
        if (GUILayout.Button("Delete")) DeletePlant();

        GUI.backgroundColor = new Color(0.5f, 0.9f, 0.5f);
        if (GUILayout.Button("Save", GUILayout.Width(100)))
        {
            so.ApplyModifiedProperties();
            selected.SyncGrowthState();
            EditorUtility.SetDirty(selected);
            AssetDatabase.SaveAssets();
            so.Update();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }
}