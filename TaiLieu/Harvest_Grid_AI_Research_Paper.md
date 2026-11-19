# Harvest_Grid_AI_Research_Paper

---

# An AI-Assisted Goal-Driven Item Suggestion Framework for an Inventory-Grid and Auto-Farming Mobile Game

**[Author Names]**  
*Faculty of Information Technology, [University Name], Vietnam*  
*Email: {author1, author2, ...}@university.edu.vn*

---

## Abstract

Inventory management and farming simulation are two well-established gameplay genres, yet their integration into a cohesive mobile experience presents unique design challenges—particularly in guiding players to configure their inventories effectively to maximise farming output. This paper presents **Harvest Grid**, a Unity-based Android mobile game that combines a Tetris-style two-dimensional inventory grid with an automated plant growth and harvesting system. We describe a **rule-based, goal-driven item suggestion framework** implemented as `GoalItemSuggestionManager`, which analyses the current level objectives, player inventory state, and available grid space to visually suggest missing items directly on the inventory grid as ghost overlays. The system uses a priority-ordered role-matching algorithm (`Seed → Watering → HarvestTool → Fertilizer`) and a first-fit grid placement procedure with four-rotation search to identify and display actionable suggestions without blocking player interaction. The framework operates entirely on the client device (no server inference), integrates with an event-driven inventory architecture, and updates reactively on inventory changes and on a 45-second periodic loop. We discuss the system architecture including the `Farm → FarmSlot → Plant → PlantStage` growth model, the `ItemUse` factory pattern, `ScriptableObject`-based static data, and `PlayFab`-backed cloud persistence. Experimental evaluation is based on functional correctness testing and design review; full user-study evaluation is identified as future work.

**Index Terms:** inventory management game, auto-farming, item suggestion, rule-based AI, mobile game, Unity, ScriptableObject, adaptive gameplay

---

## I. Introduction

Inventory management has emerged as a primary gameplay mechanic in a growing category of modern games—from puzzle-RPGs such as *Backpack Hero* [1] to survival-crafting titles—where the spatial arrangement of items directly determines combat or production efficiency. Simultaneously, farming simulation games, popularised by series such as *Stardew Valley* [2], engage players through plant growth cycles, resource accumulation, and time management. Despite their individual popularity, the tight mechanical coupling of inventory configuration and farming output within a single mobile-first title represents a relatively unexplored design space.

A core challenge in such combined systems is **player guidance**: the combinatorial space of item placements on a grid is large, and new players may not immediately recognise which items are needed to progress through a level's crop requirements. Traditional approaches rely on static tutorials or hint text, which are cognitively disconnected from the actual inventory state. A context-sensitive, in-situ suggestion mechanism—one that understands the player's current inventory and the level's goals—can bridge this gap without imposing prescriptive control.

This paper makes the following contributions:

1. **System design** of Harvest Grid, an Android mobile game integrating a footprint-based 2D inventory grid with a resource-driven plant growth model.
2. **Description of the `GoalItemSuggestionManager`**, a rule-based, goal-driven item suggestion component that provides visual placement hints directly on the inventory grid.
3. **Analysis of the `ItemUse` factory architecture**, supporting three specialised item use types (`AddPlantUse`, `HarvestUse`, `ProviceResourceUse`) that connect inventory contents to farm outcomes.
4. **Discussion of the data architecture**, contrasting `ScriptableObject`-based static configuration with JSON-serialised dynamic player state and `PlayFab` cloud persistence.

The remainder of this paper is organised as follows: Section II reviews related work. Section III describes the system design. Section IV details the AI-based suggestion mechanism. Section V covers implementation. Section VI reports evaluation. Section VII discusses findings and limitations. Section VIII concludes.

---

## II. Related Work

### A. Inventory Management in Games

Inventory systems range from simple list-based containers to spatially constrained grids. Stankovic et al. [3] categorise game inventory designs by their spatial constraints and player cognitive load. Grid-based inventories with shape-dependent item placement—popularised by titles such as *Resident Evil 4* and *Escape from Tarkov*—introduce a combinatorial puzzle layer where item footprint and rotation must be considered. Harvest Grid adopts this paradigm, using per-item `Footprint` objects (boolean matrices) to encode spatial requirements.

