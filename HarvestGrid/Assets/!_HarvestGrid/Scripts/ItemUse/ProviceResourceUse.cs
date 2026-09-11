using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


[Serializable]
[ItemUseType(ItemUseType.ProviceResource)]
public class ProviceResourceUse : ItemUse
{
    [field:SerializeField]
    private Resource resource;
    [field: SerializeField]
    private int amount;

    public Resource Resource => resource;
    public int Amount => amount;

    public ProviceResourceUse() { }
    public ProviceResourceUse(ProviceResourceUse original)
    {
        this.resource = original.resource;
        this.amount = original.amount;
    }

    public override void Apply(ItemUseContext ctx) {
        
        FarmMono.Instance.ProvidingResourceToRandom(resource, amount);
    }

    public override ItemUse Clone()
        => new ProviceResourceUse(this);
}