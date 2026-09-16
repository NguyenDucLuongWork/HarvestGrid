using System;
using UnityEngine;

[Serializable]
public class StoredObject : ICloneable<StoredObject>
{
    [SerializeField]
    private Item item;

    [SerializeField]
    private int rotated;

    [SerializeField]
    private Vector2Int pivot;

    public Item Item
    {
        get => item;
        set => item = value;
    }

    public int Rotated
    {
        get => rotated;
        set => rotated = value;
    }

    public Vector2Int Pivot
    {
        get => pivot;
        set => pivot = value;
    }

    public StoredObject()
    {
    }

    public StoredObject(Item item, int rotated, Vector2Int pivot)
    {
        this.item = item;
        this.rotated = rotated;
        this.pivot = pivot;
    }

    public StoredObject(StoredObject original)
    {
        item = original.Item.Clone();
        rotated = original.Rotated;
        pivot = original.Pivot;
    }

    public StoredObject Clone()
    {
        return new StoredObject(this);
    }
}