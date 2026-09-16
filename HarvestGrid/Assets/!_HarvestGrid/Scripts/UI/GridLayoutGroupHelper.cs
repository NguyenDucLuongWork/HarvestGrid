using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class GridLayoutGroupHelper : MonoBehaviour
{
    [Header("Auto Size")]
    [SerializeField] private bool autoSizeEnabled = true;
    [SerializeField] private AutoSizeConfig autoSizeConfig = AutoSizeConfig.Default;

    [Header("Move Root")]
    [Tooltip("The transform that actually gets moved by Snap(). " +
             "Leave empty if this GameObject IS the item's root. " +
             "Assign the parent (e.g. ItemWithFootprintMono's RectTransform) " +
             "when this helper lives on a CHILD of the item, so the whole " +
             "item (image, other siblings, etc.) moves together instead of " +
             "just the grid.")]
    [SerializeField] private RectTransform moveRoot;

    [Header("Debug")]
    [SerializeField] private bool showDebugRect = true;
    [SerializeField] private bool showDebugCells = true;
    [SerializeField] private Color debugColor = Color.green;
    [SerializeField] private Color cellColor = new Color(0f, 1f, 1f, 0.5f);
    [SerializeField] private Color cellOriginColor = Color.red;

    private GridLayoutGroup grid;
    private RectTransform rectTransform;

    public GridLayoutGroup Grid => grid != null ? grid : (grid = GetComponent<GridLayoutGroup>());
    public RectTransform RT => rectTransform != null ? rectTransform : (rectTransform = GetComponent<RectTransform>());

    /// <summary>
    /// The transform that Snap() moves. Falls back to this grid's own
    /// RectTransform (RT) when no explicit move root is assigned, which
    /// keeps behaviour identical for setups where the helper IS the root.
    /// </summary>
    public RectTransform MoveRoot => moveRoot != null ? moveRoot : RT;

    [System.Serializable]
    public struct AutoSizeConfig
    {
        [Tooltip("Extra padding added on top of the grid padding when resizing.")]
        public Vector2 extraPadding;

        [Tooltip("Minimum size the rect is allowed to shrink to (0 = no min).")]
        public Vector2 minSize;

        [Tooltip("Maximum size the rect is allowed to grow to (0 = no max).")]
        public Vector2 maxSize;

        public static AutoSizeConfig Default => new AutoSizeConfig
        {
            extraPadding = Vector2.zero,
            minSize = Vector2.zero,
            maxSize = Vector2.zero
        };
    }

    /// <summary>
    /// Custom data type holding the 4 corners of a rect in world space.
    /// </summary>
    public struct RectCorners
    {
        public Vector3 BottomLeft;
        public Vector3 TopLeft;
        public Vector3 TopRight;
        public Vector3 BottomRight;

        public RectCorners(Vector3 bl, Vector3 tl, Vector3 tr, Vector3 br)
        {
            BottomLeft = bl;
            TopLeft = tl;
            TopRight = tr;
            BottomRight = br;
        }
    }

    /// <summary>
    /// Records the result of the last successful Snap() call: which target
    /// this grid snapped to, and which (col, row) cell of that target's
    /// overlapped region it aligned with.
    /// </summary>
    public struct SnapInfo
    {
        public GridLayoutGroupHelper Target;
        public Vector2Int CellIndex;
        public bool IsSnapped;

        public static SnapInfo None => new SnapInfo { Target = null, CellIndex = default, IsSnapped = false };
    }

    /// <summary>
    /// The most recent snap result for this grid. Read-only from outside;
    /// set internally by Snap(). SnapInfo.IsSnapped is false if never snapped.
    /// </summary>
    public SnapInfo SnappedTo { get; private set; } = SnapInfo.None;

    private void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    // ---------------------------------------------------------------
    // AUTO SIZE
    // ---------------------------------------------------------------

    public void ResizeToFit()
    {
        if (!autoSizeEnabled) return;

        int childCount = transform.childCount;

        if (childCount == 0)
        {
            Vector2 empty = new Vector2(Grid.padding.horizontal, Grid.padding.vertical) + autoSizeConfig.extraPadding;
            RT.sizeDelta = ClampToConfig(empty);
            return;
        }

        GetGridDimensions(childCount, out int columns, out int rows);

        float width =
            Grid.padding.left +
            Grid.padding.right +
            columns * Grid.cellSize.x +
            Mathf.Max(0, columns - 1) * Grid.spacing.x;

        float height =
            Grid.padding.top +
            Grid.padding.bottom +
            rows * Grid.cellSize.y +
            Mathf.Max(0, rows - 1) * Grid.spacing.y;

        Vector2 size = new Vector2(width, height) + autoSizeConfig.extraPadding;
        RT.sizeDelta = ClampToConfig(size);
    }

    private Vector2 ClampToConfig(Vector2 size)
    {
        if (autoSizeConfig.minSize.x > 0) size.x = Mathf.Max(size.x, autoSizeConfig.minSize.x);
        if (autoSizeConfig.minSize.y > 0) size.y = Mathf.Max(size.y, autoSizeConfig.minSize.y);
        if (autoSizeConfig.maxSize.x > 0) size.x = Mathf.Min(size.x, autoSizeConfig.maxSize.x);
        if (autoSizeConfig.maxSize.y > 0) size.y = Mathf.Min(size.y, autoSizeConfig.maxSize.y);
        return size;
    }

    private void GetGridDimensions(int childCount, out int columns, out int rows)
    {
        if (Grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            columns = Grid.constraintCount;
            rows = Mathf.CeilToInt((float)childCount / columns);
        }
        else if (Grid.constraint == GridLayoutGroup.Constraint.FixedRowCount)
        {
            rows = Grid.constraintCount;
            columns = Mathf.CeilToInt((float)childCount / rows);
        }
        else
        {
            columns = Mathf.CeilToInt(Mathf.Sqrt(childCount));
            rows = Mathf.CeilToInt((float)childCount / columns);
        }
    }

    /// <summary>
    /// Public helper so debug drawing / external code can know the current
    /// used grid dimensions without duplicating the constraint logic.
    /// </summary>
    public Vector2Int GetUsedDimensions()
    {
        int childCount = Mathf.Max(1, transform.childCount);
        GetGridDimensions(childCount, out int columns, out int rows);
        return new Vector2Int(columns, rows);
    }

    // ---------------------------------------------------------------
    // CORNERS / BOUNDS
    // ---------------------------------------------------------------

    /// <summary>
    /// Returns the 4 corners of this RectTransform in world space.
    /// </summary>
    public RectCorners GetWorldCorners()
    {
        Vector3[] corners = new Vector3[4];
        RT.GetWorldCorners(corners);
        // Unity order: [0] bottom-left, [1] top-left, [2] top-right, [3] bottom-right
        return new RectCorners(corners[0], corners[1], corners[2], corners[3]);
    }

    /// <summary>
    /// Returns the 4 corners of a single cell (col, row) of THIS grid, in world space.
    /// </summary>
    public RectCorners GetCellWorldCorners(int col, int row)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(RT);

        if (TryGetCellChild(col, row, out RectTransform cell))
        {
            Vector3[] corners = new Vector3[4];
            cell.GetWorldCorners(corners);
            return new RectCorners(corners[0], corners[1], corners[2], corners[3]);
        }

        GetContentLocalOrigin(out float leftX, out float bottomY);

        float localMinX = leftX + col * (Grid.cellSize.x + Grid.spacing.x);
        float localMinY = bottomY + row * (Grid.cellSize.y + Grid.spacing.y);
        float localMaxY = localMinY + Grid.cellSize.y;
        float localMaxX = localMinX + Grid.cellSize.x;

        Vector3 bl = RT.TransformPoint(new Vector3(localMinX, localMinY, 0f));
        Vector3 tl = RT.TransformPoint(new Vector3(localMinX, localMaxY, 0f));
        Vector3 tr = RT.TransformPoint(new Vector3(localMaxX, localMaxY, 0f));
        Vector3 br = RT.TransformPoint(new Vector3(localMaxX, localMinY, 0f));

        return new RectCorners(bl, tl, tr, br);
    }

    /// <summary>
    /// Axis-aligned bounding box of this rect in world space (ignores rotation).
    /// </summary>
    public UnityEngine.Rect GetWorldAABB()
    {
        RectCorners c = GetWorldCorners();
        float xMin = Mathf.Min(c.BottomLeft.x, c.TopLeft.x, c.TopRight.x, c.BottomRight.x);
        float xMax = Mathf.Max(c.BottomLeft.x, c.TopLeft.x, c.TopRight.x, c.BottomRight.x);
        float yMin = Mathf.Min(c.BottomLeft.y, c.TopLeft.y, c.TopRight.y, c.BottomRight.y);
        float yMax = Mathf.Max(c.BottomLeft.y, c.TopLeft.y, c.TopRight.y, c.BottomRight.y);
        return UnityEngine.Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    // ---------------------------------------------------------------
    // SNAPPING
    // ---------------------------------------------------------------

    /// <summary>
    /// Returns true if this grid's world rect overlaps the target grid's world rect.
    /// </summary>
    public bool CanSnap(GridLayoutGroupHelper target)
    {
        if (target == null || target == this) return false;
        return GetWorldAABB().Overlaps(target.GetWorldAABB());
    }


    /// <summary>
    /// Computes which cell of `target` this grid would snap to, WITHOUT moving
    /// anything. Lets callers validate a placement (e.g. against StoringSpace)
    /// before committing to it.
    /// </summary>
    public bool TryComputeSnapCellIndex(GridLayoutGroupHelper target, out Vector2Int targetCellIndex)
    {
        targetCellIndex = new Vector2Int(-1, -1);

        if (!CanSnap(target))
            return false;

        UnityEngine.Rect a = GetWorldAABB();
        UnityEngine.Rect b = target.GetWorldAABB();

        UnityEngine.Rect overlap = UnityEngine.Rect.MinMaxRect(
            Mathf.Max(a.xMin, b.xMin),
            Mathf.Max(a.yMin, b.yMin),
            Mathf.Min(a.xMax, b.xMax),
            Mathf.Min(a.yMax, b.yMax)
        );

        Vector3 overlapBottomLeft = new Vector3(overlap.xMin, overlap.yMin, RT.position.z);
        targetCellIndex = target.WorldPointToCellIndex(overlapBottomLeft);
        return true;
    }

    public void Snap(
        GridLayoutGroupHelper target,
        RectTransform transform,
        Vector2Int targetCellIndex)
    {
        if (target == null || target == this)
        {
            Debug.LogWarning(
                $"[{nameof(GridLayoutGroupHelper)}] Snap aborted: invalid target."
            );
            return;
        }

        // Target cell bottom-left in world space
        Vector3 targetCellWorldBL =
            target.GetCellWorldBottomLeft(
                targetCellIndex.x,
                targetCellIndex.y
            );

        // This item's (0,0) cell bottom-left offset from its root
        Vector3 myCellWorldBL =
            GetCellWorldBottomLeft(0, 0);

        // Offset between the item's root and its (0,0) cell
        Vector3 cellOffset = transform.position - myCellWorldBL;

        // Set root directly in world space
        transform.position = targetCellWorldBL + cellOffset;

        SnappedTo = new SnapInfo
        {
            Target = target,
            CellIndex = targetCellIndex,
            IsSnapped = true
        };
    }
    /// <summary>
    /// Clears the recorded snap state (does not move the object).
    /// </summary>
    public void ClearSnapInfo()
    {
        SnappedTo = SnapInfo.None;
    }

    /// <summary>
    /// World-space bottom-left corner of the cell nearest to a given world point.
    /// </summary>
    public Vector3 GetNearestCellWorldBottomLeft(Vector3 worldPoint)
    {
        Vector2Int idx = WorldPointToCellIndex(worldPoint);
        return GetCellWorldBottomLeft(idx.x, idx.y);
    }

    /// <summary>
    /// World-space bottom-left corner of cell (col, row) in THIS grid.
    /// Assumes Start Corner = Upper Left, Start Axis = Horizontal.
    /// </summary>
    private Vector3 GetCellWorldBottomLeft(int col, int row)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(RT);

        if (TryGetCellChild(col, row, out RectTransform cell))
        {
            Vector3[] corners = new Vector3[4];
            cell.GetWorldCorners(corners);
            return corners[0];
        }

        GetContentLocalOrigin(out float leftX, out float bottomY);

        float localMinX = leftX + col * (Grid.cellSize.x + Grid.spacing.x);
        float localMinY = bottomY + row * (Grid.cellSize.y + Grid.spacing.y);

        return RT.TransformPoint(new Vector3(localMinX, localMinY, 0f));
    }

    private bool TryGetCellChild(int col, int row, out RectTransform cell)
    {
        cell = null;

        Vector2Int dimensions = GetUsedDimensions();
        if (col < 0 || col >= dimensions.x || row < 0 || row >= dimensions.y)
            return false;

        int columnInStartCorner = col;
        int rowInStartCorner = row;

        switch (Grid.startCorner)
        {
            case GridLayoutGroup.Corner.UpperLeft:
                rowInStartCorner = dimensions.y - 1 - row;
                break;
            case GridLayoutGroup.Corner.UpperRight:
                columnInStartCorner = dimensions.x - 1 - col;
                rowInStartCorner = dimensions.y - 1 - row;
                break;
            case GridLayoutGroup.Corner.LowerRight:
                columnInStartCorner = dimensions.x - 1 - col;
                break;
        }

        int childIndex;
        if (Grid.startAxis == GridLayoutGroup.Axis.Horizontal)
            childIndex = rowInStartCorner * dimensions.x + columnInStartCorner;
        else
            childIndex = columnInStartCorner * dimensions.y + rowInStartCorner;

        if (childIndex < 0 || childIndex >= transform.childCount)
            return false;

        cell = transform.GetChild(childIndex) as RectTransform;
        return cell != null;
    }

    /// <summary>
    /// Converts a world point into a (col, row) cell index of THIS grid, clamped
    /// to the currently used grid dimensions.
    /// </summary>
    private Vector2Int WorldPointToCellIndex(Vector3 worldPoint)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(RT);

        Vector2Int dimensions = GetUsedDimensions();
        float closestDistance = float.PositiveInfinity;
        Vector2Int closestCell = Vector2Int.zero;

        for (int row = 0; row < dimensions.y; row++)
        {
            for (int col = 0; col < dimensions.x; col++)
            {
                if (!TryGetCellChild(col, row, out RectTransform cell))
                    continue;

                Vector3[] corners = new Vector3[4];
                cell.GetWorldCorners(corners);
                UnityEngine.Rect cellBounds = UnityEngine.Rect.MinMaxRect(
                    corners[0].x,
                    corners[0].y,
                    corners[2].x,
                    corners[2].y
                );

                if (cellBounds.Contains(worldPoint))
                    return new Vector2Int(col, row);

                float distance = (cellBounds.center - (Vector2)worldPoint).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCell = new Vector2Int(col, row);
                }
            }
        }

        if (closestDistance < float.PositiveInfinity)
            return closestCell;

        GetContentLocalOrigin(out float leftX, out float bottomY);

        Vector3 local = RT.InverseTransformPoint(worldPoint);

        float xFromLeft = local.x - leftX;
        float yFromBottom = local.y - bottomY;

        int fallbackCol = Mathf.FloorToInt(xFromLeft / (Grid.cellSize.x + Grid.spacing.x));
        int fallbackRow = Mathf.FloorToInt(yFromBottom / (Grid.cellSize.y + Grid.spacing.y));

        Vector2Int dims = GetUsedDimensions();
        fallbackCol = Mathf.Clamp(fallbackCol, 0, Mathf.Max(0, dims.x - 1));
        fallbackRow = Mathf.Clamp(fallbackRow, 0, Mathf.Max(0, dims.y - 1));

        return new Vector2Int(fallbackCol, fallbackRow);
    }

    // ---------------------------------------------------------------
    // DEBUG
    // ---------------------------------------------------------------

    private void OnDrawGizmos()
    {
        if (RT == null || Grid == null) return;

        if (showDebugRect)
        {
            RectCorners c = GetWorldCorners();
            Gizmos.color = debugColor;
            Gizmos.DrawLine(c.BottomLeft, c.TopLeft);
            Gizmos.DrawLine(c.TopLeft, c.TopRight);
            Gizmos.DrawLine(c.TopRight, c.BottomRight);
            Gizmos.DrawLine(c.BottomRight, c.BottomLeft);
        }

        if (showDebugCells)
        {
            Vector2Int dims = GetUsedDimensions();
            Gizmos.color = cellColor;

            for (int row = 0; row < dims.y; row++)
            {
                for (int col = 0; col < dims.x; col++)
                {
                    RectCorners cell = GetCellWorldCorners(col, row);

                    // Draw cell rectangle
                    Gizmos.DrawLine(cell.BottomLeft, cell.TopLeft);
                    Gizmos.DrawLine(cell.TopLeft, cell.TopRight);
                    Gizmos.DrawLine(cell.TopRight, cell.BottomRight);
                    Gizmos.DrawLine(cell.BottomRight, cell.BottomLeft);

#if UNITY_EDITOR
                    GUIStyle style = new GUIStyle(UnityEditor.EditorStyles.boldLabel);
                    style.alignment = TextAnchor.MiddleCenter;
                    style.normal.textColor = Color.red;

                    Vector3 cellCenter = (cell.BottomLeft + cell.TopRight) * 0.5f;

                    UnityEditor.Handles.Label(
                        cellCenter,
                        $"[{col},{row}]",
                        style
                    );

#endif
                }
            }
        }

        if (showDebugRect || showDebugCells)
        {
            // Mark cell (0,0) origin so snap alignment is easy to verify visually.
            Gizmos.color = cellOriginColor;
            Vector3 cellBL = GetCellWorldBottomLeft(0, 0);
            Gizmos.DrawSphere(cellBL, 4f);
        }

#if UNITY_EDITOR
        if (SnappedTo.IsSnapped && SnappedTo.Target != null)
        {
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(RT.position + Vector3.up * 10f,
                $"Snapped to: {SnappedTo.Target.name} [{SnappedTo.CellIndex.x},{SnappedTo.CellIndex.y}]");
        }
#endif
    }

    /// <summary>
    /// Computes the local-space bottom-left corner of the cell BLOCK (not a single
    /// cell) — i.e. where col=0,row=0 sits after Unity has applied Child Alignment
    /// to distribute any leftover space in the rect. Mirrors LayoutGroup's own
    /// GetStartOffset/GetAlignmentOnAxis logic.
    /// </summary>
    private void GetContentLocalOrigin(out float leftX, out float bottomY)
    {
        Vector2Int dims = GetUsedDimensions();
        int columns = dims.x;
        int rows = dims.y;

        float contentWidth = columns * Grid.cellSize.x + Mathf.Max(0, columns - 1) * Grid.spacing.x;
        float contentHeight = rows * Grid.cellSize.y + Mathf.Max(0, rows - 1) * Grid.spacing.y;

        float surplusX = RT.rect.width - Grid.padding.horizontal - contentWidth;
        float surplusY = RT.rect.height - Grid.padding.vertical - contentHeight;

        // TextAnchor layout: 0=UpperLeft ... 8=LowerRight.
        // % 3 -> 0 left, 1 center, 2 right. / 3 -> 0 upper, 1 middle, 2 lower.
        float alignX = ((int)Grid.childAlignment % 3) * 0.5f;
        float alignY = ((int)Grid.childAlignment / 3) * 0.5f;

        float offsetFromLeft = Grid.padding.left + surplusX * alignX;
        float offsetFromTop = Grid.padding.top + surplusY * alignY;

        leftX = RT.rect.xMin + offsetFromLeft;
        float topY = RT.rect.yMax - offsetFromTop;
        bottomY = topY - contentHeight;
    }
}