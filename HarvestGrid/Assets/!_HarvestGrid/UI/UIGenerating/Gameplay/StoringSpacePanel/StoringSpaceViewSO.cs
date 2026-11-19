using System;
using System.Collections.Generic;
using UIGen;
using UnityEngine;

[Serializable]
public class StoringCellData
{
    public int x;
    public int y;
    public bool isLocked; // Unavailable = true
    public bool isOccupied;
}

[Serializable]
public class StoringItemData
{
    public string itemId = "scythe";
    public string itemName = "Super Scythe";
    public string icon = "🌾";
    public Sprite iconSprite;
    public int x = 0;
    public int y = 0;
    public int width = 2;
    public int height = 2;
    public int rotation = 0;
    public float progress = 0f;
    public bool[] footprint; // flattened width * height
}

[Serializable]
public class StoringSpaceView
{
    [UIField(ControlType.Label, Label = "Board Width (Columns)")]
    public int columns = 7;

    [UIField(ControlType.Label, Label = "Board Length (Rows)")]
    public int rows = 5;

    // Aliases for width and length
    public int width { get => columns; set => columns = value; }
    public int length { get => rows; set => rows = value; }
    public int height { get => rows; set => rows = value; }

    public List<StoringCellData> cells = new();
    public List<StoringItemData> placedItems = new();
}

[CreateAssetMenu(fileName = "StoringSpaceViewSO", menuName = "Scriptable Objects/View/StoringSpaceViewSO")]
public class StoringSpaceViewSO : ScriptableObject
{
    [SerializeField]
    public StoringSpaceView storingSpaceView = new StoringSpaceView();

    public event Action OnDataChanged;

    public void NotifyChanged()
    {
        OnDataChanged?.Invoke();
    }

    private void Reset()
    {
        InitializeDefaultStorage();
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        EnsureDefaultSprites();
#endif
        EnsureCellsGrid();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureDefaultSprites();
        EnsureCellsGrid();
        NotifyChanged();
    }

    public void EnsureDefaultSprites()
    {
        if (storingSpaceView == null || storingSpaceView.placedItems == null) return;
        foreach (var item in storingSpaceView.placedItems)
        {
            if (item.iconSprite == null)
            {
                string id = item.itemId != null ? item.itemId.ToLower() : "";
                if (id.Contains("scythe"))
                {
                    var p = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemPrototype>("Assets/!_HarvestGrid/ScriptableObjects/Items/Scythe.asset");
                    if (p != null && p.item != null) item.iconSprite = p.item.Icon;
                }
                else if (id.Contains("seed") || id.Contains("cucumb"))
                {
                    var p = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemPrototype>("Assets/!_HarvestGrid/ScriptableObjects/Items/Seed_Cucumber.asset");
                    if (p != null && p.item != null) item.iconSprite = p.item.Icon;
                }
                else
                {
                    var p = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemPrototype>("Assets/!_HarvestGrid/ScriptableObjects/Items/WaterCan.asset");
                    if (p != null && p.item != null) item.iconSprite = p.item.Icon;
                }
            }
        }
    }
#endif

    public void EnsureCellsGrid()
    {
        if (storingSpaceView == null)
            storingSpaceView = new StoringSpaceView();

        storingSpaceView.columns = Mathf.Max(1, storingSpaceView.columns);
        storingSpaceView.rows = Mathf.Max(1, storingSpaceView.rows);
        storingSpaceView.cells ??= new List<StoringCellData>();

        // Preserve existing lock states by coordinate
        Dictionary<(int x, int y), bool> existingLocks = new();
        foreach (var c in storingSpaceView.cells)
        {
            if (c != null) existingLocks[(c.x, c.y)] = c.isLocked;
        }

        bool needsRebuild = storingSpaceView.cells.Count != storingSpaceView.columns * storingSpaceView.rows;
        if (!needsRebuild)
        {
            for (int y = 0; y < storingSpaceView.rows; y++)
            {
                for (int x = 0; x < storingSpaceView.columns; x++)
                {
                    if (!storingSpaceView.cells.Exists(c => c != null && c.x == x && c.y == y))
                    {
                        needsRebuild = true;
                        break;
                    }
                }
                if (needsRebuild) break;
            }
        }

        if (needsRebuild)
        {
            storingSpaceView.cells.Clear();
            for (int y = 0; y < storingSpaceView.rows; y++)
            {
                for (int x = 0; x < storingSpaceView.columns; x++)
                {
                    bool isLocked = existingLocks.TryGetValue((x, y), out bool locked) && locked;
                    storingSpaceView.cells.Add(new StoringCellData
                    {
                        x = x,
                        y = y,
                        isLocked = isLocked,
                        isOccupied = false
                    });
                }
            }
        }

        UpdateOccupiedStates();
    }

