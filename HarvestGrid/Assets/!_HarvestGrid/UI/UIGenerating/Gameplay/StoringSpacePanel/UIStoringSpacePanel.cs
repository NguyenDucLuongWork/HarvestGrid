using System.Collections.Generic;
using System.Linq;
using UIGen;
using UnityEngine;
using UnityEngine.UIElements;

public class UIStoringSpacePanel : BindedPanel
{
    [SerializeField]
    public StoringSpaceViewSO so;

    [Tooltip("Sub-UXML template for each placed item (UIItemFootprint.uxml)")]
    [SerializeField]
    public VisualTreeAsset itemFootprintTemplate;

    [SerializeField]
    public float cellSize = 126f;

    [SerializeField]
    public float cellGap = 10f;

    private readonly Dictionary<(int x, int y), VisualElement> cellElements = new();
    private readonly Dictionary<StoringItemData, VisualElement> itemElements = new();

    private int lastColumns = -1;
    private int lastRows = -1;

    protected override object DataSource => so != null ? so.storingSpaceView : null;

    protected void OnEnable()
    {
        if (so != null)
        {
            so.OnDataChanged -= HandleSODataChanged;
            so.OnDataChanged += HandleSODataChanged;
        }
    }

    protected override void OnDestroy()
    {
        if (so != null)
        {
            so.OnDataChanged -= HandleSODataChanged;
        }
        base.OnDestroy();
    }

    private void HandleSODataChanged()
    {
        RenderBoard();
    }

    protected override void OnBound()
    {
        base.OnBound();
        if (so != null)
        {
            so.OnDataChanged -= HandleSODataChanged;
            so.OnDataChanged += HandleSODataChanged;
        }
        SyncWithStoringSpaceMono();
        RenderBoard();
    }

    private void Update()
    {
        if (so == null || so.storingSpaceView == null) return;

        // 1. Dynamic width/length (columns/rows) change detection & auto re-render
        int curCols = so.storingSpaceView.columns;
        int curRows = so.storingSpaceView.rows;
        if (curCols != lastColumns || curRows != lastRows)
        {
            lastColumns = curCols;
            lastRows = curRows;
            so.EnsureCellsGrid();
            RenderBoard();
            return;
        }

        // 2. Live synchronization with StoringSpaceMono & InventoryMono at runtime
        if (StoringSpaceMono.HasInstance && StoringSpaceMono.Instance.StoringSpace != null)
        {
            var space = StoringSpaceMono.Instance.StoringSpace;
            int spaceW = space.Cells.GetLength(0);
            int spaceH = space.Cells.GetLength(1);

            if (spaceW != curCols || spaceH != curRows)
            {
                SyncWithStoringSpaceMono();
                RenderBoard();
                return;
            }

            // Sync cell states (lock / occupied / available) from game space
            for (int y = 0; y < spaceH; y++)
            {
                for (int x = 0; x < spaceW; x++)
                {
                    var cellData = so.storingSpaceView.cells.Find(c => c.x == x && c.y == y);
                    if (cellData != null)
                    {
                        var cellType = space.Cells[x, y];
                        cellData.isLocked = cellType == StoringCellType.Unavailable;
                        cellData.isOccupied = cellType == StoringCellType.Occupied;
                    }
                }
            }
        }

        // 3. Live item progress & icon sync from InventoryMono
        if (InventoryMono.HasInstance && InventoryMono.Instance.Inventory != null)
        {
            var invItems = InventoryMono.Instance.Inventory.Items;
            if (invItems != null && so.storingSpaceView.placedItems != null)
            {
                foreach (var itemData in so.storingSpaceView.placedItems)
                {
                    var match = invItems.FirstOrDefault(s =>
                        s != null && s.Item != null &&
                        s.Item.Id == itemData.itemId &&
                        s.Pivot.x == itemData.x &&
                        s.Pivot.y == itemData.y);

                    if (match != null && match.Item != null)
                    {
                        if (itemData.iconSprite == null && match.Item.Icon != null)
                        {
                            itemData.iconSprite = match.Item.Icon;
                        }

                        if (match.Item.ProgressTimer != null)
                        {
                            itemData.progress = match.Item.ProgressTimer.Progress;
                        }
                    }
                }
            }
        }

        // 4. Update cell visuals (locked vs occupied vs empty)
        UpdateCellVisuals();

        // 5. Smoothly update progress visual (cooldown veil) on placed items
        if (itemElements.Count > 0)
        {
            foreach (var kvp in itemElements)
            {
                var item = kvp.Key;
                var el = kvp.Value;
                if (item != null && el != null)
                {
                    UIItemFootprintHelper.UpdateProgress(el, item.progress);
                }
            }
        }
    }

