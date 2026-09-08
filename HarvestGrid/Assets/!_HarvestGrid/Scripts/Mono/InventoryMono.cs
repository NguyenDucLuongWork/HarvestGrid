using LgTyLib.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventoryMono : BaseSingleton<InventoryMono>
{
    [SerializeField]
    private Dictionary<string, int> itemUsesAccumilation = new();

    public Dictionary<string, int> ItemUsesAccumilation => itemUsesAccumilation;

    public void AddItemUses(string itemID, int uses)
    {
        if (string.IsNullOrEmpty(itemID))
            return;

        if (itemUsesAccumilation.TryGetValue(itemID, out int currentUses))
        {
            itemUsesAccumilation[itemID] = currentUses + uses;
        }
        else
        {
            itemUsesAccumilation.Add(itemID, uses);
        }
    }

    public int GetAndSubtractItemUses(string itemID, int requirement)
    {
        if (string.IsNullOrEmpty(itemID) || requirement <= 0)
            return 0;

        if (!itemUsesAccumilation.TryGetValue(itemID, out int currentUses))
            return 0;

        int used = Mathf.Min(currentUses, requirement);

        itemUsesAccumilation[itemID] = currentUses - used;

        return used;
    }
}