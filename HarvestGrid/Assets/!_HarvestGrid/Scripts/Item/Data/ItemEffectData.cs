using HarvestGrid.Farming.Data;
using HarvestGrid.Farming;

namespace HarvestGrid.Item.Data
{
    public struct ItemEffectData
    {
        public ItemEffectType effectType;
        public Resource resource;
        public int amount;
        public Plant target;

        public ItemEffectData(ItemEffectType effectType, Resource resource = null, int amount = 0, Plant target = null)
        {
            this.effectType = effectType;
            this.resource = resource;
            this.amount = amount;
            this.target = target;
        }
    }
}
