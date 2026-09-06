using HarvestGrid.Farming;
using HarvestGrid.Farming.Data;
using HarvestGrid.Item.Data;
using HarvestGrid.System;

namespace HarvestGrid.Item.Strategy
{
    public class ResourceItemStrategy : IItemUsageStrategy
    {
        private Resource resource;
        private int amount;

        public ResourceItemStrategy(Resource resource, int amount)
        {
            this.resource = resource;
            this.amount = amount;
        }

        public void Execute(ItemUsage context, Plant target)
        {
            ItemEffectData effect = new ItemEffectData(ItemEffectType.AddResource, resource, amount, target);
            ItemSystem.Instance.ProcessEffect(effect);
        }
    }
}
