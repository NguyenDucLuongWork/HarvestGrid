using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


[Serializable]
[ItemUseType(ItemUseType.ProviceResource)]
public class ProviceResourceUse : ItemUse
{
    public override ItemUseType UseType => ItemUseType.ProviceResource;
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
        Debug.Log(ctx.Use.ToString());
        FarmMono.Instance.ProvidingResource(resource, amount, ctx.TargetSlot);

        if (LgTyLib.Modules.Audio.AudioManager.HasInstance)
        {
            if (resource == Resource.Water)
            {
                LgTyLib.Modules.Audio.AudioManager.Instance.PlayWateringSound();
            }
            else if (resource == Resource.Nutrients)
            {
                LgTyLib.Modules.Audio.AudioManager.Instance.PlayFertilizerSound();
            }
        }
    }

    public override ItemUse Clone()
        => new ProviceResourceUse(this);
}