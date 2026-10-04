using System;
using System.Collections.Generic;
using UnityEngine;

// ---- Legacy list entries (only used to migrate data saved by the previous version of this tool) ----
[Serializable]
public class CropRequirement
{
    public CropSO cropSO;
    [Range(1, 5)] public int stars = 1;
    [Min(1)] public int amount = 1;
}

[Serializable]
public class ItemChanceEntry
{
    public ItemPrototype item;
    [Min(0f)] public float chance = 1f;
}

[CreateAssetMenu(fileName = "LevelSO", menuName = "Scriptable Objects/LevelSO")]
public class LevelSO : ScriptableObject
{
    public string levelID;
    public int money;
    public FarmMono farmMono;
    public StoringSpaceSO storingSpaceSO;

    // Crop have star inside it. Same crop id but different crop's star is still different.
    // Needs Unity 6.6+ (built-in Dictionary serialization, opt-in with [SerializeField]).
    // NOTE: Crop must override Equals/GetHashCode (CropID + Stars) so lookups still work after loading.
    [SerializeField] public Dictionary<Crop, int> requiringCrops = new();
    [SerializeField] public Dictionary<ItemPrototype, float> itemAndChancePool = new();

    // ---- Legacy data, kept hidden so old assets can be migrated. Empty after migration. ----
    [SerializeField, HideInInspector] private List<CropRequirement> requiredCrops = new();
    [SerializeField, HideInInspector] private List<ItemChanceEntry> itemPool = new();

    public IReadOnlyList<CropRequirement> RequiredCrops => requiredCrops;

    // ------------------------------------------------------------ null safety
    public void EnsureInitialized()
    {
        requiringCrops ??= new Dictionary<Crop, int>();
        itemAndChancePool ??= new Dictionary<ItemPrototype, float>();
        requiredCrops ??= new List<CropRequirement>();
        itemPool ??= new List<ItemChanceEntry>();
    }

    private void OnEnable() => EnsureInitialized();

    // ------------------------------------------------------------ add helpers (create the dictionaries when null)
    public void AddRequiredCrop(CropSO cropSO, int stars = 1, int amount = 1)
    {
        if (cropSO == null || cropSO.crop == null) return;
        EnsureInitialized();

        stars = Mathf.Clamp(stars, 1, 5);
        amount = Mathf.Max(1, amount);

        // merge by CropID + Stars, independent of Equals/GetHashCode
        foreach (var key in requiringCrops.Keys)
        {
            if (key != null && key.CropID == cropSO.crop.CropID && key.Stars == stars)
            {
                requiringCrops[key] += amount;
                return;
            }
        }

        Crop crop = cropSO.crop.Clone();
        crop.SetStars(stars);
        requiringCrops[crop] = amount;
    }

    public void AddItem(ItemPrototype item, float chance = 1f)
    {
        if (item == null) return;
        EnsureInitialized();

        itemAndChancePool.TryGetValue(item, out float current);
        itemAndChancePool[item] = current + Mathf.Max(0f, chance);
    }

    // ------------------------------------------------------------ legacy migration
    public bool HasLegacyData =>
        (requiredCrops != null && requiredCrops.Count > 0) || (itemPool != null && itemPool.Count > 0);

    /// <summary>Moves data saved by the old list-based version into the dictionaries.</summary>
    public void MigrateLegacy()
    {
        EnsureInitialized();

        foreach (var req in requiredCrops)
            if (req != null) AddRequiredCrop(req.cropSO, req.stars, req.amount);

        foreach (var entry in itemPool)
            if (entry != null) AddItem(entry.item, entry.chance);

        requiredCrops.Clear();
        itemPool.Clear();
    }
}