    public void SyncWithStoringSpaceMono()
    {
        if (StoringSpaceMono.HasInstance && StoringSpaceMono.Instance.StoringSpace != null && so != null)
        {
            var space = StoringSpaceMono.Instance.StoringSpace;
            int w = space.Cells.GetLength(0);
            int h = space.Cells.GetLength(1);

            so.storingSpaceView.columns = w;
            so.storingSpaceView.rows = h;
            so.storingSpaceView.cells.Clear();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var cellType = space.Cells[x, y];
                    so.storingSpaceView.cells.Add(new StoringCellData
                    {
                        x = x,
                        y = y,
                        isLocked = cellType == StoringCellType.Unavailable,
                        isOccupied = cellType == StoringCellType.Occupied
                    });
                }
            }

            // Sync placed items from InventoryMono if available
            if (InventoryMono.HasInstance && InventoryMono.Instance.Inventory != null)
            {
                var invItems = InventoryMono.Instance.Inventory.Items;
                if (invItems != null && invItems.Count > 0)
                {
                    so.storingSpaceView.placedItems.Clear();
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

                        so.storingSpaceView.placedItems.Add(itemData);
                    }
                }
            }

            so.UpdateOccupiedStates();
        }
    }

    public void SyncStoringSpaceMonoFromSO()
    {
        if (StoringSpaceMono.HasInstance && StoringSpaceMono.Instance.StoringSpace != null && so != null)
        {
            var space = StoringSpaceMono.Instance.StoringSpace;
            int w = space.Cells.GetLength(0);
            int h = space.Cells.GetLength(1);

            // Reset cells to available/unavailable based on lock state
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var cellData = so.storingSpaceView.cells.Find(c => c.x == x && c.y == y);
                    bool locked = cellData != null && cellData.isLocked;
                    space.Cells[x, y] = locked ? StoringCellType.Unavailable : StoringCellType.Available;
                }
            }

            // Mark occupied cells from placed items
            foreach (var it in so.storingSpaceView.placedItems)
            {
                for (int r = 0; r < it.height; r++)
                {
                    for (int c = 0; c < it.width; c++)
                    {
                        if (IsFootprintCellOccupied(it, c, r))
                        {
                            int gx = it.x + c;
                            int gy = it.y + (it.height - 1 - r);
                            if (gx >= 0 && gx < w && gy >= 0 && gy < h)
                            {
                                space.Cells[gx, gy] = StoringCellType.Occupied;
                            }
                        }
                    }
                }
            }

            // Sync pivot and rotation back to InventoryMono's StoredObjects
            if (InventoryMono.HasInstance && InventoryMono.Instance.Inventory != null)
            {
                var invItems = InventoryMono.Instance.Inventory.Items;
                if (invItems != null)
                {
                    foreach (var itemData in so.storingSpaceView.placedItems)
                    {
                        foreach (var s in invItems)
                        {
                            if (s != null && s.Item != null && s.Item.Id == itemData.itemId)
                            {
                                s.Pivot = new Vector2Int(itemData.x, itemData.y);
                                s.Rotated = itemData.rotation;
                                break;
                            }
                        }
                    }
                }
            }

            StoringSpaceMono.Instance.RefreshVisuals();
        }
    }

    public void RenderBoard()
    {
        if (root == null || so == null || so.storingSpaceView == null) return;

        lastColumns = so.storingSpaceView.columns;
        lastRows = so.storingSpaceView.rows;

        so.EnsureCellsGrid();
        EnsureBoardContentWrapper();
        RenderCellsGrid();
        RenderPlacedItems();
    }

    private void EnsureBoardContentWrapper()
    {
        var board = root.Q<VisualElement>("board");
        var grid = root.Q<VisualElement>("cells-grid");
        var layer = root.Q<VisualElement>("items-layer");
        var boardContent = root.Q<VisualElement>("board-content");

        if (boardContent == null && board != null && grid != null && layer != null)
        {
            boardContent = new VisualElement { name = "board-content" };
            boardContent.AddToClassList("board-content");
            board.Insert(board.IndexOf(grid), boardContent);
            boardContent.Add(grid);
            boardContent.Add(layer);
        }

        if (boardContent != null)
        {
            int cols = Mathf.Max(1, so.storingSpaceView.columns);
            int rows = Mathf.Max(1, so.storingSpaceView.rows);
            float totalWidth = cols * cellSize + (cols - 1) * cellGap;
            float totalHeight = rows * cellSize + (rows - 1) * cellGap;

            boardContent.style.width = totalWidth;
            boardContent.style.height = totalHeight;

            if (grid != null)
            {
                grid.style.width = totalWidth;
                grid.style.height = totalHeight;
            }

            if (layer != null)
            {
                layer.style.position = Position.Absolute;
                layer.style.left = 0;
                layer.style.top = 0;
                layer.style.width = totalWidth;
                layer.style.height = totalHeight;
            }
        }
    }

    private void RenderCellsGrid()
    {
        var grid = root.Q<VisualElement>("cells-grid");
        if (grid == null) return;

        grid.Clear();
        cellElements.Clear();

        int cols = Mathf.Max(1, so.storingSpaceView.columns);
        int rows = Mathf.Max(1, so.storingSpaceView.rows);
        float totalWidth = cols * cellSize + (cols - 1) * cellGap;

        // Render rows top-to-bottom (row 0 at top = y = rows - 1 in Cartesian)
        for (int r = 0; r < rows; r++)
        {
            int gridY = rows - 1 - r;
            var rowEl = new VisualElement();
            rowEl.AddToClassList("cell-row");
            rowEl.style.height = cellSize;
            rowEl.style.width = totalWidth;
            rowEl.style.marginBottom = (r < rows - 1) ? cellGap : 0;
            rowEl.style.paddingLeft = 0;
            rowEl.style.paddingRight = 0;
            rowEl.style.paddingTop = 0;
            rowEl.style.paddingBottom = 0;
            rowEl.style.marginLeft = 0;
            rowEl.style.marginRight = 0;
            rowEl.style.marginTop = 0;

            for (int c = 0; c < cols; c++)
            {
                var cellEl = new VisualElement();
                cellEl.AddToClassList("cell");
                cellEl.style.width = cellSize;
                cellEl.style.height = cellSize;
                cellEl.style.marginRight = (c < cols - 1) ? cellGap : 0;
                cellEl.style.paddingLeft = 0;
                cellEl.style.paddingRight = 0;
                cellEl.style.paddingTop = 0;
                cellEl.style.paddingBottom = 0;
                cellEl.style.marginLeft = 0;
                cellEl.style.marginTop = 0;
                cellEl.style.marginBottom = 0;

                var lockIcon = new Label("🔒") { name = "lock-icon" };
                lockIcon.AddToClassList("cell-lock-icon");
                lockIcon.pickingMode = PickingMode.Ignore;
                cellEl.Add(lockIcon);

                var cellData = so.storingSpaceView.cells.Find(d => d.x == c && d.y == gridY);
                ApplyCellVisual(cellEl, lockIcon, cellData);

                cellElements[(c, gridY)] = cellEl;
                rowEl.Add(cellEl);
            }

            grid.Add(rowEl);
        }
    }

    private void ApplyCellVisual(VisualElement cellEl, Label lockIcon, StoringCellData cellData)
    {
        if (cellEl == null) return;
        lockIcon ??= cellEl.Q<Label>("lock-icon");

        if (cellData != null && cellData.isLocked)
        {
            cellEl.AddToClassList("lock");
            cellEl.RemoveFromClassList("empty");
            cellEl.RemoveFromClassList("occupied");
            if (lockIcon != null) lockIcon.style.display = DisplayStyle.Flex;
        }
        else if (cellData != null && cellData.isOccupied)
        {
            cellEl.AddToClassList("occupied");
            cellEl.RemoveFromClassList("lock");
            cellEl.RemoveFromClassList("empty");
            if (lockIcon != null) lockIcon.style.display = DisplayStyle.None;
        }
        else
        {
            cellEl.AddToClassList("empty");
            cellEl.RemoveFromClassList("lock");
            cellEl.RemoveFromClassList("occupied");
            if (lockIcon != null) lockIcon.style.display = DisplayStyle.None;
        }
    }

    public void UpdateCellVisuals()
    {
        if (so == null || so.storingSpaceView == null || so.storingSpaceView.cells == null) return;

        foreach (var kvp in cellElements)
        {
            var coord = kvp.Key;
            var cellEl = kvp.Value;
            if (cellEl == null) continue;

            var cellData = so.storingSpaceView.cells.Find(d => d.x == coord.x && d.y == coord.y);
            var lockIcon = cellEl.Q<Label>("lock-icon");
            ApplyCellVisual(cellEl, lockIcon, cellData);
        }
    }

    private void RenderPlacedItems()
    {
        var layer = root.Q<VisualElement>("items-layer");
        if (layer == null) return;

        layer.Clear();
        itemElements.Clear();

        if (so == null || so.storingSpaceView == null || so.storingSpaceView.placedItems == null) return;

#if UNITY_EDITOR
        if (itemFootprintTemplate == null)
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("UIItemFootprint t:VisualTreeAsset");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                itemFootprintTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            }
        }
