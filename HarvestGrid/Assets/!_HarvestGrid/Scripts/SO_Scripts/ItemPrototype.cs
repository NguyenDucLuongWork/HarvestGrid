using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItemPrototype", menuName = "Scriptable Objects/ItemPrototype")]
public class ItemPrototype : ScriptableObject
{
    [Header("Init Config")]
    public List<ItemUseConfigEntry> useConfig = new();

    // Footprint editor grid (replaces ItemFootprintSO).
    // Same orientation as the old tool: GetCell(x, y) == grid[y * width + x],
    // and saving maps requiring[x, y] = GetCell(x, height - 1 - y).
    [Header("Footprint Editor Grid")]
    [Min(1)] public int footprintWidth = 1;
    [Min(1)] public int footprintHeight = 1;
    [SerializeField] private bool[] footprintGrid = new bool[1];

    [Header("Item")]
    public Item item;

    // ------------------------------------------------------------ null safety
    public void EnsureInitialized()
    {
        useConfig ??= new List<ItemUseConfigEntry>();
        footprintWidth = Mathf.Max(1, footprintWidth);
        footprintHeight = Mathf.Max(1, footprintHeight);

        int size = footprintWidth * footprintHeight;
        if (footprintGrid == null || footprintGrid.Length != size)
            footprintGrid = new bool[size];
    }

    private void OnEnable() => EnsureInitialized();

    // ------------------------------------------------------------ item uses
    [ContextMenu("Init Item")]
    public void InitItem()
    {
        EnsureInitialized();
        if (item == null)
        {
            Debug.LogWarning($"{name}: item data is null, can't init uses.", this);
            return;
        }

        var newUses = new List<ItemUse>();

        foreach (var entry in useConfig)
        {
            if (entry == null) continue;

            var use = ItemUseFactory.Create(entry.type);
            if (use == null)
                continue;

            if (use is AddPlantUse addPlantUse && entry.plantPrototype != null)
                addPlantUse.SetPlant(entry.plantPrototype.plant);

            newUses.Add(use);
        }

        item.SetItemUse(newUses);
    }

    // ------------------------------------------------------------ footprint grid
    public bool GetCell(int x, int y)
    {
        EnsureInitialized();
        if (x < 0 || y < 0 || x >= footprintWidth || y >= footprintHeight) return false;
        return footprintGrid[y * footprintWidth + x];
    }

    public void SetCell(int x, int y, bool value)
    {
        EnsureInitialized();
        if (x < 0 || y < 0 || x >= footprintWidth || y >= footprintHeight) return;
        footprintGrid[y * footprintWidth + x] = value;
    }

    /// <summary>Resizes the grid and keeps the cells that still fit.</summary>
    public void ResizeFootprintGrid(int newWidth, int newHeight)
    {
        EnsureInitialized();
        newWidth = Mathf.Max(1, newWidth);
        newHeight = Mathf.Max(1, newHeight);

        var newGrid = new bool[newWidth * newHeight];
        int copyW = Mathf.Min(newWidth, footprintWidth);
        int copyH = Mathf.Min(newHeight, footprintHeight);

        for (int y = 0; y < copyH; y++)
            for (int x = 0; x < copyW; x++)
                newGrid[y * newWidth + x] = footprintGrid[y * footprintWidth + x];

        footprintWidth = newWidth;
        footprintHeight = newHeight;
        footprintGrid = newGrid;
    }

    public void FillFootprintGrid(bool value)
    {
        EnsureInitialized();
        for (int i = 0; i < footprintGrid.Length; i++) footprintGrid[i] = value;
    }

    public bool IsFootprintGridEmpty()
    {
        EnsureInitialized();
        foreach (var c in footprintGrid) if (c) return false;
        return true;
    }

    /// <summary>Grid -> item.Footprint  (was ItemFootprintSO.SaveToFootprint + SetFootprint).</summary>
    [ContextMenu("Apply Footprint")]
    public void ApplyFootprint()
    {
        EnsureInitialized();
        if (item == null)
        {
            Debug.LogWarning($"{name}: item data is null, can't apply footprint.", this);
            return;
        }

        bool[,] requiring = new bool[footprintWidth, footprintHeight];
        for (int x = 0; x < footprintWidth; x++)
            for (int y = 0; y < footprintHeight; y++)
                requiring[x, y] = GetCell(x, footprintHeight - 1 - y);

        item.SetFootprint(new Footprint(requiring));
    }

    /// <summary>item.Footprint -> grid  (was ItemFootprintSO.LoadFromFootprint). Returns false if there is nothing to load.</summary>
    public bool LoadFootprintFromItem()
    {
        EnsureInitialized();
        var requiring = item?.Footprint?.Requiring;
        if (requiring == null) return false;

        int w = requiring.GetLength(0);
        int h = requiring.GetLength(1);
        if (w <= 0 || h <= 0) return false;

        footprintWidth = w;
        footprintHeight = h;
        footprintGrid = new bool[w * h];

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                footprintGrid[y * w + x] = requiring[x, h - 1 - y];

        return true;
    }

    /// <summary>True when the grid and item.Footprint describe the same shape.</summary>
    public bool FootprintMatchesItem()
    {
        EnsureInitialized();
        var requiring = item?.Footprint?.Requiring;
        if (requiring == null) return IsFootprintGridEmpty();

        if (requiring.GetLength(0) != footprintWidth || requiring.GetLength(1) != footprintHeight)
            return false;

        for (int x = 0; x < footprintWidth; x++)
            for (int y = 0; y < footprintHeight; y++)
                if (requiring[x, y] != GetCell(x, footprintHeight - 1 - y))
                    return false;

        return true;
    }
}