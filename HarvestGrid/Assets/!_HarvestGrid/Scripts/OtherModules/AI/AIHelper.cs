using HarvestGrid.Services;
using LgTyLib.Core;
using System.Collections.Generic;
using UnityEngine;

public class AIHelper : BaseSingleton<AIHelper>
{
    [Header("AI Data")]
    [SerializeField]
    private LevelSO levelSO;

    [SerializeField]
    private List<Item> itemsInShop;
    [SerializeField]
    private Inventory inventory;
    [SerializeField]
    private StoringSpace storingSpace;
    [SerializeField]
    private Farm farm;

    [Header("Config")]
    [SerializeField] private AISettings settings = new();
    [Tooltip("Re-run Analyse() (next frame) when money/items change or something is purchased.")]
    [SerializeField] private bool autoAnalyse = true;

    [Header("Output (read-only, for devs)")]
    [SerializeField] private AIWorldSnapshot snapshot = new();
    [SerializeField] private List<AIScenario> scenarios = new();
    [SerializeField] private AIRecommendation recommendation = new();

    // ------------------------------------------------------------------
    // Internal types
    // ------------------------------------------------------------------

    // One entry per thing that takes part in a re-pack simulation.
    private class RepackSource
    {
        public Item item;
        public StoredObject stored;   // null = bought-but-unplaced item, or the candidate itself
        public int rotated;           // rotations applied to item.Footprint relative to its unrotated state
    }

    private class RepackPlan
    {
        public RepackResult Result;
        public List<RepackSource> Sources;   // index-aligned with the simulated footprints
        public int CandidateIndex;           // the item being bought
        public bool Success => Result != null && Result.Success;
    }

    private struct Candidate
    {
        public Item item;
        public float chance;
        public bool inShop;
        public bool inPool;   // can appear after a roll
    }

    private class SeedEntry
    {
        public string cropID;
        public float harvest;     // sell value of one harvest
        public int yield;         // crops per harvest
        public float duplicate;   // 1 for the first copy of an item, penalty^n for later copies
        public Dictionary<Resource, int> need;
    }

    /// <summary>Result of valuing "everything the player holds" (see EvaluatePortfolio).</summary>
    private class PortfolioDetail
    {
        public float value;
        public float coverage = 1f;
        public float slotFactor = 1f;
        public float winRate;
        public Dictionary<string, int> pendingCrops = new();
        public Dictionary<Resource, float> demand = new();
        public Dictionary<Resource, float> supply = new();
    }

    // ------------------------------------------------------------------
    // State
    // ------------------------------------------------------------------

    // Bought-but-not-yet-placed items: they are owned, and need room in a re-pack.
    private List<RepackSource> unplacedSources = new();

    // Everything the player owns that has an effect: stored items + unplaced bought items.
    private List<Item> holdings = new();
    private PortfolioDetail baseline;

    // Farm facts the portfolio model reuses.
    private Dictionary<Resource, int> farmDemand = new();
    private float farmGrowingIncome;

    private bool dirty;
    private Inventory subscribedInventory;

    public AIWorldSnapshot Snapshot => snapshot;
    public IReadOnlyList<AIScenario> Scenarios => scenarios;
    public AIRecommendation Recommendation => recommendation;

    // ============================================================
    // LIFECYCLE
    // ============================================================

    private void Start()
    {
        GetData();
        Subscribe();
    }

    protected override void OnDestroy()
    {
        Unsubscribe();
        base.OnDestroy();
    }

    private void LateUpdate()
    {
        // Several events can fire in one frame (purchase + money + items); analyse once.
        if (!dirty) return;
        dirty = false;
        Analyse();
    }

    public void GetData()
    {
        levelSO = GameplayScene.Instance.LevelSO;
        itemsInShop = Shop.HasInstance ? Shop.Instance.currentItems : null;
        inventory = InventoryMono.Instance.Inventory;
        storingSpace = StoringSpaceMono.Instance.StoringSpace;
        farm = FarmMono.Instance.Farm;
    }

    /// <summary>Ask for a fresh analysis next frame (call from anything that changes the farm).</summary>
    public void MarkDirty() => dirty = true;

    private void Subscribe()
    {
        if (!autoAnalyse) return;

        ItemWithFootprintMono.OnItemPurchased += HandlePurchased;

        if (inventory != null)
        {
            subscribedInventory = inventory;
            subscribedInventory.OnMoneyChanged += HandleMoneyChanged;
            subscribedInventory.OnItemsChanged += HandleItemsChanged;
        }
    }

    private void Unsubscribe()
    {
        ItemWithFootprintMono.OnItemPurchased -= HandlePurchased;

        if (subscribedInventory != null)
        {
            subscribedInventory.OnMoneyChanged -= HandleMoneyChanged;
            subscribedInventory.OnItemsChanged -= HandleItemsChanged;
            subscribedInventory = null;
        }
    }

    private void HandlePurchased(ItemWithFootprintMono _) => dirty = true;
    private void HandleMoneyChanged(int _) => dirty = true;
    private void HandleItemsChanged(IReadOnlyList<Item> _) => dirty = true;

