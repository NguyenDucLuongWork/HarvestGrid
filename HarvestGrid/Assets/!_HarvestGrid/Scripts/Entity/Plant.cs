using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Plant : ICloneable<Plant>
{
    // Props
    [SerializeField]
    private string plantID;
    [SerializeField]
    private string plantName;
    [SerializeField]
    private int averageCropCount = 3; // Base yield amount for a fully grown harvest
    [SerializeField]
    private PlantStage currentState;
    [SerializeField]
    private Dictionary<Resource, int> nextStageRequirement;
    [SerializeField]
    private List<PlantStage> stages; // remaining stages, in growth order
    [SerializeField]
    private Crop crop;

    // Getters
    public string PlantID => plantID;
    public string PlantName => plantName;
    public int AverageCropCount => averageCropCount;
    public PlantStage CurrentState => currentState;
    public Crop Crop => crop;

    public IReadOnlyList<PlantStage> Stages => stages;
    public IReadOnlyDictionary<Resource, int> NextStageRequirement => nextStageRequirement;

    public bool IsFullyGrown => stages.Count == 0;

    // Others

    public GameObject gameObject;
    public event Action<PlantStage> OnStageChanged;

    public Plant(string plantID, string plantName, int averageCropCount, List<PlantStage> stages, Crop crop = null)
    {
        this.plantID = plantID;
        this.plantName = plantName;
        this.averageCropCount = Mathf.Max(1, averageCropCount);
        this.crop = crop?.Clone();
        this.stages = new List<PlantStage>();

        if (stages != null)
        {
            foreach (var stage in stages)
            {
                this.stages.Add(stage.Clone());
            }
        }

        currentState = null;
        nextStageRequirement = this.stages.Count > 0
            ? new Dictionary<Resource, int>(this.stages[0].RequiredResources)
            : null;
    }

    public Plant(Plant original)
    {
        if (original == null) return;

        this.plantID = original.plantID;
        this.plantName = original.plantName;
        this.averageCropCount = original.averageCropCount;
        this.currentState = original.currentState?.Clone();
        this.crop = original.crop?.Clone();
        this.stages = new List<PlantStage>();

        if (original.stages != null)
        {
            foreach (var stage in original.stages)
            {
                stages.Add(stage.Clone());
            }
        }

        this.nextStageRequirement = original.nextStageRequirement != null
            ? new Dictionary<Resource, int>(original.nextStageRequirement)
            : null;
    }

    public Plant Clone()
    {
        return new Plant(this);
    }
    public bool BeginGrow()
    {
        bool grew = GrowToNextStage();

        if (grew)
        {
            OnStageChanged?.Invoke(currentState);
        }

        return grew;
    }
    public bool GrowToNextStage()
    {
        if (stages.Count == 0)
        {
            Debug.LogWarning($"Plant '{plantName}' has no further stages to grow into.");
            return false;
        }

        currentState = stages[0];
        stages.RemoveAt(0);

        nextStageRequirement = stages.Count > 0
            ? new Dictionary<Resource, int>(stages[0].RequiredResources)
            : null;

        return true;
    }

    public bool CanGrow(Dictionary<Resource, int> availableResources)
    {
        if (nextStageRequirement == null || availableResources == null) return false;

        foreach (var kvp in nextStageRequirement)
        {
            if (!availableResources.TryGetValue(kvp.Key, out int available) || available < kvp.Value)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Harvests the plant using averageCropCount scaled by efficiency, returning up to 2 distinct star-tier crops.
    /// </summary>
    /// <param name="efficiency">Base factor is 1.0f. Higher efficiency increases total yield and star quality.</param>
    public Dictionary<Crop, int> Harvest(float efficiency = 1f)
    {
        var result = new Dictionary<Crop, int>();

        if (crop == null || !IsFullyGrown)
        {
            Debug.LogWarning($"Plant '{plantName}' is not fully grown or has no crop assigned.");
            return result;
        }

        // Calculate total yield using averageCropCount scaled by efficiency
        int totalYield = Mathf.Max(1, Mathf.RoundToInt(averageCropCount * efficiency));

        // Determine primary star tier based on efficiency (clamped between 1 and 5)
        int baseStar = Mathf.Clamp(Mathf.FloorToInt(efficiency * 2f), 1, 5);

        // Determine second star tier (adjacent rating)
        int secondStar = (baseStar < 5) ? baseStar + 1 : baseStar - 1;
        if (secondStar < 1) secondStar = 1;

        // Split quantity between primary and secondary star qualities
        int primaryAmount = Mathf.CeilToInt(totalYield * 0.6f);
        int secondaryAmount = totalYield - primaryAmount;

        // Primary star crop yield
        Crop primaryCrop = crop.Clone();
        primaryCrop.SetStars(baseStar);
        result[primaryCrop] = primaryAmount;

        // Secondary star crop yield (if applicable)
        if (secondaryAmount > 0 && baseStar != secondStar)
        {
            Crop secondaryCrop = crop.Clone();
            secondaryCrop.SetStars(secondStar);
            result[secondaryCrop] = secondaryAmount;
        }

        return result;
    }
}