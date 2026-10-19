using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LgTyLib.Core;

public class GoalItemSuggestionManager : BaseSingleton<GoalItemSuggestionManager>
{
    [Header("Configuration")]
    [Tooltip("Color of the ghost item suggesting what to buy.")]
    public Color suggestionColor = new Color(1f, 0.8f, 0.2f, 0.6f); // Yellow-ish transparent
    public CropItemRegistry registry;
    
    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Coroutine suggestionRoutine;

    // State tracking for multiple cumulative suggestions
    private Dictionary<string, GameObject> activeGhosts = new Dictionary<string, GameObject>();
    private Dictionary<string, List<Vector2Int>> ghostOccupiedCells = new Dictionary<string, List<Vector2Int>>();

    protected override void Awake()
    {
        base.Awake();
        if (registry == null)
        {
            registry = Resources.Load<CropItemRegistry>("CropItemRegistry");
        }
    }

    private IEnumerator Start()
    {
        if (InventoryMono.HasInstance && InventoryMono.Instance.Inventory != null)
        {
            InventoryMono.Instance.Inventory.OnItemsChanged += OnInventoryChanged;
            InventoryMono.Instance.Inventory.OnCropsChanged += OnCropsChanged;
        }

        // Delay first check slightly to let grid initialize
        yield return new WaitForSeconds(0.5f);
        
        if (debugLogs) Debug.Log("[GoalItemSuggestion] Initializing suggestion.");
        RecalculateSuggestion();
        
        suggestionRoutine = StartCoroutine(SuggestionLoop());
    }

    protected override void OnDestroy()
    {
        if (suggestionRoutine != null)
        {
            StopCoroutine(suggestionRoutine);
            suggestionRoutine = null;
        }

        if (InventoryMono.HasInstance && InventoryMono.Instance.Inventory != null)
        {
            InventoryMono.Instance.Inventory.OnItemsChanged -= OnInventoryChanged;
            InventoryMono.Instance.Inventory.OnCropsChanged -= OnCropsChanged;
        }
        base.OnDestroy();
    }

    private void OnInventoryChanged(IReadOnlyList<Item> items)
    {
        if (debugLogs) Debug.Log("[GoalItemSuggestion] Inventory changed -> recalculating immediately");
        RecalculateSuggestion();
    }

    private void OnCropsChanged(IReadOnlyDictionary<Crop, int> crops)
    {
        if (debugLogs) Debug.Log("[GoalItemSuggestion] Crops changed -> recalculating immediately");
        RecalculateSuggestion(); // Goal completion check
    }

