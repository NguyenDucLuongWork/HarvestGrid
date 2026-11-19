using System;
using System.Collections.Generic;
using UIGen;
using UnityEngine;

[Serializable]
public class ItemFootprintView
{
    [UIField(ControlType.Label, Label = "Item ID")]
    public string itemId = "scythe";

    [UIField(ControlType.Label, Label = "Item Name")]
    public string itemName = "Super Scythe";

    [UIField(ControlType.Label, Label = "Icon")]
    public string icon = "🌾";

    public Sprite iconSprite;

    [UIField(ControlType.Label, Label = "Width")]
    public int width = 2;

    [UIField(ControlType.Label, Label = "Height")]
    public int height = 2;

    [UIField(ControlType.Label, Label = "Rotation")]
    public int rotation = 0; // 0=0°, 1=90°, 2=180°, 3=270°

    [UIField(ControlType.ProgressBar, Label = "Cooldown Progress", Min = 0, Max = 1)]
    public float progress = 0f;

    // Row-major flat footprint: length = width * height.
    // true = cell is occupied; false = empty.
    public bool[] footprint;

    public ItemFootprintView()
    {
        width = 2;
        height = 2;
        footprint = new bool[] { true, true, true, false };
    }

    public ItemFootprintView(string id, string name, string icon, int w, int h, bool[] fp)
    {
        this.itemId = id;
        this.itemName = name;
        this.icon = icon;
        this.width = Mathf.Max(1, w);
        this.height = Mathf.Max(1, h);
        this.footprint = fp ?? new bool[width * height];
    }

    public bool IsCellOccupied(int col, int row)
    {
        if (footprint == null || col < 0 || col >= width || row < 0 || row >= height)
            return false;

        int index = row * width + col;
        return index >= 0 && index < footprint.Length && footprint[index];
    }
}

[CreateAssetMenu(fileName = "ItemFootprintViewSO", menuName = "Scriptable Objects/View/ItemFootprintViewSO")]
public class ItemFootprintViewSO : ScriptableObject
{
    [SerializeField]
    public ItemFootprintView itemFootprint = new ItemFootprintView();

    public void SetFromItem(Item item)
    {
        if (item == null) return;
        itemFootprint.itemId = item.Id;
        itemFootprint.itemName = item.Name;
        itemFootprint.iconSprite = item.Icon;

        if (item.Footprint != null && item.Footprint.Requiring != null)
        {
            int w = item.Footprint.Requiring.GetLength(0);
            int h = item.Footprint.Requiring.GetLength(1);
            itemFootprint.width = w;
            itemFootprint.height = h;
            itemFootprint.footprint = new bool[w * h];

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    // Cartesian (y up) to UI grid (row 0 at top)
                    int uiRow = h - 1 - y;
                    itemFootprint.footprint[uiRow * w + x] = item.Footprint.Requiring[x, y];
                }
            }
        }
    }
}
