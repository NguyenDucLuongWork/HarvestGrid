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

        return true;
    }

    public bool RemovePlant(Plant plant)
    {
        if (plant == null || this.plant != plant)
            return false;

        this.plant = null;

        if (plantGameObject != null)
            plantGameObject.SetActive(false);

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
        if (original == null) return;

        slotID = original.slotID;
        soilQuality = original.soilQuality;
        soilMoisture = original.soilMoisture;

        if (original.plant == null)
        {
            if (plant != null)
            {
                plant = null;
                if (plantGameObject != null)
                    plantGameObject.SetActive(false);
            }
            return;
        }

        if (plant == null)
        {
            // No plant currently occupying this scene slot — spin up a fresh
            // Plant data instance and bind it to the slot's existing GameObject.
            plant = new Plant(original.plant)
            {
                gameObject = plantGameObject
            };

            if (plantGameObject != null)
            {
                plantGameObject.SetActive(plant.CurrentState != null);
                if (plant.CurrentState != null)
                {
                    plantGameObject.GetComponent<PlantMono>()?.UpdateSprite(plant.CurrentState.Sprite);
                }
            }
        }
        else
        {
            plant.CopyData(original.plant);
        }
    }
}