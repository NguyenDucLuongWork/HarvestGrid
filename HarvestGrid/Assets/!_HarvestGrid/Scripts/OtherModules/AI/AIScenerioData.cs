using System;
using System.Collections.Generic;
using UnityEngine;

public enum AIActionType
{
    Wait,
    Buy,
    SellThenBuy,
    ReorganizeThenBuy,
    RollShop,
    BuyFarmSlot
}

[Serializable]
public class AISettings
{
    [Tooltip("How many times we expect an item to fire during the level.")]
    public int expectedCycles = 3;

    [Tooltip("Selling a stored item refunds price * this ratio (design doc: 50%).")]
    [Range(0f, 1f)] public float sellRefundRatio = 0.5f;

    [Tooltip("Crops required by the level are worth this many times their sell price.")]
    public float goalCropWeight = 1.5f;

    [Tooltip("Gold-equivalent value of going from 0% to 100% win rate.")]
    public float winRateWeight = 200f;

    [Tooltip("When plants need resources the player can't supply yet, count this fraction of that demand as 'will be fixed later'. " +
             "Lets the AI buy the first seed before any resource item exists (chicken-and-egg).")]
    [Range(0f, 1f)] public float unmetDemandWeight = 0.35f;

    [Tooltip("Each extra copy of an item you already hold is worth this fraction of the previous copy (stops repeat recommendations).")]
    [Range(0f, 1f)] public float duplicatePenalty = 0.5f;

    [Tooltip("Items currently in the shop get this fraction of their gained value added to their score.")]
    [Range(0f, 1f)] public float shopPriorityBonus = 0.05f;

    [Header("Rolling the shop")]
    [Tooltip("Let the AI recommend re-rolling the shop when the pool probably holds something better than what is offered.")]
    public bool allowRoll = true;

    [Tooltip("What one roll costs the player (in money). Set this to the shop's real roll price.")]
    public int rollCost = 0;

    [Tooltip("Uncertainty discount: a roll must beat the best shop option by roughly this much to be recommended.")]
    public float rollMargin = 3f;

    [Tooltip("How many items one roll shows. 0 = use the number of items in the shop right now.")]
    public int shopSlotsOverride = 0;

    [Tooltip("Small cost for the player's effort of re-packing the inventory, so the AI prefers a direct buy when both work.")]
    public float reorganizePenalty = 5f;

    [Tooltip("Price of one extra farm slot. 0 disables the farm-slot scenario.")]
    public int farmSlotPrice = 0;

    [Tooltip("Gold-equivalent annoyance of moving one already-placed item when reorganizing. " +
             "Keeps the AI preferring a direct buy, and fewer moves over more.")]
    public float reorganizeEffortPerMove = 0.5f;

    [Tooltip("Minimum score a scenario needs to be recommended over waiting.")]
    public float minimumScore = 1f;
}

[Serializable]
public struct ResourceDemand
{
    public Resource resource;
    public int amount;
}

[Serializable]
public struct ResourceSupply
{
    public Resource resource;
    public int amount;
}

[Serializable]
public struct CropProgress
{
    public string cropID;
    public int required;   // from level
    public int owned;      // inventory (stars >= required stars)
    public int pending;    // expected from plants already on the farm
}

/// <summary>Read-only picture of the game at the moment the AI analysed it.</summary>
[Serializable]
public class AIWorldSnapshot
{
    public int money;
    public int totalSlots;
    public int emptySlots;
    public int freeCells;              // free storing-space cells (fragmentation = many free cells, nothing fits)
    public int growingPlants;
    public int harvestablePlants;
    public int storedAddPlantItems;

    public float expectedFarmIncome;   // sell value of everything on the farm
    public float averagePlantIncome;   // per plant, one harvest
    public float winRate;              // 0..1 heuristic

    [Tooltip("How much of the plants' resource demand (farm + held seeds) is covered by held resource items. 1 = fully supplied.")]
    public float resourceCoverage = 1f;
    [Tooltip("Expected value of everything the player holds right now (farm + inventory + bought-but-unplaced).")]
    public float portfolioValue;
    public int heldItemCount;

    public List<ResourceDemand> resourceDemand = new();   // total demand: farm plants + held seeds
    public List<ResourceSupply> resourceSupply = new();   // from held resource items
    public List<CropProgress> cropProgress = new();

    public int GetDemand(Resource resource)
    {
        foreach (var d in resourceDemand)
            if (EqualityComparer<Resource>.Default.Equals(d.resource, resource))
                return d.amount;
        return 0;
    }

