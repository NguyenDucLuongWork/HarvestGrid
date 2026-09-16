using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Footprint : ICloneable<Footprint>, ISerializationCallbackReceiver
{
    // --------------------------------------------------
    // SERIALIZED BACKING FIELDS
    // --------------------------------------------------
    // Unity's serializer does NOT support multidimensional arrays
    // (bool[,]) - it silently drops them, same issue as StoringSpace.
    // We serialize a flat 1D array + width/height instead, and
    // rebuild the 2D array at runtime via ISerializationCallbackReceiver.

    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private bool[] requiringFlat;

    // Runtime-only 2D view, rebuilt from requiringFlat after deserialization.
    private bool[,] requiring;

    public bool[,] Requiring => requiring;

    public GameObject gameObject;

    // Unity's serializer instantiates [Serializable] fields without
    // running the constructor you'd expect. Without this, `requiring`
    // stays null on a freshly deserialized instance and any access
    // throws a NullReferenceException.
    public Footprint() : this(new bool[0, 0])
    {
    }

    public Footprint(bool[,] requiring)
    {
        this.requiring = requiring;
    }

    public Footprint(Footprint original)
    {
        int width = original.requiring.GetLength(0);
        int height = original.requiring.GetLength(1);

        requiring = new bool[width, height];

        Array.Copy(original.requiring, requiring, original.requiring.Length);
    }

    public Footprint Clone()
    {
        return new Footprint(this);
    }

    // --------------------------------------------------
    // SERIALIZATION CALLBACKS
    // --------------------------------------------------

    public void OnBeforeSerialize()
    {
        if (requiring == null)
        {
            requiringFlat = Array.Empty<bool>();
            width = 0;
            height = 0;
            return;
        }

        width = requiring.GetLength(0);
        height = requiring.GetLength(1);
        requiringFlat = new bool[width * height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                requiringFlat[y * width + x] = requiring[x, y];
            }
        }
    }

    public void OnAfterDeserialize()
    {
        requiring = new bool[width, height];

        if (requiringFlat != null && requiringFlat.Length == width * height)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    requiring[x, y] = requiringFlat[y * width + x];
                }
            }
        }
    }

    public List<Vector2Int> ToSpace(Vector2Int bottomLeftPivot)
    {
        List<Vector2Int> spaces = new();

        int width = requiring.GetLength(0);
        int height = requiring.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!requiring[x, y])
                    continue;

                spaces.Add(bottomLeftPivot + new Vector2Int(x, y));
            }
        }

        return spaces;
    }

    public void Rotate()
    {
        int width = requiring.GetLength(0);
        int height = requiring.GetLength(1);

        bool[,] rotated = new bool[height, width];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                rotated[y, width - 1 - x] = requiring[x, y];
            }
        }

        requiring = rotated;
    }

}