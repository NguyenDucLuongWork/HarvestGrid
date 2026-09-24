using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

public class HarvestUsePanel : ItemUsePanel<HarvestUse>
{
    [SerializeField] private Text effectiveLabel;

    protected override void OnBind()
    {
        if (effectiveLabel != null)
            effectiveLabel.text = $"Effective: {Use.Effective:0.##}";
    }
}