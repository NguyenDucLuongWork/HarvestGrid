using System;
using UnityEngine;

[Serializable]
public class FarmSlot : ICloneable<FarmSlot>
{
    //Clonable
    [SerializeField]
    private string slotID;
    [SerializeField]
    private SoilQuality soilQuality;
    [SerializeField]
    private SoilMoisture soilMoisture;

    public string SlotID => slotID;
    public SoilQuality SoilQuality => soilQuality;
    public SoilMoisture SoilMoisture => soilMoisture;

    //Not cloneable
    private Plant plant;
    public Plant Plant => plant;

    public GameObject slotGameObject;
    public GameObject plantGameObject;

    public FarmSlot(FarmSlot original)
    {
        this.slotID = original.SlotID;
        this.soilQuality = original.SoilQuality;
        this.soilMoisture = original.SoilMoisture;
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

    public bool AddPlant(Plant plant)
    {
        if (plant == null)
            return false;

        if (this.plant != null)
            return false;

        this.plant = plant;
        return true;
    }

    public bool RemovePlant(Plant plant)
    {
        if (plant == null || this.plant != plant)
            return false;

        this.plant = null;
        return true;
    }

#if !UnityEditor
    public void SetID(string id)
    {
        slotID = id;
    }
#endif
}