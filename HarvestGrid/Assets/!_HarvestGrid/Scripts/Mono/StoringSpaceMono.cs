using System.Collections.Generic;
using LgTyLib.Core;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class StoringSpaceMono : BaseSingleton<StoringSpaceMono>
{
    [Header("Grid Size")]
    [SerializeField] private int width = 3;
    [SerializeField] private int height = 3;

    [SerializeField]
    private StoringSpace storingSpace;
    public StoringSpace StoringSpace => storingSpace;

    [Header("Cell Prefabs")]
    public GameObject availableCellPrefab;
    public GameObject unavailableCellPrefab;
    // Optional. If left null, occupied cells fall back to availableCellPrefab
    // (useful if you draw the occupying item on top instead of a distinct cell look).
    public GameObject occupiedCellPrefab;

    private GridLayoutGroup gridLayoutGroup;
    private RectTransform rectTransform;
    private readonly List<GameObject> spawnedCells = new();

    // --------------------------------------------------
    // INIT
    // --------------------------------------------------

    protected override void Awake()
    {
        base.Awake();

        gridLayoutGroup = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();

        storingSpace = new StoringSpace(width, height);
        RefreshVisuals();
    }

    // --------------------------------------------------
    // SCREEN <-> GRID CONVERSION
    // Use this to figure out which cell the mouse/touch/drag
    // point is over, e.g. while dragging an inventory item.
    // --------------------------------------------------

    public bool TryGetCellUnderScreenPoint(
        Vector2 screenPosition,
        Camera eventCamera,
        out Vector2Int cell)
    {
        cell = default;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPosition, eventCamera, out Vector2 localPoint))
        {
            return false;
        }

        Rect rect = rectTransform.rect;

        // Convert from the RectTransform's local space (origin at pivot)
        // to a bottom-left-origin coordinate, matching StoringSpace's layout.
        float xFromLeft = localPoint.x - rect.xMin;
        float yFromBottom = localPoint.y - rect.yMin;

        float cellStepX = gridLayoutGroup.cellSize.x + gridLayoutGroup.spacing.x;
        float cellStepY = gridLayoutGroup.cellSize.y + gridLayoutGroup.spacing.y;

        int x = Mathf.FloorToInt(xFromLeft / cellStepX);
        int y = Mathf.FloorToInt(yFromBottom / cellStepY);

        int gridWidth = storingSpace.Cells.GetLength(0);
        int gridHeight = storingSpace.Cells.GetLength(1);

        if (x < 0 || y < 0 || x >= gridWidth || y >= gridHeight)
            return false;

        cell = new Vector2Int(x, y);
        return true;
    }

    // --------------------------------------------------
    // PLACEMENT
    // --------------------------------------------------

    public bool TryPlaceFootprint(Footprint footprint, Vector2Int bottomLeftPivot)
    {
        bool placed = storingSpace.TryAddFootprint(footprint, bottomLeftPivot);

        if (placed)
            RefreshVisuals();

        return placed;
    }

    // Convenience overload if you're placing straight from a FootprintMono
    // (e.g. an item currently being dragged).
    public bool TryPlaceFootprint(FootprintMono footprintMono, Vector2Int bottomLeftPivot)
    {
        return TryPlaceFootprint(footprintMono.Footprint, bottomLeftPivot);
    }

    public void RemoveFootprint(Footprint footprint)
    {
        storingSpace.RemoveFootprint(footprint);
        RefreshVisuals();
    }

    public bool CheckIfRotatable(Footprint footprint)
    {
        return storingSpace.CheckIfRotatable(footprint);
    }

    // --------------------------------------------------
    // VISUALS
    // --------------------------------------------------

    public void RefreshVisuals()
    {
        ClearCells();

        if (storingSpace == null)
            return;

        StoringCellType[,] cells = storingSpace.Cells;
        int gridWidth = cells.GetLength(0);
        int gridHeight = cells.GetLength(1);

        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = gridWidth;

        // Sibling order must be row-major, top row first, to match
        // Start Corner = Lower Left in the inspector (same convention
        // FootprintMono uses).
        for (int y = gridHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                GameObject prefab = GetPrefabFor(cells[x, y]);
                GameObject cell;

                if (prefab != null)
                {
                    cell = Instantiate(prefab, transform);
                }
                else
                {
                    // No prefab assigned for this state: spawn a blank
                    // placeholder so the layout still reserves the slot.
                    cell = new GameObject($"Cell_{x}_{y}", typeof(RectTransform));
                    cell.transform.SetParent(transform, false);
                }

                spawnedCells.Add(cell);
            }
        }
    }

    private GameObject GetPrefabFor(StoringCellType cellType)
    {
        switch (cellType)
        {
            case StoringCellType.Occupied:
                return occupiedCellPrefab != null ? occupiedCellPrefab : availableCellPrefab;
            case StoringCellType.Unavailable:
                return unavailableCellPrefab;
            default:
                return availableCellPrefab;
        }
    }

    private void ClearCells()
    {
        for (int i = 0; i < spawnedCells.Count; i++)
        {
            if (spawnedCells[i] != null)
                Destroy(spawnedCells[i]);
        }

        spawnedCells.Clear();
    }

    public void SetData(StoringSpace storingSpace)
    {
        this.storingSpace = storingSpace;
        RefreshVisuals();
    }
}