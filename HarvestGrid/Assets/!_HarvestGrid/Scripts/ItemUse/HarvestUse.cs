using System;
using UnityEngine;

[Serializable]
[ItemUseType(ItemUseType.Harvest)]
public class HarvestUse : ItemUse
{
    public override ItemUseType UseType => ItemUseType.Harvest;
    [SerializeField]
    private float effective;

    public float Effective => effective;

    public HarvestUse()
    {
    }

    public HarvestUse(HarvestUse original)
    {
        this.effective = original.Effective;
    }

    public override void Apply(ItemUseContext ctx) {
        FarmMono.Instance.HarvestRandom(ctx.TargetSlot, effective);
    }

    //public override ItemUse Clone()
    //{
    //    return new HarvestUse(this);
    //}
}
