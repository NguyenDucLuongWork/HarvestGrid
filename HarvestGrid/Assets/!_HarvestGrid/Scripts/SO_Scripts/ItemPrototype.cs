using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItemPrototype", menuName = "Scriptable Objects/ItemPrototype")]
public class ItemPrototype : ScriptableObject
{
    [Header("Init Config")]
    public List<ItemUseType> useConfig;
    [Header("Item")]
    public Item item;

    [ContextMenu("Init Item")]
    public void InitItem()
    {
        var newUses = new List<ItemUse>();
        foreach (var type in useConfig)
        {
            var use = ItemUseFactory.Create(type);
            if (use != null)
                newUses.Add(use);
        }
        item.SetItemUse(newUses);
    }
}
