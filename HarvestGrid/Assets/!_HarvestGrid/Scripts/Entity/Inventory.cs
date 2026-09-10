using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Inventory : ICloneable<Inventory>
{
    [SerializeField]
    private Dictionary<Crop, int> crops = new();

    [SerializeField]
    private List<Item> items = new();

    public IReadOnlyDictionary<Crop, int> Crops => crops;

    public IReadOnlyList<Item> Items => items;

    // Fired whenever the crop dictionary changes.
    public event Action<IReadOnlyDictionary<Crop, int>> OnCropsChanged;

    // Fired whenever the item list changes.
    public event Action<IReadOnlyList<Item>> OnItemsChanged;

    public Inventory()
    {
    }

    public Inventory(Inventory original)
    {
        if (original == null)
            throw new ArgumentNullException(nameof(original));

        // Deep copy crops
        crops = new Dictionary<Crop, int>();

        foreach (var pair in original.crops)
        {
            if (pair.Key == null)
                continue;

            Crop clonedCrop = pair.Key.Clone();
            crops.Add(clonedCrop, pair.Value);
        }

        // Deep copy items
        items = new List<Item>();

        foreach (var item in original.items)
        {
            if (item == null)
                continue;

            items.Add(item.Clone());
        }
    }

    // ============================================================
    // CROPS
    // ============================================================

    public void AddCrop(Crop crop, int amount)
    {
        if (crop == null)
            throw new ArgumentNullException(nameof(crop));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Amount must be greater than 0.");

        if (crops.ContainsKey(crop))
        {
            crops[crop] += amount;
        }
        else
        {
            crops.Add(crop, amount);
        }

        OnCropsChanged?.Invoke(crops);
    }

    public bool RemoveCrop(Crop crop, int amount)
    {
        if (crop == null || amount <= 0)
            return false;

        if (!crops.TryGetValue(crop, out int currentAmount))
            return false;

        if (amount >= currentAmount)
        {
            crops.Remove(crop);
        }
        else
        {
            crops[crop] = currentAmount - amount;
        }

        OnCropsChanged?.Invoke(crops);

        return true;
    }

    // ============================================================
    // ITEMS
    // ============================================================

    public void AddItem(Item item)
    {
        Debug.LogWarning("Adding item: " + item.Id);
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        items.Add(item);

        OnItemsChanged?.Invoke(items);
    }

    public bool RemoveItem(Item item)
    {
        if (item == null)
            return false;

        bool removed = items.Remove(item);

        if (removed)
        {
            OnItemsChanged?.Invoke(items);
        }

        return removed;
    }

    public bool ContainsItem(Item item)
    {
        if (item == null)
            return false;

        return items.Contains(item);
    }

    public Inventory Clone()
    {
        return new Inventory(this);
    }
}