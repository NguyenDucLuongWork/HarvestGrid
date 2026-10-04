using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public abstract class ItemUse : ICloneable<ItemUse>
{
    public abstract ItemUseType UseType { get; }
    public abstract void Apply(ItemUseContext ctx);
    public abstract ItemUse Clone();
}

[Serializable]
public class ItemUseContext
{
    public Item Item;
    [SerializeReference]
    public ItemUse Use;          // the single use this effect applies
    public FarmSlot TargetSlot;
    public GameObject Actor;
    
}

[Serializable]
public class ItemEffect
{
    public ItemUseContext useContext;
    public float remainingTime;
    public string farmSlotID;
}