    public void UpdateOccupiedStates()
    {
        if (storingSpaceView == null || storingSpaceView.cells == null) return;

        foreach (var c in storingSpaceView.cells)
        {
            c.isOccupied = false;
        }

        if (storingSpaceView.placedItems != null)
        {
            foreach (var item in storingSpaceView.placedItems)
            {
                for (int r = 0; r < item.height; r++)
                {
                    for (int c = 0; c < item.width; c++)
                    {
                        if (IsFootprintCellOccupied(item, c, r))
                        {
                            int gx = item.x + c;
                            int gy = item.y + (item.height - 1 - r);
                            var cell = storingSpaceView.cells.Find(d => d.x == gx && d.y == gy);
                            if (cell != null)
                            {
                                cell.isOccupied = true;
                            }
                        }
                    }
                }
            }
        }
    }

    public bool IsFootprintCellOccupied(StoringItemData item, int col, int row)
    {
        if (item.footprint == null || item.footprint.Length == 0)
            return true;

        if (col < 0 || col >= item.width || row < 0 || row >= item.height)
            return false;

        int idx = row * item.width + col;
        return idx >= 0 && idx < item.footprint.Length && item.footprint[idx];
    }

    public void InitializeDefaultStorage()
    {
        storingSpaceView.columns = 7;
        storingSpaceView.rows = 5;
        storingSpaceView.cells = new List<StoringCellData>();

        // Locked default positions: (6,0), (6,1), (0,4)
        HashSet<string> lockedSet = new() { "6,0", "6,1", "0,4" };

        for (int y = 0; y < storingSpaceView.rows; y++)
        {
            for (int x = 0; x < storingSpaceView.columns; x++)
            {
                storingSpaceView.cells.Add(new StoringCellData
                {
                    x = x,
                    y = y,
                    isLocked = lockedSet.Contains($"{x},{y}"),
                    isOccupied = false
                });
            }
        }

        storingSpaceView.placedItems = new List<StoringItemData>
        {
            new StoringItemData
            {
                itemId = "scythe",
                itemName = "Super Scythe",
                icon = "🌾",
                x = 0,
                y = 0,
                width = 2,
                height = 2,
                progress = 0.35f,
                footprint = new bool[] { true, true, true, false }
            },
            new StoringItemData
            {
                itemId = "seeds",
                itemName = "Cucumber Seeds",
                icon = "🥒",
                x = 2,
                y = 0,
                width = 1,
                height = 2,
                progress = 0.7f,
                footprint = new bool[] { true, true }
            },
            new StoringItemData
            {
                itemId = "gloves",
                itemName = "Lucky Gloves",
                icon = "🧤",
                x = 3,
                y = 0,
                width = 1,
                height = 1,
                progress = 0.2f,
                footprint = new bool[] { true }
            }
        };

#if UNITY_EDITOR
        EnsureDefaultSprites();
#endif
        UpdateOccupiedStates();
    }

    #region Context Menu Functions

    [ContextMenu("Rebuild / Resize Grid")]
    public void RebuildGrid()
    {
        EnsureCellsGrid();
        UpdateOccupiedStates();
        NotifyChanged();
        Debug.Log($"[StoringSpaceViewSO] Rebuilt grid: {storingSpaceView.columns}x{storingSpaceView.rows} ({storingSpaceView.cells.Count} cells).");
    }

    [ContextMenu("Sync From StoringSpaceMono (Game)")]
    public void SyncFromStoringSpaceMono()
    {
        if (StoringSpaceMono.HasInstance && StoringSpaceMono.Instance.StoringSpace != null)
        {
            var space = StoringSpaceMono.Instance.StoringSpace;
            int w = space.Cells.GetLength(0);
            int h = space.Cells.GetLength(1);

            storingSpaceView.columns = w;
            storingSpaceView.rows = h;
            storingSpaceView.cells.Clear();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var cellType = space.Cells[x, y];
                    storingSpaceView.cells.Add(new StoringCellData
                    {
                        x = x,
                        y = y,
                        isLocked = cellType == StoringCellType.Unavailable,
                        isOccupied = cellType == StoringCellType.Occupied
                    });
                }
            }

