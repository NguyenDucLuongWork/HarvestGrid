using UnityEngine;

namespace HarvestGrid.Item.Data
{
    [CreateAssetMenu(fileName = "NewItemData", menuName = "HarvestGrid/Item/ItemData")]
    public class ItemData : ScriptableObject
    {
        public string id;
        public string itemName;
        public Sprite icon;
    }
}
