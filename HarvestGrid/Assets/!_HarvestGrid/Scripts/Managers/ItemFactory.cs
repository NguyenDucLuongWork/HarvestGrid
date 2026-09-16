using LgTyLib.Core;
using LgTyLib.Modules.DataPersistence;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemFactory : BaseSingleton<ItemFactory>
{
    public ItemMono itemPrefab;
    public ItemWithFootprintMono itemWithFootprintPrefab;
    public Transform itemPlaceHolder;

    public void SpawnItemWithoutClone(Item item)
    {
        ItemMono newItemMono = Instantiate(itemPrefab);
        newItemMono.Init(item);
        Debug.Log(InventoryMono.Instance.Inventory.Items.Count);
        newItemMono.Run();
    }

    public void SpawnItemWithFootprint(StoredObject storedObject)
    {
        ItemWithFootprintMono newItem = Instantiate(itemWithFootprintPrefab, itemPlaceHolder);
        newItem.ForceAddToInventory(storedObject);
    }
}