### B. Procedural Content Generation and Adaptive Systems

Togelius et al. [4] survey procedural content generation (PCG) in games, distinguishing runtime content generation from pre-authored parametric configuration. Item pools in Harvest Grid are configured via `LevelSO.itemPool` with weighted probabilities, representing a lightweight parametric approach rather than full PCG. Shaker et al. [5] discuss search-based PCG, while Smith et al. [6] address mixed-initiative design tools; both are relevant to the design of level-specific item pools.

### C. Dynamic Difficulty Adjustment

Hunicke [7] introduced the concept of dynamic difficulty adjustment (DDA) as a mechanism for maintaining player engagement by modulating challenge in response to performance. Zohaib [8] provides a comprehensive survey of DDA approaches including rule-based, model-based, and reinforcement-learning methods. Harvest Grid's current implementation does not include a DDA component; `GoalItemSuggestionManager` focuses on inventory guidance rather than difficulty modulation. DDA is identified as a direction for future work.

### D. Rule-Based AI in Games

Many commercially successful game AI systems rely on rule-based decision logic rather than machine learning. Millington and Funge [9] categorise game AI techniques, noting that decision trees and finite state machines remain dominant in production games due to their interpretability and low runtime cost. The suggestion system in Harvest Grid falls into this category: it encodes domain knowledge as prioritised role-checking rules and applies first-fit spatial reasoning, without requiring training data or learned parameters.

### E. Mobile Game Architecture and Cloud Services

PlayFab [10] (Microsoft Azure PlayFab) provides backend-as-a-service infrastructure for game developers, including authentication, cloud save, and leaderboards. Research on mobile game persistence architectures (e.g., [11]) highlights the importance of local-first save strategies with cloud synchronisation, which Harvest Grid implements via `DataPersistenceManager`.

---

## III. System Design and Architecture

### A. Overview

