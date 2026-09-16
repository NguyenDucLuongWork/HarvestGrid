using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Inventory : ICloneable<Inventory>
{
    [SerializeField]
    private Dictionary<Crop, int> crops = new();

    [SerializeField]
    private List<StoredObject> storedItems = new();

    public IReadOnlyDictionary<Crop, int> Crops => crops;

    public IReadOnlyList<StoredObject> Items => storedItems;

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

        // Deep copy stored objects
        storedItems = new List<StoredObject>();

        foreach (var storedObject in original.storedItems)
        {
            if (storedObject == null)
                continue;

            storedItems.Add(storedObject.Clone());
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

    public void AddItem(StoredObject storedObject)
    {
        if (storedObject == null)
            throw new ArgumentNullException(nameof(storedObject));

        Debug.LogWarning("Adding item: " + storedObject.Item?.Id);

        storedItems.Add(storedObject);

        OnItemsChanged?.Invoke(GetItemList());
    }

    public bool RemoveItem(StoredObject storedObject)
    {
        if (storedObject == null)
            return false;

        bool removed = storedItems.Remove(storedObject);

        if (removed)
        {
            OnItemsChanged?.Invoke(GetItemList());
        }

        return removed;
    }

    public bool ContainsItem(StoredObject storedObject)
    {
        if (storedObject == null)
            return false;

        return storedItems.Contains(storedObject);
    }

    public Inventory Clone()
    {
        return new Inventory(this);
    }

    public IReadOnlyList<Item> GetItemList()
    {
        List<Item> rs = new List<Item>();
        foreach (StoredObject storedObject in storedItems) {
            rs.Add(storedObject.Item);
        }
        return rs.AsReadOnly();
    }
}