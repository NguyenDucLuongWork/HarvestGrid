using System;
using UnityEngine;

[Serializable]
[ItemUseType(ItemUseType.AddPlant)]
public class AddPlantUse : ItemUse
{
    public override ItemUseType UseType => ItemUseType.AddPlant;
    [SerializeField]
    private Plant plantToAdd;

    public AddPlantUse() { }
    public Plant PlantToAdd => plantToAdd;
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
        Debug.Log("Adding plant");
        FarmMono.Instance.AddPlant(plantToAdd.Clone(), ctx.TargetSlot);
    }

    public override ItemUse Clone()
    {
        var c = new AddPlantUse(this);
        return c;
    }
}