    public bool IsNeededCrop(string cropID)
    {
        foreach (var p in cropProgress)
            if (p.cropID == cropID && p.owned + p.pending < p.required)
                return true;
        return false;
    }
}

/// <summary>One step of a reorganize plan: where an item should end up.</summary>
[Serializable]
public class AIPlacementMove
{
    public string itemId;
    public bool isNewItem;          // true = the item being bought
    public Vector2Int fromPivot;    // ignored when isNewItem
    public Vector2Int toPivot;
    [Tooltip("Footprint.Rotate() calls to apply to the item's current footprint.")]
    public int rotations;
}

/// <summary>One "what if" the AI evaluated.</summary>
[Serializable]
public class AIScenario
{
    [Tooltip("Only for scenarios that reorganize: every item that has to move, plus the new item.")]
    public List<AIPlacementMove> repackPlan = new();
    public int moves;

    public AIActionType action;
    public string description;

    public string buyItemId;
    public string sellItemId;

    [Tooltip("The bought item is in the shop right now (actionable). False = only in the level's item pool.")]
    public bool inShop;

    [Tooltip("Weight of the bought item in LevelSO.itemAndChancePool (1 if the item is in the shop but not in the pool).")]
    public float poolChance = 1f;

    public int moneyBefore;
    public int moneyAfter;

    public bool fits;
    [Tooltip("True if the item only fits after the inventory is re-packed (InventorySortUIMono.SortAllItems).")]
    public bool requiresReorganize;
    [Tooltip("Where the new item lands AFTER re-packing (or directly, if no re-pack is needed).")]
    public Vector2Int placementPivot;
    public int placementRotations;

    public float valueGained;
    public float valueLost;
    public float netCost;
    public float winRateBefore;
    public float winRateAfter;
    public float resourceCoverageBefore;
    public float resourceCoverageAfter;

    public float score;
    public List<string> notes = new();

    // ------------------------------------------------------------------
    // RELATED ENTITIES - deep clones owned by this scenario, so the UI can
    // keep, display or mutate them without touching live game data.
    // [NonSerialized]: Item/Plant graphs are deep and would bloat the Inspector
    // (and hit Unity's serialization depth limit). Remove the attribute on a
    // field if you want it visible while debugging.
    // ------------------------------------------------------------------

    /// <summary>Clone of the item the AI wants to buy (set for every scenario, even rejected ones).</summary>
    [NonSerialized] public Item buyItem;

    /// <summary>Clone of the stored object that would be sold (SellThenBuy only).</summary>
    [NonSerialized] public StoredObject sellStoredObject;

    /// <summary>
    /// The bought item as it would sit in the storing space: Rotated = placementRotations,
    /// Pivot = placementPivot. Item is an independent clone (unrotated; see placementFootprint).
    /// Null when the item doesn't fit.
    /// </summary>
    [NonSerialized] public StoredObject placedBuyObject;

    /// <summary>Clone of the bought item's footprint in its FINAL orientation (for ghosts / previews).</summary>
    [NonSerialized] public Footprint placementFootprint;

    /// <summary>
    /// For scenarios with requiresReorganize: where every OTHER item goes after the re-pack
    /// (the bought item is described by placedBuyObject).
    /// </summary>
    [NonSerialized] public List<AIRepackMove> repackMoves = new();

    /// <summary>How many existing items actually change position/rotation in the re-pack.</summary>
    public int itemsMoved;

    [Tooltip("RollShop only: chance that a roll shows at least one worthwhile item.")]
    public float rollChance;

    /// <summary>RollShop only: clones of the most promising pool items (best first), for the UI.</summary>
    [NonSerialized] public List<Item> rollTargets = new();

    public Item SellItem => sellStoredObject?.Item;
}

/// <summary>One item's before/after in a re-pack. Rotations are counted from the item's unrotated state,
/// the same convention as SortSuggestionItem.SuggestedRotation / PlaceFromSort.</summary>
[Serializable]
public class AIRepackMove
{
    public Item item;                 // clone
    public bool wasPlaced;            // false = bought but not yet in the storing space
    public Vector2Int oldPivot;
    public int oldRotation;
    public Vector2Int newPivot;
    public int newRotation;
    public Footprint newFootprint;    // clone, final orientation

    public bool Moved => !wasPlaced || oldPivot != newPivot || oldRotation != newRotation;
}

[Serializable]
public class AIRecommendation
{
    public AIActionType action = AIActionType.Wait;
    public string summary = "Nothing to recommend yet.";
    public AIScenario scenario;
}