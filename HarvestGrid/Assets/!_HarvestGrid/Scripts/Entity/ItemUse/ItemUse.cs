using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public abstract class ItemUse : ICloneable<ItemUse>
{
    public AudioClip sfxClip;

    public abstract ItemUseType UseType { get; }
    public abstract void Apply(ItemUseContext ctx);

    public virtual ItemUse Clone()
    {
        return (ItemUse)MemberwiseClone();
    }
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