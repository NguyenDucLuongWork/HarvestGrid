using System;
using UnityEngine;

[Serializable]
public class ItemUseConfigEntry
{
    public ItemUseType type;

    // Only relevant/shown when type == ItemUseType.AddPlant
    public PlantPrototype plantPrototype;
}