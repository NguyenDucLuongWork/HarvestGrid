using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class FootprintMono : MonoBehaviour
{
    public ItemFootprintSO footprintSO;
    [Header("Config")]
    public GameObject requiringCellPrefab;
    public GameObject emptyCellPrefab; // optional; leave null to use a blank placeholder

    [SerializeField]
    private Footprint footprint;
    public Footprint Footprint => footprint;

    private GridLayoutGroup gridLayoutGroup;
    private readonly List<GameObject> spawnedCells = new();

    private void Awake()
    {
        gridLayoutGroup = GetComponent<GridLayoutGroup>();
    }

    [ContextMenu("Test")]
    public void Test()
    {
        SetData(footprintSO.footprint);
    }
    public void SetData(Footprint footprint)
    {
        this.footprint = footprint;
        OnDataFootprintChanged();
    }

    public void Rotate()
    {
        if (footprint == null)
            return;

        footprint.Rotate();
        OnDataFootprintChanged();
    }

    public void OnDataFootprintChanged()
    {
        ClearCells();

        if (footprint == null || requiringCellPrefab == null)
            return;

        if (gridLayoutGroup == null)
            gridLayoutGroup = GetComponent<GridLayoutGroup>();

        bool[,] requiring = footprint.Requiring;
        int width = requiring.GetLength(0);
        int height = requiring.GetLength(1);

        // Flexible constraint can't guarantee a width x height shape; force it.
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = width;

        // Sibling order must be row-major, top row first, to match
        // Start Corner = Lower Left / Start Axis = Vertical in the inspector.
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                bool isRequiring = requiring[x, y];
                GameObject prefab = isRequiring ? requiringCellPrefab : emptyCellPrefab;
                GameObject cell;

                if (prefab != null)
                {
                    cell = Instantiate(prefab, transform);
                }
                else
                {
                    // No empty-cell prefab assigned: spawn a blank placeholder
                    // so the layout still reserves this slot.
                    cell = new GameObject("EmptyCell", typeof(RectTransform));
                    cell.transform.SetParent(transform, false);
                }

                spawnedCells.Add(cell);
            }
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
}