using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using HarvestGrid.Services;
using LgTyLib.Core;


/// <summary>
/// Renders the preview "ghosts" for the Sort Best suggestion.
/// Suggests placement automatically for newly purchased unplaced items.
/// </summary>
public partial class InventorySortUIMono : BaseSingleton<InventorySortUIMono>
{
    [Header("Config")]
    public Color normalGhostColor = new Color(1f, 1f, 1f, 0.5f);
    public Color matchedGhostColor = new Color(0.5f, 1f, 0.5f, 0.7f); // Green-ish highlight for matched requirements

    private List<GameObject> activeGhosts = new List<GameObject>();
    public SortSuggestion CurrentSuggestion { get; private set; }

    private bool isBatchSorting;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnDragBegin += HandleDragBegin;
        }

        if (InventoryMono.HasInstance && InventoryMono.Instance.Inventory != null)
        {
            InventoryMono.Instance.Inventory.OnItemsChanged += HandleItemsChanged;
        }

        ItemWithFootprintMono.OnItemPurchased += HandleItemPurchased;
    }

    protected override void OnDestroy()
    {
        if (InputManager.HasInstance)
        {
            InputManager.Instance.OnDragBegin -= HandleDragBegin;
        }

        if (InventoryMono.HasInstance && InventoryMono.Instance.Inventory != null)
        {
            InventoryMono.Instance.Inventory.OnItemsChanged -= HandleItemsChanged;
        }

        ItemWithFootprintMono.OnItemPurchased -= HandleItemPurchased;

        ClearSuggestions();
        base.OnDestroy();
    }

    private void HandleDragBegin(Vector2 pos)
    {
        // Hide/Clear suggestions when player manually drags something
        ClearSuggestions();
    }

    private void HandleItemPurchased(ItemWithFootprintMono item)
    {
        // Auto trigger sort best when a new item is purchased
        if (isBatchSorting) return;
        TriggerSortBest();
    }

    private void HandleItemsChanged(IReadOnlyList<Item> items)
    {
        // Don't recompute suggestions for every single remove/add during a full re-sort.
        if (isBatchSorting) return;
        TriggerSortBest();
    }

    /// <summary>
    /// Trigger this to calculate and show the best sort suggestion for all unplaced items.
    /// </summary>
    public void TriggerSortBest()
    {
        if (InventoryMono.Instance == null || StoringSpaceMono.Instance == null)
            return;

        var storingSpace = StoringSpaceMono.Instance.StoringSpace;

        // Find all unplaced items (bought but not yet in inventory)
        var allItems = FindObjectsByType<ItemWithFootprintMono>();
        var unplacedItems = allItems.Where(i => i.bought && !i.isAddedToInventory).ToList();

        if (unplacedItems.Count == 0)
        {
            ClearSuggestions();
            return;
        }

        // Optional: You can fetch requiredFootprints from Level Manager here if needed.
        var suggestion = InventorySortService.CalculateBestSort(unplacedItems, storingSpace, null);

        //ShowSuggestions(suggestion);
    }

    public void ClearSuggestions()
    {
        foreach (var ghost in activeGhosts)
        {
            if (ghost != null)
            {
                Destroy(ghost);
            }
        }
        activeGhosts.Clear();
        CurrentSuggestion = null;
    }

    public void ShowSuggestions(SortSuggestion suggestion)
    {
        ClearSuggestions();
        CurrentSuggestion = suggestion;

        var storingHelper = StoringSpaceMono.Instance.gridLayoutGroupHelper;
        if (storingHelper == null)
            return;

        // LỖI 2 FIX: Do NOT put ghosts inside the GridLayoutGroup. 
        // Create a separate overlay sibling that mirrors the grid's transform.
        Transform parentCanvas = storingHelper.transform.parent;
        Transform overlay = parentCanvas.Find("SuggestionOverlay");
        if (overlay == null)
        {
            var overlayGo = new GameObject("SuggestionOverlay");
            overlayGo.transform.SetParent(parentCanvas, false);

            // Ensure it draws ON TOP of the grid
            overlayGo.transform.SetSiblingIndex(storingHelper.transform.GetSiblingIndex() + 1);

            var overlayRt = overlayGo.AddComponent<RectTransform>();

            // Stretch to match parent or just center it. World positions will be used anyway, 
            // so the overlay just needs to be a valid container.
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            overlay = overlayGo.transform;
        }

        foreach (var item in suggestion.Items)
        {
            CreateGhost(item, storingHelper, overlay);
        }
    }

    private void CreateGhost(SortSuggestionItem suggestionItem, GridLayoutGroupHelper storingHelper, Transform overlay)
    {
        var go = new GameObject($"Ghost_{suggestionItem.UnplacedItem.Item.Name}");
        // LỖI 2 FIX: Parent to the overlay, not the grid!
        go.transform.SetParent(overlay, false);

        var rectTransform = go.AddComponent<RectTransform>();
        var image = go.AddComponent<Image>();

        image.sprite = suggestionItem.UnplacedItem.Item.Icon;
        image.color = suggestionItem.MatchedRequirement != null ? matchedGhostColor : normalGhostColor;
        image.raycastTarget = false; // Important: do not block clicks or drag

        // 1. Pivot at center so rotation occurs around the visual center
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localRotation = Quaternion.Euler(0, 0, suggestionItem.SuggestedRotation * -90f);

        // 2. Exact Position & Size matching the grid cells exactly (LỖI 1 FIX)
        var finalFootprint = suggestionItem.FinalFootprint;
        bool[,] req = finalFootprint.Requiring;
        int cols = req.GetLength(0);
        int rows = req.GetLength(1);

        // Calculate the exact size based on cell size and spacing
        Vector2 cellSize = storingHelper.Grid.cellSize;
        Vector2 spacing = storingHelper.Grid.spacing;

        float finalWidth = cols * cellSize.x + Mathf.Max(0, cols - 1) * spacing.x;
        float finalHeight = rows * cellSize.y + Mathf.Max(0, rows - 1) * spacing.y;

        rectTransform.sizeDelta = new Vector2(finalWidth, finalHeight);

        // Get world position of Bottom-Left cell and Top-Right cell for this footprint
        var blCorner = storingHelper.GetCellWorldCorners(
            suggestionItem.SuggestedPivot.x,
            suggestionItem.SuggestedPivot.y).BottomLeft;

        var trCorner = storingHelper.GetCellWorldCorners(
            suggestionItem.SuggestedPivot.x + cols - 1,
            suggestionItem.SuggestedPivot.y + rows - 1).TopRight;

        // The center of this group of cells in world space
        Vector3 footprintCenterWorld = (blCorner + trCorner) * 0.5f;

        // Place the image center exactly at the footprint center
        rectTransform.position = footprintCenterWorld;

        // 3. Pulse Animation (Alpha)
        var pulse = go.AddComponent<GhostPulseAnimation>();
        pulse.image = image;
        pulse.baseColor = image.color;

        activeGhosts.Add(go);
    }

    [ContextMenu("Sort All Items (Re-pack)")]
    public void SortAllItems()
    {
        if (InventoryMono.Instance == null || StoringSpaceMono.Instance == null)
            return;

        var allItems = FindObjectsByType<ItemWithFootprintMono>()
            .Where(i => i.bought && i.Item != null)
            .ToList();

        if (allItems.Count == 0)
            return;

        isBatchSorting = true;
        ClearSuggestions();

        // Snapshot the current layout so we can roll back if the sort fails.
        var snapshot = allItems
            .Where(i => i.isAddedToInventory)
            .Select(i => (mono: i, rotation: i.Rotated, pivot: i.StorePivot))
            .ToList();

        try
        {
            // 1. Remove everything from the inventory and reset to rotation 0,
            //    so the service starts from a clean, canonical state.
            foreach (var mono in allItems)
            {
                if (mono.isAddedToInventory)
                    mono.RemoveFromInventory();

                mono.SetRotateZero();
            }

            // Make sure the storing space is completely empty/rebuilt.
            StoringSpaceMono.Instance.AutoUpdateDataAndRefreshUI();

            // 2. Calculate best packing.
            var storingSpace = StoringSpaceMono.Instance.StoringSpace;
            var suggestion = InventorySortService.CalculateBestSort(allItems, storingSpace, null);

            if (suggestion == null || suggestion.Items == null || suggestion.Items.Count < allItems.Count)
            {
                Debug.LogWarning("[InventorySortUIMono] Sort could not fit every item. Restoring previous layout.");
                RestoreSnapshot(allItems, snapshot);
                return;
            }

            // 3. Place everything again using the suggested rotation + pivot.
            var placed = new List<ItemWithFootprintMono>();
            bool failed = false;

            foreach (var s in suggestion.Items)
            {
                var mono = s.UnplacedItem;
                if (mono.PlaceFromSort(s.SuggestedRotation, s.SuggestedPivot))
                {
                    placed.Add(mono);
                }
                else
                {
                    failed = true;
                    break;
                }
            }

            if (failed)
            {
                Debug.LogWarning("[InventorySortUIMono] Placement failed mid-sort. Restoring previous layout.");

                // Take back whatever got placed, then restore the original.
                foreach (var mono in placed)
                {
                    mono.RemoveFromInventory();
                    mono.SetRotateZero();
                }
                StoringSpaceMono.Instance.AutoUpdateDataAndRefreshUI();
                RestoreSnapshot(allItems, snapshot);
            }
        }
        finally
        {
            isBatchSorting = false;
            StoringSpaceMono.Instance.AutoUpdateDataAndRefreshUI();
            TriggerSortBest(); // clears ghosts if nothing is left unplaced
        }
    }

    private void RestoreSnapshot(
    List<ItemWithFootprintMono> allItems,
    List<(ItemWithFootprintMono mono, int rotation, Vector2Int pivot)> snapshot)
    {
        foreach (var (mono, rotation, pivot) in snapshot)
        {
            mono.SetRotateZero();
            mono.PlaceFromSort(rotation, pivot);
        }

        // Items that were never placed originally go back to the holder.
        foreach (var mono in allItems)
        {
            if (!mono.isAddedToInventory)
                mono.ReturnToTemporaryHolder();
        }
    }

}