    /// <summary>
    /// Executes the "reorganize" half of a ReorganizeThenBuy / SellThenBuy recommendation
    /// using the existing re-pack (same algorithm the AI simulated).
    /// </summary>
    public void ReorganizeNow()
    {
        if (InventorySortUIMono.HasInstance)
            InventorySortUIMono.Instance.SortAllItems();
    }

    // ============================================================
    // ENTRY POINT
    // ============================================================

    [ContextMenu("Analyse")]
    public AIRecommendation Analyse()
    {
        GetData();
        scenarios.Clear();

        unplacedSources = GatherUnplaced();
        holdings = BuildHoldings();
        snapshot = BuildSnapshot();

        // Value of what the player already owns: every scenario is measured against this.
        baseline = EvaluatePortfolio(holdings);
        ApplyBaselineToSnapshot();

        bool[,] free = BuildFreeGrid();
        snapshot.freeCells = CountFree(free);

        List<Candidate> candidates = BuildCandidates();

        if (candidates.Count == 0)
            Debug.LogWarning("[AIHelper] No candidate items: the shop is empty and LevelSO.itemAndChancePool has no usable items.");

        if (free.GetLength(0) == 0)
            Debug.LogWarning("[AIHelper] Storing space has no cells, so nothing will fit.");

        foreach (Candidate c in candidates)
        {
            Item candidate = c.item;
            bool affordable = candidate.Price <= snapshot.money;
            bool fitsDirect = TryFindPlacement(free, candidate.Footprint, out _, out _);

            // Fragmentation check: no spot now, but would a full re-pack make room?
            RepackPlan repack = (affordable && !fitsDirect)
                ? SimulateRepackWith(candidate, null)
                : null;

            bool buyableWithoutSelling = affordable && (fitsDirect || (repack != null && repack.Success));

            AIScenario buy = EvaluateBuy(candidate, free, repack, c.inShop);
            if (buy != null)
            {
                buy.poolChance = c.chance;
                scenarios.Add(buy);
            }

            if (buyableWithoutSelling) continue; // selling would only lose value

            foreach (StoredObject stored in inventory.Items)
            {
                AIScenario swap = EvaluateSellThenBuy(candidate, stored, free, c.inShop);
                if (swap != null)
                {
                    swap.poolChance = c.chance;
                    scenarios.Add(swap);
                }
            }
        }

        AIScenario slot = EvaluateFarmSlot();
        if (slot != null) scenarios.Add(slot);

        // Must run after the buy scenarios: it reuses their scores.
        AIScenario roll = EvaluateRoll(candidates);
        if (roll != null) scenarios.Add(roll);

        scenarios.Sort((a, b) => b.score.CompareTo(a.score));
        recommendation = BuildRecommendation();

        Debug.Log($"[AIHelper] {scenarios.Count} scenarios, money={snapshot.money}, held={snapshot.heldItemCount}, " +
                  $"coverage={snapshot.resourceCoverage:P0}, freeCells={snapshot.freeCells}. {recommendation.summary}");

        return recommendation;
    }

    // ============================================================
    // CANDIDATES
    // ============================================================

    /// <summary>
    /// Everything the AI may recommend: the shop's current items (actionable, small priority bonus)
    /// plus the rest of the level's item pool (planning / "worth saving for").
    /// </summary>
    private List<Candidate> BuildCandidates()
    {
        var result = new List<Candidate>();

        // Pool first, so shop items can inherit their pool chance.
        var pool = new Dictionary<string, (Item item, float chance)>();
        if (levelSO != null && levelSO.itemAndChancePool != null)
        {
            foreach (var kvp in levelSO.itemAndChancePool)
            {
                Item item = ResolveItem(kvp.Key);

                if (item == null)
                {
                    Debug.LogWarning($"[AIHelper] Could not resolve an Item from pool entry '{kvp.Key}'.");
                    continue;
                }

                pool[item.Id ?? string.Empty] = (item, kvp.Value);
            }
        }

        var seen = new HashSet<string>();

        if (itemsInShop != null)
        {
            foreach (Item item in itemsInShop)
            {
                if (item == null) continue;

                string id = item.Id ?? string.Empty;
                bool inPool = pool.TryGetValue(id, out var entry);
                float chance = inPool ? entry.chance : 1f;

                result.Add(new Candidate { item = item, chance = chance, inShop = true, inPool = inPool });
                seen.Add(id);
            }
        }

        foreach (var kvp in pool)
        {
            if (seen.Contains(kvp.Key)) continue;
            result.Add(new Candidate { item = kvp.Value.item, chance = kvp.Value.chance, inShop = false, inPool = true });
        }

        return result;
    }

    /// <summary>
    /// The ONLY place that knows how an ItemPrototype produces an Item.
    /// Assumes ItemPrototype exposes the item data as `item`. Change this line if yours differs.
    /// </summary>
    private static Item ResolveItem(ItemPrototype prototype)
    {
        if (prototype == null) return null;
        return prototype.item;
    }

    private List<Item> BuildHoldings()
    {
        var list = new List<Item>();

        foreach (StoredObject so in inventory.Items)
            if (so?.Item != null) list.Add(so.Item);

        foreach (RepackSource src in unplacedSources)
            if (src.item != null) list.Add(src.item);

        return list;
    }

    // ============================================================
    // SNAPSHOT
    // ============================================================

