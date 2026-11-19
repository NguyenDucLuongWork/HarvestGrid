using GameJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

/// <summary>
/// Tools > Level Editor
/// Advanced Level Editor & Procedural Level Generator for LevelSO.
/// - Dev edits using ScriptableObjects (CropSO, ItemPrototype, StoringSpaceSO, FarmMono).
/// - Saves clean data (CropRequirement: id, stars, amount; ItemChanceEntry: id, chance) via StateEntity and [ContextData].
/// - Procedural Level Generator guaranteeing 3 use types (Resource provider, Add plant, Harvest), farm slots 1-25, and procedural storing space.
/// - Hardness predictor and actionable developer recommendations.
/// - Live AI Clean JSON preview.
/// </summary>
public class LevelEditorWindow : EditorWindow
{
    private enum EditorTab
    {
        ManualEdit,
        ProceduralGenerator,
        AIJsonPreview
    }

    public enum DifficultyPreset
    {
        Casual,
        Easy,
        Medium,
        Hard,
        Nightmare,
        Custom
    }

    public enum ObstaclePattern
    {
        OpenGrid,
        CornersCut,
        CenterIsland,
        Checkerboard,
        BorderFrame
    }

    // Helper row for UI binding with SO references
    private class CropRow
    {
        public CropSO cropSO;
        public string cropId = "";
        public int stars = 1;
        public int amount = 1;
    }

    private class ItemRow
    {
        public ItemPrototype item;
        public string itemId = "";
        public float chance = 1f;
    }

    // ---- Window state ----
    private EditorTab currentTab = EditorTab.ManualEdit;
    private readonly List<LevelSO> levels = new();
    private readonly List<CropRow> cropRows = new();
    private readonly List<ItemRow> itemRows = new();

    // Asset registries for lookups
    private readonly Dictionary<string, CropSO> cropById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ItemPrototype> itemById = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<CropSO> allCropSOs = new();
    private readonly List<ItemPrototype> allItemPrototypes = new();
    private readonly List<FarmMono> allFarmPrefabs = new();

    private LevelSO selected;
    private SerializedObject so;
    private SerializedProperty levelIDProp, timeBudgetProp, moneyProp, farmMonoProp, storingSpaceSOProp;
    private ReorderableList cropList, itemList;

    private Vector2 listScroll, editScroll, previewScroll;
    private string search = "";
    private string cachedCleanJson = "";

    // ---- Procedural generator parameters ----
    private DifficultyPreset proceduralDifficulty = DifficultyPreset.Medium;
    [Range(1, 25)] private int proceduralFarmSlots = 9;
    private int proceduralCropCount = 2;
    private float proceduralStarMin = 1f;
    private float proceduralStarMax = 3f;
    private int proceduralAmountMin = 3;
    private int proceduralAmountMax = 8;
    private int proceduralGridWidth = 4;
    private int proceduralGridHeight = 4;
    private ObstaclePattern proceduralObstacle = ObstaclePattern.CornersCut;

    [MenuItem("Tools/Level Editor")]
    public static void Open()
    {
        var w = GetWindow<LevelEditorWindow>("Level Editor");
        w.minSize = new Vector2(820, 520);
    }

    private void OnEnable()
    {
        RefreshAssetDatabases();
        BuildLists();
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    private void OnProjectChange()
    {
        RefreshAssetDatabases();
        Repaint();
    }

    private void OnUndoRedo()
    {
        if (HasTarget())
        {
            so.Update();
            ReloadRowsFromAsset();
        }
        Repaint();
    }

    private bool HasTarget() => selected != null && so != null && so.targetObject != null;

    // ------------------------------------------------------------ Asset Database Discovery
    private void RefreshAssetDatabases()
    {
        levels.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:LevelSO"))
        {
            var lv = AssetDatabase.LoadAssetAtPath<LevelSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (lv != null) levels.Add(lv);
        }
        levels.Sort((a, b) => a.levelID.CompareTo(b.levelID));

        allCropSOs.Clear();
        cropById.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:CropSO"))
        {
            var c = AssetDatabase.LoadAssetAtPath<CropSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (c != null && c.crop != null && !string.IsNullOrEmpty(c.crop.CropID))
            {
                allCropSOs.Add(c);
                cropById[c.crop.CropID] = c;
            }
        }

        allItemPrototypes.Clear();
        itemById.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:ItemPrototype"))
        {
            var p = AssetDatabase.LoadAssetAtPath<ItemPrototype>(AssetDatabase.GUIDToAssetPath(guid));
            if (p != null && p.item != null && !string.IsNullOrEmpty(p.item.Id))
            {
                allItemPrototypes.Add(p);
                itemById[p.item.Id] = p;
            }
        }

        allFarmPrefabs.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/!_HarvestGrid/Prefabs" }))
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (go != null)
            {
                var fm = go.GetComponent<FarmMono>();
                if (fm != null && !allFarmPrefabs.Contains(fm))
                    allFarmPrefabs.Add(fm);
            }
        }

        allFarmPrefabs.Sort((a, b) => GetSlotCount(a).CompareTo(GetSlotCount(b)));

        if (!HasTarget() && levels.Count > 0)
        {
            Select(levels[0]);
        }
    }

    private int GetSlotCount(FarmMono fm)
    {
        if (fm == null) return 0;
        return fm.GetComponentsInChildren<FarmSlotMono>(true).Length;
    }

    private void Select(LevelSO level)
    {
        selected = level;
        cropRows.Clear();
        itemRows.Clear();
        cachedCleanJson = "";

        if (level == null)
        {
            so = null;
            return;
        }

        level.EnsureInitialized();
        so = new SerializedObject(level);
        levelIDProp = so.FindProperty("levelID");
        timeBudgetProp = so.FindProperty("timeBudget");
        moneyProp = so.FindProperty("money");
        farmMonoProp = so.FindProperty("farmMono");
        storingSpaceSOProp = so.FindProperty("storingSpaceSO");

        ReloadRowsFromAsset();
        GUI.FocusControl(null);
    }

