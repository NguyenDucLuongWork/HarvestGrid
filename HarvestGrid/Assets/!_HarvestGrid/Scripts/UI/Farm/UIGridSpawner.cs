using System.Collections.Generic;
using UnityEngine;

public class UIGridSpawner : MonoBehaviour
{
    [Header("Grid")]
    [Min(1)]
    [SerializeField] private int columns = 5;

    [Min(1)]
    [SerializeField] private int rows = 5;

    [Header("Cell")]
    [SerializeField] private Vector2 cellSize = new(100f, 100f);

    [SerializeField] private Vector2 spacing = new(10f, 10f);

    [Header("Grid Rotation")]
    [Tooltip("Rotates the grid layout without rotating the spawned objects.")]
    [SerializeField] private float rotation = 30f;

    [Header("Transform")]
    [SerializeField] private Vector2 gridPosition = Vector2.zero;

    [Header("Prefab")]
    [SerializeField] private RectTransform prefab;

    [SerializeField] private RectTransform spawnParent;

    [Header("Spawn")]
    [SerializeField] private bool clearBeforeSpawn = true;

    [SerializeField] private bool spawnOnStart;

    private readonly List<RectTransform> spawnedObjects = new();

    private RectTransform Root =>
        spawnParent != null
            ? spawnParent
            : (RectTransform)transform;

    private void Start()
    {
        if (spawnOnStart)
            SpawnGrid();
    }

    [ContextMenu("Spawn Grid")]
    public void SpawnGrid()
    {
        if (prefab == null)
        {
            Debug.LogWarning($"{nameof(UIGridSpawner)}: Prefab is not assigned.", this);
            return;
        }

        if (clearBeforeSpawn)
            ClearGrid();

        RectTransform root = Root;

        root.anchoredPosition = gridPosition;

        /*
         * We DON'T rotate root.
         *
         * Instead, calculate rotated basis vectors.
         *
         * right = direction of a row
         * up    = direction between rows
         */

        float radians = rotation * Mathf.Deg2Rad;

        Vector2 right = new Vector2(
            Mathf.Cos(radians),
            Mathf.Sin(radians)
        );

        Vector2 up = new Vector2(
            -Mathf.Sin(radians),
            Mathf.Cos(radians)
        );

        // Distance from one cell center to the next.
        Vector2 horizontalStep =
            right * (cellSize.x + spacing.x);

        Vector2 verticalStep =
            up * (cellSize.y + spacing.y);

        // Center the entire grid around the origin.
        Vector2 horizontalOffset =
            horizontalStep * ((columns - 1) * 0.5f);

        Vector2 verticalOffset =
            verticalStep * ((rows - 1) * 0.5f);

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                RectTransform cell = Instantiate(prefab, root);

                cell.name = $"Cell_{column}_{row}";

                Vector2 position =
                    horizontalStep * column +
                    verticalStep * row -
                    horizontalOffset -
                    verticalOffset;

                cell.anchoredPosition = position;

                // IMPORTANT:
                // Cell itself is never rotated.
                cell.localRotation = Quaternion.identity;

                cell.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    cellSize.x
                );

                cell.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    cellSize.y
                );

                spawnedObjects.Add(cell);
            }
        }
    }

    [ContextMenu("Clear Grid")]
    public void ClearGrid()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(spawnedObjects[i].gameObject);
                else
                    Destroy(spawnedObjects[i].gameObject);
#else
                Destroy(spawnedObjects[i].gameObject);
#endif
            }
        }

        spawnedObjects.Clear();
    }

    [ContextMenu("Refresh Grid")]
    public void RefreshGrid()
    {
        ClearGrid();
        SpawnGrid();
    }

    public RectTransform GetCell(int column, int row)
    {
        if (column < 0 || column >= columns)
            return null;

        if (row < 0 || row >= rows)
            return null;

        int index = row * columns + column;

        if (index >= spawnedObjects.Count)
            return null;

        return spawnedObjects[index];
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);

        cellSize.x = Mathf.Max(0, cellSize.x);
        cellSize.y = Mathf.Max(0, cellSize.y);
        spacing.x = Mathf.Max(0, spacing.x);
        spacing.y = Mathf.Max(0, spacing.y);
    }
#endif
}