    private AIWorldSnapshot BuildSnapshot()
    {
        var snap = new AIWorldSnapshot { money = inventory.Money };
        farmDemand = new Dictionary<Resource, int>();
        farmGrowingIncome = 0f;
        var pendingByCrop = new Dictionary<string, int>();
        int plantCount = 0;

        foreach (FarmSlot slot in farm.FarmSlots)
        {
            if (slot == null) continue;
            snap.totalSlots++;

            Plant plant = slot.Plant;
            if (plant == null)
            {
                snap.emptySlots++;
                continue;
            }

            float income = EstimateHarvestValue(plant);
            snap.expectedFarmIncome += income;
            plantCount++;

            if (plant.Crop != null)
            {
                pendingByCrop.TryGetValue(plant.Crop.CropID, out int p);
                pendingByCrop[plant.Crop.CropID] = p + plant.AverageCropCount;
            }

            if (plant.IsFullyGrown)
            {
                snap.harvestablePlants++;
            }
            else
            {
                snap.growingPlants++;
                farmGrowingIncome += income;
                AddRemainingDemand(plant, farmDemand);
            }
        }

        // Seeds the player already owns (placed or not) also count towards income and slot pressure.
        float heldSeedIncome = 0f;
        foreach (Item item in holdings)
        {
            if (item?.Uses == null) continue;
            foreach (ItemUse use in item.Uses)
            {
                if (use is AddPlantUse add && add.PlantToAdd != null)
                {
                    snap.storedAddPlantItems++;
                    heldSeedIncome += EstimateHarvestValue(add.PlantToAdd);
                }
            }
        }

        snap.heldItemCount = holdings.Count;

        int incomeSamples = plantCount + snap.storedAddPlantItems;
        snap.averagePlantIncome = incomeSamples > 0
            ? (snap.expectedFarmIncome + heldSeedIncome) / incomeSamples
            : 0f;

        // Level goal progress. `pending` here is the FARM only; held seeds are added by the portfolio.
        if (levelSO != null && levelSO.requiringCrops != null)
        {
            foreach (var kvp in levelSO.requiringCrops)
            {
                if (kvp.Key == null) continue;

                int owned = 0;
                foreach (var held in inventory.Crops)
                {
                    if (held.Key != null &&
                        held.Key.CropID == kvp.Key.CropID &&
                        held.Key.Stars >= kvp.Key.Stars)
                    {
                        owned += held.Value;
                    }
                }

                pendingByCrop.TryGetValue(kvp.Key.CropID, out int pending);

                snap.cropProgress.Add(new CropProgress
                {
                    cropID = kvp.Key.CropID,
                    required = kvp.Value,
                    owned = owned,
                    pending = pending
                });
            }
        }

        return snap;
    }

    private void ApplyBaselineToSnapshot()
    {
        snapshot.winRate = baseline.winRate;
        snapshot.resourceCoverage = baseline.coverage;
        snapshot.portfolioValue = baseline.value;

        snapshot.resourceDemand.Clear();
        foreach (var kvp in baseline.demand)
            snapshot.resourceDemand.Add(new ResourceDemand { resource = kvp.Key, amount = Mathf.RoundToInt(kvp.Value) });

        snapshot.resourceSupply.Clear();
        foreach (var kvp in baseline.supply)
            snapshot.resourceSupply.Add(new ResourceSupply { resource = kvp.Key, amount = Mathf.RoundToInt(kvp.Value) });
    }

    private static void AddRemainingDemand(Plant plant, Dictionary<Resource, int> demand)
    {
        // NextStageRequirement = what is still missing for the next stage.
        // Stages[0] is that same stage, so later stages start at 1.
        if (plant.NextStageRequirement != null)
        {
            foreach (var kvp in plant.NextStageRequirement)
            {
                demand.TryGetValue(kvp.Key, out int cur);
                demand[kvp.Key] = cur + kvp.Value;
            }
        }

        AddFutureDemand(plant, demand);
    }

    /// <summary>
    /// BeginGrow() immediately grows into Stages[0] without requirements,
    /// so only Stages[1..] cost resources.
    /// </summary>
    private static void AddFutureDemand(Plant plant, Dictionary<Resource, int> demand)
    {
        if (plant?.Stages == null) return;

        for (int i = 1; i < plant.Stages.Count; i++)
        {
            foreach (var kvp in plant.Stages[i].RequiredResources)
            {
                demand.TryGetValue(kvp.Key, out int cur);
                demand[kvp.Key] = cur + kvp.Value;
            }
        }
    }

    // ============================================================
    // VALUE ESTIMATION
    // ============================================================

    /// <summary>
    /// Mirrors Plant.Harvest(efficiency = 1) without touching GameObjects
    /// (Plant.Harvest deactivates the scene object, so it can't be used to "simulate").
    /// </summary>
    private static float EstimateHarvestValue(Plant plant, float efficiency = 1f)
    {
        if (plant == null || plant.Crop == null) return 0f;

        int totalYield = Mathf.Max(1, Mathf.RoundToInt(plant.AverageCropCount * efficiency));
        int baseStar = Mathf.Clamp(Mathf.FloorToInt(efficiency * 2f), 1, 5);
        int secondStar = baseStar < 5 ? baseStar + 1 : baseStar - 1;
        if (secondStar < 1) secondStar = 1;

        int primaryAmount = Mathf.CeilToInt(totalYield * 0.6f);
        int secondaryAmount = totalYield - primaryAmount;

        Crop c = plant.Crop.Clone();
        c.SetStars(baseStar);
        float value = c.GetSellPrice() * primaryAmount;

        if (secondaryAmount > 0 && baseStar != secondStar)
        {
            c.SetStars(secondStar);
            value += c.GetSellPrice() * secondaryAmount;
        }

        return value;
    }