    /// <summary>Populates window UI rows from selected LevelSO's clean data.</summary>
    private void ReloadRowsFromAsset()
    {
        cropRows.Clear();
        itemRows.Clear();
        if (selected == null) return;
        selected.EnsureInitialized();

        // 1. Load clean required crops (CropRequirement: id, stars, amount)
        if (selected.requiredCrops != null && selected.requiredCrops.Count > 0)
        {
            foreach (var req in selected.requiredCrops)
            {
                if (req == null) continue;
                string cid = req.CropID ?? "";
                cropById.TryGetValue(cid, out var cropSO);
                cropRows.Add(new CropRow
                {
                    cropSO = cropSO,
                    cropId = cid,
                    stars = Mathf.Clamp(req.stars, 1, 5),
                    amount = Mathf.Max(1, req.amount)
                });
            }
        }
        else if (selected.requiringCrops != null && selected.requiringCrops.Count > 0)
        {
            // Fallback from runtime dictionary if clean list was empty
            foreach (var kv in selected.requiringCrops)
            {
                if (kv.Key == null) continue;
                string cid = kv.Key.CropID ?? "";
                cropById.TryGetValue(cid, out var cropSO);
                cropRows.Add(new CropRow
                {
                    cropSO = cropSO,
                    cropId = cid,
                    stars = Mathf.Clamp(kv.Key.Stars, 1, 5),
                    amount = Mathf.Max(1, kv.Value)
                });
            }
        }

        // 2. Load clean item pool (ItemChanceEntry: id, chance)
        if (selected.itemPool != null && selected.itemPool.Count > 0)
        {
            foreach (var entry in selected.itemPool)
            {
                if (entry == null) continue;
                string iid = entry.ItemID ?? "";
                itemById.TryGetValue(iid, out var itemProto);
                itemRows.Add(new ItemRow
                {
                    item = itemProto,
                    itemId = iid,
                    chance = Mathf.Max(0f, entry.chance)
                });
            }
        }
        else if (selected.itemAndChancePool != null && selected.itemAndChancePool.Count > 0)
        {
            // Fallback from runtime dictionary if clean list was empty
            foreach (var kv in selected.itemAndChancePool)
            {
                if (kv.Key == null || kv.Key.item == null) continue;
                string iid = kv.Key.item.Id ?? "";
                itemRows.Add(new ItemRow
                {
                    item = kv.Key,
                    itemId = iid,
                    chance = Mathf.Max(0f, kv.Value)
                });
            }
        }

        UpdateCleanJsonPreview();
    }

    /// <summary>Writes window rows into clean data (requiredCrops, itemPool) and syncs runtime dictionaries.</summary>
    private void SaveRowsToAsset()
    {
        if (!HasTarget()) return;
        selected.EnsureInitialized();
        Undo.RecordObject(selected, "Save Level Data");

        // 1. Save clean requiredCrops
        selected.requiredCrops.Clear();
        selected.requiringCrops.Clear();

        var mergedCrops = new Dictionary<string, (string cropId, CropSO cso, int stars, int amount)>();
        foreach (var row in cropRows)
        {
            string cid = row.cropSO != null && row.cropSO.crop != null ? row.cropSO.crop.CropID : row.cropId;
            if (string.IsNullOrEmpty(cid)) continue;

            string key = cid + "|" + row.stars;
            if (mergedCrops.TryGetValue(key, out var existing))
            {
                mergedCrops[key] = (cid, existing.cso ?? row.cropSO, row.stars, existing.amount + row.amount);
            }
            else
            {
                mergedCrops[key] = (cid, row.cropSO, row.stars, row.amount);
            }
        }

        foreach (var pair in mergedCrops.Values)
        {
            selected.requiredCrops.Add(new CropRequirement(pair.cropId, pair.stars, pair.amount));

            // Sync runtime dictionary
            Crop c = pair.cso != null && pair.cso.crop != null ? pair.cso.crop.Clone() : new Crop(null) { id = pair.cropId };
            c.SetStars(pair.stars);
            selected.requiringCrops[c] = pair.amount;
        }

        // 2. Save clean itemPool
        selected.itemPool.Clear();
        selected.itemAndChancePool.Clear();

        var mergedItems = new Dictionary<string, (string itemId, ItemPrototype proto, float chance)>();
        foreach (var row in itemRows)
        {
            string iid = row.item != null && row.item.item != null ? row.item.item.Id : row.itemId;
            if (string.IsNullOrEmpty(iid)) continue;

            if (mergedItems.TryGetValue(iid, out var existing))
            {
                mergedItems[iid] = (iid, existing.proto ?? row.item, existing.chance + row.chance);
            }
            else
            {
                mergedItems[iid] = (iid, row.item, row.chance);
            }
        }

        foreach (var pair in mergedItems.Values)
        {
            selected.itemPool.Add(new ItemChanceEntry(pair.itemId, pair.chance));

            // Sync runtime dictionary
            if (pair.proto != null)
            {
                selected.itemAndChancePool[pair.proto] = pair.chance;
            }
        }

        EditorUtility.SetDirty(selected);
        UpdateCleanJsonPreview();
    }

    private void UpdateCleanJsonPreview()
    {
        if (selected == null)
        {
            cachedCleanJson = "";
            return;
        }
        try
        {
            cachedCleanJson = CleanJson.SimplifyObject(selected, CleanJson.Options.ForAI());
        }
        catch (Exception ex)
        {
            cachedCleanJson = "// Error generating clean JSON: " + ex.Message;
        }
    }

