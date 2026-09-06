using UnityEngine;
using HarvestGrid.Item;
using HarvestGrid.Farming;

public partial class InventoryBase
{
    public void UseItem(Item item, Plant target = null)
    {
        if (item == null) return;

        ItemUsage itemUsage = item.GetComponent<ItemUsage>();
        if (itemUsage != null)
        {
            // TODO: In the future, logic to automatically find a Plant target can be added here if target == null
            // For example: target = PlantSystem.Instance.FindPlantAt(item.transform.position);
            
            itemUsage.Use(target);
        }
        else
        {
            Debug.LogWarning($"Item {item.gameObject.name} does not have an ItemUsage component!");
        }
    }
}
