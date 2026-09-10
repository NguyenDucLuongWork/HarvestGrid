using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItemPrototype", menuName = "Scriptable Objects/ItemPrototype")]
public class ItemPrototype : ScriptableObject
{
    [Header("Init Config")]
    public List<ItemUseConfigEntry> useConfig;

    [Header("Item")]
    public Item item;

    [ContextMenu("Init Item")]
    public void InitItem()
    {
        var newUses = new List<ItemUse>();

        foreach (var entry in useConfig)
        {
            var use = ItemUseFactory.Create(entry.type);
            if (use == null)
                continue;

            if (use is AddPlantUse addPlantUse && entry.plantPrototype != null)
            {
                addPlantUse.SetPlant(entry.plantPrototype.plant);
            }

            newUses.Add(use);
        }

        item.SetItemUse(newUses);
    }
}