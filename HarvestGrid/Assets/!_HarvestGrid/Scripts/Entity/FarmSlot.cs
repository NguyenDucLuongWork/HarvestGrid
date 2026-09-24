using System;
using UnityEngine;

[Serializable]
public class FarmSlot : ICloneable<FarmSlot>
{
    [SerializeField]
    private string slotID;

    [SerializeField]
    private SoilQuality soilQuality;

    [SerializeField]
    private SoilMoisture soilMoisture;

    [SerializeField]
    private Plant plant;

    public string SlotID => slotID;
    public SoilQuality SoilQuality => soilQuality;
    public SoilMoisture SoilMoisture => soilMoisture;

    public Plant Plant
    {
        get
        {
            if (plant != null && string.IsNullOrEmpty(plant.PlantID))
            {
                Debug.LogWarning(
                    $"[FarmSlot] Plant on slot '{slotID}' has an empty PlantID. Clearing plant reference."
                );

                plant = null;
            }

            return plant;
        }
    }

    public GameObject slotGameObject;
    public GameObject plantGameObject;

    public FarmSlot(FarmSlot original)
    {
        slotID = original.SlotID;
        soilQuality = original.SoilQuality;
        soilMoisture = original.SoilMoisture;
    }

    public FarmSlot Clone()
    {
        return new FarmSlot(this);
    }

    public FarmSlot CloneWithPlant()
    {
        FarmSlot newFarmSlot = new FarmSlot(this);
        newFarmSlot.plant = plant;
        return newFarmSlot;
    }

    public bool IsEmpty()
    {
        return Plant == null;
    }

    public bool AddPlant(Plant plant)
    {
        if (plant == null)
        {
            Debug.LogError("Cannot add null plant.");
            return false;
        }

        if (!IsEmpty())
        {
            Debug.LogError($"Slot '{slotID}' already has a plant.");
            return false;
        }

        this.plant = plant;

        plant.BeginGrow(plantGameObject);

        plantGameObject
            ?.GetComponent<PlantMono>()
            ?.UpdatePlantVisual(plant);

        return true;
    }

    public bool RemovePlant(Plant plant)
    {
        if (plant == null || this.plant != plant)
            return false;

        this.plant = null;

        if (plantGameObject != null)
        {
            PlantMono plantMono = plantGameObject.GetComponent<PlantMono>();

            if (plantMono != null)
                plantMono.UpdatePlantVisual(null);
            else
                plantGameObject.SetActive(false);
        }

        return true;
    }


#if !UnityEditor
    public void SetID(string id)
    {
        slotID = id;
    }
#endif

    /// <summary>
    /// Copies data from another FarmSlot (soil + plant), preserving this slot's
    /// slotGameObject/plantGameObject references.
    /// </summary>
    public void CopyData(FarmSlot original)
    {
        if (original == null)
            return;

        slotID = original.slotID;
        soilQuality = original.soilQuality;
        soilMoisture = original.soilMoisture;

        if (original.plant == null)
        {
            plant = null;

            if (plantGameObject != null)
            {
                PlantMono plantMono = plantGameObject.GetComponent<PlantMono>();

                if (plantMono != null)
                    plantMono.UpdatePlantVisual(null);
                else
                    plantGameObject.SetActive(false);
            }

            return;
        }

        if (plant == null)
        {
            plant = new Plant(original.plant)
            {
                gameObject = plantGameObject
            };
        }
        else
        {
            plant.CopyData(original.plant);
        }

        plant.gameObject = plantGameObject;

        plantGameObject
            ?.GetComponent<PlantMono>()
            ?.UpdatePlantVisual(plant);
    }
}