    // ------------------------------------------------------------ Reorderable Lists
    private void BuildLists()
    {
        // Crop list
        cropList = new ReorderableList(cropRows, typeof(CropRow), true, true, true, true);
        cropList.drawHeaderCallback = r =>
        {
            float w = r.width;
            EditorGUI.LabelField(new Rect(r.x, r.y, w * 0.45f, r.height), "Crop ScriptableObject");
            EditorGUI.LabelField(new Rect(r.x + w * 0.45f, r.y, w * 0.3f, r.height), "Stars (1-5★)");
            EditorGUI.LabelField(new Rect(r.x + w * 0.75f, r.y, w * 0.25f, r.height), "Target Amount");
        };

        cropList.drawElementCallback = (r, i, a, f) =>
        {
            if (i >= cropRows.Count) return;
            var row = cropRows[i];
            r.y += 2;
            r.height = EditorGUIUtility.singleLineHeight;
            float w = r.width;

            EditorGUI.BeginChangeCheck();

            // Crop SO Object Field
            var newSO = (CropSO)EditorGUI.ObjectField(new Rect(r.x, r.y, w * 0.45f - 6, r.height),
                row.cropSO, typeof(CropSO), false);

            if (newSO != row.cropSO)
            {
                row.cropSO = newSO;
                if (newSO != null && newSO.crop != null)
                    row.cropId = newSO.crop.CropID;
            }

            // Star slider
            row.stars = EditorGUI.IntSlider(new Rect(r.x + w * 0.45f, r.y, w * 0.3f - 6, r.height), row.stars, 1, 5);

            // Target Amount
            row.amount = Mathf.Max(1, EditorGUI.IntField(new Rect(r.x + w * 0.75f, r.y, w * 0.25f, r.height), row.amount));

            if (EditorGUI.EndChangeCheck())
            {
                SaveRowsToAsset();
            }
        };

        cropList.onAddCallback = _ =>
        {
            cropRows.Add(new CropRow());
            SaveRowsToAsset();
        };

        cropList.onRemoveCallback = l =>
        {
            if (l.index >= 0 && l.index < cropRows.Count) cropRows.RemoveAt(l.index);
            SaveRowsToAsset();
        };

        cropList.onReorderCallback = _ => SaveRowsToAsset();

        // Item list
        itemList = new ReorderableList(itemRows, typeof(ItemRow), true, true, true, true);
        itemList.drawHeaderCallback = r =>
        {
            float w = r.width;
            EditorGUI.LabelField(new Rect(r.x, r.y, w * 0.45f, r.height), "Item Prototype (ScriptableObject)");
            EditorGUI.LabelField(new Rect(r.x + w * 0.45f, r.y, w * 0.2f, r.height), "Use Type");
            EditorGUI.LabelField(new Rect(r.x + w * 0.65f, r.y, w * 0.2f, r.height), "Chance Weight");
            EditorGUI.LabelField(new Rect(r.x + w * 0.85f, r.y, w * 0.15f, r.height), "Prob %");
        };

        itemList.drawElementCallback = (r, i, a, f) =>
        {
            if (i >= itemRows.Count) return;
            var row = itemRows[i];
            r.y += 2;
            r.height = EditorGUIUtility.singleLineHeight;
            float w = r.width;

            EditorGUI.BeginChangeCheck();

            var newProto = (ItemPrototype)EditorGUI.ObjectField(new Rect(r.x, r.y, w * 0.45f - 6, r.height),
                row.item, typeof(ItemPrototype), false);

            if (newProto != row.item)
            {
                row.item = newProto;
                if (newProto != null && newProto.item != null)
                    row.itemId = newProto.item.Id;
            }

            // Use type badge
            string useTypeTag = GetItemUseBadge(row.item);
            GUI.Label(new Rect(r.x + w * 0.45f, r.y, w * 0.2f - 6, r.height), useTypeTag, EditorStyles.miniLabel);

            // Chance field
            row.chance = Mathf.Max(0f, EditorGUI.FloatField(new Rect(r.x + w * 0.65f, r.y, w * 0.2f - 6, r.height), row.chance));

            if (EditorGUI.EndChangeCheck())
            {
                SaveRowsToAsset();
            }

            // Probability display
            float total = TotalItemChance();
            float pct = total > 0f && row.item != null ? (row.chance / total * 100f) : 0f;
            EditorGUI.LabelField(new Rect(r.x + w * 0.85f, r.y, w * 0.15f, r.height), pct.ToString("0.#") + "%");
        };

        itemList.onAddCallback = _ =>
        {
            itemRows.Add(new ItemRow());
            SaveRowsToAsset();
        };

        itemList.onRemoveCallback = l =>
        {
            if (l.index >= 0 && l.index < itemRows.Count) itemRows.RemoveAt(l.index);
            SaveRowsToAsset();
        };

        itemList.onReorderCallback = _ => SaveRowsToAsset();
    }

    private string GetItemUseBadge(ItemPrototype proto)
    {
        if (proto == null || proto.item == null || proto.item.Uses == null || proto.item.Uses.Count == 0)
            return "(None)";

        var types = new List<string>();
        foreach (var u in proto.item.Uses)
        {
            if (u is AddPlantUse ap)
                types.Add(ap.PlantToAdd != null ? $"Seed:{ap.PlantToAdd.PlantName}" : "Seed");
            else if (u is ProviceResourceUse pr)
                types.Add($"{pr.Resource}+{pr.Amount}");
            else if (u is HarvestUse)
                types.Add("Harvest");
        }
        return string.Join(", ", types);
    }

    private float TotalItemChance()
    {
        float sum = 0f;
        foreach (var r in itemRows) if (r.item != null) sum += r.chance;
        return sum;
    }

