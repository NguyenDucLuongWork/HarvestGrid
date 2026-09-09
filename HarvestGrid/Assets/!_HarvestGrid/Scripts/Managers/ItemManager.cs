using LgTyLib.Core;
using LgTyLib.Modules.DataPersistence;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemManager : BaseSingleton<ItemManager>, IDataPersistence
{
    public List<ItemPrototype> items;
    public ItemMono itemPrefab;
    [SerializeField]
    private List<ItemMono> itemMonoList;
    public void SpawnItem(ItemPrototype itemPrototype)
    {
        ItemMono newItem = Instantiate(itemPrefab);
        newItem.Init(itemPrototype.item.Clone());
        newItem.Run();
    }

    
    public void SpawnRandom()
    {
        int index = Random.Range(0, items.Count);
        SpawnItem(items[index]);
    }
    public void SpawnItem(Item item)
    {
        ItemMono newItem = Instantiate(itemPrefab);
        newItem.Init(item.Clone());
        itemMonoList.Add(newItem);
        newItem.Run();
    }

    public void LoadGame(GameData gameData)
    {
        itemMonoList = new List<ItemMono>();
        List<Item> items = gameData.items;
        foreach (Item item in items) { 
            SpawnItem(item);
        }
    }

    public void SaveGame(ref GameData gameData)
    {
        var itemObjects =  FindObjectsByType<ItemMono>();
        List<Item> itemList = new List<Item>();
        foreach (ItemMono itemMono in itemObjects) {
            itemList.Add(itemMono.Item);
        }
        gameData.items = itemList;
    }

    // TODO: update, change to strategy pattern
    public void Use(Item item)
    {

    }

    private void Use(Item item, ItemUsesType type, int value)
    {
        switch (type) {
            case ItemUsesType.Harvesting:
                Debug.Log("Harvesting with value: " + value);
                break;

            case ItemUsesType.ResourceProviding:
                FarmMono.Instance.ProvideResource(item, type, value);
                break;

            case ItemUsesType.Accumilation:
                InventoryMono.Instance.AddItemUses(item.ToString(), value);
                break;

            default:
                Debug.LogWarning($"Unhandled item use type: {type}");
                break;
        }
    }
}