Harvest Grid is a single-player mobile game built with Unity (C#) targeting Android. The player progresses through a series of levels, each defined by a set of crop requirements (`requiredCrops`) and a time budget (`timeBudget`). The player must configure their inventory grid with the correct combination of items and then use those items to grow and harvest the required crops within the time limit.

The system comprises three architectural layers (Fig. 2):

- **UI Layer**: UGUI-based screens — `LoginScene`, `HomePanel`, `GameplayScene` (with embedded inventory grid and farm grid), `ShopPanel`, `SlotUpgradePanel`, `SettingPanel`.
- **Game Logic Layer**: Singleton managers — `GameManager`, `AuthManager`, `LevelsManager`, `ItemManager`, `FarmMono`, `InventoryMono`, `GoalItemSuggestionManager`, `LeaderboardManager`, `DataPersistenceManager`.
- **Data Layer**: Divided into static `ScriptableObject`-based configuration and dynamic JSON-serialised runtime state.

### B. Inventory Grid Model

The inventory is modelled as a `StoringSpace` containing a two-dimensional cell array (`StoringCellType[,]`). Each cell is either `Available`, `Occupied`, or `Unavailable`. Items are represented by an `Item` class referencing a `Footprint` object—a boolean matrix encoding the item's shape. Placing an item requires finding a valid position where all footprint cells map to `Available` grid cells.

`Footprint` supports four-rotation search via `Footprint.Rotate()`, which permutes the boolean matrix by 90° clockwise. The `ItemFootprintSO` ScriptableObject provides the editor-designed shape for each item type, serialised from a grid-based editor tool.

**Items are categorised by `ItemRole`:**
- `Seed`: carries an `AddPlantUse`, which plants a specific plant species when applied to an empty `FarmSlot`.
- `Watering` / `Fertilizer` / growth buffs: carry `ProviceResourceUse`, which supplies a typed resource to a target plant.
- `HarvestTool`: carries `HarvestUse`, which harvests a fully-grown plant with a configurable efficiency multiplier.

### C. Farm System

The farm is modelled as a `Farm` containing a list of `FarmSlot` objects. Each slot holds at most one `Plant` and has attributes for soil quality, soil moisture, and an upgrade level. When a seed item is used on an empty slot, `AddPlantUse.Apply()` calls `FarmMono.AddPlant()`, which invokes `Plant.BeginGrow()` to advance the plant to its first growth stage.

**Plant growth model** (Fig. 3):

A `Plant` maintains:
- `stages`: a remaining list of `PlantStage` objects, each specifying `requiredResources` as a `Dictionary<Resource, int>`.
- `nextStageRequirement`: the resource requirements for the next stage transition.
- `currentState`: the currently active `PlantStage` (determines visual sprite).
- `IsFullyGrown`: true when `stages.Count == 0`.

Growth advances when `Plant.Absorb(resource, amount)` is called and `nextStageRequirement` is completely satisfied. At that point, `GrowToNextStage()` dequeues the next stage and fires `OnStageChanged`. The visual representation is updated via `PlantMono.UpdateSprite()`.

**Harvest model:**
When `HarvestUse.Apply(effective)` is invoked, `Plant.Harvest(efficiency)` computes:

```
totalYield = max(1, round(averageCropCount × efficiency))
baseStar   = clamp(floor(efficiency × 2), 1, 5)
secondStar = baseStar < 5 ? baseStar + 1 : baseStar - 1
primaryAmount   = ceil(totalYield × 0.6)
secondaryAmount = totalYield - primaryAmount
```

The result is a `Dictionary<Crop, int>` with up to two quality tiers, added to `Inventory.crops` via `Inventory.AddCrop()`.

### D. Static Data Architecture

Static game configuration is encoded as Unity `ScriptableObject` assets packed with the application binary (Table I):

**Table I. ScriptableObject Assets**

| Class | Asset Instances | Key Fields |
|-------|----------------|------------|
| `LevelSO` | 5 (Level 1–5) | `levelID`, `timeBudget`, `money`, `requiredCrops[]`, `itemPool[]` with chance weights |
| `CropSO` | 4 (Bean, Cucumber, Tomato, Wheat) | `crop` definition |
| `ItemSO` | 11 items | `Role`, `Rarity`, `Icon`, `footprintSO`, `ItemUse` config |
| `ItemFootprintSO` | 8 shapes | `width`, `height`, `bool[] grid`, `Footprint` |
| `StoringSpaceSO` | 2 | grid dimensions and cell layout |
| `GameplaySceneDataSO` | 1 | scene-level configuration |

### E. Dynamic Data and Persistence

Dynamic player state is serialised via `DataPersistenceManager` (from the `LgTyLib.Modules.DataPersistence` library) as local JSON files. The persistence schema covers:

| Data Group | Key Fields | Storage |
|-----------|-----------|---------|
| `UserData` | `userId`, `username` | Local JSON + PlayFab |
| `Inventory` | `money`, `crops`, `storedItems` | Local JSON |
| `Farm` | `FarmSlot[]` → `Plant` → `PlantStage[]` | Local JSON |
| `Settings` | graphics/audio preferences | Local JSON |
| `LeaderboardData` | scores, rankings | PlayFab Cloud |

**Persistence flow:** On application start, `GameManager.Setup()` calls `UserManager.Instance.Load()` → `DataPersistenceManager.LoadGame()` → reads local JSON. If the player authenticates via PlayFab, `PlayFabAuthService.Login()` subsequently calls `DataPersistenceManager.LoadCloudSaves()` to load cloud-saved data. Save operations are symmetric.

### F. Authentication and Cloud Architecture

`AuthManager` abstracts authentication behind an `IAuthService` interface with two concrete implementations: `LocalAuthService` (guest/offline mode) and `PlayFabAuthService` (account mode). The runtime instance is `PlayFabAuthService`, connected to PlayFab Title ID `14B566`. All game logic, rendering, and AI inference execute locally on the Android device; PlayFab serves exclusively as an authentication provider, cloud save host, and leaderboard service (Fig. 1 — Deployment Architecture).

---

## IV. AI-Based Item Suggestion Mechanism

### A. Objective and Scope

`GoalItemSuggestionManager` addresses the guidance problem: given a level's crop requirements and the player's current inventory, which items is the player missing, and where could they be placed? The output is a set of *visual ghost overlays* displayed directly on the inventory grid, using a semi-transparent yellow silhouette with a pulse animation (`GhostPulseAnimation`). This design keeps the suggestion spatially grounded and immediately actionable, reducing the cognitive distance between hint and action.

**Scope clarification:** This system is a *guidance aid*, not a difficulty adjustment mechanism. It does not modify item drop rates, adjust crop growth timing, change `timeBudget`, or alter any gameplay parameter. Dynamic Difficulty Adjustment is not implemented in the current version.

### B. Input Features

The manager reads the following state at each evaluation (Table II):

**Table II. Input Features of GoalItemSuggestionManager**

| Input Feature | Source | Description |
|--------------|--------|-------------|
| `levelSO.requiringCrops` | `LevelSO` (Static) | Target crops and quantities for the current level |
| `Inventory.Crops` | Runtime Inventory | Crops already harvested and accumulated |
| `Inventory.GetItemList()` | Runtime Inventory | Items currently held by the player |
| `StoringSpace.Cells[,]` | Runtime Grid State | Available vs. occupied cells in inventory grid |
| `ghostOccupiedCells` | Internal State | Cells reserved by previously shown ghost suggestions |

### C. Decision Algorithm

The suggestion algorithm follows a priority-ordered rule-based procedure (Algorithm 1):

```
Algorithm 1: RecalculateSuggestion

Input: levelSO, inventory, storingSpace, activeGhosts
Output: updated ghost overlay set

priorityRoles ← [Seed, Watering, HarvestTool, Fertilizer]
missingCandidates ← []
goalIncomplete ← false

For each (goalCrop, goalAmount) in levelSO.requiringCrops:
    currentAmount ← count matching crops in inventory by CropID and Stars
    If currentAmount >= goalAmount: continue
    goalIncomplete ← true
    
    For each role in priorityRoles:
        candidates ← CropItemRegistry.GetItemsForCrop(goalCrop, role)
        If player has at least one candidate in inventory: skip
        Else: add first valid candidate to missingCandidates

If not goalIncomplete: ClearAllSuggestions(); return

Remove ghosts for items no longer missing (player purchased them)

nextCandidate ← first missingCandidate not yet in activeGhosts
If nextCandidate exists:
    placement ← FindFirstFitPlacement(nextCandidate.footprint, grid)
    If placement found: SpawnGhost(nextCandidate, placement)
```

**Spatial placement** uses a first-fit search over the occupied grid, trying all four rotations of the item's `Footprint`. The `CanPlace` check verifies that all footprint cells map to unoccupied, in-bounds grid cells. Ghost-occupied cells are tracked in `ghostOccupiedCells` to prevent overlap between multiple simultaneous suggestions.

### D. Output and Integration

When a valid placement is found, `CreateVisualGhost()` instantiates a `GameObject` with an `Image` component rendered using `suggestionColor` (semi-transparent yellow) and a `GhostPulseAnimation` component. The ghost is positioned using `GridLayoutGroupHelper.GetCellWorldCorners()` to align with the actual inventory grid cell boundaries. Ghost `raycastTarget` is set to `false` to avoid interfering with player drag operations.

The suggestion system integrates with the inventory through event subscriptions:

```csharp
Inventory.OnItemsChanged += OnInventoryChanged;  // immediate recalc
Inventory.OnCropsChanged += OnCropsChanged;       // goal completion check
```

A periodic `SuggestionLoop()` coroutine recalculates every 45 seconds as a fallback.

### E. AI Classification

The system is explicitly a **rule-based decision system**. It encodes expert knowledge (which item roles are needed for which crops, in what priority order) as conditional rules evaluated against runtime state. It does not use machine learning, neural networks, training data, or parameter updates based on player history. This distinction is important for academic accuracy; the system should not be described as an adaptive or personalised AI.

---

## V. Implementation

### A. Technology Stack

- **Engine**: Unity 2022.x (LTS), C#
- **Backend**: Microsoft Azure PlayFab (SDK: `PlayFab.ClientModels`)
- **Persistence**: `LgTyLib.Modules.DataPersistence` (custom library with `[ContextData]` attribute for selective serialisation)
- **UI**: Unity UGUI with procedural grid layout (`GridLayoutGroupHelper`)
- **Other packages**: LLMUnity (present in project; integration with gameplay not confirmed in this version)

### B. ItemUse Factory Pattern

Item behaviours are implemented using an abstract factory pattern centred on `ItemUse`:

```csharp
// Abstract base
public abstract class ItemUse {
    public abstract ItemUseType UseType { get; }
    public abstract void Apply(ItemUseContext ctx);
}

// Concrete: planting
[ItemUseType(ItemUseType.AddPlant)]
public class AddPlantUse : ItemUse {
    private Plant plantToAdd;
    public override void Apply(ItemUseContext ctx) =>
        FarmMono.Instance.AddPlant(plantToAdd.Clone(), ctx.TargetSlot);
}

// Concrete: harvesting
[ItemUseType(ItemUseType.Harvest)]
public class HarvestUse : ItemUse {
    private float effective;
    public override void Apply(ItemUseContext ctx) =>
        FarmMono.Instance.HarvestRandom(ctx.TargetSlot, effective);
}

// Concrete: resource provision
public class ProviceResourceUse : ItemUse {
    // provides typed Resource to target Plant
    public override void Apply(ItemUseContext ctx) =>
        // Plant.Absorb(resourceType, amount)
}
```

`ItemUseFactory` uses the `[ItemUseType]` attribute for runtime type resolution, enabling the system to deserialise item use configurations from `ScriptableObject` assets without hard-coded type switching.

### C. AI Runtime Location

`GoalItemSuggestionManager` runs entirely within the Unity `MonoBehaviour` lifecycle on the Android device. No network calls are made during suggestion evaluation. The `CropItemRegistry` is a local `ScriptableObject`-derived registry mapping crop types to item prototypes.

### D. Data Serialisation

The `[ContextData]` attribute marks fields eligible for serialisation by `DataPersistenceManager`. This selective marking avoids serialising Unity `GameObject` references (which are not JSON-serialisable) while capturing the full game state. Deep-copy is handled via the `ICloneable<T>` interface implemented by `Farm`, `FarmSlot`, `Plant`, `PlantStage`, `Inventory`, and `Crop`.

---

## VI. Experiments and Evaluation

### A. Evaluation Approach

Given the stage of the project (functional prototype), evaluation consists of:

1. **Functional correctness tests**: verifying that individual system components behave as specified.
2. **Integration tests**: verifying cross-system interactions (item use → farm state → inventory update → suggestion recalculation).
3. **Design review**: structured assessment of the suggestion system's decision logic against intended behaviour.

No controlled user study or A/B experiment has been conducted at the time of writing. Quantitative evaluation with participants is identified as a primary direction for future work.

### B. Functional Test Scenarios

**Table III. Functional Test Scenarios**

| ID | Scenario | Expected Outcome | Status |
|----|----------|-----------------|--------|
| T1 | Apply `AddPlantUse` on empty `FarmSlot` | Plant appears at Stage 0, `FarmSlot.IsEmpty()` returns `false` | ✅ Implemented |
| T2 | Apply `ProviceResourceUse` satisfying full `nextStageRequirement` | `Plant.GrowToNextStage()` fires, visual sprite updates | ✅ Implemented |
| T3 | Apply `HarvestUse` with `effective = 1.0` on fully-grown plant | `Inventory.crops` updated with yield, slot cleared | ✅ Implemented |
| T4 | `HarvestUse` with `effective = 2.0` | `baseStar = clamp(4, 1, 5) = 4`; higher-tier crops generated | ✅ Implemented |
| T5 | `GoalItemSuggestionManager` when player missing Seed | Ghost overlay appears at valid grid position | ✅ Implemented |
| T6 | Player purchases suggested item → ghost clears | Ghost removed from `activeGhosts` and grid | ✅ Implemented |
| T7 | All `requiredCrops` met → all suggestions clear | `ClearAllSuggestions()` called | ✅ Implemented |
| T8 | PlayFab login → cloud save loaded | `AuthSession` populated, `DataPersistenceManager.LoadCloudSaves()` called | ✅ Implemented |
| T9 | Guest mode → local save only | No PlayFab calls; `LocalAuthService` path | ✅ Implemented |
| T10 | `Farm.ValidateData()` clears stale plant data | Plants with empty `PlantID` removed from slots | ✅ Implemented |

### C. Suggestion System Accuracy

The suggestion logic was reviewed against three representative level configurations. In all cases, the priority ordering correctly identified the most foundational missing item (Seed before Watering tool, Watering before HarvestTool). The first-fit placement algorithm correctly handles grid states with varying occupancy patterns, including rotated items.

### D. Known Limitations

- **No user study data**: the subjective helpfulness of ghost suggestions has not been measured with real players.
- **No DDA**: difficulty remains static within a level; `timeBudget` and crop requirements are fixed in `LevelSO`.
- **No offline fallback detection**: the system does not gracefully handle `PlayFab` connectivity failures with explicit UI feedback.
- **Leaderboard evaluation**: the fairness and anti-cheat properties of the `PlayFab` leaderboard integration have not been formally assessed.

---

## VII. Discussion

### A. Design Implications

The integration of a footprint-based inventory with a resource-driven growth model creates a meaningful strategic link: the player's choice of which items to hold (and how to arrange them spatially) directly determines farming efficiency. The `GoalItemSuggestionManager` makes this link legible by surfacing the gap between current inventory and level requirements in situ.

Placing suggestions *on the inventory grid* (rather than in a separate UI panel) reduces the indirection between hint and action. The ghost overlay's transparency and pulse animation signal intent without occluding actual inventory contents, and the `raycastTarget = false` setting preserves full drag interactivity.

### B. Generalisability

The suggestion framework is not specific to farming: any game with a goal-driven inventory system where item roles can be classified can apply the same `priorityRole → missingCandidates → first-fit placement` pipeline. The `CropItemRegistry` abstraction decouples the goal-role-item mapping from the core suggestion logic, enabling extension to new crop types and item roles without modifying `GoalItemSuggestionManager`.

### C. Comparison with Alternative Approaches

A machine-learning approach (e.g., recommendation based on play history) could potentially personalise suggestions to individual player tendencies. However, it would require: (a) collected play-session data, (b) a training pipeline, and (c) handling of cold-start for new players. The rule-based approach provides deterministic, immediately interpretable suggestions with zero training cost and no cold-start problem—appropriate for a game with a small, well-defined item space and clear level objectives.

### D. Limitations

- The suggestion system does not consider item synergies (e.g., pairing a GrowthBuff with a specific Seed type for efficiency); it only checks for role coverage.
- The `first-fit` placement is not optimal: it may suggest a spatially suboptimal position compared to an inventory-aware packing heuristic.
- The 45-second polling interval is a fixed constant; an adaptive interval based on activity could reduce unnecessary computation.

---

## VIII. Conclusion and Future Work

This paper presented **Harvest Grid**, a Unity-based Android mobile game that couples a footprint-based 2D inventory grid with a resource-driven auto-farming model across five configurable levels. The core technical contributions include:

1. The `Farm → FarmSlot → Plant → PlantStage` growth model, where plant advancement is driven by resource absorption from player-used items.
2. The `ItemUse` factory pattern (`AddPlantUse`, `HarvestUse`, `ProviceResourceUse`), which connects inventory item configuration to concrete farm actions.
3. The `GoalItemSuggestionManager`, a rule-based, goal-driven item suggestion system that provides in-situ visual guidance on the inventory grid.
4. A hybrid persistence architecture combining local JSON serialisation with PlayFab cloud save and authentication.

**Future work** includes:

- **Dynamic Difficulty Adjustment**: implement a DDA layer that monitors player performance metrics (time remaining, harvest efficiency, level completion rate) and adjusts `LevelSO` parameters (or selects among predefined difficulty tiers) at runtime.
- **User study**: conduct a controlled experiment comparing player performance and experience with and without the suggestion system.
- **ML-based personalisation**: explore using play-session data collected via PlayFab to train a lightweight recommendation model that adapts suggestion priority to individual player styles.
- **Offline resilience**: improve handling of PlayFab connectivity failures with explicit user feedback and local-only fallback.
- **Synergy-aware suggestions**: extend the suggestion algorithm to consider item interaction effects (e.g., recommending buff items that complement the player's existing seed selection).

---

## References

[1] Cassette Beasts development team, "Backpack Hero," *Jaspel*, 2023. [Online]. Available: https://store.steampowered.com/app/1970580/Backpack_Hero/

[2] E. Barone, "Stardew Valley," *ConcernedApe*, 2016. [Online]. Available: https://www.stardewvalley.net/

[3] D. Stankovic, M. Ristic, and B. Protic, "Comparative analysis of inventory management mechanics in action-RPG games," in *Proc. Int. Conf. on Entertainment Computing (ICEC)*, Springer, 2019, pp. 212–221. *(Note: verify exact citation before final submission)*

[4] J. Togelius, G. N. Yannakakis, K. O. Stanley, and C. Browne, "Search-based procedural content generation: A taxonomy and survey," *IEEE Trans. Comput. Intell. AI Games*, vol. 3, no. 3, pp. 172–186, Sep. 2011. DOI: 10.1109/TCIAIG.2011.2148116

[5] N. Shaker, J. Togelius, and M. J. Nelson, *Procedural Content Generation in Games*, Springer, 2016. [Online]. Available: http://pcgbook.com/

[6] G. Smith, M. Treanor, J. Whitehead, and M. Mateas, "Rhythm-based level generation for 2D platformers," in *Proc. ACM SIGGRAPH Symp. Video Games (Sandbox)*, 2009, pp. 175–182. DOI: 10.1145/1581073.1581098

[7] R. Hunicke, "The case for dynamic difficulty adjustment in games," in *Proc. ACM SIGCHI Conf. Advances Computer Entertainment Technology (ACE)*, 2005, pp. 429–433. DOI: 10.1145/1178477.1178573

[8] M. Zohaib, "Dynamic difficulty adjustment (DDA) in computer games: A review," *Advances in Human-Computer Interaction*, vol. 2018, Article ID 5681652, 2018. DOI: 10.1155/2018/5681652

[9] I. Millington and J. Funge, *Artificial Intelligence for Games*, 2nd ed. Burlington, MA: Morgan Kaufmann, 2009.

[10] Microsoft, "Azure PlayFab," *Microsoft Azure Game Services*, 2023. [Online]. Available: https://azure.microsoft.com/en-us/products/playfab/

[11] Y. Jiang, J. Huang, and L. Wang, "Client-server architectures for mobile cloud gaming: A survey," *IEEE Access*, vol. 7, pp. 123051–123067, 2019. DOI: 10.1109/ACCESS.2019.2938263

---

## Appendix: Data Requiring Completion Before Submission

> This section lists items that must be resolved before the paper is ready for IEEE submission. Remove before final draft.

- [ ] **Author names and affiliations** (Section I heading)
- [ ] **Figure 1** (Deployment Architecture diagram) — embed rendered version of Diagram 3 from `diagrams_v7.md`
- [ ] **Figure 2** (Logical Architecture diagram) — embed rendered version of Diagram 2
- [ ] **Figure 3** (Plant Growth Activity diagram) — embed rendered version of Diagram 8
- [ ] **Figure 4** (AI Pipeline diagram) — embed rendered version of Diagram 9
- [ ] **User study data** — Table III is functional tests only; a user experiment is needed for a strong evaluation section
- [ ] **References [1], [3]**: verify exact publication details and DOIs; [1] is a game title (cite appropriately per IEEE style for software/games)
- [ ] **LLMUnity integration**: clarify whether LLMUnity package is used in gameplay; if yes, describe; if no, remove from Section V
- [ ] **DDA status**: confirm formally that DDA is not implemented; update Abstract and Section IV accordingly if any DDA logic is found after full codebase review
- [ ] **SlotUpgradePanel mechanics**: confirm purchase/upgrade conditions for Farm Slot; update Section III.C if confirmed
- [ ] **Page count compliance**: target 6–8 pages in IEEE two-column format; condense as needed
