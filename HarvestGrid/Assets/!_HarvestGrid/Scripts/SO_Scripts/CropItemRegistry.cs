using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class CropItemRelationship
{
    public CropSO cropSO;
    public ItemPrototype itemPrototype;
    public ItemRole role;
}

[CreateAssetMenu(fileName = "CropItemRegistry", menuName = "Scriptable Objects/CropItemRegistry")]
public class CropItemRegistry : ScriptableObject
{
    [Header("Auto Registration")]
    [Tooltip("Drag all ItemPrototypes here. The registry will auto-build relationships for Seeds, Water, and Fertilizer.")]
    public List<ItemPrototype> allItems = new List<ItemPrototype>();
    
    [Header("Manual Overrides / Missing Data")]
    [Tooltip("Use this to manually assign tools (e.g., Sickle to Tomato) since HarvestUse doesn't link to a specific crop.")]
    public List<CropItemRelationship> manualRelationships = new List<CropItemRelationship>();

    // Runtime Lookup: CropID -> List of (ItemPrototype, Role)
    private Dictionary<string, List<(ItemPrototype item, ItemRole role)>> lookup;

    public void EnsureInitialized()
    {
        if (lookup != null) return;
        
        lookup = new Dictionary<string, List<(ItemPrototype, ItemRole)>>();

        // 1. Process Auto Registration from allItems
        // A. Seeds mapping
        foreach (var itemProto in allItems)
        {
            if (itemProto == null || itemProto.item == null || itemProto.item.Uses == null)
                continue;

            foreach (var use in itemProto.item.Uses)
            {
                if (use is AddPlantUse addPlantUse && addPlantUse.PlantToAdd != null && addPlantUse.PlantToAdd.Crop != null)
                {
                    string cropID = addPlantUse.PlantToAdd.Crop.CropID;
                    AddRelationship(cropID, itemProto, ItemRole.Seed);
                }
            }
        }
        
        // B. Resources mapping (Watering, Fertilizer)
        var allPlants = allItems
            .Where(i => i != null && i.item != null && i.item.Uses != null)
            .SelectMany(i => i.item.Uses.OfType<AddPlantUse>())
            .Select(use => use.PlantToAdd)
            .Where(p => p != null && p.Crop != null && p.Stages != null)
            .ToList();

        foreach (var plant in allPlants)
        {
            string cropID = plant.Crop.CropID;
            var requiredResources = new HashSet<Resource>();
            
            foreach (var stage in plant.Stages)
            {
                if (stage.RequiredResources != null)
                {
                    foreach (var res in stage.RequiredResources.Keys)
                    {
                        requiredResources.Add(res);
                    }
                }
            }

            foreach (var res in requiredResources)
            {
                // Find all items that provide this resource
                var providers = allItems.Where(i => i != null && i.item != null && i.item.Uses != null && 
                                                    i.item.Uses.OfType<ProviceResourceUse>().Any(u => u.Resource == res));
                
                ItemRole role = (res == Resource.Water) ? ItemRole.Watering : ItemRole.Fertilizer;
                foreach (var provider in providers)
                {
                    AddRelationship(cropID, provider, role);
                }
            }
        }

        // 2. Process Manual Relationships (e.g. Harvest Tools)
        foreach (var rel in manualRelationships)
        {
            if (rel != null && rel.cropSO != null && rel.cropSO.crop != null && rel.itemPrototype != null)
            {
                AddRelationship(rel.cropSO.crop.CropID, rel.itemPrototype, rel.role);
            }
        }
    }

    private void AddRelationship(string cropID, ItemPrototype item, ItemRole role)
    {
        if (string.IsNullOrEmpty(cropID) || item == null) return;
        
        if (!lookup.ContainsKey(cropID))
            lookup[cropID] = new List<(ItemPrototype, ItemRole)>();

        // Avoid duplicates
        if (!lookup[cropID].Any(r => r.item == item && r.role == role))
        {
            lookup[cropID].Add((item, role));
        }
    }

    public List<ItemPrototype> GetItemsForCrop(Crop crop)
    {
        if (crop == null) return new List<ItemPrototype>();
        EnsureInitialized();
        
        if (lookup.TryGetValue(crop.CropID, out var list))
        {
            return list.Select(r => r.item).Distinct().ToList();
        }
        return new List<ItemPrototype>();
    }

    public List<ItemPrototype> GetItemsForCrop(Crop crop, ItemRole role)
    {
        if (crop == null) return new List<ItemPrototype>();
        EnsureInitialized();
        
        if (lookup.TryGetValue(crop.CropID, out var list))
        {
            return list.Where(r => r.role == role).Select(r => r.item).Distinct().ToList();
        }
        return new List<ItemPrototype>();
    }
    
    public List<ItemPrototype> GetSeedsForCrop(Crop crop) => GetItemsForCrop(crop, ItemRole.Seed);
    public List<ItemPrototype> GetHarvestToolsForCrop(Crop crop) => GetItemsForCrop(crop, ItemRole.HarvestTool);
}
