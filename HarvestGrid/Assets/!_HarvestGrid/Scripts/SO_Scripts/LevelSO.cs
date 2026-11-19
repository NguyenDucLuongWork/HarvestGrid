using GameJson;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Crop requirement for a level. Holds only ID string, stars, and amount.
/// Inherits StateEntity and uses [ContextData] for clean AI JSON serialization.
/// </summary>
[Serializable]
public class CropRequirement : StateEntity
{
    [ContextData] [Range(1, 5)] public int stars = 1;
    [ContextData] [Min(1)] public int amount = 1;

    public string CropID => id;

    public CropRequirement() : base()
    {
    }

    public CropRequirement(string cropId, int stars = 1, int amount = 1) : base(cropId)
    {
        this.stars = Mathf.Clamp(stars, 1, 5);
        this.amount = Mathf.Max(1, amount);
    }
}

/// <summary>
/// Item chance entry for shop pool. Holds only ID string and chance weight.
/// Inherits StateEntity and uses [ContextData] for clean AI JSON serialization.
/// </summary>
[Serializable]
public class ItemChanceEntry : StateEntity
{
    [ContextData] [Min(0f)] public float chance = 1f;

    public string ItemID => id;

    public ItemChanceEntry() : base()
    {
    }

    public ItemChanceEntry(string itemId, float chance = 1f) : base(itemId)
    {
        this.chance = Mathf.Max(0f, chance);
    }
}

[CreateAssetMenu(fileName = "LevelSO", menuName = "Scriptable Objects/LevelSO")]
public class LevelSO : ScriptableObject
{
    [ContextData] public int levelID;
    [ContextData] public float timeBudget = 120f;
    [ContextData] public int money = 50;
    public FarmMono farmMono;

    public StoringSpaceSO storingSpaceSO;

    // Clean data for AI & serialization (ID strings only, zero Unity object references)
    [ContextData] [SerializeField] public List<CropRequirement> requiredCrops = new();
    [ContextData] [SerializeField] public List<ItemChanceEntry> itemPool = new();

    // Runtime dictionaries kept for compatibility with Shop, GoalItemSuggestionManager, etc.
    [SerializeField] public Dictionary<Crop, int> requiringCrops = new();
    [SerializeField] public Dictionary<ItemPrototype, float> itemAndChancePool = new();

    public IReadOnlyList<CropRequirement> RequiredCrops => requiredCrops;
    public IReadOnlyList<ItemChanceEntry> ItemPool => itemPool;

    // ------------------------------------------------------------ null safety & initialization
    public void EnsureInitialized()
    {
        requiredCrops ??= new List<CropRequirement>();
        itemPool ??= new List<ItemChanceEntry>();
        requiringCrops ??= new Dictionary<Crop, int>();
        itemAndChancePool ??= new Dictionary<ItemPrototype, float>();
    }

    private void OnEnable() => EnsureInitialized();

    // ------------------------------------------------------------ Add helpers
    public void AddRequiredCrop(string cropId, int stars = 1, int amount = 1)
    {
        if (string.IsNullOrEmpty(cropId)) return;
        EnsureInitialized();

        stars = Mathf.Clamp(stars, 1, 5);
        amount = Mathf.Max(1, amount);

        var existing = requiredCrops.Find(r => r.CropID == cropId && r.stars == stars);
        if (existing != null)
        {
            existing.amount += amount;
        }
        else
        {
            requiredCrops.Add(new CropRequirement(cropId, stars, amount));
        }
    }

    public void AddItemChance(string itemId, float chance = 1f)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        EnsureInitialized();

        var existing = itemPool.Find(e => e.ItemID == itemId);
        if (existing != null)
        {
            existing.chance += Mathf.Max(0f, chance);
        }
        else
        {
            itemPool.Add(new ItemChanceEntry(itemId, chance));
        }
    }

    // ------------------------------------------------------------ Inspector context menu actions
    [ContextMenu("Debug AI Clean JSON")]
    public void DebugAICleanJson()
    {
        EnsureInitialized();
        string json = CleanJson.SimplifyObject(this, CleanJson.Options.ForAI());
        Debug.Log($"[LevelSO AI Clean JSON (Level {levelID})]:\n{json}");
    }

    [ContextMenu("Sync Clean Data From Runtime Dictionaries")]
    public void SyncFromRuntimeDictionaries()
    {
        EnsureInitialized();
        requiredCrops.Clear();
        foreach (var kvp in requiringCrops)
        {
            if (kvp.Key == null || string.IsNullOrEmpty(kvp.Key.CropID)) continue;
            AddRequiredCrop(kvp.Key.CropID, kvp.Key.Stars, kvp.Value);
        }

        itemPool.Clear();
        foreach (var kvp in itemAndChancePool)
        {
            if (kvp.Key == null || kvp.Key.item == null || string.IsNullOrEmpty(kvp.Key.item.Id)) continue;
            AddItemChance(kvp.Key.item.Id, kvp.Value);
        }
    }

    /// <summary>Moves legacy data from dictionaries into clean lists if clean lists are empty.</summary>
    public bool HasLegacyData =>
        (requiredCrops == null || requiredCrops.Count == 0) &&
        ((requiringCrops != null && requiringCrops.Count > 0) || (itemAndChancePool != null && itemAndChancePool.Count > 0));

    public void MigrateLegacy()
    {
        SyncFromRuntimeDictionaries();
    }
}
