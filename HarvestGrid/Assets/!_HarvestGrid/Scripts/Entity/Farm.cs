using LgTyLib.Modules.DataPersistence;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Farm : ICloneable<Farm>
{
    [SerializeField]
    private string farmID;
    [SerializeField]
    private List<FarmSlot> farmSlots;
    public string FarmID => farmID;
    public List<FarmSlot> FarmSlots => farmSlots;

    public GameObject gameObject;

    public Farm()
    {
        this.farmSlots = new List<FarmSlot>();
    }

    public Farm(Farm original)
    {
        this.farmID = original.FarmID;
        this.farmSlots = new List<FarmSlot>();
        for (int i = 0; i < original.FarmSlots.Count; i++)
        {
            this.farmSlots.Add(original.FarmSlots[i].CloneWithPlant());
        }
    }

    public Farm Clone()
    {
        return new Farm(this);
    }

    /// <summary>
    /// Removes any plant with a blank/missing ID from every slot.
    /// Guards against stale or malformed plant data (e.g. serialization artifacts).
    /// </summary>
    public int ValidateData()
    {
        int clearedCount = 0;

        if (farmSlots == null) return clearedCount;

        foreach (FarmSlot slot in farmSlots)
        {
            Plant plant = slot?.Plant;
            if (plant != null && string.IsNullOrEmpty(plant.PlantID))
            {
                slot.RemovePlant(plant);

                if (slot.plantGameObject != null)
                    slot.plantGameObject.SetActive(false);

                clearedCount++;
            }
        }

        return clearedCount;
    }

    public FarmSlot GetEmptySlot()
    {
        foreach (FarmSlot slot in farmSlots)
        {
            if (slot != null && slot.IsEmpty())
            {
                return slot;
            }
        }

        return null;
    }

    public List<Plant> GetPlants()
    {
        List<Plant> plant = new List<Plant>();
        foreach (FarmSlot slot in farmSlots)
        {
            if (!slot.IsEmpty())
            {
                plant.Add(slot.Plant);
            }
        }
        return plant;
    }

    public List<Plant> GetGrowablePlants()
    {
        List<Plant> allPlants = GetPlants();
        List<Plant> growablePlants = new List<Plant>();
        foreach (Plant plant in allPlants)
        {
            if (!plant.IsFullyGrown)
            {
                growablePlants.Add(plant);
            }
        }
        return growablePlants;
    }

    public List<Plant> GetHarvestablePlants()
    {
        List<Plant> allPlants = GetPlants();
        List<Plant> harvestablePlants = new List<Plant>();
        foreach (Plant plant in allPlants)
        {
            if (plant.IsFullyGrown)
            {
                harvestablePlants.Add(plant);
            }
        }
        return harvestablePlants;
    }

    public void SetFarmSlotForFarmSlotMono()
    {
        foreach(var farmSlot in farmSlots){
            FarmSlotMono farmSlotMono = farmSlot.slotGameObject.GetComponent<FarmSlotMono>();
            farmSlotMono.SetFarmSlot(farmSlot);
        }
    }

    public void CopyData(Farm farm)
    {
        if (farm == null || farm.farmSlots == null) return;

        farmID = farm.farmID;

        if (farmSlots == null)
        {
            farmSlots = new List<FarmSlot>();
        }

        // Index the scene's existing slots by ID so GameObject refs are preserved.
        var sceneSlotsByID = new Dictionary<string, FarmSlot>();
        foreach (var slot in farmSlots)
        {
            if (slot != null && !string.IsNullOrEmpty(slot.SlotID))
            {
                sceneSlotsByID[slot.SlotID] = slot;
            }
        }

        foreach (var loadedSlot in farm.farmSlots)
        {
            if (loadedSlot == null || string.IsNullOrEmpty(loadedSlot.SlotID))
                continue;

            if (sceneSlotsByID.TryGetValue(loadedSlot.SlotID, out var sceneSlot))
            {
                sceneSlot.CopyData(loadedSlot);
            }
            else
            {
                Debug.LogWarning($"Farm '{farmID}': loaded slot '{loadedSlot.SlotID}' has no matching scene slot; skipping (no GameObject to bind).");
            }
        }
    }
}