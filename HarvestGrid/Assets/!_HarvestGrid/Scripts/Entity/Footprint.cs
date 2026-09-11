using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Footprint : ICloneable<Footprint>
{
    private bool[,] requiring;

    public bool[,] Requiring => requiring;

    public GameObject gameObject;

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