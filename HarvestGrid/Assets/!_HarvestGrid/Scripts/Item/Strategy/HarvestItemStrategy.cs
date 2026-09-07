using HarvestGrid.Farming;
using HarvestGrid.Item.Data;
using HarvestGrid.System;

namespace HarvestGrid.Item.Strategy
{
    public class HarvestItemStrategy : IItemUsageStrategy
    {
        public void Execute(ItemUsage context, Plant target)
        {
            ItemEffectData effect = new ItemEffectData(ItemEffectType.Harvest, null, 0, target);
            ItemSystem.Instance.ProcessEffect(effect);
        }
    }
}
