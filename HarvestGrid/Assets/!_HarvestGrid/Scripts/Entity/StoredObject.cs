using GameJson;
using System;
using UnityEngine;

[Serializable]
public class StoredObject : ICloneable<StoredObject>
{
    [ContextData]
    [SerializeField]
    private Item item;

    [ContextData]
    [SerializeField]
    private int rotated;

    [ContextData]
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
