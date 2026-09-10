using LgTyLib.Core;
using LgTyLib.Modules.DataPersistence;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemFactory : BaseSingleton<ItemFactory>
{
    public ItemMono itemPrefab;

    public void SpawnRandom()
    {
        var itemAndChancePool = GameplayScene.Instance.LevelSO.itemAndChancePool;

        if (itemAndChancePool == null || itemAndChancePool.Count == 0)
        {
            Debug.LogWarning("Item spawn pool is empty.");
            return;
        }

        int index = Random.Range(0, itemAndChancePool.Count);

        ItemPrototype item = itemAndChancePool.Keys.ElementAt(index);
        SpawnItem(item);
    }

    public void SpawnItem(ItemPrototype itemPrototype)
    {
        SpawnItem(itemPrototype.item);
    }

    public void SpawnItem(Item item)
    {
        ItemMono newItem = Instantiate(itemPrefab);
        newItem.Init(item.Clone());
        InventoryMono.Instance.Inventory.AddItem(newItem.Item);
        Debug.Log(InventoryMono.Instance.Inventory.Items.Count);
        newItem.Run();
    }
}