#endif

        if (itemFootprintTemplate == null)
        {
            Debug.LogWarning("[UIStoringSpacePanel] Missing 'itemFootprintTemplate' (UIItemFootprint.uxml).", this);
            return;
        }

        int rows = Mathf.Max(1, so.storingSpaceView.rows);
        float step = cellSize + cellGap;

        foreach (var item in so.storingSpaceView.placedItems)
        {
            var itemInstance = itemFootprintTemplate.Instantiate();
            itemInstance.AddToClassList("placed-item");
            itemInstance.dataSource = item; // Bind dataSource to item!

            // Calculate pixel position on the board
            float left = item.x * step;
            float top = (rows - (item.y + item.height)) * step;
            float itemW = item.width * cellSize + (item.width - 1) * cellGap;
            float itemH = item.height * cellSize + (item.height - 1) * cellGap;

            itemInstance.style.position = Position.Absolute;
            itemInstance.style.left = left;
            itemInstance.style.top = top;
            itemInstance.style.width = itemW;
            itemInstance.style.height = itemH;

            // Bind footprint view data
            var fpView = new ItemFootprintView(
                item.itemId,
                item.itemName,
                item.icon,
                item.width,
                item.height,
                item.footprint
            )
            {
                iconSprite = item.iconSprite,
                rotation = item.rotation,
                progress = item.progress
            };

            UIItemFootprintHelper.BindFootprint(itemInstance, fpView, cellSize, cellGap);

            // Hook up Drag & Drop with Grid Snapping + Tap-to-Rotate
            SetupDragAndDrop(itemInstance, item);

            layer.Add(itemInstance);
            itemElements[item] = itemInstance;
        }
    }

    private void SetupDragAndDrop(VisualElement itemInstance, StoringItemData item)
    {
        bool isPointerDown = false;
        bool isDragging = false;
        Vector2 startPointerPos = Vector2.zero;
        float startLeft = 0f;
        float startTop = 0f;
        int startGridX = item.x;
        int startGridY = item.y;
        int candidateX = item.x;
        int candidateY = item.y;
        bool canPlace = true;

        float step = cellSize + cellGap;
        int rows = Mathf.Max(1, so.storingSpaceView.rows);

        itemInstance.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 0) return;

            isPointerDown = true;
            isDragging = false;
            startPointerPos = evt.position;
            startLeft = item.x * step;
            startTop = (rows - (item.y + item.height)) * step;
            startGridX = item.x;
            startGridY = item.y;
            candidateX = item.x;
            candidateY = item.y;
            canPlace = true;

            itemInstance.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        });

        itemInstance.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (!isPointerDown || !itemInstance.HasPointerCapture(evt.pointerId))
                return;

            Vector2 delta = (Vector2)evt.position - startPointerPos;

            if (!isDragging)
            {
                if (delta.sqrMagnitude > 64f) // 8px drag threshold
                {
                    isDragging = true;
                    itemInstance.BringToFront();
                    itemInstance.AddToClassList("item-drag");
                }
            }

            if (isDragging)
            {
                float curLeft = startLeft + delta.x;
                float curTop = startTop + delta.y;

                itemInstance.style.left = curLeft;
                itemInstance.style.top = curTop;

                // Calculate candidate snapped grid coordinate
                candidateX = Mathf.RoundToInt(curLeft / step);
                candidateY = rows - item.height - Mathf.RoundToInt(curTop / step);

                canPlace = CanPlaceItem(item, candidateX, candidateY, ignoreItem: item);

                if (canPlace)
                {
                    itemInstance.RemoveFromClassList("item-bad");
                    itemInstance.AddToClassList("item-ok");
                }
                else
                {
                    itemInstance.RemoveFromClassList("item-ok");
                    itemInstance.AddToClassList("item-bad");
                }

                UpdateGridHighlights(item, candidateX, candidateY, canPlace);
            }

            evt.StopPropagation();
        });

        itemInstance.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (!isPointerDown) return;
            isPointerDown = false;

            if (itemInstance.HasPointerCapture(evt.pointerId))
            {
                itemInstance.ReleasePointer(evt.pointerId);
            }

            ClearGridHighlights();
            itemInstance.RemoveFromClassList("item-drag");
            itemInstance.RemoveFromClassList("item-ok");
            itemInstance.RemoveFromClassList("item-bad");

            if (isDragging)
            {
                isDragging = false;

                if (canPlace)
                {
                    // Snap to grid
                    item.x = candidateX;
                    item.y = candidateY;
                    Debug.Log($"[UIStoringSpacePanel] Snapped {item.itemName} to grid ({item.x}, {item.y})");
                    so.UpdateOccupiedStates();
                    UpdateCellVisuals();
                    SyncStoringSpaceMonoFromSO();
                }
                else
                {
                    // Snap back to start position
                    item.x = startGridX;
                    item.y = startGridY;
                    Debug.Log($"[UIStoringSpacePanel] Invalid position ({candidateX}, {candidateY}). Snapped back to ({startGridX}, {startGridY})");
                }

                RenderPlacedItems();
            }
            else
            {
                // Tap without drag -> Tap to Rotate!
                bool rotated = TryRotateItem(item);
                if (rotated)
                {
                    Debug.Log($"[UIStoringSpacePanel] Rotated {item.itemName}. New size: {item.width}x{item.height}, rot: {item.rotation}");
                    so.UpdateOccupiedStates();
                    UpdateCellVisuals();
                    SyncStoringSpaceMonoFromSO();
                }
                else
                {
                    Debug.Log($"[UIStoringSpacePanel] Cannot rotate {item.itemName} at ({item.x}, {item.y}) - blocked or out of bounds.");
                }

                RenderPlacedItems();
            }

            evt.StopPropagation();
        });

        itemInstance.RegisterCallback<PointerCaptureOutEvent>(evt =>
        {
            if (isDragging)
            {
                isDragging = false;
                isPointerDown = false;
                ClearGridHighlights();
                itemInstance.RemoveFromClassList("item-drag");
                itemInstance.RemoveFromClassList("item-ok");
                itemInstance.RemoveFromClassList("item-bad");
                item.x = startGridX;
                item.y = startGridY;
                RenderPlacedItems();
            }
        });
    }

    public bool CanPlaceItem(StoringItemData item, int targetX, int targetY, StoringItemData ignoreItem = null)
    {
        if (so == null || so.storingSpaceView == null) return false;

        int cols = so.storingSpaceView.columns;
        int rows = so.storingSpaceView.rows;

        // Check if bounding box fits within the grid
        if (targetX < 0 || targetY < 0 || targetX + item.width > cols || targetY + item.height > rows)
            return false;

        // Collect locked cells
        HashSet<(int x, int y)> lockedSet = new();
        if (so.storingSpaceView.cells != null)
        {
            foreach (var c in so.storingSpaceView.cells)
            {
                if (c.isLocked)
                    lockedSet.Add((c.x, c.y));
            }
        }

        // Collect cells occupied by OTHER items
        HashSet<(int x, int y)> otherOccupied = new();
        if (so.storingSpaceView.placedItems != null)
        {
            foreach (var other in so.storingSpaceView.placedItems)
            {
                if (other == ignoreItem) continue;

                for (int r = 0; r < other.height; r++)
                {
                    for (int c = 0; c < other.width; c++)
                    {
                        if (IsFootprintCellOccupied(other, c, r))
                        {
                            int gx = other.x + c;
                            int gy = other.y + (other.height - 1 - r);
                            otherOccupied.Add((gx, gy));
                        }
                    }
                }
            }
        }

        // Validate each active cell of the item at (targetX, targetY)
        for (int r = 0; r < item.height; r++)
        {
            for (int c = 0; c < item.width; c++)
            {
                if (IsFootprintCellOccupied(item, c, r))
                {
                    int gx = targetX + c;
                    int gy = targetY + (item.height - 1 - r);

                    if (gx < 0 || gx >= cols || gy < 0 || gy >= rows)
                        return false;

                    if (lockedSet.Contains((gx, gy)))
                        return false;

                    if (otherOccupied.Contains((gx, gy)))
                        return false;
                }
            }
        }

        return true;
    }

    public bool TryRotateItem(StoringItemData item)
    {
        int oldW = item.width;
        int oldH = item.height;
        int newW = oldH;
        int newH = oldW;

        bool[] newFp = new bool[newW * newH];

        for (int r = 0; r < oldH; r++)
        {
            for (int c = 0; c < oldW; c++)
            {
                int nc = oldH - 1 - r;
                int nr = c;
                bool occupied = IsFootprintCellOccupied(item, c, r);
                newFp[nr * newW + nc] = occupied;
            }
        }

        // Test item template for checking fit
        var testItem = new StoringItemData
        {
            itemId = item.itemId,
            width = newW,
            height = newH,
            footprint = newFp
        };

        // Try candidate positions (current, followed by nearby nudges)
        Vector2Int[] candidateOffsets = {
            new Vector2Int(0, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-2, 0),
            new Vector2Int(0, -2),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1)
        };

        foreach (var offset in candidateOffsets)
        {
            int testX = item.x + offset.x;
            int testY = item.y + offset.y;

            if (CanPlaceItem(testItem, testX, testY, ignoreItem: item))
            {
                item.width = newW;
                item.height = newH;
                item.footprint = newFp;
                item.x = testX;
                item.y = testY;
                item.rotation = (item.rotation + 1) % 4;
                return true;
            }
        }

        return false;
    }

    private bool IsFootprintCellOccupied(StoringItemData item, int col, int row)
    {
        if (item.footprint == null || item.footprint.Length == 0)
            return true;

        if (col < 0 || col >= item.width || row < 0 || row >= item.height)
            return false;

        int idx = row * item.width + col;
        return idx >= 0 && idx < item.footprint.Length && item.footprint[idx];
    }

    private void UpdateGridHighlights(StoringItemData item, int targetX, int targetY, bool canPlace)
    {
        ClearGridHighlights();

        for (int r = 0; r < item.height; r++)
        {
            for (int c = 0; c < item.width; c++)
            {
                if (IsFootprintCellOccupied(item, c, r))
                {
                    int gx = targetX + c;
                    int gy = targetY + (item.height - 1 - r);

                    if (cellElements.TryGetValue((gx, gy), out var cellEl))
                    {
                        cellEl.AddToClassList(canPlace ? "snap-preview" : "snap-invalid");
                    }
                }
            }
        }
    }

    private void ClearGridHighlights()
    {
        foreach (var cellEl in cellElements.Values)
        {
            cellEl.RemoveFromClassList("snap-preview");
            cellEl.RemoveFromClassList("snap-invalid");
        }
    }

    #region Context Menus

    [ContextMenu("Force Re-Render Board")]
    public void ForceRenderBoard()
    {
        RenderBoard();
    }

    [ContextMenu("Sync From StoringSpaceMono")]
    public void ContextSyncWithMono()
    {
        SyncWithStoringSpaceMono();
        RenderBoard();
    }

    #endregion
}
