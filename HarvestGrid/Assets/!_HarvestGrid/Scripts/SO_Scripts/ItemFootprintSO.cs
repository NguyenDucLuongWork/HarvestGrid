using UnityEngine;

[CreateAssetMenu(
    fileName = "ItemFootprintSO",
    menuName = "Scriptable Objects/ItemFootprintSO"
)]
public class ItemFootprintSO : ScriptableObject
{
    [Header("Size")]
    [Min(1)]
    public int width = 1;

    [Min(1)]
    public int height = 1;

    [Header("Editor Grid")]
    [SerializeField]
    private bool[] grid;

    [Header("Saved Footprint")]
    public Footprint footprint;

    public bool GetCell(int x, int y)
    {
        if (grid == null)
            return false;

        return grid[y * width + x];
    }

    public void SetCell(int x, int y, bool value)
    {
        grid[y * width + x] = value;
    }

    public void InitializeGrid()
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);

        grid = new bool[width * height];
    }

    public void SaveToFootprint()
    {
        bool[,] requiring = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // grid[] is stored top-row-first (screen space);
                // Footprint expects y = 0 at the bottom (Cartesian/world space).
                requiring[x, y] = GetCell(x, height - 1 - y);
            }
        }

        footprint = new Footprint(requiring);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }

    public void LoadFromFootprint()
    {
        if (footprint == null || footprint.Requiring == null)
            return;

        width = footprint.Requiring.GetLength(0);
        height = footprint.Requiring.GetLength(1);

        grid = new bool[width * height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[y * width + x] = footprint.Requiring[x, height - 1 - y];
            }
        }
    }
}