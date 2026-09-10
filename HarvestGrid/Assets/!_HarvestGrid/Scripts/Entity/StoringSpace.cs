using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StoringSpace : ICloneable<StoringSpace>
{
    private StoringCellType[,] cells;

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
        int width = original.cells.GetLength(0);
        int height = original.cells.GetLength(1);

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
}