    /// <summary>
    /// Values EVERYTHING the player would hold, so an item is worth its marginal effect on the whole set:
    ///
    ///  - Seeds earn money, but only if the plants get resources, so income is scaled by resource COVERAGE
    ///    (supply from resource items vs demand of farm plants + seeds). A seed with no supplier is worth little,
    ///    a resource item next to a starving plant is worth a lot.
    ///  - Seeds compete for farm slots (slotFactor) and a repeated copy of the same item is discounted.
    ///  - Harvest items speed up collecting, capped by how many plants exist.
    ///  - Crops the level asks for are worth goalCropWeight x, but only for the amount still missing.
    ///
    /// Marginal value = Evaluate(holdings + item) - Evaluate(holdings).
    /// </summary>
    private PortfolioDetail EvaluatePortfolio(IReadOnlyList<Item> items)
    {
        int cycles = Mathf.Max(1, settings.expectedCycles);
        var d = new PortfolioDetail();

        // ---- 1. Collect what the items do ----
        var seeds = new List<SeedEntry>();
        var copies = new Dictionary<string, int>();
        int harvestUses = 0;
        float harvestEffectSum = 0f;

        foreach (Item item in items)
        {
            if (item?.Uses == null) continue;

            string id = item.Id ?? string.Empty;
            copies.TryGetValue(id, out int copy);
            copies[id] = copy + 1;

            foreach (ItemUse use in item.Uses)
            {
                switch (use)
                {
                    case AddPlantUse add when add.PlantToAdd != null && add.PlantToAdd.Crop != null:
                        {
                            var need = new Dictionary<Resource, int>();
                            AddFutureDemand(add.PlantToAdd, need);

                            seeds.Add(new SeedEntry
                            {
                                cropID = add.PlantToAdd.Crop.CropID,
                                harvest = EstimateHarvestValue(add.PlantToAdd),
                                yield = add.PlantToAdd.AverageCropCount,
                                duplicate = Mathf.Pow(settings.duplicatePenalty, copy),
                                need = need
                            });
                            break;
                        }

                    case ProviceResourceUse res:
                        {
                            d.supply.TryGetValue(res.Resource, out float cur);
                            d.supply[res.Resource] = cur + res.Amount * cycles;
                            break;
                        }

                    case HarvestUse harvest:
                        {
                            harvestUses++;
                            harvestEffectSum += harvest.Effective;
                            break;
                        }
                }
            }
        }

        // ---- 2. Slots: how many of these plantings actually find a free slot ----
        int plantings = seeds.Count * cycles;
        int farmPlants = snapshot.growingPlants + snapshot.harvestablePlants;
        int capacity = Mathf.Max(0, snapshot.totalSlots * cycles - farmPlants);
        int usable = Mathf.Min(plantings, capacity);
        d.slotFactor = plantings > 0 ? (float)usable / plantings : 1f;

        // ---- 3. Resource demand (farm plants + seeds that will be planted) vs supply ----
        foreach (var kvp in farmDemand)
            d.demand[kvp.Key] = kvp.Value;

        foreach (SeedEntry seed in seeds)
        {
            foreach (var kvp in seed.need)
            {
                d.demand.TryGetValue(kvp.Key, out float cur);
                d.demand[kvp.Key] = cur + kvp.Value * cycles * d.slotFactor;
            }
        }

        float totalDemand = 0f;
        float met = 0f;
        foreach (var kvp in d.demand)
        {
            totalDemand += kvp.Value;
            d.supply.TryGetValue(kvp.Key, out float supplied);
            met += Mathf.Min(kvp.Value, supplied);
        }

        // Demand nobody supplies yet is partly counted as "will be solved later" so the first seed is buyable.
        d.coverage = totalDemand > 0.0001f
            ? (met + settings.unmetDemandWeight * (totalDemand - met)) / totalDemand
            : 1f;

        // ---- 4. Goal crops: bonus only for the amount the level still needs ----
        var rawYield = new Dictionary<string, float>();
        foreach (SeedEntry seed in seeds)
        {
            rawYield.TryGetValue(seed.cropID, out float cur);
            rawYield[seed.cropID] = cur + seed.yield * cycles;
        }

        var missing = new Dictionary<string, float>();
        foreach (CropProgress p in snapshot.cropProgress)
        {
            missing.TryGetValue(p.cropID, out float cur);
            missing[p.cropID] = cur + Mathf.Max(0, p.required - p.owned - p.pending);
        }

        float seedIncome = 0f;
        foreach (SeedEntry seed in seeds)
        {
            float raw = rawYield[seed.cropID];
            missing.TryGetValue(seed.cropID, out float cap);
            float neededShare = raw > 0f ? Mathf.Min(1f, cap / raw) : 0f;
            float multiplier = 1f + (settings.goalCropWeight - 1f) * neededShare;

            seedIncome += seed.harvest * cycles * multiplier * seed.duplicate;
        }

        // ---- 5. Totals ----
        float farmValue = farmGrowingIncome * d.coverage;
        float seedValue = seedIncome * d.slotFactor * d.coverage;

        float harvestValue = 0f;
        if (harvestUses > 0)
        {
            float backlog = snapshot.harvestablePlants + 0.5f * (snapshot.growingPlants + usable);
            float harvests = Mathf.Min(cycles * harvestUses, backlog);
            harvestValue = snapshot.averagePlantIncome * harvests * (harvestEffectSum / harvestUses) * d.coverage;
        }

        d.value = farmValue + seedValue + harvestValue;

        // ---- 6. Crops this portfolio is expected to deliver (for the win-rate estimate) ----
        foreach (var kvp in rawYield)
            d.pendingCrops[kvp.Key] = Mathf.RoundToInt(kvp.Value * d.slotFactor * d.coverage);

        d.winRate = ComputeWinRate(snapshot.cropProgress, d.pendingCrops);
        return d;
    }

