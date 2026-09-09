using System;
using UnityEngine;

[Serializable]
[ItemUseType(ItemUseType.Harvest)]
public class HarvestUse : ItemUse
{
    [SerializeField] private AnimationCurve qualityToStarChance;

    public override void Apply(ItemUseContext ctx) { /* ... */ }

    public override ItemUse Clone()
    {
        var c = new HarvestUse();
        c.qualityToStarChance = new AnimationCurve(qualityToStarChance.keys);
        return c;
    }
}
