using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


[Serializable]
[ItemUseType(ItemUseType.Water)]
public class WaterUse : ItemUse
{
    [SerializeField] private int currentCharge;

    public override void Apply(ItemUseContext ctx) { /* ... */ }

    public override ItemUse Clone()
        => new WaterUse { currentCharge = currentCharge };
}