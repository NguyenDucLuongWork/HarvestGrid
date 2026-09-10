using UnityEngine;

[CreateAssetMenu(fileName = "StoringSpaceSO", menuName = "Scriptable Objects/StoringSpaceSO")]
public class StoringSpaceSO : ScriptableObject
{
    [Header("Grid Size")]
    [Min(1)] public int width = 4;
    [Min(1)] public int height = 4;

    public StoringSpace storingSpace;

    /// <summary>
    /// Creates a brand new StoringSpace using the current width/height.
    /// Wipes out any existing cell data / stored footprints.
    /// </summary>
    public void CreateGrid()
    {
        storingSpace = new StoringSpace(width, height);
    }

    /// <summary>
    /// True if a grid has been created and its dimensions match
    /// the current width/height fields.
    /// </summary>
    public bool HasValidGrid()
    {
        return storingSpace != null &&
               storingSpace.Cells != null &&
               storingSpace.Cells.GetLength(0) == width &&
               storingSpace.Cells.GetLength(1) == height;
    }
}