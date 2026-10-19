using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlantStage : ICloneable<PlantStage>
{
    [SerializeField]
    private Sprite sprite;

    // Serialized natively by Unity 6.6+ (opt-in with [SerializeField]).
    [SerializeField]
    private Dictionary<Resource, int> requiredResources = new();

    public Sprite Sprite => sprite;

    /// <summary>Never null.</summary>
    public Dictionary<Resource, int> RequiredResources => requiredResources ??= new Dictionary<Resource, int>();

    public PlantStage(Sprite sprite, Dictionary<Resource, int> requiredResources)
    {
        this.sprite = sprite;
        this.requiredResources = requiredResources != null
            ? new Dictionary<Resource, int>(requiredResources)
            : new Dictionary<Resource, int>();
    }

    public PlantStage(PlantStage original)
    {
        this.sprite = original.sprite;
        this.requiredResources = original.requiredResources != null
            ? new Dictionary<Resource, int>(original.requiredResources)
            : new Dictionary<Resource, int>();
    }

    public PlantStage Clone()
    {
        return new PlantStage(this);
    }
}