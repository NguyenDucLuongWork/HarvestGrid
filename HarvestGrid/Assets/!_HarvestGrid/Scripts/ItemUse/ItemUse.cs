using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public abstract class ItemUse : ICloneable<ItemUse>
{
    public abstract void Apply(ItemUseContext ctx);
    public abstract ItemUse Clone();
}

public class ItemUseContext
{
    public Item Item;
    public FarmSlot TargetSlot;
    public GameObject Actor;
}