    private static float ComputeWinRate(List<CropProgress> progress, Dictionary<string, int> extraPending)
    {
        if (progress == null || progress.Count == 0) return 1f;

        float sum = 0f;
        foreach (CropProgress p in progress)
        {
            int extra = 0;
            extraPending?.TryGetValue(p.cropID, out extra);

            float have = p.owned + p.pending + extra;
            sum += Mathf.Clamp01(have / Mathf.Max(1, p.required));
        }

        return sum / progress.Count;
    }

    // ============================================================
    // SCENARIOS
    // ============================================================

    private AIScenario EvaluateBuy(Item candidate, bool[,] free, RepackPlan repack, bool inShop)
    {
        var s = new AIScenario
        {
            action = AIActionType.Buy,
            buyItemId = candidate.Id,
            inShop = inShop,
            description = $"Buy {candidate.Name} for {candidate.Price}",
            moneyBefore = snapshot.money,
            moneyAfter = snapshot.money - candidate.Price,
            winRateBefore = snapshot.winRate,
            resourceCoverageBefore = snapshot.resourceCoverage
        };

        if (candidate.Price > snapshot.money)
        {
            // Still listed so devs can see why it was rejected;
            // sell-then-buy scenarios may make it affordable.
            s.fits = false;
            s.notes.Add($"Rejected: costs {candidate.Price}, only {snapshot.money} available.");
            s.score = float.NegativeInfinity;
            AttachEntities(s, candidate, null, null);
            return s;
        }

        s.fits = TryFindPlacement(free, candidate.Footprint, out s.placementPivot, out s.placementRotations);

        if (!s.fits)
        {
            if (repack != null && repack.Success)
            {
                // Items were placed badly: there is room overall, just not in one piece.
                RepackPlacement placement = repack.Result.Find(repack.CandidateIndex);

                s.action = AIActionType.ReorganizeThenBuy;
                s.description = $"Re-pack the inventory, then buy {candidate.Name} for {candidate.Price}";
                s.fits = true;
                s.requiresReorganize = true;

                if (placement != null)
                {
                    s.placementPivot = placement.Pivot;
                    s.placementRotations = placement.Rotation;
                }

                s.notes.Add($"Storing space is fragmented: {snapshot.freeCells} free cells but no spot fits {candidate.Name}. " +
                            "Re-packing makes room without selling anything.");
            }
            else
            {
                s.notes.Add("No room, even after re-packing. See SellThenBuy scenarios.");
                s.score = float.NegativeInfinity;
                AttachEntities(s, candidate, null, null);
                return s;
            }
        }

        FinishScore(s, candidate, null);
        AttachEntities(s, candidate, null, repack);
        return s;
    }

    private AIScenario EvaluateSellThenBuy(Item candidate, StoredObject stored, bool[,] free, bool inShop)
    {
        if (stored?.Item == null) return null;

        int refund = Mathf.RoundToInt(stored.Item.Price * settings.sellRefundRatio);
        int moneyAfter = snapshot.money + refund - candidate.Price;
        if (moneyAfter < 0) return null;

        // Free the cells the sold item occupies, on a copy of the grid.
        bool[,] grid = (bool[,])free.Clone();
        foreach (Vector2Int p in stored.Item.Footprint.ToSpace(stored.Pivot))
        {
            if (p.x >= 0 && p.y >= 0 && p.x < grid.GetLength(0) && p.y < grid.GetLength(1))
                grid[p.x, p.y] = true;
        }

        var s = new AIScenario
        {
            action = AIActionType.SellThenBuy,
            buyItemId = candidate.Id,
            sellItemId = stored.Item.Id,
            inShop = inShop,
            description = $"Sell {stored.Item.Name} (+{refund}) then buy {candidate.Name} (-{candidate.Price})",
            moneyBefore = snapshot.money,
            moneyAfter = moneyAfter,
            winRateBefore = snapshot.winRate,
            resourceCoverageBefore = snapshot.resourceCoverage
        };

        s.fits = TryFindPlacement(grid, candidate.Footprint, out s.placementPivot, out s.placementRotations);

        RepackPlan repack = null;
        if (!s.fits)
        {
            // Maybe it fits once the remaining items are re-packed.
            repack = SimulateRepackWith(candidate, stored);
            if (!repack.Success) return null;

            RepackPlacement placement = repack.Result.Find(repack.CandidateIndex);
            s.fits = true;
            s.requiresReorganize = true;
            s.description += " after re-packing";

            if (placement != null)
            {
                s.placementPivot = placement.Pivot;
                s.placementRotations = placement.Rotation;
            }

            s.notes.Add("Fits only after re-packing the remaining items.");
        }

        s.netCost = candidate.Price - refund;
        FinishScore(s, candidate, stored);
        AttachEntities(s, candidate, stored, repack);
        return s;
    }

