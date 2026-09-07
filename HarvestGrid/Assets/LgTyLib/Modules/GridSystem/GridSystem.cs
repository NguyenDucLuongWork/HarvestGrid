using System;
using UnityEngine;

namespace LgTyLib.Modules.GridSystem
{
    public class GridSystem : MonoBehaviour
    {
        [SerializeField] private int width, height;
        [SerializeField] private GridCell cellPrefab;
        [SerializeField] private RectTransform gridContainer;
        [SerializeField] private RectTransform systemContainer;
        [SerializeField] private CellStyleHandler styleHandler;
        [SerializeField] private Vector2 cellSize = new(64f, 64f);
        [SerializeField] private Vector2 spacing = Vector2.zero;

        private GridCell[,] cells;
        private Enum[,] cellValues;
        private bool[,] cellEnabled;
        public Enum[,] CellValues => cellValues;

        public int Width => width;
        public int Height => height;

        public event Action<int, int, Enum> OnCellChanged;
        public event Action<int, int, bool> OnCellEnableChanged;
        public event Action<GridCell> OnCellClicked;

        public void Init(int width, int height)
        {
            this.width = width;
            this.height = height;

            ClearGrid();

            cells = new GridCell[width, height];
            cellValues = new Enum[width, height];
            cellEnabled = new bool[width, height];

            var parent = gridContainer != null ? gridContainer : (RectTransform)transform;

            // Resize the container to fit the grid, pivot/anchor at left-bottom.
            parent.pivot = Vector2.zero;
            parent.anchorMin = Vector2.zero;
            parent.anchorMax = Vector2.zero;
            parent.sizeDelta = new Vector2(
                width * cellSize.x + (width - 1) * spacing.x,
                height * cellSize.y + (height - 1) * spacing.y);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = Instantiate(cellPrefab, parent);
                    cell.name = $"Cell_{x}_{y}";
                    cell.SetCoordinates(x, y);

                    if (cell.transform is RectTransform rt)
                    {
                        rt.pivot = Vector2.zero;
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.zero;

                        rt.anchoredPosition = new Vector2(
                            x * (cellSize.x + spacing.x),
                            y * (cellSize.y + spacing.y));
                        rt.sizeDelta = cellSize;
                    }

                    // Wire every cell's click event up to a single GridSystem-level event.
                    cell.OnClicked += HandleCellClicked;

                    cells[x, y] = cell;
                    cellEnabled[x, y] = true;
                }
            }
            systemContainer.sizeDelta = gridContainer.sizeDelta;
        }

        private void HandleCellClicked(GridCell cell)
        {
            OnCellClicked?.Invoke(cell);
        }

        /// <summary>
        /// Initializes the grid and seeds every cell with the given base enum value.
        /// </summary>
        public void Init(int width, int height, Enum baseValue)
        {
            Init(width, height);

            if (baseValue == null)
                return;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    SetCell(x, y, baseValue);
                }
            }
        }

        /// <summary>
        /// Initializes the grid, seeding each cell using a generator function (e.g. ItemGenerator.Instance.CellType).
        /// baseValue is used as a fallback whenever the generator returns null.
        /// </summary>
        public void Init(int width, int height, Enum baseValue, Func<Enum> valueGenerator)
        {
            Init(width, height);

            if (valueGenerator == null)
            {
                if (baseValue == null)
                    return;

                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        SetCell(x, y, baseValue);

                return;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var value = valueGenerator() ?? baseValue;
                    if (value != null)
                        SetCell(x, y, value);
                }
            }
        }

        public void Clear() => ClearGrid();

        private void ClearGrid()
        {
            if (cells == null) return;

            foreach (var cell in cells)
            {
                if (cell != null)
                {
                    cell.OnClicked -= HandleCellClicked;
                    Destroy(cell.gameObject);
                }
            }

            cells = null;
            cellValues = null;
            cellEnabled = null;
        }

        public void SetCell(int x, int y, Enum value)
        {
            if (!IsValidCoordinate(x, y))
            {
                Debug.LogWarning($"[GridSystem] Coordinate ({x},{y}) is out of bounds.");
                return;
            }

            if (styleHandler == null)
            {
                Debug.LogWarning("[GridSystem] No CellStyleHandler assigned.");
                return;
            }

            if (!styleHandler.Supports(value.GetType()))
            {
                Debug.LogWarning(
                    $"[GridSystem] Style handler '{styleHandler.name}' does not support enum type {value.GetType().Name}.");
                return;
            }

            cellValues[x, y] = value;
            RefreshCellSprite(x, y);
            OnCellChanged?.Invoke(x, y, value);
        }

        /// <summary>
        /// Enables or disables a single cell. Disabled cells display the style handler's
        /// disabled sprite instead of the sprite for their current value, and do not fire click events.
        /// </summary>
        public void SetCellEnable(int x, int y, bool enable)
        {
            if (!IsValidCoordinate(x, y))
            {
                Debug.LogWarning($"[GridSystem] Coordinate ({x},{y}) is out of bounds.");
                return;
            }

            cellEnabled[x, y] = enable;
            cells[x, y].SetEnable(enable);
            RefreshCellSprite(x, y);
            OnCellEnableChanged?.Invoke(x, y, enable);
        }

        /// <summary>
        /// Enables or disables a rectangular block of cells, starting at (x, y)
        /// with the given width/height. (x, y) is the bottom-left corner of the block.
        /// </summary>
        public void SetCellsEnable(int x, int y, int width, int height, bool enable)
        {
            if (width <= 0 || height <= 0)
            {
                Debug.LogWarning($"[GridSystem] Invalid block size ({width}x{height}).");
                return;
            }

            for (int j = y; j < y + height; j++)
            {
                for (int i = x; i < x + width; i++)
                {
                    SetCellEnable(i, j, enable);
                }
            }
        }

        /// <summary>
        /// Re-applies the correct sprite for a cell based on its current value and enabled state.
        /// </summary>
        private void RefreshCellSprite(int x, int y)
        {
            if (styleHandler == null)
                return;

            var sprite = cellEnabled[x, y]
                ? styleHandler.GetSprite(cellValues[x, y])
                : styleHandler.GetDisabledSprite();

            cells[x, y].UpdateCellSprite(sprite);
        }

        /// <summary>
        /// Sets a rectangular block of cells, starting at (x, y) with the given width/height,
        /// to the same enum value. (x, y) is the bottom-left corner of the block.
        /// </summary>
        public void SetCells(int x, int y, int width, int height, Enum value)
        {
            if (width <= 0 || height <= 0)
            {
                Debug.LogWarning($"[GridSystem] Invalid block size ({width}x{height}).");
                return;
            }

            for (int j = y; j < y + height; j++)
            {
                for (int i = x; i < x + width; i++)
                {
                    SetCell(i, j, value);
                }
            }
        }

        /// <summary>
        /// Sets a rectangular block of cells starting at (x, y) using a 2D array of values.
        /// values[i, j] maps to grid cell (x + i, y + j), where (x, y) is the bottom-left corner.
        /// width/height must match the array's dimensions.
        /// </summary>
        public void SetCells(int x, int y, int width, int height, Enum[,] values)
        {
            if (values == null)
            {
                Debug.LogWarning("[GridSystem] SetCells called with null value array.");
                return;
            }

            if (width <= 0 || height <= 0)
            {
                Debug.LogWarning($"[GridSystem] Invalid block size ({width}x{height}).");
                return;
            }

            if (values.GetLength(0) != width || values.GetLength(1) != height)
            {
                Debug.LogWarning(
                    $"[GridSystem] Value array size ({values.GetLength(0)}x{values.GetLength(1)}) does not match given width/height ({width}x{height}).");
                return;
            }

            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    var value = values[i, j];
                    if (value == null)
                        continue;

                    SetCell(x + i, y + j, value);
                }
            }
        }

        public Enum GetCellValue(int x, int y)
        {
            if (!IsValidCoordinate(x, y))
            {
                Debug.LogWarning($"[GridSystem] Coordinate ({x},{y}) is out of bounds.");
                return null;
            }

            if (cellValues == null)
            {
                Debug.LogWarning($"[GridSystem] '{name}' has not been initialized (call Init first).");
                return null;
            }

            return cellValues[x, y];
        }

        public bool IsCellEnabled(int x, int y)
        {
            if (!IsValidCoordinate(x, y))
            {
                Debug.LogWarning($"[GridSystem] Coordinate ({x},{y}) is out of bounds.");
                return false;
            }
            return cellEnabled[x, y];
        }

        public GridCell GetCell(int x, int y)
        {
            return IsValidCoordinate(x, y) ? cells[x, y] : null;
        }

        public bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public RectTransform GridContainer => gridContainer;
        public Vector2 CellSize => cellSize;
        public Vector2 Spacing => spacing;

        /// <summary>
        /// Populates this GridSystem from GridCell components that already exist as
        /// children (e.g. hand-placed in the editor for a custom footprint shape),
        /// instead of instantiating new ones from cellPrefab. Existing child cells are
        /// kept as-is; width/height and each cell's (x, y) are inferred from their
        /// anchored position using cellSize/spacing. Cell values default to null —
        /// seed them afterward with SetCell/SetCells if needed.
        /// </summary>
        public void GetCellsFromChildren()
        {
            var parent = gridContainer != null ? gridContainer : (RectTransform)transform;
            var found = parent.GetComponentsInChildren<GridCell>(true);

            if (found == null || found.Length == 0)
            {
                Debug.LogWarning($"[GridSystem] '{name}' has no GridCell children to initialize from.");
                return;
            }

            var step = cellSize + spacing;
            int maxX = 0, maxY = 0;

            // Hand-placed cells won't have SetCoordinates called on them yet,
            // so infer (x, y) from anchored position.
            foreach (var cell in found)
            {
                if (cell.transform is not RectTransform rt)
                    continue;

                int x = Mathf.RoundToInt(rt.anchoredPosition.x / step.x);
                int y = Mathf.RoundToInt(rt.anchoredPosition.y / step.y);
                cell.SetCoordinates(x, y);

                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }

            width = maxX + 1;
            height = maxY + 1;

            cells = new GridCell[width, height];
            cellValues = new Enum[width, height];
            cellEnabled = new bool[width, height];

            foreach (var cell in found)
            {
                int x = cell.X;
                int y = cell.Y;

                if (!IsValidCoordinate(x, y))
                {
                    Debug.LogWarning($"[GridSystem] Child cell '{cell.name}' resolved to out-of-range coordinate ({x},{y}).");
                    continue;
                }

                cell.OnClicked += HandleCellClicked;
                cells[x, y] = cell;
                cellEnabled[x, y] = true;
            }

            if (systemContainer != null && gridContainer != null)
                systemContainer.sizeDelta = gridContainer.sizeDelta;
        }
    }


}