    // ------------------------------------------------------------ GUI Layout
    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        DrawLevelSidebar();
        DrawMainArea();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLevelSidebar()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(220));

        if (GUILayout.Button("+ New Level", GUILayout.Height(28))) CreateNewLevel();
        search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);

        listScroll = EditorGUILayout.BeginScrollView(listScroll, "box");
        foreach (var lv in levels)
        {
            if (lv == null) continue;
            if (!string.IsNullOrEmpty(search) && lv.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            bool isSel = lv == selected;
            var style = new GUIStyle(EditorStyles.label) { fontStyle = isSel ? FontStyle.Bold : FontStyle.Normal };
            Rect r = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
            if (isSel) EditorGUI.DrawRect(r, new Color(0.24f, 0.48f, 0.9f, 0.35f));

            string label = $"[Lv.{lv.levelID}] {lv.name}";
            if (GUI.Button(r, label, style)) Select(lv);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void DrawMainArea()
    {
        EditorGUILayout.BeginVertical();

        var picked = (LevelSO)EditorGUILayout.ObjectField("Selected Level", selected, typeof(LevelSO), false);
        if (picked != selected) Select(picked);

        if (!HasTarget())
        {
            EditorGUILayout.HelpBox("Select a LevelSO on the left or click '+ New Level'.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        // Tabs
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Toggle(currentTab == EditorTab.ManualEdit, "Manual Level Editor", EditorStyles.toolbarButton))
            currentTab = EditorTab.ManualEdit;
        if (GUILayout.Toggle(currentTab == EditorTab.ProceduralGenerator, "Procedural Generator & Hardness", EditorStyles.toolbarButton))
            currentTab = EditorTab.ProceduralGenerator;
        if (GUILayout.Toggle(currentTab == EditorTab.AIJsonPreview, "AI Clean JSON Preview", EditorStyles.toolbarButton))
        {
            currentTab = EditorTab.AIJsonPreview;
            UpdateCleanJsonPreview();
        }
        EditorGUILayout.EndHorizontal();

        editScroll = EditorGUILayout.BeginScrollView(editScroll);

        if (currentTab == EditorTab.ManualEdit)
        {
            DrawManualEditor();
        }
        else if (currentTab == EditorTab.ProceduralGenerator)
        {
            DrawProceduralGenerator();
        }
        else
        {
            DrawAIJsonPreview();
        }

        EditorGUILayout.EndScrollView();

        DrawFooter();
        EditorGUILayout.EndVertical();
    }

    // ------------------------------------------------------------ Tab 1: Manual Editor
    private void DrawManualEditor()
    {
        so.Update();

        EditorGUILayout.LabelField("Level Configuration", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(levelIDProp);
        EditorGUILayout.PropertyField(timeBudgetProp);
        EditorGUILayout.PropertyField(moneyProp);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(farmMonoProp);
        if (selected.farmMono != null)
        {
            int slots = GetSlotCount(selected.farmMono);
            EditorGUILayout.LabelField($"({slots} slots)", GUILayout.Width(70));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(storingSpaceSOProp);
        if (selected.storingSpaceSO != null)
        {
            EditorGUILayout.LabelField($"({selected.storingSpaceSO.width}x{selected.storingSpaceSO.height})", GUILayout.Width(70));
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        so.ApplyModifiedProperties();

        // 3 Use Types Health Indicator
        DrawThreeTypesCoverageBanner();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Required Crops (Clean ID: stars, amount)", EditorStyles.boldLabel);
        DropArea<CropSO>("Drop CropSO assets here to add", AddCropSO);
        cropList.DoLayoutList();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Item & Chance Pool (Clean ID: chance)", EditorStyles.boldLabel);
        DropArea<ItemPrototype>("Drop ItemPrototype assets here to add", AddItemProto);
        itemList.DoLayoutList();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Total chance weight: {TotalItemChance():0.###}", EditorStyles.miniLabel);
        if (GUILayout.Button("Normalize to 1", GUILayout.Width(110))) NormalizeItemChances();
        if (GUILayout.Button("Equalize", GUILayout.Width(80))) EqualizeItemChances();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField(
            $"Clean serialized: {selected.requiredCrops.Count} crops, {selected.itemPool.Count} items. (Synced to runtime dictionaries)",
            EditorStyles.miniLabel);
    }

    private void DrawThreeTypesCoverageBanner()
    {
        int seedCount = 0;
        int resourceCount = 0;
        int harvestCount = 0;

        foreach (var row in itemRows)
        {
            if (row.item == null || row.item.item == null || row.item.item.Uses == null) continue;
            foreach (var u in row.item.item.Uses)
            {
                if (u is AddPlantUse) seedCount++;
                else if (u is ProviceResourceUse) resourceCount++;
                else if (u is HarvestUse) harvestCount++;
            }
        }

        bool allThreeCovered = seedCount > 0 && resourceCount > 0 && harvestCount > 0;
        MessageType msgType = allThreeCovered ? MessageType.Info : MessageType.Warning;

        string statusText = $"Required Use Types Coverage:\n" +
                            $"• Add Plant (Seeds): {seedCount} item(s)\n" +
                            $"• Resource Providers: {resourceCount} item(s)\n" +
                            $"• Harvest Tools: {harvestCount} item(s)";

        if (!allThreeCovered)
        {
            statusText += "\n[WARNING] Level is missing at least one critical item use type for gameplay loop!";
        }

        EditorGUILayout.HelpBox(statusText, msgType);
    }

    // ------------------------------------------------------------ Tab 2: Procedural Generator & Hardness Predictor
    private void DrawProceduralGenerator()
    {
        EditorGUILayout.LabelField("Procedural Level Generator", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        // Preset
        EditorGUI.BeginChangeCheck();
        proceduralDifficulty = (DifficultyPreset)EditorGUILayout.EnumPopup("Difficulty Preset", proceduralDifficulty);
        if (EditorGUI.EndChangeCheck())
        {
            ApplyDifficultyPreset(proceduralDifficulty);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Generation Constraints", EditorStyles.boldLabel);

        // Farm slots slider: 1 to 25
        proceduralFarmSlots = EditorGUILayout.IntSlider("Target Farm Slots (1-25)", proceduralFarmSlots, 1, 25);
        FarmMono matchedFarm = FindBestFarmPrefab(proceduralFarmSlots);
        EditorGUILayout.LabelField($"Selected Farm Prefab: {(matchedFarm != null ? matchedFarm.name + " (" + GetSlotCount(matchedFarm) + " slots)" : "None found")}", EditorStyles.miniLabel);

        // Crops settings
        proceduralCropCount = EditorGUILayout.IntSlider("Distinct Crop Types", proceduralCropCount, 1, Mathf.Max(1, allCropSOs.Count));
        EditorGUILayout.MinMaxSlider(new GUIContent("Star Range (★)"), ref proceduralStarMin, ref proceduralStarMax, 1f, 5f);
        EditorGUILayout.LabelField($"Stars: {Mathf.RoundToInt(proceduralStarMin)}★ to {Mathf.RoundToInt(proceduralStarMax)}★", EditorStyles.miniLabel);

        proceduralAmountMin = EditorGUILayout.IntField("Min Amount / Crop", proceduralAmountMin);
        proceduralAmountMax = EditorGUILayout.IntField("Max Amount / Crop", proceduralAmountMax);

        // Storing Space settings
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Procedural Storing Space", EditorStyles.boldLabel);
        proceduralGridWidth = EditorGUILayout.IntSlider("Grid Width (2-6)", proceduralGridWidth, 2, 6);
        proceduralGridHeight = EditorGUILayout.IntSlider("Grid Height (2-6)", proceduralGridHeight, 2, 6);
        proceduralObstacle = (ObstaclePattern)EditorGUILayout.EnumPopup("Obstacle Pattern", proceduralObstacle);

        EditorGUILayout.Space(10);
        GUI.backgroundColor = new Color(0.4f, 0.85f, 0.5f);
        if (GUILayout.Button("⚡ Generate Procedural Level", GUILayout.Height(34)))
        {
            GenerateProceduralLevel();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(12);

        // Hardness Predictor & Recommendations
        DrawHardnessPredictorAndRecommendations();
    }

    private void ApplyDifficultyPreset(DifficultyPreset preset)
    {
        switch (preset)
        {
            case DifficultyPreset.Casual:
                proceduralFarmSlots = 12;
                proceduralCropCount = 1;
                proceduralStarMin = 1f;
                proceduralStarMax = 2f;
                proceduralAmountMin = 2;
                proceduralAmountMax = 4;
                proceduralGridWidth = 5;
                proceduralGridHeight = 5;
                proceduralObstacle = ObstaclePattern.OpenGrid;
                break;
            case DifficultyPreset.Easy:
                proceduralFarmSlots = 9;
                proceduralCropCount = 2;
                proceduralStarMin = 1f;
                proceduralStarMax = 2f;
                proceduralAmountMin = 3;
                proceduralAmountMax = 6;
                proceduralGridWidth = 4;
                proceduralGridHeight = 4;
                proceduralObstacle = ObstaclePattern.OpenGrid;
                break;
            case DifficultyPreset.Medium:
                proceduralFarmSlots = 9;
                proceduralCropCount = 2;
                proceduralStarMin = 2f;
                proceduralStarMax = 3f;
                proceduralAmountMin = 4;
                proceduralAmountMax = 8;
                proceduralGridWidth = 4;
                proceduralGridHeight = 4;
                proceduralObstacle = ObstaclePattern.CornersCut;
                break;
            case DifficultyPreset.Hard:
                proceduralFarmSlots = 6;
                proceduralCropCount = 3;
                proceduralStarMin = 2f;
                proceduralStarMax = 4f;
                proceduralAmountMin = 6;
                proceduralAmountMax = 12;
                proceduralGridWidth = 4;
                proceduralGridHeight = 3;
                proceduralObstacle = ObstaclePattern.CenterIsland;
                break;
            case DifficultyPreset.Nightmare:
                proceduralFarmSlots = 4;
                proceduralCropCount = 4;
                proceduralStarMin = 3f;
                proceduralStarMax = 5f;
                proceduralAmountMin = 8;
                proceduralAmountMax = 16;
                proceduralGridWidth = 3;
                proceduralGridHeight = 3;
                proceduralObstacle = ObstaclePattern.Checkerboard;
                break;
        }
    }

    private FarmMono FindBestFarmPrefab(int targetSlots)
    {
        if (allFarmPrefabs.Count == 0) return null;
        FarmMono best = allFarmPrefabs[0];
        int bestDiff = Mathf.Abs(GetSlotCount(best) - targetSlots);

        foreach (var fm in allFarmPrefabs)
        {
            int diff = Mathf.Abs(GetSlotCount(fm) - targetSlots);
            if (diff < bestDiff)
            {
                best = fm;
                bestDiff = diff;
            }
        }
        return best;
    }

    private void GenerateProceduralLevel()
    {
        if (!HasTarget()) return;
        Undo.RecordObject(selected, "Generate Procedural Level");

        // 1. Assign FarmMono matching slot count
        selected.farmMono = FindBestFarmPrefab(proceduralFarmSlots);

        // 2. Procedural Storing Space
        if (selected.storingSpaceSO == null)
        {
            selected.storingSpaceSO = CreateInstance<StoringSpaceSO>();
            string path = AssetDatabase.GenerateUniqueAssetPath($"Assets/!_HarvestGrid/ScriptableObjects/StoringSpace_Lv{selected.levelID}.asset");
            AssetDatabase.CreateAsset(selected.storingSpaceSO, path);
        }

        selected.storingSpaceSO.width = proceduralGridWidth;
        selected.storingSpaceSO.height = proceduralGridHeight;
        selected.storingSpaceSO.CreateGrid();
        ApplyObstaclePattern(selected.storingSpaceSO.storingSpace, proceduralObstacle);
        EditorUtility.SetDirty(selected.storingSpaceSO);

        // 3. Procedural Crop Requirements
        cropRows.Clear();
        var shuffledCrops = allCropSOs.OrderBy(_ => Random.value).Take(Mathf.Clamp(proceduralCropCount, 1, allCropSOs.Count)).ToList();

        int totalUnitsRequired = 0;
        var requiredResources = new HashSet<Resource>();

        int starMin = Mathf.RoundToInt(proceduralStarMin);
        int starMax = Mathf.RoundToInt(proceduralStarMax);

        foreach (var cso in shuffledCrops)
        {
            int stars = Random.Range(starMin, starMax + 1);
            int amount = Random.Range(Mathf.Min(proceduralAmountMin, proceduralAmountMax), Mathf.Max(proceduralAmountMin, proceduralAmountMax) + 1);
            totalUnitsRequired += amount;

            cropRows.Add(new CropRow
            {
                cropSO = cso,
                cropId = cso.crop.CropID,
                stars = stars,
                amount = amount
            });

            // Inspect needed resources for this crop's plant
            FindRequiredResourcesForCrop(cso, requiredResources);
        }

        // 4. Procedural Item Pool (Guaranteed 3 types of uses)
        itemRows.Clear();

        // Type A: Add Plant (Seeds for all required crops)
        foreach (var cso in shuffledCrops)
        {
            ItemPrototype seedProto = FindSeedForItem(cso.crop.CropID);
            if (seedProto != null && !itemRows.Exists(r => r.item == seedProto))
            {
                itemRows.Add(new ItemRow { item = seedProto, itemId = seedProto.item.Id, chance = 1.0f });
            }
        }

        // Type B: Resource Providers (for every resource needed by selected plants)
        if (requiredResources.Count == 0) requiredResources.Add(Resource.Water);
        foreach (var res in requiredResources)
        {
            var providers = FindProvidersForResource(res);
            foreach (var prov in providers.Take(2))
            {
                if (!itemRows.Exists(r => r.item == prov))
                {
                    itemRows.Add(new ItemRow { item = prov, itemId = prov.item.Id, chance = 1.2f });
                }
            }
        }

        // Type C: Harvest Tools
        var harvestTools = allItemPrototypes.Where(p => p.item != null && p.item.Uses != null && p.item.Uses.OfType<HarvestUse>().Any()).ToList();
        if (harvestTools.Count > 0)
        {
            foreach (var tool in harvestTools.Take(2))
            {
                if (!itemRows.Exists(r => r.item == tool))
                {
                    itemRows.Add(new ItemRow { item = tool, itemId = tool.item.Id, chance = 0.8f });
                }
            }
        }

        NormalizeItemChances();

        // 5. Auto time budget and money
        int farmSlotCount = GetSlotCount(selected.farmMono);
        if (farmSlotCount <= 0) farmSlotCount = 4;
        float baseTimePerCrop = 15f;
        float calculatedTime = Mathf.Max(60f, (totalUnitsRequired / (float)farmSlotCount) * baseTimePerCrop * 2.2f + 40f);
        selected.timeBudget = Mathf.Round(calculatedTime / 10f) * 10f;
        selected.money = Mathf.Clamp(50 + (proceduralFarmSlots * 5), 50, 300);

        SaveRowsToAsset();
        EditorUtility.SetDirty(selected);
        AssetDatabase.SaveAssets();

        so.Update();
        Debug.Log($"[LevelEditor] Generated Procedural Level {selected.levelID} with {cropRows.Count} crops and {itemRows.Count} items.");
    }

    private void FindRequiredResourcesForCrop(CropSO cso, HashSet<Resource> resSet)
    {
        if (cso == null || cso.crop == null) return;
        string cid = cso.crop.CropID;

        foreach (var proto in allItemPrototypes)
        {
            if (proto == null || proto.item == null || proto.item.Uses == null) continue;
            foreach (var u in proto.item.Uses.OfType<AddPlantUse>())
            {
                if (u.PlantToAdd != null && u.PlantToAdd.Crop != null && u.PlantToAdd.Crop.CropID == cid)
                {
                    if (u.PlantToAdd.Stages != null)
                    {
                        foreach (var st in u.PlantToAdd.Stages)
                        {
                            if (st.RequiredResources != null)
                            {
                                foreach (var k in st.RequiredResources.Keys) resSet.Add(k);
                            }
                        }
                    }
                }
            }
        }
    }

    private ItemPrototype FindSeedForItem(string cropId)
    {
        return allItemPrototypes.FirstOrDefault(p =>
            p != null && p.item != null && p.item.Uses != null &&
            p.item.Uses.OfType<AddPlantUse>().Any(u => u.PlantToAdd != null && u.PlantToAdd.Crop != null && u.PlantToAdd.Crop.CropID == cropId));
    }

    private List<ItemPrototype> FindProvidersForResource(Resource res)
    {
        return allItemPrototypes.Where(p =>
            p != null && p.item != null && p.item.Uses != null &&
            p.item.Uses.OfType<ProviceResourceUse>().Any(u => u.Resource == res)).ToList();
    }

    private void ApplyObstaclePattern(StoringSpace space, ObstaclePattern pattern)
    {
        if (space == null || space.Cells == null) return;
        int w = space.Cells.GetLength(0);
        int h = space.Cells.GetLength(1);

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                space.Cells[x, y] = StoringCellType.Available;

        switch (pattern)
        {
            case ObstaclePattern.CornersCut:
                if (w >= 3 && h >= 3)
                {
                    space.Cells[0, 0] = StoringCellType.Unavailable;
                    space.Cells[w - 1, 0] = StoringCellType.Unavailable;
                    space.Cells[0, h - 1] = StoringCellType.Unavailable;
                    space.Cells[w - 1, h - 1] = StoringCellType.Unavailable;
                }
                break;
            case ObstaclePattern.CenterIsland:
                if (w >= 3 && h >= 3)
                {
                    space.Cells[w / 2, h / 2] = StoringCellType.Unavailable;
                }
                break;
            case ObstaclePattern.Checkerboard:
                for (int x = 0; x < w; x++)
                    for (int y = 0; y < h; y++)
                        if ((x + y) % 3 == 0) space.Cells[x, y] = StoringCellType.Unavailable;
                break;
            case ObstaclePattern.BorderFrame:
                if (w >= 4 && h >= 4)
                {
                    space.Cells[1, 1] = StoringCellType.Unavailable;
                    space.Cells[w - 2, 1] = StoringCellType.Unavailable;
                }
                break;
        }
    }

    // ------------------------------------------------------------ Hardness Predictor & Recommendations
    private void DrawHardnessPredictorAndRecommendations()
    {
        EditorGUILayout.LabelField("Level Hardness & Developer Recommendations", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        int totalUnits = cropRows.Sum(r => r.amount);
        int totalStars = cropRows.Sum(r => r.stars * r.amount);
        int farmSlots = selected.farmMono != null ? GetSlotCount(selected.farmMono) : 4;
        if (farmSlots <= 0) farmSlots = 4;

        float seedProb = 0f, resourceProb = 0f, harvestProb = 0f;
        float totalChance = TotalItemChance();

        var resourcesProvided = new HashSet<Resource>();
        foreach (var row in itemRows)
        {
            if (row.item == null || row.item.item == null || row.item.item.Uses == null) continue;
            float weight = totalChance > 0f ? row.chance / totalChance : 0f;
            foreach (var u in row.item.item.Uses)
            {
                if (u is AddPlantUse) seedProb += weight;
                else if (u is ProviceResourceUse pr)
                {
                    resourceProb += weight;
                    resourcesProvided.Add(pr.Resource);
                }
                else if (u is HarvestUse) harvestProb += weight;
            }
        }

        var resourcesDemanded = new HashSet<Resource>();
        foreach (var r in cropRows)
        {
            if (r.cropSO != null) FindRequiredResourcesForCrop(r.cropSO, resourcesDemanded);
        }

        // Hardness score calculation (0 - 100)
        float score = 0f;
        score += Mathf.Clamp(totalUnits * 2.5f, 0, 30);
        score += Mathf.Clamp((totalStars / Mathf.Max(1f, totalUnits) - 1f) * 15f, 0, 20);

        // Farm slot throughput pressure
        float throughputRatio = (float)totalUnits / farmSlots;
        score += Mathf.Clamp(throughputRatio * 5f, 0, 25);

        // Tool distribution balance penalty
        if (harvestProb < 0.12f) score += 15f;
        if (resourceProb < 0.20f) score += 10f;
        if (seedProb < 0.20f) score += 10f;

        // Missing resource penalty
        int missingResourceCount = resourcesDemanded.Count(res => !resourcesProvided.Contains(res));
        if (missingResourceCount > 0) score += 35f;

        // Time pressure penalty
        float recommendedTime = Mathf.Max(45f, throughputRatio * 25f + 35f);
        if (selected.timeBudget < recommendedTime * 0.75f) score += 15f;
        else if (selected.timeBudget > recommendedTime * 1.5f) score -= 10f;

        score = Mathf.Clamp(score, 5f, 100f);

        // Score display
        Color badgeColor;
        string tierLabel;
        if (score < 25f) { badgeColor = new Color(0.3f, 0.8f, 0.3f); tierLabel = "Casual / Easy"; }
        else if (score < 50f) { badgeColor = new Color(0.2f, 0.6f, 0.9f); tierLabel = "Moderate / Normal"; }
        else if (score < 75f) { badgeColor = new Color(0.95f, 0.65f, 0.1f); tierLabel = "Challenging / Hard"; }
        else { badgeColor = new Color(0.9f, 0.25f, 0.25f); tierLabel = "Nightmare / Extreme"; }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Predicted Hardness Score: {score:0}/100", EditorStyles.boldLabel);
        var oldBg = GUI.backgroundColor;
        GUI.backgroundColor = badgeColor;
        GUILayout.Box($"[{tierLabel}]", GUILayout.Width(160));
        GUI.backgroundColor = oldBg;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"• Workload: {totalUnits} crops ({totalStars} total star points across {cropRows.Count} types)");
        EditorGUILayout.LabelField($"• Throughput: {throughputRatio:0.1} batches needed ({farmSlots} farm slots available)");
        EditorGUILayout.LabelField($"• Shop Distribution: Seeds {seedProb * 100:0}%, Resources {resourceProb * 100:0}%, Tools {harvestProb * 100:0}%");
        EditorGUILayout.LabelField($"• Time Budget: {selected.timeBudget:0}s (Recommended: ~{recommendedTime:0}s)");

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Recommendations & Issues:", EditorStyles.boldLabel);

        var recommendations = new List<string>();

        if (missingResourceCount > 0)
        {
            var missing = resourcesDemanded.Where(res => !resourcesProvided.Contains(res));
            recommendations.Add($"[CRITICAL] Missing resource provider! Plant(s) require [{string.Join(", ", missing)}], but no shop item provides it.");
        }

        if (harvestProb <= 0f)
            recommendations.Add("[CRITICAL] No Harvest tool in item pool! Player will be unable to harvest fully grown crops.");
        else if (harvestProb < 0.12f)
            recommendations.Add("[WARNING] Harvest tool chance is very low (<12%). Plots may remain clogged with mature crops.");

        if (seedProb < 0.20f)
            recommendations.Add("[WARNING] Seed drop chance is under 20%. Players may struggle to obtain required crops.");

        if (throughputRatio > 4f)
            recommendations.Add($"[WARNING] High bottleneck: {totalUnits} crops required on only {farmSlots} slots. Consider expanding farm slots or increasing harvest tool chance.");

        if (selected.timeBudget < recommendedTime * 0.75f)
            recommendations.Add($"[SUGGESTION] Time budget ({selected.timeBudget}s) is very tight for {totalUnits} crops. Suggest increasing to ~{recommendedTime:0}s.");

        if (recommendations.Count == 0)
        {
            EditorGUILayout.HelpBox("✓ Level balance looks great! All 3 use types are covered and throughput is within good parameters.", MessageType.Info);
        }
        else
        {
            foreach (var rec in recommendations)
            {
                MessageType mType = rec.StartsWith("[CRITICAL]") ? MessageType.Error :
                                   rec.StartsWith("[WARNING]") ? MessageType.Warning : MessageType.Info;
                EditorGUILayout.HelpBox(rec, mType);
            }

            if (GUILayout.Button("✨ Apply Recommendations (Auto-Balance)"))
            {
                ApplyBalanceRecommendations(resourcesDemanded, recommendedTime);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void ApplyBalanceRecommendations(HashSet<Resource> neededResources, float recommendedTime)
    {
        Undo.RecordObject(selected, "Apply Balance Recommendations");

        // 1. Fix time budget
        selected.timeBudget = Mathf.Round(recommendedTime / 10f) * 10f;

        // 2. Add missing resources
        foreach (var res in neededResources)
        {
            var providers = FindProvidersForResource(res);
            if (providers.Count > 0 && !itemRows.Exists(r => providers.Contains(r.item)))
            {
                var p = providers[0];
                itemRows.Add(new ItemRow { item = p, itemId = p.item.Id, chance = 1f });
            }
        }

        // 3. Ensure at least one harvest tool
        bool hasHarvest = itemRows.Any(r => r.item != null && r.item.item != null && r.item.item.Uses != null && r.item.item.Uses.OfType<HarvestUse>().Any());
        if (!hasHarvest)
        {
            var harvestTools = allItemPrototypes.Where(p => p.item != null && p.item.Uses != null && p.item.Uses.OfType<HarvestUse>().Any()).ToList();
            if (harvestTools.Count > 0)
            {
                var tool = harvestTools[0];
                itemRows.Add(new ItemRow { item = tool, itemId = tool.item.Id, chance = 1f });
            }
        }

        NormalizeItemChances();
        SaveRowsToAsset();
        EditorUtility.SetDirty(selected);
        AssetDatabase.SaveAssets();
        so.Update();
    }

    // ------------------------------------------------------------ Tab 3: AI Clean JSON Preview
    private void DrawAIJsonPreview()
    {
        EditorGUILayout.LabelField("AI Clean JSON Payload (GameJson.CleanJson)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This is the exact JSON structure serialized for AI. All Unity object references, instanceIDs, and visual assets are omitted, keeping only pure StateEntity ID strings.", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Preview", GUILayout.Width(130))) UpdateCleanJsonPreview();
        if (GUILayout.Button("Copy to Clipboard", GUILayout.Width(150)))
        {
            EditorGUIUtility.systemCopyBuffer = cachedCleanJson;
            ShowNotification(new GUIContent("Clean JSON copied to clipboard!"));
        }
        EditorGUILayout.LabelField($"Length: {cachedCleanJson.Length} chars (~{cachedCleanJson.Length / 4} tokens)", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        previewScroll = EditorGUILayout.BeginScrollView(previewScroll, "box", GUILayout.MinHeight(260));
        EditorGUILayout.TextArea(cachedCleanJson, EditorStyles.textArea);
        EditorGUILayout.EndScrollView();
    }

    // ------------------------------------------------------------ Footer Actions
    private void DrawFooter()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Ping Asset")) EditorGUIUtility.PingObject(selected);
        if (GUILayout.Button("Reload")) ReloadRowsFromAsset();
        if (GUILayout.Button("Duplicate")) DuplicateLevel();
        if (GUILayout.Button("Delete")) DeleteLevel();

        GUI.backgroundColor = new Color(0.45f, 0.85f, 0.45f);
        if (GUILayout.Button("Save Level", GUILayout.Width(120)))
        {
            SaveRowsToAsset();
            EditorUtility.SetDirty(selected);
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("Level saved successfully!"));
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    // ------------------------------------------------------------ Asset Operations
    private void CreateNewLevel()
    {
        string path = EditorUtility.SaveFilePanelInProject("New Level", "LevelSO", "asset", "Choose where to save the level");
        if (string.IsNullOrEmpty(path)) return;

        var level = CreateInstance<LevelSO>();
        int.TryParse(System.IO.Path.GetFileNameWithoutExtension(path).Replace("LevelSO", "").Trim(), out level.levelID);
        level.EnsureInitialized();

        AssetDatabase.CreateAsset(level, path);
        AssetDatabase.SaveAssets();
        RefreshAssetDatabases();
        Select(level);
    }

    private void DuplicateLevel()
    {
        if (!HasTarget()) return;
        string src = AssetDatabase.GetAssetPath(selected);
        string dst = AssetDatabase.GenerateUniqueAssetPath(src);
        if (AssetDatabase.CopyAsset(src, dst))
        {
            RefreshAssetDatabases();
            Select(AssetDatabase.LoadAssetAtPath<LevelSO>(dst));
        }
    }

    private void DeleteLevel()
    {
        if (!HasTarget()) return;
        if (!EditorUtility.DisplayDialog("Delete Level", $"Delete '{selected.name}'? This cannot be undone.", "Delete", "Cancel")) return;
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(selected));
        selected = null;
        RefreshAssetDatabases();
    }

    private void AddCropSO(CropSO cropSO)
    {
        if (!HasTarget() || cropSO == null || cropSO.crop == null) return;
        var existing = cropRows.Find(r => r.cropSO == cropSO && r.stars == 1);
        if (existing != null) existing.amount++;
        else cropRows.Add(new CropRow { cropSO = cropSO, cropId = cropSO.crop.CropID });
        SaveRowsToAsset();
    }

    private void AddItemProto(ItemPrototype proto)
    {
        if (!HasTarget() || proto == null || proto.item == null) return;
        if (itemRows.Exists(r => r.item == proto)) return;
        itemRows.Add(new ItemRow { item = proto, itemId = proto.item.Id, chance = 1f });
        NormalizeItemChances();
        SaveRowsToAsset();
    }

    private void NormalizeItemChances()
    {
        float total = TotalItemChance();
        if (total <= 0f) return;
        foreach (var r in itemRows) r.chance /= total;
        SaveRowsToAsset();
    }

    private void EqualizeItemChances()
    {
        int count = itemRows.Count(r => r.item != null);
        if (count == 0) return;
        foreach (var r in itemRows) r.chance = 1f / count;
        SaveRowsToAsset();
    }

    private static void DropArea<T>(string label, Action<T> onDrop) where T : Object
    {
        Rect r = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
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
            {
                if (obj is T t) onDrop(t);
            }
            e.Use();
        }
    }
}