    /// <summary>
    /// Scores a scenario by how the player's whole portfolio changes:
    ///   lost   = baseline - (baseline without the sold item)
    ///   gained = (without sold + candidate) - (without sold)
    /// </summary>
    private void FinishScore(AIScenario s, Item candidate, StoredObject sold)
    {
        var without = holdings;
        PortfolioDetail withoutDetail = baseline;

        if (sold != null)
        {
            without = new List<Item>(holdings);
            without.Remove(sold.Item);
            withoutDetail = EvaluatePortfolio(without);
        }

        var withCandidate = new List<Item>(without) { candidate };
        PortfolioDetail after = EvaluatePortfolio(withCandidate);

        s.valueLost = baseline.value - withoutDetail.value;
        s.valueGained = after.value - withoutDetail.value;

        if (sold == null)
            s.netCost = candidate.Price;

        s.winRateAfter = after.winRate;
        s.resourceCoverageAfter = after.coverage;

        s.score = s.valueGained - s.valueLost - s.netCost
                  + (s.winRateAfter - s.winRateBefore) * settings.winRateWeight;

        if (s.requiresReorganize)
            s.score -= settings.reorganizePenalty;

        // Items in the shop right now are directly actionable: small bonus.
        if (s.inShop)
            s.score += settings.shopPriorityBonus * Mathf.Max(0f, s.valueGained);

        if (Mathf.Abs(s.resourceCoverageAfter - s.resourceCoverageBefore) >= 0.01f)
            s.notes.Add($"Resource coverage {s.resourceCoverageBefore:P0} -> {s.resourceCoverageAfter:P0}");

        if (after.slotFactor < 0.999f)
            s.notes.Add($"Farm slots limit planting to {after.slotFactor:P0} of the held seeds.");

        if (s.winRateAfter > s.winRateBefore)
            s.notes.Add($"Win rate {s.winRateBefore:P0} -> {s.winRateAfter:P0}");

        if (!s.inShop)
            s.notes.Add("Not in the shop right now (from the level's item pool).");
    }

    private AIScenario EvaluateFarmSlot()
    {
        if (settings.farmSlotPrice <= 0 || snapshot.money < settings.farmSlotPrice)
            return null;

        var s = new AIScenario
        {
            action = AIActionType.BuyFarmSlot,
            inShop = true,
            description = $"Buy an extra farm slot for {settings.farmSlotPrice}",
            moneyBefore = snapshot.money,
            moneyAfter = snapshot.money - settings.farmSlotPrice,
            winRateBefore = snapshot.winRate,
            winRateAfter = snapshot.winRate,
            netCost = settings.farmSlotPrice,
            fits = true
        };

        // A slot only helps if plants are waiting for a place to grow.
        bool contention = snapshot.storedAddPlantItems > snapshot.emptySlots;
        if (contention)
        {
            s.valueGained = snapshot.averagePlantIncome * Mathf.Max(1, settings.expectedCycles) * snapshot.resourceCoverage;
            s.notes.Add($"{snapshot.storedAddPlantItems} plant item(s) held vs {snapshot.emptySlots} empty slot(s).");
        }
        else
        {
            s.notes.Add("No slot contention; extra slot would sit empty.");
        }

        s.score = s.valueGained - s.netCost;
        return s;
    }

