using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
[RequireComponent(typeof(GridLayoutGroupHelper))]
public class FootprintRequiringVisual : MonoBehaviour
{
    [Header("Config")]
    public GameObject requiringCellPrefab;
    public GameObject emptyCellPrefab; // optional; leave null to use a blank placeholder

    private GridLayoutGroup gridLayoutGroup;
    public GridLayoutGroupHelper GridLayoutGroupHelper { get; private set; }

    private readonly List<GameObject> spawnedCells = new();

    // Used to avoid rebuilding the cell grid every drag frame when the
    // shape hasn't actually changed (e.g. no rotation happened).
    private Footprint lastAppliedFootprint;
    private int lastAppliedRotationSignature = int.MinValue;

    private void Awake()
    {
        gridLayoutGroup = GetComponent<GridLayoutGroup>();
        GridLayoutGroupHelper = GetComponent<GridLayoutGroupHelper>();
    }

    /// <summary>
    /// Rebuilds the placeholder cells to mirror the given footprint's shape.
    /// Safe to call every frame — it no-ops if the shape hasn't changed.
    /// </summary>
    public void SetFootprint(Footprint footprint, int rotationSignature = 0)
    {
        if (footprint == null || requiringCellPrefab == null)
        {
            ClearCells();
            lastAppliedFootprint = null;
            return;
        }

        if (footprint == lastAppliedFootprint && rotationSignature == lastAppliedRotationSignature)
            return; // shape unchanged, nothing to rebuild

        ClearCells();

        bool[,] requiring = footprint.Requiring;
        int width = requiring.GetLength(0);
        int height = requiring.GetLength(1);

        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = width;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isRequiring = requiring[x, y];
                GameObject prefab = isRequiring ? requiringCellPrefab : emptyCellPrefab;
                GameObject cell;

                if (prefab != null)
                {
                    cell = Instantiate(prefab, gridLayoutGroup.transform);
                }
                else
                {
                    cell = new GameObject("EmptyCell", typeof(RectTransform));
                    cell.transform.SetParent(gridLayoutGroup.transform, false);
                }

                spawnedCells.Add(cell);
            }
        }

        GridLayoutGroupHelper.ResizeToFit();

        lastAppliedFootprint = footprint;
        lastAppliedRotationSignature = rotationSignature;
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

    public void Show() => gameObject.SetActive(true);

    public void Hide() => gameObject.SetActive(false);

    /// <summary>
    /// Moves this preview object onto the given pivot cell of the storing space.
    /// Does NOT touch the original item's GridLayoutGroupHelper.
    /// </summary>
    public void SnapTo(GridLayoutGroupHelper storingHelper, Vector2Int pivot)
    {
        GridLayoutGroupHelper.Snap(storingHelper, (RectTransform)transform, pivot);
    }
}