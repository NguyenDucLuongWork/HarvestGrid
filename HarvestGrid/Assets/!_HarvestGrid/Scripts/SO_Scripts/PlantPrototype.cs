using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlantPrototype", menuName = "Scriptable Objects/PlantPrototype")]
public class PlantPrototype : ScriptableObject
{
    public Plant plant;

    // ------------------------------------------------------------ null safety
    /// <summary>Creates the plant if it is null (new asset).</summary>
    public void EnsureInitialized()
    {
        plant ??= new Plant(string.Empty, string.Empty, 3, new List<PlantStage>());
    }

    private void OnEnable() => EnsureInitialized();

    // ------------------------------------------------------------ rebuild helpers
    // Plant has no setters, so the plant is rebuilt through its constructor.
    // The constructor resets the growth state: currentState = null and
    // nextStageRequirement = a copy of the first stage's RequiredResources.

    /// <summary>Re-syncs nextStageRequirement with the first stage. Call after editing the stages.</summary>
    [ContextMenu("Sync Growth State")]
    public void SyncGrowthState()
    {
        EnsureInitialized();
        plant = new Plant(plant.PlantID, plant.PlantName, plant.AverageCropCount, CopyStages(), plant.Crop);
    }

    /// <summary>Assigns the crop (null clears it) and re-syncs the growth state.</summary>
    public void SetCrop(Crop crop)
    {
        EnsureInitialized();
        plant = new Plant(plant.PlantID, plant.PlantName, plant.AverageCropCount, CopyStages(), crop);
    }

    private List<PlantStage> CopyStages()
    {
        var list = new List<PlantStage>();
        if (plant.Stages == null) return list;
        foreach (var stage in plant.Stages)
            if (stage != null) list.Add(stage);   // the Plant constructor clones them
        return list;
    }
}