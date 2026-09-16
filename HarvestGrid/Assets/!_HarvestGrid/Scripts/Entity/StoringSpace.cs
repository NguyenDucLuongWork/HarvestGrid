using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StoringSpace : ICloneable<StoringSpace>, ISerializationCallbackReceiver
{
    // --------------------------------------------------
    // SERIALIZED BACKING FIELDS
    // --------------------------------------------------
    // Unity's serializer does NOT support multidimensional arrays
    // (StoringCellType[,]) - it silently drops them (see UAC1009).
    // We serialize a flat 1D array + width/height instead, and
    // rebuild the 2D array at runtime via ISerializationCallbackReceiver.

    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private StoringCellType[] cellsFlat;

    // Runtime-only 2D view, rebuilt from cellsFlat after deserialization.
    private StoringCellType[,] cells;

    // Unity's native Dictionary serialization refuses key types that
    // are or contain an IEnumerable (UAC1013) - Footprint contains
    // bool[,]/bool[] arrays, so it can't be a serialized dictionary key.
    // We serialize two parallel lists instead and rebuild the runtime
    // dictionary via ISerializationCallbackReceiver, same as the cells array.
    [SerializeField] private List<Footprint> footprintKeys = new();
    [SerializeField] private List<Vector2Int> footprintPivots = new();

    private Dictionary<Footprint, Vector2Int> storedObjectAndBottomLeftPivot
        = new();

    public StoringCellType[,] Cells => cells;

    public IReadOnlyDictionary<Footprint, Vector2Int> StoredObjects => storedObjectAndBottomLeftPivot;

    // Unity's serializer instantiates [Serializable] fields without
    // running the constructor you'd expect (it can skip constructors
    // entirely). Without this, `cells` stays null on a freshly created
    // ScriptableObject and any access throws a NullReferenceException.
    public StoringSpace() : this(0, 0)
    {
    }

    public StoringSpace(int width, int height)
    {
        this.width = width;
        this.height = height;

        cells = new StoringCellType[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = StoringCellType.Available;
            }
        }
    }

    public StoringSpace(StoringSpace original)
    {
        width = original.width;
        height = original.height;

        // Deep copy cells
        cells = new StoringCellType[width, height];
        Array.Copy(original.cells, cells, original.cells.Length);

        // Copy dictionary
        storedObjectAndBottomLeftPivot = new();

        foreach (var pair in original.storedObjectAndBottomLeftPivot)
        {
            storedObjectAndBottomLeftPivot.Add(
                pair.Key.Clone(),
                pair.Value
            );
        }
    }

    public StoringSpace Clone()
    {
        return new StoringSpace(this);
    }

    // --------------------------------------------------
    // SERIALIZATION CALLBACKS
    // --------------------------------------------------

    public void OnBeforeSerialize()
    {
        // Flatten the 2D array into the serializable 1D array.
        if (cells == null)
        {
            cellsFlat = Array.Empty<StoringCellType>();
            width = 0;
            height = 0;
        }
        else
        {

            width = cells.GetLength(0);
            height = cells.GetLength(1);
            cellsFlat = new StoringCellType[width * height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    cellsFlat[y * width + x] = cells[x, y];
                }
            }

            footprintKeys.Clear();
            footprintPivots.Clear();

            foreach (var pair in storedObjectAndBottomLeftPivot)
            {
                footprintKeys.Add(pair.Key);
                footprintPivots.Add(pair.Value);
            }
        }
    }
    public void OnAfterDeserialize()
    {
        // Rebuild the 2D array from the flat serialized array.
        cells = new StoringCellType[width, height];

        if (cellsFlat != null && cellsFlat.Length == width * height)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    cells[x, y] = cellsFlat[y * width + x];
                }
            }
        }
    }

    // --------------------------------------------------
    // ADD
    // --------------------------------------------------

    public void AddFootprint(
        Footprint footprint,
        Vector2Int bottomLeftPivot)
    {
        foreach (Vector2Int position in footprint.ToSpace(bottomLeftPivot))
        {
            cells[position.x, position.y] = StoringCellType.Occupied;
        }

        storedObjectAndBottomLeftPivot[footprint] = bottomLeftPivot;
    }

    // --------------------------------------------------
    // REMOVE
    // --------------------------------------------------

    public void RemoveFootprint(Footprint footprint)
    {
        if (!storedObjectAndBottomLeftPivot.TryGetValue(
                footprint,
                out Vector2Int bottomLeftPivot))
        {
            return;
        }

        foreach (Vector2Int position in footprint.ToSpace(bottomLeftPivot))
        {
            if (IsInside(position))
            {
                cells[position.x, position.y] =
                    StoringCellType.Available;
            }
        }

        storedObjectAndBottomLeftPivot.Remove(footprint);
    }

    // --------------------------------------------------
    // TRY ADD
    // --------------------------------------------------

    public bool TryAddFootprint(
        Footprint footprint,
        Vector2Int bottomLeftPivot)
    {
        if (!CanPlaceFootprint(footprint, bottomLeftPivot))
            return false;

        AddFootprint(footprint, bottomLeftPivot);
        return true;
    }

    // --------------------------------------------------
    // CHECK ROTATION
    // --------------------------------------------------

    public bool CheckIfRotatable(Footprint footprint)
    {
        if (!storedObjectAndBottomLeftPivot.TryGetValue(
                footprint,
                out Vector2Int bottomLeftPivot))
        {
            return false;
        }

        // Temporarily rotate
        footprint.Rotate();

        bool canRotate =
            CanPlaceFootprint(
                footprint,
                bottomLeftPivot,
                footprintToIgnore: footprint);

        // Rotate back to original orientation
        footprint.Rotate();
        footprint.Rotate();
        footprint.Rotate();

        return canRotate;
    }

    // --------------------------------------------------
    // CHECK PLACEMENT
    // --------------------------------------------------

    private bool CanPlaceFootprint(
        Footprint footprint,
        Vector2Int bottomLeftPivot,
        Footprint footprintToIgnore = null)
    {
        foreach (Vector2Int position in
                 footprint.ToSpace(bottomLeftPivot))
        {
            if (!IsInside(position))
                return false;

            StoringCellType cell = cells[position.x, position.y];

            if (cell == StoringCellType.Unavailable)
                return false;

            if (cell == StoringCellType.Occupied)
            {
                // When checking rotation of an already stored
                // footprint, its current cells are allowed.
                if (footprintToIgnore == null)
                    return false;

                // Check whether this occupied cell belongs
                // to the footprint we're rotating.
                if (!IsPartOfFootprint(
                        footprintToIgnore,
                        position))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool IsPartOfFootprint(
        Footprint footprint,
        Vector2Int position)
    {
        if (!storedObjectAndBottomLeftPivot.TryGetValue(
                footprint,
                out Vector2Int pivot))
        {
            return false;
        }

        return footprint
            .ToSpace(pivot)
            .Contains(position);
    }

    private bool IsInside(Vector2Int position)
    {
        return position.x >= 0 &&
               position.y >= 0 &&
               position.x < cells.GetLength(0) &&
               position.y < cells.GetLength(1);
    }

    public void UpdateOccupiedDataFully(IReadOnlyList<StoredObject> storedObjects)
    {
        // --------------------------------------------------
        // Clear current occupied data
        // --------------------------------------------------

        storedObjectAndBottomLeftPivot.Clear();

        // Reset every cell to Available.
        // Preserve Unavailable cells.
        for (int x = 0; x < cells.GetLength(0); x++)
        {
            for (int y = 0; y < cells.GetLength(1); y++)
            {
                if (cells[x, y] == StoringCellType.Occupied)
                {
                    cells[x, y] = StoringCellType.Available;
                }
            }
        }

        if (storedObjects == null)
            return;

        // --------------------------------------------------
        // Rebuild occupied data
        // --------------------------------------------------

        foreach (StoredObject storedObject in storedObjects)
        {
            if (storedObject == null || storedObject.Item == null)
                continue;

            Footprint footprint = storedObject.Item.Footprint;

            if (footprint == null)
                continue;

            // Clone because we must NOT rotate the Item's original
            // footprint.
            Footprint rotatedFootprint = footprint.Clone();

            // Apply saved rotation.
            int rotationCount = storedObject.Rotated % 4;

            if (rotationCount < 0)
                rotationCount += 4;

            for (int i = 0; i < rotationCount; i++)
            {
                rotatedFootprint.Rotate();
            }

            Vector2Int pivot = storedObject.Pivot;

            // --------------------------------------------------
            // Validate footprint positions
            // --------------------------------------------------

            List<Vector2Int> positions =
                rotatedFootprint.ToSpace(pivot);

            bool valid = true;

            foreach (Vector2Int position in positions)
            {
                if (!IsInside(position))
                {
                    Debug.LogWarning(
                        $"[{nameof(StoringSpace)}] " +
                        $"Stored object '{storedObject.Item.Name}' " +
                        $"is outside storing space at {position}."
                    );

                    valid = false;
                    break;
                }

                if (cells[position.x, position.y] ==
                    StoringCellType.Unavailable)
                {
                    Debug.LogWarning(
                        $"[{nameof(StoringSpace)}] " +
                        $"Stored object '{storedObject.Item.Name}' " +
                        $"overlaps unavailable cell at {position}."
                    );

                    valid = false;
                    break;
                }

                if (cells[position.x, position.y] ==
                    StoringCellType.Occupied)
                {
                    Debug.LogWarning(
                        $"[{nameof(StoringSpace)}] " +
                        $"Stored object '{storedObject.Item.Name}' " +
                        $"overlaps another stored object at {position}."
                    );

                    valid = false;
                    break;
                }
            }

            if (!valid)
                continue;

            // --------------------------------------------------
            // Occupy cells
            // --------------------------------------------------

            foreach (Vector2Int position in positions)
            {
                cells[position.x, position.y] =
                    StoringCellType.Occupied;
            }

            // Store the rotated footprint and its bottom-left pivot.
            storedObjectAndBottomLeftPivot.Add(
                rotatedFootprint,
                pivot
            );
        }
    }
}