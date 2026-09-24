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
        newItemMono.transform.localScale = Vector3.one;
        newItemMono.Init(item);
        newItemMono.Run();
    }

    public ItemWithFootprintMono SpawnItemMonoWithFootprint(StoredObject storedObject)
    {
        ItemWithFootprintMono newItem = Instantiate(itemWithFootprintPrefab, itemPlaceHolder);
        newItem.ForceAddToInventory(storedObject);
        newItem.LateSnap();
        return newItem;
    }
}