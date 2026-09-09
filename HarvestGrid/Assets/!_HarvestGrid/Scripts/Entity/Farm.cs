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
}