            if (InventoryMono.HasInstance && InventoryMono.Instance.Inventory != null)
            {
                var invItems = InventoryMono.Instance.Inventory.Items;
                if (invItems != null && invItems.Count > 0)
                {
                    storingSpaceView.placedItems.Clear();
                    foreach (var stored in invItems)
                    {
                        if (stored == null || stored.Item == null) continue;
                        var it = stored.Item;
                        var itemData = new StoringItemData
                        {
                            itemId = it.Id,
                            itemName = it.Name,
                            icon = "",
                            iconSprite = it.Icon,
                            x = stored.Pivot.x,
                            y = stored.Pivot.y,
                            rotation = stored.Rotated,
                            progress = it.ProgressTimer != null ? it.ProgressTimer.Progress : 0f
                        };

                        if (it.Footprint != null && it.Footprint.Requiring != null)
                        {
                            int fw = it.Footprint.Requiring.GetLength(0);
                            int fh = it.Footprint.Requiring.GetLength(1);
                            itemData.width = fw;
                            itemData.height = fh;
                            itemData.footprint = new bool[fw * fh];

                            for (int fx = 0; fx < fw; fx++)
                            {
                                for (int fy = 0; fy < fh; fy++)
                                {
                                    int uiRow = fh - 1 - fy;
                                    itemData.footprint[uiRow * fw + fx] = it.Footprint.Requiring[fx, fy];
                                }
                            }
                        }

                        storingSpaceView.placedItems.Add(itemData);
                    }
                }
            }

            NotifyChanged();
            Debug.Log($"[StoringSpaceViewSO] Synced from StoringSpaceMono: {w}x{h}, {storingSpaceView.cells.Count} cells, {storingSpaceView.placedItems.Count} items.");
        }
        else
        {
            Debug.LogWarning("[StoringSpaceViewSO] StoringSpaceMono is not active in the scene.");
        }
    }

    [ContextMenu("Push To StoringSpaceMono (Game)")]
    public void PushToStoringSpaceMono()
    {
        if (StoringSpaceMono.HasInstance && StoringSpaceMono.Instance.StoringSpace != null)
        {
            var space = StoringSpaceMono.Instance.StoringSpace;
            int w = space.Cells.GetLength(0);
            int h = space.Cells.GetLength(1);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var cellData = storingSpaceView.cells.Find(c => c.x == x && c.y == y);
                    bool locked = cellData != null && cellData.isLocked;
                    space.Cells[x, y] = locked ? StoringCellType.Unavailable : StoringCellType.Available;
                }
            }

            UpdateOccupiedStates();

            foreach (var cell in storingSpaceView.cells)
            {
                if (cell.x >= 0 && cell.x < w && cell.y >= 0 && cell.y < h)
                {
                    if (cell.isOccupied && space.Cells[cell.x, cell.y] != StoringCellType.Unavailable)
                    {
                        space.Cells[cell.x, cell.y] = StoringCellType.Occupied;
                    }
                }
            }

            StoringSpaceMono.Instance.RefreshVisuals();
            Debug.Log("[StoringSpaceViewSO] Pushed data to StoringSpaceMono.");
        }
        else
        {
            Debug.LogWarning("[StoringSpaceViewSO] StoringSpaceMono is not active in the scene.");
        }
    }

    [ContextMenu("Lock Default 3 Corners")]
    public void LockDefaultCorners()
    {
        EnsureCellsGrid();
        HashSet<string> defaultCorners = new()
        {
            $"{storingSpaceView.columns - 1},0",
            $"{storingSpaceView.columns - 1},1",
            $"0,{storingSpaceView.rows - 1}"
        };

        foreach (var cell in storingSpaceView.cells)
        {
            if (defaultCorners.Contains($"{cell.x},{cell.y}"))
            {
                cell.isLocked = true;
            }
        }
        UpdateOccupiedStates();
        NotifyChanged();
    }

    [ContextMenu("Unlock All Cells")]
    public void UnlockAllCells()
    {
        EnsureCellsGrid();
        foreach (var cell in storingSpaceView.cells)
        {
            cell.isLocked = false;
        }
        UpdateOccupiedStates();
        NotifyChanged();
    }

    [ContextMenu("Recalculate Occupied States")]
    public void RecalculateOccupied()
    {
        UpdateOccupiedStates();
        NotifyChanged();
    }

    #endregion
}