    /// <summary>
    /// "Roll the shop". The shop may offer nothing good right now (for example the player holds a seed
    /// and a scythe, but the useful item is a resource provider that is not in the shop). This estimates
    /// the value of re-rolling from the level's item pool:
    ///
    ///   P(at least one good item appears) x average score of a good item - roll cost - uncertainty margin
    ///
    /// "Good" = the item's best buy scenario scores >= minimumScore and is still affordable after paying
    /// for the roll. Scores are taken WITHOUT the in-shop bonus, since a rolled item isn't in the shop yet.
    /// </summary>
    private AIScenario EvaluateRoll(List<Candidate> candidates)
    {
        if (!settings.allowRoll || snapshot.money < settings.rollCost) return null;

        int budget = snapshot.money - settings.rollCost;
        float totalWeight = 0f;
        float goodWeight = 0f;
        float goodScoreWeight = 0f;
        var targets = new List<(Item item, float weight, float score)>();

        foreach (Candidate c in candidates)
        {
            if (!c.inPool) continue;

            float weight = Mathf.Max(0f, c.chance);
            totalWeight += weight;

            if (weight <= 0f || c.item.Price > budget) continue;

            // Best way to buy this item (direct or after a re-pack).
            AIScenario best = null;
            foreach (AIScenario s in scenarios)
            {
                if (s.buyItemId != c.item.Id) continue;
                if (s.action != AIActionType.Buy && s.action != AIActionType.ReorganizeThenBuy) continue;
                if (float.IsNegativeInfinity(s.score)) continue;

                if (best == null || s.score > best.score) best = s;
            }

            if (best == null) continue;

            float score = best.score - (best.inShop ? settings.shopPriorityBonus * Mathf.Max(0f, best.valueGained) : 0f);
            if (score < settings.minimumScore) continue;

            goodWeight += weight;
            goodScoreWeight += weight * score;
            targets.Add((c.item, weight, score));
        }

        if (totalWeight <= 0f || goodWeight <= 0f) return null;

        int slots = settings.shopSlotsOverride > 0
            ? settings.shopSlotsOverride
            : Mathf.Max(1, itemsInShop?.Count ?? 1);

        float pGood = goodWeight / totalWeight;
        float rollChance = 1f - Mathf.Pow(1f - pGood, slots);
        float averageGoodScore = goodScoreWeight / goodWeight;

        targets.Sort((a, b) => (b.weight * b.score).CompareTo(a.weight * a.score));

        var s2 = new AIScenario
        {
            action = AIActionType.RollShop,
            inShop = true, // actionable now
            moneyBefore = snapshot.money,
            moneyAfter = snapshot.money - settings.rollCost,
            winRateBefore = snapshot.winRate,
            winRateAfter = snapshot.winRate,
            resourceCoverageBefore = snapshot.resourceCoverage,
            resourceCoverageAfter = snapshot.resourceCoverage,
            netCost = settings.rollCost,
            fits = true,
            rollChance = rollChance,
            valueGained = rollChance * averageGoodScore
        };

        s2.score = s2.valueGained - settings.rollCost - settings.rollMargin;

        var names = new List<string>();
        for (int i = 0; i < Mathf.Min(3, targets.Count); i++)
        {
            names.Add(targets[i].item.Name);
            Item clone = SafeClone(targets[i].item);
            if (clone != null) s2.rollTargets.Add(clone);
        }

        s2.description = $"Roll the shop (cost {settings.rollCost}): {rollChance:P0} chance to find something better, e.g. {string.Join(", ", names)}";
        s2.notes.Add($"{goodWeight / totalWeight:P0} of the pool is worth buying now; the shop shows {slots} item(s) per roll.");
        s2.notes.Add($"Average score of a good item {averageGoodScore:F1}, minus roll cost {settings.rollCost} and margin {settings.rollMargin:F1}.");

        return s2;
    }

    // ============================================================
    // RECOMMENDATION
    // ============================================================

    private AIRecommendation BuildRecommendation()
    {
        bool shopKnown = itemsInShop != null && itemsInShop.Count > 0;

        // Best scenario the player can act on now. If the shop contents are known,
        // pool-only items are not actionable.
        AIScenario actionable = null;
        AIScenario upcoming = null;

        foreach (AIScenario s in scenarios)
        {
            if (s.score < settings.minimumScore) break; // sorted descending

            bool isActionable = !shopKnown || s.inShop;

            if (isActionable)
            {
                actionable = s;
                break;
            }

            upcoming ??= s;
        }

        if (actionable != null)
        {
            string summary = $"{actionable.description} (score {actionable.score:F1}, win rate {actionable.winRateBefore:P0} -> {actionable.winRateAfter:P0})";

            if (upcoming != null && upcoming.score > actionable.score)
                summary += $" | Better later if it appears in the shop: {upcoming.description}";

            return new AIRecommendation
            {
                action = actionable.action,
                scenario = actionable,
                summary = summary
            };
        }

        string why;
        if (scenarios.Count == 0)
        {
            why = "No scenarios were generated (empty shop?).";
        }
        else
        {
            AIScenario best = scenarios[0];
            why = $"Best option was '{best.description}' with score {best.score:F1} " +
                  $"(gained {best.valueGained:F1}, lost {best.valueLost:F1}, cost {best.netCost:F1}). " +
                  $"Needed >= {settings.minimumScore}.";

            if (upcoming != null)
                why += $" Worth saving for if it appears in the shop: {upcoming.description}.";
        }

        return new AIRecommendation
        {
            action = AIActionType.Wait,
            summary = "Waiting. " + why
        };
    }

    // ============================================================
    // STORING SPACE HELPERS
    // ============================================================

    /// <summary>true = cell is free to place on. Built from Cells only, so no clones needed.</summary>
    private bool[,] BuildFreeGrid()
    {
        StoringCellType[,] cells = storingSpace?.Cells;
        if (cells == null) return new bool[0, 0];

        int w = cells.GetLength(0);
        int h = cells.GetLength(1);
        var free = new bool[w, h];

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                free[x, y] = cells[x, y] == StoringCellType.Available;

        return free;
    }

    private static int CountFree(bool[,] free)
    {
        int n = 0;
        foreach (bool f in free) if (f) n++;
        return n;
    }

