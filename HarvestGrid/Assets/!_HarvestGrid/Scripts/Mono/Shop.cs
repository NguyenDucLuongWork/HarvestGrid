using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{
    [SerializeField]
    private List<ItemInShop> itemInShops;

    private void Start()
    {
        RerollShop();
    }

    public void RerollShop()
    {
        Dictionary<ItemPrototype, float> itemPool =
            GameplayScene.Instance.LevelSO.itemAndChancePool;

        if (itemPool == null || itemPool.Count == 0)
            return;

        List<KeyValuePair<ItemPrototype, float>> pool =
            new List<KeyValuePair<ItemPrototype, float>>(itemPool);

        int poolIndex = 0;

        foreach (ItemInShop itemInShop in itemInShops)
        {

            bool found = false;

            while (!found)
            {
                KeyValuePair<ItemPrototype, float> candidate = pool[poolIndex];

                // Move to next item, wrapping around
                poolIndex = (poolIndex + 1) % pool.Count;

                // New random value for every attempt
                float randomValue = Random.value;

                if (randomValue <= candidate.Value)
                {
                    itemInShop.SetData(candidate.Key.item.Clone());
                    found = true;
                }
            }
        }
    }
}