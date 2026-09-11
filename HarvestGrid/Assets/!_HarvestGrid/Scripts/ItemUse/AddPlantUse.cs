using System;
using UnityEngine;

[Serializable]
[ItemUseType(ItemUseType.AddPlant)]
public class AddPlantUse : ItemUse
{
    [SerializeField]
    private Plant plantToAdd;

    public AddPlantUse() { }

    public AddPlantUse(AddPlantUse original)
    {
        this.plantToAdd = original.plantToAdd.Clone();
    }

    public void SetPlant(Plant plant)
    {
        plantToAdd = plant;
    }

    public override void Apply(ItemUseContext ctx)
    {
        Debug.Log("Planting");
        FarmMono.Instance.PlantToRandomSlot(plantToAdd.Clone());
    }

    public override ItemUse Clone()
    {
        var c = new AddPlantUse(this);
        return c;
    }
}