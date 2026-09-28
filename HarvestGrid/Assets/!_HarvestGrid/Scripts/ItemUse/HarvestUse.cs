using System;
using UnityEngine;

[Serializable]
[ItemUseType(ItemUseType.Harvest)]
public class HarvestUse : ItemUse
{
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
        FarmMono.Instance.HarvestRandom(effective);
        if (LgTyLib.Modules.Audio.AudioManager.HasInstance)
        {
            LgTyLib.Modules.Audio.AudioManager.Instance.PlaySickleSound();
        }
    }

    public override ItemUse Clone()
    {
        return new HarvestUse(this);
    }
}
