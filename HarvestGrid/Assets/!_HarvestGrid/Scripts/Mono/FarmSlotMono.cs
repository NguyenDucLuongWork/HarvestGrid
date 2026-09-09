using System.Collections.Generic;
using UnityEngine;

public class FarmSlotMono : MonoBehaviour
{
    [SerializeField]
    private FarmSlot farmSlot;
    public FarmSlot FarmSlot => farmSlot;

    private PlantMono plantMono;

    [SerializeField]
    private bool harvestActive;
    public bool HarvestActive => harvestActive;

    public void CachePlantMono()
    {
        plantMono = farmSlot.plantGameObject != null
            ? farmSlot.plantGameObject.GetComponent<PlantMono>()
            : null;
    }

    public bool AddPlant(Plant plant)
    {
        if (!farmSlot.AddPlant(plant))
            return false;

        if (plantMono == null) CachePlantMono();

        if (farmSlot.plantGameObject != null)
            farmSlot.plantGameObject.SetActive(true);

        plant.OnStageChanged += HandleStageChanged; // named method, so it CAN be removed later

        plant.BeginGrow();
        harvestActive = plant.IsFullyGrown;

        return true;
    }

    /// <summary>Attempts to advance the current plant one stage using the given resources.</summary>
    public bool TryGrow(Dictionary<Resource, int> availableResources)
    {
        Plant plant = farmSlot.Plant;
        if (plant == null || plant.IsFullyGrown) return false;
        if (!plant.CanGrow(availableResources)) return false;

        plant.BeginGrow();
        harvestActive = plant.IsFullyGrown;
        return true;
    }

    public Dictionary<Crop, int> Harvest()
    {
        Plant plant = farmSlot.Plant;
        if (plant == null || !plant.IsFullyGrown) return null;

        Dictionary<Crop, int> yieldResult = plant.Harvest();

        plant.OnStageChanged -= HandleStageChanged;
        farmSlot.RemovePlant(plant);
        harvestActive = false;

        if (farmSlot.plantGameObject != null)
            farmSlot.plantGameObject.SetActive(false);

        return yieldResult;
    }

    private void HandleStageChanged(PlantStage stage)
    {
        plantMono?.UpdateSprite(stage.Sprite);
    }
}