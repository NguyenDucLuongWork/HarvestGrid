
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlantStage : ICloneable<PlantStage>
{
    [SerializeField]
    private Sprite sprite;
    [SerializeField]
    private Dictionary<Resource, int> requiredResources;

    public Sprite Sprite => sprite;
    public Dictionary<Resource, int> RequiredResources => requiredResources;

    public PlantStage(PlantStage original)
    {
        this.sprite = original.sprite;
        this.requiredResources = new Dictionary<Resource, int>(original.requiredResources);
    }

    public PlantStage Clone()
    {
        return new PlantStage(this);
    }
}
