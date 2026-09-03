using LgTyLib.Core;
using LgTyLib.Modules.GridSystem;
using UnityEngine;

public class Inventory : BaseSingleton<Inventory>
{
    [SerializeField]
    private GridSystem gridSystem;
    private void Start()
    {
        gridSystem.Init(5, 5, InventoryCellType.Empty);

        gridSystem.OnCellChanged += (x, y, value) =>
        {
            Debug.Log($"Cell ({x},{y}) changed to {value}");
        };

        gridSystem.GetCell(0, 0).OnClicked += cell =>
        {
            Debug.Log($"Clicked cell at {cell.X},{cell.Y}, current value: {gridSystem.GetCellValue(cell.X, cell.Y)}");
        };

        gridSystem.SetCells(1, 1, 2, 2, InventoryCellType.Occupied);
    }

}