    private static List<RepackSource> GatherUnplaced()
    {
        var list = new List<RepackSource>();

        foreach (var mono in Object.FindObjectsByType<ItemWithFootprintMono>())
        {
            if (mono != null && mono.bought && !mono.isAddedToInventory && mono.Item?.Footprint != null)
                list.Add(new RepackSource { item = mono.Item, stored = null, rotated = mono.Rotated });
        }

        return list;
    }

    /// <summary>
    /// The item's footprint in its UNROTATED state. Stored items keep a rotated Footprint,
    /// but SortAllItems resets rotation to zero before packing, and the rotation we report
    /// is "number of Rotate() calls from zero" (what PlaceFromSort expects).
    /// </summary>
    private static Footprint Canonical(Footprint footprint, int rotated)
    {
        Footprint c = footprint.Clone();
        int undo = (4 - ((rotated % 4) + 4) % 4) % 4;
        for (int i = 0; i < undo; i++) c.Rotate();
        return c;
    }

    /// <summary>
    /// "What if I re-pack everything (all stored items, minus `excluded`, plus bought-but-unplaced
    /// items) together with `candidate`?" Uses the same algorithm as InventorySortUIMono.SortAllItems.
    /// </summary>
    private RepackPlan SimulateRepackWith(Item candidate, StoredObject excluded)
    {
        var sources = new List<RepackSource>();

        foreach (StoredObject so in inventory.Items)
        {
            if (so == null || so == excluded || so.Item?.Footprint == null) continue;
            sources.Add(new RepackSource { item = so.Item, stored = so, rotated = so.Rotated });
        }

        sources.AddRange(unplacedSources);
        sources.Add(new RepackSource { item = candidate, stored = null, rotated = 0 });

        var footprints = new List<Footprint>(sources.Count);
        foreach (RepackSource src in sources)
            footprints.Add(Canonical(src.item.Footprint, src.rotated));

        return new RepackPlan
        {
            Result = InventorySortService.SimulateRepack(footprints, storingSpace?.Cells),
            Sources = sources,
            CandidateIndex = sources.Count - 1
        };
    }

    // ============================================================
    // ENTITY CLONES (so the UI can hold a scenario's data safely)
    // ============================================================

    private void AttachEntities(AIScenario s, Item candidate, StoredObject sold, RepackPlan plan)
    {
        s.buyItem = SafeClone(candidate);

        if (sold != null)
            s.sellStoredObject = SafeClone(sold);

        if (s.fits)
        {
            Footprint final = candidate.Footprint.Clone();
            for (int i = 0; i < s.placementRotations; i++) final.Rotate();
            s.placementFootprint = final;

            Item placedItem = SafeClone(candidate);
            if (placedItem != null)
                s.placedBuyObject = new StoredObject(placedItem, s.placementRotations, s.placementPivot);
        }

        if (s.requiresReorganize && plan != null)
        {
            s.repackMoves = BuildRepackMoves(plan);
            s.itemsMoved = 0;
            foreach (AIRepackMove move in s.repackMoves)
                if (move.Moved) s.itemsMoved++;
        }
    }

    private static List<AIRepackMove> BuildRepackMoves(RepackPlan plan)
    {
        var moves = new List<AIRepackMove>();

        foreach (RepackPlacement placement in plan.Result.Placements)
        {
            if (placement.Index == plan.CandidateIndex) continue; // described by placedBuyObject

            RepackSource src = plan.Sources[placement.Index];
            bool wasPlaced = src.stored != null;

            moves.Add(new AIRepackMove
            {
                item = SafeClone(src.item),
                wasPlaced = wasPlaced,
                oldPivot = wasPlaced ? src.stored.Pivot : Vector2Int.zero,
                oldRotation = ((src.rotated % 4) + 4) % 4,
                newPivot = placement.Pivot,
                newRotation = placement.Rotation,
                newFootprint = placement.FinalFootprint.Clone()
            });
        }

        return moves;
    }

    // Item.Clone() dereferences ProgressTimer/Footprint, so a half-configured item would throw.
    private static Item SafeClone(Item item)
    {
        if (item == null) return null;

        try { return item.Clone(); }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AIHelper] Could not clone item '{item.Name}': {e.Message}");
            return null;
        }
    }

    private static StoredObject SafeClone(StoredObject stored)
    {
        if (stored == null) return null;

        try { return stored.Clone(); }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AIHelper] Could not clone stored object: {e.Message}");
            return null;
        }
    }

    private static bool TryFindPlacement(bool[,] free, Footprint footprint, out Vector2Int pivot, out int rotations)
    {
        pivot = Vector2Int.zero;
        rotations = 0;

        if (footprint == null) return false;

        int w = free.GetLength(0);
        int h = free.GetLength(1);
        Footprint test = footprint.Clone();

        for (int r = 0; r < 4; r++)
        {
            List<Vector2Int> local = test.ToSpace(Vector2Int.zero);

            if (local.Count > 0)
            {
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        bool ok = true;

                        foreach (Vector2Int c in local)
                        {
                            int px = c.x + x;
                            int py = c.y + y;

                            if (px >= w || py >= h || !free[px, py])
                            {
                                ok = false;
                                break;
                            }
                        }

                        if (ok)
                        {
                            pivot = new Vector2Int(x, y);
                            rotations = r;
                            return true;
                        }
                    }
                }
            }

            test.Rotate();
        }

        return false;
    }
}