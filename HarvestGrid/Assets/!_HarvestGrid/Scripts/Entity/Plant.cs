using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Plant : ICloneable<Plant>
{
    [SerializeField]
    private string plantID;
    [SerializeField]
    private string plantName;
    [SerializeField]
    private PlantStage currentState;
    [SerializeField]
    private Dictionary<Resource, int> nextStageRequirement;
    [SerializeField]
    private List<PlantStage> stages; // remaining stages, in growth order

    public GameObject gameObject;

    public string PlantID => plantID;
    public string PlantName => plantName;
    public PlantStage CurrentState => currentState;
    public List<PlantStage> Stages => stages;
    public Dictionary<Resource, int> NextStageRequirement => nextStageRequirement;
    public bool IsFullyGrown => stages.Count == 0;

    public Plant(string plantID, string plantName, List<PlantStage> stages)
    {
        this.plantID = plantID;
        this.plantName = plantName;
        this.stages = new List<PlantStage>();

        foreach (var stage in stages)
        {
            this.stages.Add(stage.Clone());
        }

        // Start "pre-growth": current state is null until first GrowToNextStage call,
        // or seed it with the first stage immediately if you want plants to spawn already visible.
        currentState = null;
        nextStageRequirement = this.stages.Count > 0
            ? new Dictionary<Resource, int>(this.stages[0].RequiredResources)
            : null;
    }

    public Plant(Plant original)
    {
        this.plantID = original.plantID;
        this.plantName = original.plantName;
        this.currentState = original.currentState?.Clone();
        this.stages = new List<PlantStage>();

        foreach (var stage in original.stages)
        {
            stages.Add(stage.Clone());
        }

        this.nextStageRequirement = original.nextStageRequirement != null
            ? new Dictionary<Resource, int>(original.nextStageRequirement)
            : null;
    }

    public Plant Clone()
    {
        return new Plant(this);
    }

    /// <summary>
    /// Advances the plant to the next stage in the queue, if one is available.
    /// </summary>
    public bool GrowToNextStage()
    {
        if (stages.Count == 0)
        {
            Debug.LogWarning($"Plant '{plantName}' has no further stages to grow into.");
            return false;
        }

        // The next stage in the queue becomes the current stage.
        currentState = stages[0];
        stages.RemoveAt(0);

        // Requirement now points to whatever comes after this new current stage.
        nextStageRequirement = stages.Count > 0
            ? new Dictionary<Resource, int>(stages[0].RequiredResources)
            : null;

        return true;
    }

    /// <summary>
    /// Checks whether the given available resources satisfy the requirement
    /// for advancing to the next stage.
    /// </summary>
    public bool CanGrow(Dictionary<Resource, int> availableResources)
    {
        if (nextStageRequirement == null) return false;

        foreach (var kvp in nextStageRequirement)
        {
            if (!availableResources.TryGetValue(kvp.Key, out int available) || available < kvp.Value)
            {
                return false;
            }
        }

        return true;
    }
}