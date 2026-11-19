using GameJson;
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
    [ContextData]
    public Item Item;
    [ContextData]
    [SerializeReference]
    public ItemUse Use;          // the single use this effect applies
    [ContextData]
    public FarmSlot TargetSlot;
    public GameObject Actor;
}

[Serializable]
public class ItemEffect
{
    [ContextData]
    public ItemUseContext useContext;
    [ContextData]
    public float remainingTime;
    [ContextData]
    public string farmSlotID;
}
