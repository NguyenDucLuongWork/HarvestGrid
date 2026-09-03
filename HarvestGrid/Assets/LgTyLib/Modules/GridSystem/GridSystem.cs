using System;
using UnityEngine;

namespace LgTyLib.Modules.GridSystem
{
    public class GridSystem : MonoBehaviour
    {
        [SerializeField] private int width, height;
        [SerializeField] private GridCell cellPrefab;
        [SerializeField] private RectTransform gridContainer;
        [SerializeField] private CellStyleHandler styleHandler;
        [SerializeField] private Vector2 cellSize = new(64f, 64f);
        [SerializeField] private Vector2 spacing = Vector2.zero;

        private GridCell[,] cells;
        private Enum[,] cellValues;
        public Enum[,] CellValues => cellValues;

        public int Width => width;
        public int Height => height;

        public event Action<int, int, Enum> OnCellChanged;



        public void Init(int width, int height)
        {
            this.width = width;
            this.height = height;

            ClearGrid();

            cells = new GridCell[width, height];
            cellValues = new Enum[width, height];

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
                        // Left-bottom pivot/anchor so anchoredPosition == cell's bottom-left corner.
                        rt.pivot = Vector2.zero;
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.zero;

                        rt.anchoredPosition = new Vector2(
                            x * (cellSize.x + spacing.x),
                            y * (cellSize.y + spacing.y));
                        rt.sizeDelta = cellSize;
                    }

                    cells[x, y] = cell;
                }
            }
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

        public void Clear() => ClearGrid();

        private void ClearGrid()
        {
            if (cells == null) return;

            foreach (var cell in cells)
            {
                if (cell != null)
                    Destroy(cell.gameObject);
            }

            cells = null;
            cellValues = null;
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
            cells[x, y].UpdateCellSprite(styleHandler.GetSprite(value));
            OnCellChanged?.Invoke(x, y, value);
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
            return cellValues[x, y];
        }

        public GridCell GetCell(int x, int y)
        {
            return IsValidCoordinate(x, y) ? cells[x, y] : null;
        }

        public bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
    }
}