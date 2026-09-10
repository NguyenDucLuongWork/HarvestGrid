using LgTyLib.Core;
using LgTyLib.Modules.DataPersistence;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class InventoryMono : BaseSingleton<InventoryMono>, IDataPersistence
{
    [Header("Config")]
    [SerializeField]
    private CropUIComponent cropUIComponent;

    [SerializeField]
    private RectTransform cropBar;

    [SerializeField]
    private ItemUIComponent itemUIComponent;

    [SerializeField]
    private RectTransform itemBar;

    [Header("Data")]
    [SerializeField]
    private Inventory inventory;

    [SerializeField]
    private Dictionary<string, int> itemUsesAccumilation = new();

    public Inventory Inventory => inventory;

    public Dictionary<string, int> ItemUsesAccumilation => itemUsesAccumilation;

    // Tracks spawned crop UI entries.
    private readonly Dictionary<(string cropID, int stars), CropUIComponent> cropUIEntries = new();

    // Tracks spawned item UI entries.
    private readonly Dictionary<Item, ItemUIComponent> itemUIEntries = new();

    protected override void Awake()
    {
        base.Awake();

        inventory ??= new Inventory();
    }

    private void Start()
    {
        inventory ??= new Inventory();

        // Crop changes
        inventory.OnCropsChanged += HandleCropsChanged;

        // Item changes
        inventory.OnItemsChanged += HandleItemsChanged;

        // Build initial UI
        AddCropToCropBar();
        AddItemToItemBar();
    }

    protected override void OnDestroy()
    {
        if (inventory == null)
            return;

        inventory.OnCropsChanged -= HandleCropsChanged;
        inventory.OnItemsChanged -= HandleItemsChanged;
        base.OnDestroy();
    }

    public void AddItemUses(string itemID, int uses)
    {
        if (string.IsNullOrEmpty(itemID))
            return;

        if (itemUsesAccumilation.TryGetValue(itemID, out int currentUses))
        {
            itemUsesAccumilation[itemID] = currentUses + uses;
        }
        else
        {
            itemUsesAccumilation.Add(itemID, uses);
        }
    }

    public int GetAndSubtractItemUses(string itemID, int requirement)
    {
        if (string.IsNullOrEmpty(itemID) || requirement <= 0)
            return 0;

        if (!itemUsesAccumilation.TryGetValue(itemID, out int currentUses))
            return 0;

        int used = Mathf.Min(currentUses, requirement);

        itemUsesAccumilation[itemID] = currentUses - used;

        return used;
    }

    // ============================================================
    // CROP BAR
    // ============================================================

    private void HandleCropsChanged(IReadOnlyDictionary<Crop, int> crops)
    {
        AddCropToCropBar();
    }

    public void AddCropToCropBar()
    {
        if (cropBar == null || cropUIComponent == null)
            return;

        var crops = inventory.Crops;

        // Group by CropID + Stars, summing amounts for matches.
        var grouped = new Dictionary<(string cropID, int stars), (Crop crop, int amount)>();

        foreach (var kvp in crops)
        {
            Crop crop = kvp.Key;
            int amount = kvp.Value;

            if (crop == null)
                continue;

            var key = (crop.CropID, crop.Stars);

            if (grouped.TryGetValue(key, out var existing))
            {
                grouped[key] = (existing.crop, existing.amount + amount);
            }
            else
            {
                grouped[key] = (crop, amount);
            }
        }

        // Remove UI entries for groups that no longer exist.
        List<(string cropID, int stars)> toRemove = null;

        foreach (var kvp in cropUIEntries)
        {
            if (!grouped.ContainsKey(kvp.Key))
            {
                (toRemove ??= new List<(string, int)>()).Add(kvp.Key);
            }
        }

        if (toRemove != null)
        {
            foreach (var key in toRemove)
            {
                Destroy(cropUIEntries[key].gameObject);
                cropUIEntries.Remove(key);
            }
        }

        // Add/update entries.
        int index = 0;

        foreach (var kvp in grouped)
        {
            var key = kvp.Key;
            Crop crop = kvp.Value.crop;
            int amount = kvp.Value.amount;

            if (!cropUIEntries.TryGetValue(key, out CropUIComponent uiEntry))
            {
                uiEntry = Instantiate(cropUIComponent, cropBar);
                cropUIEntries.Add(key, uiEntry);
            }

            uiEntry.SetData(crop, amount);
            uiEntry.transform.SetSiblingIndex(index);

            index++;
        }
    }

    // ============================================================
    // ITEM BAR
    // ============================================================

    private void HandleItemsChanged(IReadOnlyList<Item> items)
    {
        AddItemToItemBar();
    }

    public void AddItemToItemBar()
    {
        if (itemBar == null || itemUIComponent == null)
            return;

        var items = inventory.Items;

        // Remove UI entries for items that no longer exist.
        List<Item> toRemove = null;

        foreach (var kvp in itemUIEntries)
        {
            if (!items.Contains(kvp.Key))
            {
                (toRemove ??= new List<Item>()).Add(kvp.Key);
            }
        }

        if (toRemove != null)
        {
            foreach (var item in toRemove)
            {
                Destroy(itemUIEntries[item].gameObject);
                itemUIEntries.Remove(item);
            }
        }

        // Add/update entries.
        int index = 0;

        foreach (Item item in items)
        {
            if (item == null)
                continue;

            if (!itemUIEntries.TryGetValue(item, out ItemUIComponent uiEntry))
            {
                uiEntry = item.gameObject.GetComponent<ItemUIComponent>();
                item.gameObject.transform.parent = itemBar.transform; 
                itemUIEntries.Add(item, uiEntry);
            }

            uiEntry.SetItem(item);
            uiEntry.transform.SetSiblingIndex(index);

            index++;
        }
    }

    // ============================================================
    // Save Load
    // ============================================================
    public void LoadGame(GameData gameData)
    {
        // =========================
        // CROPS
        // =========================
        foreach (var pair in gameData.inventory.Crops)
        {
            Crop crop = pair.Key;
            int amount = pair.Value;

            if (crop == null || amount <= 0)
                continue;

            inventory.AddCrop(crop, amount);
        }

        // =========================
        // ITEMS
        // =========================
        IReadOnlyList<Item> itemsToAdd = gameData.inventory.Items;

        for (int i = 0; i < itemsToAdd.Count; i++)
        {
            if (itemsToAdd[i] == null)
                continue;

            ItemFactory.Instance.SpawnItem(itemsToAdd[i]);
        }
    }

    public void SaveGame(ref GameData gameData)
    {
        gameData.inventory = this.inventory;
    }
}