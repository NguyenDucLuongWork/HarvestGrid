using LgTyLib.Core;
using LgTyLib.Modules.DataPersistence;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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

    [SerializeField]
    private TextMeshProUGUI moneyText;

    [SerializeField]
    public RectTransform itemTemporaryHolder;

    [Header("Data")]
    [SerializeField]
    private Inventory inventory;

    [SerializeField]
    private Dictionary<string, int> itemUsesAccumilation = new();

    public Inventory Inventory => inventory;

    public Dictionary<string, int> ItemUsesAccumilation => itemUsesAccumilation;

    public Vector3 GetCenterPosition(
        Vector2Int pivotBottomLeft,
        Vector2Int widthAndHeight)
    {
        if (widthAndHeight.x <= 0 || widthAndHeight.y <= 0)
        {
            return new Vector3(
                pivotBottomLeft.x,
                pivotBottomLeft.y,
                0f
            );
        }

        return new Vector3(
            pivotBottomLeft.x + widthAndHeight.x * 0.5f,
            pivotBottomLeft.y + widthAndHeight.y * 0.5f,
            0f
        );
    }

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

        // Money changes
        inventory.OnMoneyChanged += HandleMoneyChanged;

        // Crop changes
        inventory.OnCropsChanged += HandleCropsChanged;

        // Item changes
        inventory.OnItemsChanged += HandleItemsChanged;

        // Build initial UI
        UpdateMoneyText();
        AddCropToCropBar();
        AddItemToItemBar();
    }

    protected override void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnMoneyChanged -= HandleMoneyChanged;
            inventory.OnCropsChanged -= HandleCropsChanged;
            inventory.OnItemsChanged -= HandleItemsChanged;
        }

        base.OnDestroy();
    }

    // ============================================================
    // MONEY
    // ============================================================

    private void HandleMoneyChanged(int money)
    {
        UpdateMoneyText(money);
    }

    private void UpdateMoneyText()
    {
        if (inventory == null)
            return;

        UpdateMoneyText(inventory.Money);
    }

    private void UpdateMoneyText(int money)
    {
        if (moneyText == null)
            return;

        moneyText.text = money.ToString();
    }

    public void SellOne(Crop crop)
    {
        if (crop == null || inventory == null)
            return;

        if (!inventory.Crops.TryGetValue(crop, out int amount) || amount <= 0)
            return;

        if (!inventory.RemoveCrop(crop, 1))
            return;

        int sellPrice = crop.GetSellPrice();
        inventory.AddMoney(sellPrice);
    }

    public void AddMoney(int amount)
    {
        if (inventory == null)
            return;

        inventory.AddMoney(amount);
    }

    public bool RemoveMoney(int amount)
    {
        if (inventory == null)
            return false;

        return inventory.RemoveMoney(amount);
    }

    // ============================================================
    // ITEM USES
    // ============================================================

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

    private void HandleCropsChanged(
        IReadOnlyDictionary<Crop, int> crops)
    {
        AddCropToCropBar();
    }

    public void AddCropToCropBar()
    {
        if (cropBar == null || cropUIComponent == null)
            return;

        var crops = inventory.Crops;

        // Group by CropID + Stars, summing amounts for matches.
        var grouped =
            new Dictionary<
                (string cropID, int stars),
                (Crop crop, int amount)
            >();

        foreach (var kvp in crops)
        {
            Crop crop = kvp.Key;
            int amount = kvp.Value;

            if (crop == null)
                continue;

            var key = (crop.CropID, crop.Stars);

            if (grouped.TryGetValue(key, out var existing))
            {
                grouped[key] =
                    (existing.crop, existing.amount + amount);
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
                (toRemove ??=
                    new List<(string, int)>()).Add(kvp.Key);
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

        foreach (var kvp in grouped.OrderBy(g => g.Key.cropID)
                                    .ThenBy(g => g.Key.stars))
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
        StoringSpaceMono.Instance.AutoUpdateDataAndRefreshUI();
    }

    public void AddItemToItemBar()
    {
        if (itemBar == null || itemUIComponent == null)
            return;

        var items = inventory.GetItemList();

        // Remove UI entries for items that no longer exist.
        List<Item> toRemove = null;

        foreach (var kvp in itemUIEntries)
        {
            if (!items.Contains(kvp.Key))
            {
                (toRemove ??=
                    new List<Item>()).Add(kvp.Key);
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

            if (!itemUIEntries.TryGetValue(
                    item,
                    out ItemUIComponent uiEntry))
            {
                uiEntry =
                    item.gameObject.GetComponent<ItemUIComponent>();

                item.gameObject.transform.parent =
                    itemBar.transform;

                itemUIEntries.Add(item, uiEntry);
            }

            uiEntry.SetItem(item);
            uiEntry.transform.SetSiblingIndex(index);

            index++;
        }
    }

    // ============================================================
    // SAVE / LOAD
    // ============================================================

    public void LoadGame(GameData gameData)
    {
        // 1. Clear existing items, crops, and UI before loading new data.
        ClearAllInventory();

        if (gameData == null || gameData.inventory == null)
            return;

        // =========================
        // MONEY
        // =========================

        if (gameData.inventory.Money > 0)
        {
            inventory.AddMoney(gameData.inventory.Money);
        }

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

        foreach (StoredObject savedObject
                 in gameData.inventory.Items)
        {
            if (savedObject == null || savedObject.Item == null)
                continue;

            StoredObject restoredObject =
                savedObject.Clone();

            // The ItemMono owns the inventory-bar UI object and
            // assigns it to Item.gameObject before the item is
            // added to the inventory.
            ItemFactory.Instance.SpawnItemWithoutClone(
                restoredObject.Item);

            inventory.AddItem(restoredObject);

            // This restores the storage-space visual only.
            ItemWithFootprintMono itemMono =
                ItemFactory.Instance
                    .SpawnItemMonoWithFootprint(restoredObject);
        }
    }

    /// <summary>
    /// Clears all items, crops, tracking dictionaries, and UI elements.
    /// </summary>
    private void ClearAllInventory()
    {
        // --- Clear Items ---
        foreach (var kvp in itemUIEntries)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);

            // If the Item GameObject is separate from the UI component,
            // destroy it as well.
            if (kvp.Key != null &&
                kvp.Key.gameObject != null &&
                (kvp.Value == null ||
                 kvp.Key.gameObject != kvp.Value.gameObject))
            {
                Destroy(kvp.Key.gameObject);
            }
        }

        itemUIEntries.Clear();

        // --- Clear Crops UI ---
        foreach (var kvp in cropUIEntries)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }

        cropUIEntries.Clear();

        // --- Clear Data ---
        itemUsesAccumilation.Clear();

        // Re-initialize inventory instance to ensure a clean state.
        inventory = new Inventory();

        // Re-subscribe events for the new inventory instance.
        inventory.OnMoneyChanged += HandleMoneyChanged;
        inventory.OnCropsChanged += HandleCropsChanged;
        inventory.OnItemsChanged += HandleItemsChanged;

        // Refresh money UI for the new inventory.
        UpdateMoneyText();
    }

    public void SaveGame(ref GameData gameData)
    {
        gameData.inventory = inventory;
    }
}