    private IEnumerator SuggestionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(45f);
            if (debugLogs) Debug.Log("[GoalItemSuggestion] Timer tick (45s) -> Checking suggestions...");
            RecalculateSuggestion();
        }
    }

    public void RecalculateSuggestion()
    {
        if (registry == null) return;

        var levelSO = GameplayScene.Instance?.LevelSO;
        if (levelSO == null || levelSO.requiringCrops == null || levelSO.requiringCrops.Count == 0)
        {
            ClearAllSuggestions();
            return;
        }

        var inventoryItems = InventoryMono.Instance.Inventory.GetItemList();
        var currentCrops = InventoryMono.Instance.Inventory.Crops;

        ItemRole[] priorityRoles = new[] { 
            ItemRole.Seed, 
            ItemRole.Watering, 
            ItemRole.HarvestTool, 
            ItemRole.Fertilizer 
        };

        HashSet<string> missingItemIds = new HashSet<string>();
        List<ItemPrototype> missingCandidates = new List<ItemPrototype>();
        bool goalIncomplete = false;

        foreach (var reqCropKvp in levelSO.requiringCrops)
        {
            Crop goalCrop = reqCropKvp.Key;
            if (goalCrop == null) continue;
            
            int goalAmount = reqCropKvp.Value;
            
            // Safely check harvested amount by CropID and Stars
            int currentAmount = 0;
            if (currentCrops != null)
            {
                foreach (var kvp in currentCrops)
                {
                    if (kvp.Key != null && kvp.Key.CropID == goalCrop.CropID && kvp.Key.Stars == goalCrop.Stars)
                    {
                        currentAmount += kvp.Value;
                    }
                }
            }

            // If goal already reached for this crop, skip it
            if (currentAmount >= goalAmount)
                continue;

            goalIncomplete = true;

            foreach (var role in priorityRoles)
            {
                var candidates = registry.GetItemsForCrop(goalCrop, role);
                if (candidates == null || candidates.Count == 0) continue;

                // Safely check if we have AT LEAST ONE item from the candidates in inventory
                bool hasItem = inventoryItems != null && inventoryItems.Any(invItem => 
                    invItem != null && candidates.Any(proto => 
                        proto != null && proto.item != null && proto.item.Id == invItem.Id));
                
                if (!hasItem)
                {
                    // Player is missing an item for this role.
                    var candidate = candidates.FirstOrDefault(c => c != null && c.item != null);
                    if (candidate != null)
                    {
                        missingItemIds.Add(candidate.item.Id);
                        if (!missingCandidates.Any(c => c.item.Id == candidate.item.Id))
                        {
                            missingCandidates.Add(candidate);
                        }
                    }
                }
            }
        }

        // If all goals are met, clear all and return
        if (!goalIncomplete)
        {
            if (debugLogs && activeGhosts.Count > 0) Debug.Log("[GoalItemSuggestion] Goal completed -> clearing all suggestions");
            ClearAllSuggestions();
            return;
        }

        // Remove ghosts that are no longer missing (e.g., player bought them)
        List<string> ghostsToRemove = new List<string>();
        foreach (var ghostId in activeGhosts.Keys)
        {
            if (!missingItemIds.Contains(ghostId))
            {
                ghostsToRemove.Add(ghostId);
            }
        }

        foreach (var id in ghostsToRemove)
        {
            if (debugLogs) Debug.Log($"[GoalItemSuggestion] Item purchased or no longer needed: {id}. Removing suggestion.");
            ClearSuggestionForItem(id);
        }

        // Find the FIRST missing item that hasn't been suggested yet, and add exactly ONE new suggestion
        ItemPrototype nextToSuggest = missingCandidates.FirstOrDefault(c => !activeGhosts.ContainsKey(c.item.Id));
        if (nextToSuggest != null)
        {
            if (debugLogs) Debug.Log($"[GoalItemSuggestion] Adding suggestion: {nextToSuggest.item.Name}");
            ProcessSuggestionForItem(nextToSuggest);
        }
    }

    private void ProcessSuggestionForItem(ItemPrototype prototype)
    {
        if (prototype == null || prototype.item == null || prototype.item.Footprint == null) return;

        var storingSpace = StoringSpaceMono.Instance?.StoringSpace;
        var storingHelper = StoringSpaceMono.Instance?.gridLayoutGroupHelper;
        if (storingSpace == null || storingHelper == null) return;

        // Extract basic grid availability
        int gridWidth = storingSpace.Cells.GetLength(0);
        int gridHeight = storingSpace.Cells.GetLength(1);
        bool[,] grid = new bool[gridWidth, gridHeight];
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (storingSpace.Cells[x, y] == StoringCellType.Unavailable || 
                    storingSpace.Cells[x, y] == StoringCellType.Occupied)
                {
                    grid[x, y] = true;
                }
            }
        }

        // Add cells currently reserved by other active visual ghosts
        foreach (var cellList in ghostOccupiedCells.Values)
        {
            foreach (var cell in cellList)
            {
                if (cell.x >= 0 && cell.x < gridWidth && cell.y >= 0 && cell.y < gridHeight)
                {
                    grid[cell.x, cell.y] = true;
                }
            }
        }

        Vector2Int? bestPivot = null;
        int bestRotation = 0;
        Footprint finalFootprint = null;
        bool found = false;

        // Simple First-Fit algorithm for finding a place to show the suggestion
        for (int rot = 0; rot < 4; rot++)
        {
            Footprint rotated = prototype.item.Footprint.Clone();
            for (int r = 0; r < rot; r++) rotated.Rotate();

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    var pivot = new Vector2Int(x, y);
                    if (CanPlace(grid, gridWidth, gridHeight, rotated, pivot))
                    {
                        bestPivot = pivot;
                        bestRotation = rot;
                        finalFootprint = rotated;
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }
            if (found) break;
        }

        if (found)
        {
            GameObject newGhost = CreateVisualGhost(prototype.item, finalFootprint, bestPivot.Value, bestRotation, storingHelper);
            
            activeGhosts[prototype.item.Id] = newGhost;
            
            // Record the cells this ghost occupies so future suggestions avoid them
            var occupiedPositions = finalFootprint.ToSpace(bestPivot.Value).ToList();
            ghostOccupiedCells[prototype.item.Id] = occupiedPositions;
        }
        else
        {
            if (debugLogs) Debug.LogWarning($"[GoalItemSuggestion] No valid placement found for {prototype.item.Name}. Suggestion hidden.");
        }
    }

    private bool CanPlace(bool[,] grid, int gridWidth, int gridHeight, Footprint footprint, Vector2Int pivot)
    {
        var positions = footprint.ToSpace(pivot);
        foreach (var pos in positions)
        {
            if (pos.x < 0 || pos.x >= gridWidth || pos.y < 0 || pos.y >= gridHeight)
                return false;
            if (grid[pos.x, pos.y])
                return false;
        }
        return true;
    }

    private GameObject CreateVisualGhost(Item item, Footprint footprint, Vector2Int pivot, int rotation, GridLayoutGroupHelper storingHelper)
    {
        Transform parentCanvas = storingHelper.transform.parent;
        Transform overlay = parentCanvas.Find("SuggestionOverlay");
        
        // Reuse overlay from InventorySortUIMono if it exists, else create it
        if (overlay == null)
        {
            var overlayGo = new GameObject("SuggestionOverlay");
            overlayGo.transform.SetParent(parentCanvas, false);
            overlayGo.transform.SetSiblingIndex(storingHelper.transform.GetSiblingIndex() + 1);
            var overlayRt = overlayGo.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            overlay = overlayGo.transform;
        }

        GameObject ghostGo = new GameObject($"GoalSuggest_{item.Name}");
        ghostGo.transform.SetParent(overlay, false);
        
        var rectTransform = ghostGo.AddComponent<RectTransform>();
        var image = ghostGo.AddComponent<Image>();
        image.sprite = item.Icon;
        image.color = suggestionColor;
        image.raycastTarget = false; // Important: Do not block drags

        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localRotation = Quaternion.Euler(0, 0, rotation * -90f);

        bool[,] req = footprint.Requiring;
        int cols = req.GetLength(0);
        int rows = req.GetLength(1);

        Vector2 cellSize = storingHelper.Grid.cellSize;
        Vector2 spacing = storingHelper.Grid.spacing;

        float finalWidth = cols * cellSize.x + Mathf.Max(0, cols - 1) * spacing.x;
        float finalHeight = rows * cellSize.y + Mathf.Max(0, rows - 1) * spacing.y;

        rectTransform.sizeDelta = new Vector2(finalWidth, finalHeight);

        var blCorner = storingHelper.GetCellWorldCorners(pivot.x, pivot.y).BottomLeft;
        var trCorner = storingHelper.GetCellWorldCorners(pivot.x + cols - 1, pivot.y + rows - 1).TopRight;

        Vector3 footprintCenterWorld = (blCorner + trCorner) * 0.5f;
        rectTransform.position = footprintCenterWorld;

        var pulse = ghostGo.AddComponent<GhostPulseAnimation>();
        if (pulse != null)
        {
            pulse.image = image;
            pulse.baseColor = image.color;
        }

        return ghostGo;
    }

    public void ClearSuggestionForItem(string itemId)
    {
        if (activeGhosts.TryGetValue(itemId, out GameObject ghost))
        {
            if (ghost != null) Destroy(ghost);
            activeGhosts.Remove(itemId);
        }
        ghostOccupiedCells.Remove(itemId);
    }

    public void ClearAllSuggestions()
    {
        foreach (var ghost in activeGhosts.Values)
        {
            if (ghost != null) Destroy(ghost);
        }
        activeGhosts.Clear();
        ghostOccupiedCells.Clear();
    }
}
